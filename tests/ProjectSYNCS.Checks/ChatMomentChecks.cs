using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 46–49, 58. Long absences, late replies, replies to her morning hello, edits, channel milestones.
public static class ChatMomentChecks
{
    public static void Run()
    {
        // 46. « Ça faisait longtemps » (LongAbsence): three weeks from the last active day, across months and
        // years; nobody seen before is not back from anywhere. No returning line says how long: nobody knows why they were away.
        {
            foreach (var (today, last, expected) in new (int, int?, int?)[]
            {
                (20261117, null, null), (20261117, 20261116, null), (20261117, 20261028, null), (20261117, 20261027, 21),
                (20260105, 20251201, 35), (20260301, 20260201, 28), (20260301, 20260208, 21), (20260301, 20260209, null),
            })
                if (LongAbsence.DaysAway(today, last) != expected)
                    Fail($"long absence: {last} → {today} is {LongAbsence.DaysAway(today, last)}, expected {expected}");
            foreach (var field in new[] { "ReturningGreetings", "OwnerReturningGreetings" })
                foreach (var l in (string[])Field(field))
                {
                    var low = l.ToLowerInvariant();
                    if (l.Contains("{2}") || low.Contains("jours") || low.Contains("semaines")) Fail($"{field}: says how long they were away: {l}");
                    if (low.Contains("bonjour") || low.Contains("bonsoir")) Fail($"{field}: greets for one time of day: {l}");
                }
        }

        // 47. Late replies (LateReply): her message's age in words, by Paris calendar day past six hours.
        {
            DateTimeOffset At(int d, int h, int mi = 0) => AppTime.AtWallClock(new DateTime(2026, 11, d, h, mi, 0));
            var written = At(17, 8);
            foreach (var (now, expected) in new (DateTimeOffset, string?)[]
            {
                (At(17, 13, 59), null), (At(17, 14), "il y a 6 heures"), (At(17, 23, 30), "il y a 15 heures"),
                (At(18, 1), "hier"), (At(18, 7, 59), "hier"), (At(20, 9), "il y a 3 jours"), (At(23, 9), "il y a 6 jours"),
                (At(24, 9), "la semaine dernière"), (At(30, 9), "la semaine dernière"),
                (AppTime.AtWallClock(new DateTime(2026, 12, 7, 9, 0, 0)), "il y a 2 semaines"),
                (AppTime.AtWallClock(new DateTime(2027, 1, 14, 9, 0, 0)), "il y a 8 semaines"),
                (AppTime.AtWallClock(new DateTime(2027, 3, 1, 9, 0, 0)), "il y a des mois"),
            })
                if (LateReply.Ago(written, now) != expected)
                    Fail($"late reply: {now:yyyy'-'MM'-'dd HH':'mm} reads « {LateReply.Ago(written, now)} », expected « {expected} »");
            // Across midnight but under six hours: still just a reply.
            if (LateReply.Ago(At(17, 22), At(18, 2)) is not null) Fail("late reply: four hours across midnight");
            // Amused at her own words, never at the person's timing.
            foreach (var field in new[] { "LateReplyLines", "OwnerLateReplyLines" })
                foreach (var l in ((string[])Field(field)).Select(l => l.ToLowerInvariant()))
                    if (l.Contains("enfin") || l.Contains("t'étais où") || l.Contains("en retard"))
                        Fail($"{field}: blames their timing: {l}");
        }

        // 58. Replies to her morning hello (MorningHelloGreetedPool): greeted back with her bonjour lines by
        // day, Papa, Tata and a chouchou with their own; at night none (her classic greetings follow the
        // hour); anything else answered without a greeting. In the wild she said « bonjour » to every reply.
        {
            var pool = br.GetMethod("MorningHelloGreetedPool")!;
            string[]? PoolFor(HelloGreeter who, int h) =>
                (string[]?)pool.Invoke(null, [who, AppTime.AtWallClock(new DateTime(2026, 11, 18, h, 0, 0))]);
            var contextLines = (IReadOnlyDictionary<ReplyContext, string[]>)Field("ReplyContextLines");
            var helloPlain = contextLines[ReplyContext.MorningHello];
            var greeted = (string[])Field("MorningHelloGreetedLines");
            var greetedBy = new Dictionary<HelloGreeter, string>
            {
                [HelloGreeter.Anyone] = "MorningHelloGreetedLines", [HelloGreeter.Papa] = "OwnerMorningHelloGreetedLines",
                [HelloGreeter.Tata] = "TataMorningHelloGreetedLines", [HelloGreeter.Chouchou] = "ChouchouMorningHelloGreetedLines",
            };
            foreach (var who in Enum.GetValues<HelloGreeter>())
            {
                if (!greetedBy.TryGetValue(who, out var field)) { Fail($"hello reply: no bonjour lines for {who}"); continue; }
                if (PoolFor(who, 9) != (string[])Field(field)) Fail($"hello reply: {who} greeting by day isn't {field}");
                if (PoolFor(who, 21) is not null) Fail($"hello reply: {who} greeted back with bonjour in the evening");
                foreach (var l in (string[])Field(field))
                    if (!l.ToLowerInvariant().Contains("bonjour")) Fail($"{field}: no bonjour: {l}");
            }
            // Papa is Papa: no name placeholder.
            foreach (var l in (string[])Field("OwnerMorningHelloGreetedLines"))
                if (l.Contains("{0}")) Fail($"OwnerMorningHelloGreetedLines: addresses him by name: {l}");

            // What decides it: the reply's own greeting, as the reply path reads it.
            bool Greets(string text)
            {
                var mood = analyzeClaim.Invoke(null, [text])!;
                return (bool)mood.GetType().GetProperty("IsGreeting")!.GetValue(mood)!;
            }
            foreach (var (text, expected) in new[]
            {
                ("bonjour !", true), ("coucou syncs", true), ("bien dormi ?", false), ("ok", false),
                ("t'es matinale aujourd'hui", false), ("moi j'ai mal dormi", false),
            })
                if (Greets(text) != expected) Fail($"hello reply: « {text} » greets = {!expected}");

            var helloWord = new System.Text.RegularExpressions.Regex(@"\b(mon|mes|deux|dire|dis|dit) bonjours?\b");
            foreach (var l in helloPlain)
            {
                var s = Sticker.Strip(string.Format(l, "Nom", "lundi")).ToLowerInvariant();
                if (Greets(s) || helloWord.Replace(s, "").Contains("bonjour") || s.Contains("bonsoir"))
                    Fail($"ReplyContextLines[MorningHello]: greets back: {s}");
            }
            if (greeted.Intersect(helloPlain).Any()) Fail("hello reply: a line in both halves");

            // Under the hello, what the reply says comes first: a question, news or a word gets its intent's
            // lines; every other post keeps its context line.
            var yields = asm.GetType("ProjectSYNCS.Services.ReplyContexts")!.GetMethod("YieldsToIntent")!;
            bool Yields(ReplyContext c, string intent) => (bool)yields.Invoke(null, [c, Enum.Parse(intentType, intent)])!;
            foreach (var intent in new[] { "Question", "Sharing", "LowEffort" })
            {
                if (!Yields(ReplyContext.MorningHello, intent)) Fail($"hello reply: {intent} gets the hello's line");
                if (Yields(ReplyContext.SessionCall, intent)) Fail($"hello reply: {intent} under the late call skips its line");
            }
            if (Yields(ReplyContext.MorningHello, "None")) Fail("hello reply: nothing read, yet no hello line");
            // How the usual answers to her hello read.
            foreach (var (text, expected) in new[]
            {
                ("bien dormi ?", "HowAreYou"), ("bonjour, bien dormi ?", "HowAreYou"), ("t'as fait quoi cette nuit ?", "Question"),
                ("j'ai trop mal dormi", "Sharing"), ("je commence le taf", "Sharing"), ("ok", "LowEffort"), ("👍", "LowEffort"),
                ("bonjour !", "None"),
            })
            {
                var got = readIntent.Invoke(null, [text])!.ToString();
                if (got != expected) Fail($"hello reply: ReadIntent(\"{text}\") = {got}, expected {expected}");
            }
        }

        // 48. Edits of a message she answered (EditReading): a typo, a case change or a link preview is
        // nothing; a rewrite, a flipped yes or no, a negation added, is real; the mean reading turning
        // either way wins. Her rewritten reply strikes her old words line by line and fits one message.
        {
            foreach (var (before, after, expected) in new[]
            {
                ("slaut", "salut", false), ("merci", "merci !", false), ("Merci beaucoup", "merci   beaucoup", false),
                ("ça va ?", "ça va ? 😄", false), ("je suis la", "je suis là", false), ("oui", "non", true),
                ("je viens ce soir", "je viens pas ce soir", true), ("ouais grave", "nan grave", true),
                ("ça va ?", "ok", true), ("t'as vu le match hier ?", "qui vient ce soir en session ?", true),
                ("Tu sais ce qu'on fait ce soir pour la session, on joue à quoi finalement ?",
                    "Tu sais ce qu'on fait ce soir pour la session, on joue à quoi finalement ??", false),
                (new string('a', 3000), new string('b', 3000), true), (new string('a', 3000), new string('a', 2990), false),
            })
                if (EditReading.IsRealChange(before, after) != expected)
                    Fail($"edits: « {before[..Math.Min(40, before.Length)]} » → « {after[..Math.Min(40, after.Length)]} » real change is {!expected}");

            if (EditReading.Read("merci", "merci", wasMean: false, isMean: true) != EditKind.TurnedMean
                || EditReading.Read("t'es nulle", "t'es cool", wasMean: true, isMean: false) != EditKind.CoveredUp
                || EditReading.Read("slaut", "salut", false, false) != EditKind.None
                || EditReading.Read("oui", "non", true, true) != EditKind.Rewritten)
                Fail("edits: Read misjudges the mean reading");

            if (EditReading.Struck("Coucou ♡", "Hmm.") != "~~Coucou ♡~~\nHmm.") Fail("edits: Struck, one line");
            if (EditReading.Struck("Un\n\nDeux ~~barré~~", "Hmm.") != "~~Un~~\n~~Deux barré~~\nHmm.") Fail("edits: Struck, lines");
            if (EditReading.Struck(new string('x', 1995), "Hmm.") != "Hmm." || EditReading.Struck(new string('x', 1990), "Hmm.").Length != 1999)
                Fail("edits: Struck at 2000");
            if (EditReading.Struck("", "Hmm.") != "Hmm.") Fail("edits: Struck, nothing of hers");

            // Her new lines only take a name, and every pool reads as an edit, not a fresh answer.
            foreach (var field in new[] { "EditRewrittenLines", "EditDeletedLines", "EditTurnedMeanLines", "EditCoveredUpLines",
                         "OwnerEditLines", "OwnerEditMeanLines" })
                foreach (var l in (string[])Field(field))
                    if (l.Contains("{1}") || l.Contains("{2}")) Fail($"{field}: only {{0}} is given: {l}");
        }

        // 49. Channel milestones (ChannelMilestones): 1 000, 10 000, 100 000, then every 100 000; the next
        // one after any count; the number written the French way.
        {
            foreach (var n in new long[] { 1000, 10000, 100000, 200000, 1300000 })
                if (!ChannelMilestones.IsMilestone(n)) Fail($"milestones: {n} is one");
            foreach (var n in new long[] { 0, 999, 5000, 50000, 150000, 100001, -1000 })
                if (ChannelMilestones.IsMilestone(n)) Fail($"milestones: {n} is not one");
            foreach (var (count, next) in new (long, long)[]
            {
                (0, 1000), (999, 1000), (1000, 10000), (9999, 10000), (10000, 100000), (100000, 200000), (250000, 300000),
            })
                if (ChannelMilestones.NextAfter(count) != next) Fail($"milestones: after {count} comes {ChannelMilestones.NextAfter(count)}, expected {next}");
            if (ChannelMilestones.NextUnreached(9990, new HashSet<long> { 1000, 10000 }) != 100000
                || ChannelMilestones.NextUnreached(500, new HashSet<long>()) != 1000)
                Fail("milestones: the next unreached one");
            var tenK = ChannelMilestones.Format(10000);
            if (tenK != 10000.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) || tenK.Contains(',') || tenK.Contains('.'))
                Fail($"milestones: 10000 written « {tenK} »");
            // /debug milestones at its worst: a 100-character channel name, 45 milestones (three pages),
            // 20-digit ids: every page under the embed caps, page 0 the newest, the last page the oldest,
            // and buttons only when there is more than a page, each with its own custom-id.
            var milestoneCards = asm.GetType("ProjectSYNCS.Commands.MilestoneCards")!;
            var milestoneCard = milestoneCards.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)!;
            var milestoneButtons = milestoneCards.GetMethod("BuildButtons", BindingFlags.Public | BindingFlags.Static)!;
            const ulong bigId = 12345678901234567890UL;
            var manyReached = Enumerable.Range(1, 45).Select(i => new ProjectSYNCS.Models.ChannelMilestone
            {
                GuildId = bigId, ChannelId = bigId, Milestone = i * 100_000L, MessageId = bigId - (ulong)i, AuthorId = bigId - (ulong)i,
                ReachedAt = DateTimeOffset.UtcNow, Announced = i % 2 == 0,
            }).ToList();
            Discord.Embed Card(List<ProjectSYNCS.Models.ChannelMilestone> list, bool caughtUp, int page) =>
                (Discord.Embed)milestoneCard.Invoke(null,
                    [bigId, new string('n', 100), new MilestoneView(4_523_456, caughtUp, list, 4_600_000), page])!;
            foreach (var page in new[] { -1, 0, 1, 2, 3 })
            {
                var embed = Card(manyReached, false, page);
                if (embed.Description.Length > 4096 || embed.Title.Length > 256 || embed.Length > 6000)
                    Fail($"milestones: /debug card page {page} is {embed.Description.Length} / {embed.Length} chars");
            }
            string Shown(int page) => Card(manyReached, true, page).Description;
            if (!Shown(0).Contains($"**{ChannelMilestones.Format(4_500_000)}**")
                || Shown(0).Contains($"**{ChannelMilestones.Format(2_500_000)}**")
                || !Shown(2).Contains($"**{ChannelMilestones.Format(100_000)}**")
                || Shown(2).Contains($"**{ChannelMilestones.Format(600_000)}**")
                || Card(manyReached, true, 1).Footer?.Text != "Paliers 6 à 25 sur 45")
                Fail("milestones: the pages don't slice newest first");
            var emptyCard = Card([], true, 0);
            if (!emptyCard.Description.Contains("Aucun") || emptyCard.Footer is not null) Fail("milestones: the empty card");

