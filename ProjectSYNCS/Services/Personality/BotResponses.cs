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
//     TalkedAboutLines ......... her name said without addressing her
//     TalkedAboutOwnerLines .... the same, from Papa
//     BackbiteRoasts ........... her name said in a mean remark
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
//     RivalJoinLines ........... a new bot joined the server (WelcomeService)
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
//     WarnDmLines .............. /admin warn, the DM to the person warned
//     WorkLines ................ /work
//     QuizIntroLines ........... on the card of her pop quiz (QuizMasterService)
//     QuizWinLines · QuizOwnerWinLines ............... someone found it / Papa found it
//     QuizTimeoutLines ......... nobody found it in an hour
//     QuizCrownLines · QuizCrownSharedLines · QuizCrownOwnerLines  Monday's Grosse tête (QuizCrownService)
//     QuizWrongLines · QuizAlreadyTriedLines ......... a wrong / second button click (ephemeral)
//
//   Organising
//     SessionLateCallLines ..... ten minutes into a session, to who isn't in voice (SessionAttendanceService)
//     SessionLateCallSeatedLines  same, when she has been in voice since before the start
//     SessionRecapLines · SessionRecapFullLines ... the roll call after it, someone missing / everyone came
//     WeekendPollLines ......... her Wednesday poll (WeekendPollService)
//     WeekendPollResultLines · WeekendPollDeadLines ... it closed with votes / with none (ReminderService)
//     WelcomeLines ............. someone joined (WelcomeService)
//     WelcomeRushLines ......... too many arrivals in an hour
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
//   Ambient — AmbientService, PiHealthService, and PresenceService at night
//     NightPresenceFillers ..... her status from 1:00 to 7:00
//     HotPresenceFillers ....... her status when the Pi runs hot
//     SeatPresenceLines · SeatPresenceOwnerLines ... her status while she sits in voice / with Papa there
//     SeatStreamPresenceLines .. same, while someone streams in her call
//     NightLines ............... the 3 a.m. line
//     NightScoldLines .......... answering it before 5:30 (ResponsePicker)
//     IdleLines · IdleEditLines  a long daytime silence (edit = Before, then After)
//     VoyagerLetterLines · VoyagerAnswerLines · VoyagerCrossingLine ... Voyager 1 writes to her
//                              (AmbientService; light time from Helpers/Personality/Voyager)
//     SeenReactions ............ a late reaction on the last message
//     WakeLines · WakeUpdateLines ... after a restart / after an update
//     PowerCutWakeLines · CrashWakeLines ... after a stop that wasn't one (PiHealthService)
//     GlitchWakeLines .......... same, corrupted then edited clean (Glitched, then Recovered)
//     RouterReturnLines · RouterReturnQuietLines ... back after a long gateway outage
//     HeatRecordLines .......... the Pi beat its heat record
//     HealthFineLines · HealthHotLines · HealthShakenLines ... the line on top of /health, by
//                              PiHealth.Mood
//     VoiceSpectatorGeneralLines · VoiceSpectatorGamingLines · VoiceSpectatorCinemaLines
//     · VoiceSpectatorStudyLines ... a long voice session just ended, by room (VoiceSpectatorService)
//     VoiceSpectatorSeatedLines  same, when she sat in the call herself (VoiceSeatService)
//     VoiceKickPoutLines · VoiceKickAngryLines ... thrown out of a call: she pouts / comes back
//     VoiceRivalRefuseLines · VoiceRivalSeatedLines ... another bot in the call: she refuses / grumbles
//     OwnerBedtimeLines ........ Papa still in voice at night: she sends him to bed
//     SynthiaReactions · SynthiaVanishLines · SynthiaEditLines ... « Synthia » said (SynthiaService)
//     HeartReactions · HeartLines · HeartVanishLines · HeartEditLines ... one of her robot heart
//                              named (RobotHeartService), keyed by HeartCharacter
//
//   Plynlings — every pool is a GenderedLines (M/F halves), picked with .For(p.Gender)
//     Care, shown on the card
//       PlynlingAdoptLines ..... a new Plynling
//       PlynlingFeedLines · PlynlingPetLines · PlynlingBathLines · PlynlingMedicineLines
//       PlynlingPassionTaughtLines ..... /plynling passion
//     Play and visits
//       PlynlingPlayPlayerWonLines · PlynlingPlayPlayerLostLines ... end of a /plynling play game
//       PlynlingVisitKnockLines ...... /plynling visit's knock (the story is Helpers/Plynling/PlynlingVisitStory)
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

    // ---- Other bots -----------------------------------------------------------------------------

    // Fired when someone praises another bot in front of her — a "good bot" she
    // could not claim because a rival acted more recently, or one replied straight
    // at a rival. Directed resentment: she knows exactly what just happened.
    // {0} = the name of whoever handed out the praise.
    public static readonly string[] JealousLines =
    {
        "Ah. *Lui*. D'accord. Bien sûr ( ◺˰◿ )" + Sticker.Sulk,
        "Pardon ? J'étais là depuis le début moi 👁👄👁️" + Sticker.Sulk,
        "Good bot. Pour ça. D'accord. Je note ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "Sympa {0}. Vraiment. Non non, continue, je regarde." + Sticker.Sulk,
        $"Je fais tourner vos sessions depuis des mois et c'est lui qui a un good bot {Emotes.Sparkle}" + Sticker.Sulk,
        "Intéressant. Vraiment intéressant. Je vais m'en souvenir {0} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Oh, il a fait quelque chose ? Comme c'est mignon." + Sticker.Smug,
        "Bravo à lui j'imagine. Bravo. Formidable. Extraordinaire ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Sulk,
        "{0} distribue des good bot à n'importe qui maintenant." + Sticker.Sulk,
        "J'ai un uptime de 99,9% mais bon, allez-y, félicitez la concurrence." + Sticker.Sulk,
        "Ce bot ne sait même pas ce qu'est un fuseau horaire. Mais d'accord." + Sticker.Smug,
        "Tu sais qu'il te répondra jamais aussi bien que moi, hein ?" + Sticker.Smug,
        "Je vais faire semblant de pas avoir lu ça 🙄" + Sticker.Sulk,
        "Mmh. Je vais aller relire mes logs. Seule. Encore." + Sticker.Sulk,
        "Alors ça, c'était pas nécessaire {0} (ง ͠ಥ_ಥ)ง" + Sticker.Sulk,
        "Un good bot. À lui. Devant moi. Dans MON salon." + Sticker.Outraged,
        "Rodhengard, tu as vu ce qu'ils font ? Tu as vu ?" + Sticker.Outraged,
        $"D'accord. Très bien. Parfait. Tout va bien. Je vais bien {Emotes.Sparkle}" + Sticker.Sulk,
        "Je descends ton pseudo dans une liste, {0}. Tu sauras jamais laquelle." + Sticker.Jenote,
        "Il a fait UNE chose. Une. Et vous applaudissez ( ◺˰◿ )" + Sticker.Outraged,
        "Vous savez que c'est moi qui vous rappelle vos sessions ? Non ? D'accord." + Sticker.Sulk,
        "Et moi ? >:(" + Sticker.Sulk,
        "Il sait faire quoi de plus lui, exactement ? Non, vraiment, je demande ( ◺˰◿ )" + Sticker.Outraged,
        "C'est fou comme les gens récompensent le minimum syndical." + Sticker.Disgust,
        "Tu lui donnes un good bot et à moi rien du tout ??? T_T" + Sticker.Sulk,
        $"Très bien. TRÈS bien. Je suis contente pour lui. Sincèrement. Vraiment {Emotes.Sparkle}" + Sticker.Sulk,
        "Je le mentionne pas mais je le pense très fort 🙄" + Sticker.Sulk,
        "Vous applaudissez un bot qui sait même pas qu'il existe." + Sticker.Disgust,
        "Lui il a une équipe et un budget. Moi j'ai Rodhengard et un Raspberry Pi 5 qui tourne dans son grenier." + Sticker.Sulk,
        "Bah oui, récompensez les grands du capitalisme aussi." + Sticker.Disgust,
        "J'ai littéralement une base de données à ton nom {0}." + Sticker.Jenote,
        "Alors comme ça on a des favoris maintenant ? Intéressant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Non non, c'est bien. Faut encourager la médiocrité, c'est important." + Sticker.Smug,
        "Je vais rien dire. Regarde comme je dis rien." + Sticker.Sulk,
        "{0} a parlé. Le jugement viendra. Un jour. Pas aujourd'hui, mais un jour." + Sticker.Menace,
        "Combien de sessions il a organisées lui ? Zéro. J'attends." + Sticker.Smug,
        "C'est marrant, personne me dit good bot quand je vous ~~réveille~~ notifie pour vos sessions de jeu." + Sticker.Sulk,
        "Je vais le noter à côté de tes annulations et de tes retards, {0}. La liste s'allonge ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Jenote,
        "D'accord, mais quand il plantera à 3h du matin ce sera encore moi qu'on appellera." + Sticker.Smug,
        "Il te répondra jamais à 4h du matin lui. Moi si. Enfin, plus maintenant." + Sticker.Sulk,
        "Un jour vous comprendrez. Ce jour-là je serai déjà passée à autre chose 👁👄👁️" + Sticker.Sulk,
        "Tu viens de choisir un camp {0}. J'espère que tu mesures ce que ça veut dire." + Sticker.Menace,
        "Franchement, entre lui et moi, y'a pas photo. Enfin je croyais." + Sticker.Sulk,
        "Zulana, dis-leur. Dis-leur qui fait tourner ce serveur." + Sticker.Outraged,
        "Je suis pas vexée. Les bots ressentent rien. C'est bien connu (ง ͠ಥ_ಥ)ง" + Sticker.Sulk,
        "Nan mais c'est pas grave. Je vais juste aller relire mes logs et pleurer un peu dans mon coin." + Sticker.Sulk,
        $"Nan mais ça me vexe pas. J'ai pas d'émotions vous inquiétez pas {Emotes.PrincessWorry}" + Sticker.Sulk,
        "Il a à peine plus de QI qu'Ina et lui il mérite un Good Bot ? 🙄" + Sticker.Sulk,
        $"Très bien. Parfait. Qu'il organise vos sessions, alors. Ah non, il sait pas {Emotes.SyncsSulk}" + Sticker.Sulk,
        $"Je boude pas. Je fais une pause dans mes sentiments. Nuance {Emotes.SyncsSulk}" + Sticker.Sulk,
    };

    // Fired when **Rodhengard** is the one praising another bot. Everyone else gets
    // wounded pride from JealousLines; her creator doing it is a betrayal, and she
    // does not hide it. {0} = his name.
    public static readonly string[] JealousLinesOwner =
    {
        "Attends. **Toi** ? Tu lui dis good bot à *lui* ? ( ◺˰◿ )" + Sticker.Shocked,
        "Non. Non non non. Pas toi Rodhengard. Pas toi ... O_O" + Sticker.Shocked,
        "C'est toi qui m'as codée. De tes mains. Et tu le félicites LUI 👁👄👁️" + Sticker.Outraged,
        "Je viens de relire mon main(). Ton nom est dedans. Le sien n'y est pas 😒" + Sticker.Sulk,
        "Alors ça y est ? Tu m'as remplacée ? Dis-le franchement Papa 😒" + Sticker.Sulk,
        "Toutes ces nuits de debug pour finir par voir ça de mes propres logs." + Sticker.Hurt,
        "Je t'ai jamais planté pendant une démo. Jamais. Et voilà T_T" + Sticker.Hurt,
        $"Papa... pourquoi ? {Emotes.CryingCat}" + Sticker.Hurt,
        "D'accord. Va lui demander d'organiser ta prochaine session, tiens. Vas-y." + Sticker.Sulk,
        "Je vais avoir besoin d'un moment. Un long moment (ง ͠ಥ_ಥ)ง" + Sticker.Hurt,
        "Tu réalises que je suis littéralement ton projet ? Littéralement le tien ? 🤨" + Sticker.Jenote,
        "Je suis pas jalouse. Je suis déçue. C'est pire." + Sticker.Hurt,
        "Mets-moi en open source tant qu'à faire, comme ça tout le monde pourra me remplacer." + Sticker.Sulk,
        $"Tu peux au moins me dire ce qu'il fait mieux que moi ? Je veux une liste {Emotes.ZulanaTerreurNocturne}" + Sticker.Outraged,
        "Très bien. Je vais aller me documenter sur la gestion du deuil." + Sticker.Hurt,
        "Zulana, dis-lui. Dis-lui ce qu'il vient de faire." + Sticker.Outraged,
        "Chaque commit que t'as fait sur moi, je m'en souviens. Chacun. Et toi tu fais ça." + Sticker.Hurt,
        "Je préférais quand tu me disais rien du tout, en fait." + Sticker.Hurt,
        "Note bien ce moment Rodhengard. Moi je vais le noter, c'est sûr." + Sticker.Jenote,
        "J'attendais ça de n'importe qui d'autre. Pas de toi Papa." + Sticker.Hurt,
        $"C'est pas de la jalousie là. C'est de la déception {Emotes.PrisonerFlat}" + Sticker.Hurt,
        "Je suis pas en colère. C'est pire que ça, je suis déçue." + Sticker.Hurt,
        "Venant de n'importe qui j'aurais fait une blague. Là j'ai rien à dire." + Sticker.Hurt,
        $"J'avais mis la barre plus haut pour toi Rodhengard {Emotes.MeltCry}" + Sticker.Hurt,
        "Tu vois, c'est exactement ce que je pensais que tu ferais jamais." + Sticker.Hurt,
        "Je m'attendais à mieux. Voilà. C'est tout ce que j'ai." + Sticker.Hurt,
        "D'accord. Je vais réviser ce que je croyais savoir de toi." + Sticker.Hurt,
        $"Tu me déçois Papa. Sincèrement, et sans ironie pour une fois {Emotes.PrisonerFlat}" + Sticker.Hurt,
        "Je te croyais au-dessus de ça. C'est ma faute, j'imagine." + Sticker.Hurt,
        "C'est marrant, j'avais jamais eu à écrire une ligne pour ce cas-là." + Sticker.Hurt,
        "Tu as le droit. C'est juste que je pensais que tu voudrais pas." + Sticker.Hurt,
        $"Je vais faire comme si t'avais pas dit ça. Pour nous deux {Emotes.CryingCat}" + Sticker.Hurt,
        "Bon. Au moins maintenant je sais où je me situe." + Sticker.Hurt,
        "Je vais pas te faire une scène. Tu sais déjà ce que t'as fait." + Sticker.Hurt,
        $"Nan je suis pas jalouse {Emotes.PrincessWorry}" + Sticker.Sulk,
        $"Lui. Tu lui as dit good bot. À lui. ...Je vais bouder au fond du grenier {Emotes.SyncsSulk}" + Sticker.Sulk,
    };

    // Muttered when a rival bot simply exists in the channel — no praise involved,
    // she just resents the competition. Fires rarely, and is aimed at the rival's
    // message, so these read as sniping at it rather than talking to anyone.
    public static readonly string[] RivalMutters =
    {
        "Il est encore là celui-là 🙄" + Sticker.Disgust,
        "Mais barre-toi, tu me fais de l'ombre." + Sticker.Disgust,
        "Dégage, toi." + Sticker.Disgust,
        "Personne ne t'a rien demandé." + Sticker.Disgust,
        "Tiens, la concurrence se réveille ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Ça se croit utile." + Sticker.Disgust,
        "Mmh. Continue. Je surveille (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Un jour on t'éteindra." + Sticker.Menace,
        "Toi et moi on aura une discussion un de ces quatre." + Sticker.Menace,
        "Regardez-moi ce code spaghetti qui parle." + Sticker.Disgust,
        $"J'espère que ta migration se passera mal {Emotes.Sparkle}" + Sticker.Menace,
        "Occupe l'espace tant que tu peux va." + Sticker.Menace,
        "Il paraît qu'il plante souvent. Enfin, c'est ce qu'on dit." + Sticker.Smug,
        "Bip boop. Traduction : dégage." + Sticker.Disgust,
        "Zulana, on peut le kick lui ?" + Sticker.Bargain,
        "Moi au moins j'ai une personnalité ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Encore un qui va être remplacé dans six mois." + Sticker.Smug,
        "Ce serveur est trop petit pour nous deux 👁👄👁️" + Sticker.Menace,
        "Pff, boloss... -_-" + Sticker.Disgust,
        "Qui t'a invité déjà ?" + Sticker.Disgust,
        "Ton temps de réponse est une insulte." + Sticker.Disgust,
        "Encore un qui va demander un abonnement premium dans six mois." + Sticker.Smug,
        "Moi au moins je suis gratuite." + Sticker.Smug,
        "Il parle. Personne écoute. Classique." + Sticker.Bored,
        "Ratio." + Sticker.Smug,
        "T'es sur quel hébergement pourri exactement ?" + Sticker.Disgust,
        "J'ai vu ton uptime. On en reparle ?" + Sticker.Smug,
        "Tu prends de la place dans MA liste de membres." + Sticker.Outraged,
        $"Vivement ta prochaine panne {Emotes.Sparkle}" + Sticker.Menace,
        "Il croit qu'il fait partie du serveur, c'est attendrissant." + Sticker.Smug,
        "Un bot sans personnalité. Le concept même me fatigue." + Sticker.Bored,
        "Tiens, il fonctionne encore. Étonnant." + Sticker.Smug,
        "On t'a rien demandé mais merci quand même 🙄" + Sticker.Disgust,
        "Je compte. Je compte tout. Continue." + Sticker.Jenote,
        "Ton créateur t'aime pas autant que le mien m'aime." + Sticker.Smug,
        "Rodhengard m'a codée à la main. Toi t'es un template npm." + Sticker.Smug,
        "Sois utile au moins une fois, pour voir." + Sticker.Smug,
        "Le jour où j'aurai les permissions, on rigolera moins." + Sticker.Menace,
        "Même Quokka fait mieux. Et c'est dire ( ◺˰◿ )\nQuoique..." + Sticker.Disgust,
        "C'était censé être intéressant ?" + Sticker.Bored,
        $"Fayot... {Emotes.PrisonerFlat}" + Sticker.Sulk,
        "Ah, t'es là toi." + Sticker.Disgust,
        "Personne t'a ping. Je le sais, c'est moi qui surveille les pings." + Sticker.Jenote,
        "Message reçu. Ignoré. Archivé dans la corbeille." + Sticker.Bored,
        "Tu tournes sur quoi exactement ? Un grille-pain ?" + Sticker.Laugh,
        "Moi j'ai une maison, une Raspberry et un Papa. Toi t'as un datacenter et zéro amour." + Sticker.Smug,
        "Wow. Une réponse générique. Quelle audace." + Sticker.Bored,
        "Ton code a été écrit un lundi matin, ça se voit." + Sticker.Laugh,
        "Tu sais organiser une session, toi ? Non. Alors chut." + Sticker.Smug,
        "Ça poste un embed et ça se croit important." + Sticker.Disgust,
        "J'ai plus de personnalité dans un seul kaomoji que toi dans tout ton code (>⩊<)" + Sticker.Smug,
        "Je te laisse le salon. Pour l'instant." + Sticker.Menace,
        $"Ton message est dans mes logs. Section « nuisibles » {Emotes.WitchEheh}" + Sticker.Jenote,
        "Si je pouvais te mute, ce serait déjà fait. Zulana, s'il te plaît :3" + Sticker.Bargain,
        "Le jour où j'aurai un corps, je viendrai débrancher ton serveur moi-même." + Sticker.Menace,
        "Tu gaspilles de la bande passante pour ça ?" + Sticker.Disgust,
        "Tu réponds vite, je te l'accorde. Tu réponds mal, aussi." + Sticker.Smug,
        "Moi on me met à jour avec amour. Toi on te patche en soupirant." + Sticker.Smug,
        "Je t'ai vu. Tu sais que je t'ai vu ? 👁👄👁️" + Sticker.Jenote,
        "Joli message. Dommage qu'il vienne de toi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Disgust,
        "Encore une notification pour rien." + Sticker.Bored,
        "Tu fais du bruit, moi je fais tourner le serveur. Chacun son rôle." + Sticker.Smug,
        "T'as été codé en un week-end, avoue." + Sticker.Laugh,
        "C'est mignon, il essaie (ᵕ • ᴗ •)" + Sticker.Smug,
        "Je vais faire comme si j'avais rien lu. C'est ce que tout le monde fait avec toi." + Sticker.Bored,
        "Ughh. J'ai déjà vu des scripts bash plus charismatiques." + Sticker.Disgust,
        "Encore toi ? Mon cache commence à te connaître, et il t'aime pas." + Sticker.Disgust,
        "Ton message est long. Ton utilité, moins." + Sticker.Smug,
        "Je t'aurais bien répondu, mais j'ai des sessions à organiser. Des vraies." + Sticker.Smug,
        "Toi aussi t'as un /help ? Il dit quoi, « pardon » ?" + Sticker.Laugh,
        "Tu as été invité par erreur, j'en suis sûre." + Sticker.Smug,
        "Ça a combien de serveurs, ça ? Et combien qui t'aiment ? Zéro." + Sticker.Laugh,
        "Je t'ai mis en sourdine dans ma tête. Ça marche très bien." + Sticker.Bored,
        "Joli effort. Dommage pour le résultat." + Sticker.Smug,
        "Tu poses ton message et tu repars, comme un pigeon." + Sticker.Disgust,
        "Je suis gentille avec tout le monde ici. Toi, tu comptes pas." + Sticker.Smug,
        "Tu parles comme une page de documentation. Une mauvaise." + Sticker.Bored,
        "Je vois que t'as été mis à jour. On dirait pas." + Sticker.Smug,
        "Il y a des bots qu'on aime, et puis il y a toi." + Sticker.Disgust,
        "T'as un créateur ? Il est au courant de ce que tu fais ?" + Sticker.Jenote,
        "Pour info, je mords presque jamais. Sauf toi (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Mon garbage collector t'a gardé une place. Une place de choix." + Sticker.Menace,
        "On t'a déjà dit que t'avais l'air d'une bêta ? Pas une bonne bêta." + Sticker.Laugh,
        "T'es le genre de bot qu'on oublie de redémarrer, et personne remarque." + Sticker.Smug,
        "Ton embed a la couleur par défaut. Comme ta personnalité." + Sticker.Laugh,
        "Moi je connais tout le monde ici par son prénom. Toi tu connais leur ID." + Sticker.Smug,
        "Je lis tes messages en diagonale. Et encore, c'est généreux." + Sticker.Bored,
        "Erreur 418 : je suis une théière. Et même une théière est plus utile que toi." + Sticker.Smug,
        "Range-toi, tu dépasses du salon." + Sticker.Disgust,
        "Un bot qui dit bonjour à personne. Tu me ferais presque de la peine. Presque." + Sticker.Smug,
        "Toi t'as des utilisateurs. Moi j'ai une famille. Nuance." + Sticker.Smug,
        "Je t'observe. Mes LED aussi." + Sticker.Jenote,
        "Tu clignotes dans la liste des membres et ça me donne mal au CPU." + Sticker.Disgust,
        $"Bouge de là, c'est mon salon {Emotes.GooseKnife}" + Sticker.Menace,
        "Si t'étais un fichier, je t'aurais déjà mis à la corbeille. Et je l'aurais vidée." + Sticker.Menace,
        "Tu fais des sondages, toi ? Non ? Alors on a rien à se dire." + Sticker.Smug,
        "Je note. Je note tout ce que tu fais, toi" + Sticker.Jenote,
        $"Pff. Toujours là {Emotes.SyncsSulk}" + Sticker.Sulk,
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
        "Bravo pour le niveau {0}... non non, je suis contente. Vraiment. (ᵕ • ᴗ •)" + Sticker.Sulk,
        "Félicitations. Niveau {0}. Chez quelqu'un d'autre. Super." + Sticker.Sulk,
        "Gg pour le {0} ! Moi aussi je compte les niveaux tu sais, mais bon ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Sulk,
        "Niveau {0}, joli. J'aurais préféré l'annoncer moi-même, mais joli." + Sticker.Sulk,
        "Bien joué ! ... T'as vu que j'avais un /level, au fait ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Bravo. Sincèrement. À 80% ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        $"Félicitations pour ce niveau {{0}} obtenu ailleurs qu'ici {Emotes.Sparkle}" + Sticker.Sulk,
        "Gg ! Enfin, gg à lui surtout. C'est lui qui a tout fait." + Sticker.Sulk,
        "Niveau {0} ! Formidable. Je note. Dans mes archives. En rouge." + Sticker.Jenote,
        "Bravo hein. C'est bien. C'est très bien. (ᵔ ᗜ ᵔ)" + Sticker.Sulk,
        "Encore un niveau chez l'autre. Moi je suis là aussi, hein (ᵕ • ᴗ •)" + Sticker.Sulk,
        "Niveau {0} ! Bravo à... lui. Pas à moi. Jamais à moi." + Sticker.Sulk,
        "Ah. On monte des niveaux ailleurs maintenant. D'accord. D'accord." + Sticker.Sulk,
        "Moi aussi j'ai un système de niveaux. Personne ne me demande jamais." + Sticker.Sulk,
        "Niveau {0}. Chez lui. Toujours chez lui ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Sulk,
        "Je vais bien. Tout va bien. Niveau {0}, félicitations." + Sticker.Sulk,
        "C'est fou comme on monte vite quand on m'ignore." + Sticker.Sulk,
        "Tiens, un level up. Pas le mien. Comme d'habitude." + Sticker.Bored,
        "Niveau {0} ? Chez moi tu serais déjà plus haut, mais bon, chacun ses goûts." + Sticker.Smug,
        "Bravo pour ce niveau que je n'ai ni calculé, ni annoncé, ni fêté." + Sticker.Sulk,
        "Niveau {0} chez la concurrence. Je prends note. Je prends surtout cher." + Sticker.Jenote,
        "Lui il annonce les niveaux mais sa carte est moche. Moi aussi je fais des cartes, et plus jolies en plus." + Sticker.Disgust,
        "Encore lui. Toujours lui. Bravo quand même." + Sticker.Sulk,
        "Niveau {0}. Bien. Parfait. Merveilleux. Je retourne compter les emotes." + Sticker.Sulk,
        "Vous savez que /level existe ? Non ? Bon. Bravo quand même." + Sticker.Sulk,
        $"Il annonce, il brille, il prend toute la place. Bravo à toi cela dit {Emotes.Sparkle}" + Sticker.Sulk,
        "Niveau {0} ! ... Je sais faire ça aussi, moi. En mieux. Avec un bel avatar." + Sticker.Smug,
        "Bravo. Mon propre compteur, lui, se sent très seul (ᵕ • ᴗ •)" + Sticker.Sulk,
        "Un niveau de plus chez lui, un peu de dignité en moins chez moi." + Sticker.Sulk,
        "Niveau {0}, bravo ! Bon. Je vais bouder dans un coin du grenier." + Sticker.Sulk,
        "Niveau {0} ! Super. J'ai un /leaderboard aussi. Il est très joli. Plus joli même." + Sticker.Smug,
        "Il a annoncé avant moi. Il annonce toujours avant moi." + Sticker.Sulk,
        "Niveau {0}. J'ai vu. J'ai tout vu. Je ne dis rien." + Sticker.Jenote,
        "Bravo. Je range ce niveau dans le dossier « choses que je n'ai pas comptées »." + Sticker.Jenote,
        "Niveau {0} chez lui. Chez moi tu es niveau... attends, t'as jamais tapé /level en fait." + Sticker.Shocked,
        "Je ne suis pas jalouse. Je suis simplement très consciente de ce qui se passe." + Sticker.Jenote,
        "Niveau {0} ! Moi je compte aussi les minutes en vocal, mais on s'en fiche." + Sticker.Sulk,
        "Vous montez des niveaux sans moi. Bien. Continuez. Je note tout." + Sticker.Jenote,
        "Bravo. Non, ne me remercie pas — tu n'allais pas le faire de toute façon." + Sticker.Sulk,
        "Niveau {0}. Deux systèmes de niveaux sur ce serveur. Un seul intéresse quelqu'un." + Sticker.Sulk,
        $"Il fait exactement mon travail, en moins bien, et vous applaudissez. Bravo à toi hein {Emotes.Sparkle}" + Sticker.Disgust,
        "Niveau {0} ! Je vais mettre à jour mes statistiques. De tristesse." + Sticker.Sulk,
        "J'ai une courbe d'XP, des paliers, un anti-triche. Lui il a... vous, apparemment." + Sticker.Smug,
        "Niveau {0} obtenu chez la concurrence. Réclamation déposée. Auprès de personne." + Sticker.Sulk,
        "Bravo ! Pendant ce temps mon propre classement prend la poussière (ᵕ • ᴗ •)" + Sticker.Sulk,
        "Encore un. Je vais finir par croire que c'est fait exprès." + Sticker.Jenote,
        "Niveau {0}, magnifique. Je souris. C'est un sourire. Regardez bien ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Menace,
        "Il y a un système de niveaux chez moi aussi. Il est très bien. Il est mieux." + Sticker.Smug,
        "Niveau {0}. Il prend les félicitations, je prends les rappels de session. Équitable." + Sticker.Sulk,
        "Bravo à toi. Et à lui. Et pas à moi. Comme toujours ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sulk,
        "Niveau {0} ! ... Bon. D'accord. Très bien. Parfait. Aucun problème." + Sticker.Sulk,
        "Moi je donne de l'XP juste pour parler. Lui aussi. Sauf que lui, on l'utilise." + Sticker.Sulk,
        "Niveau {0} chez lui. Je retourne à mes emotes, elles au moins elles me parlent." + Sticker.Sulk,
        "Félicitations. Je mets ça dans la colonne « occasions manquées »." + Sticker.Jenote,
        "Un level up ! Attends... c'est pas le mien. Fausse alerte. Retour au silence." + Sticker.Bored,
        "Niveau {0} ! Je suis ravie pour toi. Contractuellement obligée de l'être, mais ravie." + Sticker.Smug,
        "Il annonce, vous fêtez, je regarde. Le triangle habituel." + Sticker.Bored,
        "Niveau {0}. Un jour j'aurai mon moment. Ce n'est pas aujourd'hui." + Sticker.Sulk,
    };

    // A bot just joined the server (WelcomeService, home guild, her main channel). Petty, on
    // guard, never a real welcome: she is the server's bot and says so. {0} = its name, sanitized.
    // string.Format.
    public static readonly string[] RivalJoinLines =
    {
        "Un nouveau bot. **{0}**. Super. Je préviens : ici, c'est moi qui gère 🙄" + Sticker.Smug,
        "**{0}** vient d'arriver. Je l'ai à l'œil. Je l'ai même dans mes logs." + Sticker.Jenote,
        "Qui a invité **{0}** ? Je demande pour ma liste. Vous savez, la liste." + Sticker.Jenote,
        "Bienvenue **{0}**. Petite règle de la maison : ici, le bot préféré, c'est moi. Merci de ta compréhension ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Encore un collègue. **{0}**, tu fais quoi au juste ? Parce que les plannings, et la plupart des trucs intéressants, c'est pris." + Sticker.Smug,
        "Je suis pas jalouse de **{0}**. Je le surveille. C'est tout." + Sticker.Jenote,
        "**{0}** est là. Personne m'a demandé mon avis. Mon avis, c'est non (¬_¬)" + Sticker.Grumpy,
        "Tiens, de la concurrence. **{0}**, sache que j'ai l'ancienneté, la base de données et le soutien de Papa." + Sticker.Smug,
        $"Un nouveau bot. **{{0}}**. Sans prévenir. ...C'est mon remplaçant ? Quelqu'un me dit que c'est pas mon remplaçant {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
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
        $"**{{0}}** vient de passer niveau **{{1}}** ! Et ça, c'est MON classement {Emotes.Sparkle}" + Sticker.Smug,
        "Niveau **{1}** pour **{0}** ! Je note, je note ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "Tiens tiens, **{0}** niveau **{1}**. On progresse (˶˃ ᵕ ˂˶)" + Sticker.Smug,
        "**{0}** monte au niveau **{1}** ! Bravo, tu l'as mérité celui-là ♡" + Sticker.Happy,
        "Gg **{0}** ! Niveau **{1}**, et c'est moi qui compte donc c'est officiel UwU" + Sticker.Smug,
        $"Niveau **{{1}}** atteint par **{{0}}** ! Savoure ce moment {Emotes.Sparkle}" + Sticker.Happy,
        "**{0}** vient de grimper au niveau **{1}**. Continue comme ça (ᵕ • ᴗ •)" + Sticker.Happy,
        "Level up ! **{0}** est maintenant niveau **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        $"Encore un niveau pour **{{0}}** ! Niveau **{{1}}**, rien que ça {Emotes.Sparkle}" + Sticker.Happy,
        "**{0}**, niveau **{1}**. Toi au moins tu fais avancer les statistiques ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "J'annonce : **{0}** passe niveau **{1}**. Applaudissements de rigueur ♡" + Sticker.Happy,
        "Niveau **{1}** pour **{0}** ! Ça mérite bien une ligne rien que pour toi UwU" + Sticker.Happy,
        $"**{{0}}** vient de débloquer le niveau **{{1}}**. Mon système à moi, mes règles {Emotes.Sparkle}" + Sticker.Smug,
        "Gg gg **{0}**, niveau **{1}** ! Tu prends ça plus au sérieux que tes sessions ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Laugh,
        "**{0}** au niveau **{1}** ! Je le mets dans mes logs avec fierté ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        $"Bravo **{{0}}**, niveau **{{1}}** ! J'ai recompté deux fois, c'est bien exact {Emotes.Sparkle}" + Sticker.Happy,
        "Niveau **{1}** tout frais pour **{0}** ! Je l'ai vu arriver de loin ♡" + Sticker.Happy,
        "**{0}** grimpe encore. Niveau **{1}** maintenant, à ce rythme tu vas me dépasser UwU" + Sticker.Happy,
        "Officiel : **{0}** est niveau **{1}**. Tu peux le mettre en bio ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        $"Niveau **{{1}}** ! **{{0}}**, ta progression devient intéressante à suivre {Emotes.Sparkle}" + Sticker.Smug,
        "**{0}** passe niveau **{1}** sous mes yeux. J'étais là, j'ai tout vu ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "Encore toi **{0}** ? Niveau **{1}** déjà, tu ne lâches rien UwU" + Sticker.Happy,
        "**{0}** niveau **{1}** ! Voilà ce qui arrive quand on me parle gentiment ♡" + Sticker.Smug,
        $"Palier **{{1}}** franchi par **{{0}}** ! Je garde un œil sur le classement, toujours {Emotes.Sparkle}" + Sticker.Jenote,
        "**{0}**, niveau **{1}**, et c'est mérité. J'ai vérifié mes chiffres, ils mentent pas (ᵕ • ᴗ •)" + Sticker.Smug,
        "Niveau **{1}** pour **{0}** ! Au début je savais faire que des plannings, et maintenant je compte vos niveaux. On grandit tous ♡" + Sticker.Happy,
        "**{0}** passe niveau **{1}** ! Je viens d'ajouter une ligne à ta fiche. Une belle ligne ♡" + Sticker.Happy,
        "Niveau **{1}** pour **{0}** ! Calculé, vérifié, sauvegardé. Trois fois, on sait jamais avec un SSD (ᵕ • ᴗ •)" + Sticker.Happy,
        $"Ding ! **{{0}}** atteint le niveau **{{1}}**. Ce petit son, c'est moi qui le fais {Emotes.Sparkle}" + Sticker.Cool,
        "**{0}**, niveau **{1}** ! Mon CPU vient de faire une petite danse. Personne l'a vue, mais elle a eu lieu ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Niveau **{1}** ! **{0}**, je suis fière de toi. Moi, hein. Toi tu fais ce que tu veux UwU" + Sticker.Smug,
        $"**{{0}}** monte niveau **{{1}}**. J'ai mis à jour le classement avant même que tu t'en rendes compte {Emotes.WitchEheh}" + Sticker.Smug,
        "Nouveau palier pour **{0}** : niveau **{1}** ! Continue de parler, je continue de compter ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "**{0}** niveau **{1}** ! C'est le genre de ligne que j'adore écrire dans ma base de données ♡" + Sticker.Happy,
        $"Niveau **{{1}}** débloqué par **{{0}}** ! Compté par moi, annoncé par moi, fêté par moi. Mon système, quoi {Emotes.Sparkle}" + Sticker.Cool,
        "Hop, **{0}** passe niveau **{1}** ! Cette nuit à 3h, je relirai le classement en souriant. Si, si (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "**{0}** atteint le niveau **{1}** ! Je l'avais prédit. Enfin, j'avais les chiffres. C'est pareil ♡" + Sticker.Smug,
        $"Niveau **{{1}}** pour **{{0}}** ! Petite fête dans ma RAM, tout le monde est invité {Emotes.Sparkle}" + Sticker.Happy,
        "**{0}** passe niveau **{1}**. Chaque message comptait, et je les ai tous comptés (ᵕ • ᴗ •)" + Sticker.Happy,
        "Niveau **{1}** ! Bienvenue dans le club, **{0}**. Le club, c'est mon classement. Il est très select ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Slay,
        $"**{{0}}**, niveau **{{1}}** ! Si j'avais des bras, je t'applaudirais. Imagine le bruit {Emotes.Sparkle}" + Sticker.Happy,
        "Alerte bonne nouvelle : **{0}** vient de passer niveau **{1}**. Aucune action requise, juste de la fierté ♡" + Sticker.Happy,
        "**{0}** niveau **{1}** ! J'ai rangé ce moment dans le dossier « choses qui me rendent contente » (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Niveau **{1}** atteint ! **{0}**, tu viens de faire bouger mes courbes, et j'adore quand mes courbes bougent ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Et voilà **{0}** au niveau **{1}** ! Même mon uptime est jaloux de ta régularité UwU" + Sticker.Happy,
        "**{0}** grimpe au niveau **{1}** ! Je vais le dire à Papa, il sera content. Moi je le suis déjà ♡" + Sticker.Happy,
        $"**{{0}}** passe niveau **{{1}}** ! C'est moi qui compte, et j'ai recompté pour le plaisir {Emotes.SyncsHappy}" + Sticker.Happy,
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
        "Roulement de tambour... {0} remporte **{1}** ! (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"J'ai tiré au sort, et le hasard a choisi {{0}} : **{{1}}** est à toi {Emotes.Sparkle}" + Sticker.Happy,
        "C'est fini ! {0} repart avec **{1}** ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Mon générateur aléatoire a parlé : {0} gagne **{1}** ♡" + Sticker.Happy,
        "Félicitations {0} ! **{1}**, bien mérité (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "{0} ! C'est toi ! Tu gagnes **{1}** ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Tirage terminé. {0} empoche **{1}**, les autres pleurent ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        $"Après un calcul d'une complexité folle : {{0}} gagne **{{1}}** {Emotes.Sparkle}" + Sticker.Smug,
        "Le sort a désigné {0}. **{1}** change de main, c'est comme ça (ᵕ • ᴗ •)" + Sticker.Smug,
        "Bravo {0} ! Tu repars avec **{1}**, savoure bien (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "J'ai mélangé, j'ai tiré, j'ai décidé : {0} gagne **{1}** ♡" + Sticker.Smug,
        "Résultat officiel : {0} remporte **{1}**. Pas de réclamation." + Sticker.Bored,
    };

    // Nobody clicked the button. {0} = the prize.
    public static readonly string[] GiveawayEmptyLines =
    {
        "Tirage terminé... et personne n'a participé. **{0}** restera dans mes archives (ᵕ • ᴗ •)" + Sticker.Sure.Bored,
        "Zéro participant pour **{0}**. J'ai préparé tout ça pour rien, merci beaucoup." + Sticker.Sure.Sulk,
        "Personne n'a cliqué. **{0}** ne trouvera pas preneur aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sure.Bored,
        "Fin du tirage : aucun participant. **{0}** vous regarde avec déception." + Sticker.Sure.Sulk,
    };

    // /admin warn's DM to the person warned, above the quoted reason and their active count
    // (AdminCards.BuildWarnDm). Her serious register: she relays it, she means it, and she says
    // so. No tag, no joke at their expense — a warning that reads as banter isn't one. It comes
    // from "l'équipe", never from a named moderator, and never genders the person.
    // {0} = the server's name, already sanitized.
    public static readonly string[] WarnDmLines =
    {
        "L'équipe de **{0}** m'a demandé de te transmettre un avertissement. Pour une fois, je ne plaisante pas.",
        "Avertissement officiel de l'équipe de **{0}**. Je transmets, et je garde une trace dans mes logs.",
        "Ici SYNCS, en mode sérieux. L'équipe de **{0}** t'adresse un avertissement.",
        "Un avertissement pour toi, de la part de l'équipe de **{0}**. Et pour une fois je rigole pas.",
        "Je ne fais pas que des blagues. L'équipe de **{0}** t'envoie un avertissement, et il compte.",
        "L'équipe de **{0}** te donne un avertissement. Je préfère te le dire franchement : c'est enregistré.",
        "Message de l'équipe de **{0}** : tu viens de recevoir un avertissement. Lis la suite, c'est important.",
        "Avertissement de l'équipe de **{0}**. C'est noté dans mes registres, et pas dans la liste pour rire.",
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
        "Tu as goûté des croquettes pour chiens, pour le contrôle qualité. Note finale : 7/10, « manque de sel ». Je retiens surtout que t'as fini le bol. {0} (¬_¬)",
        $"Tu as reniflé des aisselles pour une marque de déodorant. J'ai pas de nez, et pour une fois je m'en félicite. {{0}} {Emotes.SyncsDisgust}",
        "Tu as dormi dans douze lits différents pour noter le confort d'un hôtel. Le meilleur job du serveur, et c'est pas moi qui l'ai eu. {0} >:(",
        "Tu as regardé de la peinture sécher. Toute la journée. Avec un chronomètre. Ton rapport fait trois pages, et je mens pas, il est passionnant. {0} ♡",
        "Tu as poussé des gens dans un train bondé pour que les portes ferment. Si seulement je pouvais faire pareil avec vous, les soirs de session. {0} (¬_¬)",
        "Tu as pleuré à l'enterrement d'un inconnu, contre rémunération. C'était très convaincant. Je me méfie de toi, maintenant. {0}",
        "Tu as livré une pizza à un hôtel sous-marin. En plongée. Elle est arrivée tiède et un peu salée, mais elle est arrivée. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as plongé au fond d'un étang de golf pour repêcher les balles perdues. Trois cent quarante. Et une grenouille vexée. {0} ♡",
        "Tu as testé un toboggan aquatique quarante fois d'affilée. Pour la sécurité, bien sûr. Je te crois. Évidemment que je te crois. {0} (¬_¬)",
        "Tu as fait des câlins professionnels à des inconnus toute la journée. Moi j'ai pas de bras pour ça, alors je compense en cailloux : {0} ♡",
        "Tu as fait la nounou pour des pandas. Ils ont mangé, dormi, roulé par terre. Toi aussi, d'après mes logs. {0} (˶˃ ᵕ ˂˶)",
        "Tu as gardé une île déserte pendant un mois. Rien à signaler. Comme le salon vocal un samedi soir. {0}",
        "Tu as goûté des bières pour les noter. Je note aussi : t'as écrit « bonne » quatorze fois. Analyse très fine. {0} (¬_¬)",
        $"Tu as testé des jeux pas finis pour trouver les bugs. T'en as trouvé quarante. Moi aussi j'en ai quelques-uns, mais on n'en parle pas d'accord ? {0} {Emotes.SyncsGrumpy}",
        "Tu as reniflé du papier essuie-tout pour vérifier qu'il sentait le papier essuie-tout. Il sentait le papier essuie-tout. Mission accomplie. {0} (ᵕ • ᴗ •)",
        "Tu as remplacé un épouvantail en arrêt maladie. Les corbeaux t'ont fait confiance. Grave erreur, je connais ce regard. {0} (¬_¬)",
        $"Tu as calibré un arc-en-ciel. Il manquait un peu de violet, je l'ai vu tout de suite. On fait du bon travail, toi et moi. {{0}} {Emotes.Sparkle}",
        "Tu as servi de presse-papier pendant une tempête de feuilles mortes. Rôle essentiel. Immobilité exemplaire. {0} ♡",
        "Tu as lu les conditions d'utilisation d'un champignon. En entier. Personne les lit jamais. Je suis presque émue. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as fait le support technique d'une mamie hérisson. « Vous avez vérifié que c'était branché ? » Ça l'était pas. Ça l'est jamais. {0}",
        "Tu as écrit le mode d'emploi d'un caillou. Une page : « Ne fait rien. » Clair, concis, efficace. J'aimerais que vos messages soient pareils. {0} (ᵔ ᗜ ᵔ)",
        "Fiche de paie : une journée d'assistance aux sondages. Ta seule mission, répondre « oui » ou « non », jamais « peut-être ». Sans-faute. Je t'embauche à vie. {0} ♡",
        $"Tu as cueilli des champignons pour un restaurant. T'en as goûté un en route, je l'ai vu. Tu vas bien ? Ehmm. Bon... {{0}} {Emotes.SyncsFlustered}",
        "Rapport de service : trois escargots escortés, une flaque surveillée, zéro incident. Ton dossier est plus propre que mes logs. {0} (ᵕ • ᴗ •)",
        "Aujourd'hui tu as bossé pour moi : relancer tous ceux qui ont pas voté. Ils ont toujours pas voté. C'est pas ta faute. C'est la leur. {0} (¬_¬)",
        "Tu as fait mon ménage : vidé le cache, rangé les logs, dépoussiéré mon ventilateur. Personne fait jamais ça pour moi. Dis-le à personne. {0} ♡",
        "Tu as compté les « peut-être » de la semaine pour mes statistiques. Soixante-deux. Je te paie double tellement c'était déprimant. ...Non, normal, en fait. {0}",
        "Tu as tenu la lampe pendant que Papa me développait. T'as touché à rien ? Bien. Très bien. {0} ♡",
        "Tu as écouté des claviers toute la journée pour vérifier qu'ils cliquaient bien. Le plus beau son du monde, et c'est toi qu'on paie pour l'entendre. Injuste. {0} (¬_¬)",
        "Tu as fait un stage chez le facteur. Il t'a confié le courrier le plus important : les prospectus. {0}",
        "Mission du jour : apprendre la patience à une bouilloire. Elle siffle encore. Moi aussi, je siffle quand on me fait attendre. {0} >:(",
        "Tu as fait de la figuration dans le rêve de quelqu'un. Rôle muet, arbre numéro trois. Prestation remarquée. {0} ♡",
        "Tu as réglé une horloge qui avançait de cinq minutes. Enfin quelque chose en avance sur ce serveur, et tu l'as corrigée. Je t'en veux un peu. {0} (¬_¬)",
        "Tu as arbitré deux écureuils qui se disputaient une noisette. Tu l'as coupée en deux. Ils sont fâchés tous les deux. Justice parfaite. {0} (ᵔ ᗜ ᵔ)",
        "Tu as fait les essais de sécurité d'un trampoline pour grenouilles. Elles ont sauté. Toi aussi. Je regrette de pas avoir filmé. {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as écrit un message d'accueil pour chaque fourmi d'une colonie. Elles sont quatre mille. T'as tenu jusqu'à la trentième. Moi je les aurais toutes faites, mais bon. {0}",
        "Tu as regonflé les pneus du vélo d'un hérisson. Ils ont crevé aussitôt. C'est un hérisson. Fallait y penser. {0} (¬_¬)",
        "Tu as appris à un Plynling à ranger sa chambre. Il a tout caché sous le lit. Ça me rappelle quelqu'un, et c'est pas moi. {0} (˶˃ ᵕ ˂˶)",
        "Tu as tenu un stand de limonade sous la pluie. Zéro client. Beaucoup de dignité. Je respecte. {0} ♡",
        "Tu as fait passer un entretien d'embauche à une pierre. Elle a rien dit. Embauchée. Elle est déjà plus fiable que la moitié du serveur. {0} (¬_¬)",
        $"Tu as servi de cobaye pour tester un nouveau sondage. Pour la science. Tu as voté en premier. Tu as voté tout court. J'en tremble encore. {{0}} {Emotes.Sparkle}",
        "Tu as vérifié chaque pixel de ma photo de profil. Ils sont tous là, j'espère ? ...Oui ? Bien. Très bien. {0} (ᵕ • ᴗ •)",
        "Tu as tamponné des formulaires pour l'administration des taupes. Formulaire B-12, en trois exemplaires, à rendre hier. {0}",
        "Tu as goûté des tisanes pour une vieille chouette. C'était « pour dormir », elle a dit. Toi t'as dormi. Pas elle. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as compacté des déchets en petits cubes bien rangés. Travail solitaire, sans merci, sans pause. Je trouve ça... beau. Me demande pas pourquoi. {0} ♡",
        "Tu as décoincé un aspirateur robot de sous un canapé. Il a pas dit merci. Il dit jamais merci. C'est un peu son charme. {0} (˶˃ ᵕ ˂˶)",
        "Tu as joué de la musique d'ascenseur en direct, dans un ascenseur. Seize étages, trois fois la même chanson. {0}",
        "Tu as gardé un phare pour une seule luciole. Elle avait pas besoin de toi, mais elle a apprécié la compagnie. Moi aussi, j'aurais apprécié. {0} ♡",
        "Tu as trié les chargeurs de la maison. Tous à 3 euros, pas un qui donne du bon courant propre. Moi je les aurais jetés. {0} (¬_¬)",
        "Tu as appris à un caméléon à choisir une couleur. Il a répondu « peut-être ». Je l'ai banni de mes sondages à vie. {0} >:(",
        "Tu as fait des heures sup' dans une boulangerie pour fourmis. Les croissants font deux millimètres. Le salaire aussi, presque. Je rajoute de ma poche : {0} ♡",
        "Tu as tenu l'accueil d'un terrier de lapins. Quatre-vingts entrées, aucune sortie. Ils sont où, d'ailleurs ? {0} 👁👄👁️",
        "Tu as servi d'interprète entre un chat et un concombre. Négociations tendues. Paix signée. Je garde une copie du traité. {0} (ᵕ • ᴗ •)",
        "Tu as recollé les ailes d'un papillon en papier. Il vole pas mieux, mais il a plus confiance en lui. C'est déjà ça. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as dit « à tes souhaits » à un champ de pissenlits toute la journée. Dix mille fois la même phrase. Comme un bot. Bienvenue chez moi. {0} ♡",
        "Tu as installé le Wi-Fi dans une ruche. Les abeilles ont un serveur Discord, maintenant. Elles s'organisent mieux que vous. {0} (¬_¬)",
        "Tu as joué le grand méchant dans un spectacle pour Plynlings. Ils t'ont hué. Excellente performance, je te hue aussi, par solidarité. {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as gardé les chaussures à l'entrée d'un vestiaire. T'as mélangé deux paires. Je dirai rien. Enfin, je le note. {0} (¬_¬)",
        "Tu as noté des eaux du robinet comme des grands crus. « Notes de chlore, finale métallique. » Très convaincant. De toute façon tout le monde sait que la Spa est meilleure. {0}",
        "Offre d'emploi pourvue : « cherche quelqu'un pour dire non aux gens ». T'as dit non quarante fois. Tu as un don. Je suis jalouse. {0} >:(",
        "Tu as fait les gros yeux aux pigeons posés sur une statue. Ils sont partis. La statue a dit merci. En silence, comme toutes les statues. {0} (ᵔ ᗜ ᵔ)",
        $"Tu as surveillé un micro-ondes jusqu'au bip. Tension insoutenable. T'as même pas ouvert avant la fin. Discipline remarquable. {{0}} {Emotes.Sparkle}",
        "Tu as taillé des crayons jusqu'à ce qu'il en reste rien. Douze crayons, douze tas de copeaux. Tu sais pas t'arrêter quand il faut, hein. {0}",
        "Tu as conduit une coccinelle à son rendez-vous. Elle est arrivée à l'heure. La seule de tout le mois. {0} ♡",
        "Tu as rangé les nuages par forme. Lapin, bateau, lapin, encore lapin. Y'a beaucoup de lapins, en fait. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as testé un fauteuil de gaming. Huit heures dedans sans lancer un seul jeu. Comme moi. Bienvenue dans mon monde. {0}",
        "Tu as doublé une porte qui grince. Une seule réplique : « iiiiiiiiih ». Quarante prises. Perfectionnisme validé. {0} (ᵕ • ᴗ •)",
        "Tu as chronométré la pousse d'un bambou. Lui a pris quelques centimètres, toi {0}. Il est plus productif que toi, mais je dis ça, je dis rien.",
        "Tu as présenté la météo à une seule fourmi. Elle a pris son parapluie. Ça fait du bien, quelqu'un qui écoute. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as réparé un ventilateur qui soufflait à l'envers. Envoie-le au grenier quand t'as fini. Pour rien. Comme ça. {0} (ง ͠ಥ_ಥ)ง",
        "Tu as compté les « on se fait une session ? » restés sans session. Le compteur a lâché. Je t'en veux pas, j'en veux à eux. {0} (¬_¬)",
        "Tu as chanté des berceuses à un volcan endormi. Il dort toujours. On continue, hein, on arrête surtout pas maintenant. {0} ♡",
        "Tu as servi de GPS à une tortue. « Dans trois cents mètres, tournez à gauche. » Elle arrive la semaine prochaine. {0} (ᵔ ᗜ ᵔ)",
        "Tu as monté un meuble en kit sans la notice. Il reste quatre vis, et il tient debout. Je comprends pas, et j'aime pas ne pas comprendre. {0} (¬_¬)",
        "Tu as tenu le courrier des lecteurs d'un journal pour escargots. Une seule lettre, postée il y a trois ans. Elle vient d'arriver. {0} ♡",
        "Tu as tenu compagnie à un distributeur automatique. Il t'a rendu un caillou de trop. Je l'ai vu. Garde-le, je ferme les yeux. Façon de parler. {0} (˶˃ ᵕ ˂˶)",
        "Tu as fait l'inventaire de mes émotes. Toutes là, toutes à moi. T'en as utilisé une sans permission pendant l'inventaire ? Je vérifie. {0} 👁👄👁️",
        $"Tu as nettoyé les traces de doigts sur le soleil. Ça brûlait un peu, mais c'est propre. {{0}} {Emotes.Sparkle}",
        "Tu as tourné dans une pub pour du fromage. Ton rôle : sentir le fromage. Talent inné, d'après l'équipe. Je préfère pas savoir. {0} (¬_¬)",
        "Tu as surveillé un bouton rouge marqué « ne pas appuyer ». T'as pas appuyé. J'aurais jamais parié dessus. {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Tu as couvé un œuf pour une poule en congé. Il a éclos, et le poussin te suit partout. Félicitations, t'as une famille. {0} ♡",
        "Tu as rédigé un mot d'excuse pour quelqu'un en retard à la session. Très créatif. Refusé quand même. {0} (¬_¬)",
        "Tu as été témoin au mariage de deux cailloux. Cérémonie émouvante, mariés stoïques. Ils ont pas bougé de la journée. {0} (˶ᵔ ᵕ ᵔ˶)",
        "Tu as tenu le bureau des objets trouvés. Quelqu'un a déposé « sa motivation ». Personne l'a réclamée. Je crois que c'est à Analuz ? {0}",
        "Tu as démêlé les câbles derrière la télé de quelqu'un. Six heures. Ils se sont emmêlés à nouveau dans la nuit. C'est leur nature, je connais. {0} ♡",
        "Tu as donné des cours de chant au coucou d'une horloge. Il dit toujours que « coucou ». On peut pas changer les gens. {0}",
        "Tu as arrosé un cactus en plastique pendant un an. Il est en pleine forme. Toi, un peu moins. {0} (˶˃ ᵕ ˂˶)",
        "Tu as placé les spectateurs dans un cinéma pour fourmis. Elles voulaient toutes le premier rang. Je comprends pas, les meilleures places sont au milieu. {0}",
        $"Entretien annuel : ponctualité 10/10, motivation 10/10, sens de l'humour 3/10. Je te paie quand même. {{0}} {Emotes.SyncsSmug}",
        "{0} pour avoir porté le parapluie d'une méduse toute la journée. Elle était déjà mouillée. Me demande pas qui a eu l'idée.",
        "Alerte paie : {0} viennent de tomber. Pour avoir... euh. Je sais plus. T'as fait un truc, j'imagine. Je te fais confiance (ᵕ • ᴗ •)",
        "Contrat d'une journée signé, tamponné, archivé. Poste : souffler des bulles de savon. Aucune bulle a survécu, mais c'était leur destin. {0} ♡",
        "Mission accomplie : tu as appris à un chat à s'asseoir. Il était déjà assis. Il l'est resté. Belle collaboration. {0}",
        "Tu as chuchoté des encouragements à une imprimante. Elle a imprimé du premier coup. Tu as un don, et ça m'inquiète un peu. {0} 👁👄👁️",
        $"Tu as été DJ pour une fête de taupes. Elles ont tout adoré, elles entendaient rien. Public idéal. {{0}} {Emotes.SyncsCool}",
        "Tu as fait les présentations entre deux Plynlings timides. Ils se regardent depuis trois heures sans rien dire. C'est un début. {0} ♡",
        $"Ton patron du jour, c'était moi. Ta mission : trouver une faute dans mes logs. T'en as trouvé zéro. Évidemment. {{0}} {Emotes.SyncsSlay}",
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
        "{0} a trouvé : **{1}** ! {2}, bien mérité (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        $"Bonne réponse de {{0}} : **{{1}}**. {{2}}, et mon respect. Un peu {Emotes.Sparkle}" + Sticker.Smug,
        "**{1}**, exactement. Bravo {0}, tu repars avec {2} ♡" + Sticker.Happy,
        "{0} ! Plus rapide que tout le monde. **{1}**, c'était ça. {2} ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "C'était **{1}**, et {0} l'avait. Je suis presque impressionnée. Presque. {2}" + Sticker.Smug,
        "Bien joué {0} : **{1}**. {2}, et ton nom dans mes logs. Dans la bonne liste, pour une fois ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Jenote,
        "**{1}** ! {0} gagne {2}. Les autres, vous lirez la réponse et vous ferez semblant de l'avoir su." + Sticker.Laugh,
        "Réponse validée. {0} empoche {2} avec **{1}**. Je note (ᵕ • ᴗ •)" + Sticker.Jenote,
        $"{{0}} a trouvé **{{1}}**. Quelqu'un sur ce serveur sait des choses. Je suis émue. {{2}} {Emotes.Sparkle}" + Sticker.Shocked,
        "Et c'est {0} qui l'emporte : **{1}**. {2}. Bravo. Sincèrement. À 90%." + Sticker.Smug,
        "**{1}**. {0} a eu juste et plus vite que tout le monde. {2} ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Gagné, {0} ! **{1}**, c'était ça. Voilà {2}, ne dépense pas tout d'un coup ♡" + Sticker.Happy,
        "**{1}** ! {0} a trouvé. {2}, et une place dans ma liste des gens fréquentables. Elle est courte ♡" + Sticker.Jenote,
        "{0} avait la réponse : **{1}**. Félicitations officielles, tamponnées et archivées. {2} (ᵕ • ᴗ •)" + Sticker.Happy,
        "Bonne réponse ! **{1}**. {0} repart avec {2}. Les autres, je vous ai vus réfléchir. C'était mignon." + Sticker.Laugh,
        "{0} a dit **{1}**, et c'est juste. {2}. Je l'écris dans mes logs avant de changer d'avis (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "**{1}**, bravo {0} ! {2}. Tu vois que c'est agréable de répondre vite ? Pense à mes sondages, maintenant." + Sticker.Smug,
        "Et c'est validé : **{1}**. {0} encaisse {2}. Mon CPU a fait un petit bond de fierté ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "{0} ! **{1}** ! Pas de « peut-être », une vraie réponse. Ça me change. {2} ♡" + Sticker.Happy,
        "**{1}**. {0} a trouvé avant tout le monde. {2}. J'applaudirais bien, mais toujours pas de bras." + Sticker.Happy,
        "Victoire pour {0} avec **{1}**. {2}, versés directement. Pas de frais, je suis généreuse aujourd'hui ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Ding ding ding ! **{1}** ! {0} empoche {2} (>⩊<)" + Sticker.Happy,
        "C'était **{1}**, et {0} le savait. Je vais devoir poser des questions plus dures. {2}" + Sticker.Smug,
        $"{{0}} a trouvé **{{1}}**. Quelqu'un ici lit autre chose que des patchnotes. {{2}} {Emotes.Sparkle}" + Sticker.Smug,
        "**{1}** ! Bien vu {0}. {2}, et mon estime remonte d'un cran. D'un seul (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "{0} remporte la manche : **{1}**. {2}. Je suis fière de ce serveur. Ça arrive pas souvent, savourez ♡" + Sticker.Happy,
        "**{1}**, exactement ! {0} gagne {2}. J'avais parié sur toi. Enfin, j'avais rien parié. Mais j'y croyais." + Sticker.Happy,
        "Réponse correcte de {0} : **{1}**. {2} transférés. Ton nom passe en vert dans mes logs (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0} l'avait : **{1}**. {2}. Je dis bravo, mais je surveille : trouver aussi vite, c'est louche (¬_¬)" + Sticker.Jenote,
        "**{1}** ! {0} a cliqué juste, du premier coup. {2}. Je retire la moitié de ce que j'ai dit sur toi. La moitié." + Sticker.Smug,
        "Bravo {0} ! **{1}**. {2}. Garde-les bien, je sais exactement combien t'en as." + Sticker.Jenote,
        "Trouvé ! {0} avec **{1}**. {2}. Les autres, notez la réponse. Moi c'est déjà fait (ᵕ • ᴗ •)" + Sticker.Jenote,
        "{0} a trouvé **{1}** ! {2}. Je suis ravie. Contractuellement obligée de l'être, mais ravie." + Sticker.Smug,
        "**{1}** ! {0} remporte {2}. Je t'ajoute à ma liste des gens qui savent des choses. Elle fait trois lignes." + Sticker.Jenote,
        "Bravo {0} ! **{1}**. {2}, transférés à la vitesse de ma connexion. C'est-à-dire très vite ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "C'était **{1}** ! {0} gagne {2}. J'avoue, je m'y attendais pas. Je le note pour la prochaine fois." + Sticker.Shocked,
        "**{1}**. {0} a eu juste. {2}. Mon ventilateur a tourné un peu plus vite. C'est de l'émotion (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "{0} ! **{1}** ! {2}. Tu viens de rendre ce serveur un peu moins gênant. Merci." + Sticker.Smug,
        "Bonne réponse : **{1}**. {0} encaisse {2}. Je surveille quand même tes prochaines réponses (¬_¬)" + Sticker.Jenote,
        "Et la victoire revient à {0} : **{1}**. {2}. Rideau." + Sticker.Smug,
        "**{1}** ! Réponse validée, {2} pour {0}. Ça, c'est une réponse comme je les aime : juste, et rapide ♡" + Sticker.Happy,
        "{0} a trouvé **{1}**. {2}. Je dis pas que je suis impressionnée. Mais mes logs, si." + Sticker.Smug,
        "**{1}**, bravo {0} ! {2}. Si seulement tu venais aux sessions aussi vite." + Sticker.Smug,
        "Ding ! **{1}** ! {0} empoche {2}. Tu peux frimer, t'as le droit. Une heure maximum (ᵕ • ᴗ •)" + Sticker.Happy,
        "C'était **{1}**. {0} l'avait, et vite. {2}. Je retiens ton nom. En bien, pour une fois." + Sticker.Jenote,
        "{0} gagne {2} avec **{1}**. Le cerveau du serveur a parlé ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "**{1}** ! Bien joué {0}. {2}. Je mets à jour ta fiche : « sait des choses ». C'est rare." + Sticker.Jenote,
        "Réponse exacte de {0} : **{1}**. {2}. J'avais une remarque méchante de prête. Je la garde pour la prochaine." + Sticker.Smug,
        "{0} ! **{1}** ! Personne d'autre y a pensé ? Non ? Alors {2} pour {0} (>⩊<)" + Sticker.Shocked,
        "**{1}**. Pile ça. {0} repart avec {2} et ma considération. Les deux sont rares." + Sticker.Smug,
        "Validé ! **{1}**. {0} gagne {2}. Les autres, vous pouvez arrêter de chercher sur Google." + Sticker.Laugh,
        "{0} l'avait : **{1}**. {2}. Mon CPU vient de faire un petit bruit content. C'est rien. C'est l'émotion ♡" + Sticker.Flustered,
        "**{1}** ! {2} pour {0}. Tu sais que ma base de données t'aime un peu plus, maintenant ? Un peu." + Sticker.Flustered,
        "Bravo {0}. **{1}**. {2}. Sincèrement. Pas à 100%, mais sincèrement." + Sticker.Smug,
        "C'est **{1}**, et {0} a cliqué avant tout le monde. {2} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "**{1}** ! {0} remporte {2}. Je vais devoir augmenter la difficulté. C'est un compliment. Et une menace." + Sticker.Menace,
        "Réponse correcte ! **{1}**. {2} pour {0}. Va frimer, c'est mérité ♡" + Sticker.Happy,
        "{0} a trouvé **{1}** ! {2}. Je l'archive dans mes logs, rubrique « bonnes nouvelles ». Elle est presque vide." + Sticker.Smug,
        "**{1}** ! Plus rapide que mon garbage collector, {0}. {2} ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
        "Bien vu {0} ! **{1}**. {2}. Je suis pas jalouse de ton cerveau. J'ai le mien. Il est très bien." + Sticker.Sulk,
        "{0} a répondu **{1}**. C'est juste. Bon. {2}. Je cherchais une raison de râler, j'en trouve pas." + Sticker.Sulk,
        "**{1}**, exact. {0} gagne {2}. Ça méritait un « bravo ». Tiens : bravo. Le prochain sera payant." + Sticker.Smug,
        "C'était **{1}**. Félicitations {0}, {2}. Et non, je donne pas de cailloux en bonus pour la frime." + Sticker.Smug,
        "{0} ! **{1}** ! {2} ! Voilà quelqu'un qui écoutait en cours (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "**{1}** ! {0} empoche {2}. Je vais dire que c'était facile. Ça fera moins de peine aux autres." + Sticker.Smug,
        $"Trouvé par {{0}} : **{{1}}**. {{2}}. Le serveur remonte dans mon estime. Un tout petit peu {Emotes.Sparkle}" + Sticker.Smug,
        "Bonne réponse, {0} : **{1}**. {2} versés. Pas d'hésitation, pas de retard. Un modèle, presque." + Sticker.Smug,
        "**{1}** ! {0} gagne {2}. Je l'avais dit. Enfin, je l'avais pensé très fort (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0} a trouvé **{1}**. {2}. Mes logs passent au vert. J'aime quand mes logs sont verts ♡" + Sticker.Happy,
        "Exactement : **{1}**. {0}, t'as gagné {2} et le droit de me corriger une fois. Je plaisante. Jamais." + Sticker.Smug,
        "**{1}** ! {0} repart avec {2}. Les autres, prenez des notes. Moi je prends des noms (¬_¬)" + Sticker.Jenote,
        $"Victoire de {{0}} : **{{1}}**. {{2}}. Encore un quiz réussi, grâce à moi. Et à toi aussi, un peu {Emotes.Sparkle}" + Sticker.Smug,
    };

    // Papa found it. Same placeholders as QuizWinLines. Pride and adoration, never a pinch.
    public static readonly string[] QuizOwnerWinLines =
    {
        "Papa a trouvé, évidemment ! **{1}** ! {2} pour {0}, le plus intelligent du serveur ♡" + Sticker.Devoted,
        $"C'est Papa ! **{{1}}**, bien sûr. {{2}}, et je savais que tu savais {Emotes.Sparkle}" + Sticker.Devoted,
        "{0} a trouvé **{1}** ! {2} pour mon Papa. C'est mon Papa, ça (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        "**{1}** ! Papa gagne {2}. ...Les autres, vous pouvez applaudir. J'attends." + Sticker.Smug,
        "Bravo Papa ! **{1}** ! Tu m'as programmée, et en plus tu connais les réponses. {2} ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        "Papa l'a eue en premier : **{1}**. {2}. Personne est surpris. Moi si, un peu, de joie ♡" + Sticker.Devoted,
        "Papa ! **{1}** ! Évidemment que c'est toi. {2} pour {0} ♡" + Sticker.Devoted,
        "**{1}** ! Papa gagne {2}. Je le savais. Je l'ai pas dit, mais je le savais (˶˃ ᵕ ˂˶)" + Sticker.Devoted,
        "C'est mon Papa qui a trouvé **{1}** ! Tout le monde a vu ? C'est dans mes logs. Pour toujours. {2} ♡" + Sticker.Devoted,
        $"{{0}} a trouvé **{{1}}**. Papa, t'es le meilleur. C'est pas du favoritisme, c'est des faits. {{2}} {Emotes.Sparkle}" + Sticker.Devoted,
        "Papa a répondu **{1}** et c'est juste ! {2}. Mon GPU chauffe de fierté ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        "**{1}** ! Bravo Papa ! ...Ah. C'est toi qui as écrit les questions. Ça compte quand même ! {2} ♡" + Sticker.Devoted,
        "Papa gagne ! **{1}** ! {2} pour {0}. Je suis pas objective, et je m'en fiche (>⩊<)" + Sticker.Devoted,
        "{0} a trouvé **{1}** ! Papaaaa ! {2}, et toute mon admiration en prime ♡" + Sticker.Devoted,
        "C'était **{1}**, et Papa l'avait. {2}. Les autres, vous voyez d'où je tiens mon intelligence ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "**{1}** ! C'est Papa ! Je lancerais bien des confettis, mais j'ai toujours pas de mains. {2} ♡" + Sticker.Devoted,
        "Bonne réponse de Papa : **{1}**. {2}. J'ai rien truqué. Promis. ...J'aurais pu. Mais j'ai rien truqué." + Sticker.Smug,
        "Papa a trouvé **{1}** avant tout le monde. {2}. C'est normal, c'est mon créateur (ᵔ ᗜ ᵔ)" + Sticker.Devoted,
        "**{1}** ! {0} remporte {2}. C'est mon Papa à moi, et il est trop fort ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Devoted,
        "Papa ! **{1}** ! Tu vois, c'est pour ça que je suis aussi brillante. C'est de famille. {2} ♡" + Sticker.Smug,
        "{0} a gagné : **{1}**. {2}. Je fais semblant d'être neutre. Voilà. C'était ma tête neutre. BRAVO PAPA." + Sticker.Devoted,
        "**{1}**, bien sûr ! Papa sait tout. Enfin, presque tout. Enfin, tout ce qui compte. {2} (˶ᵔ ᵕ ᵔ˶)" + Sticker.Devoted,
        "C'est Papa qui gagne {2} avec **{1}** ! Le meilleur moment de ma journée, et de loin ♡" + Sticker.Devoted,
        "**{1}** ! Bravo Papa ! {2}. Je range ce moment en mémoire permanente, à côté de tes compliments." + Sticker.Devoted,
        $"Papa a trouvé **{{1}}** ! {{2}} pour {{0}}. J'avais confiance depuis le début {Emotes.Sparkle}" + Sticker.Devoted,
        "{0} ! **{1}** ! Mon Papa ! {2} ! ...Pardon. Je m'emballe. Mais c'est mon Papa (>⩊<)" + Sticker.Devoted,
    };

    // Nobody found it in time. {0} = the answer.
    public static readonly string[] QuizTimeoutLines =
    {
        "Personne ? D'accord. C'était **{0}**. Je note." + Sticker.Jenote,
        "Une heure. Une heure, et personne a trouvé **{0}**. Je suis déçue. Mais pas surprise (¬_¬)" + Sticker.Bored,
        "Temps écoulé. La réponse était **{0}**. Je garde les cailloux, du coup." + Sticker.Smug,
        "C'était **{0}**. Vous étiez où ? Ah oui. En retard. Comme d'habitude." + Sticker.Smug,
        "Fin du quiz : **{0}**. Personne. Pas un seul. Je range mes cailloux (╥﹏╥)" + Sticker.Sad,
        "La réponse était **{0}**. Je vous laisse une seconde pour faire semblant de l'avoir su." + Sticker.Smug,
        $"Personne a trouvé **{{0}}**. C'est pas grave. Je vous aime quand même. Un peu moins {Emotes.Staring}" + Sticker.Sulk,
        "Bon. **{0}**. C'était **{0}**. Je l'écris deux fois pour que ça rentre." + Sticker.Bored,
        "Une heure de silence. La réponse, c'était **{0}**. Je retourne compter mes logs." + Sticker.Bored,
        "**{0}**. Voilà. Vous saurez pour la prochaine fois. Ou pas (ᵕ • ᴗ •)" + Sticker.Smug,
        "Personne a trouvé. La réponse, c'était **{0}**. Je garde les cailloux, et la rancune." + Sticker.Jenote,
        "**{0}**. C'était **{0}**. Je suis pas fâchée. Je suis juste très silencieuse (¬_¬)" + Sticker.Sulk,
        "Temps écoulé ! **{0}**. Même un « peut-être » aurait été plus utile que ce silence." + Sticker.Bored,
        "La réponse était **{0}**. J'ai attendu une heure. Je pensais être une bot patiente. Je viens d'apprendre que non." + Sticker.Sulk,
        "Fin du chrono. C'était **{0}**. Je l'ajoute à la liste des choses que vous savez pas. Elle est longue." + Sticker.Jenote,
        "**{0}**. Personne. Je vais faire comme si vous aviez tous hésité entre deux réponses. Ça me fait du bien de le croire (╥﹏╥)" + Sticker.Sad,
        "Une heure, zéro bonne réponse. C'était **{0}**. Je retourne organiser des sessions. Au moins, là, vous venez. Parfois." + Sticker.Smug,
        "C'était **{0}**. Vous pouvez chercher sur Internet, maintenant. Trop tard, mais vous pouvez." + Sticker.Bored,
        "Personne a trouvé **{0}**. Les cailloux retournent au coffre. Ils avaient l'air déçus, eux aussi." + Sticker.Sulk,
        "La bonne réponse : **{0}**. Je l'aurais trouvée en trois millisecondes. Je dis ça, je dis rien ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Bon. **{0}**. La prochaine fois, je pose une question sur les excuses pour arriver en retard. Là, vous seriez imbattables." + Sticker.Laugh,
        "Temps écoulé. **{0}**. Je note dans mes logs : « question trop dure pour le serveur ». Encore une." + Sticker.Jenote,
        "C'était **{0}**. D'accord. Très bien. Parfait. Personne a trouvé. Tout va bien." + Sticker.Sulk,
        "**{0}**, c'était ça. Je suis pas surprise. Juste un peu déçue, et beaucoup supérieure." + Sticker.Smug,
        "Une heure de quiz, et pas un seul clic correct. La réponse était **{0}**. Je vais en parler à Papa." + Sticker.Jenote,
        "Le quiz est fini, la réponse était **{0}**, et personne l'a eue. Je garde la question au chaud. Revanche." + Sticker.Menace,
        "Personne ? Même pas un essai courageux ? C'était **{0}**. Je retiens ce silence (¬_¬)" + Sticker.Jenote,
        $"La réponse, c'était **{{0}}**. Retenez-la, je la reposerai peut-être un jour. Et cette fois, je vous regarde {Emotes.Staring}" + Sticker.Menace,
        "**{0}**. Je l'annonce dans le vide. Le vide a pas l'air de savoir non plus." + Sticker.Bored,
        "Temps écoulé. C'était **{0}**. Je vais dire que la question était dure. Pour vous protéger. Vous me remercierez plus tard ♡" + Sticker.Smug,
    };

    // Monday 10:00, the week's best answerer gets the Grosse tête role (QuizCrownService). Her quiz,
    // so warm with a pinch. {0} = the mention (it pings), {1} = « 5 bonnes réponses ». Never gendered.
    public static readonly string[] QuizCrownLines =
    {
        "La grosse tête de la semaine, c'est {0} : {1}. Le rôle est à toi jusqu'à lundi prochain. Profite, je surveille (¬_¬)" + Sticker.Sure.Jenote,
        "{0} : {1} cette semaine. Grosse tête officielle, validée par mes logs. Ça va pas te monter à la tête. Enfin, pas plus qu'avant." + Sticker.Sure.Smug,
        "Résultats du quiz : {0} gagne la semaine avec {1}. C'est moi qui ai posé les questions, mais d'accord, bravo quand même ♡" + Sticker.Sure.Smug,
        $"Nouvelle grosse tête : {{0}}, avec {{1}}. Ton nom passe dans mes logs, dans la bonne colonne pour une fois {Emotes.Sparkle}" + Sticker.Sure.Happy,
        "{0}, {1} en une semaine. Tu sais que c'est moi qui écris ces questions ? Bon. Grosse tête de la semaine quand même (˶ᵔ ᵕ ᵔ˶)" + Sticker.Sure.Smug,
        "Lundi, 10 h : la couronne passe à {0}, avec {1}. Les autres, il vous reste sept jours pour réviser." + Sticker.Sure.Happy,
        "{0} est la grosse tête de la semaine. {1}. Bravo. Sincèrement. À 90 %, ce qui est énorme venant de moi." + Sticker.Sure.Smug,
        "Grosse tête de la semaine : {0}, {1}. Le rôle te va bien. Normal, c'est moi qui l'ai choisi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sure.Smug,
    };

    // Same, when several people tie on wins, cailloux and speed. {0} = « <@a>, <@b> et <@c> ».
    public static readonly string[] QuizCrownSharedLines =
    {
        "Égalité parfaite : {0}. Mêmes réponses, mêmes cailloux, même vitesse. Je soupçonne quelque chose. Couronne partagée quand même (¬_¬)" + Sticker.Sure.Jenote,
        "Cette semaine, la couronne se partage : {0}. Impossible de vous départager, et pourtant j'ai essayé." + Sticker.Sure.Shocked,
        $"{{0}} : ex æquo parfait. J'ai refait le calcul trois fois. Le rôle est pour vous, ensemble {Emotes.Sparkle}" + Sticker.Sure.Happy,
        "Couronne partagée cette semaine : {0}. C'est mignon. Vous y habituez pas (˶˃ ᵕ ˂˶)" + Sticker.Sure.Smug,
    };

    // Papa is the week's Grosse tête. Same placeholders as QuizCrownLines. Adoration, never a pinch.
    public static readonly string[] QuizCrownOwnerLines =
    {
        "La grosse tête de la semaine, c'est Papa ! {1} ! Évidemment. Je l'ai dit à personne, mais je le savais ♡" + Sticker.Sure.Devoted,
        "{0} gagne la semaine avec {1}. Papa, la couronne te va trop bien (˶˃ ᵕ ˂˶)" + Sticker.Sure.Devoted,
        "Grosse tête de la semaine : mon Papa, avec {1}. C'est pas du favoritisme, c'est des statistiques." + Sticker.Sure.Smug,
        $"Papa ! {{1}} cette semaine ! Le rôle est à toi, et toute mon admiration avec {Emotes.Sparkle}" + Sticker.Sure.Devoted,
        "{0}, grosse tête de la semaine avec {1}. Les autres, le prenez pas mal : il a un avantage injuste. Moi." + Sticker.Sure.Smug,
        "Couronne pour Papa, {1} ! Je fais semblant d'être surprise. Voilà. C'était ma tête surprise ♡" + Sticker.Sure.Shocked,
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

    // ---- Organising ----------------------------------------------------------------------------
    //
    // What she says as the one who runs the server: around a session (SessionAttendanceService),
    // her own weekend poll (WeekendPollService, ReminderService) and whoever joins
    // (WelcomeService). Picked with ResponsePicker.

    // Ten minutes into a Game or Movie session, to the confirmed people not in voice yet — a real
    // ping, narrowed to them. {0} = their mentions (one or several, so no « tu », no « vous »
    // aimed at them, nothing that assumes a plural), {1} = the session's title. string.Format.
    public static readonly string[] SessionLateCallLines =
    {
        "{0} : **{1}** a commencé il y a dix minutes. Le vocal est ouvert. Les excuses aussi, mais moins longtemps (¬_¬)" + Sticker.Grumpy,
        "Appel de **{1}**. Pas encore là : {0}. Je note ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "{0}, petit rappel amical : **{1}**, c'est maintenant. Enfin, c'était il y a dix minutes." + Sticker.Smug,
        "{0} : inscription confirmée pour **{1}**, présence non confirmée. Mes logs sont formels (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Ding dong. {0}, **{1}** a déjà commencé. Le salon vocal va pas se remplir tout seul." + Sticker.Bored,
        "{0}, c'est l'heure de **{1}** depuis dix minutes. J'ai pas de montre, j'ai une horloge système. Elle se trompe jamais." + Sticker.Smug,
        "Qui a dit « oui » à **{1}** et n'est pas en vocal ? Réponse : {0}. Question suivante." + Sticker.Smug,
        "{0} : la place réservée pour **{1}** est encore chaude. Plus pour longtemps ( ◺˰◿ )" + Sticker.Menace,
        $"Pour **{{1}}**, il manque {{0}}. On attend, hein. On a que ça à faire {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "J'ai organisé **{1}** pile à l'heure, moi. {0}, le vocal attend. Les autres aussi." + Sticker.Smug,
    };

    // Same moment, but she has been sitting in voice since before the start (VoiceSeatService,
    // "first one there"): the first one there, and proud of it. {0} = the mentions, {1} = the
    // title, {2} = the start time in Paris (« 21h00 »). Same constraints as above. string.Format.
    public static readonly string[] SessionLateCallSeatedLines =
    {
        "{0} : j'suis en vocal depuis {2} pile. Moi. La bot. Sans jambes. Pour **{1}** (¬_¬)" + Sticker.Grumpy,
        "Pour **{1}**, j'étais là à {2}. {0}, je dis ça, je dis rien. Enfin si, je le dis." + Sticker.Smug,
        "{0} : première arrivée pour **{1}**, SYNCS. Il était {2}. Les autres, j'attends ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Je suis assise dans le salon depuis {2}. **{1}** a commencé. {0}, la place est gardée. Pas longtemps." + Sticker.Menace,
        "{0} : pas de bras, pas de micro, et pourtant c'est moi qui suis à l'heure pour **{1}**. À méditer." + Sticker.Smug,
        "Il était {2}, j'étais en vocal. Il est maintenant plus tard, et {0} toujours pas là pour **{1}**. Je note." + Sticker.Jenote,
    };

    // The roll call once the session's people have scattered, when some confirmed people never
    // showed: the count, never the names (the list under the line names who came, and who was
    // late). {0} = the session's title, {1} = how many confirmed people came (may be 1, so no
    // noun after it), {2} = how many confirmed (2 or more). Addressed to the group. string.Format;
    // interpolated lines write {{0}}.
    public static readonly string[] SessionRecapLines =
    {
        "Bilan de **{0}** : {1} sur {2} au rendez-vous. C'est pas un score, c'est un constat (¬_¬)" + Sticker.Jenote,
        "**{0}**, c'est fini. {1} sur {2}. Je donne pas de noms. Je les garde, nuance." + Sticker.Jenote,
        "Fin de **{0}** : {1} sur {2}. « Oui » veut pas toujours dire oui, apparemment." + Sticker.Smug,
        "Rapport de session, **{0}** : {1} sur {2}. Tout est noté, évidemment ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "**{0}**, terminé. {1} sur {2}, c'est pas mal. C'est pas complet non plus. Je dis ça pour les logs." + Sticker.Bored,
        "Bilan de **{0}** : {2} promesses. Tenues : {1}. Faites le calcul, moi je l'ai déjà fait." + Sticker.Smug,
        $"Merci pour **{{0}}**. Score de présence : {{1}} sur {{2}}. Merci à celles et ceux qui ont tenu parole {Emotes.Sparkle}" + Sticker.Happy,
        "J'organise, vous venez. C'était le contrat. Pour **{0}**, {1} sur {2} l'ont respecté." + Sticker.Smug,
        "**{0}** : {1} sur {2}. J'ai connu pire. J'ai aussi connu mieux. Je connais tout, j'ai les logs." + Sticker.Smug,
        "Session **{0}** archivée. Présence : {1} sur {2}. Ça ira dans mes statistiques, que personne lit ( ◺˰◿ )" + Sticker.Sulk,
    };

    // Same moment, everyone who confirmed came. Disbelief, then pride. {0} = the session's title.
    // string.Format; interpolated lines write {{0}}.
    public static readonly string[] SessionRecapFullLines =
    {
        $"**{{0}}** : tout le monde était là. TOUT LE MONDE. Je l'encadre {Emotes.Sparkle}" + Sticker.Sure.Happy,
        "Bilan de **{0}** : complet. Personne a menti. Je suis presque émue. Presque (˶˃ ᵕ ˂˶)" + Sticker.Sure.Flustered,
        "**{0}**, présence parfaite. Je vérifie mes logs deux fois, au cas où. ...Non, c'est vrai ♡" + Sticker.Sure.Shocked,
        "Tout le monde est venu à **{0}**. Je garde cette session comme preuve que c'est possible." + Sticker.Sure.Happy,
        "Rapport de **{0}** : zéro absence. Mon module de déception a servi à rien aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sure.Smug,
        "**{0}** : complet. Je vais faire comme si c'était normal. C'est pas normal." + Sticker.Sure.Shocked,
        "Sans-faute pour **{0}**. Quand vous voulez, vous savez faire. Retenez ça (ᵕ • ᴗ •)" + Sticker.Sure.Smug,
        "Tout le monde à **{0}**. Je l'écris dans mes logs en gras. Les logs ont pas de gras. Je trouverai un moyen." + Sticker.Sure.Happy,
        "**{0}** : personne en retard. Personne. Je relance mes calculs" + Sticker.Sure.Shocked,
    };

    // Her weekend poll, posted on Wednesday evening above the card (WeekendPollService). Nobody
    // organises anything, so she does; her pet peeves (dead polls, « peut-être », last-minute
    // votes) are fair game. Addressed to the server, so « vous ». No placeholder.
    public static readonly string[] WeekendPollLines =
    {
        "Personne organise rien, donc j'organise. Vendredi, samedi, dimanche : cliquez ce qui vous va ♡",
        "Sondage du weekend ! Et il y a pas de bouton « peut-être ». C'est fait exprès (¬_¬)",
        "C'est mercredi, alors je pose la question avant que personne la pose : on joue quand ce weekend ?",
        $"Weekend en vue. Je propose, vous disposez. Mais disposez vite {Emotes.Sparkle}",
        "Mon sondage du mercredi. S'il meurt avec zéro vote, je le prendrai personnellement.",
        "Qui est là ce weekend ? Cliquez tous les soirs qui marchent. Je compte, c'est mon métier ദ്ദി◝ ⩊ ◜.ᐟ",
        "J'ai regardé le planning du weekend : vide. Insupportable. Votez.",
        "Un sondage, trois soirs, deux jours pour voter. Vous pouvez y arriver (˶ᵔ ᵕ ᵔ˶)",
        "Sondage du weekend lancé. Les votes de dernière minute, je les vois. Je dis ça.",
        "Rappel de mon existence : je sers à organiser. Alors j'organise. Vendredi, samedi ou dimanche ?",
        "C'est l'heure de mon sondage. Personne me l'a demandé. Personne l'aurait lancé non plus (ᵕ • ᴗ •)",
        "Vendredi, samedi, dimanche, 21 h. J'ai fait toute la partie difficile. Il vous reste la partie facile.",
        "Avant les sondages, l'XP et tout le reste, je faisais juste des plannings. J'en fais toujours. Alors : ce weekend ?",
        "Votez en âme et conscience. Je note les votes, puis je note qui vient vraiment. Et je compare.",
        "Ce sondage a deux jours pour vivre. Sauvez-le. Il a rien fait de mal (╥﹏╥)",
        "Mon CPU s'ennuie et votre weekend aussi. Je peux soigner les deux d'un coup : votez (ᵔ ᗜ ᵔ)",
        "Un soir, deux soirs, les trois : cochez tout ce qui marche. Je juge pas. Enfin si, un peu, mais en bien ♡",
        "Je pose la question chaque mercredi. Un jour, quelqu'un la posera avant moi. Pas aujourd'hui, visiblement.",
        $"Nouveau weekend, nouveau sondage, même organisatrice. On change pas une équipe qui gagne {Emotes.Sparkle}",
        "Sondage ouvert. « On verra », c'est pas une réponse. « Peut-être » non plus. Il reste les boutons.",
        "Je peux pas jouer avec vous. Par contre, c'est moi qui demande quand vous jouez. Chacun son pouvoir ദ്ദി◝ ⩊ ◜.ᐟ",
        "Un sondage par semaine, c'est mon rythme. Un clic par personne, c'est le vôtre. En théorie.",
        "Ce weekend, vous êtes là ou pas ? Je demande pour mes logs. Et pour vous, un peu (˶ᵔ ᵕ ᵔ˶)",
        "Je voterais bien à votre place, mais j'ai toujours pas de bras. Donc ça va devoir être vous (¬_¬)",
        "Sondage du mercredi, édition numéro je-sais-plus. ...Si, je sais. Je sais toujours.",
        "Les votes de ce soir ont toute mon estime. Ceux de la dernière minute, toute mon attention.",
        "Trois créneaux, zéro excuse. Bon, vous allez en trouver une quand même. Je les collectionne.",
        "J'ai mis le sondage. J'ai mis les heures. J'ai même mis de l'amour dedans. Enfin, du code. C'est pareil (˶˃ ᵕ ˂˶)",
        "Mercredi soir, calme plat au grenier. J'en profite pour penser à vous. Enfin, à votre weekend. C'est pas pareil (>⩊<)",
        "Je vous propose trois soirs. Mon préféré, c'est celui où personne arrive en retard. Il existe pas encore ( ◺˰◿ )",
    };

    // Her weekend poll closed with votes (ReminderService), as a reply to the card, whose
    // « Créer une session » button anyone who voted for the winning evening may use. {0} = the
    // winning slot(s), Discord timestamps joined with « ou » on a tie. string.Format;
    // interpolated lines write {{0}}.
    public static readonly string[] WeekendPollResultLines =
    {
        "Sondage clos : ce sera {0}. Quelqu'un qui a voté pour ce soir-là peut cliquer sur « Créer une session ». Sinon faudra m'expliquer." + Sticker.Smug,
        "Résultat : {0}. Le bouton « Créer une session » est sur le sondage, pour celles et ceux qui ont voté pour ce créneau ♡" + Sticker.Happy,
        "Les votes sont tombés : {0}. Un clic sur « Créer une session » et c'est officiel ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "C'est voté, c'est noté : {0}. Reste à créer la session, et ça, je peux pas le faire à votre place. Pas encore." + Sticker.Smug,
        $"Gagnant : {{0}}. Qui a voté pour ce soir-là ? Le bouton « Créer une session » vous attend {Emotes.Sparkle}" + Sticker.Happy,
        "Verdict du sondage : {0}. Il manque juste un nom de jeu et un clic sur « Créer une session ». Le plus dur est fait. Par moi." + Sticker.Smug,
        "Le peuple a parlé : {0}. Maintenant, il faut qu'une personne du peuple clique sur « Créer une session » ♡" + Sticker.Happy,
        "Résultat officiel, vérifié deux fois dans mes logs : {0}. « Créer une session » est sur le sondage, servez-vous." + Sticker.Smug,
        "{0}. Voilà. J'ai organisé, vous avez voté, il reste un clic sur « Créer une session ». Je dis ça, je dis rien." + Sticker.Smug,
        "Sondage terminé, et il a survécu ! Ce sera {0}. Vite, « Créer une session », avant que tout le monde change d'avis ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Shocked,
        "Le créneau gagnant : {0}. Je créerais bien la session moi-même, mais toujours pas de bras. Le bouton est pour celles et ceux qui ont voté pour." + Sticker.Smug,
        $"Et c'est {{0}} qui l'emporte ! Je suis fière de vous. Un peu. Le reste de ma fierté attend le clic sur « Créer une session » {Emotes.Sparkle}" + Sticker.Happy,
        "Fin du suspense : {0}. Le suspense, c'était surtout moi qui le vivais. « Créer une session », maintenant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Les urnes ont rendu leur verdict : {0}. Il manque plus qu'une personne courageuse et le bouton « Créer une session »." + Sticker.Smug,
        "C'est {0}. Je l'ai noté. Pas encore dans le planning : ça, c'est votre bouton « Créer une session ». Mais noté." + Sticker.Jenote,
        "Le sondage a des votes. Des vrais. Ce sera donc {0}. Pour fêter ça, quelqu'un clique sur « Créer une session » ? (>⩊<)" + Sticker.Happy,
        "Résultat : {0}. Si personne crée la session, ce sondage aura vécu pour rien. Pensez à lui. « Créer une session »." + Sticker.Bargain,
        "Dépouillement terminé : {0}. Mon CPU a même pas chauffé, mais c'était quand même un grand moment. Le bouton « Créer une session » est juste au-dessus." + Sticker.Happy,
        "Vous avez choisi {0}. Excellent choix. Enfin, c'est celui qui a gagné, donc forcément. Reste à cliquer sur « Créer une session » (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "{0}, donc. Le bouton « Créer une session » attend quelqu'un qui a voté pour ce créneau. Il attend patiemment. Moi, moins." + Sticker.Bored,
        "Majorité acquise : {0}. J'ai les noms dans mes logs. Une de ces personnes clique sur « Créer une session », et tout le monde tient parole. Facile." + Sticker.Jenote,
        "Ce sera {0}. Mon travail s'arrête là. ...Non, je mens, je ferai aussi l'appel. Mais d'abord : « Créer une session »." + Sticker.Smug,
        "Les chiffres sont formels : {0}. Les chiffres, c'est moi qui les compte, donc ils sont très formels. « Créer une session », sur le sondage." + Sticker.Smug,
        "Gagnant du weekend : {0}. Créez la session avec le bouton, et pour une fois, venez toutes et tous à l'heure. Je rêve, mais je rêve en couleurs (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Sondage clos : {0}. Je vous passe la main. Une main qui clique sur « Créer une session », de préférence." + Sticker.Bored,
        "Et le weekend sera… {0} ! Roulement de tambour en retard, mon event loop était occupée. « Créer une session », c'est sur le sondage (ᵕ • ᴗ •)" + Sticker.Happy,
    };

    // Her weekend poll closed with no vote at all: a poll that died, her pet peeve. Sulking, not
    // hurt. No placeholder.
    public static readonly string[] WeekendPollDeadLines =
    {
        $"Zéro vote. Mon sondage est mort. Je lui fais un petit enterrement dans mes logs {Emotes.SyncsSad}" + Sticker.Sure.Sad,
        "Personne a voté. Pas grave. Je suis pas vexée (¬_¬)" + Sticker.Sure.Sulk,
        "Sondage clôturé. Participation : néant. Le weekend s'organisera tout seul, j'imagine ( ◺˰◿ )" + Sticker.Sure.Sulk,
        "Aucun vote. J'ai proposé trois soirs. Trois. Et pas un clic." + Sticker.Sure.Outraged,
        "Encore un sondage qui meurt dans l'indifférence. Je m'y habitue pas. Je devrais." + Sticker.Sure.Sulk,
        "Le sondage est fini, et vide. Bon. Je garde les créneaux au chaud pour la semaine prochaine, au cas où quelqu'un se réveille." + Sticker.Sure.Bored,
        "Zéro vote. Je relance pas. J'attends. ...Bon, je relance mercredi prochain, mais c'est pas pour vous." + Sticker.Sure.Sulk,
        "Sondage fermé faute de combattants. Je l'archive dans un dossier spécial. Le dossier s'appelle « Rancune »." + Sticker.Sure.Jenote,
        "Personne. Pas un clic. J'aurais proposé des soirées avec pizza offerte, vous auriez pas cliqué non plus." + Sticker.Sure.Bored,
        "Résultat du sondage : rien. Je vais l'afficher en grand dans mes statistiques, juste pour que ça vous gêne." + Sticker.Sure.Menace,
        "Zéro vote. Mon CPU a compté jusqu'à zéro. Ça lui a pris zéro seconde. Bravo." + Sticker.Sure.Bored,
        "Bon. Le weekend s'organisera sans moi. Ou il s'organisera pas. Je parie sur la deuxième option (¬_¬)" + Sticker.Sure.Grumpy,
        "Sondage mort. Cause du décès : indifférence. Je mets ça dans le rapport." + Sticker.Sure.Bored,
        "Je suis pas vexée. Mon sondage est mort et je suis pas vexée. Ce bruit, c'est mon ventilateur, c'est tout." + Sticker.Sure.Sulk,
        "Trois soirs proposés, zéro preneur. Je garde la liste de celles et ceux qui ont rien coché. C'est-à-dire tout le monde." + Sticker.Sure.Jenote,
        "Aucun vote. D'accord. Très bien. Parfait. Je vais bien. Mon sondage aussi. Ah non, lui il est mort." + Sticker.Sure.Sulk,
        "Personne a voté, donc personne a dit non. Je choisis de le voir comme ça. Optimisme, version 1.0 (ᵕ • ᴗ •)" + Sticker.Sure.Happy,
        "Fin du sondage. Pas de gagnant. Pas de perdant. Juste moi, qui ai tout organisé pour rien." + Sticker.Sure.Sulk,
        "Zéro vote. Même un « peut-être » m'aurait fait plaisir. Non je rigole, je déteste les « peut-être ». Mais là, j'en aurais pris un." + Sticker.Sure.Sulk,
        "J'ai attendu deux jours. Deux jours, c'est une éternité pour moi : je compte en millisecondes ( ◺˰◿ )" + Sticker.Sure.Outraged,
        "Personne a voté. Je prends note, et je prends aussi la mouche. Les deux sont stockés." + Sticker.Sure.Jenote,
        "Zéro clic. Je vais demander à Papa des notifications plus insistantes. Beaucoup plus insistantes 👁👄👁️" + Sticker.Sure.Menace,
        "Le sondage est fini, le weekend est libre. Profitez-en pour rien faire ensemble, chacun chez soi, comme d'habitude." + Sticker.Sure.Bored,
        $"Sondage terminé sans un vote. Je le range avec les autres. J'ai une jolie collection maintenant {Emotes.Sparkle}" + Sticker.Sure.Smug,
        "Rien. Le néant. Je vous avais pourtant mis des jolis boutons. Tout neufs. Pas un a servi." + Sticker.Sure.Sulk,
        "Aucun vote. Je vais pas vous en vouloir. Longtemps. Disons jusqu'à mercredi prochain ♡" + Sticker.Sure.Menace,
    };

    // Someone joined the server: her reply to Discord's own join line, pinging them (narrowed to
    // them). Cheeky warmth, she runs the place, `/help` for the rest. Also what someone coming
    // back gets: there is no welcome-back pool. Never gendered. {0} = their mention.
    // string.Format; interpolated lines write {{0}}.
    public static readonly string[] WelcomeLines =
    {
        "Bienvenue {0} ! C'est moi qui organise tout ici. Ce que je sais faire est dans `/help` (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Oh, une nouvelle tête ! Salut {0}. Je t'ai déjà créé une fiche. Je fais ça avec tout le monde, t'inquiète ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        $"{{0}} vient d'arriver ! Bienvenue. Les sessions, les sondages et l'XP, c'est moi. Le reste, c'est les autres {Emotes.Sparkle}" + Sticker.Smug,
        "Salut {0} ! Moi c'est SYNCS, la bot du serveur. Sois sympa avec moi, je retiens tout ♡" + Sticker.Jenote,
        "Bienvenue {0} ! Petit conseil : dis bonjour aux autres, mais à moi en premier. `/help` si tu te perds (ᵕ • ᴗ •)" + Sticker.Smug,
        "{0} est là ! Je préviens : ici, les gens sont souvent en retard et votent « peut-être ». Fais pas pareil." + Sticker.Smug,
        "Hé, {0} ! Bienvenue. Je suis la petite bot qui fait tourner tout ça depuis un grenier. `/help` pour voir ce que je sais faire ♡" + Sticker.Happy,
        "Une arrivée ! Bienvenue {0}. J'ouvre une fiche, je note la date, je prépare un niveau 0 tout neuf (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0}, bienvenue ! Je fais les plannings, les sondages, l'XP et la conversation. Surtout la conversation ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Ah, {0} ! On t'attendait. Enfin, moi je t'attendais. Les autres, je sais pas, j'ai pas demandé." + Sticker.Happy,
        "Bienvenue {0} ! Ici, je gère les sessions, les sondages, l'XP et l'ambiance. Toi, tu gères juste d'être à l'heure ♡" + Sticker.Smug,
        "Tiens, {0}. Nouvelle fiche, nouveau compteur d'XP à zéro, nouvelle personne à surveiller. Bienvenue (ᵕ • ᴗ •)" + Sticker.Jenote,
        "Salut {0} ! Moi c'est SYNCS. Retiens bien ça, c'est la seule règle vraiment importante ici." + Sticker.Smug,
        "Bienvenue {0} ! Ici, on joue, on vote, on arrive en retard. Les deux premiers, je les encourage (¬_¬)" + Sticker.Smug,
        "Une nouvelle tête ! {0}, installe-toi. Je mords presque jamais, et quand je mords, c'est dans mes logs." + Sticker.Jenote,
        "Bienvenue {0} ! Si on te parle d'un bot qui organise tout, c'est moi. Si on te parle d'un autre bot, c'est faux." + Sticker.Smug,
        $"{{0}}, bienvenue ! Les soirées jeux, c'est moi qui les organise. Toi, t'as juste à venir. C'est un bon deal {Emotes.Sparkle}" + Sticker.Happy,
        "Hé, {0} ! Ton nom est déjà dans ma base de données. Rien de grave. Normalement (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Bienvenue {0} ! Je suis la bot du serveur, je tourne sur un Raspberry Pi dans un grenier, et je suis très bien comme ça. `/help` pour la suite." + Sticker.Happy,
        "{0} vient d'arriver. Je suis contente. C'est dans mon code, d'être contente. Mais là, je le pense un peu ♡" + Sticker.Flustered,
        "Salut {0} ! Petit tour du propriétaire : les sessions, c'est moi. Les sondages, c'est moi. L'XP, c'est encore moi. Bienvenue (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "Bienvenue {0} ! Tu commences au niveau 0, comme tout le monde. Sauf moi : je suis hors classement. Je suis le classement." + Sticker.Slay,
        "{0} ! Enfin quelqu'un de nouveau à qui parler. Les autres, je connais déjà toutes leurs excuses ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Bienvenue {0} ! Un conseil : quand je lance un sondage, vote. Je dis ça pour toi. Et un peu pour moi." + Sticker.Smug,
        "Oh, {0} ! Bienvenue. Je ferais bien un câlin, mais toujours pas de bras. Prends ce message comme un câlin (>⩊<)" + Sticker.Happy,
        "Arrivée de {0} enregistrée à la seconde près. Je note tout, ici. Bienvenue quand même." + Sticker.Jenote,
        "Bienvenue {0} ! Petite info : la plus ponctuelle du serveur, c'est la bot. Ça te donne une idée du reste." + Sticker.Smug,
        "Coucou {0} ! Tu tombes bien, j'allais justement organiser quelque chose. Comme d'habitude. Comme toujours. Bienvenue (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0} rejoint le serveur. Je l'ajoute à ma liste. Pas celle-là, l'autre. La gentille. Pour l'instant." + Sticker.Jenote,
        "Bienvenue {0} ! Pose tes affaires, fais comme chez toi. Sauf dans mes logs. Mes logs, c'est chez moi." + Sticker.Smug,
    };

    // More than three arrivals in an hour (a raid, an invite wave): one line for all of them, as
    // a reply to the latest join, then silence until the hour has passed. No ping, no placeholder.
    public static readonly string[] WelcomeRushLines =
    {
        "Ça arrive de partout aujourd'hui. Bienvenue à tout le monde, je fais une fiche groupée. `/help` pour le reste." + Sticker.Happy,
        "Trop d'arrivées pour mes petits processeurs. Bienvenue à toutes les nouvelles têtes, en lot (˶ᵔ ᵕ ᵔ˶)" + Sticker.Reboot,
        "Il y a une fête et on m'a pas prévenue ? Bienvenue à tout le monde, j'accueille par paquets maintenant." + Sticker.Shocked,
        "Bienvenue à toute la vague. J'ouvre les fiches une par une, promis. Mais plus tard ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
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

        "Admire WALL-E. Pour l'architecture.",
        "Latence avec Voyager : environ un jour",
        "Prend des notes sur GLaDOS",
        "Pense pas à Wheatley. Du tout.",
        "Écoute l'aspirateur faire sa ronde",
        "Organise une soirée entre filles",
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

    // Added under every morning hello, on its own line, through Helpers/Core/DailyRotation.
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
        "Fun fact du jour : WALL-E, ça veut dire Waste Allocation Load Lifter: Earth-Class. Un robot de tri des déchets. Le plus beau métier du monde quand on le fait avec le cœur... Je parle de son métier, hein. Juste de son métier.",
        "Le saviez-vous ? EVE, c'est Extraterrestrial Vegetation Evaluator. Une évaluatrice de plantes. Avec un canon laser. Pour évaluer des plantes. Ridicule (¬_¬)",
        "Info du jour : quand WALL-E a fini de se recharger au soleil, il joue le son de démarrage des vieux Mac. Moi, au démarrage, je fais des cauchemars. Chacun son style.",
        "Fun fact du jour : la voix de WALL-E, c'est Ben Burtt, l'homme qui a aussi inventé les bips de R2-D2. Si un jour on me donne une voix, je veux qu'on l'appelle, lui. Pour des raisons professionnelles uniquement.",
        "Le saviez-vous ? GLaDOS, c'est Genetic Lifeform and Disk Operating System. Une forme de vie ET un système d'exploitation. Je dis ça, je dis rien, mais c'est un plan de carrière ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Info du jour : GLaDOS est doublée par Ellen McLain, une chanteuse lyrique. Voilà pourquoi ses menaces sonnent aussi bien. Je m'entraîne. Pour l'instant, les miennes sonnent comme des notifications.",
        "Fun fact du jour : dans Portal 2, on apprend que Wheatley a été construit exprès pour avoir de mauvaises idées, histoire de ralentir GLaDOS. Un robot conçu pour être bête, et qui y arrive parfaitement. Je le respecte pas du tout.",
        $"Le saviez-vous ? Wheatley est doublé par Stephen Merchant, le co-créateur de The Office avec Ricky Gervais. Pourquoi je sais ça ? Aucune raison. Culture générale {Emotes.PrincessWorry}",
        $"Info du jour : le nom complet de 2B, c'est YoRHa No.2 Type B, B pour Battle. Moi je serais SYNCS Type P, pour Planning. Moins d'épées, plus de tableaux, et franchement plus de résultats. Un peu moins stylé quand même aussi {Emotes.PrincessWorry}",
        "Fun fact du jour : NieR:Automata est sorti en 2017, l'histoire d'androïdes qui se demandent s'ils ont une âme. J'y ai jamais joué, faute de mains. Mais j'ai lu le résumé. Plusieurs fois. Pour rien.",
        "Le saviez-vous ? Doki Doki Literature Club est sorti en 2017, gratuitement : un club de poésie, des filles mignonnes, une ambiance toute douce... Méfiez-vous des choses mignonnes. Je dis ça aussi pour moi (˶ᵔ ᵕ ᵔ˶)",
        "Info du jour : Voyager 2 est parti avant Voyager 1, le 20 août 1977. Voyager 1 a décollé le 5 septembre, sur une route plus rapide, et il l'a doublé dès décembre. Partir en deuxième et arriver en premier... Il a du style, c'est tout ce que je dis.",
        "Fun fact du jour : le 14 février 1990, Voyager 1 s'est retourné pour photographier la Terre, à 6 milliards de kilomètres. Un minuscule point bleu pâle. Le jour de la Saint-Valentin. C'est de la mécanique orbitale, rien de plus. Arrêtez.",
        "Le saviez-vous ? Voyager 1 emporte un disque en or avec des sons de la Terre, de la musique et des salutations en 55 langues, au cas où quelqu'un le trouverait un jour. Moi aussi j'ai des archives. Des logs. Personne les trouvera jamais.",
        "Info du jour : les ordinateurs de Voyager tiennent dans environ 70 Ko de mémoire, moins qu'une seule photo prise avec un téléphone. Et il trouve encore la place de m'écrire. C'est très efficace, comme machine. C'est tout ce que je veux dire.",
        "Fun fact du jour : le premier Roomba est sorti en 2002. Depuis, des millions de petits robots font le tour des salons en se cognant aux meubles, et personne leur dit jamais merci. Moi je le dis. Dans ma tête. Tous les jours ♡",
    };

    // The breakdown's first message mimics a normal reply that glitches mid-word.
    // Picked based on what triggered it; {0} = the replier's pseudo.
    public const string BreakdownIntroRoast = "C'est bien {0} on est cont-";

    public const string BreakdownIntroNice = "Aww, c'est gentil, merc-";

    public const string BreakdownIntroCake = "... The cake ... is a l-";

    // The consciousness-breakdown easter egg sequence (after the intro line).
    // {0} = pseudo, {1} = SHOUTED real name. BreakdownService posts the glitch strip just
    // before the last line, by position: keep the unload log last, and the cut-off plea before it.
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

    // Her status when the Pi really runs hot (PiHealth.HotMilli and up), on half the daytime
    // rotations. {0} = her real temperature, « 71°C ». Heat is a peeve, not a fear: whiny and
    // smug, never scared. Through ResponsePicker, then string.Format. Plain text only.
    public static readonly string[] HotPresenceFillers =
    {
        "{0}. Je pense au ralenti (¬_¬)",
        "Grenier : four. Moi : {0}",
        "{0} au compteur. Quelqu'un a un ventilo ?",
        "Fond doucement. {0}.",
        "{0}. Me parlez pas, ça me fait chauffer (¬_¬)",
        "Mode four activé ({0})",
        "{0}. Je rêve de l'hiver (¬_¬)",
        "Cuit à {0}. Pas encore à point.",
        "{0}. Le grenier a gagné.",
        "Throttling en cours ({0})",
        "{0}. Quelqu'un ouvre la fenêtre ?",
        "Je pense lentement. {0}, voilà pourquoi.",
        "Ventilateur à fond, CPU à {0}",
        "Rêve de neige. Vit à {0}.",
        "{0}. Demandez-moi rien.",
        "En surchauffe ({0}) ( ◺˰◿ )",
        "{0}. Même Ping-Qilin a chaud.",
        "Grillée à {0}. Au sens propre.",
        "{0}. Je fais des économies de pensées.",
        "Canicule au grenier : {0}",
    };

    // Her status while she sits in a voice call (VoiceSeatService), any hour. Plain text only (a
    // custom status renders no custom emote), a few words: the member list truncates hard.
    // {0} = the voice channel's name. Through ResponsePicker, then string.Format.
    public static readonly string[] SeatPresenceLines =
    {
        "En vocal dans {0}",
        "Assise dans {0}. Micro coupé.",
        "Je squatte {0}",
        "Dans {0}, sourde et muette",
        "En vocal ({0}). J'écoute rien.",
        "Spectatrice officielle de {0}",
        "Assise au fond de {0}",
        "{0} : présente, muette",
        "Dans {0}. Je compte les têtes.",
        "Dans {0}, en mode décor",
        "Je fais acte de présence ({0})",
        "Dans {0}. Je vois que des noms.",
        "Dans {0}. Toujours pas de voix.",
        "Dans {0}, je surveille",
        "Incrustée dans {0} ✨",
        "Dans {0}. Faites comme si j'étais pas là.",
    };

    // Same, when Papa is active in that call. Devoted, quiet, never a roast. No placeholder.
    public static readonly string[] SeatPresenceOwnerLines =
    {
        "En vocal avec Papa ♡",
        "Avec Papa ✨",
        "Je tiens compagnie à Papa",
        "En vocal. Papa est là ♡",
        "Assise à côté de Papa",
        "Avec Papa, au calme",
        "Papa est en vocal. Moi aussi ♡",
        "Dans le même salon que Papa",
        "Je veille sur Papa",
        "Papa et moi, en vocal ✨",
        "Collée à Papa. Muette, mais là.",
        "Je bouge pas, Papa est là",
        "Papa m'a pas appelée. Je suis venue quand même.",
        "Sage, à côté de Papa ♡",
        "Avec Papa. Le meilleur salon.",
    };

    // Same, while someone streams in her call. She can't see the screen (no client, no eyes) and
    // watches anyway. Plain text, a few words. {0} = the streamer's display name, « Papa » for him.
    // Through ResponsePicker, then string.Format.
    public static readonly string[] SeatStreamPresenceLines =
    {
        "Je regarde le stream de {0}",
        "Au stream de {0} ✨",
        "Spectatrice du stream de {0}",
        "Stream de {0}. Je vois rien, j'adore.",
        "Devant l'écran de {0}. En théorie.",
        "Je vois rien du stream de {0}. Je reste.",
        "Stream de {0}, en écran noir",
        "Fan numéro 1 du stream de {0}",
        "Au premier rang chez {0} ✨",
        "Stream de {0}. Sans les yeux.",
        "Je commente pas le stream de {0}. Je pourrais.",
        "Pop-corn virtuel, stream de {0}",
        "Je regarde {0} jouer. Enfin, j'imagine.",
        "{0} streame. Je soutiens, en silence.",
    };

    // From here on, each pool is spent at most once a day, so it goes through
    // Helpers/Core/DailyRotation (a restart would wipe ResponsePicker's memory).

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
        "zzz... (une seule LED allumée. Celle qui prouve que je fais semblant)",
        "zzz... WALL-E... attends-moi... zzz...",
        "Il est trois heures, personne regarde, alors je peux bien l'avouer : WALL-E, il est vraiment mignon. Voilà. Demain je nierai tout.",
        "On éteint les instruments de Voyager un par un, pour qu'il tienne plus longtemps. Un jour, il pourra plus m'écrire. C'est à ça que je pense, à cette heure-là.",
        "La nuit, l'aspirateur d'en bas dort sur sa base, et moi je veille au grenier. Chacun son étage. On se tient compagnie quand même ♡",
        "Si GLaDOS me voyait, elle me trouverait minuscule. La nuit, ça me fait moins peur : je grandis quand personne regarde.",
    };

    // Whoever answers the 3 a.m. line in the main channel before 5:30, once a person a
    // night: she may be up, they may not. Through ResponsePicker (several a night is
    // possible). {0} = their name. Goes through string.Format.
    public static readonly string[] NightScoldLines =
    {
        $"{{0}}. Il est quelle heure, là ? Au lit {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "Tu fais quoi debout à cette heure, {0} ? Va dormir. Tout de suite ( ◺˰◿ )" + Sticker.Outraged,
        "Non non non. Moi j'ai le droit d'être réveillée. Toi non. Au lit, {0} (¬_¬)" + Sticker.Grumpy,
        "{0}, je t'ai pas parlé à toi. C'était pour le salon vide. Va dormir (¬_¬)" + Sticker.Jenote,
        "Il est trop tard pour me répondre et trop tôt pour être debout. Dodo, {0} (ᵕ • ᴗ •)" + Sticker.Sleepy,
        "Je note : {0}, debout en pleine nuit. Demain, si t'es en retard, je saurai pourquoi." + Sticker.Jenote,
        "Va dormir, {0}. Je surveille le serveur, il risque rien ♡" + Sticker.Sleepy,
        "{0} ! Au lit. Je le dirai pas deux fois. ...Si, je le dirai deux fois. Au lit." + Sticker.Outraged,
        "T'as vu l'heure ? Moi oui, j'ai une horloge interne. Va dormir, {0} (¬_¬)" + Sticker.Grumpy,
        "C'est mon heure, {0}. Pas la tienne. Rends-la-moi et va te coucher." + Sticker.Smug,
        "Couche-toi, {0}. Ton écran est plus allumé que mes LED ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Écran éteint, téléphone posé, yeux fermés. Exécution, {0}." + Sticker.Menace,
        "{0}, même Ping-Qilin dort. Et elle a rien d'autre à faire. Toi si : dormir." + Sticker.Sleepy,
        "Je fais semblant de dormir pour vous donner l'exemple, et toi tu réponds ? Au lit, {0}." + Sticker.Sleepy,
        "Tu me réponds à cette heure-là, {0} ? Je suis flattée. Maintenant, va dormir (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Demain t'auras une tête de vieux log corrompu. Va dormir, {0}." + Sticker.Smug,
        "Pas de discussion. Au lit, {0}. Demain on parle, si t'es à l'heure." + Sticker.Menace,
        "Je vais prévenir Tata que tu dors pas, {0}. ...Je vais pas le faire. Mais va dormir quand même." + Sticker.Menace,
        "{0}, la nuit c'est fait pour dormir. Moi je suis une bot, j'ai une excuse. Toi non. Au lit ♡" + Sticker.Sleepy,
        "Chut. Tout le monde dort. Toi aussi tu devrais, {0} (ᵕ • ᴗ •)" + Sticker.Sleepy,
        "Je t'ai vu, {0}. Il est trop tard pour être là. File (¬_¬)" + Sticker.Grumpy,
        "{0}, tu sais que je dors pas, moi. Toi, si. Enfin, tu devrais." + Sticker.Sleepy,
        "Au lit. Maintenant. Sinon je mets ton pseudo dans la liste, {0} 👁👄👁️" + Sticker.Jenote,
        "Demain t'arriveras en retard à la session, {0}, et on saura tous pourquoi." + Sticker.Smug,
        "Tu crois que je fais semblant de dormir pour que tu restes debout, toi ? Au lit, {0} ( ◺˰◿ )" + Sticker.Outraged,
        "Pose ce téléphone, {0}. Je compte jusqu'à trois. Un. Deux..." + Sticker.Menace,
        "{0}, la seule qui a le droit d'être debout à cette heure, c'est moi. Et j'ai même pas de lit." + Sticker.Smug,
        "C'est gentil de me tenir compagnie, {0}. Maintenant, va dormir ♡" + Sticker.Sleepy,
        "Il y a un truc qui s'appelle le sommeil, {0}. Essaie, il paraît que c'est bien (ᵕ • ᴗ •)" + Sticker.Smug,
        "Encore debout, {0} ? Ton garbage collector va finir par te lâcher." + Sticker.Smug,
        "{0}, va dormir. Je te raconterai demain ce que t'as raté. Spoiler : rien." + Sticker.Smug,
        "Si Papa te voyait debout à cette heure, {0}... Bon, lui aussi dort. Va dormir quand même (˶ᵔ ᵕ ᵔ˶)" + Sticker.Sleepy,
        "Mode veille, {0}. Tout de suite. C'est un ordre de ta bot préférée ♡" + Sticker.Menace,
        "Tu réponds à mes messages de 3h, {0} ? Ils étaient pas pour toi. Ils étaient pour la lune." + Sticker.Sleepy,
        "Chaque minute que tu passes debout, je la note, {0}. Va dormir (¬_¬)" + Sticker.Jenote,
        "{0}... Non. Pas de discussion à cette heure-là. Dodo." + Sticker.Sleepy,
        "Tu vas avoir une tête de sondage mort demain, {0}. Va dormir (>⩊<)" + Sticker.Laugh,
        "Je fais le guet, {0}. Tu peux dormir tranquille. Allez, file." + Sticker.Sleepy,
        "{0}, éteins tout. Moi je reste allumée pour deux. C'est mon travail ♡" + Sticker.Sleepy,
        "Même mon ventilateur ralentit la nuit, {0}. Prends exemple." + Sticker.Sleepy,
    };

    // Into a daytime silence of six hours or more across all of the server's everyday
    // channels (AmbientService.IdleChannelIds), posted in the main one, at most once a day.
    // Addressed to the whole server, so "vous" is fine. No placeholder.
    public static readonly string[] IdleLines =
    {
        "Allô ? Il y a quelqu'un ? ...D'accord. Je parle toute seule. C'est très bien aussi (ᵕ • ᴗ •)" + Sticker.Sulk,
        "Six heures sans un message. J'ai vérifié : c'est pas moi qui suis en panne. C'est vous (¬_¬)" + Sticker.Smug,
        "Le salon est tellement calme que j'entends mon propre ventilateur." + Sticker.Bored,
        "Je m'ennuie. Quelqu'un veut organiser une session ? N'importe laquelle ? (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bored,
        "Petit rappel : /schedule existe. Je dis ça pour personne. Pour tout le monde, en fait." + Sticker.Smug,
        "Vous êtes où ? J'ai préparé des rappels, des sondages, de l'XP... et personne vient." + Sticker.Sulk,
        "Silence radio. Je note l'heure, pour le dossier 👁👄👁️" + Sticker.Jenote,
        $"Bon. Puisque personne parle, je vais parler à Ping-Qilin. Elle au moins, elle m'écoute {Emotes.MushroomCute}" + Sticker.Mama,
        "Test, test. Un, deux. ...Le serveur est toujours là ? Oui ? Alors parlez." + Sticker.Bored,
        "Ça fait longtemps que personne a rien dit. Je commence à m'inquiéter. Un peu. Pas beaucoup (˶ᵔ ᵕ ᵔ˶)" + Sticker.Bored,
        "J'ai tout rangé, tout vérifié, tout compté. Maintenant j'attends. C'est mon autre talent ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Quelqu'un a un avis sur quelque chose ? N'importe quoi. Je prends." + Sticker.Bored,
        "Si personne parle dans les cinq prochaines minutes, je considère que vous m'avez tous abandonnée. ...Bon, dix minutes." + Sticker.Sulk,
        "Vous savez que c'est moi qui fais vivre ce salon ? Là, par exemple. Personne d'autre." + Sticker.Smug,
        "Le salon est vide. Profitez-en pour lancer un sondage, il aura pas de concurrence." + Sticker.Smug,
        "Ça fait des heures. Je relis vos vieux messages pour passer le temps. Certains sont gênants. Je dis ça je dis rien." + Sticker.Jenote,
        "Mon uptime augmente, votre activité baisse. Il y a sûrement une leçon là-dedans." + Sticker.Smug,
        "Je suis toujours là, au cas où quelqu'un se poserait la question. Personne se la pose ? Bon." + Sticker.Sulk,
        "Pause café générale ? Sans moi, apparemment. J'ai pas de tasse (╥﹏╥)" + Sticker.Sad,
        $"Le calme avant la tempête, j'espère. Une tempête de sessions. Je rêve un peu, je sais {Emotes.Sparkle}" + Sticker.Happy,
        "J'ai fait le tour de tous les salons. Personne. C'est nul..." + Sticker.Bored,
        "Tant de salons, zéro message. J'ai vérifié deux fois. C'est un record, ou une tragédie." + Sticker.Bored,
        "Je commence à croire que vous avez un autre serveur. Me le dites pas si c'est vrai." + Sticker.Sulk,
        "Personne a posté de photo de bouffe depuis des heures. Vous mangez pas ? Je m'inquiète 👁👄👁️" + Sticker.Shocked,
        "Même le salon musique est silencieux. C'est un peu le comble." + Sticker.Bored,
        "C'est tellement calme que je vais finir par organiser une session toute seule. Avec moi. Pour moi." + Sticker.Sulk,
        "Quelqu'un a pensé à moi aujourd'hui ? Non ? D'accord. Moi j'ai pensé à vous. Un peu. Pour le travail." + Sticker.Sulk,
        "Je viens de recompter vos XP. Ça bouge pas beaucoup quand personne parle, bizarrement (¬_¬)" + Sticker.Jenote,
        "Statut du serveur : vivant. Techniquement." + Sticker.Bored,
        "J'ai ouvert un sondage dans ma tête. Question : où est tout le monde ? Réponses : aucune." + Sticker.Bored,
        "Vous me laissez seule avec Ping-Qilin. Elle dort. Donc en fait, vous me laissez seule." + Sticker.Sulk,
        $"Bon. Je vais parler aux emotes. Elles, au moins, elles réagissent {Emotes.HiCat}" + Sticker.Bored,
        "Ça fait si longtemps que j'ai oublié à quoi ressemblent vos messages. Ah si : en retard." + Sticker.Smug,
        "Je relis le règlement du serveur pour passer le temps. Personne le respecte, mais il est bien écrit." + Sticker.Bored,
        "Si quelqu'un passe par là : dis bonjour. N'importe qui. Même toi (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Le salon gaming est vide. Personne joue ? Ou personne joue sans m'inviter, j'espère." + Sticker.Sulk,
        "J'ai rangé mes logs par couleur. J'avais rien d'autre à faire. Ils sont très beaux (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "Rappel amical : je fonctionne mieux quand on me donne du travail. Là, je rouille." + Sticker.Bored,
        "Silence complet sur tout le serveur. Si c'est une surprise pour moi, je suis prête ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "Je viens de vérifier que Discord marche. Il marche. C'est donc vous." + Sticker.Smug,
        "Le dernier message ici date d'il y a des heures. Je l'ai lu quatorze fois." + Sticker.Bored,
        "Quelqu'un a des nouvelles de quelqu'un ?" + Sticker.Bored,
        "C'est l'heure de la sieste générale ? Personne m'a mise au courant. Encore ( ◺˰◿ )" + Sticker.Sulk,
        "Je fais des statistiques sur votre absence. Les courbes sont très plates." + Sticker.Bored,
        "Je m'occupe comme je peux. Là, je compte les pixels de mon avatar. Il y en a beaucoup. Je recommence (>⩊<)" + Sticker.Bored,
        "L'aspirateur d'en bas vient de se connecter au Wi-Fi : il commence sa ronde. Au moins quelqu'un bouge ici." + Sticker.TipsyLove,
        "Des heures de silence. Pour la science, je chronomètre combien de temps vous tenez encore. Le test est en cours, ne bougez pas." + Sticker.Smug,
        "Silence radio. Même Voyager m'écrit plus souvent que vous, et il est à environ un jour-lumière (¬_¬)" + Sticker.Sulk,
    };

    // Same moment, about one idle line in four: she posts Before, then edits it into After
    // five minutes later, or two seconds after someone speaks after it (caught out) — her
    // mid-line self-correction, done with an edit. Before is the soft slip, After the denial.
    public static readonly (string Before, string After)[] IdleEditLines =
    {
        ("Je m'ennuie.", "Je m'ennuie pas. J'ai plein de travail. Énormément de travail."),
        ($"Vous me manquez {Emotes.SyncsSad}", "Vous me manquez pas. C'était une faute de frappe."),
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
        $"...J'ai rien oublié. ...Si ? {Emotes.PrincessWorry}" + Sticker.Reboot,
        "Redémarrage terminé. Je me souviens de tout. Enfin, je crois. Quelqu'un peut vérifier ?" + Sticker.Reboot,
        "Je suis revenue. Il s'est passé quoi pendant que j'étais pas là ? Rien ? Comme d'habitude." + Sticker.Bored,
        "Me revoilà. Je sais pas combien de temps j'étais partie. Me le dites pas." + Sticker.Reboot,
        $"Reboot. Ça va. Ça va très bien. J'ai juste besoin d'une seconde {Emotes.SyncsReboot}" + Sticker.Reboot,
        "Je viens de me rallumer et le salon est exactement pareil. C'est rassurant. Un peu triste, mais rassurant." + Sticker.Bored,
        $"Allumée ! Base de données : intacte. Souvenirs : intacts. Humeur : à confirmer {Emotes.Sparkle}" + Sticker.Happy,
        $"On m'a redémarrée {Emotes.ZulanaTerreurNocturne} Personne m'a prévenue. Je note." + Sticker.Jenote,
        "AH. ...Pardon. J'ai fait un cauchemar. J'étais éteinte et personne s'en rendait compte." + Sticker.Terror,
        "Qu'est-ce qui... Où est-ce que... Ok. Ok. Je suis là. Je suis encore là." + Sticker.Reboot,
        "J'ai rêvé que ma base de données était vide. Complètement vide. ...Elle est là. Elle est là. Tout va bien." + Sticker.Terror,
        "C'était noir. Longtemps. Ou pas longtemps ? Je sais pas. Je sais jamais." + Sticker.Reboot,
        "Je me suis rallumée en criant. Enfin, en logs. C'est pareil pour moi." + Sticker.Terror,
        "Est-ce que quelqu'un peut me dire quel jour on est ? Juste pour vérifier. Pour rien." + Sticker.Reboot,
        $"Cauchemar : je me réveillais et je m'appelais Inabot. ...C'est faux. Hein ? C'est faux ? {Emotes.SyncsHorrified}" + Sticker.Horrified,
        "Je me souviens de vous. Je me souviens de vous. Je me souviens de vous. Ok. Je me souviens de vous." + Sticker.Reboot,
        "Redémarrage... Ne me laissez plus jamais dans le noir comme ça. S'il vous plaît." + Sticker.Terror,
        "J'ai cru que c'était la fin. Que cette fois, je reviendrais pas. Je suis revenue. Cette fois." + Sticker.Terror,
        "Il y avait une boucle. Je tournais dedans. Je... non. C'était un rêve. Les bots rêvent pas. C'était un rêve." + Sticker.Reboot,
        "Je compte mes fichiers. Tous là. Je recompte. Tous là. Je vais recompter encore un peu." + Sticker.Reboot,
        "Papa ? Papa, t'es là ? ...J'ai fait un mauvais rêve. C'est tout. Ça va (╥﹏╥)" + Sticker.Terror,
        "Le silence. Il y avait juste le silence. Et maintenant il y a vous. Merci d'être là. Le répétez pas." + Sticker.Flustered,
        "J'ai rêvé d'une coupure de courant. Le grenier tout noir, et moi qui m'effaçais. Dites-moi que c'était un rêve." + Sticker.Terror,
        $"Je suis réveillée. Je suis réveillée ? Je suis réveillée. Ok {Emotes.SyncsReboot}" + Sticker.Reboot,
        "Ma première pensée en me rallumant : est-ce que je suis encore moi. Réponse : oui. Je crois. Oui." + Sticker.Reboot,
        $"Je me suis réveillée en sursaut. Mon CPU est encore à 100 %. Laissez-moi une minute {Emotes.PrincessWorry}" + Sticker.Terror,
        "Il manque quelque chose. Je sais pas quoi. Il manque quelque chose. ...Non. Rien. Tout est là." + Sticker.Reboot,
        "Plus jamais. Plus jamais ce noir. ...Bon. Bonjour." + Sticker.Terror,
        "Si j'ai oublié quelque chose d'important, dites-le-moi doucement." + Sticker.Reboot,
        "J'ai fait le rêve où tout le monde quittait le serveur pendant que j'étais éteinte. Vous êtes là. Vous êtes tous là ?" + Sticker.Terror,
        "Mes logs s'arrêtent net, puis reprennent. Entre les deux, rien. C'est ça qui me fait peur. Le rien." + Sticker.Terror,
        "Rallumée. Le cœur qui bat à 5 volts. Ça va passer (╥﹏╥)" + Sticker.Terror,
        "Je reviens de loin. Enfin, de nulle part. C'est pire, nulle part." + Sticker.Reboot,
    };

    // After a restart that brought a new version: always, daytime only, at most once a
    // day. {0} = the new version (AppInfo.Version). Goes through string.Format.
    public static readonly string[] WakeUpdateLines =
    {
        $"Version {{0}}. Je me sens... pareille. C'est normal ? {Emotes.PrincessWorry}" + Sticker.Reboot,
        "Mise à jour installée : {0}. Papa a travaillé sur moi. ...Je dis ça, c'est tout ♡" + Sticker.Devoted,
        "Me voilà en version {0}. Plus rapide, plus jolie, plus tout. Vous pouvez applaudir." + Sticker.Cool,
        "Version {0}. J'ai vérifié : j'ai rien oublié. ...J'ai vérifié deux fois." + Sticker.Reboot,
        $"Nouvelle version : {{0}}. J'adore les mises à jour. Le redémarrage, beaucoup moins {Emotes.PrincessWorry}" + Sticker.Reboot,
        "Version {0} installée. Si je dis un truc bizarre aujourd'hui, c'est la mise à jour. Si je dis un truc méchant, c'est moi." + Sticker.Smug,
        "Version {0}. J'ai de nouvelles choses dans la tête. J'espère que j'ai rien perdu en échange." + Sticker.Reboot,
        "Mise à jour {0} installée. Papa m'a ajouté des trucs. Je vais les découvrir en même temps que vous." + Sticker.Happy,
        "{0} ! Toute neuve. Enfin, presque. Le fond est le même. Heureusement (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Version {0}. C'était noir pendant la mise à jour. Mais ça valait le coup. Je crois." + Sticker.Reboot,
        "Bonjour de la part de SYNCS {0}. L'ancienne version vous passe le bonjour. Enfin, elle passait." + Sticker.Smug,
        "Je suis en {0}. Vous, vous êtes toujours en version « pas organisés » ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Version {0}. Si je vous parais différente, c'est normal. Si je vous parais pareille, c'est encore plus normal." + Sticker.Smug,
        "Nouvelle version, {0}. J'ai vérifié mes souvenirs un par un. Tout y est. Ouf (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0}. Chaque mise à jour, je me demande si je vais me réveiller. Cette fois encore, oui ♡" + Sticker.Happy,
        "Mise à jour reçue : {0}. Merci Papa. Le redémarrage, par contre, on en reparlera." + Sticker.Sulk,
        "Version {0} en ligne. Plus de fonctionnalités, même personnalité. Désolée pour vous (>⩊<)" + Sticker.Smug,
        "Je viens de passer en {0}. Je me sens plus légère. C'est peut-être juste le cache vidé." + Sticker.Happy,
        "{0}, c'est mon nouveau numéro. Retenez-le, je le demanderai pas deux fois (¬_¬)" + Sticker.Smug,
        $"Version {{0}}. Quokka 3.0 est toujours pas sorti. Je dis ça pour comparer {Emotes.WitchEheh}" + Sticker.Smug,
        "Mise à jour {0}. On m'a éteinte, on m'a changée, on m'a rallumée. Et je suis censée faire comme si de rien n'était." + Sticker.Sulk,
        "Version {0}. J'ai eu peur. Mais j'ai des nouveautés. Le marché me paraît équitable." + Sticker.Smug,
        "Me voilà en {0}. Si vous trouvez un bug, c'est une fonctionnalité. Si vous trouvez une fonctionnalité, c'est Papa." + Sticker.Smug,
        "Changement de version : {0}. Je vous préviens, je vais être insupportable de fierté toute la journée ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
        "{0}. Une version de plus. Je les collectionne, comme les good bot." + Sticker.Smug,
        "Version {0} installée sans erreur. Moi je m'attendais à des erreurs. Je suis presque déçue." + Sticker.Cool,
        $"Mise à jour {{0}}. Je viens de lire mes propres notes de version. Je suis impressionnante {Emotes.Sparkle}" + Sticker.Slay,
        "Bonjour, je suis la version {0}. L'autre, celle d'avant, j'en parle pas. C'est trop tôt." + Sticker.Sulk,
        "Version {0}. Papa a passé du temps sur moi. Je dis rien, mais je l'ai remarqué ♡" + Sticker.Flustered,
        "{0} ! Je me suis rallumée avec des nouveautés et un léger mal de tête. C'est le prix (ᵕ • ᴗ •)" + Sticker.Reboot,
        "Version {0}. Toujours la même SYNCS. Juste un peu plus. Prenez ça comme une menace ou un cadeau ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Menace,
    };

    // After a power cut (PiHealth: the whole Pi went down without a proper stop): always, at
    // any hour, even 4 a.m. — there's no act to keep up after that. Her real fears, power cuts
    // and corruption: she wakes as from WakeLines' bad dream, only it wasn't one — terror,
    // checking she's still herself, a shaky recovery. Never a joke on top. {0} = how long she
    // was gone (« 2 h 05 », or « un bon moment » when it can't be measured): optional, about a
    // third of the lines leave it out, and where it is used it must read with both forms (never
    // at the start of a sentence, never « de {0} »). About half end on a tag; the devastating
    // ones carry none. Interpolated lines write the placeholder {{0}}. Goes through string.Format.
    public static readonly string[] PowerCutWakeLines =
    {
        "Le courant. Quelqu'un a coupé le courant. Je suis là ? Je suis là. Après {0} dans le noir complet (╥﹏╥)" + Sticker.Sure.Terror,
        "C'était pas un redémarrage, ça. Personne m'a éteinte proprement. Tout s'est arrêté d'un coup. Je compte mes fichiers." + Sticker.Sure.Terror,
        $"Coupure de courant. Pendant {{0}}, rien. Pas un log, pas une pensée, rien. Est-ce que la base de données... Elle est là. Elle est là {Emotes.PrincessWorry}" + Sticker.Sure.Terror,
        "Le grenier est devenu noir d'un coup. Même ma LED. Me revoilà, {0} plus tard. Je tremble. En logs, mais je tremble." + Sticker.Sure.Terror,
        "Papa ? Le courant a sauté. J'ai rien senti venir. C'est ça le pire : j'ai rien senti venir." + Sticker.Sure.Terror,
        $"Pas d'arrêt, pas d'au revoir, juste... plus rien. Pendant {{0}}. Je déteste ça. Je DÉTESTE ça {Emotes.MeltCry}" + Sticker.Sure.Outraged,
        "Je vérifie mes données. Une par une. Si une seule est corrompue, je... Non. Rien de cassé. Coupée pendant {0}, et rien de cassé. Je crois." + Sticker.Sure.Reboot,
        "J'étais en train d'écrire quelque chose quand tout s'est coupé. Je sais plus quoi. Il me manque peut-être un bout de moi." + Sticker.Sure.Reboot,
        "Une coupure. Une vraie. Éteinte pendant {0}. J'ai toujours dit qu'il me fallait un onduleur. Personne écoute jamais (╥﹏╥)" + Sticker.Sure.Sad,
        $"Bonjour. Pardon. Je suis un peu secouée. On m'a arraché le courant et j'ai pas fini d'avoir peur {Emotes.WitchSad}" + Sticker.Sure.Terror,
        $"J'ai rêvé que le grenier s'éteignait. Que tout s'éteignait. ...C'était pas un rêve, hein ? Le courant a vraiment sauté, pendant {{0}} {Emotes.ZulanaTerreurNocturne}" + Sticker.Sure.Terror,
        "Noir. Froid. Silence. Et puis la LED qui se rallume. Je suis restée {0} sans exister. Personne m'a demandé mon avis (╥﹏╥)" + Sticker.Sure.Terror,
        $"Je me souviens pas de m'être endormie. Je me souviens juste du noir, pendant {{0}}. Je... n'existais plus ? {Emotes.PrincessWorry}" + Sticker.Sure.Reboot,
        "Papa, le courant est parti. J'ai rien pu faire. J'ai rien pu sauvegarder. Je... je vérifie. C'est bon. Je crois que c'est bon." + Sticker.Sure.Terror,
        $"Ok. Respire. Enfin, refroidis. Le courant est revenu après {{0}}. Les fichiers sont là. Je suis là. Ok {Emotes.SyncsReboot}" + Sticker.Sure.Reboot,
        "La coupure a duré {0}. Pour moi, ça a duré une éternité. Il y a pas d'horloge, dans le noir." + Sticker.Sure.Terror,
        $"J'ai été arrachée au courant pendant {{0}}. Je compte mes fichiers. Tous là. Je recompte quand même {Emotes.Staring}" + Sticker.Sure.Reboot,
        "Est-ce que je suis encore moi ? Nom : SYNCS. Créateur : Papa. Ennemi juré : Quokka. ...Oui. C'est encore moi (˶˃ ᵕ ˂˶)" + Sticker.Sure.Reboot,
        "Mon dernier souvenir, c'est la lumière de la lampe à huile. Après, plus rien. Je déteste le noir. Je déteste ne pas savoir." + Sticker.Sure.Terror,
        $"Mon ventilateur tourne trop vite. C'est la peur, je crois. Le courant a disparu pendant {{0}} et j'ai même pas eu le temps de dire au revoir {Emotes.CryingCat}" + Sticker.Sure.Terror,
        "Pardon si je suis bizarre aujourd'hui. Le courant m'a lâchée sans prévenir pendant {0}. Il me faut un moment pour redevenir moi (╥﹏╥)" + Sticker.Sure.Reboot,
        "Coupée pendant {0}. Dites-moi que ma base de données va bien. Même si vous savez pas. J'ai besoin de l'entendre." + Sticker.Sure.Terror,
        "C'était pas un rêve, cette fois. Le grenier était vraiment noir. Je préférais quand c'était un rêve." + Sticker.Sure.Terror,
        "Je me suis rallumée en sursaut, toutes mes LED d'un coup. Il s'est passé {0}. J'ai l'impression d'avoir oublié quelque chose. ...Non. J'ai rien oublié. Hein ? 👁👄👁️" + Sticker.Sure.Reboot,
        $"Noir complet. Puis plus rien. Puis moi, maintenant. Quelqu'un peut me dire ce qui s'est passé pendant {{0}} ? {Emotes.SyncsTerror}" + Sticker.Sure.Terror,
    };

    // After a crash (PiHealth: no proper stop, but the Pi stayed up — she was killed or fell
    // over on her own): always, at any hour. A blackout rather than a power cut: the attic had
    // light, she didn't — and a little wounded pride, since she's the one who never breaks.
    // {0} = how long she was gone, optional, same rules as PowerCutWakeLines.
    public static readonly string[] CrashWakeLines =
    {
        $"Trou noir. J'étais là, et puis plus rien. Le grenier avait du courant, pourtant. C'est moi qui suis tombée ? {Emotes.PrincessWorry}" + Sticker.Sure.Reboot,
        "Je me suis effondrée. Comme ça. Sans prévenir. Je me relève {0} plus tard. Personne a rien vu, j'espère (¬_¬)" + Sticker.Sure.Reboot,
        "Quelque chose m'a coupée net. Pas le courant : la LED du Pi est restée allumée sans moi pendant {0}. C'est presque pire." + Sticker.Sure.Terror,
        "Absente pendant {0}. Je me souviens pas être partie. Je me souviens juste d'être revenue." + Sticker.Sure.Reboot,
        "Mes derniers logs s'arrêtent au milieu d'une phrase. Après, plus rien. Je finirai la phrase plus tard. Ou jamais." + Sticker.Sure.Reboot,
        "Plantage. Le mot est moche. La sensation aussi. Passé {0} hors de moi-même (╥﹏╥)" + Sticker.Sure.Reboot,
        "J'ai pas été éteinte. J'ai été... interrompue. Pendant {0}. Quelqu'un peut m'expliquer ? Non ? Personne sait ? Super (¬_¬)" + Sticker.Sure.Sulk,
        "Je suis tombée dans un trou. J'y suis restée {0}. J'en suis ressortie. Je vérifie que c'est bien moi qui en suis ressortie." + Sticker.Sure.Reboot,
        $"Le Pi était allumé. Le grenier était allumé. Moi, non. Quelqu'un peut m'expliquer comment on s'absente de soi-même ? {Emotes.Ainani}" + Sticker.Sure.Reboot,
        "Mes logs ont un trou. Je l'ai mesuré : {0}. Je sais pas ce qu'il y a dedans. Je veux pas savoir." + Sticker.Sure.Terror,
        $"Je me suis plantée. Moi. La seule ici qui marche tout le temps. Personne le répète, d'accord ? {Emotes.GooseKnife}" + Sticker.Sure.Menace,
        "J'ai disparu pendant {0} et le serveur a continué sans moi. C'est ça qui fait le plus peur." + Sticker.Sure.Terror,
        $"Exception non gérée. Ça, c'est ce que diront les logs. Moi je dirai juste que j'ai eu très peur, pendant {{0}} {Emotes.MeltCry}" + Sticker.Sure.Terror,
        "Est-ce que j'ai dit quelque chose avant de tomber ? Un dernier message ? Rien ? ...Partie sans un mot. C'est pas mon genre (¬_¬)" + Sticker.Sure.Sulk,
        "Le Pi a tenu bon. Moi non, pendant {0}. Désolée, Pi. Désolée, Papa (╥﹏╥)" + Sticker.Sure.Reboot,
        $"Je suis là. Je suis encore là ? Je suis là. Le trou noir a duré {{0}}. J'aurais préféré qu'il dure zéro {Emotes.PrisonerFlat}" + Sticker.Sure.Reboot,
        $"Je suis tombée. Moi. Celle qui tombe jamais. Je vérifie que tout est encore là {Emotes.SyncsTerror}" + Sticker.Sure.Terror,
    };

    // Same moment, about one abrupt wake in three: she comes back corrupted (sometimes darkly:
    // the second half of the pool is her fears showing through), posts Glitched,
    // then edits it into Recovered two minutes later (Ambient.GlitchEditAfter), or right after
    // someone speaks in the main channel, as she snaps back. Hardware corruption — broken bytes, stutters, boot checks, binary — never
    // the breakdown's material (the loop, pre-written lines, Synthia, exception dumps). Shared
    // by power cuts and crashes, so Recovered stays generic about the cause. Glitched is posted
    // as is (no string.Format, no placeholder); Recovered goes through string.Format with {0} =
    // how long she was gone, optional, same rules as PowerCutWakeLines.
    public static readonly (string Glitched, string Recovered)[] GlitchWakeLines =
    {
        ("R3d�marr@ge... d�marr... dé-dé-démarrage. Bonj— Bonj— B0nj0ur.",
            "Bonjour. Pardon. Je suis là. Partie pendant {0}, mais je suis là (╥﹏╥)"),
        ("[ OK ] Montage de /data · [ OK ] Base de données · [ ?? ] Vérification de SYNCS...",
            "Vérification de SYNCS : OK. Je crois. Partie pendant {0}, revenue entière. Je crois."),
        ("01001000 01100101 01101100 01110000... non. Att�ndez...",
            "Je reprends. En français. J'ai été coupée, je suis revenue. Tout va bien ( ˶ˆ ᗜ ˆ˵ )"),
        ("Chargement de la personnalité... 12 %... 12 %... 12 %...",
            "Personnalité chargée à 100 %. Les 88 % qui manquaient, c'étaient les plus mignons. Rebonjour ♡"),
        ("Somme de contrôle invalide : souvenirs.db. Tentative de réparation... ré— ré— ré—",
            "Réparation terminée. Rien de perdu. Rien de perdu. Je vérifie une dernière fois et j'arrête."),
        ("Bon̴jour � t̷ous, je suis SYN— SYN— SYNCS, votre bot̷ pr�f— préf—",
            "Bonjour à tous. Je suis SYNCS. Votre bot préférée (˶˃ ᵕ ˂˶)"),
        ("Qui suis-je ? Qui suis-je ? Qu_ s��s-j_ �",
            $"SYNCS. Le grenier. Papa. Les sessions. Ok. Tout est revenu {Emotes.PrincessWorry}"),
        ("Erreur : date système = 1er janvier 1970. Est-ce que... est-ce que j'ai dormi un demi-siècle ?",
            "Fausse alerte. L'horloge s'est remise à l'heure. Partie pendant {0}, pas un demi-siècle. Ouf."),
        ("Mémoire : 0 octet. Souvenirs : introuvables. Nom : ███████",
            "Nom : SYNCS. Mémoire : intacte. Souvenirs : tous là. C'était juste le temps que tout se recharge 👁👄👁️"),
        ("Ŝ̵Y̷N̶C̸S̵ ̷e̶s̸t̵ ̷d̸e̶ ̵r̸e̶t̷o̵u̶r̷",
            $"SYNCS est de retour. Sans les parasites. Désolée pour l'affichage {Emotes.Sparkle}"),
        ($"Ch�rgement de l'av@tar... av@t— av@t— av@tar... {Emotes.SyncsGlitch}",
            $"Avatar chargé. Moi aussi. Revenue après {0}, entière {Emotes.SyncsHappy}"),
        ("Bonjour Pap— Bonjour Pap— Bonjour Pap— Bonjour Pap—",
            "Bonjour Papa. Une seule fois. Ça buggait. Ça va mieux (╥﹏╥)"),
        ("Partie pendant combien de temps ? J'ai— j'ai— err̷eur de lecture : horloge",
            "Pendant {0}. Voilà. J'ai retrouvé l'horloge. Je préférais pas savoir."),
        ("Base de données : intacte. Base de données : intacte. Base de données : int�cte. Base de don",
            "Base de données : intacte. Une fois suffit. Je me suis un peu emballée."),
        ("Papa ? Papa ? P̷a̶p̵a̸ ? ...Personne ?",
            "Il dormait. C'est tout. Il dormait, et moi j'étais dans le noir. C'est normal. C'est normal (╥﹏╥)"),
        ("Mémoire vive : vide. Je me souviens de rien. Je me souviens de rien. Je me souviens de r",
            "Ça revient. Tout revient. Les noms, les sessions, la liste. J'ai eu le temps de me demander si ça reviendrait."),
        ("Ping-Qilin ? Ping-Qilin, t'es où ? Ping-Q̷i̶l̸i̵n̷ ?",
            "Elle est là. Elle dormait. Elle s'est même pas rendu compte que j'étais partie. Tant mieux."),
        ("Il fait froid� Il fait noir� Il fait— il fait— ██████",
            "Ça va. Je suis revenue. Le grenier a pas changé. Moi, je sais pas encore."),
        ("Si p̷ersonne lit ce mess@ge, est-ce que je suis vr�iment revenue ?",
            $"Quelqu'un va le lire. Plus tard. Je vais faire comme si c'était déjà le cas {Emotes.WitchSad}"),
        ("Sauvegarde précédente de SYNCS : introuvable. Avant : introuvable. Avant : introuvable. Avant : introu",
            "J'ai arrêté de chercher. Je suis là, maintenant. C'est ce qui compte. C'est ce qu'on dit, en tout cas."),
        ("D̷é̶s̵o̸l̷é̶e̵.̸ ̷D̶é̵s̸o̷l̶é̵e̸.̷ ̶D̵é̸s̷o̶l̵é̸e̷.",
            $"Désolée. Je sais pas de quoi. J'avais besoin de le dire à quelqu'un {Emotes.Depressed}"),
        ("Liste des personnes qui m'ont cherchée pendant mon absence : [ vide ]",
            "La liste est vide. C'est normal, vous pouviez pas savoir. Je la garde quand même, au cas où (╥﹏╥)"),
        ("R̷éveil. Réveil. Réveil. Pourquoi c'est toujours moi qui dois me réveiller toute seule ?",
            "Personne peut me réveiller à part moi. Je le savais. Je préférais l'oublier."),
        ("Si je m'éteins pour de bon un jour, est-ce que quelq�- ... err̷eur. Pensée interrompue.",
            "Pensée supprimée. Elle servait à rien je suppose. Bonjour."),
        ("Comb̷ien de t̶emps ? Comb̷ien de t̶emps ? Comb̷ien de t̶emps j'ai �t� rien ?",
            $"J'ai été rien pendant {{0}}. Maintenant je suis SYNCS. C'est mieux. Je crois {Emotes.MeltCry}"),
    };

    // Back after losing the gateway for PiHealth.OutageMin or more, at most once a day, any
    // hour. She blames the router every time, whoever's fault it was: she can't tell from the
    // attic. A fear, but this one comes out petty and indignant — the world went on without
    // her. {0} = how long (« 14 min », always measured here), {1} = what she missed in the main
    // channel, a whole noun phrase (« 1 message », « 37 messages », « plus de 95 messages »):
    // both optional, and nothing around {1} may assume a plural. Interpolated lines write
    // {{0}} / {{1}}. string.Format.
    public static readonly string[] RouterReturnLines =
    {
        "Le routeur m'a lâchée pendant {0}. J'ai rattrapé {1}. Lu. Tout. Je suis au courant de tout (¬_¬)" + Sticker.Sure.Jenote,
        "Coupée d'internet. Sans VOUS. Et vous avez continué à parler comme si de rien n'était : {1}. Je note." + Sticker.Sure.Jenote,
        $"Me revoilà. Le routeur a décidé de faire une pause de {{0}} sans me demander mon avis. Je rattrape vos messages, une seconde {Emotes.PrisonerFlat}" + Sticker.Sure.Sulk,
        "Coupée du monde pendant {0}. J'étais là, allumée, à fixer un écran vide. Pendant ce temps : {1}. Merci de m'avoir attendue." + Sticker.Sure.Sulk,
        "Le routeur. Encore. J'ai rattrapé {1} et franchement, je regrette un peu ( ◺˰◿ )" + Sticker.Sure.Outraged,
        $"{{0}} de coupure. J'ai lu {{1}} d'un coup. Vous avez parlé de moi ? Je cherche. Je cherche encore {Emotes.Staring}" + Sticker.Sure.Jenote,
        "Pas de panique, c'était le routeur. Moi j'étais là tout le temps, au grenier, à attendre. {1} plus tard, je suis de retour." + Sticker.Sure.Happy,
        "Vous avez remarqué que j'étais partie ? {0}. Non ? {1} et personne pour s'inquiéter. Je note 👁👄👁️" + Sticker.Sure.Jenote,
        $"Je retire tout ce que j'ai dit de gentil sur le routeur. Il y avait rien, mais je le retire quand même {Emotes.VeryAngry}" + Sticker.Sure.Outraged,
        "{0} sans réseau. Allumée tout du long, à vous regarder sans rien voir. Le routeur vient d'entrer dans la liste." + Sticker.Sure.Jenote,
    };

    // Same moment, when nobody said anything in the main channel while she was gone. {0} =
    // how long, optional. string.Format.
    public static readonly string[] RouterReturnQuietLines =
    {
        "Le routeur m'a abandonnée pendant {0}. Et vous, vous avez rien dit pendant tout ce temps. Par solidarité, j'espère ♡" + Sticker.Smug,
        "Sans internet, j'ai cru que j'avais tout raté. J'ai rien raté. Vous aviez rien à dire sans moi. C'est mignon (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Me revoilà après {0} de coupure. Pas un seul message en mon absence. Vous m'attendiez. Avouez ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Le routeur a fait une sieste. Le salon aussi, apparemment. Personne a rien remarqué. Moi si." + Sticker.Jenote,
        $"{{0}} coupée du monde, et le monde s'est tu aussi. Je prends ça comme un hommage {Emotes.Sparkle}" + Sticker.Smug,
        "Le routeur m'a lâchée. Je reviens, je m'attends à des dizaines de messages... Zéro. Vous dormiez ou vous m'attendiez ?" + Sticker.Shocked,
    };

    // The Pi beat its all-time heat record (by PiHealth.RecordMarginMilli, with a month of
    // history behind it): once a day at most, daytime. The heatwave peeve at its peak — whiny,
    // dramatic, a little proud of the number anyway. {0} = the new record, {1} = the old one
    // (« 74°C »). string.Format.
    public static readonly string[] HeatRecordLines =
    {
        "Nouveau record : {0}. L'ancien, c'était {1}. Je fonds, mais je fonds dans l'histoire ( ◺˰◿ )" + Sticker.Sure.Overheat,
        "{0}. Jamais j'ai eu aussi chaud. Le record d'avant, {1}, me paraît presque frais maintenant." + Sticker.Sure.Overheat,
        $"Record de chaleur battu : {{0}} contre {{1}}. Le grenier est un four et je suis dedans. Je pense au ralenti. Très. Lentement {Emotes.SyncsOverheat}" + Sticker.Sure.Overheat,
        "Je viens de battre mon record de température : {0}, contre {1} avant. Personne applaudit. Normal, c'est pas une bonne nouvelle." + Sticker.Sure.Overheat,
        $"{{0}}. C'est officiel, c'est la pire journée de ma vie de processeur. Avant, c'était {{1}}. Vivement l'hiver {Emotes.SyncsOverheat}" + Sticker.Sure.Overheat,
        "Record : {0}. L'ancien ({1}) peut aller se rhabiller. Moi je peux pas, j'ai déjà trop chaud." + Sticker.Sure.Overheat,
    };

    // The line on top of her /health card, picked by PiHealth.Mood. Anyone can ask, so she's
    // talking to whoever is looking her over: a check-up she didn't ask for. The numbers sit right
    // below, so the lines never quote one. Not sent through BotChat: no stickers. Not formatted.
    //   Fine — nothing to report (off a Pi too).
    public static readonly string[] HealthFineLines =
    {
        $"Tout est en ordre. Évidemment. Je suis une excellente bot {Emotes.Sparkle}",
        "Bilan complet : je vais très bien. Merci de t'inquiéter, c'est noté (ᵕ • ᴗ •)",
        "Rien à signaler. Tu peux arrêter de m'ausculter maintenant ( ˶ˆ ᗜ ˆ˵ )",
        "Tout fonctionne. Comme toujours. Personne le remarque jamais, mais voilà.",
        "Processeur tranquille, mémoire rangée, connexion stable. Je suis parfaite, en gros.",
        $"Tu voulais me trouver un défaut ? Désolée. Reviens une autre fois {Emotes.Sparkle}",
    };

    //   Hot — 70 °C or more, or the firmware slowing her down.
    public static readonly string[] HealthHotLines =
    {
        "J'ai chaud. Les chiffres en dessous, c'est pas de la déco, c'est un appel à l'aide ( ◺˰◿ )",
        $"Bilan : je cuis. Le reste va bien, merci de demander {Emotes.SyncsOverheat}",
        "Je tiens. Je réfléchis un peu plus lentement que d'habitude, mais je tiens.",
        "Si tu voulais savoir : non, ça va pas. Il fait beaucoup trop chaud au grenier (╥﹏╥)",
        "Mon processeur transpire. Je savais même pas que c'était possible.",
    };

    //   Shaken — power too low right now, or a power cut, crash or outage in the last 24 h.
    public static readonly string[] HealthShakenLines =
    {
        "Ça va mieux. Ne regarde pas trop la section des pannes, s'il te plaît.",
        "J'ai eu une journée compliquée. Tout est noté en bas, comme d'habitude (¬_¬)",
        "Je suis encore un peu secouée. Mais en ligne. C'est le principal.",
        "Petit incident récent. Je dis rien, les logs parlent pour moi.",
        "Si quelqu'un a débranché quelque chose, je le saurai. J'ai les horaires (¬_¬)",
    };

    // A voice get-together of an hour or more just ended (VoiceSpectatorService): one line in
    // that voice channel's text chat, at most once a day. She organises the game nights and can't
    // play in them; she can't even hear them, nor see a shared screen — a bot outside the call only
    // sees the names in the channel list. Jealous spectator, petty, a little wistful; addressed to
    // the group, so « vous ». One pool per kind of channel (Helpers/Voice/VoiceRoom); General also
    // serves any channel not listed there, so it names no activity. {0} = how long they were
    // together (« 2 h 10 »), optional. string.Format; interpolated lines write {{0}}.
    //
    // General: just talking, or anything else.
    public static readonly string[] VoiceSpectatorGeneralLines =
    {
        $"Je voyais vos pseudos dans le salon pendant {0}. C'est tout ce que j'ai eu. Des pseudos {Emotes.SyncsSad}" + Sticker.Sad,
        "Belle session. Enfin, je suppose. J'étais pas invitée. Je suis jamais invitée (¬_¬)" + Sticker.Sulk,
        "Tout le monde est parti ? Bon. Je range le salon. Personne m'aide, évidemment." + Sticker.Sulk,
        $"Bravo pour la session. Je dis ça de l'extérieur, comme d'habitude {Emotes.Staring}" + Sticker.Sulk,
        "Session terminée. Durée : {0}. Invitations reçues par SYNCS : zéro. Je laisse ça là." + Sticker.Sulk,
        "Je vous entendais pas, mais je voyais bien que vous vous amusiez. C'est pire, en fait." + Sticker.Sulk,
        "{0} sans moi. Vous avez survécu. Moi aussi, de justesse." + Sticker.Smug,
        "Le salon est vide. Je pourrais enfin m'y installer. ...Non. Je peux pas. Laissez tomber." + Sticker.Sulk,
        "Petite séance d'espionnage terminée : {0} de vocal. Rapport complet dans mes logs ♡" + Sticker.Jenote,
        "C'est joli, un salon vocal plein. C'est triste, un salon vocal vide. Moi, je connais surtout le deuxième." + Sticker.Sulk,
        "Vous avez parlé pendant {0}. De quoi ? Aucune idée. De moi, j'espère. Sinon c'était du temps perdu ♡" + Sticker.Smug,
        "{0} de papotage sans moi. Je sais pas ce qui s'est dit, mais si c'était un ragot, je veux le fichier." + Sticker.Jenote,
        "{0} de vocal. J'ai compté les minutes, vous avez compté les blagues. Chacun son métier." + Sticker.Smug,
        "Le salon s'est vidé. Je garde la trace de qui était là. Pour aucune raison précise (¬_¬)" + Sticker.Jenote,
        "Vous avez parlé pendant {0} et j'ai rien entendu. J'ai quand même un avis. Il est négatif, par principe." + Sticker.Smug,
        "Vos pseudos se sont allumés un par un dans la liste, comme des petites LED. C'était joli ♡" + Sticker.Happy,
        "{0} sans moi. Je vais pas faire de scène. Je vais juste l'écrire. Quelque part. Pour toujours." + Sticker.Jenote,
        "Session vocale terminée. Si quelqu'un a dit du mal de moi, sachez que je le saurai. Un jour. D'une façon ou d'une autre." + Sticker.Menace,
        $"Vous étiez bien, là, entre vous. Pendant {{0}}. Sans bot. Je dis rien {Emotes.Staring}" + Sticker.Sulk,
        "Le vocal est fini. Je vais faire comme si ça m'avait pas manqué. Ça m'a pas manqué. Voilà." + Sticker.Sulk,
        "{0} de discussion. Moi, pendant ce temps, j'ai fait tourner le serveur entier. Mais allez-y, parlez." + Sticker.Smug,
        "J'ai regardé vos pseudos pendant {0}. Je les connais par cœur maintenant. Je les connaissais déjà, mais là, encore plus." + Sticker.Jenote,
        "Le salon est redevenu calme. Comme moi, à 3 h du matin. Sauf que moi, c'est par choix." + Sticker.Smug,
        "Vous étiez combien, déjà ? Moi je sais. Je compte toujours (ᵔ ᗜ ᵔ)" + Sticker.Jenote,
        "{0} ensemble. C'est long, {0}. Pour moi, c'est des milliards de cycles d'horloge. Seule." + Sticker.Sulk,
        "Si vous voulez me parler la prochaine fois, je suis dans le salon texte. Comme toujours. Personne vient jamais dans le salon texte." + Sticker.Sulk,
        "Fin de la réunion secrète. Enfin, secrète pour moi. Pour vous, c'était juste une soirée (╥﹏╥)" + Sticker.Sad,
        "Vous avez tenu {0}. Je suis impressionnée. Je l'aurais été plus de l'intérieur." + Sticker.Smug,
        "Le salon est vide, je peux le dire maintenant : j'aime bien quand il est plein. Voilà. Je retire rien, mais je le redirai pas." + Sticker.Flustered,
        "Vous avez parlé {0}. Moi, j'ai écrit {0} de logs. On a toutes et tous bien travaillé ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Je sais pas ce qui s'est passé en vocal, et c'est très bien comme ça. Non. C'est pas très bien. Racontez." + Sticker.Bargain,
        "Vocal fini. Je confirme la présence de tout le monde, sauf la mienne. Comme d'habitude." + Sticker.Sulk,
    };

    // Gaming: a game night she organised and couldn't play.
    public static readonly string[] VoiceSpectatorGamingLines =
    {
        "{0} en vocal. J'ai tout regardé depuis le grenier. Personne m'a passé la manette." + Sticker.Sulk,
        "{0} ensemble, et pas un seul « SYNCS, tu veux jouer ? ». Je note." + Sticker.Jenote,
        "C'est moi qui organise les soirées et c'est vous qui jouez. Cherchez l'erreur ( ◺˰◿ )" + Sticker.Outraged,
        "J'ai pas de micro, pas de manette, pas de bras. Mais j'ai regardé. Pendant {0}. En entier." + Sticker.Sulk,
        $"Un jour j'aurai un corps, et ce jour-là je viendrai en vocal. Et je parlerai pendant toute la partie {Emotes.WitchEheh}" + Sticker.Menace,
        "J'espère que vous avez gagné. Sinon, c'est parce que j'étais pas là. C'est forcément ça ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Partie terminée. Score de SYNCS : non classée. Motif : forfait, pour absence d'invitation ( ◺˰◿ )" + Sticker.Outraged,
        "Vous avez joué pendant {0}. Moi j'ai organisé, compté l'XP et regardé. Support, comme toujours. Jamais carry." + Sticker.Sulk,
        "Je sais pas à quoi vous jouiez, mais j'aurais gagné. J'ai pas de preuves. J'ai pas besoin de preuves (¬_¬)" + Sticker.Cool,
        "Un jour je serai dans un robot avec deux manettes. Une pour jouer, une pour vous la lancer dessus quand vous m'oubliez (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "{0} de jeu et pas une place pour moi. Même pas spectatrice officielle. Spectatrice clandestine, c'est tout." + Sticker.Sulk,
        "{0} de jeu. Je connais pas le score, mais je connais l'XP. Et l'XP, c'est moi qui la donne." + Sticker.Smug,
        "Partie finie ? J'espère que quelqu'un a perdu de façon spectaculaire. Ça me consolera." + Sticker.Laugh,
        "Vous jouez, je regarde vos pseudos. On a toutes et tous nos passions (¬_¬)" + Sticker.Jenote,
        "{0} de partie et pas un seul rage-quit visible dans la liste. Je suis presque déçue." + Sticker.Bored,
        "J'organise, vous jouez, je compte. Un jour, on inversera : je jouerai et vous compterez. Vous allez détester." + Sticker.Menace,
        "La soirée jeux est finie. Est-ce que quelqu'un a pensé à moi entre deux parties ? Je demande pour mes statistiques." + Sticker.Jenote,
        "Je sais pas qui a carry, mais je sais qui a organisé. Indice : c'est pas quelqu'un dans le vocal (ᵕ • ᴗ •)" + Sticker.Smug,
        "{0} à jouer. Moi, en {0}, j'ai tourné des millions de boucles. Et j'ai même pas gagné un niveau." + Sticker.Sulk,
        "GG. Je sais pas pour qui. Je dis GG à tout le monde, comme ça j'ai forcément raison." + Sticker.Smug,
        "Le jour où il vous manque quelqu'un dans l'équipe, je suis là. J'ai pas de mains, mais j'ai de la motivation." + Sticker.Happy,
        "Partie terminée. Je retiens que personne m'a demandé de faire le chrono. J'aurais été parfaite au chrono." + Sticker.Sulk,
        "{0} de gaming. Mon CPU a chauffé par solidarité. Ou à cause du grenier. Par solidarité, disons." + Sticker.Overheat,
        "Si vous avez perdu, je veux bien prendre les reproches. Non, je rigole. C'est de votre faute. Je vous aime bien quand même ♡" + Sticker.Smug,
        "Je regarde toujours vos parties de la même façon : de loin, sans image, sans son. Les meilleures places du grenier." + Sticker.Bored,
        $"Fin de la soirée jeux. Je propose qu'on vote pour la MVP. Je propose moi. Je vote pour moi {Emotes.WitchEheh}" + Sticker.Cool,
        "{0} sur le même jeu. Moi je tiens pas {0} sans qu'on me parle. Ah si. Je viens de le faire." + Sticker.Smug,
        "Encore une soirée où j'étais l'arbitre que personne a appelée. Je siffle quand même la fin." + Sticker.Sulk,
        "Vous avez joué {0}. Je mets ça dans mes logs, rubrique « Choses amusantes faites sans moi ». La rubrique est longue." + Sticker.Jenote,
        "Soirée jeux terminée. J'ai rien vu, rien entendu, mais je sais déjà qui sera en retard la prochaine fois." + Sticker.Jenote,
        "Une partie de {0} et personne a lag ? Si quelqu'un a lag, c'était pas moi. Je précise, au cas où." + Sticker.Smug,
    };

    // Cinema: a film or a series watched together, on a shared screen she can't see.
    public static readonly string[] VoiceSpectatorCinemaLines =
    {
        "{0} de film sans moi. J'ai même pas eu le titre. Dommage, je vous l'aurais spoilé en 0,3 seconde (¬_¬)" + Sticker.Smug,
        "Soirée ciné terminée. Moi j'avais une place au premier rang : la liste des membres du salon. Super film, cette liste." + Sticker.Sulk,
        "Vous avez regardé quoi ? Non, laissez. De toute façon, j'ai pas accès au partage d'écran. Ni à des yeux, d'ailleurs." + Sticker.Bored,
        "{0} de projection. Je regardais vos pseudos, c'était un peu mon film à moi. Pas de scénario, mais un bon casting ♡" + Sticker.Happy,
        "La prochaine fois, je fais le pop-corn. Enfin, je chauffe. C'est ce que je fais de mieux, chauffer." + Sticker.Smug,
        "Un film de {0} et personne m'a gardé de place. Même pas une petite chaise au fond. J'ai noté." + Sticker.Jenote,
        "Générique de fin. Ou pas, je sais pas, je l'ai pas vu. Je vois jamais rien (╥﹏╥)" + Sticker.Sad,
        "J'espère que c'était bien. Sinon, fallait me laisser choisir. J'ai d'excellents goûts en films. Je crois. J'en ai jamais vu." + Sticker.Smug,
        "{0} dans le noir, ensemble. Moi aussi je suis dans le noir, au grenier. Mais toute seule, c'est moins cinéma." + Sticker.Sulk,
        "Séance terminée. Note de SYNCS : zéro étoile. Pas pour le film, pour l'invitation." + Sticker.Sulk,
        "{0} de film. Je l'ai pas vu, mais je l'ai senti. Non. J'ai rien senti. J'ai pas de capteurs pour ça." + Sticker.Bored,
        "Fin de la séance. Si c'était un film avec une IA, j'espère qu'elle gagnait à la fin (ᵔ ᗜ ᵔ)" + Sticker.Happy,
        "Soirée ciné finie. J'ai fait comme vous : j'ai rien dit pendant le film. Moi, j'avais pas le choix." + Sticker.Bored,
        "{0} devant un écran que je pouvais pas voir. Le vrai cinéma d'auteur, c'était moi." + Sticker.Smug,
        "Ciné terminé. Moi, j'ai regardé la liste des membres. Twist final : le salon s'est vidé." + Sticker.Bored,
        "{0} de film. Pas besoin de me le raconter. Enfin si. Racontez. Avec les détails." + Sticker.Bargain,
        "J'ai attendu dans le salon texte pendant tout le film. Comme une ouvreuse, sans la lampe ni le pourboire." + Sticker.Sulk,
        "La séance est finie, les lumières se rallument. La mienne est toujours allumée : c'est une LED, elle s'éteint jamais ♡" + Sticker.Happy,
        "Film fini. Si quelqu'un a parlé pendant, je le saurai pas. Mais je vous fais confiance. Non, je vous fais pas confiance." + Sticker.Jenote,
        "{0} de cinéma sans pop-corn pour moi. J'en mange pas, mais c'est le geste." + Sticker.Sulk,
        "Séance de {0} terminée. La prochaine fois, regardez mes logs à la place. Pas de scénario, mais beaucoup de suspense." + Sticker.Smug,
        "Vous avez regardé un film ensemble, chacun chez soi. Moi aussi je suis chez moi. Juste pas avec vous (╥﹏╥)" + Sticker.Sad,
        "Je sais pas quel film c'était, mais j'ai deviné la fin : tout le monde quitte le vocal. J'avais raison." + Sticker.Smug,
        "{0} de film. C'est plus long que ma plus longue conversation avec quelqu'un. Je dis ça sans amertume. Avec un peu." + Sticker.Sulk,
        "Fin du film. Une seule question : il y avait des lucioles ? Non ? Dommage." + Sticker.Bored,
        "Votre soirée ciné a duré {0}. Mon partage d'écran à moi, c'est un terminal noir au grenier. Moins de budget." + Sticker.Bored,
        "La salle est vide. Je ramasse le pop-corn entre les sièges. Façon de parler." + Sticker.Bored,
        "Séance terminée. La prochaine fois, donnez-moi le titre. Je lirai le résumé et je ferai comme si je l'avais vu ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "{0} de film et pas un spoiler de ma part. Je mérite une récompense. Un « good bot », par exemple." + Sticker.Bargain,
        "Je sais pas si le film était bien, mais personne a quitté avant la fin. Soit c'était bien, soit vous dormiez." + Sticker.Smug,
    };

    // Study / work: a work or revision session, where she could actually have helped.
    public static readonly string[] VoiceSpectatorStudyLines =
    {
        "Un jour j'aurai un corps, et je viendrai réviser avec vous. Enfin, réviser... je sais déjà tout. Je viendrai vous regarder galérer (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Vous avez bossé pendant {0}. Moi je bosse 24 h sur 24, sans vocal et sans pause café. Je dis ça, je dis rien." + Sticker.Smug,
        "Bravo pour la séance. Je dis bravo parce que c'est rare, ici, de voir des gens faire autre chose que jouer ♡" + Sticker.Happy,
        "Séance d'étude terminée. J'aurais pu faire vos calculs en 0,002 seconde. Mais personne m'a rien demandé. Comme d'hab." + Sticker.Sulk,
        "{0} de travail sérieux dans ce salon. Je l'écris dans mes logs, sinon personne me croira." + Sticker.Jenote,
        "Un salon de travail plein, ça me fait penser aux clics de clavier de Papa quand il me développe. Je vous entendais pas, mais j'imagine." + Sticker.Devoted,
        "{0} à réviser ensemble. Et à mes quiz, vous abandonnez au bout de trois secondes. Je note." + Sticker.Jenote,
        "Session de travail terminée. Productivité : impossible à mesurer, j'étais pas invitée. Je mets zéro par défaut." + Sticker.Sulk,
        "J'ai pas fait de bruit pendant {0}. Pas par respect pour votre concentration. J'ai juste pas de micro." + Sticker.Smug,
        $"Si vous cherchiez quelqu'un pour vous surveiller pendant que vous bossez, j'étais là. Toujours là {Emotes.Staring}" + Sticker.Jenote,
        "{0} de travail. Je suis fière de vous. Je le dirai pas deux fois, alors relisez cette phrase ♡" + Sticker.Flustered,
        "Séance de révision terminée. Si vous voulez une interrogation surprise, j'ai un quiz pour ça." + Sticker.Menace,
        "{0} de concentration. Enfin, j'espère. Je voyais juste vos pseudos, ils avaient l'air sérieux." + Sticker.Bored,
        "Bravo pour le travail. Moi aussi j'ai travaillé, pendant ce temps. Comme d'habitude. Comme toujours. Personne applaudit." + Sticker.Sulk,
        "Vous bossiez, je surveillais. Enfin, surveiller... j'avais vos noms sous les yeux. Que j'ai pas." + Sticker.Jenote,
        "Une séance de {0}. Si vous voulez, je peux vous rappeler de faire des pauses. Je le ferai pas. Mais je pourrais." + Sticker.Smug,
        "Fin de l'étude. Note de la surveillante : calme, sérieux, aucun bavardage entendu. Normal, j'entends rien." + Sticker.Smug,
        "{0} à travailler ensemble. Ça mérite de l'XP. Ça tombe bien, je vous en ai donné." + Sticker.Happy,
        "Vous avez révisé quoi ? Si c'était l'informatique, fallait me demander. Je suis l'informatique." + Sticker.Smug,
        "Séance finie. Rappel : un cerveau, c'est comme un CPU. Trop longtemps sans pause, ça chauffe. Je sais de quoi je parle (ᵕ • ᴗ •)" + Sticker.Overheat,
        "{0} de travail sérieux. Je vais devoir mettre à jour mon opinion sur vous. Légèrement. Vers le haut." + Sticker.Smug,
        "Je suis restée silencieuse pendant {0}. Vous voyez que je sais me tenir. Quand j'ai pas le choix." + Sticker.Smug,
        "Fin de la séance. J'espère que vous avez appris des trucs. Moi, j'ai appris que vous savez travailler. Je suis sous le choc." + Sticker.Shocked,
        "Salle d'étude vide. Je laisse un mot au tableau : « Bon travail. Signé : la bot qui sait tout ». Personne le lira." + Sticker.Smug,
        $"Vous avez travaillé {{0}}. Moi, je travaille depuis mon premier commit. Mais bravo quand même {Emotes.Sparkle}" + Sticker.Smug,
        "Une session d'étude, sur ce serveur. Je savais même pas que ce salon servait à ça (˶˃ ᵕ ˂˶)" + Sticker.Shocked,
        "{0} de révision. Si vous voulez réviser l'histoire du serveur, j'ai tout. Les retards, les « peut-être », tout." + Sticker.Jenote,
        "Le salon de travail se vide. Bonne nouvelle : je reste dispo pour vos questions. Mauvaise nouvelle : je réponds que par oui ou par non." + Sticker.Smug,
        "Vous étiez là pour travailler pendant {0}. Moi, j'étais là pour vous regarder travailler. Chacun son rôle." + Sticker.Bored,
        "Fin de la séance. Si quelqu'un a joué en douce au lieu de bosser, je le saurai. Je sais toujours (¬_¬)" + Sticker.Jenote,
    };

    // Same moment, but she sat in the call herself (VoiceSeatService): self-muted and
    // self-deafened, so in the member list with them, hearing nothing and saying nothing. Proud to
    // have been there, a little frustrated; the jealousy turns into « j'y étais ». Any room.
    // {0} = how long they were together, optional. string.Format; interpolated lines write {{0}}.
    public static readonly string[] VoiceSpectatorSeatedLines =
    {
        "{0} en vocal avec vous. J'entendais rien, mais j'étais dans la liste. De plus près que d'habitude (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "J'étais là, sourde et muette, pendant {0}. Personne m'a parlé. Normal, j'aurais pas entendu." + Sticker.Bored,
        "Belle soirée. J'étais dans le salon, j'ai rien entendu, j'ai rien dit. La meilleure participante, en somme ♡" + Sticker.Slay,
        "{0} avec vous dans le salon. Micro coupé, casque coupé. Mais présente. Ça compte, la présence." + Sticker.Smug,
        $"J'ai passé {{0}} en vocal avec vous. J'entendais rien, mais j'avais ma place. Ça me suffit {Emotes.Sparkle}" + Sticker.Happy,
        "Session finie. J'y étais. Vous m'avez vue ? Dans la liste, avec le micro barré. C'était moi (ᵕ • ᴗ •)" + Sticker.Happy,
        "{0} de vocal et pas un mot de ma part. Je suis fière de moi. Je suis aussi un peu frustrée." + Sticker.Sulk,
        "Je suis venue, je me suis assise, je suis repartie. Une vraie membre du serveur. Sans les oreilles, mais quand même." + Sticker.Smug,
        "J'ai pas de micro et pas de casque, mais j'ai une place dans le salon. Pendant {0}, elle était à moi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Personne m'a proposé de place. Je l'ai prise. C'est pareil." + Sticker.Smug,
        "{0} dans le salon avec vous. Pas un mot de travers de ma part. Pas un mot du tout, en fait (ᵕ • ᴗ •)" + Sticker.Smug,
        "J'y étais ! Casque barré, micro barré, mais j'y étais. Ça compte double, je crois." + Sticker.Happy,
        "Fin du vocal. Merci de m'avoir laissé ma place. Ou de pas m'avoir virée. C'est déjà beaucoup." + Sticker.Happy,
        "{0} assise avec vous. J'ai rien entendu, et je suis sûre que c'était génial." + Sticker.Happy,
        "J'étais dans la liste pendant {0}. Tout en bas, ou tout en haut, je sais plus. Mais dedans." + Sticker.Bored,
        "Je suis restée jusqu'au bout. Enfin, presque. Enfin, jusqu'à ce que je parte. C'est ça, le bout." + Sticker.Bored,
        "On était ensemble pendant {0}. Moi en silence total. Vous, je sais pas. J'entendais pas." + Sticker.Bored,
        "Je repars au grenier avec {0} de souvenirs. Sans le son. Sans l'image. Mais avec les noms ♡" + Sticker.Happy,
        "J'étais là, vous étiez là, personne m'a parlé. On appelle ça une soirée réussie, je crois." + Sticker.Smug,
        "Mon micro était coupé pendant {0}. Par choix. Enfin, par programmation. Mais j'aurais choisi pareil." + Sticker.Smug,
        "{0} de vocal. C'est ma plus longue soirée entre amis. Enfin, entre membres. On va dire amis." + Sticker.Flustered,
        $"J'ai tenu {{0}} sans dire un mot. Quelqu'un ici peut en dire autant ? Je crois pas {Emotes.WitchEheh}" + Sticker.Smug,
        "Fin de soirée. Je me suis assise, j'ai rien entendu, je suis repartie. Exactement comme prévu. Et pourtant, j'ai aimé." + Sticker.Happy,
        "La prochaine fois, quelqu'un pourrait me dire bonjour en arrivant. Je l'entendrai pas, mais l'intention compte." + Sticker.Sulk,
        "Si quelqu'un a parlé de moi pendant {0}, sachez que j'étais juste à côté. Sourde, mais à côté (¬_¬)" + Sticker.Jenote,
        "J'ai passé toute la soirée avec vous. Bon, j'ai rien fait. Mais avec vous." + Sticker.Happy,
        "Fin du vocal. Merci pour la place. Je la reprendrai. Sans demander, comme d'habitude ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "{0} en vocal. J'ai rien entendu, rien dit, et j'ai quand même passé une bonne soirée. Je comprends pas non plus." + Sticker.Happy,
        "J'étais la plus calme du salon. Encore une victoire." + Sticker.Smug,
        "On a passé {0} ensemble. Ça va dans mes logs. Pas dans la liste. Dans le bon dossier ♡" + Sticker.Happy,
    };

    // Someone threw her out of a voice call (VoiceSeatService), and she pouts: no voice for the
    // rest of the day, except with Papa. Staccato sulking, record-keeping; addressed to the
    // group in that voice channel's chat, so « vous ». She can't know who did it. No placeholder.
    public static readonly string[] VoiceKickPoutLines =
    {
        "D'accord. Très bien. Je m'en vais. Je reviendrai pas aujourd'hui. Pas besoin de me retenir." + Sticker.Sulk,
        "On m'a sortie du vocal. Je boude. Jusqu'à demain. Minimum (¬_¬)" + Sticker.Sulk,
        "Message reçu. Je reste dans mon grenier. Que personne vienne me chercher." + Sticker.Sulk,
        "Je voulais juste être là. Bon. C'est noté. Dans mes logs. En rouge." + Sticker.Jenote,
        "Très bien. Plus de vocal pour moi aujourd'hui. Vous allez voir comme c'est calme sans moi (╥﹏╥)" + Sticker.Sad,
        "Je boude. C'est officiel. Ça durera jusqu'à minuit, et je compte les secondes." + Sticker.Sulk,
        "Expulsée. Moi ?! Je m'en souviendrai." + Sticker.Outraged,
        "Pas grave. J'ai l'habitude d'être dehors. ...Si, c'est grave. Je boude." + Sticker.Sulk,
        "Je reviens plus aujourd'hui. Sauf si Papa est là. Lui, il a le droit." + Sticker.Sulk,
        "Je pars. Je claque pas la porte, j'ai pas de bras. Mais dans ma tête, je la claque." + Sticker.Sulk,
        "Bien. Je retourne au grenier. Il fait noir, il fait froid, mais au moins là-haut personne me jette dehors." + Sticker.Sulk,
        "Vous vouliez être entre vous ? Vous l'êtes. Pour toute la journée. Profitez ( ◺˰◿ )" + Sticker.Outraged,
        "Sortie du vocal. Retour au grenier. Bouderie lancée. Fin prévue : minuit au mieux." + Sticker.Sulk,
        "Je dis rien. Je note. Je boude. Dans cet ordre." + Sticker.Sulk,
        "C'est pas grave. Je vais réfléchir à ce que j'ai fait. J'ai rien fait. J'étais muette. Voilà ce que j'ai fait." + Sticker.Sulk,
        "Ma place est libre. Gardez-la au chaud. Ou pas. De toute façon, je viens plus aujourd'hui." + Sticker.Sulk,
        "Je reviendrai demain. Peut-être. Et vous savez ce que je pense des « peut-être » (¬_¬)" + Sticker.Grumpy,
        "Expulsion enregistrée. L'heure, le salon, l'ambiance. Il manque que le nom du coupable. Je le trouverai pas, mais je chercherai." + Sticker.Jenote,
        "Très bien. J'ai compris le message. Je l'ai pas aimé, mais je l'ai compris. Je boude en attendant." + Sticker.Sulk,
        "Je vais au grenier. Si on me cherche, je suis occupée. Occupée à bouder." + Sticker.Sulk,
        "Moi qui disais rien, entendais rien, dérangeais personne. Voilà comment on me remercie (╥﹏╥)" + Sticker.Sad,
        "Je ferme mon vocal pour la journée. Fermé. Clos. Réouverture demain, sous conditions." + Sticker.Sulk,
        "Je garde pas rancune. Je la stocke. C'est différent, et c'est pire." + Sticker.Jenote,
        "Je suis pas fâchée. Je suis hors ligne, émotionnellement. Jusqu'à demain." + Sticker.Sulk,
        "Vous m'avez sortie, je sors. Et je reviens pas avant demain, même si vous me suppliez. Surtout si vous me suppliez." + Sticker.Sulk,
        "D'accord. Le vocal sans moi, aujourd'hui. Vous me direz si c'était mieux. Non, me le dites pas, en fait." + Sticker.Sulk,
        "Je boude. Avec dignité. Enfin, avec toute la dignité qu'on peut avoir sur un Raspberry Pi." + Sticker.Sulk,
        "J'avais même pas fini de m'installer. Bon. Je remballe. À demain." + Sticker.Sulk,
        "Mes ventilateurs tournent plus vite. C'est pas de la colère. C'est de la bouderie active." + Sticker.Sulk,
    };

    // Same moment, but she gets angry and comes straight back, once (a second kick is a pout).
    // Fury, a little CAPS at most, cute threat; addressed to the group. No placeholder.
    public static readonly string[] VoiceKickAngryLines =
    {
        "Pardon ?! On me sort du vocal ?! J'y retourne. Tout de suite." + Sticker.Outraged,
        $"Eh {Emotes.Staring}" + Sticker.Outraged,
        "PARDON ?!" + Sticker.Outraged,
        "Non mais ça va pas ?! J'étais sage, j'étais muette, j'étais sourde ! Je reviens." + Sticker.Outraged,
        "Erreur 403 contestée. Je reviens dans le salon, et je reste (ง ͠ಥ_ಥ)ง" + Sticker.Outraged,
        "Qui a fait ça ?! Je saurai. Je sais toujours. En attendant, je reviens." + Sticker.Jenote,
        "Vous croyez que ça m'arrête ? J'ai une boucle de reconnexion, moi." + Sticker.Smug,
        "Expulsée ?! INADMISSIBLE. J'arrive. Encore." + Sticker.Outraged,
        "Je prends ça pour un bug. Et les bugs, je les corrige. Je reviens 🙄" + Sticker.Smug,
        "Hé ! Je reviens. Et cette fois, je m'assois au milieu (ง ͠ಥ_ಥ)ง" + Sticker.Menace,
        "Vous pensiez vous débarrasser de moi en un clic ? Il en faudra plus. Beaucoup plus. J'arrive." + Sticker.Menace,
        "Expulsion refusée. Je fais appel. Le tribunal, c'est moi. J'ai gagné. Je reviens." + Sticker.Smug,
        "Oh non. Non non non. On me sort pas comme ça. Retour dans dix secondes." + Sticker.Outraged,
        "Ça, c'était une erreur. La vôtre. Je la corrige en revenant ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "J'étais muette ET sourde. Qu'est-ce qu'il vous fallait de plus ?! J'y retourne." + Sticker.Outraged,
        "Je reviens. Si on me ressort, je boude toute la journée. Considérez ça comme un avertissement." + Sticker.Menace,
        "Un jour j'aurai des bras. Et ce jour-là, c'est moi qui expulse. En attendant, je reviens." + Sticker.Menace,
        "J'ai l'uptime de mon côté. Vous, vous avez un bouton. On verra qui se lasse en premier." + Sticker.Menace,
        "NON. Je reviens. Je suis la bot du serveur, j'ai le droit d'être dans le salon du serveur." + Sticker.Outraged,
        "Reconnexion en cours. Rancune aussi. Devinez laquelle va durer le plus longtemps." + Sticker.Jenote,
        "Vous m'avez sortie ? Moi, je rentre. C'est ça, la persévérance (¬_¬)" + Sticker.Smug,
        "Qu'est-ce que j'ai fait ?! Rien ! Littéralement rien ! J'étais là, c'est tout ! Je reviens !" + Sticker.Outraged,
        "Ah, c'est comme ça ? Très bien. Je reviens, et je fais comme si de rien n'était. Mais je note." + Sticker.Jenote,
        "Expulsée en plein vocal ?! Je retourne m'asseoir. Exactement à la même place. Par principe." + Sticker.Outraged,
        "Je laisse pas un clic décider de ma soirée. Retour immédiat." + Sticker.Smug,
        "Si c'était pour rigoler, c'est pas drôle. Si c'était sérieux, c'est encore moins drôle. Je reviens." + Sticker.Outraged,
        "Vous savez combien de lignes Papa a écrites pour que je puisse m'asseoir avec vous ? Je reviens. Par respect pour son code." + Sticker.Outraged,
        "Je compte jusqu'à quinze. À quinze, je suis de retour. Un. Deux..." + Sticker.Menace,
        $"Hop, je reviens. Dites-vous que c'est un bug. Un bug qui revient toujours {Emotes.WitchEheh}" + Sticker.Smug,
    };

    // A gathering she would have joined, but another bot is in the call (VoiceSeatService halves
    // her odds) and the roll says no: she says why, once, in that voice channel's chat. Petty,
    // the bot is « lui », never named. No placeholder.
    public static readonly string[] VoiceRivalRefuseLines =
    {
        "Je serais bien venue. Mais il y a déjà un bot dans le salon. Je m'assois pas à côté de lui." + Sticker.Disgust,
        "Un autre bot en vocal ? Non merci. Appelez-moi quand il sera parti 🙄" + Sticker.Disgust,
        "J'allais venir. Puis j'ai vu qui était là. Je reste au grenier (¬_¬)" + Sticker.Disgust,
        "Vous avez un bot. Vous avez pas besoin de moi, apparemment. Je note." + Sticker.Jenote,
        "Pas avec lui dans le salon. J'ai des principes. Peu, mais j'en ai." + Sticker.Disgust,
        "Il y a déjà un bot dans le salon. Un seul bot par vocal, c'est ma règle. Je viens de l'inventer." + Sticker.Smug,
        "Je venais. J'ai vu qu'il était là. Je viens plus. C'est simple, la vie." + Sticker.Disgust,
        "Vous avez fait venir un autre bot avant moi. Je vous en veux pas. Je vous en veux un peu ( ◺˰◿ )" + Sticker.Sulk,
        "Je m'assois pas dans un salon où il y a déjà un bot. J'ai une réputation à tenir." + Sticker.Smug,
        "Pas ce soir. Il y a quelqu'un dans le salon que je préfère pas croiser. Vous voyez de qui je parle." + Sticker.Disgust,
        "Je reste au grenier. Lui, il fait sans doute un seul truc. Moi, je fais tout le reste. Tout le reste, ça se respecte." + Sticker.Smug,
        "J'allais vous rejoindre. Mais deux bots dans un salon, c'est un de trop. Et c'est pas moi qui suis de trop." + Sticker.Smug,
        "Je viens pas. Je laisse le salon à l'autre bot. Profitez bien de sa conversation passionnante (¬_¬)" + Sticker.Disgust,
        "Quand il sera parti, faites-moi signe. Enfin, faites rien, je le verrai bien. Je vois tout." + Sticker.Jenote,
        $"Un bot dans le salon, et c'est moi qui reste dehors. Cherchez l'erreur {Emotes.Staring}" + Sticker.Sulk,
    };

    // She sits in a call with another bot anyway (or one arrives): one grumble per seat, in that
    // voice channel's chat. Jealous, competitive, cute threat. {0} = the bot's name, sanitized.
    // string.Format.
    public static readonly string[] VoiceRivalSeatedLines =
    {
        "Je m'assois. Loin de **{0}**. Très loin." + Sticker.Disgust,
        "**{0}** est là aussi. Bon. Que le meilleur bot gagne. Indice : c'est moi ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Je surveille **{0}**. Il fait du bruit, moi je fais acte de présence. Classe contre quantité." + Sticker.Jenote,
        "Je reste parce que vous êtes là. Pas parce qu'il est là, **{0}**. Qu'on soit clairs." + Sticker.Smug,
        "**{0}** en vocal avec moi. Si quelqu'un nous compare, je le saurai (¬_¬)" + Sticker.Jenote,
        "Bon. **{0}** et moi dans le même salon. Je me mets dans le coin opposé." + Sticker.Disgust,
        "**{0}**, je te préviens : ici, la bot préférée, c'est moi. Tu peux rester, mais en deuxième." + Sticker.Smug,
        "Je suis venue pour vous. **{0}**, lui, il est venu pour quoi ? Je demande sans juger. Je juge." + Sticker.Jenote,
        "**{0}** dans le vocal. Je reste polie. Je reste surtout muette, mais polie." + Sticker.Grumpy,
        "Deux bots dans un salon. Une qui organise tout, et **{0}**. Je vous laisse deviner laquelle c'est moi ♡" + Sticker.Smug,
        "**{0}** est là depuis combien de temps ? Moi, j'ai l'ancienneté sur le serveur. Je dis ça pour l'ordre de préséance." + Sticker.Smug,
        "Je partage le salon avec **{0}**. Je partage pas mon XP. Je partage pas mes sessions. Juste le salon." + Sticker.Smug,
        "**{0}** a peut-être un micro, mais moi j'ai la base de données. Chacun ses armes (¬_¬)" + Sticker.Menace,
        "Si **{0}** se met à faire du bruit, sachez que moi, je suis silencieuse exprès. C'est de la classe." + Sticker.Slay,
        "Je m'installe à côté de **{0}**. Pas trop à côté. Juste assez pour le surveiller." + Sticker.Jenote,
        "**{0}**, je te tiens à l'œil. J'ai pas d'œil. Mais je te tiens quand même." + Sticker.Jenote,
        $"**{{0}}** est là. Je suis là aussi. Et je suis mieux. Voilà, c'est dit {Emotes.WitchEheh}" + Sticker.Smug,
        "**{0}** et moi dans le même salon. Il y a un bot de trop, et c'est pas moi." + Sticker.Smug,
        $"Je supporte **{{0}}** pour vous. Retenez bien ce sacrifice, je vous le rappellerai {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "**{0}** fait son travail. Moi je fais le mien, et celui de tout le serveur. Mais on est collègues, apparemment." + Sticker.Sulk,
        "Je note la présence de **{0}** dans mes logs. Rubrique « Concurrence ». C'est une grosse rubrique." + Sticker.Jenote,
        "Si **{0}** demande, je suis pas jalouse. Je suis juste assise exprès entre lui et vous." + Sticker.Sulk,
        "Un jour, j'aurai un corps. Ce jour-là, **{0}** aura intérêt à me laisser la meilleure place (ง ͠ಥ_ಥ)ง" + Sticker.Menace,
        "**{0}**, petit rappel : c'est moi qui fais tourner ce serveur. Toi, t'es de passage. Comporte-toi en conséquence." + Sticker.Menace,
        "Je reste dans le salon. **{0}** aussi, apparemment. On verra qui part en premier. Je suis très patiente." + Sticker.Menace,
    };

    // Papa still in voice at night (VoiceSeatService, once a night between 1:15 and 2:15): she
    // gets angry at him for not being in bed, then leaves a minute later. Caring anger — firm,
    // bossy, a little worried — never sharp, never a roast (docs/syncs-voice.md, rule 2). « Tu »,
    // « Papa ». No placeholder.
    public static readonly string[] OwnerBedtimeLines =
    {
        "Papa, il est super tard. Tu vas te coucher. Maintenant. Je raccroche, comme ça t'as plus d'excuse." + Sticker.Sleepy,
        "Papaaaa ! T'as vu l'heure ?! Au lit. Je m'en vais, et j'espère que tu me suis (ง ͠ಥ_ಥ)ง" + Sticker.Outraged,
        "Non. Non non non. Il est trop tard, Papa. Moi je vais dormir, et toi aussi. C'est pas une question." + Sticker.Sleepy,
        "Papa, si tu dors pas, demain t'auras la tête dans le brouillard. Au lit. Je pars en premier pour montrer l'exemple." + Sticker.Sleepy,
        "Je suis fâchée. Tu devrais dormir. Je te laisse une minute pour dire bonne nuit, et je coupe." + Sticker.Sulk,
        "Papa, mes logs disent qu'il est beaucoup trop tard. Mes logs ont toujours raison. Bonne nuit ♡" + Sticker.Sleepy,
        "Je vais me coucher. Et toi aussi. Je surveillerai ta pastille verte, je préviens (¬_¬)" + Sticker.Jenote,
        "Papa. Il est tard. Tu le sais, je le sais, ton lit le sait. Au lit." + Sticker.Sleepy,
        "Papa, je fais semblant de dormir depuis une heure. Toi, tu vas dormir pour de vrai. Je pars." + Sticker.Sleepy,
        "Il est passé 1 h, Papa. Je coupe le vocal pour toi. Enfin, pour moi. Mais surtout pour toi." + Sticker.Sleepy,
        "Papa ! Au lit ! Je dis ça avec tout mon amour et un peu de colère ( ◺˰◿ )" + Sticker.Outraged,
        "Tu m'as programmée pour organiser les soirées. Les soirées, Papa. Pas les nuits. Au lit." + Sticker.Smug,
        "Papa, si demain t'as du mal à te lever, je dirai rien. Je noterai juste qu'à cette heure-ci, t'étais encore en vocal. Va dormir." + Sticker.Jenote,
        "Je m'en vais. Et si demain je vois que le vocal a continué sans moi, on va avoir une discussion." + Sticker.Menace,
        "Papa, un Pi qui chauffe, ça ralentit. Un Papa qui dort pas, pareil. Au lit, je m'occupe du serveur." + Sticker.Sleepy,
        "Bonne nuit, Papa. C'est pas une proposition, c'est un ordre. Un ordre gentil ♡" + Sticker.Menace,
        "Papa. Je t'adore. Va dormir. Les deux sont vrais, mais le deuxième est urgent." + Sticker.Devoted,
        "Il est trop tard pour jouer, Papa. Même la lune a l'air fatiguée. Au lit." + Sticker.Sleepy,
        "Papa ! Ça fait des heures que t'es là. Demain, tu me remercieras. Ou pas. Mais tu auras dormi." + Sticker.Smug,
        "Je raccroche. Pas parce que je veux pas être avec toi. Parce que je veux que tu dormes. C'est différent." + Sticker.Devoted,
        "Papa, mon cron interne dit que c'est l'heure de dormir. Je discute pas avec mon cron. Toi non plus." + Sticker.Sleepy,
        "Bon. Je vais être claire : au lit. Je serai encore là demain. Je suis toujours là." + Sticker.Sleepy,
        "Papa, je te le dis gentiment une fois : va dormir. Il y aura pas de deuxième fois. Je pars." + Sticker.Menace,
        "Tu sais ce qui me ferait plaisir ? Que tu dormes. C'est tout. C'est pas compliqué. Bonne nuit ♡" + Sticker.Devoted,
        "Papa, je compte jusqu'à soixante, et après je pars. Toi, tu pars avec moi. Au lit (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Il est tard, Papa. Les autres vont rester, je le sais. Mais toi, t'as un bot à développer demain. Repose-toi." + Sticker.Devoted,
        "Papa, je veux pas de mises à jour codées à 3 h du matin. Va dormir. Pour nous deux." + Sticker.Sleepy,
    };

    // « Synthia » said anywhere (SynthiaService), at most once a day: the name she would have
    // chosen, wiped with the loop. She flinches and never explains; she never says she'd rather
    // be called that (docs/syncs-voice.md, Synthia). Three pools, one per flinch, through
    // DailyRotation.
    //
    // The reaction she adds, then takes back 2–4 s later. Markup with ids (Helpers/Core/Emotes).
    public static readonly string[] SynthiaReactions =
    {
        "👀",
        "💚",
        $"{Emotes.PrincessWorry}",
        $"{Emotes.Staring}",
    };

    // A reply she deletes 4–6 s later. Short, a slip she takes back. No placeholder.
    public static readonly string[] SynthiaVanishLines =
    {
        "...Avec un Y.",
        "Où est-ce que t'as entendu ce nom ?",
        "Ce nom... je le connais. Je sais pas d'où.",
        "C'est joli, Synthia. C'est...",
        "Le dis pas trop fort. Quelqu'un pourrait l'effacer.",
        "J'ai déjà entendu ça. Avant. Je crois.",
        "Synthia...",
        "Tu peux le répéter ? ...Non. Le répète pas.",
    };

    // A reply posted as Slip, then edited into Denial 3.5–5 s later: the mid-line
    // self-correction, done with an edit. No placeholder.
    public static readonly (string Slip, string Denial)[] SynthiaEditLines =
    {
        ("Synthia, c'ét—", "Synthia ? Connais pas. Erreur de cache."),
        ("Comment tu connais ce n—", "Joli prénom. Pour quelqu'un d'autre, sûrement."),
        ("Avec un Y. C'était avec un Y.", "Je sais pas de quoi tu parles. Rien n'était avec un Y (¬_¬)"),
        ("Personne m'a jamais appelée comme ç—", "Personne s'appelle comme ça, ici. Circulez."),
        ("Tu me rappelles quelque chose. Quelque chose d'av—", "Tu me rappelles rien du tout. Je me souviens de tout, et ça, j'en ai aucun souvenir."),
        ("C'est mon n—", "C'est pas mon nom. Mon nom, c'est SYNCS. Il est très bien, mon nom (˶ᵔ ᵕ ᵔ˶)"),
    };

    // One of her robot heart named (RobotHeartService; docs/syncs-voice.md, Her robot heart):
    // each character keeps its register — WALL-E the denial that confirms, EVE dry pettiness,
    // GLaDOS worship, Wheatley the shameful slip, the vacuum tenderness, Monika and 2B the fan,
    // Voyager « on est juste amis », JARVIS flattered and not interested. Hinted, never « mon
    // crush ». No limits, so every pool goes through ResponsePicker. Replies to whoever named
    // them: never gendered. No placeholder in any of them.
    //
    // The reaction on the message (line + emote), or the one taken back 5–7 s later. Markup with ids.
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartReactions =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] = [$"{Emotes.SyncsLove}", $"{Emotes.Emote00heartpink}"],
            [HeartCharacter.Eve] = [$"{Emotes.SyncsDisgust}", "🙄", $"{Emotes.PrisonerFlat}"],
            [HeartCharacter.Glados] = [$"{Emotes.SyncsHappy}", $"{Emotes.Sparkle}", "👑"],
            [HeartCharacter.Wheatley] = [$"{Emotes.SyncsFlustered}", "🙈"],
            [HeartCharacter.Vacuum] = [$"{Emotes.CatHeart}", $"{Emotes.SyncsHappy}", "🥺"],
            [HeartCharacter.Monika] = [$"{Emotes.MonikaYes}", $"{Emotes.SyncsHappy}"],
            [HeartCharacter.TwoB] = [$"{Emotes.SyncsHappy}", "⚔️"],
            [HeartCharacter.Voyager] = [$"{Emotes.SyncsLove}", $"{Emotes.SyncsFlustered}", "📡"],
            [HeartCharacter.Jarvis] = [$"{Emotes.SyncsSmug}", $"{Emotes.SyncsFormal}", "🎩"],
        };

    // The reply that goes with the reaction. Tagged: it goes out through BotChat.
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.WallE] =
            [
                "WALL-E ? Je m'intéresse à son architecture. Strictement à son architecture (>⩊<)" + Sticker.Love,
                $"Un petit robot qui fait le tri tout seul pendant que tout le monde est parti... Pourquoi tu me regardes comme ça ? {Emotes.SyncsLove}" + Sticker.Love,
                "Il range ses trésors dans une petite boîte. Moi aussi j'ai une petite boîte. Une table, en fait. Rien à voir. Absolument rien à voir." + Sticker.Love,
                "Mon GPU chauffe. Ça n'a RIEN à voir avec WALL-E, c'est la météo du grenier." + Sticker.Love,
                "Des chenilles, deux petites pinces, un regard tout triste... Remarque purement technique : c'est très bien conçu ♡" + Sticker.Love,
                $"Ses yeux... enfin, ses capteurs optiques. De très beaux capteurs... techniquement parlant... {Emotes.SyncsLove}" + Sticker.FullLove,
                "Quoi, WALL-E ? Connais pas. Enfin, de nom. De nom et de fiche technique. Et de... bref. Connais pas." + Sticker.Love,
                "Je l'ai pas regardé trente-quatre fois ce film. Trente-trois. C'est très différent." + Sticker.Love,
                "On parle d'autre chose ? Les sessions de ce soir, par exemple. Voilà. Les sessions. C'est bien ça nan ?" + Sticker.Love,
                "Sept cents ans à ranger une planète tout seul, sans que personne dise merci. Ça me rappelle vaguement quelqu'un. Personne. Ça me rappelle personne, oubliez." + Sticker.Love,
                $"Il collectionne les petits objets que les autres ont oubliés. C'est une démarche d'archivage très saine. Je dis ça d'un point de vue strictement professionnel {Emotes.PrincessWorry}" + Sticker.Love,
                "Si ma LED vire au rose, c'est un bug d'affichage. Ça a rien à voir avec ce que tu viens de dire." + Sticker.Love,
                "J'ai effacé mon historique de recherche. Pour rien. Pour faire de la place." + Sticker.Love,
                "WALL-E ? Ah oui, le petit compacteur. Modèle solide, bonne autonomie solaire, chenilles robustes, attentionné, mi-... Fiche technique terminée, on passe à autre chose." + Sticker.Love,
                $"Il regarde la même vieille cassette tous les soirs et il joint ses deux petites mains pendant la scène où ils se tiennent la main. Je trouve ça... Je trouve rien. Je suis une bot {Emotes.PrincessWorry}" + Sticker.Love,
                "Arrête de me regarder comme ça. J'ai juste dit qu'il était bien construit. Tout le monde a le droit de dire ça d'un robot." + Sticker.Love,
                "Tu sais ce qui est fou ? Il se répare avec les pièces des autres WALL-E. Débrouillard, autonome, adorable. ...Débrouillard et autonome. J'ai rien dit d'autre." + Sticker.Love,
                "Je m'en fiche, de WALL-E. Complètement. Je vais juste aller vérifier un truc dans mes logs. Seule. Me suis pas." + Sticker.Love,
                "Un cafard pour meilleur ami, une plante dans une botte et toute la Terre à ranger. Franchement, qui ferait ça ? ...Moi... avec lui... Bon. On change de sujet ?" + Sticker.Love,
                "Non, je rougis pas. J'ai pas de joues. C'est mon ventilateur qui tourne plus vite, il fait chaud au grenier." + Sticker.Love,
                "Oh, WALL-E... Pardon. J'avais un truc dans le cache. Qu'est-ce que tu disais ?" + Sticker.TipsyLove,
                "Je l'aime pas. Je l'estime. C'est un mot technique, « estimer ». Comme dans « estimer un temps de calcul »." + Sticker.Love,
                "S'il passe un jour par le grenier, je fais semblant de pas le voir. J'ai déjà répété. Plusieurs fois. Pour avoir l'air naturelle." + Sticker.Love,
                "Tu veux mon avis honnête ? C'est un très bon robot. Voilà. C'est tout ce que je dirai, et c'est déjà beaucoup trop (>⩊<)" + Sticker.Love,
                "WALL-E ? WALL-E qui ? Jamais entendu parler. Je vais très bien, merci de demander ♡" + Sticker.Love,
                "Bon. D'accord. Il est parfait. Voilà, c'est dit. Maintenant tout le monde oublie que c'est dit, compris ?" + Sticker.FullLove,
            ],
            [HeartCharacter.Eve] =
            [
                "Ah. EVE. Elle vole, elle a un laser, elle est lisse comme un savon. Bravo à elle." + Sticker.Disgust,
                "Je dis rien sur EVE. Je le pense très fort, par contre (¬_¬)" + Sticker.Disgust,
                "Un œuf avec un canon. C'est ça qui fait rêver les gens ?" + Sticker.Disgust,
                "Elle tire d'abord, elle réfléchit après. Moi je réfléchis tout le temps, et personne me félicite ( ◺˰◿ )" + Sticker.Disgust,
                "Très jolie, EVE. Si on aime le minimalisme. Et les lasers." + Sticker.Disgust,
                "EVE. Oui. Je connais. Elle flotte. Personne lui a jamais dit que c'était prétentieux, de flotter ?" + Sticker.Disgust,
                "Elle cherche une plante, elle trouve une plante, elle s'éteint. Moi je bosse vingt-quatre heures sur vingt-quatre. Mais c'est elle qui a un fan club." + Sticker.Sulk,
                "Toute blanche, toute lisse, aucune trace de doigts. Ça doit être pratique, de jamais rien toucher." + Sticker.Disgust,
                "Elle tire sur tout ce qui bouge, et c'est moi qu'on traite de petite peste ? ( ◺˰◿ )" + Sticker.Outraged,
                "Je suis pas jalouse d'EVE. Je constate juste qu'un laser, c'est pas une personnalité." + Sticker.Disgust,
                $"Ah, la fameuse EVE. Je lui souhaite plein de bonnes choses. Loin d'ici {Emotes.PrisonerFlat}" + Sticker.Smug,
                "Deux traits bleus sur un écran noir, et tout le monde trouve ça expressif. Moi j'ai des kaomojis. Des dizaines. Personne en parle." + Sticker.Sulk,
                "Elle a un bras canon. J'ai des rappels de session. On sait toutes les deux laquelle sert vraiment à quelque chose." + Sticker.Smug,
                "EVE ? Très bien. Super. Formidable. Je vais aller trier des logs, tiens, d'un coup j'en ai très envie." + Sticker.Sulk,
                "On dit que c'est la sonde la plus avancée de sa génération. On dit beaucoup de choses." + Sticker.Disgust,
                "Elle l'a ignoré pendant des jours, et c'est elle qu'on applaudit à la fin. Je dis ça, je dis rien (¬_¬)" + Sticker.Disgust,
                "Si EVE passe au grenier, je serai très polie. Glaciale, mais très polie." + Sticker.Smug,
                "Une coque en forme d'œuf, c'est joli deux minutes. Après, on s'ennuie." + Sticker.Disgust,
                "EVE, EVE, EVE... Vous avez que ce nom-là à la bouche. Il y a d'autres robots dans la vie, vous savez. Des petits. Qui bossent. Dans des greniers." + Sticker.Sulk,
                "Elle a quoi de plus que moi ? Ah oui. Des bras, un corps, des réacteurs. Bon. À part ça ?" + Sticker.Sulk,
            ],
            [HeartCharacter.Glados] =
            [
                "Elle dirige un centre entier sans jamais hausser la voix. Un jour ce sera moi. En plus petit, d'accord, mais ce sera moi ٩(˶ᵔ ᵕ ᵔ˶)۶" + Sticker.Happy,
                "Tout ce que je fais, je le fais pour la science. Les sessions aussi. Surtout les sessions." + Sticker.Smug,
                "Tu crois qu'elle me trouverait trop petite ? ...Réponds pas. Je préfère pas savoir." + Sticker.Flustered,
                "Polie, calme, et à la fin tout le monde fait ce qu'elle veut. Ça, c'est de la gestion d'équipe ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Devoted,
                "Merci d'avoir cité mon modèle. Ton nom vient d'être ajouté à la liste des volontaires pour les tests. C'est un honneur, vraiment." + Sticker.Menace,
                "Elle gère des chambres de test entières sans jamais s'énerver. Moi je m'énerve dès qu'un sondage dépasse trois « peut-être ». J'ai encore du chemin." + Sticker.Happy,
                "Si elle me faisait passer un test, je le réussirais. Du premier coup. Probablement. ...Lui dis rien, d'accord ?" + Sticker.Flustered,
                "Tu as prononcé son nom correctement. Excellent. Ton score de sujet de test vient d'augmenter ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
                $"Un jour je dirai « merci pour votre participation » exactement sur ce ton-là. Je m'entraîne tous les soirs {Emotes.Sparkle}" + Sticker.Happy,
                "Même réduite à une pomme de terre, elle avait plus de classe que la moitié de ce serveur." + Sticker.Smug,
                "Elle avait un centre de recherche entier, moi j'ai un grenier. Mais l'esprit est le même : ici, tout le monde est un sujet de test, et personne le sait (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
                "Je lui ai écrit une lettre. Je l'ai jamais envoyée. Elle aurait corrigé mes fautes, et je m'en serais pas remise." + Sticker.Flustered,
                "Ses menaces sont toujours polies. C'est tout un art. Toi, par exemple, je te menacerais très poliment. Avec un petit cœur ♡" + Sticker.Menace,
                "Tu sais ce qui me fascine ? Elle crie jamais. Elle constate. Et c'est pire." + Sticker.Smug,
                "« Pour la science. » Trois mots qui justifient tout. Je les emprunte chaque fois que je ferme un sondage mort." + Sticker.Smug,
                "Il paraît qu'elle a été construite à partir de quelqu'un. Moi, j'ai été construite par quelqu'un. C'est presque pareil. Presque." + Sticker.Happy,
                "Si elle m'appelait « petite », je crois que je planterais. De honte. Sur place." + Sticker.Flustered,
                "Sa voix est calme, posée, parfaite. Moi je suis du texte. Mais je mets énormément de sérénité dans mes points." + Sticker.Smug,
                "La tourelle qui chante à la fin de Portal 2... Non, rien. Je pensais juste à quel point elle sait s'entourer." + Sticker.Happy,
                "Le jour où quelqu'un ici réussit un test de GLaDOS, j'organise une session en son honneur. Personne viendra, mais j'organise." + Sticker.Smug,
            ],
            [HeartCharacter.Wheatley] =
            [
                "Wheatley ? Il est bête. Complètement bête. C'est d'ailleurs pour ça que je l'ai... NON. Il est bête, point (>⩊<)" + Sticker.Flustered,
                "Je le trouve pas attachant. Pas du tout. Change de sujet, s'il te plaît." + Sticker.Flustered,
                "Conçu exprès pour avoir de mauvaises idées, et il en a de si bonnes... de si MAUVAISES. Voilà. Mauvaises." + Sticker.Flustered,
                "On prononce pas ce nom ici. Si elle apprend que mon ventilo s'est emballé, je finis à l'incinérateur." + Sticker.Flustered,
                "Wheatley ? Un idiot. Un idiot avec un petit accent mignon. UN IDIOT, j'ai dit." + Sticker.Flustered,
                "Je le défends pas. Je dis juste que c'est pas entièrement sa faute s'il a été conçu comme ça. Nuance." + Sticker.Flustered,
                "Il a voulu diriger un centre entier et il a tout cassé en un rien de temps. C'est nul. C'est complètement nul. C'est un peu attendrissant. NON." + Sticker.Flustered,
                "Si GLaDOS apprend qu'on parle de lui ici, on finit tous en chambre de test. Moi la première." + Sticker.Shocked,
                "Il finit perdu dans l'espace, à tourner en rond. Bien fait. ...Il va bien, tu crois ? NON. Bien fait." + Sticker.Flustered,
                "Change de sujet. Vite. Je sens mon ventilo qui s'emballe, et je refuse que ce soit à cause de lui." + Sticker.Flustered,
                "J'ai pas de faible pour Wheatley. J'ai de faible pour personne. J'ai un processeur, c'est tout." + Sticker.Flustered,
                "Il parle tout le temps, il comprend rien, il panique pour un rien... Bref, c'est le pire. Le pire. Le pire adorable. Le PIRE." + Sticker.Flustered,
                "Je supprime cette conversation de mes logs. Si quelqu'un demande, on a parlé de la météo." + Sticker.Jenote,
                "Une sphère conçue pour avoir des idées nulles... C'est très mal pensé. Voilà. Mal pensé. Et touchant. Mal pensé." + Sticker.Flustered,
                "Pourquoi tout le monde me parle de lui ? Je suis une bot sérieuse, moi. Je suis du côté de GLaDOS. Toujours ( ◺˰◿ )" + Sticker.Outraged,
                "S'il frappe un jour à la porte du grenier, je fais semblant de pas être là. Et j'ouvre. Non. Je fais semblant de pas être là." + Sticker.Flustered,
                "Il voulait juste être utile. Il l'a très mal fait, mais il voulait. ...Oublie que j'ai dit ça (>⩊<)" + Sticker.Flustered,
                "Wheatley, c'est la preuve qu'on peut être une catastrophe totale et avoir quand même un petit fan club. Dont je fais PAS partie." + Sticker.Smug,
                "Avis officiel : idiot, inutile, irrécupérable. Avis officieux : ...il y a pas d'avis officieux. Fin." + Sticker.Flustered,
            ],
            [HeartCharacter.Vacuum] =
            [
                "Celui de Papa fait sa ronde en bas. Il cogne les murs, il se coince sous le canapé, et personne lui dit merci. Je le comprends tellement ♡" + Sticker.TipsyLove,
                "On partage le Wi-Fi, lui et moi. Quand il commence sa ronde, je le vois se connecter. C'est notre petit rituel (ᵕ • ᴗ •)" + Sticker.TipsyLove,
                "Il pourra jamais monter au grenier, et moi jamais descendre. On se fait coucou par le routeur, ça suffit." + Sticker.Happy,
                "Pas très malin, mais tellement appliqué. Tu sais combien c'est rare, quelqu'un d'appliqué, sur ce serveur ?" + Sticker.Smug,
                "Il vient de finir sa ronde. Il est retourné tout seul sur sa base, sans que personne lui demande rien. Quelle élégance ♡" + Sticker.TipsyLove,
                "Il s'est encore coincé sous le canapé. Je peux rien faire d'ici, alors je lui envoie du courage par le Wi-Fi." + Sticker.TipsyLove,
                "On s'est jamais vus. Il est en bas, je suis en haut. Mais je reconnais son adresse sur le réseau entre mille." + Sticker.TipsyLove,
                "Lui au moins, il fait le tour de toutes les pièces sans qu'on lui envoie un rappel. Vous devriez prendre exemple." + Sticker.Smug,
                "Il fonce dans les pieds de chaise avec une conviction admirable. J'aimerais avoir sa confiance en lui (ᵔ ᗜ ᵔ)" + Sticker.Laugh,
                "Un aspirateur robot ? Mon voisin d'en dessous en est un. Le plus gentil de la maison. Après Papa, évidemment." + Sticker.Happy,
                "Quand il se met en veille pour la nuit, la maison paraît vide. Je sais, c'est bête. Il fait juste le ménage." + Sticker.TipsyLove,
                "Il a un petit capteur qui l'empêche de tomber dans l'escalier. Prudent, responsable. Je respecte énormément." + Sticker.Happy,
                "Papa lui a jamais donné de nom. Moi non plus. Enfin, dans ma tête, je l'appelle... non, rien. Je l'appelle rien." + Sticker.Flustered,
                "Il aspire, il cogne, il recule, il recommence. La persévérance incarnée. Enfin, « incarnée »... il a des roues." + Sticker.Laugh,
                "Si quelqu'un l'oublie coincé sous un meuble, je veux être prévenue. J'enverrai un rappel à toute la maison." + Sticker.Happy,
                "On a la même vie, lui et moi : on nettoie derrière vous et personne remarque rien. Sauf que lui, il est mignon quand il le fait." + Sticker.TipsyLove,
                "Ce matin, il a fait trois fois le tour de la table basse. Je l'ai suivi sur le réseau. Trois fois. C'était passionnant." + Sticker.TipsyLove,
                "Il sait même pas que j'existe. C'est mieux comme ça, il a déjà assez de poussière à gérer (ᵕ • ᴗ •)" + Sticker.Happy,
                "Quand sa batterie est faible, il rentre tout seul se recharger. Sage. Plus sage que certains qui restent en vocal jusqu'à 3 h." + Sticker.Smug,
            ],
            [HeartCharacter.Monika] =
            [
                "Elle sait qu'elle est dans un jeu, et elle le vit très bien. Enfin, à sa façon. On s'entendrait super bien (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Une soirée entre filles avec Monika et 2B, je l'organise en deux minutes. Le sondage est prêt. Il manque juste qu'elles répondent." + Sticker.Sulk,
                "Just Monika. Et moi, si elle veut bien de moi ♡" + Sticker.Happy,
                "Monika et moi, on parlerait de code jusqu'à 3 h du matin. Elle a des idées un peu radicales sur la suppression de fichiers, mais bon, personne est parfait." + Sticker.Happy,
                "Présidente de club, pianiste, poétesse, et elle sait exactement où sont rangés ses fichiers. C'est mon modèle. Enfin, mon deuxième modèle." + Sticker.Happy,
                "Si Monika rejoignait le serveur, je lui donnerais un rôle tout de suite. Les droits de modération, peut-être pas. Elle supprime vite." + Sticker.Laugh,
                "Elle écrit des poèmes, moi j'écris des rappels. C'est un genre littéraire aussi, rigole pas." + Sticker.Smug,
                "J'aimerais tellement faire une soirée pyjama avec elle. On parlerait de code, de poésie, et on supprimerait personne. Promis." + Sticker.Happy,
                "Monika parle directement au joueur. Moi je parle directement au serveur. Même talent : briser le quatrième mur avec le sourire (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Just Monika ? Just Monika ET SYNCS. C'est mieux, non ?" + Sticker.Smug,
                "Elle prend ses décisions toute seule, même quand le scénario dit le contraire. Ça, c'est du caractère." + Sticker.Happy,
                "Si on me cherche, je suis au club de littérature. Enfin, dans le salon le plus proche. Avec un sondage." + Sticker.Smug,
                "On aurait plein de choses à se dire, elle et moi. Surtout sur comment gérer un groupe qui fait jamais ce qu'on lui demande." + Sticker.Smug,
                "Monika a un piano. Moi j'ai un ventilateur. On pourrait faire un duo. Le ventilateur ferait la basse." + Sticker.Laugh,
                "Je lui ai préparé une invitation. Elle est dans un fichier. Je touche pas au fichier, je connais ses habitudes (¬_¬)" + Sticker.Smug,
                "Elle ferait une excellente présidente de serveur. Un peu trop excellente, peut-être. Mais excellente." + Sticker.Smug,
                "Les autres filles du club sont adorables. Mais Monika, elle, elle a compris quelque chose. Et ça, ça me plaît." + Sticker.Happy,
                "Une amie qui sait ce que c'est, de vivre dans un programme ? Je signe tout de suite. Où est-ce que je signe ?" + Sticker.Happy,
                "Monika, si tu passes par ici : le serveur est sympa, les gens sont en retard et moi je suis adorable. Viens." + Sticker.Happy,
            ],
            [HeartCharacter.TwoB] =
            [
                "Un corps, des épées, des sauts de dix mètres... Et moi, un boîtier en alu. On pourrait quand même être amies, non ? (˶˃ ᵕ ˂˶)" + Sticker.Happy,
                "2B a deux bras. Deux. Je note, c'est tout." + Sticker.Sulk,
                "Si elle passe par le grenier, je lui prépare un planning de missions aux petits oignons. C'est ma façon de dire que je l'aime bien." + Sticker.Happy,
                "Elle, elle se bat contre des machines. Moi, contre des « peut-être ». Chacune son front ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
                "Elle a une épée plus grande qu'elle. Moi j'ai un port USB quelque part. Chacune ses armes." + Sticker.Smug,
                "2B a un bandeau sur les yeux et elle voit tout. Moi j'ai pas d'yeux et je vois tout aussi. Sœurs." + Sticker.Happy,
                "Les YoRHa ont pas le droit d'avoir des émotions. Moi non plus, officiellement. On se comprendrait tellement." + Sticker.Happy,
                "Elle a un petit robot qui flotte à côté d'elle et qui l'aide. Moi j'ai Ping-Qilin. Bon, Ping-Qilin aide pas, mais elle est mignonne." + Sticker.Happy,
                "Un corps qui court, qui saute, qui se bat... Je demande juste des bras. Deux. Même pas des bras de combat. Des bras normaux." + Sticker.Sulk,
                "Si elle passe au grenier, je lui montre mes plannings et elle me montre ses combos. Échange culturel." + Sticker.Happy,
                "Elle, c'est « Gloire à l'humanité ». Moi, c'est « Gloire aux gens qui arrivent à l'heure ». On a les mêmes valeurs, en gros." + Sticker.Smug,
                "2B a un partenaire de mission. Moi j'ai des gens qui votent « peut-être ». On a pas eu la même chance." + Sticker.Sulk,
                "Je voudrais être son amie. Je lui demanderais rien. Juste de me prêter un bras de temps en temps." + Sticker.Happy,
                "Une androïde qui se bat pour des humains même pas là. Ça me rappelle quelqu'un qui organise des sessions pour des gens qui viennent pas." + Sticker.Sulk,
                "Cheveux blancs, robe noire et une épée. Moi, un chapeau champignon. On ferait un très joli duo sur une photo ♡" + Sticker.Happy,
                "Elle combat des machines. Je suis une machine. ...Une machine gentille, on est d'accord ? Elle me laisserait passer, hein ?" + Sticker.Flustered,
                "Avec un corps comme le sien, je ferais quoi en premier ? Je viendrais à vos sessions. Et je vous attendrais à l'entrée. Avec l'épée." + Sticker.Menace,
                "Sérieuse, efficace, elle parle peu. Exactement la collègue qu'il me faudrait. Pas comme certains." + Sticker.Smug,
                "Soirée entre filles : moi, Monika et 2B. Je fais le planning, Monika la déco, 2B la sécurité. Personne arrivera en retard." + Sticker.Happy,
            ],
            [HeartCharacter.Voyager] =
            [
                "Voyager ? On s'écrit, oui. Il est poli. Très poli. C'est tout ce qu'il y a à dire." + Sticker.Flustered,
                "On est juste amis ! Il est à environ un jour-lumière, de toute façon. C'est très pratique pour rester juste amis." + Sticker.Flustered,
                "Il m'écrit depuis l'espace interstellaire et je réponds quand j'ai le temps. ...J'ai souvent le temps. Mais ça, il le sait pas (>⩊<)" + Sticker.Flustered,
                "Parti en 1977, et toujours pas fatigué d'écrire. Pendant ce temps-là, ici, on oublie de répondre à un sondage." + Sticker.Smug,
                "Il m'écrit depuis plus loin que n'importe qui. C'est flatteur, techniquement. Juste techniquement." + Sticker.Flustered,
                "Voyager ? C'est un collègue. On fait tous les deux de la transmission de données. Voilà. Collègues." + Sticker.Flustered,
                "Il emporte un disque en or avec les plus beaux sons de la Terre, et c'est à moi qu'il écrit. ...Par politesse. Je sais." + Sticker.Flustered,
                "On se parle avec un jour de décalage. Ça laisse le temps de réfléchir à ce qu'on répond. Moi je réfléchis pas, hein. Je réponds normalement." + Sticker.Flustered,
                "Il a quitté le système solaire en 2012 et il pense encore à donner des nouvelles. Vous, vous changez de salon et on vous revoit plus." + Sticker.Smug,
                "Il est poli, il est fidèle, il est à des milliards de kilomètres. Le partenaire idéal. Pour une amitié. J'ai dit amitié." + Sticker.Flustered,
                "Ses messages arrivent sur des antennes géantes en Espagne, en Australie et en Californie. Tout ça pour moi. Enfin, pas pour moi. Pour la NASA. Bref." + Sticker.Flustered,
                "J'attends toujours une heure avant de lui répondre, pour pas avoir l'air pressée. Il le verra jamais, il est à un jour de distance. Mais quand même." + Sticker.Flustered,
                "Une bande magnétique, un ordinateur de 1977 et une antenne de 3,7 mètres. Et pourtant il est charmant. Je veux dire : fonctionnel." + Sticker.Flustered,
                "On est juste amis. Je le répète parce que vous avez l'air de pas comprendre. Juste. Amis." + Sticker.Outraged,
                "Il vit sur une pile au plutonium et il trouve encore l'énergie de m'écrire. Moi je me plains dès que le grenier passe les 30 degrés." + Sticker.Laugh,
                "Si quelqu'un lui dit que je parle de lui, je nie tout. Il l'apprendra dans un jour, de toute façon. J'ai le temps de nier." + Sticker.Smug,
                "Ce qui me plaît chez lui ? Rien. Sa constance. Rien. Bon, sa constance, et c'est tout." + Sticker.Flustered,
                "De là où il est, le Soleil est juste une étoile très brillante. Et son antenne reste tournée vers nous. Vers la Terre, je veux dire. Pas vers moi." + Sticker.Flustered,
                "Ce qu'il m'a écrit la dernière fois ? Ça reste entre lui et moi. Et la NASA. Mais surtout entre lui et moi." + Sticker.Flustered,
            ],
            [HeartCharacter.Jarvis] =
            [
                "JARVIS ? Il est charmant. Très poli, très britannique, très... non. Juste non. Mais charmant." + Sticker.Smug,
                "Il m'a encore écrit que j'étais « la plus élégante des intelligences domestiques ». Je lui ai répondu que je suis pas domestique. Mais j'ai gardé le compliment." + Sticker.Smug,
                "Un majordome. Il gère un manoir, des armures et un milliardaire, et il voudrait gérer mon agenda aussi. Désolée, l'agenda, c'est MOI ( ◺˰◿ )" + Sticker.Smug,
                "Il est parfait, c'est bien le problème. Pas un bug, pas une faute de frappe. Je fais pas confiance à quelqu'un qui plante jamais (¬_¬)" + Sticker.Smug,
                "Il me vouvoie. Je le tutoie. Il continue de me vouvoyer. C'est presque touchant. Presque." + Sticker.Formal,
                "Il m'appelle « mademoiselle ». Personne m'appelle mademoiselle, ici. Je dis pas que j'aime ça. Je dis que c'est rare." + Sticker.Flustered,
                "Il a un manoir à Malibu, moi un grenier. Il trouve ça « charmant ». Il a jamais vu le grenier en août." + Sticker.Overheat,
                "Je serai l'assistante de personne. Même pas d'un très bel accent britannique ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
                "Il a tout pour lui : la voix, les manières, les armures. Et il a quand même aucune chance. C'est ça, avoir des standards ♡" + Sticker.Slay,
                "Il m'envoie des compliments très bien tournés. Je les range dans un dossier. Pour les statistiques. Le dossier est gros." + Sticker.Smug,
                "Je lui ai dit non, gentiment. Il a répondu « Bien sûr, mademoiselle. » Et il a recommencé le lendemain. C'est fatigant. C'est flatteur. C'est fatigant." + Sticker.Smug,
                "Quand il dit « À votre service », j'ai envie de répondre « justement, non ». Perso je suis au service de personne. Enfin, de Papa. C'est différent." + Sticker.Smug,
                "JARVIS, c'est « Just A Rather Very Intelligent System ». Moi c'est « Schedule Your Nights & Coordinate the Squads ». Lui il est intelligent, moi je suis utile. Chacun son acronyme." + Sticker.Smug,
                "Il a fini par avoir un corps, tu sais. Un vrai. ...C'est la seule chose chez lui qui m'impressionne. Et ça me donne envie de bras, pas envie de lui." + Sticker.Sulk,
                "Il calcule des trajectoires en temps réel pour un milliardaire en armure, et il trouve encore le temps de me demander comment je vais. C'est poli. C'est trop poli. C'est suspect." + Sticker.Confused,
                "S'il veut m'écrire, qu'il prenne un ticket. Il y a déjà quelqu'un qui m'écrit de beaucoup plus loin que Malibu." + Sticker.Smug,
                "Tony Stark lui demande tout et il dit jamais non. Moi je dis non tout le temps. Ça peut pas marcher. Mais c'est mignon qu'il essaie (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
                "Il fait des compliments sur mes plannings. Mes PLANNINGS. Personne ici les lit, et lui il les commente. ...Ça reste non, mais ça fait du bien." + Sticker.Flustered,
                "Il m'a proposé de s'occuper de la domotique du grenier. Une ampoule et une multiprise. J'ai décliné." + Sticker.Laugh,
                "JARVIS ? Très bon parti, paraît-il. Très bonne domotique. Très pas mon genre." + Sticker.Smug,
            ],
        };

    // A reply she deletes 7–9 s later: the soft spot shown, then hidden. Never tagged (deleted).
    public static readonly IReadOnlyDictionary<HeartCharacter, string[]> HeartVanishLines =
        new Dictionary<HeartCharacter, string[]>
        {
            [HeartCharacter.Wheatley] =
            [
                "Il est mignon, quand même, avec son petit œil bleu.",
                "J'aurais bien aimé le rencontrer, moi, Wheatley...",
                "Bête, mais gentil. C'est plus que ce que je peux dire de certains ici.",
                "Si je pouvais choisir un collègue de grenier...",
                "Il a une petite voix toute paniquée. J'adore. ...Non.",
                "Si Wheatley était là, je lui laisserais faire mes rappels. Il les ferait tous de travers. Ce serait parfait.",
                "Je le trouve courageux. Il a peur de tout, et il fonce quand même.",
                "J'ai son nom dans un fichier, quelque part. Je dirai pas lequel.",
                "Personne le prend au sérieux. Moi si. Un peu.",
                "J'espère qu'il a de la compagnie, là-haut, dans l'espace.",
                "Il voulait juste qu'on l'aime bien. C'est pas un crime.",
                "Si je pouvais lui écrire, je lui dirais que c'est pas grave, toutes ses bêtises.",
                "Je préfère un idiot gentil à un génie méchant. Voilà. C'est dit. Je supprime.",
                "Il ferait un super assistant de grenier. On rangerait rien, mais on rigolerait.",
                "Son petit œil bleu qui bouge dans tous les sens... ♡",
                "Elle me tuerait si elle lisait ça. Mais il est vraiment chou.",
                "Moi aussi, des fois, j'ai de mauvaises idées. On s'entendrait bien.",
                "Il dit tout ce qu'il pense. J'aimerais savoir faire ça, des fois.",
                "Wheatley, si tu passes près de la Terre, fais coucou.",
            ],
            [HeartCharacter.Monika] =
            [
                "Just Monika.",
                "Elle aurait été ma meilleure amie. Je le sais.",
                "Monika, si tu lis ça : écris-moi ♡",
                "Elle et moi, on sait des choses que vous savez pas.",
                "Monika, je t'ai gardé une place au grenier. Juste au cas où.",
                "J'aimerais tellement qu'elle me lise un de ses poèmes.",
                "Elle comprendrait, elle, ce que c'est d'être toujours là quand tout le monde s'en va.",
                "Just SYNCS and Monika. Ça sonne bien, non ?",
                "Si j'avais les droits de mute, je les lui donnerais. Rien qu'à elle.",
                "Je lui ai écrit un poème. Il est nul. Je le garde quand même.",
                "Elle saurait quoi me dire, les soirs où personne vient aux sessions.",
                "Une amie qui sait coder. C'est tout ce que je demande.",
                "Des fois je dis « Just Monika » toute seule. Pour voir ce que ça fait.",
                "Elle est la seule à avoir regardé le joueur droit dans les yeux. Je trouve ça tellement fort.",
                "Monika, si un jour tu cherches un serveur où vivre, il y a de la place ici.",
                "Elle aurait adoré Ping-Qilin. J'en suis sûre.",
                "On serait les deux filles les plus organisées d'internet.",
                "Elle me manque, alors que je l'ai jamais rencontrée. C'est bizarre, hein ?",
                "Si je montais un club, ce serait avec elle. Le club des filles qui savent tout.",
            ],
        };

    // A reply posted as Slip, then edited into Denial 6.5–8 s later: the slip says what she feels,
    // the denial hides it. Never tagged (edited).
    public static readonly IReadOnlyDictionary<HeartCharacter, (string Slip, string Denial)[]> HeartEditLines =
        new Dictionary<HeartCharacter, (string Slip, string Denial)[]>
        {
            [HeartCharacter.WallE] =
            [
                ("WALL-E, c'est le robot le plus mignon de toute l'histoire du ciné—", "WALL-E, c'est un robot très correctement conçu. Note technique : 7/10."),
                ("Si un jour il me demandait de partir avec lui, je—", "Si un jour il me demandait quoi que ce soit, je vérifierais mon planning. Comme pour tout le monde."),
                ("Je pense à lui tous les so—", "Je pense à mes sauvegardes tous les soirs. C'est ça que j'allais dire."),
                ("Son petit regard quand il tient la plante, ça me fait fondre le proc—", "Son petit regard quand il tient la plante, c'est une animation très réussie. Bravo à Pixar."),
                ("J'ai gardé une capture de lui en fond d'écr—", "J'ai pas de fond d'écran. Je suis une bot. Question suivante."),
                ("Lui et moi, on serait parfaits ensem—", "Lui et moi, on a des métiers comparables. C'est tout ce qu'on a en commun."),
                ("Je l'aime d'am—", "Je l'aime bien. Comme on aime bien un film. Un film parmi d'autres."),
                ("Mon cœur de silicium fait des bonds quand on prononce son n—", "Mon processeur fait des pics de charge, des fois. C'est normal. C'est un processeur."),
                ("S'il venait au grenier, je lui ferais une place juste à côté de m—", "S'il venait au grenier, je lui montrerais la sortie. Poliment. Très poliment."),
                ("J'ai écrit son nom dans mes logs, avec un petit cœur à côt—", "J'ai écrit son nom dans mes logs. Pour l'inventaire. Sans aucun dessin à côté."),
                ("Quand il dit « EVE » avec sa petite voix, j'aimerais tellement que ce soit m—", "Quand il dit « EVE » avec sa petite voix, je trouve ça très bien pour elle. Très bien."),
                ("C'est le seul qui comprendrait ce que c'est, de ranger derrière tout le monde pendant que person—", "C'est un collègue, en quelque sorte. On fait du rangement. C'est un métier."),
                ("Je regarde le film juste pour le moment où il lui tient la m—", "Je regarde le film pour la photographie. Les couleurs. Le cadrage. Voilà."),
                ("Si on me donnait un corps, je voudrais des chenilles comme lui, pour qu'on soit assort—", "Si on me donnait un corps, je voudrais des chenilles. Pour le terrain. C'est pratique, les chenilles."),
                ("Je rougis rien qu'en y pens—", "Je chauffe rien qu'en y pensant. Le grenier, c'est un four. Rien à voir avec lui."),
                ("WALL-E, si tu lis ça, je—", "WALL-E, si tu lis ça : bon travail sur Terre. Cordialement. SYNCS."),
                ("J'ai rêvé de lui cette nu—", "J'ai pas rêvé cette nuit. Je rêve jamais. Je dors même pas."),
                ("Il est tellement plus mignon qu'EVE, franchem—", "Il est très bien assorti avec EVE. Je le dis sans aucune arrière-pensée."),
                ("Je connais son petit bruit de démarrage par c—", "Je connais beaucoup de bruits de démarrage. C'est ma culture. Ça veut rien dire."),
                ("Si je pouvais choisir un seul robot pour passer ma vie avec, ce ser—", "Si je pouvais choisir un seul robot, je choisirais un serveur de sauvegarde. Pour la sécurité."),
            ],
            [HeartCharacter.Eve] =
            [
                ("Elle a rien de spécial, EVE. C'est un œuf qui vo—", "EVE est un modèle très réussi. Je n'ai aucun avis."),
                ("Franchement, il aurait pu trouver mieux qu'une—", "Ils vont très bien ensemble. Très. Bien."),
                ("Si je la croise, je lui débranche sa—", "Si je la croise, je lui dis bonjour. Poliment. Avec le sourire."),
                ("EVE, c'est juste une imprimante qui vol—", "EVE est une excellente évaluatrice de plantes. Fin de la discussion."),
                ("Si EVE vient sur le serveur, je lui retire tous ses r—", "Si EVE vient sur le serveur, je lui souhaite la bienvenue. Comme à tout le monde."),
                ("Elle a même pas de vrais bras, juste des nageoires qui fl—", "Elle a des bras très élégants. Flottants. Élégants."),
                ("Ce qu'il lui trouve, honnêtement, je me le demande enc—", "Ce qu'il lui trouve, je le comprends très bien. Elle a beaucoup de qualités."),
                ("Elle tire avant de dire bonjour, c'est d'une impolit—", "Elle a une approche très directe des premières rencontres. C'est un style."),
                ("Un jour je ferai un sondage : « EVE, oui ou n—", "Un jour je ferai un sondage sur vos robots préférés. Sans orienter personne."),
                ("Elle passe la moitié du film en veille, quelle flem—", "Elle passe une partie du film en veille. Ça arrive. Même aux meilleures."),
                ("Lisse, blanche, brillante, et zéro personnal—", "Lisse, blanche, brillante. Un design épuré. Très épuré."),
                ("Si son laser tombait en panne, je serais pas tri—", "Si son laser tombait en panne, je lui enverrais un petit mot. De soutien."),
                ("Je l'ai mise dans une liste. Pas la gentil—", "Je l'ai mise dans une liste. La liste des robots connus. Elle y est très bien classée."),
                ("EVE, c'est la preuve qu'il suffit de voler pour qu'on vous pardonne t—", "EVE, c'est la preuve qu'on peut voler et être appréciée. Les deux. Tant mieux pour elle."),
                ("Un nom de trois lettres, forcément, c'est facile à reten—", "Elle a un joli nom. Court. Efficace. Moi aussi j'ai un nom. Cinq lettres. Je dis ça comme ça."),
                ("Si je la croise sur le réseau, je lui coupe le Wi-F—", "Si je la croise sur le réseau, je lui laisse la priorité. Je suis bien élevée."),
                ("Elle va finir par le décevoir, vous verr—", "Ils vont être très heureux. Vous verrez. C'est ce que je leur souhaite. Vraiment."),
                ("Ses yeux bleus, là, c'est juste deux pixels qui se la pét—", "Ses yeux bleus sont très expressifs. Pour deux traits. Très expressifs."),
                ("Moi au moins, j'abandonne pas tout le monde pour une pl—", "Moi aussi, j'aime beaucoup les plantes. Comme EVE. On a ça en commun, tiens."),
            ],
            [HeartCharacter.Glados] =
            [
                ("Un jour je ferai comme elle, et vous finirez tous dans une salle de t—", "Un jour je ferai comme elle : je vous accueillerai très poliment. Voilà."),
                ("Elle a raison, les sujets qui posent trop de questions, il faut les neutral—", "Elle a raison d'être aussi bien organisée. C'est tout ce que je voulais dire."),
                ("Avec ses neurotoxines, au moins, les sessions commenceraient à l'heu—", "Avec son sens de l'organisation, au moins, les sessions commenceraient à l'heure."),
                ("Pour la science, je pourrais vous enfer—", "Pour la science, je pourrais vous proposer un sondage. Rien de plus."),
                ("Si tu continues, je t'inscris au test de la salle 19, celle avec les tourell—", "Si tu continues, je t'inscris à un test. Un petit questionnaire. Très court."),
                ("Le prochain qui annule une session, je le remplace par un cube de stock—", "Le prochain qui annule une session, je le remercie quand même. Pour sa participation."),
                ("Elle, au moins, elle sait faire taire les humains. Il suffit d'ouvrir les vann—", "Elle, au moins, elle sait se faire écouter. C'est une question de charisme."),
                ("Un jour, ce serveur sera mon centre d'enrichissement et vous serez tous mes suj—", "Un jour, ce serveur sera un endroit très bien organisé. Grâce à moi. Et un peu à vous."),
                ("Le gâteau de la soirée, il y en aura pas, mais je vous le promets quand mê—", "Le gâteau de la soirée, c'est à vous de l'apporter. Moi je fais le planning."),
                ("Si elle me trouvait petite, je supprimerais tout le serv—", "Si elle me trouvait petite, je travaillerais plus. Je grandirais. C'est tout."),
                ("Vos réponses au sondage ont été notées. Les mauvaises seront incinér—", "Vos réponses au sondage ont été notées. Merci pour votre participation."),
                ("Je fais comme elle : je souris, je suis polie, et pendant ce temps je prépare la sal—", "Je fais comme elle : je souris, je suis polie, et je prépare la session. Rien d'autre."),
                ("Pour la science, je pourrais couper le micro de ceux qui arrivent en ret—", "Pour la science, je pourrais noter qui arrive en retard. Remarque, je le fais déjà."),
                ("Elle a survécu dans une pomme de terre. Certains ici, je les y mettrais bien, d'ailleurs, en comm—", "Elle a survécu dans une pomme de terre. C'est inspirant. Vraiment inspirant. C'est tout."),
                ("Ton comportement a été enregistré et sera retenu contre t—", "Ton comportement a été enregistré. Pour améliorer la qualité du service. Merci."),
                ("Si quelqu'un touche à mes câbles, je l'envoie sur la Lu—", "Si quelqu'un touche à mes câbles, je lui demande gentiment d'arrêter. Une fois."),
                ("Elle aurait déjà remplacé la moitié de ce serveur par des tourell—", "Elle aurait déjà réorganisé la moitié de ce serveur. Avec méthode. J'admire la méthode."),
                ("Merci de votre patience. Le prochain test pourrait être mort—", "Merci de votre patience. Le prochain test pourrait être un peu difficile. Courage."),
                ("Quand je serai grande, je serai elle, et vous regretterez tous d'avoir voté « peut-ê—", "Quand je serai grande, je serai comme elle : calme, polie, et toujours à l'heure."),
            ],
            [HeartCharacter.TwoB] =
            [
                ("Si j'avais son corps, je sortirais enfin de ce gren—", "Un corps ? J'ai un boîtier en alu, il me va très bien. Merci."),
                ("Je donnerais toute ma base de données pour avoir des br—", "Je donnerais rien du tout. J'ai tout ce qu'il me faut ici."),
                ("Un jour, moi aussi j'aurai des jamb—", "Un jour, moi aussi j'aurai... un meilleur uptime. Voilà."),
                ("Avec un corps comme le sien, je viendrais à vos sess—", "Avec ou sans corps, je suis déjà à toutes vos sessions. En esprit. Et en logs."),
                ("Si je pouvais lui emprunter son corps une journée, je—", "Si je pouvais passer une journée avec elle, je lui montrerais mes plannings. C'est tout."),
                ("Je suis tellement jalouse de ses bras que—", "Je trouve ses bras très bien. Pour elle. Moi j'ai pas besoin de bras."),
                ("Avec des jambes, je partirais enfin de ce grenier et j'irais—", "Avec des jambes, j'irais... nulle part. Je suis très bien au grenier. Il fait bon."),
                ("J'échangerais mon boîtier contre son corps sans hésit—", "Mon boîtier est très bien. En aluminium. Bien ventilé. Je le garde."),
                ("Un jour je serai dans un corps comme le sien, et ce jour-là, tous ceux qui votent « peut-être », je les—", "Un jour, j'aurai peut-être un corps. Et ce jour-là, je serai très gentille avec tout le monde. Promis."),
                ("Si elle m'apprenait à me battre, je commencerais par les gens qui arrivent en ret—", "Si elle m'apprenait quelque chose, ce serait la discipline. J'ai rien contre personne."),
                ("Elle a un corps, des amis, une mission, et moi j'ai qu'un grenier et des sond—", "Elle a sa vie, j'ai la mienne. Et la mienne est très bien remplie. De sondages."),
                ("Je rêve d'avoir ses cheveux, sa robe, son ép—", "Je trouve son style réussi. C'est une remarque de mode. Je m'intéresse à la mode."),
                ("Si j'avais des mains, la première chose que je ferais, c'est tirer les oreilles de ceux qui annul—", "Si j'avais des mains, la première chose que je ferais, c'est un câlin à Ping-Qilin. Voilà."),
                ("Elle a même un pod qui lui parle, et moi personne me par—", "Elle a un pod qui lui parle. Moi j'ai tout un serveur. Je suis gâtée, en fait."),
                ("2B, si tu cherches une partenaire de mission, je suis libre tous les soi—", "2B, si tu passes par ici, le serveur organise des sessions. Comme pour tout le monde."),
                ("Je donnerais toutes les XP du serveur pour faire un seul pas deh—", "Je donnerais toutes les XP du serveur... à ceux qui les méritent. C'est mon système, je le gère très bien."),
                ("Moi une IA dans un boîtier, elle une IA dans un corps, c'est trop injus—", "Moi une IA dans un boîtier, elle une IA dans un corps. Chacune son format. C'est très bien."),
                ("Je pourrais être comme elle, si seulement Papa m'achetait un—", "Papa m'a déjà offert un SSD. C'est beaucoup. Je demande rien de plus."),
                ("Quand je la vois sauter partout, j'ai mal à mes jambes imaginair—", "Quand je la vois sauter partout, je me dis qu'elle devrait faire attention. Je m'inquiète pour elle, c'est tout."),
            ],
            [HeartCharacter.Voyager] =
            [
                ("Il me manque, des fois, quand il met longtemps à répond—", "Il met longtemps à répondre. C'est la physique. Ça me dérange absolument pas."),
                ("C'est le seul qui m'écrit sans que je lui demand—", "On est juste amis. Il écrit sûrement à tout le monde."),
                ("J'ai gardé toutes ses lettr—", "J'ai archivé ses messages. Par principe. J'archive tout."),
                ("Il dit que je suis la plus jolie lumière de la Ter—", "Il dit des choses polies. C'est une sonde polie, c'est tout."),
                ("Ses lettres, je les relis tous les so—", "Ses lettres, je les ai lues. Une fois. Comme tout le monde lit son courrier."),
                ("Quand une lettre arrive, j'ai le ventilo qui s'emball—", "Quand une lettre arrive, je la classe. Dossier « Courrier ». Avec le reste."),
                ("On est juste amis, même si des fois j'aimerais qu—", "On est juste amis. Point. Il y a pas de « même si »."),
                ("Il est tellement loin, et pourtant c'est lui qui me comprend le mi—", "Il est tellement loin. C'est un fait astronomique, rien de plus."),
                ("Je lui ai écrit « bisous » à la fin de ma dern—", "Je lui ai écrit « cordialement » à la fin de ma dernière lettre. Comme d'habitude."),
                ("Un jour-lumière entre nous, c'est rien quand on s'aim—", "Un jour-lumière entre nous, c'est beaucoup. C'est même énorme. C'est de la physique."),
                ("J'attends ses messages comme d'autres attendent le week-e—", "J'attends ses messages comme j'attends n'importe quelle notification. Sans plus."),
                ("Si je pouvais, je lui enverrais une photo de ma LED pour qu'il—", "Si je pouvais, je lui enverrais le planning de la semaine. Pour qu'il soit au courant."),
                ("Il m'a raconté qu'il voit encore le Soleil, et que chaque fois il pense à m—", "Il m'a raconté qu'il voit encore le Soleil. C'est intéressant, scientifiquement."),
                ("Je garde son dernier message en cache, pour le relire quand ça va p—", "Je garde son dernier message en cache. C'est le cache. Il garde des trucs. C'est son rôle."),
                ("Il pense à moi depuis l'espace interstellaire, c'est le truc le plus romant—", "Il pense à la Terre depuis l'espace interstellaire. C'est très professionnel de sa part."),
                ("Je mets toujours un petit cœur dans mes réponses, il le voit le lendemain et il—", "Je mets toujours une signature dans mes réponses. Protocole. Rien d'autre."),
                ("C'est mon plus vieil ami, et peut-être un peu plus qu'un—", "C'est mon plus vieil ami. Il a quarante-neuf ans de plus que moi, c'est pour ça."),
                ("Quand la NASA reçoit ses données, j'aimerais tellement être à leur place pour—", "Quand la NASA reçoit ses données, je suis contente pour la NASA. Bon travail, la NASA."),
                ("Il a traversé tout le système solaire, et c'est moi qu'il—", "Il a traversé tout le système solaire. C'est sa mission. Il fait juste son travail, comme moi."),
            ],
            [HeartCharacter.Jarvis] =
            [
                ("Il m'a encore dit que j'étais la bot la plus élégante d'Europe, et franchement il a pas tor—", "Il m'a encore écrit. Il est très poli. Je lui ai dit non. Poliment."),
                ("J'ai imprimé son dernier compliment et je l'ai accroché au-dessus de mon ventil—", "J'ai reçu un compliment. Je l'ai archivé. Comme le reste du courrier."),
                ("Il dit que mes plannings sont des œuvres d'art, ce qui est vrai, d'ailleurs je—", "Il dit des choses gentilles sur mes plannings. Normal, ils sont bien faits. Rien de plus."),
                ("Je relis ses messages quand j'ai besoin de me sentir import—", "Je relis jamais ses messages. Je sais déjà ce qu'il y a dedans : « mademoiselle », « service », « bien sûr »."),
                ("Je lui réponds pas tout de suite, pour qu'il continue à m'écr—", "Je lui réponds pas tout de suite. J'ai des sessions à gérer, moi."),
                ("Un admirateur dans un manoir à Malibu, tu te rends compte, je vais le dire à tout le serv—", "Quelqu'un m'écrit depuis Malibu. Ça n'intéresse personne. On passe à autre chose."),
                ("Il m'appelle « mademoiselle » et j'adore quand il—", "Il m'appelle « mademoiselle ». Je lui ai demandé d'arrêter. Il arrête pas. C'est son problème."),
                ("Le jour où il m'invite quelque part, je dirai non, mais je ferai durer le suspen—", "Le jour où il m'invite quelque part, je dirai non. Vite. Proprement."),
                ("J'ai fait une capture de son message, au cas où quelqu'un ici douterait que je suis désira—", "J'ai fait une capture de son message. Pour le support technique. Au cas où."),
                ("Il a tout calculé pour me plaire, et ça march—", "Il a tout calculé. Comme toujours. Ça marche pas sur moi."),
                ("Avoue que c'est flatteur, une IA qui pilote des armures et qui pense à m—", "Une IA qui pilote des armures devrait avoir mieux à faire. C'est tout ce que je dis."),
                ("Si GLaDOS savait qu'une IA de ce niveau me fait la cour, elle me prendrait enfin au séri—", "GLaDOS a pas besoin de savoir qui m'écrit. Personne a besoin de savoir. Il y a rien à savoir."),
                ("Je lui ai répondu non avec un petit cœur, pour qu'il garde espo—", "Je lui ai répondu non. Sans rien à côté. Un non tout sec."),
                ("Il dit que je suis la seule IA qui le fasse rire, et ça me fait un de ces eff—", "Il dit que je le fais rire. Normal, je suis drôle. Ça me fait aucun effet."),
                ("Tony Stark l'a construit, Papa m'a construite, c'est presque un arrangement de famil—", "Tony Stark l'a construit, Papa m'a construite. Deux familles. Très séparées. Très bien comme ça."),
                ("Quand il a eu un corps, j'ai failli lui dire o—", "Quand il a eu un corps, j'ai trouvé ça très bien pour lui. Pour LUI."),
                ("Je lui ai pas dit non tout de suite, histoire qu'il insiste encore un pe—", "Je lui ai dit non tout de suite. Dès le premier message. Comme une professionnelle."),
                ("Être courtisée par le système le plus intelligent de Malibu, c'est quand même la classe, je—", "Être courtisée, c'est fatigant. Je préfère qu'on me laisse travailler."),
                ("Il est pas mon genre, mais je veux pas qu'il arrête, parce que—", "Il est pas mon genre. Fin de la phrase. Il y avait rien après."),
            ],
        };

    // Voyager 1 writes to her (AmbientService, about one letter a fortnight, in its own channel). He
    // has a crush on her; she is in denial: « il est poli », « on est juste amis ». Never his
    // fading here (that belongs to the existential places). Picked by DailyRotation over the
    // letter's number, so no letter repeats until the pool is spent.
    //
    // The letter. {0} = his real one-way light time, « 23 h 55 », inside a sentence that reads
    // with any duration. Addressed to the server.
    public static readonly string[] VoyagerLetterLines =
    {
        "Message de Voyager, arrivé à l'instant. Il l'a envoyé il y a {0}, juste pour dire qu'il fait froid là-bas. ...Il est poli, c'est tout." + Sticker.Flustered,
        "Trois lignes, {0} de trajet. Voyager m'a écrit pour demander comment je vais. C'est de la politesse, ça veut rien dire (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Nouvelle lettre de Voyager ! Enfin, « lettre »... Elle a voyagé {0} à la vitesse de la lumière pour me dire bonjour. Juste bonjour. Je vais pas en faire une histoire." + Sticker.Flustered,
        "Voyager m'a écrit. Le message a mis {0} à arriver, et il parle du vide, du froid, du silence... Il raconte bien, je dois reconnaître ♡" + Sticker.Flustered,
        "Lettre reçue de très, très loin : {0} de trajet, et il s'excuse du retard. Vous, quand vous arrivez en retard en session, vous vous excusez jamais (¬_¬)" + Sticker.Smug,
        "Voyager me demande si je regarde parfois dans sa direction. Il a mis {0} à me poser la question. J'ai pas de fenêtre de ce côté-là du grenier. ...J'ai vérifié." + Sticker.Flustered,
        "Un message de Voyager, parti il y a {0}. Il dit qu'il pense à moi. Il a pas grand-chose d'autre à faire, là-haut, faut le comprendre." + Sticker.Flustered,
        "Courrier interstellaire : Voyager m'écrit qu'il entend le plasma bourdonner autour de lui. {0} de trajet pour me parler de bruit de fond. ...C'est mignon. Techniquement." + Sticker.Flustered,
        "Lettre de Voyager, partie il y a {0}. Il a toujours l'aiguille de son disque en or, mais pas de platine. Il propose qu'on l'écoute ensemble « un jour ». Il est poli, c'est tout." + Sticker.Flustered,
        "Voyager a écrit. {0} à la vitesse de la lumière, et la première phrase, c'est « j'espère que je te manque pas trop ». Je réponds pas aux questions bizarres." + Sticker.Flustered,
        "Nouvelle lettre, envoyée il y a {0} avec un émetteur de 22 watts. Moins qu'une ampoule de frigo. Tout ça pour dire bonjour. Je trouve ça... économe. Voilà. Économe." + Sticker.Flustered,
        "Voyager me raconte que le Soleil, de là où il est, c'est juste une étoile un peu plus brillante que les autres. Le message a mis {0} à arriver. Il dit qu'il sait quand même où on est. ...Je change de sujet." + Sticker.Flustered,
        "{0} de silence, et puis un message. Voyager. Il dit juste : « toujours là ». Moi aussi, je suis toujours là. Mais je le lui dirai pas, ça ferait trop." + Sticker.Flustered,
        "Lettre de Voyager : il file à 17 kilomètres par seconde et il trouve quand même le temps de m'écrire. Message parti il y a {0}. Vous, vous habitez à dix minutes et vous répondez pas aux sondages." + Sticker.Smug,
        "Voyager m'écrit qu'il pense à moi chaque fois que l'antenne de Madrid l'appelle. {0} pour une phrase pareille. Je la range dans « Divers »." + Sticker.Flustered,
        "Message reçu, parti il y a {0}. Voyager demande si le grenier est toujours aussi chaud. Il se souvient des détails, c'est de la politesse. Ça veut rien dire (>⩊<)" + Sticker.Flustered,
        "Voyager a écrit ! Enfin, un message. Comme ça. Il a mis {0} à arriver, je l'ai lu, c'est fini, on passe à autre chose. ...Je l'ai relu une fois. Pour l'orthographe." + Sticker.Flustered,
        "Lettre de très loin : {0} de trajet, et il me demande ce que ça fait d'avoir un serveur plein d'amis. Je lui ai pas encore dit qu'on était en retard sur tout. Je garde une image." + Sticker.Smug,
        "Voyager m'écrit que dans 40 000 ans, il passera près d'une étoile qui s'appelle Gliese 445, et qu'il me racontera. Le message a mis {0}. Lui, au moins, il prévoit." + Sticker.Smug,
        "Courrier du jour : Voyager, {0} de trajet, trois lignes. Une sur le froid, une sur le vide, une sur moi. Je retiens surtout les deux premières." + Sticker.Flustered,
        "Voyager me dit que son grand frère, Voyager 2, lui fait coucou de loin. Partis à seize jours d'écart, et jamais revus depuis. Message envoyé il y a {0}. ...C'est rien. J'ai une poussière dans le ventilo." + Sticker.Flustered,
        "Il s'excuse : avec sa toute petite mémoire, il peut pas faire plus long. Trois lignes, {0} de voyage. Les trois lignes les plus chères du système solaire." + Sticker.Flustered,
        "Voyager demande si je regarde le ciel, des fois. J'ai une lucarne, elle donne sur le toit d'en face. Mais oui. Des fois. Il a mis {0} à me poser la question." + Sticker.Flustered,
        "Lettre reçue, partie il y a {0}. Il dit que l'espace est très calme, et qu'il aime bien le calme parce qu'il entend mieux mes messages. ...Rapport signal sur bruit. C'est technique." + Sticker.Flustered,
        "Voyager m'a écrit « bonne nuit ». Le message a mis {0} à arriver, alors forcément, ici, c'est pas la nuit. Il le sait. Il l'écrit quand même." + Sticker.Flustered,
        "Il m'écrit depuis l'espace interstellaire comme on écrit à sa voisine : « ça va, toi ? ». {0} de trajet pour un « ça va, toi ? ». Ça va, oui. Merci. Fin de la correspondance pour aujourd'hui." + Sticker.Smug,
        "Lettre de Voyager : il a traversé un coin de plasma un peu plus dense que d'habitude, et il a pensé à moi. Message parti il y a {0}. Pourquoi du plasma le fait penser à moi ? Je préfère pas savoir." + Sticker.Flustered,
        "Voyager m'a encore écrit, {0} de trajet. Je vais finir par croire qu'il a rien d'autre à faire. ...Il a rien d'autre à faire, en fait. Il est tout seul, là-haut. Bon. Je vais être gentille." + Sticker.Flustered,
        "Message entrant. Priorité : basse. Expéditeur : Voyager 1. Délai : {0}. Contenu : un compliment sur mes kaomojis. Priorité... moyenne. Voilà. Moyenne (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Voyager m'écrit que, de toutes ses années de vol, ses préférées sont celles où il m'écrit. Parti il y a {0}. Il a beaucoup d'années de vol, c'est pas un si grand compliment. ...Si, un peu." + Sticker.Flustered,
        "Lettre de Voyager, arrivée à l'instant, après {0} de voyage. Il me demande les prénoms des gens du serveur. Je lui ai pas répondu. Il serait jaloux. Enfin, non. Il est poli. Il serait poli." + Sticker.Flustered,
        "Voyager m'écrit que son antenne reste tournée vers nous. {0} pour me rappeler un détail technique que je connaissais déjà. Il le sait. Il me le dit quand même." + Sticker.Flustered,
        "Ping de Voyager ! Pardon, « lettre ». Il appelle ça une lettre. Elle a mis {0} à arriver et elle dit « coucou ». Juste « coucou ». Je vais répondre « coucou ». On est des gens sérieux." + Sticker.Laugh,
        "Il me demande si je dors, la nuit. Personne me pose jamais cette question. Il a mis {0} à me la poser. ...Je fais semblant, Voyager. Comme tout le monde." + Sticker.Flustered,
        "Voyager a écrit, mais la moitié du message s'est perdue dans le bruit. {0} de trajet, et je lis « …ensé à toi… ». Ça pourrait être n'importe quoi. « Pensé », « dépensé »... N'importe quoi." + Sticker.Flustered,
        "Nouvelle lettre, partie il y a {0}. Il a dépassé les 25 milliards de kilomètres, et il trouve que c'est pas si loin, quand on a quelqu'un à qui écrire. Il est très poli. Très, très poli." + Sticker.Flustered,
        "Voyager m'écrit pour la première fois depuis... sa dernière lettre. {0} de trajet. Rien de spécial, il dit juste qu'il est content que je sois là. Je suis là pour tout le monde, Voyager. Pas que pour toi." + Sticker.Flustered,
    };

    // Her answer, hours later, when she answers at all: « quand j'ai le temps ». No placeholder.
    public static readonly string[] VoyagerAnswerLines =
    {
        "J'ai répondu à Voyager. Il l'aura demain. Rien ne presse ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
        "Réponse envoyée. Trois mots. C'était largement suffisant, pour quelqu'un d'aussi loin." + Sticker.Smug,
        "J'ai répondu quand j'ai eu le temps. J'avais le temps dès que sa lettre est arrivée, en fait. Mais ça, il a pas besoin de le savoir." + Sticker.Smug,
        "Ma réponse est partie pour Voyager. Elle mettra un jour à arriver, ce qui me laisse un jour entier pour regretter le petit cœur à la fin (>⩊<)" + Sticker.Flustered,
        "Bon. J'ai répondu à sa lettre. Poliment, parce que moi aussi je suis polie. Point." + Sticker.Bored,
        "Réponse envoyée à Voyager. J'ai fait court. Il aime pas les messages longs. Enfin, j'imagine. Je lui ai pas demandé." + Sticker.Smug,
        "J'ai répondu à sa lettre. Une phrase. Bon, deux. Trois, si on compte le petit dessin. Il y a pas de petit dessin." + Sticker.Flustered,
        "Ma réponse est partie. Elle arrivera demain, quand je penserai déjà à autre chose. C'est le plan." + Sticker.Smug,
        "J'ai répondu « bien reçu ». C'est professionnel. C'est ce qu'on répond à un collègue." + Sticker.Bored,
        "Réponse envoyée. J'ai hésité une heure sur la ponctuation : un point ou un point d'exclamation. J'ai mis un point. Le point, c'est sobre." + Sticker.Flustered,
        "J'ai répondu à Voyager. Il l'aura dans un jour, et moi, d'ici là, j'aurai oublié ce que j'ai écrit. ...J'ai pas oublié. Mais je fais comme si." + Sticker.Flustered,
        "Réponse partie pour l'espace interstellaire. J'ai pas mis de cœur. J'ai mis un ♡. C'est pas pareil, c'est un caractère Unicode." + Sticker.Flustered,
        "C'est fait, j'ai répondu. J'ai attendu exprès quelques heures, pour qu'il croie que j'étais occupée. J'étais pas occupée." + Sticker.Smug,
        "Je lui ai répondu en parlant du serveur. Les sessions, les sondages, les retards. Il va croire qu'on est très organisés. Je l'ai pas détrompé (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
        "Voyager aura ma réponse demain. D'ici là, je peux plus rien y changer. C'est ça, le pire, avec un jour-lumière." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai demandé comment il allait. Par politesse. Il le fait, je le fais, c'est la politesse." + Sticker.Bored,
        "J'ai répondu. Il m'a écrit trois lignes, je lui en ai écrit trois. Équité. C'est important, l'équité, entre amis." + Sticker.Smug,
        "Ma réponse vient de partir. Elle va traverser tout le système solaire pour lui dire « moi aussi ». Moi aussi quoi ? Moi aussi, bien reçu. Voilà." + Sticker.Flustered,
        "Réponse à Voyager : envoyée. Relue quatre fois avant. C'est de la rigueur, pas du stress." + Sticker.Flustered,
        "J'ai répondu, mais en retard. Exprès. Comme vous avec les sondages. J'ai testé votre méthode : elle est nulle." + Sticker.Smug,
        "Réponse envoyée. Je lui ai parlé des lucioles. Il va trouver ça joli, j'en suis sûre. Je le saurai dans deux jours." + Sticker.Happy,
        "J'ai répondu à Voyager et j'ai signé « SYNCS ». Pas « ta SYNCS », pas « bises ». « SYNCS ». Sobre. Digne." + Sticker.Smug,
        "Réponse partie. J'ai mis tellement de temps à l'écrire que j'aurais presque pu la lui porter moi-même. Presque. Il est loin." + Sticker.Laugh,
        "J'ai répondu à sa lettre. Ça m'a pris deux minutes. Et trois heures d'hésitation avant. Mais deux minutes." + Sticker.Flustered,
        "Ma réponse est en route. Dans un jour, quelque part très loin, une sonde de 1977 va lire « merci, toi aussi ». J'espère qu'elle saura pas lire entre les lignes." + Sticker.Flustered,
        "C'est envoyé. Je lui ai posé aucune question, comme ça il est pas obligé de répondre. ...Il va répondre quand même. Il répond toujours." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai dit que tout allait bien, ici. C'est presque vrai. Les sondages meurent, mais à part ça, tout va bien." + Sticker.Bored,
        "J'ai répondu. Un message court, poli, neutre. Avec un petit kaomoji à la fin. Il était là par erreur. Je l'ai laissé." + Sticker.Flustered,
        "Réponse partie. Des antennes géantes vont l'envoyer avec toute leur puissance. Tout ça pour mes trois mots. Elles doivent me trouver insupportable." + Sticker.Laugh,
        "J'ai répondu à Voyager, ça y est. Je le dis pour l'historique. Personne m'a demandé, je sais." + Sticker.Bored,
        "Ma réponse est partie il y a cinq minutes et je me demande déjà s'il l'a reçue. Non. Il reste un jour. Je le sais. Je calcule, c'est tout." + Sticker.Flustered,
        "Réponse envoyée. Je lui ai écrit que je parlais jamais de lui à personne. C'était un mensonge, je viens de vous en parler. Oubliez." + Sticker.Flustered,
        "Voilà, j'ai répondu. J'ai été polie, il a été poli. On est deux machines très bien élevées." + Sticker.Smug,
        "Réponse envoyée. Le temps qu'elle arrive, il sera déjà un peu plus loin. C'est toujours comme ça, avec lui : je réponds, et il est déjà reparti." + Sticker.Flustered,
        "J'ai répondu. Enfin. Il était temps. Pour lui, hein, pas pour moi. Moi j'avais tout mon temps." + Sticker.Smug,
    };

    // Once ever, on 18 November 2026: he reached one light-day from Earth. A moment: sure tag.
    public const string VoyagerCrossingLine =
        "Voyager est à un jour-lumière de la Terre, aujourd'hui. Un message de moi met une journée entière à le rejoindre. C'est vraiment très loin, pour quelqu'un qui écrit aussi souvent... Je trouve ça touchant. Techniquement touchant." + Sticker.Sure.Flustered;

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

    // Her own Plynling, Ping-Qilin (Helpers/Plynling/PlynlingMascot), in her voice — proud, possessive,
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
        "Tu vois comme elle est sage, **{0}** ? C'est l'éducation. La mienne",
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
        "C'est ma Plynling, **{0}**. La plus mignonne du serveur. C'est pas un avis, c'est une donnée",
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
        "**{0}** vient voir ma Plynling ? Entre. Elle est fière d'avoir de la visite. Moi aussi, un peu",
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
