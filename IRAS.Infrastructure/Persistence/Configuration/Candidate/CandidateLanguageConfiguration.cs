// IRAS.Infrastructure/Persistence/Configuration/Candidate/CandidateLanguageConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class CandidateLanguageConfiguration : IEntityTypeConfiguration<CandidateLanguage>
    {
        public void Configure(EntityTypeBuilder<CandidateLanguage> builder)
        {
            builder.HasKey(l => l.LanguageId);
            builder.ToTable("CandidateLanguages", "candidate");
        }
    }
}
