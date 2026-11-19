namespace ProjectSYNCS.Helpers;

/// <summary>
/// The shipped visit scenes (<c>Stories/Visits/*.visit</c>), loaded once on first access through
/// <see cref="VisitLibrary.Load"/> with the coverage required. Any error throws a
/// <see cref="StoryLoadException"/> naming file and line, and keeps throwing: a visit must never find
/// nothing to draw, nor a button a scene that half loaded. <c>Program.cs</c> touches it at startup,
/// beside <see cref="StoryCatalog.Compiled"/>, so a bad file stops the bot. No DI, no gateway.
/// </summary>
public static class VisitCatalog
{
    private sealed record Loaded(IReadOnlyList<VisitScene> Scenes, VisitScene Confession, IReadOnlyDictionary<string, VisitScene> ByKey);

    private static readonly object Lock = new();
    private static string _folder = Path.Combine(AppContext.BaseDirectory, "Stories", "Visits");
    private static Lazy<Loaded> _loaded = Make(_folder);

    /// <summary>
    /// The folder holding the <c>*.visit</c> files (the build output's <c>Stories/Visits/</c> by
    /// default). Setting it loads that folder on the next access (the checks do).
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

    /// <summary>The ordinary scenes, in file order (the confession is not among them).</summary>
    public static IReadOnlyList<VisitScene> Scenes => Data.Scenes;

    public static VisitScene Confession => Data.Confession;

    /// <summary>A scene of either kind by key, or null (a button naming a scene that no longer exists).</summary>
    public static VisitScene? ByKey(string key) => Data.ByKey.GetValueOrDefault(key);

    /// <summary>The ordinary scenes a pair may draw: its bond before the visit, the Paris hour, both stages.</summary>
    public static IReadOnlyList<VisitScene> Eligible(PlynlingBond bond, int hour, PlynlingStage visitor, PlynlingStage host) =>
        Data.Scenes.Where(s => s.Fits(bond, hour, visitor, host)).ToList();

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
        var load = VisitLibrary.Load(folder, requireCoverage: true);
        if (load.Errors.Count > 0) throw new StoryLoadException(load.Errors);
        return new Loaded(load.Scenes.Where(s => !s.Confession).ToList(), load.Scenes.Single(s => s.Confession),
            load.Scenes.ToDictionary(s => s.Key));
    }
}
