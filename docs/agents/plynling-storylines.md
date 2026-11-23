# Plynling storylines — the story engine

Events written as story files and played by the one event engine (`plynling-events.md`). Design:
`docs/superpowers/specs/2026-11-17-plynling-village-events-design.md`; the file format:
`docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md` (binds, except where the 2026-11-17
design and `2026-11-19-plynling-every-event-a-story-design.md` change it). The world every arc lives in is
`docs/village-lore.md`: fully cosy, every event stands alone, the village never changes. The prose follows
`docs/plynling-writing-style.md`.

**Approach:** at startup each arc is compiled into `EventDef`s; **they are the whole catalog** (since 8.0.0 no
event is hand-written in C#). There is no second engine: draw, deciding alone, stress, cards, freezes, asks,
breaks and cancellation are the existing ones. A story event differs only by what the compiled def carries
(`EventDef.Story`, `EventOption.Source`) and by the knowledge the context holds.

## Files and loading

- **One arc per file** under `ProjectSYNCS/Stories/<Family>/<key>.story`, UTF-8; CRLF, LF and a BOM
  read the same. `Stories/village.cast` lists the villagers stories may name (`key | given name | byname`).
  The folder is copied to the build **and** publish output (csproj `None Include`), so the image carries
  it; `StoryCatalog` reads `AppContext.BaseDirectory/Stories`.
- **Fixtures** under `tests/ProjectSYNCS.Checks/Stories/` are never shipped: `test_everything.story` (what a long
  arc can hold), `test_short.story`, `test_handover.story` (the handover words, which end on `[end]`, so a long arc
  cannot hold them) and `test_break.story` (the break words). **Every effect and condition kind appears in some
  fixture**, or the checks fail: a new word in the format gets a line there.
- **Code**, `Helpers/Plynling/Stories/`: `StoryModel`, `StoryTags`, `StoryVocabulary`, `StoryParser`
  (+ `.Scenes`), `StoryValidator`, `StoryLibrary` (parse + validate a folder), `StoryCompiler`,
  `StoryCatalog` (the lazy, process-wide result), `StoryKnowledge` / `StoryStanding`, `StoryConditions`
  (conditions and prose rendering), `StoryRuns` (flags and history, pure). Beside the engine:
  `PlynlingEventStakes`, `PlynlingEventStory`. The DB half is `PlynlingService.Stories.cs`.
  The walks and the report live in the checks project (`StoryWalker`, `StoryReport`).
- **A load error stops the bot.** `Program.cs` touches `StoryCatalog.Compiled` right after `Build()`,
  before `MigrateAsync`; a `StoryLoadException` logs every error (`file:line: message`) at `Critical`, one per
  line, and rethrows. Why: a half-loaded catalog would silently drop events and strand instances whose
  keys no longer exist. `PlynlingEvents.All` and everything derived from it (`ByKey`, `PulseKeys`…) read
  `StoryCatalog.Compiled`, so **nothing may read the catalog before that load**, and a static initializer
  must not copy the catalog earlier than the catalog itself.
- The validator is the gate: unknown keys, dangling `[-> …]`, a scene without a free option (no needs, not
  owner-only, not secret, no stress cost), ranges, template tokens, and Discord's caps measured with
  32-character names and each agreement at its longer form. **Every problem is reported**, with file and
  line. Parser and validator errors are in English; the path report is in French (the owner reads it as content).

## Keys are forever

Arc, scene, option, ending, memory, epithet, keepsake and flag keys are stored or compared with stored
rows: never rename or remove one. Memory, epithet and keepsake keys are global across files.

- **Event key** = `<arc>.<scene>` (`StoryCompiler.Key`). Old C# keys never contain a dot, so they cannot
  collide, and `StoryRuns.ArcOf` tells them apart. Option keys are kept as written.
- **`was: <old key>`** (one per scene, under `title:`) claims an old C# event's key for the scene that replaced
  it: `StoryCompiler.Aliases` maps it to the scene, so `PlynlingEvents.ByKey` opens an instance saved under it as
  that scene, with its old option keys. Why: instances, journals and ado choices stored under the 50 keys moved
  in 8.0.0 must keep resolving. The key has no dot, is claimed by one live scene in the whole catalog (the
  validator names both), never by a retired scene, and never one from `PlynlingEvents.Removed` (checked).
- **Compare stored keys through `ByKey`, `StoredKeys` or `Current`, never raw.** A stored `EventKey` may be an old
  key: a SQL filter lists `PlynlingEvents.StoredKeys(which)` (live, `was:` and retired keys), anything compared with
  a def's key goes through `PlynlingEvents.Current(stored)` or `ReferenceEquals(ByKey(stored), def)`, and the story
  openers include the aliases of first scenes (`StoryRuns.Openers(All, Aliases)`). A raw comparison lets a
  moved event come back as new, or queue a second break of a level.
- **The frozen check** (`tests/ProjectSYNCS.Checks/OldEvents.txt`, written once from the C# before it moved, never
  edited) holds what a player could tell of each of the 50 moved events; `OldEventChecks` fails if a file drifts
  from it. A mismatch is fixed in the `.story` file. The one approved change (a label over the cap) is listed in
  `OldEventChecks.Reworded`.
- **The first scene is positional**: it never moves and never retires (the opener and the arc's start key
  hang on it; the validator refuses `retired` on it).
- **A retired scene** stays in the file as `## scene: <key>` + `retired -> <other>`. It gets **no def**:
  `StoryCompiler.Aliases` maps its key to the live scene it leads to (following chains), and `PlynlingEvents.ByKey`
  resolves the old key to that def. An instance saved under the old key therefore opens as the successor.
  A retirement leading to an ending or nowhere is left out of the map.
- **A retired option** (`* <key> | <label> [retired]`) compiles to a `RetiredGate`: never shown, never picked,
  never counted toward the 2–4 live options; the stored pick still renders. Ado-stage pools drop retired options
  and any def left with none.
- **Narration scenes** (no options, one `[-> next]`) compile to one option, key `continue` (stored),
  label « Continuer ». A scene later given real options keeps `continue` as a `[retired]` option.

## Scene to `EventDef`

- **First scene** = the arc's opener. `trigger: draw` is a `Pulse`; every other trigger is `Triggered`:
  `adoption` on `OnAction.Adopted`, `stage teen|adult` on `BecameTeen|BecameAdult`, `sick` on `FellSick`,
  `recovered` on `Recovered`, `visit` on `AfterVisit` (the visitor is `{B}`), bare `death` on `Bereaved` (any
  mourner; the one who died is `{B}`), and `break 1|2|3` on no on-action, with `BreakLevel` n (the break
  `QueueBreaksAsync` picks when stress climbs past a level). `visit` and `death` need `other: anyone` and
  take no `other_if:`: the other Plynling comes with the trigger and the engine never filters it, so a bond
  list or a filter there would be silently ignored. `adoption`, `stage`, `sick`, `recovered` and `break`
  bring no other Plynling: they take no `other:`, and a friendship, love or rivalry arc cannot use them.
  `death <bond>`, `relation` and `old_age` are refused « not supported yet ».
- **Every later scene is a `FollowUp`** whose delay comes from the *target* scene's `pace:` — `now` 0 h (shown
  through « Événement suivant »), `next_day` 20–28 h, `+Nh` exactly N (1–48), and a range `Nh`, `Nd`, `Nh-Mh`
  or `Nd-Md` (one unit, 1 h to 720 h) drawn between its ends: every follow-up's delay is hashed. Why ranges: the
  echoes that cross a stage (14d-16d, 7d-10d) must land in adulte whatever the hash.
