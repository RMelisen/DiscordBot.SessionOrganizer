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
        new[] { SmallTalkIntentLines(), BackAndForthIntentLines() }
            .SelectMany(part => part).ToDictionary(kv => kv.Key, kv => kv.Value);

    // « je t'aime » to « t'es un bot ? ». The rest, « t'as raison » onwards, is in
    // Replies.Intents.BackAndForth.cs. An intent appears in one half only: the join throws on a twin.
    private static Dictionary<ChatIntent, string[]> SmallTalkIntentLines() =>
        new()
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
        };
}
