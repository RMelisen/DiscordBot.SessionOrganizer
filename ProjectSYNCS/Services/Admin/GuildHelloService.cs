using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her hello when she is added to a server (JoinedGuild): one card (GuildHelloCards) saying what to
// set up, in the server's system channel if she may post there, else the first text channel she may.
// Not a loop; it hooks JoinedGuild itself, like PlynlingMascotService. The work runs off the gateway
// handler, and a failure is logged, never thrown (a Discord side effect never breaks the flow).
//
// Not silence-gated: a server she has just joined can't have been silenced, and the card is a
// notice about setting her up, not chatter. Never on the home server (she was always there).
internal sealed class GuildHelloService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<GuildHelloService> _logger;

    public GuildHelloService(DiscordSocketClient client, ResponsePicker picker, ILogger<GuildHelloService> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.JoinedGuild += OnJoinedGuildAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.JoinedGuild -= OnJoinedGuildAsync;
        return Task.CompletedTask;
    }

    private Task OnJoinedGuildAsync(SocketGuild guild)
    {
        if (HomeGuild.Is(guild.Id)) return Task.CompletedTask;
        _ = Task.Run(() => HelloAsync(guild));
        return Task.CompletedTask;
    }

    private async Task HelloAsync(SocketGuild guild)
    {
        try
        {
            if (Destination(guild) is not { } channel)
            {
                _logger.LogWarning("Joined guild {GuildId} but found no channel to say hello in.", guild.Id);
                return;
            }

            await channel.SendMessageAsync(
                components: GuildHelloCards.Build(_picker.Pick(BotResponses.GuildHelloLines)),
                flags: MessageFlags.ComponentsV2,
                allowedMentions: AllowedMentions.None);
            _logger.LogInformation("Joined guild {GuildId}; hello posted in channel {ChannelId}.", guild.Id, channel.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to say hello in guild {GuildId}.", guild.Id);
        }
    }

    // The system channel first (where Discord itself greets), else the first text channel in the
    // list where she can see, write and send a card.
    private static ITextChannel? Destination(SocketGuild guild)
    {
        bool CanPost(SocketTextChannel channel) =>
            guild.CurrentUser is { } me && me.GetPermissions(channel) is var p
            && p.ViewChannel && p.SendMessages && p.EmbedLinks;

        if (guild.SystemChannel is { } system && system is not SocketVoiceChannel && CanPost(system)) return system;
        return guild.TextChannels
            .Where(c => c is not SocketVoiceChannel && c is not SocketThreadChannel)
            .OrderBy(c => c.Position)
            .FirstOrDefault(CanPost);
    }
}
