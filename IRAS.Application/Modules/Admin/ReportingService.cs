// IRAS.Application/Modules/Admin/ReportingService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.Admin.DTOs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Admin
{
    // Read-only aggregate metrics for the Admin dashboard — also doubles as the raw
    // numbers behind the thesis evaluation chapter (parsing success rate, average
    // matching relevance, skill-gap frequency) rather than something that needs
    // separately instrumenting.
    public class ReportingService : IReportingService
    {
        private readonly IrasDbContext _db;
        public ReportingService(IrasDbContext db) => _db = db;

        public async Task<DashboardStatsDto> GetDashboardAsync(CancellationToken ct)
        {
            var applicationsByStatus = await _db.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var topGaps = await _db.SkillGaps
                .GroupBy(g => g.Skill.SkillName)
                .Select(g => new TopSkillGapDto { SkillName = g.Key, Occurrences = g.Count() })
                .OrderByDescending(g => g.Occurrences)
                .Take(10)
                .ToListAsync(ct);

            var hasApplications = await _db.Applications.AnyAsync(ct);

            return new DashboardStatsDto
            {
                TotalCandidates = await _db.CandidateProfiles.CountAsync(ct),
                TotalEmployers = await _db.EmployerProfiles.CountAsync(ct),
                TotalJobs = await _db.Jobs.CountAsync(ct),
                PublishedJobs = await _db.Jobs.CountAsync(j => j.Status == JobStatus.Published, ct),
                TotalApplications = await _db.Applications.CountAsync(ct),
                ApplicationsByStatus = applicationsByStatus.ToDictionary(x => x.Status.ToString(), x => x.Count),
                TotalResumes = await _db.Resumes.CountAsync(ct),
                ParsedResumes = await _db.Resumes.CountAsync(
                    r => r.ParseStatus == ParseStatus.Parsed || r.ParseStatus == ParseStatus.ManuallyEdited, ct),
                FailedResumes = await _db.Resumes.CountAsync(r => r.ParseStatus == ParseStatus.Failed, ct),
                AverageApplicationScore = hasApplications
                    ? Math.Round(await _db.Applications.AverageAsync(a => a.TotalScore, ct), 4)
                    : 0m,
                TotalSkillGapsDetected = await _db.SkillGaps.CountAsync(ct),
                TopMissingSkills = topGaps,
                TotalJobMatches = await _db.JobMatches.CountAsync(m => m.ThresholdPassed, ct),
                PendingFeedbackReviews = await _db.Feedbacks.CountAsync(f => f.ApprovalStatus == ApprovalStatus.PendingReview, ct)
            };
        }
    }
}
