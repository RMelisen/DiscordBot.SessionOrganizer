using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads /remindme's « quand » — pure, so every form can be checked without a gateway. Two shapes:
/// <list type="bullet">
/// <item>relative: « 10m », « 2h », « 1h30 », « 3j », « 1 semaine », « dans 2 heures et 5 minutes »;</item>
/// <item>absolute, on Paris wall clock, read by <see cref="FrenchDate"/>: « 21:30 », « 21h »,
/// « demain 9h », « ajd 18h », « samedi 14h », « le 25 à 10h », « 25/12 10:00 », « ce soir 9h »
/// (21:00)… A time already past today means tomorrow; a day without a time means 9:00, « ce soir »
/// alone 20:00.</item>
/// </list>
/// Returns the instant (UTC), or null when nothing parses. The range (<see cref="Min"/>,
/// <see cref="Max"/>) is the caller's to check, so it can say which way the request missed.
/// </summary>
public static partial class ReminderTime
{
    public static readonly TimeSpan Min = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Max = TimeSpan.FromDays(60);

    private const int DefaultHour = 9;
    // « ce soir » without a time.
    private const int EveningHour = 20;

    [GeneratedRegex(@"^(?:dans\s+)?(?<parts>(?:\d+\s*[a-z]*\s*(?:et\s+)?)+)$")]
    private static partial Regex RelativeRegex();

    [GeneratedRegex(@"(?<n>\d+)\s*(?<unit>[a-z]*)")]
    private static partial Regex RelativePartRegex();

    /// <remarks>
    /// « 21h » and « 1h30 » alone are clock times, the way French says « à 21h »; a duration in
    /// hours needs « dans » (« dans 1h30 ») or a spelled unit (« 2 heures »). Minutes, days and
    /// weeks can only be durations, so « 10m » needs nothing. The confirmation shows the exact
    /// time either way.
    /// </remarks>
    public static DateTimeOffset? Parse(string input, DateTimeOffset nowUtc)
    {
        var text = FrenchDate.Clean(input);
        if (text.Length == 0) return null;
        // « dans 3 jours à 21h » isn't a duration, but it is a day.
        if (text.StartsWith("dans ")) return ParseCleanRelative(text, nowUtc) ?? FrenchDate.ParseClean(text, nowUtc, DefaultHour, EveningHour);
        return FrenchDate.ParseClean(text, nowUtc, DefaultHour, EveningHour) ?? ParseCleanRelative(text, nowUtc);
    }

    /// <summary>
    /// The durations alone (« dans 2h », « 10m »): what a session reads « dans 1h » with, since its
    /// own date has no durations.
    /// </summary>
    public static DateTimeOffset? ParseRelative(string input, DateTimeOffset nowUtc)
    {
        var text = FrenchDate.Clean(input);
        return text.Length == 0 ? null : ParseCleanRelative(text, nowUtc);
    }

    private static DateTimeOffset? ParseCleanRelative(string text, DateTimeOffset nowUtc)
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
                // « 1h30 »: a bare number right after hours is minutes, and minutes of an hour stop
                // at 59. « 21h75 » is a clock time with bad minutes, not 21 hours and 75 minutes.
                "" when lastUnit == TimeSpan.FromHours(1) && n < 60 => TimeSpan.FromMinutes(1),
                _ => null,
            };
            if (step is null) return null;
            total += step.Value * n;
            lastUnit = step;
        }
        return total > TimeSpan.Zero ? nowUtc + total : null;
    }
}
