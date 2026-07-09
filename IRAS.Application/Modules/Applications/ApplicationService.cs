// IRAS.Application/Modules/Applications/ApplicationService.cs
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Domain.Entities.Applications;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

// "Application" (the entity) and "IRAS.Application" (this project's root namespace)
// share a name — an unqualified `Application` here would resolve to the namespace,
// not the entity, and fail to compile. Alias it to keep the rest of the file readable.
using AppEntity = IRAS.Domain.Entities.Applications.Application;

namespace IRAS.Application.Modules.Applications
{
    public class ApplicationService : IApplicationService
    {
        // Experience/education match are computed and stored for transparency in the UI
        // but deliberately excluded from TotalScore — see IScoringService.ComputeTotalScore
        // for the two-input weighted formula that's actually used to rank applications.
        private static readonly Expression<Func<AppEntity, ApplicationDto>> ToApplicationDto = a => new ApplicationDto
        {
            ApplicationId = a.ApplicationId,
            JobId = a.JobId,
            JobTitle = a.Job.Title,
            CompanyName = a.Job.Employer.CompanyName,
            Status = a.Status.ToString(),
            TotalScore = a.TotalScore,
            SkillMatch = a.SkillMatch,
            ExperienceMatch = a.ExperienceMatch,
            EducationMatch = a.EducationMatch,
            SemanticSimilarity = a.SemanticSimilarity,
            AppliedAt = a.AppliedAt,
            SkillGaps = a.SkillGaps.Select(g => new SkillGapDto
            {
                SkillId = g.SkillId,
                SkillName = g.Skill.SkillName,
                Importance = g.Importance.ToString(),
                Suggestion = g.Suggestion
            }).ToList()
        };

        private readonly IrasDbContext _db;
        private readonly IScoringService _scoring;

        public ApplicationService(IrasDbContext db, IScoringService scoring)
        {
            _db = db;
            _scoring = scoring;
        }

        public async Task<ApplicationDto> ApplyAsync(int candidateId, ApplyForJobRequest request, CancellationToken ct)
        {
            var candidate = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Candidate profile not found.");

            var job = await _db.Jobs
                .Include(j => j.RequiredSkills).ThenInclude(rs => rs.Skill)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");
            if (job.Status != JobStatus.Published)
                throw new InvalidOperationException("This job is not open for applications.");

            var resume = await _db.Resumes
                .FirstOrDefaultAsync(r => r.ResumeId == request.ResumeId && r.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Resume not found.");
            if (resume.ParseStatus is not (ParseStatus.Parsed or ParseStatus.ManuallyEdited)
                || string.IsNullOrWhiteSpace(resume.ParsedText))
                throw new InvalidOperationException("Resume must be successfully parsed before it can be used in an application.");

            var alreadyApplied = await _db.Applications
                .AnyAsync(a => a.CandidateId == candidateId && a.JobId == request.JobId, ct);
            if (alreadyApplied)
                throw new InvalidOperationException("You have already applied to this job.");

            var candidateSkillIds = await _db.CandidateSkills
                .Where(cs => cs.CandidateId == candidateId)
                .Select(cs => cs.SkillId)
                .ToListAsync(ct);

            var skillMatch = _scoring.ComputeSkillMatch(job.RequiredSkills, candidateSkillIds);
            var experienceMatch = _scoring.ComputeExperienceMatch(candidate.TotalExpYears, job.MinExpYears);
            var educationMatch = _scoring.ComputeEducationMatch(candidate.EducationLevel, job.EducationReq);
            var semanticSimilarity = await _scoring.ComputeSemanticSimilarityAsync(candidateId, resume.ParsedText!, job, ct);

            var totalScore = _scoring.ComputeTotalScore(skillMatch, semanticSimilarity);

            var application = new AppEntity
            {
                CandidateId = candidateId,
                JobId = request.JobId,
                ResumeId = request.ResumeId,
                Status = ApplicationStatus.Applied,
                TotalScore = totalScore,
                SkillMatch = skillMatch,
                ExperienceMatch = experienceMatch,
                EducationMatch = educationMatch,
                SemanticSimilarity = semanticSimilarity
            };
            _db.Applications.Add(application);
            await _db.SaveChangesAsync(ct);

            var gaps = job.RequiredSkills
                .Where(rs => !candidateSkillIds.Contains(rs.SkillId))
                .Select(rs => new SkillGap
                {
                    ApplicationId = application.ApplicationId,
                    SkillId = rs.SkillId,
                    Importance = rs.Importance,
                    Suggestion = rs.Importance == ImportanceLevel.MustHave
                        ? $"This role requires {rs.Skill.SkillName}. Consider highlighting related experience or upskilling before interviewing."
                        : $"{rs.Skill.SkillName} is a nice-to-have for this role."
                })
                .ToList();
            if (gaps.Count > 0)
            {
                _db.SkillGaps.AddRange(gaps);
                await _db.SaveChangesAsync(ct);
            }

            return await _db.Applications.Where(a => a.ApplicationId == application.ApplicationId)
                .Select(ToApplicationDto).FirstAsync(ct);
        }

        public async Task<List<ApplicationDto>> GetMyApplicationsAsync(int candidateId, CancellationToken ct)
        {
            return await _db.Applications
                .Where(a => a.CandidateId == candidateId)
                .OrderByDescending(a => a.AppliedAt)
                .Select(ToApplicationDto)
                .ToListAsync(ct);
        }

        public async Task<List<RankedApplicantDto>> GetRankedApplicantsAsync(int employerId, int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");
            if (job.EmployerId != employerId)
                throw new KeyNotFoundException("Job not found.");

            return await _db.Applications
                .Where(a => a.JobId == jobId)
                .OrderByDescending(a => a.TotalScore)
                .Select(a => new RankedApplicantDto
                {
                    ApplicationId = a.ApplicationId,
                    CandidateId = a.CandidateId,
                    CandidateName = a.Candidate.FirstName + " " + a.Candidate.LastName,
                    Status = a.Status.ToString(),
                    TotalScore = a.TotalScore,
                    SkillMatch = a.SkillMatch,
                    ExperienceMatch = a.ExperienceMatch,
                    EducationMatch = a.EducationMatch,
                    SemanticSimilarity = a.SemanticSimilarity,
                    AppliedAt = a.AppliedAt,
                    SkillGaps = a.SkillGaps.Select(g => new SkillGapDto
                    {
                        SkillId = g.SkillId,
                        SkillName = g.Skill.SkillName,
                        Importance = g.Importance.ToString(),
                        Suggestion = g.Suggestion
                    }).ToList()
                })
                .ToListAsync(ct);
        }

    }
}
