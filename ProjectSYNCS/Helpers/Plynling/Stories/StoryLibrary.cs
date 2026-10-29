namespace ProjectSYNCS.Helpers;

public sealed record StoryLoad(IReadOnlyList<StoryArc> Arcs, IReadOnlyList<CastMember> Cast, IReadOnlyList<StoryError> Errors);

/// <summary>
/// Loads story files from disk: <c>village.cast</c> and every <c>*.story</c> under the given folders,
/// parsed and validated together, so a reference across files is checked. Errors name each file as
/// "&lt;folder&gt;/&lt;path inside it&gt;".
/// </summary>
public static class StoryLibrary
{
    public const string CastFile = "village.cast";

    public static StoryLoad Load(string castFolder, params string[] storyFolders)
    {
        var errors = new List<StoryError>();
        var castPath = Path.Combine(castFolder, CastFile);
        IReadOnlyList<CastMember> cast = File.Exists(castPath)
            ? StoryParser.ParseCast(Label(castFolder, castPath), File.ReadAllText(castPath), errors)
            : Missing(castPath, errors);

        var arcs = new List<StoryArc>();
        foreach (var folder in storyFolders)
        {
            if (!Directory.Exists(folder)) { errors.Add(new(new(folder, 0), "story folder not found")); continue; }
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                // Exactly ".story", lowercase: Windows matches without case, the Pi does not, and a
                // "x.story.txt" from Notepad must not vanish without a word.
                if (!file.EndsWith(".story", StringComparison.Ordinal))
                {
                    if (Path.GetFileName(file) != CastFile)
                        errors.Add(new(new(Label(folder, file), 0), "not a .story file (the name must end in lowercase .story), so it is not loaded"));
                    continue;
                }
                if (StoryParser.Parse(Label(folder, file), File.ReadAllText(file), errors) is { } arc) arcs.Add(arc);
            }
        }
        errors.AddRange(StoryValidator.Validate(arcs, cast));
        return new StoryLoad(arcs, cast, errors);
    }

    private static IReadOnlyList<CastMember> Missing(string path, List<StoryError> errors)
    {
        errors.Add(new(new(path, 0), $"{CastFile} not found"));
        return Array.Empty<CastMember>();
    }

    private static string Label(string folder, string file) =>
        Path.Combine(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)), Path.GetRelativePath(folder, file)).Replace('\\', '/');
}
