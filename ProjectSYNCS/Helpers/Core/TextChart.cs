namespace ProjectSYNCS.Helpers;

/// <summary>
/// The text-drawn charts of her staff views: /admin dashboard and /report draw the same way.
/// Embeds can't carry pictures we draw (no image library on the Pi), so a trend is a line of block
/// characters, shown in backticks so every character keeps the same width.
/// </summary>
public static class TextChart
{
    private const string Bars = "▁▂▃▄▅▆▇█";
    public const int SparklineMax = 31;

    /// <summary>
    /// One character per day, scaled to the series' own maximum; past <see cref="SparklineMax"/> days
    /// the days are averaged into that many buckets, so « Tout » stays one line. Averaged, not summed:
    /// buckets can hold different numbers of days, and a sum would draw the longer ones as surges.
    /// </summary>
    public static string Sparkline(IReadOnlyList<long> values)
    {
        var n = values.Count;
        var buckets = Math.Min(SparklineMax, n);
        var means = new double[buckets];
        for (var b = 0; b < buckets; b++)
        {
            var from = b * n / buckets;
            var to = (b + 1) * n / buckets;
            for (var i = from; i < to; i++)
                means[b] += values[i];
            means[b] /= to - from;
        }
        var max = means.Length == 0 ? 0 : means.Max();
        return new string(means.Select(v => max <= 0 ? Bars[0]
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

    /// <summary>
    /// <see cref="Delta(long, long?)"/> per counted day: the previous figure is scaled to the current
    /// window's days first, so a month counted for 13 days doesn't make the next one look like growth.
    /// </summary>
    public static string Delta(long current, long? previous, ReportPace pace)
    {
        if (previous is not { } p) return "";
        if (p == 0 || pace.PreviousDays <= 0) return current > 0 ? "nouveau" : "";
        if (pace.Days <= 0) return "";
        var before = p * (double)pace.Days / pace.PreviousDays;
        var pct = (long)Math.Round((current - before) * 100.0 / before);
        return pct == 0 ? "=" : pct > 0 ? $"+{pct} %" : $"−{-pct} %";
    }

    /// <summary><see cref="Delta(long, long?, ReportPace)"/> in brackets after a figure, or nothing when there is no trend.</summary>
    public static string Trend(long current, long? previous, ReportPace pace) =>
        Delta(current, previous, pace) is { Length: > 0 } t ? $" ({t})" : "";

    /// <summary>
    /// A fill bar, « ▰▰▰▱▱▱ »: <paramref name="width"/> cells, <paramref name="part"/> of
    /// <paramref name="total"/> filled, rounded. Any part above zero shows at least one cell, so a
    /// single vote never reads as none. All empty when there is no total.
    /// </summary>
    public static string Bar(long part, long total, int width)
    {
        if (width <= 0) return string.Empty;
        var filled = total <= 0 || part <= 0 ? 0
            : Math.Clamp((int)Math.Round(part * (double)width / total), 1, width);
        return new string('▰', filled) + new string('▱', width - filled);
    }

    /// <summary>A whole percentage of <paramref name="part"/> in <paramref name="total"/>, « — » when there is no total.</summary>
    public static string Percent(long part, long total) =>
        total <= 0 ? "—" : $"{Math.Round(part * 100.0 / total)} %";
}
