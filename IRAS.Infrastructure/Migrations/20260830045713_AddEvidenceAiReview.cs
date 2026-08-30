using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceAiReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiConfidenceScore",
                schema: "skills",
                table: "SkillPlanEvidence",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiRationale",
                schema: "skills",
                table: "SkillPlanEvidence",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoReviewed",
                schema: "skills",
                table: "SkillPlanEvidence",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiConfidenceScore",
                schema: "skills",
                table: "SkillPlanEvidence");

            migrationBuilder.DropColumn(
                name: "AiRationale",
                schema: "skills",
                table: "SkillPlanEvidence");

            migrationBuilder.DropColumn(
                name: "AutoReviewed",
                schema: "skills",
                table: "SkillPlanEvidence");
        }
    }
}
