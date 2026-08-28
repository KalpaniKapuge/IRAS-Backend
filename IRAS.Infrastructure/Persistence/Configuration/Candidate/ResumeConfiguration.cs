// IRAS.Infrastructure/Persistence/Configuration/Candidate/ResumeConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
    {
        public void Configure(EntityTypeBuilder<Resume> builder)
        {
            builder.ToTable("Resumes", "candidate");

            // SQL Server rejects a true ON DELETE SET NULL here ("may cause cycles or
            // multiple cascade paths") since Resumes and CvDocuments both cascade from
            // CandidateProfiles. ClientSetNull avoids that DB-level conflict (NO ACTION at
            // the DB); CvService.DeleteCvAsync explicitly nulls out SourceCvId on any
            // referencing resumes before deleting the CV, so orphaned FK values can't occur.
            builder.HasOne(r => r.SourceCv)
                .WithMany()
                .HasForeignKey(r => r.SourceCvId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
