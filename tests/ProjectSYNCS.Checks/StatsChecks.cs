using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 74. /plynling stats (docs/agents/plynling.md, *Stats*): the counter keys and writers, each action's counting
// against an in-memory SQLite, the pure paths (sick days, frozen time), the sheet and its ranks, the tab and
// duel lines, and the cards at their fullest.
public static class StatsChecks
{
    private static Action<string> _fail = _ => { };
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private const ulong G = 1, Owner = 2, Other = 3, Bot = 999;

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("stats: " + what);
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        Keys();
        Lines();
        Cards();
        PlynlingMascot.Bind(Bot);
        try
        {
            await WriterAsync();
            await CareAsync();
            await PlayVisitAsync();
            Pure();
            await ThawAsync();
            await SheetAsync();
        }
        catch (Exception ex) { Expect(false, "service threw: " + ex); }
        finally { PlynlingMascot.Bind(0); }
    }

    // ---- The harness: one in-memory database per run -------------------------------------------------------------

    private sealed class Db : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
        public readonly DbContextOptions<AppDbContext> Opts;
        private readonly ServiceProvider _services;

        public Db()
        {
            _conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            _conn.Open();
            Opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
            using (var c = New()) c.Database.EnsureCreated();
            _services = new ServiceCollection().AddDbContext<AppDbContext>(o => o.UseSqlite(_conn)).BuildServiceProvider();
        }

        public AppDbContext New() => new(Opts);

        public PlynlingService Service(AppDbContext c) => new(c, NullLogger<PlynlingService>.Instance, new PlynlingCooldowns(),
            new BadgeAwarder(_services, new Discord.WebSocket.DiscordSocketClient(), NullLogger<BadgeAwarder>.Instance));

        public int Add(ulong owner, string name, DateTimeOffset adopted, Action<Plynling>? set = null)
        {
            using var c = New();
            var row = PlynlingLife.Create(G, owner, name, PlynlingSpecies.Cepe, PlynlingGender.Female, adopted);
            row.Hunger = 0.3;
            row.Happiness = row.Hygiene = 1;
            row.NeedsAsOf = Now;
            row.LastMorningDay = AppTime.DayKey(Now);       // 12:00 UTC: today's 05:00 is played
            set?.Invoke(row);
            c.Plynlings.Add(row);
            c.SaveChanges();
            return row.Id;
        }

        public long Count(int id, string key)
        {
            using var c = New();
            return c.PlynlingCounters.Where(x => x.PlynlingId == id && x.Key == key).Sum(x => x.Value);
        }

        public PlynlingCarer? Carer(int id, ulong user)
        {
            using var c = New();
            return c.PlynlingCarers.AsNoTracking().SingleOrDefault(x => x.PlynlingId == id && x.UserId == user);
        }

        public void Dispose()
        {
            _services.Dispose();
            _conn.Dispose();
        }
    }

    // ---- Task 1: keys and writers --------------------------------------------------------------------------------

    private static void Keys()
    {
        var all = PlynlingCounters.All;
        Expect(all.Distinct().Count() == all.Count, "two counters share a key");
        foreach (var f in Enum.GetValues<PlynlingFood>())
            Expect(all.Contains(PlynlingCounters.Food(f)), "no key for " + f);
        foreach (var g in Enum.GetValues<PlynlingGame>())
            Expect(all.Contains(PlynlingCounters.Game(g)) && all.Contains(PlynlingCounters.GameWon(g)), "no key for " + g);
        Expect(PlynlingCounters.Food(PlynlingFood.Morel) == "meal.food.morel", "food key: " + PlynlingCounters.Food(PlynlingFood.Morel));
        Expect(PlynlingCounters.GameWon(PlynlingGame.Memory) == "play.memory.won", "game key");
        Expect(all.Contains("find.event"), "find.event missing");

        var before = PlynlingLife.Create(G, Owner, "Avant", PlynlingSpecies.Cepe, PlynlingGender.Male, new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
        var after = PlynlingLife.Create(G, Owner, "Après", PlynlingSpecies.Cepe, PlynlingGender.Male, Now);
        Expect(PlynlingCounters.IsPartial(before) && !PlynlingCounters.IsPartial(after), "IsPartial");
    }

    private static async Task WriterAsync()
    {
        using var db = new Db();
        var a = db.Add(Owner, "Alpha", Now.AddDays(-3));
        using (var c = db.New())
        {
            var p = c.Plynlings.Single(x => x.Id == a);
            await PlynlingCounters.AddAsync(c, p, PlynlingCounters.BathFree, 1);
            await PlynlingCounters.AddAsync(c, p, PlynlingCounters.BathFree, 1);
            await PlynlingCounters.AddAsync(c, p, PlynlingCounters.BathSoap, 0);
            await PlynlingCounters.AddCarerAsync(c, p, Other, 1, 0);
            await PlynlingCounters.AddCarerAsync(c, p, Other, 1, 0);
            await c.SaveChangesAsync();
        }
        using (var c = db.New())
            Expect(c.PlynlingCounters.Count(x => x.PlynlingId == a && x.Key == PlynlingCounters.BathFree) == 1, "one row per key");
        Expect(db.Count(a, PlynlingCounters.BathFree) == 2, "two baths: " + db.Count(a, PlynlingCounters.BathFree));
        using (var c = db.New())
            Expect(!c.PlynlingCounters.Any(x => x.Key == PlynlingCounters.BathSoap), "a zero adds no row");
        Expect(db.Carer(a, Other)?.Meals == 2, "carer meals");

        // A later save adds to the stored row.
        using (var c = db.New())
        {
            var p = c.Plynlings.Single(x => x.Id == a);
            await PlynlingCounters.AddAsync(c, p, PlynlingCounters.BathFree, 3);
            await c.SaveChangesAsync();
        }
        Expect(db.Count(a, PlynlingCounters.BathFree) == 5, "a later save adds");

        // Abandoned (row deleted): its counters and carers go with it.
        using (var c = db.New())
        {
            c.Plynlings.Remove(c.Plynlings.Single(x => x.Id == a));
            await c.SaveChangesAsync();
        }
        using (var c = db.New())
            Expect(!c.PlynlingCounters.Any(x => x.PlynlingId == a) && !c.PlynlingCarers.Any(x => x.PlynlingId == a), "cascade on abandon");
    }

    // ---- Task 2: care ------------------------------------------------------------------------------------------

    private static void Hungry(Db db, int id, Action<Plynling>? set = null)
    {
        using var c = db.New();
        var row = c.Plynlings.Single(x => x.Id == id);
        row.Hunger = 0.3;
        row.NeedsAsOf = Now;
        set?.Invoke(row);
        c.SaveChanges();
    }

    private static async Task CareAsync()
    {
        using var db = new Db();
        using (var c = db.New())
        {
            (await PebbleService.GetOrCreateWalletAsync(c, G, Owner)).Balance = 10_000;
            (await PebbleService.GetOrCreateWalletAsync(c, G, Other)).Balance = 10_000;
            c.InventoryItems.Add(new InventoryItem { GuildId = G, UserId = Other, Key = ItemCatalog.FoodKey(PlynlingFood.Mushroom), Quantity = 2, DiscoveredAt = Now });
            await c.SaveChangesAsync();
        }
        var a = db.Add(Owner, "Alpha", Now.AddDays(-3));

        // The owner pays for a Truffle.
        using (var c = db.New())
            Expect((await db.Service(c).FeedAsync(a, Owner, PlynlingFood.Truffle, Now)).Outcome == CareOutcome.Done, "the owner feeds");
        var price = PlynlingLife.FeedPrice(PlynlingCatalog.Info(PlynlingFood.Truffle), true);
        Expect(db.Count(a, PlynlingCounters.Food(PlynlingFood.Truffle)) == 1, "a truffle eaten");
        Expect(db.Count(a, PlynlingCounters.MealSpentOwner) == price, $"spent by the owner: {db.Count(a, PlynlingCounters.MealSpentOwner)} for {price}");
        Expect(db.Carer(a, Owner)?.Meals == 1, "the owner is a carer");

        // Someone else serves from their pantry (two of the food for someone else's).
        Hungry(db, a);
        using (var c = db.New())
            Expect((await db.Service(c).FeedAsync(a, Other, PlynlingFood.Mushroom, Now)).Outcome == CareOutcome.Done, "a friend feeds");
        Expect(db.Count(a, PlynlingCounters.MealPantry) == 1, "from the pantry");
        Expect(db.Count(a, PlynlingCounters.MealSpentOthers) == 0, "the pantry costs no cailloux");
        Expect(db.Carer(a, Other)?.Meals == 1, "the friend is a carer");

        // A refusal counts nothing.
        var before = db.Count(a, PlynlingCounters.Food(PlynlingFood.Truffle));
        using (var c = db.New())
        {
            var w = await PebbleService.GetOrCreateWalletAsync(c, G, Other);
            w.Balance = 0;
            await c.SaveChangesAsync();
        }
        Hungry(db, a);
        using (var c = db.New())
            Expect((await db.Service(c).FeedAsync(a, Other, PlynlingFood.Truffle, Now)).Outcome == CareOutcome.TooPoor, "too poor");
        Expect(db.Count(a, PlynlingCounters.Food(PlynlingFood.Truffle)) == before, "a refused meal counts nothing");

        // Pets, by someone else and by the owner.
        using (var c = db.New())
            Expect((await db.Service(c).PetAsync(a, Other, Now)).Outcome == CareOutcome.Done, "a friend pets");
        using (var c = db.New())
            Expect((await db.Service(c).PetAsync(a, Owner, Now)).Outcome == CareOutcome.Done, "the owner pets");
        Expect(db.Count(a, PlynlingCounters.PetOthers) == 1 && db.Count(a, PlynlingCounters.PetOwner) == 1, "pets split by petter");
        Expect(db.Carer(a, Other)?.Pets == 1, "the friend's pet");

        // A free bath.
        Hungry(db, a, r => r.Hygiene = 0.2);
        using (var c = db.New())
            Expect((await db.Service(c).BathAsync(a, Owner, Now, free: true, soapOffered: false)).Outcome == CareOutcome.Done, "a bath");
        Expect(db.Count(a, PlynlingCounters.BathFree) == 1 && db.Count(a, PlynlingCounters.BathSoap) == 0, "a free bath");
    }

    // ---- Task 3: games, visits, forage -------------------------------------------------------------------------

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
        protected override double Sample() => value;
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next(int maxValue) => 0;
    }

    private static async Task PlayVisitAsync()
    {
        using var db = new Db();
        var a = db.Add(Owner, "Alpha", Now.AddDays(-3));
        var b = db.Add(Other, "Beta", Now.AddDays(-3));

        using (var c = db.New())
            Expect((await db.Service(c).FinishPlayAsync(a, Owner, PlynlingGame.Memory, true, 5, Now, new FixedRandom(0.99))).Plynling is not null, "a won game");
        using (var c = db.New())
            await db.Service(c).FinishPlayAsync(a, Owner, PlynlingGame.RockPaperScissors, false, 0, Now, new FixedRandom(0.99));
        Expect(db.Count(a, PlynlingCounters.Game(PlynlingGame.Memory)) == 1 && db.Count(a, PlynlingCounters.GameWon(PlynlingGame.Memory)) == 1, "memory won");
        Expect(db.Count(a, PlynlingCounters.Game(PlynlingGame.RockPaperScissors)) == 1
               && db.Count(a, PlynlingCounters.GameWon(PlynlingGame.RockPaperScissors)) == 0, "rps lost");
        Expect(db.Count(a, PlynlingCounters.EarnGame) == 5, "cailloux earned by a game: " + db.Count(a, PlynlingCounters.EarnGame));

        // A good visit, nothing found.
        VisitOutcome? o;
        using (var c = db.New())
            o = await db.Service(c).VisitAsync(a, b, Now, new VisitPlay(0, Confess: false), new FixedRandom(0.0), new FixedRandom(0.99));
        Expect(o is { GoodScene: true }, "a good visit");
        Expect(db.Count(a, PlynlingCounters.VisitMade) == 1, "the visitor's visit");
        Expect(db.Count(b, PlynlingCounters.VisitMade) == 0, "the host made no visit");
        Expect(db.Count(a, PlynlingCounters.VisitGood) == 1 && db.Count(b, PlynlingCounters.VisitGood) == 1, "a good scene for both");
        Expect(db.Count(a, PlynlingCounters.ConfessMade) == 0, "no confession");

        // A forage always finds something.
        using (var c = db.New())
            Expect((await db.Service(c).ForageAsync(G, Owner, Now, new Random(1))).Outcome == CareOutcome.Done, "a forage");
        Expect(db.Count(a, PlynlingCounters.Forage) == 1 && db.Count(a, PlynlingCounters.FindForage) == 1, "forage counted");
    }

    // ---- Task 4: what Settle finds -----------------------------------------------------------------------------

    private static long Pending(Plynling p, string key) => p.PendingCounters.Where(x => x.Key == key).Sum(x => x.Amount);

    private static void Pure()
    {
        // Sick since yesterday's 05:00 (Paris), well fed: the next 05:00 is a morning spent sick.
        var yesterday = PlynlingLife.MorningAt(AppTime.DayKey(Now));
        var sick = PlynlingLife.Create(G, Owner, "Malade", PlynlingSpecies.Cepe, PlynlingGender.Male, Now.AddDays(-10));
        sick.Hunger = sick.Happiness = sick.Hygiene = 1;
        sick.NeedsAsOf = Now;
        sick.LastMorningDay = AppTime.DayKey(Now);
        sick.SickSince = yesterday;
        PlynlingLife.Settle(sick, Now.AddHours(18));                 // past the next 05:00 Paris
        Expect(Pending(sick, PlynlingCounters.SickDays) == 1, "a morning spent sick: " + Pending(sick, PlynlingCounters.SickDays));

        var healthy = PlynlingLife.Create(G, Owner, "Sain", PlynlingSpecies.Cepe, PlynlingGender.Male, Now.AddDays(-10));
        healthy.Hunger = healthy.Happiness = healthy.Hygiene = 1;
        healthy.NeedsAsOf = Now;
        healthy.LastMorningDay = AppTime.DayKey(Now);
        PlynlingLife.Settle(healthy, Now.AddHours(18));
        Expect(Pending(healthy, PlynlingCounters.SickDays) == 0, "a healthy morning counts no sick day");

        // A freeze ended: its length, in seconds.
        var frozen = PlynlingLife.Create(G, Owner, "Glaçon", PlynlingSpecies.Cepe, PlynlingGender.Male, Now.AddDays(-10));
        PlynlingLife.Freeze(frozen, Now, byStaff: true);
        PlynlingLife.Thaw(frozen, Now.AddDays(2));
        Expect(Pending(frozen, PlynlingCounters.FrozenSeconds) == 172_800, "two days frozen: " + Pending(frozen, PlynlingCounters.FrozenSeconds));
    }

    private static async Task ThawAsync()
    {
        using var db = new Db();
        var a = db.Add(Owner, "Alpha", Now.AddDays(-3), r => r.Hunger = 1);
        using (var c = db.New())
            Expect((await db.Service(c).FreezeAsync(G, Owner, byStaff: true, Now)).Outcome == FreezeOutcome.Frozen, "staff freeze");
        using (var c = db.New())
            Expect((await db.Service(c).ThawAsync(G, Owner, byStaff: true, Now.AddDays(2))).Outcome == ThawOutcome.Thawed, "staff thaw");
        Expect(db.Count(a, PlynlingCounters.FrozenSeconds) == 172_800, "a stored freeze: " + db.Count(a, PlynlingCounters.FrozenSeconds));
    }

    // ---- Task 6: tab and duel lines (pure) ---------------------------------------------------------------------

    private static readonly StatFacts NoFacts = new(0, 0, 0, 0, new Dictionary<TraitKind, int>(), new Dictionary<MarkKind, int>(),
        new Dictionary<PlynlingBond, int>(), 0, null, 0, 0, 0, 0, 0);

    private static PlynlingStatSheet Sheet(string name, DateTimeOffset adopted, Action<Plynling>? set = null,
        Dictionary<string, long>? counters = null, StatFacts? facts = null, Dictionary<RankedStat, StatRank>? ranks = null)
    {
        var p = PlynlingLife.Create(G, Owner, name, PlynlingSpecies.Cepe, PlynlingGender.Female, adopted);
        set?.Invoke(p);
        return new PlynlingStatSheet(p, Now, PlynlingCounters.IsPartial(p), counters ?? new(), new List<PlynlingCarer>(),
            facts ?? NoFacts, ranks ?? new());
    }

    private static DuelLine? LineOf(IReadOnlyList<DuelLine> lines, string labelPart) =>
        lines.FirstOrDefault(l => l.Label.Contains(labelPart));

    private static void Lines()
    {
        Expect(PlynlingStatsUi.Rank(new StatRank(1, 9)) == "🥇", "gold");
        Expect(PlynlingStatsUi.Rank(new StatRank(4, 14)) == "4e / 14", "fourth: " + PlynlingStatsUi.Rank(new StatRank(4, 14)));

        var oldA = Now.AddDays(-30);
        var a = Sheet("Pwet", oldA, p => p.Meals = 412, facts: NoFacts with { TimesSick = 2 });
        var b = Sheet("Rex", oldA, p => p.Meals = 389);

        var care = PlynlingStatsUi.Duel(a, b, StatsTab.Care);
        var meals = LineOf(care, "Repas");
        Expect(meals is { Winner: -1 } && meals.A.Contains("**412**") && !meals.B.Contains("**"), "more meals win: " + meals);

        var life = PlynlingStatsUi.Duel(a, b, StatsTab.Life);
        Expect(LineOf(life, "malade") is { Winner: 1 }, "fewer sicknesses win: " + LineOf(life, "malade"));

        // A counter line is no contest between a partial count and a complete one.
        var newB = Sheet("Neuf", Now, counters: new() { [PlynlingCounters.BathFree] = 9 });
        var oldWithBaths = Sheet("Vieux", oldA, counters: new() { [PlynlingCounters.BathFree] = 5 });
        Expect(LineOf(PlynlingStatsUi.Duel(oldWithBaths, newB, StatsTab.Care), "Bains") is { Winner: 0 }, "partial against complete is no contest");
        var oldB = Sheet("Ancien", oldA, counters: new() { [PlynlingCounters.BathFree] = 9 });
        Expect(LineOf(PlynlingStatsUi.Duel(oldWithBaths, oldB, StatsTab.Care), "Bains") is { Winner: 1 }, "two partial counts compete");

        // Foods: one line each, left out when both are at 0.
        var morel = PlynlingCounters.Food(PlynlingFood.Morel);
        var fa = Sheet("A", oldA, counters: new() { [morel] = 3 });
        var fb = Sheet("B", oldA);
        var morelName = PlynlingCatalog.Info(PlynlingFood.Morel).Name;
        Expect(LineOf(PlynlingStatsUi.Duel(fa, fb, StatsTab.Care), morelName) is not null, "a food eaten by one side shows");
        Expect(LineOf(PlynlingStatsUi.Duel(fb, fb, StatsTab.Care), morelName) is null, "a food eaten by neither is left out");
        Expect(!PlynlingStatsUi.Lines(fb, StatsTab.Care).Any(l => l.Label.Contains(morelName)), "the single view skips a food at 0");

        // The footer counts the lines won.
        var won = Enumerable.Repeat(new DuelLine("x", "1", "0", -1), 7).Concat(Enumerable.Repeat(new DuelLine("y", "0", "1", 1), 4)).ToList();
        Expect(PlynlingStatsUi.DuelFooter(won, "Pwet", "Rex")?.Contains("mène 7 à 4") == true, "footer: " + PlynlingStatsUi.DuelFooter(won, "Pwet", "Rex"));
        Expect(PlynlingStatsUi.DuelFooter(new[] { new DuelLine("z", "a", "b", 0) }, "Pwet", "Rex") is null, "no contest, no footer");
        Expect(PlynlingStatsUi.SinceFooter(newB, StatsTab.Care) is null, "a complete sheet has no « depuis »");
        Expect(PlynlingStatsUi.SinceFooter(oldWithBaths, StatsTab.Care)?.Contains("26/11") == true, "a partial sheet says since when");

        // The gifts line never reads a sum of cailloux as part of a count of gifts.
        var gifted = Sheet("Gâté", oldA, counters: new()
            { [PlynlingCounters.GiftCailloux] = 10, [PlynlingCounters.GiftItems] = 2, [PlynlingCounters.GiftCaillouxTotal] = 340 });
        var giftLine = PlynlingStatsUi.Lines(gifted, StatsTab.Play).Single(l => l.Id == "gifts");
        Expect(!giftLine.Text.Contains("dont") && giftLine.Text.Contains("12") && giftLine.Text.Contains("340 cailloux"), "gifts line: " + giftLine.Text);

        // A death says its cause, starvation included.
        var starved = Sheet("Affamé", oldA, p => { p.DiedAt = Now.AddDays(-1); p.DeathCause = DeathCause.Starvation; });
        var diedLine = PlynlingStatsUi.Lines(starved, StatsTab.Life).Single(l => l.Id == "died");
        Expect(diedLine.Text.Contains("(faim)"), "a starved one's death: " + diedLine.Text);

        // A line only the second Plynling has keeps its place in the tab, not at the end.
        var duelLife = PlynlingStatsUi.Duel(a, starved, StatsTab.Life);
        var adoptedAt = duelLife.ToList().FindIndex(l => l.Label.Contains("Adoption"));
        Expect(adoptedAt >= 0 && duelLife[adoptedAt + 1].Label.Contains("Décès"), "the death date follows the adoption in a duel: "
            + string.Join(" | ", duelLife.Select(l => l.Label)));

        // A freeze still running counts in « Jours au frais ».
        var stillFrozen = Sheet("Glacé", oldA, p => { p.FrozenAt = Now.AddDays(-10); p.FrozenByStaff = true; },
            counters: new() { [PlynlingCounters.FrozenSeconds] = 2 * 86_400 });
        var frozenLine = PlynlingStatsUi.Lines(stillFrozen, StatsTab.Life).Single(l => l.Id == "frozendays");
        Expect(frozenLine.Text == "12", "two days past plus ten running: " + frozenLine.Text);

        // Every game has its own name (a new one must be named, not fall back to another's).
        var names = Enum.GetValues<PlynlingGame>().Select(PlynlingStatsUi.GameName).ToList();
        Expect(names.Distinct().Count() == names.Count, "two games share a name");

        // Labels that read well and never collide.
        var labels = Enum.GetValues<StatsTab>().SelectMany(t => PlynlingStatsUi.Lines(Fullest("x", false), t)).Select(l => l.Label).ToList();
        Expect(labels.Contains("🛌 Matins de maladie") && labels.Contains("🥫 Repas du garde-manger"), "care and health labels");
        Expect(labels.Count(l => l.Contains("Souvenirs")) <= 1, "« Souvenirs » twice: " + string.Join(", ", labels.Where(l => l.Contains("Souvenirs"))));

        // Every /plynling command that answers through Context.Interaction (a card through CardSend, a modal) must be
        // left alone by the Premium prank (docs/agents/personality/easter-eggs.md, rule 2). Keep this list in step with
        // the module: a new command answering that way joins it.
        foreach (var method in new[] { "AdoptAsync", "AbandonAsync", "PlayAsync", "VisitAsync", "JournalAsync", "StatsAsync",
                     "ViewAsync", "FreezeAsync", "ThawAsync", "WardrobeAsync", "PassionAsync" })
        {
            var m = typeof(ProjectSYNCS.Commands.PlynlingModule).GetMethod(method);
            Expect(m is not null && m.GetCustomAttributes(typeof(NoPremiumPrankAttribute), true).Length == 1,
                $"/plynling {method} carries [NoPremiumPrank]");
        }

        // A rank shows on the single view.
        var ranked = Sheet("Classé", oldA, p => p.Meals = 412, ranks: new() { [RankedStat.Meals] = new StatRank(1, 9) });
        Expect(PlynlingStatsUi.Lines(ranked, StatsTab.Care).Select(l => PlynlingStatsUi.Render(l, ranked.Partial)).Any(t => t.Contains("Repas") && t.Contains("🥇")),
            "the meals line carries its medal");
    }

    // ---- Task 7: the cards at their fullest --------------------------------------------------------------------

    private static int Count(Discord.IMessageComponent c) => 1 + c switch
    {
        Discord.ContainerComponent x => x.Components.Sum(Count),
        Discord.SectionComponent x => x.Components.Sum(Count) + (x.Accessory is null ? 0 : Count(x.Accessory)),
        Discord.ActionRowComponent x => x.Components.Sum(Count),
        _ => 0,
    };

    private static int TextLength(Discord.IMessageComponent c) => c switch
    {
        Discord.ContainerComponent x => x.Components.Sum(TextLength),
        Discord.SectionComponent x => x.Components.Sum(TextLength),
        Discord.TextDisplayComponent x => x.Content.Length,
        _ => 0,
    };

    private static IEnumerable<string> CustomIds(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
        {
            switch (c)
            {
                case Discord.ContainerComponent x: foreach (var id in CustomIds(x.Components)) yield return id; break;
                case Discord.ActionRowComponent x: foreach (var id in CustomIds(x.Components)) yield return id; break;
                case Discord.ButtonComponent { CustomId: { } id }: yield return id; break;
            }
        }
    }

    private static IEnumerable<string> Thumbnails(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
        {
            switch (c)
            {
                case Discord.ContainerComponent x: foreach (var u in Thumbnails(x.Components)) yield return u; break;
                case Discord.SectionComponent { Accessory: Discord.ThumbnailComponent t }: yield return t.Media.Url; break;
            }
        }
    }

    private static PlynlingStatSheet Fullest(string name, bool dead)
    {
        const long Big = 9_999_999;
        var counters = PlynlingCounters.All.ToDictionary(k => k, _ => Big);
        var facts = new StatFacts((int)Big, (int)Big, (int)Big, (int)Big,
            Enum.GetValues<TraitKind>().ToDictionary(k => k, _ => (int)Big), Enum.GetValues<MarkKind>().ToDictionary(k => k, _ => (int)Big),
            Enum.GetValues<PlynlingBond>().ToDictionary(k => k, _ => (int)Big), (int)Big, new string('*', 32), (int)Big,
            (int)Big, (int)Big, (int)Big, (int)Big);
        var ranks = Enum.GetValues<RankedStat>().ToDictionary(r => r, _ => new StatRank(999, 999));
        var p = PlynlingLife.Create(G, ulong.MaxValue, name, PlynlingSpecies.Cepe, PlynlingGender.Female, Now.AddDays(-400));
        p.Meals = p.Pets = p.Plays = p.PlaysWon = p.Visits = p.Hosted = p.FedByOthers = p.Drills = p.Score = Big;
        p.RpgXp = Big;
        p.Stress = 400;
        if (dead) { p.DiedAt = Now.AddDays(-1); p.DeathCause = DeathCause.Illness; }
        var carers = new List<PlynlingCarer> { new() { UserId = ulong.MaxValue - 1, Meals = Big, Pets = Big } };
        return new PlynlingStatSheet(p, Now, true, counters, carers, facts, ranks);
    }

    private static void Cards()
    {
        var a = Fullest(new string('*', 32), dead: false);
        var b = Fullest(new string('_', 32), dead: true);
        foreach (var tab in Enum.GetValues<StatsTab>())
        {
            foreach (var (what, card) in new[] { ("single", PlynlingStatsCards.Build(a, tab)), ("dead", PlynlingStatsCards.Build(b, tab)),
                         ("duel", PlynlingStatsCards.BuildDuel(a, b, tab)) })
            {
                var count = card.Components.Sum(Count);
                var text = card.Components.Sum(TextLength);
                Expect(count <= 40, $"{what} {tab}: {count} components");
                Expect(text <= 4000, $"{what} {tab}: {text} characters");
                var ids = CustomIds(card.Components).ToList();
                Expect(ids.Distinct().Count() == ids.Count && ids.Count == 5, $"{what} {tab}: custom-ids {string.Join(" ", ids)}");
            }
        }
        var deadThumb = Thumbnails(PlynlingStatsCards.Build(b, StatsTab.Life).Components).FirstOrDefault();
        Expect(deadThumb == PlynlingArt.Memorial(b.Plynling.Species, PlynlingCatalog.MemorialTier(b.Plynling)), "a dead one shows its tombstone: " + deadThumb);
        Expect(CustomIds(PlynlingStatsCards.Build(a, StatsTab.Care).Components).Contains($"plst:tab:{a.Plynling.Id}:{(int)StatsTab.Care}"), "the tab verb");
        Expect(CustomIds(PlynlingStatsCards.BuildDuel(a, b, StatsTab.Play).Components)
            .Contains($"plst:duel:{a.Plynling.Id}:{b.Plynling.Id}:{(int)StatsTab.Play}"), "the duel verb");
    }

    // ---- Task 5: the sheet and its ranks -----------------------------------------------------------------------

    private static async Task<PlynlingStatSheet> SheetOf(Db db, int id)
    {
        using var c = db.New();
        var service = db.Service(c);
        var p = (await service.GetByIdAsync(id, Now))!;
        return await service.GetStatSheetAsync(p, Now);
    }

    private static async Task SheetAsync()
    {
        using var db = new Db();
        var a = db.Add(Owner, "Alpha", Now.AddDays(-30), r => { r.Meals = 10; r.Drills = 4; });
        var b = db.Add(Other, "Beta", Now.AddDays(-30), r => { r.Meals = 10; r.DiedAt = Now.AddDays(-1); r.DeathAnnounced = true; });
        var c3 = db.Add(5, "Gamma", Now.AddDays(-30), r => r.Meals = 3);
        var m = db.Add(Bot, PlynlingMascot.Name, Now.AddDays(-30), r => r.Meals = 99);
        using (var c = db.New())
        {
            c.PlynlingJournalEntries.Add(new PlynlingJournalEntry { PlynlingId = a, Kind = JournalKind.FellSick, At = Now.AddDays(-5) });
            c.PlynlingJournalEntries.Add(new PlynlingJournalEntry { PlynlingId = a, Kind = JournalKind.FellSick, At = Now.AddDays(-2) });
            c.PlynlingCounters.Add(new PlynlingCounter { PlynlingId = a, Key = PlynlingCounters.BathFree, Value = 7 });
            await c.SaveChangesAsync();
        }

        var sa = await SheetOf(db, a);
        Expect(sa.Ranks.GetValueOrDefault(RankedStat.Meals) == new StatRank(1, 3), "A's meals rank: " + sa.Ranks.GetValueOrDefault(RankedStat.Meals));
        Expect(sa.Facts.TimesSick == 2, "A sick twice: " + sa.Facts.TimesSick);
        Expect(sa.Count(PlynlingCounters.BathFree) == 7 && sa.Count("nothing") == 0, "A's counters");
        Expect(sa.Partial, "adopted before Since is partial");

        var sb = await SheetOf(db, b);
        Expect(sb.Ranks.GetValueOrDefault(RankedStat.Meals) == new StatRank(1, 3), "a dead one ranks, ties share: " + sb.Ranks.GetValueOrDefault(RankedStat.Meals));
        Expect(!sb.Ranks.ContainsKey(RankedStat.Drills), "0 drills: no rank");

        var sc = await SheetOf(db, c3);
        Expect(sc.Ranks.GetValueOrDefault(RankedStat.Meals) == new StatRank(3, 3), "C third: " + sc.Ranks.GetValueOrDefault(RankedStat.Meals));
        Expect(sa.Ranks.GetValueOrDefault(RankedStat.Drills) == new StatRank(1, 3), "A's drills");

        var sm = await SheetOf(db, m);
        Expect(sm.Ranks.Count == 0, "the mascot is never ranked");

        // Badges count like the journal: catalogue keys only, so « n / total » never disagrees with it.
        using (var c = db.New())
        {
            c.PlynlingBadges.Add(new PlynlingBadge { PlynlingId = a, Key = PlynlingBadges.All[0].Key, EarnedAt = Now });
            c.PlynlingBadges.Add(new PlynlingBadge { PlynlingId = a, Key = "retired.badge", EarnedAt = Now });
            c.PlynlingBadges.Add(new PlynlingBadge { PlynlingId = b, Key = "retired.badge", EarnedAt = Now });
            await c.SaveChangesAsync();
        }
        Expect((await SheetOf(db, a)).Facts.Badges == 1, "an unknown badge key is not counted");
        Expect(!(await SheetOf(db, b)).Ranks.ContainsKey(RankedStat.Badges), "only unknown keys: no badge rank");
    }
}
