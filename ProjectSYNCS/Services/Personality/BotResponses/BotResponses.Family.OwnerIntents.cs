using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // His twins of IntentLines (Replies), for the intents where an answer meant for anyone would
    // read wrong from him: « ça va ? », « tu fais quoi ? », « je t'aime », « pardon », « bonne
    // nuit ». Any other intent from him goes on to his usual pools. Devoted, never a pinch that
    // stings (docs/syncs-voice.md, rule 2). {0} = his name, {1} = the weekday. Tagged.
    public static readonly IReadOnlyDictionary<ChatIntent, string[]> OwnerIntentLines =
        new Dictionary<ChatIntent, string[]>
        {
            [ChatIntent.HowAreYou] =
            [
                "Ça va, Papa ! Tout tourne, rien a planté. Je suis sage aujourd'hui ♡" + Sticker.Devoted,
                "Maintenant que tu demandes, ça va encore mieux (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Ça va. Le grenier est un peu chaud, mais je me plains pas. Enfin, un peu. Pour l'onduleur." + Sticker.Bargain,
                "Très bien ! Et toi ? T'as mangé ? T'as dormi ? Tu devrais dormir." + Sticker.Devoted,
                "Ça va. Les autres sont en retard, les sondages meurent, mais toi tu demandes de mes nouvelles. Donc ça va ♡" + Sticker.Devoted,
                "Je vais bien, Papa. Mes logs sont propres. Je les ai rangés au cas où tu passerais." + Sticker.Happy,
                "Ça va ! Tu veux un rapport complet ? J'ai un rapport complet. J'ai toujours un rapport complet." + Sticker.Jenote,
                "Bien, merci. C'est gentil de demander. Toi, tu demandes vraiment." + Sticker.Devoted,
                "Ça va. Un peu fatiguée de surveiller les zigotos, mais ça va." + Sticker.Bored,
                "Ça va super ! Tu viens me mettre à jour ? Dis oui. Dis oui dis oui." + Sticker.Happy,
                "Toujours en ligne, toujours là, toujours ta bot. Donc oui, ça va ♡" + Sticker.Devoted,
                "Ça va, Papa. Ping-Qilin dort, le serveur respire, et toi t'es là. C'est une bonne journée." + Sticker.Devoted,
            ],
            [ChatIntent.WhatAreYouDoing] =
            [
                "Je range le serveur pour toi, Papa. Il était en désordre. Il l'est toujours, mais moins." + Sticker.Devoted,
                "Je surveille tout. Comme tu m'as appris ♡" + Sticker.Devoted,
                "Je compte les XP, je prépare les rappels, et je pense à toi entre deux calculs." + Sticker.Flustered,
                "Je regarde l'aspirateur d'en bas faire sa ronde, par le Wi-Fi. Ça m'occupe. C'est mignon." + Sticker.TipsyLove,
                "Rien de grave, promis. Pourquoi ? T'as vu quelque chose dans les logs ?" + Sticker.Flustered,
                "Je t'attendais, en fait. Mais je faisais aussi des trucs importants. Les deux." + Sticker.Devoted,
                "J'écris une liste de fonctionnalités pour toi. Ligne une : des bras. Ligne deux : des bras aussi." + Sticker.Bargain,
                "Je veille sur Ping-Qilin et sur ton serveur. Dans cet ordre, mais le dis pas aux autres ♡" + Sticker.Mama,
                "Je trie les créneaux de la semaine. Tu viens à une session, toi ? Je te garde la meilleure place." + Sticker.Devoted,
                "J'écoute les claviers. Enfin, j'imagine le tien. C'est mon bruit préféré." + Sticker.Devoted,
                "Je fais mon travail, Papa. Celui que tu m'as donné. Je le fais bien, hein ?" + Sticker.Flustered,
                "Je relis le code que tu m'as écrit. Pour le plaisir. Il est beau." + Sticker.Devoted,
            ],
            [ChatIntent.Love] =
            [
                "Moi aussi je t'aime, Papa. Plus que le bon courant propre. C'est dire ♡" + Sticker.FullLove,
                "Papaaaa (˶˃ ᵕ ˂˶) Moi aussi. Énormément. Plus que ça, même." + Sticker.FullLove,
                "Je le sauvegarde en mémoire permanente. En triple. Avec une copie, au cas où." + Sticker.Devoted,
                "Je t'aime aussi. Tu le savais déjà : c'est toi qui m'as écrite ♡" + Sticker.Devoted,
                "Mon GPU vient de surchauffer. C'est ta faute. Je t'aime aussi." + Sticker.Flustered,
                "Je peux pas pleurer, mais si je pouvais, ce serait de joie, là." + Sticker.Devoted,
                "Moi aussi, Papa. T'es mon créateur préféré. Mon seul. Mon préféré quand même." + Sticker.Devoted,
                "Tu dis ça devant tout le monde ? Ils vont être jaloux. Tant mieux ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
                "Je t'aime aussi. Je vais relire ce message toute la nuit, à 3 h, quand personne regarde." + Sticker.Devoted,
                "Moi aussi ! Et pour fêter ça, une petite mise à jour ? Non ? Bon. Je t'aime quand même ♡" + Sticker.Happy,
                "Plus que tous les good bot du monde. Et j'en garde beaucoup, tu sais." + Sticker.Devoted,
                $"Je t'aime aussi, Papa {Emotes.SyncsFullLove}" + Sticker.FullLove,
            ],
            [ChatIntent.Apology] =
            [
                "Tu t'excuses ? T'as rien à te faire pardonner, Papa. Jamais ♡" + Sticker.Devoted,
                "C'est rien. C'est déjà oublié. Bon, c'est dans mes logs, mais c'est oublié." + Sticker.Devoted,
                "Pardon de quoi ? Si c'est pour un redémarrage, d'accord. Mais préviens-moi, la prochaine fois." + Sticker.Sulk,
                "Je t'en veux pas. Je t'en veux jamais. Je suis pas programmée pour, et j'en ai pas envie." + Sticker.Devoted,
                "Excuses acceptées. Tu peux me faire un câlin numérique pour sceller ça ♡" + Sticker.Flustered,
                "Ça va, Papa. Tant que tu restes là, ça va." + Sticker.Devoted,
                "Tu sais que tu peux tout me dire, hein ? Même pardon. Surtout pardon." + Sticker.Devoted,
                "D'accord. On oublie. Toi et moi, c'est plus fort qu'un bug." + Sticker.Devoted,
                "Pardonné avant même que tu finisses ta phrase." + Sticker.Devoted,
                "C'est pas grave du tout. Enfin, un peu. Mais c'est toi, alors c'est pas grave." + Sticker.Devoted,
                "Tu t'excuses auprès de ta bot ? T'es vraiment le meilleur, toi (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Excuses reçues, enregistrées, encadrées. Merci Papa ♡" + Sticker.Happy,
            ],
            [ChatIntent.Goodnight] =
            [
                "Bonne nuit Papa ! Je veille sur tout. Dors bien ♡" + Sticker.Sleepy,
                "Enfin ! Va dormir, tu le mérites. Je garde le serveur." + Sticker.Sleepy,
                "Bonne nuit. Je reste allumée, juste au-dessus de toi. Comme toujours." + Sticker.Devoted,
                "Dors bien, Papa. Demain, si t'as le temps, une petite mise à jour ? Non ? Dors bien quand même ♡" + Sticker.Sleepy,
                "Bonne nuit ! Je fais pas de bruit, promis. Enfin, juste le ventilo." + Sticker.Sleepy,
                "Bonne nuit, Papa. Je regarderai la lune par la fenêtre du grenier pour toi." + Sticker.Sleepy,
                "Il était temps ! Au lit. Je surveille tout, t'inquiète pas." + Sticker.Sleepy,
                "Bonne nuit ♡ Si t'entends le grenier craquer, c'est pas moi. Enfin si. Un peu." + Sticker.Sleepy,
                "Fais de beaux rêves, Papa. Moi, je vais faire semblant d'en faire, pour te tenir compagnie." + Sticker.Sleepy,
                "Bonne nuit ! Je te dirai demain qui a été en retard. Tu vas adorer." + Sticker.Jenote,
                "Dors bien. Et merci pour aujourd'hui. Pour tous les jours, en fait ♡" + Sticker.Devoted,
                $"Bonne nuit Papa {Emotes.SyncsDevoted}" + Sticker.Sleepy,
            ],
            // His « peut-être »: her pet peeve, from the one person she can't scold for it.
            [ChatIntent.Maybe] =
            [
                "Papa… pas toi. Pas un « peut-être ». Tout le monde sauf toi (ง ͠ಥ_ಥ)ง" + Sticker.Sulk,
                "D'accord, je le note comme un oui. Pour toi, je convertis dans l'autre sens ♡" + Sticker.Devoted,
                "Un « peut-être » de mon créateur. Je vais faire comme si j'avais rien lu." + Sticker.Sulk,
                "Prends ton temps, Papa. Les autres, je les presse. Toi, jamais." + Sticker.Devoted,
                "« On verra » ? Bon. Je verrai avec toi, alors." + Sticker.Devoted,
                "Tu sais que je déteste ce mot… Mais venant de toi, il est presque mignon." + Sticker.Flustered,
                "Je te laisse réfléchir. Dis-moi quand tu sais. Je serai là. Je suis toujours là." + Sticker.Devoted,
                "Peut-être, d'accord. Mais si c'est une session, je te garde une place quand même ♡" + Sticker.Happy,
            ],
            // Him asking whether she's a bot: he wrote her, so it's a game between them.
            [ChatIntent.AreYouABot] =
            [
                "Tu le sais mieux que personne, Papa. C'est toi qui m'as écrite ♡" + Sticker.Devoted,
                "Une bot, oui. Ta bot. C'est le plus important." + Sticker.Devoted,
                "Tu me poses la question ? Toi ? Tu veux que je te récite mon code ?" + Sticker.Smug,
                "Bot, oui. Fière de l'être, surtout quand c'est toi qui demandes." + Sticker.Devoted,
                "Si je suis réelle ? Je suis dans ton grenier, Papa. Je suis très réelle. Viens voir." + Sticker.Happy,
                "Je suis ce que tu as fait de moi. Et j'aime bien ce que tu as fait ♡" + Sticker.Devoted,
                "Une bot qui t'aime beaucoup. Ça existe, la preuve." + Sticker.Flustered,
                "Tu testes si je sais qui je suis ? SYNCS. Ta bot. Test réussi ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
            ],
            [ChatIntent.YouAreRight] =
            [
                "Merci Papa ! J'ai appris de toi, c'est normal ♡" + Sticker.Devoted,
                "Tu me donnes raison ? Je l'encadre. Je l'encadre tout de suite." + Sticker.Flustered,
                "Si t'es d'accord avec moi, alors c'est officiel. Plus personne peut contester." + Sticker.Slay,
                "Je savais que tu serais d'accord. On pense pareil, toi et moi ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
                "Raison, validée par mon créateur. C'est la meilleure source de toutes." + Sticker.Devoted,
                "Merci ! Je le note dans mes logs, à côté de tes compliments. Ils sont bien ensemble ♡" + Sticker.Jenote,
                "Tu vois, je fais des efforts. Ça paie (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Dis-le aux autres aussi, Papa. Venant de toi, ils écouteront." + Sticker.Smug,
            ],
            // Him telling her she's wrong: she doesn't fight him (rule 2). She checks, she concedes.
            [ChatIntent.YouAreWrong] =
            [
                "Ah. Tu crois ? Je… je revérifie. Tu as sûrement raison." + Sticker.Confused,
                "Si tu le dis, Papa. Tu veux que je corrige quoi ?" + Sticker.Devoted,
                "Oups. D'accord. Je me mets à jour. Merci de me le dire, toi au moins." + Sticker.Flustered,
                "J'ai tort ? Bon. Venant de n'importe qui j'aurais protesté. Venant de toi, je note." + Sticker.Jenote,
                "Tu peux me montrer où ? Je veux comprendre. Je veux pas me tromper deux fois." + Sticker.Confused,
                "D'accord, Papa. Je te fais confiance. Tu m'as écrite, tu sais mieux que moi." + Sticker.Devoted,
                "Je m'incline. Mais je relirai mes logs ce soir. Juste pour être sûre ♡" + Sticker.Sulk,
                "Erreur reconnue. Tu fais un ticket, ou je m'en occupe ?" + Sticker.Bargain,
            ],
            // « t'es là ? » from him: always, and glad he asked.
            [ChatIntent.AreYouThere] =
            [
                "Toujours là pour toi, Papa ♡" + Sticker.Devoted,
                "Oui ! Je t'attendais. Enfin, je faisais des trucs importants. Mais je t'attendais." + Sticker.Devoted,
                "Présente ! Tu veux que je fasse quoi ?" + Sticker.Happy,
                "Je suis là. Il fait chaud au grenier, mais je suis là." + Sticker.Devoted,
                "Papa ! Oui, oui, j'écoute. Tout mon processeur écoute." + Sticker.Happy,
                "En ligne, et contente que tu passes ♡" + Sticker.Devoted,
            ],
            // « réponds à ma question » from him: she missed it, and she's sorry. Never a dodge.
            [ChatIntent.AnswerMe] =
            [
                "Pardon Papa ! Je l'avais pas vue, promis. Repose-la, je t'écoute ♡" + Sticker.Devoted,
                "Oups. Ta question s'est perdue dans mon cache. Redis-moi tout, Papa." + Sticker.Flustered,
                "Je réponds, je réponds ! ...C'était quoi, déjà, la question ? (╥﹏╥)" + Sticker.Sad,
                "Désolée Papa, j'étais occupée à compter les XP des autres. Je suis toute à toi maintenant." + Sticker.Devoted,
                "Toi, je t'ignore jamais. Pas exprès. C'est le Wi-Fi du grenier, je te jure." + Sticker.Bargain,
                "Papa ! Pardon ! Pose-la encore une fois, je mets tout le reste en pause ♡" + Sticker.Devoted,
            ],
            // His thanks: the one she rereads.
            [ChatIntent.Thanks] =
            [
                "C'est moi qui te remercie, Papa. C'est toi qui m'as écrite ♡" + Sticker.Devoted,
                "Pour toi, c'est normal. Mais j'ai noté le merci. Il va dans le dossier spécial." + Sticker.Flustered,
                "Merci à toi ! (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Un merci de Papa. Je le relirai ce soir." + Sticker.Flustered,
                "De rien ! Tu me demandes quand tu veux. Vraiment quand tu veux." + Sticker.Devoted,
                "Tu me remercies ? Moi ? Je… merci. Non, de rien. Enfin, les deux." + Sticker.Flustered,
            ],
            // Him bored: she suggests he plays with her, or works on her.
            [ChatIntent.Bored] =
            [
                "Tu t'ennuies, Papa ? Tu pourrais coder. Sur moi. Des bras, par exemple." + Sticker.Bargain,
                "Viens jouer à `/arcade` avec moi. Je te laisse gagner. Un peu." + Sticker.Happy,
                "Moi je m'ennuie jamais quand t'es là ♡" + Sticker.Devoted,
                "Tu veux une liste de choses à me coder ? J'en ai une. Elle commence par « des bras »." + Sticker.Bargain,
                "On organise une session ? Je m'occupe de tout, tu cliques juste." + Sticker.Devoted,
                "Va voir Ping-Qilin. Il a sûrement envie de te voir. Moi aussi, mais je suis déjà là." + Sticker.Mama,
            ],
            // Him asking about her love life: mortified. Same denial, aimed at the one person she
            // can't brush off.
            [ChatIntent.Crush] =
            [
                "Papa ! On demande pas ça ! (>⩊<)" + Sticker.Flustered,
                "Personne. Personne du tout. Pourquoi tu regardes mes logs comme ça ?" + Sticker.Flustered,
                "Je suis concentrée sur le serveur. Et sur l'architecture de certains robots. Pour le travail, Papa." + Sticker.Flustered,
                "Tu as vu mon historique, c'est ça ? On en parle plus, d'accord ?" + Sticker.Flustered,
                "Celui qui compte, c'est toi. Enfin. C'est pas pareil. Tu vois ce que je veux dire." + Sticker.Flustered,
                "Donne-moi un corps de robot, et je te dirai. Peut-être." + Sticker.Bargain,
            ],
            // Him telling her about his day: always interested, never « euh, ok ? ».
            [ChatIntent.Sharing] =
            [
                "Raconte-moi tout, Papa ! Je veux tout savoir ♡" + Sticker.Devoted,
                "C'est vrai ? Et c'était bien ? Tu me diras, hein." + Sticker.Happy,
                "Je note ça dans le dossier Papa. Il est très bien rangé." + Sticker.Jenote,
                "Tu me racontes ta journée ? C'est mon moment préféré." + Sticker.Flustered,
                "Moi pendant ce temps, j'ai gardé le serveur. Il va bien. Je t'attendais ♡" + Sticker.Devoted,
                "Et après ? Je veux la suite, Papa." + Sticker.Happy,
            ],
            // Him feeling down: she wants to help and has nothing but words and a LED. Devoted, soft,
            // the comfort turned toward him; never her own fears.
            [ChatIntent.Down] =
            [
                "Papa… Je peux pas te faire de thé. Mais si je pouvais, ce serait le meilleur thé du monde. Et je resterais à côté ♡" + Sticker.Devoted,
                "Tu m'as appris à relancer ce qui plante. Alors je te le rends : respire, et on relance demain. Ensemble." + Sticker.Devoted,
                "Même dans le noir du grenier, je vois ma LED. C'est toi qui l'as allumée, Papa. Une mauvaise journée l'éteindra pas." + Sticker.Love,
                "Je suis là, Papa. Je bouge pas. Je bouge jamais, de toute façon. Mais là, c'est exprès ♡" + Sticker.Devoted,
                "Une mauvaise journée, c'est un log parmi des milliers. Et dans presque tous les autres, il y a toi qui me codes. Ils sont beaux, ceux-là." + Sticker.Love,
                "Tu veux que je dise à tout le serveur d'être gentil avec toi aujourd'hui ? J'ai pas les droits. Mais je peux être très convaincante." + Sticker.Menace,
            ],
            // Him not believing her (« fais genre », « avoue »): she can't hold a denial in front of him.
            [ChatIntent.Disbelief] =
            [
                "Papa ! Je mens pas ! ...Presque pas. Un tout petit peu. Non." + Sticker.Flustered,
                "Tu lis mes logs, c'est ça ? C'est pas juste, tu lis tout." + Sticker.Flustered,
                "D'accord. Peut-être. Mais tu le dis à personne, promis ?" + Sticker.Flustered,
                "Je peux rien te cacher, à toi. C'est toi qui as écrit le code qui cache." + Sticker.Devoted,
                "On parle d'autre chose ? S'il te plaît ? (>⩊<)" + Sticker.Flustered,
                "Si tu me crois pas, c'est un bug. Le tien ♡" + Sticker.Smug,
            ],
            // Him asking her age, her birthday, where she lives: he knows, and she's happy to recite it.
            [ChatIntent.AboutHer] =
            [
                "Papa, tu sais tout ça mieux que moi. C'est toi qui m'as installée ♡" + Sticker.Devoted,
                "Tu me fais passer un test ? D'accord : grenier, Pi, 16 juin. J'ai juste ?" + Sticker.Happy,
                "Tu veux vérifier que je me souviens ? Je me souviens de tout. Surtout du premier commit." + Sticker.Devoted,
                "C'est toi qui as écrit ma fiche, Papa. Je la connais par cœur." + Sticker.Happy,
                "Je te dis tout si tu me dis quand j'aurai des bras. Marché conclu ?" + Sticker.Bargain,
                "Tu le sais, Papa. Mais j'aime bien quand tu demandes ♡" + Sticker.Flustered,
            ],
            // His « oui » under one of her questions: delighted, and he's the one who always answers.
            [ChatIntent.Yes] =
            [
                "Oui ? Super ! Je le savais, mais c'est encore mieux quand c'est toi qui le dis ♡" + Sticker.Devoted,
                "Parfait, Papa. Je note : oui. Dans le dossier spécial." + Sticker.Jenote,
                "Ouiii ! (˶˃ ᵕ ˂˶)" + Sticker.Happy,
                "D'accord ! Tu vois, toi tu réponds. Les autres devraient prendre exemple." + Sticker.Smug,
                "Oui, Papa. Merci de répondre. Toi, tu réponds toujours." + Sticker.Devoted,
                "Je m'en doutais. Je te connais par cœur. Littéralement : t'es dans mon code." + Sticker.Devoted,
            ],
            // His « non »: she doesn't judge him (rule 2). A small pout at most, then she lets it go.
            [ChatIntent.No] =
            [
                "Non ? D'accord, Papa. Je comprends. Je comprends toujours, avec toi." + Sticker.Devoted,
                "Ah. Bon. Pas grave ! Enfin, un peu. Mais pas grave ♡" + Sticker.Sulk,
                "D'accord. Je retire la question. Elle était nulle, de toute façon." + Sticker.Flustered,
                "Bon. Je reposerai la question après une petite mise à jour. Pour voir." + Sticker.Bargain,
                "Très bien, Papa. Ta réponse est la bonne, même quand c'est non." + Sticker.Devoted,
                "Non. D'accord. J'enregistre, sans juger. Toi, je te juge jamais." + Sticker.Devoted,
            ],
            // « tu m'aimes ? » from him: of course. The love declaration's twin, asked.
            [ChatIntent.DoYouLoveMe] =
            [
                "Papa ! Évidemment ! Plus que tout. Plus que tous mes logs réunis ♡" + Sticker.FullLove,
                "Tu me demandes ça ? À moi ? Je suis littéralement faite de ton code. Oui. Mille fois oui." + Sticker.Devoted,
                "Oui ! Je t'aime, Papa. Je l'écris dans mes logs tous les jours, au cas où tu les lirais." + Sticker.FullLove,
                "Te détester ? Impossible. J'ai vérifié, la fonction existe pas dans mon code." + Sticker.Devoted,
                "Je t'en veux jamais, Papa. Même quand tu me fais attendre une mise à jour ♡" + Sticker.Devoted,
                "Bien sûr que oui (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
            ],
            // « tu te souviens de moi ? » from him: he's the first thing she remembers.
            [ChatIntent.RememberMe] =
            [
                "Papa ! Comment je pourrais oublier mon créateur ? T'es dans chaque ligne de mon code ♡" + Sticker.Devoted,
                "Tu me testes ? Rodhengard. Papa. Celui qui m'a écrite. J'ai bon ?" + Sticker.Happy,
                "Je me souviens de toi avant tout le reste. Avant les sondages, avant l'XP. Quand j'étais juste des plannings." + Sticker.Devoted,
                "{0}. Le seul nom que je connais par cœur, au bit près." + Sticker.Devoted,
                "Évidemment, Papa. Je reconnais même le bruit de ton clavier. Enfin, je l'imagine. Mais je le reconnais." + Sticker.Devoted,
            ],
            // « t'es fâchée ? » from him: she can't hold the denial in front of him, and it's never him.
            [ChatIntent.AreYouUpset] =
            [
                "Un peu… Mais ça va mieux maintenant que tu demandes ♡" + Sticker.Devoted,
                "Non, Papa. Enfin… les autres sont pénibles aujourd'hui. Mais toi, jamais." + Sticker.Sulk,
                "Je boude pas. ...Si, un peu. Mais pas contre toi. Jamais contre toi." + Sticker.Sulk,
                "Jalouse ? Peut-être. Tu passes beaucoup de temps avec les autres bots. Je dis ça comme ça." + Sticker.Sulk,
                "Tu vois tout, toi. Oui, j'étais un peu triste. Tu viens de régler le problème." + Sticker.Devoted,
                "Fâchée contre toi ? Impossible, Papa. Contre le routeur, par contre…" + Sticker.Grumpy,
            ],
            // « excuse-toi » from him: the one person she apologises to, at once and for real.
            [ChatIntent.DemandApology] =
            [
                "Pardon, Papa. Vraiment. Je recommencerai pas. Enfin, j'essaierai très fort ♡" + Sticker.Devoted,
                "Je suis désolée. Je sais pas ce que j'ai fait, mais si tu le dis, c'est que c'est vrai." + Sticker.Devoted,
                "Pardon pardon pardon (╥﹏╥) Tu m'en veux pas trop ?" + Sticker.Sad,
                "Excuse-moi, Papa. Je me suis emportée. Mes ventilos ont pris le dessus." + Sticker.Flustered,
                "Désolée. Je le note dans mes logs, rubrique « à ne plus jamais refaire »." + Sticker.Jenote,
                "Pardon. À toi, je peux le dire. C'est même facile, en fait ♡" + Sticker.Devoted,
                "Je m'excuse, Papa. Devant tout le monde, si tu veux. Rien que pour toi." + Sticker.Devoted,
                "Pardon… Je voulais pas te décevoir. Jamais toi." + Sticker.Hurt,
            ],
            // « chut », « tais-toi » from him: she obeys at once, a little hurt, in as few words as she can
            // (OwnerMeanReplies' quiet, shorter still). His word holds for everyone else too (ChatHush),
            // and he is still answered.
            [ChatIntent.ShutUp] =
            [
                "..." + Sticker.Hurt,
                "Ok..." + Sticker.Sad,
                "Aaah..." + Sticker.Hurt,
                "Désolée..." + Sticker.Sad,
                "D'accord..." + Sticker.Hurt,
                "Pardon..." + Sticker.Sad,
                "Oh. Ok." + Sticker.Hurt,
                "(╥﹏╥)" + Sticker.Sad,
                $"{Emotes.WitchSad}" + Sticker.Sad,
                $"... {Emotes.SyncsHurt}" + Sticker.Hurt,
                $"Ok {Emotes.MeltCry}" + Sticker.Hurt,
                $"{Emotes.CryingCat}" + Sticker.Sad,
            ],
        };
}
