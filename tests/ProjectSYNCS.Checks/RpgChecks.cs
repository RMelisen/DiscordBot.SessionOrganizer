using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 65. The RPG foundation (docs/agents/plynling-rpg.md): the body stats and where they come from,
// the XP curve and the points, Énergie, the body-stat odds, the drill, and what the card shows.
public static class RpgChecks
{
    private static Action<string> _fail = _ => { };
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("rpg: " + what);
    }

    private static readonly PlynlingStat[] BodyStats =
        { PlynlingStat.Force, PlynlingStat.Agility, PlynlingStat.Endurance, PlynlingStat.Intelligence, PlynlingStat.Speed };

    private static Plynling Make(int id, PlynlingSpecies species = PlynlingSpecies.Cepe, TimeSpan? age = null)
    {
        var p = PlynlingLife.Create(1, 2, "Test", species, PlynlingGender.Female, Now - (age ?? TimeSpan.FromDays(20)));
        p.Id = id;
        return p;
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        try { await ServiceAsync(); }
        catch (Exception ex) { Expect(false, "service threw: " + ex); }
        BodyStatList();
        SpeciesLeans();
        Breakdown();
        AdultDrawUnchanged();
        Practice();
        Curve();
        Pace();
        Points();
        Energy();
        Odds();
        Drill();
        CardLine();
        Card();
        Buttons();
        Replies();
        Pools();
        UndefinedStats();
        DistinctEmojis();
        PersonalityBeforeAdo();
        OwnerButtons();
    }

    // A forged custom-id (« rpg:point:1:42 ») parses to an undefined value: never a body stat.
    private static void UndefinedStats()
    {
        Expect(!PlynlingStats.IsBody((PlynlingStat)42) && !PlynlingStats.IsBody((PlynlingStat)(-1)), "an undefined value is no body stat");
        Expect(BodyStats.All(PlynlingStats.IsBody), "every body stat is one");
    }

    // The card lines up trait, stat and modifier icons side by side: a new one must not look like an old one.
    private static void DistinctEmojis()
    {
        var traitEmojis = PlynlingTraits.All.Select(t => t.DefaultEmoji).ToHashSet();
        foreach (var stat in PlynlingStats.All)
        {
            var e = PlynlingStats.Emoji(stat);
            Expect(!traitEmojis.Contains(e), $"{stat}'s {e} is also a trait's icon");
            Expect(PlynlingStats.All.Count(s => PlynlingStats.Emoji(s) == e) == 1, $"{stat}'s {e} is another stat's");
        }
        foreach (var stat in BodyStats)
        {
            var m = PlynlingModifiers.Practice(stat);
            Expect(PlynlingModifiers.All.Count(o => o.Emoji == m.Emoji) == 1, $"{m.Key}'s {m.Emoji} is another modifier's");
            Expect(!traitEmojis.Contains(m.Emoji), $"{m.Key}'s {m.Emoji} is a trait's icon");
        }
    }

    // The RPG starts at ado and leaves the mascot out: « Personnalité » shows body stats only where it applies.
    private static void PersonalityBeforeAdo()
    {
        var traits = Array.Empty<TraitInfo>();
        string Stats(Plynling p) => PlynlingPersonality.DetailEmbed(p, traits, Now).Fields.Single(f => f.Name == "Statistiques").Value;
        var baby = Stats(Make(61, age: TimeSpan.FromDays(2)));
        Expect(!baby.Contains("Force") && baby.Contains("Courage"), "a bébé shows its mind stats only");
        var ado = Stats(Make(62));
        Expect(BodyStats.All(s => ado.Contains(PlynlingStats.Name(s))), "an ado shows the body stats");
    }

    // « Personnalité » brings the point buttons back to the owner: a dismissed drill answer must not leave
    // the points stranded until the next drill.
    private static void OwnerButtons()
    {
        var p = Make(71);
        p.OwnerId = 2;
        Expect(ProjectSYNCS.Commands.PlynlingTrainCards.OwnerPointButtons(p, 2, Now) is null, "no point, no buttons");
        p.RpgXp = PlynlingRpg.XpForLevel(3);
        Expect(ProjectSYNCS.Commands.PlynlingTrainCards.OwnerPointButtons(p, 2, Now) is not null, "the owner gets the buttons");
        Expect(ProjectSYNCS.Commands.PlynlingTrainCards.OwnerPointButtons(p, 3, Now) is null, "someone else does not");
        p.DiedAt = Now;
        Expect(ProjectSYNCS.Commands.PlynlingTrainCards.OwnerPointButtons(p, 2, Now) is null, "not once dead");
    }

    private static void CardLine()
    {
        var traits = Array.Empty<TraitInfo>();
        Expect(PlynlingRpg.CardLine(Make(31, age: TimeSpan.FromDays(2)), traits, Now) is null, "no RPG line for a bébé");
        var dead = Make(32);
        dead.DiedAt = Now;
        Expect(PlynlingRpg.CardLine(dead, traits, Now) is null, "no RPG line once dead");
        var p = Make(33);
        var max = PlynlingRpg.MaxEnergy(p, traits);
        Expect(PlynlingRpg.CardLine(p, traits, Now) == $"Niv. 1 · ⚡ {max}/{max}", $"line: {PlynlingRpg.CardLine(p, traits, Now)}");
        p.RpgXp = PlynlingRpg.XpForLevel(2);
        Expect(PlynlingRpg.CardLine(p, traits, Now) == $"Niv. 2 · ⚡ {max}/{max} · 1 point à placer", "one point to place");
        p.RpgXp = PlynlingRpg.XpForLevel(4);
        p.Energy = 12.7;
        p.EnergyAsOf = Now;
        Expect(PlynlingRpg.CardLine(p, traits, Now) == $"Niv. 4 · ⚡ 12/{max} · 3 points à placer", $"three points, energy floored: {PlynlingRpg.CardLine(p, traits, Now)}");
    }

    // Components V2: count the whole tree, like the other cards.
    private static int Count(Discord.IMessageComponent c) => 1 + c switch
    {
        Discord.ContainerComponent x => x.Components.Sum(Count),
        Discord.SectionComponent x => x.Components.Sum(Count) + (x.Accessory is null ? 0 : Count(x.Accessory)),
        Discord.ActionRowComponent x => x.Components.Sum(Count),
        _ => 0,
    };

    private static string Texts(Discord.IMessageComponent c) => c switch
    {
        Discord.ContainerComponent x => string.Join("\n", x.Components.Select(Texts)),
        Discord.SectionComponent x => string.Join("\n", x.Components.Select(Texts)),
        Discord.TextDisplayComponent x => x.Content,
        _ => "",
    };

    // The RPG line rides inside the status text: the card has as many components as before.
    private static void Card()
    {
        var traits = new[] { PlynlingTraits.ByKey("brave")!, PlynlingTraits.ByKey("calm")!, PlynlingTraits.ByKey("curious")! };
        var ado = Make(41);
        ado.RpgXp = PlynlingRpg.XpForLevel(3);
        var baby = Make(42, age: TimeSpan.FromDays(2));
        foreach (var p in new[] { ado, baby }) { p.Hunger = p.Happiness = p.Hygiene = 0.9; p.NeedsAsOf = Now; }
        var pantry = new Dictionary<PlynlingFood, int>();
        Discord.MessageComponent Build(Plynling p) => ProjectSYNCS.Commands.PlynlingModule.BuildCard(p, Now, "Une ligne.", null, "Partenaire",
            pantry, traits, pendingEvents: 2, bathSoap: 1, title: "Un titre");
        var withLine = Build(ado);
        var without = Build(baby);
        var n = withLine.Components.Sum(Count);
        Expect(n == without.Components.Sum(Count), $"the RPG line adds a component: {n} vs {without.Components.Sum(Count)}");
        Expect(n <= 40, $"card at {n} components");
        var text = string.Join("\n", withLine.Components.Select(Texts));
        Expect(text.Contains("Niv. 3 · ⚡") && text.Contains("2 points à placer"), "the card shows the level, the energy and the points");
    }

    private static void Buttons()
    {
        var card = ProjectSYNCS.Commands.PlynlingTrainCards.PointButtons(1);
        var rows = card.Components.OfType<Discord.ActionRowComponent>().ToList();
        Expect(rows.Count == 1, "one row");
        var buttons = rows.SelectMany(r => r.Components).OfType<Discord.ButtonComponent>().ToList();
        Expect(buttons.Count == 5, "five buttons, one per body stat");
        Expect(buttons.Select(b => b.CustomId).SequenceEqual(BodyStats.Select(s => $"rpg:point:1:{s}")), "ids rpg:point:1:<stat>");
        Expect(buttons.All(b => b.Style == Discord.ButtonStyle.Primary && b.Label.Length <= 80), "blurple picks, short labels");
    }

    private static string Pick(string[] pool) => pool[0];

    private static void Replies()
    {
        var p = Make(51);
        var max = 120;
        var done = new TrainResult(TrainOutcome.Done, p, new DrillResult(10, 1, 1, false, 105), max);
        var (text, buttons) = ProjectSYNCS.Commands.PlynlingTrainCards.Reply(done, PlynlingStat.Force, Pick);
        Expect(text.Contains("+10 XP") && text.Contains("Niv. 1") && text.Contains("⚡ 105/120"), $"drill reply: {text}");
        Expect(buttons is null, "no point, no buttons");
        p.RpgXp = PlynlingRpg.XpForLevel(2);
        var boosted = new TrainResult(TrainOutcome.Done, p, new DrillResult(10, 1, 2, true, 90), max);
        (text, buttons) = ProjectSYNCS.Commands.PlynlingTrainCards.Reply(boosted, PlynlingStat.Speed, Pick);
        Expect(text.Contains("💨 Vitesse +1") && text.Contains("Niv. 2"), $"boost and level shown: {text}");
        Expect(buttons is not null, "a point to place brings the buttons");
        var tired = new TrainResult(TrainOutcome.NoEnergy, p, MaxEnergy: max, EnoughAt: Now.AddHours(2));
        (text, buttons) = ProjectSYNCS.Commands.PlynlingTrainCards.Reply(tired, PlynlingStat.Force, Pick);
        Expect(text.Contains($"<t:{Now.AddHours(2).ToUnixTimeSeconds()}:R>") && buttons is not null, $"too tired says when, keeps the buttons: {text}");
        foreach (var outcome in Enum.GetValues<TrainOutcome>().Where(o => o is not TrainOutcome.Done and not TrainOutcome.NoEnergy))
        {
            var (refusal, _) = ProjectSYNCS.Commands.PlynlingTrainCards.Reply(new TrainResult(outcome, outcome == TrainOutcome.NoPlynling ? null : p), PlynlingStat.Force, Pick);
            Expect(!string.IsNullOrWhiteSpace(refusal) && refusal != PlynlingText.Unknown, $"refusal for {outcome}");
        }
    }

    // BotResponses is internal: its pools are read by reflection, like the rest of the checks.
    private static readonly Type Br = typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;

    private static readonly string[] PoolNames =
    {
        "PlynlingDrillForceLines", "PlynlingDrillAgilityLines", "PlynlingDrillEnduranceLines", "PlynlingDrillIntelligenceLines",
        "PlynlingDrillSpeedLines", "PlynlingDrillBoostLines", "PlynlingLevelUpLines", "PlynlingPointLines",
    };

    // Every new pool: both halves, at least six lines each, the halves agreeing with the Plynling, a
    // template that formats with its arguments, and at most a third opening on its name.
    private static void Pools()
    {
        // The plynlingui rules: whole words il/elle, mort/morte, and a pronoun after a hyphen (« regarde-le »).
        // Adjectives stay free — SYNCS is « contente » in either half.
        var maleOnly = new[] { "il", "lui-même", "mort" };
        var femaleOnly = new[] { "elle", "elle-même", "morte" };
        foreach (var name in PoolNames)
        {
            var pool = Br.GetField(name)?.GetValue(null) as GenderedLines;
            if (pool is null) { Expect(false, $"pool {name} missing"); continue; }
            foreach (var (half, lines, banned) in new[] { ("M", pool.M, femaleOnly), ("F", pool.F, maleOnly) })
            {
                Expect(lines.Length >= 6, $"{name}.{half} has {lines.Length} lines");
                Expect(lines.Count(l => l.StartsWith("**{0}**")) * 3 <= lines.Length, $"{name}.{half}: too many lines open on the name");
                foreach (var line in lines)
                {
                    var words = System.Text.RegularExpressions.Regex.Split(line.ToLowerInvariant(), @"[^\p{L}\-']+");
                    var hyphen = half == "M" ? "-la" : "-le";
                    var hit = words.FirstOrDefault(w => banned.Contains(w) || w.EndsWith(hyphen));
                    Expect(hit is null, $"{name}.{half} agrees with the other gender ('{hit}'): {line}");
                    try { _ = string.Format(line, "Nom", 7); }
                    catch (FormatException) { Expect(false, $"{name}.{half} does not format: {line}"); }
                }
            }
            Expect(pool.M.Length == pool.F.Length, $"{name}: halves of different lengths");
        }
    }

    private static void Curve()
    {
        Expect(PlynlingRpg.XpForLevel(1) == 0 && PlynlingRpg.XpForLevel(0) == 0, "level 1 needs no XP");
        Expect(PlynlingRpg.XpForLevel(2) == 66, $"level 2 at 66, got {PlynlingRpg.XpForLevel(2)}");
        for (var n = 2; n <= PlynlingRpg.MaxLevel; n++)
        {
            Expect(PlynlingRpg.Level(PlynlingRpg.XpForLevel(n)) == n, $"Level(XpForLevel({n}))");
            Expect(PlynlingRpg.Level(PlynlingRpg.XpForLevel(n) - 1) == n - 1, $"Level just under {n}");
        }
        Expect(PlynlingRpg.Level(0) == 1, "0 XP is level 1");
        Expect(PlynlingRpg.Level(long.MaxValue / 2) == 100, "capped at 100");
    }

    // Cap 100 in about a year of full daily play (180 XP a day, training a third of it).
    private static void Pace()
    {
        static int DayOf(int level, int perDay) => (int)Math.Ceiling(PlynlingRpg.XpForLevel(level) / (double)perDay);
        Expect(Math.Abs(DayOf(10, 180) - 10) <= 1, $"level 10 on day {DayOf(10, 180)}");
        Expect(Math.Abs(DayOf(50, 180) - 126) <= 1, $"level 50 on day {DayOf(50, 180)}");
        Expect(Math.Abs(DayOf(100, 180) - 362) <= 1, $"level 100 on day {DayOf(100, 180)}");
        var trainingOnly = PlynlingRpg.DrillXp * 6;
        Expect(DayOf(100, trainingOnly) > 900, $"training alone reaches 100 on day {DayOf(100, trainingOnly)}");
        Console.WriteLine($"rpg pace (180 XP/day): level 10 day {DayOf(10, 180)}, 50 day {DayOf(50, 180)}, 100 day {DayOf(100, 180)}; training alone 100 day {DayOf(100, trainingOnly)}");
    }

    private static void Points()
    {
        var p = Make(11);
        Expect(PlynlingRpg.UnspentPoints(p) == 0, "level 1 has no point");
        p.RpgXp = PlynlingRpg.XpForLevel(5);
        Expect(PlynlingRpg.UnspentPoints(p) == 4, $"level 5 has 4 points, got {PlynlingRpg.UnspentPoints(p)}");
        p.PointsForce = 2; p.PointsSpeed = 1;
        Expect(PlynlingRpg.UnspentPoints(p) == 1, "spent points count, Speed included");
        p.PointsIntelligence = 5;
        Expect(PlynlingRpg.UnspentPoints(p) == 0, "never negative");
    }

    private static void Energy()
    {
        var p = Make(12);
        var traits = Array.Empty<TraitInfo>();
        var max = PlynlingRpg.MaxEnergy(p, traits);
        Expect(max == 100 + 5 * PlynlingStats.Innate(p, traits, PlynlingStat.Endurance), $"max energy {max}");
        Expect(p.EnergyAsOf == default && PlynlingRpg.EnergyAt(p, max, Now) == max, "a row that never spent reads full");
        p.Energy = max - 15;
        p.EnergyAsOf = Now;
        var later = PlynlingRpg.EnergyAt(p, max, Now.AddHours(6));
        Expect(Math.Abs(later - Math.Min(max, max - 15 + max / 4.0)) < 1e-9, $"6 h later {later}");
        p.Energy = 10;
        Expect(Math.Abs(PlynlingRpg.EnergyAt(p, max, Now.AddHours(1)) - (10 + max / 24.0)) < 1e-9, "refills max/24 an hour");
        Expect(PlynlingRpg.EnergyAt(p, max, Now.AddHours(-3)) == 10, "a clock going back refills nothing");
        Expect(PlynlingRpg.EnergyAt(p, max, Now.AddDays(3)) == max, "never past max");
        p.PointsEndurance = 4; p.GrowthEndurance = 2;
        Expect(PlynlingRpg.MaxEnergy(p, traits) == max, "points and growth never raise max energy");
        var cepe = PlynlingRpg.MaxEnergy(Make(12, PlynlingSpecies.Cepe), traits);
        var russule = PlynlingRpg.MaxEnergy(Make(12, PlynlingSpecies.Russule), traits);
        Expect(cepe == russule + 5, "the Endurance lean adds 5 max energy");
    }

    private static void Odds()
    {
        Expect(PlynlingRpg.BodyChance(10, 10) == 50, "even is 50");
        Expect(PlynlingRpg.BodyChance(20, 10) == 66, $"20 vs 10 is {PlynlingRpg.BodyChance(20, 10)}");
        Expect(PlynlingRpg.BodyChance(0, 0) == 50, "0 vs 0 is 50");
        Expect(PlynlingRpg.BodyChance(100, 0) == 95 && PlynlingRpg.BodyChance(0, 100) == 5, "clamped 5..95");
        var edge = PlynlingRpg.BodyChance(60, 50);
        Expect(edge > 50 && edge < 60, $"ten ahead at a high level is an edge, not a win: {edge}");

        var p = Make(13);
        var stats = PlynlingStats.Compute(p, Array.Empty<TraitInfo>());
        var ctx = new EventContext(p, Array.Empty<TraitInfo>(), stats, PlynlingStage.Adult);
        foreach (var body in BodyStats)
        {
            var chance = PlynlingEventEngine.Chance(new EventChallenge(body, 8), ctx);
            Expect(chance == PlynlingRpg.BodyChance(ctx.Stat(body), 8), $"{body} test uses the soft curve");
        }
        var mind = PlynlingEventEngine.Chance(new EventChallenge(PlynlingStat.Courage, 3), ctx);
        Expect(mind == Math.Clamp(50 + 5 * (ctx.Stat(PlynlingStat.Courage) - 3), 5, 95), "mind stats keep 50 + 5 × diff");
    }

    private static void Drill()
    {
        var traits = Array.Empty<TraitInfo>();
        var a = Make(21);
        var b = Make(21);
        var ra = PlynlingRpg.Drill(a, traits, PlynlingStat.Force, Now);
        var rb = PlynlingRpg.Drill(b, traits, PlynlingStat.Force, Now);
        Expect(ra.Boosted == rb.Boosted, "the boost is hashed");
        var max = PlynlingRpg.MaxEnergy(a, traits);
        Expect(a.RpgXp == 10 && ra.XpGained == 10, "a drill gives 10 XP");
        Expect(Math.Abs(PlynlingRpg.EnergyAt(a, max, Now) - (max - 15)) < 1e-9 && Math.Abs(ra.EnergyLeft - (max - 15)) < 1e-9, "a drill costs 15");
        Expect(a.Drills == 1, "drills counted");
        Expect(a.GrowthForce == (ra.Boosted ? 1 : 0), "a boost is permanent growth of the drilled stat");

        var boosts = 0;
        var c = Make(22);
        for (var i = 0; i < 10_000; i++)
        {
            c.EnergyAsOf = default;                    // full every time: the rate is what is measured
            var before = c.GrowthSpeed;
            if (PlynlingRpg.Drill(c, traits, PlynlingStat.Speed, Now).Boosted) boosts++;
            Expect(c.GrowthSpeed - before == (c.GrowthSpeed > before ? 1 : 0), "at most +1 a drill");
        }
        Expect(c.RpgXp == PlynlingRpg.XpForLevel(PlynlingRpg.MaxLevel), "XP stops at the cap");

        // Levels must stay the main way a body stat grows: a year of daily drills at full energy (about
        // eight a day) gives fewer permanent +1s than the points a year of training alone earns.
        var year = Make(25);
        var drillsPerDay = PlynlingRpg.MaxEnergy(year, traits) / PlynlingRpg.DrillCost;
        for (var i = 0; i < drillsPerDay * 365; i++)
        {
            year.EnergyAsOf = default;
            PlynlingRpg.Drill(year, traits, PlynlingStat.Force, Now);
        }
        var yearPoints = PlynlingRpg.Level(year.RpgXp) - 1;
        Expect(year.GrowthForce * 2 < yearPoints, $"a year of drills: {year.GrowthForce} boosts vs {yearPoints} points");
        Console.WriteLine($"rpg boosts: {boosts} in 10 000 drills; a year of drills gives {year.GrowthForce} boosts and {yearPoints} points");

        // No boost once the level cap is reached: the stat would keep growing with nothing left to earn.
        var capped = Make(26);
        capped.RpgXp = PlynlingRpg.XpForLevel(PlynlingRpg.MaxLevel);
        var cappedBoosts = 0;
        for (var i = 0; i < 2_000; i++)
        {
            capped.EnergyAsOf = default;
            if (PlynlingRpg.Drill(capped, traits, PlynlingStat.Force, Now).Boosted) cappedBoosts++;
        }
        Expect(cappedBoosts == 0 && capped.GrowthForce == 0, $"boosts at the cap: {cappedBoosts}");

        var d = Make(23);
        d.RpgXp = PlynlingRpg.XpForLevel(2) - 5;
        var up = PlynlingRpg.Drill(d, traits, PlynlingStat.Agility, Now);
        Expect(up.LevelBefore == 1 && up.LevelAfter == 2, "a drill that crosses a level says so");
        var threw = false;
        try { PlynlingRpg.Drill(Make(24), traits, PlynlingStat.Learning, Now); } catch (ArgumentOutOfRangeException) { threw = true; }
        Expect(threw, "a mind stat cannot be drilled");
    }

    // The service against a real in-memory SQLite: the refusal order (nothing spent on a refusal), one
    // save per drill, and the point buttons (owner only, never past the level, a second click refused).
    private static async Task ServiceAsync()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        using (var c = new AppDbContext(opts)) c.Database.EnsureCreated();
        var services = new ServiceCollection().AddDbContext<AppDbContext>(o => o.UseSqlite(conn)).BuildServiceProvider();
        PlynlingService Service(AppDbContext c) => new(c, NullLogger<PlynlingService>.Instance, new PlynlingCooldowns(),
            new BadgeAwarder(services, new Discord.WebSocket.DiscordSocketClient(), NullLogger<BadgeAwarder>.Instance));

        const ulong G = 1, Owner = 2, Other = 3;
        int id;
        using (var c = new AppDbContext(opts))
        {
            var row = PlynlingLife.Create(G, Owner, "Test", PlynlingSpecies.Cepe, PlynlingGender.Female, Now.AddDays(-10));
            row.Hunger = row.Happiness = row.Hygiene = 1;
            row.NeedsAsOf = Now;
            row.LastMorningDay = AppTime.DayKey(Now);
            c.Plynlings.Add(row);
            c.SaveChanges();
            id = row.Id;
        }
        Plynling Read() { using var c = new AppDbContext(opts); return c.Plynlings.AsNoTracking().Single(x => x.Id == id); }

        using (var c = new AppDbContext(opts))
        {
            var r = await Service(c).TrainAsync(G, Owner, PlynlingStat.Force, Now);
            Expect(r.Outcome == TrainOutcome.Done && r.Drill is { XpGained: 10 }, $"an ado trains: {r.Outcome}");
            Expect(r.MaxEnergy > 0 && Math.Abs(r.Drill!.EnergyLeft - (r.MaxEnergy - 15)) < 1e-9, "the drill spent 15");
        }
        var saved = Read();
        Expect(saved.RpgXp == 10 && saved.Drills == 1 && saved.EnergyAsOf == Now, "the drill was saved");

        using (var c = new AppDbContext(opts))
        {
            var r = await Service(c).TrainAsync(G, Owner, PlynlingStat.Courage, Now);
            Expect(r.Outcome == TrainOutcome.NotABodyStat, $"a mind stat is refused: {r.Outcome}");
            var other = await Service(c).TrainAsync(G, Other, PlynlingStat.Force, Now);
            Expect(other.Outcome == TrainOutcome.NoPlynling, "no Plynling, no drill");
            var asleep = await Service(c).TrainAsync(G, Owner, PlynlingStat.Force, AppTime.AtWallClock(AppTime.ToZoned(Now).Date.AddHours(2)));
            Expect(asleep.Outcome is TrainOutcome.Asleep, $"asleep at 02:00: {asleep.Outcome}");
        }

        // Drain it: every refusal after that leaves the energy as it was, and says when it will do.
        using (var c = new AppDbContext(opts))
        {
            var row = c.Plynlings.Single(x => x.Id == id);
            row.Energy = 5;
            row.EnergyAsOf = Now;
            c.SaveChanges();
        }
        using (var c = new AppDbContext(opts))
        {
            var r = await Service(c).TrainAsync(G, Owner, PlynlingStat.Agility, Now);
            Expect(r.Outcome == TrainOutcome.NoEnergy && r.EnoughAt > Now, $"too tired: {r.Outcome} {r.EnoughAt}");
        }
        Expect(Read().Energy == 5 && Read().RpgXp == 10, "a refusal spends nothing");

        using (var c = new AppDbContext(opts))
        {
            var row = c.Plynlings.Single(x => x.Id == id);
            row.FrozenAt = Now;
            row.Energy = 100;
            c.SaveChanges();
        }
        using (var c = new AppDbContext(opts))
        {
            var r = await Service(c).TrainAsync(G, Owner, PlynlingStat.Agility, Now);
            Expect(r.Outcome == TrainOutcome.Frozen, $"frozen: {r.Outcome}");
        }
        Expect(Read().RpgXp == 10, "a frozen one does not train");

        // Points: none at level 1; one at level 2, placed once; never by someone else.
        using (var c = new AppDbContext(opts))
        {
            var row = c.Plynlings.Single(x => x.Id == id);
            row.FrozenAt = null;
            c.SaveChanges();
        }
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).SpendPointAsync(id, Owner, PlynlingStat.Speed, Now)).Outcome == PointOutcome.NoPoint, "no point at level 1");
        using (var c = new AppDbContext(opts))
        {
            var row = c.Plynlings.Single(x => x.Id == id);
            row.RpgXp = PlynlingRpg.XpForLevel(2);
            c.SaveChanges();
        }
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).SpendPointAsync(id, Other, PlynlingStat.Speed, Now)).Outcome == PointOutcome.NotOwner, "someone else cannot place it");
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).SpendPointAsync(id, Owner, PlynlingStat.Courage, Now)).Outcome == PointOutcome.NoPoint, "not into a mind stat");
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).SpendPointAsync(id, Owner, PlynlingStat.Speed, Now)).Outcome == PointOutcome.Done, "the owner places it");
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).SpendPointAsync(id, Owner, PlynlingStat.Speed, Now)).Outcome == PointOutcome.NoPoint, "a second click finds none left");
        Expect(Read().PointsSpeed == 1, "one point placed, once");
    }

    // Append-only: the five mind stats keep their numbers, the body stats follow.
    private static void BodyStatList()
    {
        Expect(PlynlingStats.All.Count == 10, $"10 stats, got {PlynlingStats.All.Count}");
        Expect((int)PlynlingStat.Courage == 4 && (int)PlynlingStat.Force == 5 && (int)PlynlingStat.Speed == 9, "stat numbers moved");
        Expect(PlynlingStats.Body.SequenceEqual(BodyStats), "Body is the five body stats in order");
        Expect(PlynlingStats.Mind.Count == 5 && PlynlingStats.Mind.All(s => !PlynlingStats.IsBody(s)), "Mind is the five mind stats");
        string[] names = { "Force", "Agilité", "Endurance", "Intelligence", "Vitesse" };
        string[] emojis = { "💪", "🤸", "❤️", "🧠", "💨" };
        for (var i = 0; i < BodyStats.Length; i++)
        {
            Expect(PlynlingStats.Name(BodyStats[i]) == names[i], $"name of {BodyStats[i]}");
            Expect(PlynlingStats.Emoji(BodyStats[i]) == emojis[i], $"emoji of {BodyStats[i]}");
        }
        foreach (var passion in Enum.GetValues<PlynlingPassion>())
            Expect(!PlynlingStats.IsBody(PlynlingStats.PassionStat(passion)), $"passion {passion} gives a body stat");
    }

    private static void SpeciesLeans()
    {
        var expected = new Dictionary<PlynlingSpecies, PlynlingStat>
        {
            [PlynlingSpecies.Amanite] = PlynlingStat.Intelligence,
            [PlynlingSpecies.Cepe] = PlynlingStat.Endurance,
            [PlynlingSpecies.Rose] = PlynlingStat.Agility,
            [PlynlingSpecies.Russule] = PlynlingStat.Force,
            [PlynlingSpecies.Mystique] = PlynlingStat.Intelligence,
            [PlynlingSpecies.Dore] = PlynlingStat.Speed,
            [PlynlingSpecies.Coprin] = PlynlingStat.Speed,
        };
        foreach (var species in Enum.GetValues<PlynlingSpecies>())
        {
            var sum = BodyStats.Sum(s => PlynlingStats.SpeciesLean(species, s));
            var mushroom = expected.TryGetValue(species, out var lean);
            Expect(sum == (mushroom ? 1 : 0), $"{species} leans {sum}");
            if (mushroom) Expect(PlynlingStats.SpeciesLean(species, lean) == 1, $"{species} leans on {lean}");
            foreach (var mind in PlynlingStats.Mind)
                Expect(PlynlingStats.SpeciesLean(species, mind) == 0, $"{species} leans on a mind stat");
        }
    }

    // The breakdown carries the species lean and the points, and the total adds them up.
    private static void Breakdown()
    {
        var p = Make(7, PlynlingSpecies.Russule);
        p.PointsForce = 3;
        p.GrowthForce = 1;
        var traits = new[] { PlynlingTraits.ByKey("brave")!, PlynlingTraits.ByKey("rowdy")! };
        var force = PlynlingStats.Compute(p, traits).Single(l => l.Stat == PlynlingStat.Force);
        Expect(force.Points == 3 && force.Growth == 1 && force.Species == 1, $"force line {force}");
        Expect(force.Traits == 2, $"brave + rowdy give Force 2, got {force.Traits}");
        Expect(force.Total == force.Base + force.Species + force.Traits + 1 + 3 + force.State, $"force total {force}");
        Expect(PlynlingStats.Innate(p, traits, PlynlingStat.Force) == force.Base + 1 + 2, "innate = die + species + traits");
        Expect(PlynlingStats.Points(p, PlynlingStat.Force) == 3, "points read back");
        PlynlingStats.AddPoint(p, PlynlingStat.Speed);
        Expect(p.PointsSpeed == 1, "AddPoint on Speed");
        var threw = false;
        try { PlynlingStats.AddPoint(p, PlynlingStat.Courage); } catch (ArgumentOutOfRangeException) { threw = true; }
        Expect(threw, "AddPoint on a mind stat throws");
        var courage = PlynlingStats.Compute(p, traits).Single(l => l.Stat == PlynlingStat.Courage);
        Expect(courage.Species == 0 && courage.Points == 0, "mind stats have no species lean nor points");
    }

    // Adding body stats to traits must not move the adulte trait draw: the weight reads mind stats only.
    private static void AdultDrawUnchanged()
    {
        var ado = new AdoYears(
            new Dictionary<AiAxis, int> { [AiAxis.Boldness] = 120, [AiAxis.Compassion] = -40, [AiAxis.Energy] = 60 },
            new Dictionary<PlynlingStat, int> { [PlynlingStat.Diplomacy] = 2, [PlynlingStat.Courage] = 1, [PlynlingStat.Learning] = 3 },
            6);
        var withBody = 0;
        foreach (var trait in PlynlingTraits.All.Where(t => t.Kind == TraitKind.Personality))
        {
            if (trait.Stats.Keys.Any(PlynlingStats.IsBody)) withBody++;
            var mindOnly = trait with { Stats = trait.Stats.Where(kv => !PlynlingStats.IsBody(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value) };
            var a = PlynlingEventEngine.AdultTraitWeight(trait, ado);
            var b = PlynlingEventEngine.AdultTraitWeight(mindOnly, ado);
            Expect(Math.Abs(a - b) < 1e-12, $"adulte weight of {trait.Key} moved: {a} vs {b}");
        }
        Expect(withBody >= 8, $"personality traits carrying a body stat: {withBody}");
    }

    private static void Practice()
    {
        foreach (var stat in BodyStats)
        {
            var m = PlynlingModifiers.Practice(stat);
            Expect(m.Stats.Count == 1 && m.Stats.GetValueOrDefault(stat) == 1, $"practice of {stat}");
            Expect(m.Key == "practice_" + stat.ToString().ToLowerInvariant(), $"practice key {m.Key}");
        }
    }
}
