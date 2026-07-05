// IRAS.Domain/Entities/Candidate/Education.cs
namespace IRAS.Domain.Entities.Candidate
{
    public class Education
    {
        public int EducationId { get; set; }
        public int CandidateId { get; set; }
        public string Degree { get; set; } = null!;
        public string Institution { get; set; } = null!;
        public string? FieldOfStudy { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public string? Grade { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
    }
}