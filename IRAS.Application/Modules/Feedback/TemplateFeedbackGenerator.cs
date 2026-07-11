// IRAS.Application/Modules/Feedback/TemplateFeedbackGenerator.cs
using System.Text;

namespace IRAS.Application.Modules.Feedback
{
    // Baseline generator: deterministic, no external calls — mirrors TemplateJdGenerator.
    // Deliberately constructive in tone (never blames the candidate) per the ethical
    // requirement that AI-generated feedback must assist, not discriminate.
    public class TemplateFeedbackGenerator : IFeedbackGenerator
    {
        public string Name => "Template";
        public bool IsAi => false;

        public Task<string> GenerateAsync(
            string jobTitle, string companyName, decimal totalScore,
            IEnumerable<(string SkillName, string Importance, string? Suggestion)> skillGaps,
            CancellationToken ct)
        {
            var gaps = skillGaps.ToList();
            var mustHave = gaps.Where(g => g.Importance == "MustHave").ToList();
            var niceToHave = gaps.Where(g => g.Importance == "NiceToHave").ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Thank you for applying for the {jobTitle} position at {companyName}.");
            sb.AppendLine();
            sb.AppendLine("After careful review, we've decided not to move forward with your application " +
                          "at this time. This reflects fit for this specific role rather than your overall potential.");
            sb.AppendLine();

            if (mustHave.Count > 0)
            {
                sb.AppendLine("Areas that would strengthen a future application for a role like this:");
                foreach (var gap in mustHave)
                    sb.AppendLine(gap.Suggestion != null ? $"- {gap.Suggestion}" : $"- {gap.SkillName}");
                sb.AppendLine();
            }

            if (niceToHave.Count > 0)
            {
                sb.AppendLine("Skills that would also add value:");
                foreach (var gap in niceToHave)
                    sb.AppendLine(gap.Suggestion != null ? $"- {gap.Suggestion}" : $"- {gap.SkillName}");
                sb.AppendLine();
            }

            sb.AppendLine("We encourage you to apply again as your experience grows, and we wish you the best in your job search.");
            return Task.FromResult(sb.ToString());
        }
    }
}
