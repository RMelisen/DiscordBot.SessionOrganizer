namespace ProjectSYNCS.Helpers;

/// <summary>
/// One <see cref="SemaphoreSlim"/> per key, made on first use and dropped once nobody holds or
/// waits for it, so a long-running bot doesn't keep one per row it ever touched. In-process only:
/// the bot is a single process, which is all a « check → write → reload » on one row needs.
/// </summary>
/// <remarks>Owns no policy: each singleton holding one decides what its keys are.</remarks>
public sealed class KeyedLock<TKey> where TKey : notnull
{
    // Long enough for a slow Pi write, short enough that a stuck holder can't hang a click forever.
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly Dictionary<TKey, Entry> _entries = new();

    private sealed class Entry
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        // Holders and waiters; the entry goes when it drops to zero.
        public int Users;
    }

    /// <summary>
    /// Waits for <paramref name="key"/>'s turn; dispose the result to let the next one in.
    /// Throws <see cref="TimeoutException"/> after ten seconds rather than waiting forever.
    /// </summary>
    public async Task<IDisposable> AcquireAsync(TKey key)
    {
        Entry entry;
        lock (_entries)
        {
            if (!_entries.TryGetValue(key, out entry!)) _entries[key] = entry = new Entry();
            entry.Users++;
        }

        bool entered;
        try
        {
            entered = await entry.Gate.WaitAsync(Wait);
        }
        catch
        {
            Leave(key, entry, held: false);
            throw;
        }
        if (!entered)
        {
            Leave(key, entry, held: false);
            throw new TimeoutException($"Lock for {key} not free after {Wait.TotalSeconds} s.");
        }
        return new Releaser(this, key, entry);
    }

    /// <summary>How many keys are held or waited on now (the checks look at pruning).</summary>
    public int Count
    {
        get { lock (_entries) return _entries.Count; }
    }

    private void Leave(TKey key, Entry entry, bool held)
    {
        if (held) entry.Gate.Release();
        lock (_entries)
        {
            if (--entry.Users == 0) _entries.Remove(key);
        }
    }

    private sealed class Releaser : IDisposable
    {
        private readonly KeyedLock<TKey> _owner;
        private readonly TKey _key;
        private Entry? _entry;

        public Releaser(KeyedLock<TKey> owner, TKey key, Entry entry)
        {
            _owner = owner;
            _key = key;
            _entry = entry;
        }

        public void Dispose()
        {
            // Twice is harmless: the second one has nothing left to release.
            if (Interlocked.Exchange(ref _entry, null) is { } entry) _owner.Leave(_key, entry, held: true);
        }
    }
}
