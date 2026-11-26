using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// Checks for how SYNCS reads chat (MessageCues, Opinion, ShipAsk), the pools that answer it, and
// the listening pieces around them. No test framework on purpose: a console that prints only what
// fails plus a few summary lines, and exits 1 on any failure.
//
//     dotnet run --project tests/ProjectSYNCS.Checks
//
// Run it before shipping a change to the cues or the pools. Add a case for every misfire found in
// the wild, next to the section it belongs to: each file below holds a run of sections, under
// ~40 KB so it can be read whole. What they share (the failure count, the internals reached by
// reflection) is in Check.cs.
//
// First: the story files the catalog compiles are the repo's, not a copy in bin. Many sections touch
// PlynlingEvents.All, whose first read loads them.
StoryCatalog.Folder = Path.Combine(StoryChecks.RepoRoot(), "ProjectSYNCS", "Stories");
VisitCatalog.Folder = VisitChecks.ShippedFolder;
// Likewise the painter's layers: every card built with a dressed Plynling draws from them.
PlynlingPainter.AssetsRoot = AccessoryArtChecks.LayersRoot;

// 1–7. Her text pools and dictionaries, sulk reactions, ReadIntent's table, conversation streaks and
// listening, ChatFacts, and the opinions, ratings and ship readings. ChatPoolChecks.cs.
ChatPoolChecks.Run();

// 8–15. /debug answers, widened cues and the misfires they must not cause, the chat sweeps, Sharing and Ship.
// ChatCueChecks.cs.
ChatCueChecks.Run();

// 16–21. Every intent under six variants, mentions and quotes, Disbelief, the widened weak intents,
// Down/ShouldI/Joke/Blame/Love, yes and no under her questions. ChatIntentChecks.cs.
ChatIntentChecks.Run();

// 21b–22. The schedule, numbers, dice and the other small asks, and Papa's pet names. ChatAskChecks.cs.
ChatAskChecks.Run();

// 23. /report and the monthly digest: windows, when the digest is due, the session cards. ReportChecks.cs.
ReportChecks.Run();

// 24–32. /report's activity keys and counts, every card at its worst, quiz, Plynlings, emotes, sleeping
// members, and the line on top. ReportCardChecks.cs.
ReportCardChecks.Run();

// 33–34. The monthly digest: « Le mois en bref » and its honesty. DigestChecks.cs.
DigestChecks.Run();

// Minecraft: when she joins and leaves, how she reads deaths, and her in-game orders. McOrderChecks.cs.
McOrderChecks.Run();

// Minecraft orders, continued: remembered and other people's places, the sweep of game-chat wordings and the
// misreads it found, more materials. McOrderSweepChecks.cs.
McOrderSweepChecks.Run();

// Minecraft: what she carries, SRV, her family in game, hit streaks, claims, and her in-game pools.
// McGameChecks.cs.
McGameChecks.Run();

// Minecraft talk (McTalkReader, McChat.Game), and a call always getting words. McTalkChecks.cs.
McTalkChecks.Run(args);

// /minecraft (MinecraftModule and Helpers/Minecraft), and the picker's narrow pick for game chat.
// MinecraftModuleChecks.cs.
MinecraftModuleChecks.Run();

// Replies under her cards (MessageCues.ReadCardReply). CardReplyChecks.cs.
CardReplyChecks.Run(args);

// 34. Member badges. BadgeChecks.cs.
await BadgeChecks.Run();

// 35–40, 44. /remindme times, quiz typed answers, blackjack, board games, the XP curve, cailloux and the
// daily streak. RuleChecks.cs.
RuleChecks.Run();

// 45. Bonjour or bonsoir (DayParts, TimedPool). DayPartChecks.cs.
DayPartChecks.Run();

// 41–43. Session places, per-message counts, and scheduling. SchedulingChecks.cs.
await SchedulingChecks.Run();

// 46–49, 58. Long absences, late replies, replies to her morning hello, edits, channel milestones.
// ChatMomentChecks.cs.
ChatMomentChecks.Run();

