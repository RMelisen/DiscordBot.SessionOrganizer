using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>A Steam game that is free to keep right now, as her card shows it.</summary>
public sealed record SteamFreeGame(uint AppId, string Name, string Description, string HeaderImage, string NormalPrice)
{
    public string StoreUrl => $"https://store.steampowered.com/app/{AppId}/";
}

/// <summary>
/// Reading Steam's two public store endpoints, no key needed: the search (everything a full 100 %
/// off right now) and the app details (what each one is). Pure, so a misread is checkable offline.
/// </summary>
/// <remarks>
/// « Free to keep » is a paid game at a 100 % discount. Free-to-play games never match: Steam lists
/// them as free, not as discounted. DLC, soundtracks and bundles are dropped by the details check.
/// </remarks>
public static class SteamFreeGames
{
    /// <summary>Games only (category 998), discounted, priced free, French prices.</summary>
    public const string SearchUrl =
        "https://store.steampowered.com/search/results/?maxprice=free&specials=1&category1=998"
        + "&infinite=1&count=50&start=0&cc=fr&l=french";

    public static string DetailsUrl(uint appId) =>
        $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=fr&l=french";

    // One app per row. A bundle lists several ids comma-separated, which the closing quote excludes.
    private static readonly Regex AppIdRegex = new("data-ds-appid=\"(\\d+)\"", RegexOptions.Compiled);

    /// <summary>The app ids in a search response (its rows come as HTML inside the JSON).</summary>
    public static IReadOnlyList<uint> ParseSearch(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results_html", out var html) || html.ValueKind != JsonValueKind.String)
            return Array.Empty<uint>();

        return AppIdRegex.Matches(html.GetString() ?? "")
            .Select(m => uint.TryParse(m.Groups[1].Value, out var id) ? id : 0)
            .Where(id => id != 0)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// The game behind an app-details response, or null when it isn't a paid game at 100 % off
    /// (a DLC, a free-to-play game, a discount that ended, an app Steam won't describe).
    /// </summary>
    public static SteamFreeGame? ParseDetails(uint appId, string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(appId.ToString(), out var entry)
            || !entry.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True
            || !entry.TryGetProperty("data", out var data))
            return null;

        if (Text(data, "type") != "game") return null;
        if (data.TryGetProperty("is_free", out var isFree) && isFree.ValueKind == JsonValueKind.True) return null;

        if (!data.TryGetProperty("price_overview", out var price)
            || !price.TryGetProperty("discount_percent", out var discount)
            || discount.ValueKind != JsonValueKind.Number || discount.GetInt32() != 100)
            return null;

        var name = System.Net.WebUtility.HtmlDecode(Text(data, "name"));
        if (string.IsNullOrWhiteSpace(name)) return null;

        return new SteamFreeGame(appId, name.Trim(),
            System.Net.WebUtility.HtmlDecode(Text(data, "short_description")).Trim(),
            Text(data, "header_image"),
            Text(price, "initial_formatted"));
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
