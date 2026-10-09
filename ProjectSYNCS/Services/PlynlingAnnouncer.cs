using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Everything Plynlings say *outside* a command reply: public announcements in the game
// channel and DMs to owners. A singleton with no database access. Every Discord call is
// swallowed and logged — a missing permission or closed DMs must never break a flow.
public sealed class PlynlingAnnouncer
{
    // Where deaths and resurrections are announced unless the guild configured its own
    // game channel (/config game-channel). A server-specific id, listed in CLAUDE.md's
    // "Hardcoded ids" beside the others.
    public const ulong DefaultGameChannelId = 1555578872488267846;

    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly ResponsePicker _picker;
    private readonly ILogger<PlynlingAnnouncer> _logger;

    public PlynlingAnnouncer(DiscordSocketClient client, GuildConfigService config, ResponsePicker picker,
        ILogger<PlynlingAnnouncer> logger)
    {
        _client = client;
        _config = config;
        _picker = picker;
        _logger = logger;
    }

    /// <summary>
    /// This guild's game channel: the configured one, else the default when it lives in
    /// this guild. Null when there is neither (an unconfigured dev guild), or when the
    /// channel is gone. The guild check matters: the default is one fixed id, so without
    /// it a Plynling from any other guild would be announced on the home server.
    /// </summary>
    public async Task<IMessageChannel?> ResolveGameChannelAsync(ulong guildId)
    {
        var configured = (await _config.GetAsync(guildId)).GameChannelId;
        var id = configured != 0 ? configured : DefaultGameChannelId;
        return _client.GetChannel(id) is IMessageChannel channel
               && channel is IGuildChannel home && home.GuildId == guildId
            ? channel
            : null;
    }

    public Task AnnounceDeathAsync(Plynling plynling, DateTimeOffset now)
    {
        var lived = PlynlingLife.Age(plynling, now);
        var tier = PlynlingCatalog.MemorialTier(lived);
        var pool = plynling.DeathCause == DeathCause.Illness ? BotResponses.PlynlingIllnessDeathLines : BotResponses.PlynlingDeathLines;
        var line = string.Format(_picker.Pick(pool.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>",
            LevelCardUi.Duration((long)lived.TotalMinutes), PlynlingCatalog.MemorialName(tier));
        return PostAsync(plynling.GuildId, line, PlynlingArt.Memorial(plynling.Species, tier), "death");
    }

    public Task AnnounceResurrectionAsync(Plynling plynling, DateTimeOffset now)
    {
        var line = string.Format(_picker.Pick(BotResponses.PlynlingResurrectLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>");
        return PostAsync(plynling.GuildId, line, PlynlingArt.SpriteOf(plynling, now), "resurrection");
    }

    // The shame of /plynling abandon, with its sad picture. The row is already gone; the
    // object still carries everything the line and the picture need.
    public Task AnnounceAbandonAsync(Plynling plynling, DateTimeOffset now)
    {
        var line = string.Format(_picker.Pick(BotResponses.PlynlingAbandonLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name), $"<@{plynling.OwnerId}>");
        return PostAsync(plynling.GuildId, line,
            PlynlingArt.SpriteOf(plynling, now, PlynlingMood.Sad), "abandon");
    }

    // An event's story. In the game channel's guild it goes there; elsewhere (the dev guild) it goes to
    // `fallback`, the channel the choice was made in, when there is one — a sweep resolution there is
    // logged and dropped, like every other announcement.
    // The stories of these resolved events, each posted as PostEventStoryAsync does. Takes the caller's
    // PlynlingService: the announcer is a singleton and never holds a database service itself.
    public async Task TellAsync(PlynlingService plynlings, IEnumerable<int> instanceIds, ulong guildId, DateTimeOffset now,
        IMessageChannel? fallback = null)
    {
        foreach (var id in instanceIds)
            if (await plynlings.GetEventStoryAsync(id, now) is { } story)
                await PostEventStoryAsync(guildId, PlynlingEventCards.BuildStory(story, 0), fallback);
    }

    public async Task PostEventStoryAsync(ulong guildId, MessageComponent story, IMessageChannel? fallback = null)
    {
        try
        {
            var channel = await ResolveGameChannelAsync(guildId) ?? fallback;
            if (channel is null)
            {
                _logger.LogInformation("Event story in guild {GuildId} not posted: no game channel there.", guildId);
                return;
            }
            await channel.SendMessageAsync(components: story, flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post an event story.");
        }
    }

    // Called only once PlynlingLife.ShouldWarn has seen death coming; the DM says so without
    // naming when.
    public Task WarnOwnerAsync(Plynling plynling)
    {
        var line = string.Format(_picker.Pick(BotResponses.PlynlingWarningLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name));
        return DmOwnerAsync(plynling.OwnerId, line);
    }

    // Once per illness, from the sweep: it fell sick this morning, and what to do about it.
    public Task WarnSickAsync(Plynling plynling)
    {
        var line = string.Format(_picker.Pick(BotResponses.PlynlingSickWarningLines.For(plynling.Gender)),
            PlynlingCardUi.SafeName(plynling.Name));
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

    private async Task PostAsync(ulong guildId, string line, string imageUrl, string what)
    {
        try
        {
            if (await ResolveGameChannelAsync(guildId) is not { } channel)
            {
                _logger.LogInformation("Plynling {What} in guild {GuildId} not announced: no game channel there.",
                    what, guildId);
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
