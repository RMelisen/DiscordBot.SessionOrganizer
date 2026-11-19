using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Helpers;

// 56. Visit scenes (docs/agents/plynling-storylines.md, Visit files; docs/agents/plynling.md, Visits):
// the parser, the validator and the catalog, the odds, the cards, the runner.
public static class VisitChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("visit: " + what);
    }

    public static string FixtureFolder => Path.Combine(StoryChecks.RepoRoot(), "tests", "ProjectSYNCS.Checks", "Visits");

    public static async Task Run(Action<string> fail, string[] args)
    {
        _fail = fail;
        Parser();
        Validator();
        Library();
        Odds();
        try { await SaveAsync(); }
        catch (Exception ex) { Expect(false, "the visit save threw: " + ex); }
        Cards(VisitLibrary.Load(FixtureFolder, requireCoverage: false).Scenes, "fixtures");
        Shipped();
        Draw();
        try { await RunnerAsync(); }
        catch (Exception ex) { Expect(false, "the visit runner threw: " + ex); }
        try { await EndingAsync(); }
        catch (Exception ex) { Expect(false, "showing a visit's ending threw: " + ex); }
    }

    // 56j. The ending comes after the save: an edit Discord refuses must not leave the knock live
    // with nobody told. The closed card takes its place; if that fails too, nothing escapes.
    private static async Task EndingAsync()
    {
        var ending = ProjectSYNCS.Commands.PlynlingVisitCards.BuildClosed("ending");
        var shown = new List<Discord.MessageComponent>();
        Task Ok(Discord.MessageComponent c) { shown.Add(c); return Task.CompletedTask; }
        Task RefuseAll(Discord.MessageComponent c) => throw new InvalidOperationException("refused");
        var errors = new List<Exception>();

        var ok = await ProjectSYNCS.Services.PlynlingVisitRunner.ShowEndingAsync(Ok, ending, "outcome", errors.Add);
        Expect(ok && shown.Count == 1 && ReferenceEquals(shown[0], ending) && errors.Count == 0, "ending: shown as built");

        shown.Clear();
        var count = 0;
        Task RefuseOnce(Discord.MessageComponent c) => count++ == 0 ? throw new InvalidOperationException("refused") : Ok(c);
        ok = await ProjectSYNCS.Services.PlynlingVisitRunner.ShowEndingAsync(RefuseOnce, ending, "outcome", errors.Add);
        Expect(!ok && shown.Count == 1 && !ReferenceEquals(shown[0], ending) && errors.Count == 1,
            "ending refused: the closed card with the outcome takes its place, the error logged");

        errors.Clear();
        ok = await ProjectSYNCS.Services.PlynlingVisitRunner.ShowEndingAsync(RefuseAll, ending, "outcome", errors.Add);
        Expect(!ok && errors.Count == 2, "ending and closed card both refused: both logged, nothing thrown");
    }

    // 56h. The draw: never a scene the pair could not have, at any bond, hour and pair of stages.
    private static void Draw()
    {
        var picker = new ProjectSYNCS.Services.ResponsePicker();
        var bad = 0;
        var day = new DateTimeOffset(2026, 11, 19, 0, 30, 0, TimeSpan.Zero);   // 01:30 Paris, then every hour
        foreach (var bond in Enum.GetValues<PlynlingBond>())
            for (var h = 0; h < 24; h++)
            {
                var at = day + TimeSpan.FromHours(h);
                var hour = AppTime.ToZoned(at).Hour;
                foreach (var v in Enum.GetValues<PlynlingStage>())
                    foreach (var host in Enum.GetValues<PlynlingStage>())
                    {
                        var s = ProjectSYNCS.Services.PlynlingVisitRunner.DrawScene(bond, at, v, host, picker);
                        if (!s.Fits(bond, hour, v, host)) bad++;
                    }
            }
        Expect(bad == 0, $"{bad} drawn scenes did not fit their pair");
        // The picker steers away from the last scenes drawn: two draws in a row differ.
        var first = ProjectSYNCS.Services.PlynlingVisitRunner.DrawScene(PlynlingBond.Friends, day + TimeSpan.FromHours(12), PlynlingStage.Adult, PlynlingStage.Adult, picker);
        var second = ProjectSYNCS.Services.PlynlingVisitRunner.DrawScene(PlynlingBond.Friends, day + TimeSpan.FromHours(12), PlynlingStage.Adult, PlynlingStage.Adult, picker);
        Expect(first.Key != second.Key, "the same scene twice in a row");
    }

    // 56i. The runner: one visit per pair and day, the claim released whenever nothing happened.
    private static async Task RunnerAsync()
    {
        var scene = VisitCatalog.ByKey("rain_shelter")!;
        var confession = VisitCatalog.Confession;
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.Friends, 30);
            var cooldowns = new ProjectSYNCS.Services.PlynlingCooldowns();
            {
                await using var c = db.New();
                var s = db.Service(c, cooldowns);
                var runner = new ProjectSYNCS.Services.PlynlingVisitRunner(s, cooldowns);
                var (o, refusal, _) = await runner.RunAsync((await s.GetByIdAsync(boy, T0))!, (await s.GetByIdAsync(girl, T0))!,
                    scene, scene.Options[0], scene.Reactions[0], T0);
                Expect(o is { TooLate: false } && refusal is null, $"a visit runs: {refusal}");
            }
            {
                await using var c = db.New();
                var s = db.Service(c, cooldowns);
                var runner = new ProjectSYNCS.Services.PlynlingVisitRunner(s, cooldowns);
                var (o, refusal, _) = await runner.RunAsync((await s.GetByIdAsync(boy, T0))!, (await s.GetByIdAsync(girl, T0))!,
                    scene, scene.Options[1], scene.Reactions[1], T0);
                Expect(o is null && refusal is not null && refusal.Contains("déjà vus"), $"a second reaction the same day is « déjà vus »: {refusal}");
            }
            Expect((await RelationAsync(db, boy, girl)).Meetings == 1, "exactly one visit saved");
        }
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.Friends, 30);
            var cooldowns = new ProjectSYNCS.Services.PlynlingCooldowns();
            await using var c = db.New();
            var s = db.Service(c, cooldowns);
            var visitor = (await s.GetByIdAsync(boy, T0))!;
            var host = (await s.GetByIdAsync(girl, T0))!;
            // Frozen between the pick and the reaction.
            await using (var w = db.New())
            {
                var row = w.Plynlings.Single(p => p.Id == boy);
                row.FrozenAt = T0;
                await w.SaveChangesAsync();
            }
            var runner = new ProjectSYNCS.Services.PlynlingVisitRunner(s, cooldowns);
            var (o, refusal, _) = await runner.RunAsync(visitor, host, scene, scene.Options[0], scene.Reactions[0], T0);
            Expect(o is null && refusal is not null, "a visitor frozen since the pick: refused");
            Expect(!cooldowns.VisitedToday(visitor.OwnerId, host.OwnerId, AppTime.DayKey(T0)), "…and the claim released");
        }
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.Friends, 30);   // friends: may not confess
            var cooldowns = new ProjectSYNCS.Services.PlynlingCooldowns();
            await using var c = db.New();
            var s = db.Service(c, cooldowns);
            var runner = new ProjectSYNCS.Services.PlynlingVisitRunner(s, cooldowns);
            var visitor = (await s.GetByIdAsync(boy, T0))!;
            var host = (await s.GetByIdAsync(girl, T0))!;
            var (o, refusal, _) = await runner.RunAsync(visitor, host, confession, confession.Options[0], confession.Reactions[0], T0);
            Expect(o is { TooLate: true } && refusal == PlynlingText.VisitTooLate, $"a confession no longer possible is too late: {refusal}");
            Expect(!cooldowns.VisitedToday(visitor.OwnerId, host.OwnerId, AppTime.DayKey(T0)), "…and the claim released");
            Expect((await RelationAsync(db, boy, girl)).Meetings == 0, "…and nothing saved");
        }
    }

    public static string ShippedFolder => Path.Combine(StoryChecks.RepoRoot(), "ProjectSYNCS", "Stories", "Visits");

    // 56g. The shipped scenes: they load with the draw covered, the catalog serves them, every card fits.
    private static void Shipped()
    {
        var load = VisitLibrary.Load(ShippedFolder, requireCoverage: true);
        Expect(load.Errors.Count == 0, "the shipped scenes load clean: " + string.Join("; ", load.Errors.Take(10)));
        Expect(load.Scenes.Count(s => !s.Confession) == 20 && load.Scenes.Count(s => s.Confession) == 1,
            $"20 scenes and one confession ship, found {load.Scenes.Count}");
        if (load.Errors.Count > 0) return;
        Expect(VisitCatalog.Scenes.Count == 20 && VisitCatalog.Confession.Key == "confess" && VisitCatalog.ByKey("ricochets") is not null,
            "the catalog serves the shipped folder");
        Cards(load.Scenes, "shipped");
    }

    // The worst names: 32 characters, every one escaped by Sanitize.
    private static readonly string LongName = new('*', 32);

    private static ProjectSYNCS.Models.Plynling Plyn(int id, ulong owner, ProjectSYNCS.Models.PlynlingGender gender) => new()
    {
        Id = id, OwnerId = owner, GuildId = Guild, Name = LongName, Gender = gender, Species = ProjectSYNCS.Models.PlynlingSpecies.Amanite,
        AdoptedAt = T0 - TimeSpan.FromDays(30), LiveSince = T0 - TimeSpan.FromDays(30), NeedsAsOf = T0, Hunger = 1, Happiness = 1, Hygiene = 1,
    };

    private static void MeasureCard(string what, Discord.MessageComponent card)
    {
        var (text, count, ids) = (0, 0, new List<string>());
        foreach (var c in card.Components)
        {
            var m = EconomySinkChecks.Measure(c);
            (text, count) = (text + m.Text, count + m.Count);
            ids.AddRange(m.Ids);
        }
        Expect(count <= 40 && text <= 4000, $"{what}: {count} components, {text} characters");
        Expect(ids.Count == ids.Distinct().Count(), $"{what}: a custom-id twice");
        Expect(ids.All(i => i.Length <= 100), $"{what}: a custom-id over 100 characters");
        foreach (var label in Buttons(card).Select(b => b.Label))
            Expect(label.Length is > 0 and <= 80, $"{what}: button label of {label.Length} characters");
        foreach (var t in Texts(card))
        {
            Expect(!t.Contains('{') && !t.Contains('}'), $"{what}: a template left in « {t} »");
            Expect(t.Split('\n').All(l => !l.StartsWith('[')), $"{what}: a face tag left in « {t} »");
        }
    }

    private static IEnumerable<Discord.ButtonComponent> Buttons(Discord.MessageComponent card) =>
        card.Components.SelectMany(Flatten).OfType<Discord.ButtonComponent>();

    private static IEnumerable<string> Texts(Discord.MessageComponent card) =>
        card.Components.SelectMany(Flatten).OfType<Discord.TextDisplayComponent>().Select(t => t.Content);

    private static IEnumerable<Discord.IMessageComponent> Flatten(Discord.IMessageComponent c) => c switch
    {
        Discord.ContainerComponent x => x.Components.SelectMany(Flatten).Prepend(c),
        Discord.SectionComponent x => x.Components.SelectMany(Flatten).Concat(x.Accessory is null ? [] : Flatten(x.Accessory)).Prepend(c),
        Discord.ActionRowComponent x => x.Components.SelectMany(Flatten).Prepend(c),
        _ => [c],
    };

    // 56f. The three cards, for every scene × option × reaction × half × gender pair, at the worst names.
    private static void Cards(IReadOnlyList<VisitScene> scenes, string what)
    {
        Expect(scenes.Count > 0, what + ": no scenes to measure");
        var expires = T0 + TimeSpan.FromHours(2);
        var genders = new[] { ProjectSYNCS.Models.PlynlingGender.Male, ProjectSYNCS.Models.PlynlingGender.Female };
        var longest = ProjectSYNCS.Commands.PlynlingVisitCards.ReactionId(int.MaxValue, ulong.MaxValue, long.MaxValue / 1_000_000_000,
            new string('s', 20), new string('o', 12), new string('r', 12));
        Expect(longest.Length <= 100, $"the longest reaction id is {longest.Length} characters");
        Expect(ProjectSYNCS.Commands.PlynlingVisitCards.ChoiceId(int.MaxValue, ulong.MaxValue, 9_999_999_999, new string('s', 20), new string('o', 12)).Length <= 100
               && ProjectSYNCS.Commands.PlynlingVisitCards.ConfessId(int.MaxValue, ulong.MaxValue, 9_999_999_999).Length <= 100, "choice and confession ids within 100");
        var ids = new[]
        {
            ProjectSYNCS.Commands.PlynlingVisitCards.ChoiceId(1, 2, 3, "s", "o"), ProjectSYNCS.Commands.PlynlingVisitCards.ConfessId(1, 2, 3),
            ProjectSYNCS.Commands.PlynlingVisitCards.ReactionId(1, 2, 3, "s", "o", "r"),
        };
        Expect(ids.Select(i => i.Split(':')[0]).Distinct().Count() == 3, "each step its own verb");

        foreach (var s in scenes)
            foreach (var gv in genders)
                foreach (var gh in genders)
                {
                    var visitor = Plyn(int.MaxValue, ulong.MaxValue - 1, gv);
                    var host = Plyn(int.MaxValue - 1, ulong.MaxValue, gh);
                    var pair = $"{what} {s.Key} {gv}/{gh}";
                    foreach (var offer in new[] { false, true })
                        MeasureCard($"{pair} choice (confession {offer})",
                            ProjectSYNCS.Commands.PlynlingVisitCards.BuildChoice(s, visitor, host, ulong.MaxValue, expires, offer, T0));
                    var choice = ProjectSYNCS.Commands.PlynlingVisitCards.BuildChoice(s, visitor, host, ulong.MaxValue, expires, true, T0);
                    Expect(Buttons(choice).Count() == s.Options.Count + 1 && Buttons(choice).Last().CustomId.StartsWith("pvx:"), $"{pair}: the confession button comes last");
                    foreach (var o in s.Options)
                    {
                        var knock = ProjectSYNCS.Commands.PlynlingVisitCards.BuildKnock(s, o, visitor, host, ulong.MaxValue, expires, T0);
                        MeasureCard($"{pair} knock {o.Key}", knock);
                        Expect(Buttons(knock).Count() == s.Reactions.Count, $"{pair} knock {o.Key}: one button per reaction");
                        foreach (var r in s.Reactions)
                            foreach (var good in new[] { true, false })
                            {
                                var confession = !s.Confession ? Confession.None : good ? Confession.Accepted : Confession.Refused;
                                var after = confession == Confession.Accepted ? PlynlingBond.Lovers : s.Confession ? PlynlingBond.BestFriends
                                    : s.Bonds.First();
                                var outcome = new ProjectSYNCS.Services.VisitOutcome(visitor, host, [], [], good || s.Confession,
                                    s.Confession ? PlynlingBond.BestFriends : s.Bonds.First(), after, confession, 0.25);
                                var ending = ProjectSYNCS.Commands.PlynlingVisitCards.BuildEnding(s, o, r, outcome, T0);
                                MeasureCard($"{pair} ending {o.Key}/{r.Key}/{(good ? "good" : "bad")}", ending);
                                Expect(!Buttons(ending).Any(), $"{pair} ending: no buttons");
                            }
                    }
                }
        MeasureCard("closed", ProjectSYNCS.Commands.PlynlingVisitCards.BuildClosed("Trop tard."));
    }

    // A Random whose every draw is fixed: NextDouble returns the value, Next(lo, hi) its low end.
    private sealed class Fixed(double value) : Random
    {
        public override double NextDouble() => value;
        protected override double Sample() => value;
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next(int maxValue) => 0;
    }

    private static Dictionary<AiAxis, int> Axes(params (AiAxis Axis, int Value)[] values) => values.ToDictionary(v => v.Axis, v => v.Value);

    // 56d. The odds: a lean judged by the other's axes, capped; the chances it shifts; the mascot's reaction.
    private static void Odds()
    {
        var bold = Axes((AiAxis.Boldness, 200));
        var craven = Axes((AiAxis.Boldness, -200));
        var lean = Axes((AiAxis.Boldness, 2));
        Expect(VisitOdds.Shift(lean, bold) > 0, "a bold lean pleases a bold judge");
        Expect(VisitOdds.Shift(lean, craven) < 0, "a bold lean displeases a craven judge");
        Expect(VisitOdds.Shift(lean, new Dictionary<AiAxis, int>()) == 0, "a judge with no axes is neutral");
        var huge = VisitOdds.Shift(Axes((AiAxis.Boldness, 3), (AiAxis.Zeal, 3)), Axes((AiAxis.Boldness, 1000), (AiAxis.Zeal, 1000)));
        Expect(huge > 0 && huge <= VisitOdds.MaxShiftPerChoice, $"a huge match never passes the cap: {huge}");
        Expect(Math.Abs(VisitOdds.Shift(Axes((AiAxis.Boldness, -3)), Axes((AiAxis.Boldness, 1000)))) <= VisitOdds.MaxShiftPerChoice, "nor a huge mismatch");

        Expect(VisitOdds.GoodChance(0, PlynlingBond.Acquaintances, 0, 0) == PlynlingBonds.GoodSceneChance(0, PlynlingBond.Acquaintances, 0), "no shift, today's chance");
        Expect(Math.Abs(VisitOdds.GoodChance(0, PlynlingBond.Acquaintances, 0, 0.075) - PlynlingBonds.GoodSceneChance(0, PlynlingBond.Acquaintances, 0) - 0.075) < 1e-9, "a shift adds to the chance");
        Expect(VisitOdds.GoodChance(20, PlynlingBond.Lovers, 0, 0.15) == 0.95 && VisitOdds.GoodChance(-20, PlynlingBond.Enemies, 100, -0.15) == 0.05, "the chance stays within 5–95 %");
        Expect(Math.Abs(VisitOdds.AcceptChance(0, 0) - 0.5) < 1e-9 && Math.Abs(VisitOdds.AcceptChance(20, 0) - 0.85) < 1e-9
               && Math.Abs(VisitOdds.AcceptChance(-20, 0) - 0.15) < 1e-9, "acceptance: 50 % average, 85 % / 15 % at the extremes");
        Expect(Math.Abs(VisitOdds.AcceptChance(0, 0.1) - 0.6) < 1e-9 && VisitOdds.AcceptChance(20, 0.15) == 0.95, "acceptance shifts, within 5–95 %");

        var now = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
        Expect(PlynlingBonds.ConfessionCoolingDown(now - TimeSpan.FromHours(71), now), "71 h after a refusal: still cooling down");
        Expect(!PlynlingBonds.ConfessionCoolingDown(now - TimeSpan.FromHours(73), now), "73 h after: open again");
        Expect(!PlynlingBonds.ConfessionCoolingDown(null, now), "never refused: open");

        // « 💌 » on a quarter of a pair's days: hashed, symmetric, and spread over days and pairs.
        var offered = 0;
        var cells = 0;
        var day0 = AppTime.DayKey(now);
        for (var pair = 1; pair <= 200; pair++)
            for (var d = 0; d < 50; d++, cells++)
            {
                var day = AppTime.DayKey(now + TimeSpan.FromDays(d));
                if (PlynlingBonds.ConfessionOffered(pair, pair + 1000, day)) offered++;
                if (PlynlingBonds.ConfessionOffered(pair, pair + 1000, day) != PlynlingBonds.ConfessionOffered(pair + 1000, pair, day))
                    _fail("visit: the confession offer depends on who visits");
            }
        Expect(Math.Abs((double)offered / cells - PlynlingBonds.ConfessionOfferChance) < 0.02, $"the offer comes on {offered * 100.0 / cells:0.0} % of days");
        Expect(Enumerable.Range(1, 200).Count(p => PlynlingBonds.ConfessionOffered(p, p + 1000, day0)) is > 25 and < 75, "on one day, some pairs are offered and others not");

        var scene = One(Valid);    // reactions: stay [energy -1], sit [energy -2, honor +1]
        Expect(VisitOdds.MascotReaction(scene, Axes((AiAxis.Energy, -50))).Key == "sit", "the mascot picks the reaction leaning her way");
        Expect(VisitOdds.MascotReaction(scene, Axes((AiAxis.Energy, 50))).Key == "stay", "…the least against her when none leans her way");
        Expect(VisitOdds.MascotReaction(scene, new Dictionary<AiAxis, int>()).Key == "stay", "a tie goes to file order");
    }

    // A fresh in-memory database per scenario, as BribeChecks does; the connection keeps it alive.
    private sealed class Db : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _conn = new("DataSource=:memory:");
        private readonly Microsoft.EntityFrameworkCore.DbContextOptions<ProjectSYNCS.Data.AppDbContext> _opts;

        public Db()
        {
            _conn.Open();
            _opts = Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions
                .UseSqlite(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ProjectSYNCS.Data.AppDbContext>(), _conn).Options;
            using var c = New();
            c.Database.EnsureCreated();
        }

        public ProjectSYNCS.Data.AppDbContext New() => new(_opts);

        public ProjectSYNCS.Services.PlynlingService Service(ProjectSYNCS.Data.AppDbContext c, ProjectSYNCS.Services.PlynlingCooldowns? cooldowns = null) =>
            new(c, Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectSYNCS.Services.PlynlingService>.Instance,
                cooldowns ?? new ProjectSYNCS.Services.PlynlingCooldowns(),
                new ProjectSYNCS.Services.BadgeAwarder(new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider(),
                    new Discord.WebSocket.DiscordSocketClient(),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectSYNCS.Services.BadgeAwarder>.Instance));

        public void Dispose() => _conn.Dispose();
    }

    private const ulong Guild = 1000;
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    // A boy and a girl, adopted; their relation set as given (lower id first).
    private static async Task<(int Boy, int Girl)> PairAsync(Db db, PlynlingBond bond, int affinity)
    {
        await using var c = db.New();
        var s = db.Service(c);
        var boy = (await s.AdoptAsync(Guild, 42, "Pix", ProjectSYNCS.Models.PlynlingSpecies.Amanite, T0 - TimeSpan.FromHours(1), ProjectSYNCS.Models.PlynlingGender.Male)).Plynling!;
        var girl = (await s.AdoptAsync(Guild, 43, "Mochi", ProjectSYNCS.Models.PlynlingSpecies.Amanite, T0 - TimeSpan.FromHours(1), ProjectSYNCS.Models.PlynlingGender.Female)).Plynling!;
        c.PlynlingRelations.Add(new ProjectSYNCS.Models.PlynlingRelation
            { PlynlingAId = Math.Min(boy.Id, girl.Id), PlynlingBId = Math.Max(boy.Id, girl.Id), Bond = bond, Affinity = affinity, Since = T0 - TimeSpan.FromDays(10) });
        await c.SaveChangesAsync();
        return (boy.Id, girl.Id);
    }

    private static async Task<ProjectSYNCS.Models.PlynlingRelation> RelationAsync(Db db, int a, int b)
    {
        await using var c = db.New();
        return c.PlynlingRelations.Single(r => r.PlynlingAId == Math.Min(a, b) && r.PlynlingBId == Math.Max(a, b));
    }

    // 56e. The visit's save with the players' play: confessions only by choice, the refusal cooldown, « trop tard ».
    private static async Task SaveAsync()
    {
        var play = new ProjectSYNCS.Services.VisitPlay(0, Confess: true);
        var noFinds = new Fixed(0.99);
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.BestFriends, 85);
            await using var c = db.New();
            var s = db.Service(c);
            var boyP = await s.GetByIdAsync(boy, T0);
            var girlP = await s.GetByIdAsync(girl, T0);
            Expect(await s.ConfessionOpenAsync(boyP!, girlP!, T0) == PlynlingBonds.ConfessionOffered(boy, girl, AppTime.DayKey(T0)),
                "an eligible boy/girl pair is offered « 💌 » on its offer days");            var o = await s.VisitAsync(boy, girl, T0, play, new Fixed(0.0), noFinds);
            Expect(o is { TooLate: false, Confession: Confession.Accepted, After: PlynlingBond.Lovers, GoodScene: true }, $"a forced acceptance makes a couple: {o}");
        }
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.BestFriends, 85);
            {
                await using var c = db.New();
                var o = await db.Service(c).VisitAsync(boy, girl, T0, play, new Fixed(0.99), noFinds);
                Expect(o is { Confession: Confession.Refused, GoodScene: true }, $"a forced refusal: {o}");
            }
            var r = await RelationAsync(db, boy, girl);
            Expect(r.LastRefusalAt == T0, "the refusal is dated");
            Expect(r.Affinity == 85 + 12 - PlynlingBonds.HeartbreakLoss, $"affinity: +12 for the scene, −25 for the refusal, got {r.Affinity}");
            await using var c2 = db.New();
            var s2 = db.Service(c2);
            var next = T0 + TimeSpan.FromHours(11.5);   // 00:30 Paris: the next Paris day, before the 05:00 morning roll
            Expect(!await s2.ConfessionOpenAsync((await s2.GetByIdAsync(girl, next))!, (await s2.GetByIdAsync(boy, next))!, next), "the other way round, still cooling down");
            var late = await s2.VisitAsync(girl, boy, next, play, new Fixed(0.0), noFinds);
            Expect(late is { TooLate: true }, $"a confession right after a refusal is too late: {late}");
            Expect((await RelationAsync(db, boy, girl)).LastVisitDay == AppTime.DayKey(T0), "too late saves nothing: the visit day is unchanged");
        }
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.BestFriends, 85);
            await using var c = db.New();
            var o = await db.Service(c).VisitAsync(boy, girl, T0, new ProjectSYNCS.Services.VisitPlay(0.15, Confess: false), new Fixed(0.0), noFinds);
            Expect(o is { Confession: Confession.None, GoodScene: true, TooLate: false } && o.After != PlynlingBond.Lovers,
                $"an ordinary play never confesses, even eligible and lucky: {o}");
        }
        using (var db = new Db())
        {
            var (boy, girl) = await PairAsync(db, PlynlingBond.Friends, 30);
            await using var c = db.New();
            var o = await db.Service(c).VisitAsync(boy, girl, T0, play, new Fixed(0.0), noFinds);
            Expect(o is { TooLate: true }, $"a confession by a pair that may not confess is too late: {o}");
        }
    }

    private static VisitScene One(string text)
    {
        var errors = new List<StoryError>();
        return VisitParser.Parse("t.visit", text, errors) ?? throw new InvalidOperationException(string.Join("; ", errors));
    }

    private static VisitScene FixtureConfession => One(File.ReadAllText(Path.Combine(FixtureFolder, "test_confession.visit")));

    // Fails unless validating the scene (beside the fixture confession) gives an error with the fragment.
    private static void ExpectInvalid(string what, string text, string fragment)
    {
        var errors = VisitValidator.Validate(new[] { One(text), FixtureConfession });
        Expect(errors.Any(e => e.Message.Contains(fragment)), $"{what}: expected an error with '{fragment}', got: {string.Join("; ", errors)}");
    }

    // 56b. The validator: one rule, one error; the coverage of the draw.
    private static void Validator()
    {
        var clean = VisitValidator.Validate(new[] { One(Valid), FixtureConfession });
        Expect(clean.Count == 0, "the inline visit validates: " + string.Join("; ", clean));
        ExpectInvalid("scene key over 20", Valid.Replace("# visit: v", "# visit: " + new string('k', 21)), "20");
        ExpectInvalid("choice key over 12", Valid.Replace("* go |", "* thirteen_char |"), "12");
        ExpectInvalid("choice keys share one namespace", Valid.Replace("> stay |", "> go |"), "twice");
        ExpectInvalid("one option", Valid.Replace("> stay", "* stay"), "reaction");
        ExpectInvalid("five reactions", Valid + string.Concat(Enumerable.Range(1, 4).Select(i =>
            $"\n> r{i} | R{i}  [lean energy +1]\n  ? good\n  Oui.\n  ? bad\n  Non.\n")), "2 to 4 reactions");
        ExpectInvalid("no lean", Valid.Replace("  [lean boldness +1]", ""), "1 or 2");
        ExpectInvalid("three leans", Valid.Replace("[lean boldness +1]", "[lean boldness +1] [lean zeal +1] [lean honor -1]"), "1 or 2");
        ExpectInvalid("a lean of 0", Valid.Replace("[lean boldness +1]", "[lean boldness 0]"), "-3 to +3");
        ExpectInvalid("a lean of 4", Valid.Replace("[lean boldness +1]", "[lean boldness +4]"), "-3 to +3");
        ExpectInvalid("an empty label", Valid.Replace("* go | Y aller", "* go |"), "label");
        ExpectInvalid("a label over 80 at worst", Valid.Replace("Y aller", "Aller voir {B} et " + new string('x', 50)), "80");
        ExpectInvalid("no title", Valid.Replace("title: T\n", ""), "title");
        ExpectInvalid("no place", Valid.Replace("place: 🌳 Ici\n", ""), "place");
        ExpectInvalid("no intro", Valid.Replace("Intro de {A} et {B}.\n\n", ""), "intro");
        ExpectInvalid("an empty half", Valid.Replace("  {B} part.\n", ""), "half");
        ExpectInvalid("a bad token", Valid.Replace("{A} y va.", "{C} y va."), "{C}");
        ExpectInvalid("{T} in a visit", Valid.Replace("{A} y va.", "{T} y va."), "{T}");
        ExpectInvalid("no bonds", Valid.Replace("bonds: friends rivals\n", ""), "bond");
        var ils = VisitValidator.Validate(new[] { One(Valid.Replace("{A} y va.", "{Ils} y vont, {ils} rient.")), FixtureConfession });
        Expect(ils.Count == 0, "{ils} / {Ils} are visit tokens: " + string.Join("; ", ils));
        var twoKeys = VisitValidator.Validate(new[] { One(Valid), One(Valid), FixtureConfession });
        Expect(twoKeys.Any(e => e.Message.Contains("already used")), "a scene key used in two files: " + string.Join("; ", twoKeys));
        Expect(VisitValidator.Validate(new[] { One(Valid) }).Any(e => e.Message.Contains("confession")), "no confession scene is an error");
        Expect(VisitValidator.Validate(new[] { One(Valid), FixtureConfession, One(ConfessionText) }).Any(e => e.Message.Contains("confession")),
            "two confession scenes are an error");

        var fixtures = Directory.EnumerateFiles(FixtureFolder, "*.visit").Order(StringComparer.Ordinal).Select(f => One(File.ReadAllText(f))).ToList();
        Expect(VisitValidator.Coverage(fixtures).Count > 0, "the fixtures alone leave cells of the draw empty");
        var everything = One(Valid.Replace("bonds: friends rivals", "bonds: acquaintances friends bestfriends lovers rivals enemies").Replace("hours: 8-20\n", ""));
        var full = VisitValidator.Coverage(new[] { everything });
        Expect(full.Count == 0, "an all-bonds, all-day, all-stages scene covers every cell: " + string.Join("; ", full.Take(3)));
        var noLovers = VisitValidator.Coverage(new[] { One(Valid.Replace("bonds: friends rivals", "bonds: acquaintances friends bestfriends rivals enemies").Replace("hours: 8-20\n", "")) });
        Expect(noLovers.Count > 0 && noLovers.All(e => e.Message.Contains("lovers")), "a missing bond is named: " + string.Join("; ", noLovers.Take(3)));
    }

    // 56c. The library and the catalog: a folder of .visit files; the shipped load stops the bot on any error.
    private static void Library()
    {
        var load = VisitLibrary.Load(FixtureFolder, requireCoverage: false);
        Expect(load.Errors.Count == 0 && load.Scenes.Count == 3, $"the fixtures load clean ({load.Scenes.Count} scenes): " + string.Join("; ", load.Errors));
        Expect(VisitLibrary.Load(FixtureFolder, requireCoverage: true).Errors.Count > 0, "the fixtures do not cover the draw");

        var probe = Path.Combine(StoryChecks.RepoRoot(), "tests", "ProjectSYNCS.Checks", "out", "probe-visits");
        if (Directory.Exists(probe)) Directory.Delete(probe, true);
        Directory.CreateDirectory(probe);
        // A story folder holding a .visit file: the story library leaves it to the visit library, silently.
        File.Copy(Path.Combine(StoryChecks.RepoRoot(), "ProjectSYNCS", "Stories", StoryLibrary.CastFile), Path.Combine(probe, StoryLibrary.CastFile));
        File.WriteAllText(Path.Combine(probe, "a.visit"), Valid);
        var stories = StoryLibrary.Load(probe, probe);
        Expect(!stories.Errors.Any(e => e.Message.Contains("not a .story")), "a .visit file is no stray story file: " + string.Join("; ", stories.Errors));
        // A stray file in a visit folder is reported, like a story folder's.
        File.WriteAllText(Path.Combine(probe, "b.visit.txt"), Valid);
        Expect(VisitLibrary.Load(probe, requireCoverage: false).Errors.Any(e => e.Message.Contains(".visit")), "a stray file in a visit folder is reported");

        // The catalog throws on any error, and keeps throwing.
        var before = VisitCatalog.Folder;
        VisitCatalog.Folder = probe;
        for (var i = 0; i < 2; i++)
        {
            try { _ = VisitCatalog.Scenes; _fail("visit: a catalog with errors loads"); }
            catch (StoryLoadException e) { Expect(e.Errors.Count > 0, "the load exception lists the errors"); }
        }
        VisitCatalog.Folder = before;
        Directory.Delete(probe, true);
    }

    private static VisitScene? ParseFile(string name, out List<StoryError> errors)
    {
        errors = new List<StoryError>();
        var path = Path.Combine(FixtureFolder, name);
        return VisitParser.Parse(name, File.ReadAllText(path), errors);
    }

    private static VisitScene? Parse(string text, out List<StoryError> errors)
    {
        errors = new List<StoryError>();
        return VisitParser.Parse("t.visit", text, errors);
    }

    // Exactly one error, on the given line, whose message contains the fragment.
    private static void ExpectOneError(string what, string text, int line, string fragment)
    {
        Parse(text, out var errors);
        Expect(errors.Count == 1 && errors[0].Pos.Line == line && errors[0].Message.Contains(fragment),
            $"{what}: expected one error on line {line} with '{fragment}', got: {string.Join("; ", errors)}");
    }

    // A small valid visit, and its confession twin; the error cases below change one thing each.
    private const string Valid =
        "# visit: v\n" +                         // 1
        "title: T\n" +                           // 2
        "place: 🌳 Ici\n" +                      // 3
        "bonds: friends rivals\n" +              // 4
        "hours: 8-20\n" +                        // 5
        "\n" +                                   // 6
        "Intro de {A} et {B}.\n" +               // 7
        "\n" +                                   // 8
        "* go | Y aller  [lean boldness +1]\n" + // 9
        "  ? good\n" +                           // 10
        "  {A} y va.\n" +                        // 11
        "  ? bad\n" +                            // 12
        "  {A} n'y va pas.\n" +                  // 13
        "\n" +                                   // 14
        "> stay | Rester  [lean energy -1]\n" +  // 15
        "  ? good\n" +                           // 16
        "  {B} reste.\n" +                       // 17
        "  ? bad\n" +                            // 18
        "  {B} part.\n" +                        // 19
        "\n" +
        "* run | Courir  [lean energy +2]\n" +
        "  ? good\n  {A} court.\n  ? bad\n  {A} tombe.\n" +
        "\n" +
        "> sit | S'asseoir  [lean energy -2] [lean honor +1]\n" +
        "  ? good\n  {B} s'assoit.\n  ? bad\n  {B} s'endort.\n";

    private static readonly string ConfessionText = Valid
        .Replace("# visit: v", "# confession: c").Replace("bonds: friends rivals\n", "")
        .Replace("? good", "? yes").Replace("? bad", "? no");

    // 56a. The parser: the fixtures, the shapes, one error per mistake, on its line.
    private static void Parser()
    {
        var v = ParseFile("test_visit.visit", out var errors);
        Expect(errors.Count == 0 && v is not null, "test_visit.visit parses: " + string.Join("; ", errors));
        if (v is not null)
        {
            Expect(v.Key == "test_visit_scene_abc" && !v.Confession, "the fixture's key and kind");
            Expect(v.Title == "Le cerf-volant" && v.Place == "🌳 Sous le grand chêne de {B}", "title and place");
            Expect(v.Bonds.Count == 6 && v.Stages.Count == 4, "all six bonds and four stages");
            Expect(v.Options.Count >= 2 && v.Reactions.Count >= 2, "two options and two reactions at least");
            Expect(v.Options.Concat(v.Reactions).All(c => c.Halves.Good.Length > 0 && c.Halves.Bad.Length > 0), "every choice has both halves");
            Expect(v.Options[0].Key == "twelve_chars" && v.Options[0].Lean[AiAxis.Boldness] == 2 && v.Options[0].Lean[AiAxis.Energy] == 1,
                "leans read back exactly");
            Expect(v.Options[1].Lean.Count == 1 && v.Options[1].Lean[AiAxis.Rationality] == -1, "a lean on its own attribute line");
            Expect(v.Options[0].Halves.Good == "{A} grimpe et décroche le cerf-volant du premier coup.", "a half's lines join with one space: " + v.Options[0].Halves.Good);
            Expect(v.Options[0].Halves.Bad.StartsWith("[sad] "), "a face tag stays in the half's text");
            Expect(v.Intro.Contains("chêne.\n{A} lève") && v.Intro.Contains("tête.\n\nUn deuxième"), "intro lines and paragraphs: " + v.Intro);
            Expect(v.IsOpenAt(8) && v.IsOpenAt(19) && !v.IsOpenAt(20) && !v.IsOpenAt(7), "hours 8-20 open from 8 to 19");
            Expect(v.Reactions.Select(r => r.Key).SequenceEqual(new[] { "hold", "laugh" }), "reactions in file order");
            Expect(v.Pos.Line == 4 && v.Options[0].Pos.Line == 16, $"positions: scene {v.Pos.Line}, option {v.Options[0].Pos.Line}");
        }
        var night = ParseFile("test_night.visit", out errors);
        Expect(errors.Count == 0 && night is not null, "test_night.visit parses: " + string.Join("; ", errors));
        if (night is not null)
        {
            Expect(night.IsOpenAt(23) && night.IsOpenAt(0) && night.IsOpenAt(20) && !night.IsOpenAt(1) && !night.IsOpenAt(12), "across midnight, 20-1");
            Expect(night.Stages.Count == 4, "no stages line means all four");
        }
        var c = ParseFile("test_confession.visit", out errors);
        Expect(errors.Count == 0 && c is not null, "test_confession.visit parses: " + string.Join("; ", errors));
        if (c is not null)
        {
            Expect(c.Confession && c.Bonds.Count == 0, "a confession has no bonds");
            Expect(c.Options[0].Halves.Good == "{A} le dit d'un trait." && c.Options[0].Halves.Bad.StartsWith("[sad]"), "'? yes' / '? no' read as the halves");
            Expect(c.IsOpenAt(3) && c.IsOpenAt(15), "no hours line means all day");
        }

        Parse(Valid, out errors);
        Expect(errors.Count == 0, "the inline visit parses: " + string.Join("; ", errors));
        Parse(ConfessionText, out errors);
        Expect(errors.Count == 0, "the inline confession parses: " + string.Join("; ", errors));
        var crlf = Parse("﻿" + Valid.Replace("\n", "\r\n"), out errors);
        var lf = Parse(Valid, out _);
        Expect(errors.Count == 0 && crlf is not null && lf is not null && crlf.Intro == lf.Intro
               && crlf.Options[0].Halves == lf.Options[0].Halves && crlf.Reactions[0].Pos == lf.Reactions[0].Pos,
            "a CRLF + BOM file parses like its LF twin: " + string.Join("; ", errors));

        ExpectOneError("unknown header field", Valid.Replace("hours: 8-20\n", "hours: 8-20\nmood: gai\n"), 6, "unknown header field 'mood'");
        ExpectOneError("bonds in a confession", ConfessionText.Replace("place: 🌳 Ici\n", "place: 🌳 Ici\nbonds: friends\n"), 4, "bonds");
        ExpectOneError("? good in a confession", ConfessionText.Replace("  ? yes\n  {A} y va.", "  ? good\n  {A} y va."), 9, "? yes");
        ExpectOneError("? yes in a visit", Valid.Replace("  ? good\n  {B} reste.", "  ? yes\n  {B} reste."), 16, "? good");
        ExpectOneError("an option without '? bad'", Valid.Replace("  ? bad\n  {A} n'y va pas.\n", ""), 9, "? bad");
        ExpectOneError("an unknown axis", Valid.Replace("[lean boldness +1]", "[lean bravery +1]"), 9, "axis");
        ExpectOneError("a roll on an option", Valid.Replace("[lean boldness +1]", "[lean boldness +1] [roll diplomacy 3]"), 9, "lean");
        ExpectOneError("a ## section", Valid.Replace("\n> stay", "\n## scene: two\n> stay"), 15, "##");
        ExpectOneError("a bad hours value", Valid.Replace("hours: 8-20", "hours: 8-8"), 5, "hours");
        ExpectOneError("a bad stage", Valid.Replace("hours: 8-20\n", "hours: 8-20\nstages: teen baby old\n"), 6, "old");
        ExpectOneError("a bad bond", Valid.Replace("friends rivals", "friends pals"), 4, "pals");
        ExpectOneError("a header field twice", Valid.Replace("hours: 8-20\n", "hours: 8-20\ntitle: U\n"), 6, "twice");
        ExpectOneError("prose before the first half", Valid.Replace("  ? good\n  {A} y va.", "  {A} hésite.\n  ? good\n  {A} y va."), 10, "? good");
        ExpectOneError("a field after the blank line", Valid.Replace("Intro de", "stages: teen\nIntro de"), 7, "header");
        ExpectOneError("a tag inside a half", Valid.Replace("{A} y va.", "{A} y va. [lean energy +1]"), 11, "tag");
        ExpectOneError("a lean twice", Valid.Replace("[lean boldness +1]", "[lean boldness +1] [lean boldness -1]"), 9, "twice");
        ExpectOneError("a wrong kind line", Valid.Replace("# visit: v", "# arc: v"), 1, "# visit");
        ExpectOneError("a bad choice line", Valid.Replace("* go | Y aller", "* go Y aller"), 9, "'* <key> | <label>'");
    }
}
