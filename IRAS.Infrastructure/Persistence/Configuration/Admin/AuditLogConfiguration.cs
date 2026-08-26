// IRAS.Infrastructure/Persistence/Configuration/Admin/AuditLogConfiguration.cs
using IRAS.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRAS.Infrastructure.Persistence.Configuration.Admin
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs", "admin");

            builder.HasKey(a => a.LogId);
        }
    }
}
