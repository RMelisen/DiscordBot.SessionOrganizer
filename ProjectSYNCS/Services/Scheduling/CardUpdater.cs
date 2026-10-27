using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Services;

/// <summary>
/// Redraws a session or poll card where it lives, from anywhere that isn't a click on the card
/// itself: the « Gérer » panel, the edit modal, the lifecycle pass, auto-close. Never throws: the
/// change is already saved, and a card a step behind is the worst case.
/// </summary>
/// <remarks>
/// A card posted before the V2 cards is an embed. The edit converts it (ApplyTo clears the embed
/// and sets the flag); if Discord refuses that conversion, the card is reposted in the same channel
/// through <see cref="CardRepost"/> instead. Transient, like the DB services it uses.
/// </remarks>
public sealed class CardUpdater
{
    private readonly CardRepost _repost;
    private readonly ILogger<CardUpdater> _logger;

    public CardUpdater(CardRepost repost, ILogger<CardUpdater> logger)
    {
        _repost = repost;
        _logger = logger;
    }

    public async Task RedrawSessionAsync(SocketGuild guild, SessionEvent session)
    {
        if (session.MessageId == 0 || guild.GetTextChannel(session.ChannelId) is not { } channel) return;
        try
        {
            if (await channel.GetMessageAsync(session.MessageId) is not IUserMessage message) return;
            await message.ModifyAsync(p => SessionCards.ApplyTo(p, session, guild));
        }
        catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.BadRequest)
        {
            await RepostAsync(guild, channel, new CardRef(CardKind.Session, session.Id), ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to redraw the card of session {EventId}.", session.Id);
        }
    }

    public async Task RedrawPollAsync(SocketGuild guild, Poll poll, string? sessionLink = null)
    {
        if (poll.MessageId == 0 || guild.GetTextChannel(poll.ChannelId) is not { } channel) return;
        try
        {
            if (await channel.GetMessageAsync(poll.MessageId) is not IUserMessage message) return;
            await message.ModifyAsync(p => PollCards.ApplyTo(p, poll, sessionLink));
        }
        catch (HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.BadRequest && !poll.IsClosed)
        {
            // A closed poll isn't reposted (nothing left to answer); its old card stays as it was.
            await RepostAsync(guild, channel, new CardRef(CardKind.Poll, poll.Id), ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to redraw the card of poll {PollId}.", poll.Id);
        }
    }

    private async Task RepostAsync(SocketGuild guild, IMessageChannel channel, CardRef card, Exception cause)
    {
        _logger.LogWarning(cause, "Card {Card} refused the V2 edit; reposting it.", card);
        try
        {
            await _repost.RepostAsync(guild, channel, card);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to repost card {Card}.", card);
        }
    }
}
