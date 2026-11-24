using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 33–34. The monthly digest: « Le mois en bref » and its honesty.
public static class DigestChecks
{
    public static void Run()
    {
        // 33. The monthly digest's « Le mois en bref »: what each version flags, and both cards at their worst.
        {
            var t12Month = ReportWindow.CalendarMonth(2026, 11);
            ProjectSYNCS.Helpers.SyncsReportData T12Syncs(int days, int? max, long answers, long unrecognised, params (ProjectSYNCS.Models.UptimeEventKind, int)[] outages) =>
                new(days, 50_000, max, max is null ? null : 20261110, 0, new List<long>(), outages.ToList(), TimeSpan.Zero, 0.99,
                    answers, unrecognised, 0, 0, 0, Array.Empty<PersonTally>());
            CommandReportData T12Cmd(params CommandLine[] failing) => new(failing.Sum(c => c.Uses), failing.Sum(c => c.Failures),
                failing, Array.Empty<CommandLine>(), new[] { "never" }, new Dictionary<string, long>(), failing);
            QuizReportData T12Quiz(params QuizGroupLine[] byDifficulty) => new(byDifficulty.Sum(l => l.Posted), byDifficulty.Sum(l => l.Found), 0,
                Array.Empty<QuizGroupLine>(), byDifficulty, null, null, Array.Empty<string>(), Array.Empty<PersonTally>());

            // Papa: a failing command (3/20) but not a rare one (1/1) nor a robust one (2/100); a power cut;
            // a heat record only against a measured month; unanswered rising 10 points; « difficile » too easy.
            var t12Alerts = DigestBrief.Owner(
                new(T12Cmd(new CommandLine("report sessions", 20, 3), new CommandLine("help", 1, 1), new CommandLine("plynling feed", 100, 2)), null),
                new(T12Syncs(30, 70_000, 200, 60, (ProjectSYNCS.Models.UptimeEventKind.PowerCut, 1)), T12Syncs(30, 60_000, 200, 40)),
                (20261110, 70_000),
                new(T12Quiz(new QuizGroupLine(QuizBank.Label(QuizDifficulty.Hard), 10, 9), new QuizGroupLine(QuizBank.Label(QuizDifficulty.Easy), 10, 8)), null),
                t12Month);
            var t12Kinds = t12Alerts.Select(a => a.Kind).ToList();
            if (!t12Kinds.SequenceEqual(new[] { DigestAlertKind.FailingCommand, DigestAlertKind.PowerCuts, DigestAlertKind.HeatRecord,
                    DigestAlertKind.UnansweredRising, DigestAlertKind.QuizTooEasy }))
                Fail($"owner alerts: {string.Join(", ", t12Kinds)}");
            if (t12Alerts[0].Subject != "report sessions") Fail("the failing command is not the one at 3/20");
            // The first month of a counter: no comparison, so no record and no « rising ».
            var t12First = DigestBrief.Owner(new(T12Cmd(), null), new(T12Syncs(12, 70_000, 200, 90), null), (20261110, 70_000),
                new(T12Quiz(), null), t12Month);
            if (t12First.Count != 0) Fail($"first month raised {string.Join(", ", t12First.Select(a => a.Kind))}");

            var t12Slots = new[] { new SlotTally(1, 3, 10, 6), new SlotTally(5, 2, 10, 9), new SlotTally(6, 1, 5, 5) };
            var t12Sessions = new SessionReportData(6, 0, Array.Empty<(ProjectSYNCS.Models.SessionCategory, int)>(), 0, 0, null, 6, 25, 20, 5, 0, null,
                t12Slots, Array.Empty<SlotTally>(), Array.Empty<PersonTally>(), Array.Empty<PersonTally>(), Array.Empty<PersonTally>(), Array.Empty<PersonTally>());
            if (DigestBrief.BestSessionDay(t12Sessions)?.Slot != 5) Fail("best session day (a single session doesn't count)");

            // Tata: active members down 30 %, more leaves than joins, 3 members last seen the month before,
            // and 20 hours in voice with no session.
            CommunityReportData T12Community(int active, int joins, int leaves, long voice = 0) => new(active, new List<long>(), 0, voice, 0, joins, leaves,
                Array.Empty<(string, long)>(), 0, 0, 0, new Dictionary<ulong, long> { [1] = 10, [2] = 30, [3] = 20, [4] = 5 },
                new Dictionary<ulong, long> { [1] = 10 }, new Dictionary<ulong, long> { [2] = 30 });
            var t12Sleeping = new SleepingReportData(30, 20, Enumerable.Range(0, 3).Select(i => new Sleeper((ulong)i, 20261015)).ToList());
            var t12NoSession = t12Sessions with { Held = 0 };
            var t12Tata = DigestBrief.Tata(new(T12Community(7, 1, 2, 1200), T12Community(10, 0, 0)), new(t12NoSession, null), t12Sleeping, t12Month);
            if (!t12Tata.Select(a => a.Kind).SequenceEqual(new[] { DigestAlertKind.ActiveDrop, DigestAlertKind.MoreLeaves, DigestAlertKind.NewSleepers, DigestAlertKind.VoiceWithoutSessions }))
                Fail($"tata alerts: {string.Join(", ", t12Tata.Select(a => a.Kind))}");
            if (t12Tata.Last().Value != 20) Fail($"voice without sessions: {t12Tata.Last().Value} h");
            // Calm: one departure, members who were never active or went quiet long ago, voice with sessions, or too little voice to matter.
            var t12Old = new SleepingReportData(30, 20, Enumerable.Range(0, 18).Select(i => new Sleeper((ulong)i, i % 2 == 0 ? null : 20260901)).ToList());
            foreach (var (t12What, t12Calm) in new[]
            {
                ("sessions held", DigestBrief.Tata(new(T12Community(7, 0, 1, 1200), T12Community(8, 0, 0)), new(t12Sessions, null), t12Old, t12Month)),
                ("little voice", DigestBrief.Tata(new(T12Community(7, 0, 1, 60), T12Community(8, 0, 0)), new(t12NoSession, null), t12Old, t12Month)),
            })
                if (t12Calm.Count != 0) Fail($"a calm month for Tata ({t12What}) raised {string.Join(", ", t12Calm.Select(a => a.Kind))}");

            // Both cards at their worst: every alert at once, the longest command keys.
            var t12Many = Enumerable.Range(0, DigestBrief.FailingListed).Select(i => new DigestAlert(DigestAlertKind.FailingCommand, 99_999, 999_999, $"config some-long-group sub-command-{i}"))
                .Concat(Enum.GetValues<DigestAlertKind>().Select(k => new DigestAlert(k, 99_999, 999_999, "x"))).ToList();
            var t12Owner = ProjectSYNCS.Commands.ReportCards.BuildOwnerBrief(
                new(T12Cmd(new CommandLine("help", 999_999, 99_999)), T12Cmd()), new(T12Syncs(30, 81_234, 9_999_999, 999_999, (ProjectSYNCS.Models.UptimeEventKind.Crash, 99)), T12Syncs(30, 1, 1, 1)),
                new(T12Quiz(Enum.GetValues<QuizDifficulty>().Select(d => new QuizGroupLine(QuizBank.Label(d), 999, 999)).ToArray()), null),
                new(new MinecraftReportData(999_999_999, 31, 999_999_999, Array.Empty<McCountLine>(), 0, 0, 0, 0, 0, Array.Empty<McCountLine>()),
                    new MinecraftReportData(1, 1, 1, Array.Empty<McCountLine>(), 0, 0, 0, 0, 0, Array.Empty<McCountLine>())),
                t12Many, t12Month, "calme");
            var t12Activity = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(),
                Enumerable.Range(0, 60).Select(i => new ChannelInfo((ulong)i, false, false)).ToList());
            var t12TataCard = ProjectSYNCS.Commands.ReportCards.BuildTataBrief(new(T12Community(999, 999, 999) with { Xp = 65 }, null), new(t12Sessions, null),
                t12Activity, t12Sleeping, 99, t12Many, t12Month, "calme", LongestName);
            var t12Shares = ProjectSYNCS.Commands.ReportCards.BuildTataBrief(new(T12Community(9, 0, 0, 60) with { Xp = 65 }, null), new(t12Sessions, null),
                t12Activity, t12Sleeping, 0, Array.Empty<DigestAlert>(), t12Month, "calme", _ => "Bob").Fields.Single(f => f.Name == "Les plus actifs").Value;
            if (!t12Shares.Contains("— 46 %") || !t12Shares.Contains("À l'écrit") || !t12Shares.Contains("— 100 %") || !t12Shares.Contains("— 50 %")) Fail($"tata brief shares: {t12Shares}");
            foreach (var (t12What, t12Embed) in new[] { ("owner brief", t12Owner), ("tata brief", t12TataCard) })
            {
                if (t12Embed.Length > 6000 || t12Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"{t12What}: {t12Embed.Length} characters");
                ReportCardChecks.batch2Widest[t12What] = t12Embed.Length;
            }
            var t12Top = string.Join(" · ", new (ulong Id, string Share)[] { (2, "46 %"), (3, "31 %"), (1, "15 %") }
                .Select(t => $"{ProjectSYNCS.Commands.ReportCards.Who(t.Id, LongestName)} — {t.Share}"));
            if (!t12TataCard.Fields.Any(f => f.Value.Contains(t12Top))) Fail("tata brief: the most active, by XP");

            // A person in a report: the name written out (a mention alone shows a raw id in a DM), sanitized,
            // with the mention kept; the mention alone when no name is known.
            if (ProjectSYNCS.Commands.ReportCards.Who(7, _ => "Bob") != "**Bob** (<@7>)") Fail("report name: name then mention");
            if (ProjectSYNCS.Commands.ReportCards.Who(7, _ => "**@everyone**").Contains("**@everyone**")) Fail("report name: not sanitized");
            if (ProjectSYNCS.Commands.ReportCards.Who(7, _ => null) != "<@7>" || ProjectSYNCS.Commands.ReportCards.Who(7, null) != "<@7>")
                Fail("report name: no name known must keep the mention alone");
            if (!ProjectSYNCS.Commands.ReportCards.BuildSleeping(new SleepingReportData(30, 20, new[] { new Sleeper(7, null) }), _ => "Bob")
                    .Fields.Any(f => f.Value.Contains("**Bob** (<@7>)")))
                Fail("sleeping report: the sleeper's name is not written out");
            if (t12Owner.Fields.Last().Value.Contains("calme") || !ProjectSYNCS.Commands.ReportCards.BuildOwnerBrief(new(T12Cmd(), null), new(T12Syncs(0, null, 0, 0), null),
                    new(T12Quiz(), null), new(MinecraftReport.Compute(Array.Empty<ProjectSYNCS.Models.MinecraftDailyStat>()), null),
                    Array.Empty<DigestAlert>(), t12Month, "calme").Fields.Last().Value.Equals("calme"))
                Fail("« À regarder » shows the alerts, or her quiet line when there are none");
            // Papa's brief: her hours in game, before « À regarder », which stays last.
            var t12NoGame = ProjectSYNCS.Commands.ReportCards.BuildOwnerBrief(new(T12Cmd(), null), new(T12Syncs(0, null, 0, 0), null), new(T12Quiz(), null),
                new(MinecraftReport.Compute(Array.Empty<ProjectSYNCS.Models.MinecraftDailyStat>()), null), Array.Empty<DigestAlert>(), t12Month, "calme");
            if (t12NoGame.Fields.Where(f => f.Name == "Minecraft").Select(f => f.Value).FirstOrDefault() != "Pas joué ce mois-ci." || t12NoGame.Fields.Last().Name != "À regarder")
                Fail("owner brief: a month she didn't play");
            if (t12Owner.Fields.Where(f => f.Name == "Minecraft").Select(f => f.Value).FirstOrDefault() is not { } t12Game || !t12Game.Contains("en jeu") || t12Owner.Fields.Last().Name != "À regarder")
                Fail("owner brief: her hours in game");
        }

