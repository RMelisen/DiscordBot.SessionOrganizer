namespace ProjectSYNCS.Helpers;

// Her seat in voice (VoiceSeatService): when a voice channel holds a real get-together, she
// sometimes comes and sits in it, self-muted and self-deafened, like a member who joins to be
// with the others. Pure and Context-free, fed one sweep at a time, so the rules can be checked
// without a gateway.
//
// A gathering starts once a channel has had its tier's MinActive active people (voice XP's rule:
// not muted, not deafened) for a settle time drawn once per gathering, and it rolls the tier's
// chance there and then — not every sweep, which would seat her in every gathering within
// minutes. A won roll holds for the whole gathering; a lost one stays lost unless a better tier
// arrives (Tata, then Papa or a live session), which rolls once more at its chance. The gathering
// ends after a grace drawn per dip under MinActive: a short dip (someone mutes for a minute)
// neither ends it nor makes her leave and come back, each of which plays Discord's sounds.
//
// The draws keep the old averages (settle 10 min, or 5 with Papa; grace 5 min), so she stays as
// long as before on average — only the regularity is gone: a bot that joins on the exact minute
// reads as a script. Same reason Papa gets 90%, not 100%.
public enum SeatTier
{
    Base,
    // Tata (BotResponses.TataId) is active in the channel: family, one notch below Papa.
    Tata,
    // Papa (AvailabilityService.OwnerId) is active, or a session she organised is under way there.
    Favoured,
}

public readonly record struct SeatRules(int MinActive, int SettleMinMinutes, int SettleMaxMinutes, double Chance);

public sealed class VoiceGathering
{
    public static readonly SeatRules BaseRules = new(MinActive: 3, SettleMinMinutes: 8, SettleMaxMinutes: 12, Chance: 0.66);
    public static readonly SeatRules TataRules = new(MinActive: 3, SettleMinMinutes: 8, SettleMaxMinutes: 12, Chance: 0.80);
    // Two people are enough: him (or the session) and one more — never him alone.
    public static readonly SeatRules FavouredRules = new(MinActive: 2, SettleMinMinutes: 3, SettleMaxMinutes: 7, Chance: 0.90);

    public const int GraceMinMinutes = 4;
    public const int GraceMaxMinutes = 6;

    public static SeatRules RulesFor(SeatTier tier) => tier switch
    {
        SeatTier.Favoured => FavouredRules,
        SeatTier.Tata => TataRules,
        _ => BaseRules,
    };

    private TimeSpan _settled;
    private TimeSpan _below;
    // Where this gathering falls in each tier's settle range, drawn once: Papa arriving halfway
    // through moves the target to his range without a second draw.
    private double _settleDraw = -1;
    private TimeSpan _grace = TimeSpan.Zero;
    // The best chance already rolled: a tier only rolls again if it beats it.
    private double _bestRolled;
    // Thrown out for good (a pout, a failed join): no roll brings her back this gathering.
    private bool _gaveUp;
    // She left after Papa: only his return rolls again.
    private bool _released;

    /// <summary>The gathering has settled and rolled.</summary>
    public bool Started { get; private set; }

    /// <summary>Started, and the roll seats her.</summary>
    public bool Won { get; private set; }

    /// <summary>When it started, for the "first come" tie-break. Meaningless unless started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>How many times she was thrown out of this gathering's call.</summary>
    public int Kicks { get; private set; }

    /// <summary>Out for good this gathering (a pout, a refused join).</summary>
    public bool GaveUp => _gaveUp;

    /// <summary>
    /// The roll that started it was lost with a rival bot in the call: she refuses to sit next to
    /// it, and says so once (VoiceSeatService). Cleared when the gathering ends.
    /// </summary>
    public bool RefusedForRival { get; private set; }

