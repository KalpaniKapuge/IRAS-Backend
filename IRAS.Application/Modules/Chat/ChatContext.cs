// IRAS.Application/Modules/Chat/ChatContext.cs
using IRAS.Application.Modules.Admin.DTOs;
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Application.Modules.Interviews.DTOs;
using IRAS.Application.Modules.Jobs.DTOs;
using IRAS.Application.Modules.Matching.DTOs;
using IRAS.Application.Modules.SkillGaps.DTOs;
using IRAS.Application.Common.Audit;

namespace IRAS.Application.Modules.Chat
{
    public record KnowledgeBaseItem(string Title, string Content);

    public record CandidateChatData(
        IReadOnlyList<SkillGapSummaryDto> SkillGaps,
        IReadOnlyList<ApplicationDto> RecentApplications,
        IReadOnlyList<JobMatchDto> JobMatches);

    public record EmployerChatData(
        IReadOnlyList<JobSummaryDto> MyJobs,
        IReadOnlyList<InterviewDto> UpcomingInterviews);

    public record AdminChatData(
        DashboardStatsDto SystemStats,
        IReadOnlyList<AuditLogDto> RecentAuditEntries,
        int PendingEvidenceReviewCount);

    // Everything a responder needs to answer, pre-fetched by ChatService via one
    // IChatContextBuilder per role (ContextBuilders/) by reusing the existing
    // per-module services — the chatbot is an integration layer over what the rest
    // of the app already exposes, not a new data source. Only the slot matching
    // Role is ever populated; the other two are null.
    public record ChatContext(
        string Role,
        IReadOnlyList<KnowledgeBaseItem> KnowledgeBase,
        int UnreadNotificationCount,
        CandidateChatData? Candidate,
        EmployerChatData? Employer,
        AdminChatData? Admin);

    public record ChatReply(string Text, string? Intent);
}
