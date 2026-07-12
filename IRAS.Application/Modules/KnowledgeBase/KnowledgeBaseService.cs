// IRAS.Application/Modules/KnowledgeBase/KnowledgeBaseService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Audit;
using IRAS.Application.Modules.KnowledgeBase.DTOs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

// "KnowledgeBase" (the entity) and "IRAS.Application.Modules.KnowledgeBase" (this
// module's own namespace) share a name — same clash as AppEntity/FeedbackEntity
// elsewhere in this codebase. Alias it.
using KnowledgeBaseEntity = IRAS.Domain.Entities.Admin.KnowledgeBase;

namespace IRAS.Application.Modules.KnowledgeBase
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private const string EntityType = "KnowledgeBase";

        private readonly IrasDbContext _db;
        private readonly IAuditLogService _audit;

        public KnowledgeBaseService(IrasDbContext db, IAuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<KnowledgeBaseDto>> GetAllAsync(CancellationToken ct)
        {
            return await _db.KnowledgeBases
                .OrderBy(k => k.Category).ThenBy(k => k.Title)
                .Select(k => ToDto(k))
                .ToListAsync(ct);
        }

        public async Task<KnowledgeBaseDto> GetByIdAsync(int kbId, CancellationToken ct)
        {
            var entry = await _db.KnowledgeBases.FirstOrDefaultAsync(k => k.KbId == kbId, ct)
                ?? throw new KeyNotFoundException("Knowledge base entry not found.");
            return ToDto(entry);
        }

        public async Task<KnowledgeBaseDto> CreateAsync(int adminId, UpsertKnowledgeBaseRequest request, CancellationToken ct)
        {
            var entry = new KnowledgeBaseEntity
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                Category = ParseEnum<KnowledgeCategory>(request.Category, nameof(request.Category)),
                IsActive = request.IsActive,
                UpdatedBy = adminId
            };
            _db.KnowledgeBases.Add(entry);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "KnowledgeBaseCreated", EntityType, entry.KbId, ct);
            return ToDto(entry);
        }

        public async Task UpdateAsync(int adminId, int kbId, UpsertKnowledgeBaseRequest request, CancellationToken ct)
        {
            var entry = await _db.KnowledgeBases.FirstOrDefaultAsync(k => k.KbId == kbId, ct)
                ?? throw new KeyNotFoundException("Knowledge base entry not found.");

            entry.Title = request.Title.Trim();
            entry.Content = request.Content.Trim();
            entry.Category = ParseEnum<KnowledgeCategory>(request.Category, nameof(request.Category));
            entry.IsActive = request.IsActive;
            entry.UpdatedBy = adminId;
            entry.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "KnowledgeBaseUpdated", EntityType, kbId, ct);
        }

        public async Task DeleteAsync(int adminId, int kbId, CancellationToken ct)
        {
            var entry = await _db.KnowledgeBases.FirstOrDefaultAsync(k => k.KbId == kbId, ct)
                ?? throw new KeyNotFoundException("Knowledge base entry not found.");
            _db.KnowledgeBases.Remove(entry);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, "KnowledgeBaseDeleted", EntityType, kbId, ct);
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }

        private static KnowledgeBaseDto ToDto(KnowledgeBaseEntity k) => new()
        {
            KbId = k.KbId,
            Title = k.Title,
            Content = k.Content,
            Category = k.Category.ToString(),
            IsActive = k.IsActive,
            UpdatedAt = k.UpdatedAt
        };
    }
}
