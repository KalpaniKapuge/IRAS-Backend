// IRAS.Infrastructure/Persistence/Configuration/Skills/SkillImprovementPlanConfiguration.cs
using IRAS.Domain.Entities.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Skills
{
    public class SkillImprovementPlanConfiguration : IEntityTypeConfiguration<SkillImprovementPlan>
    {
        public void Configure(EntityTypeBuilder<SkillImprovementPlan> builder)
        {
            builder.ToTable("SkillImprovementPlans", "skills");

            builder.HasKey(p => p.PlanId);

            // One active plan per candidate per skill — regenerating returns the existing
            // plan rather than creating a duplicate (see SkillImprovementPlanService).
            builder.HasIndex(p => new { p.CandidateId, p.SkillId }).IsUnique();

            builder.HasOne(p => p.Candidate).WithMany(c => c.ImprovementPlans)
                .HasForeignKey(p => p.CandidateId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Skill).WithMany()
                .HasForeignKey(p => p.SkillId).OnDelete(DeleteBehavior.Restrict);

            // Restrict, not SetNull — SQL Server rejects SetNull here as a multiple-cascade-path
            // conflict against the CandidateId->CandidateProfiles cascade (both ultimately trace
            // back to Users). Same fix already used for Job FKs on Application/JobMatch.
            builder.HasOne(p => p.Job).WithMany()
                .HasForeignKey(p => p.JobId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
