using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>A session asked for in chat: what it is, as written, and when. Either may be missing.</summary>
public sealed record SessionAsk(string Title, DateTimeOffset? At);

/// <summary>A `/tonight` call asked for in chat: the game, as written, and the hour if one was said for tonight.</summary>
public sealed record TonightAsk(string Game, DateTimeOffset? At);

/// <summary>A reminder asked for in chat: what to remind, as written, and when.</summary>
public sealed record ReminderAsk(string Text, DateTimeOffset? Due);

/// <summary>A vote asked for in chat: its title and its options (two to ten, or none).</summary>
public sealed record VoteAsk(string Title, IReadOnlyList<string> Options);

/// <summary>Teams asked for in chat: how many, and whether balanced by level.</summary>
public sealed record TeamsAsk(int Count, bool Balanced);

/// <summary>
/// The details of a request made to her in chat (« syncs, organise un Valorant samedi 21h »), pure, so
/// the checks can pin them. MessageCues decides it is a request and of what; this reads what it says.
/// The day and time go through the readers every typed date uses (SessionTime, ReminderTime, which
/// sit on FrenchDate): the time is the longest run of words they accept, and since they refuse an
/// unknown word, that run is the date and nothing around it. What's left, once the request's own
/// words are stripped from both ends, is the title, rebuilt from the original text so its spelling
/// and case are the person's.
/// </summary>
public static partial class ChatRequest
{
    // The longest run of words tried as a date: « samedi de la semaine prochaine à 21h30 » is 8.
    private const int MaxWhenWords = 8;

    // Read on a message this long at most: a wall of text is not a request, and the window scan is
    // quadratic in the words.
    public const int MaxLength = 300;

