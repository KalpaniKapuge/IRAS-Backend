// IRAS.Domain/Entities/Candidate/Certification.cs
namespace IRAS.Domain.Entities.Candidate
{
    public class Certification
    {
        public int CertificationId { get; set; }
        public int CandidateId { get; set; }
        public string Name { get; set; } = null!;
        public string? IssuingOrg { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
    }
}