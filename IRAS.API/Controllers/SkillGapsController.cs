// IRAS.API/Controllers/SkillGapsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.SkillGaps;

namespace IRAS.API.Controllers
{
    // Module 7 — the read side of skill-gap analysis. Gaps themselves are detected and
    // written by ApplicationService.ApplyAsync (Module 6) at the moment a candidate
    // applies; this controller only surfaces what's already been recorded.
    [ApiController]
    [Route("api/candidates/{candidateId:int}/skill-gaps")]
    [Authorize]
    public class SkillGapsController : ControllerBase
    {
        private readonly ISkillGapService _service;
        public SkillGapsController(ISkillGapService service) => _service = service;

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
    }
}
