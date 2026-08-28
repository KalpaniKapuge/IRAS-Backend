using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateLanguagesAndProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidateLanguages",
                schema: "candidate",
                columns: table => new
                {
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    LanguageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Proficiency = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateLanguages", x => x.LanguageId);
                    table.ForeignKey(
                        name: "FK_CandidateLanguages_CandidateProfiles_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "candidate",
                        principalTable: "CandidateProfiles",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateProjects",
                schema: "candidate",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProjects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_CandidateProjects_CandidateProfiles_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "candidate",
                        principalTable: "CandidateProfiles",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateLanguages_CandidateId",
                schema: "candidate",
                table: "CandidateLanguages",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProjects_CandidateId",
                schema: "candidate",
                table: "CandidateProjects",
                column: "CandidateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateLanguages",
                schema: "candidate");

            migrationBuilder.DropTable(
                name: "CandidateProjects",
                schema: "candidate");
        }
    }
}
