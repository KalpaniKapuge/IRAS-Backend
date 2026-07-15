// IRAS.Application/Modules/Auth/AuthService.cs
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

            if (role == UserRole.Candidate)
            {
                _db.CandidateProfiles.Add(new CandidateProfile
                {
                    CandidateId = user.UserId,
                    FirstName = request.FirstName!,
                    LastName = request.LastName!,
                    EducationLevel = EducationLevel.Bachelor,
                    TotalExpYears = 0
                });
            }
            else if (role == UserRole.Employer)
            {
                _db.EmployerProfiles.Add(new EmployerProfile
                {
                    EmployerId = user.UserId,
                    CompanyName = request.CompanyName!,
                    CompanySize = CompanySize.Small
                });
            }
            await _db.SaveChangesAsync();

            var response = BuildAuthResponse(user);
            await transaction.CommitAsync();
            return response;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("This account has been deactivated.");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return BuildAuthResponse(user);
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