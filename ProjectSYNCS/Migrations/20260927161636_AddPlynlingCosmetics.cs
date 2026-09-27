using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingCosmetics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessoryKey",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GraveKey",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeKey",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleKey",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessoryKey",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "GraveKey",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "ThemeKey",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "TitleKey",
                table: "Plynlings");
        }
    }
}
