namespace ProjectSYNCS.Helpers;

/// <summary>
/// The weekly lottery's rules (/arcade lottery). Pure, so the Sunday edge and the DST weekends are
/// checkable without a clock.
/// </summary>
/// <remarks>
/// Tickets are debited at purchase. The draw is on Sunday at 21:00 Paris; a ticket bought after it
/// belongs to the next Sunday. SYNCS holds one free ticket, the last of <c>total + 1</c>, and it adds
/// one ticket's worth to the pot (house money, owner's call), so a lone player can win more than
/// they paid. The winner takes <see cref="Payout"/>, the rest is burned; if her ticket wins, the
/// whole pot is.
/// </remarks>
public static class Lottery
{
    public const long TicketPrice = 20;
    public const int MaxPerBuy = 500;
    public static readonly TimeSpan DrawTime = new(21, 0, 0);

    /// <summary>The pot for <paramref name="tickets"/> bought: theirs plus her free one, nothing when nobody played.</summary>
    public static long Pot(long tickets) => tickets <= 0 ? 0 : TicketPrice * (tickets + 1);

    /// <summary>What the winner takes: three quarters, rounded down; the house burns the rest.</summary>
    public static long Payout(long pot) => pot * 3 / 4;

    /// <summary>The day key of the Sunday whose 21:00 Paris is the next draw strictly after <paramref name="now"/>.</summary>
    public static int DrawDay(DateTimeOffset now)
    {
        var today = AppTime.ToZoned(now).Date;
        var sunday = today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7);
        var key = DayKeyOf(sunday);
        return DrawAt(key) > now ? key : DayKeyOf(sunday.AddDays(7));
    }

    /// <summary>The instant of the draw on <paramref name="drawDay"/>: 21:00 Paris that day.</summary>
    public static DateTimeOffset DrawAt(int drawDay) => AppTime.AtWallClock(AppTime.FromDayKey(drawDay) + DrawTime);

    /// <summary>
    /// The winning index for <paramref name="roll"/> in <c>[0, sum + 1)</c>: each player's tickets
    /// are a range, in order; −1 is her ticket, the last.
    /// </summary>
    public static int Pick(IReadOnlyList<int> tickets, int roll)
    {
        for (var i = 0; i < tickets.Count; i++)
        {
            if (roll < tickets[i]) return i;
            roll -= tickets[i];
        }
        return -1;
    }

    private static int DayKeyOf(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;
}
