// IRAS.Application/Modules/SkillImprovementPlans/ISkillPlanEvidenceService.cs
using IRAS.Application.Modules.SkillImprovementPlans.DTOs;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    // Owns the full lifecycle of SkillPlanEvidence — both the candidate-facing add/remove
    // operations and the admin-facing review queue/verification. Kept as one service rather
    // than two because "add" and "verify" operate on the exact same small entity and change
    // together; splitting them would scatter one concept across two files for no benefit
    // (contrast with JobService/JobModerationService, which stays split because Job's
    // employer-owned lifecycle is large and genuinely independent of moderation).
    public interface ISkillPlanEvidenceService
    {
        Task<SkillPlanEvidenceDto> AddEvidenceLinkAsync(
            int candidateId, int planId, AddEvidenceLinkRequest request, CancellationToken ct);

        Task<SkillPlanEvidenceDto> AddEvidenceFileAsync(
            int candidateId, int planId, AddEvidenceFileRequest request, CancellationToken ct);

        Task RemoveEvidenceAsync(int candidateId, int planId, int evidenceId, CancellationToken ct);

        Task<List<AdminEvidenceReviewDto>> GetPendingEvidenceAsync(CancellationToken ct);

        // Approving only ever promotes the plan to Verified once its roadmap is already
        // 100% complete; rejecting never downgrades the plan's own status.
        Task<SkillPlanEvidenceDto> VerifyEvidenceAsync(
            int adminId, int evidenceId, VerifyEvidenceRequest request, CancellationToken ct);
    }
}
