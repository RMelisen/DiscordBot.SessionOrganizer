namespace ProjectSYNCS.Helpers;

/// <summary>Story files that failed to load: every parse and validation error, one per line, each with its file and line.</summary>
public sealed class StoryLoadException(IReadOnlyList<StoryError> errors)
    : Exception($"{errors.Count} story file error(s):\n" + string.Join("\n", errors))
{
    public IReadOnlyList<StoryError> Errors { get; } = errors;
}

/// <summary>
/// The shipped story files, loaded once on first access: <see cref="StoryLibrary.Load"/> (parse, then
/// <see cref="StoryValidator"/>), then <see cref="StoryCompiler.Compile"/>. Any error throws a
/// <see cref="StoryLoadException"/> naming file and line, and keeps throwing (the bot must not run on
/// half a catalog). <see cref="PlynlingEvents.All"/> reads <see cref="Compiled"/>, so whoever touches
/// the catalog first triggers the load. No DI, no gateway.
/// </summary>
public static class StoryCatalog
{
    private sealed record Loaded(IReadOnlyList<EventDef> Compiled, IReadOnlyDictionary<string, EventDef> Aliases,
        IReadOnlyDictionary<string, MemoryDecl> Memories, IReadOnlyDictionary<string, KeepsakeDecl> Keepsakes,
        IReadOnlyDictionary<string, EpithetDecl> Epithets, IReadOnlyDictionary<string, CastMember> Cast);

    private static readonly object Lock = new();
    private static string _folder = Path.Combine(AppContext.BaseDirectory, "Stories");
    private static Lazy<Loaded> _loaded = Make(_folder);

    /// <summary>
    /// The folder holding <c>village.cast</c> and the <c>*.story</c> files (the build output's
    /// <c>Stories/</c> by default). Set it before the first access: setting it again loads that
    /// folder on the next access (the checks do), but <see cref="PlynlingEvents.All"/> keeps what it read first.
    /// </summary>
    public static string Folder
    {
        get => _folder;
        set
        {
            lock (Lock)
            {
                _folder = value;
                _loaded = Make(value);
            }
        }
    }

    public static IReadOnlyList<EventDef> Compiled => Data.Compiled;

    /// <summary>A retired scene's key → the def of the live scene it leads to. Never listed in <see cref="Compiled"/>.</summary>
    public static IReadOnlyDictionary<string, EventDef> Aliases => Data.Aliases;

    public static IReadOnlyDictionary<string, MemoryDecl> Memories => Data.Memories;
    public static IReadOnlyDictionary<string, KeepsakeDecl> Keepsakes => Data.Keepsakes;
    public static IReadOnlyDictionary<string, EpithetDecl> Epithets => Data.Epithets;

    /// <summary>Every villager of <c>village.cast</c>, by key: the cast keys <see cref="StoryStanding.Compute"/> takes.</summary>
    public static IReadOnlyDictionary<string, CastMember> Cast => Data.Cast;

    private static Loaded Data
    {
        get
        {
            Lazy<Loaded> loaded;
            lock (Lock) loaded = _loaded;
            return loaded.Value;
        }
    }

    // ExecutionAndPublication (the default): one load, and a failed load keeps throwing the same error.
    private static Lazy<Loaded> Make(string folder) => new(() => Load(folder));

    private static Loaded Load(string folder)
    {
        var load = StoryLibrary.Load(folder, folder);
        if (load.Errors.Count > 0) throw new StoryLoadException(load.Errors);
        var compiled = StoryCompiler.Compile(load);
        var byKey = compiled.ToDictionary(d => d.Key);
        var aliases = StoryCompiler.Aliases(load).Where(kv => byKey.ContainsKey(kv.Value)).ToDictionary(kv => kv.Key, kv => byKey[kv.Value]);
        return new Loaded(compiled, aliases,
            load.Arcs.SelectMany(a => a.Memories).ToDictionary(m => m.Key),
            load.Arcs.SelectMany(a => a.Keepsakes).ToDictionary(k => k.Key),
            load.Arcs.SelectMany(a => a.Epithets).ToDictionary(e => e.Key),
            load.Cast.ToDictionary(c => c.Key));
    }
}
