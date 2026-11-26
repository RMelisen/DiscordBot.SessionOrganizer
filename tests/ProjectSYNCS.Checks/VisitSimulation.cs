using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 57. The bond simulation (docs/agents/plynling.md, Relationships): today's rules reproduced, then the
// visit with choices played at random and played as well as possible. One visit per pair per day.
// The tables print with `-- visit-sim`; the checks always run.
public static class VisitSimulation
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("visit-sim: " + what);
    }

    // Baseline: today's (8.1.0) rules. Random: uniform choices, and « 💌 » pressed whenever it shows (the
    // owner's call: the button is rare, so an average owner presses it). Best: the choices with the
    // highest shift, and « 💌 » whenever it shows.
    private enum Player { Baseline, Random, Best }

    // Figures in plynling.md before the rework: the spread over 100 visits, couples within 60 for a
    // boy and a girl, the median couple time at compatibility +10.
    private static readonly double[] BaselineSpread = { 0.12, 0.18, 0.21, 0.19, 0.13, 0.16 };   // enemies, rivals, acq, friends, best, lovers
    private const double BaselineCouples60 = 0.30;
    private const double BaselineMedian10 = 51;

    private sealed record Scenes(IReadOnlyList<VisitScene> Ordinary, VisitScene Confession, string Source);

    public static void Run(Action<string> fail, string[] args)
    {
        _fail = fail;
        var scenes = LoadScenes();
        if (scenes is null) { Expect(false, "no scenes to simulate with (neither Stories/Visits nor the sheet loads)"); return; }

        // The simulation's shift is VisitOdds.Shift with the softness as a parameter, so a sweep can try others.
        var probeLean = new Dictionary<AiAxis, int> { [AiAxis.Boldness] = 2 };
        var probeAxes = new Dictionary<AiAxis, int> { [AiAxis.Boldness] = 200 };
        Expect(Math.Abs(Shift(probeLean, probeAxes, VisitOdds.LeanSoftness) - VisitOdds.Shift(probeLean, probeAxes)) < 1e-12, "the simulation's shift is VisitOdds.Shift");

        if (args.Contains("visit-sweep"))
        {
            foreach (var softness in new[] { 300.0, 600, 1000, 2000, 5000, 20000, 1e12 })
            {
                var swept = new Dictionary<Player, Result>();
                foreach (var player in Enum.GetValues<Player>()) swept[player] = Simulate(player, scenes, softness);
                Console.WriteLine(Table(swept, scenes.Source, softness));
            }
        }

        var results = new Dictionary<Player, Result>();
        foreach (var player in Enum.GetValues<Player>()) results[player] = Simulate(player, scenes, VisitOdds.LeanSoftness);
        var b = results[Player.Baseline];
        var r = results[Player.Random];
        var best = results[Player.Best];

        for (var i = 0; i < 6; i++)
            Expect(Math.Abs(b.Spread[i] - BaselineSpread[i]) <= 0.03, $"baseline {Band(i)} {b.Spread[i]:P0}, plynling.md says {BaselineSpread[i]:P0}");
        Expect(Math.Abs(b.Couples60 - BaselineCouples60) <= 0.04, $"baseline couples within 60 visits {b.Couples60:P0}, plynling.md says 30 %");
        Expect(Math.Abs(b.Median10 - BaselineMedian10) <= 6, $"baseline median couple time at +10: {b.Median10}, plynling.md says 51");
        for (var i = 0; i < 6; i++)
            Expect(Math.Abs(r.Spread[i] - b.Spread[i]) <= 0.03, $"random play {Band(i)} {r.Spread[i]:P0} vs baseline {b.Spread[i]:P0}");
        // « 💌 » on 35 % of the days (the owner's choice, 8.24.0) makes couples a little faster than the
        // old automatic confession; these bounds stop anything faster still from slipping in unnoticed.
        Expect(Math.Abs(r.Couples60 - b.Couples60) <= 0.08, $"random play couples within 60 {r.Couples60:P0} vs baseline {b.Couples60:P0}");
        Expect(best.Median10 >= 0.7 * b.Median10, $"best play median couple time at +10 {best.Median10} is under 0.7 × baseline's {b.Median10}");

        if (args.Contains("visit-sim")) Console.WriteLine(Table(results, scenes.Source, VisitOdds.LeanSoftness));
    }

    private static string Band(int i) => new[] { "enemies", "rivals", "acquaintances", "friends", "best friends", "lovers" }[i];

    private static int BandIndex(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Enemies => 0,
        PlynlingBond.Rivals => 1,
        PlynlingBond.Acquaintances => 2,
        PlynlingBond.Friends => 3,
        PlynlingBond.BestFriends => 4,
        _ => 5,
    };

    // The shipped scenes once they exist and load clean; before that, the approved writing sheet's blocks.
    private static Scenes? LoadScenes()
    {
        var root = StoryChecks.RepoRoot();
        var shipped = Path.Combine(root, "ProjectSYNCS", "Stories", "Visits");
        if (Directory.Exists(shipped) && VisitLibrary.Load(shipped, requireCoverage: true) is { Errors.Count: 0 } load)
            return new Scenes(load.Scenes.Where(s => !s.Confession).ToList(), load.Scenes.Single(s => s.Confession), "Stories/Visits");
        var sheet = Path.Combine(root, "docs", "plynling-visit-scenes-sheet.md");
        if (!File.Exists(sheet)) return null;
        var parsed = new List<VisitScene>();
        foreach (Match m in Regex.Matches(File.ReadAllText(sheet).Replace("\r\n", "\n"), "```\n(# (?:visit|confession):.*?)```", RegexOptions.Singleline))
        {
            var errors = new List<StoryError>();
            if (VisitParser.Parse("sheet", m.Groups[1].Value, errors) is { } s && errors.Count == 0) parsed.Add(s);
        }
        var confession = parsed.FirstOrDefault(s => s.Confession);
        return confession is null || parsed.Count < 2 ? null : new Scenes(parsed.Where(s => !s.Confession).ToList(), confession, "the writing sheet");
    }

    private sealed record Result(double[] Spread, double Couples60, double Median10, double Median20, double Confessions, double Refusals);

    private const int SpreadPairs = 12000, SpreadVisits = 100;
    private const int CouplePairs = 8000, CoupleVisits = 60;
    private const int MedianPairs = 1500, MedianVisits = 400;

    private static Result Simulate(Player player, Scenes scenes, double softness)
    {
        // The same pairs and the same luck for every player: only the rules and the choices differ.
        var spread = new double[6];
        var rng = new Random(11);
        for (var i = 0; i < SpreadPairs; i++)
        {
            var (a, b) = Ids(rng);
            var run = Play(player, scenes, a, b, rng.Next(2) == 0, rng.Next(2) == 0, SpreadVisits, new Random(rng.Next()), softness);
            spread[BandIndex(run.Final)]++;
        }
        for (var i = 0; i < 6; i++) spread[i] /= SpreadPairs;

        rng = new Random(12);
        var couples = 0;
        double confessions = 0, refusals = 0;
        for (var i = 0; i < CouplePairs; i++)
        {
            var (a, b) = Ids(rng);
            var run = Play(player, scenes, a, b, true, false, CoupleVisits, new Random(rng.Next()), softness);
            if (run.CoupleAt is not null) couples++;
            confessions += run.Confessions;
            refusals += run.Refusals;
        }
        return new Result(spread, (double)couples / CouplePairs, Median(player, scenes, 10, softness), Median(player, scenes, 20, softness),
            confessions / CouplePairs, refusals / CouplePairs);
    }

    // The median visit on which a boy and a girl of that compatibility become a couple (never = past the horizon).
    private static double Median(Player player, Scenes scenes, int compatibility, double softness)
    {
        var rng = new Random(13 + compatibility);
        var times = new List<int>();
        while (times.Count < MedianPairs)
        {
            var (a, b) = Ids(rng);
            if (PlynlingBonds.Compatibility(a, b) != compatibility) continue;
            var run = Play(player, scenes, a, b, true, false, MedianVisits, new Random(rng.Next()), softness);
            times.Add(run.CoupleAt ?? int.MaxValue);
        }
        times.Sort();
        return times[times.Count / 2];
    }

    private static (int, int) Ids(Random rng)
    {
        var a = rng.Next(1, 1_000_000);
        int b;
        do b = rng.Next(1, 1_000_000); while (b == a);
        return (a, b);
    }

    private sealed record Life(PlynlingBond Final, int? CoupleAt, int Confessions, int Refusals);

    // One pair, one visit a day: `aMale` / `bMale` their genders; the visitor drawn each day.
    private static Life Play(Player player, Scenes scenes, int a, int b, bool aMale, bool bMale, int visits, Random rng, double softness)
    {
        var pa = new Plynling { Id = a, Gender = aMale ? PlynlingGender.Male : PlynlingGender.Female };
        var pb = new Plynling { Id = b, Gender = bMale ? PlynlingGender.Male : PlynlingGender.Female };
        var axesA = PlynlingPersonality.Axes(PlynlingTraits.Draw(a, PlynlingStage.Adult, Array.Empty<string>()));
        var axesB = PlynlingPersonality.Axes(PlynlingTraits.Draw(b, PlynlingStage.Adult, Array.Empty<string>()));
        var compatibility = PlynlingBonds.Compatibility(a, b);
        var affinity = 0;
        var bond = PlynlingBond.Acquaintances;
        int? coupleAt = null;
        var lastRefusal = int.MinValue / 2;
        int confessions = 0, refusals = 0;

        for (var day = 1; day <= visits; day++)
        {
            var aVisits = rng.Next(2) == 0;
            var (visitor, host) = aVisits ? (pa, pb) : (pb, pa);
            var (vAxes, hAxes) = aVisits ? (axesA, axesB) : (axesB, axesA);
            var confession = Confession.None;
            bool good;
            if (player == Player.Baseline)
            {
                (good, var delta) = PlynlingBonds.RollScene(compatibility, bond, affinity, rng);
                affinity = Math.Clamp(affinity + delta, -100, 100);
                if (good && PlynlingBonds.CanConfess(PlynlingBonds.BondFor(affinity, bond), affinity, visitor, host, false))
                    confession = BaselineConfession(compatibility, rng);
            }
            else
            {
                // The bot's own rules: CanConfess, the refusal cooldown, and the pair's hashed offer days.
                var offered = PlynlingBonds.CanConfess(bond, affinity, visitor, host, false)
                              && day - lastRefusal >= PlynlingBonds.RefusalCooldown.TotalDays
                              && PlynlingBonds.ConfessionOffered(a, b, day);
                var eligible = scenes.Ordinary.Where(s => s.Bonds.Contains(bond)).ToList();
                var scene = offered ? scenes.Confession : eligible[rng.Next(eligible.Count)];
                var (option, reaction) = player == Player.Best
                    ? (scene.Options.MaxBy(o => Shift(o.Lean, hAxes, softness))!, scene.Reactions.MaxBy(x => Shift(x.Lean, vAxes, softness))!)
                    : (scene.Options[rng.Next(scene.Options.Count)], scene.Reactions[rng.Next(scene.Reactions.Count)]);
                var shift = Shift(option.Lean, hAxes, softness) + Shift(reaction.Lean, vAxes, softness);
                good = offered || rng.NextDouble() < VisitOdds.GoodChance(compatibility, bond, affinity, shift);
                var delta = good ? rng.Next(12, 18) : -rng.Next(12, 19);
                affinity = Math.Clamp(affinity + delta, -100, 100);
                if (offered) confession = rng.NextDouble() < VisitOdds.AcceptChance(compatibility, shift) ? Confession.Accepted : Confession.Refused;
            }
            if (confession != Confession.None) confessions++;
            if (confession == Confession.Refused)
            {
                affinity = Math.Clamp(affinity - PlynlingBonds.HeartbreakLoss, -100, 100);
                lastRefusal = day;
                refusals++;
            }
            bond = confession == Confession.Accepted ? PlynlingBond.Lovers : PlynlingBonds.BondFor(affinity, bond);
            if (bond == PlynlingBond.Lovers) coupleAt ??= day;
        }
        return new Life(bond, coupleAt, confessions, refusals);
    }

    // Today's (8.1.0) automatic confession, copied before PlynlingBonds lost it: after a good scene that
    // leaves the pair able to confess, a quarter of the time; accepted 50 % ± 35 % by compatibility.
    private static Confession BaselineConfession(int compatibility, Random rng)
    {
        if (rng.NextDouble() >= 0.25) return Confession.None;
        return rng.NextDouble() < 0.5 + 0.35 * compatibility / PlynlingBonds.MaxCompatibility ? Confession.Accepted : Confession.Refused;
    }

    private static double Shift(IReadOnlyDictionary<AiAxis, int> lean, IReadOnlyDictionary<AiAxis, int> axes, double softness) =>
        VisitOdds.MaxShiftPerChoice * Math.Tanh(VisitOdds.Match(lean, axes) / softness);

    private static string Table(Dictionary<Player, Result> results, string source, double softness)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"visit-sim — scenes from {source}; LeanSoftness {softness}, cap {VisitOdds.MaxShiftPerChoice} per choice");
        sb.AppendLine($"{"",-9} {"enem",5} {"riv",5} {"acq",5} {"frd",5} {"best",5} {"lov",5} | {"cpl≤60",6} {"med+10",6} {"med+20",6} {"conf/60",7} {"ref/60",6}");
        foreach (var (player, r) in results)
            sb.AppendLine($"{player,-9} {string.Join(" ", r.Spread.Select(x => $"{x * 100,4:0}%"))} | {r.Couples60 * 100,5:0}% {Med(r.Median10),6} {Med(r.Median20),6} {r.Confessions,7:0.00} {r.Refusals,6:0.00}");
        return sb.ToString();
    }

    private static string Med(double m) => m >= int.MaxValue ? "never" : m.ToString("0");
}
