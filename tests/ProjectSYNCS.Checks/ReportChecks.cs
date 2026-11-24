using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 23. /report and the monthly digest: windows, when the digest is due, the session cards.
public static class ReportChecks
{
    public static void Run()
    {
        // 23. Reports (/report, the monthly digest): their windows, when the digest is due, the session
        // rules shared with /recap, and every embed measured at its worst against Discord's caps.
        var noon = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
        void WindowIs(ReportWindow? w, int from, int to, string what)
        {
            if (w is null || w.FromDay != from || w.ToDay != to) Fail($"{what}: {w?.FromDay}..{w?.ToDay}, expected {from}..{to}");
        }
        var week = ReportWindow.From(StatsPeriod.Week, noon);
        WindowIs(week, 20261013, 20261019, "week");
        WindowIs(week.Previous, 20261006, 20261012, "week before");
        var month = ReportWindow.From(StatsPeriod.Month, noon);
        WindowIs(month, 20260920, 20261019, "30 days");
        WindowIs(month.Previous, 20260821, 20260919, "30 days before");
        if (ReportWindow.From(StatsPeriod.AllTime, noon).Previous is not null) Fail("all time has a previous window");
        var feb = ReportWindow.CalendarMonth(2026, 2);
        WindowIs(feb, 20260201, 20260228, "February");
        WindowIs(feb.Previous, 20260101, 20260131, "the month before February");
        if (feb.Label != "février 2026") Fail($"month label '{feb.Label}'");
        if (ReportWindow.CalendarMonth(2026, 10).Days(0).Count != 31) Fail("October (clocks change) is not 31 days");
        // 22:30 UTC on the 19th is already the 20th in Paris.
        if (week.Contains(new DateTimeOffset(2026, 10, 19, 22, 30, 0, TimeSpan.Zero))) Fail("window ignores the Paris day");

        void DueIs(DateTimeOffset now, int last, (int, int)? expected)
        {
            var got = MonthlyDigest.Due(now, last);
            if (got != expected) Fail($"digest due at {now:u} after {last}: {got}, expected {expected}");
        }
        DueIs(new DateTimeOffset(2026, 11, 1, 7, 30, 0, TimeSpan.Zero), 202609, null);          // 08:30 Paris: too early
        DueIs(new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero), 202609, (2026, 10));
        DueIs(new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero), 202610, null);          // already sent
        DueIs(new DateTimeOffset(2026, 11, 15, 20, 0, 0, TimeSpan.Zero), 202609, (2026, 10));   // she was down on the 1st
        DueIs(new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero), 202611, (2026, 12));

        // One roll call: on time, late, a no-show, a « peut-être » who never came, one who declined.
        var start = new DateTimeOffset(2026, 10, 15, 19, 0, 0, TimeSpan.Zero);
        ProjectSYNCS.Models.Participant Person(ulong id, ProjectSYNCS.Models.ParticipantStatus status, int? seenAfter, int dayOffset = 0) => new()
        {
            UserId = id, Status = status, FirstSeenInVoiceAt = seenAfter is { } m ? start.AddDays(dayOffset).AddMinutes(m) : null,
        };
        ProjectSYNCS.Models.SessionEvent Session(DateTimeOffset at, bool recap, bool cancelled, params ProjectSYNCS.Models.Participant[] people) => new()
        {
            GuildId = 1, OrganizerId = 9, ScheduledAt = at, RecapSent = recap, IsCancelled = cancelled, MaxPlayers = 4,
            Category = ProjectSYNCS.Models.SessionCategory.Game, Participants = people.ToList(),
        };
        var J = ProjectSYNCS.Models.ParticipantStatus.Joined;
        var M = ProjectSYNCS.Models.ParticipantStatus.Maybe;
        var sessions = new[]
        {
            Session(start, true, false, Person(1, J, 5), Person(2, J, 20), Person(3, J, null), Person(4, M, null),
                Person(5, ProjectSYNCS.Models.ParticipantStatus.Declined, null)),
            Session(start.AddDays(1), false, true, Person(1, J, null)),       // cancelled
            Session(start.AddDays(2), false, false, Person(6, J, null)),      // no roll called: not a no-show
            Session(start.AddDays(-1), true, false, Person(7, J, 10, -1)),    // exactly +10 min: on time
            Session(noon.AddHours(5), false, false, Person(8, J, null)),      // later today: not held yet
        };
        var sr = SessionReport.Compute(sessions, week, noon);
        if (sr.Held != 3 || sr.Cancelled != 1) Fail($"sessions held {sr.Held}, cancelled {sr.Cancelled}");
        if (sr.RollCalls != 2 || sr.Expected != 4 || sr.Came != 3) Fail($"roll {sr.RollCalls}, came {sr.Came}/{sr.Expected}");
        if (sr.Missed != 1 || sr.NoShows.Single().UserId != 3) Fail($"no-shows {sr.Missed}: {string.Join(",", sr.NoShows.Select(x => x.UserId))}");
        if (sr.Late != 1 || sr.Latecomers.Single().UserId != 2 || sr.MedianLateMinutes != 20) Fail($"late {sr.Late}, median {sr.MedianLateMinutes}");
        if (sr.Maybes != 1) Fail($"maybes {sr.Maybes}");

        // Her Wednesday poll that became a session, a dead poll, and a /tonight that did too.
        const ulong botId = 42;
        var weekendPoll = new ProjectSYNCS.Models.Poll
        {
            Id = 1, OrganizerId = botId, Kind = ProjectSYNCS.Models.PollKind.DateSlots, IsClosed = true, CreatedAt = start.AddDays(-2),
            Options = { new() { ScheduledAt = start, Votes = { new() { UserId = 1 }, new() { UserId = 2 } } } },
        };
        var deadPoll = new ProjectSYNCS.Models.Poll { Id = 2, OrganizerId = 1, Kind = ProjectSYNCS.Models.PollKind.Text, IsClosed = true, CreatedAt = start, Options = { new() } };
        var call = new ProjectSYNCS.Models.TonightCall
        {
            OrganizerId = 1, CreatedAt = start, ThresholdAnnounced = true, SessionEventId = 5,
            Responses = { new() { UserId = 1, Status = ProjectSYNCS.Models.TonightStatus.In }, new() { UserId = 2, Status = ProjectSYNCS.Models.TonightStatus.In } },
        };
        var pr = PollReport.Compute(new[] { weekendPoll, deadPoll }, new[] { call }, sessions, week, botId);
        if (pr.WeekendPosted != 1 || pr.WeekendWon != 1 || pr.WeekendBecameSession != 1) Fail($"weekend poll {pr.WeekendPosted}/{pr.WeekendWon}/{pr.WeekendBecameSession}");
        if (pr.Dead != 1) Fail($"dead polls {pr.Dead}");
        if (pr.TonightReached != 1 || pr.TonightBecameSession != 1 || pr.Keen.Single().UserId != 2) Fail("tonight tallies (the organizer must not count as keen)");

        // Worst case for every embed: full lists of the longest ids, long emote markup, both windows.
        const ulong big = 1_234_567_890_123_456_789;
        var people = Enumerable.Range(0, SessionReport.ListSize).Select(i => new PersonTally(big + (ulong)i, 999, 9999)).ToList();
        var slots = Enumerable.Range(0, 7).Select(i => new SlotTally(i, 99, 999, 999)).ToList();
        var sFull = new SessionReportData(999, 999,
            Enum.GetValues<ProjectSYNCS.Models.SessionCategory>().Select(c => (c, 999)).ToList(),
            9999, 9999, 0.87, 999, 9999, 9999, 999, 999, 123, slots, slots.Take(5).ToList(), people, people, people, people);
        var pFull = new PollReportData(999, 999, 99, 99, 12.5, 9, 9, 9, 999, 999, 999, 7.5, people, people);
        var xpBy = Enumerable.Range(0, 30).ToDictionary(i => big + (ulong)i, i => 1_000_000L + i);
        var emotes = Enumerable.Range(0, CommunityReport.EmoteCount)
            .Select(i => ($"<a:a_very_long_custom_emote_name_{i}:{big}>", 9_999_999L)).ToList();
        var cFull = new CommunityReportData(999, Enumerable.Repeat(50L, 30).ToList(), 99_999_999, 9_999_999, 9_999_999, 999, 999,
            emotes, 99, 99, 99, xpBy, xpBy, xpBy);
        var cFading = cFull with { XpByUser = new Dictionary<ulong, long>() };
        var outageKinds = Enum.GetValues<ProjectSYNCS.Models.UptimeEventKind>().Select(k => (k, 999)).ToList();
        var yFull = new SyncsReportData(30, 65_432, 81_234, 20261015, 9999, Enumerable.Repeat(70_000L, 30).ToList(), outageKinds,
            TimeSpan.FromHours(99), 0.987, 9_999_999, 999_999, 99_999, 99_999, 9_999_999, people);

        void Measure(string what, Discord.Embed embed, bool names)
        {
            if (embed.Length > 6000) Fail($"{what} embed is {embed.Length} characters");
            if (embed.Fields.Any(f => f.Value.Length > 1024 || f.Value.Length == 0)) Fail($"a {what} field is empty or exceeds 1024");
            if (embed.Fields.Length > 25) Fail($"{what} has {embed.Fields.Length} fields");
            if (!names && (embed.Description + string.Concat(embed.Fields.Select(f => f.Value))).Contains("<@")) Fail($"{what} names someone without names");
        }
        var digest = ReportWindow.CalendarMonth(2026, 10);
        foreach (var names in new[] { true, false })
        foreach (var w in new[] { month, digest, ReportWindow.From(StatsPeriod.AllTime, noon) })
        {
            Measure("sessions", ProjectSYNCS.Commands.ReportCards.BuildSessions(new(sFull, w.IsAllTime ? null : sFull), w, names, LongestName), names);
            Measure("polls", ProjectSYNCS.Commands.ReportCards.BuildPolls(new(pFull, w.IsAllTime ? null : pFull), w, names, LongestName), names);
            Measure("community", ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(cFading, w.IsAllTime ? null : cFull), w, names, LongestName), names);
            Measure("community", ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(cFull, w.IsAllTime ? null : cFull), w, names, LongestName), names);
            Measure("syncs", ProjectSYNCS.Commands.ReportCards.BuildSyncs(new(yFull, w.IsAllTime ? null : yFull), w, (20260801, 84_000), names, LongestName), names);
        }
        var widest = new[]
        {
            ProjectSYNCS.Commands.ReportCards.BuildSessions(new(sFull, sFull), month, true, LongestName).Length,
            ProjectSYNCS.Commands.ReportCards.BuildPolls(new(pFull, pFull), month, true, LongestName).Length,
            ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(cFading, cFull), month, true, LongestName).Length,
            ProjectSYNCS.Commands.ReportCards.BuildSyncs(new(yFull, yFull), month, (20260801, 84_000), true, LongestName).Length,
        };
        Console.WriteLine($"report embeds at worst: {string.Join(" · ", widest)} chars (sessions, polls, community, syncs)");

        // /help lists /report next to /admin, close to the caps: every page measured.
        var help = asm.GetType("ProjectSYNCS.Commands.HelpModule")!.GetMethod("BuildEmbed", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var page in new[] { 0, 1, 2 })
        {
            var embed = (Discord.Embed)help.Invoke(null, new object[] { page })!;
            if (embed.Length > 6000) Fail($"/help page {page} is {embed.Length} characters");
            if (embed.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } field }) Fail($"/help field '{field}' exceeds 1024");
            Console.WriteLine($"/help page {page}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
        }

        // The staff guides, /report help and /debug help.
        foreach (var (what, embed) in new[]
                 {
                     ("/report help", ProjectSYNCS.Commands.StaffHelpCards.BuildReportHelp()),
                     ("/debug help", ProjectSYNCS.Commands.StaffHelpCards.BuildDebugHelp()),
                 })
        {
            if (embed.Length > 6000) Fail($"{what} is {embed.Length} characters");
            if (embed.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } field }) Fail($"{what} field '{field}' exceeds 1024");
            Console.WriteLine($"{what}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
        }

        // /plynling help is close to the caps too, and the shop card lists every item it sells with its icon.
        foreach (var page in new[] { 0, 1, 2 })
        {
            var embed = ProjectSYNCS.Commands.PlynlingModule.BuildHelpEmbed(page);
            if (embed.Length > 6000) Fail($"/plynling help page {page} is {embed.Length} characters");
            if (embed.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } field }) Fail($"/plynling help field '{field}' exceeds 1024");
            Console.WriteLine($"/plynling help page {page}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
        }
        {
            var held = ProjectSYNCS.Helpers.ItemCatalog.Shop.ToDictionary(s => s.Item.Key, _ => 9999);
            var (shop, shopComponents) = ProjectSYNCS.Commands.ShopCards.BuildShop(held, 1_000_000, ProjectSYNCS.Helpers.ItemCatalog.SoapKey,
                new string('x', 200));
            if (shop.Length > 6000) Fail($"/inventory shop is {shop.Length} characters");
            if (shop.Fields.FirstOrDefault(f => f.Value.Length > 1024) is { Name: { } shopField }) Fail($"/inventory shop field '{shopField}' exceeds 1024");
            if (shopComponents.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components).Count() != 4) Fail("/inventory shop should show its select and three quantity buttons");
            Console.WriteLine($"/inventory shop: {shop.Length} chars, longest field {shop.Fields.Max(f => f.Value.Length)}");
        }

        // Collection tiers: Bronze at 1, Argent 2, Or 3, Platine 5, Diamant 10.
        {
            var expected = new (int Count, ProjectSYNCS.Helpers.CollectionTier Tier)[]
            {
                (0, ProjectSYNCS.Helpers.CollectionTier.None), (1, ProjectSYNCS.Helpers.CollectionTier.Bronze),
                (2, ProjectSYNCS.Helpers.CollectionTier.Silver), (3, ProjectSYNCS.Helpers.CollectionTier.Gold),
                (4, ProjectSYNCS.Helpers.CollectionTier.Gold), (5, ProjectSYNCS.Helpers.CollectionTier.Platinum),
                (9, ProjectSYNCS.Helpers.CollectionTier.Platinum), (10, ProjectSYNCS.Helpers.CollectionTier.Diamond),
                (25, ProjectSYNCS.Helpers.CollectionTier.Diamond),
            };
            foreach (var (count, tier) in expected)
                if (ProjectSYNCS.Helpers.CollectionTiers.TierFor(count) != tier) Fail($"{count} completions should be {tier}, got {ProjectSYNCS.Helpers.CollectionTiers.TierFor(count)}");
            if (ProjectSYNCS.Helpers.CollectionTiers.Next(4) != (ProjectSYNCS.Helpers.CollectionTier.Platinum, 5)) Fail("after 4 completions the next tier is Platine at 5");
            if (ProjectSYNCS.Helpers.CollectionTiers.Next(10) is not null) Fail("Diamant has no next tier");
            if (ProjectSYNCS.Helpers.CollectionTiers.Next(0) != (ProjectSYNCS.Helpers.CollectionTier.Bronze, 1)) Fail("with no completion the next tier is Bronze at 1");
            foreach (var s in ProjectSYNCS.Helpers.ItemCatalog.Sets)
            foreach (var t in Enum.GetValues<ProjectSYNCS.Helpers.CollectionTier>().Skip(1))
            {
                var key = ProjectSYNCS.Helpers.ItemEmojis.BadgeKey(s.Key, t);
                var name = ProjectSYNCS.Helpers.ItemEmojis.EmojiName(key);
                if (!ProjectSYNCS.Helpers.CollectionTiers.IsBadgeKey(key)) Fail($"{key} is not recognised as a badge icon");
                if (name is null || name.Length > 32 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail($"badge emoji name '{name}' is not a valid Discord emoji name");
            }
            if (ProjectSYNCS.Helpers.CollectionTiers.IsBadgeKey("badge.nope_gold")) Fail("a badge key for an unknown set passed");
        }

        // The collection book at its widest: every icon uploaded (custom markup ~35 chars), every item
        // held, every set at Diamant — the overview and every set page under every filter.
        {
            var fake = "<:a_custom_emoji_name_xx:123456789012345678>";
            foreach (var i in ProjectSYNCS.Helpers.ItemCatalog.All) ProjectSYNCS.Helpers.ItemEmojis.Set(ProjectSYNCS.Helpers.ItemCatalog.PictureKey(i.Key), fake);
            foreach (var s in ProjectSYNCS.Helpers.ItemCatalog.Sets)
            {
                ProjectSYNCS.Helpers.ItemEmojis.Set(ProjectSYNCS.Helpers.ItemEmojis.SetKey(s.Key), fake);
                foreach (var t in Enum.GetValues<ProjectSYNCS.Helpers.CollectionTier>().Skip(1))
                    ProjectSYNCS.Helpers.ItemEmojis.Set(ProjectSYNCS.Helpers.ItemEmojis.BadgeKey(s.Key, t), fake);
            }
            var heldAll = ProjectSYNCS.Helpers.ItemCatalog.Collectibles
                .Select(i => new ProjectSYNCS.Models.InventoryItem { Key = i.Key, Quantity = 9999 }).ToList();
            var allDone = ProjectSYNCS.Helpers.ItemCatalog.Sets
                .Select(s => new ProjectSYNCS.Models.CollectionCompletion { SetKey = s.Key, Count = 99 }).ToList();
            var widestBook = 0;
            foreach (var page in ProjectSYNCS.Helpers.ItemCatalog.Sets.Select(s => s.Key).Prepend(ProjectSYNCS.Commands.InventoryModule.OverviewPage))
            foreach (var filter in Enum.GetValues<ProjectSYNCS.Commands.CollectionFilter>())
            foreach (var done in new[] { allDone, new List<ProjectSYNCS.Models.CollectionCompletion>() })
            {
                var (book, bookComponents) = ProjectSYNCS.Commands.InventoryModule.BuildCollectionPage(big, heldAll, done, page, filter);
                Measure($"collection book '{page}'", book, names: true);
                if (book.Description.Length > 4096) Fail($"collection book '{page}' description is {book.Description.Length}");
                widestBook = Math.Max(widestBook, book.Length);
                var ids = bookComponents.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components)
                    .Select(c => c switch { Discord.ButtonComponent b => b.CustomId, Discord.SelectMenuComponent m => m.CustomId, _ => null }).ToList();
                if (ids.Count != ids.Distinct().Count()) Fail($"collection book '{page}' repeats a custom-id");
            }
            var noFacts = new MemberBadgeFacts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, null, new Dictionary<string, StoredBadge>());
            var noStandings = MemberBadges.Evaluate(noFacts, new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));
            if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(allDone, noStandings) is not { } badgeLine || badgeLine.Length > 4000) Fail("/level badge line missing or too long");
            if (ProjectSYNCS.Commands.MemberBadgeCards.CardLine(new List<ProjectSYNCS.Models.CollectionCompletion>(), noStandings) is not null) Fail("/level shows a badge line with no completion");
            ProjectSYNCS.Helpers.ItemEmojis.Clear();
            Console.WriteLine($"collection book at worst: {widestBook} chars");
        }

        // Every digest line takes the month and nothing else.
        foreach (var line in (string[])br.GetField("OwnerDigestLines")!.GetValue(null)!)
        {
            try { if (!string.Format(line, "octobre 2026").Contains("octobre 2026")) Fail($"digest line without the month: {line}"); }
            catch (FormatException) { Fail($"digest line breaks string.Format: {line}"); }
        }
    }
}
