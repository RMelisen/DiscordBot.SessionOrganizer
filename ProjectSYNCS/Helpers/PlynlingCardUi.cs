using Discord;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The text on a Plynling card — pure string work, like LevelCardUi, so it is checkable
// without a gateway. The module assembles the components around it.
public static class PlynlingCardUi
{
    // Names are chosen by users and shown publicly: markdown and mention syntax are
    // neutralised here, and every send is AllowedMentions.None on top.
    public static string SafeName(string name) => Format.Sanitize(name);

    // The abandon confirmation: the typed name must be the Plynling's, ignoring case, the
    // spaces around it and how many spaces sit between its words — but not a missing space.
    public static bool NamesMatch(string typed, string name) =>
        string.Equals(Normalize(typed), Normalize(name), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string s) =>
        string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string Percent(double value) => $"{(int)Math.Round(value * 100)} %";

    public static string Bar(double value) => LevelCardUi.ProgressBar((long)Math.Round(value * 1000), 1000);

    public static string Heading(Plynling p, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var age = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);
        // The gender sign sits on the species line, not in the heading: a "## " line renders
        // ♂/♀ at heading size, far too big beside the name.
        return $"## {SafeName(p.Name)}\n" +
               $"{p.Gender.Symbol()} {info.Name} · *{PlynlingCatalog.RarityLabel(info.Rarity)}*\n" +
               $"à <@{p.OwnerId}> · {StageLabel(PlynlingLife.Stage(p, now), p.Gender)} · {p.Gender.Agree("âgé", "âgée")} de {age}";
    }

    public static string Status(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is { } died)
        {
            var lived = PlynlingLife.Age(p, now);
            var memorial = PlynlingCatalog.MemorialName(PlynlingCatalog.MemorialTier(lived));
            return $"🪦 {p.Gender.Agree("Mort", "Morte")} <t:{died.ToUnixTimeSeconds()}:R>, après {LevelCardUi.Duration((long)lived.TotalMinutes)} de vie. " +
                   $"{p.Gender.Agree("Il", "Elle")} repose sous {memorial}.";
        }

        var hunger = PlynlingLife.HungerAt(p, now);
        var happiness = PlynlingLife.HappinessAt(p, now);
        // Only a freeze is shown beside the hunger bar. A living Plynling's card deliberately
        // carries no "mourra de faim dans …" countdown — the owner removed it; the bar and the
        // mood already say how hungry it is. The absolute :f form is the one that takes "jusqu'au".
        var clock = p.FrozenAt is null
            ? PlynlingLife.IsAsleep(now) ? $" · 💤 {p.Gender.Agree("Endormi", "Endormie")} jusqu'à 5 h" : ""
            : p.FreezeUntil is { } until
                ? $" · ❄️ {p.Gender.Agree("Gelé", "Gelée")} jusqu'au <t:{until.ToUnixTimeSeconds()}:f>"
                : $" · ❄️ {p.Gender.Agree("Gelé", "Gelée")} par le staff";

        // The mood gets its own line: it covers hunger as well as happiness, so beside the
        // happiness bar a starving Plynling would read "Bonheur 20 % · affamé".
        return $"**Faim** `{Bar(hunger)}` {Percent(hunger)}{clock}\n" +
               $"**Bonheur** `{Bar(happiness)}` {Percent(happiness)}\n" +
               $"**Humeur** · *{MoodLabel(PlynlingLife.Mood(p, now), p.Gender)}*";
    }

    public static string MoodLabel(PlynlingMood mood, PlynlingGender gender) => mood switch
    {
        PlynlingMood.Happy => gender.Agree("heureux", "heureuse"),
        PlynlingMood.Sad => "triste",
        PlynlingMood.Hungry => gender.Agree("affamé", "affamée"),
        PlynlingMood.Starving => gender.Agree("mourant de faim", "mourante de faim"),
        PlynlingMood.Frozen => gender.Agree("gelé", "gelée"),
        PlynlingMood.Sleeping => gender.Agree("endormi", "endormie"),
        _ => gender.Agree("content", "contente"),
    };

    public static string StageLabel(PlynlingStage stage, PlynlingGender gender) => stage switch
    {
        PlynlingStage.Baby => "bébé",
        PlynlingStage.Teen => "ado",
        PlynlingStage.Elder => gender.Agree("ancien", "ancienne"),
        _ => "adulte",
    };

    public static string FoodEffect(FoodInfo food)
    {
        var parts = new List<string>();
        if (food.Hunger > 0) parts.Add($"+{(int)Math.Round(food.Hunger * 100)} % de faim");
        if (food.Happiness > 0) parts.Add($"+{(int)Math.Round(food.Happiness * 100)} % de bonheur");
        return string.Join(", ", parts);
    }

    public static string GraveyardTitle(ulong owner, GraveSortLabel sort) =>
        (owner == 0 ? "## 🪦 Cimetière des Plynlings" : $"## 🪦 Les tombes de <@{owner}>") +
        $"\n-# Tri : {sort.Text}";

    public static string EmptyGraveyard(ulong owner) => owner == 0
        ? "Le cimetière est vide. Pour l'instant."
        : $"<@{owner}> n'a encore perdu aucun Plynling. Bravo… pour l'instant.";

    public static string GraveLine(Plynling p, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var lived = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);
        return $"**{SafeName(p.Name)}** {p.Gender.Symbol()} · {info.Name} — à <@{p.OwnerId}>\n" +
               $"*a vécu {lived}* · {p.Gender.Agree("mort", "morte")} <t:{p.DiedAt!.Value.ToUnixTimeSeconds()}:R>";
    }
}

// A graveyard sort's French wording: the title's "Tri : …" text and the button label.
public readonly record struct GraveSortLabel(string Text, string Button);
