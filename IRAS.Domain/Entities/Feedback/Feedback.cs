// IRAS.Domain/Entities/Feedback/Feedback.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Applications;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Feedback
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        public int ApplicationId { get; set; }
        public string MessageText { get; set; } = null!;
        public int? ApprovedBy { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PendingReview;
        public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Queued;
        public DeliveryChannel Channel { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }

        public Application Application { get; set; } = null!;
        public User? ApprovedByUser { get; set; }
    }
}