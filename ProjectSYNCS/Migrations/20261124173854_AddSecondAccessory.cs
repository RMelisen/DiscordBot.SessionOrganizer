using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondAccessory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Accessory2Key",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Accessory2Style",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavedAccessoryStyles",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accessory2Key",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "Accessory2Style",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "SavedAccessoryStyles",
                table: "Plynlings");
        }
    }
}
