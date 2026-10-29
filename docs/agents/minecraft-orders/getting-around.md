# Minecraft orders — getting around, and the camp

Part of her Minecraft orders: read `docs/agents/minecraft-orders.md` (the index, and the rules
that bind every order) first.

## Getting around

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
