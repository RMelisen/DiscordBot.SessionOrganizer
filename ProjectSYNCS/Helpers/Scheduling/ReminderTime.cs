using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads /remindme's « quand » — pure, so every form can be checked without a gateway. Two shapes:
/// <list type="bullet">
/// <item>relative: « 10m », « 2h », « 1h30 », « 3j », « 1 semaine », « dans 2 heures et 5 minutes »;</item>
/// <item>absolute, on Paris wall clock: « 21:30 », « 21h », « demain 9h », « après-demain 18h »,
/// « samedi 14h », « 25/12 10:00 », « 25/12/2027 8h ». A time already past today means tomorrow; a
/// day without a time means 9:00.</item>
/// </list>
/// Returns the instant (UTC), or null when nothing parses. The range (<see cref="Min"/>,
/// <see cref="Max"/>) is the caller's to check, so it can say which way the request missed.
/// </summary>
public static partial class ReminderTime
{
    public static readonly TimeSpan Min = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Max = TimeSpan.FromDays(60);

    private const int DefaultHour = 9;

    private static readonly string[] Weekdays =
        { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };

    [GeneratedRegex(@"^(?:dans\s+)?(?<parts>(?:\d+\s*[a-z]*\s*(?:et\s+)?)+)$")]
    private static partial Regex RelativeRegex();

    [GeneratedRegex(@"(?<n>\d+)\s*(?<unit>[a-z]*)")]
    private static partial Regex RelativePartRegex();

    [GeneratedRegex(@"^(?:le\s+)?(?<day>demain|apres demain|aujourd hui|ce soir|ce matin|lundi|mardi|mercredi|jeudi|vendredi|samedi|dimanche|\d{1,2}/\d{1,2}(?:/\d{2,4})?)?\s*(?:a\s+)?(?<time>\d{1,2}\s*(?:h|:)\s*\d{0,2})?$")]
    private static partial Regex AbsoluteRegex();

    /// <remarks>
    /// « 21h » and « 1h30 » alone are clock times, the way French says « à 21h »; a duration in
    /// hours needs « dans » (« dans 1h30 ») or a spelled unit (« 2 heures »). Minutes, days and
    /// weeks can only be durations, so « 10m » needs nothing. The confirmation shows the exact
    /// time either way.
    /// </remarks>
    public static DateTimeOffset? Parse(string input, DateTimeOffset nowUtc)
    {
        var text = Clean(input);
        if (text.Length == 0) return null;
        if (text.StartsWith("dans ")) return ParseRelative(text, nowUtc);
        return ParseAbsolute(text, nowUtc) ?? ParseRelative(text, nowUtc);
    }

    // Lower case, accents off (« après » = « apres »), every separator but ':' and '/' to a
    // space, spaces collapsed.
    private static string Clean(string input)
    {
        var decomposed = input.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) ==
                System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) || c is ':' or '/' ? c : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static DateTimeOffset? ParseRelative(string text, DateTimeOffset nowUtc)
    {
        var m = RelativeRegex().Match(text);
        if (!m.Success) return null;

        var total = TimeSpan.Zero;
        TimeSpan? lastUnit = null;
        foreach (Match part in RelativePartRegex().Matches(m.Groups["parts"].Value))
        {
            if (!int.TryParse(part.Groups["n"].Value, out var n) || n > 100_000) return null;
            var unit = part.Groups["unit"].Value;
            TimeSpan? step = unit switch
            {
                "m" or "mn" or "min" or "mins" or "minute" or "minutes" => TimeSpan.FromMinutes(1),
                "h" or "heure" or "heures" => TimeSpan.FromHours(1),
                "j" or "jour" or "jours" or "d" => TimeSpan.FromDays(1),
                "s" or "sem" or "semaine" or "semaines" => TimeSpan.FromDays(7),
                // « 1h30 »: a bare number right after hours is minutes.
                "" when lastUnit == TimeSpan.FromHours(1) => TimeSpan.FromMinutes(1),
                _ => null,
            };
            if (step is null) return null;
            total += step.Value * n;
            lastUnit = step;
        }
        return total > TimeSpan.Zero ? nowUtc + total : null;
    }

    private static DateTimeOffset? ParseAbsolute(string text, DateTimeOffset nowUtc)
    {
        var m = AbsoluteRegex().Match(text);
        if (!m.Success) return null;
        var dayPart = m.Groups["day"].Value;
        var timePart = m.Groups["time"].Value;
        if (dayPart.Length == 0 && timePart.Length == 0) return null;

        var now = AppTime.ToZoned(nowUtc).DateTime;
        int hour = DefaultHour, minute = 0;
        if (timePart.Length > 0)
        {
            var bits = timePart.Replace(" ", "").Split('h', ':');
            if (!int.TryParse(bits[0], out hour)) return null;
            if (bits.Length > 1 && bits[1].Length > 0 && !int.TryParse(bits[1], out minute)) return null;
            if (hour > 23 || minute > 59) return null;
        }

        DateTime date;
        switch (dayPart)
        {
            case "":
            case "aujourd hui":
            case "ce soir":
            case "ce matin":
                date = now.Date;
                // A bare time already past means tomorrow; « aujourd'hui 8h » at noon is just past.
                if (dayPart.Length == 0 && date.AddHours(hour).AddMinutes(minute) <= now) date = date.AddDays(1);
                break;
            case "demain":
                date = now.Date.AddDays(1);
                break;
            case "apres demain":
                date = now.Date.AddDays(2);
                break;
            default:
                var weekday = Array.IndexOf(Weekdays, dayPart);
                if (weekday >= 0)
                {
                    // The next such day; today's name means next week only once its hour is past.
                    var ahead = ((weekday - (int)now.DayOfWeek) + 7) % 7;
                    date = now.Date.AddDays(ahead);
                    if (date.AddHours(hour).AddMinutes(minute) <= now) date = date.AddDays(7);
                    break;
                }
                if (!TryParseDate(dayPart, now, out date)) return null;
                break;
        }

        var wall = date.AddHours(hour).AddMinutes(minute);
        return AppTime.AtWallClock(wall).ToUniversalTime();
    }

    // « 25/12 » (this year, or next once it's past) and « 25/12/27 » / « 25/12/2027 ».
    private static bool TryParseDate(string text, DateTime now, out DateTime date)
    {
        date = default;
        var bits = text.Split('/');
        if (!int.TryParse(bits[0], out var day) || !int.TryParse(bits[1], out var month)) return false;
        var year = now.Year;
        var yearGiven = bits.Length > 2 && int.TryParse(bits[2], out year);
        if (yearGiven && year < 100) year += 2000;
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        date = new DateTime(year, month, day);
        if (!yearGiven && date < now.Date)
        {
            if (day > DateTime.DaysInMonth(year + 1, month)) return false;
            date = new DateTime(year + 1, month, day);
        }
        return true;
    }
}
