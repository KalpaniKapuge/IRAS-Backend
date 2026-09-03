using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Scoring;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Tests.Support;

namespace IRAS.Tests.Unit;

public class ScoringServiceTests
{
    [Fact]
    public void ComputeSkillMatch_UsesRequiredSkillWeights()
    {
        using var db = TestDb.Create();
        var service = new ScoringService(
            db,
            new FakeAiServiceClient(),
            Options.Create(new ScoringOptions()),
            NullLogger<ScoringService>.Instance);

        var required = new[]
        {
            new JobRequiredSkill { SkillId = 1, Weight = 1m },
            new JobRequiredSkill { SkillId = 2, Weight = 0.5m },
            new JobRequiredSkill { SkillId = 3, Weight = 0.5m },
        };

        var result = service.ComputeSkillMatch(required, new[] { 1, 3 });

        Assert.Equal(0.75m, result);
    }

    [Fact]
    public void ComputeTotalScore_IncludesMlAndAssessmentWeights()
    {
        using var db = TestDb.Create();
        var options = Options.Create(new ScoringOptions
        {
            SkillMatchWeight = 0.4m,
            SemanticSimilarityWeight = 0.2m,
            MlFitScoreWeight = 0.2m,
            AssessmentScoreWeight = 0.2m
        });
        var service = new ScoringService(db, new FakeAiServiceClient(), options, NullLogger<ScoringService>.Instance);

        var result = service.ComputeTotalScore(0.5m, 0.6m, 0.7m, 0.8m);

        Assert.Equal(0.62m, result);
    }

    [Fact]
    public void ComputeEducationMatch_GivesFullCreditForHigherLevel()
    {
        using var db = TestDb.Create();
        var service = new ScoringService(db, new FakeAiServiceClient(), Options.Create(new ScoringOptions()), NullLogger<ScoringService>.Instance);

        var result = service.ComputeEducationMatch(EducationLevel.Master, EducationLevel.Bachelor);

        Assert.Equal(1m, result);
    }
}
