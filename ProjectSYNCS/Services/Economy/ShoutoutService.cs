using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum BidOutcome { Leading, TooLow, TooPoor }

// Amount is what was bid; Minimum the least the next bid must be (after a lead, the new leader's plus
// a raise); Balance the bidder's after. Outbid is who got their bid back, when someone did; Cost what the bid
// takes from the bidder (the difference, when raising their own lead).
public sealed record BidResult(BidOutcome Outcome, int ForDay, long Amount, long Minimum, long Balance, ulong? Outbid, long Cost);

// The auction open now (ForDay), its leader if any, and how many won dedications wait for a hello.
public sealed record ShoutoutStatus(int ForDay, DateTimeOffset ClosesAt, ulong? Leader, long Amount, long Minimum, int Queued);

/// <summary>
/// /shoutout: the auction for a dedication in her morning hello (Helpers/Economy/Shoutouts), and the
/// two calls MorningGreetingService makes to post the winner. A bid is held, not spent: debited at
/// once, refunded at once when outbid, and logged <c>spend.shoutout</c> only
/// when the dedication is posted (<see cref="MarkShownAsync"/>).
/// </summary>
public class ShoutoutService
{
    private readonly AppDbContext _db_context;

    public ShoutoutService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    /// <summary>
    /// A bid for the hello <see cref="Shoutouts.OpenDay"/> names. Raising your own lead charges the
    /// difference; taking someone else's refunds them in the same save. One unit of work: the row's
    /// <c>Amount</c> and both wallets' balances are tokens, so a bid racing another re-checks against it.
    /// </summary>
    public Task<BidResult> BidAsync(ulong guildId, ulong userId, long amount, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => BidOnceAsync(guildId, userId, amount, now));

    private async Task<BidResult> BidOnceAsync(ulong guildId, ulong userId, long amount, DateTimeOffset now)
    {
        var day = Shoutouts.OpenDay(now);
        var row = await _db_context.ShoutoutBids.FirstOrDefaultAsync(b => b.GuildId == guildId && b.ForDay == day);
        var minimum = Shoutouts.Minimum(row?.Amount);
        var bidder = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (amount < minimum) return new BidResult(BidOutcome.TooLow, day, amount, minimum, bidder.Balance, null, 0);

        var raisingOwn = row is not null && row.UserId == userId;
        var cost = raisingOwn ? amount - row!.Amount : amount;
        if (bidder.Balance < cost) return new BidResult(BidOutcome.TooPoor, day, amount, minimum, bidder.Balance, null, cost);

        ulong? outbid = null;
        if (row is null)
        {
            row = new ShoutoutBid { GuildId = guildId, ForDay = day };
            _db_context.ShoutoutBids.Add(row);
        }
        else if (!raisingOwn)
        {
            var previous = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, row.UserId);
            previous.Balance += row.Amount;
            outbid = row.UserId;
        }

        bidder.Balance -= cost;
        row.UserId = userId;
        row.Amount = amount;
        row.PlacedAt = now;
        await _db_context.SaveChangesAsync();
        return new BidResult(BidOutcome.Leading, day, amount, Shoutouts.Minimum(amount), bidder.Balance, outbid, cost);
    }

    public async Task<ShoutoutStatus> StatusAsync(ulong guildId, DateTimeOffset now)
    {
        var day = Shoutouts.OpenDay(now);
        var today = AppTime.DayKey(now);
        var row = await _db_context.ShoutoutBids.AsNoTracking().FirstOrDefaultAsync(b => b.GuildId == guildId && b.ForDay == day);
        var queued = await _db_context.ShoutoutBids.CountAsync(b => b.GuildId == guildId && b.ShownDay == 0 && b.ForDay <= today);
        return new ShoutoutStatus(day, Shoutouts.ClosesAt(day), row?.UserId, row?.Amount ?? 0, Shoutouts.Minimum(row?.Amount), queued);
    }

    /// <summary>
    /// The dedication the next hello should carry: won (its day has come), not yet posted. Today's
    /// winner first, so the day every lead reply promised holds; the ones carried over her birthday,
    /// a silenced day or downtime then go out, oldest first, on the mornings nobody won. Oldest-first
    /// for everyone would push every later winner a day late for as long as each day sells.
    /// </summary>
    public Task<ShoutoutBid?> NextDueAsync(ulong guildId, DateTimeOffset now)
    {
        var today = AppTime.DayKey(now);
        return _db_context.ShoutoutBids.AsNoTracking()
            .Where(b => b.GuildId == guildId && b.ShownDay == 0 && b.ForDay <= today)
            .OrderByDescending(b => b.ForDay == today).ThenBy(b => b.ForDay).ThenBy(b => b.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// The dedication was posted: stamp it and log the held cailloux as spent,
    /// in one save. A bid already posted is left alone, so a second call settles nothing twice.
    /// </summary>
    public Task MarkShownAsync(int bidId, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, async () =>
        {
            var row = await _db_context.ShoutoutBids.FirstOrDefaultAsync(b => b.Id == bidId);
            if (row is null || row.ShownDay != 0) return false;
            row.ShownDay = AppTime.DayKey(now);
            await EconomyLog.AddAsync(_db_context, row.GuildId, EconomyLog.SpendShoutout, row.Amount, now);
            await _db_context.SaveChangesAsync();
            return true;
        });
}
