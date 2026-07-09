// IRAS.Application/Common/Options/AiServiceOptions.cs
namespace IRAS.Application.Common.Options
{
    public class AiServiceOptions
    {
        public const string SectionName = "AiService";

        public string BaseUrl { get; set; } = null!;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
