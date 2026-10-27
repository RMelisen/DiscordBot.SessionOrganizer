using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads a day and a clock time typed in French, the way people write them in chat — pure, so the
/// checks can pin every form. Every typed « quand » goes through it: sessions, date poll slots and
/// /remindme's clock times. The words come in any order (« 20h ajd », « ajd à 20h »):
/// <list type="bullet">
/// <item>days: « aujourd'hui » (« ajd », « auj »), « demain » (« dem », « 2main »), « après-demain »,
/// weekdays (« samedi », « sam », « samedi prochain », « samedi en huit », « samedi de la semaine
/// prochaine »), « ce week-end », « dans 3 jours », « le 25 », « 25 décembre », « 1er nov »,
/// « 25/12 », « 25-12-2027 »;</item>
/// <item>times: « 21h », « 21h30 », « 21:30 », « 21 h 30 », « 9 heures », « 9pm », « midi »,
/// « minuit », « 21h et demie », « 21h moins le quart »; a bare number after a day is an hour
/// (« demain 21 »);</item>
/// <item>parts of the day: « ce matin », « cet aprem », « ce soir », « cette nuit », « 9h du soir »:
/// alone they set an hour, with a time they read it on twelve hours (« demain soir 9h » is 21:00).</item>
/// </list>
/// A word it doesn't know makes it give up rather than guess. Returns the instant (UTC) or null;
/// whether it is past or too far is the caller's to judge.
/// </summary>
public static partial class FrenchDate
{
    /// <summary>What the replies quote when a date didn't read.</summary>
    public const string Examples = "« samedi 21h », « demain soir », « ajd 20h30 », « le 25 à 21h », « 25 déc 20h »";

    private const int MorningHour = 9;
    private const int AfternoonHour = 15;
    private const int NightHour = 23;

    private enum Part { Morning, Afternoon, Evening, Night }

    private static readonly Dictionary<string, int> DayOffsets = new()
    {
        ["aujourdhui"] = 0, ["auj"] = 0, ["ajd"] = 0, ["aujd"] = 0, ["ajrd"] = 0, ["aujrd"] = 0, ["ojd"] = 0, ["today"] = 0,
        ["demain"] = 1, ["dem"] = 1, ["dmain"] = 1, ["dmn"] = 1, ["2main"] = 1, ["tomorrow"] = 1, ["tmrw"] = 1,
        ["apresdemain"] = 2,
    };

    private static readonly Dictionary<string, DayOfWeek> Weekdays = new()
    {
        ["lundi"] = DayOfWeek.Monday, ["lun"] = DayOfWeek.Monday,
        ["mardi"] = DayOfWeek.Tuesday, ["mar"] = DayOfWeek.Tuesday,
        ["mercredi"] = DayOfWeek.Wednesday, ["mer"] = DayOfWeek.Wednesday, ["merc"] = DayOfWeek.Wednesday,
        ["jeudi"] = DayOfWeek.Thursday, ["jeu"] = DayOfWeek.Thursday,
        ["vendredi"] = DayOfWeek.Friday, ["ven"] = DayOfWeek.Friday, ["vend"] = DayOfWeek.Friday,
        ["samedi"] = DayOfWeek.Saturday, ["sam"] = DayOfWeek.Saturday,
        ["dimanche"] = DayOfWeek.Sunday, ["dim"] = DayOfWeek.Sunday,
        // « ce week-end », « ce we »: its Saturday.
        ["weekend"] = DayOfWeek.Saturday, ["we"] = DayOfWeek.Saturday,
    };

    // Only right after a day number (« 25 mar »), so « mar » alone stays Tuesday.
    private static readonly Dictionary<string, int> Months = new()
    {
        ["janvier"] = 1, ["janv"] = 1, ["jan"] = 1,
        ["fevrier"] = 2, ["fevr"] = 2, ["fev"] = 2,
        ["mars"] = 3, ["mar"] = 3,
        ["avril"] = 4, ["avr"] = 4,
        ["mai"] = 5,
        ["juin"] = 6,
        ["juillet"] = 7, ["juil"] = 7,
        ["aout"] = 8,
        ["septembre"] = 9, ["sept"] = 9, ["sep"] = 9,
        ["octobre"] = 10, ["oct"] = 10,
        ["novembre"] = 11, ["nov"] = 11,
        ["decembre"] = 12, ["dec"] = 12,
    };

