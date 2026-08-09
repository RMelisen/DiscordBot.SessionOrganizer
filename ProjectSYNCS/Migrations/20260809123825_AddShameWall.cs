using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddShameWall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShameDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    MeanHits = table.Column<long>(type: "INTEGER", nullable: false),
                    BanVotes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShameDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShameRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    MeanHits = table.Column<long>(type: "INTEGER", nullable: false),
                    BanVotes = table.Column<long>(type: "INTEGER", nullable: false),
                    LastVoteDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShameRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShameDailyStats_GuildId_Day",
                table: "ShameDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_ShameDailyStats_GuildId_UserId_Day",
                table: "ShameDailyStats",
                columns: new[] { "GuildId", "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShameRecords_GuildId_UserId",
                table: "ShameRecords",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShameDailyStats");

            migrationBuilder.DropTable(
                name: "ShameRecords");
        }
    }
}
