// IRAS.Application/Modules/Assessments/AssessmentService.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Modules.Assessments.DTOs;
using IRAS.Domain.Entities.Assessments;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Modules.Assessments
{
    public class AssessmentService : IAssessmentService
    {
        private const int QuestionCount = 10;

        private readonly IrasDbContext _db;
        private readonly IAssessmentQuestionGenerator _generator;

        public AssessmentService(IrasDbContext db, IAssessmentQuestionGenerator generator)
        {
            _db = db;
            _generator = generator;
        }

        public async Task<AssessmentStatusDto> GetStatusAsync(int candidateId, int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");

            if (!job.RequireAssessment)
                return new AssessmentStatusDto { RequireAssessment = false };

            var attempt = await _db.CandidateAssessmentAttempts
                .FirstOrDefaultAsync(a => a.CandidateId == candidateId && a.JobId == jobId, ct);

            return new AssessmentStatusDto
            {
                RequireAssessment = true,
                HasAttempted = attempt is not null,
                IsCompleted = attempt?.Status == AssessmentAttemptStatus.Completed,
                Score = attempt?.Status == AssessmentAttemptStatus.Completed ? attempt.Score : null,
            };
        }

        public async Task<StartAssessmentResponse> StartAsync(int candidateId, int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs
                .Include(j => j.RequiredSkills).ThenInclude(rs => rs.Skill)
                .FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");

            if (!job.RequireAssessment)
                throw new InvalidOperationException("This job does not require a skill assessment.");

            var existing = await _db.CandidateAssessmentAttempts
                .Include(a => a.JobAssessment).ThenInclude(ja => ja.Questions)
                .FirstOrDefaultAsync(a => a.CandidateId == candidateId && a.JobId == jobId, ct);

            if (existing is { Status: AssessmentAttemptStatus.Completed })
                throw new InvalidOperationException("You have already completed this assessment.");

            if (existing is not null)
            {
                return new StartAssessmentResponse
                {
                    AttemptId = existing.AttemptId,
                    Questions = ToCandidateDtos(existing.JobAssessment.Questions),
                };
            }

            var assessment = await GetOrCreateAssessmentAsync(job, ct);

            var attempt = new CandidateAssessmentAttempt
            {
                CandidateId = candidateId,
                JobId = jobId,
                JobAssessmentId = assessment.JobAssessmentId,
            };
            _db.CandidateAssessmentAttempts.Add(attempt);
            await _db.SaveChangesAsync(ct);

            return new StartAssessmentResponse
            {
                AttemptId = attempt.AttemptId,
                Questions = ToCandidateDtos(assessment.Questions),
            };
        }

        public async Task<AssessmentResultDto> SubmitAsync(int candidateId, int jobId, SubmitAssessmentRequest request, CancellationToken ct)
        {
            var attempt = await _db.CandidateAssessmentAttempts
                .Include(a => a.JobAssessment).ThenInclude(ja => ja.Questions)
                .FirstOrDefaultAsync(a => a.CandidateId == candidateId && a.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("No assessment attempt found for this job.");

            if (attempt.Status != AssessmentAttemptStatus.InProgress)
                throw new InvalidOperationException("This assessment has already been submitted.");

            var questions = attempt.JobAssessment.Questions.ToDictionary(q => q.AssessmentQuestionId);
            var answeredIds = request.Answers.Select(a => a.QuestionId).ToHashSet();
            if (!questions.Keys.All(answeredIds.Contains))
                throw new ArgumentException("Answer all questions before submitting.");

            var correctCount = 0;
            foreach (var answer in request.Answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                    throw new ArgumentException("One or more answers reference a question that isn't part of this assessment.");

                var isCorrect = answer.SelectedOptionIndex == question.CorrectOptionIndex;
                if (isCorrect) correctCount++;

                _db.CandidateAssessmentAnswers.Add(new CandidateAssessmentAnswer
                {
                    AttemptId = attempt.AttemptId,
                    AssessmentQuestionId = question.AssessmentQuestionId,
                    SelectedOptionIndex = answer.SelectedOptionIndex,
                    IsCorrect = isCorrect,
                });
            }

            var total = questions.Count;
            var score = total == 0 ? 0m : Math.Round((decimal)correctCount / total, 4);

            attempt.Status = AssessmentAttemptStatus.Completed;
            attempt.Score = score;
            attempt.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new AssessmentResultDto { Score = score, CorrectCount = correctCount, TotalQuestions = total };
        }

        public async Task<bool> HasPassedGateAsync(int candidateId, int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");

            if (!job.RequireAssessment)
                return true;

            return await _db.CandidateAssessmentAttempts.AnyAsync(
                a => a.CandidateId == candidateId && a.JobId == jobId && a.Status == AssessmentAttemptStatus.Completed, ct);
        }

        public async Task<decimal?> GetScoreAsync(int candidateId, int jobId, CancellationToken ct)
        {
            var attempt = await _db.CandidateAssessmentAttempts
                .FirstOrDefaultAsync(a => a.CandidateId == candidateId && a.JobId == jobId && a.Status == AssessmentAttemptStatus.Completed, ct);
            return attempt?.Score;
        }

        private async Task<JobAssessment> GetOrCreateAssessmentAsync(Job job, CancellationToken ct)
        {
            var existing = await _db.JobAssessments
                .Include(a => a.Questions)
                .FirstOrDefaultAsync(a => a.JobId == job.JobId, ct);
            if (existing is not null)
                return existing;

            var skills = job.RequiredSkills
                .Select(rs => (rs.Skill.SkillName, Importance: rs.Importance.ToString(), rs.Skill.Category))
                .ToList();

            var generated = await _generator.GenerateAsync(job, skills, QuestionCount, ct);
            if (generated.Count == 0)
                throw new InvalidOperationException("Unable to generate a skill assessment for this job. Please try again shortly.");

            var skillIdByName = job.RequiredSkills.ToDictionary(rs => rs.Skill.SkillName, rs => rs.SkillId, StringComparer.OrdinalIgnoreCase);

            var assessment = new JobAssessment
            {
                JobId = job.JobId,
                GeneratedBy = _generator.Name,
                Questions = generated.Select((q, i) => new AssessmentQuestion
                {
                    QuestionText = q.QuestionText,
                    Options = q.Options,
                    CorrectOptionIndex = q.CorrectOptionIndex,
                    QuestionOrder = i,
                    SkillId = q.SkillName is not null && skillIdByName.TryGetValue(q.SkillName, out var skillId) ? skillId : null,
                }).ToList(),
            };

            _db.JobAssessments.Add(assessment);
            await _db.SaveChangesAsync(ct);
            return assessment;
        }

        private static List<AssessmentQuestionForCandidateDto> ToCandidateDtos(IEnumerable<AssessmentQuestion> questions) =>
            questions.OrderBy(q => q.QuestionOrder)
                .Select(q => new AssessmentQuestionForCandidateDto
                {
                    QuestionId = q.AssessmentQuestionId,
                    QuestionText = q.QuestionText,
                    Options = q.Options,
                })
                .ToList();
    }
}
