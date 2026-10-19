using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class ActivityStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityHourStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Hour = table.Column<int>(type: "INTEGER", nullable: false),
                    Messages = table.Column<long>(type: "INTEGER", nullable: false),
                    VoiceMinutes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityHourStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Messages = table.Column<long>(type: "INTEGER", nullable: false),
                    VoiceMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    VoicePeak = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommandDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    Uses = table.Column<long>(type: "INTEGER", nullable: false),
                    Failures = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandDailyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityHourStats_GuildId_Day_Hour",
                table: "ActivityHourStats",
                columns: new[] { "GuildId", "Day", "Hour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDailyStats_GuildId_ChannelId_Day",
                table: "ChannelDailyStats",
                columns: new[] { "GuildId", "ChannelId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDailyStats_GuildId_Day",
                table: "ChannelDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_CommandDailyStats_GuildId_Day_Command",
                table: "CommandDailyStats",
                columns: new[] { "GuildId", "Day", "Command" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityHourStats");

            migrationBuilder.DropTable(
                name: "ChannelDailyStats");

            migrationBuilder.DropTable(
                name: "CommandDailyStats");
        }
    }
}
