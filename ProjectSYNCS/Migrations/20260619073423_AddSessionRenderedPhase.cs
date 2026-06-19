using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRenderedPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RenderedPhase",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenderedPhase",
                table: "SessionEvents");
        }
    }
}
