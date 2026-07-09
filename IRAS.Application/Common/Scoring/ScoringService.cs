// IRAS.Application/Common/Scoring/ScoringService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Ai;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Application.Common.Scoring
{
    public class ScoringService : IScoringService
    {
        private readonly IAiServiceClient _ai;
        private readonly ScoringOptions _options;
        private readonly ILogger<ScoringService> _logger;

        public ScoringService(IAiServiceClient ai, IOptions<ScoringOptions> options, ILogger<ScoringService> logger)
        {
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

        public decimal ComputeTotalScore(decimal skillMatch, decimal semanticSimilarity)
        {
            return Math.Round(_options.SkillMatchWeight * skillMatch + _options.SemanticSimilarityWeight * semanticSimilarity, 4);
        }

        public async Task<decimal> ComputeSemanticSimilarityAsync(int candidateId, string resumeText, Job job, CancellationToken ct)
        {
            var results = await ComputeSemanticSimilaritiesAsync(job, new[] { (candidateId, resumeText) }, ct);
            return results.TryGetValue(candidateId, out var score) ? score : 0m;
        }

        public async Task<Dictionary<int, decimal>> ComputeSemanticSimilaritiesAsync(
            Job job, IReadOnlyList<(int CandidateId, string ResumeText)> candidates, CancellationToken ct)
        {
            if (candidates.Count == 0) return new Dictionary<int, decimal>();

            var jobText = job.GeneratedJd ?? job.RequirementInput ?? job.Title;
            var rankResult = await _ai.RankAsync(
                jobText,
                candidates.Select(c => new RankCandidateInput(c.CandidateId, c.ResumeText)).ToList(),
                ct);

            if (!rankResult.Success)
            {
                _logger.LogWarning("Semantic similarity unavailable for job {JobId}: {Error}", job.JobId, rankResult.Error);
                return candidates.ToDictionary(c => c.CandidateId, _ => 0m);
            }

            var scores = rankResult.Results.ToDictionary(r => r.CandidateId, r => Math.Round(r.SemanticSimilarity, 4));
            // Guarantee every requested candidate has an entry even if the AI service
            // silently dropped one — callers index this dictionary without a TryGetValue.
            foreach (var c in candidates)
                scores.TryAdd(c.CandidateId, 0m);
            return scores;
        }
    }
}
