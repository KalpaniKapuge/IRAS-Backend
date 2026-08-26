// IRAS.Domain/Entities/Skills/CandidateTargetSkill.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Candidate;

namespace IRAS.Domain.Entities.Skills
{
    // A skill the candidate has chosen to work on after seeing it flagged as a gap —
    // distinct from CandidateSkill (skills they already have). Closes the loop from
    // "here's what you're missing" to "here's what you're doing about it."
    public class CandidateTargetSkill
    {
        public int CandidateId { get; set; }
        public int SkillId { get; set; }
        public TargetSkillStatus Status { get; set; } = TargetSkillStatus.Learning;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
