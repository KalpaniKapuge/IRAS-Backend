// IRAS.Application/Modules/Chat/DTOs/ChatDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Chat.DTOs
{
    public class SendChatMessageRequest
    {
        [Required, StringLength(1000, MinimumLength = 1)]
        public string Message { get; set; } = null!;
    }

    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public string Sender { get; set; } = null!;   // "User" | "Bot"
        public string Content { get; set; } = null!;
        public string? Intent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChatReplyDto
    {
        public int ConversationId { get; set; }
        public string Reply { get; set; } = null!;
        public string? Intent { get; set; }
    }
}
