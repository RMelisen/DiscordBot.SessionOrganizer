using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Draws the giveaways whose time is up: picks the winners, closes the card, and
// announces the result.
//
// Its own 1-minute tick, explicitly not ReminderService's 5 minutes. Those five are
// load-bearing there (the reminder window is 25–35 minutes precisely so no session is
// missed or reminded twice), and a giveaway drawn up to five minutes after it said it
// would end reads as broken — a 10-minute giveaway would be half again as long. Same
// shape and the same reasoning as VoiceXpService's own interval.
//
// Nothing is kept in memory: EndsAt is stored, so a restart simply resumes, and
// anything that came due while the bot was down is drawn on the next pass.
internal sealed class GiveawayDrawService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<GiveawayDrawService> _logger;

    public GiveawayDrawService(
        IServiceProvider services,
        DiscordSocketClient client,
        ResponsePicker picker,
        ILogger<GiveawayDrawService> logger)
    {
        _services = services;
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GiveawayDrawService started. Checking every {Interval} minute(s).",
            CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            // Sweeping while disconnected would read a stale gateway cache and fail
            // every edit; the giveaway keeps until the next pass.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await SweepAsync();
        }
    }

    private async Task SweepAsync()
    {
        // Singletons never hold a DB service: a scope per pass, like ReminderService.
        await using var scope = _services.CreateAsyncScope();
        var giveaways = scope.ServiceProvider.GetRequiredService<GiveawayService>();

        List<Giveaway> due;
        try
        {
            due = await giveaways.GetDueAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query giveaways due for a draw.");
            return;
        }

        foreach (var giveaway in due)
        {
            try
            {
                await DrawAsync(giveaways, giveaway);
            }
            catch (Exception ex)
            {
                // One broken giveaway must not stop the others from being drawn.
                _logger.LogError(ex, "Failed to draw giveaway {GiveawayId}.", giveaway.Id);
            }
        }
    }

    private async Task DrawAsync(GiveawayService giveaways, Giveaway giveaway)
    {
        // Decides and records in one call. Returns null if it was already drawn, which
        // is what makes the whole pass safe to repeat after a crash.
        var winners = await giveaways.TryDrawAsync(giveaway.Id);
        if (winners is null) return;

        _logger.LogInformation(
            "Giveaway {GiveawayId} drawn: {Count} winner(s) from {Entries} entries.",
            giveaway.Id, winners.Count, giveaway.Entries.Count);

        // Re-read so the card renders the entries as they were actually stored, with
        // IsWinner set — rather than the copy fetched before the draw.
        var drawn = await giveaways.GetAsync(giveaway.Id);
        if (drawn is null) return;

        var channel = _client.GetGuild(drawn.GuildId)?.GetTextChannel(drawn.ChannelId);
        if (channel is null)
        {
            // The channel is gone. The draw stands — it is recorded — and there is
            // simply nowhere left to say so.
            _logger.LogWarning("Giveaway {GiveawayId} drawn, but its channel is gone.", drawn.Id);
            return;
        }

        await UpdateCardAsync(channel, drawn);
        await AnnounceAsync(channel, drawn, winners);
    }

    private async Task UpdateCardAsync(SocketTextChannel channel, Giveaway giveaway)
    {
        if (giveaway.MessageId == 0) return;

        try
        {
            if (await channel.GetMessageAsync(giveaway.MessageId) is not IUserMessage message) return;

            await message.ModifyAsync(props =>
            {
                props.Embed = GiveawayModule.BuildEmbed(giveaway);
                props.Components = GiveawayModule.BuildComponents(giveaway);
            });
        }
        catch (Exception ex)
        {
            // A card that can't be edited is not worth losing the announcement over.
            _logger.LogWarning(ex, "Failed to close the card for giveaway {GiveawayId}.", giveaway.Id);
        }
    }

    private async Task AnnounceAsync(SocketTextChannel channel, Giveaway giveaway, List<ulong> winners)
    {
        var line = winners.Count == 0
            ? string.Format(
                _picker.Pick(channel.Id, BotResponses.GiveawayEmptyLines), giveaway.Prize)
            : string.Format(
                _picker.Pick(channel.Id, BotResponses.GiveawayDrawLines),
                string.Join(", ", winners.Select(id => $"<@{id}>")),
                giveaway.Prize);

        // The one line in the project that is *meant* to ping — winners should know.
        // Users only, never roles or @everyone, the same restriction every relay uses.
        await BotChat.PostWithTypingAsync(
            channel, line, _logger, "giveaway draw",
            new AllowedMentions(AllowedMentionTypes.Users));
    }
}
