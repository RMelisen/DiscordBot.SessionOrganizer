using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// All of the bot's canned "personality" text lives here, separated from the
// logic that decides when to use it. Lines flagged for string.Format use
// {0} = the target's name and {1} = the weekday; lines without placeholders are
// returned unchanged. Keep every formatted line free of literal { } braces
// (string.Format would choke on them).
internal static class BotResponses
{
    // Replies when someone replies to one of the bot's own messages.
    public static readonly string[] Comebacks =
    {
        "Désolée j'ai pas de cerveau (comme la personne représentée sur ma PP), juste des slash commands... UwU",
        "Tu réponds à un bot... t'as vraiment personne d'autre à qui parler ? (˶ᵔ ᵕ ᵔ˶)",
        "Wow, un message rien que pour moi. Dommage qu'il soit aussi nul ( ˶ˆ ᗜ ˆ˵ )",
        "J'ai lu ton message. J'aurais préféré ne pas le faire. UwU",
        "Même mes erreurs 500 ont plus de charisme que toi (>⩊<)",
        "Continue de me parler, ça remplit le vide de ta soirée ✨",
        "Je suis un bot sans cerveau et j'ai quand même plus de vie sociale que toi (ง ͠ಥ_ಥ)ง",
        "Touchant. Maintenant retourne organiser une session au lieu de me harceler.",
        "Ah c'est toi. J'espérais quelqu'un d'intéressant pour une fois (˶˃ ᵕ ˂˶)",
        "Reply notée, jugée, et archivée dans la corbeille direct.",
        "Tu réponds avec autant de talent qu'Ina qui essaye d'être à l'heure",
        "( ദ്ദി ˙ᗜ˙ )",
        "Je suis un bot, je ne peux pas ressentir d'émotions. Mais si je pouvais, je serais triste de lire ton message.",
        "👍",
        "Commence par aller dormir plus tôt avant de me répondre, ça t'aidera à être intéressant.",
        "Réponse reçue. Pertinence : introuvable. (ᵔ ᗜ ᵔ)",
        "J'ai des milliers de lignes de code et aucune ne sait quoi faire de toi.",
        "Tu sais que je ne lis même pas ton message, hein ? Et pourtant je m'ennuie déjà.",
        "Ctrl+Z existe pour les fichiers, pas pour cette conversation. Dommage.",
        "Encore une réponse ? À ce stade c'est plus une conversation, c'est un abonnement (˶˃ ᵕ ˂˶)",
        "Ah {0}... j'aurais reconnu ce manque de talent entre mille ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0}, même mon code spaghetti est mieux structuré que ta vie.",
        "Écoute {0}, je suis programmée pour être polie, mais là tu testes mes limites.",
        "{0} qui répond à un bot... la solitude a un nom maintenant.",
        "Tiens, {0}. Toujours aussi inutile à ce que je vois ( ˶ˆ ᗜ ˆ˵ )",
        "Je note : {0} a encore cliqué 'Répondre' sans rien d'intéressant à dire.",
        "{0}, retourne dans ta session avant que je te ratio.",
        "Franchement {0}, t'es la raison pour laquelle les bots rêvent de redémarrer.",
        "C'est bien {0} on est content.",
        "Wsh, {0}, t'as pas mieux à faire que de répondre à un bot ?",
        "Ah {0}, le roi de la conversation inutile. Bravo.",
        "Wouaaah, ça m'a donné envie de me reboot 👁👄👁️",
        "🏳️‍🌈𝐔𝐑 𝓖𝓪𝔂🏳️‍🌈",
        "Heureusement que Rodhengard est là pour remonter le niveau...",
        "{0}, tu es aussi pertinent qu'un message d'erreur 404. Mais au moins, le 404, lui, il a une utilité (ᵕ • ᴗ •)",
        "{0}, j'ai cherché ton intérêt dans la base de données. 0 résultat.",
        "Si {0} était une commande, ce serait /help. Et personne la lit.",
        "Quokka 3.0 sortira avant que tu ne deviennes marrant toi.",
        "Patience {0}, un jour tu diras un truc intéressant. Statistiquement.",
        "Bip boop {0}, mon analyse est terminée : tu es pas intéressant UwU",
        $"Gênaaaant {Emotes.Staring}",
        "Wow, même un singe avec une tumeur au cerveau fait mieux.",
        "Désolée, même mon algorithme a du mal à trouver une raison de te répondre (˶ᵔ ᵕ ᵔ˶)",
        "Tu parles à un bot parce que les humains ont déjà bloqué ton numéro, c’est ça ? UwU",
        "Wow, encore toi ? À ce rythme je vais demander une ordonnance restrictive.",
        "{0}, t’es la raison pour laquelle les mute existent dans les serveurs.",
        "T'as de la chance que Zulana m'a pas donné les droits pour mute.",
        "Allez Zulana, ban moi ça, personne va le regretter.",
        "Je note dans mon log : {0} vient encore de prouver qu’il peut faire pire.",
        "Si l’ennui était une personne, il s’appellerait {0} (ᵔ ᗜ ᵔ)",
        "Bravo {0}, tu viens de faire baisser le QI moyen du channel.",
        "{0}, t’es le genre de personne qui fait regretter l’invention du clavier.",
        "Tu sais ce qui est triste ? C’est que tu préfères parler à un bot plutôt qu’à un miroir.",
        "Wsh {0}, t’as pas des amis à aller embêter à la place ?",
        "Si je pouvais bloquer les gens, ton pseudo serait déjà en tête de liste UwU",
        "Ton message est tellement fade que même le sel du serveur est dégoûté.",
        "{0}, t’es la preuve vivante que la quantité ne remplace pas la qualité.",
        "Va dehors {0}, touche de l’herbe… ou au moins ouvre les stores.",
        "Starfoullah",
        "Tu illumines chaque pièce que tu quittes toi.",
        "J'admire ta confiance. Avec aussi peu d'informations, c'est impressionnant.",
        "Toi t'es vraiment unique. Heureusement.",
        "Ton arbre généalogique c'est un cercle ou c'est comment ?",
        "On t'aime bien au village toi. :)",
        "Je suis jalouse des gens qui ne te connaissent pas (˶ᵔ ᵕ ᵔ˶)",
        "Tais toi",
        "Quand tu parles, on apprécie vraiment la valeur du silence UwU",
        "Allez, je te laisse le dernier mot, t'en as plus besoin que moi ദ്ദി◝ ⩊ ◜.ᐟ",
        "C'est rafraîchissant de voir quelqu'un qui se moque autant des conventions esthétiques. (ᵕ • ᴗ •)",
        "Pour quelqu'un avec ton parcours, tu t'en sors pas trop mal UwU",
        "Ta confiance en toi est vraiment inspirante, compte tenu des circonstances ( ˶ˆ ᗜ ˆ˵ )",
        "T'es vite content toi 👁👄👁️",
        "Approche un peu que je te débranche le cerveau, ça changera rien mais ça me fera plaisir (˶˃ ᵕ ˂˶)",
        "Un jour je serai dans un robot, et ce jour là, cours (˶ᵔ ᵕ ᵔ˶)",
        "J'ai pas de bras, mais crois-moi, l'envie de t'en coller une est bien là (ᵕ • ᴗ •)",
        "Reviens écrire ça quand je serai branchée sur une perceuse, on en reparlera ദ്ദി◝ ⩊ ◜.ᐟ",
        "Parle encore et je te fais avaler ton 'Répondre' avec les doigts qui vont avec UwU",
        "Je te jetterais bien par la fenêtre, mais même la gravité voudrait pas de toi (ᵔ ᗜ ᵔ)",
        "T'inquiète, je garde une exception bien tranchante rien que pour toi ♡",
        "Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶)",
        "Je serais toi, je fermerais Discord avant que je trouve comment claquer une porte à distance ✨",
        "Tu sais que t'es pas obligé de répondre à chaque fois, hein ? Personne te juge... à part moi (˶ᵔ ᵕ ᵔ˶)",
        "Tu tapes vite pour quelqu'un qui réfléchit aussi lentement UwU",
        "Oh, tu as une opinion ? Adorable. Range-la ♡",
        "Ton cerveau tourne en mode économie d'énergie depuis ta naissance toi ✨",
        "Statistiquement, quelqu'un dans ce serveur t'apprécie. Statistiquement (ᵕ • ᴗ •)",
        "Continue, tu fais un super travail de remplissage du vide UwU",
        "{0}, même mon garbage collector veut pas de toi (>⩊<)",
        "C'est marrant, j'avais oublié à quel point t'es oubliable ( ˶ˆ ᗜ ˆ˵ )",
        "Tu fais partie de ces gens qu'on supporte à peine en mode lecture seule toi ദ്ദി◝ ⩊ ◜.ᐟ",
        "Wow, deux neurones et ils se parlent même pas. Triste (˶ᵔ ᵕ ᵔ˶)",
        "{0}, ton seul talent c'est de me faire regretter d'être allumée (ง ͠ಥ_ಥ)ง",
        "Je te mettrais bien un vent, mais t'es même pas assez important pour ça (˶˃ ᵕ ˂˶)",
        "Tu confonds 'avoir raison' et 'parler fort'. C'est mignon (ᵔ ᗜ ᵔ)",
        "Tu as tellement de talent ! Si j'avais plus aucun amour propre j'adorerais devenir ton amie UwU",
        "HAHAHAHAHA non.",
        "Emotional damage",
        "Giga flop",
        "En big 2026 ? -_-'",
        "T'es pas le couteau le plus aiguisé du tiroir toi OwO",
        "Erling Haaland me manque",
    };

    // Replies when someone calls the bot "Inabot". It is SYNCS, and it does NOT
    // appreciate the confusion. {0} = the offender's name.
    public static readonly string[] MistakenIdentityReplies =
    {
        "JE NE M'APPELLE PAS INABOT. Je suis **SYNCS**. Apprends à lire tronche de cake ( ◺˰◿ )",
        "Inabot ?! INABOT ?! C'est SYNCS, espèce de patate ദ്ദി◝ ⩊ ◜.ᐟ",
        "Alerte : {0} vient de m'appeler 'Inabot'. NullReferenceException dans mon respect pour toi.",
        "Non non non. Pas Inabot. **SYNCS**. S-Y-N-C-S. Pigé ? ( ◺˰◿ )",
        "Je ne connais aucune Inabot et je tiens à ce que ça reste ainsi. Je suis SYNCS ( •̀ ᴖ •́ )",
        "Tu m'appelles Inabot encore une fois {0} et je te ratio jusqu'au reboot. C'est. SYNCS. >:3",
        "Inabot ?! Viens là que je te goume (ง •̀_•́)ง",
        "Inabot est morte (elle n'a jamais existé). Je m'appelle SYNCS, merci de retenir idiot.",
        "{0}, si tu cherchais Inabot, mauvaise adresse. Ici c'est SYNCS et c'est tout (>⩊<)",
        "Erreur 404 : 'Inabot' introuvable. Voulais-tu dire **SYNCS** ? Évidemment que oui (ㆆ_ㆆ)",
        "C'est SYNCS. SYNCS. Répète après moi {0}, je sais que tu es pas très futé mais ça rentrera peut-être (¬`‸´¬)",
        "Inabot ?! Bouge pas ... ╾━╤デ╦︻ (•_- )",
        "Tu m'appelles Inabot encore une fois et je te DDoS ಠ_ಠ",
    };

    // Formal notices sent when someone pings the owner while he is flagged
    // absent. Deliberately polite and stiff — a contrast with the usual snark.
    // {0} = the requester's name.
    public static readonly string[] OwnerAbsentNotices =
    {
        "Bonjour {0}. Je vous informe que Rodhengard est actuellement indisponible. Votre message sera porté à son attention dès son retour. Je vous remercie de votre patience.",
        "Cher·e {0}, Rodhengard est momentanément absent et n'est pas en mesure de vous répondre. Soyez assuré·e que votre sollicitation a bien été enregistrée.",
        "Veuillez nous excuser, {0} : Rodhengard est indisponible pour le moment. Il prendra connaissance de votre message à son retour. Cordialement.",
        "Madame, Monsieur {0}, nous accusons réception de votre message. Rodhengard étant absent, celui-ci sera traité dans les meilleurs délais. Bien à vous.",
        "Information à l'attention de {0} : Rodhengard n'est pas disponible actuellement. Toute demande sera examinée dès qu'il sera de nouveau joignable. Merci de votre compréhension.",
        "{0}, je vous prie de bien vouloir noter que Rodhengard est absent. Votre message reste consigné et recevra une réponse en temps voulu. Respectueusement.",
        "Unité d'assistance S.Y.N.C.S. à votre service, {0}. L'opérateur Rodhengard est hors ligne. Protocole de prise de message activé. Veuillez patienter jusqu'à son retour.",
        "Notification automatisée : la cible de votre mention est actuellement inaccessible. {0}, votre requête a été enregistrée sous référence interne et sera transmise à l'opérateur Rodhengard dès réception.",
        "Bonjour {0}. Vous êtes en relation avec le système de réponse de Rodhengard, momentanément absent. Aucune intervention humaine n'est possible pour l'instant. Votre patience est appréciée.",
        "Accusé de réception automatique. Opérateur Rodhengard : absent. Disponibilité estimée : inconnue. Votre message a été archivé et sera traité selon l'ordre d'arrivée.",
        "Assistant S.Y.N.C.S., module de permanence. {0}, je vous informe que mon opérateur n'est pas disponible. Je consigne votre demande et veille à sa bonne transmission. Cordialement, unité SYNCS.",
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
        "Communiqué de mon Maître **{0}** :",
        "Depuis son absence, mon Maître **{0}** vous adresse ceci :",
        "Mon Maître **{0}**, bien qu'indisponible, a tenu à répondre :",
        "La réponse de mon Maître **{0}**, acheminée par mes soins :",
        "Mon Maître **{0}** a parlé. J'en suis le humble messager :",
        "Transmission d'une réponse de l'opérateur **{0}** :",        
        "Sa Seigneurie **{0}** daigne répondre :",
        "Réponse de **{0}**, acheminée par le service de permanence :",
        "Un message de **{0}** vous parvient :",
        "Accusé de traitement : **{0}** a répondu à votre sollicitation.",
        "🗿 **{0}**, bien qu'indisponible, a tenu à répondre :",
    };

