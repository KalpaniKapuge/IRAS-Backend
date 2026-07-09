// IRAS.API/Controllers/EmployerJobsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Jobs;
using IRAS.Application.Modules.Jobs.DTOs;

namespace IRAS.API.Controllers
{
    // Job management for the owning employer: create, edit JD, publish/close lifecycle.
    // Public browsing of published jobs lives in JobsController (api/jobs).
    [ApiController]
    [Route("api/employers/{employerId:int}/jobs")]
    [Authorize]
    public class EmployerJobsController : ControllerBase
    {
        private readonly IJobService _service;
        private readonly IApplicationService _applications;

        public EmployerJobsController(IJobService service, IApplicationService applications)
        {
            _service = service;
            _applications = applications;
        }

        private IActionResult? CheckAccess(int employerId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Employer" && User.GetUserId() == employerId) return null;
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int employerId, CreateJobRequest request)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.CreateJobAsync(employerId, request));
        }

        [HttpGet]
        public async Task<IActionResult> GetMyJobs(int employerId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.GetMyJobsAsync(employerId));
        }

        [HttpGet("{jobId:int}")]
        public async Task<IActionResult> GetJob(int employerId, int jobId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.GetJobAsync(jobId, employerId, "Employer"));
        }

        [HttpPost("{jobId:int}/generate-jd")]
        public async Task<IActionResult> GenerateJd(int employerId, int jobId, GenerateJdRequest request)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.GenerateJdAsync(employerId, jobId, request));
        }

        [HttpPut("{jobId:int}/jd")]
        public async Task<IActionResult> UpdateJd(int employerId, int jobId, UpdateJdRequest request)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _service.UpdateJdAsync(employerId, jobId, request);
            return NoContent();
        }

        [HttpPost("{jobId:int}/publish")]
        public async Task<IActionResult> Publish(int employerId, int jobId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _service.PublishJobAsync(employerId, jobId);
            return NoContent();
        }

        [HttpPost("{jobId:int}/close")]
        public async Task<IActionResult> Close(int employerId, int jobId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _service.CloseJobAsync(employerId, jobId);
            return NoContent();
        }

        [HttpDelete("{jobId:int}")]
        public async Task<IActionResult> DeleteDraft(int employerId, int jobId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _service.DeleteDraftJobAsync(employerId, jobId);
            return NoContent();
        }

        [HttpGet("{jobId:int}/applications")]
        public async Task<IActionResult> GetRankedApplicants(int employerId, int jobId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _applications.GetRankedApplicantsAsync(employerId, jobId, ct));
        }
    }
}