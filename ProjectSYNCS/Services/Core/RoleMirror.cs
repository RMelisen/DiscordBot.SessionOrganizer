using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// One role SYNCS mirrors: its log key, the <c>/config</c> subgroup that manages it, and the audit
/// reasons for her changes. Two today — the Plynling owners and the quiz's Grosse tête.
/// </summary>
public sealed record RoleKind(string Key, string Command, string GrantedReason, string RevokedReason)
{
    public static readonly RoleKind Plynling = new("plynling", "plynling-role", "Plynling vivant", "Plus de Plynling");
    public static readonly RoleKind QuizCrown = new("quiz", "quiz-role", "Grosse tête de la semaine", "Couronne passée");
}

// Keeps a role on exactly the people its owner says should hold it, and off everyone else (strict
// mirror). The owner supplies "who" — PlynlingRoleService the living Plynlings' owners,
// QuizCrownService last week's champions — and this applies it: the diff, the removal cap, the skip
// on a half-downloaded member list, the problem logged once, one try per person.
//
// Singleton: it holds the pass lock and the logged-once set. It reads no database itself (the
// owners' loaders scope their own), and nothing here throws to its caller: a failed change is
// logged, and the owner's next pass is the backstop.
public sealed class RoleMirror
{
    private readonly ILogger<RoleMirror> _logger;

    // One guild pass at a time, every kind included: the hourly passes and a `set` can overlap. The
    // member path does not take it — adding a held role or removing an absent one is a no-op on
    // Discord's side, so a race converges.
    private readonly SemaphoreSlim _passGate = new(1, 1);

    // (guild, kind) pairs whose problem was already logged, so a broken setup logs once, not hourly —
    // and a broken quiz role never hides a broken Plynling role. A plain set under a lock: entries
    // are removed when the problem goes away, and there are only ever a handful.
    private readonly object _loggedGate = new();
    private readonly HashSet<(ulong GuildId, string Key)> _loggedProblem = new();

    public RoleMirror(ILogger<RoleMirror> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Why she cannot manage <paramref name="role"/> in <paramref name="guild"/>, or null. A null
    /// <paramref name="role"/> means the configured id no longer resolves.
    /// </summary>
    public static string? Problem(SocketGuild guild, SocketRole? role, bool adopting, RoleKind kind)
    {
        var me = guild.CurrentUser;
        var bot = me is null
            ? new BotFacts(false, 0)
            : new BotFacts(me.GuildPermissions.ManageRoles, me.Hierarchy);
        var facts = role is null
            ? RoleFacts.Missing
            : new RoleFacts(true, role.IsEveryone, role.IsManaged, role.Position, role.Permissions.RawValue);
        return RoleMirrorRules.Refusal(facts, bot, adopting, kind.Command);
    }

    /// <summary>
    /// What a pass would do now, for the <c>set</c> / <c>create</c> reply, or null if the holders
    /// can't be computed — the counts only decorate the reply, so a failure must not cost it.
    /// </summary>
    public async Task<(int ToAdd, int ToRemove)?> PreviewAsync(SocketGuild guild, ulong roleId,
        Func<Task<HashSet<ulong>>> loadHolders)
    {
        try
        {
            var holders = guild.GetRole(roleId)?.Members.Select(m => m.Id).ToHashSet() ?? new HashSet<ulong>();
            var members = guild.Users.Select(u => u.Id).ToHashSet();
            var (toAdd, toRemove) = RoleMirrorRules.Diff(await loadHolders(), holders, members);
            return (toAdd.Count, toRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preview a role pass for guild {GuildId}.", guild.Id);
            return null;
        }
    }

    /// <summary>
    /// A pass in the background, after <paramref name="delay"/> — for <c>set</c> and <c>create</c>,
    /// whose reply must not wait on dozens of role calls. <paramref name="pass"/> must not throw
    /// (<see cref="MirrorGuildAsync"/> never does).
    /// </summary>
    public void StartMirror(TimeSpan delay, Func<Task> pass) =>
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            await pass();
        });

