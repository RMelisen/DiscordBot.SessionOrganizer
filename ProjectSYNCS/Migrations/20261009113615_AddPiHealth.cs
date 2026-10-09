using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPiHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PiThermalDays",
                columns: table => new
                {
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    MinMilli = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMilli = table.Column<int>(type: "INTEGER", nullable: false),
                    SumMilli = table.Column<long>(type: "INTEGER", nullable: false),
                    Samples = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    HotMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PiThermalDays", x => x.Day);
                });

            migrationBuilder.CreateTable(
                name: "UptimeEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UptimeEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UptimeEvents_Kind",
                table: "UptimeEvents",
                column: "Kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PiThermalDays");

            migrationBuilder.DropTable(
                name: "UptimeEvents");
        }
    }
}
