// IRAS.Application/Modules/SkillGaps/ISkillGapExplainer.cs
namespace IRAS.Application.Modules.SkillGaps
{
    // Same swappable-generator pattern as IJdGenerator/IFeedbackGenerator: the template
    // implementation is the deterministic baseline (no external calls, no API key needed);
    // an LLM-backed implementation produces richer, more specific explanations of why a
    // missing skill matters for the role and how to close the gap.
    public interface ISkillGapExplainer
    {
        string Name { get; }
        bool IsAi { get; }

        // Batched, not per-skill: one call explains every gap on an application at once —
        // both cheaper (a single AI-service round trip) and lets an LLM implementation write
        // gaps that read as coherent advice rather than N independently-generated sentences.
        // Returned dictionary is keyed by SkillId so callers can zip results back onto the
        // SkillGap entities they're creating.
        Task<Dictionary<int, string>> ExplainAsync(
            string jobTitle,
            IEnumerable<(int SkillId, string SkillName, string Importance)> gaps,
            CancellationToken ct);
    }
}
