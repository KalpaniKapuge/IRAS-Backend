// IRAS.API/Controllers/EmployerProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Jobs;
using IRAS.Application.Modules.Jobs.DTOs;

namespace IRAS.API.Controllers
{
    [ApiController]
    [Route("api/employers/{employerId:int}")]
    [Authorize]
    public class EmployerProfileController : ControllerBase
    {
        private readonly IJobService _service;
        public EmployerProfileController(IJobService service) => _service = service;

        private IActionResult? CheckAccess(int employerId)
        {
            var role = User.GetRole();
            if (role == "Admin") return null;
            if (role == "Employer" && User.GetUserId() == employerId) return null;
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile(int employerId)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            return Ok(await _service.GetEmployerProfileAsync(employerId));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(int employerId, UpdateEmployerProfileRequest request)
        {
            var deny = CheckAccess(employerId); if (deny != null) return deny;
            await _service.UpdateEmployerProfileAsync(employerId, request);
            return NoContent();
        }
    }
}