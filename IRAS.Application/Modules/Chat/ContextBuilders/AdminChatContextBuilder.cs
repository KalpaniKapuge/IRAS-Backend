// IRAS.Application/Modules/Chat/ContextBuilders/AdminChatContextBuilder.cs
using IRAS.Application.Common.Audit;
using IRAS.Application.Modules.Admin;
using IRAS.Application.Modules.SkillImprovementPlans;

namespace IRAS.Application.Modules.Chat.ContextBuilders
{
    public class AdminChatContextBuilder : IChatContextBuilder
    {
        public string Role => "Admin";

        private readonly IReportingService _reporting;
        private readonly IAuditLogService _auditLog;
        private readonly ISkillPlanEvidenceService _evidence;

        public AdminChatContextBuilder(IReportingService reporting, IAuditLogService auditLog, ISkillPlanEvidenceService evidence)
        {
            _reporting = reporting;
            _auditLog = auditLog;
            _evidence = evidence;
        }

        public async Task<ChatContext> BuildAsync(int userId, IReadOnlyList<KnowledgeBaseItem> kb, int unreadCount, CancellationToken ct)
        {
            var stats = await _reporting.GetDashboardAsync(ct);
            var recentAudit = await _auditLog.GetRecentAsync(10, ct);
            var pendingEvidence = await _evidence.GetEvidenceForReviewAsync(null, ct);

            return new ChatContext(
                Role, kb, unreadCount,
                Candidate: null,
                Employer: null,
                Admin: new AdminChatData(stats, recentAudit, pendingEvidence.Count));
        }
    }
}