    private static readonly Dictionary<string, Part> Parts = new()
    {
        ["matin"] = Part.Morning, ["matinee"] = Part.Morning,
        ["aprem"] = Part.Afternoon, ["aprm"] = Part.Afternoon, ["apresmidi"] = Part.Afternoon,
        ["soir"] = Part.Evening, ["soiree"] = Part.Evening, ["tonight"] = Part.Evening, ["tonite"] = Part.Evening,
        ["nuit"] = Part.Night,
    };

    // Words that carry nothing: « le samedi à 21h », « vers 21h », « à partir de 20h », « 9h du soir ».
    private static readonly HashSet<string> Fillers = new()
    {
        "le", "la", "l", "a", "au", "vers", "pour", "on", "des", "partir", "de", "du", "d", "environ", "genre",
        "pile", "ce", "cet", "cette", "en",
    };

    // The words that make a number before them an hour: « 21 h », « 9 heures », « 9 pm ».
    private static readonly HashSet<string> HourWords = new() { "h", "hr", "heure", "heures", "am", "pm" };

    [GeneratedRegex(@"^(?<h>\d{1,2})(?:h|:)(?<m>\d{0,2})$")]
    private static partial Regex ClockRegex();

    [GeneratedRegex(@"^(?<h>\d{1,2})(?:(?:h|:)(?<m>\d{2}))?(?<ampm>am|pm)$")]
    private static partial Regex AmPmRegex();

    [GeneratedRegex(@"^(?<n>\d+)(?<unit>j|jours?|sem|semaines?)$")]
    private static partial Regex GluedDaysRegex();

    // « 25.12 », « 25-12-2027 »: the separators a date can be typed with, before Clean turns them to spaces.
    [GeneratedRegex(@"(?<=\d)[.\-](?=\d)")]
    private static partial Regex DateSeparatorRegex();

    // « demain21h », « ven21h »: a word glued to a number. Two letters, so « 21h30 » stays whole.
    [GeneratedRegex(@"(?<=[a-z]{2})(?=\d)")]
    private static partial Regex WordThenNumberRegex();

