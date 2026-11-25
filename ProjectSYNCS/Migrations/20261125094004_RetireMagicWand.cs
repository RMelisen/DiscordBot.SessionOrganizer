using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <summary>
    /// Data migration (docs/agents/architecture.md), the same shape as RetirePumpkinGraveFrame: the
    /// baguette magique leaves the game (the owner's call; the Gentleman collection's canne à pommeau
    /// takes its place). Every copy held is refunded at the price it always sold at — 2 400 cailloux,
    /// rare — to the holder's wallet in that guild (a new, empty wallet for someone given one who never
    /// had any), logged as a staff earning in the economy record (/admin dashboard), and the inventory
    /// rows go. Any Plynling wearing it (alive or dead), in any of the three accessory slots, loses it
    /// with its style, and the later accessories move up so no slot sits empty in front of a filled one
    /// (the bot never wears an item twice, so one slot is freed, but any pattern closes). A remembered style in
    /// SavedAccessoryStyles is left: it is only read for an item being put on, which this one never is.
    /// Nothing to undo: Down is empty. The day is taken in SQL when the migration runs (never "now" in
    /// C#, architecture.md): SQLite's UTC date.
    /// </summary>
    public partial class RetireMagicWand : Migration
    {
        private const string Retired = "('cos.accessory.baguette')";
        private const long Refund = 2400;
        private const string Held = "Key IN " + Retired + " AND Quantity > 0";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
INSERT INTO PebbleWallets (GuildId, UserId, Balance, BribeWeek, BribesThisWeek, DailyBest, DailyDay, DailyStreak,
                           PaidToday, PassiveDay, PassiveToday, PayDay, WorkGraceDay, WorkReminder)
SELECT DISTINCT i.GuildId, i.UserId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 FROM InventoryItems i
WHERE i.{Held} AND NOT EXISTS (SELECT 1 FROM PebbleWallets w WHERE w.GuildId = i.GuildId AND w.UserId = i.UserId);");
            migrationBuilder.Sql($@"
INSERT INTO EconomyDailyStats (GuildId, Day, Metric, Value)
SELECT GuildId, CAST(strftime('%Y%m%d', 'now') AS INTEGER), 'earn.admin', 0 FROM InventoryItems
WHERE {Held} GROUP BY GuildId
ON CONFLICT (GuildId, Day, Metric) DO NOTHING;");
            migrationBuilder.Sql($@"
UPDATE EconomyDailyStats SET Value = Value + {Refund} * (
    SELECT COALESCE(SUM(i.Quantity), 0) FROM InventoryItems i WHERE i.GuildId = EconomyDailyStats.GuildId AND i.{Held})
WHERE Metric = 'earn.admin' AND Day = CAST(strftime('%Y%m%d', 'now') AS INTEGER);");
            migrationBuilder.Sql($@"
UPDATE PebbleWallets SET Balance = Balance + {Refund} * (
    SELECT COALESCE(SUM(i.Quantity), 0) FROM InventoryItems i
    WHERE i.GuildId = PebbleWallets.GuildId AND i.UserId = PebbleWallets.UserId AND i.Key IN {Retired} AND i.Quantity > 0)
WHERE EXISTS (
    SELECT 1 FROM InventoryItems i
    WHERE i.GuildId = PebbleWallets.GuildId AND i.UserId = PebbleWallets.UserId AND i.Key IN {Retired} AND i.Quantity > 0);");
            migrationBuilder.Sql($"DELETE FROM InventoryItems WHERE Key IN {Retired};");

            // Off whichever slot holds it, then the gaps closed, slot 2 first: slot 2 empty takes 3 up
            // (so a filled slot 3 always has a filled slot 2), then slot 1 empty takes 2 and 3 up.
            // That order closes every pattern, two freed slots included.
            migrationBuilder.Sql($"UPDATE Plynlings SET AccessoryKey = NULL, AccessoryStyle = NULL WHERE AccessoryKey IN {Retired};");
            migrationBuilder.Sql($"UPDATE Plynlings SET Accessory2Key = NULL, Accessory2Style = NULL WHERE Accessory2Key IN {Retired};");
            migrationBuilder.Sql($"UPDATE Plynlings SET Accessory3Key = NULL, Accessory3Style = NULL WHERE Accessory3Key IN {Retired};");
            migrationBuilder.Sql(@"
UPDATE Plynlings SET Accessory2Key = Accessory3Key, Accessory2Style = Accessory3Style,
                     Accessory3Key = NULL, Accessory3Style = NULL
WHERE Accessory2Key IS NULL AND Accessory3Key IS NOT NULL;");
            migrationBuilder.Sql(@"
UPDATE Plynlings SET AccessoryKey = Accessory2Key, AccessoryStyle = Accessory2Style,
                     Accessory2Key = Accessory3Key, Accessory2Style = Accessory3Style,
                     Accessory3Key = NULL, Accessory3Style = NULL
WHERE AccessoryKey IS NULL AND (Accessory2Key IS NOT NULL OR Accessory3Key IS NOT NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
