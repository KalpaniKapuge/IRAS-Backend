// IRAS.Application/Common/Options/FileStorageOptions.cs
namespace IRAS.Application.Common.Options
{
    public class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        public string Provider { get; set; } = "Local";
        public string ResumeRootPath { get; set; } = null!;
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        public int MaxResumesPerCandidate { get; set; } = 5;
        public string? SupabaseUrl { get; set; }
        public string? SupabaseServiceRoleKey { get; set; }
        public string? SupabaseBucket { get; set; }
        public string? SupabasePublicBaseUrl { get; set; }
    }
}
