using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class ResetXpTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only, no schema change: the second one-time XP wipe, requested after
            // the voice-XP taper landed so everyone starts again under the new economy.
            //
            // UPDATE, not DELETE -- and that is the difference from ResetMemberXp.
            // When that one ran, MemberXps held nothing but TotalXp, so dropping the
            // rows *was* zeroing the XP. The table now also carries ReactionsUsed and
            // VoiceMinutes, which are facts about what people actually did rather than
            // rewards: deleting them would falsify the Réactions and Vocal views of
            // /leaderboard, which is not what "reset the XP" asks for.
            migrationBuilder.Sql("UPDATE MemberXps SET TotalXp = 0;");

            // The daily buckets carry the same split, so only the XP column is cleared.
            // Leaving it would let /leaderboard's 7- and 30-day XP windows keep showing
            // XP that all-time no longer knows about.
            migrationBuilder.Sql("UPDATE MemberDailyStats SET XpEarned = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo -- the original totals are gone, and no rollback can
            // invent them back. This is the one kind of migration whose Down genuinely
            // cannot restore anything.
        }
    }
}