    /// <summary>
    /// Records one sweep. <paramref name="tier"/> is the best tier present right now;
    /// <paramref name="ownerActive"/> is Papa himself, apart from a live session. Every random draw
    /// comes from <paramref name="roll"/>, in [0, 1).
    /// </summary>
    public void Observe(int active, SeatTier tier, bool ownerActive, TimeSpan tick, DateTimeOffset now, Func<double> roll,
        bool rivalPresent = false)
    {
        var rules = RulesFor(tier);
        var chance = VoiceSeat.ChanceWith(rules.Chance, tier, rivalPresent);
        if (active >= rules.MinActive)
        {
            _below = TimeSpan.Zero;
            _grace = TimeSpan.Zero;
            if (Started)
            {
                if (Won || _gaveUp) return;
                if (_released)
                {
                    if (!ownerActive) return;
                    _released = false;
                    _bestRolled = 0;
                }
                if (chance <= _bestRolled) return;
                _bestRolled = chance;
                Won = roll() < chance;
                return;
            }

            if (_settleDraw < 0) _settleDraw = roll();
            _settled += tick;
            if (_settled < SettleTarget(rules, _settleDraw)) return;

            Started = true;
            StartedAt = now;
            _bestRolled = chance;
            var draw = roll();
            Won = draw < chance;
            // Only the rival's fault if, without it, the same draw would have seated her.
            RefusedForRival = !Won && draw < rules.Chance;
            return;
        }

        if (_settled == TimeSpan.Zero) return;

        if (_grace == TimeSpan.Zero) _grace = Draw(GraceMinMinutes, GraceMaxMinutes, roll());
        _below += tick;
        if (_below < _grace) return;

        _settled = TimeSpan.Zero;
        _below = TimeSpan.Zero;
        _grace = TimeSpan.Zero;
        _settleDraw = -1;
        _bestRolled = 0;
        _gaveUp = false;
        _released = false;
        Kicks = 0;
        RefusedForRival = false;
        Started = false;
        Won = false;
    }

    /// <summary>
    /// She's out for the rest of this gathering, whoever arrives (a pout, a join Discord refused).
    /// </summary>
    public void GiveUp()
    {
        Won = false;
        _gaveUp = true;
    }

    /// <summary>She left after Papa: out until he comes back to this channel.</summary>
    public void Release()
    {
        if (!Won) return;
        Won = false;
        _released = true;
    }

    /// <summary>Someone threw her out of the call. Returns how many times this gathering.</summary>
    public int RecordKick() => ++Kicks;

    /// <summary>Someone dragged her into this channel: she stays while its gathering lasts.</summary>
    public void Adopt()
    {
        if (Started) Won = true;
    }

    // Whole minutes, because the sweep is: a draw in [min, max] lands on each minute evenly, so
    // the average is the middle of the range.
    private static TimeSpan SettleTarget(SeatRules rules, double draw) =>
        Draw(rules.SettleMinMinutes, rules.SettleMaxMinutes, draw);

    private static TimeSpan Draw(int min, int max, double draw) =>
        TimeSpan.FromMinutes(min + Math.Min(max - min, (int)(draw * (max - min + 1))));
}

public static class VoiceSeat
{
    // Another bot sits in the call (a music bot, the rival): she halves her odds — she won't sit
    // next to it — except for Papa or a session she organised, where she comes anyway.
    public const double RivalChanceFactor = 0.5;

    // A Game or Movie session she organised: she sits in voice ArrivalLead before the start,
    // whatever her odds — the first one there — and holds the seat until HoldUntil past the start
    // if no gathering forms around her.
    public static readonly TimeSpan ArrivalLead = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan HoldUntil = TimeSpan.FromMinutes(15);

    // Once in a while she undeafens and unmutes for a moment, then thinks better of it: the icons
    // flicker, no sound. At most once per seat, MicGagChance of the time, between the bounds
    // after she sat down.
    public const double MicGagChance = 0.20;
    public const int MicGagMinMinutes = 5;
    public const int MicGagMaxMinutes = 40;
    public static readonly TimeSpan MicGagLength = TimeSpan.FromSeconds(2);

