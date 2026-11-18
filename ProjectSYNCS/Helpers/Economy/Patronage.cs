using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Counts the cailloux someone spent, for the « Mécène » badge and the « Mécènes » leaderboard tab.
/// Only what leaves the economy counts: the shop, meals (own and others'), medicine, cosmetics,
/// crafting, packs, the /pay tax. Not /arcade (it moves cailloux between players), not admin
/// debits, not the amount sent with /pay. Call it beside the spend's <see cref="EconomyLog"/> line.
/// </summary>
/// <remarks>
/// Writes the totals + buckets pair behind /leaderboard (<see cref="MemberXp"/> and today's
/// <see cref="MemberDailyStat"/>). Never saves, like <see cref="EconomyLog.AddAsync"/>: the
/// action's own save carries it, so a refused or failed spend counts nothing.
/// </remarks>
public static class Patronage
{
    public static async Task AddAsync(AppDbContext db, ulong guildId, ulong userId, long amount, DateTimeOffset now)
    {
        if (amount <= 0) return;

        var total = db.MemberXps.Local.FirstOrDefault(r => r.GuildId == guildId && r.UserId == userId)
                    ?? await db.MemberXps.FirstOrDefaultAsync(r => r.GuildId == guildId && r.UserId == userId);
        if (total is null)
        {
            total = new MemberXp { GuildId = guildId, UserId = userId };
            db.MemberXps.Add(total);
        }
        total.CaillouxSpent += amount;

        var day = AppTime.DayKey(now);
        var bucket = db.MemberDailyStats.Local.FirstOrDefault(r => r.GuildId == guildId && r.UserId == userId && r.Day == day)
                     ?? await db.MemberDailyStats.FirstOrDefaultAsync(r => r.GuildId == guildId && r.UserId == userId && r.Day == day);
        if (bucket is null)
        {
            bucket = new MemberDailyStat { GuildId = guildId, UserId = userId, Day = day };
            db.MemberDailyStats.Add(bucket);
        }
        bucket.CaillouxSpent += amount;
    }
}
