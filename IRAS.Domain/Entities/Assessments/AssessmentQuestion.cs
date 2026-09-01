// IRAS.Domain/Entities/Assessments/AssessmentQuestion.cs
using IRAS.Domain.Entities.Skills;

namespace IRAS.Domain.Entities.Assessments
{
    public class AssessmentQuestion
    {
        public int AssessmentQuestionId { get; set; }
        public int JobAssessmentId { get; set; }

        // Which required skill this question targets — nullable since not every
        // generated question is cleanly attributable to a single taxonomy skill.
        public int? SkillId { get; set; }

        public string QuestionText { get; set; } = null!;

        // Exactly 4 answer choices, stored as JSON via a ValueConverter (see
        // AssessmentQuestionConfiguration) rather than a child table — same "AI-generated
        // structured content in a single column" precedent as Job.GeneratedJd.
        public List<string> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }
        public int QuestionOrder { get; set; }

        public JobAssessment JobAssessment { get; set; } = null!;
        public Skill? Skill { get; set; }
    }
}
