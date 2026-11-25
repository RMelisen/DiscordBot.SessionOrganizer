using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The sentences that say what a change of accessories did to the others: « Le bonnet a remplacé
/// la petite couronne. », « La fleur est passée à gauche. ». Built from the catalogue's names, which
/// carry their indefinite article (« une écharpe », « des moufles »), so the articles are re-derived here.
/// </summary>
public static class AccessoryNotices
{
    /// <summary>
    /// The notice(s) for a change, space-joined, or null when it displaced nothing.
    /// <paramref name="incoming"/> is the item that was put on (the one that replaced another);
    /// <paramref name="preview"/> says it in the conditional (a try-on: « remplacerait »).
    /// </summary>
    public static string? Of(AccessoryChange? change, CosmeticInfo? incoming, bool preview = false)
    {
        if (change is null) return null;
        var lines = new List<string>();
        if (change.Replaced is { } replaced && incoming is not null && CosmeticCatalog.ByKey(replaced) is { } old)
            lines.Add($"{Capitalize(Definite(incoming))} {Replace(incoming, preview)} {Definite(old)}.");
        if (change.Moved is { } moved && CosmeticCatalog.ByKey(moved) is { } item)
        {
            var worn = change.Worn.FirstOrDefault(w => w.Key == moved);
            var side = worn is null ? null : AccessoryFit.Side(moved, worn.Style);
            if (side is not null)
                lines.Add($"{Capitalize(Definite(item))} {Passed(item, preview)} à {side}.");
        }
        return lines.Count == 0 ? null : string.Join(" ", lines);
    }

    /// <summary>« le bonnet », « la petite couronne », « l'écharpe », « les moufles », from the catalogue name.</summary>
    public static string Definite(CosmeticInfo c)
    {
        var name = c.Name;
        foreach (var (indefinite, definite) in new[] { ("un ", "le "), ("une ", "la "), ("des ", "les ") })
            if (name.StartsWith(indefinite, StringComparison.Ordinal))
            {
                var noun = name[indefinite.Length..];
                return definite == "les " || !StartsWithVowelSound(noun) ? definite + noun : "l'" + noun;
            }
        return name;
    }

    private static bool IsPlural(CosmeticInfo c) => c.Name.StartsWith("des ", StringComparison.Ordinal);

    // Every plural accessory is feminine (des lunettes, des moufles).
    private static string Passed(CosmeticInfo c, bool preview)
    {
        if (preview) return IsPlural(c) ? "passeraient" : "passerait";
        return IsPlural(c) ? "sont passées" : c.Name.StartsWith("une ", StringComparison.Ordinal) ? "est passée" : "est passé";
    }

    private static string Replace(CosmeticInfo c, bool preview) =>
        preview ? (IsPlural(c) ? "remplaceraient" : "remplacerait") : IsPlural(c) ? "ont remplacé" : "a remplacé";

    // The only aspirated h in the catalogue is « haut-de-forme »: any other h would elide.
    internal static bool StartsWithVowelSound(string noun) =>
        noun.Length > 0 && ("aeiouyàâéèêëîïôöûœ".Contains(char.ToLowerInvariant(noun[0])) || (char.ToLowerInvariant(noun[0]) == 'h' && !noun.StartsWith("haut", StringComparison.Ordinal)));

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
