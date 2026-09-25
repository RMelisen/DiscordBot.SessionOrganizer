namespace ProjectSYNCS.Helpers;

/// <summary>
/// "Has this key been claimed recently?" — the per-channel / per-person rationing every
/// gateway-driven tracker here does, in one place.
/// </summary>
/// <remarks>
/// <para>This replaced four near-identical implementations: `ReactionService`,
/// `RivalryService`, `ShameTracker` and `XpTracker` each had their own `TryClaim`, and
/// the last three had already generalised far enough to take the dictionary as a
/// parameter — so they differed only in the type of the key. `ShameTracker` and
/// `XpTracker` additionally carried a byte-identical `ForgetStale`.</para>
/// <para><b>This shares the mechanism, not the policy.</b> Every service still owns its
/// own instances and its own durations, which is what keeps the deliberate asymmetries
/// intact: `ReactionService`'s message path is rationed while its pile-on path is not,
/// `RivalryService`'s reaction and mutter gates stay separate so a wordless reaction
/// cannot mute the line, and the shame counters are rationed differently from
/// `Le Malfaisant`'s targeted half on purpose. Never share one instance between two
/// trigger populations to "tidy up" — that is the bug those notes exist to prevent.</para>
/// <para>Self-locking, and deliberately so: in `ShameTracker` and `XpTracker` the old
/// shared `_gate` guarded nothing else at all, and in `ReactionService` and
/// `RivalryService` the claim never read the other state under that lock. Holding a
/// narrower lock also means one gate can never block another, and it removes the
/// service-to-service lock nesting `BotFeedbackTracker` has to be careful about.</para>
/// </remarks>
public sealed class CooldownGate<TKey> where TKey : notnull
{
    // Only swept once the map is big enough to be worth walking; below this the scan
    // would cost more than the entries do.
    private const int SweepThreshold = 256;

    private readonly TimeSpan _cooldown;
    private readonly TimeSpan? _forget;
    private readonly object _lock = new();
    private readonly Dictionary<TKey, DateTimeOffset> _last = new();

    /// <param name="cooldown">How long a claimed key stays claimed.</param>
    /// <param name="forget">
    /// How old an entry must be before it is dropped. Null keeps every key seen, which
    /// is what the two channel-keyed gates did before this existed — fine while the key
    /// space is "channels this bot has spoken in", not fine for anything per-person.
    /// </param>
    public CooldownGate(TimeSpan cooldown, TimeSpan? forget = null)
    {
        _cooldown = cooldown;
        _forget = forget;
    }

    /// <summary>
    /// Claims <paramref name="key"/> if its cooldown has elapsed, and returns whether it
    /// did. A refused call changes nothing — it does not extend the existing cooldown.
    /// </summary>
    public bool TryClaim(TKey key) => TryClaim(key, out _);

    /// <summary>
    /// The same claim, also reporting when <paramref name="key"/> can next be claimed — now
    /// plus the cooldown on success, the end of the running cooldown on a refusal — so a
    /// refusal can say exactly when to come back. One lock, so the answer matches the claim.
    /// </summary>
    public bool TryClaim(TKey key, out DateTimeOffset readyAt)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (_last.TryGetValue(key, out var last) && now - last < _cooldown)
            {
                readyAt = last + _cooldown;
                return false;
            }

            _last[key] = now;
            readyAt = now + _cooldown;
            ForgetStale(now);
            return true;
        }
    }

    /// <summary>
    /// Gives a claim back, so the next call may claim again immediately. For the case
    /// where the work the claim was taken for turned out not to happen.
    /// </summary>
    public void Release(TKey key)
    {
        lock (_lock) _last.Remove(key);
    }

    private void ForgetStale(DateTimeOffset now)
    {
        if (_forget is not { } forget || _last.Count < SweepThreshold) return;

        var cutoff = now - forget;
        foreach (var key in _last.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList())
            _last.Remove(key);
    }
}
