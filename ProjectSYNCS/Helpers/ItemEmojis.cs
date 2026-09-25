using System.Collections.Concurrent;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The custom emoji markup of items pictured by the bot's own application emojis (the
/// Champignons), by item key. Filled once the gateway is ready by
/// <see cref="Services.ApplicationEmojiService"/>; until then — and for any item missing from
/// it — <see cref="ItemInfo.Emoji"/> falls back to the catalog's Unicode emoji.
/// </summary>
/// <remarks>
/// Static like the catalog it decorates: <see cref="ItemCatalog"/> is a static table read from
/// everywhere, and threading a service through every place that prints an item would buy
/// nothing. In memory only; it is rebuilt from Discord on every start.
/// </remarks>
public static class ItemEmojis
{
    // An item's emoji is named after its sprite file, which is named after its key:
    // Assets/Mushrooms/girolle.png is col.girolle and the emoji shroom_girolle (32 characters max).
    public const string MushroomPrefix = "shroom_";

    private static readonly ConcurrentDictionary<string, string> ByKey = new();

    public static string? For(string itemKey) => ByKey.TryGetValue(itemKey, out var markup) ? markup : null;

    public static void Set(string itemKey, string markup) => ByKey[itemKey] = markup;

    public static int Count => ByKey.Count;

    public static string Markup(string name, ulong id) => $"<:{name}:{id}>";

    // For the checks only: back to the fallbacks.
    public static void Clear() => ByKey.Clear();
}
