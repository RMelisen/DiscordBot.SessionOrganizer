using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// WorkReminder: whether they get a DM when the next shift is ready (the footer hints at it when off).
public sealed record WorkResult(bool Paid, long Amount, long Balance, DateTimeOffset NextWorkAt, bool WorkReminder);

public sealed record PebbleBalance(long Balance, DateTimeOffset? NextWorkAt, long PassiveToday, bool WorkReminder);

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
        if (PebbleEconomy.NextWorkAt(wallet.LastWorkAt) is { } next && next > now)
            return new WorkResult(false, 0, wallet.Balance, next, wallet.WorkReminder);

        wallet.Balance += pay;
        wallet.LastWorkAt = now;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.EarnWork, pay, now);
        await _db_context.SaveChangesAsync();
        return new WorkResult(true, pay, wallet.Balance, now + PebbleEconomy.WorkCooldown, wallet.WorkReminder);
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
