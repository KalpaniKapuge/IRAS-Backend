// IRAS.Application/Modules/Matching/JobMatchingService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Modules.Matching.DTOs;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

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
                .Include(j => j.RequiredSkills)
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

            var candidateSkillMap = await _db.CandidateSkills
                .Where(cs => eligible.Select(e => e.CandidateId).Contains(cs.CandidateId))
                .GroupBy(cs => cs.CandidateId)
                .ToDictionaryAsync(g => g.Key, g => (IReadOnlyCollection<int>)g.Select(cs => cs.SkillId).ToList(), ct);

            // One batched HTTP call to the AI service for every eligible candidate's resume
            // against this single job — not N sequential calls.
            var matchSignals = await _scoring.ComputeMatchSignalsAsync(job, eligible, ct);

            foreach (var (candidateId, _) in eligible)
            {
                var skillIds = candidateSkillMap.GetValueOrDefault(candidateId, Array.Empty<int>());
                var skillMatch = _scoring.ComputeSkillMatch(job.RequiredSkills, skillIds);
                var signals = matchSignals.GetValueOrDefault(candidateId, new MatchSignals(0m, null));
                var matchScore = _scoring.ComputeTotalScore(skillMatch, signals.SemanticSimilarity, signals.MlFitScore);
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
                .Include(j => j.RequiredSkills)
                .Include(j => j.Employer)
                .ToListAsync(ct);

            var recommendations = new List<JobRecommendationDto>();
            foreach (var job in jobs)
            {
                var skillMatch = _scoring.ComputeSkillMatch(job.RequiredSkills, candidateSkillIds);
                // One AI-service call per published job — acceptable at prototype scale
                // (the Python /api/v1/rank contract is job-centric: one job description
                // against many candidates). A high-volume production deployment would need
                // a candidate-centric batch endpoint to avoid N sequential HTTP calls here.
                var signals = await _scoring.ComputeMatchSignalAsync(candidateId, candidate.ResumeText!, job, ct);
                var matchScore = _scoring.ComputeTotalScore(skillMatch, signals.SemanticSimilarity, signals.MlFitScore);

                recommendations.Add(new JobRecommendationDto
                {
                    JobId = job.JobId,
                    JobTitle = job.Title,
                    CompanyName = job.Employer.CompanyName,
                    MatchScore = matchScore,
                    SkillMatch = skillMatch,
                    SemanticSimilarity = signals.SemanticSimilarity,
                    MlFitScore = signals.MlFitScore
                });
            }

            return recommendations.OrderByDescending(r => r.MatchScore).Take(20).ToList();
        }
    }
}
