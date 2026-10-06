using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPlynlingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastPulseDay",
                table: "Plynlings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlynlingEventInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlynlingId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventKey = table.Column<string>(type: "TEXT", nullable: false),
                    TargetPlynlingId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentInstanceId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    OptionKey = table.Column<string>(type: "TEXT", nullable: true),
                    DecidedAlone = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChallengeSucceeded = table.Column<bool>(type: "INTEGER", nullable: true),
                    ChancePercent = table.Column<int>(type: "INTEGER", nullable: true),
                    BondBefore = table.Column<int>(type: "INTEGER", nullable: true),
                    BondAfter = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlynlingEventInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlynlingEventInstances_Plynlings_PlynlingId",
                        column: x => x.PlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlynlingEventInstances_Plynlings_TargetPlynlingId",
                        column: x => x.TargetPlynlingId,
                        principalTable: "Plynlings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingEventInstances_PlynlingId_ResolvedAt",
                table: "PlynlingEventInstances",
                columns: new[] { "PlynlingId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlynlingEventInstances_TargetPlynlingId",
                table: "PlynlingEventInstances",
                column: "TargetPlynlingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlynlingEventInstances");

            migrationBuilder.DropColumn(
                name: "LastPulseDay",
                table: "Plynlings");
        }
    }
}
