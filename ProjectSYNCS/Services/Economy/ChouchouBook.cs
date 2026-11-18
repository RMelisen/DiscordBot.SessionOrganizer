using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;

namespace ProjectSYNCS.Services;

/// <summary>
/// Who is her chouchou right now (/bribe favourite), for the paths that run on every message
/// (ChatterService's mention and reply answers, ReactionService), which must never read the
/// database per message. The truth is <c>PebbleWallet.ChouchouUntil</c>; this is its cache, read
/// from the wallets once, lazily, then kept current by BribeService through <see cref="Set"/>.
/// </summary>
/// <remarks>A singleton: it owns state. It opens its own scope for the one load, never pins a context.</remarks>
public sealed class ChouchouBook
{
    private readonly IServiceProvider _services;
    private readonly ConcurrentDictionary<(ulong Guild, ulong User), DateTimeOffset> _until = new();
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private volatile bool _loaded;

    public ChouchouBook(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> IsFavouriteAsync(ulong guildId, ulong userId, DateTimeOffset now)
    {
        if (!_loaded)
        {
            // A failed load (the Pi's database busy) means a plain answer this time and another try
            // on the next message, never an exception in the chat paths.
            try { await LoadAsync(); }
            catch (Exception ex)
            {
                _services.GetService<ILogger<ChouchouBook>>()?.LogWarning(ex, "Could not load the chouchous; answering plainly until the next try");
                return false;
            }
        }
        return _until.TryGetValue((guildId, userId), out var until) && until > now;
    }

    /// <summary>Called after a bribe is saved, with the wallet's new end. Keeps the later end: two quick buys
    /// can call this out of order, and the wallet only ever moves forward.</summary>
    public void Set(ulong guildId, ulong userId, DateTimeOffset until) =>
        _until.AddOrUpdate((guildId, userId), until, (_, current) => current > until ? current : until);

    private async Task LoadAsync()
    {
        await _loadGate.WaitAsync();
        try
        {
            if (_loaded) return;
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // The date is compared in memory: SQLite cannot translate DateTimeOffset comparisons.
            var rows = await db.PebbleWallets.AsNoTracking()
                .Where(w => w.ChouchouUntil != null)
                .Select(w => new { w.GuildId, w.UserId, w.ChouchouUntil })
                .ToListAsync();
            foreach (var row in rows)
                // A bribe saved while this was loading already set a later end: keep the later one.
                _until.AddOrUpdate((row.GuildId, row.UserId), row.ChouchouUntil!.Value,
                    (_, current) => current > row.ChouchouUntil!.Value ? current : row.ChouchouUntil!.Value);
            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }
}
