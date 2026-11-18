namespace ProjectSYNCS.Models;

// One member's tickets for one weekly draw (/arcade lottery). Keyed by the draw's day (the Paris
// yyyymmdd of its Sunday, Helpers/Arcade/Lottery.DrawDay), not by ISO week: a ticket bought on
// Sunday after the draw belongs to the next one. The cailloux are debited at purchase.
public class LotteryEntry
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int DrawDay { get; set; }
    public ulong UserId { get; set; }
    public int Tickets { get; set; }
}

// A draw that happened. Unique per (guild, day): the guard that makes a draw happen once, even when
// two passes or a restart reach it.
public class LotteryDraw
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int DrawDay { get; set; }

    // The winner; 0 when her own free ticket won (nothing paid, the whole pot burned).
    public ulong WinnerId { get; set; }

    public long Pot { get; set; }
    public long Paid { get; set; }
    public int Tickets { get; set; }
    public DateTimeOffset DrawnAt { get; set; }
}
