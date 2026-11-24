using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 41–43. Session places, per-message counts, and scheduling.
public static class SchedulingChecks
{
    public static async Task Run()
    {
        // 41. Session places (SessionSeats): the waiting list opens only when a full session frees a place,
        // the « peut-être » nudge fires about an hour before with room left, and the cards stay sendable
        // with the biggest crowd a session or a poll can hold.
        {
            // A place opened: full before, not full after, capped.
            foreach (var (max, before, after, expected) in new[]
            {
                (4, 4, 3, true), (4, 5, 4, false), (4, 3, 2, false), (0, 10, 9, false), (4, 4, 4, false), (2, 2, 0, true),
            })
                if (SessionSeats.PlaceOpened(max, before, after) != expected) Fail($"seats: PlaceOpened({max}, {before}, {after})");
            foreach (var (oldMax, newMax, joined, expected) in new[]
            {
                (4, 6, 4, true), (4, 0, 4, true), (4, 4, 4, false), (4, 6, 3, false), (0, 6, 4, false), (4, 5, 6, false),
            })
                if (SessionSeats.PlaceOpenedByEdit(oldMax, newMax, joined) != expected) Fail($"seats: PlaceOpenedByEdit({oldMax}, {newMax}, {joined})");

            // The nudge window, and never on a full session.
            var sStart = new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);
            foreach (var (minutesBefore, max, joined, expected) in new[]
            {
                (60, 0, 3, true), (55, 0, 3, true), (70, 0, 3, true), (54, 0, 3, false), (71, 0, 3, false), (30, 0, 3, false),
                (-5, 0, 3, false), (60, 4, 3, true), (60, 4, 4, false), (60, 4, 5, false),
            })
                if (SessionSeats.NudgeDue(sStart, sStart.AddMinutes(-minutesBefore), max, joined) != expected)
                    Fail($"seats: NudgeDue {minutesBefore} min before, {joined}/{max}");

            // Mentions: capped with a count, nothing for nobody.
            if (SessionSeats.Mentions(Array.Empty<ulong>(), 20) != "") Fail("seats: empty roster");
            if (SessionSeats.Mentions(new ulong[] { 1, 2 }, 20) != "<@1>\n<@2>") Fail("seats: short roster");
            var sMany = Enumerable.Range(1, 25).Select(i => (ulong)i).ToList();
            var sCapped = SessionSeats.Mentions(sMany, 20);
            if (sCapped.Split('\n').Length != 21 || !sCapped.EndsWith("… et 5 autre(s)")) Fail($"seats: capped roster {sCapped}");

            // The session card with 67 joined (the cap), 40 maybes and 40 absents, all with 20-digit ids.
            const ulong sBig = 12345678901234567890UL;
            var sEvent = new ProjectSYNCS.Models.SessionEvent
            {
                Id = 1, GuildId = 1, Title = new string('T', InputCaps.Title), ScheduledAt = sStart, MaxPlayers = 67,
                Participants = Enumerable.Range(0, 147).Select(i => new ProjectSYNCS.Models.Participant
                {
                    UserId = sBig - (ulong)i,
                    Status = i < 67 ? ProjectSYNCS.Models.ParticipantStatus.Joined
                        : i < 107 ? ProjectSYNCS.Models.ParticipantStatus.Maybe : ProjectSYNCS.Models.ParticipantStatus.Declined,
                }).ToList(),
            };
            // Components V2: 40 components in the whole tree, 4000 characters of text, no custom-id twice.
            (int Text, int Count, List<string> Ids) CardMeasure(Discord.IMessageComponent c)
            {
                var text = 0; var count = 1; var ids = new List<string>();
                void Add((int Text, int Count, List<string> Ids) r) { text += r.Text; count += r.Count; ids.AddRange(r.Ids); }
                switch (c)
                {
                    case Discord.ContainerComponent x: foreach (var k in x.Components) Add(CardMeasure(k)); break;
                    case Discord.SectionComponent x:
                        foreach (var k in x.Components) Add(CardMeasure(k));
                        if (x.Accessory is not null) Add(CardMeasure(x.Accessory));
                        break;
                    case Discord.ActionRowComponent x: foreach (var k in x.Components) Add(CardMeasure(k)); break;
                    case Discord.TextDisplayComponent x: text += x.Content.Length; break;
                    case Discord.ButtonComponent x: if (x.CustomId is not null) ids.Add(x.CustomId); break;
                    case Discord.SelectMenuComponent x: ids.Add(x.CustomId); break;
                }
                return (text, count, ids);
            }
            (int Text, int Count) CardCheck(Discord.MessageComponent card, string what)
            {
                var parts = card.Components.Select(CardMeasure).ToList();
                var (text, count, ids) = (parts.Sum(p => p.Text), parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
                if (text > 4000 || count > 40 || ids.Count != ids.Distinct().Count())
                    Fail($"seats: {what} at {text} chars, {count} components, {ids.Count - ids.Distinct().Count()} duplicate id(s)");
                return (text, count);
            }

            sEvent.DurationMinutes = 360;
            sEvent.Description = new string('d', InputCaps.Description);
            sEvent.VoiceChannelId = sBig;
            sEvent.NativeEventId = sBig;
            sEvent.RecurringSessionId = 1;
            var sCard = CardCheck(ProjectSYNCS.Commands.SessionCards.Build(sEvent, null, sStart.AddDays(-1)), "session card");
            CardCheck(ProjectSYNCS.Commands.SessionCards.Build(sEvent, null, sStart.AddMinutes(30)), "session card, running");
            sEvent.IsCancelled = true;
            CardCheck(ProjectSYNCS.Commands.SessionCards.Build(sEvent, null, sStart.AddDays(-1)), "session card, cancelled");
            sEvent.IsCancelled = false;

            // The poll card with 10 options of 80 characters, 40 voters on each.
            var sPoll = new ProjectSYNCS.Models.Poll
            {
                Id = 1, Title = new string('P', InputCaps.Title), Kind = ProjectSYNCS.Models.PollKind.Text,
                Options = Enumerable.Range(0, 10).Select(o => new ProjectSYNCS.Models.PollOption
                {
                    Id = o, Label = new string('o', 80),
                    Votes = Enumerable.Range(0, 40).Select(v => new ProjectSYNCS.Models.PollVote { UserId = sBig - (ulong)v }).ToList(),
                }).ToList(),
            };
            sPoll.CreatedAt = sStart.AddDays(-1);
            var sPollCard = CardCheck(ProjectSYNCS.Commands.PollCards.Build(sPoll, null, sStart), "poll card");
            sPoll.IsClosed = true;
            CardCheck(ProjectSYNCS.Commands.PollCards.Build(sPoll, null, sStart), "poll card, closed");
            sPoll.IsClosed = false;
            sPoll.Anonymous = true;
            CardCheck(ProjectSYNCS.Commands.PollCards.Build(sPoll, null, sStart), "poll card, anonymous");
            // A date poll: ten slots, every one a leader.
            var sDates = new ProjectSYNCS.Models.Poll
            {
                Id = 2, Title = new string('P', InputCaps.Title), Kind = ProjectSYNCS.Models.PollKind.DateSlots, CreatedAt = sStart,
                Options = Enumerable.Range(0, 10).Select(o => new ProjectSYNCS.Models.PollOption
                {
                    Id = o, ScheduledAt = sStart.AddDays(o + 1),
                    Votes = Enumerable.Range(0, 40).Select(v => new ProjectSYNCS.Models.PollVote { UserId = sBig - (ulong)v }).ToList(),
                }).ToList(),
            };
            CardCheck(ProjectSYNCS.Commands.PollCards.Build(sDates, null, sStart), "date poll card");
            sDates.IsClosed = true;
            CardCheck(ProjectSYNCS.Commands.PollCards.Build(sDates, "https://discord.com/channels/1/2/3", sStart), "date poll card, closed");

            // Both DM pools take their three placeholders.
            foreach (var pool in new[] { "SessionSpotFreedLines", "SessionMaybeNudgeLines" }.Select(n => (string[])br.GetField(n)!.GetValue(null)!))
                foreach (var line in pool)
                {
                    try { _ = string.Format(line, "Titre", "<t:0:R>", "https://discord.com/channels/1/2/3"); }
                    catch (FormatException) { Fail($"seats: pool line does not format: {line}"); }
                    if (line.Contains('⟪')) Fail($"seats: a DM line carries a sticker: {line}");
                }
            Console.WriteLine($"session card at worst: {sCard.Text} chars, {sCard.Count} components · poll card at worst: {sPollCard.Text} chars, {sPollCard.Count} components");
        }

        // 42. ActivityCounter's per-message counts: emotes, reactions used, her answers. Batched, drained,
        // put back after a failed flush; a reaction taken back nets out in the same batch.
        {
            var aAt = new DateTimeOffset(2026, 10, 27, 18, 0, 0, TimeSpan.Zero); // 19:00 in Paris
            var aDay = 20261027;
            var aCounter = new ActivityCounter();
            var aSmile = EmoteRef.FromUnicode("😀");
            var aCustom = EmoteRef.Custom(42, "syncs", false);
            aCounter.CountEmotesWritten(1, new Dictionary<EmoteRef, int> { [aSmile] = 3, [aCustom] = 1 }, aAt);
            aCounter.CountEmoteReacted(1, aSmile, +1, aAt);
            aCounter.CountEmoteReacted(1, aSmile, +1, aAt);
            aCounter.CountEmoteReacted(1, aSmile, -1, aAt);
            aCounter.CountReactionUsed(1, 7, +1, aAt);
            aCounter.CountReactionUsed(1, 7, -1, aAt);
            aCounter.CountReactionUsed(1, 8, +1, aAt);
            aCounter.CountAnswer(1, "reply", "comeback", null, aAt);
            aCounter.CountAnswer(1, "reply", "comeback", null, aAt);
            aCounter.CountAnswer(0, "dm", "unrecognised", "salut", aAt);
            aCounter.CountAnswer(1, "reply", "unrecognised", "", aAt);
            var aBatch = aCounter.Drain();
            if (aBatch.Emotes[new EmoteKey(1, aDay, aSmile)] != new EmoteTallyDelta(3, 1)) Fail("activity: emote written + net reacted");
            if (aBatch.Emotes[new EmoteKey(1, aDay, aCustom)] != new EmoteTallyDelta(1, 0)) Fail("activity: custom emote");
            if (aBatch.ReactionsUsed[new ReactionUsedKey(1, 7, aDay)] != 0 || aBatch.ReactionsUsed[new ReactionUsedKey(1, 8, aDay)] != 1)
                Fail("activity: reactions used net out");
            if (aBatch.Answers[new AnswerKey(1, aDay, "reply", "comeback")] != 2) Fail("activity: answers counted");
            if (aBatch.Unanswered.Count != 1 || aBatch.Unanswered[0].GuildId != 0) Fail("activity: only a non-empty unanswered text is kept");
            if (!aBatch.GuildIds.Contains(0UL) || !aBatch.GuildIds.Contains(1UL)) Fail("activity: DMs count under guild 0");
            if (aBatch.For(1).Unanswered.Count != 0 || aBatch.For(0).Answers.Count != 1) Fail("activity: batch split by guild");
            if (!aCounter.Drain().IsEmpty) Fail("activity: Drain left emote or answer counts behind");
            aCounter.CountEmoteReacted(1, aSmile, +1, aAt);
            aCounter.Restore(aBatch);
            var aBack = aCounter.Drain();
            if (aBack.Emotes[new EmoteKey(1, aDay, aSmile)] != new EmoteTallyDelta(3, 2) || aBack.Unanswered.Count != 1)
                Fail("activity: Restore did not merge emotes or unanswered");
        }

        // 43. Scheduling: typed dates and durations, weekly series across both DST changes, poll rules,
        // places, the fill bar, the per-card lock, the drafts and the card's custom-ids.
        {
            // Monday 27 October 2026, 13:00 in Paris (winter time since the 25th).
            var tNow = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset Utc(int y, int mo, int d, int h, int mi = 0) => new(y, mo, d, h, mi, 0, TimeSpan.Zero);
            foreach (var (text, expected) in new (string, DateTimeOffset?)[]
            {
                ("samedi 21h", Utc(2026, 10, 31, 20)), ("sam 14h", Utc(2026, 10, 31, 13)), ("ven 21h", Utc(2026, 10, 30, 20)),
                ("25/12 20:30", Utc(2026, 12, 25, 19, 30)), ("demain", Utc(2026, 10, 28, 20)), ("ce soir", Utc(2026, 10, 27, 20)),
                ("27/10 10:00", null), ("15/03 21h", null), ("n'importe quoi", null), ("", null),
            })
                if (SessionTime.Parse(text, tNow) != expected) Fail($"session time: « {text} » read as {SessionTime.Parse(text, tNow)}");

            // The way people type a date in chat (FrenchDate), at Tuesday 27 October 13:00 Paris.
            DateTimeOffset Paris(int mo, int d, int h, int mi = 0) =>
                AppTime.AtWallClock(new DateTime(2026, mo, d, h, mi, 0)).ToUniversalTime();
            var tChat = new (string, DateTimeOffset?)[]
            {
                // Today, in any order and spelling.
                ("ajd 20h", Paris(10, 27, 20)), ("Ajd 20H", Paris(10, 27, 20)), ("auj 20h30", Paris(10, 27, 20, 30)),
                ("20h ajd", Paris(10, 27, 20)), ("aujourd'hui à 20h", Paris(10, 27, 20)), ("ce soir vers 21h", Paris(10, 27, 21)),
                ("ce soir 9h", Paris(10, 27, 21)), ("ce soir 21h30", Paris(10, 27, 21, 30)),
                // Tomorrow and after, with a part of the day.
                ("demain soir", Paris(10, 28, 21)), ("demain soir 9h", Paris(10, 28, 21)), ("demain matin", Paris(10, 28, 9)),
                ("demain aprem", Paris(10, 28, 15)), ("demain après-midi 3h", Paris(10, 28, 15)), ("demain 21", Paris(10, 28, 21)),
                ("2main 21h", Paris(10, 28, 21)), ("dem 21h", Paris(10, 28, 21)), ("demain21h", Paris(10, 28, 21)),
                ("dès 20h demain", Paris(10, 28, 20)), ("après-demain", Paris(10, 29, 21)), ("apres dem 20h", Paris(10, 29, 20)),
                ("cette nuit 2h", Paris(10, 28, 2)), ("ce soir minuit", Paris(10, 28, 0)),
                // Weekdays.
                ("samedi soir", Paris(10, 31, 21)), ("sam soir 20h30", Paris(10, 31, 20, 30)), ("samedi 21", Paris(10, 31, 21)),
                ("samedi prochain 21h", Paris(10, 31, 21)), ("mardi 21h", Paris(10, 27, 21)), ("mardi prochain 21h", Paris(11, 3, 21)),
                ("samedi en huit", Paris(11, 7, 21)), ("samedi de la semaine prochaine", Paris(11, 7, 21)),
                ("ce week-end", Paris(10, 31, 21)), ("ce we 18h", Paris(10, 31, 18)), ("ce week-end dimanche 18h", Paris(11, 1, 18)),
                ("à partir de 21h samedi", Paris(10, 31, 21)), ("samedi minuit", Paris(11, 1, 0)), ("samedi midi", Paris(10, 31, 12)),
                // Times.
                ("21h et demie samedi", Paris(10, 31, 21, 30)), ("samedi 21h moins le quart", Paris(10, 31, 20, 45)),
                ("samedi 9pm", Paris(10, 31, 21)), ("samedi 9h du soir", Paris(10, 31, 21)), ("samedi 21 h 30", Paris(10, 31, 21, 30)),
                // Dates.
                ("le 25", Paris(11, 25, 21)), ("le 30 à 20h", Paris(10, 30, 20)), ("le 31", Paris(10, 31, 21)),
                ("25 décembre 20h", Paris(12, 25, 20)), ("25 dec 20h30", Paris(12, 25, 20, 30)), ("1er novembre 18h", Paris(11, 1, 18)),
                ("le 1er nov", Paris(11, 1, 21)), ("samedi 31 octobre 21h", Paris(10, 31, 21)), ("mardi 3 novembre", Paris(11, 3, 21)),
                ("25-12 20h", Paris(12, 25, 20)), ("25.12.2026 20h", Paris(12, 25, 20)), ("25 mar 2027", null), // past the horizon
                // In a while.
                ("dans 3 jours", Paris(10, 30, 21)), ("dans 2 semaines 20h", Paris(11, 10, 20)), ("dans 1h", Paris(10, 27, 14)),
                ("dans 45 min", Paris(10, 27, 13, 45)), ("dans 3 mois", null),
                // What it refuses rather than guess.
                ("vendredi 31 octobre", null), ("lundi 3 novembre", null), ("demain samedi", null), ("samedi 20h 21h", null),
                ("hier 21h", null), ("samedi 25h", null), ("le 32", null), ("21h", Paris(10, 27, 21)), ("bientôt", null),
            };
            foreach (var (text, expected) in tChat)
                if (SessionTime.Parse(text, tNow) != expected) Fail($"session time: « {text} » read as {SessionTime.Parse(text, tNow)}, expected {expected}");
            // « dans 1h » is rounded up to five minutes: 13:07 + 1h is 14:10.
            if (SessionTime.Parse("dans 1h", tNow.AddMinutes(7)) != Paris(10, 27, 14, 10)) Fail("session time: « dans 1h » not rounded up");
            if (("ex. " + FrenchDate.Examples).Length > 100) Fail("session time: the examples overflow an autocomplete label");
            Console.WriteLine($"typed dates: {tChat.Length} chat readings");

            var (tSlots, tRejected) = SessionTime.ParseList("ven 21h, sam 21h; n'importe quoi\nven 21h", tNow);
            if (tSlots.Count != 2 || tSlots[0] != Utc(2026, 10, 30, 20) || tRejected.SequenceEqual(new[] { "n'importe quoi" }) is false)
                Fail($"session time: list read as {string.Join(", ", tSlots)} / {string.Join(", ", tRejected)}");
            if (SessionTime.ParseList(string.Join(",", Enumerable.Range(1, 15).Select(d => $"{d}/11 21h")), tNow).Slots.Count != SessionTime.MaxSlots)
                Fail("session time: the list isn't capped");
            if (SessionTime.Preview(Utc(2026, 10, 31, 20)) is var tPreview && (!tPreview.Contains("31") || !tPreview.EndsWith("à 21:00")))
                Fail($"session time: preview {tPreview}");
            foreach (var (text, expected) in new (string, int?)[]
            {
                ("2h", 120), ("1h30", 90), ("1 h 30", 90), ("90", 90), ("45 min", 45), ("6h", 360),
                ("10", null), ("7h", null), ("1h75", null), ("abc", null), ("", null),
            })
                if (SessionTime.ParseDuration(text) != expected) Fail($"duration: « {text} » read as {SessionTime.ParseDuration(text)}");
            if (SessionTime.DurationLabel(90) != "1 h 30" || SessionTime.DurationLabel(120) != "2 h" || SessionTime.DurationLabel(45) != "45 min")
                Fail("duration: labels");
            foreach (var m in new[] { 15, 45, 90, 120, 135, 360 })
                if (SessionTime.ParseDuration(SessionTime.DurationInput(m)) != m) Fail($"duration: {m} doesn't survive the edit modal");

            // The picker's one-click times: today while ahead, tomorrow, the coming weekend, each day once.
            string QuickText(DateTimeOffset now, int minuteOfDay) =>
                string.Join(" | ", SessionTime.QuickSlots(now, minuteOfDay).Select(s => $"{s.Label} {s.At:dd HH:mm}"));
            foreach (var (now, minuteOfDay, expected) in new (DateTimeOffset, int, string)[]
            {
                // Monday 13:00 Paris.
                (tNow, 21 * 60, "Ce soir 21h 27 20:00 | Demain 21h 28 20:00 | Samedi 21h 31 20:00 | Dimanche 21h 01 20:00"),
                // Monday 22:00 Paris: tonight is gone.
                (Utc(2026, 10, 27, 21), 21 * 60, "Demain 21h 28 20:00 | Samedi 21h 31 20:00 | Dimanche 21h 01 20:00"),
                // Friday afternoon: tomorrow is the Saturday, named once.
                (Utc(2026, 10, 30, 12), 21 * 60, "Ce soir 21h 30 20:00 | Demain 21h 31 20:00 | Dimanche 21h 01 20:00"),
                // Saturday 22:00: tomorrow is Sunday, and the Saturday is next week's.
                (Utc(2026, 10, 31, 21), 21 * 60, "Demain 21h 01 20:00 | Samedi 21h 07 20:00"),
                // An afternoon habit, with minutes.
                (Utc(2026, 10, 27, 8), 14 * 60 + 30, "Aujourd'hui 14h30 27 13:30 | Demain 14h30 28 13:30 | Samedi 14h30 31 13:30 | Dimanche 14h30 01 13:30"),
            })
                if (QuickText(now, minuteOfDay) is var quick && quick != expected) Fail($"quick slots at {now:dd HH:mm}: {quick}");
            var tTwoHours = TimeSpan.FromHours(2);
            if (!SessionTime.Overlaps(Utc(2026, 10, 31, 20), tTwoHours, Utc(2026, 10, 31, 21), tTwoHours)
                || SessionTime.Overlaps(Utc(2026, 10, 31, 20), tTwoHours, Utc(2026, 10, 31, 22), tTwoHours)
                || !SessionTime.Overlaps(Utc(2026, 10, 31, 19), TimeSpan.FromHours(6), Utc(2026, 10, 31, 20), TimeSpan.FromMinutes(30)))
                Fail("session time: overlaps");

            // The picker's memory: the last published session, per person and per server, in Paris time.
            var tHabits = new SessionHabits();
            tHabits.Remember(new ProjectSYNCS.Models.SessionEvent { GuildId = 1, OrganizerId = 7, Category = ProjectSYNCS.Models.SessionCategory.Movie, MaxPlayers = 6,
                DurationMinutes = 180, VoiceChannelId = 42, ScheduledAt = Utc(2026, 10, 31, 19, 30) });
            if (tHabits.For(1, 7) != new SessionHabit(ProjectSYNCS.Models.SessionCategory.Movie, 6, 180, 42, 20 * 60 + 30)
                || tHabits.For(2, 7) is not null || tHabits.For(1, 8) is not null)
                Fail($"session habits: {tHabits.For(1, 7)}");

            // A Thursday at 21:00 stays at 21:00 in Paris on both sides of each change of time.
            const int rNine = 21 * 60;
            var rOct = Recurrence.NextOccurrence(DayOfWeek.Thursday, rNine, Utc(2026, 10, 20, 0));
            var rOctNext = Recurrence.NextOccurrence(DayOfWeek.Thursday, rNine, rOct);
            if (rOct != Utc(2026, 10, 22, 19) || rOctNext != Utc(2026, 10, 29, 20)) Fail($"recurrence: October change {rOct} → {rOctNext}");
            var rMar = Recurrence.NextOccurrence(DayOfWeek.Thursday, rNine, Utc(2027, 3, 24, 0));
            var rMarNext = Recurrence.NextOccurrence(DayOfWeek.Thursday, rNine, rMar);
            if (rMar != Utc(2027, 3, 25, 20) || rMarNext != Utc(2027, 4, 1, 19)) Fail($"recurrence: March change {rMar} → {rMarNext}");
            if (Recurrence.SlotOf(rMarNext) != (DayOfWeek.Thursday, rNine)) Fail("recurrence: SlotOf");
            if (Recurrence.Badge(rOct) != "🔁 chaque jeudi") Fail($"recurrence: badge {Recurrence.Badge(rOct)}");
            // The same day, earlier: today's slot; later: next week's.
            if (Recurrence.NextOccurrence(DayOfWeek.Tuesday, rNine, tNow) != Utc(2026, 10, 27, 20)
                || Recurrence.NextOccurrence(DayOfWeek.Tuesday, 10 * 60, tNow) != Utc(2026, 11, 3, 9)) Fail("recurrence: same day");
            // A moved or cancelled occurrence is never posted again: the next one is after the last.
            var rSeries = new ProjectSYNCS.Models.RecurringSession { DayOfWeek = DayOfWeek.Thursday, MinuteOfDay = rNine, LastOccurrence = rOctNext };
            if (Recurrence.NextOccurrence(rSeries, Utc(2026, 10, 27, 0)) != Utc(2026, 11, 5, 20)) Fail("recurrence: after the last occurrence");
            if (!Recurrence.DueToPost(rOctNext, Utc(2026, 10, 27, 0), 3) || Recurrence.DueToPost(rOctNext, Utc(2026, 10, 25, 0), 3)
                || Recurrence.DueToPost(rOctNext, rOctNext.AddMinutes(1), 3)) Fail("recurrence: DueToPost");

            // Options: markers dropped, duplicates skipped, capped.
            var pOptions = PollRules.ParseOptions("- Among Us\n2) Gartic\namong us\n\n• Valorant\n10. Minecraft");
            if (!pOptions.SequenceEqual(new[] { "Among Us", "Gartic", "Valorant", "Minecraft" }))
                Fail($"poll rules: options read as {string.Join(" | ", pOptions)}");
            if (PollRules.ParseOptions(string.Join("\n", Enumerable.Range(1, 15).Select(i => $"Option {i}"))).Count != PollRules.MaxOptions
                || PollRules.ParseOptions(new string('x', 100)).Single().Length != PollRules.MaxLabelLength) Fail("poll rules: caps");
            var pPoll = new ProjectSYNCS.Models.Poll { CreatedAt = tNow.AddDays(-1) };
            if (pPoll.ClosesAt != tNow.AddDays(1) || !PollRules.IsOpen(pPoll, tNow) || PollRules.IsOpen(pPoll, tNow.AddDays(2))) Fail("poll rules: default deadline");
            if (PollRules.Extend(pPoll, TimeSpan.FromHours(24), tNow) != tNow.AddDays(2)) Fail("poll rules: extend an open poll");
            pPoll.EndsAt = tNow.AddHours(-1);
            if (PollRules.IsOpen(pPoll, tNow) || PollRules.Extend(pPoll, TimeSpan.FromHours(1), tNow) != tNow.AddHours(1)) Fail("poll rules: extend a lapsed poll");
            pPoll.EndsAt = tNow.AddDays(13);
            if (PollRules.Extend(pPoll, TimeSpan.FromHours(48), tNow) != tNow + PollRules.MaxLifetime) Fail("poll rules: extend past the cap");
            pPoll.IsClosed = true;
            if (PollRules.IsOpen(pPoll, tNow)) Fail("poll rules: closed by hand");

            // One-line options: commas or semicolons, then the same reading.
            var pInline = PollRules.ParseInlineOptions(" Valorant, Lethal Company;among us ,, Among Us ");
            if (!pInline.SequenceEqual(new[] { "Valorant", "Lethal Company", "among us" }))
                Fail($"poll rules: inline options read as {string.Join(" | ", pInline)}");

            // The weekend poll's clear winner: alone at the top, ahead, with enough votes.
            ProjectSYNCS.Models.PollOption Slot(DateTimeOffset at, params ulong[] voters) => new()
            {
                ScheduledAt = at,
                Votes = voters.Select((v, i) => new ProjectSYNCS.Models.PollVote { UserId = v, VotedAt = tNow.AddMinutes(i) }).ToList(),
            };
            ProjectSYNCS.Models.Poll Slots(params ProjectSYNCS.Models.PollOption[] o) => new() { Options = o.ToList() };
            var wFri = tNow.AddDays(2); var wSat = tNow.AddDays(3); var wPast = tNow.AddHours(-1);
            if (PollRules.ClearWinner(Slots(Slot(wFri, 1), Slot(wSat, 1, 2)), tNow, 2)?.ScheduledAt != wSat) Fail("poll rules: clear winner");
            if (PollRules.ClearWinner(Slots(Slot(wFri, 1, 3), Slot(wSat, 1, 2)), tNow, 2) is not null) Fail("poll rules: a tie is no clear winner");
            if (PollRules.ClearWinner(Slots(Slot(wFri), Slot(wSat, 1)), tNow, 2) is not null) Fail("poll rules: one vote is no clear winner");
            if (PollRules.ClearWinner(Slots(Slot(wPast, 1, 2, 3), Slot(wSat, 1, 2)), tNow, 2)?.ScheduledAt != wSat) Fail("poll rules: a past slot never wins");
            if (PollRules.ClearWinner(Slots(), tNow, 2) is not null) Fail("poll rules: no slot, no winner");
            var wVoters = PollRules.VotersAt(Slots(Slot(wFri, 5), Slot(wSat, 7, 5, 9)), wSat).Select(v => v.UserId);
            if (!wVoters.SequenceEqual(new ulong[] { 7, 5, 9 })) Fail($"poll rules: voters at a slot read as {string.Join(", ", wVoters)}");
            if (WeekendPoll.SessionTitle(Utc(2026, 10, 31, 20)) != "Soirée du samedi") Fail($"weekend poll: session title « {WeekendPoll.SessionTitle(Utc(2026, 10, 31, 20))} »");

            // Places: one rule at creation and at edit.
            foreach (var (input, ok, max) in new[]
            {
                ("", true, 0), ("0", true, 0), (" 5 ", true, 5), ("2", true, 2), ("67", true, 67), ("1", false, 0), ("68", false, 0), ("abc", false, 0), ("-3", false, 0),
            })
                if (SessionSeats.TryParseCap(input, out var got) != ok || (ok && got != max)) Fail($"seats: TryParseCap(« {input} »)");
            if (!SessionSeats.CapBelowJoined(3, 4) || SessionSeats.CapBelowJoined(0, 9) || SessionSeats.CapBelowJoined(4, 4)) Fail("seats: CapBelowJoined");
            var cSession = new ProjectSYNCS.Models.SessionEvent { ScheduledAt = tNow.AddHours(1) };
            if (!SessionSeats.CanRespond(cSession, tNow, out _)) Fail("seats: a future session refuses answers");
            if (SessionSeats.CanRespond(cSession, tNow.AddHours(1.5), out var cRunning) || !cRunning.Contains("commencé")) Fail("seats: a running session takes answers");
            if (SessionSeats.CanRespond(cSession, tNow.AddDays(1), out var cOver) || !cOver.Contains("terminée")) Fail("seats: a finished session takes answers");
            cSession.IsCancelled = true;
            if (SessionSeats.CanRespond(cSession, tNow, out var cCancelled) || !cCancelled.Contains("annulée")) Fail("seats: a cancelled session takes answers");

            // The fill bar: always its width, one vote never reads as none.
            foreach (var (part, total, filled) in new[] { (0L, 10L, 0), (10L, 10L, 10), (1L, 100L, 1), (5L, 10L, 5), (0L, 0L, 0), (3L, 0L, 0), (12L, 10L, 10) })
                if (TextChart.Bar(part, total, 10) is var bar && (bar.Length != 10 || bar.Count(ch => ch == '▰') != filled))
                    Fail($"bar: {part}/{total} drew {bar}");

            // The per-card lock: one holder at a time, and nothing kept once released.
            var kLock = new KeyedLock<int>();
            var kFirst = await kLock.AcquireAsync(1);
            var kSecond = kLock.AcquireAsync(1);
            var kOther = await kLock.AcquireAsync(2);
            if (kSecond.IsCompleted) Fail("lock: a second holder got in");
            kFirst.Dispose();
            (await kSecond).Dispose();
            kOther.Dispose();
            if (kLock.Count != 0) Fail($"lock: {kLock.Count} entries kept after release");

            // Drafts: kept while touched, gone after the TTL, taken once.
            var dClock = tNow;
            var dBox = new DraftBox<string>(TimeSpan.FromMinutes(30), () => dClock);
            dBox.Set(1, "a");
            dClock = dClock.AddMinutes(29);
            if (!dBox.TryGet(1, out _)) Fail("drafts: lost before the TTL");
            dClock = dClock.AddMinutes(29);
            if (!dBox.TryGet(1, out _)) Fail("drafts: touching doesn't refresh");
            dClock = dClock.AddMinutes(31);
            if (dBox.TryGet(1, out _) || dBox.Count != 0) Fail("drafts: kept past the TTL");
            dBox.Set(2, "b");
            if (!dBox.TryTake(2, out var dTaken) || dTaken != "b" || dBox.TryTake(2, out _)) Fail("drafts: taken twice");

            // A reply under the card finds it from any of its buttons, the new ones included.
            foreach (var (ids, expected) in new (string[], CardRef?)[]
            {
                (new[] { "poll:manage:5" }, new CardRef(CardKind.Poll, 5)), (new[] { "poll:mine:7" }, new CardRef(CardKind.Poll, 7)),
                (new[] { "event:manage:3" }, new CardRef(CardKind.Session, 3)), (new[] { "event:sub:3" }, new CardRef(CardKind.Session, 3)),
                (new[] { "poll:extend:5:24" }, null),
            })
                if (CardRefs.Read(ids) != expected) Fail($"card refs: {ids[0]}");

            // Results announced: the organiser, the title and the result all land, a sticker each.
            foreach (var name in new[] { "PollClosedSlotLines", "VoteClosedResultLines", "PollClosedEmptyLines" })
                foreach (var line in (string[])br.GetField(name)!.GetValue(null)!)
                {
                    var s = string.Format(line, "<@1>", "Titre", "Résultat");
                    if (!s.Contains("<@1>") || !s.Contains("Titre") || (name != "PollClosedEmptyLines" && !s.Contains("Résultat")))
                        Fail($"{name}: a placeholder is missing: {line}");
                }
        }
    }
}
