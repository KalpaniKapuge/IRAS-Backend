// IRAS.Application/Modules/Chat/RuleBasedChatResponder.cs
using System.Text;
using System.Text.RegularExpressions;

namespace IRAS.Application.Modules.Chat
{
    // Deterministic, keyword/intent-based responder — the baseline behind IChatResponder.
    // Two jobs, in order: (1) refuse anything outside the recruitment-platform domain,
    // (2) for in-scope messages, answer from the candidate's own data or the knowledge
    // base. Intentionally simple and auditable — precision over recall, same philosophy
    // as the AI service's resume skill-detector (a false "I can help with that" on an
    // off-topic question is worse than an occasional unnecessary refusal).
    public class RuleBasedChatResponder : IChatResponder
    {
        public string Name => "RuleBased";
        public bool IsAi => false;

        private static readonly Regex WordPattern = new(@"[a-zA-Z']+", RegexOptions.Compiled);

        // The allow-list that gates whether a message is even considered. Deliberately
        // curated and static rather than derived from free text, so the gate can't be
        // widened by accident (e.g. by knowledge-base prose containing common words).
        private static readonly HashSet<string> DomainVocabulary = new(StringComparer.OrdinalIgnoreCase)
        {
            "resume", "resumes", "cv", "upload", "uploaded", "parse", "parsed", "parsing",
            "skill", "skills", "gap", "gaps", "missing",
            "application", "applications", "apply", "applied", "applying",
            "job", "jobs", "vacancy", "vacancies", "position", "positions", "role", "roles",
            "employer", "employers", "company", "companies",
            "candidate", "candidates", "profile", "profiles", "account",
            "match", "matches", "matching", "matched",
            "score", "scores", "scoring", "rank", "ranking", "ranked",
            "interview", "interviews", "shortlist", "shortlisted",
            "feedback", "reject", "rejected", "rejection", "hire", "hired", "hiring",
            "notification", "notifications", "unread", "status", "update", "updates", "progress",
            "register", "registration", "login", "password", "email",
            "certification", "certifications", "education", "experience", "qualification", "qualifications",
            "chatbot", "chat", "assistant", "platform", "system", "iras",
            "recruitment", "recruiting", "recruiter",
            "requirement", "requirements", "qualify", "qualified", "suitable", "fit",
            "learn", "improve", "improvement", "advice", "recommend", "recommendation", "help"
        };

        private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "is", "are", "was", "were", "do", "does", "did", "how", "what",
            "when", "where", "why", "who", "which", "i", "my", "me", "to", "for", "of", "in",
            "on", "at", "and", "or", "you", "your", "can", "could", "will", "would", "should",
            "it", "its", "this", "that", "these", "those", "be", "have", "has", "had", "with",
            "about", "get", "got", "am", "please"
        };

        private static readonly HashSet<string> GreetingWords = new(StringComparer.OrdinalIgnoreCase)
            { "hi", "hello", "hey" };

        private static readonly HashSet<string> AckWords = new(StringComparer.OrdinalIgnoreCase)
            { "thanks", "thank", "thx", "ok", "okay", "cool", "great", "nice", "awesome" };

        public Task<ChatReply> RespondAsync(string message, ChatContext context, CancellationToken ct)
        {
            var tokens = Tokenize(message);

            if (tokens.Count <= 4 && tokens.Overlaps(GreetingWords))
                return Task.FromResult(new ChatReply(
                    "Hi! I'm the IRAS assistant. I can help with your resume, applications, skill gaps, " +
                    "job matches, and how this platform works — what would you like to know?",
                    "Greeting"));

            if (tokens.Count <= 4 && tokens.Overlaps(AckWords))
                return Task.FromResult(new ChatReply(
                    "You're welcome! Anything else about your resume, applications, or this platform I can help with?",
                    "Acknowledgement"));

            var isCapabilitiesQuery = (tokens.Contains("help") && tokens.Count <= 3)
                || message.Contains("what can you do", StringComparison.OrdinalIgnoreCase);
            if (isCapabilitiesQuery)
                return Task.FromResult(new ChatReply(BuildCapabilitiesMessage(context), "Capabilities"));

            if (!tokens.Overlaps(DomainVocabulary))
                return Task.FromResult(new ChatReply(
                    "I can only help with questions about this recruitment platform — your resume, " +
                    "applications, skill gaps, job matches, or how the system works. I'm not able to help " +
                    "with anything outside that. Try asking about your profile, applications, or skills instead.",
                    "OutOfScope"));

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
                var titleTokens = Tokenize(entry.Title);
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

        private static HashSet<string> Tokenize(string text)
        {
            return WordPattern.Matches(text)
                .Select(m => m.Value.ToLowerInvariant())
                .Where(w => w.Length > 1 && !Stopwords.Contains(w))
                .ToHashSet();
        }
    }
}
