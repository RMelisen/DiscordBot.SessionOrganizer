namespace ProjectSYNCS.Helpers;

/// <summary>
/// The pure half of what the service stores and reads about story runs (design spec 2026-11-17,
/// "How C works"): a run's flags, stored on its instances as <c>key;key</c> (none is null, never
/// ""), what finished runs leave (<c>arc.ending</c>, <c>arc.flag</c>) and when each arc last started.
/// Instance keys are <c>arc.scene</c> (StoryCompiler.Key); a C# key has no dot.
/// </summary>
public static class StoryRuns
{
    private const char Separator = ';';

    public static IReadOnlySet<string> ParseFlags(string? stored) =>
        (stored ?? "").Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal);

    // Sorted, so the same set always writes the same text.
    public static string? WriteFlags(IEnumerable<string> flags)
    {
        var keys = flags.Where(f => f.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        return keys.Count == 0 ? null : string.Join(Separator, keys);
    }

    /// <summary>The run's flags after a step: what it carried, then each applied [flag] / [unflag] in order.</summary>
    public static IReadOnlySet<string> NextFlags(string? stored, IEnumerable<EventEffect> applied)
    {
        var flags = new HashSet<string>(ParseFlags(stored), StringComparer.Ordinal);
        foreach (var effect in applied)
        {
            if (effect is SetFlag s) flags.Add(s.Key);
            else if (effect is ClearFlag c) flags.Remove(c.Key);
        }
        return flags;
    }

    /// <summary>A story instance's arc: its key's part before the dot. Null for a C# event.</summary>
    public static string? ArcOf(string eventKey) => eventKey.IndexOf('.') is var dot and > 0 ? eventKey[..dot] : null;

    /// <summary>What finished runs leave for later conditions: <c>arc.ending</c>, and <c>arc.flag</c> for each of the ending step's final flags.</summary>
    public static (IReadOnlySet<string> Ended, IReadOnlySet<string> ArcFlags) Finished(
        IEnumerable<(string EventKey, string? EndingKey, string? Flags)> runs)
    {
        var ended = new HashSet<string>(StringComparer.Ordinal);
        var arcFlags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (eventKey, endingKey, flags) in runs)
        {
            if (endingKey is null || ArcOf(eventKey) is not { } arc) continue;
            ended.Add($"{arc}.{endingKey}");
            foreach (var flag in ParseFlags(flags)) arcFlags.Add($"{arc}.{flag}");
        }
        return (ended, arcFlags);
    }

    /// <summary>Each story arc's first scene key → its arc. Built from the arc, not the defs' types, so a stored key always counts.</summary>
    public static IReadOnlyDictionary<string, StoryArc> Openers(IEnumerable<EventDef> defs) =>
        Openers(defs, new Dictionary<string, EventDef>());

    /// <summary>
    /// Each story arc's first scene key → its arc, plus every alias (an old C# key, `was:`) of a first scene:
    /// an arc started under its old key counts as started.
    /// </summary>
    public static IReadOnlyDictionary<string, StoryArc> Openers(IEnumerable<EventDef> defs, IReadOnlyDictionary<string, EventDef> aliases)
    {
        var map = defs.Select(d => d.Story?.Arc).OfType<StoryArc>().Where(a => a.Scenes.Count > 0).Distinct()
            .ToDictionary(a => StoryCompiler.Key(a.Key, a.Scenes[0].Key));
        foreach (var (key, def) in aliases)
            if (def.Story is { } link && link.Arc.Scenes.Count > 0 && ReferenceEquals(link.Scene, link.Arc.Scenes[0])) map.TryAdd(key, link.Arc);
        return map;
    }

    /// <summary>
    /// When each arc last started (its first scene's instance, by <c>CreatedAt</c>), and the latest start
    /// of any long arc, drawn or triggered: the 21 days between long drawn arcs count from either.
    /// </summary>
    public static (IReadOnlyDictionary<string, DateTimeOffset> LastStarted, DateTimeOffset? LastLongStart) Starts(
        IEnumerable<(string EventKey, DateTimeOffset CreatedAt)> instances, IReadOnlyDictionary<string, StoryArc> openers)
    {
        var last = new Dictionary<string, DateTimeOffset>();
        DateTimeOffset? lastLong = null;
        foreach (var (eventKey, createdAt) in instances)
        {
            if (!openers.TryGetValue(eventKey, out var arc)) continue;
            if (!last.TryGetValue(arc.Key, out var seen) || createdAt > seen) last[arc.Key] = createdAt;
            if (arc.Header.Size == StorySize.Long && (lastLong is null || createdAt > lastLong)) lastLong = createdAt;
        }
        return (last, lastLong);
    }
}
