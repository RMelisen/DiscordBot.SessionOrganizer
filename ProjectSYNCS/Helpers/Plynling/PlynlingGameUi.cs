using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The text of a /plynling play game at every step — pure string work, like PlynlingCardUi,
// so every state of every game is checkable without a gateway. The name is already
// sanitised by the caller.
public static class PlynlingGameUi
{
    public static string Text(PlynlingGameState s, string name, PlynlingGender g) => s.Game switch
    {
        PlynlingGame.HideAndSeek => HideText(s, name, g),
        PlynlingGame.RockPaperScissors => RpsText(s, name),
        PlynlingGame.PebbleBag => BagText(s, name, g),
        PlynlingGame.Memory => MemoryText(s, name, g),
        PlynlingGame.LastPebble => NimText(s, name),
        _ => TreasureText(s, name, g),
    };

    private static string HideText(PlynlingGameState s, string name, PlynlingGender g)
    {
        const string head = "## 🙈 Cache-cache\n";
        return s.Status switch
        {
            GameStatus.Won => head + $"Trouvé ! **{name}** était derrière le rocher {s.LastPick + 1}.",
            GameStatus.Lost => head + $"Perdu… **{name}** était derrière le rocher {s.HiddenBehind + 1}.",
            _ when s.LastPick is { } missed => head +
                $"Raté, ce n'était pas le rocher {missed + 1} ! **{name}** s'est {g.Agree("recaché", "recachée")}… Lequel, cette fois ?\n" +
                $"-# Manche {s.Round}/{PlynlingGameState.HideRounds}",
            _ => head + $"**{name}** s'est {g.Agree("caché", "cachée")} derrière un rocher. Lequel ?\n" +
                 $"-# Manche {s.Round}/{PlynlingGameState.HideRounds}",
        };
    }

    private static string RpsText(PlynlingGameState s, string name)
    {
        var text = "## ✊ Pierre-papier-ciseaux\n" +
                   $"Premier à {PlynlingGameState.RpsToWin} manches · toi **{s.PlayerScore}** – **{s.PlynlingScore}** {name}";
        if (s.LastPlayerThrow is { } mine && s.LastPlynlingThrow is { } its && s.LastResult is { } result)
        {
            text += $"\nTu joues {Emoji(mine)}, **{name}** joue {Emoji(its)} : " + result switch
            {
                RpsResult.Win => "gagné !",
                RpsResult.Lose => "perdu…",
                _ => "égalité, on rejoue.",
            };
        }
        return text + s.Status switch
        {
            GameStatus.Won => "\n**Tu gagnes la partie !**",
            GameStatus.Lost => $"\n**{name}** gagne la partie !",
            _ => "",
        };
    }

    private static string BagText(PlynlingGameState s, string name, PlynlingGender g)
    {
        var text = "## 🪨 Le sac de cailloux\n";
        var trail = s.Drawn.Count > 1 ? $"\n-# Tirés : {string.Join(" · ", s.Drawn)}" : "";
        var previous = s.Drawn.Count > 1 ? s.Drawn[^2] : 0;
        return s.Status switch
        {
            GameStatus.Won => text + $"**{previous}** puis **{s.Pebble}** : trois sur trois ! **{name}** retourne le sac, " +
                              $"{g.Agree("beau joueur", "belle joueuse")}." + trail,
            GameStatus.Lost => text + $"**{previous}** puis **{s.Pebble}** : raté ! **{name}** referme le sac d'un air entendu." + trail,
            _ when s.LastRight == true => text + $"**{previous}** puis **{s.Pebble}** : bien vu ! Et le suivant, plus haut ou plus bas ?\n" +
                                          $"-# Série {s.Streak}/{PlynlingGameState.BagToWin}" + trail,
            _ => text + $"**{name}** tire du sac un caillou marqué **{s.Pebble}**. Le suivant sera plus haut ou plus bas ?\n" +
                 $"-# Trois bonnes réponses d'affilée pour gagner (de {PlynlingGameState.BagMin} à {PlynlingGameState.BagMax}, un caillou identique retourne dans le sac)",
        };
    }

    private static string MemoryText(PlynlingGameState s, string name, PlynlingGender g)
    {
        var text = "## 🍄 Mémoire\n";
        var revealed = string.Join("\n", s.Sequence.Select((key, i) => $"{i + 1}. {MushroomEmoji(key)} {MushroomLabel(key)}"));
        if (s.Status == GameStatus.Won)
            return text + $"Les quatre, dans l'ordre ! **{name}** te regarde comme si tu avais des spores de génie.\n{revealed}";
        if (s.Status == GameStatus.Lost)
            return text + $"Raté, pas {(s.WrongSlot is { } w ? MushroomLabel(s.Options[w]) : "celui-là")} ! **{name}** soulève les feuilles :\n{revealed}";
        if (!s.Memorised)
            return text + $"**{name}** aligne quatre champignons dans l'herbe, très {g.Agree("sérieux", "sérieuse")}. Retiens l'ordre :\n" +
                   revealed + "\n-# Clique « J'ai retenu ! » quand c'est bon : ils seront cachés.";
        var progress = string.Join(" ", s.Sequence.Select((key, i) => i < s.Progress ? MushroomEmoji(key) : "❔"));
        return text + $"**{name}** a couvert les champignons de feuilles. Lequel était le n° {s.Progress + 1} ?\n{progress}";
    }

