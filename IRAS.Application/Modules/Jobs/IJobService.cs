// IRAS.Application/Modules/Jobs/IJobService.cs
using IRAS.Application.Modules.Jobs.DTOs;

namespace IRAS.Application.Modules.Jobs
{
    public interface IJobService
    {
        Task<EmployerProfileDto> GetEmployerProfileAsync(int employerId);
        Task UpdateEmployerProfileAsync(int employerId, UpdateEmployerProfileRequest request);

        Task<JobDto> CreateJobAsync(int employerId, CreateJobRequest request);
        Task<JobDto> GetJobAsync(int jobId, int requesterId, string requesterRole);
        Task<List<JobSummaryDto>> GetMyJobsAsync(int employerId);
        Task<List<JobSummaryDto>> GetPublishedJobsAsync(string? query);

        Task<GenerateJdResponse> GenerateJdAsync(int employerId, int jobId, GenerateJdRequest request);
        Task UpdateJdAsync(int employerId, int jobId, UpdateJdRequest request);

        Task PublishJobAsync(int employerId, int jobId);
        Task CloseJobAsync(int employerId, int jobId);
        Task DeleteDraftJobAsync(int employerId, int jobId);
    }
}