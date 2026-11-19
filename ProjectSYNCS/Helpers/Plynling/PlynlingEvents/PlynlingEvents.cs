using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The event catalog: every event is a story file (ProjectSYNCS/Stories/, docs/agents/plynling-storylines.md),
/// compiled by StoryCatalog. There are no hand-written events. Keys (events and options) are stored:
/// **never rename one**. An old C# event's key resolves to the scene that claims it (`was:`), through
/// <see cref="ByKey"/>; compare stored keys through ByKey, StoredKeys or Current, never raw.
/// </summary>
public static partial class PlynlingEvents
{
    // The compiled story files, loaded on first access (a broken file throws). StoryCatalog.Compiled is a
    // lazy property, so reading it from this initializer is safe whatever the order of partial files.
    public static readonly IReadOnlyList<EventDef> All = StoryCatalog.Compiled;

    // Deleted events' titles, by key: a journal line or an owner report still names them. A removed key
    // is never reused (the checks hold it), so an old instance can never open as something else.
    public static readonly IReadOnlyDictionary<string, string> Removed = RemovedTitles();

    public static string? TitleOf(string key) => ByKey(key)?.Title ?? Removed.GetValueOrDefault(key);

    // Every def by key, plus each retired story scene's key → its successor's def (a run waiting on the
    // old key is offered the new scene).
    private static readonly Dictionary<string, EventDef> ByKeyMap = KeyMap(All, StoryCatalog.Aliases);

    public static Dictionary<string, EventDef> KeyMap(IEnumerable<EventDef> defs, IReadOnlyDictionary<string, EventDef> aliases) =>
        defs.Select(e => KeyValuePair.Create(e.Key, e)).Concat(aliases).ToDictionary();

    public static EventDef? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    /// <summary>Each alias key (a retired scene's, an old C# event's `was:`) → the live def it resolves to.</summary>
    public static IReadOnlyDictionary<string, EventDef> Aliases => StoryCatalog.Aliases;

    /// <summary>
    /// Every key a stored instance may carry for a def `which` matches: live keys, old keys (`was:`) and
    /// retired scenes' keys. What a SQL filter on <c>EventKey</c> must list, or it misses rows saved before a move.
    /// </summary>
    public static string[] StoredKeys(Func<EventDef, bool> which) => ByKeyMap.Where(kv => which(kv.Value)).Select(kv => kv.Key).ToArray();

    /// <summary>The live key a stored key now plays as (an unknown key is returned as it is): compare this with def keys, never the stored one.</summary>
    public static string Current(string storedKey) => ByKey(storedKey)?.Key ?? storedKey;

    // Event text, expanded: names in bold, agreements resolved, {T} the new trait(s) in bold. Names must
    // already be safe (PlynlingCardUi.SafeName). Without a target, {B} never appears in the text. {T} is
    // replaced first, so a trait name is never read as a template.
    public static string Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male,
        string? traits = null) =>
        PlynlingVisitStory.Expand(template.Replace("{T}", traits is null ? "" : $"**{traits}**"), a, ga, b ?? "", gb);
}
