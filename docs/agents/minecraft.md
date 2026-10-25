# Her body on the Minecraft server

SYNCS joins the group's Minecraft server (Java Edition, offline mode) as a real player.
`MinecraftBodyService` (C#) decides and speaks. The body (`ProjectSYNCS/MinecraftBody/index.js`,
Node + Mineflayer) moves and reports.

## Two processes, one voice

The body is a Node child process the service starts and stops. They talk in JSON lines: commands
go in on stdin, events come out on stdout, and logs go to stderr, which the service forwards to
`ILogger`. The protocol is listed at the top of `index.js`.

**The body holds no words and no policy.** It never decides to speak, to join or to leave, and it
has no French in it. Every line she says in game comes from the `Mc*` pools in
`BotResponses.Minecraft.cs`, through `ResponsePicker`, so `docs/syncs-voice.md` and the checks
cover them like any other pool. A new reaction is a new event in the body plus a pool and a branch
in the service, never a string in JS.

**She types before she speaks**, as on Discord: every line goes through `SayLocked`, which queues
it behind her previous one and waits 0.7–3 s by its length. A new send path goes through it too.
Two lines skip the wait: a forced remark (a creeper behind someone, drowning: it can't wait) and
her goodbye, which the quit right behind it would cut off.

**The child starts with an empty environment.** It gets only `PATH`, `HOME`, the temp variables,
`SYSTEMROOT` (which Node needs on Windows) and its own `MC_*` variables. The bot's environment holds
the Discord token (`run.sh` exports it), and a third-party npm tree must never see it. Don't
"simplify" this by inheriting the environment.

**Death and advancement messages are read by translate key** (`death.attack.creeper`,
`chat.type.advancement.task`), with the victim as the first argument and the killer as the second.
The keys are the same in every language. Mineflayer renders the text from its own English data, so
`McDeath` never parses text, only keys and entity keys (`entity.minecraft.creeper`). A death whose
victim isn't on the server, such as a named pet's (sent to its owner only), is ignored.

## When she joins and leaves

**Nothing happens until the owner allows it.** `/debug minecraft` → « L'autoriser à jouer » /
« Lui interdire de jouer » flips a switch kept in `minecraft-state.json` next to the database. It
survives a restart, because « off until I say so » must not turn itself back on after a crash. (The
file also keeps where she last saw Papa and Tata, see *Arriving*, and the places she was asked to
remember, see *Orders*; everything else is in memory.)
It starts off (no file = forbidden). While forbidden she still
pings the server, so `/debug` shows it, but never joins, and a forced join is refused. Forbidding
takes her out at once.

`McPresence.Decide` is pure, and the checks walk it:

- **Join** when at least 4 players are on, or the owner is (`Minecraft:Owner`, compared without
  case).
- **Leave** once 2 or fewer others have been on for more than 3 minutes, or when the server stops
  answering its status ping. While the owner is on, the "≤2" rule is suspended, or she would join
  for the owner, leave 3 minutes later and come straight back.
- **Three players is a dead band**: she stays if she's already in, but doesn't join for three.

Before she's in, the count and names come from the **status ping** (`ServerListPing`, which needs
nothing on the server). Once she's in, they come from her own player list. Two limits of the ping:
it lists at most 12 names, and a player who turned off « Allow Server Listings » shows as
« Anonymous Player ». So the owner can be missed before she joins: the count still applies, and so
does `/debug minecraft join`.

**An address with no port** is how most hosts hand out a server: the real host and port are in a
DNS SRV record, `_minecraft._tcp.<name>`. `McAddress` does that lookup for the ping (DnsClient)
under the same conditions as the client library: the default port, and a name that isn't an IP.
The body gets the address as configured and does its own lookup, so its handshake carries the same
host a player's client would. Leave `minecraft_port` at 25565 for such an address.

The service polls on **its own 1-minute interval**, like every loop. The server is only up during
sessions and G-Portal has no start/stop API, so she **detects** the server and never starts it.

## Orders

A player gives her an order in chat (`McCommand`, pure, with cases in the checks). It needs her
name in the message (`McCommand.HerNames` plus her in-game name: « sync » counts), because the
chat is shared. Stay is read before refusals, because « bouge pas » means stay; « reste avec moi »
is read before stay, because it means follow.

**Refusals are read next to the verb only**: « ne » up to two words before, « pas » just before or
up to two words after, « jamais » after, « plus » after unless it means more (« plus de bois »).
A « pas » further away belongs to another sentence (« c'est pas grave, syncs viens »).

**The phrase lists are written the way people type in game chat**: familiar French (« ramène ta
fraise », « fais du bois », « il me faut »), shortcuts (« vien », « vi1 », « diams »), a little
English, and the infinitives of questions (« tu peux venir ? »). Every phrase is whole words,
longest first. A word that is common outside orders stays out (« assez », « help » alone, « à
moi »): a missed order costs a retype, a false one makes her walk off. Add the misses found in the
wild, with a case in the checks.

Before matching, `Clean` squeezes letters held down for emphasis (three or more become one:
« viiiiens »), maps game-chat spellings (`Slang`: « mwa », « twa », « chui » → « je suis », so
« chui là » is never « suis » = follow) and drops politeness. Words of six letters or more match
with one slip (`Close`: a letter added, dropped, changed or two swapped), and so does her name;
shorter words must be exact, because at four or five letters one slip makes another word
(« viens » / « liens »).

**The slip cuts both ways at six letters too**: « ranger » / « manger », « ranges » / « manges »,
« préviens » / « reviens », « pêches » / « poches ». Such a word stays out of a list alone and only
comes in a longer phrase (« ranger tes affaires », « préviens-moi »). Check any new six-letter
word against the common words one slip away.

**Talk about oneself is not an order.** A phrase right after « je » doesn't count (« je range ma
chambre », « ça fait 5 min que j'attends »), unless it starts with it (« j'ai faim »); nor does one
right after « ai » or « as », the past tense (« je t'ai donné ma pioche », « j'ai vidé mes
poches »). A few one-word verbs are nouns or participles after certain words (`NounAfter`: « la
pêche », « j'ai pêché », « c'est cuit », « au fond », « la base est protégée ? »), looked up by the
word as typed, since the slip lets « pêché » match « pêcher ».

**Counts** come from a number anywhere (« 20 », « x20 », « 20x »), else from a quantity in words
right before the resource (« dix bûches », « une stack de pierre », « une dizaine de bûches »),
else 10. Only right before: « du bois pour une maison » isn't one log. **Coordinates** accept
F3's decimals, floored to the block (-300.7 is block -301), and labels or brackets around them.

**An order can come in two steps.** A message that only names her (« syncs », « hé syncs ? ») is a
call, but only when the name opens or closes it; elsewhere it's talk about her. She answers with a
short « oui ? » (`McCalledLines`), and that player's **next message within 30 seconds** is read as
if it carried her name (`McCommand.Parse(…, called: true)`): « syncs » … « suis moi ». One message
only, whatever it turns out to be, so a call followed by chat doesn't keep her listening; a
follow-up she doesn't understand is read as talk (*Talk*, below), never another « oui ? ». A call
with something she reads as talk in it (« syncs ça va ? ») is answered as talk instead of « oui ? »:
only a call with nothing to answer (`McTalkReader.IsBareCall`, or nothing read) gets it. Other players still need her
name in that window. **A compliment or a thank-you in that same place is not a call**: « gg syncs »,
« bravo syncs » (`Praise`, with a hop) and « merci syncs » (`Thanks`) get their own lines. An
adjective counts only when said to her (« t'es géniale »): « syncs la base est jolie » is about the
base, and stays a call. The window lives in the service, per player, in memory. There is no help
list: the owner removed it.

- **Come** walks to where the player stood (`GoalNear`, 2 blocks). **Follow** keeps a moving goal
  on the player (`GoalFollow`). **Stay** clears the goal and cancels a gathering run.
- **Go to** takes 2 numbers (x z, `GoalNearXZ`) or 3 (x y z), read from the raw text so a minus
  sign survives normalisation. Over 1500 blocks she refuses (`tooFar`): the pathfinder can walk
  that far by planning partial paths as chunks load, but she'd be gone for the evening.
- **Guard** follows the player and, every physics tick, picks the closest monster within 10 blocks
  of them (or 6 of her), chases it, and hits it on a 650 ms cooldown, with the best sword or axe she
  carries. Our own loop, not `mineflayer-pvp`, which pulls a whole Mineflayer 2.x through
  `mineflayer-utils`. Endermen and piglins are left alone: hitting them starts the fight she's
  supposed to prevent; so are the warden (two hits kill her) and the creaking (nothing hurts it while
  its heart stands). **Only a monster in sight** (`inSight`: nothing solid between her eyes and its head or chest,
  `bot.world.raycast`) is taken on, hit or shot; one she chases that stays out of sight 4 s (`FoeUnseen`) is let
  go. A monster in a cave under the ground is near by distance alone: she shot arrows into the ground at a creeper
  there. **What's a monster is `isFoe`**: minecraft-data's `hostile`, plus the few it
  files elsewhere (slimes, magma cubes and phantoms as `mob`, hoglins as `animal`), which she
  otherwise never fought. Kills are a rationed remark.
- **Collect** (`mineflayer-collectblock`) finds the requested blocks within 48 blocks, checks she
  can harvest them (with what she carries, or by hand; ores and stone need a pickaxe, `noTool`),
  breaks them, then walks back and tosses what she gained (an inventory diff) at the player's
  feet, **whole stacks only** (`deliverGains`): `toss` with a count drops one item per click, and
  on 26.1 only the first landed while her inventory went out of step with the server's (one wheat
  of 25 reached Papa, then the seeds were « not found »). A stack partly hers from before stays with
  her. collectblock asks the pathfinder's movements before breaking anything, so it gets
  `gatheringMovements`: `safeToBreak` is true **only for the target blocks and the tree's own
  natural leaves within 2 of a target log**, so nothing else on the way is ever broken. A log
  wrapped in leaves has an open face (its top, the far side) but none she can see from where she
  stands, and the search for one ran its 5 s for every log of a forest, nothing brought back; a
  player punches through, so does she. Wood is a non-stripped log with *natural* leaves within reach
  (`persistent: false`): a log in a wall has none, and placed leaves are persistent, never broken.
