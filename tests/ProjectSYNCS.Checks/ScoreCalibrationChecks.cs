using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 68 (part). The Plynling score's calibration (docs/agents/plynling.md, *Score*): a well-cared life, simulated
// day by day through the real point values, the badge catalog, the stages, the RPG curve and the visit odds,
// must reach each tombstone at about the age the old rule used (7, 28, 90, 180 days); a neglected life must get
// there clearly later; and the rebuild, fed only what the database keeps of the same life, must land on the
// same tombstone or one tier off. The thresholds in PlynlingCatalog.MemorialScores come out of this: retune by
// re-running it (`-- score-sim` prints the days), never by feel.
public static class ScoreCalibrationChecks
{
    private static Action<string> _fail = _ => { };
    private static readonly DateTimeOffset T0 = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly int[] OldAges = { 7, 28, 90, 180 };   // the old rule's day for tiers 2..5
    private const int Days = 260;
    private const double Tolerance = 0.20;

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("score calibration: " + what);
    }

    // One way of looking after a Plynling, as rates a day (fractions accumulate: 0.5 is every other day).
    public sealed record Profile(
        string Name,
        double Meals, PlynlingFood[] Foods, double Pets, double Baths, double Plays, double WinRate,
        double Visits, double Forages, double Drills, double Events, int LongEvery, double GoodMornings,
        int CollectionEvery, int FriendMealDay);

    // An engaged owner, not a perfect one: about one meal a day (hunger lasts three days), a few pets (the
    // owner's and friends'), a bath every other day, a game, a forage, two drills from ado on, a visit every
    // other day, an event every other day (one run in six a long one), a collection every ten days, well kept
    // at 05:00 every morning.
    public static readonly Profile WellCared = new("well-cared",
        Meals: 1.0, Foods: new[] { PlynlingFood.Shiitake, PlynlingFood.Mushroom }, Pets: 3, Baths: 0.5, Plays: 1, WinRate: 0.5,
        Visits: 0.5, Forages: 1, Drills: 2, Events: 0.5, LongEvery: 6, GoodMornings: 1, CollectionEvery: 10, FriendMealDay: 3);

    // Kept alive and little more: mushrooms, a pet a day, rare baths and forages, no games, visits or drills,
    // a few events, well kept one morning in three.
    public static readonly Profile Neglected = new("neglected",
        Meals: 0.6, Foods: new[] { PlynlingFood.Mushroom }, Pets: 1, Baths: 0.25, Plays: 0, WinRate: 0,
        Visits: 0, Forages: 0.3, Drills: 0, Events: 0.2, LongEvery: 0, GoodMornings: 0.33, CollectionEvery: 0, FriendMealDay: 0);

    // A simulated life: the live score each day, and what the database would hold at the end of each day.
    public sealed record Day(int Index, long Live, PlynlingHistory Stored);

    public static List<Day> Simulate(Profile f)
    {
        var p = PlynlingLife.Create(1, 2, "Sim", PlynlingSpecies.Cepe, PlynlingGender.Female, T0);
        p.Id = 1;
        var earned = new HashSet<string>();
        var rng = new Random(20261124);
        long score = 0;
        int traits = 0, recoveries = 0, shortEnds = 0, longEnds = 0, collections = 0, runs = 0, foodIndex = 0;
        var bondsReached = new HashSet<(int Pair, PlynlingBond Bond)>();
        // Three regular visit partners, each with its own fixed compatibility, met in turn.
        var pairs = new[] { (Compat: 12, Affinity: 0, Bond: PlynlingBond.Acquaintances, Scored: 0),
                            (Compat: -8, Affinity: 0, Bond: PlynlingBond.Acquaintances, Scored: 0),
                            (Compat: 4, Affinity: 0, Bond: PlynlingBond.Acquaintances, Scored: 0) };
        var acc = new Dictionary<string, double>();
        int Due(string key, double rate)
        {
            acc[key] = acc.GetValueOrDefault(key) + rate;
            var n = (int)Math.Floor(acc[key] + 1e-9);
            acc[key] -= n;
            return n;
        }
        void Badges(DateTimeOffset at, BadgeEvent evt = BadgeEvent.None)
        {
            foreach (var b in PlynlingBadges.Newly(p, earned, at, evt))
            {
                earned.Add(b.Key);
                score += b.Reward;                       // PlynlingScore.ForMoment(Badge, key)
            }
        }

        var days = new List<Day>();
        var stage = PlynlingStage.Baby;
        traits++; score += PlynlingScore.TraitGained;    // the childhood trait, drawn at adoption
        for (var d = 1; d <= Days; d++)
        {
            var at = T0.AddDays(d);
            if (f.GoodMornings > 0 && Due("morning", f.GoodMornings) > 0) score += PlynlingScore.GoodMorning;

            // Growing up, with a trait at each stage (the adolescent and adult draws).
            var now = PlynlingLife.StageAt(TimeSpan.FromDays(d));
            while (stage < now)
            {
                stage++;
                score += PlynlingScore.Stage(stage);
                if (stage is PlynlingStage.Teen or PlynlingStage.Adult) { traits++; score += PlynlingScore.TraitGained; }
            }
            // A rare illness, got over: two in a long life.
            if (d is 60 or 150) { recoveries++; score += PlynlingScore.Recovered; }

            for (var i = Due("meal", f.Meals); i > 0; i--)
            {
                p.Meals++;
                score += PlynlingScore.Meal(f.Foods[foodIndex++ % f.Foods.Length]);
            }
            if (f.FriendMealDay > 0 && d == f.FriendMealDay) p.FedByOthers++;
            for (var i = Due("pet", f.Pets); i > 0; i--) { p.Pets++; score += PlynlingScore.Pet; }
            score += Due("bath", f.Baths) * PlynlingScore.Bath;
            score += Due("forage", f.Forages) * PlynlingScore.Forage;
            for (var i = Due("play", f.Plays); i > 0; i--)
            {
                p.Plays++;
                if (Due("win", f.WinRate) > 0) p.PlaysWon++;
                score += PlynlingScore.Play;
            }
            if (d >= 7)
                for (var i = Due("drill", f.Drills); i > 0; i--)
                {
                    var before = PlynlingRpg.Level(p.RpgXp);
                    p.RpgXp = Math.Min(PlynlingRpg.XpForLevel(PlynlingRpg.MaxLevel), p.RpgXp + PlynlingRpg.DrillXp);
                    p.Drills++;
                    score += PlynlingScore.Drill + (PlynlingRpg.Level(p.RpgXp) - before) * PlynlingScore.LevelUp;
                }

            var evt = BadgeEvent.None;
            for (var i = Due("visit", f.Visits); i > 0; i--)
            {
                var k = (int)(p.Visits % pairs.Length);
                var pair = pairs[k];
                var good = rng.NextDouble() < ProjectSYNCS.Helpers.VisitOdds.GoodChance(pair.Compat, pair.Bond, pair.Affinity, 0);
                pair.Affinity = Math.Clamp(pair.Affinity + (good ? rng.Next(12, 18) : -rng.Next(12, 19)), -100, 100);
                var before = pair.Bond;
                var after = PlynlingBonds.BondFor(pair.Affinity, before);
                pair.Bond = after;
                p.Visits++;
                score += PlynlingScore.Visit + (good ? PlynlingScore.VisitGoodScene : 0);
                // The bond's moment, as PlynlingService.BondMoment writes it, scored once per pair and bond.
                var closer = PlynlingBonds.Closeness(after) < PlynlingBonds.Closeness(before);
                var reached = after != before && (after switch
                {
                    PlynlingBond.Friends or PlynlingBond.BestFriends => closer,
                    PlynlingBond.Rivals => before != PlynlingBond.Enemies,
                    PlynlingBond.Enemies => true,
                    _ => false,
                });
                if (reached)
                {
                    bondsReached.Add((k, after));
                    if ((pair.Scored & PlynlingScore.BondBit(after)) == 0)
                    {
                        pair.Scored |= PlynlingScore.BondBit(after);
                        score += PlynlingScore.Bond(after);
                    }
                    if (closer && after == PlynlingBond.Friends) evt = BadgeEvent.BecameFriends;
                    if (closer && after == PlynlingBond.BestFriends) evt = BadgeEvent.BecameBestFriends;
                }
                pairs[k] = pair;
            }

            for (var i = Due("event", f.Events); i > 0; i--)
            {
                runs++;
                if (f.LongEvery > 0 && runs % f.LongEvery == 0) { longEnds++; score += PlynlingScore.EventLong; }
                else { shortEnds++; score += PlynlingScore.EventShort; }
            }
            if (f.CollectionEvery > 0 && d % f.CollectionEvery == 0) { collections++; score += PlynlingScore.Collection; }

            Badges(at, evt);
            var stored = new PlynlingHistory(TimeSpan.FromDays(d), p.Meals, p.Pets, p.Plays, p.Visits, p.Drills, p.RpgXp,
                earned.ToList(), traits, recoveries, bondsReached.Select(b => b.Bond).ToList(), shortEnds, longEnds, collections);
            days.Add(new Day(d, score, stored));
        }
        return days;
    }

    // The first day a life's score reaches each tombstone (tiers 2..5), or null if it never does.
    public static int?[] TierDays(IEnumerable<Day> life, Func<Day, long> score) =>
        Enumerable.Range(2, 4).Select(tier => life.FirstOrDefault(d => PlynlingCatalog.MemorialTierForScore(score(d)) >= tier)?.Index).ToArray();

    public static void Run(Action<string> fail, string[] args)
    {
        _fail = fail;
        var cared = Simulate(WellCared);
        var neglected = Simulate(Neglected);
        var caredDays = TierDays(cared, d => d.Live);
        var neglectedDays = TierDays(neglected, d => d.Live);
        var rebuiltDays = TierDays(cared, d => PlynlingScoreHistory.Rebuild(d.Stored));

        if (args.Contains("score-sim"))
        {
            foreach (var day in new[] { 7, 14, 28, 60, 90, 120, 180, 200 })
                Console.WriteLine($"day {day,3}: well-cared {cared[day - 1].Live,5} (rebuilt {PlynlingScoreHistory.Rebuild(cared[day - 1].Stored),5}) · neglected {neglected[day - 1].Live,5}");
            Console.WriteLine($"tombstones (petite stèle, gravée, urne, statue) — old ages {string.Join("/", OldAges)}");
            Console.WriteLine($"  well-cared: {string.Join("/", caredDays.Select(Show))}");
            Console.WriteLine($"  rebuilt:    {string.Join("/", rebuiltDays.Select(Show))}");
            Console.WriteLine($"  neglected:  {string.Join("/", neglectedDays.Select(Show))}");
        }

        for (var i = 0; i < OldAges.Length; i++)
        {
            var old = OldAges[i];
            var tier = i + 2;
            Expect(caredDays[i] is { } day && Math.Abs(day - old) <= Math.Max(1, old * Tolerance),
                $"a well-cared life reaches tier {tier} on day {Show(caredDays[i])}, the old rule's day {old} ±{Tolerance:P0}");
            Expect(neglectedDays[i] is null || neglectedDays[i] >= old * 1.5,
                $"a neglected life reaches tier {tier} on day {Show(neglectedDays[i])}, not clearly later than {old}");
        }

        // The rebuild sees only what the database keeps; on every day of the well-cared life it must land on the
        // live tombstone or one tier off.
        foreach (var day in cared)
        {
            var live = PlynlingCatalog.MemorialTierForScore(day.Live);
            var rebuilt = PlynlingCatalog.MemorialTierForScore(PlynlingScoreHistory.Rebuild(day.Stored));
            if (Math.Abs(live - rebuilt) > 1)
            {
                Expect(false, $"day {day.Index}: rebuilt tier {rebuilt} against live tier {live}");
                break;
            }
        }
        // And a neglected history must not be rebuilt into a well-cared one.
        foreach (var day in new[] { 30, 90, 180 })
            Expect(PlynlingScoreHistory.Rebuild(neglected[day - 1].Stored) < cared[day - 1].Live * 0.6,
                $"a neglected life's rebuild on day {day} ({PlynlingScoreHistory.Rebuild(neglected[day - 1].Stored)}) is close to a well-cared one's ({cared[day - 1].Live})");
    }

    private static string Show(int? day) => day?.ToString() ?? "never";
}
