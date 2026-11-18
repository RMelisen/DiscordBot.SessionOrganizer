using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum JarGiveOutcome { Given, TooPoor }

/// <param name="Reached">How many goals this gift reached; each left a reward pending.</param>
public sealed record JarGiveResult(JarGiveOutcome Outcome, long Amount, long Balance, int Reached, JarStatus Jar);

public sealed record JarStatus(int Level, long Filled, long Goal, JarReward Next,
    IReadOnlyList<(ulong UserId, long Total)> Top, long Mine,
    int DoubleDailyFrom, int DoubleDailyUntil);

/// <param name="Level">Which goal (0-based) this reward was for.</param>
/// <param name="Day">The bonus day a timed reward added (Paris yyyymmdd), 0 otherwise.</param>
/// <param name="Fed">How many Plynlings ate at a feast.</param>
/// <param name="Giveaway">The giveaway created, its card still to post.</param>
public sealed record JarUnlock(JarReward Reward, int Level, int Day, int Fed, Giveaway? Giveaway);

// The guild's jar (/jar) — transient, like every service wrapping AppDbContext. A gift is one unit
// of work through EconomySave: two gifts racing collide on the jar's Filled token (or the wallet's
// Balance) and the slower one re-applies on top. The rewards a gift unlocks are carried out
// afterwards, one per save (ResolveNextRewardAsync).
public class JarService
{
    public const int TopCount = 5;

    private readonly AppDbContext _db;

    public JarService(AppDbContext db)
    {
        _db = db;
    }

    public Task<JarGiveResult> GiveAsync(ulong guildId, ulong userId, long amount, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db, () => GiveOnceAsync(guildId, userId, amount, now));

    private async Task<JarGiveResult> GiveOnceAsync(ulong guildId, ulong userId, long amount, DateTimeOffset now)
    {
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db, guildId, userId);
        if (wallet.Balance < amount)
            return new JarGiveResult(JarGiveOutcome.TooPoor, amount, wallet.Balance, 0, await StatusAsync(guildId, userId, now));

        var jar = await GetOrCreateJarAsync(guildId);
        var before = jar.Level;
        (jar.Level, jar.Filled) = Jar.Fill(jar.Level, jar.Filled, amount);
        jar.Given += amount;
        wallet.Balance -= amount;

        var gift = _db.JarGifts.Local.FirstOrDefault(g => g.GuildId == guildId && g.UserId == userId)
                   ?? await _db.JarGifts.FirstOrDefaultAsync(g => g.GuildId == guildId && g.UserId == userId);
        if (gift is null)
        {
            gift = new JarGift { GuildId = guildId, UserId = userId };
            _db.JarGifts.Add(gift);
        }
        gift.Total += amount;

