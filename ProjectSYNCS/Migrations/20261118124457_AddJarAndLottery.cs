using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddJarAndLottery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PebblePrize",
                table: "Giveaways",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CaillouxJars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Filled = table.Column<long>(type: "INTEGER", nullable: false),
                    Given = table.Column<long>(type: "INTEGER", nullable: false),
                    RewardsDone = table.Column<int>(type: "INTEGER", nullable: false),
                    DoubleDailyFrom = table.Column<int>(type: "INTEGER", nullable: false),
                    DoubleDailyUntil = table.Column<int>(type: "INTEGER", nullable: false),
                    QuizDayFrom = table.Column<int>(type: "INTEGER", nullable: false),
                    QuizDayUntil = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaillouxJars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JarGifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Total = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarGifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LotteryDraws",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    DrawDay = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Pot = table.Column<long>(type: "INTEGER", nullable: false),
                    Paid = table.Column<long>(type: "INTEGER", nullable: false),
                    Tickets = table.Column<int>(type: "INTEGER", nullable: false),
                    DrawnAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotteryDraws", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LotteryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    DrawDay = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Tickets = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotteryEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaillouxJars_GuildId",
                table: "CaillouxJars",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JarGifts_GuildId_UserId",
                table: "JarGifts",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotteryDraws_GuildId_DrawDay",
                table: "LotteryDraws",
                columns: new[] { "GuildId", "DrawDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotteryEntries_GuildId_DrawDay_UserId",
                table: "LotteryEntries",
                columns: new[] { "GuildId", "DrawDay", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaillouxJars");

            migrationBuilder.DropTable(
                name: "JarGifts");

            migrationBuilder.DropTable(
                name: "LotteryDraws");

            migrationBuilder.DropTable(
                name: "LotteryEntries");

            migrationBuilder.DropColumn(
                name: "PebblePrize",
                table: "Giveaways");
        }
    }
}
