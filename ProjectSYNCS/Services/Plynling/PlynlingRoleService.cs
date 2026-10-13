using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Keeps the configured Plynling role on everyone with a living Plynling and off everyone else
// (strict mirror — see docs/agents/plynling.md, "The Plynling role"). Two paths, one rule:
//
// - SyncMemberAsync, right after adopt / abandon / staff resurrect, so the role arrives at once;
// - SyncGuildAsync, the last step of every PlynlingSweepService pass, which catches what raises
//   nothing: lazy deaths, a rejoin, a role given or taken by hand, a hook that failed.
//
// Singleton: it holds the pass lock and the logged-once set. It takes IServiceProvider and scopes
// per unit of work, never a DB service. Nothing here throws to its caller: a failed role change is
// logged, and the next hourly pass is the backstop.
public sealed class PlynlingRoleService
{
    private static readonly RequestOptions Granted = new() { AuditLogReason = "Plynling vivant" };
    private static readonly RequestOptions Revoked = new() { AuditLogReason = "Plus de Plynling" };

    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly ILogger<PlynlingRoleService> _logger;

    // One guild pass at a time: the sweep and a `set` can overlap. The member hook does not take it —
    // same rule, and adding a held role or removing an absent one is a no-op on Discord's side.
    private readonly SemaphoreSlim _passGate = new(1, 1);

    // Guilds whose role problem was already logged, so a broken setup logs once, not hourly. A plain
    // set under a lock rather than a BoundedSet: entries are removed when the problem goes away, and
    // there are only ever a handful of guilds.
    private readonly object _loggedGate = new();
    private readonly HashSet<ulong> _loggedProblem = new();

    public PlynlingRoleService(IServiceProvider services, GuildConfigService config, ILogger<PlynlingRoleService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Why she cannot manage <paramref name="role"/> in <paramref name="guild"/>, or null. A null
    /// <paramref name="role"/> means the configured id no longer resolves.
    /// </summary>
    public static string? Problem(SocketGuild guild, SocketRole? role, bool adopting)
    {
        var me = guild.CurrentUser;
        var bot = me is null
            ? new BotFacts(false, 0)
            : new BotFacts(me.GuildPermissions.ManageRoles, me.Hierarchy);
        var facts = role is null
            ? RoleFacts.Missing
            : new RoleFacts(true, role.IsEveryone, role.IsManaged, role.Position, role.Permissions.RawValue);
        return PlynlingRoleSync.Refusal(facts, bot, adopting);
    }

    /// <summary>
    /// What a sync would do now, for the <c>set</c> / <c>create</c> reply, or null if the owners
    /// can't be read — the counts only decorate the reply, so a failure must not cost it.
    /// </summary>
    public async Task<(int ToAdd, int ToRemove)?> PreviewAsync(SocketGuild guild, ulong roleId)
    {
        try
        {
            var holders = guild.GetRole(roleId)?.Members.Select(m => m.Id).ToHashSet() ?? new HashSet<ulong>();
            var members = guild.Users.Select(u => u.Id).ToHashSet();
            var owners = await LoadOwnersAsync(guild.Id);
            var (toAdd, toRemove) = PlynlingRoleSync.Diff(owners, holders, members);
            return (toAdd.Count, toRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preview the Plynling role sync for guild {GuildId}.", guild.Id);
            return null;
        }
    }

    /// <summary>
    /// A guild sync in the background, after <paramref name="delay"/> — for <c>set</c> and
    /// <c>create</c>, whose reply must not wait on dozens of role calls.
    /// </summary>
    public void StartSync(SocketGuild guild, TimeSpan delay) =>
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            await SyncGuildAsync(guild);
        });

    /// <summary>Brings the whole guild in line with the database. Never throws.</summary>
    public async Task SyncGuildAsync(SocketGuild guild)
    {
        await _passGate.WaitAsync();
        try
        {
            await SyncGuildCoreAsync(guild);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plynling role sync failed for guild {GuildId}.", guild.Id);
        }
        finally
        {
            _passGate.Release();
        }
    }

