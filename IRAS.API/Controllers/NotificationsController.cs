// IRAS.API/Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Common.Notifications;

namespace IRAS.API.Controllers
{
    // Notifications apply to any authenticated user regardless of role (a candidate gets
    // JobMatch/ApplicationUpdate notifications, an employer could get others later), so
    // this follows the same "/me" convention as GET api/auth/me instead of a
    // role-specific path parameter.
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationsController(INotificationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetMine(CancellationToken ct)
            => Ok(await _service.GetMyNotificationsAsync(User.GetUserId(), ct));

        [HttpPost("{notificationId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId, CancellationToken ct)
        {
            await _service.MarkAsReadAsync(User.GetUserId(), notificationId, ct);
            return NoContent();
        }
    }
}
