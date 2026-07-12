// IRAS.API/Controllers/AdminJobsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Admin;

namespace IRAS.API.Controllers
{
    // "Job Post Monitoring" in the Admin workflow — spans every employer, unlike
    // EmployerJobsController which is scoped to one employer's own jobs.
    [ApiController]
    [Route("api/admin/jobs")]
    [Authorize(Roles = "Admin")]
    public class AdminJobsController : ControllerBase
    {
        private readonly IJobModerationService _service;
        public AdminJobsController(IJobModerationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
            => Ok(await _service.GetAllAsync(status, ct));

        [HttpPost("{jobId:int}/close")]
        public async Task<IActionResult> ForceClose(int jobId, CancellationToken ct)
        {
            await _service.ForceCloseAsync(User.GetUserId(), jobId, ct);
            return NoContent();
        }
    }
}
