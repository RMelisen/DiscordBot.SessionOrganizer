namespace ProjectSYNCS.Helpers;

/// <summary>
/// A set that remembers the last N things added and forgets the oldest beyond that —
/// "have I already seen this?" for a stream of ids that never stops arriving.
/// </summary>
/// <remarks>
/// <para>`BotFeedbackTracker` had three copies of this: the messages she has
/// acknowledged, the replies nobody may pass verdict on, and the (message, user) pairs
/// that have already spent their one reaction. Each was a HashSet, a parallel Queue, a
/// cap, and the same four-line add-and-trim dance.</para>
/// <para><b>Deliberately not thread-safe, unlike <see cref="CooldownGate{TKey}"/>.</b>
/// That one owns its lock because a claim is a complete operation by itself. These are
/// the opposite case: in `SuppressJudgement` the add and a write to `_lastActions` have
/// to happen under one lock, or the gateway echo can interleave between them and
/// re-open an action that was just withdrawn. So the caller keeps holding its own lock
/// and this stays a plain collection — giving it a lock of its own would look safer
/// while quietly breaking that atomicity.</para>
/// <para>Falling off the end is harmless by design in all three uses: the oldest
/// entries are messages old enough that nobody is still reacting to them.</para>
/// </remarks>
public sealed class BoundedSet<T> where T : notnull
{
    private readonly int _capacity;
    private readonly HashSet<T> _items = new();
    private readonly Queue<T> _order = new();

    public BoundedSet(int capacity) => _capacity = capacity;

    /// <summary>
    /// Adds <paramref name="item"/> and returns whether it was new. Adding something
    /// already present changes nothing — it does not refresh its position, so an id
    /// ages out on when it was first seen rather than last.
    /// </summary>
    public bool Add(T item)
    {
        if (!_items.Add(item)) return false;

        _order.Enqueue(item);
        if (_order.Count > _capacity) _items.Remove(_order.Dequeue());

        return true;
    }

    public bool Contains(T item) => _items.Contains(item);
}
