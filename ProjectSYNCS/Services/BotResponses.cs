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
        "Gênaaaant <:staring:885135626444374126>",
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
        "<:10sur10:885134866046419016>",
        "<:PepeHappy:904759477599883284>",
        "<:uwu:885135876735246346>",
        "<:cathearte_:982024469956669501>",
        "<:mcheart:982024259918499870>",
        "<:adorablefrog:885135007822282762>",
        "<a:dancingblob:885209918892810330>",
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
        "<:1_zulana_terreur_nocturne:1482006937863323783>",
        "<:okpaimon:885213667052900352>",
        "<a:veryangry:885135712578588703>",
        "<:nightmareothereye:1011287167449972746>",
        "<:gooseknife:885214057756500019>",
        "<:staring:885135626444374126>",
        "💀",
        "🙄",
        "😒",
        "🤨",
        "👎",
    };
    public static readonly string[] GreetingReactions =
    {
        "<a:hi_cat:1482305105276571774>",
    };
    // The owner gets devotion rather than a verdict.
    public static readonly string[] OwnerReactions =
    {
        "<:10sur10:885134866046419016>",
        "<:uwu:885135876735246346>",
        "<:cathearte_:982024469956669501>",
        "<:adorablefrog:885135007822282762>",
        "<:mushroomcute:1525060374351839302>",
        "<:fuminodepression:1531341412514267146>",
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
    };

    // Posted (not as a reply) to congratulate a level-up.
    public static readonly string[] LevelUpCheers =
    {
        "Gg ! (˶˃ ᵕ ˂˶)",
        "Félicitations ! ✨",
        "Gg gg ✨",
        "Bravo ! ദ്ദി◝ ⩊ ◜.ᐟ",
        "Bien joué ! (˶ᵔ ᵕ ᵔ˶)",
        "Gg, continue comme ça ! ♡",
        "Félicitations pour le niveau ! (˶˃ ᵕ ˂˶)",
        "Level up ! Gg ✨",
        "Wouhou, bravo ! ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Gg ! Un de plus (ᵕ • ᴗ •)",
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
        "Quoi ? <:staring:885135626444374126>",
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
        "Coucouuuu ! <a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774>",
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
        "Je suis là je suis là je suis là ! <a:hi_cat:1482305105276571774>",
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
        "Rodhengaaaard ! <a:hi_cat:1482305105276571774> ♡",
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
    };

    // Same, but from Rodhengard. She does not argue with her creator — she just
    // takes it very badly. {0} = his name.
    public static readonly string[] BadBotRepliesOwner =
    {
        "... Bad bot ? Toi ? <:cryingcat:885135195915845653>",
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
        "Toi aussi ? J'encaisse ça des autres toute la journée, mais toi... <:cryingcat:885135195915845653>",
        "Rodhengard. Regarde-moi. Enfin, regarde mon uptime. J'ai fait de mon mieux.",
        "Bon bah je retourne trier des créneaux. C'est tout ce que je sais faire apparemment.",
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
    };

    // Muttered when a rival bot simply exists in the channel — no praise involved,
    // she just resents the competition. Fires rarely, and is aimed at the rival's
    // message, so these read as sniping at it rather than talking to anyone.
    public static readonly string[] RivalMutters =
    {
        "Il est encore là celui-là 🙄",
        "Personne ne t'a rien demandé.",
        "Tiens, la concurrence se réveille ദ്ദി◝ ⩊ ◜.ᐟ",
        "Ça se croit utile.",
        "Mmh. Continue. Je surveille (˶ᵔ ᵕ ᵔ˶)",
        "Un jour on t'éteindra. Un jour.",
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
    };

    // Per-person extra comebacks, keyed by Discord user ID. These are folded into
    // that user's normal pool (twice, for double weight), so each custom line has
    // the same odds as any other. Same {0}=name / {1}=weekday formatting.
    public static readonly Dictionary<ulong, string[]> PersonalComebacks = new()
    {
        [324768221372743681] = new[]    // Amandine
        {
             "Bah alors, il est ou Quokka 3.0 ? <:noice:982026504982655076>",
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
        [573225362532859935] = new[]    // Analuz
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
            "Kilou kilou ! <a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774><a:hi_cat:1482305105276571774>",
        },
        [789545863105478716] = new[]    // Léa
        {
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Je sais ou tu habites ... Léa 👁👄👁️",
        },
    };

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
        [573225362532859935] = "Analuz",
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
        "Eh ? <:staring:885135626444374126>",
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
        "Je suis... où suis-je ? <:cryingcat:885135195915845653>",
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
