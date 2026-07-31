// IRAS.Application/Modules/Interviews/IInterviewService.cs
using IRAS.Application.Modules.Interviews.DTOs;

namespace IRAS.Application.Modules.Interviews
{
    public interface IInterviewService
    {
        // Scheduling also advances the application to ApplicationStatus.Interview (via
        // IApplicationService.UpdateStatusAsync) the first time an interview is booked for
        // it — an application already at or past that stage is left alone.
        Task<InterviewDto> ScheduleAsync(int employerId, int applicationId, ScheduleInterviewRequest request, CancellationToken ct);

        Task<InterviewDto> RescheduleAsync(int employerId, int interviewId, RescheduleInterviewRequest request, CancellationToken ct);

        Task CancelAsync(int employerId, int interviewId, CancelInterviewRequest request, CancellationToken ct);

        Task UpdateOutcomeAsync(int employerId, int interviewId, UpdateInterviewOutcomeRequest request, CancellationToken ct);

        Task<List<InterviewDto>> GetForApplicationAsync(int employerId, int applicationId, CancellationToken ct);

        // All interviews across every job this employer owns — soonest first.
        Task<List<InterviewDto>> GetForEmployerAsync(int employerId, CancellationToken ct);

        Task<List<InterviewDto>> GetForCandidateAsync(int candidateId, CancellationToken ct);
    }
}
