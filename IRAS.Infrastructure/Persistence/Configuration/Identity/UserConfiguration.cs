// IRAS.Infrastructure/Persistence/Configuration/Identity/UserConfiguration.cs
using IRAS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Identity
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "identity");

            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.AuthProvider).HasMaxLength(20).HasDefaultValue("Local");
        }
    }
}