    // A word, with what can sit inside a date (« 21:30 », « 25/12 », « 25.12 ») and an elided
    // article kept on it (« d' »). Hyphens split: « rappelle-moi » is two words.
    [GeneratedRegex(@"[\p{L}\p{N}]+(?:[:/.][\p{L}\p{N}]+)*['’]?", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"<a?:\w+:\d+>|<[@#][!&]?\d+>|https?://\S+", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex MarkupRegex();

    // Options: commas, semicolons, slashes, « ou ».
    [GeneratedRegex(@"\s*(?:[,;/]|\bou\b|\bor\b)\s*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 200)]
    private static partial Regex OptionSplitRegex();

    // « entre pizza et sushi »: « et » splits too, only after « entre ».
    [GeneratedRegex(@"\s*(?:[,;/]|\bou\b|\bet\b)\s*", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 200)]
    private static partial Regex BetweenSplitRegex();

    private readonly record struct Word(string Norm, int Start, int End);

    // A lone abbreviation FrenchDate reads as a day, but that chat uses as a word: « un jeu » is not
    // Thursday, « sam » may be a name. Next to another date word they still count (« sam 21h »).
    private static readonly HashSet<string> AmbiguousAlone = new()
    {
        "jeu", "mar", "mer", "merc", "dim", "ven", "vend", "sam", "lun", "we", "dem", "dmn", "auj", "nuit", "matin",
    };

    // Said around any request: her name, politeness, the asking itself.
    private static readonly string[] AskWords =
    {
        "syncs", "sync", "stp", "svp", "sil", "te", "plait", "plais", "please", "plz", "merci", "tu", "peux", "pourrais", "peut",
        "veux", "voudrais", "voulais", "est", "ce", "que", "qu", "ca", "dirait", "dit", "j", "aimerais", "faudrait", "il",
        "hey", "he", "eh", "dis", "yo", "allez", "bon", "alors", "ok", "oui", "vous", "toi", "pas", "s",
    };

    // A session's own words: the asking, proposing, the generic nouns and their links.
    private static readonly HashSet<string> SessionStrip = new(AskWords.Concat(new[]
    {
        "on", "nous", "se", "s", "fait", "fais", "faire", "faisait", "faisiez", "refait", "refais", "refaire",
        "organise", "organiser", "organises", "organisait", "organisez", "lance", "lancer", "lances", "relance", "relancer",
        "cree", "creer", "creez", "prepare", "preparer", "programme", "programmer", "planifie", "planifier", "et", "si", "me",
        "m", "moi", "un", "une", "des", "du", "la", "le", "les", "l", "petite", "petit", "nouvelle", "nouveau", "session",
        "sessions", "soiree", "soirees", "partie", "parties", "game", "games", "event", "evenement", "rdv", "seance",
        "planning", "de", "d", "sur", "a", "au", "pour", "avec", "entre", "potes", "copains", "ensemble",
    }));

    // A /tonight call's: the session's, and the call to the room.
    private static readonly HashSet<string> TonightStrip = new(SessionStrip.Concat(new[]
    {
        "qui", "chaud", "chauds", "chaude", "chaudes", "partant", "partants", "partante", "chaud", "tonight", "appel",
        "jouer", "joue", "dispo", "dispos", "y", "a",
    }));

    // A reminder's: the asking only. Articles stay: « la lessive » is what to remind.
    private static readonly HashSet<string> ReminderStrip = new(AskWords.Concat(new[]
    {
        "rappelle", "rappelles", "rappeler", "rappel", "previens", "prevenir", "previent", "fais", "fait", "faire",
        "penser", "moi", "me", "m", "nous", "de", "d", "a", "un", "petit", "pour", "dire", "si",
    }));

    // A vote's topic words: the options come after them.
    private static readonly HashSet<string> VoteWords = new() { "sondage", "sondages", "vote", "votes", "poll", "strawpoll" };

    private static readonly HashSet<string> VoteLeadStrip = new() { "pour", "sur", "de", "d", "a", "au", "du", "rapide", "entre" };

    private static readonly HashSet<string> TeamWords = new() { "equipe", "equipes", "team", "teams", "groupe", "groupes" };

    private static readonly HashSet<string> BalancedWords = new()
    {
        "equilibre", "equilibree", "equilibrees", "equilibres", "equitable", "equitables", "niveau", "niveaux",
    };

    private static readonly Dictionary<string, int> SpelledCounts = new() { ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4 };

    // ---- What a request says ----------------------------------------------------------------

    public static SessionAsk ReadSession(string content, DateTimeOffset nowUtc)
    {
        var (text, words) = Words(content);
        var when = FindWhen(text, words, w => SessionTime.Parse(w, nowUtc));
        return new SessionAsk(Rest(text, words, when, SessionStrip, InputCaps.Title), when?.At);
    }

    public static TonightAsk ReadTonight(string content, DateTimeOffset nowUtc)
    {
        var (text, words) = Words(content);
        var when = FindWhen(text, words, w => SessionTime.Parse(w, nowUtc));
        // Tonight only: today, or the small hours after it (a call for « 1h » at 23:00).
        var at = when?.At is { } t && IsTonight(t, nowUtc) ? t : (DateTimeOffset?)null;
        return new TonightAsk(Rest(text, words, when, TonightStrip, InputCaps.TonightGame), at);
    }

    public static ReminderAsk ReadReminder(string content, DateTimeOffset nowUtc)
    {
        var (text, words) = Words(content);
        var when = FindWhen(text, words, w => ReminderTime.Parse(w, nowUtc));
        return new ReminderAsk(Rest(text, words, when, ReminderStrip, InputCaps.Reminder, keepNegation: true), when?.At);
    }

    public static VoteAsk ReadVote(string content)
    {
        var (text, words) = Words(content);
        var topic = words.FindIndex(w => VoteWords.Contains(w.Norm));
        if (topic < 0) return new VoteAsk(string.Empty, Array.Empty<string>());

        var after = text[words[topic].End..];
        var title = string.Empty;
        var colon = after.IndexOf(':');
        // « un sondage pour ce soir : valo, lol »: the title before the colon, the options after.
        // A colon inside a time (« 21:30 ») has no space after it, and is not this one.
        if (colon >= 0 && (colon + 1 == after.Length || !char.IsDigit(after[colon + 1])))
        {
            var (titleText, titleWords) = Words(after[..colon]);
            title = Rest(titleText, titleWords, null, VoteLeadStrip, InputCaps.Title);
            after = after[(colon + 1)..];
        }

        var (optText, optWords) = Words(after);
        var between = optWords.Count > 0 && optWords[0].Norm == "entre";
        var lead = optWords.TakeWhile(w => VoteLeadStrip.Contains(w.Norm)).Count();
        var listText = lead < optWords.Count ? optText[optWords[lead].Start..] : string.Empty;
        var pieces = (between ? BetweenSplitRegex() : OptionSplitRegex()).Split(listText)
            .Select(p => TrimTail(p.Trim().TrimEnd('?', '!', '.', ' ')))
            .Where(p => p.Length > 0);
        var options = PollRules.ParseOptions(string.Join('\n', pieces));
        if (options.Count < 2) return new VoteAsk(title, Array.Empty<string>());

        if (title.Length == 0)
        {
            var asked = string.Join(" ou ", options) + " ?";
            if (asked.Length <= InputCaps.Title) title = asked;
        }
        return new VoteAsk(title, options);
    }

    public static TeamsAsk ReadTeams(string content)
    {
        var (_, words) = Words(content);
        var count = 2;
        for (var i = 0; i < words.Count; i++)
        {
            var n = Count(words[i].Norm);
            if (n is null) continue;
            var next = i + 1 < words.Count ? words[i + 1].Norm : null;
            var prev = i > 0 ? words[i - 1].Norm : null;
            if (next is not null && TeamWords.Contains(next) || prev == "en")
            {
                count = n.Value;
                break;
            }
        }
        var balanced = words.Any(w => BalancedWords.Contains(w.Norm));
        return new TeamsAsk(count, balanced);
    }

    /// <summary>
    /// Whether some run of words reads as a day or a time, past or not: what makes « organise un
    /// valo samedi » a session request. The cues have no clock, so this asks the shape only.
    /// </summary>
    public static bool HasWhen(string content)
    {
        var (text, words) = Words(content);
        var now = DateTimeOffset.UtcNow;
        return FindWhen(text, words, w => FrenchDate.Parse(w, now, SessionTime.DefaultHour, SessionTime.DefaultHour)
                                          ?? ReminderTime.ParseRelative(w, now)) is not null;
    }

    // ---- The pieces ----------------------------------------------------------------------------

    private static (string Text, List<Word> Words) Words(string content)
    {
        var text = MarkupRegex().Replace(content.Length > MaxLength ? content[..MaxLength] : content, " ");
        var words = new List<Word>();
        foreach (Match m in WordRegex().Matches(text))
        {
            var norm = FrenchDate.Clean(m.Value).Replace(" ", "");
            if (norm.Length > 0) words.Add(new Word(norm, m.Index, m.Index + m.Length));
        }
        return (text, words);
    }

    private readonly record struct When(int First, int Last, DateTimeOffset At);

    // The run of words the reader accepts that says the most: the most date words (fillers like « à »,
    // « pour », « le » don't count), a number breaking a tie (« à 22h » says more than « tonight
    // pour »), then the tightest, then the first. So the fillers around a date stay with the words
    // they belong to (« à 18h la lessive » keeps « la »). A run needs a letter, a « : » or a « / »:
    // a bare number is a count (« 3 équipes »), not an hour; and a run whose only date word is a chat
    // word FrenchDate also reads as a day (« un jeu de société ») is that word.
    private static When? FindWhen(string text, List<Word> words, Func<string, DateTimeOffset?> read)
    {
        When? best = null;
        var bestScore = 0;
        for (var i = 0; i < words.Count; i++)
        {
            for (var len = 1; len <= Math.Min(MaxWhenWords, words.Count - i); len++)
            {
                var run = words.GetRange(i, len);
                if (Fillers.Contains(run[^1].Norm)) continue;
                if (!run.Any(w => w.Norm.Any(char.IsLetter) || w.Norm.Contains(':') || w.Norm.Contains('/'))) continue;
                var dateWords = run.Where(w => !Fillers.Contains(w.Norm)).ToList();
                if (dateWords.Count == 1 && AmbiguousAlone.Contains(dateWords[0].Norm)) continue;
                var score = dateWords.Count * 2 + (run.Any(w => w.Norm.Any(char.IsDigit)) ? 1 : 0);
                if (score <= bestScore) continue;
                if (read(text[run[0].Start..run[^1].End]) is { } at)
                {
                    best = new When(i, i + len - 1, at);
                    bestScore = score;
                }
            }
        }
        return best;
    }

    // FrenchDate's words that carry nothing (« le samedi à 21h », « vers 21h »): a date run never
    // ends on one, so « à 18h la lessive » leaves « la » with the lessive. It may start on one: « le
    // 25 », « ce soir ».
    private static readonly HashSet<string> Fillers = new()
    {
        "le", "la", "l", "a", "au", "vers", "pour", "on", "des", "partir", "de", "du", "d", "environ", "genre",
        "pile", "ce", "cet", "cette", "en",
    };

    // What's left once the date and the request's own words at both ends are gone, rebuilt from the
    // original text: each stretch between the date and the edges as written, joined by a space.
    // With keepNegation, a « pas » right after the link « de » / « d' » is what's asked, not the asking:
    // « rappelle-moi de pas écouter Ina » is « pas écouter Ina » (« tu peux pas me rappeler » still strips).
    private static string Rest(string text, List<Word> words, When? when, HashSet<string> strip, int cap, bool keepNegation = false)
    {
        var kept = Enumerable.Range(0, words.Count).Where(i => when is not { } w || i < w.First || i > w.Last).ToList();
        while (kept.Count > 0 && strip.Contains(words[kept[0]].Norm)
               && !(keepNegation && words[kept[0]].Norm == "pas" && kept[0] > 0 && words[kept[0] - 1].Norm is "de" or "d"))
            kept.RemoveAt(0);
        while (kept.Count > 0 && strip.Contains(words[kept[^1]].Norm)) kept.RemoveAt(kept.Count - 1);
        if (kept.Count == 0) return string.Empty;

        var pieces = new List<string>();
        var runStart = kept[0];
        for (var k = 1; k <= kept.Count; k++)
        {
            if (k < kept.Count && kept[k] == kept[k - 1] + 1) continue;
            pieces.Add(text[words[runStart].Start..words[kept[k - 1]].End]);
            if (k < kept.Count) runStart = kept[k];
        }
        var rest = string.Join(' ', pieces).Trim();
        return rest.Length <= cap ? rest : rest[..cap].TrimEnd();
    }

    // An option's own trailing politeness: « sushi stp » is sushi.
    private static string TrimTail(string option)
    {
        var (text, words) = Words(option);
        var last = words.Count;
        while (last > 0 && words[last - 1].Norm is "stp" or "svp" or "syncs" or "sync" or "merci" or "plz") last--;
        return last == 0 ? string.Empty : text[words[0].Start..words[last - 1].End];
    }

    private static int? Count(string norm)
    {
        if (int.TryParse(norm, out var n)) return n is >= 2 and <= 4 ? n : null;
        return SpelledCounts.TryGetValue(norm, out var spelled) ? spelled : null;
    }

    private static bool IsTonight(DateTimeOffset at, DateTimeOffset nowUtc)
    {
        var today = AppTime.ToZoned(nowUtc).Date;
        var local = AppTime.ToZoned(at).DateTime;
        return local.Date == today || (local.Date == today.AddDays(1) && local.Hour < 6);
    }
}
