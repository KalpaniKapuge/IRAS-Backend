// IRAS.Domain/Entities/Candidate/CandidateProject.cs
namespace IRAS.Domain.Entities.Candidate
{
    public class CandidateProject
    {
        public int ProjectId { get; set; }
        public int CandidateId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? ProjectUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
    }
}
