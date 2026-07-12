// IRAS.Application/Modules/Admin/ISystemStatusService.cs
using IRAS.Application.Modules.Admin.DTOs;

namespace IRAS.Application.Modules.Admin
{
    public interface ISystemStatusService
    {
        Task<AiModelStatusDto> GetAiModelStatusAsync(CancellationToken ct);
        SystemSettingsDto GetSystemSettings();
    }
}
