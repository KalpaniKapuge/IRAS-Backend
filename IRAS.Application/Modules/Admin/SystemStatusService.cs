// IRAS.Application/Modules/Admin/SystemStatusService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Ai;
using IRAS.Application.Common.Options;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Modules.Admin.DTOs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Admin
{
    public class SystemStatusService : ISystemStatusService
    {
        private readonly IrasDbContext _db;
        private readonly IAiServiceClient _aiClient;
        private readonly AiServiceOptions _aiOptions;
        private readonly ScoringOptions _scoringOptions;
        private readonly FileStorageOptions _fileStorageOptions;

        public SystemStatusService(
            IrasDbContext db, IAiServiceClient aiClient,
            IOptions<AiServiceOptions> aiOptions, IOptions<ScoringOptions> scoringOptions,
            IOptions<FileStorageOptions> fileStorageOptions)
        {
            _db = db;
            _aiClient = aiClient;
            _aiOptions = aiOptions.Value;
            _scoringOptions = scoringOptions.Value;
            _fileStorageOptions = fileStorageOptions.Value;
        }

        public async Task<AiModelStatusDto> GetAiModelStatusAsync(CancellationToken ct)
        {
            var online = await _aiClient.CheckHealthAsync(ct);

            var parsed = await _db.Resumes.CountAsync(
                r => r.ParseStatus == ParseStatus.Parsed || r.ParseStatus == ParseStatus.ManuallyEdited, ct);
            var failed = await _db.Resumes.CountAsync(r => r.ParseStatus == ParseStatus.Failed, ct);
            var total = parsed + failed;

            return new AiModelStatusDto
            {
                AiServiceOnline = online,
                AiServiceBaseUrl = _aiOptions.BaseUrl,
                TotalResumesParsed = parsed,
                TotalResumesFailed = failed,
                ParseSuccessRate = total > 0 ? Math.Round((decimal)parsed / total, 4) : 0m
            };
        }

        public SystemSettingsDto GetSystemSettings() => new()
        {
            SkillMatchWeight = _scoringOptions.SkillMatchWeight,
            SemanticSimilarityWeight = _scoringOptions.SemanticSimilarityWeight,
            AutoMatchThreshold = _scoringOptions.AutoMatchThreshold,
            AiServiceBaseUrl = _aiOptions.BaseUrl,
            AiServiceTimeoutSeconds = _aiOptions.TimeoutSeconds,
            MaxResumeFileSizeBytes = _fileStorageOptions.MaxFileSizeBytes,
            MaxResumesPerCandidate = _fileStorageOptions.MaxResumesPerCandidate
        };
    }
}
