// IRAS.Application/Common/Audit/AuditLogService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using IRAS.Domain.Entities.Admin;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Common.Audit
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IrasDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(IrasDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(int userId, string action, string entityType, int entityId, CancellationToken ct)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<AuditLogDto>> GetRecentAsync(int take, CancellationToken ct)
        {
            take = Math.Clamp(take, 1, 500);

            return await _db.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .Select(a => new AuditLogDto
                {
                    LogId = a.LogId,
                    UserId = a.UserId,
                    UserEmail = a.User.Email,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    IpAddress = a.IpAddress,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(ct);
        }
    }
}
