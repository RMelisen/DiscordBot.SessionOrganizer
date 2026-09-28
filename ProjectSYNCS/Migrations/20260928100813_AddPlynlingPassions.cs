using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingPassions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Passion",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaughtPassion",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TaughtPassionAt",
                table: "Plynlings",
                type: "TEXT",
                nullable: true);

            // Plynlings older than passions get one, once, from their id: stable, and no runtime
            // roll. New ones roll theirs at adoption. The third migration here that carries data —
            // see CLAUDE.md; nothing is wiped, a required trait is filled in.
            migrationBuilder.Sql("UPDATE \"Plynlings\" SET \"Passion\" = (\"Id\" * 5 + 1) % 12;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Passion",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "TaughtPassion",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "TaughtPassionAt",
                table: "Plynlings");
        }
    }
}
