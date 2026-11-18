using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

public enum BribeOutcome { Done, TooPoor, Family, NothingToForgive }

// Price is what this bribe cost (or would have: TooPoor names it); NextPrice what the next one this
// week will cost. FavouriteUntil is set by a chouchou bought.
public sealed record BribeResult(BribeOutcome Outcome, long Price, long Balance, long NextPrice, DateTimeOffset? FavouriteUntil);

/// <summary>
/// /bribe: paying her off. Both kinds share one doubling weekly price (Helpers/Economy/Bribes), are
/// logged <c>spend.bribe</c> (not « Mécène »: only the jar counts). Each is one unit of work through
/// <see cref="EconomySave"/>, inside a transaction because erasing a shame hit runs its SQL at once.
/// </summary>
public class BribeService
{
    private readonly AppDbContext _db_context;
    private readonly ChouchouBook _book;

    public BribeService(AppDbContext db_context, ChouchouBook book)
    {
        _db_context = db_context;
        _book = book;
    }

    /// <summary>24 h as her chouchou, from now or from the end of the current one. Papa and Tata are refused.</summary>
    public async Task<BribeResult> BuyFavouriteAsync(ulong guildId, ulong userId, DateTimeOffset now)
    {
        // Family first, before any price: their tiers stay what they are (people.md).
        if (userId == AvailabilityService.OwnerId || userId == BotResponses.TataId)
            return new BribeResult(BribeOutcome.Family, 0, 0, 0, null);

        var result = await EconomySave.RetryAsync(_db_context, () => PayOnceAsync(guildId, userId, now, wallet =>
        {
            var from = wallet.ChouchouUntil is { } until && until > now ? until : now;
            wallet.ChouchouUntil = from + Bribes.FavouriteFor;
            return Task.FromResult<(BribeOutcome, DateTimeOffset?)>((BribeOutcome.Done, wallet.ChouchouUntil));
        }));
        if (result.FavouriteUntil is { } end) _book.Set(guildId, userId, end);
        return result;
    }

    /// <summary>One /shame hit erased in <paramref name="category"/>; refused when there is none.</summary>
    public Task<BribeResult> ForgiveShameAsync(ulong guildId, ulong userId, ShameCategory category, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => PayOnceAsync(guildId, userId, now, async _ =>
            await ShameService.ForgiveOneAsync(_db_context, guildId, userId, category)
                ? (BribeOutcome.Done, null)
                : (BribeOutcome.NothingToForgive, (DateTimeOffset?)null)));

    // The shared half: price, balance check, the thing bought, payment, log, one save, one
    // commit. `buy` runs only once the bribe is affordable, and returns Done or why it was refused;
    // a refusal rolls back whatever it did.
    private async Task<BribeResult> PayOnceAsync(ulong guildId, ulong userId, DateTimeOffset now,
        Func<Models.PebbleWallet, Task<(BribeOutcome Outcome, DateTimeOffset? Until)>> buy)
    {
        await using var tx = await _db_context.Database.BeginTransactionAsync();
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        var week = CosmeticCatalog.WeekKey(now);
        var price = Bribes.Price(wallet.BribeWeek, wallet.BribesThisWeek, week);
        if (wallet.Balance < price) return new BribeResult(BribeOutcome.TooPoor, price, wallet.Balance, price, null);

        var (outcome, until) = await buy(wallet);
        if (outcome != BribeOutcome.Done) return new BribeResult(outcome, price, wallet.Balance, price, null);

        wallet.Balance -= price;
        wallet.BribesThisWeek = (wallet.BribeWeek == week ? wallet.BribesThisWeek : 0) + 1;
        wallet.BribeWeek = week;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendBribe, price, now);
        await _db_context.SaveChangesAsync();
        await tx.CommitAsync();
        return new BribeResult(BribeOutcome.Done, price, wallet.Balance,
            Bribes.Price(wallet.BribeWeek, wallet.BribesThisWeek, week), until);
    }
}
