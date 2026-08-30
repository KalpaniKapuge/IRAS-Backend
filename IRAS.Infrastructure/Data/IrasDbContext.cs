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
        public DbSet<CandidateLanguage> CandidateLanguages => Set<CandidateLanguage>();
        public DbSet<CandidateProject> CandidateProjects => Set<CandidateProject>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<SkillAlias> SkillAliases => Set<SkillAlias>();
        public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
        public DbSet<SkillResource> SkillResources => Set<SkillResource>();
        public DbSet<CandidateTargetSkill> CandidateTargetSkills => Set<CandidateTargetSkill>();
        public DbSet<SkillImprovementPlan> SkillImprovementPlans => Set<SkillImprovementPlan>();
        public DbSet<SkillPlanStep> SkillPlanSteps => Set<SkillPlanStep>();
        public DbSet<SkillPlanEvidence> SkillPlanEvidence => Set<SkillPlanEvidence>();
        public DbSet<CvDocument> CvDocuments => Set<CvDocument>();
        public DbSet<CvSectionItem> CvSectionItems => Set<CvSectionItem>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobRequiredSkill> JobRequiredSkills => Set<JobRequiredSkill>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
        public DbSet<Interview> Interviews => Set<Interview>();
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
            // Table names, schemas, keys, indexes, and FK behavior per entity now live
            // in IRAS.Infrastructure/Persistence/Configuration/<Module>/*Configuration.cs,
            // one class per table, grouped and scoped into a SQL Server schema per module.
            b.ApplyConfigurationsFromAssembly(typeof(IrasDbContext).Assembly);

            // ---- Store enums as strings, not ints (readability in SQL Server) ----
            foreach (var entityType in b.Model.GetEntityTypes())
                foreach (var property in entityType.ClrType.GetProperties())
                    if (property.PropertyType.IsEnum)
                        b.Entity(entityType.Name).Property(property.Name)
                            .HasConversion<string>().HasMaxLength(30);
        }
    }
}