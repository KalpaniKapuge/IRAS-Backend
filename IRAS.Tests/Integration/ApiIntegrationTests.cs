using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using IRAS.Domain.Entities.Applications;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Engagement;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;
using Xunit;
using AppEntity = IRAS.Domain.Entities.Applications.Application;

namespace IRAS.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<IrasApiFactory>
{
    private readonly IrasApiFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ApiIntegrationTests(IrasApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@iras.local",
            password = "ChangeMe@123"
        });

        await EnsureSuccessAsync(response);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Admin", doc.RootElement.GetProperty("role").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Employer_CanCreateJob()
    {
        var client = _factory.CreateClient();
        var employer = await RegisterAsync(client, "employer-create@test.local", "Employer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employer.Token);
        var skillId = await FirstSkillIdAsync();

        var response = await client.PostAsJsonAsync($"/api/employers/{employer.UserId}/jobs", new
        {
            title = "Backend Developer",
            seniorityLevel = "Junior",
            minExpYears = 1,
            educationReq = "Bachelor",
            employmentType = "FullTime",
            workArrangement = "Hybrid",
            location = "Colombo",
            requiredSkills = new[]
            {
                new { skillId, importance = "MustHave", weight = 1m, minYears = 1 }
            },
            requireAssessment = false
        });

        await EnsureSuccessAsync(response);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Backend Developer", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("Hybrid", doc.RootElement.GetProperty("workArrangement").GetString());
    }

    [Fact]
    public async Task Candidate_CanApplyAndReceiveSkillGap()
    {
        var client = _factory.CreateClient();
        var candidate = await RegisterAsync(client, "candidate-apply@test.local", "Candidate");
        var employer = await RegisterAsync(client, "employer-apply@test.local", "Employer");
        var seeded = await SeedApplicationScenarioAsync(candidate.UserId, employer.UserId, requireAssessment: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidate.Token);

        var response = await client.PostAsJsonAsync("/api/applications", new
        {
            jobId = seeded.JobId,
            resumeId = seeded.ResumeId
        });

        await EnsureSuccessAsync(response);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(seeded.JobId, doc.RootElement.GetProperty("jobId").GetInt32());
        Assert.True(doc.RootElement.GetProperty("skillGaps").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Candidate_CanStartAndSubmitAssessment()
    {
        var client = _factory.CreateClient();
        var candidate = await RegisterAsync(client, "candidate-assessment@test.local", "Candidate");
        var employer = await RegisterAsync(client, "employer-assessment@test.local", "Employer");
        var seeded = await SeedApplicationScenarioAsync(candidate.UserId, employer.UserId, requireAssessment: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidate.Token);

        var start = await client.PostAsync($"/api/jobs/{seeded.JobId}/assessment/start", null);
        await EnsureSuccessAsync(start);
        using var startDoc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var answers = startDoc.RootElement.GetProperty("questions")
            .EnumerateArray()
            .Select(q => new { questionId = q.GetProperty("questionId").GetInt32(), selectedOptionIndex = 1 })
            .ToList();

        var submit = await client.PostAsJsonAsync($"/api/jobs/{seeded.JobId}/assessment/submit", new { answers });

        await EnsureSuccessAsync(submit);
        using var resultDoc = JsonDocument.Parse(await submit.Content.ReadAsStringAsync());
        Assert.Equal(1m, resultDoc.RootElement.GetProperty("score").GetDecimal());
    }

    [Fact]
    public async Task Candidate_CanDeleteOwnNotification()
    {
        var client = _factory.CreateClient();
        var candidate = await RegisterAsync(client, "candidate-notify@test.local", "Candidate");
        int notificationId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IrasDbContext>();
            var notification = new Notification
            {
                UserId = candidate.UserId,
                Type = NotificationType.System,
                Title = "Test",
                Message = "Smoke notification",
                Channel = DeliveryChannel.InApp
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            notificationId = notification.NotificationId;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", candidate.Token);
        var response = await client.DeleteAsync($"/api/notifications/{notificationId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<AuthResult> RegisterAsync(HttpClient client, string email, string role)
    {
        var body = new Dictionary<string, object?>
        {
            ["email"] = email,
            ["password"] = "Password123!",
            ["role"] = role,
            ["firstName"] = role == "Employer" ? null : "Test",
            ["lastName"] = role == "Employer" ? null : "Candidate",
            ["companyName"] = role == "Employer" ? "Acme Tech" : null
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<AuthResult>(_json);
        return result!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private async Task<int> FirstSkillIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IrasDbContext>();
        return db.Skills.OrderBy(s => s.SkillId).Select(s => s.SkillId).First();
    }

    private async Task<(int JobId, int ResumeId)> SeedApplicationScenarioAsync(int candidateId, int employerId, bool requireAssessment)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IrasDbContext>();
        var skills = db.Skills.OrderBy(s => s.SkillId).Take(2).ToList();
        if (skills.Count < 2)
        {
            skills = new()
            {
                new Skill { SkillName = "C#", Category = SkillCategory.ProgrammingLanguage },
                new Skill { SkillName = "Docker", Category = SkillCategory.Tool }
            };
            db.Skills.AddRange(skills);
            await db.SaveChangesAsync();
        }

        var resume = new Resume
        {
            CandidateId = candidateId,
            FileUrl = "http://localhost/uploads/resume.pdf",
            FileFormat = ResumeFormat.PDF,
            IsPrimary = true,
            ParsedText = "C# backend API developer",
            ParseStatus = ParseStatus.Parsed
        };
        db.Resumes.Add(resume);
        db.CandidateSkills.Add(new CandidateSkill { CandidateId = candidateId, SkillId = skills[0].SkillId });

        var job = new Job
        {
            EmployerId = employerId,
            Title = "Backend Developer",
            SeniorityLevel = "Junior",
            EducationReq = EducationLevel.Bachelor,
            EmploymentType = EmploymentType.FullTime,
            WorkArrangement = WorkArrangement.Hybrid,
            Status = JobStatus.Published,
            TemplateKey = "modern",
            GeneratedJd = "Backend developer with C# and Docker skills",
            RequireAssessment = requireAssessment,
            RequiredSkills =
            {
                new JobRequiredSkill { SkillId = skills[0].SkillId, Importance = ImportanceLevel.MustHave, Weight = 1m },
                new JobRequiredSkill { SkillId = skills[1].SkillId, Importance = ImportanceLevel.MustHave, Weight = 1m }
            }
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        return (job.JobId, resume.ResumeId);
    }

    private sealed record AuthResult(int UserId, string Email, string Role, string Token, DateTime ExpiresAt);
}
