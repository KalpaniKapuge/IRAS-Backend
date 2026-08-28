using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeSourceCvId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceCvId",
                schema: "candidate",
                table: "Resumes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_SourceCvId",
                schema: "candidate",
                table: "Resumes",
                column: "SourceCvId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resumes_CvDocuments_SourceCvId",
                schema: "candidate",
                table: "Resumes",
                column: "SourceCvId",
                principalSchema: "candidate",
                principalTable: "CvDocuments",
                principalColumn: "CvId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resumes_CvDocuments_SourceCvId",
                schema: "candidate",
                table: "Resumes");

            migrationBuilder.DropIndex(
                name: "IX_Resumes_SourceCvId",
                schema: "candidate",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "SourceCvId",
                schema: "candidate",
                table: "Resumes");
        }
    }
}
