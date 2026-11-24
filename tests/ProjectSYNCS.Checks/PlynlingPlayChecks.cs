using ProjectSYNCS.Helpers;

// 66. /plynling play's games: the rules of each, their simulated odds, the text and the cards.
public static class PlynlingPlayChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("play: " + what);
    }

    private static bool Throws(Action a)
    {
        try { a(); return false; }
        catch (InvalidOperationException) { return true; }
        catch (ArgumentException) { return true; }
    }

    private static IReadOnlyList<string> Mushrooms =>
        ItemCatalog.InSet(ItemCatalog.MushroomSet).Select(i => i.Key).ToList();

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Bag();
        Memory();
        Nim();
        Treasure();
        Texts();
        Cards();
        Draw();
    }

    // ---- The card of every game at every step, and the draw ---------------------------------------

    private static IEnumerable<Discord.ButtonComponent> Buttons(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
            switch (c)
            {
                case Discord.ButtonComponent b: yield return b; break;
                case Discord.ContainerComponent x: foreach (var b in Buttons(x.Components)) yield return b; break;
                case Discord.ActionRowComponent x: foreach (var b in Buttons(x.Components)) yield return b; break;
            }
    }

    private static IEnumerable<Discord.ActionRowComponent> Rows(IEnumerable<Discord.IMessageComponent> components) =>
        components.OfType<Discord.ActionRowComponent>();

    private static void Cards()
    {
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var plynling = new ProjectSYNCS.Models.Plynling
        {
            Name = "Pépin", Species = ProjectSYNCS.Models.PlynlingSpecies.Amanite, AdoptedAt = now.AddDays(-60), NeedsAsOf = now, Hygiene = 1.0,
        };
        foreach (var game in Enum.GetValues<PlynlingGame>())
            for (var seed = 0; seed < 20; seed++)
                PlayThrough(game, seed, aimToWin: seed % 2 == 0, s =>
                {
                    var session = new ProjectSYNCS.Services.PlynlingGameSession("abc123def456", 1, 2, 3, s, now);
                    var what = $"card: {game} seed {seed} {s.Status}";
                    Discord.MessageComponent card;
                    try { card = ProjectSYNCS.Commands.PlynlingPlayCards.BuildGame(session, plynling, now, s.Status == GameStatus.Playing ? null : "Fin."); }
                    catch (Exception ex) { _fail($"play: {what} throws {ex.GetType().Name}: {ex.Message}"); return; }
                    var parts = card.Components.Select(EconomySinkChecks.Measure).ToList();
                    var (count, ids) = (parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
                    Expect(count <= 40 && ids.Count == ids.Distinct().Count() && ids.All(i => i.Length <= 100),
                        $"{what}: {count} components, {ids.Count - ids.Distinct().Count()} duplicate id(s)");
                    var buttons = Buttons(card.Components).ToList();
                    Expect(buttons.All(b => (b.Label?.Length ?? 0) <= 80), $"{what}: a label over 80 characters");
                    if (s.Status != GameStatus.Playing)
                    {
                        Expect(buttons.All(b => b.IsDisabled), $"{what}: a live button on a finished game");
                        return;
                    }
                    Expect(buttons.Any(b => !b.IsDisabled), $"{what}: no button to play");
                    Expect(Rows(card.Components).All(r => r.Components.Count <= 5), $"{what}: a row of more than 5 buttons");
                    switch (game)
                    {
                        case PlynlingGame.Memory when !s.Memorised:
                            Expect(buttons.Count == 1 && buttons[0].Style == Discord.ButtonStyle.Primary, $"{what}: one blurple « J'ai retenu ! »");
                            break;
                        case PlynlingGame.Memory:
                            Expect(buttons.Count == PlynlingGameState.MemoryOptions && Rows(card.Components).Count() == 2
                                && buttons.All(b => b.Emote is not null && !string.IsNullOrEmpty(b.Label)),
                                $"{what}: six mushroom buttons with emoji and name, two rows");
                            break;
                        case PlynlingGame.LastPebble:
                            Expect(buttons.Count == PlynlingGameState.NimMaxTake
                                && buttons.Select((b, i) => b.IsDisabled == (i + 1 > s.Pile)).All(x => x),
                                $"{what}: « 1 2 3 », taking more than the pile ({s.Pile}) disabled");
                            Expect(buttons.All(b => b.CustomId.EndsWith($":{s.Step}")), $"{what}: nim buttons carry the step they were drawn at");
                            break;
                        case PlynlingGame.TreasureHunt:
                            Expect(buttons.Count == 16 && Rows(card.Components).Count() == 4
                                && s.Dug.Keys.All(c => buttons[c].IsDisabled && buttons[c].Emote?.Name == PlynlingGameUi.HintEmoji(s.Dug[c]))
                                && buttons.Where((_, c) => !s.Dug.ContainsKey(c)).All(b => !b.IsDisabled && b.Style == Discord.ButtonStyle.Primary),
                                $"{what}: 16 cells in four rows, dug ones disabled with their hint");
                            break;
                        case PlynlingGame.PebbleBag:
                            Expect(buttons.Count == 2 && buttons.All(b => b.Style == Discord.ButtonStyle.Primary), $"{what}: ⬆️ ⬇️ blurple");
                            Expect(buttons.All(b => b.CustomId.EndsWith($":{s.Step}")), $"{what}: bag buttons carry the step they were drawn at");
                            break;
                    }
                });
    }

    private static void Draw()
    {
        var play = new ProjectSYNCS.Services.PlynlingPlayService();
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        var rng = new Random(7);
        var without = Enumerable.Range(0, 600).Select(_ => play.Start(1, 2, 3, now, rng, Array.Empty<string>()).State.Game).ToHashSet();
        Expect(!without.Contains(PlynlingGame.Memory), "draw: Mémoire drawn with no mushroom emojis loaded");
        Expect(without.Count == Enum.GetValues<PlynlingGame>().Length - 1, $"draw: without mushrooms only {without.Count} games drawn");
        var with = Enumerable.Range(0, 600).Select(_ => play.Start(1, 2, 3, now, rng, Mushrooms).State.Game).ToHashSet();
        Expect(with.Count == Enum.GetValues<PlynlingGame>().Length, $"draw: with mushrooms only {with.Count} games drawn");
    }

    // ---- The text of every game at every step -----------------------------------------------------

    // Plays a seeded game of each kind to its end with a fixed policy, calling see() on every state.
    private static void PlayThrough(PlynlingGame game, int seed, bool aimToWin, Action<PlynlingGameState> see)
    {
        var rng = new Random(seed);
        var s = new PlynlingGameState(game, rng, Mushrooms);
        see(s);
        var guard = 0;
        while (s.Status == GameStatus.Playing && guard++ < 50)
        {
            var step = s.Step;
            switch (game)
            {
                case PlynlingGame.HideAndSeek: s.Hide(aimToWin ? s.HiddenBehind : (s.HiddenBehind + 1) % PlynlingGameState.Rocks, rng); break;
                case PlynlingGame.RockPaperScissors: s.Throw((RpsThrow)rng.Next(3), rng); break;
                case PlynlingGame.PebbleBag: s.Bag(s.Pebble <= 5, rng); break;
                case PlynlingGame.Memory:
                    if (!s.Memorised) s.Memorise();
                    else s.Pick(aimToWin ? IndexOf(s.Options, s.Sequence[s.Progress])
                        : Enumerable.Range(0, s.Options.Count).First(i => !s.Sequence.Contains(s.Options[i])));
                    break;
                case PlynlingGame.LastPebble:
                    var best = PlynlingGameState.NimBest(s.Pile);
                    s.Take(aimToWin && best > 0 ? best : 1, rng);
                    break;
                case PlynlingGame.TreasureHunt:
                    s.Dig(aimToWin ? s.Treasure : Enumerable.Range(0, 16).First(c => c != s.Treasure && !s.Dug.ContainsKey(c)));
                    break;
            }
            Expect(s.Step == step + 1, $"step: {game} move did not advance the step ({step} → {s.Step})");
            see(s);
        }
    }

    private static void Texts()
    {
        var names = Mushrooms.Select(PlynlingGameUi.MushroomLabel).ToList();
        Expect(names.All(n => n.Length is > 0 and <= 80), "text: a mushroom label is empty or over 80 characters");
        Expect(PlynlingGameUi.HintEmoji(DigHint.Burning) == "🔥" && PlynlingGameUi.HintEmoji(DigHint.Warm) == "♨️"
            && PlynlingGameUi.HintEmoji(DigHint.Cold) == "❄️", "text: dig hints are 🔥 ♨️ ❄️");

        var ends = new HashSet<(PlynlingGame, GameStatus)>();
        foreach (var game in Enum.GetValues<PlynlingGame>())
            for (var seed = 0; seed < 40; seed++)
                foreach (var gender in new[] { ProjectSYNCS.Models.PlynlingGender.Male, ProjectSYNCS.Models.PlynlingGender.Female })
                    PlayThrough(game, seed, aimToWin: seed % 2 == 0, s =>
                    {
                        var text = PlynlingGameUi.Text(s, "Bulle", gender);
                        Expect(text.StartsWith("## ") && text.Length <= 1500 && !text.Contains('{') && !text.Contains('}'),
                            $"text: {game} {s.Status}: bad text « {text} »");
                        if (s.Status != GameStatus.Playing) ends.Add((game, s.Status));
                        Expect(!text.Contains("cachée") || game != PlynlingGame.TreasureHunt, $"text: treasure « cachée » agrees with the Plynling, not le gland: {text}");
                        Expect(!text.Contains("jamais deux fois"), $"text: the bag promises no repeats, but only the showing pebble is excluded: {text}");
                        Expect(!text.Contains("il est à toi"), $"text: « il est à toi » has no clear antecedent: {text}");
                        if (game == PlynlingGame.LastPebble && s.Status == GameStatus.Playing && s.Pile < PlynlingGameState.NimMaxTake)
                            Expect(!text.Contains("1, 2 ou 3"), $"text: nim offers 3 with {s.Pile} left: {text}");
                        if (game == PlynlingGame.Memory && s.Status == GameStatus.Playing)
                        {
                            var shown = s.Sequence.Select(PlynlingGameUi.MushroomLabel).Count(text.Contains);
                            Expect(s.Memorised ? shown < s.Sequence.Count : shown == s.Sequence.Count,
                                $"text: memory shows {shown} of the sequence (memorised: {s.Memorised})");
                        }
                    });
        foreach (var game in Enum.GetValues<PlynlingGame>())
            foreach (var status in new[] { GameStatus.Won, GameStatus.Lost })
                Expect(ends.Contains((game, status)), $"text: {game} never reached {status} in the seeded games");
    }

    // ---- Le sac de cailloux -----------------------------------------------------------------------

    private static void Bag()
    {
        const int games = 20000;
        var wins = 0;
        for (var seed = 0; seed < games; seed++)
        {
            var rng = new Random(seed);
            var s = new PlynlingGameState(PlynlingGame.PebbleBag, rng);
            Expect(s.Pebble is >= PlynlingGameState.BagMin and <= PlynlingGameState.BagMax, $"bag: first pebble {s.Pebble} out of range");
            while (s.Status == GameStatus.Playing)
            {
                var before = s.Pebble;
                var streak = s.Streak;
                var higher = before <= 5;                      // the careful call
                var right = s.Bag(higher, rng);
                if (s.Pebble == before) { Expect(false, "bag: a tie was counted"); break; }
                var shouldBeRight = higher ? s.Pebble > before : s.Pebble < before;
                Expect(right == shouldBeRight, $"bag: {before} then {s.Pebble}, called {(higher ? "higher" : "lower")}, judged {right}");
                Expect(right ? s.Streak == streak + 1 : s.Status == GameStatus.Lost, "bag: a right call adds one, a wrong one loses");
            }
            if (s.Status == GameStatus.Won)
            {
                wins++;
                Expect(s.Streak == PlynlingGameState.BagToWin, $"bag: won at streak {s.Streak}");
            }
            for (var i = 1; i < s.Drawn.Count; i++)
                if (s.Drawn[i] == s.Drawn[i - 1]) { Expect(false, "bag: two equal pebbles in a row in Drawn"); break; }
            if (seed == 0) Expect(Throws(() => s.Bag(true, rng)), "bag: a move after the end must throw");
        }
        var rate = wins / (double)games;
        Expect(rate is >= 0.38 and <= 0.48, $"bag: careful win rate {rate:P1}, expected ~43 %");
    }

    // ---- Mémoire ----------------------------------------------------------------------------------

    private static void Memory()
    {
        var keys = Mushrooms;
        Expect(keys.Count >= PlynlingGameState.MemoryOptions, $"memory: only {keys.Count} mushrooms in the catalog");
        Expect(Throws(() => new PlynlingGameState(PlynlingGame.Memory, new Random(1), keys.Take(PlynlingGameState.MemoryOptions - 1).ToList())),
            "memory: too few mushrooms must throw");
        Expect(Throws(() => new PlynlingGameState(PlynlingGame.Memory, new Random(1))), "memory: no mushrooms must throw");

        for (var seed = 0; seed < 300; seed++)
        {
            var rng = new Random(seed);
            var s = new PlynlingGameState(PlynlingGame.Memory, rng, keys);
            Expect(s.Sequence.Count == PlynlingGameState.MemoryLength && s.Sequence.Distinct().Count() == s.Sequence.Count,
                "memory: the sequence has 4 distinct mushrooms");
            Expect(s.Options.Count == PlynlingGameState.MemoryOptions && s.Options.Distinct().Count() == s.Options.Count,
                "memory: 6 distinct options");
            Expect(s.Sequence.All(s.Options.Contains), "memory: every mushroom of the sequence is an option");
            Expect(s.Options.All(keys.Contains), "memory: options come from the given mushrooms");
            Expect(!s.Memorised && Throws(() => s.Pick(0)), "memory: a pick before « J'ai retenu » must throw");

            s.Memorise();
            Expect(s.Memorised, "memory: Memorise sets Memorised");
            if (seed % 2 == 0)
            {
                foreach (var key in s.Sequence) s.Pick(IndexOf(s.Options, key));
                Expect(s.Status == GameStatus.Won && s.Progress == PlynlingGameState.MemoryLength, "memory: the sequence in order wins");
            }
            else
            {
                Expect(s.Pick(IndexOf(s.Options, s.Sequence[0])), "memory: the first right pick is right");
                var decoy = Enumerable.Range(0, s.Options.Count).First(i => !s.Sequence.Contains(s.Options[i]));
                Expect(!s.Pick(decoy), "memory: a decoy is wrong");
                Expect(s.Status == GameStatus.Lost && s.WrongSlot == decoy && s.Progress == 1, "memory: a decoy loses and is remembered");

                // A double click on a right mushroom: the second click (same, stale card) changes nothing.
                var d = new PlynlingGameState(PlynlingGame.Memory, new Random(seed), keys);
                d.Memorise();
                var first = IndexOf(d.Options, d.Sequence[0]);
                d.Pick(first);
                d.Pick(first);
                Expect(d.Status == GameStatus.Playing && d.Progress == 1, "memory: clicking a mushroom already found twice loses");
            }
        }
    }

    private static int IndexOf(IReadOnlyList<string> list, string key)
    {
        for (var i = 0; i < list.Count; i++)
            if (list[i] == key) return i;
        return -1;
    }

    // ---- Le dernier caillou -----------------------------------------------------------------------

    private static void Nim()
    {
        Expect(PlynlingGameState.NimBest(13) == 1 && PlynlingGameState.NimBest(12) == 0
            && PlynlingGameState.NimBest(7) == 3 && PlynlingGameState.NimBest(2) == 2, "nim: NimBest leaves a multiple of 4");

        var probe = new PlynlingGameState(PlynlingGame.LastPebble, new Random(3));
        Expect(Throws(() => probe.Take(0, new Random(3))) && Throws(() => probe.Take(4, new Random(3))), "nim: takes outside 1–3 must throw");

        double Rate(bool perfect)
        {
            const int games = 20000;
            var wins = 0;
            for (var seed = 0; seed < games; seed++)
            {
                var rng = new Random(seed);
                var s = new PlynlingGameState(PlynlingGame.LastPebble, rng);
                if (!perfect && seed == 0)
                    Expect(s.Pile is >= PlynlingGameState.NimMin and <= PlynlingGameState.NimMax, $"nim: pile {s.Pile} out of range");
                while (s.Status == GameStatus.Playing)
                {
                    var pile = s.Pile;
                    if (pile < PlynlingGameState.NimMaxTake && !Throws(() => s.Take(pile + 1, rng)))
                    { Expect(false, $"nim: taking {pile + 1} from {pile} must throw"); break; }
                    var take = perfect && PlynlingGameState.NimBest(pile) > 0
                        ? PlynlingGameState.NimBest(pile)
                        : perfect ? 1 : rng.Next(1, Math.Min(PlynlingGameState.NimMaxTake, pile) + 1);
                    s.Take(take, rng);
                    Expect(s.Pile >= 0 && s.LastTake == take, "nim: the pile never goes negative, LastTake is the take");
                    if (s.Status == GameStatus.Won) Expect(s.Pile == 0 && take == pile, "nim: the player won by taking the last");
                    if (s.Status == GameStatus.Lost) Expect(s.Pile == 0 && s.PlynlingTake is > 0, "nim: the Plynling took the last");
                }
                if (s.Status == GameStatus.Won) wins++;
            }
            return wins / (double)games;
        }

        var random = Rate(perfect: false);
        var best = Rate(perfect: true);
        Expect(random is >= 0.11 and <= 0.21, $"nim: random clicker wins {random:P1}, expected ~16 %");
        Expect(best is >= 0.72 and <= 0.82, $"nim: a player who knows the trick wins {best:P1}, expected ~77 %");
    }

    // ---- Chasse au trésor -------------------------------------------------------------------------

    private static void Treasure()
    {
        Expect(PlynlingGameState.HintFor(0, 0) == DigHint.Found && PlynlingGameState.HintFor(0, 1) == DigHint.Burning
            && PlynlingGameState.HintFor(0, 5) == DigHint.Warm && PlynlingGameState.HintFor(0, 15) == DigHint.Cold
            && PlynlingGameState.HintFor(5, 6) == DigHint.Burning && PlynlingGameState.HintFor(3, 12) == DigHint.Cold,
            "treasure: hints by Manhattan distance 0 / 1 / 2 / 3+");

        for (var seed = 0; seed < 200; seed++)
        {
            var s = new PlynlingGameState(PlynlingGame.TreasureHunt, new Random(seed));
            var cells = PlynlingGameState.GridSize * PlynlingGameState.GridSize;
            Expect(s.Treasure >= 0 && s.Treasure < cells, $"treasure: cell {s.Treasure} off the grid");
            Expect(Throws(() => s.Dig(-1)) && Throws(() => s.Dig(cells)), "treasure: a dig off the grid must throw");

            var misses = Enumerable.Range(0, cells).Where(c => c != s.Treasure).Take(PlynlingGameState.Digs).ToList();
            Expect(!s.Dig(misses[0]) && s.Dug.Count == 1 && s.Dug[misses[0]] == PlynlingGameState.HintFor(misses[0], s.Treasure),
                "treasure: a miss records its hint");
            Expect(!s.Dig(misses[0]) && s.Dug.Count == 1 && s.Status == GameStatus.Playing, "treasure: digging the same cell twice changes nothing");
            s.Dig(misses[1]);
            Expect(s.Status == GameStatus.Playing, "treasure: two misses still play");
            s.Dig(misses[2]);
            Expect(s.Status == GameStatus.Lost, "treasure: three misses lose");

            var w = new PlynlingGameState(PlynlingGame.TreasureHunt, new Random(seed));
            Expect(w.Dig(w.Treasure) && w.Status == GameStatus.Won && w.Dug[w.Treasure] == DigHint.Found, "treasure: digging the gland wins");
        }
    }
}
