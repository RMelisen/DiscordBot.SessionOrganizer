using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

// 52. Mécène (Patronage: jar gifts only), the badge and tab, /pay, packs: what counts toward it, the
// pay rules and cap, the packs' price against what they return. Against a real in-memory SQLite, so
// every query must translate.
public static class EconomySinkChecks
{
    private static Action<string> _fail = _ => { };
    private const ulong G = 1000, Me = 42, You = 43;

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("economy sinks: " + what);
    }

    // A fresh database per scenario; the connection keeps the in-memory database alive.
    private sealed class Db : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _conn = new("DataSource=:memory:");
        private readonly DbContextOptions<AppDbContext> _opts;

        public Db()
        {
            _conn.Open();
            _opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
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

        public void Dispose() => _conn.Dispose();
    }

    public static async Task Run(Action<string> fail)
    {
        _fail = fail;
        try { await SpentCounterAsync(); }
        catch (Exception ex) { Expect(false, "spent counter threw: " + ex.Message); }
        Badge();
        PackPrice();
        try { await PacksAsync(); }
        catch (Exception ex) { Expect(false, "packs threw: " + ex.Message); }
        try { await PayAsync(); }
        catch (Exception ex) { Expect(false, "pay threw: " + ex.Message); }
        try { await PatronsTabAsync(); }
        catch (Exception ex) { Expect(false, "mécènes tab threw: " + ex.Message); }
        try { await RetryAsync(); }
        catch (Exception ex) { Expect(false, "retry threw: " + ex.Message); }
        LeaderboardCard();
    }

    private static async Task PatronsTabAsync()
    {
        const ulong a = 50, b = 51, idle = 52;
        using var db = new Db();
        using (var c = db.New())
        {
            c.MemberXps.AddRange(
                new MemberXp { GuildId = G, UserId = a, TotalXp = 10, CaillouxSpent = 900 },
                new MemberXp { GuildId = G, UserId = b, TotalXp = 10, CaillouxSpent = 300 },
                new MemberXp { GuildId = G, UserId = idle, TotalXp = 5000 });
            c.MemberDailyStats.AddRange(
                new MemberDailyStat { GuildId = G, UserId = a, Day = AppTime.KeyDaysAgo(0), CaillouxSpent = 100 },
                new MemberDailyStat { GuildId = G, UserId = b, Day = AppTime.KeyDaysAgo(0), CaillouxSpent = 300 },
                new MemberDailyStat { GuildId = G, UserId = a, Day = AppTime.KeyDaysAgo(40), CaillouxSpent = 800 },
                new MemberDailyStat { GuildId = G, UserId = idle, Day = AppTime.KeyDaysAgo(0), XpEarned = 50 });
            c.SaveChanges();
        }
        using (var c = db.New())
        {
            var xp = new XpService(c);
            var all = (await xp.GetRankingAsync(G, LeaderboardView.Patrons, StatsPeriod.AllTime)).Select(t => t.UserId).ToList();
            var week = (await xp.GetRankingAsync(G, LeaderboardView.Patrons, StatsPeriod.Week)).Select(t => t.UserId).ToList();
            Expect(all.SequenceEqual(new[] { a, b }), "mécènes all-time order: " + string.Join(",", all));
            Expect(week.SequenceEqual(new[] { b, a }), "mécènes week order: " + string.Join(",", week));
            var weekA = (await xp.GetRankingAsync(G, LeaderboardView.Patrons, StatsPeriod.Week)).Single(t => t.UserId == a);
            Expect(weekA.ValueFor(LeaderboardView.Patrons) == 100, $"mécènes week value: {weekA.CaillouxSpent}");
        }
        Expect(LevelCardUi.ViewLabel(LeaderboardView.Patrons) == "Mécènes", "mécènes tab label");
        Expect(LevelCardUi.Title(LeaderboardView.Patrons) == "## Classement des mécènes", "mécènes title");
        Expect(LevelCardUi.Spent(2500) == PebbleEconomy.Cailloux(2500) + " versés au bocal", "Spent(2500): " + LevelCardUi.Spent(2500));
        Expect(LevelCardUi.Spent(1) == "1 caillou versé au bocal", "Spent(1): " + LevelCardUi.Spent(1));
        Expect(LevelCardUi.Row(1, a, LeaderboardView.Patrons, StatsPeriod.AllTime, new MemberTally(a, 0, 0, 0, 0, 900)).EndsWith(LevelCardUi.Spent(900)), "mécènes row");
        Expect(LevelCardUi.EmptyLine(LeaderboardView.Patrons, StatsPeriod.AllTime).StartsWith("Personne n'a encore rien versé au bocal"), "mécènes empty line");
        Expect(Enum.GetValues<LeaderboardView>().Length <= 5, "more than 5 leaderboard tabs on one row");
    }

    private static async Task PayAsync()
    {
        var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var today = AppTime.DayKey(now);
        var yesterday = AppTime.DayKey(now.AddDays(-1));
        Expect(PebbleEconomy.PayTax(10) == 1 && PebbleEconomy.PayTax(15) == 2 && PebbleEconomy.PayTax(100) == 10,
            $"pay tax: {PebbleEconomy.PayTax(10)} {PebbleEconomy.PayTax(15)} {PebbleEconomy.PayTax(100)}");
        Expect(PebbleEconomy.PayRoom(yesterday, 2000, today) == 2000, "pay room: yesterday's sends count today");
        Expect(PebbleEconomy.PayRoom(today, 1500, today) == 500, "pay room: 1 500 sent today leaves " + PebbleEconomy.PayRoom(today, 1500, today));

        long Balance(Db db, ulong user) { using var c = db.New(); return c.PebbleWallets.Single(w => w.UserId == user).Balance; }

        using (var db = new Db())
        {
            db.Wallet(Me, 1000);
            PayResult r;
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 500, now);
            Expect(r.Outcome == PayOutcome.Paid && r.Amount == 500 && r.Tax == 50 && r.Balance == 450, $"pay 500: {r}");
            var (paidText, paidRefused) = ProjectSYNCS.Commands.EconomyModule.PayReply(r, Me, You);
            Expect(!paidRefused && paidText == $"💸 <@{Me}> a donné **500 cailloux** à <@{You}> · taxe : 50 cailloux", "pay text: " + paidText);
            Expect(Balance(db, Me) == 450 && Balance(db, You) == 500, $"pay 500 moved {Balance(db, Me)} / {Balance(db, You)}");
            using var c2 = db.New();
            var tax = c2.EconomyDailyStats.SingleOrDefault(s => s.Metric == EconomyLog.SpendTax && s.Day == today)?.Value;
            Expect(tax == 50, $"spend.tax today: {tax}");
            Expect(!c2.MemberXps.Any(x => x.CaillouxSpent > 0), "the /pay tax or the amount sent counted toward Mécène");
        }

        using (var db = new Db())
        {
            db.Wallet(Me, 105);
            PayResult r;
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 100, now);
            Expect(r.Outcome == PayOutcome.TooPoor && r.Amount + r.Tax == 110, $"pay 100 with 105: {r}");
            Expect(ProjectSYNCS.Commands.EconomyModule.PayReply(r, Me, You).Text.Contains("110 cailloux"), "too poor does not name the total");
            using var c2 = db.New();
            Expect(Balance(db, Me) == 105 && !c2.PebbleWallets.Any(w => w.UserId == You && w.Balance != 0), "a refused pay moved something");
            Expect(!c2.EconomyDailyStats.Any(s => s.Metric == EconomyLog.SpendTax), "a refused pay logged a tax");
        }

        using (var db = new Db())
        {
            using (var c = db.New())
            {
                c.PebbleWallets.Add(new PebbleWallet { GuildId = G, UserId = Me, Balance = 10_000, PayDay = today, PaidToday = 1900 });
                c.SaveChanges();
            }
            PayResult r;
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 200, now);
            Expect(r.Outcome == PayOutcome.OverCap && r.RoomLeft == 100 && Balance(db, Me) == 10_000, $"pay over the cap: {r}");
            Expect(ProjectSYNCS.Commands.EconomyModule.PayReply(r, Me, You).Text.Contains("100 cailloux"), "over cap does not name the room left");
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 200, now.AddDays(1));
            Expect(r.Outcome == PayOutcome.Paid, $"pay the next Paris day: {r}");
            using var c2 = db.New();
            var w = c2.PebbleWallets.Single(x => x.UserId == Me);
            Expect(w.PayDay == AppTime.DayKey(now.AddDays(1)) && w.PaidToday == 200, $"pay stamp: {w.PayDay} {w.PaidToday}");
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 9, now.AddDays(1));
            Expect(r.Outcome == PayOutcome.TooSmall, $"pay 9: {r}");
            Expect(ProjectSYNCS.Commands.EconomyModule.PayReply(r, Me, You).Refused, "pay 9 answered publicly");
        }

        // A self-pay that gets past the command is still refused, before any tax is burned.
        using (var db = new Db())
        {
            db.Wallet(Me, 1000);
            PayResult r;
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, Me, 100, now);
            Expect(r.Outcome == PayOutcome.Self && Balance(db, Me) == 1000, $"self-pay in the service: {r}, balance {Balance(db, Me)}");
            var (selfText, selfRefused) = ProjectSYNCS.Commands.EconomyModule.PayReply(r, Me, Me);
            Expect(selfRefused && selfText == ProjectSYNCS.Commands.EconomyModule.PaySelfRefusal, "self-pay reply: " + selfText);
            using var c2 = db.New();
            Expect(!c2.MemberXps.Any(x => x.CaillouxSpent > 0), "a self-pay counted as spent");
        }

        // Room left under the minimum: nothing more can be sent today, so no amount is suggested.
        var under = ProjectSYNCS.Commands.EconomyModule.PayReply(new PayResult(PayOutcome.OverCap, 10, 1, 5000, 5), Me, You).Text;
        Expect(!under.Contains("ne peux plus donner que") && under.Contains("minuit"), "room 5 under the minimum: " + under);

        // The cap resets at midnight Paris time, not UTC: 22:30 UTC on 17 Nov is still the 17th in Paris
        // (23:30), 23:30 UTC is already the 18th (00:30).
        using (var db = new Db())
        {
            var noon17 = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
            using (var c = db.New())
            {
                c.PebbleWallets.Add(new PebbleWallet { GuildId = G, UserId = Me, Balance = 10_000, PayDay = AppTime.DayKey(noon17), PaidToday = 1900 });
                c.SaveChanges();
            }
            PayResult r;
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 200, new DateTimeOffset(2026, 11, 17, 22, 30, 0, TimeSpan.Zero));
            Expect(r.Outcome == PayOutcome.OverCap, $"23:30 Paris is still the same day: {r}");
            using (var c = db.New()) r = await new PebbleService(c).PayAsync(G, Me, You, 200, new DateTimeOffset(2026, 11, 17, 23, 30, 0, TimeSpan.Zero));
            Expect(r.Outcome == PayOutcome.Paid, $"00:30 Paris is a new day: {r}");
        }
    }

    // What a pack really returns, with the best possible player: the discovery pays a set's reward and
    // keeps the items (as InventoryService.AddAsync does), then every turn-in spends one of each for the
    // reward again, the moment the set is whole; whatever is left is sold. The price must stay well
    // above it, or buying packs makes money. 5 000 packs over a year, so every season shows up.
    private static double PackReturn(int packs)
    {
        var rng = new Random(52);
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var held = new Dictionary<string, int>();
        var discovered = new HashSet<string>();
        long returned = 0;
        for (var p = 0; p < packs; p++)
        {
            var at = start.AddDays(365.0 * p / packs);
            for (var d = 0; d < ItemCatalog.PackSize; d++)
            {
                var item = ItemCatalog.DrawCollectible(rng, at);
                held[item.Key] = held.GetValueOrDefault(item.Key) + 1;
                if (item.Set is not { } setKey) continue;
                var set = ItemCatalog.InSet(setKey).ToList();
                if (!set.All(i => held.GetValueOrDefault(i.Key) > 0)) continue;
                var reward = ItemCatalog.Sets.First(s => s.Key == setKey).Reward;
                if (discovered.Add(setKey)) returned += reward;
                foreach (var i in set) held[i.Key]--;
                returned += reward;
            }
        }
        returned += held.Sum(h => h.Value * ItemCatalog.SellPrice(ItemCatalog.ByKey(h.Key)!.Rarity));
        return returned / (double)packs;
    }

    private static void PackPrice()
    {
        // Against the cheapest a pack can be bought: by five, with the bulk discount.
        var back = PackReturn(5000);
        var unit = ItemCatalog.PackCost(5) / 5.0;
        Console.WriteLine($"pack: {unit:0.#} cailloux by five, returns {back:0.0} at best ({back / unit:P0})");
        Expect(back <= 0.7 * unit, $"a pack returns {back:0.0}, more than 70 % of its {unit:0.#}");
        Expect(ItemCatalog.PackCost(1) == ItemCatalog.PackPrice && ItemCatalog.PackCost(5) == 675, $"pack costs: {ItemCatalog.PackCost(1)}, {ItemCatalog.PackCost(5)}");
    }

    private static async Task PacksAsync()
    {
        var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var today = AppTime.DayKey(now);
        long Metric(AppDbContext c, string m) => c.EconomyDailyStats.SingleOrDefault(s => s.Metric == m && s.Day == today)?.Value ?? 0;

        using (var db = new Db())
        {
            db.Wallet(Me, 200);
            PackResult r;
            using (var c = db.New()) r = await new InventoryService(c, null!).OpenPackAsync(G, Me, 1, now, new Random(7));
            Expect(r.Outcome == PackOutcome.Opened && r.Finds.Count == 3 && r.Finds.All(f => f.Item.Kind == ItemKind.Collectible),
                $"pack ×1: {r.Outcome}, {r.Finds.Count} finds");
            Expect(r.Price == ItemCatalog.PackPrice && r.Balance == 200 - ItemCatalog.PackPrice, $"pack ×1 price {r.Price}, balance {r.Balance}");
            using var c2 = db.New();
            Expect(Metric(c2, EconomyLog.SpendPack) == ItemCatalog.PackPrice, "spend.pack today: " + Metric(c2, EconomyLog.SpendPack));
            Expect(Metric(c2, EconomyLog.ItemFound) == 3, "item.found today: " + Metric(c2, EconomyLog.ItemFound));
            Expect(!c2.MemberXps.Any(x => x.UserId == Me && x.CaillouxSpent > 0), "a pack counted toward Mécène");
            Expect(c2.InventoryItems.Where(i => i.UserId == Me).Sum(i => i.Quantity) == 3, "the pack's finds are not in the inventory");
            Expect(!c2.InventoryItems.Any(i => i.Key == ItemCatalog.PackKey), "a pack was stored as an item");
            Expect(ProjectSYNCS.Commands.ShopCards.PackNotice(r).Contains(r.Finds[0].Item.Name), "the pack notice does not list its finds");
        }

        // Five packs at once: the discounted price, and a receipt that fits a message.
        using (var db = new Db())
        {
            db.Wallet(Me, 1000);
            PackResult r;
            using (var c = db.New()) r = await new InventoryService(c, null!).OpenPackAsync(G, Me, 5, now, new Random(7));
            Expect(r.Outcome == PackOutcome.Opened && r.Price == 675 && r.Balance == 1000 - 675 + r.Finds.SelectMany(f => f.Completed).Sum(s => s.Reward),
                $"pack ×5: {r.Outcome}, price {r.Price}, balance {r.Balance}");
            var receipt = ProjectSYNCS.Commands.ShopCards.Receipt(ProjectSYNCS.Commands.ShopCards.PackNotice(r));
            Expect(receipt.Length <= 2000, $"pack ×5 receipt is {receipt.Length} characters");
        }

        using (var db = new Db())
        {
            db.Wallet(Me, ItemCatalog.PackCost(5) - 1);
            PackResult r;
            using (var c = db.New()) r = await new InventoryService(c, null!).OpenPackAsync(G, Me, 5, now, new Random(7));
            Expect(r.Outcome == PackOutcome.TooPoor && r.Price == ItemCatalog.PackCost(5) && r.Finds.Count == 0, $"pack ×5 too poor: {r.Outcome} {r.Price}");
            using (var c = db.New()) r = await new InventoryService(c, null!).OpenPackAsync(G, Me, 2, now, new Random(7));
            Expect(r.Outcome == PackOutcome.BadQuantity, "pack ×2: " + r.Outcome);
            using var c2 = db.New();
            Expect(!c2.InventoryItems.Any() && c2.PebbleWallets.Single(w => w.UserId == Me).Balance == ItemCatalog.PackCost(5) - 1, "a refused pack moved something");
            Expect(ProjectSYNCS.Commands.ShopCards.PackNotice(new PackResult(PackOutcome.TooPoor, 300, 299, Array.Empty<ItemFind>())).Contains("299"), "too poor does not name the balance");
        }

        // The pack supplies the last missing item of a set: the set is paid in the same save.
        using (var db = new Db())
        {
            var first = ItemCatalog.DrawCollectible(new Random(7), now);
            var set = ItemCatalog.Sets.First(s => s.Key == first.Set);
            db.Wallet(Me, 200);
            using (var c = db.New())
            {
                foreach (var item in ItemCatalog.InSet(set.Key).Where(i => i.Key != first.Key))
                    c.InventoryItems.Add(new InventoryItem { GuildId = G, UserId = Me, Key = item.Key, Quantity = 1, DiscoveredAt = now });
                c.SaveChanges();
            }
            PackResult r;
            using (var c = db.New()) r = await new InventoryService(c, null!).OpenPackAsync(G, Me, 1, now, new Random(7));
            Expect(r.Finds[0].Item.Key == first.Key && r.Finds[0].Completed.Contains(set), $"pack completing {set.Key}: {r.Finds[0].Item.Key}");
            Expect(r.Balance == 200 - ItemCatalog.PackPrice + set.Reward, $"pack completing a set: balance {r.Balance}");
            Expect(ProjectSYNCS.Commands.ShopCards.PackNotice(r).Contains(set.Name), "the pack notice does not name the completed set");
        }

        // The shop card with the pack picked: its two buttons, each with its own id.
        var held = ItemCatalog.Shop.ToDictionary(s => s.Item.Key, _ => 9999);
        var (embed, components) = ProjectSYNCS.Commands.ShopCards.BuildShop(held, 1_000_000, ItemCatalog.PackKey, new string('x', 200));
        var parts = components.Components.OfType<Discord.ActionRowComponent>().SelectMany(row => row.Components).ToList();
        var ids = parts.OfType<Discord.ButtonComponent>().Select(b => b.CustomId).ToList();
        Expect(parts.Count == 3 && ids.SequenceEqual(new[] { "shop:pack:1", "shop:pack:5" }), "shop with the pack picked: " + string.Join(",", ids));
        Expect(embed.Length <= 6000 && embed.Fields.All(f => f.Value.Length <= 1024), $"shop with the pack: {embed.Length} chars");
        var (_, poor) = ProjectSYNCS.Commands.ShopCards.BuildShop(held, ItemCatalog.PackPrice, ItemCatalog.PackKey, null);
        var poorButtons = poor.Components.OfType<Discord.ActionRowComponent>().SelectMany(row => row.Components).OfType<Discord.ButtonComponent>().ToList();
        Expect(poorButtons.Count == 2 && !poorButtons[0].IsDisabled && poorButtons[1].IsDisabled, "pack buttons are not disabled by price");
    }

    private static void Badge()
    {
        var now = DateTimeOffset.UtcNow;
        MemberBadgeFacts Facts(long spent) => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, null, new Dictionary<string, StoredBadge>(), CaillouxSpent: spent);
        BadgeStanding Mecene(long spent) => MemberBadges.Evaluate(Facts(spent), now).Single(s => s.Info.Key == "mecene");
        var silver = Mecene(5_000);
        Expect(silver.Tier == CollectionTier.Silver && silver.Value == 5000, $"mécène at 5 000: {silver.Tier} {silver.Value}");
        Expect(Mecene(499).Tier == CollectionTier.None, "mécène at 499 has a tier");
        Expect(Mecene(99_999).Tier == CollectionTier.Platinum && Mecene(100_000).Tier == CollectionTier.Diamond, "mécène: Diamant is 100 000");
        Expect(!MemberBadges.IsAwardable(G, "mecene"), "mécène is awardable (it is derived, never stored)");
    }

    private static async Task SpentCounterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var today = AppTime.DayKey(now);
        using var db = new Db();
        db.Wallet(Me, 1000);

        // Only the jar counts: the shop spends without touching Mécène.
        using (var c = db.New())
        {
            var (outcome, price, _) = await new InventoryService(c, null!).BuyAsync(G, Me, ItemCatalog.SoapKey, 5, now);
            Expect(outcome == InventoryService.ShopOutcome.Bought && price == 90, $"soap ×5: {outcome} {price}");
        }
        using (var c = db.New())
            Expect(!await c.MemberXps.AnyAsync(x => x.UserId == Me && x.CaillouxSpent > 0), "the shop counted toward Mécène");

        using (var c = db.New()) await new JarService(c).GiveAsync(G, Me, 90, now);
        using (var c = db.New())
        {
            var xp = await c.MemberXps.SingleAsync(x => x.GuildId == G && x.UserId == Me);
            var day = await c.MemberDailyStats.SingleAsync(x => x.GuildId == G && x.UserId == Me && x.Day == today);
            Expect(xp.CaillouxSpent == 90 && day.CaillouxSpent == 90, $"jar gift counted: total {xp.CaillouxSpent}, today {day.CaillouxSpent}");
            Expect(xp.TotalXp == 0 && day.XpEarned == 0, "a jar gift grants XP");
        }

        using (var c = db.New()) await new JarService(c).GiveAsync(G, Me, 20, now);
        using (var c = db.New())
        {
            Expect(await c.MemberXps.CountAsync(x => x.UserId == Me) == 1, "a second gift makes a second MemberXp row");
            Expect(await c.MemberDailyStats.CountAsync(x => x.UserId == Me) == 1, "a second gift makes a second bucket");
            Expect((await c.MemberXps.SingleAsync(x => x.UserId == Me)).CaillouxSpent == 110, "a second gift does not add up");
        }

        using (var c = db.New()) await new PebbleService(c).AdjustAsync(G, Me, -100);
        using (var c = db.New())
            Expect((await c.MemberXps.SingleAsync(x => x.UserId == Me)).CaillouxSpent == 110, "an admin debit counts toward Mécène");

        // A member whose only row comes from a jar gift is unranked, and not active.
        using (var c = db.New())
        {
            Expect(await new XpService(c).GetRankAsync(G, Me) is null, "0 XP spender gets a rank");
            Expect(await new XpService(c).LastActiveDayBeforeAsync(G, Me, today + 1) is null, "a spent-only bucket counts as activity");
        }
    }
    // Two first spends of the day racing: the slower one's insert of today's rows hits the unique
    // index (a plain DbUpdateException, not a concurrency one). It must re-run from a clean slate,
    // find the rows the other one saved and add to them, rather than fail the click.
    private static async Task RetryAsync()
    {
        var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        using var db = new Db();
        var attempts = 0;
        using (var c = db.New())
        {
            await EconomySave.RetryAsync(c, async () =>
            {
                attempts++;
                await Patronage.AddAsync(c, G, Me, 30, now);
                if (attempts == 1)
                {
                    using var other = db.New();
                    await Patronage.AddAsync(other, G, Me, 20, now);
                    await other.SaveChangesAsync();
                }
                await c.SaveChangesAsync();
                return 0;
            });
        }
        using var c2 = db.New();
        Expect(attempts == 2, $"unique-index race: {attempts} attempt(s)");
        Expect(c2.MemberXps.Single(x => x.UserId == Me).CaillouxSpent == 50, "unique-index race: total " + c2.MemberXps.Single(x => x.UserId == Me).CaillouxSpent);
        Expect(c2.MemberDailyStats.Single(x => x.UserId == Me).CaillouxSpent == 50, "unique-index race: today's bucket");
    }

    // The /leaderboard page at its fullest, every view and window: Components V2 allows 40 components
    // in the whole tree (thumbnails and buttons count), 4000 characters, no custom-id twice.
    private static void LeaderboardCard()
    {
        const long huge = 9_999_999_999;
        var full = Enumerable.Range(1, 12).Select(i => new MemberTally((ulong)i, huge, huge, huge, huge, huge)).ToList();
        var worst = 0;
        foreach (var view in Enum.GetValues<LeaderboardView>())
        foreach (var period in Enum.GetValues<StatsPeriod>())
        foreach (var ranking in new[] { full, new List<MemberTally>() })
        {
            var card = ProjectSYNCS.Commands.LevelModule.BuildPage(ranking, view, period, 1, 7, _ => null);
            var parts = card.Components.Select(Measure).ToList();
            var (text, count, ids) = (parts.Sum(p => p.Text), parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
            worst = Math.Max(worst, count);
            Expect(text <= 4000 && count <= 40 && ids.Count == ids.Distinct().Count(),
                $"leaderboard {view}/{period}/{ranking.Count}: {text} chars, {count} components, {ids.Count - ids.Distinct().Count()} duplicate id(s)");
        }
        Console.WriteLine($"leaderboard page at worst: {worst} components");
    }

    internal static (int Text, int Count, List<string> Ids) Measure(Discord.IMessageComponent c)
    {
        var text = 0; var count = 1; var ids = new List<string>();
        void Add((int Text, int Count, List<string> Ids) r) { text += r.Text; count += r.Count; ids.AddRange(r.Ids); }
        switch (c)
        {
            case Discord.ContainerComponent x: foreach (var k in x.Components) Add(Measure(k)); break;
            case Discord.SectionComponent x:
                foreach (var k in x.Components) Add(Measure(k));
                if (x.Accessory is not null) Add(Measure(x.Accessory));
                break;
            case Discord.ActionRowComponent x: foreach (var k in x.Components) Add(Measure(k)); break;
            case Discord.TextDisplayComponent x: text += x.Content.Length; break;
            case Discord.ButtonComponent x: if (x.CustomId is not null) ids.Add(x.CustomId); break;
        }
        return (text, count, ids);
    }
}
