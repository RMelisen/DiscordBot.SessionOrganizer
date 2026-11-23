using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectSYNCS.Migrations
{
    /// <summary>
    /// Data migration (docs/agents/architecture.md), the same shape as RetireBackpackAndHeadphones: the
    /// Citrouilles grave frame (seasonal, autumn, 8.6.0) leaves the game (8.8.0, the owner's call).
    /// Every copy held is refunded at its shop price — 2 400 cailloux, the seasonal price — to the
    /// holder's wallet in that guild (a new, empty wallet for someone given one who never had any),
    /// logged as a staff earning in the economy record (/admin dashboard), the inventory rows go, and
    /// any Plynling wearing it (alive or dead) wears no frame. Nothing to undo: Down is empty.
    /// The day is taken in SQL when the migration runs (never "now" in C#, architecture.md): SQLite's
    /// UTC date, so a deploy in Paris' first hour after midnight logs it on the day before.
    /// </summary>
    public partial class RetirePumpkinGraveFrame : Migration
    {
        private const string Retired = "('cos.grave.citrouilles')";
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
            migrationBuilder.Sql($"UPDATE Plynlings SET GraveKey = NULL WHERE GraveKey IN {Retired};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
