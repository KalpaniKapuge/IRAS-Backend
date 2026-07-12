// IRAS.API/Controllers/KnowledgeBaseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IRAS.API.Extensions;
using IRAS.Application.Modules.KnowledgeBase;
using IRAS.Application.Modules.KnowledgeBase.DTOs;

namespace IRAS.API.Controllers
{
    // Admin-only management of the chatbot's knowledge base — the "Chatbot Knowledge
    // Management" item in the Admin workflow. The chatbot itself only ever reads
    // IsActive entries (see ChatService); this is where an admin curates that content.
    [ApiController]
    [Route("api/admin/knowledge-base")]
    [Authorize(Roles = "Admin")]
    public class KnowledgeBaseController : ControllerBase
    {
        private readonly IKnowledgeBaseService _service;
        public KnowledgeBaseController(IKnowledgeBaseService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{kbId:int}")]
        public async Task<IActionResult> GetById(int kbId, CancellationToken ct)
            => Ok(await _service.GetByIdAsync(kbId, ct));

        [HttpPost]
        public async Task<IActionResult> Create(UpsertKnowledgeBaseRequest request, CancellationToken ct)
            => Ok(await _service.CreateAsync(User.GetUserId(), request, ct));

        [HttpPut("{kbId:int}")]
        public async Task<IActionResult> Update(int kbId, UpsertKnowledgeBaseRequest request, CancellationToken ct)
        {
            await _service.UpdateAsync(User.GetUserId(), kbId, request, ct);
            return NoContent();
        }

        [HttpDelete("{kbId:int}")]
        public async Task<IActionResult> Delete(int kbId, CancellationToken ct)
        {
            await _service.DeleteAsync(User.GetUserId(), kbId, ct);
            return NoContent();
        }
    }
}
