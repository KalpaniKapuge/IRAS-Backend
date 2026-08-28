using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillImprovementPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillImprovementPlans",
                schema: "skills",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstimatedDays = table.Column<int>(type: "int", nullable: false),
                    Overview = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GapReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectTask = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectExpectedOutput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillImprovementPlans", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_SkillImprovementPlans_CandidateProfiles_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "candidate",
                        principalTable: "CandidateProfiles",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillImprovementPlans_Jobs_JobId",
                        column: x => x.JobId,
                        principalSchema: "jobs",
                        principalTable: "Jobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SkillImprovementPlans_Skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "skills",
                        principalTable: "Skills",
                        principalColumn: "SkillId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillPlanSteps",
                schema: "skills",
                columns: table => new
                {
                    StepId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillPlanSteps", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_SkillPlanSteps_SkillImprovementPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "skills",
                        principalTable: "SkillImprovementPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillImprovementPlans_CandidateId_SkillId",
                schema: "skills",
                table: "SkillImprovementPlans",
                columns: new[] { "CandidateId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillImprovementPlans_JobId",
                schema: "skills",
                table: "SkillImprovementPlans",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillImprovementPlans_SkillId",
                schema: "skills",
                table: "SkillImprovementPlans",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillPlanSteps_PlanId",
                schema: "skills",
                table: "SkillPlanSteps",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillPlanSteps",
                schema: "skills");

            migrationBuilder.DropTable(
                name: "SkillImprovementPlans",
                schema: "skills");
        }
    }
}
