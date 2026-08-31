// IRAS.Application/Modules/Admin/IUserManagementService.cs
using IRAS.Application.Modules.Admin.DTOs;

namespace IRAS.Application.Modules.Admin
{
    public interface IUserManagementService
    {
        Task<List<UserSummaryDto>> GetAllAsync(string? role, CancellationToken ct);
        Task<UserSummaryDto> GetByIdAsync(int userId, CancellationToken ct);

        // Only route to provision an Admin account beyond the initial seeded bootstrap one —
        // self-registration as Admin is rejected (see RegisterRequest.Validate), so an
        // already-logged-in admin creating another is the real-world equivalent of that
        // bootstrap step, done safely and audited instead of via direct DB/config surgery.
        Task<UserSummaryDto> CreateAdminAsync(int adminId, CreateAdminUserRequest request, CancellationToken ct);

        // Deactivating blocks login (AuthService already checks User.IsActive) — this is
        // the account-suspension mechanism, not a delete.
        Task SetActiveAsync(int adminId, int userId, bool isActive, CancellationToken ct);
    }
}
