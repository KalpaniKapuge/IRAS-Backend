// IRAS.API/Controllers/EmployerProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Interviews;
using IRAS.Application.Modules.Jobs;
using IRAS.Application.Modules.Jobs.DTOs;

namespace IRAS.API.Controllers
{
    [ApiController]
    [Route("api/employers/{employerId:int}")]
    [Authorize]
    public class EmployerProfileController : ControllerBase
    {
        private readonly IJobService _service;
        private readonly IInterviewService _interviews;

        public EmployerProfileController(IJobService service, IInterviewService interviews)
        {
            _service = service;
            _interviews = interviews;
        }

        private IActionResult? CheckAccess(int employerId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Employer" && User.GetUserId() == employerId) return null;
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile(int employerId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.GetEmployerProfileAsync(employerId));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(int employerId, UpdateEmployerProfileRequest request)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _service.UpdateEmployerProfileAsync(employerId, request);
            return NoContent();
        }

        [HttpPost("logo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadLogo(int employerId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;

            var file = Request.Form.Files.GetFile("file")
                ?? Request.Form.Files.GetFile("logo")
                ?? Request.Form.Files.FirstOrDefault();

            if (file is null)
                return BadRequest(new { message = "Company logo file is required." });

            return Ok(await _service.UploadEmployerLogoAsync(employerId, file, ct));
        }

        // All interviews across every job this employer owns, soonest first — a
        // dashboard-style view distinct from the per-application list in
        // EmployerApplicationsController.
        [HttpGet("interviews")]
        public async Task<IActionResult> GetInterviews(int employerId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _interviews.GetForEmployerAsync(employerId, ct));
        }
    }
}