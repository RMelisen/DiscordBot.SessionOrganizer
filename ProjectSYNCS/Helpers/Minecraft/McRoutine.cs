namespace ProjectSYNCS.Helpers;

/// <summary>
/// Which remembered places her day in game is built on (the body's routine): a farm, a fishing spot, an animal pen and
/// home, found by their names. Pure, so the checks walk it.
/// </summary>
/// <remarks>
/// Keys are already normalised (<see cref="McCommand.PlaceKey"/>): lower case, no accents. An animal-pen word anywhere in
/// the name makes a pen (« ferme aux animaux »); a farm or fishing word must open it (« champ de blé », « étang »), so
/// « chemin du lac » is no fishing spot. With several of a kind, the first by name.
/// </remarks>
public static class McRoutine
{
    /// <summary>The names a home goes by, in the order « rentre » tries them.</summary>
    public static readonly string[] HomeKeys = { "maison", "base", "home", "chez nous", "chez moi", "qg" };

    private static readonly HashSet<string> RanchWords = new()
    {
        "enclos", "paturage", "paturages", "bergerie", "etable", "poulailler", "animaux", "vaches", "moutons", "cochons", "poules",
        "elevage",
    };
    private static readonly HashSet<string> FarmWords = new()
    {
        "ferme", "champ", "champs", "potager", "plantation", "plantations", "culture", "cultures",
    };
    private static readonly HashSet<string> FishWords = new() { "peche", "etang", "lac", "ponton", "riviere", "mare" };

    /// <summary>The key of each kind of place among <paramref name="keys"/>, null where there's none.</summary>
    public static (string? Farm, string? Fish, string? Ranch, string? Home) Classify(IEnumerable<string> keys)
    {
        string? farm = null, fish = null, ranch = null;
        var sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        foreach (var key in sorted)
        {
            var words = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) continue;
            if (words.Any(RanchWords.Contains)) ranch ??= key;
            else if (FarmWords.Contains(words[0])) farm ??= key;
            else if (FishWords.Contains(words[0])) fish ??= key;
        }
        return (farm, fish, ranch, HomeKeys.FirstOrDefault(sorted.Contains));
    }
}
