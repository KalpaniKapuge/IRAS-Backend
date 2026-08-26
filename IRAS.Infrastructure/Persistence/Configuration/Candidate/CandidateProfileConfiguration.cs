// IRAS.Infrastructure/Persistence/Configuration/Candidate/CandidateProfileConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class CandidateProfileConfiguration : IEntityTypeConfiguration<CandidateProfile>
    {
        public void Configure(EntityTypeBuilder<CandidateProfile> builder)
        {
            builder.ToTable("CandidateProfiles", "candidate");

            builder.HasKey(c => c.CandidateId);

            builder.HasOne(c => c.User).WithOne(u => u.CandidateProfile)
                .HasForeignKey<CandidateProfile>(c => c.CandidateId);

            builder.Property(c => c.TotalExpYears).HasColumnType("decimal(4,1)");
        }
    }
}
