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
node run.js all              # all of it (about 7 minutes); exits 1 on any failure
```

`-v` prints her events and logs as they come. The body runs with `MC_TRACE_MOVES=1`, so every server correction
(the server putting her back where she was) is counted: a lane passes only with none.

Trampling and item rolls are chance: a fix for one of those passes `all` twice in a row, not once.

## Files

- `course.js`: the lanes, one kind of terrain each, as lane-relative block commands.
- `run.js`: building, the runs, the reports.
- `rcon.js`: a minimal RCON client (also usable alone: `node rcon.js "cmd" ...`).
