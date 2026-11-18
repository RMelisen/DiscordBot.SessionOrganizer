using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class DropJarQuizDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuizDayFrom",
                table: "CaillouxJars");

            migrationBuilder.DropColumn(
                name: "QuizDayUntil",
                table: "CaillouxJars");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuizDayFrom",
                table: "CaillouxJars",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuizDayUntil",
                table: "CaillouxJars",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
