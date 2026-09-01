// IRAS.Application/Modules/Assessments/TemplateAssessmentAnswerGrader.cs
using System.Text.RegularExpressions;

namespace IRAS.Application.Modules.Assessments
{
    // Deterministic fallback used when no Gemini API key is configured — scores a free-text
    // answer by how much of the model answer's significant vocabulary shows up in the
    // candidate's answer. Crude compared to AI grading (no understanding of correctness,
    // just lexical overlap), but keeps the quiz gradeable without an API key.
    public class TemplateAssessmentAnswerGrader : IAssessmentAnswerGrader
    {
        public string Name => "Template";

        private static readonly Regex WordPattern = new(@"[a-zA-Z0-9_]+", RegexOptions.Compiled);

        public Task<decimal> GradeAsync(string questionText, string modelAnswer, string candidateAnswer, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(candidateAnswer))
                return Task.FromResult(0m);

            var modelTokens = Tokenize(modelAnswer);
            if (modelTokens.Count == 0)
                return Task.FromResult(0m);

            var candidateTokens = Tokenize(candidateAnswer);
            var overlap = modelTokens.Count(t => candidateTokens.Contains(t));

            return Task.FromResult(Math.Clamp((decimal)overlap / modelTokens.Count, 0m, 1m));
        }

        private static HashSet<string> Tokenize(string text) => WordPattern.Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(w => w.Length > 1)
            .ToHashSet();
    }
}
