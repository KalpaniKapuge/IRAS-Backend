// IRAS.Application/Modules/Assessments/AssessmentService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private const int SecondsPerQuestion = 60;

        private readonly IrasDbContext _db;
        private readonly IAssessmentQuestionGenerator _generator;
        private readonly TemplateAssessmentQuestionGenerator _fallbackGenerator;
        private readonly IAssessmentAnswerGrader _answerGrader;
        private readonly ILogger<AssessmentService> _logger;

        public AssessmentService(
            IrasDbContext db, IAssessmentQuestionGenerator generator, TemplateAssessmentQuestionGenerator fallbackGenerator,
            IAssessmentAnswerGrader answerGrader, ILogger<AssessmentService> logger)
        {
            _db = db;
            _generator = generator;
            _fallbackGenerator = fallbackGenerator;
            _answerGrader = answerGrader;
            _logger = logger;
        }

        public async Task<AssessmentStatusDto> GetStatusAsync(int candidateId, int jobId, CancellationToken ct)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                ?? throw new KeyNotFoundException("Job not found.");

            if (!job.RequireAssessment)
                return new AssessmentStatusDto { RequireAssessment = false };

            var attempt = await _db.CandidateAssessmentAttempts
                .Include(a => a.JobAssessment).ThenInclude(ja => ja.Questions)
                .FirstOrDefaultAsync(a => a.CandidateId == candidateId && a.JobId == jobId, ct);

            var isCompleted = attempt?.Status == AssessmentAttemptStatus.Completed;

            return new AssessmentStatusDto
            {
                RequireAssessment = true,
                HasAttempted = attempt is not null,
                IsCompleted = isCompleted,
                Score = isCompleted ? attempt!.Score : null,
                DeadlineAt = attempt is { Status: AssessmentAttemptStatus.InProgress } ? ComputeDeadline(attempt) : null,
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
                    StartedAt = existing.StartedAt,
                    DeadlineAt = ComputeDeadline(existing),
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
                StartedAt = attempt.StartedAt,
                DeadlineAt = attempt.StartedAt.AddSeconds(assessment.Questions.Count * SecondsPerQuestion),
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
            var answersByQuestionId = request.Answers
                .Where(a => questions.ContainsKey(a.QuestionId))
                .ToDictionary(a => a.QuestionId);

            var correctCount = 0;
            var answeredCount = 0;
            var totalScoreFraction = 0m;

            // Grade every question the assessment has, not just the ones answered — a
            // partial/empty submission (the timer ran out) is valid, and unanswered
            // questions simply score 0. This is the "close the quiz and show marks for what
            // was done" behavior.
            foreach (var question in questions.Values.OrderBy(q => q.QuestionOrder))
            {
                answersByQuestionId.TryGetValue(question.AssessmentQuestionId, out var answer);

                decimal scoreFraction;
                int? selectedOptionIndex = null;
                string? freeTextAnswer = null;

                if (question.QuestionType == AssessmentQuestionType.MultipleChoice)
                {
                    selectedOptionIndex = answer?.SelectedOptionIndex;
                    scoreFraction = selectedOptionIndex.HasValue && selectedOptionIndex == question.CorrectOptionIndex ? 1m : 0m;
                    if (selectedOptionIndex.HasValue) answeredCount++;
                }
                else
                {
                    freeTextAnswer = answer?.FreeTextAnswer;
                    if (!string.IsNullOrWhiteSpace(freeTextAnswer))
                    {
                        answeredCount++;
                        scoreFraction = await _answerGrader.GradeAsync(question.QuestionText, question.ModelAnswer ?? "", freeTextAnswer, ct);
                    }
                    else
                    {
                        scoreFraction = 0m;
                    }
                }

                if (scoreFraction >= 0.6m) correctCount++;
                totalScoreFraction += scoreFraction;

                _db.CandidateAssessmentAnswers.Add(new CandidateAssessmentAnswer
                {
                    AttemptId = attempt.AttemptId,
                    AssessmentQuestionId = question.AssessmentQuestionId,
                    SelectedOptionIndex = selectedOptionIndex,
                    FreeTextAnswer = freeTextAnswer,
                    ScoreFraction = scoreFraction,
                });
            }

            var total = questions.Count;
            var score = total == 0 ? 0m : Math.Round(totalScoreFraction / total, 4);

            attempt.Status = AssessmentAttemptStatus.Completed;
            attempt.Score = score;
            attempt.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new AssessmentResultDto { Score = score, CorrectCount = correctCount, AnsweredCount = answeredCount, TotalQuestions = total };
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

        public async Task<EmployerAssessmentReviewDto?> GetReviewForEmployerAsync(int employerId, int applicationId, CancellationToken ct)
        {
            var application = await _db.Applications
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.Job.EmployerId == employerId, ct)
                ?? throw new KeyNotFoundException("Application not found.");

            var attempt = await _db.CandidateAssessmentAttempts
                .Include(a => a.JobAssessment).ThenInclude(ja => ja.Questions)
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.CandidateId == application.CandidateId && a.JobId == application.JobId
                    && a.Status == AssessmentAttemptStatus.Completed, ct);

            if (attempt is null) return null;

            var answersByQuestionId = attempt.Answers.ToDictionary(a => a.AssessmentQuestionId);

            return new EmployerAssessmentReviewDto
            {
                Score = attempt.Score ?? 0,
                CompletedAt = attempt.CompletedAt,
                Questions = attempt.JobAssessment.Questions
                    .OrderBy(q => q.QuestionOrder)
                    .Select(q =>
                    {
                        answersByQuestionId.TryGetValue(q.AssessmentQuestionId, out var answer);
                        return new AssessmentQuestionReviewDto
                        {
                            QuestionType = q.QuestionType.ToString(),
                            QuestionText = q.QuestionText,
                            Options = q.Options,
                            CorrectOptionIndex = q.QuestionType == AssessmentQuestionType.MultipleChoice ? q.CorrectOptionIndex : null,
                            ModelAnswer = q.ModelAnswer,
                            SelectedOptionIndex = answer?.SelectedOptionIndex,
                            FreeTextAnswer = answer?.FreeTextAnswer,
                            ScoreFraction = answer?.ScoreFraction ?? 0,
                        };
                    })
                    .ToList(),
            };
        }

        private static DateTime ComputeDeadline(CandidateAssessmentAttempt attempt) =>
            attempt.StartedAt.AddSeconds(attempt.JobAssessment.Questions.Count * SecondsPerQuestion);

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

            List<GeneratedQuestion> generated;
            IAssessmentQuestionGenerator usedGenerator;
            try
            {
                generated = await _generator.GenerateAsync(job, skills, QuestionCount, ct);
                if (generated.Count == 0)
                    throw new InvalidOperationException("The AI service returned no questions.");
                usedGenerator = _generator;
            }
            catch (Exception ex)
            {
                // A Gemini timeout, malformed/truncated response, or rate limit must never
                // block a candidate from starting (and therefore applying for) a job that
                // requires this assessment — same resilience posture as JobService falling
                // back to a template JD on an AI outage. Scoped tightly around just the AI
                // call, so a real "job not found" failure earlier in this method is never
                // swallowed by accident.
                _logger.LogWarning(ex, "AI assessment question generation unavailable for job {JobId}; falling back to template", job.JobId);
                generated = await _fallbackGenerator.GenerateAsync(job, skills, QuestionCount, ct);
                usedGenerator = _fallbackGenerator;
            }

            if (generated.Count == 0)
                throw new InvalidOperationException("Unable to generate a skill assessment for this job. Please try again shortly.");

            var skillIdByName = job.RequiredSkills.ToDictionary(rs => rs.Skill.SkillName, rs => rs.SkillId, StringComparer.OrdinalIgnoreCase);

            var assessment = new JobAssessment
            {
                JobId = job.JobId,
                GeneratedBy = usedGenerator.Name,
                Questions = generated.Select((q, i) => new AssessmentQuestion
                {
                    QuestionType = q.QuestionType,
                    QuestionText = q.QuestionText,
                    Options = q.Options,
                    CorrectOptionIndex = q.CorrectOptionIndex,
                    ModelAnswer = q.ModelAnswer,
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
                    QuestionType = q.QuestionType.ToString(),
                    QuestionText = q.QuestionText,
                    Options = q.Options,
                })
                .ToList();
    }
}
