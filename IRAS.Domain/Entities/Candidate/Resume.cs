// IRAS.Domain/Entities/Candidate/Resume.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Candidate
{
    public class Resume
    {
        public int ResumeId { get; set; }
        public int CandidateId { get; set; }
        public string FileUrl { get; set; } = null!;
        public ResumeFormat FileFormat { get; set; }
        public bool IsPrimary { get; set; }
        public string? ParsedText { get; set; }
        public ParseStatus ParseStatus { get; set; } = ParseStatus.Pending;
        public string? ParseError { get; set; }              // correction #7
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public CandidateProfile Candidate { get; set; } = null!;
    }
}