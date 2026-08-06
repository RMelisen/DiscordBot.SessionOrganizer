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
    private readonly ReactionService _reactions;
    private readonly BotFeedbackTracker _feedback;
    private readonly RivalryService _rivalry;

    public BotService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IConfiguration config,
        ILogger<BotService> logger,
        ChatterService chatter,
        EmoteTracker emotes,
        ReactionService reactions,
        BotFeedbackTracker feedback,
        RivalryService rivalry)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _config = config;
        _logger = logger;
        _chatter = chatter;
        _emotes = emotes;
        _reactions = reactions;
        _feedback = feedback;
        _rivalry = rivalry;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += LogAsync;
        _interactions.Log += LogAsync;

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += HandleInteractionAsync;
        _client.MessageReceived += HandleMessageAsync;
        _client.ReactionAdded += HandleReactionAddedAsync;
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
    // BotFeedbackTracker is the only one that cares about the bot's *own* messages —
    // it watches them to know when she last acted — so it must stay in the fan-out
    // even though the other three ignore anything she wrote. A message carrying a
    // "good bot" / "bad bot" verdict belongs to it alone; the other two skip those
    // themselves rather than being routed around here.
    private async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        await _emotes.HandleMessageAsync(rawMessage);
        // Before the feedback tracker: a rival's message has to be on record as the
        // most recent action before any verdict arrives that it might have earned.
        await _rivalry.HandleMessageAsync(rawMessage);
        await _feedback.HandleMessageAsync(rawMessage);
        await _reactions.HandleMessageAsync(rawMessage);
        await _chatter.HandleMessageAsync(rawMessage);
    }

    // Counting the reaction comes first, so the tally reflects the human who added it
    // before the bot decides whether to pile on with the same emote. The feedback
    // tracker comes last and looks only at the bot's own reactions, which is how a
    // reaction counts as "something she did" for a later "good bot".
    private async Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        await _emotes.HandleReactionAddedAsync(message, channel, reaction);
        await _reactions.HandleReactionAddedAsync(message, channel, reaction);
        await _feedback.HandleReactionAddedAsync(message, channel, reaction);
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
