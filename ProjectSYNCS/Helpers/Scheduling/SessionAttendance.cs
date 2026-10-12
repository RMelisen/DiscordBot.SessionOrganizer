using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The timing rules of SessionAttendanceService, pure so they can be checked without a gateway.
// A session has no voice channel of its own (its native event is External, « Salon vocal »), so
// "present" means seen in any of the server's voice channels, the AFK one aside, inside the window.
public static class SessionAttendance
{
    // Seen this long before the start already counts: people drop in early.
    public static readonly TimeSpan WatchBefore = TimeSpan.FromMinutes(15);

    // First seen later than start + LateAfter is late; the late call goes out at that point.
    public static readonly TimeSpan LateAfter = TimeSpan.FromMinutes(10);

    // A late call that couldn't go out by then (a restart, a deploy) is skipped, not sent stale.
    public static readonly TimeSpan LateCallUntil = TimeSpan.FromMinutes(30);

    // The recap never comes earlier than this after the start, whatever voice looks like.
    public static readonly TimeSpan RecapMinElapsed = TimeSpan.FromMinutes(30);

    // Fewer than two of the people who came still in voice for this long: the group has
    // scattered, and the recap goes out.
    public static readonly TimeSpan ScatterToRecap = TimeSpan.FromMinutes(15);

    // The recap goes out by then at the latest, scattered or not.
    public static readonly TimeSpan RecapFallback = SessionEvent.Duration + TimeSpan.FromHours(2);

    // A session needs this many confirmed people for her to call the roll at all.
    public const int MinConfirmed = 2;

    /// <summary>Whether a sighting at <paramref name="now"/> still counts for the session.</summary>
    public static bool InSightingWindow(DateTimeOffset now, DateTimeOffset start) =>
        now >= start - WatchBefore && now <= start + SessionEvent.Duration;

    public static bool IsLate(DateTimeOffset firstSeen, DateTimeOffset start) =>
        firstSeen > start + LateAfter;

    public static bool LateCallDue(DateTimeOffset now, DateTimeOffset start) =>
        now >= start + LateAfter && now <= start + LateCallUntil;

    public static bool LateCallExpired(DateTimeOffset now, DateTimeOffset start) =>
        now > start + LateCallUntil;

    public static bool RecapAllowed(DateTimeOffset now, DateTimeOffset start) =>
        now >= start + RecapMinElapsed;

    public static bool RecapForced(DateTimeOffset now, DateTimeOffset start) =>
        now >= start + RecapFallback;

    /// <summary>Whether the session is under way: what the seat's tie-break calls "live".</summary>
    public static bool IsLive(DateTimeOffset now, DateTimeOffset start) =>
        now >= start && now <= start + SessionEvent.Duration;
}
