# Deployment and protocol patches

Part of `docs/agents/minecraft.md`: read that first.

The Dockerfile has a `node:22-bookworm-slim` stage that runs `npm ci` from the lockfile, then copies
`index.js` and `lib/`. The final image gets the `node` binary and `/app/body`. The service looks for the body at
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

The workarounds below are all in `lib/compat.js` (its header is the checklist for a bump, see *The
body's code*), and « Voir son corps » in `/debug minecraft` shows which ones the server's version
needs.

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

**Testing movement locally** (`tools/mc-pathtest`) is in `minecraft-body.md`.


**stdout carries events only.** The protocol libraries print their errors through `console.*`, so
the body sends every console method to stderr before loading them, and the service logs any stdout
line that isn't JSON instead of parsing it.

Options (`config.yaml` → `run.sh` → `Minecraft__*`): `minecraft_host` (empty: off),
`minecraft_port`, `minecraft_username`, `minecraft_owner`, `minecraft_tata` (Tata's in-game name,
for her lines only). If the server has a whitelist, her name
must be on it. Offline mode means anyone can log in under any name, so she trusts names only to
choose who to talk to, never for anything that matters.
