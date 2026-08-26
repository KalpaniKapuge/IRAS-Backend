// IRAS.Application/Modules/SkillDevelopment/SkillDevelopmentService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Audit;
using IRAS.Application.Modules.SkillDevelopment.DTOs;
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Skills;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.SkillDevelopment
{
    public class SkillDevelopmentService : ISkillDevelopmentService
    {
        private const string ResourceEntityType = "SkillResource";

        private readonly IrasDbContext _db;
        private readonly IAuditLogService _audit;

        public SkillDevelopmentService(IrasDbContext db, IAuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<SkillResourceDto>> GetAllResourcesAsync(CancellationToken ct)
        {
            return await _db.SkillResources
                .OrderBy(r => r.Skill.SkillName).ThenBy(r => r.Title)
                .Select(r => new SkillResourceDto
                {
                    ResourceId = r.ResourceId,
                    SkillId = r.SkillId,
                    SkillName = r.Skill.SkillName,
                    Title = r.Title,
                    Url = r.Url,
                    ResourceType = r.ResourceType.ToString(),
                    Provider = r.Provider,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(ct);
        }

        public async Task<SkillResourceDto> CreateResourceAsync(int adminId, UpsertSkillResourceRequest request, CancellationToken ct)
        {
            var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillId == request.SkillId, ct)
                ?? throw new KeyNotFoundException("Skill not found.");

            var resource = new SkillResource
            {
                SkillId = request.SkillId,
                Title = request.Title.Trim(),
                Url = request.Url.Trim(),
                ResourceType = ParseEnum<SkillResourceType>(request.ResourceType, nameof(request.ResourceType)),
                Provider = string.IsNullOrWhiteSpace(request.Provider) ? null : request.Provider.Trim(),
                IsActive = request.IsActive,
                CreatedBy = adminId
            };
            _db.SkillResources.Add(resource);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "SkillResourceCreated", ResourceEntityType, resource.ResourceId, ct);

            return new SkillResourceDto
            {
                ResourceId = resource.ResourceId,
                SkillId = resource.SkillId,
                SkillName = skill.SkillName,
                Title = resource.Title,
                Url = resource.Url,
                ResourceType = resource.ResourceType.ToString(),
                Provider = resource.Provider,
                IsActive = resource.IsActive,
                CreatedAt = resource.CreatedAt
            };
        }

        public async Task UpdateResourceAsync(int adminId, int resourceId, UpsertSkillResourceRequest request, CancellationToken ct)
        {
            var resource = await _db.SkillResources.FirstOrDefaultAsync(r => r.ResourceId == resourceId, ct)
                ?? throw new KeyNotFoundException("Skill resource not found.");

            var skillExists = await _db.Skills.AnyAsync(s => s.SkillId == request.SkillId, ct);
            if (!skillExists) throw new KeyNotFoundException("Skill not found.");

            resource.SkillId = request.SkillId;
            resource.Title = request.Title.Trim();
            resource.Url = request.Url.Trim();
            resource.ResourceType = ParseEnum<SkillResourceType>(request.ResourceType, nameof(request.ResourceType));
            resource.Provider = string.IsNullOrWhiteSpace(request.Provider) ? null : request.Provider.Trim();
            resource.IsActive = request.IsActive;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "SkillResourceUpdated", ResourceEntityType, resourceId, ct);
        }

        public async Task DeleteResourceAsync(int adminId, int resourceId, CancellationToken ct)
        {
            var resource = await _db.SkillResources.FirstOrDefaultAsync(r => r.ResourceId == resourceId, ct)
                ?? throw new KeyNotFoundException("Skill resource not found.");
            _db.SkillResources.Remove(resource);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "SkillResourceDeleted", ResourceEntityType, resourceId, ct);
        }

        public async Task<List<TargetSkillDto>> GetMyTargetSkillsAsync(int candidateId, CancellationToken ct)
        {
            return await _db.CandidateTargetSkills
                .Where(t => t.CandidateId == candidateId)
                .OrderByDescending(t => t.AddedAt)
                .Select(t => new TargetSkillDto
                {
                    SkillId = t.SkillId,
                    SkillName = t.Skill.SkillName,
                    Status = t.Status.ToString(),
                    AddedAt = t.AddedAt,
                    CompletedAt = t.CompletedAt
                })
                .ToListAsync(ct);
        }

        public async Task<TargetSkillDto> AddTargetSkillAsync(int candidateId, AddTargetSkillRequest request, CancellationToken ct)
        {
            var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillId == request.SkillId, ct)
                ?? throw new KeyNotFoundException("Skill not found.");

            var target = await _db.CandidateTargetSkills
                .FirstOrDefaultAsync(t => t.CandidateId == candidateId && t.SkillId == request.SkillId, ct);

            if (target == null)
            {
                target = new CandidateTargetSkill { CandidateId = candidateId, SkillId = request.SkillId };
                _db.CandidateTargetSkills.Add(target);
                await _db.SaveChangesAsync(ct);
            }

            return new TargetSkillDto
            {
                SkillId = target.SkillId,
                SkillName = skill.SkillName,
                Status = target.Status.ToString(),
                AddedAt = target.AddedAt,
                CompletedAt = target.CompletedAt
            };
        }

        public async Task MarkTargetSkillCompletedAsync(int candidateId, int skillId, CancellationToken ct)
        {
            var target = await _db.CandidateTargetSkills
                .FirstOrDefaultAsync(t => t.CandidateId == candidateId && t.SkillId == skillId, ct)
                ?? throw new KeyNotFoundException("Target skill not found.");

            target.Status = TargetSkillStatus.Completed;
            target.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task RemoveTargetSkillAsync(int candidateId, int skillId, CancellationToken ct)
        {
            var target = await _db.CandidateTargetSkills
                .FirstOrDefaultAsync(t => t.CandidateId == candidateId && t.SkillId == skillId, ct)
                ?? throw new KeyNotFoundException("Target skill not found.");
            _db.CandidateTargetSkills.Remove(target);
            await _db.SaveChangesAsync(ct);
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
