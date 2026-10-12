namespace ProjectSYNCS.Helpers;

// When AmbientService (and PresenceService's night status) may do what. Pure, so the
// windows can be checked without a gateway. Every check reads the wall-clock hour in the
// app's zone, so DST only moves the instants, never the rules: the March changeover skips
// 2:00–3:00 and the October one repeats it, and neither touches a window's edges.
public static class Ambient
{
    // Her pretend sleep: the idle moon and the sleepy status lines. She doesn't sleep; she
    // does it to be like everyone (docs/syncs-voice.md, "Her nights").
    public const int SleepStartHour = 1;
    public const int SleepEndHour = 7;

    // 3 a.m., her favourite hour: the one time she drops the act.
    public const int NightLineHour = 3;
    public const double NightLineChance = 0.25;
    public static readonly TimeSpan NightQuiet = TimeSpan.FromHours(1);
    // Anyone who answers the 3 a.m. line before this gets told to go to bed.
    public static readonly TimeSpan ScoldUntil = new(5, 30, 0);

    // Daytime silence she may speak into, once a day at most.
    public const int IdleStartHour = 10;
    public const int IdleEndHour = 23;
    public static readonly TimeSpan IdleQuiet = TimeSpan.FromHours(6);
    public const double IdleChance = 0.5;
    // Of an idle turn: a late "seen" reaction instead of a line.
    public const double SeenChance = 0.3;
    // Of an idle line: the self-correcting edit instead of a plain line.
    public const double EditChance = 0.25;
    // The edit lands this long after the line, or this soon after someone speaks after it:
    // she got caught.
    public static readonly TimeSpan EditAfter = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan CaughtEditDelay = TimeSpan.FromSeconds(2);

    // "SYNCS est en train d'écrire…" and then nothing, on a message breaking a silence.
    public static readonly TimeSpan GhostTypingQuiet = TimeSpan.FromHours(1);
    public const double GhostTypingChance = 0.03;

    // Waking after a restart: every time, but daytime only (a 2 a.m. deploy stays silent)
    // and at most once a day. Not after a power cut or a crash (PiHealth): those always speak.
    public const int WakeStartHour = 9;
    public const int WakeEndHour = 23;

    // After a power cut or a crash: how often she comes back corrupted (GlitchWakeLines) instead
    // of shaken, and how long the corrupted line stands before she edits it clean — unless
    // someone speaks in the main channel first: then she snaps back CaughtEditDelay after it.
    public const double GlitchWakeChance = 0.35;
    public static readonly TimeSpan GlitchEditAfter = TimeSpan.FromMinutes(2);

    public static bool IsSleepHours(DateTimeOffset now) => InHours(now, SleepStartHour, SleepEndHour);

    public static bool IsNightLineHour(DateTimeOffset now) => InHours(now, NightLineHour, NightLineHour + 1);

    public static bool IsIdleHours(DateTimeOffset now) => InHours(now, IdleStartHour, IdleEndHour);

    public static bool IsWakeHours(DateTimeOffset now) => InHours(now, WakeStartHour, WakeEndHour);

    /// <summary>Whether a message at <paramref name="now"/> is still early enough to be scolded.</summary>
    public static bool IsBeforeScoldEnd(DateTimeOffset now) => AppTime.ToZoned(now).TimeOfDay < ScoldUntil;

    /// <summary>
    /// Tonight's 3 a.m. decision: the minute past three she'll speak at, or null when she
    /// stays quiet tonight. Drawn once per night by the caller.
    /// </summary>
    public static int? NightMinute(Random random) =>
        random.NextDouble() < NightLineChance ? random.Next(0, 50) : null;

    // [start, end) in whole wall-clock hours.
    private static bool InHours(DateTimeOffset now, int start, int end)
    {
        var hour = AppTime.ToZoned(now).Hour;
        return hour >= start && hour < end;
    }
}
