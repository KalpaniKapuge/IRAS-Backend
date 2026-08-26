// IRAS.Infrastructure/Persistence/Configuration/Engagement/ChatConversationConfiguration.cs
using IRAS.Domain.Entities.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Engagement
{
    public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
    {
        public void Configure(EntityTypeBuilder<ChatConversation> builder)
        {
            builder.ToTable("ChatConversations", "engagement");

            builder.HasKey(c => c.ConversationId);
        }
    }
}
