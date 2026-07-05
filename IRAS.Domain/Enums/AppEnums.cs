// IRAS.Domain/Enums/AppEnums.cs
namespace IRAS.Domain.Enums
{
    public enum UserRole { Candidate, Employer, Admin }

    public enum EducationLevel { HighSchool, Diploma, Bachelor, Master, PhD }
    public enum ResumeFormat { PDF, DOCX }
    public enum ParseStatus { Pending, Parsed, Failed, ManuallyEdited }

    public enum ProficiencyLevel { Beginner, Intermediate, Advanced, Expert }
    public enum SkillSource { ResumeParsed, ManuallyAdded, ChatbotInferred }
    public enum SkillCategory { ProgrammingLanguage, Framework, Database, CloudPlatform, Tool, SoftSkill, Other }
    public enum AliasSource { SystemSeeded, AdminAdded, LearnedFromParsing }

    public enum ImportanceLevel { MustHave, NiceToHave }
    public enum EmploymentType { FullTime, PartTime, Contract, Internship, Remote }
    public enum JobStatus { Draft, Published, Closed, Archived }
    public enum CompanySize { Startup, Small, Medium, Large, Enterprise }

    public enum ApplicationStatus { Applied, Screened, Shortlisted, Interview, Rejected, Hired, Withdrawn }

    public enum ApprovalStatus { PendingReview, Approved, Edited, Rejected }
    public enum DeliveryStatus { Queued, Sent, Failed }
    public enum DeliveryChannel { Email, InApp, Both }

    public enum NotificationType { JobMatch, ApplicationUpdate, Feedback, System }
    public enum RelatedEntityType { Job, Application, Feedback, Candidate }

    public enum ChatSender { User, Bot }
    public enum KnowledgeCategory { FAQ, PolicyGuideline, SkillAdvice, PlatformHowTo }
}