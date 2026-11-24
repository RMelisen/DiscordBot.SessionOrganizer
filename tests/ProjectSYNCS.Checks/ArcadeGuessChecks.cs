using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

// 67. /arcade guess (plus ou moins, moved from /plynling play): the rules, the payout's average,
// and the card at every step.
public static class ArcadeGuessChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("arcade guess: " + what);
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;

        Expect(NumberGuess.Judge(30, 50) == NumberGuess.Hint.Higher && NumberGuess.Judge(70, 50) == NumberGuess.Hint.Lower
            && NumberGuess.Judge(50, 50) == NumberGuess.Hint.Correct, "Judge says whether the number is higher or lower");
        Expect(NumberGuess.Net(true, 100) == 40 && NumberGuess.Net(true, 1) == 0 && NumberGuess.Net(false, 50) == -50
            && NumberGuess.Net(true, 0) == 0 && NumberGuess.Net(false, 0) == 0, "a win pays ×1.4 rounded down, a loss the stake");
        Expect(NumberGuess.CountsForBadge(true, 100) && NumberGuess.CountsForBadge(true, 3) && !NumberGuess.CountsForBadge(true, 2)
            && !NumberGuess.CountsForBadge(true, 1) && !NumberGuess.CountsForBadge(true, 0) && !NumberGuess.CountsForBadge(false, 100),
            "only a win that actually gains cailloux feeds the badge");
        var touched = New(5);
        touched.LastActionAt = DateTimeOffset.UnixEpoch;
        touched.Touch();
        Expect(DateTimeOffset.UtcNow - touched.LastActionAt < TimeSpan.FromMinutes(1), "Touch (the modal opening) resets the idle clock");
        Expect(NumberGuess.Parse(" 42 ") == 42 && NumberGuess.Parse("100") == 100 && NumberGuess.Parse("0") is null
            && NumberGuess.Parse("101") is null && NumberGuess.Parse("abc") is null, "Parse takes 1–100 only");

        // A player halving the range every time finds 63 numbers of 100 in six tries, and gets back
        // under 90 % of the stake on average.
        var found = 0;
        for (var secret = NumberGuess.Min; secret <= NumberGuess.Max; secret++)
        {
            var g = New(secret);
            int lo = NumberGuess.Min, hi = NumberGuess.Max;
            while (!g.Over)
            {
                var guess = (lo + hi) / 2;
                var hint = g.Guess(guess);
                if (hint == NumberGuess.Hint.Higher) lo = guess + 1;
                else if (hint == NumberGuess.Hint.Lower) hi = guess - 1;
            }
            Expect(g.Guesses.Count <= NumberGuess.Tries, $"secret {secret}: {g.Guesses.Count} guesses allowed");
            if (g.Won) found++;
        }
        Expect(found == 63, $"halving finds {found} numbers of 100, expected 63");
        var back = found / 100.0 * (1 + NumberGuess.Net(true, 1000) / 1000.0);
        Expect(back <= 0.9, $"a perfect player gets back {back:P1} of the stake on average");

        // Her lines are also said in free games: no paying, no keeping the stake.
        var responses = typeof(PartyCards).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;
        string[] Pool(string name) => (string[])responses.GetField(name)!.GetValue(null)!;
        foreach (var line in Pool("GuessWinLines").Concat(Pool("GuessLoseLines")))
            Expect(!line.Contains("pai", StringComparison.OrdinalIgnoreCase) && !line.Contains("mise", StringComparison.OrdinalIgnoreCase)
                && !line.Contains("caillou", StringComparison.OrdinalIgnoreCase), $"a line talks money, wrong in a free game: {line}");
        // Prod runs with no culture set (invariant): the footer must not depend on it.
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try { Expect(PartyCards.GuessCard(New(1)).Footer!.Value.Text.Contains("×1,4"), "the footer writes ×1,4 the French way"); }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }

        // The card: fresh, after two guesses, won, lost.
        var fresh = New(37);
        Card(fresh, null, "fresh");
        var mid = New(37);
        mid.Guess(50);
        mid.Guess(25);
        Card(mid, null, "mid");
        var won = New(37);
        won.Guess(37);
        Card(won, "Gagné.", "won");
        var lost = New(37);
        foreach (var n in new[] { 1, 2, 3, 4, 5, 6 }) lost.Guess(n);
        Expect(lost.Over && !lost.Won, "six misses end the game");
        Card(lost, "Perdu.", "lost");
    }

    private static GuessGame New(int secret) => new() { GuildId = 1, ChannelId = 2, PlayerId = 3, Stake = 100, Secret = secret };

    private static void Card(GuessGame g, string? result, string what)
    {
        var embed = PartyCards.GuessCard(g, result);
        var buttons = PartyCards.GuessButtons(g, over: result is not null);
        Expect(embed.Description.Length <= 4096 && !string.IsNullOrEmpty(embed.Title), $"{what}: card text");
        var ids = buttons.Components.SelectMany(r => ((Discord.ActionRowComponent)r).Components).OfType<Discord.ButtonComponent>().ToList();
        Expect(result is null ? ids.Count == 1 && ids[0].Style == Discord.ButtonStyle.Primary : ids.Count == 0,
            $"{what}: {ids.Count} buttons");
        if (result is not null) Expect(embed.Description.Contains($"**{g.Secret}**"), $"{what}: the number is revealed at the end");
        foreach (var (guess, _) in g.Guesses)
            Expect(embed.Description.Contains(guess.ToString()), $"{what}: guess {guess} missing from the card");
    }
}
