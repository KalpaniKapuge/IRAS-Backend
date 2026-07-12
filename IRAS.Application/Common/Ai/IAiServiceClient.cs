// IRAS.Application/Common/Ai/IAiServiceClient.cs
namespace IRAS.Application.Common.Ai
{
    public record TaxonomyItem(int SkillId, string SkillName, List<string> Aliases);

    public record DetectedSkillResult(int SkillId, string SkillName, string MatchedText, string MatchedBy, int Occurrences);

    public record ParseResumeResult(
        bool Success,
        string? Error,
        string? ParsedText,
        List<DetectedSkillResult> DetectedSkills,
        List<string> Emails,
        List<string> Phones,
        int WordCount);

    public record RankCandidateInput(int CandidateId, string ResumeText);

    public record RankedResult(int CandidateId, decimal SemanticSimilarity);

    public record RankResult(bool Success, string? Error, List<RankedResult> Results);

    public interface IAiServiceClient
    {
        Task<ParseResumeResult> ParseResumeAsync(
            Stream file, string fileName, string fileFormat,
            IReadOnlyList<TaxonomyItem> taxonomy, CancellationToken ct);

        Task<RankResult> RankAsync(
            string jobDescription, IReadOnlyList<RankCandidateInput> candidates, CancellationToken ct);

        // Liveness check against the Python service's /health — backs Admin's "AI Model
        // Monitoring" screen. Returns false on any failure (timeout, connection refused,
        // non-2xx) rather than throwing; a monitoring check should never itself crash.
        Task<bool> CheckHealthAsync(CancellationToken ct);
    }
}
