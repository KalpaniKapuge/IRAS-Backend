// IRAS.Application/Modules/Chat/ContextBuilders/EmployerChatContextBuilder.cs
using IRAS.Application.Modules.Interviews;
using IRAS.Application.Modules.Jobs;

namespace IRAS.Application.Modules.Chat.ContextBuilders
{
    public class EmployerChatContextBuilder : IChatContextBuilder
    {
        public string Role => "Employer";

        private readonly IJobService _jobs;
        private readonly IInterviewService _interviews;

        public EmployerChatContextBuilder(IJobService jobs, IInterviewService interviews)
        {
            _jobs = jobs;
            _interviews = interviews;
        }

        public async Task<ChatContext> BuildAsync(int userId, IReadOnlyList<KnowledgeBaseItem> kb, int unreadCount, CancellationToken ct)
        {
            var myJobs = await _jobs.GetMyJobsAsync(userId);
            var upcomingInterviews = await _interviews.GetForEmployerAsync(userId, ct);

            return new ChatContext(
                Role, kb, unreadCount,
                Candidate: null,
                Employer: new EmployerChatData(myJobs, upcomingInterviews),
                Admin: null);
        }
    }
}
