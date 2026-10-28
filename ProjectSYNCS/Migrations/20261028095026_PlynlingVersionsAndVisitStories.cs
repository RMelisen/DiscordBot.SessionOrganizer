using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class PlynlingVersionsAndVisitStories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastVisitDay",
                table: "PlynlingRelations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PlynlingRelations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VisitStoryRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Story = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitStoryRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitStoryRecords_Day",
                table: "VisitStoryRecords",
                column: "Day");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitStoryRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "LastVisitDay",
                table: "PlynlingRelations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PlynlingRelations");
        }
    }
}
