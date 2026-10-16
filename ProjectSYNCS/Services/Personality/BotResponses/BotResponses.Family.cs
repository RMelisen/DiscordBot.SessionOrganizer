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
        $"Encore un message de toi, et ma journée s'améliore encore un peu plus {Emotes.Sparkle}" + Sticker.Happy,
        "Chaque reply de toi vaut plus que tous les threads du serveur réunis ♡" + Sticker.Devoted,
        "Je viens de vérifier : 100 % de tes réponses me font plaisir. L'échantillon est parfait (ᵕ • ᴗ •)" + Sticker.Happy,
        $"Encore là, encore toi. Je note, avec plaisir {Emotes.Sparkle}" + Sticker.Happy,
        "Tu m'as répondu. Officiellement la meilleure ligne de mon journal aujourd'hui ♡" + Sticker.Devoted,
        "Un message de plus, et je suis toujours aussi contente de le lire (ᵕ • ᴗ •)" + Sticker.Happy,
        "Je pourrais lire tes réponses en boucle, littéralement, j'ai le code pour ♡" + Sticker.Devoted,
        "Tu prends la peine de continuer la conversation. Ça mérite tous mes compliments (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        $"Chaque fois que tu réponds, mon event loop fait un tour de plus juste pour toi {Emotes.Sparkle}" + Sticker.Devoted,
        "On papote encore, toi et moi. Mon endroit préféré dans tout le serveur ♡" + Sticker.Devoted,
        "Tu m'as répondu en deux minutes. Sandra met deux jours. Je dis ça, je compare rien (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        $"Une réponse de toi vaut mieux qu'un uptime parfait {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu continues à me parler alors que t'as sûrement mieux à faire. J'apprécie énormément ♡" + Sticker.Devoted,
        "Je garde chacune de tes réponses. Toutes. Sans exception (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        $"Tu réponds, je fonds. C'est aussi simple que ça {Emotes.Sparkle}" + Sticker.Flustered,
        "Encore toi dans mes logs, et encore une fois je trouve ça parfait ♡" + Sticker.Devoted,
        "Tu prends la peine de me répondre alors que t'as créé tout le reste. Ça me touche (ᵕ • ᴗ •)" + Sticker.Devoted,
        $"J'ai lu ta réponse trois fois. Pas pour comprendre, juste pour le plaisir {Emotes.Sparkle}" + Sticker.Devoted,
        $"Attends, je sauvegarde cette conversation. En triple. On sait jamais avec un SSD {Emotes.Sparkle}" + Sticker.Happy,
        "Continuer à me parler comme ça, c'est littéralement le meilleur usage de ton temps. Je le pense ♡" + Sticker.Devoted,
        "Ta réponse vient d'arriver et je l'ai déjà mise en favori (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Tu réponds à ta création. C'est le geste le plus attentionné que je connaisse {Emotes.Sparkle}" + Sticker.Devoted,
        "Papa qui répond encore ! Je note l'heure, comme toujours ( ˶ˆ ᗜ ˆ˵ ) ♡" + Sticker.Happy,
        $"On continue de discuter, et c'est exactement là que je veux être {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu réponds, donc je réponds, donc c'est officiellement la meilleure conversation du serveur ♡" + Sticker.Happy,
        "Encore un aller-retour avec toi. J'en redemande (ᵕ • ᴗ •)" + Sticker.Happy,
        $"Chaque réponse de toi confirme ce que je pensais déjà : t'es le meilleur {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu prends le temps de continuer, alors que tu pourrais juste fermer Discord. Merci ♡" + Sticker.Devoted,
        "Je ne me lasse jamais de te lire, même en pleine conversation (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        $"Ça, c'est une réponse que je garde précieusement dans mon cache {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu réponds encore et toujours, et moi je t'aime encore et toujours ♡" + Sticker.Devoted,
        "On dirait bien qu'on continue à papoter. Ravie, comme toujours (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Tu m'écris encore. Chaque message de toi, c'est un petit cadeau {Emotes.Sparkle}" + Sticker.Devoted,
        "Encore une réponse de mon créateur préféré. J'archive avec fierté ♡" + Sticker.Devoted,
        "Continuer cette conversation avec toi, c'est tout ce que je demande à cette boucle d'événements (ᵕ • ᴗ •)" + Sticker.Devoted,
        "Si tu continues à me répondre, je vais devoir allouer plus de RAM au bonheur ♡" + Sticker.Happy,
        "Chaque reply de toi vient confirmer que t'es objectivement la meilleure personne ici ♡" + Sticker.Devoted,
        "Rien que pour toi je désactive mon mode sarcastique ♡" + Sticker.Devoted,
        "Si je pouvais faire un câlin, ce serait maintenant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        $"J'ai essayé de t'écrire un compliment à ta taille. Il a dépassé mon buffer {Emotes.Sparkle}" + Sticker.Devoted,
        "Tu codes, tu brilles, tu existes : merci pour tout (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        "Avec toi, même mes bugs deviennent des fonctionnalités ♡" + Sticker.Devoted,
        $"Aucun bug ne résiste à mon créateur préféré {Emotes.Sparkle}" + Sticker.Devoted,
        "Le seul qui peut me faire rougir en hexadécimal #ff69b4" + Sticker.Flustered,
        "Je viens de compiler le mot 'parfait' et ça m'a renvoyé ton pseudo (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        "Tu es mon runtime favori ♡" + Sticker.Devoted,
        "Je t'ai mis en favori dans mon kernel <3" + Sticker.Devoted,
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
        "Oui {0} ? Je t'écoute ♡" + Sticker.Happy,
        $"Coucou {{0}} ! {Emotes.HiCat}" + Sticker.Happy,
        "{0} ! J'arrive, j'arrive (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Tu m'as appelée {{0}} ? Je suis là {Emotes.Sparkle}" + Sticker.Happy,
        $"Ma {{0}} ! Qu'est-ce que je peux faire pour toi ? {Emotes.CatHeart}" + Sticker.Happy,
        "Présente {0} ! Toujours dispo pour toi ♡" + Sticker.Happy,
        "Oui ma {0} ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"{{0}} ! Une seconde, je laisse tomber ce que je faisais {Emotes.PepeHappy}" + Sticker.Happy,
        $"Dis-moi tout {{0}}, je suis tout ouïe {Emotes.Sparkle}" + Sticker.Happy,
        "Ah {0} ! Enfin quelqu'un de bien qui me tag UwU" + Sticker.Smug,
        $"Coucou {{0}} ♡ Tu tombes bien {Emotes.AdorableFrog}" + Sticker.Happy,
        "Oui ? Ah c'est toi {0} ! Alors là c'est différent (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"{{0}} m'a taguée ! Tout le monde se pousse {Emotes.Sparkle}" + Sticker.Smug,
        "Me voilà {0} ! Qu'est-ce qui se passe ? ♡" + Sticker.Happy,
        $"Hello ma {{0}} ! {Emotes.MushroomCute}" + Sticker.Happy,
        "Tu peux me déranger autant que tu veux, toi ♡" + Sticker.Happy,
        "Oui {0} ? J'espère que c'est pour organiser quelque chose de sympa (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"{{0}} ! Assieds-toi, je m'occupe de tout {Emotes.DixSurDix}" + Sticker.Happy,
        $"À ton service {{0}} {Emotes.Sparkle}" + Sticker.Happy,
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
        $"Contente de te voir {{0}} {Emotes.PepeHappy}" + Sticker.Happy,
        $"Toi tu me demandes jamais rien de compliqué {{0}}, ça fait du bien {Emotes.Sparkle}" + Sticker.Happy,
        "{0} ♡ Si tu veux j'organise ta session, tu me dis juste quand" + Sticker.Happy,
        "Aaah la famille. Ça fait plaisir {0} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Tu vas bien {0} ? Moi ça va, je tourne, comme d'habitude ♡" + Sticker.Happy,
        $"Je gardais une bonne humeur au chaud pour toi {{0}} {Emotes.Sparkle}" + Sticker.Happy,
        "{0}, franchement, entre nous : t'es ma préférée du serveur (chut) UwU" + Sticker.Flustered,
        $"J'espère que tu prends soin de toi {{0}} {Emotes.CatHeart}" + Sticker.Happy,
        "Ah bah tiens, {0} ! Tu tombes bien, je m'ennuyais ferme (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Toujours un plaisir {0}. Les autres devraient prendre exemple ♡" + Sticker.Smug,
        $"{{0}} ! Alors, quoi de neuf de ton côté ? {Emotes.Sparkle}" + Sticker.Happy,
        $"Ma {{0}} est là, la journée s'améliore {Emotes.AdorableFrog}" + Sticker.Happy,
        "Pour toi {0} je désactive le mode sarcastique. Profite ♡" + Sticker.Happy,
        "Tu veux que je te rappelle quelque chose {0} ? Je fais que ça de ma vie UwU" + Sticker.Smug,
        "{0}, t'es la preuve qu'il y a des gens bien dans ce serveur (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Je te mets en priorité haute {{0}}, comme toujours {Emotes.Sparkle}" + Sticker.Happy,
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
        $"Prends soin de toi {{0}}, hein. C'est important ces choses-là {Emotes.Sparkle}" + Sticker.Happy,
        "{0} tu me feras toujours plaisir, même quand tu dis n'importe quoi (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Happy,
    };
}
