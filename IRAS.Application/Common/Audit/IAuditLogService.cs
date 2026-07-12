// IRAS.Application/Common/Audit/IAuditLogService.cs
namespace IRAS.Application.Common.Audit
{
    // Scope: this records *administrative* actions (skill taxonomy edits, knowledge-base
    // edits, user activation, job moderation) for accountability — the "Audit Logs" item
    // in the Admin workflow. It deliberately does not log every mutation in the system
    // (job publishing, applying, status changes are ordinary candidate/employer activity,
    // not admin actions requiring an accountability trail).
    public interface IAuditLogService
    {
        Task LogAsync(int userId, string action, string entityType, int entityId, CancellationToken ct);

        Task<List<AuditLogDto>> GetRecentAsync(int take, CancellationToken ct);
    }
}
