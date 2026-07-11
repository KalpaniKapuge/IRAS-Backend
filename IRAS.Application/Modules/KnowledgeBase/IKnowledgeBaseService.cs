// IRAS.Application/Modules/KnowledgeBase/IKnowledgeBaseService.cs
using IRAS.Application.Modules.KnowledgeBase.DTOs;

namespace IRAS.Application.Modules.KnowledgeBase
{
    public interface IKnowledgeBaseService
    {
        // Admin-facing management. The chatbot itself (ChatService) reads active
        // entries directly — this interface is for maintaining the content, not serving it.
        Task<List<KnowledgeBaseDto>> GetAllAsync(CancellationToken ct);
        Task<KnowledgeBaseDto> GetByIdAsync(int kbId, CancellationToken ct);
        Task<KnowledgeBaseDto> CreateAsync(int adminId, UpsertKnowledgeBaseRequest request, CancellationToken ct);
        Task UpdateAsync(int adminId, int kbId, UpsertKnowledgeBaseRequest request, CancellationToken ct);
        Task DeleteAsync(int kbId, CancellationToken ct);
    }
}
