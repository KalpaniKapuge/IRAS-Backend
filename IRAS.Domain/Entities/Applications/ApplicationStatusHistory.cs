// IRAS.Domain/Entities/Applications/ApplicationStatusHistory.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Applications
{
    public class ApplicationStatusHistory
    {
        public int HistoryId { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationStatus OldStatus { get; set; }
        public ApplicationStatus NewStatus { get; set; }
        public int ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public Application Application { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;
    }
}