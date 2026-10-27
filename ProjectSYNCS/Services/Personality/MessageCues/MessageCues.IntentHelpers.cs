using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Reading an intent: choices, topics, sharing, phrase finders and the tokenizer.
internal static partial class MessageCues
{
    /// <summary>
    /// The two options of a « X ou Y ? » asked with nothing else (« Valo ou LoL ? », « je joue ou je
    /// dors ? », « pile ou face »), or null. Each side is one to four words; « … ou pas ? », « … ou
    /// quoi ? » are not options, and a side opening on a question word or on « tu » asks something
    /// else (« c'est vrai ou faux ? », « tu dors ou quoi ? »).
    /// </summary>
    public static (string A, string B)? ReadChoice(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = Opinion.TrimTail(_leadingAddress.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ")).Trim();
        var match = _choiceRegex.Match(text);
        if (!match.Success) return null;
        if (!text.EndsWith('?') && !string.Equals(text.Trim('.', '!', ' '), "pile ou face", StringComparison.OrdinalIgnoreCase))
            return null;

        var (a, b) = (match.Groups["a"].Value.Trim(), match.Groups["b"].Value.Trim());
        var (ta, tb) = (TokenizeOrdered(a), TokenizeOrdered(b));
        if (ta.Count is 0 or > MaxChoiceSideTokens || tb.Count is 0 or > MaxChoiceSideTokens) return null;
        if (_notChoiceStart.Contains(ta[0]) || _notChoiceEnd.Contains(tb[0])) return null;
        return (a, b);
    }

    // The side's words, the « ou » between, and a closing « ? » (or « pile ou face » as said).
    private static readonly Regex _choiceRegex = new(
        @"^(?:(?:alors|bon|du coup|bref|dis|dis moi|dis-moi)[\s,]+)?(?<a>[^?,;.!:]+?)\s+ou\s+(?<b>[^?,;.!:]+?)[\s?!.]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private const int MaxChoiceSideTokens = 4;
    private static readonly HashSet<string> _notChoiceStart = new()
    {
        "qui", "quoi", "quel", "quelle", "quels", "quelles", "comment", "pourquoi", "ou", "quand", "combien", "est",
        "tu", "t", "vous", "ca", "c", "il", "elle", "ils", "elles", "mais", "et", "sinon",
    };
    private static readonly HashSet<string> _notChoiceEnd = new()
    {
        "pas", "non", "quoi", "bien", "alors", "sinon", "plus", "meme", "ptet", "autre", "koi", "comment", "rien",
    };

    // « je devrais y aller ? », « j'y vais ou pas ? », « Valo ou LoL ? », « pile ou face ».
    private static bool IsShouldI(List<string> t, string stripped, bool asks, HashSet<int> breaks)
    {
        if (ReadChoice(stripped) is not null) return true;
        if (!asks || t.Any(_openQuestionWords.Contains)) return false;
        if (FindPhrase(t, _shouldIPhrases, cancelAfter: false, cancelBefore: false)) return true;
        // « … ou pas ? » closing a question that is about the asker.
        return t.Count is >= 3 and <= 10 && t.Any(_firstPerson.Contains)
            && (EndsWith(t, "ou pas") || breaks.Any(b => b >= 2 && t[b - 2] == "ou" && t[b - 1] == "pas"));
    }

    // « réponds à ma question », « t'as pas répondu », « tu esquives », « réponds ! » (see _answerMePhrases).
    private static bool IsAnswerMe(List<string> t, HashSet<int> breaks)
    {
        if (t.Count is 0 or > MaxAnswerMeTokens) return false;
        if (FindPhrase(t, _answerMePhrases, cancelAfter: false, cancelBefore: false, notAfter: _notAnswerMeAfter)) return true;
        if (IsClosingAsk(t, _answerMeClosing, breaks, _answerMeFollowers)) return true;
        if (t.Any(w => w is "reponds" or "repond") && t.All(w => w is "reponds" or "repond" || _answerMeFillers.Contains(w)))
            return true;
        if (!FindPhrase(t, _myQuestion, cancelAfter: false, cancelBefore: false)) return false;
        for (var i = 0; i < t.Count; i++)
            if (_answerWords.Contains(t[i])
                && !t.Skip(Math.Max(0, i - AnswerSubjectWindow)).Take(i - Math.Max(0, i - AnswerSubjectWindow)).Any(_notHerAnswering.Contains))
                return true;
        return false;
    }

    // « tais-toi » in a short message, or « chut » / « silence » / « la ferme » as (nearly) all of it.
    private static bool IsShutUp(List<string> t)
    {
        if (t.Count is 0 or > MaxShutUpTokens) return false;
        // A « non » opening the message refuses what she said, it doesn't negate the shush: « non,
        // tais-toi », « non mais tais-toi ».
        var lead = 0;
        while (lead < t.Count && t[lead] == "non") lead++;
        if (FindPhrase(t.GetRange(lead, t.Count - lead), _shutUpPhrases, cancelAfter: false, cancelBefore: true)) return true;
        if (IsClosingAsk(t, _shutUpClosing, _noBreaks, _shutUpClosingFollowers)) return true;
        var said = string.Join(' ', t.Where(w => !_shutUpFillers.Contains(w)).Select(Squash));
        return _shutUpAlone.Contains(said) || _shutUpAlone.Contains(string.Join(' ', t.Select(Squash)));
    }

    /// <summary>What a MyStats question asks about (level, cailloux, Plynling, everything), or null.</summary>
    public static MyStatsTopic? ReadMyStatsTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        return ReadMyStatsTopic(TokenizeOrdered(text), text.Contains('?'), QuestionBreaks(text));
    }

    private static MyStatsTopic? ReadMyStatsTopic(List<string> t, bool asks, HashSet<int> breaks)
    {
        foreach (var (topic, phrases) in _myStatsPhrases)
            foreach (var phrase in phrases)
                if ((asks || phrase.Split(' ').Any(_myStatsAskWords.Contains))
                    && FindPhrase(t, [phrase], cancelAfter: false, cancelBefore: false))
                    return topic;

        if (!asks || t.Count > MaxMyStatsShortTokens) return null;
        foreach (var (topic, phrases) in _myStatsShortAsks)
            if (IsClosingAsk(t, phrases, breaks, _myStatsFollowers)) return topic;
        return null;
    }

    // « tu me notes ? », or « je suis beau ? » / « tu me trouves drôle ? » asked with the adjective closing.
    private static bool IsRateMe(List<string> t, bool asks, HashSet<int> breaks)
    {
        if (FindPhrase(t, _rateMePhrases, cancelAfter: false, cancelBefore: true)) return true;
        if (!asks) return false;

        foreach (var subject in _rateMeSubjects)
        {
            var length = subject.Split(' ').Length;
            for (var i = 0; i + length < t.Count; i++)
            {
                if (!PhraseStartsAt(t, i, subject)) continue;
                var j = i + length;
                var skipped = 0;
                while (j < t.Count && skipped < 2 && _downIntensifiers.Contains(t[j])) (j, skipped) = (j + 1, skipped + 1);
                if (j >= t.Count || !_rateMeAdjectives.Contains(Squash(t[j])) && !_rateMeAdjectives.Contains(t[j])) continue;
                var end = j + 1;
                if (end == t.Count || breaks.Contains(end) || _rateMeFollowers.Contains(t[end])) return true;
                // « en vrai » only: « je suis beau en vrai ? » asks, « je suis fort en maths ? » doesn't.
                if (t[end] == "en" && end + 1 < t.Count && t[end + 1] == "vrai") return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A call and nothing else: her name or tag with at most an interjection and punctuation (« syncs ? »,
    /// « hé syncs », a bare tag). What comes next from that person is for her (ChatListening). Shared
    /// with her Minecraft body's call (McTalkReader.IsBareCall), mentions and emotes dropped first.
    /// </summary>
    public static bool IsBareCall(string content)
    {
        if (content is null) return false;
        var text = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        return McTalkReader.IsBareCall(text);
    }

    /// <summary>
    /// Whether the message speaks to someone (« tu », « toi », « t'es »…). A sad message that does is
    /// not let through a mean reading: « j'en ai marre de toi, t'es nulle » is an insult.
    /// </summary>
    public static bool SpeaksToSomeone(string content) =>
        !string.IsNullOrWhiteSpace(content) && TokenizeOrdered(_mentionMarkup.Replace(content, " ")).Any(_secondPerson.Contains);

    /// <summary>
    /// What a Request asks her to do, or null when the message isn't one. A request marker
    /// (« tu peux », « stp »…) not cancelled by a negator (« tu peux pas »), or an imperative
    /// opening the message; then the first topic it names. A marker without a topic is
    /// <see cref="RequestTopic.Other"/>; an imperative without one (« fais gaffe ») is no request.
    /// </summary>
    public static RequestTopic? ReadRequestTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        return ReadRequestTopic(TokenizeOrdered(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ")));
    }

    private static RequestTopic? ReadRequestTopic(List<string> tokens)
    {
        var marker = FindPhrase(tokens, _requestMarkers, cancelAfter: true, cancelBefore: false, notAfter: _notRequestAfter);
        var imperative = tokens.Count > 0 && _requestImperatives.Contains(tokens[0]);
        if (!marker && !imperative) return null;

        var topic = FindTopic(tokens);
        return topic != RequestTopic.Other ? topic : marker ? RequestTopic.Other : null;
    }

    /// <summary>
    /// The command topic a message names, request or not (« comment ça marche les sondages ? » is
    /// Poll), or <see cref="RequestTopic.Other"/>: what a Help answer points at.
    /// </summary>
    public static RequestTopic ReadTopic(string content) =>
        string.IsNullOrWhiteSpace(content)
            ? RequestTopic.Other
            : FindTopic(TokenizeOrdered(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ")));

    private static RequestTopic FindTopic(List<string> tokens)
    {
        foreach (var (topic, words) in _requestTopics)
            if (FindPhrase(tokens, words, cancelAfter: false, cancelBefore: false)) return topic;
        return RequestTopic.Other;
    }

    /// <summary>What an AboutHer question asks (age, birthday, home, favourites, version), or null.</summary>
    public static HerTopic? ReadHerTopic(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        return ReadHerTopic(TokenizeOrdered(text), QuestionBreaks(text));
    }

    private static HerTopic? ReadHerTopic(List<string> tokens, HashSet<int> questionBreaks)
    {
        if (IsClosingAsk(tokens, _versionPhrases, questionBreaks, _versionFollowers)) return HerTopic.Version;
        foreach (var (topic, phrases) in _aboutHerPhrases)
            if (FindPhrase(tokens, phrases, cancelAfter: false, cancelBefore: false)) return topic;

        // « ton jeu préféré », « c'est quoi ta couleur favorite ».
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!_yourWords.Contains(tokens[i])) continue;
            for (var j = i + 1; j <= Math.Min(tokens.Count - 1, i + FavouriteWindow); j++)
                if (_favouriteWords.Contains(tokens[j])) return HerTopic.Favourite;
        }

        return null;
    }

    // « à plus » / « à toute » opening the message, or closing it after a word that isn't « y »,
    // « en », « il »…
    private static bool IsShortFarewell(List<string> tokens)
    {
        if (tokens.Count < 2) return false;
        if (tokens[0] == "a" && _shortFarewells.Contains(tokens[1])) return true;

        var i = tokens.Count - 2;
        return tokens[i] == "a" && _shortFarewells.Contains(tokens[i + 1])
               && (i == 0 || !_notFarewellBefore.Contains(tokens[i - 1]));
    }

    // A first-person statement about their own life, once the opening fillers are skipped: « je »
    // or « j' » with something after it that isn't an opinion, a wish or « te » (_notSharingAfterSelf),
    // or « on » with a verb of what people do (« on a perdu », « on est sortis »).
    private static bool IsSharing(List<string> tokens)
    {
        // Fillers and openers first, so « tu sais quoi, j'ai gagné » is judged on « j'ai gagné ».
        var k = 0;
        while (k < tokens.Count)
        {
            if (_sharingFillers.Contains(tokens[k])) { k++; continue; }
            var opener = _sharingOpeners.FirstOrDefault(p => PhraseStartsAt(tokens, k, p));
            if (opener is null) break;
            k += opener.Split(' ').Length;
        }
        if (k + 1 >= tokens.Count) return false;
        if (tokens.Skip(k).Any(_notSharingWords.Contains)) return false;

        var (first, next) = (tokens[k], tokens[k + 1]);
        if (_sharingSelf.Contains(first)) return !_notSharingAfterSelf.Contains(next);
        // « on s'est fait battre » tells; « on s'appelle plus tard » proposes.
        if (first == "on" && next == "s") return k + 2 < tokens.Count && tokens[k + 2] == "est";
        if (first == "on") return _sharingUsVerbs.Contains(next);
        if (_sharingWeVerbs.Contains(first)) return true;
        // « mon avis c'est que… » is an opinion, and « c'est » describes rather than tells.
        if (!_sharingMine.Contains(first) || _notSharingMineNouns.Contains(next)) return false;
        for (var j = k + 1; j < Math.Min(tokens.Count, k + 1 + SharingMineWindow); j++)
            if (_sharingMineVerbs.Contains(tokens[j]) && tokens[j - 1] != "c") return true;
        return false;
    }

    // One of the phrases asking something of her: closing the message or its own question (« salut,
    // ça va ? tu viens ce soir ? »), or followed by one of the followers (« toi », « bien »…) — never
    // by anything else, a negator included (« ça va pas la tête », « comment tu vas faire », « t'es là
    // ce soir ? »).
    private static bool IsClosingAsk(List<string> tokens, string[] phrases, HashSet<int> questionBreaks,
        HashSet<string> followers)
    {
        foreach (var phrase in phrases)
        {
            var length = phrase.Split(' ').Length;
            for (var i = 0; i + length <= tokens.Count; i++)
            {
                if (!PhraseStartsAt(tokens, i, phrase)) continue;
                var end = i + length;
                if (end == tokens.Count || questionBreaks.Contains(end) || followers.Contains(tokens[end])) return true;
            }
        }

        return false;
    }

    // The token indexes a « ? » sits just before: « ça va ? tu viens » gives { 2 }. The « ? » is a
    // separator to TokenizeOrdered, so tokenizing each piece and counting lands on the same tokens.
    private static HashSet<int> QuestionBreaks(string text) => Breaks(text, '?');

    // The same for any end of sentence or clause: « je vais dormir, à demain » gives { 3 }.
    private static HashSet<int> SentenceBreaks(string text) => Breaks(text, '?', '!', '.', ',', ';', '…', '\n');

    private static HashSet<int> Breaks(string text, params char[] separators)
    {
        var breaks = new HashSet<int>();
        var count = 0;
        var pieces = text.Split(separators);
        for (var p = 0; p < pieces.Length - 1; p++)
        {
            count += TokenizeOrdered(pieces[p]).Count;
            breaks.Add(count);
        }

        return breaks;
    }

    // Whether the phrase closes the message.
    private static bool EndsWith(List<string> tokens, string phrase)
    {
        var length = phrase.Split(' ').Length;
        return tokens.Count >= length && PhraseStartsAt(tokens, tokens.Count - length, phrase);
    }

    // One of the phrases on adjacent tokens, not cancelled by a negator within two tokens after
    // it (cancelAfter) or three before it (cancelBefore). « plus que » after a phrase is the
    // comparative, not a negation: « je t'aime plus que tout ». A phrase right after a word of
    // notAfter, or right before a word of notFollowedBy, doesn't count either.
    private static bool FindPhrase(List<string> tokens, string[] phrases, bool cancelAfter, bool cancelBefore,
        HashSet<string>? notAfter = null, HashSet<string>? notFollowedBy = null)
    {
        foreach (var phrase in phrases)
        {
            var length = phrase.Split(' ').Length;
            for (var i = 0; i + length <= tokens.Count; i++)
            {
                if (!PhraseStartsAt(tokens, i, phrase)) continue;
                if (notAfter is not null && i > 0 && notAfter.Contains(tokens[i - 1])) continue;
                if (notFollowedBy is not null && i + length < tokens.Count && notFollowedBy.Contains(tokens[i + length])) continue;

                var end = i + length;
                var negatedAfter = cancelAfter && Enumerable.Range(end, Math.Min(ForwardNegationWindow, tokens.Count - end))
                    .Any(j => IsNegator(tokens[j]) && !(tokens[j] == "plus" && j + 1 < tokens.Count && tokens[j + 1] == "que"));
                var negatedBefore = cancelBefore && Enumerable.Range(Math.Max(0, i - NegationWindow), i - Math.Max(0, i - NegationWindow))
                    .Any(j => IsNegator(tokens[j]));
                if (!negatedAfter && !negatedBefore) return true;
            }
        }

        return false;
    }

    // True when the message calls the bot by the wrong name "Inabot" — written as
    // one word ("inabot"), split in two ("ina bot"), or spelled out letter by
    // letter ("I.N.A.B.O.T", "I N A B O T", "i-n-a-b-o-t"). The bot is SYNCS, and
    // it takes offence at being mistaken for Inabot.
    public static bool IsMistakenIdentity(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;

        var words = TokenizeOrdered(content);
        // A spelled-out name tokenizes into isolated letters (the dots and dashes
        // are dropped as separators), so try again with those runs glued back
        // together. Gluing only runs of single letters keeps innocent text safe:
        // "domina bottes" has no such run, and "in a bot" starts with a two-letter
        // token, so neither collapses into the name.
        //
        // Each form is also tried with repeated letters squashed, so drawing the
        // name out ("innnabot") doesn't smuggle it past her. "nabot" — which is a
        // real word, and one of Amandine's comebacks — squashes to itself and stays
        // safe, since the test needs "ina" before "bot".
        var squashed = words.Select(Squash).ToList();

        return NamesInabot(words) || NamesInabot(GlueSpelledOutRuns(words))
            || NamesInabot(squashed) || NamesInabot(GlueSpelledOutRuns(squashed));
    }

    // The name test itself: one word, or "ina" somewhere before "bot".
    private static bool NamesInabot(List<string> words)
    {
        if (words.Contains("inabot")) return true;
        int ina = words.IndexOf("ina");
        return ina >= 0 && words.IndexOf("bot") > ina;
    }

    // Merges every run of consecutive single-letter tokens into one word,
    // preserving the order of everything else: ["i","n","a","bot"] -> ["ina","bot"].
    private static List<string> GlueSpelledOutRuns(List<string> words)
    {
        var glued = new List<string>(words.Count);
        var run = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length == 1)
            {
                run.Append(word);
                continue;
            }

            if (run.Length > 0)
            {
                glued.Add(run.ToString());
                run.Clear();
            }
            glued.Add(word);
        }

        if (run.Length > 0) glued.Add(run.ToString());
        return glued;
    }

    // Splits text into an ordered list of lowercase, accent-stripped words,
    // preserving position for cues that depend on word order. The ligatures are spelled out, since
    // they have no decomposition: « cœur » reads as the cue "coeur".
    private static List<string> TokenizeOrdered(string content)
    {
        var sb = new StringBuilder(content.Length);
        foreach (var ch in content.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (ch == 'œ') { sb.Append("oe"); continue; }
            if (ch == 'æ') { sb.Append("ae"); continue; }
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        return sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}
