using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// What happened alongside an action that a badge can hinge on, beyond the counts.
public enum BadgeEvent { None, SavedFromStarving, Resurrected, BecameFriends, BecameBestFriends, BecameLovers }

/// <summary>
/// One badge. The key is stored on each PlynlingBadge row, so it is **stable**: renaming one
/// orphans every copy already earned. The name agrees with the Plynling (« Sauvée de
/// justesse »). The reward goes to the owner once, when the badge is earned.
/// </summary>
public sealed class BadgeInfo
{
    private readonly string _nameM;
    private readonly string _nameF;
    private readonly Func<Plynling, TimeSpan, BadgeEvent, bool> _earned;

    public BadgeInfo(string key, string emoji, string nameM, string nameF, long reward, Func<Plynling, TimeSpan, BadgeEvent, bool> earned)
    {
        Key = key;
        Emoji = emoji;
        _nameM = nameM;
        _nameF = nameF;
        Reward = reward;
        _earned = earned;
    }

    public string Key { get; }
    public string Emoji { get; }
    public long Reward { get; }

    public string Name(PlynlingGender gender) => gender.Agree(_nameM, _nameF);

    public bool IsEarned(Plynling p, TimeSpan age, BadgeEvent evt) => _earned(p, age, evt);
}

// Every badge a Plynling can earn — its own, not its owner's: they follow it to the graveyard.
// Checked after each action that can earn one, in the same save (PlynlingService), and hourly
// by the sweep for what time earns.
public static class PlynlingBadges
{
    public static readonly IReadOnlyList<BadgeInfo> All = new[]
    {
        Lived("week", "🌱", "Une semaine", 10, 7),
        Lived("month", "🌿", "Un mois", 20, 30),
        Lived("six_months", "🌳", "Six mois", 40, 180),
        Lived("year", "☀️", "Un an", 50, 365),
        Count("first_game", "🎲", "Première partie", 10, p => p.Plays, 1),
        Count("wins_10", "🎯", "10 victoires", 20, p => p.PlaysWon, 10),
        Count("wins_50", "🏆", "50 victoires", 40, p => p.PlaysWon, 50),
        Count("games_100", "🎮", "100 parties", 40, p => p.Plays, 100),
        Count("first_visit", "🏡", "Première visite", 10, p => p.Visits, 1),
        Count("visits_10", "💞", "10 visites", 20, p => p.Visits, 10),
        Count("visits_30", "🌍", "30 visites", 40, p => p.Visits, 30),
        Count("meals_100", "🍄", "100 repas", 20, p => p.Meals, 100),
        Count("pets_100", "🤲", "100 caresses", 20, p => p.Pets, 100),
        new BadgeInfo("fed_by_friend", "🎁", "Nourri par un ami", "Nourrie par un ami", 10, (p, _, _) => p.FedByOthers >= 1),
        new BadgeInfo("saved", "😮‍💨", "Sauvé de justesse", "Sauvée de justesse", 20, (_, _, e) => e == BadgeEvent.SavedFromStarving),
        // Pays nothing: only staff can resurrect, so it cannot be something anyone earns on purpose.
        new BadgeInfo("resurrected", "✨", "Revenu d'entre les morts", "Revenue d'entre les morts", 0, (_, _, e) => e == BadgeEvent.Resurrected),
        // Relationships: a first friend (reaching any closer bond counts too), a best friend, a couple.
        new BadgeInfo("first_friend", "🤝", "Premier ami", "Première amie", 10,
            (_, _, e) => e is BadgeEvent.BecameFriends or BadgeEvent.BecameBestFriends or BadgeEvent.BecameLovers),
        new BadgeInfo("best_friend", "💛", "Meilleur ami", "Meilleure amie", 20,
            (_, _, e) => e is BadgeEvent.BecameBestFriends or BadgeEvent.BecameLovers),
        new BadgeInfo("couple", "💞", "En couple", "En couple", 30, (_, _, e) => e == BadgeEvent.BecameLovers),
    };

    private static readonly Dictionary<string, BadgeInfo> ByKeyMap = All.ToDictionary(b => b.Key);

    public static BadgeInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    /// <summary>The badges this Plynling qualifies for now and has not earned yet, in catalog order.</summary>
    public static List<BadgeInfo> Newly(Plynling p, IReadOnlySet<string> earned, DateTimeOffset now, BadgeEvent evt = BadgeEvent.None)
    {
        var age = PlynlingLife.Age(p, now);
        return All.Where(b => !earned.Contains(b.Key) && b.IsEarned(p, age, evt)).ToList();
    }

    // Under the card that answered the action: one line per badge just earned.
    public static string NewBadgeLines(IEnumerable<BadgeInfo> badges, PlynlingGender gender) =>
        string.Join("\n", badges.Select(b =>
            $"🏅 Nouveau badge : {b.Emoji} {b.Name(gender)}" + (b.Reward > 0 ? $" · +{PebbleEconomy.Cailloux(b.Reward)}" : "")));

    private static BadgeInfo Lived(string key, string emoji, string name, long reward, int days) =>
        new(key, emoji, name, name, reward, (_, age, _) => age.TotalDays >= days);

    private static BadgeInfo Count(string key, string emoji, string name, long reward, Func<Plynling, long> value, long threshold) =>
        new(key, emoji, name, name, reward, (p, _, _) => value(p) >= threshold);
}
