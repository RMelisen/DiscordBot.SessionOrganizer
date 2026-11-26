namespace ProjectSYNCS.Models;

// One all-time count for one Plynling (/plynling stats): how many baths, how many Morilles eaten… Key is a
// PlynlingCounters key — stored, so **append-only, never renamed**. One row per (Plynling, key), unique.
// Written by PlynlingCounters.AddAsync inside the action's own save, never alone. Deleted with the Plynling
// (abandoned), kept when it dies. Counting started on PlynlingCounters.Since: older Plynlings show « depuis ».
public class PlynlingCounter
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public string Key { get; set; } = string.Empty;
    public long Value { get; set; }

    // Concurrency token, bumped on every save of the row (AppDbContext): two units bumping one counter (an event
    // decided alone does not touch the Plynling row) must not overwrite each other.
    public int Version { get; set; }
}
