using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddTonightAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Sent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalReminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TonightCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrganizerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Game = table.Column<string>(type: "TEXT", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Minimum = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ThresholdAnnounced = table.Column<bool>(type: "INTEGER", nullable: false),
                    Closed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionEventId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonightCalls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TonightResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TonightCallId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonightResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TonightResponses_TonightCalls_TonightCallId",
                        column: x => x.TonightCallId,
                        principalTable: "TonightCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalReminders_Sent",
                table: "PersonalReminders",
                column: "Sent");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalReminders_UserId_Sent",
                table: "PersonalReminders",
                columns: new[] { "UserId", "Sent" });

            migrationBuilder.CreateIndex(
                name: "IX_TonightCalls_GuildId_Closed",
                table: "TonightCalls",
                columns: new[] { "GuildId", "Closed" });

            migrationBuilder.CreateIndex(
                name: "IX_TonightResponses_TonightCallId_UserId",
                table: "TonightResponses",
                columns: new[] { "TonightCallId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalReminders");

            migrationBuilder.DropTable(
                name: "TonightResponses");

            migrationBuilder.DropTable(
                name: "TonightCalls");
        }
    }
}
