// IRAS.API/Controllers/AdminReportsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.Application.Modules.Admin;

namespace IRAS.API.Controllers
{
    // "Reports" in the Admin workflow.
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly IReportingService _service;
        public AdminReportsController(IReportingService service) => _service = service;

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
            => Ok(await _service.GetDashboardAsync(ct));
    }
}
