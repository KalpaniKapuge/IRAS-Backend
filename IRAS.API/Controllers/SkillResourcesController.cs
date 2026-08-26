// IRAS.API/Controllers/SkillResourcesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.SkillDevelopment;
using IRAS.Application.Modules.SkillDevelopment.DTOs;

namespace IRAS.API.Controllers
{
    // Admin-curated learning resources per skill, surfaced to candidates on the Skill
    // Gaps page so a detected gap comes with a real next step, not just a name.
    [ApiController]
    [Route("api/skill-resources")]
    [Authorize]   // everyone logged-in can read; only admins curate
    public class SkillResourcesController : ControllerBase
    {
        private readonly ISkillDevelopmentService _service;
        public SkillResourcesController(ISkillDevelopmentService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllResourcesAsync(ct));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(UpsertSkillResourceRequest request, CancellationToken ct)
            => Ok(await _service.CreateResourceAsync(User.GetUserId(), request, ct));

        [HttpPut("{resourceId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int resourceId, UpsertSkillResourceRequest request, CancellationToken ct)
        {
            await _service.UpdateResourceAsync(User.GetUserId(), resourceId, request, ct);
            return NoContent();
        }

        [HttpDelete("{resourceId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int resourceId, CancellationToken ct)
        {
            await _service.DeleteResourceAsync(User.GetUserId(), resourceId, ct);
            return NoContent();
        }
    }
}
