// IRAS.Application/Common/Notifications/NotificationService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Email;
using IRAS.Domain.Entities.Engagement;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Common.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IrasDbContext _db;
        private readonly IEmailSender _email;

        public NotificationService(IrasDbContext db, IEmailSender email)
        {
            _db = db;
            _email = email;
        }

        public async Task NotifyAsync(
            int userId, NotificationType type, string title, string message,
            RelatedEntityType? relatedEntityType, int? relatedEntityId,
            DeliveryChannel channel, CancellationToken ct)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Channel = channel,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            });
            await _db.SaveChangesAsync(ct);

            if (channel is DeliveryChannel.Email or DeliveryChannel.Both)
            {
                var email = await _db.Users.Where(u => u.UserId == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
                if (email != null)
                    await _email.SendAsync(email, title, message, ct);
            }
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync(int userId, CancellationToken ct)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Type = n.Type.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    RelatedEntityType = n.RelatedEntityType != null ? n.RelatedEntityType.ToString() : null,
                    RelatedEntityId = n.RelatedEntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(ct);
        }

        public async Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, ct)
                ?? throw new KeyNotFoundException("Notification not found.");

            notification.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
