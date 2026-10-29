# Minecraft orders — moving and gathering

Part of her Minecraft orders: read `docs/agents/minecraft-orders.md` (the index, and the rules
that bind every order) first.

## Moving and gathering

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
- **Stone is quarried** (`quarry.js`, for the order, the camp's cobblestone and a craft's stone alike): one spot dug into, the way a
  player gets it. Block by block, each the closest open stone within 48 and a pathfinder search and a walk apiece, she
  hopped between surface blocks all over a hillside (40 stone in 69 s on the test face; 34 s now). She starts at a face
  she **sees** from where she stands (`quarryFace`: of the 32 closest open stone, the most stone around it for the walk;
  none in sight, the closest): scored on stone around alone, a sealed cave's wall under the ground won, and its search
  ran out of time. Then **everything in arm's reach** (`stoneInReach`, 4.4 from her eyes, a face she sees) is dug
  straight with `bot.dig`, no search, what each dig opened included; she only walks (collectblock, the closest open
  stone) when nothing is left in reach, and the drops lie in her own hole, so picking them up is a step in. Her floor's
  level comes last (`FloorLast`): it's always closest to her eyes, and on stone ground she pocked the ground round her
  feet before the face. **What she never digs** (`quarrySafe`): under herself; lower than one block under where the
  quarry began (`QuarryDeep`: at two she dug a 1x1 pit two deep and spent a 5 s search getting to its bottom); a block
  by lava or water; one under sand or gravel; and one with no solid block under it at her feet's level or lower, or at
  the quarry's bottom wherever she stands (from inside her own pit, the ground over a cave was at her feet and she dug
  through). The cube she scans after each dig is read by state id first (`getBlockStateId`), never a whole block per spot.
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
