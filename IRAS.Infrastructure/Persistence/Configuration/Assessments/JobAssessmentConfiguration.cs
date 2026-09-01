// IRAS.Infrastructure/Persistence/Configuration/Assessments/JobAssessmentConfiguration.cs
using IRAS.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Assessments
{
    public class JobAssessmentConfiguration : IEntityTypeConfiguration<JobAssessment>
    {
        public void Configure(EntityTypeBuilder<JobAssessment> builder)
        {
            builder.ToTable("JobAssessments", "assessments");

            builder.HasKey(a => a.JobAssessmentId);

            // One assessment per job — generated once on first use, reused for every candidate.
            builder.HasIndex(a => a.JobId).IsUnique();

            // Restrict, not Cascade — Job already has other Restrict-configured dependents
            // (see JobRequiredSkillConfiguration/ApplicationConfiguration comments); a second
            // cascade path from Job would trip SQL Server's multiple-cascade-paths rule.
            builder.HasOne(a => a.Job).WithMany()
                .HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
