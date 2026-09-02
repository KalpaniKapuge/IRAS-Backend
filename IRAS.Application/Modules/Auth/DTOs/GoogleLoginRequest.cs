using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Auth.DTOs
{
    public class GoogleLoginRequest
    {
        // The ID token (JWT) issued by Google Identity Services on the client — the value of
        // the `credential` field in the GIS callback. Verified server-side against Google's
        // public keys and our configured OAuth client ID.
        [Required(ErrorMessage = "Google credential is required.")]
        public string IdToken { get; set; } = null!;

        // Only used the first time a given Google account signs in, to decide which kind of
        // profile to create. Ignored for accounts that already exist. "Candidate" or
        // "Employer"; defaults to Candidate when omitted (the login page doesn't ask).
        public string? Role { get; set; }
    }
}
