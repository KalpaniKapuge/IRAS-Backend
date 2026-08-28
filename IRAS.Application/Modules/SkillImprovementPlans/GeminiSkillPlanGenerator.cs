// IRAS.Application/Modules/SkillImprovementPlans/GeminiSkillPlanGenerator.cs
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;
using IRAS.Domain.Enums;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    // Real LLM-backed roadmap generator (Google Gemini API) — same REST-call and strict-JSON
    // pattern as GeminiSkillGapExplainer/GeminiFeedbackGenerator/GeminiJdGenerator.
    // TemplateSkillPlanGenerator remains the deterministic fallback used when no Gemini API
    // key is configured.
    public class GeminiSkillPlanGenerator : ISkillPlanGenerator
    {
        public string Name => "Gemini";
        public bool IsAi => true;

        private const string SystemPrompt = """
            You are a career-development coach on the IRAS recruitment platform, creating a
            complete, practical, step-by-step learning roadmap so a candidate can close ONE
            specific missing skill for a job they applied to (or are targeting generally).

            You will be given: the skill name, a job title (or "General" if not tied to one
            specific job), and whether the skill is "MustHave" or "NiceToHave" for that role.

            Rules:
            1. Produce 7 to 10 ordered roadmap stages taking the candidate from zero knowledge
               to being able to demonstrate the skill in a small real project. Early stages
               cover fundamentals and setup, middle stages cover hands-on practice, and the
               final stages are always a practical mini-project and a review stage.
            2. Every stage needs: a short title, a one-sentence description of what to learn,
               a concrete activity the candidate should actually DO (not just "read about X"),
               and the expected output/outcome of completing that stage.
            3. Choose a realistic total duration in days (typically 7-21 depending on how deep
               the skill is) and a target level (Beginner, Intermediate, or JobReady)
               appropriate for what a job applicant would realistically need for this role.
            4. Include exactly one concrete mini-project: a title, a one-sentence task
               description, and the expected deliverable that proves the skill was learned.
            5. Write a 1-2 sentence "overview" of what the skill is and why it exists.
            6. Write a 1-2 sentence "gapReason": a neutral, non-judgmental explanation of why
               this skill is likely missing from the candidate's current profile and why it
               matters for this role. Never blame the candidate.
            7. Never fabricate specific external product names, exact certifications, or named
               paid courses — describe resource TYPES only (e.g. "an introductory video
               course", "the official documentation"), never a fabricated specific title.
            8. Respond with ONLY a single JSON object, no Markdown code fences, no commentary,
               in exactly this shape:
               {"overview": "...", "gapReason": "...", "targetLevel": "Beginner|Intermediate|JobReady",
                "priority": "High|Medium|Low", "estimatedDays": <integer>,
                "projectTitle": "...", "projectTask": "...", "projectExpectedOutput": "...",
                "steps": [{"title": "...", "description": "...", "activity": "...", "output": "..."}]}
            """;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiSkillPlanGenerator> _logger;

        public GeminiSkillPlanGenerator(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiSkillPlanGenerator> logger)
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

        public async Task<SkillPlanGenerationResult> GenerateAsync(
            string skillName, string? jobTitle, string importance, CancellationToken ct)
        {
            var userPrompt = BuildUserPrompt(skillName, jobTitle, importance);

            // thinking_level "minimal" — same reasoning as the other Gemini generators: this
            // is a constrained, structured-output task, not deep reasoning, and the default
            // thinking budget risks truncating the actual JSON answer. Roadmap JSON is larger
            // than the other generators' outputs, so the token budget is raised accordingly.
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
                _logger.LogError(ex, "Gemini skill plan generation failed for skill '{SkillName}'", skillName);
                throw new InvalidOperationException(
                    "The AI skill-plan service is temporarily unavailable. Please try again shortly.");
            }

            var text = (result?.Steps ?? new List<GeminiStep>())
                .SelectMany(s => s.Content ?? new List<GeminiContentPart>())
                .Where(c => c.Type == "text" && !string.IsNullOrWhiteSpace(c.Text))
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("Gemini skill plan generation returned no text content for skill '{SkillName}' (status={Status})",
                    skillName, result?.Status ?? "null");
                throw new InvalidOperationException("The AI service did not return a skill plan. Please try again.");
            }

            return ParsePlan(text, skillName, importance);
        }

        private static SkillPlanGenerationResult ParsePlan(string text, string skillName, string importance)
        {
            var cleaned = text.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNewline >= 0 && lastFence > firstNewline)
                    cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
            }

            var payload = JsonSerializer.Deserialize<PlanPayload>(cleaned, JsonOpts)
                ?? throw new InvalidOperationException("The AI service returned an empty skill plan.");

            if (payload.Steps is null || payload.Steps.Count == 0)
                throw new InvalidOperationException("The AI service returned a skill plan with no roadmap steps.");

            return new SkillPlanGenerationResult(
                Overview: payload.Overview ?? $"{skillName} is relevant to this role.",
                GapReason: payload.GapReason ?? $"{skillName} is not yet reflected in your profile.",
                TargetLevel: ParseEnum<SkillTargetLevel>(payload.TargetLevel, SkillTargetLevel.Intermediate),
                Priority: ParseEnum<SkillPlanPriority>(payload.Priority,
                    string.Equals(importance, "MustHave", StringComparison.OrdinalIgnoreCase)
                        ? SkillPlanPriority.High : SkillPlanPriority.Medium),
                EstimatedDays: payload.EstimatedDays is > 0 ? payload.EstimatedDays.Value : 14,
                ProjectTitle: payload.ProjectTitle ?? $"{skillName} Mini Project",
                ProjectTask: payload.ProjectTask ?? $"Build a small project applying {skillName}.",
                ProjectExpectedOutput: payload.ProjectExpectedOutput ?? "A working project demonstrating the skill.",
                Steps: payload.Steps
                    .Select(s => new SkillPlanStepDraft(
                        s.Title ?? "Untitled step", s.Description ?? "", s.Activity ?? "", s.Output ?? ""))
                    .ToList());
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
            => !string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
                ? parsed
                : fallback;

        private static string BuildUserPrompt(string skillName, string? jobTitle, string importance)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Skill: {skillName}");
            sb.AppendLine($"Job Title: {(string.IsNullOrWhiteSpace(jobTitle) ? "General" : jobTitle)}");
            sb.AppendLine($"Importance: {importance}");
            sb.AppendLine();
            sb.AppendLine("Respond now with the JSON object described in the system instructions.");
            return sb.ToString();
        }

        private record PlanPayload(
            string? Overview,
            [property: JsonPropertyName("gapReason")] string? GapReason,
            [property: JsonPropertyName("targetLevel")] string? TargetLevel,
            string? Priority,
            [property: JsonPropertyName("estimatedDays")] int? EstimatedDays,
            [property: JsonPropertyName("projectTitle")] string? ProjectTitle,
            [property: JsonPropertyName("projectTask")] string? ProjectTask,
            [property: JsonPropertyName("projectExpectedOutput")] string? ProjectExpectedOutput,
            List<PlanStepPayload>? Steps);

        private record PlanStepPayload(string? Title, string? Description, string? Activity, string? Output);

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
