// IRAS.Application/Common/Notifications/NotificationDto.cs
namespace IRAS.Application.Common.Notifications
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