        // 34. The digest's honesty: partial months, trends per counted day, /debug apart, reach, incidents, her disk.
        {
            var dOct = ReportWindow.CalendarMonth(2026, 10);
            var dNov = ReportWindow.CalendarMonth(2026, 11);
            const int dStart = 20261019;

            // A counter started on 19 October covers 13 of October's 31 days, and all of November's.
            if (dOct.TotalDays != 31 || dOct.CountedDays(dStart) != 13 || !dOct.IsPartial(dStart)) Fail("partial month: October from the 19th");
            if (dOct.CountedDays(null) != 31 || dOct.IsPartial(null) || dOct.CountedDays(20260801) != 31 || dOct.IsPartial(20260801)) Fail("partial month: a counter older than the window");
            if (dOct.CountedDays(20261201) != 0) Fail("partial month: a counter newer than the window");
            if (dNov.Pace(dStart) != new ReportPace(30, 13)) Fail($"pace {dNov.Pace(dStart)}");
            if (ReportWindow.From(StatsPeriod.AllTime, DateTimeOffset.UtcNow).IsPartial(dStart)) Fail("all time is never partial");

            // The same pace over a 13-day month and a whole one is no change; raw totals would say +130 %.
            if (TextChart.Delta(1490, 646, new ReportPace(30, 13)) != "=") Fail($"per-day trend: {TextChart.Delta(1490, 646, new ReportPace(30, 13))}");
            if (TextChart.Delta(2980, 646, new ReportPace(30, 13)) != "+100 %") Fail($"per-day growth: {TextChart.Delta(2980, 646, new ReportPace(30, 13))}");
            if (TextChart.Delta(5, 0, new ReportPace(30, 0)) != "nouveau" || TextChart.Delta(0, 0, new ReportPace(30, 13)) != "" || TextChart.Delta(5, null, new ReportPace(30, 13)) != "")
                Fail("per-day trend: nothing to compare with");

            // /debug: out of the totals, the ranking and the never-used lists, its failures still flagged.
            ProjectSYNCS.Models.CommandDailyStat DCmd(string command, long uses, long failures = 0) =>
                new() { GuildId = 1, Day = dStart, Command = command, Uses = uses, Failures = failures };
            ProjectSYNCS.Models.CommandMemberDailyStat DWho(string command, ulong user, int day = dStart) =>
                new() { GuildId = 1, Day = day, Command = command, UserId = user };
            var dCmd = CommandReport.Compute(
                new[] { DCmd("work", 10), DCmd("debug minecraft", 47, 3), DCmd(";work", 2) },
                new[] { "work", "debug minecraft", "debug answers", "arcade duel", "config main-channel set", "admin warn" },
                new[] { DWho("work", 1), DWho("work", 2), DWho("work", 1, dStart + 1), DWho("plynling view", 2), DWho("debug minecraft", 9) });
            if (dCmd.Uses != 12 || dCmd.DebugUses != 47 || dCmd.Failures != 0) Fail($"debug in the totals: {dCmd.Uses}/{dCmd.DebugUses}/{dCmd.Failures}");
            if (dCmd.Top.Any(l => CommandPath.IsDebug(l.Command)) || dCmd.UsesByCommand.Keys.Any(CommandPath.IsDebug)) Fail("debug in the ranking");
            if (dCmd.Failing.SingleOrDefault()?.Command != "debug minecraft") Fail("a failing /debug must still be flagged");
            if (!dCmd.NeverUsed.SequenceEqual(new[] { "arcade duel" }) || !dCmd.NeverUsedStaff.SequenceEqual(new[] { "admin warn", "config main-channel set" }))
                Fail($"never used split: {string.Join(",", dCmd.NeverUsed)} / {string.Join(",", dCmd.NeverUsedStaff)}");

            // Reach: different people, not uses; /debug is nobody.
            if (dCmd.People.Count != 2 || dCmd.PeopleByCommand["work"] != 2 || dCmd.Features[0] != ("plynling", 1) && dCmd.Features[0] != ("work", 2)) Fail("reach counts");
            if (dCmd.Features.Any(f => f.Feature == "debug") || dCmd.People.Contains(9)) Fail("debug in the reach");
            var dBefore = CommandReport.Compute(Array.Empty<ProjectSYNCS.Models.CommandDailyStat>(), Array.Empty<string>(), new[] { DWho("work", 2), DWho("work", 3) });
            if (CommandReport.Returning(new(dCmd, dBefore)) != 1 || CommandReport.Returning(new(dCmd, null)) is not null) Fail("people already there before");

            var dCard = ProjectSYNCS.Commands.ReportCards.BuildCommands(new(dCmd, null, dStart), dOct, true);
            if (!dCard.Description.Contains("Mois partiel") || !dCard.Description.Contains("13 jours sur 31") || !dCard.Description.Contains("47 fois")) Fail($"commands card: {dCard.Description}");
            var dFields = dCard.Fields.Select(f => f.Name).ToList();
            if (dFields.FindIndex(f => f.StartsWith("Pas utilisées (")) >= dFields.FindIndex(f => f.Contains("côté staff"))) Fail("the members' never-used must come first");
            if (ProjectSYNCS.Commands.ReportCards.BuildCommands(new(dCmd, null, dStart), dNov, true).Description.Contains("partiel")) Fail("a whole month called partial");

            // The counter: a shortcut is the command it stands for, /debug is nobody, a person counts once a day.
            var dAt = new DateTimeOffset(2026, 11, 2, 20, 0, 0, TimeSpan.Zero);
            var dCounter = new ActivityCounter();
            dCounter.CountCommand(1, ";work", true, dAt, 7);
            dCounter.CountCommand(1, "work", true, dAt, 7);
            dCounter.CountCommand(1, "debug minecraft", true, dAt, 7);
            dCounter.CountCommand(1, "help", true, dAt);
            var dBatch = dCounter.Drain();
            if (dBatch.People.Count != 1 || !dBatch.People.Contains(new CommandPersonKey(1, 20261102, "work", 7))) Fail($"people counted: {string.Join(",", dBatch.People)}");
            if (dBatch.For(2).People.Count != 0 || dBatch.For(1).People.Count != 1) Fail("people split by guild");
            dCounter.Restore(dBatch);
            if (dCounter.Drain().People.Count != 1) Fail("Restore lost the people");
            foreach (var (dKey, dWanted) in new[] { (";pl", "plynling view"), (";forage", "plynling forage"), (";pl visit", "plynling visit"), (";pl ?", "plynling help"), ("work", "work") })
                if (CommandPath.Reached(dKey) != dWanted) Fail($"reached {dKey}");
            if (CommandPath.Verb("poll:republish:42") != "poll:republish" || CommandPath.Verb("poll:republish") != "poll:republish" || CommandPath.Verb("x") != "x") Fail("custom-id verb");
            if (!CommandPath.IsStaff("config main-channel set") || !CommandPath.IsStaff("report quiz") || CommandPath.IsStaff("arcade duel") || !CommandPath.IsDebug("debug") || CommandPath.IsDebug("debugger")) Fail("command groups");

            // Incidents: counted, drained apart from the batch, put back when a write fails.
            dCounter.CountIncident(IncidentKinds.Button, "poll:vote", dAt);
            dCounter.CountIncident(IncidentKinds.Button, "poll:vote", dAt);
            if (!dCounter.Drain().IsEmpty) Fail("incidents leaked into the batch");
            var dDrained = dCounter.DrainIncidents();
            if (dDrained[new IncidentKey(20261102, IncidentKinds.Button, "poll:vote")] != 2 || dCounter.DrainIncidents().Count != 0) Fail("incident drain");
            dCounter.RestoreIncidents(dDrained);
            if (dCounter.DrainIncidents().Values.Single() != 2) Fail("incident restore");

            // The log provider counts warnings and up, by logger, and a locked database once.
            var dLogged = new ActivityCounter();
            var dProvider = new IncidentLogProvider(dLogged);
            var dService = dProvider.CreateLogger("ProjectSYNCS.Services.MonthlyDigestService");
            var dEf = dProvider.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command");
            var dEfUpdate = dProvider.CreateLogger("Microsoft.EntityFrameworkCore.Update");
            var dBusy = new InvalidOperationException("wrapped", new Microsoft.Data.Sqlite.SqliteException("locked", 5));
            string DFormat(string s, Exception? e) => s;
            dService.Log(Microsoft.Extensions.Logging.LogLevel.Information, default, "x", null, DFormat);
            dService.Log(Microsoft.Extensions.Logging.LogLevel.Warning, default, "x", null, DFormat);
            dService.Log(Microsoft.Extensions.Logging.LogLevel.Error, default, "x", null, DFormat);
            dEf.Log(Microsoft.Extensions.Logging.LogLevel.Error, default, "x", dBusy, DFormat);
            dEfUpdate.Log(Microsoft.Extensions.Logging.LogLevel.Error, default, "x", dBusy, DFormat);
            dEf.Log(Microsoft.Extensions.Logging.LogLevel.Error, default, "x", new InvalidOperationException("other"), DFormat);
            var dLogs = dLogged.DrainIncidents().ToDictionary(x => (x.Key.Kind, x.Key.Key), x => x.Value);
            if (dLogs.GetValueOrDefault((IncidentKinds.LogWarning, "MonthlyDigestService")) != 1 || dLogs.GetValueOrDefault((IncidentKinds.LogError, "MonthlyDigestService")) != 1
                || dLogs.GetValueOrDefault((IncidentKinds.LogError, "EF Core")) != 3 || dLogs.GetValueOrDefault((IncidentKinds.SqliteBusy, "")) != 1 || dLogs.Count != 4)
                Fail($"log counts: {string.Join(", ", dLogs)}");

            // The incident report's arithmetic, and the alerts it raises.
            ProjectSYNCS.Models.IncidentDailyStat DInc(string kind, string key, long count) => new() { Day = dStart, Kind = kind, Key = key, Count = count };
            var dInc = IncidentReport.Compute(new[]
            {
                DInc(IncidentKinds.Slash, "work", 1), DInc(IncidentKinds.Button, "poll:vote", 4), DInc(IncidentKinds.Modal, "event:editmodal", 1),
                DInc(IncidentKinds.Late, "plynling view", 6), DInc(IncidentKinds.LogError, "EF Core", 25), DInc(IncidentKinds.LogWarning, "BotService", 2),
                DInc(IncidentKinds.SqliteBusy, "", 1), DInc(IncidentKinds.Disconnect, "", 3), DInc(IncidentKinds.Restart, "Clean", 5), DInc(IncidentKinds.Restart, "Crash", 1),
            });
            if (dInc.Failed != 6 || dInc.Buttons != 4 || dInc.Late != 6 || dInc.TopFailed[0].Key != "poll:vote" || dInc.TopLogs[0].Key != "EF Core"
                || dInc.Restarts[0] != ("Clean", 5) || dInc.Disconnects != 3)
                Fail($"incident report: {dInc}");

            // Her disk: sizes keep the last reading, peaks the worst, and the pace needs a week of days.
            ProjectSYNCS.Models.PiBodyDay DBody(int day, long database, long free, long process, long available) =>
                new() { Day = day, DatabaseBytes = database, DiskFreeBytes = free, DiskTotalBytes = 1000, ProcessPeakBytes = process, MemoryMinAvailableBytes = available, MemoryTotalBytes = 100 };
            var dRow = new ProjectSYNCS.Models.PiBodyDay { Day = dStart };
            PiBodyReport.Add(dRow, new PiBodySample(100, 7, 500, 1000, 50, (100L, 40L), true, false, 2));
            PiBodyReport.Add(dRow, new PiBodySample(null, 3, 490, 1000, 30, (100L, 60L), true, true, 2));
            if (dRow.DatabaseBytes != 100 || dRow.DiskFreeBytes != 490 || dRow.WalPeakBytes != 7 || dRow.ProcessPeakBytes != 50 || dRow.MemoryMinAvailableBytes != 40
                || dRow.UnderVoltageMinutes != 4 || dRow.SlowedMinutes != 2)
                Fail("body sample folded wrong");
            var dWeek = Enumerable.Range(0, 11).Select(i => DBody(20261101 + i, 100 + i, 500 - 10 * i, 50 + i, 40 - i)).ToList();
            var dBody = PiBodyReport.Compute(dWeek)!;
            if (dBody.Database != 110 || dBody.DatabaseGrowth != 10 || dBody.DiskFree != 400 || dBody.DaysUntilFull != 40 || dBody.ProcessPeak != 60 || dBody.MemoryMinAvailable != 30 || dBody.FirstDay != 20261101)
                Fail($"body report: {dBody}");
            if (PiBodyReport.Compute(dWeek.Take(3).ToList())!.DaysUntilFull is not null) Fail("a disk's pace from three days");
            if (PiBodyReport.Compute(Array.Empty<ProjectSYNCS.Models.PiBodyDay>()) is not null) Fail("no body rows must be nothing");

            // The uptime says what it covers: from the first reading of the window, 9 October.
            var dThermal = Enumerable.Range(9, 23).Select(i => new ProjectSYNCS.Models.PiThermalDay { Day = 20261000 + i, MinMilli = 40_000, MaxMilli = 50_000, SumMilli = 45_000, Samples = 1 }).ToList();
            var dSyncs = SyncsReport.Compute(dThermal, Array.Empty<ProjectSYNCS.Models.UptimeEvent>(), Array.Empty<ProjectSYNCS.Models.ChatAnswerDailyStat>(),
                Array.Empty<ProjectSYNCS.Models.BotFeedbackDailyStat>(), Array.Empty<ProjectSYNCS.Models.VoiceCompanionDailyStat>(), dOct, new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
            if (dSyncs.UptimeDays != 23 || dSyncs.UptimeFromDay != 20261009) Fail($"uptime span {dSyncs.UptimeDays} from {dSyncs.UptimeFromDay}");
            var dSyncsCard = ProjectSYNCS.Commands.ReportCards.BuildSyncs(new(dSyncs with { Body = dBody }, null, 20261009), dOct, null, true);
            if (!dSyncsCard.Fields.Any(f => f.Value.Contains("sur 23 jours (depuis le 09/10)"))) Fail("syncs card: the uptime's span");
            if (!dSyncsCard.Fields.Any(f => f.Name.StartsWith("Disque") && f.Value.Contains("plein dans 40 jours"))) Fail("syncs card: the disk's pace");

            // « Champs » counted produce, not fields.
            var dMc = ProjectSYNCS.Commands.ReportCards.BuildMinecraft(new(new MinecraftReportData(9, 1, 0, Array.Empty<McCountLine>(), 0, 0, 0, 0, 167, Array.Empty<McCountLine>()), null, 20261027), dOct, true);
            if (!dMc.Fields.Any(f => f.Value.Contains("Cultures récoltées : **167**")) || !dMc.Description.Contains("5 jours sur 31")) Fail($"minecraft card: {dMc.Description}");

            // Alerts: every new one raised at once, and a quiet month raising none.
            var dQuiet = new CommandReportData(0, 0, Array.Empty<CommandLine>(), Array.Empty<CommandLine>(), Array.Empty<string>(), new Dictionary<string, long>(), Array.Empty<CommandLine>());
            var dQuiz = new QuizReportData(0, 0, 0, Array.Empty<QuizGroupLine>(), Array.Empty<QuizGroupLine>(), null, null, Array.Empty<string>(), Array.Empty<PersonTally>());
            var dHeavy = dBody with { DiskFree = 100, DaysUntilFull = 40, MemoryMinAvailable = 5, UnderVoltageMinutes = 2 };
            var dKinds = DigestBrief.Owner(new(dQuiet, null), new(dSyncs with { Body = dHeavy }, null), null, new(dQuiz, null), dOct, dInc).Select(a => a.Kind).ToList();
            if (!dKinds.SequenceEqual(new[] { DigestAlertKind.InteractionFailures, DigestAlertKind.LateAnswers, DigestAlertKind.LogErrors, DigestAlertKind.SqliteBusy,
                    DigestAlertKind.DiskLow, DigestAlertKind.MemoryLow, DigestAlertKind.UnderVoltage }))
                Fail($"incident alerts: {string.Join(", ", dKinds)}");
            var dCalm = IncidentReport.Compute(new[] { DInc(IncidentKinds.Button, "x", 2), DInc(IncidentKinds.Late, "x", 4), DInc(IncidentKinds.LogError, "x", 19), DInc(IncidentKinds.Restart, "Clean", 9) });
            if (DigestBrief.Owner(new(dQuiet, null), new(dSyncs with { Body = dBody with { DiskFree = 900, DaysUntilFull = null } }, null), null, new(dQuiz, null), dOct, dCalm).Count != 0)
                Fail("a calm month's incidents raised something");

            // Every card at its worst: the longest keys, 9-digit counts, every field present.
            IncidentLine DLong(string kind, int i) => new(kind, $"some-long-custom-id-verb:or-a-very-long-logger-name-{i}", 999_999_999);
            var dWorstInc = new IncidentReportData(999_999_999, 999_999_999, 999_999_999, 999_999_999, 999_999_999, 999_999_999, 999_999_999, 999_999_999,
                new[] { ("Clean", 99_999L), ("PowerCut", 99_999L), ("Crash", 99_999L), ("Unknown", 99_999L) },
                Enumerable.Range(0, IncidentReport.Listed).Select(i => DLong(IncidentKinds.Button, i)).ToList(),
                Enumerable.Range(0, IncidentReport.Listed).Select(i => DLong(IncidentKinds.Late, i)).ToList(),
                Enumerable.Range(0, IncidentReport.Listed).Select(i => DLong(IncidentKinds.LogError, i)).ToList());
            var dWorstCmd = new CommandReportData(9_999_999, 99_999,
                Enumerable.Range(0, CommandReport.TopCommands).Select(i => new CommandLine($"plynling some-long-subcommand-{i}", 999_999, 999)).ToList(),
                new[] { new CommandLine(";pl", 9999, 0), new CommandLine(";work", 9999, 0), new CommandLine(";pl visit", 9999, 0) },
                Enumerable.Range(0, 150).Select(i => $"plynling some-group-name sub-{i}").ToList(),
                new Dictionary<string, long>(), Array.Empty<CommandLine>())
            {
                NeverUsedStaff = Enumerable.Range(0, 150).Select(i => $"config some-group-name sub-{i}").ToList(),
                DebugUses = 999_999,
                People = Enumerable.Range(0, 400).Select(i => (ulong)i).ToHashSet(),
                PeopleByCommand = Enumerable.Range(0, CommandReport.TopCommands).ToDictionary(i => $"plynling some-long-subcommand-{i}", i => 400),
                Features = Enumerable.Range(0, 60).Select(i => ($"some-long-feature-{i}", 400)).ToList(),
                PeopleSince = dStart,
            };
            var dWorstBody = new PiBodyReportData(20261101, long.MaxValue / 4, long.MaxValue / 8, long.MaxValue / 4, long.MaxValue / 4, long.MaxValue / 2, 30, long.MaxValue / 4, long.MaxValue / 4, long.MaxValue / 2, 99_999, 99_999);
            var dWorstSyncs = new SyncsReportData(31, 65_432, 81_234, 20261015, 9999, Enumerable.Repeat(70_000L, 30).ToList(),
                new[] { (ProjectSYNCS.Models.UptimeEventKind.PowerCut, 99), (ProjectSYNCS.Models.UptimeEventKind.Crash, 99), (ProjectSYNCS.Models.UptimeEventKind.NetworkOutage, 99) },
                TimeSpan.FromDays(9), 0.99, 9_999_999, 999_999, 999_999, 999_999, 999_999,
                Enumerable.Range(0, SessionReport.ListSize).Select(i => new PersonTally((ulong)i + 1_234_567_890_123_456_789, 99_999, 0)).ToList())
            {
                UptimeDays = 31, UptimeFromDay = 20261002, AnswersSince = dStart, VoiceSince = dStart, Body = dWorstBody,
            };
            var dAlerts = Enum.GetValues<DigestAlertKind>().Select(k => new DigestAlert(k, 99_999, 999_999, "x")).ToList();
            foreach (var (dWhat, dEmbed) in new[]
            {
                ("incidents", ProjectSYNCS.Commands.ReportCards.BuildIncidents(new(dWorstInc, dWorstInc, dStart), dOct, true)),
                ("commands with reach", ProjectSYNCS.Commands.ReportCards.BuildCommands(new(dWorstCmd, dWorstCmd, dStart), dOct, true)),
                ("syncs with body", ProjectSYNCS.Commands.ReportCards.BuildSyncs(new(dWorstSyncs, dWorstSyncs, dStart), dOct, (20260801, 84_000), true, LongestName)),
                ("owner brief with incidents", ProjectSYNCS.Commands.ReportCards.BuildOwnerBrief(new(dWorstCmd, dWorstCmd, dStart), new(dWorstSyncs, dWorstSyncs, dStart), new(dQuiz, null),
                    new(MinecraftReport.Compute(Array.Empty<ProjectSYNCS.Models.MinecraftDailyStat>()), null), dAlerts, dOct, "calme", new(dWorstInc, dWorstInc, dStart))),
            })
            {
                ReportCardChecks.batch2Widest[dWhat] = dEmbed.Length;
                if (dEmbed.Length > 6000) Fail($"{dWhat}: {dEmbed.Length} characters");
                if (dEmbed.Fields.Length > 25 || dEmbed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"{dWhat}: an empty, oversized or 26th field");
                if (dEmbed.Description is { Length: > 4096 }) Fail($"{dWhat}: description too long");
            }
            if (ProjectSYNCS.Commands.ReportCards.BuildIncidents(new(IncidentReport.Compute(Array.Empty<ProjectSYNCS.Models.IncidentDailyStat>()), null), dOct, true).Fields.Length != 0)
                Fail("incidents card: nothing counted yet");
            foreach (var dAlert in dAlerts)
                if (ProjectSYNCS.Commands.ReportCards.AlertLine(dAlert).Contains("sans signe de vie") != (dAlert.Kind == DigestAlertKind.NewSleepers)) Fail($"alert line for {dAlert.Kind} fell through");
        }

        Console.WriteLine($"batch 2 report embeds at worst: {string.Join(" · ", ReportCardChecks.batch2Widest.Select(x => $"{x.Key} {x.Value}"))} chars");
    }
}
