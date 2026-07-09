// IRAS.API/Controllers/ResumesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Resumes;
using IRAS.Application.Modules.Resumes.DTOs;

namespace IRAS.API.Controllers
{
    // Exceptions map to HTTP status codes via ApiExceptionFilter (registered globally in
    // Program.cs) — same convention as JobsController/EmployerJobsController, no try/catch here.
    [ApiController]
    [Route("api/resumes")]
    [Authorize(Roles = "Candidate")]
    public class ResumesController : ControllerBase
    {
        private readonly IResumeService _service;
        public ResumesController(IResumeService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetMine(CancellationToken ct)
            => Ok(await _service.GetMyResumesAsync(User.GetUserId(), ct));

        [HttpPost]
        [RequestSizeLimit(12 * 1024 * 1024)]   // slightly above the logical 10 MB limit
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
            => Ok(await _service.UploadAndParseAsync(User.GetUserId(), file, ct));

        [HttpPost("{resumeId:int}/retry-parse")]
        public async Task<IActionResult> RetryParse(int resumeId, CancellationToken ct)
            => Ok(await _service.RetryParseAsync(User.GetUserId(), resumeId, ct));

        [HttpPost("{resumeId:int}/confirm-skills")]
        public async Task<IActionResult> ConfirmSkills(int resumeId, ConfirmSkillsRequest request, CancellationToken ct)
        {
            await _service.ConfirmSkillsAsync(User.GetUserId(), resumeId, request, ct);
            return NoContent();
        }

        [HttpPost("{resumeId:int}/set-primary")]
        public async Task<IActionResult> SetPrimary(int resumeId, CancellationToken ct)
        {
            await _service.SetPrimaryAsync(User.GetUserId(), resumeId, ct);
            return NoContent();
        }

        [HttpDelete("{resumeId:int}")]
        public async Task<IActionResult> Delete(int resumeId, CancellationToken ct)
        {
            await _service.DeleteAsync(User.GetUserId(), resumeId, ct);
            return NoContent();
        }
    }
}
