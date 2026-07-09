// IRAS.Application/Modules/Resumes/ResumeService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Ai;
using IRAS.Application.Common.Options;
using IRAS.Application.Common.Storage;
using IRAS.Application.Modules.Resumes.DTOs;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Resumes
{
    public class ResumeService : IResumeService
    {
        // Magic bytes: %PDF and PK (zip container, which DOCX is).
        private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 };
        private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };

        private readonly IrasDbContext _db;
        private readonly IFileStorage _storage;
        private readonly IAiServiceClient _ai;
        private readonly FileStorageOptions _options;
        private readonly ILogger<ResumeService> _logger;

        public ResumeService(
            IrasDbContext db, IFileStorage storage, IAiServiceClient ai,
            IOptions<FileStorageOptions> options, ILogger<ResumeService> logger)
        {
            _db = db;
            _storage = storage;
            _ai = ai;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<List<ResumeDto>> GetMyResumesAsync(int candidateId, CancellationToken ct)
        {
            return await _db.Resumes
                .Where(r => r.CandidateId == candidateId)
                .OrderByDescending(r => r.UploadedAt)
                .Select(r => new ResumeDto
                {
                    ResumeId = r.ResumeId,
                    FileFormat = r.FileFormat.ToString(),
                    IsPrimary = r.IsPrimary,
                    ParseStatus = r.ParseStatus.ToString(),
                    ParseError = r.ParseError,
                    UploadedAt = r.UploadedAt
                })
                .ToListAsync(ct);
        }

        public async Task<ParseResultDto> UploadAndParseAsync(int candidateId, IFormFile file, CancellationToken ct)
        {
            await EnsureCandidateExistsAsync(candidateId, ct);

            var resumeCount = await _db.Resumes.CountAsync(r => r.CandidateId == candidateId, ct);
            if (resumeCount >= _options.MaxResumesPerCandidate)
                throw new InvalidOperationException(
                    $"Maximum of {_options.MaxResumesPerCandidate} resumes reached. Delete one first.");

            var format = await ValidateFileAsync(file, ct);

            // 1. Persist file + Pending row first: whatever happens to parsing,
            //    the candidate's upload is never lost.
            var storedName = $"{Guid.NewGuid():N}.{format.ToString().ToLowerInvariant()}";
            string storedPath;
            await using (var stream = file.OpenReadStream())
            {
                storedPath = await _storage.SaveAsync(stream, candidateId.ToString(), storedName, ct);
            }

            var resume = new Resume
            {
                CandidateId = candidateId,
                FileUrl = storedPath,
                FileFormat = format,
                IsPrimary = resumeCount == 0,   // first upload becomes primary
                ParseStatus = ParseStatus.Pending
            };
            _db.Resumes.Add(resume);
            await _db.SaveChangesAsync(ct);

            // 2. Parse (state transitions handled inside)
            return await ParseAndPersistAsync(resume, ct);
        }

        public async Task<ParseResultDto> RetryParseAsync(int candidateId, int resumeId, CancellationToken ct)
        {
            var resume = await GetOwnedResumeAsync(candidateId, resumeId, ct);
            if (resume.ParseStatus == ParseStatus.Parsed)
                throw new InvalidOperationException("Resume is already parsed.");

            return await ParseAndPersistAsync(resume, ct);
        }

        public async Task ConfirmSkillsAsync(int candidateId, int resumeId, ConfirmSkillsRequest request, CancellationToken ct)
        {
            var resume = await GetOwnedResumeAsync(candidateId, resumeId, ct);
            if (resume.ParseStatus != ParseStatus.Parsed)
                throw new InvalidOperationException("Cannot confirm skills for an unparsed resume.");

            var validIds = await _db.Skills
                .Where(s => request.SkillIds.Contains(s.SkillId))
                .Select(s => s.SkillId)
                .ToListAsync(ct);
            var invalid = request.SkillIds.Except(validIds).ToList();
            if (invalid.Count > 0)
                throw new KeyNotFoundException($"Unknown skill id(s): {string.Join(", ", invalid)}");

            var existing = await _db.CandidateSkills
                .Where(cs => cs.CandidateId == candidateId)
                .Select(cs => cs.SkillId)
                .ToListAsync(ct);

            foreach (var skillId in validIds.Except(existing))
            {
                _db.CandidateSkills.Add(new CandidateSkill
                {
                    CandidateId = candidateId,
                    SkillId = skillId,
                    Proficiency = ProficiencyLevel.Intermediate,   // sensible default; candidate can edit
                    YearsExp = 0,
                    Source = SkillSource.ResumeParsed,
                    IsVerified = false
                });
            }

            resume.ParseStatus = ParseStatus.ManuallyEdited;   // confirmation recorded
            await _db.SaveChangesAsync(ct);
        }

        public async Task SetPrimaryAsync(int candidateId, int resumeId, CancellationToken ct)
        {
            var resume = await GetOwnedResumeAsync(candidateId, resumeId, ct);

            var current = await _db.Resumes
                .Where(r => r.CandidateId == candidateId && r.IsPrimary)
                .ToListAsync(ct);
            foreach (var r in current) r.IsPrimary = false;

            resume.IsPrimary = true;
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int candidateId, int resumeId, CancellationToken ct)
        {
            var resume = await GetOwnedResumeAsync(candidateId, resumeId, ct);

            var usedByApplications = await _db.Applications.AnyAsync(a => a.ResumeId == resumeId, ct);
            if (usedByApplications)
                throw new InvalidOperationException(
                    "This resume was used in job applications and cannot be deleted.");

            _db.Resumes.Remove(resume);
            await _db.SaveChangesAsync(ct);
            await _storage.DeleteAsync(resume.FileUrl, ct);   // DB first, then disk
        }

        // ---- internals ----

        private async Task<ParseResultDto> ParseAndPersistAsync(Resume resume, CancellationToken ct)
        {
            var taxonomy = await _db.Skills
                .Include(s => s.Aliases)
                .Select(s => new TaxonomyItem(
                    s.SkillId, s.SkillName, s.Aliases.Select(a => a.AliasText).ToList()))
                .ToListAsync(ct);

            ParseResumeResult result;
            await using (var stream = await _storage.OpenReadAsync(resume.FileUrl, ct))
            {
                result = await _ai.ParseResumeAsync(
                    stream, Path.GetFileName(resume.FileUrl), resume.FileFormat.ToString(), taxonomy, ct);
            }

            if (!result.Success)
            {
                resume.ParseStatus = ParseStatus.Failed;
                resume.ParseError = result.Error;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Parse failed for resume {ResumeId}: {Error}", resume.ResumeId, result.Error);
                return new ParseResultDto
                {
                    ResumeId = resume.ResumeId,
                    ParseStatus = resume.ParseStatus.ToString(),
                    ParseError = resume.ParseError
                };
            }

            resume.ParsedText = result.ParsedText;
            resume.ParseStatus = ParseStatus.Parsed;
            resume.ParseError = null;
            await _db.SaveChangesAsync(ct);

            var profileSkillIds = await _db.CandidateSkills
                .Where(cs => cs.CandidateId == resume.CandidateId)
                .Select(cs => cs.SkillId)
                .ToListAsync(ct);

            return new ParseResultDto
            {
                ResumeId = resume.ResumeId,
                ParseStatus = resume.ParseStatus.ToString(),
                SuggestedSkills = result.DetectedSkills.Select(s => new SuggestedSkillDto
                {
                    SkillId = s.SkillId,
                    SkillName = s.SkillName,
                    MatchedText = s.MatchedText,
                    Occurrences = s.Occurrences,
                    AlreadyOnProfile = profileSkillIds.Contains(s.SkillId)
                }).ToList(),
                DetectedEmails = result.Emails,
                DetectedPhones = result.Phones
            };
        }

        private async Task<ResumeFormat> ValidateFileAsync(IFormFile file, CancellationToken ct)
        {
            if (file.Length == 0)
                throw new ArgumentException("File is empty.");
            if (file.Length > _options.MaxFileSizeBytes)
                throw new ArgumentException($"File exceeds the {_options.MaxFileSizeBytes / 1024 / 1024} MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var declared = ext switch
            {
                ".pdf" => ResumeFormat.PDF,
                ".docx" => ResumeFormat.DOCX,
                _ => throw new ArgumentException("Only PDF and DOCX files are supported.")
            };

            // Verify magic bytes: a .pdf that doesn't start with %PDF is lying.
            var header = new byte[4];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAtLeastAsync(header, 4, throwOnEndOfStream: false, ct);
                if (read < 4) throw new ArgumentException("File is too small to be valid.");
            }

            var signatureOk = declared switch
            {
                ResumeFormat.PDF => header.AsSpan().StartsWith(PdfSignature),
                ResumeFormat.DOCX => header.AsSpan().StartsWith(ZipSignature),
                _ => false
            };
            if (!signatureOk)
                throw new ArgumentException("File content does not match its extension.");

            return declared;
        }

        private async Task EnsureCandidateExistsAsync(int candidateId, CancellationToken ct)
        {
            var exists = await _db.CandidateProfiles.AnyAsync(c => c.CandidateId == candidateId, ct);
            if (!exists) throw new KeyNotFoundException("Candidate profile not found.");
        }

        private async Task<Resume> GetOwnedResumeAsync(int candidateId, int resumeId, CancellationToken ct)
        {
            return await _db.Resumes
                .FirstOrDefaultAsync(r => r.ResumeId == resumeId && r.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Resume not found.");
        }
    }
}
