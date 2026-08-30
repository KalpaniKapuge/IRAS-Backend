// IRAS.Application/Modules/SkillImprovementPlans/SkillPlanEvidenceService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Audit;
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
        private const string EntityType = "SkillPlanEvidence";

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
        private readonly IAuditLogService _audit;

        public SkillPlanEvidenceService(
            IrasDbContext db, IFileStorage storage, IEvidenceReviewer reviewer,
            IOptions<EvidenceReviewOptions> reviewOptions, IAuditLogService audit)
        {
            _db = db;
            _storage = storage;
            _reviewer = reviewer;
            _reviewOptions = reviewOptions.Value;
            _audit = audit;
        }

        public async Task<SkillPlanEvidenceDto> AddEvidenceLinkAsync(
            int candidateId, int planId, AddEvidenceLinkRequest request, CancellationToken ct)
        {
            await EnsurePlanOwnedAsync(candidateId, planId, ct);

            var evidence = new SkillPlanEvidence
            {
                PlanId = planId,
                EvidenceType = ParseEnum<SkillEvidenceType>(request.EvidenceType, nameof(request.EvidenceType)),
                EvidenceUrl = request.EvidenceUrl.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                VerificationStatus = EvidenceVerificationStatus.Draft
            };

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
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                VerificationStatus = EvidenceVerificationStatus.Draft
            };
            _db.SkillPlanEvidence.Add(evidence);
            await _db.SaveChangesAsync(ct);
            return MapToDto(evidence);
        }

        public async Task<SkillPlanEvidenceDto> SubmitEvidenceForReviewAsync(
            int candidateId, int planId, int evidenceId, CancellationToken ct)
        {
            var evidence = await _db.SkillPlanEvidence
                .Include(e => e.Plan).ThenInclude(p => p.Skill)
                .Include(e => e.Plan).ThenInclude(p => p.Steps)
                .FirstOrDefaultAsync(e => e.EvidenceId == evidenceId && e.PlanId == planId
                    && e.Plan.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Evidence not found.");

            if (evidence.VerificationStatus != EvidenceVerificationStatus.Draft)
                throw new ArgumentException("This evidence has already been submitted for review.");

            if (FileBackedTypes.Contains(evidence.EvidenceType))
            {
                // No automatic review capability for files/screenshots (see IEvidenceReviewer)
                // — submitting always hands it straight to the admin queue.
                evidence.VerificationStatus = EvidenceVerificationStatus.Pending;
            }
            else
            {
                var review = await _reviewer.ReviewAsync(
                    evidence.Plan.Skill.SkillName, evidence.Plan.ProjectTitle, evidence.Plan.ProjectTask,
                    evidence.Plan.ProjectExpectedOutput, evidence.EvidenceType.ToString(), evidence.EvidenceUrl,
                    evidence.Notes, ct);

                evidence.AiConfidenceScore = review.ConfidenceScore;
                evidence.AiRationale = review.Rationale;

                if (review.ConfidenceScore >= _reviewOptions.AutoApproveThreshold)
                {
                    evidence.VerificationStatus = EvidenceVerificationStatus.Approved;
                    evidence.VerifiedAt = DateTime.UtcNow;
                    evidence.AutoReviewed = true;

                    if (evidence.Plan.Steps.Count > 0 && evidence.Plan.Steps.All(s => s.IsCompleted))
                        await PromoteToVerifiedAsync(evidence.Plan, ct);
                }
                else if (review.ConfidenceScore <= _reviewOptions.AutoRejectThreshold)
                {
                    evidence.VerificationStatus = EvidenceVerificationStatus.Rejected;
                    evidence.VerifiedAt = DateTime.UtcNow;
                    evidence.AutoReviewed = true;
                }
                else
                {
                    // Genuinely ambiguous middle band — still reaches a human.
                    evidence.VerificationStatus = EvidenceVerificationStatus.Pending;
                }
            }

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
                .Include(e => e.Plan).ThenInclude(p => p.Job)
                .Include(e => e.Plan).ThenInclude(p => p.Steps)
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
                    JobTitle = e.Plan.Job != null ? e.Plan.Job.Title : null,
                    PlanOverview = e.Plan.Overview,
                    ProjectTitle = e.Plan.ProjectTitle,
                    ProjectTask = e.Plan.ProjectTask,
                    ProjectExpectedOutput = e.Plan.ProjectExpectedOutput,
                    EvidenceType = e.EvidenceType.ToString(),
                    EvidenceUrl = e.EvidenceUrl,
                    Notes = e.Notes,
                    UploadedAt = e.UploadedAt,
                    VerificationStatus = e.VerificationStatus.ToString(),
                    AiConfidenceScore = e.AiConfidenceScore,
                    AiRationale = e.AiRationale,
                    StepsCompleted = e.Plan.Steps.Count(s => s.IsCompleted),
                    TotalSteps = e.Plan.Steps.Count
                })
                .ToListAsync(ct);
        }

        public async Task<SkillPlanEvidenceDto> VerifyEvidenceAsync(
            int adminId, int evidenceId, VerifyEvidenceRequest request, CancellationToken ct)
        {
            var decision = ParseEnum<VerificationDecision>(request.Decision, nameof(request.Decision));

            var evidence = await _db.SkillPlanEvidence
                .Include(e => e.Plan).ThenInclude(p => p.Steps)
                .FirstOrDefaultAsync(e => e.EvidenceId == evidenceId, ct)
                ?? throw new KeyNotFoundException("Evidence not found.");

            if (evidence.VerificationStatus != EvidenceVerificationStatus.Pending)
                throw new ArgumentException(
                    "Only evidence that is Pending review can be decided on. It may be a Draft the candidate " +
                    "hasn't submitted yet, or a submission that's already been decided.");

            evidence.VerificationStatus = decision switch
            {
                VerificationDecision.Approve => EvidenceVerificationStatus.Approved,
                VerificationDecision.Reject => EvidenceVerificationStatus.Rejected,
                VerificationDecision.RequestRevision => EvidenceVerificationStatus.RevisionRequired,
                _ => throw new ArgumentException($"Unhandled decision '{decision}'.")
            };
            evidence.VerifiedBy = adminId;
            evidence.VerifiedAt = DateTime.UtcNow;
            evidence.VerifierNotes = string.IsNullOrWhiteSpace(request.VerifierNotes) ? null : request.VerifierNotes.Trim();

            // Promote to Verified — and sync the candidate's real skill record — only once
            // the roadmap is fully complete AND this evidence was approved. A rejection or
            // revision request is feedback on the evidence, not a verdict on the candidate's
            // underlying progress, so the plan's step-derived status is left untouched.
            if (decision == VerificationDecision.Approve
                && evidence.Plan.Steps.Count > 0 && evidence.Plan.Steps.All(s => s.IsCompleted))
                await PromoteToVerifiedAsync(evidence.Plan, ct);

            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                adminId,
                decision switch
                {
                    VerificationDecision.Approve => "SkillPlanEvidenceApproved",
                    VerificationDecision.Reject => "SkillPlanEvidenceRejected",
                    _ => "SkillPlanEvidenceRevisionRequested"
                },
                EntityType, evidenceId, ct);

            return MapToDto(evidence);
        }

        // Runs once, from whichever path first satisfies "roadmap complete + evidence
        // approved" — manual admin approval or the AI auto-approve branch. Marks the plan
        // Verified, closes out the matching CandidateTargetSkill, and upserts the candidate's
        // real CandidateSkill row so future job matching (which reads CandidateSkill live —
        // see ApplicationService/JobMatchingService) immediately reflects the verified skill.
        private async Task PromoteToVerifiedAsync(SkillImprovementPlan plan, CancellationToken ct)
        {
            plan.Status = SkillPlanStatus.Verified;

            var targetSkill = await _db.CandidateTargetSkills
                .FirstOrDefaultAsync(t => t.CandidateId == plan.CandidateId && t.SkillId == plan.SkillId, ct);
            if (targetSkill != null)
            {
                targetSkill.Status = TargetSkillStatus.Completed;
                targetSkill.CompletedAt = DateTime.UtcNow;
            }

            var proficiency = plan.TargetLevel switch
            {
                SkillTargetLevel.Beginner => ProficiencyLevel.Beginner,
                SkillTargetLevel.Intermediate => ProficiencyLevel.Intermediate,
                _ => ProficiencyLevel.Advanced
            };

            var candidateSkill = await _db.CandidateSkills
                .FirstOrDefaultAsync(cs => cs.CandidateId == plan.CandidateId && cs.SkillId == plan.SkillId, ct);
            if (candidateSkill == null)
            {
                _db.CandidateSkills.Add(new CandidateSkill
                {
                    CandidateId = plan.CandidateId,
                    SkillId = plan.SkillId,
                    Proficiency = proficiency,
                    YearsExp = 0,
                    Source = SkillSource.VerifiedImprovement,
                    IsVerified = true
                });
            }
            else
            {
                candidateSkill.IsVerified = true;
                candidateSkill.Source = SkillSource.VerifiedImprovement;
                // Never downgrade a proficiency the candidate already had self-reported (or
                // previously verified) higher than what this plan's target level implies.
                if (proficiency > candidateSkill.Proficiency)
                    candidateSkill.Proficiency = proficiency;
            }
        }

        private enum VerificationDecision { Approve, Reject, RequestRevision }

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
