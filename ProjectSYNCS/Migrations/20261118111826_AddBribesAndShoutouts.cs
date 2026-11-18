using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddBribesAndShoutouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BribeWeek",
                table: "PebbleWallets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BribesThisWeek",
                table: "PebbleWallets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ChouchouUntil",
                table: "PebbleWallets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShoutoutBids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ForDay = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ShownDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoutoutBids", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoutoutBids_GuildId_ForDay",
                table: "ShoutoutBids",
                columns: new[] { "GuildId", "ForDay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoutoutBids");

            migrationBuilder.DropColumn(
                name: "BribeWeek",
                table: "PebbleWallets");

            migrationBuilder.DropColumn(
                name: "BribesThisWeek",
                table: "PebbleWallets");

            migrationBuilder.DropColumn(
                name: "ChouchouUntil",
                table: "PebbleWallets");
        }
    }
}
