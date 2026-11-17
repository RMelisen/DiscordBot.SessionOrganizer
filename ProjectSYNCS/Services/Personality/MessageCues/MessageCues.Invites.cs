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
    private static readonly string[] _invitePhrases =
    {
        "t invite", "jt invite", "t inviter", "t invites",
        "t es invitee", "tu es invitee", "t es invite", "t es conviee", "tu es conviee",
        "t es la bienvenue", "tu es la bienvenue",
        "viens jouer", "viens avec nous", "viens nous rejoindre", "rejoins nous", "joins nous",
        "tu veux venir", "venir jouer", "jouer avec nous", "tu viens jouer", "tu viens avec nous",
        "viens je t attends", "viens on t attend", "viens manger",
    };

    // The phrases above that turn into an uninvitation when negated.
    private static readonly string[] _invitedYouPhrases = { "t invite", "jt invite", "t inviter", "t invites" };

    // Uninvited outright.
    private static readonly string[] _uninvitePhrases =
    {
        "t es pas invitee", "tu es pas invitee", "t es pas invite", "tu n es pas invitee", "t es pas conviee",
        "t es desinvitee", "tu es desinvitee", "je te desinvite", "on te desinvite",
        "t es pas la bienvenue", "tu es pas la bienvenue", "personne t invite", "personne ne t invite",
    };

    private static ChatIntent? ReadInvite(List<string> t)
    {
        if (t.Count > MaxInviteTokens) return null;
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

    private static readonly string[] _workOnHerPhrases =
    {
        "tests sur syncs", "test sur syncs", "tester syncs", "teste syncs", "testez syncs",
        "update syncs", "updater syncs", "mettre a jour syncs", "mets a jour syncs", "mise a jour de syncs",
        "maj de syncs", "maj syncs", "version de syncs",
        "bosser sur syncs", "bosse sur syncs", "travailler sur syncs", "travaille sur syncs",
        "coder syncs", "code de syncs", "debug syncs", "debugger syncs", "deboguer syncs", "ameliorer syncs",
        "fonctionnalite pour syncs", "fonctionnalites pour syncs", "feature pour syncs", "features pour syncs",
        "nouveau truc pour syncs", "nouveaux trucs pour syncs", "apprendre a syncs",
    };

    // A dig behind her back that isn't an insult, so the mood never reads it as mean: what she does
    // wrong, how much she's around, being useless or ignored. Kept out of the mood cues on purpose: as
    // mean cues they would score on /shame and draw her reactions on every message nobody aimed at her.
    // Anywhere in a message that names her and isn't said to her. Not « osef syncs » (said to her, as
    // often as not) or « plus de syncs » (more, or no more?).
    private static readonly string[] _digPhrases =
    {
        "a cote de la plaque", "trop presente", "trop envahissante", "trop bavarde", "parle trop", "trop de syncs",
        "s en fiche", "s en fout", "s en moque", "sert a rien", "servait a rien", "sert pas a grand chose",
        "son esclave", "esclave de syncs", "comprend rien", "comprend jamais rien", "repond n importe comment",
        "bug tout le temps", "bugue tout le temps", "plante tout le temps", "deconne", "pas drole",
        "marre de syncs", "me saoule", "m agace", "est lourde", "on peut s en passer", "on s en passe",
    };

    // Ship talk: one of these words within a few words of her name, either side. Squashed like the
    // words they meet. No « copain », « chéri » (« mon copain dit que syncs a planté »), and no « mariée »,
    // which squashes onto the name Marie.
    private static readonly HashSet<string> _shipTalkWords = SquashAll(
        "ship", "shippe", "shipper", "shippez", "shippent", "shippons", "jalouse", "amoureuse", "crush", "couple",
        "celibataire", "marier", "epouser");
    private const int ShipTalkWindow = 4;

    /// <summary>
    /// What a message naming her says about her: someone defending her, Papa working on her, or ship
    /// talk; <see cref="NameTalk.None"/> otherwise. Pure; the caller has already checked the message
    /// names her and isn't said to her.
    /// </summary>
    public static NameTalk ReadNameTalk(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return NameTalk.None;
        var words = AddressWords(content);
        if (!words.Any(_selfNames.Contains)) words = GlueSpelledOutRuns(words);
        var t = words.Select(w => _selfNames.Contains(w) ? "syncs" : w).ToList();

        if (FindPhrase(t, _defendedPhrases, cancelAfter: false, cancelBefore: false)) return NameTalk.Defended;
        if (FindPhrase(t, _workOnHerPhrases, cancelAfter: false, cancelBefore: false)) return NameTalk.WorkOnHer;
        // « syncs sert à rien » is a dig, « syncs sert pas à rien » isn't: a negator before cancels.
        if (FindPhrase(t, _digPhrases, cancelAfter: false, cancelBefore: true)) return NameTalk.Dig;
        for (var i = 0; i < t.Count; i++)
        {
            if (t[i] != "syncs") continue;
            for (var j = Math.Max(0, i - ShipTalkWindow); j <= Math.Min(t.Count - 1, i + ShipTalkWindow); j++)
                if (_shipTalkWords.Contains(t[j])) return NameTalk.Ship;
        }
        return NameTalk.None;
    }
}
