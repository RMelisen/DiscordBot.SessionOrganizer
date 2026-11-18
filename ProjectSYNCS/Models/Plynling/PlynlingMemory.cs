namespace ProjectSYNCS.Models;

// A memory a story gave a Plynling. MemoryKey is a story's key: stored, never renamed. Held from
// AcquiredAt; RevealedAt stays null until a later story brings it up. One row per (Plynling, key),
// enforced by a unique index; deleted with the Plynling (abandoned), kept when it dies. InstanceId is
// the event that gave it — a plain column, not a foreign key, since instances are history.
public class PlynlingMemory
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public string MemoryKey { get; set; } = string.Empty;
    public DateTimeOffset AcquiredAt { get; set; }
    public DateTimeOffset? RevealedAt { get; set; }
    public int? InstanceId { get; set; }
}