    private static string NimText(PlynlingGameState s, string name)
    {
        var text = "## 🪨 Le dernier caillou\n";
        var moves = s.LastTake is { } mine
            ? $"Tu en prends {mine}" + (s.PlynlingTake is { } its ? $", **{name}** en prend {its}." : ".") + "\n"
            : "";
        return s.Status switch
        {
            GameStatus.Won => text + $"Tu prends {(s.LastTake == 1 ? "le dernier caillou" : $"les {s.LastTake} derniers")} : la partie est à toi !",
            GameStatus.Lost => text + $"Tu en prends {s.LastTake}, et **{name}** rafle " +
                               (s.PlynlingTake == 1 ? "le dernier caillou." : $"les {s.PlynlingTake} derniers.") + " Bien joué, petit champignon.",
            _ => text + moves + $"{Pebbles(s.Pile)}\nIl reste **{s.Pile}** caillou{(s.Pile > 1 ? "x" : "")}. " +
                 s.Pile switch
                 {
                     1 => "Prends-le : celui qui prend le dernier gagne.",
                     2 => "Prends-en 1 ou 2 : celui qui prend le dernier gagne.",
                     _ => "Prends-en 1, 2 ou 3 : celui qui prend le dernier gagne.",
                 },
        };
    }

    private static string Pebbles(int n) => string.Concat(Enumerable.Repeat("🪨", n));

    private static string TreasureText(PlynlingGameState s, string name, PlynlingGender g)
    {
        var text = "## 🌰 Chasse au trésor\n";
        var left = PlynlingGameState.Digs - s.Dug.Count;
        var legend = "\n-# 🔥 brûlant : juste à côté · ♨️ chaud : à deux pas · ❄️ froid : plus loin";
        if (s.Status == GameStatus.Won)
            return text + $"Le gland était là ! **{name}** le reprend aussitôt, {g.Agree("ravi", "ravie")} qu'on l'ait trouvé.";
        var last = s.LastDig is { } cell ? $"Coup de pelle : **{HintWord(s.Dug[cell])}** {HintEmoji(s.Dug[cell])}\n" : "";
        if (s.Status == GameStatus.Lost)
            return text + last + $"Plus de coups de pelle… Le gland était là 🌰. **{name}** l'avait bien caché, son trésor.";
        if (s.LastDig is null)
            return text + $"**{name}** a enterré un gland quelque part dans le carré, et sifflote. Tu as {left} coups de pelle." + legend;
        return text + last + $"Encore {left} coup{(left > 1 ? "s" : "")} de pelle." + legend;
    }

    private static string HintWord(DigHint h) => h switch
    {
        DigHint.Burning => "brûlant",
        DigHint.Warm => "chaud",
        DigHint.Cold => "froid",
        _ => "trouvé",
    };

    public static string HintEmoji(DigHint h) => h switch
    {
        DigHint.Burning => "🔥",
        DigHint.Warm => "♨️",
        DigHint.Cold => "❄️",
        _ => "🌰",
    };

    // A Mémoire mushroom by item key: its French name, and its emoji (the pack's, once loaded).
    public static string MushroomLabel(string key) => ItemCatalog.ByKey(key)?.Name ?? key;

    public static string MushroomEmoji(string key) => ItemCatalog.ByKey(key)?.Emoji ?? "🍄";

    /// <summary>
    /// The mushrooms Mémoire may draw: every one of the set whose own emoji is loaded. Before the
    /// gateway is ready (or if the upload failed) they would all show 🍄, so none qualify.
    /// </summary>
    public static IReadOnlyList<string> LoadedMushrooms() =>
        ItemCatalog.InSet(ItemCatalog.MushroomSet)
            .Where(i => ItemEmojis.For(ItemCatalog.PictureKey(i.Key)) is not null)
            .Select(i => i.Key)
            .ToList();

    // What a finished game gave: happiness always, cailloux on a win.
    public static string Reward(bool won, long pebbles, long balance)
    {
        var happiness = (int)Math.Round((PlynlingLife.PlayAmount + (won ? PlynlingLife.PlayWinBonus : 0)) * 100);
        return won && pebbles > 0
            ? $"-# +{happiness} % de bonheur · +{PebbleEconomy.Cailloux(pebbles)} (il te reste {PebbleEconomy.Cailloux(balance)})"
            : $"-# +{happiness} % de bonheur";
    }

    public static string Emoji(RpsThrow t) => t switch
    {
        RpsThrow.Rock => "✊",
        RpsThrow.Paper => "✋",
        _ => "✌️",
    };
}
