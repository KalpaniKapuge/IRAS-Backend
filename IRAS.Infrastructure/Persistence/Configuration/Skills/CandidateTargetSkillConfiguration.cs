// IRAS.Infrastructure/Persistence/Configuration/Skills/CandidateTargetSkillConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class CandidateTargetSkillConfiguration : IEntityTypeConfiguration<CandidateTargetSkill>
    {
        public void Configure(EntityTypeBuilder<CandidateTargetSkill> builder)
        {
            builder.ToTable("CandidateTargetSkills", "skills");

            builder.HasKey(t => new { t.CandidateId, t.SkillId });

            builder.HasOne(t => t.Skill).WithMany()
                .HasForeignKey(t => t.SkillId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
