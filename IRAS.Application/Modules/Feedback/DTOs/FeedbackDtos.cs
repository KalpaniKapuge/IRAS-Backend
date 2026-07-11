// IRAS.Application/Modules/Feedback/DTOs/FeedbackDtos.cs
using System.ComponentModel.DataAnnotations;
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.Feedback.DTOs
{
    public class FeedbackDto
    {
        public int FeedbackId { get; set; }
        public int ApplicationId { get; set; }
        public string MessageText { get; set; } = null!;
        public string ApprovalStatus { get; set; } = null!;
        public string DeliveryStatus { get; set; } = null!;
        public string Channel { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }

    public class ReviewFeedbackRequest : IValidatableObject
    {
        [Required]
        public string Decision { get; set; } = null!;   // Approved | Edited | Rejected

        [StringLength(2000)]
        public string? EditedMessageText { get; set; }

        [Required]
        public string Channel { get; set; } = null!;     // Email | InApp | Both

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.Equals(Decision, nameof(ApprovalStatus.Edited), StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(EditedMessageText))
            {
                yield return new ValidationResult(
                    "EditedMessageText is required when the decision is Edited.", [nameof(EditedMessageText)]);
            }
        }
    }
}
