// IRAS.Application/Modules/SkillImprovementPlans/SkillPlanEvidenceService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;
using IRAS.Application.Common.Storage;
using IRAS.Application.Modules.SkillImprovementPlans.DTOs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    public class SkillPlanEvidenceService : ISkillPlanEvidenceService
    {
        private const long MaxEvidenceBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> EvidenceExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx", ".zip"
        };

        private static readonly HashSet<string> EvidenceContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf", "image/jpeg", "image/png", "image/webp",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/zip", "application/x-zip-compressed"
        };

        private static readonly HashSet<SkillEvidenceType> FileBackedTypes =
            [SkillEvidenceType.File, SkillEvidenceType.Screenshot, SkillEvidenceType.Certificate];

        private readonly IrasDbContext _db;
        private readonly IFileStorage _storage;
        private readonly IEvidenceReviewer _reviewer;
        private readonly EvidenceReviewOptions _reviewOptions;

        public SkillPlanEvidenceService(
            IrasDbContext db, IFileStorage storage, IEvidenceReviewer reviewer, IOptions<EvidenceReviewOptions> reviewOptions)
        {
            _db = db;
            _storage = storage;
            _reviewer = reviewer;
            _reviewOptions = reviewOptions.Value;
        }

        public async Task<SkillPlanEvidenceDto> AddEvidenceLinkAsync(
            int candidateId, int planId, AddEvidenceLinkRequest request, CancellationToken ct)
        {
            var plan = await _db.SkillImprovementPlans
                .Include(p => p.Skill).Include(p => p.Steps)
                .FirstOrDefaultAsync(p => p.PlanId == planId && p.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Skill improvement plan not found.");

            var evidenceType = ParseEnum<SkillEvidenceType>(request.EvidenceType, nameof(request.EvidenceType));
            var evidenceUrl = request.EvidenceUrl.Trim();
            var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            var evidence = new SkillPlanEvidence
            {
                PlanId = planId,
                EvidenceType = evidenceType,
                EvidenceUrl = evidenceUrl,
                Notes = notes
            };

            // Automatic triage — link-type evidence only (see IEvidenceReviewer for why
            // file-backed types skip this and always land in the admin queue).
            var review = await _reviewer.ReviewAsync(
                plan.Skill.SkillName, plan.ProjectTitle, plan.ProjectTask, plan.ProjectExpectedOutput,
                evidenceType.ToString(), evidenceUrl, notes, ct);

            evidence.AiConfidenceScore = review.ConfidenceScore;
            evidence.AiRationale = review.Rationale;

            if (review.ConfidenceScore >= _reviewOptions.AutoApproveThreshold)
            {
                evidence.VerificationStatus = EvidenceVerificationStatus.Approved;
                evidence.VerifiedAt = DateTime.UtcNow;
                evidence.AutoReviewed = true;

                if (plan.Steps.Count > 0 && plan.Steps.All(s => s.IsCompleted))
                    plan.Status = SkillPlanStatus.Verified;
            }
            else if (review.ConfidenceScore <= _reviewOptions.AutoRejectThreshold)
            {
                evidence.VerificationStatus = EvidenceVerificationStatus.Rejected;
                evidence.VerifiedAt = DateTime.UtcNow;
                evidence.AutoReviewed = true;
            }
            // else: stays Pending — the genuinely ambiguous middle band still reaches a human.

            _db.SkillPlanEvidence.Add(evidence);
            await _db.SaveChangesAsync(ct);
            return MapToDto(evidence);
        }

        public async Task<SkillPlanEvidenceDto> AddEvidenceFileAsync(
            int candidateId, int planId, AddEvidenceFileRequest request, CancellationToken ct)
        {
            await EnsurePlanOwnedAsync(candidateId, planId, ct);

            var file = request.File ?? throw new ArgumentException("Evidence file is required.");
            ValidateEvidenceFile(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = $"{Guid.NewGuid():N}{extension}";

            string url;
            await using (var stream = file.OpenReadStream())
                url = await _storage.SaveAsync(stream, $"skill-plans/{candidateId}/{planId}/evidence", storedName, ct);

            var evidence = new SkillPlanEvidence
            {
                PlanId = planId,
                EvidenceType = ParseEnum<SkillEvidenceType>(request.EvidenceType, nameof(request.EvidenceType)),
                EvidenceUrl = url,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };
            _db.SkillPlanEvidence.Add(evidence);
            await _db.SaveChangesAsync(ct);
            return MapToDto(evidence);
        }

        public async Task RemoveEvidenceAsync(int candidateId, int planId, int evidenceId, CancellationToken ct)
        {
            await EnsurePlanOwnedAsync(candidateId, planId, ct);

            var evidence = await _db.SkillPlanEvidence
                .FirstOrDefaultAsync(e => e.EvidenceId == evidenceId && e.PlanId == planId, ct)
                ?? throw new KeyNotFoundException("Evidence not found.");

            _db.SkillPlanEvidence.Remove(evidence);
            await _db.SaveChangesAsync(ct);

            // Link-type evidence (GitHub/Other) points at an external URL — nothing of ours
            // to clean up in storage. File-backed types own an uploaded object.
            if (FileBackedTypes.Contains(evidence.EvidenceType))
                await _storage.DeleteAsync(evidence.EvidenceUrl, ct);
        }

        public async Task<List<AdminEvidenceReviewDto>> GetPendingEvidenceAsync(CancellationToken ct)
        {
            return await _db.SkillPlanEvidence
                .Include(e => e.Plan).ThenInclude(p => p.Skill)
                .Include(e => e.Plan).ThenInclude(p => p.Candidate)
                .Where(e => e.VerificationStatus == EvidenceVerificationStatus.Pending)
                .OrderBy(e => e.UploadedAt)
                .Select(e => new AdminEvidenceReviewDto
                {
                    EvidenceId = e.EvidenceId,
                    PlanId = e.PlanId,
                    CandidateId = e.Plan.CandidateId,
                    CandidateName = e.Plan.Candidate.FirstName + " " + e.Plan.Candidate.LastName,
                    SkillId = e.Plan.SkillId,
                    SkillName = e.Plan.Skill.SkillName,
                    EvidenceType = e.EvidenceType.ToString(),
                    EvidenceUrl = e.EvidenceUrl,
                    Notes = e.Notes,
                    UploadedAt = e.UploadedAt,
                    VerificationStatus = e.VerificationStatus.ToString(),
                    AiConfidenceScore = e.AiConfidenceScore,
                    AiRationale = e.AiRationale
                })
                .ToListAsync(ct);
        }

        public async Task<SkillPlanEvidenceDto> VerifyEvidenceAsync(
            int adminId, int evidenceId, VerifyEvidenceRequest request, CancellationToken ct)
        {
            var evidence = await _db.SkillPlanEvidence
                .Include(e => e.Plan).ThenInclude(p => p.Steps)
                .FirstOrDefaultAsync(e => e.EvidenceId == evidenceId, ct)
                ?? throw new KeyNotFoundException("Evidence not found.");

            evidence.VerificationStatus = request.Approved
                ? EvidenceVerificationStatus.Approved
                : EvidenceVerificationStatus.Rejected;
            evidence.VerifiedBy = adminId;
            evidence.VerifiedAt = DateTime.UtcNow;
            evidence.VerifierNotes = string.IsNullOrWhiteSpace(request.VerifierNotes) ? null : request.VerifierNotes.Trim();

            // Promote to Verified only once the roadmap is fully complete AND this evidence
            // was approved. A rejection is feedback on the evidence, not a verdict on the
            // candidate's underlying progress, so the plan's step-derived status is untouched.
            if (request.Approved && evidence.Plan.Steps.Count > 0 && evidence.Plan.Steps.All(s => s.IsCompleted))
                evidence.Plan.Status = SkillPlanStatus.Verified;

            await _db.SaveChangesAsync(ct);
            return MapToDto(evidence);
        }

        private async Task EnsurePlanOwnedAsync(int candidateId, int planId, CancellationToken ct)
        {
            var exists = await _db.SkillImprovementPlans.AnyAsync(p => p.PlanId == planId && p.CandidateId == candidateId, ct);
            if (!exists) throw new KeyNotFoundException("Skill improvement plan not found.");
        }

        private static void ValidateEvidenceFile(IFormFile file)
        {
            if (file.Length == 0)
                throw new ArgumentException("Evidence file is empty.");

            if (file.Length > MaxEvidenceBytes)
                throw new ArgumentException($"Evidence file exceeds the {MaxEvidenceBytes / 1024 / 1024} MB limit.");

            var extension = Path.GetExtension(file.FileName);
            if (!EvidenceExtensions.Contains(extension))
                throw new ArgumentException("Evidence file has an unsupported file extension.");

            if (!EvidenceContentTypes.Contains(file.ContentType))
                throw new ArgumentException("Evidence file has an unsupported content type.");
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }

        private static SkillPlanEvidenceDto MapToDto(SkillPlanEvidence e) => new()
        {
            EvidenceId = e.EvidenceId,
            PlanId = e.PlanId,
            EvidenceType = e.EvidenceType.ToString(),
            EvidenceUrl = e.EvidenceUrl,
            Notes = e.Notes,
            UploadedAt = e.UploadedAt,
            VerificationStatus = e.VerificationStatus.ToString(),
            VerifiedAt = e.VerifiedAt,
            VerifierNotes = e.VerifierNotes,
            AiConfidenceScore = e.AiConfidenceScore,
            AiRationale = e.AiRationale,
            AutoReviewed = e.AutoReviewed
        };
    }
}
