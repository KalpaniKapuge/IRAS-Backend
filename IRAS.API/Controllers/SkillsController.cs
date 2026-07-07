// IRAS.API/Controllers/SkillsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.Application.Modules.SkillTaxonomy;
using IRAS.Application.Modules.SkillTaxonomy.DTOs;

namespace IRAS.API.Controllers
{
    [ApiController]
    [Route("api/skills")]
    [Authorize]                       // everyone logged-in can read (candidates need it for autocomplete)
    public class SkillsController : ControllerBase
    {
        private readonly ISkillTaxonomyService _service;
        public SkillsController(ISkillTaxonomyService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? query, [FromQuery] string? category,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _service.SearchAsync(query, category, page, pageSize));

        [HttpGet("resolve")]
        public async Task<IActionResult> Resolve([FromQuery] string text)
            => Ok(await _service.ResolveAsync(text));

        [HttpGet("export")]
        [Authorize(Roles = "Admin")]   // AI service / admin use, not for regular users
        public async Task<IActionResult> ExportAll()
            => Ok(await _service.ExportAllAsync());

        [HttpGet("{skillId:int}")]
        public async Task<IActionResult> GetById(int skillId)
            => Ok(await _service.GetByIdAsync(skillId));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateSkillRequest request)
            => Ok(await _service.CreateAsync(request));

        [HttpPut("{skillId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int skillId, UpdateSkillRequest request)
        {
            await _service.UpdateAsync(skillId, request);
            return NoContent();
        }

        [HttpDelete("{skillId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int skillId)
        {
            await _service.DeleteAsync(skillId);
            return NoContent();
        }

        [HttpPost("{skillId:int}/aliases")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddAlias(int skillId, AddAliasRequest request)
            => Ok(await _service.AddAliasAsync(skillId, request));

        [HttpDelete("{skillId:int}/aliases/{aliasId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAlias(int skillId, int aliasId)
        {
            await _service.DeleteAliasAsync(skillId, aliasId);
            return NoContent();
        }
    }
}