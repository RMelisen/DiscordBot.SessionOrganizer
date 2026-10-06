namespace ProjectSYNCS.Models;

// Stored as an int, so this is **append-only**: a new species goes at the end, never in
// between, or every existing row silently changes species. Which family a species belongs
// to is PlynlingCatalog's business, not a column.
public enum PlynlingSpecies
{
    // Mystique and Dore now show as « Mycène » and « Girolle » (PlynlingCatalog): only the
    // display names changed — the values are stored and the art files are keyed on them.
    Amanite, Cepe, Rose, Russule, Mystique, Dore,                   // mushrooms
    Tournesol, Citron, Roux, Ivoire, Nocturne, Solaire,             // sunflowers
    Coprin,                                                         // a mushroom, appended later
}

// Rolled 50/50 at adoption and never changed. Male is 0 so the column's default is a real
// value for the rows that predate it.
public enum PlynlingGender { Male, Female }

// Why it died. Stored as an int: **append-only**. Starvation is 0, so every row that predates
// illness reads right.
public enum DeathCause { Starvation, Illness }

// A Plynling's innate passion (Helpers/PlynlingPassions). Stored as an int: **append-only** —
// a value inserted in the middle would turn every later Plynling's passion into its neighbour's.
public enum PlynlingPassion { Cooking, Music, Gaming, Astronomy, Gardening, Rocks, Stories, Dance, Painting, Sport, Insects, Naps }

// One Plynling, alive or dead. A death does not create a second row: DiedAt is set and
// the row is what the graveyard lists; a resurrection clears DiedAt on the same row.
//
// **Nothing here is a live value.** Hunger and happiness are stored as they were at
// NeedsAsOf, and Helpers/PlynlingLife derives the current value from the time elapsed —
// the same idea as the voice-XP taper, which works from minutes already banked. No job
// ticks anything down and a restart loses nothing. Every transition (feeding, freezing,
// thawing, dying) *rebases*: it computes the current values, stores them with a new
// NeedsAsOf, and carries on from there.
public class Plynling
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;
    public PlynlingSpecies Species { get; set; }
    // Decides the French: a female one is "une Plynling", "gelée", "morte".
    public PlynlingGender Gender { get; set; }
    // Rolled at adoption, never changes. Rows older than passions were backfilled from the id.
    public PlynlingPassion Passion { get; set; }
    // What its owner taught it (/plynling passion): free text, cleaned, hostile input like the name.
    public string? TaughtPassion { get; set; }
    // When it was last taught or cleared — the start of the change cooldown.
    public DateTimeOffset? TaughtPassionAt { get; set; }
    public DateTimeOffset AdoptedAt { get; set; }

    // 0..1, as of NeedsAsOf. While frozen or dead they are simply the stored values.
    public double Hunger { get; set; }
    public double Happiness { get; set; }
    // Below PlynlingLife.DirtyBelow it is « sale ». Starts clean.
    public double Hygiene { get; set; } = 1.0;
    public DateTimeOffset NeedsAsOf { get; set; }

    // Age = AgeBankedSeconds, plus the current stretch since LiveSince while alive and
    // not frozen. Frozen or dead time is never added — the memorial tier is measured on
    // time actually lived.
    public long AgeBankedSeconds { get; set; }
    public DateTimeOffset LiveSince { get; set; }

    public DateTimeOffset? FrozenAt { get; set; }
    // When a self-freeze thaws on its own. Null for a staff freeze, which only staff lift.
    public DateTimeOffset? FreezeUntil { get; set; }
    public bool FrozenByStaff { get; set; }
    // When the owner's last self-freeze ended — the 7-day cooldown counts from here.
    public DateTimeOffset? LastSelfThawAt { get; set; }

    // The single ~3h warning DM. Re-armed when feeding pushes death back past the lead.
    public bool WarningSent { get; set; }

    // What it has done, kept for the achievements and the journal: games played with its
    // owner (/plynling play), how many of them were won, and visits made or received
    // (/plynling visit, which counts for both). Recorded from the day they shipped.
    public long Plays { get; set; }
    public long PlaysWon { get; set; }
    public long Visits { get; set; }

    // Care received, for the badges: meals (whoever paid), pets (from anyone), and meals paid
    // by someone other than the owner. Recorded from the day they shipped, like the above.
    public long Meals { get; set; }
    public long Pets { get; set; }
    public long FedByOthers { get; set; }

    // What events have taught it, per stat (Helpers/PlynlingStats). The rest of a stat is computed —
    // a hashed die, its passion, its traits — so only what was earned is stored. 0 until events exist.
    public int GrowthDiplomacy { get; set; }
    public int GrowthStewardship { get; set; }
    public int GrowthLearning { get; set; }
    public int GrowthIntrigue { get; set; }
    public int GrowthCourage { get; set; }

    // The Paris day (AppTime.DayKey) of its last happy-gift draw, win or lose — one a day,
    // stored so a restart cannot grant a second. 0 = never.
    public int LastGiftDay { get; set; }

    // The last 05:00 Paris morning already played (yyyymmdd, like AppTime.DayKey):
    // PlynlingLife.Settle plays every later one, in order, exactly once.
    public int LastMorningDay { get; set; }

    // Sickness (Helpers/PlynlingSickness): the morning it fell ill (null when healthy), the hidden
    // 0..100 healing bar, its last dose of medicine, and whether its owner has been told.
    public DateTimeOffset? SickSince { get; set; }
    public int Recovery { get; set; }
    public DateTimeOffset? LastMedicineAt { get; set; }
    public bool SickNotified { get; set; }

    public DateTimeOffset? DiedAt { get; set; }
    // Set by the sweep once the death has been announced (or attempted), so it is never
    // announced twice. A death found lazily by a command is announced by the next sweep.
    public bool DeathAnnounced { get; set; }
    public DeathCause DeathCause { get; set; }

    // What it wears — CosmeticCatalog keys, one per slot, null for none. The cosmetics belong to
    // the owner's inventory; wearing uses nothing up. Kept on a dead row, which is how its grave
    // shows its frame; InventoryService takes one off a *living* Plynling when its owner no
    // longer holds any.
    public string? ThemeKey { get; set; }
    public string? TitleKey { get; set; }
    public string? AccessoryKey { get; set; }
    public string? GraveKey { get; set; }

    // Moments a Settle produced (fell sick, recovered) that the journal still has to record.
    // Settle is pure, so whoever saves the row writes them (PlynlingService.FlushMomentsAsync).
    // Never stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<(Helpers.JournalKind Kind, DateTimeOffset At)> PendingMoments { get; } = new();
}
