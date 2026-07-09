// IRAS.API/Controllers/ApplicationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Applications.DTOs;

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
        public ApplicationsController(IApplicationService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Apply(ApplyForJobRequest request, CancellationToken ct)
            => Ok(await _service.ApplyAsync(User.GetUserId(), request, ct));

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine(CancellationToken ct)
            => Ok(await _service.GetMyApplicationsAsync(User.GetUserId(), ct));
    }
}
