using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberDailyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    XpEarned = table.Column<long>(type: "INTEGER", nullable: false),
                    ReactionsUsed = table.Column<long>(type: "INTEGER", nullable: false),
                    VoiceMinutes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberDailyStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberDailyStats_GuildId_Day",
                table: "MemberDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberDailyStats_GuildId_UserId_Day",
                table: "MemberDailyStats",
                columns: new[] { "GuildId", "UserId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberDailyStats");
        }
    }
}
