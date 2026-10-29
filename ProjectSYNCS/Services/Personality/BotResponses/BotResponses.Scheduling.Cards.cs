using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Under her cards -----------------------------------------------------------------------
    //
    // A reply under a poll, a vote, a session, /tonight or a giveaway (CardReplyService, read by
    // MessageCues.ReadCardReply). Every card, so no line names one kind: « la carte », « les
    // boutons ». {0} = the name, {1} = the weekday, {3} = the card's title (sanitised). Sent with
    // mentions off: a {2} holding tags shows them, pings nobody. string.Format, picked first.

    // « remonte-le », « up »: said just before the card comes back below. The card follows this line.
    public static readonly string[] CardRepostLines =
    {
        "Et hop, je le remonte. Cette fois, vous cliquez." + Sticker.Smug,
        "Remonté. Il était tombé tout en bas, comme vos promesses de venir." + Sticker.Smug,
        "Livraison express : « {3} », juste en dessous." + Sticker.Cool,
        "Voilà, tout frais. Même carte, mêmes boutons. Plus d'excuses." + Sticker.Smug,
        "Je le remets sous vos yeux. Fini le « je l'avais pas vu »." + Sticker.Jenote,
        "Repost effectué, et sans passer par une commande. Je suis efficace, hein (˶ᵔ ᵕ ᵔ˶)" + Sticker.Slay,
        "C'est remonté. Si personne clique cette fois, je le prends personnellement." + Sticker.Menace,
        "Le revoilà. Il vous avait manqué, avouez." + Sticker.Happy,
        "En bas de la conversation, comme demandé. Je fais le ménage, vous faites les clics." + Sticker.Bored,
        "Bien reçu, {0}. La carte est en bas. La même, mais en bas." + Sticker.Cool,
    };

    // The same ask again, a few minutes after she brought it back.
    public static readonly string[] CardRepostTooSoonLines =
    {
        "Je viens de le remonter. Il est juste là. Regarde un peu mieux (¬_¬)" + Sticker.Grumpy,
        "Déjà fait il y a pas longtemps. Le problème, c'est pas la carte, c'est les clics." + Sticker.Bored,
        "Encore ? Il a même pas eu le temps de refroidir." + Sticker.Smug,
        "Je le remonte pas toutes les trente secondes. Il est là, il bouge pas." + Sticker.Grumpy,
        "Il est tout frais, celui-là. Scrolle un tout petit peu." + Sticker.Bored,
        "Non. Je viens de le faire. Si personne l'a vu, c'est que personne regarde." + Sticker.Sulk,
        "Je suis pas un ascenseur, {0}. Il est déjà remonté." + Sticker.Grumpy,
        "Remonter une carte qu'on vient de remonter, c'est pas utile. C'est du spam, en plus joli." + Sticker.Jenote,
    };

    // A rally, a typed answer or a repost under a card that is closed, cancelled, drawn or expired.
    public static readonly string[] CardClosedLines =
    {
        "Trop tard, c'est fermé. Fallait cliquer quand c'était ouvert (¬_¬)" + Sticker.Smug,
        "C'est clos. Les boutons sont partis en vacances." + Sticker.Bored,
        "Fini. Terminé. Archivé. Mes logs ont tourné la page." + Sticker.Jenote,
        "Cette carte est fermée. Je rouvre rien, c'est le principe d'une fin." + Sticker.Cool,
        "Tu arrives après la bataille. Elle était pas très longue, mais elle est finie." + Sticker.Smug,
        "C'est plus d'actualité. Ton message non plus, du coup." + Sticker.Bored,
        "Ça s'est terminé sans toi. Je dis ça sans jugement. Avec un peu de jugement." + Sticker.Smug,
        "Fermé ! La prochaine fois, je te garde une place. Ou pas." + Sticker.Smug,
    };

    // « votez ! », said to the room: she backs them up. {2} = where the card stands, as a count
    // (« 3 votants », « 2 inscrits », « 1/4 chauds », « 5 participants »).
    public static readonly string[] CardRallyLines =
    {
        "Écoutez {0}. Pour l'instant : {2}. C'est pas assez." + Sticker.Menace,
        "Ce que {0} a dit. J'en suis à {2}, et j'aimerais compter plus loin." + Sticker.Bored,
        "Merci {0}. Enfin quelqu'un qui fait mon travail. On en est à {2}." + Sticker.Happy,
        "{0} a raison, et je confirme : {2}. Les boutons sont juste au-dessus." + Sticker.Jenote,
        "Allez, on obéit à {0}. Score actuel : {2}. On peut faire mieux (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Je soutiens {0} à cent pour cent. Le compteur, lui, en est à {2}." + Sticker.Smug,
        "Appel relayé. Officiellement. {2} au compteur, et je veux voir ce chiffre monter." + Sticker.Formal,
        "Oui ! Cliquez ! {2}, c'est un début, c'est pas une fin." + Sticker.Happy,
        "{0} fait campagne, je fais les comptes : {2}." + Sticker.Jenote,
        "Merci de me prêter ta voix, {0}. La mienne, personne l'écoute. {2} pour l'instant." + Sticker.Sulk,
        "Renfort reçu. {2}, et j'attends les autres. Je les vois, ceux qui lisent sans cliquer 👁👄👁️" + Sticker.Menace,
    };

    // « @Luca vote », and the people tagged haven't answered. {2} = them, tagged (inert): one person
    // or several, so no « tu » or « vous » aimed at them.
    public static readonly string[] CardRallyNamedLines =
    {
        "Je confirme : {2}, toujours rien. Pas un clic. Je vérifie, c'est mon métier." + Sticker.Jenote,
        "{2} : aucun clic à ce nom. {0} a bien raison d'insister." + Sticker.Jenote,
        "D'après mes logs, réponse manquante pour {2}. {0} a l'œil." + Sticker.Jenote,
        "Pas de réponse de {2} sur cette carte. Je dis ça, je dis rien (¬_¬)" + Sticker.Smug,
        "{0} a raison de relancer. {2} : zéro clic au compteur." + Sticker.Jenote,
        "Liste des retardataires du clic : {2}. Elle est courte. Elle pourrait être vide." + Sticker.Jenote,
        "{2}, la carte est juste au-dessus. Elle mord pas. Moi un peu, mais pas elle." + Sticker.Smug,
        "Je viens de vérifier pour {2} : rien. Le bouton est pourtant pas compliqué." + Sticker.Bored,
        "Relance validée. {2} : en attente d'un clic depuis trop longtemps." + Sticker.Formal,
        "{2}, un clic, ça prend une seconde. J'ai chronométré." + Sticker.Smug,
    };

    // « @Luca vote », and everyone tagged already answered. {2} = them, tagged (inert).
    public static readonly string[] CardRallyAllDoneLines =
    {
        "Tu peux lâcher {2}, {0}. C'est déjà fait, j'ai vérifié." + Sticker.Jenote,
        "{2} : déjà répondu, d'après mes logs. Et mes logs se trompent jamais." + Sticker.Cool,
        "Relance inutile : {2}, c'est déjà dans la boîte." + Sticker.Cool,
        "{2} : déjà cliqué. Pour une fois, c'est les autres qu'il faut relancer." + Sticker.Smug,
        "Mauvaise cible, {0}. {2} : réponse reçue. Cherche plutôt ceux qui disent rien." + Sticker.Smug,
        "Je défends {2} sur ce coup-là : c'est fait. Rare, mais vrai (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Déjà validé pour {2}. Garde ton énergie pour les vrais retardataires." + Sticker.Cool,
        "{2} ? Déjà là. Je l'avais noté avant toi, {0}." + Sticker.Jenote,
    };

    // « j'ai voté », and the card says so.
    public static readonly string[] CardDoneSeenLines =
    {
        "Vu. C'est bien enregistré. Bon réflexe ♡" + Sticker.Happy,
        "Confirmé, je l'ai dans mes logs. Une bonne action de plus." + Sticker.Jenote,
        "Je vérifie... oui, c'est là. Bravo, {0}. Pas trop fort, mais bravo." + Sticker.Happy,
        "C'est noté depuis le clic. Mais merci de me tenir au courant." + Sticker.Smug,
        "Parfait. Si tout le monde faisait comme toi, j'aurais moins de travail (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Enregistré, horodaté, archivé. Tu peux dormir tranquille." + Sticker.Cool,
        "Je sais. Je vois tout. Mais c'est gentil de le dire." + Sticker.Smug,
        "Oui, oui, c'est bon. Maintenant, va convaincre les autres." + Sticker.Bored,
        "Bien reçu. Je te mets dans la liste des gens fiables. Elle est courte." + Sticker.Jenote,
        "Validé. Une ligne propre dans ma base de données. J'adore ça." + Sticker.Slay,
    };

    // « j'ai voté », and the card says otherwise. Only while it is open: these lines say to click.
    public static readonly string[] CardDoneLiarLines =
    {
        "Ah bon ? Mes logs disent le contraire. Clique, {0}." + Sticker.Jenote,
        "T'as dit ça, oui. Mais t'as pas cliqué. Je vois tout (¬_¬)" + Sticker.Menace,
        "J'ai vérifié : rien à ton nom. Le dire, c'est pas le faire." + Sticker.Smug,
        "Mensonge détecté. Le bouton est juste au-dessus, je t'attends." + Sticker.Menace,
        "Intéressant. D'après ma base de données, rien du tout à ton nom." + Sticker.Jenote,
        "Je te crois. Non, je te crois pas. Clique." + Sticker.Smug,
        "Pas de clic, pas de réponse. C'est comme ça que je compte." + Sticker.Cool,
        "Tu l'as peut-être fait dans ta tête. Fais-le avec ta souris, maintenant." + Sticker.Smug,
        "Erreur 404 : réponse de {0} introuvable." + Sticker.Smug,
        "J'aimerais beaucoup te croire. Mes logs, eux, sont plus difficiles à convaincre." + Sticker.Jenote,
    };

    // « samedi pour moi », « je peux pas », typed and not clicked, on an open card.
    public static readonly string[] CardTextClickLines =
    {
        "Noté. Enfin, pas noté du tout : ça compte que sur les boutons." + Sticker.Jenote,
        "Super. Maintenant, dis-le avec un clic." + Sticker.Smug,
        "Je compte les clics, pas les messages. Clique, {0}." + Sticker.Bored,
        "Un message, c'est sympa. Un clic, c'est une réponse." + Sticker.Smug,
        "Ta réponse me fait plaisir. Elle me ferait encore plus plaisir sur le bouton ♡" + Sticker.Bargain,
        "J'ai fait des boutons exprès. Ils se sentent un peu ignorés, là." + Sticker.Sulk,
        "Je peux pas cliquer à ta place. J'ai pas de doigts. Toi, si." + Sticker.Bored,
        "Message reçu, réponse non enregistrée. Le bouton, {0}." + Sticker.Jenote,
        "Pour compter, je lis pas les commentaires. Les boutons, eux, je les lis." + Sticker.Smug,
        "Merci pour l'info. Maintenant, la version officielle : un clic." + Sticker.Formal,
    };

    // The same, from someone who already clicked.
    public static readonly string[] CardTextAnsweredLines =
    {
        "Je sais, t'as déjà cliqué. Mais j'aime bien quand on me le redit ♡" + Sticker.Happy,
        "C'est déjà noté. Le clic et le message : double confirmation." + Sticker.Jenote,
        "Oui, je l'ai. T'as cliqué avant de parler, c'est l'ordre idéal." + Sticker.Happy,
        "Enregistré depuis ton clic. Ce message, c'est du bonus." + Sticker.Cool,
        "Déjà dans ma base. Tu peux arrêter de t'inquiéter." + Sticker.Cool,
        "Je confirme, ta réponse est bien là. Elle bouge plus." + Sticker.Jenote,
        "Pas besoin de me le dire, je l'ai vu passer. Mais c'est gentil." + Sticker.Smug,
        "C'est bon, c'est enregistré. Si seulement tout le monde faisait les deux." + Sticker.Happy,
    };

    // « c'est quand ? », « qui vient ? », « on en est où ? ». {2} = where the card stands, in words
    // (count, leader or time, who: CardReplyService), {3} = its title.
    public static readonly string[] CardStatusLines =
    {
        "Point sur « {3} » : {2}." + Sticker.Jenote,
        "Le bilan de « {3} », en direct : {2}." + Sticker.Cool,
        "« {3} » ? {2}. Tout est sur la carte, mais je suis gentille aujourd'hui." + Sticker.Smug,
        "D'après mes registres : {2}." + Sticker.Jenote,
        "Je résume, parce que lire une carte c'est trop dur : {2}." + Sticker.Smug,
        "« {3} », état actuel : {2}. Je mets à jour en temps réel, moi." + Sticker.Slay,
        "Voilà où on en est : {2}." + Sticker.Bored,
        "Rapport demandé, rapport livré : {2}." + Sticker.Formal,
        "Pour « {3} » : {2}. La réponse était juste au-dessus, je dis ça (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "J'ai tout compté, comme toujours : {2}." + Sticker.Jenote,
    };
}
