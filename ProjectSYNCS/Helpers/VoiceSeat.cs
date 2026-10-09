namespace ProjectSYNCS.Helpers;

// Her seat in voice (VoiceSeatService): when a voice channel holds a real get-together, she
// sometimes comes and sits in it, self-muted and self-deafened, like a member who joins to be
// with the others. Pure and Context-free, fed one sweep at a time, so the rules can be checked
// without a gateway.
//
// A gathering starts once a channel has had MinActive active people (voice XP's rule: not
// muted, not deafened) for SettleTime, and it rolls JoinChance once, there and then — not every
// sweep, which would seat her in every gathering within minutes. A won roll holds for the
// whole gathering; a lost one stays lost until the gathering ends and a new one starts. The
// gathering ends after GraceToEnd under MinActive: a short dip (someone mutes for a minute)
// neither ends it nor makes her leave and come back, each of which plays Discord's sounds.
public sealed class VoiceGathering
{
    public const int MinActive = 3;
    public const double JoinChance = 0.30;
    public static readonly TimeSpan SettleTime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan GraceToEnd = TimeSpan.FromMinutes(5);

    private TimeSpan _settled;
    private TimeSpan _below;

    /// <summary>The gathering has settled and rolled.</summary>
    public bool Started { get; private set; }

    /// <summary>Started, and the roll seats her.</summary>
    public bool Won { get; private set; }

    /// <summary>When it started, for the "first come" tie-break. Meaningless unless started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    /// Records one sweep. <paramref name="roll"/> is drawn at most once per gathering, in [0, 1).
    /// </summary>
    public void Observe(int active, TimeSpan tick, DateTimeOffset now, Func<double> roll)
    {
        if (active >= MinActive)
        {
            _below = TimeSpan.Zero;
            if (Started) return;

            _settled += tick;
            if (_settled < SettleTime) return;

            Started = true;
            StartedAt = now;
            Won = roll() < JoinChance;
            return;
        }

        if (_settled == TimeSpan.Zero) return;

        _below += tick;
        if (_below < GraceToEnd) return;

        _settled = TimeSpan.Zero;
        _below = TimeSpan.Zero;
        Started = false;
        Won = false;
    }

    /// <summary>
    /// Someone else decided for her (kicked her out of the call, or the join failed): she stays out
    /// until this gathering ends.
    /// </summary>
    public void GiveUp() => Won = false;

    /// <summary>Someone dragged her into this channel: she stays while its gathering lasts.</summary>
    public void Adopt()
    {
        if (Started) Won = true;
    }
}

public static class VoiceSeat
{
    /// <summary>One channel as the seat chooser sees it.</summary>
    public readonly record struct Candidate(ulong ChannelId, int Active, bool Won, bool SessionLive, DateTimeOffset StartedAt);

    /// <summary>
    /// Where she should sit, or null for nowhere. She never switches while her channel's gathering
    /// still holds her (every switch plays the leave and join sounds in two channels); otherwise
    /// the won channel with the most active people, then one hosting a live session, then the one
    /// that started first.
    /// </summary>
    public static ulong? Choose(ulong? current, IReadOnlyList<Candidate> channels)
    {
        if (current is { } seat && channels.Any(c => c.ChannelId == seat && c.Won))
            return seat;

        return channels
            .Where(c => c.Won)
            .OrderByDescending(c => c.Active)
            .ThenByDescending(c => c.SessionLive)
            .ThenBy(c => c.StartedAt)
            .ThenBy(c => c.ChannelId)
            .Select(c => (ulong?)c.ChannelId)
            .FirstOrDefault();
    }
}
