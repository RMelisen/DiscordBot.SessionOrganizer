namespace ProjectSYNCS.Models;

// Who cared for one Plynling and how much (/plynling stats: how many people, its biggest fan): meals given and
// pets, the owner included. One row per (Plynling, person), unique. Written by PlynlingCounters.AddCarerAsync in
// the meal's or pet's own save. Deleted with the Plynling (abandoned), kept when it dies. Counted from
// PlynlingCounters.Since.
public class PlynlingCarer
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public ulong UserId { get; set; }
    public long Meals { get; set; }
    public long Pets { get; set; }

    // Concurrency token, bumped on every save of the row (AppDbContext), like PlynlingCounter's.
    public int Version { get; set; }
}
