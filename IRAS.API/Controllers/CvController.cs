// IRAS.API/Controllers/CvController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Cv;
using IRAS.Application.Modules.Cv.DTOs;

namespace IRAS.API.Controllers
{
    // Scoped to the authenticated candidate via the JWT, not a route id — same pattern as
    // ResumesController, since a candidate only ever manages their own CVs.
    [ApiController]
    [Route("api/cv")]
    [Authorize(Roles = "Candidate")]
    public class CvController : ControllerBase
    {
        private readonly ICvService _service;
        public CvController(ICvService service) => _service = service;

        [HttpGet("templates")]
        public IActionResult GetTemplates() => Ok(_service.GetAvailableTemplates());

        [HttpGet]
        public async Task<IActionResult> GetMine(CancellationToken ct)
            => Ok(await _service.GetMyCvsAsync(User.GetUserId(), ct));

        [HttpGet("{cvId:int}")]
        public async Task<IActionResult> GetDetail(int cvId, CancellationToken ct)
            => Ok(await _service.GetCvDetailAsync(User.GetUserId(), cvId, ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateCvRequest request, CancellationToken ct)
            => Ok(await _service.CreateCvAsync(User.GetUserId(), request, ct));

        [HttpPut("{cvId:int}")]
        public async Task<IActionResult> Update(int cvId, UpdateCvRequest request, CancellationToken ct)
        {
            await _service.UpdateCvAsync(User.GetUserId(), cvId, request, ct);
            return NoContent();
        }

        [HttpPost("{cvId:int}/photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(int cvId, CancellationToken ct)
        {
            var file = Request.Form.Files.GetFile("file") ?? Request.Form.Files.GetFile("photo");
            if (file == null) return BadRequest(new { message = "CV photo file is required." });
            return Ok(await _service.UploadCvPhotoAsync(User.GetUserId(), cvId, file, ct));
        }

        [HttpPut("{cvId:int}/items")]
        public async Task<IActionResult> UpdateItems(int cvId, UpdateCvSectionItemsRequest request, CancellationToken ct)
        {
            await _service.UpdateSectionItemsAsync(User.GetUserId(), cvId, request, ct);
            return NoContent();
        }

        [HttpDelete("{cvId:int}")]
        public async Task<IActionResult> Delete(int cvId, CancellationToken ct)
        {
            await _service.DeleteCvAsync(User.GetUserId(), cvId, ct);
            return NoContent();
        }

        [HttpGet("{cvId:int}/download")]
        public async Task<IActionResult> Download(int cvId, CancellationToken ct)
        {
            var pdf = await _service.RenderPdfAsync(User.GetUserId(), cvId, ct);
            return File(pdf, "application/pdf", $"cv-{cvId}.pdf");
        }
    }
}
