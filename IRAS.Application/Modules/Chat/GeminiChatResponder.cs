// IRAS.Application/Modules/Chat/GeminiChatResponder.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Modules.Chat
{
    // Real LLM-backed chat responder (Google Gemini API — same free-tier REST approach as
    // GeminiJdGenerator; no official Google-maintained C# SDK exists for this endpoint).
    //
    // The off-topic refusal requirement does NOT rely on the model's own judgment: every
    // message passes through ChatScopeGate first — the same deterministic domain-vocabulary
    // gate RuleBasedChatResponder uses. A message that fails the gate is refused immediately
    // and Gemini is never called for it. That means "can't ask how to cook rice" is
    // guaranteed by code, not by prompting — the LLM is only ever invoked for messages
    // already confirmed on-topic, where it earns its cost by giving richer, more natural
    // answers than the rule-based responder's fixed templates (e.g. it can synthesize one
    // answer that spans both skill gaps and job matches for a single compound question).
    public class GeminiChatResponder : IChatResponder
    {
        public string Name => "Gemini";
        public bool IsAi => true;

        // Each role gets its own persona/scope so the assistant genuinely only discusses
        // that role's domain — this is enforced both here (what the model is told it may
        // discuss) and, before the model is ever called, by ChatScopeGate's per-role
        // vocabulary check (a wrong-domain question never reaches Gemini at all).
        private const string SharedRules = """

            DATA:
            Below is a "Current user context" block containing the ONLY facts you know about
            this specific user. Never invent, guess, or assume a fact that is not explicitly
            present in that context. If it doesn't contain something the user asks about, say
            so honestly instead of making it up.

            STYLE:
            Be concise, friendly, and clear. Plain conversational text — short paragraphs or a
            few bullet points at most. No Markdown headers.
            """;

        private const string CandidateSystemPrompt = """
            You are the IRAS Assistant for a CANDIDATE, speaking to an already-authenticated
            job seeker on the Intelligent Recruitment Automation System (IRAS).

            SCOPE:
            You may ONLY discuss: the candidate's own resume/CV, profile, job applications,
            skill gaps, job matches, notifications, and how any part of IRAS works from a
            candidate's point of view (resume parsing, scoring, matching, feedback, applying
            to jobs). If asked about anything else — including employer or admin features not
            relevant to a candidate — politely decline and explain you can only help with a
            candidate's own recruitment activity on this platform. Do not answer the
            off-topic part of the question even briefly.
            """ + SharedRules;

        private const string EmployerSystemPrompt = """
            You are the IRAS Assistant for an EMPLOYER, speaking to an already-authenticated
            hiring account on the Intelligent Recruitment Automation System (IRAS).

            SCOPE:
            You may ONLY discuss: the employer's own job postings, applicants, interview
            scheduling, applicant scoring/ranking, and how any part of IRAS works from an
            employer's point of view (posting jobs, generating descriptions, reviewing
            applicants, scheduling interviews). You do NOT have access to any candidate's
            personal data (their skill gaps, other applications, etc.) beyond what's visible
            in this employer's own applicant lists — if asked for that, explain it's scoped to
            candidate accounts and point them to their applicant list instead. If asked about
            anything else — including candidate-only or admin-only features — politely decline
            and explain you can only help with this employer's own hiring activity on this
            platform. Do not answer the off-topic part of the question even briefly.
            """ + SharedRules;

        private const string AdminSystemPrompt = """
            You are the IRAS Assistant for an ADMINISTRATOR, speaking to an already-
            authenticated platform admin on the Intelligent Recruitment Automation System
            (IRAS).

            SCOPE:
            You may ONLY discuss: platform-wide statistics and dashboards, user management,
            audit logs, skill-plan evidence review, knowledge base management, and how any
            part of IRAS works from an administrative point of view. You do NOT have access to
            any individual candidate's or employer's private application data beyond what is
            summarized in the platform statistics you're given. If asked about anything else —
            including candidate-only or employer-only workflows — politely decline and explain
            you can only help with platform administration. Do not answer the off-topic part
            of the question even briefly.
            """ + SharedRules;

        private static string SystemPromptFor(string role) => role switch
        {
            "Employer" => EmployerSystemPrompt,
            "Admin" => AdminSystemPrompt,
            _ => CandidateSystemPrompt,
        };

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiChatResponder> _logger;

        public GeminiChatResponder(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiChatResponder> logger)
        {
            _options = options.Value;
            var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey)
                ? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                : _options.ApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "No Gemini API key configured. Set Gemini:ApiKey via user-secrets, " +
                    "or the GEMINI_API_KEY environment variable.");

            _http = http;
            _http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            _logger = logger;
        }

        public async Task<ChatReply> RespondAsync(string message, ChatContext context, CancellationToken ct)
        {
            var tokens = ChatScopeGate.Tokenize(message);

            if (ChatScopeGate.IsGreeting(tokens))
                return new ChatReply(ChatScopeGate.GreetingMessage, "Greeting");

            if (ChatScopeGate.IsAcknowledgement(tokens))
                return new ChatReply(ChatScopeGate.AcknowledgementMessage, "Acknowledgement");

            if (ChatScopeGate.IsCapabilitiesQuery(tokens, message))
                return new ChatReply(BuildCapabilitiesMessage(context), "Capabilities");

            // Hard safety net — see the class comment. Gemini is never reached for this case.
            if (!ChatScopeGate.IsInScope(tokens, context.Role))
                return new ChatReply(ChatScopeGate.OutOfScopeMessage, "OutOfScope");

            var userPrompt = BuildUserPrompt(message, context);

            var requestBody = new GeminiRequest(
                _options.Model,
                SystemPromptFor(context.Role),
                userPrompt,
                new GeminiGenerationConfig(1024, "minimal"));

            GeminiResponse? result;
            try
            {
                var httpResponse = await _http.PostAsJsonAsync("/v1beta/interactions", requestBody, JsonOpts, ct);
                httpResponse.EnsureSuccessStatusCode();
                result = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Gemini chat call failed");
                return new ChatReply(
                    "I'm having trouble reaching the assistant service right now — please try again in a moment.",
                    "Error");
            }

            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Gemini chat call returned no text content (status={Status})", result?.Status ?? "null");
                return new ChatReply(
                    "I'm having trouble reaching the assistant service right now — please try again in a moment.",
                    "Error");
            }

            return new ChatReply(text.Trim(), "Gemini");
        }

        private static string BuildCapabilitiesMessage(ChatContext context) => context.Role switch
        {
            "Employer" =>
                "I can help with: your job postings, applicants, applicant scoring, interview scheduling, " +
                "and how posting and hiring works on IRAS.",
            "Admin" =>
                "I can help with: platform statistics, user management, audit logs, pending skill-plan " +
                "evidence reviews, and how administration works on IRAS.",
            _ =>
                "I can help with: how to upload and parse a resume, what skill gaps mean, your application " +
                "statuses, job matches, and how application scoring works on IRAS.",
        };

        private static string BuildUserPrompt(string message, ChatContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Current user context:");
            sb.AppendLine($"Role: {context.Role}");
            sb.AppendLine();

            if (context.Candidate is { } candidate)
            {
                sb.AppendLine("Skill gaps:");
                if (candidate.SkillGaps.Count == 0)
                    sb.AppendLine("(none recorded yet)");
                else
                    foreach (var g in candidate.SkillGaps.OrderByDescending(g => g.MustHaveCount).ThenByDescending(g => g.TotalOccurrences).Take(10))
                        sb.AppendLine($"- {g.SkillName}: must-have in {g.MustHaveCount} application(s), nice-to-have in {g.NiceToHaveCount} application(s)");
                sb.AppendLine();

                sb.AppendLine("Recent applications:");
                if (candidate.RecentApplications.Count == 0)
                    sb.AppendLine("(none yet)");
                else
                    foreach (var a in candidate.RecentApplications.Take(5))
                        sb.AppendLine($"- {a.JobTitle}{(a.CompanyName != null ? $" at {a.CompanyName}" : "")}: status {a.Status}, score {a.TotalScore:P0}");
                sb.AppendLine();

                sb.AppendLine("Job matches:");
                if (candidate.JobMatches.Count == 0)
                    sb.AppendLine("(none yet)");
                else
                    foreach (var m in candidate.JobMatches.Take(5))
                        sb.AppendLine($"- {m.JobTitle}{(m.CompanyName != null ? $" at {m.CompanyName}" : "")}: {m.MatchScore:P0} match");
                sb.AppendLine();
            }

            if (context.Employer is { } employer)
            {
                sb.AppendLine("Your job postings:");
                if (employer.MyJobs.Count == 0)
                    sb.AppendLine("(none yet)");
                else
                    foreach (var j in employer.MyJobs.Take(10))
                        sb.AppendLine($"- {j.Title} ({j.Status}): {j.ApplicationCount} applicant(s)");
                sb.AppendLine();

                sb.AppendLine("Upcoming interviews:");
                if (employer.UpcomingInterviews.Count == 0)
                    sb.AppendLine("(none scheduled)");
                else
                    foreach (var i in employer.UpcomingInterviews.Take(5))
                        sb.AppendLine($"- {i.CandidateName} for {i.JobTitle}: {i.ScheduledAt:g} ({i.Mode}, {i.Status})");
                sb.AppendLine();
            }

            if (context.Admin is { } admin)
            {
                var s = admin.SystemStats;
                sb.AppendLine("Platform statistics:");
                sb.AppendLine($"- Candidates: {s.TotalCandidates}, Employers: {s.TotalEmployers}");
                sb.AppendLine($"- Jobs: {s.TotalJobs} total, {s.PublishedJobs} published");
                sb.AppendLine($"- Applications: {s.TotalApplications} total, average score {s.AverageApplicationScore:P0}");
                sb.AppendLine($"- Resumes: {s.TotalResumes} total, {s.ParsedResumes} parsed, {s.FailedResumes} failed");
                sb.AppendLine($"- Pending feedback reviews: {s.PendingFeedbackReviews}");
                sb.AppendLine($"- Pending skill-plan evidence reviews: {admin.PendingEvidenceReviewCount}");
                sb.AppendLine();

                sb.AppendLine("Recent admin activity:");
                if (admin.RecentAuditEntries.Count == 0)
                    sb.AppendLine("(none recorded)");
                else
                    foreach (var a in admin.RecentAuditEntries.Take(5))
                        sb.AppendLine($"- {a.Action} on {a.EntityType} by {a.UserEmail ?? "unknown"} at {a.CreatedAt:g}");
                sb.AppendLine();
            }

            sb.AppendLine($"Unread notifications: {context.UnreadNotificationCount}");
            sb.AppendLine();

            sb.AppendLine("Knowledge base articles:");
            if (context.KnowledgeBase.Count == 0)
                sb.AppendLine("(none available)");
            else
                foreach (var kb in context.KnowledgeBase)
                {
                    sb.AppendLine($"- Title: {kb.Title}");
                    sb.AppendLine($"  Content: {kb.Content}");
                }
            sb.AppendLine();

            sb.AppendLine($"User's message: \"{message}\"");
            sb.AppendLine();
            sb.AppendLine("Respond to the user's message now, following the system instructions exactly.");

            return sb.ToString();
        }

        // Mirrors Gemini's Interactions API JSON shape — see GeminiJdGenerator for the
        // same contract, confirmed live against the real endpoint.
        private record GeminiRequest(
            string Model,
            [property: JsonPropertyName("system_instruction")] string SystemInstruction,
            string Input,
            [property: JsonPropertyName("generation_config")] GeminiGenerationConfig GenerationConfig);

        private record GeminiGenerationConfig(
            [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens,
            [property: JsonPropertyName("thinking_level")] string ThinkingLevel);

        private record GeminiResponse(string Status, List<GeminiStep>? Steps);
        private record GeminiStep(List<GeminiContentPart>? Content);
        private record GeminiContentPart(string Type, string? Text);
    }
}
