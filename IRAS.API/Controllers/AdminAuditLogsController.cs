// IRAS.API/Controllers/AdminAuditLogsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.Application.Common.Audit;

namespace IRAS.API.Controllers
{
    // "Audit Logs" in the Admin workflow — a read-only trail of administrative actions
    // (see IAuditLogService for exactly which actions are recorded and why).
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AdminAuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _service;
        public AdminAuditLogsController(IAuditLogService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 100, CancellationToken ct = default)
            => Ok(await _service.GetRecentAsync(take, ct));
    }
}
