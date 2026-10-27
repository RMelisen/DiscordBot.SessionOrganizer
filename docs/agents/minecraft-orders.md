# Her orders and her free time on the Minecraft server

The rest of her Minecraft life: `minecraft.md` (the code, the two processes, joining and leaving, talk,
`/minecraft`, failures, her remarks, deployment) and `minecraft-body.md` (water, fights, body language,
arriving, testing movement locally).

## Orders

A player gives her an order in chat (`McCommand`, pure, with cases in the checks). It needs her
name in the message (`McCommand.HerNames` plus her in-game name: « sync » counts), because the
chat is shared. Stay is read before refusals, because « bouge pas » means stay; « reste avec moi »
is read before stay, because it means follow.

**Refusals are read next to the verb only**: « ne » up to two words before, « pas » just before or
up to two words after, « jamais » after, « plus » after unless it means more (« plus de bois »).
A « pas » further away belongs to another sentence (« c'est pas grave, syncs viens »), and « ou pas »
is impatience (« tu viens ou pas ? »). **Something she keeps doing refused with « plus »** (follow,
guard, patrol, farm, ranch, fishing: « ne me suis plus », « ne pêche plus ») is a stop, so Stay;
not with « tu » just before, a complaint (« tu me suis plus ? »). Stay itself can be refused
(« reste pas plantée là » is nothing) unless the phrase says no itself (« bouge pas ») or it stops
a follow (« reste pas avec moi »); « arrête de dire ça » is about her talk, not her task. A
message with « demain », « hier » or « pourquoi » is never an order (« tu peux venir demain ? »):
it goes to talk.

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
comes in a longer phrase (« ranger tes affaires », « préviens-moi »). When the list word must stay,
the common word goes in `NoSlip` instead, and is then only ever itself: « reviens » read as
« retiens » saved a place instead of coming, « jardin » farmed, « entre » walked her home,
« coucher » sat her down. Check any new six-letter word against the common words one slip away.

**Talk about oneself is not an order.** A phrase right after « je » doesn't count (« je range ma
chambre », « ça fait 5 min que j'attends »), unless it starts with it (« j'ai faim »), nor one after
« je » and a modal, two words back at most (`OwnPlan`: « je vais chercher du bois », « je dois aller
chercher »; further back the lost comma may have ended the sentence: « je m'en vais, suis-moi »);
nor one right after « il » or « elle » (« il reste du blé »), or « en train de »; nor one after an
auxiliary, maybe past an adverb, the past tense (« je t'ai donné ma pioche », « j'ai déjà vidé mes
poches », « t'as bien dormi »), or after « a » when the word as typed is a participle (« on a
trouvé du fer », « Bob a coupé du bois »: « à » comes before an infinitive, « aide-moi à couper »,
and « y a » is « il y a »). A few one-word verbs are nouns or participles after certain words (`NounAfter`: « la
pêche », « j'ai pêché », « c'est cuit », « au fond », « la base est protégée ? »), looked up by the
word as typed, since the slip lets « pêché » match « pêcher ».

**Counts** come from a number anywhere (« 20 », « x20 », « 20x »), else from a quantity in words
right before the resource (« dix bûches », « une stack de pierre », « une dizaine de bûches »),
else 10. Only right before: « du bois pour une maison » isn't one log. A number before « stack(s) »
is 64 each (« 2 stacks » was 2 logs). **A resource with no verb** is gathered when every other word
is filler and there's a partitive, a quantity or a number (`BareAsk`: « syncs du bois », « syncs 20
bois », « il faut de la pierre », « je veux du fer »); « syncs un arbre » points at one, and « syncs
bois » alone is « drink ». A resource between a going verb and a known place is the errand, not the
place (« va chercher du bois à la maison » is wood). **Coordinates** accept
F3's decimals, floored to the block (-300.7 is block -301), and labels or brackets around them.

