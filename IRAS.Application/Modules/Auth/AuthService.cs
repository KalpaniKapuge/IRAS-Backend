// IRAS.Application/Modules/Auth/AuthService.cs
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IRAS.Application.Modules.Auth.DTOs;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Employer;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IrasDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(IrasDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // RegisterRequest.Validate() (IValidatableObject) already rejects anything that
            // doesn't parse to a defined UserRole before this service method is ever reached,
            // so this parse cannot fail in practice — it just recovers the typed value.
            var role = ParseEnum<UserRole>(request.Role, nameof(request.Role));

            if (role == UserRole.Admin)
                throw new InvalidOperationException("Admin accounts cannot be self-registered.");

            var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                throw new InvalidOperationException("An account with this email already exists.");

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role,
                IsActive = true
            };

            // Wrap the writes and token issuance in one transaction so a failure building the
            // token (e.g. bad JWT config) can't leave behind a user with no way to log in via this response.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            _db.Users.Add(user);
            await _db.SaveChangesAsync();   // save first to get UserId

            AddProfileForRole(user, role, request.FirstName, request.LastName, request.CompanyName);
            await _db.SaveChangesAsync();

            var response = BuildAuthResponse(user);
            await transaction.CommitAsync();
            return response;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user is { PasswordHash: null })
                throw new UnauthorizedAccessException(
                    "This account was created with Google. Use \"Continue with Google\" to sign in.");

            if (user == null || user.PasswordHash == null
                || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("This account has been deactivated.");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var clientId = _config["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException(
                    "Google sign-in is not configured on the server (Authentication:Google:ClientId).");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // InvalidJwtException for a bad signature / wrong audience / expired token, but
                // a malformed token string surfaces as JsonReaderException / FormatException /
                // ArgumentException from the parser — all mean "this token isn't usable".
                throw new UnauthorizedAccessException("Google sign-in could not be verified. Please try again.");
            }

            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
                throw new UnauthorizedAccessException("Your Google account has no verified email address.");

            var email = payload.Email.Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                if (!user.IsActive)
                    throw new UnauthorizedAccessException("This account has been deactivated.");

                user.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return BuildAuthResponse(user);
            }

            // First sign-in for this Google account — create it. The register page passes the
            // chosen role; the login page doesn't, so default to Candidate.
            var role = string.IsNullOrWhiteSpace(request.Role)
                ? UserRole.Candidate
                : ParseEnum<UserRole>(request.Role, nameof(request.Role));
            if (role == UserRole.Admin)
                throw new InvalidOperationException("Admin accounts cannot be self-registered.");

            var firstName = string.IsNullOrWhiteSpace(payload.GivenName) ? "New" : payload.GivenName.Trim();
            var lastName = string.IsNullOrWhiteSpace(payload.FamilyName) ? "User" : payload.FamilyName.Trim();
            var companyName = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name.Trim();

            var newUser = new User
            {
                Email = email,
                PasswordHash = null,
                AuthProvider = "Google",
                Role = role,
                IsActive = true,
                LastLogin = DateTime.UtcNow
            };

            await using var transaction = await _db.Database.BeginTransactionAsync();

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            AddProfileForRole(newUser, role, firstName, lastName, companyName);
            await _db.SaveChangesAsync();

            var response = BuildAuthResponse(newUser);
            await transaction.CommitAsync();
            return response;
        }

        private void AddProfileForRole(User user, UserRole role, string? firstName, string? lastName, string? companyName)
        {
            if (role == UserRole.Candidate)
            {
                _db.CandidateProfiles.Add(new CandidateProfile
                {
                    CandidateId = user.UserId,
                    FirstName = firstName!,
                    LastName = lastName!,
                    EducationLevel = EducationLevel.Bachelor,
                    TotalExpYears = 0
                });
            }
            else if (role == UserRole.Employer)
            {
                _db.EmployerProfiles.Add(new EmployerProfile
                {
                    EmployerId = user.UserId,
                    CompanyName = companyName!,
                    CompanySize = CompanySize.Small
                });
            }
        }

        private AuthResponse BuildAuthResponse(User user)
        {
            var jwtKey = _config["Jwt:Key"]!;
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "120");
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(expiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expires
            };
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }
    }
}