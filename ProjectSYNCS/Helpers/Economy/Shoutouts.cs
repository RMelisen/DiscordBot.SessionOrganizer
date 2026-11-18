namespace ProjectSYNCS.Helpers;

/// <summary>
/// /shoutout's rules: an auction for a dedication in a morning hello. Bids for a day close at 20:00
/// Paris the evening before; until then a bid is for tomorrow, from 20:00 for the day after, so an
/// auction is always open. A bid must beat the leader by <see cref="MinRaise"/>, so two bids never tie.
/// </summary>
public static class Shoutouts
{
    public static readonly TimeSpan CloseAt = new(20, 0, 0);
    public const long MinOpening = 50;
    public const long MinRaise = 10;
    public const long MaxBid = 1_000_000;

    /// <summary>The Paris yyyymmdd of the hello a bid placed at <paramref name="now"/> is for.</summary>
    public static int OpenDay(DateTimeOffset now)
    {
        var local = AppTime.ToZoned(now);
        return Key(local.Date.AddDays(local.TimeOfDay < CloseAt ? 1 : 2));
    }

    /// <summary>When bidding for <paramref name="forDay"/> stops: 20:00 Paris the day before.</summary>
    public static DateTimeOffset ClosesAt(int forDay) =>
        AppTime.AtWallClock(AppTime.FromDayKey(forDay).AddDays(-1) + CloseAt);

    /// <summary>The least a bid must be: the opening bid, or the leader's plus a raise.</summary>
    public static long Minimum(long? leading) => leading is { } l ? l + MinRaise : MinOpening;

    private static int Key(DateTime day) => day.Year * 10000 + day.Month * 100 + day.Day;
}
