// IRAS.API/Controllers/EmployerApplicationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Application.Modules.Assessments;
using IRAS.Application.Modules.Feedback;
using IRAS.Application.Modules.Feedback.DTOs;
using IRAS.Application.Modules.Interviews;
using IRAS.Application.Modules.Interviews.DTOs;

namespace IRAS.API.Controllers
{
    // Ranked applicant view, status changes, feedback review, and interview scheduling
    // for a job's owning employer. Candidate-facing application routes live in
    // ApplicationsController (api/applications).
    [ApiController]
    [Route("api/employers/{employerId:int}/jobs/{jobId:int}/applicants")]
    [Authorize]
    public class EmployerApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applications;
        private readonly IFeedbackService _feedback;
        private readonly IInterviewService _interviews;
        private readonly IAssessmentService _assessments;

        public EmployerApplicationsController(
            IApplicationService applications, IFeedbackService feedback, IInterviewService interviews, IAssessmentService assessments)
        {
            _applications = applications;
            _feedback = feedback;
            _interviews = interviews;
            _assessments = assessments;
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

        // Skill-assessment review — the quiz that was generated for this job's role/required
        // skills, alongside exactly what this candidate answered. 404 if they never completed
        // one (job doesn't require an assessment, or they haven't finished it yet).
        [HttpGet("{applicationId:int}/assessment")]
        public async Task<IActionResult> GetAssessmentReview(int employerId, int jobId, int applicationId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            var review = await _assessments.GetReviewForEmployerAsync(employerId, applicationId, ct);
            return review is null ? NotFound() : Ok(review);
        }

        // ---- Interview scheduling ----

        [HttpGet("{applicationId:int}/interviews")]
        public async Task<IActionResult> GetInterviews(int employerId, int jobId, int applicationId, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _interviews.GetForApplicationAsync(employerId, applicationId, ct));
        }

        [HttpPost("{applicationId:int}/interviews")]
        public async Task<IActionResult> ScheduleInterview(
            int employerId, int jobId, int applicationId, ScheduleInterviewRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _interviews.ScheduleAsync(employerId, applicationId, request, ct));
        }

        [HttpPut("{applicationId:int}/interviews/{interviewId:int}")]
        public async Task<IActionResult> RescheduleInterview(
            int employerId, int jobId, int applicationId, int interviewId, RescheduleInterviewRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _interviews.RescheduleAsync(employerId, interviewId, request, ct));
        }

        [HttpPost("{applicationId:int}/interviews/{interviewId:int}/cancel")]
        public async Task<IActionResult> CancelInterview(
            int employerId, int jobId, int applicationId, int interviewId, CancelInterviewRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _interviews.CancelAsync(employerId, interviewId, request, ct);
            return NoContent();
        }

        [HttpPut("{applicationId:int}/interviews/{interviewId:int}/outcome")]
        public async Task<IActionResult> UpdateInterviewOutcome(
            int employerId, int jobId, int applicationId, int interviewId, UpdateInterviewOutcomeRequest request, CancellationToken ct)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _interviews.UpdateOutcomeAsync(employerId, interviewId, request, ct);
            return NoContent();
        }
    }
}
