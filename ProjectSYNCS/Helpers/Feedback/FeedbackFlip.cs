using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Spots someone changing their mind about her within a minute: « good bot » then « bad bot »
/// (or the other way round). Pure and in memory, so it can be checked without a gateway.
/// </summary>
/// <remarks>
/// Owns its lock. Every call drops the entries older than <see cref="Window"/>, so the memory
/// stays bounded by the people who judged her in the last minute.
/// </remarks>
public sealed class FeedbackFlip
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    private readonly object _lock = new();
    private readonly Dictionary<ulong, (FeedbackKind Verdict, DateTimeOffset At)> _last = new();

    /// <summary>
    /// Records a verdict. True when this person's previous recorded verdict was the opposite
    /// one and less than <see cref="Window"/> ago.
    /// </summary>
    public bool Record(ulong userId, FeedbackKind verdict, DateTimeOffset now)
    {
        if (verdict is not (FeedbackKind.Good or FeedbackKind.Bad)) return false;

        lock (_lock)
        {
            foreach (var stale in _last.Where(e => now - e.Value.At >= Window).Select(e => e.Key).ToList())
                _last.Remove(stale);

            bool flipped = _last.TryGetValue(userId, out var previous) && previous.Verdict != verdict;
            _last[userId] = (verdict, now);
            return flipped;
        }
    }
}
