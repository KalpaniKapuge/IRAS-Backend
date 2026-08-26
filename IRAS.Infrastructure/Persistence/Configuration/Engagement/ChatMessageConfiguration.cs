// IRAS.Infrastructure/Persistence/Configuration/Engagement/ChatMessageConfiguration.cs
using IRAS.Domain.Entities.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Engagement
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages", "engagement");

            builder.HasKey(m => m.MessageId);
        }
    }
}
