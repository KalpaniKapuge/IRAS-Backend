// IRAS.Application/Modules/SkillGaps/GeminiSkillGapExplainer.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Modules.SkillGaps
{
    // Real LLM-backed skill-gap explainer (Google Gemini API) — same REST-call pattern as
    // GeminiJdGenerator/GeminiFeedbackGenerator/GeminiChatResponder. Asks the model to
    // return strict JSON (one explanation per requested skill) rather than free text, since
    // the result has to be matched back to specific SkillGap rows by the caller.
    public class GeminiSkillGapExplainer : ISkillGapExplainer
    {
        public string Name => "Gemini";
        public bool IsAi => true;

        private const string SystemPrompt = """
            You are a career advisor on the IRAS recruitment platform, explaining to a job
            candidate why specific skills they are missing matter for a role they applied to,
            and how they could realistically close each gap.

            You will be given a job title and a list of missing skills, each marked as either
            "MustHave" (required for the role) or "NiceToHave" (adds value but not required).

            Rules:
            1. For each skill, write ONE encouraging, specific sentence: briefly explain why
               that skill matters for a role like this, and suggest a realistic, general way to
               build it (a type of course, a personal project idea, practical experience, or a
               certification category) — general and realistic, never a fabricated specific
               product, company, or exact certification name.
            2. Never use discouraging or blaming language.
            3. Respond with ONLY a single JSON object, no Markdown code fences, no commentary,
               in exactly this shape:
               {"skills": [{"skillName": "<exact skill name as given>", "explanation": "<one sentence>"}]}
            4. Include every skill you were given, in the same order, with the exact skillName
               text you were given (do not rename, translate, or alter it).
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiSkillGapExplainer> _logger;

        public GeminiSkillGapExplainer(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiSkillGapExplainer> logger)
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

        public async Task<Dictionary<int, string>> ExplainAsync(
            string jobTitle,
            IEnumerable<(int SkillId, string SkillName, string Importance)> gaps,
            CancellationToken ct)
        {
            var gapList = gaps.ToList();
            if (gapList.Count == 0) return new Dictionary<int, string>();

            var userPrompt = BuildUserPrompt(jobTitle, gapList);

            // thinking_level "minimal" — same reasoning as the other Gemini generators in
            // this codebase: this is a constrained, structured-output task, not deep
            // reasoning, and the default thinking budget risks truncating the actual answer.
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
                _logger.LogError(ex, "Gemini skill gap explanation failed for job '{JobTitle}'", jobTitle);
                throw new InvalidOperationException(
                    "The AI skill gap service is temporarily unavailable. Please try again shortly.");
            }

            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Gemini skill gap explanation returned no text content for job '{JobTitle}' (status={Status})",
                    jobTitle, result?.Status ?? "null");
                throw new InvalidOperationException("The AI service did not return skill gap explanations. Please try again.");
            }

            var parsed = ParseExplanations(text, jobTitle);

            // Match back to SkillId by exact skillName — the prompt instructs the model not
            // to alter the given names, so this should always resolve; any skill the model
            // dropped or renamed falls back to a plain statement rather than losing the gap
            // entirely.
            var byName = parsed.ToDictionary(p => p.SkillName, p => p.Explanation, StringComparer.OrdinalIgnoreCase);
            return gapList.ToDictionary(
                g => g.SkillId,
                g => byName.TryGetValue(g.SkillName, out var explanation)
                    ? explanation
                    : $"{g.SkillName} would strengthen this application.");
        }

        private static List<(string SkillName, string Explanation)> ParseExplanations(string text, string jobTitle)
        {
            try
            {
                // Defensive: strip Markdown code fences in case the model wraps the JSON
                // despite the system prompt explicitly asking it not to.
                var cleaned = text.Trim();
                if (cleaned.StartsWith("```"))
                {
                    var firstNewline = cleaned.IndexOf('\n');
                    var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                    if (firstNewline >= 0 && lastFence > firstNewline)
                        cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
                }

                var payload = JsonSerializer.Deserialize<SkillExplanationPayload>(cleaned, JsonOpts);
                return (payload?.Skills ?? new List<SkillExplanationItem>())
                    .Select(s => (s.SkillName, s.Explanation))
                    .ToList();
            }
            catch (JsonException)
            {
                // A malformed response degrades to an empty list rather than failing the whole
                // request — ExplainAsync's fallback then supplies a plain statement per skill.
                return new List<(string, string)>();
            }
        }

        private static string BuildUserPrompt(string jobTitle, List<(int SkillId, string SkillName, string Importance)> gaps)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Job Title: {jobTitle}");
            sb.AppendLine();
            sb.AppendLine("Missing skills:");
            foreach (var g in gaps)
                sb.AppendLine($"- {g.SkillName} ({g.Importance})");
            sb.AppendLine();
            sb.AppendLine("Respond now with the JSON object described in the system instructions, covering every skill listed above.");
            return sb.ToString();
        }

        private record SkillExplanationPayload(List<SkillExplanationItem> Skills);
        private record SkillExplanationItem(
            [property: JsonPropertyName("skillName")] string SkillName,
            [property: JsonPropertyName("explanation")] string Explanation);

        // Mirrors Gemini's Interactions API JSON shape — same schema as the other generators.
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
