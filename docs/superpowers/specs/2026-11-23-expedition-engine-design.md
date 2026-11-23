# The expedition engine

Design agreed with the owner on 2026-11-23 (brainstorm). Not implemented yet. It's the engine half of
project 2 in `2026-11-23-plynling-rpg-design.md`; the place it ships with is
`2026-11-23-combe-aux-mousses-design.md`. The 10-28 expeditions design holds except where those two, or
this, change it. Decisions are the owner's unless marked *(my call)*: proposed in the brainstorm, not
contested, but never chosen explicitly.

## Intent

Let a Plynling go on an expedition into a place written as text files, played on the spot in one
public message, and come back with discoveries: creatures for the person's notebook, a mushroom, XP. The
first place is the Combe aux Mousses. Later places reuse the engine and only add files.

## Order of work and review gates

Your rule holds: content is reviewed before any code (Combe design; the content process in
`plynling-events.md`).

1. **The writing sheet** (the Combe's French text) and **the art sheet** (eight creatures and a silhouette),
   both for the owner's review. **Both approved on 2026-11-23.**
2. **The engine**, which may be built meanwhile against test files in the checks project. It doesn't need the
   real content until step 3.
3. **The real `.place` file**, copied word for word from the approved sheet, then the simulation on it. Its
   numbers are set by the simulation.
4. **The release**, once both are in. It's one minor version.

## Approaches considered

- **The fight system with expeditions, or after?** The owner chose **expeditions first**. The fight system
  (turn-based, moves, signature moves, training teaching moves, later Plynling-vs-Plynling) gets its own
  design and release right after. Until then, the Combe's two fights (the Hérisson's bluff, the Lucane's
  shove) are **short sequences of stat tests**, and the fight system replaces them later. Shipping them
  together would have delayed exploring until the move pool and its balance were done.
