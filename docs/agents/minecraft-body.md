# How her body moves on the Minecraft server

The rest of her Minecraft life: `minecraft.md` (the code, the two processes, joining and leaving, talk,
`/minecraft`, failures, her remarks, deployment) and `minecraft-orders.md` (orders and free time).

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

**Water columns** (`columnMoves`). The pathfinder plans no step up or down in water (none under the surface, and no jump
out of it), so to it a shaft someone swims up to their base is a wall. In a column she gets her own
steps beside the pathfinder's: a block up, a block down, in and out at its foot where it ends in other water (the
puddle a waterfall spreads into, a lake it falls into), and out at the top onto a ledge a block over her feet (out into
the shallow stream feeding a waterfall is the pathfinder's own level step). **A column is water one block wide**: one
of its four sides at most is wet (the doorway at its foot, where it spills out), and every block of a lake has two, a
corner too. So a cave waterfall down a wall is one, all but the block where it hits the floor. So a lake or the sea is never one: open water stays the dive's, and the
sea's magma columns stay walls. In a column, any bubble column is water to swim (`gentleMovements` marks it a liquid),
**never up against a drag nor down against a lift**. **The whole way under must fit in her air** (`ColumnRun`, 8 of
the 9 s before `AirLow`), at the speeds Mineflayer's physics gives her, measured: in still water she swims up 3.3
blocks a second but **sinks only 0.5** (Mineflayer has no crouching down); falling water with something solid beside
it (a waterfall down a wall, a shaft of flowing water) pulls her down, so 2 up and 1.8 down; a lift carries her 10 a
second, a drag 4. So 24 blocks up and 4 down in still water, 16 up and 14 down a waterfall, 64 up a lift, 32 down a
drag (`columnKind`, by the block she moves into: over a waterfall's edge the water still runs level). **Down a
waterfall with a puddle beside its foot, she usually jumps off the ledge into it** instead: the pathfinder's own drop
into water, which breaks any fall. Going down, the pathfinder would hold jump (it
does in any water) and she'd never sink: `columnSteer` lets go of it, and of forward once she's over the point (pushing
on would take her out of a column in the open air, a waterfall). The pathfinder puts a path point a block lower when its
block has no top (a bubble column, kelp: `postProcessPath`), so such a step keeps its height (`columnY`,
`columnPoint`). No dive starts in a column (it would steer against the path, and in a drag only try to go up), and in
a drag she doesn't go up for air while the path takes her down: the way out is at the bottom. `column-check.js` plans
each kind with the real pathfinder, no server needed; the course's `column-*` lanes have **not been run yet** on the
local server.

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
`gentleMovements` makes it a wall and her own swimming avoids it, except in a water column (above).

Lines: `McDiveLines` (with twins), `McDiveAirLines`, `McDrowningLines`, `McBreathingLines`,
`McBoardLines` (with twins), `McShakeLines`.

## Fights

Papa's call (`docs/superpowers/specs/2026-10-25-combat-design.md`): she stays alive in most dangerous situations
without anyone minding her. The decisions that need no `bot` are in `MinecraftBody/lib/fight.js` (`fight-check.js` in
the test tool checks them, no server needed); everything that moves her is in `combat.js`, `strike.js`, `ranged.js`,
`hazards.js` and `emergency.js`.

- **She defends herself, whatever she's doing** (`selfDefend`, `fightTick`, task `fight`). Hurt by a monster
  (`entityHurt` on her), or a monster in sight within 4 blocks coming at her (`threatTick`, `comingAt`: closer by
  0.2 over half a second, or under 2 blocks), she stops and deals with it, then goes back to what she was doing
  (`resumeFor`, as after defending someone). One standing about (a skeleton on a hill) leaves her to her work. Her
  life comes before an errand: one that can't be started again (a furnace, handing over, storing) is dropped and
  its player told (`dropped` with no attacker → `McDangerDroppedLines`). Not asleep, in a boat, crossing a portal or
  running from a player. Guarding, patrolling or defending someone, the fight is `guardTick`'s or `defendTick`'s.
  Putting herself out comes first: while she walks to water (`hazard`), nothing starts a fight (the two replaced
  each other on every tick, and what she was doing was lost). A fight, a walk to water or a defence taken over hands
  on its own way back (`resumeFor`), an order held mid-fight first.
- **Who she never hits** (`answerTo`): a tamed animal (`owneruuid`: Papa's wolf attacks her when he hits her), an
  iron golem, bees, goats, llamas, polar bears, wolves; the warden and the creaking. She gets clear of them instead,
  mid-fight too (one hurting her turns the fight to getting clear, never a target).
  Endermen and piglins are fought once they've hurt her, never before.
- **She sizes the fight up** (`sizeUp`, every 1.5 s at most so it doesn't flip): every monster in sight within 12
  weighs (`KindWeights`, in zombies, less with distance) against her strength (`strength`: two zombies at full
  health bare-handed, more with armour points, her best weapon's tier, and her player beside her). **Fight**,
  **fall back** (to her player: the one of the order she was on, else Papa, else Tata, else the closest, within 32;
  hitting whatever keeps up, eating when nothing is in reach, `McRetreatLines`), or **get out** (a warden or a
  creaking within 16: beside her player on their far side from it, 6 beyond them, else 16 away; never a hit). At 3
  hearts it's always fall back. Back from falling back only at 0.75 of the odds. No weapon is no reason to run:
  bare-handed she still takes on one or two. Alone and still chased after 20 s (`FallBackAlone`), she turns and
  fights it out.
- **Orders mid-fight** (Papa's call, `holdForFight`): « viens » and « suis-moi » bring her to that player at once,
  falling back as she fights; any other order (« reste » included: she can't stand still under attack) waits for
  the fight, a newer one replacing it, and C# keeps it pending until the body reports it under way.
- **One strike path** (`strike`) for guard, patrol, defend, self-defence and the ranch's cull: her weapon's own
  cooldown (`cooldownFor`: sword 625 ms, axes 1000–1250, trident 909, mace 1667, hand 250). **A player beside the
  target** (`sweepReaches`: inside its box grown by 1 sideways and a quarter up and down; never the target itself, a
  player she defends Papa from): a sword's sweep would land on them, so she hits from a jump (a critical hit never
  sweeps); not swinging at all let zombies hugging Papa kill him. **No hazard behind her** (`edgeBehind`,
  `hazardAt`: a drop over 3, lava, the void): off the lip as soon as a foe is close, not only when she swings (its
  hits come on its own time). **Her fight walk** (`fightMovements`): no parkour, no drop over 2, the lip of a drop
  or lava costs 40. A foe 12 from where it started is let go (`FightLeash`).
- **Ranged foes** (`RangedKinds`, a drowned with a trident; `rangedStep`): with a bow, shot from 6 or more
  (`shoot`'s rules); without, closed in on. A ghast or a blaze she can't get at (over 3 above her): out of its
  sight, by the player she guards or falls back to (their far side from it), else 16 away (away from Papa, she hopped
  60 blocks off). A spot she's sent to (a hop, cover) is set again once a second at most, and no way there is no
  failure to tell anyone: her player out of reach, she hops instead for 10 s. **The shield** (`shieldTick`): up against a ranged foe out to get her (its `mob_flags` aggressive bit: the
  server never sends a skeleton's drawn bow) and facing her, not while she runs in at that one (it slows her to a
  crawl), and between her hits; down to hit, eat or draw. An axe disables it 5 s (`entity_status` 30).
- **Fireballs** (`fireballTick`): her client hears where a fireball is only every few ticks (5 blocks apart), so
  she guesses from its last two positions and swings every tick from 7 blocks for 1 s; a landed punch sends it back
  the way she looks, at it. From 4 blocks, the one swing in reach came too late.
- **Emergencies** (`emergencyTick`): at 4 hearts a totem goes in her off-hand (the shield to her pockets, back past
  7 hearts, or once the totem popped); with nothing in reach, a healing potion, else a golden apple (enchanted
  first), every 3 s at most. Potions by their registry id as her 26.1 client gets them (healing 24, poison 28, fire
  resistance 11 on the local server), never drunk blind. A totem popping (`entity_status` 35) is `totem` →
  `McTotemLines`, said at once.
- **Fire and lava** (`hazardTick`, after her swimming): in lava or fire she steers herself to the closest standable
  block within 4 by her own keys (the pathfinder avoids both and won't plan from inside them); on fire on dry land
  she makes for water within 8 (`hazard` task, 10 s at most, through `resumeFor`). A fire resistance potion is
  drunk at once.
- **Not surrounded**: no manoeuvre of its own. Her chase already took her out of a ring of four zombies in every
  layout tried, pressed against her included (`surround`).

Checked by `node run.js survival`: `ravine`, `beside`, `farmzombie`, `followzombie`, `furnace`, `farmskeleton`,
`golem`, `golemfight`, `horde`, `hordeperch`, `alone`, `warden`, `ordered`, `takenover`, `burningzombie`,
`ghastguard`, `ghastbow`, `skeletonshield` (three live skeletons: chance-driven, about one run
in six below its bar), `skeletonbow`, `fireball`, `surround`, `gapple`, `totem`, `keepkit`, `burning`, `lava`.

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

## Testing movement locally

**Testing movement locally: `tools/mc-pathtest`.** A server's refusals and the pathfinder's mistakes
can't be reasoned out from the code: reproduce them. The tool runs her real body against a local
Paper with ViaVersion and ViaBackwards (the group server's pair; setup in its README): an obstacle
course of 43 lanes (corridors, corners, stairs and slabs, fences, decorations, trapdoors, gaps, a
low tunnel, panes, furniture, soul sand and cobwebs, a door with a turn, a ladder, leaves, water, water columns up and
down with both bubble kinds, a waterfall up and down, a
field of pillars, lava, a cliff, a long wall to go round, a dead-end pocket, a furnished room, holes
to step round and to jump, a gap up and one onto a slab, a railless bridge, ice, turning stairs, a
cluttered corridor, a pane zigzag, a diagonal of fence posts, an unreachable goal, ladders met from
the side, round a corner and passed by in a corridor), crossings of a
ripe field, a death among decorations, a grove to cut, a plot to harvest, every packet-level action
(`actions`), every order (`orders`: smelting each kind and its refusals, gathering stone, coal,
iron and sand, a quarry and one over lava, gravel and a cave, handing over by kind, a full chest, every crop, a farm, a patrol, where/mark/sit), a
herd to keep (`safety`), night fights and dangers (`survival`), a long trip, and players to follow
(across open ground and through the room, the holes and the zigzag), come to, guide, lose and seek
(`players`). `wild.js` takes her into a generated world: ten real biomes behind a walking Papa,
a village's fields, orders where they happen, the Nether's five biomes with monsters, a portal; and
`lag.js` puts the road to a remote server between her and it (`--lag`). Everything is checked on
the server's side, never on what she says.
A lane passes only with no server correction (`MC_TRACE_MOVES` logs every one) and no damage. **Run `node run.js all` before shipping a movement change**, twice for a fix to
something chance-driven (trampling, rolling items), and add a lane or a spot for every snag found
