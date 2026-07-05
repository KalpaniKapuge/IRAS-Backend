// IRAS.Domain/Entities/Jobs/JobMatch.cs
using IRAS.Domain.Entities.Candidate;

namespace IRAS.Domain.Entities.Jobs
{
    public class JobMatch
    {
        public int MatchId { get; set; }
        public int JobId { get; set; }
        public int CandidateId { get; set; }
        public decimal MatchScore { get; set; }
        public bool ThresholdPassed { get; set; }
        public bool IsNotified { get; set; }
        public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

        public Job Job { get; set; } = null!;
        public CandidateProfile Candidate { get; set; } = null!;
    }
}