using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddMinecraftLinksAndStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MinecraftLinks",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Player = table.Column<string>(type: "TEXT", nullable: false),
                    PlayerKey = table.Column<string>(type: "TEXT", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinecraftLinks", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "MinecraftStats",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinecraftStats", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftLinks_PlayerKey",
                table: "MinecraftLinks",
                column: "PlayerKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinecraftLinks");

            migrationBuilder.DropTable(
                name: "MinecraftStats");
        }
    }
}
