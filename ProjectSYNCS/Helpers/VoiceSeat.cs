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
//
// Papa (AvailabilityService.OwnerId) active in the channel changes the rules — her favouritism:
// two people are enough (him and one more, never him alone), it settles in OwnerSettleTime, and
// it rolls OwnerJoinChance. If he arrives after a lost roll, the gathering rolls once more at his
// chance. Not certain on purpose: a bot that shows up every single time reads as a script.
public sealed class VoiceGathering
{
    public const int MinActive = 3;
    public const int OwnerMinActive = 2;
    public const double JoinChance = 0.66;
    public const double OwnerJoinChance = 0.90;
    public static readonly TimeSpan SettleTime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan OwnerSettleTime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan GraceToEnd = TimeSpan.FromMinutes(5);

    private TimeSpan _settled;
    private TimeSpan _below;
    // His second chance is spent (rolled with him there), or someone threw her out: no more rolls
    // this gathering.
    private bool _ownerRolled;
    private bool _gaveUp;

    /// <summary>The gathering has settled and rolled.</summary>
    public bool Started { get; private set; }

    /// <summary>Started, and the roll seats her.</summary>
    public bool Won { get; private set; }

    /// <summary>When it started, for the "first come" tie-break. Meaningless unless started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>How many active people make a gathering, with or without him.</summary>
    public static int MinActiveFor(bool ownerActive) => ownerActive ? OwnerMinActive : MinActive;

    /// <summary>
    /// Records one sweep. <paramref name="roll"/> is drawn at most twice per gathering (once when it
    /// starts, once more if he arrives after a lost roll), in [0, 1).
    /// </summary>
    public void Observe(int active, bool ownerActive, TimeSpan tick, DateTimeOffset now, Func<double> roll)
    {
        if (active >= MinActiveFor(ownerActive))
        {
            _below = TimeSpan.Zero;
            if (Started)
            {
                if (ownerActive && !Won && !_ownerRolled && !_gaveUp)
                {
                    _ownerRolled = true;
                    Won = roll() < OwnerJoinChance;
                }
                return;
            }

            _settled += tick;
            if (_settled < (ownerActive ? OwnerSettleTime : SettleTime)) return;

            Started = true;
            StartedAt = now;
            _ownerRolled = ownerActive;
            Won = roll() < (ownerActive ? OwnerJoinChance : JoinChance);
            return;
        }

        if (_settled == TimeSpan.Zero) return;

        _below += tick;
        if (_below < GraceToEnd) return;

        _settled = TimeSpan.Zero;
        _below = TimeSpan.Zero;
        _ownerRolled = false;
        _gaveUp = false;
        Started = false;
        Won = false;
    }

    /// <summary>
    /// Someone else decided for her (kicked her out of the call, or the join failed): she stays out
    /// until this gathering ends, his arrival included.
    /// </summary>
    public void GiveUp()
    {
        Won = false;
        _gaveUp = true;
    }

    /// <summary>Someone dragged her into this channel: she stays while its gathering lasts.</summary>
    public void Adopt()
    {
        if (Started) Won = true;
    }
}

public static class VoiceSeat
{
    /// <summary>One channel as the seat chooser sees it.</summary>
    public readonly record struct Candidate(ulong ChannelId, int Active, bool Won, bool SessionLive, DateTimeOffset StartedAt, bool OwnerHere = false);

    /// <summary>
    /// Where she should sit, or null for nowhere. A won channel where Papa is active comes first,
    /// even if it means leaving her seat: she follows him, and only him. Otherwise she never
    /// switches while her channel's gathering still holds her (every switch plays the leave and
    /// join sounds in two channels); failing that, the won channel with the most active people,
    /// then one hosting a live session, then the one that started first.
    /// </summary>
    public static ulong? Choose(ulong? current, IReadOnlyList<Candidate> channels)
    {
        var papa = channels.Where(c => c.Won && c.OwnerHere).Select(c => (ulong?)c.ChannelId).FirstOrDefault();
        if (papa is not null) return papa;

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
