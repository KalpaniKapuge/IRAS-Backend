// IRAS.API/Controllers/AdminSystemController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.Application.Modules.Admin;

namespace IRAS.API.Controllers
{
    // "AI Model Monitoring" and "System Settings" in the Admin workflow.
    [ApiController]
    [Route("api/admin/system")]
    [Authorize(Roles = "Admin")]
    public class AdminSystemController : ControllerBase
    {
        private readonly ISystemStatusService _service;
        public AdminSystemController(ISystemStatusService service) => _service = service;

        [HttpGet("ai-status")]
        public async Task<IActionResult> GetAiStatus(CancellationToken ct)
            => Ok(await _service.GetAiModelStatusAsync(ct));

        [HttpGet("settings")]
        public IActionResult GetSettings()
            => Ok(_service.GetSystemSettings());
    }
}
