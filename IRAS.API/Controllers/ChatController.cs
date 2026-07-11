// IRAS.API/Controllers/ChatController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.Chat;
using IRAS.Application.Modules.Chat.DTOs;

namespace IRAS.API.Controllers
{
    // Module 10 — any authenticated user can chat; personalized answers (skill gaps,
    // application status, job matches) only apply for candidates. Follows the "/me"
    // convention (like GET api/auth/me) since a conversation is inherently per-user.
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _service;
        public ChatController(IChatService service) => _service = service;

        [HttpPost("messages")]
        public async Task<IActionResult> Send(SendChatMessageRequest request, CancellationToken ct)
            => Ok(await _service.SendMessageAsync(User.GetUserId(), User.GetRole(), request, ct));

        [HttpGet("messages")]
        public async Task<IActionResult> GetHistory(CancellationToken ct)
            => Ok(await _service.GetMyHistoryAsync(User.GetUserId(), ct));
    }
}
