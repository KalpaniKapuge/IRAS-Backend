// IRAS.Application/Modules/Chat/ContextBuilders/IChatContextBuilder.cs
namespace IRAS.Application.Modules.Chat.ContextBuilders
{
    // One implementation per role — ChatService picks the matching builder by Role
    // instead of branching on role itself, so adding a future role only means adding
    // a new builder, not editing ChatService or ChatContext's construction logic.
    public interface IChatContextBuilder
    {
        string Role { get; }

        Task<ChatContext> BuildAsync(int userId, IReadOnlyList<KnowledgeBaseItem> kb, int unreadCount, CancellationToken ct);
    }
}
