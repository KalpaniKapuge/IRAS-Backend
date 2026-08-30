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

        public SkillImprovementPlan Plan { get; set; } = null!;
        public User? VerifiedByUser { get; set; }
    }
}
