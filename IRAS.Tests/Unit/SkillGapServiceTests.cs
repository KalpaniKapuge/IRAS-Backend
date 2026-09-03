using IRAS.Application.Modules.SkillGaps;
using IRAS.Domain.Entities.Applications;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Employer;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Tests.Support;
using AppEntity = IRAS.Domain.Entities.Applications.Application;

namespace IRAS.Tests.Unit;

public class SkillGapServiceTests
{
    [Fact]
    public async Task GetMyGapSummary_GroupsAndPrioritizesMissingSkills()
    {
        using var db = TestDb.Create();
        db.Users.AddRange(
            new User { UserId = 1, Email = "candidate@test.local", PasswordHash = "x", Role = UserRole.Candidate },
            new User { UserId = 2, Email = "employer@test.local", PasswordHash = "x", Role = UserRole.Employer });
        db.CandidateProfiles.Add(new CandidateProfile { CandidateId = 1, FirstName = "A", LastName = "B", EducationLevel = EducationLevel.Bachelor });
        db.EmployerProfiles.Add(new EmployerProfile { EmployerId = 2, CompanyName = "Acme", CompanySize = CompanySize.Small });
        db.Skills.AddRange(
            new Skill { SkillId = 10, SkillName = "Docker", Category = SkillCategory.Tool },
            new Skill { SkillId = 11, SkillName = "Azure", Category = SkillCategory.CloudPlatform });
        db.Jobs.Add(new Job { JobId = 20, EmployerId = 2, Title = "Backend Developer", SeniorityLevel = "Junior", EducationReq = EducationLevel.Bachelor, EmploymentType = EmploymentType.FullTime });
        db.Applications.AddRange(
            new AppEntity { ApplicationId = 30, CandidateId = 1, JobId = 20, ResumeId = 1 },
            new AppEntity { ApplicationId = 31, CandidateId = 1, JobId = 20, ResumeId = 1 });
        db.SkillGaps.AddRange(
            new SkillGap { ApplicationId = 30, SkillId = 10, Importance = ImportanceLevel.MustHave },
            new SkillGap { ApplicationId = 31, SkillId = 10, Importance = ImportanceLevel.NiceToHave },
            new SkillGap { ApplicationId = 31, SkillId = 11, Importance = ImportanceLevel.NiceToHave });
        await db.SaveChangesAsync();

        var result = await new SkillGapService(db).GetMyGapSummaryAsync(1, CancellationToken.None);

        Assert.Equal("Docker", result[0].SkillName);
        Assert.Equal(1, result[0].MustHaveCount);
        Assert.Equal(2, result[0].TotalOccurrences);
    }
}
