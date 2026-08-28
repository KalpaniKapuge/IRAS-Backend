// IRAS.Infrastructure/Persistence/Configuration/Candidate/CandidateProjectConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class CandidateProjectConfiguration : IEntityTypeConfiguration<CandidateProject>
    {
        public void Configure(EntityTypeBuilder<CandidateProject> builder)
        {
            builder.HasKey(p => p.ProjectId);
            builder.ToTable("CandidateProjects", "candidate");
        }
    }
}
