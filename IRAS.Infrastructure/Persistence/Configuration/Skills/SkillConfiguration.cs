// IRAS.Infrastructure/Persistence/Configuration/Skills/SkillConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class SkillConfiguration : IEntityTypeConfiguration<Skill>
    {
        public void Configure(EntityTypeBuilder<Skill> builder)
        {
            builder.ToTable("Skills", "skills");

            builder.HasIndex(s => s.SkillName).IsUnique();
        }
    }
}
