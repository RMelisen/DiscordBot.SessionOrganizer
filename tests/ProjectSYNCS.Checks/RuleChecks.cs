using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 35–40, 44. /remindme times, quiz typed answers, blackjack, board games, the XP curve,
// cailloux and the daily streak.
public static class RuleChecks
{
    public static void Run()
    {
        // 35. /remindme's « quand » (ReminderTime): durations, clock times, days, the range, DST, garbage.
        // « 21h » / « 1h30 » alone are clock times; a duration in hours needs « dans » or a spelled unit.
        {
            DateTimeOffset RtParis(int y, int mo, int d, int h, int mi = 0) =>
                AppTime.AtWallClock(new DateTime(y, mo, d, h, mi, 0)).ToUniversalTime();
            string RtShow(DateTimeOffset? t) => t is null ? "null"
                : AppTime.ToZoned(t.Value).ToString("ddd dd/MM/yyyy HH:mm zzz", System.Globalization.CultureInfo.InvariantCulture);
            void WhenIs(string input, DateTimeOffset at, DateTimeOffset? expected)
            {
                var got = ReminderTime.Parse(input, at);
                if (got != expected) Fail($"remindme \"{input}\" at {RtShow(at)}: {RtShow(got)}, expected {RtShow(expected)}");
            }

            // Tuesday 27 October 2026, 14:00 Paris (winter time since the 25th).
            var rtTue = RtParis(2026, 10, 27, 14);
            var rtCases = new (string Input, DateTimeOffset? Expected)[]
            {
                // The README's examples.
                ("10m", rtTue.AddMinutes(10)), ("dans 2h", rtTue.AddHours(2)), ("dans 1h30", rtTue.AddMinutes(90)),
                ("3j", rtTue.AddDays(3)), ("21h30", RtParis(2026, 10, 27, 21, 30)), ("demain 9h", RtParis(2026, 10, 28, 9)),
                ("samedi 14h", RtParis(2026, 10, 31, 14)), ("25/12 10:00", RtParis(2026, 12, 25, 10)),

                // A bare hour is a clock time, never a duration.
                ("21h", RtParis(2026, 10, 27, 21)), ("2h", RtParis(2026, 10, 28, 2)), ("1h30", RtParis(2026, 10, 28, 1, 30)),
                ("1h 30", RtParis(2026, 10, 28, 1, 30)), ("21 h 30", RtParis(2026, 10, 27, 21, 30)), ("21:30", RtParis(2026, 10, 27, 21, 30)),
                ("à 21h", RtParis(2026, 10, 27, 21)),
                // A clock time already past, or exactly now, is tomorrow's.
                ("14h", RtParis(2026, 10, 28, 14)), ("13h59", RtParis(2026, 10, 28, 13, 59)), ("14h01", RtParis(2026, 10, 27, 14, 1)),
                ("0h", RtParis(2026, 10, 28, 0)),

                // Durations: « dans », or a spelled unit.
                ("dans 21h", rtTue.AddHours(21)), ("dans 1 h 30", rtTue.AddMinutes(90)), ("2 heures", rtTue.AddHours(2)),
                ("dans 2 heures et 5 minutes", rtTue.AddMinutes(125)), ("1h30min", rtTue.AddMinutes(90)), ("10 min", rtTue.AddMinutes(10)),
                ("dans 10 minutes", rtTue.AddMinutes(10)), ("1 semaine", rtTue.AddDays(7)), ("dans 2 jours", rtTue.AddDays(2)),
                ("2sem", rtTue.AddDays(14)),

                // Days: a day without a time is 9:00.
                ("demain", RtParis(2026, 10, 28, 9)), ("demain à 9h", RtParis(2026, 10, 28, 9)),
                ("après-demain 18h", RtParis(2026, 10, 29, 18)), ("apres demain 18h", RtParis(2026, 10, 29, 18)),
                ("aujourd'hui 18h", RtParis(2026, 10, 27, 18)),
                ("aujourd'hui 8h", RtParis(2026, 10, 27, 8)), // past on purpose: the caller says it's too soon
                ("ce soir 21h", RtParis(2026, 10, 27, 21)),
                ("ce soir 9h", RtParis(2026, 10, 27, 21)),    // « ce soir 9h » is 21:00 in French
                ("mardi 18h", RtParis(2026, 10, 27, 18)),     // today's name, hour still ahead: today
                ("mardi 10h", RtParis(2026, 11, 3, 10)),      // today's name, hour past: next week
                ("mardi", RtParis(2026, 11, 3, 9)), ("samedi", RtParis(2026, 10, 31, 9)), ("lundi 10h", RtParis(2026, 11, 2, 10)),
                ("dimanche 10h", RtParis(2026, 11, 1, 10)), ("le samedi à 14h", RtParis(2026, 10, 31, 14)),
                ("le 25/12", RtParis(2026, 12, 25, 9)), ("25/12/2027 8h", RtParis(2027, 12, 25, 8)), ("25/12/27 8h", RtParis(2027, 12, 25, 8)),
                ("1/1", RtParis(2027, 1, 1, 9)), ("26/10 10h", RtParis(2027, 10, 26, 10)), // a date past this year is next year's
                // Chat spellings, read by FrenchDate; « dans » + days with a time is a day, not a duration.
                ("ajd 18h", RtParis(2026, 10, 27, 18)), ("demain matin", RtParis(2026, 10, 28, 9)), ("demain soir", RtParis(2026, 10, 28, 20)),
                ("le 30 à 10h", RtParis(2026, 10, 30, 10)), ("dans 3 jours à 21h", RtParis(2026, 10, 30, 21)),

                // Case, accents and spacing don't count.
                ("  DEMAIN   9H  ", RtParis(2026, 10, 28, 9)), ("Après-Demain 18H", RtParis(2026, 10, 29, 18)), ("DANS 2H", rtTue.AddHours(2)),
                ("Samedi à 14h", RtParis(2026, 10, 31, 14)), ("10M", rtTue.AddMinutes(10)), ("dans  10   m", rtTue.AddMinutes(10)),

                // Garbage.
                ("", null), ("   ", null), ("bientôt", null), ("plus tard", null), ("30", null), ("dans 5", null), ("dans", null),
                ("dans 0m", null), ("0m", null), ("10 secondes", null), ("dans 3 mois", null), ("dans 100001m", null),
                ("dans 99999999999m", null), ("31/02 10h", null), ("29/02", null), ("32/01", null), ("12/13", null),
                ("demain 24h", null), ("lundi 25h", null), ("hier 9h", null),
                ("21h75", null), ("13h60", null), // not a clock time, and a duration in hours needs « dans »
            };
            foreach (var (input, expected) in rtCases) WhenIs(input, rtTue, expected);

            // The range is the caller's: 1 minute to 60 days, both ends included.
            if (ReminderTime.Min != TimeSpan.FromMinutes(1) || ReminderTime.Max != TimeSpan.FromDays(60)) Fail("remindme range: not 1 min .. 60 days");
            bool RtInRange(string input)
            {
                var due = ReminderTime.Parse(input, rtTue);
                return due is not null && due.Value - rtTue >= ReminderTime.Min && due.Value - rtTue <= ReminderTime.Max;
            }
            foreach (var input in new[] { "dans 1m", "14h01", "dans 60j", "8 semaines", "25/12 10:00" })
                if (!RtInRange(input)) Fail($"remindme \"{input}\": out of range, expected within");
            foreach (var input in new[] { "dans 61j", "90j", "9 semaines", "26/10 10h", "1/1", "25/12/2027 8h", "aujourd'hui 8h" })
                if (RtInRange(input)) Fail($"remindme \"{input}\": within range, expected refused");

            // Clocks going back (25 October 2026, 3:00 CEST → 2:00 CET): a wall-clock time keeps its hour,
            // a duration counts real hours.
            var rtAutumnEve = RtParis(2026, 10, 24, 20);
            WhenIs("demain 9h", rtAutumnEve, new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero));
            WhenIs("dans 24h", rtAutumnEve, rtAutumnEve.AddHours(24));
            WhenIs("9h", RtParis(2026, 10, 24, 23), new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero));
            WhenIs("dimanche 10h", RtParis(2026, 10, 23, 12), new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));
            var rtTwice = ReminderTime.Parse("demain 2h30", rtAutumnEve); // 2:30 happens twice: either is fine
            if (rtTwice != new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero) && rtTwice != new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero))
                Fail($"remindme \"demain 2h30\" on the night clocks go back: {RtShow(rtTwice)}");
            // Clocks going forward (29 March 2026, 2:00 CET → 3:00 CEST).
            var rtSpringEve = RtParis(2026, 3, 28, 20);
            WhenIs("demain 9h", rtSpringEve, new DateTimeOffset(2026, 3, 29, 7, 0, 0, TimeSpan.Zero));
            WhenIs("lundi 9h", rtSpringEve, new DateTimeOffset(2026, 3, 30, 7, 0, 0, TimeSpan.Zero));
            WhenIs("dans 12h", rtSpringEve, rtSpringEve.AddHours(12));
            var rtGap = ReminderTime.Parse("demain 2h30", rtSpringEve); // 2:30 never happens: somewhere around it, not lost
            if (rtGap is null || rtGap < new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero) || rtGap > new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero))
                Fail($"remindme \"demain 2h30\" on the night clocks go forward: {RtShow(rtGap)}");

            // The year turning.
            var rtNye = RtParis(2026, 12, 30, 12); // a Wednesday
            WhenIs("demain 9h", rtNye, RtParis(2026, 12, 31, 9));
            WhenIs("après-demain 9h", rtNye, RtParis(2027, 1, 1, 9));
            WhenIs("2/1 10h", rtNye, RtParis(2027, 1, 2, 10));
            WhenIs("vendredi 10h", rtNye, RtParis(2027, 1, 1, 10));
            Console.WriteLine($"remindme: {rtCases.Length + 13} readings");
        }

        // 36. Quiz typed answers (QuizAnswer): case, accents, sentences, typos by length, near misses.
        {
            var qzCount = 0;
            void QuizIs(string message, bool expected, params string[] accepted)
            {
                qzCount++;
                if (QuizAnswer.Matches(message, accepted) != expected)
                    Fail($"quiz \"{message}\" vs [{string.Join(" | ", accepted)}]: {!expected}, expected {expected}");
            }
            // Case, accents, punctuation, spacing.
            QuizIs("Pétrichor !", true, "pétrichor");
            QuizIs("petri chor", true, "pétrichor");
            QuizIs("PETRICHOR", true, "Pétrichor");
            QuizIs("coeur", true, "cœur");
            QuizIs("H2O !", true, "h2o");
            QuizIs("back rub", true, "BackRub");
            // Inside a sentence, as long as the rest is filler or at most two other words.
            QuizIs("je pense que c'est backrub non ?", true, "BackRub");
            QuizIs("alors euh je pense que c'est le pétrichor lol", true, "pétrichor");
            QuizIs("c'est luigi", true, "Luigi");
            QuizIs("mario luigi peach", true, "Luigi");
            QuizIs("mario luigi peach bowser", false, "Luigi");
            QuizIs("mario peach bowser toad", false, "Luigi");
            // Leading articles dropped from the accepted answer.
            QuizIs("vatican", true, "le Vatican");
            QuizIs("le vatican", true, "le Vatican");
            QuizIs("everest", true, "l'Everest");
            QuizIs("la tour eiffel", true, "Tour Eiffel");
            QuizIs("tour", false, "Tour Eiffel");
            QuizIs("eiffel", false, "Tour Eiffel");
            // Any accepted answer counts.
            QuizIs("mercury", true, "Mercure", "Mercury");
            // Typos: none under 5 letters, one from 5, two from 10, none with a digit.
            QuizIs("link", true, "Link");
            QuizIs("lint", false, "Link");
            QuizIs("mar", false, "Mars");
            QuizIs("mard", false, "Mars");
            QuizIs("luigii", true, "Luigi");
            QuizIs("lugi", true, "Luigi");
            QuizIs("lugia", false, "Luigi");
            QuizIs("louis", false, "Luigi");
            QuizIs("petrichore", true, "pétrichor");
            QuizIs("patrichar", false, "pétrichor");
            QuizIs("mitocondrie", true, "Mitochondrie");
            QuizIs("mytocondrie", true, "Mitochondrie");
            QuizIs("mytocondri", false, "Mitochondrie");
            QuizIs("h20", false, "h2o");
            QuizIs("pichu", false, "Pikachu");
            // A different word that merely contains the answer.
            QuizIs("artichaut", false, "art");
            QuizIs("ras", false, "Ra");
            // Nothing said.
            QuizIs("", false, "Luigi");
            QuizIs("!!! ???", false, "Luigi");
            var qzTokens = QuizAnswer.Tokenize("C'est l'Éverest, non ?!");
            if (!qzTokens.SequenceEqual(new[] { "c", "est", "l", "everest", "non" })) Fail($"quiz tokenize: {string.Join(",", qzTokens)}");
            Console.WriteLine($"quiz answers: {qzCount + 1} cases");
        }

        // 37. Blackjack: totals with soft aces, naturals, the dealer standing on every 17, judging, payouts.
        {
            List<Card> Hand(params int[] ranks) => ranks.Select(r => new Card(r, 0)).ToList();
            var bjCount = 0;
            void TotalIs(int expected, params int[] ranks)
            {
                bjCount++;
                if (Blackjack.Total(Hand(ranks)) != expected) Fail($"blackjack total [{string.Join(",", ranks)}]: {Blackjack.Total(Hand(ranks))}, expected {expected}");
            }
            TotalIs(21, 1, 13); TotalIs(12, 1, 1); TotalIs(21, 1, 1, 9); TotalIs(16, 1, 5, 13); TotalIs(21, 13, 12, 1);
            TotalIs(22, 13, 12, 2); TotalIs(11, 5, 6); TotalIs(17, 1, 6); TotalIs(14, 1, 1, 1, 1); TotalIs(20, 11, 12); TotalIs(0);
            if (!Blackjack.IsNatural(Hand(1, 13)) || !Blackjack.IsNatural(Hand(10, 1))) Fail("blackjack: ace + ten is a natural");
            if (Blackjack.IsNatural(Hand(13, 12, 1)) || Blackjack.IsNatural(Hand(7, 7, 7))) Fail("blackjack: three cards are never a natural");
            if (new Card(1, 0).ToString() != "A♠" || new Card(10, 1).ToString() != "10♥" || new Card(13, 3).ToString() != "R♣") Fail("blackjack: card labels");

            void JudgeIs(int[] player, int[] dealer, Blackjack.Outcome expected)
            {
                bjCount++;
                var got = Blackjack.Judge(Hand(player), Hand(dealer));
                if (got != expected) Fail($"blackjack judge [{string.Join(",", player)}] vs [{string.Join(",", dealer)}]: {got}, expected {expected}");
            }
            JudgeIs([10, 6, 9], [10, 6, 8], Blackjack.Outcome.Lose);  // both bust: the player busted first
            JudgeIs([1, 13], [1, 12], Blackjack.Outcome.Push);
            JudgeIs([1, 13], [7, 7, 7], Blackjack.Outcome.Natural);   // a natural beats a three-card 21
            JudgeIs([7, 7, 7], [1, 13], Blackjack.Outcome.Lose);
            JudgeIs([7, 7, 7], [10, 5, 6], Blackjack.Outcome.Push);
            JudgeIs([10, 10], [10, 6, 9], Blackjack.Outcome.Win);
            JudgeIs([10, 8], [9, 9], Blackjack.Outcome.Push);
            JudgeIs([10, 7], [10, 8], Blackjack.Outcome.Lose);
            JudgeIs([10, 9], [10, 8], Blackjack.Outcome.Win);
            JudgeIs([2, 3, 4, 5, 6], [10, 9], Blackjack.Outcome.Win);  // five cards to 20 beat 19
            JudgeIs([2, 3, 4, 5, 6], [10, 10], Blackjack.Outcome.Push);
            if (Blackjack.Net(Blackjack.Outcome.Natural, Blackjack.MinStake) != 15 || Blackjack.Net(Blackjack.Outcome.Natural, Blackjack.MaxStake) != 300
                || Blackjack.Net(Blackjack.Outcome.Win, 50) != 50 || Blackjack.Net(Blackjack.Outcome.Push, 50) != 0
                || Blackjack.Net(Blackjack.Outcome.Lose, 50) != -50) Fail("blackjack payouts: natural 3:2, win 1:1, push 0, loss the stake");

            var soft17 = Hand(1, 6);
            Blackjack.PlayDealer(soft17, new Random(1));
            if (soft17.Count != 2) Fail("blackjack: the dealer hits a soft 17");
            for (var seed = 0; seed < 500; seed++)
            {
                var dealer = Hand(10, 2);
                Blackjack.PlayDealer(dealer, new Random(seed));
                if (Blackjack.Total(dealer) < 17 || Blackjack.Total(dealer.Take(dealer.Count - 1).ToList()) >= 17)
                { Fail($"blackjack: dealer stops wrong ({Blackjack.Show(dealer)})"); break; }
            }
            var bjRng = new Random(5);
            var bjSeen = new HashSet<(int, int)>();
            for (var i = 0; i < 20_000; i++)
            {
                var c = Blackjack.Draw(bjRng);
                if (c.Rank is < 1 or > 13 || c.Suit is < 0 or > 3) { Fail($"blackjack draw out of range: {c.Rank}/{c.Suit}"); break; }
                bjSeen.Add((c.Rank, c.Suit));
            }
            if (bjSeen.Count != 52) Fail($"blackjack draw: {bjSeen.Count} of 52 cards ever drawn");
            Console.WriteLine($"blackjack: {bjCount} cases");
        }

        // 38. Board games: four (or three) in a row in every direction, no wrap-around, full boards, her moves.
        {
            int[] C4Board(params (int R, int C, int P)[] cells)
            {
                var b = Connect4.NewBoard();
                foreach (var (r, c, p) in cells) b[r * Connect4.Cols + c] = p;
                return b;
            }
            void C4WinnerIs(string what, int[] board, int expected)
            {
                var got = Connect4.Winner(board);
                if (got != expected) Fail($"puissance4 winner, {what}: {got}, expected {expected}");
            }
            C4WinnerIs("empty", Connect4.NewBoard(), 0);
            C4WinnerIs("bottom row", C4Board((5, 0, 1), (5, 1, 1), (5, 2, 1), (5, 3, 1)), 1);
            C4WinnerIs("top row, right edge", C4Board((0, 3, 2), (0, 4, 2), (0, 5, 2), (0, 6, 2)), 2);
            C4WinnerIs("column, right edge", C4Board((2, 6, 2), (3, 6, 2), (4, 6, 2), (5, 6, 2)), 2);
            C4WinnerIs("column, top", C4Board((0, 0, 1), (1, 0, 1), (2, 0, 1), (3, 0, 1)), 1);
            C4WinnerIs("diagonal ↘", C4Board((2, 0, 1), (3, 1, 1), (4, 2, 1), (5, 3, 1)), 1);
            C4WinnerIs("diagonal ↙", C4Board((2, 6, 2), (3, 5, 2), (4, 4, 2), (5, 3, 2)), 2);
            C4WinnerIs("diagonal ↙, top left", C4Board((0, 3, 1), (1, 2, 1), (2, 1, 1), (3, 0, 1)), 1);
            C4WinnerIs("three in a row", C4Board((5, 0, 1), (5, 1, 1), (5, 2, 1), (5, 3, 2)), 0);
            C4WinnerIs("three and a gap", C4Board((5, 0, 1), (5, 1, 1), (5, 2, 1), (5, 4, 1)), 0);
            C4WinnerIs("wrapping round a row end", C4Board((0, 5, 1), (0, 6, 1), (1, 0, 1), (1, 1, 1)), 0);
            C4WinnerIs("wrapping round a diagonal", C4Board((1, 4, 1), (2, 5, 1), (3, 6, 1), (5, 0, 1)), 0); // cells 11, 19, 27, 35
            var c4Col = Connect4.NewBoard();
            var c4Rows = Enumerable.Range(0, 6).Select(_ => Connect4.Drop(c4Col, 2, 1)).ToArray();
            if (!c4Rows.SequenceEqual(new[] { 5, 4, 3, 2, 1, 0 })) Fail($"puissance4 drop rows: {string.Join(",", c4Rows)}");
            if (Connect4.CanDrop(c4Col, 2) || Connect4.Drop(c4Col, 2, 2) != -1) Fail("puissance4: a full column takes a piece");
            if (Connect4.Drop(c4Col, 7, 1) != -1 || Connect4.Drop(c4Col, -1, 1) != -1) Fail("puissance4: a column off the board takes a piece");
            C4WinnerIs("stacked by Drop", c4Col, 1);
            // Full with nobody winning: pairs of columns alternating each row (runs of two at most).
            var c4Draw = new int[Connect4.Rows * Connect4.Cols];
            for (var r = 0; r < Connect4.Rows; r++)
            for (var c = 0; c < Connect4.Cols; c++) c4Draw[r * Connect4.Cols + c] = 1 + (c / 2 + r) % 2;
            if (!Connect4.Full(c4Draw) || Connect4.Winner(c4Draw) != 0) Fail("puissance4: a full board without four isn't a draw");
            if (Connect4.Full(c4Col)) Fail("puissance4: one full column is a full board");
            if (Connect4.BestMove(c4Draw, 2, new Random(1)) != -1) Fail("puissance4: a move on a full board");
            for (var seed = 0; seed < 5; seed++)
            {
                var c4Rng = new Random(seed);
                // She takes her win.
                if (Connect4.BestMove(C4Board((5, 0, 2), (5, 1, 2), (5, 2, 2), (5, 5, 1), (5, 6, 1), (4, 6, 1)), 2, c4Rng) != 3)
                    Fail("puissance4: she misses her four");
                // She blocks theirs.
                if (Connect4.BestMove(C4Board((5, 4, 1), (4, 4, 1), (3, 4, 1), (5, 0, 2), (5, 6, 2)), 2, c4Rng) != 4)
                    Fail("puissance4: she leaves their four open");
                // Her win before their block.
                if (Connect4.BestMove(C4Board((5, 0, 2), (4, 0, 2), (3, 0, 2), (5, 6, 1), (4, 6, 1), (3, 6, 1)), 2, c4Rng) != 0)
                    Fail("puissance4: she blocks instead of winning");
            }

            // Morpion.
            var tttLines = new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };
            foreach (var line in tttLines)
            foreach (var p in new[] { 1, 2 })
            {
                var b = TicTacToe.NewBoard();
                foreach (var i in line) b[i] = p;
                if (TicTacToe.Winner(b) != p) Fail($"morpion: line {string.Join("", line)} for {p} not a win");
                b[line[2]] = 3 - p;
                if (TicTacToe.Winner(b) != 0) Fail($"morpion: mixed line {string.Join("", line)} is a win");
            }
            var tttDraw = new[] { 1, 2, 1, 1, 2, 2, 2, 1, 1 };
            if (!TicTacToe.Full(tttDraw) || TicTacToe.Winner(tttDraw) != 0) Fail("morpion: the drawn board");
            if (TicTacToe.Winner(TicTacToe.NewBoard()) != 0 || TicTacToe.Full(TicTacToe.NewBoard())) Fail("morpion: the empty board");
            if (TicTacToe.BestMove(new[] { 2, 2, 0, 1, 1, 0, 1, 0, 0 }, 2, new Random(1)) != 2) Fail("morpion: she misses her win");
            if (TicTacToe.BestMove(new[] { 1, 1, 0, 0, 2, 0, 0, 0, 0 }, 2, new Random(1)) != 2) Fail("morpion: she leaves their line open");
            // She never loses: every reply to every one of her moves, her first or second.
            int tttEnds = 0, tttLosses = 0;
            void TttExplore(int[] b, int her, int toMove, int seed)
            {
                var w = TicTacToe.Winner(b);
                if (w != 0 || TicTacToe.Full(b))
                {
                    tttEnds++;
                    if (w != 0 && w != her && tttLosses++ == 0) Fail($"morpion: she loses on {string.Join("", b)}");
                    return;
                }
                if (toMove == her)
                {
                    var m = TicTacToe.BestMove(b, her, new Random(seed + tttEnds));
                    if (m is < 0 or > 8 || b[m] != 0) { Fail($"morpion: illegal move {m} on {string.Join("", b)}"); return; }
                    b[m] = her; TttExplore(b, her, 3 - toMove, seed); b[m] = 0;
                    return;
                }
                for (var i = 0; i < 9; i++)
                {
                    if (b[i] != 0) continue;
                    b[i] = toMove; TttExplore(b, her, 3 - toMove, seed); b[i] = 0;
                }
            }
            for (var seed = 0; seed < 3; seed++)
            {
                TttExplore(TicTacToe.NewBoard(), her: 2, toMove: 1, seed);
                TttExplore(TicTacToe.NewBoard(), her: 1, toMove: 1, seed);
            }
            Console.WriteLine($"morpion: {tttEnds} games played out against her, {tttLosses} lost");
        }

        // 39. The XP curve (LevelCurve): costs, thresholds and the reverse lookup agree at every boundary.
        {
            if (LevelCurve.XpForLevel(0) != 100 || LevelCurve.XpForLevel(1) != 155 || LevelCurve.XpForLevel(-5) != 100) Fail("level curve: XpForLevel");
            if (LevelCurve.ThresholdForLevel(0) != 0 || LevelCurve.ThresholdForLevel(1) != 100 || LevelCurve.ThresholdForLevel(2) != 255
                || LevelCurve.ThresholdForLevel(-3) != 0) Fail("level curve: ThresholdForLevel");
            for (var n = 0; n <= 3000; n++)
            {
                var at = LevelCurve.ThresholdForLevel(n);
                if (LevelCurve.ThresholdForLevel(n + 1) - at != LevelCurve.XpForLevel(n)) { Fail($"level curve: threshold step at {n}"); break; }
                if (LevelCurve.XpForLevel(n + 1) <= LevelCurve.XpForLevel(n)) { Fail($"level curve: not increasing at {n}"); break; }
                if (LevelCurve.LevelForXp(at) != n) { Fail($"level curve: LevelForXp({at}) = {LevelCurve.LevelForXp(at)}, expected {n}"); break; }
                if (n > 0 && LevelCurve.LevelForXp(at - 1) != n - 1) { Fail($"level curve: one XP short of {n}"); break; }
                if (LevelCurve.XpIntoLevel(at) != 0 || LevelCurve.XpForNextLevel(at) != LevelCurve.XpForLevel(n)) { Fail($"level curve: progress at {n}"); break; }
            }
            if (LevelCurve.LevelForXp(0) != 0 || LevelCurve.LevelForXp(-50) != 0 || LevelCurve.LevelForXp(99) != 0) Fail("level curve: below level 1");
            if (LevelCurve.LevelForXp(long.MaxValue) != 1_000_000) Fail($"level curve: the cap ({LevelCurve.LevelForXp(long.MaxValue)})");
            var lcRng = new Random(11);
            for (var i = 0; i < 2000; i++)
            {
                var total = (long)(lcRng.NextDouble() * 50_000_000);
                var level = LevelCurve.LevelForXp(total);
                var into = LevelCurve.XpIntoLevel(total);
                var left = LevelCurve.XpForNextLevel(total);
                if (into < 0 || left <= 0 || into + left != LevelCurve.XpForLevel(level)) { Fail($"level curve: progress at {total} XP"); break; }
            }
        }

        // 40. Cailloux (PebbleEconomy): the passive cap stays a bonus, the cooldown, the shift pay, the plural.
        {
            var cap = PebbleEconomy.PassiveDailyCap;
            if (cap <= 0 || cap >= PebbleEconomy.ShiftsPerDay * PebbleEconomy.WorkMin) Fail($"economy: passive cap {cap} isn't a bonus below a day's /work");
            if (PebbleEconomy.Passive(0, 1) != (1, 1)) Fail("economy: passive from nothing");
            if (PebbleEconomy.Passive(cap - 1, 5) != (1, cap)) Fail("economy: passive up to the cap");
            if (PebbleEconomy.Passive(cap, 5) != (0, cap)) Fail("economy: passive past the cap");
            if (PebbleEconomy.Passive(cap + 10, 1) != (0, cap + 10)) Fail("economy: passive over a lowered cap");
            if (PebbleEconomy.Passive(0, -3) != (0, 0)) Fail("economy: a negative grant");
            var peAt = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
            if (PebbleEconomy.NextWorkAt(null) is not null || PebbleEconomy.NextWorkAt(peAt) != peAt.AddHours(4)) Fail("economy: work cooldown");
            // The /work DM reminder: once per finished cooldown, never for one that ended before opting in.
            if (!PebbleEconomy.WorkReminderDue(peAt, null, peAt.AddHours(4))) Fail("work reminder: due when the cooldown ends");
            if (PebbleEconomy.WorkReminderDue(peAt, null, peAt.AddHours(4).AddSeconds(-1))) Fail("work reminder: due before the cooldown ends");
            if (PebbleEconomy.WorkReminderDue(null, null, peAt)) Fail("work reminder: due without ever working");
            if (PebbleEconomy.WorkReminderDue(peAt, peAt.AddHours(5), peAt.AddHours(6))) Fail("work reminder: due twice for one shift");
            if (!PebbleEconomy.WorkReminderDue(peAt, peAt.AddHours(-1), peAt.AddHours(4))) Fail("work reminder: not due after an earlier shift's reminder");
            var midShift = PebbleEconomy.ReminderStampOnEnable(peAt, null, peAt.AddHours(1));
            if (!PebbleEconomy.WorkReminderDue(peAt, midShift, peAt.AddHours(4))) Fail("work reminder: switched on mid-cooldown, this shift's DM is lost");
            var lateOn = PebbleEconomy.ReminderStampOnEnable(peAt, null, peAt.AddHours(5));
            if (PebbleEconomy.WorkReminderDue(peAt, lateOn, peAt.AddHours(6))) Fail("work reminder: switched on after the cooldown ended, still DMs");
            if (PebbleEconomy.WorkReminderDue(null, PebbleEconomy.ReminderStampOnEnable(null, null, peAt), peAt.AddHours(5))) Fail("work reminder: never worked, still DMs");
            // The /work grace: the last minutes of the cooldown only, once per Paris day.
            var peNext = peAt.AddHours(4);
            if (!PebbleEconomy.WorkGraceApplies(peNext, peNext - PebbleEconomy.WorkGrace, 0, 20261027)) Fail("work grace: refused at the window's edge");
            if (!PebbleEconomy.WorkGraceApplies(peNext, peNext.AddSeconds(-30), 20261026, 20261027)) Fail("work grace: yesterday's grace blocks today's");
            if (PebbleEconomy.WorkGraceApplies(peNext, peNext - PebbleEconomy.WorkGrace - TimeSpan.FromSeconds(1), 0, 20261027)) Fail("work grace: granted before the window");
            if (PebbleEconomy.WorkGraceApplies(peNext, peNext.AddSeconds(-30), 20261027, 20261027)) Fail("work grace: granted twice in a day");
            if (PebbleEconomy.WorkGraceApplies(peNext, peNext, 0, 20261027)) Fail("work grace: spent on a shift that was already available");
            var pays = Enumerable.Range(0, 2000).Select(_ => PebbleEconomy.RollWorkPay()).ToList();
            if (pays.Min() != PebbleEconomy.WorkMin || pays.Max() != PebbleEconomy.WorkMax) Fail($"economy: work pay {pays.Min()}..{pays.Max()}");
            if (PebbleEconomy.Cailloux(0) != "0 caillou" || PebbleEconomy.Cailloux(1) != "1 caillou" || PebbleEconomy.Cailloux(-1) != "-1 caillou"
                || PebbleEconomy.Cailloux(2) != "2 cailloux" || !PebbleEconomy.Cailloux(1500).EndsWith("500 cailloux")
                || PebbleEconomy.Cailloux(1500).Contains(',')) Fail($"economy: plural ({PebbleEconomy.Cailloux(1500)})");
        }

        // 44. Daily streak (DailyStreak): one claim per Paris day, a missed day resets, the pay and the
        // item chance grow and stay capped.
        {
            if (DailyStreak.NextStreak(0, 0, 20261116) != 1) Fail("daily: first claim ever");
            if (DailyStreak.NextStreak(20261116, 4, 20261116) is not null) Fail("daily: twice in a day");
            if (DailyStreak.NextStreak(20261117, 4, 20261116) is not null) Fail("daily: a stamp from the future pays");
            if (DailyStreak.NextStreak(20261115, 4, 20261116) != 5) Fail("daily: yesterday continues");
            if (DailyStreak.NextStreak(20261114, 40, 20261116) != 1) Fail("daily: a missed day resets");
            foreach (var (last, today) in new[] { (20261130, 20261201), (20261231, 20270101), (20260228, 20260301),
                                                  (20261025, 20261026), (20261024, 20261025), (20260329, 20260330), (20260328, 20260329) })
                if (DailyStreak.NextStreak(last, 3, today) != 4) Fail($"daily: {last} -> {today} is not consecutive");

            if (DailyStreak.Base(1) != 10 || DailyStreak.Base(2) != 15 || DailyStreak.Base(19) != 100 || DailyStreak.Base(10_000) != 100) Fail("daily: base");
            if (DailyStreak.Spread(5) != 5 || DailyStreak.Spread(6) != 10 || DailyStreak.Spread(14) != 10 || DailyStreak.Spread(15) != 15) Fail("daily: spread");
            var dRng = new Random(44);
            foreach (var (dStreak, lo, hi) in new[] { (1, 5L, 15L), (6, 25L, 45L), (19, 85L, 100L), (10_000, 85L, 100L) })
            {
                var rolls = Enumerable.Range(0, 3000).Select(_ => DailyStreak.RollCailloux(dStreak, dRng)).ToList();
                if (rolls.Min() != lo || rolls.Max() != hi) Fail($"daily: streak {dStreak} rolls {rolls.Min()}..{rolls.Max()}, want {lo}..{hi}");
            }
            if (Math.Abs(DailyStreak.ItemChance(1) - 0.10) > 1e-9 || Math.Abs(DailyStreak.ItemChance(26) - 0.60) > 1e-9
                || Math.Abs(DailyStreak.ItemChance(10_000) - 0.60) > 1e-9) Fail("daily: item chance");

            var dEvening = new DateTimeOffset(2026, 11, 16, 22, 30, 0, TimeSpan.Zero);          // 23:30 Paris (CET)
            if (DailyStreak.NextClaimAt(dEvening) != new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero)) Fail("daily: next claim in winter");
            var dSummer = new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero);             // first CEST day
            if (DailyStreak.NextClaimAt(dSummer) != new DateTimeOffset(2026, 3, 29, 22, 0, 0, TimeSpan.Zero)) Fail("daily: next claim in summer");
            if (DailyStreak.Days(1) != "1 jour" || DailyStreak.Days(12) != "12 jours") Fail("daily: plural");

            // Days she was offline 3 h or more are excused; overlapping records count once (Paris = UTC+1 in November).
            DateTimeOffset Paris(int day, int hour, int minute = 0) => new DateTimeOffset(2026, 11, day, hour, minute, 0, TimeSpan.FromHours(1));
            string Excused(params (DateTimeOffset, DateTimeOffset)[] spans) => string.Join(",", DailyStreak.ExcusedDays(spans).OrderBy(d => d));
            foreach (var (what, got, want) in new[]
            {
                ("an evening outage", Excused((Paris(10, 19), Paris(10, 23))), "20261110"),
                ("just under 3 h", Excused((Paris(10, 19), Paris(10, 21, 59))), ""),
                ("across midnight, 2 h + 1 h", Excused((Paris(10, 22), Paris(11, 1))), ""),
                ("evening to next morning", Excused((Paris(10, 18), Paris(11, 10))), "20261110,20261111"),
                ("two stretches adding up", Excused((Paris(10, 8), Paris(10, 9, 30)), (Paris(10, 20), Paris(10, 21, 30))), "20261110"),
                ("overlapping records count once", Excused((Paris(10, 19), Paris(10, 21)), (Paris(10, 19, 30), Paris(10, 20, 30))), ""),
                ("overlapping records merged", Excused((Paris(10, 19), Paris(10, 21)), (Paris(10, 20), Paris(10, 22))), "20261110"),
                ("three days down", Excused((Paris(10, 12), Paris(13, 12))), "20261110,20261111,20261112,20261113"),
                ("a reversed record", Excused((Paris(10, 23), Paris(10, 19))), ""),
            })
                if (got != want) Fail($"daily: excused days, {what}: {got}, want {want}");
            var dExcused = new HashSet<int> { 20261109, 20261110 };
            if (DailyStreak.NextStreak(20261108, 7, 20261111, dExcused) != 8) Fail("daily: an excused gap continues");
            if (DailyStreak.NextStreak(20261108, 7, 20261111, new HashSet<int> { 20261110 }) != 1) Fail("daily: a partly excused gap doesn't reset");
            if (DailyStreak.NextStreak(20261110, 7, 20261111, dExcused) != 8) Fail("daily: yesterday with excuses");
            if (DailyStreak.NextStreak(20261111, 7, 20261111, dExcused) is not null) Fail("daily: excuses pay twice in a day");
            if (DailyStreak.NextStreak(0, 0, 20261111, dExcused) != 1) Fail("daily: excuses before the first claim");

            // The reply, shared by /daily and ;daily: one of her lines on top, then the streak and the
            // cailloux, a find appended, completed sets after it. A refusal carries no line.
            var dAt = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
            var dPicker = new ResponsePicker();
            for (var i = 0; i < 60; i++)
            {
                var (dText, dRefused) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(true, 12, 62, null, dAt), 1, false, dPicker);
                var dLines = dText.Split('\n');
                if (dRefused || dLines.Length != 3 || dLines[0].Trim().Length == 0 || dLines[1] != "Série : 12 jours" || dLines[2] != "+62 cailloux")
                    Fail($"daily reply: {dText}");
            }
            var dItem = ItemCatalog.All.First(i => i.Kind == ItemKind.Collectible);
            var dAllSets = new ItemFind(dItem, ItemCatalog.Sets.ToList());
            var (dFound, _) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(true, 365, 100, dAllSets, dAt), ulong.MaxValue, false, dPicker);
            if (!dFound.Split('\n', 2)[1].StartsWith("Série : 365 jours\n+100 cailloux  ·  trouvé : ") || dFound.Length > 2000) Fail($"daily reply with a find ({dFound.Length})");
            var (dNo, dNoRefused) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(false, 3, 0, null, dAt), 1, false, dPicker);
            // Papa, Tata and a chouchou open on their own lines; Papa and Tata win over a chouchou flag.
            foreach (var (dWho, dChouchou, dPool) in new (ulong, bool, string)[]
                     {
                         (AvailabilityService.OwnerId, false, "OwnerDailyLines"), (AvailabilityService.OwnerId, true, "OwnerDailyLines"),
                         ((ulong)Field("TataId"), true, "TataDailyLines"), (1, true, "ChouchouDailyLines"), (1, false, "DailyLines"),
                     })
            {
                var dLines = (string[])Field(dPool);
                if (dLines.Length < 6) Fail($"daily: {dPool} has {dLines.Length} lines");
                var (dOwn, _) = ProjectSYNCS.Commands.EconomyModule.DailyReply(new DailyResult(true, 2, 50, null, dAt), dWho, dChouchou, dPicker);
                if (!dLines.Contains(dOwn.Split('\n')[0])) Fail($"daily: {dWho} (chouchou {dChouchou}) didn't get {dPool}: {dOwn}");
                if (dPool != "DailyLines" && dLines.Any(l => l.Contains('{') || l.Contains("onjour") || l.Contains("onsoir")))
                    Fail($"daily: {dPool} has a placeholder or a greeting tied to the hour");
            }
            if (!dNoRefused || !dNo.Contains($"<t:{dAt.ToUnixTimeSeconds()}:R>")) Fail("daily reply: refusal");
            if (PlynlingShortcut.Parse(";daily") != new Shortcut(ShortcutKind.Daily) || PlynlingShortcut.Parse(";pl daily") != new Shortcut(ShortcutKind.Daily)
                || PlynlingShortcut.Parse(";daily x") != new Shortcut(ShortcutKind.Unknown) || PlynlingShortcut.Parse(";dailyx") is not null) Fail("daily shortcut");
            if (CommandPath.Shortcut(ShortcutKind.Daily) != ";daily" || CommandPath.Reached(";daily") != "daily") Fail("daily shortcut key");
            // Each help names every shortcut (plynling.md).
            if (!new[] { 0, 1, 2 }.Any(p => ProjectSYNCS.Commands.PlynlingModule.BuildHelpEmbed(p).Description?.Contains("`;daily`") == true))
                Fail("daily: /plynling help doesn't name ;daily");
        }
    }
}
