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
        _ => GuessText(s, name),
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

    private static string GuessText(PlynlingGameState s, string name)
    {
        var text = "## 🔢 Plus ou moins\n" +
                   $"**{name}** pense à un nombre entre {PlynlingGameState.GuessMin} et {PlynlingGameState.GuessMax}.";
        if (s.Status == GameStatus.Won) return text + $"\nBravo, c'était **{s.LastGuess}** !";
        if (s.Status == GameStatus.Lost)
            return text + $"\n{s.LastGuess} ? **C'est {Hint(s.LastHint)} !**\nPerdu… c'était **{s.Secret}**.";
        if (s.LastGuess is { } guess) text += $"\n{guess} ? **C'est {Hint(s.LastHint)} !**";
        return text + $"\n-# {s.TriesLeft} essai{(s.TriesLeft > 1 ? "s" : "")} restant{(s.TriesLeft > 1 ? "s" : "")}";
    }

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

    private static string Hint(GuessHint? hint) => hint == GuessHint.Higher ? "plus" : "moins";
}
