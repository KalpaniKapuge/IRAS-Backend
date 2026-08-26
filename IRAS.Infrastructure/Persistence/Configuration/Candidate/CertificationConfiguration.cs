// IRAS.Infrastructure/Persistence/Configuration/Candidate/CertificationConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class CertificationConfiguration : IEntityTypeConfiguration<Certification>
    {
        public void Configure(EntityTypeBuilder<Certification> builder)
        {
            builder.ToTable("Certifications", "candidate");
        }
    }
}
