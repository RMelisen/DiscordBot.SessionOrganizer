using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The per-Plynling counters behind /plynling stats (docs/agents/plynling.md, *Stats*). Every key is stored, so
/// **append-only, never renamed**. The writers never save: they ride the action's own save, beside its
/// EconomyLog and PlynlingScore calls, so a refused action counts nothing. Settle is pure, so what it finds
/// (a sick morning, an expired freeze) goes on Plynling.PendingCounters for FlushMomentsAsync.
/// </summary>
public static class PlynlingCounters
{
    /// <summary>The day counting started. Older Plynlings show « depuis le 26/11 » beside these counts.
    /// A property, not a field: AppTime's zone lives in another file (cross-file static order).</summary>
    public static DateTimeOffset Since => AppTime.AtWallClock(new DateTime(2026, 11, 26));

    public static bool IsPartial(Plynling p) => p.AdoptedAt < Since;

    // Care
    public const string MealPantry = "meal.pantry";
    public const string MealSpentOwner = "meal.spent.owner";
    public const string MealSpentOthers = "meal.spent.others";
    public const string PetOwner = "pet.owner";
    public const string PetOthers = "pet.others";
    public const string BathFree = "bath.free";
    public const string BathSoap = "bath.soap";
    public const string Medicine = "medicine";

    // Health
    public const string SickDays = "sick.days";
    public const string Warning = "warning";
    public const string FrozenSeconds = "frozen.seconds";

    // Gifts, outings, finds
    public const string GiftCailloux = "gift.cailloux";
    public const string GiftItems = "gift.items";
    public const string GiftCaillouxTotal = "gift.cailloux.total";
    public const string Forage = "forage";
    public const string FindForage = "find.forage";
    public const string FindPlay = "find.play";
    public const string FindVisit = "find.visit";
    public const string FindGift = "find.gift";
    public const string FindEvent = "find.event";

    // Visits
    public const string VisitMade = "visit.made";
    public const string VisitGood = "visit.good";
    public const string ConfessMade = "confess.made";
    public const string ConfessAccepted = "confess.accepted";
    public const string ConfessRefused = "confess.refused";
    public const string ConfessReceived = "confess.received";

    // Cailloux earned for its owner
    public const string EarnGame = "earn.game";
    public const string EarnBadge = "earn.badge";
    public const string EarnEvent = "earn.event";

    public static string Food(PlynlingFood food) => "meal.food." + food.ToString().ToLowerInvariant();
    public static string Game(PlynlingGame game) => "play." + game.ToString().ToLowerInvariant();
    public static string GameWon(PlynlingGame game) => Game(game) + ".won";
    /// <summary>Drills of one body stat (8.27.0). Built from the enum name like Food and Game: renaming a
    /// PlynlingStat member orphans its count.</summary>
    public static string Drill(PlynlingStat stat) => "drill." + stat.ToString().ToLowerInvariant();

    /// <summary>Every key, the per-food, per-game and per-drill ones expanded.</summary>
    public static IReadOnlyList<string> All => new[]
        {
            MealPantry, MealSpentOwner, MealSpentOthers, PetOwner, PetOthers, BathFree, BathSoap, Medicine,
            SickDays, Warning, FrozenSeconds,
            GiftCailloux, GiftItems, GiftCaillouxTotal, Forage, FindForage, FindPlay, FindVisit, FindGift, FindEvent,
            VisitMade, VisitGood, ConfessMade, ConfessAccepted, ConfessRefused, ConfessReceived,
            EarnGame, EarnBadge, EarnEvent,
        }
        .Concat(Enum.GetValues<PlynlingFood>().Select(Food))
        .Concat(Enum.GetValues<PlynlingGame>().SelectMany(g => new[] { Game(g), GameWon(g) }))
        .Concat(PlynlingStats.Body.Select(Drill))
        .ToList();

    /// <summary>Adds <paramref name="amount"/> to one counter. Not saved: the caller's save carries it.</summary>
    public static async Task AddAsync(AppDbContext db, Plynling p, string key, long amount)
    {
        if (amount <= 0) return;
        var row = db.PlynlingCounters.Local.FirstOrDefault(r => r.PlynlingId == p.Id && r.Key == key)
                  ?? await db.PlynlingCounters.FirstOrDefaultAsync(r => r.PlynlingId == p.Id && r.Key == key);
        if (row is null)
        {
            row = new PlynlingCounter { PlynlingId = p.Id, Key = key };
            db.PlynlingCounters.Add(row);
        }
        row.Value += amount;
    }

    /// <summary>Adds meals and pets to one person's care of this Plynling. Not saved, like AddAsync.</summary>
    public static async Task AddCarerAsync(AppDbContext db, Plynling p, ulong userId, long meals, long pets)
    {
        if (meals <= 0 && pets <= 0) return;
        var row = db.PlynlingCarers.Local.FirstOrDefault(r => r.PlynlingId == p.Id && r.UserId == userId)
                  ?? await db.PlynlingCarers.FirstOrDefaultAsync(r => r.PlynlingId == p.Id && r.UserId == userId);
        if (row is null)
        {
            row = new PlynlingCarer { PlynlingId = p.Id, UserId = userId };
            db.PlynlingCarers.Add(row);
        }
        row.Meals += meals;
        row.Pets += pets;
    }
}
