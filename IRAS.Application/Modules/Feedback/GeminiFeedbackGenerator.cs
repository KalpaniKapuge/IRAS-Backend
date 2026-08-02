// IRAS.Application/Modules/Feedback/GeminiFeedbackGenerator.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Modules.Feedback
{
    // Real LLM-backed feedback generator (Google Gemini API) — same REST-call pattern as
    // GeminiJdGenerator/GeminiChatResponder (no official Google-maintained C# SDK for this
    // endpoint). TemplateFeedbackGenerator remains as the deterministic baseline for the
    // thesis's evaluation chapter; this is what actually serves requests once registered
    // as the active IFeedbackGenerator in Program.cs.
    public class GeminiFeedbackGenerator : IFeedbackGenerator
    {
        public string Name => "Gemini";
        public bool IsAi => true;

        private const string SystemPrompt = """
            You are an expert, empathetic recruiter writing rejection feedback for a candidate
            on the IRAS recruitment platform. You will be given the job title, company name,
            the candidate's overall match score, and the specific skill gaps that were
            identified for this application.

            Rules:
            1. Never blame the candidate or use discouraging language. This reflects fit for
               this specific role, not the candidate's overall worth or potential.
            2. Be constructive and specific: reference the actual skill gaps given, and explain
               briefly why each matters for this kind of role and how the candidate could
               realistically close it (a course, a personal project, certification, or
               practical experience type — general and realistic, not a fabricated specific
               product name or company).
            3. Do not invent facts not given to you (no fabricated interview details, no
               specific reasons beyond the provided skill gaps and score).
            4. Use inclusive, neutral, unbiased language — no assumptions about age, gender,
               background, or culture.
            5. Tone: warm, professional, encouraging. End by inviting the candidate to apply
               again as their experience grows.
            6. Output ONLY the final feedback message as plain text (a few short paragraphs).
               No Markdown headers, no preamble, no commentary before or after it.
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiFeedbackGenerator> _logger;

        public GeminiFeedbackGenerator(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiFeedbackGenerator> logger)
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

        public async Task<string> GenerateAsync(
            string jobTitle, string companyName, decimal totalScore,
            IEnumerable<(string SkillName, string Importance, string? Suggestion)> skillGaps,
            CancellationToken ct)
        {
            var userPrompt = BuildUserPrompt(jobTitle, companyName, totalScore, skillGaps);

            // thinking_level "minimal" — same reasoning as GeminiJdGenerator: this is a
            // constrained rewriting/composition task, not deep multi-step reasoning, and
            // leaving thinking at its default risks consuming the token budget before the
            // actual feedback text is produced.
            var requestBody = new GeminiRequest(
                _options.Model,
                SystemPrompt,
                userPrompt,
                new GeminiGenerationConfig(2048, "minimal"));

            GeminiResponse? result;
            try
            {
                var httpResponse = await _http.PostAsJsonAsync("/v1beta/interactions", requestBody, JsonOpts, ct);
                httpResponse.EnsureSuccessStatusCode();
                result = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Gemini feedback generation failed for job '{JobTitle}'", jobTitle);
                throw new InvalidOperationException(
                    "The AI feedback service is temporarily unavailable. Please try again shortly.");
            }

            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Gemini feedback generation returned no text content for job '{JobTitle}' (status={Status})",
                    jobTitle, result?.Status ?? "null");
                throw new InvalidOperationException("The AI service did not return feedback text. Please try again.");
            }

            if (result!.Status == "incomplete")
                _logger.LogWarning("Gemini feedback generation for job '{JobTitle}' was truncated (status=incomplete); returning partial text", jobTitle);

            return text.Trim();
        }

        private static string BuildUserPrompt(
            string jobTitle, string companyName, decimal totalScore,
            IEnumerable<(string SkillName, string Importance, string? Suggestion)> skillGaps)
        {
            var gaps = skillGaps.ToList();
            var must = gaps.Where(g => g.Importance == "MustHave").ToList();
            var nice = gaps.Where(g => g.Importance == "NiceToHave").ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Job Title: {jobTitle}");
            sb.AppendLine($"Company: {companyName}");
            sb.AppendLine($"Candidate's overall match score: {totalScore:P0}");
            sb.AppendLine();

            sb.AppendLine("Must-have skill gaps (required for the role, candidate is missing these):");
            foreach (var g in must)
                sb.AppendLine(g.Suggestion != null ? $"- {g.SkillName}: {g.Suggestion}" : $"- {g.SkillName}");
            if (must.Count == 0) sb.AppendLine("(none)");
            sb.AppendLine();

            sb.AppendLine("Nice-to-have skill gaps:");
            foreach (var g in nice)
                sb.AppendLine(g.Suggestion != null ? $"- {g.SkillName}: {g.Suggestion}" : $"- {g.SkillName}");
            if (nice.Count == 0) sb.AppendLine("(none)");
            sb.AppendLine();

            sb.AppendLine("Write the complete rejection feedback message now, following the system instructions exactly.");
            return sb.ToString();
        }

        // Mirrors Gemini's Interactions API JSON shape — same schema as GeminiJdGenerator.
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
