// IRAS.API/Controllers/AssessmentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Assessments;
using IRAS.Application.Modules.Assessments.DTOs;

namespace IRAS.API.Controllers
{
    // Candidate-only pre-application skill assessment for a job — gates ApplicationsController's
    // POST api/applications when Job.RequireAssessment is set (see ApplicationService.ApplyAsync).
    [ApiController]
    [Route("api/jobs/{jobId:int}/assessment")]
    [Authorize(Roles = "Candidate")]
    public class AssessmentsController : ControllerBase
    {
        private readonly IAssessmentService _service;
        public AssessmentsController(IAssessmentService service) => _service = service;

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(int jobId, CancellationToken ct)
            => Ok(await _service.GetStatusAsync(User.GetUserId(), jobId, ct));

        [HttpPost("start")]
        public async Task<IActionResult> Start(int jobId, CancellationToken ct)
            => Ok(await _service.StartAsync(User.GetUserId(), jobId, ct));

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(int jobId, SubmitAssessmentRequest request, CancellationToken ct)
            => Ok(await _service.SubmitAsync(User.GetUserId(), jobId, request, ct));
    }
}
