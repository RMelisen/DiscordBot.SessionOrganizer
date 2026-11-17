using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelMessageCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: false),
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMessageCounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Milestone = table.Column<long>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    AuthorId = table.Column<long>(type: "INTEGER", nullable: false),
                    ReachedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Announced = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMilestones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMessageCounts_GuildId_ChannelId",
                table: "ChannelMessageCounts",
                columns: new[] { "GuildId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMilestones_ChannelId_Milestone",
                table: "ChannelMilestones",
                columns: new[] { "ChannelId", "Milestone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMilestones_GuildId",
                table: "ChannelMilestones",
                column: "GuildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMessageCounts");

            migrationBuilder.DropTable(
                name: "ChannelMilestones");
        }
    }
}
