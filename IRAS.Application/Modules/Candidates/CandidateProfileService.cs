// IRAS.Application/Modules/Candidates/CandidateProfileService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using IRAS.Application.Common.Storage;
using IRAS.Application.Modules.Candidates.DTOs;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Candidates
{
    public class CandidateProfileService : ICandidateProfileService
    {
        private const long MaxProfilePictureBytes = 2 * 1024 * 1024;
        private const long MaxCertificateBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> ProfilePictureExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> ProfilePictureContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private static readonly HashSet<string> CertificateExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx"
        };

        private static readonly HashSet<string> CertificateContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/webp",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private readonly IrasDbContext _db;
        private readonly IFileStorage _storage;

        public CandidateProfileService(IrasDbContext db, IFileStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<CandidateProfileDto> GetProfileAsync(int candidateId)
        {
            var profile = await _db.CandidateProfiles
                .Include(c => c.Educations)
                .Include(c => c.WorkExperiences)
                .Include(c => c.Certifications)
                .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Candidate profile not found.");

            return MapToDto(profile);
        }

        public async Task UpdateProfileAsync(int candidateId, UpdateCandidateProfileRequest request)
        {
            var profile = await GetOwnedProfileAsync(candidateId);

            profile.FirstName = request.FirstName;
            profile.LastName = request.LastName;
            profile.Citizenship = request.Citizenship;
            profile.Phone = request.Phone;
            profile.Headline = request.Headline;
            profile.EducationLevel = ParseEnum<EducationLevel>(request.EducationLevel, nameof(request.EducationLevel));
            profile.OptInMatching = request.OptInMatching;

            await _db.SaveChangesAsync();
        }

        public async Task<CandidateProfileDto> UploadProfilePictureAsync(int candidateId, IFormFile file, CancellationToken ct)
        {
            var profile = await GetOwnedProfileAsync(candidateId, ct);
            ValidateUpload(file, MaxProfilePictureBytes, ProfilePictureExtensions, ProfilePictureContentTypes, "Profile picture");

            var oldPath = profile.ProfilePictureUrl;
            var storedPath = await SaveUploadAsync(file, $"candidate-profiles/{candidateId}/profile-picture", ct);

            profile.ProfilePictureUrl = storedPath;
            await _db.SaveChangesAsync(ct);

            await DeleteStoredFileIfPresentAsync(oldPath, ct);

            return await GetProfileAsync(candidateId);
        }

        public async Task<EducationDto> AddEducationAsync(int candidateId, EducationDto dto)
        {
            await GetOwnedProfileAsync(candidateId);

            var entity = new Education
            {
                CandidateId = candidateId,
                Degree = dto.Degree,
                Institution = dto.Institution,
                FieldOfStudy = dto.FieldOfStudy,
                StartYear = dto.StartYear,
                EndYear = dto.EndYear,
                Grade = dto.Grade
            };
            _db.Educations.Add(entity);
            await _db.SaveChangesAsync();
            dto.EducationId = entity.EducationId;
            return dto;
        }

        public async Task UpdateEducationAsync(int candidateId, int educationId, EducationDto dto)
        {
            var entity = await _db.Educations
                .FirstOrDefaultAsync(e => e.EducationId == educationId && e.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Education record not found.");

            entity.Degree = dto.Degree;
            entity.Institution = dto.Institution;
            entity.FieldOfStudy = dto.FieldOfStudy;
            entity.StartYear = dto.StartYear;
            entity.EndYear = dto.EndYear;
            entity.Grade = dto.Grade;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteEducationAsync(int candidateId, int educationId)
        {
            var entity = await _db.Educations
                .FirstOrDefaultAsync(e => e.EducationId == educationId && e.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Education record not found.");
            _db.Educations.Remove(entity);
            await _db.SaveChangesAsync();
        }

        // ---- Work experience: writes + derived-field recalculation share one transaction ----

        public async Task<WorkExperienceDto> AddWorkExperienceAsync(int candidateId, WorkExperienceDto dto)
        {
            await GetOwnedProfileAsync(candidateId);

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var entity = new WorkExperience
                {
                    CandidateId = candidateId,
                    CompanyName = dto.CompanyName,
                    JobTitle = dto.JobTitle,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsCurrent = dto.IsCurrent,
                    Description = dto.Description
                };
                _db.WorkExperiences.Add(entity);
                await _db.SaveChangesAsync();

                await RecalculateTotalExperienceAsync(candidateId);
                await tx.CommitAsync();

                dto.ExperienceId = entity.ExperienceId;
                return dto;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateWorkExperienceAsync(int candidateId, int experienceId, WorkExperienceDto dto)
        {
            var entity = await _db.WorkExperiences
                .FirstOrDefaultAsync(e => e.ExperienceId == experienceId && e.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Work experience record not found.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                entity.CompanyName = dto.CompanyName;
                entity.JobTitle = dto.JobTitle;
                entity.StartDate = dto.StartDate;
                entity.EndDate = dto.EndDate;
                entity.IsCurrent = dto.IsCurrent;
                entity.Description = dto.Description;
                await _db.SaveChangesAsync();

                await RecalculateTotalExperienceAsync(candidateId);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteWorkExperienceAsync(int candidateId, int experienceId)
        {
            var entity = await _db.WorkExperiences
                .FirstOrDefaultAsync(e => e.ExperienceId == experienceId && e.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Work experience record not found.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.WorkExperiences.Remove(entity);
                await _db.SaveChangesAsync();

                await RecalculateTotalExperienceAsync(candidateId);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<CertificationDto> AddCertificationAsync(int candidateId, CertificationDto dto)
        {
            await GetOwnedProfileAsync(candidateId);

            var entity = new Certification
            {
                CandidateId = candidateId,
                Name = dto.Name,
                IssuingOrg = dto.IssuingOrg,
                IssueDate = dto.IssueDate,
                ExpiryDate = dto.ExpiryDate
            };
            _db.Certifications.Add(entity);
            await _db.SaveChangesAsync();
            dto.CertificationId = entity.CertificationId;
            return dto;
        }

        public async Task<CertificationDto> AddCertificationAsync(int candidateId, CertificationUploadRequest request, CancellationToken ct)
        {
            await GetOwnedProfileAsync(candidateId, ct);

            var file = request.File ?? request.CertificateFile;
            string? storedPath = null;
            if (file is not null)
            {
                ValidateCertificateFile(file);
                storedPath = await SaveUploadAsync(file, $"candidate-profiles/{candidateId}/certifications", ct);
            }

            var entity = new Certification
            {
                CandidateId = candidateId,
                Name = request.Name,
                IssuingOrg = request.IssuingOrg,
                IssueDate = request.IssueDate,
                ExpiryDate = request.ExpiryDate,
                CertificateFileUrl = storedPath,
                CertificateFileName = file?.FileName,
                CertificateContentType = file?.ContentType
            };
            _db.Certifications.Add(entity);
            await _db.SaveChangesAsync(ct);

            return MapCertification(entity);
        }

        public async Task<CertificationDto> UploadCertificationFileAsync(
            int candidateId, int certificationId, IFormFile file, CancellationToken ct)
        {
            var entity = await _db.Certifications
                .FirstOrDefaultAsync(c => c.CertificationId == certificationId && c.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Certification record not found.");

            ValidateCertificateFile(file);

            var oldPath = entity.CertificateFileUrl;
            var storedPath = await SaveUploadAsync(file, $"candidate-profiles/{candidateId}/certifications", ct);

            entity.CertificateFileUrl = storedPath;
            entity.CertificateFileName = file.FileName;
            entity.CertificateContentType = file.ContentType;
            await _db.SaveChangesAsync(ct);

            await DeleteStoredFileIfPresentAsync(oldPath, ct);

            return MapCertification(entity);
        }

        public async Task DeleteCertificationAsync(int candidateId, int certificationId)
        {
            var entity = await _db.Certifications
                .FirstOrDefaultAsync(c => c.CertificationId == certificationId && c.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Certification record not found.");
            _db.Certifications.Remove(entity);
            await _db.SaveChangesAsync();
            await DeleteStoredFileIfPresentAsync(entity.CertificateFileUrl, CancellationToken.None);
        }

        public async Task UpsertSkillAsync(int candidateId, UpsertCandidateSkillRequest request)
        {
            await GetOwnedProfileAsync(candidateId);

            var skillExists = await _db.Skills.AnyAsync(s => s.SkillId == request.SkillId);
            if (!skillExists)
                throw new KeyNotFoundException("Skill not found in taxonomy. Ask admin to add it first.");

            var existing = await _db.CandidateSkills
                .FirstOrDefaultAsync(cs => cs.CandidateId == candidateId && cs.SkillId == request.SkillId);

            if (existing == null)
            {
                _db.CandidateSkills.Add(new CandidateSkill
                {
                    CandidateId = candidateId,
                    SkillId = request.SkillId,
                    Proficiency = ParseEnum<ProficiencyLevel>(request.Proficiency, nameof(request.Proficiency)),
                    YearsExp = request.YearsExp,
                    Source = SkillSource.ManuallyAdded,
                    IsVerified = false
                });
            }
            else
            {
                existing.Proficiency = ParseEnum<ProficiencyLevel>(request.Proficiency, nameof(request.Proficiency));
                existing.YearsExp = request.YearsExp;
            }
            await _db.SaveChangesAsync();
        }

        public async Task RemoveSkillAsync(int candidateId, int skillId)
        {
            var entity = await _db.CandidateSkills
                .FirstOrDefaultAsync(cs => cs.CandidateId == candidateId && cs.SkillId == skillId)
                ?? throw new KeyNotFoundException("Candidate does not have this skill listed.");
            _db.CandidateSkills.Remove(entity);
            await _db.SaveChangesAsync();
        }

        // ---- helpers ----

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }

        private async Task<CandidateProfile> GetOwnedProfileAsync(int candidateId)
        {
            return await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.CandidateId == candidateId)
                ?? throw new KeyNotFoundException("Candidate profile not found.");
        }

        private async Task<CandidateProfile> GetOwnedProfileAsync(int candidateId, CancellationToken ct)
        {
            return await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Candidate profile not found.");
        }

        private static void ValidateCertificateFile(IFormFile file)
        {
            ValidateUpload(file, MaxCertificateBytes, CertificateExtensions, CertificateContentTypes, "Certificate file");
        }

        private static void ValidateUpload(
            IFormFile file,
            long maxBytes,
            HashSet<string> allowedExtensions,
            HashSet<string> allowedContentTypes,
            string label)
        {
            if (file.Length == 0)
                throw new ArgumentException($"{label} is empty.");

            if (file.Length > maxBytes)
                throw new ArgumentException($"{label} exceeds the {maxBytes / 1024 / 1024} MB limit.");

            var extension = Path.GetExtension(file.FileName);
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"{label} has an unsupported file extension.");

            if (!allowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException($"{label} has an unsupported content type.");
        }

        private async Task<string> SaveUploadAsync(IFormFile file, string folder, CancellationToken ct)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = $"{Guid.NewGuid():N}{extension}";

            await using var stream = file.OpenReadStream();
            return await _storage.SaveAsync(stream, folder, storedName, ct);
        }

        private async Task DeleteStoredFileIfPresentAsync(string? storedPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return;

            await _storage.DeleteAsync(storedPath, ct);
        }

        private async Task RecalculateTotalExperienceAsync(int candidateId)
        {
            var experiences = await _db.WorkExperiences
                .Where(e => e.CandidateId == candidateId)
                .ToListAsync();

            double totalMonths = 0;
            foreach (var exp in experiences)
            {
                var end = exp.IsCurrent ? DateTime.UtcNow : (exp.EndDate ?? exp.StartDate);
                totalMonths += (end.Year - exp.StartDate.Year) * 12 + (end.Month - exp.StartDate.Month);
            }

            var profile = await GetOwnedProfileAsync(candidateId);
            profile.TotalExpYears = Math.Round((decimal)(totalMonths / 12.0), 1);
            await _db.SaveChangesAsync();
        }

        private static CandidateProfileDto MapToDto(CandidateProfile p) => new()
        {
            CandidateId = p.CandidateId,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Citizenship = p.Citizenship,
            Phone = p.Phone,
            Headline = p.Headline,
            ProfilePictureUrl = p.ProfilePictureUrl,
            TotalExpYears = p.TotalExpYears,
            EducationLevel = p.EducationLevel.ToString(),
            OptInMatching = p.OptInMatching,
            Educations = p.Educations.Select(e => new EducationDto
            {
                EducationId = e.EducationId, Degree = e.Degree, Institution = e.Institution,
                FieldOfStudy = e.FieldOfStudy, StartYear = e.StartYear, EndYear = e.EndYear, Grade = e.Grade
            }).ToList(),
            WorkExperiences = p.WorkExperiences.Select(w => new WorkExperienceDto
            {
                ExperienceId = w.ExperienceId, CompanyName = w.CompanyName, JobTitle = w.JobTitle,
                StartDate = w.StartDate, EndDate = w.EndDate, IsCurrent = w.IsCurrent, Description = w.Description
            }).ToList(),
            Certifications = p.Certifications.Select(c => new CertificationDto
            {
                CertificationId = c.CertificationId, Name = c.Name, IssuingOrg = c.IssuingOrg,
                IssueDate = c.IssueDate, ExpiryDate = c.ExpiryDate,
                CertificateFileUrl = c.CertificateFileUrl, CertificateFileName = c.CertificateFileName,
                CertificateContentType = c.CertificateContentType
            }).ToList(),
            Skills = p.CandidateSkills.Select(cs => new CandidateSkillDto
            {
                SkillId = cs.SkillId, SkillName = cs.Skill.SkillName, Proficiency = cs.Proficiency.ToString(),
                YearsExp = cs.YearsExp, Source = cs.Source.ToString(), IsVerified = cs.IsVerified
            }).ToList()
        };

        private static CertificationDto MapCertification(Certification c) => new()
        {
            CertificationId = c.CertificationId,
            Name = c.Name,
            IssuingOrg = c.IssuingOrg,
            IssueDate = c.IssueDate,
            ExpiryDate = c.ExpiryDate,
            CertificateFileUrl = c.CertificateFileUrl,
            CertificateFileName = c.CertificateFileName,
            CertificateContentType = c.CertificateContentType
        };
    }
}
