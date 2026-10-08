// IRAS.Application/Modules/Assessments/GeminiAssessmentAnswerGrader.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Modules.Assessments
{
    // Real LLM-backed grader (Google Gemini API) — same REST-call and strict-JSON pattern as
    // the other Gemini-backed classes in this module. TemplateAssessmentAnswerGrader is the
    // deterministic fallback used when no Gemini API key is configured.
    public class GeminiAssessmentAnswerGrader : IAssessmentAnswerGrader
    {
        public string Name => "Gemini";

        private const string SystemPrompt = """
            You are a technical interviewer grading ONE candidate's written answer (which may
            be prose, code, or a query such as SQL) to a skill-assessment question on the IRAS
            recruitment platform.

            You will be given the question, a model/expected answer, and the candidate's
            actual answer. Judge how correct and complete the candidate's answer is compared
            to the model answer — award partial credit for a mostly-correct answer (e.g. right
            logic with a minor syntax slip), and low/zero credit for an answer that is wrong,
            irrelevant, or empty. Do not require an exact textual match; judge understanding
            and correctness.

            Respond with ONLY a single JSON object, no Markdown code fences, no commentary, in
            exactly this shape:
            {"score": <integer 0-100>, "rationale": "..."}
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiAssessmentAnswerGrader> _logger;

        public GeminiAssessmentAnswerGrader(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiAssessmentAnswerGrader> logger)
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

        public async Task<decimal> GradeAsync(string questionText, string modelAnswer, string candidateAnswer, CancellationToken ct)
        {
            var userPrompt = BuildUserPrompt(questionText, modelAnswer, candidateAnswer);

            var requestBody = new GeminiRequest(
                new GeminiContent(null, [new GeminiPart(SystemPrompt)]),
                [new GeminiContent("user", [new GeminiPart(userPrompt)])],
                new GeminiGenerationConfig(512, "application/json"));

            try
            {
                var endpoint = $"/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent";
                var httpResponse = await _http.PostAsJsonAsync(endpoint, requestBody, JsonOpts, ct);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Gemini answer grading failed. Status={StatusCode}, Body={Body}",
                        (int)httpResponse.StatusCode, errorBody);
                    return 0m;
                }
                var result = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);

                var text = (result?.Candidates ?? new List<GeminiCandidate>())
                    .SelectMany(c => c.Content?.Parts ?? new List<GeminiResponsePart>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.Text))
                    .Select(p => p.Text)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Gemini answer grading returned no text content");
                    return 0m;
                }

                return ParseScore(text);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Grading one free-text answer must never fail the whole submission — a
                // network hiccup here just scores this question 0 rather than blocking the
                // candidate's other answers from being saved.
                _logger.LogError(ex, "Gemini answer grading failed");
                return 0m;
            }
        }

        private static decimal ParseScore(string text)
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
                var payload = JsonSerializer.Deserialize<ScorePayload>(cleaned, JsonOpts);
                var score = payload?.Score ?? 0;
                return Math.Clamp(score, 0, 100) / 100m;
            }
            catch (JsonException)
            {
                return 0m;
            }
        }

        private static string BuildUserPrompt(string questionText, string modelAnswer, string candidateAnswer)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Question: {questionText}");
            sb.AppendLine();
            sb.AppendLine("Model/expected answer:");
            sb.AppendLine(modelAnswer);
            sb.AppendLine();
            sb.AppendLine("Candidate's answer:");
            sb.AppendLine(string.IsNullOrWhiteSpace(candidateAnswer) ? "(no answer given)" : candidateAnswer);
            sb.AppendLine();
            sb.AppendLine("Grade the candidate's answer now, following the system instructions exactly.");
            return sb.ToString();
        }

        private record ScorePayload([property: JsonPropertyName("score")] int Score, [property: JsonPropertyName("rationale")] string? Rationale);

        private record GeminiRequest(
            [property: JsonPropertyName("system_instruction")] GeminiContent SystemInstruction,
            List<GeminiContent> Contents,
            [property: JsonPropertyName("generation_config")] GeminiGenerationConfig GenerationConfig);

        private record GeminiContent(string? Role, List<GeminiPart> Parts);
        private record GeminiPart(string Text);

        private record GeminiGenerationConfig(
            [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens,
            [property: JsonPropertyName("response_mime_type")] string ResponseMimeType);

        private record GeminiResponse(List<GeminiCandidate>? Candidates);
        private record GeminiCandidate(GeminiResponseContent? Content);
        private record GeminiResponseContent(List<GeminiResponsePart>? Parts);
        private record GeminiResponsePart(string? Text);
    }
}
