using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlynlingRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingAId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlynlingBId = table.Column<int>(type: "INTEGER", nullable: false),
                    Affinity = table.Column<int>(type: "INTEGER", nullable: false),
                    Bond = table.Column<int>(type: "INTEGER", nullable: false),
                    Meetings = table.Column<int>(type: "INTEGER", nullable: false),
                    Since = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingRelations_Plynlings_PlynlingAId",
                        column: x => x.PlynlingAId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlynlingRelations_Plynlings_PlynlingBId",
                        column: x => x.PlynlingBId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingRelations_PlynlingAId_PlynlingBId",
                table: "PlynlingRelations",
                columns: new[] { "PlynlingAId", "PlynlingBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingRelations_PlynlingBId",
                table: "PlynlingRelations",
                column: "PlynlingBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlynlingRelations");
        }
    }
}
