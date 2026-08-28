// IRAS.Domain/Entities/Candidate/CandidateLanguage.cs
namespace IRAS.Domain.Entities.Candidate
{
    public class CandidateLanguage
    {
        public int LanguageId { get; set; }
        public int CandidateId { get; set; }
        public string LanguageName { get; set; } = null!;
        public string Proficiency { get; set; } = null!;   // free text: "Native", "Fluent", "B2", etc.

        public CandidateProfile Candidate { get; set; } = null!;
    }
}
