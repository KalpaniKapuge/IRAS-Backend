// IRAS.Infrastructure/Persistence/Configuration/Candidate/WorkExperienceConfiguration.cs
using IRAS.Domain.Entities.Candidate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Candidate
{
    public class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
    {
        public void Configure(EntityTypeBuilder<WorkExperience> builder)
        {
            builder.ToTable("WorkExperiences", "candidate");

            builder.HasKey(w => w.ExperienceId);
        }
    }
}
