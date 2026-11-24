using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One person's standing in one of the wall's two rankings.</summary>
public readonly record struct ShameTally(ulong UserId, long Count);

/// <summary>What came of a vote against someone.</summary>
public enum ShameVoteResult
{
    /// <summary>Counted. The vote is on the record and the announcement should go out.</summary>
    Recorded,
    /// <summary>This target has already taken today's allowance. Nothing was written.</summary>
    TargetLimitReached,
}

/// <summary>Every title on the wall over one window, already ranked and trimmed.</summary>
public readonly record struct ShameWall(
    IReadOnlyList<ShameTally> Malfaisants,
    IReadOnlyList<ShameTally> Bannis,
    IReadOnlyList<ShameTally> Perfides,
    IReadOnlyList<ShameTally> Hysteriques,
    IReadOnlyList<ShameTally> Indignes);

// Persists the wall of shame. Deliberately dumb in the same way BotFeedbackService is:
// it records what it is told and reads it back. Deciding *whether* a message was mean
// and how many people it was mean to belongs to ShameTracker, which is the piece that
// can see the message.
//
// Transient, because it wraps AppDbContext.
public class ShameService
{
    /// <summary>How many names each title shows on the wall.</summary>
    public const int TopCount = 3;

    private readonly AppDbContext _db_context;

    public ShameService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    /// <summary>
    /// Records that <paramref name="userId"/> was hostile to <paramref name="hits"/>
    /// people in one message. Written to the all-time total and today's bucket in one
    /// call, so every bucketed person necessarily has a totals row.
    /// </summary>
    public async Task AddMeanHitsAsync(ulong guildId, ulong userId, long hits)
    {
        if (hits <= 0) return;

        await AddHitsAsync(guildId, userId, ShameCategory.Mean, hits);
    }

    /// <summary>
    /// Records one interaction with a rival bot. Always one hit: unlike hostility,
    /// which scales with how many people it was aimed at, turning to another bot is
    /// one act however many bots were in the message.
    /// </summary>
    public async Task AddPerfidyHitAsync(ulong guildId, ulong userId)
    {
        await AddHitsAsync(guildId, userId, ShameCategory.Perfidy, 1);
    }

    /// <summary>
    /// Records one shouted message. Always one hit: unlike hostility, which scales with
    /// how many people it was aimed at, shouting is aimed at the room.
    /// </summary>
    public async Task AddShoutHitAsync(ulong guildId, ulong userId)
    {
        await AddHitsAsync(guildId, userId, ShameCategory.Shout, 1);
    }

    /// <summary>Records one abandoned Plynling. Always one hit, never rationed.</summary>
    public async Task AddAbandonHitAsync(ulong guildId, ulong userId)
    {
        await AddHitsAsync(guildId, userId, ShameCategory.Abandon, 1);
    }

