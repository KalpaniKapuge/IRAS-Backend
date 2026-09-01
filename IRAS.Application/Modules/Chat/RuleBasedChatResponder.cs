// IRAS.Application/Modules/Chat/RuleBasedChatResponder.cs
using System.Text;

namespace IRAS.Application.Modules.Chat
{
    public class RuleBasedChatResponder : IChatResponder
    {
        public string Name => "RuleBased";
        public bool IsAi => false;

        public Task<ChatReply> RespondAsync(string message, ChatContext context, CancellationToken ct)
        {
            var tokens = ChatScopeGate.Tokenize(message);

            if (ChatScopeGate.IsGreeting(tokens))
                return Task.FromResult(new ChatReply(ChatScopeGate.GreetingMessage, "Greeting"));

            if (ChatScopeGate.IsAcknowledgement(tokens))
                return Task.FromResult(new ChatReply(ChatScopeGate.AcknowledgementMessage, "Acknowledgement"));

            if (ChatScopeGate.IsCapabilitiesQuery(tokens, message))
                return Task.FromResult(new ChatReply(BuildCapabilitiesMessage(context), "Capabilities"));

            if (!ChatScopeGate.IsInScope(tokens, context.Role))
                return Task.FromResult(new ChatReply(ChatScopeGate.OutOfScopeMessage, "OutOfScope"));

            var best = BestKnowledgeBaseMatch(tokens, context.KnowledgeBase);
            if (best is not null)
                return Task.FromResult(new ChatReply(best.Content, $"KnowledgeBase:{best.Title}"));

            if (tokens.Contains("gap") || tokens.Contains("gaps")
                || (tokens.Overlaps(new[] { "skill", "skills" }) && tokens.Overlaps(new[] { "missing", "need", "learn", "improve" })))
            {
                return Task.FromResult(context.Candidate is { } candidateForGaps
                    ? new ChatReply(BuildSkillGapsMessage(candidateForGaps), "SkillGap")
                    : new ChatReply(
                        "Skill gaps are tracked per candidate application — as an employer, you can see a candidate's gaps from their entry in your job's applicant list.",
                        "SkillGap.NotApplicable"));
            }

            if (tokens.Overlaps(new[] { "application", "applications", "applied" }))
            {
                return Task.FromResult(BuildApplicationsReply(context));
            }

            if (tokens.Overlaps(new[] { "match", "matches", "matching", "matched" }))
            {
                return Task.FromResult(context.Candidate is { } candidateForMatches
                    ? new ChatReply(BuildMatchesMessage(candidateForMatches), "JobMatch")
                    : new ChatReply(
                        "Job matches are calculated per candidate — as an employer, matching runs automatically against opted-in candidates whenever you publish a job.",
                        "JobMatch.NotApplicable"));
            }

            if (tokens.Overlaps(new[] { "applicant", "applicants" }))
            {
                return Task.FromResult(context.Employer is { } employerForApplicants
                    ? new ChatReply(BuildApplicantsMessage(employerForApplicants), "Applicants")
                    : new ChatReply(
                        "Applicant lists are scoped to an employer's own job postings — open one of your jobs to review its applicants.",
                        "Applicants.NotApplicable"));
            }

            if (tokens.Overlaps(new[] { "user", "users" }) && context.Admin is { } employerForUsers)
            {
                return Task.FromResult(new ChatReply(
                    $"There are {employerForUsers.SystemStats.TotalCandidates} candidate account(s) and {employerForUsers.SystemStats.TotalEmployers} employer account(s) on the platform. Manage individual accounts from the Users admin page.",
                    "UserStats"));
            }

            if (tokens.Overlaps(new[] { "evidence", "review", "reviews" }) && context.Admin is { } adminForEvidence)
            {
                return Task.FromResult(new ChatReply(
                    $"There are {adminForEvidence.PendingEvidenceReviewCount} skill-plan evidence submission(s) waiting for review.",
                    "PendingEvidence"));
            }

            if (tokens.Overlaps(new[] { "audit", "log", "logs" }) && context.Admin is { } adminForAudit)
            {
                return Task.FromResult(new ChatReply(BuildAuditLogMessage(adminForAudit), "AuditLog"));
            }

            if (tokens.Overlaps(new[] { "dashboard", "statistic", "statistics", "report", "reports" }) && context.Admin is { } adminForStats)
            {
                return Task.FromResult(new ChatReply(BuildAdminStatsMessage(adminForStats), "SystemStats"));
            }

            if (tokens.Overlaps(new[] { "interview", "interviews" }) && context.Employer is { } employerForInterviews)
            {
                return Task.FromResult(new ChatReply(BuildInterviewsMessage(employerForInterviews), "Interviews"));
            }

            if (tokens.Overlaps(new[] { "notification", "notifications", "unread" }))
            {
                return Task.FromResult(new ChatReply(
                    $"You have {context.UnreadNotificationCount} unread notification(s). Check your notifications list for the full details.",
                    "Notification"));
            }

            return Task.FromResult(new ChatReply(
                "That's on-topic, but I don't have a specific answer for it yet. Try rephrasing around " +
                RoleTopicsHint(context.Role) + ".",
                "Unmatched"));
        }

        private static string RoleTopicsHint(string role) => role switch
        {
            "Employer" => "your job postings, applicants, or interview scheduling",
            "Admin" => "user accounts, audit logs, evidence reviews, or platform statistics",
            _ => "resume uploads and parsing, skill gaps, application status, job matches, or notifications",
        };

