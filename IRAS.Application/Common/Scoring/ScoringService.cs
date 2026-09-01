// IRAS.Application/Common/Scoring/ScoringService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Ai;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Common.Scoring
{
    public class ScoringService : IScoringService
    {
        private readonly IrasDbContext _db;
        private readonly IAiServiceClient _ai;
        private readonly ScoringOptions _options;
        private readonly ILogger<ScoringService> _logger;

        public ScoringService(
            IrasDbContext db, IAiServiceClient ai, IOptions<ScoringOptions> options, ILogger<ScoringService> logger)
        {
            _db = db;
            _ai = ai;
            _options = options.Value;
            _logger = logger;
        }

        public decimal ComputeSkillMatch(IEnumerable<JobRequiredSkill> requiredSkills, IReadOnlyCollection<int> candidateSkillIds)
        {
            var required = requiredSkills.ToList();
            if (required.Count == 0) return 1.0m;

            var totalWeight = required.Sum(rs => rs.Weight);
            if (totalWeight <= 0) return 0m;

            var matchedWeight = required.Where(rs => candidateSkillIds.Contains(rs.SkillId)).Sum(rs => rs.Weight);
            return Math.Round(matchedWeight / totalWeight, 4);
        }

        public decimal ComputeExperienceMatch(decimal candidateExpYears, int jobMinExpYears)
        {
            if (jobMinExpYears <= 0) return 1.0m;
            return Math.Round(Math.Min(1.0m, candidateExpYears / jobMinExpYears), 4);
        }

        public decimal ComputeEducationMatch(EducationLevel candidateLevel, EducationLevel requiredLevel)
        {
            if (candidateLevel >= requiredLevel) return 1.0m;
            // Partial credit for being close rather than an all-or-nothing cutoff:
            // e.g. a Diploma holder against a Bachelor requirement scores 2/3, not 0.
            return Math.Round((decimal)((int)candidateLevel + 1) / ((int)requiredLevel + 1), 4);
        }

        public decimal ComputeTotalScore(decimal skillMatch, decimal semanticSimilarity, decimal? mlFitScore = null, decimal? assessmentScore = null)
        {
            var score = _options.SkillMatchWeight * skillMatch + _options.SemanticSimilarityWeight * semanticSimilarity;
            if (mlFitScore.HasValue)
                score += _options.MlFitScoreWeight * mlFitScore.Value;
            if (assessmentScore.HasValue)
                score += _options.AssessmentScoreWeight * assessmentScore.Value;
            return Math.Round(score, 4);
        }

        public async Task<MatchSignals> ComputeMatchSignalAsync(int candidateId, string resumeText, Job job, CancellationToken ct)
        {
            var results = await ComputeMatchSignalsAsync(job, new[] { (candidateId, resumeText) }, ct);
            return results.TryGetValue(candidateId, out var signals) ? signals : new MatchSignals(0m, null);
        }

        public async Task<Dictionary<int, MatchSignals>> ComputeMatchSignalsAsync(
            Job job, IReadOnlyList<(int CandidateId, string ResumeText)> candidates, CancellationToken ct)
        {
            if (candidates.Count == 0) return new Dictionary<int, MatchSignals>();

            // Same taxonomy shape/source ResumeService.ParseAndPersistAsync already sends to
            // /parse-resume — the fit classifier needs it to compute skill-overlap features.
            var taxonomy = await _db.Skills
                .Include(s => s.Aliases)
                .Select(s => new TaxonomyItem(
                    s.SkillId, s.SkillName, s.Aliases.Select(a => a.AliasText).ToList()))
                .ToListAsync(ct);

            var jobText = job.GeneratedJd ?? job.RequirementInput ?? job.Title;
            var rankResult = await _ai.RankAsync(
                jobText,
                candidates.Select(c => new RankCandidateInput(c.CandidateId, c.ResumeText)).ToList(),
                taxonomy,
                ct);

            if (!rankResult.Success)
            {
                _logger.LogWarning("Match signals unavailable for job {JobId}: {Error}", job.JobId, rankResult.Error);
                return candidates.ToDictionary(c => c.CandidateId, _ => new MatchSignals(0m, null));
            }

            var signals = rankResult.Results.ToDictionary(
                r => r.CandidateId,
                r => new MatchSignals(
                    Math.Round(r.SemanticSimilarity, 4),
                    r.FitScore.HasValue ? Math.Round(r.FitScore.Value, 4) : null));
            // Guarantee every requested candidate has an entry even if the AI service
            // silently dropped one — callers index this dictionary without a TryGetValue.
            foreach (var c in candidates)
                signals.TryAdd(c.CandidateId, new MatchSignals(0m, null));
            return signals;
        }
    }
}
