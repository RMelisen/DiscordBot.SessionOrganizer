using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class StoreEventAges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelfAgeSeconds",
                table: "PlynlingEventInstances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetAgeSeconds",
                table: "PlynlingEventInstances",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelfAgeSeconds",
                table: "PlynlingEventInstances");

            migrationBuilder.DropColumn(
                name: "TargetAgeSeconds",
                table: "PlynlingEventInstances");
        }
    }
}
