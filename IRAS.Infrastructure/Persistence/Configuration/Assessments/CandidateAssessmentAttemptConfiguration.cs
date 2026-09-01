// IRAS.Infrastructure/Persistence/Configuration/Assessments/CandidateAssessmentAttemptConfiguration.cs
using IRAS.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Assessments
{
    public class CandidateAssessmentAttemptConfiguration : IEntityTypeConfiguration<CandidateAssessmentAttempt>
    {
        public void Configure(EntityTypeBuilder<CandidateAssessmentAttempt> builder)
        {
            builder.ToTable("CandidateAssessmentAttempts", "assessments");

            builder.HasKey(a => a.AttemptId);

            // One attempt per candidate per job — enforces the "one attempt only" rule at
            // the DB level, same pattern as ApplicationConfiguration's CandidateId+JobId index.
            builder.HasIndex(a => new { a.CandidateId, a.JobId }).IsUnique();

            builder.Property(a => a.Score).HasColumnType("decimal(5,4)");

            // Restrict on both Job FKs — Job already has other Restrict-configured
            // dependents; a second cascade path would trip SQL Server's multiple
            // cascade-paths rule (same reasoning as ApplicationConfiguration/JobAssessmentConfiguration).
            builder.HasOne(a => a.Job).WithMany()
                .HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.JobAssessment).WithMany()
                .HasForeignKey(a => a.JobAssessmentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
