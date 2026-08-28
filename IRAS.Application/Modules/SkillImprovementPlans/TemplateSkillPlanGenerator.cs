// IRAS.Application/Modules/SkillImprovementPlans/TemplateSkillPlanGenerator.cs
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    // Baseline generator: deterministic, no external calls — mirrors TemplateJdGenerator /
    // TemplateFeedbackGenerator / TemplateSkillGapExplainer. Produces a generic but genuinely
    // usable 8-stage roadmap (fundamentals -> setup -> practice -> real project -> review)
    // parameterized by skill name, so the feature works end-to-end even without a Gemini
    // API key configured.
    public class TemplateSkillPlanGenerator : ISkillPlanGenerator
    {
        public string Name => "Template";
        public bool IsAi => false;

        public Task<SkillPlanGenerationResult> GenerateAsync(
            string skillName, string? jobTitle, string importance, CancellationToken ct)
        {
            var isMustHave = string.Equals(importance, "MustHave", StringComparison.OrdinalIgnoreCase);
            var roleContext = string.IsNullOrWhiteSpace(jobTitle) ? "roles like this" : $"the {jobTitle} role";

            var result = new SkillPlanGenerationResult(
                Overview: $"{skillName} is a skill relevant to {roleContext}. Building hands-on, " +
                          $"demonstrable familiarity with it will strengthen your fit for similar positions.",
                GapReason: $"Your profile doesn't currently show confirmed experience with {skillName}, " +
                           $"which this role treats as a {(isMustHave ? "must-have" : "nice-to-have")} skill.",
                TargetLevel: isMustHave ? SkillTargetLevel.JobReady : SkillTargetLevel.Intermediate,
                Priority: isMustHave ? SkillPlanPriority.High : SkillPlanPriority.Medium,
                EstimatedDays: isMustHave ? 14 : 10,
                ProjectTitle: $"{skillName}-Powered Mini Project",
                ProjectTask: $"Build a small project that applies {skillName} in a realistic, working way.",
                ProjectExpectedOutput: "A working project with source code, a short README, and a brief " +
                                       "explanation of what was built and learned.",
                Steps:
                [
                    new($"Foundation",
                        $"What {skillName} is, the problem it solves, and where it fits in a typical workflow.",
                        "Read introductory documentation and watch a beginner-level overview.",
                        $"Can explain what {skillName} is and why it's used, in your own words."),

                    new($"Setup",
                        $"Install and configure {skillName} in your own development environment.",
                        $"Install {skillName} and verify it works with a minimal test.",
                        $"{skillName} is installed and confirmed working locally."),

                    new($"Core Concepts",
                        $"The fundamental commands, syntax, or concepts of {skillName}.",
                        "Practice the core operations directly, not just reading about them.",
                        $"Comfortable performing basic {skillName} operations without a guide."),

                    new($"Guided Practice",
                        $"Apply {skillName} to a small, well-defined example or tutorial exercise.",
                        "Follow a hands-on guided exercise from start to finish.",
                        "Completed a guided exercise using nothing but your own setup."),

                    new($"Intermediate Techniques",
                        $"More advanced or realistic features of {skillName} beyond the basics.",
                        "Explore intermediate functionality relevant to real-world use.",
                        $"Can apply {skillName} to a more realistic scenario, not just a tutorial."),

                    new($"Real Project Integration",
                        $"Apply {skillName} inside an existing personal or practice project.",
                        $"Integrate {skillName} into a project you already have, or a new small one.",
                        $"{skillName} is used in a real, working codebase, not an isolated exercise."),

                    new($"Troubleshooting",
                        $"How to diagnose and fix the most common problems when using {skillName}.",
                        "Deliberately break something, then debug and fix it yourself.",
                        $"Can resolve common {skillName} problems independently."),

                    new($"Mini Project & Review",
                        "Consolidate everything learned into one complete, presentable project.",
                        "Build, document, and review the mini project described above.",
                        $"A portfolio-ready project that demonstrably proves {skillName} competency.")
                ]);

            return Task.FromResult(result);
        }
    }
}
