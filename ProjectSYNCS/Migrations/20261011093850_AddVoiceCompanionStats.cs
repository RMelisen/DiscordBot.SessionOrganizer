using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceCompanionStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceCompanionDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Minutes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceCompanionDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceCompanionStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Minutes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceCompanionStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceCompanionDailyStats_GuildId_Day",
                table: "VoiceCompanionDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceCompanionDailyStats_GuildId_UserId_Day",
                table: "VoiceCompanionDailyStats",
                columns: new[] { "GuildId", "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceCompanionStats_GuildId_UserId",
                table: "VoiceCompanionStats",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceCompanionDailyStats");

            migrationBuilder.DropTable(
                name: "VoiceCompanionStats");
        }
    }
}
