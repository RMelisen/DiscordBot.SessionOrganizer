# Plynling Storylines — the story file format and the engine's data model

Design agreed with the owner on 2026-10-29. Not implemented yet. It is step 2 of « Still to design »
in `2026-10-28-plynling-storylines-design.md`, which this document builds on and never contradicts:
read that one first. Decisions are the owner's unless marked *builder's call* (proposed during the
session, not contested, but never explicitly chosen).

## Intent

Every storyline arc is written in a story file, and **the file is the review sheet**: what the owner
reads is what runs. The owner reads arcs in **Visual Studio or Claude Desktop** — plain text, no
rendered preview, no syntax colours — and **edits some lines by hand**. So the format must read as a
screenplay first and parse second, and a hand edit that breaks something must say so loudly, with
the file and the line.

The same format and data model are later reused by the chronicle, the calendar, the crises and the
expeditions' scene pools (one scene engine, one story file format, one memory store).

## Goals, non-goals, anti-goals

- **Goal:** an arc file the owner can review top to bottom without opening C#, and edit without
  fear.
- **Goal:** every rule the storylines spec sets (branch and bottleneck, gated options shown locked,
  rolls, stress, in-character decisions, handed-over moments, marks, interruptions, living messages)
  is expressible in the file.
- **Goal:** a **path report** the owner can read before the dev-guild playthrough (owner's choice).
- **Non-goal:** any arc's content — Arrival is the next session. The sample below is a placeholder.
- **Non-goal:** the chronicle and calendar engine, the crisis runner, the expedition runner. This
  document only reserves their file kinds (see *Later kinds*).
- **Non-goal:** retiring the old catalog; the old engine stays untouched next to the new one.
- **Anti-goal:** a story that needs C# to be understood, or behaviour that lives outside the file.
- **Anti-goal:** a typo that silently does nothing (a gate never true, a memory never given).

## The sample (layout accepted by the owner)

```text
# arc: arrival
title: L'arrivée
family: milestone
stage: baby
trigger: adoption
public: Une pousse vient de sortir de la Souche.

## memories
lichen.came_first    | lichen  | private | +5 | Est venue à elle d'elle-même, le premier matin.
lichen.ran_to_wood   | lichen  | private | -3 | A filé vers le Bois Sourd avant même de savoir marcher.

## keepsakes
lichen_pebble | Le galet de la Mère Lichen | Un galet de la Sommeille, tiède, que la Mère Lichen a posé près de {A} le premier soir.

## scene: first_steps
title: Les premiers pas
pace: next_day
public: {A} hésite au bord de la Souche.

La pluie s'est arrêtée dans la nuit. {A} est {a:sorti|sortie} de la Souche avant l'aube, et reste là, collé{a:|e} à l'écorce.
[if trait curious] {A} a déjà goûté une feuille, puis une deuxième.
À quelques pas, une vieille femme grise est assise sur une pierre. Elle ne dit rien. Elle attend.

* to_her | Aller vers la vieille femme
  [lean sociability +2]
  {A} fait trois pas, s'arrête, en fait quatre autres. La vieille femme ne bouge pas, mais quelque chose se détend dans ses épaules.
  [flag came_to_her] [remember lichen.came_first] [-> the_cabin]

* stay | Rester contre la Souche
  [lean boldness -1] [stress rowdy 10]
  {A} se colle à l'écorce jusqu'à midi. La vieille femme non plus ne bouge pas. Elles sont deux à savoir attendre.
  [-> the_cabin]

* wood | Partir vers les grands arbres  [needs trait curious]  [roll learning 7]
  [lean boldness +2]
  ? success
    {A} trottine jusqu'à la lisière, renifle, et revient de son propre chef, le bout du chapeau trempé.
    [-> the_cabin]
  ? failure
    {A} s'enfonce sous les fougères et ne retrouve plus la lumière. C'est elle qui vient le chercher.
    [remember lichen.ran_to_wood] [stress 10] [-> lost_at_edge]

## ending: welcomed
title: Le premier soir
...
[keepsake lichen_pebble] [cailloux 50]
```

## What the first build covers (owner's choice)

**Tools first, content after.** The first build is **the format, the parser and the checker with its
path report** — enough to write Arrival as a file, read its report and review it before any engine
exists. The runner, the tables, the living message, the card entry and the `/debug` tools come in a
later build.

- *Builder's call:* the parser and the parsed model are pure code under
  `ProjectSYNCS/Helpers/Plynling/Stories/` (checkable without a gateway, used later by the engine);
  the checks project reads them the way it reads `MessageCues`. Story files live in
  `ProjectSYNCS/Stories/` from the start.
- *Builder's call:* the startup load (« failing loudly ») is wired with the engine, not now: until
  then the checker is the only reader, and nothing changes for players. Pure helpers compiled into the
  bot still make it a bot change, so the build takes a **patch bump**.
- The data model below is designed now so the format fits it; its tables and migration ship with the
  engine.
- A tiny **test arc** (not Arrival) exercises every tag, so the parser and checker can be finished
  and verified before any real content.

## The format

### Files

- *Builder's call:* one file per arc under `ProjectSYNCS/Stories/`, in a subfolder per family
  (`Stories/Milestone/arrival.story`), UTF-8, extension `.story`. Copied to the output and publish
  directories like `Assets/` (a `<None Include>` in the csproj), so the add-on image carries them.
- *Builder's call:* a shared **`Stories/village.cast`** declares the cast once — character key
  (`lichen`, `frene`, `intendant`…), given name, byname — from `docs/village-lore.md`. Memory
  holders and standing refer to these keys. A character added to the bible gets a line here.
- Editing a story file changes the bot: it needs the usual **patch bump** and changelog line (new
  text is "lines" in CLAUDE.md's sense). An arc's first file is part of a minor batch.

### Structure

- `# arc: <key>` opens the file, followed by `key: value` header lines.
- `## <section>` opens a section: declarations (`memories`, `epithets`, `keepsakes`), `scene: <key>`,
  `ending: <key>`.
- *Builder's call:* lines starting with `//` are **author notes**, ignored by the parser — the place
  for "why this branch exists" comments to the owner.
- *Builder's call:* prose may wrap across lines; consecutive lines join into one paragraph, a blank
  line starts a new one. Indentation only marks what belongs to an option or an outcome.

### Header

| Field | Meaning |
|---|---|
| `title` | The arc's name (card, journal, chronicle page). |
| `family` | One of the nine families of the storylines spec (`milestone`, `friendship`, `love`, `rivalry`, `village`, `personal`, `hardship`…). |
| `stage` | Stages it may start in (`baby`, `teen`, `adult`, `elder`, or several). |
| `trigger` | What brings it: `adoption`, `stage teen`, a relation event, a real game event, or `draw` for drawn life arcs. |
| `shelf` | For a triggered arc: how long it stays queued before it means nothing (`shelf: 7d`). |
| `weight` | For a drawn arc: its draw weight, with `[if …]` conditions that raise it (traits, passion, memories, season). |
| `requires` | Conditions to be eligible at all (same vocabulary as gates). |
| `other` | For a social arc: who the other Plynling is (`other: friend`, `other: rival`…). |
| `public` | The line that opens the living public message. |

### Declarations

Declared once in the arc that introduces them; scenes refer to them by key. Keys are **global**
across all story files (another arc may read `lichen.came_first`) and stored, so append-only.

- **Memories** — `key | holder | private/public | standing shift | wording as the village tells it`.
  The key is `<character>.<fact>`; the holder is a cast key.
- **Epithets** — `key | {a:masc|fem}` title.
- **Keepsakes** — `key | name | its story`, with *builder's call* `| hurts` for a keepsake that is a
  scar.
- Flags are **not** declared: they belong to one run of one arc. The checker catches a flag tested
  but never set, or set but never tested.
- *Builder's call:* **another arc may read a finished or interrupted run's flags** (the storylines
  spec lets a hard-times arc read the interrupted arc's flags) with `flag <arc>.<flag>`; the startup
  check verifies that arc sets that flag somewhere. A flag read from another file is therefore
  stored and append-only like any key. Facts the *village* knows still go through memories.

### Scenes

```text
## scene: <key>
title: …
pace: next_day | now | +6h
public: <teaser of the dilemma, never the options>
bump                      (optional: post a short new message linking to the living one)
handover                  (optional: this decision goes to the other Plynling's owner)

<prose, with [if …] lines>

* <option> …
```

- `pace` is when this scene becomes available after the previous decision: `next_day` (the usual
  one decision a day), `now` (a tense moment chained right away), or a number of hours.
- A scene may end with no options and a single `[-> next]` (a narration beat), *builder's call*.
- *Builder's call:* **in a `handover` scene, the decider is the other Plynling.** Its gates,
  `[lean]` and `[stress <trait> n]` read the decider's traits and stats, and stress costs land on the
  decider (the other owner's own choice; dropped when it decided alone, as always). Every other
  effect lands on the arc's owner, except those prefixed `other`: `[other remember <memory>]` and
  `[affinity ±n]`, which already touches both. Anti-griefing stays: outside a handed-over moment,
  nothing lands on the other Plynling but the relation.

### Conditional prose

`[if <condition>]` at the start of a line makes that line conditional; `[unless <condition>]` is its
opposite. *Builder's call:* no inline alternatives inside a sentence — write two `[if]`/`[unless]`
lines instead, so every variant reads as a whole sentence.

### Options

```text
* <key> | <label>  [needs …]  [roll <stat> <difficulty>]
  [lean <axis> ±n] [stress <trait> n] [owner-only]
  <outcome prose>
  [<effects>] [-> <scene or ending>]
```

- **Gate:** `[needs <condition>]`. A locked option is **shown disabled** with its requirement
  (« 🔒 Curieux »). The lock text comes from the condition (a trait's name, a memory's wording); a
  flag gate gives its own text: `[needs flag owes_magpie "La pie vous doit une faveur"]`.
  *Builder's call:* `[secret]` hides an option while it is locked, for the rare surprise.
- **Roll:** `[roll <stat> <difficulty>]` is today's `EventChallenge` (odds shown on the button).
  The outcome then splits into `? success` and `? failure`, each with its own prose and effects.
- **In character:** `[lean <axis> ±n]` are today's AI weights (`AiAxis`), used when it decides alone.
- **Stress:** `[stress <trait> n]` costs n when the Plynling holds that trait, as today; an arc's
  heaviest moments may go to about 60.
- `[owner-only]` — never picked when it decides alone (big gambles, declarations).
- Labels ≤ 80 characters, including what the button adds (odds, lock).

### Conditions

One vocabulary for `[if]`, `[unless]`, `[needs]`, `requires` and `weight` — *builder's call* on the
exact list, at least:

`trait <key>` · `stage <stage>` · `flag <key>` · `memory <key>` · `stat <stat> >= n` ·
`standing <character> >= n` · `relation <bond>` (with the other) · `other trait <key>` ·
`season <season>` · `passion <key>` · `keepsake <key>` · `epithet <key>` · `ended <arc>.<ending>`
(a past arc's ending) · `chain <key>` (an expedition chain, later) · `gender m|f`.
Several tags on one line all must hold; `not` negates one.

### Effects

| Tag | Effect |
|---|---|
| `[-> <key>]` | Next scene, or an ending. Every outcome has exactly one. |
| `[flag <key>]` / `[unflag <key>]` | Set or clear a run flag. |
| `[remember <memory>]` / `[forget <memory>]` | Give or remove a village memory. |
| `[reveal <memory>]` | A private memory becomes public (gossip, only in a written scene). |
| `[stress n]` / `[stress -n]` | Stress gained or relieved. |
| `[modifier <key>]` | Today's modifiers. |
| `[grow <stat>]` | Today's `GrowStat` (Practice, rarely for good). |
| `[affinity ±n]` | With the other Plynling, through today's relation code. |
| `[cailloux n]` · `[item <key>]` · `[lift <need> x]` | Today's rewards; cailloux up to about 100 for a whole arc. |
| `[epithet <key>]` · `[keepsake <key>]` · `[unlock <arc>]` | Marks. |
| `[trait <key>]` · `[trait <old> -> <new>]` | Gain or swap a trait; endings only, one per arc. |

The "not playing is free" rule stays in code, as today: when it decided alone, stress gains,
negative modifiers, scars and coping traits are dropped; the narrative goes on in full.

### Endings

```text
## ending: <key>
title: …
<closing prose>
[marks]
```

- 3–5 per arc. Two special endings, by key:
  - `## ending: interrupted` — **required**: the short closing paragraph when the Plynling dies
    mid-arc.
  - `## ending: loss` (or a scene `loss` that leads to endings) — **required** for love and
    close-friendship arcs, optional elsewhere: where the arc jumps when the other Plynling dies or is
    abandoned.

### Text tokens

Today's: `{A}`, `{B}`, agreements `{a:m|f}` / `{b:m|f}` / `{p:…|…}` from `PlynlingVisitStory.Expand`,
`{T}` for a new trait. Villagers are named in plain text (their gender is fixed). Since agreements
exist, « il »/« elle » are allowed through `{a:il|elle}`; the checker renders all four gender pairs.

### Public lines

The living message shows each step's `public:` teaser, then a line for what happened. *Builder's
call:* that line defaults to the chosen option's label (« {A} : Rester contre la Souche »); an option
may give its own with `[public "…"]` when the label would read badly or spoil.

## Editing a live arc (owner's choice)

**Plynlings partway through an arc get the new version from their next step on.** That brings:

- **Keys are forever.** Scene, option, ending, memory, epithet and keepsake keys are stored; never
  rename or remove one. A scene taken out stays as `## scene: <key>` with `retired -> <other key>`;
  a run waiting on it moves on to the other scene. *Builder's call:* a removed option stays as
  `* <key> | <label> [retired]` — never offered, still rendered in old stories.
- *Builder's call:* **past steps re-render from the current text**, like today's stories rebuilt from
  the row: a fixed sentence shows in the chronicle page and the journal too.
- At startup, a run waiting on a scene key that no file knows (neither live nor retired) is logged as
  an error and gets its `interrupted` ending, *builder's call*.

## Failing loudly

- **At startup**, *builder's call*: every file is parsed and cross-checked (unknown tag, unknown key,
  undeclared memory, duplicate global key, a `[-> x]` to nowhere, a missing `interrupted` ending,
  bad agreement syntax, a label or text over a Discord cap). Any error **stops the bot**, naming the
  file, the line and what is wrong. A broken story never reaches a player.
- **The checker** (in `tests/ProjectSYNCS.Checks`, *builder's call* from the storylines spec) does
  the slower walks: every ending reachable, a stress-free path when deciding alone, all four gender
  pairs rendered, every flag tested is set somewhere and vice versa, no scene unreachable, Discord
  caps on every rendered variant.

## The path report (owner's choice)

The checker writes, per arc, a plain-text report the owner reads before the dev-guild playthrough:

```text
L'arrivée (arrival) — 6 scènes, 4 fins, 11 chemins

Fins atteignables
  welcomed      7 chemins   le plus court : 4 étapes
  wary          2 chemins
  lost_and_found 1 chemin   seulement via « wood » raté (curious)
  ⚠ alone_at_dawn 0 chemin  — jamais atteinte

Décidé seul (sans le propriétaire)
  welcomed : first_steps/to_her → the_cabin/listen → ... (stress 0) ✓
  pire cas en personnage (rowdy) : stress 0, fin wary

Chemin 3 / 11 → wary
  1. Les premiers pas — « Rester contre la Souche »   stress rowdy +10
     {A} se colle à l'écorce jusqu'à midi. La vieille femme non plus...
  ...
  Marques : épithète « La Méfiante » · 20 cailloux
```

- The summary: reachable endings, an unreachable one flagged.
- The deciding-alone check: a stress-free path exists, and where an in-character Plynling tends to
  end.
- Every path written out as reading: labels, outcome prose, costs, locks, marks.
- Conditional prose is shown once in the masculine form with its variants marked, so the report does
  not explode in length.
- *Builder's call:* run as `dotnet run --project tests/ProjectSYNCS.Checks -- story <arc>`, written
  to a gitignored folder next to the checks project.

## The engine's data model

*Builder's call* throughout on exact column names; the shape below is what the rules need. Every
snowflake `.HasConversion<long>()`; every date window filtered in memory; every key stored as a
string and append-only.

- **`StoryRun`** — one arc lived by one Plynling: `PlynlingId`, `GuildId`, `ArcKey`,
  `OtherPlynlingId?` (set null if abandoned), `StartedAt`, `EndedAt?`, `EndingKey?`, a state
  (running, ended, interrupted, cancelled), `Flags` (`key;key`), the living message's channel and
  message ids, a concurrency token.
- **`StoryStep`** — one decision: `RunId`, `SceneKey`, `AvailableAt`, `LiveSince` (frozen time
  restarts the 36 h window, like `PlynlingEventEngine.DueAt`), `DeciderPlynlingId` (the other one on
  a handed-over moment), `OptionKey?`, `DecidedAlone`, `ResolvedAt` as a concurrency token (never
  applied twice), and what the story must not recompute: chance shown, roll result, stress delta,
  bond before/after, `GrewForGood`, gained trait.
- **`VillageMemory`** — `PlynlingId`, `GuildId`, `MemoryKey`, `AcquiredAt`, `RevealedAt?` (a private
  memory made public by a scene), `RunId?` (where it came from; an expedition may give one later).
  Holder, wording and shift come from the declaration, so standing is **computed**, never stored.
  *Builder's call* on the formula, fixed before the first arc so gates never move: a Plynling's
  standing with a character is the sum of the shifts of the memories that character holds about it;
  a **public** memory also gives every other character **±1** in its direction. Unbounded; the
  « Village » view shows it in words (bands), never the number.
- **`PlynlingMark`** — epithets, keepsakes, unlocks and scars: `PlynlingId`, `Kind`, `Key`,
  `RunId`, `AcquiredAt`.
- **`StoryTrigger`** — the queue of triggered arcs waiting for the slot: `PlynlingId`, `ArcKey`,
  `OtherPlynlingId?`, `QueuedAt`, `ExpiresAt` (the shelf life).
- Rolls and in-character picks are hashed with `StableRoll` from the step's id, never a `Random`.
- Writes follow today's events: one `PlynlingService` unit of work per decision (growth, relation,
  badges, journal, memories and marks in one save), the sweep opening a scope per Plynling and
  catching per item.

## Old pulses next to arcs

*Builder's call*, reading the storylines spec's "when nothing is eligible, an old-catalog pulse fills
the day": **while a personal arc runs, no old pulse is drawn** for that Plynling (its day is the
arc's step); during the 3–5 day gap and whenever no arc is eligible, old pulses fill the days as
today. Old follow-ups, responses, breaks and on-actions already queued still come. An arc step does
not count toward the old "3 pulse events waiting" skip.

## Later kinds (reserved, not designed here)

The same file grammar, with another `#` kind and a few more header fields, *builder's call*:

- `# beat:` — a chronicle beat: a firing window, branching on the guild's standing with a character
  or on a memory count.
- `# calendar:` — a yearly arc, reading last year's outcome.
- `# crisis:` — every Plynling of the guild, endings picked by share, a daily mood line.
- `# pool:` — an expedition place's scene pool, with wounds and loot.

The parser should not need rewriting for them, only new header fields and effects.

## Left to the builder

The parser's implementation, the exact condition and effect lists beyond those above, column names
and indexes, how the card's « Événement » entry lists arc steps next to old events, the living
message's rendering, the `/debug` tools for arcs (start an arc, jump to a scene, expire a step), and
where `docs/agents/` documents the new engine (a new `plynling-storylines.md` with a row in
CLAUDE.md's table is the natural place).
