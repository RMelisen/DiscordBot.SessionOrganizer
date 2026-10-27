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

        _ = Task.Run(() => AnswerAsync(message, guild, card, intent, name, weekday));
        return true;
    }

    // What the card says now: whether it is open, who answered it, its count and its standing in words.
    private sealed record CardState(bool Open, HashSet<ulong> Answered, string Count, string Status, string Title);

    private async Task AnswerAsync(SocketUserMessage message, SocketGuild guild, CardRef card, CardReplyIntent intent,
        string name, string weekday)
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
            _activity.CountAnswer(guild.Id, AnswerKinds.Key(ChatPath.Reply), AnswerKinds.Card(intent), null, DateTimeOffset.UtcNow);

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
                if (answered) return (BotResponses.CardDoneSeenLines, string.Empty);
                return (state.Open ? BotResponses.CardDoneLiarLines : BotResponses.CardClosedLines, string.Empty);

            case CardReplyIntent.TextAnswer:
                if (answered) return (BotResponses.CardTextAnsweredLines, string.Empty);
                return (state.Open ? BotResponses.CardTextClickLines : BotResponses.CardClosedLines, string.Empty);

            case CardReplyIntent.Repost:
                if (!state.Open) return (BotResponses.CardClosedLines, string.Empty);
                return (_repostGate.TryClaim(card) ? BotResponses.CardRepostLines : BotResponses.CardRepostTooSoonLines,
                    string.Empty);

            default:
            {
                if (!state.Open) return (BotResponses.CardClosedLines, string.Empty);
                var tagged = Tagged(message);
                if (tagged.Count == 0) return (BotResponses.CardRallyLines, state.Count);
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
                return new CardState(!poll.IsClosed, voters, count, status, Sanitized(poll.Title));
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