    /// <summary>Brings one person in line, right after a life change. Never throws.</summary>
    public async Task SyncMemberAsync(SocketGuild guild, ulong userId)
    {
        try
        {
            var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
            if (roleId == 0) return;
            var role = guild.GetRole(roleId);
            if (Problem(guild, role, adopting: false) is not null) return;   // the guild pass logs it
            if (guild.GetUser(userId) is not { } member) return;

            bool owns;
            await using (var scope = _services.CreateAsyncScope())
                owns = await scope.ServiceProvider.GetRequiredService<PlynlingService>()
                    .HasLivingAsync(guild.Id, userId, DateTimeOffset.UtcNow);

            bool holds = member.Roles.Any(r => r.Id == roleId);
            if (owns && !holds) await member.AddRoleAsync(roleId, Granted);
            else if (!owns && holds) await member.RemoveRoleAsync(roleId, Revoked);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plynling role update failed for {UserId} in guild {GuildId}; the hourly pass will retry.",
                userId, guild.Id);
        }
    }

    private async Task SyncGuildCoreAsync(SocketGuild guild)
    {
        var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
        if (roleId == 0) return;

        var role = guild.GetRole(roleId);
        if (Problem(guild, role, adopting: false) is { } problem)
        {
            bool first;
            lock (_loggedGate) first = _loggedProblem.Add(guild.Id);
            if (first)
                _logger.LogWarning("Plynling role {RoleId} in guild {GuildId} can't be managed: {Problem}",
                    roleId, guild.Id, problem);
            return;
        }
        lock (_loggedGate) _loggedProblem.Remove(guild.Id);

        // With a partial member cache an owner looks absent and would be skipped as "left the
        // guild"; doing nothing for an hour is better than acting on half a list.
        if (!guild.HasAllMembers)
        {
            _logger.LogDebug("Skipping the Plynling role sync for guild {GuildId}: member list incomplete.", guild.Id);
            return;
        }

        // Cache first, owners second. The owner read is the slow part on the Pi; reading holders
        // after it would count someone who adopted meanwhile (and got the role from the hook) as a
        // holder without a Plynling, and strip it. This order can only cause a redundant add.
        var holders = role!.Members.Select(m => m.Id).ToHashSet();
        var members = guild.Users.Select(u => u.Id).ToHashSet();
        var owners = await LoadOwnersAsync(guild.Id);
        var (toAdd, toRemove) = PlynlingRoleSync.Diff(owners, holders, members);

        foreach (var id in toAdd)
        {
            try
            {
                if (guild.GetUser(id) is { } member) await member.AddRoleAsync(roleId, Granted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to give the Plynling role to {UserId} in guild {GuildId}.", id, guild.Id);
            }
        }

        if (toRemove.Count > PlynlingRoleSync.MaxRemovalsPerPass)
            _logger.LogWarning("Plynling role: {Count} removals due in guild {GuildId}; doing {Max} this pass.",
                toRemove.Count, guild.Id, PlynlingRoleSync.MaxRemovalsPerPass);
        foreach (var id in toRemove.Take(PlynlingRoleSync.MaxRemovalsPerPass))
        {
            try
            {
                if (guild.GetUser(id) is { } member) await member.RemoveRoleAsync(roleId, Revoked);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove the Plynling role from {UserId} in guild {GuildId}.", id, guild.Id);
            }
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
            _logger.LogInformation("Plynling role synced in guild {GuildId}: +{Added} −{Removed}.",
                guild.Id, toAdd.Count, Math.Min(toRemove.Count, PlynlingRoleSync.MaxRemovalsPerPass));
    }

    private async Task<HashSet<ulong>> LoadOwnersAsync(ulong guildId)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PlynlingService>()
            .GetLivingOwnerIdsAsync(guildId, DateTimeOffset.UtcNow);
    }
}
