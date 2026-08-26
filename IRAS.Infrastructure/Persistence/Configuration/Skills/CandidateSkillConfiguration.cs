// IRAS.Infrastructure/Persistence/Configuration/Skills/CandidateSkillConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class CandidateSkillConfiguration : IEntityTypeConfiguration<CandidateSkill>
    {
        public void Configure(EntityTypeBuilder<CandidateSkill> builder)
        {
            builder.ToTable("CandidateSkills", "skills");

            builder.HasKey(cs => new { cs.CandidateId, cs.SkillId });

            builder.Property(c => c.YearsExp).HasColumnType("decimal(4,1)");

            builder.HasOne(cs => cs.Skill).WithMany()
                .HasForeignKey(cs => cs.SkillId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
