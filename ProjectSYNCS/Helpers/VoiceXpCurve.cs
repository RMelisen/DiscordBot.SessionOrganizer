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
// The day is the Europe/Paris day, since the figure it reads is MemberDailyStat's
// bucket, keyed by AppTime.DayKey — so the rate resets at local midnight.
public static class VoiceXpCurve
{
    /// <summary>Minutes paid at the full rate each day.</summary>
    public const long FullRateMinutes = 60;

    /// <summary>Minutes (cumulative) after which only the trickle rate applies.</summary>
    public const long ReducedRateMinutes = 120;

    public const long FullRate = 10;
    public const long ReducedRate = 3;
    public const long TrickleRate = 1;

    /// <summary>
    /// What the *next* minute is worth to someone who has already earned
    /// <paramref name="minutesEarnedToday"/> today. Display and explanation only —
    /// granting goes through <see cref="XpForSpan"/>, which stays correct even if a
    /// tick ever covers more than one minute.
    /// </summary>
    public static long RateAt(long minutesEarnedToday) =>
        minutesEarnedToday < FullRateMinutes ? FullRate
        : minutesEarnedToday < ReducedRateMinutes ? ReducedRate
        : TrickleRate;

    /// <summary>
    /// Total XP the first <paramref name="minutes"/> minutes of a day are worth.
    /// Closed-form, like LevelCurve.ThresholdForLevel — the span below is its
    /// difference, so the two can never disagree about a tier boundary.
    /// </summary>
    public static long TotalForMinutes(long minutes)
    {
        if (minutes <= 0) return 0;

        var full = Math.Min(minutes, FullRateMinutes);
        var reduced = Math.Clamp(minutes - FullRateMinutes, 0, ReducedRateMinutes - FullRateMinutes);
        var trickle = Math.Max(minutes - ReducedRateMinutes, 0);

        return full * FullRate + reduced * ReducedRate + trickle * TrickleRate;
    }

    /// <summary>
    /// What <paramref name="minutes"/> more minutes are worth to someone already on
    /// <paramref name="minutesAlreadyEarnedToday"/>. Computed as a difference of totals
    /// so a span straddling a tier boundary is split correctly rather than being paid
    /// entirely at one rate.
    /// </summary>
    public static long XpForSpan(long minutesAlreadyEarnedToday, long minutes)
    {
        if (minutes <= 0) return 0;

        var before = Math.Max(0, minutesAlreadyEarnedToday);
        return TotalForMinutes(before + minutes) - TotalForMinutes(before);
    }
}
