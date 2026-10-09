using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class ConfigChannelsAndVoters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GameChannelId",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MainChannelId",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "GuildIdleChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildIdleChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildShameVoters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildShameVoters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildIdleChannels_GuildId",
                table: "GuildIdleChannels",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildIdleChannels_GuildId_ChannelId",
                table: "GuildIdleChannels",
                columns: new[] { "GuildId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildShameVoters_GuildId",
                table: "GuildShameVoters",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildShameVoters_GuildId_UserId",
                table: "GuildShameVoters",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildIdleChannels");

            migrationBuilder.DropTable(
                name: "GuildShameVoters");

            migrationBuilder.DropColumn(
                name: "GameChannelId",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "MainChannelId",
                table: "GuildSettings");
        }
    }
}
