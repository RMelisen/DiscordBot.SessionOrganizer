using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Who should hold the Plynling role — everyone with a living Plynling, SYNCS included through her
// mascot (see docs/agents/plynling.md, "The Plynling role") — handed to RoleMirror, which applies it.
// Two paths, one rule:
//
// - SyncMemberAsync, right after adopt / abandon / staff resurrect, so the role arrives at once;
// - SyncGuildAsync, the last step of every PlynlingSweepService pass, which catches what raises
//   nothing: lazy deaths, a rejoin, a role given or taken by hand, a hook that failed.
//
// Singleton like RoleMirror; it scopes per unit of work and never injects a DB service. Nothing here
// throws to its caller.
public sealed class PlynlingRoleService
{
    public const string DefaultName = "Maîtres de Plynlings";

    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly RoleMirror _mirror;
    private readonly ILogger<PlynlingRoleService> _logger;

    public PlynlingRoleService(IServiceProvider services, GuildConfigService config, RoleMirror mirror,
        ILogger<PlynlingRoleService> logger)
    {
        _services = services;
        _config = config;
        _mirror = mirror;
        _logger = logger;
    }

    /// <summary>Who owns a living Plynling in this guild, settled in memory (read-only).</summary>
    public async Task<HashSet<ulong>> LoadHoldersAsync(ulong guildId)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PlynlingService>()
            .GetLivingOwnerIdsAsync(guildId, DateTimeOffset.UtcNow);
    }

    /// <summary>Brings the whole guild in line with the database. Never throws.</summary>
    public async Task SyncGuildAsync(SocketGuild guild)
    {
        // GetAsync never throws (it degrades to an empty config), and MirrorGuildAsync catches
        // everything: keep it that way, the sweep relies on it.
        var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
        if (roleId == 0) return;
        await _mirror.MirrorGuildAsync(guild, RoleKind.Plynling, roleId, () => LoadHoldersAsync(guild.Id));
    }

    /// <summary>Brings one person in line, right after a life change. Never throws.</summary>
    public async Task SyncMemberAsync(SocketGuild guild, ulong userId)
    {
        try
        {
            var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
            if (roleId == 0) return;

            bool owns;
            await using (var scope = _services.CreateAsyncScope())
                owns = await scope.ServiceProvider.GetRequiredService<PlynlingService>()
                    .HasLivingAsync(guild.Id, userId, DateTimeOffset.UtcNow);

            await _mirror.MirrorMemberAsync(guild, RoleKind.Plynling, roleId, userId, owns);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plynling role update failed for {UserId} in guild {GuildId}; the hourly pass will retry.",
                userId, guild.Id);
        }
    }
}
