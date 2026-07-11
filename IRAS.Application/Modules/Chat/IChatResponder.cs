// IRAS.Application/Modules/Chat/IChatResponder.cs
namespace IRAS.Application.Modules.Chat
{
    // Same swappable pattern as IJdGenerator/IFeedbackGenerator: RuleBasedChatResponder
    // is the deterministic, no-API-key baseline. An LLM-backed responder can implement
    // this same interface later — built from the same ChatContext plus a constrained
    // system prompt — without any caller changing. Pure text generation: no DB access,
    // ChatService owns all persistence and context-fetching.
    public interface IChatResponder
    {
        string Name { get; }
        bool IsAi { get; }

        Task<ChatReply> RespondAsync(string message, ChatContext context, CancellationToken ct);
    }
}
