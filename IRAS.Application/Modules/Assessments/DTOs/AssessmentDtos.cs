// IRAS.Application/Modules/Assessments/DTOs/AssessmentDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Assessments.DTOs
{
    public class AssessmentStatusDto
    {
        public bool RequireAssessment { get; set; }
        public bool HasAttempted { get; set; }
        public bool IsCompleted { get; set; }
        public decimal? Score { get; set; }

        // Set only when there's an in-progress (not yet completed) attempt — lets a reloaded
        // page resume the same countdown instead of restarting it.
        public DateTime? DeadlineAt { get; set; }
    }

    public class AssessmentQuestionForCandidateDto
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = null!; // "MultipleChoice" | "FreeText"
        public string QuestionText { get; set; } = null!;
        public List<string> Options { get; set; } = new(); // empty for FreeText
    }

    public class StartAssessmentResponse
    {
        public int AttemptId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime DeadlineAt { get; set; }
        public List<AssessmentQuestionForCandidateDto> Questions { get; set; } = new();
    }

    public class SubmitAssessmentAnswer
    {
        [Required]
        public int QuestionId { get; set; }
        public int? SelectedOptionIndex { get; set; }
        public string? FreeTextAnswer { get; set; }
    }

    public class SubmitAssessmentRequest
    {
        // No [MinLength(1)] — a submission with zero or partial answers is valid (the timer
        // ran out, or the candidate is submitting early on purpose); unanswered questions
        // simply score 0.
        public List<SubmitAssessmentAnswer> Answers { get; set; } = new();
    }

    public class AssessmentResultDto
    {
        public decimal Score { get; set; }
        public int CorrectCount { get; set; }   // questions scored >= 0.6, for a friendly "X/N" display
        public int AnsweredCount { get; set; }
        public int TotalQuestions { get; set; }
    }

    // Employer-facing view of a completed attempt — unlike AssessmentQuestionForCandidateDto,
    // this exposes the answer key (CorrectOptionIndex/ModelAnswer) alongside what the
    // candidate actually answered, so the employer can judge the quiz for themselves.
    public class AssessmentQuestionReviewDto
    {
        public string QuestionType { get; set; } = null!;
        public string QuestionText { get; set; } = null!;
        public List<string> Options { get; set; } = new();
        public int? CorrectOptionIndex { get; set; }
        public string? ModelAnswer { get; set; }
        public int? SelectedOptionIndex { get; set; }
        public string? FreeTextAnswer { get; set; }
        public decimal ScoreFraction { get; set; }
    }

    public class EmployerAssessmentReviewDto
    {
        public decimal Score { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<AssessmentQuestionReviewDto> Questions { get; set; } = new();
    }
}
