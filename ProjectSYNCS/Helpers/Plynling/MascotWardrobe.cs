using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Ping-Qilin's outfits: whole looks picked by hand to go together (never a per-slot mix, which
/// clashed), one a day from the Plynling morning, worn by <see cref="PlynlingLife.Tend"/> on every
/// read of her row. She owns none of it: nothing checks that a worn cosmetic is owned, and the bot
/// must never hold items (no wallet, nothing from nowhere in the dashboard). The cadre is left alone:
/// she cannot die.
/// </summary>
/// <remarks>
/// A function of the day, not state, like the weekly shop: each Monday-to-Sunday week deals the
/// season's looks as a shuffled deck seeded by the week, so every look shows once a week and a
/// restart changes nothing. A deck whose first look would repeat Sunday's swaps its first two. A
/// deck is dealt from the pool of its Monday's season; a day in the next season wears that season's
/// look in place of the old one. The checks (section 73) walk years of days: never the same look
/// two days running, a seasonal look only in its season, every look's layers drawn.
/// </remarks>
public static class MascotWardrobe
{
    /// <summary>One outfit: full cosmetic keys, accessories with their styles already resolved, in slot order.</summary>
    public sealed record Look(string Name, Season Season, string Theme, string Title, IReadOnlyList<WornAccessory> Accessories, string Aura);

    /// <summary>Days in a deck: a week, Monday to Sunday (DayNumber 0, 1 January of year 1, was a Monday).</summary>
    public const int DeckDays = 7;

    private static WornAccessory Acc(string name, string? style = null) => new("cos.accessory." + name, style);

    private static Look Dress(string name, Season season, string theme, string title, string aura, params WornAccessory[] accessories) =>
        new(name, season, "cos.theme." + theme, "cos.title." + title, accessories, "cos.aura." + aura);

    // Every season's pool must hold at least DeckDays looks (the year-round ones and one per season),
    // or a deck would wrap inside its week. Names are for the checks and logs only.
    public static readonly IReadOnlyList<Look> All = new[]
    {
        Dress("Sieste royale", Season.None, "royal", "magnifique", "etoiles_filantes", Acc("couronne"), Acc("medaillon", "haut")),
        Dress("Grasse matinée", Season.None, "nuit_etoilee", "dormeur", "scintillements", Acc("doudou", "gauche")),
        Dress("Soleil-Levant", Season.None, "torii", "sensei", "koi", Acc("kanzashi", "indigo_droite"), Acc("eventail", "indigo_gauche")),
        Dress("Gentlewoman", Season.None, "velours", "distingue", "scintillements",
            Acc("haut_de_forme", "rouge"), Acc("monocle"), Acc("canne", "droite")),
        Dress("Coquette", Season.None, "aurore", "legendaire", "coeurs",
            Acc("lunettes_coeur", "roses"), Acc("noeud", "blanc_gauche"), Acc("ballon", "jaune_gauche")),
        // She cannot die: the title is a fact.
        Dress("L'Immortelle", Season.None, "aurore_boreale", "immortel", "etoiles_filantes", Acc("aureole"), Acc("plume_doree", "droite")),
        Dress("Hiver", Season.Winter, "flocons", "frileux", "flocons", Acc("bonnet", "bleu"), Acc("echarpe", "bleue_haut"), Acc("moufles", "bleues")),
        Dress("Printemps", Season.Spring, "cerisiers", "reveur", "petales", Acc("couronne_fleurs"), Acc("tulipe", "jaune_droite")),
        Dress("Été", Season.Summer, "plage", "bronze", "lucioles", Acc("chapeau_paille"), Acc("lunettes"), Acc("glace", "fraise_droite")),
        Dress("Automne", Season.Autumn, "feuilles_mortes", "mysterieux", "feuilles", Acc("lanterne", "ambree_droite"), Acc("echarpe", "violette_haut")),
    };

    /// <summary>The looks a season deals from: every year-round one and the season's own.</summary>
    public static IReadOnlyList<Look> Pool(Season season) => All.Where(l => l.Season == Season.None || l.Season == season).ToList();

    /// <summary>The look of the Plynling morning <paramref name="dayKey"/> (<see cref="PlynlingLife.MorningDayAtOrBefore"/>).</summary>
    public static Look For(int dayKey)
    {
        var day = DateOnly.FromDateTime(AppTime.FromDayKey(dayKey)).DayNumber;
        return InSeason(Deck(day / DeckDays)[day % DeckDays], SeasonOf(day));
    }

    /// <summary>
    /// Dresses <paramref name="p"/> in <paramref name="look"/>, writing only the slots that differ, so
    /// a row already wearing it is not dirtied. True when anything changed.
    /// </summary>
    public static bool Wear(Plynling p, Look look)
    {
        var changed = false;
        foreach (var (slot, key) in new[] { (CosmeticSlot.Theme, look.Theme), (CosmeticSlot.Title, look.Title), (CosmeticSlot.Aura, look.Aura) })
        {
            if (CosmeticSlots.Get(p, slot) == key) continue;
            CosmeticSlots.Set(p, slot, key);
            changed = true;
        }
        if (!CosmeticSlots.Accessories(p).SequenceEqual(look.Accessories))
        {
            CosmeticSlots.Apply(p, new AccessoryChange(look.Accessories, null, null));
            changed = true;
        }
        return changed;
    }

    // A seasonal look worn outside its season gives way to that season's (a deck dealt before the
    // season turned). Only one seasonal look per pool, so this never puts one look on two days running.
    private static Look InSeason(Look look, Season season) =>
        look.Season == Season.None || look.Season == season ? look : All.First(l => l.Season == season);

    private static List<Look> Deck(int week)
    {
        var deck = Shuffled(week);
        var sunday = week * DeckDays - 1;
        if (InSeason(deck[0], SeasonOf(sunday + 1)) == InSeason(Shuffled(week - 1)[DeckDays - 1], SeasonOf(sunday)))
            (deck[0], deck[1]) = (deck[1], deck[0]);
        return deck;
    }

    // The week's pool, shuffled with the week as seed (Fisher–Yates): the same for every guild, across restarts.
    private static List<Look> Shuffled(int week)
    {
        var deck = Pool(SeasonOf(week * DeckDays)).ToList();
        var rng = new Random(week);
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        return deck;
    }

    // The season of a day by its number (midday UTC of that date: the month is all SeasonAt reads).
    private static Season SeasonOf(int dayNumber) =>
        ItemCatalog.SeasonAt(new DateTimeOffset(DateOnly.FromDayNumber(dayNumber).ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));
}
