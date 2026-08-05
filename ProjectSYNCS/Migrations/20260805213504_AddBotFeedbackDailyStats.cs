using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddBotFeedbackDailyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotFeedbackDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    GoodCount = table.Column<long>(type: "INTEGER", nullable: false),
                    BadCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotFeedbackDailyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotFeedbackDailyStats_GuildId_Day",
                table: "BotFeedbackDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_BotFeedbackDailyStats_GuildId_UserId_Day",
                table: "BotFeedbackDailyStats",
                columns: new[] { "GuildId", "UserId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotFeedbackDailyStats");
        }
    }
}
