// IRAS.Application/Common/Scoring/ScoringOptions.cs
namespace IRAS.Application.Common.Scoring
{
    public class ScoringOptions
    {
        public const string SectionName = "Scoring";

        // Weighted formula: TotalScore = SkillMatchWeight * skillMatch
        //                                + SemanticSimilarityWeight * semanticSimilarity
        //                                + MlFitScoreWeight * mlFitScore
        //                                + AssessmentScoreWeight * assessmentScore.
        // Skill match is exact/taxonomy-based and auditable; semantic similarity is a softer
        // free-text signal; mlFitScore is the trained candidate-job fit classifier's Good-Fit
        // probability; assessmentScore is the candidate's score on the job's skill assessment
        // (only present when the job requires one). Both MlFitScoreWeight and
        // AssessmentScoreWeight default to 0 so existing deployments keep prior behavior
        // until an operator deliberately opts in. Must sum to 1.
        public decimal SkillMatchWeight { get; set; } = 0.6m;
        public decimal SemanticSimilarityWeight { get; set; } = 0.4m;
        public decimal MlFitScoreWeight { get; set; } = 0m;
        public decimal AssessmentScoreWeight { get; set; } = 0m;

        // Minimum TotalScore for the proactive matcher (Module 8) to notify a candidate.
        public decimal AutoMatchThreshold { get; set; } = 0.5m;

        // Separate weighted formula for "Total marks" — the headline figure shown to employers
        // on the applicant list (ScoringService.ComputeTotalMarks). Deliberately distinct from
        // TotalScore above: it only uses the signals an employer can actually SEE broken out in
        // the UI (skill/experience/education match, resume relevance, assessment score) — never
        // the opaque MlFitScore — so the number the employer sees is always auditable from the
        // bars right below it. Skill match and assessment score are weighted highest since they
        // most directly answer "does this candidate really have the required skills" — the
        // whole reason the assessment feature exists; a candidate with 0% skill match should
        // score poorly here even with strong experience/education, not be masked by an average.
        // Must sum to 1. When a job has no assessment (AssessmentScore is null), its weight is
        // dropped and the rest are renormalized so the result still reads as a true 0-100%.
        public decimal MarksSkillWeight { get; set; } = 0.35m;
        public decimal MarksAssessmentWeight { get; set; } = 0.30m;
        public decimal MarksExperienceWeight { get; set; } = 0.15m;
        public decimal MarksEducationWeight { get; set; } = 0.10m;
        public decimal MarksSemanticWeight { get; set; } = 0.10m;
    }
}
