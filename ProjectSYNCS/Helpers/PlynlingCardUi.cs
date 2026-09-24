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

    public static string Percent(double value) => $"{(int)Math.Round(value * 100)} %";

    public static string Bar(double value) => LevelCardUi.ProgressBar((long)Math.Round(value * 1000), 1000);

    public static string Heading(Plynling p, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var age = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);
        return $"## {SafeName(p.Name)}\n" +
               $"{info.Name} · *{PlynlingCatalog.RarityLabel(info.Rarity)}*\n" +
               $"à <@{p.OwnerId}> · âgé de {age}";
    }

    public static string Status(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is { } died)
        {
            var lived = PlynlingLife.Age(p, now);
            var memorial = PlynlingCatalog.MemorialName(PlynlingCatalog.MemorialTier(lived));
            return $"🪦 Mort <t:{died.ToUnixTimeSeconds()}:R>, après {LevelCardUi.Duration((long)lived.TotalMinutes)} de vie. " +
                   $"Il repose sous {memorial}.";
        }

        var hunger = PlynlingLife.HungerAt(p, now);
        var happiness = PlynlingLife.HappinessAt(p, now);
        // A relative timestamp renders as "dans 2 jours", so the words before it must read
        // with that: "mourra de faim dans 2 jours", never "jusqu'à dans 2 jours". The
        // absolute :f form is the one that takes "jusqu'au".
        var clock = p.FrozenAt is not null
            ? p.FreezeUntil is { } until
                ? $"❄️ Gelé jusqu'au <t:{until.ToUnixTimeSeconds()}:f>"
                : "❄️ Gelé par le staff"
            : $"mourra de faim <t:{PlynlingLife.DeathAt(p)!.Value.ToUnixTimeSeconds()}:R>";

        // The mood gets its own line: it covers hunger as well as happiness, so beside the
        // happiness bar a starving Plynling would read "Bonheur 20 % · affamé".
        return $"**Faim** `{Bar(hunger)}` {Percent(hunger)} · {clock}\n" +
               $"**Bonheur** `{Bar(happiness)}` {Percent(happiness)}\n" +
               $"**Humeur** · *{MoodLabel(PlynlingLife.Mood(p, now))}*";
    }

    public static string MoodLabel(PlynlingMood mood) => mood switch
    {
        PlynlingMood.Happy => "heureux",
        PlynlingMood.Sad => "triste",
        PlynlingMood.Hungry => "affamé",
        PlynlingMood.Starving => "mourant de faim",
        PlynlingMood.Frozen => "gelé",
        _ => "content",
    };

    public static string FoodEffect(FoodInfo food)
    {
        var parts = new List<string>();
        if (food.Hunger > 0) parts.Add($"+{(int)Math.Round(food.Hunger * 100)} % de faim");
        if (food.Happiness > 0) parts.Add($"+{(int)Math.Round(food.Happiness * 100)} % de bonheur");
        return string.Join(", ", parts);
    }
}