- **Wood is felled a whole tree at a time** (`treesNear`, `fell`), the closest tree to where she
  stands first, each to its last log, so she brings more than asked rather than leave a trunk floating
  half cut (cutting the closest logs anywhere did that all over a forest). A tree is the logs of one
  kind touching each other, corners included (acacia and dark oak branches), from the bottom of its
  trunk, within 6 sideways and 40 up, with natural leaves near its 8 highest logs (`treeOf`: a big
  spruce's trunk has none for its first ten blocks). Bottom up, in rounds; a round ends at the first
  log she can't get to, so a tall top never costs a 5 s search per log. **Out of reach, she climbs**
  (`gatheringMovements`' `climb`): a pillar stacked under herself (`allow1by1towers`, dirt or stone
  first, else logs, `ScaffoldItems`), straight up only, never a bridge, **up the trunk's own column**
  (a block placed anywhere else costs `ClimbBeside`): stacked beside it, the logs she cut fell past her
  out of reach and she ran out of blocks halfway up a spruce; under them, each one falls on her. Before
  she climbs, she picks up what lies on the ground (a drop is gone after 5 minutes, and a 2x2 spruce
  takes about 6 by hand). After the tree, **the pillar comes down** from the top (`unstack`: she
  breaks the block she stands on), then the drops are picked up. Two pathfinder snags this works
  round, both in `gather`: every node after the first block to break or place is left at a block's
  corner (`centred` moves them to its middle: on each step of the pillar she walked for the corner of
  the block she stood on), and the step up sets its block while her feet are still inside it, which
  the server refuses (the placement waits for the top of the jump). And she **lands before every dig** (`landed`, in
  `toolOrHand`): a block takes five times as long in the air, and Mineflayer times the dig when it
  starts (16 s instead of 3 for a log, right after a step up). The camp gets its wood the same way.
  **Only blocks with an open face** (`exposed`), open to air, not water (coal at the bottom of a
  lake: she went down for it, mined at a fifth of the speed with her head under, and drowned; and
  when she has to surface, a dig in progress is dropped, since it goes on whatever her keys say),
  the closest 256 of those, open being part of the
  search (`findBlocks`' `useExtraInfo`), not a filter after it: she never digs, so stone under the
  grass is out of reach, and on a mountain the nearest 256 stone were all under her feet while a
  cliff face stood 20 blocks off. **What she counts is what the resource drops** (`Yields`: logs,
  cobblestone, coal, raw iron…); the saplings, sticks and apples from the leaves are handed over
  but not counted as wood. **In rounds**: what's open now, then what that opened (a trunk's top log, boxed in by
  leaves, only opens once the logs under it are gone: she brought 12 of 16), up to the count, four
  rounds at most, ending early on a round that broke nothing. **One block at a time** through
  collectblock, closest first: it gave up on the whole
  list on its 5 s search for one block it couldn't reach. **collectblock only breaks** (`gather`): it would walk into each drop's own block and wait for it
  to vanish, with no time limit, and a drop on leaves, by a fence or under something low kept her
  there for good. The drops of what it breaks are noted instead, within 2 blocks of a broken block
  (`itemDrop`; collectblock's own half-block left a lump of coal lying), and picked up after by `pickUp`. **Blocks with no collision box** (wheat, carrots, potatoes,
  beetroot) don't go to collectblock at all: it walks to where a ray from her eyes hits the block,
  a ray goes through those, and it searched for 5 s and gave up. `breakSoft` walks within 2 blocks
  of each and breaks it **by hand** (`emptyHand`): a tool from her last order (an axe, a sword) is
  put away by switching the hotbar to an empty slot or one without a tool, never by moving her
  pockets (window clicks put her inventory out of step with the server's). Melons and pumpkins are
  solid, go to collectblock, and get the axe. **A tool only when it beats the hand** (`toolOrHand`,
  over `bot.tool.equipForBlock`, so collectblock and the camp get it too): the tool plugin keeps
  what she holds when nothing is faster, and she cut cacti with a pickaxe and bamboo with an axe.
  Now a cactus is cut by hand (`emptyHand`), bamboo with a sword, by hand without one. Replanting picks the seed stack after the walk to the
  patch, not before: pickups on the way moved it, and she placed with an empty hand.
- **Columns** (bamboo, sugar cane, cactus: `Crops`, `column`): she cuts each one's second block,
  which brings down everything above it, and the bottom one stays to grow again (no replanting).
  Only when named or on a farm, never in « everything ripe »: a bamboo jungle, a desert's cacti
  and a river's cane are mostly wild. Their pieces drop where each piece was, up to 16 high, so
  drops are noted by where they land (`dropOf`: within 2 sideways, any height above), not within 2
  of the broken block. **A cactus is no floor**: once a column was cut to its base the pathfinder
  walked her onto it and she stood there being pricked; `gentleMovements` makes it neither floor
  nor passage. « canne » alone is her fishing rod, so sugar cane is read by its « sucre ».
- **What lies in a field is the field's** (`fieldDrop`, `FieldSweep`): produce (`FarmProduce`) on
  or beside farmland or a crop. A farm round sweeps it every round, ripe or not; a harvest picks up
  what lies near what it cut. A carrot that rolled further than the noted drops, or one someone
  trampled out, stayed on the ground round after round. Anything else lying about is still left
  where it is (a player's dropped things are theirs).
- **Picking items up** (`pickUp`, `GoalPickUp`): a player picks up what touches their box grown by a
  block sideways and half a block up and down, so the goal is « within that reach », not « within a
  block of it » (`GoalNear` in whole blocks never allowed an item on a fence post, on a pillar's
  edge, or under a slab too low to stand under). Each item gets 8 s (`PickUpTry`), and another try
  (three in all) only if it rolled more than a block since. The same 8 s caps an item of loot. **In
  reach is not picked up**: a fresh drop waits half a second before anyone can take it, so she waits
  up to 1.5 s for it to go before the next. A way to an item she can't find ends that item's try,
  never the whole sweep (`noWay` leaves a sweeping `retrieve` alone). A node counts within 1.5 of the
  item sideways (`PickUpReach`: her box reaches 1.3, leaning against what's in the way 0.2 more),
  and an item still there when she arrives gets her leaning in towards it for half a second: coal at
  the back of the hole its ore left in a quarry face had no node within 1.2, and stayed there.
- **`bot.pathfinder.goto` is ours** (`goto`, set on spawn). The pathfinder's resolved on any search
  that came back with no path, and a search's first slice (`tickTimeout`) often does: she « got
  there » without moving, stood 2 blocks from a cull's beef and left it, again and again, and
  everything awaiting goto (bed, replanting, collectblock) had the same lie. Ours resolves on
  `goal_reached` or a *finished* search with nothing to walk (over farmland, 15/16 high, her floored
  position is a block lower than the search's start and never matches the goal), and rejects as
  before on no path, a search out of time, another goal, a stop.
- **Sleep**: the night only passes when every player sleeps (by default), and that includes her.
  When a player goes to bed at night (`entitySleep`) she looks for a bed within 48 blocks, once per
  night, and says so when there's none (`McNoBedLines`), since she is then why the night won't pass.
  « syncs dodo » does the same on demand.
- **She also goes to bed on her own** once it's night by the clock (never for a daytime storm), so the
  night passes the moment the players lie down (`autoSleepTick`, on the body's `time` event). Only when
  nobody needs her: free, or at an activity, which she goes back to when she wakes. Never in the
  Nether or the End, where a bed explodes (the same guard covers the other two ways to bed). Not on a
  night she was told to stay or sit. A bed that refuses her (monsters near, occupied) is tried again
  two minutes later; no bed within 48 blocks and she gives up for the night, silently: on her own,
  nobody is waiting for an answer. In bed she says so once (`McBedtimeLines`, a rationed remark).
  **Any order gets her out of bed** (`stay()` calls `wake()`: lying down, she can't move), and once an
  order has woken her she stays up that night, so « viens » doesn't end with her walking back to bed.
- **Harvest** (« syncs récolte le blé », McOrder.Harvest with a `McCrop`, `Any` for « syncs
  récolte ») breaks the *ripe* crops within 24 blocks (wheat, carrots, potatoes at age 7, beetroot
  at 3), puts a seed back on each patch of farmland left bare, and tosses the haul at the player's
  feet like a gathering run. **Number states come as strings on 26.1** (an age of « 7 »), booleans
  as booleans: `isRipe` converts, and while it only took numbers she saw nothing ripe at all.
  Melons and pumpkins count only next to an *attached* stem, so a pumpkin
  placed as decoration is never touched, and need no replanting. In the parser a crop word is read
  before the resources (« pommes de terre » has « terre », dirt); a generic word (« champ ») never
  beats a resource (« du bois dans le champ »), and « récolte » after an article is the noun. A crop
  takes any gathering verb plus a few that mean nothing alone (`CropOnlyVerbs`: « arrache »,
  « farm », « déterre »; « arrache-toi » is not an order), and the verb must be another word than
  the crop (« la récolte est prête » names it, doesn't ask). Crop words take one slip from six
  letters (« citrouile »); further misspellings are listed (« carrote », « betrave »). « courge »
  stays out: one slip from « course ».
- **Farm** (« syncs occupe-toi de la ferme ») anchors her where she stands. Every 15 seconds she
  harvests what ripened within 16 blocks of that spot, replants, and stores the produce in a chest
  or barrel within 10 blocks, keeping 16 of each seed back to replant and 16 wheat for the animals (`FeedKept`;
  the seeds, carrots and potatoes kept already feed chickens, pigs and rabbits); a full chest leaves the rest
  on her. No crops, or **no chest**, and she refuses (`McFarmNoneLines`, `McFarmNoChestLines`)
  rather than fill her pockets all evening. The farm cue is read before stay (« reste à la ferme »)
  and guard (« garde le champ »): a phrase that says it alone (« cultive », « mode fermière »), or
  a **care verb with a farm word within four words after it** (`FarmCare`: « prends soin de mes
  plantations », « surveille le potager »). The verb alone never farms: « garde-moi » is guard,
  « il reste du blé » is stay. Only the unambiguous care verbs (`CropCareVerbs`: « occupe-toi »,
  « entretiens ») take a crop instead of a farm word; « garde des carottes » keeps some.
  « ferme » counts only after an article or possessive, since « et ferme-la » means shut up. Going to bed pauses the farm and waking resumes it
  (`resumeAfterSleep`); any order but an errand ends it. A farm and a guarded place are the claims
  that don't run out while their keeper is on the server (`McClaim.Decide(…, lasting)`); family can
  still take her.
- **Ranch** (« syncs occupe-toi des animaux », « nourris les vaches », « fais de l'élevage ») anchors
  her where she stands, like a farm, and lasts like one. Every 15 seconds, for the cows, sheep, pigs,
  chickens and rabbits within 16 blocks: a species under 8 (adults and babies) gets two adults fed
  its food (not the same animal twice in 5 minutes), sheep are sheared, the adults over 8 are culled
  (2 a round at most: the closest first, 6 s to land a first hit on one before trying the next,
  since in a crowded pen she spent whole rounds on one behind the others), and the drops (picked up
  within 4 blocks of the kill, 20 s) go in the chest within 10 blocks (meat, leather, wool,
  feathers; 8 pieces of meat stay on her to eat, and breeding food is never stored). **Short of a species' food,
  she takes some from the pen's chests** (within 10 blocks, 16 at most, `fetchFeed`): the wheat a farm stored is
  there. Chests that had none aren't opened again for that species for 2 minutes; only then is it `lacking`. **Never a named
  animal, never a baby, never another species**; where the server's version doesn't name the baby
  flag (`metadataKeys`), she culls nothing at all. No animals or no chest, and she refuses
  (`McRanchNoneLines`, `McRanchNoChestLines`). In the parser it's a care verb with an animal word
  within four words after it, read **before the farm** (« occupe-toi des animaux de la ferme »).
  « garde » and « surveille » are not ranch verbs: guarding animals stays a patrol and must never
  start a cull. Animal words match exactly (« lapins » is one slip from « sapins »), and « élever »
  stays out (one slip from « enlever »).
- **Food and armour** need no order: she eats when hunger drops below 14 (never rotten flesh and
  the like), and wears the best piece she carries for each slot on spawn and after each pickup,
  and a shield in an empty off-hand (`wearBestArmor`). Both are our own few lines;
  `mineflayer-auto-eat` is ESM-only now and the armour plugin is unmaintained. Tiers go by
  `ArmorTiers`; **a tier missing from it still beats nothing** (copper armour, missing, was never put
  on). A piece someone gives her (an armour item picked up) gets the closest player's thanks
  (`armoured`, `McArmouredLines` and twins), but not her own things coming back after a death.
- **Armour on and off** (`Dress`, `Undress`, free orders answered from the body's event):
  « mets ton armure », « équipe-toi » put on the best she carries; « enlève ton armure », « retire
  ton armure » take it off **and keep it** in her pockets (`bare`), and she puts nothing on by
  herself until told to or **given a piece**, which is a gift and goes on (all of it). Each piece
  needs a free slot: with full pockets what's left stays on (`McUndressFullLines`). « donne-moi ton
  armure », « rends-moi ton armure » still hand it over, shield included (`wornGear`); so does
  « enlève ton armure et donne-la moi », which has another give verb. « enlève » and « retire » are
  give verbs too, so armour on or off is read just before handing over, and « mets » and « porte »
  need the armour word (« mets tout dans le coffre », « la porte »).
- **Hurt, she eats to heal** (`healTick`, on every health update and every 5 s): Minecraft heals her
  only while her food bar is at 18 or more, and quickly only with saturation left, so below full
  health she eats when the bar is under 18, or under 20 with no saturation left. Not with a monster
  within 10 blocks (a fight's retreat eats for her, and the food in her hand would be her sword), and
  not while she digs, draws her bow, stacks a block or fishes: the food in her hand would cut it off.
  With nothing to eat, 7 hearts or less and the bar under 18, she goes to find some (`startForage`).
- **With nothing to eat, she feeds herself first** (`startForage`, a body-only task with no player and
  **no words**, the owner's call). Only when she's free or at an activity (paused and gone back to,
  like an errand: `pausedActivity`); never during someone's order, and not while fishing, which feeds
  her anyway. Sometimes she picks the 3 closest ripe carrots, potatoes, beetroots or melons within
  24 blocks (replanted, like any harvest), sometimes she fishes for up to 2 minutes (quietly:
  `fishLoop` with `forage` reports no catch), whichever is around, then the other if the first gave
  nothing. If she's not starving (food above 6) she then cooks one kind of raw food (up to 8) in the
  closest furnace or smoker within 16 blocks that has nothing in it at all, with her own fuel, and
  takes back whatever is left. Until then she doesn't eat the raw catch. She eats the most filling
  thing she carries. Nothing found, and she asks as below, and doesn't look again for 5 minutes.
  A farm keeps 8 beetroots and 8 melon slices back for her as well as the seeds (the carrots and
  potatoes kept to replant are food too).
- **With nothing to eat and no way to find any, she asks.** Below 14 food with no edible item, the body reports `hungry`
  each time the bar drops a notch; the service decides: `McHungryLines` every 10 minutes at most,
  `McStarvingLines` every 5 once she's at 6 or less (Minecraft stops her sprinting there, so she
  can't run away). Each ask has its own gate, given back when the remark gates swallow the line.
  The next food she picks up after asking is someone feeding her: the closest player within 8
  blocks gets `McFedLines` (Papa and Tata their twins), said at once, and she eats.
- **Guard and patrol never hit a creeper.** Its blast would land on the player she guards. With a
  bow and arrows she shoots it from 6 blocks or more (our own few lines, aimed a little high for the
  drop, not `mineflayer-hawkeye`: three arrows for three hits at 8, 12 and 15 blocks on the local
  server). She sees one from 16 blocks (`CreeperSight`): it walks 10 in about 3 s, two shots, and it
  takes three. **Never through a player** (`playerInLine`): behind Papa she put four arrows in his
  back while it walked up to him, so with someone in the line she steps 3 blocks aside first, and
  if someone steps in while she draws, the arrow goes up. Without a bow she keeps out of its reach and warns once per creeper
  (`McCreeperLines`). Under 3 hearts she stops fighting and backs off, eating to heal, until she's
  at 7 hearts or 20 s have passed (`McRetreatLines`). The tick changes her goal only when what she's
  doing changes (`setMode`): a goal set on every tick is never walked.
- **Patrol** (« syncs surveille la base ») is guard around a spot: where she stands, or a remembered
  place named with it. Read before stay and guard: a care verb with a place word within four words
  after (`BaseWords`, « ici » included), never when the verb is followed by « moi » (« protège-moi à
  la maison » is guard). Like a farm, it lasts while its keeper is on the server.
- **Errands and activities.** Farming, fishing, keeping animals and patrolling are activities: they last until
  someone says otherwise. Handing something over (Give) and putting her things away (Store) are
  errands: they pause an activity, the body goes back to it after (a fresh copy of the task, so the
  old loop sees it's no longer hers and stops), and the service gives the claim back to the
  activity (`McJob.Resume`), without telling its keeper she left. A night's sleep pauses and resumes
  every activity the same way (`resumeAfterSleep`).
- **Give** (« vide tes poches », « donne-moi ton poisson ») tosses what she carries at the player's
  feet; armour includes what she wears. A gathering word only becomes Give with a possessive
  (« donne-moi ton fer »): « donne-moi du fer » stays the gathering order. Goods she can't fetch
  (`CarriedOnly`: food, fish, tools…) take any article. Give is read before Store, whose « ranger »
  is one slip from « manger ». **Store** (« range tes affaires », or anything put « dans le coffre »)
  fills the closest chest or barrel within 16 blocks and keeps her kit: the best sword, pickaxe, axe
  and shovel, a rod, shears, her bow and arrows.
- **Fishing** walks to open water within 16 blocks and casts until told otherwise. No bite in 45 s
  and she reels in and casts again; only then does she click to reel in, since after any other end
  a click would cast. The catch stays on her; a remark now and then (a treasure always).
- **Smelt** uses only a furnace someone else isn't using (nothing in its input or output, no other
  fuel), smokers for food and blast furnaces for ore too, with her own fuel. She waits by it, takes
  the result as it comes, and brings it back. Whether a furnace is free is only known by opening it,
  so « all busy » is the one refusal that comes after she has dropped her task.
- **Remembered places** (`minecraft-state.json`, up to 20 per player, by `McCommand.PlaceKey`) are
  where the player stood when they said « retiens ici : nom »; the body reports it (`mark`). **They are
  kept by player**: each has their own « maison ». A place named alone is looked up in the player's own
  map, then Papa's, Tata's and everyone else's by name, so the group's map still works for someone who
  saved nothing. « la maison de X », « d'X », « chez X » (X's home, whatever home word X used) name
  someone else's: X is anyone with a place or on the server, or « Papa » / « Tata ». The parser takes
  « de X » out and reads the rest as usual, keeping that reading only when the plain one is also about
  a place, or nothing (« va chercher du bois chez X » stays wood). Her lines show the name as typed
  (`PlaceName`) in guillemets, with whose it is when it isn't the asker's (« « maison » de Papa »).
  Anyone may add a place to anyone's map; only the player it belongs to, its saver or family may move
  or forget it. A known place is read anywhere after a going verb, before gathering (« va à la mine
  de fer » has « mine » and « fer »); an unknown name only after a preposition (« va à la X »), and
  the service says it doesn't know it. « rentre » is her home: the family's « maison », else the other
  home words. The list (« tes lieux ») is the asker's places, then the others they can reach. A file
  from before places were kept by player (one shared `Places` map) is split by saver on load.
  **Guide** walks ahead and waits when the player falls 12 blocks behind; 30 s out of sight and she
  gives up. A place in another dimension is refused (`otherDimension`).
- **What she carries** (« t'as quoi sur toi ? », « montre ton inventaire », « t'as combien de fer ? »,
  « t'as du bois ? »). The body reports her inventory on spawn and after each change, at most every 2 s
  (`inventory`): raw ids and counts, each stack tagged with the kinds of goods `give()` would hand over
  for it, so « t'as du fer ? » counts exactly what « donne-moi ton fer » would toss. The service keeps
  the last report and answers from it, sending nothing. The words are `McItems`, a hand-written table
  (singular, plural, gender: « une pioche en fer », « 23 pains »), since the game's names have neither;
  an item not in it is « un autre truc » in game and its id on Discord. Add an item when she starts
  handling a new one. `McInventory` builds the line, biggest first, within `ListMax`, the rest
  « et N autres trucs »; what she wears only counts when armour is asked. `/debug minecraft` →
  « Voir son inventaire » shows it all, with full counts.
  In the parser (`CarryQuestion`, read before stay and before handing over): « combien » is always a
  question, so is « t'as quoi » alone or followed by « sur toi », « dans… », « comme… », and an
  inventory word of *hers* (« ton inv », not « mes poches ») with a show verb or alone. A gathering,
  give or store verb anywhere makes it an order instead. « t'as du fer ? » asks, because iron is
  something she could be sent to fetch; « t'as du poisson ? » and « t'as quoi comme épée ? » stay
  requests to hand it over, as they always were, except armour (« t'as quoi comme armure ? » can't
  mean « take it off »).
- **Answered at once, taking her from nobody**: where she is (the body reports her position), what
  she carries, where a place is, the list, forgetting, saving, hopping, waving (any greeting, read last so « salut
  syncs viens » is come), compliments and thanks and **reminders** (« préviens-moi dans 5 min », « rappelle-moi dans 10 min
  de sortir le fer »). A timer is read first, as its note can hold any word; the length comes from
  the raw text (`Duration`), the note is what follows the first « de », « pour » or « que » once the
  length is out. Three per player, two hours at most, in memory, gone with the body. Sitting is
  stay, crouched.
- **She never digs and never places a block.** `canDig = false`, no scaffolding blocks, no
  pillaring: a path through someone's wall or a dirt tower in their build is not help. Where
  walking can't get there she says so (`McNoPathLines`) instead. **The exceptions are a seed
  on bare farmland where she just harvested** (`harvestBlocks`), **a gathered tree's natural
  leaves** (`gatheringMovements`), **the pillar she climbs a tree with, taken down after**
  (`fell`, `unstack`; an order that takes her away mid-tree leaves it standing, with a log line)
  **and her camp's own blocks and floor** (below): nothing else, nowhere else.
  Keep them that narrow.
- **The camp** (« monte le camp », `makeCamp`; « démonte le camp », `breakCamp`): Papa and Tata
  only (C#: `McCampRefusedLines` for anyone else), one at a time. An open camp round a campfire, as
  `CampLayout` draws it (nine strings, the back to the north; the owner's design,
  `docs/superpowers/specs/2026-10-22-camp-design.md`): the fire in a pit of four flat trapdoors, two
  benches (a log on its side and two bottom slabs) on two sides, the workstation (table, furnace,
  chest) on the third, a bed in the back-right corner, four torches on fence posts, each a cell
  clockwise from a corner, and a patchwork floor of path and plain dirt. Nothing square on purpose:
  keep it that way. On the closest site within 12 (`campFits`): one level, firm full ground and two
  blocks of room, **only under the fire, its pit, the benches, the workstation and the bed**; the
  torch posts and the floor follow the ground a block up or down (`campHeight`; `campHeights`,
  found when the site is chosen and kept on the task, since a fence set down reads as ground), and
  the floor may have no deeper drop (she stood in a two-deep hole in it to reach a block and never
  got out), and **nobody standing where a block goes** (centred on her, the fire came under the feet
  of the one who asked, beside her, and the server refused it). The cells left alone aren't checked. All of it at one level was rare outside a flat
  world: at a forest's edge, none in reach. Grass is built over (`CampOver`),
  nothing else. Everything crafted from what she carries, what's short gathered first (`campNeeds`:
  wood, then stone with a wooden pickaxe she makes, coal, else charcoal in the camp's own furnace;
  wood again while trees are in reach, up to six gatherings: a pass whose drops she missed brought
  nothing in, and stopping there left the camp a log short at a forest's edge);
  a wooden axe (taking it down faster) and a wooden shovel (the path) too. The trapdoors, slabs,
  fences and the benches' logs are of the wood she has most of when there's enough of it, else of
  another kind (`craftWooden`): a recipe takes one kind of planks, and with mixed logs one wood
  alone left a fence post short with planks to spare. **The bed** (`campBed`), best first:
  three wool of a colour; wool sheared on the spot (shears and a sheep, never one killed); a straw
  bed from 3 hay bales or 27 wheat **she already carries** (never fetched: the wheat near her is
  someone's field); else **no bed**: the camp goes up anyway (`bed: false`, `McCampNoBedLines`), it
  never refuses for want of wool. **Her 26.1 client has no straw bed**: ViaBackwards shows it to her
  as a `yellow_bed`, in hand and set down, so that's the name it's crafted, placed and taken down by,
  through her recipe book only (minecraft-data doesn't know the recipe). **She crafts through her
  recipe book** (`craftOne`, `bookCraft`): `craft_recipe_request` has the server fill the grid
  itself, one shift-click takes the result. Mineflayer's `bot.craft` places every ingredient by
  clicks the server checks one by one, and on 26.1 it kept failing whatever the delays (the first
  table craft of a mixed recipe always; with a count it made one and put her inventory out of step
  with the server's). The book (`recipeBook`, from `recipe_book_add` at login and as recipes unlock)
  is listened to from the moment she connects; a recipe not in it yet falls back to `bot.craft`.
  Recipes unlock through advancements, so a test that takes her recipes must revoke her
  advancements too, or the book stays empty for good (`camp.js` does both). Each craft counts only
  once the item is in her pockets, and is tried again otherwise. **Placing** (`campPlace`): from a
  given stand for the bed (laid the way she looks, head to the back) and the workstation (it turns
  to face her, so it's set from the fire's side), from wherever she reaches otherwise
  (`GoalPlaceBlock`). A log lies along the face it's set against, so a bench's slabs go first and its
  log against the end slab's side (`against`); a trapdoor and a slab set on the ground's top come
  out flat and bottom-half. Two rounds: what the server refused (a sheep in its cell) is tried again
  once the rest is down. **The floor** (`campFloor`, `campTread`), last: a pattern drawn per camp
  (about 60% path, the way from the fire to the entrance always path), kept on the task; a cell
  with a flower on it is left as it is. **Nothing is dug**: a path cell with the shovel on the bare
  top (short grass dug first); a plain dirt cell the same, then a block set on it (`CampFiller`: the
  game turns a covered path back to dirt) and taken back. Dug out and its dirt set back instead,
  the drop popped onto the table out of reach, and a dip's hole was two deep (she stepped in for the
  dirt and stayed down there). The block on a path is in `placed` while it's there (a fight in
  between, it comes down with the camp); a cell someone stands on stays a path; she picks up only
  her own block (anything else lying about may be someone's). A cell found dug out is filled
  (earth, else cobblestone, else planks). Only grass-family ground is touched. Plain dirt grows back to grass within
  minutes (the game's doing). **The floor is never in `placed`** and stays when the camp comes down.
  The blocks she placed go to C# (`camped`: `complete` is every block but the bed, `bed` the bed;
  also a half-built camp's `cantCamp`), kept in the state file (`McCamp`) and sent back to take it
  down: only those, still what she placed: the torches first (they'd drop off their posts), then
  the nearest from where she stands (`campDig`, collectblock for what's out of reach), then
  everything that fell, the chest's contents with it. She keeps it all; a campfire broken without
  silk touch gives two charcoal, not itself (the game's rule). **A camp gives way to a fight or a
  creeper** like any restartable order (`Restartable`), and is taken up again at the same site with
  what's already on it and the same floor (`resumeFor` carries `site`, `placed`, `floor` and
  `heights`; `campNeeds(placed)` counts what's set down as done, `placed` cut down first to what still stands:
  a creeper may have blown some away); a floor cell left dug is filled first, and any hole once more
  at the end. **Nothing escapes `makeCamp`**: a throw ends it as a failed craft (`cantCamp` with
  the blocks placed), or she'd hang on a dead task and C# would never know what to take down. A chest is in the pathfinder's `blocksCantBreak`;
  `gatheringMovements` lifts that for a chest among its targets. `tools/mc-pathtest/camp.js` builds
  one and checks every cell, takes it down, and has `interrupt`, `nobed`, `straw`, `holes`,
  `charcoal`, `mixed` and `uneven` modes.
- **Doors.** The pathfinder sees every door as a wall (its own `canOpenDoors` is off, flagged flaky
  by its authors, and covers fence gates only), so `gentleMovements` wraps its `getBlock`: a wooden
  or copper door or a fence gate is passable whatever its state, except on a diagonal step (a corner
  catches the panel). **« Open » doesn't mean passable**: a door is a thin panel on one edge of its
  block, and a click swings it to the next edge, so a door left open across a corridor is a wall
  and a closed one can be walked past along its panel. `doorTick` looks at her next steps
  (`path_update`, `stepsAhead`) and, within 2.5 blocks, clicks a door whose panel stands on the edge
  she's about to cross (`panelSide`, from its collision box), whether it's open or closed; a fence
  gate is a bar across its block when closed, so a closed one is clicked open. **Path points are
  block centres (x.5)**: `stepsAhead` compares them as blocks, or a point in the door's own row reads
  as beside it, an open door « blocks » the next step, and she clicks it shut in her own face.
  **The pathfinder lifts a point onto whatever stands in its block** once planned (1.5 onto a closed
  gate's bar, 1 onto a door's panel), so `setPath` puts a point standing on a gate or a door's lower
  half back down, centred (`floorDoorNode`): otherwise she can't reach it, and `doorTick` looks a
  block too high and never opens the gate. Once she's through (out of the doorway on the far side
  from where she clicked it, or 3 blocks off either way) and nobody (a player, an animal) stands
  within 2 blocks, she puts every door she moved **back as she found it** (`movedDoors`, by state
  id): the way someone set a door is theirs. A door the server
  ignored twice (spawn protection, a plugin) is a wall again for 2 minutes, and the pathfinder's own
  stuck check re-plans around it. Never iron doors (redstone) nor trapdoors (as often floors as
  doors). Clicking a door is using it, not placing anything: the never-builds rule stands. Checked
  on the local server (*Testing movement locally*): doors closed and open, both hinges, both ways, a
  door set across the wall, and a fence gate, each crossed and left as found.
- **Ladders** are the pathfinder's own: it climbs up and down them, a 10-rung shaft included, and
  she follows someone up and down one (checked on the local server). **From the side**
  (`climbFromTheFoot`): the pathfinder lifts a climb's first node to the top of the ladder's plate,
  which works walking into the plate; walking along the wall into the ladder's block, she wasn't,
  the step couldn't be simulated, and it let go of every key and planned the same again, stuck
  beside the ladder for good. Each `path_update` gets back the step into the ladder's foot (its
  centre: at the plate her box overlapped it) before such a node, in place, since the pathfinder
  walks the array it hands out. Barring side entry instead would close a corridor with a ladder on
  its wall (`ladder-passing`).
- **Trapdoors** the pathfinder takes for full blocks; `gentleMovements` corrects it. An open one is
  a panel on one edge: passable along that edge, a wall when it stands across her way (`panelSide`),
  and a route point lifted onto its panel comes back down like a door's. A closed one in the top half
  of the block at head height leaves 1.8125 under it, and she's 1.8: she walks under it when the
  floor below is a full block or lower. She never clicks a trapdoor.
- **Fields.** Walking never tramples farmland; landing on it from over half a block does, crop and
  all. So a step onto farmland costs 4 more (`FarmlandStep`: she walks round a field when it's not
  far), a field's water hole costs 40 (she swam back out of it with a push and landed on the
  farmland beside it), a drop onto farmland costs 40 (a block's fall tramples it one time in two),
  no jump over a gap takes off from or lands on farmland (`getNeighbors` filtered), and over
  farmland she doesn't jump unless she's climbing (`farmlandTick`, which also clears Mineflayer's
  queued jump: letting go of jump doesn't take a pressed one back). She stands *inside* a farmland
  block (15/16 high): « the block under her » is the one at her feet minus 0.1. Harvesting and
  hops included: no hop over a field. Checked on the local server: none trampled in two dozen
  harvests and field crossings, against one crossing in three before.
- **Everything the pathfinder calls into runs in the physics tick**: `getBlock`, `safeToBreak`, the
  step costs, `getNeighbors`. A throw there takes the whole body down (`safeToBreak` did, on a block
  in a chunk that isn't loaded, which comes as a stub with no position). Guard for the stub, and
  keep the step costs cheap (state ids, not `blockAt`): a slow search hits the pathfinder's 5 s
  limit, and a goal out of reach reads as « still looking » instead of « no way ».
- **She can only reach a player the server shows her** (render distance). Beyond that the body
  answers `cantReach notVisible` at once (`McTooFarLines`). A followed player who goes out of
  sight ends the follow (`lost`).
- **Through a portal after them** (`crossAfter`, `portalTick`). A followed or guarded player who
  vanishes by a nether portal, within 24 blocks of her and still online, went through it: she walks
  into that portal and stands in it until it takes her, steps out on the other side, and goes on
  with the same order as soon as she sees them (15 s at most, else `lost`), without a second
  acknowledgement (`follow`/`guard` `quiet`). Back the same way. `crossing` gets `McPortalLines`
  (family twins), once every 3 minutes, and only once they're still listed half a second on: the
  server drops a leaving player's entity before their name, and a logout by a portal is no crossing
  (checked every tick until she's through; the portal takes 4 s, so she never goes alone). **A
  portal is never walked into otherwise**: `gentleMovements` avoids it, and only the crossing's own
  movements allow it. A death mid-crossing ends it. Checked both ways and behind lag by
  `wild.js portal`.
- **An order that fails at once changes nothing.** The body checks an order can be done (in sight,
  not too far, something to gather, a bed) *before* `stay()` drops the current one, so
  « syncs va chercher des diamants » with none around never pulls her off someone's follow. Keep
  that order in any new op.
- **Orders are acknowledged when the body reports them under way** (`moving`, `collecting`,
  `sleeping`), not when they're read. « J'arrive » followed by « je te vois pas » reads as a broken
  promise. Stay is acknowledged straight away.
- **Whispered orders get whispered answers**, outcome included. The service remembers who gave the
  current order and how. In every line Papa and Tata are « Papa » and « Tata » (`Who`), never their
  in-game names.
- **She works for one player at a time** (`McClaim`, pure, walked by the checks). Whoever gave the
  order under way holds her: only they can stop her or send her elsewhere, and anyone else gets
  `McBusyLines` (once every 10 seconds per player). Family outranks: Papa > Tata > everyone, so
  family can always take her and nobody takes her from them; whoever she leaves mid-order hears why
  (`McTakenLines`). A claim runs out **10 minutes** after its order got under way, Papa's included,
  so a forgotten « suis-moi » never locks her for the evening; family can still take her back.
  The service keeps the order under way (`_order`) apart from the one just sent (`_pending`): the
  pending one only becomes hers when the body confirms it, so a failed order leaves the claim where
  it was. Going to bed (asked, or because a player did) ends any claim.
- **Orders aren't rationed like remarks.** One gate per player ignores a second order within the
  same second (a double send), never a stop, so « viens » then « stop » always works. Another gate
  gives each player her « oui ? » once every 10 seconds at most (the window still opens). Only their
  acks are held back, when the same order comes again (*Her remarks*).

## Free time

Between orders she has her own life. **Own tasks** (`own: true`, no player) are body-only, like
eating: they hold no claim, report no order events, and any order ends one through `stay()`. She
counts as free during one (`isFree`), so she can still defend family, forage and go to bed. Running
away or defending someone never goes back to an own task: autonomy picks again by itself.

`autonomyTick` (once a second) starts nothing while she's asleep, eating, sitting, busy with an
order, **held** or in the **grace**:

- « reste » and « assis » hold her for **10 minutes**: no routine, tagging along or retrieval (body
  language still runs). Any other order that gets under way lets her go.
- Once an order is over she waits **45 s** before starting anything, so « viens » doesn't end with
  her walking off at once.

Then, in this order: getting her things back, the routine, tagging along, nothing. **What she
starts on her own gets a rare rolled remark** (the owner's call): the routine 25 %, tagging along
20 %. Getting her things back always gets one.

- **Routine (named places).** `McRoutine.Classify` (pure, in the checks) finds among the remembered
  places (Papa's first, then Tata's, then anyone's, kind by kind) a pen (an animal-pen word anywhere in the name: « ferme aux animaux »), a farm or a fishing
  spot (a farm or water word first: « champ de blé », « étang »), and home (`McRoutine.HomeKeys`,
  shared with « rentre »). The service sends them (`routine`) on spawn and after every save, move or
  forget. By day (time 0–11499) she works in turns, taking the places in turn, **9 minutes in the fields, 6 fishing,
  3 at the pen** (`WorkBlock`, the owner's call: a pen needs a few minutes now and then, the fields waited), each
  started with the order's own checks (crops and a chest, water and a rod, animals and a chest); her
  fishing is quiet (no catch remarks). At dusk (11500–12541) she goes home and stays there till
  morning; the night is `autoSleepTick`'s. A spot in another dimension or over **256 blocks** away is
  left out (her own trips are far shorter than an order's 1500); one that fails (no path, not reached
  in 3 minutes, nothing to do there) is left out for 10 minutes, silently, with a log line. A
  player's order always replaces her own activity, and then holds her under `McClaim` as usual. Her
  activities pause for errands and sleep like a player's.
- **Tagging along.** With nothing in the routine, she keeps near Papa, else Tata, when one is in
  sight: over 8 blocks away she walks up to 4, **without sprinting** unless they're over 16 off (her
  own `Movements`; `stay()` puts the usual one back), and every 15–40 s she takes a few slow steps
  around them. They go out of sight, and it ends silently. Family only: the twins are the only pools.
- **Getting her things back.** On death the body notes where, in which dimension, and whether there
  was lava; 3 s after respawning she goes back (`retrieve`, ahead of the routine) unless they're in
  another dimension, over 500 blocks off, in the void or in lava (`retrieveLost`). At the spot she
  sweeps the items within 8 blocks for 20 s (`RetrieveRadius`: a death scatters them up to 6 off,
  and they roll); her armour goes back on by itself. An order comes first;
  the walk waits until **5 minutes after the death**, when the items despawn. Still carrying things
  after respawning means the server keeps inventories: she doesn't go. Lines: `McRetrieveLines`
  (said at once), `McRetrieveEmptyLines` when nothing was left (always), `McRetrievedLines` and
  `McRetrieveLostLines` half the time.
- **Loot.** Where something she was fighting or culling dies is a loot spot for 30 s. Guarding a
  player or a place, between fights, she walks onto the items within 3 blocks of a loot spot, within
  6 of her and inside what she guards; one she can't reach is left. The ranch picks up its own drops.
  **She never picks up anything else**: a player's dropped items stay where they are.
- **Creeper warnings.** A creeper within 4 blocks of a player (within 32 of her) and behind them
  (over 100° off where they look) gets a warning, once per creeper; the player she guards is
  `guardTick`'s. One per player every 15 s (`_creeperGate`), said at once (`force`): a warning can't
  wait. `McCreeperBehindLines` has family twins.
- **Needs.** Every 10 s the body checks her kit (best sword, pickaxe, axe and shovel, rod, shears,
  bow): a tool at **10 %** durability or less is reported once (`wornOut`), again only for a new one;
  one reported worn that disappears without being handed over, stored or lost in a death `broke`.
  She also reports what she lacks (`lacking`): a rod for her own fishing, shears for sheep, food for
  the animals, arrows when guarding with a bow, room in her pockets. Wearing no armour is not a lack
  (the owner's call): she never asks for any. The service says each once every 10 minutes at most (`_needGate`, given back when the
  remark gates swallow the line). They're about the game, so the pools are shared.

## Water

The pathfinder swims only on the surface: in water it holds jump while it walks her, never plans a
step down under it, and once it stops nothing keeps her up. Mineflayer doesn't either. So the body
has its own swimming (`waterTick`, every physics tick), **registered on spawn**, after the
pathfinder's own tick listener (its plugin is injected after `attach()` runs), so that what
`waterTick` sets on the controls wins. In this order:

- **Air.** With her head under water and `bot.oxygenLevel` at 8 of 20 or less, she swims up
  whatever she's doing (`swim.mode = 'surface'`): straight up when it's open to the sky, else
  towards the closest bit of surface within 8 blocks (`surfaceSpot`, under ice or a ceiling). It
  only takes the controls and leaves the pathfinder's goal alone. Air at 0 is `drowning` (said at
  once), and her head out again afterwards is `breathing` (rolled).
- **Diving.** Following someone, or tagging along with them, when their head is under water within
  24 blocks and she's in the water with nearly full air, she dives (`swim.mode = 'dive'`). The
  follow goal is dropped, or it would fight her steering and report no path. She steers towards
  their eyes, rising when they're above her and sinking when they're below, and stops beside them.
  She comes up when her air is down to 7 (`surfacing`, unless they came up too), when they come up
  or get out of reach, when she's badly hurt (then no more diving for a minute), or in a column
  pulling down. Once her head is out, the follow goal comes back, and she goes down again once
  she's breathed back to 19. `diving` is reported once per order. Following someone under water
  never reports `cantReach noPath`, because the dive is the answer to that.
- **Treading.** In water, off the bottom or with her head under, and the pathfinder not moving
  her: jump held, so she stays at the surface (waiting beside someone, told to stay, between casts).
  Standing in shallow water she stays put. In Mineflayer crouching doesn't make her sink, so
  « assis » in water is treading crouched. A current still carries her.
- **Shaking off.** Out of the water onto dry land after at least 3 s in it, she crouches three
  times quickly, at most once a minute (`shook`, rolled). Not in a fight, on the run, sitting,
  eating or greeting.

**Boats.** Following someone, or tagging along with them, who sits in a boat or raft with a free
seat (two seats, one on a chest boat), she climbs in within 5 blocks of its centre (`BoardReach`:
the server takes an interaction within reach plus 3 of the boat's box, and at 3 she stood on the bank
by a boat a block out, never getting in; `boardTick`; tagging along, she first walks right up to it).
**Mineflayer only updates the passengers a vehicle still has**: whoever got out kept pointing at it,
so she stayed in a boat Papa had left, and once out she still « rode » it with her physics off. A
`set_passengers` listener takes out whoever the new list leaves out, her included. **Out of a boat on
the water the server puts her on top of it**, and Mineflayer's physics knows no entity to stand on:
she fell into it on her side, the server put her back, and she never walked off. `offBoatTick` stands
her on its top on her side too (0.5625 above it) and hops her off, away from its centre towards
whoever she's with, until she's clear of its box. Three refusals and she leaves that boat alone for 30 s.
They steer and she's the passenger, so she still places nothing. **Mineflayer stops her physics
while she rides**, so no physics tick runs then: the ride is watched by a timer (`rideTick`), and
she gets out when they do, when the boat goes, or when her task changes. Any order gets her out
(`stay()`), as it gets her out of bed. Mineflayer's `dismount` sends jump, which no longer gets a
passenger out since 1.21.2 (shift does), so `leaveBoat` sends the shift input itself. While she
rides, her own position isn't updated: « t'es où ? » answers with the boat's.

**Out of reach.** Guarding or defending, she leaves alone a monster whose head is under water (a
drowned at the bottom), since she can't reach it there. A bubble column over magma (`drag`) pulls
whoever swims in it down faster than jump lifts them, and the pathfinder takes it for air, so
`gentleMovements` makes it a wall and her own swimming avoids it.

Lines: `McDiveLines` (with twins), `McDiveAirLines`, `McDrowningLines`, `McBreathingLines`,
`McBoardLines` (with twins), `McShakeLines`.

## Body language

Body only, no words. Her gaze is free when she isn't asleep, eating, drawing her bow or walking, and
has nothing to do, is home, is tagging along, **is following someone, or guarding them between
fights** (home mode, no foe, no retreat), or is between rounds at her own farm or pen (fishing,
fights and furnaces keep it). Then she looks **the way a person would, never a fixed stare**: at
whoever chatted in the last 8 s within 12 blocks (no glancing away while they talk), else at the one
she follows or guards, else at the closest player within 8, for 2.5–7 s (1.5–4 on guard: she's
watching for monsters), then away for 0.7–2.5 s, then back. A glance goes to another player within
12, a creature in sight, or a direction off to one side, now and then down at her feet or up. With
nobody around she looks somewhere new every 4–9 s, mostly near where she already looks. Every turn
is smooth (no `force`). **Walking, none of this runs**: Mineflayer walks where she looks, and the
pathfinder turns her towards the next step every tick, so a look elsewhere would steer her off her
path. `node run.js players gaze` checks it on the local server.
**For 10 s after a player hits her she turns her back on them**, Papa included: she's hurt. When
Papa or Tata walks up (back within 5 blocks after being out of sight or over 10 off) she looks at
them and crouches twice, at most once every 2 minutes each; sitting, she stands up and back down.
Not while fighting, running, defending, asleep or eating.

## Arriving

Two seconds after her hello she walks up to **Papa, or else Tata**, when either is on (`seek`), and
she's not already busy with an order. In sight, she goes straight to them. Out of sight, the server
gives her no way to know where they are (positions only reach a client within render distance; no op,
no map plugin), so she goes to **where she last saw them**: the body reports `seen` each time a
player leaves her sight, including logging out next to her, and when she quits. Players log back in
where they logged out, so that spot is a good guess. The service keeps only the family's spots, in
`minecraft-state.json`, with the dimension: a spot in another dimension, or over 1500 blocks, is not
tried. If they show up on her way she heads for them. Found: `McSeekMetLines…`; not found, or no
spot known: `McSeekMissLines…` (« t'es où ? »). Any order replaces the walk; it is never a claim.

Player chat reaches the body as Mineflayer's `chat` and `whisper` events. A chat plugin that
reformats messages can hide them from Mineflayer's parser; if orders go unanswered on a server
with one, that's the first thing to check.

## Talk

Chat that isn't an order is read the way Discord reads it (`McTalkReader`, pure, with cases in the
checks; Discord's side is `personality.md`, *Listening*), so someone talking to her gets an answer,
not silence. **An answer to someone is not a remark** (the owner's call): no remark gate, no
budget. Only she, speaking on her own, is rationed.

- **When.** McCommand found no order (`None`, or `Unknown` for a call) and the message names her,
  or it's a whisper (always for her), or it comes **within 30 s of her answering that player**:
  every answer opens the window again, so a conversation goes on without her name. A message she
  lets go (« mdr t'as vu ça », to someone else) doesn't close it (`_answeredAt`); only the order
  reading is one message long (`_calledAt`), so « viens » said to a friend a bit later stays theirs. A refused
  order (« syncs viens pas », `McRequest.Refused`) is never talk. Her in-game name is turned into
  « syncs » before reading (`HerNameAsSyncs`), since the cues know only that one.
- **The ladder**, Discord's, first match wins: « Inabot », a shutdown threat, a mean reading
  (`Comebacks`; Papa `OwnerMeanReplies`), a specific intent with Papa's and Tata's twins
  (`OwnerIntentLines`, `TataIntentLines`), Papa's pet names, a kind word (`NiceReplies`), a greeting,
  news (`Sharing`), a question or a low-effort word, Tata's warmth roll, then a comeback
  (`Comebacks`, Papa `OwnerComebacks`). A bare call, or a call with nothing read, gets « oui ? » as
  before.
- **Without her name, right after she answered**, only what is plainly for her answers: a specific
  intent or Papa's pet name. An insult, a « ok » or a question there may be for someone else, and
  gets silence.
- **Talked about, not to** (« syncs a pris mon fer », « je joue avec syncs »: `AddressesHer` is
  false) is her own initiative: a remark (`talkedAbout`, half the time, then the gates and the
  budget), from `TalkedAboutLines`, `BackbiteRoasts` when it's mean.
- **What only Discord has is left out**: her records (fact lines, « tu m'aimes ? » from feedback,
  the next session, someone's level or cailloux), a ship, a blame and a side taken (Discord people),
  « chut » (the hush is per Discord channel), the help list (none in game: « aide-moi » is a call),
  and the pointers to slash commands. Dice and « note-moi » work in game as on Discord. A favour she has no order for (`Request`) gets `McCantDoLines`, the
  one game-only pool here. An opinion is /rate's verdict, « moi » being the player
  (`McTalkReader.AskerId`: Papa and Tata by their Discord ids, anyone else by a stable hash of the
  name).
- **The conversation streak** is Discord's (`ConversationStreak`, its own instance, per player):
  the third message answered by a comeback becomes `StreakNudgeLines`, the fifth ends it
  (`StreakEndLines`) and she says nothing to them for 10 minutes. Never Papa.

**The pools are Discord's, made fit for the game by `McChat.Game`**, computed once per pool: no
sticker tag, no custom emote, no ✨ or bold, no face the font can't draw at the end (« (>⩊<) »,
« ദ്ദി◝ ⩊ ◜.ᐟ », stripped, the line kept). A line that needs Discord to make sense is left out:
backticks or a slash command, Discord words (`McChat.DiscordWords`: « tag », « salon », « vocal »,
« emote », her Discord XP, cailloux and ranking…; « serveur », « message », « sondage » and « session »
stay), what is only true off the game (`McChat.NotInGamePhrases`: « good bot », and her having no
arms, legs or back, or never moving, which in game is false), anything undrawable, over
256 characters. Faces are thinned to one line in four, the in-game rate. Whispered, only the lines
under 200 characters (the body cuts a whisper there). **A Discord line edited for Discord is edited
for the game too**: run `dotnet run --project tests/ProjectSYNCS.Checks -- --mc-review` to see what
the game leaves out (« - », with the reason) and every line as she says it in game (« + »). The
checks fail when a pool she uses in game ends up empty, or a kept line isn't plain game chat.

## `/minecraft`

Her Minecraft life seen from Discord (`MinecraftModule`, design in
`docs/superpowers/specs/2026-10-21-minecraft-command-design.md`). Seven subcommands, all answers
ephemeral. Orders, chat lines and reminders sent into the game from Discord were considered and
dropped by the owner: the only thing Discord does in game is `invite`.

- **`help`** is `McHelp.Sections`, two pages (`minecraft:help:N`, `MinecraftHelpHandler`). **Every
  example is parsed by the checks** and must read as the order it shows (talk examples, as talk): a
  change to `McCommand` that breaks one breaks the checks, so fix the phrasing or the parser, never
  drop the check. A new order gets an example here.
- **`status`** (`PublicStatusText`) shows nothing of the owner's (address, kick reasons, the allow
  switch: `/debug`), and what she's doing through `McDoing.ForStatus` from the order under way; a
  whispered order keeps its place or coordinates private.
  **`inventory`** is `InventoryText`, the owner's view, which holds nothing private. **`where`** sends
  the body a `where` with the asker `DiscordAsker` (U+0001, which no player name can hold), so the
  `position` answer goes back to Discord, not to a player; one question at a time, shared by
  simultaneous callers, 3 s at most.
- **`link`** (`MinecraftLink`, by member, unique on `PlayerKey`, the name in lower case): the member
  gets a 4-digit code (`McLinkCodes`, in memory, 10 minutes, one live code per member) and whispers
  « lien 1234 » to her from that name. A whispered code is never talk: a wrong one gets silence; a
  right one whose save fails (taken meanwhile, or the database refused) gets `McLinkFailedLines`, since
  the code is spent.
  **Papa's and Tata's names only link to their own Discord ids** (offline mode lets anyone log in
  under any name), and **a name linked to someone else is refused**: the holder frees it with
  `remove`, the owner frees anyone's with `remove` and `player`. A link never makes anyone family:
  `Minecraft:Owner` and `Minecraft:Tata` still decide that in game.
- **`invite`** (linked members only, `McInvite.Decide`): refused when she isn't allowed, during the
  owner's 30-minute leave hold, when she's in, when the server didn't answer its last ping, or while
  another invite runs. Otherwise she joins and
  the presence tick wants her in for `McInvite.Length` (an hour) as long as the server answers;
  forbidding her or a forced leave ends it. Her arrival line is then `McInvitedLines` (who invited
  her, `McInvite.Inviter`: only while the invite runs, so an invite whose body never spawned credits
  nobody later) instead of `McArrivalLines`.
- **`stats`** (`MinecraftStat`, totals by key, `McStats`: **stored keys, append-only**): counted in
  memory (`McTally`) from the body's events (her deaths, fish and treasures, guard kills, gathered
  blocks by resource, harvests: an order's `harvested` and every farm round's `farmed`, ordered or her
  own routine, both as items gained; orders confirmed under way, total and per player) and her minutes in
  game, written every 5 minutes by the loop and on a clean stop; a failed write puts the counts back.
  The command adds what's still in memory, so it's current. Totals only, no daily buckets: nothing
  ranks them by date.

## Failures

An unplanned end (crash, kick, refused login, unsupported version) backs off 2, then 5, then
15 minutes. Spawning resets the count. The last kick reason shows in `/debug minecraft`. The tick
catches everything; nothing escapes the loop.

`/debug minecraft leave` keeps her out for 30 minutes, or the next tick would send her back.
`join` clears the backoff and the hold. Once she's in, the rule applies as usual.

## Her remarks

- One per kind every 2 minutes, and never two within 20 seconds. Two separate `CooldownGate`s,
  owned by the service.
- **And at most 4 in any 10 minutes** (`RemarkBudget`), the owner's call: she talks less than she
  did. A forced remark is neither held back nor counted. A remark the budget swallows gives both
  gates back, like one the gates swallow, so the callers that release their own gate still do.
- **The commonest remarks are rolled on top**, so the chat stays the players': night falls (25 %),
  advancements (30 %), an ordinary death (30 %; hers, a kill by a player and family's always get a
  word), a first hit from anyone but family (60 %, rare enough to keep its odds; the escalation is
  never rolled), a guard kill (10 %: kills come in waves) and a catch (15 %, junk 25 %, a treasure
  always). So are the answers whose gesture already says it: arriving where she was called (25 %)
  and the hop (50 %). Refusals and failures are never rolled: they're how a player knows she heard.
- **Order acks are said once every 3 minutes for the same order** from the same player (`Ack`,
  keyed by the order plus what makes it another one: the place, the coordinates, the resource, the
  crop). « suis-moi » sent again gets her following, not the same words. Refusals, outcomes, « je
  suis avec X » and « je te vois pas » are never held back.
- **A burst gets words once** (`_burstGate`, per kind, across players, 30 s): a hello, a goodbye, a
  « gg », a « merci » or a bare call from several players at once. The first player gets the line;
  the others only the gesture (the wave, the hop; a wave for a thank-you or a call, whose window
  still opens). **Papa and Tata always get their words** (`FirstOfBurst`, the owner's call), and
  still start the burst, so whoever follows them gets the gesture.
- **Lines come from small pools** (most have 2 to 9), heard by the same few players in one evening,
  so every pick in game is `ResponsePicker.Pick(pool, LineChoices)`: among the 2 lines said least
  recently, not among the half of the pool not said lately. A line comes back only once the pool has
  nearly gone round. A new send path uses it too.
- **Who hit her is known for sure** since 1.19.4: the server's `damage_event` names the cause (the
  shooter, for an arrow), which Mineflayer passes as `entityHurt`'s second argument. A mob, a fall
  or fire is no player, and she says nothing. On older versions there's no source, and the body
  falls back to guessing the closest player within reach (`exactHits`).
- **Repeated hits escalate** (`McHitStreaks`, pure, walked by the checks). The same player hitting
  her three times with no more than a minute between two hits is someone trying to kill her: that
  third hit gets `McHitAgainLines` (angry and scared at once) **at once, past the remark gates**
  (`Remark(…, force: true)`, which still starts both cooldowns). Further hits in the streak keep that
  pool, rationed like any remark. Streaks are per player and start over after a minute's pause, or
  when she leaves. Papa's twin is scared and hurt, never angry; Tata's is panicked bargaining.
- **And she runs** (`flee`): from the escalating hit on, the service sends `flee` with the attacker,
  **never for Papa**, the owner's call: she stays and takes it, scared. The body sprints away
  (`GoalInvert(GoalFollow(attacker, 20))`) until she's 20 blocks off, out of sight, or 15 seconds
  have passed (a new hit while running extends it), then reports `safe` (`McFleeSafeLines`, a
  rationed remark). Still no digging or building: with no way out she's `cornered`
  (`McCorneredLines`, said at once). An activity (`resumeActivity`) or a follow or guard is picked
  up again after; a one-off order is dropped and its player told why (`dropped` →
  `McFleeDroppedLines`), unless they're the attacker. She sprints only while her food bar is above
  6, so she runs better when she has something to eat.
- **She defends whoever a monster hurts near her, and Papa and Tata against players.** The same
  `damage_event` tells her who hurts whom, as long as they're in her sight. She goes when free, during
  an activity, following or guarding anyone, or busy with an order she can start again
  (`Restartable`: collect, harvest, come, goto, guide), and goes back to it after (`resumeFor`,
  `resumeAfterBreak`, shared with running away). Gathering and harvesting look around where she
  stands, so she first walks back to where she stopped (`back`, 20 s at most), then the same
  command runs again from the pockets as they were (`since`): what she cut before the fight still
  counts and is handed over, with no second acknowledgement. Only what's over in seconds or can't be
  left keeps her (a furnace, handing over, storing, a bed, a portal, running away). She never hits
  family, nor a creeper.
  - **A monster** hitting any player within 16 blocks of her: she fights it, for up to 20 s,
    silently. Body-only, like eating.
  - **A player** hitting one of them is the owner's three steps, by that player's streak
    (`McHitStreaks.Tally`, same one-minute window, `DefendStage`, walked by the checks): the first
    hit gets a warning (`McFamilyHitLines`, rationed), the second within the window she hits them
    back once (`McFamilySwatLines`, said at once), the third and after she fights them for 15 s
    after each hit (`McFamilyFightLines`, the start said at once). Busy with someone's order, the
    body answers `cantDefend` and it stays words. While she's fighting someone, their hits on her
    don't count as hits: no remark, no running away (`_defending`, 30 s). Below 3 hearts she breaks
    off and eats. PvP must be on for her hits to land.
- In-game lines are **plain text under 256 characters**: no custom emotes, no sticker tags (the
  checks enforce it).
- **Kaomoji in game must be drawable by Minecraft's font**, which covers the Basic Multilingual
  Plane through Unifont, one code point at a time. So: nothing past U+FFFF (most emoji, `👁`), no
  combining marks (`(•̀ᴗ•́)`, `( ͡° ͜ʖ ͡°)`: the accents land beside the face), no format characters,
  no `§` (colour codes), no Discord markup (`<:`, `<a:`, `<@`, `<#`, `<t:`, which would show raw;
  a plain `<` like « <3 » is fine), and no script that needs shaping
  (Malayalam's `ദ്ദി`). The checks reject the first four. Katakana, box drawing, `ω`, `ಠ`, `✧`,
  `✿` all draw fine. The in-game set (`syncs-voice.md`) is wider than Discord's, but used
  sparingly, the owner's call: **about one line in four** at most, none in most pools of two or
  three lines; dry lines and Papa hurting her stay bare.
- She posts nothing on Discord when she joins or leaves the server: it was noise, so keep it that way.

**Family in game.** `McFamily.Of` matches a player against `Minecraft:Owner` (Papa) and
`Minecraft:Tata` without case, and the service picks a twin pool (`…Owner`, `…Tata`) for the
lines aimed at one person: the « oui ? », their death, them hitting her, their advancement, and the
acks for come, follow, guard and a delivery. Twins call them « Papa » and « Tata », never `{0}`
(the checks enforce it). Same tiers as on Discord (`personality.md`, *Favouritism*): Papa is never
roasted and, when he hits her, she's hurt and forgives; Tata gets cheeky warmth and the mute
rights. Every other pool is shared, because it's about the game rather than the person. A new
person-aimed pool gets its twins too, and its name goes in the checks' `McFamilyPools`. Offline
mode lets anyone take those names, which is fine: names only pick lines here.

## Deployment

The Dockerfile has a `node:22-bookworm-slim` stage that runs `npm ci` from the lockfile. The final
image gets the `node` binary and `/app/body`. The service looks for the body at
`<app>/body/index.js`, then at `MinecraftBody/index.js` under the working directory (a dev
`dotnet run`), or at `Minecraft:BodyScript`.

**The server's version.** Mineflayer lags each Minecraft release by weeks. 26.3 came out while
4.39.0 stopped at 26.1, with PrismarineJS's 26.3 work still in open pull requests. So the body pings
first (`pickVersion`). It speaks the server's version when `minecraft-protocol` supports it, and
otherwise the newest version it does support, reporting a `version` event with `fallback: true`
that `/debug minecraft` shows. A newer server accepts that older client only with
**ViaBackwards** (with ViaVersion) installed, a Paper/Spigot plugin or a Fabric mod. Without it,
the server kicks her for an outdated client and she backs off. When Mineflayer catches up, bump
`mineflayer`, `minecraft-protocol` and `minecraft-data` together in `package.json` (exact pins) and
the fallback goes quiet by itself. Mods that add blocks or entities can break the body; plugins that
only add commands can't.

**`tick_end` after every move (PrismarineJS/mineflayer#4189).** Since 26.3 the server accepts one
position packet per client tick and resets that count only on the client's `tick_end`, which real
clients have sent after every tick since 1.21.2. Mineflayer never sends it, so a 26.3 server
(ViaBackwards or not) kicks a stock bot for `invalid_player_movement` on its second move, about
1.5 s in. `sendTickEnds` wraps the client's `write` and follows every movement packet with a
`tick_end`, on versions whose protocol has one. Remove it once Mineflayer sends the packet itself,
and check the issue before bumping Mineflayer.

**A hair wider than a player (`playerHalfWidth = 0.3001`).** Mineflayer's collisions leave her
side exactly on a block's face (the step she's jumping up, a wall she walks along), and a 26.3
server takes that as walking into the block: it puts her back 0.01 short, every tick, with no
warning in its console, and she never climbs a one-block step. 0.0001 off the face is enough and
fits every gap a player fits. Found and checked on a local server (*Testing movement locally*):
without it she's stuck at the first step; with it she climbs, sprinting or not.

**Creepers, whatever she's doing** (`creeperTick`, `dodgeTick`), **in sight only** (hers, or that of the player it's
near: one in a cave under them can't get at anyone). Out of everyone's sight 3 s, the dodge ends; out of hers alone,
she waits for it to show rather than shoot the tree; and an arrow whose target went behind something while she drew
goes up. The same goes for the creeper-behind warning, from the player's eyes. guarding, a creeper was always
`guardTick`'s; busy otherwise (a field, fishing, gathering, following) she paid it no mind and one
blew up beside her. One closer than 10 blocks, or within 10 of a player near her (it blew up by the player at her
camp: at 5, three 1.1 s draws came too late), now takes her off whatever can be left and picked up
again (`canDefend`'s rule, `resumeFor`'s way back): shot from 6 blocks or more with a bow and
arrows, else kept out of its reach, never hit by hand; dead, gone or 16 blocks off, she goes back to
it. What can't be left (a furnace, handing over, a bed, a portal, running away) is over in seconds.

**The wider box has two edges to keep (`clearFaces`, the `AABB` patch).** The server places her
(a correction, a teleport) with its own 0.3, so often flush against a face, and her 0.3001 box
then starts 0.0001 inside that block. Mineflayer's collisions only stop a box that starts outside
one, so she walked straight in, was put back flush, walked in again: 364 times in 20 s against a
dark forest step, never arriving. After each placement she's moved out of any face she overlaps by
under 0.001 (`clearFaces`, on a microtask: Mineflayer's own handler for the packet is registered
after hers). And Mineflayer compares faces exactly, so a box rounded a hair past one
(-127.99999999999999 for -128) lost the block in front of it the same way: its three
`computeOffset` take the game's own 1e-7 tolerance. Each half alone still let corrections through.

**Her keys go to the server (`inputTick`).** Since 1.21.2 clients send `player_input` (forward,
jump, sprint, shift…) whenever their keys change; Mineflayer sends it only for sneaking, with every
other key off. `inputTick` sends her whole control state whenever it changes, as a real client
does; not while riding, where `leaveBoat` holds shift itself. **It runs first in the physics tick**
(`prependListener`): Mineflayer moves her, then runs the tick handlers (which press and release
keys for the next tick), then sends the move, so keys read after the handlers would reach the server
a tick early. The same handler sends the `tick_end` of a tick that sent no move, so the server
counts every tick, as with a real client. Neither was what stopped her at steps (the local server
refuses her with or without them), but both are what a client sends.

**`entity_action` by name (`fixEntityActions`).** 1.21.6 moved sneaking to `player_input` and
renumbered the other actions; Mineflayer 4.39 still sends the old numbers, so on 26.1 « start
sprinting » arrived as « start riding jump » and getting out of bed as « stop sprinting ». The
wrapper sends the old numbers as names on versions with the new order. Check Mineflayer's
`physics.js` and `bed.js` before bumping it, and remove the wrapper once they send names.

**The packets 26.1 changed** (`attack`, now its own packet, `use_entity`, `block_place`,
`use_item`, `arm_animation`, `client_command`; `window_click` in 1.21.5) all encode right from what
Mineflayer 4.39 writes, and every action built on them was checked on the local server, on the
server's side (`node run.js actions`): eating, armour (given, off, on, handed back, full pockets), storing, handing over, smelting, sleeping and
getting up, fishing, a boat. Two loose ends: Mineflayer waits 2.5 s for « done eating », a meal takes
1.6 s, and a slow link eats the rest, so a timeout with the food gone is not a failure; and the
server keeps where a sleeper lies in `sleeping_pos` on 26.x, for anyone reading player data.

**Teleports are answered once** (`calmTeleports`). Lying down in a bed, the server puts her in it,
Mineflayer answers every teleport with a confirm and a move to the same spot, and Paper, which still
has her where she stood that tick, takes the move for a sleeper wandering and puts her back: 230 of
each a second until the tick ended, and a kick for « too many packets » (Paper's packet limiter is on
by default; local server). The confirm always goes; the move to the spot she was just put in goes
once, and again only 250 ms later. **A second over 200 packets is logged** with the busiest ones
(« sending N packets a second », every 30 s at most): a normal second is under 60, so the next
flood is named in the add-on log before Paper's kick.

**A search that runs out of time ends nothing by itself.** Out of time (`thinkTimeout`), the
pathfinder walks her to its best point and never searches again (it only does when a path is
reset): she stood there with her goal set, silent. And a goal out of reach, which a fast machine on a
small world ends with `noPath`, runs out of time instead on the Pi or in a big loaded world, so it
became a walk that never ended. `timeoutTick` searches again once she has walked a timed-out partial
path, if it got her 2 blocks or more; got nowhere, or a timed-out search whose best point is where she
stands (`stalledSearch`), and it's `noWay()`, the same as `noPath`. A 250-block trip on the Pi's
budget re-planned ten times and got there; the unreachable lane says « no way » in 3 to 5 s at any
speed. **The Pi's budget is reproduced with `MC_CPU_FACTOR=3.5`** (the time budgets divided, about
how much slower a Pi 5 runs JavaScript than a desktop): run `all` with it before shipping anything
that makes a search heavier. Step costs read state ids (`getBlockStateId`), never `blockAt`: a whole
block per step slowed the search past its limit once.

**What hurts is avoided.** The pathfinder avoids fire, cobwebs and lava; `gentleMovements` adds sweet
berry bushes, wither roses, soul fire and powder snow, makes a magma or campfire floor cost 40, and a
step beside a berry bush, cactus or wither rose cost 8 (a turn cut close clipped a bush: a heart).

**Server teleports while she walks are logged** (`watchCorrections`, « server teleport while
walking », every 30 s at most): what the server sent, where she was, her keys, her last move sent
and the block ahead, and every block her box touches at her feet as her client knows them. A body
stuck against the terrain looks the same from outside whatever the server objects to; this line
says what it refused.

**Testing movement locally: `tools/mc-pathtest`.** A server's refusals and the pathfinder's mistakes
can't be reasoned out from the code: reproduce them. The tool runs her real body against a local
Paper with ViaVersion and ViaBackwards (the group server's pair; setup in its README): an obstacle
course of 37 lanes (corridors, corners, stairs and slabs, fences, decorations, trapdoors, gaps, a
low tunnel, panes, furniture, soul sand and cobwebs, a door with a turn, a ladder, leaves, water, a
field of pillars, lava, a cliff, a long wall to go round, a dead-end pocket, a furnished room, holes
to step round and to jump, a gap up and one onto a slab, a railless bridge, ice, turning stairs, a
cluttered corridor, a pane zigzag, a diagonal of fence posts, an unreachable goal, ladders met from
the side, round a corner and passed by in a corridor), crossings of a
ripe field, a death among decorations, a grove to cut, a plot to harvest, every packet-level action
(`actions`), every order (`orders`: smelting each kind and its refusals, gathering stone, coal,
iron and sand, handing over by kind, a full chest, every crop, a farm, a patrol, where/mark/sit), a
herd to keep (`safety`), night fights and dangers (`survival`), a long trip, and players to follow
(across open ground and through the room, the holes and the zigzag), come to, guide, lose and seek
(`players`). `wild.js` takes her into a generated world: ten real biomes behind a walking Papa,
a village's fields, orders where they happen, the Nether's five biomes with monsters, a portal; and
`lag.js` puts the road to a remote server between her and it (`--lag`). Everything is checked on
the server's side, never on what she says.
A lane passes only with no server correction (`MC_TRACE_MOVES` logs every one) and no damage. **Run `node run.js all` before shipping a movement change**, twice for a fix to
something chance-driven (trampling, rolling items), and add a lane or a spot for every snag found
in the wild. Accepting the Minecraft EULA for the test server is the owner's call.

**stdout carries events only.** The protocol libraries print their errors through `console.*`, so
the body sends every console method to stderr before loading them, and the service logs any stdout
line that isn't JSON instead of parsing it.

Options (`config.yaml` → `run.sh` → `Minecraft__*`): `minecraft_host` (empty: off),
`minecraft_port`, `minecraft_username`, `minecraft_owner`, `minecraft_tata` (Tata's in-game name,
for her lines only). If the server has a whitelist, her name
must be on it. Offline mode means anyone can log in under any name, so she trusts names only to
choose who to talk to, never for anything that matters.
