// IRAS.Application/Modules/Jobs/JobService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.Jobs.DTOs;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Jobs
{
    public class JobService : IJobService
    {
        private readonly IrasDbContext _db;
        private readonly IJdGenerator _jdGenerator;

        public JobService(IrasDbContext db, IJdGenerator jdGenerator)
        {
            _db = db;
            _jdGenerator = jdGenerator;
        }

        // ---- Employer profile ----

        public async Task<EmployerProfileDto> GetEmployerProfileAsync(int employerId)
        {
            var p = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.EmployerId == employerId)
                ?? throw new KeyNotFoundException("Employer profile not found.");

            return new EmployerProfileDto
            {
                EmployerId = p.EmployerId, CompanyName = p.CompanyName, Industry = p.Industry,
                CompanySize = p.CompanySize.ToString(), Website = p.Website,
                Location = p.Location, Description = p.Description
            };
        }

        public async Task UpdateEmployerProfileAsync(int employerId, UpdateEmployerProfileRequest request)
        {
            var p = await _db.EmployerProfiles.FirstOrDefaultAsync(e => e.EmployerId == employerId)
                ?? throw new KeyNotFoundException("Employer profile not found.");

            var size = ParseEnum<CompanySize>(request.CompanySize, nameof(request.CompanySize));

            p.CompanyName = request.CompanyName;
            p.Industry = request.Industry;
            p.CompanySize = size;
            p.Website = request.Website;
            p.Location = request.Location;
            p.Description = request.Description;
            await _db.SaveChangesAsync();
        }

        // ---- Job lifecycle ----

        public async Task<JobDto> CreateJobAsync(int employerId, CreateJobRequest request)
        {
            var employerExists = await _db.EmployerProfiles.AnyAsync(e => e.EmployerId == employerId);
            if (!employerExists) throw new KeyNotFoundException("Employer profile not found.");

            var edu = ParseEnum<EducationLevel>(request.EducationReq, nameof(request.EducationReq));
            var empType = ParseEnum<EmploymentType>(request.EmploymentType, nameof(request.EmploymentType));
            if (request.ClosingDate.HasValue && request.ClosingDate.Value.Date <= DateTime.UtcNow.Date)
                throw new ArgumentException("Closing date must be in the future.");

            // Validate every referenced skill exists BEFORE writing anything
            var skillIds = request.RequiredSkills.Select(s => s.SkillId).Distinct().ToList();
            if (skillIds.Count != request.RequiredSkills.Count)
                throw new ArgumentException("Duplicate skills in the required skills list.");

            var existingIds = await _db.Skills
                .Where(s => skillIds.Contains(s.SkillId))
                .Select(s => s.SkillId).ToListAsync();
            var missing = skillIds.Except(existingIds).ToList();
            if (missing.Count > 0)
                throw new KeyNotFoundException($"Skill id(s) not found in taxonomy: {string.Join(", ", missing)}");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var job = new Job
                {
                    EmployerId = employerId,
                    Title = request.Title.Trim(),
                    SeniorityLevel = request.SeniorityLevel.Trim(),
                    MinExpYears = request.MinExpYears,
                    EducationReq = edu,
                    EmploymentType = empType,
                    Location = request.Location,
                    ClosingDate = request.ClosingDate,
                    Status = JobStatus.Draft
                };
                _db.Jobs.Add(job);
                await _db.SaveChangesAsync();

                foreach (var rs in request.RequiredSkills)
                {
                    var importance = ParseEnum<ImportanceLevel>(rs.Importance, nameof(rs.Importance));

                    _db.JobRequiredSkills.Add(new JobRequiredSkill
                    {
                        JobId = job.JobId,
                        SkillId = rs.SkillId,
                        Importance = importance,
                        // sensible defaults the scoring engine will use later
                        Weight = rs.Weight ?? (importance == ImportanceLevel.MustHave ? 1.0m : 0.5m),
                        MinYears = rs.MinYears
                    });
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetJobInternalAsync(job.JobId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<JobDto> GetJobAsync(int jobId, int requesterId, string requesterRole)
        {
            var job = await GetJobInternalAsync(jobId);

            // Visibility rule: drafts/closed are private to the owner (and Admin);
            // published jobs are visible to everyone.
            var isOwner = job.EmployerId == requesterId;
            var isAdmin = requesterRole == "Admin";
            if (job.Status != JobStatus.Published.ToString() && !isOwner && !isAdmin)
                throw new KeyNotFoundException("Job not found.");   // don't reveal that a draft exists

            return job;
        }

        public async Task<List<JobSummaryDto>> GetMyJobsAsync(int employerId)
        {
            return await _db.Jobs
                .Where(j => j.EmployerId == employerId)
                .OrderByDescending(j => j.JobId)
                .Select(j => new JobSummaryDto
                {
                    JobId = j.JobId, Title = j.Title,
                    CompanyName = j.Employer.CompanyName,
                    SeniorityLevel = j.SeniorityLevel,
                    EmploymentType = j.EmploymentType.ToString(),
                    Location = j.Location, Status = j.Status.ToString(),
                    PostedAt = j.PostedAt, ClosingDate = j.ClosingDate,
                    RequiredSkillCount = j.RequiredSkills.Count
                }).ToListAsync();
        }

        public async Task<List<JobSummaryDto>> GetPublishedJobsAsync(string? query)
        {
            var q = _db.Jobs.Where(j => j.Status == JobStatus.Published);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                q = q.Where(j => j.Title.Contains(term)
                              || j.Employer.CompanyName.Contains(term)
                              || (j.Location != null && j.Location.Contains(term)));
            }

            return await q.OrderByDescending(j => j.PostedAt)
                .Select(j => new JobSummaryDto
                {
                    JobId = j.JobId, Title = j.Title,
                    CompanyName = j.Employer.CompanyName,
                    SeniorityLevel = j.SeniorityLevel,
                    EmploymentType = j.EmploymentType.ToString(),
                    Location = j.Location, Status = j.Status.ToString(),
                    PostedAt = j.PostedAt, ClosingDate = j.ClosingDate,
                    RequiredSkillCount = j.RequiredSkills.Count
                }).ToListAsync();
        }

        // ---- JD generation (human-in-the-loop workflow) ----

        public async Task<GenerateJdResponse> GenerateJdAsync(int employerId, int jobId, GenerateJdRequest request)
        {
            var job = await GetOwnedJobAsync(employerId, jobId);
            if (job.Status != JobStatus.Draft)
                throw new InvalidOperationException("JD can only be generated while the job is a draft.");

            var employer = await _db.EmployerProfiles.FirstAsync(e => e.EmployerId == employerId);
            var skills = await _db.JobRequiredSkills
                .Where(j => j.JobId == jobId)
                .Select(j => new { j.Skill.SkillName, j.Importance, j.MinYears })
                .ToListAsync();

            var jd = await _jdGenerator.GenerateAsync(
                job,
                skills.Select(s => (s.SkillName, s.Importance.ToString(), s.MinYears)),
                employer.CompanyName, employer.Description, request.AdditionalNotes);

            // Store the raw employer input alongside the output
            job.RequirementInput = request.AdditionalNotes;
            job.GeneratedJd = jd;
            job.IsAiGenerated = _jdGenerator.IsAi;
            await _db.SaveChangesAsync();

            return new GenerateJdResponse
            {
                GeneratedJd = jd,
                IsAiGenerated = _jdGenerator.IsAi,
                GeneratorUsed = _jdGenerator.Name
            };
        }

        public async Task UpdateJdAsync(int employerId, int jobId, UpdateJdRequest request)
        {
            var job = await GetOwnedJobAsync(employerId, jobId);
            if (job.Status == JobStatus.Closed || job.Status == JobStatus.Archived)
                throw new InvalidOperationException("Cannot edit the JD of a closed or archived job.");

            job.GeneratedJd = request.JdText;
            // The employer has edited it — it's no longer purely AI output.
            job.IsAiGenerated = false;
            await _db.SaveChangesAsync();
        }

        public async Task PublishJobAsync(int employerId, int jobId)
        {
            var job = await GetOwnedJobAsync(employerId, jobId);

            if (job.Status != JobStatus.Draft)
                throw new InvalidOperationException($"Only draft jobs can be published (current: {job.Status}).");
            if (string.IsNullOrWhiteSpace(job.GeneratedJd))
                throw new InvalidOperationException("A job description is required before publishing. Generate or write one first.");

            var hasMustHave = await _db.JobRequiredSkills
                .AnyAsync(j => j.JobId == jobId && j.Importance == ImportanceLevel.MustHave);
            if (!hasMustHave)
                throw new InvalidOperationException("At least one must-have skill is required before publishing.");

            job.Status = JobStatus.Published;
            job.PostedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task CloseJobAsync(int employerId, int jobId)
        {
            var job = await GetOwnedJobAsync(employerId, jobId);
            if (job.Status != JobStatus.Published)
                throw new InvalidOperationException("Only published jobs can be closed.");
            job.Status = JobStatus.Closed;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteDraftJobAsync(int employerId, int jobId)
        {
            var job = await GetOwnedJobAsync(employerId, jobId);
            if (job.Status != JobStatus.Draft)
                throw new InvalidOperationException("Only draft jobs can be deleted. Close published jobs instead.");

            var requiredSkills = _db.JobRequiredSkills.Where(j => j.JobId == jobId);
            _db.JobRequiredSkills.RemoveRange(requiredSkills);
            _db.Jobs.Remove(job);
            await _db.SaveChangesAsync();
        }

        // ---- helpers ----

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }

        private async Task<Job> GetOwnedJobAsync(int employerId, int jobId)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId)
                ?? throw new KeyNotFoundException("Job not found.");
            if (job.EmployerId != employerId)
                throw new KeyNotFoundException("Job not found.");
            return job;
        }

        private async Task<JobDto> GetJobInternalAsync(int jobId)
        {
            var job = await _db.Jobs
                .Include(j => j.Employer)
                .Include(j => j.RequiredSkills).ThenInclude(rs => rs.Skill)
                .FirstOrDefaultAsync(j => j.JobId == jobId)
                ?? throw new KeyNotFoundException("Job not found.");

            return new JobDto
            {
                JobId = job.JobId,
                EmployerId = job.EmployerId,
                CompanyName = job.Employer.CompanyName,
                Title = job.Title,
                SeniorityLevel = job.SeniorityLevel,
                RequirementInput = job.RequirementInput,
                GeneratedJd = job.GeneratedJd,
                IsAiGenerated = job.IsAiGenerated,
                MinExpYears = job.MinExpYears,
                EducationReq = job.EducationReq.ToString(),
                EmploymentType = job.EmploymentType.ToString(),
                Location = job.Location,
                Status = job.Status.ToString(),
                PostedAt = job.PostedAt,
                ClosingDate = job.ClosingDate,
                RequiredSkills = job.RequiredSkills.Select(rs => new JobRequiredSkillDto
                {
                    SkillId = rs.SkillId, SkillName = rs.Skill.SkillName,
                    Importance = rs.Importance.ToString(), Weight = rs.Weight, MinYears = rs.MinYears
                }).ToList()
            };
        }
    }
}