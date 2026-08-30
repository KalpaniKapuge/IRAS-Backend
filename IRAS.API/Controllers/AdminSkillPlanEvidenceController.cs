// IRAS.API/Controllers/AdminSkillPlanEvidenceController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.SkillImprovementPlans;
using IRAS.Application.Modules.SkillImprovementPlans.DTOs;

namespace IRAS.API.Controllers
{
    // Admin review queue for candidate-submitted skill-plan evidence — approving evidence
    // for an already-complete plan promotes it to Verified (see
    // SkillPlanEvidenceService.VerifyEvidenceAsync); this is the "AI Model Monitoring"-style
    // Admin workflow item for the skill-development feature specifically.
    [ApiController]
    [Route("api/admin/skill-plan-evidence")]
    [Authorize(Roles = "Admin")]
    public class AdminSkillPlanEvidenceController : ControllerBase
    {
        private readonly ISkillPlanEvidenceService _service;
        public AdminSkillPlanEvidenceController(ISkillPlanEvidenceService service) => _service = service;

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending(CancellationToken ct)
            => Ok(await _service.GetPendingEvidenceAsync(ct));

        [HttpPut("{evidenceId:int}/verify")]
        public async Task<IActionResult> Verify(int evidenceId, VerifyEvidenceRequest request, CancellationToken ct)
            => Ok(await _service.VerifyEvidenceAsync(User.GetUserId(), evidenceId, request, ct));
    }
}