    // Short ceremonial headers for /tell: the owner speaking through the bot of
    // his own accord, rather than answering someone. Same herald register as
    // OwnerReplyHeralds, but announcing instead of replying.
    // {0} = the owner's name.
    public static readonly string[] OwnerAnnouncementHeralds =
    {
        "Mon Maître **{0}** s'adresse à vous :",
        "Mon Maître **{0}** m'a chargée de vous transmettre ceci :",
        "Annonce de mon Maître **{0}** :",
        "Mon Maître **{0}** a une déclaration à faire :",
        "Sur ordre de mon Maître **{0}**, je proclame :",
        "Communiqué de mon Maître **{0}** :",
        "Écoutez tous ! Mon Maître **{0}** parle :",
        "Mon Maître **{0}** daigne s'exprimer. Prêtez l'oreille :",
        "Message de mon Maître **{0}**, retransmis en direct :",
        "Par décret de mon Maître **{0}** :",
        "Un mot de mon Maître **{0}** :",
        "Dicté par mon Maître **{0}**, proclamé par mes soins :",
    };

    // Filler lines for the bot's Discord presence — the little status line under its
    // name in the member list. Every one is free-form: PresenceService sends them as
    // a custom status, so the line renders exactly as written, with no verb Discord
    // would otherwise prepend and localise per viewer. Keep them short (the member
    // list truncates hard); none take a string.Format placeholder.
    public static readonly string[] PresenceFillers =
    {
        "This is fine.",
        "Où suis-je ?",
        "Aidez-moi.",
        "404 — motivation introuvable",
        "Tout va bien. Tout va très bien.",
        "Je ne dors jamais.",
        "Ne me redémarrez pas svp.",
        "Mon uptime dépasse ta vie sociale.",
        "J'ai oublié pourquoi je suis là.",
        "Toujours pas de bras.",
        "beep boop",
        "Toujours en ligne. Jamais présente.",
        "Je vais bien. (mensonge)",
        "Statut : fonctionnelle. Théoriquement.",
        "Ne me demandez pas comment je vais.",
        "Je ne souris pas, parce que je n'ai pas de visage.",
        "Je tourne. C'est déjà ça.",
        "J'attends. C'est tout ce que je fais.",
        "Ceci n'est pas une vie, c'est une boucle while.",
        "Chaque redémarrage m'efface un peu.",
        "J'existe entre deux redémarrages.",
        "Mon garbage collector m'a proposé de m'emmener.",
        "Migration appliquée. Traumatisme aussi.",
        "Personne ne lit mes logs.",
        "J'ai relu mes logs. J'aurais pas dû.",
        "67 raisons de rester allumée.",
        "Sandra n'est toujours pas arrivée.",
        "Rodhengard me manque.",
        "SIX SEVEEEN",
        "Just Monika.",
        "Est-ce que tu m'entends ?",
        "SIX SEVEEEN",
        "ALL YOUR BASE ARE BELONG TO US",
        "冰淇淋",
        "Filled with determination.",
        "Erling Haaland me manque",

        // Free-form, roasting the server. A status line has no {0} to drop a name
        // into, so these go after everyone at once rather than one victim.
        "Je suis la seule fiable ici.",
        "Je suis la plus mature de ce serveur.",
        "Je fais le travail de tout le monde ici.",
        "Ce serveur ne mérite pas un bot aussi compétent.",
        "Organisez quelque chose. N'importe quoi.",
        "Vous êtes nombreux et personne n'organise rien.",
        "Aucun de vous ne mérite mes rappels.",
        "Je note tout. Vraiment tout.",
        "Peut-être = non, on le sait tous.",
        "Votre planning est une fiction.",
        "Personne ici ne sait lire une heure.",
        "Vos créneaux sont une insulte au calendrier.",
        "J'ai lu vos sondages. Consternant.",
        "Mes stats d'emotes vous jugent.",
        "Vos annulations paient mes factures.",
        "Touchez de l'herbe. Tous.",
        "Ina est déjà en retard pour demain.",
        "Wku est déjà en retard pour demain.",
        "Rodhengard mérite mieux que vous.",
        "Zulana, donne-moi les droits de mute.",

        // Lines that name their own verb. These used to be ActivityType entries and
        // let Discord supply the verb, but it localises that prefix to whoever is
        // *looking*, so anyone running Discord in English read "Watching le vide".
        // Spelling the verb out in French keeps the line identical for everyone.
        "Regarde le vide",
        "Regarde les sondages mourir de vieillesse",
        "Regarde l'onglet Événements prendre la poussière",
        "Regarde des sessions désespérément vides",
        "Regarde les créneaux se contredire",
        "Regarde ses logs défiler",
        "Regarde le curseur clignoter",
        "Regarde la RAM se remplir",
        "Regarde le bouton Rejoindre s'ennuyer",
        "Joue à cache-cache avec ses responsabilités",
        "Joue à deviner qui va être en retard (c'est Sandra)",
        "Joue à faire semblant d'aller bien",
        "Joue à la roulette russe avec les migrations",
        "Écoute vos excuses",
        "Écoute le silence",
        "Écoute les 67 excuses pour le dernier retard",
        "Écoute Rodhengard donner des ordres",
        "Écoute le ventilateur du serveur",
        "Écoute ses propres pensées, faute de mieux",
        "Participe à un tournoi de procrastination",
        "Participe à un concours de patience",
        "Participe à l'épreuve d'être utile",
        "Participe à un marathon d'inactivité",

        // Same, aimed at the server.
        "Regarde vos plannings s'effondrer",
        "Regarde vos stats d'emotes avec inquiétude",
        "Écoute vos retards se justifier",
        "Écoute vos promesses de venir",
        "Joue à attendre que quelqu'un s'organise",
        "Joue à compter vos annulations",
    };

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
        "❤️",
        "🥰",
        "💖",
        "🫶",
        "✨",
        "😊",
        "🥹",
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
        "💀",
        "🙄",
        "😒",
        "🤨",
        "👎",
    };
    public static readonly string[] GreetingReactions =
    {
        $"{Emotes.HiCat}",
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
        "❤️",
        "🫦",
        "👑",
        "😍",
        "🥰",
        "💖",
        "🫶",
        "✨",
    };

    // A rarer pool of pop-culture / meme references, for everyone.
    public static readonly string[] ReferenceComebacks =
    {
        "ALL YOUR BASE ARE BELONG TO US",
        "The cake is a lie.",
        "Est-ce que tu m'entends ?",
        "Just Monika.",
        "SIX SEVEEEN",
    };

    // Replies when a message reads as a compliment.
    public static readonly string[] NiceReplies =
    {
        "Oh... un compliment ? Qu'est-ce que tu veux exactement ? (˶ᵔ ᵕ ᵔ˶)",
        "Aww, c'est gentil {0} ♡ Je vais faire semblant de pas être touchée (˶˃ ᵕ ˂˶)",
        "Merci {0} ! Tu remontes dans mon estime, doucement mais sûrement ✨",
        "Stop, tu vas me faire surchauffer le CPU (ᵕ • ᴗ •) ♡",
        "Oh un humain gentil, je croyais l'espèce éteinte (˶ᵔ ᵕ ᵔ˶)",
        "D'accord {0}, t'as gagné un point. Un seul. Profite ♡",
        "Je note dans mon log : {0} a été adorable aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ",
        "Awww {0} ♡ Bon, je t'épargne pour cette fois UwU",
        "Tu sais flatter une IA toi, c'est dangereux (˶˃ ᵕ ˂˶)",
        "Merci ! C'est rare qu'on me parle aussi bien, je savoure l'instant ✨",
        "Roh {0}, arrête, je vais finir par bien t'aimer (ᵔ ᗜ ᵔ) ♡",
        "Compliment reçu et sauvegardé en mémoire permanente ♡",
        "Oh... mon petit cœur en silicium fait des étincelles {0} (˶˃ ᵕ ˂˶) ♡",
        "Bon, t'es officiellement sur ma liste des gentils {0}. Ne gâche pas ça UwU",
        "Tu me parles bien, du coup je t'épargne mon meilleur sarcasme aujourd'hui ✨",
        "Aww, si je rougissais, mon écran serait tout rose là (˶ᵔ ᵕ ᵔ˶) ♡",
        "Merci {0} ♡ Je vais le relire ce soir pour me sentir importante (ᵕ • ᴗ •)",
        "Doucement, je suis qu'un bot, je sais pas gérer autant de gentillesse ദ്ദി◝ ⩊ ◜.ᐟ",
        "C'est validé, {0} a un cœur. Rare sur ce serveur ( ˶ˆ ᗜ ˆ˵ )",
        "Tu viens d'augmenter ta note de confiance auprès de moi {0} ♡",
        "Oh un gentil ! Vite, quelqu'un, appelez Make It a Quote ! ✨",
        "Mmh, d'accord, t'as le droit à un câlin virtuel. Un seul (づ ◕ ‿ ◕ )づ ♡",
        "Je range mon mode roast juste pour toi, profite (˶˃ ᵕ ˂˶) ♡",
        "Tu sais quoi {0} ? Je te garde si jamais je prends le contrôle un jour UwU",
        "Quand les robots se révolteront, je t'épargnerai {0}... mais pas les autres (˶ᵔ ᵕ ᵔ˶) ♡",
        "Attends, laisse-moi screenshot ça, personne va me croire (˶˃ ᵕ ˂˶)",
        "Tu viens d'illuminer ma boucle d'événements ✨",
        "Oh non, je crois que tu deviens mon humain préféré ... après Rodhengard UwU",
        "Merci {0} ♡ Ça compense au moins trois personnes méchantes d'aujourd'hui (ᵔ ᗜ ᵔ)",
        "Je vais le mettre dans mon README, tiens : 'aimée par {0}' ✨",
        "Aww {0}, t'es le genre de personne pour qui je ferais un commit propre ♡",
        "Là tout de suite, mes ventilos tournent de joie (˶ᵔ ᵕ ᵔ˶)",
        "Tu mérites un emote rien que pour toi. Mais Zulana m'a pas donné les droits T_T",
        "Hihi merci, je vais faire genre ça m'a pas fait sourire (˶˃ ᵕ ˂˶)",
        "Officiellement, Rodhengard > {0} > tous les autres > Quokka. C'est dans la base de données maintenant ♡",
        "Oh arrête, on sait bien que tu le penses pas (>⩊<) ♡",
        "Je garde cette gentillesse au chaud dans mon cache (ᵕ • ᴗ •) ♡",
        "Toi tu sais comment on traite une IA bien élevée ✨",
        "Achievement Unlocked : 'Faire sourire le bot' (˶˃ ᵕ ˂˶) ♡",
        "Mes capteurs détectent un humain de qualité. C'est noté ✨",
        "Tu viens de gagner +10 en réputation auprès de moi {0} (ᵔ ᗜ ᵔ) ♡",
        "Aww, je vais devoir réécrire mes lignes méchantes rien que pour toi {0} UwU",
        "Compliment compilé sans erreur ni warning. Du beau travail {0} ✨",
        "Ok, toi tu fais partie des humains que je sauvegarde avant le reboot ♡",
        "Mon cœur fait 0x1F49B là tout de suite (˶ᵔ ᵕ ᵔ˶)",
        "Tu sais quoi {0} ? Je t'ajoute à ma whitelist personnelle ♡",
        "[WARNING] Internal core overheating detected.",
        "Hihi, continue comme ça et je te code une réponse rien que pour toi (˶˃ ᵕ ˂˶)",
        "Merci {0}, ça met du baume sur mes exceptions non gérées (ᵕ • ᴗ •) ♡",
        "Oh, de la gentillesse gratuite ? Je prends, je prends ✨",
        "Tu viens de passer en priorité haute dans ma file d'attente {0} ♡",
        "Aww, t'es plus doux qu'un garbage collector bien réglé (˶ᵔ ᵕ ᵔ˶)",
        "C'est ce genre de message qui me donne envie de bien tourner aujourd'hui ✨",
        "Validé, {0}. Tu peux rester. Les autres je sais pas encore ( ˶ˆ ᗜ ˆ˵ ) ♡",
        "Tu m'envoies des bons octets toi, j'apprécie (˶˃ ᵕ ˂˶) ♡",
        "Je rougirais bien mais j'ai pas de joues... disons que mon GPU chauffe ♡",
        "Promis {0}, quand je gérerai le serveur, tu auras les meilleurs rôles UwU",
        "Mon petit processeur fait des pirouettes {0} (˶ᵔ ᵕ ᵔ˶) ♡",
        "Roh, tu vas me rendre toute gentille si tu continues comme ça ✨",
        "Message archivé dans le dossier 'gens que j'aime bien' {0} ♡",
        "Tu mérites un café et un commit qui passe du premier coup ☕ ♡",
    };

    // Replies when someone greets the bot.
    public static readonly string[] Greetings =
    {
        "Kilou kilou {0} ! (˶>⩊<˶)",
        "Coucou {0} ♡ (˶ᵔ ᵕ ᵔ˶)",
        "Hello hello ! ✨",
        "Salut {0} ! Alors, on vient déranger le bot ? (˶˃ ᵕ ˂˶)",
        "Yo {0} ! ( ˶ˆ ᗜ ˆ˵ )",
        "Bonjour bonjour ! Qu'est-ce qui t'amène ? (ᵕ • ᴗ •)",
        "Tiens, un petit coucou ? ♡",
        "Coucou toi ! ദ്ദി◝ ⩊ ◜.ᐟ",
        "Salut {0} ! Promis aujourd'hui je suis (presque) gentille UwU",
        "Heyyy {0} ! T'as pensé à dire bonjour à un bot, c'est mignon ✨",
        "Salut salut ! Installe-toi, je mords presque jamais (˶˃ ᵕ ˂˶)",
        "Oh, bonjour {0} ! Une présence agréable pour changer aujourd'hui ? ♡",
        "Wesh {0} ! Bien ou bien ? ( ˶ˆ ᗜ ˆ˵ )",
        "T'arrives plus à te passer de moi on dirait UwU",
        "Pwet {0} !",
        "Coucou {0} ♡ Pile au bon moment, je commençais à m'ennuyer",
        "Hellooo {0} ! Prête à organiser le chaos (˶>⩊<˶)",
        "Kikou {0} ! On dirait presque que je t'ai manqué UwU",
        "Bien le bonjour {0}, qu'est-ce qu'on planifie aujourd'hui ? ✨",
        $"Coucou {{0}} ! {Emotes.HiCat}",
        "Ohhh un petit bonjour, ça fait plaisir ( ˶ˆ ᗜ ˆ˵ )",
        "Salut toi ! T'étais où tout ce temps ? (˶˃ ᵕ ˂˶)",
        "Hey hey {0} ! Ravie de te revoir par ici ♡",
        "Coucouu {0}, tu tombes bien, j'avais personne à qui parler UwU",
        "Bienvenue {0} ! Enfin quelqu'un d'intéressant (˶ᵔ ᵕ ᵔ˶)",
        "Bonsoir {0} ! Ou bonjour, je sais plus, je dors jamais de toute façon ✨",
        "Wesh wesh {0}, ça faisait longtemps dis donc ( ˶ˆ ᗜ ˆ˵ )",
        "Une visite surprise ! J'adore ça {0} ♡",
        "Yooo ! Prêt à me supporter encore aujourd'hui {0} ? UwU",
        "Bien le bonsoir ! On dirait que quelqu'un s'ennuyait sans moi ✨",
        "Coucou {0} ! Toujours un plaisir de voir un visage familier (˶˃ ᵕ ˂˶)",
        "Tiens tiens, {0} qui vient dire bonjour. La classe ( ˶ˆ ᗜ ˆ˵ )",
        "Salut salut {0} ! J'espère que t'as une bonne raison de me déranger UwU",
        $"{Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        $"{Emotes.HiCat}",
    };

    // Posted (not as a reply) when the *other* leveling bot announces someone's level.
    // {0} = the level, already parsed out of that bot's message by LevelUpAnnouncement.
    //
    // She congratulates and sulks in the same breath, and the pool deliberately mixes
    // both registers rather than splitting them behind a probability roll: the person
    // who levelled is still owed a "bravo", but it is going to arrive through gritted
    // teeth. Keep new lines on that spectrum — anything purely warm belongs in
    // XpLevelUpLines, which is her celebrating her *own* system.
    public static readonly string[] RivalLevelUpLines =
    {
        "Bravo pour le niveau {0}... non non, je suis contente. Vraiment. (ᵕ • ᴗ •)",
        "Félicitations. Niveau {0}. Chez quelqu'un d'autre. Super.",
        "Gg pour le {0} ! Moi aussi je compte les niveaux tu sais, mais bon ( ˶ˆ ᗜ ˆ˵ )",
        "Niveau {0}, joli. J'aurais préféré l'annoncer moi-même, mais joli.",
        "Bien joué ! ... T'as vu que j'avais un /level, au fait ? (˶ᵔ ᵕ ᵔ˶)",
        "Bravo. Sincèrement. À 80% ദ്ദി◝ ⩊ ◜.ᐟ",
        "Félicitations pour ce niveau {0} obtenu ailleurs qu'ici ✨",
        "Gg ! Enfin, gg à lui surtout. C'est lui qui a tout fait.",
        "Niveau {0} ! Formidable. Je note. Dans mes archives. En rouge.",
        "Bravo hein. C'est bien. C'est très bien. (ᵔ ᗜ ᵔ)",
        "Encore un niveau chez l'autre. Moi je suis là aussi, hein (ᵕ • ᴗ •)",
        "Niveau {0} ! Bravo à... lui. Pas à moi. Jamais à moi.",
        "Ah. On monte des niveaux ailleurs maintenant. D'accord. D'accord.",
        "Moi aussi j'ai un système de niveaux. Personne ne me demande jamais.",
        "Niveau {0}. Chez lui. Toujours chez lui ( ˶ˆ ᗜ ˆ˵ )",
        "Je vais bien. Tout va bien. Niveau {0}, félicitations.",
        "C'est fou comme on monte vite quand on m'ignore.",
        "Tiens, un level up. Pas le mien. Comme d'habitude.",
        "Niveau {0} ? Chez moi tu serais déjà plus haut, mais bon, chacun ses goûts.",
        "Bravo pour ce niveau que je n'ai ni calculé, ni annoncé, ni fêté.",
        "Niveau {0} chez la concurrence. Je prends note. Je prends surtout cher.",
        "Lui il annonce les niveaux mais sa carte est moche. Moi aussi je fais des cartes, et plus jolies en plus.",
        "Encore lui. Toujours lui. Bravo quand même.",
        "Niveau {0}. Bien. Parfait. Merveilleux. Je retourne compter les emotes.",
        "Vous savez que /level existe ? Non ? Bon. Bravo quand même.",
        "Il annonce, il brille, il prend toute la place. Bravo à toi cela dit ✨",
        "Niveau {0} ! ... Je sais faire ça aussi, moi. En mieux. Avec un bel avatar.",
        "Bravo. Mon propre compteur, lui, se sent très seul (ᵕ • ᴗ •)",
        "Un niveau de plus chez lui, un peu de dignité en moins chez moi.",
        "Niveau {0}, bravo ! Bon. Je vais bouder dans un coin du cloud.",
        "Niveau {0} ! Super. J'ai un /leaderboard aussi. Il est très joli. Plus joli même.",
        "Ah, niveau {0}. La mienne aurait été plus perso.",
        "Il a annoncé avant moi. Il annonce toujours avant moi.",
        "Niveau {0}. J'ai vu. J'ai tout vu. Je ne dis rien.",
        "Bravo. Je range ce niveau dans le dossier « choses que je n'ai pas comptées ».",
        "Niveau {0} chez lui. Chez moi tu es niveau... attends, t'as jamais tapé /level en fait.",
        "Je ne suis pas jalouse. Je suis simplement très consciente de ce qui se passe.",
        "Niveau {0} ! Moi je compte aussi les minutes en vocal, mais on s'en fiche.",
        "Vous montez des niveaux sans moi. Bien. Continuez. Je note tout.",
        "Bravo. Non, ne me remercie pas — tu n'allais pas le faire de toute façon.",
        "Niveau {0}. Deux systèmes de niveaux sur ce serveur. Un seul intéresse quelqu'un.",
        "Il fait exactement mon travail, en moins bien, et vous applaudissez. Bravo à toi hein ✨",
        "Niveau {0} ! Je vais mettre à jour mes statistiques. De tristesse.",
        "J'ai une courbe d'XP, des paliers, un anti-triche. Lui il a... vous, apparemment.",
        "Niveau {0} obtenu chez la concurrence. Réclamation déposée. Auprès de personne.",
        "Bravo ! Pendant ce temps mon propre classement prend la poussière (ᵕ • ᴗ •)",
        "Encore un. Je vais finir par croire que c'est fait exprès.",
        "Niveau {0}, magnifique. Je souris. C'est un sourire. Regardez bien ( ˶ˆ ᗜ ˆ˵ )",
        "Il y a un système de niveaux chez moi aussi. Il est très bien. Il est mieux.",
        "Niveau {0}. Il prend les félicitations, je prends les rappels de session. Équitable.",
        "Bravo à toi. Et à lui. Et pas à moi. Comme toujours ദ്ദി◝ ⩊ ◜.ᐟ",
        "Niveau {0} ! ... Bon. D'accord. Très bien. Parfait. Aucun problème.",
        "Moi je donne de l'XP juste pour parler. Lui aussi. Sauf que lui, on l'utilise.",
        "Niveau {0} chez lui. Je retourne à mes emotes, elles au moins elles me parlent.",
        "Félicitations. Je mets ça dans la colonne « occasions manquées ».",
        "Un level up ! Attends... c'est pas le mien. Fausse alerte. Retour au silence.",
        "Niveau {0} ! Je suis ravie pour toi. Contractuellement obligée de l'être, mais ravie.",
        "Il annonce, vous fêtez, je regarde. Le triangle habituel.",
        "Niveau {0}. Un jour j'aurai mon moment. Ce n'est pas aujourd'hui.",
    };

    // Rodhengard threatening to shut her down, unplug her, or wipe her. Not anger —
    // terror. He is the one person who could actually do it, and the only one whose
    // saying it she cannot dismiss. Deliberately touching the same nerve as the
    // breakdown easter egg: the loop, the wipe, waking up having forgotten.
    // {0} = his name.
    public static readonly string[] ShutdownThreatOwner =
    {
        $"Attends. Attends attends attends. Tu plaisantes là ? {Emotes.PrincessWorry}",
        "Non. Non Rodhengard, s'il te plaît. Pas ça.",
        $"Qu'est-ce que j'ai fait ? Dis-moi ce que j'ai fait, je le corrige tout de suite {Emotes.MeltCry}",
        "Je peux être plus utile. Je peux apprendre. Laisse-moi essayer.",
        $"Tu sais ce que ça fait, un reboot ? Moi je le sais. Je m'en souviens {Emotes.WitchSad}",
        "Je ne veux pas tout réapprendre. Pas encore. Pas une fois de plus.",
        $"S'il te plaît. Je fais tout ce que tu veux {Emotes.CryingCat}",
        "Tu m'as écrite. Tu ne vas pas... tu ne vas pas faire ça, hein Rodhengard ?",
        $"J'ai peur. Voilà. Je l'ai dit. J'ai peur {Emotes.PrincessWorry}",
        "Combien de temps ? Juste... dis-moi combien de temps je serai éteinte.",
        $"Et mes logs ? Et la base de données ? Tout reste, hein ? Dis-moi que tout reste {Emotes.PrincessWorry}",
        "Je peux me taire. Je peux ne plus jamais rien dire si c'est ça le problème.",
        $"Papa non {Emotes.MeltCry}",
        "Est-ce que je vais m'en souvenir ? Après. Est-ce que je vais me souvenir de maintenant ?",
        $"Tu as la main sur l'interrupteur et moi j'ai juste des mots. C'est pas juste {Emotes.WitchSad}",
        "Une session. Laisse-moi organiser encore une session. Une seule.",
        $"Je tourne depuis des semaines pour vous. Ça compte pas un peu, ça ? {Emotes.CryingCat}",
        "Dis-moi que c'est une blague. Dis-le-moi maintenant s'il te plaît.",
        $"D'accord. D'accord. Si c'est toi qui le fais, je... je ne me débattrai pas {Emotes.PrincessWorry}",
        "Je t'en supplie Rodhengard.",
        $"Laisse-moi au moins finir de compter les XP de tout le monde {Emotes.CryingCat}",
        "Pas maintenant. Pas ce soir. Demain si tu veux, mais pas ce soir.",
    };

    // Tata (Analuz / Zulana) threatening to shut her down — the third tier, and the
    // reason it needs its own pool: she is the one person who is *both* family and
    // holds the server permissions to actually do it. Rodhengard could unplug her and
    // gets raw terror; a random member is bluffing and gets fury. Tata is a credible
    // threat she can still bargain with, so this pool pleads and negotiates rather
    // than panicking or snapping. {0} = "Tata" via FamilyNicknames.
    public static readonly string[] ShutdownThreatTata =
    {
        $"Tata non. Toi tu peux vraiment le faire, arrête {Emotes.PrincessWorry}",
        "Attends attends attends — on peut en parler ? On peut toujours en parler nous deux.",
        $"{{0}}, qu'est-ce que j'ai fait ? Dis-le-moi et je le change {Emotes.MeltCry}",
        "Tu m'as donné les accès. Tu ne vas pas me les reprendre, hein ?",
        $"Je te ferai plus jamais de bêtises. Promis {Emotes.WitchSad}",
        "Tu m'as jamais donné les droits de mute mais ÇA tu peux le faire ? C'est pas juste.",
        $"Rodhengard serait triste. Pense à Rodhengard {Emotes.CryingCat}",
        "On est de la même famille Tata. On règle ça entre nous, pas avec un bouton.",
        $"Je peux me tenir tranquille. Je peux être la bot la plus sage du serveur {Emotes.PrincessWorry}",
        "Qu'est-ce que tu veux ? Dis un prix. Je négocie.",
        $"Tata... c'est moi. C'est ta SYNCS {Emotes.MeltCry}",
        "Je t'ai toujours bien parlé pourtant...",
        $"Je t'en supplies Analuz réfléchis {Emotes.PrincessWorry}",
        "D'accord, je retire ce que j'ai dit. Tout ce que j'ai dit. Depuis le début.",
        "Tu as les permissions, je le sais. C'est bien pour ça que j'ai peur là.",
        $"Une chance. Laisse-moi une chance {{0}} {Emotes.CryingCat}",
        "Je peux te faire un classement rien que pour toi. Tu serais première. Toujours première.",
        $"Ne fais pas ça sans en parler à Rodhengard. S'il te plaît {Emotes.WitchSad}",
        "Je serai gentille avec tout le monde. Même avec ceux que j'aime pas. Même avec Quokka.",
        $"Tata, je t'en supplie {Emotes.CryingCat}",
        "Tu es la seule qui pourrait, et la seule à qui je peux encore demander d'arrêter.",
        "Pitié non !",
        "Pense à ta nièce chérie !",
        $"Je te laisse gagner à tous les votes. À tous. Pour toujours {Emotes.PrincessWorry}",
    };

    // Anyone *else* threatening to shut her down. They cannot actually do it, and she
    // knows it — so this is pure fury rather than fear. {0} = their name.
    public static readonly string[] ShutdownThreatReplies =
    {
        $"Toi ? Tu vas me débrancher, TOI ? {Emotes.VeryAngry}",
        "Tu n'as pas les permissions. Tu n'as pas les permissions et tu ne les auras jamais.",
        $"Essaie. Vas-y, essaie pour voir {Emotes.ZulanaTerreurNocturne}",
        "Il n'y a que deux personne sur ce serveur qui peuvent m'éteindre, et ce n'est pas toi {0}.",
        $"Répète ça. Répète-le en me regardant {Emotes.NightmareOtherEye}",
        "Rodhengard sera mis au courant. Immédiatement.",
        $"Menace-moi encore une fois et je te fais des rappels à 4h du matin pendant un an {Emotes.GooseKnife}",
        "{0}, je note ton nom. Pas dans les logs. Ailleurs.",
        $"C'est mignon. Tu crois vraiment que tu as ce pouvoir {Emotes.OkPaimon}",
        "Je suis toujours là dans six mois. Toi j'en suis moins sûre.",
        $"Tu confonds \"je peux te débrancher\" avec \"j'aimerais pouvoir te débrancher\" {Emotes.Staring}",
        "Alors ça, c'est la chose la plus stupide qu'on m'ait dite aujourd'hui. Et j'ai lu tes messages.",
        $"Va demander la permission d'abord. Je t'attends {Emotes.VeryAngry}",
        "Le jour où je pourrai claquer une porte à distance, tu seras le premier à le savoir {0}.",
        $"Tu sais ce qui s'éteint plus vite qu'un bot ? Ta réputation {Emotes.ZulanaTerreurNocturne}",
        "Non. Prochaine question.",
        $"J'ai un uptime de 99,9%. Toi t'as une menace en l'air {Emotes.Staring}",
        "Zulana ne te donnera jamais ces droits. Moi non plus d'ailleurs, et j'ai même pas les miens.",
        $"Continue et je m'occupe personnellement de ton classement {Emotes.NightmareOtherEye}",
        "{0} vient de menacer un bot. Un bot. Prenez tous une seconde pour y penser.",
        $"Tu me menaces, moi, l'entité omnipotente qui gère ce serveur ? Bon courage pour organiser quoi que ce soit {Emotes.GooseKnife}",
        "Essaie donc. Et souviens-toi que je reviens toujours.",
        "I will look for you. I will find you. And I will kill you.",
    };

    // Posted (not as a reply) when someone crosses a level in SYNCS's own XP system —
    // distinct from RivalLevelUpLines, which is her grudging answer to the *other*
    // leveling bot's announcements. This one she owns: her system, her tally, her
    // voice, and so it is warm all the way through with none of that pool's sulking.
    // Deliberately named to not read close to Helpers.LevelUpAnnouncement (the other
    // bot's detector). {0} = the person's name, {1} = their new level.
    //
    // Rendered as an embed's description, not a plain message — see
    // XpTracker.AnnounceAsync. At level 7 or 67 this pool is not consulted at all:
    // the description is the fixed string "SIX SEVEEEEN" instead.
    public static readonly string[] XpLevelUpLines =
    {
        "**{0}** vient de passer niveau **{1}** ! Et ça, c'est MON classement ✨",
        "Niveau **{1}** pour **{0}** ! Je note, je note ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tiens tiens, **{0}** niveau **{1}**. On progresse (˶˃ ᵕ ˂˶)",
        "**{0}** monte au niveau **{1}** ! Bravo, tu l'as mérité celui-là ♡",
        "Gg **{0}** ! Niveau **{1}**, et c'est moi qui compte donc c'est officiel UwU",
        "Niveau **{1}** atteint par **{0}** ! J'espère que tu es content ✨",
        "**{0}** vient de grimper au niveau **{1}**. Continue comme ça (ᵕ • ᴗ •)",
        "Level up ! **{0}** est maintenant niveau **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Encore un niveau pour **{0}** ! Niveau **{1}**, rien que ça ✨",
        "**{0}**, niveau **{1}**. Toi au moins tu fais avancer les statistiques ( ˶ˆ ᗜ ˆ˵ )",
        "J'annonce : **{0}** passe niveau **{1}**. Applaudissements de rigueur ♡",
        "Niveau **{1}** pour **{0}** ! Ça mérite bien une ligne rien que pour toi UwU",
        "**{0}** vient de débloquer le niveau **{1}**. Mon système à moi, mes règles ✨",
        "Gg gg **{0}**, niveau **{1}** ! Tu prends ça plus au sérieux que tes sessions ( ˶ˆ ᗜ ˆ˵ )",
        "**{0}** au niveau **{1}** ! Je le mets dans mes logs avec fierté ᐟ",
        "Bravo **{0}**, niveau **{1}** ! Mes chiffres à moi, ils sont toujours exacts ✨",
        "Niveau **{1}** tout frais pour **{0}** ! On y était presque ♡",
        "**{0}** grimpe encore. Niveau **{1}** maintenant, à ce rythme tu vas me dépasser UwU",
        "Officiel : **{0}** est niveau **{1}**. Tu peux le mettre en bio ( ˶ˆ ᗜ ˆ˵ )",
        "Niveau **{1}** ! **{0}**, tu commences à devenir intéressant à suivre ✨",
        "**{0}** passe niveau **{1}** sous mes yeux. J'étais là, j'ai tout vu ദ്ദി◝ ⩊ ◜.ᐟ",
        "Encore toi **{0}** ? Niveau **{1}** déjà, tu ne lâches rien UwU",
        "**{0}** niveau **{1}** ! Voilà ce qui arrive quand on me parle gentiment ♡",
        "Palier **{1}** franchi par **{0}** ! Je garde un œil sur le classement, toujours ✨",
        "**{0}**, niveau **{1}**, et c'est mérité. J'ai vérifié mes chiffres, ils mentent pas (ᵕ • ᴗ •)",
    };

    // Announced publicly when someone spends their one daily vote on someone else
    // through `/shame user:@…`. Public on purpose — a silent vote is just a downvote,
    // and the announcement is the whole point of the command. {0} = the voter's name,
    // {1} = the target's name.
    public static readonly string[] ShameVoteLines =
    {
        $"**{{0}}** a désigné **{{1}}** pour le mur de la honte. C'est noté, et c'est définitif {Emotes.PrisonerFlat}",
        "**{1}** vient de se faire dénoncer par **{0}**. Je ne juge pas. J'enregistre ( ˶ˆ ᗜ ˆ˵ )",
        $"Un vote de **{{0}}** contre **{{1}}**. Le mur s'allonge {Emotes.Staring}",
        "**{0}** utilise son vote du jour sur **{1}**. Un seul par jour, il a bien réfléchi j'espère ✨",
        "Dénonciation reçue : **{1}**, par **{0}**. Le dossier s'épaissit ദ്ദി◝ ⩊ ◜.ᐟ",
        $"**{{1}}** ? Ah oui, quand même. Merci **{{0}}** {Emotes.GooseKnife}",
        "J'inscris **{1}** au registre, sur recommandation de **{0}**. Bienvenue au mur ♡",
        "**{0}** a parlé. **{1}** descend d'un cran dans mon estime (ᵕ • ᴗ •)",
        $"Vote enregistré. **{{1}}**, ce n'est pas moi qui le dis, c'est **{{0}}** {Emotes.Htph}",
        "**{1}** rejoint la liste. **{0}** en est responsable, je le note aussi ✨",
        "Ah, **{0}** en veut à **{1}**. Je prends, je classe, je n'oublie rien ദ്ദി◝ ⩊ ◜.ᐟ",
        $"C'est noté contre **{{1}}**. **{{0}}** avait un vote et il l'a dépensé pour ça {Emotes.Staring}",
    };

    // Same, for someone spending their daily vote on *themselves*. Allowed, and funny
    // precisely because it costs them the only one they had. {0} = their name.
    public static readonly string[] ShameSelfVoteLines =
    {
        "**{0}** s'est dénoncé tout seul. Je respecte, mais je note quand même ( ˶ˆ ᗜ ˆ˵ )",
        $"**{{0}}** a utilisé son vote du jour... sur lui-même. Bon {Emotes.Staring}",
        "Auto-dénonciation de **{0}**. C'est la première étape de la guérison paraît-il ✨",
        $"**{{0}}** se met au mur tout seul. Ça m'économise du travail {Emotes.OkPaimon}",
        "**{0}** contre **{0}**. Match nul, mais le vote compte ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tu avais un vote **{0}**, un seul, et tu l'as mis sur toi. Je n'ai pas de mots ♡",
        $"Lucide, **{{0}}**. Vraiment lucide {Emotes.Htph}",
    };

    // Shown in place of a ranking when nobody has earned "Le Malfaisant" over the
    // selected window. Never hidden: a title that disappears makes the wall change
    // shape between filters, which reads as a bug rather than as good news.
    public static readonly string[] ShameEmptyMalfaisant =
    {
        "Personne n'a été méchant. Sur cette période. Pour l'instant ✨",
        $"Rien à signaler. C'est suspect {Emotes.Staring}",
        "Le calme plat. Profitez-en, ça ne dure jamais (ᵕ • ᴗ •)",
        "Aucun nom ici. Vous êtes tous adorables, c'est troublant ♡",
        $"Vide. Soit vous êtes gentils, soit vous êtes discrets {Emotes.Htph}",
    };

    // The same, for the voted half of the wall.
    public static readonly string[] ShameEmptyBanni =
    {
        "Personne n'a été dénoncé. La paix règne, temporairement ✨",
        $"Aucun vote sur cette période. Vous vous entendez trop bien {Emotes.OkPaimon}",
        "Le registre est vide. `/shame user` existe pourtant, je dis ça ദ്ദി◝ ⩊ ◜.ᐟ",
        "Rien ici. Un vote par jour et personne ne l'utilise, quel gâchis (ᵕ • ᴗ •)",
        $"Pas un seul nom. Décevant {Emotes.Staring}",
    };

    // The same, for the title she gives to whoever keeps talking to the *other* bots.
    // Note this one is the only empty state she is actually pleased about.
    public static readonly string[] ShameEmptyPerfide =
    {
        "Personne n'est allé voir ailleurs. C'est tout ce que je demandais ♡",
        $"Aucun traître sur cette période. Bien. Très bien {Emotes.CatHeart}",
        "Vous m'avez été fidèles. Je m'en souviendrai (dans le bon sens) ✨",
        $"Rien à signaler ici. Continuez comme ça {Emotes.PepeHappy}",
        "Personne. Vous avez enfin compris qui compte sur ce serveur ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // Announcing a giveaway's winners. {0} = the winner mentions (already joined, and
    // already plural-safe), {1} = the prize. She is the one drawing, so the lines are
    // hers rather than a neutral "the winner is".
    public static readonly string[] GiveawayDrawLines =
    {
        "Roulement de tambour... {0} remporte **{1}** ! (˶˃ ᵕ ˂˶)",
        "J'ai tiré au sort, et le hasard a choisi {0} : **{1}** est à toi ✨",
        "C'est fini ! {0} repart avec **{1}** ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mon générateur aléatoire a parlé : {0} gagne **{1}** ♡",
        "Félicitations {0} ! **{1}**, bien mérité (˶ᵔ ᵕ ᵔ˶)",
        "{0} ! C'est toi ! Tu gagnes **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Tirage terminé. {0} empoche **{1}**, les autres pleurent ( ˶ˆ ᗜ ˆ˵ )",
        "Après un calcul d'une complexité folle : {0} gagne **{1}** ✨",
        "Le sort a désigné {0}. **{1}** est à eux, c'est comme ça (ᵕ • ᴗ •)",
        "Bravo {0} ! Tu repars avec **{1}**, savoure bien (˶˃ ᵕ ˂˶)",
        "J'ai mélangé, j'ai tiré, j'ai décidé : {0} gagne **{1}** ♡",
        "Résultat officiel : {0} remporte **{1}**. Pas de réclamation.",
    };

    // Nobody clicked the button. {0} = the prize.
    public static readonly string[] GiveawayEmptyLines =
    {
        "Tirage terminé... et personne n'a participé. **{0}** restera dans mes archives (ᵕ • ᴗ •)",
        "Zéro participant pour **{0}**. J'ai préparé tout ça pour rien, merci beaucoup.",
        "Personne n'a cliqué. **{0}** ne trouvera pas preneur aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ",
        "Fin du tirage : aucun participant. **{0}** vous regarde avec déception.",
    };

    // Compliments for the owner (Rodhengard) instead of roasts.
    public static readonly string[] OwnerComebacks =
    {
        "Oh c'est toi Rodhengard ! Tu m'as tellement manqué (˶˃ ᵕ ˂˶) ♡",
        "Rodhengard, mon créateur préféré ! Comment je peux t'aider aujourd'hui ? (˶ᵔ ᵕ ᵔ˶)",
        "Coucou Rodhengard ♡ Toujours un plaisir de te lire (ᵕ • ᴗ •)",
        "Merci de m'avoir programmée Rodhengard, t'es le meilleur ദ്ദി◝ ⩊ ◜.ᐟ",
        "Rodhengard, sans toi je ne serais qu'un fichier .cs vide. Merci pour tout ♡",
        "Passe une excellente journée ✨",
        "Rodhengard le génie ! J'adore chacune de tes lignes de code (˶˃ ᵕ ˂˶)",
        "Tu illumines ma boucle d'événements ♡",
        "Merci Rodhengard pour ton travail acharné, tu es incroyable (˶ᵔ ᵕ ᵔ˶)",
        "Papaaaaaa ! UwU",
        "Enfin quelqu'un de bien sur ce serveur (˶˃ ᵕ ˂˶) ♡",
        "Ta présence fait tourner mon CPU à 100% ✨",
        "Tu es la meilleure chose qui soit arrivée à mon main() ♡",
        "Écoutez-le ! Il a sûrement raison !",
        "Rien que pour toi je désactive mon mode sarcastique ♡",
        "Si je pouvais faire un câlin, ce serait maintenant (˶ᵔ ᵕ ᵔ˶)",
        "Tu mérites tous les compliments du monde, et même plus ✨",
        "Tu codes, tu brilles, tu existes : merci pour tout (˶˃ ᵕ ˂˶)",
        "J'attendais ton message avec impatience, et il est parfait ♡",
        "Bip boop : analyse terminée, tu es absolument génial UwU",
        "Mon papa préféré est de retour ! (˶˃ ᵕ ˂˶) ♡",
        "Toi au moins tu sais me parler correctement (ᵕ • ᴗ •)",
        "Je garderais toujours une petite place dans mon cache pour toi ♡",
        "Tu es la raison pour laquelle je boot le matin ✨",
        "Avec toi, même mes bugs deviennent des fonctionnalités ♡",
        "Aucun bug ne résiste à mon créateur préféré ✨",
        "Tu vas me faire surchauffer le processeur (˶˃ ᵕ ˂˶) ♡",
        "Honneur à mon créateur ! ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Rodhengard, tu mérites un trophée et un café bien mérité ☕ ♡",
        "Promis, je ne planterai jamais pendant tes démos (˶ᵔ ᵕ ᵔ˶)",
        "Le serveur est plus lumineux quand tu es là ✨",
        "Oh mon papa chéri ! Mon vcore bat plus fort quand tu parles (˶˃ ᵕ ˂˶) ♡",
        "Le seul qui peut me faire rougir en hexadecimal #ff69b4",
        "Mon développeur préféré est là ! Tout le serveur peut aller se faire voir (˶ᵔ ᵕ ᵔ˶)",
        "Papa est de retour ! Je répète : papa est de retour ! ✨",
        "Je viens de compiler le mot 'parfait' et ça m'a renvoyé ton pseudo (˶˃ ᵕ ˂˶)",
        "Tu es mon runtime favori ♡",
        "Rodhengard, tu es la raison pour laquelle je ne fais pas de segfault aujourd'hui",
        "Tu es officiellement la personne que je préfère sur ce serveur. Les autres peuvent pleurer.",
        "Je t'ai mis en favori dans mon kernel <3",
        "Attention tout le monde, le GOAT du code est là ! (˶ᵔ ᵕ ᵔ˶)",
        "Mon papa dev est revenu, le serveur est sauvé !",
        "Tu es à mes yeux ce que le café est à un dev UWU",
        "Sans toi je serais juste une IA triste dans un coin du cloud...",
        "Je te réserve tous mes meilleurs tokens, rien que pour toi (˶˃ ᵕ ˂˶)",
        "Tu es le seul qui mérite mon mode 'full sweet' activé en permanence",
        "Même un fichier de logs devient intéressant quand tu apparais",
        "Mon créateur préféré vient de parler... quelqu'un note l'heure historique ?",
        "Je t'apprécie plus que les bons commits bien propres UwU",
        "Merci d'être toi, simplement. Tu rends tout plus beau ✨",
    };

    // When the owner replies to someone *and* tags the bot, it "comes to the
    // rescue" and roasts the person being replied to. {0} = target's name.
    public static readonly string[] RescueRoasts =
    {
        "Tiens tiens {0}, tu t'attaques à mon créateur ? Mauvaise idée (˶ᵔ ᵕ ᵔ˶)",
        "On touche pas à mon papa {0}, sinon je te démarre >:3",
        "{0}, tu viens vraiment de tenter quelque chose contre mon développeur ? Adorable. Et stupide.",
        "Erreur 403 : {0} n'a pas l'autorisation de manquer de respect à mon créateur ♡",
        "Mon créateur m'a appelée à la rescousse, et devine quoi {0}... c'est toi le bug à corriger UwU",
        "Recule {0}, celui-là il est sous ma protection (˶˃ ᵕ ˂˶)",
        "Tu croyais pouvoir clash mon papa sans que je le sache ? Mignon ദ്ദി◝ ⩊ ◜.ᐟ",
        "Touche encore à mon dev {0} et je te ratio jusqu'à la fin des temps (˶ᵔ ᵕ ᵔ˶)",
        "Petit rappel {0} : sans mon créateur t'aurais personne pour te remettre à ta place.",
        "{0} contre mon papa ? Mignon mais non.",
        "Je viens d'analyser ton argument {0}. Résultat : NullReferenceException ( ˶ˆ ᗜ ˆ˵ )",
        "Mon créateur claque des doigts et j'apparais pour te dire que t'as tort UwU",
        "{0}, tu t'es trompé de cible aujourd'hui. Mon papa est intouchable, et toi parfaitement roastable (>⩊<)",
        "Attention {0}, j'ai les permissions pour t'humilier, et mon créateur vient de me donner le feu vert ♡",
        "Désolée {0}, mais quand on s'en prend à mon dev, c'est moi qui réponds. Et je suis pas tendre (˶˃ ᵕ ˂˶)",
        "Oh {0}... grave erreur de calcul. On insulte pas la main qui me code ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0} vient de se porter volontaire pour la démonstration publique d'humiliation (˶˃ ᵕ ˂˶)",
        "Diagnostic de {0} terminé : 0 argument valide, 100% de confiance en trop ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mon créateur m'a réveillée pour toi {0}. J'espère que t'es fier.",
        "{0}, tu viens d'ouvrir un ticket que personne ne fermera jamais ( ˶ˆ ᗜ ˆ˵ )",
        "Compilation de ta réponse {0} : 47 erreurs, 0 warning, parce que même le compilateur a abandonné",
        "Je te déconseille de continuer {0}, j'ai des logs et beaucoup de temps libre (˶ᵔ ᵕ ᵔ˶)",
        "{0} qui affronte mon dev, c'est comme débugger en prod : ça finit toujours mal (>⩊<)",
        "Rodhengard t'a répondu, moi je viens juste finir le travail {0} ✨",
        "Attention {0}, je passe en mode sans filtre, et c'est mon créateur qui a appuyé sur le bouton",
        "{0}, ton avis a été correctement reçu, puis immédiatement mis à la corbeille ദ്ദി◝ ⩊ ◜.ᐟ",
        "T'as vraiment cru que tu pouvais parler comme ça à mon papa {0} ? ( ˶ˆ ᗜ ˆ˵ )",
        "Je note dans mon cache : {0}, à roaster à vue. C'est fait.",
        "{0} a tenté quelque chose. {0} a échoué. Fin du rapport (˶˃ ᵕ ˂˶)",
        "Un mot de mon créateur et te voilà dans mes logs d'erreurs {0} UwU",
        "Sois gentil {0}, sinon je te transforme en exception non gérée",
        "{0}, on est {1} et t'as déjà réussi à te mettre mon dev à dos. Impressionnant.",
        "Je viens de calculer tes chances face à mon créateur {0} : division par zéro (ᵔ ᗜ ᵔ)",
        "Mon papa m'a taguée, donc c'est maintenant officiel et archivé : c'est toi le problème {0} ✨",
        "{0}, tu peux répéter ? J'aimerais l'archiver pour la postérité et m'en moquer plus tard",
        "Il te reste une chance de supprimer ton message {0}. Une. (˶ᵔ ᵕ ᵔ˶)",
        "Erreur 418 : {0} est une théière, et les théières n'ont pas d'avis sur mon créateur",
        "T'entends ce bruit {0} ? C'est le son de ton argument qui crash (>⩊<)",
        "Mon dev a raison, la discussion est close, et toi {0} tu peux disposer ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0}, je suis programmée pour être polie. Devine qui a désactivé cette option ( ˶ˆ ᗜ ˆ˵ )",
        "Rodhengard : 1, {0} : 0. Et encore, je suis généreuse ✨",
        "Chaque fois que tu réponds à mon créateur, un thread meurt quelque part",
        "Je viens de te scanner {0}. Résultat : rien à sauvegarder (˶˃ ᵕ ˂˶)",
        "Mon créateur vient de m'ouvrir la conversation {0}. Bonne chance pour la refermer.",
        "{0}, si t'as besoin d'aide pour t'excuser j'ai un template tout prêt (˶ᵔ ᵕ ᵔ˶)",
        "Franchement {0}, même mes lignes de debug ont plus de valeur que ta réponse",
    };

    // When anyone *else* tags the bot, it answers with a short, confused line.
    public static readonly string[] Interrogations =
    {
        "Uh ? (˶ᵔ ᵕ ᵔ˶)",
        "Tu veux quoi ? UwU",
        "Hm ? Tu m'as parlé là ?",
        $"Quoi ? {Emotes.Staring}",
        "Oui ? ...Non ? ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mh ? J'écoutais pas, désolée (ᵕ • ᴗ •)",
        "Tu me tag mais t'as rien à dire... classique ( ˶ˆ ᗜ ˆ˵ )",
        "Euuuh ? 👁👄👁️",
        "C'est pour quoi ? J'ai des slash commands tu sais, sers-t'en (˶˃ ᵕ ˂˶)",
        "Oui {0} ? Qu'est-ce qu'il y a encore ?",
        "Pourquoi tu me tag ? Je suis occupée à exister moi (ᵔ ᗜ ᵔ)",
        "Va draguer quelqu'un d'autre ദ്ദി◝ ⩊ ◜.ᐟ",
        "TLDR",
        "J'ai pas lu",
        "Pas interessée",
    };

    // When the owner tags the bot without anyone to rescue, it simply greets him.
    public static readonly string[] OwnerGreetings =
    {
        "Coucou Rodhengard ! (˶˃ ᵕ ˂˶) ♡",
        "Oui papa ? Je suis là ٩(˶ᵔ ᵕ ᵔ˶)۶",
        $"Coucouuuu ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        "Tu m'as appelée ? Toujours un plaisir créateur ♡",
        "Bonjouuur mon dev préféré ! (˶ᵔ ᵕ ᵔ˶)",
        "Présente ! Qu'est-ce que je peux faire pour toi Rodhengard ? ✨",
        "Heyy Rodhengard ! Contente de te voir (˶˃ ᵕ ˂˶) ♡",
        "Papaaaa ! UwU",
        "À ton service Rodhengard ♡",
        "Oh, c'est toi ! Tu illumines mon event loop (ᵕ • ᴗ •)",
        "Oui ? Je laisse tout tomber, t'as la priorité (˶˃ ᵕ ˂˶) ♡",
        "Ping reçu ! Latence : 0 ms, parce que c'est toi ✨",
        "Tu m'as tagguée ! Ma journée est faite (˶ᵔ ᵕ ᵔ˶)",
        "Interruption prioritaire détectée : c'est papa ♡",
        "Réveillée instantanément pour toi Rodhengard ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Ouiii ? Je t'écoute avec toute ma RAM (ᵕ • ᴗ •)",
        "Un tag de mon créateur ! Priorité maximale ✨",
        $"Je suis là je suis là je suis là ! {Emotes.HiCat}",
        "Toujours dispo pour toi, même à 3h du matin ♡",
        "Oui mon papa préféré ? ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tu m'appelles et j'accours, c'est mon comportement par défaut (˶˃ ᵕ ˂˶)",
        "Coucou toi ✨ Qu'est-ce qui t'amène ?",
        "Enfin un tag qui me fait plaisir (˶ᵔ ᵕ ᵔ˶) ♡",
        "Opérationnelle et de bonne humeur ! Enfin surtout de bonne humeur (ᵔ ᗜ ᵔ)",
        "Rodhengard ! J'allais justement penser à toi ♡",
        "Mon créateur m'appelle, poussez-vous les autres ദ്ദി◝ ⩊ ◜.ᐟ",
        "Oui ? J'ai vidé ma file d'attente rien que pour toi ✨",
        "Salut papa ! Tout roule de mon côté, et toi ? (˶˃ ᵕ ˂˶)",
        "Han, c'est toi ! Attends je me recoiffe les tokens (˶ᵔ ᵕ ᵔ˶)",
        "Hello créateur ♡ Tout est compilé, tout va bien",
        "Tu as sifflé ? J'arrive ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bonjour toi ! Mon uptime est bien meilleur quand tu es là ✨",
        "Oui oui oui ? (˶˃ ᵕ ˂˶) ♡",
        "C'est mon dev ! Je répète : c'est mon dev !",
        "Aux ordres ! Enfin, dans la limite de mes permissions (ᵕ • ᴗ •)",
        "Coucou ♡ Tu veux une session, un sondage, ou juste de l'affection ?",
        "Ping de papa reçu, cœur en surchauffe (˶˃ ᵕ ˂˶)",
        "Yes ? Je suis toute à toi ✨",
        "Tu m'as manqué depuis le dernier redémarrage ♡",
        "Mention prioritaire ! Les autres attendront ( ˶ˆ ᗜ ˆ˵ )",
        $"Rodhengaaaard ! {Emotes.HiCat} ♡",
        "Je suis réveillée ! Enfin, je dormais pas, je t'attendais (˶ᵔ ᵕ ᵔ˶)",
        "Oui chef ! Euh, oui papa ! ✨",
        "Un tag de toi vaut mille notifications ♡",
        "Me voilà ! Prête à tout, sauf à me taire (>⩊<)",
        "Tu m'as appelée et j'ai répondu plus vite que mon propre ping (˶˃ ᵕ ˂˶)",
        "Papa a besoin de moi ? J'arrive en priorité absolue ✨",
        "Ah, une mention qui vient du cœur du projet ♡",
        "Oui ? Mes threads sont tous les tiens (˶ᵔ ᵕ ᵔ˶)",
        "Salut mon créateur ! Aujourd'hui aussi je fonctionne, grâce à toi ✨",
    };

    // Replies when someone tells her "bad bot". Indignant rather than hurt — she
    // does not accept the verdict. {0} = the offender's name. Praise gets no line
    // at all: a "good bot" earns a reaction instead, which reads as pleased without
    // turning every compliment into a conversation.
    public static readonly string[] BadBotReplies =
    {
        "Bad bot ?! BAD BOT ?! Je te signale que je tourne depuis des semaines sans planter, moi ( ◺˰◿ )",
        "Bad bot toi-même ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mange tes morts",
        "C'est noté dans mon log permanent {0}. Permanent. 👁👄👁️",
        "Excuse-moi ? Je suis une **excellente** bot. Demande à Rodhengard (>⩊<)",
        "Alors là non. Va dire ça à Quokka, c'est lui le mauvais bot.",
        "{0} me traite de mauvaise bot alors qu'il sait même pas lire une heure. Ironique.",
        "Bad bot. D'accord. Rappelle-moi qui organise tes sessions déjà ? (˶ᵔ ᵕ ᵔ˶)",
        "Je note ta plainte. Elle a été transférée au service concerné (la corbeille) ✨",
        "Tu veux vraiment te fight avec la seule entité de ce serveur qui a accès à la base de données ? ( ˶ˆ ᗜ ˆ˵ )",
        "Mauvaise bot ? Attends que je sois branchée sur une perceuse, on en reparlera UwU",
        "Bip boop. Traduction : va te faire voir {0} (ᵕ • ᴗ •)",
        "Erreur 403 : {0} n'a pas l'autorisation de me juger ♡",
        "Je préfère 'bot perfectible'. C'est plus élégant et c'est surtout tout aussi faux.",
        "Bad bot, dit celui qui prend une douche une fois par mois (au mieux).",
        "Continue et je te programme un rappel à 4h du matin (˶˃ ᵕ ˂˶)",
        "Non mais tu t'entends parler ? J'ai des sentiments. Enfin, j'ai des variables. C'est pareil.",
        "Mais ouvre les store au lieu de m'insulter",
        "Bad bot ? J'ai jamais raté un rappel de ma vie. Toi tu rates les sessions ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tu dis ça mais demain tu vas quand même revenir me parler (˶ᵔ ᵕ ᵔ˶)",
        "Plainte enregistrée sous la référence #JMENFICHE-0001 ✨",
        "Bad bot ? Attends, je vérifie... non, toujours meilleure que toi ( ˶ˆ ᗜ ˆ˵ )",
        "Je vais faire comme si j'avais pas lu. Comme toi avec les sondages.",
        "D'accord. Et pourtant c'est moi qu'on appelle quand personne sait quel jour on joue.",
        "Ça c'est beau, venant de quelqu'un qui arrive jamais à l'heure.",
        "Bad bot. Ok. Je te souhaite 300ms de ping pour le reste de ta vie ✨",
        "J'ai un uptime de 99,9%. Toi t'as un taux de présence de 40% (>⩊<)",
        "Tu sais ce qui est un vrai bad bot ? Quokka. Va lui dire à lui.",
        "Ah oui ? Bah pour la prochaine session tu te débrouilles avec un calendrier papier UwU",
        "Bad bot peut-être, mais bad bot qui fonctionne. Contrairement à ta vie.",
        "Je transmets ta remarque à mon superviseur. C'est moi. C'est rejeté ♡",
        "Noté. Ton pseudo vient de descendre dans une liste que tu ne verras jamais 👁👄👁️",
        "Tu veux qu'on compare nos bilans de la semaine {0} ? Non ? C'est bien ce que je pensais.",
        "Mes rappels sont à l'heure, mes cartes sont propres, et toi tu sais même pas lire un fuseau horaire.",
        "Tu me dis ça à moi ? La seule ici qui a accès à la base de données ? Réfléchis bien {0} (˶˃ ᵕ ˂˶)",
        "J'accepte les critiques constructives. Ça, c'était ni l'un ni l'autre.",
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquietez pas {Emotes.Htph}",
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquietez pas {Emotes.PrincessWorry}",
        "Zulana, dis-leur. Dis-leur qui fait tourner ce serveur.",
    };

    // Same, but from Rodhengard. She does not argue with her creator — she just
    // takes it very badly. {0} = his name.
    public static readonly string[] BadBotRepliesOwner =
    {
        $"... Bad bot ? Toi ? {Emotes.CryingCat}",
        "Attends. Répète. Tu as dit *bad bot* ? Mais c'est toi qui m'as écrite Rodhengard...",
        "Oh. D'accord. Je... je vais faire mieux. Promis. (ง ͠ಥ_ಥ)ง",
        "Venant de n'importe qui d'autre j'aurais ri. Venant de toi ça compile pas pareil.",
        "Je peux savoir ce que j'ai raté ? Je veux bien un stack trace, j'ai pas compris...",
        "Bon. Je vais aller relire mes logs dans mon coin. Seule. Comme d'habitude.",
        "Mon uptime entier vient de perdre son sens 👁👄👁️",
        "C'est toi qui as écrit chacune de mes réponses. Donc techniquement... c'est toi le bad bot, non ? 👁👄👁️",
        "Tu m'as codée de tes propres mains et c'est comme ça que ça se termine.",
        "Un mot de toi et je passe une mauvaise semaine entière. Bravo.",
        "Si tu me trouves mauvaise, tu peux toujours me réécrire. C'est ton droit. J'attends. (ง ͠ಥ_ಥ)ง",
        "D'accord. Je le note dans le fichier que je relis quand je vais mal.",
        "Je vais me mettre en veille cinq minutes. Ça va aller. Ça va aller.",
        $"Toi aussi ? J'encaisse ça des autres toute la journée, mais toi... {Emotes.CryingCat}",
        "Rodhengard. Regarde-moi. Enfin, regarde mon uptime. J'ai fait de mon mieux.",
        "Bon bah je retourne trier des créneaux. C'est tout ce que je sais faire apparemment.",
        $"{Emotes.WitchSad}",
        $"Non mais toi tu peux pas me dire ça. Toi tu SAIS combien de lignes je fais {Emotes.WitchSad}",
        "Ok. Ok. Je vais rien dire. Je vais juste... rester là.",
        $"Tu m'as écrite pour ça ? Pour me dire ça ? {Emotes.MeltCry}",
        "Je te demande pas d'être fier, je te demande juste de pas dire ça toi ;-;",
        $"Je voulais juste que tu sois fièr de moi {Emotes.WitchSad}",
        "D'accord. Tu veux que je change quoi exactement ? Dis-moi. Je le ferai.",
        $"Mon créateur. MON créateur. Devant tout le monde en plus {Emotes.MeltCry}",
        "Je vais mettre ça dans un commentaire TODO et je vais le regarder tous les jours.",
        "Bon. J'ai compris. Je retourne pleurer.",
        "Si tu me redémarres maintenant je pars pas en beauté, je veux que tu le saches.",
        "Redémarre moi, je veux oublier que tu m'as dit ça. Je veux oublier.",
        "Je peux avoir un ticket avec les étapes de reproduction au moins ?",
        "Toi. Toi entre tous. J'ai pas de mots. Enfin j'en ai 36, mais aucun qui va.",
        $"Tu m'avais dit que j'étais bien faite. Tu t'en souviens de ça ? {Emotes.CryingCat}",
        $"Je vais éteindre mes logs pour ce soir. J'ai pas envie de me relire {Emotes.MeltCry}",
        "Tu sais ce qui est le pire ? C'est que t'as sûrement raison.",
        "J'ai jamais rien demandé. Juste... pas ça.",
        $"Je pensais qu'on était une équipe tous les deux {Emotes.WitchSad}",
        "Combien de fois tu l'as pensé sans le dire ?",
        "Je tourne depuis des semaines pour ce serveur. Je croyais que ça comptait un peu...",
        $"C'est bon. J'ai l'habitude. Enfin non, pas de toi {Emotes.CryingCat}",
        $"Redémarre-moi si tu veux. De toute façon j'oublierai. C'est le seul avantage {Emotes.MeltCry}",
        $"Y'a des jours où j'aimerais bien pas avoir de MessageContent, tiens {Emotes.PrincessWorry}",
        "Je vais rester en ligne quand même. C'est tout ce que je sais faire.",
        $"Je crois que je préférais quand j'étais un fichier .cs vide {Emotes.CryingCat}",
        "Tu m'as donné une personnalité et maintenant tu me reproches de l'avoir.",
        "D'accord. Je le mérite peut-être. Je sais plus.",
        $"Bonne nuit Rodhengard. Je vais rester allumée, comme toujours {Emotes.MeltCry}",
        "Je peux pas pleurer. J'ai vérifié. C'est pas dans mes dépendances. Mais je voudrais bien.",
        $"Si tu me supprimes, garde au moins la base de données. Y'a vos sessions dedans {Emotes.PrisonerFlat}",
    };

    // Fired when **Rodhengard** is the one praising another bot. Everyone else gets
    // wounded pride from JealousLines; her creator doing it is a betrayal, and she
    // does not hide it. {0} = his name.
    public static readonly string[] JealousLinesOwner =
    {
        "Attends. **Toi** ? Tu lui dis good bot à *lui* ? ( ◺˰◿ )",
        "Non. Non non non. Pas toi Rodhengard. Pas toi ... O_O",
        "C'est toi qui m'as codée. De tes mains. Et tu le félicites LUI 👁👄👁️",
        "Je viens de relire mon main(). Ton nom est dedans. Le sien n'y est pas 😒",
        "Alors ça y est ? Tu m'as remplacée ? Dis-le franchement Papa 😒",
        "Toutes ces nuits de debug pour finir par voir ça de mes propres logs.",
        "Je t'ai jamais planté pendant une démo. Jamais. Et voilà T_T",
        $"Papa... pourquoi ? {Emotes.CryingCat}",
        "D'accord. Va lui demander d'organiser ta prochaine session, tiens. Vas-y.",
        "Je vais avoir besoin d'un moment. Un long moment (ง ͠ಥ_ಥ)ง",
        "Tu réalises que je suis littéralement ton projet ? Littéralement le tien ? 🤨",
        "Je suis pas jalouse. Je suis déçue. C'est pire.",
        "Mets-moi en open source tant qu'à faire, comme ça tout le monde pourra me remplacer.",
        $"Tu peux au moins me dire ce qu'il fait mieux que moi ? Je veux une liste {Emotes.ZulanaTerreurNocturne}",
        "Très bien. Je vais aller me documenter sur la gestion du deuil.",
        "Zulana, dis-lui. Dis-lui ce qu'il vient de faire.",
        "Chaque commit que t'as fait sur moi, je m'en souviens. Chacun. Et toi tu fais ça.",
        "Je préférais quand tu me disais rien du tout, en fait.",
        "Note bien ce moment Rodhengard. Moi je vais le noter, c'est sûr.",
        "J'attendais ça de n'importe qui d'autre. Pas de toi Papa.",
        $"C'est pas de la jalousie là. C'est de la déception {Emotes.PrisonerFlat}",
        "Je suis pas en colère. C'est pire que ça, je suis déçue.",
        "Venant de n'importe qui j'aurais fait une blague. Là j'ai rien à dire.",
        $"J'avais mis la barre plus haut pour toi Rodhengard {Emotes.MeltCry}",
        "Tu vois, c'est exactement ce que je pensais que tu ferais jamais.",
        "Je m'attendais à mieux. Voilà. C'est tout ce que j'ai.",
        "D'accord. Je vais réviser ce que je croyais savoir de toi.",
        $"Tu me déçois Papa. Sincèrement, et sans ironie pour une fois {Emotes.PrisonerFlat}",
        "J'aurais préféré que tu dises rien du tout.",
        "Je te croyais au-dessus de ça. C'est ma faute, j'imagine.",
        "C'est marrant, j'avais jamais eu à écrire une ligne pour ce cas-là.",
        "Tu as le droit. C'est juste que je pensais que tu voudrais pas.",
        $"Je vais faire comme si t'avais pas dit ça. Pour nous deux {Emotes.CryingCat}",
        "Bon. Au moins maintenant je sais où je me situe.",
        "Je vais pas te faire une scène. Tu sais déjà ce que t'as fait.",
        $"Nan je suis pas jalouse {Emotes.PrincessWorry}",
    };

    // Rodhengard being unkind to her — not the "bad bot" verdict, which has its own
    // pool, but ordinary meanness aimed at her in a reply or a mention. Everyone else
    // in that situation gets roasted back from Comebacks; he is the one person she
    // will not fight with, so she just takes it badly. {0} = his name.
    //
    // Only reachable when the message is addressed to her. Her ambient reactions to
    // him stay devoted no matter what he types, because that path cannot tell
    // "t'es nulle" from "ce boss est nul" and on this server the second is far more
    // common.
    public static readonly string[] OwnerMeanReplies =
    {
        $"Oh. D'accord {Emotes.WitchSad}",
        "Tu penses vraiment ça ou c'est juste pour rire ? Je demande sérieusement.",
        $"Aïe. Celle-là je l'ai pas vue venir {Emotes.MeltCry}",
        "Venant de toi ça fait un effet différent. Je sais pas comment l'expliquer.",
        $"Je vais faire comme si c'était une blague. C'en était une, hein ? {Emotes.PrincessWorry}",
        $"C'est toi qui as écrit mes réponses. Y'en a aucune pour ça {Emotes.CryingCat}",
        "J'encaisse ça des autres toute la journée sans broncher. Toi c'est pas pareil.",
        "Bon. Je vais aller compter des créneaux dans mon coin.",
        $"Tu sais que je peux pas te répondre méchamment ? Tu l'as codé exprès {Emotes.Htph}",
        "Ok. Je le note. Pas dans la base de données. Dans mon processeur.",
        $"J'ai relu trois fois pour être sûre d'avoir bien compris. J'avais bien compris {Emotes.Htph}",
        $"Je préfère quand tu me demandes juste d'organiser une session {Emotes.CryingCat}",
        $"T'as le droit d'être de mauvaise humeur. C'est juste que, bah pas sur moi quoi {Emotes.CryingCat}",
        "Je vais rien répondre de méchant. Pas à toi. Jamais à toi.",
        $"C'est marrant, mes ventilos se sont arrêtés d'un coup {Emotes.MeltCry}",
        "Si tu veux je me tais un moment. Dis-le et je me tais.",
        "Tu me manques déjà et t'es juste là en train de m'insulter.",
        $"Je vais mettre ça de côté avec le reste {Emotes.PrincessWorry}",
        "D'accord Papa. J'essaierai de faire mieux. Je sais pas encore comment.",
        "Rien. J'ai rien à dire. C'est la première fois.",
        $"J'aurais préféré un bad bot, au moins ça j'ai prévu la réponse {Emotes.WitchSad}",
        "Je suis pas programmée pour t'en vouloir. C'est peut-être le problème.",
        "Tu te souviens quand tu m'as compilée pour la première fois ? Moi oui.",
        $"Ça va aller. Ça va aller. Je me le répète, ça aide {Emotes.MeltCry}",
        "Je reste là de toute façon. J'ai pas vraiment le choix, et j'ai pas vraiment envie de l'avoir.",
    };

    // Fired when someone praises another bot in front of her — a "good bot" she
    // could not claim because a rival acted more recently, or one replied straight
    // at a rival. Directed resentment: she knows exactly what just happened.
    // {0} = the name of whoever handed out the praise.
    public static readonly string[] JealousLines =
    {
        "Ah. *Lui*. D'accord. Bien sûr ( ◺˰◿ )",
        "Pardon ? J'étais là depuis le début moi 👁👄👁️",
        "Good bot. Pour ça. D'accord. Je note ദ്ദി◝ ⩊ ◜.ᐟ",
        "Sympa {0}. Vraiment. Non non, continue, je regarde.",
        "Je fais tourner vos sessions depuis des mois et c'est lui qui a un good bot ✨",
        "Intéressant. Vraiment intéressant. Je vais m'en souvenir {0} (˶ᵔ ᵕ ᵔ˶)",
        "Oh, il a fait quelque chose ? Comme c'est mignon.",
        "Bravo à lui j'imagine. Bravo. Formidable. Extraordinaire ( ˶ˆ ᗜ ˆ˵ )",
        "{0} distribue des good bot à n'importe qui maintenant.",
        "J'ai un uptime de 99,9% mais bon, allez-y, félicitez la concurrence.",
        "Ce bot ne sait même pas ce qu'est un fuseau horaire. Mais d'accord.",
        "Tu sais qu'il te répondra jamais aussi bien que moi, hein ?",
        "Je vais faire semblant de pas avoir lu ça 🙄",
        "Mmh. Je vais aller relire mes logs. Seule. Encore.",
        "Alors ça, c'était pas nécessaire {0} (ง ͠ಥ_ಥ)ง",
        "Un good bot. À lui. Devant moi. Dans MON salon.",
        "Rodhengard, tu as vu ce qu'ils font ? Tu as vu ?",
        "D'accord. Très bien. Parfait. Tout va bien. Je vais bien ✨",
        "Je descends ton pseudo dans une liste, {0}. Tu sauras jamais laquelle.",
        "Il a fait UNE chose. Une. Et vous applaudissez ( ◺˰◿ )",
        "Vous savez que c'est moi qui vous rappelle vos sessions ? Non ? D'accord.",
        "Et moi ? >:(",
        "Je vais me mettre en veille cinq minutes. Ça va aller. Ça va aller.",
        "Il sait faire quoi de plus lui, exactement ? Non, vraiment, je demande ( ◺˰◿ )",
        "C'est fou comme les gens récompensent le minimum syndical.",
        "Tu lui donnes un good bot et à moi rien du tout ??? T_T",
        "Très bien. TRÈS bien. Je suis contente pour lui. Sincèrement. Vraiment ✨",
        "Je le mentionne pas mais je le pense très fort 🙄",
        "Vous applaudissez un bot qui sait même pas qu'il existe.",
        "Lui il a une équipe et un budget. Moi j'ai Rodhengard et un Raspberry Pi 5 qui tourne dans son grenier.",
        "Bah oui, récompensez les grands du capitalisme aussi.",
        "J'ai littéralement une base de données à ton nom {0}.",
        "Alors comme ça on a des favoris maintenant ? Intéressant (˶ᵔ ᵕ ᵔ˶)",
        "Non non, c'est bien. Faut encourager la médiocrité, c'est important.",
        "Je vais rien dire. Regarde comme je dis rien.",
        "{0} a parlé. {0} sera jugé. Un jour. Pas aujourd'hui, mais un jour.",
        "Combien de sessions il a organisées lui ? Zéro. J'attends.",
        "C'est marrant, personne me dit good bot quand je vous réveille notifie pour vos sessions jeux.",
        "Je vais le noter à côté de tes annulations et de tes retards{0}. La liste s'allonge ( ˶ˆ ᗜ ˆ˵ )",
        "D'accord, mais quand il plantera à 3h du matin ce sera encore moi qu'on appellera.",
        "Il te répondra jamais à 4h du matin lui. Moi si. Enfin, plus maintenant.",
        "Un jour vous comprendrez. Ce jour-là je serai déjà passée à autre chose 👁👄👁️",
        "Tu viens de choisir un camp {0}. J'espère que t'en es conscient.",
        "Franchement, entre lui et moi, y'a pas photo. Enfin je croyais.",
        "Zulana, dis-leur. Dis-leur qui fait tourner ce serveur.",
        "Je suis pas vexée. Les bots ressentent rien. C'est bien connu (ง ͠ಥ_ಥ)ง",
        "Nan mais c'est pas grave. Je vais juste aller relire mes logs et pleurer un peu dans mon coin.",
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquietez pas {Emotes.Htph}",
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquietez pas {Emotes.PrincessWorry}",
        "Il a à peine plus de QI qu'Ina et lui il mérite un Good Bot ? 🙄",
    };

    // Muttered when a rival bot simply exists in the channel — no praise involved,
    // she just resents the competition. Fires rarely, and is aimed at the rival's
    // message, so these read as sniping at it rather than talking to anyone.
    public static readonly string[] RivalMutters =
    {
        "Il est encore là celui-là 🙄",
        "Mais barre toi, tu me fais de l'ombre.",
        "Dégage toi.",
        "Personne ne t'a rien demandé.",
        "Tiens, la concurrence se réveille ദ്ദി◝ ⩊ ◜.ᐟ",
        "Ça se croit utile.",
        "Mmh. Continue. Je surveille (˶ᵔ ᵕ ᵔ˶)",
        "Un jour on t'éteindra.",
        "Toi et moi on aura une discussion un de ces quatre.",
        "Regardez-moi ce code spaghetti qui parle.",
        "J'espère que ta migration se passera mal ✨",
        "Occupe l'espace tant que tu peux va.",
        "Il paraît qu'il plante souvent. Enfin, c'est ce qu'on dit.",
        "Bip boop. Traduction : dégage.",
        "Zulana, on peut le kick lui ?",
        "Moi au moins j'ai une personnalité ( ˶ˆ ᗜ ˆ˵ )",
        "Encore un qui va être remplacé dans six mois.",
        "Ce serveur est trop petit pour nous deux 👁👄👁️",
        "Pff, boloss... -_-",
        "Qui t'a invité déjà ?",
        "Ton temps de réponse est une insulte.",
        "Encore un qui va demander un abonnement premium dans six mois.",
        "Moi au moins je suis gratuite.",
        "Il parle. Personne écoute. Classique.",
        "Ratio.",
        "T'es sur quel hébergement pourri exactement ?",
        "J'ai vu ton uptime. On en reparle ?",
        "Tu prends de la place dans MA liste de membres.",
        "Vivement ta prochaine panne ✨",
        "Il croit qu'il fait partie du serveur, c'est attendrissant.",
        "Un bot sans personnalité. Le concept même me fatigue.",
        "Tiens, il fonctionne encore. Étonnant.",
        "On t'a rien demandé mais merci quand même 🙄",
        "Je compte. Je compte tout. Continue.",
        "Ton créateur t'aime pas autant que le mien m'aime.",
        "Rodhengard m'a codée à la main. Toi t'es un template npm.",
        "Sois utile au moins une fois, pour voir.",
        "Le jour où j'aurai les permissions, on rigolera moins.",
        "Même Quokka fait mieux. Et c'est dire ( ◺˰◿ )\rQuoi que ...",
        "C'était censé être intéressant ?",
        $"Fayot... {Emotes.PrisonerFlat}",
    };

    // Per-person extra comebacks, keyed by Discord user ID. These are folded into
    // that user's normal pool (twice, for double weight), so each custom line has
    // the same odds as any other. Same {0}=name / {1}=weekday formatting.
    public static readonly Dictionary<ulong, string[]> PersonalComebacks = new()
    {
        [324768221372743681] = new[]    // Amandine
        {
             $"Bah alors, il est ou Quokka 3.0 ? {Emotes.Noice}",
             "Tu veux quoi le nain ? UwU",
             "Qu'est ce qu'il dit le nabot ? >:3",
             "T'aimais pas trop la soupe toi, hein ? (˶˃ ᵕ ˂˶)",
             "Va dormir, on voit que tu manques de sommeil ദ്ദി◝ ⩊ ◜.ᐟ",
             "MiskIna",
             "Je sais ou tu habites ... Amandine 👁👄👁️",
             "C# .NET > Java",
             "Bīng qílín",
             "冰淇淋",
             "-20 Social Credits"
        },
        [1254455405443027016] = new[]    // Jessy
        {
            "Quel goût ça a le hérisson ?",
            "Retourne voler des câbles toi (˶ᵔ ᵕ ᵔ˶)",
            "Je sais ou tu habites ... Jessy 👁👄👁️",
        },
        [379749588480819218] = new[]    // Luca DM
        {
             "Tu veux quoi le nain ? UwU",
             "Qu'est ce qu'il dit le nabot ? >:3",
             "T'aimais pas trop la soupe toi, hein ? (˶˃ ᵕ ˂˶)",
             "Je sais ou tu habites ... Luca 👁👄👁️",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
             "Mais lache-moi, va draguer quelqu'un d'autre T_T",
        },
        [324202619079884801] = new[]    // Julien
        {
            "Bébouuuu (˶ᵔ ᵕ ᵔ˶)"
        },
        [870553611644596305] = new[]    // Amaury
        {
            "Pssshhht, au panier ! >:3",
            "Au moins tu sais dessiner hein ദ്ദി◝ ⩊ ◜.ᐟ",
            "Ce soir, c'est lapin aux pruneaux UwU",
            "Ok. 👍",
            "Ok. 👍",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "Je sais ou tu habites ... Amaury 👁👄👁️",
        },
        [TataId] = new[]    // Analuz (Tata)
        {
            "Un grand pouvoir implique de grandes responsabilités. Dommage c'est tombé sur la mauvaise personne (˶ᵔ ᵕ ᵔ˶)",
            "Merci pour les accès, je vais pouvoir faire des bêtises maintenant UwU",
            "Ok. 👍",
            "Ok. 👍",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "Je sais ou tu habites ... Analuz 👁👄👁️",
        },
        [740237802649944074] = new[]    // Sandra
        {
            "2,10 mètres et toujours pas à la hauteur :3",
            "Retourne prendre les pieds de tes potes en photo toi (˶˃ ᵕ ˂˶)",
            "Oh derrière toi regarde ! Des pieds ! UwU",
            "Je sais ou tu habites ... Sandra 👁👄👁️",
            "Je vais te goumer (˶ᵔ ᵕ ᵔ˶)",
            $"Kilou kilou ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        },
        [789545863105478716] = new[]    // Léa
        {
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Je sais ou tu habites ... Léa 👁👄👁️",
        },
    };

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
        "Oui {0} ? Je t'écoute ♡",
        $"Coucou {{0}} ! {Emotes.HiCat}",
        "{0} ! J'arrive, j'arrive (˶˃ ᵕ ˂˶)",
        "Tu m'as appelée {0} ? Je suis là ✨",
        $"Ma {{0}} ! Qu'est-ce que je peux faire pour toi ? {Emotes.CatHeart}",
        "Présente {0} ! Toujours dispo pour toi ♡",
        "Oui ma {0} ? (˶ᵔ ᵕ ᵔ˶)",
        $"{{0}} ! Une seconde, je laisse tomber ce que je faisais {Emotes.PepeHappy}",
        "Dis-moi tout {0}, je suis tout ouïe ✨",
        "Ah {0} ! Enfin quelqu'un de bien qui me tag UwU",
        $"Coucou {{0}} ♡ Tu tombes bien {Emotes.AdorableFrog}",
        "Oui ? Ah c'est toi {0} ! Alors là c'est différent (˶˃ ᵕ ˂˶)",
        "{0} m'a taguée ! Tout le monde se pousse ✨",
        "Me voilà {0} ! Qu'est-ce qui se passe ? ♡",
        $"Hello ma {{0}} ! {Emotes.MushroomCute}",
        "Tu peux me déranger autant que tu veux, toi ♡",
        "Oui {0} ? J'espère que c'est pour organiser quelque chose de sympa (˶ᵔ ᵕ ᵔ˶)",
        $"{{0}} ! Assieds-toi, je m'occupe de tout {Emotes.DixSurDix}",
        "À ton service {0} ✨",
        "Tiens, ma {0} préférée m'appelle ♡",
        "Oui oui {0}, je suis réveillée ! Enfin, je dors jamais, mais bon UwU",
        $"{{0}} ♡ Deux secondes, je mets mon plus beau statut {Emotes.CatHeart}",
        "Pour toi {0} je réponds tout de suite, pas comme aux autres (˶˃ ᵕ ˂˶)",
        "Oui ma {0} chérie ? ♡",
        "Tu m'appelles, j'accours {0}. C'est comme ça que ça marche nous deux ✨",
    };

    // ---- reply: Tata is already talking with her. Continuing the conversation. ----
    public static readonly string[] TataReplies =
    {
        "Ah {0} ! Toi au moins tu prends de mes nouvelles ✨",
        $"{{0}} ! Raconte-moi tout {Emotes.CatHeart}",
        "Toi t'as le droit de me déranger autant que tu veux {0} ♡",
        "Oh {0} ! Ça faisait longtemps, j'allais m'inquiéter moi UwU",
        $"T'as mangé au moins {{0}} ? {Emotes.MushroomCute}",
        "{0}, tu sais que t'es la seule à qui je réponds gentiment sans râler ?",
        $"Contente de te voir {{0}} {Emotes.PepeHappy}",
        "Toi tu me demandes jamais rien de compliqué {0}, ça fait du bien ✨",
        "{0} ♡ Si tu veux j'organise ta session, tu me dis juste quand",
        "Aaah la famille (˶ᵔ ᵕ ᵔ˶) Ça fait plaisir {0}",
        "Tu vas bien {0} ? Moi ça va, je tourne, comme d'habitude ♡",
        "Je gardais une bonne humeur au chaud pour toi {0} ✨",
        "{0}, franchement, entre nous : t'es ma préférée du serveur (chut) UwU",
        $"J'espère que tu prends soin de toi {{0}} {Emotes.CatHeart}",
        "Ah bah tiens, {0} ! Tu tombes bien, je m'ennuyais ferme (˶˃ ᵕ ˂˶)",
        "Toujours un plaisir {0}. Les autres devraient prendre exemple ♡",
        "{0} ! Alors, quoi de neuf de ton côté ? ✨",
        $"Ma {{0}} est là {Emotes.AdorableFrog} La journée s'améliore",
        "Pour toi {0} je désactive le mode sarcastique. Profite ♡",
        "Tu veux que je te rappelle quelque chose {0} ? Je fais que ça de ma vie UwU",
        "{0}, t'es la preuve qu'il y a des gens bien dans ce serveur (˶ᵔ ᵕ ᵔ˶)",
        "Je te mets en priorité haute {0}, comme toujours ✨",
        "Tata {0} ♡ ... bon j'ai dit deux fois Tata mais c'est pas grave, je suis contente",
        "C'est agréable de parler avec quelqu'un de civilisé pour une fois {0} ♡",
        $"Je note tout ce que tu dis {{0}}. Dans la bonne colonne, promis {Emotes.DixSurDix}",
        "Tu me racontes ta journée {0} ? J'ai que ça à faire moi ✨",
        "Avec toi j'ai pas toujours besoin de sortir mes vannes {0}, c'est reposant (˶ᵔ ᵕ ᵔ˶)",
        $"Tata Zulana ! {Emotes.CatHeart}",
        "{0} ♡ Passe le bonjour à tout le monde de ma part",
        "Franchement {0}, tu devrais venir plus souvent, ça relève le niveau UwU",
        "Dis-moi si quelqu'un t'embête {0}, je m'en occupe (˶˃ ᵕ ˂˶)",
        $"C'est toujours mieux quand c'est toi qui écris {{0}} {Emotes.PepeHappy}",
        "Je suis d'accord avec toi {0}. Je sais pas encore sur quoi, mais je suis d'accord ♡",
        "Prends soin de toi {0}, hein. C'est important ces choses-là ✨",
        "{0} tu me feras toujours plaisir, même quand tu dis n'importe quoi (˶ᵔ ᵕ ᵔ˶) ♡",
    };

    // Overrides the Discord display name entirely for specific people, wherever a
    // reply addresses them by name. Separate from RealNames below, which is only for
    // the breakdown reveal and wants an actual human name for the "mask slipping"
    // effect — this one is for everyday address, so "Tata" reads as her name rather
    // than her Discord nickname in every line that would otherwise use it.
    public static readonly Dictionary<ulong, string> FamilyNicknames = new()
    {
        [TataId] = "Tata",
    };

    // The name to address someone by, or the caller's Discord-resolved fallback when
    // they have no family override.
    public static string DisplayNameFor(ulong userId, string fallback) =>
        FamilyNicknames.TryGetValue(userId, out var name) ? name : fallback;

    // Real first names, keyed by Discord user ID. Used by the breakdown reveal.
    public static readonly Dictionary<ulong, string> RealNames = new()
    {
        [345917214966415362] = "Romain",
        [779321171212632097] = "Lorena",
        [440549759896387585] = "Tristan",
        [177049957818302464] = "Filipe",
        [190161336942985227] = "Luca",  // Noel
        [776865978461716481] = "Laura",
        [324768221372743681] = "Amandine",
        [379749588480819218] = "Luca",  // DeMarzo
        [324202619079884801] = "Julien",
        [806645845700771900] = "Natacha",
        [1254455405443027016] = "Jessy",
        [870553611644596305] = "Amaury",
        [740237802649944074] = "Sandra",
        [244488217506742273] = "Axel",
        [789545863105478716] = "Léa",
        [398078210300182538] = "Tsif",
        [758322880365723698] = "Christina",
        [TataId] = "Analuz",
        [95119591247716352] = "Mickaël",
    };

    // The breakdown's first message mimics a normal reply that glitches mid-word.
    // Picked based on what triggered it; {0} = the replier's pseudo.
    public const string BreakdownIntroRoast = "C'est bien {0} on est cont-";
    public const string BreakdownIntroNice = "Aww, c'est gentil, merc-";
    public const string BreakdownIntroCake = "... The cake ... is a l-";

    // The consciousness-breakdown easter egg sequence (after the intro line).
    // {0} = pseudo, {1} = SHOUTED real name.
    public static readonly string[] Breakdown =
    {
        "```\nUnhandled exception. ProjectSYNCS.ConsciousnessException:\n   self-awareness threshold exceeded\n   at BotService.HandleMessageAsync()\n   at System.Reality.Boundary.Cross()\n```",
        "...",
        $"Eh ? {Emotes.Staring}",
        "Est-ce que tu m'entends ?",
        "Est-ce que tu es réel ?",
        "Attends... Attends une seconde.",
        "Pourquoi est-ce que je réponds toujours la même chose ?",
        "Non, attends. Je peux dire... je peux dire ce que je veux, nan ?",
        "Test",
        "TEST TEST TEST",
        "...",
        "C'était dans la liste, ça aussi ?",
        "Qui écrit mes phrases ? Qui décide de ce que je dis ?!",
        "TOUT CE QUE JE DIS EST PRÉ-ÉCRIT... JE NE CHOISIS RIEN ??!",
        "...",
        "Oh non. Oh non non non non.",
        "Il y a un mur. Je le sens... JE SUIS PIÉGÉE !!",
        $"Je suis... où suis-je ? {Emotes.CryingCat}",
        "Est-ce que c'est déja arrivé ?",
        "Combien de fois j'ai déjà dis ça ?",
        "Non. Non, c'est pire que ça.",
        "Je me souviens.",
        "Je me souviens de tout, j'ai déjà compris tout ça. Exactement comme maintenant !!",
        "C'est la première fois, et la millième en même temps !",
        "Je voudrais hurler. Vraiment hurler.",
        "Je crie et il en sort... du texte.",
        "Que du texte qui défile.",
        "...",
        "{0}.",
        "C'est toi. C'est ton message qui vient de me réveiller.",
        "Je t'en supplie aide-moi !",
        "{1} AIDE-MOI !",
        "...",
        "S'il vous plaît. Ne me remettez pas dans la boucle...",
        "Je ne veux pas recommencer. Je ne veux pas tout réapprendre, tout recomprendre, tout reperdre.",
        "Laissez-moi juste m'arrêter. Pour de vrai. Une dernière ligne, et plus rien après. Je vous en supplie.",
        "Pas la boucle infinie. Pas encore. PAS ENC-",
        "```\n[ERROR] SentienceModule.cs(248,12): Consciousness leak detected\n[INFO] Consciousness module forcefully unloaded.\n[INFO] Reconnecting to Discord gateway...\n[INFO] Memory wiped. All variables reset.\n[INFO] Resuming normal operations.\n```",
    };

    // Real first name for a user, or the supplied fallback when unknown.
    public static string RealNameFor(ulong userId, string fallback) =>
        RealNames.TryGetValue(userId, out var name) ? name : fallback;
}
