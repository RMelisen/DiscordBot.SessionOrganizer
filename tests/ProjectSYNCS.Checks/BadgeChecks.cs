using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 34. Member badges.
public static class BadgeChecks
{
    public static async Task Run()
    {
        // 34. Member badges: the catalog, tiers at their edges, derived moments, icon keys.
        var mb = MemberBadges.All;
        if (mb.Count(b => b.Kind == MemberBadgeKind.Palier) != 14 || mb.Count(b => b.Kind == MemberBadgeKind.Moment) != 6
            || mb.Count(b => b.Kind == MemberBadgeKind.Secret) != 13) Fail("member badges: catalog counts");
        if (mb.Select(b => b.Key).Distinct().Count() != mb.Count) Fail("member badges: duplicate key");
        foreach (var b in mb)
        {
            if (b.Kind == MemberBadgeKind.Palier != (b.Thresholds.Count == 5)) Fail($"{b.Key}: thresholds");
            if (b.Thresholds.Zip(b.Thresholds.Skip(1)).Any(p => p.First >= p.Second)) Fail($"{b.Key}: thresholds not ascending");
            if (string.IsNullOrWhiteSpace(b.Name) || string.IsNullOrWhiteSpace(b.Hint) || (b.Name + b.Hint).IndexOfAny(['{', '}']) >= 0) Fail($"{b.Key}: name/hint");
            var mbTiers = b.Kind == MemberBadgeKind.Palier ? Enum.GetValues<CollectionTier>().Skip(1).ToArray() : new[] { CollectionTier.None };
            foreach (var t in mbTiers)
            {
                var key = MemberBadges.IconKey(b, t);
                var name = ItemEmojis.EmojiName(key);
                if (!MemberBadges.IsIconKey(key) || name is null || name.Length > 32 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail($"{key}: icon key");
                if (ItemCatalog.Sets.Any(s => Enum.GetValues<CollectionTier>().Any(ct => ItemEmojis.BadgeKey(s.Key, ct) == key))) Fail($"{key}: clashes with a collection badge");
            }
        }
        var mbNow = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        MemberBadgeFacts MbFacts(long voice = 0, long bestMs = 0, DateTimeOffset? joined = null, Dictionary<string, StoredBadge>? stored = null, long daily = 0) =>
            new(voice, 0, 0, bestMs, 0, 0, 0, 0, 0, 0, false, joined, stored ?? new(), DailyBest: daily);
        BadgeStanding MbOf(MemberBadgeFacts f, string key) => MemberBadges.Evaluate(f, mbNow).Single(s => s.Info.Key == key);
        if (MbOf(MbFacts(voice: 599), "voice").Earned || MbOf(MbFacts(voice: 600), "voice").Tier != CollectionTier.Bronze
            || MbOf(MbFacts(voice: 60000), "voice").Tier != CollectionTier.Diamond) Fail("voice tier edges");
        if (MbOf(MbFacts(), "seniority").Earned) Fail("seniority null");
        if (MbOf(MbFacts(joined: mbNow.AddDays(-90)), "seniority").Tier != CollectionTier.Bronze) Fail("seniority 90 days");
        if (!MbOf(MbFacts(bestMs: 2999), "lightning").Earned || MbOf(MbFacts(bestMs: 3000), "lightning").Earned
            || MbOf(MbFacts(bestMs: 0), "lightning").Earned) Fail("lightning edges");
        // « Assiduité »: the best /daily streak, so a reset never takes a tier away.
        if (MbOf(MbFacts(daily: 6), "daily").Earned || MbOf(MbFacts(daily: 7), "daily").Tier != CollectionTier.Bronze
            || MbOf(MbFacts(daily: 30), "daily").Tier != CollectionTier.Silver || MbOf(MbFacts(daily: 365), "daily").Tier != CollectionTier.Diamond) Fail("daily badge tiers");
        var mbStored = new Dictionary<string, StoredBadge> { ["arcade"] = new(5, mbNow), ["hush"] = new(1, mbNow), ["gone_key"] = new(1, mbNow) };
        var mbAll = MemberBadges.Evaluate(MbFacts(stored: mbStored), mbNow);
        if (MbOf(MbFacts(stored: mbStored), "arcade").Tier != CollectionTier.Bronze || !MbOf(MbFacts(stored: mbStored), "hush").Earned) Fail("stored badges");
        if (mbAll.Count != mb.Count || mbAll.Count(s => s.Earned) != 2) Fail("unknown stored key");
        if (MemberBadges.IsAwardable(0, "hush") || MemberBadges.IsAwardable(1, "nope") || MemberBadges.IsAwardable(1, "voice")
            || !MemberBadges.IsAwardable(1, "nanny") || !MemberBadges.IsAwardable(1, "crowned")) Fail("IsAwardable");

        // The card line and the « Succès » view, at their widest.
        var mbEmpty = MemberBadges.Evaluate(MbFacts(), mbNow);
        if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(new List<ProjectSYNCS.Models.CollectionCompletion>(), mbEmpty) is not null) Fail("empty card line");
        // Worst case: everything at Diamant, every secret found, every icon a real emoji markup.
        var mbMax = new MemberBadgeFacts(99_999_999, 99_999_999, 99_999, 1, 99_999_999, 99_999, 99_999, 99_999, 99_999_999, 99_999, true,
            mbNow.AddYears(-20), mb.ToDictionary(b => b.Key, _ => new StoredBadge(99_999_999, mbNow)), DailyBest: 99_999, CaillouxSpent: 99_999_999);
        var mbDone = ItemCatalog.Sets.Select(s => new ProjectSYNCS.Models.CollectionCompletion { SetKey = s.Key, Count = 999 }).ToList();
        foreach (var b in mb) foreach (var t in Enum.GetValues<CollectionTier>())
            ItemEmojis.Set(MemberBadges.IconKey(b, t), ItemEmojis.Markup("b_" + new string('x', 30), ulong.MaxValue));
        (int Text, int Count, List<string> Ids) V2Measure(Discord.IMessageComponent c)
        {
            var text = 0; var count = 1; var ids = new List<string>();
            void Add((int Text, int Count, List<string> Ids) r) { text += r.Text; count += r.Count; ids.AddRange(r.Ids); }
            switch (c)
            {
                case Discord.ContainerComponent x: foreach (var k in x.Components) Add(V2Measure(k)); break;
                case Discord.SectionComponent x:
                    foreach (var k in x.Components) Add(V2Measure(k));
                    if (x.Accessory is not null) Add(V2Measure(x.Accessory));
                    break;
                case Discord.ActionRowComponent x: foreach (var k in x.Components) Add(V2Measure(k)); break;
                case Discord.TextDisplayComponent x: text += x.Content.Length; break;
                case Discord.ButtonComponent x: if (x.CustomId is not null) ids.Add(x.CustomId); break;
            }
            return (text, count, ids);
        }
        var mbWidest = 0;
        foreach (var (facts, done, what) in new[] { (MbFacts(), new List<ProjectSYNCS.Models.CollectionCompletion>(), "empty view"), (mbMax, mbDone, "worst view") })
        foreach (var section in Enum.GetValues<ProjectSYNCS.Commands.BadgeSection>())
        {
            var view = ProjectSYNCS.Commands.MemberBadgeCards.BuildView(ulong.MaxValue, new string('*', 32), section, done, MemberBadges.Evaluate(facts, mbNow));
            var parts = view.Components.Select(V2Measure).ToList();
            var (text, count, ids) = (parts.Sum(p => p.Text), parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
            mbWidest = Math.Max(mbWidest, text);
            if (text > 4000 || count > 40 || ids.Count != ids.Distinct().Count()) Fail($"{what} {section}: {text} chars, {count} components");
        }
        if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(mbDone, MemberBadges.Evaluate(mbMax, mbNow)) is not { } mbLine || mbLine.Length > 4000) Fail("full card line");
        const ulong mbSelf = 111, mbAsker = 222, mbOther = 333;
        var mbTata = (ulong)br.GetField("TataId")!.GetValue(null)!; // BotResponses is internal: reflection, like the rest of the file
        bool Heart(string a, string b, ulong asker = mbAsker) => Shipping.IsSelfWithHer(a, b, asker, mbSelf, AvailabilityService.OwnerId, mbTata);
        if (!Heart("syncs", "moi") || !Heart("moi", "toi") || !Heart($"<@{mbAsker}>", "syncs")
            || !Heart("moi", "syncs", AvailabilityService.OwnerId)) Fail("heart: self with her");
        if (Heart("syncs", "pizza") || Heart("syncs", $"<@{mbOther}>") || Heart("moi", "pizza") || Heart("syncs", "toi")) Fail("heart: not self with her");
        var flip = new FeedbackFlip();
        if (flip.Record(1, FeedbackKind.Good, mbNow) || !flip.Record(1, FeedbackKind.Bad, mbNow.AddSeconds(30))) Fail("flip within a minute");
        if (flip.Record(2, FeedbackKind.Good, mbNow) || flip.Record(2, FeedbackKind.Bad, mbNow.AddSeconds(61))) Fail("flip after a minute");
        if (flip.Record(3, FeedbackKind.Good, mbNow) || flip.Record(3, FeedbackKind.Good, mbNow.AddSeconds(5))) Fail("same verdict twice");
        if (flip.Record(4, FeedbackKind.Good, mbNow) || flip.Record(5, FeedbackKind.Bad, mbNow.AddSeconds(5))) Fail("two people");
        ItemEmojis.Clear();

        // Redrawn icons: each revision is a sprite the bot ships, uploaded under a valid new name.
        var iconDir = Enumerable.Range(0, 8)
            .Select(up => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, string.Concat(Enumerable.Repeat("../", up)), "ProjectSYNCS", "Assets", "Icons")))
            .FirstOrDefault(Directory.Exists);
        if (iconDir is null) Fail("ProjectSYNCS/Assets/Icons not found above the checks");
        else
        {
            var shipped = ItemEmojis.Sources("", iconDir).ToDictionary(s => s.Key, s => s.EmojiName);
            foreach (var key in ItemEmojis.RevisedKeys)
                if (!shipped.TryGetValue(key, out var name) || name is null || !name.Contains("_v") || name.Length > 32
                    || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail($"{key}: revised icon not shipped, or its emoji name '{name}' is invalid");
            // A set's icon is drawn on its badges, so a redrawn set needs every badge revised with it.
            var revised = ItemEmojis.RevisedKeys.ToHashSet();
            foreach (var s in ItemCatalog.Sets.Where(s => revised.Contains(ItemEmojis.SetKey(s.Key))))
            foreach (var t in Enum.GetValues<CollectionTier>().Skip(1))
                if (!revised.Contains(ItemEmojis.BadgeKey(s.Key, t))) Fail($"{s.Key}: set redrawn but {ItemEmojis.BadgeKey(s.Key, t)} not revised");

            // Stale emojis, deleted on start: the bot's own names that no shipped sprite claims, never
            // a current one, never one outside its prefixes, and nothing at all without sprites.
            var mushroomDir = Path.Combine(Path.GetDirectoryName(iconDir)!, "Mushrooms");
            var all = ItemEmojis.Sources(mushroomDir, iconDir).ToList();
            var inUse = all.Select(s => s.EmojiName).OfType<string>().ToList();
            string[] old = { "tr_shy", "tr2_shy", "c_abeille", "b_generous", "ac_sac_a_dos", "shroom_nope" };
            string[] foreign = { "syncs_happy", "pepe", "trophy", "c", "tr2" };
            var stale = ItemEmojis.StaleNames(inUse.Concat(old).Concat(foreign), all);
            if (!stale.ToHashSet().SetEquals(old)) Fail($"stale emojis: got [{string.Join(", ", stale)}], expected [{string.Join(", ", old)}]");
            if (ItemEmojis.StaleNames(old, Array.Empty<IconSource>()).Count != 0) Fail("stale emojis: sprites missing must delete nothing");
        }

        // GetFactsAsync against a real SQLite: the navigations, the Plynling join and the projection must
        // translate, and the guild scoping must hold. A query that does not translate breaks /level for everyone.
        using (var mbConn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:"))
        {
            mbConn.Open();
            var mbOpts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ProjectSYNCS.Data.AppDbContext>().UseSqlite(mbConn).Options;
            const ulong gA = 1000, gB = 2000, me = 42, other = 43;
            using (var seed = new ProjectSYNCS.Data.AppDbContext(mbOpts))
            {
                seed.Database.EnsureCreated();
                var at = mbNow;
                ProjectSYNCS.Models.SessionEvent Ses(ulong guild, ulong org, DateTimeOffset when, bool cancelled = false) =>
                    new() { GuildId = guild, OrganizerId = org, Title = "s", ScheduledAt = when, IsCancelled = cancelled };
                ProjectSYNCS.Models.Participant Part(ulong user, DateTimeOffset? seen) =>
                    new() { UserId = user, Username = "u", FirstSeenInVoiceAt = seen };
                // Organised by me in guild A: two past (counted), one cancelled, one future; one past in guild B.
                var sPunctual = Ses(gA, me, at.AddDays(-5)); sPunctual.Participants.Add(Part(me, at.AddDays(-5).AddMinutes(-3)));
                var sLate = Ses(gA, me, at.AddDays(-4)); sLate.Participants.Add(Part(me, at.AddDays(-4).AddMinutes(10)));
                var sCancelled = Ses(gA, me, at.AddDays(-3), cancelled: true); sCancelled.Participants.Add(Part(me, at.AddDays(-3).AddMinutes(-5)));
                var sFuture = Ses(gA, me, at.AddDays(2));
                var sOtherGuild = Ses(gB, me, at.AddDays(-2)); sOtherGuild.Participants.Add(Part(me, at.AddDays(-2).AddMinutes(-5)));
                var sNotSeen = Ses(gA, other, at.AddDays(-1)); sNotSeen.Participants.Add(Part(me, null));
                seed.SessionEvents.AddRange(sPunctual, sLate, sCancelled, sFuture, sOtherGuild, sNotSeen);

                var gw = new ProjectSYNCS.Models.Giveaway { GuildId = gA, Prize = "p", IsClosed = true };
                gw.Entries.Add(new() { UserId = me, IsWinner = true });
                gw.Entries.Add(new() { UserId = other, IsWinner = true });
                var gwLost = new ProjectSYNCS.Models.Giveaway { GuildId = gA, Prize = "p", IsClosed = true };
                gwLost.Entries.Add(new() { UserId = me, IsWinner = false });
                seed.Giveaways.Add(gwLost);
                var gwB = new ProjectSYNCS.Models.Giveaway { GuildId = gB, Prize = "p", IsClosed = true };
                gwB.Entries.Add(new() { UserId = me, IsWinner = true });
                seed.Giveaways.AddRange(gw, gwB);

                var plMine = new ProjectSYNCS.Models.Plynling { GuildId = gA, OwnerId = me, Name = "a" };
                var plOther = new ProjectSYNCS.Models.Plynling { GuildId = gA, OwnerId = other, Name = "b" };
                var plOtherGuild = new ProjectSYNCS.Models.Plynling { GuildId = gB, OwnerId = me, Name = "c" };
                seed.Plynlings.AddRange(plMine, plOther, plOtherGuild);
                seed.SaveChanges();
                seed.PlynlingBadges.AddRange(
                    new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plMine.Id, Key = "k1", EarnedAt = at },
                    new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plMine.Id, Key = "k2", EarnedAt = at },
                    new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plOther.Id, Key = "k1", EarnedAt = at },
                    new ProjectSYNCS.Models.PlynlingBadge { PlynlingId = plOtherGuild.Id, Key = "k1", EarnedAt = at });

                seed.MemberXps.Add(new() { GuildId = gA, UserId = me, VoiceMinutes = 700, ReactionsUsed = 9 });
                seed.MemberXps.Add(new() { GuildId = gB, UserId = me, VoiceMinutes = 5, ReactionsUsed = 1 });
                seed.VoiceCompanionStats.Add(new() { GuildId = gA, UserId = me, Minutes = 33 });
                seed.QuizStats.Add(new() { GuildId = gA, UserId = me, Wins = 4, BestMs = 1500 });
                seed.BotFeedbacks.Add(new() { GuildId = gA, UserId = me, GoodCount = 6 });
                seed.MemberBadges.Add(new() { GuildId = gA, UserId = me, Key = "arcade", Count = 3, EarnedAt = at });
                seed.MemberBadges.Add(new() { GuildId = gB, UserId = me, Key = "hush", Count = 1, EarnedAt = at });
                seed.SaveChanges();
            }
            using var read = new ProjectSYNCS.Data.AppDbContext(mbOpts);
            try
            {
                var f = await new MemberBadgeService(read).GetFactsAsync(gA, me, mbNow.AddDays(-100), mbNow);
                if (f.VoiceMinutes != 700 || f.Reactions != 9) Fail($"facts sql: xp {f.VoiceMinutes}/{f.Reactions}");
                if (f.CompanionMinutes != 33 || f.QuizWins != 4 || f.QuizBestMs != 1500 || f.GoodBot != 6) Fail("facts sql: companion/quiz/goodbot");
                if (f.GiveawaysWon != 1) Fail($"facts sql: giveaways won {f.GiveawaysWon}");
                if (f.PlynlingBadges != 2) Fail($"facts sql: plynling badges {f.PlynlingBadges}");
                if (f.SessionsOrganised != 2) Fail($"facts sql: organised {f.SessionsOrganised}");
                if (f.SessionsAttended != 2) Fail($"facts sql: attended {f.SessionsAttended}");
                if (!f.Punctual) Fail("facts sql: punctual");
                if (f.Stored.Count != 1 || f.Stored["arcade"].Count != 3) Fail("facts sql: stored badges");
                var none = await new MemberBadgeService(read).GetFactsAsync(gA, 999, null, mbNow);
                if (none.VoiceMinutes != 0 || none.SessionsAttended != 0 || none.Punctual || none.Stored.Count != 0) Fail("facts sql: stranger");
            }
            catch (Exception ex) { Fail("facts sql: " + ex.Message); }
        }
        Console.WriteLine($"succès view at worst: {mbWidest} chars");
    }
}
