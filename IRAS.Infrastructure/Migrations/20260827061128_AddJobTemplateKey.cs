using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTemplateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                schema: "jobs",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateKey",
                schema: "jobs",
                table: "Jobs");
        }
    }
}
