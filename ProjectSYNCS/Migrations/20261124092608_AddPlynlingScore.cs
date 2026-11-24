using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Score",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "ScoreRebuilt",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TombstoneFloor",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScoredBonds",
                table: "PlynlingRelations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlynlingOwnerScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingOwnerScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlynlingScoreDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingScoreDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingScoreDays_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingOwnerScores_GuildId_UserId",
                table: "PlynlingOwnerScores",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingScoreDays_GuildId_Day",
                table: "PlynlingScoreDays",
                columns: new[] { "GuildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingScoreDays_PlynlingId_Day",
                table: "PlynlingScoreDays",
                columns: new[] { "PlynlingId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlynlingOwnerScores");

            migrationBuilder.DropTable(
                name: "PlynlingScoreDays");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "ScoreRebuilt",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "TombstoneFloor",
                table: "Plynlings");

            migrationBuilder.DropColumn(
                name: "ScoredBonds",
                table: "PlynlingRelations");
        }
    }
}
