namespace ProjectSYNCS.Services;

// What people say *about* her when her name comes up untagged (NameTalk), and the invitations said to
// her (ReadInvite).
internal enum NameTalk
{
    None,
    // « laissez syncs tranquille », « faites gaffe à ce que vous dites sur syncs ».
    Defended,
    // « je vais faire des tests sur syncs », « mise à jour de syncs ». Answered from Papa only.
    WorkOnHer,
    // « vous la shippez avec qui, syncs ? », « syncs est jalouse ».
    Ship,
    // « à côté de la plaque comme syncs », « je trouve syncs trop présente »: a dig, not an insult.
    Dig,
}

// Invitations (ChatIntent.Invite / Uninvite) and the name-talk reading. Every list here is read by the
// methods below only, never by another file's static initializer.
internal static partial class MessageCues
{
    private const int MaxInviteTokens = 20;

    // « je t'invite », « viens jouer avec nous ». A negator around « t'invite » makes it an uninvitation
    // (« on t'invite pas »), the other phrases are simply not read when negated (« tu veux pas venir »
    // doesn't even match). No bare « viens » (« viens voir », and in game it's an order) and no « tu
    // viens ce soir ? », which asks whether she'll be at the session (a Question).
    // No « t'invites » (« t'invites qui ce soir ? » asks whom she invites) and no bare « jouer avec nous »
    // (« jouer avec nous c'est cool »).
    private static readonly string[] _invitePhrases =
    {
        "t invite", "jt invite", "t inviter",
        "t es invitee", "tu es invitee", "t es invite", "t es conviee", "tu es conviee",
        "t es la bienvenue", "tu es la bienvenue",
        "viens jouer", "viens avec nous", "viens nous rejoindre", "viens nous voir", "rejoins nous", "joins nous",
        "viens en vocal", "viens en voc", "viens dans le vocal", "viens dans le voc",
        "tu veux venir", "venir jouer", "tu veux jouer avec nous", "tu viens jouer", "tu viens avec nous",
        "viens je t attends", "viens on t attend", "viens manger",
    };

    // The phrases above that turn into an uninvitation when negated.
    private static readonly string[] _invitedYouPhrases = { "t invite", "jt invite", "t inviter" };

    // Uninvited outright, or asked about it (« on t'a pas invitée ? »).
    private static readonly string[] _uninvitePhrases =
    {
        "t es pas invitee", "tu es pas invitee", "t es pas invite", "tu n es pas invitee", "t es pas conviee",
        "t es desinvitee", "tu es desinvitee", "je te desinvite", "on te desinvite",
        "t es pas la bienvenue", "tu es pas la bienvenue", "personne t invite", "personne ne t invite",
        "on t a pas invitee", "on t a pas invite", "t as pas ete invitee", "t a pas invitee",
    };

    // « je t'invite à voter », « je t'invite à te calmer »: urging, not inviting. « je t'invite à manger »
    // and « …à mon anniv » still invite.
    private static readonly HashSet<string> _urgedVerbs = new()
    {
        "voter", "lire", "relire", "consulter", "regarder", "verifier", "utiliser", "taper", "faire", "aller",
        "cliquer", "tester", "essayer", "reessayer", "reflechir", "te", "t", "partir", "quitter", "sortir",
        "arreter", "eviter", "respecter", "suivre", "poster", "ecrire", "rejoindre", "ouvrir", "lancer",
    };

