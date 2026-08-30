// IRAS.Application/Modules/SkillImprovementPlans/IEvidenceReviewer.cs
namespace IRAS.Application.Modules.SkillImprovementPlans
{
    public record EvidenceReviewResult(int ConfidenceScore, string Rationale);

    // Automatic first-pass judgement of submitted evidence, used to triage the admin
    // verification queue at scale (see SkillPlanEvidenceService.AddEvidenceLinkAsync):
    // high-confidence submissions auto-approve, low-confidence ones auto-reject with the
    // rationale as candidate-facing feedback, and only the genuinely ambiguous middle band
    // still reaches a human. Same Gemini/Template dual-implementation pattern as
    // ISkillPlanGenerator/ISkillGapExplainer elsewhere in this codebase.
    public interface IEvidenceReviewer
    {
        string Name { get; }
        bool IsAi { get; }

        Task<EvidenceReviewResult> ReviewAsync(
            string skillName, string projectTitle, string projectTask, string projectExpectedOutput,
            string evidenceType, string evidenceUrl, string? candidateNotes, CancellationToken ct);
    }
}
