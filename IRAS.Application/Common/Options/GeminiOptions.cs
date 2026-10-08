// IRAS.Application/Common/Options/GeminiOptions.cs
namespace IRAS.Application.Common.Options
{
    // ApiKey is deliberately not required here — never meant to live in a checked-in
    // appsettings file. Leave it unset and the generator falls back to the
    // GEMINI_API_KEY environment variable (set via `dotnet user-secrets` in dev).
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";
        private const string DefaultModel = "gemini-2.5-flash";
        private string _model = DefaultModel;

        public string? ApiKey { get; set; }
        public string Model
        {
            get => NormalizeModelName(_model);
            set => _model = value;
        }
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

        private static string NormalizeModelName(string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return DefaultModel;

            var trimmed = model.Trim();
            return trimmed.Equals("gemini-3.6-flash", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Equals("gemini-3.5-flash", StringComparison.OrdinalIgnoreCase)
                ? DefaultModel
                : trimmed;
        }
    }
}