    /// <summary>
    /// Brings <paramref name="roleId"/> in line with <paramref name="loadHolders"/> for the whole
    /// guild. Never throws.
    /// </summary>
    public async Task MirrorGuildAsync(SocketGuild guild, RoleKind kind, ulong roleId,
        Func<Task<HashSet<ulong>>> loadHolders)
    {
        await _passGate.WaitAsync();
        try
        {
            await MirrorGuildCoreAsync(guild, kind, roleId, loadHolders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The {Kind} role pass failed for guild {GuildId}.", kind.Key, guild.Id);
        }
        finally
        {
            _passGate.Release();
        }
    }

    /// <summary>Brings one person in line, right after a change that concerns them. Never throws.</summary>
    public async Task MirrorMemberAsync(SocketGuild guild, RoleKind kind, ulong roleId, ulong userId, bool shouldHold)
    {
        try
        {
            if (Problem(guild, guild.GetRole(roleId), adopting: false, kind) is not null) return;   // the guild pass logs it
            if (guild.GetUser(userId) is not { } member) return;

            bool holds = member.Roles.Any(r => r.Id == roleId);
            if (shouldHold && !holds) await member.AddRoleAsync(roleId, Granted(kind));
            else if (!shouldHold && holds) await member.RemoveRoleAsync(roleId, Revoked(kind));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The {Kind} role update failed for {UserId} in guild {GuildId}; the next pass will retry.",
                kind.Key, userId, guild.Id);
        }
    }

    private async Task MirrorGuildCoreAsync(SocketGuild guild, RoleKind kind, ulong roleId,
        Func<Task<HashSet<ulong>>> loadHolders)
    {
        var role = guild.GetRole(roleId);
        if (Problem(guild, role, adopting: false, kind) is { } problem)
        {
            bool first;
            lock (_loggedGate) first = _loggedProblem.Add((guild.Id, kind.Key));
            if (first)
                _logger.LogWarning("The {Kind} role {RoleId} in guild {GuildId} can't be managed: {Problem}",
                    kind.Key, roleId, guild.Id, problem);
            return;
        }
        lock (_loggedGate) _loggedProblem.Remove((guild.Id, kind.Key));

        // With a partial member cache a holder looks absent and would be skipped as "left the
        // guild"; doing nothing until the next pass is better than acting on half a list.
        if (!guild.HasAllMembers)
        {
            _logger.LogDebug("Skipping the {Kind} role pass for guild {GuildId}: member list incomplete.", kind.Key, guild.Id);
            return;
        }

        // Cache first, holders second. The loader is the slow part on the Pi; reading the role's
        // members after it would count someone the member path just gave the role to as a holder
        // who shouldn't be, and strip it. This order can only cause a redundant add.
        var current = role!.Members.Select(m => m.Id).ToHashSet();
        var members = guild.Users.Select(u => u.Id).ToHashSet();
        var desired = await loadHolders();
        var (toAdd, toRemove) = RoleMirrorRules.Diff(desired, current, members);

        foreach (var id in toAdd)
        {
            try
            {
                if (guild.GetUser(id) is { } member) await member.AddRoleAsync(roleId, Granted(kind));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to give the {Kind} role to {UserId} in guild {GuildId}.", kind.Key, id, guild.Id);
            }
        }

        if (toRemove.Count > RoleMirrorRules.MaxRemovalsPerPass)
            _logger.LogWarning("The {Kind} role: {Count} removals due in guild {GuildId}; doing {Max} this pass.",
                kind.Key, toRemove.Count, guild.Id, RoleMirrorRules.MaxRemovalsPerPass);
        foreach (var id in toRemove.Take(RoleMirrorRules.MaxRemovalsPerPass))
        {
            try
            {
                if (guild.GetUser(id) is { } member) await member.RemoveRoleAsync(roleId, Revoked(kind));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove the {Kind} role from {UserId} in guild {GuildId}.", kind.Key, id, guild.Id);
            }
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
            _logger.LogInformation("The {Kind} role synced in guild {GuildId}: +{Added} −{Removed}.",
                kind.Key, guild.Id, toAdd.Count, Math.Min(toRemove.Count, RoleMirrorRules.MaxRemovalsPerPass));
    }

    private static RequestOptions Granted(RoleKind kind) => new() { AuditLogReason = kind.GrantedReason };
    private static RequestOptions Revoked(RoleKind kind) => new() { AuditLogReason = kind.RevokedReason };
}