- **A handover scene** is a `Response` (see *Handover* below): an option leading to it compiles to
  `AskTarget("<arc>.<scene>")` instead of a follow-up.
- **Weight:** an unwritten `weight:` parses as 0 and **compiles to 100**, for every arc, drawn or triggered —
  and so does an explicit `weight: 0`. Never read an unwritten weight as 0 in the engine. `weight: +n [if …]`
  lines become `EventDef.WeightBonus`, added before the factors; they hold only on the opener.
- **Targets** (`other:`): a bond list (`other: friends, bestfriends`) targets a **Hostile** Plynling when every
  bond is rivals or enemies, else one it **Knows**, and the list filters candidates (mixing hostile and other
  bonds is refused); `other: known` is any non-hostile one it has met, `hostile` a rival or enemy, `anyone`
  any living Plynling of the guild. **`other_if:`** (tags only: `[relation <bond>]`, `[affinity >= n]` or
  `< n`, -100 to 100, and `[can_couple]`, the visit rule) adds conditions on the candidate, compiled into
  `TargetCondition` (`StoryConditions.OnTarget`). `affinity` and `can_couple` read a candidate, so they are
  refused anywhere else.
- **Opening conditions** (opener only, one predicate): `requires:`; `once` (a long event is `once: life` by default);
  `cooldown: Nd` since the arc last started; `locked` until the Plynling holds the `unlock` mark;
  for a **long drawn** arc, no long arc started in the last **21 days**.
  Cooldowns and « once » are computed from the instance history (`StoryKnowledge.LastStarted`), never stored.
