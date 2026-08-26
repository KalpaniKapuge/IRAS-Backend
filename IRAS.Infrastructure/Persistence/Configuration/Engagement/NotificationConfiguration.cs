// IRAS.Infrastructure/Persistence/Configuration/Engagement/NotificationConfiguration.cs
using IRAS.Domain.Entities.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Engagement
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications", "engagement");
        }
    }
}
