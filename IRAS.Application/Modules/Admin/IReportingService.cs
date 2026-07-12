// IRAS.Application/Modules/Admin/IReportingService.cs
using IRAS.Application.Modules.Admin.DTOs;

namespace IRAS.Application.Modules.Admin
{
    public interface IReportingService
    {
        Task<DashboardStatsDto> GetDashboardAsync(CancellationToken ct);
    }
}
