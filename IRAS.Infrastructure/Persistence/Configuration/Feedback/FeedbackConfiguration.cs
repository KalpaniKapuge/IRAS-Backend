// IRAS.Infrastructure/Persistence/Configuration/Feedback/FeedbackConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FeedbackEntity = IRAS.Domain.Entities.Feedback.Feedback;

namespace IRAS.Infrastructure.Persistence.Configuration.Feedback
{
    public class FeedbackConfiguration : IEntityTypeConfiguration<FeedbackEntity>
    {
        public void Configure(EntityTypeBuilder<FeedbackEntity> builder)
        {
            builder.ToTable("Feedbacks", "feedback");

            builder.HasOne(f => f.ApprovedByUser).WithMany()
                .HasForeignKey(f => f.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
