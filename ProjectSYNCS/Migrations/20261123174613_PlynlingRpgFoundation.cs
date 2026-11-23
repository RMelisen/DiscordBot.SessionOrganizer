using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class PlynlingRpgFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Drills",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "Energy",
                table: "Plynlings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnergyAsOf",
                table: "Plynlings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "GrowthAgility",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GrowthEndurance",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GrowthForce",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GrowthIntelligence",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GrowthSpeed",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsAgility",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsEndurance",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsForce",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsIntelligence",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsSpeed",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "RpgXp",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Drills",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "Energy",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "EnergyAsOf",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "GrowthAgility",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "GrowthEndurance",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "GrowthForce",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "GrowthIntelligence",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "GrowthSpeed",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "PointsAgility",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "PointsEndurance",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "PointsForce",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "PointsIntelligence",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "PointsSpeed",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "RpgXp",
                table: "Plynlings");
        }
    }
}
