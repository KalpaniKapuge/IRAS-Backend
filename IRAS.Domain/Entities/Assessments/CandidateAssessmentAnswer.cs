// IRAS.Domain/Entities/Assessments/CandidateAssessmentAnswer.cs
namespace IRAS.Domain.Entities.Assessments
{
    public class CandidateAssessmentAnswer
    {
        public int AnswerId { get; set; }
        public int AttemptId { get; set; }
        public int AssessmentQuestionId { get; set; }

        // MultipleChoice only; null when the question is FreeText or was left unanswered.
        public int? SelectedOptionIndex { get; set; }

        // FreeText only; null when the question is MultipleChoice or was left unanswered.
        public string? FreeTextAnswer { get; set; }

        // 0..1 — binary (1/0) for MultipleChoice, AI/keyword-graded fraction for FreeText,
        // 0 for a question the candidate never answered (e.g. the timer ran out).
        public decimal ScoreFraction { get; set; }

        public CandidateAssessmentAttempt Attempt { get; set; } = null!;
        public AssessmentQuestion Question { get; set; } = null!;
    }
}
