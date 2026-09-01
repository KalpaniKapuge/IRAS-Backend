// IRAS.Application/Modules/Assessments/IAssessmentService.cs
using IRAS.Application.Modules.Assessments.DTOs;

namespace IRAS.Application.Modules.Assessments
{
    public interface IAssessmentService
    {
        Task<AssessmentStatusDto> GetStatusAsync(int candidateId, int jobId, CancellationToken ct);
        Task<StartAssessmentResponse> StartAsync(int candidateId, int jobId, CancellationToken ct);
        Task<AssessmentResultDto> SubmitAsync(int candidateId, int jobId, SubmitAssessmentRequest request, CancellationToken ct);

        // Used internally by ApplicationService.ApplyAsync — true when the job doesn't
        // require an assessment, or the candidate has a completed attempt for it.
        Task<bool> HasPassedGateAsync(int candidateId, int jobId, CancellationToken ct);

        // 0..1 fractional score for the candidate's completed attempt at this job, or null
        // if not required/not completed — fed into IScoringService.ComputeTotalScore.
        Task<decimal?> GetScoreAsync(int candidateId, int jobId, CancellationToken ct);

        // Full quiz detail (questions, answer key, and the candidate's actual answers) for
        // the employer reviewing one applicant — null if the candidate has no completed
        // attempt for that application's job. Throws KeyNotFoundException if the application
        // doesn't belong to a job owned by employerId.
        Task<EmployerAssessmentReviewDto?> GetReviewForEmployerAsync(int employerId, int applicationId, CancellationToken ct);
    }
}
