using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>One open /wouldyourather: two options, hidden votes, a deadline.</summary>
public sealed class WyrRound
{
    public int Id { get; init; }
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    // Set once the card is up; until then a click can't reach it anyway.
    public ulong MessageId { get; set; }
    public string A { get; init; } = string.Empty;
    public string B { get; init; } = string.Empty;
    public string Intro { get; init; } = string.Empty;
    public DateTimeOffset EndsAt { get; init; }
    // Who chose what: 0 for A, 1 for B. Changeable until the end. Guarded by the service's lock.
    public Dictionary<ulong, int> Votes { get; } = new();
    // Every edit of the card goes through here, so a count redraw can never land after the reveal.
    public SemaphoreSlim Edits { get; } = new(1, 1);
    // Set under Edits once the reveal starts: later count redraws skip.
    public bool Closed { get; set; }
}

/// <summary>
/// The open /wouldyourather rounds, and the timer that closes each one. Singleton: the votes live
/// here, in memory, so a restart drops open rounds by design (their buttons then answer « expiré »).
/// The close is a fire-and-forget task that catches everything, like ArcadeRunner's timers, and
/// reaches the card through the client — no database involved.
/// </summary>
public sealed class WouldYouRatherService
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly object _lock = new();
    private readonly Dictionary<int, WyrRound> _rounds = new();
    private int _nextId;

    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<WouldYouRatherService> _logger;

    public WouldYouRatherService(DiscordSocketClient client, ResponsePicker picker, ILogger<WouldYouRatherService> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    public WyrRound Open(ulong guildId, ulong channelId, string a, string b, string intro)
    {
        lock (_lock)
        {
            var round = new WyrRound
            {
                Id = ++_nextId, GuildId = guildId, ChannelId = channelId, A = a, B = b, Intro = intro,
                EndsAt = DateTimeOffset.UtcNow + Window,
            };
            _rounds[round.Id] = round;
            return round;
        }
    }

    // The card never went up: forget the round.
    public void Drop(int id)
    {
        lock (_lock) _rounds.Remove(id);
    }

    /// <summary>
    /// Records a vote. Null when the round is gone (closed, or lost to a restart) or the click
    /// came from another message — ids restart at 1 after a restart, so an old card's button could
    /// otherwise vote in a new round.
    /// </summary>
    public WyrRound? Vote(int id, ulong messageId, ulong userId, int side)
    {
        lock (_lock)
        {
            if (!_rounds.TryGetValue(id, out var round) || round.MessageId != messageId) return null;
            round.Votes[userId] = side;
            return round;
        }
    }

    /// <summary>Redraws the open card's vote count. Skipped once the reveal has started.</summary>
    public async Task RefreshCountAsync(WyrRound round, IUserMessage message)
    {
        await round.Edits.WaitAsync();
        try
        {
            if (round.Closed) return;
            int total;
            lock (_lock) total = round.Votes.Count;
            await message.ModifyAsync(p => p.Embed = WouldYouRatherModule.Card(round, total));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update the would-you-rather count on {MessageId}.", message.Id);
        }
        finally
        {
            round.Edits.Release();
        }
    }

    public void ScheduleClose(WyrRound round) => _ = Task.Run(async () =>
    {
        try
        {
            var wait = round.EndsAt - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait);
            await CloseAsync(round.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Would-you-rather {RoundId} failed to close.", round.Id);
        }
    });

    private async Task CloseAsync(int id)
    {
        WyrRound? round;
        Dictionary<ulong, int> votes;
        lock (_lock)
        {
            if (!_rounds.Remove(id, out round)) return;
            votes = new Dictionary<ulong, int>(round.Votes);
        }

        var forA = votes.Where(v => v.Value == 0).Select(v => v.Key).ToList();
        var forB = votes.Where(v => v.Value == 1).Select(v => v.Key).ToList();
        var comment = _picker.Pick(CommentPool(forA.Count, forB.Count));
        var herPick = string.Format(_picker.Pick(BotResponses.WyrHerPickLines), HerPick(round));

        var embed = WouldYouRatherModule.ResultCard(round, forA, forB, comment, herPick);
        var buttons = WouldYouRatherModule.Buttons(round.Id, disabled: true);

        // A Discord side effect never breaks anything: a card deleted in the meantime is just logged.
        await round.Edits.WaitAsync();
        try
        {
            round.Closed = true;
            if (_client.GetChannel(round.ChannelId) is not IMessageChannel channel) return;
            if (await channel.GetMessageAsync(round.MessageId) is not IUserMessage message) return;
            await message.ModifyAsync(p =>
            {
                p.Embed = embed;
                p.Components = buttons;
                p.AllowedMentions = AllowedMentions.None;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reveal would-you-rather {RoundId}.", id);
        }
        finally
        {
            round.Edits.Release();
        }
    }

    private static string[] CommentPool(int a, int b)
    {
        var (min, max) = (Math.Min(a, b), Math.Max(a, b));
        if (max == 0) return BotResponses.WyrNobodyLines;
        if (a == b) return BotResponses.WyrTieLines;
        if (min == 0) return BotResponses.WyrUnanimousLines;
        if (max >= 3 * min) return BotResponses.WyrLandslideLines;
        return BotResponses.WyrCloseLines;
    }

    // Her own choice: stable for the same dilemma, so asking the same thing twice gets the same
    // answer from her, whatever the room voted.
    private static string HerPick(WyrRound round)
    {
        var a = StableRoll.Unit(Rating.Hash($"{round.A}\u0001{round.B}"), 9201, 0) < 0.5;
        return a ? $"🅰️ « {round.A} »" : $"🅱️ « {round.B} »";
    }
}
