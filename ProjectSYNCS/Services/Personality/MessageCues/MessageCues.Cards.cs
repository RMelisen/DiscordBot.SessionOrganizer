namespace ProjectSYNCS.Services;

// What a reply under one of her cards (a poll, a vote, a session, /tonight, a giveaway) is doing.
// Stored by name as `/debug answers` keys (AnswerKinds.Card): reorder freely, never rename.
internal enum CardReplyIntent
{
    None,
    // « remonte le sondage », « up », « remets-le en bas »: she brings the card back down (CardRepost).
    Repost,
    // « votez ! », « @Luca vote », « @Luca t'as voté ? », a bare tag: rallying the others, she backs them up.
    Rally,
    // « j'ai voté », « c'est fait », « je me suis inscrit »: checked against the card.
    Done,
    // « c'est quand ? », « qui vient ? », « on en est où ? »: answered from the card.
    Status,
    // « samedi pour moi », « je peux pas », « chaud »: an answer typed instead of clicked.
    TextAnswer,
}

// Which card « syncs remonte le sondage » names, said with no card replied to (MessageCues.ReadNamedRepost).
internal enum NamedCard
{
    None,
    // « la carte », « remonte tout »: whatever is open.
    Any,
    // « le sondage », « le vote »: /when and /vote alike.
    Poll,
    // « la session », « le tonight »: a session or /tonight.
    Session,
    // « le tirage », « le giveaway ».
    Giveaway,
}

// The card named, and whether she brings back every open one of it (« remonte tous les sondages »)
// or only the newest (« remonte le sondage »).
internal readonly record struct NamedRepost(NamedCard Card, bool All)
{
    public static readonly NamedRepost None = new(NamedCard.None, false);
}

internal static partial class MessageCues
{
    // The card named outside a reply. Only nouns that say which card: once no reply points at it,
    // « remets le message » or « remonte le truc » could be anything.
    private static readonly Dictionary<string, NamedCard> _namedCards = new()
    {
        ["sondage"] = NamedCard.Poll, ["sondages"] = NamedCard.Poll, ["vote"] = NamedCard.Poll, ["votes"] = NamedCard.Poll,
        ["poll"] = NamedCard.Poll, ["polls"] = NamedCard.Poll,
        ["session"] = NamedCard.Session, ["sessions"] = NamedCard.Session, ["tonight"] = NamedCard.Session,
        ["event"] = NamedCard.Session, ["events"] = NamedCard.Session, ["evenement"] = NamedCard.Session,
        ["evenements"] = NamedCard.Session,
        ["tirage"] = NamedCard.Giveaway, ["tirages"] = NamedCard.Giveaway, ["giveaway"] = NamedCard.Giveaway,
        ["giveaways"] = NamedCard.Giveaway, ["concours"] = NamedCard.Giveaway,
        ["carte"] = NamedCard.Any, ["cartes"] = NamedCard.Any,
    };

    // The plural says every one of them: « remonte les sondages ». « concours » is both, so read singular.
    private static readonly HashSet<string> _pluralCardNouns = new()
    {
        "sondages", "votes", "polls", "sessions", "events", "evenements", "tirages", "giveaways", "cartes",
    };

    // « remonte tout », « remonte-les tous », « tu peux tout remonter »: next to the verb.
    private static readonly HashSet<string> _allWords = new() { "tout", "tous", "toutes" };

    // … but not as a determiner or an adverb: « remonte tout le monde », « remets-le tout en bas ».
    private static readonly HashSet<string> _notAllBefore = new()
    {
        "le", "la", "les", "l", "un", "une", "de", "du", "des", "d", "en", "au", "aux", "a", "droit", "suite", "seul",
        "seule", "pres", "haut", "bas", "mon", "ton", "son", "ma", "ta", "sa", "mes", "tes", "ses", "nos", "vos",
    };

    /// <summary>
    /// « syncs remonte le sondage », with no card replied to: a repost as <see cref="ReadCardReply"/>
    /// reads one, with the card named, so she knows which to look for; <see cref="NamedRepost.All"/>
    /// when it asks for every one (« tous les sondages », « les sessions », « remonte tout »).
    /// <see cref="NamedRepost.None"/> otherwise: « remonte-le » points at nothing outside a reply.
    /// </summary>
    public static NamedRepost ReadNamedRepost(string content)
    {
        if (ReadCardReply(content, tagsSomeone: false) != CardReplyIntent.Repost) return NamedRepost.None;
        var t = TokenizeOrdered(_mentionMarkup.Replace(content, " "));

        var all = SaysAll(t);
        for (var i = 0; i < t.Count; i++)
            if (_namedCards.TryGetValue(t[i], out var named) && (i == 0 || !_possessives.Contains(t[i - 1])))
                return new NamedRepost(named, all || _pluralCardNouns.Contains(t[i]));
        return all ? new NamedRepost(NamedCard.Any, true) : NamedRepost.None;
    }

