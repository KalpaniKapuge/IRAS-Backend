using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTypesAndGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCorrect",
                schema: "assessments",
                table: "CandidateAssessmentAnswers");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedOptionIndex",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "FreeTextAnswer",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreFraction",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                type: "decimal(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ModelAnswer",
                schema: "assessments",
                table: "AssessmentQuestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionType",
                schema: "assessments",
                table: "AssessmentQuestions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "MultipleChoice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeTextAnswer",
                schema: "assessments",
                table: "CandidateAssessmentAnswers");

            migrationBuilder.DropColumn(
                name: "ScoreFraction",
                schema: "assessments",
                table: "CandidateAssessmentAnswers");

            migrationBuilder.DropColumn(
                name: "ModelAnswer",
                schema: "assessments",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                schema: "assessments",
                table: "AssessmentQuestions");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedOptionIndex",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                schema: "assessments",
                table: "CandidateAssessmentAnswers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
