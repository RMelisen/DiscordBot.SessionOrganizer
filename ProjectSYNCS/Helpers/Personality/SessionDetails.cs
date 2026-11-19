using System.Text.RegularExpressions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a session asked for in chat says besides its name and time (ChatRequest.ReadSession): its
/// places, its length, whether it's every week, and its kind. Each piece read is blanked out of the
/// text (same length, so positions hold), and ChatRequest reads the date and title from what's left:
/// « un lol samedi 21h pendant 3h à 5 joueurs » is « lol », samedi 21h, 3 h, 5 places.
/// Only said things count: a number is places only next to its noun or « max » (« à 5 » alone may be
/// an hour), a length only after its word or as a range, and a bare « pour 2h » only up to 6 hours
/// (« pour 21h » is a time).
/// </summary>
internal static partial class SessionDetails
{
    public sealed record Reading(string Masked, int? Places, int? Minutes, bool Weekly, SessionCategory? Category);

    private const int Timeout = 200;
    private const string Before = @"(?<![\p{L}\p{N}])";
    private const string After = @"(?![\p{L}\p{N}])";

    // « à 5 joueurs », « pour 6 personnes », « jusqu'à 8 joueurs », « 5 places max », « avec 6 personnes max ».
    [GeneratedRegex(Before + @"(?:(?:avec|pour|à|a|jusqu['’]\s*(?:à|a)|max(?:imum)?)\s+)?(?<n>\d{1,2})\s*(?:places?|joueurs?|joueuses?|personnes?|pers|participants?|participantes?|potes|slots?)(?:\s+max(?:imum)?)?" + After,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex PlacesNounRegex();

    // « max 6 », « 6 max ».
    [GeneratedRegex(Before + @"(?:max(?:imum)?\s*(?<n>\d{1,2})(?![\p{L}\p{N}:/.])|(?<n>\d{1,2})\s*max(?:imum)?" + After + ")",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex PlacesMaxRegex();

    // « … le 25/12 à 20:30 pour 4 »: a bare count closing the message.
    [GeneratedRegex(Before + @"pour\s+(?<n>\d{1,2})(?![\p{L}\p{N}:/.])[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex PlacesTrailingRegex();

    // « de 21h à 23h30 », « entre 20h et 22h »: the start stays for the date, the end goes.
    [GeneratedRegex(Before + @"(?:de|entre)\s+(?<a>\d{1,2})\s*[h:]\s*(?<am>\d{2})?\s*(?<end>(?:à|a|et|-|jusqu['’]\s*(?:à|a))\s*(?<b>\d{1,2})\s*[h:]\s*(?<bm>\d{2})?)" + After,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex RangeRegex();

    // « pendant 3h », « durée 1h30 », « de 90 minutes », « pour 2h », « sur 2 heures ».
    [GeneratedRegex(Before + @"(?<lead>pendant|durée(?:\s+de)?|duree(?:\s+de)?|pour|sur|de)\s+(?:(?<h>\d{1,2})\s*(?:h|heures?)\s*(?<m>\d{2})?|(?<min>\d{1,3})\s*(?:min|mn|minutes?))" + After,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex LengthRegex();

    // « chaque samedi », « tous les samedis »: the day stays, singular, for the date.
    [GeneratedRegex(Before + @"(?<mark>chaque|tous\s+les|toutes\s+les)\s+(?<day>lundi|mardi|mercredi|jeudi|vendredi|samedi|dimanche)(?<s>s)?" + After,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex WeeklyDayRegex();

    [GeneratedRegex(Before + @"(?:chaque\s+semaine|toutes\s+les\s+semaines|une\s+fois\s+par\s+semaine|(?:en\s+)?hebdo(?:madaire)?s?|weekly)" + After,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout)]
    private static partial Regex WeeklyRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.None, Timeout)]
    private static partial Regex WordRegex();

    // Its kind, by a word it says. A game is the default and never read.
    private static readonly HashSet<string> MovieWords = new()
    {
        "film", "films", "cine", "cinema", "movie", "movies", "visionnage", "netflix", "projection", "watchparty",
    };

    private static readonly HashSet<string> ActivityWords = new()
    {
        "resto", "restau", "restaurant", "sortie", "bowling", "apero", "karaoke", "balade", "rando", "randonnee",
        "escape", "laser", "lasergame", "accrobranche", "piscine", "bar", "picnic", "piquenique", "pique", "barbecue",
        "bbq", "concert", "musee", "expo", "plage", "paintball", "kart", "karting", "patinoire", "fete",
    };

    public static Reading Read(string content)
    {
        var buffer = content.ToCharArray();
        void Blank(int start, int length)
        {
            for (var k = start; k < start + length && k < buffer.Length; k++) buffer[k] = ' ';
        }

        try
        {
            int? places = null;
            foreach (var regex in new[] { PlacesNounRegex(), PlacesMaxRegex(), PlacesTrailingRegex() })
            {
                if (places is not null) break;
                foreach (Match m in regex.Matches(content))
                {
                    if (!int.TryParse(m.Groups["n"].Value, out var n) || n < SessionSeats.MinPlayers || n > SessionSeats.MaxPlayers) continue;
                    places = n;
                    Blank(m.Index, m.Length);
                    break;
                }
            }

            int? minutes = null;
            if (RangeRegex().Match(content) is { Success: true } range)
            {
                var start = int.Parse(range.Groups["a"].Value) * 60 + (range.Groups["am"].Success ? int.Parse(range.Groups["am"].Value) : 0);
                var end = int.Parse(range.Groups["b"].Value) * 60 + (range.Groups["bm"].Success ? int.Parse(range.Groups["bm"].Value) : 0);
                if (end <= start) end += 24 * 60;
                var length = end - start;
                if (SessionTime.ParseDuration(length.ToString()) is { } ok)
                {
                    minutes = ok;
                    Blank(range.Groups["end"].Index, range.Groups["end"].Length);
                }
            }
            if (minutes is null)
            {
                foreach (Match m in LengthRegex().Matches(content))
                {
                    int? read;
                    if (m.Groups["min"].Success) read = SessionTime.ParseDuration(m.Groups["min"].Value);
                    else
                    {
                        var h = int.Parse(m.Groups["h"].Value);
                        // « pour 21h », « de 20h » are times: only « pendant » / « durée » take any length.
                        var lead = m.Groups["lead"].Value.ToLowerInvariant();
                        if (h > 6 && lead is "pour" or "sur" or "de") continue;
                        read = SessionTime.ParseDuration(m.Groups["m"].Success ? $"{h}h{m.Groups["m"].Value}" : $"{h}h");
                    }
                    if (read is null) continue;
                    minutes = read;
                    Blank(m.Index, m.Length);
                    break;
                }
            }

            var weekly = false;
            if (WeeklyDayRegex().Match(content) is { Success: true } day)
            {
                weekly = true;
                Blank(day.Groups["mark"].Index, day.Groups["mark"].Length);
                if (day.Groups["s"].Success) Blank(day.Groups["s"].Index, 1);
            }
            else if (WeeklyRegex().Match(content) is { Success: true } week)
            {
                weekly = true;
                Blank(week.Index, week.Length);
            }

            SessionCategory? category = null;
            var words = WordRegex().Matches(content).Select(m => FrenchDate.Clean(m.Value).Replace(" ", "")).ToList();
            if (words.Any(MovieWords.Contains)) category = SessionCategory.Movie;
            else if (words.Any(ActivityWords.Contains) && !words.Contains("jeux") && !words.Contains("jeu")) category = SessionCategory.Activity;

            return new Reading(new string(buffer), places, minutes, weekly, category);
        }
        catch (RegexMatchTimeoutException)
        {
            // Hostile text: read nothing more than the date and title.
            return new Reading(content, null, null, false, null);
        }
    }
}