    // « tout » / « tous » / « toutes » within two words of a repost verb, as its object.
    private static bool SaysAll(List<string> t)
    {
        for (var i = 0; i < t.Count; i++)
        {
            if (!_allWords.Contains(t[i]) || (i + 1 < t.Count && _notAllBefore.Contains(t[i + 1]))) continue;
            for (var j = Math.Max(0, i - 2); j <= Math.Min(t.Count - 1, i + 2); j++)
                if (j != i && IsRepostVerb(t[j])) return true;
        }
        return false;
    }

    private static bool IsRepostVerb(string w) =>
        _repostVerbs.Contains(w) || _repostObjectVerbs.Contains(w) || _placeVerbs.Contains(w);

    // A card reply is short: past this it is a conversation, read the usual way.
    private const int MaxCardReplyTokens = 16;
    private const int MaxTextAnswerTokens = 10;
    private const int MaxUpTokens = 3;
    // « @Luca chaud ? »: a short question to someone tagged, under a card, is about the card.
    private const int MaxTaggedAskTokens = 4;
    private const int MaxStatusOpenerTokens = 6;

    // The card itself, named: « remets le sondage », « renvoie la carte », « remets le vote ». Every
    // named noun, plus the vague ones a reply makes clear. _namedCards is above, in this file.
    private static readonly HashSet<string> _cardNouns = new(_namedCards.Keys.Concat(["message", "post", "truc"]));

    private static readonly HashSet<string> _possessives = new()
    {
        "mon", "ton", "son", "ma", "ta", "sa", "mes", "tes", "ses", "notre", "votre", "nos", "vos", "leur", "leurs",
    };

    // An object pronoun: « remets-le », « tu peux le renvoyer ».
    private static readonly HashSet<string> _cardPronouns = new() { "le", "la", "l", "les", "ca" };

    // Bringing it back, on their own: « remonte-le », « tu peux le republier ? ». A negator after
    // cancels (« le remonte pas »), not before (« tu peux pas le remonter ? » asks). Never after a
    // subject (« je remonte », « on remonte la pente »).
    private static readonly string[] _repostVerbs =
    {
        "remonte", "remonter", "remontes", "remontez", "republie", "republier", "republies", "republiez",
        "reposte", "reposter", "repostes", "repostez", "repost", "bump", "bumper", "reup",
        // « re-poste » splits in two.
        "re poste", "re poster", "re publie", "re publier",
    };

    // The same, only about the card (a pronoun next to them, the card named, or closing the reply):
    // « ramène des chips », « renvoie-moi le lien » are something else.
    private static readonly string[] _repostObjectVerbs =
    {
        "renvoie", "renvoi", "renvoyer", "renvoies", "renvoyez", "ramene", "ramener", "ramenes", "ramenez", "remets",
        "remet", "remettre", "remettez", "rebalance", "rebalancer", "rebalances", "rebalancez", "redescends",
        "redescend", "redescendre", "redescendez",
    };

    private static readonly HashSet<string> _repostSubjects = new() { "je", "j", "on", "il", "elle", "ils", "elles", "nous" };

    // Putting it somewhere, which is a repost only with the place: « mets-le en bas », « poste-le à la fin ».
    private static readonly string[] _placeVerbs =
    {
        "mets", "met", "mettre", "mettez", "remets", "remet", "remettre", "remettez", "poste", "poster", "repasse", "repasser",
    };

    private static readonly string[] _bottomPhrases = { "en bas", "a la fin", "en dernier", "plus bas", "tout en bas" };

    // « on le voit plus »: it got buried. Said of « le » / « la », so only of the card.
    private static readonly string[] _buriedPhrases =
    {
        "on le voit plus", "on la voit plus", "on le voit pas", "on la voit pas", "on le trouve plus", "on la trouve plus",
    };

    // The same with the card named (« le sondage est tout en haut », « la carte est noyée »).
    private static readonly string[] _buriedNamedPhrases =
    {
        "perdu", "perdue", "noye", "noyee", "enterre", "enterree", "tout en haut", "trop haut", "on le voit plus",
    };

