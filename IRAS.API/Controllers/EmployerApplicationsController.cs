// IRAS.API/Controllers/EmployerApplicationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Applications;

namespace IRAS.API.Controllers
{
    // Ranked applicant view for a job's owning employer. Candidate-facing application
    // routes (apply, list mine) live in ApplicationsController (api/applications).
    [ApiController]
    [Route("api/employers/{employerId:int}/jobs/{jobId:int}/applicants")]
    [Authorize]
    public class EmployerApplicationsController : ControllerBase
    {
        private readonly IApplicationService _service;
        public EmployerApplicationsController(IApplicationService service) => _service = service;

        private IActionResult? CheckAccess(int employerId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Employer" && User.GetUserId() == employerId) return null;
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> GetRanked(int employerId, int jobId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.GetRankedApplicantsAsync(employerId, jobId, ct));
        }
    }
}
