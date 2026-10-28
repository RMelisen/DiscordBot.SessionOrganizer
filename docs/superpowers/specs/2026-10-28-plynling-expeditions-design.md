# Plynling Expeditions — the RPG engine

Design agreed with the owner on 2026-10-28 (brainstorm, then a grilling pass). Not implemented yet.
Decisions are the owner's unless marked *builder's call* (proposed during the session, not
contested, but never explicitly chosen).

## Intent

The events design (`2026-10-06-plynling-events-design.md`) left room for "a later adventure/RPG
layer" with body stats Force, Agilité, Endurance — `PlynlingStat` is append-only for that reason.
The RPG comes in this order:

1. **Expeditions** (this document) — a solo adventure for your own Plynling, played as a short
   gamebook. It brings the body stats and the energy gauge the rest reuses.
2. **Arena** — Plynling vs Plynling, a separate project designed later. It will spend the same
   energy and lean on the same body stats.
3. **Server campaign** — maybe, later.

This document covers the expedition **engine**, shipped with **one** place. That place's content —
script, paths, choices, loot, risks, branches — gets its own deep design session before it is
written, by the owner's explicit wish: each place must be carefully designed and deep. More places
come later, one at a time, the same way.

### Prior art (and what we took from it)

- **Dank Memer `/adventure`** — button scenes played on the spot. Its trap: choices that don't
  matter, so people learn "the good button". Here odds are shown and options are real trade-offs.
- **Fallout Shelter** — sending someone out and deciding how far to push: the push-your-luck after
  a wound.
- **WoW garrison missions** — traits that counter threats, with the odds shown: trait-gated options.
- **EPIC RPG** — cooldown grinding that floods channels: what to avoid (one public message per run,
  an energy gauge).
- **OwO bot, Shakes & Fidget** — async PvP against an absent player's character: noted for the arena.

## Goals, non-goals, anti-goals

