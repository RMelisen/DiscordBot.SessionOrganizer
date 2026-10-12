namespace ProjectSYNCS.Models;

// Stored as an int: append-only, never reorder or rename (CLAUDE.md).
public enum UptimeEventKind
{
    PowerCut,
    Crash,
    NetworkOutage,
}

// A stretch she was gone for, recorded by PiHealthService: a power cut or crash (found on the
// next start, StartedAt being her last heartbeat) or a lost gateway connection of
// PiHealth.OutageMin or more. Read by the monthly report.
public class UptimeEvent
{
    public int Id { get; set; }

    public UptimeEventKind Kind { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
}
