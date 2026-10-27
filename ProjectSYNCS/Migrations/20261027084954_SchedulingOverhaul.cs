using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class SchedulingOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SessionEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.AddColumn<int>(
                name: "RecurringSessionId",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VoiceChannelId",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "Anonymous",
                table: "Polls",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndsAt",
                table: "Polls",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionEventId",
                table: "Polls",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SingleChoice",
                table: "Polls",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RecurringSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    OrganizerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    MinuteOfDay = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    VoiceChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    LeadDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastOccurrence = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionEvents_RecurringSessionId_ScheduledAt",
                table: "SessionEvents",
                columns: new[] { "RecurringSessionId", "ScheduledAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSessions_Active_GuildId",
                table: "RecurringSessions",
                columns: new[] { "Active", "GuildId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringSessions");

            migrationBuilder.DropIndex(
                name: "IX_SessionEvents_RecurringSessionId_ScheduledAt",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "RecurringSessionId",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "VoiceChannelId",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "Anonymous",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "SessionEventId",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "SingleChoice",
                table: "Polls");
        }
    }
}
