using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One guild's runtime configuration, as the hot paths need to read it.</summary>
/// <remarks>
/// A snapshot rather than live entities: it is handed out from a cache and read from
/// several threads, so it has to be immutable.
/// </remarks>
public sealed record GuildConfig(ulong ModeratorRoleId, IReadOnlySet<ulong> ExcludedChannels)
{
    /// <summary>What an unconfigured guild looks like — no role, nothing extra excluded.</summary>
    public static readonly GuildConfig Empty = new(0, new HashSet<ulong>());
}

// Per-guild settings an admin can change at runtime, and the cache that makes them
// affordable to read.
//
// **Singleton, unlike every other service wrapping AppDbContext**, and deliberately so:
// it holds cache state, which a transient would drop on every resolve. It therefore
// takes IServiceProvider and opens a scope per unit of work rather than injecting
// AppDbContext, exactly like XpTracker and the other stateful singletons — injecting the
// context directly would pin one for the process lifetime.
//
// **The cache is not premature.** XpTracker's exclusion check runs on *every* message,
// and it must run before TryClaim (a message in an excluded channel must not burn that
// person's cooldown — see CLAUDE.md). Most messages never reach a database today,
// because the 60-second claim stops them first; an uncached read here would put an EF
// scope and a query on every single message instead. Correctness is cheap to reason
// about because this service is the only writer and the process is the only one
// touching the file: any write drops that guild's entry, and the next read reloads it.
// Public, unlike the other stateful singletons here: ConfigModule and ShameModule are
// public (Discord.Net discovers modules by reflection and needs them so), and a public
// constructor cannot take an internal parameter type.
public sealed class GuildConfigService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GuildConfigService> _logger;

    private readonly object _gate = new();
    private readonly Dictionary<ulong, GuildConfig> _cache = new();

    public GuildConfigService(IServiceProvider services, ILogger<GuildConfigService> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// This guild's configuration, from cache when possible. Never throws and never
    /// returns null: a failed read degrades to <see cref="GuildConfig.Empty"/>, which
    /// is the unconfigured behaviour, so a database problem can only lose the *extra*
    /// exclusions — never the hardcoded ones, which live in code and are checked
    /// separately.
    /// </summary>
    public async Task<GuildConfig> GetAsync(ulong guildId)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(guildId, out var cached)) return cached;
        }

        GuildConfig loaded;
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var settings = await db.GuildSettings.FirstOrDefaultAsync(s => s.GuildId == guildId);
            var channels = await db.GuildExcludedChannels
                .Where(c => c.GuildId == guildId)
                .Select(c => c.ChannelId)
                .ToListAsync();

            loaded = new GuildConfig(settings?.ModeratorRoleId ?? 0, channels.ToHashSet());
        }
        catch (Exception ex)
        {
            // Not cached: a transient failure must not pin "unconfigured" for the
            // process lifetime, so the next call tries again.
            _logger.LogWarning(ex, "Failed to read config for guild {GuildId}; treating it as unconfigured.", guildId);
            return GuildConfig.Empty;
        }

        lock (_gate)
        {
            // Two callers can race to load the same guild. Both computed the same
            // thing from the same table, so last-writer-wins is harmless.
            _cache[guildId] = loaded;
        }
        return loaded;
    }

    /// <summary>
    /// Adds a channel to this guild's excluded list. Returns false if it was already
    /// there — the caller says so rather than reporting a change that did not happen.
    /// </summary>
    public async Task<bool> AddExcludedChannelAsync(ulong guildId, ulong channelId)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.GuildExcludedChannels.AnyAsync(c => c.GuildId == guildId && c.ChannelId == channelId))
            return false;

        db.GuildExcludedChannels.Add(new GuildExcludedChannel { GuildId = guildId, ChannelId = channelId });
        await db.SaveChangesAsync();

        Invalidate(guildId);
        return true;
    }

    /// <summary>
    /// Removes an admin-added channel. Returns false if it was not on the list.
    /// Cannot touch the hardcoded exclusions — those are not in this table at all.
    /// </summary>
    public async Task<bool> RemoveExcludedChannelAsync(ulong guildId, ulong channelId)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var row = await db.GuildExcludedChannels
            .FirstOrDefaultAsync(c => c.GuildId == guildId && c.ChannelId == channelId);
        if (row is null) return false;

        db.GuildExcludedChannels.Remove(row);
        await db.SaveChangesAsync();

        Invalidate(guildId);
        return true;
    }

    /// <summary>
    /// Sets the moderator role, or clears it with <paramref name="roleId"/> of zero.
    /// </summary>
    public async Task SetModeratorRoleAsync(ulong guildId, ulong roleId)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.GuildSettings.FirstOrDefaultAsync(s => s.GuildId == guildId);
        if (settings is null)
        {
            settings = new GuildSettings { GuildId = guildId };
            db.GuildSettings.Add(settings);
        }

        settings.ModeratorRoleId = roleId;
        await db.SaveChangesAsync();

        Invalidate(guildId);
    }

    // Dropped rather than updated in place: the next read rebuilds it from the table
    // that was just written, so there is no second place for the new value to be
    // assembled slightly differently.
    private void Invalidate(ulong guildId)
    {
        lock (_gate)
        {
            _cache.Remove(guildId);
        }
    }
}