    // Sitting with Papa at night: once a night, at a minute drawn in [BedtimeFrom, BedtimeTo)
    // (Paris), she scolds him for not being in bed and leaves; no call at all until morning.
    public static readonly TimeSpan BedtimeFrom = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(15);
    public static readonly TimeSpan BedtimeTo = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(15);
    public static readonly TimeSpan BedtimeLeaveAfter = TimeSpan.FromMinutes(1);

    /// <summary>A tier's chance once a rival bot in the call is accounted for.</summary>
    public static double ChanceWith(double chance, SeatTier tier, bool rivalPresent) =>
        rivalPresent && tier != SeatTier.Favoured ? chance * RivalChanceFactor : chance;

    /// <summary>
    /// Tonight's bedtime for the night that <paramref name="now"/> belongs to (Paris): a minute in
    /// the window, drawn from the date so a restart lands on the same one.
    /// </summary>
    public static DateTimeOffset BedtimeFor(DateTimeOffset now)
    {
        var night = AppTime.ToZoned(now).Date;
        var span = (int)(BedtimeTo - BedtimeFrom).TotalMinutes;
        var minute = (int)(StableRoll.Unit(DateOnly.FromDateTime(night).DayNumber, 0x4245_4454, 0) * span);
        return AppTime.AtWallClock(night + BedtimeFrom + TimeSpan.FromMinutes(minute));
    }

    // Papa leaves the channel she sits in (and isn't somewhere she can follow): half the time she
    // goes too, a minute or three later — she was there for him. The others stay; that's the point.
    public const double LeaveAfterOwnerChance = 0.5;
    public const int LeaveAfterOwnerMinMinutes = 1;
    public const int LeaveAfterOwnerMaxMinutes = 3;

    // Thrown out of a call: the first time, a pout (a line, and no joining for the rest of the
    // Paris day except where Papa is) or anger (a line, and she comes straight back once). A
    // second time in the same gathering is always a pout.
    public const double PoutChance = 0.40;
    public const int RetryMinSeconds = 10;
    public const int RetryMaxSeconds = 20;

    /// <summary>One channel as the seat chooser sees it.</summary>
    /// <param name="Allowed">
    /// Whether she may sit down there now: false at night (Ambient.IsSleepHours) or while she
    /// pouts, unless Papa is there. A seat she already holds isn't affected.
    /// </param>
    public readonly record struct Candidate(
        ulong ChannelId, int Active, bool Won, bool SessionLive, DateTimeOffset StartedAt,
        bool OwnerHere = false, bool Allowed = true);

    /// <summary>
    /// Where she should sit, or null for nowhere. A won channel where Papa is active comes first,
    /// even if it means leaving her seat: she follows him, and only him. Otherwise she never
    /// switches while her channel's gathering still holds her (every switch plays the leave and
    /// join sounds in two channels); failing that, the won channel with the most active people,
    /// then one hosting a live session, then the one that started first.
    /// </summary>
    public static ulong? Choose(ulong? current, IReadOnlyList<Candidate> channels)
    {
        var open = channels.Where(c => c.Won && (c.Allowed || c.ChannelId == current)).ToList();

        var papa = open.Where(c => c.OwnerHere).Select(c => (ulong?)c.ChannelId).FirstOrDefault();
        if (papa is not null) return papa;

        if (current is { } seat && open.Any(c => c.ChannelId == seat))
            return seat;

        return open
            .OrderByDescending(c => c.Active)
            .ThenByDescending(c => c.SessionLive)
            .ThenBy(c => c.StartedAt)
            .ThenBy(c => c.ChannelId)
            .Select(c => (ulong?)c.ChannelId)
            .FirstOrDefault();
    }

    /// <summary>The best tier present in a channel.</summary>
    public static SeatTier TierFor(bool ownerActive, bool sessionLive, bool tataActive) =>
        ownerActive || sessionLive ? SeatTier.Favoured
        : tataActive ? SeatTier.Tata
        : SeatTier.Base;

    /// <summary>A whole number of minutes or seconds in [min, max], from a draw in [0, 1).</summary>
    public static int Between(int min, int max, double draw) => min + Math.Min(max - min, (int)(draw * (max - min + 1)));
}
