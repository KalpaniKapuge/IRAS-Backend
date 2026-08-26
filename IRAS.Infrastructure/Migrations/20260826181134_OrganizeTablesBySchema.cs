using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrganizeTablesBySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "applications");

            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.EnsureSchema(
                name: "candidate");

            migrationBuilder.EnsureSchema(
                name: "skills");

            migrationBuilder.EnsureSchema(
                name: "engagement");

            migrationBuilder.EnsureSchema(
                name: "employer");

            migrationBuilder.EnsureSchema(
                name: "feedback");

            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.RenameTable(
                name: "WorkExperiences",
                newName: "WorkExperiences",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "Skills",
                newName: "Skills",
                newSchema: "skills");

            migrationBuilder.RenameTable(
                name: "SkillResources",
                newName: "SkillResources",
                newSchema: "skills");

            migrationBuilder.RenameTable(
                name: "SkillGaps",
                newName: "SkillGaps",
                newSchema: "applications");

            migrationBuilder.RenameTable(
                name: "SkillAliases",
                newName: "SkillAliases",
                newSchema: "skills");

            migrationBuilder.RenameTable(
                name: "Resumes",
                newName: "Resumes",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "Notifications",
                newSchema: "engagement");

            migrationBuilder.RenameTable(
                name: "KnowledgeBases",
                newName: "KnowledgeBases",
                newSchema: "admin");

            migrationBuilder.RenameTable(
                name: "Jobs",
                newName: "Jobs",
                newSchema: "jobs");

            migrationBuilder.RenameTable(
                name: "JobRequiredSkills",
                newName: "JobRequiredSkills",
                newSchema: "jobs");

            migrationBuilder.RenameTable(
                name: "JobMatches",
                newName: "JobMatches",
                newSchema: "jobs");

            migrationBuilder.RenameTable(
                name: "Interviews",
                newName: "Interviews",
                newSchema: "applications");

            migrationBuilder.RenameTable(
                name: "Feedbacks",
                newName: "Feedbacks",
                newSchema: "feedback");

            migrationBuilder.RenameTable(
                name: "EmployerProfiles",
                newName: "EmployerProfiles",
                newSchema: "employer");

            migrationBuilder.RenameTable(
                name: "Educations",
                newName: "Educations",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "CvSectionItems",
                newName: "CvSectionItems",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "CvDocuments",
                newName: "CvDocuments",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                newName: "ChatMessages",
                newSchema: "engagement");

            migrationBuilder.RenameTable(
                name: "ChatConversations",
                newName: "ChatConversations",
                newSchema: "engagement");

            migrationBuilder.RenameTable(
                name: "Certifications",
                newName: "Certifications",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "CandidateTargetSkills",
                newName: "CandidateTargetSkills",
                newSchema: "skills");

            migrationBuilder.RenameTable(
                name: "CandidateSkills",
                newName: "CandidateSkills",
                newSchema: "skills");

            migrationBuilder.RenameTable(
                name: "CandidateProfiles",
                newName: "CandidateProfiles",
                newSchema: "candidate");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "admin");

            migrationBuilder.RenameTable(
                name: "ApplicationStatusHistories",
                newName: "ApplicationStatusHistories",
                newSchema: "applications");

            migrationBuilder.RenameTable(
                name: "Applications",
                newName: "Applications",
                newSchema: "applications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkExperiences",
                schema: "candidate",
                newName: "WorkExperiences");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "identity",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Skills",
                schema: "skills",
                newName: "Skills");

            migrationBuilder.RenameTable(
                name: "SkillResources",
                schema: "skills",
                newName: "SkillResources");

            migrationBuilder.RenameTable(
                name: "SkillGaps",
                schema: "applications",
                newName: "SkillGaps");

            migrationBuilder.RenameTable(
                name: "SkillAliases",
                schema: "skills",
                newName: "SkillAliases");

            migrationBuilder.RenameTable(
                name: "Resumes",
                schema: "candidate",
                newName: "Resumes");

            migrationBuilder.RenameTable(
                name: "Notifications",
                schema: "engagement",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "KnowledgeBases",
                schema: "admin",
                newName: "KnowledgeBases");

            migrationBuilder.RenameTable(
                name: "Jobs",
                schema: "jobs",
                newName: "Jobs");

            migrationBuilder.RenameTable(
                name: "JobRequiredSkills",
                schema: "jobs",
                newName: "JobRequiredSkills");

            migrationBuilder.RenameTable(
                name: "JobMatches",
                schema: "jobs",
                newName: "JobMatches");

            migrationBuilder.RenameTable(
                name: "Interviews",
                schema: "applications",
                newName: "Interviews");

            migrationBuilder.RenameTable(
                name: "Feedbacks",
                schema: "feedback",
                newName: "Feedbacks");

            migrationBuilder.RenameTable(
                name: "EmployerProfiles",
                schema: "employer",
                newName: "EmployerProfiles");

            migrationBuilder.RenameTable(
                name: "Educations",
                schema: "candidate",
                newName: "Educations");

            migrationBuilder.RenameTable(
                name: "CvSectionItems",
                schema: "candidate",
                newName: "CvSectionItems");

            migrationBuilder.RenameTable(
                name: "CvDocuments",
                schema: "candidate",
                newName: "CvDocuments");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                schema: "engagement",
                newName: "ChatMessages");

            migrationBuilder.RenameTable(
                name: "ChatConversations",
                schema: "engagement",
                newName: "ChatConversations");

            migrationBuilder.RenameTable(
                name: "Certifications",
                schema: "candidate",
                newName: "Certifications");

            migrationBuilder.RenameTable(
                name: "CandidateTargetSkills",
                schema: "skills",
                newName: "CandidateTargetSkills");

            migrationBuilder.RenameTable(
                name: "CandidateSkills",
                schema: "skills",
                newName: "CandidateSkills");

            migrationBuilder.RenameTable(
                name: "CandidateProfiles",
                schema: "candidate",
                newName: "CandidateProfiles");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "admin",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "ApplicationStatusHistories",
                schema: "applications",
                newName: "ApplicationStatusHistories");

            migrationBuilder.RenameTable(
                name: "Applications",
                schema: "applications",
                newName: "Applications");
        }
    }
}