    private static ChatIntent? ReadInvite(List<string> t)
    {
        if (t.Count > MaxInviteTokens) return null;
        // « plus tard » is later, not « no more »: « je t'invite plus tard ».
        t = t.Where((w, i) => !(w == "plus" && i + 1 < t.Count && t[i + 1] == "tard")).ToList();
        for (var i = 0; i + 3 < t.Count; i++)
            if (t[i] == "t" && t[i + 1] == "invite" && t[i + 2] == "a" && _urgedVerbs.Contains(t[i + 3])) return null;
        if (FindPhrase(t, _uninvitePhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.Uninvite;
        if (FindPhrase(t, _invitePhrases, cancelAfter: true, cancelBefore: true)) return ChatIntent.Invite;
        if (FindPhrase(t, _invitedYouPhrases, cancelAfter: false, cancelBefore: false)) return ChatIntent.Uninvite;
        return null;
    }

    // ---- Talked about: what is said about her ------------------------------------------------------
    // Read on AddressWords (squashed, mentions dropped) with every spelling of her name as « syncs »,
    // so the phrases name her once. PhraseStartsAt compares squashed, so they are written plainly.

    private static readonly string[] _defendedPhrases =
    {
        "laissez syncs tranquille", "laisse syncs tranquille", "laissez syncs en paix", "laisse syncs en paix",
        "foutez la paix a syncs", "fichez la paix a syncs", "fous la paix a syncs", "fiche la paix a syncs",
        "touchez pas a syncs", "touche pas a syncs", "on touche pas a syncs", "personne touche a syncs",
        "ce que vous dites sur syncs", "ce que tu dis sur syncs", "ce que vous dites de syncs", "ce que tu dis de syncs",
        "respectez syncs", "respecte syncs", "soyez gentils avec syncs", "soyez gentil avec syncs", "sois gentil avec syncs",
        "sois gentille avec syncs", "soyez sympa avec syncs", "sois sympa avec syncs", "parlez mieux a syncs",
        "parle mieux a syncs", "pas mechants avec syncs", "pas mechant avec syncs", "pas mechante avec syncs",
        "attaquez pas syncs", "embetez pas syncs", "embete pas syncs", "harcelez pas syncs",
    };

    // Not « version de syncs » alone: « c'est quoi la version de syncs ? » asks, it announces nothing.
    private static readonly string[] _workOnHerPhrases =
    {
        "tests sur syncs", "test sur syncs", "tester syncs", "teste syncs", "testez syncs",
        "update syncs", "updater syncs", "mettre a jour syncs", "mets a jour syncs", "mise a jour de syncs",
        "maj de syncs", "maj syncs", "nouvelle version de syncs",
        "bosser sur syncs", "bosse sur syncs", "travailler sur syncs", "travaille sur syncs",
        "coder syncs", "code syncs", "code de syncs", "debug syncs", "debugger syncs", "deboguer syncs", "ameliorer syncs",
        "fonctionnalite pour syncs", "fonctionnalites pour syncs", "feature pour syncs", "features pour syncs",
        "nouveau truc pour syncs", "nouveaux trucs pour syncs", "apprendre a syncs", "ajouter a syncs",
        "truc a syncs", "trucs a syncs",
    };

    // A dig that isn't an insult, so the mood never reads it as mean: what she does wrong, how much
    // she's around, being useless. Kept out of the mood cues on purpose: as mean cues they would score
    // on /shame and draw her reactions on every message nobody aimed at her. Not « osef syncs » (said to
    // her as often as not), « plus de syncs » (more, or no more?) or « déconne » (« on déconne » jokes).
    //
    // Her name as the subject, right before (« syncs comprend rien », « syncs elle bug tout le temps »,
    // « syncs qui s'en fiche… »): these words are about anything else otherwise (« ce jeu me saoule »).
    private static readonly string[] _subjectDigPhrases =
    {
        "comprend rien", "comprend jamais rien", "repond n importe comment", "repond a cote",
        "bug tout le temps", "bugue tout le temps", "plante tout le temps", "pas drole",
        "m agace", "me saoule", "me soule", "m enerve", "parle trop", "s en fiche", "s en fout", "s en moque",
        "sert a rien", "servait a rien", "sert pas a grand chose", "lourde", "trop bavarde",
    };
    // What may stand between her name and the dig: « syncs elle… », « syncs qui… », « syncs est… ».
    // Squashed like the words they meet (« elle » is « ele »).
    private static readonly HashSet<string> _digSubjectGlue = SquashAll("elle", "qui", "est", "c", "etait", "vraiment", "toujours");

    // Near her name, either side (DigWindow words), the name possibly inside: these only fit her (the
    // feminine « présente »), or name her.
    private static readonly string[] _nearDigPhrases =
    {
        "a cote de la plaque", "trop presente", "trop envahissante", "trop bavarde", "son esclave", "esclave de syncs",
        "trop de syncs", "marre de syncs", "en fiche de syncs", "en fout de syncs", "moque de syncs",
    };
    private const int DigWindow = 6;

    // Ship talk about her love life, not about /ship's results (« syncs a ship Luca et Sandra », « le ship
    // de syncs était nul »): a love word within a few words of her name, or a ship verb with her as its
    // object (« vous la shippez avec qui ? », « shippez syncs »). Squashed like the words they meet. No
    // « copain », « chéri » (« mon copain dit que syncs a planté »), no « mariée » (it squashes onto Marie).
    private static readonly HashSet<string> _loveTalkWords = SquashAll(
        "jalouse", "amoureuse", "crush", "couple", "celibataire", "marier", "epouser");
    private static readonly string[] _shipHerPhrases =
    {
        "la ship", "la shippe", "la shipper", "la shippez", "la shippent", "la shippons",
        "ship syncs", "shippe syncs", "shipper syncs", "shippez syncs", "shippent syncs",
    };
    private const int ShipTalkWindow = 4;

    /// <summary>
    /// What a message naming her says about her: someone defending her, Papa working on her, a dig, or
    /// ship talk; <see cref="NameTalk.None"/> otherwise. Pure; the caller has already checked the
    /// message names her, and reads a specific intent first when it is said to her.
    /// </summary>
    public static NameTalk ReadNameTalk(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return NameTalk.None;
        var words = AddressWords(content);
        if (!words.Any(_selfNames.Contains)) words = GlueSpelledOutRuns(words);
        var t = words.Select(w => _selfNames.Contains(w) ? "syncs" : w).ToList();
        var names = Enumerable.Range(0, t.Count).Where(i => t[i] == "syncs").ToList();
        if (names.Count == 0) return NameTalk.None;

        if (FindPhrase(t, _defendedPhrases, cancelAfter: false, cancelBefore: false)) return NameTalk.Defended;
        if (FindPhrase(t, _workOnHerPhrases, cancelAfter: false, cancelBefore: false)) return NameTalk.WorkOnHer;
        if (names.Any(i => IsSubjectDig(t, i)) || IsNearDig(t, names)) return NameTalk.Dig;
        if (names.Any(i => NearPhrase(t, i, _shipHerPhrases, ShipTalkWindow))) return NameTalk.Ship;
        foreach (var i in names)
            for (var j = Math.Max(0, i - ShipTalkWindow); j <= Math.Min(t.Count - 1, i + ShipTalkWindow); j++)
                if (_loveTalkWords.Contains(t[j])) return NameTalk.Ship;
        return NameTalk.None;
    }

    // A subject dig right after her name, past at most three glue words, with no negator on the way
    // (« syncs sert pas à rien »).
    private static bool IsSubjectDig(List<string> t, int name)
    {
        var j = name + 1;
        while (j < t.Count && j - name <= 3 && _digSubjectGlue.Contains(t[j])) j++;
        if (j >= t.Count) return false;
        if (Enumerable.Range(name + 1, j - name - 1).Any(k => IsNegator(t[k]))) return false;
        return _subjectDigPhrases.Any(p => PhraseStartsAt(t, j, p));
    }

    // A near dig within DigWindow words of one of her names, no negator just before it (« pas trop présente »).
    private static bool IsNearDig(List<string> t, List<int> names)
    {
        foreach (var p in _nearDigPhrases)
        {
            var length = p.Split(' ').Length;
            for (var k = 0; k + length <= t.Count; k++)
            {
                if (!PhraseStartsAt(t, k, p)) continue;
                if (Enumerable.Range(Math.Max(0, k - NegationWindow), k - Math.Max(0, k - NegationWindow)).Any(n => IsNegator(t[n])))
                    continue;
                if (names.Any(i => i >= k - DigWindow && i <= k + length - 1 + DigWindow)) return true;
            }
        }
        return false;
    }

    // Whether one of the phrases starts within window words of index i.
    private static bool NearPhrase(List<string> t, int i, string[] phrases, int window)
    {
        for (var k = Math.Max(0, i - window); k <= Math.Min(t.Count - 1, i + window); k++)
            if (phrases.Any(p => PhraseStartsAt(t, k, p))) return true;
        return false;
    }
}
