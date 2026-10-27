using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Reading an intent: ReadIntent, ReadAnswer and the tests they lean on.
internal static partial class MessageCues
{
    /// <summary>
    /// What a message aimed at her is doing. First match wins, in the order of
    /// <see cref="ChatIntent"/>. A negator just after a declaration or a « ça va » cancels it
    /// (« je t'aime pas », « ça va pas la tête ? »), and just before an apology (« pas désolé »).
    /// </summary>
    public static ChatIntent ReadIntent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return ChatIntent.None;

        // Reported speech is someone else's words (« il a écrit « je t'aime » à sa copine »): quotes are
        // dropped when something is said outside them. A message that is only a quote keeps it.
        var unquoted = _quotedSpeech.Replace(content, " ");
        if (TokenizeOrdered(_mentionMarkup.Replace(unquoted, " ")).Count > 0) content = unquoted;

        var unaddressed = _aPlus.Replace(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " "), " a plus ");
        // « syncs ? » alone is checked on the name itself, before the address is dropped below.
        var nameAlone = TokenizeOrdered(unaddressed) is { Count: > 0 } named && named.All(w => _selfNames.Contains(Squash(w)));
        // Her name as an address (« syncs, organise une session », « merci syncs ») is dropped, so the
        // rules that look at the first or last word see the message itself.
        var stripped = _trailingAddress.Replace(_leadingAddress.Replace(unaddressed, " "), "");
        // Her name alone, unasked, with nothing else (not even « 😂 »), is her name said: the
        // talked-about line, not a reply to read.
        if (nameAlone && !unaddressed.Contains('?') && stripped.Trim(' ', ',', '.', '!', ':').Length == 0)
            return ChatIntent.None;
        var t = TokenizeOrdered(stripped);
        var asks = stripped.Contains('?');

        // « Pardon ? » alone is "what did you say", not an apology.
        if (t.Count == 1 && t[0] == "pardon" && asks) return ChatIntent.Question;

        if (FindPhrase(t, _lovePhrases, cancelAfter: true, cancelBefore: true)) return ChatIntent.Love;
        var breaks = QuestionBreaks(stripped);
        // Before the apology: « dis pardon » asks for one, it doesn't give one.
        if (FindPhrase(t, _demandApologyPhrases, cancelAfter: false, cancelBefore: true, notAfter: _notDemandAfter)
            || (asks && IsClosingAsk(t, _demandApologyAsked, breaks, _noWords)))
            return ChatIntent.DemandApology;
        if (FindPhrase(t, _apologyPhrases, cancelAfter: false, cancelBefore: true)
            || (t.Count <= MaxShortApologyTokens && FindPhrase(t, _apologyShortWords, cancelAfter: false, cancelBefore: false)))
            return ChatIntent.Apology;
        if (IsShutUp(t)) return ChatIntent.ShutUp;
        if (IsClosingAsk(t, _doYouLoveMePhrases, breaks, _lovesMeFollowers)) return ChatIntent.DoYouLoveMe;
        // « comment tu vas » needs no « ? » to be a question; « cv », « tranquille »… count opening a
        // short one (« cv et toi ? »).
        var isOpener = t.Count is > 0 and <= MaxHowAreYouOpenerTokens && _howAreYouOpeners.Contains(t[0])
                       && t.Skip(1).All(_askingFollowers.Contains);
        if ((asks || (t.Count > 0 && t[0] == "comment"))
            && (IsClosingAsk(t, _howAreYouPhrases, breaks, _askingFollowers) || isOpener))
            return ChatIntent.HowAreYou;
        // « t'es là ? », « réveille-toi », or her name alone with a « ? » (« syncs ? »).
        if (IsClosingAsk(t, _areYouTherePhrases, breaks, _thereFollowers)
            || (t.Count <= MaxWakeUpTokens && FindPhrase(t, _wakeUpPhrases, cancelAfter: false, cancelBefore: false))
            || (asks && nameAlone)
            || (t.Count > 0 && Squash(t[0]) == "alo" && t.Skip(1).All(_alloFollowers.Contains)))
            return ChatIntent.AreYouThere;
        if (IsAnswerMe(t, breaks)) return ChatIntent.AnswerMe;
        if (FindPhrase(t, _whoAreYouPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.WhoAreYou;
        if (IsClosingAsk(t, _rememberMePhrases, breaks, _rememberFollowers)
            || (asks && IsClosingAsk(t, _rememberMeAskedPhrases, breaks, _rememberFollowers)))
            return ChatIntent.RememberMe;
        // Theirs, not hers: « mon niveau ? » before AboutHer's « ton … préféré ».
        if (ReadMyStatsTopic(t, asks, breaks) is not null) return ChatIntent.MyStats;
        if (ReadHerTopic(t, breaks) is not null) return ChatIntent.AboutHer;
        if (FindPhrase(t, _areYouABotPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.AreYouABot;
        if (asks && IsClosingAsk(t, _areYouUpsetPhrases, breaks, _upsetFollowers)) return ChatIntent.AreYouUpset;
        if (FindPhrase(t, _crushPhrases, cancelAfter: false, cancelBefore: false)
            || IsClosingAsk(t, _crushClosingPhrases, breaks, _crushFollowers))
            return ChatIntent.Crush;
        if (FindPhrase(t, _whatAreYouDoingPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.WhatAreYouDoing;
        // On the raw text: « t'en penses quoi de @Luca ? » keeps its subject. Ship first: « tu penses
        // quoi de @A et @B ? » asks about a pair of people, not about the word « et ».
        if (ShipAsk.Read(content) is not null) return ChatIntent.Ship;
        // Before the opinion: « qui a raison, moi ou Luca ? » sides with one of two people.
        if (TakeSides.Read(content) is not null) return ChatIntent.TakeSides;
        if (Opinion.Read(content) is not null) return ChatIntent.Opinion;
        if (IsRateMe(t, asks, breaks)) return ChatIntent.RateMe;
        if (FindPhrase(t, _blamePhrases, cancelAfter: false, cancelBefore: false) && (asks || _blamePhrases.Any(p => EndsWith(t, p))))
            return ChatIntent.Blame;
        // Before ShouldI (« on joue ce soir ou pas ? ») and Request (« tu peux me dire c'est quand la
        // prochaine session ? »): the schedule answers both.
        if ((asks || (t.Count > 0 && _nextSessionOpeners.Contains(t[0])))
            && FindPhrase(t, _nextSessionPhrases, cancelAfter: false, cancelBefore: false))
            return ChatIntent.NextSession;
        if (IsShouldI(t, stripped, asks, breaks)) return ChatIntent.ShouldI;
        // Before the request: « tu peux lancer un dé ? » wants the roll.
        if (Dice.Read(stripped) is not null) return ChatIntent.Roll;
        // Before the request: « tu peux me raconter une blague ? » wants the joke, not a command.
        if (FindPhrase(t, _jokePhrases, cancelAfter: false, cancelBefore: true) || _jokeAlone.Contains(string.Join(' ', t)))
            return ChatIntent.Joke;
        if (FindPhrase(t, _funFactPhrases, cancelAfter: false, cancelBefore: true)
            || IsClosingAsk(t, _funFactClosingPhrases, breaks, _funFactFollowers))
            return ChatIntent.FunFact;
        // A request naming a topic is a request (« tu peux m'aider à lancer un sondage ? »); without
        // one, asking for help is Help (« tu peux m'aider ? »), and anything else asked is a request.
        var topic = ReadRequestTopic(t);
        if (topic is not null and not RequestTopic.Other) return ChatIntent.Request;
        if (FindPhrase(t, _helpPhrases, cancelAfter: true, cancelBefore: true)
            || (t.Count <= MaxHelpClosingTokens && _helpClosingPhrases.Any(p => EndsWith(t, p)))
            || (t.Any(_featureWords.Contains) && FindPhrase(t, _howToPhrases, cancelAfter: false, cancelBefore: true)))
            return ChatIntent.Help;
        if (topic is not null) return ChatIntent.Request;
        var sentenceBreaks = SentenceBreaks(stripped);
        // Before boredom and the farewells: « journée de merde, je vais dormir » is the day, said.
        if (IsDown(t, stripped, content, asks, sentenceBreaks)) return ChatIntent.Down;
        if (t.Count <= MaxBoredTokens
            && (FindPhrase(t, _boredPhrases, cancelAfter: true, cancelBefore: true, notFollowedBy: _missingWords)
                || IsClosingAsk(t, _nothingToDoPhrases, sentenceBreaks, _nothingToDoFollowers)
                || IsClosingAsk(t, _boredClosingPhrases, breaks, _noWords)))
            return ChatIntent.Bored;
        if (t.Count <= MaxFarewellTokens)
        {
            if (FindPhrase(t, _goodnightPhrases, cancelAfter: false, cancelBefore: false, notAfter: _notGoodnightAfter)
                || IsClosingAsk(t, _sleepPhrases, sentenceBreaks, _sleepFollowers))
                return ChatIntent.Goodnight;
            if (IsShortFarewell(t) || FindPhrase(t, _goodbyePhrases, cancelAfter: false, cancelBefore: false)
                || _closingGoodbyePhrases.Any(p => EndsWith(t, p)))
                return ChatIntent.Goodbye;
        }

        // Before agreement and disagreement: « mais oui c'est ça » agrees with nothing, and « mais
        // non, fais genre » is teasing, not a correction.
        if (t.Count <= MaxDisbeliefTokens
            && (FindPhrase(t, _disbeliefPhrases, cancelAfter: false, cancelBefore: false, notAfter: _notDisbeliefAfter)
                || FindPhrase(t, _liarPhrases, cancelAfter: true, cancelBefore: false, notAfter: _notDisbeliefAfter)
                || ((t.Count <= 2 || t.Any(_secondPerson.Contains))
                    && FindPhrase(t, _liarWords, cancelAfter: false, cancelBefore: true))
                || (asks && IsClosingAsk(t, _sureAsks, breaks, _doubtFollowers))
                || (asks && t.Count > 0 && _seriousWords.Contains(Squash(t[0])) && t.Skip(1).All(w => _doubtFollowers.Contains(w) || w == "quoi"))
                || IsClosingAsk(t, _doubtPhrases, sentenceBreaks, _doubtFollowers)))
            return ChatIntent.Disbelief;

        if (t.Count <= MaxMaybeTokens
            && (FindPhrase(t, _maybePhrases, cancelAfter: false, cancelBefore: false)
                || FindPhrase(t, _maybeNegatablePhrases, cancelAfter: false, cancelBefore: true)))
            return ChatIntent.Maybe;

        if ((t.Count <= MaxDisagreementTokens && FindPhrase(t, _youAreRightPhrases, cancelAfter: false, cancelBefore: false))
            || (!asks && t.Count <= MaxDisagreementTokens && FindPhrase(t, _cestVrai, cancelAfter: false, cancelBefore: false))
            || (!asks && t.Count <= MaxAgreementTokens
                && t.Any(w => _youAreRightWords.Contains(w) || _youAreRightWords.Contains(Squash(w)))))
            return ChatIntent.YouAreRight;

        if (t.Count <= MaxDisagreementTokens
            && (FindPhrase(t, _youAreWrongPhrases, cancelAfter: false, cancelBefore: false)
                || (t.Count <= MaxShortDisagreementTokens && FindPhrase(t, _youAreWrongShortPhrases, cancelAfter: false, cancelBefore: false))
                || (t.Count <= MaxAgreementTokens && FindPhrase(t, _youAreWrongWords, cancelAfter: false, cancelBefore: true))))
            return ChatIntent.YouAreWrong;

        if (FindPhrase(t, _thanksPhrases, cancelAfter: false, cancelBefore: true, notAfter: _notThanksAfter)
            || (t.Count <= MaxShortThanksTokens && FindPhrase(t, _thanksShortWords, cancelAfter: false, cancelBefore: false)))
            return ChatIntent.Thanks;

        if (t.Count <= MaxLaughterTokens && IsLaughing(t, stripped, content)) return ChatIntent.Laughter;

        if (asks && t.Count >= 2) return ChatIntent.Question;
        if (t.Count >= MinUnmarkedQuestionTokens
            && (_questionOpeners.Contains(t[0])
                || (t[0] == "ou" && !_notQuestionAfterOu.Contains(t[1]))
                || _questionOpenerPhrases.Any(p => PhraseStartsAt(t, 0, p))))
            return ChatIntent.Question;

        if (!asks && t.Count is >= MinSharingTokens and <= MaxSharingTokens && IsSharing(t)) return ChatIntent.Sharing;

        if (t.Count == 0 || (t.Count <= MaxLowEffortTokens
                             && t.All(w => _lowEffortWords.Contains(w) || _lowEffortWords.Contains(Squash(w)))))
            return ChatIntent.LowEffort;

        return ChatIntent.None;
    }

    /// <summary>
    /// <see cref="ChatIntent.Yes"/> or <see cref="ChatIntent.No"/> when the whole message is a plain
    /// answer (« oui », « bah non », « carrément », « pas du tout »), else null. Never asked
    /// (« oui ? » is someone answering a call), and never mixed (« oui et non », « oui mais demain »).
    /// The caller decides whether there was a question to answer.
    /// </summary>
    public static ChatIntent? ReadAnswer(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var stripped = _trailingAddress.Replace(
            _leadingAddress.Replace(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " "), " "), "");
        if (stripped.Contains('?')) return null;

        var t = TokenizeOrdered(stripped);
        if (t.Count is 0 or > MaxAnswerTokens) return null;
        var words = new List<string>();
        foreach (var w in t.Select(Squash))
            if (!_answerFillers.Contains(w) && (words.Count == 0 || words[^1] != w)) words.Add(w);

        var said = string.Join(' ', words);
        if (_yesAnswers.Contains(said)) return ChatIntent.Yes;
        if (_noAnswers.Contains(said)) return ChatIntent.No;
        return null;
    }

    /// <summary>
    /// A short answer to an open question (« Java », « 8 jours », « ça s'appelle comme ça »), as written
    /// without mentions, emotes, links, her name or closing punctuation; else null. One to
    /// <see cref="MaxShortAnswerTokens"/> words, not asked, and not about her (« t'es nulle » speaks to
    /// her, it doesn't answer). The caller checks there was a question and that nothing else read it.
    /// </summary>
    public static string? ReadShortAnswer(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Contains('?')) return null;
        var text = _trailingAddress.Replace(
            _leadingAddress.Replace(_link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " "), " "), "");
        var t = TokenizeOrdered(text);
        if (t.Count is 0 or > MaxShortAnswerTokens || t.Any(_secondPerson.Contains)) return null;
        text = _whitespaceRun.Replace(text, " ").Trim().TrimEnd('.', '!', '…', ',', ';', ':', ' ');
        return text.Length is 0 or > MaxShortAnswerLength ? null : text;
    }

    private const int MaxShortAnswerTokens = 5;
    private const int MaxShortAnswerLength = 60;
    private static readonly Regex _whitespaceRun = new(@"\s+", RegexOptions.Compiled);

    // A laugh anywhere: a laughter word or run (« mdr », « hahaha »), a phrase, an emoji, GigaLaugh.
    private static bool IsLaughing(List<string> t, string stripped, string content) =>
        t.Any(w => _laughterWords.Contains(w) || _laughterWords.Contains(Shorten(w))
                   || _laughterWords.Contains(Squash(w))
                   || (Squash(w).Length >= 4 && _laughRun.IsMatch(Squash(w))))
        || FindPhrase(t, _laughterPhrases, cancelAfter: false, cancelBefore: true)
        || _laughterSymbols.Any(stripped.Contains)
        || _laughterEmoteIds.Any(content.Contains);

    // Feeling down, about themselves (see _downSubjects and the lists after it). Not while laughing.
    private static bool IsDown(List<string> t, string stripped, string content, bool asks, HashSet<int> sentenceBreaks)
    {
        if (IsLaughing(t, stripped, content) || t.Contains("rire")) return false;
        if (FindPhrase(t, _downPhrases, cancelAfter: false, cancelBefore: true)) return true;
        if (!asks && (FindPhrase(t, _downDayPhrases, cancelAfter: false, cancelBefore: true)
                      || IsClosingAsk(t, _downClosingPhrases, sentenceBreaks, _downFollowers)))
            return true;

        foreach (var subject in _downSubjects)
        {
            var length = subject.Split(' ').Length;
            for (var i = 0; i + length < t.Count; i++)
            {
                if (!PhraseStartsAt(t, i, subject)) continue;
                var j = i + length;
                var skipped = 0;
                while (j < t.Count && skipped < 2 && _downIntensifiers.Contains(t[j])) (j, skipped) = (j + 1, skipped + 1);
                if (j < t.Count && _downAdjectives.Contains(t[j]) && IsDownAdjective(t, j)) return true;
            }
        }

        return false;
    }

    // Words that only read as feeling down closing the message or before one of _downAdjectiveFollowers:
    // « je me sens mal », not « je suis mal placé »; « je suis seul », not « je suis seul en vocal ».
    // « bout » and « plat » only after « à » / « au » (« je suis à bout », « à plat »).
    private static readonly HashSet<string> _closingDownAdjectives = new() { "mal", "seul", "seule", "vide" };
    private static readonly HashSet<string> _downAdjectiveFollowers = new()
    {
        "aujourd", "syncs", "sync", "hein", "depuis", "moi", "ce", "en", "la",
    };

    private static bool IsDownAdjective(List<string> t, int j)
    {
        if (t[j] is "bout" or "plat") return j > 0 && t[j - 1] is "a" or "au";
        if (!_closingDownAdjectives.Contains(t[j])) return true;
        if (j + 1 == t.Count) return true;
        // « en » and « la » only as « en ce moment » / « là », not « seul en vocal ».
        var next = t[j + 1];
        if (next == "en") return j + 2 < t.Count && t[j + 2] == "ce";
        if (next == "la") return j + 2 == t.Count;
        return _downAdjectiveFollowers.Contains(next);
    }
}
