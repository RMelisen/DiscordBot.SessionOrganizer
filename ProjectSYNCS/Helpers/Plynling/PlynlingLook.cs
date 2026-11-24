using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>An accessory as worn: its full cosmetic key and its style, already resolved.</summary>
public sealed record WornAccessory(string Key, string? Style);

/// <summary>
/// Everything a dressed Plynling picture depends on, and the attachment name it travels under.
/// <see cref="Shape"/> is the art key with « _baby » for a baby (« amanite_baby »), <see cref="Face"/>
/// the lowercase mood, <see cref="Aura"/> the aura key without « cos.aura. ». The record is the
/// painter's cache key (<see cref="PlynlingPainter"/>).
/// </summary>
public sealed record PlynlingLook(string Shape, string Face, bool Dirty, WornAccessory? First, WornAccessory? Second, string? Aura)
{
    private const string Prefix = "pl", Extension = ".webp", AccessoryPrefix = "cos.accessory.", AuraTag = "aura-";

    /// <summary>
    /// The attachment name: « pl__amanite_baby__content__dirty__noeud-rose_droite__ballon-rouge__aura-bulles.webp ».
    /// Segments are joined by « __ »; each item is its key without « cos.accessory. », then « -style »
    /// when it has one, in draw order. A look with a second item and no first isn't canonical: it
    /// names the item as the first.
    /// </summary>
    public string FileName
    {
        get
        {
            var parts = new List<string> { Prefix, Shape, Face };
            if (Dirty) parts.Add("dirty");
            foreach (var item in new[] { First, Second })
                if (item is not null) parts.Add(ItemSegment(item));
            if (Aura is not null) parts.Add(AuraTag + Aura);
            return string.Join("__", parts) + Extension;
        }
    }

    private static string ItemSegment(WornAccessory item)
    {
        var name = item.Key.StartsWith(AccessoryPrefix, StringComparison.Ordinal) ? item.Key[AccessoryPrefix.Length..] : item.Key;
        return item.Style is null ? name : $"{name}-{item.Style}";
    }

    // One name part: letters and digits in runs joined by single « _ » (no dots, no dashes, no slashes).
    private static readonly Regex Token = new("^[A-Za-z0-9]+(?:_[A-Za-z0-9]+)*$", RegexOptions.CultureInvariant);

    /// <summary>Whether <paramref name="s"/> is safe as one part of a look's name and of a layer's path.</summary>
    public static bool IsToken(string? s) => s is not null && Token.IsMatch(s);

    /// <summary>The look <paramref name="fileName"/> names (the inverse of <see cref="FileName"/>); null for anything else.</summary>
    public static PlynlingLook? Parse(string? fileName)
    {
        if (fileName is null || !fileName.EndsWith(Extension, StringComparison.Ordinal)) return null;
        var parts = fileName[..^Extension.Length].Split("__");
        if (parts.Length < 3 || parts[0] != Prefix || !IsToken(parts[1]) || !IsToken(parts[2])) return null;
        var i = 3;
        var dirty = i < parts.Length && parts[i] == "dirty";
        if (dirty) i++;
        string? aura = null;
        if (parts[^1].StartsWith(AuraTag, StringComparison.Ordinal))
        {
            aura = parts[^1][AuraTag.Length..];
            if (!IsToken(aura)) return null;
        }
        var end = parts.Length - (aura is null ? 0 : 1);
        var items = new List<WornAccessory>();
        for (; i < end; i++)
        {
            if (parts[i].StartsWith(AuraTag, StringComparison.Ordinal)) return null;     // an aura is last, never an item
            var dash = parts[i].IndexOf('-');
            var (key, style) = dash < 0 ? (parts[i], null) : (parts[i][..dash], parts[i][(dash + 1)..]);
            if (!IsToken(key) || (style is not null && !IsToken(style))) return null;
            items.Add(new WornAccessory(AccessoryPrefix + key, style));
        }
        if (items.Count > 2) return null;
        var look = new PlynlingLook(parts[1], parts[2], dirty, items.ElementAtOrDefault(0), items.ElementAtOrDefault(1), aura);
        // Only the canonical spelling: anything else (an aura before an item, a stray segment) is not a look.
        return look.FileName == fileName ? look : null;
    }
}
