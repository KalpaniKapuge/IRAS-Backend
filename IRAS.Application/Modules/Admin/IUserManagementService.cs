// IRAS.Application/Modules/Admin/IUserManagementService.cs
using IRAS.Application.Modules.Admin.DTOs;

namespace IRAS.Application.Modules.Admin
{
    public interface IUserManagementService
    {
        Task<List<UserSummaryDto>> GetAllAsync(string? role, CancellationToken ct);
        Task<UserSummaryDto> GetByIdAsync(int userId, CancellationToken ct);

        // Deactivating blocks login (AuthService already checks User.IsActive) — this is
        // the account-suspension mechanism, not a delete.
        Task SetActiveAsync(int adminId, int userId, bool isActive, CancellationToken ct);
    }
}
