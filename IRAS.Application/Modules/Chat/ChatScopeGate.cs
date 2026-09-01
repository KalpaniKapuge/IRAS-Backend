// IRAS.Application/Modules/Chat/ChatScopeGate.cs
using System.Text.RegularExpressions;

namespace IRAS.Application.Modules.Chat
{
    // Shared, deterministic message classification used by every IChatResponder
    // implementation — rule-based and LLM-backed alike. Centralizing this here means the
    // "must refuse off-topic questions" guarantee is enforced identically by code
    // regardless of which responder is active — it never depends on an LLM being
    // prompted correctly. See GeminiChatResponder for how this is used as a hard gate
    // in front of the real API call.
    public static class ChatScopeGate
    {
        private static readonly Regex WordPattern = new(@"[a-zA-Z']+", RegexOptions.Compiled);

        // Deliberately curated and static rather than derived from free text, so the gate
        // can't be widened by accident (e.g. by knowledge-base prose containing common words).
        // Baseline vocabulary every role can use — account/platform basics plus enough
        // shared recruitment terms (job, application, score, interview) that all three
        // roles legitimately discuss, just from different angles. Role-specific scoring
        // (IsInScope) layers each role's own vocabulary on top of this.
        private static readonly HashSet<string> DomainVocabulary = new(StringComparer.OrdinalIgnoreCase)
        {
            "application", "applications", "apply", "applied", "applying",
            "job", "jobs", "vacancy", "vacancies", "position", "positions", "role", "roles",
            "employer", "employers", "company", "companies",
            "candidate", "candidates", "profile", "profiles", "account",
            "score", "scores", "scoring", "rank", "ranking", "ranked",
            "interview", "interviews",
            "notification", "notifications", "unread", "status", "update", "updates", "progress",
            "register", "registration", "login", "password", "email",
            "chatbot", "chat", "assistant", "platform", "system", "iras",
            "recruitment", "recruiting", "recruiter",
            "requirement", "requirements", "qualify", "qualified", "suitable", "fit",
            "help"
        };

        // Candidate-only vocabulary: resume/CV, skill gaps, learning, and the outcomes of
        // their own applications.
        private static readonly HashSet<string> CandidateVocabulary = new(StringComparer.OrdinalIgnoreCase)
        {
            "resume", "resumes", "cv", "upload", "uploaded", "parse", "parsed", "parsing",
            "skill", "skills", "gap", "gaps", "missing",
            "match", "matches", "matching", "matched",
            "feedback", "reject", "rejected", "rejection", "hire", "hired", "hiring",
            "certification", "certifications", "education", "experience", "qualification", "qualifications",
            "learn", "improve", "improvement", "advice", "recommend", "recommendation"
        };

        // Employer-only vocabulary: posting/managing jobs and reviewing applicants.
        private static readonly HashSet<string> EmployerVocabulary = new(StringComparer.OrdinalIgnoreCase)
        {
            "post", "posting", "postings", "publish", "published", "draft", "close", "closed",
            "applicant", "applicants", "shortlist", "shortlisted",
            "hire", "hired", "hiring", "reject", "rejected", "rejection", "feedback",
            "description", "template", "generate", "generated",
            "skill", "skills", "required", "match", "matches", "matching"
        };

        // Admin-only vocabulary: platform administration, moderation, and oversight.
        private static readonly HashSet<string> AdminVocabulary = new(StringComparer.OrdinalIgnoreCase)
        {
            "user", "users", "activate", "deactivate", "moderation", "moderate",
            "audit", "log", "logs", "evidence", "review", "reviews", "verify", "verified",
            "verification", "approve", "approved", "knowledgebase", "article", "articles",
            "report", "reports", "reporting", "statistic", "statistics", "dashboard",
            "taxonomy", "resource", "resources"
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

        public const string OutOfScopeMessage =
            "I can only help with questions about this recruitment platform — your resume, " +
            "applications, skill gaps, job matches, or how the system works. I'm not able to help " +
            "with anything outside that. Try asking about your profile, applications, or skills instead.";

        public const string GreetingMessage =
            "Hi! I'm the IRAS assistant. I can help with your resume, applications, skill gaps, " +
            "job matches, and how this platform works — what would you like to know?";

        public const string AcknowledgementMessage =
            "You're welcome! Anything else about your resume, applications, or this platform I can help with?";

        public static HashSet<string> Tokenize(string text) => WordPattern.Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 1 && !Stopwords.Contains(w))
            .ToHashSet();

        public static bool IsGreeting(HashSet<string> tokens) => tokens.Count <= 4 && tokens.Overlaps(GreetingWords);

        public static bool IsAcknowledgement(HashSet<string> tokens) => tokens.Count <= 4 && tokens.Overlaps(AckWords);

        public static bool IsCapabilitiesQuery(HashSet<string> tokens, string rawMessage) =>
            (tokens.Contains("help") && tokens.Count <= 3)
            || rawMessage.Contains("what can you do", StringComparison.OrdinalIgnoreCase);

        // The hard safety net: anything that doesn't touch the domain vocabulary at all is
        // refused outright. This is what blocks "how do I cook rice?" — the same check
        // whether the responder behind it is rule-based or LLM-backed.
        private static readonly IReadOnlyDictionary<string, HashSet<string>> RoleVocabulary = new Dictionary<string, HashSet<string>>
        {
            ["Candidate"] = CandidateVocabulary,
            ["Employer"] = EmployerVocabulary,
            ["Admin"] = AdminVocabulary,
        };

        public static bool IsInScope(HashSet<string> tokens, string role) =>
            tokens.Overlaps(DomainVocabulary)
            || (RoleVocabulary.TryGetValue(role, out var roleVocab) && tokens.Overlaps(roleVocab));
    }
}
