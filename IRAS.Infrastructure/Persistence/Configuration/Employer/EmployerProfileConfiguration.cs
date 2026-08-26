// IRAS.Infrastructure/Persistence/Configuration/Employer/EmployerProfileConfiguration.cs
using IRAS.Domain.Entities.Employer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Employer
{
    public class EmployerProfileConfiguration : IEntityTypeConfiguration<EmployerProfile>
    {
        public void Configure(EntityTypeBuilder<EmployerProfile> builder)
        {
            builder.ToTable("EmployerProfiles", "employer");

            builder.HasKey(e => e.EmployerId);

            builder.HasOne(e => e.User).WithOne(u => u.EmployerProfile)
                .HasForeignKey<EmployerProfile>(e => e.EmployerId);
        }
    }
}
