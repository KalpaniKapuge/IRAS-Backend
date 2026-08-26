// IRAS.Infrastructure/Persistence/Configuration/Skills/SkillAliasConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class SkillAliasConfiguration : IEntityTypeConfiguration<SkillAlias>
    {
        public void Configure(EntityTypeBuilder<SkillAlias> builder)
        {
            builder.ToTable("SkillAliases", "skills");

            builder.HasKey(a => a.AliasId);

            builder.HasIndex(a => a.AliasText).IsUnique();
        }
    }
}
