using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum LotteryBuyOutcome { Bought, TooPoor }

/// <param name="Pot">What the draw would share out now, her free ticket included.</param>
/// <param name="Tickets">The players' tickets, hers not counted.</param>
public sealed record LotteryStatus(int DrawDay, DateTimeOffset DrawAt, long Pot, int Tickets, int Mine, int Players);

public sealed record LotteryBuy(LotteryBuyOutcome Outcome, int Tickets, long Cost, long Balance, LotteryStatus Status);

/// <param name="Winner">Null when her own ticket won: nothing paid, the whole pot burned.</param>
public sealed record LotteryResult(int DrawDay, ulong? Winner, long Pot, long Paid, int Tickets);

// The weekly lottery (/arcade lottery) — transient, like every service wrapping AppDbContext.
//
// Not through ArcadeService.SettleAsync: that path holds nothing in escrow and logs arcade keys, and
// a ticket must stay bought until Sunday. Tickets are debited at purchase (spend.lottery), the
// winner paid at the draw (earn.lottery). Lottery money never counts toward « Mécène ».
//
// A buy and a draw each run in one transaction, and SQLite opens it IMMEDIATE (the write lock taken
// at BEGIN, before the first read), so the two never interleave. Without it a ticket bought at
// 20:59:59 could save between the draw's read of the tickets and its save: paid for, never drawn.
// Serialized, a buy either lands before the draw (and is drawn) or sees the week's draw row and goes
// to the next week (OpenDayAsync).
public class LotteryService
{
    private readonly AppDbContext _db;

    public LotteryService(AppDbContext db)
    {
        _db = db;
    }

    public Task<LotteryBuy> BuyAsync(ulong guildId, ulong userId, int tickets, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db, () => BuyOnceAsync(guildId, userId, tickets, now));

    private async Task<LotteryBuy> BuyOnceAsync(ulong guildId, ulong userId, int tickets, DateTimeOffset now)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        var cost = tickets * Lottery.TicketPrice;
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db, guildId, userId);
        if (wallet.Balance < cost)
            return new LotteryBuy(LotteryBuyOutcome.TooPoor, tickets, cost, wallet.Balance, await StatusAsync(guildId, userId, now));

        var day = await OpenDayAsync(guildId, now);
        var entry = await _db.LotteryEntries.FirstOrDefaultAsync(e => e.GuildId == guildId && e.DrawDay == day && e.UserId == userId);
        if (entry is null)
        {
            entry = new LotteryEntry { GuildId = guildId, DrawDay = day, UserId = userId };
            _db.LotteryEntries.Add(entry);
        }
        entry.Tickets += tickets;
        wallet.Balance -= cost;
        await EconomyLog.AddAsync(_db, guildId, EconomyLog.SpendLottery, cost, now);
        await _db.SaveChangesAsync();
        var status = await StatusAsync(guildId, userId, now);
        await tx.CommitAsync();
        return new LotteryBuy(LotteryBuyOutcome.Bought, tickets, cost, wallet.Balance, status);
    }

    // The week a ticket bought now goes to: the next draw, or the one after when that draw already
    // ran (a buy that started before 21:00 and reached the database after the draw).
    private async Task<int> OpenDayAsync(ulong guildId, DateTimeOffset now)
    {
        var day = Lottery.DrawDay(now);
        while (await _db.LotteryDraws.AnyAsync(d => d.GuildId == guildId && d.DrawDay == day))
            day = Lottery.DrawDay(Lottery.DrawAt(day));
        return day;
    }

    public async Task<LotteryStatus> StatusAsync(ulong guildId, ulong userId, DateTimeOffset now)
    {
        var day = await OpenDayAsync(guildId, now);
        var entries = await _db.LotteryEntries.AsNoTracking()
            .Where(e => e.GuildId == guildId && e.DrawDay == day)
            .Select(e => new { e.UserId, e.Tickets })
            .ToListAsync();
        var total = entries.Sum(e => e.Tickets);
        var mine = entries.Where(e => e.UserId == userId).Sum(e => e.Tickets);
        return new LotteryStatus(day, Lottery.DrawAt(day), Lottery.Pot(total), total, mine, entries.Count(e => e.Tickets > 0));
    }

    /// <summary>
    /// Every (guild, draw day) with tickets, not drawn yet, whose draw time has passed. A week nobody
    /// bought into is never due, so it is never drawn or announced.
    /// </summary>
    public async Task<List<(ulong GuildId, int DrawDay)>> DueAsync(DateTimeOffset now)
    {
        var pending = await _db.LotteryEntries.AsNoTracking()
            .Where(e => e.Tickets > 0)
            .Where(e => !_db.LotteryDraws.Any(d => d.GuildId == e.GuildId && d.DrawDay == e.DrawDay))
            .Select(e => new { e.GuildId, e.DrawDay })
            .Distinct()
            .ToListAsync();
        return pending.Where(p => Lottery.DrawAt(p.DrawDay) <= now)
            .OrderBy(p => p.DrawDay)
            .Select(p => (p.GuildId, p.DrawDay))
            .ToList();
    }

    /// <summary>
    /// Draws one week: the winner (or her ticket), the payout and the draw row, in one save. Null when
    /// it was already drawn — the unique row is what makes a second pass, or a race, a no-op.
    /// </summary>
    public Task<LotteryResult?> DrawAsync(ulong guildId, int drawDay, Random rng, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db, () => DrawOnceAsync(guildId, drawDay, rng, now));

    private async Task<LotteryResult?> DrawOnceAsync(ulong guildId, int drawDay, Random rng, DateTimeOffset now)
    {
        // The write lock first: no ticket can be added between reading the tickets and saving the draw.
        await using var tx = await _db.Database.BeginTransactionAsync();
        if (await _db.LotteryDraws.AnyAsync(d => d.GuildId == guildId && d.DrawDay == drawDay)) return null;

        var entries = (await _db.LotteryEntries.AsNoTracking()
                .Where(e => e.GuildId == guildId && e.DrawDay == drawDay && e.Tickets > 0)
                .ToListAsync())
            .OrderBy(e => e.UserId).ToList();
        var total = entries.Sum(e => e.Tickets);
        // Nobody played: no draw, nothing posted. Her free ticket alone would win every empty week.
        if (total == 0) return null;
        var pot = Lottery.Pot(total);
        var index = Lottery.Pick(entries.Select(e => e.Tickets).ToList(), rng.Next(total + 1));
        ulong? winner = index >= 0 ? entries[index].UserId : null;
        long paid = 0;
        if (winner is { } id)
        {
            paid = Lottery.Payout(pot);
            (await PebbleService.GetOrCreateWalletAsync(_db, guildId, id)).Balance += paid;
            await EconomyLog.AddAsync(_db, guildId, EconomyLog.EarnLottery, paid, now);
        }

        _db.LotteryDraws.Add(new LotteryDraw
        {
            GuildId = guildId, DrawDay = drawDay, WinnerId = winner ?? 0,
            Pot = pot, Paid = paid, Tickets = total, DrawnAt = now,
        });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return new LotteryResult(drawDay, winner, pot, paid, total);
    }
}
