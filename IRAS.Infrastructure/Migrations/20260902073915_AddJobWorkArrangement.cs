using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWorkArrangement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "" isn't a valid WorkArrangement enum member — existing rows need a real value
            // since the string<->enum conversion (IrasDbContext) would fail to deserialize an
            // empty string back into the enum on read. OnSite matches the entity's C# default.
            migrationBuilder.AddColumn<string>(
                name: "WorkArrangement",
                schema: "jobs",
                table: "Jobs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "OnSite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkArrangement",
                schema: "jobs",
                table: "Jobs");
        }
    }
}
