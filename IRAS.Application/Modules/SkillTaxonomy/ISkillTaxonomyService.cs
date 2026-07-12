// IRAS.Application/Modules/SkillTaxonomy/ISkillTaxonomyService.cs
using IRAS.Application.Modules.SkillTaxonomy.DTOs;

namespace IRAS.Application.Modules.SkillTaxonomy
{
    public interface ISkillTaxonomyService
    {
        Task<PagedResult<SkillDto>> SearchAsync(string? query, string? category, int page, int pageSize);
        Task<SkillDto> GetByIdAsync(int skillId);
        Task<SkillResolveResult> ResolveAsync(string text);
        Task<List<SkillDto>> ExportAllAsync();                    // for the Python AI service

        // adminId is recorded to the audit log — Skill Management is an Admin-workflow
        // item, so every taxonomy edit needs an accountable "who".
        Task<SkillDto> CreateAsync(int adminId, CreateSkillRequest request);
        Task UpdateAsync(int adminId, int skillId, UpdateSkillRequest request);
        Task DeleteAsync(int adminId, int skillId);

        Task<SkillAliasDto> AddAliasAsync(int adminId, int skillId, AddAliasRequest request);
        Task DeleteAliasAsync(int adminId, int skillId, int aliasId);
    }
}