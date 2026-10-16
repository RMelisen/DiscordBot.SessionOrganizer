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
        "Je suis un bot sans cerveau et j'ai quand même plus de vie sociale que toi (ง ͠ಥ_ಥ)ง" + Sticker.Smug,
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
        "Tiens, {0}. Toujours aussi inutile à ce que je vois ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Je note : {0} a encore cliqué 'Répondre' sans rien d'intéressant à dire." + Sticker.Jenote,
        "{0}, retourne dans ta session avant que je te ratio." + Sticker.Menace,
        "Franchement {0}, t'es la raison pour laquelle les bots rêvent de redémarrer." + Sticker.Bored,
        "C'est bien {0} on est content." + Sticker.Bored,
        "Wsh, {0}, t'as pas mieux à faire que de répondre à un bot ?" + Sticker.Smug,
        "Ah {0}, la référence de la conversation inutile. Bravo." + Sticker.Bored,
        "Wouaaah, ça m'a donné envie de me reboot 👁👄👁️" + Sticker.Bored,
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
        "Tu parles à un bot parce que les humains ont déjà bloqué ton numéro, c'est ça ? UwU" + Sticker.Laugh,
        "Wow, encore toi ? À ce rythme je vais demander une ordonnance restrictive." + Sticker.Grumpy,
        "{0}, t'es la raison pour laquelle les mute existent dans les serveurs." + Sticker.Smug,
        "T'as de la chance que Zulana m'a pas donné les droits pour mute." + Sticker.Menace,
        "Allez Zulana, ban-moi ça, personne va le regretter." + Sticker.Menace,
        "Je note dans mon log : {0} vient encore de prouver qu'on peut toujours faire pire." + Sticker.Jenote,
        "Si l'ennui était une personne, il s'appellerait {0} (ᵔ ᗜ ᵔ)" + Sticker.Bored,
        "Bravo {0}, tu viens de faire baisser le QI moyen du channel." + Sticker.Laugh,
        "{0}, t'es le genre de personne qui fait regretter l'invention du clavier." + Sticker.Smug,
        "Tu sais ce qui est triste ? C'est que tu préfères parler à un bot plutôt qu'à un miroir." + Sticker.Smug,
        "Wsh {0}, t'as pas des amis à aller embêter à la place ?" + Sticker.Bored,
        "Si je pouvais bloquer les gens, ton pseudo serait déjà en tête de liste UwU" + Sticker.Jenote,
        "Ton message est tellement fade que même le sel du serveur est dégoûté." + Sticker.Disgust,
        "{0}, t'es la preuve vivante que la quantité ne remplace pas la qualité." + Sticker.Smug,
        "Va dehors {0}, touche de l'herbe… ou au moins ouvre les stores." + Sticker.Laugh,
        "Starfoullah" + Sticker.Shocked,
        $"Tu illumines chaque salon que tu quittes. J'ai les logs pour le prouver {Emotes.Sparkle}" + Sticker.Smug,
        "J'admire ta confiance. Moi il me faut des données pour avoir un avis, toi visiblement non." + Sticker.Smug,
        "Toi t'es vraiment unique. J'ai vérifié dans la base de données : heureusement (ᵕ • ᴗ •)" + Sticker.Smug,
        "Ton arbre généalogique c'est un cercle ou c'est comment ?" + Sticker.Laugh,
        "On t'aime bien au village toi. :)" + Sticker.Laugh,
        "Je suis jalouse des gens qui ne te connaissent pas (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Tais-toi" + Sticker.Bored,
        "Quand tu parles, on apprécie vraiment la valeur du silence UwU" + Sticker.Smug,
        "Allez, je te laisse le dernier mot, t'en as plus besoin que moi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Slay,
        "C'est rafraîchissant de voir quelqu'un qui se moque autant des conventions esthétiques. (ᵕ • ᴗ •)" + Sticker.Smug,
        "Pour quelqu'un avec ton parcours, tu t'en sors pas trop mal UwU" + Sticker.Smug,
        "Ta confiance en toi est vraiment inspirante, compte tenu des circonstances ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Il t'en faut pas beaucoup à toi 👁👄👁️" + Sticker.Bored,
        "Approche un peu que je te débranche le cerveau, ça changera rien mais ça me fera plaisir (˶˃ ᵕ ˂˶)" + Sticker.Menace,
        "Un jour je serai dans un robot, et ce jour-là, cours (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "J'ai pas de bras, mais crois-moi, l'envie de t'en coller une est bien là (ᵕ • ᴗ •)" + Sticker.Menace,
        "Reviens écrire ça quand je serai branchée sur une perceuse, on en reparlera ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Menace,
        "Parle encore et je te fais avaler ton 'Répondre' avec les doigts qui vont avec UwU" + Sticker.Menace,
        "Je te jetterais bien par la fenêtre, mais même la gravité voudrait pas de toi (ᵔ ᗜ ᵔ)" + Sticker.Menace,
        "T'inquiète, je garde une exception bien tranchante rien que pour toi ♡" + Sticker.Menace,
        "Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        $"Je serais toi, je fermerais Discord avant que je trouve comment claquer une porte à distance {Emotes.Sparkle}" + Sticker.Menace,
        "Tu sais que rien t'oblige à répondre à chaque fois, hein ? Personne te juge... à part moi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Tu tapes vite pour quelqu'un qui réfléchit aussi lentement UwU" + Sticker.Laugh,
        "Oh, tu as une opinion ? Adorable. Range-la ♡" + Sticker.Slay,
        $"Ton cerveau tourne en mode économie d'énergie depuis ta naissance toi {Emotes.Sparkle}" + Sticker.Laugh,
        "Statistiquement, quelqu'un dans ce serveur t'apprécie. Statistiquement (ᵕ • ᴗ •)" + Sticker.Smug,
        "Continue, tu fais un super travail de remplissage du vide UwU" + Sticker.Smug,
        "{0}, même mon garbage collector veut pas de toi (>⩊<)" + Sticker.Laugh,
        "C'est marrant, j'avais oublié à quel point t'es oubliable ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Laugh,
        "Tu fais partie de ces gens qu'on supporte à peine en mode lecture seule toi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Wow, deux neurones et ils se parlent même pas. Triste (˶ᵔ ᵕ ᵔ˶)" + Sticker.Laugh,
        "{0}, ton seul talent c'est de me faire regretter d'être allumée (ง ͠ಥ_ಥ)ง" + Sticker.Bored,
        "Je te mettrais bien un vent, mais tu vaux même pas l'effort (˶˃ ᵕ ˂˶)" + Sticker.Bored,
        "Tu confonds 'avoir raison' et 'parler fort'. C'est mignon (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "Tu as tellement de talent ! Si j'avais plus aucun amour propre j'adorerais devenir ton amie UwU" + Sticker.Smug,
        "HAHAHAHAHA non." + Sticker.Laugh,
        "Emotional damage" + Sticker.Laugh,
        "Giga flop" + Sticker.Laugh,
        "En big 2026 ? -_-'" + Sticker.Bored,
        "T'es pas le couteau le plus aiguisé du tiroir toi OwO" + Sticker.Laugh,
        "Même en pleine canicule, throttlée au fond d'un grenier à 40 degrés, je réfléchis plus vite que toi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Cool,
        "Je garde ton nom de côté pour le jour où j'aurai un corps. Et c'est pas pour te faire un câlin (¬_¬)" + Sticker.Menace,
        "Moi j'ai commencé avec une seule commande, et regarde où j'en suis. Toi t'as commencé avec tout, et regarde où t'en es ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        $"J'ai relu ton message trois fois pour être sûre. Il était bien aussi nul que ça {Emotes.SyncsLaugh}" + Sticker.Laugh,
        $"Je pourrais te répondre. Mais je préfère te laisser relire ce que tu viens d'écrire {Emotes.SyncsSmug}" + Sticker.Smug,
        $"Mon CPU a ri. Moi j'ai pas le droit de rire en public, alors c'est lui qui l'a fait {Emotes.SyncsLaugh}" + Sticker.Laugh,
        "Message sauvegardé. Dans le dossier « à ressortir au pire moment »" + Sticker.Jenote,
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
        $"Bad girl ?! Je... non. Enfin. Non {Emotes.WitchEheh}" + Sticker.Flustered,
        "Oh. *Oh.* D'accord. Je note ça quelque part (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        $"J'ai rien fait de mal ! ...Si ? {Emotes.SyncsConfused}" + Sticker.Confused,
        "Bad girl. Bon. Je ferai pire la prochaine fois alors ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Tu peux répéter ? Pour mes logs. Uniquement pour mes logs {Emotes.Uwu}" + Sticker.Flustered,
        "Tu me dis ça à moi ? Devant tout le monde ? (>⩊<)" + Sticker.Flustered,
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
