using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingsAndPebbles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PebbleWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Balance = table.Column<long>(type: "INTEGER", nullable: false),
                    LastWorkAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PassiveDay = table.Column<int>(type: "INTEGER", nullable: false),
                    PassiveToday = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PebbleWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plynlings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Species = table.Column<int>(type: "INTEGER", nullable: false),
                    AdoptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Hunger = table.Column<double>(type: "REAL", nullable: false),
                    Happiness = table.Column<double>(type: "REAL", nullable: false),
                    NeedsAsOf = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AgeBankedSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    LiveSince = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FrozenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FreezeUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FrozenByStaff = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSelfThawAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    WarningSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeathAnnounced = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plynlings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PebbleWallets_GuildId_UserId",
                table: "PebbleWallets",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plynlings_GuildId",
                table: "Plynlings",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Plynlings_GuildId_OwnerId",
                table: "Plynlings",
                columns: new[] { "GuildId", "OwnerId" },
                unique: true,
                filter: "\"DiedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PebbleWallets");

            migrationBuilder.DropTable(
                name: "Plynlings");
        }
    }
}
