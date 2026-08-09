using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfidyCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PerfidyHits",
                table: "ShameRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PerfidyHits",
                table: "ShameDailyStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerfidyHits",
                table: "ShameRecords");

            migrationBuilder.DropColumn(
                name: "PerfidyHits",
                table: "ShameDailyStats");
        }
    }
}