    // … said of the card, not of someone: « je suis perdu avec ce sondage » asks for help.
    private static readonly HashSet<string> _notBuriedAfter = new()
    {
        "suis", "jsuis", "chui", "es", "etes", "sommes", "me", "m", "te", "se",
    };

    // A repost verb right after its subject is someone doing it (« tu ramènes des chips ? »), and
    // after an auxiliary it is done already (« je l'ai remonté », « syncs a remonté le sondage »,
    // « le sondage a été remonté »: « remonté » reads « remonte »).
    private static readonly HashSet<string> _repostObjectSubjects = new()
    {
        "je", "j", "on", "il", "elle", "ils", "elles", "nous", "tu", "t",
        "ai", "jai", "a", "as", "avez", "avons", "ont", "deja", "ete", "est", "sont", "etait",
    };

    // Rallying everyone, said to the room: anywhere in the reply.
    private static readonly string[] _rallyPluralPhrases =
    {
        "votez", "allez voter", "faut voter", "venez voter", "go voter", "go vote", "pensez a voter",
        "oubliez pas de voter", "inscrivez vous", "repondez", "cliquez", "participez", "venez",
        "tout le monde vote", "tout le monde clique", "vos votes", "faut repondre", "faut s inscrire", "allez on vote",
        "bougez vous", "depechez vous", "personne a vote", "personne vote", "personne a repondu", "personne repond",
        "manque des votes", "il manque du monde", "il manque des gens", "et vous",
    };

    // Rallying one person: only with someone tagged (« @Luca vote », « @Luca t'as voté ? »), and the
    // imperatives never as a noun or a past (« le vote », « il a voté »: « voté » reads « vote »).
    private static readonly string[] _rallySingularPhrases =
    {
        "vote", "votes", "va voter", "viens", "inscris toi", "reponds", "clique", "participe", "pense a voter",
        "oublie pas de voter", "et toi",
    };

    // Asking the person tagged whether they did it: « @Luca t'as voté ? », « @Luca tu viens ? ».
    private static readonly string[] _rallyAskPhrases =
    {
        "t as vote", "tas vote", "tu as vote", "t as clique", "tas clique", "t as repondu", "tas repondu", "tu viens",
        "tu votes", "tu participes", "t es chaud", "t es chaude", "tes chaud", "tes chaude", "t es dispo", "tes dispo",
    };

    private static readonly HashSet<string> _notRallyAfter = new()
    {
        "j", "je", "jai", "ai", "a", "as", "avez", "avons", "ont", "deja", "pas", "il", "elle", "on", "moi",
        "le", "la", "les", "un", "une", "du", "des", "de", "mon", "ton", "son", "ma", "ta", "sa", "mes", "tes",
        "ses", "nos", "vos", "leur", "leurs", "ce", "mais", "qui",
    };

    // Saying it is done. The subject is in the phrase, so « j'ai pas voté » never matches.
    private static readonly string[] _donePhrases =
    {
        "j ai vote", "jai vote", "j ai deja vote", "jai deja vote", "c est fait", "cest fait", "c est deja fait",
        "j ai clique", "jai clique", "j ai repondu", "jai repondu", "je me suis inscrit", "je me suis inscrite",
        "jme suis inscrit", "jme suis inscrite", "je suis inscrit", "je suis inscrite", "jsuis inscrit",
        "jsuis inscrite", "j ai participe", "jai participe", "je participe", "j ai rejoint", "jai rejoint",
        "c est vote", "cest vote", "j ai coche", "jai coche", "on a vote", "on a tous vote",
    };

    // The same, as the whole reply: « voté ! », « a voté », « fait », « done », « c'est bon ».
    private static readonly HashSet<string> _doneAlone = new()
    {
        "vote", "a vote", "deja vote", "fait", "done", "inscrit", "inscrite", "deja fait", "c est bon", "cest bon",
        "voila", "check",
    };

    // A check mark alone says it too.
    private static readonly string[] _doneMarks = { "✅", "☑️", "✔️" };

    // Asking about the card: when, who, how many, how it stands. Each names the card's own business:
    // a bare « quand », « combien » or « quelle heure » may be about anything (« il est quelle heure ? »).
    private static readonly string[] _cardStatusPhrases =
    {
        "c est quand", "cest quand", "c est pour quand", "ca commence quand", "on commence quand", "on joue quand",
        "ca finit quand", "ca se termine quand", "ca ferme quand", "jusqu a quand", "c est a quelle heure",
        "ca commence a quelle heure", "on commence a quelle heure", "on joue a quelle heure", "a quelle heure on",
        "quel jour", "qui vient", "qui viens", "qui est chaud", "qui est chaude", "qui est la", "qui participe", "qui a vote",
        "qui a gagne", "qui gagne", "qui est inscrit", "qui est dispo", "y a qui", "on est combien", "combien on est",
        "y a combien", "combien de votes", "combien de votants", "combien d inscrits", "combien de participants",
        "combien de personnes", "combien de chauds", "on en est ou", "ca en est ou", "resultat", "resultats",
        "ca donne quoi", "en tete", "qui est en tete", "on part sur quoi", "on fait quoi finalement",
    };

