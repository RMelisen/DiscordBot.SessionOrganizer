using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAnswerStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatAnswerDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatAnswerDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatAnswerStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatAnswerStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnswerDailyStats_GuildId_Day_Path_Kind",
                table: "ChatAnswerDailyStats",
                columns: new[] { "GuildId", "Day", "Path", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnswerStats_GuildId_Path_Kind",
                table: "ChatAnswerStats",
                columns: new[] { "GuildId", "Path", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatAnswerDailyStats");

            migrationBuilder.DropTable(
                name: "ChatAnswerStats");
        }
    }
}
