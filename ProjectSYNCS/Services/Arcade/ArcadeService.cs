using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <param name="Payers">Who could cover the stake at settlement, in the order given.</param>
/// <param name="Dropped">Who couldn't, and was left out.</param>
/// <param name="Nets">What each payer's balance moved by.</param>
public sealed record ArcadeSettlement(
    IReadOnlyList<ulong> Payers,
    IReadOnlyList<ulong> Dropped,
    IReadOnlyDictionary<ulong, long> Nets);

// The one settlement path for every /arcade game — transient, like every service wrapping
// AppDbContext. Nothing is held in escrow while a game runs: balances are checked when someone
// joins, then re-read here, and every move lands in one SaveChanges. A restart mid-game therefore
// moves no cailloux.
public class ArcadeService
{
    private readonly AppDbContext _db_context;

    public ArcadeService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // The check made when someone joins; settlement checks again.
    public async Task<long> BalanceAsync(ulong guildId, ulong userId) =>
        await _db_context.PebbleWallets.AsNoTracking()
            .Where(w => w.GuildId == guildId && w.UserId == userId)
            .Select(w => w.Balance)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Re-reads every player's wallet, leaves out whoever can no longer cover
    /// <paramref name="stake"/>, asks <paramref name="decide"/> what each payer's balance moves by
    /// (never below −stake), applies it and logs it, in one save.
    /// </summary>
    /// <remarks>
    /// The log records the *net* flows (<c>earn.arcade</c> for the positives, <c>spend.arcade</c>
    /// for the negatives), so a duel's winner doesn't show as having spent and earned their own
    /// stake back.
    /// </remarks>
    public async Task<ArcadeSettlement> SettleAsync(ulong guildId, IReadOnlyList<ulong> players, long stake,
        Func<IReadOnlyList<ulong>, IReadOnlyDictionary<ulong, long>> decide)
    {
        var wallets = new Dictionary<ulong, Models.PebbleWallet>();
        var payers = new List<ulong>();
        var dropped = new List<ulong>();
        foreach (var id in players.Distinct())
        {
            var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, id);
            wallets[id] = wallet;
            (wallet.Balance >= stake ? payers : dropped).Add(id);
        }

        var nets = decide(payers);
        var applied = new Dictionary<ulong, long>();
        long earned = 0, spent = 0;
        foreach (var id in payers)
        {
            var net = Math.Max(-stake, nets.GetValueOrDefault(id));
            var wallet = wallets[id];
            var old = wallet.Balance;
            wallet.Balance = Math.Max(0, old + net);
            var moved = wallet.Balance - old;
            applied[id] = moved;
            if (moved > 0) earned += moved;
            else spent -= moved;
        }

        var now = DateTimeOffset.UtcNow;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.EarnArcade, earned, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendArcade, spent, now);
        await _db_context.SaveChangesAsync();
        return new ArcadeSettlement(payers, dropped, applied);
    }
}
