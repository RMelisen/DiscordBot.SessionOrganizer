using System.Collections.Concurrent;
using Discord;

namespace ProjectSYNCS.Services;

// What one of her own messages was, so a reply to it gets an answer about *that* message rather
// than a roast picked at random (ChatterService; BotResponses.ReplyContextLines has one pool per
// kind). In memory only, never stored: free to reorder.
public enum ReplyContext
{
    Welcome,
    MorningHello,
    SessionCall,
    SessionRecap,
    WeekendPoll,
    LevelUp,
    Lonely,
    Wake,
    CommandResult,
}

/// <summary>
/// The kind of the messages she posted on her own initiative lately, by message id. Each sender
/// remembers what it just said; ChatterService looks up the message someone replied to.
/// </summary>
/// <remarks>
/// <para>Singleton, in memory, bounded: a restart forgets it by design, and a reply to a forgotten
/// message falls back to the ordinary reply path. Two kinds are never remembered here, because
/// they can be recognised without it: the morning hello by its text (MorningGreetingService
/// .IsMorningHello, which also survives a restart), and any slash-command result by its
/// interaction metadata (ReplyContext.CommandResult).</para>
/// <para>NightLines are not here: AmbientService answers a reply to the 3 a.m. line itself, before
/// ChatterService sees it.</para>
/// </remarks>
public sealed class HerMessages
{
    // A day of her own posts is a few dozen; this keeps several days without growing.
    private const int MaxTracked = 500;

    private readonly ConcurrentDictionary<ulong, ReplyContext> _kinds = new();
    private readonly ConcurrentQueue<ulong> _order = new();

    /// <summary>Remembers what <paramref name="sent"/> was. Null-safe: a failed send records nothing.</summary>
    public void Remember(IUserMessage? sent, ReplyContext kind)
    {
        if (sent is null) return;

        _kinds[sent.Id] = kind;
        _order.Enqueue(sent.Id);

        while (_order.Count > MaxTracked && _order.TryDequeue(out var oldest))
            _kinds.TryRemove(oldest, out _);
    }

    public bool TryGet(ulong messageId, out ReplyContext kind) => _kinds.TryGetValue(messageId, out kind);
}
