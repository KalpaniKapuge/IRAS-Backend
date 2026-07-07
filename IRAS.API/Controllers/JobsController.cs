// IRAS.API/Controllers/JobsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Jobs;

namespace IRAS.API.Controllers
{
    // Public browsing (candidates and anyone authenticated): published jobs only,
    // except the owning employer/Admin who can also see their own draft via GetJob.
    [ApiController]
    [Route("api/jobs")]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _service;
        public JobsController(IJobService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Browse([FromQuery] string? query)
            => Ok(await _service.GetPublishedJobsAsync(query));

        [HttpGet("{jobId:int}")]
        public async Task<IActionResult> GetJob(int jobId)
            => Ok(await _service.GetJobAsync(jobId, User.GetUserId(), User.GetRole()));
    }
}