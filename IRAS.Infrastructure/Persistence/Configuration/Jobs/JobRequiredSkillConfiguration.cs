// IRAS.Infrastructure/Persistence/Configuration/Jobs/JobRequiredSkillConfiguration.cs
using IRAS.Domain.Entities.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Jobs
{
    public class JobRequiredSkillConfiguration : IEntityTypeConfiguration<JobRequiredSkill>
    {
        public void Configure(EntityTypeBuilder<JobRequiredSkill> builder)
        {
            builder.ToTable("JobRequiredSkills", "jobs");

            builder.HasKey(jrs => new { jrs.JobId, jrs.SkillId });

            builder.Property(j => j.Weight).HasColumnType("decimal(5,4)");

            builder.HasOne(jrs => jrs.Skill).WithMany()
                .HasForeignKey(jrs => jrs.SkillId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
