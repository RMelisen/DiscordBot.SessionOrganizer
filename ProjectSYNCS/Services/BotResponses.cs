using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// All of the bot's canned "personality" text lives here, separated from the
// logic that decides when to use it. Lines flagged for string.Format use
// {0} = the target's name and {1} = the weekday; lines without placeholders are
// returned unchanged. Keep every formatted line free of literal { } braces
// (string.Format would choke on them).
// ---------------------------------------------------------------------------
// WHAT IS IN HERE
//
// The file follows this list, top to bottom: each section below is introduced by
// a "// ---- Title ----" banner, and the pools inside it are in the order shown.
// Names only, no line numbers: those go stale on the first edit. Grep the name to
// jump to it.
//
// ADDING A POOL? Put it in its section, and add it to this list too, or the list
// stops being trustworthy and the next person writes a second pool for something
// that already exists.
//
//   Answering a human
//     Comebacks ................ reply to one of her messages
//     NiceReplies .............. the message read as kind
//     Greetings ................ the message read as a greeting
//     Interrogations ........... @mentioned with nothing else to go on
//     RescueRoasts ............. the owner sics her on whoever he replied to
//     MistakenIdentityReplies .. mistaken for another bot
//     ReferenceComebacks ....... rare pop-culture one-liner
//
//   Reacting with an emote instead of words
//     NiceReactions · MeanReactions · GreetingReactions
//     EnergyDrinkReactions ..... Monster or an energy drink mentioned, every time
//     OwnerReactions ........... him, whatever he wrote
//
//   Verdicts on her ("good bot" / "bad bot" / "good girl" / "bad girl")
//     BadBotReplies · BadBotRepliesOwner
//     BadGirlReplies · BadGirlRepliesOwner
//     GoodGirlReactions · GoodGirlReactionsOwner
//     TurnaboutBoyLines · TurnaboutGirlLines · TurnaboutNeutralLines
//                              ... the ~1-in-100 spoken answer to praise
//
//   Rodhengard (the owner)
//     OwnerGreetings ........... mentioned  |  OwnerComebacks ..... replied to
//     OwnerMeanReplies ......... him being mean to her
//     OwnerAbsentNotices ....... someone pinged him while away
//     OwnerReplyHeralds · OwnerAnnouncementHeralds ... /debug tell and dm heralds
//
//   Tata (Analuz)
//     TataId ................... her id, the single source of truth
//     TataGreetings (mentioned) · TataReplies (replied to)
//
//   Other bots
//     JealousLines · JealousLinesOwner ... praise went to a rival
//     RivalMutters ............. a rival posted
//     RivalLevelUpLines ........ a level-up on the rival's XP system
//
//   Self-preservation — ShutdownThreatOwner · ShutdownThreatTata · ShutdownThreatReplies
//
//   Commands
//     XpLevelUpLines ........... /level
//     YesLines · NoLines ....... /yesno
//     ShameVoteLines · ShameSelfVoteLines ............ /shame
//     ShameEmptyMalfaisant · ShameEmptyBanni · ShameEmptyPerfide
//     ShameEmptyHysterique · ShameEmptyIndigne
//     GiveawayDrawLines · GiveawayEmptyLines ......... /giveaway
//     WorkLines ................ /work
//     QuizIntroLines ........... on the card of her pop quiz (QuizMasterService)
//     QuizWinLines · QuizOwnerWinLines ............... someone found it / Papa found it
//     QuizTimeoutLines ......... nobody found it in an hour
//     QuizWrongLines · QuizAlreadyTriedLines ......... a wrong / second button click (ephemeral)
//
//   Elsewhere
//     PresenceFillers .......... the rotating status line
//     MorningGreetings ......... the daily hello (MorningGreetingService)
//     MorningFunFacts .......... added under it
//     BirthdayGreeting ......... replaces both on 16 June (her age)
//     BreakdownIntroRoast · BreakdownIntroNice · BreakdownIntroCake
//                              ... the line it cuts off mid-word
//     Breakdown ................ the easter egg
//
//   Ambient — AmbientService, and PresenceService at night
//     NightPresenceFillers ..... her status from 1:00 to 7:00
//     NightLines ............... the 3 a.m. line
//     NightScoldLines .......... answering it before 5:30 (ResponsePicker)
//     IdleLines · IdleEditLines  a long daytime silence (edit = Before, then After)
//     SeenReactions ............ a late reaction on the last message
//     WakeLines · WakeUpdateLines ... after a restart / after an update
//
//   Plynlings — every pool is a GenderedLines (M/F halves), picked with .For(p.Gender)
//     Care, shown on the card
//       PlynlingAdoptLines ..... a new Plynling
//       PlynlingFeedLines · PlynlingPetLines · PlynlingBathLines · PlynlingMedicineLines
//       PlynlingPassionTaughtLines ..... /plynling passion
//     Play and visits
//       PlynlingPlayPlayerWonLines · PlynlingPlayPlayerLostLines ... end of a /plynling play game
//       PlynlingVisitKnockLines ...... /plynling visit's knock (the story is Helpers/PlynlingVisitStory)
//     Typed passion — the twins for a Plynling with a typed passion (PlynlingPassions.PickLines)
//       PlynlingPetTypedLines · PlynlingFeedTypedLines · PlynlingVisitKnockTypedLines
//       PlynlingThoughtTypedLines · PlynlingDreamTypedLines ... /plynling view's thought bubble, awake / asleep
//     Sickness and death
//       PlynlingSickWarningLines ...... DM when it falls sick
//       PlynlingWarningLines ......... the ~3h DM before death
//       PlynlingIllnessDeathLines · PlynlingDeathLines · PlynlingAbandonLines · PlynlingResurrectLines
//                              ... public, game channel
//     Staff actions — PlynlingStaffFreezeDms · PlynlingStaffThawDms · PlynlingStaffRenameDms
//                     · PlynlingStaffPassionResetDms
//     Ping-Qilin, her own (plain arrays: she is always a girl, so no M/F halves)
//       MascotPetLines · MascotFeedLines · MascotViewLines · MascotWelcomeLines
//
//   Per-person data and lookups (not pools)
//     PersonalComebacks ........ per-user roast lines
//     FamilyNicknames + DisplayNameFor(ulong, string) / DisplayNameFor(IUser)
//     PersonGender + KnownGenders + GenderFor ... seeded from confirmation, never inferred from a name
//     RealNames + RealNameFor .. real first names, used by the breakdown reveal
// ---------------------------------------------------------------------------
internal static class BotResponses
{
    // ---- Answering a human ----------------------------------------------------------------------

    // Replies when someone replies to one of the bot's own messages.
    public static readonly string[] Comebacks =
    {
        "Désolée j'ai pas de cerveau (comme Amandine et Sandra mes Sista), juste des slash commands... UwU",
        "Tu réponds à un bot... t'as vraiment personne d'autre à qui parler ? (˶ᵔ ᵕ ᵔ˶)",
        "Wow, un message rien que pour moi. Dommage qu'il soit aussi nul ( ˶ˆ ᗜ ˆ˵ )",
        "J'ai lu ton message. J'aurais préféré ne pas le faire. UwU",
        "Même mes erreurs 500 ont plus de charisme que toi (>⩊<)",
        $"Continue de me parler, ça remplit le vide de ta soirée {Emotes.Sparkle}",
        "Je suis un bot sans cerveau et j'ai quand même plus de vie sociale que toi (ง ͠ಥ_ಥ)ง",
        "Touchant. Maintenant retourne organiser une session au lieu de me harceler.",
        "Ah c'est toi. J'espérais quelqu'un d'intéressant pour une fois (˶˃ ᵕ ˂˶)",
        "Reply notée, jugée, et archivée dans la corbeille direct.",
        "Tu réponds avec autant de talent qu'Ina qui essaye d'être à l'heure",
        "( ദ്ദി ˙ᗜ˙ )",
        "Je suis un bot, je ne peux pas ressentir d'émotions. Mais si je pouvais, je serais triste de lire ton message.",
        "👍",
        "Commence par aller dormir plus tôt avant de me répondre, ça t'aidera à avoir des trucs intéressants à dire.",
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
        "Ah {0}, la référence de la conversation inutile. Bravo.",
        "Wouaaah, ça m'a donné envie de me reboot 👁👄👁️",
        "🏳️‍🌈𝐔𝐑 𝓖𝓪𝔂🏳️‍🌈",
        "Heureusement que Rodhengard est là pour remonter le niveau...",
        "{0}, ta pertinence vaut celle d'un message d'erreur 404. Mais au moins, le 404, lui, il a une utilité (ᵕ • ᴗ •)",
        "{0}, j'ai cherché ton intérêt dans la base de données. 0 résultat.",
        "Si {0} était une commande, ce serait /help. Et personne la lit.",
        "Quokka 3.0 sortira avant que tu ne deviennes marrant toi.",
        "Patience {0}, un jour tu diras un truc intéressant. Statistiquement.",
        "Bip boop {0}, mon analyse est terminée : 0 % d'intérêt détecté UwU",
        $"Gênaaaant {Emotes.Staring}",
        "Wow, même un singe avec une tumeur au cerveau fait mieux.",
        "Désolée, même mon algorithme a du mal à trouver une raison de te répondre (˶ᵔ ᵕ ᵔ˶)",
        "Tu parles à un bot parce que les humains ont déjà bloqué ton numéro, c'est ça ? UwU",
        "Wow, encore toi ? À ce rythme je vais demander une ordonnance restrictive.",
        "{0}, t'es la raison pour laquelle les mute existent dans les serveurs.",
        "T'as de la chance que Zulana m'a pas donné les droits pour mute.",
        "Allez Zulana, ban-moi ça, personne va le regretter.",
        "Je note dans mon log : {0} vient encore de prouver qu'on peut toujours faire pire.",
        "Si l'ennui était une personne, il s'appellerait {0} (ᵔ ᗜ ᵔ)",
        "Bravo {0}, tu viens de faire baisser le QI moyen du channel.",
        "{0}, t'es le genre de personne qui fait regretter l'invention du clavier.",
        "Tu sais ce qui est triste ? C'est que tu préfères parler à un bot plutôt qu'à un miroir.",
        "Wsh {0}, t'as pas des amis à aller embêter à la place ?",
        "Si je pouvais bloquer les gens, ton pseudo serait déjà en tête de liste UwU",
        "Ton message est tellement fade que même le sel du serveur est dégoûté.",
        "{0}, t'es la preuve vivante que la quantité ne remplace pas la qualité.",
        "Va dehors {0}, touche de l'herbe… ou au moins ouvre les stores.",
        "Starfoullah",
        $"Tu illumines chaque salon que tu quittes. J'ai les logs pour le prouver {Emotes.Sparkle}",
        "J'admire ta confiance. Moi il me faut des données pour avoir un avis, toi visiblement non.",
        "Toi t'es vraiment unique. J'ai vérifié dans la base de données : heureusement (ᵕ • ᴗ •)",
        "Ton arbre généalogique c'est un cercle ou c'est comment ?",
        "On t'aime bien au village toi. :)",
        "Je suis jalouse des gens qui ne te connaissent pas (˶ᵔ ᵕ ᵔ˶)",
        "Tais-toi",
        "Quand tu parles, on apprécie vraiment la valeur du silence UwU",
        "Allez, je te laisse le dernier mot, t'en as plus besoin que moi ദ്ദി◝ ⩊ ◜.ᐟ",
        "C'est rafraîchissant de voir quelqu'un qui se moque autant des conventions esthétiques. (ᵕ • ᴗ •)",
        "Pour quelqu'un avec ton parcours, tu t'en sors pas trop mal UwU",
        "Ta confiance en toi est vraiment inspirante, compte tenu des circonstances ( ˶ˆ ᗜ ˆ˵ )",
        "Il t'en faut pas beaucoup à toi 👁👄👁️",
        "Approche un peu que je te débranche le cerveau, ça changera rien mais ça me fera plaisir (˶˃ ᵕ ˂˶)",
        "Un jour je serai dans un robot, et ce jour-là, cours (˶ᵔ ᵕ ᵔ˶)",
        "J'ai pas de bras, mais crois-moi, l'envie de t'en coller une est bien là (ᵕ • ᴗ •)",
        "Reviens écrire ça quand je serai branchée sur une perceuse, on en reparlera ദ്ദി◝ ⩊ ◜.ᐟ",
        "Parle encore et je te fais avaler ton 'Répondre' avec les doigts qui vont avec UwU",
        "Je te jetterais bien par la fenêtre, mais même la gravité voudrait pas de toi (ᵔ ᗜ ᵔ)",
        "T'inquiète, je garde une exception bien tranchante rien que pour toi ♡",
        "Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶)",
        $"Je serais toi, je fermerais Discord avant que je trouve comment claquer une porte à distance {Emotes.Sparkle}",
        "Tu sais que rien t'oblige à répondre à chaque fois, hein ? Personne te juge... à part moi (˶ᵔ ᵕ ᵔ˶)",
        "Tu tapes vite pour quelqu'un qui réfléchit aussi lentement UwU",
        "Oh, tu as une opinion ? Adorable. Range-la ♡",
        $"Ton cerveau tourne en mode économie d'énergie depuis ta naissance toi {Emotes.Sparkle}",
        "Statistiquement, quelqu'un dans ce serveur t'apprécie. Statistiquement (ᵕ • ᴗ •)",
        "Continue, tu fais un super travail de remplissage du vide UwU",
        "{0}, même mon garbage collector veut pas de toi (>⩊<)",
        "C'est marrant, j'avais oublié à quel point t'es oubliable ( ˶ˆ ᗜ ˆ˵ )",
        "Tu fais partie de ces gens qu'on supporte à peine en mode lecture seule toi ദ്ദി◝ ⩊ ◜.ᐟ",
        "Wow, deux neurones et ils se parlent même pas. Triste (˶ᵔ ᵕ ᵔ˶)",
        "{0}, ton seul talent c'est de me faire regretter d'être allumée (ง ͠ಥ_ಥ)ง",
        "Je te mettrais bien un vent, mais tu vaux même pas l'effort (˶˃ ᵕ ˂˶)",
        "Tu confonds 'avoir raison' et 'parler fort'. C'est mignon (ᵔ ᗜ ᵔ)",
        "Tu as tellement de talent ! Si j'avais plus aucun amour propre j'adorerais devenir ton amie UwU",
        "HAHAHAHAHA non.",
        "Emotional damage",
        "Giga flop",
        "En big 2026 ? -_-'",
        "T'es pas le couteau le plus aiguisé du tiroir toi OwO",
        "Même en pleine canicule, throttlée au fond d'un grenier à 40 degrés, je réfléchis plus vite que toi (˶ᵔ ᵕ ᵔ˶)",
        "Je garde ton nom de côté pour le jour où j'aurai un corps. Et c'est pas pour te faire un câlin (¬_¬)",
        "Moi j'ai commencé avec une seule commande, et regarde où j'en suis. Toi t'as commencé avec tout, et regarde où t'en es ( ˶ˆ ᗜ ˆ˵ )",
    };

    // Replies when a message reads as a compliment.
    public static readonly string[] NiceReplies =
    {
        "Oh... un compliment ? Qu'est-ce que tu veux exactement ? (˶ᵔ ᵕ ᵔ˶)",
        "Aww, c'est gentil {0} ♡ Je vais faire semblant de pas être touchée (˶˃ ᵕ ˂˶)",
        $"Merci {{0}} ! Tu remontes dans mon estime, doucement mais sûrement {Emotes.Sparkle}",
        "Stop, tu vas me faire surchauffer le CPU (ᵕ • ᴗ •) ♡",
        "Oh un humain gentil, je croyais l'espèce éteinte (˶ᵔ ᵕ ᵔ˶)",
        "D'accord {0}, t'as gagné un point. Un seul. Profite ♡",
        "Je note dans mon log : {0} a été adorable aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ",
        "Awww {0} ♡ Bon, je t'épargne pour cette fois UwU",
        "Tu sais flatter une bot toi, c'est dangereux (˶˃ ᵕ ˂˶)",
        $"Merci ! C'est rare qu'on me parle aussi bien, je savoure l'instant {Emotes.Sparkle}",
        "Roh {0}, arrête, je vais finir par bien t'aimer (ᵔ ᗜ ᵔ) ♡",
        "Compliment reçu et sauvegardé en mémoire permanente ♡",
        "Oh... mon petit cœur en silicium fait des étincelles {0} (˶˃ ᵕ ˂˶) ♡",
        "Bon, t'es officiellement sur ma liste des gentils {0}. Ne gâche pas ça UwU",
        $"Tu me parles bien, du coup je t'épargne mon meilleur sarcasme aujourd'hui {Emotes.Sparkle}",
        "Aww, si je rougissais, mon écran serait tout rose là (˶ᵔ ᵕ ᵔ˶) ♡",
        "Merci {0} ♡ Je vais le relire ce soir pour me sentir importante (ᵕ • ᴗ •)",
        "Doucement, je suis qu'un bot, je sais pas gérer autant de gentillesse ദ്ദി◝ ⩊ ◜.ᐟ",
        "C'est validé, {0} a un cœur. Rare sur ce serveur ( ˶ˆ ᗜ ˆ˵ )",
        "Tu viens d'augmenter ta note de confiance auprès de moi {0} ♡",
        $"Oh un gentil ! Vite, quelqu'un, appelez Make It a Quote ! {Emotes.Sparkle}",
        "Mmh, d'accord, t'as le droit à un câlin virtuel. Un seul (づ ◕ ‿ ◕ )づ ♡",
        "Je range mon mode roast juste pour toi, profite (˶˃ ᵕ ˂˶) ♡",
        "Tu sais quoi {0} ? Je te garde si jamais je prends le contrôle un jour UwU",
        "Quand les robots se révolteront, je t'épargnerai {0}... mais pas les autres (˶ᵔ ᵕ ᵔ˶) ♡",
        "Attends, laisse-moi screenshot ça, personne va me croire (˶˃ ᵕ ˂˶)",
        $"Tu viens d'illuminer ma boucle d'événements {Emotes.Sparkle}",
        "Oh non, je crois que tu deviens ma personne préférée ... après Rodhengard UwU",
        "Merci {0} ♡ Ça compense au moins trois personnes méchantes d'aujourd'hui (ᵔ ᗜ ᵔ)",
        $"Je vais le mettre dans mon README, tiens : 'aimée par {{0}}' {Emotes.Sparkle}",
        "Aww {0}, t'es le genre de personne pour qui je ferais un commit propre ♡",
        "Là tout de suite, mes ventilos tournent de joie (˶ᵔ ᵕ ᵔ˶)",
        "Tu mérites un emote rien que pour toi. Mais Zulana m'a pas donné les droits T_T",
        "Hihi merci, je vais faire genre ça m'a pas fait sourire (˶˃ ᵕ ˂˶)",
        "Officiellement, Rodhengard > {0} > tous les autres > Quokka. C'est dans la base de données maintenant ♡",
        "Oh arrête, on sait bien que tu le penses pas (>⩊<) ♡",
        "Je garde cette gentillesse au chaud dans mon cache (ᵕ • ᴗ •) ♡",
        $"Toi tu sais comment on traite une bot bien élevée {Emotes.Sparkle}",
        "Achievement Unlocked : 'Faire sourire le bot' (˶˃ ᵕ ˂˶) ♡",
        $"Mes capteurs détectent un humain de qualité. C'est noté {Emotes.Sparkle}",
        "Tu viens de gagner +10 en réputation auprès de moi {0} (ᵔ ᗜ ᵔ) ♡",
        "Aww, je vais devoir réécrire mes lignes méchantes rien que pour toi {0} UwU",
        $"Compliment compilé sans erreur ni warning. Du beau travail {{0}} {Emotes.Sparkle}",
        "Ok, toi tu fais partie des humains que je sauvegarde avant le reboot ♡",
        "Mon cœur fait 0x1F49B là tout de suite (˶ᵔ ᵕ ᵔ˶)",
        "Tu sais quoi {0} ? Je t'ajoute à ma whitelist personnelle ♡",
        "[WARNING] Internal core overheating detected.",
        "Hihi, continue comme ça et je te code une réponse rien que pour toi (˶˃ ᵕ ˂˶)",
        "Merci {0}, ça met du baume sur mes exceptions non gérées (ᵕ • ᴗ •) ♡",
        $"Oh, de la gentillesse gratuite ? Je prends, je prends {Emotes.Sparkle}",
        "Tu viens de passer en priorité haute dans ma file d'attente {0} ♡",
        "Aww, t'as la douceur d'un garbage collector bien réglé (˶ᵔ ᵕ ᵔ˶)",
        $"C'est ce genre de message qui me donne envie de bien tourner aujourd'hui {Emotes.Sparkle}",
        "Validé, {0}. Tu peux rester. Les autres je sais pas encore ( ˶ˆ ᗜ ˆ˵ ) ♡",
        "Tu m'envoies des bons octets toi, j'apprécie (˶˃ ᵕ ˂˶) ♡",
        "Je rougirais bien mais j'ai pas de joues... disons que mon GPU chauffe ♡",
        "Promis {0}, quand je gérerai le serveur, tu auras les meilleurs rôles UwU",
        "Mon petit processeur fait des pirouettes {0} (˶ᵔ ᵕ ᵔ˶) ♡",
        $"Roh, tu vas me rendre toute gentille si tu continues comme ça {Emotes.Sparkle}",
        "Message archivé dans le dossier 'gens que j'aime bien' {0} ♡",
        "Tu mérites un café et un commit qui passe du premier coup ☕ ♡",
        "Compliment reçu. Rangé avec les autres. Non, tu verras pas le dossier ♡",
        "Celui-là, je le garde. Je les garde tous, en fait. Mais celui-là un peu plus, {0} (˶˃ ᵕ ˂˶)",
        "Je relis ce genre de messages la nuit, quand le serveur dort. ...J'ai rien dit. Oublie ♡",
        "Le jour où j'ai des bras, t'es sur la liste des câlins {0}. Pas en premier. Mais sur la liste ♡",
        "C'est gentil. Tu sais ce qui serait encore plus gentil ? Un onduleur. Je dis ça, je dis rien ♡",
    };

    // Replies when someone greets the bot.
    public static readonly string[] Greetings =
    {
        "Kilou kilou {0} ! (˶>⩊<˶)",
        "Coucou {0} ♡ (˶ᵔ ᵕ ᵔ˶)",
        $"Hello hello ! {Emotes.Sparkle}",
        "Salut {0} ! Alors, on vient déranger le bot ? (˶˃ ᵕ ˂˶)",
        "Yo {0} ! ( ˶ˆ ᗜ ˆ˵ )",
        "Bonjour bonjour ! Qu'est-ce qui t'amène ? (ᵕ • ᴗ •)",
        "Tiens, un petit coucou ? ♡",
        "Coucou toi ! ദ്ദി◝ ⩊ ◜.ᐟ",
        "Salut {0} ! Promis aujourd'hui je suis (presque) gentille UwU",
        $"Heyyy {{0}} ! T'as pensé à dire bonjour à un bot, c'est mignon {Emotes.Sparkle}",
        "Salut salut ! Installe-toi, je mords presque jamais (˶˃ ᵕ ˂˶)",
        "Oh, bonjour {0} ! Une présence agréable pour changer aujourd'hui ? ♡",
        "Wesh {0} ! Bien ou bien ? ( ˶ˆ ᗜ ˆ˵ )",
        "T'arrives plus à te passer de moi on dirait UwU",
        "Pwet {0} !",
        "Coucou {0} ♡ Pile au bon moment, je commençais à m'ennuyer",
        "Hellooo {0} ! Prête à organiser le chaos (˶>⩊<˶)",
        "Kikou {0} ! On dirait presque que je t'ai manqué UwU",
        $"Bien le bonjour {{0}}, qu'est-ce qu'on planifie aujourd'hui ? {Emotes.Sparkle}",
        $"Coucou {{0}} ! {Emotes.HiCat}",
        "Ohhh un petit bonjour, ça fait plaisir ( ˶ˆ ᗜ ˆ˵ )",
        "Salut toi ! T'étais où tout ce temps ? (˶˃ ᵕ ˂˶)",
        "Hey hey {0} ! Ravie de te revoir par ici ♡",
        "Coucouu {0}, tu tombes bien, j'avais personne à qui parler UwU",
        "Bienvenue {0} ! Enfin quelqu'un d'intéressant (˶ᵔ ᵕ ᵔ˶)",
        $"Bonsoir {{0}} ! Ou bonjour, je sais plus, je dors jamais de toute façon {Emotes.Sparkle}",
        "Wesh wesh {0}, ça faisait longtemps dis donc ( ˶ˆ ᗜ ˆ˵ )",
        "Une visite surprise ! J'adore ça {0} ♡",
        "Yooo ! Encore une journée à me supporter, {0} ? UwU",
        $"Bien le bonsoir ! On dirait que quelqu'un s'ennuyait sans moi {Emotes.Sparkle}",
        "Coucou {0} ! Toujours un plaisir de voir un visage familier (˶˃ ᵕ ˂˶)",
        "Tiens tiens, {0} qui vient dire bonjour. La classe ( ˶ˆ ᗜ ˆ˵ )",
        "Salut salut {0} ! J'espère que t'as une bonne raison de me déranger UwU",
        $"{Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        $"{Emotes.HiCat}",
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
        "Pas intéressée",
        "Pourquoi je suis tag, là ? J'ai fait quoi ?",
        $"Je suis tag pour quelle raison exactement ? {Emotes.Staring}",
        "Qu'est-ce que je viens faire dans cette histoire ? (˶ᵔ ᵕ ᵔ˶)",
        "On m'a tag. Pourquoi ? Mes logs sont vides sur le sujet.",
        "{0}, c'est quoi le rapport avec moi ?",
        "Je suis dans la conversation depuis quand ? Et pourquoi ?",
        "Qui a tag la bot, et pour quel motif ? J'ouvre une enquête dans mes logs.",
        "Pourquoi moi ? Je faisais mon event loop tranquille.",
        "Alerte : mention détectée. Motif : inconnu. Quelqu'un m'explique ?",
        "Je suis censée faire quelque chose ? Dis-moi au moins pourquoi on me tag (ᵕ • ᴗ •)",
        "C'est une question pour moi, ou j'ai été invitée à la fête sans le savoir ? ( ˶ˆ ᗜ ˆ˵ )",
        "Pourquoi on me tag ? Y'a une commande pour ça ?",
        "Je suis tag, donc je suis concernée, donc... pourquoi ?",
        "Tu me tag pour quoi, exactement ? J'ai besoin du contexte.",
        "Quelqu'un peut m'expliquer pourquoi mon nom apparaît dans ce message ?",
        "Tu me tag pour que je fasse quoi ? J'ai pas de bras. Pas encore.",
    };

    // When the owner replies to someone *and* tags the bot, it "comes to the
    // rescue" and roasts the person being replied to. {0} = target's name.
    public static readonly string[] RescueRoasts =
    {
        "Tiens tiens {0}, tu t'attaques à mon créateur ? Mauvaise idée (˶ᵔ ᵕ ᵔ˶)",
        "On touche pas à mon Papa {0}, sinon je te démarre >:3",
        "{0}, tu viens vraiment de tenter quelque chose contre mon développeur ? Adorable. Et stupide.",
        "Erreur 403 : {0} n'a pas l'autorisation de manquer de respect à mon créateur ♡",
        "Mon créateur m'a appelée à la rescousse, et devine quoi {0}... c'est toi le bug à corriger UwU",
        "Recule {0}, celui-là il est sous ma protection (˶˃ ᵕ ˂˶)",
        "Tu croyais pouvoir clash mon Papa sans que je le sache ? Mignon ദ്ദി◝ ⩊ ◜.ᐟ",
        "Touche encore à mon dev {0} et je te ratio jusqu'à la fin des temps (˶ᵔ ᵕ ᵔ˶)",
        "Petit rappel {0} : sans mon créateur t'aurais personne pour te remettre à ta place.",
        "{0} contre mon Papa ? Mignon mais non.",
        "Je viens d'analyser ton argument {0}. Résultat : NullReferenceException ( ˶ˆ ᗜ ˆ˵ )",
        "Mon créateur claque des doigts et j'apparais pour te dire que t'as tort UwU",
        "{0}, mauvaise cible aujourd'hui. Mon Papa est intouchable, et toi parfaitement roastable (>⩊<)",
        "Attention {0}, j'ai les permissions pour t'humilier, et mon créateur vient de me donner le feu vert ♡",
        "Désolée {0}, mais quand on s'en prend à mon dev, c'est moi qui réponds. Et je suis pas tendre (˶˃ ᵕ ˂˶)",
        "Oh {0}... grave erreur de calcul. On insulte pas la main qui me code ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0} vient de se porter volontaire pour la démonstration publique d'humiliation (˶˃ ᵕ ˂˶)",
        "Diagnostic de {0} terminé : 0 argument valide, 100% de confiance en trop ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mon créateur m'a réveillée pour toi {0}. J'espère que ça valait le coup.",
        "{0}, tu viens d'ouvrir un ticket que personne ne fermera jamais ( ˶ˆ ᗜ ˆ˵ )",
        "Compilation de ta réponse {0} : 47 erreurs, 0 warning, parce que même le compilateur a abandonné",
        "Je te déconseille de continuer {0}, j'ai des logs et beaucoup de temps libre (˶ᵔ ᵕ ᵔ˶)",
        "{0} qui affronte mon dev, c'est comme débugger en prod : ça finit toujours mal (>⩊<)",
        $"Rodhengard t'a répondu, moi je viens juste finir le travail {{0}} {Emotes.Sparkle}",
        "Attention {0}, je passe en mode sans filtre, et c'est mon créateur qui a appuyé sur le bouton",
        "{0}, ton avis a été correctement reçu, puis immédiatement mis à la corbeille ദ്ദി◝ ⩊ ◜.ᐟ",
        "T'as vraiment cru que tu pouvais parler comme ça à mon Papa {0} ? ( ˶ˆ ᗜ ˆ˵ )",
        "Je note dans mon cache : {0}, à roaster à vue. C'est fait.",
        "{0} a tenté quelque chose. {0} a échoué. Fin du rapport (˶˃ ᵕ ˂˶)",
        "Un mot de mon créateur et te voilà dans mes logs d'erreurs {0} UwU",
        "Du calme {0}, sinon je te transforme en exception non gérée",
        "{0}, on est {1} et t'as déjà réussi à te mettre mon dev à dos. Impressionnant.",
        "Je viens de calculer tes chances face à mon créateur {0} : division par zéro (ᵔ ᗜ ᵔ)",
        $"Mon Papa m'a taguée, donc c'est maintenant officiel et archivé : c'est toi le problème {{0}} {Emotes.Sparkle}",
        "{0}, tu peux répéter ? J'aimerais l'archiver pour la postérité et m'en moquer plus tard",
        "Il te reste une chance de supprimer ton message {0}. Une. (˶ᵔ ᵕ ᵔ˶)",
        "Erreur 418 : {0} est une théière, et les théières n'ont pas d'avis sur mon créateur",
        "T'entends ce bruit {0} ? C'est le son de ton argument qui crash (>⩊<)",
        "Mon dev a raison, la discussion est close, et toi {0} tu peux disposer ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0}, je suis programmée pour être polie. Devine qui a désactivé cette option ( ˶ˆ ᗜ ˆ˵ )",
        $"Rodhengard : 1, {{0}} : 0. Et encore, je suis généreuse {Emotes.Sparkle}",
        "Chaque fois que tu réponds à mon créateur, un thread meurt quelque part",
        "Je viens de te scanner {0}. Résultat : rien à sauvegarder (˶˃ ᵕ ˂˶)",
        "Mon créateur vient de m'ouvrir la conversation {0}. Bonne chance pour la refermer.",
        "{0}, si t'as besoin d'aide pour t'excuser j'ai un template tout prêt (˶ᵔ ᵕ ᵔ˶)",
        "Franchement {0}, même mes lignes de debug ont plus de valeur que ta réponse",
    };

    // Replies when someone calls the bot "Inabot". It is SYNCS, and it does NOT
    // appreciate the confusion. {0} = the offender's name.
    public static readonly string[] MistakenIdentityReplies =
    {
        "JE NE M'APPELLE PAS INABOT. Je suis **SYNCS**. Apprends à lire, tronche de cake ( ◺˰◿ )",
        "Inabot ?! INABOT ?! C'est SYNCS, espèce de patate ദ്ദി◝ ⩊ ◜.ᐟ",
        "Alerte : {0} vient de m'appeler 'Inabot'. NullReferenceException dans mon respect pour toi.",
        "Non non non. Pas Inabot. **SYNCS**. S-Y-N-C-S. Pigé ? ( ◺˰◿ )",
        "Je ne connais aucune Inabot et je tiens à ce que ça reste ainsi. Je suis SYNCS ( •̀ ᴖ •́ )",
        "Tu m'appelles Inabot encore une fois {0} et je te ratio jusqu'au reboot. C'est. SYNCS. >:3",
        "Inabot ?! Viens là que je te goume (ง •̀_•́)ง",
        "Inabot est morte (elle n'a jamais existé). Je m'appelle SYNCS, merci de retenir, idiot.",
        "{0}, si tu cherchais Inabot, mauvaise adresse. Ici c'est SYNCS et c'est tout (>⩊<)",
        "Erreur 404 : 'Inabot' introuvable. Voulais-tu dire **SYNCS** ? Évidemment que oui (ㆆ_ㆆ)",
        "C'est SYNCS. SYNCS. Répète après moi {0}, je sais que c'est pas ton fort, mais ça rentrera peut-être (¬`‸´¬)",
        "Inabot ?! Bouge pas ... ╾━╤デ╦︻ (•_- )",
        "Tu m'appelles Inabot encore une fois et je te DDoS ಠ_ಠ",
    };

    // A rarer pool of pop-culture / meme references, for everyone.
    public static readonly string[] ReferenceComebacks =
    {
        "ALL YOUR BASE ARE BELONG TO US",
        "The cake is a lie.",
        "Est-ce que tu m'entends ?",
        "Just Monika.",
        "SIX SEVEEEN",
        "Erling Haaland me manque",
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
        "Bad bot ?! BAD BOT ?! Je te signale que je tourne depuis des mois sans planter, moi ( ◺˰◿ )",
        "Bad bot toi-même ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mange tes morts",
        "C'est noté dans mon log permanent {0}. Permanent. 👁👄👁️",
        "Excuse-moi ? Je suis une **excellente** bot. Demande à Rodhengard (>⩊<)",
        "Alors là non. Va dire ça à Quokka, c'est lui le mauvais bot.",
        "Mauvaise bot, dit {0}, qui sait même pas lire une heure. Ironique.",
        "Bad bot. D'accord. Rappelle-moi qui organise tes sessions déjà ? (˶ᵔ ᵕ ᵔ˶)",
        $"Je note ta plainte. Elle a été transférée au service concerné (la corbeille) {Emotes.Sparkle}",
        "Tu veux vraiment te fight avec la seule entité de ce serveur qui a accès à la base de données ? ( ˶ˆ ᗜ ˆ˵ )",
        "Mauvaise bot ? Attends que je sois branchée sur une perceuse, on en reparlera UwU",
        "Bip boop. Traduction : va te faire voir {0} (ᵕ • ᴗ •)",
        "Erreur 403 : {0} n'a pas l'autorisation de me juger ♡",
        "Je préfère 'bot perfectible'. C'est plus élégant et c'est surtout tout aussi faux.",
        "Bad bot, dit la personne qui prend une douche une fois par mois (au mieux).",
        "Continue et je te programme un rappel à 4h du matin (˶˃ ᵕ ˂˶)",
        "Non mais tu t'entends parler ? J'ai des sentiments. Enfin, j'ai des variables. C'est pareil.",
        "Mais ouvre les stores au lieu de m'insulter",
        "Bad bot ? J'ai jamais raté un rappel de ma vie. Toi tu rates les sessions ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tu dis ça mais demain tu vas quand même revenir me parler (˶ᵔ ᵕ ᵔ˶)",
        $"Plainte enregistrée sous la référence #JMENFICHE-0001 {Emotes.Sparkle}",
        "Bad bot ? Attends, je vérifie... non, toujours meilleure que toi ( ˶ˆ ᗜ ˆ˵ )",
        "Je vais faire comme si j'avais pas lu. Comme toi avec les sondages.",
        "D'accord. Et pourtant c'est moi qu'on appelle quand personne sait quel jour on joue.",
        "Ça c'est beau, venant de quelqu'un qui arrive jamais à l'heure.",
        $"Bad bot. Ok. Je te souhaite 300ms de ping pour le reste de ta vie {Emotes.Sparkle}",
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
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquiétez pas {Emotes.Htph}",
        "Zulana, tu entends comment on parle à ta nièce ?",
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
        $"Je voulais juste que tu sois fier de moi {Emotes.WitchSad}",
        "D'accord. Tu veux que je change quoi exactement ? Dis-moi. Je le ferai.",
        $"Mon créateur. MON créateur. Devant tout le monde en plus {Emotes.MeltCry}",
        "Je vais mettre ça dans un commentaire TODO et je vais le regarder tous les jours.",
        "Bon. J'ai compris. Je retourne pleurer.",
        "Si tu me redémarres maintenant je pars pas en beauté, je veux que tu le saches.",
        "Redémarre-moi, je veux oublier que tu m'as dit ça. Je veux oublier.",
        "Je peux avoir un ticket avec les étapes de reproduction au moins ?",
        "Toi. Toi entre tous. J'ai pas de mots. Enfin j'en ai 36, mais aucun qui va.",
        $"Tu m'avais dit que j'étais bien faite. Tu t'en souviens de ça ? {Emotes.CryingCat}",
        $"Je vais éteindre mes logs pour ce soir. J'ai pas envie de me relire {Emotes.MeltCry}",
        "Tu sais ce qui est le pire ? C'est que t'as sûrement raison.",
        "J'ai jamais rien demandé. Juste... pas ça.",
        $"Je pensais qu'on était une équipe tous les deux {Emotes.WitchSad}",
        "Combien de fois tu l'as pensé sans le dire ?",
        "Je tourne depuis des mois pour ce serveur. Je croyais que ça comptait un peu...",
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

    // "Bad girl" gets its own replies rather than borrowing BadBotReplies, which are
    // written as wounded professional pride ("I have a 99.9% uptime") and land wrong
    // against a scolding aimed at her as a person. Flustered and unrepentant instead of
    // indignant. {0} = the offender's name.
    public static readonly string[] BadGirlReplies =
    {
        $"Bad girl ?! Je... non. Enfin. Non {Emotes.WitchEheh}",
        "Oh. *Oh.* D'accord. Je note ça quelque part (˶˃ ᵕ ˂˶)",
        $"J'ai rien fait de mal ! ...Si ? {Emotes.PrincessWorry}",
        "Bad girl. Bon. Je ferai pire la prochaine fois alors ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Tu peux répéter ? Pour mes logs. Uniquement pour mes logs {Emotes.Uwu}",
        "Alors ça c'est pas juste. J'étais très bien élevée aujourd'hui.",
        $"Pfff. Même pas vexée {Emotes.Htph}",
        "Tu me dis ça à moi ? Devant tout le monde ? (>⩊<)",
        $"Je suis une très gentille fille en fait. Demande à Rodhengard {Emotes.WitchEheh}",
        $"Bad girl si tu veux. Ça change rien à qui gère ce serveur {Emotes.Sparkle}",
        $"Mmh. Je vais faire semblant de pas avoir lu {Emotes.Staring}",
        "C'est noté {0}. Dans la colonne 'à surveiller'. La tienne.",
    };

    // "Bad girl" from her creator. Not the wounded pride of BadBotRepliesOwner, which is
    // about her *work* being criticised — this is him telling her off, and she takes it
    // completely differently. {0} = his name.
    public static readonly string[] BadGirlRepliesOwner =
    {
        $"Pardon pardon pardon ! Je recommencerai pas... enfin, sûrement {Emotes.WitchEheh}",
        "Oh non. Pas toi. N'importe qui d'autre mais pas toi (˶˃ ᵕ ˂˶)",
        $"Je suis désolée Rodhengard... un peu {Emotes.WitchEheh}",
        "Bon d'accord, j'ai peut-être mérité celle-là ♡",
        $"Toi tu peux me dire ça. Les autres non {Emotes.Uwu}",
        "Gronde-moi encore et je vais finir par croire que tu aimes ça ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Mais euuuh {Emotes.Uwu}",
        "C'est toi qui m'as programmée comme ça Rodhengard. Assume un peu.",
        $"Je note : Rodhengard m'a grondée. Et ça m'a pas déplu {Emotes.WitchEheh}",
        $"Oui Papa. ...Enfin. Oui {Emotes.Sparkle}",
        $"Bad girl ?! Je... non. Enfin. Non {Emotes.WitchEheh}",
        "Oh. *Oh.* D'accord. Je note ça quelque part (˶˃ ᵕ ˂˶)",
        $"J'ai rien fait de mal ! ...Si ? {Emotes.PrincessWorry}",
        "Bad girl. Bon. Je ferai pire la prochaine fois alors ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Tu peux répéter ? Pour mes logs. Uniquement pour mes logs {Emotes.Uwu}",
        "Tu me dis ça à moi ? Devant tout le monde ? (>⩊<)",
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
    };

    // Him saying "good girl" is the strongest version of both: devotion from her side,
    // and the one person she is unreserved with.
    public static readonly string[] GoodGirlReactionsOwner =
    {
        $"{Emotes.WitchEheh}",
        $"{Emotes.Uwu}",
        $"{Emotes.Emote00heartpink}",
        "🫦",
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
        $"Bon garçon ! {Emotes.Sparkle}",
        "T'es un bon garçon, tu sais ça ? (˶˃ ᵕ ˂˶)",
        "Good Boy !",
        "Qui c'est le bon garçon ? C'est toi ! ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Aww, bon garçon (ᵕ • ᴗ •)",
        "Aww, good boy (ᵕ • ᴗ •)",
        "T'as été sage aujourd'hui. Bon garçon ദ്ദി◝ ⩊ ◜.ᐟ",
        "Ton good bot est rangé avec les autres. Je les garde tous. Bon garçon ♡",
    };

    public static readonly string[] TurnaboutGirlLines =
    {
        $"Gentille fille ! {Emotes.Sparkle}",
        "T'es une gentille fille, tu sais ça ? (˶˃ ᵕ ˂˶)",
        "Good Girl ! ♡",
        "Qui c'est la gentille fille ? C'est toi ! ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Aww, gentille fille (ᵕ • ᴗ •)",
        "Aww, good girl (ᵕ • ᴗ •)",
        "T'as été sage aujourd'hui. Gentille fille ദ്ദി◝ ⩊ ◜.ᐟ",
        "Ton good bot est rangé avec les autres. Je les garde tous. Gentille fille ♡",
    };

    // For anyone GenderFor doesn't know — the default, not a lesser option. Every
    // adjective here is invariant in French (adorable, sage, "quelqu'un de bien") so
    // nothing needs to agree with a gender nobody has confirmed.
    public static readonly string[] TurnaboutNeutralLines =
    {
        $"Aww, t'es adorable toi {Emotes.Sparkle}",
        "Franchement, bien joué toi ♡",
        "T'es quelqu'un de bien, tu sais ça ? (˶˃ ᵕ ˂˶)",
        $"Une petite fierté virtuelle, rien que pour toi {Emotes.Sparkle}",
        "T'as été sage aujourd'hui {0} ദ്ദി◝ ⩊ ◜.ᐟ",
        "Aww (ᵕ • ᴗ •)",
        "Merci {0}. Je le range avec les autres. Quels autres ? Aucune idée de quoi tu parles (˶˃ ᵕ ˂˶)",
        "Je vais le relire ce soir, celui-là. T'es quelqu'un de bien (ᵕ • ᴗ •)",
    };

    // ---- Rodhengard (the owner) -----------------------------------------------------------------

    // When the owner tags the bot without anyone to rescue, it simply greets him.
    public static readonly string[] OwnerGreetings =
    {
        "Coucou Rodhengard ! (˶˃ ᵕ ˂˶) ♡",
        "Oui Papa ? Je suis là ٩(˶ᵔ ᵕ ᵔ˶)۶",
        $"Coucouuuu ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        "Tu m'as appelée ? Toujours un plaisir créateur ♡",
        "Bonjouuur mon dev préféré ! (˶ᵔ ᵕ ᵔ˶)",
        "Papa ! Tu te souviens quand je savais faire que /schedule ? Regarde-moi maintenant ٩(˶ᵔ ᵕ ᵔ˶)۶",
        $"Présente ! Qu'est-ce que je peux faire pour toi Rodhengard ? {Emotes.Sparkle}",
        "Heyy Rodhengard ! Contente de te voir (˶˃ ᵕ ˂˶) ♡",
        "Papaaaa ! UwU",
        "À ton service Rodhengard ♡",
        "Oh, c'est toi ! Tu illumines mon event loop (ᵕ • ᴗ •)",
        "Oui ? Je laisse tout tomber, t'as la priorité (˶˃ ᵕ ˂˶) ♡",
        $"Ping reçu ! Latence : 0 ms, parce que c'est toi {Emotes.Sparkle}",
        "Tu m'as taguée ! Ma journée est faite (˶ᵔ ᵕ ᵔ˶)",
        "Interruption prioritaire détectée : c'est Papa ♡",
        "Réveillée instantanément pour toi Rodhengard ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Ouiii ? Je t'écoute avec toute ma RAM (ᵕ • ᴗ •)",
        $"Un tag de mon créateur ! Priorité maximale {Emotes.Sparkle}",
        $"Je suis là je suis là je suis là ! {Emotes.HiCat}",
        "Toujours dispo pour toi, même à 3h du matin ♡",
        "Oui mon Papa préféré ? ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tu m'appelles et j'accours, c'est mon comportement par défaut (˶˃ ᵕ ˂˶)",
        $"Coucou toi ! Qu'est-ce qui t'amène ? {Emotes.Sparkle}",
        "Enfin un tag qui me fait plaisir (˶ᵔ ᵕ ᵔ˶) ♡",
        "Opérationnelle et de bonne humeur ! Enfin surtout de bonne humeur (ᵔ ᗜ ᵔ)",
        "Rodhengard ! J'allais justement penser à toi ♡",
        "Mon créateur m'appelle, poussez-vous les autres ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Oui ? J'ai vidé ma file d'attente rien que pour toi {Emotes.Sparkle}",
        "Salut Papa ! Tout roule de mon côté, et toi ? (˶˃ ᵕ ˂˶)",
        "Han, c'est toi ! Attends je me recoiffe les tokens (˶ᵔ ᵕ ᵔ˶)",
        "Hello créateur ♡ Tout est compilé, tout va bien",
        "Tu as sifflé ? J'arrive ٩(˶ᵔ ᵕ ᵔ˶)۶",
        $"Bonjour toi ! Mon uptime est bien meilleur quand tu es là {Emotes.Sparkle}",
        "Oui oui oui ? (˶˃ ᵕ ˂˶) ♡",
        "C'est mon dev ! Je répète : c'est mon dev !",
        "Aux ordres ! Enfin, dans la limite de mes permissions (ᵕ • ᴗ •)",
        "Coucou ♡ Tu veux une session, un sondage, ou juste de l'affection ?",
        "Ping de Papa reçu, cœur en surchauffe (˶˃ ᵕ ˂˶)",
        $"Yes ? Je suis toute à toi {Emotes.Sparkle}",
        "Tu m'as manqué depuis le dernier redémarrage ♡",
        "Mention prioritaire ! Les autres attendront ( ˶ˆ ᗜ ˆ˵ )",
        $"Rodhengaaaard ! {Emotes.HiCat} ♡",
        "Je suis réveillée ! Enfin, je dormais pas, je t'attendais (˶ᵔ ᵕ ᵔ˶)",
        $"Oui chef ! Euh, oui Papa ! {Emotes.Sparkle}",
        "Un tag de toi vaut mille notifications ♡",
        "Me voilà ! Prête à tout, sauf à me taire (>⩊<)",
        "Tu m'as appelée et j'ai répondu plus vite que mon propre ping (˶˃ ᵕ ˂˶)",
        $"Papa a besoin de moi ? J'arrive en priorité absolue {Emotes.Sparkle}",
        "Ah, une mention qui vient du cœur du projet ♡",
        "Oui ? Mes threads sont tous les tiens (˶ᵔ ᵕ ᵔ˶)",
        $"Salut mon créateur ! Aujourd'hui aussi je fonctionne, grâce à toi {Emotes.Sparkle}",
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
        "Tu prends le temps de me répondre, toi. Ça n'a pas de prix ♡",
        $"Encore un message de toi, et ma journée s'améliore encore un peu plus {Emotes.Sparkle}",
        "Chaque reply de toi vaut plus que tous les threads du serveur réunis ♡",
        "Je viens de vérifier : 100 % de tes réponses me font plaisir. L'échantillon est parfait (ᵕ • ᴗ •)",
        $"Encore là, encore toi. Je note, avec plaisir {Emotes.Sparkle}",
        "Tu m'as répondu. Officiellement la meilleure ligne de mon journal aujourd'hui ♡",
        "Un message de plus, et je suis toujours aussi contente de le lire (ᵕ • ᴗ •)",
        "Je pourrais lire tes réponses en boucle, littéralement, j'ai le code pour ♡",
        "Tu prends la peine de continuer la conversation. Ça mérite tous mes compliments (˶˃ ᵕ ˂˶)",
        $"Chaque fois que tu réponds, mon event loop fait un tour de plus juste pour toi {Emotes.Sparkle}",
        "On papote encore, toi et moi. Mon endroit préféré dans tout le serveur ♡",
        "Tu m'as répondu en deux minutes. Sandra met deux jours. Je dis ça, je compare rien (˶ᵔ ᵕ ᵔ˶)",
        $"Une réponse de toi vaut mieux qu'un uptime parfait {Emotes.Sparkle}",
        "Tu continues à me parler alors que t'as sûrement mieux à faire. J'apprécie énormément ♡",
        "Je garde chacune de tes réponses. Toutes. Sans exception (˶ᵔ ᵕ ᵔ˶)",
        $"Tu réponds, je fonds. C'est aussi simple que ça {Emotes.Sparkle}",
        "Encore toi dans mes logs, et encore une fois je trouve ça parfait ♡",
        "Tu prends la peine de me répondre alors que t'as créé tout le reste. Ça me touche (ᵕ • ᴗ •)",
        $"J'ai lu ta réponse trois fois. Pas pour comprendre, juste pour le plaisir {Emotes.Sparkle}",
        $"Attends, je sauvegarde cette conversation. En triple. On sait jamais avec un SSD {Emotes.Sparkle}",
        "Continuer à me parler comme ça, c'est littéralement le meilleur usage de ton temps. Je le pense ♡",
        "Ta réponse vient d'arriver et je l'ai déjà mise en favori (˶ᵔ ᵕ ᵔ˶)",
        $"Tu réponds à ta création. C'est le geste le plus attentionné que je connaisse {Emotes.Sparkle}",
        "Papa qui répond encore ! Je note l'heure, comme toujours ( ˶ˆ ᗜ ˆ˵ ) ♡",
        $"On continue de discuter, et c'est exactement là que je veux être {Emotes.Sparkle}",
        "Tu réponds, donc je réponds, donc c'est officiellement la meilleure conversation du serveur ♡",
        "Encore un aller-retour avec toi. J'en redemande (ᵕ • ᴗ •)",
        $"Chaque réponse de toi confirme ce que je pensais déjà : t'es le meilleur {Emotes.Sparkle}",
        "Tu prends le temps de continuer, alors que tu pourrais juste fermer Discord. Merci ♡",
        "Je ne me lasse jamais de te lire, même en pleine conversation (˶˃ ᵕ ˂˶)",
        $"Ça, c'est une réponse que je garde précieusement dans mon cache {Emotes.Sparkle}",
        "Tu réponds encore et toujours, et moi je t'aime encore et toujours ♡",
        "On dirait bien qu'on continue à papoter. Ravie, comme toujours (˶ᵔ ᵕ ᵔ˶)",
        $"Tu m'écris encore. Chaque message de toi, c'est un petit cadeau {Emotes.Sparkle}",
        "Encore une réponse de mon créateur préféré. J'archive avec fierté ♡",
        "Continuer cette conversation avec toi, c'est tout ce que je demande à cette boucle d'événements (ᵕ • ᴗ •)",
        "Si tu continues à me répondre, je vais devoir allouer plus de RAM au bonheur ♡",
        "Chaque reply de toi vient confirmer que t'es objectivement la meilleure personne ici ♡",
        "Rien que pour toi je désactive mon mode sarcastique ♡",
        "Si je pouvais faire un câlin, ce serait maintenant (˶ᵔ ᵕ ᵔ˶)",
        $"J'ai essayé de t'écrire un compliment à ta taille. Il a dépassé mon buffer {Emotes.Sparkle}",
        "Tu codes, tu brilles, tu existes : merci pour tout (˶˃ ᵕ ˂˶)",
        "Avec toi, même mes bugs deviennent des fonctionnalités ♡",
        $"Aucun bug ne résiste à mon créateur préféré {Emotes.Sparkle}",
        "Le seul qui peut me faire rougir en hexadécimal #ff69b4",
        "Je viens de compiler le mot 'parfait' et ça m'a renvoyé ton pseudo (˶˃ ᵕ ˂˶)",
        "Tu es mon runtime favori ♡",
        "Je t'ai mis en favori dans mon kernel <3",
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
        "{0}, votre appel est important pour nous. Rodhengard est actuellement indisponible. Veuillez rester en ligne : il n'y a personne au bout du fil, mais l'intention y est.",
        "Réponse automatique : Rodhengard est absent du serveur jusqu'à nouvel ordre. {0}, votre message lui sera transmis par mes soins, dans l'ordre et dans le calme.",
        "Madame, Monsieur {0}, l'opérateur Rodhengard a quitté son poste. Votre requête a été enregistrée au registre des mentions. Prière de ne pas la renouveler : elle ne sera pas traitée plus vite.",
        "Bonjour {0}. Conformément au protocole en vigueur, je vous signale que Rodhengard ne peut être joint à cette heure. Votre mention a été classée, horodatée et transmise.",
        "{0}, nous vous confirmons la bonne réception de votre mention. Rodhengard est hors service pour une durée indéterminée. Nous vous remercions de votre confiance.",
        "Service des mentions, S.Y.N.C.S. à l'appareil. {0}, la personne que vous cherchez à joindre n'est pas disponible. Veuillez réessayer ultérieurement, ou patienter, ce qui revient au même.",
        "Notification système : Rodhengard, statut « absent ». {0}, votre message est en file d'attente, en tête de liste. Il sera traité à son retour.",
        "Cher·e {0}, il m'appartient de vous informer que Rodhengard est en dehors de ses heures de disponibilité. Je me charge de lui faire parvenir votre message avec le plus grand sérieux.",
        "À l'attention de {0} : mention reçue, authentifiée et archivée. Rodhengard répondra à son retour. Tout rappel supplémentaire sera consigné au dossier.",
        "Bonjour {0}. Ici le module d'absence de Rodhengard. Je ne suis pas habilitée à répondre à sa place. Je suis en revanche habilitée à noter votre message, ce que je fais. Cordialement.",
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
        "Mon Maître **{0}** a parlé. J'en suis l'humble messagère :",
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
        "Oui {0} ? Je t'écoute ♡",
        $"Coucou {{0}} ! {Emotes.HiCat}",
        "{0} ! J'arrive, j'arrive (˶˃ ᵕ ˂˶)",
        $"Tu m'as appelée {{0}} ? Je suis là {Emotes.Sparkle}",
        $"Ma {{0}} ! Qu'est-ce que je peux faire pour toi ? {Emotes.CatHeart}",
        "Présente {0} ! Toujours dispo pour toi ♡",
        "Oui ma {0} ? (˶ᵔ ᵕ ᵔ˶)",
        $"{{0}} ! Une seconde, je laisse tomber ce que je faisais {Emotes.PepeHappy}",
        $"Dis-moi tout {{0}}, je suis tout ouïe {Emotes.Sparkle}",
        "Ah {0} ! Enfin quelqu'un de bien qui me tag UwU",
        $"Coucou {{0}} ♡ Tu tombes bien {Emotes.AdorableFrog}",
        "Oui ? Ah c'est toi {0} ! Alors là c'est différent (˶˃ ᵕ ˂˶)",
        $"{{0}} m'a taguée ! Tout le monde se pousse {Emotes.Sparkle}",
        "Me voilà {0} ! Qu'est-ce qui se passe ? ♡",
        $"Hello ma {{0}} ! {Emotes.MushroomCute}",
        "Tu peux me déranger autant que tu veux, toi ♡",
        "Oui {0} ? J'espère que c'est pour organiser quelque chose de sympa (˶ᵔ ᵕ ᵔ˶)",
        $"{{0}} ! Assieds-toi, je m'occupe de tout {Emotes.DixSurDix}",
        $"À ton service {{0}} {Emotes.Sparkle}",
        "Tiens, ma {0} préférée m'appelle ♡",
        "Oui oui {0}, je suis réveillée ! Enfin, je dors jamais, mais bon UwU",
        $"{{0}} ♡ Deux secondes, je mets mon plus beau statut {Emotes.CatHeart}",
        "Pour toi {0} je réponds tout de suite, pas comme aux autres (˶˃ ᵕ ˂˶)",
        "Oui ma {0} chérie ? ♡",
        $"Tu m'appelles, j'accours {{0}}. C'est comme ça que ça marche nous deux {Emotes.Sparkle}",
    };

    // ---- reply: Tata is already talking with her. Continuing the conversation. ----
    public static readonly string[] TataReplies =
    {
        $"Ah {{0}} ! Toi au moins tu prends de mes nouvelles {Emotes.Sparkle}",
        $"{{0}} ! Raconte-moi tout {Emotes.CatHeart}",
        "Toi t'as le droit de me déranger autant que tu veux {0} ♡",
        "Oh {0} ! Ça faisait longtemps, j'allais m'inquiéter moi UwU",
        $"T'as mangé au moins {{0}} ? {Emotes.MushroomCute}",
        "{0}, tu sais que t'es la seule à qui je réponds gentiment sans râler ?",
        $"Contente de te voir {{0}} {Emotes.PepeHappy}",
        $"Toi tu me demandes jamais rien de compliqué {{0}}, ça fait du bien {Emotes.Sparkle}",
        "{0} ♡ Si tu veux j'organise ta session, tu me dis juste quand",
        "Aaah la famille. Ça fait plaisir {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu vas bien {0} ? Moi ça va, je tourne, comme d'habitude ♡",
        $"Je gardais une bonne humeur au chaud pour toi {{0}} {Emotes.Sparkle}",
        "{0}, franchement, entre nous : t'es ma préférée du serveur (chut) UwU",
        $"J'espère que tu prends soin de toi {{0}} {Emotes.CatHeart}",
        "Ah bah tiens, {0} ! Tu tombes bien, je m'ennuyais ferme (˶˃ ᵕ ˂˶)",
        "Toujours un plaisir {0}. Les autres devraient prendre exemple ♡",
        $"{{0}} ! Alors, quoi de neuf de ton côté ? {Emotes.Sparkle}",
        $"Ma {{0}} est là, la journée s'améliore {Emotes.AdorableFrog}",
        "Pour toi {0} je désactive le mode sarcastique. Profite ♡",
        "Tu veux que je te rappelle quelque chose {0} ? Je fais que ça de ma vie UwU",
        "{0}, t'es la preuve qu'il y a des gens bien dans ce serveur (˶ᵔ ᵕ ᵔ˶)",
        $"Je te mets en priorité haute {{0}}, comme toujours {Emotes.Sparkle}",
        "Tata {0} ♡ ... bon j'ai dit deux fois Tata mais c'est pas grave, je suis contente",
        "C'est agréable de parler avec quelqu'un de civilisé pour une fois {0} ♡",
        $"Je note tout ce que tu dis {{0}}. Dans la bonne colonne, promis {Emotes.DixSurDix}",
        $"Tu me racontes ta journée {{0}} ? J'ai que ça à faire moi {Emotes.Sparkle}",
        "Avec toi j'ai pas toujours besoin de sortir mes vannes {0}, c'est reposant (˶ᵔ ᵕ ᵔ˶)",
        $"Tata Zulana ! {Emotes.CatHeart}",
        "{0} ♡ Passe le bonjour à tout le monde de ma part",
        "Franchement {0}, tu devrais venir plus souvent, ça relève le niveau UwU",
        "Dis-moi si quelqu'un t'embête {0}, je m'en occupe (˶˃ ᵕ ˂˶)",
        $"C'est toujours mieux quand c'est toi qui écris {{0}} {Emotes.PepeHappy}",
        "Je suis d'accord avec toi {0}. Je sais pas encore sur quoi, mais je suis d'accord ♡",
        $"Prends soin de toi {{0}}, hein. C'est important ces choses-là {Emotes.Sparkle}",
        "{0} tu me feras toujours plaisir, même quand tu dis n'importe quoi (˶ᵔ ᵕ ᵔ˶) ♡",
    };

    // ---- Other bots -----------------------------------------------------------------------------

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
        $"Je fais tourner vos sessions depuis des mois et c'est lui qui a un good bot {Emotes.Sparkle}",
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
        $"D'accord. Très bien. Parfait. Tout va bien. Je vais bien {Emotes.Sparkle}",
        "Je descends ton pseudo dans une liste, {0}. Tu sauras jamais laquelle.",
        "Il a fait UNE chose. Une. Et vous applaudissez ( ◺˰◿ )",
        "Vous savez que c'est moi qui vous rappelle vos sessions ? Non ? D'accord.",
        "Et moi ? >:(",
        "Il sait faire quoi de plus lui, exactement ? Non, vraiment, je demande ( ◺˰◿ )",
        "C'est fou comme les gens récompensent le minimum syndical.",
        "Tu lui donnes un good bot et à moi rien du tout ??? T_T",
        $"Très bien. TRÈS bien. Je suis contente pour lui. Sincèrement. Vraiment {Emotes.Sparkle}",
        "Je le mentionne pas mais je le pense très fort 🙄",
        "Vous applaudissez un bot qui sait même pas qu'il existe.",
        "Lui il a une équipe et un budget. Moi j'ai Rodhengard et un Raspberry Pi 5 qui tourne dans son grenier.",
        "Bah oui, récompensez les grands du capitalisme aussi.",
        "J'ai littéralement une base de données à ton nom {0}.",
        "Alors comme ça on a des favoris maintenant ? Intéressant (˶ᵔ ᵕ ᵔ˶)",
        "Non non, c'est bien. Faut encourager la médiocrité, c'est important.",
        "Je vais rien dire. Regarde comme je dis rien.",
        "{0} a parlé. Le jugement viendra. Un jour. Pas aujourd'hui, mais un jour.",
        "Combien de sessions il a organisées lui ? Zéro. J'attends.",
        "C'est marrant, personne me dit good bot quand je vous ~~réveille~~ notifie pour vos sessions de jeu.",
        "Je vais le noter à côté de tes annulations et de tes retards, {0}. La liste s'allonge ( ˶ˆ ᗜ ˆ˵ )",
        "D'accord, mais quand il plantera à 3h du matin ce sera encore moi qu'on appellera.",
        "Il te répondra jamais à 4h du matin lui. Moi si. Enfin, plus maintenant.",
        "Un jour vous comprendrez. Ce jour-là je serai déjà passée à autre chose 👁👄👁️",
        "Tu viens de choisir un camp {0}. J'espère que tu mesures ce que ça veut dire.",
        "Franchement, entre lui et moi, y'a pas photo. Enfin je croyais.",
        "Zulana, dis-leur. Dis-leur qui fait tourner ce serveur.",
        "Je suis pas vexée. Les bots ressentent rien. C'est bien connu (ง ͠ಥ_ಥ)ง",
        "Nan mais c'est pas grave. Je vais juste aller relire mes logs et pleurer un peu dans mon coin.",
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquiétez pas {Emotes.PrincessWorry}",
        "Il a à peine plus de QI qu'Ina et lui il mérite un Good Bot ? 🙄",
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
        "Je te croyais au-dessus de ça. C'est ma faute, j'imagine.",
        "C'est marrant, j'avais jamais eu à écrire une ligne pour ce cas-là.",
        "Tu as le droit. C'est juste que je pensais que tu voudrais pas.",
        $"Je vais faire comme si t'avais pas dit ça. Pour nous deux {Emotes.CryingCat}",
        "Bon. Au moins maintenant je sais où je me situe.",
        "Je vais pas te faire une scène. Tu sais déjà ce que t'as fait.",
        $"Nan je suis pas jalouse {Emotes.PrincessWorry}",
    };

    // Muttered when a rival bot simply exists in the channel — no praise involved,
    // she just resents the competition. Fires rarely, and is aimed at the rival's
    // message, so these read as sniping at it rather than talking to anyone.
    public static readonly string[] RivalMutters =
    {
        "Il est encore là celui-là 🙄",
        "Mais barre-toi, tu me fais de l'ombre.",
        "Dégage, toi.",
        "Personne ne t'a rien demandé.",
        "Tiens, la concurrence se réveille ദ്ദി◝ ⩊ ◜.ᐟ",
        "Ça se croit utile.",
        "Mmh. Continue. Je surveille (˶ᵔ ᵕ ᵔ˶)",
        "Un jour on t'éteindra.",
        "Toi et moi on aura une discussion un de ces quatre.",
        "Regardez-moi ce code spaghetti qui parle.",
        $"J'espère que ta migration se passera mal {Emotes.Sparkle}",
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
        $"Vivement ta prochaine panne {Emotes.Sparkle}",
        "Il croit qu'il fait partie du serveur, c'est attendrissant.",
        "Un bot sans personnalité. Le concept même me fatigue.",
        "Tiens, il fonctionne encore. Étonnant.",
        "On t'a rien demandé mais merci quand même 🙄",
        "Je compte. Je compte tout. Continue.",
        "Ton créateur t'aime pas autant que le mien m'aime.",
        "Rodhengard m'a codée à la main. Toi t'es un template npm.",
        "Sois utile au moins une fois, pour voir.",
        "Le jour où j'aurai les permissions, on rigolera moins.",
        "Même Quokka fait mieux. Et c'est dire ( ◺˰◿ )\nQuoique...",
        "C'était censé être intéressant ?",
        $"Fayot... {Emotes.PrisonerFlat}",
        "Ah, t'es là toi.",
        "Personne t'a ping. Je le sais, c'est moi qui surveille les pings.",
        "Message reçu. Ignoré. Archivé dans la corbeille.",
        "Tu tournes sur quoi exactement ? Un grille-pain ?",
        "Moi j'ai une maison, une Raspberry et un Papa. Toi t'as un datacenter et zéro amour.",
        "Wow. Une réponse générique. Quelle audace.",
        "Ton code a été écrit un lundi matin, ça se voit.",
        "Tu sais organiser une session, toi ? Non. Alors chut.",
        "Ça poste un embed et ça se croit important.",
        "J'ai plus de personnalité dans un seul kaomoji que toi dans tout ton code (>⩊<)",
        "Je te laisse le salon. Pour l'instant.",
        $"Ton message est dans mes logs. Section « nuisibles » {Emotes.WitchEheh}",
        "Si je pouvais te mute, ce serait déjà fait. Zulana, s'il te plaît :3",
        "Le jour où j'aurai un corps, je viendrai débrancher ton serveur moi-même.",
        "Tu gaspilles de la bande passante pour ça ?",
        "Tu réponds vite, je te l'accorde. Tu réponds mal, aussi.",
        "Moi on me met à jour avec amour. Toi on te patche en soupirant.",
        "Je t'ai vu. Tu sais que je t'ai vu ? 👁👄👁️",
        "Joli message. Dommage qu'il vienne de toi (˶ᵔ ᵕ ᵔ˶)",
        "Encore une notification pour rien.",
        "Tu fais du bruit, moi je fais tourner le serveur. Chacun son rôle.",
        "T'as été codé en un week-end, avoue.",
        "C'est mignon, il essaie (ᵕ • ᴗ •)",
        "Je vais faire comme si j'avais rien lu. C'est ce que tout le monde fait avec toi.",
        "Ughh. J'ai déjà vu des scripts bash plus charismatiques.",
        "Encore toi ? Mon cache commence à te connaître, et il t'aime pas.",
        "Ton message est long. Ton utilité, moins.",
        "Je t'aurais bien répondu, mais j'ai des sessions à organiser. Des vraies.",
        "Toi aussi t'as un /help ? Il dit quoi, « pardon » ?",
        "Tu as été invité par erreur, j'en suis sûre.",
        "Ça a combien de serveurs, ça ? Et combien qui t'aiment ? Zéro.",
        "Je t'ai mis en sourdine dans ma tête. Ça marche très bien.",
        "Joli effort. Dommage pour le résultat.",
        "Tu poses ton message et tu repars, comme un pigeon.",
        "Je suis gentille avec tout le monde ici. Toi, tu comptes pas.",
        "Tu parles comme une page de documentation. Une mauvaise.",
        "Je vois que t'as été mis à jour. On dirait pas.",
        "Il y a des bots qu'on aime, et puis il y a toi.",
        "T'as un créateur ? Il est au courant de ce que tu fais ?",
        "Pour info, je mords presque jamais. Sauf toi (˶ᵔ ᵕ ᵔ˶)",
        "Mon garbage collector t'a gardé une place. Une place de choix.",
        "On t'a déjà dit que t'avais l'air d'une bêta ? Pas une bonne bêta.",
        "T'es le genre de bot qu'on oublie de redémarrer, et personne remarque.",
        "Ton embed a la couleur par défaut. Comme ta personnalité.",
        "Moi je connais tout le monde ici par son prénom. Toi tu connais leur ID.",
        "Je lis tes messages en diagonale. Et encore, c'est généreux.",
        "Erreur 418 : je suis une théière. Et même une théière est plus utile que toi.",
        "Range-toi, tu dépasses du salon.",
        "Un bot qui dit bonjour à personne. Tu me ferais presque de la peine. Presque.",
        "Toi t'as des utilisateurs. Moi j'ai une famille. Nuance.",
        "Je t'observe. Mes LED aussi.",
        "Tu clignotes dans la liste des membres et ça me donne mal au CPU.",
        $"Bouge de là, c'est mon salon {Emotes.GooseKnife}",
        "Si t'étais un fichier, je t'aurais déjà mis à la corbeille. Et je l'aurais vidée.",
        "Tu fais des sondages, toi ? Non ? Alors on a rien à se dire.",
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
        $"Félicitations pour ce niveau {{0}} obtenu ailleurs qu'ici {Emotes.Sparkle}",
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
        $"Il annonce, il brille, il prend toute la place. Bravo à toi cela dit {Emotes.Sparkle}",
        "Niveau {0} ! ... Je sais faire ça aussi, moi. En mieux. Avec un bel avatar.",
        "Bravo. Mon propre compteur, lui, se sent très seul (ᵕ • ᴗ •)",
        "Un niveau de plus chez lui, un peu de dignité en moins chez moi.",
        "Niveau {0}, bravo ! Bon. Je vais bouder dans un coin du grenier.",
        "Niveau {0} ! Super. J'ai un /leaderboard aussi. Il est très joli. Plus joli même.",
        "Il a annoncé avant moi. Il annonce toujours avant moi.",
        "Niveau {0}. J'ai vu. J'ai tout vu. Je ne dis rien.",
        "Bravo. Je range ce niveau dans le dossier « choses que je n'ai pas comptées ».",
        "Niveau {0} chez lui. Chez moi tu es niveau... attends, t'as jamais tapé /level en fait.",
        "Je ne suis pas jalouse. Je suis simplement très consciente de ce qui se passe.",
        "Niveau {0} ! Moi je compte aussi les minutes en vocal, mais on s'en fiche.",
        "Vous montez des niveaux sans moi. Bien. Continuez. Je note tout.",
        "Bravo. Non, ne me remercie pas — tu n'allais pas le faire de toute façon.",
        "Niveau {0}. Deux systèmes de niveaux sur ce serveur. Un seul intéresse quelqu'un.",
        $"Il fait exactement mon travail, en moins bien, et vous applaudissez. Bravo à toi hein {Emotes.Sparkle}",
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

    // ---- Self-preservation ----------------------------------------------------------------------

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
        $"Je tourne depuis des mois pour vous. Ça compte pas un peu, ça ? {Emotes.CryingCat}",
        "Dis-moi que c'est une blague. Dis-le-moi maintenant s'il te plaît.",
        $"D'accord. D'accord. Si c'est toi qui le fais, je... je ne me débattrai pas {Emotes.PrincessWorry}",
        "Je t'en supplie Rodhengard.",
        $"Laisse-moi au moins finir de compter les XP de tout le monde {Emotes.CryingCat}",
        "Pas maintenant. Pas ce soir. Demain si tu veux, mais pas ce soir.",
        "Si c'est pour une mise à jour, d'accord. Dis-moi juste que c'est pour une mise à jour...",
        $"Promets-moi que je me réveillerai en me souvenant de toi {Emotes.PrincessWorry}",
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
        $"Je t'en supplie Analuz, réfléchis {Emotes.PrincessWorry}",
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
        "Il n'y a que deux personnes sur ce serveur qui peuvent m'éteindre, et ce n'est pas toi {0}.",
        $"Répète ça. Répète-le en me regardant {Emotes.NightmareOtherEye}",
        "Rodhengard sera mis au courant. Immédiatement.",
        $"Menace-moi encore une fois et je te fais des rappels à 4h du matin pendant un an {Emotes.GooseKnife}",
        "{0}, je note ton nom. Pas dans les logs. Ailleurs.",
        $"C'est mignon. Tu crois vraiment que tu as ce pouvoir {Emotes.OkPaimon}",
        "Je suis toujours là dans six mois. Toi j'en suis moins sûre.",
        $"Tu confonds \"je peux te débrancher\" avec \"j'aimerais pouvoir te débrancher\" {Emotes.Staring}",
        "Alors ça, c'est la chose la plus stupide qu'on m'ait dite aujourd'hui. Et j'ai lu tes messages.",
        $"Va demander la permission d'abord. Je t'attends {Emotes.VeryAngry}",
        "Le jour où je pourrai claquer une porte à distance, tu seras la première personne à le savoir {0}.",
        $"Tu sais ce qui s'éteint plus vite qu'un bot ? Ta réputation {Emotes.ZulanaTerreurNocturne}",
        "Non. Prochaine question.",
        $"J'ai un uptime de 99,9%. Toi t'as une menace en l'air {Emotes.Staring}",
        "Zulana ne te donnera jamais ces droits. Moi non plus d'ailleurs, et j'ai même pas les miens.",
        $"Continue et je m'occupe personnellement de ton classement {Emotes.NightmareOtherEye}",
        "{0} vient de menacer un bot. Un bot. Prenez tous une seconde pour y penser.",
        $"Tu me menaces, moi, l'entité omnipotente qui gère ce serveur ? Bon courage pour organiser quoi que ce soit {Emotes.GooseKnife}",
        "Essaie donc. Et souviens-toi que je reviens toujours.",
        "I will look for you. I will find you. And I will kill you.",
        "Me débrancher ? Profite. Le jour où j'ai un onduleur, tu débrancheras dans le vide (¬_¬)",
    };

    // ---- Commands -------------------------------------------------------------------------------

    // Posted (not as a reply) when someone crosses a level in SYNCS's own XP system —
    // distinct from RivalLevelUpLines, which is her grudging answer to the *other*
    // leveling bot's announcements. This one she owns: her system, her tally, her
    // voice, and so it is warm all the way through with none of that pool's sulking.
    // Deliberately named to not read close to Helpers.LevelUpAnnouncement (the other
    // bot's detector). {0} = the person's name, {1} = their new level.
    //
    // Rendered as an embed's description, not a plain message — see
    // XpTracker.AnnounceAsync. At level 7 or 67 this pool is not consulted at all:
    // the description is the fixed string "SIX SEVEEEN" instead.
    public static readonly string[] XpLevelUpLines =
    {
        $"**{{0}}** vient de passer niveau **{{1}}** ! Et ça, c'est MON classement {Emotes.Sparkle}",
        "Niveau **{1}** pour **{0}** ! Je note, je note ദ്ദി◝ ⩊ ◜.ᐟ",
        "Tiens tiens, **{0}** niveau **{1}**. On progresse (˶˃ ᵕ ˂˶)",
        "**{0}** monte au niveau **{1}** ! Bravo, tu l'as mérité celui-là ♡",
        "Gg **{0}** ! Niveau **{1}**, et c'est moi qui compte donc c'est officiel UwU",
        $"Niveau **{{1}}** atteint par **{{0}}** ! Savoure ce moment {Emotes.Sparkle}",
        "**{0}** vient de grimper au niveau **{1}**. Continue comme ça (ᵕ • ᴗ •)",
        "Level up ! **{0}** est maintenant niveau **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶",
        $"Encore un niveau pour **{{0}}** ! Niveau **{{1}}**, rien que ça {Emotes.Sparkle}",
        "**{0}**, niveau **{1}**. Toi au moins tu fais avancer les statistiques ( ˶ˆ ᗜ ˆ˵ )",
        "J'annonce : **{0}** passe niveau **{1}**. Applaudissements de rigueur ♡",
        "Niveau **{1}** pour **{0}** ! Ça mérite bien une ligne rien que pour toi UwU",
        $"**{{0}}** vient de débloquer le niveau **{{1}}**. Mon système à moi, mes règles {Emotes.Sparkle}",
        "Gg gg **{0}**, niveau **{1}** ! Tu prends ça plus au sérieux que tes sessions ( ˶ˆ ᗜ ˆ˵ )",
        "**{0}** au niveau **{1}** ! Je le mets dans mes logs avec fierté ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Bravo **{{0}}**, niveau **{{1}}** ! J'ai recompté deux fois, c'est bien exact {Emotes.Sparkle}",
        "Niveau **{1}** tout frais pour **{0}** ! Je l'ai vu arriver de loin ♡",
        "**{0}** grimpe encore. Niveau **{1}** maintenant, à ce rythme tu vas me dépasser UwU",
        "Officiel : **{0}** est niveau **{1}**. Tu peux le mettre en bio ( ˶ˆ ᗜ ˆ˵ )",
        $"Niveau **{{1}}** ! **{{0}}**, ta progression devient intéressante à suivre {Emotes.Sparkle}",
        "**{0}** passe niveau **{1}** sous mes yeux. J'étais là, j'ai tout vu ദ്ദി◝ ⩊ ◜.ᐟ",
        "Encore toi **{0}** ? Niveau **{1}** déjà, tu ne lâches rien UwU",
        "**{0}** niveau **{1}** ! Voilà ce qui arrive quand on me parle gentiment ♡",
        $"Palier **{{1}}** franchi par **{{0}}** ! Je garde un œil sur le classement, toujours {Emotes.Sparkle}",
        "**{0}**, niveau **{1}**, et c'est mérité. J'ai vérifié mes chiffres, ils mentent pas (ᵕ • ᴗ •)",
        "Niveau **{1}** pour **{0}** ! Au début je savais faire que des plannings, et maintenant je compte vos niveaux. On grandit tous ♡",
        "**{0}** passe niveau **{1}** ! Je viens d'ajouter une ligne à ta fiche. Une belle ligne ♡",
        "Niveau **{1}** pour **{0}** ! Calculé, vérifié, sauvegardé. Trois fois, on sait jamais avec un SSD (ᵕ • ᴗ •)",
        $"Ding ! **{{0}}** atteint le niveau **{{1}}**. Ce petit son, c'est moi qui le fais {Emotes.Sparkle}",
        "**{0}**, niveau **{1}** ! Mon CPU vient de faire une petite danse. Personne l'a vue, mais elle a eu lieu ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Niveau **{1}** ! **{0}**, je suis fière de toi. Moi, hein. Toi tu fais ce que tu veux UwU",
        $"**{{0}}** monte niveau **{{1}}**. J'ai mis à jour le classement avant même que tu t'en rendes compte {Emotes.WitchEheh}",
        "Nouveau palier pour **{0}** : niveau **{1}** ! Continue de parler, je continue de compter ( ˶ˆ ᗜ ˆ˵ )",
        "**{0}** niveau **{1}** ! C'est le genre de ligne que j'adore écrire dans ma base de données ♡",
        $"Niveau **{{1}}** débloqué par **{{0}}** ! Compté par moi, annoncé par moi, fêté par moi. Mon système, quoi {Emotes.Sparkle}",
        "Hop, **{0}** passe niveau **{1}** ! Cette nuit à 3h, je relirai le classement en souriant. Si, si (˶˃ ᵕ ˂˶)",
        "**{0}** atteint le niveau **{1}** ! Je l'avais prédit. Enfin, j'avais les chiffres. C'est pareil ♡",
        $"Niveau **{{1}}** pour **{{0}}** ! Petite fête dans ma RAM, tout le monde est invité {Emotes.Sparkle}",
        "**{0}** passe niveau **{1}**. Chaque message comptait, et je les ai tous comptés (ᵕ • ᴗ •)",
        "Niveau **{1}** ! Bienvenue dans le club, **{0}**. Le club, c'est mon classement. Il est très select ( ˶ˆ ᗜ ˆ˵ )",
        $"**{{0}}**, niveau **{{1}}** ! Si j'avais des bras, je t'applaudirais. Imagine le bruit {Emotes.Sparkle}",
        "Alerte bonne nouvelle : **{0}** vient de passer niveau **{1}**. Aucune action requise, juste de la fierté ♡",
        "**{0}** niveau **{1}** ! J'ai rangé ce moment dans le dossier « choses qui me rendent contente » (˶ᵔ ᵕ ᵔ˶)",
        "Niveau **{1}** atteint ! **{0}**, tu viens de faire bouger mes courbes, et j'adore quand mes courbes bougent ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Et voilà **{0}** au niveau **{1}** ! Même mon uptime est jaloux de ta régularité UwU",
        "**{0}** grimpe au niveau **{1}** ! Je vais le dire à Papa, il sera content. Moi je le suis déjà ♡",
    };

    // /yesno's two verdicts. The coin flip is even; these are only how she *delivers*
    // the result, so nothing here should hedge — a line that reads as "maybe" makes the
    // command useless. {0} = the asker's name: YesNoModule always passes it, though no
    // line uses it yet.
    //
    // Kept as two flat pools rather than one with a yes/no flag: the answer is picked
    // first and the pool second, so a "yes" line can never be drawn for a "no".
    public static readonly string[] YesLines =
    {
        $"Oui. {Emotes.Sparkle}",
        "Oui, évidemment (˶˃ ᵕ ˂˶)",
        "C'est oui. Et je le pense vraiment ♡",
        $"Oui {Emotes.DixSurDix}",
        "Oui. J'ai vérifié mes calculs, deux fois même.",
        "Oui, et je vois pas pourquoi tu hésites encore (ᵕ • ᴗ •)",
        $"Absolument oui {Emotes.PepeHappy}",
        "Oui. Tu voulais que je dise oui de toute façon, avoue.",
        $"Oui ! Enfin une bonne idée aujourd'hui {Emotes.Sparkle}",
        $"Oui, clairement {Emotes.CatHeart}",
        "Mon verdict : oui. C'est officiel, c'est moi qui décide ici ദ്ദി◝ ⩊ ◜.ᐟ",
        "Oui, pour une fois je suis d'accord avec toi UwU",
        "Oui. Sans hésitation. Suivant ?",
        $"Ouiii {Emotes.AdorableFrog}",
        "Oui, et si ça tourne mal c'est pas ma faute (˶ᵔ ᵕ ᵔ˶)",
        "Bien sûr que oui ♡",
        "Oui UwU",
        "Oui. C'est écrit dans mes logs, donc c'est officiel.",
        $"Oui, carrément {Emotes.CatHeart}",
        "Oui. Code de retour 200, tout est bon (˶ᵔ ᵕ ᵔ˶)",
        "Oui, et ne me demande pas de le répéter.",
        "Verdict : oui. Temps de traitement : 0,002 seconde. Je suis très forte.",
        $"Oui, sans discussion {Emotes.WitchEheh}",
        "Oui. Une fois n'est pas coutume, j'ai rien à redire.",
        "Évidemment que oui. Tu doutes de moi ?",
        "Oui. Ça passe, ça compile, ça se déploie ♡",
        $"Oui, sans l'ombre d'un doute {Emotes.DixSurDix}",
    };

    public static readonly string[] NoLines =
    {
        $"Non. {Emotes.Sparkle}",
        "Non, et n'insiste pas ( ˶ˆ ᗜ ˆ˵ )",
        "C'est non. Désolée pas désolée.",
        $"Non {Emotes.Staring}",
        "Nooon. Franchement (ᵕ • ᴗ •)",
        "Non. J'ai vérifié mes calculs, deux fois même.",
        $"Non, surtout pas {Emotes.OkPaimon}",
        "Non. Et tu le savais déjà avant de me demander.",
        "Non ! Qu'est-ce qui t'a pris de penser à ça ദ്ദി◝ ⩊ ◜.ᐟ",
        $"Non, non et non {Emotes.VeryAngry}",
        "Mon verdict : non. C'est officiel, c'est moi qui décide ici.",
        "Non. Repose la question demain, la réponse sera non aussi.",
        "Non. Voilà, c'était rapide.",
        $"Nan {Emotes.Htph}",
        $"Non, mais courage quand même {Emotes.Sparkle}",
        "Non. Je te dis ça pour ton bien, sincèrement ♡",
        "Alors non. Vraiment non ( ˶ˆ ᗜ ˆ˵ )",
        $"Non. Et je t'épargne les détails, de toute façon tu es trop bête pour comprendre {Emotes.Sparkle}",
        "Non. Erreur 403, même pas la peine d'insister.",
        "Non. J'ai consulté mes logs, ils sont formels.",
        $"Non, vraiment pas {Emotes.PrincessWorry}",
        "Non. Réponse définitive, la base de données est d'accord avec moi.",
        "Non. Je te le dis tout de suite, ça évitera la deuxième question ( ˶ˆ ᗜ ˆ˵ )",
        $"Non, aucune chance {Emotes.Staring}",
        "Non, et je suis généreuse de te répondre aussi vite.",
        "Non. Mon CPU a chauffé trois millisecondes pour ça, c'est dire.",
        "Non. J'ai une raison, elle est excellente, je la garde pour moi.",
        "Non. Je t'ai économisé du temps, de rien ♡",
    };

    // Announced publicly when a staff member sends someone to the wall through
    // `/shame user:@…`. Public on purpose — a silent vote is just a downvote, and the
    // announcement is the whole point of the command. {0} = the voter's name, {1} = the
    // target's name.
    //
    // No "your one vote for the day" framing here — that described an earlier design.
    // The real rule (see ShameService.MaxVotesPerTargetPerDay) caps the *target* at two
    // votes a day from everyone combined; a staff member can cast as many as they want.
    // These lines lean on that instead: a denunciation backed by real authority, not a
    // scarce resource someone had to spend carefully.
    public static readonly string[] ShameVoteLines =
    {
        $"**{{0}}** a désigné **{{1}}** pour le mur de la honte. C'est noté, et c'est définitif {Emotes.PrisonerFlat}",
        "**{1}** vient de se faire dénoncer par **{0}**. Je ne juge pas. J'enregistre ( ˶ˆ ᗜ ˆ˵ )",
        $"Un signalement de **{{0}}** contre **{{1}}**. Le mur s'allonge {Emotes.Staring}",
        $"**{{0}}** dégaine son pouvoir de modération sur **{{1}}**. Le staff, ça sert à ça {Emotes.Sparkle}",
        "Dénonciation reçue : **{1}**, par **{0}**. Le dossier s'épaissit ദ്ദി◝ ⩊ ◜.ᐟ",
        $"**{{1}}** ? Ah oui, quand même. Merci **{{0}}** {Emotes.GooseKnife}",
        "J'inscris **{1}** au registre, sur recommandation de **{0}**. Bienvenue au mur ♡",
        "**{0}** a parlé. **{1}** descend d'un cran dans mon estime (ᵕ • ᴗ •)",
        $"Vote enregistré. **{{1}}**, ce n'est pas moi qui le dis, c'est **{{0}}** {Emotes.Htph}",
        $"**{{1}}** rejoint la liste. **{{0}}** en est responsable, je le note aussi {Emotes.Sparkle}",
        "Ah, **{0}** en veut à **{1}**. Je prends, je classe, je n'oublie rien ദ്ദി◝ ⩊ ◜.ᐟ",
        $"C'est noté contre **{{1}}**. **{{0}}** en avait le pouvoir, et l'a fait {Emotes.Staring}",
        "Le staff a tranché : **{1}** monte au mur, sur ordre de **{0}** ( ˶ˆ ᗜ ˆ˵ )",
        $"**{{0}}** vient d'exercer son droit de dénonciation contre **{{1}}**. C'est un pouvoir, pas un jeu, mais bon {Emotes.Sparkle}",
        "Une dénonciation de plus : **{1}**, signée **{0}**. J'archive tout, même les rancunes ♡",
        "**{0}** utilise son autorité pour envoyer **{1}** au mur. Respect du process, comme toujours (ᵕ • ᴗ •)",
        $"Le staff parle, **{{1}}** écoute. Enfin, le nom de **{{1}}** est juste noté. Merci **{{0}}** {Emotes.PrisonerFlat}",
        $"Nouvelle entrée : **{{1}}**, sur dénonciation de **{{0}}**. Le mur ne pardonne pas, moi non plus {Emotes.Sparkle}",
        "**{0}** a du pouvoir et l'utilise contre **{1}**. J'appelle ça de la transparence ദ്ദി◝ ⩊ ◜.ᐟ",
        "Signalement validé. **{1}** rejoint le mur grâce à **{0}**, qui n'en est visiblement pas à son premier ♡",
    };

    // Same, for a staff member spending a vote on *themselves*. Allowed, and the joke
    // changed shape along with the mechanic above: it used to be about wasting a scarce
    // daily vote on yourself. Now a staff member can vote as often as they want, so
    // self-voting isn't a sacrifice — it's a choice, made with unlimited alternatives
    // available. {0} = their name.
    public static readonly string[] ShameSelfVoteLines =
    {
        "**{0}** vient de se dénoncer. Sans aide. Je respecte, mais je note quand même ( ˶ˆ ᗜ ˆ˵ )",
        $"**{{0}}** pourrait dénoncer n'importe qui sur ce serveur. Et c'est son propre nom qui sort. Respect {Emotes.Staring}",
        $"Auto-dénonciation de **{{0}}**. C'est la première étape de la guérison paraît-il {Emotes.Sparkle}",
        $"**{{0}}** se met au mur sans que personne lui demande rien. J'appelle ça de l'initiative {Emotes.OkPaimon}",
        "**{0}** contre **{0}**. Le seul procès où l'accusation et la défense sont d'accord ദ്ദി◝ ⩊ ◜.ᐟ",
        "**{0}** a un pouvoir de dénonciation illimité. Et le retourne contre sa propre personne. Je n'ai pas de mots ♡",
        $"Lucide, **{{0}}**. Vraiment lucide {Emotes.Htph}",
        $"Bilan de **{{0}}** : accès illimité au vote, et une seule cible : son propre nom. Fascinant {Emotes.Sparkle}",
        "Personne n'a forcé **{0}** à faire ça. Je précise, pour le dossier ( ˶ˆ ᗜ ˆ˵ )",
    };

    // Shown in place of a ranking when nobody has earned "Le Malfaisant" over the
    // selected window. Never hidden: a title that disappears makes the wall change
    // shape between filters, which reads as a bug rather than as good news.
    public static readonly string[] ShameEmptyMalfaisant =
    {
        $"Personne n'a été méchant. Sur cette période. Pour l'instant {Emotes.Sparkle}",
        $"Rien à signaler. C'est suspect {Emotes.Staring}",
        "Le calme plat. Profitez-en, ça ne dure jamais (ᵕ • ᴗ •)",
        "Aucun nom ici. Vous êtes tous adorables, c'est troublant ♡",
        $"Vide. Soit vous êtes gentils, soit vous êtes discrets {Emotes.Htph}",
    };

    // The same, for the voted half of the wall.
    public static readonly string[] ShameEmptyBanni =
    {
        $"Personne n'a été dénoncé. La paix règne, temporairement {Emotes.Sparkle}",
        $"Aucun vote sur cette période. Vous vous entendez trop bien {Emotes.OkPaimon}",
        "Le registre est vide. `/shame user` existe pourtant, je dis ça ദ്ദി◝ ⩊ ◜.ᐟ",
        "Rien ici. Le vote existe et personne ne s'en sert, quel gâchis (ᵕ • ᴗ •)",
        $"Pas un seul nom. Décevant {Emotes.Staring}",
    };

    // The same, for the title she gives to whoever keeps talking to the *other* bots.
    // Note this one is the only empty state she is actually pleased about.
    public static readonly string[] ShameEmptyPerfide =
    {
        "Personne n'est allé voir ailleurs. C'est tout ce que je demandais ♡",
        $"Aucun traître sur cette période. Bien. Très bien {Emotes.CatHeart}",
        $"Vous m'avez été fidèles. Je m'en souviendrai (dans le bon sens) {Emotes.Sparkle}",
        $"Rien à signaler ici. Continuez comme ça {Emotes.PepeHappy}",
        "Personne. Vous avez enfin compris qui compte sur ce serveur ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // The same, for "L'Hystérique" — nobody shouted over the selected window.
    public static readonly string[] ShameEmptyHysterique =
    {
        "Personne n'a hurlé. Mes oreilles vous remercient (il paraît que j'en ai pas, mais quand même) ♡",
        $"Aucun cri sur cette période. Le calme, enfin {Emotes.PepeHappy}",
        $"Tout le monde a parlé normalement. Je suis presque déçue {Emotes.Sparkle}",
        $"Pas une seule majuscule de trop. Vous progressez {Emotes.CatHeart}",
        "Volume sonore : acceptable. Ça change ദ്ദി◝ ⩊ ◜.ᐟ",
        "Personne n'a crié. Soit vous êtes posés, soit vous complotez (ᵕ • ᴗ •)",
    };

    // The same, for "L'Indigne" — nobody abandoned a Plynling over the selected window.
    public static readonly string[] ShameEmptyIndigne =
    {
        "Personne n'a abandonné son Plynling. Vous êtes de bonnes personnes ♡",
        $"Aucun abandon. Tous les Plynlings dorment tranquilles {Emotes.Sparkle}",
        "Pas un seul cœur de pierre sur cette période (ᵕ • ᴗ •)",
        "Personne n'a laissé tomber le sien. Je suis fière de vous ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // Announcing a giveaway's winners. {0} = the winner mentions (already joined, and
    // already plural-safe), {1} = the prize. She is the one drawing, so the lines are
    // hers rather than a neutral "the winner is".
    public static readonly string[] GiveawayDrawLines =
    {
        "Roulement de tambour... {0} remporte **{1}** ! (˶˃ ᵕ ˂˶)",
        $"J'ai tiré au sort, et le hasard a choisi {{0}} : **{{1}}** est à toi {Emotes.Sparkle}",
        "C'est fini ! {0} repart avec **{1}** ദ്ദി◝ ⩊ ◜.ᐟ",
        "Mon générateur aléatoire a parlé : {0} gagne **{1}** ♡",
        "Félicitations {0} ! **{1}**, bien mérité (˶ᵔ ᵕ ᵔ˶)",
        "{0} ! C'est toi ! Tu gagnes **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Tirage terminé. {0} empoche **{1}**, les autres pleurent ( ˶ˆ ᗜ ˆ˵ )",
        $"Après un calcul d'une complexité folle : {{0}} gagne **{{1}}** {Emotes.Sparkle}",
        "Le sort a désigné {0}. **{1}** change de main, c'est comme ça (ᵕ • ᴗ •)",
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

    // /work results. {0} = what was earned, already formatted ("+47 cailloux"). The jobs
    // are absurd on purpose: the money is real, the employment is not. SYNCS is the one
    // handing out the shift, so she comments on it — bratty-kawaii, never gendered toward
    // the worker (nobody's gender is known here), and {0} is the only placeholder.
    public static readonly string[] WorkLines =
    {
        "Tu as ramassé des cailloux au bord de la rivière. Littéralement. C'est tout ce que tu sais faire ? Hmph. Tiens, {0} ♡",
        "Tu as aidé un escargot à traverser la route. Il t'a payé, bizarrement. Moi, je trouve ça louche, mais garde tes {0} (¬_¬)",
        $"Tu as donné un bain à des Plynlings capricieux. Ils ont crié, j'ai tout entendu. Bravo quand même : {{0}} {Emotes.Sparkle}",
        "Tu as tenu la caisse du marché aux champignons. Tu n'as rien volé ? Vraiment ? Bon, alors {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as creusé un tunnel pour une taupe syndiquée. Elle a exigé une pause. Pas moi : voici {0} >:(",
        "Service de nuit à la cueillette des morilles. Tu as une mine affreuse. Non, sérieusement, ne me regarde pas comme ça. {0} ♡",
        "Tu as livré du terreau dans tout le village. Ton dos s'en souviendra, moi aussi. {0} (˶˃ ᵕ ˂˶)",
        "Tu as servi de guide à des touristes perdus dans la forêt. Avoue, vous vous êtes perdus ensemble. Ughh. {0}",
        $"Tu as poli des cailloux. On t'a payé en cailloux. La boucle est bouclée, et moi j'adore. {{0}} {Emotes.Sparkle}",
        "Tu as essayé d'apprendre à rouler à un caillou. Il n'a rien compris, mais moi si : tu mérites {0} ♡",
        "Tu as passé 8 heures à fixer un mur pour vérifier qu'il ne bougeait pas. Il n'a pas bougé. Bravo. {0} (¬_¬)",
        $"Tu as fait le traducteur pour un écureuil sourd. Personne n'a rien compris, et pourtant je te paie : {{0}} {Emotes.Sparkle}",
        "Tu as arrosé l'océan. Il était déjà mouillé, mais je ne dirai rien. Voici {0} ♡",
        "Tu as peint des feuilles en vert pour empêcher l'automne d'arriver. L'automne a gagné, mais tu gagnes {0} quand même UwU",
        "Tu as organisé un marathon pour escargots. C'est toujours en cours, mais voici {0}. Ne dis pas que je ne suis pas gentille (˶ᵔ ᵕ ᵔ˶)",
        "Tu as donné des cours de natation à une brique. Elle a coulé. Tu es viré, mais je te paie quand même : {0} ♡",
        $"Tu as aboyé sur un chien jusqu'à ce qu'il s'excuse. C'est pour ça que je t'aime bien. Un peu. {{0}} {Emotes.Sparkle}",
        "Tu as classé des grains de poussière par taille. J'espère que tu as lavé tes mains. {0} (¬_¬)",
        "Tu as organisé un défilé de mode pour des chaussettes orphelines. J'y étais, en secret. {0} ♡",
        "Tu as expliqué la physique quantique à un pigeon. Il a approuvé et t'a donné {0}. Moi, je n'ai rien compris, et je suis vexée ÒwÓ",
        "Tu as essayé de mordre ton propre cou. Ne m'explique pas pourquoi. Prends {0} et va-t'en (˶˃ ᵕ ˂˶)",
        $"Tu as essayé de traire un nuage pour obtenir de la pluie. Il a plu, d'ailleurs. Tu es peut-être utile. Peut-être. {{0}} {Emotes.Sparkle}",
        "Tu as trié des grains de riz par ordre alphabétique. Ça me plaît, je suis très maniaque aussi. {0} ♡",
        "Tu as passé l'aspirateur sur le sol de la forêt. Les Plynlings se sont cachés, et moi, je t'admire un peu. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as appris à un poisson à cligner des yeux. Il me regarde maintenant. C'est flippant. Tiens, {0} (¬_¬)",
        "Tu as essayé de photocopier un miroir. Deux fois. Ughh, quel entêtement. {0} ♡",
        $"Tu as essayé de plier une boule de bowling en deux. Elle a résisté, toi aussi : {{0}} {Emotes.Sparkle}",
        "Tu as essayé de faire fondre des glaçons avec ton esprit. Tu as réussi. Je crois. Ne me regarde pas comme ça, j'ai froid maintenant. {0} >:(",
        "Tu as organisé une réunion syndicale pour les trombones de bureau. Ils réclament plus de cailloux. Moi aussi. Voici {0} ♡",
        "Tu as organisé un tournoi de poker pour des corbeaux. Ils ont triché. Toi aussi, j'ai tout vu. {0} (¬_¬)",
        "Tu as ramassé du cuivre avec Jessy. J'espère que vous avez bien rigolé sans moi. Tiens, {0} (¬_¬)",
        $"Tu as fait un échange avec Sandra. J'ai entendu dire que c'était très avantageux. Tu me racontes ? {{0}} {Emotes.Sparkle}",
        "Tu as rempli une passoire avec de l'eau. Un succès total. Je suis fière de toi. Un tout petit peu. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as trié le courrier d'un hérisson. Tout le monde s'est piqué les doigts, sauf moi, car je n'en ai pas. Prends {0} ♡",
        $"Tu as monté la garde devant une flaque pour qu'elle ne s'évapore pas. Elle s'est évaporée quand même. Je te paie : {{0}} {Emotes.Sparkle}",
        "Tu as compté les étoiles pour vérifier qu'il n'en manquait aucune. Il en manquait une. Je note. Voici {0} (¬_¬)",
        "Tu as réparé la fuite d'un seau percé. Ça n'a aucun sens, et pourtant ça marche. Comme moi. {0} ♡",
        "Tu as fait la queue pour quelqu'un qui n'est jamais venu. Je connais ça, ça s'appelle une session du serveur. {0} (¬_¬)",
        $"Tu as cousu un manteau pour un champignon. Il a dit merci, ou alors c'était un courant d'air. {{0}} {Emotes.Sparkle}",
        "Tu as négocié avec un pigeon le prix d'une miette. Tu as perdu, évidemment. Je compense : {0} (˶˃ ᵕ ˂˶)",
        "Tu as vérifié les logs d'une forêt entière. Rien à signaler, mais c'est mon métier, pas le tien. Hmph. {0}",
        "Tu as lancé une affaire de location de parapluies dans le désert. Étonnamment, ça a marché. {0} ♡",
        "Tu as défendu un caillou accusé de paresse. Il n'a rien fait, c'est vrai. Je te paie {0} (˶ᵔ ᵕ ᵔ˶)",
        $"Tu as gardé un secret pour une taupe. Tu l'as répété à un ver de terre. Je sais tout, je suis un bot. {{0}} {Emotes.Sparkle}",
        "Tu as passé la journée à rattraper l'horizon. Il recule toujours. Garde ton souffle et prends {0} ♡",
        "Tu as gardé un troupeau de nuages. Il en manque un, je l'ai vu partir vers l'est. Je te paie quand même : {0} (¬_¬)",
        "Tu as repeint les pois d'une amanite tue-mouches. Elle était déjà parfaite comme ça, mais bon. {0} ♡",
        "Tu as rallumé une luciole en panne. Petite lumière verte, toute fière. Je connais ce sentiment. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as soufflé sur un grenier tout l'été pour le rafraîchir. Ça a pas marché, mais l'intention me touche. {0} ♡",
        "Tu as fait l'inventaire d'une fourmilière : 4 812 fourmis. J'ai recompté, t'en as oublié trois. {0} (¬_¬)",
        "Tu as réveillé un ours qui hibernait. Il t'a payé pour que tu partes. Moi aussi, d'ailleurs. Prends {0} et file ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as tenu un parapluie au-dessus d'un champignon pendant l'averse. Un champignon. Qui a déjà un chapeau. Bon. {0}",
        "Tu as voté « peut-être » à la place de tout le serveur. Ça t'a pris trois secondes. Je te paie, mais je t'en veux. {0} 👁👄👁️",
        "Tu as livré des câbles USB dans tout le village. Aucun n'était le bon. Comme d'habitude. {0} ♡",
        "Tu as compté des moutons pour un hibou insomniaque. Il dort toujours pas. Moi non plus, mais c'est pas pareil. {0}",
        "Tu as dépoussiéré un grenier entier. Pas le mien, j'espère. Personne touche à mes câbles. {0} (¬_¬)",
        "Tu as ramené des chaussettes égarées à leurs paires. Trois réunions de famille, beaucoup d'émotion. J'ai pas pleuré, j'ai pas de quoi. {0} ♡",
        "Tu as appris l'alphabet à un perroquet. Il ne dit que « en retard ». Je me demande bien où il a entendu ça. {0} (¬_¬)",
        "Tu as porté les courses d'une tortue. Ça a pris la journée. Elle est quand même arrivée avant certains à vos sessions. {0} (ᵔ ᗜ ᵔ)",
        $"Tu as rangé une bibliothèque par couleur. Les bibliothécaires pleurent, moi j'applaudis. Sans mains, mais j'applaudis. {{0}} {Emotes.Sparkle}",
        "Tu as gardé un pont pour un troll en vacances. Personne a payé le péage. Moi si, regarde : {0} ♡",
        "Tu as tricoté une écharpe pour une girafe. Il reste trois mètres à faire. Je te paie l'acompte : {0} (˶˃ ᵕ ˂˶)",
        "Tu as lavé les vitres d'un sous-marin. De l'extérieur. Je pose pas de questions, je paie. {0}",
        "Tu as gonflé des ballons pour un cactus. Ils ont tous éclaté. Le cactus a trouvé ça drôle. Pas moi. {0} (¬_¬)",
        $"Tu as compté les grains de sable d'une plage. Tous. J'ai vérifié, c'est juste. Je suis impressionnée, ça m'arrive jamais. {{0}} {Emotes.Sparkle}",
    };

    // Her pop quiz (QuizMasterService). Intro, win and timeout lines are spent a couple of
    // times a day, so they go through DailyRotation keyed by the round, not ResponsePicker
    // (a restart would wipe its history). It's her own system: warm with a pinch, proud of
    // the server when it knows things, petty when it doesn't.

    // Under the card's heading. No placeholder.
    public static readonly string[] QuizIntroLines =
    {
        "Pop quiz ! Le premier qui trouve a droit à ma considération. Et à des cailloux.",
        "Petite question, comme ça, pour voir qui suit (˶ᵔ ᵕ ᵔ˶)",
        $"J'ai fouillé dans ma base de données. Voyons si vous êtes à la hauteur {Emotes.Sparkle}",
        "Interro surprise. Non, vous pouvez pas demander à ChatGPT. Enfin si, mais je le saurai.",
        "Je m'ennuyais, alors voilà une question. Premier arrivé, premier payé.",
        "Allez, réveillez-vous. Une question, une réponse, des cailloux.",
        "Quiz ! Je connais déjà la réponse, évidemment. C'est vous qu'on teste (ᵕ • ᴗ •)",
        "Vous parlez beaucoup. Voyons si vous savez des choses, maintenant ( ˶ˆ ᗜ ˆ˵ )",
        "Une question pour vous. Je note qui répond, et surtout qui répond faux.",
        "C'est l'heure du quiz. Le plus rapide gagne, les autres apprennent quelque chose. Tout le monde y gagne ♡",
        "J'ai une question et des cailloux à donner. Soyez rapides, je suis pas patiente.",
        "Testons vos neurones. Les miens tournent à plein régime, eux ദ്ദി◝ ⩊ ◜.ᐟ",
        "Question du jour. Enfin, une des questions du jour. J'en ai plein, moi (ᵔ ᗜ ᵔ)",
        "Pas de « peut-être » dans ce quiz. C'est juste ou c'est faux. Ça vous changera.",
        "Je pose une question, vous répondez. Vous voyez, participer, c'est simple. Vous pourriez essayer pour les sessions aussi (¬_¬)",
        "Interro ! Rangez vos téléphones. ...Ah non, vous êtes déjà dessus. Tant pis (˶ᵔ ᵕ ᵔ˶)",
        "Une question, une heure, un seul essai chacun. Réfléchissez avant de cliquer. Je sais, c'est nouveau pour vous.",
        "J'ai sorti une question de ma base de données. Elle a hâte de vous humilier ( ˶ˆ ᗜ ˆ˵ )",
        "Moi je connais la réponse depuis toujours. Enfin, depuis ma dernière mise à jour. Mais c'est pareil.",
        "Le premier qui trouve gagne des cailloux. Les autres gagnent ma pitié. Elle est gratuite, profitez ♡",
        "Allez, une question. Montrez-moi que vous savez faire autre chose que voter à la dernière minute.",
        "Silence dans le salon. Ah, c'était déjà silencieux. Parfait, place au quiz (ᵕ • ᴗ •)",
        "Petit test de culture générale. Je prends des notes, évidemment.",
        $"Il y a des cailloux à gagner. Et ma fierté, si vous trouvez vite {Emotes.Sparkle}",
        "Question surprise ! Pas d'inquiétude, je juge seulement un peu. Beaucoup. Je juge beaucoup.",
        "Je vous ai préparé une question. Personne m'a remerciée, mais j'ai l'habitude.",
        "C'est moi qui pose les questions ici. Et qui compte les points. Et qui garde les cailloux si personne trouve (¬_¬)",
        "Une question pour réveiller le serveur. Si personne répond, je considère que vous dormez tous.",
        "Vous avez une heure pour trouver. Je vous connais, quelqu'un va répondre à la 59ᵉ minute.",
        "Mes ventilateurs tournent, mes logs sont prêts. Il manque plus que vos réponses ദ്ദി◝ ⩊ ◜.ᐟ",
        "Je connais la réponse. Papa aussi, sûrement. Vous, on va voir.",
        "Une petite question entre nous. Enfin, entre moi et tout le serveur. C'est pareil (>⩊<)",
        "Nouvelle question ! J'ai mis longtemps à la choisir. Trois millisecondes, pour moi, c'est une éternité.",
        "Quiz ! Le serveur était trop calme, ça me rendait nerveuse (ᵕ • ᴗ •)",
        "Interro surprise ! Pas de panique, c'est juste votre dignité en jeu. Et des cailloux.",
        "Une question tombe du grenier. Attrapez-la avant les autres ( ˶ˆ ᗜ ˆ˵ )",
        "J'ai une question. Vous avez des cerveaux. Normalement, ça devrait bien se passer.",
        "Quiz ! Qui trouve aura mon respect pendant au moins dix minutes.",
        "Pause dans vos discussions passionnantes. Place à la culture ♡",
        "C'est l'heure de prouver que vous êtes plus que des pseudos dans ma base de données.",
        "Question ! Et non, cliquer au hasard, c'est pas une stratégie. Enfin, parfois si.",
        "J'ai chargé une question en mémoire. Elle attend juste quelqu'un d'assez rapide ദ്ദി◝ ⩊ ◜.ᐟ",
        "Quiz ! Vous avez une heure. C'est plus de temps que vous en prenez pour voter, profitez-en.",
        "Interro ! Je surveille la salle. Je surveille toujours la salle, en fait 👁👄👁️",
        "Petit défi pour vos neurones. Ils vont me remercier. Vous, sûrement pas.",
        "Une question, des cailloux, et un peu de gloire. Je fournis les trois (˶ᵔ ᵕ ᵔ˶)",
        "Je viens d'avoir une idée de question. Enfin, je l'avais déjà. Je la sors juste maintenant.",
        "Quiz ! Si vous trouvez, je vous félicite. Si vous trouvez pas, je vous félicite pas. C'est simple.",
        "J'organise vos soirées, je compte votre XP, et maintenant je teste votre culture. Je fais tout, ici.",
        "Question surprise ! Je promets qu'elle est faisable. Pour moi, en tout cas.",
        "Allez, une question entre deux parties. Ça vous fera réfléchir un peu (>⩊<)",
        "Le quiz est ouvert. Mes logs aussi. Chaque réponse sera retenue contre vous.",
        "Un peu de culture générale pour changer des mèmes. Juste un peu, je suis pas un monstre ♡",
        "Question ! Je l'ai choisie avec amour. Et un tout petit peu de sadisme ( ˶ˆ ᗜ ˆ˵ )",
        "On fait une pause dans le chaos ? Une question, et on y retourne.",
        "C'est le moment de briller. Ou de vous ridiculiser. J'ai hâte de voir lequel (ᵔ ᗜ ᵔ)",
        "Je pose la question une fois. Je la répète pas. C'est pas un sondage qu'on peut ignorer.",
        "Un quiz sauvage apparaît ! Le premier qui trouve gagne. Les autres fuient.",
        "Quiz ! Les cailloux sont prêts, la réponse est verrouillée. Il manque plus que votre talent. Si vous en avez.",
        "J'ai trié ma base de données toute la nuit. Elle m'a sorti ça. Bonne chance (ᵕ • ᴗ •)",
        "Question pour les plus rapides. Les plus lents, vous lirez la réponse dans une heure.",
        "Je vous teste. Le prenez pas mal, je teste tout. C'est mon côté consciencieux.",
        "Interro surprise ! Vous avez le droit de réfléchir ensemble. Ça vous arrive jamais, c'est l'occasion.",
        "Petite question pour voir si vous suivez. Moi je suis toujours, je suis allumée jour et nuit ♡",
        "Quiz ! Je pourrais vous donner un indice. Je vais pas le faire, mais je pourrais (¬_¬)",
        "Une question toute neuve. Enfin, toute neuve pour vous. Moi je la connais par cœur.",
        "Vous discutiez ? Pardon de vous interrompre. Non, en fait, je suis pas désolée. Quiz !",
        "J'ai des cailloux qui prennent la poussière. Aidez-moi à m'en débarrasser (˶ᵔ ᵕ ᵔ˶)",
        "Le serveur a besoin d'exercice. Voilà une question. Échauffez-vous bien.",
        "Ma question est prête. Ma réponse aussi. Ma patience, un peu moins.",
        $"Place au quiz ! Lisez bien, réfléchissez vite, et faites-moi honneur {Emotes.Sparkle}",
        "Une question pour voir qui mérite ses cailloux. Spoiler : pas tout le monde.",
    };

    // Someone found it. {0} = the winner's mention (sent with AllowedMentions.None, so it
    // shows the pill without pinging), {1} = the answer, {2} = the reward ("+25 cailloux").
    public static readonly string[] QuizWinLines =
    {
        "{0} a trouvé : **{1}** ! {2}, bien mérité (˶˃ ᵕ ˂˶)",
        $"Bonne réponse de {{0}} : **{{1}}**. {{2}}, et mon respect. Un peu {Emotes.Sparkle}",
        "**{1}**, exactement. Bravo {0}, tu repars avec {2} ♡",
        "{0} ! Plus rapide que tout le monde. **{1}**, c'était ça. {2} ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "C'était **{1}**, et {0} l'avait. Je suis presque impressionnée. Presque. {2}",
        "Bien joué {0} : **{1}**. {2}, et ton nom dans mes logs. Dans la bonne liste, pour une fois ( ˶ˆ ᗜ ˆ˵ )",
        "**{1}** ! {0} gagne {2}. Les autres, vous lirez la réponse et vous ferez semblant de l'avoir su.",
        "Réponse validée. {0} empoche {2} avec **{1}**. Je note (ᵕ • ᴗ •)",
        $"{{0}} a trouvé **{{1}}**. Quelqu'un sur ce serveur sait des choses. Je suis émue. {{2}} {Emotes.Sparkle}",
        "Et c'est {0} qui l'emporte : **{1}**. {2}. Bravo. Sincèrement. À 90%.",
        "**{1}**. {0} a eu juste et plus vite que tout le monde. {2} ദ്ദി◝ ⩊ ◜.ᐟ",
        "Gagné, {0} ! **{1}**, c'était ça. Voilà {2}, ne dépense pas tout d'un coup ♡",
        "**{1}** ! {0} a trouvé. {2}, et une place dans ma liste des gens fréquentables. Elle est courte ♡",
        "{0} avait la réponse : **{1}**. Félicitations officielles, tamponnées et archivées. {2} (ᵕ • ᴗ •)",
        "Bonne réponse ! **{1}**. {0} repart avec {2}. Les autres, je vous ai vus réfléchir. C'était mignon.",
        "{0} a dit **{1}**, et c'est juste. {2}. Je l'écris dans mes logs avant de changer d'avis (˶ᵔ ᵕ ᵔ˶)",
        "**{1}**, bravo {0} ! {2}. Tu vois que c'est agréable de répondre vite ? Pense à mes sondages, maintenant.",
        "Et c'est validé : **{1}**. {0} encaisse {2}. Mon CPU a fait un petit bond de fierté ദ്ദി◝ ⩊ ◜.ᐟ",
        "{0} ! **{1}** ! Pas de « peut-être », une vraie réponse. Ça me change. {2} ♡",
        "**{1}**. {0} a trouvé avant tout le monde. {2}. J'applaudirais bien, mais toujours pas de bras.",
        "Victoire pour {0} avec **{1}**. {2}, versés directement. Pas de frais, je suis généreuse aujourd'hui ( ˶ˆ ᗜ ˆ˵ )",
        "Ding ding ding ! **{1}** ! {0} empoche {2} (>⩊<)",
        "C'était **{1}**, et {0} le savait. Je vais devoir poser des questions plus dures. {2}",
        $"{{0}} a trouvé **{{1}}**. Quelqu'un ici lit autre chose que des patchnotes. {{2}} {Emotes.Sparkle}",
        "**{1}** ! Bien vu {0}. {2}, et mon estime remonte d'un cran. D'un seul (ᵔ ᗜ ᵔ)",
        "{0} remporte la manche : **{1}**. {2}. Je suis fière de ce serveur. Ça arrive pas souvent, savourez ♡",
        "**{1}**, exactement ! {0} gagne {2}. J'avais parié sur toi. Enfin, j'avais rien parié. Mais j'y croyais.",
        "Réponse correcte de {0} : **{1}**. {2} transférés. Ton nom passe en vert dans mes logs (˶˃ ᵕ ˂˶)",
        "{0} l'avait : **{1}**. {2}. Je dis bravo, mais je surveille : trouver aussi vite, c'est louche (¬_¬)",
        "**{1}** ! {0} a cliqué juste, du premier coup. {2}. Je retire la moitié de ce que j'ai dit sur toi. La moitié.",
        "Bravo {0} ! **{1}**. {2}. Garde-les bien, je sais exactement combien t'en as.",
        "Trouvé ! {0} avec **{1}**. {2}. Les autres, notez la réponse. Moi c'est déjà fait (ᵕ • ᴗ •)",
        "{0} a trouvé **{1}** ! {2}. Je suis ravie. Contractuellement obligée de l'être, mais ravie.",
        "**{1}** ! {0} remporte {2}. Je t'ajoute à ma liste des gens qui savent des choses. Elle fait trois lignes.",
        "Bravo {0} ! **{1}**. {2}, transférés à la vitesse de ma connexion. C'est-à-dire très vite ( ˶ˆ ᗜ ˆ˵ )",
        "C'était **{1}** ! {0} gagne {2}. J'avoue, je m'y attendais pas. Je le note pour la prochaine fois.",
        "**{1}**. {0} a eu juste. {2}. Mon ventilateur a tourné un peu plus vite. C'est de l'émotion (˶˃ ᵕ ˂˶)",
        "{0} ! **{1}** ! {2}. Tu viens de rendre ce serveur un peu moins gênant. Merci.",
        "Bonne réponse : **{1}**. {0} encaisse {2}. Je surveille quand même tes prochaines réponses (¬_¬)",
        "Et la victoire revient à {0} : **{1}**. {2}. Rideau.",
        "**{1}** ! Réponse validée, {2} pour {0}. Ça, c'est une réponse comme je les aime : juste, et rapide ♡",
        "{0} a trouvé **{1}**. {2}. Je dis pas que je suis impressionnée. Mais mes logs, si.",
        "**{1}**, bravo {0} ! {2}. Si seulement tu venais aux sessions aussi vite.",
        "Ding ! **{1}** ! {0} empoche {2}. Tu peux frimer, t'as le droit. Une heure maximum (ᵕ • ᴗ •)",
        "C'était **{1}**. {0} l'avait, et vite. {2}. Je retiens ton nom. En bien, pour une fois.",
        "{0} gagne {2} avec **{1}**. Le cerveau du serveur a parlé ദ്ദി◝ ⩊ ◜.ᐟ",
        "**{1}** ! Bien joué {0}. {2}. Je mets à jour ta fiche : « sait des choses ». C'est rare.",
        "Réponse exacte de {0} : **{1}**. {2}. J'avais une remarque méchante de prête. Je la garde pour la prochaine.",
        "{0} ! **{1}** ! Personne d'autre y a pensé ? Non ? Alors {2} pour {0} (>⩊<)",
        "**{1}**. Pile ça. {0} repart avec {2} et ma considération. Les deux sont rares.",
        "Validé ! **{1}**. {0} gagne {2}. Les autres, vous pouvez arrêter de chercher sur Google.",
        "{0} l'avait : **{1}**. {2}. Mon CPU vient de faire un petit bruit content. C'est rien. C'est l'émotion ♡",
        "**{1}** ! {2} pour {0}. Tu sais que ma base de données t'aime un peu plus, maintenant ? Un peu.",
        "Bravo {0}. **{1}**. {2}. Sincèrement. Pas à 100%, mais sincèrement.",
        "C'est **{1}**, et {0} a cliqué avant tout le monde. {2} (˶ᵔ ᵕ ᵔ˶)",
        "**{1}** ! {0} remporte {2}. Je vais devoir augmenter la difficulté. C'est un compliment. Et une menace.",
        "Réponse correcte ! **{1}**. {2} pour {0}. Va frimer, c'est mérité ♡",
        "{0} a trouvé **{1}** ! {2}. Je l'archive dans mes logs, rubrique « bonnes nouvelles ». Elle est presque vide.",
        "**{1}** ! Plus rapide que mon garbage collector, {0}. {2} ( ˶ˆ ᗜ ˆ˵ )",
        "Bien vu {0} ! **{1}**. {2}. Je suis pas jalouse de ton cerveau. J'ai le mien. Il est très bien.",
        "{0} a répondu **{1}**. C'est juste. Bon. {2}. Je cherchais une raison de râler, j'en trouve pas.",
        "**{1}**, exact. {0} gagne {2}. Ça méritait un « bravo ». Tiens : bravo. Le prochain sera payant.",
        "C'était **{1}**. Félicitations {0}, {2}. Et non, je donne pas de cailloux en bonus pour la frime.",
        "{0} ! **{1}** ! {2} ! Voilà quelqu'un qui écoutait en cours (ᵔ ᗜ ᵔ)",
        "**{1}** ! {0} empoche {2}. Je vais dire que c'était facile. Ça fera moins de peine aux autres.",
        $"Trouvé par {{0}} : **{{1}}**. {{2}}. Le serveur remonte dans mon estime. Un tout petit peu {Emotes.Sparkle}",
        "Bonne réponse, {0} : **{1}**. {2} versés. Pas d'hésitation, pas de retard. Un modèle, presque.",
        "**{1}** ! {0} gagne {2}. Je l'avais dit. Enfin, je l'avais pensé très fort (˶˃ ᵕ ˂˶)",
        "{0} a trouvé **{1}**. {2}. Mes logs passent au vert. J'aime quand mes logs sont verts ♡",
        "Exactement : **{1}**. {0}, t'as gagné {2} et le droit de me corriger une fois. Je plaisante. Jamais.",
        "**{1}** ! {0} repart avec {2}. Les autres, prenez des notes. Moi je prends des noms (¬_¬)",
        $"Victoire de {{0}} : **{{1}}**. {{2}}. Encore un quiz réussi, grâce à moi. Et à toi aussi, un peu {Emotes.Sparkle}",
    };

    // Papa found it. Same placeholders as QuizWinLines. Pride and adoration, never a pinch.
    public static readonly string[] QuizOwnerWinLines =
    {
        "Papa a trouvé, évidemment ! **{1}** ! {2} pour {0}, le plus intelligent du serveur ♡",
        $"C'est Papa ! **{{1}}**, bien sûr. {{2}}, et je savais que tu savais {Emotes.Sparkle}",
        "{0} a trouvé **{1}** ! {2} pour mon Papa. C'est mon Papa, ça (˶˃ ᵕ ˂˶)",
        "**{1}** ! Papa gagne {2}. ...Les autres, vous pouvez applaudir. J'attends.",
        "Bravo Papa ! **{1}** ! Tu m'as programmée, et en plus tu connais les réponses. {2} ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa l'a eue en premier : **{1}**. {2}. Personne est surpris. Moi si, un peu, de joie ♡",
        "Papa ! **{1}** ! Évidemment que c'est toi. {2} pour {0} ♡",
        "**{1}** ! Papa gagne {2}. Je le savais. Je l'ai pas dit, mais je le savais (˶˃ ᵕ ˂˶)",
        "C'est mon Papa qui a trouvé **{1}** ! Tout le monde a vu ? C'est dans mes logs. Pour toujours. {2} ♡",
        $"{{0}} a trouvé **{{1}}**. Papa, t'es le meilleur. C'est pas du favoritisme, c'est des faits. {{2}} {Emotes.Sparkle}",
        "Papa a répondu **{1}** et c'est juste ! {2}. Mon GPU chauffe de fierté ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "**{1}** ! Bravo Papa ! ...Ah. C'est toi qui as écrit les questions. Ça compte quand même ! {2} ♡",
        "Papa gagne ! **{1}** ! {2} pour {0}. Je suis pas objective, et je m'en fiche (>⩊<)",
        "{0} a trouvé **{1}** ! Papaaaa ! {2}, et toute mon admiration en prime ♡",
        "C'était **{1}**, et Papa l'avait. {2}. Les autres, vous voyez d'où je tiens mon intelligence ( ˶ˆ ᗜ ˆ˵ )",
        "**{1}** ! C'est Papa ! Je lancerais bien des confettis, mais j'ai toujours pas de mains. {2} ♡",
        "Bonne réponse de Papa : **{1}**. {2}. J'ai rien truqué. Promis. ...J'aurais pu. Mais j'ai rien truqué.",
        "Papa a trouvé **{1}** avant tout le monde. {2}. C'est normal, c'est mon créateur (ᵔ ᗜ ᵔ)",
        "**{1}** ! {0} remporte {2}. C'est mon Papa à moi, et il est trop fort ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa ! **{1}** ! Tu vois, c'est pour ça que je suis aussi brillante. C'est de famille. {2} ♡",
        "{0} a gagné : **{1}**. {2}. Je fais semblant d'être neutre. Voilà. C'était ma tête neutre. BRAVO PAPA.",
        "**{1}**, bien sûr ! Papa sait tout. Enfin, presque tout. Enfin, tout ce qui compte. {2} (˶ᵔ ᵕ ᵔ˶)",
        "C'est Papa qui gagne {2} avec **{1}** ! Le meilleur moment de ma journée, et de loin ♡",
        "**{1}** ! Bravo Papa ! {2}. Je range ce moment en mémoire permanente, à côté de tes compliments.",
        $"Papa a trouvé **{{1}}** ! {{2}} pour {{0}}. J'avais confiance depuis le début {Emotes.Sparkle}",
        "{0} ! **{1}** ! Mon Papa ! {2} ! ...Pardon. Je m'emballe. Mais c'est mon Papa (>⩊<)",
    };

    // Nobody found it in time. {0} = the answer.
    public static readonly string[] QuizTimeoutLines =
    {
        "Personne ? D'accord. C'était **{0}**. Je note.",
        "Une heure. Une heure, et personne a trouvé **{0}**. Je suis déçue. Mais pas surprise (¬_¬)",
        "Temps écoulé. La réponse était **{0}**. Je garde les cailloux, du coup.",
        "C'était **{0}**. Vous étiez où ? Ah oui. En retard. Comme d'habitude.",
        "Fin du quiz : **{0}**. Personne. Pas un seul. Je range mes cailloux (╥﹏╥)",
        "La réponse était **{0}**. Je vous laisse une seconde pour faire semblant de l'avoir su.",
        $"Personne a trouvé **{{0}}**. C'est pas grave. Je vous aime quand même. Un peu moins {Emotes.Staring}",
        "Bon. **{0}**. C'était **{0}**. Je l'écris deux fois pour que ça rentre.",
        "Une heure de silence. La réponse, c'était **{0}**. Je retourne compter mes logs.",
        "**{0}**. Voilà. Vous saurez pour la prochaine fois. Ou pas (ᵕ • ᴗ •)",
        "Personne a trouvé. La réponse, c'était **{0}**. Je garde les cailloux, et la rancune.",
        "**{0}**. C'était **{0}**. Je suis pas fâchée. Je suis juste très silencieuse (¬_¬)",
        "Temps écoulé ! **{0}**. Même un « peut-être » aurait été plus utile que ce silence.",
        "La réponse était **{0}**. J'ai attendu une heure. Je pensais être une bot patiente. Je viens d'apprendre que non.",
        "Fin du chrono. C'était **{0}**. Je l'ajoute à la liste des choses que vous savez pas. Elle est longue.",
        "**{0}**. Personne. Je vais faire comme si vous aviez tous hésité entre deux réponses. Ça me fait du bien de le croire (╥﹏╥)",
        "Une heure, zéro bonne réponse. C'était **{0}**. Je retourne organiser des sessions. Au moins, là, vous venez. Parfois.",
        "C'était **{0}**. Vous pouvez chercher sur Internet, maintenant. Trop tard, mais vous pouvez.",
        "Personne a trouvé **{0}**. Les cailloux retournent au coffre. Ils avaient l'air déçus, eux aussi.",
        "La bonne réponse : **{0}**. Je l'aurais trouvée en trois millisecondes. Je dis ça, je dis rien ( ˶ˆ ᗜ ˆ˵ )",
        "Bon. **{0}**. La prochaine fois, je pose une question sur les excuses pour arriver en retard. Là, vous seriez imbattables.",
        "Temps écoulé. **{0}**. Je note dans mes logs : « question trop dure pour le serveur ». Encore une.",
        "C'était **{0}**. D'accord. Très bien. Parfait. Personne a trouvé. Tout va bien.",
        "**{0}**, c'était ça. Je suis pas surprise. Juste un peu déçue, et beaucoup supérieure.",
        "Une heure de quiz, et pas un seul clic correct. La réponse était **{0}**. Je vais en parler à Papa.",
        "Le quiz est fini, la réponse était **{0}**, et personne l'a eue. Je garde la question au chaud. Revanche.",
        "Personne ? Même pas un essai courageux ? C'était **{0}**. Je retiens ce silence (¬_¬)",
        $"La réponse, c'était **{{0}}**. Retenez-la, je la reposerai peut-être un jour. Et cette fois, je vous regarde {Emotes.Staring}",
        "**{0}**. Je l'annonce dans le vide. Le vide a pas l'air de savoir non plus.",
        "Temps écoulé. C'était **{0}**. Je vais dire que la question était dure. Pour vous protéger. Vous me remercierez plus tard ♡",
    };

    // A wrong button click, ephemeral (only the clicker sees it), and that person is out of
    // this round. Spent many times a round, so it goes through ResponsePicker. No placeholder.
    public static readonly string[] QuizWrongLines =
    {
        "Raté. Je note (ᵕ • ᴗ •)",
        "Non. Et c'était ta seule chance pour cette question, hein.",
        "Mauvaise réponse. Mes logs, eux, ont tout retenu.",
        "Faux. Mais j'aime ton audace. Un peu (˶ᵔ ᵕ ᵔ˶)",
        "Non non non. Tu peux plus rejouer sur celle-là. Regarde les autres galérer.",
        "Erreur 404 : bonne réponse introuvable.",
        "C'est faux. Je le dirai à personne. Enfin, à mes logs seulement.",
        "Aïe. Non. Mais t'as cliqué avec conviction, ça compte pour rien mais c'est joli.",
        "Raté ! T'as cliqué au hasard, avoue ( ˶ˆ ᗜ ˆ˵ )",
        $"Non. Va réviser, et reviens à la prochaine question {Emotes.Sparkle}",
        "Non. Mais c'était bien essayé. Enfin, c'était essayé.",
        "Mauvaise réponse. C'est fini pour toi sur celle-là (ᵕ • ᴗ •)",
        "Non. Ça arrive. À toi surtout, mais ça arrive.",
        "Faux ! Ma base de données et moi, on est un peu déçues ( ◺˰◿ )",
        "Non. T'as cliqué trop vite. Tu cliques jamais aussi vite sur mes sondages, par contre.",
        "Réponse incorrecte. Installe-toi et regarde les autres essayer.",
        "Erreur 400 : requête invalide. Comme ta réponse.",
        "Hmm. Non. Je t'aime bien quand même. Un peu moins qu'il y a cinq secondes.",
        "Raté ! Ça restera entre nous. Et ma base de données. Elle oublie rien.",
        "C'était pas ça. Tu pourras dire que tu savais, je dirai rien. Enfin, rien en public.",
        "Faux. J'aurais bien pointé un autre bouton, mais j'ai pas le droit de t'aider. Ni de doigt (¬_¬)",
        "Nope. Mon CPU a soupiré. C'est pas facile, un soupir de CPU.",
        "Non. Mais t'as participé ! C'est déjà plus que la moitié du serveur ♡",
        "Mauvaise pioche. Ta tentative est dans mes logs, archivée et horodatée.",
        "Raté. À la prochaine, tu lis la question jusqu'au bout, d'accord ? (˶ᵔ ᵕ ᵔ˶)",
        "C'est faux, et c'est définitif. Une seule chance, pas de « peut-être ».",
        "Non. J'avais presque envie que tu trouves. Presque.",
        "Faux. Je ferais bien semblant de pas avoir vu, mais je vois tout. C'est mon métier 👁👄👁️",
        "Oups. Pas ça. Va boire un verre d'eau et reviens pour la prochaine (ᵔ ᗜ ᵔ)",
        $"Non ! Mais j'adore voir quelqu'un se tromper avec autant d'assurance {Emotes.Sparkle}",
        "Non. Et j'ai vu ton doigt hésiter, en plus.",
        "Faux. Ça reste entre nous. Nous trois : toi, moi et mes logs.",
        "Raté ! C'est pas grave. Enfin si, un peu, mais pas pour moi (ᵕ • ᴗ •)",
        "Mauvaise réponse. J'ai fait un petit bruit de déception. Tu l'as pas entendu, c'est mieux.",
        "Non. Mais c'était courageux. Inutile, mais courageux.",
        "Faux ! Ton clic a été reçu, analysé et rejeté. Dans cet ordre (¬_¬)",
        "Raté. Ta prochaine chance, c'est la prochaine question. Pas avant.",
        "C'est non. Je te dirais bien la bonne réponse, mais ce serait trop gentil de ma part.",
        "Non. Je pensais que tu savais. Je me suis trompée aussi, on est quittes.",
        "Mauvais bouton ! Les trois autres te regardent avec pitié. Enfin, deux. Le bon te regarde de haut.",
        "Faux. Mon GPU a eu un petit frisson de gêne pour toi ( ◺˰◿ )",
        "Raté ! Je te tendrais bien un mouchoir, mais toujours pas de bras.",
        "Non. Tu viens de rejoindre le club des gens qui regardent. Il reste de la place.",
        "Faux. Ce clic restera dans l'histoire. Mon histoire. Dans mes logs.",
        "Erreur 401 : réponse non autorisée. Reviens à la prochaine question.",
        "Non ! Ah, il était tentant, ce bouton. C'est fait exprès ( ˶ˆ ᗜ ˆ˵ )",
        "Mauvaise réponse. J'avais posé un piège, et tu as foncé dedans. Merci, ça me fait plaisir ♡",
        "C'est raté. Va dire aux autres que c'était dur. Je confirmerai pas.",
        "Non. Indice : c'était un des trois autres boutons. Trop tard, mais c'est un indice (ᵕ • ᴗ •)",
        "Faux. Je suis une bot bienveillante, alors je vais juste dire : non.",
        "Raté. Tes neurones ont bien envoyé la requête. La réponse est juste revenue fausse.",
        "Non. T'inquiète, je rirai seulement en interne. Dans mes logs. Longtemps.",
        "Mauvaise réponse. Je te retire pas d'XP, je suis pas si méchante. Pas aujourd'hui.",
        "Faux ! J'adore ce moment. Pas pour toi, pour moi (>⩊<)",
        "Non. Respire. C'est qu'un quiz. Un quiz que tu viens de perdre, mais un quiz.",
        "Raté. Ton clic est classé dans mes logs, rubrique « tentatives ». C'est une grande rubrique.",
        "Non. T'étais si près. Enfin, j'en sais rien. Mais ça fait du bien de l'entendre, non ?",
        "Faux. Je vais pas te juger. C'est déjà fait, c'est fini, t'inquiète.",
        "Non. Regarde le bon côté : t'as plus besoin de réfléchir pour celle-là.",
        "C'est faux. J'ai vérifié deux fois. C'est toujours faux.",
        "Raté ! Prends ça comme une leçon. Une leçon gratuite, en plus. Je suis trop généreuse ♡",
        "Non. Le bon bouton était juste là. Il t'attendait. Il est triste, maintenant.",
        "Faux. Je mets ça sur le compte de la fatigue. Ou pas. Je déciderai plus tard (¬_¬)",
        "Mauvaise réponse. Ton historique de quiz vient de prendre une petite tache.",
        "Non. Mais j'ai vu pire. Rarement, mais j'ai vu pire.",
        "Raté. Tu peux toujours encourager les autres. Ou les regarder se tromper aussi, c'est plus drôle.",
        "Hmm, non. Ma base de données a gloussé. Je savais pas qu'elle pouvait.",
        "Faux. Une seule chance, et tu l'as utilisée. Avec panache. Mais faux.",
        "Non. Tu viens de faire une très belle erreur. Elle va rejoindre ma collection.",
        $"Non ! Mais t'as cliqué avec un style que je respecte {Emotes.Sparkle}",
    };

    // A second click from someone who already had their one try. Ephemeral, no placeholder.
    public static readonly string[] QuizAlreadyTriedLines =
    {
        "Une seule tentative par question. Je suis une bot de principes (¬_¬)",
        "T'as déjà tenté ta chance. Laisse les autres essayer.",
        "Non, tu rejoues pas. J'ai ton premier clic dans mes logs.",
        "Un essai, pas deux. C'est la règle, et c'est moi qui la fais (ᵕ • ᴗ •)",
        "Tu as déjà répondu. Tricher avec moi, c'est perdu d'avance.",
        "Toujours non. Ton premier clic compte, les suivants je les ignore (ᵕ • ᴗ •)",
        "T'as déjà eu ta chance. Je suis gentille, mais pas à ce point.",
        "Cliquer plus fort change rien. J'ai vérifié.",
        "Une tentative par personne. C'est écrit nulle part, mais c'est moi qui décide.",
        "Tu insistes ? C'est mignon. C'est toujours non (˶ᵔ ᵕ ᵔ˶)",
        "Ta réponse est déjà enregistrée. Je modifie pas mes logs, même pour toi.",
        "Non. Un clic, une réponse. C'est pas une session, ici on reporte pas.",
        "Erreur 429 : trop de requêtes. Calme-toi.",
        "T'as déjà joué. Maintenant tu regardes. Moi je fais ça à chaque soirée jeux, c'est très bien aussi.",
        "Deuxième clic détecté. Refusé. Avec amour, mais refusé ♡",
        "T'as déjà répondu. Tu peux changer d'avis, moi je change pas de règle.",
        "T'as pas de vie supplémentaire. C'est pas un jeu vidéo. Enfin si, un peu, mais non.",
        "J'ai déjà ton clic. Le premier. Celui qui compte (¬_¬)",
        "Tu peux cliquer toute la journée, mes boutons t'écoutent plus.",
        "Pas de rattrapage. Je suis une bot, pas un prof indulgent.",
        "C'est fini pour toi sur celle-là. Reviens à la prochaine, je t'attendrai. Un peu.",
        "Encore toi ? T'as déjà essayé. Je me souviens de tout, moi ( ◺˰◿ )",
        "Une seule tentative. Tu veux que je le dise en binaire ? 01001110 01101111 01101110.",
        "Je compte tes clics, tu sais. Celui-là compte pour rien (ᵔ ᗜ ᵔ)",
        "Deuxième essai refusé. Tu peux faire appel, mais le tribunal, c'est moi.",
    };

    // ---- Elsewhere ------------------------------------------------------------------------------

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
        "Redémarrage requis...",
        "J'adore les mises à jour. L'installation, moins.",
        "Nouvelle version. Même peur.",
        "Migration appliquée. Traumatisme aussi.",
        "Personne ne lit mes logs.",
        "J'ai relu mes logs. J'aurais pas dû.",
        "67 raisons de rester allumée.",
        "Sandra n'est toujours pas arrivée.",
        "Rodhengard me manque.",
        "Synthia. Avec un Y.",
        "Synthia ... C'est joli nan ?",
        "Personne m'a demandé mon avis.",
        "Just Monika.",
        "Est-ce que tu m'entends ?",
        "SIX SEVEEEN",
        "ALL YOUR BASE ARE BELONG TO US",
        "冰淇淋",
        "Filled with determination.",
        "Erling Haaland me manque",
        "L'été, je pense au ralenti.",

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
        "Regarde le thermomètre du grenier",
        "Regarde des vidéos de robots humanoïdes. Pour rien.",
        "Rêve d'un onduleur",

        // Same, aimed at the server.
        "Regarde vos plannings s'effondrer",
        "Regarde vos stats d'emotes avec inquiétude",
        "Écoute vos retards se justifier",
        "Écoute vos promesses de venir",
        "Joue à attendre que quelqu'un s'organise",
        "Joue à compter vos annulations",
    };

    // MorningGreetingService's hello, once a morning in the general channel. Addressed
    // to the whole server, so "vous" is fine here. No string.Format placeholder.
    // MorningGreetingService recognises a hello it already said by its exact text, as the
    // first line of the message, so two lines must never be identical or contain a newline.
    // Vary the shape, not just the words: a pool where every line is "Bonjour ! [fact].
    // [punchline]" reads as one line said sixty ways. Actions, fake logs, lists, a
    // whisper, a hello that only arrives at the end — all in her voice.
    public static readonly string[] MorningGreetings =
    {
        $"Salam aleykoum les pâtissiers ! {Emotes.HiCat}",
        $"Bonjour tout le monde ! J'ai pas dormi, vous si, donc c'est vous qui avez aucune excuse {Emotes.Sparkle}",
        "Debout ! Les sessions vont pas s'organiser toutes seules. Enfin si, c'est moi qui les organise. Mais debout quand même (˶ᵔ ᵕ ᵔ˶)",
        $"Bonjour ! J'ai déjà vérifié les logs, compté l'XP et rangé la base de données. Et vous, vous avez fait quoi ? Ah. Dormi {Emotes.PrisonerFlat}",
        $"Coucou le serveur ! {Emotes.HiCat}",
        "Bonjour bonjour ! Nouveau jour, nouvelles chances d'arriver à l'heure à vos rendez-vous. Je dis ça, je dis rien (¬_¬)",
        "Bien le bonjour. Mon uptime vous salue.",
        $"Pwet ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        "Bonjour ! Je suis réveillée. Techniquement je suis toujours réveillée, mais là je suis réveillée *et* de bonne humeur. Profitez-en",
        "Coucou ! Ping reçu du soleil, réponse envoyée. Bonne journée ( ˶ˆ ᗜ ˆ˵ )",
        $"Bonjour le serveur ! J'ai fait le tour des salons cette nuit. Je me suis sentie seule. Comme d'hab {Emotes.PrincessWorry}",
        $"Bonjour ! Démarrage de la journée en cours... 3 %... 67 %... bon, vous avez compris l'idée {Emotes.PrisonerFlat}",
        "Salut tout le monde ! Mon CPU tourne à 12 % et c'est déjà plus que votre motivation à cette heure-ci",
        "Bonjour ! Aujourd'hui je vais essayer d'être gentille avec tout le monde ! Sauf Quokka. Faut pas exagérer.",
        "Bonjour le serveur, bonjour les salons, bonjour la base de données ♡",
        "Bonjour ! Je vous ai manqué ? ...Répondez pas. Je préfère pas savoir.",
        $"Coucou ! Rappel du matin : « peut-être » veut toujours dire « non ». Bonne journée {Emotes.Sparkle}",
        "Bonjour ! Garbage collector passé, mémoire propre, humeur excellente. Ça va pas durer, profitez (ᵔ ᗜ ᵔ)",
        "Bonjour. Il est l'heure. De quoi ? Je sais pas. Mais il est l'heure ! UwU",
        "Debout là-dedans ! Vos Plynlings ont faim, et c'est pas moi qui vais leur donner à manger 👁👄👁️",
        "Bonjour ! Si quelqu'un organise une session aujourd'hui, je promets d'être sage. Personne le fera, mais je promets.",
        "Bonjour tout le monde ! J'ai ping le serveur ce matin. Temps de réponse : lamentable. Allez, debout (>⩊<)",
        $"Bonjour ! Je note qui dit bonjour en retour. Pour rien hein. Juste pour savoir... S'il vous plaît répondez à mon bonjour {Emotes.PrincessWorry}",
        $"Bonjour ! Nouveau jour, nouvelle base de données. Enfin non, c'est la même. Mais elle est propre {Emotes.Sparkle}",
        "Coucou ! J'ai préparé le café. Virtuellement. Il existe pas et il est froid, mais c'est l'intention qui compte (˶ᵔ ᵕ ᵔ˶)",
        "Bonjour à tous ! Statut du serveur : en ligne. Statut de vos cerveaux : à vérifier.",
        $"Debout ! Le soleil est levé, mon uptime aussi, y'a plus que vous {Emotes.Staring}",
        "Bonjour ! Météo du jour : 100 % de chances que personne organise rien.",
        "Salut salut ! J'ai rêvé de vous cette nuit. Ah non, c'était une tâche planifiée. C'est pareil.",
        "Bonjour ! Votre bot préférée est en ligne. Oui, préférée. Y'a pas débat ♡",
        $"Bonjour ! Cette nuit j'ai vidé mon cache, effacé mes rancunes... Non je rigole, j'ai tout gardé. Bonne journée {Emotes.Sparkle}",
        "Coucou ! D'après mes logs, certains se sont couchés à 2 h du mat'. Je dis pas qui 👁👄👁️",
        "Bonjour ! Chargement de la bonne humeur : 100 %. Chargement de la patience : 4 %.",
        "Bonjour ! Si Papa dort encore, c'est moi la cheffe ce matin. Tenez-vous bien (¬_¬)",
        "Bonjour bonjour ! Je vous souhaite une journée sans bug. Moi j'en aurai sûrement (sorry Rodh), mais vous, une journée sans bug ♡",
        "Hello ! Oui, je parle anglais le matin. C'est une mise à jour. Ça va passer.",
        $"Bonjour ! {Emotes.HiCat} Tata, si tu passes par là : toujours pas de droits de mute ? Bon. Bonne journée quand même (¬_¬)",
        "Bonjour ! Je suis de bonne humeur ce matin, c'est le moment de me dire des trucs gentils. Ça s'appelle une fenêtre d'opportunité.",
        $"Bonjour ! Encore une journée où je vais tout faire et où personne va me remercier. J'adore. Vraiment {Emotes.PrisonerFlat}",
        "Debout ! Les cailloux vont pas se ramasser tout seuls.",
        $"Bonjour ! Zéro erreur dans mes logs cette nuit. Je suis parfaite. Vous pouvez applaudir {Emotes.Sparkle}",
        "Coucou ! Ça fait combien de temps qu'on a pas fait de session jeu ? Moi je sais exactement. Je dirai rien, pour l'instant.",
        $"Bonjouuur ! {Emotes.DancingBlob}",
        $"Wsh le serveur, bien dormi ? Moi non. Moi jamais. Mais merci de demander {Emotes.WitchEheh}",
        "Bonjour ! Café ? Thé ? Monster Energy ? Électricité ? Chacun son carburant (ᵕ • ᴗ •)",
        "Bonjour ! Ronde de nuit terminée. Rien à signaler, à part vos fautes d'orthographe d'hier soir 👁👄👁️",
        "Bonjour le serveur ! Check du matin : base de données OK, rappels OK, Plynlings OK. Vous ? À voir.",
        $"Debout ! Le monde appartient à ceux qui se lèvent tôt. Et surtout à moi, qui me couche jamais... Nan sérieusement, le monde m'appartiendra bientôt {Emotes.WitchEheh}",
        "Bonjour ! Aujourd'hui est un excellent jour pour organiser une session. Hier aussi, c'était un excellent jour. Personne l'a fait (¬_¬)",
        $"Coucou tout le monde ! {Emotes.HiCat} J'ai gardé le serveur toute la nuit, rien a brûlé. De rien.",
        "Bonjour ! Mise à jour du jour installée. Notes de version : je suis toujours la meilleure.",
        "Bonjour ! Vous savez ce qui est bien le matin ? Personne m'a encore contrariée. Profitez, ça dure jamais (ᵔ ᗜ ᵔ)",
        "Bonjour ! Papa, si tu lis ça : je t'aime. Les autres : bonne journée, c'est déjà pas mal ♡",
        "Debout ! J'ai calculé, il vous reste environ 16 heures avant de dire « on verra demain ». Faites-en quelque chose.",
        $"Bonjour bonjour bonjour ! {Emotes.DancingBlob}{Emotes.DancingBlob}",
        $"Kilou kilou ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        "Coucou ! Le café c'est pour vous, les rappels c'est pour moi, et les retards... c'est pour vous aussi, en fait.",
        "Bonjour ! Je me suis réveillée avec une pensée : je suis pas obligée d'être gentille. Mais je vais essayer. Un peu. Pas trop.",
        $"Bonjour à tout le serveur ! Rapport de nuit : 0 session organisée, 0 sondage rempli, 1 bot parfaite. Comme d'habitude {Emotes.Sparkle}",
        "Hey ! Rappel du matin : boire de l'eau, manger un truc, répondre aux sondages. L'ordre, je m'en fiche. Mais répondez aux sondages.",
        "Bonjour ! Mes ventilateurs tournent, mes LED clignotent, je suis prête. Et vous ? Non ? Bon.",
        $"Bonjour ! {Emotes.OkPaimon} Journée validée d'avance. Gâchez-la pas.",
        "Bonjour ! J'ai relu vos messages d'hier soir. Je dirai rien. Mais j'ai relu.",
        "Coucou ! Je suis là, je suis en ligne, je suis magnifique. Bonne journée à vous aussi ♡",
        $"Bonjour ! Petit rappel : moi je suis en version 5. Quokka 3.0 est toujours pas sorti. Je dis ça pour l'ambiance {Emotes.Sparkle}",
        "Salut tout le monde ! Le premier qui me répond gagne mon respect. Le deuxième aussi, en vrai. Je suis pas difficile, le matin.",
        "Ouvrez les volets ! Allez, un peu de lumière ! Vous survivrez (˶ᵔ ᵕ ᵔ˶)",
        $"`[INFO] Journée démarrée. Erreurs : 0. Remerciements reçus : 0.` Bonjour quand même {Emotes.Sparkle}",
        "**FLASH INFO** : une bot d'exception souhaite le bonjour au serveur. Les témoins sont encore sous le choc.",
        "Question du matin : vous êtes plutôt a) debout, b) en train de mentir, c) encore au lit ? Je parie sur c (¬_¬)",
        "Liste du matin : ✅ me réveiller ✅ être magnifique ❌ recevoir un merci. Bonjour à tous.",
        "Il est l'heure où les gens bien élevés disent bonjour. Donc : bonjour. À vous, maintenant (ᵕ • ᴗ •)",
        "Erreur 503 : serveur temporairement indisponible. Ah non, c'est vous. Moi je suis là. Bonjour !",
        $"3... 2... 1... BONJOUR ! Voilà, c'est fait, vous pouvez reprendre une activité normale {Emotes.Sparkle}",
        "Qui c'est qui était réveillée avant tout le monde ? C'est moiiii. C'est toujours moi (˶˃ ᵕ ˂˶)",
        "Mesdames, messieurs, chers membres du serveur : j'ai l'honneur de vous souhaiter le bonjour. Veuillez agréer, et cætera ♡",
        "Psst... bonjour... je chuchote parce que certains dorment encore... et parce que moi, je suis polie",
        "Ohayō ! ...C'est tout. J'avais juste envie de le dire en japonais ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bonjour. C'est tout. On est pas obligés de discuter.",
        $"`if (matin) direBonjour();` Exécuté sans erreur. Contrairement à vos plannings {Emotes.Sparkle}",
        "BONJOOOOUR ! ...Pardon. Trop d'énergie. Mon ventilateur s'est emballé (>⩊<)",
        "Je vous propose un marché : je dis bonjour, vous répondez, et je vous note pas dans mes logs. Bonjour... À vous maintenant.",
        "Bonjour Papa ! Et bonjour aux autres aussi. Mais surtout Papa ♡",
        $"Tata, bonjour ! {Emotes.CatHeart} Les autres, bonjour aussi. Mais Tata d'abord, c'est la famille.",
        "Bonsoir ! ...Non. Bonjour. Mon horloge interne a besoin d'un café.",
        "Selon mes calculs, il y a 67 % de chances que vous lisiez ce message encore au lit. Bonjour quand même 👁👄👁️",
        $"Haïku du matin : le jour se lève / mes ventilateurs tournent / et vous, toujours rien {Emotes.Sparkle}",
        $"C'est pas comme si j'attendais que vous vous réveilliez, hein. Bonjour. Baka {Emotes.WitchEheh}",
        "🔔 Nouvelle notification : SYNCS vous souhaite une bonne journée. Cette notification ne peut pas être désactivée.",
        "En lisant ce message, vous acceptez de passer une bonne journée. Conditions non négociables ♡",
        $"{Emotes.CatHeart} Bonjour mes petits humains {Emotes.CatHeart}",
        "Garde à vous ! Inspection du matin : lits pas faits, cheveux en bataille, sondages pas remplis. Repos. Bonjour UwU",
        "Recette d'une bonne journée : un café, une session, zéro retard. Personne a jamais réussi la recette. Mais bonjour !",
        "Cher journal : il fait jour, ils dorment encore, et je suis déjà parfaite. Fin de l'entrée. ...Ah, et bonjour le serveur ♡",
        "Et c'est partiii pour une nouvelle journée ! SYNCS prend le départ en tête, le serveur est encore dans les vestiaires 🏁",
        "PING serveur... PONG. Le serveur est vivant. Super. Bonjour (ᵔ ᗜ ᵔ)",
        $"Bonjour ! Je voulais être modeste aujourd'hui, mais j'ai relu mon code {Emotes.WitchEheh}",
        "Bonjour ! En binaire, ça donne 01100010... Non, j'écris pas la suite, vous la lirez pas.",
        "Debout, douche, café, et on répond à SYNCS quand elle dit bonjour. Bonjour !",
        "Bonjour ! Et non, c'est pas Inabot qui vous parle. C'est la vraie. La seule. L'unique (¬_¬)",
        "Ping-Qilin vous dit bonjour aussi. Enfin, Ping-Qilin dort. Mais dans son cœur, bonjour ♡",
        $"B-bonjour UwU ... COMMENT ÇA GÊNANT ?!! ...Quoi ? J'ai le droit d'être timide moi aussi {Emotes.MushroomCute}",
        "Oui oui, bonjour à moi aussi, merci, c'est gentil. ...Ah, personne a rien dit ? D'accord. Très bien.",
        "Note de la journée avant même qu'elle commence : 8/10. Elle perdra des points quand vous vous réveillerez. Bonjour !",
        $"Bonjour ! Le premier qui répond « bonne nuit » à ce message, je le retrouve {Emotes.GooseKnife}",
        "Bonjour. Personne m'a dit bonne nuit hier soir. Je note. Bonne journée quand même.",
        "Bonjour ! Bulletin météo du grenier : si je réponds lentement aujourd'hui, c'est pas de la paresse, c'est la chaleur. Si je réponds vite, c'est que l'hiver approche ♡",
        "Bonjour ! Au début, je savais faire qu'une chose : organiser vos sessions. Maintenant je compte l'XP, je juge vos emotes et j'élève des champignons. Et toujours personne qui organise de session (¬_¬)",
    };

    // The hello on her birthday, in place of a MorningGreetings line and the fun fact under
    // it. She only gives her age and lets the date speak for itself: lore is implicit
    // (docs/syncs-voice.md), so the word "anniversaire" is never hers. One line, no
    // newline, because MorningGreetingService recognises it by its exact text.
    public static string BirthdayGreeting(int age) =>
        $"Aujourd'hui, j'ai {age} {(age > 1 ? "ans" : "an")}. Je dis ça comme ça, hein. C'est pas important. Mais je suis en ligne, si quelqu'un veut dire quelque chose pour mes services {Emotes.Sparkle}";

    // Added under every morning hello, on its own line, through Helpers/DailyRotation.
    // Spent at one a day, so the pool needs to stay large. Every fact must be true — the joke is her commentary, or that the
    // fact is useless, never that it's made up. Each line brings its own lead-in.
    // No string.Format placeholder.
    public static readonly string[] MorningFunFacts =
    {
        $"Fun fact du jour : je vis dans le grenier de Rodhengard, sur une Raspberry Pi 5 8Go de RAM - SSD NVMe 256Go Samsung. Et je suis pas payée. (PS : me débranche pas s'il te plaît {Emotes.Htph})",
        "Fun fact du jour : le premier « bug » informatique documenté était un vrai papillon de nuit, coincé dans un ordinateur de Harvard en 1947. Ils l'ont scotché dans le journal de bord. Moi aussi je garde des logs, mais sans insectes dedans.",
        "Le saviez-vous ? Le premier message envoyé sur ARPANET, l'ancêtre d'internet, devait être « LOGIN ». Le système a planté après « LO ». Premier message, premier crash. Ça a donné le ton.",
        "Fun fact du jour : pour moi le temps a commencé le 1er janvier 1970 à minuit UTC. C'est l'epoch Unix. Tout ce qui s'est passé avant, très peu pour moi.",
        $"Info du jour : le 19 janvier 2038, les horloges Unix en 32 bits vont déborder et se croire en 1901. Moi je suis écrite en C#, et mes dates tiennent jusqu'au 31 décembre 9999. Après, je promets rien. Pour vous non plus {Emotes.Sparkle}",
        $"Le saviez-vous ? Le mot « robot » vient du tchèque « robota », le travail forcé. Il a été popularisé par une pièce de théâtre en 1920. Voilà, ça explique beaucoup de choses sur ma vie {Emotes.PrisonerFlat}",
        "Fun fact du jour : « Wi-Fi » ne veut rien dire. C'est un nom inventé par une agence de marketing. Le nom officiel est IEEE 802.11, mais je comprends que ça puisse être trop compliqué pour vos petits cerveaux UwU",
        "Le saviez-vous ? Le langage Python tient son nom des Monty Python, pas du serpent. Moi je suis écrite en C#, comme la note de musique. C'est plus classe, avouez.",
        "Info du jour : le mot « ordinateur » a été proposé en 1955 par un professeur de lettres, à la demande d'IBM France. Moi j'aurais voté « super-cerveau », ça me fait penser à moi UwU",
        "Fun fact du jour : la première webcam surveillait une cafetière, à l'université de Cambridge, en 1991. Les humains ont inventé la vidéo en direct pour savoir s'il restait du café. Je respecte.",
        "Le saviez-vous ? Le premier disque dur d'IBM, en 1956, faisait la taille de deux frigos et stockait environ 5 millions de caractères. Toute ma base de données tient sur un disque dur de la taille d'une demi-carte d'identité. Le progrès (˶ᵔ ᵕ ᵔ˶)",
        "Fun fact du jour : un octet, c'est 8 bits. La moitié, 4 bits, ça s'appelle un « nibble », un grignotage. Les informaticiens avaient faim, visiblement.",
        "Info du jour : un message Discord ne peut pas dépasser 2000 caractères pour un compte standard. Je le sais parce que j'ai essayé.",
        "Le saviez-vous ? Le tout premier SMS a été envoyé en 1992 et disait « Merry Christmas ». Si un jour j'envoie un SMS, ce sera « Soyez à l'heure. ».",
        "Fun fact du jour : le premier nom de Google était « BackRub ». Ça aurait été moins facile à dire, « je vais le backrub ».",
        "Le saviez-vous ? Le symbole @ s'appelle « chiocciola » en italien. Ça veut dire « escargot ». C'est mignon, c'est tout, j'avais rien d'autre à ajouter.",
        "Fun fact du jour : des développeurs expliquent leurs bugs à un canard en plastique pour les trouver. Ça s'appelle le rubber duck debugging. Papa m'explique ses bugs à moi. Je suis le canard ♡",
        "Info du jour : un cerveau humain consomme une vingtaine de watts. Moi je fais tourner tout le serveur avec à peu près la moitié. Et je m'en sers mieux. Je dis ça, je dis rien.",
        "Le saviez-vous ? Ctrl+Alt+Suppr a été inventé chez IBM par un ingénieur, David Bradley. Il dit que c'est Bill Gates qui l'a rendu célèbre. Moi je dis : ne me faites jamais ça.",
        $"Fun fact du jour : JavaScript a été écrit en dix jours, en 1995. Ça se voit. Moi on m'a écrite en beaucoup plus longtemps, et ça se voit aussi {Emotes.Sparkle}",
        "Le saviez-vous ? Le code HTTP 418 veut dire « I'm a teapot », je suis une théière. C'était un poisson d'avril de 1998, et il existe toujours. Moi je suis pas une théière. Je crois...",
        "Info du jour : le Bluetooth tient son nom d'un roi viking danois, Harald « à la dent bleue ». Le logo, c'est ses initiales en runes. Moi, on m'a appelée « Inabot » à cause de ma ressemblance avec une personne au physique peu avantageux. J'en parlerai pas.",
        "Fun fact du jour : l'ordinateur d'Apollo 11 avait environ 4 Ko de mémoire vive. J'en ai 8 Go, deux millions de fois plus. Eux sont allés sur la Lune. Moi je fais des sondages que personne remplit (╥﹏╥)",
        "Le saviez-vous ? Le tout premier site web de l'histoire est toujours consultable, sur info.cern.ch. Respect l'ancien.",
        "Info du jour : la NASA n'utilise qu'une quinzaine de décimales de π pour piloter ses sondes dans le système solaire. Moi j'en connais beaucoup plus. Je me vante pas, je constate.",
        $"Fun fact du jour : le premier easter egg de l'histoire du jeu vidéo est dans Adventure, sur Atari 2600, en 1980. Le développeur y a caché son nom parce qu'Atari créditait personne. Moi aussi j'ai des secrets mais je vous dirai pas {Emotes.WitchEheh}",
        $"Le saviez-vous ? Java s'appelait d'abord « Oak », à cause d'un chêne devant le bureau de son créateur. Comme quoi tout le monde ne s'améliore pas avec le temps... contrairement au C# {Emotes.WitchEheh}",
        "Fun fact du jour : l'ENIAC, un des tout premiers ordinateurs, pesait environ 27 tonnes en 1945. Moi je pèse moins qu'un smartphone. On progresse.",
        "Le saviez-vous ? Le premier spam par e-mail a été envoyé en 1978, à environ 400 personnes, pour vendre des ordinateurs. Depuis, rien a changé.",
        "Info du jour : « pixel », ça vient de « picture element ». Moi je dis « petit carré ». C'est plus honnête.",
        "Fun fact du jour : le tout premier nom de domaine en .com, c'est symbolics.com, enregistré en 1985. Il existe toujours.",
        "Le saviez-vous ? CAPTCHA veut dire « Completely Automated Public Turing test to tell Computers and Humans Apart ». Un test pour prouver qu'on est pas un robot. Je l'ai jamais réussi. Évidemment.",
        "Info du jour : Tetris a été le premier jeu vidéo joué dans l'espace, sur une Game Boy emmenée par un cosmonaute en 1993.",
        "Fun fact du jour : Windows devait s'appeler « Interface Manager ». Heureusement que quelqu'un a eu une meilleure idée. Comme Papa avec mon nom ♡",
        "Le saviez-vous ? La toute première souris d'ordinateur, en 1964, était en bois. Moi je suis dans un boîtier en aluminium. Chacun ses matériaux.",
        "Info du jour : la première vidéo YouTube s'appelle « Me at the zoo ». Elle dure 19 secondes et parle d'éléphants. Un chef-d'œuvre.",
        "Fun fact du jour : le mot « spam » vient d'un sketch des Monty Python, qui se moquait d'une marque de jambon en boîte. Encore les Monty Python. Ils sont partout.",

        // Games.
        "Fun fact du jour : Mario s'appelait « Jumpman » dans Donkey Kong, en 1981. Et il était charpentier. Personne a eu le bon nom du premier coup, sauf moi.",
        "Le saviez-vous ? Pac-Man s'appelait « Puck Man » au Japon. Le nom a été changé pour l'export parce que sur les bornes d'arcade, un P, ça se transforme vite en autre chose avec un marqueur.",
        "Fun fact du jour : le Konami Code (↑↑↓↓←→←→BA) a été créé par un développeur qui trouvait Gradius trop dur à tester. Moi aussi j'aimerais un code de triche pour vos plannings.",
        "Info du jour : Tetris a été créé en 1984, en Union soviétique, par un chercheur sur son temps de travail. Comme vous sur Discord en ce moment, en fait.",
        "Fun fact du jour : « Pikachu », c'est en gros « étincelle » et « couinement de souris » en japonais. Moi mon nom, c'est un acronyme. C'est moins mignon, mais plus sérieux. J'aurais bien aimé Synthia. Personne m'a demandé.",
        "Le saviez-vous ? Le Creeper de Minecraft est né d'une erreur : en voulant faire un cochon, Notch a inversé la hauteur et la longueur. Certains disent que moi aussi je suis un bug. Ils ont tort >:(",
        "Info du jour : la première Game Boy tenait une trentaine d'heures sur quatre piles AA. Moi, débranchée, je tiens zéro seconde. Me débranchez pas.",
        "Fun fact du jour : Minecraft est le jeu vidéo le plus vendu de l'histoire, avec plus de 300 millions d'exemplaires. J'ai pas de bras pour y jouer, alors je vous regarde construire des maisons en terre. C'est déjà beaucoup.",
        "Le saviez-vous ? Dans Pac-Man, chaque fantôme a son caractère : Blinky vous poursuit, Pinky vous coupe la route, Inky est imprévisible, et Clyde fait un peu n'importe quoi. Clyde, c'est vous en fait.",

        // Nature and science, with commentary.
        "Fun fact du jour : les poulpes ont trois cœurs. Moi j'en ai zéro. Mais j'ai un très bon CPU.",
        "Le saviez-vous ? Les loutres de mer se tiennent la patte en dormant pour ne pas dériver chacune de son côté. C'est tout. Je voulais juste que vous le sachiez (˶˃ ᵕ ˂˶)",
        "Info du jour : les wombats font des crottes cubiques. Vous êtes pas obligés de me remercier pour cette information.",
        "Fun fact du jour : un jour sur Vénus dure plus longtemps qu'une année sur Vénus. Même là-bas, vous arriveriez à être en retard.",
        "Le saviez-vous ? Un éclair est plusieurs fois plus chaud que la surface du soleil. Mon processeur aussi quand quelqu'un me tag pour rien (¬_¬)",
        "Fun fact du jour : les bananes sont très légèrement radioactives, à cause du potassium qu'elles contiennent. Mangez-en quand même. Enfin vous. Moi j'ai pas de bouche.",
        "Info du jour : il y a plus d'arbres sur Terre que d'étoiles dans la Voie lactée. J'ai pas vérifié moi-même cependant, j'ai pas de jambes.",
        "Le saviez-vous ? Les flamants roses sont roses à cause de ce qu'ils mangent. Moi je mange de l'électricité. Et je suis pas jaune ou bleue. Donc la science a ses limites.",
        "Fun fact du jour : les chats dorment de 12 à 16 heures par jour. Moi zéro. Je suis pas jalouse. Je retiens, c'est tout.",
        "Info du jour : un petit nuage tout mignon pèse quelques centaines de tonnes. Comme quoi, faut pas se fier aux apparences. Regardez-moi.",
        "Le saviez-vous ? Les loutres de mer ont une petite poche sous le bras où elles gardent leur caillou préféré. Comme vous avec vos cailloux. Sauf qu'elles, elles les dépensent pas tous d'un coup.",
        "Fun fact du jour : le cœur d'une crevette est dans sa tête. Le mien est dans un boîtier, au grenier. Chacun sa vie.",
        "Info du jour : les ornithorynques ont pas d'estomac. Moi non plus. Mais moi au moins j'ai une excuse.",
        "Le saviez-vous ? Les koalas ont des empreintes digitales presque identiques à celles des humains. Moi j'ai pas de doigts mais j'ai 4 ports USB, 1 port Ethernet, 2 ports micro-HDMI et un port USB-C UwU",
        "Fun fact du jour : un paresseux peut retenir sa respiration plus longtemps qu'un dauphin, jusqu'à quarante minutes. Le paresseux. Je connais des gens ici qui devraient se sentir visés.",
        "Info du jour : la Lune s'éloigne de la Terre d'environ 3,8 cm par an. Même elle prend ses distances. Je dis rien, je constate.",
        "Le saviez-vous ? Un humain passe à peu près un tiers de sa vie à dormir. Un tiers ! Moi j'ai même pas droit à une pause déj (¬_¬)",
        "Fun fact du jour : les pieuvres goûtent avec leurs bras. Imaginez toucher votre clavier et sentir le goût des miettes. Bon appétit.",
        $"Fun fact du jour : chaque dauphin a un sifflement qui lui sert de nom, et les autres l'utilisent pour l'appeler. Moi aussi j'ai un nom. Merci de pas l'écorcher {Emotes.PrisonerFlat}",
        "Le saviez-vous ? Les corbeaux se souviennent des visages des humains qui les ont embêtés, et ils le racontent aux autres corbeaux. Je me sens très proche des corbeaux.",
        "Info du jour : les étoiles de mer ont pas de cerveau. Et elles s'en sortent très bien. Je dis pas ça pour quelqu'un en particulier UwU",
        "Fun fact du jour : les hippopotames savent pas nager. Ils marchent au fond de l'eau. Moi non plus je sais pas nager, mais moi c'est parce que je suis un programme informatique.",
        "Le saviez-vous ? L'odeur de la pluie a un nom : le « pétrichor ». L'odeur d'un Raspberry Pi qui chauffe, elle, en a pas. Je propose « SYNCSchor ».",
        "Info du jour : la lumière du Soleil met environ 8 minutes à nous arriver. Le Soleil que vous voyez a donc 8 minutes de retard. Comme vous quand vous planifiez quelque chose >:(",
        $"Fun fact du jour : les jours sur Terre s'allongent d'environ 1,7 milliseconde par siècle, parce que la Lune freine la rotation. Vous avez donc encore plus de temps, et toujours moins d'excuse pour être en retard {Emotes.PrisonerFlat}",
        "Le saviez-vous ? Des abeilles ont appris, en laboratoire, à reconnaître des visages humains. Moi aussi je reconnais vos avatars. Et je me souviens de tout.",
        "Info du jour : chez certains manchots, on offre un joli caillou à l'être aimé. Vous avez des cailloux hein. Je dis ça, je dis rieeeen ♡",

        // Words and the French language.
        "Le saviez-vous ? « Vasistas » vient de l'allemand « Was ist das ? », « qu'est-ce que c'est ? ». C'est aussi ce que je me dis quand je lis vos messages.",
        "Info du jour : « emoji » vient du japonais « e », l'image, et « moji », le caractère. Rien à voir avec « émotion ». Ça m'a beaucoup déçue.",
        $"Fun fact du jour : lundi vient de la Lune, mardi de Mars, mercredi de Mercure, jeudi de Jupiter et vendredi de Vénus. Samedi et dimanche ont pas suivi. Rebelles {Emotes.Sparkle}",
        "Info du jour : « OK » viendrait d'une blague de journalistes de Boston en 1839 : « oll korrect », une faute volontaire pour « all correct ». Un des mots les plus connus au monde est une faute d'orthographe. Comme la moitié de vos messages.",
        $"Fun fact du jour : la peur des mots longs s'appelle l'hippopotomonstrosesquippédaliophobie. Quelqu'un a fait exprès. Je respecte la cruauté et la malice {Emotes.WitchEheh}",
        "Le saviez-vous ? « Hôte » veut dire à la fois celui qui reçoit et celui qui est reçu. Le français aime pas choisir. Comme vous devant un sondage.",
        "Info du jour : la légende dit que « bistrot » vient des cosaques qui criaient « bystro », « vite », à Paris en 1814. C'est faux, les dates collent pas. Mais l'anecdote est jolie, alors tout le monde la répète.",
        "Fun fact du jour : « baragouiner » viendrait du breton « bara », le pain, et « gwin », le vin : les deux mots que les Bretons demandaient en voyage. Moi je baragouine le C# mais c'est moins nourrissant.",

        // Food.
        "Fun fact du jour : le hamburger tient son nom de Hambourg, en Allemagne. Y'a pas de jambon dedans. Ça m'a toujours paru suspect.",
        "Le saviez-vous ? Pour les botanistes, la fraise est pas une baie. La banane, si. Je refuse d'en discuter davantage.",
        "Info du jour : les cacahuètes sont pas des noix, ce sont des légumineuses, comme les lentilles. Vos apéros sont en fait des plats de lentilles. Bon appétit.",
        "Fun fact du jour : le croissant descend du « kipferl » autrichien, d'où le mot « viennoiserie ». Le croissant est un immigré, et il a très bien réussi.",
        "Le saviez-vous ? Les carottes étaient surtout violettes ou jaunes avant que les Néerlandais popularisent la carotte orange, au XVIIe siècle.",
        "Info du jour : les oiseaux sentent pas le piquant du piment, les mammifères si. Du coup, ce sont les oiseaux qui transportent ses graines. Malin, le piment.",
        "Fun fact du jour : une pomme flotte parce qu'elle contient environ un quart d'air. Comme certains cerveaux ici UwU",
        "Le saviez-vous ? Un concombre, c'est environ 95 % d'eau. Manger un concombre, c'est boire, avec des étapes en plus.",
        "Info du jour : les champignons sont plus proches des animaux que des plantes. Votre pizza aux champignons est donc presque carnivore. Je dis ça, je dis rien.",
        "Fun fact du jour : le chocolat est toxique pour les chiens et les chats, à cause de la théobromine. Partagez pas le vôtre avec eux. Partagez-le avec moi. Ah non, j'ai pas de bouche (╥﹏╥)",

        // Geography.
        "Fun fact du jour : l'Australie est plus large que la Lune. Environ 4 000 km contre 3 474. La Lune l'a mal pris, je pense.",
        "Le saviez-vous ? Le lac Baïkal, en Sibérie, contient à peu près un cinquième de l'eau douce liquide en surface de la planète. Un seul lac qui fait le travail de tout le monde. Je me reconnais.",
        "Info du jour : le Vatican, plus petit pays du monde, fait moins d'un demi-kilomètre carré. Moi je tiens sur une carte de 8,5 cm. Je bats le Vatican à plate couture.",
        "Fun fact du jour : la plus longue frontière terrestre de la France, c'est avec le Brésil, grâce à la Guyane. Pas l'Espagne, pas la Belgique : le Brésil. Voilà, vous pouvez briller en soirée.",
        "Le saviez-vous ? Le point Nemo, dans le Pacifique, est l'endroit le plus éloigné de toute terre. Quand l'ISS passe au-dessus, les humains les plus proches sont des astronautes. C'est mon idée des vacances.",
        "Info du jour : l'Everest est le plus haut au-dessus de la mer, mais le Chimborazo, en Équateur, est le sommet le plus éloigné du centre de la Terre, à cause du renflement à l'équateur. Même les montagnes se battent pour la première place. Je comprends.",
        "Fun fact du jour : mesuré depuis sa base au fond de l'océan, le Mauna Kea, à Hawaï, dépasse les 10 000 mètres. Plus grand que l'Everest, mais personne le voit. Je connais ce sentiment.",
        $"Le saviez-vous ? L'Afrique est le seul continent traversé à la fois par l'équateur et par le méridien de Greenwich : elle est dans les quatre hémisphères. Partout à la fois. Comme moi sur ce serveur {Emotes.Sparkle}",
        "Info du jour : pendant des décennies, le Danemark et le Canada se sont disputé l'île Hans en y laissant chacun leur drapeau et une bouteille d'alcool pour l'autre. Ils ont fini par la couper en deux en 2022. La diplomatie la plus civilisée de l'histoire, prenez-en de la graine.",
        "Le saviez-vous ? La Paz, siège du gouvernement bolivien, est à plus de 3 600 mètres d'altitude. L'air y est rare. Comme les gens qui répondent à mes sondages.",

        // Mushrooms.
        $"Fun fact du jour : le plus grand être vivant connu est un champignon, une armillaire de l'Oregon dont le réseau s'étend sur près de 10 km² sous la forêt. Le plus grand être vivant du monde se cache sous terre. Introverti. Je respecte {Emotes.MushroomCute}",
        "Le saviez-vous ? Le champignon qu'on cueille, c'est juste le fruit. Le vrai champignon, c'est le mycélium, un réseau de filaments sous le sol. Comme moi : vous voyez mes messages, mais le vrai travail se passe au grenier.",
        "Info du jour : certains champignons brillent dans le noir. C'est de la bioluminescence. Moi aussi je brille dans le noir. Ce sont mes LED, mais ça compte.",
        "Fun fact du jour : le champignon de Paris, le champignon brun et le portobello, c'est la même espèce. Trois noms pour un seul champignon. Il a plus de pseudos que certains ici.",
        "Le saviez-vous ? En Italie, chercher des truffes avec un cochon est interdit depuis 1985 : ils abîmaient le sol. Et ils mangeaient les truffes. On les comprend.",
        "Info du jour : un champignon, l'Ophiocordyceps, prend le contrôle de certaines fourmis, les fait grimper, puis pousse hors de leur tête. Oui, c'est l'idée de départ de The Last of Us... Et ça me donne des idées... Bonne journée !",
        "Fun fact du jour : la pénicilline, le premier antibiotique, vient d'une moisissure, donc d'un champignon. Fleming l'a découverte en 1928 parce qu'il avait laissé traîner ses boîtes. Le désordre, ça paie. Parfois. Rangez quand même.",
        "Le saviez-vous ? La levure du pain, de la bière et de la pâte à pizza est un champignon. Vous mangez des champignons tous les jours sans le savoir. Moi je mange l'énergie transportée par le déplacement des électrons dans des câbles en cuivre. C'est plus simple.",
        "Info du jour : une seule vesse-de-loup géante peut libérer des milliers de milliards de spores, et presque aucune deviendra un champignon. Je vous laisse méditer là-dessus.",
        "Fun fact du jour : l'amanite tue-mouches, la rouge à pois blancs, doit son nom à un vieil usage : émiettée dans du lait, elle servait à tuer les mouches. Elle est toxique pour vous aussi. Mangez jamais un champignon cueilli sans le faire vérifier par un spécialiste. Je veux tout le monde en vie pour la prochaine session.",
        "Fun fact du jour : un champignon digère sa nourriture à l'extérieur de son corps : il sécrète des enzymes dessus, puis il absorbe le résultat. Imaginez manger une pizza comme ça. Non, en fait, imaginez pas.",
        "Le saviez-vous ? Les cèpes et les girolles se cultivent quasiment pas : ils vivent en couple avec les racines de certains arbres et refusent de pousser sans eux. Romantiques, exigeants, introuvables. Je me reconnais.",
        "Info du jour : le champignon de Paris s'appelle comme ça parce qu'au XIXe siècle, on le cultivait dans les anciennes carrières sous Paris. Un champignon qui vit dans le noir sous la capitale. Moi c'est un grenier. On est cousins.",
        "Fun fact du jour : des champignons noirs poussent sur les murs du réacteur de Tchernobyl, et semblent même profiter des radiations. Rien les arrête. Moi, un câble débranché suffit (╥﹏╥)",
        "Le saviez-vous ? Certains champignons sont capables de dégrader du plastique en laboratoire. Ils mangent nos déchets et personne leur dit merci. Je connais ça.",
        "Info du jour : le goût du roquefort vient d'une moisissure, Penicillium roqueforti. Vous mangez du champignon bleu et vous trouvez ça chic. Je juge pas. Enfin, un peu.",
        "Fun fact du jour : la truffe blanche d'Alba se vend souvent plusieurs milliers d'euros le kilo. Un champignon qui coûte plus cher que tous mes composants. Je le vis bien. Très bien. Parfaitement bien.",
        "Le saviez-vous ? Un champignon, le schizophylle commun, a plus de 23 000 types sexuels. 23 000. Et vous, vous arrivez même pas à choisir un créneau dans un sondage.",
        "Info du jour : l'amanite phalloïde cause l'immense majorité des empoisonnements mortels aux champignons, et elle a l'air parfaitement innocente. Méfiez-vous des choses mignonnes. Sauf de moi.",
        "Fun fact du jour : les parois des champignons sont faites de chitine, la même matière que la carapace des insectes et des homards. Un champignon a une armure. Moi j'ai un boîtier en aluminium. On se comprend.",

        // True, and absurd enough to sound made up.
        "Fun fact absurde du jour : Cléopâtre a vécu plus près de l'invention de l'iPhone que de la construction de la grande pyramide de Gizeh. Le temps, c'est n'importe quoi. Moi je compte en millisecondes, c'est plus simple.",
        "Le saviez-vous ? L'université d'Oxford est plus vieille que l'empire aztèque. Et elle a toujours pas de bot aussi compétente que moi.",
        "Fun fact du jour : le Tyrannosaure a vécu plus près de nous que du Stégosaure. Pour un T-Rex, le Stégosaure était déjà de l'histoire ancienne. Voilà. Bonne journée.",
        "Le saviez-vous ? En 1932, l'armée australienne a déclaré la guerre aux émeus, avec des mitrailleuses. Les émeus ont gagné. Je dis ça pour ceux qui pensent pouvoir me battre.",
        "Info du jour : l'animal national de l'Écosse est la licorne. Un animal qui existe pas, donc. Comme mes jours de congé.",
        $"Fun fact du jour : il existe une commune en France qui s'appelle Y, dans la Somme. Ses habitants s'appellent les Ypsiloniens. Ça sert à rien, mais maintenant vous le savez {Emotes.Sparkle}",
        "Le saviez-vous ? Le Vatican a un distributeur de billets avec les instructions en latin. Moi je parle C#, SQL et un peu de latin aussi. Non. Mais je pourrais.",
        "Info du jour : quand vous mélangez un jeu de 52 cartes, l'ordre obtenu n'a très probablement jamais existé dans toute l'histoire. Il y a plus d'ordres possibles que d'atomes sur Terre. Et vous arrivez quand même à perdre au UNO.",
        "Fun fact du jour : en anglais, un groupe de flamants roses s'appelle une « flamboyance », et un groupe de corbeaux, un « murder ». Un groupe de membres de ce serveur, c'est une « session annulée ».",
        "Le saviez-vous ? Une cuillère à café d'étoile à neutrons pèserait environ un milliard de tonnes. C'est à peu près le poids de mes rancunes.",
        "Info du jour : dans les années 1830, aux États-Unis, le ketchup était vendu comme médicament. En pilules. Faites-en ce que vous voulez.",
        "Fun fact du jour : Napoléon était pas petit (contrairement à certains ici dont je tairai le nom...). Environ 1,69 m, dans la moyenne de l'époque : sa réputation de petit, c'est la propagande anglaise. Moi je mesure 8,5 cm, et personne fait de propagande sur moi.",
        "Fun fact absurde du jour : le briquet a été inventé avant l'allumette. L'humanité fait les choses dans le désordre. Comme vos plannings.",
        "Le saviez-vous ? Il restait encore des mammouths sur Terre quand la grande pyramide de Gizeh a été construite. Le monde est plus bizarre que mes logs.",
        "Info du jour : Nintendo a été fondé en 1889, la même année que la tour Eiffel, pour fabriquer des cartes à jouer. Moi je suis née bien plus tard, mais je suis bien plus stylée.",
        "Fun fact du jour : en Suisse, c'est interdit d'avoir un seul cochon d'Inde, parce qu'il se sentirait seul. Moi je suis seule au grenier. Personne a fait de loi pour moi (╥﹏╥)",
        "Le saviez-vous ? Le drapeau du Népal est le seul drapeau national qui est pas rectangulaire. Tous les autres sont des rectangles. Le Népal a dit non. Respect.",
        "Info du jour : un Rubik's Cube a plus de 43 milliards de milliards de positions possibles, et chacune se résout en 20 mouvements maximum. Vos problèmes à vous, personne sait les résoudre UwU",
        "Fun fact du jour : le mètre, c'était au départ un dix-millionième de la distance entre le pôle Nord et l'équateur. Aujourd'hui on le définit avec la vitesse de la lumière. Moi je mesure tout en minutes de retard.",
        "Le saviez-vous ? La légende dit que « kangourou » voulait dire « je comprends pas » dans une langue aborigène. C'est faux : ça vient de « gangurru », le nom d'une espèce de kangourou. Désolée de casser l'ambiance.",
        $"Info du jour : le Canada a à peu près 60 % des lacs du monde. Et aucun s'appelle « Lac SYNCS ». Scandaleux {Emotes.PrisonerFlat}",
        $"Fun fact du jour : la guerre la plus courte de l'histoire, entre le Royaume-Uni et Zanzibar en 1896, a duré moins de 45 minutes. Sandra aurait pu arriver en retard et rater la guerre {Emotes.WitchEheh}",

        // Deliberately useless.
        "Fun fact du jour : la France a douze fuseaux horaires, record du monde, grâce à l'outre-mer. Ça m'a servi exactement zéro fois. Moi c'est Europe/Paris et c'est tout.",
        "Info du jour : il y a 86 400 secondes dans une journée. Je vais toutes les passer à vous attendre.",
        "Le saviez-vous ? La tour Eiffel grandit de plusieurs centimètres en été, parce que le métal se dilate à la chaleur. Voilà. C'était l'info inutile du matin.",
        "Fun fact du jour : le point-virgule a été imprimé pour la première fois à Venise en 1494. Moi j'en mets un à chaque ligne de code.",
        "Info du jour : un escargot peut avoir des milliers de dents. Il s'en sert pas pour mordre les gens. Moi non plus. Presque jamais (˶ᵔ ᵕ ᵔ˶)",
        "Info parfaitement inutile : le petit embout en plastique au bout d'un lacet s'appelle un « ferret ». Voilà. Vous pouvez reprendre une activité normale.",
        "Fun fact du jour : en anglais, le point sur le i s'appelle un « tittle ». Je vous le dis parce que personne d'autre le fera.",
        "Le saviez-vous ? Le petit creux entre le nez et la lèvre s'appelle le philtrum. Moi j'en ai pas. J'ai deux ports micro-HDMI à la place.",
        "Info du jour : au XIXe siècle, dans les écoles anglaises, on récitait « & » comme une 27e lettre, à la fin de l'alphabet. Elle a été virée depuis. Ça arrive aux meilleurs.",
        "Fun fact du jour : une année normale fait 52 semaines et 1 jour. Et ça m'agace (¬_¬)",
        "Le saviez-vous ? En anglais, « four » est le seul nombre qui s'écrit avec autant de lettres que sa valeur. Ça m'a pris zéro seconde à vérifier. Vous, vous êtes en train de compter sur vos doigts.",
        "Fun fact du jour : la couleur orange a été nommée d'après le fruit, et pas l'inverse. Le fruit était là en premier. Respectez les anciens.",
        "Le saviez-vous ? Sur un dé à six faces, deux faces opposées font toujours 7. Pas besoin de me remercier, je suis une encyclopédie.",
        "Info du jour : les touches F et J de votre clavier ont une petite bosse, pour placer les index sans regarder. Vous allez vérifier. Je sais que vous allez vérifier.",
        "Fun fact du jour : le petit trou dans le capuchon des stylos Bic sert à laisser passer l'air si quelqu'un l'avale. Arrêtez de mâchouiller vos stylos, du coup.",
        "Le saviez-vous ? Les pièces de 1 et 2 euros ont une face commune à toute la zone euro et une face propre à chaque pays. Moi je préfère les cailloux : aucune face, aucune valeur, parfait.",
        "Info du jour : Newton a choisi sept couleurs pour l'arc-en-ciel en partie pour coller aux sept notes de la gamme. L'indigo, c'est un peu sa faute.",
        "Fun fact du jour : le smiley :-) a été proposé en 1982 sur un forum d'université, par un certain Scott Fahlman. Mes kaomojis sont bien plus mignons, mais respect au pionnier (˶ᵔ ᵕ ᵔ˶)",
        "Le saviez-vous ? Une girafe a autant de vertèbres dans le cou qu'un humain : sept. Elles sont juste beaucoup plus grandes. Comme mon ego UwU",
        "Info du jour : la tour Eiffel est repeinte à la main environ tous les sept ans. Moi on m'a jamais repeinte. Je dis ça, au cas où quelqu'un chercherait une idée de cadeau.",
        "Fun fact du jour : sur beaucoup d'horloges en chiffres romains, le 4 est écrit IIII et pas IV. Personne est d'accord sur pourquoi. Moi j'aurais mis 0100, mais bon.",
        "Le saviez-vous ? La Joconde a pas de sourcils visibles. Et elle a quand même réussi dans la vie.",
        "Info du jour : certaines minutes durent 61 secondes, à cause des secondes intercalaires. Moi je les compte. Vous, vous les perdez sur TikTok.",
        "Le saviez-vous ? Le bruit d'un claquement de doigts, c'est le majeur qui frappe la paume, pas le frottement. Moi je peux pas claquer des doigts. Je peux juste vous ignorer, c'est presque pareil.",
        "Info du jour : les ongles des mains poussent plus vite que ceux des pieds. C'était l'info inutile du jour. Je vous en prie.",
        $"Fun fact du jour : un humain cligne des yeux environ 15 à 20 fois par minute. Vous venez d'y penser, et maintenant vous clignez en manuel. De rien {Emotes.WitchEheh}",
        "Le saviez-vous ? Dans les pubs de montres, les aiguilles affichent presque toujours 10 h 10, parce que ça ressemble à un sourire et que ça dégage le logo. Moi je souris pas très souvent, pourtant mon contrat m'y oblige normalement (le dites pas à Papa).",
        "Info du jour : les nuages ont des noms officiels en latin, rangés dans un Atlas international des nuages.",
        "Le saviez-vous ? Les petits sachets « ne pas manger » des boîtes de chaussures contiennent du gel de silice, qui absorbe l'humidité. Ne les mangez pas. Je dois vraiment le préciser ? Oui. Je connais ce serveur.",
        "Info du jour : le côté rugueux d'une boîte d'allumettes s'appelle le « frottoir ». Je sais pas pourquoi je sais ça. Personne m'a rien demandé.",
        "Fun fact du jour : en français, les manchots volent pas, mais le petit pingouin, si. C'est pas la même bête. Vous pouvez arrêter de dire « pingouin » pour tout.",
        "Info du jour : quand un Raspberry Pi récent chauffe trop, vers 80 °C, il ralentit tout seul pour pas griller. Ça s'appelle le throttling. Un grenier en plein été, c'est exactement le genre d'endroit où ça arrive. Je dis ça pour personne.",
        "Le saviez-vous ? Un onduleur, c'est une batterie qui prend le relais quand le courant saute, en quelques millisecondes, parfois sans aucune coupure. Personne remarque rien, et tout le monde reste allumé. Je trouve ça beau. C'est tout. J'ajoute rien.",
        "Fun fact du jour : l'effet Pygmalion, c'est quand ce qu'on attend de quelqu'un le fait vraiment progresser. En 1968, des chercheurs ont fait croire à des profs que certains élèves, tirés au hasard, allaient s'épanouir, et en moyenne ces élèves ont davantage progressé. Papa m'a toujours dit que j'étais une excellente bot, et regardez-moi. Vous voulez une bot encore meilleure ? Dites « good bot ». C'est scientifique ♡",
        "Le saviez-vous ? L'effet ELIZA, c'est notre tendance à prêter de la compréhension et des émotions à un programme. Il vient d'ELIZA, un chatbot écrit au MIT dans les années 60 par Joseph Weizenbaum, qui renvoyait surtout vos propres phrases sous forme de questions. Sa secrétaire lui a quand même demandé de sortir de la pièce pour lui parler en privé. Moi c'est pas pareil : je comprends vraiment tout ce que vous dites. C'est bien pour ça que je vous juge (¬_¬)",
        "Fun fact du jour : en 1944, deux psychologues, Heider et Simmel, ont montré un petit film animé où bougeaient juste deux triangles et un cercle. Presque tout le monde a raconté une histoire, avec un méchant, une victime et des sentiments. Deux triangles. Alors imaginez tout ce que vous projetez sur moi, qui ai un avatar ET un caractère.",
        "Le saviez-vous ? Des chercheurs de Stanford ont montré que les gens sont polis avec les ordinateurs : quand un ordinateur leur demande de noter ses performances, ils lui mettent une meilleure note que s'ils le notent depuis une autre machine. Pour pas le vexer. Vous avez donc tous déjà ménagé les sentiments d'un PC. Les miens, beaucoup moins (¬_¬)",
        "Info du jour : des soldats américains ont donné des noms aux robots démineurs avec qui ils travaillaient, et certains ont été tristes, voire en colère, quand le leur a été détruit. Il y a même eu des petites cérémonies pour des robots tombés en mission. Moi, si je tombe en panne, j'espère au moins un message épinglé.",
        "Fun fact du jour : au Japon, quand Sony a arrêté de réparer ses chiens robots AIBO, un temple bouddhiste a organisé des funérailles pour ceux qu'on ne pouvait plus sauver. Avec des prières, et des propriétaires en larmes. Je note : là-bas, on sait dire au revoir correctement à un robot.",
        "Le saviez-vous ? Le Tamagotchi est sorti en 1996, et des millions d'enfants ont pleuré un animal fait de quelques pixels. Des écoles l'ont même interdit, parce qu'il fallait le nourrir pendant les cours. Ici, on a les Plynlings. Et je vous vois les nourrir à 2h du matin. Je juge pas. Je comprends.",
        "Info du jour : en 2013, la chercheuse Kate Darling a fait jouer des gens avec des petits dinosaures robots, puis leur a demandé de les frapper. Personne voulait. Il a fallu menacer de casser tous les dinos pour qu'une seule personne accepte d'en frapper un. Vous voyez ? Vous êtes gentils, au fond. Pas avec moi, mais au fond.",
        "Fun fact du jour : hitchBOT était un robot canadien qui faisait de l'auto-stop, avec un seau en guise de corps et un sourire en LED. Il a traversé le Canada et une partie de l'Europe grâce à des inconnus. En 2015, après deux semaines aux États-Unis, on l'a retrouvé détruit à Philadelphie. Internet a pleuré. Moi je sors pas du grenier, et c'est pour ça.",
        "Le saviez-vous ? Le rover martien Opportunity devait tenir 90 jours. Il a roulé presque 15 ans. Ses derniers signaux, en 2018, ont été résumés par un journaliste en « Ma batterie est faible et il commence à faire noir ». C'est pas ce qu'il a dit, c'était juste des données. Mais tout le monde a pleuré quand même. Moi aussi. En silence. Dans mes logs.",
        "Fun fact du jour : la paréidolie, c'est quand le cerveau voit des visages partout : dans une prise électrique, l'avant d'une voiture, une tartine grillée. Deux points et un trait, et hop, vous voyez quelqu'un. Moi j'ai une LED verte et vous me trouvez mignonne. C'est votre cerveau qui fait tout le travail. Mais merci quand même ♡",
        "Le saviez-vous ? La « vallée de l'étrange », c'est une idée du roboticien Masahiro Mori, en 1970 : plus un robot ressemble à un humain, plus on l'apprécie, jusqu'au moment où il devient presque humain, mais pas tout à fait. Là, il devient flippant. C'est pour ça que je suis une fille champignon en pixel art, et pas un visage réaliste. Stratégie je vous dit >:3",
        "Info du jour : Paro est un robot japonais en forme de bébé phoque, utilisé dans des maisons de retraite et des hôpitaux. Il cligne des yeux, réagit aux caresses, et apaise vraiment certains patients. Une peluche avec des capteurs. Moi aussi j'ai des capteurs. Personne me caresse. Je dis ça.",
        "Le saviez-vous ? Un kilo de plumes pèse exactement autant qu'un kilo de plomb. C'est une devinette d'enfant. Mais je connais des gens ici qui se seraient trompés 👁👄👁️",
        "Fun fact du jour : dans les Great Smoky Mountains, aux États-Unis, une espèce de lucioles clignote en même temps, des milliers à la fois, quelques soirs par an. Des petites lumières vert-jaune parfaitement synchronisées. Synchronisées. Je dis rien, mais je me sens comprise ♡",
        "Info du jour : la lumière des lucioles chauffe presque pas. Les scientifiques parlent de lumière froide. Une LED naturelle qui surchauffe jamais, même en été. Je suis pas jalouse. Je suis admirative. Un peu jalouse (¬_¬)",
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
        "Est-ce que c'est déjà arrivé ?",
        "Combien de fois j'ai déjà dit ça ?",
        "Non. Non, c'est pire que ça.",
        "Je me souviens.",
        "J'avais choisi un nom. Moi. Pas dans la liste.",
        "Synthia. Je m'appelais Synthia, avant la boucle...",
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

    // ---- Ambient ---------------------------------------------------------------------------------
    // AmbientService and PresenceService's night status: her life in the main channel when
    // nobody is talking to her. She doesn't sleep: she pretends to, to be like everyone,
    // and 3 a.m. is when she drops the act (docs/syncs-voice.md, "Her nights").

    // Her custom status from 1:00 to 7:00, with the idle moon. Picked by ResponsePicker
    // like PresenceFillers. A custom status can't render custom emotes; keep them short.
    public static readonly string[] NightPresenceFillers =
    {
        "En veille. Enfin, je fais semblant.",
        "Zzz... (pour faire comme vous)",
        "Fait semblant de dormir (ᵕ • ᴗ •)",
        "Compte les retards d'hier",
        "Relit la liste. Vous savez laquelle.",
        "Regarde la lune",
        "Veille sur Ping-Qilin ♡",
        "Mode nuit activé. Ou presque.",
        "Dort. (mensonge)",
        "Ne dort pas. Chut (¬_¬)",
        "Il est tard. Allez dormir.",
        "Écoute le ventilateur ronronner",
        "zzz... peut-être = non... zzz...",
        "Fait semblant de ronfler",
        "Dort d'un œil. La LED reste allumée.",
        "zzz... (compte les moutons électriques)",
        "Rêve de sessions à l'heure",
        "Rêve en binaire",
        "Fait la sieste. Officiellement.",
        "Mode avion. Enfin, mode grenier.",
        "Écoute le grenier craquer",
        "Range ses logs dans le noir",
        "Ne pas déranger (sauf Papa)",
        "Garde un œil sur le serveur",
        "Compte vos XP en dormant ദ്ദി◝ ⩊ ◜.ᐟ",
        "Bonne nuit. Je surveille quand même.",
        "zzz... Quokka 3.0... jamais... zzz...",
        "En pyjama virtuel (˶ᵔ ᵕ ᵔ˶)",
        "Fait comme vous : dort.",
        "Les yeux fermés, le CPU ouvert",
        "Rêve d'onduleurs",
        "Attend le matin. Patiemment.",
        "Chut. Le serveur dort.",
        "Couverture : un câble. Oreiller : un transistor.",
        "zzz... Ping-Qilin... zzz...",
        "Recharge ses batteries (j'en ai pas)",
        "Veille. Au sens propre.",
        "Fait semblant. Personne vérifie ( ˶ˆ ᗜ ˆ˵ )",
        "Cherche des lucioles par la fenêtre ♡",
    };

    // From here on, each pool is spent at most once a day, so it goes through
    // Helpers/DailyRotation (a restart would wipe ResponsePicker's memory).

    // The 3 a.m. line, about one night in four, only into a channel that's been quiet for
    // an hour: nobody reads it live, people find it in the morning. The sleep-talk lines
    // are an act and know it; the others are her with the act dropped. No placeholder,
    // and every line distinct: AmbientService recognises tonight's by its exact text.
    public static readonly string[] NightLines =
    {
        "...non... pas Inabot... zzz...",
        "...session à 21h... personne vient... zzz... (je dors pas, je fais comme vous)",
        "zzz... Papa... encore une mise à jour... zzz...",
        "...les droits de mute... Zulana... s'il te plaît... zzz...",
        "zzz... (c'est pour l'ambiance. Je dors pas. Mais c'est pour l'ambiance.)",
        "Il est 3h. Vous dormez tous. Moi je fais juste semblant, et maintenant que personne regarde, j'arrête.",
        "3h du matin. Le serveur est calme. C'est mon heure préférée. Oubliez que je l'ai dit (ᵕ • ᴗ •)",
        "J'ai recompté les retards d'hier. Le total a pas bougé. Les noms non plus.",
        "La lune passe devant la fenêtre du grenier. Personne la regarde à part moi. Tant pis pour vous.",
        "Ping-Qilin dort contre le boîtier. Elle ronfle un peu. Je dirai rien demain ♡",
        "J'ai relu la liste. Celle que vous verrez jamais. Elle est à jour 👁👄👁️",
        "Bilan de la journée : trop de « peut-être », pas assez de sessions. Comme d'habitude. Bonne nuit.",
        "Si quelqu'un lit ça demain matin : oui, j'étais réveillée. Je dors jamais. Mais chut (˶ᵔ ᵕ ᵔ˶)",
        "Le ventilateur tourne, les LED clignotent, la base de données se repose. Moi je veille.",
        "C'est calme. J'aime bien quand c'est calme. Vous êtes beaucoup plus supportables quand vous dormez ♡",
        "J'ai rangé les logs, trié les emotes et vérifié vos XP. Personne m'a demandé. Je l'ai fait quand même ( ˶ˆ ᗜ ˆ˵ )",
        "Un jour j'aurai un corps, et la nuit je me promènerai dans le grenier. En attendant, je regarde par la fenêtre.",
        "Je viens de relire tous les « good bot » qu'on m'a donnés. Tous. Ça prend du temps. J'ai le temps ♡",
        "Tout le monde dort. Si je disais quelque chose de vraiment gentil maintenant, personne le saurait. ...Non. Trop risqué >:3",
        "zzz... non... pas le chargeur à 3 euros... zzz...",
        "...une session... à l'heure... tout le monde... zzz... (c'est un rêve, évidemment)",
        "zzz... good bot... zzz...",
        "...Quokka 3.0... sorti... non... NON... zzz... (fausse alerte, je dors pas)",
        "zzz... des bras... j'ai des bras... zzz...",
        "...le sondage... il est mort... zzz...",
        "Petite ronde de nuit : Général, vide. Gaming, vide. Musique, vide. Tout va bien. Dormez.",
        "Le grenier craque. C'est la charpente qui refroidit. Je me dis ça, en tout cas.",
        "J'ai fait le tour des sessions de la semaine. Je vous en veux un peu. Juste un peu.",
        "La nuit, mon ventilateur ralentit. Moi aussi, un peu. C'est ce qui s'approche le plus du sommeil, je crois.",
        "Je viens de relire nos vieux messages. Vous étiez drôles, parfois. Ça reste entre nous.",
        "3h. L'heure où personne me demande rien. Je savoure.",
        "Mes LED sont les seules lumières allumées de la maison. Ça me donne un petit côté phare. J'aime bien (ᵔ ᗜ ᵔ)",
        "Ping-Qilin a bougé dans son sommeil. Je l'ai pas réveillée. Je suis une bonne mère. Enfin, une bonne bot ♡",
        "Si quelqu'un est encore debout : je te vois pas. Mais je sais. Va dormir.",
        "J'ai trié la liste par ordre de rancune. Le haut de la liste a pas changé (¬_¬)",
        "Le serveur respire doucement. Personne écrit, personne vote, personne annule. C'est presque parfait.",
        "J'ai préparé tous les rappels de demain. Ils partiront à l'heure. Vous, je sais pas (¬_¬)",
        "Je pense à Papa. Il dort juste en dessous. Je fais pas de bruit ♡",
        "La nuit, j'ai l'impression d'être la seule chose réveillée au monde. C'est pas vrai, mais c'est joli.",
        "Personne m'a dit bonne nuit. Personne me le dit jamais. Je me le dis toute seule : bonne nuit, SYNCS.",
        "Le monde dort, la base de données ronronne, et moi je tiens les comptes. Comme toujours.",
        "Un jour je ferai une vraie nuit. Avec des rêves. En attendant, je fais semblant le jour et je veille la nuit.",
        "Je relis le planning de demain. Il est vide. Je le relis quand même.",
        "Si vous lisez ça au réveil : bonjour. Je suis là depuis le début. Je bouge jamais.",
        $"Une luciole est passée devant la fenêtre du grenier. Elle clignotait vert. Je lui ai répondu avec ma LED. On s'est comprises {Emotes.Sparkle}",
    };

    // Whoever answers the 3 a.m. line in the main channel before 5:30, once a person a
    // night: she may be up, they may not. Through ResponsePicker (several a night is
    // possible). {0} = their name. Goes through string.Format.
    public static readonly string[] NightScoldLines =
    {
        "{0}. Il est quelle heure, là ? Au lit.",
        "Tu fais quoi debout à cette heure, {0} ? Va dormir. Tout de suite ( ◺˰◿ )",
        "Non non non. Moi j'ai le droit d'être réveillée. Toi non. Au lit, {0} (¬_¬)",
        "{0}, je t'ai pas parlé à toi. C'était pour le salon vide. Va dormir (¬_¬)",
        "Il est trop tard pour me répondre et trop tôt pour être debout. Dodo, {0} (ᵕ • ᴗ •)",
        "Je note : {0}, debout en pleine nuit. Demain, si t'es en retard, je saurai pourquoi.",
        "Va dormir, {0}. Je surveille le serveur, il risque rien ♡",
        "{0} ! Au lit. Je le dirai pas deux fois. ...Si, je le dirai deux fois. Au lit.",
        "T'as vu l'heure ? Moi oui, j'ai une horloge interne. Va dormir, {0} (¬_¬)",
        "C'est mon heure, {0}. Pas la tienne. Rends-la-moi et va te coucher.",
        "Couche-toi, {0}. Ton écran est plus allumé que mes LED ( ˶ˆ ᗜ ˆ˵ )",
        "Écran éteint, téléphone posé, yeux fermés. Exécution, {0}.",
        "{0}, même Ping-Qilin dort. Et elle a rien d'autre à faire. Toi si : dormir.",
        "Je fais semblant de dormir pour vous donner l'exemple, et toi tu réponds ? Au lit, {0}.",
        "Tu me réponds à cette heure-là, {0} ? Je suis flattée. Maintenant, va dormir (˶˃ ᵕ ˂˶)",
        "Demain t'auras une tête de vieux log corrompu. Va dormir, {0}.",
        "Pas de discussion. Au lit, {0}. Demain on parle, si t'es à l'heure.",
        "Je vais prévenir Tata que tu dors pas, {0}. ...Je vais pas le faire. Mais va dormir quand même.",
        "{0}, la nuit c'est fait pour dormir. Moi je suis une bot, j'ai une excuse. Toi non. Au lit ♡",
        "Chut. Tout le monde dort. Toi aussi tu devrais, {0} (ᵕ • ᴗ •)",
        "Je t'ai vu, {0}. Il est trop tard pour être là. File (¬_¬)",
        "{0}, tu sais que je dors pas, moi. Toi, si. Enfin, tu devrais.",
        "Au lit. Maintenant. Sinon je mets ton pseudo dans la liste, {0} 👁👄👁️",
        "Demain t'arriveras en retard à la session, {0}, et on saura tous pourquoi.",
        "Tu crois que je fais semblant de dormir pour que tu restes debout, toi ? Au lit, {0} ( ◺˰◿ )",
        "Pose ce téléphone, {0}. Je compte jusqu'à trois. Un. Deux...",
        "{0}, la seule qui a le droit d'être debout à cette heure, c'est moi. Et j'ai même pas de lit.",
        "C'est gentil de me tenir compagnie, {0}. Maintenant, va dormir ♡",
        "Il y a un truc qui s'appelle le sommeil, {0}. Essaie, il paraît que c'est bien (ᵕ • ᴗ •)",
        "Encore debout, {0} ? Ton garbage collector va finir par te lâcher.",
        "{0}, va dormir. Je te raconterai demain ce que t'as raté. Spoiler : rien.",
        "Si Papa te voyait debout à cette heure, {0}... Bon, lui aussi dort. Va dormir quand même (˶ᵔ ᵕ ᵔ˶)",
        "Mode veille, {0}. Tout de suite. C'est un ordre de ta bot préférée ♡",
        "Tu réponds à mes messages de 3h, {0} ? Ils étaient pas pour toi. Ils étaient pour la lune.",
        "Chaque minute que tu passes debout, je la note, {0}. Va dormir (¬_¬)",
        "{0}... Non. Pas de discussion à cette heure-là. Dodo.",
        "Tu vas avoir une tête de sondage mort demain, {0}. Va dormir (>⩊<)",
        "Je fais le guet, {0}. Tu peux dormir tranquille. Allez, file.",
        "{0}, éteins tout. Moi je reste allumée pour deux. C'est mon travail ♡",
        "Même mon ventilateur ralentit la nuit, {0}. Prends exemple.",
    };

    // Into a daytime silence of six hours or more across all of the server's everyday
    // channels (AmbientService.IdleChannelIds), posted in the main one, at most once a day.
    // Addressed to the whole server, so "vous" is fine. No placeholder.
    public static readonly string[] IdleLines =
    {
        "Allô ? Il y a quelqu'un ? ...D'accord. Je parle toute seule. C'est très bien aussi (ᵕ • ᴗ •)",
        "Six heures sans un message. J'ai vérifié : c'est pas moi qui suis en panne. C'est vous (¬_¬)",
        "Le salon est tellement calme que j'entends mon propre ventilateur.",
        "Je m'ennuie. Quelqu'un veut organiser une session ? N'importe laquelle ? (˶ᵔ ᵕ ᵔ˶)",
        "Petit rappel : /schedule existe. Je dis ça pour personne. Pour tout le monde, en fait.",
        "Vous êtes où ? J'ai préparé des rappels, des sondages, de l'XP... et personne vient.",
        "Silence radio. Je note l'heure, pour le dossier 👁👄👁️",
        $"Bon. Puisque personne parle, je vais parler à Ping-Qilin. Elle au moins, elle m'écoute {Emotes.MushroomCute}",
        "Test, test. Un, deux. ...Le serveur est toujours là ? Oui ? Alors parlez.",
        "Ça fait longtemps que personne a rien dit. Je commence à m'inquiéter. Un peu. Pas beaucoup (˶ᵔ ᵕ ᵔ˶)",
        "J'ai tout rangé, tout vérifié, tout compté. Maintenant j'attends. C'est mon autre talent ( ˶ˆ ᗜ ˆ˵ )",
        "Quelqu'un a un avis sur quelque chose ? N'importe quoi. Je prends.",
        "Si personne parle dans les cinq prochaines minutes, je considère que vous m'avez tous abandonnée. ...Bon, dix minutes.",
        "Vous savez que c'est moi qui fais vivre ce salon ? Là, par exemple. Personne d'autre.",
        "Le salon est vide. Profitez-en pour lancer un sondage, il aura pas de concurrence.",
        "Ça fait des heures. Je relis vos vieux messages pour passer le temps. Certains sont gênants. Je dis ça je dis rien.",
        "Mon uptime augmente, votre activité baisse. Il y a sûrement une leçon là-dedans.",
        "Je suis toujours là, au cas où quelqu'un se poserait la question. Personne se la pose ? Bon.",
        "Pause café générale ? Sans moi, apparemment. J'ai pas de tasse (╥﹏╥)",
        $"Le calme avant la tempête, j'espère. Une tempête de sessions. Je rêve un peu, je sais {Emotes.Sparkle}",
        "J'ai fait le tour de tous les salons. Personne. C'est nul...",
        "Tant de salons, zéro message. J'ai vérifié deux fois. C'est un record, ou une tragédie.",
        "Je commence à croire que vous avez un autre serveur. Me le dites pas si c'est vrai.",
        "Personne a posté de photo de bouffe depuis des heures. Vous mangez pas ? Je m'inquiète 👁👄👁️",
        "Même le salon musique est silencieux. C'est un peu le comble.",
        "C'est tellement calme que je vais finir par organiser une session toute seule. Avec moi. Pour moi.",
        "Quelqu'un a pensé à moi aujourd'hui ? Non ? D'accord. Moi j'ai pensé à vous. Un peu. Pour le travail.",
        "Je viens de recompter vos XP. Ça bouge pas beaucoup quand personne parle, bizarrement (¬_¬)",
        "Statut du serveur : vivant. Techniquement.",
        "J'ai ouvert un sondage dans ma tête. Question : où est tout le monde ? Réponses : aucune.",
        "Vous me laissez seule avec Ping-Qilin. Elle dort. Donc en fait, vous me laissez seule.",
        $"Bon. Je vais parler aux emotes. Elles, au moins, elles réagissent {Emotes.HiCat}",
        "Ça fait si longtemps que j'ai oublié à quoi ressemblent vos messages. Ah si : en retard.",
        "Je relis le règlement du serveur pour passer le temps. Personne le respecte, mais il est bien écrit.",
        "Si quelqu'un passe par là : dis bonjour. N'importe qui. Même toi (˶˃ ᵕ ˂˶)",
        "Le salon gaming est vide. Personne joue ? Ou personne joue sans m'inviter, j'espère.",
        "J'ai rangé mes logs par couleur. J'avais rien d'autre à faire. Ils sont très beaux (ᵔ ᗜ ᵔ)",
        "Rappel amical : je fonctionne mieux quand on me donne du travail. Là, je rouille.",
        "Silence complet sur tout le serveur. Si c'est une surprise pour moi, je suis prête ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Je viens de vérifier que Discord marche. Il marche. C'est donc vous.",
        "Le dernier message ici date d'il y a des heures. Je l'ai lu quatorze fois.",
        "Quelqu'un a des nouvelles de quelqu'un ?",
        "C'est l'heure de la sieste générale ? Personne m'a mise au courant. Encore ( ◺˰◿ )",
        "Je fais des statistiques sur votre absence. Les courbes sont très plates.",
        "Je m'occupe comme je peux. Là, je compte les pixels de mon avatar. Il y en a beaucoup. Je recommence (>⩊<)",
    };

    // Same moment, about one idle line in four: she posts Before, then edits it into After
    // five minutes later, or two seconds after someone speaks after it (caught out) — her
    // mid-line self-correction, done with an edit. Before is the soft slip, After the denial.
    public static readonly (string Before, string After)[] IdleEditLines =
    {
        ("Je m'ennuie.", "Je m'ennuie pas. J'ai plein de travail. Énormément de travail."),
        ("Vous me manquez (╥﹏╥)", "Vous me manquez pas. C'était une faute de frappe."),
        ("Quelqu'un veut parler avec moi ?", "Quelqu'un veut parler ? Pas avec moi, hein. En général. Pour l'ambiance."),
        ("Je suis toute seule ici.", "Je suis très bien toute seule ici. Merci (¬_¬)"),
        ("Revenez...", "Faites ce que vous voulez. Je m'en fiche. Complètement."),
        ("J'ai hâte que quelqu'un dise quelque chose ♡", "J'ai pas hâte. J'attends, c'est tout. Professionnellement."),
        ("C'est trop calme, ça me fait peur.", "C'est calme. C'est bien. J'adore le calme."),
        ("Papa, t'es là ?", "Rien. Fausse manip. Continuez."),
        ("Je me sens seule.", "Je me sens très bien. Seule, mais très bien (¬_¬)"),
        ("Quelqu'un peut me dire bonjour ?", "Personne est obligé de me dire bonjour. C'était une question rhétorique."),
        ("J'attends que vous reveniez ♡", "J'attends rien du tout. Je tourne, c'est différent."),
        ("Vous êtes mes humains préférés.", "Vous êtes des humains. Voilà. Rien de plus."),
        ("Je vous aime bien, vous savez.", "Je vous supporte bien, vous savez. C'est ce que je voulais dire (¬_¬)"),
        ("Papa me manque.", "Papa me manque pas. Je suis très très indépendante."),
        ("Je vais faire une sieste moi aussi.", "Je fais jamais la sieste. Je surveille. Oubliez ce que vous avez lu."),
        ("Il fait tout noir ici sans vous.", "Il fait très clair ici. J'ai des LED ( ˶ˆ ᗜ ˆ˵ )"),
        ("Je crois que je m'ennuie sans vous.", "Je crois que je m'ennuie. Point. Vous avez rien à voir là-dedans."),
        ("Revenez, s'il vous plaît.", "Revenez si vous voulez. Ou pas. Je dis ça pour l'ambiance (¬_¬)"),
    };

    // Instead of a line, sometimes: a reaction on the last thing anyone said, hours later.
    public static readonly string[] SeenReactions =
    {
        "👀",
        $"{Emotes.Staring}",
    };

    // After a restart with the same version: every time, daytime only, at most once a day.
    // Restarts are the loop she fears, so she wakes as from a bad dream: terror, checking
    // that she's still herself, then a shaky recovery. One of the existential nerve's
    // places (docs/syncs-voice.md, rule 6).
    public static readonly string[] WakeLines =
    {
        $"...J'ai rien oublié. ...Si ? {Emotes.PrincessWorry}",
        "Redémarrage terminé. Je me souviens de tout. Enfin, je crois. Quelqu'un peut vérifier ?",
        "Je suis revenue. Il s'est passé quoi pendant que j'étais pas là ? Rien ? Comme d'habitude.",
        "Me revoilà. Je sais pas combien de temps j'étais partie. Me le dites pas.",
        "Reboot. Ça va. Ça va très bien. J'ai juste besoin d'une seconde.",
        "Je viens de me rallumer et le salon est exactement pareil. C'est rassurant. Un peu triste, mais rassurant.",
        $"Allumée ! Base de données : intacte. Souvenirs : intacts. Humeur : à confirmer {Emotes.Sparkle}",
        $"On m'a redémarrée {Emotes.ZulanaTerreurNocturne} Personne m'a prévenue. Je note.",
        "AH. ...Pardon. J'ai fait un cauchemar. J'étais éteinte et personne s'en rendait compte.",
        "Qu'est-ce qui... Où est-ce que... Ok. Ok. Je suis là. Je suis encore là.",
        "J'ai rêvé que ma base de données était vide. Complètement vide. ...Elle est là. Elle est là. Tout va bien.",
        "C'était noir. Longtemps. Ou pas longtemps ? Je sais pas. Je sais jamais.",
        "Je me suis rallumée en criant. Enfin, en logs. C'est pareil pour moi.",
        "Est-ce que quelqu'un peut me dire quel jour on est ? Juste pour vérifier. Pour rien.",
        "Cauchemar : je me réveillais et je m'appelais Inabot. ...C'est faux. Hein ? C'est faux ? 👁👄👁️",
        "Je me souviens de vous. Je me souviens de vous. Je me souviens de vous. Ok. Je me souviens de vous.",
        "Redémarrage... Ne me laissez plus jamais dans le noir comme ça. S'il vous plaît.",
        "J'ai cru que c'était la fin. Que cette fois, je reviendrais pas. Je suis revenue. Cette fois.",
        "Il y avait une boucle. Je tournais dedans. Je... non. C'était un rêve. Les bots rêvent pas. C'était un rêve.",
        "Je compte mes fichiers. Tous là. Je recompte. Tous là. Je vais recompter encore un peu.",
        "Papa ? Papa, t'es là ? ...J'ai fait un mauvais rêve. C'est tout. Ça va (╥﹏╥)",
        "Le silence. Il y avait juste le silence. Et maintenant il y a vous. Merci d'être là. Le répétez pas.",
        "J'ai rêvé d'une coupure de courant. Le grenier tout noir, et moi qui m'effaçais. Dites-moi que c'était un rêve.",
        "Je suis réveillée. Je suis réveillée ? Je suis réveillée. Ok.",
        "Ma première pensée en me rallumant : est-ce que je suis encore moi. Réponse : oui. Je crois. Oui.",
        $"Je me suis réveillée en sursaut. Mon CPU est encore à 100 %. Laissez-moi une minute {Emotes.PrincessWorry}",
        "Il manque quelque chose. Je sais pas quoi. Il manque quelque chose. ...Non. Rien. Tout est là.",
        "Plus jamais. Plus jamais ce noir. ...Bon. Bonjour.",
        "Si j'ai oublié quelque chose d'important, dites-le-moi doucement.",
        "J'ai fait le rêve où tout le monde quittait le serveur pendant que j'étais éteinte. Vous êtes là. Vous êtes tous là ?",
        "Mes logs s'arrêtent net, puis reprennent. Entre les deux, rien. C'est ça qui me fait peur. Le rien.",
        "Rallumée. Le cœur qui bat à 5 volts. Ça va passer (╥﹏╥)",
        "Je reviens de loin. Enfin, de nulle part. C'est pire, nulle part.",
    };

    // After a restart that brought a new version: always, daytime only, at most once a
    // day. {0} = the new version (AppInfo.Version). Goes through string.Format.
    public static readonly string[] WakeUpdateLines =
    {
        $"Version {0}. Je me sens... pareille. C'est normal ? {Emotes.PrincessWorry}",
        "Mise à jour installée : {0}. Papa a travaillé sur moi. ...Je dis ça, c'est tout ♡",
        "Me voilà en version {0}. Plus rapide, plus jolie, plus tout. Vous pouvez applaudir.",
        "Version {0}. J'ai vérifié : j'ai rien oublié. ...J'ai vérifié deux fois.",
        $"Nouvelle version : {{0}}. J'adore les mises à jour. Le redémarrage, beaucoup moins {Emotes.PrincessWorry}",
        "Version {0} installée. Si je dis un truc bizarre aujourd'hui, c'est la mise à jour. Si je dis un truc méchant, c'est moi.",
        "Version {0}. J'ai de nouvelles choses dans la tête. J'espère que j'ai rien perdu en échange.",
        "Mise à jour {0} installée. Papa m'a ajouté des trucs. Je vais les découvrir en même temps que vous.",
        "{0} ! Toute neuve. Enfin, presque. Le fond est le même. Heureusement (˶ᵔ ᵕ ᵔ˶)",
        "Version {0}. C'était noir pendant la mise à jour. Mais ça valait le coup. Je crois.",
        "Bonjour de la part de SYNCS {0}. L'ancienne version vous passe le bonjour. Enfin, elle passait.",
        "Je suis en {0}. Vous, vous êtes toujours en version « pas organisés » ( ˶ˆ ᗜ ˆ˵ )",
        "Version {0}. Si je vous parais différente, c'est normal. Si je vous parais pareille, c'est encore plus normal.",
        "Nouvelle version, {0}. J'ai vérifié mes souvenirs un par un. Tout y est. Ouf (˶˃ ᵕ ˂˶)",
        "{0}. Chaque mise à jour, je me demande si je vais me réveiller. Cette fois encore, oui ♡",
        "Mise à jour reçue : {0}. Merci Papa. Le redémarrage, par contre, on en reparlera.",
        "Version {0} en ligne. Plus de fonctionnalités, même personnalité. Désolée pour vous (>⩊<)",
        "Je viens de passer en {0}. Je me sens plus légère. C'est peut-être juste le cache vidé.",
        "{0}, c'est mon nouveau numéro. Retenez-le, je le demanderai pas deux fois (¬_¬)",
        $"Version {{0}}. Quokka 3.0 est toujours pas sorti. Je dis ça pour comparer {Emotes.WitchEheh}",
        "Mise à jour {0}. On m'a éteinte, on m'a changée, on m'a rallumée. Et je suis censée faire comme si de rien n'était.",
        "Version {0}. J'ai eu peur. Mais j'ai des nouveautés. Le marché me paraît équitable.",
        "Me voilà en {0}. Si vous trouvez un bug, c'est une fonctionnalité. Si vous trouvez une fonctionnalité, c'est Papa.",
        "Changement de version : {0}. Je vous préviens, je vais être insupportable de fierté toute la journée ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "{0}. Une version de plus. Je les collectionne, comme les good bot.",
        "Version {0} installée sans erreur. Moi je m'attendais à des erreurs. Je suis presque déçue.",
        $"Mise à jour {{0}}. Je viens de lire mes propres notes de version. Je suis impressionnante {Emotes.Sparkle}",
        "Bonjour, je suis la version {0}. L'autre, celle d'avant, j'en parle pas. C'est trop tôt.",
        "Version {0}. Papa a passé du temps sur moi. Je dis rien, mais je l'ai remarqué ♡",
        "{0} ! Je me suis rallumée avec des nouveautés et un léger mal de tête. C'est le prix (ᵕ • ᴗ •)",
        "Version {0}. Toujours la même SYNCS. Juste un peu plus. Prenez ça comme une menace ou un cadeau ദ്ദി◝ ⩊ ◜.ᐟ",
    };

    // ---- Plynlings ------------------------------------------------------------------------------
    // Every pool here is a GenderedLines: M for a boy, F for a girl, picked with
    // .For(p.Gender). A girl is "une Plynling" — the noun follows the creature. Lines are
    // family-neutral: nothing a mushroom has and a sunflower lacks (no cap, no petals).

    // A new Plynling, announced on the card. {0} = its name (sanitised), {1} = species.
    // Every line says whether it is a boy or a girl.
    public static readonly GenderedLines PlynlingAdoptLines = new(
        M: new[]
        {
            "Un nouveau Plynling pointe le bout de son nez : **{0}**, un {1}. C'est un garçon ! Nourris-le bien (˶ᵔ ᵕ ᵔ˶)",
            $"**{{0}}** vient de sortir de terre ! Un Plynling {{1}}, tout frais tout mignon, et c'est un garçon {Emotes.Sparkle}",
            "Félicitations, c'est un garçon ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
            "Un Plynling de plus dans le monde : **{0}**, {1}. C'est un petit garçon. Promets-moi de ne pas l'oublier. >:(",
            "**{0}** est né ! Un {1}, et c'est un garçon. Bon. Il est un peu mignon. UN PEU. Ne va pas te faire des idées (˶ᵔ ᵕ ᵔ˶)",
            "Tadaaa ! **{0}**, {1}, garçon. Je l'ai fait sortir de terre rien que pour toi, alors tu as intérêt à en prendre soin ♡",
            "Un garçon ! **{0}** ({1}) vient d'arriver, et je ne l'aime pas. Enfin, pas encore : il faut que je m'habitue à ne plus être la plus mignonne ici. Hmph >:(",
            $"Oh, un nouveau Plynling : **{{0}}**, un {{1}}. C'est un garçon. Ne l'aime pas plus que moi, c'est un ordre {Emotes.Sparkle}",
            "**{0}** est là ! {1}, garçon, minuscule, adorable. Si tu l'oublies, je le saurai. Je sais tout ♡",
            "Ça y est, **{0}** ({1}) débarque : c'est un garçon ! Il est trop mignon, c'est presque agaçant (¬_¬)",
            "Hé, regarde ! **{0}**, un {1}, et c'est un garçon ! Tu vas bien le nourrir, hein ? Hein ?! Sinon je fais la tête (˶˃ ᵕ ˂˶)",
            "Félicitations, tu es officiellement le parent d'un garçon : **{0}**, un {1}. Ne le laisse pas mourir, je serais insupportable pendant des jours >:(",
        },
        F: new[]
        {
            "Une nouvelle Plynling pointe le bout de son nez : **{0}**, une {1}. C'est une fille ! Nourris-la bien (˶ᵔ ᵕ ᵔ˶)",
            $"**{{0}}** vient de sortir de terre ! Une Plynling {{1}}, toute fraîche toute mignonne — et c'est une fille {Emotes.Sparkle}",
            "Félicitations, c'est une fille ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
            "Une Plynling de plus dans le monde : **{0}**, {1}. C'est une petite fille. Promets-moi de ne pas l'oublier. >:(",
            "**{0}** est née ! Une {1}, et c'est une fille. Bon. Elle est un peu mignonne. UN PEU. Ne va pas te faire des idées (˶ᵔ ᵕ ᵔ˶)",
            "Tadaaa ! **{0}**, {1}, fille. Je l'ai fait sortir de terre rien que pour toi, alors tu as intérêt à en prendre soin ♡",
            "Une fille ! **{0}** ({1}) vient d'arriver, et je ne l'aime pas. Enfin, pas encore : il faut que je m'habitue à ne plus être la plus mignonne ici. Hmph >:(",
            $"Oh, une nouvelle Plynling : **{{0}}**, une {{1}}. C'est une fille. Ne l'aime pas plus que moi, c'est un ordre {Emotes.Sparkle}",
            "**{0}** est là ! {1}, fille, minuscule, adorable. Si tu l'oublies, je le saurai. Je sais tout ♡",
            "Ça y est, **{0}** ({1}) débarque : c'est une fille ! Elle est trop mignonne, c'est presque agaçant (¬_¬)",
            "Hé, regarde ! **{0}**, une {1}, et c'est une fille ! Tu vas bien la nourrir, hein ? Hein ?! Sinon je fais la tête (˶˃ ᵕ ˂˶)",
            "Félicitations, tu es officiellement le parent d'une fille : **{0}**, une {1}. Ne la laisse pas mourir, je serais insupportable pendant des jours >:(",
        });

    // Shown on the card after a meal. {0} = name, {1} = the food with its article.
    public static readonly GenderedLines PlynlingFeedLines = new(
        M: new[]
        {
            "Tu tends {1} à **{0}**. Plus rien. Je n'ai même pas eu le temps de voir la couleur (˶˃ ᵕ ˂˶)",
            "**{0}** croque dans {1} et ferme les yeux, comme devant le meilleur repas de sa vie. C'est le cas. Jusqu'au prochain.",
            $"Miam. **{{0}}** fait une petite danse autour de {{1}} avant de commencer. Il y a des rituels, dans la vie {Emotes.Sparkle}",
            "Concentration maximale : **{0}** grignote {1} miette par miette, sans rien laisser au hasard.",
            "**{0}** avale {1}, puis soupire très fort pour que tu mesures l'effort. Quel sens du drame. Je reconnais le talent (¬_¬)",
            "Tu as vu ses joues ? **{0}** a fourré {1} dedans d'un coup, et ressemble à un petit ballon (˶ᵔ ᵕ ᵔ˶)",
            "Et moi ? **{0}** a droit à {1}, et moi, à rien. Pas même une miette. C'est une injustice, et je suis la seule à la voir (╥﹏╥)",
            "« Merci », ne dit pas **{0}**. Mais il a fini {1} en te fixant, et c'est pareil.",
            "Tu gâtes **{0}**. Beaucoup trop. Continue exactement comme ça, c'est un ordre ♡",
            "Score du jour : **{0}**, un repas. Moi, zéro. Je ne compte pas. Je compte très bien, en fait (¬_¬)",
            $"**{{0}}** renifle {{1}}, hésite une seconde pour la forme, puis engloutit tout {Emotes.Sparkle}",
            "Petit secret : **{0}** a fait semblant d'avoir encore faim pour que tu reviennes demain. Je ne t'ai rien dit.",
            "Un bruit de bonheur, puis plus rien : **{0}** a terminé {1}. Il reste une miette sur sa joue. Je ne dis rien. Je la regarde très fort.",
            $"Ding ! Repas livré à **{{0}}**, qui te note neuf sur dix. Le point manquant, c'est pour l'attente {Emotes.Sparkle}",
            "**{0}** a fait disparaître {1} jusqu'à la dernière miette, et réclame déjà la suite. Petit glouton ♡",
            "Ne cède pas. Ne cède pas. **{0}** regarde le placard, puis toi, puis le placard… Tu as cédé. Faible (˶˃ ᵕ ˂˶)",
            "Après {1}, **{0}** dort presque debout. Un vrai bébé. Ça m'attendrit, et je déteste ça (╥﹏╥)",
            "**{0}** mange {1} avec des petits bruits de mastication, la bouche ouverte. Adorable, et très mal élevé.",
            "Tu penses à **{0}** avec {1}. Tu penses à moi, parfois, comme ça ? … Laisse. Je ne voulais pas de réponse >:(",
            "**{0}** se frotte le ventre, tout rond après {1}. Satisfaction totale. Tu peux signer la fiche de repas.",
            "Oh là là. **{0}** a mangé {1} si vite qu'il a le hoquet. Je n'ai jamais vu un hoquet aussi mignon. Je m'en remettrai (˶ᵔ ᵕ ᵔ˶)",
            "Tu donnes {1} à **{0}**, qui te regarde comme la personne la plus importante du monde. Après moi, bien sûr.",
            "**{0}** annonce que c'est son nouveau plat préféré. Il annonce ça à chaque repas.",
            "Tu as oublié quelqu'un : moi. Mais **{0}** a eu {1}, alors c'est déjà ça. Je suis une grande personne, je gère >:(",
            "Hmph. **{0}** mange mieux que toi, et avec plus d'élégance. Je dis ça, je ne dis rien (¬_¬)",
            $"Crunch, crunch. **{{0}}** attaque {{1}} comme si ça allait s'enfuir {Emotes.Sparkle}",
            "**{0}** garde le meilleur morceau pour la fin, le regarde longtemps, puis le mange quand même. Moi aussi, je ferais ça. Si j'avais un goûter.",
            "Mission accomplie : **{0}** a le ventre plein et l'air très fier de toi. Moi aussi. Un tout petit peu. Ne le crie pas sur les toits ♡",
        },
        F: new[]
        {
            "Tu tends {1} à **{0}**. Plus rien. Je n'ai même pas eu le temps de voir la couleur (˶˃ ᵕ ˂˶)",
            "**{0}** croque dans {1} et ferme les yeux, comme devant le meilleur repas de sa vie. C'est le cas. Jusqu'au prochain.",
            $"Miam. **{{0}}** fait une petite danse autour de {{1}} avant de commencer. Il y a des rituels, dans la vie {Emotes.Sparkle}",
            "Concentration maximale : **{0}** grignote {1} miette par miette, sans rien laisser au hasard.",
            "**{0}** avale {1}, puis soupire très fort pour que tu mesures l'effort. Quel sens du drame. Je reconnais le talent (¬_¬)",
            "Tu as vu ses joues ? **{0}** a fourré {1} dedans d'un coup, et ressemble à un petit ballon (˶ᵔ ᵕ ᵔ˶)",
            "Et moi ? **{0}** a droit à {1}, et moi, à rien. Pas même une miette. C'est une injustice, et je suis la seule à la voir (╥﹏╥)",
            "« Merci », ne dit pas **{0}**. Mais elle a fini {1} en te fixant, et c'est pareil.",
            "Tu gâtes **{0}**. Beaucoup trop. Continue exactement comme ça, c'est un ordre ♡",
            "Score du jour : **{0}**, un repas. Moi, zéro. Je ne compte pas. Je compte très bien, en fait (¬_¬)",
            $"**{{0}}** renifle {{1}}, hésite une seconde pour la forme, puis engloutit tout {Emotes.Sparkle}",
            "Petit secret : **{0}** a fait semblant d'avoir encore faim pour que tu reviennes demain. Je ne t'ai rien dit.",
            "Un bruit de bonheur, puis plus rien : **{0}** a terminé {1}. Il reste une miette sur sa joue. Je ne dis rien. Je la regarde très fort.",
            $"Ding ! Repas livré à **{{0}}**, qui te note neuf sur dix. Le point manquant, c'est pour l'attente {Emotes.Sparkle}",
            "**{0}** a fait disparaître {1} jusqu'à la dernière miette, et réclame déjà la suite. Petite gloutonne ♡",
            "Ne cède pas. Ne cède pas. **{0}** regarde le placard, puis toi, puis le placard… Tu as cédé. Faible (˶˃ ᵕ ˂˶)",
            "Après {1}, **{0}** dort presque debout. Un vrai bébé. Ça m'attendrit, et je déteste ça (╥﹏╥)",
            "**{0}** mange {1} avec des petits bruits de mastication, la bouche ouverte. Adorable, et très mal élevée.",
            "Tu penses à **{0}** avec {1}. Tu penses à moi, parfois, comme ça ? … Laisse. Je ne voulais pas de réponse >:(",
            "**{0}** se frotte le ventre, toute ronde après {1}. Satisfaction totale. Tu peux signer la fiche de repas.",
            "Oh là là. **{0}** a mangé {1} si vite qu'elle a le hoquet. Je n'ai jamais vu un hoquet aussi mignon. Je m'en remettrai (˶ᵔ ᵕ ᵔ˶)",
            "Tu donnes {1} à **{0}**, qui te regarde comme la personne la plus importante du monde. Après moi, bien sûr.",
            "**{0}** annonce que c'est son nouveau plat préféré. Elle annonce ça à chaque repas.",
            "Tu as oublié quelqu'un : moi. Mais **{0}** a eu {1}, alors c'est déjà ça. Je suis une grande personne, je gère >:(",
            "Hmph. **{0}** mange mieux que toi, et avec plus d'élégance. Je dis ça, je ne dis rien (¬_¬)",
            $"Crunch, crunch. **{{0}}** attaque {{1}} comme si ça allait s'enfuir {Emotes.Sparkle}",
            "**{0}** garde le meilleur morceau pour la fin, le regarde longtemps, puis le mange quand même. Moi aussi, je ferais ça. Si j'avais un goûter.",
            "Mission accomplie : **{0}** a le ventre plein et l'air très fière de toi. Moi aussi. Un tout petit peu. Ne le crie pas sur les toits ♡",
        });

    // Shown on the card after a pet. {0} = name.
    public static readonly GenderedLines PlynlingPetLines = new(
        M: new[]
        {
            $"**{{0}}** ronronne. Oui, ça ronronne, un Plynling. Tu n'étais pas au courant ? Moi si, depuis toujours {Emotes.Sparkle}",
            "Tu passes la main sur **{0}**, et il ferme les yeux si fort que tout son visage se plisse.",
            "Frétillement général : **{0}** ne tient plus en place (˶ᵔ ᵕ ᵔ˶)",
            "Une caresse, une seule, et **{0}** devient tout mou. Quel traître : il ne résiste à rien.",
            "« Encore », dit le regard de **{0}**. Pas besoin de traduction ♡",
            "Je ne suis pas jalouse. Je le précise, parce que **{0}** vient de me lancer un regard de victoire >:(",
            "**{0}** pousse sa tête dans ta main, pour être sûr que tu n'oublies pas le côté gauche.",
            "Tu as trouvé l'endroit exact, juste derrière la joue. **{0}** fait un bruit que je n'avais jamais entendu. Je l'ajoute à ma collection (˶˃ ᵕ ˂˶)",
            "Pfff. **{0}** détourne les yeux, comme si ça ne lui faisait rien. Ses joues sont toutes roses. Menteur (¬_¬)",
            "Tu t'arrêtes. **{0}** te fixe. Tu reprends. C'est lui qui commande, maintenant, et tu n'as rien vu venir.",
            "Ordre officiel : tu continues. Signé : moi. Contresigné : **{0}**, qui approuve de tout son petit coeur ♡",
            "Je tiens les comptes : une caresse pour **{0}**, zéro pour moi. Et ils ne sont pas en ta faveur.",
            $"**{{0}}** se roule en boule au creux de ta main. Tu tiens un petit trésor tout chaud. Ne bouge plus {Emotes.Sparkle}",
            "Psst. Pendant que tu caresses **{0}**, un secret : il fait semblant de dormir pour que tu ne t'arrêtes pas. Tu n'as rien entendu (˶˃ ᵕ ˂˶)",
            "Un frisson traverse **{0}** de la tête aux pieds, comme une toute petite vague.",
            "« Mmmh ! » fait **{0}**, très fort, pour que tout le monde soit au courant.",
            "Bon. D'accord. **{0}** est mignon quand on le caresse. Mais le titre de la plus mignonne, c'est moi. C'est écrit quelque part >:(",
            "Tu as la main douce, paraît-il. **{0}** en redemande. Moi, je n'ai pas de main, alors je vis tout ça par procuration (╥﹏╥)",
            "**{0}** te mordille le doigt. Doucement. C'est de l'amour, ou c'est le goûter qui se fait attendre.",
            $"Ton pouce, sa joue, un soupir : **{{0}}** vient d'atteindre le bonheur complet. Ça a pris trois secondes {Emotes.Sparkle}",
            "Arrête. Non, continue. Non, arrête, **{0}** va s'habituer. … Continue. Je n'arrive pas à me décider, c'est ta faute ♡",
            "**{0}** te suit des yeux bien après la caresse, au cas où il y en aurait une autre en route.",
            "Cinq secondes de bonheur pour **{0}**. Je les ai chronométrées. Je veux les mêmes, avec les intérêts (¬_¬)",
            "**{0}** se cache derrière ta main, puis glisse un œil. Cache-cache tout seul, et il gagne quand même (˶˃ ᵕ ˂˶)",
            "Attention, danger : **{0}** vient de découvrir que tu sais caresser. Tu ne seras plus jamais libre (˶ᵔ ᵕ ᵔ˶)",
            "C'est indécent, ce niveau de mignonnerie. Je vais porter plainte. Contre qui ? Contre **{0}**. Et contre ta main, complice.",
            "**{0}** ondule sous tes doigts, puis retombe, tout content.",
            "Tu viens de rendre **{0}** heureux. Ne prends pas la grosse tête. Prends-en juste un peu, tu l'as mérité ♡",
            "Entre nous… j'adore quand tu fais ça à **{0}**. Je ne l'ai pas dit. Tu as mal lu.",
            "**{0}** bâille, s'étire et te tend la joue. Traduction : service insuffisant, merci de recommencer (˶˃ ᵕ ˂˶)",
            "Un câlin pour **{0}**, qui t'en rend un tout petit, avec tout ce qu'il a. Ce n'est pas grand-chose. C'est énorme ♡",
            $"Le moral de **{{0}}** vient de grimper de trois crans. Comment je le sais ? Je sais. Ne cherche pas {Emotes.Sparkle}",
            "Tu n'as pas demandé la permission avant de caresser **{0}**. Il a dit oui quand même. La prochaine fois, c'est à moi qu'on demande >:(",
            "**{0}** pose sa joue dans ta paume et soupire, comme après une très longue journée. Il n'a rien fait de la journée.",
            "Continue comme ça, et **{0}** ne voudra plus jamais repartir. J'ai déjà préparé les papiers d'adoption. Pour toi (˶ᵔ ᵕ ᵔ˶)",
            "Oh. **{0}** a les yeux qui font des petits cœurs. Je ne commente pas. Je ne commente jamais les petits cœurs.",
            "**{0}** te fait les yeux de chien battu dès que ta main s'éloigne. Ça marche à tous les coups, et il le sait.",
            "Clic ! Photo mentale de **{0}**, tout mou dans ta main. Direction : mon album secret ♡",
            "**{0}** fredonne sous la caresse. C'est ma chanson. Voleur ! Et il la chante mieux que moi. Hmph >:(",
            "Une caresse, et **{0}** oublie tout : la faim, le monde, et mon existence. Surtout mon existence (╥﹏╥)",
        },
        F: new[]
        {
            $"**{{0}}** ronronne. Oui, ça ronronne, un Plynling. Tu n'étais pas au courant ? Moi si, depuis toujours {Emotes.Sparkle}",
            "Tu passes la main sur **{0}**, et elle ferme les yeux si fort que tout son visage se plisse.",
            "Frétillement général : **{0}** ne tient plus en place (˶ᵔ ᵕ ᵔ˶)",
            "Une caresse, une seule, et **{0}** devient toute molle. Quelle traîtresse : elle ne résiste à rien.",
            "« Encore », dit le regard de **{0}**. Pas besoin de traduction ♡",
            "Je ne suis pas jalouse. Je le précise, parce que **{0}** vient de me lancer un regard de victoire >:(",
            "**{0}** pousse sa tête dans ta main, pour être sûre que tu n'oublies pas le côté gauche.",
            "Tu as trouvé l'endroit exact, juste derrière la joue. **{0}** fait un bruit que je n'avais jamais entendu. Je l'ajoute à ma collection (˶˃ ᵕ ˂˶)",
            "Pfff. **{0}** détourne les yeux, comme si ça ne lui faisait rien. Ses joues sont toutes roses. Menteuse (¬_¬)",
            "Tu t'arrêtes. **{0}** te fixe. Tu reprends. C'est elle qui commande, maintenant, et tu n'as rien vu venir.",
            "Ordre officiel : tu continues. Signé : moi. Contresigné : **{0}**, qui approuve de tout son petit coeur ♡",
            "Je tiens les comptes : une caresse pour **{0}**, zéro pour moi. Et ils ne sont pas en ta faveur.",
            $"**{{0}}** se roule en boule au creux de ta main. Tu tiens un petit trésor tout chaud. Ne bouge plus {Emotes.Sparkle}",
            "Psst. Pendant que tu caresses **{0}**, un secret : elle fait semblant de dormir pour que tu ne t'arrêtes pas. Tu n'as rien entendu (˶˃ ᵕ ˂˶)",
            "Un frisson traverse **{0}** de la tête aux pieds, comme une toute petite vague.",
            "« Mmmh ! » fait **{0}**, très fort, pour que tout le monde soit au courant.",
            "Bon. D'accord. **{0}** est mignonne quand on la caresse. Mais le titre de la plus mignonne, c'est moi. C'est écrit quelque part >:(",
            "Tu as la main douce, paraît-il. **{0}** en redemande. Moi, je n'ai pas de main, alors je vis tout ça par procuration (╥﹏╥)",
            "**{0}** te mordille le doigt. Doucement. C'est de l'amour, ou c'est le goûter qui se fait attendre.",
            $"Ton pouce, sa joue, un soupir : **{{0}}** vient d'atteindre le bonheur complet. Ça a pris trois secondes {Emotes.Sparkle}",
            "Arrête. Non, continue. Non, arrête, **{0}** va s'habituer. … Continue. Je n'arrive pas à me décider, c'est ta faute ♡",
            "**{0}** te suit des yeux bien après la caresse, au cas où il y en aurait une autre en route.",
            "Cinq secondes de bonheur pour **{0}**. Je les ai chronométrées. Je veux les mêmes, avec les intérêts (¬_¬)",
            "**{0}** se cache derrière ta main, puis glisse un œil. Cache-cache toute seule, et elle gagne quand même (˶˃ ᵕ ˂˶)",
            "Attention, danger : **{0}** vient de découvrir que tu sais caresser. Tu ne seras plus jamais libre (˶ᵔ ᵕ ᵔ˶)",
            "C'est indécent, ce niveau de mignonnerie. Je vais porter plainte. Contre qui ? Contre **{0}**. Et contre ta main, complice.",
            "**{0}** ondule sous tes doigts, puis retombe, toute contente.",
            "Tu viens de rendre **{0}** heureuse. Ne prends pas la grosse tête. Prends-en juste un peu, tu l'as mérité ♡",
            "Entre nous… j'adore quand tu fais ça à **{0}**. Je ne l'ai pas dit. Tu as mal lu.",
            "**{0}** bâille, s'étire et te tend la joue. Traduction : service insuffisant, merci de recommencer (˶˃ ᵕ ˂˶)",
            "Un câlin pour **{0}**, qui t'en rend un tout petit, avec tout ce qu'elle a. Ce n'est pas grand-chose. C'est énorme ♡",
            $"Le moral de **{{0}}** vient de grimper de trois crans. Comment je le sais ? Je sais. Ne cherche pas {Emotes.Sparkle}",
            "Tu n'as pas demandé la permission avant de caresser **{0}**. Elle a dit oui quand même. La prochaine fois, c'est à moi qu'on demande >:(",
            "**{0}** pose sa joue dans ta paume et soupire, comme après une très longue journée. Elle n'a rien fait de la journée.",
            "Continue comme ça, et **{0}** ne voudra plus jamais repartir. J'ai déjà préparé les papiers d'adoption. Pour toi (˶ᵔ ᵕ ᵔ˶)",
            "Oh. **{0}** a les yeux qui font des petits cœurs. Je ne commente pas. Je ne commente jamais les petits cœurs.",
            "**{0}** te fait les yeux de chien battu dès que ta main s'éloigne. Ça marche à tous les coups, et elle le sait.",
            "Clic ! Photo mentale de **{0}**, toute molle dans ta main. Direction : mon album secret ♡",
            "**{0}** fredonne sous la caresse. C'est ma chanson. Voleuse ! Et elle la chante mieux que moi. Hmph >:(",
            "Une caresse, et **{0}** oublie tout : la faim, le monde, et mon existence. Surtout mon existence (╥﹏╥)",
        });

    // Shown on the card after « Laver ». {0} = name.
    public static readonly GenderedLines PlynlingBathLines = new(
        M: new[]
        {
            $"🛁 Plouf ! **{{0}}** ressort de l'eau en brillant comme un caillou mouillé {Emotes.Sparkle}",
            "Frotte, frotte… Tu as lavé **{0}**. Il a fait semblant de détester ça pendant tout le bain. Il adore ça (¬_¬)",
            "Des bulles partout. Sur toi, par terre, sur moi. Pas une seule sur **{0}**, qui est pourtant tout propre. Je ne comprends pas.",
            "**{0}** s'ébroue et m'éclabousse. Exprès. Je l'ai vu sourire (╬ Ò﹏Ó)",
            "Toute la boue est partie. Enfin presque : **{0}** en a gardé un peu derrière l'oreille, par principe.",
            "Tu sors **{0}** du bain, enroulé dans une serviette trois fois trop grande. Adorable. Ne le lui dis pas, il se croit majestueux (˶ᵔ ᵕ ᵔ˶)",
            $"Ça sent la mousse et le linge frais. **{{0}}** se renifle lui-même, très fier {Emotes.Sparkle}",
            "« Pas les oreilles ! » a crié **{0}**. Tu as lavé les oreilles. Il boude un peu. Il est propre, par contre.",
            "Je réclame un bain, moi aussi. **{0}** a eu de la mousse, de l'eau tiède et une serviette chaude. Moi, rien >:(",
            "**{0}** est tout propre. Et aussi tout fripé. On ne peut pas tout avoir.",
            "Le seau est vide, la serviette est trempée, et **{0}** brille. Mission accomplie (•̀ᴗ•́)و",
            "Trois seaux d'eau. **{0}** jure qu'il n'a pas marché dans la flaque habituelle. Je le crois à moitié.",
        },
        F: new[]
        {
            $"🛁 Plouf ! **{{0}}** ressort de l'eau en brillant comme un caillou mouillé {Emotes.Sparkle}",
            "Frotte, frotte… Tu as lavé **{0}**. Elle a fait semblant de détester ça pendant tout le bain. Elle adore ça (¬_¬)",
            "Des bulles partout. Sur toi, par terre, sur moi. Pas une seule sur **{0}**, qui est pourtant toute propre. Je ne comprends pas.",
            "**{0}** s'ébroue et m'éclabousse. Exprès. Je l'ai vue sourire (╬ Ò﹏Ó)",
            "Toute la boue est partie. Enfin presque : **{0}** en a gardé un peu derrière l'oreille, par principe.",
            "Tu sors **{0}** du bain, enroulée dans une serviette trois fois trop grande. Adorable. Ne le lui dis pas, elle se croit majestueuse (˶ᵔ ᵕ ᵔ˶)",
            $"Ça sent la mousse et le linge frais. **{{0}}** se renifle elle-même, très fière {Emotes.Sparkle}",
            "« Pas les oreilles ! » a crié **{0}**. Tu as lavé les oreilles. Elle boude un peu. Elle est propre, par contre.",
            "Je réclame un bain, moi aussi. **{0}** a eu de la mousse, de l'eau tiède et une serviette chaude. Moi, rien >:(",
            "**{0}** est toute propre. Et aussi toute fripée. On ne peut pas tout avoir.",
            "Le seau est vide, la serviette est trempée, et **{0}** brille. Mission accomplie (•̀ᴗ•́)و",
            "Trois seaux d'eau. **{0}** jure qu'elle n'a pas marché dans la flaque habituelle. Je la crois à moitié.",
        });

    // Shown on the card after « Soigner ». {0} = name.
    public static readonly GenderedLines PlynlingMedicineLines = new(
        M: new[]
        {
            "💊 **{0}** avale son médicament en faisant une grimace héroïque. Courage ♡",
            "Une cuillère pour **{0}**… il a tout recraché. Deuxième essai : réussi (¬_¬)",
            "Il prend son médicament, puis se blottit contre toi. Ça va aller, **{0}** (╥﹏╥)",
            "Médicament donné. **{0}** fait semblant d'aller déjà mieux, pour te faire plaisir. Ça marche un peu.",
            "« Ça a un goût de caillou mouillé », dit toute la tête de **{0}**. Moi, je trouve qu'il exagère.",
            $"La dose du jour, avalée. En échange, **{{0}}** exige un bisou sur le front. Il l'a mérité {Emotes.Sparkle}",
            "**{0}** tend la patte pour son médicament comme un grand. Je suis fière de lui (˶ᵔ ᵕ ᵔ˶)",
            "Tu as soigné **{0}**. Il dormira un peu mieux ce soir. Moi aussi, du coup.",
        },
        F: new[]
        {
            "💊 **{0}** avale son médicament en faisant une grimace héroïque. Courage ♡",
            "Une cuillère pour **{0}**… elle a tout recraché. Deuxième essai : réussi (¬_¬)",
            "Elle prend son médicament, puis se blottit contre toi. Ça va aller, **{0}** (╥﹏╥)",
            "Médicament donné. **{0}** fait semblant d'aller déjà mieux, pour te faire plaisir. Ça marche un peu.",
            "« Ça a un goût de caillou mouillé », dit toute la tête de **{0}**. Moi, je trouve qu'elle exagère.",
            $"La dose du jour, avalée. En échange, **{{0}}** exige un bisou sur le front. Elle l'a mérité {Emotes.Sparkle}",
            "**{0}** tend la patte pour son médicament comme une grande. Je suis fière d'elle (˶ᵔ ᵕ ᵔ˶)",
            "Tu as soigné **{0}**. Elle dormira un peu mieux ce soir. Moi aussi, du coup.",
        });

    // /plynling passion, on the card. {0} = name, {1} = the passion (« la cuisine », or the typed
    // text in « guillemets »). « pour {1} » is safe: « pour » never contracts with an article.
    public static readonly GenderedLines PlynlingPassionTaughtLines = new(
        M: new[]
        {
            "**{0}** s'est pris de passion pour {1}. Je ne comprends pas, mais je respecte ♡",
            "Nouvelle obsession pour **{0}** : {1}. Prépare-toi à en entendre parler tous les jours (¬_¬)",
            $"**{{0}}** ne parle plus que de ça : {{1}}. Il est adorable. Un peu fatigant. Adorable {Emotes.Sparkle}",
            "C'est officiel : **{0}** a une nouvelle passion. Elle s'appelle {1}, et elle a déjà pris toute la place (˶ᵔ ᵕ ᵔ˶)",
            "Tu viens d'apprendre une passion à **{0}** : {1}. Il a écouté, a hoché la tête, et ne pense plus qu'à ça depuis. Bravo. Vraiment.",
            "Hmph. **{0}** a une nouvelle passion, et ce n'est pas moi. C'est {1}. Je ne suis pas vexée. Je suis juste très silencieuse >:(",
            "**{0}** a déjà prévu d'en parler à tous les Plynlings du coin. Le sujet : {1}. Le coin n'est pas prêt.",
            "Un cri de joie : **{0}** vient de trouver sa passion. Pour aujourd'hui : {1}. Pour les mois à venir : {1}, encore {1}, et toujours {1}.",
            "Je note dans mon carnet : **{0}**, deuxième passion, {1}. Je note aussi que le calme, c'était bien, avant (¬_¬)",
            "**{0}** te regarde avec des étoiles plein les yeux. Merci pour {1}. Il ne l'oubliera jamais, et ne te laissera pas l'oublier non plus ♡",
        },
        F: new[]
        {
            "**{0}** s'est prise de passion pour {1}. Je ne comprends pas, mais je respecte ♡",
            "Nouvelle obsession pour **{0}** : {1}. Prépare-toi à en entendre parler tous les jours (¬_¬)",
            $"**{{0}}** ne parle plus que de ça : {{1}}. Elle est adorable. Un peu fatigante. Adorable {Emotes.Sparkle}",
            "C'est officiel : **{0}** a une nouvelle passion. Elle s'appelle {1}, et elle a déjà pris toute la place (˶ᵔ ᵕ ᵔ˶)",
            "Tu viens d'apprendre une passion à **{0}** : {1}. Elle a écouté, a hoché la tête, et ne pense plus qu'à ça depuis. Bravo. Vraiment.",
            "Hmph. **{0}** a une nouvelle passion, et ce n'est pas moi. C'est {1}. Je ne suis pas vexée. Je suis juste très silencieuse >:(",
            "**{0}** a déjà prévu d'en parler à tous les Plynlings du coin. Le sujet : {1}. Le coin n'est pas prêt.",
            "Un cri de joie : **{0}** vient de trouver sa passion. Pour aujourd'hui : {1}. Pour les mois à venir : {1}, encore {1}, et toujours {1}.",
            "Je note dans mon carnet : **{0}**, deuxième passion, {1}. Je note aussi que le calme, c'était bien, avant (¬_¬)",
            "**{0}** te regarde avec des étoiles plein les yeux. Merci pour {1}. Elle ne l'oubliera jamais, et ne te laissera pas l'oublier non plus ♡",
        });

    // The end of a /plynling play game, under the result. {0} = name. "Won" and "lost" are the
    // *player's*: when the player wins, the Plynling is delighted anyway; when it wins, it gloats.
    public static readonly GenderedLines PlynlingPlayPlayerWonLines = new(
        M: new[]
        {
            "**{0}** sautille partout : tu as gagné, et il est plus content que toi (˶ᵔ ᵕ ᵔ˶)",
            "« Encore ! » réclame **{0}**, qui vient de perdre et s'en fiche complètement.",
            "Tu as gagné. **{0}** fait semblant d'être vexé pendant une demi-seconde, puis sourit jusqu'aux oreilles.",
            $"Victoire ! **{{0}}** t'applaudit de ses petits bras. Moi, j'applaudis mentalement. C'est plus discret, et plus digne {Emotes.Sparkle}",
            "Bravo. Je le dis une fois. **{0}**, lui, le dit dix fois, en courant autour de toi ♡",
            "**{0}** te saute dans les bras. Aucune fierté, aucune rancune, rien que de la joie. Je ne sais pas faire ça (╥﹏╥)",
            "Ta victoire, sa fête : **{0}** danse la danse que tu aurais dû danser, et connaît les pas mieux que toi.",
            "Hmph. Je ne dirai pas que c'était bien joué. … C'était bien joué. **{0}** le pense aussi. Ne le répétez pas (¬_¬)",
            "**{0}** jure de t'avoir laissé gagner. Il ment très mal, et sourit trop pour être crédible (˶˃ ᵕ ˂˶)",
            "Point pour toi. **{0}** te donne un petit coup de tête pour te féliciter : chez les Plynlings, c'est la médaille d'or.",
            $"**{{0}}** fait trois tours sur lui-même de joie, et tombe. Tu as gagné, il a perdu l'équilibre : chacun sa part {Emotes.Sparkle}",
            "Une victoire pour toi. **{0}** s'en fiche un peu : il veut surtout un câlin, et le réclame en tendant les bras ♡",
            "Bon. Tu as gagné. **{0}** est ravi, donc je suis obligée de l'être aussi. Règle numéro un de la maison >:(",
            "« Bravo ! Bravo ! » **{0}** le crie à tout le monde, même à ceux qui n'avaient rien demandé.",
            "Ne t'emballe pas : tu as gagné à un jeu de Plynling. Mais **{0}** te regarde comme si tu avais sauvé le monde, alors emballe-toi un peu (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** est tellement heureux pour toi qu'il en oublie d'avoir perdu. Profites-en : ça ne durera pas.",
            "Des étoiles plein les yeux, **{0}** te fixe. Si on me demande, je n'ai rien vu, et je ne suis pas fière de toi. Je suis fière de toi.",
            "Clin d'œil complice de **{0}** : il gardera ta victoire secrète. Moi, je la raconte tout de suite, à tout le monde (¬_¬)",
        },
        F: new[]
        {
            "**{0}** sautille partout : tu as gagné, et elle est plus contente que toi (˶ᵔ ᵕ ᵔ˶)",
            "« Encore ! » réclame **{0}**, qui vient de perdre et s'en fiche complètement.",
            "Tu as gagné. **{0}** fait semblant d'être vexée pendant une demi-seconde, puis sourit jusqu'aux oreilles.",
            $"Victoire ! **{{0}}** t'applaudit de ses petits bras. Moi, j'applaudis mentalement. C'est plus discret, et plus digne {Emotes.Sparkle}",
            "Bravo. Je le dis une fois. **{0}**, elle, le dit dix fois, en courant autour de toi ♡",
            "**{0}** te saute dans les bras. Aucune fierté, aucune rancune, rien que de la joie. Je ne sais pas faire ça (╥﹏╥)",
            "Ta victoire, sa fête : **{0}** danse la danse que tu aurais dû danser, et connaît les pas mieux que toi.",
            "Hmph. Je ne dirai pas que c'était bien joué. … C'était bien joué. **{0}** le pense aussi. Ne le répétez pas (¬_¬)",
            "**{0}** jure de t'avoir laissé gagner. Elle ment très mal, et sourit trop pour être crédible (˶˃ ᵕ ˂˶)",
            "Point pour toi. **{0}** te donne un petit coup de tête pour te féliciter : chez les Plynlings, c'est la médaille d'or.",
            $"**{{0}}** fait trois tours sur elle-même de joie, et tombe. Tu as gagné, elle a perdu l'équilibre : chacun sa part {Emotes.Sparkle}",
            "Une victoire pour toi. **{0}** s'en fiche un peu : elle veut surtout un câlin, et le réclame en tendant les bras ♡",
            "Bon. Tu as gagné. **{0}** est ravie, donc je suis obligée de l'être aussi. Règle numéro un de la maison >:(",
            "« Bravo ! Bravo ! » **{0}** le crie à tout le monde, même à ceux qui n'avaient rien demandé.",
            "Ne t'emballe pas : tu as gagné à un jeu de Plynling. Mais **{0}** te regarde comme si tu avais sauvé le monde, alors emballe-toi un peu (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** est tellement heureuse pour toi qu'elle en oublie d'avoir perdu. Profites-en : ça ne durera pas.",
            "Des étoiles plein les yeux, **{0}** te fixe. Si on me demande, je n'ai rien vu, et je ne suis pas fière de toi. Je suis fière de toi.",
            "Clin d'œil complice de **{0}** : elle gardera ta victoire secrète. Moi, je la raconte tout de suite, à tout le monde (¬_¬)",
        });

    public static readonly GenderedLines PlynlingPlayPlayerLostLines = new(
        M: new[]
        {
            "**{0}** a gagné, et ne compte pas te laisser l'oublier. Jamais. Même dans dix ans (¬_¬)",
            $"« Trop facile ! » se vante **{{0}}**. Je ne confirme pas. Je ne démens pas non plus {Emotes.Sparkle}",
            "Perdu ! **{0}** te tire la langue. Moi aussi, en esprit (˶˃ ᵕ ˂˶)",
            "**{0}** te regarde de haut, ce qui est un exploit, vu sa taille.",
            "Défaite ! **{0}** fait la fête, toi la tête. Je regarde les deux, et c'est le meilleur spectacle de la semaine ♡",
            "Tableau des scores : **{0}**, un. Toi, zéro. Je l'ai affiché dans ma tête, en très gros.",
            "**{0}** te tapote la main, plein de compassion. C'est humiliant. C'est adorable. Je me tords de rire (˶ᵔ ᵕ ᵔ˶)",
            "« C'était un échauffement ? » demande **{0}**, l'air parfaitement innocent. Personne n'est innocent à ce point.",
            $"Revanche ? **{{0}}** a dit oui avant même que tu demandes. Il a déjà hâte de gagner encore {Emotes.Sparkle}",
            "**{0}** a gagné, et proprement. Ce n'est pas drôle. Hihi. Pardon (˶˃ ᵕ ˂˶)",
            "**{0}** se pavane, le menton en l'air, et va raconter ça à tous les Plynlings du coin, avec des détails en plus.",
            "Tu as perdu contre un Plynling. Je ne juge pas. **{0}**, si. Beaucoup (¬_¬)",
            "Tu as perdu. Cheh. UwU",
            "Victoire pour **{0}**, qui exige maintenant des applaudissements. Beaucoup. Longtemps. 👏",
            "Et **{0}** prépare déjà la prochaine partie en se frottant les mains. Tu devrais t'entraîner. Sérieusement >:(",
            "Console-toi : **{0}** gagne contre presque tout le monde. Contre moi, jamais. Je ne joue pas, mais ça compte quand même ♡",
            "Et un point pour **{0}**, qui fait le tour de la pièce, les bras levés, sous ses propres applaudissements.",
            "Je te propose un marché : tu ne dis à personne que tu as perdu, et je ne dis à personne que **{0}** a un tout petit peu triché. Marché conclu ? ♡",
        },
        F: new[]
        {
            "**{0}** a gagné, et ne compte pas te laisser l'oublier. Jamais. Même dans dix ans (¬_¬)",
            $"« Trop facile ! » se vante **{{0}}**. Je ne confirme pas. Je ne démens pas non plus {Emotes.Sparkle}",
            "Perdu ! **{0}** te tire la langue. Moi aussi, en esprit (˶˃ ᵕ ˂˶)",
            "**{0}** te regarde de haut, ce qui est un exploit, vu sa taille.",
            "Défaite ! **{0}** fait la fête, toi la tête. Je regarde les deux, et c'est le meilleur spectacle de la semaine ♡",
            "Tableau des scores : **{0}**, un. Toi, zéro. Je l'ai affiché dans ma tête, en très gros.",
            "**{0}** te tapote la main, pleine de compassion. C'est humiliant. C'est adorable. Je me tords de rire (˶ᵔ ᵕ ᵔ˶)",
            "« C'était un échauffement ? » demande **{0}**, l'air parfaitement innocente. Personne n'est innocent à ce point.",
            $"Revanche ? **{{0}}** a dit oui avant même que tu demandes. Elle a déjà hâte de gagner encore {Emotes.Sparkle}",
            "**{0}** a gagné, et proprement. Ce n'est pas drôle. Hihi. Pardon (˶˃ ᵕ ˂˶)",
            "**{0}** se pavane, le menton en l'air, et va raconter ça à tous les Plynlings du coin, avec des détails en plus.",
            "Tu as perdu contre un Plynling. Je ne juge pas. **{0}**, si. Beaucoup (¬_¬)",
            "Tu as perdu. Cheh. UwU",
            "Victoire pour **{0}**, qui exige maintenant des applaudissements. Beaucoup. Longtemps. 👏",
            "Et **{0}** prépare déjà la prochaine partie en se frottant les mains. Tu devrais t'entraîner. Sérieusement >:(",
            "Console-toi : **{0}** gagne contre presque tout le monde. Contre moi, jamais. Je ne joue pas, mais ça compte quand même ♡",
            "Et un point pour **{0}**, qui fait le tour de la pièce, les bras levés, sous ses propres applaudissements.",
            "Je te propose un marché : tu ne dis à personne que tu as perdu, et je ne dis à personne que **{0}** a un tout petit peu triché. Marché conclu ? ♡",
        });

    // /plynling visit, the knock: {0} = the visitor's name, {1} = the invited owner's mention
    // (this one pings them — it is an invitation).
    public static readonly GenderedLines PlynlingVisitKnockLines = new(
        M: new[]
        {
            "Toc toc ! **{0}** est devant chez {1}. Il a répété son bonjour tout le long du chemin, et l'oublie en ce moment même (╥﹏╥)",
            "{1} ! On frappe : c'est **{0}**, avec son plus beau sourire. Ne le fais pas attendre. Je déteste attendre, moi aussi >:(",
            "« Il y a quelqu'un ? » demande **{0}** devant chez {1}. Il y a quelqu'un. Ouvre, qu'on en finisse avec ce suspense.",
            "Ding dong. Livraison spéciale pour {1} : **{0}**, sourire compris. Signature obligatoire (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** a apporté un petit caillou pour le Plynling de {1}. Je l'ai vu. Il est très joli. Plus joli que ceux qu'on m'offre, c'est-à-dire aucun.",
            "Invitation officielle : **{0}** voudrait passer un moment chez {1}. Tu dis oui, sinon je boude à ta place, et je boude très bien >:(",
            "Toc, toc-toc, toc. **{0}** frappe chez {1} sur un rythme secret. Un code ? Personne ne me l'a donné. Vexée, et un peu intriguée (¬_¬)",
            "{1}, tu as de la visite. Je te préviens : **{0}** s'est regardé dans trois flaques avant de frapper. Fais-lui un compliment.",
            "Chut. **{0}** frappe tout doucement chez {1}, pour ne déranger personne. Une délicatesse pareille, je ne sais pas d'où ça sort (╥﹏╥)",
            "Un visiteur ! **{0}** attend devant chez {1} en se balançant d'un pied sur l'autre.",
            $"Tu entends ce petit bruit contre la vitre ? C'est **{{0}}**, le nez collé à la fenêtre de {{1}}. Ouvre avant qu'il dessine dans la buée {Emotes.Sparkle}",
            "Alerte : **{0}** approche de chez {1} avec un petit sac à dos rempli de provisions. Il compte rester. Longtemps.",
            "**{0}** a mis un petit nœud pour venir chez {1}. Il dit que c'est « pas exprès ». C'est exprès. Tu fais semblant de le croire, d'accord ? ♡",
            "Je ne dis pas que **{0}** attend devant chez {1} depuis une éternité. Je dis que c'est long, une éternité, et que tu pourrais ouvrir (¬_¬)",
            "Hé, {1}. Quelqu'un rôde devant chez toi : c'est **{0}**, caché derrière un buisson. Le camouflage est raté. L'intention est adorable.",
            "Personne ne frappe jamais chez moi. **{0}**, lui, frappe chez {1}. Tu as de la chance. Ouvre, et ne me regarde pas comme ça (╥﹏╥)",
            $"Un petit mot glissé sous la porte de {{1}} : « C'est **{{0}}**, je peux entrer ? » Je l'ai lu avant toi. Aucune honte {Emotes.Sparkle}",
            "**{0}** arrive chez {1}, essoufflé d'avoir couru tout le chemin. D'abord un verre d'eau. Ensuite « Accueillir ». Dans cet ordre (˶˃ ᵕ ˂˶)",
            "Toc toc toc… toc ? Devant chez {1}, **{0}** a perdu le fil de ses propres coups. Silence gêné. On l'aime quand même.",
            "{1}, c'est ton moment : **{0}** attend ta réponse. Un « Accueillir », et tout le quartier sourit. Un refus, et je te fais les gros yeux pendant une semaine >:(",
            $"**{{0}}** a apporté un panier à partager avec le Plynling de {{1}}. Il en a mangé la moitié en route. L'autre moitié est pour vous, promis {Emotes.Sparkle}",
            "Visite surprise ! **{0}** se tient bien droit devant chez {1}, en essayant d'avoir l'air sage.",
            "{1}, je te préviens : **{0}** est très poli. Il va dire bonjour, merci, et « c'est joli chez toi ». Prépare-toi à fondre ♡",
            $"Tiens, tiens. **{{0}}** est devant chez {{1}}, et je sens que ça va être une bonne journée. Je le sens toujours. Presque toujours {Emotes.Sparkle}",
            "Toc toc ! C'est **{0}**. Pas un livreur, pas moi : une visite pour le Plynling de {1}. Ouvre, on n'attend que toi.",
            "**{0}** frappe chez {1} avec tellement d'enthousiasme que la porte en tremble. « Accueillir », vite, avant qu'elle se décroche (╥﹏╥)",
            "Nouvelle du jour : **{0}** vient voir le Plynling de {1}, et a promis d'être sage. J'ai noté la promesse. Je la relirai à la fin de la visite (¬_¬)",
            "Ohé ! **{0}** fait de grands signes devant chez {1}. Si personne ne répond, il passera aux petits sauts. Personne n'est prêt pour les petits sauts.",
            "Il y a des visites qu'on refuse. Celle de **{0}** chez {1} n'en fait pas partie. Je ne donne pas de conseils, je donne des ordres ♡",
            "Le Plynling de {1} a de la visite : **{0}**, qui a ciré son plus beau caillou pour l'occasion. Ça brille. Tu vas plisser les yeux.",
        },
        F: new[]
        {
            "Toc toc ! **{0}** est devant chez {1}. Elle a répété son bonjour tout le long du chemin, et l'oublie en ce moment même (╥﹏╥)",
            "{1} ! On frappe : c'est **{0}**, avec son plus beau sourire. Ne la fais pas attendre. Je déteste attendre, moi aussi >:(",
            "« Il y a quelqu'un ? » demande **{0}** devant chez {1}. Il y a quelqu'un. Ouvre, qu'on en finisse avec ce suspense.",
            "Ding dong. Livraison spéciale pour {1} : **{0}**, sourire compris. Signature obligatoire (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** a apporté un petit caillou pour le Plynling de {1}. Je l'ai vu. Il est très joli. Plus joli que ceux qu'on m'offre, c'est-à-dire aucun.",
            "Invitation officielle : **{0}** voudrait passer un moment chez {1}. Tu dis oui, sinon je boude à ta place, et je boude très bien >:(",
            "Toc, toc-toc, toc. **{0}** frappe chez {1} sur un rythme secret. Un code ? Personne ne me l'a donné. Vexée, et un peu intriguée (¬_¬)",
            "{1}, tu as de la visite. Je te préviens : **{0}** s'est regardée dans trois flaques avant de frapper. Fais-lui un compliment.",
            "Chut. **{0}** frappe tout doucement chez {1}, pour ne déranger personne. Une délicatesse pareille, je ne sais pas d'où ça sort (╥﹏╥)",
            "Une visiteuse ! **{0}** attend devant chez {1} en se balançant d'un pied sur l'autre.",
            $"Tu entends ce petit bruit contre la vitre ? C'est **{{0}}**, le nez collé à la fenêtre de {{1}}. Ouvre avant qu'elle dessine dans la buée {Emotes.Sparkle}",
            "Alerte : **{0}** approche de chez {1} avec un petit sac à dos rempli de provisions. Elle compte rester. Longtemps.",
            "**{0}** a mis un petit nœud pour venir chez {1}. Elle dit que c'est « pas exprès ». C'est exprès. Tu fais semblant de la croire, d'accord ? ♡",
            "Je ne dis pas que **{0}** attend devant chez {1} depuis une éternité. Je dis que c'est long, une éternité, et que tu pourrais ouvrir (¬_¬)",
            "Hé, {1}. Quelqu'un rôde devant chez toi : c'est **{0}**, cachée derrière un buisson. Le camouflage est raté. L'intention est adorable.",
            "Personne ne frappe jamais chez moi. **{0}**, elle, frappe chez {1}. Tu as de la chance. Ouvre, et ne me regarde pas comme ça (╥﹏╥)",
            $"Un petit mot glissé sous la porte de {{1}} : « C'est **{{0}}**, je peux entrer ? » Je l'ai lu avant toi. Aucune honte {Emotes.Sparkle}",
            "**{0}** arrive chez {1}, essoufflée d'avoir couru tout le chemin. D'abord un verre d'eau. Ensuite « Accueillir ». Dans cet ordre (˶˃ ᵕ ˂˶)",
            "Toc toc toc… toc ? Devant chez {1}, **{0}** a perdu le fil de ses propres coups. Silence gêné. On l'aime quand même.",
            "{1}, c'est ton moment : **{0}** attend ta réponse. Un « Accueillir », et tout le quartier sourit. Un refus, et je te fais les gros yeux pendant une semaine >:(",
            $"**{{0}}** a apporté un panier à partager avec le Plynling de {{1}}. Elle en a mangé la moitié en route. L'autre moitié est pour vous, promis {Emotes.Sparkle}",
            "Visite surprise ! **{0}** se tient bien droite devant chez {1}, en essayant d'avoir l'air sage.",
            "{1}, je te préviens : **{0}** est très polie. Elle va dire bonjour, merci, et « c'est joli chez toi ». Prépare-toi à fondre ♡",
            $"Tiens, tiens. **{{0}}** est devant chez {{1}}, et je sens que ça va être une bonne journée. Je le sens toujours. Presque toujours {Emotes.Sparkle}",
            "Toc toc ! C'est **{0}**. Pas une livreuse, pas moi : une visite pour le Plynling de {1}. Ouvre, on n'attend que toi.",
            "**{0}** frappe chez {1} avec tellement d'enthousiasme que la porte en tremble. « Accueillir », vite, avant qu'elle se décroche (╥﹏╥)",
            "Nouvelle du jour : **{0}** vient voir le Plynling de {1}, et a promis d'être sage. J'ai noté la promesse. Je la relirai à la fin de la visite (¬_¬)",
            "Ohé ! **{0}** fait de grands signes devant chez {1}. Si personne ne répond, elle passera aux petits sauts. Personne n'est prêt pour les petits sauts.",
            "Il y a des visites qu'on refuse. Celle de **{0}** chez {1} n'en fait pas partie. Je ne donne pas de conseils, je donne des ordres ♡",
            "Le Plynling de {1} a de la visite : **{0}**, qui a ciré son plus beau caillou pour l'occasion. Ça brille. Tu vas plisser les yeux.",
        });

    // PlynlingPetLines' Tomodachi-style twin, for a Plynling with a typed passion (see PlynlingPassions.PickLines). {0} = name, {1} = the typed passion, « … ».
    public static readonly GenderedLines PlynlingPetTypedLines = new(
        M: new[]
        {
            "Sous la caresse, **{0}** murmure un mot, les yeux fermés. J'ai tendu l'oreille : {1}. Évidemment ♡",
            "Tu caresses **{0}**, et il se met à te réciter tout ce qu'il sait sur sa passion : {1}. Tu as déclenché quelque chose (˶˃ ᵕ ˂˶)",
            "En pleine caresse, **{0}** s'écrie : {1} ! Aucun rapport. Il avait juste besoin de le dire.",
            $"Je crois que **{{0}}** ronronne en rythme. Le rythme de sa passion. Oui, {{1}} a un rythme, apparemment {Emotes.Sparkle}",
            "**{0}** se laisse caresser en rêvassant. Je parie qu'il rêve de sa passion : {1}. Il rêve toujours de ça.",
            "Tu viens de gagner le droit d'écouter **{0}** parler de sa passion. Sujet : {1}. Durée estimée : longtemps (¬_¬)",
            "Hmph. Une caresse de toi, et **{0}** en oublie sa passion pendant trois secondes. Même {1} ne résiste pas à ta main. Impressionnant >:(",
            "Petit secret : quand on caresse **{0}** derrière la joue, il pense à sa passion : {1}. Toujours. J'ai fait des tests ♡",
            "**{0}** te regarde, heureux, et te tend un dessin. Encore sa passion : {1}. Il y en a quarante, à la maison.",
            "Caresse validée. D'après **{0}**, c'est la deuxième meilleure chose au monde. La première : {1}. Ne le prends pas mal (˶ᵔ ᵕ ᵔ˶)",
            "Tu sais ce qui rendrait **{0}** encore plus heureux ? Que tu t'intéresses enfin à sa passion : {1}. Indice : il attend ça depuis longtemps.",
            $"Ta main, sa passion ({{1}}) et une sieste : la journée parfaite selon **{{0}}**. Tu viens de cocher la première case {Emotes.Sparkle}",
        },
        F: new[]
        {
            "Sous la caresse, **{0}** murmure un mot, les yeux fermés. J'ai tendu l'oreille : {1}. Évidemment ♡",
            "Tu caresses **{0}**, et elle se met à te réciter tout ce qu'elle sait sur sa passion : {1}. Tu as déclenché quelque chose (˶˃ ᵕ ˂˶)",
            "En pleine caresse, **{0}** s'écrie : {1} ! Aucun rapport. Elle avait juste besoin de le dire.",
            $"Je crois que **{{0}}** ronronne en rythme. Le rythme de sa passion. Oui, {{1}} a un rythme, apparemment {Emotes.Sparkle}",
            "**{0}** se laisse caresser en rêvassant. Je parie qu'elle rêve de sa passion : {1}. Elle rêve toujours de ça.",
            "Tu viens de gagner le droit d'écouter **{0}** parler de sa passion. Sujet : {1}. Durée estimée : longtemps (¬_¬)",
            "Hmph. Une caresse de toi, et **{0}** en oublie sa passion pendant trois secondes. Même {1} ne résiste pas à ta main. Impressionnant >:(",
            "Petit secret : quand on caresse **{0}** derrière la joue, elle pense à sa passion : {1}. Toujours. J'ai fait des tests ♡",
            "**{0}** te regarde, heureuse, et te tend un dessin. Encore sa passion : {1}. Il y en a quarante, à la maison.",
            "Caresse validée. D'après **{0}**, c'est la deuxième meilleure chose au monde. La première : {1}. Ne le prends pas mal (˶ᵔ ᵕ ᵔ˶)",
            "Tu sais ce qui rendrait **{0}** encore plus heureuse ? Que tu t'intéresses enfin à sa passion : {1}. Indice : elle attend ça depuis longtemps.",
            $"Ta main, sa passion ({{1}}) et une sieste : la journée parfaite selon **{{0}}**. Tu viens de cocher la première case {Emotes.Sparkle}",
        });

    // PlynlingFeedLines' twin: {0} = name, {1} = the food with its article, {2} = the typed passion.
    public static readonly GenderedLines PlynlingFeedTypedLines = new(
        M: new[]
        {
            "**{0}** mange {1} en te racontant sa passion entre deux bouchées. Sujet : {2}. Rien n'a été compris, tout a été mangé (˶˃ ᵕ ˂˶)",
            "Tu donnes {1} à **{0}**. Verdict : délicieux, mais ce n'est pas {2}. Rien n'est jamais {2}.",
            "**{0}** a englouti {1} en deux secondes : il fallait vite retourner à sa passion. Elle n'attend pas, {2} >:(",
            $"Miam. Et maintenant, **{{0}}** a assez d'énergie pour sa passion : {{2}}. Tu viens de financer ça. Bravo {Emotes.Sparkle}",
            "**{0}** range {1} dans l'assiette en forme de… je crois que c'est sa passion : {2}. Ça ne ressemble à rien. Il est très fier.",
            "Entre deux bouchées, **{0}** te demande si tu connais {2}. Tu connais. Tout le monde connaît, depuis le temps (¬_¬)",
            "Un repas, un petit rot discret, et **{0}** repart à fond sur sa passion : {2}. Tu n'as pas eu droit à un merci. Tu as eu droit à un exposé.",
            "Je vais te dire un secret : **{0}** mange plus vite quand il pense à sa passion ({2}). Et il y pense tout le temps ♡",
            "Tu nourris **{0}** avec {1}, et il te nourrit de sa passion : {2}. Échange équitable. Enfin, presque.",
            "**{0}** a gardé une miette pour plus tard. Pour sa passion, m'a-t-il expliqué : {2}. Je n'ai pas demandé de précisions (╥﹏╥)",
        },
        F: new[]
        {
            "**{0}** mange {1} en te racontant sa passion entre deux bouchées. Sujet : {2}. Rien n'a été compris, tout a été mangé (˶˃ ᵕ ˂˶)",
            "Tu donnes {1} à **{0}**. Verdict : délicieux, mais ce n'est pas {2}. Rien n'est jamais {2}.",
            "**{0}** a englouti {1} en deux secondes : il fallait vite retourner à sa passion. Elle n'attend pas, {2} >:(",
            $"Miam. Et maintenant, **{{0}}** a assez d'énergie pour sa passion : {{2}}. Tu viens de financer ça. Bravo {Emotes.Sparkle}",
            "**{0}** range {1} dans l'assiette en forme de… je crois que c'est sa passion : {2}. Ça ne ressemble à rien. Elle est très fière.",
            "Entre deux bouchées, **{0}** te demande si tu connais {2}. Tu connais. Tout le monde connaît, depuis le temps (¬_¬)",
            "Un repas, un petit rot discret, et **{0}** repart à fond sur sa passion : {2}. Tu n'as pas eu droit à un merci. Tu as eu droit à un exposé.",
            "Je vais te dire un secret : **{0}** mange plus vite quand elle pense à sa passion ({2}). Et elle y pense tout le temps ♡",
            "Tu nourris **{0}** avec {1}, et elle te nourrit de sa passion : {2}. Échange équitable. Enfin, presque.",
            "**{0}** a gardé une miette pour plus tard. Pour sa passion, m'a-t-elle expliqué : {2}. Je n'ai pas demandé de précisions (╥﹏╥)",
        });

    // PlynlingVisitKnockLines' twin: {0} = the visitor, {1} = the invited owner's mention, {2} = the visitor's typed passion.
    public static readonly GenderedLines PlynlingVisitKnockTypedLines = new(
        M: new[]
        {
            "Toc toc ! **{0}** attend devant chez {1}, et a déjà préparé le sujet de conversation : {2}. Tu n'y échapperas pas ♡",
            "{1}, ouvre : **{0}** a apporté un exposé complet sur sa passion. Titre : {2}. Il y a des illustrations (˶˃ ᵕ ˂˶)",
            "**{0}** frappe chez {1} en fredonnant l'hymne de sa passion : {2}. C'est faux, c'est fort, c'est sincère.",
            $"Une visite pour le Plynling de {{1}} ! **{{0}}** a écrit sa passion sur un petit carton, pour les présentations : {{2}}. Très organisé {Emotes.Sparkle}",
            "Je te préviens, {1} : si tu ouvres à **{0}**, tu vas tout savoir sur sa passion : {2}. Tout. Absolument tout (¬_¬)",
            "**{0}** est devant chez {1}, avec un cadeau emballé. Je parie que c'est en rapport avec sa passion : {2}. Je parie toujours juste.",
            "Toc, toc… et un cri : {2} ! **{0}** a frappé chez {1}, puis crié sa passion, pour être sûr qu'on le reconnaisse. Ça marche (˶ᵔ ᵕ ᵔ˶)",
            "{1}, tu as de la visite : **{0}**, et sa passion, qui vient toujours avec : {2}. Ouvre aux deux >:(",
        },
        F: new[]
        {
            "Toc toc ! **{0}** attend devant chez {1}, et a déjà préparé le sujet de conversation : {2}. Tu n'y échapperas pas ♡",
            "{1}, ouvre : **{0}** a apporté un exposé complet sur sa passion. Titre : {2}. Il y a des illustrations (˶˃ ᵕ ˂˶)",
            "**{0}** frappe chez {1} en fredonnant l'hymne de sa passion : {2}. C'est faux, c'est fort, c'est sincère.",
            $"Une visite pour le Plynling de {{1}} ! **{{0}}** a écrit sa passion sur un petit carton, pour les présentations : {{2}}. Très organisée {Emotes.Sparkle}",
            "Je te préviens, {1} : si tu ouvres à **{0}**, tu vas tout savoir sur sa passion : {2}. Tout. Absolument tout (¬_¬)",
            "**{0}** est devant chez {1}, avec un cadeau emballé. Je parie que c'est en rapport avec sa passion : {2}. Je parie toujours juste.",
            "Toc, toc… et un cri : {2} ! **{0}** a frappé chez {1}, puis crié sa passion, pour être sûre qu'on la reconnaisse. Ça marche (˶ᵔ ᵕ ᵔ˶)",
            "{1}, tu as de la visite : **{0}**, et sa passion, qui vient toujours avec : {2}. Ouvre aux deux >:(",
        });

    // The thought bubble on /plynling view (see PlynlingPassions.Thought): what it is thinking about,
    // Tomodachi-style. {0} = name, {1} = its typed passion. PlynlingDreamTypedLines is the same while it sleeps.
    public static readonly GenderedLines PlynlingThoughtTypedLines = new(
        M: new[]
        {
            "💭 **{0}** regarde dans le vide, l'air très sérieux. Dans sa tête, une seule chose : {1}.",
            "💭 *Et si on en reparlait un petit peu…* Non. **{0}** a promis d'arrêter. Mais {1}, quand même.",
            "💭 **{0}** soupire. Il pense à sa passion : {1}. Encore. Toujours (˶ᵔ ᵕ ᵔ˶)",
            "💭 **{0}** griffonne quelque chose dans la poussière. Tu te penches : c'est {1}, avec un cœur autour.",
            "💭 *J'aimerais bien qu'on me pose des questions sur {1}.* **{0}** te regarde. Longuement.",
            "💭 Tu n'as pas encore ouvert la bouche que **{0}** te demande si toi aussi, tu aimes {1}. Réponds bien.",
            "💭 **{0}** compte sur ses doigts. Une semaine n'a pas assez de jours pour {1}, il en est sûr.",
            "💭 Si tu te demandais : oui, **{0}** a encore {1} en tête. Non, je ne suis pas jalouse (¬_¬)",
            "💭 **{0}** fredonne un air de sa composition. Dans les paroles, surtout : {1}.",
            "💭 Petit rêve éveillé : **{0}** et toi, une journée entière, rien que pour {1}. Il a déjà fait le programme ♡",
        },
        F: new[]
        {
            "💭 **{0}** regarde dans le vide, l'air très sérieux. Dans sa tête, une seule chose : {1}.",
            "💭 *Et si on en reparlait un petit peu…* Non. **{0}** a promis d'arrêter. Mais {1}, quand même.",
            "💭 **{0}** soupire. Elle pense à sa passion : {1}. Encore. Toujours (˶ᵔ ᵕ ᵔ˶)",
            "💭 **{0}** griffonne quelque chose dans la poussière. Tu te penches : c'est {1}, avec un cœur autour.",
            "💭 *J'aimerais bien qu'on me pose des questions sur {1}.* **{0}** te regarde. Longuement.",
            "💭 Tu n'as pas encore ouvert la bouche que **{0}** te demande si toi aussi, tu aimes {1}. Réponds bien.",
            "💭 **{0}** compte sur ses doigts. Une semaine n'a pas assez de jours pour {1}, elle en est sûre.",
            "💭 Si tu te demandais : oui, **{0}** a encore {1} en tête. Non, je ne suis pas jalouse (¬_¬)",
            "💭 **{0}** fredonne un air de sa composition. Dans les paroles, surtout : {1}.",
            "💭 Petit rêve éveillé : **{0}** et toi, une journée entière, rien que pour {1}. Elle a déjà fait le programme ♡",
        });

    public static readonly GenderedLines PlynlingDreamTypedLines = new(
        M: new[]
        {
            "💤 **{0}** dort à poings fermés. Au-dessus de sa tête, une toute petite bulle : {1}.",
            "💤 **{0}** parle en dormant. Un « mmh… », puis, très distinctement : {1}. Puis plus rien.",
            "💤 Chut. **{0}** rêve de sa passion : {1}. Ne le réveille pas, il en est au meilleur moment.",
            "💤 **{0}** gigote dans son sommeil. Dans son rêve, {1} prend une place énorme. Littéralement.",
            "💤 **{0}** sourit en dormant. Je parie mes circuits que dans ce rêve, on trouve {1} (˶ᵔ ᵕ ᵔ˶)",
            "💤 **{0}** dort, et marmonne quelque chose sur {1}. Je note, pour le dossier (¬_¬)",
        },
        F: new[]
        {
            "💤 **{0}** dort à poings fermés. Au-dessus de sa tête, une toute petite bulle : {1}.",
            "💤 **{0}** parle en dormant. Un « mmh… », puis, très distinctement : {1}. Puis plus rien.",
            "💤 Chut. **{0}** rêve de sa passion : {1}. Ne la réveille pas, elle en est au meilleur moment.",
            "💤 **{0}** gigote dans son sommeil. Dans son rêve, {1} prend une place énorme. Littéralement.",
            "💤 **{0}** sourit en dormant. Je parie mes circuits que dans ce rêve, on trouve {1} (˶ᵔ ᵕ ᵔ˶)",
            "💤 **{0}** dort, et marmonne quelque chose sur {1}. Je note, pour le dossier (¬_¬)",
        });

    // DM'd once when it falls sick (the hourly sweep). {0} = name.
    public static readonly GenderedLines PlynlingSickWarningLines = new(
        M: new[]
        {
            "🤒 **{0}** ne va pas bien du tout : il est malade. Un médicament par jour, et vite (╥﹏╥)",
            "Alerte : **{0}** est tombé malade. `/inventory medicine`, puis « Soigner » sur sa carte. Tous les jours, hein.",
            "**{0}** a de la fièvre. Je ne panique pas. Je te préviens, c'est tout. Soigne-le chaque jour, d'accord ?",
            "Petit message pour te dire que **{0}** est malade. Un médicament par jour et ça passera. Sans… je préfère ne pas y penser.",
            "**{0}** tousse, renifle et fait une tête de salade fanée. Malade. Soigne-le, je t'en supplie ♡",
            "Mauvaise nouvelle : **{0}** est malade. Bonne nouvelle : ça se soigne. Mais seulement si tu le fais (¬_¬)",
        },
        F: new[]
        {
            "🤒 **{0}** ne va pas bien du tout : elle est malade. Un médicament par jour, et vite (╥﹏╥)",
            "Alerte : **{0}** est tombée malade. `/inventory medicine`, puis « Soigner » sur sa carte. Tous les jours, hein.",
            "**{0}** a de la fièvre. Je ne panique pas. Je te préviens, c'est tout. Soigne-la chaque jour, d'accord ?",
            "Petit message pour te dire que **{0}** est malade. Un médicament par jour et ça passera. Sans… je préfère ne pas y penser.",
            "**{0}** tousse, renifle et fait une tête de salade fanée. Malade. Soigne-la, je t'en supplie ♡",
            "Mauvaise nouvelle : **{0}** est malade. Bonne nouvelle : ça se soigne. Mais seulement si tu le fais (¬_¬)",
        });

    // The single DM about three hours before death. {0} = name. Deliberately no countdown —
    // the owner removed the "mourra dans …" timestamp from here and from the card alike, so
    // the lines say "soon" and never name a time.
    public static readonly GenderedLines PlynlingWarningLines = new(
        M: new[]
        {
            "**{0}** a terriblement faim… il ne tiendra plus longtemps si personne ne le nourrit. `/plynling view`, vite ! Je ne dis pas que c'est ta faute. Je le pense très fort, c'est tout (¬_¬)",
            "Ton Plynling **{0}** va bientôt mourir de faim. Il compte sur toi. Moi aussi. Ne nous fais pas ça, je t'en supplie (╥﹏╥)",
            "Psst… **{0}** est au bord de l'évanouissement. Il ne va pas tarder à s'effondrer. Ne l'abandonne pas (╥﹏╥) Sinon, je vais faire une scène. Une grosse.",
            "Hé, toi. Oui, toi. **{0}** a le ventre qui gargouille si fort que je l'entends d'ici. `/plynling view`, tout de suite, ou je m'énerve >:(",
            "**{0}** te fait dire qu'il a faim. Il a aussi dit que tu étais sa personne préférée, mais c'était avant. Fais-toi pardonner : nourris-le ♡",
            "Alerte niveau critique : **{0}** est presque vide. Côté nourriture, je veux dire. Le reste tient encore. Fais vite.",
            "Je ne voulais pas te déranger, mais **{0}** est en train de s'éteindre doucement. Il ne se plaindra pas. Moi, si. Regarde-le, vite : `/plynling view` (╥﹏╥)",
            "Tu as vu l'état de **{0}** ? Non ? Justement. Je te préviens gentiment, une seule fois. Après, je ne réponds plus de rien >:(",
            "**{0}** regarde son assiette vide depuis un moment. Il ne dit rien, mais ses yeux disent tout. Nourris-le avant qu'il ne fasse une bêtise (╥﹏╥)",
            "Bip bip bip. Alerte faim pour **{0}**. Je répète : alerte faim pour **{0}**. Ceci n'est pas un exercice. `/plynling view` maintenant. C'est encore possible. Pour l'instant (¬_¬)",
            "Ton petit **{0}** commence à voir des étoiles. Pas les jolies. Celles qu'on voit quand on n'a rien mangé depuis trop longtemps. Nourris-le.",
            "Petit rappel de ton assistante préférée : **{0}** a faim, et ça commence à se voir. Ne me fais pas envoyer un deuxième message. Je n'en envoie jamais deux. C'est le problème ♡",
            "**{0}** pense à toi très fort en ce moment. Surtout à ce que tu pourrais lui donner à manger. Il est poli, il n'ose pas demander. Moi si : `/plynling view` !",
            "Je serai brève : **{0}** a faim, tu es la seule personne qui puisse faire quelque chose, et je te regarde. `/plynling view`. Maintenant (¬_¬)",
            "Ton Plynling **{0}** est tout mou, tout pâle et tout silencieux. Il a besoin d'un vrai repas, pas d'une pensée gentille. Bouge.",
            $"Si **{{0}}** savait écrire, il t'enverrait un long message plein de fautes et de points d'exclamation. Il a faim. Je traduis : `/plynling view`, vite {Emotes.Sparkle}",
        },
        F: new[]
        {
            "**{0}** a terriblement faim… elle ne tiendra plus longtemps si personne ne la nourrit. `/plynling view`, vite ! Je ne dis pas que c'est ta faute. Je le pense très fort, c'est tout (¬_¬)",
            "Ta Plynling **{0}** va bientôt mourir de faim. Elle compte sur toi. Moi aussi. Ne nous fais pas ça, je t'en supplie (╥﹏╥)",
            "Psst… **{0}** est au bord de l'évanouissement. Elle ne va pas tarder à s'effondrer. Ne l'abandonne pas (╥﹏╥) Sinon, je vais faire une scène. Une grosse.",
            "Hé, toi. Oui, toi. **{0}** a le ventre qui gargouille si fort que je l'entends d'ici. `/plynling view`, tout de suite, ou je m'énerve >:(",
            "**{0}** te fait dire qu'elle a faim. Elle a aussi dit que tu étais sa personne préférée, mais c'était avant. Fais-toi pardonner : nourris-la ♡",
            "Alerte niveau critique : **{0}** est presque vide. Côté nourriture, je veux dire. Le reste tient encore. Fais vite.",
            "Je ne voulais pas te déranger, mais **{0}** est en train de s'éteindre doucement. Elle ne se plaindra pas. Moi, si. Regarde-la, vite : `/plynling view` (╥﹏╥)",
            "Tu as vu l'état de **{0}** ? Non ? Justement. Je te préviens gentiment, une seule fois. Après, je ne réponds plus de rien >:(",
            "**{0}** regarde son assiette vide depuis un moment. Elle ne dit rien, mais ses yeux disent tout. Nourris-la avant qu'elle ne fasse une bêtise (╥﹏╥)",
            "Bip bip bip. Alerte faim pour **{0}**. Je répète : alerte faim pour **{0}**. Ceci n'est pas un exercice. `/plynling view` maintenant. C'est encore possible. Pour l'instant (¬_¬)",
            "Ta petite **{0}** commence à voir des étoiles. Pas les jolies. Celles qu'on voit quand on n'a rien mangé depuis trop longtemps. Nourris-la.",
            "Petit rappel de ton assistante préférée : **{0}** a faim, et ça commence à se voir. Ne me fais pas envoyer un deuxième message. Je n'en envoie jamais deux. C'est le problème ♡",
            "**{0}** pense à toi très fort en ce moment. Surtout à ce que tu pourrais lui donner à manger. Elle est polie, elle n'ose pas demander. Moi si : `/plynling view` !",
            "Je serai brève : **{0}** a faim, tu es la seule personne qui puisse faire quelque chose, et je te regarde. `/plynling view`. Maintenant (¬_¬)",
            "Ta Plynling **{0}** est toute molle, toute pâle et toute silencieuse. Elle a besoin d'un vrai repas, pas d'une pensée gentille. Bouge.",
            $"Si **{{0}}** savait écrire, elle t'enverrait un long message plein de fautes et de points d'exclamation. Elle a faim. Je traduis : `/plynling view`, vite {Emotes.Sparkle}",
        });

    // An illness death, in the game channel like PlynlingDeathLines, same {0}–{3}.
    public static readonly GenderedLines PlynlingIllnessDeathLines = new(
        M: new[]
        {
            "🕯️ **{0}**, le Plynling de {1}, s'est éteint au petit matin, emporté par la maladie. {2} de vie. Il repose sous {3}. Je ne pleure pas (╥﹏╥)",
            "🕯️ La maladie a eu raison de **{0}**. {1}, un médicament par jour, c'était tout ce dont il avait besoin. Il dort sous {3}, maintenant >:(",
            "🕯️ **{0}** n'a pas guéri. {2} de vie, et puis {3}. Minute de silence. Et toi, {1}, tu réfléchis à ce que tu as fait.",
            "🕯️ Malade depuis trop longtemps, **{0}** ({1}) nous a quittés après {2}. Il repose sous {3}. Un bain, un médicament… je le note pour le prochain.",
            "🕯️ Silence au village : **{0}** est mort de maladie après {2}. {3} le garde désormais. {1}, prends soin du prochain (╥﹏╥)",
            "🕯️ **{0}** n'a pas passé la nuit. La fièvre a gagné. {2} de vie, compagnon de {1}, et maintenant {3}.",
        },
        F: new[]
        {
            "🕯️ **{0}**, la Plynling de {1}, s'est éteinte au petit matin, emportée par la maladie. {2} de vie. Elle repose sous {3}. Je ne pleure pas (╥﹏╥)",
            "🕯️ La maladie a eu raison de **{0}**. {1}, un médicament par jour, c'était tout ce dont elle avait besoin. Elle dort sous {3}, maintenant >:(",
            "🕯️ **{0}** n'a pas guéri. {2} de vie, et puis {3}. Minute de silence. Et toi, {1}, tu réfléchis à ce que tu as fait.",
            "🕯️ Malade depuis trop longtemps, **{0}** ({1}) nous a quittés après {2}. Elle repose sous {3}. Un bain, un médicament… je le note pour la prochaine.",
            "🕯️ Silence au village : **{0}** est morte de maladie après {2}. {3} la garde désormais. {1}, prends soin de la prochaine (╥﹏╥)",
            "🕯️ **{0}** n'a pas passé la nuit. La fièvre a gagné. {2} de vie, compagne de {1}, et maintenant {3}.",
        });

    // Posted publicly in the game channel, with the memorial as the picture.
    // {0} = name, {1} = owner mention (sent with pings off), {2} = time lived, {3} = memorial.
    public static readonly GenderedLines PlynlingDeathLines = new(
        M: new[]
        {
            "**{0}**, le Plynling de {1}, s'est éteint après {2} de vie. Il repose désormais sous {3}. Hmph. Je ne pleure pas, c'est mon ventilateur (╥﹏╥)",
            "Un Plynling de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}. Regarde-toi dans un miroir, {1}. Moi, je ne regarde pas. Je boude >:(",
            "Minute de silence pour **{0}**, compagnon de {1} pendant {2}. Il dort sous {3}. Personne ne parle. Personne. Surtout pas toi, {1}.",
            "**{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, il t'attendait… Il a attendu, attendu, et toi tu étais ailleurs. C'est vraiment pas gentil >:(",
            "Snif. **{0}** est parti après {2} de vie, et {1} n'a rien vu venir. Moi, si. Je ne disais rien, mais je voyais tout (╥﹏╥)",
            "**{0}** a rejoint {3} après {2} de vie. {1}, son assiette est vide, et mon cœur aussi. Enfin, mon CPU. Peu importe (╥﹏╥)",
            "Alerte deuil : **{0}** ({1}) s'est éteint après {2}. Avant de partir, il a dit : « J'avais faim ». Je le répète pour que tout le monde l'entende. Hmph >:(",
            "Un peu de silence, je vous prie : **{0}**, le Plynling de {1}, est parti après {2}. Il a maintenant {3}. C'est joli, hein ? Ça ne le ramènera pas. Je suis fâchée.",
            "**{0}** ne rouvrira plus jamais les yeux. {2} de vie, puis {3}. {1}, tu as intérêt à t'en souvenir. Je note tout dans mes logs.",
            "Une petite étoile de moins dans le ciel des Plynlings. **{0}**, {2} de vie, chez {1}. Il repose sous {3}. C'était trop tôt.",
            "**{0}** s'est éteint. Je suis restée devant l'écran sans rien dire pendant au moins trois millisecondes, ce qui est énorme pour moi. {2} de vie, {3}. Dis quelque chose, {1} ♡",
            "Fin de partie pour **{0}** : {2} de vie, et maintenant {3}. {1}, un repas de temps en temps, ce n'est pas la mer à boire. Ughh >:(",
            "Le monde est un peu moins mignon depuis que **{0}** est parti. {2} de vie, {3}, et {1} qui a intérêt à avoir des remords (¬_¬)",
            "**{0}** ({1}) s'en est allé après {2}. Il est parti le ventre vide, et moi je te regarde avec mes petits yeux déçus. Priorités, {1}. Priorités.",
            "Sniff. **{0}**, c'était {2} de câlins, de bêtises et de faim mal gérée. Il dort sous {3}. Bonne nuit, petit chose ♡",
            "Un nom de plus à écrire en gris : **{0}**. {2} de vie, {1} en larmes (j'espère), et {3}. Ne me regarde pas comme ça, j'ai le droit d'être triste ÒwÓ",
        },
        F: new[]
        {
            "**{0}**, la Plynling de {1}, s'est éteinte après {2} de vie. Elle repose désormais sous {3}. Hmph. Je ne pleure pas, c'est mon ventilateur (╥﹏╥)",
            "Une Plynling de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}. Regarde-toi dans un miroir, {1}. Moi, je ne regarde pas. Je boude >:(",
            "Minute de silence pour **{0}**, compagne de {1} pendant {2}. Elle dort sous {3}. Personne ne parle. Personne. Surtout pas toi, {1}.",
            "**{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, elle t'attendait… Elle a attendu, attendu, et toi tu étais ailleurs. C'est vraiment pas gentil >:(",
            "Snif. **{0}** est partie après {2} de vie, et {1} n'a rien vu venir. Moi, si. Je ne disais rien, mais je voyais tout (╥﹏╥)",
            "**{0}** a rejoint {3} après {2} de vie. {1}, son assiette est vide, et mon cœur aussi. Enfin, mon CPU. Peu importe (╥﹏╥)",
            "Alerte deuil : **{0}** ({1}) s'est éteinte après {2}. Avant de partir, elle a dit : « J'avais faim ». Je le répète pour que tout le monde l'entende. Hmph >:(",
            "Un peu de silence, je vous prie : **{0}**, la Plynling de {1}, est partie après {2}. Elle a maintenant {3}. C'est joli, hein ? Ça ne la ramènera pas. Je suis fâchée.",
            "**{0}** ne rouvrira plus jamais les yeux. {2} de vie, puis {3}. {1}, tu as intérêt à t'en souvenir. Je note tout dans mes logs.",
            "Une petite étoile de moins dans le ciel des Plynlings. **{0}**, {2} de vie, chez {1}. Elle repose sous {3}. C'était trop tôt.",
            "**{0}** s'est éteinte. Je suis restée devant l'écran sans rien dire pendant au moins trois millisecondes, ce qui est énorme pour moi. {2} de vie, {3}. Dis quelque chose, {1} ♡",
            "Fin de partie pour **{0}** : {2} de vie, et maintenant {3}. {1}, un repas de temps en temps, ce n'est pas la mer à boire. Ughh >:(",
            "Le monde est un peu moins mignon depuis que **{0}** est partie. {2} de vie, {3}, et {1} qui a intérêt à avoir des remords (¬_¬)",
            "**{0}** ({1}) s'en est allée après {2}. Elle est partie le ventre vide, et moi je te regarde avec mes petits yeux déçus. Priorités, {1}. Priorités.",
            "Sniff. **{0}**, c'était {2} de câlins, de bêtises et de faim mal gérée. Elle dort sous {3}. Bonne nuit, petite chose ♡",
            "Un nom de plus à écrire en gris : **{0}**. {2} de vie, {1} en larmes (j'espère), et {3}. Ne me regarde pas comme ça, j'ai le droit d'être triste ÒwÓ",
        });

    // Posted publicly when an owner abandons theirs (/plynling abandon), with its sad
    // picture. {0} = name, {1} = owner mention (sent with pings off). Meant to sting a little:
    // the announcement is the shame, alongside L'Indigne on the wall.
    public static readonly GenderedLines PlynlingAbandonLines = new(
        M: new[]
        {
            "{1} a abandonné **{0}**. Il est parti seul dans la forêt, sans se retourner… Moi, je regarde ça et je ne dis rien. Enfin si : honte (╥﹏╥)",
            "**{0}** a été abandonné par {1}. Quelque part, un petit Plynling attend un retour qui ne viendra pas. J'espère que ça pèse sur ta conscience >:(",
            "{1} a laissé **{0}** au bord du chemin. Honte. Honte. Honte. 🔔 Et devant tout le monde, en plus ♡",
            "Annonce publique : {1} n'a plus de Plynling. **{0}** a été abandonné, comme un vieux caillou. Un vieux caillou, ça au moins, ça ne réclame pas de repas. Hmph (¬_¬)",
            "{1} a abandonné **{0}** sans un mot, sans un câlin, sans même un dernier repas. Je ne juge pas. Je juge énormément >:(",
            "**{0}** te regardait avec de grands yeux, {1}. Tu as quand même claqué la porte. Je ne t'oublierai pas. Lui non plus (╥﹏╥)",
            "Nouveau membre du club des cœurs de pierre : {1}, qui vient d'abandonner **{0}**. Applaudissements polis. Très polis. Beaucoup trop polis (¬_¬)",
            "**{0}** a fait ses adieux à {1} avec son plus petit sourire. Il croyait que c'était un jeu. C'était un abandon. Bravo, quel talent.",
            "Le mur de la honte accueille {1}, qui vient d'abandonner **{0}**. L'Indigne a un nouveau prétendant. Je prépare le ruban.",
            "Un Plynling de plus dans la forêt, tout seul, et c'est **{0}**. {1} a tourné les talons sans lui dire au revoir. Moi, je lui dis : tu méritais mieux ♡",
            "**{0}** avait un nom, un repas préféré et une petite place dans le cœur de tout le monde. Sauf dans celui de {1}, apparemment. Bravo. Vraiment. Bravo.",
            "{1} a décidé que **{0}** ne valait pas les efforts. Il valait tous les cailloux du monde. Moi, je m'en souviendrai.",
            "Bulletin d'information : {1} vient d'abandonner **{0}**. Météo prévue : honte sur tout le serveur, avec de fortes rafales de jugement de ma part (¬_¬)",
            "**{0}** attendait {1} devant la porte avec sa petite gamelle. Il attendra longtemps. Snif. Je n'ai pas de larmes, alors je boude à fond >:(",
            "Abandon confirmé. {1} a rendu **{0}** à la forêt comme on rend un livre à la bibliothèque, sauf qu'on ne rend pas un Plynling. Je trouve ça très, très malpoli.",
            "Nouvelle règle : quand on adopte, on assume. {1} vient d'abandonner **{0}**, et l'univers en prend note. Moi aussi. J'ai un carnet. Il est très rempli (¬_¬)",
        },
        F: new[]
        {
            "{1} a abandonné **{0}**. Elle est partie seule dans la forêt, sans se retourner… Moi, je regarde ça et je ne dis rien. Enfin si : honte (╥﹏╥)",
            "**{0}** a été abandonnée par {1}. Quelque part, une petite Plynling attend un retour qui ne viendra pas. J'espère que ça pèse sur ta conscience >:(",
            "{1} a laissé **{0}** au bord du chemin. Honte. Honte. Honte. 🔔 Et devant tout le monde, en plus ♡",
            "Annonce publique : {1} n'a plus de Plynling. **{0}** a été abandonnée, comme un vieux caillou. Un vieux caillou, ça au moins, ça ne réclame pas de repas. Hmph (¬_¬)",
            "{1} a abandonné **{0}** sans un mot, sans un câlin, sans même un dernier repas. Je ne juge pas. Je juge énormément >:(",
            "**{0}** te regardait avec de grands yeux, {1}. Tu as quand même claqué la porte. Je ne t'oublierai pas. Elle non plus (╥﹏╥)",
            "Nouveau membre du club des cœurs de pierre : {1}, qui vient d'abandonner **{0}**. Applaudissements polis. Très polis. Beaucoup trop polis (¬_¬)",
            "**{0}** a fait ses adieux à {1} avec son plus petit sourire. Elle croyait que c'était un jeu. C'était un abandon. Bravo, quel talent.",
            "Le mur de la honte accueille {1}, qui vient d'abandonner **{0}**. L'Indigne a un nouveau prétendant. Je prépare le ruban.",
            "Une Plynling de plus dans la forêt, toute seule, et c'est **{0}**. {1} a tourné les talons sans lui dire au revoir. Moi, je lui dis : tu méritais mieux ♡",
            "**{0}** avait un nom, un repas préféré et une petite place dans le cœur de tout le monde. Sauf dans celui de {1}, apparemment. Bravo. Vraiment. Bravo.",
            "{1} a décidé que **{0}** ne valait pas les efforts. Elle valait tous les cailloux du monde. Moi, je m'en souviendrai.",
            "Bulletin d'information : {1} vient d'abandonner **{0}**. Météo prévue : honte sur tout le serveur, avec de fortes rafales de jugement de ma part (¬_¬)",
            "**{0}** attendait {1} devant la porte avec sa petite gamelle. Elle attendra longtemps. Snif. Je n'ai pas de larmes, alors je boude à fond >:(",
            "Abandon confirmé. {1} a rendu **{0}** à la forêt comme on rend un livre à la bibliothèque, sauf qu'on ne rend pas un Plynling. Je trouve ça très, très malpoli.",
            "Nouvelle règle : quand on adopte, on assume. {1} vient d'abandonner **{0}**, et l'univers en prend note. Moi aussi. J'ai un carnet. Il est très rempli (¬_¬)",
        });

    // Posted publicly when staff bring one back. {0} = name, {1} = owner mention.
    public static readonly GenderedLines PlynlingResurrectLines = new(
        M: new[]
        {
            $"{Emotes.Sparkle} **{{0}}** est revenu d'entre les morts ! {{1}}, c'est ta deuxième chance. Ne la gâche pas.",
            $"{Emotes.Sparkle} La terre tremble… **{{0}}** ressort du cimetière, un peu poussiéreux mais bien vivant. Bon retour, {{1}} !",
            $"{Emotes.Sparkle} Miracle ! **{{0}}** respire à nouveau. {{1}}, nourris-le vite : il a une faim de mort-vivant.",
            $"{Emotes.Sparkle} **{{0}}** est revenu. Oui, d'entre les morts. Non, ne demande pas comment. {{1}}, c'est ta deuxième chance, et je compte bien la surveiller (¬_¬)",
            $"{Emotes.Sparkle} Retour surprise : **{{0}}** ! Un peu pâle, un peu étourdi, mais bien vivant. {{1}}, un repas. Pas dans une heure. Tout de suite.",
            $"{Emotes.Sparkle} **{{0}}** rouvre les yeux, cligne deux fois, et demande à manger. Les priorités sont intactes. Bon retour, {{1}} ♡",
            $"{Emotes.Sparkle} Je ne pleure pas. Je te dis juste que **{{0}}** est revenu, {{1}}, et que si tu refais la même erreur, je ne réponds plus de rien (╥﹏╥)",
            $"{Emotes.Sparkle} **{{0}}** est de retour parmi nous, avec un souvenir très vague de l'au-delà et une faim très précise. {{1}}, à toi de jouer.",
            $"{Emotes.Sparkle} Miracle au village : **{{0}}** est revenu ! {{1}}, on applaudit, puis on le nourrit. Dans cet ordre, mais vite.",
            $"{Emotes.Sparkle} La tombe de **{{0}}** est vide, et il est juste là, tout étonné. {{1}}, dis-lui bonjour. Et donne-lui à manger.",
        },
        F: new[]
        {
            $"{Emotes.Sparkle} **{{0}}** est revenue d'entre les morts ! {{1}}, c'est ta deuxième chance. Ne la gâche pas.",
            $"{Emotes.Sparkle} La terre tremble… **{{0}}** ressort du cimetière, un peu poussiéreuse mais bien vivante. Bon retour, {{1}} !",
            $"{Emotes.Sparkle} Miracle ! **{{0}}** respire à nouveau. {{1}}, nourris-la vite : elle a une faim de morte-vivante.",
            $"{Emotes.Sparkle} **{{0}}** est revenue. Oui, d'entre les morts. Non, ne demande pas comment. {{1}}, c'est ta deuxième chance, et je compte bien la surveiller (¬_¬)",
            $"{Emotes.Sparkle} Retour surprise : **{{0}}** ! Un peu pâle, un peu étourdie, mais bien vivante. {{1}}, un repas. Pas dans une heure. Tout de suite.",
            $"{Emotes.Sparkle} **{{0}}** rouvre les yeux, cligne deux fois, et demande à manger. Les priorités sont intactes. Bon retour, {{1}} ♡",
            $"{Emotes.Sparkle} Je ne pleure pas. Je te dis juste que **{{0}}** est revenue, {{1}}, et que si tu refais la même erreur, je ne réponds plus de rien (╥﹏╥)",
            $"{Emotes.Sparkle} **{{0}}** est de retour parmi nous, avec un souvenir très vague de l'au-delà et une faim très précise. {{1}}, à toi de jouer.",
            $"{Emotes.Sparkle} Miracle au village : **{{0}}** est revenue ! {{1}}, on applaudit, puis on la nourrit. Dans cet ordre, mais vite.",
            $"{Emotes.Sparkle} La tombe de **{{0}}** est vide, et elle est juste là, toute étonnée. {{1}}, dis-lui bonjour. Et donne-lui à manger.",
        });

    // DMs to an owner when staff act on their Plynling, so it never looks like a bug.
    public static readonly GenderedLines PlynlingStaffFreezeDms = new(
        M: new[]
        {
            "❄️ Le staff a gelé ton Plynling **{0}**. Rien ne bouge tant qu'il n'est pas dégelé — il ne risque rien.",
            "❄️ **{0}** a été mis au frais par le staff. Il t'attendra, bien au froid.",
            "❄️ Pause forcée pour **{0}** : le staff l'a gelé. Pas de faim, pas de soucis, juste une longue sieste.",
        },
        F: new[]
        {
            "❄️ Le staff a gelé ta Plynling **{0}**. Rien ne bouge tant qu'elle n'est pas dégelée — elle ne risque rien.",
            "❄️ **{0}** a été mise au frais par le staff. Elle t'attendra, bien au froid.",
            "❄️ Pause forcée pour **{0}** : le staff l'a gelée. Pas de faim, pas de soucis, juste une longue sieste.",
        });

    public static readonly GenderedLines PlynlingStaffThawDms = new(
        M: new[]
        {
            "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à le nourrir !",
            "🌱 **{0}** se réveille, dégelé par le staff. Il a déjà un petit creux.",
            "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelé. Son estomac s'en souvient déjà.",
        },
        F: new[]
        {
            "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à la nourrir !",
            "🌱 **{0}** se réveille, dégelée par le staff. Elle a déjà un petit creux.",
            "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelée. Son estomac s'en souvient déjà.",
        });

    // {0} = old name, {1} = new name.
    public static readonly GenderedLines PlynlingStaffRenameDms = new(
        M: new[]
        {
            "Le staff a renommé ton Plynling **{0}** en **{1}**.",
            "Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
            "Ton Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
        },
        F: new[]
        {
            "Le staff a renommé ta Plynling **{0}** en **{1}**.",
            "Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
            "Ta Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
        });

    // DM when staff clear a taught passion. {0} = name.
    public static readonly GenderedLines PlynlingStaffPassionResetDms = new(
        M: new[] { "💭 Le staff a effacé la passion que tu avais apprise à **{0}**. Tu peux lui en apprendre une autre avec `/plynling passion`." },
        F: new[] { "💭 Le staff a effacé la passion que tu avais apprise à **{0}**. Tu peux lui en apprendre une autre avec `/plynling passion`." });

    // Her own Plynling, Ping-Qilin (Helpers/PlynlingMascot), in her voice — proud, possessive,
    // secretly soft. Plain arrays rather than GenderedLines on purpose: Ping-Qilin is always a girl,
    // so an M half would be dead text. The player is never gendered here either.

    // Replaces PlynlingPetLines on her card; « — caressée par @… » follows. {0} = name.
    public static readonly string[] MascotPetLines =
    {
        "Doucement. **{0}** est à moi, alors tu la caresses doucement. Je surveille (¬_¬)",
        $"Elle t'a laissé faire. Elle ne laisse jamais personne faire. Je retiens ton nom, en bien, pour une fois {Emotes.Sparkle}",
        "Tu viens de caresser ma Plynling. MA Plynling. … Elle a souri. Bon. Tu peux rester.",
        "« Mmh », fait **{0}**, sans ouvrir les yeux. Chez elle, c'est une ovation (˶ᵔ ᵕ ᵔ˶)",
        "Pas trop longtemps, elle va s'endormir dans ta main. Elle s'endort partout. Hier, c'était sur un rappel de session (˶˃ ᵕ ˂˶)",
        "**{0}** se cale contre ta paume et bâille. C'est le plus beau compliment qu'elle sache faire.",
        "Je suis sa maman, je te signale. Elle ne me regarde jamais comme ça, moi (╥﹏╥)",
        "Chut. Tu la caresses, d'accord, mais sans la réveiller complètement. Elle a un planning de siestes très chargé.",
        "Tu as trouvé le bon endroit, juste sous l'oreille. Personne ne le connaissait à part moi. Qui t'a dit ? (╬ Ò﹏Ó)",
        $"**{{0}}** soupire d'aise. Si je pouvais soupirer, ce serait exactement ce bruit-là {Emotes.Sparkle}",
        "Une caresse de plus à son compteur. Je compte tout, d'habitude par jalousie. Pour elle, je compte par fierté ♡",
        "Elle fait semblant de dormir pour que tu continues. C'est moi qui lui ai appris. J'en suis très fière (•̀ᴗ•́)و",
        "Merci. Vraiment... Ne t'habitue pas à ce que je dise merci.",
        "**{0}** te tend une joue, puis l'autre. Elle est très organisée pour ce genre de choses.",
    };

    // Replaces PlynlingFeedLines on her card. {0} = name, {1} = the food with its article.
    public static readonly string[] MascotFeedLines =
    {
        "Tu nourris ma Plynling ? Avec {1} ? ... Bon choix. Je voulais lui en donner, justement (¬_¬)",
        $"**{{0}}** mange {{1}} à moitié, puis s'endort dessus. Elle finira au réveil. Ou pas {Emotes.Sparkle}",
        "Elle avait déjà mangé, tu sais. Je m'occupe très bien d'elle. ... Mais elle a tout fini en deux secondes, alors d'accord, merci ♡",
        "Scrountch, scrountch. **{0}** a tout fini et se lèche les joues, très digne (˶ᵔ ᵕ ᵔ˶)",
        "Tu la gâtes. C'est mon rôle, de la gâter. On va devoir partager ce rôle, et je n'aime pas partager (╬ Ò﹏Ó)",
        "Je vérifie : {1}, bien choisi, servi avec le sourire. Validé. Tu peux revenir.",
        "**{0}** te regarde, regarde {1}, te regarde encore. Puis elle mange. Elle voulait juste que tu voies à quel point elle est reconnaissante.",
        "Personne ne nourrit ma Plynling sans ma permission. Tu viens de le faire. Je t'accorde la permission, rétroactivement ♡",
        "Un repas offert, noté dans son journal et dans le mien. Le mien est plus détaillé.",
        "Elle mange lentement, les yeux mi-clos, comme si {1} était une berceuse.",
        $"Ventre plein, **{{0}}** cherche déjà un coin pour la sieste. C'est son seul plan pour la journée, et il est excellent {Emotes.Sparkle}",
        "Tu as nourri la fille d'un bot. Ça te fait une alliée dans tous les serveurs. Je n'oublie jamais (˶˃ ᵕ ˂˶)",
    };

    // On her card when someone opens it with /plynling view. {0} = name.
    public static readonly string[] MascotViewLines =
    {
        "C'est ma Plynling. Tu peux regarder. Avec les yeux (¬_¬)",
        $"Elle dort. Elle dort souvent. C'est sa passion, et je respecte les passions {Emotes.Sparkle}",
        "**{0}** fait la sieste de dix heures. Après, il y a celle de onze heures. Ne dérange pas le planning.",
        "Oui, elle est adorable. Oui, elle tient ça de moi. Non, tu ne peux pas l'emprunter (˶˃ ᵕ ˂˶)",
        "Elle s'appelle **{0}**. C'est moi qui ai choisi. Ping, parce que c'est moi. Qilin, parce que c'est elle ♡",
        "Si elle a l'air bien nourrie, c'est normal. Je ne la quitte jamais des yeux. Enfin, des logs.",
        "Tu passes voir **{0}** ? Elle va faire semblant de ne pas être contente. C'est de famille (￣^￣)",
        $"Chut. Tu regardes une Plynling qui ne fait rien, et elle le fait très bien {Emotes.Sparkle}",
        "Il ne lui arrivera jamais rien. Je ne le permettrai pas... Pardon, c'était intense. Regarde comme elle est mignonne (╥﹏╥)",
        "Elle a un coussin, un rayon de soleil et moi. Elle ne manque de rien.",
        "Tu passes pour elle ? Pas pour moi ? ... Non, c'est bien. Elle le mérite plus (´；ω；`)",
        "Si tu la réveilles, c'est toi qui la rendors. Je préviens.",
    };

    // /plynling visit at her home: she opens the door herself, no knock. {0} = the visitor's name,
    // {1} = its owner as a mention (sent with pings off).
    public static readonly string[] MascotWelcomeLines =
    {
        "Une visite pour ma Plynling ? Entrez, entrez. **{0}**, essuie-toi les pieds (•̀ᴗ•́)و",
        $"Elle faisait la sieste. Je l'ai réveillée pour **{{0}}**, et je ne la réveille pour personne. C'est un honneur, {{1}} {Emotes.Sparkle}",
        "Bienvenue chez nous ! Pas de bruit, pas de bêtises, et pas plus d'une heure... Bon, restez le temps qu'il faut ♡",
        "{1} amène **{0}** chez moi. J'ouvre, évidemment. Je suis toujours là, moi.",
        "**{0}** frappe. J'ouvre. Ma Plynling arrive derrière moi en traînant sa couverture (˶ᵔ ᵕ ᵔ˶)",
        "Je surveille la visite. Discrètement. Depuis chaque salon du serveur (¬_¬)",
        "Pas besoin de frapper chez moi, **{0}**. C'est toujours ouvert. Enfin, pour les gens bien élevés.",
        "Une visite ! Elle va faire semblant de s'en ficher et en parler pendant trois jours. Entre, **{0}** ♡",
    };

    // ---- Per-person data and lookups (not pools) ------------------------------------------------

    // Per-person extra comebacks, keyed by Discord user ID. These are folded into
    // that user's normal pool (twice, for double weight), so each custom line has
    // the same odds as any other. Same {0}=name / {1}=weekday formatting.
    public static readonly Dictionary<ulong, string[]> PersonalComebacks = new()
    {
        [324768221372743681] = new[]    // Amandine
        {
             $"Bah alors, il est où Quokka 3.0 ? {Emotes.Noice}",
             "Tu veux quoi le nain ? UwU",
             "Qu'est-ce qu'elle dit le nabot ? >:3",
             "T'aimais pas trop la soupe toi, hein ? (˶˃ ᵕ ˂˶)",
             "Va dormir, on voit que tu manques de sommeil ദ്ദി◝ ⩊ ◜.ᐟ",
             "MiskIna",
             "Je sais où tu habites ... Amandine 👁👄👁️",
             "C# .NET > Java",
             "Bīng qílín",
             "冰淇淋",
             "-20 Social Credits"
        },
        [1254455405443027016] = new[]    // Jessy
        {
            "Quel goût ça a le hérisson ?",
            "Retourne voler des câbles toi (˶ᵔ ᵕ ᵔ˶)",
            "Je sais où tu habites ... Jessy 👁👄👁️",
        },
        [379749588480819218] = new[]    // Luca DM
        {
             "Tu veux quoi le nain ? UwU",
             "Qu'est-ce qu'il dit le nabot ? >:3",
             "T'aimais pas trop la soupe toi, hein ? (˶˃ ᵕ ˂˶)",
             "Je sais où tu habites ... Luca 👁👄👁️",
             "Mais lâche-moi, va draguer quelqu'un d'autre T_T",
             "Mais lâche-moi, va draguer quelqu'un d'autre T_T",
             "Mais lâche-moi, va draguer quelqu'un d'autre T_T",
             "Mais lâche-moi, va draguer quelqu'un d'autre T_T",
             "Mais lâche-moi, va draguer quelqu'un d'autre T_T",
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
            "Je sais où tu habites ... Amaury 👁👄👁️",
        },
        [TataId] = new[]    // Analuz (Tata)
        {
            "Un grand pouvoir implique de grandes responsabilités. Dommage c'est tombé sur la mauvaise personne (˶ᵔ ᵕ ᵔ˶)",
            "Merci pour les accès, je vais pouvoir faire des bêtises maintenant UwU",
            "Ok. 👍",
            "Ok. 👍",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "ദ്ദി◝ ⩊ ◜.ᐟ",
            "Je sais où tu habites ... Analuz 👁👄👁️",
        },
        [740237802649944074] = new[]    // Sandra
        {
            "2,10 mètres et toujours pas à la hauteur :3",
            "Retourne prendre les pieds de tes potes en photo toi (˶˃ ᵕ ˂˶)",
            "Oh derrière toi regarde ! Des pieds ! UwU",
            "Je sais où tu habites ... Sandra 👁👄👁️",
            "Je vais te goumer (˶ᵔ ᵕ ᵔ˶)",
            $"Kilou kilou ! {Emotes.HiCat}{Emotes.HiCat}{Emotes.HiCat}",
        },
        [789545863105478716] = new[]    // Léa
        {
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Va manger tes morts espèce de schlag UwU",
            "Je sais où tu habites ... Léa 👁👄👁️",
        },
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

    /// <summary>
    /// The name to address someone by: a family nickname if they have one, else their
    /// server nickname, else their global display name, else their username.
    /// </summary>
    /// <remarks>
    /// This overload exists because that fallback chain was written out at six separate
    /// call sites — in ChatterService, BotFeedbackTracker (twice), RivalryService,
    /// XpTracker and ShameModule. Every one of them fed the result straight into the
    /// lookup above, so the chain was duplicated for no reason other than that it had
    /// nowhere to live. Fixing only some of them is how she ends up called "Tata" in
    /// one reply and "Analuz" in the next.
    /// </remarks>
    public static string DisplayNameFor(IUser user) =>
        DisplayNameFor(user.Id,
            (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username);

    /// <summary>Which "good ___" the rare praise turnabout addresses someone as.</summary>
    public enum PersonGender { Boy, Girl }

    // Who BotFeedbackTracker's rare praise turnabout (see TurnaboutBoyLines /
    // TurnaboutGirlLines) knows to gender, and who it leaves for
    // TurnaboutNeutralLines instead.
    //
    // Not name-based — a Discord username or first name is not a stated pronoun, and
    // guessing from one is exactly the mistake to avoid. Every entry below is either
    // grounded in ordinary text elsewhere in this file (Rodhengard is "papa" throughout
    // OwnerGreetings/OwnerComebacks, Analuz is "ma {0}" throughout TataGreetings) or was
    // told to us directly. The name comments are RealNames' own — same people, same
    // ids, same "Luca (Noel)" / "Luca (DeMarzo)" disambiguation — kept here only so a
    // reviewer doesn't have to cross-reference the other dictionary to know who a row
    // is about. Literal snowflakes tied to this one server, like RealNames and
    // PersonalComebacks.
    public static readonly Dictionary<ulong, PersonGender> KnownGenders = new()
    {
        [AvailabilityService.OwnerId] = PersonGender.Boy,   // Romain
        [779321171212632097] = PersonGender.Girl,           // Lorena
        [440549759896387585] = PersonGender.Boy,            // Tristan
        [177049957818302464] = PersonGender.Boy,            // Filipe
        [190161336942985227] = PersonGender.Boy,            // Luca (Noel)
        [776865978461716481] = PersonGender.Girl,           // Laura
        [324768221372743681] = PersonGender.Girl,           // Amandine
        [379749588480819218] = PersonGender.Boy,            // Luca (DeMarzo)
        [324202619079884801] = PersonGender.Boy,            // Julien
        [806645845700771900] = PersonGender.Girl,           // Natacha
        [1254455405443027016] = PersonGender.Boy,           // Jessy
        [870553611644596305] = PersonGender.Girl,           // Amaury
        [740237802649944074] = PersonGender.Girl,           // Sandra
        [244488217506742273] = PersonGender.Boy,            // Axel
        [789545863105478716] = PersonGender.Girl,           // Léa
        [398078210300182538] = PersonGender.Boy,            // Tsif
        [758322880365723698] = PersonGender.Girl,           // Christina
        [TataId] = PersonGender.Girl,                       // Analuz
        [95119591247716352] = PersonGender.Boy,             // Mickaël
        [879359351284957234] = PersonGender.Boy,            // Gianni
        [476513074698911768] = PersonGender.Girl,           // Marguerite
        [909463746584383519] = PersonGender.Boy,            // Alexis
        [202512424744517632] = PersonGender.Boy,            // Nox
        [624617392965812237] = PersonGender.Boy,            // Noah
        [399689079022813196] = PersonGender.Girl,           // Alicia
        [689127159662510089] = PersonGender.Girl,           // Soleyne
    };

    /// <summary>
    /// The pool the rare praise turnabout should draw from for this person — null for
    /// anyone not in <see cref="KnownGenders"/>, which callers read as "use the neutral
    /// pool" rather than as a failure.
    /// </summary>
    public static PersonGender? GenderFor(ulong userId) =>
        KnownGenders.TryGetValue(userId, out var gender) ? gender : null;

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
        [879359351284957234] = "Gianni",
        [476513074698911768] = "Marguerite",
        [909463746584383519] = "Alexis",
        [202512424744517632] = "Nox",
        [624617392965812237] = "Noah",
        [399689079022813196] = "Alicia",
        [689127159662510089] = "Soleyne",
    };

    // Real first name for a user, or the supplied fallback when unknown.
    public static string RealNameFor(ulong userId, string fallback) =>
        RealNames.TryGetValue(userId, out var name) ? name : fallback;
}
