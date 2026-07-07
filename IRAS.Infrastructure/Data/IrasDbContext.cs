// IRAS.Infrastructure/Data/IrasDbContext.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Candidate;
using IRAS.Domain.Entities.Employer;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Entities.Jobs;
using IRAS.Domain.Entities.Applications;
using IRAS.Domain.Entities.Feedback;
using IRAS.Domain.Entities.Engagement;
using IRAS.Domain.Entities.Admin;

namespace IRAS.Infrastructure.Data
{
    public class IrasDbContext : DbContext
    {
        public IrasDbContext(DbContextOptions<IrasDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
        public DbSet<EmployerProfile> EmployerProfiles => Set<EmployerProfile>();
        public DbSet<Resume> Resumes => Set<Resume>();
        public DbSet<Education> Educations => Set<Education>();
        public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
        public DbSet<Certification> Certifications => Set<Certification>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<SkillAlias> SkillAliases => Set<SkillAlias>();
        public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobRequiredSkill> JobRequiredSkills => Set<JobRequiredSkill>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
        public DbSet<SkillGap> SkillGaps => Set<SkillGap>();
        public DbSet<JobMatch> JobMatches => Set<JobMatch>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // ---- Identity: one-to-one specialization (User -> Candidate/Employer) ----
            b.Entity<CandidateProfile>().HasKey(c => c.CandidateId);
            b.Entity<CandidateProfile>()
                .HasOne(c => c.User).WithOne(u => u.CandidateProfile)
                .HasForeignKey<CandidateProfile>(c => c.CandidateId);
            
            b.Entity<EmployerProfile>().HasKey(e => e.EmployerId);

            b.Entity<EmployerProfile>()
                .HasOne(e => e.User).WithOne(u => u.EmployerProfile)
                .HasForeignKey<EmployerProfile>(e => e.EmployerId);

            b.Entity<User>().HasIndex(u => u.Email).IsUnique();          // correction #2

            // ---- Non-conventional primary key names ----
            b.Entity<AuditLog>().HasKey(a => a.LogId);
            b.Entity<ChatMessage>().HasKey(m => m.MessageId);
            b.Entity<WorkExperience>().HasKey(w => w.ExperienceId);
            b.Entity<ChatConversation>().HasKey(c => c.ConversationId);
            b.Entity<JobMatch>().HasKey(m => m.MatchId);
            b.Entity<SkillGap>().HasKey(g => g.GapId);
            b.Entity<ApplicationStatusHistory>().HasKey(h => h.HistoryId);
            b.Entity<KnowledgeBase>().HasKey(k => k.KbId);
            b.Entity<SkillAlias>().HasKey(a => a.AliasId);

            // ---- Composite keys ----
            b.Entity<CandidateSkill>().HasKey(cs => new { cs.CandidateId, cs.SkillId });
            b.Entity<JobRequiredSkill>().HasKey(jrs => new { jrs.JobId, jrs.SkillId });

            // ---- Unique constraints (correction #2) ----
            b.Entity<Application>().HasIndex(a => new { a.CandidateId, a.JobId }).IsUnique();
            b.Entity<JobMatch>().HasIndex(m => new { m.JobId, m.CandidateId }).IsUnique();

            // ---- Decimal precision (correction #7) ----
            foreach (var prop in new[] { "TotalScore", "SkillMatch", "ExperienceMatch", "EducationMatch", "SemanticSimilarity" })
                b.Entity<Application>().Property(prop).HasColumnType("decimal(5,4)");
            b.Entity<JobMatch>().Property(m => m.MatchScore).HasColumnType("decimal(5,4)");
            b.Entity<JobRequiredSkill>().Property(j => j.Weight).HasColumnType("decimal(5,4)");
            b.Entity<CandidateProfile>().Property(c => c.TotalExpYears).HasColumnType("decimal(4,1)");
            b.Entity<CandidateSkill>().Property(c => c.YearsExp).HasColumnType("decimal(4,1)");

            // ---- Skill deletion safety: block deletes when the skill is referenced ----
            b.Entity<CandidateSkill>().HasOne(cs => cs.Skill).WithMany()
                .HasForeignKey(cs => cs.SkillId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<JobRequiredSkill>().HasOne(jrs => jrs.Skill).WithMany()
                .HasForeignKey(jrs => jrs.SkillId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<SkillGap>().HasOne(g => g.Skill).WithMany()
                .HasForeignKey(g => g.SkillId).OnDelete(DeleteBehavior.Restrict);

            // ---- Skill taxonomy uniqueness ----
            b.Entity<Skill>().HasIndex(s => s.SkillName).IsUnique();
            b.Entity<SkillAlias>().HasIndex(a => a.AliasText).IsUnique();

            // ---- Restrict cascade paths that SQL Server would reject as multiple cascade paths ----
            b.Entity<Application>().HasOne(a => a.Resume).WithMany()
                .HasForeignKey(a => a.ResumeId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<Application>().HasOne(a => a.Job).WithMany()
                .HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<JobMatch>().HasOne(m => m.Job).WithMany()
                .HasForeignKey(m => m.JobId).OnDelete(DeleteBehavior.Restrict);
            b.Entity<ApplicationStatusHistory>().HasOne(h => h.ChangedByUser).WithMany()
                .HasForeignKey(h => h.ChangedBy).OnDelete(DeleteBehavior.Restrict);
            b.Entity<Feedback>().HasOne(f => f.ApprovedByUser).WithMany()
                .HasForeignKey(f => f.ApprovedBy).OnDelete(DeleteBehavior.Restrict);
            b.Entity<KnowledgeBase>().HasOne(k => k.UpdatedByUser).WithMany()
                .HasForeignKey(k => k.UpdatedBy).OnDelete(DeleteBehavior.Restrict);

            // ---- Store enums as strings, not ints (readability in SQL Server) ----
            foreach (var entityType in b.Model.GetEntityTypes())
                foreach (var property in entityType.ClrType.GetProperties())
                    if (property.PropertyType.IsEnum)
                        b.Entity(entityType.Name).Property(property.Name)
                            .HasConversion<string>().HasMaxLength(30);
        }
    }
}