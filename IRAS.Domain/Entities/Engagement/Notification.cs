// IRAS.Domain/Entities/Engagement/Notification.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Engagement
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DeliveryChannel Channel { get; set; }
        public RelatedEntityType? RelatedEntityType { get; set; }   // correction #6
        public int? RelatedEntityId { get; set; }                   // correction #6
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}