    // The record and today's bucket, created if missing, then incremented in SQL: a count read into
    // memory and written back would overwrite a /bribe shame erasing a hit in between (ForgiveOneAsync),
    // and the member would have paid for nothing. Both increments in one transaction.
    private async Task AddHitsAsync(ulong guildId, ulong userId, ShameCategory category, long hits)
    {
        var today = AppTime.TodayKey;
        await using var tx = await _db_context.Database.BeginTransactionAsync();
        await GetOrCreateRecordAsync(guildId, userId);
        await GetOrCreateDailyAsync(guildId, userId, today);
        await _db_context.SaveChangesAsync();

        var record = _db_context.ShameRecords.Where(r => r.GuildId == guildId && r.UserId == userId);
        var bucket = _db_context.ShameDailyStats.Where(b => b.GuildId == guildId && b.UserId == userId && b.Day == today);
        switch (category)
        {
            case ShameCategory.Mean:
                await record.ExecuteUpdateAsync(s => s.SetProperty(r => r.MeanHits, r => r.MeanHits + hits));
                await bucket.ExecuteUpdateAsync(s => s.SetProperty(b => b.MeanHits, b => b.MeanHits + hits));
                break;
            case ShameCategory.Ban:
                await record.ExecuteUpdateAsync(s => s.SetProperty(r => r.BanVotes, r => r.BanVotes + hits));
                await bucket.ExecuteUpdateAsync(s => s.SetProperty(b => b.BanVotes, b => b.BanVotes + hits));
                break;
            case ShameCategory.Perfidy:
                await record.ExecuteUpdateAsync(s => s.SetProperty(r => r.PerfidyHits, r => r.PerfidyHits + hits));
                await bucket.ExecuteUpdateAsync(s => s.SetProperty(b => b.PerfidyHits, b => b.PerfidyHits + hits));
                break;
            case ShameCategory.Shout:
                await record.ExecuteUpdateAsync(s => s.SetProperty(r => r.ShoutHits, r => r.ShoutHits + hits));
                await bucket.ExecuteUpdateAsync(s => s.SetProperty(b => b.ShoutHits, b => b.ShoutHits + hits));
                break;
            case ShameCategory.Abandon:
                await record.ExecuteUpdateAsync(s => s.SetProperty(r => r.AbandonHits, r => r.AbandonHits + hits));
                await bucket.ExecuteUpdateAsync(s => s.SetProperty(b => b.AbandonHits, b => b.AbandonHits + hits));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category));
        }
        await tx.CommitAsync();
    }

    /// <summary>
    /// Erases one hit in <paramref name="category"/> (/bribe shame): from the record, and from the most
    /// recent day holding one, so the wall's windows move too (today is usually empty). Hits from
    /// before the buckets existed come off the record alone. False, and nothing touched, when there
    /// is no hit to erase.
    /// </summary>
    /// <remarks>
    /// Runs its SQL at once (ExecuteUpdate): call it inside the caller's transaction, which also holds
    /// the payment, and never saves. A hit added meanwhile cannot undo it, because the adders
    /// (<see cref="AddHitsAsync"/>) increment in SQL too rather than writing back a count read earlier.
    /// </remarks>
    public static async Task<bool> ForgiveOneAsync(AppDbContext db, ulong guildId, ulong userId, ShameCategory category)
    {
        var records = db.ShameRecords.Where(r => r.GuildId == guildId && r.UserId == userId);
        var buckets = db.ShameDailyStats.Where(s => s.GuildId == guildId && s.UserId == userId);
        int erased;
        switch (category)
        {
            case ShameCategory.Mean:
                erased = await records.Where(r => r.MeanHits > 0).ExecuteUpdateAsync(s => s.SetProperty(r => r.MeanHits, r => r.MeanHits - 1));
                if (erased > 0 && await buckets.Where(b => b.MeanHits > 0).OrderByDescending(b => b.Day).Select(b => (int?)b.Id).FirstOrDefaultAsync() is { } mean)
                    await buckets.Where(b => b.Id == mean).ExecuteUpdateAsync(s => s.SetProperty(b => b.MeanHits, b => b.MeanHits - 1));
                break;
            case ShameCategory.Ban:
                erased = await records.Where(r => r.BanVotes > 0).ExecuteUpdateAsync(s => s.SetProperty(r => r.BanVotes, r => r.BanVotes - 1));
                if (erased > 0 && await buckets.Where(b => b.BanVotes > 0).OrderByDescending(b => b.Day).Select(b => (int?)b.Id).FirstOrDefaultAsync() is { } ban)
                    await buckets.Where(b => b.Id == ban).ExecuteUpdateAsync(s => s.SetProperty(b => b.BanVotes, b => b.BanVotes - 1));
                break;
            case ShameCategory.Perfidy:
                erased = await records.Where(r => r.PerfidyHits > 0).ExecuteUpdateAsync(s => s.SetProperty(r => r.PerfidyHits, r => r.PerfidyHits - 1));
                if (erased > 0 && await buckets.Where(b => b.PerfidyHits > 0).OrderByDescending(b => b.Day).Select(b => (int?)b.Id).FirstOrDefaultAsync() is { } perfidy)
                    await buckets.Where(b => b.Id == perfidy).ExecuteUpdateAsync(s => s.SetProperty(b => b.PerfidyHits, b => b.PerfidyHits - 1));
                break;
            case ShameCategory.Shout:
                erased = await records.Where(r => r.ShoutHits > 0).ExecuteUpdateAsync(s => s.SetProperty(r => r.ShoutHits, r => r.ShoutHits - 1));
                if (erased > 0 && await buckets.Where(b => b.ShoutHits > 0).OrderByDescending(b => b.Day).Select(b => (int?)b.Id).FirstOrDefaultAsync() is { } shout)
                    await buckets.Where(b => b.Id == shout).ExecuteUpdateAsync(s => s.SetProperty(b => b.ShoutHits, b => b.ShoutHits - 1));
                break;
            case ShameCategory.Abandon:
                erased = await records.Where(r => r.AbandonHits > 0).ExecuteUpdateAsync(s => s.SetProperty(r => r.AbandonHits, r => r.AbandonHits - 1));
                if (erased > 0 && await buckets.Where(b => b.AbandonHits > 0).OrderByDescending(b => b.Day).Select(b => (int?)b.Id).FirstOrDefaultAsync() is { } abandon)
                    await buckets.Where(b => b.Id == abandon).ExecuteUpdateAsync(s => s.SetProperty(b => b.AbandonHits, b => b.AbandonHits - 1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category));
        }
        return erased > 0;
    }

    /// <summary>How many votes one person can take in a single day, from everyone.</summary>
    /// <remarks>
    /// The cap is on the <em>target</em>, not the voter: the point is that nobody gets
    /// piled on, not that any one voter is rationed. With the command restricted to
    /// staff, a voter hitting several different people in a day is moderation doing its
    /// job; the same person taking eight votes in an evening is a dogpile.
    /// </remarks>
    public const long MaxVotesPerTargetPerDay = 2;

    /// <summary>
    /// Records a vote against <paramref name="targetId"/>, unless they have already
    /// taken <see cref="MaxVotesPerTargetPerDay"/> today.
    /// </summary>
    /// <remarks>
    /// The limit is read straight off today's bucket, which already counts exactly
    /// "votes this person took on this day" — so the rule needs no state of its own, and
    /// it cannot drift from the number the wall displays. Refusing writes nothing. The vote
    /// goes through <see cref="AddHitsAsync"/> like every hit, incremented in SQL, so a
    /// <c>/bribe shame</c> buying a ban vote back in between is not overwritten; a vote bought
    /// back from today's bucket frees that slot of today's cap.
    /// <para>Who may vote at all is the module's business, not this method's: it is the
    /// piece holding the <c>IUser</c> and its guild permissions. The voter's identity
    /// does not reach here at all any more — with the cap on the target, nothing about
    /// the vote depends on who cast it, and taking an id this method cannot use would
    /// suggest otherwise.</para>
    /// </remarks>
    public async Task<ShameVoteResult> TryVoteAsync(ulong guildId, ulong targetId)
    {
        var today = AppTime.TodayKey;

        var votesToday = await _db_context.ShameDailyStats
            .Where(b => b.GuildId == guildId && b.UserId == targetId && b.Day == today)
            .Select(b => b.BanVotes)
            .FirstOrDefaultAsync();
        if (votesToday >= MaxVotesPerTargetPerDay) return ShameVoteResult.TargetLimitReached;

        await AddHitsAsync(guildId, targetId, ShameCategory.Ban, 1);
        return ShameVoteResult.Recorded;
    }

    /// <summary>
    /// The top few of each title over <paramref name="period"/>. Both rankings come
    /// from one read of whichever table the window needs, since they live in the same
    /// row and the wall always shows both.
    /// </summary>
    public async Task<ShameWall> GetWallAsync(ulong guildId, StatsPeriod period)
    {
        if (period == StatsPeriod.AllTime)
        {
            var all = await _db_context.ShameRecords
                .Where(r => r.GuildId == guildId)
                .ToListAsync();

            return new ShameWall(
                Rank(all.Select(r => (r.Id, r.UserId, r.MeanHits))),
                Rank(all.Select(r => (r.Id, r.UserId, r.BanVotes))),
                Rank(all.Select(r => (r.Id, r.UserId, r.PerfidyHits))),
                Rank(all.Select(r => (r.Id, r.UserId, r.ShoutHits))),
                Rank(all.Select(r => (r.Id, r.UserId, r.AbandonHits))));
        }

        // Inclusive lower bound: 6 days ago plus today is a week. Day is an int, so
        // unlike every DateTimeOffset window in this project this really is filtered by
        // the database rather than in memory.
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);

        var buckets = await _db_context.ShameDailyStats
            .Where(b => b.GuildId == guildId && b.Day >= since)
            .ToListAsync();

        var byUser = buckets.GroupBy(b => b.UserId).ToList();

        return new ShameWall(
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.MeanHits)))),
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.BanVotes)))),
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.PerfidyHits)))),
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.ShoutHits)))),
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.AbandonHits)))));
    }

    // Ranked highest first, ties broken by the row that appeared first — the earliest
    // Id in the all-time table, or the earliest bucket inside the window. Any stable
    // tie-break would do; what matters is that it *is* stable, or two people level on
    // count would swap places on every re-render and the wall would look broken.
    private static List<ShameTally> Rank(IEnumerable<(int Id, ulong UserId, long Count)> rows) =>
        rows
            .Where(r => r.Count > 0)
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Id)
            .Take(TopCount)
            .Select(r => new ShameTally(r.UserId, r.Count))
            .ToList();

    private async Task<ShameRecord> GetOrCreateRecordAsync(ulong guildId, ulong userId)
    {
        var row = await _db_context.ShameRecords
            .FirstOrDefaultAsync(r => r.GuildId == guildId && r.UserId == userId);

        if (row is null)
        {
            row = new ShameRecord { GuildId = guildId, UserId = userId };
            _db_context.ShameRecords.Add(row);
        }
        return row;
    }

    private async Task<ShameDailyStat> GetOrCreateDailyAsync(ulong guildId, ulong userId, int day)
    {
        var row = await _db_context.ShameDailyStats.FirstOrDefaultAsync(b =>
            b.GuildId == guildId && b.UserId == userId && b.Day == day);

        if (row is null)
        {
            row = new ShameDailyStat { GuildId = guildId, UserId = userId, Day = day };
            _db_context.ShameDailyStats.Add(row);
        }
        return row;
    }
}
