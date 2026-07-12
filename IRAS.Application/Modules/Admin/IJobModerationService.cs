// IRAS.Application/Modules/Admin/IJobModerationService.cs
using IRAS.Application.Modules.Jobs.DTOs;

namespace IRAS.Application.Modules.Admin
{
    public interface IJobModerationService
    {
        // Unlike IJobService.GetMyJobsAsync (scoped to one employer), this spans every
        // employer — the "Job Post Monitoring" item in the Admin workflow.
        Task<List<JobSummaryDto>> GetAllAsync(string? status, CancellationToken ct);

        // Admin moderation power: close any published job without needing to know or
        // route through its owning employer's ID.
        Task ForceCloseAsync(int adminId, int jobId, CancellationToken ct);
    }
}
