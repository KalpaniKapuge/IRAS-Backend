// IRAS.Domain/Entities/Assessments/JobAssessment.cs
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Domain.Entities.Assessments
{
    // One per Job, generated lazily the first time a candidate needs it and reused
    // for every candidate who applies — same question set for everyone so scores are
    // comparable. Editing a job's required skills after this exists does not
    // regenerate it (a deliberate v1 scope decision, not an oversight).
    public class JobAssessment
    {
        public int JobAssessmentId { get; set; }
        public int JobId { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // "Gemini" or "Template" — which IAssessmentQuestionGenerator produced this,
        // same audit signal as IJdGenerator/ISkillPlanGenerator's Name property elsewhere.
        public string GeneratedBy { get; set; } = null!;

        public Job Job { get; set; } = null!;
        public ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
    }
}
