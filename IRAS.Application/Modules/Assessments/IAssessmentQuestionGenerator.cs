// IRAS.Application/Modules/Assessments/IAssessmentQuestionGenerator.cs
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.Assessments
{
    public record GeneratedQuestion(string QuestionText, List<string> Options, int CorrectOptionIndex, string? SkillName);

    // Same swappable-strategy shape as IJdGenerator/ISkillPlanGenerator/IEvidenceReviewer:
    // a Gemini-backed implementation plus a deterministic Template fallback so the platform
    // keeps working without an API key configured. Category is included alongside each
    // skill so the Template fallback (no AI, can't reason about a skill name) can still
    // pick topically-relevant generic questions.
    public interface IAssessmentQuestionGenerator
    {
        string Name { get; }

        Task<List<GeneratedQuestion>> GenerateAsync(
            Job job, IEnumerable<(string SkillName, string Importance, SkillCategory Category)> skills, int questionCount, CancellationToken ct);
    }
}
