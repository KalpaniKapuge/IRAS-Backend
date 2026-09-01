// IRAS.Infrastructure/Persistence/Configuration/Applications/ApplicationConfiguration.cs
using IRAS.Domain.Entities.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Applications
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            builder.ToTable("Applications", "applications");

            builder.HasIndex(a => new { a.CandidateId, a.JobId }).IsUnique();

            foreach (var prop in new[] { "TotalScore", "SkillMatch", "ExperienceMatch", "EducationMatch", "SemanticSimilarity", "AssessmentScore" })
                builder.Property(prop).HasColumnType("decimal(5,4)");

            builder.HasOne(a => a.Resume).WithMany()
                .HasForeignKey(a => a.ResumeId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Job).WithMany()
                .HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
