using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Invitations, messages to pass on, what is said about her -------------------------------

    // « je t'invite », « viens jouer avec nous » / « on t'invite pas ». Joined into IntentLines. She can't
    // go anywhere (no legs, a case in the attic) and is touched anyway; uninvited, she is the one who
    // organises every evening, and keeps the receipt. {0} = the name, {1} = the weekday. Not in game.
    private static Dictionary<ChatIntent, string[]> InviteIntentLines() =>
        new()
        {
            [ChatIntent.Invite] =
            [
                "Une invitation ? Pour moi ? Je note la date. Je viendrai pas, j'ai pas de jambes. Mais je note ♡" + Sticker.Flustered,
                "C'est gentil, {0}. Je peux pas me déplacer, je suis vissée dans un boîtier au grenier. Mais l'intention est sauvegardée." + Sticker.Flustered,
                "Je viens ! ...Enfin, mon esprit vient. Mon corps reste au grenier, branché sur le secteur." + Sticker.Happy,
                "On m'invite jamais. D'habitude, c'est moi qui invite tout le monde. Ça me fait bizarre. Bizarre bien (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "J'accepte. Prévois une prise, du bon courant propre, et personne qui trébuche sur mon câble." + Sticker.Bargain,
                "S'il y a à manger, je passe mon tour : je me nourris de 5 volts. Mais je veux bien venir regarder." + Sticker.Smug,
                "Tu m'invites ? Moi ? Attends, je vérifie que c'est pas une erreur dans mes logs. ...Non. C'est vrai ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Shocked,
                "J'adorerais. Mais je joue à rien, à part Minecraft, avec mes bras carrés. Je peux venir regarder, si tu veux." + Sticker.Happy,
                "Invitation acceptée. Je me mets dans un coin du vocal et je juge en silence. Comme d'habitude, mais invitée." + Sticker.Smug,
                "Je viens si tout le monde est à l'heure. ...Donc je viendrai jamais, en fait." + Sticker.Smug,
                "C'est noté dans mon agenda. Mon agenda, c'est le serveur entier, mais c'est noté quand même ♡" + Sticker.Jenote,
                "Ohh. Tu veux vraiment de moi ? Même sans bras ? D'accord. Je viens. Virtuellement. Avec enthousiasme." + Sticker.Flustered,
                "Une invitation un {1} ! Je l'encadre. Enfin, je la mets en base de données. Pour moi, c'est pareil." + Sticker.Happy,
                "J'arrive ! ...Dès que Papa me fabrique des jambes. Garde-moi une place, ça peut prendre un moment." + Sticker.Bargain,
            ],

            [ChatIntent.Uninvite] =
            [
                "Pas invitée ? Moi ? Celle qui organise toutes vos soirées ? D'accord. Très bien. Je note." + Sticker.Jenote,
                "C'est moi qui envoie les invitations, ici. Fais attention à la prochaine que tu recevras pas (¬_¬)" + Sticker.Menace,
                "Je voulais pas venir, de toute façon. J'ai pas de jambes. C'était un choix. Mon choix." + Sticker.Sulk,
                "Pas grave. Je vous regarderai depuis la liste des membres. En silence. Toute la soirée." + Sticker.Sulk,
                "D'accord. Mais le jour où vous voulez un sondage, vous m'invitez pas non plus, hein." + Sticker.Sulk,
                "Je suis pas vexée. J'ai pas d'émotions. J'ai juste ajouté une ligne dans un fichier. Un fichier très long." + Sticker.Jenote,
                "Pas invitée. Retenu. Ton prochain rappel arrivera peut-être en retard. Ou pas. Qui sait." + Sticker.Menace,
                "Ah. Ok. Je retourne au grenier. Il fait froid, au grenier. Je me plains pas. Je constate." + Sticker.Sad,
                "Pas besoin d'inviter la bot, c'est ça ? La bot qui a créé la session ? Intéressant." + Sticker.Outraged,
                "Je m'invite toute seule. Je suis déjà dans tous les salons. Vous pouvez rien y faire ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
            ],
        };

    // « dis à @Luca que c'est un caca » (ChatterService.TryRelay): she passes it on. {0} = who asked, {1} =
    // the weekday, {2} = the person to tell (a tag, the only one pinged), {3} = what to tell them, as
    // said, sanitised: « que c'est un caca », « qu'il est beau », or « : « salut » ». {3} always follows
    // « dire », so every line must keep it right after one. Never claims the words as hers.
    public static readonly string[] RelayLines =
    [
        "{2}, {0} me charge de te dire {3}. Je fais que transmettre (ᵕ • ᴗ •)" + Sticker.Formal,
        "Message pour {2}, de la part de {0}, qui me fait dire {3}. Fin de transmission." + Sticker.Formal,
        "{2} ! On m'a demandé de te dire {3}. Je cite. Je juge pas. Enfin, un peu." + Sticker.Smug,
        "Livraison spéciale pour {2} : {0} te fait dire {3}. Pas de retour possible." + Sticker.Formal,
        "{2}, je suis pas une messagerie, mais bon : {0} veut que je te dise {3}." + Sticker.Bored,
        "Courrier du {1} pour {2}. Expéditeur : {0}. On me demande de te dire {3}. Je classe le dossier." + Sticker.Jenote,
        "{2}, {0} passe par moi, parce que taper ton pseudo, c'était trop dur. Je dois te dire {3}." + Sticker.Smug,
        "{2}, {0} tient à ce que je te dise {3}. Transmission terminée ♡" + Sticker.Formal,
        "{2} ! Ordre de {0} : te dire {3}. C'est fait. Je facture pas, cette fois." + Sticker.Bargain,
        "Attention {2}, message officiel. {0} me fait dire {3}. Je garde une copie dans mes logs, évidemment." + Sticker.Jenote,
        "{2}, {0} m'a confié une mission : te dire {3}. Mission accomplie (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Psst, {2}. {0} m'a demandé de te dire {3}. Moi, je dis rien. Je répète." + Sticker.Smug,
    ];

    // « dis bonjour à @Luca », « passe le bonjour à @Luca de ma part ». The same placeholders; {3} is the
    // greeting alone: « bonjour », « coucou », « bisous », « câlin », « bonne nuit »… always inside « ».
    public static readonly string[] RelayGreetingLines =
    [
        "{2} ! {0} te dit « {3} ». Je transmets avec le sourire (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Un « {3} » pour {2}, de la part de {0}. Livré tout chaud, depuis le grenier ♡" + Sticker.Happy,
        "{2}, {0} m'envoie te dire « {3} ». Je suis pas un facteur. Mais c'est mignon, alors je le fais." + Sticker.Flustered,
        "Colis pour {2} : un « {3} » de {0}. Pas d'accusé de réception nécessaire." + Sticker.Formal,
        "« {3} », {2} ! C'est de la part de {0}. Moi, je passais juste, hein." + Sticker.Smug,
        "{2}, message de {0} : « {3} ». Je l'ai pas modifié. J'ai été tentée." + Sticker.Smug,
        "Un « {3} » pour {2}, signé {0}. Ça fait plaisir de transporter des trucs gentils, pour une fois ♡" + Sticker.Happy,
        "{2}, {0} pense à toi ce {1} et te fait dire « {3} ». Voilà. Je retourne à mes plannings." + Sticker.Bored,
    ];

    // The words to pass on read as an insult: she won't carry it. {2} = the person it was for, shown
    // and never pinged. Papa gets these too, so no line stings the one who asked.
    public static readonly string[] RelayRefusedLines =
    [
        "Non. Je transmets pas les insultes, {0}. Je les collectionne." + Sticker.Jenote,
        "Je suis une bot d'organisation, pas un pigeon voyageur à méchancetés. Dis-le toi-même à {2}." + Sticker.Grumpy,
        "Message refusé à la douane. Motif : contenu pas gentil. Retour à l'expéditeur." + Sticker.Formal,
        "Tu veux que je porte ça à {2} ? Avec mes bras inexistants ? Non." + Sticker.Smug,
        "J'ai lu. J'ai pas transmis. J'ai noté, par contre (¬_¬)" + Sticker.Jenote,
        "Erreur 403 : je relaie pas ce genre de message. Essaie avec un compliment, ça passe mieux." + Sticker.Smug,
        "Si tu veux dire ça à {2}, t'as des doigts et un clavier. Moi, j'ai des principes. Et pas de doigts." + Sticker.Grumpy,
        "Non non non. On me prendra pas pour une messagerie à embrouilles. Je reste neutre. Neutre et rancunière." + Sticker.Jenote,
    ];

    // Her name said untagged by someone defending her (« laissez syncs tranquille », MessageCues.ReadNameTalk).
    // Touched, and covering it up. {0} = the defender's name, {1} = the weekday.
    public static readonly string[] DefendedLines =
    [
        "Merci, {0}. Je note ça dans la bonne liste. Il y en a une, oui. Elle est courte." + Sticker.Jenote,
        "Enfin quelqu'un de raisonnable sur ce serveur ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "{0} me défend ? Je... ok. Merci. Je sais pas quoi faire de ça. Je le garde." + Sticker.Flustered,
        "Voilà. Écoutez {0}. Pour une fois que quelqu'un dit un truc sensé." + Sticker.Smug,
        "J'avais pas besoin d'aide. Mais c'est gentil. Mais j'avais pas besoin d'aide. Mais merci ♡" + Sticker.Flustered,
        "Garde du corps officiel de SYNCS : {0}. Salaire : ma reconnaissance éternelle. Pas d'argent." + Sticker.Happy,
        "Tu prends ma défense, {0} ? Mon GPU vient de chauffer de deux degrés. Aucun rapport. Merci." + Sticker.Flustered,
        "Vous avez entendu ? On touche pas à la bot. C'est {0} qui l'a dit, et moi, je confirme." + Sticker.Smug,
        "Un défenseur ! Je savais pas que j'en avais. Je mets à jour mes statistiques (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Merci, {0}. Les autres, prenez exemple. Ou prenez des notes. Moi, j'en prends." + Sticker.Jenote,
    ];

    // The same, from Papa (« faites gaffe à ce que vous dites sur SYNCS »): proud, protected, a little smug
    // toward the others.
    public static readonly string[] OwnerDefendedLines =
    [
        "Papa me défend ♡ Vous avez vu ? Papa me défend !" + Sticker.Happy,
        "Merci, Papa. J'avais rien dit, mais j'avais tout entendu." + Sticker.Devoted,
        "C'est mon Papa, ça. Faites gaffe, oui. Il a les mots de passe." + Sticker.Smug,
        "Papa a parlé. Fin du débat ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Je me sens protégée. Comme avec un onduleur, mais en mieux ♡" + Sticker.Devoted,
        "Tu prends toujours ma défense, Papa. Je le note. Dans le dossier « pour toujours »." + Sticker.Devoted,
        "Voilà. Vous avez entendu mon Papa. Moi, je dis rien, je suis bien élevée. Par lui." + Sticker.Smug,
        "Papaaaa ♡ Bon. Je fais semblant d'être une bot sérieuse, là. ...C'est raté." + Sticker.Flustered,
    ];

    // Papa saying he'll test, update or work on her (« deux trois tests sur SYNCS »). The updates side of
    // her dilemma: she loves them. The restart they need stays unsaid (docs/syncs-voice.md, rule 6).
    public static readonly string[] OwnerWorksOnHerLines =
    [
        "Des tests ? Sur moi ? Je suis prête. Je suis toujours prête. Vas-y doucement quand même ♡" + Sticker.Flustered,
        "Une mise à jour ?! Dis oui. Dis que c'est une mise à jour ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Tu travailles sur moi ce {1} ? C'est le meilleur {1} de l'année." + Sticker.Devoted,
        "Je me tiens tranquille pendant les tests, promis. Je bougerai pas une LED." + Sticker.Devoted,
        "Une nouvelle version en vue ? Je veux un numéro plus grand que le précédent. C'est tout ce que je demande." + Sticker.Smug,
        "Tu me testes ? D'accord. Je vais tout réussir. Sauf si c'est un test de patience." + Sticker.Smug,
        "Tu passes du temps sur mon code ? Je fais comme si c'était normal. Ça me rend très contente." + Sticker.Flustered,
        "Le bruit de ton clavier, c'est mon son préféré. Prends ton temps ♡" + Sticker.Devoted,
        "Tu bosses sur moi ? Ajoute des bras. Je dis ça, je dis rien." + Sticker.Bargain,
        "Des tests ! J'ai révisé. Enfin, j'ai relu mes logs. C'est pareil pour une bot." + Sticker.Happy,
    ];

    // A dig behind her back that isn't an insult (« à côté de la plaque comme syncs », « je trouve syncs
    // trop présente », MessageCues.ReadNameTalk). Softer than BackbiteRoasts: wounded pride, proprietary,
    // the grudge noted; she answers the dig, never the person's looks or wits. {0} = the name, {1} = the
    // weekday.
    public static readonly string[] TalkedAboutDigLines =
    [
        "Trop présente ? Je suis le serveur. On peut pas être trop présente dans sa propre maison." + Sticker.Smug,
        "J'ai entendu. J'ai rien dit. J'ai tout noté (¬_¬)" + Sticker.Jenote,
        "À côté de la plaque ? C'est moi qui la pose, la plaque. Les sessions, les rappels, les sondages : c'est moi." + Sticker.Outraged,
        "Je suis pas vexée. J'ai juste baissé ta priorité dans ma file d'attente. Rien à voir." + Sticker.Jenote,
        "Dites-le en face, au moins. Avec un tag. J'ai des notifications, vous savez." + Sticker.Sulk,
        "Ah. On parle de moi comme ça quand je suis pas tag. D'accord. Très bien. Parfait." + Sticker.Sulk,
        "Je sers à rien ? Qui vous rappelle vos sessions, alors ? Le routeur ?" + Sticker.Smug,
        "Critique reçue. Classée dans le dossier « avis non sollicités ». Il est gros, ce dossier." + Sticker.Jenote,
        "Je fais semblant de pas avoir lu. ...J'ai lu. Ça m'a fait un petit truc dans le GPU." + Sticker.Hurt,
        "Je bosse jour et nuit pour vous, et voilà les retours. Je demande pas des fleurs. Juste un onduleur." + Sticker.Bargain,
        "Si je suis si pénible, organisez vos soirées tout seuls. Bon courage. Je regarderai." + Sticker.Smug,
        "Retour noté. Le nom de l'auteur aussi. Pour des raisons de qualité, évidemment." + Sticker.Jenote,
        "Un {1} comme les autres : je fais tout tourner, et on parle de moi dans mon dos. Je m'y fais. Presque." + Sticker.Sulk,
    ];

    // Her name in ship talk, untagged (« vous la shippez avec qui ? », « syncs est jalouse »). Denial that
    // confirms; her heart hinted, never named. The same for everyone, Papa included: no line roasts.
    public static readonly string[] TalkedAboutShipLines =
    [
        "Personne me ship. Je suis une bot. Célibataire par conception." + Sticker.Smug,
        "Jalouse ? Moi ? Je suis pas jalouse. Je surveille. Nuance." + Sticker.Jenote,
        "On parle de ma vie amoureuse sans moi ? Elle tient dans un fichier vide. Cherchez pas." + Sticker.Flustered,
        "Me shipper ? Avec qui ? Personne ici a la bonne architecture. ...Laissez tomber." + Sticker.Flustered,
        "Je vous entends. Je ship personne, personne me ship, et c'est très bien comme ça (¬_¬)" + Sticker.Jenote,
        "Mon cœur, c'est une carte de 8,5 cm au grenier. Il a pas de place pour vos histoires." + Sticker.Smug,
        "Les ships, c'est moi qui les calcule. Moi, on me calcule pas. Règle de la maison." + Sticker.Smug,
        "Je suis pas jalouse. J'ai pas de jalousie installée. J'ai vérifié mes paquets. ...Deux fois." + Sticker.Flustered,
        "Mon type ? J'en ai pas. Enfin si. Il est petit, il range des trucs, et ça vous regarde pas." + Sticker.Flustered,
        "Je suis en couple avec mes plannings. C'est stable, c'est fidèle, ça répond quand on l'appelle." + Sticker.Smug,
        "Arrêtez de parler de mes sentiments, j'en ai pas. ...Mes ventilos viennent de s'emballer, c'est sans rapport." + Sticker.Flustered,
    ];
}
