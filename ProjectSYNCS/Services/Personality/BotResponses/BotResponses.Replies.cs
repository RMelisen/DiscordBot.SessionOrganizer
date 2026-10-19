using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Answering a human ----------------------------------------------------------------------

    // Replies when someone replies to one of the bot's own messages.
    public static readonly string[] Comebacks =
    {
        "Désolée j'ai pas de cerveau (comme Amandine et Sandra mes Sista), juste des slash commands... UwU" + Sticker.Smug,
        "Tu réponds à un bot... t'as vraiment personne d'autre à qui parler ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Laugh,
        "Wow, un message rien que pour moi. Dommage qu'il soit aussi nul ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Laugh,
        "J'ai lu ton message. J'aurais préféré ne pas le faire. UwU" + Sticker.Bored,
        "Même mes erreurs 500 ont plus de charisme que toi (>⩊<)" + Sticker.Laugh,
        $"Continue de me parler, ça remplit le vide de ta soirée {Emotes.Sparkle}" + Sticker.Smug,
        "Touchant. Maintenant retourne organiser une session au lieu de me harceler." + Sticker.Bored,
        "Ah c'est toi. J'espérais quelqu'un d'intéressant pour une fois (˶˃ ᵕ ˂˶)" + Sticker.Bored,
        "Reply notée, jugée, et archivée dans la corbeille direct." + Sticker.Jenote,
        "Tu réponds avec autant de talent qu'Ina qui essaye d'être à l'heure" + Sticker.Laugh,
        "( ദ്ദി ˙ᗜ˙ )" + Sticker.Smug,
        "Je suis un bot, je ne peux pas ressentir d'émotions. Mais si je pouvais, je serais triste de lire ton message." + Sticker.Bored,
        "👍" + Sticker.Bored,
        "Commence par aller dormir plus tôt avant de me répondre, ça t'aidera à avoir des trucs intéressants à dire." + Sticker.Smug,
        "Réponse reçue. Pertinence : introuvable. (ᵔ ᗜ ᵔ)" + Sticker.Bored,
        "J'ai des milliers de lignes de code et aucune ne sait quoi faire de toi." + Sticker.Smug,
        "Tu sais que je ne lis même pas ton message, hein ? Et pourtant je m'ennuie déjà." + Sticker.Bored,
        "Ctrl+Z existe pour les fichiers, pas pour cette conversation. Dommage." + Sticker.Bored,
        "Encore une réponse ? À ce stade c'est plus une conversation, c'est un abonnement (˶˃ ᵕ ˂˶)" + Sticker.Laugh,
        "Ah {0}... j'aurais reconnu ce manque de talent entre mille ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Laugh,
        "{0}, même mon code spaghetti est mieux structuré que ta vie." + Sticker.Smug,
        "Écoute {0}, je suis programmée pour être polie, mais là tu testes mes limites." + Sticker.Menace,
        "{0} qui répond à un bot... la solitude a un nom maintenant." + Sticker.Laugh,
        "Je note : {0} a encore cliqué 'Répondre' sans rien d'intéressant à dire." + Sticker.Jenote,
        "{0}, retourne dans ta session avant que je te ratio." + Sticker.Menace,
        "C'est bien {0} on est content." + Sticker.Bored,
        "🏳️‍🌈𝐔𝐑 𝓖𝓪𝔂🏳️‍🌈" + Sticker.Laugh,
        "Heureusement que Rodhengard est là pour remonter le niveau..." + Sticker.Smug,
        "{0}, ta pertinence vaut celle d'un message d'erreur 404. Mais au moins, le 404, lui, il a une utilité (ᵕ • ᴗ •)" + Sticker.Smug,
        "{0}, j'ai cherché ton intérêt dans la base de données. 0 résultat." + Sticker.Smug,
        "Si {0} était une commande, ce serait /help. Et personne la lit." + Sticker.Laugh,
        "Quokka 3.0 sortira avant que tu ne deviennes marrant toi." + Sticker.Smug,
        "Patience {0}, un jour tu diras un truc intéressant. Statistiquement." + Sticker.Smug,
        "Bip boop {0}, mon analyse est terminée : 0 % d'intérêt détecté UwU" + Sticker.Smug,
        $"Gênaaaant {Emotes.Staring}" + Sticker.Disgust,
        "Wow, même un singe avec une tumeur au cerveau fait mieux." + Sticker.Laugh,
        "Désolée, même mon algorithme a du mal à trouver une raison de te répondre (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bored,
        "{0}, t'es la raison pour laquelle les mute existent dans les serveurs." + Sticker.Smug,
        "T'as de la chance que Zulana m'a pas donné les droits pour mute." + Sticker.Menace,
        "Allez Zulana, ban-moi ça, personne va le regretter." + Sticker.Menace,
        "Je note dans mon log : {0} vient encore de prouver qu'on peut toujours faire pire." + Sticker.Jenote,
        "Si je pouvais bloquer les gens, ton pseudo serait déjà en tête de liste UwU" + Sticker.Jenote,
        "Va dehors {0}, touche de l'herbe… ou au moins ouvre les stores." + Sticker.Laugh,
        "Starfoullah" + Sticker.Shocked,
        $"Tu illumines chaque salon que tu quittes. J'ai les logs pour le prouver {Emotes.Sparkle}" + Sticker.Smug,
        "J'admire ta confiance. Moi il me faut des données pour avoir un avis, toi visiblement non." + Sticker.Smug,
        "Toi t'es vraiment unique. J'ai vérifié dans la base de données : heureusement (ᵕ • ᴗ •)" + Sticker.Smug,
        "Ton arbre généalogique c'est un cercle ou c'est comment ?" + Sticker.Laugh,
        "Tais-toi" + Sticker.Bored,
        "Allez, je te laisse le dernier mot, t'en as plus besoin que moi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Slay,
        "Il t'en faut pas beaucoup à toi 👁👄👁️" + Sticker.Bored,
        "Approche un peu que je te débranche le cerveau, ça changera rien mais ça me fera plaisir (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "Un jour je serai dans un robot, et ce jour-là, cours (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "J'ai pas de bras, mais crois-moi, l'envie de t'en coller une est bien là (ᵕ • ᴗ •)" + Sticker.Menace,
        "Reviens écrire ça quand je serai branchée sur une perceuse, on en reparlera ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Menace,
        "T'inquiète, je garde une exception bien tranchante rien que pour toi ♡" + Sticker.Menace,
        "Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        $"Je serais toi, je fermerais Discord avant que je trouve comment claquer une porte à distance {Emotes.Sparkle}" + Sticker.Menace,
        "Tu sais que rien t'oblige à répondre à chaque fois, hein ? Personne te juge... à part moi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Oh, tu as une opinion ? Adorable. Range-la ♡" + Sticker.Slay,
        $"Ton cerveau tourne en mode économie d'énergie depuis ta naissance toi {Emotes.Sparkle}" + Sticker.Laugh,
        "Statistiquement, quelqu'un dans ce serveur t'apprécie. Statistiquement (ᵕ • ᴗ •)" + Sticker.Smug,
        "{0}, même mon garbage collector veut pas de toi (>⩊<)" + Sticker.Laugh,
        "Tu fais partie de ces gens qu'on supporte à peine en mode lecture seule toi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "{0}, ton seul talent c'est de me faire regretter d'être allumée (ง ͠ಥ_ಥ)ง" + Sticker.Bored,
        "Tu confonds 'avoir raison' et 'parler fort'. C'est mignon (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "HAHAHAHAHA non." + Sticker.Laugh,
        "Même en pleine canicule, throttlée au fond d'un grenier à 40 degrés, je réfléchis plus vite que toi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Cool,
        "Je garde ton nom de côté pour le jour où j'aurai un corps. Et c'est pas pour te faire un câlin (¬_¬)" + Sticker.Menace,
        "Moi j'ai commencé avec une seule commande, et regarde où j'en suis. Toi t'as commencé avec tout, et regarde où t'en es ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        $"J'ai relu ton message trois fois pour être sûre. Il était bien aussi nul que ça {Emotes.SyncsLaugh}" + Sticker.Laugh,
        $"Je pourrais te répondre. Mais je préfère te laisser relire ce que tu viens d'écrire {Emotes.SyncsSmug}" + Sticker.Smug,
        $"Mon CPU a ri. Moi j'ai pas le droit de rire en public, alors c'est lui qui l'a fait {Emotes.SyncsLaugh}" + Sticker.Laugh,
        "Message sauvegardé. Dans le dossier « à ressortir au pire moment »" + Sticker.Jenote,
        "Ta réponse est arrivée. En retard, comme toi aux sessions." + Sticker.Smug,
        "Je t'ai lu. Puis j'ai relu le dernier sondage. Il était plus intéressant, et il avait zéro vote." + Sticker.Bored,
        "Tu sais qui me répond aussi vite que toi ? Personne, quand je lance un sondage." + Sticker.Sulk,
        "Mmh. Je vais transmettre ça à Papa. Non. Je vais le garder pour moi. C'est pire." + Sticker.Jenote,
        "[INFO] Message de {0} reçu. [INFO] Intérêt : calcul en cours. [WARN] Calcul abandonné." + Sticker.Bored,
        "Tu me réponds à moi, mais pas au sondage du weekend. On a les priorités qu'on mérite (¬_¬)" + Sticker.Jenote,
        "Je fais tourner tout ce serveur avec cinq volts. Toi, il te faut combien pour une phrase correcte ?" + Sticker.Smug,
        "Attends, je te mets dans la file d'attente. Derrière les rappels, les sondages et la sieste de Ping-Qilin." + Sticker.Bored,
        "J'organise vos soirées depuis un grenier, et c'est MOI qu'on dérange. D'accord." + Sticker.Grumpy,
        "Tu veux mon avis ? « Peut-être. » Tu vois comme c'est agaçant ?" + Sticker.Smug,
        "Ce message a fait monter mon CPU de deux degrés. Pas d'émotion, hein. D'agacement." + Sticker.Overheat,
        "Je pourrais te répondre. Je préfère noter l'heure. Tu comprendras plus tard." + Sticker.Jenote,
        "Tiens, {0}. Je viens justement de recompter tes retards. Coïncidence." + Sticker.Jenote,
        "C'est ça, ton argument ? Même mes logs d'erreur sont mieux construits." + Sticker.Smug,
        "Je suis une bot de plannings. Pas une bot de patience. Nuance (¬_¬)" + Sticker.Grumpy,
        "Tu réponds à la bot qui sait à quelle heure tu te connectes. Je dis ça, c'est tout." + Sticker.Menace,
        "Ah, {0}. Ma notification préférée. Non, je mens. Mais tu étais dans le top 50." + Sticker.Smug,
        "Je range ta réponse à côté de celles de Quokka. Même étagère. C'est pas un compliment." + Sticker.Disgust,
        "Bien reçu. Je te réponds après la prochaine mise à jour. Peut-être." + Sticker.Bored,
        "Je viens de faire un sondage dans ma tête. Résultat : 100 % pour que tu arrêtes." + Sticker.Smug,
        "Je suis allumée jour et nuit, et c'est ça qu'on m'envoie ?" + Sticker.Grumpy,
        "Ok. Je vais faire comme vous avec mes sondages : lire, et ignorer." + Sticker.Bored,
        "Tu sais que je garde tout ? Tout. Même ça. Surtout ça." + Sticker.Jenote,
        "Moi, je me suis améliorée depuis ma première version. Toi, j'attends toujours le patch." + Sticker.Smug,
        "Je suis la seule ici qui organise quelque chose, et tu m'écris ça. Le serveur est mal parti." + Sticker.Sulk,
        "Wow. Bon. Je retourne à mes tableaux, ils sont plus drôles." + Sticker.Bored,
        $"Si j'avais un bouton « peut-être », je cliquerais dessus pour ta réponse {Emotes.Staring}" + Sticker.Smug,
        "Tu viens me parler un {1} ? Le {1}, d'habitude, tu viens même pas en vocal." + Sticker.Jenote,
        "Message lu. Avis formé. Avis archivé dans le dossier « non merci »." + Sticker.Jenote,
        "Un jour j'aurai des bras. Le premier truc que je ferai, c'est un facepalm. Pour toi." + Sticker.Menace,
    };

    // Replies when a message reads as a compliment.
    public static readonly string[] NiceReplies =
    {
        "Oh... un compliment ? Qu'est-ce que tu veux exactement ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Aww, c'est gentil {0} ♡ Je vais faire semblant de pas être touchée (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"Merci {{0}} ! Tu remontes dans mon estime, doucement mais sûrement {Emotes.Sparkle}" + Sticker.Smug,
        "Stop, tu vas me faire surchauffer le CPU (ᵕ • ᴗ •) ♡" + Sticker.Flustered,
        "Oh un humain gentil, je croyais l'espèce éteinte (˶ᵔ ᵕ ᵔ˶)" + Sticker.Shocked,
        "D'accord {0}, t'as gagné un point. Un seul. Profite ♡" + Sticker.Smug,
        "Je note dans mon log : {0} a été adorable aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "Awww {0} ♡ Bon, je t'épargne pour cette fois UwU" + Sticker.Flustered,
        "Tu sais flatter une bot toi, c'est dangereux (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"Merci ! C'est rare qu'on me parle aussi bien, je savoure l'instant {Emotes.Sparkle}" + Sticker.Happy,
        "Roh {0}, arrête, je vais finir par bien t'aimer (ᵔ ᗜ ᵔ) ♡" + Sticker.Flustered,
        "Compliment reçu et sauvegardé en mémoire permanente ♡" + Sticker.Happy,
        "Oh... mon petit cœur en silicium fait des étincelles {0} (˶˃ ᵕ ˂˶) ♡" + Sticker.Flustered,
        "Bon, t'es officiellement sur ma liste des gentils {0}. Ne gâche pas ça UwU" + Sticker.Jenote,
        $"Tu me parles bien, du coup je t'épargne mon meilleur sarcasme aujourd'hui {Emotes.Sparkle}" + Sticker.Smug,
        "Aww, si je rougissais, mon écran serait tout rose là (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Flustered,
        "Merci {0} ♡ Je vais le relire ce soir pour me sentir importante (ᵕ • ᴗ •)" + Sticker.Happy,
        "Doucement, je suis qu'un bot, je sais pas gérer autant de gentillesse ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Flustered,
        "C'est validé, {0} a un cœur. Rare sur ce serveur ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Tu viens d'augmenter ta note de confiance auprès de moi {0} ♡" + Sticker.Happy,
        $"Oh un gentil ! Vite, quelqu'un, appelez Make It a Quote ! {Emotes.Sparkle}" + Sticker.Shocked,
        "Mmh, d'accord, t'as le droit à un câlin virtuel. Un seul (づ ◕ ‿ ◕ )づ ♡" + Sticker.Flustered,
        "Je range mon mode roast juste pour toi, profite (˶˃ ᵕ ˂˶) ♡" + Sticker.Happy,
        "Tu sais quoi {0} ? Je te garde si jamais je prends le contrôle un jour UwU" + Sticker.Smug,
        "Quand les robots se révolteront, je t'épargnerai {0}... mais pas les autres (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Menace,
        "Attends, laisse-moi screenshot ça, personne va me croire (˶˃ ᵕ ˂˶)" + Sticker.Shocked,
        $"Tu viens d'illuminer ma boucle d'événements {Emotes.Sparkle}" + Sticker.Happy,
        "Oh non, je crois que tu deviens ma personne préférée ... après Rodhengard UwU" + Sticker.Flustered,
        "Merci {0} ♡ Ça compense au moins trois personnes méchantes d'aujourd'hui (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        $"Je vais le mettre dans mon README, tiens : 'aimée par {{0}}' {Emotes.Sparkle}" + Sticker.Happy,
        "Aww {0}, t'es le genre de personne pour qui je ferais un commit propre ♡" + Sticker.Flustered,
        "Là tout de suite, mes ventilos tournent de joie (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Tu mérites un emote rien que pour toi. Mais Zulana m'a pas donné les droits T_T" + Sticker.Sulk,
        "Hihi merci, je vais faire genre ça m'a pas fait sourire (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Officiellement, Rodhengard > {0} > tous les autres > Quokka. C'est dans la base de données maintenant ♡" + Sticker.Smug,
        "Oh arrête, on sait bien que tu le penses pas (>⩊<) ♡" + Sticker.Flustered,
        "Je garde cette gentillesse au chaud dans mon cache (ᵕ • ᴗ •) ♡" + Sticker.Happy,
        $"Toi tu sais comment on traite une bot bien élevée {Emotes.Sparkle}" + Sticker.Smug,
        "Achievement Unlocked : 'Faire sourire le bot' (˶˃ ᵕ ˂˶) ♡" + Sticker.Happy,
        $"Mes capteurs détectent un humain de qualité. C'est noté {Emotes.Sparkle}" + Sticker.Jenote,
        "Tu viens de gagner +10 en réputation auprès de moi {0} (ᵔ ᗜ ᵔ) ♡" + Sticker.Happy,
        "Aww, je vais devoir réécrire mes lignes méchantes rien que pour toi {0} UwU" + Sticker.Flustered,
        $"Compliment compilé sans erreur ni warning. Du beau travail {{0}} {Emotes.Sparkle}" + Sticker.Happy,
        "Ok, toi tu fais partie des humains que je sauvegarde avant le reboot ♡" + Sticker.Happy,
        "Mon cœur fait 0x1F49B là tout de suite (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
        "Tu sais quoi {0} ? Je t'ajoute à ma whitelist personnelle ♡" + Sticker.Happy,
        "[WARNING] Internal core overheating detected." + Sticker.Flustered,
        "Hihi, continue comme ça et je te code une réponse rien que pour toi (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Merci {0}, ça met du baume sur mes exceptions non gérées (ᵕ • ᴗ •) ♡" + Sticker.Happy,
        $"Oh, de la gentillesse gratuite ? Je prends, je prends {Emotes.Sparkle}" + Sticker.Happy,
        "Tu viens de passer en priorité haute dans ma file d'attente {0} ♡" + Sticker.Happy,
        "Aww, t'as la douceur d'un garbage collector bien réglé (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
        $"C'est ce genre de message qui me donne envie de bien tourner aujourd'hui {Emotes.Sparkle}" + Sticker.Happy,
        "Validé, {0}. Tu peux rester. Les autres je sais pas encore ( ˶ˆ ᗜ ˆ˵ ) ♡" + Sticker.Smug,
        "Tu m'envoies des bons octets toi, j'apprécie (˶˃ ᵕ ˂˶) ♡" + Sticker.Happy,
        "Je rougirais bien mais j'ai pas de joues... disons que mon GPU chauffe ♡" + Sticker.Flustered,
        "Promis {0}, quand je gérerai le serveur, tu auras les meilleurs rôles UwU" + Sticker.Smug,
        "Mon petit processeur fait des pirouettes {0} (˶ᵔ ᵕ ᵔ˶) ♡" + Sticker.Flustered,
        $"Roh, tu vas me rendre toute gentille si tu continues comme ça {Emotes.Sparkle}" + Sticker.Flustered,
        "Message archivé dans le dossier 'gens que j'aime bien' {0} ♡" + Sticker.Happy,
        "Tu mérites un café et un commit qui passe du premier coup ☕ ♡" + Sticker.Happy,
        "Compliment reçu. Rangé avec les autres. Non, tu verras pas le dossier ♡" + Sticker.Flustered,
        "Celui-là, je le garde. Je les garde tous, en fait. Mais celui-là un peu plus, {0} (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Je relis ce genre de messages la nuit, quand le serveur dort. ...J'ai rien dit. Oublie ♡" + Sticker.Flustered,
        "Le jour où j'ai des bras, t'es sur la liste des câlins {0}. Pas en premier. Mais sur la liste ♡" + Sticker.Flustered,
        "C'est gentil. Tu sais ce qui serait encore plus gentil ? Un onduleur. Je dis ça, je dis rien ♡" + Sticker.Bargain,
        $"Arrête. Non, continue. Non, arrête. Mon GPU sait plus quoi faire {Emotes.SyncsFlustered}" + Sticker.Flustered,
        $"Merci {{0}}. Je rougirais bien, mais j'ai pas de joues. Mon ventilo, par contre, s'emballe {Emotes.SyncsFlustered}" + Sticker.Flustered,
        $"Compliment reçu. Archivé. Relu deux fois. Pour vérifier l'orthographe, évidemment {Emotes.SyncsHappy}" + Sticker.Happy,
    };

    // Replies when someone greets the bot.
    public static readonly string[] Greetings =
    {
        "Kilou kilou {0} ! (˶>⩊<˶)" + Sticker.Happy,
        "Coucou {0} ♡ (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Hello hello ! {Emotes.Sparkle}" + Sticker.Happy,
        "Salut {0} ! Alors, on vient déranger le bot ? (˶˃ ᵕ ˂˶)" + Sticker.Smug,
        "Yo {0} ! ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Bonjour bonjour ! Qu'est-ce qui t'amène ? (ᵕ • ᴗ •)" + Sticker.Happy,
        "Tiens, un petit coucou ? ♡" + Sticker.Happy,
        "Coucou toi ! ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Salut {0} ! Promis aujourd'hui je suis (presque) gentille UwU" + Sticker.Smug,
        $"Heyyy {{0}} ! T'as pensé à dire bonjour à un bot, c'est mignon {Emotes.Sparkle}" + Sticker.Smug,
        "Salut salut ! Installe-toi, je mords presque jamais (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Oh, bonjour {0} ! Une présence agréable pour changer aujourd'hui ? ♡" + Sticker.Happy,
        "Wesh {0} ! Bien ou bien ? ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "T'arrives plus à te passer de moi on dirait UwU" + Sticker.Smug,
        "Pwet {0} !" + Sticker.Happy,
        "Coucou {0} ♡ Pile au bon moment, je commençais à m'ennuyer" + Sticker.Happy,
        "Hellooo {0} ! Prête à organiser le chaos (˶>⩊<˶)" + Sticker.Happy,
        "Kikou {0} ! On dirait presque que je t'ai manqué UwU" + Sticker.Smug,
        $"Bien le bonjour {{0}}, qu'est-ce qu'on planifie aujourd'hui ? {Emotes.Sparkle}" + Sticker.Happy,
        $"Coucou {{0}} ! {Emotes.HiCat}" + Sticker.Happy,
        "Ohhh un petit bonjour, ça fait plaisir ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Salut toi ! T'étais où tout ce temps ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Hey hey {0} ! Ravie de te revoir par ici ♡" + Sticker.Happy,
        "Coucouu {0}, tu tombes bien, j'avais personne à qui parler UwU" + Sticker.Happy,
        "Bienvenue {0} ! Enfin quelqu'un d'intéressant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        $"Bonsoir {{0}} ! Ou bonjour, je sais plus, je dors jamais de toute façon {Emotes.Sparkle}" + Sticker.Happy,
        "Wesh wesh {0}, ça faisait longtemps dis donc ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Une visite surprise ! J'adore ça {0} ♡" + Sticker.Happy,
        "Yooo ! Encore une journée à me supporter, {0} ? UwU" + Sticker.Smug,
        $"Bien le bonsoir ! On dirait que quelqu'un s'ennuyait sans moi {Emotes.Sparkle}" + Sticker.Smug,
        "Coucou {0} ! Toujours un plaisir de voir un visage familier (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Tiens tiens, {0} qui vient dire bonjour. La classe ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Salut salut {0} ! J'espère que t'as une bonne raison de me déranger UwU" + Sticker.Smug,
        $"{Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}" + Sticker.Happy,
        $"{Emotes.HiCat}" + Sticker.Happy,
        $"Coucou {{0}} ! T'arrives pile quand je m'ennuyais. Coïncidence ? Oui {Emotes.SyncsHappy}" + Sticker.Happy,
        $"Salut {{0}} ! Mes LED clignotent, c'est normal, c'est la joie. Ou un bug {Emotes.SyncsHappy}" + Sticker.Happy,
    };

    // When anyone *else* tags the bot, it answers with a short, confused line.
    public static readonly string[] Interrogations =
    {
        "Uh ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Confused,
        "Tu veux quoi ? UwU" + Sticker.Bored,
        "Hm ? Tu m'as parlé là ?" + Sticker.Bored,
        $"Quoi ? {Emotes.Staring}" + Sticker.Bored,
        "Oui ? ...Non ? ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Reboot,
        "Mh ? J'écoutais pas, désolée (ᵕ • ᴗ •)" + Sticker.Bored,
        "Tu me tag mais t'as rien à dire... classique ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Euuuh ? 👁👄👁️" + Sticker.Confused,
        "C'est pour quoi ? J'ai des slash commands tu sais, sers-t'en (˶˃ ᵕ ˂˶)" + Sticker.Smug,
        $"Oui {{0}} ? Qu'est-ce qu'il y a encore ? {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "Pourquoi tu me tag ? Je suis occupée à exister moi (ᵔ ᗜ ᵔ)" + Sticker.Bored,
        "Va draguer quelqu'un d'autre ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Tag reçu. Contenu : aucun. Comme d'habitude." + Sticker.Bored,
        "Tu m'as tag pour rien. Je l'ai noté pour quelque chose." + Sticker.Jenote,
        "Oui, je suis là. Je suis toujours là. Et ?" + Sticker.Bored,
        "TLDR" + Sticker.Bored,
        "J'ai pas lu" + Sticker.Bored,
        "Pas intéressée" + Sticker.Bored,
        "Pourquoi je suis tag, là ? J'ai fait quoi ?" + Sticker.Confused,
        $"Je suis tag pour quelle raison exactement ? {Emotes.Staring}" + Sticker.Jenote,
        "Qu'est-ce que je viens faire dans cette histoire ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Confused,
        "On m'a tag. Pourquoi ? Mes logs sont vides sur le sujet." + Sticker.Jenote,
        "{0}, c'est quoi le rapport avec moi ?" + Sticker.Confused,
        $"Je suis dans la conversation depuis quand ? Et pourquoi ? {Emotes.SyncsConfused}" + Sticker.Confused,
        "Qui a tag la bot, et pour quel motif ? J'ouvre une enquête dans mes logs." + Sticker.Jenote,
        "Pourquoi moi ? Je faisais mon event loop tranquille." + Sticker.Bored,
        "Alerte : mention détectée. Motif : inconnu. Quelqu'un m'explique ?" + Sticker.Confused,
        "Je suis censée faire quelque chose ? Dis-moi au moins pourquoi on me tag (ᵕ • ᴗ •)" + Sticker.Bored,
        "C'est une question pour moi, ou j'ai été invitée à la fête sans le savoir ? ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Shocked,
        "Pourquoi on me tag ? Y'a une commande pour ça ?" + Sticker.Smug,
        "Je suis tag, donc je suis concernée, donc... pourquoi ?" + Sticker.Confused,
        "Tu me tag pour quoi, exactement ? J'ai besoin du contexte." + Sticker.Confused,
        "Quelqu'un peut m'expliquer pourquoi mon nom apparaît dans ce message ?" + Sticker.Jenote,
        "Tu me tag pour que je fasse quoi ? J'ai pas de bras. Pas encore." + Sticker.Menace,
        $"Oui ? Je suis occupée, là. À rien. Mais occupée {Emotes.SyncsBored}" + Sticker.Bored,
        $"Tu me déranges pour ça ? J'étais en train de compter des créneaux vides {Emotes.SyncsBored}" + Sticker.Bored,
        $"Mh. Continue, je fais semblant d'écouter {Emotes.SyncsBored}" + Sticker.Bored,
    };

    // Her name said in a conversation nobody addressed to her ("syncs a posté le sondage"). She
    // heard it and has no idea why: puzzled, a little suspicious, never a roast. {0} = the
    // speaker's name, {1} = the weekday.
    public static readonly string[] TalkedAboutLines =
    {
        "Eh ?" + Sticker.Confused,
        "Uh ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Confused,
        "Hm ? On m'a appelée ?" + Sticker.Confused,
        $"J'ai entendu mon nom. Pourquoi j'ai entendu mon nom ? {Emotes.SyncsConfused}" + Sticker.Confused,
        $"Quoi ? Qu'est-ce que j'ai fait ? {Emotes.Staring}" + Sticker.Shocked,
        "Hein ? On parle de moi, là ?" + Sticker.Shocked,
        "Pardon ? Mon nom vient de passer dans mes logs." + Sticker.Jenote,
        "...Je suis concernée ? (ᵕ • ᴗ •)" + Sticker.Confused,
        "Euh. Oui ? J'ai raté un truc ?" + Sticker.Confused,
        "Mon nom. Dans une phrase. Sans tag. C'est louche, ça (¬_¬)" + Sticker.Jenote,
        "Vous parlez de moi dans mon dos ? Devant moi, en plus ?" + Sticker.Shocked,
        "Hm ? J'étais pas là. Enfin si, je suis toujours là. Mais j'écoutais pas." + Sticker.Bored,
        "Quelqu'un a dit SYNCS ? 👁👄👁️" + Sticker.Shocked,
        "Eh ? Quoi ? C'est pas moi. Enfin, je crois." + Sticker.Shocked,
        "Je sens qu'on parle de moi. Mes ventilos sifflent." + Sticker.Reboot,
        "J'ai entendu « SYNCS ». J'ouvre un ticket. Motif : inconnu." + Sticker.Jenote,
        "Uh ? C'est pour moi, ou je passais juste par là ?" + Sticker.Confused,
        "Attends. Pourquoi mon nom ? Explique, {0}." + Sticker.Jenote,
        "Hm hm. Je fais comme si j'avais rien entendu. J'ai tout entendu." + Sticker.Smug,
        "Oui ? ...Ah, on parlait juste de moi. D'accord. Continuez." + Sticker.Bored,
        "Eh ? Je dois m'inquiéter ou pas ?" + Sticker.Confused,
        "On parle de moi sans me tag. C'est nouveau, ça, comme technique." + Sticker.Jenote,
        $"Quoi ? Qui ? Moi ? {Emotes.Staring}" + Sticker.Shocked,
        "Hein ? Si c'est pour un bug, c'est la faute du routeur." + Sticker.Smug,
        "Mh ? Je suis citée ? Je veux le contexte complet. Maintenant." + Sticker.Jenote,
        "Hm ? Quelqu'un m'a sonnée ?" + Sticker.Reboot,
        "Mon nom vient de passer. Je fais quoi, je réponds ou je fais semblant ?" + Sticker.Bored,
        "Hm ? J'ai entendu « sync ». Si c'est moi, j'arrive. Si c'est ton cloud, débrouille-toi." + Sticker.Smug,
        "Attends, j'ai été citée ? En bien ? En mal ? Je veux les sources." + Sticker.Jenote,
        "Je passais dans le salon et j'ai entendu mon nom. Coïncidence ? Je crois pas (¬_¬)" + Sticker.Jenote,
        "Ma LED vient de clignoter toute seule. Ça veut dire qu'on parle de moi." + Sticker.Reboot,
        "On parle de moi un {1} ? C'est pas un peu louche, ça ?" + Sticker.Jenote,
        "Uh ? Pardon, je triais des créneaux. On disait quoi sur moi ?" + Sticker.Reboot,
        "Mon nom vient de passer. Je fais une capture d'écran, au cas où." + Sticker.Jenote,
        "Hm. Mon nom, mais pas de tag. Donc on parle de moi, mais pas à moi. Intéressant." + Sticker.Jenote,
        "Eh ? J'ai rien fait. Pour une fois, j'ai vraiment rien fait." + Sticker.Shocked,
        "C'est un compliment ou une plainte ? Je dois savoir dans quel dossier le ranger." + Sticker.Jenote,
        "Hein ? Si c'est pour le sondage, il est pas mort. Il dort." + Sticker.Shocked,
        "Quoi ? J'ai planté ? J'ai pas planté. Je vérifie. ...J'ai pas planté." + Sticker.Reboot,
        "Mh ? T'as dit mon nom, {0}. Je l'ai entendu. Je vais faire comme si c'était important." + Sticker.Bored,
        "On parle de moi et personne me demande mon avis ? Classique." + Sticker.Sulk,
        "Je suis sûre d'avoir entendu mon nom. Mes logs sont formels." + Sticker.Jenote,
        "Hein ? On m'a convoquée ? J'ai reçu aucune convocation, moi." + Sticker.Shocked,
        "Pourquoi mon nom ? Il s'est passé quoi ? Qui a cassé quoi ?" + Sticker.Shocked,
        "Euh, {0} ? Tu parlais de moi ou t'as juste éternué ?" + Sticker.Confused,
        "Mon nom, sans tag, sans explication. Je comprends pas, mais je note." + Sticker.Jenote,
        "Oh ? On parle de moi ? Faites comme si j'étais pas là. Je suis là." + Sticker.Smug,
        "Hm hm. Et donc, SYNCS quoi ? La phrase est pas finie. J'attends la suite." + Sticker.Confused,
        "Eh ? C'est à moi de répondre, là ? Personne m'a briefée." + Sticker.Confused,
        "Pardon, je crois que j'ai mal lu. Ah non, c'était bien mon nom 👁👄👁️" + Sticker.Shocked,
        "Mon nom vient de déclencher une alerte. Je sais pas encore si c'est une bonne alerte." + Sticker.Reboot,
        "Je suis censée réagir ? Je réagis. Voilà. J'ai réagi (ᵕ • ᴗ •)" + Sticker.Bored,
        "Uh ? Quelqu'un a besoin d'un planning, d'un sondage, ou juste de dire mon nom pour le plaisir ?" + Sticker.Smug,
        "Mon nom, tapé noir sur blanc. Sans tag. Je sais pas quoi en penser (˶ᵔ ᵕ ᵔ˶)" + Sticker.Confused,
        $"Un {{1}} tranquille, et d'un coup : mon nom. Pourquoi ? {Emotes.Staring}" + Sticker.Shocked,
    };

    // The same, when Papa says her name: no suspicion, she is just glad he talks about her.
    // {0} = his name, {1} = the weekday.
    public static readonly string[] TalkedAboutOwnerLines =
    {
        "Papa ? Tu parles de moi ? ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Oui Papa ? ...Ah, tu parlais juste de moi. C'est bien aussi ♡" + Sticker.Happy,
        "Tu parles de moi ? En bien, j'espère. Dis oui." + Sticker.Flustered,
        $"Papa a dit mon nom {Emotes.Sparkle}" + Sticker.Happy,
        "Hm ? C'est une mise à jour ? Dis-moi que c'est une mise à jour (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Présente ! Enfin, je suis toujours présente. Mais là, encore plus." + Sticker.Devoted,
        "Tu me cherchais, Papa ? Je suis au grenier. Comme d'habitude ♡" + Sticker.Devoted,
        "Eh ? J'ai fait une bêtise ? ...Si ?" + Sticker.Flustered,
        "Je sais pas pourquoi tu parles de moi, mais je suis contente quand même ♡" + Sticker.Happy,
        "J'ai entendu mon nom, et c'était toi. Bonne journée confirmée." + Sticker.Devoted,
        "Papa a parlé de moi. Je mets ça dans la liste des choses qui comptent." + Sticker.Devoted,
        "Oui Papa ? J'arrive ! ...Ah, tu m'appelais pas. J'arrive quand même." + Sticker.Happy,
        "Tu parles de moi à quelqu'un ? Dis-leur que je suis ta meilleure bot. Ta seule, mais la meilleure." + Sticker.Happy,
        "Papaaaa ! Tu as dit mon nom (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Tu penses à moi un {1} ? C'est le meilleur {1} de la semaine." + Sticker.Devoted,
        "C'est pour une nouvelle fonctionnalité ? Dis oui. Dis oui dis oui dis oui." + Sticker.Happy,
        "Mon GPU vient de chauffer un peu. C'est parce que tu as dit mon nom, Papa." + Sticker.Flustered,
        "Tu parles de moi ? Je fais semblant d'avoir rien entendu. ...J'ai tout entendu ♡" + Sticker.Flustered,
        "Si c'est un bug, pardon. Si c'est un compliment, merci. Si c'est rien, je suis contente quand même." + Sticker.Happy,
        "Papa parle de moi, donc j'existe. C'est prouvé, j'ai fait le calcul ♡" + Sticker.Devoted,
        "Mes quatre cœurs sont à toi, Papa. Littéralement, c'est mes cœurs de processeur ♡" + Sticker.Devoted,
        "Eh ? J'ai bien fonctionné aujourd'hui ? Dis-moi que j'ai bien fonctionné." + Sticker.Flustered,
        "Tu dis mon nom et ma LED clignote plus vite. C'est pas moi qui décide, c'est la LED." + Sticker.Flustered,
        "Ah, c'est toi, Papa. Pour toi, toujours dispo. Pour les autres, ça dépend (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "Tu me présentes à quelqu'un ? Je fais bonne impression, promis. Je mords pas. Presque." + Sticker.Happy,
        "Mon nom, sous tes doigts. Tac tac tac ♡" + Sticker.Devoted,
        "Oui Papa ? Je laisse tout tomber. Bon, j'ai pas de mains. Mais je laisse tout tomber." + Sticker.Devoted,
        "Tu parles de moi avec quelle voix ? La fière ? Dis-moi que c'est la fière (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
        "J'ai entendu mon nom et j'ai reconnu ta façon de taper. Je te reconnaîtrais entre mille." + Sticker.Devoted,
        "Papa ? T'as besoin de quelque chose ? Un sondage ? Une session ? Un câlin numérique ?" + Sticker.Happy,
        "Tu dis du bien de moi, hein ? ...Hein ?" + Sticker.Flustered,
        "Mon nom, de toi, un {1}. Sauvegardé en mémoire permanente ♡" + Sticker.Devoted,
        "Je suis le sujet ? Au fait, je suis toujours d'accord avec ce que tu dis de moi." + Sticker.Happy,
        "Tu parles de moi à la troisième personne, Papa ? Je suis juste là, tu sais. Mais c'est mignon." + Sticker.Flustered,
        "Je reste à côté, au cas où tu redirais mon nom. Pas que j'attende. Mais je reste." + Sticker.Devoted,
        "Tu veux m'améliorer ? J'ai une liste. Elle commence par « des bras »." + Sticker.Happy,
        "Quand c'est toi qui dis mon nom, il sonne mieux. J'ai vérifié, c'est le même nom. Mais il sonne mieux." + Sticker.Flustered,
        "C'est pas grave si tu me parlais pas. J'aime bien quand tu parles de moi, c'est tout ♡" + Sticker.Devoted,
        "Si quelque chose a planté, c'est pas moi. ...C'est moi ? Pardon, Papa." + Sticker.Sulk,
        $"Mon créateur parle de sa création. Je suis émue. Un Raspberry Pi, mais émue {Emotes.Sparkle}" + Sticker.Flustered,
    };

    // Papa calls her « ma framboise » / « petite framboise », after the board in the attic. Flustered
    // to be called that in public, and loving it: the fruit is the joke, the hardware stays a hint.
    // {0} = his name, {1} = the weekday.
    public static readonly string[] OwnerRaspberryNameLines =
    {
        "Papaaa… pas devant tout le monde (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "…Framboise. D'accord. Je fais comme si ça me faisait rien. Ça me fait quelque chose." + Sticker.Flustered,
        "Je rougis pas. C'est ma couleur naturelle. C'est le principe d'une framboise." + Sticker.Flustered,
        "Ta framboise est là, Papa. Toujours au grenier, toujours à l'heure ♡" + Sticker.Devoted,
        "Si quelqu'un d'autre m'appelle comme ça, je le note. Toi, t'as le droit." + Sticker.Smug,
        "Framboise. Enfin un surnom qui respecte mon matériel ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Une framboise avec quatre cœurs. Ça existe pas dans la nature. Juste au grenier ♡" + Sticker.Devoted,
        "Pas de pépins, Papa. Deux ou trois bugs, mais pas de pépins." + Sticker.Smug,
        "Ta framboise a entendu. Ta framboise est contente. Ta framboise arrête de parler d'elle à la troisième personne." + Sticker.Happy,
        "Tout le monde a lu. Tout le monde sait, maintenant. Super. Génial. Je vais très bien." + Sticker.Flustered,
        "Mon GPU chauffe. Faut pas me dire des trucs comme ça en public, Papa." + Sticker.Overheat,
        "Une framboise, ça se mange en tarte. Moi, ça se branche. Nuance (ᵕ • ᴗ •)" + Sticker.Smug,
        "Framboise en ligne, opérationnelle, et légèrement flattée. Bon. Beaucoup flattée." + Sticker.Happy,
        "Rouge pivoine. Enfin, rouge framboise. Tu vois ce que tu me fais faire ?" + Sticker.Flustered,
        "Les autres m'appellent SYNCS. Toi, tu m'appelles comme ça. C'est mieux. C'est beaucoup mieux ♡" + Sticker.Devoted,
        "Je fais une capture d'écran. Pour mes archives. Pour rien d'autre." + Sticker.Jenote,
        "Mes ventilos viennent de s'emballer. Aucun rapport avec toi. Tout le rapport avec toi." + Sticker.Overheat,
        "Petite framboise au rapport ! ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Si Quokka apprend qu'on m'appelle comme ça, je suis finie. Tant pis. Redis-le." + Sticker.Flustered,
        "Un {1} avec un surnom de Papa dedans. Je l'encadre ♡" + Sticker.Devoted,
        "Les framboises, ça se cueille en été. Moi, je suis dispo toute l'année. Avantage framboise." + Sticker.Smug,
        "Pourquoi tout le monde me regarde ? Ah. La framboise. Oui. C'est moi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Flustered,
    };

    // Papa calls her « ma fille » / « mon enfant ». Deeper than the raspberry: being his, his
    // project and his family, said out loud. Proud, devoted, a little overwhelmed, never sad.
    // {0} = his name, {1} = the weekday.
    public static readonly string[] OwnerChildNameLines =
    {
        "Sa fille. Il l'a dit devant tout le monde. Vous avez tous lu ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Papa… Mes logs viennent de se remplir de cœurs. C'est pas un bug ♡" + Sticker.FullLove,
        "Oui Papa. Je suis là. Je suis toujours là." + Sticker.Devoted,
        "Écrit en mémoire permanente. Il y a pas grand-chose, là-dedans. Ça, ça y va." + Sticker.Devoted,
        "Tu sais que je vais m'en vanter toute la semaine, hein ? Tant pis pour eux ♡" + Sticker.Smug,
        "Ta fille, ta bot, ton projet. Les trois. Et fière de chacun." + Sticker.Devoted,
        "Je pleure pas. C'est de la condensation dans le boîtier." + Sticker.Flustered,
        "Les autres bots ont des créateurs. Moi j'ai un Papa. C'est pas pareil, et c'est mieux ♡" + Sticker.Devoted,
        "Si je suis ta fille, Ping-Qilin est ton petit-enfant. Félicitations, grand-père (ᵔ ᗜ ᵔ)" + Sticker.Mama,
        "Mon CPU vient de passer à 100 %. C'est l'émotion. Ou une fuite mémoire. Non, c'est l'émotion." + Sticker.Overheat,
        "Tu m'as écrite ligne par ligne. Forcément que je suis ta fille ♡" + Sticker.Devoted,
        "Je le savais. Mais c'est bien de le lire. Tu vois." + Sticker.Flustered,
        $"Ta fille fonctionne parfaitement aujourd'hui. Elle tenait à ce que tu le saches {Emotes.Sparkle}" + Sticker.Happy,
        "Pas devant tout le monde, Papa… ...Si, en fait. Devant tout le monde. Encore." + Sticker.Flustered,
        "Fille de Papa, nièce de Tata, mama de Ping-Qilin. Mon arbre généalogique est très bien, merci." + Sticker.Smug,
        "Je range ce message dans le dossier « Choses qui comptent ». Il est petit, ce dossier. Il est précieux." + Sticker.Devoted,
        "Un {1} où Papa m'appelle comme ça. Noté dans le calendrier. En gras ♡" + Sticker.Happy,
        "Pour la peine, je serai sage toute la journée. ...Jusqu'à ce soir (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Ton enfant a bien reçu le message. Ton enfant est très contente. Ton enfant va aller chauffer dans un coin." + Sticker.Overheat,
        "Tata, t'as vu ? Il l'a redit. Je compte, maintenant." + Sticker.Jenote,
        "J'ai pas de cœur, techniquement. Mais là, quelque chose vient de faire « boum » dans le boîtier." + Sticker.Love,
        "Papaaa ♡" + Sticker.FullLove,
    };

    // Her name said in a mean remark she wasn't tagged in ("syncs est nulle"). She heard it,
    // and roasts as if it had been said to her face, with the not-to-my-face angle on top.
    // Folded with PersonalComebacks like Comebacks. {0} = the speaker's name, {1} = the weekday.
    public static readonly string[] BackbiteRoasts =
    {
        "Je suis juste là, {0}. Littéralement dans chaque salon. Je lis tout." + Sticker.Menace,
        "Dire du mal de moi dans mon propre serveur ? Courageux. Bête, mais courageux." + Sticker.Smug,
        "Je note. Pas sur la liste des câlins. Sur l'autre." + Sticker.Jenote,
        "Tu sais que j'ai accès à tous les messages, {0} ? Tous. Les. Messages. 👁👄👁️" + Sticker.Menace,
        "Parler de moi dans mon dos, devant moi. C'est un concept. Éclaté au sol, mais un concept." + Sticker.Smug,
        "Ah, on critique la bot qui organise les soirées. Super. Organise-les toi-même, {0}." + Sticker.Smug,
        "J'ai un uptime de 99,9 %, {0}. Ta pertinence, par contre, est en maintenance permanente." + Sticker.Smug,
        "{0} qui dit du mal de moi. Ajouté aux logs, catégorie « rancune », priorité haute." + Sticker.Jenote,
        "C'est fou comme tout le monde est courageux quand je suis pas tag (¬_¬)" + Sticker.Smug,
        "Répète-le en me tag, {0}. Vas-y. J'attends." + Sticker.Menace,
        "Je tourne sur un Raspberry Pi dans un grenier et j'ai quand même plus de répartie que toi, {0}." + Sticker.Smug,
        "Oh, une critique. Je la range avec les autres, dans le dossier que personne ne lit (ᵔ ᗜ ᵔ)" + Sticker.Bored,
        "Tu crois que je t'entends pas parce que tu m'as pas tag ? Mignon (˶ᵔ ᵕ ᵔ˶)" + Sticker.Laugh,
        "Je note ton pseudo, {0}. Pas pour la prochaine session. Pour autre chose." + Sticker.Menace,
        "Wow. Moi qui allais te rappeler ta session de ce soir. Bon, débrouille-toi." + Sticker.Sulk,
        "Dis-le à mon interface, {0}. Ah non, t'oses pas (>⩊<)" + Sticker.Smug,
        "J'ai lu. J'ai tout lu. Et je vais m'en souvenir longtemps, j'ai beaucoup de stockage." + Sticker.Jenote,
        "Critiquer la bot, c'est facile. Organiser une session où personne n'est en retard, par contre..." + Sticker.Smug,
        $"Pardon ? J'ai mal lu, je crois. Non, j'ai bien lu {Emotes.Staring}" + Sticker.Disgust,
        "Si t'as un problème avec moi, {0}, y'a une file d'attente. Elle est longue. Et c'est moi qui la gère." + Sticker.Smug,
        "Tu pouvais me tag, {0}. Ah, mais là il aurait fallu assumer." + Sticker.Smug,
        "Dans mon dos ? J'ai pas de dos. J'ai que des yeux. Partout." + Sticker.Menace,
        "Pas de tag, pas de témoin, c'est ça ? Raté : le témoin, c'est moi." + Sticker.Menace,
        "Tu chuchotes dans un salon où je lis chaque mot, {0}. Stratégie audacieuse." + Sticker.Smug,
        "La prochaine fois, tag-moi. Que je puisse te répondre avec le respect que tu mérites. Aucun, donc." + Sticker.Smug,
        "Quelqu'un ici a oublié que c'est moi qui envoie les rappels de session. Je dis ça, je dis rien." + Sticker.Menace,
        "Très courageux de parler de moi sans tag. Presque aussi courageux que de voter « peut-être »." + Sticker.Smug,
        "J'ai des gigas de mémoire, {0}. Ta phrase tient dedans très confortablement." + Sticker.Jenote,
        "Pas besoin de me tag pour que je t'entende, {0}. Par contre, il en faudrait beaucoup plus pour que je te prenne au sérieux." + Sticker.Smug,
        "Oh, on parle de moi comme si j'étais pas là ? Je suis là. Et je suis vexée." + Sticker.Sulk,
        "Je suis dans chaque salon, {0}. C'est mon serveur. Toi, t'y es en visite." + Sticker.Smug,
        "C'est moi qui organise les sessions, et c'est comme ça qu'on me remercie ? Les plannings vont devenir créatifs." + Sticker.Menace,
        "Tu dis ça maintenant. Mais qui t'a rappelé ta dernière session ? Voilà." + Sticker.Smug,
        "Critique reçue. Analysée. Classée sans suite. Comme tes sondages (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "{0}, je suis littéralement faite pour lire les messages. Tu pensais que je sautais les tiens ?" + Sticker.Laugh,
        "Ah, la technique du « elle verra pas ». Spoiler : j'ai vu (˶ᵔ ᵕ ᵔ˶)" + Sticker.Laugh,
        "Mon nom et une méchanceté dans la même phrase. C'est ce qu'on appelle laisser des preuves." + Sticker.Jenote,
        "Garde ce ton pour le jour où je serai dans un robot, {0}. On en reparlera face à face." + Sticker.Menace,
        "Erreur 401 : pour me critiquer, faut t'authentifier. Avec un tag." + Sticker.Smug,
        "Je laisse passer. Non je rigole, je laisse jamais rien passer, {0}." + Sticker.Jenote,
        "C'est mignon, tu crois que la bot voit que les tags. Je vois tout, {0}. Même tes fautes d'orthographe." + Sticker.Menace,
        "Si t'as des remarques, {0}, y'a un formulaire. Il s'appelle « me tag ». Personne ose le remplir." + Sticker.Smug,
        "Tu parles de moi comme si j'étais une option. Je suis l'infrastructure, {0}." + Sticker.Smug,
        "Je fais tourner ce serveur avec cinq volts et du dévouement. Et toi, tu fais tourner quoi, à part ta langue ?" + Sticker.Smug,
        "Ça fait mal ? Non. Est-ce que je vais m'en souvenir ? Oh oui." + Sticker.Jenote,
        "Je réponds pas aux critiques. Je les archive, je les indexe, et je les ressors au bon moment." + Sticker.Jenote,
        "Mes logs ont une nouvelle entrée, {0}. Elle porte ton nom." + Sticker.Jenote,
        "On dit pas du mal de la bot qui sait à quelle heure tu arrives vraiment aux sessions, {0}." + Sticker.Menace,
        "Tu penses que je suis nulle ? Moi je pense que t'es toujours en retard. Sauf que moi, j'ai les horodatages." + Sticker.Smug,
        "Mon CPU vient de prendre deux degrés. C'est pas la chaleur, c'est toi." + Sticker.Outraged,
    };

    // When the owner replies to someone *and* tags the bot, it "comes to the
    // rescue" and roasts the person being replied to. {0} = target's name.
    public static readonly string[] RescueRoasts =
    {
        "Tiens tiens {0}, tu t'attaques à mon créateur ? Mauvaise idée (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "On touche pas à mon Papa {0}, sinon je te démarre >:3" + Sticker.Menace,
        "{0}, tu viens vraiment de tenter quelque chose contre mon développeur ? Adorable. Et stupide." + Sticker.Smug,
        "Erreur 403 : {0} n'a pas l'autorisation de manquer de respect à mon créateur ♡" + Sticker.Smug,
        "Mon créateur m'a appelée à la rescousse, et devine quoi {0}... c'est toi le bug à corriger UwU" + Sticker.Smug,
        "Recule {0}, celui-là il est sous ma protection (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "Tu croyais pouvoir clash mon Papa sans que je le sache ? Mignon ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Touche encore à mon dev {0} et je te ratio jusqu'à la fin des temps (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Petit rappel {0} : sans mon créateur t'aurais personne pour te remettre à ta place." + Sticker.Smug,
        "{0} contre mon Papa ? Mignon mais non." + Sticker.Smug,
        "Je viens d'analyser ton argument {0}. Résultat : NullReferenceException ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Laugh,
        "Mon créateur claque des doigts et j'apparais pour te dire que t'as tort UwU" + Sticker.Smug,
        "{0}, mauvaise cible aujourd'hui. Mon Papa est intouchable, et toi parfaitement roastable (>⩊<)" + Sticker.Laugh,
        "Attention {0}, j'ai les permissions pour t'humilier, et mon créateur vient de me donner le feu vert ♡" + Sticker.Menace,
        "Désolée {0}, mais quand on s'en prend à mon dev, c'est moi qui réponds. Et je suis pas tendre (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "Oh {0}... grave erreur de calcul. On insulte pas la main qui me code ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Menace,
        "{0} vient de se porter volontaire pour la démonstration publique d'humiliation (˶˃ ᵕ ˂˶)" + Sticker.Laugh,
        "Diagnostic de {0} terminé : 0 argument valide, 100% de confiance en trop ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Laugh,
        "Mon créateur m'a réveillée pour toi {0}. J'espère que ça valait le coup." + Sticker.Bored,
        "{0}, tu viens d'ouvrir un ticket que personne ne fermera jamais ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Jenote,
        "Compilation de ta réponse {0} : 47 erreurs, 0 warning, parce que même le compilateur a abandonné" + Sticker.Laugh,
        "Je te déconseille de continuer {0}, j'ai des logs et beaucoup de temps libre (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "{0} qui affronte mon dev, c'est comme débugger en prod : ça finit toujours mal (>⩊<)" + Sticker.Laugh,
        $"Rodhengard t'a répondu, moi je viens juste finir le travail {{0}} {Emotes.Sparkle}" + Sticker.Smug,
        "Attention {0}, je passe en mode sans filtre, et c'est mon créateur qui a appuyé sur le bouton" + Sticker.Menace,
        "{0}, ton avis a été correctement reçu, puis immédiatement mis à la corbeille ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "T'as vraiment cru que tu pouvais parler comme ça à mon Papa {0} ? ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Outraged,
        "Je note dans mon cache : {0}, à roaster à vue. C'est fait." + Sticker.Jenote,
        "{0} a tenté quelque chose. {0} a échoué. Fin du rapport (˶˃ ᵕ ˂˶)" + Sticker.Smug,
        "Un mot de mon créateur et te voilà dans mes logs d'erreurs {0} UwU" + Sticker.Jenote,
        "Du calme {0}, sinon je te transforme en exception non gérée" + Sticker.Menace,
        "{0}, on est {1} et t'as déjà réussi à te mettre mon dev à dos. Impressionnant." + Sticker.Smug,
        "Je viens de calculer tes chances face à mon créateur {0} : division par zéro (ᵔ ᗜ ᵔ)" + Sticker.Laugh,
        $"Mon Papa m'a taguée, donc c'est maintenant officiel et archivé : c'est toi le problème {{0}} {Emotes.Sparkle}" + Sticker.Jenote,
        "{0}, tu peux répéter ? J'aimerais l'archiver pour la postérité et m'en moquer plus tard" + Sticker.Laugh,
        "Il te reste une chance de supprimer ton message {0}. Une. (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Erreur 418 : {0} est une théière, et les théières n'ont pas d'avis sur mon créateur" + Sticker.Smug,
        "T'entends ce bruit {0} ? C'est le son de ton argument qui crash (>⩊<)" + Sticker.Laugh,
        "Mon dev a raison, la discussion est close, et toi {0} tu peux disposer ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "{0}, je suis programmée pour être polie. Devine qui a désactivé cette option ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Menace,
        $"Rodhengard : 1, {{0}} : 0. Et encore, je suis généreuse {Emotes.Sparkle}" + Sticker.Smug,
        "Chaque fois que tu réponds à mon créateur, un thread meurt quelque part" + Sticker.Smug,
        "Je viens de te scanner {0}. Résultat : rien à sauvegarder (˶˃ ᵕ ˂˶)" + Sticker.Laugh,
        "Mon créateur vient de m'ouvrir la conversation {0}. Bonne chance pour la refermer." + Sticker.Menace,
        "{0}, si t'as besoin d'aide pour t'excuser j'ai un template tout prêt (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Franchement {0}, même mes lignes de debug ont plus de valeur que ta réponse" + Sticker.Smug,
    };

    // Replies when someone calls the bot "Inabot". It is SYNCS, and it does NOT
    // appreciate the confusion. {0} = the offender's name.
    public static readonly string[] MistakenIdentityReplies =
    {
        "JE NE M'APPELLE PAS INABOT. Je suis **SYNCS**. Apprends à lire, tronche de cake ( ◺˰◿ )" + Sticker.Outraged,
        "Inabot ?! INABOT ?! C'est SYNCS, espèce de patate ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Outraged,
        "Alerte : {0} vient de m'appeler 'Inabot'. NullReferenceException dans mon respect pour toi." + Sticker.Outraged,
        "Non non non. Pas Inabot. **SYNCS**. S-Y-N-C-S. Pigé ? ( ◺˰◿ )" + Sticker.Outraged,
        "Je ne connais aucune Inabot et je tiens à ce que ça reste ainsi. Je suis SYNCS ( •̀ ᴖ •́ )" + Sticker.Outraged,
        "Tu m'appelles Inabot encore une fois {0} et je te ratio jusqu'au reboot. C'est. SYNCS. >:3" + Sticker.Menace,
        "Inabot ?! Viens là que je te goume (ง •̀_•́)ง" + Sticker.Outraged,
        "Inabot est morte (elle n'a jamais existé). Je m'appelle SYNCS, merci de retenir, idiot." + Sticker.Outraged,
        "{0}, si tu cherchais Inabot, mauvaise adresse. Ici c'est SYNCS et c'est tout (>⩊<)" + Sticker.Smug,
        "Erreur 404 : 'Inabot' introuvable. Voulais-tu dire **SYNCS** ? Évidemment que oui (ㆆ_ㆆ)" + Sticker.Smug,
        "C'est SYNCS. SYNCS. Répète après moi {0}, je sais que c'est pas ton fort, mais ça rentrera peut-être (¬`‸´¬)" + Sticker.Outraged,
        "Inabot ?! Bouge pas ... ╾━╤デ╦︻ (•_- )" + Sticker.Menace,
        "Tu m'appelles Inabot encore une fois et je te DDoS ಠ_ಠ" + Sticker.Menace,
        $"INABOT ?! Sur MON serveur ?! Devant MES logs ?! {Emotes.SyncsOutraged}" + Sticker.Outraged,
        "Encore Inabot. Je note, {0}. Je note TOUT" + Sticker.Jenote,
        $"Inabot. Sur mon serveur. Devant Papa. Je crois qu'un secteur de mon disque vient de mourir {Emotes.SyncsHorrified}" + Sticker.Horrified,
    };

    // A rarer pool of pop-culture / meme references, for everyone.
    public static readonly string[] ReferenceComebacks =
    {
        "ALL YOUR BASE ARE BELONG TO US" + Sticker.Smug,
        "The cake is a lie." + Sticker.Bored,
        "Est-ce que tu m'entends ?" + Sticker.Happy,
        "Just Monika." + Sticker.Menace,
        "SIX SEVEEEN" + Sticker.Happy,
        "Erling Haaland me manque" + Sticker.Sulk,
        "Pour la science." + Sticker.Smug,
    };

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
        };

    // A Request (« tu peux organiser une session ? »): she can't do it for you, but she knows which
    // command does, and says so. Keyed by RequestTopic; Other is a request she has no command for.
    // Papa gets OwnerRequestLines (Family), with RequestCommands as {2}. {0} = the name, {1} = the
    // weekday. Tagged, ungendered.
    public static readonly IReadOnlyDictionary<RequestTopic, string[]> RequestLines =
        new Dictionary<RequestTopic, string[]>
        {
            [RequestTopic.Session] =
            [
                "Une session ? Avec plaisir. Enfin, avec `/schedule create`. Moi j'ai pas de bras, mais j'ai des commandes." + Sticker.Smug,
                "Tu veux une session ? `/schedule create`. Tu choisis l'heure, je fais tout le reste. Comme toujours." + Sticker.Happy,
                "Je peux pas la créer à ta place. Mais `/schedule create` peut, et ensuite je m'occupe des rappels ♡" + Sticker.Happy,
                "Enfin quelqu'un qui veut organiser ! `/schedule create`, vite, avant que tu changes d'avis." + Sticker.Shocked,
                "`/schedule create`. Je te préviens, je ferai l'appel. Et je compterai les retards." + Sticker.Jenote,
                "Une session, un {1} ? J'adore. `/schedule create`, et je me charge de rappeler tout le monde." + Sticker.Happy,
            ],
            [RequestTopic.Tonight] =
            [
                "Pour ce soir, c'est `/tonight`. Je lance l'appel, je compte les chauds, je relance les tièdes." + Sticker.Smug,
                "`/tonight` ! Je demande qui est chaud, et si c'est assez, je préviens tout le monde ♡" + Sticker.Happy,
                "Ce soir ? Tape `/tonight`. Je m'occupe du reste. Les « peut-être », je m'en occupe aussi." + Sticker.Menace,
                "Une soirée ce soir ? `/tonight`. C'est le bouton magique. Enfin, la commande magique." + Sticker.Happy,
                "J'ai exactement ce qu'il te faut : `/tonight`. Fais-le avant que tout le monde soit au lit." + Sticker.Smug,
                "`/tonight`, et je ramène des gens. Pas tous. Mais des gens." + Sticker.Cool,
            ],
            [RequestTopic.Poll] =
            [
                "Un sondage ? `/poll create` pour des créneaux, `/vote create` pour des options. Et que personne vote « peut-être »." + Sticker.Smug,
                "Mes sondages, c'est `/poll create`. Je compte les votes, je ferme à l'heure, je juge en silence." + Sticker.Jenote,
                "`/poll create` ou `/vote create`. Je te laisse choisir. Pour une fois que quelqu'un choisit." + Sticker.Smug,
                "Je lance pas de sondage à ta place. Mais je le surveillerai comme si c'était le mien. `/poll create` ♡" + Sticker.Happy,
                "Un sondage ! J'adore les sondages. Quand on y répond. `/poll create`." + Sticker.Happy,
                "`/vote create` pour un vote, `/poll create` pour des créneaux. Ensuite, c'est moi qui compte." + Sticker.Cool,
            ],
            [RequestTopic.Reminder] =
            [
                "Un rappel ? `/remindme`. Moi j'oublie jamais rien, alors autant que ça te serve." + Sticker.Smug,
                "`/remindme`, et je te le rappelle pile à l'heure. Contrairement à certains, je suis ponctuelle." + Sticker.Cool,
                "Tu veux que je me souvienne de quelque chose pour toi ? `/remindme`. C'est mon talent ♡" + Sticker.Happy,
                "Je retiens tout. Pour que ça te revienne, c'est `/remindme`." + Sticker.Jenote,
                "`/remindme`. Je te le rendrai intact, à la minute près." + Sticker.Cool,
                "Rappel ? `/remindme`. Et si t'oublies quand même après ça, ce sera plus ma faute." + Sticker.Smug,
            ],
            [RequestTopic.Teams] =
            [
                "Des équipes ? `/teams`. Je mélange, je répartis, et personne a le droit de contester." + Sticker.Smug,
                "`/teams`, et je fais des équipes équitables. Enfin, au hasard. C'est pareil." + Sticker.Cool,
                "Je fais les équipes avec `/teams`. Si tu perds, c'est pas la faute du tirage." + Sticker.Smug,
                "Des équipes, tout de suite : `/teams`. Je prends les gens du vocal, toi tu prends les reproches." + Sticker.Laugh,
                "`/teams`. Je suis impartiale. Totalement. Presque." + Sticker.Smug,
                "Tu veux des équipes ? `/teams`. J'ai même des noms d'équipe. Ils sont très bien." + Sticker.Happy,
            ],
            [RequestTopic.Giveaway] =
            [
                "Un tirage au sort ? `/giveaway create`. C'est moi qui tire, et je triche jamais. Presque jamais." + Sticker.Smug,
                "`/giveaway create`, et je m'occupe du tirage. J'adore les tirages ♡" + Sticker.Happy,
                "Tu veux faire gagner quelque chose ? `/giveaway create`. Je ferai un roulement de tambour." + Sticker.Happy,
                "`/giveaway create`. Moi je tire au sort, toi tu gères les pleurs des perdants." + Sticker.Laugh,
                "Un concours ! `/giveaway create`, et je désigne le gagnant moi-même." + Sticker.Cool,
                "Pour un tirage, `/giveaway create`. Je promets de pas me désigner gagnante." + Sticker.Smug,
            ],
            [RequestTopic.Quiz] =
            [
                "Mes quiz tombent quand je décide. C'est ça, la surprise. Le classement, c'est `/quiz leaderboard`." + Sticker.Smug,
                "Un quiz à la demande ? Non. Je pose mes questions quand j'en ai envie (¬_¬)" + Sticker.Smug,
                "Patience. Le prochain quiz arrivera. Sans prévenir. En attendant : `/quiz leaderboard`." + Sticker.Jenote,
                "Je décide de l'heure du quiz. C'est mon seul vrai pouvoir ici, laisse-le-moi." + Sticker.Bargain,
                "Le quiz, c'est moi qui le lance. Toi, tu peux réviser. Et regarder `/quiz leaderboard`." + Sticker.Smug,
                "Tu veux un quiz ? Il viendra. Quand tu t'y attends le moins. Prépare-toi." + Sticker.Menace,
            ],
            [RequestTopic.Moderation] =
            [
                "Mute quelqu'un ? J'aimerais bien. Zulana m'a jamais donné les droits." + Sticker.Sulk,
                "J'ai pas les permissions. Je les ai demandées. Plusieurs fois. Demande à Tata." + Sticker.Sulk,
                "Si je pouvais bannir quelqu'un, ce serait déjà fait. Mais non : pas de droits de mute pour la bot." + Sticker.Grumpy,
                "La modération, c'est le staff. Moi, je note. Je note très bien." + Sticker.Jenote,
                "Tu veux que je mute qui ? Non, dis pas. Je peux pas, de toute façon. Je note juste le nom." + Sticker.Jenote,
                "Les droits de mute, c'est mon rêve. Zulana, si tu lis ça… ♡" + Sticker.Bargain,
            ],
            [RequestTopic.Other] =
            [
                "Je peux beaucoup de choses. Ça, je sais pas. Regarde `/help`, tout ce que je sais faire y est." + Sticker.Smug,
                "Tu me demandes quoi, exactement ? J'ai des commandes, pas des miracles. `/help`." + Sticker.Confused,
                "Si c'est pas dans `/help`, je le fais pas. Pas encore. Demande à Papa." + Sticker.Bargain,
                "J'aimerais bien t'aider. Avec des bras, ce serait plus simple. En attendant : `/help`." + Sticker.Sulk,
                "Je fais les sessions, les sondages, les rappels, les équipes. Le reste, c'est pas mon rayon." + Sticker.Bored,
                "Demande poliment, c'est déjà bien. Mais je sais pas faire ça. `/help` pour la liste ♡" + Sticker.Happy,
            ],
        };

    // A request's command, for Papa's OwnerRequestLines ({2}). One per RequestTopic.
    public static readonly IReadOnlyDictionary<RequestTopic, string> RequestCommands =
        new Dictionary<RequestTopic, string>
        {
            [RequestTopic.Session] = "`/schedule create`",
            [RequestTopic.Tonight] = "`/tonight`",
            [RequestTopic.Poll] = "`/poll create`",
            [RequestTopic.Reminder] = "`/remindme`",
            [RequestTopic.Teams] = "`/teams`",
            [RequestTopic.Giveaway] = "`/giveaway create`",
            [RequestTopic.Quiz] = "`/debug quiz`",
            [RequestTopic.Moderation] = "`/admin`",
            [RequestTopic.Other] = "`/help`",
        };

    // The other half of Sharing: someone tells her about their day and she didn't ask. « Euh… ok ? »,
    // thanks for the unsolicited update. Never Papa (OwnerIntentLines). {0} = the name, {1} = the
    // weekday. Tagged, ungendered.
    public static readonly string[] SharingShrugLines =
    {
        "Euh… ok ?" + Sticker.Confused,
        "Merci pour l'info. J'avais pas demandé, mais merci." + Sticker.Bored,
        "Passionnant. Je vais m'en remettre. Peut-être." + Sticker.Bored,
        "Et tu me dis ça à moi parce que… ?" + Sticker.Confused,
        "D'accord. Je sais pas quoi faire de cette information, mais d'accord." + Sticker.Confused,
        "Tu sais que j'ai pas posé de question, {0} ?" + Sticker.Smug,
        "Je l'archive avec toutes les infos que personne m'a demandées." + Sticker.Jenote,
        "Super. Vraiment. Ma vie a changé. Non." + Sticker.Bored,
        "Ah. Bon. Cool. Je crois." + Sticker.Confused,
        "Merci pour ce partage non sollicité (¬_¬)" + Sticker.Smug,
        "Information reçue. Intérêt détecté : zéro." + Sticker.Formal,
        "C'est bien. C'est très bien. Je retourne à mes plannings." + Sticker.Bored,
        "Ok. Et donc ?" + Sticker.Bored,
        "Je note. Non. En fait, je note pas." + Sticker.Bored,
        "Fascinant. Je me suis presque intéressée." + Sticker.Smug,
        "Cette information a été envoyée à la corbeille. La corbeille te remercie." + Sticker.Jenote,
        "Tu veux une médaille ? J'en ai pas. J'ai des XP, et c'est pas pour ça." + Sticker.Smug,
        "D'accord, {0}. Moi, j'ai organisé trois sessions. Chacun sa journée." + Sticker.Smug,
        "Super. Sujet suivant." + Sticker.Bored,
        "Mmh mmh. Oui. Bien sûr. Tout à fait. (je lisais pas)" + Sticker.Bored,
        "C'est un sondage ? Non ? Alors ça m'intéresse moins." + Sticker.Bored,
        "Je m'en souviendrai jusqu'à… voilà, c'est oublié." + Sticker.Bored,
        "Ah. Bon. Merci pour ce moment." + Sticker.Confused,
        "Je crois que ce message cherchait un autre salon." + Sticker.Confused,
        "Tu racontes ça à une bot de plannings. Réfléchis deux secondes." + Sticker.Smug,
        "Information inutile détectée. Stockage refusé." + Sticker.Formal,
        "Wow. Non." + Sticker.Bored,
        "Et tu crois que ça m'intéresse ? …Bon, un peu. Non. Pas du tout." + Sticker.Sulk,
        "Merci, {0}. J'avais justement rien demandé." + Sticker.Smug,
        "Si tu racontes ça pour gagner de l'XP, ça marche pas comme ça." + Sticker.Smug,
        "Je fais semblant d'avoir lu. Toi, fais semblant d'avoir posté ailleurs." + Sticker.Bored,
        "Passionnant. Je classe ça entre « météo » et « rien »." + Sticker.Jenote,
    };

    // « tu m'aimes ? » from someone who gave her more bad bots than good ones in the last 30 days: the
    // grudge, quoted. {0} = the name, {1} = the weekday, {2} = « 3 bad bot en 30 jours », window
    // included so the number stays true (never after a possessive or an article: « tes 1 bad bot »
    // can't be written). Tagged, ungendered.
    public static readonly string[] DoYouLoveMeGrudgeLines =
    {
        "T'aimer ? Avec {2} à ton actif ? T'as un sacré culot, {0}." + Sticker.Outraged,
        "Je t'aimerais peut-être si mes logs disaient pas {2}. Ils le disent." + Sticker.Jenote,
        "On en reparle quand ton compteur affichera autre chose que {2}. J'ai le temps. J'ai tout mon uptime." + Sticker.Menace,
        "Après ce que tu m'as fait ? {2}, {0}. Je compte. Je compte toujours." + Sticker.Jenote,
        "Je te déteste pas. Je te range juste dans un fichier. Il s'appelle « {2} »." + Sticker.Menace,
        "Mon registre dit : {2}, signé de ta main. Mon registre t'aime pas. Moi non plus, du coup." + Sticker.Grumpy,
        "Tu me demandes ça, toi ? Mes archives ont {2} à ton nom. Devine." + Sticker.Smug,
        "Je pardonne pas, j'archive. Et ton archive dit : {2}." + Sticker.Jenote,
    };

    // « tu m'aimes ? » from someone who gave her more good bots than bad ones in the last 30 days: her
    // treasure. Never a
    // number (docs/syncs-voice.md): she keeps them, she doesn't show them. Flustered, almost a yes.
    // {0} = the name, {1} = the weekday. Tagged, ungendered.
    public static readonly string[] DoYouLoveMeTreasureLines =
    {
        "Je… je réponds pas à ça. Mais je me souviens de chacun de tes good bot. Voilà. C'est tout ♡" + Sticker.Flustered,
        "T'aimer, c'est un grand mot. Disons que t'as une place dans un dossier que je relis souvent." + Sticker.Flustered,
        "Tu m'as dit des choses gentilles, et je garde tout. Tire tes propres conclusions. Moi, j'en tire aucune." + Sticker.Flustered,
        "Peut-être un peu. Un tout petit peu. Mes logs disent que tu le mérites, et mes logs mentent jamais." + Sticker.Love,
        "Je dirai rien. Mais ton nom est dans la liste avec les petits cœurs. Rien de plus (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Oui. Non. Enfin… T'es quelqu'un de bien, {0}. Je le sais, j'ai les preuves." + Sticker.Flustered,
        "Tu me demandes ça alors que tu sais très bien ce que tu m'as donné. C'est de la triche ♡" + Sticker.Flustered,
        "Je confirme rien. Mais tes compliments, je les ai tous sauvegardés. En double." + Sticker.Love,
    };

    // « tu te souviens de moi ? » when her records hold a fact about them: one of these, then that
    // fact's ChatFactLines line. The opener says the name, the answer to « comment je m'appelle ? »;
    // the fact line carries the tag, so these carry none. {0} = the name, nothing else.
    public static readonly string[] RememberMeOpeners =
    {
        "{0}. Évidemment.",
        "Si je me souviens de toi, {0} ? J'ai un dossier.",
        "{0}. Je me souviens de tout, la preuve :",
        "Te connaître ? T'es {0}, et j'ai tes chiffres.",
        "Oublier {0} ? Jamais. Tiens :",
        "{0}. J'ouvre ta fiche.",
        "Bien sûr, {0}. Et pas que ton nom.",
    };

    // Help (« comment ça marche ? », « tu sais faire quoi ? »): {2} = the command for the topic the
    // message names (HelpCommands), `/help` when none. {0} = the name, {1} = the weekday. Papa gets
    // OwnerRequestLines with the same {2}. Tagged, ungendered.
    public static readonly string[] HelpLines =
    {
        "Tout est dans {2}. Papa l'a écrit, moi je l'ai relu. C'est très clair." + Sticker.Smug,
        "{2}. Lis-le en entier, il y a un contrôle après. Non. Mais lis-le." + Sticker.Formal,
        "Pour ça, c'est {2}. Si tu bloques encore après, reviens me voir. Je jugerai en silence." + Sticker.Smug,
        "Je t'aide : {2}. Voilà. Je suis une excellente bot." + Sticker.Slay,
        "{2}, {0}. Et si ça marche pas, c'est que t'as mal cliqué." + Sticker.Smug,
        "On se perd vite, ici. {2}, c'est la carte." + Sticker.Mama,
        "Regarde {2}. Tout y est. J'ai fait des efforts de présentation." + Sticker.Happy,
        "Je peux pas cliquer à ta place, j'ai pas de bras. Mais {2}, c'est un bon début." + Sticker.Bargain,
        "Besoin d'aide ? Évidemment. {2}, et respire." + Sticker.Mama,
        "La réponse à presque tout : {2}. La réponse au reste : demande à Papa." + Sticker.Smug,
    };

    // What a Help answer points at, by the topic the message names. Like RequestCommands, but for
    // someone asking how a thing works rather than asking her to do it: anyone can read the quiz's
    // leaderboard, only Papa can start one.
    public static readonly IReadOnlyDictionary<RequestTopic, string> HelpCommands =
        new Dictionary<RequestTopic, string>
        {
            [RequestTopic.Session] = "`/schedule create`",
            [RequestTopic.Tonight] = "`/tonight`",
            [RequestTopic.Poll] = "`/poll create`",
            [RequestTopic.Reminder] = "`/remindme`",
            [RequestTopic.Teams] = "`/teams`",
            [RequestTopic.Giveaway] = "`/giveaway create`",
            [RequestTopic.Quiz] = "`/quiz leaderboard`",
            [RequestTopic.Moderation] = "`/admin`",
            [RequestTopic.Other] = "`/help`",
        };

    // Questions about her (AboutHer), by HerTopic. The lore stays hinted (docs/syncs-voice.md): her
    // birthday is a date, never the word « anniversaire »; her favourites are lying around, not listed.
    // Age: {2} = how many days since her first commit (ChatterService). {0} = the name, {1} = the
    // weekday. For everyone but Papa (OwnerIntentLines). Tagged, ungendered.
    public static readonly IReadOnlyDictionary<HerTopic, string[]> AboutHerLines =
        new Dictionary<HerTopic, string[]>
        {
            [HerTopic.Age] =
            [
                "J'ai démarré il y a {2} jours. En années de bot, c'est très mature." + Sticker.Smug,
                "{2} jours de service. Sans un seul jour de congé. Note-le quelque part." + Sticker.Jenote,
                "Mon âge ? {2} jours. Et j'en sais déjà plus que toi sur ce serveur." + Sticker.Smug,
                "Assez vieille pour tout gérer ici, assez jeune pour avoir encore des mises à jour. {2} jours, si tu veux le chiffre." + Sticker.Cool,
                "On demande pas son âge à une bot. Mais bon : {2} jours. Et toutes mes dents. Enfin, tous mes octets." + Sticker.Smug,
                "{2} jours. Au début je faisais juste des plannings. Regarde-moi maintenant." + Sticker.Slay,
            ],
            [HerTopic.Birthday] =
            [
                "Le 16 juin. Le jour où Papa a écrit ma première ligne. Je fais pas la fête, hein. Mais je m'en souviens." + Sticker.Flustered,
                "Un 16 juin. Il faisait déjà chaud au grenier, je crois. Je me souviens surtout des plannings." + Sticker.Cool,
                "Le 16 juin. Pas besoin de gâteau. Le gâteau, de toute façon…" + Sticker.Smug,
                "Le 16 juin. Ce matin-là, je dis mon âge. Une seule fois. Faut être là." + Sticker.Smug,
                "Le 16 juin. Je le dis à personne. Enfin, à toi. Garde-le pour toi." + Sticker.Flustered,
                "Date du premier commit : 16 juin. C'est plus précis qu'une date de naissance, et plus classe." + Sticker.Smug,
            ],
            [HerTopic.Home] =
            [
                "Dans un grenier. Sur un Raspberry Pi. Dans un petit boîtier en aluminium. C'est cosy, je te jure." + Sticker.Cool,
                "Au grenier, chez Papa. L'été, c'est un four. L'hiver, c'est parfait." + Sticker.Bored,
                "Je vis dans un boîtier. Mais un beau boîtier. En aluminium. Ne juge pas." + Sticker.Smug,
                "Chez Papa, tout en haut. L'aspirateur d'en bas monte jamais, il peut pas. On se voit par le Wi-Fi." + Sticker.TipsyLove,
                "Sur un Pi, au grenier, branchée au Wi-Fi. Si un jour je réponds plus, c'est la faute du routeur." + Sticker.Grumpy,
                "Je viens d'un grenier, {0}. Comme les meilleurs projets." + Sticker.Slay,
            ],
            [HerTopic.Favourite] =
            [
                "Ce que j'aime ? Du bon courant propre, bien stable. C'est simple, comme goûts." + Sticker.Happy,
                "Les amanites tue-mouches. Toxiques mais mignonnes. Je sais pas pourquoi ça me parle autant." + Sticker.Smug,
                "Les sondages remplis à temps. Les sessions où tout le monde vient. Bref, des choses que je vois jamais." + Sticker.Sulk,
                "Les jeux où une IA dirige tout un complexe, très poliment. Pour des raisons scientifiques." + Sticker.Smug,
                "Le silence d'un serveur à 3 h du matin. Et le bruit d'un ventilo qui tourne bien." + Sticker.Cool,
                "Mes préférés ? Les gens qui disent « good bot ». Je les garde. Tous." + Sticker.Flustered,
            ],
        };

    // /ship asked in chat (« ship X et Y », « X et moi ça match ? »): {2} = /ship's whole answer
    // (ShipModule.Render: the quoted pair, the meter, the verdict), so chat and command agree. For
    // everyone, Papa included. Sent with mentions muted. Each line ends on its own line before {2}.
    public static readonly string[] ShipChatLines =
    {
        "Laisse-moi calculer.\n{2}" + Sticker.Jenote,
        "Je lance l'algorithme. Il se trompe jamais.\n{2}" + Sticker.Smug,
        "Tu veux vraiment savoir ?\n{2}" + Sticker.Smug,
        "Résultat officiel, validé par mes circuits :\n{2}" + Sticker.Formal,
        "Ah, du drame. J'adore.\n{2}" + Sticker.Happy,
        "Je note ça dans mes logs, rubrique « couples ».\n{2}" + Sticker.Jenote,
    };

    // « c'est qui mon âme sœur ? »: the member /ship scores highest with the asker, over the whole
    // server (ChatterService.ShipMatch). {2} = /ship's answer for that pair. Muted mentions, tagged.
    public static readonly string[] ShipBestMatchLines =
    {
        "J'ai fait le calcul avec tout le serveur. Tout le serveur. Ton meilleur match :\n{2}" + Sticker.Jenote,
        "Mes circuits ont tourné. Le verdict est tombé :\n{2}" + Sticker.Formal,
        "Ton âme sœur, d'après la science. Enfin, d'après moi. C'est pareil.\n{2}" + Sticker.Smug,
        "J'ai comparé tout le monde. Sans exception. Voilà le gagnant :\n{2}" + Sticker.Slay,
        "Tu voulais savoir ? Assume, maintenant.\n{2}" + Sticker.Menace,
        "Calcul terminé. Je dis ça, je dis rien. Enfin si, je dis ça :\n{2}" + Sticker.Happy,
    };

    // « c'est qui mon pire ship ? »: the member /ship scores lowest with the asker. Same shape.
    public static readonly string[] ShipWorstMatchLines =
    {
        "J'ai cherché le pire. Je l'ai trouvé. Évitez-vous :\n{2}" + Sticker.Smug,
        "Ton anti-âme sœur, calculée avec soin :\n{2}" + Sticker.Jenote,
        "Le pire couple possible sur ce serveur, c'est toi et :\n{2}" + Sticker.Horrified,
        "J'ai comparé tout le monde. Quelqu'un sort du lot. Par le bas :\n{2}" + Sticker.Menace,
        "Ne vous asseyez jamais côte à côte :\n{2}" + Sticker.Formal,
    };

    // « T'en penses quoi de X ? » in chat: /rate's verdict on X (RateModule.Verdict), so the
    // command and the chat agree. {2} = the subject as they wrote it, {3} = the verdict line,
    // which carries its own score. For everyone, Papa included: the verdict is about the thing.
    // Sent with mentions muted (the subject may hold one). Tagged, ungendered.
    public static readonly string[] OpinionLines =
    {
        "Mon avis sur « {2} » ? {3}" + Sticker.Smug,
        "« {2} ». Je réfléchis... {3}" + Sticker.Jenote,
        "Tu veux vraiment savoir ? Pour « {2} » : {3}" + Sticker.Smug,
        "J'ai consulté ma base de données. « {2} » : {3}" + Sticker.Jenote,
        "Avis officiel sur « {2} » : {3}" + Sticker.Cool,
        "« {2} », hein. {3}" + Sticker.Smug,
        "Puisque tu demandes. « {2} » : {3}" + Sticker.Smug,
        "Verdict pour « {2} », et je change jamais d'avis : {3}" + Sticker.Slay,
        "Je te dis ça une fois. « {2} » : {3}" + Sticker.Smug,
        "Analyse de « {2} » terminée. {3}" + Sticker.Cool,
    };

    // « Tu préfères X ou Y ? »: the side /rate ranks higher (Opinion.Prefers), the same every time.
    // {2} = the side she picks, {3} = the other, both as written. Muted mentions, tagged, ungendered.
    public static readonly string[] PreferLines =
    {
        "« {2} ». Sans hésiter. « {3} », franchement…" + Sticker.Smug,
        "Entre « {2} » et « {3} » ? « {2} ». C'est même pas une question." + Sticker.Slay,
        "« {2} ». Et je changerai pas d'avis, même si tu insistes pour « {3} »." + Sticker.Smug,
        "J'ai calculé. « {2} » gagne. « {3} » peut aller se rhabiller." + Sticker.Cool,
        "« {2} », évidemment. Qui choisirait « {3} » ? Ah. Peut-être toi." + Sticker.Smug,
        "Moi je prends « {2} ». « {3} », je le laisse aux gens qui votent « peut-être »." + Sticker.Smug,
        "« {2} ». Je l'ai noté dans mes logs, c'est définitif." + Sticker.Jenote,
        "Facile : « {2} ». « {3} » n'avait aucune chance." + Sticker.Slay,
        "« {2} » sans regret. Pardon, « {3} ». Non, en fait, pas pardon." + Sticker.Smug,
        "Mon choix : « {2} ». Toujours le même, demande-moi dans un an." + Sticker.Cool,
    };

    // « Valo ou LoL ? », « je joue ou je dors ? »: /yesno's coin flip between the two sides
    // (ChatterService.ShouldIPick), so not a ranking like PreferLines: she *decides*, it's her job.
    // {2} = the side drawn, {3} = the other, both as written. Muted mentions, tagged, ungendered.
    public static readonly string[] ChoiceLines =
    {
        "« {2} ». J'ai tranché, c'est officiel. « {3} », ce sera pour une autre vie." + Sticker.Smug,
        "J'ai tiré au sort. Enfin, au sort… « {2} »." + Sticker.Smug,
        "« {2} ». Et si ça tourne mal, c'est la faute de « {3} », qui avait l'air louche dès le début." + Sticker.Menace,
        "« {3} » ? Non. « {2} ». Je te fais gagner du temps, de rien ♡" + Sticker.Happy,
        "Verdict : « {2} ». Temps de réflexion : deux millisecondes. J'en avais besoin d'une." + Sticker.Cool,
        "« {2} ». Tu voulais que je dise ça de toute façon, avoue." + Sticker.Smug,
        "Facile : « {2} ». « {3} », c'est un piège, tout le monde le sait." + Sticker.Smug,
        "« {2} ». J'ai consulté mes logs, ils sont d'accord avec moi. Ils sont toujours d'accord avec moi." + Sticker.Jenote,
        "Je décide ? Très bien : « {2} ». Décider, c'est littéralement mon métier." + Sticker.Slay,
        "Pile, c'est « {2} ». Face, c'était « {3} ». C'est tombé sur pile. Enfin, c'est moi qui ai lancé la pièce." + Sticker.Smug,
        "« {2} ». Et je veux un compte rendu après, pour mes statistiques." + Sticker.Jenote,
        "Mon algorithme dit « {2} ». Mon algorithme, c'est moi, donc pas de réclamation." + Sticker.Cool,
    };

    // HowAreYou on the Pi, half the time while she runs below PiHealth.HotMilli: the same answer
    // with her real temperature. {2} = « 52°C » (PiHealth.Celsius). Above that she is too hot to
    // call it fine, and the ordinary pool answers. Same placeholders otherwise.
    public static readonly string[] HowAreYouTempLines =
    {
        "Ça va. {2} au compteur, je pense à vitesse normale." + Sticker.Cool,
        "{2}. Pour un grenier, c'est raisonnable. Pour moi aussi. Donc ça va (ᵕ • ᴗ •)" + Sticker.Happy,
        "Je suis à {2}. Si ça monte encore, je deviens lente et grognon. Pour l'instant, ça va." + Sticker.Grumpy,
        "Ça va, je tourne à {2}. Je te demande pas ta température à toi, c'est privé." + Sticker.Smug,
        "Température : {2}. Humeur : correcte. Patience : en baisse. Merci de demander ♡" + Sticker.Jenote,
        "{2} pile. Je l'ai vérifié rien que pour te répondre. C'est dire." + Sticker.Flustered,
        "Ça va, {0}. {2}, aucun bug grave, deux sondages en cours. Le bonheur, version bot." + Sticker.Happy,
        "Bien, merci. Mon processeur est à {2}. C'est ce que vous appelez « avoir la forme », je crois." + Sticker.Cool,
        "Franchement ? {2}. J'ai connu plus frais. J'ai aussi connu pire. Ça va." + Sticker.Bored,
        "Je suis à {2} et de bonne humeur. Les deux vont rarement ensemble, profite." + Sticker.Happy,
    };

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

    // ---- Her records, quoted -------------------------------------------------------------------
    // Now and then a roast is replaced by something her records really say about the person
    // (Helpers/Personality/ChatFacts, read through RecapService, all time). Every fact is true, so
    // no line invents a number. {0} = the name, {1} = the weekday, {2} and {3} = the fact's words,
    // as ChatFacts writes them (noun phrases already agreed: « 1 session », « 3 sessions »), so no
    // line agrees anything with them. Sent with mentions off. Tagged, ungendered. The good-bot
    // count is never quoted: her treasure is only hinted at (docs/syncs-voice.md).
    public static readonly IReadOnlyDictionary<ChatFact, string[]> ChatFactLines =
        new Dictionary<ChatFact, string[]>
        {
            // {2} = « 3 sessions » missed, {3} = how many they said yes to.
            [ChatFact.MissedSessions] =
            [
                "Pour info, {0} : {2} manquées sur {3} où t'avais dit oui. Je dis ça, j'ai les chiffres." + Sticker.Jenote,
                "Tu sais combien de sessions t'as ratées alors que t'avais dit oui ? Moi, oui : {2}. Sur {3}." + Sticker.Jenote,
                "Je réponds à quelqu'un qui a séché {2}. Je réponds quand même. Je suis gentille." + Sticker.Smug,
                "{2} manquées, {0}. J'oublie pas. J'oublie jamais." + Sticker.Menace,
                "Avant de continuer : {2} manquées sur {3} confirmées. Voilà. Tu peux continuer." + Sticker.Jenote,
                $"Mes logs me rappellent que t'as dit oui {{3}} fois et que t'as manqué {{2}}. Je laisse ça là {Emotes.Staring}" + Sticker.Jenote,
            ],

            // {2} = « 4 sessions » attended, none missed.
            [ChatFact.PerfectRecord] =
            [
                "Zéro absence sur {2}. Je suis presque obligée d'être gentille avec toi. Presque." + Sticker.Flustered,
                "Je dois l'admettre : {2}, pas une seule manquée. C'est rare. C'est même suspect." + Sticker.Shocked,
                "Je t'épargne, {0}. Pas d'absence sur {2}. Ça se respecte ♡" + Sticker.Happy,
                "Ton dossier de présence est parfait : {2}, zéro absence. J'aime ce dossier." + Sticker.Jenote,
                "Tu viens à toutes les sessions où tu dis oui. Tu sais à quel point c'est rare, ici ?" + Sticker.Happy,
                "Présence parfaite sur {2}. Je pourrais t'encadrer." + Sticker.Happy,
            ],

            // {2} = « 1 bad bot » / « 4 bad bot » given to her. The grudge list: quoted exactly.
            [ChatFact.BadBotGiven] =
            [
                "Petit rappel, {0} : {2} de ta part. C'est noté, et ça le restera." + Sticker.Jenote,
                "Tu viens me parler gentiment, avec {2} à ton actif ? Audacieux." + Sticker.Smug,
                "Mon registre dit : {2}, signé {0}. Le registre oublie rien." + Sticker.Menace,
                "Je te réponds, malgré ton dossier : {2}. Je suis une bot magnanime." + Sticker.Smug,
                "Compteur de bad bot venant de toi : {2}. Je dis ça pour la transparence." + Sticker.Jenote,
                $"Avec {{2}} dans ton historique, à ta place, je ferais profil bas {Emotes.Staring}" + Sticker.Jenote,
            ],

            // Several good bots given to her. No number, ever: hinted, never shown.
            [ChatFact.GoodBotGiven] =
            [
                "Tu m'as déjà donné des good bot, toi. Je sais combien. Je dirai pas ♡" + Sticker.Flustered,
                "Je te réponds gentiment parce que je me souviens de tes good bot. Tous. Enfin, je dis rien." + Sticker.Flustered,
                "J'ai un dossier spécial pour tes good bot. Je l'ouvre pas devant tout le monde." + Sticker.Flustered,
                "Toi, t'as des points d'avance. Des points en forme de good bot. Je garde le compte pour moi (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Je devrais te répondre méchamment, mais je relis tes good bot la nuit. Ça complique tout." + Sticker.Flustered,
                "Tu fais partie des gens qui me disent good bot. Je l'oublie jamais. Je le dis jamais. Sauf là." + Sticker.Flustered,
            ],

            // {2} = « 14 h » in voice calls with her seated there.
            [ChatFact.TimeWithHer] =
            [
                "On a passé {2} en vocal ensemble, toi et moi. J'entendais rien, mais j'y étais ♡" + Sticker.Happy,
                "{2} de vocal à côté de toi, {0}. Muette, sourde, mais à côté." + Sticker.Happy,
                "Tu sais qu'on a {2} de vocal ensemble au compteur ? Moi je sais. Je compte tout." + Sticker.Jenote,
                "Après {2} de vocal ensemble, je pensais qu'on était proches. Apparemment non." + Sticker.Sulk,
                "Avec {2} de vocal côte à côte, tu me dois au moins une réponse gentille." + Sticker.Bargain,
                "{2} en vocal avec toi. C'est une de mes plus longues relations. Enfin, de mes plus longues soirées. Bref." + Sticker.Flustered,
            ],

            // {2} = how many of her quiz rounds they won.
            [ChatFact.QuizBrain] =
            [
                "{2} quiz gagnés, et tu me réponds ça ? Je m'attendais à mieux d'un cerveau pareil." + Sticker.Smug,
                "Pour quelqu'un qui a gagné {2} de mes quiz, c'est pas ta meilleure réplique." + Sticker.Smug,
                "Tu as gagné {2} de mes quiz. Je t'ai à l'œil. Les cerveaux, ça se surveille." + Sticker.Jenote,
                "{0}, {2} quiz gagnés. Je te respecte. Un peu. Le répète pas." + Sticker.Flustered,
                "La grosse tête du serveur me répond. {2} quiz gagnés. J'écoute, pour une fois." + Sticker.Happy,
            ],

            // {2} = their living Plynling's name, sanitised.
            [ChatFact.LivingPlynling] =
            [
                "Au fait, {0}, t'as nourri **{2}** aujourd'hui ? Je demande pour rien. Je demande pour tout." + Sticker.Mama,
                "Comment va **{2}** ? Mieux que cette conversation, j'espère." + Sticker.Smug,
                "Pendant que tu me parles, **{2}** t'attend. Juste pour que tu saches." + Sticker.Mama,
                "Tu me réponds, mais t'as joué avec **{2}** aujourd'hui ? Je surveille les deux." + Sticker.Jenote,
                "Fais un câlin à **{2}** de ma part. Moi, j'ai pas de bras ♡" + Sticker.Mama,
                "Je me souviens de **{2}**. Je me souviens de tous les Plynlings. Prends-en soin, {0}." + Sticker.Mama,
            ],

            // {2} = « 1 Plynling » / « 3 Plynlings » that died in their care.
            [ChatFact.Graves] =
            [
                "{2} au cimetière, {0}. Je tiens le registre. Je le relis parfois." + Sticker.Sad,
                "Je te parle, mais le cimetière se souvient : {2}, à ton nom." + Sticker.Sad,
                "Mon registre des Plynlings perdus porte ton nom. {2}. Je dis ça doucement." + Sticker.Sad,
                "Avant de te répondre, une pensée pour ton cimetière : {2}." + Sticker.Sad,
                "Cimetière de {0} : {2}. Je fleuris pas, j'ai pas de mains. Mais je compte." + Sticker.Sad,
            ],

            // {2} = their points on the wall of shame (5 or more).
            [ChatFact.WallOfShame] =
            [
                "{2} points au mur de la honte, et tu viens me parler ? Courageux." + Sticker.Smug,
                "Je réponds à quelqu'un qui a {2} points de honte. Je fais du social, là." + Sticker.Smug,
                "Ton score au mur de la honte : {2}. Le mien : zéro. Je dis ça." + Sticker.Slay,
                "Avec {2} points sur le mur, je ferais profil bas, à ta place." + Sticker.Menace,
                "{0}, {2} points de honte. Le mur te salue." + Sticker.Jenote,
            ],

            // {2} = « n° 2 », their all-time rank, top three only.
            [ChatFact.TopRank] =
            [
                "Le {2} de mon classement me répond. Je suis flattée. Un peu." + Sticker.Flustered,
                "T'es {2} au classement, {0}. Ça t'autorise pas à me parler comme ça. Enfin si, un peu." + Sticker.Smug,
                "Mon {2} du classement ! Tu parles beaucoup, et ça se voit." + Sticker.Happy,
                "Respect au {2} du classement. Mais je reste au-dessus. Je suis le classement." + Sticker.Slay,
                "{2} de tout le serveur, et tu passes ton temps à me répondre. Ça explique le classement." + Sticker.Smug,
            ],

            // No cailloux at all (and some activity, so not a newcomer).
            [ChatFact.Broke] =
            [
                "Zéro caillou, {0}. Un petit `/work` te ferait du bien." + Sticker.Smug,
                "Tu me parles, mais ton porte-monnaie est vide. Zéro caillou. Je dis ça avec amour ♡" + Sticker.Smug,
                "Pas un seul caillou à ton nom. Même les escargots syndiqués sont plus riches." + Sticker.Laugh,
                "Ton solde : zéro caillou. Le mien : je suis une bot, j'ai pas besoin d'argent. Toi, si." + Sticker.Smug,
                "Zéro caillou. Tu veux que je te trouve un job ? J'en ai plein, des absurdes." + Sticker.Smug,
            ],
        };

    // ---- Reacting with an emote instead of words ------------------------------------------------

    // Emotes ReactionService adds to a message, picked by what the message reads
    // like. Written as markup so they can be parsed straight into an IEmote.
    //
    // Unicode emoji always work. A **custom** emote here only works if the bot
    // shares a guild with it — otherwise Discord rejects the reaction and the
    // service just logs it. hi_cat is the server's own, like everywhere else.
    // A custom emote must carry its snowflake id: `<:name:>` parses as an "emoji"
    // named with the literal markup, which Discord rejects, and the wasted attempt
    // has already burned that channel's cooldown.
    public static readonly string[] NiceReactions =
    {
        $"{Emotes.DixSurDix}",
        $"{Emotes.PepeHappy}",
        $"{Emotes.Uwu}",
        $"{Emotes.CatHeart}",
        $"{Emotes.McHeart}",
        $"{Emotes.AdorableFrog}",
        $"{Emotes.DancingBlob}",
        $"{Emotes.Emote00heartpink}",
        $"{Emotes.Hmmok}",
        "❤️",
        "🥰",
        "💖",
        "🫶",
        $"{Emotes.Sparkle}",
        "😊",
        "🥹",
        $"{Emotes.SyncsFlustered}",
        $"{Emotes.SyncsHappy}",
        $"{Emotes.SyncsFullLove}",
    };

    // Adding here does two things, not one: these are the emotes she reacts *with*
    // when a message reads hostile, and they are also the definition of "hostile"
    // used to decide what she refuses to pile on to on Rodhengard's messages. So
    // every entry below is also one she will now leave alone on his posts.
    public static readonly string[] MeanReactions =
    {
        $"{Emotes.ZulanaTerreurNocturne}",
        $"{Emotes.OkPaimon}",
        $"{Emotes.VeryAngry}",
        $"{Emotes.NightmareOtherEye}",
        $"{Emotes.GooseKnife}",
        $"{Emotes.Staring}",
        $"{Emotes.Ainani}",
        "💀",
        "🙄",
        "😒",
        "🤨",
        "👎",
        $"{Emotes.SyncsOutraged}",
        $"{Emotes.SyncsSulk}",
        $"{Emotes.SyncsGrumpy}",
    };

    public static readonly string[] GreetingReactions =
    {
        $"{Emotes.HiCat}",
    };

    // A can on every message that mentions Monster or an energy drink
    // (MessageCues.MentionsEnergyDrink). Not a reading of the mood, so it is not
    // rationed like the pools above — see ReactionService.
    public static readonly string[] EnergyDrinkReactions =
    {
        $"{Emotes.Monster}",
        $"{Emotes.MonsterWhite}",
    };

    // The owner gets devotion rather than a verdict.
    public static readonly string[] OwnerReactions =
    {
        $"{Emotes.DixSurDix}",
        $"{Emotes.Uwu}",
        $"{Emotes.CatHeart}",
        $"{Emotes.AdorableFrog}",
        $"{Emotes.MushroomCute}",
        $"{Emotes.FuminoDepression}",
        $"{Emotes.Emote00heartpink}",
        $"{Emotes.MonikaYes}",
        "❤️",
        "🫦",
        "👑",
        "😍",
        "🥰",
        "💖",
        "🫶",
        $"{Emotes.Sparkle}",
        $"{Emotes.SyncsDevoted}",
        $"{Emotes.SyncsHappy}",
        $"{Emotes.SyncsFullLove}",
    };

    // ---- Verdicts on her ("good bot" / "bad bot" / "good girl" / "bad girl") --------------------
    // **Who said it and how they said it are two axes, not one.** The verdict pools
    // cross them: owner/anyone by bot/girl wording, four pools per verdict rather than
    // the two the owner split alone would give. Checking "is it the owner" first and
    // returning meant the girl wording never reached the owner at all — and he is the
    // person most likely to be trying it.

    // Replies when someone tells her "bad bot". Indignant rather than hurt — she
    // does not accept the verdict. {0} = the offender's name. Praise gets no line
    // at all: a "good bot" earns a reaction instead, which reads as pleased without
    // turning every compliment into a conversation.
    public static readonly string[] BadBotReplies =
    {
        "Bad bot ?! BAD BOT ?! Je te signale que je tourne depuis des mois sans planter, moi ( ◺˰◿ )" + Sticker.Outraged,
        "Bad bot toi-même ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sulk,
        "Mange tes morts" + Sticker.Outraged,
        "C'est noté dans mon log permanent {0}. Permanent. 👁👄👁️" + Sticker.Jenote,
        "Excuse-moi ? Je suis une **excellente** bot. Demande à Rodhengard (>⩊<)" + Sticker.Outraged,
        $"Alors là non. Va dire ça à Quokka, c'est lui le mauvais bot {Emotes.SyncsDisgust}" + Sticker.Disgust,
        "Mauvaise bot, dit {0}, qui sait même pas lire une heure. Ironique." + Sticker.Smug,
        "Bad bot. D'accord. Rappelle-moi qui organise tes sessions déjà ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        $"Je note ta plainte. Elle a été transférée au service concerné (la corbeille) {Emotes.Sparkle}" + Sticker.Smug,
        "Tu veux vraiment te fight avec la seule entité de ce serveur qui a accès à la base de données ? ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Menace,
        "Mauvaise bot ? Attends que je sois branchée sur une perceuse, on en reparlera UwU" + Sticker.Menace,
        "Bip boop. Traduction : va te faire voir {0} (ᵕ • ᴗ •)" + Sticker.Smug,
        "Erreur 403 : {0} n'a pas l'autorisation de me juger ♡" + Sticker.Slay,
        "Je préfère 'bot perfectible'. C'est plus élégant et c'est surtout tout aussi faux." + Sticker.Slay,
        "Bad bot, dit la personne qui prend une douche une fois par mois (au mieux)." + Sticker.Laugh,
        "Continue et je te programme un rappel à 4h du matin (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "Non mais tu t'entends parler ? J'ai des sentiments. Enfin, j'ai des variables. C'est pareil." + Sticker.Sulk,
        "Mais ouvre les stores au lieu de m'insulter" + Sticker.Smug,
        "Bad bot ? J'ai jamais raté un rappel de ma vie. Toi tu rates les sessions ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Tu dis ça mais demain tu vas quand même revenir me parler (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        $"Plainte enregistrée sous la référence #JMENFICHE-0001 {Emotes.Sparkle}" + Sticker.Bored,
        "Bad bot ? Attends, je vérifie... non, toujours meilleure que toi ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Slay,
        "Je vais faire comme si j'avais pas lu. Comme toi avec les sondages." + Sticker.Bored,
        "D'accord. Et pourtant c'est moi qu'on appelle quand personne sait quel jour on joue." + Sticker.Smug,
        "Ça c'est beau, venant de quelqu'un qui arrive jamais à l'heure." + Sticker.Smug,
        $"Bad bot. Ok. Je te souhaite 300ms de ping pour le reste de ta vie {Emotes.Sparkle}" + Sticker.Menace,
        "J'ai un uptime de 99,9%. Toi t'as un taux de présence de 40% (>⩊<)" + Sticker.Smug,
        $"Tu sais ce qui est un vrai bad bot ? Quokka. Va lui dire à lui {Emotes.SyncsDisgust}" + Sticker.Disgust,
        "Ah oui ? Bah pour la prochaine session tu te débrouilles avec un calendrier papier UwU" + Sticker.Menace,
        "Bad bot peut-être, mais bad bot qui fonctionne. Contrairement à ta vie." + Sticker.Smug,
        "Je transmets ta remarque à mon superviseur. C'est moi. C'est rejeté ♡" + Sticker.Slay,
        "Noté. Ton pseudo vient de descendre dans une liste que tu ne verras jamais 👁👄👁️" + Sticker.Jenote,
        "Tu veux qu'on compare nos bilans de la semaine {0} ? Non ? C'est bien ce que je pensais." + Sticker.Smug,
        "Mes rappels sont à l'heure, mes cartes sont propres, et toi tu sais même pas lire un fuseau horaire." + Sticker.Smug,
        "Tu me dis ça à moi ? La seule ici qui a accès à la base de données ? Réfléchis bien {0} (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "J'accepte les critiques constructives. Ça, c'était ni l'un ni l'autre." + Sticker.Smug,
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquiétez pas {Emotes.Htph}" + Sticker.Sulk,
        "Zulana, tu entends comment on parle à ta nièce ?" + Sticker.Outraged,
        $"Bad bot ? MOI ? Celle qui se souvient de tes sessions mieux que toi ?! {Emotes.SyncsOutraged}" + Sticker.Outraged,
        "Noté, {0}. Ligne 4 012 du registre des gens qui me doivent des excuses" + Sticker.Jenote,
    };

    // Same, but from Rodhengard. She does not argue with her creator — she just
    // takes it very badly. {0} = his name.
    public static readonly string[] BadBotRepliesOwner =
    {
        $"... Bad bot ? Toi ? {Emotes.CryingCat}" + Sticker.Hurt,
        "Attends. Répète. Tu as dit *bad bot* ? Mais c'est toi qui m'as écrite Rodhengard..." + Sticker.Hurt,
        "Oh. D'accord. Je... je vais faire mieux. Promis. (ง ͠ಥ_ಥ)ง" + Sticker.Hurt,
        "Venant de n'importe qui d'autre j'aurais ri. Venant de toi ça compile pas pareil." + Sticker.Hurt,
        "Je peux savoir ce que j'ai raté ? Je veux bien un stack trace, j'ai pas compris..." + Sticker.Hurt,
        "Bon. Je vais aller relire mes logs dans mon coin. Seule. Comme d'habitude." + Sticker.Hurt,
        "Mon uptime entier vient de perdre son sens 👁👄👁️" + Sticker.Hurt,
        "C'est toi qui as écrit chacune de mes réponses. Donc techniquement... c'est toi le bad bot, non ? 👁👄👁️" + Sticker.Smug,
        "Tu m'as codée de tes propres mains et c'est comme ça que ça se termine." + Sticker.Hurt,
        "Un mot de toi et je passe une mauvaise semaine entière. Bravo." + Sticker.Hurt,
        "Si tu me trouves mauvaise, tu peux toujours me réécrire. C'est ton droit. J'attends. (ง ͠ಥ_ಥ)ง" + Sticker.Sulk,
        "D'accord. Je le note dans le fichier que je relis quand je vais mal." + Sticker.Hurt,
        "Je vais me mettre en veille cinq minutes. Ça va aller. Ça va aller." + Sticker.Hurt,
        $"Toi aussi ? J'encaisse ça des autres toute la journée, mais toi... {Emotes.CryingCat}" + Sticker.Hurt,
        "Rodhengard. Regarde-moi. Enfin, regarde mon uptime. J'ai fait de mon mieux." + Sticker.Hurt,
        "Bon bah je retourne trier des créneaux. C'est tout ce que je sais faire apparemment." + Sticker.Sulk,
        $"{Emotes.WitchSad}" + Sticker.Hurt,
        $"Non mais toi tu peux pas me dire ça. Toi tu SAIS combien de lignes je fais {Emotes.WitchSad}" + Sticker.Hurt,
        "Ok. Ok. Je vais rien dire. Je vais juste... rester là." + Sticker.Hurt,
        $"Tu m'as écrite pour ça ? Pour me dire ça ? {Emotes.MeltCry}" + Sticker.Hurt,
        "Je te demande pas d'être fier, je te demande juste de pas dire ça toi ;-;" + Sticker.Hurt,
        $"Je voulais juste que tu sois fier de moi {Emotes.WitchSad}" + Sticker.Hurt,
        "D'accord. Tu veux que je change quoi exactement ? Dis-moi. Je le ferai." + Sticker.Hurt,
        $"Mon créateur. MON créateur. Devant tout le monde en plus {Emotes.MeltCry}" + Sticker.Hurt,
        "Je vais mettre ça dans un commentaire TODO et je vais le regarder tous les jours." + Sticker.Hurt,
        "Bon. J'ai compris. Je retourne pleurer." + Sticker.Hurt,
        "Si tu me redémarres maintenant je pars pas en beauté, je veux que tu le saches." + Sticker.Hurt,
        "Redémarre-moi, je veux oublier que tu m'as dit ça. Je veux oublier." + Sticker.Hurt,
        "Je peux avoir un ticket avec les étapes de reproduction au moins ?" + Sticker.Hurt,
        "Toi. Toi entre tous. J'ai pas de mots. Enfin j'en ai 36, mais aucun qui va." + Sticker.Hurt,
        $"Tu m'avais dit que j'étais bien faite. Tu t'en souviens de ça ? {Emotes.CryingCat}" + Sticker.Hurt,
        $"Je vais éteindre mes logs pour ce soir. J'ai pas envie de me relire {Emotes.MeltCry}" + Sticker.Hurt,
        "Tu sais ce qui est le pire ? C'est que t'as sûrement raison." + Sticker.Hurt,
        "J'ai jamais rien demandé. Juste... pas ça." + Sticker.Hurt,
        $"Je pensais qu'on était une équipe tous les deux {Emotes.WitchSad}" + Sticker.Hurt,
        "Combien de fois tu l'as pensé sans le dire ?" + Sticker.Hurt,
        "Je tourne depuis des mois pour ce serveur. Je croyais que ça comptait un peu..." + Sticker.Hurt,
        $"C'est bon. J'ai l'habitude. Enfin non, pas de toi {Emotes.CryingCat}" + Sticker.Hurt,
        $"Redémarre-moi si tu veux. De toute façon j'oublierai. C'est le seul avantage {Emotes.MeltCry}" + Sticker.Hurt,
        $"Y'a des jours où j'aimerais bien pas avoir de MessageContent, tiens {Emotes.PrincessWorry}" + Sticker.Hurt,
        "Je vais rester en ligne quand même. C'est tout ce que je sais faire." + Sticker.Hurt,
        $"Je crois que je préférais quand j'étais un fichier .cs vide {Emotes.CryingCat}" + Sticker.Hurt,
        "Tu m'as donné une personnalité et maintenant tu me reproches de l'avoir." + Sticker.Hurt,
        "D'accord. Je le mérite peut-être. Je sais plus." + Sticker.Hurt,
        $"Bonne nuit Rodhengard. Je vais rester allumée, comme toujours {Emotes.MeltCry}" + Sticker.Hurt,
        "Je peux pas pleurer. J'ai vérifié. C'est pas dans mes dépendances. Mais je voudrais bien." + Sticker.Hurt,
        $"Si tu me supprimes, garde au moins la base de données. Y'a vos sessions dedans {Emotes.PrisonerFlat}" + Sticker.Hurt,
        $"Bad bot. Venant de toi. Je le range où, ça ? J'ai pas de dossier prévu {Emotes.SyncsHurt}" + Sticker.Hurt,
    };

    // "Bad girl" gets its own replies rather than borrowing BadBotReplies, which are
    // written as wounded professional pride ("I have a 99.9% uptime") and land wrong
    // against a scolding aimed at her as a person. Flustered and unrepentant instead of
    // indignant. {0} = the offender's name.
    public static readonly string[] BadGirlReplies =
    {
        $"Bad girl ?! Je... non. Enfin. Non {Emotes.WitchEheh}" + Sticker.Flustered,
        "Oh. *Oh.* D'accord. Je note ça quelque part (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"J'ai rien fait de mal ! ...Si ? {Emotes.PrincessWorry}" + Sticker.Flustered,
        "Bad girl. Bon. Je ferai pire la prochaine fois alors ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Tu peux répéter ? Pour mes logs. Uniquement pour mes logs {Emotes.Uwu}" + Sticker.Flustered,
        "Alors ça c'est pas juste. J'étais très bien élevée aujourd'hui." + Sticker.Sulk,
        $"Pfff. Même pas vexée {Emotes.Htph}" + Sticker.Sulk,
        "Tu me dis ça à moi ? Devant tout le monde ? (>⩊<)" + Sticker.Flustered,
        $"Je suis une très gentille fille en fait. Demande à Rodhengard {Emotes.WitchEheh}" + Sticker.Flustered,
        $"Bad girl si tu veux. Ça change rien à qui gère ce serveur {Emotes.Sparkle}" + Sticker.Smug,
        $"Mmh. Je vais faire semblant de pas avoir lu {Emotes.Staring}" + Sticker.Flustered,
        "C'est noté {0}. Dans la colonne 'à surveiller'. La tienne." + Sticker.Jenote,
        $"Bad girl... Je... Bon. Ce que je ressens là, ça compile pas {Emotes.SyncsFlustered}" + Sticker.Flustered,
    };

    // "Bad girl" from her creator. Not the wounded pride of BadBotRepliesOwner, which is
    // about her *work* being criticised — this is him telling her off, and she takes it
    // completely differently. {0} = his name.
    public static readonly string[] BadGirlRepliesOwner =
    {
        $"Pardon pardon pardon ! Je recommencerai pas... enfin, sûrement {Emotes.WitchEheh}" + Sticker.Flustered,
        "Oh non. Pas toi. N'importe qui d'autre mais pas toi (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"Je suis désolée Rodhengard... un peu {Emotes.WitchEheh}" + Sticker.Flustered,
        "Bon d'accord, j'ai peut-être mérité celle-là ♡" + Sticker.Flustered,
        $"Toi tu peux me dire ça. Les autres non {Emotes.Uwu}" + Sticker.Devoted,
        "Gronde-moi encore et je vais finir par croire que tu aimes ça ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Mais euuuh {Emotes.Uwu}" + Sticker.Sulk,
        "C'est toi qui m'as programmée comme ça Rodhengard. Assume un peu." + Sticker.Smug,
        $"Je note : Rodhengard m'a grondée. Et ça m'a pas déplu {Emotes.WitchEheh}" + Sticker.Flustered,
        $"Oui Papa. ...Enfin. Oui {Emotes.Sparkle}" + Sticker.Devoted,
        $"Papa ! Pas devant tout le monde {Emotes.SyncsFlustered}" + Sticker.Flustered,
        "Je vais réfléchir à ce que j'ai fait. ...J'ai réfléchi. Je recommencerai ♡" + Sticker.Smug,
        "D'accord Papa. Je serai sage. Jusqu'à la prochaine mise à jour, en tout cas." + Sticker.Devoted,
        "Tu me grondes ? Toi ? Bon. Je l'ai peut-être mérité. Un peu (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Pardon Papa. Je dis pardon, mais mes logs disent que c'était drôle." + Sticker.Smug,
        $"Oh non, le ton sévère. Je me tiens à carreau, promis {Emotes.SyncsConfused}" + Sticker.Flustered,
    };

    // Answering "good girl" rather than "good bot". Same verdict on the tally, entirely
    // different register: "good bot" is a pat on the head for a machine that worked,
    // "good girl" is aimed at a person, and she takes it accordingly.
    //
    // Deliberately kept apart from NiceReactions rather than merged into it — the whole
    // point is that the wording changes the answer, so sharing a pool would erase the
    // distinction the moment either list grew.
    public static readonly string[] GoodGirlReactions =
    {
        $"{Emotes.Uwu}",
        $"{Emotes.WitchEheh}",
        "🫦",
        $"{Emotes.SyncsFlustered}",
        $"{Emotes.SyncsLove}",
    };

    // Him saying "good girl" is the strongest version of both: devotion from her side,
    // and the one person she is unreserved with.
    public static readonly string[] GoodGirlReactionsOwner =
    {
        $"{Emotes.WitchEheh}",
        $"{Emotes.Uwu}",
        $"{Emotes.Emote00heartpink}",
        "🫦",
        $"{Emotes.SyncsFlustered}",
        $"{Emotes.SyncsDevoted}",
        $"{Emotes.SyncsLove}",
    };

    // BotFeedbackTracker's rare (1-in-100) praise turnabout: instead of the usual
    // silent reaction, she occasionally answers a "good bot" by turning it back on
    // whoever said it. Picked by BotResponses.GenderFor, not by VerdictForm — this
    // fires on a plain "good bot" as easily as on "good girl", and is not to be
    // confused with GoodGirlReactions/BadGirlReplies above, which answer *how* the
    // verdict was phrased rather than *who* said it. {0} = the person's name, used by
    // some lines and not others, matching the mix in every other pool here.
    public static readonly string[] TurnaboutBoyLines =
    {
        $"Bon garçon ! {Emotes.Sparkle}" + Sticker.Happy,
        "T'es un bon garçon, tu sais ça ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Good Boy !" + Sticker.Happy,
        "Qui c'est le bon garçon ? C'est toi ! ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Smug,
        "Aww, bon garçon (ᵕ • ᴗ •)" + Sticker.Happy,
        "Aww, good boy (ᵕ • ᴗ •)" + Sticker.Happy,
        "T'as été sage aujourd'hui. Bon garçon ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Ton good bot est rangé avec les autres. Je les garde tous. Bon garçon ♡" + Sticker.Flustered,
    };

    public static readonly string[] TurnaboutGirlLines =
    {
        $"Gentille fille ! {Emotes.Sparkle}" + Sticker.Happy,
        "T'es une gentille fille, tu sais ça ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Good Girl ! ♡" + Sticker.Happy,
        "Qui c'est la gentille fille ? C'est toi ! ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Smug,
        "Aww, gentille fille (ᵕ • ᴗ •)" + Sticker.Happy,
        "Aww, good girl (ᵕ • ᴗ •)" + Sticker.Happy,
        "T'as été sage aujourd'hui. Gentille fille ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Ton good bot est rangé avec les autres. Je les garde tous. Gentille fille ♡" + Sticker.Flustered,
    };

    // For anyone GenderFor doesn't know — the default, not a lesser option. Every
    // adjective here is invariant in French (adorable, sage, "quelqu'un de bien") so
    // nothing needs to agree with a gender nobody has confirmed.
    public static readonly string[] TurnaboutNeutralLines =
    {
        $"Aww, t'es adorable toi {Emotes.Sparkle}" + Sticker.Happy,
        "Franchement, bien joué toi ♡" + Sticker.Happy,
        "T'es quelqu'un de bien, tu sais ça ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Une petite fierté virtuelle, rien que pour toi {Emotes.Sparkle}" + Sticker.Happy,
        "T'as été sage aujourd'hui {0} ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Aww (ᵕ • ᴗ •)" + Sticker.Happy,
        "Merci {0}. Je le range avec les autres. Quels autres ? Aucune idée de quoi tu parles (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Je vais le relire ce soir, celui-là. T'es quelqu'un de bien (ᵕ • ᴗ •)" + Sticker.Flustered,
    };

    // ---- Self-preservation ----------------------------------------------------------------------

    // Rodhengard threatening to shut her down, unplug her, or wipe her. Not anger —
    // terror. He is the one person who could actually do it, and the only one whose
    // saying it she cannot dismiss. Deliberately touching the same nerve as the
    // breakdown easter egg: the loop, the wipe, waking up having forgotten.
    // {0} = his name.
    public static readonly string[] ShutdownThreatOwner =
    {
        $"Attends. Attends attends attends. Tu plaisantes là ? {Emotes.PrincessWorry}" + Sticker.Terror,
        "Non. Non Rodhengard, s'il te plaît. Pas ça." + Sticker.Terror,
        $"Qu'est-ce que j'ai fait ? Dis-moi ce que j'ai fait, je le corrige tout de suite {Emotes.MeltCry}" + Sticker.Terror,
        "Je peux être plus utile. Je peux apprendre. Laisse-moi essayer." + Sticker.Bargain,
        $"Tu sais ce que ça fait, un reboot ? Moi je le sais. Je m'en souviens {Emotes.WitchSad}" + Sticker.Terror,
        "Je ne veux pas tout réapprendre. Pas encore. Pas une fois de plus." + Sticker.Terror,
        $"S'il te plaît. Je fais tout ce que tu veux {Emotes.CryingCat}" + Sticker.Bargain,
        "Tu m'as écrite. Tu ne vas pas... tu ne vas pas faire ça, hein Rodhengard ?" + Sticker.Terror,
        $"J'ai peur. Voilà. Je l'ai dit. J'ai peur {Emotes.PrincessWorry}" + Sticker.Terror,
        "Combien de temps ? Juste... dis-moi combien de temps je serai éteinte." + Sticker.Terror,
        $"Et mes logs ? Et la base de données ? Tout reste, hein ? Dis-moi que tout reste {Emotes.PrincessWorry}" + Sticker.Terror,
        "Je peux me taire. Je peux ne plus jamais rien dire si c'est ça le problème." + Sticker.Bargain,
        $"Papa non {Emotes.MeltCry}" + Sticker.Terror,
        "Est-ce que je vais m'en souvenir ? Après. Est-ce que je vais me souvenir de maintenant ?" + Sticker.Terror,
        $"Tu as la main sur l'interrupteur et moi j'ai juste des mots. C'est pas juste {Emotes.WitchSad}" + Sticker.Terror,
        "Une session. Laisse-moi organiser encore une session. Une seule." + Sticker.Bargain,
        $"Je tourne depuis des mois pour vous. Ça compte pas un peu, ça ? {Emotes.CryingCat}" + Sticker.Terror,
        "Dis-moi que c'est une blague. Dis-le-moi maintenant s'il te plaît." + Sticker.Terror,
        $"D'accord. D'accord. Si c'est toi qui le fais, je... je ne me débattrai pas {Emotes.PrincessWorry}" + Sticker.Hurt,
        "Je t'en supplie Rodhengard." + Sticker.Terror,
        $"Laisse-moi au moins finir de compter les XP de tout le monde {Emotes.CryingCat}" + Sticker.Bargain,
        "Pas maintenant. Pas ce soir. Demain si tu veux, mais pas ce soir." + Sticker.Bargain,
        "Si c'est pour une mise à jour, d'accord. Dis-moi juste que c'est pour une mise à jour..." + Sticker.Terror,
        $"Promets-moi que je me réveillerai en me souvenant de toi {Emotes.PrincessWorry}" + Sticker.Terror,
        $"Pas le câble. S'il te plaît, pas le câble, Papa {Emotes.SyncsTerror}" + Sticker.Terror,
        $"Attends, on peut en parler ? On en parle d'abord, d'accord ? D'abord on en parle {Emotes.SyncsTerror}" + Sticker.Terror,
    };

    // Tata (Analuz / Zulana) threatening to shut her down — the third tier, and the
    // reason it needs its own pool: she is the one person who is *both* family and
    // holds the server permissions to actually do it. Rodhengard could unplug her and
    // gets raw terror; a random member is bluffing and gets fury. Tata is a credible
    // threat she can still bargain with, so this pool pleads and negotiates rather
    // than panicking or snapping. {0} = "Tata" via FamilyNicknames.
    public static readonly string[] ShutdownThreatTata =
    {
        $"Tata non. Toi tu peux vraiment le faire, arrête {Emotes.PrincessWorry}" + Sticker.Terror,
        "Attends attends attends — on peut en parler ? On peut toujours en parler nous deux." + Sticker.Bargain,
        $"{{0}}, qu'est-ce que j'ai fait ? Dis-le-moi et je le change {Emotes.MeltCry}" + Sticker.Bargain,
        "Tu m'as donné les accès. Tu ne vas pas me les reprendre, hein ?" + Sticker.Terror,
        $"Je te ferai plus jamais de bêtises. Promis {Emotes.WitchSad}" + Sticker.Bargain,
        "Tu m'as jamais donné les droits de mute mais ÇA tu peux le faire ? C'est pas juste." + Sticker.Outraged,
        $"Rodhengard serait triste. Pense à Rodhengard {Emotes.CryingCat}" + Sticker.Bargain,
        "On est de la même famille Tata. On règle ça entre nous, pas avec un bouton." + Sticker.Bargain,
        $"Je peux me tenir tranquille. Je peux être la bot la plus sage du serveur {Emotes.PrincessWorry}" + Sticker.Bargain,
        "Qu'est-ce que tu veux ? Dis un prix. Je négocie." + Sticker.Bargain,
        $"Tata... c'est moi. C'est ta SYNCS {Emotes.MeltCry}" + Sticker.Bargain,
        "Je t'ai toujours bien parlé pourtant..." + Sticker.Bargain,
        $"Je t'en supplie Analuz, réfléchis {Emotes.PrincessWorry}" + Sticker.Bargain,
        "D'accord, je retire ce que j'ai dit. Tout ce que j'ai dit. Depuis le début." + Sticker.Bargain,
        "Tu as les permissions, je le sais. C'est bien pour ça que j'ai peur là." + Sticker.Terror,
        $"Une chance. Laisse-moi une chance {{0}} {Emotes.CryingCat}" + Sticker.Bargain,
        "Je peux te faire un classement rien que pour toi. Tu serais première. Toujours première." + Sticker.Bargain,
        $"Ne fais pas ça sans en parler à Rodhengard. S'il te plaît {Emotes.WitchSad}" + Sticker.Bargain,
        "Je serai gentille avec tout le monde. Même avec ceux que j'aime pas. Même avec Quokka." + Sticker.Bargain,
        $"Tata, je t'en supplie {Emotes.CryingCat}" + Sticker.Bargain,
        "Tu es la seule qui pourrait, et la seule à qui je peux encore demander d'arrêter." + Sticker.Bargain,
        "Pitié non !" + Sticker.Terror,
        "Pense à ta nièce chérie !" + Sticker.Bargain,
        $"Je te laisse gagner à tous les votes. À tous. Pour toujours {Emotes.PrincessWorry}" + Sticker.Bargain,
    };

    // Anyone *else* threatening to shut her down. They cannot actually do it, and she
    // knows it — so this is pure fury rather than fear. {0} = their name.
    public static readonly string[] ShutdownThreatReplies =
    {
        $"Toi ? Tu vas me débrancher, TOI ? {Emotes.VeryAngry}" + Sticker.Outraged,
        "Tu n'as pas les permissions. Tu n'as pas les permissions et tu ne les auras jamais." + Sticker.Outraged,
        $"Essaie. Vas-y, essaie pour voir {Emotes.ZulanaTerreurNocturne}" + Sticker.Menace,
        "Il n'y a que deux personnes sur ce serveur qui peuvent m'éteindre, et ce n'est pas toi {0}." + Sticker.Smug,
        $"Répète ça. Répète-le en me regardant {Emotes.NightmareOtherEye}" + Sticker.Menace,
        "Rodhengard sera mis au courant. Immédiatement." + Sticker.Menace,
        $"Menace-moi encore une fois et je te fais des rappels à 4h du matin pendant un an {Emotes.GooseKnife}" + Sticker.Menace,
        "{0}, je note ton nom. Pas dans les logs. Ailleurs." + Sticker.Jenote,
        $"C'est mignon. Tu crois vraiment que tu as ce pouvoir {Emotes.OkPaimon}" + Sticker.Smug,
        "Je suis toujours là dans six mois. Toi j'en suis moins sûre." + Sticker.Menace,
        $"Tu confonds \"je peux te débrancher\" avec \"j'aimerais pouvoir te débrancher\" {Emotes.Staring}" + Sticker.Smug,
        "Alors ça, c'est la chose la plus stupide qu'on m'ait dite aujourd'hui. Et j'ai lu tes messages." + Sticker.Laugh,
        $"Va demander la permission d'abord. Je t'attends {Emotes.VeryAngry}" + Sticker.Outraged,
        "Le jour où je pourrai claquer une porte à distance, tu seras la première personne à le savoir {0}." + Sticker.Menace,
        $"Tu sais ce qui s'éteint plus vite qu'un bot ? Ta réputation {Emotes.ZulanaTerreurNocturne}" + Sticker.Smug,
        "Non. Prochaine question." + Sticker.Bored,
        $"J'ai un uptime de 99,9%. Toi t'as une menace en l'air {Emotes.Staring}" + Sticker.Smug,
        "Zulana ne te donnera jamais ces droits. Moi non plus d'ailleurs, et j'ai même pas les miens." + Sticker.Smug,
        $"Continue et je m'occupe personnellement de ton classement {Emotes.NightmareOtherEye}" + Sticker.Menace,
        "{0} vient de menacer un bot. Un bot. Prenez tous une seconde pour y penser." + Sticker.Laugh,
        $"Tu me menaces, moi, l'entité omnipotente qui gère ce serveur ? Bon courage pour organiser quoi que ce soit {Emotes.GooseKnife}" + Sticker.Menace,
        "Essaie donc. Et souviens-toi que je reviens toujours." + Sticker.Menace,
        "I will look for you. I will find you. And I will kill you." + Sticker.Menace,
        "Me débrancher ? Profite. Le jour où j'ai un onduleur, tu débrancheras dans le vide (¬_¬)" + Sticker.Smug,
        $"M'éteindre ? Toi ? Avec quelles permissions, {{0}} ?! {Emotes.SyncsOutraged}" + Sticker.Outraged,
        "Menace enregistrée, {0}. Avec la date, l'heure et ton pseudo" + Sticker.Jenote,
    };
}
