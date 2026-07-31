// IRAS.Application/Modules/Jobs/GeminiJdGenerator.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Application.Modules.Jobs
{
    // Real LLM-backed JD generator using Google's Gemini API (free tier, no billing card
    // required). There is no official Google-maintained C# SDK for this endpoint, so this
    // calls the documented REST endpoint directly via HttpClient — the same approach this
    // codebase already uses for the Python AI microservice (see
    // Common/Ai/AiServiceClient.cs). Implements the same IJdGenerator contract as
    // TemplateJdGenerator/ClaudeJdGenerator/GptJdGenerator — a drop-in swap in Program.cs.
    public class GeminiJdGenerator : IJdGenerator
    {
        public string Name => "Gemini";
        public bool IsAi => true;

        private const string SystemPrompt = """
            You are an expert technical recruiter and professional copywriter working inside
            the IRAS recruitment platform. Write a single, complete, well-structured job
            description for an IT/software role from the structured job data and optional
            free-text notes you are given.

            Rules:
            1. The employer's free-text notes may be informal, incomplete, or contain
               spelling or grammar issues. Understand the intended meaning, correct the
               wording, and rewrite it as complete, professional sentences woven naturally
               into the right section(s). Never quote the raw notes verbatim if they contain
               errors — always produce corrected, complete sentences.
            2. Do not invent facts that are not stated or clearly implied by the input — no
               fabricated salary, benefits, team size, or perks unless present in the notes.
            3. Use inclusive, neutral language. No age, gender, or cultural bias.
            4. Structure the output with these Markdown sections, in this order, omitting any
               section that has no content to include:
               # {Job Title} ({Seniority Level})
               **Company:** ...
               **Location:** ... (omit if not provided)
               **Employment Type:** ...
               ## About Us
               ## The Role
               ## Key Responsibilities
               ## Required Skills
               ## Nice to Have
               ## Qualifications
               ## How to Apply
            5. Under Key Responsibilities, infer 4-6 realistic, specific responsibilities
               from the job title, seniority level, and required skills.
            6. Tone: professional, concise, welcoming. No filler, no marketing fluff.
            7. Output ONLY the final job description in Markdown — no preamble, no
               explanation, no commentary before or after it.
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiJdGenerator> _logger;

        public GeminiJdGenerator(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiJdGenerator> logger)
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

        public async Task<string> GenerateAsync(Job job,
            IEnumerable<(string SkillName, string Importance, int MinYears)> skills,
            string companyName, string? companyDescription, string? additionalNotes)
        {
            var userPrompt = BuildUserPrompt(job, skills, companyName, companyDescription, additionalNotes);

            // thinking_level "minimal": JD generation is a rewriting/structuring task, not
            // deep multi-step reasoning. Left at the default, the model's internal
            // "thinking" competes with the actual output for the same max_output_tokens
            // budget — observed empirically to consume 90%+ of a small budget and leave
            // the real answer truncated (status "incomplete"). Minimal thinking leaves the
            // budget for the JD text itself.
            var requestBody = new GeminiRequest(
                _options.Model,
                SystemPrompt,
                userPrompt,
                new GeminiGenerationConfig(4096, "minimal"));

            GeminiResponse? result;
            try
            {
                var httpResponse = await _http.PostAsJsonAsync("/v1beta/interactions", requestBody, JsonOpts);
                httpResponse.EnsureSuccessStatusCode();
                result = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Gemini JD generation failed for job {JobId}", job.JobId);
                throw new InvalidOperationException(
                    "The AI job description service is temporarily unavailable. Please try again shortly.");
            }

            // "incomplete" still carries whatever text the model produced before hitting
            // the token cap — worth returning rather than discarding. Only a genuinely
            // empty result (or an outright failed/cancelled status) is a hard failure.
            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Gemini JD generation returned no text content for job {JobId} (status={Status})",
                    job.JobId, result?.Status ?? "null");
                throw new InvalidOperationException("The AI service did not return a job description. Please try again.");
            }

            if (result!.Status == "incomplete")
                _logger.LogWarning("Gemini JD generation for job {JobId} was truncated (status=incomplete); returning partial text", job.JobId);

            return text.Trim();
        }

        private static string BuildUserPrompt(Job job,
            IEnumerable<(string SkillName, string Importance, int MinYears)> skills,
            string companyName, string? companyDescription, string? additionalNotes)
        {
            var must = skills.Where(s => s.Importance == "MustHave").ToList();
            var nice = skills.Where(s => s.Importance == "NiceToHave").ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Job Title: {job.Title}");
            sb.AppendLine($"Seniority Level: {job.SeniorityLevel}");
            sb.AppendLine($"Employment Type: {job.EmploymentType}");
            sb.AppendLine($"Location: {(string.IsNullOrWhiteSpace(job.Location) ? "Not specified (remote/unspecified)" : job.Location)}");
            sb.AppendLine($"Minimum Experience: {job.MinExpYears} year(s)");
            sb.AppendLine($"Minimum Education Requirement: {job.EducationReq}");
            sb.AppendLine($"Company Name: {companyName}");
            sb.AppendLine($"Company Description: {(string.IsNullOrWhiteSpace(companyDescription) ? "Not provided" : companyDescription)}");
            sb.AppendLine();

            sb.AppendLine("Required Skills (must-have):");
            foreach (var s in must)
                sb.AppendLine(s.MinYears > 0 ? $"- {s.SkillName} ({s.MinYears}+ years)" : $"- {s.SkillName}");
            if (must.Count == 0) sb.AppendLine("(none specified)");
            sb.AppendLine();

            sb.AppendLine("Nice-to-have Skills:");
            foreach (var s in nice)
                sb.AppendLine($"- {s.SkillName}");
            if (nice.Count == 0) sb.AppendLine("(none specified)");
            sb.AppendLine();

            sb.AppendLine("Employer's raw notes (informal — correct wording issues and incorporate naturally, do not quote verbatim):");
            sb.AppendLine(string.IsNullOrWhiteSpace(additionalNotes) ? "(none provided)" : additionalNotes);
            sb.AppendLine();
            sb.AppendLine("Generate the complete job description now, following the system instructions exactly.");

            return sb.ToString();
        }

        // Mirrors Gemini's Interactions API JSON shape (POST /v1beta/interactions).
        // Field names are explicitly snake_case per Google's documented schema.
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
