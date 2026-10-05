namespace ProjectSYNCS.Services;

// Picks a line from a response pool while steering away from the ones she said most
// recently. Pure random picking makes a 20-line pool feel like a 5-line one, because
// back-to-back repeats are what people notice. Kept as a singleton so the memory is
// shared across every entry point; like the rest of the personality state it lives in
// memory and resets on restart (pools spent once a day use Helpers/DailyRotation).
//
// The memory is global, not per channel: the same people read every channel and their
// DMs, so a line said in one place and repeated in another a minute later is still a
// repeat. It records when each line was last said, so a pool used rarely isn't crowded
// out by busy ones (emote reactions fire far more often than any reply), and pools
// built on the fly (concatenations, gendered halves) need no registration — a line is
// the same line whichever array it arrives in.
// Public (like AvailabilityService) because the public DebugModule injects it.
public sealed class ResponsePicker
{
    // The most lines of one pool excluded on a pick; the pool/2 cap usually binds first.
    private const int MaxExcluded = 50;

    // A safety bound on the memory. Every pool is made of constant templates, so the
    // number of distinct lines is fixed and far below this; it only matters if a
    // generated string ever reaches Pick, which would otherwise grow the map forever.
    private const int MaxRemembered = 20_000;

    private readonly object _gate = new();

    // When each line was last said, as a running count of picks.
    private readonly Dictionary<string, long> _lastSaid = new();
    private long _clock;

    /// <summary>
    /// A random line from <paramref name="pool"/>, avoiding the pool's
    /// <c>min(50, pool.Length / 2)</c> most recently said lines, wherever she said them.
    /// </summary>
    public string Pick(string[] pool)
    {
        if (pool.Length == 0) return string.Empty;
        if (pool.Length == 1) return pool[0];

        lock (_gate)
        {
            // Never exclude more than half the pool. With a fixed window a small pool
            // (ReferenceComebacks has 5 lines) would have every line excluded and
            // nothing left to say; halving also keeps larger pools feeling random
            // rather than cycling predictably through their lines.
            int window = Math.Min(MaxExcluded, pool.Length / 2);

            var excluded = pool
                .Distinct()
                .Where(_lastSaid.ContainsKey)
                .OrderByDescending(line => _lastSaid[line])
                .Take(window)
                .ToHashSet();

            var candidates = pool.Where(line => !excluded.Contains(line)).ToArray();
            // The window cap above guarantees at least one survivor; this only keeps
            // a future change to that formula from producing an empty pick.
            if (candidates.Length == 0) candidates = pool;

            var chosen = candidates[Random.Shared.Next(candidates.Length)];

            _lastSaid[chosen] = ++_clock;
            if (_lastSaid.Count > MaxRemembered) ForgetOldestHalf();
            return chosen;
        }
    }

    private void ForgetOldestHalf()
    {
        foreach (var line in _lastSaid.OrderBy(e => e.Value).Take(_lastSaid.Count / 2).Select(e => e.Key).ToList())
            _lastSaid.Remove(line);
    }
}
