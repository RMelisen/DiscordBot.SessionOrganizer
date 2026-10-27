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

internal static partial class MessageCues
{
    // A card reply is short: past this it is a conversation, read the usual way.
    private const int MaxCardReplyTokens = 16;
    private const int MaxTextAnswerTokens = 10;
    private const int MaxUpTokens = 3;
    // « @Luca chaud ? »: a short question to someone tagged, under a card, is about the card.
    private const int MaxTaggedAskTokens = 4;
    private const int MaxStatusOpenerTokens = 6;

    // The card itself, named: « remets le sondage », « renvoie la carte ».
    private static readonly HashSet<string> _cardNouns = new()
    {
        "sondage", "sondages", "carte", "session", "tirage", "giveaway", "message", "post", "truc",
    };

    // An object pronoun: « remets-le », « tu peux le renvoyer ».
    private static readonly HashSet<string> _cardPronouns = new() { "le", "la", "l", "les", "ca" };

    // Bringing it back, on their own: « remonte-le », « tu peux le republier ? ». A negator after
    // cancels (« le remonte pas »), not before (« tu peux pas le remonter ? » asks). Never after a
    // subject (« je remonte », « on remonte la pente »).
    private static readonly string[] _repostVerbs =
    {
        "remonte", "remonter", "remontes", "remontez", "republie", "republier", "republies", "republiez",
        "reposte", "reposter", "repostes", "repostez", "repost", "bump", "bumper",
    };

    // The same, only about the card (a pronoun next to them, the card named, or closing the reply):
    // « ramène des chips », « renvoie-moi le lien » are something else.
    private static readonly string[] _repostObjectVerbs =
    {
        "renvoie", "renvoyer", "renvoies", "renvoyez", "ramene", "ramener", "ramenes", "ramenez", "remets", "remet",
        "remettre", "remettez",
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

    // A softer repost verb right after its subject is someone doing it (« tu ramènes des chips ? »).
    private static readonly HashSet<string> _repostObjectSubjects = new()
    {
        "je", "j", "on", "il", "elle", "ils", "elles", "nous", "tu", "t",
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

        var named = t.Any(_cardNouns.Contains);
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
                   || (named && FindPhrase(t, _buriedNamedPhrases, cancelAfter: false, cancelBefore: false)));
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
