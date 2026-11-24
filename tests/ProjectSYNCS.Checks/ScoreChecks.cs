using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 68. The Plynling score (docs/agents/plynling.md, *Score*): the point values, the hooks against an in-memory
// SQLite (a meal, someone else's meal, a refusal, the mascot, a drill, bonds once per pair, good mornings, the
// staff cure, a good visit and a squabble, a run's end — short, long, continued, decided alone), the rebuild of a history kept before the score (floor, collections, once only), the tombstone,
// and the /leaderboard Plynlings view at its fullest. The calibration is ScoreCalibrationChecks.
public static class ScoreChecks
{
    private static Action<string> _fail = _ => { };
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private const ulong G = 1, Owner = 2, Other = 3, Poor = 4, Bot = 999, Twin = 5;

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("score: " + what);
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        Values();
        Tombstone();
        LeaderboardPage();
        PlynlingMascot.Bind(Bot);
        try { await ServiceAsync(); }
        catch (Exception ex) { Expect(false, "service threw: " + ex); }
        finally { PlynlingMascot.Bind(0); }
    }

    private static void Values()
    {
        Expect(PlynlingScore.Meal(PlynlingFood.Mushroom) < PlynlingScore.Meal(PlynlingFood.Shiitake)
               && PlynlingScore.Meal(PlynlingFood.Shiitake) < PlynlingScore.Meal(PlynlingFood.Morel)
               && PlynlingScore.Meal(PlynlingFood.Morel) < PlynlingScore.Meal(PlynlingFood.Truffle), "finer foods are worth more");
        foreach (var badge in PlynlingBadges.All)
            Expect(PlynlingScore.ForMoment(JournalKind.Badge, badge.Key) == badge.Reward, $"badge {badge.Key} scores its reward");
        Expect(PlynlingScore.ForMoment(JournalKind.GrewUp, nameof(PlynlingStage.Teen)) == 10
               && PlynlingScore.ForMoment(JournalKind.GrewUp, nameof(PlynlingStage.Adult)) == 20
               && PlynlingScore.ForMoment(JournalKind.GrewUp, nameof(PlynlingStage.Elder)) == 40, "growing up: 10 / 20 / 40");
        Expect(PlynlingScore.ForMoment(JournalKind.TraitGained, "x") == PlynlingScore.TraitGained
               && PlynlingScore.ForMoment(JournalKind.Recovered, null) == PlynlingScore.Recovered, "a trait, a recovery");
        foreach (var kind in new[] { JournalKind.BecameFriends, JournalKind.BecameBestFriends, JournalKind.BecameLovers,
                     JournalKind.BecameRivals, JournalKind.BecameEnemies, JournalKind.Heartbroken, JournalKind.BrokeUp,
                     JournalKind.EventStory, JournalKind.Adopted, JournalKind.Died })
            Expect(PlynlingScore.ForMoment(kind, "x") == 0, $"{kind} scores nothing through the journal");
        Expect(PlynlingScore.Bond(PlynlingBond.Acquaintances) == 0 && PlynlingScore.Bond(PlynlingBond.Lovers) == 40
               && PlynlingScore.Bond(PlynlingBond.Enemies) == 15, "bond values");
        var bits = Enum.GetValues<PlynlingBond>().Select(PlynlingScore.BondBit).ToList();
        Expect(bits.Distinct().Count() == bits.Count, "every bond has its own bit");
        foreach (var kind in Enum.GetValues<JournalKind>())
            Expect(PlynlingScoreHistory.BondOf(kind) is null || kind.ToString().StartsWith("Became"), $"{kind} names no bond");
    }

    private static void Tombstone()
    {
        var p = new Plynling();
        Expect(PlynlingCatalog.MemorialTier(p) == 1, "no score: a cairn");
        var scores = PlynlingCatalog.MemorialScores;
        Expect(scores.Zip(scores.Skip(1)).All(t => t.First < t.Second), "the thresholds climb");
        for (var i = 0; i < scores.Count; i++)
        {
            p.Score = scores[i] - 1;
            Expect(PlynlingCatalog.MemorialTier(p) == i + 1, $"just under {scores[i]}: tier {i + 1}");
            p.Score = scores[i];
            Expect(PlynlingCatalog.MemorialTier(p) == i + 2, $"at {scores[i]}: tier {i + 2}");
        }
        p.Score = 0;
        p.TombstoneFloor = 4;
        Expect(PlynlingCatalog.MemorialTier(p) == 4, "a low score keeps the floor: never worse");
        p.Score = scores[^1];
        p.TombstoneFloor = 2;
        Expect(PlynlingCatalog.MemorialTier(p) == 5, "a high score raises an old grave");
    }

    // The Plynlings view at its fullest: twelve Plynlings, long names, the dead among them, every window.
    private static void LeaderboardPage()
    {
        var ranking = Enumerable.Range(1, 12).Select(i =>
        {
            var p = PlynlingLife.Create(G, (ulong)(100 + i), new string('W', 40), PlynlingSpecies.Cepe, PlynlingGender.Male, Now.AddDays(-30));
            p.Id = i;
            if (i % 2 == 0) p.DiedAt = Now.AddDays(-1);
            p.Score = 9_999_999_999;
            return new PlynlingStanding(p, 9_999_999_999);
        }).ToList();
        var worst = 0;
        foreach (var period in Enum.GetValues<StatsPeriod>())
        foreach (var list in new[] { ranking, new List<PlynlingStanding>() })
        foreach (var viewer in new ulong[] { 7, 104 })
        {
            var card = ProjectSYNCS.Commands.LevelModule.BuildPlynlingPage(list, period, 1, viewer, Now);
            var parts = card.Components.Select(EconomySinkChecks.Measure).ToList();
            var (text, count, ids) = (parts.Sum(x => x.Text), parts.Sum(x => x.Count), parts.SelectMany(x => x.Ids).ToList());
            worst = Math.Max(worst, count);
            Expect(text <= 4000 && count <= 40 && ids.Count == ids.Distinct().Count(),
                $"plynlings page {period}/{list.Count}: {text} chars, {count} components, {ids.Count - ids.Distinct().Count()} duplicate id(s)");
        }
        Console.WriteLine($"leaderboard Plynlings page at worst: {worst} components");
        var row = LevelCardUi.PlynlingRow(1, ranking[1].Plynling, 1234);
        Expect(row.Contains("🕯️") && row.Contains("<@102>") && row.EndsWith(LevelCardUi.Points(1234)), "a dead one's row: " + row);
        Expect(!LevelCardUi.PlynlingRow(1, ranking[0].Plynling, 1).Contains("🕯️") && LevelCardUi.Points(1) == "1 point", "a living one's row");
    }

    private static async Task ServiceAsync()
    {
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        using (var c = new AppDbContext(opts)) c.Database.EnsureCreated();
        var services = new ServiceCollection().AddDbContext<AppDbContext>(o => o.UseSqlite(conn)).BuildServiceProvider();
        PlynlingService Service(AppDbContext c) => new(c, NullLogger<PlynlingService>.Instance, new PlynlingCooldowns(),
            new BadgeAwarder(services, new Discord.WebSocket.DiscordSocketClient(), NullLogger<BadgeAwarder>.Instance));

        int Add(ulong owner, string name, int ageDays, Action<Plynling>? set = null)
        {
            using var c = new AppDbContext(opts);
            var row = PlynlingLife.Create(G, owner, name, PlynlingSpecies.Cepe, PlynlingGender.Female, Now.AddDays(-ageDays));
            row.Hunger = 0.3;
            row.Happiness = row.Hygiene = 1;
            row.NeedsAsOf = Now;
            row.LastMorningDay = AppTime.DayKey(Now);       // 12:00 UTC: today's 05:00 is played, no morning to replay
            set?.Invoke(row);
            c.Plynlings.Add(row);
            c.SaveChanges();
            return row.Id;
        }
        Plynling Read(int id) { using var c = new AppDbContext(opts); return c.Plynlings.AsNoTracking().Single(x => x.Id == id); }
        long Bucket(int id) { using var c = new AppDbContext(opts); return c.PlynlingScoreDays.Where(b => b.PlynlingId == id).Sum(b => b.Points); }
        long OwnerTotal(ulong user) { using var c = new AppDbContext(opts); return c.PlynlingOwnerScores.Where(o => o.UserId == user).Sum(o => o.Points); }
        // Badges score their reward too, and an action may earn one (a 20-day-old earns « Une semaine » on its first).
        long Badges(int id)
        {
            using var c = new AppDbContext(opts);
            return c.PlynlingBadges.Where(x => x.PlynlingId == id).Select(x => x.Key).ToList().Sum(k => PlynlingBadges.ByKey(k)!.Reward);
        }

        using (var c = new AppDbContext(opts))
        {
            (await PebbleService.GetOrCreateWalletAsync(c, G, Owner)).Balance = 10_000;
            (await PebbleService.GetOrCreateWalletAsync(c, G, Other)).Balance = 10_000;
            await c.SaveChangesAsync();
        }

        // A meal: its food's points, today's bucket and the owner's total, in the meal's save.
        var a = Add(Owner, "Alpha", 20);
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).FeedAsync(a, Owner, PlynlingFood.Truffle, Now)).Outcome == CareOutcome.Done, "the owner feeds");
        var truffle = PlynlingScore.Meal(PlynlingFood.Truffle);
        Expect(Read(a).Score == truffle + Badges(a) && Bucket(a) == Read(a).Score && OwnerTotal(Owner) == Read(a).Score,
            $"a truffle: score {Read(a).Score}, bucket {Bucket(a)}, owner {OwnerTotal(Owner)}");

        // Someone else's meal scores the Plynling, and so its owner, never the helper.
        using (var c = new AppDbContext(opts))
        {
            var row = c.Plynlings.Single(x => x.Id == a);
            row.Hunger = 0.3;
            row.NeedsAsOf = Now;
            c.SaveChanges();
        }
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).FeedAsync(a, Other, PlynlingFood.Mushroom, Now)).Outcome == CareOutcome.Done, "a friend feeds");
        var afterFriend = truffle + PlynlingScore.Meal(PlynlingFood.Mushroom);
        Expect(Read(a).Score >= afterFriend && OwnerTotal(Owner) == Read(a).Score && OwnerTotal(Other) == 0,
            $"a friend's meal: score {Read(a).Score}, owner {OwnerTotal(Owner)}, helper {OwnerTotal(Other)}");

        Expect(Read(a).Score == afterFriend + Badges(a), $"a friend's meal: {Read(a).Score} for {afterFriend} + badges {Badges(a)}");

        // A refusal scores nothing.
        var before = Read(a).Score;
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).FeedAsync(a, Poor, PlynlingFood.Truffle, Now)).Outcome == CareOutcome.TooPoor, "too poor");
        Expect(Read(a).Score == before, "a refused meal scores nothing");

        // A pet, a drill (an adulte trains).
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).PetAsync(a, Now)).Outcome == CareOutcome.Done, "a pet");
        Expect(Read(a).Score == before + PlynlingScore.Pet, "a pet scores " + PlynlingScore.Pet);
        before = Read(a).Score;
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).TrainAsync(G, Owner, PlynlingStat.Force, Now)).Outcome == TrainOutcome.Done, "a drill");
        Expect(Read(a).Score == before + PlynlingScore.Drill, $"a drill scores {PlynlingScore.Drill}: {Read(a).Score - before}");

        // The mascot is cared for like anyone, and never scores.
        var m = Add(Bot, PlynlingMascot.Name, 40);
        using (var c = new AppDbContext(opts))
            await Service(c).FeedAsync(m, Owner, PlynlingFood.Shiitake, Now);
        using (var c = new AppDbContext(opts))
            await Service(c).PetAsync(m, Now);
        Expect(Read(m).Score == 0 && Bucket(m) == 0 && OwnerTotal(Bot) == 0, "the mascot never scores");

        // A bond scores each side once per pair: making up after drifting apart adds nothing.
        var b = Add(Other, "Beta", 20);
        long Pair() => Read(a).Score + Read(b).Score - Badges(a) - Badges(b);     // badges aside: they are the Plynling's firsts
        async Task Shift(int delta)
        {
            using var c = new AppDbContext(opts);
            var s = Service(c);
            var pa = c.Plynlings.Single(x => x.Id == a);
            var pb = c.Plynlings.Single(x => x.Id == b);
            await s.ShiftAffinityAsync(pa, pb, delta, Now);
            await c.SaveChangesAsync();
        }
        var pairBefore = Pair();
        await Shift(45);
        Expect(Pair() - pairBefore == 2 * PlynlingScore.Bond(PlynlingBond.Friends), $"friends: {Pair() - pairBefore}");
        await Shift(-45);
        var drifted = Pair();
        await Shift(45);
        Expect(Pair() == drifted, $"friends again scores nothing: {Pair() - drifted}");
        using (var c = new AppDbContext(opts))
            Expect((c.PlynlingRelations.Single().ScoredBonds & PlynlingScore.BondBit(PlynlingBond.Friends)) != 0, "the pair's friends bit is set");

        // Good mornings: a list settles in memory and saves nothing; the real read then scores each once.
        var g = Add(Owner + 100, "Gamma", 20, row =>
        {
            row.Hunger = row.Happiness = row.Hygiene = 1;
            row.NeedsAsOf = Now.AddDays(-2);
            row.LastMorningDay = AppTime.DayKey(Now.AddDays(-2));
        });
        var copy = Read(g);
        PlynlingLife.Settle(copy, Now);
        var expected = copy.PendingScore.Sum(x => x.Points);
        using (var c = new AppDbContext(opts))
            await Service(c).GetLivingAsync(G, Now);
        Expect(Read(g).Score == 0, "a list's settle scores nothing");
        using (var c = new AppDbContext(opts))
            await Service(c).GetByIdAsync(g, Now);
        Expect(expected > 0 && Read(g).Score == expected, $"good mornings: {Read(g).Score} for {expected} expected");
        using (var c = new AppDbContext(opts))
            await Service(c).GetByIdAsync(g, Now);
        Expect(Read(g).Score == expected, "a morning scores once");

        // A frozen Plynling's mornings score nothing.
        var f = Add(Owner + 200, "Frozen", 20, row =>
        {
            row.Hunger = row.Happiness = row.Hygiene = 1;
            row.NeedsAsOf = Now.AddDays(-2);
            row.LastMorningDay = AppTime.DayKey(Now.AddDays(-2));
            row.FrozenAt = Now.AddDays(-2);
            row.FrozenByStaff = true;
        });
        using (var c = new AppDbContext(opts))
            await Service(c).GetByIdAsync(f, Now);
        Expect(Read(f).Score == 0, "frozen mornings score nothing");

        // The staff cure writes « guéri » and scores nothing.
        var s2 = Add(Owner + 300, "Sick", 20, row => row.SickSince = Now.AddDays(-1));
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).CureAsync(G, Owner + 300, Now)).Outcome == CureOutcome.Cured, "staff cure");
        Expect(Read(s2).Score == 0, "the staff cure scores nothing");

        // The rebuild: a Plynling that died before the score, its history counted once, its old tombstone a floor;
        // a collection counts only on the days it lived.
        int Dead(ulong owner, string name) => Add(owner, name, 60, row =>
        {
            row.ScoreRebuilt = false;
            row.Meals = 30;
            row.Pets = 40;
            row.Plays = 10;
            row.DiedAt = Now.AddDays(-20);
            row.AgeBankedSeconds = (long)TimeSpan.FromDays(40).TotalSeconds;
        });
        var d1 = Dead(Twin, "Old");
        var d2 = Dead(Twin + 1, "OldTwin");
        using (var c = new AppDbContext(opts))
        {
            c.CollectionDailyStats.Add(new CollectionDailyStat { GuildId = G, UserId = Twin, Day = AppTime.DayKey(Now.AddDays(-30)), Completions = 2 });
            c.CollectionDailyStats.Add(new CollectionDailyStat { GuildId = G, UserId = Twin, Day = AppTime.DayKey(Now.AddDays(-5)), Completions = 7 });
            c.SaveChanges();
        }
        var living = Add(Owner + 400, "Living", 30, row => row.ScoreRebuilt = false);
        using (var c = new AppDbContext(opts))
            Expect((await Service(c).GetScoreRebuildIdsAsync()).OrderBy(x => x).SequenceEqual(new[] { d1, d2, living }.OrderBy(x => x)),
                "the rebuild takes the rows not yet rebuilt, the dead included");
        foreach (var id in new[] { d1, d2, living })
            using (var c = new AppDbContext(opts))
                Expect(await Service(c).RebuildScoreAsync(id, Now) > 0, $"rebuild {id}");
        var old = Read(d1);
        Expect(old.ScoreRebuilt && old.TombstoneFloor == PlynlingCatalog.MemorialTier(TimeSpan.FromDays(40)), $"the dead keep their old tombstone as a floor: {old.TombstoneFloor}");
        Expect(PlynlingCatalog.MemorialTier(old) >= old.TombstoneFloor, "never worse");
        Expect(Read(living).TombstoneFloor == 0, "the living get no floor");
        Expect(old.Score - Read(d2).Score == 2 * PlynlingScore.Collection, $"collections on its days only: {old.Score - Read(d2).Score}");
        Expect(OwnerTotal(Twin) == old.Score, "the rebuild adds to the owner's total");
        Expect(Bucket(d1) == 0, "the rebuild writes no bucket: the windows start on release day");
        using (var c = new AppDbContext(opts))
            Expect(await Service(c).RebuildScoreAsync(d1, Now) is null, "a second rebuild does nothing");
        Expect(Read(d1).Score == old.Score, "rebuilt once");

        // A visit scores both sides: Visit, plus the good-scene bonus when it went well. Fresh pairs, the same
        // day (a next day would play a morning), no finds (an item could complete a set and score it).
        async Task<(long Visitor, long Host)> VisitDelta(double roll)
        {
            var v = Add(Owner + 500 + (ulong)(roll * 100), "Visitor", 20);
            var h = Add(Owner + 600 + (ulong)(roll * 100), "Host", 20);
            long Net(int id) => Read(id).Score - Badges(id);
            using var c = new AppDbContext(opts);
            var o = await Service(c).VisitAsync(v, h, Now, new VisitPlay(0, Confess: false), new FixedRandom(roll), new FixedRandom(0.99));
            Expect(o is { TooLate: false } && o.GoodScene == roll < 0.5, $"the visit played (roll {roll}): good {o?.GoodScene}");
            return (Net(v), Net(h));
        }
        var goodVisit = await VisitDelta(0.0);
        var goodPoints = PlynlingScore.Visit + PlynlingScore.VisitGoodScene;
        Expect(goodVisit == (goodPoints, goodPoints), $"a good visit scores {goodPoints} each: {goodVisit}");
        var badVisit = await VisitDelta(0.99);
        Expect(badVisit == (PlynlingScore.Visit, PlynlingScore.Visit), $"a squabble scores {PlynlingScore.Visit} each: {badVisit}");

        await EventsAsync(opts, Service, Add, Read, Badges);

        // The ranking: the dead included, the mascot never; a window reads the buckets.
        using (var c = new AppDbContext(opts))
        {
            var all = await Service(c).GetScoreRankingAsync(G, StatsPeriod.AllTime, Now);
            Expect(all.Any(x => x.Plynling.Id == d1) && all.All(x => x.Plynling.Id != m), "all time: the dead in, the mascot out");
            Expect(all.Zip(all.Skip(1)).All(t => t.First.Points >= t.Second.Points), "ranked by points");
            var week = await Service(c).GetScoreRankingAsync(G, StatsPeriod.Week, Now);
            Expect(week.All(x => x.Plynling.Id != d1) && week.Any(x => x.Plynling.Id == a), "the week reads today's buckets, not the rebuilt history");
        }
    }

    // A run scores once, at its end, only when its owner chose: a short run's last step 3, a long story's ending
    // 10, a step that queues a follow-up nothing, a step the sweep decides alone nothing. Real events from the
    // catalog: for each case, the first option an adult can pick (no roll, nothing that reaches another
    // Plynling, no trait change or item that would score on its own).
    private static async Task EventsAsync(DbContextOptions<AppDbContext> opts, Func<AppDbContext, PlynlingService> service,
        Func<ulong, string, int, Action<Plynling>?, int> add, Func<int, Plynling> read, Func<int, long> badges)
    {
        static bool Plain(EventOption o) => o.Challenge is null && o.Gate is null && !o.OwnerOnly
            && o.OnSuccess.All(e => e is not (AskTarget or GainCoping or SwapTrait or Couple or Heartbreak or GiveItem
                or AffinityShift or SetAffinityAtLeast));
        static bool Continues(EventOption o) => o.OnSuccess.Any(e => e is FollowUp);
        static bool Solo(EventDef d) => d.Target == TargetKind.None && d.Type != EventType.Response && d.BreakLevel == 0;

        var next = 0;
        // Picks `option` of `def` on a fresh adult; the score it gained, badges aside, or null if it wasn't pickable.
        async Task<long?> Pick(EventDef def, EventOption option)
        {
            var id = add(Owner + 1000 + (ulong)next++, "Eventful", 20, null);
            int instance;
            using (var c = new AppDbContext(opts))
            {
                var row = new PlynlingEventInstance
                {
                    PlynlingId = id, EventKey = def.Key, CreatedAt = Now.AddHours(-1),
                    AvailableAt = Now.AddHours(-1), ExpiresAt = Now.AddHours(23),
                };
                c.PlynlingEventInstances.Add(row);
                c.SaveChanges();
                instance = row.Id;
            }
            using (var c = new AppDbContext(opts))
            {
                var r = await service(c).PickEventAsync(instance, option.Key, Owner + 1000 + (ulong)(next - 1), Now);
                if (r.Outcome != EventPickOutcome.Done) return null;
            }
            return read(id).Score - badges(id);
        }

        // The first candidate the engine lets an adult pick; its points against what it should score.
        async Task Case(string what, IEnumerable<(EventDef Def, EventOption Option)> candidates, long expected)
        {
            foreach (var (def, option) in candidates)
            {
                if (await Pick(def, option) is not { } got) continue;
                Expect(got == expected, $"{what} ({def.Key}/{option.Key}) scores {expected}: {got}");
                Console.WriteLine($"score events: {what} — {def.Key}/{option.Key}, {got} point(s)");
                return;
            }
            Expect(false, $"no pickable event for « {what} »");
        }

        var all = PlynlingEvents.All.Where(Solo).ToList();
        await Case("a short run's last step",
            all.Where(d => !PlynlingEventEngine.IsLong(d)).SelectMany(d => d.Options.Where(o => Plain(o) && !Continues(o)).Select(o => (d, o))),
            PlynlingScore.EventShort);
        await Case("a long story's ending",
            all.Where(PlynlingEventEngine.IsLong)
                .SelectMany(d => d.Options.Where(o => Plain(o) && !Continues(o) && o.OnSuccess.Any(e => e is EndStory)).Select(o => (d, o))),
            PlynlingScore.EventLong);
        await Case("a step that continues",
            all.SelectMany(d => d.Options.Where(o => Plain(o) && Continues(o)).Select(o => (d, o))),
            0);

        // Left to decide alone: the sweep resolves it once expired, and that scores nothing.
        var alone = all.First(d => !PlynlingEventEngine.IsLong(d) && d.Options.Any(o => Plain(o) && !Continues(o)));
        var pid = add(Owner + 2000, "Alone", 20, null);
        int inst;
        using (var c = new AppDbContext(opts))
        {
            var row = new PlynlingEventInstance
            {
                PlynlingId = pid, EventKey = alone.Key, CreatedAt = Now.AddDays(-2),
                AvailableAt = Now.AddDays(-2), ExpiresAt = Now.AddMinutes(-1),
            };
            c.PlynlingEventInstances.Add(row);
            c.SaveChanges();
            inst = row.Id;
        }
        using (var c = new AppDbContext(opts))
            await service(c).SweepOneAsync(pid, Now);
        using (var c = new AppDbContext(opts))
            Expect(c.PlynlingEventInstances.Single(i => i.Id == inst) is { ResolvedAt: not null, DecidedAlone: true },
                "the sweep decided the expired event alone");
        Expect(read(pid).Score - badges(pid) == 0, $"decided alone scores nothing: {read(pid).Score - badges(pid)}");
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
        protected override double Sample() => value;
    }
}