A few readings sit between orders. **Come or stay with a known place right after**, past a
preposition (`PlaceRightAfter`: « viens à la base », « attends-moi à la mine », « reste à la
maison »), is going there; « viens voir ma base » is still come. **A killing verb with a monster
within three words** is guard (« tue les zombies », `KillVerbs`, `FoeWords`), and **« aide-moi à
… »** is help with what follows, read without the « aide-moi » (« aide-moi à couper du bois » is
wood). « lâche-moi » and « laisse-moi » are stay only as the whole message (« lâche-moi tes
diamants » hands them over). « y a du fer par ici ? » says what's here and doesn't call her. **A
follow before a going verb** is the player leading (« suis-moi, on va à la mine »: follow, not her
walk alone). After « tu » / « t' », a liking or knowing verb asks about her (`LikeVerbs`: « t'aimes
pêcher ? », « tu sais pêcher ? »); not « aime », or « je t'aime, suis-moi » would lose its order.
« œ » is spelled « oe » before reading (Normalize drops it: « œufs » read as « ufs »).

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
  of them (or 6 of her), chases it, and hits it through the shared strike path (*Fights*: her weapon's own
  cooldown, never by an edge or through a sweep onto a player), with the best sword or axe she carries. One on
  her comes first; one she finds no way to is set aside 10 s. Our own loop, not `mineflayer-pvp`, which pulls a whole Mineflayer 2.x through
  `mineflayer-utils`. Endermen and piglins are left alone: hitting them starts the fight she's
  supposed to prevent; so are the warden (two hits kill her) and the creaking (nothing hurts it while
  its heart stands). **Only a monster in sight** (`inSight`: nothing solid between her eyes and its head or chest,
  `bot.world.raycast`) is taken on, hit or shot; one she chases that stays out of sight 4 s (`FoeUnseen`) is let
  go. A monster in a cave under the ground is near by distance alone: she shot arrows into the ground at a creeper
  there. **What's a monster is `isFoe`**: minecraft-data's `hostile`, plus the few it
  files elsewhere (slimes, magma cubes, phantoms and ghasts as `mob`, hoglins as `animal`), which she
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
  but not counted as wood, and gravel's flint counts as gravel. **A block that drops several**
  (`PerBlock`: clay, redstone, lapis, 4 at the least) is broken the count divided by that, or
  « 20 redstone » was 20 ores and about 90 dust. Granite, diorite and andesite drop themselves; quartz
  is only in the Nether, red sand is sand. A new resource is a `McResource`, its words, its name in
  `McResourceNames`, and `Resources` and `Yields` in the body. **In rounds**: what's open now, then what that opened (a trunk's top log, boxed in by
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
  and a shield in an empty off-hand (`wearBestArmor`). Golden apples are never a meal (`Reserved`):
  they're for *Fights*' emergencies. Both are our own few lines;
  `mineflayer-auto-eat` is ESM-only now and the armour plugin is unmaintained. Tiers go by
  `ArmorTiers`; **a tier missing from it still beats nothing** (copper armour, missing, was never put
  on). A piece someone gives her (an armour item picked up) gets the closest player's thanks
  (`armoured`, `McArmouredLines` and twins), but not her own things coming back after a death.
