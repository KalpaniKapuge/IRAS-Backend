// IRAS.Application/Modules/SkillImprovementPlans/DTOs/SkillImprovementPlanDtos.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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
        public List<SkillPlanEvidenceDto> Evidence { get; set; } = new();
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

    public class SkillPlanEvidenceDto
    {
        public int EvidenceId { get; set; }
        public int PlanId { get; set; }
        public string EvidenceType { get; set; } = null!;
        public string EvidenceUrl { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string VerificationStatus { get; set; } = null!;
        public DateTime? VerifiedAt { get; set; }
        public string? VerifierNotes { get; set; }

        // Null for file-backed evidence (no automatic review — see IEvidenceReviewer).
        public int? AiConfidenceScore { get; set; }
        public string? AiRationale { get; set; }
        public bool AutoReviewed { get; set; }
    }

    // For GitHub links or any other external URL — no file involved.
    public class AddEvidenceLinkRequest
    {
        [Required]
        public string EvidenceType { get; set; } = null!;   // GitHub | Other

        [Required, StringLength(500)]
        public string EvidenceUrl { get; set; } = null!;

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    // For File | Screenshot | Certificate — bound via [FromForm], File resolved either from
    // this property or Request.Form.Files as a fallback (same pattern as
    // CandidateProfileController's certificate upload).
    public class AddEvidenceFileRequest
    {
        [Required]
        public string EvidenceType { get; set; } = null!;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public IFormFile? File { get; set; }
    }

    // Approve => Evidence Approved (+ plan/skill promotion if roadmap complete).
    // Reject => Evidence Rejected, terminal for this submission; candidate may resubmit.
    // RequestRevision => Evidence RevisionRequired, candidate acts on VerifierNotes and resubmits.
    public class VerifyEvidenceRequest
    {
        [Required]
        public string Decision { get; set; } = null!;   // Approve | Reject | RequestRevision

        [StringLength(1000)]
        public string? VerifierNotes { get; set; }
    }

    // Admin-facing review queue row — flattened with enough candidate/skill/job context and
    // a roadmap-completion summary to review without a separate lookup per row.
    public class AdminEvidenceReviewDto
    {
        public int EvidenceId { get; set; }
        public int PlanId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = null!;
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string? JobTitle { get; set; }

        // Enough of the roadmap for the admin to judge whether the evidence actually proves
        // it, without a separate lookup — see the spec's "Improvement plan" review bullet.
        public string PlanOverview { get; set; } = null!;
        public string ProjectTitle { get; set; } = null!;
        public string ProjectTask { get; set; } = null!;
        public string ProjectExpectedOutput { get; set; } = null!;

        public string EvidenceType { get; set; } = null!;
        public string EvidenceUrl { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string VerificationStatus { get; set; } = null!;
        public int? AiConfidenceScore { get; set; }
        public string? AiRationale { get; set; }
        public int StepsCompleted { get; set; }
        public int TotalSteps { get; set; }

        // Populated only once a decision has been made — lets the admin history view (any
        // status other than Pending) show what was already decided and why, read-only.
        public DateTime? VerifiedAt { get; set; }
        public string? VerifierNotes { get; set; }
    }
}
