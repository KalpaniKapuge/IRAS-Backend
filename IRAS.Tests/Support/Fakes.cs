using IRAS.Application.Common.Ai;
using IRAS.Application.Common.Email;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Modules.Assessments;
using IRAS.Application.Modules.Feedback;
using IRAS.Application.Modules.SkillGaps;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Tests.Support;

internal sealed class FakeAiServiceClient : IAiServiceClient
{
    public Task<bool> CheckHealthAsync(CancellationToken ct) => Task.FromResult(true);

    public Task<ParseResumeResult> ParseResumeAsync(
        Stream file, string fileName, string fileFormat, IReadOnlyList<TaxonomyItem> taxonomy, CancellationToken ct) =>
        Task.FromResult(new ParseResumeResult(
            true, null, "Parsed resume text", new(), new() { "candidate@example.com" }, new(), 3));

    public Task<RankResult> RankAsync(
        string jobDescription, IReadOnlyList<RankCandidateInput> candidates, IReadOnlyList<TaxonomyItem> taxonomy, CancellationToken ct) =>
        Task.FromResult(new RankResult(
            true, null,
            candidates.Select(c => new RankedResult(c.CandidateId, 0.8m, "Good Fit", 0.9m)).ToList()));
}

internal sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

internal sealed class FakeScoringService : IScoringService
{
    public decimal ComputeSkillMatch(IEnumerable<JobRequiredSkill> requiredSkills, IReadOnlyCollection<int> candidateSkillIds) => 1m;
    public decimal ComputeExperienceMatch(decimal candidateExpYears, int jobMinExpYears) => 1m;
    public decimal ComputeEducationMatch(EducationLevel candidateLevel, EducationLevel requiredLevel) => 1m;
    public decimal ComputeTotalScore(decimal skillMatch, decimal semanticSimilarity, decimal? mlFitScore = null, decimal? assessmentScore = null) => 0.9m;
    public decimal ComputeTotalMarks(decimal skillMatch, decimal experienceMatch, decimal educationMatch, decimal semanticSimilarity, decimal? assessmentScore) => 0.9m;
    public Task<MatchSignals> ComputeMatchSignalAsync(int candidateId, string resumeText, Job job, CancellationToken ct) =>
        Task.FromResult(new MatchSignals(0.8m, 0.9m));
    public Task<Dictionary<int, MatchSignals>> ComputeMatchSignalsAsync(Job job, IReadOnlyList<(int CandidateId, string ResumeText)> candidates, CancellationToken ct) =>
        Task.FromResult(candidates.ToDictionary(c => c.CandidateId, _ => new MatchSignals(0.8m, 0.9m)));
}

internal sealed class FakeFeedbackService : IFeedbackService
{
    public Task GenerateDraftAsync(int applicationId, CancellationToken ct) => Task.CompletedTask;

    public Task<IRAS.Application.Modules.Feedback.DTOs.FeedbackDto?> GetMyFeedbackAsync(int candidateId, int applicationId, CancellationToken ct) =>
        Task.FromResult<IRAS.Application.Modules.Feedback.DTOs.FeedbackDto?>(null);

    public Task<IRAS.Application.Modules.Feedback.DTOs.FeedbackDto> GetForEmployerAsync(int employerId, int applicationId, CancellationToken ct) =>
        Task.FromResult(new IRAS.Application.Modules.Feedback.DTOs.FeedbackDto());

    public Task<IRAS.Application.Modules.Feedback.DTOs.FeedbackDto> ReviewAsync(
        int employerId, int applicationId, IRAS.Application.Modules.Feedback.DTOs.ReviewFeedbackRequest request, CancellationToken ct) =>
        Task.FromResult(new IRAS.Application.Modules.Feedback.DTOs.FeedbackDto());
}

internal sealed class FakeSkillGapExplainer : ISkillGapExplainer
{
    public string Name => "Fake";
    public bool IsAi => false;

    public Task<Dictionary<int, string>> ExplainAsync(
        string jobTitle, IEnumerable<(int SkillId, string SkillName, string Importance)> missingSkills, CancellationToken ct) =>
        Task.FromResult(missingSkills.ToDictionary(s => s.SkillId, s => $"Improve {s.SkillName} for {jobTitle}."));
}

internal sealed class FakeAssessmentService : IAssessmentService
{
    public decimal? Score { get; set; }
    public bool PassedGate { get; set; } = true;

    public Task<IRAS.Application.Modules.Assessments.DTOs.AssessmentStatusDto> GetStatusAsync(int candidateId, int jobId, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IRAS.Application.Modules.Assessments.DTOs.StartAssessmentResponse> StartAsync(int candidateId, int jobId, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IRAS.Application.Modules.Assessments.DTOs.AssessmentResultDto> SubmitAsync(int candidateId, int jobId, IRAS.Application.Modules.Assessments.DTOs.SubmitAssessmentRequest request, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<bool> HasPassedGateAsync(int candidateId, int jobId, CancellationToken ct) => Task.FromResult(PassedGate);
    public Task<decimal?> GetScoreAsync(int candidateId, int jobId, CancellationToken ct) => Task.FromResult(Score);

    public Task<IRAS.Application.Modules.Assessments.DTOs.EmployerAssessmentReviewDto?> GetReviewForEmployerAsync(int employerId, int applicationId, CancellationToken ct) =>
        Task.FromResult<IRAS.Application.Modules.Assessments.DTOs.EmployerAssessmentReviewDto?>(null);
}

internal sealed class ThrowingQuestionGenerator : IAssessmentQuestionGenerator
{
    public string Name => "Throwing";

    public Task<List<GeneratedQuestion>> GenerateAsync(
        Job job, IEnumerable<(string SkillName, string Importance, SkillCategory Category)> skills, int questionCount, CancellationToken ct) =>
        throw new TimeoutException("AI unavailable");
}

internal sealed class ExactTextAnswerGrader : IAssessmentAnswerGrader
{
    public string Name => "Exact";
    public Task<decimal> GradeAsync(string questionText, string modelAnswer, string candidateAnswer, CancellationToken ct) =>
        Task.FromResult(string.Equals(modelAnswer, candidateAnswer, StringComparison.OrdinalIgnoreCase) ? 1m : 0m);
}
