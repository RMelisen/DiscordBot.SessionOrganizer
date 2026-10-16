namespace ProjectSYNCS.Helpers;

/// <summary>
/// How long someone has been going back and forth with her in one channel, and whether she has
/// ended that conversation. ChatterService records every reply and mention aimed at her; a gap of
/// more than <see cref="Gap"/> starts the count again. At <see cref="NudgeAt"/> she remarks on it,
/// at <see cref="EndAt"/> she ends it herself and only reacts for <see cref="Closed"/>.
/// </summary>
/// <remarks>
/// In memory only, reset on restart by design. Self-locking like <see cref="CooldownGate{TKey}"/>:
/// nothing else is read under its lock. The clock is passed in so the rules can be checked without
/// waiting.
/// </remarks>
public sealed class ConversationStreak
{
    public static readonly TimeSpan Gap = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan Closed = TimeSpan.FromMinutes(10);
    public const int NudgeAt = 3;
    public const int EndAt = 5;

    // Only swept once the map is big enough to be worth walking, as CooldownGate does.
    private const int SweepThreshold = 128;

    private readonly object _lock = new();
    private readonly Dictionary<(ulong Channel, ulong User), Entry> _entries = new();

    private sealed class Entry
    {
        public int Count;
        public DateTimeOffset Last;
        public DateTimeOffset? ClosedUntil;
    }

    /// <summary>
    /// Counts one more message from this person in this channel and returns the running count:
    /// 1 for the first, or again after a pause longer than <see cref="Gap"/>.
    /// </summary>
    public int Record(ulong channelId, ulong userId, DateTimeOffset now)
    {
        lock (_lock)
        {
            Sweep(now);
            var key = (channelId, userId);
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry();
                _entries[key] = entry;
            }

            entry.Count = entry.Count > 0 && now - entry.Last <= Gap ? entry.Count + 1 : 1;
            entry.Last = now;
            return entry.Count;
        }
    }

    /// <summary>She ended the conversation: closed for <see cref="Closed"/>, and the count restarts after.</summary>
    public void Close(ulong channelId, ulong userId, DateTimeOffset now)
    {
        lock (_lock)
        {
            var key = (channelId, userId);
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry { Last = now };
                _entries[key] = entry;
            }

            entry.ClosedUntil = now + Closed;
            entry.Count = 0;
        }
    }

    /// <summary>Whether she ended this conversation less than <see cref="Closed"/> ago.</summary>
    public bool IsClosed(ulong channelId, ulong userId, DateTimeOffset now)
    {
        lock (_lock)
            return _entries.TryGetValue((channelId, userId), out var entry)
                   && entry.ClosedUntil is { } until && now < until;
    }

    private void Sweep(DateTimeOffset now)
    {
        if (_entries.Count < SweepThreshold) return;

        foreach (var key in _entries
                     .Where(kv => now - kv.Value.Last > Gap && (kv.Value.ClosedUntil is null || kv.Value.ClosedUntil <= now))
                     .Select(kv => kv.Key).ToList())
            _entries.Remove(key);
    }
}
