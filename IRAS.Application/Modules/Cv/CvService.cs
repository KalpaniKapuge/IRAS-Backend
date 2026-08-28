// IRAS.Application/Modules/Cv/CvService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Storage;
using IRAS.Application.Modules.Cv.DTOs;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Cv
{
    public class CvService : ICvService
    {
        private static readonly List<CvTemplateDto> Templates = new()
        {
            new() { Name = "Classic", Description = "Traditional single-column layout — formal, ATS-friendly." },
            new() { Name = "Modern", Description = "Two-column layout with a shaded sidebar for skills and contact info." },
            new() { Name = "Compact", Description = "Dense, space-efficient layout for candidates with a longer history." },
        };

        private static readonly string[] DefaultSectionOrder =
            { "Summary", "Skills", "Experience", "Education", "Certifications", "Languages", "Projects" };

        private const long MaxPhotoBytes = 2 * 1024 * 1024;

        private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> PhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private readonly IrasDbContext _db;
        private readonly ICvPdfRenderer _renderer;
        private readonly IFileStorage _storage;

        public CvService(IrasDbContext db, ICvPdfRenderer renderer, IFileStorage storage)
        {
            _db = db;
            _renderer = renderer;
            _storage = storage;
        }

        public List<CvTemplateDto> GetAvailableTemplates() => Templates;

        public async Task<List<CvSummaryDto>> GetMyCvsAsync(int candidateId, CancellationToken ct)
        {
            return await _db.CvDocuments
                .Where(c => c.CandidateId == candidateId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new CvSummaryDto
                {
                    CvId = c.CvId, Title = c.Title, TemplateName = c.TemplateName,
                    PhotoUrl = c.PhotoUrl, UpdatedAt = c.UpdatedAt
                })
                .ToListAsync(ct);
        }

        public async Task<CvDetailDto> GetCvDetailAsync(int candidateId, int cvId, CancellationToken ct)
        {
            var cv = await GetOwnedCvAsync(candidateId, cvId, ct);
            var profile = await GetProfileAsync(candidateId, ct);
            var explicitItems = await _db.CvSectionItems.Where(i => i.CvId == cvId).ToListAsync(ct);
            var customized = ParseCustomizedTypes(cv.CustomizedReferenceTypes);

            var resolved = ResolveContent(profile, explicitItems, customized);

            return new CvDetailDto
            {
                CvId = cv.CvId,
                Title = cv.Title,
                TemplateName = cv.TemplateName,
                Summary = cv.Summary,
                PhotoUrl = cv.PhotoUrl,
                FullName = $"{profile.FirstName} {profile.LastName}",
                Headline = profile.Headline,
                Email = profile.User.Email,
                Phone = profile.Phone,
                GithubUrl = profile.GithubUrl,
                LinkedInUrl = profile.LinkedInUrl,
                SectionOrder = ParseSectionOrder(cv.SectionOrder),
                Education = BuildItemDtos(
                    profile.Educations.Select(e => (e.EducationId, $"{e.Degree} — {e.Institution}")),
                    explicitItems, CvReferenceType.Education, customized),
                Experience = BuildItemDtos(
                    profile.WorkExperiences.Select(w => (w.ExperienceId, $"{w.JobTitle} at {w.CompanyName}")),
                    explicitItems, CvReferenceType.Experience, customized),
                Certifications = BuildItemDtos(
                    profile.Certifications.Select(c => (c.CertificationId, c.Name)),
                    explicitItems, CvReferenceType.Certification, customized),
                Skills = BuildItemDtos(
                    profile.CandidateSkills.Select(s => (s.SkillId, s.Skill.SkillName)),
                    explicitItems, CvReferenceType.Skill, customized),
                Languages = BuildItemDtos(
                    profile.Languages.Select(l => (l.LanguageId, $"{l.LanguageName} ({l.Proficiency})")),
                    explicitItems, CvReferenceType.Language, customized),
                Projects = BuildItemDtos(
                    profile.Projects.Select(p => (p.ProjectId, p.Title)),
                    explicitItems, CvReferenceType.Project, customized),
                ResolvedEducation = resolved.Education,
                ResolvedExperience = resolved.Experience,
                ResolvedCertifications = resolved.Certifications,
                ResolvedSkills = resolved.Skills,
                ResolvedLanguages = resolved.Languages,
                ResolvedProjects = resolved.Projects,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
            };
        }

        public async Task<CvDetailDto> CreateCvAsync(int candidateId, CreateCvRequest request, CancellationToken ct)
        {
            await GetProfileAsync(candidateId, ct); // 404s early if the candidate has no profile yet

            var cv = new CvDocument
            {
                CandidateId = candidateId,
                Title = request.Title,
                TemplateName = ValidateTemplate(request.TemplateName),
                SectionOrder = string.Join(",", DefaultSectionOrder)
            };
            _db.CvDocuments.Add(cv);
            await _db.SaveChangesAsync(ct);

            return await GetCvDetailAsync(candidateId, cv.CvId, ct);
        }

        public async Task UpdateCvAsync(int candidateId, int cvId, UpdateCvRequest request, CancellationToken ct)
        {
            var cv = await GetOwnedCvAsync(candidateId, cvId, ct);

            var validSections = new HashSet<string>(Enum.GetNames<CvSectionType>());
            var requested = request.SectionOrder.Distinct().ToList();
            if (requested.Any(s => !validSections.Contains(s)))
                throw new ArgumentException(
                    $"SectionOrder can only contain: {string.Join(", ", validSections)}.");

            cv.Title = request.Title;
            cv.TemplateName = ValidateTemplate(request.TemplateName);
            cv.Summary = request.Summary;
            cv.SectionOrder = string.Join(",", requested);
            cv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<CvDetailDto> UploadCvPhotoAsync(int candidateId, int cvId, IFormFile file, CancellationToken ct)
        {
            var cv = await GetOwnedCvAsync(candidateId, cvId, ct);
            ValidatePhotoUpload(file);

            var oldPhotoUrl = cv.PhotoUrl;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = $"{Guid.NewGuid():N}{extension}";

            await using (var stream = file.OpenReadStream())
            {
                cv.PhotoUrl = await _storage.SaveAsync(stream, $"cv-documents/{cvId}/photo", storedName, ct);
            }
            cv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(oldPhotoUrl))
                await _storage.DeleteAsync(oldPhotoUrl, ct);

            return await GetCvDetailAsync(candidateId, cvId, ct);
        }

        private static void ValidatePhotoUpload(IFormFile file)
        {
            if (file.Length == 0)
                throw new ArgumentException("CV photo is empty.");

            if (file.Length > MaxPhotoBytes)
                throw new ArgumentException($"CV photo exceeds the {MaxPhotoBytes / 1024 / 1024} MB limit.");

            var extension = Path.GetExtension(file.FileName);
            if (!PhotoExtensions.Contains(extension))
                throw new ArgumentException("CV photo has an unsupported file extension.");

            if (!PhotoContentTypes.Contains(file.ContentType))
                throw new ArgumentException("CV photo has an unsupported content type.");
        }

        public async Task UpdateSectionItemsAsync(int candidateId, int cvId, UpdateCvSectionItemsRequest request, CancellationToken ct)
        {
            var cv = await GetOwnedCvAsync(candidateId, cvId, ct);
            var refType = ParseEnum<CvReferenceType>(request.ReferenceType, nameof(request.ReferenceType));

            // Validate every referenced id actually belongs to this candidate before saving —
            // otherwise a crafted request could make one candidate's CV point at another
            // candidate's education/experience/certification/skill rows.
            var profile = await GetProfileAsync(candidateId, ct);
            var validIds = refType switch
            {
                CvReferenceType.Education => profile.Educations.Select(e => e.EducationId).ToHashSet(),
                CvReferenceType.Experience => profile.WorkExperiences.Select(w => w.ExperienceId).ToHashSet(),
                CvReferenceType.Certification => profile.Certifications.Select(c => c.CertificationId).ToHashSet(),
                CvReferenceType.Skill => profile.CandidateSkills.Select(s => s.SkillId).ToHashSet(),
                CvReferenceType.Language => profile.Languages.Select(l => l.LanguageId).ToHashSet(),
                CvReferenceType.Project => profile.Projects.Select(p => p.ProjectId).ToHashSet(),
                _ => new HashSet<int>()
            };
            if (request.ReferenceIds.Any(id => !validIds.Contains(id)))
                throw new ArgumentException("One or more referenced items do not belong to this candidate's profile.");

            var existing = await _db.CvSectionItems
                .Where(i => i.CvId == cvId && i.ReferenceType == refType)
                .ToListAsync(ct);
            _db.CvSectionItems.RemoveRange(existing);

            for (var i = 0; i < request.ReferenceIds.Count; i++)
            {
                _db.CvSectionItems.Add(new CvSectionItem
                {
                    CvId = cvId, ReferenceType = refType, ReferenceId = request.ReferenceIds[i], OrderIndex = i
                });
            }

            var customized = ParseCustomizedTypes(cv.CustomizedReferenceTypes);
            customized.Add(refType.ToString());
            cv.CustomizedReferenceTypes = string.Join(",", customized);

            cv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteCvAsync(int candidateId, int cvId, CancellationToken ct)
        {
            var cv = await GetOwnedCvAsync(candidateId, cvId, ct);
            _db.CvDocuments.Remove(cv); // cascades CvSectionItem rows
            await _db.SaveChangesAsync(ct);
        }

        public async Task<byte[]> RenderPdfAsync(int candidateId, int cvId, CancellationToken ct)
        {
            var cv = await GetOwnedCvAsync(candidateId, cvId, ct);
            var profile = await GetProfileAsync(candidateId, ct);
            var explicitItems = await _db.CvSectionItems.Where(i => i.CvId == cvId).ToListAsync(ct);
            var customized = ParseCustomizedTypes(cv.CustomizedReferenceTypes);

            var resolved = ResolveContent(profile, explicitItems, customized);

            byte[]? photoBytes = null;
            if (!string.IsNullOrWhiteSpace(cv.PhotoUrl))
            {
                try
                {
                    await using var photoStream = await _storage.OpenReadAsync(cv.PhotoUrl, ct);
                    using var buffer = new MemoryStream();
                    await photoStream.CopyToAsync(buffer, ct);
                    photoBytes = buffer.ToArray();
                }
                catch
                {
                    // A missing/unreachable photo should never block downloading the rest of the CV.
                }
            }

            var data = new RenderedCvData
            {
                FullName = $"{profile.FirstName} {profile.LastName}",
                Headline = profile.Headline,
                Email = profile.User.Email,
                Phone = profile.Phone,
                GithubUrl = profile.GithubUrl,
                LinkedInUrl = profile.LinkedInUrl,
                Summary = cv.Summary,
                PhotoBytes = photoBytes,
                SectionOrder = ParseSectionOrder(cv.SectionOrder),
                Education = resolved.Education,
                Experience = resolved.Experience,
                Certifications = resolved.Certifications,
                Skills = resolved.Skills,
                Languages = resolved.Languages,
                Projects = resolved.Projects
            };

            return _renderer.Render(cv.TemplateName, data);
        }

        // ---- helpers ----

        private async Task<CvDocument> GetOwnedCvAsync(int candidateId, int cvId, CancellationToken ct)
        {
            var cv = await _db.CvDocuments.FirstOrDefaultAsync(c => c.CvId == cvId, ct)
                ?? throw new KeyNotFoundException("CV not found.");
            if (cv.CandidateId != candidateId)
                throw new KeyNotFoundException("CV not found.");
            return cv;
        }

        private async Task<CandidateProfile> GetProfileAsync(int candidateId, CancellationToken ct)
        {
            return await _db.CandidateProfiles
                .Include(c => c.User)
                .Include(c => c.Educations)
                .Include(c => c.WorkExperiences)
                .Include(c => c.Certifications)
                .Include(c => c.Languages)
                .Include(c => c.Projects)
                .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Candidate profile not found.");
        }

        private static List<CvItemDto> BuildItemDtos(
            IEnumerable<(int Id, string Label)> available,
            List<CvSectionItem> explicitItems,
            CvReferenceType type,
            HashSet<string> customizedTypes)
        {
            var chosen = explicitItems.Where(i => i.ReferenceType == type)
                .OrderBy(i => i.OrderIndex).ToList();
            var availableList = available.ToList();
            var isCustomized = customizedTypes.Contains(type.ToString());

            if (!isCustomized)
            {
                // Never explicitly configured — everything is included, in profile order.
                return availableList.Select((a, idx) => new CvItemDto
                {
                    ReferenceId = a.Id, Label = a.Label, Included = true, OrderIndex = idx
                }).ToList();
            }

            var labelById = availableList.ToDictionary(a => a.Id, a => a.Label);
            var result = chosen.Where(c => labelById.ContainsKey(c.ReferenceId)).Select(c => new CvItemDto
            {
                ReferenceId = c.ReferenceId, Label = labelById[c.ReferenceId], Included = true, OrderIndex = c.OrderIndex
            }).ToList();

            // Items that exist on the profile but weren't chosen still show up, unchecked,
            // so the editor can offer them without the candidate hunting through the profile.
            // This includes the case where the type was customized to an explicitly empty
            // selection — every item then shows up unchecked, none marked Included.
            var chosenIds = chosen.Select(c => c.ReferenceId).ToHashSet();
            var nextOrder = result.Count;
            foreach (var a in availableList.Where(a => !chosenIds.Contains(a.Id)))
                result.Add(new CvItemDto { ReferenceId = a.Id, Label = a.Label, Included = false, OrderIndex = nextOrder++ });

            return result;
        }

        private static List<int> ResolveOrder(
            List<CvSectionItem> explicitItems, CvReferenceType type, IEnumerable<int> defaultOrder, HashSet<string> customizedTypes)
        {
            if (!customizedTypes.Contains(type.ToString()))
                return defaultOrder.ToList();

            return explicitItems.Where(i => i.ReferenceType == type)
                .OrderBy(i => i.OrderIndex).Select(i => i.ReferenceId).ToList();
        }

        // Resolves the final, ordered, fully-detailed content for whichever items are
        // currently included — shared by GetCvDetailAsync (web preview) and RenderPdfAsync
        // (PDF), so the two never drift out of sync with each other.
        private static (
            List<CvResolvedEducationDto> Education,
            List<CvResolvedExperienceDto> Experience,
            List<CvResolvedCertificationDto> Certifications,
            List<string> Skills,
            List<CvResolvedLanguageDto> Languages,
            List<CvResolvedProjectDto> Projects) ResolveContent(
                CandidateProfile profile, List<CvSectionItem> explicitItems, HashSet<string> customized)
        {
            var educationIds = ResolveOrder(explicitItems, CvReferenceType.Education, profile.Educations.Select(e => e.EducationId), customized);
            var experienceIds = ResolveOrder(explicitItems, CvReferenceType.Experience, profile.WorkExperiences.Select(w => w.ExperienceId), customized);
            var certificationIds = ResolveOrder(explicitItems, CvReferenceType.Certification, profile.Certifications.Select(c => c.CertificationId), customized);
            var skillIds = ResolveOrder(explicitItems, CvReferenceType.Skill, profile.CandidateSkills.Select(s => s.SkillId), customized);
            var languageIds = ResolveOrder(explicitItems, CvReferenceType.Language, profile.Languages.Select(l => l.LanguageId), customized);
            var projectIds = ResolveOrder(explicitItems, CvReferenceType.Project, profile.Projects.Select(p => p.ProjectId), customized);

            var educationById = profile.Educations.ToDictionary(e => e.EducationId);
            var experienceById = profile.WorkExperiences.ToDictionary(w => w.ExperienceId);
            var certById = profile.Certifications.ToDictionary(c => c.CertificationId);
            var skillById = profile.CandidateSkills.ToDictionary(s => s.SkillId);
            var languageById = profile.Languages.ToDictionary(l => l.LanguageId);
            var projectById = profile.Projects.ToDictionary(p => p.ProjectId);

            var education = educationIds.Where(educationById.ContainsKey).Select(id =>
            {
                var e = educationById[id];
                return new CvResolvedEducationDto
                {
                    Degree = e.Degree, Institution = e.Institution, FieldOfStudy = e.FieldOfStudy,
                    StartYear = e.StartYear, EndYear = e.EndYear, Grade = e.Grade
                };
            }).ToList();

            var experience = experienceIds.Where(experienceById.ContainsKey).Select(id =>
            {
                var w = experienceById[id];
                return new CvResolvedExperienceDto
                {
                    JobTitle = w.JobTitle, CompanyName = w.CompanyName, StartDate = w.StartDate,
                    EndDate = w.EndDate, IsCurrent = w.IsCurrent, Description = w.Description
                };
            }).ToList();

            var certifications = certificationIds.Where(certById.ContainsKey).Select(id =>
            {
                var c = certById[id];
                return new CvResolvedCertificationDto { Name = c.Name, IssuingOrg = c.IssuingOrg, IssueDate = c.IssueDate };
            }).ToList();

            var skills = skillIds.Where(skillById.ContainsKey).Select(id => skillById[id].Skill.SkillName).ToList();

            var languages = languageIds.Where(languageById.ContainsKey).Select(id =>
            {
                var l = languageById[id];
                return new CvResolvedLanguageDto { LanguageName = l.LanguageName, Proficiency = l.Proficiency };
            }).ToList();

            var projects = projectIds.Where(projectById.ContainsKey).Select(id =>
            {
                var p = projectById[id];
                return new CvResolvedProjectDto
                {
                    Title = p.Title, Description = p.Description, ProjectUrl = p.ProjectUrl,
                    StartDate = p.StartDate, EndDate = p.EndDate
                };
            }).ToList();

            return (education, experience, certifications, skills, languages, projects);
        }

        private static HashSet<string> ParseCustomizedTypes(string raw) =>
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        private static List<string> ParseSectionOrder(string raw) =>
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private static string ValidateTemplate(string templateName)
        {
            if (!Templates.Any(t => string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"'{templateName}' is not a valid template. Valid values: {string.Join(", ", Templates.Select(t => t.Name))}.");
            return templateName;
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
                throw new ArgumentException(
                    $"'{value}' is not a valid {fieldName}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
            return result;
        }
    }
}
