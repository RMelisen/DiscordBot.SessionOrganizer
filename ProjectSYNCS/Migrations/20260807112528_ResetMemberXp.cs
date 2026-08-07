using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class ResetMemberXp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only, no schema change: a one-time wipe of every XP total, timed to
            // ship with the level-up card rework so the totals and the new card format
            // start clean together. The only migration in this project that touches
            // data rather than shape -- deliberately a one-off, not the beginning of a
            // reusable "reset XP" feature.
            migrationBuilder.Sql("DELETE FROM MemberXps;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo -- the original totals are gone, and no rollback can
            // invent them back.
        }
    }
}
