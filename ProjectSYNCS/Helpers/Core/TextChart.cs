namespace ProjectSYNCS.Helpers;

/// <summary>
/// The text-drawn charts of her staff views: /admin dashboard and /report draw the same way.
/// Embeds can't carry pictures we draw (no image library on the Pi), so a trend is a line of block
/// characters, shown in backticks so every character keeps the same width.
/// </summary>
public static class TextChart
{
    private const string Bars = "▁▂▃▄▅▆▇█";
    public const int SparklineMax = 30;

    /// <summary>
    /// One character per day, scaled to the series' own maximum; past <see cref="SparklineMax"/> days
    /// the days are summed into that many buckets, so « Tout » stays one line.
    /// </summary>
    public static string Sparkline(IReadOnlyList<long> values)
    {
        var n = values.Count;
        var buckets = Math.Min(SparklineMax, n);
        var sums = new long[buckets];
        for (var b = 0; b < buckets; b++)
            for (var i = b * n / buckets; i < (b + 1) * n / buckets; i++)
                sums[b] += values[i];
        var max = sums.Length == 0 ? 0 : sums.Max();
        return new string(sums.Select(v => max <= 0 ? Bars[0]
            : Bars[(int)Math.Round(v * 7.0 / max, MidpointRounding.ToZero)]).ToArray());
    }

    /// <summary>The trend against the previous window: « +12 % », « −8 % », « = », « nouveau », or nothing.</summary>
    public static string Delta(long current, long? previous)
    {
        if (previous is not { } p) return "";
        if (p == 0) return current > 0 ? "nouveau" : "";
        var pct = (long)Math.Round((current - p) * 100.0 / p);
        return pct == 0 ? "=" : pct > 0 ? $"+{pct} %" : $"−{-pct} %";
    }

    /// <summary><see cref="Delta"/> in brackets after a figure, or nothing when there is no trend.</summary>
    public static string Trend(long current, long? previous) =>
        Delta(current, previous) is { Length: > 0 } t ? $" ({t})" : "";

    /// <summary>A whole percentage of <paramref name="part"/> in <paramref name="total"/>, « — » when there is no total.</summary>
    public static string Percent(long part, long total) =>
        total <= 0 ? "—" : $"{Math.Round(part * 100.0 / total)} %";
}