- **Goal:** choices that matter, with visible odds, where stats and traits change what is possible.
- **Goal:** a run others in the channel can watch — a small server moment, not a private grind.
- **Goal:** a reason to keep coming back to each place (its own collection, rare finds, story chains).
- **Non-goal:** groups/parties (not chosen), PvP (the arena's project), more than one place at launch.
- **Anti-goal:** an expedition that kills a Plynling. Never — the worst outcome is a heavy injury.
- **Anti-goal:** channel spam or a farmable money source.
- **Anti-goal:** throttling the pet game: care, play and visits stay free of energy.

## Who can go, and when

- **Ado and older.** A bébé is too young to leave; the first expedition is a milestone of day 7.
- Refused while **sick, frozen, asleep (01:00–05:00 Paris), or already away**.
- *Builder's call:* the mascot does not go — no player behind her, and her rewards pay nothing.
- Started with **`/plynling expedition`** (and `/pl expedition`) with the destination. `/plynling`
  goes from 15 to 16 subcommands. It coexists with `/plynling forage` (the quick 4 h search); an
  expedition is the deep, chosen one.
- **Start gate on needs:** refused unless hunger lasts the whole travel time with a margin. Needs
  keep draining normally while away, so it may come home hungry or dirty, but it can never starve
  out there.

## Énergie — the RPG gauge

- **0–100, refills fully in 24 h** (about 4 an hour). Stored as a value at an instant and derived
  from elapsed time, like the needs — nothing ticks; every change rebases.
- **Max +5 per Endurance point**, so the body stats matter from day one.
- Spent by **expeditions now and the arena later**. Feeding, petting, play and visits never cost energy.
- Each place sets its own cost. Reference scale: short 30, medium 50, long 70.
- *Builder's call:* shown on the card next to the other gauges.

## The gamebook — played on the spot

- **One public message** in the channel where it was started, **rewritten in place** scene by
  scene. Only the owner can click; anyone can watch.
- A trip is: an arrival, a number of scenes set by the place (*builder's call:* fixed per place),
  a return.
- **Scenes come from the place's pool**, drawn so the owner's recent trips are not repeated, plus
  **rare multi-trip story chains**: a thread found on one trip (a sealed door, a stranger) resumes
  on a later one. Chain progress is stored per Plynling.
- **Each scene offers 2–4 options:**
  - a risky option shows **the stat tested and its odds** (« Escalader la falaise · Agilité · 65 % »);
  - an option gated by a trait shows **the trait's icon**;
  - there is **always one safe option**, with no roll and a small or no reward.
- **Failure is written per scene, per option** — a wound, lost loot, stress, a modifier, lost time…
  whatever fits the story in progress. Not one generic penalty.
- **After a wound the owner chooses** « Rentrer » (go home now, keep the loot so far) or
  « Continuer » (risk more for more).
- **A third wound means it is rescued** (by whom is the content session's call): the trip ends on
  the spot, part of the loot found is lost, and it comes home with a heavy injury modifier
  (« Gravement blessé » — longer, larger malus). Lighter wounds bring home a smaller temporary
  modifier and some stress. **It never dies.**
- **An abandoned run** — no click for 15 minutes — heads home on its own with what it found, taking
  no new risk. Energy was spent at the start.
- **The run is persisted** in the database, so a restart mid-run loses nothing. (Unlike visit
  stories and play sessions, which reset by design: here energy has been spent and loot found.)

## Away — the travel time

- Once the run is over (finished, retreated, rescued or abandoned), the Plynling is **away for the
  place's duration**, counted from the start. Reference scale: short 1 h, medium 2 h, long 4 h.
- The card shows « En expédition · retour <t:…:R> ». While away: **no feeding, petting or bathing,
  no play, no visits either way, no forage.**
- *Builder's call:* events keep working while away (it is still living its life).
- On return, the loot, the cailloux and a journal moment land.

## Rewards

- **Place-specific loot:** each place has its own collectible set in the existing
  book/tier/turn-in system, plus food.
- **A few cailloux**, kept below a `/work` shift (25–40) so expeditions never become the best money source.
- **Stat growth** the way events do it: a passed test can apply the stat's Practice modifier
  (+1, temporary), and only rarely permanent growth (`GrowsForGood` style).
- **Very rare cosmetics** found nowhere else.

## Body stats — Force, Agilité, Endurance

- Appended at the **end** of `PlynlingStat` (append-only), with names, emojis and growth columns
  (a migration).
- Each is a **hashed 1d6** (the existing `BaseSalt + stat` scheme continues naturally), plus a
  **species leaning** (a sturdy species +1 Endurance, a slender one +1 Agilité…), plus **trait
  modifiers** (Brave → Force, Paresseux → −Endurance…), plus growth and state as today.
- Existing Plynlings get theirs **instantly**: computed, nothing to backfill.
- *Builder's call:* the passion +2 stays on the five mind stats; the body stats show in
  « Personnalité » with the rest.

## Constraints from the project rules

- Stored keys and int enums are append-only: stats, modifiers, journal kinds, place, scene and
  chain keys.
- Rolls are hashed (`StableRoll`, from the run id), never a `Random`.
- SQLite: no `DateTimeOffset` comparison in SQL; snowflakes need `.HasConversion<long>()`.
- Every Plynling line is gendered (`GenderedLines`) and follows `docs/plynling-writing-style.md`.
- Components V2: 40 components across the whole tree, 80-char button labels, every button row its
  own verb, `AllowedMentions.None`, static card builders, defer before any database work.
- Timeouts (the 15-minute abandon, the return) are settled lazily on read like the rest of
  `PlynlingLife`; whatever sweep refreshes the public message catches per item.
- Shipping it adds a migration and a subcommand: a **minor** bump, a changelog entry,
  `/plynling help` (close to its 6000 cap — measure), `/help` and README, and a subsystem note in
  `docs/agents/` with a row in CLAUDE.md's table.

## Left to the builder

The table shapes, the energy constants beyond those above, the injury modifiers' exact values, how
the species leanings are spread, the custom-id verbs, and how the public message is refreshed on an
abandon or a return.

## Left to the content session (first place)

The place itself, its scene pool, the options and their tests, each failure's consequence, the
rescue, the loot and its set, the rare cosmetics and the story chains.
