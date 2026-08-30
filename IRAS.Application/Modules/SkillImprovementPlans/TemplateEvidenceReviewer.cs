// IRAS.Application/Modules/SkillImprovementPlans/TemplateEvidenceReviewer.cs
namespace IRAS.Application.Modules.SkillImprovementPlans
{
    // Baseline reviewer: deterministic, no external calls — mirrors TemplateSkillGapExplainer
    // / TemplateSkillPlanGenerator. Unlike those, this one can't meaningfully judge anything
    // without an LLM, so it deliberately always returns a neutral mid-band score rather than
    // guessing. That keeps every submission routed to the admin queue when this is active —
    // exactly today's pre-triage behavior — instead of risking an unsafe blind
    // auto-approval/rejection if Gemini isn't configured.
    public class TemplateEvidenceReviewer : IEvidenceReviewer
    {
        public string Name => "Template";
        public bool IsAi => false;

        public Task<EvidenceReviewResult> ReviewAsync(
            string skillName, string projectTitle, string projectTask, string projectExpectedOutput,
            string evidenceType, string evidenceUrl, string? candidateNotes, CancellationToken ct)
        {
            return Task.FromResult(new EvidenceReviewResult(
                50, "Automatic review is unavailable — routed to manual review."));
        }
    }
}
