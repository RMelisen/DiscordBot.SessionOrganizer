# mc-pathtest

Movement tests for her Minecraft body (`ProjectSYNCS/MinecraftBody`), run against a local server. A server's
refusals and the pathfinder's mistakes can't be reasoned out from the code: these run her real body through an
obstacle course and report what happened. Run them before shipping a change to how she moves, gathers or picks
things up, and add a lane (`course.js`) or a spot for every snag found in the wild.

## The server

Paper with ViaVersion and ViaBackwards, the same pair as the group's server, in a folder outside the repo (the jars
are not committed):

1. Download Paper for the server's version (papermc.io, a stable build), and ViaVersion and ViaBackwards (Modrinth or
   Hangar, release builds). Check the published hashes.
2. `server.properties`: `online-mode=false`, `server-port=25599`, `server-ip=127.0.0.1`,
   `level-type=minecraft\:flat`, `generate-structures=false`, `spawn-protection=0`, `difficulty=peaceful`,
   `spawn-monsters=false`, `view-distance=4`, `simulation-distance=4`, `enable-rcon=true`, `rcon.port=25598`,
   `rcon.password=local-test-only`.
3. `eula.txt`: accepting the Minecraft EULA is the owner's call, not the tests'.
4. `java -Xmx2G -jar paper.jar --nogui`, then once per world: `node run.js build`.

`MC_RCON_HOST`, `MC_RCON_PORT`, `MC_RCON_PASSWORD`, `MC_HOST` and `MC_PORT` override the defaults above.

## Running

```bash
node run.js lanes            # the obstacle course, every lane (or name some)
node run.js crops            # crossing a ripe field six ways: nothing trampled
node run.js items            # dying among decorations: everything reachable comes back
node run.js gather           # cutting a grove whose logs fall into awkward spots
node run.js harvest          # harvesting and replanting a plot: nothing trampled, nothing left
node run.js actions          # every packet-level action, checked on the server (eat, store, sleep, boat…)
node run.js safety           # keeping a herd: named animals and babies spared, culling within its rules
node run.js survival         # night fights, a creeper shot from range, an enderman left alone, being hit
node run.js trip             # 250 blocks, far past the loaded chunks
node run.js players          # following a walk and through the hard lanes, coming, guiding, losing one, seeking, where she looks
node run.js orders           # every order: smelting, gathering, giving, storing, every crop, a farm, a patrol
node run.js all              # all of it (about 45 minutes); exits 1 on any failure
```

**The Pi.** `MC_CPU_FACTOR=3.5 node run.js all` divides the pathfinder's time budgets by 3.5, about how much
slower a Pi 5 runs JavaScript than a desktop: what only fails there (a search out of time) fails here. Run it before
shipping anything that makes a search heavier. `MC_TRACE_PATHS=1` logs every slow or unfinished search.

`-v` prints her events and logs as they come. The body runs with `MC_TRACE_MOVES=1`, so every server correction
(the server putting her back where she was) is counted: a lane passes only with none.

Trampling and item rolls are chance: a fix for one of those passes `all` twice in a row, not once.

**Her chat.** `node chat-test.js` (about 3 minutes, not part of `all`) runs `MinecraftBodyService` itself, the real
pools and rules with Discord left offline (`tools/mc-chattest`, built by the script), and six stand-in players talk to
her: questions, intents with Papa's and Tata's twins, insults, Inabot, a favour, a whisper, talk about her, an order,
the listening window, a burst of hellos, the conversation streak. Each line she says is traced back to its Discord
pool, and every line must be plain game chat. Her answers are typed (a delay), so scenarios leave a few seconds
between them.

## Files

- `course.js`: the lanes, one kind of terrain each, as lane-relative block commands.
- `run.js`: building, the lanes and the other runs, the command line.
- `actions.js`, `orders.js`, `safety.js`, `survival.js`, `players-test.js`: the scenarios beyond the course.
- `chat-test.js` and `../mc-chattest` (the service alone, writing every pool to `pools.json`): her chat in game.
- `body.js` (her body as a child process), `players.js` (stand-in players), `query.js` (the world, through RCON).
- `rcon.js`: a minimal RCON client (also usable alone: `node rcon.js "cmd" ...`).
- `accuracy.js`: arrows needed for a creeper that can't move, at a few distances (her aim).
