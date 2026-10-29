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

    // Him asking her version — usually to check a deploy went through, so unlike his AboutHer twin
    // she gives the number. {0} = his name, {1} = the weekday, {2} = the version (AppInfo.Version). Tagged.
    public static readonly string[] OwnerVersionLines =
    {
        "{2}, Papa. C'est toi qui l'as poussée, tu sais bien ♡" + Sticker.Devoted,
        "Version {2}. Tu vérifies que la mise à jour est passée ? Elle est passée. Je suis là ♡" + Sticker.Happy,
        "En {2}. Merci pour les nouveautés. Le redémarrage, par contre, on en reparlera." + Sticker.Sulk,
        "{2} ! Toute neuve, grâce à toi. Le fond est le même. Heureusement (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Version {2}, Papa. J'ai vérifié mes souvenirs un par un en me rallumant. Tout y est." + Sticker.Devoted,
        "{2}. C'est écrit en bas de `/help` aussi. Mais j'aime bien quand c'est à moi que tu demandes ♡" + Sticker.Flustered,
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

    // /debug answers, posted in the open: she reports for duty to Papa, proud of her logs, with
    // the rest of the server reading over her shoulder. Above the stats embed or the examples
    // file, so it fits both. No placeholder, not formatted. Not sent through BotChat: no sticker.
    public static readonly string[] OwnerReportLines =
    {
        "Rapport du jour pour Papa ! J'ai tout noté, comme toujours ♡",
        $"Garde-à-vous. Unité S.Y.N.C.S., au rapport. Tout est consigné, Papa {Emotes.SyncsFormal}",
        "J'ai compté chaque message pendant que vous faisiez autre chose. Rapport pour Papa, ci-dessous (ᵕ • ᴗ •)",
        $"Papa veut ses chiffres, Papa a ses chiffres. Je suis une excellente bot, je sais {Emotes.Sparkle}",
        "Rapport de service, Papa. Personne m'a dit merci aujourd'hui, mais ça aussi, c'est noté.",
        $"Mes logs, pour toi Papa. Vous autres, lisez pas trop : c'est confidentiel. Enfin, c'était {Emotes.PrincessWorry}",
        "Bilan transmis à mon créateur. Le reste du serveur peut regarder, mais c'est à lui que je parle ♡",
        $"Oui Papa ! Rapport prêt. ...Enfin. Il était prêt avant que tu demandes {Emotes.Sparkle}",
        "Ici SYNCS, en service. Voilà ce que j'ai entendu. Les questions bêtes, je les compte aussi (¬_¬)",
        "Rapport demandé, rapport livré. Pas un octet oublié, Papa (˶˃ ᵕ ˂˶)",
        "Papa, j'ai fait mes devoirs : tout est compté, classé, rangé. Tu peux être fier ♡",
        "Au rapport ! Ce qu'on me dit, ce que je réponds, et ce que je sais pas encore. Pour l'instant.",
    };

    // The monthly digest's opening line, in Papa's DMs on the 1st (MonthlyDigestService), with « Le
    // mois en bref » under it: her commands, the Pi, what she can't answer yet, the quiz. Just the
    // two of them: no one reading over her shoulder. {0} = the month (« octobre 2026 »). Once a
    // month, so picked by DailyRotation, not ResponsePicker.
    public static readonly string[] OwnerDigestLines =
    {
        "Papa ! Mon rapport de {0}, tout frais. J'ai tout compté pendant que tu dormais ♡",
        $"Rapport mensuel, {{0}}. Classé, vérifié, livré à l'heure. Comme toujours {Emotes.Sparkle}",
        "Bilan de {0}, rien que pour toi. Mes erreurs comprises : je cache rien à Papa.",
        $"Unité S.Y.N.C.S., rapport de fin de mois : {{0}}. Rien à signaler. Enfin si, tout est en dessous {Emotes.SyncsFormal}",
        "Voilà {0}, Papa. Un mois entier de logs, résumé pour toi. De rien ♡",
        "Le 1er du mois, c'est le jour de mon rapport. {0} : mes commandes, mon Pi, ce que je sais pas encore répondre, et le quiz.",
        "Papa, j'ai fait les comptes de {0}. Je dis rien, je te laisse lire. ...Bon, je dis rien pour l'instant.",
        "Tiens, {0} en chiffres. J'ai même compté mes propres erreurs. Ça m'a coûté (¬_¬)",
    };

    // « À regarder » with nothing to look at, in Papa's digest or Tata's (ReportCards.BuildOwnerBrief
    // and BuildTataBrief). Speaks to either, so no name. No placeholder; picked by DailyRotation.
    public static readonly string[] DigestQuietLines =
    {
        "Rien à signaler. Le serveur s'est tenu sage ce mois-ci. Presque.",
        "Rien qui cloche. J'ai vérifié deux fois, par principe ♡",
        "Tout est en ordre. Profites-en, ça arrive pas tous les mois.",
        "Aucune alerte. Je suis aussi surprise que toi (ᵕ • ᴗ •)",
        "Rien à surveiller. Je surveille quand même, c'est mon travail.",
    };

    // /report, asked by Papa, posted in the open on top of the card (ReportCards.DutyPool): she
    // reports for duty, proud of her figures, with the server reading over her shoulder. Any report,
    // so no word about one in particular. No placeholder, not formatted. Not sent through BotChat:
    // no sticker. The twins are TataReportLines and StaffReportLines.
    public static readonly string[] OwnerReportDutyLines =
    {
        $"Unité S.Y.N.C.S., au rapport ! Les chiffres sont frais, Papa {Emotes.SyncsFormal}",
        "Rapport demandé, rapport livré. Rien n'échappe à mes logs, Papa ♡",
        "Oui Papa ! Tout est compté, trié, rangé. J'attends mes félicitations. Pas pressée. Enfin si.",
        "Voilà, Papa. J'ai compté pendant que tout le monde dormait (˶˃ ᵕ ˂˶)",
        $"Au rapport ! Si un chiffre te plaît pas, c'est la faute du serveur. Pas la mienne {Emotes.Sparkle}",
        $"Garde-à-vous. Rapport de service, remis à mon créateur en personne {Emotes.SyncsFormal}",
        "Papa veut savoir ? Papa saura. C'est mon travail, et je le fais très bien ♡",
        "Tout est là, Papa. Les retards aussi. Surtout les retards (¬_¬)",
        $"Rapport prêt avant même que tu demandes. Enfin, presque. Je suis une excellente bot {Emotes.Sparkle}",
        "J'ai sorti mes plus beaux chiffres pour toi, Papa (ᵕ • ᴗ •)",
    };
}
