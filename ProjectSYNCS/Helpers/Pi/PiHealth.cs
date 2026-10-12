namespace ProjectSYNCS.Helpers;

/// <summary>How the previous run of the bot ended, as read on the next start.</summary>
public enum StopKind
{
    // No record: the first start with this feature, or the state file was lost.
    Unknown,
    // Stopped properly: an update, a restart from Home Assistant.
    Clean,
    // Stopped without a word, and the machine booted since: the whole Pi went down.
    PowerCut,
    // Stopped without a word on a machine that stayed up: killed or crashed.
    Crash,
}

/// <summary>
/// What PiHealthService keeps in health-state.json. <see cref="CleanExit"/> is written false on
/// start and true on a proper stop; <see cref="LastAliveAt"/> is refreshed on every tick, so on
/// the next start it says, within one tick, when she went dark.
/// </summary>
public sealed record HealthState(bool CleanExit, DateTimeOffset LastAliveAt, int LastRecordDay = 0, int LastRouterDay = 0);

// Her body, read as lore: thresholds for the hot status lines and the heat record,
// how long a lost connection must last before she blames the router, and the classification
// of the last stop. Pure apart from the clock values passed in, so it can be checked without
// a Pi. The temperature thresholds are first guesses for a Pi 5 in a passive aluminium case;
// tune them against a few weeks of PiThermalDay rows.
public static class PiHealth
{
    // Status lines: at or above HotMilli she complains with the real number, on half the
    // rotations, so the ordinary lines still come round.
    public const int HotMilli = 70_000;
    public const double StatusChance = 0.5;

    // The Pi 5 starts throttling around 80 °C: time spent there is « au ralenti » for real.
    public const int ThrottleMilli = 80_000;

    // A heat record must beat every other day by this much, and only once there is enough
    // history for a record to mean something (the first weeks would break one daily).
    public const int RecordMarginMilli = 1_000;
    public const int RecordMinDays = 30;
    public const int RecordStartHour = 10;
    public const int RecordEndHour = 23;

    // A gateway gap shorter than this is Discord's ordinary reconnect, not an outage.
    public static readonly TimeSpan OutageMin = TimeSpan.FromMinutes(10);

    // Said when the downtime can't be measured (no heartbeat, or a clock that went backwards
    // after the power came back). Every downtime slot in the pools must read well with it.
    public const string UnknownDowntime = "un bon moment";

    // A machine up for less than this when she starts has just booted: Home Assistant OS
    // starts its add-ons a minute or two after power-on.
    public static readonly TimeSpan FreshBoot = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How the last run ended. Unclean on a machine that has just booted, or that booted after
    /// her last heartbeat, means the machine itself went down (a power cut, or a hard reset that
    /// looks the same from inside); unclean on a machine that stayed up, or whose uptime can't
    /// be read, is a crash.
    /// </summary>
    /// <remarks>
    /// The fresh-boot test needs no clock: a Pi without an RTC battery can start with the wrong
    /// time until NTP catches up, which would put the boot before the last heartbeat.
    /// </remarks>
    public static StopKind Classify(HealthState? previous, DateTimeOffset now, TimeSpan? hostUptime)
    {
        if (previous is null) return StopKind.Unknown;
        if (previous.CleanExit) return StopKind.Clean;
        if (hostUptime is not { } up) return StopKind.Crash;
        return up < FreshBoot || now - up > previous.LastAliveAt ? StopKind.PowerCut : StopKind.Crash;
    }

    public static bool IsAbrupt(StopKind kind) => kind is StopKind.PowerCut or StopKind.Crash;

    /// <summary>« 71°C », rounded to the degree.</summary>
    public static string Celsius(int milli) => $"{(int)Math.Round(milli / 1000.0, MidpointRounding.AwayFromZero)}°C";

    /// <summary>A duration as she says it (« 14 min », « 2 h 05 », « 1 j 3 h »), or the unknown wording.</summary>
    public static string Duration(TimeSpan? span) =>
        span is { } s && s > TimeSpan.Zero
            ? LevelCardUi.Duration(Math.Max(1, (long)Math.Round(s.TotalMinutes)))
            : UnknownDowntime;

    /// <summary>
    /// The messages she missed, as a noun phrase: « 1 message », « 37 messages », or « plus de
    /// 95 messages » when the history she read didn't reach back to the start of the outage.
    /// </summary>
    public static string Missed(int count, bool more) =>
        more ? $"plus de {count} messages" : count == 1 ? "1 message" : $"{count} messages";

    public static bool IsRecordHours(DateTimeOffset now)
    {
        var hour = AppTime.ToZoned(now).Hour;
        return hour >= RecordStartHour && hour < RecordEndHour;
    }
}
