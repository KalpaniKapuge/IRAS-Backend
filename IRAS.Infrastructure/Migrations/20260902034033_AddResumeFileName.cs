using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "candidate",
                table: "Resumes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "candidate",
                table: "Resumes");
        }
    }
}
