using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingSickness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing Plynling starts healthy, and every existing death reads as starvation
            // (DeathCause 0), which is what it was.
            migrationBuilder.AddColumn<int>(
                name: "DeathCause",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastMedicineAt",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Recovery",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SickNotified",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SickSince",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeathCause",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "LastMedicineAt",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "Recovery",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "SickNotified",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "SickSince",
                table: "Plynlings");
        }
    }
}
