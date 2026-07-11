// IRAS.Application/Modules/Chat/ChatContext.cs
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Application.Modules.Matching.DTOs;
using IRAS.Application.Modules.SkillGaps.DTOs;

namespace IRAS.Application.Modules.Chat
{
    public record KnowledgeBaseItem(string Title, string Content);

    // Everything a responder needs to answer, pre-fetched by ChatService by reusing
    // the existing per-module services (skill gaps, applications, matches,
    // notifications) rather than re-querying the database directly — the chatbot is
    // an integration layer over what Modules 6-8 already expose, not a new data source.
    public record ChatContext(
        bool IsCandidate,
        IReadOnlyList<KnowledgeBaseItem> KnowledgeBase,
        IReadOnlyList<SkillGapSummaryDto> SkillGaps,
        IReadOnlyList<ApplicationDto> RecentApplications,
        IReadOnlyList<JobMatchDto> JobMatches,
        int UnreadNotificationCount);

    public record ChatReply(string Text, string? Intent);
}
