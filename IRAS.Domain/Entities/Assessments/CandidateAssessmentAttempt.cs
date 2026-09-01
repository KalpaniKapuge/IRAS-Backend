// IRAS.Domain/Entities/Assessments/CandidateAssessmentAttempt.cs
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Assessments
{
    // One attempt per (CandidateId, JobId), enforced with a unique index in
    // AssessmentConfiguration — a candidate gets exactly one attempt at a given job's
    // assessment, matching the confirmed "one attempt only" product decision.
    public class CandidateAssessmentAttempt
    {
        public int AttemptId { get; set; }
        public int CandidateId { get; set; }
        public int JobId { get; set; }
        public int JobAssessmentId { get; set; }

        public AssessmentAttemptStatus Status { get; set; } = AssessmentAttemptStatus.InProgress;

        // 0..1 fractional, same convention as Application.SkillMatch/TotalScore/etc.
        // (frontend's formatScore() multiplies by 100 for display).
        public decimal? Score { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
        public Job Job { get; set; } = null!;
        public JobAssessment JobAssessment { get; set; } = null!;
        public ICollection<CandidateAssessmentAnswer> Answers { get; set; } = new List<CandidateAssessmentAnswer>();
    }
}
