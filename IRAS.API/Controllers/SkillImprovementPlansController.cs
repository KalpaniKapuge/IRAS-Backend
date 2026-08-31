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
        private readonly ISkillPlanEvidenceService _evidence;
        public SkillImprovementPlansController(ISkillImprovementPlanService service, ISkillPlanEvidenceService evidence)
        {
            _service = service;
            _evidence = evidence;
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

        // Two actions sharing one route, disambiguated at runtime by [Consumes] — same
        // pattern as CandidateProfileController's certification upload. The Swagger
        // conflict this causes is already resolved globally in Program.cs
        // (options.ResolveConflictingActions).
        [HttpPost("{planId:int}/evidence")]
        [Consumes("application/json")]
        public async Task<IActionResult> AddEvidenceLink(
            int candidateId, int planId, AddEvidenceLinkRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _evidence.AddEvidenceLinkAsync(candidateId, planId, request, ct));
        }

        [HttpPost("{planId:int}/evidence")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddEvidenceFile(
            int candidateId, int planId, [FromForm] AddEvidenceFileRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;

            request.File ??= Request.Form.Files.GetFile("file") ?? Request.Form.Files.FirstOrDefault();

            return Ok(await _evidence.AddEvidenceFileAsync(candidateId, planId, request, ct));
        }

        [HttpDelete("{planId:int}/evidence/{evidenceId:int}")]
        public async Task<IActionResult> RemoveEvidence(int candidateId, int planId, int evidenceId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _evidence.RemoveEvidenceAsync(candidateId, planId, evidenceId, ct);
            return NoContent();
        }

        // The candidate's explicit "Submit for Review" action — evidence sits as Draft
        // (visible only to the candidate) until this is called, at which point it enters
        // the Pending/AI-triage pipeline and becomes visible to admin.
        [HttpPut("{planId:int}/evidence/{evidenceId:int}/submit")]
        public async Task<IActionResult> SubmitEvidence(int candidateId, int planId, int evidenceId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _evidence.SubmitEvidenceForReviewAsync(candidateId, planId, evidenceId, ct));
        }
    }
}
