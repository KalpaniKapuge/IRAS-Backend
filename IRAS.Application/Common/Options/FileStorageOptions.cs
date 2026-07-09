// IRAS.Application/Common/Options/FileStorageOptions.cs
namespace IRAS.Application.Common.Options
{
    public class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        public string ResumeRootPath { get; set; } = null!;
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        public int MaxResumesPerCandidate { get; set; } = 5;
    }
}
