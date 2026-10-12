using System.Linq.Expressions;
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
///
/// Ids of zero mean "not configured": the single-channel settings then fall back to their
/// hardcoded default — except the quiz channel, which has none (zero means no quiz). The
/// sets hold only what an admin added on top of the hardcoded floor.
/// </remarks>
public sealed record GuildConfig(
    ulong ModeratorRoleId,
    ulong GameChannelId,
    ulong MainChannelId,
    IReadOnlySet<ulong> ExcludedChannels,
    IReadOnlySet<ulong> IdleChannels,
    IReadOnlySet<ulong> ShameVoters,
    ulong QuizChannelId)
{
    /// <summary>What an unconfigured guild looks like — nothing set, nothing added.</summary>
    public static readonly GuildConfig Empty = new(0, 0, 0,
        new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), 0);
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
            var excluded = await db.GuildExcludedChannels
                .Where(c => c.GuildId == guildId)
                .Select(c => c.ChannelId)
                .ToListAsync();
            var idle = await db.GuildIdleChannels
                .Where(c => c.GuildId == guildId)
                .Select(c => c.ChannelId)
                .ToListAsync();
            var voters = await db.GuildShameVoters
                .Where(v => v.GuildId == guildId)
                .Select(v => v.UserId)
                .ToListAsync();

            loaded = new GuildConfig(
                settings?.ModeratorRoleId ?? 0,
                settings?.GameChannelId ?? 0,
                settings?.MainChannelId ?? 0,
                excluded.ToHashSet(), idle.ToHashSet(), voters.ToHashSet(),
                settings?.QuizChannelId ?? 0);
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
    public Task<bool> AddExcludedChannelAsync(ulong guildId, ulong channelId) =>
        AddRowAsync(guildId, db => db.GuildExcludedChannels,
            c => c.GuildId == guildId && c.ChannelId == channelId,
            () => new GuildExcludedChannel { GuildId = guildId, ChannelId = channelId });

    /// <summary>
    /// Removes an admin-added channel. Returns false if it was not on the list.
    /// Cannot touch the hardcoded exclusions — those are not in this table at all.
    /// </summary>
    public Task<bool> RemoveExcludedChannelAsync(ulong guildId, ulong channelId) =>
        RemoveRowAsync(guildId, db => db.GuildExcludedChannels,
            c => c.GuildId == guildId && c.ChannelId == channelId);

    /// <summary>Adds an idle channel. Same contract as <see cref="AddExcludedChannelAsync"/>.</summary>
    public Task<bool> AddIdleChannelAsync(ulong guildId, ulong channelId) =>
        AddRowAsync(guildId, db => db.GuildIdleChannels,
            c => c.GuildId == guildId && c.ChannelId == channelId,
            () => new GuildIdleChannel { GuildId = guildId, ChannelId = channelId });

    /// <summary>Removes an admin-added idle channel. Returns false if it was not on the list.</summary>
    public Task<bool> RemoveIdleChannelAsync(ulong guildId, ulong channelId) =>
        RemoveRowAsync(guildId, db => db.GuildIdleChannels,
            c => c.GuildId == guildId && c.ChannelId == channelId);

    /// <summary>Adds a /shame voter. Returns false if they were already on the list.</summary>
    public Task<bool> AddShameVoterAsync(ulong guildId, ulong userId) =>
        AddRowAsync(guildId, db => db.GuildShameVoters,
            v => v.GuildId == guildId && v.UserId == userId,
            () => new GuildShameVoter { GuildId = guildId, UserId = userId });

    /// <summary>
    /// Removes an admin-added voter. Returns false if they were not on the list. Cannot
    /// touch ShameModule's hardcoded voters — those are not in this table at all.
    /// </summary>
    public Task<bool> RemoveShameVoterAsync(ulong guildId, ulong userId) =>
        RemoveRowAsync(guildId, db => db.GuildShameVoters,
            v => v.GuildId == guildId && v.UserId == userId);

    /// <summary>
    /// Sets the moderator role, or clears it with <paramref name="roleId"/> of zero.
    /// </summary>
    public Task SetModeratorRoleAsync(ulong guildId, ulong roleId) =>
        UpdateSettingsAsync(guildId, s => s.ModeratorRoleId = roleId);

    /// <summary>Sets the Plynling game channel, or goes back to the default with zero.</summary>
    public Task SetGameChannelAsync(ulong guildId, ulong channelId) =>
        UpdateSettingsAsync(guildId, s => s.GameChannelId = channelId);

    /// <summary>Sets her main channel, or goes back to the default with zero.</summary>
    public Task SetMainChannelAsync(ulong guildId, ulong channelId) =>
        UpdateSettingsAsync(guildId, s => s.MainChannelId = channelId);

    /// <summary>Sets the pop quiz channel, or turns the quiz off with zero.</summary>
    public Task SetQuizChannelAsync(ulong guildId, ulong channelId) =>
        UpdateSettingsAsync(guildId, s => s.QuizChannelId = channelId);

    // The settings row is created lazily, the first time anything is configured.
    private async Task UpdateSettingsAsync(ulong guildId, Action<GuildSettings> apply)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await db.GuildSettings.FirstOrDefaultAsync(s => s.GuildId == guildId);
        if (settings is null)
        {
            settings = new GuildSettings { GuildId = guildId };
            db.GuildSettings.Add(settings);
        }

        apply(settings);
        await db.SaveChangesAsync();

        Invalidate(guildId);
    }

    // The three set-tables (excluded channels, idle channels, voters) share one shape: a
    // row per (guild, id), added once, removed if present.
    private async Task<bool> AddRowAsync<T>(ulong guildId, Func<AppDbContext, DbSet<T>> table,
        Expression<Func<T, bool>> match, Func<T> create) where T : class
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var set = table(db);
        if (await set.AnyAsync(match)) return false;

        set.Add(create());
        await db.SaveChangesAsync();

        Invalidate(guildId);
        return true;
    }

    private async Task<bool> RemoveRowAsync<T>(ulong guildId, Func<AppDbContext, DbSet<T>> table,
        Expression<Func<T, bool>> match) where T : class
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var set = table(db);
        var row = await set.FirstOrDefaultAsync(match);
        if (row is null) return false;

        set.Remove(row);
        await db.SaveChangesAsync();

        Invalidate(guildId);
        return true;
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
