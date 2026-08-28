// IRAS.Infrastructure/Persistence/Configuration/Skills/SkillPlanStepConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class SkillPlanStepConfiguration : IEntityTypeConfiguration<SkillPlanStep>
    {
        public void Configure(EntityTypeBuilder<SkillPlanStep> builder)
        {
            builder.ToTable("SkillPlanSteps", "skills");

            builder.HasKey(s => s.StepId);

            builder.HasOne(s => s.Plan).WithMany(p => p.Steps)
                .HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
