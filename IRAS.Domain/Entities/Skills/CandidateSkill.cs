// IRAS.Domain/Entities/Skills/CandidateSkill.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Candidate;

namespace IRAS.Domain.Entities.Skills
{
    public class CandidateSkill
    {
        public int CandidateId { get; set; }
        public int SkillId { get; set; }
        public ProficiencyLevel Proficiency { get; set; }
        public decimal YearsExp { get; set; }
        public SkillSource Source { get; set; }
        public bool IsVerified { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}