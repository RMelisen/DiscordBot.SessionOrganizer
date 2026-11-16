namespace ProjectSYNCS.Models;

// A stretch she was gone after stopping properly (an update, the add-on stopped from Home
// Assistant), recorded by PiHealthService on the next start: StartedAt is the clean exit's last
// heartbeat, EndedAt the start. Kept apart from UptimeEvent on purpose — /health and the monthly
// report count incidents, and a planned stop isn't one. Its one reader is /daily, which excuses a
// day she was away long enough (DailyStreak.ExcusedDays).
public class CleanStop
{
    public int Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
}
