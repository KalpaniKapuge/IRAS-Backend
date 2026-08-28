// IRAS.Application/Modules/SkillImprovementPlans/DTOs/SkillImprovementPlanDtos.cs
namespace IRAS.Application.Modules.SkillImprovementPlans.DTOs
{
    public class SkillImprovementPlanDto
    {
        public int PlanId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public int? JobId { get; set; }
        public string? JobTitle { get; set; }

        public string Priority { get; set; } = null!;
        public string TargetLevel { get; set; } = null!;
        public int EstimatedDays { get; set; }

        public string Overview { get; set; } = null!;
        public string GapReason { get; set; } = null!;

        public string ProjectTitle { get; set; } = null!;
        public string ProjectTask { get; set; } = null!;
        public string ProjectExpectedOutput { get; set; } = null!;

        public string Status { get; set; } = null!;
        public string GeneratedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Rounded percentage of Steps with IsCompleted=true — computed at read time so it's
        // never allowed to drift out of sync with the steps themselves.
        public int ProgressPercent { get; set; }

        public List<SkillPlanStepDto> Steps { get; set; } = new();
    }

    public class SkillPlanStepDto
    {
        public int StepId { get; set; }
        public int StepOrder { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Activity { get; set; } = null!;
        public string Output { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class GeneratePlanRequest
    {
        // Which application surfaced this gap, for job-title context in the generated
        // roadmap. Omit when generating from the aggregated Skill Gaps summary rather than
        // one specific application.
        public int? JobId { get; set; }
    }

    public class SetStepCompletionRequest
    {
        public bool IsCompleted { get; set; } = true;
    }
}
