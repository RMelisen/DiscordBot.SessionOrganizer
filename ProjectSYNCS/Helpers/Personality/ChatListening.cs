namespace ProjectSYNCS.Helpers;

/// <summary>
/// Who she is still listening to, per (channel, person), for messages that neither tag her, reply to
/// her nor say her name: the Discord twin of her Minecraft body's call and answered windows
/// (docs/agents/minecraft-orders.md, *An order can come in two steps*).
/// <list type="bullet">
/// <item><b>Called</b>: they only called her (« syncs ? », a bare tag). Their next message within
/// <see cref="Window"/> is read as said to her, whatever it is. One message only.</item>
/// <item><b>Answered</b>: she just answered them. Within <see cref="Window"/> of her last answer, what
/// they say is heard only when it plainly asks her something (a specific intent), since they may be
/// talking to someone else.</item>
/// </list>
/// </summary>
/// <remarks>
/// In memory only, reset on restart by design. Self-locking like <see cref="ConversationStreak"/>: the
/// clock is passed in so the rules can be checked without waiting. The streak still counts every turn,
/// so a follow-up conversation ends on its fifth message like any other.
/// </remarks>
public sealed class ChatListening
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    // Only swept once the map is big enough to be worth walking, as ConversationStreak does.
    private const int SweepThreshold = 128;

    private readonly object _lock = new();
    private readonly Dictionary<(ulong Channel, ulong User), Entry> _entries = new();

    private sealed class Entry
    {
        public DateTimeOffset? CalledAt;
        public DateTimeOffset? AnsweredAt;
    }

    /// <summary>They called her and nothing else: their next message is for her.</summary>
    public void Called(ulong channelId, ulong userId, DateTimeOffset now) => Update(channelId, userId, now, e => e.CalledAt = now);

    /// <summary>She answered them: what they ask her next is heard without her name.</summary>
    public void Answered(ulong channelId, ulong userId, DateTimeOffset now) => Update(channelId, userId, now, e => e.AnsweredAt = now);

    /// <summary>
    /// Whether this message from them follows a call or an answer of hers, within <see cref="Window"/>.
    /// Takes the call: it covers one message, whatever that message turns out to be.
    /// </summary>
    public (bool Called, bool Answered) Take(ulong channelId, ulong userId, DateTimeOffset now)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue((channelId, userId), out var entry)) return (false, false);
            var called = entry.CalledAt is { } c && now - c <= Window;
            var answered = entry.AnsweredAt is { } a && now - a <= Window;
            entry.CalledAt = null;
            if (!answered) _entries.Remove((channelId, userId));
            return (called, answered);
        }
    }

    private void Update(ulong channelId, ulong userId, DateTimeOffset now, Action<Entry> set)
    {
        lock (_lock)
        {
            Sweep(now);
            if (!_entries.TryGetValue((channelId, userId), out var entry))
            {
                entry = new Entry();
                _entries[(channelId, userId)] = entry;
            }
            set(entry);
        }
    }

    private void Sweep(DateTimeOffset now)
    {
        if (_entries.Count < SweepThreshold) return;
        static DateTimeOffset Latest(Entry e) =>
            (e.CalledAt ?? DateTimeOffset.MinValue) > (e.AnsweredAt ?? DateTimeOffset.MinValue)
                ? e.CalledAt!.Value
                : e.AnsweredAt ?? DateTimeOffset.MinValue;
        foreach (var key in _entries.Where(kv => now - Latest(kv.Value) > Window).Select(kv => kv.Key).ToList())
            _entries.Remove(key);
    }
}
