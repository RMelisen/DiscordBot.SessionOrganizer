using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public sealed record WorkResult(bool Paid, long Amount, long Balance, DateTimeOffset NextWorkAt);

public sealed record PebbleBalance(long Balance, DateTimeOffset? NextWorkAt, long PassiveToday);

// EF access for cailloux — transient, like every service wrapping AppDbContext.
public class PebbleService
{
    private readonly AppDbContext _db_context;

    public PebbleService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // The pay is a parameter rather than rolled here, so the amount is testable.
    public async Task<WorkResult> WorkAsync(ulong guildId, ulong userId, long pay, DateTimeOffset now)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (PebbleEconomy.NextWorkAt(wallet.LastWorkAt) is { } next && next > now)
            return new WorkResult(false, 0, wallet.Balance, next);

        wallet.Balance += pay;
        wallet.LastWorkAt = now;
        await _db_context.SaveChangesAsync();
        return new WorkResult(true, pay, wallet.Balance, now + PebbleEconomy.WorkCooldown);
    }

    // Returns how much was actually granted — zero once today's cap is reached.
    public async Task<long> AddPassiveAsync(ulong guildId, ulong userId, long amount)
    {
        var wallet = await GetOrCreateWalletAsync(_db_context, guildId, userId);
        var today = AppTime.TodayKey;
        var (granted, total) = PebbleEconomy.Passive(wallet.PassiveDay == today ? wallet.PassiveToday : 0, amount);
        if (granted <= 0) return 0;

        wallet.Balance += granted;
        wallet.PassiveDay = today;
        wallet.PassiveToday = total;
        await _db_context.SaveChangesAsync();
        return granted;
    }

    public async Task<PebbleBalance> GetBalanceAsync(ulong guildId, ulong userId)
    {
        var wallet = await _db_context.PebbleWallets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (wallet is null) return new PebbleBalance(0, null, 0);

        return new PebbleBalance(
            wallet.Balance,
            PebbleEconomy.NextWorkAt(wallet.LastWorkAt),
            wallet.PassiveDay == AppTime.TodayKey ? wallet.PassiveToday : 0);
    }

    // Static and context-taking so PlynlingService can load the wallet in *its own*
    // context — paying and feeding must save together, and AppDbContext is transient.
    public static async Task<PebbleWallet> GetOrCreateWalletAsync(AppDbContext db, ulong guildId, ulong userId)
    {
        var wallet = await db.PebbleWallets.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (wallet is null)
        {
            wallet = new PebbleWallet { GuildId = guildId, UserId = userId };
            db.PebbleWallets.Add(wallet);
        }
        return wallet;
    }
}