        await EconomyLog.AddAsync(_db, guildId, EconomyLog.SpendJar, amount, now);
        await Patronage.AddAsync(_db, guildId, userId, amount, now);
        await _db.SaveChangesAsync();
        return new JarGiveResult(JarGiveOutcome.Given, amount, wallet.Balance, jar.Level - before, await StatusAsync(guildId, userId, now));
    }

    public async Task<JarStatus> StatusAsync(ulong guildId, ulong userId, DateTimeOffset now)
    {
        var jar = await _db.CaillouxJars.AsNoTracking().FirstOrDefaultAsync(j => j.GuildId == guildId)
                  ?? new CaillouxJar { GuildId = guildId };
        var top = (await _db.JarGifts.AsNoTracking()
                .Where(g => g.GuildId == guildId && g.Total > 0)
                .OrderByDescending(g => g.Total).ThenBy(g => g.Id)
                .Take(TopCount)
                .Select(g => new { g.UserId, g.Total })
                .ToListAsync())
            .Select(g => (g.UserId, g.Total)).ToList();
        var mine = await _db.JarGifts.AsNoTracking()
            .Where(g => g.GuildId == guildId && g.UserId == userId)
            .Select(g => g.Total).FirstOrDefaultAsync();
        return new JarStatus(jar.Level, jar.Filled, Jar.GoalFor(jar.Level), Jar.RewardFor(jar.Level), top, mine,
            jar.DoubleDailyFrom, jar.DoubleDailyUntil);
    }

    /// <summary>Whether <c>/daily</c> pays double on <paramref name="day"/>. Read inside the claim's own unit.</summary>
    public static async Task<bool> IsDoubleDailyAsync(AppDbContext db, ulong guildId, int day)
    {
        var range = await db.CaillouxJars.AsNoTracking()
            .Where(j => j.GuildId == guildId)
            .Select(j => new { j.DoubleDailyFrom, j.DoubleDailyUntil })
            .FirstOrDefaultAsync();
        return range is not null && Jar.Covers(range.DoubleDailyFrom, range.DoubleDailyUntil, day);
    }

    /// <summary>
    /// Carries out the oldest pending reward (a goal reached whose reward hasn't run yet), in one
    /// save with the count that marks it done. Null when nothing is pending.
    /// </summary>
    /// <remarks>
    /// Called in a loop after each gift. Two resolvers racing collide on <c>RewardsDone</c>: the slower
    /// retries, re-reads, and takes the next reward or none, so each runs once. A failure leaves it
    /// pending for the next gift. The giveaway's card is posted by the caller; the row is created here,
    /// in <paramref name="channelId"/>, organised by the bot.
    /// </remarks>
    public Task<JarUnlock?> ResolveNextRewardAsync(ulong guildId, ulong botId, ulong channelId, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db, () => ResolveOnceAsync(guildId, botId, channelId, now));

    private async Task<JarUnlock?> ResolveOnceAsync(ulong guildId, ulong botId, ulong channelId, DateTimeOffset now)
    {
        var jar = await _db.CaillouxJars.FirstOrDefaultAsync(j => j.GuildId == guildId);
        if (jar is null || jar.RewardsDone >= jar.Level) return null;

        var level = jar.RewardsDone;
        var reward = Jar.RewardFor(level);
        var tomorrow = Jar.NextDay(AppTime.DayKey(now));
        int day = 0, fed = 0;
        Giveaway? giveaway = null;
        switch (reward)
        {
            case JarReward.DoubleDaily:
                (jar.DoubleDailyFrom, jar.DoubleDailyUntil) = Jar.Extend(jar.DoubleDailyFrom, jar.DoubleDailyUntil, tomorrow);
                day = jar.DoubleDailyUntil;
                break;
            case JarReward.Feast:
                var food = PlynlingCatalog.Info(Jar.FeastFood);
                var living = await _db.Plynlings
                    .Where(p => p.GuildId == guildId && p.DiedAt == null && p.FrozenAt == null)
                    .ToListAsync();
                foreach (var p in living) PlynlingLife.Feed(p, food, now);
                fed = living.Count;
                break;
            case JarReward.Giveaway:
                giveaway = new Giveaway
                {
                    GuildId = guildId,
                    ChannelId = channelId,
                    OrganizerId = botId,
                    Prize = Jar.GiveawayPrizeText,
                    WinnerCount = Jar.GiveawayWinners,
                    PebblePrize = Jar.GiveawayPrize,
                    CreatedAt = now,
                    EndsAt = now + Jar.GiveawayFor,
                };
                _db.Giveaways.Add(giveaway);
                break;
        }

        jar.RewardsDone++;
        await _db.SaveChangesAsync();
        return new JarUnlock(reward, level, day, fed, giveaway);
    }

    private async Task<CaillouxJar> GetOrCreateJarAsync(ulong guildId)
    {
        var jar = _db.CaillouxJars.Local.FirstOrDefault(j => j.GuildId == guildId)
                  ?? await _db.CaillouxJars.FirstOrDefaultAsync(j => j.GuildId == guildId);
        if (jar is null)
        {
            jar = new CaillouxJar { GuildId = guildId };
            _db.CaillouxJars.Add(jar);
        }
        return jar;
    }
}
