// IRAS.Application/Common/Scoring/IScoringService.cs
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Application.Common.Scoring
{
    // Single source of truth for candidate<->job scoring, shared by the reactive path
    // (Module 6 — scoring an application at the moment a candidate applies) and the
    // proactive path (Module 8 — scanning opted-in candidates when a job publishes).
    // Neither caller should recompute this logic independently.
    public interface IScoringService
    {
        decimal ComputeSkillMatch(IEnumerable<JobRequiredSkill> requiredSkills, IReadOnlyCollection<int> candidateSkillIds);

        decimal ComputeExperienceMatch(decimal candidateExpYears, int jobMinExpYears);

        decimal ComputeEducationMatch(EducationLevel candidateLevel, EducationLevel requiredLevel);

        decimal ComputeTotalScore(decimal skillMatch, decimal semanticSimilarity);

        Task<decimal> ComputeSemanticSimilarityAsync(int candidateId, string resumeText, Job job, CancellationToken ct);

        // Batch form: one HTTP round-trip to the AI service for many candidates against a
        // single job, instead of N sequential calls. This is what Module 8 needs when
        // scoring every opted-in candidate against a newly-published job.
        Task<Dictionary<int, decimal>> ComputeSemanticSimilaritiesAsync(
            Job job, IReadOnlyList<(int CandidateId, string ResumeText)> candidates, CancellationToken ct);
    }
}
