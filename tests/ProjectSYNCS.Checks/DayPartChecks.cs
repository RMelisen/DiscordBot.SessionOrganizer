using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 45. Bonjour or bonsoir (DayParts, TimedPool).
public static class DayPartChecks
{
    public static void Run()
    {
        // 45. Bonjour or bonsoir (DayParts, TimedPool): the switch hours in Paris time across both DST
        // changes, and every time-aware pool keeps its greetings on the right side of them.
        DayPart PartAt(int y, int mo, int d, int h, int mi) => DayParts.At(AppTime.AtWallClock(new DateTime(y, mo, d, h, mi, 0)));
        // 30 March, not the 29th: on the night clocks go forward, 02:59 doesn't exist in Paris.
        foreach (var (mo, d) in new[] { (1, 15), (3, 30), (7, 1), (10, 25) })
        {
            if (PartAt(2026, mo, d, 2, 59) != DayPart.Evening) Fail($"dayparts {mo}/{d} 02:59: not evening");
            if (PartAt(2026, mo, d, 3, 0) != DayPart.Day) Fail($"dayparts {mo}/{d} 03:00: not day");
            if (PartAt(2026, mo, d, 17, 59) != DayPart.Day) Fail($"dayparts {mo}/{d} 17:59: not day");
            if (PartAt(2026, mo, d, 18, 0) != DayPart.Evening) Fail($"dayparts {mo}/{d} 18:00: not evening");
            if (PartAt(2026, mo, d, 0, 30) != DayPart.Evening) Fail($"dayparts {mo}/{d} 00:30: not evening");
        }
        // A UTC instant reads in Paris time: 16:30 UTC is 18:30 in summer, 17:30 in winter.
        if (DayParts.At(new DateTimeOffset(2026, 7, 1, 16, 30, 0, TimeSpan.Zero)) != DayPart.Evening) Fail("dayparts: summer UTC");
        if (DayParts.At(new DateTimeOffset(2026, 1, 15, 16, 30, 0, TimeSpan.Zero)) != DayPart.Day) Fail("dayparts: winter UTC");

        var tp = new TimedPool<string>(["a"], ["d"], ["e"]);
        var tpNoon = tp.At(AppTime.AtWallClock(new DateTime(2026, 6, 1, 12, 0, 0)));
        var tpNight = tp.At(AppTime.AtWallClock(new DateTime(2026, 6, 1, 23, 0, 0)));
        if (!tpNoon.SequenceEqual(["a", "d"]) || !tpNight.SequenceEqual(["a", "e"])) Fail("TimedPool: wrong half");

        // Broad daylight (a « bonne nuit » is a nap): 8:00 to the evening, Paris time.
        bool DaylightAt(int h, int mi) => DayParts.IsDaylight(AppTime.AtWallClock(new DateTime(2026, 7, 1, h, mi, 0)));
        if (DaylightAt(7, 59) || !DaylightAt(8, 0) || !DaylightAt(17, 59) || DaylightAt(18, 0) || DaylightAt(3, 0))
            Fail("dayparts: daylight window");

        // The clock intents: a goodbye by the hour (his twin for Papa), a goodnight teased only by day.
        var clock = br.GetMethod("ClockIntentLines")!;
        // A Wednesday, no week moment; roll 0 would take one if there were.
        string[]? Clocked(string intent, bool owner, int h) => (string[]?)clock.Invoke(null,
            [Enum.Parse(intentType, intent), owner, AppTime.AtWallClock(new DateTime(2026, 7, 1, h, 0, 0)), 0.0]);
        var noonBye = Clocked("Goodbye", false, 12)!;
        var nightBye = Clocked("Goodbye", false, 22)!;
        if (!noonBye.Any(l => l.Contains("journée")) || noonBye.Any(l => l.Contains("soirée")))
            Fail("clock intents: a noon goodbye without its day lines");
        if (!nightBye.Any(l => l.Contains("soirée")) || nightBye.Any(l => l.Contains("journée")))
            Fail("clock intents: an evening goodbye without its evening lines");
        var ownerBye = (TimedPool<string>)Field("OwnerGoodbyeLines");
        if (!Clocked("Goodbye", true, 12)!.SequenceEqual(ownerBye.At(AppTime.AtWallClock(new DateTime(2026, 7, 1, 12, 0, 0)))))
            Fail("clock intents: Papa's goodbye is not his twin");
        if (Clocked("Goodnight", false, 14) != (string[])Field("DaytimeGoodnightLines")
            || Clocked("Goodnight", true, 14) != (string[])Field("OwnerDaytimeGoodnightLines"))
            Fail("clock intents: a daytime goodnight isn't teased");
        if (Clocked("Goodnight", false, 23) is not null || Clocked("Goodnight", false, 5) is not null
            || Clocked("Love", false, 12) is not null)
            Fail("clock intents: an intent off the clock got a clock pool");

        // Week moments (WeekMoments): Paris time, the small hours belonging to the evening before.
        // 16 Nov 2026 is a Monday.
        WeekMoment MomentAt(int d, int h, int mi = 0) => WeekMoments.At(AppTime.AtWallClock(new DateTime(2026, 11, d, h, mi, 0)));
        foreach (var (d, h, mi, expected) in new[]
        {
            (16, 3, 0, WeekMoment.MondayMorning), (16, 11, 59, WeekMoment.MondayMorning), (16, 12, 0, WeekMoment.None),
            (16, 2, 59, WeekMoment.SundayEvening), (18, 10, 0, WeekMoment.None), (18, 21, 0, WeekMoment.None),
            (20, 17, 59, WeekMoment.None), (20, 18, 0, WeekMoment.FridayEvening), (21, 2, 59, WeekMoment.FridayEvening),
            (21, 3, 0, WeekMoment.WeekendDay), (21, 20, 0, WeekMoment.SaturdayEvening), (22, 1, 0, WeekMoment.SaturdayEvening),
            (22, 12, 0, WeekMoment.WeekendDay), (22, 18, 0, WeekMoment.SundayEvening), (23, 2, 59, WeekMoment.SundayEvening),
            (23, 3, 0, WeekMoment.MondayMorning),
        })
            if (MomentAt(d, h, mi) != expected) Fail($"week moments: {d}/11 {h}:{mi:00} is {MomentAt(d, h, mi)}, expected {expected}");
        // Across the October DST change too (25 Oct 2026, a Sunday).
        if (WeekMoments.At(AppTime.AtWallClock(new DateTime(2026, 10, 25, 19, 0, 0))) != WeekMoment.SundayEvening)
            Fail("week moments: Sunday evening on the DST night");

        // Every moment has its four pools, and each greets like the half of the day it sits in.
        var weekPools = new[] { "WeekGreetingLines", "WeekGoodbyeLines", "OwnerWeekGreetingLines", "OwnerWeekGoodbyeLines" };
        foreach (var field in weekPools)
        {
            var pools = (IReadOnlyDictionary<WeekMoment, string[]>)Field(field);
            foreach (var moment in Enum.GetValues<WeekMoment>().Where(m => m != WeekMoment.None))
            {
                if (!pools.TryGetValue(moment, out var lines) || lines.Length == 0) { Fail($"{field} misses {moment}"); continue; }
                var evening = moment is WeekMoment.FridayEvening or WeekMoment.SaturdayEvening or WeekMoment.SundayEvening;
                foreach (var l in lines.Select(l => l.ToLowerInvariant()))
                    if (evening ? l.Contains("bonjour") || l.Contains("journée") : l.Contains("bonsoir") || l.Contains("soir"))
                        Fail($"{field}[{moment}]: greets for the wrong half: {l}");
            }
        }

        // The roll decides: under the chance a Friday-evening goodbye or greeting is the moment's, over it the clock's.
        var friday = AppTime.AtWallClock(new DateTime(2026, 11, 20, 19, 0, 0));
        var greet = br.GetMethod("GreetingLines")!;
        var weekGreets = (IReadOnlyDictionary<WeekMoment, string[]>)Field("WeekGreetingLines");
        if (greet.Invoke(null, [false, friday, 0.0]) != weekGreets[WeekMoment.FridayEvening]
            || !((string[])greet.Invoke(null, [false, friday, 0.99])!).SequenceEqual(((TimedPool<string>)Field("Greetings")).At(friday)))
            Fail("week moments: the greeting roll");
        var weekByes = (IReadOnlyDictionary<WeekMoment, string[]>)Field("WeekGoodbyeLines");
        if (clock.Invoke(null, [Enum.Parse(intentType, "Goodbye"), false, friday, 0.0]) != weekByes[WeekMoment.FridayEvening]
            || clock.Invoke(null, [Enum.Parse(intentType, "Goodbye"), false, friday, 0.99]) == weekByes[WeekMoment.FridayEvening])
            Fail("week moments: the goodbye roll");

        // Every TimedPool on BotResponses: day lines say bonjour (or « journée »), evening lines
        // bonsoir (or « soirée »), never the other one; any-time lines greet with neither, or both
        // as a joke. « dire bonjour » is saying hello, at any hour. Pairs are read as one text, and
        // stretched letters fold back (« Bonjouuur »).
        var stretched = new System.Text.RegularExpressions.Regex(@"(\p{L})\1{2,}");
        string Text(object line) => stretched.Replace(line is System.Runtime.CompilerServices.ITuple t
            ? string.Join(" | ", Enumerable.Range(0, t.Length).Select(i => t[i]))
            : (string)line, "$1");
        var saysHello = new System.Text.RegularExpressions.Regex(@"\b(dire|dis|dit) bonjour\b");
        var timedFields = br.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(TimedPool<>)).ToList();
        if (timedFields.Count < 7) Fail($"TimedPool: only {timedFields.Count} pools found");
        foreach (var f in timedFields)
        {
            var pool = f.GetValue(null)!;
            string[] Part(string name) => ((IEnumerable)f.FieldType.GetProperty(name)!.GetValue(pool)!).Cast<object>().Select(Text).ToArray();
            var (any, day, eve) = (Part("AnyTime"), Part("Day"), Part("Evening"));
            if (day.Length == 0 || eve.Length == 0) Fail($"{f.Name}: a time-aware pool with an empty half");
            foreach (var l in day.Select(l => l.ToLowerInvariant()))
                if (!(l.Contains("bonjour") || l.Contains("journée")) || l.Contains("bonsoir") || l.Contains("soirée"))
                    Fail($"{f.Name}.Day: not a daytime line: {l}");
            foreach (var l in eve.Select(l => l.ToLowerInvariant()))
                if (!(l.Contains("bonsoir") || l.Contains("soirée")) || l.Contains("bonjour") || l.Contains("journée"))
                    Fail($"{f.Name}.Evening: not an evening line: {l}");
            foreach (var l in any.Select(l => l.ToLowerInvariant()))
            {
                var jour = saysHello.Replace(l, "").Contains("bonjour");
                if (jour != l.Contains("bonsoir") || l.Contains("bonne journée") || l.Contains("bonne soirée"))
                    Fail($"{f.Name}.AnyTime: greets for one time of day: {l}");
            }
        }
    }
}
