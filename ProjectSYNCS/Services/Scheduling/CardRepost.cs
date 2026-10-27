using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

public enum RepostResult
{
    Reposted,
    NotFound,
    // Closed, cancelled, drawn or expired: nothing left to answer, so nothing to bring back.
    Closed,
}

/// <summary>
/// Brings a card back to the bottom of a channel: the old message is deleted, the card is redrawn
/// through its module's static builders, and the row learns where it lives now. The one path for
/// every card, shared by the `/… list` republish menus and a reply asking her to « remonter » it
/// (CardReplyService). Transient, like the DB services it wraps.
/// </summary>
public sealed class CardRepost
{
    private readonly PollService _polls;
    private readonly EventService _events;
    private readonly GiveawayService _giveaways;
    private readonly TonightService _tonight;
    private readonly ILogger<CardRepost> _logger;

    public CardRepost(PollService polls, EventService events, GiveawayService giveaways, TonightService tonight,
        ILogger<CardRepost> logger)
    {
        _polls = polls;
        _events = events;
        _giveaways = giveaways;
        _tonight = tonight;
        _logger = logger;
    }

    public async Task<RepostResult> RepostAsync(SocketGuild guild, IMessageChannel target, CardRef card)
    {
        switch (card.Kind)
        {
            case CardKind.Poll:
            {
                var poll = await _polls.GetPollWithVotesAsync(card.Id);
                if (poll is null || poll.GuildId != guild.Id) return RepostResult.NotFound;
                if (poll.IsClosed) return RepostResult.Closed;

                await DeleteOldAsync(guild, poll.ChannelId, poll.MessageId);
                var message = await PollCards.SendAsync(target, poll);
                await _polls.SetMessageLocationAsync(poll.Id, target.Id, message.Id);
                return RepostResult.Reposted;
            }
            case CardKind.Session:
            {
                var session = await _events.GetEventWithParticipantsAsync(card.Id);
                if (session is null || session.GuildId != guild.Id) return RepostResult.NotFound;
                if (session.IsCancelled) return RepostResult.Closed;

                await DeleteOldAsync(guild, session.ChannelId, session.MessageId);
                var message = await SessionCards.SendAsync(target, session, guild);
                await _events.SetMessageLocationAsync(session.Id, target.Id, message.Id);

                // The card moved, so the native event's "détails" link must point at it.
                session.ChannelId = target.Id;
                session.MessageId = message.Id;
                await SessionEventSync.UpdateAsync(guild, session, _logger);
                return RepostResult.Reposted;
            }
            case CardKind.Giveaway:
            {
                var giveaway = await _giveaways.GetAsync(card.Id);
                if (giveaway is null || giveaway.GuildId != guild.Id) return RepostResult.NotFound;
                if (giveaway.IsClosed) return RepostResult.Closed;

                await DeleteOldAsync(guild, giveaway.ChannelId, giveaway.MessageId);
                var message = await target.SendMessageAsync(
                    embed: GiveawayModule.BuildEmbed(giveaway), components: GiveawayModule.BuildComponents(giveaway));
                await _giveaways.SetMessageLocationAsync(giveaway.Id, target.Id, message.Id);
                return RepostResult.Reposted;
            }
            default:
            {
                var call = await _tonight.GetAsync(card.Id);
                if (call is null || call.GuildId != guild.Id) return RepostResult.NotFound;
                if (call.Closed || call.ExpiresAt <= DateTimeOffset.UtcNow) return RepostResult.Closed;

                await DeleteOldAsync(guild, call.ChannelId, call.MessageId);
                var message = await target.SendMessageAsync(
                    embed: TonightModule.BuildEmbed(call), components: TonightModule.BuildComponents(call),
                    allowedMentions: AllowedMentions.None);
                await _tonight.SetMessageLocationAsync(call.Id, target.Id, message.Id);
                return RepostResult.Reposted;
            }
        }
    }

    // Removes the previous card if it still exists, so one row never has two live cards.
    private async Task DeleteOldAsync(SocketGuild guild, ulong channelId, ulong messageId)
    {
        if (messageId == 0 || guild.GetTextChannel(channelId) is not { } channel) return;
        try
        {
            if (await channel.GetMessageAsync(messageId) is { } old) await old.DeleteAsync();
        }
        catch (Exception ex)
        {
            // Already gone, or no Manage Messages there: the new card goes out regardless.
            _logger.LogDebug(ex, "Could not delete the old card {MessageId}.", messageId);
        }
    }
}
