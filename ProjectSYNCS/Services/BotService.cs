using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ProjectSYNCS.Services;

// Hosts the Discord connection: logs in, registers slash commands, wires gateway
// events to the collaborators that handle them, and dispatches interactions.
// The actual behaviour lives in ChatterService (personality) and EmoteTracker
// (emote stats).
internal sealed class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<BotService> _logger;
    private readonly ChatterService _chatter;
    private readonly EmoteTracker _emotes;

    public BotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IConfiguration config,
        ILogger<BotService> logger,
        ChatterService chatter,
        EmoteTracker emotes)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _config = config;
        _logger = logger;
        _chatter = chatter;
        _emotes = emotes;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _interactions.Log += LogAsync;

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += HandleInteractionAsync;
        _client.MessageReceived += HandleMessageAsync;
        _client.ReactionAdded += _emotes.HandleReactionAddedAsync;
        _client.ReactionRemoved += _emotes.HandleReactionRemovedAsync;
        _client.Ready += RegisterCommandsAsync;

        var token = _config["Discord:Token"]
            ?? throw new InvalidOperationException("Discord:Token is not configured.");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task RegisterCommandsAsync()
    {
        bool global = _config.GetValue<bool>("Discord:RegisterCommandsGlobally");

        if (global)
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Registered slash commands globally.");
        }
        else
        {
            ulong guildId = _config.GetValue<ulong>("Discord:DevelopmentGuildId");
            await _interactions.RegisterCommandsToGuildAsync(guildId, deleteMissing: true);
            _logger.LogInformation("Registered slash commands to guild {GuildId}.", guildId);
        }
    }

    // Fans a received message out to the emote tracker and the personality logic.
    private async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        await _emotes.HandleMessageAsync(rawMessage);
        await _chatter.HandleMessageAsync(rawMessage);
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        var result = await _interactions.ExecuteCommandAsync(ctx, _services);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Interaction failed: {Error} — {Reason}",
                result.Error, result.ErrorReason);
        }
    }

    private Task LogAsync(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };
        _logger.Log(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}
