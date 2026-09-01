// IRAS.Application/Modules/Assessments/GeminiAssessmentQuestionGenerator.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.Assessments
{
    // Real LLM-backed question generator (Google Gemini API) — same REST-call and
    // strict-JSON pattern as GeminiSkillPlanGenerator/GeminiEvidenceReviewer/GeminiJdGenerator.
    // TemplateAssessmentQuestionGenerator is the deterministic fallback used when no Gemini
    // API key is configured.
    public class GeminiAssessmentQuestionGenerator : IAssessmentQuestionGenerator
    {
        public string Name => "Gemini";

        private const string SystemPrompt = """
            You are a technical interviewer creating a short screening quiz for the IRAS
            recruitment platform, so an employer can verify a candidate actually has the
            skills they claim on their CV before being interviewed.

            You will be given a job title, seniority level, a job description, and its
            required skills (must-have and nice-to-have).

            Rules:
            1. Produce exactly the requested number of questions, using a MIX of two types:
               - "MultipleChoice": exactly 4 answer options and exactly one correct option.
               - "FreeText": a question the candidate answers by writing text, code, or a
                 query (e.g. "Write a SQL query that..." or "Write a function that..."), with
                 a concise model/expected answer a grader can compare against.
               Aim for roughly 70% MultipleChoice and 30% FreeText, rounding sensibly — use
               FreeText especially for skills where writing real code/queries proves
               understanding better than a multiple-choice question could (e.g. SQL,
               programming languages). If the requested count is very small (1-2 questions),
               MultipleChoice only is fine.
            2. Every question must test practical, real understanding of one of the listed
               required skills (or the job description) — no trivia, no ambiguous wording, no
               "all of the above" / "none of the above" options.
            3. Favor must-have skills over nice-to-have skills, and spread questions across as
               many different required skills as reasonably possible rather than repeating one
               skill.
            4. For MultipleChoice, distractors (wrong options) must be plausible to someone
               who doesn't actually know the skill — not obviously silly.
            5. For FreeText, the model answer should be concise (a few lines at most) and
               describe what a correct answer must contain, not a full essay.
            6. Difficulty should match the stated seniority level.
            7. Respond with ONLY a single JSON object, no Markdown code fences, no commentary,
               in exactly this shape:
               {"questions": [
                 {"type": "MultipleChoice", "questionText": "...",
                  "options": ["...", "...", "...", "..."], "correctOptionIndex": <0-3>,
                  "skillName": "..."},
                 {"type": "FreeText", "questionText": "...", "modelAnswer": "...",
                  "skillName": "..."}
               ]}
               For FreeText entries, omit "options" and "correctOptionIndex" (or leave them
               empty/0) and always include "modelAnswer".
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiAssessmentQuestionGenerator> _logger;

        public GeminiAssessmentQuestionGenerator(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiAssessmentQuestionGenerator> logger)
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

        public async Task<List<GeneratedQuestion>> GenerateAsync(
            Job job, IEnumerable<(string SkillName, string Importance, SkillCategory Category)> skills, int questionCount, CancellationToken ct)
        {
            var userPrompt = BuildUserPrompt(job, skills, questionCount);

            var requestBody = new GeminiRequest(
                _options.Model,
                SystemPrompt,
                userPrompt,
                new GeminiGenerationConfig(4096, "minimal"));

            GeminiResponse? result;
            try
            {
                var httpResponse = await _http.PostAsJsonAsync("/v1beta/interactions", requestBody, JsonOpts, ct);
                httpResponse.EnsureSuccessStatusCode();
                result = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogError(ex, "Gemini assessment question generation failed for job {JobId}", job.JobId);
                throw new InvalidOperationException(
                    "The AI assessment service is temporarily unavailable. Please try again shortly.");
            }

            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Gemini assessment question generation returned no text content for job {JobId} (status={Status})",
                    job.JobId, result?.Status ?? "null");
                throw new InvalidOperationException("The AI service did not return any assessment questions. Please try again.");
            }

            return ParseQuestions(text);
        }

        private static List<GeneratedQuestion> ParseQuestions(string text)
        {
            var cleaned = text.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNewline >= 0 && lastFence > firstNewline)
                    cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
            }

            var payload = JsonSerializer.Deserialize<QuestionsPayload>(cleaned, JsonOpts)
                ?? throw new InvalidOperationException("The AI service returned an empty assessment.");

            if (payload.Questions is null || payload.Questions.Count == 0)
                throw new InvalidOperationException("The AI service returned an assessment with no questions.");

            return payload.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText))
                .Select(ToGeneratedQuestion)
                .Where(q => q is not null)
                .Select(q => q!)
                .ToList();
        }

        private static GeneratedQuestion? ToGeneratedQuestion(QuestionPayload q)
        {
            // Infer the type from shape if the model's "type" field is missing/malformed —
            // a well-formed 4-option MCQ is treated as MultipleChoice, anything else with a
            // model answer is treated as FreeText.
            var isMcq = string.Equals(q.Type, "MultipleChoice", StringComparison.OrdinalIgnoreCase)
                || (q.Type is null && q.Options is { Count: 4 } && q.CorrectOptionIndex is >= 0 and <= 3);

            if (isMcq)
            {
                if (q.Options is not { Count: 4 } || q.CorrectOptionIndex is < 0 or > 3)
                    return null;
                return new GeneratedQuestion(AssessmentQuestionType.MultipleChoice, q.QuestionText!, q.Options, q.CorrectOptionIndex, null, q.SkillName);
            }

            if (string.IsNullOrWhiteSpace(q.ModelAnswer))
                return null;
            return new GeneratedQuestion(AssessmentQuestionType.FreeText, q.QuestionText!, new List<string>(), 0, q.ModelAnswer, q.SkillName);
        }

        private static string BuildUserPrompt(Job job, IEnumerable<(string SkillName, string Importance, SkillCategory Category)> skills, int questionCount)
        {
            var must = skills.Where(s => s.Importance == "MustHave").ToList();
            var nice = skills.Where(s => s.Importance == "NiceToHave").ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Job Title: {job.Title}");
            sb.AppendLine($"Seniority Level: {job.SeniorityLevel}");
            sb.AppendLine($"Number of questions to generate: {questionCount}");
            sb.AppendLine();

            sb.AppendLine("Job description:");
            sb.AppendLine(string.IsNullOrWhiteSpace(job.GeneratedJd) ? job.Title : job.GeneratedJd);
            sb.AppendLine();

            sb.AppendLine("Must-have skills:");
            foreach (var s in must) sb.AppendLine($"- {s.SkillName}");
            if (must.Count == 0) sb.AppendLine("(none specified)");
            sb.AppendLine();

            sb.AppendLine("Nice-to-have skills:");
            foreach (var s in nice) sb.AppendLine($"- {s.SkillName}");
            if (nice.Count == 0) sb.AppendLine("(none specified)");
            sb.AppendLine();

            sb.AppendLine("Generate the assessment questions now, following the system instructions exactly.");

            return sb.ToString();
        }

        private record QuestionsPayload(List<QuestionPayload>? Questions);
        private record QuestionPayload(
            [property: JsonPropertyName("type")] string? Type,
            [property: JsonPropertyName("questionText")] string? QuestionText,
            [property: JsonPropertyName("options")] List<string>? Options,
            [property: JsonPropertyName("correctOptionIndex")] int CorrectOptionIndex,
            [property: JsonPropertyName("modelAnswer")] string? ModelAnswer,
            [property: JsonPropertyName("skillName")] string? SkillName);

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
