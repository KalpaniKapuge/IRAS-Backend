// IRAS.Application/Modules/Feedback/FeedbackService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Modules.Feedback.DTOs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

// "Feedback" (the entity) and "IRAS.Application.Modules.Feedback" (this module's own
// namespace) share a name — an unqualified `Feedback` here resolves to the enclosing
// namespace, not the entity, and fails to compile (CS0118). Alias it, same fix as
// AppEntity in ApplicationService.cs for the identical Application/Application clash.
using FeedbackEntity = IRAS.Domain.Entities.Feedback.Feedback;

namespace IRAS.Application.Modules.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IrasDbContext _db;
        private readonly IFeedbackGenerator _generator;
        private readonly INotificationService _notifications;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            IrasDbContext db, IFeedbackGenerator generator, INotificationService notifications,
            ILogger<FeedbackService> logger)
        {
            _db = db;
            _generator = generator;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task GenerateDraftAsync(int applicationId, CancellationToken ct)
        {
            var alreadyExists = await _db.Feedbacks.AnyAsync(f => f.ApplicationId == applicationId, ct);
            if (alreadyExists) return;

            var application = await _db.Applications
                .Include(a => a.Job).ThenInclude(j => j.Employer)
                .Include(a => a.SkillGaps).ThenInclude(g => g.Skill)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct);
            if (application is null) return;

            var gaps = application.SkillGaps
                .Select(g => (g.Skill.SkillName, g.Importance.ToString(), g.Suggestion));

            var message = await _generator.GenerateAsync(
                application.Job.Title, application.Job.Employer.CompanyName, application.TotalScore, gaps, ct);

            _db.Feedbacks.Add(new FeedbackEntity
            {
                ApplicationId = applicationId,
                MessageText = message,
                ApprovalStatus = ApprovalStatus.PendingReview,
                DeliveryStatus = DeliveryStatus.Queued,
                Channel = DeliveryChannel.InApp
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task<FeedbackDto> GetForEmployerAsync(int employerId, int applicationId, CancellationToken ct)
        {
            var feedback = await GetOwnedByEmployerAsync(employerId, applicationId, ct);
            return ToDto(feedback);
        }

        public async Task<FeedbackDto> ReviewAsync(
            int employerId, int applicationId, ReviewFeedbackRequest request, CancellationToken ct)
        {
            var feedback = await GetOwnedByEmployerAsync(employerId, applicationId, ct);
            if (feedback.ApprovalStatus != ApprovalStatus.PendingReview)
                throw new InvalidOperationException($"This feedback was already reviewed ({feedback.ApprovalStatus}).");

            // Note: ApprovalStatus.Rejected here means "the employer chose not to send this
            // draft" — a different thing from ApplicationStatus.Rejected (the candidate not
            // getting the job), which is what triggered the draft's existence in the first place.
            var decision = ParseEnum<ApprovalStatus>(request.Decision, nameof(request.Decision));
            if (decision == ApprovalStatus.PendingReview)
                throw new ArgumentException("Decision must be Approved, Edited, or Rejected.");

            if (decision == ApprovalStatus.Edited)
                feedback.MessageText = request.EditedMessageText!;

            feedback.ApprovalStatus = decision;
            feedback.ApprovedBy = employerId;
            feedback.Channel = ParseEnum<DeliveryChannel>(request.Channel, nameof(request.Channel));

            if (decision is ApprovalStatus.Approved or ApprovalStatus.Edited)
            {
                try
                {
                    await _notifications.NotifyAsync(
                        feedback.Application.CandidateId, NotificationType.Feedback,
                        $"Update on your application for {feedback.Application.Job.Title}",
                        feedback.MessageText, RelatedEntityType.Feedback, feedback.FeedbackId,
                        feedback.Channel, ct);
                    feedback.DeliveryStatus = DeliveryStatus.Sent;
                    feedback.SentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deliver feedback {FeedbackId}", feedback.FeedbackId);
                    feedback.DeliveryStatus = DeliveryStatus.Failed;
                }
            }
            // decision == Rejected: the draft is discarded — DeliveryStatus stays Queued
            // (never sent) rather than trying to shoehorn "discarded" into a Sent/Failed value.

            await _db.SaveChangesAsync(ct);
            return ToDto(feedback);
        }

        public async Task<FeedbackDto?> GetMyFeedbackAsync(int candidateId, int applicationId, CancellationToken ct)
        {
            var feedback = await _db.Feedbacks
                .Include(f => f.Application)
                .FirstOrDefaultAsync(f => f.ApplicationId == applicationId && f.Application.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Feedback not found.");

            // A pending or discarded draft is an internal artifact — only ever surface
            // feedback that was actually reviewed and delivered.
            return feedback.DeliveryStatus == DeliveryStatus.Sent ? ToDto(feedback) : null;
        }

        // ---- helpers ----

        private async Task<FeedbackEntity> GetOwnedByEmployerAsync(int employerId, int applicationId, CancellationToken ct)
        {
            var feedback = await _db.Feedbacks
                .Include(f => f.Application).ThenInclude(a => a.Job)
                .FirstOrDefaultAsync(f => f.ApplicationId == applicationId, ct)
                ?? throw new KeyNotFoundException("Feedback not found.");
            if (feedback.Application.Job.EmployerId != employerId)
                throw new KeyNotFoundException("Feedback not found.");
            return feedback;
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }

        private static FeedbackDto ToDto(FeedbackEntity f) => new()
        {
            FeedbackId = f.FeedbackId,
            ApplicationId = f.ApplicationId,
            MessageText = f.MessageText,
            ApprovalStatus = f.ApprovalStatus.ToString(),
            DeliveryStatus = f.DeliveryStatus.ToString(),
            Channel = f.Channel.ToString(),
            GeneratedAt = f.GeneratedAt,
            SentAt = f.SentAt
        };
    }
}
