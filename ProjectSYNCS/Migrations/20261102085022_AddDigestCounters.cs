using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddDigestCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandMemberDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandMemberDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PiBodyDays",
                columns: table => new
                {
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    DatabaseBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    WalPeakBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DiskFreeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    DiskTotalBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    ProcessPeakBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    MemoryMinAvailableBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    MemoryTotalBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    UnderVoltageMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SlowedMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiBodyDays", x => x.Day);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandMemberDailyStats_GuildId_Day_Command_UserId",
                table: "CommandMemberDailyStats",
                columns: new[] { "GuildId", "Day", "Command", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentDailyStats_Day_Kind_Key",
                table: "IncidentDailyStats",
                columns: new[] { "Day", "Kind", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandMemberDailyStats");

            migrationBuilder.DropTable(
                name: "IncidentDailyStats");

            migrationBuilder.DropTable(
                name: "PiBodyDays");
        }
    }
}
