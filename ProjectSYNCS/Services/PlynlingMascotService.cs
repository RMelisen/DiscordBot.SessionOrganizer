using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Binds the bot's own user id as the owner of Ping-Qilin (PlynlingMascot) and makes sure every
// guild it is in has her. Not a loop, and it hooks Ready itself, like ApplicationEmojiService:
// unlike that one it runs on *every* Ready, because it is idempotent (one query per guild) and the
// bind is what the rest of the Plynling code reads — plus JoinedGuild, for a server added later.
//
// The bind happens synchronously inside the handler so it is in place before any command can
// run; the creating is pushed to the background so the gateway handler returns at once.
internal sealed class PlynlingMascotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<PlynlingMascotService> _logger;

    public PlynlingMascotService(DiscordSocketClient client, IServiceProvider services, ILogger<PlynlingMascotService> logger)
    {
        _client = client;
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += OnReadyAsync;
        _client.JoinedGuild += OnJoinedGuildAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        _client.JoinedGuild -= OnJoinedGuildAsync;
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        PlynlingMascot.Bind(_client.CurrentUser.Id);
        var guilds = _client.Guilds.Select(g => g.Id).ToList();
        _ = Task.Run(() => EnsureAsync(guilds));
        return Task.CompletedTask;
    }

    private Task OnJoinedGuildAsync(SocketGuild guild)
    {
        PlynlingMascot.Bind(_client.CurrentUser.Id);
        _ = Task.Run(() => EnsureAsync(new[] { guild.Id }));
        return Task.CompletedTask;
    }

    private async Task EnsureAsync(IEnumerable<ulong> guildIds)
    {
        foreach (var guildId in guildIds)
        {
            // Per guild: one failing write must not leave the others without her.
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var plynlings = scope.ServiceProvider.GetRequiredService<PlynlingService>();
                if (await plynlings.EnsureMascotAsync(guildId, DateTimeOffset.UtcNow) is not null)
                    _logger.LogInformation("Created {Name} in guild {GuildId}.", PlynlingMascot.Name, guildId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not create {Name} in guild {GuildId}.", PlynlingMascot.Name, guildId);
            }
        }
    }
}
