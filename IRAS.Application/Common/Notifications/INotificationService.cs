// IRAS.Application/Common/Notifications/INotificationService.cs
using IRAS.Domain.Enums;

namespace IRAS.Application.Common.Notifications
{
    public interface INotificationService
    {
        // Always persists an in-app Notification row; additionally emails the user
        // when channel is Email or Both (see IEmailSender for how that's dispatched).
        Task NotifyAsync(
            int userId, NotificationType type, string title, string message,
            RelatedEntityType? relatedEntityType, int? relatedEntityId,
            DeliveryChannel channel, CancellationToken ct);

        Task<List<NotificationDto>> GetMyNotificationsAsync(int userId, CancellationToken ct);

        Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct);
    }
}
