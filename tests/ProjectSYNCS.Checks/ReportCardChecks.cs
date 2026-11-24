using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 24–32. /report's activity keys and counts, every card at its worst, quiz, Plynlings,
// emotes, sleeping members, and the line on top.
public static class ReportCardChecks
{
    public static readonly Dictionary<string, int> batch2Widest = new Dictionary<string, int>();

    public static void Run()
    {
        // 24. Activity keys and command paths (/report activity and commands).
        {
            // 23:30 UTC on 19 Oct 2026 (CEST, +2) is 01:30 on the 20th in Paris.
            if (ActivityKeys.DayHour(new DateTimeOffset(2026, 10, 19, 23, 30, 0, TimeSpan.Zero)) != (20261020, 1)) Fail("DayHour misses the Paris day");
            // Clocks go back on 25 Oct 2026 at 01:00 UTC: 01:30 UTC is 02:30 CET.
            if (ActivityKeys.DayHour(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero)) != (20261025, 2)) Fail("DayHour across the clock change");
            // Winter (CET, +1): 23:30 UTC on 1 Dec is 00:30 on the 2nd.
            if (ActivityKeys.DayHour(new DateTimeOffset(2026, 12, 1, 23, 30, 0, TimeSpan.Zero)) != (20261202, 0)) Fail("DayHour in winter");

            if (CommandPath.Join(new[] { "pl" }, "feed") != "plynling feed") Fail("/pl is not counted as /plynling");
            if (CommandPath.Join(new[] { "config", "excluded-channels" }, "add") != "config excluded-channels add") Fail("nested groups");
            if (CommandPath.Join(Array.Empty<string>(), "help") != "help") Fail("top-level command");
            if (CommandPath.Shortcut(ShortcutKind.Work) != ";work" || CommandPath.Shortcut(ShortcutKind.Visit) != ";pl visit") Fail("shortcut keys");
        }

        // 25. ActivityCounter: counts, Drain empties, Restore merges a failed flush back.
        {
            var t2 = new ActivityCounter();
            var t2At = new DateTimeOffset(2026, 10, 19, 18, 0, 0, TimeSpan.Zero); // 20:00 in Paris
            t2.CountMessage(1, 10, t2At);
            t2.CountMessage(1, 10, t2At);
            t2.CountMessage(1, 11, t2At.AddHours(1));
            t2.CountVoice(1, 20, 3, 1, t2At);
            t2.CountVoice(1, 20, 5, 1, t2At);
            t2.CountVoice(1, 20, 0, 1, t2At);                 // an empty room adds nothing
            t2.CountCommand(1, "report sessions", true, t2At);
            t2.CountCommand(1, "report sessions", false, t2At);
            var t2Batch = t2.Drain();
            if (t2Batch.Hours[new HourKey(1, 20261019, 20)] != new HourTally(2, 8)) Fail("hour tally");
            if (t2Batch.Channels[new ChannelKey(1, 20, 20261019)] != new ChannelTally(0, 8, 5)) Fail("voice minutes or peak");
            if (t2Batch.Commands[new CommandKey(1, 20261019, "report sessions")] != new CommandTally(2, 1)) Fail("command tally");
            if (!t2.Drain().IsEmpty) Fail("Drain left counts behind");
            if (!t2Batch.For(2).IsEmpty || t2Batch.GuildIds.Single() != 1) Fail("batch split by guild");
            t2.CountMessage(1, 10, t2At);
            t2.Restore(t2Batch);
            if (t2.Drain().Channels[new ChannelKey(1, 10, 20261019)] != new ChannelTally(3, 0, 0)) Fail("Restore did not merge");
        }

        // The widest embed of each batch-2 report, printed once their sections have run.