// 50–51. Requests made to her in chat, and the /debug answers misses of November 2026. ChatRequestChecks.cs.
ChatRequestChecks.Run();

// 52. Mécène (jar gifts, Patronage), the badge and tab, /pay, packs: EconomySinkChecks.cs.
await EconomySinkChecks.Run(Fail);

// 53. Bribes and the dedication auction: BribeChecks.cs.
await BribeChecks.Run(Fail);

// 54. The jar and the weekly lottery: JarLotteryChecks.cs.
await JarLotteryChecks.Run(Fail);

// 34. Story files: StoryChecks.cs.
StoryChecks.Run(Fail, args);

// 56. Visit scenes: VisitChecks.cs.
await VisitChecks.Run(Fail, args);

// 57. The bond simulation for visits with choices: VisitSimulation.cs (`-- visit-sim` prints its table).
VisitSimulation.Run(Fail, args);

// 50. Story engine: StoryEngineChecks.cs.
StoryEngineChecks.Run(Fail);

// 55. The 50 old C# events, frozen before they moved to story files: OldEventChecks.cs.
OldEventChecks.Run(Fail);

// 59. The cosmetics screen: every tab and item view inside the caps, the try-on: CosmeticScreenChecks.cs.
CosmeticScreenChecks.Run(Fail);

// 60. Forage places, the aimed draw and the typed names: ForagePlaceChecks.cs.
ForagePlaceChecks.Run(Fail);

// 61. Crafting: « any commons » ingredients, the recipe catalog and its bands: CraftingChecks.cs.
await CraftingChecks.Run(Fail);

// 62. Accessories on the sprite: every picture exists, styles match the art, the picker:
// AccessoryArtChecks.cs.
AccessoryArtChecks.Run(Fail);

// 63. The pictures on an event's story (faces, face tags, the stage then): EventFaceChecks.cs.
EventFaceChecks.Run(Fail);

// 64. Auras, the fifth slot: catalogue, worn with an accessory, the picture, the screens: AuraChecks.cs.
await AuraChecks.Run(Fail);

// 65. The RPG foundation (body stats, levels, Énergie, the drill): RpgChecks.cs.
await RpgChecks.Run(Fail);

// 66. /plynling play's games: rules, simulated odds, text and cards: PlynlingPlayChecks.cs.
PlynlingPlayChecks.Run(Fail);

// 67. /arcade guess: rules, the payout's average, the card: ArcadeGuessChecks.cs.
ArcadeGuessChecks.Run(Fail);

// 68. The Plynling score: the hooks, once per pair, the mornings, the rebuild, the tombstone, the leaderboard
// view (ScoreChecks.cs), and the tombstone calibration against the old ages (ScoreCalibrationChecks.cs,
// `-- score-sim` prints the days).
await ScoreChecks.Run(Fail);
ScoreCalibrationChecks.Run(Fail, args);

// 69. The online user guide names every member and staff command: GuideChecks.cs.
GuideChecks.Run(Fail);

// 70. Dressed pictures drawn from layers (PlynlingLook, PlynlingPainter): PaintChecks.cs.
PaintChecks.Run(Fail);

// 71. Wearing two accessories (categories, sides, picks, try-ons, restyles): AccessoryFitChecks.cs.
AccessoryFitChecks.Run(Fail);

// 72. More than one server: home-only people and voters, per-server silence: MultiServerChecks.cs.
MultiServerChecks.Run(Fail);

// 73. Ping-Qilin's daily outfit: the looks, the week decks, the seasons: MascotWardrobeChecks.cs.
MascotWardrobeChecks.Run(Fail);

// 74. /plynling stats: counters, their hooks, the sheet, ranks, duel lines, cards: StatsChecks.cs.
await StatsChecks.Run(Fail);

// 75. Plynling badge art: icon keys, emoji names, fallbacks, shipped files, journal length: PlynlingBadgeArtChecks.cs.
PlynlingBadgeArtChecks.Run(Fail);

Console.WriteLine(Failures == 0 ? "ALL CHECKS PASSED" : $"{Failures} FAILURE(S)");
return Failures == 0 ? 0 : 1;
