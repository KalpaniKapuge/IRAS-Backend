// IRAS.Application/Modules/Admin/UserManagementService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Audit;
using IRAS.Application.Modules.Admin.DTOs;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Admin
{
    public class UserManagementService : IUserManagementService
    {
        private const string EntityType = "User";

        private readonly IrasDbContext _db;
        private readonly IAuditLogService _audit;

        public UserManagementService(IrasDbContext db, IAuditLogService audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<List<UserSummaryDto>> GetAllAsync(string? role, CancellationToken ct)
        {
            var q = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(role))
            {
                var parsedRole = ParseEnum<UserRole>(role, nameof(role));
                q = q.Where(u => u.Role == parsedRole);
            }

            return await q.OrderBy(u => u.UserId).Select(u => ToDto(u)).ToListAsync(ct);
        }

        public async Task<UserSummaryDto> GetByIdAsync(int userId, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct)
                ?? throw new KeyNotFoundException("User not found.");
            return ToDto(user);
        }

        public async Task SetActiveAsync(int adminId, int userId, bool isActive, CancellationToken ct)
        {
            if (userId == adminId)
                throw new InvalidOperationException("You cannot change your own account's active status.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct)
                ?? throw new KeyNotFoundException("User not found.");

            if (!isActive && user.Role == UserRole.Admin)
            {
                var otherActiveAdmins = await _db.Users
                    .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.UserId != userId, ct);
                if (otherActiveAdmins == 0)
                    throw new InvalidOperationException("Cannot deactivate the last active admin account.");
            }

            user.IsActive = isActive;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(adminId, isActive ? "UserActivated" : "UserDeactivated", EntityType, userId, ct);
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }

        private static UserSummaryDto ToDto(User u) => new()
        {
            UserId = u.UserId,
            Email = u.Email,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            LastLogin = u.LastLogin,
            CreatedAt = u.CreatedAt
        };
    }
}