        // 26. Activity and command arithmetic.
        {
            ProjectSYNCS.Models.ActivityHourStat T5Hour(int day, int hour, long messages, long voice) =>
                new() { GuildId = 1, Day = day, Hour = hour, Messages = messages, VoiceMinutes = voice };
            ProjectSYNCS.Models.ChannelDailyStat T5Chan(ulong id, long messages, long voice, int peak) =>
                new() { GuildId = 1, ChannelId = id, Day = 20261019, Messages = messages, VoiceMinutes = voice, VoicePeak = peak };
            var t5Guild = new[] { new ChannelInfo(10, false, false), new ChannelInfo(11, false, true), new ChannelInfo(12, false, false),
                                  new ChannelInfo(20, true, false), new ChannelInfo(21, true, false) };
            var t5 = ActivityReport.Compute(
                new[] { T5Hour(20261019, 21, 50, 30), T5Hour(20261019, 14, 5, 0), T5Hour(20261018, 21, 10, 0) },  // 19 Oct 2026 is a Monday
                new[] { T5Chan(10, 40, 0, 0), T5Chan(11, 15, 0, 0), T5Chan(20, 0, 30, 3), T5Chan(99, 10, 0, 0) },  // 99: deleted channel
                t5Guild);
            if (t5.Messages != 65 || t5.VoiceMinutes != 30) Fail($"activity totals {t5.Messages}/{t5.VoiceMinutes}");
            if (t5.MessagesByHour.Count != 24 || t5.MessagesByHour[21] != 60 || t5.BusiestHours[0] != 21) Fail("hour profile");
            if (t5.BusiestWeekday != DayOfWeek.Monday) Fail($"busiest weekday {t5.BusiestWeekday}");
            // Two Mondays of 30 against one Sunday of 40: Sunday talks more, per Sunday.
            var t5Week = ActivityReport.Compute(new[] { T5Hour(20261019, 21, 30, 0), T5Hour(20261025, 21, 40, 0), T5Hour(20261026, 21, 30, 0) },
                Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
            if (t5Week.BusiestWeekday != DayOfWeek.Sunday) Fail($"busiest weekday per occurrence {t5Week.BusiestWeekday}");
            // A channel nobody could write in or join isn't silent, it's closed.
            var t5Closed = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(),
                new[] { new ChannelInfo(30, false, false, Open: false), new ChannelInfo(31, true, false, Open: false), new ChannelInfo(32, false, false) });
            if (t5Closed.SilentText.Single().ChannelId != 32 || t5Closed.EmptyVoice.Count != 0) Fail("closed channels listed as silent or unused");
            // A 31-day month keeps one bar per day; a longer series averages, so uneven buckets draw no surge.
            var t5Spark = Enumerable.Repeat(1L, 31).ToList();
            t5Spark[29] = 7;
            if (TextChart.Sparkline(t5Spark) is var t5Line && (t5Line.Length != 31 || t5Line[29] != '█' || t5Line[30] != '▂')) Fail($"sparkline over 31 days: {t5Line}");
            if (TextChart.Sparkline(Enumerable.Repeat(5L, 40).ToList()).Distinct().Count() != 1) Fail("sparkline over 40 flat days isn't flat");
            if (t5.TopText[0].ChannelId != 10 || !t5.TopText.Single(l => l.ChannelId == 11).Excluded) Fail("text ranking or excluded mark");
            if (!t5.TopText.Any(l => l.ChannelId == 99)) Fail("a deleted channel with counts must still be listed");
            if (t5.SilentText.Single().ChannelId != 12) Fail("silent channels");
            if (t5.TopVoice.Single().Peak != 3 || t5.EmptyVoice.Single().ChannelId != 21) Fail("voice rooms");
            var t5Empty = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
            if (t5Empty.Messages != 0 || t5Empty.BusiestWeekday is not null || t5Empty.SilentText.Count != 0) Fail("empty activity (guild unavailable)");

            ProjectSYNCS.Models.CommandDailyStat T5Cmd(string command, long uses, long failures) =>
                new() { GuildId = 1, Day = 20261019, Command = command, Uses = uses, Failures = failures };
            var t5c = CommandReport.Compute(
                new[] { T5Cmd("report sessions", 3, 1), T5Cmd("report sessions", 2, 0), T5Cmd("help", 9, 0), T5Cmd(";work", 4, 0), T5Cmd("old name", 1, 0) },
                new[] { "help", "report sessions", "report polls", "plynling feed" });
            if (t5c.Uses != 19 || t5c.Failures != 1) Fail("command totals");
            if (t5c.Top[0].Command != "help" || t5c.Top.Single(l => l.Command == "report sessions").Uses != 5) Fail("command ranking");
            if (t5c.Shortcuts.Single().Command != ";work" || t5c.Top.Any(l => l.Command.StartsWith(';'))) Fail("shortcuts kept apart");
            if (!t5c.NeverUsed.SequenceEqual(new[] { "plynling feed" }) || !t5c.NeverUsedStaff.SequenceEqual(new[] { "report polls" }))
                Fail($"never used: {string.Join(",", t5c.NeverUsed)} / staff: {string.Join(",", t5c.NeverUsedStaff)}");
            if (!t5c.Top.Any(l => l.Command == "old name")) Fail("a renamed command keeps its old rows");
        }

