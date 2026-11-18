using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>
/// Answers a reply under one of her cards (a poll, a vote, a session, /tonight, a giveaway) that
/// <see cref="MessageCues.ReadCardReply"/> read: she backs up whoever rallies the others, checks a
/// « j'ai voté » against the card, says where it stands, sends typed answers to the buttons, and
/// brings the card back down when asked (<see cref="CardRepost"/>). ChatterService calls it from
/// the reply path; everything that reads the database runs off the gateway handler.
/// </summary>
/// <remarks>Singleton: its two gates are in memory. One rally line per card at a time (several
/// people urging the room get one backing, not a chorus), one repost per card at a time.</remarks>
internal sealed class CardReplyService
{
    private static readonly TimeSpan RallyCooldown = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RepostCooldown = TimeSpan.FromMinutes(5);

    // How many tagged people a rally line names before it stops listing.
    private const int MaxTagged = 10;

    // Titles and tags are shown, never pinged; the person answered still gets the reply ping.
    private static readonly AllowedMentions Muted = new(AllowedMentionTypes.None) { MentionRepliedUser = true };

    private readonly CooldownGate<CardRef> _rallyGate = new(RallyCooldown, forget: RallyCooldown);
    private readonly CooldownGate<CardRef> _repostGate = new(RepostCooldown, forget: RepostCooldown);

    private readonly DiscordSocketClient _client;
    private readonly GlitchService _glitch;
    private readonly ResponsePicker _picker;
    private readonly IServiceProvider _services;
    private readonly ActivityCounter _activity;
    private readonly ILogger<CardReplyService> _logger;

