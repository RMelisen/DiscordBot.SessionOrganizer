namespace ProjectSYNCS.Helpers;

// What a story file may remember about a Plynling and the village. Stored as an int: append only.
public enum MarkKind { Epithet, Keepsake, Unlock }

/// <summary>
/// Everything a story's conditions read, gathered by the caller (the service reads the rows, the
/// checks build it by hand): the run's flags, the memories and marks the Plynling holds, the endings
/// and flags finished runs left, the standing computed from its memories (<see cref="StoryStanding"/>),
/// when each arc last started, and the clock.
/// </summary>
public sealed record StoryKnowledge(
    IReadOnlySet<string> Flags,
    IReadOnlySet<string> Memories,
    IReadOnlySet<(MarkKind Kind, string Key)> Marks,
    IReadOnlySet<string> Ended,
    IReadOnlySet<string> ArcFlags,
    IReadOnlyDictionary<string, int> Standing,
    IReadOnlyDictionary<string, DateTimeOffset> LastStarted,
    DateTimeOffset? LastLongStart,
    Season Season,
    DateTimeOffset Now)
{
    public static StoryKnowledge Empty(DateTimeOffset now) => new(
        new HashSet<string>(), new HashSet<string>(), new HashSet<(MarkKind, string)>(), new HashSet<string>(), new HashSet<string>(),
        new Dictionary<string, int>(), new Dictionary<string, DateTimeOffset>(), null, Season.None, now);
}

/// <summary>
/// Standing with the villagers, computed and never stored (format spec, <c>VillageMemory</c>): the
/// holder of a memory gets its shift; a public (or revealed) memory also gives every other cast
/// member ±1 in its direction. The cast (<c>castKeys</c>) is every villager, including those who hold no memory.
/// </summary>
public static class StoryStanding
{
    public static IReadOnlyDictionary<string, int> Compute(IEnumerable<string> heldMemoryKeys,
        IReadOnlyDictionary<string, MemoryDecl> decls, IEnumerable<string> revealed, IEnumerable<string> castKeys)
    {
        var reveal = revealed.ToHashSet();
        var cast = castKeys.Distinct().ToList();
        var standing = new Dictionary<string, int>();
        foreach (var key in heldMemoryKeys.Distinct())
        {
            if (!decls.TryGetValue(key, out var d)) continue;
            standing[d.Holder] = standing.GetValueOrDefault(d.Holder) + d.Shift;
            if (!d.Public && !reveal.Contains(key)) continue;
            foreach (var other in cast.Where(c => c != d.Holder))
                standing[other] = standing.GetValueOrDefault(other) + Math.Sign(d.Shift);
        }
        return standing;
    }
}
