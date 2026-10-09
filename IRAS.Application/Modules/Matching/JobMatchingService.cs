// IRAS.Application/Modules/Matching/JobMatchingService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Modules.Matching.DTOs;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace IRAS.Application.Modules.Matching
{
    public class JobMatchingService : IJobMatchingService
    {
        private readonly IrasDbContext _db;
        private readonly IScoringService _scoring;
        private readonly INotificationService _notifications;
        private readonly ScoringOptions _options;

        public JobMatchingService(
            IrasDbContext db, IScoringService scoring, INotificationService notifications,
            IOptions<ScoringOptions> options)
        {
            _db = db;
            _scoring = scoring;
            _notifications = notifications;
            _options = options.Value;
        }

        public async Task RunMatchingForJobAsync(int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs
                .Include(j => j.RequiredSkills).ThenInclude(rs => rs.Skill)
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(j => j.JobId == jobId, ct);
            if (job is null || job.Status != JobStatus.Published) return;

            var alreadyConsidered = await _db.JobMatches
                .Where(m => m.JobId == jobId)
                .Select(m => m.CandidateId)
                .ToListAsync(ct);

            var candidates = await _db.CandidateProfiles
                .Where(c => c.OptInMatching && !alreadyConsidered.Contains(c.CandidateId))
                .Select(c => new
                {
                    c.CandidateId,
                    ResumeText = c.Resumes
                        .Where(r => r.IsPrimary && r.ParsedText != null)
                        .Select(r => r.ParsedText)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            // Only candidates with a parsed resume carry a semantic signal — matching
            // without one would just be skill-only, which the reactive path (Module 6)
            // already gives once they apply.
            var eligible = candidates
                .Where(c => !string.IsNullOrWhiteSpace(c.ResumeText))
                .Select(c => (c.CandidateId, ResumeText: c.ResumeText!))
                .ToList();
            if (eligible.Count == 0) return;

            var candidateSkills = await _db.CandidateSkills
                .Where(cs => eligible.Select(e => e.CandidateId).Contains(cs.CandidateId))
                .Select(cs => new { cs.CandidateId, cs.SkillId })
                .ToListAsync(ct);

            var candidateSkillMap = candidateSkills
                .GroupBy(cs => cs.CandidateId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<int>)g.Select(cs => cs.SkillId).ToList());

            // One batched HTTP call to the AI service for every eligible candidate's resume
            // against this single job — not N sequential calls.
            Dictionary<int, MatchSignals> matchSignals;
            try
            {
                matchSignals = await _scoring.ComputeMatchSignalsAsync(job, eligible, ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                matchSignals = new Dictionary<int, MatchSignals>();
            }

            foreach (var (candidateId, _) in eligible)
            {
                var skillIds = candidateSkillMap.GetValueOrDefault(candidateId, Array.Empty<int>());
                var skillMatch = _scoring.ComputeSkillMatch(job.RequiredSkills, skillIds);
                var signals = matchSignals.GetValueOrDefault(candidateId, new MatchSignals(0m, null));
                var semanticSimilarity = signals.SemanticSimilarity;
                var matchScore = _scoring.ComputeTotalScore(skillMatch, semanticSimilarity, signals.MlFitScore);
                var passed = matchScore >= _options.AutoMatchThreshold;

                _db.JobMatches.Add(new JobMatch
                {
                    JobId = jobId,
                    CandidateId = candidateId,
                    MatchScore = matchScore,
                    ThresholdPassed = passed,
                    IsNotified = passed
                });

                if (passed)
                {
                    await _notifications.NotifyAsync(
                        candidateId, NotificationType.JobMatch, "New job match found",
                        $"\"{job.Title}\" at {job.Employer.CompanyName} looks like a strong match for your profile.",
                        RelatedEntityType.Job, jobId, DeliveryChannel.InApp, ct);
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<JobMatchDto>> GetMyMatchesAsync(int candidateId, CancellationToken ct)
        {
            var liveMatches = (await GetRecommendedJobsAsync(candidateId, ct))
                .Where(r => r.MatchScore >= _options.AutoMatchThreshold)
                .ToList();

            if (liveMatches.Count > 0)
            {
                var liveJobIds = liveMatches.Select(r => r.JobId).ToList();
                var existingMatches = await _db.JobMatches
                    .Where(m => m.CandidateId == candidateId && liveJobIds.Contains(m.JobId))
                    .ToListAsync(ct);
                var existingByJobId = existingMatches.ToDictionary(m => m.JobId);

                foreach (var liveMatch in liveMatches)
                {
                    if (existingByJobId.TryGetValue(liveMatch.JobId, out var existing))
                    {
                        existing.MatchScore = liveMatch.MatchScore;
                        existing.ThresholdPassed = true;
                        existing.IsNotified = true;
                    }
                    else
                    {
                        _db.JobMatches.Add(new JobMatch
                        {
                            JobId = liveMatch.JobId,
                            CandidateId = candidateId,
                            MatchScore = liveMatch.MatchScore,
                            ThresholdPassed = true,
                            IsNotified = true
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);
            }

            return await _db.JobMatches
                .Where(m => m.CandidateId == candidateId && m.ThresholdPassed)
                .OrderByDescending(m => m.MatchScore)
                .Select(m => new JobMatchDto
                {
                    MatchId = m.MatchId,
                    JobId = m.JobId,
                    JobTitle = m.Job.Title,
                    CompanyName = m.Job.Employer.CompanyName,
                    MatchScore = m.MatchScore,
                    ThresholdPassed = m.ThresholdPassed,
                    HasApplied = _db.Applications.Any(a => a.CandidateId == candidateId && a.JobId == m.JobId),
                    MatchedAt = m.MatchedAt
                })
                .ToListAsync(ct);
        }

        public async Task<List<JobRecommendationDto>> GetRecommendedJobsAsync(int candidateId, CancellationToken ct)
        {
            var candidate = await _db.CandidateProfiles
                .Where(c => c.CandidateId == candidateId)
                .Select(c => new
                {
                    ResumeText = c.Resumes
                        .Where(r => r.IsPrimary && r.ParsedText != null)
                        .Select(r => r.ParsedText)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            // No parsed primary resume — same precondition RunMatchingForJobAsync applies,
            // since skill-only recommendations without a free-text signal would be
            // misleadingly ranked (every job would tie or rank purely on skill overlap).
            if (candidate is null || string.IsNullOrWhiteSpace(candidate.ResumeText))
                return new List<JobRecommendationDto>();

            var candidateSkillIds = await _db.CandidateSkills
                .Where(cs => cs.CandidateId == candidateId)
                .Select(cs => cs.SkillId)
                .ToListAsync(ct);

            var jobs = await _db.Jobs
                .Where(j => j.Status == JobStatus.Published)
                .Include(j => j.RequiredSkills).ThenInclude(rs => rs.Skill)
                .Include(j => j.Employer)
                .ToListAsync(ct);
            var appliedJobIds = await _db.Applications
                .Where(a => a.CandidateId == candidateId)
                .Select(a => a.JobId)
                .ToListAsync(ct);
            var appliedJobIdSet = appliedJobIds.ToHashSet();

            Dictionary<int, MatchSignals> signalsByJob;
            try
            {
                using var aiTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                aiTimeout.CancelAfter(TimeSpan.FromSeconds(4));
                signalsByJob = await _scoring.ComputeMatchSignalsForCandidateAsync(
                    candidateId, candidate.ResumeText!, jobs, aiTimeout.Token);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                signalsByJob = jobs.ToDictionary(
                    job => job.JobId,
                    job => new MatchSignals(ComputeLexicalResumeRelevance(JobText(job), candidate.ResumeText!), null));
            }

            var recommendations = jobs.Select(job =>
            {
                var skillMatch = _scoring.ComputeSkillMatch(job.RequiredSkills, candidateSkillIds);
                var signals = signalsByJob.GetValueOrDefault(
                    job.JobId,
                    new MatchSignals(ComputeLexicalResumeRelevance(JobText(job), candidate.ResumeText!), null));
                return new JobRecommendationDto
                {
                    JobId = job.JobId,
                    JobTitle = job.Title,
                    CompanyName = job.Employer.CompanyName,
                    MatchScore = _scoring.ComputeTotalScore(skillMatch, signals.SemanticSimilarity, signals.MlFitScore),
                    SkillMatch = skillMatch,
                    SemanticSimilarity = signals.SemanticSimilarity,
                    MlFitScore = signals.MlFitScore,
                    HasApplied = appliedJobIdSet.Contains(job.JobId)
                };
            });

            return recommendations.OrderByDescending(r => r.MatchScore).Take(20).ToList();
        }

        private static string JobText(Job job)
        {
            var requiredSkills = string.Join(' ', job.RequiredSkills.Select(rs => rs.Skill.SkillName));
            return string.Join(' ', new[]
            {
                job.Title,
                FirstNonBlank(job.GeneratedJd, job.RequirementInput),
                requiredSkills
            }.Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        private static string FirstNonBlank(params string?[] values)
            => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        private static decimal ComputeLexicalResumeRelevance(string jobText, string resumeText)
        {
            var jobTokens = Tokenize(jobText).ToHashSet();
            var resumeTokens = Tokenize(resumeText).ToHashSet();
            if (jobTokens.Count == 0 || resumeTokens.Count == 0) return 0m;

            var overlap = jobTokens.Count(resumeTokens.Contains);
            var precision = (decimal)overlap / resumeTokens.Count;
            var recall = (decimal)overlap / jobTokens.Count;
            var f1 = precision + recall == 0m ? 0m : 2m * precision * recall / (precision + recall);

            var requiredSkills = jobTokens.Where(t => t.Length > 1).ToList();
            var skillCoverage = requiredSkills.Count == 0
                ? 0m
                : (decimal)requiredSkills.Count(resumeTokens.Contains) / requiredSkills.Count;

            return Math.Clamp((0.65m * skillCoverage) + (0.35m * f1), 0m, 1m);
        }

        private static IEnumerable<string> Tokenize(string text)
        {
            return Regex.Matches(text.ToLowerInvariant(), "[a-z][a-z0-9+#.]{1,}")
                .Select(m => m.Value.Trim('.', '#'))
                .Where(t => t.Length > 1 && !ResumeRelevanceStopWords.Contains(t));
        }

        private static readonly HashSet<string> ResumeRelevanceStopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "and", "the", "for", "with", "from", "that", "this", "will", "you", "your", "our", "are",
            "job", "role", "work", "team", "using", "use", "have", "has", "must", "nice", "good",
            "candidate", "developer", "engineer", "experience", "skills", "skill", "required"
        };
    }
}
