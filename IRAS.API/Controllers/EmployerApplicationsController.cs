// IRAS.API/Controllers/EmployerApplicationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Application.Modules.Feedback;
using IRAS.Application.Modules.Feedback.DTOs;

namespace IRAS.API.Controllers
{
    // Ranked applicant view, status changes, and feedback review for a job's owning
    // employer. Candidate-facing application routes live in ApplicationsController
    // (api/applications).
    [ApiController]
    [Route("api/employers/{employerId:int}/jobs/{jobId:int}/applicants")]
    [Authorize]
    public class EmployerApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applications;
        private readonly IFeedbackService _feedback;

        public EmployerApplicationsController(IApplicationService applications, IFeedbackService feedback)
        {
            _applications = applications;
            _feedback = feedback;
        }

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
            return Ok(await _applications.GetRankedApplicantsAsync(employerId, jobId, ct));
        }

        [HttpPut("{applicationId:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int employerId, int jobId, int applicationId, UpdateApplicationStatusRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _applications.UpdateStatusAsync(employerId, applicationId, request, ct);
            return NoContent();
        }

        // Module 9 — only meaningful once the application has been rejected, which is
        // what generates the draft this reads.
        [HttpGet("{applicationId:int}/feedback")]
        public async Task<IActionResult> GetFeedback(int employerId, int jobId, int applicationId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _feedback.GetForEmployerAsync(employerId, applicationId, ct));
        }

        [HttpPut("{applicationId:int}/feedback")]
        public async Task<IActionResult> ReviewFeedback(
            int employerId, int jobId, int applicationId, ReviewFeedbackRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _feedback.ReviewAsync(employerId, applicationId, request, ct));
        }
    }
}
