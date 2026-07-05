// IRAS.Application/Modules/Auth/DTOs/LoginRequest.cs
namespace IRAS.Application.Modules.Auth.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}