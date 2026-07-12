// IRAS.Application/Modules/Admin/JobModerationService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Audit;
using IRAS.Application.Modules.Jobs.DTOs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Admin
{
    public class JobModerationService : IJobModerationService
    {
        private const string EntityType = "Job";

        private readonly IrasDbContext _db;
        private readonly IAuditLogService _audit;

        public JobModerationService(IrasDbContext db, IAuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<JobSummaryDto>> GetAllAsync(string? status, CancellationToken ct)
        {
            var q = _db.Jobs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = ParseEnum<JobStatus>(status, nameof(status));
                q = q.Where(j => j.Status == parsed);
            }

            return await q.OrderByDescending(j => j.JobId)
                .Select(j => new JobSummaryDto
                {
                    JobId = j.JobId,
                    Title = j.Title,
                    CompanyName = j.Employer.CompanyName,
                    SeniorityLevel = j.SeniorityLevel,
                    EmploymentType = j.EmploymentType.ToString(),
                    Location = j.Location,
                    Status = j.Status.ToString(),
                    PostedAt = j.PostedAt,
                    ClosingDate = j.ClosingDate,
                    RequiredSkillCount = j.RequiredSkills.Count
                })
                .ToListAsync(ct);
        }

        public async Task ForceCloseAsync(int adminId, int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");
            if (job.Status != JobStatus.Published)
                throw new InvalidOperationException($"Only published jobs can be closed (current: {job.Status}).");

            job.Status = JobStatus.Closed;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "JobForceClosed", EntityType, jobId, ct);
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }
    }
}
