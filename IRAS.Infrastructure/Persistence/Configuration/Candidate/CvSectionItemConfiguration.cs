// IRAS.Infrastructure/Persistence/Configuration/Candidate/CvSectionItemConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class CvSectionItemConfiguration : IEntityTypeConfiguration<CvSectionItem>
    {
        public void Configure(EntityTypeBuilder<CvSectionItem> builder)
        {
            builder.ToTable("CvSectionItems", "candidate");
        }
    }
}
