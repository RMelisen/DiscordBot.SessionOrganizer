# The camp — design

## Why

« syncs monte le camp » builds a closed 5×5 plank hut today (7.32). It works, but it's a box: 71
planks of wall and roof, some 28 logs to gather, and nothing nice to arrive in. Papa wants **a cozy
little camp with the basic necessities**: not too expensive for her to build, still nice to arrive
in. What matters: **crafting, cooking, sleeping, and a small lit area round a fire.**

## Goals

- An open camp round a campfire, with places to sit, a workstation, a bed and light.
- Looks hand-made, not square: uneven trodden ground, nothing perfectly symmetric.
- Costs less than the hut. My estimate: about 18–21 logs, 8 cobblestone and 2 coal or charcoal,
  plus 3 wool for a real bed. The hut needed about 28 logs.
- Everything that works today still works. She crafts it all from what she carries and gathers what's
  missing. Only Papa and Tata can order it. She keeps one camp at a time, and taking it down returns
  every placed block. A fight or a creeper interrupts the build, and she then finishes the same camp.

## Non-goals

- No hut: no walls, no roof, no door. The enclosed hut is dropped, not kept as an option.
- No change to who may order it, to the C# commands, or to how the camp is stored (`McCamp`: the
  blocks she placed, the dimension, the centre).
- No farming, animals, storage sorting or anything « living » at the camp.

## Anti-goals (a failure even if it works)

- Breaking or changing anything that isn't hers: flowers, saplings, someone's blocks. She builds
  only on her own cells and touches nothing else (`CampOver` rules stay).
- A camp that comes out lopsided or half-floating because the ground wasn't flat where it mattered.
- Getting stuck building it: the camp must stay interruptible like it is in 7.32.1.

## The layout (decided with Papa)

A 9×9 square, seen from above, with the back of the camp to the north (row 0) and the entrance to
the south (row 8). `x` runs east and `z` runs south, counted from the north-west corner at her feet'
level, as in the code today.

```
     x 0 1 2 3 4 5 6 7 8
z 0    . l . . . . . . .
  1    . . . L S S . B l
  2    . . p D p p . B .
  3    . L p D d p p D .
  4    . S p d F d p T .
  5    . S D p d p D K .
  6    . . p D p p D C .
  7    l . . p D p . . .
  8    . . . . p . . l .
```

| Mark | What | Where |
|---|---|---|
| `F` | campfire (lit) | (4,4), the centre |
| `d` | fire pit: 4 trapdoors lying flat (closed, bottom half), a cross round the fire | (4,3) (3,4) (5,4) (4,5) |
| `L` `S` | bench: a log on its side, then two bottom slabs, in a line | back: L (3,1), S (4,1) (5,1), running east. Left: L (1,3), S (1,4) (1,5), running south |
| `T` `K` `C` | workstation: crafting table, furnace, chest, in a row | (7,4) (7,5) (7,6), on the right, facing the fire |
| `B` | bed, head to the back | (7,1) head, (7,2) foot |
| `l` | torch on a fence post (fence at ground level, torch on top) | (1,0) (8,1) (7,8) (0,7): each corner moved one cell clockwise |
| `p` | dirt path (made with a shovel, nothing placed) | the floor, see below |
| `D` | plain dirt | the floor, see below |
| `.` | left as it was | |

The benches go on two sides of the fire only, with the workstation on the third side (bench, bench,
workstation). This keeps the camp from looking symmetric. Papa decided this, along with the bench
shape (log + slab + slab), the fire pit and the torch positions.

### The floor

The floor is a patchwork of dirt path and plain dirt over the cells marked `p` or `D`. Its outline
is irregular, not a square, and it narrows to a path at the entrance. **The floor stays when the camp
is taken down** (Papa: fine to leave it): it's the trodden ground of a place someone camped.

- Path: the grass block's top used with a shovel. Nothing is placed.
- Plain dirt: *(changed while building)* the cell made a path, then a block set on it for a moment and taken
  back, which turns a path to dirt in vanilla. Digging and refilling lost the dirt now and then, and left a
  2-deep hole in a dip.
- Only grass and dirt-family ground (grass, dirt, coarse dirt, podzol, mycelium, rooted dirt) is
  changed. Any other ground (sand, stone, snow) is left as it is.

*My call:* the outline is the one above, but which floor cells are path and which are plain dirt
changes from camp to camp, so two camps don't look alike. About three cells in five are path, and
the cells from the fire to the entrance are always path. The pattern is drawn when the site is
chosen and kept on the task, so a camp taken up after an interruption carries on with the same one.

### The bed

She tries these in order (Papa decided the last two):

1. **A real bed**, in the colour she has three wool of.
2. **Wool sheared on the spot**, as today: shears and a sheep in sight, never a sheep killed.
3. **A straw bed, only from what she carries**: 3 hay bales, or 27 wheat to make them. She never
   goes looking for hay or wheat. The wheat near her is usually someone's field.
