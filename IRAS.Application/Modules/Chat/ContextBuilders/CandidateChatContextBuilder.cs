// IRAS.Application/Modules/Chat/ContextBuilders/CandidateChatContextBuilder.cs
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Matching;
using IRAS.Application.Modules.SkillGaps;

namespace IRAS.Application.Modules.Chat.ContextBuilders
{
    public class CandidateChatContextBuilder : IChatContextBuilder
    {
        public string Role => "Candidate";

        private readonly ISkillGapService _skillGaps;
        private readonly IApplicationService _applications;
        private readonly IJobMatchingService _matching;

        public CandidateChatContextBuilder(ISkillGapService skillGaps, IApplicationService applications, IJobMatchingService matching)
        {
            _skillGaps = skillGaps;
            _applications = applications;
            _matching = matching;
        }

        public async Task<ChatContext> BuildAsync(int userId, IReadOnlyList<KnowledgeBaseItem> kb, int unreadCount, CancellationToken ct)
        {
            var skillGaps = await _skillGaps.GetMyGapSummaryAsync(userId, ct);
            var applications = await _applications.GetMyApplicationsAsync(userId, ct);
            var matches = await _matching.GetMyMatchesAsync(userId, ct);

            return new ChatContext(
                Role, kb, unreadCount,
                Candidate: new CandidateChatData(skillGaps, applications, matches),
                Employer: null,
                Admin: null);
        }
    }
}
