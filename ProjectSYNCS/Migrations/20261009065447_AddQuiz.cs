using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "QuizChannelId",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "QuizDailyStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizDailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizRounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuestionKey = table.Column<string>(type: "TEXT", nullable: false),
                    ChoiceOrder = table.Column<string>(type: "TEXT", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClosesAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Reward = table.Column<long>(type: "INTEGER", nullable: false),
                    WinnerId = table.Column<long>(type: "INTEGER", nullable: false),
                    WonAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Closed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizRounds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Wins = table.Column<long>(type: "INTEGER", nullable: false),
                    BestMs = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizDailyStats_GuildId_Day",
                table: "QuizDailyStats",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizDailyStats_GuildId_UserId_Day",
                table: "QuizDailyStats",
                columns: new[] { "GuildId", "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizRounds_Closed",
                table: "QuizRounds",
                column: "Closed");

            migrationBuilder.CreateIndex(
                name: "IX_QuizRounds_GuildId_Day",
                table: "QuizRounds",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizStats_GuildId_UserId",
                table: "QuizStats",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizDailyStats");

            migrationBuilder.DropTable(
                name: "QuizRounds");

            migrationBuilder.DropTable(
                name: "QuizStats");

            migrationBuilder.DropColumn(
                name: "QuizChannelId",
                table: "GuildSettings");
        }
    }
}
