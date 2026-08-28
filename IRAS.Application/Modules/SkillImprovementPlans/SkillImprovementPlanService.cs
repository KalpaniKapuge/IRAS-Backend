// IRAS.Application/Modules/SkillImprovementPlans/SkillImprovementPlanService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.SkillImprovementPlans.DTOs;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.SkillImprovementPlans
{
    public class SkillImprovementPlanService : ISkillImprovementPlanService
    {
        private readonly IrasDbContext _db;
        private readonly ISkillPlanGenerator _generator;

        public SkillImprovementPlanService(IrasDbContext db, ISkillPlanGenerator generator)
        {
            _db = db;
            _generator = generator;
        }

        public async Task<List<SkillImprovementPlanDto>> GetMyPlansAsync(int candidateId, CancellationToken ct)
        {
            var plans = await _db.SkillImprovementPlans
                .Include(p => p.Skill)
                .Include(p => p.Job)
                .Include(p => p.Steps)
                .Where(p => p.CandidateId == candidateId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return plans.Select(MapToDto).ToList();
        }

        public async Task<SkillImprovementPlanDto> GetPlanAsync(int candidateId, int planId, CancellationToken ct)
        {
            var plan = await LoadOwnedPlanAsync(candidateId, planId, ct);
            return MapToDto(plan);
        }

        public async Task<SkillImprovementPlanDto> GeneratePlanAsync(
            int candidateId, int skillId, GeneratePlanRequest request, CancellationToken ct)
        {
            // Idempotent: regenerating loses no candidate progress and costs nothing extra.
            var existing = await _db.SkillImprovementPlans
                .Include(p => p.Skill).Include(p => p.Job).Include(p => p.Steps)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId && p.SkillId == skillId, ct);
            if (existing != null)
                return MapToDto(existing);

            var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillId == skillId, ct)
                ?? throw new KeyNotFoundException("Skill not found.");

            // Source generation context (job title + importance) from an actual detected
            // gap — a plan is always grounded in a real skill gap, never generated blind.
            var gapsQuery = _db.SkillGaps
                .Include(g => g.Application).ThenInclude(a => a.Job)
                .Where(g => g.SkillId == skillId && g.Application.CandidateId == candidateId);

            var gap = request.JobId.HasValue
                ? await gapsQuery.FirstOrDefaultAsync(g => g.Application.JobId == request.JobId.Value, ct)
                : await gapsQuery.OrderByDescending(g => g.DetectedAt).FirstOrDefaultAsync(ct);

            if (gap is null)
                throw new ArgumentException("No detected skill gap found for this candidate and skill.");

            var generated = await _generator.GenerateAsync(
                skill.SkillName, gap.Application.Job.Title, gap.Importance.ToString(), ct);

            // A plan is how a candidate starts actively tracking a skill — ensure the
            // underlying target-skill bookkeeping exists rather than duplicating it.
            var alreadyTracked = await _db.CandidateTargetSkills
                .AnyAsync(t => t.CandidateId == candidateId && t.SkillId == skillId, ct);
            if (!alreadyTracked)
                _db.CandidateTargetSkills.Add(new CandidateTargetSkill { CandidateId = candidateId, SkillId = skillId });

            var plan = new SkillImprovementPlan
            {
                CandidateId = candidateId,
                SkillId = skillId,
                JobId = gap.Application.JobId,
                Priority = generated.Priority,
                TargetLevel = generated.TargetLevel,
                EstimatedDays = generated.EstimatedDays,
                Overview = generated.Overview,
                GapReason = generated.GapReason,
                ProjectTitle = generated.ProjectTitle,
                ProjectTask = generated.ProjectTask,
                ProjectExpectedOutput = generated.ProjectExpectedOutput,
                Status = SkillPlanStatus.NotStarted,
                GeneratedBy = _generator.Name,
                Steps = generated.Steps.Select((s, i) => new SkillPlanStep
                {
                    StepOrder = i + 1,
                    Title = s.Title,
                    Description = s.Description,
                    Activity = s.Activity,
                    Output = s.Output
                }).ToList()
            };

            _db.SkillImprovementPlans.Add(plan);
            await _db.SaveChangesAsync(ct);

            // Reload with the Skill/Job navigations populated for mapping (Add() alone
            // doesn't populate references to already-existing rows unless they were tracked).
            plan.Skill = skill;
            plan.Job = gap.Application.Job;
            return MapToDto(plan);
        }

        public async Task<SkillImprovementPlanDto> SetStepCompletionAsync(
            int candidateId, int planId, int stepId, bool isCompleted, CancellationToken ct)
        {
            var plan = await LoadOwnedPlanAsync(candidateId, planId, ct);

            var step = plan.Steps.FirstOrDefault(s => s.StepId == stepId)
                ?? throw new KeyNotFoundException("Skill plan step not found.");

            step.IsCompleted = isCompleted;
            step.CompletedAt = isCompleted ? DateTime.UtcNow : null;

            plan.Status = ComputeStatus(plan.Steps);
            await _db.SaveChangesAsync(ct);

            return MapToDto(plan);
        }

        private async Task<SkillImprovementPlan> LoadOwnedPlanAsync(int candidateId, int planId, CancellationToken ct)
        {
            return await _db.SkillImprovementPlans
                .Include(p => p.Skill).Include(p => p.Job).Include(p => p.Steps)
                .FirstOrDefaultAsync(p => p.PlanId == planId && p.CandidateId == candidateId, ct)
                ?? throw new KeyNotFoundException("Skill improvement plan not found.");
        }

        // NotStarted -> Learning -> Practicing -> Completed, driven purely by step-completion
        // ratio. Verified is deliberately never set here — that's an admin action (Phase 2).
        private static SkillPlanStatus ComputeStatus(ICollection<SkillPlanStep> steps)
        {
            if (steps.Count == 0) return SkillPlanStatus.NotStarted;

            var completedRatio = steps.Count(s => s.IsCompleted) / (double)steps.Count;
            return completedRatio switch
            {
                <= 0 => SkillPlanStatus.NotStarted,
                >= 1 => SkillPlanStatus.Completed,
                < 0.5 => SkillPlanStatus.Learning,
                _ => SkillPlanStatus.Practicing
            };
        }

        private static SkillImprovementPlanDto MapToDto(SkillImprovementPlan p)
        {
            var steps = p.Steps.OrderBy(s => s.StepOrder).ToList();
            var progress = steps.Count == 0 ? 0 : (int)Math.Round(100.0 * steps.Count(s => s.IsCompleted) / steps.Count);

            return new SkillImprovementPlanDto
            {
                PlanId = p.PlanId,
                SkillId = p.SkillId,
                SkillName = p.Skill.SkillName,
                JobId = p.JobId,
                JobTitle = p.Job?.Title,
                Priority = p.Priority.ToString(),
                TargetLevel = p.TargetLevel.ToString(),
                EstimatedDays = p.EstimatedDays,
                Overview = p.Overview,
                GapReason = p.GapReason,
                ProjectTitle = p.ProjectTitle,
                ProjectTask = p.ProjectTask,
                ProjectExpectedOutput = p.ProjectExpectedOutput,
                Status = p.Status.ToString(),
                GeneratedBy = p.GeneratedBy,
                CreatedAt = p.CreatedAt,
                ProgressPercent = progress,
                Steps = steps.Select(s => new SkillPlanStepDto
                {
                    StepId = s.StepId,
                    StepOrder = s.StepOrder,
                    Title = s.Title,
                    Description = s.Description,
                    Activity = s.Activity,
                    Output = s.Output,
                    IsCompleted = s.IsCompleted,
                    CompletedAt = s.CompletedAt
                }).ToList()
            };
        }
    }
}