- **`LastLongStart` counts every long arc the Plynling started, triggered ones included** (a milestone
  postpones the next drawn long event), though only drawn ones are gated by it.
- **Options:** `[needs …]` → `StoryGate` (conditions + lock text + `secret`; `[needs trait k] [secret]` is the old
  hidden trait gate); `[roll stat n]` → `EventChallenge`; `[roll stat vs]` → a challenge against the other's same
  stat (`VsTarget`, difficulty 0 when no other is known; needs `other:`); `[lean …]` → `Ai`; `[stress trait n]` →
  `StressCosts`; `[owner-only]` → `OwnerOnly`; `[mystery]` → `Mystery`; `[accept]`/`[refuse]` → `Stance`
  (handover scenes only).
- **Effects** map in order: `grow`, `affinity`, `stress`, `modifier`, `cailloux`, `item`, `lift` to their existing
  effects; `flag`/`unflag` → `SetFlag`/`ClearFlag`; `remember`/`forget`/`reveal`; marks → `GiveMark`;
  `trait a -> b` → `SwapTrait`; `[coping]`/`[coping k]` → `GainCoping` (break arcs only, k a coping trait);
  `[couple]` → `Couple` and `[heartbreak]` → `Heartbreak` (handover scenes only); `[bond peace|friends|bestfriends]`
  → `SetAffinityAtLeast` 0, `FriendsFrom + BondMargin`, `BestFriendsFrom + BondMargin` (past `BondFor`'s margin, or
  the bond would not change; needs `other:`); `[-> scene]` → `FollowUp` (or `AskTarget` for a handover scene);
  `[-> ending]` → `EndStory` then the ending's marks and other effects; `[end]` adds nothing (short events only).
  `[other …]` effects are refused « not supported yet ».
- **Deciding alone** uses the existing in-character pick. It never picks a locked, secret, owner-only or
  stress-costing option. `[mystery]` changes nothing for it.

## Handover: asking the other owner

A `handover` scene is the other owner's answer, played by the existing `Response` engine on their card
(queued on the other Plynling with `ParentInstanceId`, frozen and dead targets, deciding alone by stance). The
compiled def is a `Response` with no target, condition or trigger, as the old replies were. Rules (validator):

- **Never the first scene, never a narration, `pace: now`** (the answer is queued at once), and the arc has `other:`.
- **The run stops at the answer:** every outcome of every live option is `[end]`. Continuing the asker's arc after
  the answer is not supported.
- **No run flags:** no `[needs flag …]`/`[if flag …]` and no `[flag]`/`[unflag]` in it, cross-arc ones included.
  The answer is a new instance on another Plynling, so the asker's run flags are not there.
