using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The hidden Plynling score (docs/agents/plynling.md, *Score*): every point value in one place, and the one
/// writer. Flat points per action — finer foods worth more, no daily cap (the owner's call) — plus the
/// milestones. The score buys nothing and shows only on /leaderboard's Plynlings view; it picks the tombstone
/// (<see cref="PlynlingCatalog.MemorialTier(Plynling)"/>).
/// </summary>
/// <remarks>
/// Points go to the Plynling cared for, whoever does the caring, and so to its <b>owner's</b> total, never the
/// helper's. The mascot never scores (everyone feeds her and she decides every event alone, so she would top the
/// board for good). Retune the values by re-running the calibration checks, never by feel: the tombstone
/// thresholds are measured against a well-cared life built from these numbers.
/// </remarks>
public static class PlynlingScore
{
    // ---- Actions ----------------------------------------------------------------------------------------------
    public const int Pet = 1;
    public const int Bath = 2;              // free or with soap
    public const int Medicine = 5;
    public const int Play = 3;              // a /plynling play game finished, won or lost
    public const int Visit = 4;             // each side
    public const int VisitGoodScene = 2;    // on top, each side
    public const int Forage = 2;
    public const int Drill = 2;
    public const int EventShort = 3;        // a C# event resolved, or a short story's run ending
    public const int EventLong = 10;        // a long story's ## ending:
    public const int Collection = 10;       // a set discovered or turned in
    public const int Cosmetic = 5;          // bought or crafted

    // ---- Milestones -------------------------------------------------------------------------------------------
    public const int TraitGained = 5;
    public const int Recovered = 10;
    public const int LevelUp = 5;           // per RPG level gained
    public const int GoodMorning = 3;

    // A good morning: well kept at 05:00. Hunger is checked lower than happiness because the night drains it.
    public const double GoodMorningHunger = 0.40;
    public const double GoodMorningHappiness = 0.50;

    /// <summary>A meal, by food: the finer the food, the more it is worth.</summary>
    public static int Meal(PlynlingFood food) => food switch
    {
        PlynlingFood.Mushroom => 1,
        PlynlingFood.Shiitake => 2,
        PlynlingFood.Morel => 3,
        PlynlingFood.Truffle => 5,
        _ => 1,
    };

    /// <summary>The average meal, for history that never stored which food it was.</summary>
    public const int MealAverage = 2;

    /// <summary>Growing into a stage. A bébé is where it starts, so it scores nothing.</summary>
    public static int Stage(PlynlingStage stage) => stage switch
    {
        PlynlingStage.Teen => 10,
        PlynlingStage.Adult => 20,
        PlynlingStage.Elder => 40,
        _ => 0,
    };

    /// <summary>
    /// A bond reached, for each side of the pair, once per pair and bond (<see cref="PlynlingRelation.ScoredBonds"/>).
    /// Rivals and enemies score too: a feud is a story. Drifting back to acquaintances is nothing.
    /// </summary>
    public static int Bond(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Friends => 10,
        PlynlingBond.BestFriends => 20,
        PlynlingBond.Lovers => 40,
        PlynlingBond.Rivals => 10,
        PlynlingBond.Enemies => 15,
        _ => 0,
    };

    /// <summary>The bit a bond takes in <see cref="PlynlingRelation.ScoredBonds"/>. PlynlingBond is append-only.</summary>
    public static int BondBit(PlynlingBond bond) => 1 << (int)bond;

    /// <summary>
    /// What a journal moment is worth, read where every moment is written (PlynlingService.AddMomentAsync): a badge
    /// its own cailloux reward, growing up its stage, a trait, a recovery. Everything else is 0 — the bond moments
    /// included, which score at the bond itself, once per pair.
    /// </summary>
    public static int ForMoment(JournalKind kind, string? detail) => kind switch
    {
        JournalKind.Badge => (int)(PlynlingBadges.ByKey(detail ?? "")?.Reward ?? 0),
        JournalKind.GrewUp => Enum.TryParse<PlynlingStage>(detail, out var stage) ? Stage(stage) : 0,
        JournalKind.TraitGained => TraitGained,
        JournalKind.Recovered => Recovered,
        _ => 0,
    };

    /// <summary>
    /// Whether the 05:00 <paramref name="morning"/> finds it well kept: not sick, fed, happy and clean. Pure, read
    /// by <see cref="PlynlingLife"/>'s morning before anything else that morning changes it.
    /// </summary>
    public static bool IsGoodMorning(Plynling p, DateTimeOffset morning) =>
        !PlynlingLife.IsSick(p)
        && PlynlingLife.HungerAt(p, morning) >= GoodMorningHunger
        && PlynlingLife.HappinessAt(p, morning) >= GoodMorningHappiness
        && PlynlingLife.HygieneAt(p, morning) >= PlynlingLife.DirtyBelow;

    /// <summary>
    /// Adds <paramref name="points"/> to the Plynling's total, the day's bucket (the Paris day of
    /// <paramref name="at"/>) and its owner's total. Never saves: the action's own SaveChanges carries it, so a
    /// refused or failed action scores nothing. The mascot and non-positive amounts are skipped.
    /// </summary>
    public static async Task AddAsync(AppDbContext db, Plynling p, long points, DateTimeOffset at)
    {
        if (points <= 0 || PlynlingMascot.Is(p)) return;
        p.Score += points;

        var day = AppTime.DayKey(at);
        var bucket = db.PlynlingScoreDays.Local.FirstOrDefault(b => b.PlynlingId == p.Id && b.Day == day)
                     ?? await db.PlynlingScoreDays.FirstOrDefaultAsync(b => b.PlynlingId == p.Id && b.Day == day);
        if (bucket is null)
        {
            bucket = new PlynlingScoreDay { PlynlingId = p.Id, GuildId = p.GuildId, Day = day };
            db.PlynlingScoreDays.Add(bucket);
        }
        bucket.Points += points;

        await AddToOwnerAsync(db, p.GuildId, p.OwnerId, points);
    }

    /// <summary>Adds to one person's lifetime total. Never saves.</summary>
    public static async Task AddToOwnerAsync(AppDbContext db, ulong guildId, ulong userId, long points)
    {
        if (points <= 0) return;
        var owner = db.PlynlingOwnerScores.Local.FirstOrDefault(o => o.GuildId == guildId && o.UserId == userId)
                    ?? await db.PlynlingOwnerScores.FirstOrDefaultAsync(o => o.GuildId == guildId && o.UserId == userId);
        if (owner is null)
        {
            owner = new PlynlingOwnerScore { GuildId = guildId, UserId = userId };
            db.PlynlingOwnerScores.Add(owner);
        }
        owner.Points += points;
    }

    /// <summary>
    /// Scores a person-level action (a collection, a cosmetic) to their living Plynling, if they have one. Used by
    /// the inventory and cosmetic services, which load no Plynling of their own; they run under
    /// EconomySave.RetryAsync, which retries the Plynling row's Version conflict like their own. Never saves.
    /// </summary>
    public static async Task AddToLivingAsync(AppDbContext db, ulong guildId, ulong userId, long points, DateTimeOffset at)
    {
        if (points <= 0) return;
        var p = db.Plynlings.Local.FirstOrDefault(x => x.GuildId == guildId && x.OwnerId == userId && x.DiedAt == null)
                ?? await db.Plynlings.FirstOrDefaultAsync(x => x.GuildId == guildId && x.OwnerId == userId && x.DiedAt == null);
        if (p is not null) await AddAsync(db, p, points, at);
    }
}
