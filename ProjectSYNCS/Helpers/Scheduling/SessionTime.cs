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
