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

        // status omitted/"Pending" = the actionable queue (default, unchanged behavior);
        // any other EvidenceVerificationStatus name = read-only decision history for that
        // status. "pending" kept as an explicit alias of the same root route so any existing
        // caller/bookmark of the old URL keeps working.
        [HttpGet]
        [HttpGet("pending")]
        public async Task<IActionResult> GetForReview([FromQuery] string? status, CancellationToken ct)
            => Ok(await _service.GetEvidenceForReviewAsync(status, ct));

        [HttpPut("{evidenceId:int}/verify")]
        public async Task<IActionResult> Verify(int evidenceId, VerifyEvidenceRequest request, CancellationToken ct)
            => Ok(await _service.VerifyEvidenceAsync(User.GetUserId(), evidenceId, request, ct));
    }
}