        private static string BuildCapabilitiesMessage(ChatContext context) => context.Role switch
        {
            "Employer" =>
                "I can help with: your job postings, applicants, applicant scoring, interview scheduling, " +
                "and how posting and hiring works on IRAS.",
            "Admin" =>
                "I can help with: platform statistics, user management, audit logs, pending skill-plan " +
                "evidence reviews, and how administration works on IRAS.",
            _ =>
                "I can help with: how to upload and parse a resume, what skill gaps mean, how application " +
                "scoring and job matching work, and general platform how-to questions. I can also look up " +
                "your own skill gaps, application statuses, job matches, and unread notifications.",
        };

        private static ChatReply BuildApplicationsReply(ChatContext context)
        {
            if (context.Candidate is { } candidate)
                return new ChatReply(BuildApplicationsMessage(candidate), "ApplicationStatus");

            if (context.Employer is { } employer)
                return new ChatReply(BuildApplicantsMessage(employer), "ApplicationStatus");

            return new ChatReply(
                "Application data is scoped to candidate and employer accounts — as an admin, see platform-wide application counts from the dashboard.",
                "ApplicationStatus.NotApplicable");
        }

        private static string BuildSkillGapsMessage(CandidateChatData candidate)
        {
            if (candidate.SkillGaps.Count == 0)
                return "You don't have any recorded skill gaps yet — apply to a job and I'll be able to tell you what would strengthen your profile.";

            var top = candidate.SkillGaps.OrderByDescending(g => g.MustHaveCount).ThenByDescending(g => g.TotalOccurrences).Take(5);
            var lines = top.Select(g => g.MustHaveCount > 0
                ? $"- {g.SkillName}: required (must-have) in {g.MustHaveCount} application(s)"
                : $"- {g.SkillName}: would help (nice-to-have) in {g.NiceToHaveCount} application(s)");
            return "Skills that would most improve your applications:\n" + string.Join("\n", lines);
        }

        private static string BuildApplicationsMessage(CandidateChatData candidate)
        {
            if (candidate.RecentApplications.Count == 0)
                return "You haven't applied to any jobs yet — browse published jobs and apply to get started.";

            var lines = candidate.RecentApplications.Take(5)
                .Select(a => $"- {a.JobTitle}{(a.CompanyName != null ? $" at {a.CompanyName}" : "")}: {a.Status} (score {a.TotalScore:P0})");
            return "Your most recent applications:\n" + string.Join("\n", lines);
        }

        private static string BuildMatchesMessage(CandidateChatData candidate)
        {
            if (candidate.JobMatches.Count == 0)
                return "No automatic matches yet. Make sure matching is turned on in your profile and you have a parsed resume — matching runs whenever an employer publishes a new job.";

            var lines = candidate.JobMatches.Take(5)
                .Select(m => $"- {m.JobTitle}{(m.CompanyName != null ? $" at {m.CompanyName}" : "")}: {m.MatchScore:P0} match");
            return "You've been automatically matched to:\n" + string.Join("\n", lines);
        }

        private static string BuildApplicantsMessage(EmployerChatData employer)
        {
            if (employer.MyJobs.Count == 0)
                return "You haven't posted any jobs yet — create a job posting to start receiving applicants.";

            var totalApplicants = employer.MyJobs.Sum(j => j.ApplicationCount);
            var lines = employer.MyJobs.Take(5)
                .Select(j => $"- {j.Title} ({j.Status}): {j.ApplicationCount} applicant(s)");
            return $"You have {totalApplicants} applicant(s) across {employer.MyJobs.Count} job posting(s):\n" + string.Join("\n", lines);
        }

        private static string BuildInterviewsMessage(EmployerChatData employer)
        {
            if (employer.UpcomingInterviews.Count == 0)
                return "You don't have any interviews scheduled right now.";

            var lines = employer.UpcomingInterviews.Take(5)
                .Select(i => $"- {i.CandidateName} for {i.JobTitle}: {i.ScheduledAt:g} ({i.Mode})");
            return "Your upcoming interviews:\n" + string.Join("\n", lines);
        }

        private static string BuildAdminStatsMessage(AdminChatData admin)
        {
            var s = admin.SystemStats;
            return "Platform snapshot:\n" +
                $"- Candidates: {s.TotalCandidates}, Employers: {s.TotalEmployers}\n" +
                $"- Jobs: {s.TotalJobs} total, {s.PublishedJobs} published\n" +
                $"- Applications: {s.TotalApplications} total, average score {s.AverageApplicationScore:P0}\n" +
                $"- Pending skill-plan evidence reviews: {admin.PendingEvidenceReviewCount}";
        }

        private static string BuildAuditLogMessage(AdminChatData admin)
        {
            if (admin.RecentAuditEntries.Count == 0)
                return "No administrative actions have been recorded yet.";

            var lines = admin.RecentAuditEntries.Take(5)
                .Select(a => $"- {a.Action} on {a.EntityType} by {a.UserEmail ?? "unknown"} at {a.CreatedAt:g}");
            return "Recent admin activity:\n" + string.Join("\n", lines);
        }

        private static KnowledgeBaseItem? BestKnowledgeBaseMatch(HashSet<string> messageTokens, IReadOnlyList<KnowledgeBaseItem> kb)
        {
            KnowledgeBaseItem? best = null;
            var bestOverlap = 0;

            foreach (var entry in kb)
            {
                var titleTokens = ChatScopeGate.Tokenize(entry.Title);
                if (titleTokens.Count == 0) continue;

                var overlap = titleTokens.Count(t => messageTokens.Contains(t));
                var fraction = (double)overlap / titleTokens.Count;

                if (overlap >= 2 && fraction >= 0.6 && overlap > bestOverlap)
                {
                    best = entry;
                    bestOverlap = overlap;
                }
            }

            return best;
        }
    }
}
