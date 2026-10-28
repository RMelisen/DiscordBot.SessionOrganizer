# Plynling Storylines — the event rework

Design agreed with the owner on 2026-10-28 (grilling pass). Not implemented yet. Decisions are the
owner's unless marked *builder's call* (proposed during the session, not contested, but never
explicitly chosen).

## Intent

Today's events (`2026-10-06-plynling-events-design.md`, `docs/agents/plynling-events.md`) are many
and short: 342 events, one pulse a day, mostly one scene with 2–4 options, chains of 3–4 steps that
can only branch on success or failure. The rework replaces them, over time, with **far fewer and far
deeper storylines**, each designed one by one with the owner: script, paths, choices, branches,
rewards, for each kind of situation.

"Deeper" means all four of:

1. **Longer arcs** — several decision points over several days.
2. **Choices remembered** — flags steer later scenes, options and endings.
3. **Richer scenes** — more text, more options, more of them gated by traits, stats, memories, relations.
4. **Lasting consequences** — the ending leaves a mark after the arc is over.

CK3 is the base (as inspiration only — its files never enter the repo, nothing is copied or
translated).

Scope: **events only.** Visit conversations (`PlynlingScripts/`) are out of scope and may get their
own rework later.

## Tone

The register may go **darker and closer to real life** than today's "tender, a little melancholy".
Stories may **end badly**; the only remaining guard is no cynicism for its own sake.

- **Allowed, when the story earns it:** grief (a villager or a friend Plynling dies), illness and
  ageing, loneliness, a friendship fading, failure and shame, betrayal and regret, hardship (a bad
  winter, a lost harvest, a flood), choices between two losses.
- **Never:** gore or graphic violence; abuse; cruelty played for laughs; sexual content; self-harm or
  suicide, even hinted; addiction; real-world references (war, politics); a Plynling's family
  (Plynlings stay family-neutral — villagers may have families); **a child's death** — villager
  children may be sick, lost, endangered, hungry or sent away, but they always live; the cost of a
  bad ending falls on adults.

`docs/plynling-writing-style.md` will need a section for the new register when the first arc is
written.

## The storyline

### Shape

- **4–8 decision points**, each a scene of 4–8 sentences, 2–4 options.
- **Branch and bottleneck:** options set flags and pick the next scene; branches rejoin at bottleneck
  scenes. At most 2–3 live branches at any point, **3–5 distinct endings** per arc.
- **Scenes adapt without branching:** conditional sentences on flags, traits, village memories,
  relation, stage.
- **Rolls stay:** a stat challenge with its odds shown; success and failure may lead to different scenes.
- **Locked options are shown**, disabled, with their requirement (« 🔒 Courageux », « 🔒 La pie vous
  doit une faveur ») — no longer hidden.

### Pacing

- **One personal arc at a time** per Plynling.
- **Usually one decision a day**; a tense moment may chain two or three choices back to back, or bring
  the next one a few hours later.
- **A gap of 3–5 days before a drawn arc.** Milestones and triggered arcs may start the day after the
  last one ended. Players are mostly daily, which this pacing assumes.
- About 3–4 arcs a month per Plynling.

### Deciding alone

