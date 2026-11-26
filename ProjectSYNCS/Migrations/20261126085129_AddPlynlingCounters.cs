using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlynlingCarers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Meals = table.Column<long>(type: "INTEGER", nullable: false),
                    Pets = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingCarers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingCarers_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlynlingCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingCounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingCounters_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingCarers_PlynlingId_UserId",
                table: "PlynlingCarers",
                columns: new[] { "PlynlingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingCounters_PlynlingId_Key",
                table: "PlynlingCounters",
                columns: new[] { "PlynlingId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlynlingCarers");

            migrationBuilder.DropTable(
                name: "PlynlingCounters");
        }
    }
}
