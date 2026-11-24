# `/plynling play` minigames and `/arcade guess` — design

## Intent

`/plynling play` should be quick: one click per move, no typing. Plus ou moins (guess 1–100 in
7 tries) needed a modal per guess, and the round trips made it drag. It leaves the Plynling and
becomes an arcade bet; two quick button-only games take its place.

## Goals / non-goals / anti-goals

- Goal: every `/plynling play` game is buttons only, a handful of clicks.
- Goal: plus ou moins lives on in `/arcade` with 6 tries — a real bet (perfect halving finds the
  number only 63 times in 100, where 7 tries always did).
- Non-goal: changing play's rewards, hourly limit, expiry or happiness effects.
- Anti-goal: a game that waits on a timer or a typed answer (the latency it replaces).
- Anti-goal: a memory game unplayable because the mushroom emojis did not load (all fall back to 🍄).

## Approaches considered (owner picked 1 and 3)

1. **Le sac de cailloux** — higher/lower on numbered pebbles. Picked.
2. Pair ou impair — one click, pure luck, near-copy of cache-cache. Dropped.
3. **Mémoire** — repeat a shown sequence. Picked, with the mushroom pack's emojis
   (30 species, `Assets/Mushrooms`, `ItemEmojis.For("col.<slug>")`).

A second round of ideas (owner picked 1 and 2): **Le dernier caillou** (Nim) and **Chasse au
trésor** (dig on a grid with hot/cold hints) added. Dropped: la pile de feuilles (cache-cache on a
bigger grid), la cueillette (the pebble bag's streak again).

## `/plynling play` — six games, drawn at random

Cache-cache and pierre-papier-ciseaux unchanged. `PlynlingGame` becomes
`{ HideAndSeek, RockPaperScissors, PebbleBag, Memory, LastPebble, TreasureHunt }` (in memory
only, never stored, so the enum may change freely). Same session/`Gate`/`FinishPlayAsync` path as
today. Each game is drawn with equal chance (Memory excluded when its emojis are missing).

**Le sac de cailloux** (`plyn:bag:{id}:{up|down}`)
- The Plynling draws a pebble numbered 1–10 and shows it. ⬆️ « Plus haut » / ⬇️ « Plus bas ».
- It draws the next one: right → streak +1, wrong → lost. A tie draws again (no penalty).
- Three right in a row wins (~43 % for a careful player, simulated). The card shows the pebbles so far.

**Mémoire** (`plyn:memogo:{id}`, `plyn:memo:{id}:{slot}`)
- The card shows a sequence of **4 distinct mushrooms** (emoji + French name) and one blurple
  button « J'ai retenu ! ». No timer: the player hides it when ready.
- The card then shows **6 buttons** (the 4 + 2 decoys, shuffled, two rows of 3), each emoji + name.
  Click them in order: a right click marks the progress line, a wrong click loses at once.
  All 4 right wins. Ids carry the slot, so each is unique.
- Drawn only when at least 6 mushroom emojis are loaded; otherwise play draws among the others.

**Le dernier caillou** (`plyn:nim:{id}:{n}`)
- A pile of 12–16 pebbles (random). The player moves first; each turn takes 1, 2 or 3; whoever
  takes the last pebble wins. Buttons « 1 » « 2 » « 3 » (blurple; one taking more than is left is
  disabled — ids differ by `n`).
- The Plynling answers in the same click: it plays the winning move (leave a multiple of 4)
  3 times in 4 when one exists, otherwise a random legal take. The card shows the pile and the last
  two moves.
- Simulated (2026-11-24): a random clicker wins ~16 %, a player who knows the trick ~77 %; a
  casual player lands between. The checks hold both within ±5 points.

**Chasse au trésor** (`plyn:dig:{id}:{cell}`)
- The Plynling buried a gland on a 4×4 grid (16 grey buttons, four rows). **3 digs.**
- A dig on the gland wins. Otherwise the cell shows a hint by Manhattan distance and is disabled:
  1 « brûlant » 🔥, 2 « chaud » ♨️, 3+ « froid » ❄️. Three misses lose; the gland's cell is revealed.
- Simulated (2026-11-24): an optimal digger wins ~62 % (four bands gave 81 %, too easy).
- Components: 1 container + section + text + thumbnail + 4 rows + 16 buttons = 24 of 40.

## `/arcade guess` — plus ou moins with a stake

- `/arcade guess [stake]`, stake 0–`Duel.MaxStake` (500), checked against the wallet at start.
- 1–100, **6 tries**, the existing `GuessModal` per guess (moved from the Plynling handler), card
  lists every guess with « plus » / « moins ». Only the player's clicks count. One game per person.
- Win returns **stake × 1.4** (rounded down): a perfect player gets back ~88 % on average, in line
  with arcade's ~90 % rule. Loss: the stake. Settles through `ArcadeService.SettleAsync`; a human
  win increments the `arcade` badge. 5 idle minutes loses (like the boards).
- Follows the « newer games » shape: `ArcadeGame` subclass, `ArcadeTables.TryOpen`/`End`,
  card in `PartyCards`, pure rules in `Helpers/Arcade`.
- Arcade goes from 11 to 12 subcommands.

## Left to the builder

SYNCS / Plynling lines for the new games (style guides apply), card wording, whether decoys avoid
look-alike species, exact command description text. Minor bump (new subcommand), changelog, `/help`,
`/plynling help`, README, `plynling.md`, `arcade.md`, checks for the pure rules.
