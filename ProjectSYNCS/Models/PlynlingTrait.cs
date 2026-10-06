namespace ProjectSYNCS.Models;

// Stored as an int: **append-only**.
public enum TraitKind { Childhood, Personality, Coping }

// One trait a Plynling holds (Helpers/PlynlingTraits holds the catalog). One row per
// (Plynling, trait), enforced by a unique index. Drawn once and kept: a trait appended to the
// catalog must never change anyone's existing traits. Deleted with the Plynling (abandoned);
// kept when it dies.
public class PlynlingTrait
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }

    // PlynlingTraits key — stable, never renamed.
    public string Key { get; set; } = string.Empty;
    public TraitKind Kind { get; set; }
    public DateTimeOffset AcquiredAt { get; set; }
}
