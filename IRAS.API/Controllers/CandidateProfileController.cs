// IRAS.API/Controllers/CandidateProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Candidates;
using IRAS.Application.Modules.Candidates.DTOs;

namespace IRAS.API.Controllers
{
    [ApiController]
    [Route("api/candidates/{candidateId:int}")]
    [Authorize]
    public class CandidateProfileController : ControllerBase
    {
        private readonly ICandidateProfileService _service;
        public CandidateProfileController(ICandidateProfileService service) => _service = service;

        private IActionResult? CheckAccess(int candidateId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Candidate" && User.GetUserId() == candidateId) return null;
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile(int candidateId)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.GetProfileAsync(candidateId));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(int candidateId, UpdateCandidateProfileRequest request)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.UpdateProfileAsync(candidateId, request);
            return NoContent();
        }

        [HttpPost("profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfilePicture(int candidateId, CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;

            var file = Request.Form.Files.GetFile("file")
                ?? Request.Form.Files.GetFile("profilePicture")
                ?? Request.Form.Files.FirstOrDefault();

            if (file is null)
                return BadRequest(new { message = "Profile picture file is required." });

            return Ok(await _service.UploadProfilePictureAsync(candidateId, file, ct));
        }

        [HttpPost("education")]
        public async Task<IActionResult> AddEducation(int candidateId, EducationDto dto)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.AddEducationAsync(candidateId, dto));
        }

        [HttpPut("education/{educationId:int}")]
        public async Task<IActionResult> UpdateEducation(int candidateId, int educationId, EducationDto dto)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.UpdateEducationAsync(candidateId, educationId, dto);
            return NoContent();
        }

        [HttpDelete("education/{educationId:int}")]
        public async Task<IActionResult> DeleteEducation(int candidateId, int educationId)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.DeleteEducationAsync(candidateId, educationId);
            return NoContent();
        }

        [HttpPost("experience")]
        public async Task<IActionResult> AddExperience(int candidateId, WorkExperienceDto dto)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.AddWorkExperienceAsync(candidateId, dto));
        }

        [HttpPut("experience/{experienceId:int}")]
        public async Task<IActionResult> UpdateExperience(int candidateId, int experienceId, WorkExperienceDto dto)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.UpdateWorkExperienceAsync(candidateId, experienceId, dto);
            return NoContent();
        }

        [HttpDelete("experience/{experienceId:int}")]
        public async Task<IActionResult> DeleteExperience(int candidateId, int experienceId)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.DeleteWorkExperienceAsync(candidateId, experienceId);
            return NoContent();
        }

        [HttpPost("certifications")]
        [Consumes("application/json")]
        public async Task<IActionResult> AddCertification(int candidateId, CertificationDto dto)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            return Ok(await _service.AddCertificationAsync(candidateId, dto));
        }

        [HttpPost("certifications")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddCertificationWithFile(
            int candidateId,
            [FromForm] CertificationUploadRequest request,
            CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;

            request.File ??= Request.Form.Files.GetFile("file");
            request.CertificateFile ??= Request.Form.Files.GetFile("certificateFile")
                ?? Request.Form.Files.FirstOrDefault();

            return Ok(await _service.AddCertificationAsync(candidateId, request, ct));
        }

        [HttpPost("certifications/{certificationId:int}/file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCertificationFile(
            int candidateId,
            int certificationId,
            CancellationToken ct)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;

            var file = Request.Form.Files.GetFile("file")
                ?? Request.Form.Files.GetFile("certificateFile")
                ?? Request.Form.Files.FirstOrDefault();

            if (file is null)
                return BadRequest(new { message = "Certificate file is required." });

            return Ok(await _service.UploadCertificationFileAsync(candidateId, certificationId, file, ct));
        }

        [HttpDelete("certifications/{certificationId:int}")]
        public async Task<IActionResult> DeleteCertification(int candidateId, int certificationId)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.DeleteCertificationAsync(candidateId, certificationId);
            return NoContent();
        }

        [HttpPut("skills")]
        public async Task<IActionResult> UpsertSkill(int candidateId, UpsertCandidateSkillRequest request)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.UpsertSkillAsync(candidateId, request);
            return NoContent();
        }

        [HttpDelete("skills/{skillId:int}")]
        public async Task<IActionResult> RemoveSkill(int candidateId, int skillId)
        {
            var deny = CheckAccess(candidateId); if (deny != null) return deny;
            await _service.RemoveSkillAsync(candidateId, skillId);
            return NoContent();
        }
    }
}
