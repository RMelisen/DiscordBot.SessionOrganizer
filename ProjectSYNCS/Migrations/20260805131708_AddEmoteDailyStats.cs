using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddEmoteDailyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmoteDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    EmoteId = table.Column<long>(type: "INTEGER", nullable: false),
                    Unicode = table.Column<string>(type: "TEXT", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    WrittenCount = table.Column<long>(type: "INTEGER", nullable: false),
                    ReactedCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmoteDailyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmoteDailyStats_GuildId_Day",
                table: "EmoteDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_EmoteDailyStats_GuildId_EmoteId_Unicode_Day",
                table: "EmoteDailyStats",
                columns: new[] { "GuildId", "EmoteId", "Unicode", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmoteDailyStats");
        }
    }
}
