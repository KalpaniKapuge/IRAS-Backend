// IRAS.Infrastructure/Persistence/Configuration/Assessments/AssessmentQuestionConfiguration.cs
using System.Text.Json;
using IRAS.Domain.Entities.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Assessments
{
    public class AssessmentQuestionConfiguration : IEntityTypeConfiguration<AssessmentQuestion>
    {
        public void Configure(EntityTypeBuilder<AssessmentQuestion> builder)
        {
            builder.ToTable("AssessmentQuestions", "assessments");

            builder.HasKey(q => q.AssessmentQuestionId);

            // Plain JSON-in-column ValueConverter, not EF Core's native JSON-column API —
            // simpler, more portable, and this is a small fixed 4-item list, not a query target.
            // The explicit ValueComparer tells EF how to detect changes on the List<string>
            // itself (by value, not reference) since a converted property loses its default one.
            builder.Property(q => q.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                    new ValueComparer<List<string>>(
                        (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                        v => v.ToList()))
                .HasColumnType("nvarchar(max)");

            builder.HasOne(q => q.JobAssessment).WithMany(a => a.Questions)
                .HasForeignKey(q => q.JobAssessmentId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(q => q.Skill).WithMany()
                .HasForeignKey(q => q.SkillId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
