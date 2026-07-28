using System.Collections.Concurrent;

namespace ProjectSYNCS.Services;

// Picks a line from a response pool while steering away from the ones most recently
// used in the same channel. Pure random picking makes a 20-line pool feel like a
// 5-line one, because back-to-back repeats are what people notice. Kept as a
// singleton so the history is shared across every entry point; like the rest of the
// personality state it lives in memory and resets on restart.
// Public (like AvailabilityService) because the public SpeakModule injects it.
public sealed class ResponsePicker
{
    // Upper bound on the lines remembered per channel. The number actually excluded
    // on a given pick is scaled to the pool (see Pick), so this is only a ceiling.
    private const int HistoryLength = 10;

    // Boxed so one picker can serve pools of plain lines and of richer entries (the
    // presence fillers pair a line with an activity type). Comparison goes through
    // Equals, which is value equality for both strings and tuples.
    private readonly ConcurrentDictionary<ulong, List<object>> _recent = new();

    /// <summary>
    /// A random entry from <paramref name="pool"/>, preferring one that hasn't been
    /// used recently in this bucket. History is per bucket, not per pool, so it is a
    /// rolling record of what the bot last said there. The bucket is normally a
    /// channel id; anything stable works for pools with no channel.
    /// </summary>
    public T Pick<T>(ulong bucketId, T[] pool)
    {
        if (pool.Length == 0) return default!;
        if (pool.Length == 1) return pool[0];

        var history = _recent.GetOrAdd(bucketId, _ => new List<object>());

        lock (history)
        {
            // Never exclude more than half the pool. With a fixed window a small pool
            // (ReferenceComebacks has 5 lines) would have every line excluded and
            // nothing left to say; halving also keeps larger pools feeling random
            // rather than cycling predictably through their lines.
            int window = Math.Min(HistoryLength, pool.Length / 2);
            var excluded = history.Count <= window
                ? history
                : history.GetRange(history.Count - window, window);

            var candidates = pool.Where(entry => !excluded.Contains(entry!)).ToArray();
            // The window cap above guarantees at least one survivor; this only keeps
            // a future change to that formula from producing an empty pick.
            if (candidates.Length == 0) candidates = pool;

            var chosen = candidates[Random.Shared.Next(candidates.Length)];

            history.Add(chosen!);
            if (history.Count > HistoryLength) history.RemoveAt(0);
            return chosen;
        }
    }
}
