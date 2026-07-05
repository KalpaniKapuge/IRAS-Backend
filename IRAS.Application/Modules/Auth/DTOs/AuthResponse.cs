// IRAS.Application/Modules/Auth/DTOs/AuthResponse.cs
namespace IRAS.Application.Modules.Auth.DTOs
{
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}