// IRAS.Application/Common/Audit/AuditLogDto.cs
namespace IRAS.Application.Common.Audit
{
    public class AuditLogDto
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
