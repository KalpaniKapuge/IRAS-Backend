// IRAS.Application/Modules/Chat/IChatService.cs
using IRAS.Application.Modules.Chat.DTOs;

namespace IRAS.Application.Modules.Chat
{
    public interface IChatService
    {
        Task<ChatReplyDto> SendMessageAsync(int userId, string role, SendChatMessageRequest request, CancellationToken ct);

        Task<List<ChatMessageDto>> GetMyHistoryAsync(int userId, CancellationToken ct);
    }
}
