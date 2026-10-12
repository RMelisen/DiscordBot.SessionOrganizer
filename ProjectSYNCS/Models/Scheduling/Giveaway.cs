namespace ProjectSYNCS.Models;

// A prize draw: people enter by clicking a button, and when EndsAt passes a
// background sweep picks the winners at random.
//
// EndsAt is an absolute instant rather than a duration, so a restart loses nothing —
// the sweep asks "what is due now", not "how long has this been running". Both it and
// CreatedAt are UTC DateTimeOffsets, which SQLite cannot compare in a query: every
// read that filters on them pulls the open rows first and applies the cutoff in
// memory. See GiveawayService.
public class Giveaway
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }

    // Named to match SessionEvent and Poll so SessionPermissions.CanManage can treat
    // all three the same way.
    public ulong OrganizerId { get; set; }

    public string Prize { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // How many entrants to draw. Fewer are drawn if fewer entered.
    public int WinnerCount { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EndsAt { get; set; }

    // Set once the draw has happened — and the guard that stops it happening twice.
    public bool IsClosed { get; set; } = false;

    public List<GiveawayEntry> Entries { get; set; } = new();
}

// One person's participation. A winner is by definition an entrant, so the result of
// the draw is recorded here rather than in a second table: no join, and the drawn set
// survives restarts and every later re-render of the card.
public class GiveawayEntry
{
    public int Id { get; set; }
    public int GiveawayId { get; set; }

    public ulong UserId { get; set; }
    public DateTimeOffset EnteredAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsWinner { get; set; } = false;

    public Giveaway Giveaway { get; set; } = null!;
}
