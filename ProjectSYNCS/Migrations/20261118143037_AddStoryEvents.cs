using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndingKey",
                table: "PlynlingEventInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Flags",
                table: "PlynlingEventInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlynlingMarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    AcquiredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    InstanceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingMarks_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlynlingMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryKey = table.Column<string>(type: "TEXT", nullable: false),
                    AcquiredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevealedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    InstanceId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingMemories_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingMarks_PlynlingId_Kind_Key",
                table: "PlynlingMarks",
                columns: new[] { "PlynlingId", "Kind", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingMemories_PlynlingId_MemoryKey",
                table: "PlynlingMemories",
                columns: new[] { "PlynlingId", "MemoryKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlynlingMarks");

            migrationBuilder.DropTable(
                name: "PlynlingMemories");

            migrationBuilder.DropColumn(
                name: "EndingKey",
                table: "PlynlingEventInstances");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "PlynlingEventInstances");
        }
    }
}
