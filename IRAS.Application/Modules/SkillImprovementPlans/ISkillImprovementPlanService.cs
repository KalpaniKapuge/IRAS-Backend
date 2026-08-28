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

        Task<SkillImprovementPlanDto> SetStepCompletionAsync(
            int candidateId, int planId, int stepId, bool isCompleted, CancellationToken ct);
    }
}
