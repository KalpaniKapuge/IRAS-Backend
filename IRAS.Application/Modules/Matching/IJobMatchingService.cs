// IRAS.Application/Modules/Matching/IJobMatchingService.cs
using IRAS.Application.Modules.Matching.DTOs;

namespace IRAS.Application.Modules.Matching
{
    public interface IJobMatchingService
    {
        // Scans every opted-in candidate with a parsed primary resume against the given
        // (published) job, scores them with the same formula Module 6 uses for real
        // applications, and notifies whoever clears the configured threshold. Idempotent
        // per job/candidate pair — re-running a job that's already been matched only
        // scores candidates who weren't previously considered.
        Task RunMatchingForJobAsync(int jobId, CancellationToken ct);

        Task<List<JobMatchDto>> GetMyMatchesAsync(int candidateId, CancellationToken ct);
    }
}
