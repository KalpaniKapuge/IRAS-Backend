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
    }

    public class AssessmentQuestionForCandidateDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public List<string> Options { get; set; } = new();
    }

    public class StartAssessmentResponse
    {
        public int AttemptId { get; set; }
        public List<AssessmentQuestionForCandidateDto> Questions { get; set; } = new();
    }

    public class SubmitAssessmentAnswer
    {
        [Required]
        public int QuestionId { get; set; }
        public int SelectedOptionIndex { get; set; }
    }

    public class SubmitAssessmentRequest
    {
        [Required, MinLength(1)]
        public List<SubmitAssessmentAnswer> Answers { get; set; } = new();
    }

    public class AssessmentResultDto
    {
        public decimal Score { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
    }
}
