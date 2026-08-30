// IRAS.Infrastructure/Persistence/Configuration/Skills/SkillPlanEvidenceConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class SkillPlanEvidenceConfiguration : IEntityTypeConfiguration<SkillPlanEvidence>
    {
        public void Configure(EntityTypeBuilder<SkillPlanEvidence> builder)
        {
            builder.ToTable("SkillPlanEvidence", "skills");

            builder.HasKey(e => e.EvidenceId);

            builder.HasOne(e => e.Plan).WithMany(p => p.Evidence)
                .HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.VerifiedByUser).WithMany()
                .HasForeignKey(e => e.VerifiedBy).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
