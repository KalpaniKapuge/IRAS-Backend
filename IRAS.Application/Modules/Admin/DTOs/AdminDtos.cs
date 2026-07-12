// IRAS.Application/Modules/Admin/DTOs/AdminDtos.cs
namespace IRAS.Application.Modules.Admin.DTOs
{
    public class UserSummaryDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SetUserActiveRequest
    {
        public bool IsActive { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalCandidates { get; set; }
        public int TotalEmployers { get; set; }
        public int TotalJobs { get; set; }
        public int PublishedJobs { get; set; }
        public int TotalApplications { get; set; }
        public Dictionary<string, int> ApplicationsByStatus { get; set; } = new();
        public int TotalResumes { get; set; }
        public int ParsedResumes { get; set; }
        public int FailedResumes { get; set; }
        public decimal AverageApplicationScore { get; set; }
        public int TotalSkillGapsDetected { get; set; }
        public List<TopSkillGapDto> TopMissingSkills { get; set; } = new();
        public int TotalJobMatches { get; set; }
        public int PendingFeedbackReviews { get; set; }
    }

    public class TopSkillGapDto
    {
        public string SkillName { get; set; } = null!;
        public int Occurrences { get; set; }
    }

    // "AI Model Monitoring" in the Admin workflow — liveness of the Python AI service
    // plus resume-parsing outcome counts, the closest proxy this system has for AI
    // performance without a dedicated model-metrics pipeline.
    public class AiModelStatusDto
    {
        public bool AiServiceOnline { get; set; }
        public string AiServiceBaseUrl { get; set; } = null!;
        public int TotalResumesParsed { get; set; }
        public int TotalResumesFailed { get; set; }
        public decimal ParseSuccessRate { get; set; }
    }

    // "System Settings" in the Admin workflow — read-only visibility into the current
    // effective configuration (scoring weights, AI service, file storage limits).
    // Deliberately read-only: these are bound via IValidateOptions.ValidateOnStart(),
    // so runtime mutation would need a DB-backed settings store, not a quick add-on.
    public class SystemSettingsDto
    {
        public decimal SkillMatchWeight { get; set; }
        public decimal SemanticSimilarityWeight { get; set; }
        public decimal AutoMatchThreshold { get; set; }
        public string AiServiceBaseUrl { get; set; } = null!;
        public int AiServiceTimeoutSeconds { get; set; }
        public long MaxResumeFileSizeBytes { get; set; }
        public int MaxResumesPerCandidate { get; set; }
    }
}
