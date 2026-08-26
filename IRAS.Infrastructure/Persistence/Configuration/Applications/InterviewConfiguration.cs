// IRAS.Infrastructure/Persistence/Configuration/Applications/InterviewConfiguration.cs
using IRAS.Domain.Entities.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Applications
{
    public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
    {
        public void Configure(EntityTypeBuilder<Interview> builder)
        {
            builder.ToTable("Interviews", "applications");

            builder.HasOne(i => i.ScheduledByUser).WithMany()
                .HasForeignKey(i => i.ScheduledBy).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
