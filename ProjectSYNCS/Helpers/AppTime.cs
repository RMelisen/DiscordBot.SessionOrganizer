using System.Globalization;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// All user-entered times are interpreted in this fixed zone, independent of the
/// server's local time (which is usually UTC in production). DST is handled by
/// resolving the offset for the specific wall-clock moment.
/// </summary>
public static class AppTime
{
    public static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        // IANA id works on Linux/macOS; the Windows id is the fallback for dev.
        foreach (var id in new[] { "Europe/Paris", "Romance Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Local;
    }

    /// <summary>Current time expressed in the app's zone.</summary>
    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    /// <summary>Re-expresses any instant as wall-clock time in the app's zone.</summary>
    public static DateTimeOffset ToZoned(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    /// <summary>
    /// Parses a wall-clock string (e.g. "2026-06-20T20:30") as a time in the app's
    /// zone, attaching the correct UTC offset for that date (incl. DST).
    /// </summary>
    public static bool TryParseWallClock(string value, string format, out DateTimeOffset result)
    {
        result = default;
        if (!DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var naive))
            return false;

        var offset = Zone.GetUtcOffset(naive);
        result = new DateTimeOffset(naive, offset);
        return true;
    }
}