- A step waits **36 h** (today's pulses: 24 h).
- Then the Plynling **decides in character** (traits lean the pick), and its character may take the
  story to a bad ending — the narrative consequences apply in full.
- **Mechanical penalties never apply when it decided alone** (stress gained, negative modifiers,
  coping traits, scars, lost items) — today's "not playing is free" rule, kept in code.
- **Owner-only options are never picked alone** (big gambles, declarations).

### Families

| Group | Family | Brought by |
|---|---|---|
| Milestones (guaranteed, once per life) | 1. Arrival (first week; also teaches the system) | adoption |
| | 2. Growing up (bébé→ado, ado→adulte, adulte→ancien) | stage change |
| | 3. Legacy (the ancien's last arc: passing on, a last wish) | old age |
| Relationships | 4. Friendship · 5. Love · 6. Rivalry | the actual relation |
| Life (drawn) | 7. Village (villagers, festivals, hardships) · 8. Personal (passion, ambition, a flaw) | the draw |
| Hard times | 9. Loss and illness (incl. mental breaks) | real game events |

Life spans to write for: bébé 7 days, ado 7 days, adulte 166 days, ancien from day 180 with no end.

### Selection

- **Once per life:** an arc happens at most once to a Plynling. Calendar arcs (below) are the
  exception — they come back each year and remember the year before.
- **Priority when the slot opens:** a due milestone → a hard-times arc from a real event → a
  relationship arc whose moment has come → a drawn life arc (weighted by traits, passion, village
  memories, season, stage).
- **Triggers queue** while an arc runs, and each triggered arc carries a **shelf life** (a "just became
  friends" arc means nothing three weeks later).
- **When nothing is eligible**, an old-catalog pulse fills the day while the old catalog lives; after
  that, the day is quiet.

### Social arcs

- An arc belongs to **whoever triggered it**; it exists once, never as a mirrored "other side".
- The other Plynling is a character in it, acting in character from its traits.
- **1–3 key moments are handed over** (accepting a declaration, forgiving, answering a challenge): the
  other owner gets that decision on their card with their own 36 h window; unanswered, their Plynling
  answers in character. They get a journal entry whenever their Plynling appears in the arc.
- Anti-griefing is unchanged: another owner's Plynling is touched only through the relation and the
  handed-over moments.

### Consequences

On top of village memories, an ending gives **one or two** marks, chosen per arc:

1. **An epithet** — a permanent title on the card; the card lists them.
2. **A keepsake** — a unique named object tied to the story, kept on the Plynling, not tradable, its
   story readable.
3. **A trait change** (CK3-style) — gain a personality trait or swap one for its opposite. At most one
   per arc, at endings only, respecting exclusion groups. Traits become partly earned, not only drawn.
4. **An unlock** — a later arc that cannot happen otherwise.
5. **Cailloux** — up to about **100** for a completed arc (today's cap: 30).
6. **A scar** — a long negative modifier, a fear trait, a keepsake that hurts. Only from the owner's
   own choices, never when it decided alone.

### Stress, breaks, modifiers

- **Stress stays** as the cost of acting against character; an arc's heaviest moments may cost up to
  **~60** (today 10–40).
- **Mental breaks become short hard-times arcs** (2–3 steps) in the new register; the old breaks stay
  until replaced.
- **Modifiers stay** as the short-term layer; epithets, keepsakes, traits and memories are the
  long-term layer.

### Interruptions

- **Freeze:** the arc pauses; its 36 h window restarts at the thaw. A crisis runs on without it, and it
  does not count.
- **Stage change mid-arc:** the arc carries on; scenes may read the current stage.
- **Its own death:** an **interrupted ending**, a short closing paragraph written per arc; the living
  message closes on it publicly.
- **The other Plynling dies or is abandoned mid-arc:** the arc jumps to a **loss branch** —
  **required** for love and close-friendship arcs, optional elsewhere — or else gets an interrupted
  ending. A later hard-times arc may read the interrupted arc's flags.
- **Abandoned by its owner:** cancelled silently, no public closure.

### Ping-Qilin (the mascot)

- Lives arcs **at the normal pace** — each step decided in character when it falls due, never instantly.
  Her living messages are a public showcase of the system.
- Counted in crises like everyone.
- Answers a key moment handed to her **at once**.
- No cailloux or items (no wallet); she does get epithets, keepsakes, traits and village memories.

## The shared village

One village **per guild**, shared by every Plynling there (the dev guild has its own).

### The world

All seven, written down as a **village bible** before any arc that leans on it:

1. A cast with real lives (names, ages, pasts, flaws, secrets, ties to each other; villagers may have
   families).
2. A cast that changes (ages, falls ill, moves away, dies, marries, falls out) — persisted.
3. Institutions (a mayor and a council, a school, a healer, the market, the mill, the post).
4. An economy that works (trades, harvests, debts, prices, rich and poor).
5. Places (a fixed map every arc agrees on).
6. A calendar (real seasons on the real date, festivals, a hard winter).
7. Social friction (gossip, reputation, unwelcome newcomers, old feuds, a decision that splits the village).

**The cast is reworked from scratch** — names, roles, maybe not animals at all — in its own deep
design session. The old catalog's figures (the owl, the tortoise, the snail…) are **tolerated** as a
separate layer until the old catalog retires; no in-world handover, no rewrite of old text. Visit
conversations keep their mentions until their own rework.

### Who changes it

- **Big changes** (deaths, departures, who runs what, disasters) come only from the **chronicle**.
- **Personal standing** comes from individual arcs and touches only that Plynling.
- **Small shared traces** from an individual arc (a plaque, a dish on the café menu) are allowed if
  they take nothing away from anyone.

### Calendar and chronicle

- **The calendar** loops yearly (fairs, harvest, winter); its scenes read the village state, so the
  second spring fair knows who is gone.
- **The chronicle** sits on top: one-off beats written a season ahead — about **one major beat a month
  and one crisis a season**. If the owner stops writing, the calendar keeps the village alive and the
  chronicle pauses.
- **Firing:** each beat carries a window (« début décembre »); the bot fires it inside the window,
  never a crisis while another is open, never two beats in one week. The owner gets a **DM preview a
  day ahead** and can **fire early, delay or cancel** from `/debug`.

### Crises

- Every living, unfrozen Plynling of the guild gets the crisis; it runs **in parallel** with a personal
  arc (a crisis day may mean two prompts).
- 3–4 decisions over about a week; 2–3 possible endings.
- Every choice pushes toward an ending, choices made alone included (in character). The result uses
  each ending's **share** of participants, so server size does not matter.
- **A daily mood line** in the game channel shows where the village leans, never the numbers.
- **One public reveal**, then a **personal epilogue** per Plynling joining the village's outcome with
  what it did; standing with the characters involved changes per Plynling.

### Standing and memories

- **Memories are the source of truth:** discrete facts the village holds about a Plynling ("stayed
  with the sandbags", "lied to the owl at the exam", "owes the badger 40 cailloux"). Arcs check them
  in conditions and gates.
- **Each memory shifts a score** with the character concerned, which sets the general tone.
- **Private or public, marked by the author.** A private memory is held by one character; a public
  one is known to the whole village and shifts every character's attitude a little.
- **Gossip only in written scenes.** A private memory becomes public only through a scene written for
  it — at the inn, through the innkeeper (her goodwill keeps a Plynling's secrets) — never by a
  hidden automatic spread.
- **A « Village » view** on the card shows each character's attitude and the memories they hold, as
  the village tells it.
- **A chronicle beat may branch on the guild's standing with a character** — every living
  Plynling's memories of them, taken together — as well as on crisis outcomes. The Intendant's turn in
  season one is the first case: written both ways, picked by how the server treated him.
- **A queued chronicle beat may fire on a count**: when enough Plynlings of the guild hold a given
  memory (the baker reported to the steward), or on another beat's outcome, or by the owner's hand.
  This is how a betrayal stays a real individual choice without one player ruining the village: each
  report is remembered, and only the sum acts.
- Standing belongs to the **Plynling**: it dies, the village forgets — except a line or two naming the
  previous Plynling of the same owner (« … a connu ton prédécesseur »), with no standing carried over.

### The village bible

Designed with the owner on 2026-10-28 (second grilling pass). The bible itself is
`docs/village-lore.md`, in French, drafted in one pass and reviewed sheet by sheet (a short grilling
wherever the owner pushes back). The frame it was drafted to:

- **Medieval, with one quiet magic.** Hand tools and mills, no machines, no spells. The quiet magic is
  only the place, the unseen keepers and the rare return.
- **Villagers are plant and fungus folk** of every kind; animals are wildlife, livestock and threats,
  never neighbours. **Plynlings are mushrooms only** (sunflower species are not coming), so the style
  guide's "never name a cap, petals or spores" rule is gone.
- **Plynlings sprout from one ancient place** at the forest's edge, whose mycelium runs under the
  valley, and return to it at death — which is why they have no family. The place can, rarely, give
  one back (the fiction acknowledges staff resurrection; a very rare resurrect-once item may come
  after expeditions).
- **The player is an unseen keeper.** The village has a belief about the watched-over young; nobody
  has seen a keeper.
- **The bible decides the whole truth** of the mystery, and it lives in the repo (public, accepted).
- **Power:** a distant lord beyond the hill, newly dead, the heir unknown (season one's question); a
  steward who comes down the road; a council and the head it chooses. **No religious order.**
- **The place** is common ground by custom, watched by one old villager out of habit; whether the
  lord's rights cover it is unclear.
- **Scale:** about 15 households, 60–80 folk; **13 core characters** (including the orphanage keeper
  and the village fool), about 15 minor ones; players' Plynlings are the village's young.
- **A Plynling's life:** bébé at the watcher's, ado at **the orphanage**, adulte with its own home, a
  **stored trade** (nine, shared, opened by passion) and household dues; the ancien passes it on. The
  watcher's succession is an arc of its own.
- **The village fool** is sometimes a holy fool, sometimes a jester, and the story never settles
  which. Written from the inside: the mockery is the village's, never the story's.
- **Plynlings never couple with villagers.** Couples stay between players' Plynlings (the existing
  bond rules). A Plynling may fall for a villager in an arc — a crush, a longing, a heartbreak — but
  it stays one-sided: the villager is older, promised, or does not see it that way.
- **Names:** a given name plus a byname; daily speech uses the byname, intimacy the given name.
- **Money:** cailloux are the village coin, daily life runs on barter; a fixed price scale (a loaf ≈ 1,
  a day's labour ≈ 5, a tool ≈ 30, a season's dues ≈ 100); the wallet goes unexplained.
- **Calendar:** real seasons; four yearly arcs (sowing and fair, harvest, rains and dues day, the long
  night), dues day as the anchor; secular.
- **History:** seven landmarks — the founding, the great flood, the fever winter, what the fool saw,
  the feud, the newcomer's arrival, the new lord.
- **Map:** the canon places in the bible; the deep forest is the boundary, expedition country beyond.

## Visibility

- **Choices are private** (ephemeral), and each step's outcome is told there in full.
- **One living public message per arc** in the game channel: opened at the start, **edited in place**
  at each step with a line for what happened and a teaser of the dilemma at hand (never the options),
  turned at the end into a **chronicle page** with turnable pages for the full text.
- **Bumped** — a short new post linking to it — only at moments the author marks (the climax, a
  betrayal). Otherwise edits are silent.
- A key moment handed to another owner pings them lightly.
- Crises are public by nature (mood line, reveal).
- No spectator guessing button.

## Engine and authoring

- **One scene engine, one story file format, one memory store**, shared later with expeditions
  (`2026-10-28-plynling-expeditions-design.md`): an expedition runner plays scenes back to back in one
  sitting, the storyline runner one step a day with living messages. An expedition can leave a village
  memory; an arc can read an expedition chain. **Storylines are built first** and lay these down.
- *Builder's call:* a **new engine next to the old one**, reusing what works — effect types,
  `StableRoll`, challenges, in-character picks (AI axes), stress. The old engine is untouched until its
  catalog retires.
- **One story file per arc**, in a dedicated screenplay-like text format — scene headers, options as
  bullets, conditions and effects as short tags (`[si mémoire: chouette.mensonge]`,
  `[+stress lâche 30]`, `[→ la_digue_tient]`). Shipped with the bot, parsed and checked at startup,
  failing loudly. **The file is the review sheet**: what the owner reviews is what runs.
  *Builder's call:* English tags, French prose.
- **The checker** verifies every ending is reachable, a stress-free path decided alone exists, gender
  agreement in all four pairs, valid keys, Discord caps. *Builder's call:* in `tests/`.
- *Builder's call:* the card's existing « Événement » entry opens arc steps too; no extra DM
  reminders.
- Stored keys (arcs, scenes, options, flags, memories, epithets, keepsakes) are append-only, as
  everywhere.

### Per-arc workflow

Premise → branch diagram → owner review → full text in the story file → owner review → checker →
playthrough on the dev guild with `/debug`. Each arc gets its own deep design session.

## Rollout

1. **Foundations** — the engine, the village bible, village memories and the « Village » view, living
   messages, `/debug` tools. Content: **Arrival**, **bébé→ado**, **ado→adulte**.
2. **Relationships** — one friendship, one love, one rivalry arc; handed-over key moments.
3. **The living village** — chronicle and calendar engine, the first season's beats, the first crisis.
4. **Waves** — drawn life arcs, hard times, mental-break arcs, adulte→ancien, legacy.

### Retiring the old catalog

Per stage, as each new pool gets deep enough:

- **bébé / ado:** once their milestone arcs plus 2–3 drawn arcs exist.
- **adulte:** at about **20 drawn arcs** (one adulte uses about that many in a lifetime).
- **ancien:** at about **10** — it has no end, so quiet days are accepted.

The old catalog stays in the code, unplayable, so stories and journals that reference it still render.

## Still to design

Each in its own deep session, in this order:

1. **The village bible** — done: `docs/village-lore.md`, reviewed sheet by sheet with the owner on
   2026-10-28.
2. **The story file format** in detail, with the engine's data model.
3. Every arc, one by one.

The visit conversations may get their own rework afterwards.
