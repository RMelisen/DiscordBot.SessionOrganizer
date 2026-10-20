namespace ProjectSYNCS.Helpers;

/// <summary>One stack, as the body reports it: the game id, how many, and the kinds of goods it counts as
/// (<see cref="McGoods"/> in lower case, the same test the body uses to hand things over).</summary>
public sealed record McStack(string Id, int Count, IReadOnlyList<string> Goods);

/// <summary>
/// What she carries, as last reported by the body: what she wears, what's in her hand and her off hand, and her
/// pockets (the hotbar included, so the item in her hand is in there too). Pure: the lines she says about it are built
/// here, so the checks can measure them.
/// </summary>
public sealed record McInventory(IReadOnlyList<McStack> Worn, McStack? Held, McStack? Offhand, IReadOnlyList<McStack> Pockets)
{
    public static readonly McInventory Empty = new([], null, null, []);

    /// <summary>What she carries (pockets and off hand, not what she wears), summed by id, the biggest first.</summary>
    public IReadOnlyList<(string Id, int Count)> Carried() => Sum(Pockets.Concat(Offhand is { } off ? [off] : []));

    /// <summary>
    /// « 64 bûches de chêne, 23 pains, une pioche en fer et 9 autres trucs »: what she carries, the biggest first, as
    /// much as fits in <paramref name="budget"/> characters; the rest, and anything she has no name for, is « autres
    /// trucs ». Null when her pockets are empty.
    /// </summary>
    public string? Overview(int budget) => Phrase(Carried(), budget);

    /// <summary>
    /// « 12 lingots de fer et 5 fers bruts »: what she carries of one kind of goods, armour including what she wears, in
    /// <paramref name="budget"/> characters at most. Null when she has none.
    /// </summary>
    public string? Of(McGoods goods, int budget)
    {
        if (goods == McGoods.All) return Overview(budget);
        var key = goods.ToString().ToLowerInvariant();
        var source = Pockets.Concat(Offhand is { } off ? [off] : []);
        if (goods == McGoods.Armour) source = source.Concat(Worn);
        return Phrase(Sum(source.Where(s => s.Goods.Contains(key))), budget);
    }

    /// <summary>Stacks summed by id, the biggest first.</summary>
    public static IReadOnlyList<(string Id, int Count)> Sum(IEnumerable<McStack> stacks) =>
        stacks.Where(s => s.Count > 0)
            .GroupBy(s => s.Id)
            .Select(g => (Id: g.Key, Count: g.Sum(s => s.Count)))
            .OrderByDescending(t => t.Count).ThenBy(t => t.Id, StringComparer.Ordinal)
            .ToList();

    // The named ones in order while they fit, then « et N autres trucs » for the rest (left out or without a name).
    private static string? Phrase(IReadOnlyList<(string Id, int Count)> totals, int budget)
    {
        if (totals.Count == 0) return null;
        var named = totals.Select(t => McItems.Name(t.Id) is { } name ? name.Count(t.Count) : null).ToList();
        var shown = new List<string>();
        var left = totals.Count;
        foreach (var part in named)
        {
            if (part is null) continue;
            var rest = left - 1;
            var candidate = Join(shown.Append(part).ToList(), rest);
            // Keep room for the « et N autres » that would follow if this were the last one to fit.
            if (candidate.Length > budget) break;
            shown.Add(part);
            left--;
        }
        return Join(shown, left);
    }

    // « a, b et c », with « N autres trucs » as the last item when some are left out.
    private static string Join(List<string> parts, int others)
    {
        var all = new List<string>(parts);
        if (others > 0)
            all.Add(parts.Count == 0
                ? others == 1 ? "un truc" : $"{others} trucs"
                : others == 1 ? "un autre truc" : $"{others} autres trucs");
        return all.Count switch
        {
            0 => "",
            1 => all[0],
            _ => string.Join(", ", all.Take(all.Count - 1)) + " et " + all[^1],
        };
    }
}
