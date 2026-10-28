using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// The visits told recently, so their ◀ ▶ can page back through them — stored (VisitStoryRecord), so a
// restart, which every update is, no longer breaks the arrows on the stories people scroll back to.
// Each story is kept whole as it was told: it cannot be rebuilt, since its words come from the
// ResponsePicker's history and the pools change between versions. Kept KeepDays; an older card's
// arrows then say the story is no longer available. Nothing is lost that mattered: what the visit
// did is in the Plynlings and their relation, the story is only its telling.
// Transient: it wraps AppDbContext.
public sealed class VisitStories
{
    public const int KeepDays = 30;

    private const string Alphabet = "abcdefghijkmnpqrstuvwxyz23456789";   // no 0/o, 1/l

    // Enums by name: a renamed face or stage then fails to read (« plus disponible ») instead of
    // silently showing another one, and reordering an enum changes nothing.
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    private readonly AppDbContext _db;
    private readonly ILogger<VisitStories> _logger;

    public VisitStories(AppDbContext db, ILogger<VisitStories> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Keeps the story under a fresh short id, and returns it carrying that id. The visit is already
    // saved, so a failed store is logged and the story still told: only its arrows will say it is gone.
    public async Task<VisitStory> AddAsync(VisitStory story, DateTimeOffset now, Random rng)
    {
        string id;
        do id = new string(Enumerable.Range(0, 6).Select(_ => Alphabet[rng.Next(Alphabet.Length)]).ToArray());
        while (await ExistsAsync(id));

        var kept = story with { Id = id };
        try
        {
            _db.VisitStoryRecords.Add(new VisitStoryRecord { Id = id, Day = AppTime.DayKey(now), Story = JsonSerializer.Serialize(kept, Json) });
            await _db.SaveChangesAsync();
            var oldest = AppTime.DayKey(now - TimeSpan.FromDays(KeepDays));
            await _db.VisitStoryRecords.Where(r => r.Day < oldest).ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store visit story {StoryId}.", id);
        }
        return kept;
    }

    public async Task<VisitStory?> GetAsync(string id)
    {
        var row = await _db.VisitStoryRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (row is null) return null;
        try
        {
            return JsonSerializer.Deserialize<VisitStory>(row.Story, Json);
        }
        catch (JsonException ex)
        {
            // A story stored by a version whose shapes have since changed: no longer available.
            _logger.LogWarning(ex, "Unreadable visit story {StoryId}.", id);
            return null;
        }
    }

    private async Task<bool> ExistsAsync(string id)
    {
        try
        {
            return await _db.VisitStoryRecords.AnyAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check visit story id {StoryId}.", id);
            return false;     // AddAsync's own try then logs the failed store
        }
    }
}