        // 27. /report activity and commands: every card measured at its worst.
        {
            void T6Fits(string what, Discord.Embed e, bool names)
            {
                batch2Widest[what] = Math.Max(batch2Widest.GetValueOrDefault(what), e.Length);
                if (e.Length > 6000) Fail($"{what} embed is {e.Length} characters");
                if (e.Fields.Length > 25 || e.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"{what} has an empty, oversized or 26th field");
                if (!names && (e.Description + string.Concat(e.Fields.Select(f => f.Value))).Contains("<@")) Fail($"{what} names someone");
            }
            const ulong t6Big = 1_234_567_890_123_456_789;
            var t6Lines = Enumerable.Range(0, ActivityReport.TopChannels).Select(i => new ChannelLine(t6Big + (ulong)i, 99_999_999, 99, i % 2 == 0)).ToList();
            var t6Many = Enumerable.Range(0, 60).Select(i => new ChannelLine(t6Big + 100 + (ulong)i, 0, 0, true)).ToList();
            var t6Act = new ActivityReportData(99_999_999, 99_999_999, Enumerable.Repeat(9_999L, 24).ToList(), Enumerable.Repeat(9_999L, 24).ToList(),
                new[] { 21, 22, 23 }, DayOfWeek.Wednesday, t6Lines, t6Many, t6Lines, t6Many, t6Lines.ToDictionary(l => l.ChannelId, l => 5L));
            var t6Cmd = new CommandReportData(9_999_999, 99_999,
                Enumerable.Range(0, CommandReport.TopCommands).Select(i => new CommandLine($"plynling some-long-subcommand-{i}", 999_999, 999)).ToList(),
                new[] { new CommandLine(";pl", 9999, 0), new CommandLine(";work", 9999, 0), new CommandLine(";pl visit", 9999, 0) },
                Enumerable.Range(0, 150).Select(i => $"config some-group-name sub-{i}").ToList(),
                new Dictionary<string, long>(), Array.Empty<CommandLine>());
            var t6Month = ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow);
            foreach (var t6Names in new[] { true, false })
            {
                T6Fits("activity", ProjectSYNCS.Commands.ReportCards.BuildActivity(new(t6Act, t6Act), t6Month, t6Names), t6Names);
                T6Fits("commands", ProjectSYNCS.Commands.ReportCards.BuildCommands(new(t6Cmd, t6Cmd), t6Month, t6Names), t6Names);
            }
            var t6Quiet = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
            T6Fits("empty activity", ProjectSYNCS.Commands.ReportCards.BuildActivity(new(t6Quiet, null), ReportWindow.From(StatsPeriod.AllTime, DateTimeOffset.UtcNow), true), true);
            // /report minecraft: her time in game, orders and adventures, from the daily buckets.
            ProjectSYNCS.Models.MinecraftDailyStat T6Mc(int day, string key, long count) => new() { Day = day, Key = key, Count = count };
            var t6McData = MinecraftReport.Compute(new[]
            {
                T6Mc(20261003, McStats.Minutes, 90), T6Mc(20261004, McStats.Minutes, 30), T6Mc(20261004, McStats.Orders, 7),
                T6Mc(20261004, McStats.OrdersBy("bob"), 5), T6Mc(20261004, McStats.OrdersBy("papa"), 2),
                T6Mc(20261004, McStats.Gathered("wood"), 40), T6Mc(20261005, McStats.Deaths, 1),
            });
            if (t6McData.Minutes != 120 || t6McData.DaysPlayed != 2 || t6McData.Orders != 7 || t6McData.TopPlayers[0] != new McCountLine("bob", 5)
                || t6McData.TopPlayers.Count != 2 || t6McData.Gathered[0] != new McCountLine("wood", 40) || t6McData.Deaths != 1 || !t6McData.Played)
                Fail($"minecraft report: {t6McData}");
            if (MinecraftReport.Compute(Array.Empty<ProjectSYNCS.Models.MinecraftDailyStat>()).Played) Fail("minecraft report: an empty month played");
            // At its worst: five 32-character players, every resource, 9-digit counts.
            var t6McName = new string('w', 32);
            var t6McWorst = new MinecraftReportData(999_999_999, 31, 999_999_999,
                Enumerable.Range(0, MinecraftReport.ListedPlayers).Select(i => new McCountLine(t6McName[..^1] + i, 999_999_999)).ToList(),
                999_999_999, 999_999_999, 999_999_999, 999_999_999, 999_999_999,
                Enum.GetValues<McResource>().Select(r => new McCountLine(r.ToString().ToLowerInvariant(), 999_999_999)).ToList());
            foreach (var t6Names in new[] { true, false })
            {
                var t6McCard = ProjectSYNCS.Commands.ReportCards.BuildMinecraft(new(t6McWorst, t6McWorst), t6Month, t6Names);
                T6Fits("minecraft", t6McCard, t6Names);
                var t6McText = t6McCard.Description + string.Concat(t6McCard.Fields.Select(f => f.Value));
                if (t6McText.Contains(t6McName[..^1]) != t6Names) Fail($"minecraft card: player names {(t6Names ? "missing" : "shown")}");
            }
            var t6McEmpty = ProjectSYNCS.Commands.ReportCards.BuildMinecraft(new(MinecraftReport.Compute(Array.Empty<ProjectSYNCS.Models.MinecraftDailyStat>()), null), t6Month, true);
            if (t6McEmpty.Description != "Elle n'a pas joué sur cette période.") Fail("minecraft card: an empty window");
            if (!ReportKinds.Digest(DigestRecipient.Owner).SequenceEqual(new[] { ReportKind.Syncs, ReportKind.Commands, ReportKind.Incidents, ReportKind.Quiz, ReportKind.Minecraft })
                || !ReportKinds.Digest(DigestRecipient.Tata).SequenceEqual(new[] { ReportKind.Community, ReportKind.Sessions, ReportKind.Activity, ReportKind.Sleeping }))
                Fail("digest lists");
        }

        // 28. /report quiz: posted, found, unanswered; a round whose send failed is not a question.
        {
            var t7At = new DateTimeOffset(2026, 10, 19, 18, 0, 0, TimeSpan.Zero);
            var t7Q0 = QuizBank.All[0];
            var t7Q1 = QuizBank.All[1];
            var t7Rounds = new List<ProjectSYNCS.Models.QuizRound>
            {
                new() { GuildId = 1, MessageId = 5, QuestionKey = t7Q0.Key, PostedAt = t7At, Closed = true, WinnerId = 7, WonAt = t7At.AddSeconds(42), Day = 20261019 },
                new() { GuildId = 1, MessageId = 6, QuestionKey = t7Q1.Key, PostedAt = t7At, Closed = true, Day = 20261019 },
                new() { GuildId = 1, MessageId = 0, QuestionKey = t7Q1.Key, PostedAt = t7At, Closed = true, Day = 20261019 },   // send failed
            };
            var t7 = QuizReport.Compute(t7Rounds);
            if (t7.Posted != 2 || t7.Found != 1 || t7.Unanswered != 1) Fail($"quiz {t7.Posted}/{t7.Found}/{t7.Unanswered}");
            if (t7.MedianSeconds != 42 || t7.Fastest?.UserId != 7) Fail("quiz timing");
            if (!t7.UnfoundPrompts.SequenceEqual(new[] { t7Q1.Prompt })) Fail("unfound prompts");
            if (t7.ByCategory.Sum(l => l.Posted) != 2) Fail("quiz by category");
            var t7Worst = new QuizReportData(999, 999, 999,
                Enum.GetValues<QuizCategory>().Select(x => new QuizGroupLine(QuizBank.Label(x), 999, 999)).ToList(),
                Enum.GetValues<QuizDifficulty>().Select(x => new QuizGroupLine(QuizBank.Label(x), 999, 999)).ToList(),
                999, new PersonTally(1_234_567_890_123_456_789, 999_999, 0),
                Enumerable.Repeat(new string('Q', 400), QuizReport.ListedPrompts).ToList(),
                Enumerable.Range(0, 5).Select(i => new PersonTally(1_234_567_890_123_456_789 + (ulong)i, 999, 999)).ToList());
            var t7Embed = ProjectSYNCS.Commands.ReportCards.BuildQuiz(new(t7Worst, t7Worst), ReportWindow.From(StatsPeriod.Month, t7At), true, LongestName);
            batch2Widest["quiz"] = t7Embed.Length;
            if (t7Embed.Length > 6000 || t7Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"quiz embed {t7Embed.Length}");
            // The community report counts the same posted rounds.
            var t7Community = CommunityReport.Compute(Array.Empty<ProjectSYNCS.Models.MemberDailyStat>(), Array.Empty<ProjectSYNCS.Models.MemberFlowDailyStat>(),
                Array.Empty<ProjectSYNCS.Models.EmoteDailyStat>(), new Dictionary<(ulong, string), string>(), t7Rounds, ReportWindow.From(StatsPeriod.Month, t7At));
            if (t7Community.QuizRounds != 2) Fail($"community counts {t7Community.QuizRounds} quiz rounds, a failed send included");
        }

        // 29. /report plynlings: births, deaths, the living's state; the mascot never counts.
        {
            var t8Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
            var t8Window = ReportWindow.From(StatsPeriod.Week, t8Now);
            ProjectSYNCS.Models.Plynling T8Pet(int id, ulong owner, int adoptedDaysAgo)
            {
                var p = PlynlingLife.Create(1, owner, $"Pet{id}", ProjectSYNCS.Models.PlynlingSpecies.Amanite, ProjectSYNCS.Models.PlynlingGender.Male, t8Now.AddDays(-adoptedDaysAgo));
                p.Id = id;
                p.Hunger = 0.9;
                p.NeedsAsOf = t8Now;
                return p;
            }
            var t8Healthy = T8Pet(1, 101, 30);
            var t8Starving = T8Pet(2, 102, 2);
            t8Starving.Hunger = 0.1;
            var t8Sick = T8Pet(3, 103, 40);
            t8Sick.SickSince = t8Now.AddHours(-3);
            var t8Dead = T8Pet(4, 104, 20);
            t8Dead.DiedAt = t8Now.AddDays(-1);
            t8Dead.DeathCause = ProjectSYNCS.Models.DeathCause.Illness;
            var t8Mascot = T8Pet(5, 999, 1);
            PlynlingMascot.Bind(999);
            var t8 = PlynlingReport.Compute(new[] { t8Healthy, t8Starving, t8Sick, t8Dead, t8Mascot },
                new[] { new ProjectSYNCS.Models.PlynlingEventInstance { PlynlingId = 1, EventKey = "nope", ResolvedAt = t8Now.AddDays(-2), DecidedAlone = true, ChallengeSucceeded = true } },
                Array.Empty<ProjectSYNCS.Models.PlynlingRelation>(), Array.Empty<ProjectSYNCS.Models.PlynlingBadge>(), Array.Empty<ProjectSYNCS.Models.PlynlingTrait>(),
                t8Window, t8Now);
            PlynlingMascot.Bind(0);
            if (t8.Living != 3) Fail($"living {t8.Living} (the mascot must not count)");
            if (t8.Born != 1) Fail($"born {t8.Born}");
            if (t8.Deaths.Single() != (ProjectSYNCS.Models.DeathCause.Illness, 1)) Fail("deaths by cause");
            if (t8.Starving != 1 || t8.Sick != 1) Fail($"starving {t8.Starving}, sick {t8.Sick}");
            if (t8.Watch.Count != 2 || !t8.Watch[0].Sick) Fail("watch list (sick first)");
            if (t8.EventsResolved != 1 || t8.DecidedAlone != 1 || t8.ChallengesWon != 1 || t8.TopEvents.Single().Title != "nope") Fail("events (an unknown key shows its key)");

            var t8Worst = t8 with
            {
                TopEvents = Enumerable.Range(0, PlynlingReport.TopEventCount).Select(i => (new string('E', 80), 999)).ToList(),
                Watch = Enumerable.Range(0, PlynlingReport.WatchCount).Select(i => new PlynlingWatch(1_234_567_890_123_456_789 + (ulong)i, "**@everyone** " + new string('N', 30), true, true)).ToList(),
            };
            var t8Embed = ProjectSYNCS.Commands.ReportCards.BuildPlynlings(new(t8Worst, t8Worst), t8Window, true, LongestName);
            batch2Widest["plynlings"] = t8Embed.Length;
            if (t8Embed.Length > 6000 || t8Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"plynlings embed {t8Embed.Length}");
            if (t8Embed.Fields.Any(f => f.Value.Contains("**@everyone**"))) Fail("a Plynling name was not sanitized");
        }

        // 30. /report emotes: own, foreign and Unicode; never-used server emotes; movers.
        {
            ProjectSYNCS.Models.EmoteDailyStat T9Row(ulong id, string unicode, long written, long reacted) =>
                new() { GuildId = 1, EmoteId = id, Unicode = unicode, Day = 20261019, WrittenCount = written, ReactedCount = reacted };
            var t9Server = new[] { new ServerEmote(1, "<:one:1>"), new ServerEmote(2, "<:two:2>"), new ServerEmote(3, "<:three:3>") };
            var t9Markup = new Dictionary<(ulong, string), string> { [(50, "")] = "<:elsewhere:50>" };
            var t9 = EmoteReport.Compute(new[] { T9Row(1, "", 5, 5), T9Row(2, "", 1, 0), T9Row(50, "", 4, 0), T9Row(77, "", 1, 0), T9Row(0, "😂", 9, 0) }, t9Server, t9Markup);
            if (t9.Own != 11 || t9.Foreign != 5 || t9.Unicode != 9) Fail($"emote split {t9.Own}/{t9.Foreign}/{t9.Unicode}");
            if (t9.TopOwn[0].Markup != "<:one:1>" || !t9.UnusedOwn.SequenceEqual(new[] { "<:three:3>" })) Fail("own ranking or never used");
            if (!t9.TopForeign.Any(l => l.Markup.Contains("77"))) Fail("a foreign emote without a stat row must still show");
            var t9Before = EmoteReport.Compute(new[] { T9Row(2, "", 8, 0) }, t9Server, t9Markup);
            if (EmoteReport.Movers(t9, t9Before, rising: true)[0].Markup != "<:one:1>" || EmoteReport.Movers(t9, t9Before, rising: false)[0].Markup != "<:two:2>") Fail("movers");
            var t9NoGuild = EmoteReport.Compute(new[] { T9Row(1, "", 5, 5) }, Array.Empty<ServerEmote>(), t9Markup);
            if (t9NoGuild.Own != 0 || t9NoGuild.UnusedOwn.Count != 0) Fail("guild unavailable: no emote counts as the server's");

            var t9ManyServer = Enumerable.Range(0, 80).Select(i => new ServerEmote(1000 + (ulong)i, $"<a:a_long_emote_name_{i}:{1_234_567_890_123_456_789 + (ulong)i}>")).ToList();
            var t9Worst = EmoteReport.Compute(t9ManyServer.Take(40).Select(e => T9Row(e.Id, "", 999_999, 999_999)).ToList(), t9ManyServer, t9Markup);
            var t9Embed = ProjectSYNCS.Commands.ReportCards.BuildEmotes(new(t9Worst, t9Before), ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow), true);
            batch2Widest["emotes"] = t9Embed.Length;
            if (t9Embed.Length > 6000 || t9Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"emotes embed {t9Embed.Length}");
            // Every emote that opens must close: a cut one leaves a « <:name » with no full « <:name:id> ».
            if (t9Embed.Fields.Any(f => System.Text.RegularExpressions.Regex.Matches(f.Value, "<a?:").Count
                                        != System.Text.RegularExpressions.Regex.Matches(f.Value, @"<a?:\w+:\d+>").Count)) Fail("an emote was cut in half");
        }

        // 31. /report sleeping: who has been silent N days; newcomers and bots never listed.
        {
            var t10Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
            var t10Members = new[]
            {
                new MemberSnapshot(1, t10Now.AddDays(-200), false),   // active 5 days ago
                new MemberSnapshot(2, t10Now.AddDays(-200), false),   // last seen 40 days ago
                new MemberSnapshot(3, t10Now.AddDays(-200), false),   // never active
                new MemberSnapshot(4, t10Now.AddDays(-10), false),    // joined recently: never listed
                new MemberSnapshot(5, null, false),                    // unknown join date: an old member
                new MemberSnapshot(6, t10Now.AddDays(-200), true),    // a bot
            };
            var t10Last = new Dictionary<ulong, int> { [1] = AppTime.DayKey(t10Now.AddDays(-5)), [2] = AppTime.DayKey(t10Now.AddDays(-40)) };
            var t10 = SleepingReport.Compute(t10Members, t10Last, 30, t10Now);
            if (t10.Members != 5) Fail($"members {t10.Members} (bots excluded)");
            if (!t10.Sleepers.Select(s => s.UserId).SequenceEqual(new ulong[] { 2, 3, 5 })) Fail($"sleepers {string.Join(",", t10.Sleepers.Select(s => s.UserId))} (never-active last)");
            if (SleepingReport.Compute(t10Members, t10Last, 60, t10Now).Sleepers.Any(s => s.UserId == 2)) Fail("40 days silent is not asleep at 60");
            if (SleepingReport.Compute(Array.Empty<MemberSnapshot>(), t10Last, 30, t10Now).Sleepers.Count != 0) Fail("guild unavailable");

            var t10Many = Enumerable.Range(0, 300).Select(i => new Sleeper(1_234_567_890_123_456_789 + (ulong)i, i % 2 == 0 ? null : 20260901)).ToList();
            var t10Embed = ProjectSYNCS.Commands.ReportCards.BuildSleeping(new SleepingReportData(90, 400, t10Many), LongestName);
            batch2Widest["sleeping"] = t10Embed.Length;
            if (t10Embed.Length > 6000 || t10Embed.Fields.Length > 3 || t10Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"sleeping embed {t10Embed.Length}, {t10Embed.Fields.Length} fields");
            if (!t10Embed.Footer!.Value.Text.Contains("autres") || !t10Embed.Fields.Last().Value.Contains("autres")) Fail("the sleepers that don't fit must be counted");
            var t10Mixed = ProjectSYNCS.Commands.ReportCards.BuildSleeping(new SleepingReportData(30, 20, new[] { new Sleeper(7, 20260901), new Sleeper(8, null), new Sleeper(9, null) }), _ => "Bob");
            if (t10Mixed.Fields.Length != 2 || !t10Mixed.Fields[0].Value.Contains("dernier signe") || t10Mixed.Fields[1].Value.Contains("dernier signe") || !t10Mixed.Fields[1].Name.Contains("(2)"))
                Fail("sleeping report: recent silences first, the never-active apart");

            // Who earned a few messages' worth isn't going quiet; who was busy is.
            var t10Before = new CommunityReportData(2, new List<long>(), 0, 0, 0, 0, 0, Array.Empty<(string, long)>(), 0, 0, 0,
                new Dictionary<ulong, long> { [1] = 20, [2] = CommunityReport.FadingMinXp }, new Dictionary<ulong, long>(), new Dictionary<ulong, long>());
            var t10Fading = CommunityReport.Fading(t10Before with { XpByUser = new Dictionary<ulong, long>() }, t10Before);
            if (t10Fading.Single().UserId != 2) Fail($"fading: {string.Join(",", t10Fading.Select(x => x.UserId))}");

            // Chat and voice ranked apart. 1 sits in calls (60 min pay 600 XP) and barely writes; 2 only writes;
            // 3's two 60-minute days pay the full rate twice, which one 120-minute total would not.
            var t10Window = ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow);
            ProjectSYNCS.Models.MemberDailyStat T10Day(ulong user, int day, long xp, long voice) =>
                new() { UserId = user, Day = day, XpEarned = xp, VoiceMinutes = voice };
            var t10Split = CommunityReport.Compute(new[]
                {
                    T10Day(1, t10Window.ToDay, 700, 60),
                    T10Day(2, t10Window.ToDay, 500, 0),
                    T10Day(3, t10Window.FromDay, 600, 60),
                    T10Day(3, t10Window.ToDay, 600, 60),
                    T10Day(4, t10Window.ToDay, 100, 60),   // staff took XP back: never below zero
                }, Array.Empty<ProjectSYNCS.Models.MemberFlowDailyStat>(), Array.Empty<ProjectSYNCS.Models.EmoteDailyStat>(),
                new Dictionary<(ulong, string), string>(), Array.Empty<ProjectSYNCS.Models.QuizRound>(), t10Window);
            if (string.Join(",", CommunityReport.Busiest(t10Split, null).Select(x => x.UserId)) != "3,1,2,4") Fail("busiest overall");
            if (string.Join(",", CommunityReport.BusiestInChat(t10Split, null).Select(x => $"{x.UserId}:{x.Value}")) != "2:500,1:100") Fail("busiest in chat");
            if (string.Join(",", CommunityReport.BusiestInVoice(t10Split, null).Select(x => $"{x.UserId}:{x.Value}")) is not ("3:120,1:60,4:60" or "3:120,4:60,1:60")) Fail("busiest in voice");
            var t10Card = ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(t10Split, null), t10Window, true, _ => "Bob");
            if (!t10Card.Fields.Any(f => f.Name.Contains("À l'écrit")) || !t10Card.Fields.Any(f => f.Name.Contains("En vocal"))) Fail("community card must rank chat and voice apart");
            var t10Anonymous = ProjectSYNCS.Commands.ReportCards.BuildCommunity(new(t10Split, null), t10Window, false, _ => "Bob");
            if (t10Anonymous.Fields.Any(f => f.Value.Contains("<@"))) Fail("community card without names must name nobody");
            // Each share is of its own list's total: 1200 of 2500 XP, 500 of 600 chat XP, 120 of 240 minutes.
            bool T10Share(string field, string share) => t10Card.Fields.Single(f => f.Name.Contains(field)).Value.Split('\n')[0].EndsWith($"{share} du total");
            if (!T10Share("tout compris", "48 %") || !T10Share("À l'écrit", "83 %") || !T10Share("En vocal", "50 %")) Fail("community shares");
        }

        // 32. /report's line on top of the card: whose pool, untagged, and never past the caps.
        {
            var t11Tata = (ulong)Field("TataId");
            if (!ReferenceEquals(ProjectSYNCS.Commands.ReportCards.DutyPool(AvailabilityService.OwnerId), Field("OwnerReportDutyLines"))
                || !ReferenceEquals(ProjectSYNCS.Commands.ReportCards.DutyPool(t11Tata), Field("TataReportLines"))
                || !ReferenceEquals(ProjectSYNCS.Commands.ReportCards.DutyPool(42), Field("StaffReportLines")))
                Fail("report line: wrong pool for Papa, Tata or other staff");
            var t11Card = new Discord.EmbedBuilder().WithTitle("t").WithDescription("figures").Build();
            var t11Full = new Discord.EmbedBuilder().WithTitle("t").WithDescription(new string('x', 4090)).Build();
            foreach (var t11Pool in new[] { "OwnerReportDutyLines", "TataReportLines", "StaffReportLines" })
                foreach (var l in (string[])Field(t11Pool))
                {
                    if (Sticker.Strip(l) != l || l.Contains('{') || l.Contains('}')) Fail($"{t11Pool}: tagged or braced: {l}");
                    if (l.Length > 200) Fail($"{t11Pool}: {l.Length} characters: {l}");
                    if (!ProjectSYNCS.Commands.ReportCards.WithHeadline(t11Card, l).Description.StartsWith(l + "\n\nfigures")) Fail($"{t11Pool}: not on top: {l}");
                    if (ProjectSYNCS.Commands.ReportCards.WithHeadline(t11Full, l).Description.Length > 4096) Fail("report line pushed a description past 4096");
                }
        }
    }
}
