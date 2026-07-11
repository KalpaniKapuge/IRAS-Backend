// IRAS.API/Controllers/ApplicationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Application.Modules.Feedback;

namespace IRAS.API.Controllers
{
    // Exceptions map to HTTP status codes via ApiExceptionFilter (registered globally in
    // Program.cs) — same convention as ResumesController/JobsController, no try/catch here.
    [ApiController]
    [Route("api/applications")]
    [Authorize(Roles = "Candidate")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _service;
        private readonly IFeedbackService _feedback;

        public ApplicationsController(IApplicationService service, IFeedbackService feedback)
        {
            _service = service;
            _feedback = feedback;
        }

        [HttpPost]
        public async Task<IActionResult> Apply(ApplyForJobRequest request, CancellationToken ct)
            => Ok(await _service.ApplyAsync(User.GetUserId(), request, ct));

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine(CancellationToken ct)
            => Ok(await _service.GetMyApplicationsAsync(User.GetUserId(), ct));

        // Module 9 — only returns feedback the employer has actually reviewed and sent;
        // 204 (not the draft) while it's still pending review.
        [HttpGet("{applicationId:int}/feedback")]
        public async Task<IActionResult> GetFeedback(int applicationId, CancellationToken ct)
        {
            var feedback = await _feedback.GetMyFeedbackAsync(User.GetUserId(), applicationId, ct);
            return feedback is null ? NoContent() : Ok(feedback);
        }
    }
}
