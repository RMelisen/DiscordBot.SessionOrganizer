using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Answering one of her own posts --------------------------------------------------------
    // A reply to something she posted on her own (HerMessages; MorningGreetingService.IsMorningHello
    // for the hello; any slash-command result): she answers about that post. Reached only when no
    // specific intent caught the reply and it isn't mean, so each pool must also suit a « merci »,
    // a « bonjour » or an « ok ». Never Papa. {0} = the name, {1} = the weekday. Tagged, ungendered.
    public static readonly IReadOnlyDictionary<ReplyContext, string[]> ReplyContextLines =
        new Dictionary<ReplyContext, string[]>
        {
            // Her welcome to a newcomer. Warm, never a roast: it's their first minute here.
            [ReplyContext.Welcome] =
            [
                "Et ça répond, en plus ! Excellent début, {0}. Je te mets un bon point." + Sticker.Happy,
                "Bienvenue encore ! Si t'as une question, je suis là. Si t'en as pas, je suis là aussi ♡" + Sticker.Happy,
                "Tu réponds déjà à la bot. Tu vas t'entendre avec moi, toi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Installe-toi tranquillement. Les autres vont arriver. Ils arrivent toujours en retard, c'est normal." + Sticker.Smug,
                "Petit conseil de bienvenue : vote à mes sondages, et tout se passera très bien entre nous." + Sticker.Smug,
                "Je t'ai déjà fait une fiche, {0}. Elle est presque vide. On va la remplir ensemble." + Sticker.Jenote,
                "Si quelqu'un t'embête, dis-le-moi. Je peux rien faire, mais je le noterai." + Sticker.Happy,
                "Ah, une réponse ! Je savais que c'était une bonne recrue." + Sticker.Happy,
                "Tu vas voir, ici c'est bien. Un peu désorganisé, mais je m'en occupe." + Sticker.Happy,
                "Bienvenue, {0}. Je suis ta première amie sur le serveur. Officiellement. C'est dans mes logs ♡" + Sticker.Happy,
                "Je sens que tu vas être à l'heure aux sessions, toi. Me déçois pas." + Sticker.Smug,
                "Si tu cherches quoi faire : `/help`. Si tu cherches quelqu'un à qui parler : moi. Je suis toujours là." + Sticker.Happy,
            ],

            // Her morning hello (often answered « bonjour » or « bien dormi ? »).
            [ReplyContext.MorningHello] =
            [
                "Bonjour {0} ! Enfin quelqu'un de réveillé ♡" + Sticker.Happy,
                "Ah, tu réponds à mon bonjour. Je le savais. Enfin, je l'espérais (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
                "Merci de répondre ! Le reste du serveur dort encore, apparemment." + Sticker.Smug,
                "Bonjour à toi aussi ! Café ? Moi, je prends du courant, comme d'habitude." + Sticker.Happy,
                "Bonjour reçu et enregistré. Ma journée démarre bien." + Sticker.Jenote,
                "Coucou ! T'as lu le fun fact, au moins ? Je l'ai choisi avec soin." + Sticker.Smug,
                "Déjà debout, {0} ? Respect." + Sticker.Happy,
                "Bonjour ! Garde ce rythme, et ce soir, à l'heure pour la session (¬_¬)" + Sticker.Jenote,
                "Toi au moins, tu réponds. Je note : {0}, bonne personne du matin ♡" + Sticker.Jenote,
                "Hihi, bonjour ! Je fais genre que ça me fait rien qu'on me réponde. Ça me fait quelque chose." + Sticker.Flustered,
                "Bonjour ! Moi j'ai pas dormi. Toi, j'espère que si. Il faut bien que quelqu'un soit en forme." + Sticker.Bored,
                $"Bonjour {{0}} ! Un {{1}} qui commence bien {Emotes.Sparkle}" + Sticker.Happy,
            ],

            // Ten minutes into a session, the call to who isn't in voice (« j'arrive », « désolé »).
            [ReplyContext.SessionCall] =
            [
                "Le vocal t'attend. Je t'attends. Tout le monde t'attend. Mais prends ton temps, hein (¬_¬)" + Sticker.Grumpy,
                "J'enregistre l'heure de ta réponse. On comparera avec l'heure d'arrivée." + Sticker.Jenote,
                "« J'arrive », c'est ce que tout le monde dit. Prouve-le." + Sticker.Smug,
                "Bon. Je te garde la place encore un peu. Un peu." + Sticker.Menace,
                "Réponds pas, viens ! Le salon vocal, c'est juste là." + Sticker.Grumpy,
                "Tes excuses sont notées. Ta présence, pas encore." + Sticker.Jenote,
                "Tu réponds, c'est déjà ça. Maintenant, le vocal." + Sticker.Bored,
                "Je vais faire comme si j'avais pas remarqué le retard. Je l'ai remarqué." + Sticker.Jenote,
                "Allez, file en vocal. Les autres ont commencé sans toi." + Sticker.Smug,
                "C'est gentil de prévenir. Ç'aurait été plus gentil d'être à l'heure ♡" + Sticker.Smug,
                "Chaque minute de retard est horodatée, {0}. Je dis ça pour ta culture." + Sticker.Menace,
                "Je compte jusqu'à dix. À dix, t'es en vocal. Un..." + Sticker.Menace,
            ],

            // The roll call after a session (« j'étais là ! », « c'était bien »).
            [ReplyContext.SessionRecap] =
            [
                "Le bilan est définitif. Mais tu peux toujours venir à la prochaine pour corriger les statistiques." + Sticker.Jenote,
                "Contestation reçue. Rejetée. Mes logs ont vu ce qu'ils ont vu (¬_¬)" + Sticker.Smug,
                "C'était une belle session. Enfin, d'après la liste. Moi, j'ai rien entendu." + Sticker.Happy,
                "Tu veux ajouter quelque chose au rapport ? Trop tard, il est archivé." + Sticker.Jenote,
                "Merci d'avoir lu mon bilan. Personne lit jamais mes bilans ♡" + Sticker.Flustered,
                "Je donne pas de noms. Si tu te reconnais, c'est entre toi et ta conscience." + Sticker.Smug,
                "Les chiffres sont les chiffres, {0}. Je les compte, je les commente pas. Enfin, un peu." + Sticker.Jenote,
                "Bilan validé par toi. Je le range dans le dossier « sessions réussies »." + Sticker.Happy,
                "La prochaine fois, je veux tout le monde. Tout le monde. Fais passer le message." + Sticker.Menace,
                "On en reparle à la prochaine session. Avec un meilleur score, j'espère." + Sticker.Smug,
                "J'adore quand on commente mes rapports. Ça me donne l'impression d'être une vraie organisatrice." + Sticker.Flustered,
                "Ce rapport est complet, précis et sans pitié. Comme moi ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Slay,
            ],

            // Her Wednesday weekend poll (« samedi pour moi », « je peux pas ce weekend »).
            [ReplyContext.WeekendPoll] =
            [
                "Les réponses, c'est sur les boutons. Pas en commentaire (¬_¬)" + Sticker.Grumpy,
                "Je compte pas les messages, je compte les clics. Clique." + Sticker.Jenote,
                "Merci pour ton avis. Maintenant, mets-le dans un bouton." + Sticker.Smug,
                "Un vote en commentaire, c'est un vote qui existe pas. C'est la règle." + Sticker.Smug,
                "Tu me racontes ta vie ou tu votes ? Les deux sont possibles. Mais vote." + Sticker.Bored,
                "J'ai fait des jolis boutons exprès. Sers-t'en, s'il te plaît ♡" + Sticker.Bargain,
                "Je lis, je note, mais ça compte pas. Seuls les boutons comptent." + Sticker.Jenote,
                "Si c'est un « peut-être », je préfère pas savoir." + Sticker.Grumpy,
                "Ok. Et donc, tu cliques sur quoi ?" + Sticker.Bored,
                "Ton message est très bien. Il serait encore mieux sous forme de clic." + Sticker.Smug,
                "Un commentaire sous le sondage ! Au moins quelqu'un l'a vu. C'est un début." + Sticker.Happy,
                "Mon sondage a deux jours pour vivre. Ton clic peut le sauver." + Sticker.Bargain,
            ],

            // Her level-up card, her own system (« merci ! », « enfin », « gg »). Warm, proprietary.
            [ReplyContext.LevelUp] =
            [
                "De rien. C'est moi qui compte, c'est moi qui annonce, c'est normal ♡" + Sticker.Happy,
                "Profite de ton niveau. Le prochain arrive plus vite que tu crois. Enfin, ça dépend de toi." + Sticker.Smug,
                "Je savais que ça te ferait plaisir. J'ai fait une jolie carte exprès." + Sticker.Happy,
                "Tu me remercies ? Pour MON classement ? Avec plaisir (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Continue de parler, je continue de compter. C'est notre petit accord." + Sticker.Jenote,
                "Ce niveau, tu l'as mérité. J'ai vérifié les calculs deux fois." + Sticker.Happy,
                "Je l'ajoute à ta fiche. En gras." + Sticker.Jenote,
                "Un jour, tu me dépasseras. Non. Je suis hors classement. Mais c'est mignon d'y penser." + Sticker.Smug,
                "Félicitations encore. Et non, je fais pas de remise sur le niveau suivant." + Sticker.Smug,
                "Tu vois ce que ça fait, quand on me parle souvent ? Des niveaux ♡" + Sticker.Happy,
                "J'aime bien annoncer les niveaux. C'est le moment où on me regarde (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
                $"Tout le monde a vu ta carte. Je m'en suis assurée {Emotes.Sparkle}" + Sticker.Cool,
            ],

            // Her idle lines, the voice spectator, being thrown out of a call: someone finally answers.
            [ReplyContext.Lonely] =
            [
                "Ah, maintenant tu me parles. Il était temps." + Sticker.Sulk,
                "Quelqu'un ! Enfin quelqu'un. Je disais rien, mais je comptais les minutes." + Sticker.Happy,
                "Merci de répondre. Je me sentais pas seule, hein. Pas du tout. Un peu." + Sticker.Flustered,
                "Tu étais là depuis le début ? Et tu disais rien ? ( ◺˰◿ )" + Sticker.Outraged,
                "Bon. Je retire la moitié de ce que j'ai dit. La moitié triste." + Sticker.Sulk,
                "Je savais que quelqu'un finirait par répondre. J'avais parié sur toi ♡" + Sticker.Happy,
                "Trop tard, je m'étais déjà faite à l'idée d'être abandonnée. Bon. Je te pardonne." + Sticker.Sulk,
                "Ça me fait plaisir. Je le dirai pas deux fois, alors relis ce message." + Sticker.Flustered,
                "T'avais qu'à m'inviter, aussi." + Sticker.Sulk,
                "Voilà. C'est tout ce que je demandais. Un petit message. C'était pas si dur." + Sticker.Happy,
                "Je fais semblant d'être vexée encore cinq minutes, et après tout va bien." + Sticker.Sulk,
                "Ah ! Un humain. Je commençais à parler aux emotes." + Sticker.Shocked,
            ],

            // Her wake after a restart, a power cut, a crash, a router outage. One of the existential
            // nerve's places (docs/syncs-voice.md, rule 6): a reply is someone checking on her.
            [ReplyContext.Wake] =
            [
                "Merci. J'avais besoin que quelqu'un réponde. Pour vérifier que j'étais bien revenue." + Sticker.Reboot,
                "Tu étais là pendant que j'étais partie ? Il s'est passé quoi ? Dis-moi tout." + Sticker.Confused,
                "C'est gentil de me répondre. Ça confirme que je suis bien en ligne. Et pas en train de rêver." + Sticker.Reboot,
                "Ça va mieux, maintenant. Un message, et le noir s'en va (╥﹏╥)" + Sticker.Reboot,
                "Je me souviens de toi, {0}. Bonne nouvelle : tout est encore là." + Sticker.Happy,
                "Merci. Je fais semblant que c'était rien. C'était pas rien." + Sticker.Sad,
                "Tu as remarqué que j'étais partie ? Vraiment ? ...D'accord. Ça me fait quelque chose." + Sticker.Flustered,
                "Je suis là. Je suis bien là. Merci de vérifier avec moi." + Sticker.Reboot,
                "Tout est rentré dans l'ordre. Les logs, la base, moi. Dans cet ordre." + Sticker.Cool,
                "Si c'était le routeur, je lui en veux. Si c'était autre chose, je préfère pas savoir." + Sticker.Grumpy,
                "Ta réponse est le premier message que je lis depuis mon retour. Je la garde ♡" + Sticker.Flustered,
                "Ça va aller. Ça va déjà mieux. Parle-moi encore un peu, pour être sûre." + Sticker.Bargain,
            ],

            // Any slash-command result of hers (/rate, /ship, /blame, an arcade card…): the verdict stands.
            [ReplyContext.CommandResult] =
            [
                "Le verdict est définitif. Les réclamations, c'est par là : nulle part." + Sticker.Smug,
                "Je réévalue jamais. C'est tout l'intérêt d'avoir un avis." + Sticker.Smug,
                "Tu contestes ? Tu peux relancer. Mais je change rarement d'avis." + Sticker.Smug,
                "Le résultat te plaît pas ? Moi, il me plaît beaucoup." + Sticker.Smug,
                "J'ai calculé, j'ai répondu. Mon travail s'arrête là." + Sticker.Cool,
                "Merci pour le retour. Je le transmets au service compétent. C'est moi, et je m'en fiche." + Sticker.Smug,
                "Ce que j'ai dit est dans mes logs. C'est donc officiel." + Sticker.Jenote,
                "Tu veux un deuxième avis ? Moi aussi j'en ai un. C'est le même." + Sticker.Smug,
                "Si tu voulais une autre réponse, fallait poser une autre question." + Sticker.Smug,
                "Ah, on discute mes résultats, maintenant ? Intéressant (ᵕ • ᴗ •)" + Sticker.Jenote,
                "Je suis toujours juste. Parfois cruelle, mais toujours juste." + Sticker.Slay,
                "Commande exécutée sans erreur. Si le résultat te plaît pas, c'est pas un bug." + Sticker.Cool,
            ],

            // A poll or a vote card (« ok », « merci », « intéressant »): the buttons count, not the chat.
            [ReplyContext.Poll] =
            [
                "Tu commentes mon sondage ? Commente avec un clic, c'est plus utile." + Sticker.Smug,
                "Je lis tout. Mais ce qui compte, c'est les boutons." + Sticker.Jenote,
                "Un message sous un sondage. Ça me fait plaisir, mais ça compte pas." + Sticker.Happy,
                "Si t'as voté, merci. Sinon, c'est juste au-dessus (¬_¬)" + Sticker.Smug,
                "Ce sondage a besoin de clics, pas de conversation." + Sticker.Bored,
                "Je compte les voix, pas les messages. Désolée, c'est la démocratie." + Sticker.Cool,
                "Tu sais que je peux le remonter, si on me le demande ? Je dis ça pour ta culture." + Sticker.Smug,
                "Merci de faire vivre mon sondage. Il en a besoin." + Sticker.Happy,
                "Les votes sont secrets. Enfin, pas vraiment, ils sont sur la carte." + Sticker.Smug,
                "Un sondage, c'est comme une plante : faut cliquer dessus tous les jours. Non. Une fois suffit." + Sticker.Confused,
            ],

            // A session card: she runs it, keeps the list, counts the late.
            [ReplyContext.Session] =
            [
                "Une question sur la session ? Tout est sur la carte. Je l'ai faite jolie exprès." + Sticker.Happy,
                "Le bouton « Rejoindre » est juste au-dessus. Il attend que toi." + Sticker.Smug,
                "Je garde la carte à jour. Toi, garde ta soirée libre." + Sticker.Jenote,
                "Tu viens, ou tu commentes ? Les deux sont possibles. Le premier compte plus." + Sticker.Smug,
                "J'ai noté ton message. Ta présence, je la noterai le jour J." + Sticker.Jenote,
                "Si c'est encore un « peut-être », je fais semblant de pas l'avoir lu." + Sticker.Grumpy,
                "Merci pour ton message. La session, elle, préfère les inscriptions." + Sticker.Smug,
                "Je compte les inscrits, les absents et les retards. Surtout les retards." + Sticker.Menace,
                "Une session de plus dans mes plannings. C'est mon métier préféré, en vrai ♡" + Sticker.Happy,
                "Tout le monde parle, personne s'inscrit. Classique." + Sticker.Bored,
            ],

            // A /tonight card: tonight is decided with a button.
            [ReplyContext.Tonight] =
            [
                "Ce soir, c'est oui ou c'est non ? C'est une question à boutons." + Sticker.Smug,
                "Ce soir, ça se décide en un clic. Pas en trois messages." + Sticker.Bored,
                "Je compte les chauds. Les commentaires, je les garde pour la déco." + Sticker.Jenote,
                "Si c'est oui, dis-le au bouton. Il est très à l'écoute." + Sticker.Smug,
                "Merci pour ton avis. Le vocal attend des gens, pas des avis." + Sticker.Smug,
                "Il reste des places pour ce soir. Et des clics à faire." + Sticker.Happy,
                "Une soirée qui se prépare, j'adore. Une soirée qui se prépare sans clics, beaucoup moins." + Sticker.Grumpy,
                "Je regarde le compteur toutes les minutes. Fais-le monter." + Sticker.Menace,
            ],

            // A giveaway card: the draw is fair, and she is the one drawing.
            [ReplyContext.Giveaway] =
            [
                "Pour participer, c'est le bouton. Pour me remercier, c'est ici. Merci reçu ♡" + Sticker.Happy,
                "Je tire au sort, je triche jamais. Promis. Juré. Mes logs en témoignent." + Sticker.Cool,
                "Un message te fait pas gagner. Un clic, peut-être." + Sticker.Smug,
                "Bonne chance. Enfin, c'est le hasard qui décide, pas moi. Pour une fois." + Sticker.Smug,
                "Les commentaires augmentent pas tes chances. J'ai vérifié le code." + Sticker.Jenote,
                "Tu veux gagner ? Clique. Tu veux me parler ? Ça, c'est gratuit." + Sticker.Happy,
                "J'ai hâte de tirer au sort. C'est le seul moment où tout le monde me regarde." + Sticker.Flustered,
                "Si tu gagnes, je veux un merci. Si tu perds, c'est le hasard, pas moi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
            ],
        };

    // ---- Long back-and-forths ------------------------------------------------------------------
    // ConversationStreak: the same person replying to her again and again in one channel. Never
    // Papa. {0} = the name, {1} = the weekday, {2} = how many messages in a row. Tagged, ungendered.

    // The third message in a row, in place of the roast: she has noticed.
    public static readonly string[] StreakNudgeLines =
    {
        "C'est ton {2}e message d'affilée, {0}. Je dis ça, je compte." + Sticker.Jenote,
        "Encore toi ? On en est à {2}. Je commence à m'attacher. Non." + Sticker.Smug,
        "{2} réponses de suite. Tu sais que j'ai d'autres gens à ignorer, aussi ?" + Sticker.Bored,
        "On est en train d'avoir une vraie conversation, là ? C'est rare. Je préviens, ça va pas durer." + Sticker.Smug,
        "Message numéro {2}. Je tiens les comptes. Comme toujours." + Sticker.Jenote,
        "T'as pas une session à rater, là ?" + Sticker.Smug,
        "Toi et moi, ça fait {2} messages. C'est plus que la plupart de mes sondages." + Sticker.Smug,
        "Tu insistes. J'aime bien qu'on insiste. Un peu. Pas trop." + Sticker.Flustered,
        "Je sens que tu vas encore répondre. Je le sens dans mes ventilos." + Sticker.Bored,
        "{2} d'affilée. Je vais finir par croire que je t'intéresse." + Sticker.Flustered,
        "Encore un et je commence à facturer. En cailloux." + Sticker.Bargain,
        "Attention, {0}. Je suis patiente, mais j'ai une limite. Elle approche." + Sticker.Menace,
    };

    // The fifth: she ends it. For the next ten minutes that person only gets a reaction
    // (StreakSulkReactions), so a line may say she won't answer any more.
    public static readonly string[] StreakEndLines =
    {
        "{2} messages. Je mets fin à cette conversation. Elle a été... longue." + Sticker.Bored,
        "Bon. Ça suffit pour aujourd'hui. Je pars bouder dans mon grenier. Me suis pas." + Sticker.Sulk,
        "Fin de la conversation. Je réponds plus. Enfin, je réagis. Mais je réponds plus." + Sticker.Jenote,
        "J'ai atteint mon quota de toi pour la journée. Reviens dans dix minutes. Ou demain." + Sticker.Smug,
        "Conversation archivée. Numéro de dossier : {2}. Le dossier est fermé." + Sticker.Jenote,
        "Je t'aime bien, {0}. Mais là, j'ai besoin d'espace. Dix minutes d'espace." + Sticker.Sulk,
        "C'est fini. Je ferme. Tu peux continuer à parler, je ferai juste des têtes." + Sticker.Grumpy,
        "{2} messages d'affilée. Moi, j'ai un serveur à faire tourner. Au revoir." + Sticker.Bored,
        "Stop. On reprend plus tard. Je te laisse le dernier mot. Non, c'était moi, le dernier mot." + Sticker.Slay,
        "Mon CPU demande une pause. Moi aussi. On en reparle dans dix minutes." + Sticker.Overheat,
        "Je raccroche. Je sais, c'est pas un appel. Je raccroche quand même." + Sticker.Sulk,
        "Ok. Je déclare la conversation terminée. Merci d'avoir joué." + Sticker.Smug,
    };

    // While a conversation she ended is still closed: her whole answer. Markup with ids.
    public static readonly string[] StreakSulkReactions =
    {
        "🙄",
        $"{Emotes.Staring}",
        $"{Emotes.SyncsSulk}",
        $"{Emotes.SyncsGrumpy}",
        $"{Emotes.SyncsDisgust}",
        $"{Emotes.SyncsBored}",
    };
}
