namespace ProjectSYNCS.Models;

// What went wrong on one Paris day, for /report incidents: one row per (day, kind, key), unique. Kind
// is an IncidentKinds key and Key what it happened to (a command path, a custom-id's verb, a logger's
// short name, how the last run stopped): both stored strings, so append-only. Not per guild, like the
// Pi's tables: there is one bot. Buckets only, from 7.56.0 on, written through ActivityCounter.
public class IncidentDailyStat
{
    public int Id { get; set; }
    public int Day { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; }
}
