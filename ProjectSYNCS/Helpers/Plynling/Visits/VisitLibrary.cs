namespace ProjectSYNCS.Helpers;

public sealed record VisitLoad(IReadOnlyList<VisitScene> Scenes, IReadOnlyList<StoryError> Errors);

/// <summary>
/// Loads every <c>*.visit</c> under a folder, parsed and validated together (keys are unique across
/// files, and the set holds one confession). <paramref name="requireCoverage"/> adds
/// <see cref="VisitValidator.Coverage"/>: the shipped folder must cover the draw, the fixtures need not.
/// Errors name each file as "&lt;folder&gt;/&lt;path inside it&gt;".
/// </summary>
public static class VisitLibrary
{
    public const string Extension = ".visit";

    public static VisitLoad Load(string folder, bool requireCoverage)
    {
        var errors = new List<StoryError>();
        var scenes = new List<VisitScene>();
        if (!Directory.Exists(folder)) errors.Add(new(new(folder, 0), "visit folder not found"));
        else
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                // Exactly ".visit", lowercase, as for stories: the Pi's file system has case.
                if (!file.EndsWith(Extension, StringComparison.Ordinal))
                {
                    errors.Add(new(new(Label(folder, file), 0), "not a .visit file (the name must end in lowercase .visit), so it is not loaded"));
                    continue;
                }
                if (VisitParser.Parse(Label(folder, file), File.ReadAllText(file), errors) is { } scene) scenes.Add(scene);
            }
        errors.AddRange(VisitValidator.Validate(scenes));
        if (requireCoverage) errors.AddRange(VisitValidator.Coverage(scenes));
        return new VisitLoad(scenes, errors);
    }

    private static string Label(string folder, string file) =>
        Path.Combine(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)), Path.GetRelativePath(folder, file)).Replace('\\', '/');
}
