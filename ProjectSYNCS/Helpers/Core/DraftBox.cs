using System.Collections.Concurrent;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a wizard holds for someone between two clicks, keyed by their user id, forgotten after
/// <c>ttl</c> without a touch. Pruned on every access, so a wizard closed by walking away doesn't
/// stay in memory until the next restart.
/// </summary>
/// <remarks>
/// <see cref="TryTake"/> is the claim a « Terminer » takes first: of two clicks, one gets the draft
/// and publishes, the other finds nothing. A failed publish puts it back with <see cref="Set"/>.
/// </remarks>
public sealed class DraftBox<TValue>
{
    private readonly ConcurrentDictionary<ulong, (TValue Value, DateTimeOffset At)> _items = new();
    private readonly TimeSpan _ttl;
    private readonly Func<DateTimeOffset> _clock;

    public DraftBox(TimeSpan ttl, Func<DateTimeOffset>? clock = null)
    {
        _ttl = ttl;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public void Set(ulong userId, TValue value)
    {
        Prune();
        _items[userId] = (value, _clock());
    }

    /// <summary>The draft, if still fresh; touching it restarts its clock.</summary>
    public bool TryGet(ulong userId, out TValue value)
    {
        Prune();
        if (_items.TryGetValue(userId, out var item))
        {
            _items[userId] = (item.Value, _clock());
            value = item.Value;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>Removes and returns the draft: only one caller ever gets it.</summary>
    public bool TryTake(ulong userId, out TValue value)
    {
        Prune();
        if (_items.TryRemove(userId, out var item))
        {
            value = item.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public void Remove(ulong userId) => _items.TryRemove(userId, out _);

    public int Count
    {
        get { Prune(); return _items.Count; }
    }

    private void Prune()
    {
        var cutoff = _clock() - _ttl;
        foreach (var (key, item) in _items)
            if (item.At < cutoff) _items.TryRemove(key, out _);
    }
}
