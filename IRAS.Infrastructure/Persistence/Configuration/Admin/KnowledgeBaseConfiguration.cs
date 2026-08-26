// IRAS.Infrastructure/Persistence/Configuration/Admin/KnowledgeBaseConfiguration.cs
using IRAS.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Admin
{
    public class KnowledgeBaseConfiguration : IEntityTypeConfiguration<KnowledgeBase>
    {
        public void Configure(EntityTypeBuilder<KnowledgeBase> builder)
        {
            builder.ToTable("KnowledgeBases", "admin");

            builder.HasKey(k => k.KbId);

            builder.HasOne(k => k.UpdatedByUser).WithMany()
                .HasForeignKey(k => k.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
