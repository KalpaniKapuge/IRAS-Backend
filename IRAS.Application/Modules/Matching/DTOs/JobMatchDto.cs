// IRAS.Application/Modules/Matching/DTOs/JobMatchDto.cs
namespace IRAS.Application.Modules.Matching.DTOs
{
    public class JobMatchDto
    {
        public int MatchId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string? CompanyName { get; set; }
        public decimal MatchScore { get; set; }
        public bool ThresholdPassed { get; set; }
        public DateTime MatchedAt { get; set; }
    }
}
