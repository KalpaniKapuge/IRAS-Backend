// IRAS.Application/Common/Options/OpenAiOptions.cs
namespace IRAS.Application.Common.Options
{
    // ApiKey is deliberately not required here — never meant to live in a checked-in
    // appsettings file. Leave it unset and the client falls back to the OPENAI_API_KEY
    // environment variable (set via `dotnet user-secrets` in dev).
    public class OpenAiOptions
    {
        public const string SectionName = "OpenAI";

        public string? ApiKey { get; set; }
        public string Model { get; set; } = "gpt-5.6-terra";
    }
}
