using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads a session's « quand » and « durée », typed in French — pure, so the checks can pin every
/// form. Dates go through <see cref="ReminderTime"/>'s absolute parser with session defaults: a day
/// without a time means 21:00 (« ce soir » alone too), only the future counts, and no further than
/// <see cref="Horizon"/>.
/// </summary>
public static partial class SessionTime
{
    public const int DefaultHour = 21;
    public static readonly TimeSpan Horizon = TimeSpan.FromDays(90);

    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(6);

    /// <summary>The most slots a date poll holds (two rows of five buttons).</summary>
    public const int MaxSlots = 10;

    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>The instant (UTC), or null when it doesn't parse, is past, or is too far ahead.</summary>
    public static DateTimeOffset? Parse(string text, DateTimeOffset nowUtc)
    {
        var at = ReminderTime.ParseAbsolute(text, nowUtc, DefaultHour, DefaultHour);
        if (at is not { } t || t <= nowUtc || t - nowUtc > Horizon) return null;
        return t;
    }

    /// <summary>
    /// « ven 21h, sam 21h, dim 18h »: comma, semicolon or line separated. The slots come back sorted
    /// and without duplicates, at most <see cref="MaxSlots"/>; every piece that didn't read as a
    /// future date is named back, so the reply can say which.
    /// </summary>
    public static (List<DateTimeOffset> Slots, List<string> Rejected) ParseList(string text, DateTimeOffset nowUtc)
    {
        var slots = new SortedSet<DateTimeOffset>();
        var rejected = new List<string>();
        foreach (var piece in text.Split(new[] { ',', ';', '\n' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (Parse(piece, nowUtc) is { } slot) slots.Add(slot);
            else rejected.Add(piece);
        }
        return (slots.Take(MaxSlots).ToList(), rejected);
    }

    /// <summary>« sam. 1 nov. à 21:00 », in Paris time: what the autocomplete previews.</summary>
    public static string Preview(DateTimeOffset at)
    {
        var zoned = AppTime.ToZoned(at);
        return $"{zoned.ToString("ddd d MMM", Fr)} à {zoned:HH:mm}";
    }

    /// <summary>The most one-click times the session picker offers: a row of five, « Autre… » last.</summary>
    public const int MaxQuickSlots = 4;

    /// <summary>
    /// The session picker's one-click times, at <paramref name="minuteOfDay"/> Paris time: today
    /// while it's still ahead, tomorrow, then the coming Saturday and Sunday. Each day once, in order,
    /// at most <see cref="MaxQuickSlots"/>. Labels: « Ce soir 21h », « Demain 21h », « Samedi 21h ».
    /// </summary>
    public static List<(string Label, DateTimeOffset At)> QuickSlots(DateTimeOffset nowUtc, int minuteOfDay)
    {
        var today = AppTime.ToZoned(nowUtc).Date;
        DateTimeOffset At(DateTime day) => AppTime.AtWallClock(day.AddMinutes(minuteOfDay)).ToUniversalTime();

        var days = new SortedSet<DateTime> { today, today.AddDays(1) };
        // The coming Saturday and Sunday: a week on when today is that day and the time is past.
        foreach (var weekend in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            var day = today.AddDays(((int)weekend - (int)today.DayOfWeek + 7) % 7);
            days.Add(At(day) > nowUtc ? day : day.AddDays(7));
        }

        var time = HourLabel(minuteOfDay);
        var slots = new List<(string, DateTimeOffset)>();
        foreach (var day in days)
        {
            var at = At(day);
            if (at <= nowUtc) continue;
            var name = day == today ? (minuteOfDay >= 18 * 60 ? "Ce soir" : "Aujourd'hui")
                : day == today.AddDays(1) ? "Demain"
                : Fr.TextInfo.ToTitleCase(Recurrence.DayName(day.DayOfWeek));
            slots.Add(($"{name} {time}", at));
            if (slots.Count == MaxQuickSlots) break;
        }
        return slots;
    }

    /// <summary>« 21h », « 20h30 ».</summary>
    public static string HourLabel(int minuteOfDay) =>
        minuteOfDay % 60 == 0 ? $"{minuteOfDay / 60}h" : $"{minuteOfDay / 60}h{minuteOfDay % 60:D2}";

    /// <summary>Whether two sessions share any minute (one ending as the other starts doesn't count).</summary>
    public static bool Overlaps(DateTimeOffset aStart, TimeSpan aLength, DateTimeOffset bStart, TimeSpan bLength) =>
        aStart < bStart + bLength && bStart < aStart + aLength;

    [GeneratedRegex(@"^(?<h>\d{1,2})\s*h\s*(?<m>\d{1,2})?\s*(?:min|mn|m)?$")]
    private static partial Regex HoursRegex();

    [GeneratedRegex(@"^(?<m>\d{1,3})\s*(?:min|mn|m|minutes?)?$")]
    private static partial Regex MinutesRegex();

    /// <summary>
    /// « 2h », « 1h30 », « 90 », « 45 min », in minutes; null outside
    /// <see cref="MinDuration"/>–<see cref="MaxDuration"/> or when it doesn't parse. A bare number
    /// is minutes.
    /// </summary>
    public static int? ParseDuration(string text)
    {
        var clean = text.Trim().ToLowerInvariant();
        int minutes;
        if (HoursRegex().Match(clean) is { Success: true } h)
        {
            minutes = int.Parse(h.Groups["h"].Value) * 60;
            if (h.Groups["m"].Success)
            {
                var m = int.Parse(h.Groups["m"].Value);
                if (m > 59) return null;
                minutes += m;
            }
        }
        else if (MinutesRegex().Match(clean) is { Success: true } mm)
        {
            minutes = int.Parse(mm.Groups["m"].Value);
        }
        else return null;

        return minutes >= MinDuration.TotalMinutes && minutes <= MaxDuration.TotalMinutes ? minutes : null;
    }

    /// <summary>« 2 h », « 1 h 30 », « 45 min ».</summary>
    public static string DurationLabel(int minutes)
    {
        var h = minutes / 60;
        var m = minutes % 60;
        return h == 0 ? $"{m} min" : m == 0 ? $"{h} h" : $"{h} h {m:D2}";
    }

    /// <summary>The same, in the edit modal's own field: « 2h », « 1h30 ».</summary>
    public static string DurationInput(int minutes)
    {
        var h = minutes / 60;
        var m = minutes % 60;
        return h == 0 ? $"{m}" : m == 0 ? $"{h}h" : $"{h}h{m:D2}";
    }
}
