using System.Text.RegularExpressions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// A session asked for in chat: what it is, as written, and when. Either may be missing. The rest is
/// what the message said, null (or false) when it said nothing: places (« à 5 joueurs »), length in
/// minutes (« pendant 2h », « de 21h à 23h »), every week (« tous les samedis »), and its kind
/// (« soirée film » is a Movie, « un resto » an Activity; a game is never read, it's the default).
/// </summary>
public sealed record SessionAsk(string Title, DateTimeOffset? At, int? Places = null, int? Minutes = null,
    bool Weekly = false, SessionCategory? Category = null);

/// <summary>A `/tonight` call asked for in chat: the game, as written, and the hour if one was said for tonight.</summary>
public sealed record TonightAsk(string Game, DateTimeOffset? At);

/// <summary>A reminder asked for in chat: what to remind, as written, and when.</summary>
public sealed record ReminderAsk(string Text, DateTimeOffset? Due);

/// <summary>
/// A vote asked for in chat: its title and its options (two to ten, or none). Asked: the title is a
/// question she read (« pour savoir qui vient » → « Qui vient ? »), not one built from the options.
/// </summary>
public sealed record VoteAsk(string Title, IReadOnlyList<string> Options, bool Asked = false);

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
        "planning", "de", "d", "sur", "a", "au", "pour", "avec", "entre", "potes", "copains", "ensemble", "jouer",
    }));

    // What a session's name may end on: the session's own words, less its generic nouns, which can
    // close a name (« escape game »).
    private static readonly HashSet<string> SessionTail = new(SessionStrip.Except(new[]
    {
        "game", "games", "partie", "parties", "session", "sessions", "soiree", "soirees", "event", "evenement", "seance",
        "rdv", "planning",
    }));

    // A /tonight call's: the session's, and the call to the room.
    private static readonly HashSet<string> TonightStrip = new(SessionStrip.Concat(new[]
    {
        "qui", "chaud", "chauds", "chaude", "chaudes", "partant", "partants", "partante", "chaud", "tonight", "appel",
        "jouer", "joue", "dispo", "dispos", "y", "a",
    }));

    // A reminder's verbs: once one is said, the link after it (« de », « d' », « à », « que »…) opens
    // what to remind.
    private static readonly HashSet<string> ReminderVerbs = new()
    {
        "rappelle", "rappelles", "rappeler", "rappel", "rappels", "previens", "prevenir", "previent", "penser", "dire",
        "reveille", "reveiller", "remind", "remindme",
    };

    // A reminder's: the asking only. Articles stay: « la lessive » is what to remind.
    private static readonly HashSet<string> ReminderStrip = new(AskWords.Concat(ReminderVerbs).Concat(new[]
    {
        "fais", "fait", "faire", "moi", "me", "m", "nous", "de", "d", "a", "un", "petit", "pour", "si", "to",
    }));

    // What a reminder may end on that isn't its own: politeness and her name.
    private static readonly HashSet<string> ReminderTail = new()
    {
        "stp", "svp", "sil", "s", "il", "te", "plait", "plais", "please", "plz", "merci", "syncs", "sync", "hein",
    };

    // What joins the asking to what's asked.
    private static readonly HashSet<string> Links = new() { "de", "d", "a", "que", "qu", "pour", "to" };

    // Articles: written with a capital mid-sentence, they start a name (« Les Sims », « A Way Out »).
    private static readonly HashSet<string> Articles = new()
    {
        "le", "la", "les", "l", "un", "une", "des", "du", "de", "d", "a", "au",
    };

    // A vote's topic words: the options come after them.
    private static readonly HashSet<string> VoteWords = new() { "sondage", "sondages", "vote", "votes", "poll", "strawpoll" };

    private static readonly HashSet<string> VoteLeadStrip = new() { "pour", "sur", "de", "d", "a", "au", "du", "rapide", "entre" };

    // A vote's topic, as its title: its leads, articles, the asking and her name. Not « ce », which
    // opens a topic (« pour ce soir »).
    private static readonly HashSet<string> VoteTitleStrip = new(VoteLeadStrip.Concat(Articles).Concat(AskWords).Concat(new[]
    {
        "concernant", "propos", "sujet", "savoir", "demander", "voir", "petit", "vite", "fait",
    }).Where(w => w != "ce"));

    private static readonly HashSet<string> TeamWords = new() { "equipe", "equipes", "team", "teams", "groupe", "groupes" };

    private static readonly HashSet<string> BalancedWords = new()
    {
        "equilibre", "equilibree", "equilibrees", "equilibres", "equitable", "equitables", "niveau", "niveaux",
    };

    private static readonly Dictionary<string, int> SpelledCounts = new() { ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4 };

    // ---- What a request says ----------------------------------------------------------------

    public static SessionAsk ReadSession(string content, DateTimeOffset nowUtc)
    {
        // Places, length and « every week » are read first and blanked out, so neither the date nor
        // the title sees them (« pendant 2h » is no hour, « 5 joueurs » no part of the name).
        var details = SessionDetails.Read(content.Length > MaxLength ? content[..MaxLength] : content);
        var (text, words) = Words(details.Masked);
        var when = FindWhen(text, words, w => SessionTime.Parse(w, nowUtc));
        var title = Rest(text, words, when, SessionStrip, InputCaps.Title, tail: SessionTail);
        if (title.Length > 0) title = char.ToUpperInvariant(title[0]) + title[1..];
        return new SessionAsk(title, when?.At, details.Places, details.Minutes, details.Weekly, details.Category);
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
        return new ReminderAsk(Rest(text, words, when, ReminderStrip, InputCaps.Reminder, ReminderVerbs, ReminderTail), when?.At);
    }

    public static VoteAsk ReadVote(string content)
    {
        var (text, words) = Words(content);
        var topic = words.FindIndex(w => VoteWords.Contains(w.Norm));
        if (topic < 0) return new VoteAsk(string.Empty, Array.Empty<string>());

        var after = text[words[topic].End..];
        var title = string.Empty;
        var colon = after.IndexOf(':');
        // A colon inside a time (« 21:30 ») has no space after it, and is not this one.
        var hasColon = colon >= 0 && (colon + 1 == after.Length || !char.IsDigit(after[colon + 1]));

        // « un sondage oui ou non pour le défi créatif »: the answers said, the rest is what it's about.
        var (leadText, leadWords) = Words(after);
        if (leadWords.Count >= 2 && leadWords[0].Norm == "oui"
            && (leadWords[1].Norm == "non" || leadWords.Count >= 3 && leadWords[1].Norm is "ou" or "or" && leadWords[2].Norm == "non"))
        {
            var skip = leadWords[1].Norm == "non" ? 2 : 3;
            var about = Topic(skip < leadWords.Count ? leadText[leadWords[skip].Start..] : string.Empty);
            return new VoteAsk(about, ["Oui", "Non"], Asked: about.Length > 0);
        }

        // « un sondage pour savoir qui vient samedi », « un sondage : qui est chaud ? »: a question,
        // read before the options, since « si on joue à valo ou lol » would split into two.
        if (ReadQuestion(hasColon ? after[(colon + 1)..] : after, needsLead: !hasColon) is { } question) return question;

        // « un sondage pour ce soir : valo, lol »: the title before the colon, the options after. A
        // question there is the title (« pour savoir qui vient : Valo ou LoL » → « Qui vient ? »).
        var askedTitle = false;
        if (hasColon)
        {
            var before = after[..colon];
            if (ReadQuestion(before, needsLead: true) is { } asked)
            {
                title = asked.Title;
                askedTitle = true;
            }
            else title = Topic(before);
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
        if (options.Count < 2)
        {
            // « un sondage pour le défi créatif »: no options, but what it's for is its title.
            if (!hasColon && optWords.Count > 0 && optWords[0].Norm is "pour" or "sur" or "concernant") title = Topic(after);
            return new VoteAsk(title, Array.Empty<string>(), askedTitle);
        }

        if (title.Length == 0)
        {
            var built = string.Join(" ou ", options) + " ?";
            if (built.Length <= InputCaps.Title) title = built;
        }
        return new VoteAsk(title, options, askedTitle);
    }

    // What a vote is about, as its title: « pour le défi créatif » → « Défi créatif ».
    private static string Topic(string source)
    {
        var (text, words) = Words(source);
        var rest = TrimTail(Rest(text, words, null, VoteTitleStrip, InputCaps.Title).TrimEnd('?', '!', '.', ' '));
        return rest.Length == 0 ? rest : char.ToUpperInvariant(rest[0]) + rest[1..];
    }

    // The question a vote is for, from the words after the vote word (or after its colon):
    // - « qui … » keeps its words: « Qui voudrait participer au défi créatif ? ». Its options come from
    //   the verb (« participer » → Participe / Ne participe pas), « Moi » / « Pas moi » for a wish
    //   (« qui veut une pizza »), none otherwise (« qui est le meilleur ? » needs names).
    // - « si … » turns direct, Oui / Non: « si on joue samedi » → « On joue samedi ? ». With an « ou »
    //   inside it's a choice, so it keeps the title and leaves the options to them.
    // - « est-ce que … » reads like « si … » (« Vous venez samedi ? »).
    // - « quel / quand / où / comment / à quoi… » and « ce que » (→ « Qu'est-ce que ») give the title only.
    // - After a colon, any question closed on « ? » is its title as written (« : on joue à quoi ce
    //   soir ? »), read like « si … » unless a question word leaves the answers open. A short list
    //   with a « ? » (« : Valorant, LoL ? ») stays a list.
    // Straight after the vote word, « qui » is a relative (« un sondage qui dure 2 jours »): without a
    // colon, the question needs an asking word before it (« pour savoir », « demander », « sur »).
    private static VoteAsk? ReadQuestion(string source, bool needsLead)
    {
        var (text, words) = Words(source);
        var i = 0;
        var askedFor = false;
        while (i < words.Count && QuestionLead.Contains(words[i].Norm))
        {
            if (AskingLead.Contains(words[i].Norm)) askedFor = true;
            i++;
        }
        if (i >= words.Count - 1 || needsLead && !askedFor) return null;

        var head = words[i];
        var body = i + 1;
        string? title;
        IReadOnlyList<string> options = Array.Empty<string>();
        switch (head.Norm)
        {
            case "qui":
                title = Question(text, words, i);
                options = WhoOptions(text, words, body);
                break;
            case "si" or "s":
                // « s'il », « s'ils »: the pronoun opens the direct question.
                if (head.Norm == "s" && words[body].Norm is not ("il" or "ils")) return null;
                title = Question(text, words, body);
                if (title is not null) options = DirectOptions(title);
                break;
            case "est" when body + 1 < words.Count && words[body].Norm == "ce" && words[body + 1].Norm is "que" or "qu":
                title = Question(text, words, body + 2);
                if (title is not null) options = DirectOptions(title);
                break;
            case "ce" when words[body].Norm is "que" or "qu":
                title = Question(text, words, body) is { } rest ? "Qu'est-ce " + char.ToLowerInvariant(rest[0]) + rest[1..] : null;
                break;
            case "quoi":
                // « à quoi on joue » keeps its « à », which the lead skipped.
                title = Question(text, words, i > 0 && words[i - 1].Norm is "a" or "de" or "en" or "sur" or "avec" ? i - 1 : i);
                break;
            default:
                if (IsQuestionWord(text, head)) title = Question(text, words, i);
                else if (!needsLead && IsDirectQuestion(source)) title = Question(text, words, 0);
                else return null;
                if (title is not null && !IsQuestionWord(text, head)) options = DirectOptions(title);
                break;
        }
        return title is null || title.Length > InputCaps.Title ? null : new VoteAsk(title, options, Asked: true);
    }

    // « quel », « quand », « où »… opening a question. « où » is « ou » once cleaned, which is also the
    // options' « ou »: the accent decides.
    private static bool IsQuestionWord(string text, Word word) =>
        QuestionWords.Contains(word.Norm) || word.Norm == "ou" && text[word.Start..word.End].Contains('ù');

    // After a colon: a question closed on « ? », not a short list (« Valorant, LoL ? »): a list's first
    // piece is a name, a question's first piece says something (three words or more).
    private static bool IsDirectQuestion(string source)
    {
        var trimmed = source.TrimEnd();
        if (!trimmed.EndsWith('?')) return false;
        var pieces = OptionSplitRegex().Split(trimmed.TrimEnd('?', ' ')).Where(p => p.Trim().Length > 0).ToList();
        return pieces.Count < 2 || Words(pieces[0]).Words.Count >= 3;
    }

    // The answers to a yes-or-no question as a title (« On joue samedi ? »): none when a question word
    // leaves them open (« on joue à quoi »), otherwise IfOptions.
    private static IReadOnlyList<string> DirectOptions(string title)
    {
        var clause = title.EndsWith(" ?") ? title[..^2] : title;
        var (text, words) = Words(clause);
        if (words.Any(w => w.Norm is "quoi" or "qui" || IsQuestionWord(text, w))) return Array.Empty<string>();
        return IfOptions(clause);
    }

    // The words from `from` to the end, as written, capitalised, closed on « ? »: trailing politeness,
    // her name and punctuation dropped (TrimTail).
    private static string? Question(string text, List<Word> words, int from)
    {
        if (from >= words.Count) return null;
        var rest = TrimTail(text[words[from].Start..].Trim().TrimEnd('?', '!', '.', ' ', '…'));
        if (rest.Length == 0) return null;
        return char.ToUpperInvariant(rest[0]) + rest[1..] + " ?";
    }

    // « si … »: Oui / Non, unless an « ou » (or a comma list) makes it a choice, whose options are
    // what it chooses between: « on joue à Valo ou LoL » → Valo, LoL. Each option after the first is
    // its piece, less a leading link (« ou à Gartic », « ou des sushis »). The first is the end of the
    // piece before: after its last link (« on joue à Among Us » → Among Us), or as many words as the
    // second has (« on joue samedi ou dimanche » → samedi; « on reste ou on part » → on reste).
    // « ou pas », « ou non » keep it a yes-or-no.
    private static IReadOnlyList<string> IfOptions(string clause)
    {
        var pieces = OptionSplitRegex().Split(clause).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (pieces.Count < 2) return ["Oui", "Non"];

        var others = pieces.Skip(1).Select(StripLinks).Where(p => p.Length > 0).ToList();
        if (others.Count == 0 || others.Any(o => Words(o).Words.All(w => w.Norm is "pas" or "non" or "du" or "tout" or "not")))
            return ["Oui", "Non"];

        var (leftText, leftWords) = Words(pieces[0]);
        if (leftWords.Count == 0) return Array.Empty<string>();
        var link = leftWords.FindLastIndex(w => ChoiceLinks.Contains(w.Norm));
        var start = link >= 0 && link < leftWords.Count - 1
            ? link + 1
            : Math.Max(0, leftWords.Count - Words(others[0]).Words.Count);
        var first = leftText[leftWords[start].Start..].Trim();
        // « ou à LoL ce soir »: the date after the last option belongs to the question.
        others[^1] = TrimTrailingWhen(others[^1]);

        var options = PollRules.ParseOptions(string.Join('\n', others.Prepend(first)));
        return options.Count >= 2 ? options : Array.Empty<string>();
    }

    // An option less the date it ends on: « LoL ce soir » is LoL. The longest such tail, never the
    // whole option, never a lone chat word FrenchDate also reads as a day (« un jeu »).
    private static string TrimTrailingWhen(string option)
    {
        var (text, words) = Words(option);
        var now = DateTimeOffset.UtcNow;
        for (var k = 1; k < words.Count; k++)
        {
            if (words.Count - k == 1 && AmbiguousAlone.Contains(words[k].Norm)) continue;
            var tail = text[words[k].Start..];
            if (FrenchDate.Parse(tail, now, SessionTime.DefaultHour, SessionTime.DefaultHour) is not null
                || ReminderTime.ParseRelative(tail, now) is not null)
                return text[..words[k].Start].Trim();
        }
        return option;
    }

    // An option less the link or article it opens on: « à Gartic » is Gartic, « des sushis » sushis.
    private static string StripLinks(string piece)
    {
        var (text, words) = Words(piece);
        var k = 0;
        while (k < words.Count && ChoiceLinks.Contains(words[k].Norm) && !StartsName(text, words[k])) k++;
        return k < words.Count ? text[words[k].Start..].Trim() : string.Empty;
    }

    // What sits before a choice's option: « on joue à X », « on mange des X », « on part sur X ».
    private static readonly HashSet<string> ChoiceLinks = new()
    {
        "a", "au", "aux", "de", "du", "des", "d", "le", "la", "les", "l", "un", "une", "sur", "en", "avec", "pour",
        "plutot", "entre",
    };

    // « qui … »: the first verb or state in the next few words that says how to answer; a wish only
    // when none does (« qui voudrait participer » is Participe, not Moi).
    private static IReadOnlyList<string> WhoOptions(string text, List<Word> words, int from)
    {
        var window = words.Skip(from).Take(6).ToList();
        foreach (var w in window)
        {
            if (WhoAnswers.TryGetValue(w.Norm, out var pair)) return [pair.Yes, pair.No];
            // « qui sera là » — « là », not the article « la ».
            if (w.Norm == "la" && text[w.Start..w.End].Contains('à')) return ["Présent", "Absent"];
        }
        return window.Any(w => Wishes.Contains(w.Norm)) ? ["Moi", "Pas moi"] : Array.Empty<string>();
    }

    // What may sit between the vote word and its question: « pour savoir », « afin de demander »,
    // « rapide sur », « à propos de ».
    private static readonly HashSet<string> QuestionLead = new()
    {
        "pour", "afin", "histoire", "de", "d", "sur", "savoir", "demander", "voir", "connaitre", "rapide", "petit",
        "vite", "fait", "a", "propos", "au", "sujet", "du", "qu", "on", "sache", "saches", "sachiez", "sachions",
    };

    // The ones that make it a question rather than a relative clause.
    private static readonly HashSet<string> AskingLead = new()
    {
        "savoir", "demander", "voir", "connaitre", "sur", "propos", "sujet", "sache", "saches", "sachiez", "sachions",
    };

    private static readonly HashSet<string> QuestionWords = new()
    {
        "quel", "quelle", "quels", "quelles", "quand", "comment", "combien", "lequel", "laquelle", "lesquels",
        "lesquelles", "pourquoi",
    };

    // A « qui » question's answers, by the verb or state it asks about: the third person, as the
    // question asks it (« Qui participe ? » — « Participe »).
    private static readonly Dictionary<string, (string Yes, string No)> WhoAnswers = BuildWhoAnswers();

    private static Dictionary<string, (string Yes, string No)> BuildWhoAnswers()
    {
        var map = new Dictionary<string, (string, string)>();
        void Add((string, string) pair, params string[] forms)
        {
            foreach (var f in forms) map[f] = pair;
        }
        Add(("Participe", "Ne participe pas"), "participer", "participe", "participent", "participera", "participeront",
            "participerait", "participeraient", "participes");
        Add(("Vient", "Ne vient pas"), "venir", "vient", "viennent", "viendra", "viendront", "viendrait", "viendraient");
        Add(("Joue", "Ne joue pas"), "jouer", "joue", "jouent", "jouera", "joueront", "jouerait", "joueraient");
        Add(("Dispo", "Pas dispo"), "dispo", "dispos", "disponible", "disponibles");
        Add(("Chaud", "Pas chaud"), "chaud", "chauds", "chaude", "chaudes", "chaud");
        Add(("Partant", "Pas partant"), "partant", "partants", "partante", "partantes");
        Add(("Présent", "Absent"), "present", "presents", "presente", "presentes");
        Add(("Inscrit", "Pas inscrit"), "inscrire", "inscrit", "inscrits", "inscrira", "inscrirait");
        return map;
    }

    // A wish with no verb above (« qui veut une pizza ? », « qui serait motivé ? »): answered by who.
    private static readonly HashSet<string> Wishes = new()
    {
        "veut", "veulent", "voudrait", "voudraient", "aimerait", "aimeraient", "envie", "motive", "motives", "motivee",
        "motivees", "interesse", "interesses", "interessee", "interessees", "tente", "tentes", "tentee", "tentees", "souhaite",
        "souhaitent", "souhaiterait",
    };

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
    // - An article written with a capital mid-sentence starts a name, and stays: « une session Les
    //   Sims », « une partie de A Way Out ».
    // - With verbs, the head ends at the link after the asking verb: everything after « rappelle-moi
    //   de » is what's asked, even words the asking also uses (« de pas écouter Ina », « de me
    //   coucher », « de faire la vaisselle », « de rappeler Paul »).
    // - tail is what may be stripped at the end (the strip set when null): a reminder's end is only
    //   politeness, « ce que tu dis » is its own.
    private static string Rest(string text, List<Word> words, When? when, HashSet<string> strip, int cap,
        HashSet<string>? verbs = null, HashSet<string>? tail = null)
    {
        var kept = Enumerable.Range(0, words.Count).Where(i => when is not { } w || i < w.First || i > w.Last).ToList();
        var asked = false;
        while (kept.Count > 0 && strip.Contains(words[kept[0]].Norm) && !StartsName(text, words[kept[0]]))
        {
            var norm = words[kept[0]].Norm;
            kept.RemoveAt(0);
            if (verbs is null) continue;
            if (verbs.Contains(norm)) asked = true;
            else if (asked && Links.Contains(norm)) break;
        }
        tail ??= strip;
        while (kept.Count > 0 && tail.Contains(words[kept[^1]].Norm)) kept.RemoveAt(kept.Count - 1);
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

    // An article with a capital that isn't a sentence's first word: the start of a name.
    private static bool StartsName(string text, Word word)
    {
        if (!Articles.Contains(word.Norm) || !char.IsUpper(text[word.Start])) return false;
        var before = text[..word.Start].TrimEnd();
        return before.Length > 0 && before[^1] is not ('.' or '!' or '?' or ':');
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
