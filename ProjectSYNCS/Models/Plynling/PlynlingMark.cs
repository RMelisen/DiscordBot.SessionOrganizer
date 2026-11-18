using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Models;

// A mark a story left on a Plynling: an epithet, a keepsake or an unlock (MarkKind is stored as an
// int: append-only). Key is a story's key: stored, never renamed. One row per (Plynling, kind, key),
// enforced by a unique index; deleted with the Plynling (abandoned), kept when it dies. InstanceId is
// the event that gave it — a plain column, not a foreign key, since instances are history.
public class PlynlingMark
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public MarkKind Kind { get; set; }
    public string Key { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAt { get; set; }
    public int? InstanceId { get; set; }
}
