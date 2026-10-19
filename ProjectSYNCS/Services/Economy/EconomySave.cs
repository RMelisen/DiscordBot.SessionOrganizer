using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;

namespace ProjectSYNCS.Services;

/// <summary>
/// Runs one economy unit of work, and runs it again from a clean slate when a concurrent save moved
/// the same wallet or item first. <c>PebbleWallet.Balance</c> and <c>InventoryItem.Quantity</c> are
/// concurrency tokens, so the slower of two saves throws instead of overwriting, and rolls back
/// whole. The retry re-reads and re-checks everything: two simultaneous gives of the last 5 items
/// end as one gift and one « pas assez », never two gifts.
/// </summary>
/// <remarks>
/// <paramref name="work"/> must do all of its own reads and nothing but database work: it may run
/// more than once, and only its last run is saved. The context must be the work's own (every
/// service here is transient), since clearing it drops whatever else it tracks.
/// </remarks>
public static class EconomySave
{
    // A third conflict in a row is no longer a race between two clicks; let it surface.
    private const int Attempts = 3;

    public static async Task<T> RetryAsync<T>(AppDbContext db, Func<Task<T>> work)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await work();
            }
            catch (DbUpdateConcurrencyException) when (attempt < Attempts)
            {
                db.ChangeTracker.Clear();
            }
        }
    }
}
