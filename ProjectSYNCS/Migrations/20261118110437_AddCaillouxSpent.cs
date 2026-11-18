using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddCaillouxSpent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PaidToday",
                table: "PebbleWallets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "PayDay",
                table: "PebbleWallets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "CaillouxSpent",
                table: "MemberXps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CaillouxSpent",
                table: "MemberDailyStats",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidToday",
                table: "PebbleWallets");

            migrationBuilder.DropColumn(
                name: "PayDay",
                table: "PebbleWallets");

            migrationBuilder.DropColumn(
                name: "CaillouxSpent",
                table: "MemberXps");

            migrationBuilder.DropColumn(
                name: "CaillouxSpent",
                table: "MemberDailyStats");
        }
    }
}
