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
    }
}
