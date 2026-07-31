// IRAS.Application/Modules/Interviews/InterviewService.cs
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Applications.DTOs;
using IRAS.Application.Modules.Interviews.DTOs;
using IRAS.Domain.Entities.Applications;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

// "Application" (the entity) collides with "IRAS.Application" (this project's own root
// namespace, which every Modules.* namespace nests under) — same recurring gotcha as
// ApplicationService.cs. Alias it wherever the bare type is needed.
using AppEntity = IRAS.Domain.Entities.Applications.Application;

namespace IRAS.Application.Modules.Interviews
{
    public class InterviewService : IInterviewService
    {
        private static readonly ApplicationStatus[] TerminalStatuses =
            { ApplicationStatus.Rejected, ApplicationStatus.Hired, ApplicationStatus.Withdrawn };

        private static readonly Expression<Func<Interview, InterviewDto>> ToInterviewDto = i => new InterviewDto
        {
            InterviewId = i.InterviewId,
            ApplicationId = i.ApplicationId,
            JobId = i.Application.JobId,
            JobTitle = i.Application.Job.Title,
            CompanyName = i.Application.Job.Employer.CompanyName,
            CandidateId = i.Application.CandidateId,
            CandidateName = i.Application.Candidate.FirstName + " " + i.Application.Candidate.LastName,
            ScheduledAt = i.ScheduledAt,
            DurationMinutes = i.DurationMinutes,
            Mode = i.Mode.ToString(),
            Location = i.Location,
            MeetingLink = i.MeetingLink,
            InterviewerNames = i.InterviewerNames,
            Notes = i.Notes,
            Status = i.Status.ToString(),
            CancellationReason = i.CancellationReason,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        };

        private readonly IrasDbContext _db;
        private readonly IApplicationService _applications;
        private readonly INotificationService _notifications;

        public InterviewService(IrasDbContext db, IApplicationService applications, INotificationService notifications)
        {
            _db = db;
            _applications = applications;
            _notifications = notifications;
        }

        public async Task<InterviewDto> ScheduleAsync(int employerId, int applicationId, ScheduleInterviewRequest request, CancellationToken ct)
        {
            var application = await _db.Applications
                .Include(a => a.Job).ThenInclude(j => j.Employer)
                .Include(a => a.Candidate)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId, ct)
                ?? throw new KeyNotFoundException("Application not found.");
            if (application.Job.EmployerId != employerId)
                throw new KeyNotFoundException("Application not found.");
            if (TerminalStatuses.Contains(application.Status))
                throw new InvalidOperationException($"Cannot schedule an interview for an application that is already {application.Status}.");

            var mode = ValidateSchedulingInput(request.ScheduledAt, request.Mode, request.Location, request.MeetingLink);

            await EnsureNoConflictAsync(application.CandidateId, request.ScheduledAt, request.DurationMinutes, excludeInterviewId: null, ct);

            var interview = new Interview
            {
                ApplicationId = applicationId,
                ScheduledBy = employerId,
                ScheduledAt = request.ScheduledAt,
                DurationMinutes = request.DurationMinutes,
                Mode = mode,
                Location = request.Location,
                MeetingLink = request.MeetingLink,
                InterviewerNames = request.InterviewerNames,
                Notes = request.Notes
            };
            _db.Interviews.Add(interview);
            await _db.SaveChangesAsync(ct);

            // First interview booked for this application also advances its status — an
            // application already at Interview or beyond (e.g. a second interview round)
            // is left as-is.
            if (application.Status != ApplicationStatus.Interview)
                await _applications.UpdateStatusAsync(employerId, applicationId, new UpdateApplicationStatusRequest { Status = "Interview" }, ct);

            var title = $"Interview Scheduled: {application.Job.Title} at {application.Job.Employer.CompanyName}";
            var message = BuildScheduledMessage(application, interview);
            await _notifications.NotifyAsync(
                application.CandidateId, NotificationType.Interview, title, message,
                RelatedEntityType.Interview, interview.InterviewId, DeliveryChannel.Both, ct);

            return await _db.Interviews.Where(i => i.InterviewId == interview.InterviewId).Select(ToInterviewDto).FirstAsync(ct);
        }

