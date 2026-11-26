using System.Globalization;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum StatsTab { Life, Care, Play, Social, Collection }

/// <summary>Which side of a duel line wins: the higher number, the lower (a healthier life), or neither.</summary>
public enum Better { Higher, Lower, None }

/// <summary>
/// One line of a tab. Id is stable across Plynlings (a duel pairs lines by it); Value is what a duel compares
/// (null when the line is not a number); SinceRelease marks a line read from the counters, which started on
/// PlynlingCounters.Since.
/// </summary>
public sealed record SheetLine(string Id, string Label, string Text, double? Value, Better Better, bool SinceRelease, StatRank? Rank = null);

/// <summary>One line of the duel: both sides' text, the winner's in bold. Winner: -1 the first, 1 the second, 0 none.</summary>
public sealed record DuelLine(string Label, string A, string B, int Winner);

/// <summary>
/// /plynling stats' wording (docs/agents/plynling.md, *Stats*): each tab's lines from a sheet, the duel's pairing and
/// leader rule, the footers. Pure, so the checks can build every card at its fullest. Labels are neutral (a
/// duel puts a boy and a girl on one line); names come sanitised from the caller.
/// </summary>
public static class PlynlingStatsUi
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public static string TabLabel(StatsTab tab) => tab switch
    {
        StatsTab.Life => "Vie",
        StatsTab.Care => "Soins",
        StatsTab.Play => "Jeux",
        StatsTab.Social => "Social",
        _ => "Collection",
    };

    public static string TabEmoji(StatsTab tab) => tab switch
    {
        StatsTab.Life => "🌱",
        StatsTab.Care => "🍽️",
        StatsTab.Play => "🎲",
        StatsTab.Social => "🤝",
        _ => "🏅",
    };

    public static string N(long n) => n.ToString("N0", Fr);
    public static string Percent(long part, long whole) => whole <= 0 ? "—" : $"{Math.Round(100.0 * part / whole):0} %";

    public static string Rank(StatRank r) => r.Rank switch
    {
        1 => "🥇",
        2 => "🥈",
        3 => "🥉",
        _ => $"{r.Rank}e / {r.Of}",
    };

    public static string GameName(PlynlingGame game) => game switch
    {
        PlynlingGame.HideAndSeek => "🙈 Cache-cache",
        PlynlingGame.RockPaperScissors => "✊ Pierre-papier-ciseaux",
        PlynlingGame.PebbleBag => "👝 Le sac de cailloux",
        PlynlingGame.Memory => "🍄 Mémoire",
        PlynlingGame.LastPebble => "🪨 Le dernier caillou",
        PlynlingGame.TreasureHunt => "🌰 Chasse au trésor",
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, "a new game needs its name here"),
    };

    private static string BondName(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Friends => "Amis",
        PlynlingBond.BestFriends => "Meilleurs amis",
        PlynlingBond.Lovers => "En couple",
        PlynlingBond.Rivals => "Rivaux",
        PlynlingBond.Enemies => "Ennemis",
        _ => "Connaissances",
    };

    private static string MarkName(MarkKind kind) => kind switch
    {
        MarkKind.Epithet => "🏷️ Surnoms",
        MarkKind.Keepsake => "🎒 Objets gardés",
        _ => "🔓 Découvertes",
    };

    // Lines whose Id starts with one of these are list items: left out of the single view at 0, and of a duel
    // when both sides are at 0.
    private static readonly string[] ListPrefixes = { "food.", "game.", "bond.", "mark." };
    private static bool IsListItem(SheetLine l) => ListPrefixes.Any(l.Id.StartsWith);

    /// <summary>A tab's lines for the single view (list items at 0 left out).</summary>
    public static IReadOnlyList<SheetLine> Lines(PlynlingStatSheet s, StatsTab tab) =>
        Build(s, tab).Where(l => !IsListItem(l) || l.Value is > 0).ToList();

    public static string Render(SheetLine l, bool partial) =>
        $"{l.Label}{(l.SinceRelease && partial ? "*" : "")} : {l.Text}{(l.Rank is { } r ? " · " + Rank(r) : "")}";

    /// <summary>« -# * compté depuis le 26/11 » when the sheet is partial and the tab shows a counter.</summary>
    public static string? SinceFooter(PlynlingStatSheet s, StatsTab tab) =>
        s.Partial && Build(s, tab).Any(l => l.SinceRelease) ? SinceNote : null;

    public static string SinceNote =>
        $"-# * compté depuis le {AppTime.ToZoned(PlynlingCounters.Since).ToString("dd'/'MM", Fr)}";

    // ---- The duel ------------------------------------------------------------------------------------------

    public static IReadOnlyList<DuelLine> Duel(PlynlingStatSheet a, PlynlingStatSheet b, StatsTab tab)
    {
        var listA = Build(a, tab).ToList();
        var listB = Build(b, tab).ToList();
        var la = listA.ToDictionary(l => l.Id);
        var lb = listB.ToDictionary(l => l.Id);
        // The first side's order, each line only the second has slotted in after the line it follows there,
        // so a death date or a fan stays where it belongs in the tab.
        var ids = listA.Select(l => l.Id).ToList();
        for (var i = 0; i < listB.Count; i++)
        {
            if (la.ContainsKey(listB[i].Id)) continue;
            var after = i == 0 ? -1 : ids.IndexOf(listB[i - 1].Id);
            ids.Insert(after + 1, listB[i].Id);
        }
        var lines = new List<DuelLine>();
        foreach (var id in ids)
        {
            var x = la.GetValueOrDefault(id);
            var y = lb.GetValueOrDefault(id);
            var any = x ?? y!;
            if (IsListItem(any) && (x?.Value ?? 0) <= 0 && (y?.Value ?? 0) <= 0) continue;
            var winner = Winner(any.Better, any.SinceRelease && a.Partial != b.Partial, x?.Value, y?.Value);
            var label = any.Label + (any.SinceRelease && (a.Partial || b.Partial) ? "*" : "");
            string Side(SheetLine? l, bool won) => l is null ? "—" : won ? $"**{l.Text}**" : l.Text;
            lines.Add(new DuelLine(label, Side(x, winner == -1), Side(y, winner == 1), winner));
        }
        return lines;
    }

    private static int Winner(Better better, bool unfair, double? a, double? b)
    {
        if (better == Better.None || unfair || a is null || b is null || a == b) return 0;
        var aAhead = better == Better.Higher ? a > b : a < b;
        return aAhead ? -1 : 1;
    }

    /// <summary>« 🏆 Pwet mène 7 à 4 », « 🤝 Égalité 5 à 5 », or null when no line competed.</summary>
    public static string? DuelFooter(IReadOnlyList<DuelLine> lines, string aName, string bName)
    {
        var a = lines.Count(l => l.Winner == -1);
        var b = lines.Count(l => l.Winner == 1);
        if (a + b == 0) return null;
        if (a == b) return $"🤝 Égalité {a} à {b}";
        return a > b ? $"🏆 **{aName}** mène {a} à {b}" : $"🏆 **{bName}** mène {b} à {a}";
    }

    // ---- The tabs --------------------------------------------------------------------------------------------

    private static IEnumerable<SheetLine> Build(PlynlingStatSheet s, StatsTab tab) => tab switch
    {
        StatsTab.Life => LifeLines(s),
        StatsTab.Care => CareLines(s),
        StatsTab.Play => PlayLines(s),
        StatsTab.Social => SocialLines(s),
        _ => CollectionLines(s),
    };

    private static SheetLine Num(string id, string label, long n, Better better, bool since = false, StatRank? rank = null) =>
        new(id, label, N(n), n, better, since, rank);

    private static StatRank? R(PlynlingStatSheet s, RankedStat stat) => s.Ranks.GetValueOrDefault(stat);

    private static IEnumerable<SheetLine> LifeLines(PlynlingStatSheet s)
    {
        var p = s.Plynling;
        var age = PlynlingLife.Age(p, s.Now);
        yield return new SheetLine("age", "🎂 Âge", LevelCardUi.Duration((long)age.TotalMinutes), age.TotalMinutes, Better.Higher, false);
        yield return new SheetLine("stage", "🌱 Stade", PlynlingCardUi.StageLabel(PlynlingLife.Stage(p, s.Now), p.Gender), null, Better.None, false);
        yield return new SheetLine("adopted", "📅 Adoption", $"<t:{p.AdoptedAt.ToUnixTimeSeconds()}:D>", null, Better.None, false);
        if (p.DiedAt is { } died)
            yield return new SheetLine("died", "🕯️ Décès",
                $"<t:{died.ToUnixTimeSeconds()}:D> ({(p.DeathCause == DeathCause.Illness ? "maladie" : "faim")})", null, Better.None, false);
        yield return Num("score", "⭐ Score", p.Score, Better.Higher, rank: R(s, RankedStat.Score));
        yield return Num("level", "🏋️ Niveau", PlynlingRpg.Level(p.RpgXp), Better.Higher, rank: R(s, RankedStat.Level));
        yield return new SheetLine("stress", "😰 Stress", $"{N(p.Stress)} / 400", p.Stress, Better.Lower, false);
        yield return Num("sick", "🤒 Fois malade", s.Facts.TimesSick, Better.Lower);
        yield return Num("sickdays", "🛌 Matins de maladie", s.Count(PlynlingCounters.SickDays), Better.Lower, since: true);
        yield return Num("warning", "😨 Alertes de faim", s.Count(PlynlingCounters.Warning), Better.Lower, since: true);
        yield return Num("frozen", "🧊 Gels", s.Facts.TimesFrozen, Better.None);
        // Past freezes, plus the one still running (it is counted for good when it ends).
        var running = p.FrozenAt is { } since && s.Now > since ? (s.Now - since).TotalSeconds : 0;
        var frozenDays = (s.Count(PlynlingCounters.FrozenSeconds) + running) / 86_400.0;
        yield return new SheetLine("frozendays", "❄️ Jours au frais", frozenDays.ToString("0.#", Fr), frozenDays, Better.None, true);
    }

    private static IEnumerable<SheetLine> CareLines(PlynlingStatSheet s)
    {
        var p = s.Plynling;
        yield return Num("meals", "🍽️ Repas", p.Meals, Better.Higher, rank: R(s, RankedStat.Meals));
        yield return Num("fedbyothers", "💝 Repas offerts par d'autres", p.FedByOthers, Better.Higher);
        var foods = Enum.GetValues<PlynlingFood>().Select(f => (Food: f, N: s.Count(PlynlingCounters.Food(f)))).ToList();
        if (foods.Any(f => f.N > 0))
        {
            var fav = foods.OrderByDescending(f => f.N).ThenBy(f => f.Food).First();
            yield return new SheetLine("favourite", "😋 Plat préféré", $"{PlynlingCatalog.Info(fav.Food).Name} ({N(fav.N)})", null, Better.None, true);
        }
        foreach (var (food, n) in foods)
            yield return Num("food." + food.ToString().ToLowerInvariant(), "· " + PlynlingCatalog.Info(food).Name, n, Better.Higher, since: true);
        yield return Num("pantry", "🥫 Repas du garde-manger", s.Count(PlynlingCounters.MealPantry), Better.Higher, since: true);
        yield return Num("spentowner", "🪙 Cailloux pour ses repas (son humain)", s.Count(PlynlingCounters.MealSpentOwner), Better.Higher, since: true);
        yield return Num("spentothers", "🪙 Cailloux pour ses repas (les autres)", s.Count(PlynlingCounters.MealSpentOthers), Better.Higher, since: true);
        yield return Num("pets", "🤗 Caresses", p.Pets, Better.Higher, rank: R(s, RankedStat.Pets));
        yield return Num("petsowner", "🤗 Caresses de son humain", s.Count(PlynlingCounters.PetOwner), Better.Higher, since: true);
        yield return Num("petsothers", "🤗 Caresses des autres", s.Count(PlynlingCounters.PetOthers), Better.Higher, since: true);
        yield return Num("carers", "👥 Personnes aux petits soins", s.Carers.Count, Better.Higher, since: true);
        var fan = s.Carers.Where(c => c.UserId != p.OwnerId).OrderByDescending(c => c.Meals + c.Pets).ThenBy(c => c.UserId).FirstOrDefault();
        if (fan is not null)
            yield return new SheetLine("fan", "🌟 Plus grand fan",
                $"<@{fan.UserId}> ({N(fan.Meals)} repas, {N(fan.Pets)} caresses)", null, Better.None, true);
        var free = s.Count(PlynlingCounters.BathFree);
        var soap = s.Count(PlynlingCounters.BathSoap);
        yield return new SheetLine("baths", "🛁 Bains", soap > 0 ? $"{N(free + soap)} (dont {N(soap)} au savon)" : N(free + soap),
            free + soap, Better.Higher, true);
        yield return Num("medicine", "💊 Médicaments", s.Count(PlynlingCounters.Medicine), Better.None, since: true);
    }

    private static IEnumerable<SheetLine> PlayLines(PlynlingStatSheet s)
    {
        var p = s.Plynling;
        yield return Num("plays", "🎲 Parties", p.Plays, Better.Higher, rank: R(s, RankedStat.Plays));
        yield return Num("won", "🏆 Victoires", p.PlaysWon, Better.Higher);
        if (p.Plays > 0)
            yield return new SheetLine("winrate", "📈 Taux de victoire", Percent(p.PlaysWon, p.Plays), (double)p.PlaysWon / p.Plays, Better.Higher, false);
        foreach (var game in Enum.GetValues<PlynlingGame>())
        {
            var played = s.Count(PlynlingCounters.Game(game));
            var won = s.Count(PlynlingCounters.GameWon(game));
            yield return new SheetLine("game." + game.ToString().ToLowerInvariant(), GameName(game),
                $"{N(played)} {(played > 1 ? "parties" : "partie")} · {Percent(won, played)}", played, Better.Higher, true);
        }
        yield return Num("drills", "💪 Entraînements", p.Drills, Better.Higher, rank: R(s, RankedStat.Drills));
        yield return Num("forage", "🧺 Cueillettes", s.Count(PlynlingCounters.Forage), Better.Higher, since: true);
        var finds = new[]
        {
            ("cueillette", PlynlingCounters.FindForage), ("jeux", PlynlingCounters.FindPlay), ("visites", PlynlingCounters.FindVisit),
            ("cadeaux", PlynlingCounters.FindGift), ("histoires", PlynlingCounters.FindEvent),
        }.Select(f => (f.Item1, N: s.Count(f.Item2))).ToList();
        var found = finds.Sum(f => f.N);
        var detail = string.Join(", ", finds.Where(f => f.N > 0).Select(f => $"{f.Item1} {N(f.N)}"));
        yield return new SheetLine("finds", "🔍 Objets trouvés", found > 0 ? $"{N(found)} ({detail})" : "0", found, Better.Higher, true);
        var gifts = s.Count(PlynlingCounters.GiftCailloux) + s.Count(PlynlingCounters.GiftItems);
        var giftCailloux = s.Count(PlynlingCounters.GiftCaillouxTotal);
        yield return new SheetLine("gifts", "🎀 Cadeaux trouvés", giftCailloux > 0 ? $"{N(gifts)} · {N(giftCailloux)} cailloux en tout" : N(gifts),
            gifts, Better.Higher, true);
        var earned = s.Count(PlynlingCounters.EarnGame) + s.Count(PlynlingCounters.EarnBadge) + s.Count(PlynlingCounters.EarnEvent) + giftCailloux;
        yield return Num("earned", "🪙 Cailloux rapportés", earned, Better.Higher, since: true);
    }

    private static IEnumerable<SheetLine> SocialLines(PlynlingStatSheet s)
    {
        var p = s.Plynling;
        var f = s.Facts;
        yield return Num("visits", "🏠 Visites", p.Visits, Better.Higher, rank: R(s, RankedStat.Visits));
        yield return Num("visitmade", "🚶 Visites rendues", s.Count(PlynlingCounters.VisitMade), Better.Higher, since: true);
        yield return Num("visithosted", "🏡 Visites reçues", p.Hosted, Better.Higher, since: true);
        yield return Num("visitgood", "✨ Bonnes visites", s.Count(PlynlingCounters.VisitGood), Better.Higher, since: true);
        yield return Num("meetings", "🤝 Rencontres", f.Meetings, Better.Higher);
        if (f.MostMetName is { } name)
            yield return new SheetLine("mostmet", "👯 Le plus rencontré", $"**{PlynlingCardUi.SafeName(name)}** ({N(f.MostMetMeetings)})", null, Better.None, false);
        foreach (var bond in new[] { PlynlingBond.Lovers, PlynlingBond.BestFriends, PlynlingBond.Friends, PlynlingBond.Acquaintances, PlynlingBond.Rivals, PlynlingBond.Enemies })
            yield return Num("bond." + bond.ToString().ToLowerInvariant(), $"{PlynlingBonds.Emoji(bond)} {BondName(bond)}",
                f.Bonds.GetValueOrDefault(bond), Better.None);
        yield return Num("confessmade", "💌 Déclarations faites", s.Count(PlynlingCounters.ConfessMade), Better.Higher, since: true);
        yield return Num("confessaccepted", "💘 Déclarations acceptées", s.Count(PlynlingCounters.ConfessAccepted), Better.Higher, since: true);
        yield return Num("confessrefused", "💔 Déclarations refusées", s.Count(PlynlingCounters.ConfessRefused), Better.None, since: true);
        yield return Num("confessreceived", "📨 Déclarations reçues", s.Count(PlynlingCounters.ConfessReceived), Better.Higher, since: true);
        yield return Num("events", "📖 Événements vécus", f.EventsLived, Better.Higher, rank: R(s, RankedStat.EventsLived));
        if (f.ChallengesTried > 0)
            yield return new SheetLine("challenges", "🎯 Défis réussis",
                $"{N(f.ChallengesWon)} / {N(f.ChallengesTried)} ({Percent(f.ChallengesWon, f.ChallengesTried)})", f.ChallengesWon, Better.Higher, false);
        yield return Num("stories", "📚 Histoires terminées", f.StoriesFinished, Better.Higher);
    }

    private static IEnumerable<SheetLine> CollectionLines(PlynlingStatSheet s)
    {
        var f = s.Facts;
        yield return new SheetLine("badges", "🏅 Badges", $"{N(f.Badges)} / {N(PlynlingBadges.All.Count)}", f.Badges, Better.Higher, false,
            R(s, RankedStat.Badges));
        var traits = f.Traits.Values.Sum();
        var parts = new[] { (TraitKind.Childhood, "enfance"), (TraitKind.Personality, "caractère"), (TraitKind.Coping, "refuge") }
            .Where(t => f.Traits.GetValueOrDefault(t.Item1) > 0)
            .Select(t => $"{t.Item2} {f.Traits[t.Item1]}");
        yield return new SheetLine("traits", "🧬 Traits", traits > 0 ? $"{N(traits)} ({string.Join(", ", parts)})" : "0", traits, Better.None, false);
        yield return Num("memories", "💭 Souvenirs d'histoires", f.Memories, Better.Higher);
        foreach (var kind in Enum.GetValues<MarkKind>())
            yield return Num("mark." + kind.ToString().ToLowerInvariant(), MarkName(kind), f.Marks.GetValueOrDefault(kind), Better.Higher);
    }
}
