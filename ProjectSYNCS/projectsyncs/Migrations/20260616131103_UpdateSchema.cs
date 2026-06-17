using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GameName",
                table: "SessionEvents",
                newName: "Title");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "SessionEvents");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "SessionEvents",
                newName: "GameName");
        }
    }
}
