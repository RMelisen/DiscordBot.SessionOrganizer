using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// WorkReminder: whether they get a DM when the next shift is ready (the footer hints at it when off).
// Graced: paid a few minutes early, through the once-a-day grace.
public sealed record WorkResult(bool Paid, long Amount, long Balance, DateTimeOffset NextWorkAt, bool WorkReminder, bool Graced = false);

// Claimed false: today was already claimed, Streak is the stored one. NextClaimAt: the next Paris midnight.
// Doubled: a jar bonus day (JarService.IsDoubleDailyAsync), Amount already doubled.
public sealed record DailyResult(bool Claimed, int Streak, long Amount, ItemFind? Find, DateTimeOffset NextClaimAt, bool Doubled = false);

public sealed record PebbleBalance(long Balance, DateTimeOffset? NextWorkAt, long PassiveToday, bool WorkReminder);

// Self is also refused by /pay itself; the service refuses it too, so no other caller can burn a tax on
// nothing and count it toward « Mécène ».
public enum PayOutcome { Paid, TooSmall, TooPoor, OverCap, Self }

// Amount and Tax are what was asked, even when refused (TooPoor names Amount + Tax). Balance is the
// sender's after the pay; RoomLeft what they may still send today.
public sealed record PayResult(PayOutcome Outcome, long Amount, long Tax, long Balance, long RoomLeft);

// EF access for cailloux — transient, like every service wrapping AppDbContext.
public class PebbleService
{
    private readonly AppDbContext _db_context;

    public PebbleService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // The pay is a parameter rather than rolled here, so the amount is testable. Every write here
    // goes through EconomySave: two /work at once must pay once, not race the cooldown.
    public Task<WorkResult> WorkAsync(ulong guildId, ulong userId, long pay, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => WorkOnceAsync(guildId, userId, pay, now));

    private async Task<WorkResult> WorkOnceAsync(ulong guildId, ulong userId, long pay, DateTimeOffset now)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        var graced = false;
        if (PebbleEconomy.NextWorkAt(wallet.LastWorkAt) is { } next && next > now)
        {
            // The grace is stamped in the same save as the pay, so two early /work at once grace once.
            var today = AppTime.DayKey(now);
            if (!PebbleEconomy.WorkGraceApplies(next, now, wallet.WorkGraceDay, today))
                return new WorkResult(false, 0, wallet.Balance, next, wallet.WorkReminder);
            wallet.WorkGraceDay = today;
            graced = true;
        }

