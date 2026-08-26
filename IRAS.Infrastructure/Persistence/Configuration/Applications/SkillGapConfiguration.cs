// IRAS.Infrastructure/Persistence/Configuration/Applications/SkillGapConfiguration.cs
using IRAS.Domain.Entities.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Applications
{
    public class SkillGapConfiguration : IEntityTypeConfiguration<SkillGap>
    {
        public void Configure(EntityTypeBuilder<SkillGap> builder)
        {
            builder.ToTable("SkillGaps", "applications");

            builder.HasKey(g => g.GapId);

            builder.HasOne(g => g.Skill).WithMany()
                .HasForeignKey(g => g.SkillId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
