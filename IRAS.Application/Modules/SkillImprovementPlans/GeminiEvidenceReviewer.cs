// IRAS.Application/Modules/SkillImprovementPlans/GeminiEvidenceReviewer.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    // Real LLM-backed evidence reviewer (Google Gemini API) — same REST-call and strict-JSON
    // pattern as GeminiSkillPlanGenerator/GeminiSkillGapExplainer.
    //
    // Deliberately fail-safe: unlike the other Gemini generators (which throw and surface a
    // 503-style error to the caller), every failure path here returns a neutral score instead
    // of throwing. A submission failing to get an AI opinion must never block the candidate
    // from submitting evidence — it should just fall back to manual review, same as
    // TemplateEvidenceReviewer.
    public class GeminiEvidenceReviewer : IEvidenceReviewer
    {
        public string Name => "Gemini";
        public bool IsAi => true;

        private const string SystemPrompt = """
            You are reviewing evidence a candidate submitted to prove they completed a skill
            improvement mini-project on the IRAS recruitment platform. You will be given the
            skill name, the mini-project's title/task/expected-output, the evidence type, the
            evidence URL, and the candidate's own notes about it.

            You cannot browse the URL or open the link — you have no way to inspect its actual
            contents. Judge plausibility only from the URL's structure (does it look like a
            real, specific project, not a placeholder or an unrelated link) and, most
            importantly, whether the candidate's own notes specifically and credibly describe
            how it satisfies the expected output.

            Rules:
            1. Score confidence from 0 to 100 that this evidence genuinely satisfies the
               mini-project's expected output. Be conservative — since you cannot verify
               contents, a bare link with no explanatory notes should never score high.
            2. Only score 80 or above when the candidate's notes specifically and credibly
               explain how the evidence demonstrates the expected output.
            3. Score 25 or below when the evidence is clearly insufficient, generic,
               unrelated to the stated skill, or the URL looks like a placeholder or fake.
            4. Write one short, neutral sentence explaining the score — it may be shown
               directly to the candidate, so keep the tone constructive, never harsh.
            5. Respond with ONLY a single JSON object, no Markdown code fences, no commentary,
               in exactly this shape:
               {"confidenceScore": <integer 0-100>, "rationale": "..."}
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiEvidenceReviewer> _logger;

        public GeminiEvidenceReviewer(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiEvidenceReviewer> logger)
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

        public async Task<EvidenceReviewResult> ReviewAsync(
            string skillName, string projectTitle, string projectTask, string projectExpectedOutput,
            string evidenceType, string evidenceUrl, string? candidateNotes, CancellationToken ct)
        {
            var userPrompt = BuildUserPrompt(skillName, projectTitle, projectTask, projectExpectedOutput, evidenceType, evidenceUrl, candidateNotes);

            var requestBody = new GeminiRequest(
                _options.Model, SystemPrompt, userPrompt, new GeminiGenerationConfig(1024, "minimal"));

            GeminiResponse? result;
            try
            {
                var httpResponse = await _http.PostAsJsonAsync("/v1beta/interactions", requestBody, JsonOpts, ct);
                httpResponse.EnsureSuccessStatusCode();
                result = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Gemini evidence review failed for skill '{SkillName}' — falling back to manual review", skillName);
                return new EvidenceReviewResult(50, "Automatic review failed — routed to manual review.");
            }

            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini evidence review returned no text content for skill '{SkillName}' (status={Status})",
                    skillName, result?.Status ?? "null");
                return new EvidenceReviewResult(50, "Automatic review returned no result — routed to manual review.");
            }

            return ParseReview(text);
        }

        private static EvidenceReviewResult ParseReview(string text)
        {
            var cleaned = text.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNewline >= 0 && lastFence > firstNewline)
                    cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
            }

            try
            {
                var payload = JsonSerializer.Deserialize<ReviewPayload>(cleaned, JsonOpts);
                if (payload is null)
                    return new EvidenceReviewResult(50, "Automatic review returned an empty result — routed to manual review.");

                var score = Math.Clamp(payload.ConfidenceScore, 0, 100);
                return new EvidenceReviewResult(score, payload.Rationale ?? "No rationale provided.");
            }
            catch (JsonException)
            {
                return new EvidenceReviewResult(50, "Automatic review response was malformed — routed to manual review.");
            }
        }

        private static string BuildUserPrompt(
            string skillName, string projectTitle, string projectTask, string projectExpectedOutput,
            string evidenceType, string evidenceUrl, string? candidateNotes)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Skill: {skillName}");
            sb.AppendLine($"Mini-project title: {projectTitle}");
            sb.AppendLine($"Mini-project task: {projectTask}");
            sb.AppendLine($"Expected output: {projectExpectedOutput}");
            sb.AppendLine();
            sb.AppendLine($"Evidence type: {evidenceType}");
            sb.AppendLine($"Evidence URL: {evidenceUrl}");
            sb.AppendLine($"Candidate's notes: {(string.IsNullOrWhiteSpace(candidateNotes) ? "(none provided)" : candidateNotes)}");
            sb.AppendLine();
            sb.AppendLine("Respond now with the JSON object described in the system instructions.");
            return sb.ToString();
        }

        private record ReviewPayload(
            [property: JsonPropertyName("confidenceScore")] int ConfidenceScore,
            string? Rationale);

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
