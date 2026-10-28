namespace ProjectSYNCS.Models;

// A visit's story as it was told, so its ◀ ▶ still page after a restart (VisitStories). The story is
// JSON: it is only the telling — what the visit did is in the Plynlings and their relation — and it
// is never queried by its contents. Kept VisitStories.KeepDays, by Day (AppTime.DayKey) so the
// pruning filters in SQL.
public class VisitStoryRecord
{
    // The short id the pager's custom-ids carry (vis:next:{id}:{beat}).
    public string Id { get; set; } = string.Empty;
    public int Day { get; set; }
    public string Story { get; set; } = string.Empty;
}
