namespace IRAS.Application.Modules.Matching.DTOs
{
    public class JobRecommendationDto
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string? CompanyName { get; set; }
        public decimal MatchScore { get; set; }
        public decimal SkillMatch { get; set; }
        public decimal SemanticSimilarity { get; set; }
        public decimal? MlFitScore { get; set; }
    }
}
