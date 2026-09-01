// IRAS.Domain/Entities/Assessments/CandidateAssessmentAnswer.cs
namespace IRAS.Domain.Entities.Assessments
{
    public class CandidateAssessmentAnswer
    {
        public int AnswerId { get; set; }
        public int AttemptId { get; set; }
        public int AssessmentQuestionId { get; set; }
        public int SelectedOptionIndex { get; set; }
        public bool IsCorrect { get; set; }

        public CandidateAssessmentAttempt Attempt { get; set; } = null!;
        public AssessmentQuestion Question { get; set; } = null!;
    }
}
