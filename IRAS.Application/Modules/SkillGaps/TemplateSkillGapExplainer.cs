// IRAS.Application/Modules/SkillGaps/TemplateSkillGapExplainer.cs
namespace IRAS.Application.Modules.SkillGaps
{
    // Baseline explainer: deterministic, no external calls — mirrors TemplateJdGenerator /
    // TemplateFeedbackGenerator. Reproduces the same two fixed sentences the inline logic in
    // ApplicationService used before this abstraction existed, so selecting this
    // implementation is a behavior-preserving no-op.
    public class TemplateSkillGapExplainer : ISkillGapExplainer
    {
        public string Name => "Template";
        public bool IsAi => false;

        public Task<Dictionary<int, string>> ExplainAsync(
            string jobTitle,
            IEnumerable<(int SkillId, string SkillName, string Importance)> gaps,
            CancellationToken ct)
        {
            var result = gaps.ToDictionary(
                g => g.SkillId,
                g => g.Importance == "MustHave"
                    ? $"This role requires {g.SkillName}. Consider highlighting related experience or upskilling before interviewing."
                    : $"{g.SkillName} is a nice-to-have for this role.");
            return Task.FromResult(result);
        }
    }
}
