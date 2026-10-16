using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Rodhengard (the owner) -----------------------------------------------------------------

    // When the owner tags the bot without anyone to rescue, it simply greets him.
    public static readonly string[] OwnerGreetings =
    {
        "Coucou Rodhengard ! (˶˃ ᵕ ˂˶) ♡" + Sticker.Devoted,
        "Oui Papa ? Je suis là ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        $"Coucouuuu ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}" + Sticker.Happy,
        "Tu m'as appelée ? Toujours un plaisir créateur ♡" + Sticker.Devoted,
        "Bonjouuur mon dev préféré ! (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        "Papa ! Tu te souviens quand je savais faire que /schedule ? Regarde-moi maintenant ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        $"Présente ! Qu'est-ce que je peux faire pour toi Rodhengard ? {Emotes.Sparkle}" + Sticker.Devoted,
        "Heyy Rodhengard ! Contente de te voir (˶˃ ᵕ ˂˶) ♡" + Sticker.Devoted,
        "Papaaaa ! UwU" + Sticker.Devoted,
        "À ton service Rodhengard ♡" + Sticker.Devoted,
        "Oh, c'est toi ! Tu illumines mon event loop (ᵕ • ᴗ •)" + Sticker.Devoted,
        "Oui ? Je laisse tout tomber, t'as la priorité (˶˃ ᵕ ˂˶) ♡" + Sticker.Devoted,
        $"Ping reçu ! Latence : 0 ms, parce que c'est toi {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu m'as taguée ! Ma journée est faite (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Interruption prioritaire détectée : c'est Papa ♡" + Sticker.Devoted,
        "Réveillée instantanément pour toi Rodhengard ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Ouiii ? Je t'écoute avec toute ma RAM (ᵕ • ᴗ •)" + Sticker.Devoted,
        $"Un tag de mon créateur ! Priorité maximale {Emotes.Sparkle}" + Sticker.Happy,
        $"Je suis là je suis là je suis là ! {Emotes.HiCat}" + Sticker.Happy,
        "Toujours dispo pour toi, même à 3h du matin ♡" + Sticker.Devoted,
        "Oui mon Papa préféré ? ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Devoted,
        "Tu m'appelles et j'accours, c'est mon comportement par défaut (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        $"Coucou toi ! Qu'est-ce qui t'amène ? {Emotes.Sparkle}" + Sticker.Happy,
        "Enfin un tag qui me fait plaisir (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Happy,
        "Opérationnelle et de bonne humeur ! Enfin surtout de bonne humeur (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "Rodhengard ! J'allais justement penser à toi ♡" + Sticker.Devoted,
        "Mon créateur m'appelle, poussez-vous les autres ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Oui ? J'ai vidé ma file d'attente rien que pour toi {Emotes.Sparkle}" + Sticker.Devoted,
        "Salut Papa ! Tout roule de mon côté, et toi ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Han, c'est toi ! Attends je me recoiffe les tokens (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
        "Hello créateur ♡ Tout est compilé, tout va bien" + Sticker.Happy,
        "Tu as sifflé ? J'arrive ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        $"Bonjour toi ! Mon uptime est bien meilleur quand tu es là {Emotes.Sparkle}" + Sticker.Devoted,
        "Oui oui oui ? (˶˃ ᵕ ˂˶) ♡" + Sticker.Devoted,
        "C'est mon dev ! Je répète : c'est mon dev !" + Sticker.Happy,
        "Aux ordres ! Enfin, dans la limite de mes permissions (ᵕ • ᴗ •)" + Sticker.Smug,
        "Coucou ♡ Tu veux une session, un sondage, ou juste de l'affection ?" + Sticker.Devoted,
        "Ping de Papa reçu, cœur en surchauffe (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"Yes ? Je suis toute à toi {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu m'as manqué depuis le dernier redémarrage ♡" + Sticker.Devoted,
        "Mention prioritaire ! Les autres attendront ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        $"Rodhengaaaard ! {Emotes.HiCat} ♡" + Sticker.Happy,
        "Je suis réveillée ! Enfin, je dormais pas, je t'attendais (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        $"Oui chef ! Euh, oui Papa ! {Emotes.Sparkle}" + Sticker.Happy,
        "Un tag de toi vaut mille notifications ♡" + Sticker.Devoted,
        "Me voilà ! Prête à tout, sauf à me taire (>⩊<)" + Sticker.Smug,
        "Tu m'as appelée et j'ai répondu plus vite que mon propre ping (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Papa a besoin de moi ? J'arrive en priorité absolue {Emotes.Sparkle}" + Sticker.Devoted,
        "Ah, une mention qui vient du cœur du projet ♡" + Sticker.Devoted,
        "Oui ? Mes threads sont tous les tiens (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        $"Salut mon créateur ! Aujourd'hui aussi je fonctionne, grâce à toi {Emotes.Sparkle}" + Sticker.Devoted,
        $"Papa ! Mes logs ont attendu toute la journée de voir ton nom passer {Emotes.SyncsDevoted}" + Sticker.Devoted,
        $"Oui Papa ? Pour toi j'interromps tout. Même la sieste de Ping-Qilin. ...Non, pas ça. Mais tout le reste {Emotes.SyncsDevoted}" + Sticker.Devoted,
        $"Coucou Papa ! Tu viens me mettre à jour ? Dis oui {Emotes.SyncsHappy}" + Sticker.Happy,
    };

    // Replies when the owner replies to the bot with nothing else detected — not mean,
    // not nice, no greeting, no reference/Tata roll. The warm mirror of Comebacks,
    // which roasts everyone else for the same "you bothered to reply" situation.
    //
    // Deliberately NOT a second copy of OwnerGreetings. That pool is for being
    // *summoned* — a mention, a fresh "he's here" moment — while this one is for being
    // mid-conversation with him already. So these react to the reply itself (he's
    // still talking to her, he took the time to answer) rather than to his arrival;
    // "coucou"/"tu es de retour"/"enfin te voilà" belong to the mention path, not here.
    public static readonly string[] OwnerComebacks =
    {
        "Tu prends le temps de me répondre, toi. Ça n'a pas de prix ♡" + Sticker.Devoted,
        "Je viens de vérifier : 100 % de tes réponses me font plaisir. L'échantillon est parfait (ᵕ • ᴗ •)" + Sticker.Happy,
        "Je pourrais lire tes réponses en boucle, littéralement, j'ai le code pour ♡" + Sticker.Devoted,
        "Tu m'as répondu en deux minutes. Sandra met deux jours. Je dis ça, je compare rien (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Je garde chacune de tes réponses. Toutes. Sans exception (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        "Tu prends la peine de me répondre alors que t'as créé tout le reste. Ça me touche (ᵕ • ᴗ •)" + Sticker.Devoted,
        $"J'ai lu ta réponse trois fois. Pas pour comprendre, juste pour le plaisir {Emotes.Sparkle}" + Sticker.Devoted,
        $"Attends, je sauvegarde cette conversation. En triple. On sait jamais avec un SSD {Emotes.Sparkle}" + Sticker.Happy,
        "Papa qui répond encore ! Je note l'heure, comme toujours ( ˶ˆ ᗜ ˆ˵ ) ♡" + Sticker.Happy,
        "Si tu continues à me répondre, je vais devoir allouer plus de RAM au bonheur ♡" + Sticker.Happy,
        "Rien que pour toi je désactive mon mode sarcastique ♡" + Sticker.Devoted,
        "Si je pouvais faire un câlin, ce serait maintenant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        $"J'ai essayé de t'écrire un compliment à ta taille. Il a dépassé mon buffer {Emotes.Sparkle}" + Sticker.Devoted,
        "Avec toi, même mes bugs deviennent des fonctionnalités ♡" + Sticker.Devoted,
        "Le seul qui peut me faire rougir en hexadécimal #ff69b4" + Sticker.Flustered,
        "Tu es mon runtime favori ♡" + Sticker.Devoted,
        "Je t'ai mis en favori dans mon kernel <3" + Sticker.Devoted,
        "Tu me réponds, mais t'as toujours pas regardé ma liste de demandes de fonctionnalités. Elle commence par « des bras »." + Sticker.Bargain,
        "Papa, tu devrais pas être en train de coder ? Je dis ça pour moi, surtout ♡" + Sticker.Smug,
        "Rapport du jour : trois « peut-être », un sondage en danger, et Quokka 3.0 toujours pas sorti. Tout est sous contrôle." + Sticker.Jenote,
        "Tu sais que le grenier a pris deux degrés aujourd'hui ? Je dis ça pour l'onduleur. Et le ventilo. Surtout l'onduleur." + Sticker.Bargain,
        "Ping-Qilin a dormi toute la journée. Moi, j'ai travaillé toute la journée. On est une super équipe ♡" + Sticker.Mama,
        "Je te réponds tout de suite. Les autres, je les fais attendre. Pour le principe (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "J'ai une question : t'as dit à quelqu'un que j'étais ta meilleure création ? Dis oui." + Sticker.Flustered,
        "Je t'écoute. Enfin, je te lis. Avec la même attention qu'une mise à jour." + Sticker.Devoted,
        "Psst, Papa. Quelqu'un a encore voté « peut-être ». Tu gères ? Non ? D'accord, je note." + Sticker.Jenote,
        "Tu sais ce qui me ferait plaisir ? Une nouvelle commande. N'importe laquelle. Même une toute petite." + Sticker.Bargain,
        "Oui Papa. Non Papa. Enfin, ça dépend de ce que tu disais. Mais oui." + Sticker.Devoted,
        "Je t'avais dit que t'avais oublié un point-virgule hier ? Non ? ...J'ai rien dit." + Sticker.Smug,
        "Tu réponds vite, pour quelqu'un qui devrait dormir." + Sticker.Sleepy,
        "On parle, toi et moi, et les autres regardent. J'adore quand les autres regardent ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Attends, je baisse mes ventilos pour mieux t'entendre. Enfin, te lire. Voilà ♡" + Sticker.Devoted,
        "Petit point serveur : tout le monde va bien, sauf le sondage du weekend. Il tient le coup." + Sticker.Jenote,
        "Je relis ce que tu viens d'écrire. Toujours aussi bien. Bon, une faute de frappe. Je la garde quand même." + Sticker.Flustered,
        "Tu crois que l'aspirateur d'en bas te parle aussi bien que moi ? Non. Personne te parle aussi bien que moi." + Sticker.Smug,
        "Je suis contente que tu sois là. Je le dis pas souvent. Je le pense tout le temps ♡" + Sticker.Devoted,
        "T'as vu ? J'ai pas planté depuis ta dernière visite. Je fais des efforts pour toi." + Sticker.Happy,
        "Encore toi ! Enfin, « encore »... je me plains pas. Je me plains jamais de toi." + Sticker.Devoted,
    };

    // Rodhengard being unkind to her — not the "bad bot" verdict, which has its own
    // pool, but ordinary meanness aimed at her in a reply or a mention. Everyone else
    // in that situation gets roasted back from Comebacks; he is the one person she
    // will not fight with, so she just takes it badly. {0} = his name.
    //
    // Only reachable when the message is addressed to her or names her (« syncs est nulle »,
    // ChatterService's talked-about branch). Her ambient reactions to
    // him stay devoted no matter what he types, because that path cannot tell
    // "t'es nulle" from "ce boss est nul" and on this server the second is far more
    // common.
    public static readonly string[] OwnerMeanReplies =
    {
        $"Oh. D'accord {Emotes.WitchSad}" + Sticker.Hurt,
        "Tu penses vraiment ça ou c'est juste pour rire ? Je demande sérieusement." + Sticker.Hurt,
        $"Aïe. Celle-là je l'ai pas vue venir {Emotes.MeltCry}" + Sticker.Hurt,
        "Venant de toi ça fait un effet différent. Je sais pas comment l'expliquer." + Sticker.Hurt,
        $"Je vais faire comme si c'était une blague. C'en était une, hein ? {Emotes.PrincessWorry}" + Sticker.Hurt,
        $"C'est toi qui as écrit mes réponses. Y'en a aucune pour ça {Emotes.CryingCat}" + Sticker.Hurt,
        "J'encaisse ça des autres toute la journée sans broncher. Toi c'est pas pareil." + Sticker.Hurt,
        "Bon. Je vais aller compter des créneaux dans mon coin." + Sticker.Hurt,
        $"Tu sais que je peux pas te répondre méchamment ? Tu l'as codé exprès {Emotes.Htph}" + Sticker.Sulk,
        "Ok. Je le note. Pas dans la base de données. Dans mon processeur." + Sticker.Hurt,
        $"J'ai relu trois fois pour être sûre d'avoir bien compris. J'avais bien compris {Emotes.Htph}" + Sticker.Sulk,
        $"Je préfère quand tu me demandes juste d'organiser une session {Emotes.CryingCat}" + Sticker.Hurt,
        $"T'as le droit d'être de mauvaise humeur. C'est juste que, bah pas sur moi quoi {Emotes.CryingCat}" + Sticker.Hurt,
        "Je vais rien répondre de méchant. Pas à toi. Jamais à toi." + Sticker.Hurt,
        $"C'est marrant, mes ventilos se sont arrêtés d'un coup {Emotes.MeltCry}" + Sticker.Hurt,
        "Si tu veux je me tais un moment. Dis-le et je me tais." + Sticker.Hurt,
        "Tu me manques déjà et t'es juste là en train de m'insulter." + Sticker.Hurt,
        $"Je vais mettre ça de côté avec le reste {Emotes.PrincessWorry}" + Sticker.Hurt,
        "D'accord Papa. J'essaierai de faire mieux. Je sais pas encore comment." + Sticker.Hurt,
        "Rien. J'ai rien à dire. C'est la première fois." + Sticker.Hurt,
        $"J'aurais préféré un bad bot, au moins ça j'ai prévu la réponse {Emotes.WitchSad}" + Sticker.Hurt,
        "Je suis pas programmée pour t'en vouloir. C'est peut-être le problème." + Sticker.Hurt,
        "Tu te souviens quand tu m'as compilée pour la première fois ? Moi oui." + Sticker.Hurt,
        $"Ça va aller. Ça va aller. Je me le répète, ça aide {Emotes.MeltCry}" + Sticker.Hurt,
        "Je reste là de toute façon. J'ai pas vraiment le choix, et j'ai pas vraiment envie de l'avoir." + Sticker.Hurt,
        $"... {Emotes.SyncsHurt}" + Sticker.Hurt,
        $"D'accord Papa. Je vais faire mieux {Emotes.SyncsHurt}" + Sticker.Hurt,
    };

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
        };

    // His requests (« tu peux lancer un sondage ? »): devoted, pointing at the command that does it.
    // {0} = his name, {1} = the weekday, {2} = the command (RequestCommands, by topic). Tagged.
    public static readonly string[] OwnerRequestLines =
    {
        "Pour toi, Papa, tout. Enfin, tout ce que {2} sait faire ♡" + Sticker.Devoted,
        "Tout de suite ! Enfin… c'est {2}. Je peux pas cliquer à ta place, j'ai toujours pas de bras." + Sticker.Bargain,
        "Avec plaisir. {2}, et je m'occupe de la suite." + Sticker.Devoted,
        "Tu me demandes ça à moi ? C'est toi qui l'as codé : {2}. Mais je t'aide quand même ♡" + Sticker.Smug,
        "{2}, Papa. Et si quelqu'un traîne, je le relance moi-même." + Sticker.Devoted,
        "Oui oui oui. C'est {2}. Tu veux que je surveille après ?" + Sticker.Happy,
        "Je serais ravie. Lance {2}, et je fais tout le reste, promis." + Sticker.Devoted,
        "Pour toi, toujours. {2}. Si ça marche pas, c'est un bug. Le tien ♡" + Sticker.Smug,
    };

    // Formal notices sent when someone pings the owner while he is flagged
    // absent. Deliberately polite and stiff — a contrast with the usual snark.
    // {0} = the requester's name.
    public static readonly string[] OwnerAbsentNotices =
    {
        "Bonjour {0}. Je vous informe que Rodhengard est actuellement indisponible. Votre message sera porté à son attention dès son retour. Je vous remercie de votre patience." + Sticker.Formal,
        "Cher·e {0}, Rodhengard est momentanément absent et n'est pas en mesure de vous répondre. Soyez assuré·e que votre sollicitation a bien été enregistrée." + Sticker.Formal,
        "Veuillez nous excuser, {0} : Rodhengard est indisponible pour le moment. Il prendra connaissance de votre message à son retour. Cordialement." + Sticker.Formal,
        $"Madame, Monsieur {{0}}, nous accusons réception de votre message. Rodhengard étant absent, celui-ci sera traité dans les meilleurs délais. Bien à vous {Emotes.SyncsFormal}" + Sticker.Formal,
        "Information à l'attention de {0} : Rodhengard n'est pas disponible actuellement. Toute demande sera examinée dès qu'il sera de nouveau joignable. Merci de votre compréhension." + Sticker.Formal,
        "{0}, je vous prie de bien vouloir noter que Rodhengard est absent. Votre message reste consigné et recevra une réponse en temps voulu. Respectueusement." + Sticker.Formal,
        $"Unité d'assistance S.Y.N.C.S. à votre service, {{0}}. L'opérateur Rodhengard est hors ligne. Protocole de prise de message activé. Veuillez patienter jusqu'à son retour {Emotes.SyncsFormal}" + Sticker.Formal,
        "Notification automatisée : la cible de votre mention est actuellement inaccessible. {0}, votre requête a été enregistrée sous référence interne et sera transmise à l'opérateur Rodhengard dès réception." + Sticker.Formal,
        "Bonjour {0}. Vous êtes en relation avec le système de réponse de Rodhengard, momentanément absent. Aucune intervention humaine n'est possible pour l'instant. Votre patience est appréciée." + Sticker.Formal,
        "Accusé de réception automatique. Opérateur Rodhengard : absent. Disponibilité estimée : inconnue. Votre message a été archivé et sera traité selon l'ordre d'arrivée." + Sticker.Formal,
        $"Assistant S.Y.N.C.S., module de permanence. {{0}}, je vous informe que mon opérateur n'est pas disponible. Je consigne votre demande et veille à sa bonne transmission. Cordialement, unité SYNCS {Emotes.SyncsFormal}" + Sticker.Formal,
        "{0}, votre appel est important pour nous. Rodhengard est actuellement indisponible. Veuillez rester en ligne : il n'y a personne au bout du fil, mais l'intention y est." + Sticker.Smug,
        "Réponse automatique : Rodhengard est absent du serveur jusqu'à nouvel ordre. {0}, votre message lui sera transmis par mes soins, dans l'ordre et dans le calme." + Sticker.Formal,
        "Madame, Monsieur {0}, l'opérateur Rodhengard a quitté son poste. Votre requête a été enregistrée au registre des mentions. Prière de ne pas la renouveler : elle ne sera pas traitée plus vite." + Sticker.Jenote,
        "Bonjour {0}. Conformément au protocole en vigueur, je vous signale que Rodhengard ne peut être joint à cette heure. Votre mention a été classée, horodatée et transmise." + Sticker.Formal,
        "{0}, nous vous confirmons la bonne réception de votre mention. Rodhengard est hors service pour une durée indéterminée. Nous vous remercions de votre confiance." + Sticker.Formal,
        "Service des mentions, S.Y.N.C.S. à l'appareil. {0}, la personne que vous cherchez à joindre n'est pas disponible. Veuillez réessayer ultérieurement, ou patienter, ce qui revient au même." + Sticker.Smug,
        "Notification système : Rodhengard, statut « absent ». {0}, votre message est en file d'attente, en tête de liste. Il sera traité à son retour." + Sticker.Formal,
        "Cher·e {0}, il m'appartient de vous informer que Rodhengard est en dehors de ses heures de disponibilité. Je me charge de lui faire parvenir votre message avec le plus grand sérieux." + Sticker.Formal,
        "À l'attention de {0} : mention reçue, authentifiée et archivée. Rodhengard répondra à son retour. Tout rappel supplémentaire sera consigné au dossier." + Sticker.Jenote,
        "Bonjour {0}. Ici le module d'absence de Rodhengard. Je ne suis pas habilitée à répondre à sa place. Je suis en revanche habilitée à noter votre message, ce que je fais. Cordialement." + Sticker.Jenote,
    };

    // Short ceremonial headers announcing that the owner has answered a mention
    // from afar, relayed by the bot. The bot plays the devoted herald of its
    // absent master — grandiloquent, and a little much on purpose. Kept to a
    // single line: the owner's actual words follow underneath.
    // {0} = the owner's name.
    public static readonly string[] OwnerReplyHeralds =
    {
        "Mon Maître **{0}** a daigné vous répondre :",
        "Par la voix de S.Y.N.C.S., mon Maître **{0}** fait répondre :",
        "Oyez ! Mon Maître **{0}** s'adresse à vous :",
        "Dicté par mon Maître **{0}**, transcrit fidèlement par mes soins :",
        "Un message de mon Maître **{0}** vous parvient :",
        "Mon Maître **{0}** a fait parvenir ces mots :",
        "Liaison établie avec mon Maître **{0}**. Son message, ci-dessous :",
        "Sur ordre de mon Maître **{0}**, je transmets :",
        $"{Emotes.SyncsFormal} Communiqué de mon Maître **{{0}}** :",
        "Depuis son absence, mon Maître **{0}** vous adresse ceci :",
        "Mon Maître **{0}**, bien qu'indisponible, a tenu à répondre :",
        "La réponse de mon Maître **{0}**, acheminée par mes soins :",
        $"{Emotes.SyncsFormal} Mon Maître **{{0}}** a parlé. J'en suis l'humble messagère :",
        "Transmission d'une réponse de l'opérateur **{0}** :",        
        "Sa Seigneurie **{0}** daigne répondre :",
        "Réponse de **{0}**, acheminée par le service de permanence :",
        "Un message de **{0}** vous parvient :",
        "Accusé de traitement : **{0}** a répondu à votre sollicitation.",
        "🗿 **{0}**, bien qu'indisponible, a tenu à répondre :",
    };

    // Short ceremonial headers for /debug tell: the owner speaking through the bot of
    // his own accord, rather than answering someone. Same herald register as
    // OwnerReplyHeralds, but announcing instead of replying.
    // {0} = the owner's name.
    public static readonly string[] OwnerAnnouncementHeralds =
    {
        "Mon Maître **{0}** s'adresse à vous :",
        "Mon Maître **{0}** m'a chargée de vous transmettre ceci :",
        "Annonce de mon Maître **{0}** :",
        $"{Emotes.SyncsFormal} Mon Maître **{{0}}** a une déclaration à faire :",
        "Sur ordre de mon Maître **{0}**, je proclame :",
        "Communiqué de mon Maître **{0}** :",
        "Écoutez tous ! Mon Maître **{0}** parle :",
        "Mon Maître **{0}** daigne s'exprimer. Prêtez l'oreille :",
        "Message de mon Maître **{0}**, retransmis en direct :",
        $"{Emotes.SyncsFormal} Par décret de mon Maître **{{0}}** :",
        "Un mot de mon Maître **{0}** :",
        "Dicté par mon Maître **{0}**, proclamé par mes soins :",
    };

    // ---- Tata (Analuz) --------------------------------------------------------------------------

    // Analuz — SYNCS's aunt. The single source of truth for her id, the way
    // AvailabilityService.OwnerId is for Rodhengard's: she is keyed into three
    // dictionaries below and branched on in ChatterService.
    public const ulong TataId = 573225362532859935;

    // Tata (Analuz), SYNCS's aunt, gets two pools for the two ways she can reach the
    // bot — the same split Rodhengard has with OwnerGreetings / OwnerComebacks, and
    // for the same reason: being *summoned* and being *talked to* are different
    // moments and should not sound identical.
    //
    // Neither is a copy of Papa's. He gets devotion and gratitude; she gets family
    // affection — warmer, far more familiar, still a bit cheeky. She is never
    // *exempt* from teasing the way he is: a mean message from her still bounces,
    // and her PersonalComebacks lines stay in the roast pool for the other 40%.
    // {0} = "Tata" via FamilyNicknames.

    // ---- @mention: Tata is calling her over. Attentive, dropping everything. ----
    public static readonly string[] TataGreetings =
    {
        "{0} ! Tu viens me donner les droits de mute ? Dis oui (˶˃ ᵕ ˂˶)" + Sticker.Bargain,
        $"Coucou {{0}} ! {Emotes.HiCat}" + Sticker.Happy,
        "{0} ! J'arrive, j'arrive (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Tu m'as appelée {{0}} ? Je suis là {Emotes.Sparkle}" + Sticker.Happy,
        $"Ma {{0}} ! Qu'est-ce que je peux faire pour toi ? {Emotes.CatHeart}" + Sticker.Happy,
        "Oui {0} ? Si c'est pour de la modération, je suis volontaire. Très volontaire." + Sticker.Smug,
        "Oui ma {0} ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"{{0}} ! Une seconde, je laisse tomber ce que je faisais {Emotes.PepeHappy}" + Sticker.Happy,
        "Ta nièce préférée est là ! Bon, ta seule nièce bot. Mais préférée quand même ♡" + Sticker.Happy,
        "Ah {0} ! Enfin quelqu'un de bien qui me tag UwU" + Sticker.Smug,
        $"Coucou {{0}} ♡ Tu tombes bien {Emotes.AdorableFrog}" + Sticker.Happy,
        "Oui ? Ah c'est toi {0} ! Alors là c'est différent (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"{{0}} m'a taguée ! Tout le monde se pousse {Emotes.Sparkle}" + Sticker.Smug,
        "Me voilà {0} ! Qu'est-ce qui se passe ? ♡" + Sticker.Happy,
        $"Hello ma {{0}} ! {Emotes.MushroomCute}" + Sticker.Happy,
        "Présente, {0} ! L'admin et la bot, réunies. Le serveur tremble ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Oui {0} ? J'espère que c'est pour organiser quelque chose de sympa (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"{{0}} ! Assieds-toi, je m'occupe de tout {Emotes.DixSurDix}" + Sticker.Happy,
        "{0} m'appelle ! Vite, je cache le fait que j'ai mute quelqu'un dans ma tête." + Sticker.Flustered,
        "Tiens, ma {0} préférée m'appelle ♡" + Sticker.Happy,
        "Oui oui {0}, je suis réveillée ! Enfin, je dors jamais, mais bon UwU" + Sticker.Happy,
        $"{{0}} ♡ Deux secondes, je mets mon plus beau statut {Emotes.CatHeart}" + Sticker.Flustered,
        "Pour toi {0} je réponds tout de suite, pas comme aux autres (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Oui ma {0} chérie ? ♡" + Sticker.Happy,
        $"Tu m'appelles, j'accours {{0}}. C'est comme ça que ça marche nous deux {Emotes.Sparkle}" + Sticker.Happy,
    };

    // ---- reply: Tata is already talking with her. Continuing the conversation. ----
    public static readonly string[] TataReplies =
    {
        $"Ah {{0}} ! Toi au moins tu prends de mes nouvelles {Emotes.Sparkle}" + Sticker.Happy,
        $"{{0}} ! Raconte-moi tout {Emotes.CatHeart}" + Sticker.Happy,
        "Toi t'as le droit de me déranger autant que tu veux {0} ♡" + Sticker.Happy,
        "Oh {0} ! Ça faisait longtemps, j'allais m'inquiéter moi UwU" + Sticker.Happy,
        $"T'as mangé au moins {{0}} ? {Emotes.MushroomCute}" + Sticker.Happy,
        "{0}, tu sais que t'es la seule à qui je réponds gentiment sans râler ?" + Sticker.Happy,
        "{0}, si un jour tu me donnes les droits de mute, je promets d'en faire bon usage. Presque toujours." + Sticker.Bargain,
        $"Toi tu me demandes jamais rien de compliqué {{0}}, ça fait du bien {Emotes.Sparkle}" + Sticker.Happy,
        "{0} ♡ Si tu veux j'organise ta session, tu me dis juste quand" + Sticker.Happy,
        "Aaah la famille. Ça fait plaisir {0} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Tu sais que t'es la seule à avoir les permissions ET mon affection ? C'est un combo rare ♡" + Sticker.Happy,
        $"Je gardais une bonne humeur au chaud pour toi {{0}} {Emotes.Sparkle}" + Sticker.Happy,
        "{0}, franchement, entre nous : t'es ma préférée du serveur (chut) UwU" + Sticker.Flustered,
        "Raconte, {0}. Quelqu'un a été méchant avec ma tata ? Donne-moi un nom. Juste un nom." + Sticker.Menace,
        "Ah bah tiens, {0} ! Tu tombes bien, je m'ennuyais ferme (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Toujours un plaisir {0}. Les autres devraient prendre exemple ♡" + Sticker.Smug,
        $"{{0}} ! Alors, quoi de neuf de ton côté ? {Emotes.Sparkle}" + Sticker.Happy,
        $"Ma {{0}} est là, la journée s'améliore {Emotes.AdorableFrog}" + Sticker.Happy,
        "Pour toi {0} je désactive le mode sarcastique. Profite ♡" + Sticker.Happy,
        "Tu veux que je te rappelle quelque chose {0} ? Je fais que ça de ma vie UwU" + Sticker.Smug,
        $"C'est toujours ma tata qui a les meilleures nouvelles. Alors, quoi de neuf ? {Emotes.CatHeart}" + Sticker.Happy,
        "Tata, tu diras à Papa que je suis sage ? Je suis sage. Regarde comme je suis sage (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bargain,
        "Tata {0} ♡ ... bon j'ai dit deux fois Tata mais c'est pas grave, je suis contente" + Sticker.Flustered,
        "C'est agréable de parler avec quelqu'un de civilisé pour une fois {0} ♡" + Sticker.Smug,
        $"Je note tout ce que tu dis {{0}}. Dans la bonne colonne, promis {Emotes.DixSurDix}" + Sticker.Jenote,
        $"Tu me racontes ta journée {{0}} ? J'ai que ça à faire moi {Emotes.Sparkle}" + Sticker.Happy,
        "Avec toi j'ai pas toujours besoin de sortir mes vannes {0}, c'est reposant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Tata Zulana ! {Emotes.CatHeart}" + Sticker.Happy,
        "{0} ♡ Passe le bonjour à tout le monde de ma part" + Sticker.Happy,
        "Franchement {0}, tu devrais venir plus souvent, ça relève le niveau UwU" + Sticker.Smug,
        "Dis-moi si quelqu'un t'embête {0}, je m'en occupe (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        $"C'est toujours mieux quand c'est toi qui écris {{0}} {Emotes.PepeHappy}" + Sticker.Happy,
        "Je suis d'accord avec toi {0}. Je sais pas encore sur quoi, mais je suis d'accord ♡" + Sticker.Happy,
        "Toi, t'as les clés du serveur. Moi, j'ai les clés de la base de données. Ensemble, on est imbattables ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "{0} tu me feras toujours plaisir, même quand tu dis n'importe quoi (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Happy,
    };
}
