namespace ProjectSYNCS.Models;

// The leading bid for one morning hello's dedication (/shoutout bid). One row per (guild, day): a
// new leader overwrites it, and the one it replaces is refunded in the same save, so the row is
// always the winner-so-far. The cailloux are held from the bid (debited, not logged); they are
// logged as spent, and counted toward « Mécène », only when the dedication is posted
// (ShoutoutService.MarkShownAsync). Stored so a restart keeps the auction and the money.
public class ShoutoutBid
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    // The Paris yyyymmdd of the hello this bid is for. Bids close at 20:00 Paris the day before.
    public int ForDay { get; set; }

    public ulong UserId { get; set; }

    // The amount held. A concurrency token: two bids read the leader, the slower save must fail and
    // re-check against the new one rather than overwrite it (and lose a refund).
    public long Amount { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    // The Paris yyyymmdd it was posted on, 0 until then. Later than ForDay when it was carried over
    // her birthday, a silenced day, or the bot being down.
    public int ShownDay { get; set; }
}
