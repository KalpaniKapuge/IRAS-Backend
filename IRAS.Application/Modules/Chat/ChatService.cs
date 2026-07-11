// IRAS.Application/Modules/Chat/ChatService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Chat.DTOs;
using IRAS.Application.Modules.Matching;
using IRAS.Application.Modules.SkillGaps;
using IRAS.Application.Common.Notifications;
using IRAS.Domain.Entities.Engagement;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Chat
{
    public class ChatService : IChatService
    {
        private readonly IrasDbContext _db;
        private readonly IChatResponder _responder;
        private readonly ISkillGapService _skillGaps;
        private readonly IApplicationService _applications;
        private readonly IJobMatchingService _matching;
        private readonly INotificationService _notifications;

        public ChatService(
            IrasDbContext db, IChatResponder responder, ISkillGapService skillGaps,
            IApplicationService applications, IJobMatchingService matching, INotificationService notifications)
        {
            _db = db;
            _responder = responder;
            _skillGaps = skillGaps;
            _applications = applications;
            _matching = matching;
            _notifications = notifications;
        }

        public async Task<ChatReplyDto> SendMessageAsync(int userId, string role, SendChatMessageRequest request, CancellationToken ct)
        {
            var conversation = await _db.ChatConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastActivity)
                .FirstOrDefaultAsync(ct);
            if (conversation is null)
            {
                conversation = new ChatConversation { UserId = userId };
                _db.ChatConversations.Add(conversation);
                await _db.SaveChangesAsync(ct);
            }

            var context = await BuildContextAsync(userId, role, ct);
            var reply = await _responder.RespondAsync(request.Message, context, ct);

            _db.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.ConversationId,
                Sender = ChatSender.User,
                Content = request.Message,
                Intent = reply.Intent
            });
            _db.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.ConversationId,
                Sender = ChatSender.Bot,
                Content = reply.Text
            });
            conversation.LastActivity = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new ChatReplyDto { ConversationId = conversation.ConversationId, Reply = reply.Text, Intent = reply.Intent };
        }

        public async Task<List<ChatMessageDto>> GetMyHistoryAsync(int userId, CancellationToken ct)
        {
            return await _db.ChatMessages
                .Where(m => m.Conversation.UserId == userId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    MessageId = m.MessageId,
                    Sender = m.Sender.ToString(),
                    Content = m.Content,
                    Intent = m.Intent,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);
        }

        private async Task<ChatContext> BuildContextAsync(int userId, string role, CancellationToken ct)
        {
            var kb = await _db.KnowledgeBases
                .Where(k => k.IsActive)
                .OrderBy(k => k.KbId)
                .Select(k => new KnowledgeBaseItem(k.Title, k.Content))
                .ToListAsync(ct);

            var isCandidate = role == "Candidate";
            if (!isCandidate)
                return new ChatContext(false, kb, [], [], [], 0);

            var skillGaps = await _skillGaps.GetMyGapSummaryAsync(userId, ct);
            var applications = await _applications.GetMyApplicationsAsync(userId, ct);
            var matches = await _matching.GetMyMatchesAsync(userId, ct);
            var notifications = await _notifications.GetMyNotificationsAsync(userId, ct);
            var unreadCount = notifications.Count(n => !n.IsRead);

            return new ChatContext(true, kb, skillGaps, applications, matches, unreadCount);
        }
    }
}