4. **No bed.** She builds everything else and says the bed is missing: `complete: false` and a
   partial line that names the bed. She no longer refuses at the start for want of wool
   (`missing: wool` goes away from the camp's start), because a camp without a bed is still a place
   to craft, cook and warm up.
- **Checked on the local 26.3 server.** The straw bed exists (`minecraft:straw_bed`). Its recipe is
  3 hay bales in a row, which makes 4 beds, so one bed costs 27 wheat. It drops itself when broken.
  Her 26.1 client has no straw bed in its data. ViaBackwards shows it to her as a **`yellow_bed`**,
  both in her inventory and as a block (head and foot, `facing`, `occupied`), and its recipe arrives
  in her recipe book as a yellow bed. So:
  - She crafts it through the book like everything else (`craftOne`). The book entry with the hay
    recipe is the straw bed. She can't use `bot.recipesFor`, because minecraft-data doesn't know the
    recipe.
  - Once made, she can't tell it from a real yellow bed, and doesn't need to: taking the camp down
    compares the names she recorded (`yellow_bed`), which stay the same.

### Cost

My estimate, with the recipes as they are in vanilla:

| Part | Cost |
|---|---|
| campfire | 3 logs, 3 sticks, 1 coal or charcoal |
| fire pit | 4 trapdoors: 2 crafts of 6 planks |
| benches | 2 logs and 4 slabs (one craft of 3 planks makes 6) |
| crafting table, chest | 4 planks, 8 planks |
| furnace | 8 cobblestone |
| bed | 3 wool and 3 planks |
| torch posts | 4 fences (2 crafts of 4 planks and 2 sticks) and 4 torches (1 coal and 1 stick) |
| tools | wooden pickaxe (stone), wooden axe (taking down faster), **wooden shovel** (new: the path) |

That comes to roughly 55 planks of crafting, about 14 logs, plus the 5 logs placed or burnt whole,
plus charcoal logs if there's no coal. The slabs, trapdoors and fences use the wood she has the most
of. *My call:* the bench log and its slabs are the same kind of wood when she has it, so a bench is
one colour.

## Building it

The same flow as today, with a new list of blocks to place:

1. Wool if she needs it.
2. Wood.
3. The crafting table, then the tools.
4. Stone and coal (charcoal from the camp's own furnace as the fallback).
5. Craft the rest.
6. Place everything.
7. Do the floor last.

Placement order (*my call*, the builder may reorder for reach):

1. Workstation first. The furnace is needed early for charcoal.
2. The bed.
3. The fire pit trapdoors, then the campfire.
4. The benches.
5. The fence posts, then their torches.
6. The floor.

Placement details the builder must handle:

- **A log's lie depends on the face it's placed against.** So place a bench's slabs first, then its
  log against the end slab's side. The back bench's log is set against (4,1)'s west face (log along
  x). The left bench's log is set against (1,4)'s north face (log along z).
- **Trapdoors** are placed against the ground's top face, which gives a flat, closed, bottom-half
  trapdoor. Check this on the server: a trapdoor placed against a side face hangs on the wall
  instead.
- **The bed** is laid in the direction she looks. She stands at (7,3) facing north, which puts the
  foot at (7,2) and the head at (7,1).
- **The table, furnace and chest** turn to face her when placed. She places them from the fire's
  side (x 6), so they face the fire.
- **Slabs** are bottom slabs: placed against the ground's top face, or against a block's lower half.

## Where it fits (`campFits`)

The site must be flat **where something goes**: every `p`, `D`, `d`, `F`, bench, workstation, bed
and fence cell has firm full ground at y−1 and room above it (2 blocks, a torch post 3). The `.`
cells aren't checked, so the camp sits into uneven ground. This is *my call*: a 9×9 flat square
is rare on a meadow, and a wider footprint left no room even with the 5×5 hut.

Search radius, the site nearest her middle-first, and `noRoom` stay as they are.

## Taking it down

- She breaks only what she placed and what's still there.
- That means the benches, trapdoors, campfire, workstation (the chest's contents with it), bed,
  fences and torches. The floor stays. She keeps everything.
- The order changes now that it's open (*my call*): torches first (they'd drop off their posts
  anyway), then the rest nearest-first from where she stands, then a sweep of what fell.
- The hut-specific order (from before the door, then from the middle, walls from the top) goes away.
  No hut was ever built in play (Papa), so there's no old hut to support. The fallback to
  collectblock (`gather`) stays for anything out of reach.

## Interruptions

These stay as they are in 7.32.1. A fight or a creeper takes her off the camp, and she comes back to
the same site with what's already placed (`resumeFor` carries `site` and `placed`, and
`campNeeds(placed)`). New with the floor:

- A cell dug for plain dirt and not yet filled is a hole. Taken up again, she fills any floor
  cell that's empty.
- The floor pattern comes back with the task (above).

## Things to update with it

- `BotResponses.Minecraft.cs`: the lines that talk about a hut, a roof or a door.
  - Ack: « Une cabane, un lit, un feu de camp ».
  - Done: « Voilà ta cabane », « Et avec un toit », « Entre, installe-toi. Les chaussures, tu les
    laisses dehors ».
  - Refused: « Les cabanes, c'est sur commande ».
  - No room: « un coin plat de cinq sur cinq ».
  - Partial: a line or two that say the bed is missing. The `missing` lines lose « de la laine »
    for the camp.
  - Rewrite them for an open camp round a fire, in her voice (`docs/syncs-voice.md`).
- `McHelp` (« elle construit une petite cabane… »), `README.md`, `docs/agents/minecraft.md` (the
  camp paragraph), the CHANGELOG.
- `tools/mc-pathtest/camp.js` checks the new layout block by block: the right block, the logs on
  their side along the right axis, the trapdoors flat, the bed's head to the north. It also checks
  that the floor stays after taking the camp down and that nothing else changed. The `interrupt`
  mode and `wild.js camp` keep working.
- Version: a minor bump (new user-visible behaviour).

## Left to the builder

The exact build order for reach, how many standing spots she uses, the floor-pattern drawing, the
crafting order, and the rewritten lines.

The builder may also settle these smaller points:

- The floor pattern lives on the task in the body: a resume never leaves the process, so C# needs
  no change.
- Floor cells stay out of `placed`.
- Short grass on a floor cell is cleared before the shovel is used.
- The centre sent to C# becomes (4,4).

Anything marked *my call* above can be changed if the server says otherwise. Say so in the notes.