        public async Task<InterviewDto> RescheduleAsync(int employerId, int interviewId, RescheduleInterviewRequest request, CancellationToken ct)
        {
            var interview = await LoadOwnedInterviewAsync(employerId, interviewId, ct);
            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Cannot reschedule an interview that is {interview.Status}.");

            var mode = ValidateSchedulingInput(request.ScheduledAt, request.Mode, request.Location, request.MeetingLink);
            await EnsureNoConflictAsync(interview.Application.CandidateId, request.ScheduledAt, request.DurationMinutes, excludeInterviewId: interviewId, ct);

            var oldScheduledAt = interview.ScheduledAt;

            interview.ScheduledAt = request.ScheduledAt;
            interview.DurationMinutes = request.DurationMinutes;
            interview.Mode = mode;
            interview.Location = request.Location;
            interview.MeetingLink = request.MeetingLink;
            interview.InterviewerNames = request.InterviewerNames;
            interview.Notes = request.Notes;
            interview.ScheduledBy = employerId;
            interview.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var title = $"Interview Rescheduled: {interview.Application.Job.Title} at {interview.Application.Job.Employer.CompanyName}";
            var message = $"Your interview for {interview.Application.Job.Title} has been moved from " +
                          $"{oldScheduledAt:dddd, dd MMMM yyyy 'at' HH:mm} UTC to {interview.ScheduledAt:dddd, dd MMMM yyyy 'at' HH:mm} UTC.\n\n" +
                          BuildScheduledMessage(interview.Application, interview);
            await _notifications.NotifyAsync(
                interview.Application.CandidateId, NotificationType.Interview, title, message,
                RelatedEntityType.Interview, interview.InterviewId, DeliveryChannel.Both, ct);

            return await _db.Interviews.Where(i => i.InterviewId == interviewId).Select(ToInterviewDto).FirstAsync(ct);
        }

        public async Task CancelAsync(int employerId, int interviewId, CancelInterviewRequest request, CancellationToken ct)
        {
            var interview = await LoadOwnedInterviewAsync(employerId, interviewId, ct);
            if (interview.Status is InterviewStatus.Cancelled or InterviewStatus.Completed)
                throw new InvalidOperationException($"Interview is already {interview.Status}.");

            interview.Status = InterviewStatus.Cancelled;
            interview.CancellationReason = request.Reason;
            interview.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var title = $"Interview Cancelled: {interview.Application.Job.Title} at {interview.Application.Job.Employer.CompanyName}";
            var message = $"Your interview for {interview.Application.Job.Title} originally scheduled for " +
                          $"{interview.ScheduledAt:dddd, dd MMMM yyyy 'at' HH:mm} UTC has been cancelled." +
                          (string.IsNullOrWhiteSpace(request.Reason) ? "" : $"\n\nReason: {request.Reason}");
            await _notifications.NotifyAsync(
                interview.Application.CandidateId, NotificationType.Interview, title, message,
                RelatedEntityType.Interview, interview.InterviewId, DeliveryChannel.Both, ct);
        }

