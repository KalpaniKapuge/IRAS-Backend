// IRAS.Application/Modules/SkillDevelopment/ISkillDevelopmentService.cs
using IRAS.Application.Modules.SkillDevelopment.DTOs;

namespace IRAS.Application.Modules.SkillDevelopment
{
    public interface ISkillDevelopmentService
    {
        // Resources — admin-curated, readable by any authenticated user.
        Task<List<SkillResourceDto>> GetAllResourcesAsync(CancellationToken ct);
        Task<SkillResourceDto> CreateResourceAsync(int adminId, UpsertSkillResourceRequest request, CancellationToken ct);
        Task UpdateResourceAsync(int adminId, int resourceId, UpsertSkillResourceRequest request, CancellationToken ct);
        Task DeleteResourceAsync(int adminId, int resourceId, CancellationToken ct);

        // Target skills — the candidate's own "working on this" list, separate from
        // CandidateSkill (skills they already have and have confirmed).
        Task<List<TargetSkillDto>> GetMyTargetSkillsAsync(int candidateId, CancellationToken ct);
        Task<TargetSkillDto> AddTargetSkillAsync(int candidateId, AddTargetSkillRequest request, CancellationToken ct);
        Task MarkTargetSkillCompletedAsync(int candidateId, int skillId, CancellationToken ct);
        Task RemoveTargetSkillAsync(int candidateId, int skillId, CancellationToken ct);
    }
}
