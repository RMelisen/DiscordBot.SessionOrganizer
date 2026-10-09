using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LateCallSent",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RecapSent",
                table: "SessionEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstSeenInVoiceAt",
                table: "Participants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateCallSent",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "RecapSent",
                table: "SessionEvents");

            migrationBuilder.DropColumn(
                name: "FirstSeenInVoiceAt",
                table: "Participants");
        }
    }
}
