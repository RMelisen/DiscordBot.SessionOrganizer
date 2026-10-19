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
    public Task<ArcadeSettlement> SettleAsync(ulong guildId, IReadOnlyList<ulong> players, long stake,
        Func<IReadOnlyList<ulong>, IReadOnlyDictionary<ulong, long>> decide) =>
        SettleAsync(guildId, players.Distinct().Select(p => (p, stake)).ToList(), decide);

    /// <summary>
    /// The same, with a stake per player (a race's bets, a crash's): each must still cover their
    /// own, and a net never goes below minus that stake. A stake of 0 always pays: that's how a
    /// reward with nothing at risk (a quiz-like win, a race owner's bonus) goes through here too.
    /// </summary>
    /// <remarks>
    /// Through <see cref="EconomySave"/>: two games settling for the same player at once both land,
    /// one after the other. A retry asks <paramref name="decide"/> again, so it must only compute
    /// (what it captures is reassigned by the run that is saved).
    /// </remarks>
    public Task<ArcadeSettlement> SettleAsync(ulong guildId, IReadOnlyList<(ulong Player, long Stake)> stakes,
        Func<IReadOnlyList<ulong>, IReadOnlyDictionary<ulong, long>> decide) =>
        EconomySave.RetryAsync(_db_context, () => SettleOnceAsync(guildId, stakes, decide));

    private async Task<ArcadeSettlement> SettleOnceAsync(ulong guildId, IReadOnlyList<(ulong Player, long Stake)> stakes,
        Func<IReadOnlyList<ulong>, IReadOnlyDictionary<ulong, long>> decide)
    {
        var wallets = new Dictionary<ulong, Models.PebbleWallet>();
        var stakeOf = new Dictionary<ulong, long>();
        var payers = new List<ulong>();
        var dropped = new List<ulong>();
        foreach (var (id, stake) in stakes)
        {
            if (stakeOf.ContainsKey(id)) continue;
            var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, id);
            wallets[id] = wallet;
            stakeOf[id] = stake;
            (wallet.Balance >= stake ? payers : dropped).Add(id);
        }

        var nets = decide(payers);
        var applied = new Dictionary<ulong, long>();
        long earned = 0, spent = 0;
        foreach (var id in payers)
        {
            var net = Math.Max(-stakeOf[id], nets.GetValueOrDefault(id));
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
