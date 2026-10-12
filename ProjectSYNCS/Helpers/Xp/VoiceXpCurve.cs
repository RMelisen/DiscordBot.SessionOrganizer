namespace ProjectSYNCS.Helpers;

// What a minute in voice is worth, given how much has already been earned *today*.
//
// Pure and dependency-free like LevelCurve, and for the same reason: the payout policy
// is arithmetic and should be reasoned about — and checked — without a gateway or a
// database anywhere near it.
//
// The rate falls as the day's total grows. A real session pays well; sitting in a
// channel for eight hours pays almost nothing. This is the anti-abuse measure for the
// one case no mute rule can catch — two accounts idling unmuted, where the sweep can
// see presence but not participation. It is deliberately a taper rather than a hard
// cap: a cliff would just teach people the exact number of minutes to park for.
//
// **TotalForMinutes is the single source of truth.** The per-minute rate is defined as
// its difference, never the other way round, which is what keeps everything exact in
// integer arithmetic and makes the invariants hold by construction rather than by
// agreement: spans are additive, a tick covering several minutes pays exactly what
// those minutes pay one at a time, and RateAt can never drift from what is granted.
//
// The day is the Europe/Paris day, since the figure it reads is MemberDailyStat's
// bucket, keyed by AppTime.DayKey — so the rate resets at local midnight.
public static class VoiceXpCurve
{
    /// <summary>
    /// The taper, as (cumulative minute the tier ends at, XP per minute inside it).
    /// </summary>
    /// <remarks>
    /// The first hour is deliberately flat at the original pre-taper rate, so a normal
    /// session is worth exactly what it always was and only the marathon is cut. After
    /// that the rate steps down every half hour rather than falling off a cliff: the
    /// sharpest drop is 10 → 8, where a two-tier curve dropped 10 → 3 at a single
    /// minute and made hour two feel like a punishment.
    ///
    /// Must stay sorted by <c>UpToMinute</c> ascending with non-increasing
    /// <c>XpPerMinute</c>; the probe in the scratchpad asserts both. Anything past the
    /// last tier pays <see cref="TrickleRate"/>.
    /// </remarks>
    private static readonly (long UpToMinute, long XpPerMinute)[] Tiers =
    {
        ( 60, 10),  // first hour — unchanged from the flat rate that preceded the taper
        ( 90,  8),
        (120,  6),
        (150,  5),
        (180,  4),
        (210,  3),
        (240,  2),
    };

    /// <summary>What every minute past the last tier is worth.</summary>
    /// <remarks>
    /// Deliberately non-zero: the moment it reaches zero this stops being a taper and
    /// becomes a hard cap, which is the thing worth avoiding — a cap tells people the
    /// exact number of minutes to park for.
    /// </remarks>
    public const long TrickleRate = 1;

    /// <summary>The rate paid during the first tier, before any taper applies.</summary>
    public static long FullRate => Tiers[0].XpPerMinute;

    /// <summary>Minutes paid at the full rate each day.</summary>
    public static long FullRateMinutes => Tiers[0].UpToMinute;

    /// <summary>The minute past which only <see cref="TrickleRate"/> applies.</summary>
    public static long TaperEndsAtMinute => Tiers[^1].UpToMinute;

    /// <summary>
    /// Total XP the first <paramref name="minutes"/> minutes of a day are worth.
    /// </summary>
    /// <remarks>
    /// Walks the tiers, taking whatever part of each the span reaches. Exact in integer
    /// arithmetic — no rounding anywhere — and the loop is over a handful of entries.
    /// </remarks>
    public static long TotalForMinutes(long minutes)
    {
        if (minutes <= 0) return 0;

        long total = 0;
        long consumed = 0;

        foreach (var (upTo, rate) in Tiers)
        {
            if (minutes <= consumed) return total;

            var inThisTier = Math.Min(minutes, upTo) - consumed;
            total += inThisTier * rate;
            consumed = upTo;
        }

        return total + Math.Max(0, minutes - consumed) * TrickleRate;
    }

    /// <summary>
    /// What <paramref name="minutes"/> more minutes are worth to someone already on
    /// <paramref name="minutesAlreadyEarnedToday"/>. A difference of two totals, so a
    /// span straddling a tier boundary is split correctly rather than being paid
    /// entirely at one rate.
    /// </summary>
    public static long XpForSpan(long minutesAlreadyEarnedToday, long minutes)
    {
        if (minutes <= 0) return 0;

        var before = Math.Max(0, minutesAlreadyEarnedToday);
        return TotalForMinutes(before + minutes) - TotalForMinutes(before);
    }

    /// <summary>
    /// What the *next* minute is worth to someone who has already earned
    /// <paramref name="minutesEarnedToday"/> today. For display and explanation;
    /// granting goes through <see cref="XpForSpan"/>. Defined as the difference above
    /// rather than by looking the tier up separately, so the two can never disagree.
    /// </summary>
    public static long RateAt(long minutesEarnedToday) => XpForSpan(minutesEarnedToday, 1);
}