    public CardReplyService(DiscordSocketClient client, GlitchService glitch, ResponsePicker picker,
        IServiceProvider services, ActivityCounter activity, ILogger<CardReplyService> logger)
    {
        _activity = activity;
        _client = client;
        _glitch = glitch;
        _picker = picker;
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Starts the answer and returns true when the reply is hers to take: an intent, in a guild. A
    /// rally while the card's last one is still fresh is taken and left unanswered.
    /// </summary>
    public bool TryAnswer(SocketUserMessage message, CardRef card, CardReplyIntent intent, string name, string weekday)
    {
        if (intent == CardReplyIntent.None || message.Channel is not SocketGuildChannel { Guild: var guild }) return false;

        if (intent == CardReplyIntent.Rally && !_rallyGate.TryClaim(card))
        {
            _logger.LogInformation("{Name} rallied the others under card {Card} — she already backed someone up.", name, card);
            return true;
        }

        _ = Task.Run(() => AnswerAsync(message, guild, card, intent, name, weekday, ChatPath.Reply));
        return true;
    }

    /// <summary>
    /// « syncs remonte le sondage », with no card replied to (<see cref="MessageCues.ReadNamedRepost"/>):
    /// she brings back the newest open card of that kind in this channel, as if the ask had been a
    /// reply under it, or says there is none here. « remonte tous les sondages » brings back every
    /// open one, oldest first, so they land in the order they were posted. Only this channel: moving
    /// the weekend poll out of the main channel because someone asked elsewhere is the `/… list` menus' job.
    /// </summary>
    public bool TryRepostNamed(SocketUserMessage message, NamedRepost named, string name, string weekday, ChatPath path)
    {
        if (named.Card == NamedCard.None || message.Channel is not SocketGuildChannel { Guild: var guild }) return false;
        _ = Task.Run(() => RepostNamedAsync(message, guild, named, name, weekday, path));
        return true;
    }

    private async Task RepostNamedAsync(SocketUserMessage message, SocketGuild guild, NamedRepost named, string name,
        string weekday, ChatPath path)
    {
        try
        {
            List<CardRef> cards;
            await using (var scope = _services.CreateAsyncScope())
            {
                cards = await FindOpenAsync(scope.ServiceProvider, guild.Id, message.Channel.Id, named.Card);
                if (named.All && cards.Count > 1)
                {
                    // A session already running is not one to bring back; the single ask says so, this one skips it.
                    var open = new List<CardRef>();
                    foreach (var card in cards)
                        if (await LoadAsync(scope.ServiceProvider, guild, card) is { Open: true })
                            open.Add(card);
                    // None still open: the newest answers « c'est fermé », as asked alone.
                    cards = open.Count > 0 ? open : [cards[^1]];
                }
            }

            if (cards.Count == 0)
            {
                _logger.LogInformation("{Name} asked her to bring back a card ({Named}) — none open here.", name, named);
                await ReplyAsync(message, guild, BotResponses.CardRepostNoneLines, name, weekday, string.Empty, path,
                    "named repost, nothing here");
                return;
            }

            // One card, or the newest: the same answer as a reply under it.
            if (!named.All || cards.Count == 1)
            {
                await AnswerAsync(message, guild, cards[^1], CardReplyIntent.Repost, name, weekday, path);
                return;
            }

            await RepostAllAsync(message, guild, cards, named, name, weekday, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not answer a named repost ({Named}).", named);
        }
    }

    // Every card the gate lets through, oldest first, after one line for all of them.
    private async Task RepostAllAsync(SocketUserMessage message, SocketGuild guild, List<CardRef> cards, NamedRepost named,
        string name, string weekday, ChatPath path)
    {
        var claimed = cards.Where(_repostGate.TryClaim).ToList();
        if (claimed.Count == 0)
        {
            await ReplyAsync(message, guild, BotResponses.CardRepostTooSoonLines, name, weekday, string.Empty, path,
                "repost all, too soon");
            return;
        }

        _logger.LogInformation("{Name} asked her to bring back every card ({Named}) — {Count} of {Open}.", name, named,
            claimed.Count, cards.Count);
        await ReplyAsync(message, guild, BotResponses.CardRepostAllLines, name, weekday, claimed.Count.ToString(), path,
            "repost all");

        await using var scope = _services.CreateAsyncScope();
        var repost = scope.ServiceProvider.GetRequiredService<CardRepost>();
        foreach (var card in claimed)
        {
            try
            {
                await repost.RepostAsync(guild, message.Channel, card);
            }
            catch (Exception ex)
            {
                // Not brought back, so the next ask may try again; the others still go.
                _repostGate.Release(card);
                _logger.LogWarning(ex, "Could not bring back card {Card} with the others.", card);
            }
        }
    }

    private async Task ReplyAsync(SocketUserMessage message, SocketGuild guild, string[] pool, string name, string weekday,
        string a, ChatPath path, string what)
    {
        var line = string.Format(_picker.Pick(pool), name, weekday, a);
        await _glitch.ReplyWithTypingAsync(message, line, _logger, what, Muted);
        _activity.CountAnswer(guild.Id, AnswerKinds.Key(path), AnswerKinds.Card(CardReplyIntent.Repost), null,
            DateTimeOffset.UtcNow);
    }

    // The open cards of that kind in this channel, oldest first by message id (snowflakes grow with
    // time, so the last one is the newest across kinds). Open as LoadAsync reads it for polls; a
    // session is listed while active, and LoadAsync says whether it still takes answers.
    private static async Task<List<CardRef>> FindOpenAsync(IServiceProvider sp, ulong guildId, ulong channelId, NamedCard named)
    {
        var now = DateTimeOffset.UtcNow;
        var found = new List<(ulong MessageId, CardRef Card)>();
        bool Here(ulong channel, ulong messageId) => channel == channelId && messageId != 0;

        if (named is NamedCard.Any or NamedCard.Poll)
        {
            var polls = sp.GetRequiredService<PollService>();
            foreach (var kind in Enum.GetValues<PollKind>())
                foreach (var poll in await polls.GetActivePollsAsync(guildId, kind))
                    if (Here(poll.ChannelId, poll.MessageId) && PollRules.IsOpen(poll, now))
                        found.Add((poll.MessageId, new CardRef(CardKind.Poll, poll.Id)));
        }
        if (named is NamedCard.Any or NamedCard.Session)
        {
            foreach (var session in await sp.GetRequiredService<EventService>().GetActiveEventsAsync(guildId))
                if (Here(session.ChannelId, session.MessageId))
                    found.Add((session.MessageId, new CardRef(CardKind.Session, session.Id)));
            if (await sp.GetRequiredService<TonightService>().GetOpenAsync(guildId) is { SessionEventId: 0 } call
                && Here(call.ChannelId, call.MessageId))
                found.Add((call.MessageId, new CardRef(CardKind.Tonight, call.Id)));
        }
        if (named is NamedCard.Any or NamedCard.Giveaway)
        {
            foreach (var giveaway in await sp.GetRequiredService<GiveawayService>().GetActiveAsync(guildId))
                if (Here(giveaway.ChannelId, giveaway.MessageId))
                    found.Add((giveaway.MessageId, new CardRef(CardKind.Giveaway, giveaway.Id)));
        }

        return found.OrderBy(f => f.MessageId).Select(f => f.Card).Distinct().ToList();
    }

    // What the card says now: whether it is open, who answered it, its count and its standing in words.
    // Anonymous: an anonymous poll, where she never says who voted, under the card or in chat.
    private sealed record CardState(bool Open, HashSet<ulong> Answered, string Count, string Status, string Title,
        bool Anonymous = false);

    private async Task AnswerAsync(SocketUserMessage message, SocketGuild guild, CardRef card, CardReplyIntent intent,
        string name, string weekday, ChatPath path)
    {
        var what = $"card reply ({intent}, {card.Kind} #{card.Id})";
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            if (await LoadAsync(sp, guild, card) is not { } state)
            {
                _logger.LogInformation("{Name} replied under card {Card}, which no longer exists.", name, card);
                return;
            }

            var (pool, a) = Choose(message, intent, state, card);
            var line = string.Format(_picker.Pick(pool), name, weekday, a, state.Title);
            var repost = pool == BotResponses.CardRepostLines;

            _logger.LogInformation("{Name} replied under card {Card} — {Intent}.", name, card, intent);
            await _glitch.ReplyWithTypingAsync(message, line, _logger, what, Muted);
            // In memory (ActivityCounter): a count can never cost the answer, nor the repost.
            _activity.CountAnswer(guild.Id, AnswerKinds.Key(path), AnswerKinds.Card(intent), null, DateTimeOffset.UtcNow);

            if (!repost) return;
            try
            {
                // Her line first, then the card: it is the last thing in the channel, as asked.
                await sp.GetRequiredService<CardRepost>().RepostAsync(guild, message.Channel, card);
            }
            catch
            {
                // Not brought back, so the next ask may try again.
                _repostGate.Release(card);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not answer a {Intent} reply under card {Card}.", intent, card);
        }
    }

    // The pool and its {2}.
    private (string[] Pool, string A) Choose(SocketUserMessage message, CardReplyIntent intent, CardState state, CardRef card)
    {
        var answered = state.Answered.Contains(message.Author.Id);
        switch (intent)
        {
            case CardReplyIntent.Status:
                return (BotResponses.CardStatusLines, state.Status);

            case CardReplyIntent.Done:
                if (state.Anonymous) return (BotResponses.CardDoneAnonymousLines, string.Empty);
                if (answered) return (BotResponses.CardDoneSeenLines, string.Empty);
                return (state.Open ? BotResponses.CardDoneLiarLines : BotResponses.CardClosedLines, string.Empty);

            case CardReplyIntent.TextAnswer:
                // « tu as déjà cliqué » would tell the room who voted.
                if (answered && !state.Anonymous) return (BotResponses.CardTextAnsweredLines, string.Empty);
                return (state.Open ? BotResponses.CardTextClickLines : BotResponses.CardClosedLines, string.Empty);

            case CardReplyIntent.Repost:
                if (!state.Open) return (BotResponses.CardClosedLines, string.Empty);
                return (_repostGate.TryClaim(card) ? BotResponses.CardRepostLines : BotResponses.CardRepostTooSoonLines,
                    string.Empty);

            default:
            {
                if (!state.Open) return (BotResponses.CardClosedLines, string.Empty);
                var tagged = Tagged(message);
                // Naming who hasn't voted would unmask an anonymous poll: the room gets the count.
                if (tagged.Count == 0 || state.Anonymous) return (BotResponses.CardRallyLines, state.Count);
                var missing = tagged.Where(id => !state.Answered.Contains(id)).ToList();
                return missing.Count > 0
                    ? (BotResponses.CardRallyNamedLines, Tags(missing))
                    : (BotResponses.CardRallyAllDoneLines, Tags(tagged));
            }
        }
    }

    // The humans the reply tags: not her (Discord adds her for the reply ping), not its author.
    private List<ulong> Tagged(SocketUserMessage message) =>
        message.MentionedUsers
            .Where(u => !u.IsBot && u.Id != message.Author.Id && u.Id != _client.CurrentUser.Id)
            .Select(u => u.Id)
            .Distinct()
            .Take(MaxTagged)
            .ToList();

    private static string Tags(IReadOnlyList<ulong> ids) => Join(ids.Select(id => $"<@{id}>").ToList());

    // « A », « A et B », « A, B et C ».
    private static string Join(IReadOnlyList<string> items) => items.Count switch
    {
        0 => string.Empty,
        1 => items[0],
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} et {items[^1]}",
    };

    // « 0 votant », « 1 votant », « 3 votants »: French counts zero in the singular.
    private static string Count(int n, string one, string many) => $"{n} {(n > 1 ? many : one)}";

    private static string Sanitized(string text) => Format.Sanitize(text);

    private static async Task<CardState?> LoadAsync(IServiceProvider sp, SocketGuild guild, CardRef card)
    {
        var now = DateTimeOffset.UtcNow;
        switch (card.Kind)
        {
            case CardKind.Poll:
            {
                var poll = await sp.GetRequiredService<PollService>().GetPollWithVotesAsync(card.Id);
                if (poll is null || poll.GuildId != guild.Id) return null;
                var voters = poll.Options.SelectMany(o => o.Votes).Select(v => v.UserId).ToHashSet();
                var count = Count(voters.Count, "votant", "votants");
                var status = voters.Count == 0 ? "personne n'a encore voté" : $"{count}, {PollLeaders(poll)}";
                if (poll.IsClosed) status += ", et c'est clôturé";
                return new CardState(PollRules.IsOpen(poll, now), voters, count, status, Sanitized(poll.Title), poll.Anonymous);
            }
            case CardKind.Session:
            {
                var session = await sp.GetRequiredService<EventService>().GetEventWithParticipantsAsync(card.Id);
                if (session is null || session.GuildId != guild.Id) return null;
                var joined = session.Participants
                    .Where(p => p.Status == ParticipantStatus.Joined)
                    .OrderBy(p => p.JoinedAt)
                    .Select(p => Sanitized(guild.GetUser(p.UserId) is { } user ? BotResponses.DisplayNameFor(user) : p.Username))
                    .ToList();
                var maybes = session.Participants.Count(p => p.Status == ParticipantStatus.Maybe);
                var unix = session.ScheduledAt.ToUnixTimeSeconds();
                var phase = session.PhaseAt(now);
                var status = session.IsCancelled
                    ? "la session est annulée"
                    : $"<t:{unix}:F> (<t:{unix}:R>), {SessionRoster.Describe(joined, maybes)}"
                      + (phase == SessionPhase.InProgress ? ", et c'est en cours" : phase == SessionPhase.Finished ? ", et c'est terminé" : string.Empty);
                return new CardState(!session.IsCancelled && phase == SessionPhase.Scheduled,
                    session.Participants.Select(p => p.UserId).ToHashSet(),
                    Count(joined.Count, "inscription", "inscriptions"), status, Sanitized(session.Title));
            }
            case CardKind.Giveaway:
            {
                var giveaway = await sp.GetRequiredService<GiveawayService>().GetAsync(card.Id);
                if (giveaway is null || giveaway.GuildId != guild.Id) return null;
                var count = Count(giveaway.Entries.Count, "participation", "participations");
                var winners = giveaway.Entries.Where(e => e.IsWinner).Select(e => $"<@{e.UserId}>").ToList();
                var status = !giveaway.IsClosed
                    ? $"{count}, tirage <t:{giveaway.EndsAt.ToUnixTimeSeconds()}:R>"
                    : winners.Count == 0 ? "tiré, et personne n'avait participé" : $"tiré : {Join(winners)}";
                return new CardState(!giveaway.IsClosed, giveaway.Entries.Select(e => e.UserId).ToHashSet(), count, status,
                    Sanitized(giveaway.Prize));
            }
            default:
            {
                var call = await sp.GetRequiredService<TonightService>().GetAsync(card.Id);
                if (call is null || call.GuildId != guild.Id) return null;
                var open = !call.Closed && call.ExpiresAt > now;
                var inCount = TonightModule.CountIn(call);
                var maybes = call.Responses.Count(r => r.Status == TonightStatus.Maybe);
                var count = $"{Count(inCount, "chaud", "chauds")} sur {call.Minimum}";
                var ts = call.StartsAt.ToUnixTimeSeconds();
                var status = call.SessionEventId != 0 ? "c'est devenu une vraie session"
                    : !open ? "c'est fermé pour ce soir"
                    : $"{count}{(maybes > 0 ? $", {maybes} « peut-être »" : string.Empty)}, rendez-vous <t:{ts}:t> (<t:{ts}:R>)";
                var title = string.IsNullOrEmpty(call.Game) ? "ce soir" : Sanitized(call.Game);
                return new CardState(open, call.Responses.Select(r => r.UserId).ToHashSet(), count, status, title);
            }
        }
    }

    // « en tête : <date> avec 3 voix », or a tie of up to three.
    private static string PollLeaders(Models.Poll poll)
    {
        var best = poll.Options.Max(o => o.Votes.Count);
        var leaders = poll.Options
            .Where(o => o.Votes.Count == best)
            .OrderBy(o => o.ScheduledAt).ThenBy(o => o.Id)
            .Take(3)
            .Select(o => poll.Kind == PollKind.DateSlots
                ? $"<t:{o.ScheduledAt.ToUnixTimeSeconds()}:F>"
                : $"« {Sanitized(o.Label)} »")
            .ToList();
        var votes = Count(best, "voix", "voix");
        return leaders.Count == 1
            ? $"en tête : {leaders[0]} avec {votes}"
            : $"égalité en tête entre {Join(leaders)}, {votes} chacun";
    }
}