    // The same, unasked, opening a short reply (« c'est quand », « qui vient »). Not « résultat » or
    // « en tête », which open sentences (« résultat, je suis mort »).
    private static readonly string[] _cardStatusOpeners =
    {
        "c est quand", "cest quand", "c est pour quand", "ca commence quand", "on commence quand", "on joue quand",
        "c est a quelle heure", "qui vient", "qui est chaud", "qui est chaude", "qui est dispo", "qui participe",
        "qui a vote", "on est combien", "on en est ou", "y a qui",
    };

    // Asked as the whole reply: « alors ? », « et alors ? ».
    private static readonly HashSet<string> _statusAlone = new() { "alors", "et alors", "alors alors", "du coup" };

    // An answer typed in the chat instead of clicked. No « ? »: « je peux voter ? » asks.
    private static readonly string[] _textAnswerPhrases =
    {
        "pour moi", "dispo", "dispos", "je peux", "jpeux", "je pourrai", "je viens", "je viendrai", "je serai la",
        "je suis la", "j y serai", "jy serai", "present", "presente", "partant", "partante", "je vote", "je prends",
        "absent", "absente", "je passe", "sans moi", "pas ce soir", "count me in", "moi aussi", "compte sur moi",
        "comptez sur moi", "ca me va", "je suis in", "je suis out", "chui dispo", "chui pas dispo",
    };

    // « je peux voter / cliquer… » is asking permission, not answering.
    private static readonly HashSet<string> _notAnsweringAfterCan = new()
    {
        "voter", "cliquer", "participer", "m", "changer", "annuler", "inviter",
    };

    // « chaud » is keen, except as the weather or « tough » (« il fait chaud », « c'est chaud »).
    private static readonly HashSet<string> _notKeenAfter = new() { "fait", "est", "etre", "c", "cest" };

    // The whole reply is one of these: agreeing with a choice someone typed, or « +1 ».
    private static readonly HashSet<string> _textAnswerAlone = new()
    {
        "moi", "moi moi", "1", "pareil", "idem", "same", "in", "out", "go", "go go", "go go go", "partant", "chaud chaud",
    };

    // A day or an evening picked in text: « samedi ! », « plutôt dimanche », « samedi ou dimanche ».
    // Only with the words around a choice, so a day said in passing (« je dors chez Luca ce soir »,
    // « à demain ») is not a vote.
    private static readonly HashSet<string> _dayWords = new()
    {
        "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi", "dimanche", "demain", "soir", "weekend",
    };

    private static readonly HashSet<string> _dayChoiceWords = new()
    {
        "plutot", "moi", "perso", "ok", "oui", "non", "et", "ou", "le", "ce", "go", "top", "parfait", "nickel", "aussi",
        "sinon", "mais", "pas", "je", "vote", "prefere", "dispo", "pour", "c", "est", "mieux", "bien", "ca", "va", "me",
    };

