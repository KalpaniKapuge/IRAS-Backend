// IRAS.API/Controllers/AdminUsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Admin;
using IRAS.Application.Modules.Admin.DTOs;

namespace IRAS.API.Controllers
{
    // "User Management" in the Admin workflow.
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserManagementService _service;
        public AdminUsersController(IUserManagementService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? role, CancellationToken ct)
            => Ok(await _service.GetAllAsync(role, ct));

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetById(int userId, CancellationToken ct)
            => Ok(await _service.GetByIdAsync(userId, ct));

        [HttpPut("{userId:int}/status")]
        public async Task<IActionResult> SetActive(int userId, SetUserActiveRequest request, CancellationToken ct)
        {
            await _service.SetActiveAsync(User.GetUserId(), userId, request.IsActive, ct);
            return NoContent();
        }
    }
}
