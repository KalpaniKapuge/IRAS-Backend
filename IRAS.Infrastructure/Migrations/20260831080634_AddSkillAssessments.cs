using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assessments");

            migrationBuilder.AddColumn<bool>(
                name: "RequireAssessment",
                schema: "jobs",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AssessmentScore",
                schema: "applications",
                table: "Applications",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobAssessments",
                schema: "assessments",
                columns: table => new
                {
                    JobAssessmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAssessments", x => x.JobAssessmentId);
                    table.ForeignKey(
                        name: "FK_JobAssessments_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentQuestions",
                schema: "assessments",
                columns: table => new
                {
                    AssessmentQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobAssessmentId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "int", nullable: false),
                    QuestionOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentQuestions", x => x.AssessmentQuestionId);
                    table.ForeignKey(
                        name: "FK_AssessmentQuestions_JobAssessments_JobAssessmentId",
                        column: x => x.JobAssessmentId,
                        principalSchema: "assessments",
                        principalTable: "JobAssessments",
                        principalColumn: "JobAssessmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentQuestions_Skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "skills",
                        principalTable: "Skills",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CandidateAssessmentAttempts",
                schema: "assessments",
                columns: table => new
                {
                    AttemptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    JobAssessmentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAssessmentAttempts", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAttempts_CandidateProfiles_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "candidate",
                        principalTable: "CandidateProfiles",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAttempts_JobAssessments_JobAssessmentId",
                        column: x => x.JobAssessmentId,
                        principalSchema: "assessments",
                        principalTable: "JobAssessments",
                        principalColumn: "JobAssessmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAttempts_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CandidateAssessmentAnswers",
                schema: "assessments",
                columns: table => new
                {
                    AnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptId = table.Column<int>(type: "int", nullable: false),
                    AssessmentQuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionIndex = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAssessmentAnswers", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAnswers_AssessmentQuestions_AssessmentQuestionId",
                        column: x => x.AssessmentQuestionId,
                        principalSchema: "assessments",
                        principalTable: "AssessmentQuestions",
                        principalColumn: "AssessmentQuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateAssessmentAnswers_CandidateAssessmentAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalSchema: "assessments",
                        principalTable: "CandidateAssessmentAttempts",
                        principalColumn: "AttemptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_JobAssessmentId",
                schema: "assessments",
                table: "AssessmentQuestions",
                column: "JobAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_SkillId",
                schema: "assessments",
                table: "AssessmentQuestions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAnswers_AssessmentQuestionId",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                column: "AssessmentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAnswers_AttemptId",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAttempts_CandidateId_JobId",
                schema: "assessments",
                table: "CandidateAssessmentAttempts",
                columns: new[] { "CandidateId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAttempts_JobAssessmentId",
                schema: "assessments",
                table: "CandidateAssessmentAttempts",
                column: "JobAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAssessmentAttempts_JobId",
                schema: "assessments",
                table: "CandidateAssessmentAttempts",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssessments_JobId",
                schema: "assessments",
                table: "JobAssessments",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateAssessmentAnswers",
                schema: "assessments");

            migrationBuilder.DropTable(
                name: "AssessmentQuestions",
                schema: "assessments");

            migrationBuilder.DropTable(
                name: "CandidateAssessmentAttempts",
                schema: "assessments");

            migrationBuilder.DropTable(
                name: "JobAssessments",
                schema: "assessments");

            migrationBuilder.DropColumn(
                name: "RequireAssessment",
                schema: "jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AssessmentScore",
                schema: "applications",
                table: "Applications");
        }
    }
}
