using Microsoft.Extensions.Options;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Modules.Matching;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Employer;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Tests.Support;

namespace IRAS.Tests.Unit;

public class JobMatchingServiceTests
{
    [Fact]
    public async Task RunMatchingForJob_AddsMatchAndNotification_WhenThresholdPasses()
    {
        using var db = TestDb.Create();
        db.Users.AddRange(
            new User { UserId = 1, Email = "candidate@test.local", PasswordHash = "x", Role = UserRole.Candidate },
            new User { UserId = 2, Email = "employer@test.local", PasswordHash = "x", Role = UserRole.Employer });
        db.CandidateProfiles.Add(new CandidateProfile { CandidateId = 1, FirstName = "A", LastName = "B", OptInMatching = true, EducationLevel = EducationLevel.Bachelor });
        db.EmployerProfiles.Add(new EmployerProfile { EmployerId = 2, CompanyName = "Acme", CompanySize = CompanySize.Small });
        db.Resumes.Add(new Resume { ResumeId = 3, CandidateId = 1, FileUrl = "resume.pdf", FileFormat = ResumeFormat.PDF, IsPrimary = true, ParsedText = "C# API", ParseStatus = ParseStatus.Parsed });
        db.Skills.Add(new Skill { SkillId = 4, SkillName = "C#", Category = SkillCategory.ProgrammingLanguage });
        db.CandidateSkills.Add(new CandidateSkill { CandidateId = 1, SkillId = 4 });
        db.Jobs.Add(new Job
        {
            JobId = 5,
            EmployerId = 2,
            Title = "Backend Developer",
            SeniorityLevel = "Junior",
            EducationReq = EducationLevel.Bachelor,
            EmploymentType = EmploymentType.FullTime,
            Status = JobStatus.Published,
            RequiredSkills = { new JobRequiredSkill { JobId = 5, SkillId = 4, Importance = ImportanceLevel.MustHave, Weight = 1m } }
        });
        await db.SaveChangesAsync();

        var notifications = new NotificationService(db, new FakeEmailSender());
        var service = new JobMatchingService(
            db,
            new FakeScoringService(),
            notifications,
            Options.Create(new ScoringOptions { AutoMatchThreshold = 0.5m }));

        await service.RunMatchingForJobAsync(5, CancellationToken.None);

        Assert.Contains(db.JobMatches, m => m.JobId == 5 && m.CandidateId == 1 && m.ThresholdPassed);
        Assert.Contains(db.Notifications, n => n.UserId == 1 && n.Type == NotificationType.JobMatch);
    }
}
