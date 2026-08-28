// IRAS.API/Controllers/SkillImprovementPlansController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.SkillImprovementPlans;
using IRAS.Application.Modules.SkillImprovementPlans.DTOs;

namespace IRAS.API.Controllers
{
    // A plan's own lifecycle (list, detail, step completion) once it's been generated via
    // SkillGapsController.GeneratePlan — separate controller because a plan outlives and
    // is queried independently of the gap that originally produced it.
    [ApiController]
    [Route("api/candidates/{candidateId:int}/skill-improvement-plans")]
    [Authorize]
    public class SkillImprovementPlansController : ControllerBase
    {
        private readonly ISkillImprovementPlanService _service;
        public SkillImprovementPlansController(ISkillImprovementPlanService service) => _service = service;

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
            return Ok(await _service.GetMyPlansAsync(candidateId, ct));
        }

        [HttpGet("{planId:int}")]
        public async Task<IActionResult> GetById(int candidateId, int planId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.GetPlanAsync(candidateId, planId, ct));
        }

        [HttpPut("{planId:int}/steps/{stepId:int}/complete")]
        public async Task<IActionResult> SetStepCompletion(
            int candidateId, int planId, int stepId, [FromBody] SetStepCompletionRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.SetStepCompletionAsync(candidateId, planId, stepId, request.IsCompleted, ct));
        }
    }
}
