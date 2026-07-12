// IRAS.Application/Common/Ai/AiServiceClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace IRAS.Application.Common.Ai
{
    public class AiServiceClient : IAiServiceClient
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly ILogger<AiServiceClient> _logger;

        public AiServiceClient(HttpClient http, ILogger<AiServiceClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<ParseResumeResult> ParseResumeAsync(
            Stream file, string fileName, string fileFormat,
            IReadOnlyList<TaxonomyItem> taxonomy, CancellationToken ct)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file);
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(fileFormat), "file_format");
            form.Add(new StringContent(JsonSerializer.Serialize(taxonomy, JsonOpts)), "taxonomy_json");

            try
            {
                var response = await _http.PostAsync("/api/v1/parse-resume", form, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PyParseResponse>(JsonOpts, ct)
                    ?? throw new InvalidOperationException("AI service returned an empty body.");

                return new ParseResumeResult(
                    result.Success, result.Error, result.ParsedText,
                    result.DetectedSkills.Select(s => new DetectedSkillResult(
                        s.SkillId, s.SkillName, s.MatchedText, s.MatchedBy, s.Occurrences)).ToList(),
                    result.Emails, result.Phones, result.WordCount);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Infrastructure failure != parse failure. The caller decides what
                // to persist; we log and surface a candidate-safe message.
                _logger.LogError(ex, "AI service call failed for {FileName}", fileName);
                return new ParseResumeResult(false,
                    "Resume analysis service is temporarily unavailable. Your file was saved; you can retry parsing later.",
                    null, new List<DetectedSkillResult>(), new List<string>(), new List<string>(), 0);
            }
        }

        public async Task<RankResult> RankAsync(
            string jobDescription, IReadOnlyList<RankCandidateInput> candidates, CancellationToken ct)
        {
            var body = new
            {
                JobDescription = jobDescription,
                Candidates = candidates.Select(c => new { c.CandidateId, c.ResumeText })
            };

            try
            {
                var response = await _http.PostAsJsonAsync("/api/v1/rank", body, JsonOpts, ct);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PyRankResponse>(JsonOpts, ct)
                    ?? throw new InvalidOperationException("AI service returned an empty body.");

                if (!result.Success)
                    return new RankResult(false, result.Error, new List<RankedResult>());

                return new RankResult(true, null,
                    result.Results.Select(r => new RankedResult(r.CandidateId, (decimal)r.SemanticSimilarity)).ToList());
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "AI service ranking call failed ({CandidateCount} candidates)", candidates.Count);
                return new RankResult(false,
                    "Resume ranking service is temporarily unavailable. Please try again shortly.",
                    new List<RankedResult>());
            }
        }

        public async Task<bool> CheckHealthAsync(CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync("/health", ct);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "AI service health check failed");
                return false;
            }
        }

        // Mirrors the Python response model. FastAPI emits snake_case, so we map explicitly
        // rather than relying on JsonSerializerDefaults.Web's camelCase convention.
        // Note: [property: JsonPropertyName] only works on ctor params that don't have a
        // body-declared override below — for DetectedSkills (which needs a null-coalescing
        // default) the attribute has to move onto the redeclared property instead.
        private sealed record PyParseResponse(
            bool Success,
            string? Error,
            [property: JsonPropertyName("parsed_text")] string? ParsedText,
            List<PySkill> DetectedSkills,
            List<string> Emails,
            List<string> Phones,
            [property: JsonPropertyName("word_count")] int WordCount)
        {
            [JsonPropertyName("detected_skills")]
            public List<PySkill> DetectedSkills { get; init; } = DetectedSkills ?? new();
            public List<string> Emails { get; init; } = Emails ?? new();
            public List<string> Phones { get; init; } = Phones ?? new();
        }

        private sealed record PySkill(
            [property: JsonPropertyName("skill_id")] int SkillId,
            [property: JsonPropertyName("skill_name")] string SkillName,
            [property: JsonPropertyName("matched_text")] string MatchedText,
            [property: JsonPropertyName("matched_by")] string MatchedBy,
            int Occurrences);

        private sealed record PyRankResponse(bool Success, string? Error, List<PyRankedResult> Results)
        {
            public List<PyRankedResult> Results { get; init; } = Results ?? new();
        }

        private sealed record PyRankedResult(
            [property: JsonPropertyName("candidate_id")] int CandidateId,
            [property: JsonPropertyName("semantic_similarity")] double SemanticSimilarity);
    }
}
