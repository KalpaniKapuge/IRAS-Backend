// IRAS.Application/Common/Options/AnthropicOptions.cs
namespace IRAS.Application.Common.Options
{
    // ApiKey is deliberately not required here — it's never meant to live in a
    // checked-in appsettings file. Leave it unset and the client falls back to the
    // ANTHROPIC_API_KEY environment variable (set via `dotnet user-secrets` in dev).
    public class AnthropicOptions
    {
        public const string SectionName = "Anthropic";

        public string? ApiKey { get; set; }
        public string Model { get; set; } = "claude-opus-4-8";
    }
}
