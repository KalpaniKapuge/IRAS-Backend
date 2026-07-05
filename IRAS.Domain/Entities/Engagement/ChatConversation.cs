// IRAS.Domain/Entities/Engagement/ChatConversation.cs
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Engagement
{
    public class ChatConversation
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string? Topic { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}