            Discord.MessageComponent? Buttons(int reached, int page) => (Discord.MessageComponent?)milestoneButtons.Invoke(null, [bigId, reached, page]);
            if (Buttons(20, 0) is not null || Buttons(0, 0) is not null) Fail("milestones: buttons on a single page");
            foreach (var page in new[] { 0, 1, 2 })
            {
                var row = Buttons(45, page)!.Components.OfType<Discord.ActionRowComponent>().Single().Components
                    .OfType<Discord.ButtonComponent>().ToList();
                if (row.Count != 2 || row[0].CustomId == row[1].CustomId) Fail($"milestones: buttons page {page} collide");
                if (row[0].IsDisabled != (page == 2) || row[1].IsDisabled != (page == 0)) Fail($"milestones: buttons page {page} ends");
                if (row[0].CustomId != $"milestones:older:{bigId}:{page + 1}" || row[1].CustomId != $"milestones:newer:{bigId}:{page - 1}")
                    Fail($"milestones: buttons page {page} lead nowhere: {row[0].CustomId} / {row[1].CustomId}");
            }

            // Every announcement says the number.
            foreach (var field in new[] { "MilestoneLines", "MilestoneBotLines", "MilestoneSelfLines" })
                foreach (var l in (string[])Field(field))
                    if (!l.Contains("{1}")) Fail($"{field}: doesn't say the number: {l}");
        }
    }
}
