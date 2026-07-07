// IRAS.Application/Modules/Jobs/TemplateJdGenerator.cs
using System.Text;
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Application.Modules.Jobs
{
    // Baseline generator: deterministic, no external calls.
    // Later, an LlmJdGenerator implements the same interface via the AI service —
    // and this class becomes the comparison baseline in the evaluation chapter.
    public class TemplateJdGenerator : IJdGenerator
    {
        public string Name => "Template";
        public bool IsAi => false;

        public Task<string> GenerateAsync(Job job,
            IEnumerable<(string SkillName, string Importance, int MinYears)> skills,
            string companyName, string? companyDescription, string? additionalNotes)
        {
            var must = skills.Where(s => s.Importance == "MustHave").ToList();
            var nice = skills.Where(s => s.Importance == "NiceToHave").ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"# {job.Title} ({job.SeniorityLevel})");
            sb.AppendLine();
            sb.AppendLine($"**Company:** {companyName}");
            if (!string.IsNullOrWhiteSpace(job.Location))
                sb.AppendLine($"**Location:** {job.Location}");
            sb.AppendLine($"**Employment Type:** {job.EmploymentType}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(companyDescription))
            {
                sb.AppendLine("## About Us");
                sb.AppendLine(companyDescription);
                sb.AppendLine();
            }

            sb.AppendLine("## The Role");
            sb.AppendLine($"We are looking for a {job.SeniorityLevel} {job.Title} " +
                          $"with at least {job.MinExpYears} year(s) of relevant experience " +
                          $"and a minimum education level of {job.EducationReq}.");
            if (!string.IsNullOrWhiteSpace(additionalNotes))
                sb.AppendLine(additionalNotes);
            sb.AppendLine();

            if (must.Count > 0)
            {
                sb.AppendLine("## Required Skills");
                foreach (var s in must)
                    sb.AppendLine(s.MinYears > 0
                        ? $"- {s.SkillName} ({s.MinYears}+ years)"
                        : $"- {s.SkillName}");
                sb.AppendLine();
            }

            if (nice.Count > 0)
            {
                sb.AppendLine("## Nice to Have");
                foreach (var s in nice)
                    sb.AppendLine($"- {s.SkillName}");
                sb.AppendLine();
            }

            sb.AppendLine("## How to Apply");
            sb.AppendLine("Submit your application through our recruitment portal. " +
                          "Shortlisted candidates will be contacted for interviews.");

            return Task.FromResult(sb.ToString());
        }
    }
}