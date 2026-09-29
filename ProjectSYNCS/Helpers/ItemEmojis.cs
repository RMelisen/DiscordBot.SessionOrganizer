using System.Collections.Concurrent;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The custom emoji markup of items and sets pictured by the bot's own application emojis — the
/// Champignons (Assets/Mushrooms, a third-party pack) and every other icon (Assets/Icons, drawn by
/// tools/item-art) — by icon key. Filled once the gateway is ready by
/// <see cref="Services.ApplicationEmojiService"/>; until then, and for any key missing from it,
/// each caller falls back to its catalog's Unicode emoji.
/// </summary>
/// <remarks>
/// Static like the catalog it decorates: <see cref="ItemCatalog"/> is a static table read from
/// everywhere, and threading a service through every place that prints an item would buy
/// nothing. In memory only; it is rebuilt from Discord on every start.
/// </remarks>
public static class ItemEmojis
{
    // A mushroom's emoji is named after its sprite file, which is named after its key:
    // Assets/Mushrooms/girolle.png is col.girolle and the emoji shroom_girolle.
    public const string MushroomPrefix = "shroom_";

    public static string MushroomDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Mushrooms");
    public static string IconDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

    // Sets are not items, so their icon keys get a prefix of their own.
    public static string SetKey(string setKey) => "set." + setKey;

    // Every other icon is Assets/Icons/<icon key>.png, and its emoji name swaps the key's prefix for
    // a short one: Discord allows [A-Za-z0-9_] and 32 characters, and « cos.accessory. » alone would
    // eat 14 of them. The longest today is ac_collier_coquillages (22).
    private static readonly (string Key, string Emoji)[] Prefixes =
    {
        ("col.", "c_"), ("set.", "s_"), ("cos.theme.", "th_"), ("cos.title.", "ti_"),
        ("cos.accessory.", "ac_"), ("cos.grave.", "gr_"),
    };

    private static readonly ConcurrentDictionary<string, string> ByKey = new();

    public static string? For(string itemKey) => ByKey.TryGetValue(itemKey, out var markup) ? markup : null;

    public static void Set(string itemKey, string markup) => ByKey[itemKey] = markup;

    public static int Count => ByKey.Count;

    public static string Markup(string name, ulong id) => $"<:{name}:{id}>";

    /// <summary>The emoji name for an icon in Assets/Icons; null for a key of no known kind.</summary>
    public static string? EmojiName(string iconKey)
    {
        foreach (var (key, emoji) in Prefixes)
            if (iconKey.StartsWith(key, StringComparison.Ordinal)) return emoji + iconKey[key.Length..];
        return null;
    }

    /// <summary>
    /// Every sprite the bot ships, with its icon key and emoji name — or a null name when the file
    /// matches nothing and must be skipped. Pure apart from listing the two folders, so the check
    /// reads them exactly as the bot does.
    /// </summary>
    public static IEnumerable<IconSource> Sources(string mushroomDir, string iconDir)
    {
        foreach (var file in PngsIn(mushroomDir))
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            var key = $"col.{slug}";
            yield return new IconSource(file, key, ItemCatalog.ByKey(key) is null ? null : MushroomPrefix + slug);
        }
        foreach (var file in PngsIn(iconDir))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            yield return new IconSource(file, key, ItemCatalog.IsIconKey(key) ? EmojiName(key) : null);
        }
    }

    private static string[] PngsIn(string dir) => Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png") : Array.Empty<string>();

    // For the checks only: back to the fallbacks.
    public static void Clear() => ByKey.Clear();
}

/// <summary>A sprite file, the icon key it pictures, and its emoji name (null: skip it).</summary>
public sealed record IconSource(string File, string Key, string? EmojiName);
