// IRAS.Application/Modules/Auth/IAuthService.cs
using IRAS.Application.Modules.Auth.DTOs;

namespace IRAS.Application.Modules.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}