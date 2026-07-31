// IRAS.Domain/Entities/Applications/Interview.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Applications
{
    public class Interview
    {
        public int InterviewId { get; set; }
        public int ApplicationId { get; set; }
        public int ScheduledBy { get; set; }   // Employer's UserId who scheduled/last modified it

        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 60;

        public InterviewMode Mode { get; set; }
        public string? Location { get; set; }        // required when Mode == Onsite
        public string? MeetingLink { get; set; }      // required when Mode == Remote
        public string? InterviewerNames { get; set; }
        public string? Notes { get; set; }

        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Application Application { get; set; } = null!;
        public User ScheduledByUser { get; set; } = null!;
    }
}