- **On its card `{A}` is the one deciding (the other owner's Plynling) and `{B}` the one who asked**; its gates,
  rolls, leans and stress read the decider, and **its effects land on the decider**, as the old replies' did
  (`reply_rival` grows the decider's Courage). `[couple]`, `[heartbreak]`, `[accept]` and `[refuse]` go only here.
- `[accept]`/`[refuse]` lean the answer decided alone; an option with neither is neutral.
- **An asking option is written `[owner-only]`** unless the Plynling may ask alone (the old `tell` in the secret,
  `pact`, `mend` may): deciding alone then never risks a refusal, and the file says so.
- A handed-over decision is the other's: the walker does not count it, and reads it without the profile's traits.
- The asker's story page shows its own option's prose only; the answer's page opens on the answer's prose
  (pinned by the engine checks).

## Flags: carried by the instance

A flag lives within one run. `PlynlingEventInstance.Flags` stores `key;key` (sorted, distinct; **none is
`null`, never `""`**, `StoryRuns.WriteFlags`). A follow-up is created with its parent's flags **plus the chosen
option's `[flag]`/`[unflag]`** applied in order (set then clear leaves it absent; clear then set, present). Only
effects that *applied* count: a decision alone still sets flags, but never an effect `AppliesWhenAlone` refuses.
The first scene starts empty. Conditional prose renders from the instance's stored flags; past steps re-render
from the current text, and from the instance's flags, so a story page never needs the run in memory. A past step's page is re-rendered
with the flags, memories and marks as they stand after the step, so a page can differ from the card the
player saw when the final option changes a flag that scene's prose tests.

A step that reaches an ending sets `EndingKey` on that instance and rewrites its `Flags` with the final set
(its own option's included). What later events read about a finished run is exactly that:
`[if ended <arc>.<ending>]` and `[if flag <arc>.<f>]` come from instances that carry an `EndingKey`
(`StoryRuns.Finished`). A run that was cancelled leaves nothing readable; there are **no « interrupted » or
« loss » endings** — a cancelled long event just stops.

## Knowledge: memories, marks, standing

`EventContext.Knowledge` (`StoryKnowledge`) is always filled by `PlynlingService.GetEventContextAsync`: the run's flags,
the Plynling's memories and marks, `Ended`/`ArcFlags` from finished runs, `Standing`, `LastStarted`,
`LastLongStart`, the season (`ItemCatalog.SeasonAt`) and the clock.

- **Memories** (`PlynlingMemory`, unique per Plynling + key) are one villager's recollection of one Plynling, declared
  in a file with a holder and a shift. A later event reads them (`[if memory …]`, `[needs memory …]`) — **this is how a
  choice comes back** while the village never changes. They belong to the Plynling and go with it. `reveal` makes a
  private memory public (only a private one may be revealed).
- **Marks** (`PlynlingMark`, kind int, **append-only**: epithet, keepsake, unlock; unique per Plynling + kind + key).
  Epithets are titles (the card heading shows the latest, « Personnalité » all of them), keepsakes are named objects
  with a story, not tradable (a `hurts` keepsake is a scar), `unlock` lets a `locked` arc join the draw.
- **Standing** is computed, never stored: the holder of a memory gets its shift; a public or revealed memory gives **every
  cast member** except the holder ±1. `StoryStanding.Compute` takes the whole cast (`StoryCatalog.Cast.Keys`), including
  villagers who hold no memory, or a public memory would reach only the holders.
- **Reads go through the change tracker's `Local` view**, so rows added or removed earlier in the same unit of work
  count; every write is idempotent (a re-run after a concurrency conflict finds the row and adds nothing; `Forget`
  then `Remember` revives the deleted entry). Two units writing the same memory or mark at once (a click and
  the sweep, on two open events) cannot both win: EF inserts the row before it updates the Plynling, so the loser
  fails on the unique index (a plain `DbUpdateException`, not a concurrency retry). That costs one failed attempt
  (logged per item in the sweep; on a click the pick fails after the defer and the person clicks again), and the
  next attempt finds the row. Nothing is poisoned.
- **The mascot** gets titles, keepsakes and memories; never trait changes, cailloux or items.

## Rules of the marks and endings

- **Marks only in endings.** Short events give none; a long event gives **at most one** of epithet, keepsake or
  trait per ending. `[unlock]` is not a mark for this count.
- **A trait change is a swap**, `[trait <from> -> <to>]`, never a gain (the trait draw counts held personality
  traits, so a gain would eat a slot or make a fourth). Same exclusion group (`PlynlingTraits.Group`); adulte and
  ancien arcs only; at most one per ending. `SwapTrait` skips the mascot, a Plynling that does not hold `from`,
  and one that already holds `to`.
- **Every `locked` arc is unlocked by some `[unlock <arc>]`, and every `[unlock]` names a `locked` arc.**
- A title earned is posted as a short line in the game channel, and in the story page.

## Per size (validator and walks)

| | short | long |
|---|---|---|
| Decisions on every path (a handed-over one not counted) | 1–3 | 3–5 |
| Endings | `[end]` straight from an option (the walker's pseudo-ending `end`) | `## ending:` blocks, 2–5; `[end]` refused |
| Marks | none | one per ending |
| Cailloux per path | ≤ 30 | ≤ 100 |
| Once | per its header | `once: life` by default |

Both: 2–4 live options per scene, at least one free; at most one `[mystery]` per scene; every flag set is tested and
every flag tested is set (`flag <arc>.<f>` in another arc counts); every scene and ending reachable, no loop; deciding
alone never dead-ends; no template left in any of the four gender pairs. `{T}` (the trait a moment reveals) is only for
the first scene of an adoption or stage arc; the engine fills it from `GainedTraitKey`. The walks follow the arc's own
flags exactly and count every other condition as possible.

## What the player sees

- **The stakes line** (`PlynlingEventStakes.Line`) goes under each option's label, for **every** event, C# ones
  included: the trait or stat needed, `😣 +n stress (trait)`, then what it gives — `🪙` cailloux, `🎁` item, `❤️ <byname> s'en
  souviendra` (plain name for a negative memory), a modifier, a stat lesson, and at an ending `🏷️ un titre`, `🎁 un souvenir à
  garder` or `✨ un trait change`. A roll reads `🎲 Stat 55 % : win · sinon lose`. Costs and odds are the real ones for
  this Plynling (traits scale stress). Buttons are plain labels; only an option needing a trait keeps that trait's icon.
- **Mystery options** (`[mystery]`) show « ❔ on verra bien » in place of the rewards **and still show costs and odds**,
  so nobody walks into a penalty blind. Use one only when a surprise makes sense or it leads to a follow-up.
- **Locked options.** A story option behind a non-secret `[needs …]` is **shown locked**: disabled button, `🔒` and the
  requirement (the author's lock text, else the missing needs said plainly). A `[secret]` one shows only once its needs
  hold (`[needs trait k] [secret]` is how the old hidden trait gates are written).
- **`Shown` vs `Visible`** (`PlynlingEventEngine`): `Shown` is what the card lists; `Visible` is what may be picked
  (by a click or alone). They differ only for a locked story option: shown, not visible. A click on a stale card for an
  option no longer visible answers « Ce choix n'est plus possible. » and applies nothing. Never use `Shown` to decide what an option
  may do, nor `Visible` to decide what to print.
- The card heading shows `Name, la Title` (the latest epithet, article lower-cased); « Personnalité » has an
  « Histoire » field (titles, keepsakes, memories prefixed by their holder). An ending's title, keepsake or swap shows
  as a last line of the outcome, only when it applied.

## Pacing

Short events use the daily pulse unchanged. A long event runs one step a day inside the same 24 h window.
`BlocksPulse`: **while a long event is open for a Plynling, no pulse is drawn**, and a long drawn arc starts only on a
day when **nothing waits** (`allowLong` on `PickPulse`; open instances plus those added in the same unit count, or a step
decided alone a moment earlier would let a pulse in beside it). The day is still spent, so a long run ending in the
afternoon does not let a second event in. Old follow-ups, echoes, on-action moments, responses and breaks still come,
and a triggered long arc (a milestone) starts whatever waits. The mascot decides at once. Freezes pause the clock and
deaths cancel what waits, as for every event.

## « Not playing is free » for stories

`PlynlingEventEngine.AppliesWhenAlone` is the one rule, and story effects obey it. Alone, an event never gains stress,
applies a negative modifier or a coping trait; additionally `SwapTrait` and a `GiveMark { Hurts: true }` never apply.
Titles, plain keepsakes, memories, flags, cailloux and items always do. A new story effect that costs or changes the
Plynling for the worse gets an `AppliesWhenAlone` case in the same change. **Inside a mental break the rule does not
apply** (only the owner's own choices raise stress that far): a break arc may give a coping trait or a negative
modifier alone, and its `[stress n]` relief goes down to -150 (-100 elsewhere).

## Debug and checks

- **`/debug event <key>`** forces any event; an old key (`was:`) works too. Its unknown-key reply lists each arc's **first scene
  only**. A later scene can be typed by key; it then starts **without any story flag**, and the reply says so. Use
  `mode:Pending` to click through, `mode:Expired` then `/debug sweep` to decide alone.
- **Checks:**

```bash
dotnet run --project tests/ProjectSYNCS.Checks                      # every check, story files included
dotnet run --project tests/ProjectSYNCS.Checks -- story <arc>       # also writes out/<arc>.paths.txt
dotnet run --project tests/ProjectSYNCS.Checks -- story all
```

The report (`tests/ProjectSYNCS.Checks/out/`, gitignored) lists the reachable endings and what each needs, the shortest
path decided alone, where an in-character Plynling ends, then every path as reading: the scene's text, the choice with
its **stakes line as the card renders it for a neutral context** (female, no traits), `(mystère)` after a mystery
option, its roll, lock and stress, the outcome, the ending, its marks and memories. Agreements show both forms
(« sorti/sortie », « seul(e) »), conditional lines as `[si …]`. The checks also measure every def's choice card (worst
case 40-character names, all flags on: characters, 40 components, 80-char labels, unique ids) and the « Personnalité »
embed with every declared title, keepsake and memory.

The checks' catalog sweeps (« il »/« elle », free option) over `PlynlingEvents.All` must skip `d.Story is not null`
where they do not apply: unconditional story prose may say « Il a plu » of the weather.

## Writing and review

Per arc: premise → branch map → owner review → the story file → owner review → checks and report → playthrough on the
dev guild with `/debug`. A scene is 2–3 sentences, an outcome 1–3 lines; every option brings a different *kind* of
result; a memory only when a later event will plausibly read it. Built on a villager's « accroches » from the lore.

- **`lift` and `affinity` never show on the stakes line.** An option whose only effects are those reads as a blank
  line under its label: give it a shown effect too (a modifier, a lesson, a memory) or make it the scene's `[mystery]`.
  Stress relief (`[stress -n]`) shows (`😌 −20 stress`); a stress gain never does, its cost is said first.
- **Stress is what makes the breaks happen**, and only events raise it: keep about one option in five with a
  `[stress trait n]` (10–20) on a choice against that trait (a shy one at the inn, a lazy one at the harvest, an
  honest one fibbing), and about one in twenty restful options with `[stress -10]`–`[stress -20]` (a nap, a tisane,
  sitting with the Mère Lichen). Bébé costs use childhood traits only (`bossy`, `charming`, `curious`, `pensive`,
  `rowdy`). The village arcs held 57 costs and 13 reliefs in 264 options in 7.74.2.
- **A roll's difficulty is 3 for bébé and ado** (`50 + 5 × (stat − difficulty)`, a young stat is about 3–4): around
  50–55 % for an average Plynling. The report's neutral context reads lower.
- **A label that names `{B}` is measured with a 40-character name**: with a roll or a lock beside it, keep `{B}` out of
  the label (« Aller lui parler »).
- **Animals in village prose are plain animals** (a duck, a fox, bees), never the old layer's figures (no heron,
  owl, tortoise…), even as a passing animal.

| Arc | File | State |
|---|---|---|
| L'arrivée | `Stories/Milestone/arrival.story` | long milestone, triggered by adoption; replaces the C# adoption moment; names the childhood trait with `{T}`; awaiting the owner's review |
| Wave 1, bébé and ado (30 arcs) | `Stories/Village/*.story` | short drawn events; shortlist in `docs/superpowers/content/2026-11-19-village-events-wave-1-shortlist.md`; bébé and ado retired their old pulses in 7.74.2 (`plynling-events.md`); dev-guild playthrough not done |
| Wave 2, adulte and ancien (54 arcs) | `Stories/Village/*.story` | short drawn events: 30 `stage: adult, elder` (an ancien also draws the adulte events, owner's rule), 10 adulte only, 10 ancien only, 4 seasonal (`requires: [season …]`); shortlist in `docs/superpowers/content/2026-11-19-village-events-wave-2-shortlist.md`; old adulte/ancien pulses still drawn; dev-guild playthrough not done |
| The old C# events (50 keys, 28 arcs) | `Stories/{Friendship,Love,Rivalry,Milestone,Personal,Hardship}/*.story` | moved in 8.0.0, same texts, options and effects, each scene with its `was:`: the asks and their handover answers (campfire, secret, befriend, pact, declare, letters, rival, mend), the moments (teen, adult, forgot, sick, recovered, bereaved), the bébé dream and the ado apprenticeship (echoes in adulte), and the 12 breaks (`Hardship/break_*`); dev-guild playthrough not done |

## Visit files

The played visits (`plynling.md`, *Visits*) reuse this format's reader, tags and vocabulary, not the
engine: **one scene per file**, `ProjectSYNCS/Stories/Visits/<key>.visit`, shipped with the rest of
`Stories/`. `StoryLibrary` skips `.visit` files; `VisitLibrary` loads the folder with `VisitParser` and
`VisitValidator` (code in `Helpers/Plynling/Visits/`). Fixtures: `tests/ProjectSYNCS.Checks/Visits/` —
every word of the format appears in one.

```
# visit: ricochets            (or '# confession: confess': exactly one in the folder)
title: Les ricochets
place: 🌊 Au bord de la Sommeille      ← the card's heading
bonds: acquaintances friends bestfriends rivals   (one or more; none in the confession)
hours: 7-21                   (optional, Paris; From inclusive, To exclusive; 20-1 runs across midnight)
stages: teen adult elder      (optional, all four by default)

The intro: paragraphs, no tags, no fields.

* far | Viser le record  [lean boldness +1] [lean energy +1]   ← the visitor's option, 2–4
  ? good                      ('? yes' / '? no' in the confession)
  The visitor's half when the scene goes well…
  ? bad
  …and when it goes badly.
> cheer | Applaudir chaque essai  [lean sociability +1]       ← the host's reaction, 2–4
  ? good
  …
```

- **Leans**: 1–2 per choice, `[lean <axis> ±n]` with the `AiAxis` names, n within ±3 and not 0. An option is
  judged by the host's axes, a reaction by the visitor's (`VisitOdds`). Nothing else may be tagged; a face tag
  (`[sad]`, `[A:… B:…]`) may open a half.
- **Text**: `{A}` the visitor, `{B}` the host, `{ils}` / `{Ils}`, `{a:m|f}` / `{b:m|f}` / `{p:m|f}`; no `{T}`, no
  other token. A host's half must read right after **any** of the visitor's options.
- **Caps** (worst case, 32-character names, each agreement at its longer form): labels 80 (a button), the place 100;
  scene keys 20 and choice keys 12, so the custom-ids stay under Discord's 100.
- **Coverage**: every bond × stage pair needs a scene open at every hour (`VisitValidator.Coverage`; one error per
  pair, naming the missing hours).
- **A load error stops the bot**, as for stories: `Program.cs` touches `VisitCatalog.Scenes` and
  `VisitCatalog.Confession` in the same `try` as `StoryCatalog.Compiled`, and a `StoryLoadException` lists every
  error. `VisitCatalog` is lazy and process-wide; the checks point `VisitCatalog.Folder` at the repo's folder.
- **Keys**: a scene or choice key rides in the custom-ids of open visit messages (two hours) and in
  `ResponsePicker`'s history, and is stored nowhere. Renaming one closes the visits open on it as expired, so treat
  keys as forever anyway.
- **Writing**: Claude writes a sheet in French (`docs/plynling-visit-scenes-sheet.md` for the launch's 20 + 1,
  `docs/plynling-visit-scenes-sheet-2.md` for the second wave's 8), the
  owner reviews it, and the files are copied from the approved sheet word for word; any wording change goes back to
  the owner.
