using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingHygiene : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing Plynling starts clean.
            migrationBuilder.AddColumn<double>(
                name: "Hygiene",
                table: "Plynlings",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);

            // Existing Plynlings start at the latest morning, so none replays mornings from before
            // the feature. Evaluated when the migration runs, not when it was written.
            migrationBuilder.AddColumn<int>(
                name: "LastMorningDay",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: ProjectSYNCS.Helpers.PlynlingLife.MorningDayAtOrBefore(DateTimeOffset.UtcNow));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hygiene",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "LastMorningDay",
                table: "Plynlings");
        }
    }
}
