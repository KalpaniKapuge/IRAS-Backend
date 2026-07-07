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

        Task<SkillDto> CreateAsync(CreateSkillRequest request);
        Task UpdateAsync(int skillId, UpdateSkillRequest request);
        Task DeleteAsync(int skillId);

        Task<SkillAliasDto> AddAliasAsync(int skillId, AddAliasRequest request);
        Task DeleteAliasAsync(int skillId, int aliasId);
    }
}