- **How a place is written:** its own file format (like visits' `.visit`), reusing the story reader and
  tags with its own loader and validator *(my call, agreed)*. The alternative, place scenes as ordinary story
  arcs on the event engine, doesn't fit: the event engine plays one step a day from the card, an expedition
  plays a few scenes in one sitting.

## Goals, non-goals

- **Goal:** a trip of a few scenes in one sitting, odds shown, a safe option everywhere, never death
  (10-28, as amended below).
- **Goal:** a person's creatures' notebook that grows across trips and Plynlings.
- **Goal:** a place added by writing a file and drawing its pictures, with no engine change.
- **Non-goal:** fights with moves (the next release), groups, more than one place at launch.

## Who can go, and when (10-28, RPG design)

`/plynling expedition place:` (so also `/pl expedition`; one place now, as a choice) *(my call: the option)*.
`/plynling` goes from 17 to 18 subcommands, each group under Discord's 25.

- **Refused** while: no Plynling, dead, frozen, **the mascot**, **a bébé** (`PlynlingRpg.CanTrain`),
  asleep (01:00–05:00 Paris), sick, **already away or on a trip**, not enough Énergie for the place, or **hunger
  not lasting the trip plus a margin** (10-28: it can come home hungry, never starve out there). Every refusal
  comes before anything is spent.
- **Starting spends the place's Énergie** (the Combe: 30) through `PlynlingRpg`'s spend, which rebases.
- **One trip at a time per Plynling** *(my call)*: a unique open run per Plynling, enforced by the database (a
  filtered unique index, as for one living Plynling per person).

## The run — one public message (agreed, with the sketch below)

```
┌ 🌿 La Combe aux Mousses — Pwet · scène 2/3 ────────────────┐
│ [Plynling sprite]  ❤️‍🩹 Blessures : 1/2 · 🍄 1 · 🪙 8       │
│ « Le furet a filé avec ta trouvaille ! »   (last outcome)  │
│ ─────────────────────────────────────────────────          │
│ Les fougères s'écartent : un gros scarabée couleur         │
│ d'écorce garde un tronc couché…                            │
│   🎲 Force 58 % : il te laisse passer · sinon une bosse     │
│ [Le pousser]  [Le contourner]  [Attendre qu'il dorme]      │
└────────────────────────────────────────────────────────────┘
```

- **Components V2, one public message in the channel where it started, rewritten in place:** the last
  outcome, then the next scene and its options. **Only the owner clicks**; anyone else gets a short private
  « Ce n'est pas ton expédition. » Anyone can watch. Mentions off.
- **Each option shows its stakes line**, like events (`PlynlingEventStakes`): the stat and its odds, what a
  success gives, what a failure costs. Option buttons are **blurple**.
- **After a wound:** « Rentrer » (green: the safe commit) and « Continuer » (red: it can hurt), per CLAUDE.md's
  colours *(my call)*.
- **The header keeps count:** wounds out of 2, the mushroom, cailloux so far.
- **The return** rewrites the message into the trip's summary: what was found, XP (and a level-up), cailloux,
  notebook steps (« 📔 Crapaud-Mousse : Approché ! »), the thread's progress, and « de retour <t:…:R> ».
  **A notebook step is announced only there**, never in a separate post (agreed).
- **Every handler defers first** (it reads and saves before it can answer), and the custom-ids carry the run
  id and a step number, so a click on a stale message (a step already played) answers « déjà joué » and
  applies nothing *(my call)*. One verb per button row (CLAUDE.md).

## The trip's shape (Combe design; rules *(my call)* unless noted)

- **Arrival** (one of the place's arrival variants), **N scenes** (the place says: the Combe 3), **return**.
- **The draw** is hashed from the run id (`StableRoll`), stored on the run as the list of scene keys, so a
  restart replays nothing. It follows the place's limits: the Combe allows at most two encounters, one thread
  step and one mushroom a trip, avoids the person's last trip's scenes, and lowers the weight of a creature
  whose three steps are done. A scene drawn later that depends on the trip so far (the Furet needs a find
  earlier on this trip) is picked when its turn comes, not up front, still hashed.
- **A wound** comes from a failed option that says so. After a wound, « Rentrer / Continuer ». **The second
  wound brings the rescue** (owner, every expedition): the rescue scene (Asha in the Combe), the trip's
  mushroom left behind, XP, cailloux and steps kept, the « Tout cabossé » modifier and some stress.
  A single wound brings « Égratigné » home *(my call: names; values below)*.
- **Abandoned:** no click for **15 minutes**, and the run heads home with what it found, taking no new risk (10-28).
- **Rolls:** every test is `PlynlingRpg.BodyChance` for a body stat, or the events' formula for a mind stat
  (`PlynlingEventEngine.Chance`), against the option's difficulty, rolled with `StableRoll` from the run id and
  the step. The odds shown are the real ones for this Plynling.
- **Trait-only options** (`[needs trait k]`) work as in story files, shown with the trait's icon (Combe design).

## Away — the travel time (10-28)

- From the start, the Plynling is **away for the place's duration** (the Combe: 1 h), whether the run ended
  early or not. `ReturnsAt` is on the run. Away is **derived from the run, never a flag on the Plynling**
  *(my call)*: a Plynling is away while a run of its has `ReturnsAt` in the future.
- **While away:** no feeding, petting, bathing, play, visits (either way), forage, train or second trip. Each
  refusal names the return time. **Events keep coming** (10-28 builder call). The card shows
  « 🌿 En expédition · retour <t:…:R> » in its status text and **hides its care buttons**.
- **The trip's rewards land when the run ends** (finished, home early, rescued or abandoned), not at
  `ReturnsAt` *(my call: simpler, nothing to sweep at the return, and the summary can show them)*. Only the
  away time waits.

## The notebook (agreed)

- **`/inventory notebook [user]`**, open to anyone like the collection book (`/inventory` goes from 8 to 9).
  **One page per place**: each creature with its three steps (👁️ Aperçu, 🤝 Approché, 💚 Apprivoisé). A creature
  never met shows as **« ❔ ??? »**, a silhouette with no name, until its Aperçu. A select opens a creature's
  page: its picture, a few lines, and each step with the Plynling that earned it and when.
- **Storage:** one row per (guild, user, creature, step), with the Plynling's id and name *(my call: the name
  copied, so a step earned by a Plynling long gone still reads right)* and the date. Unique on (guild, user,
  creature, step), so a step is earned once. Keys (`place.creature`) are stored and append-only.
- **The pictures:** 32×32 pixel art in the Plynlings' style, drawn in `tools/` and exported once, reviewed by
  the owner as a sheet before shipping: eight creatures and one silhouette. **Approved on 2026-11-23** after a
  second pass; drawn by code in `tools/creature-art/creatures.py`.

## The place file

`ProjectSYNCS/Stories/Expeditions/<place>.place` *(my call: one file per place)*. It's read with the story
format's reader and tags, by its own `PlaceParser` / `PlaceValidator` / `PlaceCatalog` (like `Visits/`).
A load error stops the bot, in the same `try` as the story and visit catalogs. Fixtures in the checks
project hold every word.

It declares, beyond what story scenes have:

- **The place:** key, name, Énergie, duration, scene count, the minimum level, and the draw limits.
- **Its creatures:** key, name, a line about them, their picture.
- **Scenes by kind:** `arrival`, `encounter <creature>`, `find`, `obstacle`, `thread <step>`, `return`, `rescue`.
  A scene has its text, then 2–4 options, **one of them with no roll**.
- **New option words:** `[wound]` on a failure, `[step <creature> seen|close|tamed]` (a notebook step),
  `[find]` (the trip's mushroom, from the place's rarity split), `[lose find]`, plus conditions:
  `[if step …]` / `[needs step …]` on the person's notebook, and `[if found]` on the trip so far. The existing
  effects keep working: `stress`, `modifier`, `cailloux`, `lift`, `grow`, marks for the thread's unlocks, and the
  epithet at the thread's end.
- **Validator:**
  - every scene has a free option;
  - every creature can earn all three steps somewhere;
  - the draw limits can always fill a trip;
  - every `[needs step]` is reachable;
  - Discord's caps, measured as for stories (32-character names, each agreement at its longer form).
- **A sequence of tests** (the two fights until the fight system) is written as a scene whose options lead to a
  second short scene within the same step, so it needs no special word *(my call; the builder may find a
  simpler shape)*.

## Rewards and numbers *(my call, all tuned by the checks' simulation)*

- **XP:** about 30 a Combe trip, plus a few for each test passed, **up to 40**, given through `PlynlingRpg`
  (the curve and the level-up line are the RPG foundation's; a level-up gets its public line, as with drills).
- **The XP ceiling** (from the owner's year-long pace): **no activity gives more than about 1.33 XP per
  Énergie**. A full day spent on any mix (trips now, fights later) then stays at or under the 180 XP a day the
  year was set on, and levels can never be taken back. Drills (0.67) stay slower, as decided.
- **Cailloux:** 5–15 a trip, under a new metric key **`earn.expedition`**. The trip itself counts as **`act.expedition`**.
  Both are stored keys, so append-only.
- **The mushroom:** a new `ItemCatalog` draw, Champignons only, rarity **25 / 40 / 28 / 7** for the Combe (the place
  sets it), seasons as now. Granted through `InventoryService.GrantAsync`, so a completed set pays as usual.
- **The simulation** (checks project) plays thousands of Combe days for a Plynling at full Énergie, choosing at
  random and choosing best:
  - XP per Énergie is **at least a drill's (0.67) and at most 1.33**, and the RPG pace check is re-pinned with
    trips in: a full day of best play never reaches level 100 before about day 360;
  - **a day's cailloux, every Énergie point spent on best-played trips with finds sold and sets turned in,
    stays under three average `/work` shifts** (the daily norm the passive cap already uses, `economy.md`). If
    it doesn't, **finds become rarer per trip, never less rare** (owner);
  - how often Asha comes is printed, for the owner to judge.
- **Modifiers** (append-only keys): « Égratigné » (a few hours, −1 to the stat that failed) and « Tout cabossé »
  (a day, −1 to every body stat, slower happiness). Values are tuned by feel, not stored anywhere but the catalog.

## Where things live *(my call)*

- `Helpers/Plynling/Expeditions/`: the pure rules (the draw, the step, the wound and rescue logic, the rewards).
  `Services/Plynling/PlynlingService.Expeditions.cs`: the units of work, each through `UnitAsync`.
- `ExpeditionRun` (one row per trip) and `NotebookEntry`, both in the existing database, by migration.
- `Commands/Plynling/ExpeditionCards.cs` (static, measured: the 40-component and 4000-character caps) and
  `Interactions/Components/ExpeditionHandler.cs`. `/inventory notebook` in `InventoryModule`, and its pages in
  `NotebookCards`.
- **`ExpeditionSweepService`**, its own loop every minute: it closes runs left alone for 15 minutes and rewrites their
  message. It catches errors per run (CLAUDE.md). It skips silence mode, because finishing a run is the answer to a
  command, not unprompted chatter.
- A new subsystem note `docs/agents/plynling-expeditions.md`, a CLAUDE.md row, `/plynling help`, `/help`, the
  README, a **minor** bump with a CHANGELOG entry.

## Edge cases *(my call)*

- **« Rentrer / Continuer » is not a scene:** "every scene has a free option" stands as written.
- **One scene gives at most one notebook step.**
- **A freeze mid-run** (a staff freeze; a self-freeze is refused while away) ends the run as if left alone: it
  heads home with what it found. **Death mid-run** can't come from starvation (the hunger check at the start
  covers the trip), and a run whose Plynling died or was abandoned anyway is closed with nothing given, and
  its message says so.
- **The thread's progress** is stored as unlock marks on the Plynling (the existing `PlynlingMark`), which
  replaces the Combe design's "story flags".

## Constraints

- **Concurrency:** a click and the sweep can meet on the same run. The run row carries a concurrency token
  (like `Plynling.Version`), so the slower save throws, and `UnitAsync` reruns it and finds the step already played.
- **SQLite:** no `DateTimeOffset` comparison in SQL (open runs are found by flag, dates compared in memory);
  snowflakes `.HasConversion<long>()`.
- **"Not playing is free":** an abandoned run takes no risk; the rescue never kills; refusals spend nothing.
- **Lore and writing:** the Combe design's constraints hold for every line the engine prints.

## Left to the builder

Table shapes, custom-id verbs, the exact place-file grammar, the hunger margin, the modifiers' exact
values, the rarity of a creature's reappearance, and how a test sequence is written.
