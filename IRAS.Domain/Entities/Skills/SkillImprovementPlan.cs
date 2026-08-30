// IRAS.Domain/Entities/Skills/SkillImprovementPlan.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Domain.Entities.Skills
{
    // The A-Z learning roadmap generated for one missing skill — the core artifact behind
    // the "closed-loop skill development" research contribution. One plan per
    // (CandidateId, SkillId); JobId is context only (which application surfaced the gap),
    // nullable because a candidate can also start a plan from the aggregated Skill Gaps
    // summary rather than one specific application.
    public class SkillImprovementPlan
    {
        public int PlanId { get; set; }
        public int CandidateId { get; set; }
        public int SkillId { get; set; }
        public int? JobId { get; set; }

        public SkillPlanPriority Priority { get; set; }
        public SkillTargetLevel TargetLevel { get; set; }
        public int EstimatedDays { get; set; }

        public string Overview { get; set; } = null!;
        public string GapReason { get; set; } = null!;

        public string ProjectTitle { get; set; } = null!;
        public string ProjectTask { get; set; } = null!;
        public string ProjectExpectedOutput { get; set; } = null!;

        public SkillPlanStatus Status { get; set; } = SkillPlanStatus.NotStarted;

        // "Gemini" or "Template" — which ISkillPlanGenerator produced this, same audit
        // signal as IJdGenerator/IFeedbackGenerator's Name property elsewhere.
        public string GeneratedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CandidateProfile Candidate { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
        public Job? Job { get; set; }
        public ICollection<SkillPlanStep> Steps { get; set; } = new List<SkillPlanStep>();
        public ICollection<SkillPlanEvidence> Evidence { get; set; } = new List<SkillPlanEvidence>();
    }
}
