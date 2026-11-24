namespace ProjectSYNCS.Helpers;

/// <summary>
/// The styles a Plynling remembers per accessory (<c>Plynling.SavedAccessoryStyles</c>): « key=style »
/// pairs joined by « ; », full cosmetic keys (never containing « = » or « ; »). Empty is null.
/// </summary>
public static class SavedStyles
{
    public static IReadOnlyDictionary<string, string> Parse(string? text)
    {
        var map = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(text)) return map;
        foreach (var pair in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var at = pair.IndexOf('=');
            if (at <= 0 || at == pair.Length - 1) continue;
            map[pair[..at]] = pair[(at + 1)..];
        }
        return map;
    }

    public static string? Format(IReadOnlyDictionary<string, string> saved)
    {
        var text = string.Join(';', saved.Select(kv => $"{kv.Key}={kv.Value}"));
        return text.Length == 0 ? null : text;
    }
}
