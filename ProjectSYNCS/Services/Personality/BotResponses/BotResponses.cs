using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// All of the bot's canned "personality" text lives in this folder, separated from
// the logic that decides when to use it. Lines flagged for string.Format use
// {0} = the target's name and {1} = the weekday; lines without placeholders are
// returned unchanged. Keep every formatted line free of literal { } braces
// (string.Format would choke on them).
// ---------------------------------------------------------------------------
// WHAT IS IN HERE
//
// BotResponses is one partial class split by category, one file per category:
// each heading below names its file, as [Replies] for BotResponses.Replies.cs.
// Inside a file, each section is introduced by a "// ---- Title ----" banner, and
// the pools inside it are in the order shown. Names only, no line numbers: those
// go stale on the first edit. Grep the name to jump to it.
//
// ADDING A POOL? Put it in its file and section, and add it to this list too, or
// the list stops being trustworthy and the next person writes a second pool for
// something that already exists. A pool's initializer must not read another
// file's pool: static initialization order across partial files is undefined.
//
//   [Replies] Answering a human
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
//   [Replies] Answering what she was told (MessageCues.ReadIntent)
//     IntentLines .............. keyed by ChatIntent: « je t'aime », « pardon », « ça va ? »…
//     HowAreYouTempLines ....... « ça va ? » with her real temperature, on the Pi
//     RequestLines ............. keyed by RequestTopic: pointing at the command that does it
//     RequestCommands .......... each topic's command, as Papa's {2}
//     OpinionLines · PreferLines « t'en penses quoi de X ? » / « tu préfères X ou Y ? » (/rate's verdict)
//     HelpLines · HelpCommands « comment ça marche ? », pointing at /help or the topic's command
//     AboutHerLines ............ keyed by HerTopic: her age (in days), birthday, home, favourites
//     ShipChatLines ............ « ship X et Y » in chat, around /ship's own answer
//     ShipBestMatchLines · ShipWorstMatchLines « c'est qui mon âme sœur ? »: the server-wide search
//     SharingShrugLines ........ « j'ai mangé de la soupe » — « euh… ok ? », half the time
//     DoYouLoveMeGrudgeLines · DoYouLoveMeTreasureLines « tu m'aimes ? »: more bad bots, or more good bots
//     RememberMeOpeners ........ « tu te souviens de moi ? »: their name, before one of their ChatFactLines
//
//   [Replies] Answering one of her own posts (HerMessages)
//     ReplyContextLines ........ keyed by ReplyContext: the welcome, the late call, the poll…
//
//   [Replies] Long back-and-forths (ConversationStreak)
//     StreakNudgeLines ......... the third message in a row
//     StreakEndLines ........... the fifth: she ends it
//     StreakSulkReactions ...... her only answer while it stays ended
//
//   [Replies] Her records, quoted — ChatFactLines, keyed by ChatFact (Helpers/Personality/ChatFacts)
//
//   [Replies] Reacting with an emote instead of words
//     NiceReactions · MeanReactions · GreetingReactions
//     EnergyDrinkReactions ..... Monster or an energy drink mentioned, every time
//     OwnerReactions ........... him, whatever he wrote
//
//   [Replies] Verdicts on her ("good bot" / "bad bot" / "good girl" / "bad girl")
//     BadBotReplies · BadBotRepliesOwner
//     BadGirlReplies · BadGirlRepliesOwner
//     GoodGirlReactions · GoodGirlReactionsOwner
//     TurnaboutBoyLines · TurnaboutGirlLines · TurnaboutNeutralLines
//                              ... the ~1-in-100 spoken answer to praise
//
//   [Replies] Self-preservation — ShutdownThreatOwner · ShutdownThreatTata · ShutdownThreatReplies
//
//   [Family] Rodhengard (the owner)
//     OwnerGreetings ........... mentioned  |  OwnerComebacks ..... replied to
//     OwnerMeanReplies ......... him being mean to her
//     OwnerIntentLines ......... his twins of IntentLines (ça va, tu fais quoi, je t'aime, pardon, bonne nuit,
//                                peut-être, t'es un bot, t'as raison, t'as tort, t'es là, merci,
//                                je m'ennuie, t'as un crush, son âge et sa maison, fais genre, sa journée,
//                                oui, non, tu m'aimes, tu te souviens de moi, t'es fâchée)
//     OwnerRequestLines ........ his requests, with the command as {2}
//     OwnerAbsentNotices ....... someone pinged him while away
//     OwnerReplyHeralds · OwnerAnnouncementHeralds ... /debug tell and dm heralds
//
//   [Family] Tata (Analuz)
//     TataId ................... her id, the single source of truth
//     TataGreetings (mentioned) · TataReplies (replied to)
//     TataIntentLines .......... « tu m'aimes ? », « tu te souviens de moi ? » from her
//
//   [Rivals] Other bots
//     JealousLines · JealousLinesOwner ... praise went to a rival
//     RivalMutters ............. a rival posted
//     RivalLevelUpLines ........ a level-up on the rival's XP system
//     RivalJoinLines ........... a new bot joined the server (WelcomeService)
//
//   [Commands] Commands
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
//   [Scheduling] Organising
//     SessionLateCallLines ..... ten minutes into a session, to who isn't in voice (SessionAttendanceService)
//     SessionLateCallSeatedLines  same, when she has been in voice since before the start
//     SessionRecapLines · SessionRecapFullLines ... the roll call after it, someone missing / everyone came
//     WeekendPollLines ......... her Wednesday poll (WeekendPollService)
//     WeekendPollResultLines · WeekendPollDeadLines ... it closed with votes / with none (ReminderService)
//     WelcomeLines ............. someone joined (WelcomeService)
//     WelcomeRushLines ......... too many arrivals in an hour
//     ReminderSetLines ......... /remindme's confirmation
//     ReminderLines · ReminderLateLines ... the reminder itself, on time / late (PersonalReminderService)
//     TeamsDrawLines · TeamNames ... /teams
//     TonightCardLines · TonightExpiredLines ... /tonight's card, open / closed at midnight
//     TonightThresholdLines .... /tonight reached its minimum
//
//   [FunCommands] /rate, /ship, /blame, /recap, /wouldyourather
//     RateBand0Lines … RateBand4Lines + RateBand(score) ... /rate, by score band
//     RateHerselfLines · RateOwnerLines · RateOwnerSelfLines · RateTataSelfLines · RateCrushLines · RateNemesisLines · RateDevilLines
//     · RateGodLines · RateTataLines · RateRoutierLines · RateMitrailletteLines ... /rate's fixed verdicts
//     ShipBand0Lines … ShipBand5Lines + ShipBand(percent) ... /ship, by percent band
//     ShipSelfLines · ShipHerselfPapaLines · ShipHerselfQuokkaLines · ShipHerselfOtherLines
//     · ShipHeartLines (per robot-heart character) · ShipWallEEveLines · ShipPapaLines · ShipNemesisLines ... /ship's fixed verdicts
//     BlameLines · BlameSelfLines · BlameQuokkaLines · BlameNobodyLines ... /blame
//     BlameOnPapaLines → BlamePapaDodgeLines · BlamePapaDodgeQuokkaLines ... /blame's slip on Papa, then its edit
//     RecapLines (per RecapProfileKind) · RecapOwnerLines · RecapHerselfLines ... /recap's last line
//     WouldYouRatherDilemmas · WyrIntroLines · WyrUnanimousLines · WyrTieLines · WyrLandslideLines
//     · WyrCloseLines · WyrNobodyLines · WyrHerPickLines ... /wouldyourather
//
//   [Arcade] /arcade games and Quokka's visits
//     DuelChallengeLines · DuelOwnerWarningLines · DuelHerAcceptLines · DuelAcceptedLines
//     DuelDeclinedLines · DuelWithdrawnLines · DuelTieLines · DuelDrawLines
//     DuelWinLines · DuelHerWinLines · DuelBrokeLines · DuelExpiredLines · DuelAbandonedLines ... /arcade duel
//     HeistIntroLines · HeistBeatOneLines · HeistBeatTwoLines · HeistBeatThreeLines
//     HeistSuccessLines · HeistFailLines · HeistBrokeLines · HeistCooldownLines ... /arcade heist
//     RaceIntroLines · RaceStartLines · RaceLeadLines · RaceWinLines · RaceNoRunnersLines · RaceAsleepLines ... /arcade race
//     CrashIntroLines · CrashBoomLines · CrashGroundedLines · CrashEmptyLines ... /arcade crash
//     BlackjackDealLines · BlackjackWinLines · BlackjackLoseLines · BlackjackBustLines
//     · BlackjackPushLines · BlackjackNaturalLines ... /arcade blackjack
//     BombIntroLines · BombLitLines · BombPassLines · BombBoomLines · BombCancelLines ... /arcade bomb
//     QuoteIntroLines · QuoteWinLines · QuoteTimeoutLines · QuoteWrongLines · QuoteSelfLines · QuoteNothingLines ... /arcade quote
//     TempIntroLines · TempWinLines · TempCancelLines · TempNoSensorLines ... /arcade temperature
//     BoardChallengeLines · BoardHerStartLines · BoardWinLines · BoardHerWinLines · BoardBeatHerLines
//     · BoardDrawLines · BoardHerDrawLines · BoardForfeitLines · BoardTimeoutLines · BoardCheatLines
//     · BoardExpiredLines · BoardDeclinedLines ... /arcade puissance4 · morpion
//     QuokkaRaidLines · QuokkaHitLines · QuokkaFledLines · QuokkaTooLateLines ... Quokka's visits (QuokkaRaidService)
//
//   [Elsewhere] Elsewhere
//     PresenceFillers .......... the rotating status line
//     MorningGreetings ......... the daily hello (MorningGreetingService)
//     MorningFunFacts .......... added under it
//     BirthdayGreeting ......... replaces both on 16 June (her age)
//     BreakdownIntroRoast · BreakdownIntroNice · BreakdownIntroCake
//                              ... the line it cuts off mid-word
//     Breakdown ................ the easter egg
//
//   [Ambient] Ambient — AmbientService, PiHealthService, and PresenceService at night
//     NightPresenceFillers ..... her status from 1:00 to 7:00
//     HotPresenceFillers ....... her status when the Pi runs hot
//     SeatPresenceLines · SeatPresenceOwnerLines ... her status while she sits in voice / with Papa there
//     SeatStreamPresenceLines .. same, while someone streams in her call
//     NightLines ............... the 3 a.m. line
//     NightScoldLines .......... answering it before 5:30 (ResponsePicker)
//     IdleLines · IdleEditLines  a long daytime silence (edit = Before, then After)
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
//
//   [Lore] Synthia, her robot heart, Voyager
//     SynthiaReactions · SynthiaVanishLines · SynthiaEditLines ... « Synthia » said (SynthiaService)
//     HeartReactions · HeartLines · HeartVanishLines · HeartEditLines ... one of her robot heart
//                              named (RobotHeartService), keyed by HeartCharacter
//     VoyagerLetterLines · VoyagerAnswerLines · VoyagerCrossingLine ... Voyager 1 writes to her
//                              (AmbientService; light time from Helpers/Personality/Voyager)
//
//   [Plynlings] Plynlings — every pool is a GenderedLines (M/F halves), picked with .For(p.Gender)
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
//   [this file] Per-person data and lookups (not pools)
//     PersonalComebacks ........ per-user roast lines
//     FamilyNicknames + DisplayNameFor(ulong, string) / DisplayNameFor(IUser)
//     PersonGender + KnownGenders + GenderFor ... seeded from confirmation, never inferred from a name
//     RealNames + RealNameFor .. real first names, used by the breakdown reveal
// ---------------------------------------------------------------------------
internal static partial class BotResponses
{
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
