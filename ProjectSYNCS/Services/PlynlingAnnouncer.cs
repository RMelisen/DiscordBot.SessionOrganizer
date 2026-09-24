using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Everything Plynlings say *outside* a command reply: public announcements in the game
// channel and DMs to owners. A singleton with no database access. Every Discord call is
// swallowed and logged — a missing permission or closed DMs must never break a flow.
public sealed class PlynlingAnnouncer
{
    // Where deaths and resurrections are announced. A server-specific id, listed in
    // CLAUDE.md's "Hardcoded ids" beside the others.
    public const ulong GameChannelId = 878305034432045080;

    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<PlynlingAnnouncer> _logger;

    public PlynlingAnnouncer(DiscordSocketClient client, ResponsePicker picker, ILogger<PlynlingAnnouncer> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    public Task AnnounceDeathAsync(Plynling plynling, DateTimeOffset now)
    {
        var lived = PlynlingLife.Age(plynling, now);
        var tier = PlynlingCatalog.MemorialTier(lived);
        var line = string.Format(_picker.Pick(GameChannelId, BotResponses.PlynlingDeathLines),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>",
            LevelCardUi.Duration((long)lived.TotalMinutes), PlynlingCatalog.MemorialName(tier));
        return PostAsync(line, PlynlingArt.Memorial(plynling.Species, tier), "death");
    }

    public Task AnnounceResurrectionAsync(Plynling plynling, DateTimeOffset now)
    {
        var line = string.Format(_picker.Pick(GameChannelId, BotResponses.PlynlingResurrectLines),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>");
        return PostAsync(line, PlynlingArt.Sprite(plynling.Species, PlynlingLife.Mood(plynling, now)), "resurrection");
    }

    public Task WarnOwnerAsync(Plynling plynling)
    {
        var death = PlynlingLife.DeathAt(plynling);
        if (death is null) return Task.CompletedTask;
        var line = string.Format(_picker.Pick(plynling.OwnerId, BotResponses.PlynlingWarningLines),
            PlynlingCardUi.SafeName(plynling.Name), $"<t:{death.Value.ToUnixTimeSeconds()}:R>");
        return DmOwnerAsync(plynling.OwnerId, line);
    }

    public async Task DmOwnerAsync(ulong ownerId, string line)
    {
        try
        {
            var user = await _client.GetUserAsync(ownerId);
            if (user is null) return;
            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync(line, allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            // Closed DMs, or no longer sharing a server — the same case reminder DMs handle.
            _logger.LogInformation("User {UserId} does not accept DMs; Plynling notice skipped.", ownerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to DM user {UserId} about their Plynling.", ownerId);
        }
    }

    private async Task PostAsync(string line, string imageUrl, string what)
    {
        try
        {
            if (_client.GetChannel(GameChannelId) is not IMessageChannel channel)
            {
                _logger.LogWarning("Game channel {ChannelId} not found; Plynling {What} not announced.", GameChannelId, what);
                return;
            }

            var components = new ComponentBuilderV2()
                .AddComponent(new ContainerBuilder()
                    .AddComponent(new SectionBuilder()
                        .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(imageUrl)))
                        .AddComponent(new TextDisplayBuilder(line))))
                .Build();
            await channel.SendMessageAsync(components: components, flags: MessageFlags.ComponentsV2,
                allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the Plynling {What} announcement.", what);
        }
    }
}
