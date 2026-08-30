namespace IRAS.Application.Common.Options
{
    // Thresholds for automatic skill-plan-evidence triage (see IEvidenceReviewer /
    // SkillPlanEvidenceService). AutoApproveThreshold must be greater than
    // AutoRejectThreshold — the band between them is what still reaches the admin queue.
    public class EvidenceReviewOptions
    {
        public const string SectionName = "EvidenceReview";

        public int AutoApproveThreshold { get; set; } = 80;
        public int AutoRejectThreshold { get; set; } = 25;
    }
}
