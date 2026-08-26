// IRAS.Infrastructure/Persistence/Configuration/Jobs/JobMatchConfiguration.cs
using IRAS.Domain.Entities.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Jobs
{
    public class JobMatchConfiguration : IEntityTypeConfiguration<JobMatch>
    {
        public void Configure(EntityTypeBuilder<JobMatch> builder)
        {
            builder.ToTable("JobMatches", "jobs");

            builder.HasKey(m => m.MatchId);

            builder.HasIndex(m => new { m.JobId, m.CandidateId }).IsUnique();

            builder.Property(m => m.MatchScore).HasColumnType("decimal(5,4)");

            builder.HasOne(m => m.Job).WithMany()
                .HasForeignKey(m => m.JobId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
