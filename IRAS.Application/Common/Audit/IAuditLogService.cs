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
        // details is optional free text for actions that carry a human-written comment (e.g.
        // a skill-evidence rejection reason) — most actions have nothing to add here and pass
        // nothing, which is why every existing call site above still compiles unchanged.
        Task LogAsync(int userId, string action, string entityType, int entityId, CancellationToken ct, string? details = null);

        Task<List<AuditLogDto>> GetRecentAsync(int take, CancellationToken ct);
    }
}
