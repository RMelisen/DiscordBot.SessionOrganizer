using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Count",
                table: "CollectionCompletions",
                type: "INTEGER",
                nullable: false,
                // Every existing row is one completion (its discovery), so it starts at Bronze.
                // Only the column's default for the rows already there: the model sets Count itself.
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "CollectionDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Completions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionDailyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionDailyStats_GuildId_UserId_Day",
                table: "CollectionDailyStats",
                columns: new[] { "GuildId", "UserId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionDailyStats");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "CollectionCompletions");
        }
    }
}
