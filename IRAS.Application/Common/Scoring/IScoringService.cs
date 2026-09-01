// IRAS.Application/Common/Scoring/IScoringService.cs
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Application.Common.Scoring
{
    // Both signals from a single AI-service call: SBERT semantic similarity and, when the
    // trained fit-classifier is enabled, its Good-Fit probability. MlFitScore is null if the
    // AI service response didn't include one (e.g. an older service version, or a failed call).
    public record MatchSignals(decimal SemanticSimilarity, decimal? MlFitScore);

    // Single source of truth for candidate<->job scoring, shared by the reactive path
    // (Module 6 — scoring an application at the moment a candidate applies) and the
    // proactive path (Module 8 — scanning opted-in candidates when a job publishes).
    // Neither caller should recompute this logic independently.
    public interface IScoringService
    {
        decimal ComputeSkillMatch(IEnumerable<JobRequiredSkill> requiredSkills, IReadOnlyCollection<int> candidateSkillIds);

        decimal ComputeExperienceMatch(decimal candidateExpYears, int jobMinExpYears);

        decimal ComputeEducationMatch(EducationLevel candidateLevel, EducationLevel requiredLevel);

        // mlFitScore is optional: pass null to fall back to the original two-term formula
        // (e.g. MlFitScoreWeight is 0, or the AI service didn't return a fit score).
        decimal ComputeTotalScore(decimal skillMatch, decimal semanticSimilarity, decimal? mlFitScore = null, decimal? assessmentScore = null);

        Task<MatchSignals> ComputeMatchSignalAsync(int candidateId, string resumeText, Job job, CancellationToken ct);

        // Batch form: one HTTP round-trip to the AI service for many candidates against a
        // single job, instead of N sequential calls. This is what Module 8 needs when
        // scoring every opted-in candidate against a newly-published job.
        Task<Dictionary<int, MatchSignals>> ComputeMatchSignalsAsync(
            Job job, IReadOnlyList<(int CandidateId, string ResumeText)> candidates, CancellationToken ct);
    }
}
