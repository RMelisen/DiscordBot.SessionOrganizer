using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// The one path every member-badge write takes: fire and forget, after the flow's own save, with
/// failures swallowed and logged so a badge never breaks what it rides on. Never announces.
/// </summary>
public class BadgeAwarder
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<BadgeAwarder> _logger;

    // Claims already taken, so a hot path doesn't hit the database for a badge already held.
    // Bounded by members x badges; a plain set because a failed save must be able to release.
    private readonly HashSet<(ulong, ulong, string)> _claims = new();
    private readonly object _lock = new();

    public BadgeAwarder(IServiceProvider services, DiscordSocketClient client, ILogger<BadgeAwarder> logger)
    {
        _services = services;
        _client = client;
        _logger = logger;
    }

    /// <summary>Earns a moment or secret once.</summary>
    public void Award(ulong guildId, ulong userId, string key)
    {
        if (!MayWrite(guildId, userId, key)) return;
        var claim = (guildId, userId, key);
        lock (_lock)
        {
            if (!_claims.Add(claim)) return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<MemberBadgeService>()
                    .AwardAsync(guildId, userId, key, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                lock (_lock) _claims.Remove(claim);
                _logger.LogWarning(ex, "Could not award the {Key} badge.", key);
            }
        });
    }

    /// <summary>Adds one to a counter palier's count (arcade, nanny).</summary>
    public void Increment(ulong guildId, ulong userId, string key)
    {
        if (!MayWrite(guildId, userId, key)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<MemberBadgeService>()
                    .IncrementAsync(guildId, userId, key, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not count the {Key} badge.", key);
            }
        });
    }

    private bool MayWrite(ulong guildId, ulong userId, string key) =>
        MemberBadges.IsAwardable(guildId, key) && userId != _client.CurrentUser?.Id;
}