- **Armour on and off** (`Dress`, `Undress`, free orders answered from the body's event):
  « mets ton armure », « équipe-toi » put on the best she carries (any piece named is all of it: « mets
  ton casque », « donne-moi ton bouclier »); « enlève ton armure », « retire
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
  (`McCreeperLines`). Under 3 hearts she stops fighting and backs off, eating to heal and hitting what keeps
  up with her, until she's at 7 hearts or 20 s have passed (`McRetreatLines`). The tick changes her goal only when what she's
  doing changes (`setMode`): a goal set on every tick is never walked.
- **Patrol** (« syncs surveille la base ») is guard around a spot: where she stands, or a remembered
  place named with it. Read before stay and guard: a care verb with a place word within four words
  after (`BaseWords`, « ici » included), never when the verb is followed by « moi » (« protège-moi à
  la maison » is guard). Like a farm, it lasts while its keeper is on the server.
- **Errands and activities.** Farming, fishing, keeping animals and patrolling are activities: they last until
  someone says otherwise. Handing something over (Give), putting her things away (Store), fetching
  from a chest (Fetch) and crafting (Craft) are errands: they pause an activity, the body goes back to it after (a fresh copy of the task, so the
  old loop sees it's no longer hers and stops), and the service gives the claim back to the
  activity (`McJob.Resume`), without telling its keeper she left. A night's sleep pauses and resumes
  every activity the same way (`resumeAfterSleep`).
- **Give** (« vide tes poches », « donne-moi ton poisson ») tosses what she carries at the player's
  feet; armour includes what she wears. A gathering word only becomes Give with a possessive
  (« donne-moi ton fer »): « donne-moi du fer » stays the gathering order. Goods she can't fetch
  (`CarriedOnly`: food, fish, tools, torches, wool, string, pearls…) take any article. « poudre »
  is gunpowder unless « redstone » follows (`GoodsAt`); « t'as vu ma pioche ? » asks about something
  else (`NotGoods`). A new kind is a `McGoods`, its words, `McGoodsNames`, and `Goods` in the body,
  whose key is the enum's name in lower case. Give is read before Store, whose « ranger »
  is one slip from « manger ». **Store** (« range tes affaires », or anything put « dans le coffre »)
  fills the closest chest or barrel within 16 blocks and keeps her kit: the best sword, pickaxe, axe
  and shovel, a rod, shears, her bow and arrows, and what keeps her alive (golden apples, a totem, healing and
  fire resistance potions).
- **Fetch** (« ramène-moi du fer du coffre », « va chercher 20 pains dans le coffre », `fetch.js`)
  takes a kind of goods from the chests and barrels within 16 blocks of her or of the asker, the
  closest to her first, and hands it over (`handOver`: whole stacks, smallest first, the last one
  whole even when it's more than asked). A number caps it, else up to a stack. Any chest, no owner
  check (the owner's call); a chest is opened and closed, never broken. A bare carrying verb
  (« ramène », « apporte », « rapporte », « amène ») is a fetch only with « du / des / de … coffre »:
  « ramène le fer au coffre » means put it there. « tout » or goods she doesn't know get
  `McFetchUnknownLines` at once: she won't empty chests for nobody knows what. Read before Store, so
  « dans le coffre » after a fetch verb is where to look. No chest near is refused before she drops
  anything (`noChest`); chests with none of it, after (`none`).
- **Craft** (« fabrique-moi une pioche en fer », « fais-moi 16 torches », `McCraft`, `craftorder.js`):
  tools and weapons in wood, stone, iron, gold and diamond, a bow, a fishing rod, shears, planks,
  sticks, a crafting table, a chest, a furnace, torches and ladders. A count is items, made in whole
  recipes (16 torches: 4 crafts), a tool one by default (4 at most), anything else one recipe's
  worth (64 at most). A bare tool (« une pioche ») is the best she can afford: diamond, iron, stone
  (also when there's open stone to gather), else wood; never gold unasked. « fabrique », « crafte »
  take any item or none (« fabrique-moi un truc » gets `McCraftUnknownLines`); « fais-moi » only with
  an item in the next three words, so « fais-moi peur », « fais-moi rire » stay talk and
  « fais-moi du bois » stays gathering. Read after guard and before the chest flag: « craft un
  coffre » makes one. Wood she cuts (whole trees), stone she gathers from open faces with a wooden
  pickaxe she makes if she has no pickaxe; iron, gold, diamonds, coal and string she must carry
  (`cantCraft missing`, named by `McCraftMaterialNames`), checked before she drops anything, like no
  tree (`noWood`) or no open stone (`noStone`) when she's short. She crafts at a crafting table within
  16 blocks, whoever's it is, else at her own (below), and hands over only what she made for them;
  the planks, sticks and wooden pickaxe made along the way stay hers. Planks are of the wood she has
  most logs of. A fight starts the craft again where it was (`Restartable`, with her table and her
  pockets at the start); some made and then a craft refused, she hands over what she has.
- **Fishing** walks to open water within 16 blocks and casts until told otherwise. No bite in 45 s
  and she reels in and casts again; only then does she click to reel in, since after any other end
  a click would cast. The catch stays on her; a remark now and then (a treasure always).
- **Smelt** uses only a furnace someone else isn't using (nothing in its input or output, no other
  fuel), smokers for food and blast furnaces for ore too, with her own fuel. She waits by it, takes
  the result as it comes, and brings it back. Whether a furnace is free is only known by opening it,
  so « all busy » is the one refusal that comes after she has dropped her task. Clay makes bricks and
  logs make charcoal (`McSmelt.Clay`, `Wood`; « fais du charbon de bois »), in a furnace only; the logs
  going in are never her fuel. « fais du charbon » alone stays a trip for coal, like « fais du bois ».
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
  length is out. Three per player, two hours at most, in memory, gone with the body. Only a delay:
  « réveille-moi à 20h » is a time of day (`ClockTime`) and sets nothing, where it once set twenty
  hours. A bare number after « timer » or « minuteur » is minutes. Sitting is stay, crouched, and
  standing up (« lève-toi », « debout ») is stay, which lets go of the crouch and gets her out of bed.
- **« tu fais quoi ? »** (`ChatIntent.WhatAreYouDoing`, read in `Talk` before anything else talk
  does) is answered from what she's really doing, in her own words (`McDoing.FirstPerson`,
  `OwnLife`): the order under way or just sent first (« je pêche pour Bob », « je te suis » to its
  own player), else her own life from the body's last `doing` report (the routine and where it goes,
  tagging along, walking up to someone, foraging, getting her things back, a danger, sleep, sitting
  or held), else `McDoingNothingLines`. The body sends `doing {kind,player,what}` when it changes,
  checked once a second (`doing.js`), and again on every spawn. A whispered order's place is never
  said to anyone else. `/minecraft status` uses the same report in the third person when she's on
  nobody's order (« elle pêche (sa routine) »).
- **She never digs and never places a block.** `canDig = false`, no scaffolding blocks, no
  pillaring: a path through someone's wall or a dirt tower in their build is not help. Where
  walking can't get there she says so (`McNoPathLines`) instead. **The exceptions are a seed
  on bare farmland where she just harvested** (`harvestBlocks`), **a gathered tree's natural
  leaves** (`gatheringMovements`), **the pillar she climbs a tree with, taken down after**
  (`fell`, `unstack`; an order that takes her away mid-tree leaves it standing, with a log line),
  **her crafting table for a craft when none stands within 16 blocks**, set down beside her (an
  empty cell over firm ground, nobody on it) and broken and picked up once she's done (`setTable`,
  `takeTableBack`; an order that takes her away mid-craft leaves it standing, with a log line),
  **her camp's own blocks and floor** (below) **and a bridge of two blocks at most, taken down once
  she's across** (below): nothing else, nowhere else. Keep them that narrow.
- **A bridge when walking can't get there** (`bridges.js`). « No way there » on a walk a player asked
  for (come, follow, go to, guide, guard) or her walk up to family is planned once more with blocks
  she may place: **2 at most** (the pathfinder plans with `min(what she carries, 2)`), of what she'd
  least miss (dirt, coarse dirt, cobblestone, cobbled deepslate, netherrack; never a log), each worth
  10 steps so any way round wins, never into or over lava, never a pillar. Only then, and still no way,
  does she say so. That covers a ditch or a ravine's narrow end too wide to jump under a low ceiling,
  and the step out of a two-deep trench. **Every block comes down**: once she's 1.5 blocks off them
  she stops her walk, breaks them from where she stands (the drops fall where they fall: dirt is
  cheap) and walks on. One someone stands on or out of her reach stays, with a log line. Not again
  for the same walk within 30 s, and a try that placed nothing gives the plain walk back after 20 s.
  The search can't run from inside the pathfinder's own `path_update` (it sets the path it found
  after its listeners, and a search asked for in one never runs), so the retry waits a beat.
  `/debug minecraft` « Voir son corps » says when she's ready to bridge and how many of her blocks
  still stand.
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
- **« quartier libre »** (« t'es libre », « fais ta vie », « tu peux y aller », « repos »,
  `McOrder.Free`, `FreePhrases`) gives her back her life at once: the order or activity ends, a
  « reste » hold is lifted (`letGo`) and the grace skipped (`skipGrace`). A stop like stay, so the
  double-send gate never holds it back. Not when a time word follows (`FreeLaterWords`: « t'es
  libre ce soir ? », « demain », « samedi »…): that's asking a friend, not freeing her. With no order,
  no pending one and no hold, she says she already is (`McFreeAlreadyLines`). « arrête de me suivre »
  stays stay; « tu peux arrêter de me suivre » frees her.

Then, in this order: getting her things back, the routine, tagging along, nothing. **What she
starts on her own gets a rare rolled remark** (the owner's call): the routine 25 %, tagging along
20 %. Getting her things back always gets one.

- **Routine (named places).** `McRoutine.Classify` (pure, in the checks) finds among the remembered
  places (Papa's first, then Tata's, then anyone's, kind by kind) a pen (an animal-pen word anywhere in the name: « ferme aux animaux »), a farm or a fishing
  spot (a farm or water word first: « champ de blé », « étang »), and home (`McRoutine.HomeKeys`,
  shared with « rentre »). The service sends them (`routine`) on spawn and after every save, move or
  forget. By day (time 0–11499) she works in turns at the farm and the fishing spot, **9 minutes in the fields, 6
  fishing** (`WorkBlock`), and **visits the pen twice a day** (the owner's call): in the morning (a visit may start
  from 0 to 2999) and before dusk (from 8000), **2 minutes each, counted from her arrival** (`PenSlots`). Twice a day
  breeds twice (an adult breeds again after 5 minutes, and yesterday's babies are grown) and shears twice. A visit's
  hour preempts a turn at the fields; a visit under way runs to its end whatever the clock says, and counts only once
  it has (`penDone`, by `bot.time.day`), so one an order cut short is made again while its hours last. With only a pen,
  she's free between the visits. With the clock stopped (`advance_time` off), the pen gets the visit of the hour it
  stopped at, once. Each turn starts with the order's own checks (crops and a chest, water and a rod, animals and a
  chest); her fishing is quiet (no catch remarks). At dusk (11500–12541) she goes home and stays there till
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
