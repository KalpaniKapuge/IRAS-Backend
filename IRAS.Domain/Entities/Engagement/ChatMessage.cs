// IRAS.Domain/Entities/Engagement/ChatMessage.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Engagement
{
    public class ChatMessage
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public ChatSender Sender { get; set; }
        public string Content { get; set; } = null!;
        public string? Intent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ChatConversation Conversation { get; set; } = null!;
    }
}