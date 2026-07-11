// IRAS.Application/Modules/Applications/IApplicationService.cs
using IRAS.Application.Modules.Applications.DTOs;

namespace IRAS.Application.Modules.Applications
{
    public interface IApplicationService
    {
        Task<ApplicationDto> ApplyAsync(int candidateId, ApplyForJobRequest request, CancellationToken ct);
        Task<List<ApplicationDto>> GetMyApplicationsAsync(int candidateId, CancellationToken ct);
        Task<List<RankedApplicantDto>> GetRankedApplicantsAsync(int employerId, int jobId, CancellationToken ct);

        // Rejecting an application (Module 9) auto-generates a feedback draft for the
        // employer to review — see IFeedbackService.
        Task UpdateStatusAsync(int employerId, int applicationId, UpdateApplicationStatusRequest request, CancellationToken ct);
    }
}
