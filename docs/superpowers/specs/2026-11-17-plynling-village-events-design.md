# Plynling village events — the event rework, cosy version

Design agreed with the owner on 2026-11-17. Not implemented yet. Decisions are the owner's unless
marked *builder's call* (proposed during the session, not contested, but never explicitly chosen).

**This replaces `2026-10-28-plynling-storylines-design.md`**, written for a darker, plot-driven
village the owner rejected on 2026-11-10. The file format spec
(`2026-10-29-plynling-story-file-format-design.md`) still binds, except where *Format changes* below
says otherwise. The world is `docs/village-lore.md` (fully cosy: every event stands alone, the
village never changes); the prose follows `docs/plynling-writing-style.md`. The current events are
described in `docs/agents/plynling-events.md`, the story file tools in
`docs/agents/plynling-storylines.md`.

## Intent

Today's 342 events (C# catalog, one pulse a day) have a problem the owner names plainly: **the
options feel the same, the outcome barely changes, and people don't read them.** « Le colis
égaré » is typical: open the parcel, look for its owner or shelve it — each gives +1 to a stat;
only the prose differs.

The owner wants, in their words:

- **More variety and village flavour** — events set in Humeval, built on its villagers.
- **Short events with real choices** — each option leads somewhere different.
- **Some rarer, longer events with interesting rewards**, like a title.
- **Some events flagged « once in a lifetime ».**
- **CK3's event features**: the trait icon on an option that needs a trait, stress for acting
  against one's character, flags and cooldowns for rare events and chains.

## Goals, non-goals, anti-goals

- **Goal:** a player can tell, before choosing, what each option is likely to bring — and the
  options bring genuinely different kinds of result.
- **Goal:** a choice can come back: a villager remembers, and a later event reads it.
- **Goal:** the owner writes and reviews events as story files (the file is what runs), with the
  existing checks and path report.
- **Non-goal:** visit conversations (`PlynlingScripts/`), mental breaks, expeditions. They stay as
  they are.
- **Non-goal:** a shared village state, a chronicle, crises, villagers who die, leave or change —
  all dropped with the old direction.
- **Anti-goal:** long prose nobody reads. A scene is 2–3 sentences; an outcome 1–3 lines.
- **Anti-goal:** three options that are three flavours of +1.
- **Anti-goal:** a player punished for not playing. The existing rule holds: deciding alone never
  costs stress, a negative modifier or a scar (`AppliesWhenAlone`).
- **Anti-goal:** emoji clutter on buttons.

## The two sizes (owner's choice)

