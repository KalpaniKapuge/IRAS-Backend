// IRAS.Domain/Entities/Admin/KnowledgeBase.cs
using IRAS.Domain.Enums;
using IRAS.Domain.Entities.Identity;

namespace IRAS.Domain.Entities.Admin
{
    public class KnowledgeBase
    {
        public int KbId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public KnowledgeCategory Category { get; set; }
        public bool IsActive { get; set; } = true;
        public int UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User UpdatedByUser { get; set; } = null!;
    }
}