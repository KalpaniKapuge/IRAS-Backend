// IRAS.Infrastructure/Persistence/Configuration/Applications/ApplicationStatusHistoryConfiguration.cs
using IRAS.Domain.Entities.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Applications
{
    public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
    {
        public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
        {
            builder.ToTable("ApplicationStatusHistories", "applications");

            builder.HasKey(h => h.HistoryId);

            builder.HasOne(h => h.ChangedByUser).WithMany()
                .HasForeignKey(h => h.ChangedBy).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
