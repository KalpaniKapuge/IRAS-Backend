// IRAS.Application/Modules/SkillImprovementPlans/ISkillImprovementPlanService.cs
using IRAS.Application.Modules.SkillImprovementPlans.DTOs;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    public interface ISkillImprovementPlanService
    {
        Task<List<SkillImprovementPlanDto>> GetMyPlansAsync(int candidateId, CancellationToken ct);

        Task<SkillImprovementPlanDto> GetPlanAsync(int candidateId, int planId, CancellationToken ct);

        // Idempotent: if a plan already exists for this candidate+skill, returns it as-is
        // rather than generating a duplicate (see SkillImprovementPlanService for why).
        Task<SkillImprovementPlanDto> GeneratePlanAsync(
            int candidateId, int skillId, GeneratePlanRequest request, CancellationToken ct);

        // Toggling a step also recomputes the plan's overall Status from the resulting
        // checklist completion fraction — progress is never candidate-selectable, only ever
        // derived from actual task completion (see SkillImprovementPlanService).
        Task<SkillImprovementPlanDto> SetStepCompletionAsync(
            int candidateId, int planId, int stepId, bool isCompleted, CancellationToken ct);
    }
}
