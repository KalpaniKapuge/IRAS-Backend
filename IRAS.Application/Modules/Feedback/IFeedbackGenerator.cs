// IRAS.Application/Modules/Feedback/IFeedbackGenerator.cs
namespace IRAS.Application.Modules.Feedback
{
    // Same swappable-generator pattern as IJdGenerator: the template implementation is
    // the baseline (deterministic, no external calls, no API key needed); an LLM-backed
    // generator can implement this same interface later for richer wording without any
    // caller changing.
    public interface IFeedbackGenerator
    {
        string Name { get; }
        bool IsAi { get; }

        Task<string> GenerateAsync(
            string jobTitle, string companyName, decimal totalScore,
            IEnumerable<(string SkillName, string Importance, string? Suggestion)> skillGaps,
            CancellationToken ct);
    }
}