        wallet.Balance += pay;
        wallet.LastWorkAt = now;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.EarnWork, pay, now);
        await _db_context.SaveChangesAsync();
        return new WorkResult(true, pay, wallet.Balance, now + PebbleEconomy.WorkCooldown, wallet.WorkReminder, graced);
    }

    // /daily: once per Paris day, through EconomySave so two claims at once pay once (Balance is a
    // concurrency token: the loser retries, re-reads DailyDay and is refused). The cailloux, the
    // streak and any find land in one save; GrantAsync logs the find and pays a completed set into
    // this same wallet. Rolled inside the work, so a retry simply rolls again.
    public Task<DailyResult> ClaimDailyAsync(ulong guildId, ulong userId, DateTimeOffset now, Random rng) =>
        EconomySave.RetryAsync(_db_context, () => ClaimDailyOnceAsync(guildId, userId, now, rng));

    private async Task<DailyResult> ClaimDailyOnceAsync(ulong guildId, ulong userId, DateTimeOffset now, Random rng)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        var today = AppTime.DayKey(now);
        var nextAt = DailyStreak.NextClaimAt(now);
        var excused = await ExcusedDaysAsync(wallet.DailyDay, today);
        if (DailyStreak.NextStreak(wallet.DailyDay, wallet.DailyStreak, today, excused) is not { } streak)
            return new DailyResult(false, wallet.DailyStreak, 0, null, nextAt);

        var amount = DailyStreak.RollCailloux(streak, rng);
        var doubled = await JarService.IsDoubleDailyAsync(_db_context, guildId, today);
        if (doubled) amount *= 2;
        wallet.Balance += amount;
        wallet.DailyDay = today;
        wallet.DailyStreak = streak;
        wallet.DailyBest = Math.Max(wallet.DailyBest, streak);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.EarnDaily, amount, now);

        ItemFind? find = null;
        if (rng.NextDouble() < DailyStreak.ItemChance(streak))
            find = await InventoryService.GrantAsync(_db_context, guildId, userId, ItemCatalog.DrawForage(rng, now), now);

        await _db_context.SaveChangesAsync();
        return new DailyResult(true, streak, amount, find, nextAt, doubled);
    }

    // The days between the last claim and today that she was offline long enough to excuse. Only read
    // when there is a gap; every incident and clean stop is loaded and its dates compared in memory
    // (SQLite cannot translate DateTimeOffset comparisons) — a handful of rows a month.
    private async Task<IReadOnlySet<int>> ExcusedDaysAsync(int lastDay, int today)
    {
        if (lastDay == 0 || lastDay >= today) return new HashSet<int>();
        var from = AppTime.FromDayKey(lastDay).AddDays(1);
        var to = AppTime.FromDayKey(today);
        if (from >= to) return new HashSet<int>();

        var windowStart = AppTime.AtWallClock(from);
        var windowEnd = AppTime.AtWallClock(to);
        var incidents = await _db_context.UptimeEvents.AsNoTracking().Select(x => new { x.StartedAt, x.EndedAt }).ToListAsync();
        var stops = await _db_context.CleanStops.AsNoTracking().Select(x => new { x.StartedAt, x.EndedAt }).ToListAsync();
        return DailyStreak.ExcusedDays(incidents.Concat(stops)
            .Where(x => x.EndedAt > windowStart && x.StartedAt < windowEnd)
            .Select(x => (x.StartedAt, x.EndedAt)));
    }

    // Returns how much was actually granted — zero once today's cap is reached.
    public Task<long> AddPassiveAsync(ulong guildId, ulong userId, long amount) =>
        EconomySave.RetryAsync(_db_context, () => AddPassiveOnceAsync(guildId, userId, amount));

    private async Task<long> AddPassiveOnceAsync(ulong guildId, ulong userId, long amount)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        var today = AppTime.TodayKey;
        var (granted, total) = PebbleEconomy.Passive(wallet.PassiveDay == today ? wallet.PassiveToday : 0, amount);
        if (granted <= 0) return 0;

        wallet.Balance += granted;
        wallet.PassiveDay = today;
        wallet.PassiveToday = total;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.EarnPassive, granted, DateTimeOffset.UtcNow);
        await _db_context.SaveChangesAsync();
        return granted;
    }

    public async Task<PebbleBalance> GetBalanceAsync(ulong guildId, ulong userId)
    {
        var wallet = await _db_context.PebbleWallets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (wallet is null) return new PebbleBalance(0, null, 0, false);

        return new PebbleBalance(
            wallet.Balance,
            PebbleEconomy.NextWorkAt(wallet.LastWorkAt),
            wallet.PassiveDay == AppTime.TodayKey ? wallet.PassiveToday : 0,
            wallet.WorkReminder);
    }

    // /balance's reminder button. Through EconomySave because Balance is a concurrency token: a
    // /work landing at the same moment makes this save throw, and the retry re-reads.
    //
    // The stamp is taken here, after the read, not by the caller: a /work already running records
    // the `now` it started with, and a stamp from a clock read earlier still could land after it,
    // then hide that new shift from WorkReminderDue. Reading the clock as late as possible keeps
    // that window to the database's own delay.
    public Task SetWorkReminderAsync(ulong guildId, ulong userId, bool on) =>
        EconomySave.RetryAsync(_db_context, () => SetWorkReminderOnceAsync(guildId, userId, on));

    private async Task<bool> SetWorkReminderOnceAsync(ulong guildId, ulong userId, bool on)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (wallet.WorkReminder == on) return on;
        wallet.WorkReminder = on;
        if (on) wallet.WorkRemindedAt = PebbleEconomy.ReminderStampOnEnable(wallet.LastWorkAt, wallet.WorkRemindedAt, DateTimeOffset.UtcNow);
        await _db_context.SaveChangesAsync();
        return on;
    }

    // WorkReminderService's candidates: the flag filters in SQL, the dates are compared in memory
    // by the caller (SQLite cannot translate DateTimeOffset comparisons).
    public Task<List<PebbleWallet>> GetReminderWalletsAsync() =>
        _db_context.PebbleWallets.AsNoTracking().Where(x => x.WorkReminder).ToListAsync();

    // Claims one wallet's reminder before the DM goes out: re-checks it is still due, stamps it and
    // saves. False when it no longer is (switched off, worked again, already sent) — a crash after
    // the claim costs the DM, never a second one. The clock is read here, on each attempt, rather
    // than at the start of the loop's pass: a pass slowed by earlier DMs would otherwise stamp a
    // time minutes old, later than the start of a /work saving meanwhile, and that shift would
    // never get its DM (see SetWorkReminderAsync).
    public Task<bool> ClaimWorkReminderAsync(int walletId) =>
        EconomySave.RetryAsync(_db_context, () => ClaimWorkReminderOnceAsync(walletId));

    private async Task<bool> ClaimWorkReminderOnceAsync(int walletId)
    {
        var wallet = await _db_context.PebbleWallets.FirstOrDefaultAsync(x => x.Id == walletId);
        var now = DateTimeOffset.UtcNow;
        if (wallet is null || !wallet.WorkReminder || !PebbleEconomy.WorkReminderDue(wallet.LastWorkAt, wallet.WorkRemindedAt, now))
            return false;
        wallet.WorkRemindedAt = now;
        await _db_context.SaveChangesAsync();
        return true;
    }

    // /pay: from one wallet to another, the tax burned, in one save. Through EconomySave: Balance is a
    // concurrency token, so two pays racing on one wallet cannot both spend the same cailloux, and the
    // retry re-reads the cap. The caller refuses self and bots.
    public Task<PayResult> PayAsync(ulong guildId, ulong fromId, ulong toId, long amount, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => PayOnceAsync(guildId, fromId, toId, amount, now));

    private async Task<PayResult> PayOnceAsync(ulong guildId, ulong fromId, ulong toId, long amount, DateTimeOffset now)
    {
        var from = await GetOrCreateWalletAsync(_db_context, guildId, fromId);
        var today = AppTime.DayKey(now);
        var tax = PebbleEconomy.PayTax(amount);
        var room = PebbleEconomy.PayRoom(from.PayDay, from.PaidToday, today);
        PayOutcome? refusal =
            fromId == toId ? PayOutcome.Self
            : amount < PebbleEconomy.PayMin ? PayOutcome.TooSmall
            : amount > room ? PayOutcome.OverCap
            : from.Balance < amount + tax ? PayOutcome.TooPoor
            : null;
        if (refusal is { } r) return new PayResult(r, amount, tax, from.Balance, room);

        var to = await GetOrCreateWalletAsync(_db_context, guildId, toId);
        from.Balance -= amount + tax;
        to.Balance += amount;
        from.PaidToday = (from.PayDay == today ? from.PaidToday : 0) + amount;
        from.PayDay = today;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendTax, tax, now);
        await Patronage.AddAsync(_db_context, guildId, fromId, tax, now);
        await _db_context.SaveChangesAsync();
        return new PayResult(PayOutcome.Paid, amount, tax, from.Balance, room - amount);
    }

    // /admin pebble: a staff correction, clamped at 0 so « remove » never leaves a debt. One save.
    public Task<(long Old, long New)> AdjustAsync(ulong guildId, ulong userId, long delta) =>
        EconomySave.RetryAsync(_db_context, () => AdjustOnceAsync(guildId, userId, delta));

    private async Task<(long Old, long New)> AdjustOnceAsync(ulong guildId, ulong userId, long delta)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        var old = wallet.Balance;
        wallet.Balance = Math.Max(0, old + delta);
        var moved = wallet.Balance - old;
        await EconomyLog.AddAsync(_db_context, guildId, moved >= 0 ? EconomyLog.EarnAdmin : EconomyLog.SpendAdmin, Math.Abs(moved), DateTimeOffset.UtcNow);
        await _db_context.SaveChangesAsync();
        return (old, wallet.Balance);
    }

    // Static and context-taking so PlynlingService can load the wallet in *its own*
    // context — paying and feeding must save together, and AppDbContext is transient.
    public static async Task<PebbleWallet> GetOrCreateWalletAsync(AppDbContext db, ulong guildId, ulong userId)
    {
        // Local first: a wallet created earlier in this same unit of work is not in the
        // database yet, and a second one for the same person would break the unique index
        // (a first-time feeder whose meal also earns their Plynling a badge).
        var wallet = db.PebbleWallets.Local.FirstOrDefault(x => x.GuildId == guildId && x.UserId == userId)
                     ?? await db.PebbleWallets.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (wallet is null)
        {
            wallet = new PebbleWallet { GuildId = guildId, UserId = userId };
            db.PebbleWallets.Add(wallet);
        }
        return wallet;
    }
}
