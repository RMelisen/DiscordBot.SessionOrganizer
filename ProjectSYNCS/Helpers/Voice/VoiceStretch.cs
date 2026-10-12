namespace ProjectSYNCS.Helpers;

// One voice channel's current get-together, for the spectator line: how long at least two people
// have been in it together (the same "active" rule as voice XP: not muted, not deafened), and
// whether it has just ended. Pure and Context-free, fed one sweep at a time by
// VoiceSpectatorService, so the rules can be checked without a gateway.
//
// A stretch ends when the channel empties, or when it has had fewer than two active people for
// GapToEnd — someone left muted in a channel after everyone else went to bed must not hold the
// line back all night. A short dip (one person steps away and comes back) doesn't end it.
public sealed class VoiceStretch
{
    // Shorter than this together, the line isn't worth it: a quick call, not a session.
    public static readonly TimeSpan MinTogether = TimeSpan.FromHours(1);
    public static readonly TimeSpan GapToEnd = TimeSpan.FromMinutes(15);

    private TimeSpan _together;
    private TimeSpan _apart;

    /// <summary>
    /// Records one sweep. Returns the time spent together when the stretch has just ended and
    /// lasted <see cref="MinTogether"/> or more; null otherwise (still going, or too short).
    /// </summary>
    public TimeSpan? Observe(int active, int humans, TimeSpan tick)
    {
        if (active >= 2)
        {
            _together += tick;
            _apart = TimeSpan.Zero;
            return null;
        }

        if (_together == TimeSpan.Zero) return null;

        _apart += tick;
        if (humans > 0 && _apart < GapToEnd) return null;

        var ended = _together;
        _together = TimeSpan.Zero;
        _apart = TimeSpan.Zero;
        return ended >= MinTogether ? ended : null;
    }
}
