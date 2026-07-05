// IRAS.Application/Modules/Auth/DTOs/LoginRequest.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Auth.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = null!;
    }
}
