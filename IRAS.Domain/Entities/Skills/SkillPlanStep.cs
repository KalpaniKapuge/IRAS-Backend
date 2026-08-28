// IRAS.Domain/Entities/Skills/SkillPlanStep.cs
namespace IRAS.Domain.Entities.Skills
{
    // One ordered stage of a SkillImprovementPlan's roadmap. Deliberately doubles as the
    // completion checklist (IsCompleted) rather than tracking roadmap stages and checklist
    // items as two separate structures — a step's title/description already is the
    // checklist item description, so a second list would just duplicate this one.
    public class SkillPlanStep
    {
        public int StepId { get; set; }
        public int PlanId { get; set; }
        public int StepOrder { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Activity { get; set; } = null!;
        public string Output { get; set; } = null!;

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        public SkillImprovementPlan Plan { get; set; } = null!;
    }
}
