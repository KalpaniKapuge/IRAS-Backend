// IRAS.Application/Modules/Assessments/TemplateAssessmentQuestionGenerator.cs
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.Assessments
{
    // Deterministic fallback used when no Gemini API key is configured — a small static
    // question bank keyed by SkillCategory, since generic per-skill-name questions aren't
    // feasible without an LLM. Keeps assessments functional (if generic) rather than
    // failing the whole apply flow when AI is unavailable.
    public class TemplateAssessmentQuestionGenerator : IAssessmentQuestionGenerator
    {
        public string Name => "Template";

        private static readonly Dictionary<SkillCategory, List<GeneratedQuestion>> Bank = new()
        {
            [SkillCategory.ProgrammingLanguage] = new()
            {
                new("Which of these best describes a variable declared but never assigned a value in most statically typed languages?",
                    new() { "It has a default value", "It causes a compile error if used before assignment", "It is automatically deleted", "It becomes a constant" }, 1, null),
                new("What is the primary purpose of a function/method in programming?",
                    new() { "To store data permanently", "To group reusable logic under a name", "To style output text", "To connect to a database" }, 1, null),
            },
            [SkillCategory.Framework] = new()
            {
                new("What is a common benefit of using an established framework over writing everything from scratch?",
                    new() { "It removes the need for testing", "It provides reusable, tested building blocks and conventions", "It guarantees zero bugs", "It replaces the need for a database" }, 1, null),
            },
            [SkillCategory.Database] = new()
            {
                new("What does a database index primarily improve?",
                    new() { "Data encryption", "Read/query performance", "Backup file size", "Network latency" }, 1, null),
                new("In a relational database, what enforces that a foreign key value must exist in the referenced table?",
                    new() { "A unique constraint", "A referential integrity constraint", "An index", "A trigger" }, 1, null),
            },
            [SkillCategory.CloudPlatform] = new()
            {
                new("What is a key advantage of cloud infrastructure over fixed on-premises servers?",
                    new() { "It never has any cost", "It can scale resources up or down on demand", "It requires no security configuration", "It eliminates the need for monitoring" }, 1, null),
            },
            [SkillCategory.Tool] = new()
            {
                new("What is the main purpose of a version control system like Git?",
                    new() { "To compile source code", "To track and manage changes to files over time", "To deploy applications to production", "To design user interfaces" }, 1, null),
            },
            [SkillCategory.SoftSkill] = new()
            {
                new("When you disagree with a teammate's technical approach, what is generally the most effective first step?",
                    new() { "Escalate immediately to a manager", "Discuss the concern directly with the teammate with specific reasoning", "Silently implement it your own way instead", "Ignore it and let them find out later" }, 1, null),
            },
            [SkillCategory.Other] = new()
            {
                new("Why is it generally good practice to write automated tests for your code?",
                    new() { "They make the code run faster", "They catch regressions and document expected behavior", "They are required by all compilers", "They replace the need for code review" }, 1, null),
            },
        };

        public Task<List<GeneratedQuestion>> GenerateAsync(
            Job job, IEnumerable<(string SkillName, string Importance, SkillCategory Category)> skills, int questionCount, CancellationToken ct)
        {
            // No AI available to reason about a specific skill name, so pick generic
            // questions from the categories the job's required skills actually fall into —
            // must-have categories first, cycling through their question pools, then
            // topping up from the rest of the bank if more questions are still needed.
            var orderedCategories = skills
                .OrderByDescending(s => s.Importance == "MustHave")
                .Select(s => s.Category)
                .Distinct()
                .Where(Bank.ContainsKey)
                .ToList();

            var pool = orderedCategories.SelectMany(c => Bank[c]).ToList();
            if (pool.Count == 0)
                pool = Bank.Values.SelectMany(v => v).ToList();

            var questions = new List<GeneratedQuestion>();
            for (var i = 0; i < questionCount && pool.Count > 0; i++)
                questions.Add(pool[i % pool.Count]);

            return Task.FromResult(questions);
        }
    }
}
