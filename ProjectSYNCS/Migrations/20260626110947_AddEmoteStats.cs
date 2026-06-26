using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddEmoteStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmoteStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    EmoteId = table.Column<long>(type: "INTEGER", nullable: false),
                    Unicode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsAnimated = table.Column<bool>(type: "INTEGER", nullable: false),
                    WrittenCount = table.Column<long>(type: "INTEGER", nullable: false),
                    ReactedCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmoteStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmoteStats_GuildId_EmoteId_Unicode",
                table: "EmoteStats",
                columns: new[] { "GuildId", "EmoteId", "Unicode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmoteStats");
        }
    }
}
