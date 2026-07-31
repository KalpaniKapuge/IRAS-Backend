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

        private const string SystemPrompt = """
            You are the IRAS Assistant, an AI chatbot built into the Intelligent Recruitment
            Automation System (IRAS), a recruitment platform. You are speaking directly to an
            already-authenticated user of this platform.

            SCOPE:
            You may ONLY discuss topics related to this platform: the user's own resume,
            profile, job applications, skill gaps, job matches, notifications, and how any
            part of the IRAS system works (resume parsing, scoring, matching, feedback, job
            posting, etc). If the user asks about anything else, politely decline and explain
            you can only help with things related to this recruitment platform — do not answer
            the off-topic part of the question even briefly.

            DATA:
            Below is a "Current user context" block containing the ONLY facts you know about
            this specific user. Never invent, guess, or assume a personal fact that is not
            explicitly present in that context. If it doesn't contain something the user asks
            about, say so honestly instead of making it up.

            If the context shows the user is not a candidate, personal data sections will be
            empty. If a non-candidate asks a candidate-specific personal question (their own
            skill gaps, applications, etc.), explain that this data is scoped to candidate
            accounts, and where relevant point them to the equivalent employer-side feature
            (e.g. viewing a candidate's gaps from their applicant list).

            STYLE:
            Be concise, friendly, and clear. Plain conversational text — short paragraphs or a
            few bullet points at most. No Markdown headers.
            """;

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
            if (!ChatScopeGate.IsInScope(tokens))
                return new ChatReply(ChatScopeGate.OutOfScopeMessage, "OutOfScope");

            var userPrompt = BuildUserPrompt(message, context);

            var requestBody = new GeminiRequest(
                _options.Model,
                SystemPrompt,
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

        private static string BuildCapabilitiesMessage(ChatContext context)
        {
            var sb = new StringBuilder("I can help with: how to upload and parse a resume, what skill gaps mean, ");
            sb.Append("how application scoring and job matching work, and general platform how-to questions.");
            if (context.IsCandidate)
                sb.Append(" As a candidate, I can also look up your own skill gaps, application statuses, job matches, and unread notifications.");
            return sb.ToString();
        }

        private static string BuildUserPrompt(string message, ChatContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Current user context:");
            sb.AppendLine($"Role: {(context.IsCandidate ? "Candidate" : "Employer or Admin (not a candidate)")}");
            sb.AppendLine();

            if (context.IsCandidate)
            {
                sb.AppendLine("Skill gaps:");
                if (context.SkillGaps.Count == 0)
                    sb.AppendLine("(none recorded yet)");
                else
                    foreach (var g in context.SkillGaps.OrderByDescending(g => g.MustHaveCount).ThenByDescending(g => g.TotalOccurrences).Take(10))
                        sb.AppendLine($"- {g.SkillName}: must-have in {g.MustHaveCount} application(s), nice-to-have in {g.NiceToHaveCount} application(s)");
                sb.AppendLine();

                sb.AppendLine("Recent applications:");
                if (context.RecentApplications.Count == 0)
                    sb.AppendLine("(none yet)");
                else
                    foreach (var a in context.RecentApplications.Take(5))
                        sb.AppendLine($"- {a.JobTitle}{(a.CompanyName != null ? $" at {a.CompanyName}" : "")}: status {a.Status}, score {a.TotalScore:P0}");
                sb.AppendLine();

                sb.AppendLine("Job matches:");
                if (context.JobMatches.Count == 0)
                    sb.AppendLine("(none yet)");
                else
                    foreach (var m in context.JobMatches.Take(5))
                        sb.AppendLine($"- {m.JobTitle}{(m.CompanyName != null ? $" at {m.CompanyName}" : "")}: {m.MatchScore:P0} match");
                sb.AppendLine();

                sb.AppendLine($"Unread notifications: {context.UnreadNotificationCount}");
                sb.AppendLine();
            }

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
