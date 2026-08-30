// IRAS.Domain/Entities/Skills/SkillPlanEvidence.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Skills
{
    // Proof a candidate submits against one SkillImprovementPlan — a GitHub link, an
    // uploaded file/screenshot, or a certificate — reviewed by an admin. Approval only ever
    // promotes the plan to Verified once its roadmap is already fully complete; rejection
    // never downgrades the plan, it just means this particular piece of evidence wasn't
    // sufficient (see SkillPlanEvidenceService.VerifyEvidenceAsync).
    public class SkillPlanEvidence
    {
        public int EvidenceId { get; set; }
        public int PlanId { get; set; }

        public SkillEvidenceType EvidenceType { get; set; }
        public string EvidenceUrl { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public EvidenceVerificationStatus VerificationStatus { get; set; } = EvidenceVerificationStatus.Pending;
        public int? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifierNotes { get; set; }

        // Set at submission time for link-type evidence only (GitHub/Other) — file-backed
        // evidence (File/Screenshot/Certificate) has no automatic review, since judging file
        // contents would need vision capability this integration doesn't have; those always
        // fall through to the admin queue. AutoReviewed is true when AiConfidenceScore alone
        // decided VerificationStatus (crossed a threshold); false means either no AI review
        // happened, or a human admin made the final call via VerifyEvidenceAsync — VerifiedBy
        // being non-null is the authoritative signal for "a human decided this."
        public int? AiConfidenceScore { get; set; }
        public string? AiRationale { get; set; }
        public bool AutoReviewed { get; set; }

        public SkillImprovementPlan Plan { get; set; } = null!;
        public User? VerifiedByUser { get; set; }
    }
}
