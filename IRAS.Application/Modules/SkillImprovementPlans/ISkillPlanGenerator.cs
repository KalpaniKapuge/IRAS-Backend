// IRAS.Application/Modules/SkillImprovementPlans/ISkillPlanGenerator.cs
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    public record SkillPlanStepDraft(string Title, string Description, string Activity, string Output);

    public record SkillPlanGenerationResult(
        string Overview,
        string GapReason,
        SkillTargetLevel TargetLevel,
        SkillPlanPriority Priority,
        int EstimatedDays,
        string ProjectTitle,
        string ProjectTask,
        string ProjectExpectedOutput,
        List<SkillPlanStepDraft> Steps);

    // Same abstraction pattern as IJdGenerator/IFeedbackGenerator/ISkillGapExplainer —
    // an AI-backed implementation and a deterministic template fallback behind one
    // interface, swapped via DI registration in Program.cs.
    public interface ISkillPlanGenerator
    {
        string Name { get; }
        bool IsAi { get; }

        Task<SkillPlanGenerationResult> GenerateAsync(
            string skillName, string? jobTitle, string importance, CancellationToken ct);
    }
}
