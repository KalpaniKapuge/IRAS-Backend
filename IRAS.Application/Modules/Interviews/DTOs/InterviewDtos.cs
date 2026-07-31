// IRAS.Application/Modules/Interviews/DTOs/InterviewDtos.cs
using System.ComponentModel.DataAnnotations;

namespace IRAS.Application.Modules.Interviews.DTOs
{
    public class ScheduleInterviewRequest
    {
        [Required]
        public DateTime ScheduledAt { get; set; }   // UTC; must be in the future

        [Range(15, 480)]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public string Mode { get; set; } = null!;   // Onsite | Remote | Phone

        [StringLength(300)]
        public string? Location { get; set; }        // required when Mode == Onsite

        [StringLength(500)]
        public string? MeetingLink { get; set; }      // required when Mode == Remote

        [StringLength(300)]
        public string? InterviewerNames { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }

    public class RescheduleInterviewRequest
    {
        [Required]
        public DateTime ScheduledAt { get; set; }

        [Range(15, 480)]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public string Mode { get; set; } = null!;

        [StringLength(300)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? MeetingLink { get; set; }

        [StringLength(300)]
        public string? InterviewerNames { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }

    public class CancelInterviewRequest
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class UpdateInterviewOutcomeRequest
    {
        [Required]
        public string Status { get; set; } = null!;   // Completed | NoShow
    }

    public class InterviewDto
    {
        public int InterviewId { get; set; }
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public string? CompanyName { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; }
        public string Mode { get; set; } = null!;
        public string? Location { get; set; }
        public string? MeetingLink { get; set; }
        public string? InterviewerNames { get; set; }
        public string? Notes { get; set; }

        public string Status { get; set; } = null!;
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
