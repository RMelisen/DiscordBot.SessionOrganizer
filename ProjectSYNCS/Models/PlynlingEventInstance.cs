using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Models;

// One event that happened (or is waiting) for a Plynling — the queue and the history in one table.
// Pending = neither resolved nor cancelled, and AvailableAt reached. EventKey and OptionKey are
// PlynlingEvents keys: stored, never renamed. Everything a story needs that the catalog cannot give
// back later (the chance shown, the roll, the bond before and after) is stored here, so a story is
// rebuilt from this row alone, after any restart.
public class PlynlingEventInstance
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public string EventKey { get; set; } = string.Empty;

    // The other Plynling of a social event; set null if that one is abandoned (deleted).
    public int? TargetPlynlingId { get; set; }
    // The event this one follows from (a follow-up, or a response to an ask).
    public int? ParentInstanceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? OptionKey { get; set; }
    public bool DecidedAlone { get; set; }
    public bool? ChallengeSucceeded { get; set; }
    public int? ChancePercent { get; set; }
    public PlynlingBond? BondBefore { get; set; }
    public PlynlingBond? BondAfter { get; set; }

    // The net stress this event caused (cost + effects), and the trait key(s) it involved — the coping
    // trait a break gave, or for a trait reveal the new traits as "key,key" (its {T}). Both depend on
    // state at the time, so the story stores them rather than recomputing.
    public int? StressDelta { get; set; }
    public string? GainedTraitKey { get; set; }

    // Whether its lesson (GrowStat) stuck for good. Null when it taught nothing — or when it was
    // resolved before lessons became a few days' Practice, when every +1 was permanent: the story tells
    // those as they happened.
    public bool? GrewForGood { get; set; }
}
