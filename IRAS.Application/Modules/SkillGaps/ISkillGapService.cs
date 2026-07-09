// IRAS.Application/Modules/SkillGaps/ISkillGapService.cs
using IRAS.Application.Modules.SkillGaps.DTOs;

namespace IRAS.Application.Modules.SkillGaps
{
    public interface ISkillGapService
    {
        Task<List<CandidateSkillGapDto>> GetMyGapsAsync(int candidateId, CancellationToken ct);

        Task<List<SkillGapSummaryDto>> GetMyGapSummaryAsync(int candidateId, CancellationToken ct);
    }
}
