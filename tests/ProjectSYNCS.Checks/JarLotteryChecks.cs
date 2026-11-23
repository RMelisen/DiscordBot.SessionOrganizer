using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 54. The jar and the weekly lottery: the jar's goals, gifts and races, the double /daily, the quiz
// day, the rewards and the giveaways that pay; the lottery's days, tickets and draws. Against a real
// in-memory SQLite, so every query must translate.
public static class JarLotteryChecks
{
    private static Action<string> _fail = _ => { };
    private const ulong G = 1000, G2 = 1001, A = 42, B = 43, C = 44;
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("jar/lottery: " + what);
    }

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

        public void Wallet(ulong user, long balance, ulong guild = G)
        {
            using var c = New();
            c.PebbleWallets.Add(new PebbleWallet { GuildId = guild, UserId = user, Balance = balance });
            c.SaveChanges();
        }

        public long Balance(ulong user, ulong guild = G)
        {
            using var c = New();
            return c.PebbleWallets.Where(w => w.GuildId == guild && w.UserId == user).Select(w => w.Balance).FirstOrDefault();
        }

        public long Metric(string metric)
        {
            using var c = New();
            return c.EconomyDailyStats.Where(r => r.GuildId == G && r.Metric == metric).Sum(r => r.Value);
        }

        public long Spent(ulong user)
        {
            using var c = New();
            return c.MemberXps.Where(m => m.GuildId == G && m.UserId == user).Select(m => m.CaillouxSpent).FirstOrDefault();
        }

        public CaillouxJar Jar()
        {
            using var c = New();
            return c.CaillouxJars.AsNoTracking().Single(j => j.GuildId == G);
        }

        public void Dispose() => Conn.Dispose();
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        JarRules();
        await Guard("gifts", GiftsAsync);
        await Guard("gift race", GiftRaceAsync);
        await Guard("double daily", DoubleDailyAsync);

        await Guard("rewards", RewardsAsync);
        await Guard("two timed rewards", TwoTimedAsync);
        await Guard("feast", FeastAsync);
        await Guard("reward race", RewardRaceAsync);
        await Guard("paid giveaway", PaidGiveawayAsync);
        JarCard();
        JarPools();
        LotteryRules();
        await Guard("lottery buy", LotteryBuyAsync);
        await Guard("lottery draw", LotteryDrawAsync);
        await Guard("lottery: her ticket", LotteryHerTicketAsync);
        await Guard("lottery: an empty week", LotteryEmptyWeekAsync);
        LotteryTexts();
        // The plan's five risk cases, each pinned down (the scenarios above cover the plain paths).
        await Guard("risk 2: one gift, two goals", OneGiftTwoGoalsAsync);
        await Guard("risk 3: a ticket after the draw", LateTicketAsync);
        await Guard("risk 3: the write lock", WriteLockAsync);
        await Guard("risk 4: two draws at once", ConcurrentDrawAsync);
        await Guard("risk 5: a paid draw racing a wallet", StaleWinnerGiveawayAsync);
    }

    // ---- the plan's risk cases ------------------------------------------------------------------

    // Risk 2: one gift past two goals leaves two rewards, resolved in order, then none.
    private static async Task OneGiftTwoGoalsAsync()
    {
        using var db = new Db();
        db.Wallet(A, 4000);
        JarGiveResult r;
        using (var c = db.New()) r = await new JarService(c).GiveAsync(G, A, 3600, Now);
        Expect(r is { Outcome: JarGiveOutcome.Given, Reached: 2 }, $"one gift, two goals: {r.Outcome} reached {r.Reached}");
        var first = await ResolveAsync(db);
        var second = await ResolveAsync(db);
        var third = await ResolveAsync(db);
        Expect(first?.Reward == JarReward.DoubleDaily && second?.Reward == JarReward.Feast && third is null,
            $"one gift, two goals: {first?.Reward} then {second?.Reward} then {third?.Reward}");
        var jar = db.Jar();
        Expect(jar.Level == 2 && jar.RewardsDone == 2 && jar.Filled == 100 && db.Balance(A) + jar.Given == 4000,
            $"one gift, two goals: level {jar.Level} done {jar.RewardsDone} filled {jar.Filled}");
    }

    // Risk 3: a buy that started before 21:00 but reached the database after the draw goes to the
    // next week, never onto a week already drawn.
    private static async Task LateTicketAsync()
    {
        using var db = new Db();
        db.Wallet(A, 1000);
        using (var c = db.New())
        {
            c.LotteryDraws.Add(new LotteryDraw { GuildId = G, DrawDay = 20261122, DrawnAt = Utc(2026, 11, 22, 20, 0) });
            c.SaveChanges();
        }
        var at = Utc(2026, 11, 22, 19, 59);   // 20:59 Paris: this Sunday's draw, by the clock
        LotteryBuy r;
        using (var c = db.New()) r = await new LotteryService(c).BuyAsync(G, A, 2, at);
        using var check = db.New();
        var entry = check.LotteryEntries.AsNoTracking().Single();
        Expect(r.Outcome == LotteryBuyOutcome.Bought && entry.DrawDay == 20261129 && r.Status.DrawDay == 20261129,
            $"a ticket after the draw went to {entry.DrawDay} (status {r.Status.DrawDay}), want 20261129");
    }

    // A file database shared by two connections: what the Pi has, unlike the in-memory one.
    private sealed class FileDb : IDisposable
    {
        public readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"syncs-checks-{Guid.NewGuid():N}.db");

        public FileDb()
        {
            using var c = New();
            c.Database.EnsureCreated();
        }

        public AppDbContext New(int timeoutSeconds = 30) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={Path};Pooling=False;Default Timeout={timeoutSeconds}").Options);

        public void Dispose()
        {
            try { File.Delete(Path); } catch (IOException) { }
        }
    }

    // Risk 3, the mechanism: the draw's transaction holds the write lock from BEGIN, so no ticket can
    // be saved while it reads. If the driver ever opened it DEFERRED, this write would get through.
    private static async Task WriteLockAsync()
    {
        using var db = new FileDb();
        await using var drawing = db.New();
        await using var tx = await drawing.Database.BeginTransactionAsync();
        _ = await drawing.LotteryEntries.CountAsync();
        await using var buyer = db.New(timeoutSeconds: 1);
        buyer.LotteryEntries.Add(new LotteryEntry { GuildId = G, DrawDay = 20261122, UserId = A, Tickets = 1 });
        var blocked = false;
        try { await buyer.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 }) { blocked = true; }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5) { blocked = true; }
        Expect(blocked, "a write got in during an open lottery transaction: it isn't IMMEDIATE");
        await tx.RollbackAsync();
    }

    // Risk 4: two passes drawing the same week at the same moment, on two connections: one result,
    // one payment.
    private static async Task ConcurrentDrawAsync()
    {
        using var db = new FileDb();
        using (var c = db.New())
        {
            c.LotteryEntries.AddRange(
                new LotteryEntry { GuildId = G, DrawDay = 20261122, UserId = A, Tickets = 15 },
                new LotteryEntry { GuildId = G, DrawDay = 20261122, UserId = B, Tickets = 5 });
            c.SaveChanges();
        }
        var after = Utc(2026, 11, 22, 20, 1);
        async Task<LotteryResult?> Draw()
        {
            await using var c = db.New();
            return await new LotteryService(c).DrawAsync(G, 20261122, new FixedRandom(0, next: 3), after);
        }
        var results = await Task.WhenAll(Task.Run(Draw), Task.Run(Draw));
        await using var check = db.New();
        var paid = check.PebbleWallets.AsNoTracking().Where(w => w.GuildId == G).Sum(w => w.Balance);
        Expect(results.Count(r => r is not null) == 1 && check.LotteryDraws.Count() == 1 && paid == 315,
            $"two draws at once: {results.Count(r => r is not null)} results, {check.LotteryDraws.Count()} rows, {paid} paid");
    }

    // Risk 5: the jar's giveaway drawn while a winner's wallet is written elsewhere (a /work): the
    // draw's save hits the wallet's token, redraws on fresh wallets, and pays each winner once.
    private static async Task StaleWinnerGiveawayAsync()
    {
        using var db = new Db();
        foreach (var u in new[] { A, B, C }) db.Wallet(u, 0);
        int id;
        using (var c = db.New())
        {
            var g = new Giveaway { GuildId = G, ChannelId = 1, OrganizerId = Bot, Prize = "x", WinnerCount = 2, PebblePrize = 150, EndsAt = Now };
            foreach (var u in new[] { A, B, C }) g.Entries.Add(new GiveawayEntry { UserId = u });
            c.Giveaways.Add(g);
            c.SaveChanges();
            id = g.Id;
        }

        using var stale = db.New();
        _ = stale.PebbleWallets.Where(w => w.GuildId == G).ToList();   // tracked copies, about to go stale
        using (var other = db.New())
        {
            foreach (var w in other.PebbleWallets.Where(w => w.GuildId == G)) w.Balance += 100;
            other.SaveChanges();
        }
        var winners = await new GiveawayService(stale).TryDrawAsync(id);
        Expect(winners is { Count: 2 }, $"racing draw winners: {winners?.Count}");
        foreach (var u in new[] { A, B, C })
            Expect(db.Balance(u) == (winners?.Contains(u) == true ? 250 : 100), $"racing draw: {u} has {db.Balance(u)}");
        Expect(db.Metric(EconomyLog.EarnGiveaway) == 300, $"racing draw: earn.giveaway = {db.Metric(EconomyLog.EarnGiveaway)}");
    }

    private static void LotteryTexts()
    {
        var at = Lottery.DrawAt(20261122);
        var status = ArcadeModule.LotteryStatusText(new LotteryStatus(20261122, at, 320, 15, 15, 1));
        Expect(status.Contains("93,8 %") && status.Contains(PebbleEconomy.Cailloux(320)) && status.Contains("<t:"),
            $"lottery status: {status}");
        var empty = ArcadeModule.LotteryStatusText(new LotteryStatus(20261122, at, 0, 0, 0, 0));
        Expect(empty.Contains("Personne n'a encore joué"), $"empty lottery status: {empty}");

        var bought = ArcadeModule.LotteryBuyReply(new LotteryBuy(LotteryBuyOutcome.Bought, 3, 60, 940,
            new LotteryStatus(20261122, at, 80, 3, 3, 1)), A);
        Expect(bought.Contains($"<@{A}>") && bought.Contains(PebbleEconomy.Cailloux(80)) && bought.Contains("<t:"), $"bought: {bought}");
        var poor = ArcadeModule.LotteryBuyReply(new LotteryBuy(LotteryBuyOutcome.TooPoor, 6, 120, 100,
            new LotteryStatus(20261122, at, 0, 0, 0, 0)), A);
        Expect(poor.Contains(PebbleEconomy.Cailloux(120)) && poor.Contains(PebbleEconomy.Cailloux(100)), $"too poor: {poor}");

        CheckPool("LotteryWinLines", 8, "<@1>", "315 cailloux");
        CheckPool("LotteryGloatLines", 8, "420 cailloux");

        var commands = typeof(ArcadeModule).GetMethods()
            .Count(m => m.GetCustomAttributes(typeof(Discord.Interactions.SlashCommandAttribute), false).Length > 0);
        Expect(commands == 11, $"/arcade has {commands} subcommands, want 11 (cap 25)");
    }

    // ---- the lottery -----------------------------------------------------------------------------

    private static DateTimeOffset Utc(int y, int mo, int d, int h, int mi) => new(y, mo, d, h, mi, 0, TimeSpan.Zero);

    private static void LotteryRules()
    {
        Expect(Lottery.DrawDay(Utc(2026, 11, 22, 19, 59)) == 20261122, $"DrawDay Sunday 20:59 Paris = {Lottery.DrawDay(Utc(2026, 11, 22, 19, 59))}");
        Expect(Lottery.DrawDay(Utc(2026, 11, 22, 20, 0)) == 20261129, $"DrawDay Sunday 21:00 Paris = {Lottery.DrawDay(Utc(2026, 11, 22, 20, 0))}");
        Expect(Lottery.DrawDay(Utc(2026, 11, 18, 12, 0)) == 20261122, "DrawDay on a Wednesday");
        Expect(Lottery.DrawDay(Utc(2026, 7, 5, 18, 59)) == 20260705 && Lottery.DrawDay(Utc(2026, 7, 5, 19, 0)) == 20260712, "DrawDay in summer");
        Expect(Lottery.DrawDay(Utc(2026, 10, 25, 19, 59)) == 20261025, "DrawDay on the DST Sunday");
        Expect(Lottery.DrawAt(20261122) == Utc(2026, 11, 22, 20, 0), $"DrawAt = {Lottery.DrawAt(20261122)}");
        Expect(Lottery.Pick(new[] { 3, 2 }, 0) == 0 && Lottery.Pick(new[] { 3, 2 }, 2) == 0
               && Lottery.Pick(new[] { 3, 2 }, 3) == 1 && Lottery.Pick(new[] { 3, 2 }, 5) == -1, "Pick ranges");
        Expect(Lottery.Payout(100) == 75 && Lottery.Payout(20) == 15, "Payout");
        // Her free ticket adds one ticket's worth to the pot (owner, 2026-11-18): a lone player can win.
        Expect(Lottery.Pot(0) == 0 && Lottery.Pot(1) == 40 && Lottery.Pot(15) == 320, $"Pot: {Lottery.Pot(0)} {Lottery.Pot(1)} {Lottery.Pot(15)}");
        Expect(Lottery.Payout(Lottery.Pot(1)) > Lottery.TicketPrice, "a lone player's win doesn't beat the ticket");
    }

    private static async Task LotteryBuyAsync()
    {
        using var db = new Db();
        db.Wallet(A, 1000);
        db.Wallet(B, 100);
        using (var c = db.New())
        {
            var r = await new LotteryService(c).BuyAsync(G, A, 10, Now);
            Expect(r.Outcome == LotteryBuyOutcome.Bought && r.Cost == 200 && r.Balance == 800, $"buy 10: {r.Outcome} {r.Cost} {r.Balance}");
        }
        using (var c = db.New()) await new LotteryService(c).BuyAsync(G, A, 5, Now);
        using (var c = db.New())
        {
            var r = await new LotteryService(c).BuyAsync(G, B, 6, Now);
            Expect(r.Outcome == LotteryBuyOutcome.TooPoor && r.Cost == 120 && r.Balance == 100, $"B buys 6: {r.Outcome}");
            Expect(c.LotteryEntries.Count(e => e.GuildId == G) == 1
                   && c.LotteryEntries.Single(e => e.GuildId == G).Tickets == 15, "A's tickets in one entry");
            var s = await new LotteryService(c).StatusAsync(G, A, Now);
            Expect(s.Pot == 320 && s.Mine == 15 && s.Tickets == 15 && s.Players == 1 && s.DrawDay == 20261122,
                $"status: pot {s.Pot} mine {s.Mine} tickets {s.Tickets} players {s.Players} day {s.DrawDay}");
        }
        Expect(db.Balance(B) == 100 && db.Metric(EconomyLog.SpendLottery) == 300, $"spend.lottery = {db.Metric(EconomyLog.SpendLottery)}");
        Expect(db.Spent(A) == 0, "lottery tickets counted toward Mécène");
    }

    private static void SeedEntries(Db db)
    {
        using var c = db.New();
        c.LotteryEntries.AddRange(
            new LotteryEntry { GuildId = G, DrawDay = 20261122, UserId = A, Tickets = 15 },
            new LotteryEntry { GuildId = G, DrawDay = 20261122, UserId = B, Tickets = 5 },
            new LotteryEntry { GuildId = G, DrawDay = 20261129, UserId = C, Tickets = 1 });
        c.SaveChanges();
    }

    private static async Task LotteryDrawAsync()
    {
        using var db = new Db();
        SeedEntries(db);
        var after = Utc(2026, 11, 22, 20, 1);
        using (var c = db.New())
        {
            var due = await new LotteryService(c).DueAsync(after);
            Expect(due.Count == 1 && due[0] == (G, 20261122), $"due: {string.Join(",", due)}");
        }
        LotteryResult? r;
        using (var c = db.New()) r = await new LotteryService(c).DrawAsync(G, 20261122, new FixedRandom(0, next: 3), after);
        Expect(r is { Winner: A, Pot: 420, Paid: 315, Tickets: 20 }, $"draw: {r}");
        Expect(db.Balance(A) == 315 && db.Metric(EconomyLog.EarnLottery) == 315, $"draw paid {db.Balance(A)}, earn.lottery {db.Metric(EconomyLog.EarnLottery)}");
        using (var c = db.New())
        {
            Expect(await new LotteryService(c).DrawAsync(G, 20261122, new FixedRandom(0, next: 3), after) is null, "a second draw ran");
            Expect((await new LotteryService(c).DueAsync(after)).Count == 0, "a drawn week is still due");
        }
        Expect(db.Balance(A) == 315, "a second draw paid");
    }

    private static async Task LotteryHerTicketAsync()
    {
        using var db = new Db();
        SeedEntries(db);
        LotteryResult? r;
        using (var c = db.New()) r = await new LotteryService(c).DrawAsync(G, 20261122, new FixedRandom(0, next: 20), Utc(2026, 11, 22, 20, 1));
        Expect(r is { Winner: null, Paid: 0, Pot: 420 }, $"her ticket: {r}");
        Expect(db.Balance(A) == 0 && db.Balance(B) == 0 && db.Metric(EconomyLog.EarnLottery) == 0, "her ticket paid someone");
        using var c2 = db.New();
        Expect(c2.LotteryDraws.Single().WinnerId == 0, "her win isn't recorded as WinnerId 0");
    }

    // Nobody played: never due, and a draw forced anyway does nothing. Her free ticket would win every
    // empty week otherwise, and she'd gloat over a pot of 20 she put in herself.
    private static async Task LotteryEmptyWeekAsync()
    {
        using var db = new Db();
        using (var c = db.New())
        {
            c.LotteryEntries.Add(new LotteryEntry { GuildId = G, DrawDay = 20261122, UserId = A, Tickets = 0 });
            c.SaveChanges();
        }
        var after = Utc(2026, 11, 22, 20, 1);
        using (var c = db.New())
        {
            Expect((await new LotteryService(c).DueAsync(after)).Count == 0, "a week with no tickets is due");
            Expect(await new LotteryService(c).DrawAsync(G, 20261122, new FixedRandom(0, next: 0), after) is null,
                "a week with no tickets was drawn");
        }
        using (var c = db.New())
            Expect(await new LotteryService(c).DrawAsync(G, 20261129, new FixedRandom(0, next: 0), after) is null,
                "a week with no entries was drawn");
        using var c2 = db.New();
        Expect(!c2.LotteryDraws.Any(), "an empty week left a draw row");
    }

    // BotResponses is internal: its pools are read by reflection, like the rest of the checks.
    private static readonly Type Br = typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;
    private static string[] Pool(string name) =>
        (string[])(Br.GetField(name)?.GetValue(null) ?? throw new InvalidOperationException("no BotResponses." + name));

    private static void JarCard()
    {
        var status = new JarStatus(1, 500, 2000, JarReward.Feast, new List<(ulong, long)> { (A, 1000), (B, 600) }, 1000,
            20261119, 20261119);
        var card = JarModule.BuildCard(status, Now);
        var all = card.Title + card.Description + string.Concat(card.Fields.Select(f => f.Name + f.Value)) + card.Footer?.Text;
        Expect(all.Contains(PebbleEconomy.Cailloux(500).Split(' ')[0]) && all.Contains(PebbleEconomy.Cailloux(2000)),
            "jar card: the fill and the goal");
        Expect(all.Contains($"<@{A}>") && all.Contains($"<@{B}>"), "jar card: the donors' pills");
        Expect(all.Contains(JarModule.RewardTitle(JarReward.Feast)), "jar card: the next reward");
        Expect(all.Contains("<t:"), "jar card: the pending double day's date");
        Expect(all.Length < 6000 && card.Fields.All(f => f.Value.Length <= 1024), "jar card: over Discord's caps");

        var picker = new ResponsePicker();
        foreach (var reward in Enum.GetValues<JarReward>())
        {
            var u = new JarUnlock(reward, 0, reward == JarReward.DoubleDaily ? 20261119 : 0,
                reward == JarReward.Feast ? 7 : 0, null);
            var line = JarModule.UnlockLine(u, A, picker);
            Expect(line.Contains($"<@{A}>"), $"unlock {reward}: no giver pill: {line}");
            if (reward == JarReward.Feast) Expect(line.Contains('7'), $"feast line without its count: {line}");
            if (u.Day != 0) Expect(line.Contains("<t:"), $"unlock {reward}: no date: {line}");
        }
    }

    private static readonly System.Text.RegularExpressions.Regex Gendered =
        new(@"\b(officiel|roi|clients|il a payé|elle a payé|le meilleur)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static void CheckPool(string name, int min, params object[] args)
    {
        string[] pool;
        try { pool = Pool(name); }
        catch (Exception ex) { Expect(false, ex.Message); return; }
        Expect(pool.Length >= min, $"{name}: {pool.Length} lines, want ≥ {min}");
        Expect(pool.Distinct().Count() == pool.Length, $"{name}: a duplicate line");
        foreach (var l in pool)
        {
            try { _ = string.Format(l, args); }
            catch (FormatException) { Expect(false, $"{name}: doesn't format: {l}"); }
            if (l.Contains('✨')) Expect(false, $"{name}: ✨ instead of Emotes.Sparkle: {l}");
            if (l.Contains("(｡•́︿•̀｡)")) Expect(false, $"{name}: banned kaomoji: {l}");
            if (Gendered.Match(l) is { Success: true } m) Expect(false, $"{name}: gendered « {m.Value} »: {l}");
        }
    }

    private static void JarPools()
    {
        CheckPool("JarDoubleDailyLines", 6, "<@1>", "<t:1:D>");
        CheckPool("JarFeastLines", 6, "<@1>", 7);
        CheckPool("JarGiveawayLines", 6, "<@1>");
    }

    private const ulong Bot = 999;

    private static void SeedJar(Db db, int level, int rewardsDone, int doubleUntil = 0)
    {
        using var c = db.New();
        c.CaillouxJars.Add(new CaillouxJar
        {
            GuildId = G, Level = level, RewardsDone = rewardsDone,
            DoubleDailyFrom = doubleUntil, DoubleDailyUntil = doubleUntil,
        });
        c.SaveChanges();
    }

    private static async Task<JarUnlock?> ResolveAsync(Db db, AppDbContext? context = null)
    {
        if (context is not null) return await new JarService(context).ResolveNextRewardAsync(G, Bot, 555, Now);
        using var c = db.New();
        return await new JarService(c).ResolveNextRewardAsync(G, Bot, 555, Now);
    }

    private static async Task RewardsAsync()
    {
        using var db = new Db();
        SeedJar(db, level: 3, rewardsDone: 0);
        var unlocks = new List<JarUnlock?>();
        for (var i = 0; i < 4; i++) unlocks.Add(await ResolveAsync(db));
        Expect(unlocks[0] is { Reward: JarReward.DoubleDaily, Day: 20261119 }, $"reward 1: {unlocks[0]}");
        Expect(unlocks[1] is { Reward: JarReward.Feast }, $"reward 2: {unlocks[1]}");
        Expect(unlocks[2] is { Reward: JarReward.Giveaway, Giveaway: not null }, $"reward 3: {unlocks[2]}");
        Expect(unlocks[3] is null, $"reward 4 should be none: {unlocks[3]}");
        var jar = db.Jar();
        Expect(jar.DoubleDailyFrom == 20261119 && jar.DoubleDailyUntil == 20261119 && jar.RewardsDone == 3,
            $"jar after rewards: double {jar.DoubleDailyFrom}-{jar.DoubleDailyUntil} done {jar.RewardsDone}");
        using var c = db.New();
        var g = c.Giveaways.SingleOrDefault(x => x.GuildId == G);
        Expect(g is not null && g.PebblePrize == Jar.GiveawayPrize && g.OrganizerId == Bot && g.ChannelId == 555
               && g.WinnerCount == Jar.GiveawayWinners && g.EndsAt == Now + Jar.GiveawayFor && !g.IsClosed,
            "the jar's giveaway row");
    }

    private static async Task TwoTimedAsync()
    {
        using var db = new Db();
        SeedJar(db, level: 4, rewardsDone: 3, doubleUntil: 20261119);
        var u = await ResolveAsync(db);
        var jar = db.Jar();
        Expect(u is { Reward: JarReward.DoubleDaily, Day: 20261120 }, $"second double day: {u}");
        Expect(jar.DoubleDailyFrom == 20261119 && jar.DoubleDailyUntil == 20261120,
            $"double range {jar.DoubleDailyFrom}-{jar.DoubleDailyUntil}");
    }

    private static Plynling Pet(ulong owner, double hunger) => new()
    {
        GuildId = G, OwnerId = owner, Name = "P" + owner, Hunger = hunger, Happiness = 0.5,
        AdoptedAt = Now.AddDays(-3), NeedsAsOf = Now, LiveSince = Now.AddDays(-3),
    };

    private static async Task FeastAsync()
    {
        using var db = new Db();
        SeedJar(db, level: 2, rewardsDone: 1);
        using (var c = db.New())
        {
            c.Plynlings.Add(Pet(A, 0.1));
            var dead = Pet(B, 0.1);
            dead.DiedAt = Now.AddDays(-1);
            c.Plynlings.Add(dead);
            var frozen = Pet(C, 0.1);
            frozen.FrozenAt = Now.AddDays(-1);
            c.Plynlings.Add(frozen);
            c.SaveChanges();
        }

        var u = await ResolveAsync(db);
        Expect(u is { Reward: JarReward.Feast, Fed: 1 }, $"feast: {u}");
        using var r = db.New();
        var pets = r.Plynlings.AsNoTracking().ToDictionary(p => p.OwnerId, p => p.Hunger);
        Expect(pets[A] > 0.1, $"the living Plynling didn't eat: {pets[A]}");
        Expect(pets[B] == 0.1 && pets[C] == 0.1, $"dead/frozen ate: {pets[B]} {pets[C]}");
    }

    private static async Task RewardRaceAsync()
    {
        using var db = new Db();
        SeedJar(db, level: 1, rewardsDone: 0);
        using var stale = db.New();
        _ = stale.CaillouxJars.Single(j => j.GuildId == G);
        var first = await ResolveAsync(db);
        var second = await ResolveAsync(db, stale);
        Expect(first is not null && second is null, $"reward race: {first} / {second}");
        Expect(db.Jar().RewardsDone == 1, $"reward race: done {db.Jar().RewardsDone}");
    }

    private static async Task PaidGiveawayAsync()
    {
        using var db = new Db();
        int paid, free;
        using (var c = db.New())
        {
            var g = new Giveaway { GuildId = G, ChannelId = 1, OrganizerId = Bot, Prize = "x", WinnerCount = 2, PebblePrize = 150, EndsAt = Now };
            foreach (var u in new[] { A, B, C }) g.Entries.Add(new GiveawayEntry { UserId = u });
            var h = new Giveaway { GuildId = G, ChannelId = 1, OrganizerId = A, Prize = "y", WinnerCount = 1, EndsAt = Now };
            h.Entries.Add(new GiveawayEntry { UserId = A });
            c.Giveaways.AddRange(g, h);
            c.SaveChanges();
            (paid, free) = (g.Id, h.Id);
        }

        List<ulong>? winners;
        using (var c = db.New()) winners = await new GiveawayService(c).TryDrawAsync(paid);
        Expect(winners is { Count: 2 }, $"paid draw winners: {winners?.Count}");
        if (winners is not null)
            foreach (var u in new[] { A, B, C })
                Expect(db.Balance(u) == (winners.Contains(u) ? 150 : 0), $"paid draw: {u} has {db.Balance(u)}");
        Expect(db.Metric(EconomyLog.EarnGiveaway) == 300, $"earn.giveaway = {db.Metric(EconomyLog.EarnGiveaway)}");
        using (var c = db.New()) Expect(await new GiveawayService(c).TryDrawAsync(paid) is null, "a second draw ran");
        Expect(db.Metric(EconomyLog.EarnGiveaway) == 300, "a second draw paid");
        using (var c = db.New()) await new GiveawayService(c).TryDrawAsync(free);
        Expect(db.Balance(A) == (winners?.Contains(A) == true ? 150 : 0), "a member's giveaway moved a wallet");
    }

    private static async Task Guard(string what, Func<Task> run)
    {
        try { await run(); }
        catch (Exception ex) { Expect(false, what + " threw: " + ex.GetType().Name + ": " + ex.Message); }
    }

    // ---- the jar ---------------------------------------------------------------------------------

    private static void JarRules()
    {
        Expect(Jar.GoalFor(0) == 1500 && Jar.GoalFor(1) == 2000 && Jar.GoalFor(7) == 5000 && Jar.GoalFor(50) == 5000,
            $"GoalFor: {Jar.GoalFor(0)} {Jar.GoalFor(1)} {Jar.GoalFor(7)} {Jar.GoalFor(50)}");
        Expect(Jar.RewardFor(0) == JarReward.DoubleDaily && Jar.RewardFor(1) == JarReward.Feast && Jar.RewardFor(2) == JarReward.Giveaway
               && Jar.RewardFor(3) == JarReward.DoubleDaily, "RewardFor order");
        Expect(Jar.Fill(0, 1400, 100) == (1, 0), $"Fill(0,1400,100) = {Jar.Fill(0, 1400, 100)}");
        Expect(Jar.Fill(0, 0, 3600) == (2, 100), $"Fill(0,0,3600) = {Jar.Fill(0, 0, 3600)}");
        Expect(Jar.Fill(0, 0, 10) == (0, 10), $"Fill(0,0,10) = {Jar.Fill(0, 0, 10)}");
        Expect(Jar.Extend(0, 0, 20261119) == (20261119, 20261119), "Extend from nothing");
        Expect(Jar.Extend(20261119, 20261119, 20261119) == (20261119, 20261120), "Extend a pending day");
        Expect(Jar.Extend(20261110, 20261112, 20261119) == (20261119, 20261119), "Extend a spent range");
        Expect(Jar.NextDay(20261130) == 20261201 && Jar.NextDay(20261231) == 20270101,
            $"NextDay: {Jar.NextDay(20261130)} {Jar.NextDay(20261231)}");
        Expect(Jar.Covers(20261119, 20261120, 20261120) && !Jar.Covers(20261119, 20261120, 20261121)
               && !Jar.Covers(0, 0, 0), "Covers");
    }

    private static async Task GiftsAsync()
    {
        using var db = new Db();
        db.Wallet(A, 2000);
        db.Wallet(B, 2000);
        void Invariant(string step)
        {
            using var c = db.New();
            var given = c.CaillouxJars.Where(j => j.GuildId == G).Select(j => j.Given).FirstOrDefault();
            Expect(db.Balance(A) + db.Balance(B) + given == 4000, $"gifts: total moved after {step}");
        }

        using (var c = db.New())
        {
            var r = await new JarService(c).GiveAsync(G, A, 1000, Now);
            Expect(r.Outcome == JarGiveOutcome.Given && r.Reached == 0 && r.Balance == 1000, $"gift A 1000: {r.Outcome} {r.Reached} {r.Balance}");
        }
        Invariant("A 1000");
        using (var c = db.New())
        {
            var r = await new JarService(c).GiveAsync(G, B, 600, Now);
            Expect(r.Outcome == JarGiveOutcome.Given && r.Reached == 1, $"gift B 600: {r.Outcome} reached {r.Reached}");
        }
        Invariant("B 600");
        var jar = db.Jar();
        Expect(jar.Level == 1 && jar.Filled == 100, $"jar after 1600: level {jar.Level} filled {jar.Filled}");
        using (var c = db.New())
        {
            var r = await new JarService(c).GiveAsync(G, A, 5000, Now);
            Expect(r.Outcome == JarGiveOutcome.TooPoor && r.Balance == 1000, $"gift A 5000: {r.Outcome} {r.Balance}");
        }
        Invariant("A 5000 refused");
        Expect(db.Metric(EconomyLog.SpendJar) == 1600, $"spend.jar = {db.Metric(EconomyLog.SpendJar)}");
        Expect(db.Spent(A) == 1000, $"A CaillouxSpent = {db.Spent(A)}");
        using (var c = db.New())
        {
            var gifts = c.JarGifts.Where(g => g.GuildId == G).ToDictionary(g => g.UserId, g => g.Total);
            Expect(gifts.GetValueOrDefault(A) == 1000 && gifts.GetValueOrDefault(B) == 600, "JarGift totals");
            var s = await new JarService(c).StatusAsync(G, A, Now);
            Expect(s.Top.Count == 2 && s.Top[0] == (A, 1000) && s.Top[1] == (B, 600), "status top");
            Expect(s.Mine == 1000 && s.Goal == 2000 && s.Next == JarReward.Feast && s.Filled == 100,
                $"status: mine {s.Mine} goal {s.Goal} next {s.Next} filled {s.Filled}");
        }
    }

    private static async Task GiftRaceAsync()
    {
        using var db = new Db();
        db.Wallet(A, 2000);
        db.Wallet(B, 2000);
        using (var seed = db.New())
        {
            seed.CaillouxJars.Add(new CaillouxJar { GuildId = G });
            seed.SaveChanges();
        }

        using var stale = db.New();
        _ = stale.CaillouxJars.Single(j => j.GuildId == G);   // a tracked copy, about to go stale
        using (var other = db.New()) await new JarService(other).GiveAsync(G, A, 1400, Now);
        var r = await new JarService(stale).GiveAsync(G, B, 200, Now);
        var jar = db.Jar();
        Expect(r.Outcome == JarGiveOutcome.Given && r.Reached == 1, $"race: B {r.Outcome} reached {r.Reached}");
        Expect(jar.Level == 1 && jar.Filled == 100 && jar.Given == 1600, $"race: level {jar.Level} filled {jar.Filled} given {jar.Given}");
        // Risk 1: the money moved, not more, not less, and the goal leaves one reward, run once.
        Expect(db.Balance(A) + db.Balance(B) + jar.Given == 4000, "race: the wallets and the jar don't add up");
        var first = await ResolveAsync(db);
        var again = await ResolveAsync(db);
        Expect(first is not null && again is null && db.Jar().RewardsDone == 1, $"race: rewards {first?.Reward} / {again?.Reward}");
    }

    private static async Task DoubleDailyAsync()
    {
        using var db = new Db();
        var today = AppTime.DayKey(Now);
        using (var seed = db.New())
        {
            seed.CaillouxJars.Add(new CaillouxJar { GuildId = G, DoubleDailyFrom = today, DoubleDailyUntil = today });
            seed.SaveChanges();
        }

        DailyResult doubled, plain, nextDay;
        using (var c = db.New()) doubled = await new PebbleService(c).ClaimDailyAsync(G, A, Now, new Random(7));
        using (var c = db.New()) plain = await new PebbleService(c).ClaimDailyAsync(G2, A, Now, new Random(7));
        using (var c = db.New()) nextDay = await new PebbleService(c).ClaimDailyAsync(G, B, Now.AddDays(1), new Random(7));
        Expect(doubled.Claimed && doubled.Doubled && doubled.Amount == plain.Amount * 2,
            $"double daily: {doubled.Amount} vs {plain.Amount}, doubled {doubled.Doubled}");
        Expect(!plain.Doubled, "daily without a jar is doubled");
        Expect(!nextDay.Doubled && nextDay.Amount == plain.Amount, $"daily the next day: doubled {nextDay.Doubled}, {nextDay.Amount}");
        var (text, _) = EconomyModule.DailyReply(doubled, A, false, new ResponsePicker());
        Expect(text.Contains("bocal"), "the doubled /daily reply doesn't name the jar");
    }

    private sealed class FixedRandom(double value, int next = 0) : Random
    {
        public override double NextDouble() => value;
        public override int Next(int maxValue) => Math.Min(next, maxValue - 1);
        public override int Next(int minValue, int maxValue) => Math.Clamp(next, minValue, maxValue - 1);
    }
}
