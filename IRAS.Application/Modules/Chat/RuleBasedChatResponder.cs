// IRAS.Application/Modules/Chat/RuleBasedChatResponder.cs
using System.Text;

namespace IRAS.Application.Modules.Chat
{
    // Deterministic, keyword/intent-based responder — the zero-cost baseline behind
    // IChatResponder, and the comparison point for the thesis's evaluation chapter against
    // GeminiChatResponder. Scope gating (greeting/ack/capabilities/off-topic refusal) is
    // shared with the LLM-backed responder via ChatScopeGate — only the answering strategy
    // for in-scope messages differs here (fixed templates vs a real LLM call).
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

            if (!ChatScopeGate.IsInScope(tokens))
                return Task.FromResult(new ChatReply(ChatScopeGate.OutOfScopeMessage, "OutOfScope"));

            // ---- knowledge-base lookup (checked before the personal-data intents below) ----
            //
            // A curated FAQ title like "How is my application score calculated?" and the
            // personal-intent keyword net for ApplicationStatus both key off the word
            // "application" — without this ordering, a general mechanism question gets
            // misrouted to "here are your applications" just because it mentions the word.
            // The KB match is inherently more specific (near-exact title overlap), so it
            // gets first refusal; broad keyword-triggered personal intents are the fallback.

            var best = BestKnowledgeBaseMatch(tokens, context.KnowledgeBase);
            if (best is not null)
                return Task.FromResult(new ChatReply(best.Content, $"KnowledgeBase:{best.Title}"));

            // ---- personalized intents (checked in priority order; first match wins) ----

            if (tokens.Contains("gap") || tokens.Contains("gaps")
                || (tokens.Overlaps(new[] { "skill", "skills" }) && tokens.Overlaps(new[] { "missing", "need", "learn", "improve" })))
            {
                return Task.FromResult(context.IsCandidate
                    ? new ChatReply(BuildSkillGapsMessage(context), "SkillGap")
                    : new ChatReply(
                        "Skill gaps are tracked per candidate application — as an employer, you can see a candidate's gaps from their entry in your job's applicant list.",
                        "SkillGap.NotApplicable"));
            }

            if (tokens.Overlaps(new[] { "application", "applications", "applied" }))
            {
                return Task.FromResult(context.IsCandidate
                    ? new ChatReply(BuildApplicationsMessage(context), "ApplicationStatus")
                    : new ChatReply(
                        "I can only look up a candidate's own applications here — as an employer, view your applicants from your job's applicant list instead.",
                        "ApplicationStatus.NotApplicable"));
            }

            if (tokens.Overlaps(new[] { "match", "matches", "matching", "matched" }))
            {
                return Task.FromResult(context.IsCandidate
                    ? new ChatReply(BuildMatchesMessage(context), "JobMatch")
                    : new ChatReply(
                        "Job matches are calculated per candidate — as an employer, matching runs automatically against opted-in candidates whenever you publish a job.",
                        "JobMatch.NotApplicable"));
            }

            if (tokens.Overlaps(new[] { "notification", "notifications", "unread" }))
            {
                return Task.FromResult(new ChatReply(
                    $"You have {context.UnreadNotificationCount} unread notification(s). Check your notifications list for the full details.",
                    "Notification"));
            }

            return Task.FromResult(new ChatReply(
                "That's on-topic, but I don't have a specific answer for it yet. I can help with resume " +
                "uploads and parsing, skill gaps, application status, job matches, notifications, and how " +
                "scoring and job posting work — try rephrasing around one of those.",
                "Unmatched"));
        }

        private static string BuildCapabilitiesMessage(ChatContext context)
        {
            var sb = new StringBuilder("I can help with: how to upload and parse a resume, what skill gaps mean, ");
            sb.Append("how application scoring and job matching work, and general platform how-to questions.");
            if (context.IsCandidate)
                sb.Append(" As a candidate, I can also look up your own skill gaps, application statuses, job matches, and unread notifications.");
            return sb.ToString();
        }

        private static string BuildSkillGapsMessage(ChatContext context)
        {
            if (context.SkillGaps.Count == 0)
                return "You don't have any recorded skill gaps yet — apply to a job and I'll be able to tell you what would strengthen your profile.";

            var top = context.SkillGaps.OrderByDescending(g => g.MustHaveCount).ThenByDescending(g => g.TotalOccurrences).Take(5);
            var lines = top.Select(g => g.MustHaveCount > 0
                ? $"- {g.SkillName}: required (must-have) in {g.MustHaveCount} application(s)"
                : $"- {g.SkillName}: would help (nice-to-have) in {g.NiceToHaveCount} application(s)");
            return "Skills that would most improve your applications:\n" + string.Join("\n", lines);
        }

        private static string BuildApplicationsMessage(ChatContext context)
        {
            if (context.RecentApplications.Count == 0)
                return "You haven't applied to any jobs yet — browse published jobs and apply to get started.";

            var lines = context.RecentApplications.Take(5)
                .Select(a => $"- {a.JobTitle}{(a.CompanyName != null ? $" at {a.CompanyName}" : "")}: {a.Status} (score {a.TotalScore:P0})");
            return "Your most recent applications:\n" + string.Join("\n", lines);
        }

        private static string BuildMatchesMessage(ChatContext context)
        {
            if (context.JobMatches.Count == 0)
                return "No automatic matches yet. Make sure matching is turned on in your profile and you have a parsed resume — matching runs whenever an employer publishes a new job.";

            var lines = context.JobMatches.Take(5)
                .Select(m => $"- {m.JobTitle}{(m.CompanyName != null ? $" at {m.CompanyName}" : "")}: {m.MatchScore:P0} match");
            return "You've been automatically matched to:\n" + string.Join("\n", lines);
        }

        private static KnowledgeBaseItem? BestKnowledgeBaseMatch(HashSet<string> messageTokens, IReadOnlyList<KnowledgeBaseItem> kb)
        {
            // Scored against the title only, not content — a curated title is a short,
            // natural-language question, so overlap there is a genuine signal. Content is
            // long free-text prose; scoring against it caused false positives (e.g. an
            // unrelated FAQ whose explanatory paragraph happens to contain the word
            // "applications" would otherwise outscore the correct match).
            KnowledgeBaseItem? best = null;
            var bestOverlap = 0;

            foreach (var entry in kb)
            {
                var titleTokens = ChatScopeGate.Tokenize(entry.Title);
                if (titleTokens.Count == 0) continue;

                var overlap = titleTokens.Count(t => messageTokens.Contains(t));
                var fraction = (double)overlap / titleTokens.Count;

                // Require most of the title's distinctive words to be present, and at
                // least two of them — a single shared word (e.g. "skills") isn't enough
                // to confidently pick one FAQ entry over a personal-data intent.
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
