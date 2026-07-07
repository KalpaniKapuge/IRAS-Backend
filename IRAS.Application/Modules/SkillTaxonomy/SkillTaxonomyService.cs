// IRAS.Application/Modules/SkillTaxonomy/SkillTaxonomyService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.SkillTaxonomy.DTOs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.SkillTaxonomy
{
    public class SkillTaxonomyService : ISkillTaxonomyService
    {
        private readonly IrasDbContext _db;
        public SkillTaxonomyService(IrasDbContext db) => _db = db;

        public async Task<PagedResult<SkillDto>> SearchAsync(string? query, string? category, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.Skills.Include(s => s.Aliases).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                q = q.Where(s => s.SkillName.Contains(term)
                              || s.Aliases.Any(a => a.AliasText.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = ParseEnum<SkillCategory>(category, nameof(category));
                q = q.Where(s => s.Category == cat);
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(s => s.SkillName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new PagedResult<SkillDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total, Page = page, PageSize = pageSize
            };
        }

        public async Task<SkillDto> GetByIdAsync(int skillId)
        {
            var skill = await _db.Skills.Include(s => s.Aliases)
                .FirstOrDefaultAsync(s => s.SkillId == skillId)
                ?? throw new KeyNotFoundException("Skill not found.");
            return MapToDto(skill);
        }

        // The normalization function: exact name match first, then alias match.
        // "JS" -> JavaScript, "ReactJS" -> React, unknown text -> Found = false.
        public async Task<SkillResolveResult> ResolveAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new SkillResolveResult { Found = false };

            var term = text.Trim();

            var byName = await _db.Skills
                .FirstOrDefaultAsync(s => s.SkillName == term);
            if (byName != null)
                return new SkillResolveResult
                {
                    Found = true, SkillId = byName.SkillId,
                    SkillName = byName.SkillName, MatchedBy = "name"
                };

            var byAlias = await _db.SkillAliases.Include(a => a.Skill)
                .FirstOrDefaultAsync(a => a.AliasText == term);
            if (byAlias != null)
                return new SkillResolveResult
                {
                    Found = true, SkillId = byAlias.SkillId,
                    SkillName = byAlias.Skill.SkillName, MatchedBy = "alias"
                };

            return new SkillResolveResult { Found = false };
        }

        public async Task<List<SkillDto>> ExportAllAsync()
        {
            var skills = await _db.Skills.Include(s => s.Aliases)
                .OrderBy(s => s.SkillId)
                .ToListAsync();
            return skills.Select(MapToDto).ToList();
        }

        public async Task<SkillDto> CreateAsync(CreateSkillRequest request)
        {
            var category = ParseEnum<SkillCategory>(request.Category, nameof(request.Category));
            var name = request.SkillName.Trim();

            var nameTaken = await _db.Skills.AnyAsync(s => s.SkillName == name);
            if (nameTaken)
                throw new InvalidOperationException("A skill with this name already exists.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var skill = new Skill
                {
                    SkillName = name,
                    Category = category,
                    Description = request.Description
                };
                _db.Skills.Add(skill);
                await _db.SaveChangesAsync();

                foreach (var aliasText in request.Aliases
                             .Select(a => a.Trim())
                             .Where(a => a.Length > 0)
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var aliasTaken = await _db.SkillAliases.AnyAsync(a => a.AliasText == aliasText);
                    if (aliasTaken)
                        throw new InvalidOperationException($"Alias '{aliasText}' is already mapped to another skill.");

                    _db.SkillAliases.Add(new SkillAlias
                    {
                        SkillId = skill.SkillId,
                        AliasText = aliasText,
                        Source = AliasSource.AdminAdded
                    });
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetByIdAsync(skill.SkillId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(int skillId, UpdateSkillRequest request)
        {
            var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillId == skillId)
                ?? throw new KeyNotFoundException("Skill not found.");

            var category = ParseEnum<SkillCategory>(request.Category, nameof(request.Category));
            var name = request.SkillName.Trim();

            var nameTaken = await _db.Skills.AnyAsync(s => s.SkillName == name && s.SkillId != skillId);
            if (nameTaken)
                throw new InvalidOperationException("Another skill already uses this name.");

            skill.SkillName = name;
            skill.Category = category;
            skill.Description = request.Description;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int skillId)
        {
            var skill = await _db.Skills.Include(s => s.Aliases)
                .FirstOrDefaultAsync(s => s.SkillId == skillId)
                ?? throw new KeyNotFoundException("Skill not found.");

            // Friendly guard before the DB Restrict constraint fires:
            var usedByCandidates = await _db.CandidateSkills.CountAsync(cs => cs.SkillId == skillId);
            var usedByJobs = await _db.JobRequiredSkills.CountAsync(j => j.SkillId == skillId);
            if (usedByCandidates > 0 || usedByJobs > 0)
                throw new InvalidOperationException(
                    $"Cannot delete: skill is used by {usedByCandidates} candidate(s) and {usedByJobs} job(s). " +
                    "Remove those references first, or keep the skill.");

            _db.SkillAliases.RemoveRange(skill.Aliases);
            _db.Skills.Remove(skill);
            await _db.SaveChangesAsync();
        }

        public async Task<SkillAliasDto> AddAliasAsync(int skillId, AddAliasRequest request)
        {
            var skillExists = await _db.Skills.AnyAsync(s => s.SkillId == skillId);
            if (!skillExists) throw new KeyNotFoundException("Skill not found.");

            var text = request.AliasText.Trim();

            var aliasTaken = await _db.SkillAliases.AnyAsync(a => a.AliasText == text);
            if (aliasTaken)
                throw new InvalidOperationException("This alias is already mapped to a skill.");

            // Without this guard, ResolveAsync could match the same text two different
            // ways (as a skill name and as an alias) depending on lookup order.
            var clashesWithName = await _db.Skills.AnyAsync(s => s.SkillName == text);
            if (clashesWithName)
                throw new InvalidOperationException("This text is already a skill name; an alias would be ambiguous.");

            var alias = new SkillAlias
            {
                SkillId = skillId,
                AliasText = text,
                Source = AliasSource.AdminAdded
            };
            _db.SkillAliases.Add(alias);
            await _db.SaveChangesAsync();

            return new SkillAliasDto
            {
                AliasId = alias.AliasId, AliasText = alias.AliasText, Source = alias.Source.ToString()
            };
        }

        public async Task DeleteAliasAsync(int skillId, int aliasId)
        {
            var alias = await _db.SkillAliases
                .FirstOrDefaultAsync(a => a.AliasId == aliasId && a.SkillId == skillId)
                ?? throw new KeyNotFoundException("Alias not found for this skill.");
            _db.SkillAliases.Remove(alias);
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

        private static SkillDto MapToDto(Skill s) => new()
        {
            SkillId = s.SkillId,
            SkillName = s.SkillName,
            Category = s.Category.ToString(),
            Description = s.Description,
            Aliases = s.Aliases.Select(a => new SkillAliasDto
            {
                AliasId = a.AliasId, AliasText = a.AliasText, Source = a.Source.ToString()
            }).ToList()
        };
    }
}