    /// <summary>
    /// What a reply under one of her cards is doing, or None to read it the usual way. First match
    /// wins: repost, rally, done, status, a short question to someone tagged (a rally), then a typed
    /// answer. <paramref name="tagsSomeone"/>: the reply tags another human (not her, not its
    /// author), which is what makes « vote » an order and « a voté » someone else's news.
    /// </summary>
    public static CardReplyIntent ReadCardReply(string content, bool tagsSomeone)
    {
        if (string.IsNullOrWhiteSpace(content)) return CardReplyIntent.None;

        var unquoted = _quotedSpeech.Replace(content, " ");
        if (TokenizeOrdered(_mentionMarkup.Replace(unquoted, " ")).Count > 0) content = unquoted;
        var cleaned = _link.Replace(_customEmote.Replace(_mentionMarkup.Replace(content, " "), " "), " ");
        var stripped = _trailingAddress.Replace(_leadingAddress.Replace(cleaned, " "), "");
        var t = TokenizeOrdered(stripped);
        var asks = stripped.Contains('?');

        // Nothing but tags (« @Luca @Sandra », « @Luca 👀 »): pointing people at the card.
        if (t.Count == 0)
        {
            if (tagsSomeone) return CardReplyIntent.Rally;
            return _doneMarks.Any(stripped.Contains) ? CardReplyIntent.Done : CardReplyIntent.None;
        }
        if (t.Count > MaxCardReplyTokens) return CardReplyIntent.None;
        var whole = string.Join(' ', t);

        if (IsRepost(t)) return CardReplyIntent.Repost;

        if (FindPhrase(t, _rallyPluralPhrases, cancelAfter: true, cancelBefore: false, notAfter: _notRallyAfter)
            || (tagsSomeone && (FindPhrase(t, _rallySingularPhrases, cancelAfter: true, cancelBefore: false, notAfter: _notRallyAfter)
                                || FindPhrase(t, _rallyAskPhrases, cancelAfter: false, cancelBefore: false))))
            return CardReplyIntent.Rally;

        // The bare forms have no subject, so with someone tagged they are about that person (« @Luca a voté »).
        if (!asks && ((!tagsSomeone && _doneAlone.Contains(whole))
                      || FindPhrase(t, _donePhrases, cancelAfter: false, cancelBefore: false)))
            return CardReplyIntent.Done;

        // Asked, or opening a short reply unasked (« c'est quand », « qui vient »).
        if ((asks && (_statusAlone.Contains(whole) || FindPhrase(t, _cardStatusPhrases, cancelAfter: false, cancelBefore: false)))
            || (t.Count <= MaxStatusOpenerTokens && _cardStatusOpeners.Any(p => PhraseStartsAt(t, 0, p))))
            return CardReplyIntent.Status;

        if (tagsSomeone && asks && t.Count <= MaxTaggedAskTokens) return CardReplyIntent.Rally;

        if (!asks && t.Count <= MaxTextAnswerTokens && IsTextAnswer(t, whole)) return CardReplyIntent.TextAnswer;

        return CardReplyIntent.None;
    }

    private static bool IsRepost(List<string> t)
    {
        if (t.Count <= MaxUpTokens && t.Any(w => Squash(w) == "up")) return true;
        // After its subject it is someone doing it (« tu remontes à quelle heure ? »); « tu le remontes ? » asks.
        if (FindPhrase(t, _repostVerbs, cancelAfter: true, cancelBefore: false, notAfter: _repostObjectSubjects)) return true;

        // Not someone's own: « remets ton vote » is changing it.
        var named = t.Where((w, i) => _cardNouns.Contains(w) && (i == 0 || !_possessives.Contains(t[i - 1]))).Any();
        for (var i = 0; i < t.Count; i++)
        {
            if (!_repostObjectVerbs.Contains(t[i]) || (i > 0 && _repostObjectSubjects.Contains(t[i - 1]))) continue;
            var pronounBefore = i > 0 && _cardPronouns.Contains(t[i - 1]);
            var pronounAfter = i + 1 < t.Count && _cardPronouns.Contains(t[i + 1]) && i + 2 >= t.Count;
            if (named || pronounBefore || pronounAfter || i == t.Count - 1) return true;
        }

        if (FindPhrase(t, _placeVerbs, cancelAfter: false, cancelBefore: false, notAfter: _repostSubjects)
            && FindPhrase(t, _bottomPhrases, cancelAfter: false, cancelBefore: false))
            return true;

        // Buried: « on le voit plus », or the card named as lost up there.
        return t.Count <= 8
               && (FindPhrase(t, _buriedPhrases, cancelAfter: false, cancelBefore: false)
                   || (named && FindPhrase(t, _buriedNamedPhrases, cancelAfter: false, cancelBefore: false,
                       notAfter: _notBuriedAfter)));
    }

    private static bool IsTextAnswer(List<string> t, string whole)
    {
        if (_textAnswerAlone.Contains(whole)) return true;
        if (FindPhrase(t, _textAnswerPhrases, cancelAfter: false, cancelBefore: false))
        {
            // « je peux voter pour deux » without its « ? » asks; everything else answers.
            var can = t.FindIndex(w => w is "peux" or "jpeux");
            if (can < 0 || can + 1 >= t.Count || !_notAnsweringAfterCan.Contains(t[can + 1])) return true;
        }
        for (var i = 0; i < t.Count; i++)
            if (t[i] is "chaud" or "chaude" && (i == 0 || !_notKeenAfter.Contains(t[i - 1])))
                return true;
        return t.Any(_dayWords.Contains) && t.All(w => _dayWords.Contains(w) || _dayChoiceWords.Contains(w));
    }
}
