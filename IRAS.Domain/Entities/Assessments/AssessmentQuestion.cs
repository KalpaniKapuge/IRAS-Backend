// IRAS.Domain/Entities/Assessments/AssessmentQuestion.cs
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Assessments
{
    public class AssessmentQuestion
    {
        public int AssessmentQuestionId { get; set; }
        public int JobAssessmentId { get; set; }

        // Which required skill this question targets — nullable since not every
        // generated question is cleanly attributable to a single taxonomy skill.
        public int? SkillId { get; set; }

        public AssessmentQuestionType QuestionType { get; set; } = AssessmentQuestionType.MultipleChoice;
        public string QuestionText { get; set; } = null!;

        // MultipleChoice only: exactly 4 answer choices, stored as JSON via a ValueConverter
        // (see AssessmentQuestionConfiguration) rather than a child table — same "AI-generated
        // structured content in a single column" precedent as Job.GeneratedJd. Empty for FreeText.
        public List<string> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }

        // FreeText only: the reference/expected answer used by IAssessmentAnswerGrader to
        // judge a candidate's written answer. Null for MultipleChoice.
        public string? ModelAnswer { get; set; }

        public int QuestionOrder { get; set; }

        public JobAssessment JobAssessment { get; set; } = null!;
        public Skill? Skill { get; set; }
    }
}
