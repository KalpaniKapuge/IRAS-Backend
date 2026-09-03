using Microsoft.Extensions.Configuration;
using IRAS.Application.Modules.Auth;
using IRAS.Application.Modules.Auth.DTOs;
using IRAS.Domain.Enums;
using IRAS.Tests.Support;

namespace IRAS.Tests.Unit;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesCandidateProfile()
    {
        using var db = TestDb.Create();
        var service = new AuthService(db, Config());

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "candidate@test.local",
            Password = "Password123!",
            Role = "Candidate",
            FirstName = "Test",
            LastName = "Candidate"
        });

        Assert.Equal("Candidate", result.Role);
        Assert.Contains(db.CandidateProfiles, p => p.CandidateId == result.UserId);
    }

    [Fact]
    public async Task LoginAsync_RejectsWrongPassword()
    {
        using var db = TestDb.Create();
        var service = new AuthService(db, Config());
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "candidate@test.local",
            Password = "Password123!",
            Role = "Candidate",
            FirstName = "Test",
            LastName = "Candidate"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest { Email = "candidate@test.local", Password = "wrong" }));
    }

    [Fact]
    public async Task RegisterAsync_RejectsAdminSelfRegistration()
    {
        using var db = TestDb.Create();
        var service = new AuthService(db, Config());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterRequest
            {
                Email = "admin@test.local",
                Password = "Password123!",
                Role = UserRole.Admin.ToString()
            }));
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "0123456789abcdef0123456789abcdef0123456789abcdef",
                ["Jwt:Issuer"] = "IRAS.Tests",
                ["Jwt:Audience"] = "IRAS.Tests",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
}
