using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationLastRefusal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRefusalAt",
                table: "PlynlingRelations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRefusalAt",
                table: "PlynlingRelations");
        }
    }
}