    /// <summary>
    /// Lower case, accents off (« après » = « apres »), every separator but ':' and '/' to a space,
    /// « 25-12 » to « 25/12 », a word glued to a number split off, spaces collapsed.
    /// </summary>
    public static string Clean(string input)
    {
        var decomposed = DateSeparatorRegex().Replace(input.ToLowerInvariant(), "/").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) || c is ':' or '/' ? c : ' ');
        }
        var spaced = WordThenNumberRegex().Replace(sb.ToString(), " ");
        return string.Join(' ', spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// The instant, with the caller's hour for a day typed without a time
    /// (<paramref name="defaultHour"/>) and for « ce soir » alone (<paramref name="eveningHour"/>).
    /// </summary>
    public static DateTimeOffset? Parse(string input, DateTimeOffset nowUtc, int defaultHour, int eveningHour)
    {
        var text = Clean(input);
        return text.Length == 0 ? null : ParseClean(text, nowUtc, defaultHour, eveningHour);
    }

    /// <summary>The same, on text already through <see cref="Clean"/>.</summary>
    public static DateTimeOffset? ParseClean(string text, DateTimeOffset nowUtc, int defaultHour, int eveningHour)
    {
        var r = new Reading();
        if (!r.Read(JoinPhrases(text.Split(' ')))) return null;
        return r.Resolve(AppTime.ToZoned(nowUtc).DateTime, defaultHour, eveningHour);
    }

    // The two-word phrases, made one token: « aujourd hui », « apres demain », « apres midi », « week end ».
    private static string[] JoinPhrases(string[] words)
    {
        var tokens = new List<string>(words.Length);
        for (var i = 0; i < words.Length; i++)
        {
            var pair = i + 1 < words.Length ? words[i] + words[i + 1] : null;
            if (pair is "aujourdhui" or "apresdemain" or "apresmidi" or "weekend")
            {
                tokens.Add(pair);
                i++;
            }
            else if (pair is "apresdem")
            {
                tokens.Add("apresdemain");
                i++;
            }
            else tokens.Add(words[i]);
        }
        return tokens.ToArray();
    }

    private static bool IsDayUnit(string unit) => unit is "j" or "jour" or "jours" or "sem" or "semaine" or "semaines";

    private sealed class Reading
    {
        private int? _offset;                           // days from today: « ajd » 0, « demain » 1, « dans 3 jours » 3
        private (int Day, int Month, int? Year)? _date; // « 25/12 », « 25 décembre »
        private int? _dayOfMonth;                       // « le 25 »: this month, or the next once past
        private DayOfWeek? _weekday;
        private bool _weekend;                          // the weekday is « ce week-end »'s Saturday
        private bool _nextOccurrence;                   // « samedi prochain »: never today
        private bool _nextWeek;                         // « samedi de la semaine prochaine »: next Monday-to-Sunday week
        private bool _inEight;                          // « samedi en huit »: a week after the coming one
        private Part? _part;
        private int? _hour, _minute;
        private bool? _pm;
        private bool _spelledHours;                     // « 9 heures »: alone, /remindme's duration
        private bool _midnight;

        private bool HasDay => _offset is not null || _date is not null || _dayOfMonth is not null || _weekday is not null;

        public bool Read(string[] t)
        {
            for (var i = 0; i < t.Length; i++)
            {
                var w = t[i];
                var prev = i > 0 ? t[i - 1] : null;
                var next = i + 1 < t.Length ? t[i + 1] : null;

                if (w is "prochain" or "prochaine")
                {
                    if (prev is "semaine") _nextWeek = true;
                    else _nextOccurrence = true;
                    continue;
                }
                if (w is "semaine" && next is "prochaine") continue;
                if (w is "en" && next is "huit")
                {
                    _inEight = true;
                    i++;
                    continue;
                }
                if (w is "et" && next is "demi" or "demie" or "quart")
                {
                    if (!Shift(next == "quart" ? 15 : 30)) return false;
                    i++;
                    continue;
                }
                if (w is "moins")
                {
                    // « moins le quart », « moins quart », « moins 10 ».
                    var skip = next is "le" ? 2 : 1;
                    var what = i + skip < t.Length ? t[i + skip] : null;
                    int back;
                    if (what is "quart") back = 15;
                    else if (skip == 1 && int.TryParse(what, out var m) && m is > 0 and < 60) back = m;
                    else return false;
                    if (!Shift(-back)) return false;
                    i += skip;
                    continue;
                }
                if (Fillers.Contains(w) || w is "et") continue;

                if (DayOffsets.TryGetValue(w, out var offset))
                {
                    if (!SetOffset(offset)) return false;
                    continue;
                }
                if (Weekdays.TryGetValue(w, out var weekday))
                {
                    var isWeekend = w is "weekend" or "we";
                    // « ce week-end, dimanche »: the named day wins over the weekend's Saturday.
                    if (isWeekend && _weekday is not null) continue;
                    if (_weekday is { } had && had != weekday && !_weekend) return false;
                    _weekday = weekday;
                    _weekend = isWeekend;
                    continue;
                }
                if (Parts.TryGetValue(w, out var part))
                {
                    if (_part is { } had && had != part) return false;
                    _part = part;
                    continue;
                }
                if (w is "midi" or "minuit")
                {
                    if (!SetTime(w == "midi" ? 12 : 0, 0)) return false;
                    _midnight = w == "minuit";
                    continue;
                }
                if (w is "dans")
                {
                    // « dans 3 jours », « dans 2 semaines », « dans 3j »: a day. « dans 2h » isn't one.
                    int days;
                    if (next is not null && GluedDaysRegex().Match(next) is { Success: true } glued)
                    {
                        days = DaysIn(int.Parse(glued.Groups["n"].Value), glued.Groups["unit"].Value);
                        i++;
                    }
                    else if (int.TryParse(next, out var n) && i + 2 < t.Length && IsDayUnit(t[i + 2]))
                    {
                        days = DaysIn(n, t[i + 2]);
                        i += 2;
                    }
                    else return false;
                    if (days is <= 0 or > 400 || !SetOffset(days)) return false;
                    continue;
                }
                if (w.Contains('/'))
                {
                    if (!TrySlashDate(w, out var date) || !SetDate(date.Day, date.Month, date.Year)) return false;
                    continue;
                }
                if (ClockRegex().Match(w) is { Success: true } clock)
                {
                    var minutes = clock.Groups["m"].Value;
                    if (!SetTime(int.Parse(clock.Groups["h"].Value), minutes.Length > 0 ? int.Parse(minutes) : null)) return false;
                    // « 1h 30 »: the minutes in their own word.
                    if (minutes.Length == 0 && TakeMinutes(t, ref i) is false) return false;
                    continue;
                }
                if (AmPmRegex().Match(w) is { Success: true } ampm)
                {
                    var minutes = ampm.Groups["m"].Value;
                    if (!SetTime(int.Parse(ampm.Groups["h"].Value), minutes.Length > 0 ? int.Parse(minutes) : null)) return false;
                    _pm = ampm.Groups["ampm"].Value == "pm";
                    continue;
                }
                if (w is "1er" or "1ere")
                {
                    if (!DayNumber(t, ref i, 1)) return false;
                    continue;
                }
                if (w.Length <= 2 && int.TryParse(w, out var number))
                {
                    if (next is not null && HourWords.Contains(next))
                    {
                        // « 21 h 30 », « 9 heures », « 9 pm ».
                        if (!SetTime(number, null)) return false;
                        i++;
                        if (next is "am" or "pm") _pm = next == "pm";
                        else
                        {
                            _spelledHours = next is "heure" or "heures";
                            if (TakeMinutes(t, ref i) is false) return false;
                        }
                        continue;
                    }
                    if ((next is not null && Months.ContainsKey(next)) || prev is "le")
                    {
                        if (!DayNumber(t, ref i, number)) return false;
                        continue;
                    }
                    // « demain 21 »: after a day, a lone number is the hour.
                    if ((HasDay || _part is not null) && _hour is null && number <= 23)
                    {
                        _hour = number;
                        continue;
                    }
                    return false;
                }
                if (w is "am" or "pm" && _hour is not null)
                {
                    _pm = w == "pm";
                    continue;
                }
                return false;
            }
            return HasDay || _part is not null || _hour is not null;
        }

        // The minutes after an hour, in their own word: « 1h 30 », « 21 h 30 ». Null when there are
        // none, false when they are out of range. « 21h 25 déc » is a date, not 21:25.
        private bool? TakeMinutes(string[] t, ref int i)
        {
            if (i + 1 >= t.Length || t[i + 1].Length != 2 || !int.TryParse(t[i + 1], out var minute)) return null;
            if (i + 2 < t.Length && Months.ContainsKey(t[i + 2])) return null;
            if (minute > 59) return false;
            _minute = minute;
            i++;
            return true;
        }

        // « le 25 », « 25 décembre », « 1er nov 2027 ».
        private bool DayNumber(string[] t, ref int i, int day)
        {
            if (day is < 1 or > 31) return false;
            if (i + 1 >= t.Length || !Months.TryGetValue(t[i + 1], out var month))
            {
                if (_dayOfMonth is not null || _date is not null || _offset is not null) return false;
                _dayOfMonth = day;
                return true;
            }
            i++;
            int? year = null;
            if (i + 1 < t.Length && t[i + 1].Length == 4 && int.TryParse(t[i + 1], out var y))
            {
                year = y;
                i++;
            }
            return SetDate(day, month, year);
        }

        private static bool TrySlashDate(string text, out (int Day, int Month, int? Year) date)
        {
            date = default;
            var bits = text.Split('/');
            if (bits.Length is < 2 or > 3 || !int.TryParse(bits[0], out var day) || !int.TryParse(bits[1], out var month)) return false;
            int? year = null;
            if (bits.Length == 3)
            {
                if (!int.TryParse(bits[2], out var y)) return false;
                year = y < 100 ? y + 2000 : y;
            }
            date = (day, month, year);
            return true;
        }

        private bool SetDate(int day, int month, int? year)
        {
            if (_date is not null || _offset is not null || _dayOfMonth is not null) return false;
            _date = (day, month, year);
            return true;
        }

        private bool SetOffset(int days)
        {
            if (_offset is { } had) return had == days;
            if (_date is not null || _dayOfMonth is not null) return false;
            _offset = days;
            return true;
        }

        // One time only: « 20h 21h » is a typo, not a choice.
        private bool SetTime(int hour, int? minute)
        {
            if (_hour is not null || hour > 23 || minute > 59) return false;
            _hour = hour;
            _minute = minute;
            return true;
        }

        // « et demie », « moins le quart »: only on a whole hour. « minuit moins le quart » is tonight.
        private bool Shift(int minutes)
        {
            if (_hour is not { } h || (_minute ?? 0) != 0) return false;
            var total = h * 60 + minutes;
            if (total < 0)
            {
                total += 24 * 60;
                _midnight = false;
            }
            _hour = total / 60;
            _minute = total % 60;
            return true;
        }

        private static int DaysIn(int n, string unit) => unit.StartsWith('j') ? n : n * 7;

        public DateTimeOffset? Resolve(DateTime now, int defaultHour, int eveningHour)
        {
            int hour, minute;
            var nextDay = _midnight;
            if (_hour is { } h)
            {
                // « 9 heures » alone is how /remindme says a duration.
                if (_spelledHours && !HasDay && _part is null && _minute is null) return null;
                hour = h;
                minute = _minute ?? 0;
                if (_pm == true && hour < 12) hour += 12;
                else if (_pm == false && hour == 12) hour = 0;
                // « ce soir 9h », « demain aprem 3h »: French counts the evening on twelve hours too.
                else if (_pm is null && _part is Part.Afternoon or Part.Evening && hour is >= 1 and < 12) hour += 12;
                // « cette nuit 2h »: after midnight.
                else if (_pm is null && _part is Part.Night && hour < 12) nextDay = true;
            }
            else
            {
                hour = _part switch
                {
                    Part.Morning => MorningHour,
                    Part.Afternoon => AfternoonHour,
                    Part.Evening => eveningHour,
                    Part.Night => NightHour,
                    _ => defaultHour,
                };
                minute = 0;
            }
            var time = new TimeSpan(hour, minute, 0) + (nextDay ? TimeSpan.FromDays(1) : TimeSpan.Zero);

            DateTime date;
            if (_date is { } d)
            {
                if (!TryDate(d.Day, d.Month, d.Year, now, out date)) return null;
            }
            else if (_dayOfMonth is { } dom)
            {
                // This month, or the first month after it that has the day, once past.
                DateTime? found = null;
                var month = new DateTime(now.Year, now.Month, 1);
                for (var k = 0; k < 13 && found is null; k++, month = month.AddMonths(1))
                {
                    if (dom > DateTime.DaysInMonth(month.Year, month.Month)) continue;
                    var candidate = new DateTime(month.Year, month.Month, dom);
                    if (candidate + time > now) found = candidate;
                }
                if (found is not { } f) return null;
                date = f;
            }
            else if (_offset is { } off)
            {
                date = now.Date.AddDays(off);
            }
            else if (_weekday is { } wd)
            {
                if (_nextWeek)
                {
                    var monday = now.Date.AddDays(7 - ((int)now.DayOfWeek + 6) % 7);
                    date = monday.AddDays(((int)wd + 6) % 7);
                }
                else
                {
                    // The next such day; today's name means next week once its hour is past, or with « prochain ».
                    date = now.Date.AddDays(((int)wd - (int)now.DayOfWeek + 7) % 7);
                    if (date == now.Date && (_nextOccurrence || date + time <= now)) date = date.AddDays(7);
                }
                if (_inEight) date = date.AddDays(7);
            }
            else
            {
                date = now.Date;
                // A bare time already past means tomorrow; « ce soir » and « ajd 8h » at noon are just past.
                if (_part is null && date + time <= now) date = date.AddDays(1);
            }

            // « lundi 3 novembre », « demain samedi »: the weekday must agree with the day.
            if (_weekday is { } named && !_weekend && (_date is not null || _dayOfMonth is not null || _offset is not null)
                && date.DayOfWeek != named)
                return null;

            return AppTime.AtWallClock(date + time).ToUniversalTime();
        }

        // « 25/12 » (this year, or next once it's past) and « 25/12/27 » / « 25/12/2027 ».
        private static bool TryDate(int day, int month, int? givenYear, DateTime now, out DateTime date)
        {
            date = default;
            var year = givenYear ?? now.Year;
            if (year is < 2000 or > 2100 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
            date = new DateTime(year, month, day);
            if (givenYear is null && date < now.Date)
            {
                if (day > DateTime.DaysInMonth(year + 1, month)) return false;
                date = new DateTime(year + 1, month, day);
            }
            return true;
        }
    }
}