        public async Task UpdateOutcomeAsync(int employerId, int interviewId, UpdateInterviewOutcomeRequest request, CancellationToken ct)
        {
            var interview = await LoadOwnedInterviewAsync(employerId, interviewId, ct);
            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Cannot set an outcome for an interview that is {interview.Status}.");

            var newStatus = ParseEnum<InterviewStatus>(request.Status, nameof(request.Status));
            if (newStatus is not (InterviewStatus.Completed or InterviewStatus.NoShow))
                throw new ArgumentException("Outcome status must be Completed or NoShow.");

            interview.Status = newStatus;
            interview.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<InterviewDto>> GetForApplicationAsync(int employerId, int applicationId, CancellationToken ct)
        {
            var owned = await _db.Applications.AnyAsync(a => a.ApplicationId == applicationId && a.Job.EmployerId == employerId, ct);
            if (!owned) throw new KeyNotFoundException("Application not found.");

            return await _db.Interviews
                .Where(i => i.ApplicationId == applicationId)
                .OrderBy(i => i.ScheduledAt)
                .Select(ToInterviewDto)
                .ToListAsync(ct);
        }

        public async Task<List<InterviewDto>> GetForEmployerAsync(int employerId, CancellationToken ct)
        {
            return await _db.Interviews
                .Where(i => i.Application.Job.EmployerId == employerId)
                .OrderBy(i => i.ScheduledAt)
                .Select(ToInterviewDto)
                .ToListAsync(ct);
        }

        public async Task<List<InterviewDto>> GetForCandidateAsync(int candidateId, CancellationToken ct)
        {
            return await _db.Interviews
                .Where(i => i.Application.CandidateId == candidateId)
                .OrderBy(i => i.ScheduledAt)
                .Select(ToInterviewDto)
                .ToListAsync(ct);
        }

        // ---- helpers ----

        private async Task<Interview> LoadOwnedInterviewAsync(int employerId, int interviewId, CancellationToken ct)
        {
            var interview = await _db.Interviews
                .Include(i => i.Application).ThenInclude(a => a.Job).ThenInclude(j => j.Employer)
                .Include(i => i.Application).ThenInclude(a => a.Candidate)
                .FirstOrDefaultAsync(i => i.InterviewId == interviewId, ct)
                ?? throw new KeyNotFoundException("Interview not found.");
            if (interview.Application.Job.EmployerId != employerId)
                throw new KeyNotFoundException("Interview not found.");
            return interview;
        }

        private static InterviewMode ValidateSchedulingInput(DateTime scheduledAt, string modeText, string? location, string? meetingLink)
        {
            if (scheduledAt <= DateTime.UtcNow)
                throw new ArgumentException("Interview time must be in the future.");

            var mode = ParseEnum<InterviewMode>(modeText, nameof(modeText));
            if (mode == InterviewMode.Onsite && string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Location is required for an onsite interview.");
            if (mode == InterviewMode.Remote && string.IsNullOrWhiteSpace(meetingLink))
                throw new ArgumentException("A meeting link is required for a remote interview.");

            return mode;
        }

        // A candidate can't be in two places at once — refuse a new/updated time that
        // overlaps another of their own interviews that's still Scheduled, regardless of
        // which employer or job it belongs to.
        private async Task EnsureNoConflictAsync(int candidateId, DateTime scheduledAt, int durationMinutes, int? excludeInterviewId, CancellationToken ct)
        {
            var newEnd = scheduledAt.AddMinutes(durationMinutes);

            var others = await _db.Interviews
                .Where(i => i.Application.CandidateId == candidateId
                         && i.Status == InterviewStatus.Scheduled
                         && (excludeInterviewId == null || i.InterviewId != excludeInterviewId))
                .Select(i => new { i.ScheduledAt, i.DurationMinutes })
                .ToListAsync(ct);

            var conflict = others.FirstOrDefault(o => scheduledAt < o.ScheduledAt.AddMinutes(o.DurationMinutes) && newEnd > o.ScheduledAt);
            if (conflict is not null)
                throw new InvalidOperationException(
                    $"This candidate already has an interview scheduled at {conflict.ScheduledAt:yyyy-MM-dd HH:mm} UTC that overlaps with the requested time.");
        }

        private static string BuildScheduledMessage(AppEntity application, Interview interview)
        {
            var lines = new List<string>
            {
                $"Your interview for {application.Job.Title} at {application.Job.Employer.CompanyName} has been scheduled.",
                "",
                $"When: {interview.ScheduledAt:dddd, dd MMMM yyyy 'at' HH:mm} UTC ({interview.DurationMinutes} minutes)",
                $"Mode: {interview.Mode}"
            };

            if (interview.Mode == InterviewMode.Onsite && !string.IsNullOrWhiteSpace(interview.Location))
                lines.Add($"Location: {interview.Location}");
            if (interview.Mode == InterviewMode.Remote && !string.IsNullOrWhiteSpace(interview.MeetingLink))
                lines.Add($"Meeting link: {interview.MeetingLink}");
            if (!string.IsNullOrWhiteSpace(interview.InterviewerNames))
                lines.Add($"Interviewer(s): {interview.InterviewerNames}");
            if (!string.IsNullOrWhiteSpace(interview.Notes))
                lines.Add($"Notes: {interview.Notes}");

            return string.Join("\n", lines);
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
