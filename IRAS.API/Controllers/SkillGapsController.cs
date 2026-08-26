// IRAS.API/Controllers/SkillGapsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.SkillGaps;
using IRAS.Application.Modules.SkillDevelopment;
using IRAS.Application.Modules.SkillDevelopment.DTOs;

namespace IRAS.API.Controllers
{
    // Module 7 — the read side of skill-gap analysis. Gaps themselves are detected and
    // written by ApplicationService.ApplyAsync (Module 6) at the moment a candidate
    // applies; this controller only surfaces what's already been recorded.
    //
    // target-skills/* closes the loop after that: lets the candidate mark a gap as
    // "working on it" and track it through to completion (see SkillDevelopmentService).
    [ApiController]
    [Route("api/candidates/{candidateId:int}/skill-gaps")]
    [Authorize]
    public class SkillGapsController : ControllerBase
    {
        private readonly ISkillGapService _service;
        private readonly ISkillDevelopmentService _development;
        public SkillGapsController(ISkillGapService service, ISkillDevelopmentService development)
        {
            _service = service;
            _development = development;
        }

        private IActionResult? CheckAccess(int candidateId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Candidate" && User.GetUserId() == candidateId) return null;
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> GetMine(int candidateId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.GetMyGapsAsync(candidateId, ct));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetMySummary(int candidateId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.GetMyGapSummaryAsync(candidateId, ct));
        }

        [HttpGet("target-skills")]
        public async Task<IActionResult> GetMyTargetSkills(int candidateId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _development.GetMyTargetSkillsAsync(candidateId, ct));
        }

        [HttpPost("target-skills")]
        public async Task<IActionResult> AddTargetSkill(int candidateId, AddTargetSkillRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _development.AddTargetSkillAsync(candidateId, request, ct));
        }

        [HttpPut("target-skills/{skillId:int}/complete")]
        public async Task<IActionResult> CompleteTargetSkill(int candidateId, int skillId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _development.MarkTargetSkillCompletedAsync(candidateId, skillId, ct);
            return NoContent();
        }

        [HttpDelete("target-skills/{skillId:int}")]
        public async Task<IActionResult> RemoveTargetSkill(int candidateId, int skillId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _development.RemoveTargetSkillAsync(candidateId, skillId, ct);
            return NoContent();
        }
    }
}
