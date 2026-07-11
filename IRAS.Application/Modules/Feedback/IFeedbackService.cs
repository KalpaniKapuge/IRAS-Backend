// IRAS.Application/Modules/Feedback/IFeedbackService.cs
using IRAS.Application.Modules.Feedback.DTOs;

namespace IRAS.Application.Modules.Feedback
{
    public interface IFeedbackService
    {
        // Called by ApplicationService when an application transitions to Rejected —
        // not exposed as its own endpoint. Idempotent: a second call for an application
        // that already has a draft is a no-op.
        Task GenerateDraftAsync(int applicationId, CancellationToken ct);

        Task<FeedbackDto> GetForEmployerAsync(int employerId, int applicationId, CancellationToken ct);

        Task<FeedbackDto> ReviewAsync(int employerId, int applicationId, ReviewFeedbackRequest request, CancellationToken ct);

        // Returns null until the employer has approved/edited and delivery has actually
        // happened — a candidate must never see an unreviewed AI draft.
        Task<FeedbackDto?> GetMyFeedbackAsync(int candidateId, int applicationId, CancellationToken ct);
    }
}
