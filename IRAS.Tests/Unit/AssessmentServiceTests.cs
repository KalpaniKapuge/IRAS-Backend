using Microsoft.Extensions.Logging.Abstractions;
using IRAS.Application.Modules.Assessments;
using IRAS.Application.Modules.Assessments.DTOs;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Employer;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Tests.Support;

namespace IRAS.Tests.Unit;

public class AssessmentServiceTests
{
    [Fact]
    public async Task StartAsync_FallsBackToTemplateQuestions_WhenPrimaryGeneratorFails()
    {
        using var db = TestDb.Create();
        db.Users.AddRange(
            new User { UserId = 1, Email = "candidate@test.local", PasswordHash = "x", Role = UserRole.Candidate },
            new User { UserId = 2, Email = "employer@test.local", PasswordHash = "x", Role = UserRole.Employer });
        db.CandidateProfiles.Add(new CandidateProfile { CandidateId = 1, FirstName = "A", LastName = "B", EducationLevel = EducationLevel.Bachelor });
        db.EmployerProfiles.Add(new EmployerProfile { EmployerId = 2, CompanyName = "Acme", CompanySize = CompanySize.Small });
        db.Skills.Add(new Skill { SkillId = 4, SkillName = "C#", Category = SkillCategory.ProgrammingLanguage });
        db.Jobs.Add(new Job
        {
            JobId = 5,
            EmployerId = 2,
            Title = "Backend Developer",
            SeniorityLevel = "Junior",
            EducationReq = EducationLevel.Bachelor,
            EmploymentType = EmploymentType.FullTime,
            Status = JobStatus.Published,
            RequireAssessment = true,
            RequiredSkills = { new JobRequiredSkill { JobId = 5, SkillId = 4, Importance = ImportanceLevel.MustHave, Weight = 1m } }
        });
        await db.SaveChangesAsync();

        var service = new AssessmentService(
            db,
            new ThrowingQuestionGenerator(),
            new TemplateAssessmentQuestionGenerator(),
            new ExactTextAnswerGrader(),
            NullLogger<AssessmentService>.Instance);

        var started = await service.StartAsync(1, 5, CancellationToken.None);

        Assert.NotEmpty(started.Questions);
        Assert.Contains(db.JobAssessments, a => a.JobId == 5 && a.GeneratedBy == "Template");
    }

    [Fact]
    public async Task SubmitAsync_ScoresMultipleChoiceAnswers()
    {
        using var db = TestDb.Create();
        db.Users.AddRange(
            new User { UserId = 1, Email = "candidate@test.local", PasswordHash = "x", Role = UserRole.Candidate },
            new User { UserId = 2, Email = "employer@test.local", PasswordHash = "x", Role = UserRole.Employer });
        db.CandidateProfiles.Add(new CandidateProfile { CandidateId = 1, FirstName = "A", LastName = "B", EducationLevel = EducationLevel.Bachelor });
        db.EmployerProfiles.Add(new EmployerProfile { EmployerId = 2, CompanyName = "Acme", CompanySize = CompanySize.Small });
        db.Skills.Add(new Skill { SkillId = 4, SkillName = "C#", Category = SkillCategory.ProgrammingLanguage });
        db.Jobs.Add(new Job { JobId = 5, EmployerId = 2, Title = "Backend Developer", SeniorityLevel = "Junior", EducationReq = EducationLevel.Bachelor, EmploymentType = EmploymentType.FullTime, RequireAssessment = true });
        await db.SaveChangesAsync();

        var service = new AssessmentService(
            db,
            new TemplateAssessmentQuestionGenerator(),
            new TemplateAssessmentQuestionGenerator(),
            new ExactTextAnswerGrader(),
            NullLogger<AssessmentService>.Instance);
        var started = await service.StartAsync(1, 5, CancellationToken.None);

        var result = await service.SubmitAsync(1, 5, new SubmitAssessmentRequest
        {
            Answers = started.Questions.Select(q => new SubmitAssessmentAnswer
            {
                QuestionId = q.QuestionId,
                SelectedOptionIndex = 1
            }).ToList()
        }, CancellationToken.None);

        Assert.Equal(1m, result.Score);
        Assert.True(await service.HasPassedGateAsync(1, 5, CancellationToken.None));
    }
}
