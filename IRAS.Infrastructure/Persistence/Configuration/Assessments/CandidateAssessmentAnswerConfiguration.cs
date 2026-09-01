// IRAS.Infrastructure/Persistence/Configuration/Assessments/CandidateAssessmentAnswerConfiguration.cs
using IRAS.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Assessments
{
    public class CandidateAssessmentAnswerConfiguration : IEntityTypeConfiguration<CandidateAssessmentAnswer>
    {
        public void Configure(EntityTypeBuilder<CandidateAssessmentAnswer> builder)
        {
            builder.ToTable("CandidateAssessmentAnswers", "assessments");

            builder.HasKey(a => a.AnswerId);

            builder.Property(a => a.ScoreFraction).HasColumnType("decimal(5,4)");

            builder.HasOne(a => a.Attempt).WithMany(t => t.Answers)
                .HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade — Attempt already cascades into this table; a second
            // cascade path via Question would trip SQL Server's multiple-cascade-paths rule.
            builder.HasOne(a => a.Question).WithMany()
                .HasForeignKey(a => a.AssessmentQuestionId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
