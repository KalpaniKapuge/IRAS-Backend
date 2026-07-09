// IRAS.Application/Modules/SkillGaps/SkillGapService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.SkillGaps.DTOs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.SkillGaps
{
    public class SkillGapService : ISkillGapService
    {
        private readonly IrasDbContext _db;
        public SkillGapService(IrasDbContext db) => _db = db;

        public async Task<List<CandidateSkillGapDto>> GetMyGapsAsync(int candidateId, CancellationToken ct)
        {
            return await _db.SkillGaps
                .Where(g => g.Application.CandidateId == candidateId)
                .OrderByDescending(g => g.DetectedAt)
                .Select(g => new CandidateSkillGapDto
                {
                    SkillId = g.SkillId,
                    SkillName = g.Skill.SkillName,
                    Importance = g.Importance.ToString(),
                    Suggestion = g.Suggestion,
                    JobId = g.Application.JobId,
                    JobTitle = g.Application.Job.Title,
                    CompanyName = g.Application.Job.Employer.CompanyName,
                    DetectedAt = g.DetectedAt
                })
                .ToListAsync(ct);
        }

        public async Task<List<SkillGapSummaryDto>> GetMyGapSummaryAsync(int candidateId, CancellationToken ct)
        {
            var gaps = await _db.SkillGaps
                .Where(g => g.Application.CandidateId == candidateId)
                .Select(g => new { g.SkillId, g.Skill.SkillName, g.Importance })
                .ToListAsync(ct);

            return gaps
                .GroupBy(g => new { g.SkillId, g.SkillName })
                .Select(group => new SkillGapSummaryDto
                {
                    SkillId = group.Key.SkillId,
                    SkillName = group.Key.SkillName,
                    MustHaveCount = group.Count(g => g.Importance == ImportanceLevel.MustHave),
                    NiceToHaveCount = group.Count(g => g.Importance == ImportanceLevel.NiceToHave),
                    TotalOccurrences = group.Count()
                })
                .OrderByDescending(s => s.MustHaveCount)
                .ThenByDescending(s => s.TotalOccurrences)
                .ToList();
        }
    }
}
