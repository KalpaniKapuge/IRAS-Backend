// IRAS.Infrastructure/Persistence/Configuration/Candidate/CvDocumentConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class CvDocumentConfiguration : IEntityTypeConfiguration<CvDocument>
    {
        public void Configure(EntityTypeBuilder<CvDocument> builder)
        {
            builder.ToTable("CvDocuments", "candidate");

            builder.HasKey(c => c.CvId);
        }
    }
}
