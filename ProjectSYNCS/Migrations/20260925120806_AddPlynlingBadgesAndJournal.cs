using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingBadgesAndJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FedByOthers",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Meals",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Pets",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "PlynlingBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    EarnedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingBadges_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlynlingJournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Detail = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingJournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingJournalEntries_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingBadges_PlynlingId_Key",
                table: "PlynlingBadges",
                columns: new[] { "PlynlingId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingJournalEntries_PlynlingId",
                table: "PlynlingJournalEntries",
                column: "PlynlingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlynlingBadges");

            migrationBuilder.DropTable(
                name: "PlynlingJournalEntries");

            migrationBuilder.DropColumn(
                name: "FedByOthers",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "Meals",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "Pets",
                table: "Plynlings");
        }
    }
}
