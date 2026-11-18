using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 53. Bribes and the dedication auction: the bribe price and its week, the chouchou and its cache,
// erasing a shame hit, the auction's days, bids and refunds, the dedication in the morning hello.
// Against a real in-memory SQLite, so every query must translate.
public static class BribeChecks
{
    private static Action<string> _fail = _ => { };
    private const ulong G = 1000, A = 42, B = 43, C = 44;
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("bribes: " + what);
    }

    // BotResponses is internal: its pools and ids are read by reflection, like the rest of the checks.
    private static readonly Type Br = typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;
    private static T Field<T>(string name) =>
        (T)(Br.GetField(name)?.GetValue(null) ?? throw new InvalidOperationException("no BotResponses." + name));

    // A fresh database per scenario; the connection keeps the in-memory database alive.
    private sealed class Db : IDisposable
    {
        public readonly Microsoft.Data.Sqlite.SqliteConnection Conn = new("DataSource=:memory:");
        private readonly DbContextOptions<AppDbContext> _opts;

        public Db()
        {
            Conn.Open();
            _opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(Conn).Options;
            using var c = New();
            c.Database.EnsureCreated();
        }

        public AppDbContext New() => new(_opts);

        public void Wallet(ulong user, long balance)
        {
            using var c = New();
            c.PebbleWallets.Add(new PebbleWallet { GuildId = G, UserId = user, Balance = balance });
            c.SaveChanges();
        }

        public long Balance(ulong user)
        {
            using var c = New();
            return c.PebbleWallets.Single(w => w.UserId == user).Balance;
        }

        public IServiceProvider Services() =>
            new ServiceCollection().AddDbContext<AppDbContext>(o => o.UseSqlite(Conn)).BuildServiceProvider();

        public BribeService Bribes(AppDbContext c) => new(c, new ChouchouBook(Services()));

        public void Dispose() => Conn.Dispose();
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        Price();
        try { await FavouriteAsync(); }
        catch (Exception ex) { Expect(false, "favourite threw: " + ex.Message); }
        try { await ChouchouBookAsync(); }
        catch (Exception ex) { Expect(false, "chouchou book threw: " + ex.Message); }
        try { await ForgiveAsync(); }
        catch (Exception ex) { Expect(false, "forgive threw: " + ex.Message); }
        Chouchou();
        ShoutoutDays();
        Dedication();
        try { await BidsAsync(); }
        catch (Exception ex) { Expect(false, "bids threw: " + ex.Message); }
        try { await DueAsync(); }
        catch (Exception ex) { Expect(false, "due dedications threw: " + ex.Message); }
    }

    private static void Price()
    {
        var w = CosmeticCatalog.WeekKey(Now);
        Expect(Bribes.Price(w, 0, w) == 100, "first bribe of the week: " + Bribes.Price(w, 0, w));
        Expect(Bribes.Price(w, 2, w) == 400, "third bribe of the week: " + Bribes.Price(w, 2, w));
        Expect(Bribes.Price(w - 1, 5, w) == 100, "last week's bribes count this week");
        Expect(Bribes.Price(w, 99, w) > 0, "the price overflows");
    }

    private static async Task FavouriteAsync()
    {
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            BribeResult r1, r2, r3;
            using (var c = db.New()) r1 = await db.Bribes(c).BuyFavouriteAsync(G, A, Now);
            using (var c = db.New()) r2 = await db.Bribes(c).BuyFavouriteAsync(G, A, Now.AddHours(2));
            using (var c = db.New()) { c.ShameRecords.Add(new ShameRecord { GuildId = G, UserId = A, MeanHits = 1 }); c.SaveChanges(); }
            using (var c = db.New()) r3 = await db.Bribes(c).ForgiveShameAsync(G, A, ShameCategory.Mean, Now.AddHours(3));
            Expect(r1.Outcome == BribeOutcome.Done && r1.Price == 100 && r1.NextPrice == 200 && r1.FavouriteUntil == Now.AddHours(24), $"first favourite: {r1}");
            Expect(r2.Outcome == BribeOutcome.Done && r2.Price == 200 && r2.FavouriteUntil == Now.AddHours(48), $"second favourite extends: {r2}");
            Expect(r3.Outcome == BribeOutcome.Done && r3.Price == 400 && r3.Balance == 300, $"a forgive shares the week's price: {r3}");
            Expect(db.Balance(A) == 300, "balance after three bribes: " + db.Balance(A));
            using var c2 = db.New();
            var logged = c2.EconomyDailyStats.SingleOrDefault(s => s.Metric == EconomyLog.SpendBribe && s.Day == AppTime.DayKey(Now))?.Value;
            Expect(logged == 700, "spend.bribe today: " + logged);
            Expect(!c2.MemberXps.Any(x => x.UserId == A && x.CaillouxSpent > 0), "bribes counted toward Mécène");
        }

        // The week turns at Monday 00:00 Paris: Sunday 23:30 and Monday 00:30 both cost 100.
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            BribeResult sunday, monday;
            using (var c = db.New()) sunday = await db.Bribes(c).BuyFavouriteAsync(G, A, new DateTimeOffset(2026, 11, 22, 22, 30, 0, TimeSpan.Zero));
            using (var c = db.New()) monday = await db.Bribes(c).BuyFavouriteAsync(G, A, new DateTimeOffset(2026, 11, 22, 23, 30, 0, TimeSpan.Zero));
            Expect(sunday.Price == 100 && monday.Price == 100, $"week rollover: {sunday.Price} then {monday.Price}");
        }

        // Papa and Tata are family already: refused, nothing taken.
        using (var db = new Db())
        {
            var tata = Field<ulong>("TataId");
            db.Wallet(AvailabilityService.OwnerId, 1000);
            db.Wallet(tata, 1000);
            BribeResult papa, aunt;
            using (var c = db.New()) papa = await db.Bribes(c).BuyFavouriteAsync(G, AvailabilityService.OwnerId, Now);
            using (var c = db.New()) aunt = await db.Bribes(c).BuyFavouriteAsync(G, tata, Now);
            Expect(papa.Outcome == BribeOutcome.Family && aunt.Outcome == BribeOutcome.Family, $"family: {papa.Outcome} {aunt.Outcome}");
            Expect(db.Balance(AvailabilityService.OwnerId) == 1000 && db.Balance(tata) == 1000, "family charged");
        }
    }

    private static async Task ChouchouBookAsync()
    {
        using var db = new Db();
        using (var c = db.New())
        {
            c.PebbleWallets.AddRange(
                new PebbleWallet { GuildId = G, UserId = A, ChouchouUntil = Now.AddHours(5) },
                new PebbleWallet { GuildId = G, UserId = B, ChouchouUntil = Now.AddHours(-1) });
            c.SaveChanges();
        }
        // A fresh book, as after a restart: it reads the wallets.
        var book = new ChouchouBook(db.Services());
        Expect(await book.IsFavouriteAsync(G, A, Now), "an active chouchou forgotten after a restart");
        Expect(!await book.IsFavouriteAsync(G, B, Now), "an expired chouchou still warm");
        Expect(!await book.IsFavouriteAsync(G, A, Now.AddHours(6)), "a chouchou past its end still warm");
        book.Set(G, C, Now.AddHours(24));
        Expect(await book.IsFavouriteAsync(G, C, Now), "Set not seen");
        // Two buys whose Sets land out of order: the later end stays.
        book.Set(G, C, Now.AddHours(48));
        book.Set(G, C, Now.AddHours(24));
        Expect(await book.IsFavouriteAsync(G, C, Now.AddHours(30)), "a late Set shortened the chouchou");
    }

    private static async Task ForgiveAsync()
    {
        int Day(int daysAgo) => AppTime.DayKey(Now.AddDays(-daysAgo));

        // The most recent bucket holding a hit loses it, with the record.
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            using (var c = db.New())
            {
                c.ShameRecords.Add(new ShameRecord { GuildId = G, UserId = A, MeanHits = 3 });
                c.ShameDailyStats.AddRange(
                    new ShameDailyStat { GuildId = G, UserId = A, Day = Day(10), MeanHits = 2 },
                    new ShameDailyStat { GuildId = G, UserId = A, Day = Day(3), MeanHits = 1 },
                    new ShameDailyStat { GuildId = G, UserId = A, Day = Day(1), ShoutHits = 4 });
                c.SaveChanges();
            }
            BribeResult r;
            using (var c = db.New()) r = await db.Bribes(c).ForgiveShameAsync(G, A, ShameCategory.Mean, Now);
            using var c2 = db.New();
            var rec = c2.ShameRecords.Single();
            var buckets = c2.ShameDailyStats.ToDictionary(s => s.Day);
            Expect(r.Outcome == BribeOutcome.Done && rec.MeanHits == 2, $"forgive: {r.Outcome}, record {rec.MeanHits}");
            Expect(buckets[Day(3)].MeanHits == 0 && buckets[Day(10)].MeanHits == 2 && buckets[Day(1)].ShoutHits == 4,
                $"forgive buckets: {buckets[Day(10)].MeanHits} / {buckets[Day(3)].MeanHits} / shout {buckets[Day(1)].ShoutHits}");
        }

        // Hits from before the buckets existed: the record alone.
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            using (var c = db.New()) { c.ShameRecords.Add(new ShameRecord { GuildId = G, UserId = A, ShoutHits = 1 }); c.SaveChanges(); }
            BribeResult r;
            using (var c = db.New()) r = await db.Bribes(c).ForgiveShameAsync(G, A, ShameCategory.Shout, Now);
            using var c2 = db.New();
            Expect(r.Outcome == BribeOutcome.Done && c2.ShameRecords.Single().ShoutHits == 0 && db.Balance(A) == 900, $"pre-bucket forgive: {r}");
        }

        // Nothing to erase: refused, nothing taken, nothing logged.
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            using (var c = db.New()) { c.ShameRecords.Add(new ShameRecord { GuildId = G, UserId = A, MeanHits = 5 }); c.SaveChanges(); }
            BribeResult r;
            using (var c = db.New()) r = await db.Bribes(c).ForgiveShameAsync(G, A, ShameCategory.Perfidy, Now);
            using var c2 = db.New();
            Expect(r.Outcome == BribeOutcome.NothingToForgive && db.Balance(A) == 1000, $"nothing to forgive: {r}");
            Expect(!c2.EconomyDailyStats.Any(s => s.Metric == EconomyLog.SpendBribe), "a refused bribe logged");
        }

        // Too poor: refused, the record untouched.
        using (var db = new Db())
        {
            db.Wallet(A, 50);
            using (var c = db.New()) { c.ShameRecords.Add(new ShameRecord { GuildId = G, UserId = A, AbandonHits = 2 }); c.SaveChanges(); }
            BribeResult r;
            using (var c = db.New()) r = await db.Bribes(c).ForgiveShameAsync(G, A, ShameCategory.Abandon, Now);
            using var c2 = db.New();
            Expect(r.Outcome == BribeOutcome.TooPoor && r.Price == 100 && db.Balance(A) == 50 && c2.ShameRecords.Single().AbandonHits == 2, $"too poor: {r}");
        }

        // A hit landing while a bribe erases one: the tracker's context read the record before the
        // erase, and its add must not write that stale count back over it (paid, hit back).
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            using (var c = db.New()) { c.ShameRecords.Add(new ShameRecord { GuildId = G, UserId = A, MeanHits = 1 }); c.SaveChanges(); }
            using var tracker = db.New();
            _ = tracker.ShameRecords.Single();
            using (var c = db.New()) await db.Bribes(c).ForgiveShameAsync(G, A, ShameCategory.Mean, Now);
            await new ShameService(tracker).AddMeanHitsAsync(G, A, 1);
            using var c2 = db.New();
            Expect(c2.ShameRecords.Single().MeanHits == 1, $"a hit overwrote the erase: record {c2.ShameRecords.Single().MeanHits}, want 1");
        }

        Expect(ShameCategories.Title(ShameCategory.Mean) == "Le Malfaisant" && ShameCategories.Title(ShameCategory.Perfidy) == "Le Perfide"
            && ShameCategories.Title(ShameCategory.Shout) == "L'Hystérique" && ShameCategories.Title(ShameCategory.Abandon) == "L'Indigne", "shame titles");
    }
    // The chouchou's side of the chat: her reactions, the /bribe replies, the pools' house rules.
    private static void Chouchou()
    {
        // ReactionService is internal too.
        var choose = typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.ReactionService")!
            .GetMethod("ChooseReactionPool", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, new[] { typeof(string), typeof(ulong), typeof(bool) });
        string[]? ChoosePool(string content, ulong author, bool chouchou) =>
            choose is null ? null : (string[]?)choose.Invoke(null, new object[] { content, author, chouchou });
        Expect(choose is not null, "no ReactionService.ChooseReactionPool(string, ulong, bool)");
        var chouchou = Field<string[]>("ChouchouReactions");
        Expect(ChoosePool("salut", 77, true) == chouchou, "a chouchou's hello gets no chouchou reaction");
        Expect(ChoosePool("t'es nul", 77, true) == Field<string[]>("MeanReactions"), "a chouchou's insult gets a warm reaction");
        Expect(ChoosePool("salut", AvailabilityService.OwnerId, true) == Field<string[]>("OwnerReactions"), "Papa lost his reactions");
        Expect(ChoosePool("salut", 77, false) == Field<string[]>("GreetingReactions"), "a plain hello changed");
        Expect(chouchou.Length >= 5 && chouchou.All(r => EmoteMarkup.Parse(r) is not null), "ChouchouReactions: fewer than 5, or one does not parse");

        var until = Now.AddHours(24);
        var fav = ProjectSYNCS.Commands.BribeModule.Reply(new BribeResult(BribeOutcome.Done, 200, 800, 400, until), A, null);
        Expect(fav.Contains(PebbleEconomy.Cailloux(200)) && fav.Contains("24 h") && fav.Contains(PebbleEconomy.Cailloux(400)), "favourite reply: " + fav);
        // A second buy extends to 48 h: the reply names the end and the 24 h added, never « pour 24 h ».
        Expect(!fav.Contains("pour 24 h") && fav.Contains("+24 h"), "favourite reply reads wrong on an extension: " + fav);
        var forgive = ProjectSYNCS.Commands.BribeModule.Reply(new BribeResult(BribeOutcome.Done, 100, 900, 200, null), A, ShameCategory.Mean);
        Expect(forgive.Contains(ShameCategories.Title(ShameCategory.Mean)) && forgive.Contains(PebbleEconomy.Cailloux(100)), "forgive reply: " + forgive);
        var poor = ProjectSYNCS.Commands.BribeModule.Reply(new BribeResult(BribeOutcome.TooPoor, 400, 120, 400, null), A, null);
        Expect(poor.Contains(PebbleEconomy.Cailloux(400)) && poor.Contains(PebbleEconomy.Cailloux(120)), "too poor reply: " + poor);
        var family = ProjectSYNCS.Commands.BribeModule.Reply(new BribeResult(BribeOutcome.Family, 0, 0, 0, null), A, null);
        var nothing = ProjectSYNCS.Commands.BribeModule.Reply(new BribeResult(BribeOutcome.NothingToForgive, 100, 500, 100, null), A, ShameCategory.Shout);
        Expect(family.Length > 0 && nothing.Contains(ShameCategories.Title(ShameCategory.Shout)) && family != nothing, $"family / nothing replies: {family} | {nothing}");

        // Paid warmth, said as such: never the family's words.
        var family_words = new[] { "papa", "tata", "famille", "ma fille", "mon enfant", "nièce" };
        foreach (var pool in new[] { "ChouchouGreetings", "ChouchouReplies" })
        {
            var lines = Field<string[]>(pool);
            Expect(lines.Length >= 12, $"{pool}: {lines.Length} lines");
            foreach (var l in lines)
                if (family_words.FirstOrDefault(w => l.Contains(w, StringComparison.OrdinalIgnoreCase)) is { } w)
                    Expect(false, $"{pool}: family word « {w} »: {l}");
        }

        // Voice rule 1: the person she talks to (or pings) is never put in the masculine. These
        // pools address a real member, so the agreements that slipped in once stay out.
        var gendered = new System.Text.RegularExpressions.Regex(@"\b(officiel|roi|clients|il a payé)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (var pool in new[] { "ChouchouGreetings", "ChouchouReplies", "DedicationLines", "DedicationCredits" })
            foreach (var l in Field<string[]>(pool))
                if (gendered.Match(l) is { Success: true } m)
                    Expect(false, $"{pool}: gendered « {m.Value} »: {l}");
    }
    // The auction's day: before 20:00 Paris a bid is for tomorrow, from 20:00 for the day after,
    // winter and summer.
    private static void ShoutoutDays()
    {
        Expect(Shoutouts.OpenDay(new DateTimeOffset(2026, 11, 18, 18, 59, 0, TimeSpan.Zero)) == 20261119, "19:59 Paris in winter is for tomorrow");
        Expect(Shoutouts.OpenDay(new DateTimeOffset(2026, 11, 18, 19, 0, 0, TimeSpan.Zero)) == 20261120, "20:00 Paris in winter is for the day after");
        Expect(Shoutouts.OpenDay(new DateTimeOffset(2026, 7, 1, 17, 59, 0, TimeSpan.Zero)) == 20260702, "19:59 Paris in summer is for tomorrow");
        Expect(Shoutouts.OpenDay(new DateTimeOffset(2026, 7, 1, 18, 0, 0, TimeSpan.Zero)) == 20260703, "20:00 Paris in summer is for the day after");
        Expect(Shoutouts.OpenDay(new DateTimeOffset(2026, 12, 31, 12, 0, 0, TimeSpan.Zero)) == 20270101, "the year turns");
        Expect(Shoutouts.ClosesAt(20261119) == new DateTimeOffset(2026, 11, 18, 19, 0, 0, TimeSpan.Zero), "closes at " + Shoutouts.ClosesAt(20261119));
        Expect(Shoutouts.Minimum(null) == 50 && Shoutouts.Minimum(100) == 110, "minimums");

        var tooLow = ProjectSYNCS.Commands.ShoutoutModule.BidReply(new BidResult(BidOutcome.TooLow, 20261119, 49, 50, 1000, null, 0), A);
        Expect(tooLow.Contains(PebbleEconomy.Cailloux(50)), "too low does not name the minimum: " + tooLow);
        var lead = ProjectSYNCS.Commands.ShoutoutModule.BidReply(new BidResult(BidOutcome.Leading, 20261119, 150, 160, 850, A, 150), B);
        Expect(lead.Contains($"<@{A}>") && lead.Contains($"<@{B}>") && lead.Contains(PebbleEconomy.Cailloux(150)), "lead reply: " + lead);
        var poor = ProjectSYNCS.Commands.ShoutoutModule.BidReply(new BidResult(BidOutcome.TooPoor, 20261119, 5000, 50, 120, null, 5000), A);
        Expect(poor.Contains(PebbleEconomy.Cailloux(120)), "too poor does not name the balance: " + poor);
        // Raising your own lead costs only the difference: the refusal names that, not the whole bid.
        var poorRaise = ProjectSYNCS.Commands.ShoutoutModule.BidReply(new BidResult(BidOutcome.TooPoor, 20261119, 300, 210, 40, null, 100), A);
        Expect(poorRaise.Contains(PebbleEconomy.Cailloux(100)) && poorRaise.Contains(PebbleEconomy.Cailloux(40)), "too poor to raise does not name the difference: " + poorRaise);
        var open = ProjectSYNCS.Commands.ShoutoutModule.StatusText(new ShoutoutStatus(20261119, Shoutouts.ClosesAt(20261119), null, 0, 50, 0));
        Expect(open.Contains(PebbleEconomy.Cailloux(50)), "an empty auction does not name the opening bid: " + open);
    }

    private static async Task BidsAsync()
    {
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            db.Wallet(B, 1000);
            long Held() { using var c = db.New(); return c.ShoutoutBids.Sum(s => (long?)s.Amount) ?? 0; }
            void Conserved(string step) => Expect(db.Balance(A) + db.Balance(B) + Held() == 2000, $"cailloux created or lost after {step}");
            async Task<BidResult> Bid(ulong who, long amount) { using var c = db.New(); return await new ShoutoutService(c).BidAsync(G, who, amount, Now); }

            var r = await Bid(A, 49);
            Expect(r.Outcome == BidOutcome.TooLow && r.Minimum == 50 && db.Balance(A) == 1000, $"A 49: {r}");
            r = await Bid(A, 100);
            Expect(r.Outcome == BidOutcome.Leading && r.ForDay == 20261119 && db.Balance(A) == 900, $"A 100: {r}");
            Conserved("A 100");
            r = await Bid(B, 105);
            Expect(r.Outcome == BidOutcome.TooLow && r.Minimum == 110, $"B 105: {r}");
            r = await Bid(B, 150);
            Expect(r.Outcome == BidOutcome.Leading && r.Outbid == A && db.Balance(A) == 1000 && db.Balance(B) == 850, $"B 150: {r}");
            Conserved("B 150");
            r = await Bid(B, 200);
            Expect(r.Outcome == BidOutcome.Leading && r.Outbid is null && db.Balance(B) == 800, $"B raises to 200: {r}, balance {db.Balance(B)}");
            Conserved("B 200");
            r = await Bid(A, 5000);
            Expect(r.Outcome == BidOutcome.TooPoor && r.Balance == 1000 && r.Cost == 5000, $"A 5000: {r}");
            r = await Bid(B, 2000);
            Expect(r.Outcome == BidOutcome.TooPoor && r.Cost == 1800 && db.Balance(B) == 800, $"B raises past its purse: {r}");
            using var c2 = db.New();
            Expect(!c2.EconomyDailyStats.Any(s => s.Metric == EconomyLog.SpendShoutout) && !c2.MemberXps.Any(x => x.CaillouxSpent > 0),
                "a held bid logged or counted as spent");
            var status = await new ShoutoutService(c2).StatusAsync(G, Now);
            Expect(status.ForDay == 20261119 && status.Leader == B && status.Amount == 200 && status.Minimum == 210, $"status: {status}");
        }

        // Two bids racing on one leader: the slower re-checks against the new one and still leads.
        using (var db = new Db())
        {
            db.Wallet(A, 1000);
            db.Wallet(B, 800);
            db.Wallet(C, 1000);
            using (var c = db.New())
            {
                c.ShoutoutBids.Add(new ShoutoutBid { GuildId = G, ForDay = 20261119, UserId = B, Amount = 200, PlacedAt = Now });
                c.SaveChanges();
            }
            using var stale = db.New();
            _ = stale.ShoutoutBids.Single();
            BidResult a, cr;
            using (var c = db.New()) a = await new ShoutoutService(c).BidAsync(G, A, 250, Now);
            cr = await new ShoutoutService(stale).BidAsync(G, C, 260, Now);
            using var c2 = db.New();
            var row = c2.ShoutoutBids.Single();
            Expect(a.Outcome == BidOutcome.Leading && cr.Outcome == BidOutcome.Leading && cr.Outbid == A && row.UserId == C && row.Amount == 260,
                $"race: A {a.Outcome}, C {cr.Outcome} outbid {cr.Outbid}, row {row.UserId}/{row.Amount}");
            Expect(db.Balance(A) == 1000 && db.Balance(B) == 1000 && db.Balance(C) == 740, $"race balances: {db.Balance(A)} {db.Balance(B)} {db.Balance(C)}");
        }
    }

    // Won dedications wait for a hello: today's winner first, then the carried ones oldest first, each
    // posted once, settled once.
    private static async Task DueAsync()
    {
        using var db = new Db();
        var d = AppTime.DayKey(Now);
        int Shift(int days) => AppTime.DayKey(Now.AddDays(days));
        using (var c = db.New())
        {
            c.ShoutoutBids.AddRange(
                new ShoutoutBid { GuildId = G, ForDay = Shift(-1), UserId = B, Amount = 70, PlacedAt = Now },
                new ShoutoutBid { GuildId = G, ForDay = Shift(-2), UserId = A, Amount = 90, PlacedAt = Now },
                new ShoutoutBid { GuildId = G, ForDay = d, UserId = C, Amount = 60, PlacedAt = Now },
                new ShoutoutBid { GuildId = G, ForDay = Shift(1), UserId = C, Amount = 999, PlacedAt = Now });
            c.SaveChanges();
        }
        ShoutoutBid? due;
        using (var c = db.New()) due = await new ShoutoutService(c).NextDueAsync(G, Now);
        // Today's winner keeps the day promised; the carried ones wait for a morning nobody won.
        Expect(due?.ForDay == d && due.UserId == C, $"first due: {due?.ForDay}, not today's");
        using (var c = db.New()) await new ShoutoutService(c).MarkShownAsync(due!.Id, Now);
        using (var c = db.New()) await new ShoutoutService(c).MarkShownAsync(due!.Id, Now);
        using (var c = db.New()) due = await new ShoutoutService(c).NextDueAsync(G, Now.AddDays(1));
        Expect(due?.ForDay == Shift(1) && due.UserId == C, $"tomorrow's winner first: {due?.ForDay}");
        using (var c = db.New()) due = await new ShoutoutService(c).NextDueAsync(G, Now);
        Expect(due?.ForDay == Shift(-2) && due.UserId == A, $"oldest carried next: {due?.ForDay}");
        using (var c = db.New())
        {
            var logged = c.EconomyDailyStats.SingleOrDefault(s => s.Metric == EconomyLog.SpendShoutout && s.Day == d)?.Value;
            Expect(logged == 60, "spend.shoutout after two marks: " + logged);
            Expect(!c.MemberXps.Any(x => x.UserId == C && x.CaillouxSpent > 0), "the dedication counted toward Mécène");
            Expect(c.ShoutoutBids.Single(s => s.UserId == C && s.ForDay == d).ShownDay == d, "ShownDay not stamped");
            var status = await new ShoutoutService(c).StatusAsync(G, Now);
            Expect(status.Queued == 2, $"queued: {status.Queued}");
        }
    }
    // The dedication in the morning hello, two shapes: in place of the hello line, or (60 % of those
    // days, by date) the classic hello ending on a credit line; the fun fact kept, the winner pinged
    // alone either way; her birthday keeps its own line (the dedication waits); recognised as a hello.
    private static void Dedication()
    {
        var lines = Field<string[]>("DedicationLines");
        var credits = Field<string[]>("DedicationCredits");
        foreach (var (name, pool) in new[] { ("DedicationLines", lines), ("DedicationCredits", credits) })
        {
            Expect(pool.Length >= 10, $"{name}: {pool.Length} lines");
            Expect(pool.Distinct().Count() == pool.Length, $"{name}: duplicate line");
            foreach (var l in pool)
                Expect(l.Split("{0}").Length == 2 && !l.Contains("{1}") && !l.Contains('\n') && Sticker.Strip(l) == l && !l.Contains("✨"),
                    $"{name}: one {{0}}, no other placeholder, no newline, no sticker, no ✨: " + l);
        }

        var first = new DateTimeOffset(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);
        var greetings = Field<string[]>("MorningGreetings");
        var facts = Field<string[]>("MorningFunFacts");

        // Each pass of 100 days holds exactly 60 credit days.
        var firstDay = AppTime.DayNumber(first);
        var pass = firstDay - firstDay % 100;
        foreach (var start in new[] { pass, pass + 100, pass + 200 })
        {
            var creditDays = Enumerable.Range(start, 100).Count(MorningGreeting.UsesCredit);
            Expect(creditDays == MorningGreeting.CreditPercent && MorningGreeting.CreditPercent == 60, $"credit days from day {start}: {creditDays}");
        }

        var morning = Enumerable.Range(0, 100).Select(i => first.AddDays(i)).First(m => !MorningGreeting.UsesCredit(AppTime.DayNumber(m)));
        var (text, mentions, uses) = MorningGreeting.ComposeHello(morning, 42);
        var parts = text.Split('\n');
        Expect(uses && parts.Length == 2 && MorningGreeting.IsDedicationLine(parts[0]) && parts[0].Contains("<@42>") && facts.Contains(parts[1]),
            "dedication hello: " + text);
        Expect(mentions.UserIds?.SequenceEqual(new ulong[] { 42 }) == true && mentions.AllowedTypes is null or AllowedMentionTypes.None,
            "the dedication pings someone other than the winner");

        var creditMorning = Enumerable.Range(0, 100).Select(i => first.AddDays(i)).First(m => MorningGreeting.UsesCredit(AppTime.DayNumber(m)));
        var creditDay = AppTime.DayNumber(creditMorning);
        var (credited, creditMentions, creditUses) = MorningGreeting.ComposeHello(creditMorning, 42);
        var creditParts = credited.Split('\n');
        Expect(creditUses && creditParts.Length == 3
               && creditParts[0] == DailyRotation.Pick(greetings, creditDay, MorningGreeting.GreetingSalt)
               && creditParts[1] == DailyRotation.Pick(facts, creditDay, MorningGreeting.FunFactSalt)
               && creditParts[2] == string.Format(DailyRotation.Pick(credits, creditDay, MorningGreeting.CreditSalt), "<@42>"),
            "credited hello: " + credited);
        Expect(creditMentions.UserIds?.SequenceEqual(new ulong[] { 42 }) == true && creditMentions.AllowedTypes is null or AllowedMentionTypes.None,
            "the credit pings someone other than the winner");
        var day = AppTime.DayNumber(morning);

        var (classic, classicMentions, classicUses) = MorningGreeting.ComposeHello(morning, null);
        var expected = DailyRotation.Pick(greetings, day, MorningGreeting.GreetingSalt) + "\n" + DailyRotation.Pick(facts, day, MorningGreeting.FunFactSalt);
        Expect(!classicUses && classic == expected && classicMentions.UserIds is not { Count: > 0 }, "classic hello changed: " + classic);

        var birthday = new DateTimeOffset(2027, 6, 16, 7, 0, 0, TimeSpan.Zero);
        var (cake, _, cakeUses) = MorningGreeting.ComposeHello(birthday, 42);
        var age = (string)Br.GetMethod("BirthdayGreeting")!.Invoke(null, new object[] { MorningGreeting.Age(birthday) })!;
        Expect(!cakeUses && cake == age, "the birthday hello carried a dedication: " + cake);

        Expect(greetings.All(g => !MorningGreeting.IsDedicationLine(g)), "a hello line reads as a dedication");
        Expect(lines.All(l => MorningGreeting.IsDedicationLine(string.Format(l, "<@123456789012345678>"))), "a dedication is not recognised");
        Expect(!MorningGreeting.IsDedicationLine(string.Format(lines[0], "Luca")), "a dedication without a mention is recognised");
    }
}
