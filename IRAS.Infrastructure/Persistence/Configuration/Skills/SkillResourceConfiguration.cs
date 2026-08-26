// IRAS.Infrastructure/Persistence/Configuration/Skills/SkillResourceConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class SkillResourceConfiguration : IEntityTypeConfiguration<SkillResource>
    {
        public void Configure(EntityTypeBuilder<SkillResource> builder)
        {
            builder.ToTable("SkillResources", "skills");

            builder.HasKey(r => r.ResourceId);

            builder.HasOne(r => r.Skill).WithMany()
                .HasForeignKey(r => r.SkillId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.CreatedByUser).WithMany()
                .HasForeignKey(r => r.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
