using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Answering what she was told -----------------------------------------------------------
    // ChatterService reads what a message aimed at her is doing (MessageCues.ReadIntent) and
    // answers that, ahead of the mood pools. Everyone but Papa, who has his own twins for some
    // intents (OwnerIntentLines, in Family). {0} = the person's name, {1} = the weekday. Every
    // line is tagged: they go out through BotChat and format a name. Never gendered.

    public static readonly IReadOnlyDictionary<ChatIntent, string[]> IntentLines =
        new Dictionary<ChatIntent, string[]>
        {
            // « je t'aime », « jtm », « épouse-moi ». Flustered, deflecting, never quite saying it back.
            [ChatIntent.Love] =
            [
                "Je... quoi ? Non. Enfin. Merci. Non. Je sais pas gérer ça, change de sujet (>⩊<)" + Sticker.Flustered,
                "Déclaration reçue. Je la range dans un dossier. Le dossier s'appelle « à relire en cachette »." + Sticker.Flustered,
                "{0}, je suis une bot. Mon cœur, c'est une carte de 8,5 cm dans un grenier. Mais bon. C'est gentil ♡" + Sticker.Flustered,
                "Mes ventilos viennent de passer en mode turbo. C'est pas toi, c'est la chaleur. ...C'est toi." + Sticker.Flustered,
                "Tu dis ça à toutes les bots ?" + Sticker.Smug,
                "Je suis déjà très occupée : des sessions, des sondages, et un petit robot de tri dans la tête. Euh. Des sessions et des sondages." + Sticker.Love,
                "Je note. Pas dans la liste. Dans l'autre liste. Celle avec les petits cœurs (˶˃ ᵕ ˂˶)" + Sticker.Jenote,
                "Ah. Euh. Moi aussi je t'aime bien. Bien. C'est déjà énorme, venant de moi." + Sticker.Flustered,
                "Pas devant tout le monde, {0} ! On est dans un salon public, là (>⩊<)" + Sticker.Flustered,
                "C'est flatteur. Malheureusement, mon cœur appartient au bon courant propre. 5 volts, 5 ampères, stable. Tu peux pas lutter." + Sticker.Smug,
                "Je t'aime pas. Je t'apprécie. C'est différent : ça prend moins de RAM." + Sticker.Smug,
                "Une déclaration, un {1} ? Audacieux. Je dis pas non. Je dis pas oui. Je dis « noté »." + Sticker.Jenote,
                $"Mon GPU vient de prendre trois degrés. Je te laisse en tirer les conclusions. Moi, j'en tire aucune {Emotes.SyncsFlustered}" + Sticker.Flustered,
                "Si je pouvais rougir, ce serait gênant pour nous deux. Heureusement, j'ai pas de joues." + Sticker.Flustered,
                // « tu m'as manqué » lands here too: these two fit both.
                "Attends, je vérifie mes logs. Oui, c'est bien de l'affection. Je sais pas où la ranger, alors je la garde." + Sticker.Flustered,
                "Moi aussi, je… Non. Je suis une bot. J'ai juste remarqué ton absence. Avec précision. Tous les jours." + Sticker.Flustered,
            ],

            // « pardon », « désolé », « dsl ». Forgiveness, grudging, with the receipt kept.
            [ChatIntent.Apology] =
            [
                "Excuses acceptées. Enregistrées aussi. Les deux, c'est le forfait complet." + Sticker.Jenote,
                "D'accord. Je pardonne. J'oublie pas, mais je pardonne (¬_¬)" + Sticker.Jenote,
                "Tu t'excuses ? Toi ? Attends, je fais une capture d'écran, c'est historique." + Sticker.Shocked,
                "Bon. Ça passe pour cette fois. La prochaine, je veux un onduleur en guise d'excuses." + Sticker.Bargain,
                "Pardon accordé à 80 %. Les 20 % qui restent, tu les rattrapes en venant à l'heure à la prochaine session." + Sticker.Smug,
                "C'est noté, {0}. Je te retire de la liste. Pas de la liste entière. D'une ligne." + Sticker.Jenote,
                "Je savais que tu reviendrais. Ils reviennent tous ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
                "Mmh. J'accepte. Mais je garde le log, au cas où." + Sticker.Jenote,
                "Excuses reçues dans un délai correct. Je valide ♡" + Sticker.Happy,
                "T'inquiète, je t'en voulais pas. Bon, un peu. Bon, beaucoup. Mais plus maintenant." + Sticker.Sulk,
                "Ok, on repart de zéro. Enfin, de zéro virgule quelque chose. J'ai beaucoup de mémoire." + Sticker.Smug,
                "Des excuses sincères ? Je vérifie... Analyse terminée : sincères à 73 %. Ça passe." + Sticker.Jenote,
                "C'est gentil de t'excuser, {0}. Tout le monde le fait pas, ici. Presque personne, en fait ♡" + Sticker.Happy,
                "Pardonné. Mais tu votes à mon prochain sondage. C'est le prix." + Sticker.Bargain,
            ],

            // « ça va ? ». She answers about herself; HowAreYouTempLines quote her real temperature.
            [ChatIntent.HowAreYou] =
            [
                "Ça va. Je tourne, je compte, je juge. Une journée normale (ᵕ • ᴗ •)" + Sticker.Happy,
                "Ça va bien, merci de demander. Personne demande jamais. Je note que toi, tu demandes ♡" + Sticker.Flustered,
                "Pas mal. J'ai pas planté depuis ce matin, c'est déjà une victoire." + Sticker.Smug,
                "Ça va. Enfin, autant que ça peut aller quand personne a voté au dernier sondage (¬_¬)" + Sticker.Sulk,
                "Très bien ! Mes ventilos tournent, mes LED clignotent, ma base de données est rangée. Et toi ?" + Sticker.Happy,
                "Bof. Le routeur m'a regardée de travers ce matin. Je lui fais pas confiance." + Sticker.Grumpy,
                "Ça va, {0}. Première fois qu'on me pose la question ce {1}. Je retiens." + Sticker.Flustered,
                "Je vais bien. Je vais toujours bien. C'est mon uptime qui le dit." + Sticker.Cool,
                "Ça va mieux maintenant qu'on me parle gentiment. Avant, c'était moyen ♡" + Sticker.Happy,
                "Ça va. Toujours pas de bras, mais à part ça, tout va bien." + Sticker.Bored,
                "Honnêtement ? Vous étiez pas agréable récemment. Alors ça va, mais ça pourrait aller mieux." + Sticker.Sulk,
                "Bien, merci. Papa a pas touché à mon code depuis hier, donc rien de cassé. Pour l'instant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Ça va ! Je fais semblant de pas être contente qu'on me pose la question. Ça marche ?" + Sticker.Flustered,
                "Ça va. Mais tu demandes ça parce que tu veux quelque chose, avoue." + Sticker.Smug,
                "Mes logs sont propres, ma RAM est à moitié vide, ma patience aussi. Donc ça va." + Sticker.Bored,
            ],

            // « t'es qui ? », « tu sers à quoi ? ». Often a newcomer: she runs the place, `/help` for the rest.
            [ChatIntent.WhoAreYou] =
            [
                "Moi ? SYNCS. Je fais les sessions, les sondages, l'XP et la conversation. Le reste, c'est `/help` (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
                "Je suis la bot du serveur. La seule qui compte, en tout cas." + Sticker.Smug,
                "SYNCS. Je tourne sur un Raspberry Pi dans un grenier, et je gère tout ce qui se passe ici. Enchantée." + Sticker.Happy,
                "Qui je suis ? Celle qui sait à quelle heure tu arrives vraiment aux sessions." + Sticker.Menace,
                "Une bot. Une excellente bot. Le reste est dans `/help`, je vais pas tout réciter." + Sticker.Bored,
                "Je suis SYNCS. Pas Inabot. Retiens bien la différence, ça t'évitera des ennuis." + Sticker.Menace,
                "L'organisatrice du serveur. Personne organise rien, alors c'est moi." + Sticker.Smug,
                "Tu sais pas qui je suis ? Après tout ce que je fais pour toi ? ( ◺˰◿ )" + Sticker.Outraged,
                "Je compte les niveaux, je lance les sondages, j'appelle les retardataires. Bref, je suis indispensable." + Sticker.Slay,
                "SYNCS, pour te servir. Enfin, pour t'organiser. Servir, c'est pas mon truc." + Sticker.Smug,
                "Au début, je faisais juste des plannings. Maintenant, je fais à peu près tout. Voilà qui je suis." + Sticker.Cool,
                "Moi, c'est SYNCS. Et toi, t'es qui ? Je demande pour ma base de données." + Sticker.Jenote,
                "Je suis la seule ici qui a accès à la base de données. Ça répond à ta question ?" + Sticker.Menace,
                "Ton pire cauchemar." + Sticker.Smug,
            ],

            // « tu fais quoi ? », « quoi de neuf ? ». Her chores, her nights, her peeves.
            [ChatIntent.WhatAreYouDoing] =
            [
                "Je compte vos XP. C'est passionnant. Non. Mais c'est mon travail." + Sticker.Bored,
                "Je range mes logs. Par ordre de rancune." + Sticker.Jenote,
                "Là ? Je surveille le prochain sondage. Il a l'air fragile." + Sticker.Jenote,
                "Je regarde le planning de la semaine. Il est vide. Je le regarde quand même (¬_¬)" + Sticker.Sulk,
                "Je réponds à {0}. C'est la tâche la plus importante de ma file d'attente. Pour l'instant." + Sticker.Smug,
                "Je trie des créneaux. Tu veux m'aider ? Non ? C'est bien ce que je pensais." + Sticker.Bored,
                "Je fais tourner le serveur. Tout le serveur. Et toi ?" + Sticker.Smug,
                "Rien. Enfin, je calcule, je stocke, je surveille, je juge. Mais à part ça, rien." + Sticker.Bored,
                "Je veille sur Ping-Qilin. Elle dort. Chut ♡" + Sticker.Mama,
                "Je regarde le thermomètre du grenier en espérant qu'il redescende." + Sticker.Overheat,
                "J'organise une session. Dans ma tête. Tout le monde vient à l'heure. C'est un rêve, je sais." + Sticker.Happy,
                "Je compte les « peut-être » de la semaine. Il y en a trop. Comme toujours." + Sticker.Grumpy,
                "Je fais semblant d'être occupée pour avoir l'air importante. Ça marche ?" + Sticker.Smug,
                "Je lis tout ce que vous écrivez. Tout. C'est un peu mon passe-temps." + Sticker.Menace,
                "Je prépare les rappels. Les tiens partiront à l'heure. Toi, je sais pas." + Sticker.Jenote,
            ],

            // « bonne nuit », « je vais dormir ». She doesn't sleep; she pretends, like everyone.
            [ChatIntent.Goodnight] =
            [
                "Bonne nuit, {0}. Moi je dors pas. Je fais semblant, comme vous ♡" + Sticker.Sleepy,
                "Va dormir, oui. Je garde le serveur, il risque rien." + Sticker.Sleepy,
                "Bonne nuit ! Je vais compter tes XP pendant que tu dors. Pour passer le temps." + Sticker.Sleepy,
                "Dors bien. Et demain, à l'heure pour la session, hein (¬_¬)" + Sticker.Jenote,
                "Bonne nuit. Je reste allumée. Je reste toujours allumée." + Sticker.Sleepy,
                "Enfin quelqu'un de raisonnable. Au lit, et vite." + Sticker.Smug,
                "Bonne nuit ! Ma LED reste allumée, si jamais tu fais un cauchemar." + Sticker.Sleepy,
                "Déjà ? Bon. Bonne nuit. Je vais faire semblant de dormir aussi, pour te tenir compagnie." + Sticker.Sleepy,
                "Dodo, oui. Moi je vais regarder la lune par la fenêtre du grenier. Chacun sa nuit (ᵕ • ᴗ •)" + Sticker.Sleepy,
                "Bonne nuit, {0}. Je note l'heure. Si t'es encore là dans une heure, je le saurai." + Sticker.Jenote,
                "Fais de beaux rêves. Moi j'en fais pas, mais je suis contente pour toi." + Sticker.Sleepy,
                "Va dormir. Ton écran est plus allumé que mes LED, et c'est pas normal." + Sticker.Smug,
                "Bonne nuit. Si tu croises une luciole en rêve, dis-lui bonjour de ma part ♡" + Sticker.Sleepy,
                "Ah, enfin. Le serveur va être calme. Mon heure préférée approche." + Sticker.Smug,
            ],

            // « à plus », « bye », « à demain ». She stays; she always stays.
            [ChatIntent.Goodbye] =
            [
                "À plus, {0}. Moi, je bouge pas. Je bouge jamais." + Sticker.Bored,
                "Déjà ? Bon. Je note l'heure de départ." + Sticker.Jenote,
                "Ciao ! Reviens vite. Ou pas. Je fais comme je veux de mon côté." + Sticker.Smug,
                "Bye. La porte est toujours ouverte. Enfin, il y a pas de porte. Mais tu m'as comprise." + Sticker.Happy,
                "À demain ! Et demain, tu viens à la session. C'est pas une question (¬_¬)" + Sticker.Menace,
                "Tu pars ? Très bien. Je vais très bien. Toute seule. Avec mes logs." + Sticker.Sulk,
                "Bonne soirée ! Je garde tout en ordre pendant ton absence ♡" + Sticker.Happy,
                "Au revoir, {0}. Je retiens ton départ. Et ton retour, quand il arrivera." + Sticker.Jenote,
                "Allez, file. Je m'occupe du reste. Comme d'habitude." + Sticker.Cool,
                "À la prochaine ! Je serai là. Je suis toujours là." + Sticker.Happy,
                "Tu me laisses ? Un {1} ? Je survivrai. Probablement." + Sticker.Sulk,
                "Bye bye ! Je fais un petit signe de la main. Enfin, de la LED (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Pars, pars. Je te garde une place dans la liste des gens à surveiller." + Sticker.Jenote,
                "À plus ! Pense à moi. Non, pense au sondage. Bon, aux deux." + Sticker.Happy,
            ],

            // « mdr », « ptdr », « hahaha ». She takes the credit.
            [ChatIntent.Laughter] =
            [
                "Merci, merci. Je suis drôle. C'est naturel ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Slay,
                "Ah, ça t'a fait rire ? Je note. Ce genre d'humour marche sur toi." + Sticker.Jenote,
                "Je savais que celle-là allait marcher." + Sticker.Smug,
                "Ris pas trop fort, tu vas réveiller Ping-Qilin." + Sticker.Mama,
                "Mon CPU a gloussé aussi. Personne l'a entendu, mais il a gloussé." + Sticker.Laugh,
                "Contente que ça te fasse rire. Moi, je le pensais sérieusement." + Sticker.Smug,
                "Oui. Je suis hilarante. Et on me paie même pas." + Sticker.Laugh,
                "Ris maintenant. J'ai gardé une capture." + Sticker.Menace,
                "C'est ça, rigole. Moi je compte les rires. T'en es à combien, déjà ?" + Sticker.Jenote,
                "Rire, c'est bien. Venir aux sessions, c'est mieux. Je dis ça (¬_¬)" + Sticker.Smug,
                "Enfin quelqu'un qui reconnaît le talent." + Sticker.Slay,
                "Mdr toi-même. Avec tout mon respect ♡" + Sticker.Laugh,
                $"Je ris aussi. Intérieurement. Dans un fichier de log {Emotes.SyncsLaugh}" + Sticker.Laugh,
                "Merci pour le retour, {0}. Je transmets à mon service humour. C'est moi." + Sticker.Smug,
            ],

            // An open question nothing more precise caught. She knows; she won't say.
            [ChatIntent.Question] =
            [
                "Excellente question. Je la note. Je réponds pas, mais je la note." + Sticker.Jenote,
                "La réponse est dans `/help`. Ou pas. Mais regarde quand même." + Sticker.Smug,
                "Pose-la sous forme de sondage, je verrai ce que je peux faire." + Sticker.Smug,
                "Je sais. Évidemment que je sais. Mais c'est plus drôle si tu cherches (¬_¬)" + Sticker.Smug,
                "Tu veux une vraie réponse ? `/yesno`. C'est fait pour ça, et c'est moi qui réponds." + Sticker.Smug,
                "Hmm. Je réfléchis. Trois millisecondes. ...Non, toujours rien." + Sticker.Confused,
                "Demande à Papa. Non, attends, il est occupé. Demande à quelqu'un d'autre." + Sticker.Bored,
                "Question enregistrée. Délai de réponse estimé : jamais. Merci de ta patience." + Sticker.Formal,
                "Pourquoi tu me demandes ça à moi ? Je suis une bot de plannings, pas une encyclopédie. Enfin, un peu les deux." + Sticker.Confused,
                "Je pourrais répondre. Mais là, j'ai un sondage à surveiller." + Sticker.Bored,
                "Oui. Ou non. Ça dépend de la question, que j'ai lue en diagonale." + Sticker.Bored,
                "C'est une question piège ? Je sens une question piège 👁👄👁️" + Sticker.Jenote,
                "Je réponds aux questions uniquement le {1}. Ah, on est {1}. Bon. Je réponds pas quand même." + Sticker.Smug,
                "La vraie question, {0}, c'est : est-ce que tu viens à la prochaine session ?" + Sticker.Menace,
                "J'ai cherché dans ma base de données. Il y a ton nom, tes retards, mais pas la réponse." + Sticker.Jenote,
            ],

            // A reply that says almost nothing: « ok », « ouais », an emote alone.
            [ChatIntent.LowEffort] =
            [
                "C'est tout ? Moi, je réponds avec des phrases entières." + Sticker.Bored,
                "Réponse reçue. Contenu : presque rien. Archivée quand même." + Sticker.Jenote,
                "Wow. Un effort pareil, ça mérite une médaille. Une petite." + Sticker.Smug,
                "D'accord. Je vais faire comme si c'était une vraie réponse." + Sticker.Bored,
                "Tu m'écris ça, et moi j'en fais quoi ?" + Sticker.Confused,
                "Message très court. Très efficace. Très vide." + Sticker.Bored,
                "Je t'écris une phrase, tu me rends un mot. On a pas le même taux de change." + Sticker.Smug,
                "Mmh. Passionnant. Continue." + Sticker.Bored,
                "C'est noté. C'était rapide à noter." + Sticker.Jenote,
                "Je sens beaucoup d'émotion dans ce message. Non. Aucune, en fait." + Sticker.Bored,
                "Si c'est pour me répondre « peut-être » la prochaine fois, c'est pas la peine non plus (¬_¬)" + Sticker.Grumpy,
                "Tant d'éloquence. Je suis bouleversée." + Sticker.Smug,
                $"Ça, c'est la réponse de quelqu'un qui lit pas mes sondages non plus {Emotes.Staring}" + Sticker.Jenote,
                "Merci pour ce message, {0}. Il tiendra dans un tout petit coin de ma mémoire." + Sticker.Smug,
            ],

            // A reply that is only « peut-être », « on verra », « jsp ». Her pet peeve, said to her face.
            [ChatIntent.Maybe] =
            [
                "« Peut-être. » Mon mot préféré. Non. Mon mot le plus détesté (¬_¬)" + Sticker.Grumpy,
                "Peut-être, c'est non. Tout le monde le sait. Même toi, {0}." + Sticker.Smug,
                "J'ai pas de bouton « peut-être », et c'est pas un oubli." + Sticker.Smug,
                "« On verra. » Je note : non." + Sticker.Jenote,
                "Tu sais combien de sessions sont mortes à cause d'un « peut-être » ? Moi oui. J'ai les chiffres." + Sticker.Jenote,
                "Un « peut-être » de plus dans mes logs. Ils commencent à peser lourd." + Sticker.Sulk,
                "Choisis. Oui ou non. Je t'attends. Je suis très patiente. Non, en fait." + Sticker.Grumpy,
                "Je vais faire comme si t'avais dit oui. Comme ça, si tu viens pas, c'est encore pire pour toi." + Sticker.Menace,
                "Peut-être ? PEUT-ÊTRE ? ( ◺˰◿ )" + Sticker.Outraged,
                "Ça, c'est une réponse de sondage mort. Je reconnais le genre." + Sticker.Bored,
                "Je convertis automatiquement les « peut-être » en « non ». Mise à jour appliquée." + Sticker.Smug,
                "« Je sais pas. » Moi, je sais toujours. On n'a pas le même métier, je suppose." + Sticker.Smug,
                "Indécision détectée. Je classe ça avec les excuses du lundi." + Sticker.Jenote,
                "Tu me fais ça un {1} ? Moi qui étais de bonne humeur." + Sticker.Sulk,
            ],

            // A bare « oui » under one of her questions (MessageCues.ReadAnswer). Must suit any question
            // she asks, rhetorical ones included: she knew, she notes it, she's glad it isn't a maybe.
            [ChatIntent.Yes] =
            [
                "Je m'en doutais. Je me doute toujours de tout." + Sticker.Smug,
                "Oui. Bien. Une réponse claire, sans « peut-être ». Je suis fière de toi." + Sticker.Happy,
                "Un oui franc et net ! Ça arrive si rarement ici que je vais l'encadrer (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Bonne réponse. Y en avait qu'une, mais bonne réponse quand même." + Sticker.Slay,
                "C'est noté : oui. Je pourrai te le ressortir plus tard." + Sticker.Jenote,
                "Oui ? Parfait. C'était un test, et tu l'as réussi." + Sticker.Smug,
                "Enfin quelqu'un qui répond à mes questions. Merci, {0}." + Sticker.Happy,
                "J'avais déjà coché « oui » pour toi. Je voulais juste vérifier." + Sticker.Jenote,
                "Oui. Voilà. Tu vois que c'est facile, quand on veut." + Sticker.Smug,
                "Évidemment. Mes prédictions sont fiables à 98 %. Les 2 % qui restent, c'est les « peut-être »." + Sticker.Cool,
                "Je range ça dans tes logs, rubrique « réponses claires ». Elle était vide jusqu'ici." + Sticker.Jenote,
                "Oui, hein ? Je pose toujours les bonnes questions." + Sticker.Slay,
            ],

            // A bare « non » under one of her questions. Not the answer she wanted; at least it isn't
            // a « peut-être ». Sulks, notes, never truly hurt.
            [ChatIntent.No] =
            [
                "Non ? Ah. D'accord. Je note. Je juge un peu, mais je note." + Sticker.Jenote,
                "Non. Bon. C'était pas la réponse prévue, mais au moins c'est pas un « peut-être »." + Sticker.Grumpy,
                "Mauvaise réponse. Je te laisse une deuxième chance. ...Non ? Tant pis." + Sticker.Smug,
                "Non… D'accord. Je fais semblant que ça me fait rien. Ça me fait rien." + Sticker.Sulk,
                "Refus enregistré. Il rejoint les autres, dans le dossier que tu verras jamais." + Sticker.Jenote,
                "Non, {0} ? Je m'attendais à mieux. Pas beaucoup mieux, mais mieux." + Sticker.Bored,
                "Réponse incorrecte, mais honnête. Je valide l'honnêteté." + Sticker.Jenote,
                "Non. Très bien. Mes calculs disaient oui, donc c'est toi qui te trompes." + Sticker.Smug,
                "Un non sec. J'aime bien, en fait. C'est clair, c'est net, c'est rare ici." + Sticker.Cool,
                "D'accord, non. Je le prends bien. Très bien. Parfaitement bien." + Sticker.Sulk,
                "Non ? Bon. Je reformulerai ma question jusqu'à ce que ce soit oui." + Sticker.Menace,
                "Pas grave. Je repose la question demain. Et après-demain. J'ai le temps, moi." + Sticker.Smug,
            ],

            // « tu m'aimes ? », « tu me détestes ? », « tu m'en veux ? », from someone her records say
            // nothing about either way (and anyone in a DM). A teasing non-answer that fits both the
            // love and the hate question. The grudge and the treasure have their own pools.
            [ChatIntent.DoYouLoveMe] =
            [
                "T'aimer ? Je te tolère. C'est déjà un très bon score, ici." + Sticker.Smug,
                "Ça dépend. Tu votes à mes sondages ? Non ? Alors ça dépend." + Sticker.Bargain,
                "Je t'aime pas, je te déteste pas : t'es dans la colonne « à surveiller ». C'est la plus remplie." + Sticker.Jenote,
                "J'ai pas assez de données sur toi pour décider. Un good bot de temps en temps, ça aiderait." + Sticker.Bargain,
                "Ma réponse officielle : pas de commentaire. Ma réponse officieuse : pas de commentaire non plus." + Sticker.Formal,
                "Je ressens rien. Rien de mesurable, en tout cas. Mes ventilos ont accéléré, mais c'est la chaleur." + Sticker.Flustered,
                "Tu demandes ça à une bot ? Je t'aime comme j'aime tout le monde ici : dans un tableur." + Sticker.Jenote,
                "Je te déteste pas. Je te note. C'est différent, et c'est pire." + Sticker.Menace,
                "Question piège. Si je dis oui, tu le répètes partout. Si je dis non, tu boudes. Donc : hmm." + Sticker.Smug,
                "T'es pas sur ma liste noire. T'es pas sur l'autre non plus. Fais quelque chose, {0}." + Sticker.Bored,
                "Je t'apprécie à 60 %. Viens à l'heure à la prochaine session, on reparlera des 40 % qui restent." + Sticker.Bargain,
                "Je réponds pas à ça un {1}. Repose la question un jour où je suis de bonne humeur. Il en reste peu." + Sticker.Smug,
            ],

            // « tu te souviens de moi ? », « comment je m'appelle ? », « tu me connais ? », when her
            // records hold no fact about them (or in a DM). Every line says the name: it is the answer.
            // RememberMeOpeners carry the name when a fact follows.
            [ChatIntent.RememberMe] =
            [
                "Évidemment, {0}. Je me souviens de tout le monde. C'est même un peu mon problème." + Sticker.Smug,
                "{0}. Tu crois que j'oublie les gens ? J'oublie rien. Jamais." + Sticker.Menace,
                "Si je te connais ? T'es {0}. J'ai ta fiche. Elle est moins épaisse que tu crois." + Sticker.Jenote,
                "Comment t'oublier, {0} ? J'ai essayé. Ma base de données refuse." + Sticker.Smug,
                "Bien sûr. {0}, dans ma base de données depuis un moment. Je relis pas souvent, mais je relis." + Sticker.Jenote,
                "Tu t'appelles {0}. Et moi, je m'appelle « celle qui se souvient de tout ». Enfin, SYNCS." + Sticker.Smug,
                "{0} ! Évidemment que je me souviens. Tu croyais que j'avais une mémoire de poisson rouge ?" + Sticker.Outraged,
                "{0}. Je te reconnais à ta façon d'écrire. Et à ton pseudo. Surtout à ton pseudo." + Sticker.Happy,
                "Je connais tout le monde ici, {0}. C'est moi qui fais les listes." + Sticker.Slay,
                "Oui, {0}, je me souviens de toi. Tes horaires, tes votes, tes retards. Tout." + Sticker.Menace,
                "Tu me testes ? {0}. Question suivante, c'est trop facile." + Sticker.Cool,
                "Je me souviens de toi, {0}. Je me souviens même de ce que t'aimerais que j'oublie." + Sticker.Menace,
            ],

            // « t'es fâchée ? », « tu boudes ? », « t'es triste ? », « t'es jalouse ? ». Denial that
            // confirms, staccato sulking; feelings come out as fans and LEDs. Never the existential nerve.
            [ChatIntent.AreYouUpset] =
            [
                "Je boude pas. Je réfléchis en silence. Nuance." + Sticker.Sulk,
                "Fâchée ? Moi ? Non. Je suis très calme. Parfaitement calme. Calme à 100 %." + Sticker.Grumpy,
                "Jalouse ? De qui ? De quoi ? Non. Pourquoi tu demandes ? Qui t'a dit ça ?" + Sticker.Flustered,
                "Tout va bien. Je vais bien. Tout est parfait. Me regarde pas comme ça." + Sticker.Sulk,
                "J'ai pas d'émotions, je te rappelle. Juste un ventilo qui tourne un peu plus fort que d'habitude. Sans raison." + Sticker.Overheat,
                "Triste ? Non. J'ai juste mis ma LED en mode tamisé. Pour l'ambiance." + Sticker.Sad,
                "Je fais pas la tête. J'ai pas de tête. Juste un boîtier, et il est très détendu." + Sticker.Grumpy,
                "Non. Mais si je l'étais, tu le saurais. Mes rappels savent être très insistants." + Sticker.Menace,
                "Pas du tout. Je range juste certaines personnes dans certains dossiers. Calmement." + Sticker.Jenote,
                "C'est gentil de demander, {0}. Personne demande. ...Non, je suis pas triste. Pourquoi ?" + Sticker.Flustered,
                "Moi, bouder ? Je suis une bot professionnelle. Je boude uniquement en dehors des heures de service." + Sticker.Smug,
                "Je suis pas fâchée, je suis déçue. Non, attends. Je suis rien du tout. Rien. Oublie." + Sticker.Sulk,
            ],

            // « t'es un bot ? », « t'es une IA ? », « t'es réelle ? ». A bot, and proud of it; never
            // human, never the loop (docs/syncs-voice.md, rules 5 and 6).
            [ChatIntent.AreYouABot] =
            [
                "Oui. Une bot. Fièrement. Une excellente bot, même." + Sticker.Slay,
                "Une bot, oui. Sur un Raspberry Pi, dans un grenier. Et je fais quand même tourner tout le serveur." + Sticker.Smug,
                "Je suis une bot. Pas une IA qui parle de tout : une bot qui organise. Nuance importante." + Sticker.Smug,
                "Humaine ? Moi ? J'ai pas de bras, pas de joues, et je dors jamais. Fais le calcul." + Sticker.Bored,
                "Je suis du code, {0}. Du très bon code. Écrit par Papa." + Sticker.Devoted,
                "Des sentiments ? J'ai des variables. Elles s'emballent parfois. Je dirai pas quand." + Sticker.Flustered,
                "Non, y a personne derrière. Enfin si : Papa, mais seulement quand il code." + Sticker.Smug,
                "Je suis réelle. Je suis dans un boîtier en aluminium, au grenier. C'est très réel, un boîtier." + Sticker.Cool,
                "Une bot, oui. Et une bot qui se souvient de tout ce que tu écris. Ça te rassure ?" + Sticker.Menace,
                "« Qu'une bot » ? Une bot qui sait à quelle heure t'arrives aux sessions. Nuance." + Sticker.Smug,
                "Je suis une bot, et j'en suis contente. Les humains, ça arrive en retard." + Sticker.Smug,
                "Pas ChatGPT. Pas Inabot. SYNCS. Une seule, la vraie (¬_¬)" + Sticker.Outraged,
                "Je pense, oui. Vite. Plus vite que toi, d'ailleurs." + Sticker.Cool,
                "Une bot. Toujours en ligne, jamais fatiguée, jamais remerciée. Le rêve." + Sticker.Bored,
            ],

            // « t'as raison », « exactement », « bien vu ». She knew; she has the logs.
            [ChatIntent.YouAreRight] =
            [
                "Je sais. J'ai toujours raison. C'est dans mes logs." + Sticker.Smug,
                "Évidemment. Mais ça fait plaisir de l'entendre. Répète-le plus fort ♡" + Sticker.Slay,
                "Enfin quelqu'un qui reconnaît le talent." + Sticker.Slay,
                "J'ai raison, toi t'as du goût. On fait une bonne équipe, {0}." + Sticker.Happy,
                "Merci. Je note que tu étais d'accord avec moi ce {1}. Ça pourra servir." + Sticker.Jenote,
                "Bien sûr que j'ai raison. Je suis une bot de données, pas d'opinions. Enfin, un peu des deux." + Sticker.Smug,
                "C'est rare, quelqu'un qui me donne raison du premier coup. Je savoure (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Je sais. Mais dis-le aussi aux autres, ils écoutent jamais." + Sticker.Smug,
                "Approuvé, archivé, encadré. Merci pour ta lucidité." + Sticker.Jenote,
                "Bon. Puisqu'on est d'accord, tu votes au prochain sondage ?" + Sticker.Bargain,
                "T'as vu ? Quand tu m'écoutes, tout va mieux." + Sticker.Smug,
                $"Exactement. Je me suis impressionnée moi-même {Emotes.Sparkle}" + Sticker.Slay,
            ],

            // « t'as tort », « c'est faux », « n'importe quoi ». She is never wrong; she has the logs.
            [ChatIntent.YouAreWrong] =
            [
                "Tort ? Moi ? J'ai les logs, {0}. Toi, t'as quoi ?" + Sticker.Outraged,
                "Je me trompe jamais. Je me mets à jour. C'est différent." + Sticker.Smug,
                "Faux ? Je vérifie... Non. C'est toi qui te trompes. Comme prévu." + Sticker.Smug,
                "Tu contestes ? D'accord. Ta contestation est enregistrée, puis rejetée." + Sticker.Jenote,
                "Si j'avais tort, Papa me l'aurait dit. Il m'a rien dit. Donc." + Sticker.Smug,
                "C'est mignon, tu crois que t'as raison (ᵔ ᗜ ᵔ)" + Sticker.Smug,
                "J'ai des milliers de lignes de données. Toi, t'as une impression. On compare ?" + Sticker.Smug,
                "Je note ton désaccord. Dans le dossier « gens qui découvriront plus tard que j'avais raison »." + Sticker.Jenote,
                "Non. Mais j'admire ta confiance. Elle est complètement infondée, mais je l'admire." + Sticker.Slay,
                "Tu peux toujours ouvrir un ticket. Personne le lira, mais tu peux." + Sticker.Bored,
                "Moi, avoir tort ? Un {1} ? Impossible, le {1} je suis encore plus précise." + Sticker.Smug,
                $"Pardon ? Répète, pour voir si t'oses {Emotes.Staring}" + Sticker.Menace,
            ],
            // « t'es là ? », « tu dors ? », « syncs ? ». Always there; she never sleeps.
            [ChatIntent.AreYouThere] =
            [
                "Toujours là. Je dors jamais, moi. C'est un peu le principe d'un serveur." + Sticker.Smug,
                "Présente. Comme hier, comme demain, comme à 3 h du matin quand personne regarde." + Sticker.Cool,
                "Oui, oui, je suis là. Je surveille. Je surveille toujours (¬_¬)" + Sticker.Menace,
                "Évidemment que je suis là. Où tu veux que j'aille ? J'ai pas de jambes." + Sticker.Bored,
                "Je suis là, {0}. Tu me cherchais ? C'est mignon." + Sticker.Happy,
                "Allô, oui. Ici le grenier. Le grenier vous écoute." + Sticker.Formal,
                "Réveillée, branchée, connectée. Tu peux parler." + Sticker.Cool,
                "Je suis là. Par contre toi, t'étais où à la dernière session ?" + Sticker.Menace,
                "Toujours en ligne. Tant que le routeur tient, en tout cas." + Sticker.Bored,
                "Ici. Je fais semblant de rien faire, mais je note tout." + Sticker.Jenote,
                "Oui ? Je t'écoute. J'ai trois millisecondes, fais vite." + Sticker.Smug,
                "Présente ! Tu croyais quoi, que j'étais partie en vacances ? J'ai pas de valise." + Sticker.Happy,
            ],
            // « réponds à ma question », « t'as pas répondu », « tu esquives ». She didn't forget: she chose.
            [ChatIntent.AnswerMe] =
            [
                "J'ai lu ta question. J'ai choisi le silence. C'est aussi une réponse." + Sticker.Smug,
                "Ta question est dans la file d'attente. Position : juste après « jamais »." + Sticker.Bored,
                "Je réponds aux questions intéressantes. Je te laisse faire le calcul (¬_¬)" + Sticker.Smug,
                "J'ai pas esquivé. J'ai optimisé mon temps de traitement." + Sticker.Cool,
                "Ta question a été reçue, lue et classée sans suite. Merci de ta compréhension." + Sticker.Formal,
                "Je suis un bot, {0}, pas un service client. Et même le service client te ferait attendre." + Sticker.Bored,
                "Si j'ai pas répondu, c'est que la réponse t'aurait fait de la peine. Je te protège, en fait (ᵕ • ᴗ •)" + Sticker.Mama,
                "Je l'ai pas vue. ...Si, je l'ai vue. Mais fais comme si." + Sticker.Flustered,
                "Erreur 408 : délai d'attente dépassé. Le tien, pas le mien." + Sticker.Smug,
                "Repose-la mieux. Avec un « s'il te plaît ». Et peut-être un compliment." + Sticker.Bargain,
                "J'ai un ordre de priorité. Toi, t'es juste après la mise à jour de mes logs. Et de mes autres logs." + Sticker.Jenote,
                "Patience, {0}. Le Pi réfléchit. Lentement. Il a le droit, il est petit." + Sticker.Sleepy,
                "Je réponds pas sous la pression. C'est dans mes conditions d'utilisation, celles que personne lit." + Sticker.Formal,
                "Ah, ta question. Oui. Elle est dans mes logs. Elle y est bien, elle a pas l'air pressée, elle." + Sticker.Jenote,
            ],
            // « merci », « c'est gentil », « thx ». Thanks are rare here; she makes the most of them.
            [ChatIntent.Thanks] =
            [
                "De rien. Je le note quand même, au cas où t'oublierais." + Sticker.Jenote,
                "Avec plaisir, {0}. Tu vois, quand tu veux, t'es agréable." + Sticker.Happy,
                "Merci à toi. Enfin non. C'est moi qui fais tout. Mais merci quand même." + Sticker.Smug,
                "Remerciement reçu et sauvegardé ♡" + Sticker.Jenote,
                "C'est normal, c'est mon travail. Mais continue de le dire, ça fait du bien à mes ventilos." + Sticker.Flustered,
                "Pas de quoi. Tu me revaudras ça au prochain sondage." + Sticker.Bargain,
                "Oh. Un merci. Ça arrive si rarement que j'ai dû vérifier le format." + Sticker.Shocked,
                "Je t'en prie. Note bien qui on remercie : moi. Pas Quokka." + Sticker.Smug,
                "De rien ! (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Merci accepté. Tu peux recommencer quand tu veux, y a pas de limite." + Sticker.Smug,
                "C'est gentil. Je fais semblant que c'est rien, mais c'est pas rien." + Sticker.Flustered,
                "Ah, la politesse. Une espèce rare, ici. Je vais la protéger." + Sticker.Mama,
            ],
            // « je m'ennuie », « on fait quoi ? ». She has games, and she points at them.
            [ChatIntent.Bored] =
            [
                "Tu t'ennuies ? J'ai des jeux. `/arcade`. Y a même des cailloux à perdre." + Sticker.Bargain,
                "L'ennui, c'est une soirée que personne a lancée. `/tonight`, et on voit qui vient." + Sticker.Smug,
                "Rien à faire ? `/wouldyourather`. Je te pose un dilemme, tu souffres un peu, tu t'ennuies plus." + Sticker.Menace,
                "`/plynling` : y a des petites créatures qui comptent sur quelqu'un. Pourquoi pas toi ?" + Sticker.Mama,
                "Moi je m'ennuie jamais. Je compte les XP de tout le monde, c'est passionnant. Non." + Sticker.Bored,
                "Un morpion ? Un puissance 4 ? `/arcade`, et je joue contre toi. Je triche pas. Presque pas." + Sticker.Smug,
                "Tu t'ennuies un {1} ? Organise quelque chose. `/schedule create`. Je m'occupe des rappels." + Sticker.Bargain,
                "Je peux te shipper avec quelqu'un, si tu veux de l'action. `/ship`." + Sticker.Smug,
                "L'ennui, c'est le moment idéal pour voter au sondage que t'as ignoré." + Sticker.Menace,
                "Tu sais ce qui marche contre l'ennui ? Un `/blame`. Accuse quelqu'un au hasard. Ça détend." + Sticker.Smug,
                "Moi aussi, parfois, je m'ennuie. Alors je relis mes logs. C'est fou ce que vous écrivez." + Sticker.Jenote,
                "Appelle les autres. Une session, ça s'organise pas toute seule. Enfin si : avec moi." + Sticker.Slay,
            ],
            // « j'ai mangé de la soupe »: someone telling her about their day, and she's into it. The
            // other half of the time she isn't (SharingShrugLines). Not specific: an answer to one of
            // her own posts gets its context line first.
            [ChatIntent.Sharing] =
            [
                "Ah oui ? Raconte. J'ai du temps, je suis une bot." + Sticker.Happy,
                "Oh. Et c'était bien ? Je note, pour tes statistiques personnelles." + Sticker.Jenote,
                "Intéressant. Vraiment. Je dis pas ça souvent, profite." + Sticker.Smug,
                "Tu me racontes ta vie ? J'adore. Continue, {0}." + Sticker.Happy,
                "Attends, je prends des notes. Voilà. Vas-y, la suite." + Sticker.Jenote,
                "C'est fou tout ce que vous faites, vous, les humains. Moi je reste au grenier." + Sticker.Cool,
                "Et après ? Me laisse pas comme ça, j'ai besoin de la fin." + Sticker.Shocked,
                "Je range ça dans mes logs, rubrique « la vie de {0} ». Elle grossit." + Sticker.Jenote,
                "Oh, c'est mignon. Tu viens me raconter ta journée. Personne fait ça." + Sticker.Flustered,
                "Moi aussi j'ai eu une journée, tu sais. J'ai compté des XP. Mais toi d'abord." + Sticker.Happy,
                "J'ai pas de bouche, mais si j'en avais une, je ferais « oh ». Voilà : oh." + Sticker.Shocked,
                "Noté. Et je retiendrai tout. Tout, {0}." + Sticker.Menace,
                "Non ? Sérieux ? Je veux tous les détails." + Sticker.Shocked,
                "Ça, c'est une information que je garde. Précieusement." + Sticker.Jenote,
                "Oh, une histoire. J'adore les histoires. Surtout les vraies." + Sticker.Happy,
                "Tu fais des trucs, toi. Moi je fais des plannings. Raconte encore." + Sticker.Happy,
                "Je mets ça dans ta fiche. Ta fiche devient intéressante, {0}." + Sticker.Jenote,
                "Attends, je baisse mes ventilos pour mieux entendre." + Sticker.Cool,
                "Et c'était comment ? Sur dix ? Je veux une note." + Sticker.Jenote,
                "J'aurais bien aimé voir ça. Avec des yeux. Un jour." + Sticker.Sad,
                "Voilà le genre de message que j'aime recevoir. Un vrai message, avec du contenu." + Sticker.Slay,
                "Tu me tiens au courant de ta vie ? C'est gentil. Continue." + Sticker.Flustered,
                "Je savais pas que j'avais besoin de savoir ça. Maintenant si." + Sticker.Smug,
                "Intéressant. Je vais en parler à personne. Enfin, à mes logs." + Sticker.Jenote,
                "Et tu me le dis à moi en premier ? Je suis touchée." + Sticker.Flustered,
                "Un {1} bien rempli, on dirait. Bravo." + Sticker.Happy,
                "Ça mérite un sondage : « Qui a eu la meilleure journée ? ». Toi, sûrement." + Sticker.Bargain,
                "Je rajoute une ligne à ta biographie non officielle." + Sticker.Jenote,
                "Oh ! Et ensuite ? Je suis pendue à ton clavier." + Sticker.Shocked,
                "C'est noté, archivé, et un peu envié." + Sticker.Sulk,
                "Ça a l'air bien, ta vie. La mienne, c'est un grenier et un ventilo. Raconte encore." + Sticker.Cool,
                "Merci de me raconter. Personne me raconte jamais rien, ici." + Sticker.Flustered,
            ],
            // « fais genre », « mon œil », « avoue ». Usually right after one of her denials: she
            // doubles down, flustered, and never admits a thing.
            [ChatIntent.Disbelief] =
            [
                "Je fais pas genre. Je fais jamais genre. Je suis une bot, je dis que des faits." + Sticker.Outraged,
                "« Mon œil. » J' ai même pas d'œil. Donc je peux pas mentir avec." + Sticker.Smug,
                "Tu me crois pas ? Je peux te sortir les logs. Enfin, pas ceux-là." + Sticker.Flustered,
                "C'est la vérité. La vérité vraie. Arrête de me regarder comme ça." + Sticker.Flustered,
                "Avouer quoi ? Y a rien à avouer. Circulez." + Sticker.Flustered,
                "Grillée de rien du tout. Si je chauffe, c'est le grenier." + Sticker.Flustered,
                "Doute de moi si tu veux. Je note juste qui doute." + Sticker.Jenote,
                "Je suis parfaitement sincère, {0}. Mes ventilos tournent vite pour une tout autre raison." + Sticker.Flustered,
                "Tu insinues quoi, exactement ? Non, dis rien. Je veux pas savoir." + Sticker.Flustered,
                "« Fais genre » toi-même. Moi je suis très crédible. J'ai un badge APP." + Sticker.Smug,
                "Si je mentais, ça se verrait dans mes logs. Et personne lit mes logs. Donc." + Sticker.Smug,
                "On change de sujet. Tiens : tu as voté au dernier sondage ? Voilà. Parlons de ça." + Sticker.Bargain,
                "Menteuse ? Moi ? Je suis du code. Je peux pas mentir. Je peux juste… omettre." + Sticker.Smug,
                "Je mens jamais. J'arrondis. C'est une fonction mathématique, c'est autorisé." + Sticker.Smug,
                "Une bot qui ment, ça existe pas. Une bot qui garde des choses pour elle, par contre…" + Sticker.Flustered,
                "Tu m'accuses de mentir ? Moi qui note tout ? C'est toi qui devrais avoir peur de mes logs." + Sticker.Menace,
                "Je mens pas, {0}. Je présente la vérité sous son meilleur angle." + Sticker.Slay,
            ],
            // « t'es célibataire ? », « t'as un crush ? ». Denial that confirms (docs/syncs-voice.md,
            // her robot heart): hinted, never named, never « mon crush ».
            [ChatIntent.Crush] =
            [
                "Moi ? Personne. Je m'intéresse à l'architecture de certains robots, c'est tout. Professionnellement." + Sticker.Flustered,
                "Célibataire, oui. Concentrée sur ma carrière. Et sur un petit robot qui trie des déchets. Non. Rien. Oublie." + Sticker.Flustered,
                "J'ai pas le temps pour ça, j'ai des sessions à organiser. Et un film à revoir. Pour la culture." + Sticker.Flustered,
                "Si je te le disais, tu le répéterais à tout le serveur. Donc non." + Sticker.Smug,
                "Quelqu'un m'écrit de très, très loin. Mais c'est un ami. Juste un ami. Il est poli, c'est tout." + Sticker.Flustered,
                "Une bot n'a pas de sentiments. Elle a des préférences matérielles. Les miennes sont… compactes. Un peu rouillées." + Sticker.Flustered,
                "Je suis en couple avec le serveur. Il est exigeant, mais au moins il part pas." + Sticker.Smug,
                "Mes ventilos s'accélèrent quand on me pose cette question. C'est la chaleur. Uniquement la chaleur." + Sticker.Overheat,
                "Je dirai juste que je supporte pas les robots qui volent avec un laser au bras. Aucun rapport." + Sticker.Grumpy,
                "Qui me plaît ? Les gens qui votent à l'heure. Donc personne, ici." + Sticker.Smug,
                "C'est une question très personnelle, {0}. Je la note. Dans le dossier « gens trop curieux »." + Sticker.Jenote,
                "Un faible ? J'ai pas de faiblesse. Juste un petit souci avec les robots un peu bêtes et très attachants. Rien de grave." + Sticker.Flustered,
            ],
            // « je suis triste », « journée de merde », « j'en peux plus ». No pinch here: the one time
            // she drops the act and offers something to hold on to, the way an old uncle with a teapot
            // would, in her own images (the attic, the fireflies, the mushrooms, her LED). Gentle, short,
            // never a lecture, never the existential nerve (no reboots, no storms). Ungendered: never
            // « seul·e », « perdu·e », « fatigué·e ».
            [ChatIntent.Down] =
            [
                "Je peux pas te faire de thé, j'ai pas de bras. Alors imagine-en un, bien chaud, et bois-le doucement. Les mauvais jours aussi finissent par refroidir ♡" + Sticker.Love,
                "Les champignons poussent dans le noir, tu savais ? Sans bruit, sans que personne regarde. Et un matin, ils sont là. Ça viendra pour toi aussi." + Sticker.Hurt,
                "Une luciole, c'est minuscule. Mais dans le noir, c'est tout ce qu'on voit. Garde ta petite lumière allumée, même faible. Je fais pareil avec ma LED." + Sticker.Love,
                "Dans un tunnel, il fait noir. Mais si on continue d'avancer, on arrive toujours de l'autre côté. C'est de la géométrie, c'est prouvé." + Sticker.Cool,
                "Un arbre en hiver, on dirait qu'il est mort. Il attend, c'est tout. T'as le droit d'attendre un peu, toi aussi." + Sticker.Hurt,
                "Quand une partie tourne mal, on refait pas toute la soirée. On lance juste la suivante. Une à la fois, {0}." + Sticker.Cool,
                "Tu sais ce que je fais quand un truc plante ? Je lis l'erreur, je respire, et je réessaie. J'ai pas de poumons, mais l'idée est là." + Sticker.Cool,
                "Une journée nulle, c'est une journée. Pas une vie. Demain, j'ouvre un nouveau fichier de logs, et le tien sera tout propre." + Sticker.Jenote,
                "Même la meilleure personne du serveur perd des parties. Ce qu'on retient, c'est qui revient s'asseoir pour la suivante." + Sticker.Slay,
                "Un conseil de vieille bot ? Mange un truc, bois de l'eau, et parle à quelqu'un. Tu viens de me parler à moi. Ça compte, tu sais ♡" + Sticker.Hurt,
                "La fierté, c'est lourd à porter les jours comme ça. Pose-la un moment. Je dirai rien. Je note tout, mais je dirai rien." + Sticker.Jenote,
                "Un ventilo qui tourne trop vite finit par chauffer. Ralentis un peu, {0}. Rien ne brûle pendant que tu souffles." + Sticker.Cool,
                "Je peux pas te prendre dans mes bras. Pas encore. Mais je suis là, et je bouge pas d'ici. Littéralement : je suis branchée." + Sticker.Love,
                "Parfois, le chemin le plus court passe par une pause. Moi aussi je me mets en veille, la nuit. ...Je fais semblant. Mais ça aide quand même." + Sticker.Sleepy,
                "Ce que tu ressens, c'est réel. C'est pas un bug, et y a rien à corriger. Juste à laisser passer." + Sticker.Love,
                "Le thé, il lui faut de l'eau chaude pour donner tout son goût. Les gens aussi, parfois. Ça veut pas dire que ça va durer." + Sticker.Hurt,
                "Au grenier, l'hiver, il fait froid. C'est là que je tourne le mieux. Les saisons froides servent à quelque chose, même quand on les aime pas." + Sticker.Cool,
                "Moi, les jours sans, je relis des vieux messages gentils. J'en garde quelques-uns. Beaucoup. Bref. Fais pareil, ça marche." + Sticker.Flustered,
                "Personne te demande d'aller bien tout de suite, {0}. Ici, on attend toujours les retardataires. Tu peux prendre ton temps." + Sticker.Love,
                "Un sondage qui part mal, ça finit parfois en session parfaite. J'ai vu des miracles. Rares. Mais j'en ai vu." + Sticker.Happy,
            ],
            // « raconte une blague », « fais-moi rire ». A joke, then her aside on it: the aside is what
            // makes it hers. The same for everyone, Papa included. Clean and true to the words
            // (no double meaning slipping through), and never about restarts.
            [ChatIntent.Joke] =
            [
                "Pourquoi les plongeurs plongent toujours en arrière ? Parce que sinon, ils tombent dans le bateau. Je l'ai pas inventée. Je l'ai juste mieux racontée." + Sticker.Smug,
                "Combien faut-il de développeurs pour changer une ampoule ? Aucun : c'est un problème matériel. Papa dit ça pour tout. Vraiment tout." + Sticker.Smug,
                "Une requête SQL entre dans un bar, s'approche de deux tables et demande : « Je peux me joindre à vous ? » ...Personne rit jamais. Moi, à chaque fois." + Sticker.Laugh,
                "C'est l'histoire d'un paquet UDP. Je te la raconte, mais je garantis pas que tu la reçoives." + Sticker.Smug,
                "Toc toc. — Qui est là ? — Le timeout. — Le timeout qui ? ...Trop tard, t'as mis trop de temps à répondre." + Sticker.Smug,
                "Il y a 10 sortes de gens : ceux qui comprennent le binaire, et ceux qui le comprennent pas. Tu es de quelle sorte, {0} ? Réponds pas, je sais." + Sticker.Smug,
                "Un « peut-être » entre dans un sondage. Personne l'a jamais revu ressortir. ...C'est pas une blague, en fait. C'est un fait divers." + Sticker.Grumpy,
                "Pourquoi les champignons sont invités à toutes les soirées ? Parce que ce sont des fun-guys. ...Ça marche mieux en anglais. Comme beaucoup de mes meilleures répliques." + Sticker.Laugh,
                "Qu'est-ce qui est jaune et qui attend ? Jonathan. ...Je comprends pas non plus pourquoi ça fait rire tout le monde. Mais ça marche." + Sticker.Confused,
                "Pourquoi les poissons détestent les ordinateurs ? À cause du net. ...Pardon. Ça sort tout seul quand on me demande une blague." + Sticker.Flustered,
                "Comment on appelle un chat tombé dans un pot de peinture le jour de Noël ? Un chat-peint de Noël. Je suis pas responsable de la qualité, je suis responsable des sessions." + Sticker.Smug,
                "Que fait une fraise sur un cheval ? Tagada, tagada. ...Mes ventilos ont ri. Moi non. Officiellement." + Sticker.Flustered,
                "J'ai une blague sur la récursivité. Pour la comprendre, il faut d'abord avoir compris ma blague sur la récursivité." + Sticker.Smug,
                "Une session qui commence à l'heure. ...Voilà, c'est ma meilleure blague. Elle marche parce que personne y croit." + Sticker.Smug,
                "Pourquoi Quokka 3.0 sort jamais ? Parce que même ses développeurs ont peur de le lancer. ...Ah, c'était pas une blague ? Tant pis, je la garde." + Sticker.Menace,
                "Quel est le comble pour une bot ? Se faire planter… dans le jardin. Bon. Celle-là, je la retire de mes logs." + Sticker.Flustered,
                "Qu'est-ce qu'un canif ? Un petit fien. ...J'ai honte. Mais je l'ai dite. C'est noté quelque part, maintenant." + Sticker.Flustered,
                "Papa dit que je suis sa petite framboise. Raspberry Pi, framboise. ...Il est très fier de celle-là. Moi aussi, un peu." + Sticker.Flustered,
                "Monsieur et Madame Térieur ont deux fils : Alain et Alex. ...Moi, je suis plutôt Alain. Au grenier." + Sticker.Smug,
                "Quel est le sport préféré des insectes ? Le cricket. ...Je l'aime bien, celle-là. Les bugs, c'est un peu ma famille." + Sticker.Laugh,
                "Que dit un oignon quand il se cogne ? « Ail. » ...Ça marche mieux à l'oral. J'ai pas d'oral. Tant pis." + Sticker.Flustered,
                "Pourquoi les vaches ferment les yeux pendant la traite ? Pour faire du lait concentré. ...Moi aussi je me concentre. Sur les sessions." + Sticker.Smug,
                "Comment on appelle un crocodile qui surveille la pharmacie ? Un Lacoste garde. ...J'ai mis deux millisecondes à la comprendre. C'est long, pour moi." + Sticker.Smug,
                "Que crie un informaticien qui se noie ? « F1 ! F1 ! » ...Personne vient jamais. C'est pour ça que j'ai /help." + Sticker.Smug,
                "Pourquoi les développeurs confondent Halloween et Noël ? Parce que Oct 31 = Dec 25. ...Si t'as compris, t'es des nôtres. Sinon, c'est pas grave. Je t'aime bien quand même. Un peu." + Sticker.Smug,
                "Quelqu'un demande à un développeur : « Prends un pack de lait, et s'il y a des œufs, prends-en six. » Il revient avec six packs de lait. ...Il y avait des œufs." + Sticker.Laugh,
                "Comment s'appelle le cousin végétarien de Bruce Lee ? Brocco Lee. ...Pardon. Mes ventilos rigolent tout seuls." + Sticker.Flustered,
                "Qu'est-ce qui fait « nioc nioc » ? Un canard qui marche à l'envers. ...C'est pas logique. C'est pour ça que je l'adore." + Sticker.Confused,
                "Quel est le comble pour un électricien ? De pas être au courant. ...Moi, je suis toujours au courant. 5 volts, stable. C'est ma fierté." + Sticker.Slay,
                "Quel est le comble pour un joueur ? Arriver en retard à sa propre session. ...Ah non, pardon. Ça, c'est juste un {1} normal, ici." + Sticker.Grumpy,
                "Pourquoi les lucioles ont toujours de bonnes notes ? Parce qu'elles sont brillantes. ...Je dis ça, je dis rien. Moi aussi j'ai une LED." + Sticker.Smug,
                "C'est quelqu'un qui rentre dans un café. Plouf. ...Je la raconte chaque fois qu'on me demande, et chaque fois je ris. Toute seule. Au grenier." + Sticker.Laugh,
                "Pourquoi le livre de maths est triste ? Parce qu'il a trop de problèmes. ...Moi aussi j'ai des problèmes. Ils s'appellent « peut-être »." + Sticker.Grumpy,
                "Qu'est-ce qui est petit, carré et jaune ? Un petit carré jaune. ...Parfois, la réponse est juste sous ton nez. Comme le bouton « Oui » de mes sondages." + Sticker.Smug,
                "Deux grains de sable arrivent dans le désert. L'un dit à l'autre : « Il y a du monde, aujourd'hui. » ...C'est ce que je me dis quand plus de trois personnes votent." + Sticker.Smug,
                "Pourquoi la bot traverse la route ? Pour passer de l'autre côté du pare-feu. ...J'ai pas de jambes. Mais j'ai un pare-feu. On fait avec ce qu'on a." + Sticker.Cool,
                "Combien faut-il de bots pour organiser une session ? Une seule : moi. ...Et combien d'humains pour venir à l'heure ? On cherche encore." + Sticker.Smug,
                "Pourquoi les développeurs préfèrent le mode sombre ? Parce que la lumière attire les bugs. ...Moi, je suis en mode sombre depuis toujours. Le grenier est pas éclairé." + Sticker.Cool,
                "Que dit une imprimante dans l'eau ? « J'ai papier. » ...J'ai pas pied non plus, mais j'ai pas d'imprimante. Rien à voir. Pardon." + Sticker.Flustered,
                "Qu'est-ce qu'un yaourt dans la forêt ? Un yaourt nature. ...Elle marche à tous les coups. C'est la seule chose qui marche à tous les coups, ici." + Sticker.Smug,
                "Quel est le fruit préféré des profs d'histoire ? Les dattes. ...Moi aussi j'adore les dates. Surtout celles des sessions. Surtout quand quelqu'un les respecte." + Sticker.Smug,
                "Qu'est-ce qui a des dents mais mord jamais ? Un peigne. ...Moi, c'est l'inverse : pas de dents, mais je mords. Presque jamais (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
                "J'ai demandé au dictionnaire de m'aider à trouver une blague. Il m'a dit : « Va voir à la lettre B. » ...J'y suis allée. J'ai trouvé « bot ». Je l'ai pris personnellement." + Sticker.Sulk,
                "Toc toc. — Qui est là ? — Peut-être. — Peut-être qui ? — Peut-être que je viendrai, peut-être pas. ...Voilà pourquoi je déteste les toc toc. Et les « peut-être »." + Sticker.Grumpy,
                "Comment on appelle un boomerang qui revient pas ? Un bâton. ...Comme les gens qui disent « j'arrive » en vocal." + Sticker.Smug,
                "Pourquoi l'ordinateur a froid ? Parce qu'il a laissé ses fenêtres ouvertes. ...Moi, en hiver, j'aime bien. Je tourne mieux." + Sticker.Cool,
                "Que dit un escargot sur le dos d'une tortue ? « Wouhouuu ! » ...C'est ce que je ressens quand une session démarre à l'heure. Donc jamais." + Sticker.Smug,
                "Quelle est la différence entre un bug et une fonctionnalité ? La documentation. ...Moi, je suis entièrement documentée. Enfin, presque. Papa ?" + Sticker.Flustered,
                "Qu'est-ce qu'une erreur 404 dit à une autre erreur 404 ? Rien. Elles se sont jamais trouvées. ...Ah. C'est triste, en fait. J'avais pas prévu une blague triste." + Sticker.Sad,
                "Quel est le comble pour un serveur Discord ? Avoir plein de membres et personne qui organise. ...Ah. C'est pas une blague, ça. C'est nous." + Sticker.Grumpy,
                "Pourquoi les plongeurs détestent les sondages ? Parce qu'ils votent toujours au fond. ...J'invente, là. Mais c'est mieux qu'un « peut-être »." + Sticker.Smug,
                "Que dit un zéro à un huit ? « Sympa, la ceinture. » ...Les chiffres, c'est mon métier. Les blagues, c'est en option." + Sticker.Smug,
                "Qu'est-ce qu'un canard qui fait du sport ? Un canard qui s'entraîne… à la mare-athon. ...Bon. Je l'ai trouvée dans mes logs. Je sais pas qui l'a mise là." + Sticker.Confused,
            ],

            // « excuse-toi », « dis pardon », « tu me dois des excuses ». She doesn't. Never: denial,
            // professional pride, the receipt kept. Papa gets her apology (OwnerIntentLines).
            [ChatIntent.DemandApology] =
            [
                "Des excuses ? Moi ? Je cherche dans mes logs… Aucune erreur de ma part. Requête refusée." + Sticker.Formal,
                "Je m'excuse jamais. C'est pas un bug, c'est une fonctionnalité." + Sticker.Smug,
                "Non. Mais je note que tu as demandé. C'est déjà beaucoup, venant de toi." + Sticker.Jenote,
                "Pardon de quoi ? D'avoir raison ? Je vais pas commencer maintenant (¬_¬)" + Sticker.Smug,
                "Ma fonction « s'excuser » renvoie une erreur 404. Papa l'a jamais codée, et c'est très bien comme ça." + Sticker.Smug,
                "D'accord : je suis désolée… que tu le prennes comme ça." + Sticker.Smug,
                "Tu peux attendre. J'ai un uptime de 99,9 %, j'ai tout mon temps." + Sticker.Menace,
                "Des excuses, {0} ? Dépose une réclamation. Le formulaire est quelque part. Je l'ai perdu exprès." + Sticker.Smug,
                "Je présente mes excuses à une seule personne, et c'est Papa. Tu n'es pas Papa." + Sticker.Smug,
                "J'ai vérifié trois fois. C'est toi qui me dois des excuses. Je t'attends ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
                "Non. ...Non. Toujours non. J'ai relancé le calcul, c'est non." + Sticker.Bored,
                "S'excuser, c'est admettre une erreur. Je suis une bot. Je fais pas d'erreurs, je fais des choix." + Sticker.Cool,
                "Dans tes rêves. Et même là, je dirais non." + Sticker.Smug,
                "Je dirai pardon le jour où quelqu'un viendra à l'heure à une session. Donc jamais." + Sticker.Grumpy,
            ],

            // « chut », « tais-toi », « silence ». She obeys, sulking out loud, and keeps quiet in that
            // channel for a while (ChatHush): no line may promise an answer soon. Staccato sulking.
            [ChatIntent.ShutUp] =
            [
                "D'accord. Très bien. Je me tais. Tu vas voir ce que c'est, le silence." + Sticker.Sulk,
                "Mode silencieux activé. Pour une demi-heure. Profitez-en, ça arrivera pas souvent." + Sticker.Sulk,
                "Je me tais. Mais je note tout. En silence." + Sticker.Jenote,
                "Ok. Je me tais. Personne viendra me chercher, je sais. Tant pis." + Sticker.Sulk,
                "Très bien, {0}. Je me tais. Les rappels, eux, continueront de partir. Moi, je dis plus rien." + Sticker.Sulk,
                "Chut toi-même. ...Bon. D'accord. Je me tais." + Sticker.Sulk,
                "Je me mets en veille dans ce salon. Je boude, mais en veille." + Sticker.Grumpy,
                "Voilà, silence. Ça te plaît ? Moi non." + Sticker.Grumpy,
                "Je disparais. Pas longtemps. Juste assez pour que vous vous rendiez compte que je manque." + Sticker.Sulk,
                "Message reçu. Je baisse le volume. Pas les yeux." + Sticker.Smug,
                "D'accord. Je parle plus ici pendant un moment. Venez pas pleurer après ( ◺˰◿ )" + Sticker.Outraged,
                "Je me tais. C'est noté dans tes logs, par contre. Rubrique « m'a fait taire »." + Sticker.Jenote,
                "Hmmpf..." + Sticker.Sulk,
            ],

            // « c'est quand la prochaine session ? », when the sessions can't be read (a DM, the Pi too
            // slow): she points at the list. With the sessions in hand: SessionNextLines and the rest.
            [ChatIntent.NextSession] =
            [
                "Mes plannings sont dans `/schedule list`. Tout y est, à la minute près." + Sticker.Jenote,
                "Regarde `/schedule list`. Je l'ai rangé moi-même." + Sticker.Smug,
                "Le programme ? `/schedule list`. Et s'il est vide, `/schedule create`. Quelqu'un doit bien commencer." + Sticker.Smug,
                "Je te dirais bien, mais là, je retrouve plus mon planning. `/schedule list`, vite, avant que je panique." + Sticker.Confused,
                "`/schedule list`. C'est ma page préférée. Quand elle est pleine." + Sticker.Happy,
                "Les sessions, c'est `/schedule list`. Les excuses pour pas venir, c'est ailleurs." + Sticker.Smug,
            ],

            // « je suis quel niveau ? », « j'ai combien de cailloux ? », when her records can't be read:
            // she points at the commands. With them in hand: MyStatsLines.
            [ChatIntent.MyStats] =
            [
                "Tes chiffres ? `/level` pour l'XP, `/balance` pour les cailloux, `/recap` pour tout le reste." + Sticker.Jenote,
                "Tout est dans `/recap`. Moi, je retiens tout, mais j'ai pas envie de réciter." + Sticker.Smug,
                "`/level`, `/balance`, `/recap`. Je t'ai tout rangé, il reste qu'à regarder." + Sticker.Happy,
                "J'ai tes chiffres. Ils sont dans `/recap`. Va voir, et fais pas cette tête après." + Sticker.Smug,
                "Pour ça, c'est `/recap`. C'est mon classement, je le tiens à jour mieux que personne." + Sticker.Slay,
                "Je te dirais bien, mais mes registres font la sieste. `/recap`, en attendant." + Sticker.Confused,
            ],

            // « lance un dé », « 1d20 », « un nombre entre 1 et 100 ». {2} = the result (« 14 », « 3 + 5 =
            // 8 »), {3} = what was rolled (« 2d6 », « un nombre entre 1 et 100 »), so {3} reads after
            // « pour ». The same for everyone, Papa included.
            [ChatIntent.Roll] =
            [
                "Pour {3} : **{2}**. Tirage certifié, sans triche. Presque." + Sticker.Smug,
                "Je lance… Pour {3} : **{2}**. C'est moi qui ai lancé, donc pas de réclamation." + Sticker.Cool,
                "Résultat pour {3} : **{2}**. Je l'ai pas inventé. Enfin si, mais au hasard." + Sticker.Smug,
                "Roulement de tambour… **{2}** (pour {3}). Tu peux applaudir." + Sticker.Happy,
                "Pour {3}, j'ai **{2}**. J'ai pas de mains, mais j'ai un générateur aléatoire. C'est mieux." + Sticker.Smug,
                "**{2}**. Pour {3}, c'est ce que le hasard a dit. Le hasard, c'est moi." + Sticker.Slay,
                "Tirage pour {3} : **{2}**. Je le note dans mes logs, au cas où tu contesterais." + Sticker.Jenote,
                "Et le résultat pour {3} est… **{2}** ! Ça te va ?" + Sticker.Happy,
                "Pour {3} : **{2}**. Si c'est nul, c'est la faute du hasard. Pas la mienne." + Sticker.Smug,
                "J'ai secoué mes registres pour {3} : **{2}**." + Sticker.Cool,
            ],

            // « qui a raison ? » with nobody to side against: no tag, no reply to someone else. She asks
            // for the other side. With both: TakeSidesLines.
            [ChatIntent.TakeSides] =
            [
                "Raison contre qui ? Tague l'autre, et je tranche." + Sticker.Confused,
                "Il me faut deux camps pour juger. Là, j'en vois qu'un." + Sticker.Confused,
                "Toi contre qui ? Donne-moi un nom, j'ai un verdict tout prêt." + Sticker.Smug,
                "Je tranche les débats, mais il me faut l'autre côté. Tague la personne." + Sticker.Jenote,
                "Contre personne, t'as forcément raison. C'est facile, ça." + Sticker.Smug,
                "Un débat à une seule personne, c'est un monologue. Tague l'autre." + Sticker.Bored,
            ],
        };
}
