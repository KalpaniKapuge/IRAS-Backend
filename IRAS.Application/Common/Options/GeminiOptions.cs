// IRAS.Application/Common/Options/GeminiOptions.cs
namespace IRAS.Application.Common.Options
{
    // ApiKey is deliberately not required here — never meant to live in a checked-in
    // appsettings file. Leave it unset and the generator falls back to the
    // GEMINI_API_KEY environment variable (set via `dotnet user-secrets` in dev).
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string? ApiKey { get; set; }
        public string Model { get; set; } = "gemini-3.6-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    }
}
