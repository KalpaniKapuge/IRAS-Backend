using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCvBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CvDocuments",
                columns: table => new
                {
                    CvId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SectionOrder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvDocuments", x => x.CvId);
                    table.ForeignKey(
                        name: "FK_CvDocuments_CandidateProfiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CvSectionItems",
                columns: table => new
                {
                    CvSectionItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CvId = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvSectionItems", x => x.CvSectionItemId);
                    table.ForeignKey(
                        name: "FK_CvSectionItems_CvDocuments_CvId",
                        column: x => x.CvId,
                        principalTable: "CvDocuments",
                        principalColumn: "CvId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvDocuments_CandidateId",
                table: "CvDocuments",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CvSectionItems_CvId",
                table: "CvSectionItems",
                column: "CvId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvSectionItems");

            migrationBuilder.DropTable(
                name: "CvDocuments");
        }
    }
}