| | Short event | Long event (« histoire ») |
|---|---|---|
| Decisions | 1, sometimes a quick second beat (a follow-up the next day) | 3 to 5, one a day |
| How often | the daily pulse, as today | rare: about **one a month** for an adulte (*builder's call* on the number) |
| Rewards | cailloux (≤ 30), an item, a stat lesson, a modifier, a villager's memory | the same, plus **one mark** at the ending: a title, a keepsake or a trait change; up to ~100 cailloux |
| Repeats | may come back after a cooldown, unless « once in a lifetime » | once in a lifetime by default (*builder's call*) |

- **Variety comes from the villagers** (*builder's call*): an event is built on one villager's
  « accroches » from the lore (the Tilleul's bees, the Courge's soup, the Fou looking up).
- **Marks are what make a long event worth it** (owner: titles; *builder's call*: keepsakes and trait
  changes alongside). Short events never give a mark, so a title stays something to show.
- **Milestones** are long events brought by a life moment rather than the draw: Arrival
  (`Stories/Milestone/arrival.story`, awaiting the owner's review) is the first. Growing-up arcs come
  later; until then the C# `BecameTeen`/`BecameAdult` moments run as today.

## Choices that matter

### The stakes line (owner's choice: CK3 style)

Discord buttons have no tooltip, so each option gets a line under the scene saying what it costs
**and what it gives**, as CK3's tooltip does. The card already prints a line per option
(`PlynlingEventCards.Details`: trait needed, roll odds, stress cost); it gains the rewards.

```
Un colis attend devant la porte de Brindille. Pas d'adresse. Ça tinte.

Ouvrir — 🎲 Courage 55 % : 🎁 un objet · sinon une nuit blanche
Chercher le propriétaire — ❤️ la Courge s'en souviendra
Le garder — 🪙 15
[ Ouvrir ]  [ Chercher le propriétaire ]  [ Le garder ]
```

- **Shown:** cailloux, an item (named), a villager who will remember (❤️ for a positive memory,
  plain name for a negative one), a modifier (named), a stat lesson, a title / keepsake / trait at a
  long event's ending when the option leads straight to it. A roll shows what success brings, then
  « sinon … » when failure brings something different.
- **Costs stay as today:** the trait needed, the odds, `😣 +20 stress (Lâche)`.
- **Applies to every event, old C# ones included** (*builder's call*): it is the shared card, and
  showing the old events' rewards is free.
- *Builder's call:* wording and emoji set are the builder's, kept short; the line must stay within
  the card's caps (measure with 32-character names).

### Mystery options (owner's choice)

Some options keep their reward secret — **not every time, only when a surprise makes sense or when
it leads to a follow-up.** The author marks them (`[mystery]`).

- *Builder's call:* the line shows « ❔ on verra bien » **but still shows the costs** (odds,
  stress) — only the reward is hidden, so nobody walks into a penalty blind.
- *Builder's call:* at most one mystery option per scene (validator).

### Buttons (owner's choice)

**Plain labels, no emoji** — except an option that needs a trait, which keeps that trait's icon on
its button (CK3). The 🎲 the card puts on roll buttons today moves to the stakes line, for every
event.

### Locked options

- **Story events show an option they don't qualify for**, disabled, with its padlock and requirement
  (« 🔒 Courageux ») — the old spec's owner decision, kept; the format already has it (`[needs …]`
  shows locked, `[secret]` hides).
- *Builder's call:* old C# events keep hiding their gated options as today — they are retiring, and
  showing them would crowd their cards.
- A scene offers **2 to 4 options, locked ones counting** (one button row; validator).

### Stress for acting against character

Unchanged: `[stress <trait> n]` on an option, shown on the line, scaled by traits, never paid when it
decided alone.

## Memories, flags, cooldowns

- **Memories** are the village's recollection of one Plynling (« a aidé le Tilleul à ses ruches »):
  declared in the file, held by one villager, a positive or negative shift. A later event reads them
  (`[if memory …]`, `[needs memory …]`, `[if standing <villager> >= n]`) to add a line, unlock an
  option or improve an outcome. **This is how a choice comes back** while the village itself never
  changes. Memories belong to the Plynling and die with it.
- **Flags** live within one long event, carried from step to step (« came_to_her »); they steer later
  scenes and endings. `[if flag <arc>.<f>]` in another event reads a flag a finished event left.
- **Cooldowns** (owner's choice): an event can set how long before it may come back to the same
  Plynling (`cooldown: 30d`). Today's « last 14 pulses » exclusion still applies on top.
- **« Once in a lifetime »** (owner's choice): `once: life` — at most once per Plynling, ever. Long
  events default to it (*builder's call*).
- The format spec's standing formula is kept (sum of the holder's shifts; a public memory gives every
  other villager ±1). *Builder's call:* no « Village » view in the first build; memories are listed
  with the marks (below).

## Rewards and marks

- **Titles (epithets):** permanent. *Builder's call:* the card shows the latest next to the name;
  « Personnalité » lists them all.
- **Keepsakes:** a named object with its story, kept on the Plynling, not tradable; listed in
  « Personnalité » with their story. A keepsake marked `hurts` is a scar.
- **Trait change:** swap a held personality trait for another of its exclusion group (CK3); at most
  one per long event, at endings only. Today's engine only has `GainCoping`, so this is a new effect.
  *Builder's call:* **a swap only, never a gain** — the trait draw counts held personality traits to
  know what a stage is owed (2 at ado, 3 from adulte), so a gain would eat the adulte slot or make a
  fourth trait. **Adulte and ancien events only** (validator: a `trait` effect needs `stage:` limited
  to adult/elder), so the ado years' leaning on the adulte draw is untouched. **Never the mascot**: her
  four traits are fixed and restored on every Ready.
- **Unlock** (*builder's call* on the mechanics): an event whose header says `locked` is never drawn
  until an `[unlock <arc>]` ending has given that Plynling the unlock mark; it then joins the draw pool
  like any other (weight, cooldown, the long-event spacing). The validator checks every `locked` arc
  is unlocked somewhere and every `[unlock]` names a `locked` arc.
- *Builder's call:* a trait change and a `hurts` keepsake never apply when the step that led to the
  ending was decided alone (the "no penalty for not playing" rule); titles, plain keepsakes,
  memories, cailloux and items always apply.
- The mascot gets titles, keepsakes and memories; never trait changes, cailloux or items.
- A title earned is also a short public line in the game channel (« Brindille est désormais *la
  Nez-en-l'air* »).

## Pacing and selection

- **Short events:** the daily pulse, unchanged — 08:00–20:00 Paris, 24 h to choose, then the
  Plynling decides in character; skipped while 3 pulses wait; the last 14 resolved pulses excluded.
  Story events join the same pool, weighted by their header (`weight:`, `weight: +n [if …]`,
  `requires:`, `stage:`).
- **Long events:** a step every day (`pace: next_day`), the same **24 h window** (owner's choice:
  one rule — the old spec's 36 h is dropped). **While a long event runs, no pulse is drawn** for that
  Plynling: its step is the day's event (owner's choice: never two things to answer in a day).
  *Builder's call on how far that goes:* a long drawn event is only drawn on a day when **nothing
  waits** for the Plynling (no pending pulse, no short event's second beat due), and while it runs
  no pulse is drawn. What the old engine brings on its own — old follow-ups and echoes, on-action
  moments (`AfterVisit`, `FellSick`…), responses to another owner's ask, mental breaks — still comes:
  rare, and holding it back would break their timing. A triggered long event (a milestone) starts
  whatever waits.
- *Builder's call on rarity:* a long drawn event is eligible only if the Plynling started none in the
  last 21 days; its weight is tuned so one then starts within about 10 days — about one a month. The
  checks simulate an adulte's life to confirm it.
- Freezes, deaths and abandonment behave as today's events: a freeze pauses the clock, a death or
  abandonment cancels what waits. *Builder's call:* **no "interrupted" or "loss" endings** — they
  served the old living message and grief arcs; a cancelled long event simply stops.
- **The mascot** decides at once, as today.

## Visibility

As today (owner's choice, by not changing it): the choice is private; after each decision the story
is posted in the game channel with its page turns. A title earned adds its public line. The old
spec's « living message » per arc is dropped.

## Two-Plynling events

Kept for short events with today's rules (anti-griefing, relation effects, the other's traits read).
*Builder's call:* the first build supports a story event that names another Plynling as a character;
**handing a decision to the other owner** (today's `AskTarget` responses — declarations, pacts) stays
in the C# events and comes to story files in a later build. `[other remember …]` stays refused
outside a handed-over scene (the validator's anti-griefing rule — a memory can be negative), so it
waits for that build too. Long two-Plynling events come later.

## Approaches

Three ways were considered; the owner chose **C**.

- **A. Story files with a new engine** (the old plan): everything native to the files, but the card,
  draw, deciding alone, stress, social events and follow-ups rebuilt a second time, and two engines
  side by side until the old catalog retires.
- **B. Extend the C# catalog:** smallest build, but the owner reviews drafts rather than what runs,
  branching long events are awkward (a follow-up cannot know which option led to it), and the story
  file tools are wasted.
- **C. Story files run by the current engine** (chosen): at startup the files are compiled into the
  event definitions the current engine plays; the engine gains what it lacks (run flags, memories,
  marks, cooldowns, the stakes line, mystery options, locked options, trait changes). One engine; the
  owner reads the real file; everything proven is reused. The delicate part is carrying a long
  event's flags from step to step.

## How C works

### Loading

- At startup, `StoryLibrary` parses and validates `ProjectSYNCS/Stories/`; **any error stops the
  bot**, naming file and line (format spec, unchanged). Each arc is compiled into `EventDef`s and
  registered next to `PlynlingEvents.All`.
- **Watch the statics:** `PlynlingEvents.All`, `ByKey` and every array computed from them at type
  initialisation (`PlynlingService.PulseKeys`, …) must see the compiled stories. The builder chooses
  how (a registry filled before the first sweep, lazy arrays…); nothing may read the catalog before
  the load.

### Mapping

- **Keys:** a scene becomes an `EventDef` keyed `<arc>.<scene>` (old keys never contain a dot, so
  they cannot collide). Option keys are kept as written. Both are stored: append-only, never renamed
  (format spec).
- **The first scene** is a `Pulse` (`trigger: draw`) or `Triggered` (`trigger: adoption` →
  `OnAction.Adopted`, `trigger: stage teen|adult` → `BecameTeen|BecameAdult`). A story with the same
  on-action as a C# moment replaces it (Arrival replaces the C# adoption moment, `on_welcome`).
  *Builder's call:* the moment's **trait reveal is kept** — the format gains the `{T}` token
  (today's, from `GainedTraitKey`), the engine fills it on a triggered story's first step, and
  Arrival names the childhood trait with it. Growing-up arcs will do the same.
- **Later scenes** are `FollowUp`s: `[-> scene]` becomes a follow-up whose hours come from that
  scene's `pace:` (`now` = available at once, shown through « Événement suivant »; `next_day`;
  `+Nh`).
- **Flags:** a new nullable column on `PlynlingEventInstance` holds the run's flags (`key;key`). A
  follow-up is created with its parent's flags plus the chosen option's `[flag]`/`[unflag]`. The
  first scene starts empty. Conditions read the instance's flags.
- **Endings:** reached by `[-> ending]`; the ending's prose is told with the last outcome (no extra
  click) and its marks apply in the same save. A new nullable `EndingKey` column on the instance that
  reached it (for `[if ended <arc>.<ending>]` and the story); that instance's `Flags` are rewritten
  with the final flags (its own option's included), which is what `[if flag <arc>.<f>]` reads later.
- **Conditions** compile to the `EventDef`/gate predicates; `EventContext` gains the Plynling's
  memories, marks and the instance's flags. `[needs …]` becomes a new gate kind holding the
  conditions and the lock text; `[secret]` keeps it hidden.
- **Effects:** `grow` → `GrowStat`; `affinity` → `AffinityShift`; `stress` → `StressChange`;
  `[stress <trait> n]` → `StressCosts`; `modifier` → `ApplyModifier`; `cailloux` → `GiveCailloux`;
  `item` → `GiveItem`; `lift` → `LiftNeed`; `[roll stat n]` → `EventChallenge`; `[lean …]` → `Ai`;
  `[owner-only]` → `OwnerOnly`. New effects: `remember` / `forget` / `reveal` (memories), `epithet` /
  `keepsake` / `unlock` (marks), `trait` (gain or swap).
- **Deciding alone** uses the existing in-character pick (`Ai` axes); it never picks a locked,
  secret, owner-only or stress-costing option (unchanged). Mystery changes nothing for it.
- **Stories** are rebuilt from the instance rows as today. Conditional prose renders from the
  instance's stored flags; other conditions read the Plynling's current state (format spec: past
  steps re-render from the current text).

### Data

- `PlynlingEventInstance`: `Flags` (string?), `EndingKey` (string?).
- `PlynlingMemory`: `PlynlingId`, `MemoryKey`, `AcquiredAt`, `RevealedAt?`, `InstanceId?`; unique on
  Plynling + key.
- `PlynlingMark`: `PlynlingId`, `Kind` (epithet, keepsake, unlock — an int, append-only), `Key`,
  `InstanceId?`, `AcquiredAt`.
- Cooldowns and « once » are computed from the instance history (instances of the arc's first scene
  for that Plynling), never stored.
- *Builder's call* on column names and indexes. Every rule stays hashed (`StableRoll`), writes stay
  one `PlynlingService` unit of work per decision, the sweep keeps its per-Plynling scope and per-item
  `try`.
- A migration → a **minor** version bump.

## Format changes

Against the format spec (all *builder's call* unless noted):

- **Header:** `size: short|long` (required); `once: life` (owner's feature); `cooldown: <n>d`;
  `locked` (see *Unlock*). Long events default to `once: life`.
- **Token** `{T}`: the trait a triggered story's moment reveals.
- **Option tag** `[mystery]` (owner's feature).
- **`[end]`** ends a short event straight from an option, with no `## ending:` block — a one-scene
  event should not need three ending sections. Long events still end on endings.
- **Removed** (they served the dropped living message): the header's and scenes' `public:` lines,
  the option's `[public "…"]`, and `bump`. The parser rejects them; the fixture and Arrival are
  updated.
- **Not supported yet, refused by the validator with a clear message:** `handover`, and triggers other
  than `draw`, `adoption` and `stage teen|adult`. The parser keeps reading them.
- **Validator and walks, per size** (replacing « 3–5 endings plus `interrupted`, `loss` for love and
  friendship »):
  - *short:* 1–2 decisions on every path; no epithet, keepsake, trait or unlock; ≤ 30 cailloux per
    path.
  - *long:* 3–5 decisions on every path; 2–5 endings; at most one mark (epithet, keepsake or trait)
    per ending; ≤ 100 cailloux per path (existing check).
  - both: 2–4 options a Plynling can pick (unchanged), at most 5 per scene counting the locked ones
    (one button row); at most one `[mystery]` per scene; a free
    option in every scene (unchanged); every flag set is read and vice versa (unchanged).
- The path report shows each option's stakes line as the card will render it, and marks mystery
  options.

## Retiring the old catalog (owner's choice: gradual, best ones rewritten)

- Per stage, the old pulses stop being drawn once that stage's new short pool is big enough.
  *Builder's call on thresholds:* bébé and ado at **15** short story events each (a 7-day stage with a
  14-pulse exclusion), adulte at **40**, ancien at **25**. The owner flips each stage in a release; the
  thresholds are guidance.
- **The best 20–30 old premises are rewritten** as story events in the village (owner's choice) —
  chosen with the owner in the first content session.
- Retired events stay in the code, never drawn, so old stories and journals still render (keys are
  forever). Old follow-ups, responses, breaks and on-action moments not yet replaced keep running.

## Content workflow

Per wave: shortlist (premise, villager, what each option brings) → owner review → story files → owner
review of the files and their path reports → checks → dev-guild playthrough with `/debug`. A long
event gets its own session (premise → branch map → file).

Writing rules for the new events (*builder's call*, to go into `docs/plynling-writing-style.md`):
2–3 sentences per scene, 1–3 lines per outcome; every option brings a different *kind* of result; a
memory only when a later event will plausibly read it.

## Rollout

1. **Engine (C):** loading and compiling, flags, memories, marks, cooldowns, the new effects, the
   stakes line and plain buttons for every event, locked options, mystery, the format and checks
   changes, `/debug event` able to force a story scene, Arrival updated and live.
2. **First short wave:** the rewritten best premises plus new village events, bébé and ado first (the
   smallest pools to fill before retiring).
3. **Long events wave** for adulte; then more short waves, retiring each stage as it fills.
4. Later: handed-over decisions, long two-Plynling events, growing-up arcs, other triggers.

## Left to the builder

The registry mechanism, column names and indexes, the stakes line's exact wording and emojis, where
titles, keepsakes and memories show on the card and in « Personnalité » (measured against the 40
components and the text caps), the `/debug` additions, the rarity weights for long events, and the
docs: `docs/agents/plynling-storylines.md` becomes the notes for the new engine, `plynling-events.md`
points to it, and the old storylines spec gets a header line saying it is superseded by this one.
