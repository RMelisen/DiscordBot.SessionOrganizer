using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The day-by-day record of the economy behind /admin dashboard. Every place cailloux or items
/// move calls <see cref="Add"/> in its own unit of work: the helper never saves, so the count rides
/// the action's SaveChanges — a refused or failed action records nothing, and every count matches
/// something that happened.
/// </summary>
/// <remarks>Metric keys are stored: append-only, never renamed.</remarks>
public static class EconomyLog
{
    // Cailloux earned, by source.
    public const string EarnWork = "earn.work";
    public const string EarnPassive = "earn.passive";
    public const string EarnGame = "earn.game";
    public const string EarnGift = "earn.gift";
    public const string EarnBadge = "earn.badge";
    public const string EarnCollection = "earn.collection";
    public const string EarnSale = "earn.sale";
    public const string EarnAdmin = "earn.admin";

    // Cailloux spent, by sink.
    public const string SpendShop = "spend.shop";
    public const string SpendMeal = "spend.meal";
    public const string SpendMealOther = "spend.meal_other";
    public const string SpendCosmetic = "spend.cosmetic";
    public const string SpendCraft = "spend.craft";
    public const string SpendAdmin = "spend.admin";
    public const string SpendMedicine = "spend.medicine";

    // What people did.
    public const string ActMeal = "act.meal";
    public const string ActPet = "act.pet";
    public const string ActGame = "act.game";
    public const string ActVisit = "act.visit";
    public const string ActForage = "act.forage";
    public const string ActTrade = "act.trade";
    public const string ActGive = "act.give";
    public const string ActBath = "act.bath";
    public const string ActMedicine = "act.medicine";

    // What they found and bought.
    public const string ItemFound = "item.found";
    public const string ItemSet = "item.set";
    public const string CosBought = "cos.bought";
    public const string CosCrafted = "cos.crafted";

    public static readonly IReadOnlyList<string> Earnings = new[] { EarnWork, EarnPassive, EarnGame, EarnGift, EarnBadge, EarnCollection, EarnSale, EarnAdmin };
    public static readonly IReadOnlyList<string> Spendings = new[] { SpendShop, SpendMeal, SpendMealOther, SpendCosmetic, SpendCraft, SpendAdmin, SpendMedicine };
    public static readonly IReadOnlyList<string> Activities = new[] { ActMeal, ActPet, ActGame, ActVisit, ActForage, ActTrade, ActGive, ActBath, ActMedicine };
    public static readonly IReadOnlyList<string> Findings = new[] { ItemFound, ItemSet, CosBought, CosCrafted };

    /// <summary>Adds <paramref name="amount"/> to today's counter. Not saved: the caller's save carries it.</summary>
    public static async Task AddAsync(AppDbContext db, ulong guildId, string metric, long amount, DateTimeOffset now)
    {
        if (amount == 0) return;
        var day = AppTime.DayKey(now);
        var row = db.EconomyDailyStats.Local.FirstOrDefault(r => r.GuildId == guildId && r.Day == day && r.Metric == metric)
                  ?? await db.EconomyDailyStats.FirstOrDefaultAsync(r => r.GuildId == guildId && r.Day == day && r.Metric == metric);
        if (row is null)
        {
            row = new EconomyDailyStat { GuildId = guildId, Day = day, Metric = metric };
            db.EconomyDailyStats.Add(row);
        }
        row.Value += amount;
    }
}
