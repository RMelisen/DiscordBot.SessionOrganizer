using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectSYNCS.Helpers;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <inheritdoc />
    public partial class PrepareProdLaunch : Migration
    {
        // The production server, where a beta ran before the Plynlings' launch.
        private const long ProdGuildId = 878305033995825164;

        // Pwet's owner, and the test server Pwet lives on today.
        private const long CloneOwnerId = 573225362532859935;
        private const long CloneSourceGuildId = 1516718983040991273;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only, no schema change, and scoped to the production server throughout: the
            // same SQLite file also holds the test servers, which must not be touched.

            // 1. A fresh economy for the launch. Everything the beta earned is wiped — balance,
            //    today's passive cap, the /work and forage cooldowns — along with the dashboard's
            //    history, which would otherwise chart money that no longer exists.
            migrationBuilder.Sql(
                $"UPDATE PebbleWallets SET Balance = 0, PassiveDay = 0, PassiveToday = 0, " +
                $"LastWorkAt = NULL, LastForageAt = NULL WHERE GuildId = {ProdGuildId};");
            migrationBuilder.Sql($"DELETE FROM EconomyDailyStats WHERE GuildId = {ProdGuildId};");

            // 2. A baby clone of Pwet on the production server: same name, species, gender and
            //    passions, but born now — no age, badges, journal, relations or cosmetics. Taken
            //    from the living one if there is one, else the most recent. Skipped when the owner
            //    already has a living Plynling there, which the unique index would refuse anyway.
            //    Computed here rather than in SQL because the timestamps and the morning key need
            //    the Paris clock; Up runs when the migration is applied, so "now" is launch time.
            var now = DateTimeOffset.UtcNow;
            var at = now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "+00:00";
            var needs = PlynlingLife.StartNeeds.ToString(CultureInfo.InvariantCulture);
            var morning = PlynlingLife.MorningDayAtOrBefore(now);

            migrationBuilder.Sql($@"
INSERT INTO Plynlings (
    GuildId, OwnerId, Name, Species, Gender, Passion, TaughtPassion, AdoptedAt,
    Hunger, Happiness, Hygiene, NeedsAsOf, AgeBankedSeconds, LiveSince,
    FrozenByStaff, WarningSent, Plays, PlaysWon, Visits, Meals, Pets, FedByOthers,
    LastGiftDay, LastMorningDay, Recovery, SickNotified, DeathAnnounced, DeathCause)
SELECT
    {ProdGuildId}, OwnerId, Name, Species, Gender, Passion, TaughtPassion, '{at}',
    {needs}, {needs}, 1.0, '{at}', 0, '{at}',
    0, 0, 0, 0, 0, 0, 0, 0,
    0, {morning}, 0, 0, 0, 0
FROM Plynlings
WHERE GuildId = {CloneSourceGuildId} AND OwnerId = {CloneOwnerId}
  AND NOT EXISTS (SELECT 1 FROM Plynlings
                  WHERE GuildId = {ProdGuildId} AND OwnerId = {CloneOwnerId} AND DiedAt IS NULL)
ORDER BY DiedAt IS NOT NULL, Id DESC
LIMIT 1;");

            // Its first journal moment (JournalKind.Adopted = 0), like any adoption.
            migrationBuilder.Sql($@"
INSERT INTO PlynlingJournalEntries (PlynlingId, At, Kind, Detail)
SELECT p.Id, '{at}', 0, NULL
FROM Plynlings p
WHERE p.GuildId = {ProdGuildId} AND p.OwnerId = {CloneOwnerId} AND p.DiedAt IS NULL
  AND NOT EXISTS (SELECT 1 FROM PlynlingJournalEntries e WHERE e.PlynlingId = p.Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the beta's balances and dashboard history are gone, and the clone
            // may have lived a life since — deleting it would be a death nobody chose.
        }
    }
}
