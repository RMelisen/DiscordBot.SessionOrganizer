# Plynling personality and events

Traits, stats and the personality title (phase 1 of
`docs/superpowers/specs/2026-10-06-plynling-events-design.md`); events, stress and modifiers come in
later phases and get their rules here. The pet itself is in `plynling.md`. **Before writing any
trait, title or event text, read `docs/plynling-writing-style.md`.**

## Traits — `Helpers/PlynlingTraits`

- The catalog is CK3's (values from the CK3 wiki): 5 childhood, 36 personality in 18 exclusion
  groups, 10 coping (mental breaks only — never drawn). **Keys are stored and never renamed**; append
  new traits at the end of their kind. `TraitKind` is an int, append-only.
- **Owed by stage:** the childhood trait from bébé, 2 personality traits at ado, 3 from adulte. Never
  two personality traits of one group.
- **Draws are hashed** (`StableRoll`, salts 101 and 110 + slot), **then stored** in `PlynlingTrait`
  (unique on Plynling + key). Slot n always uses the same salt, so drawing at ado then adulte gives
  the same traits as drawing at adulte at once. Storing is what lets the catalog grow without
  changing anyone.
- **One path: `PlynlingService.EnsureTraitsAsync`** — "draw what the stage is owed and it lacks" —
  called at adoption, at the mascot's creation and from `ProgressAsync` on every sweep. That last
  call is also the backfill for Plynlings older than traits; there is no data migration. Dead rows
  are skipped; a resurrected one catches up on the next sweep. It never saves.
- **Only traits gained as it happens are journaled** (`PlynlingTraits.JustGained`: the stage that
  brings the trait began less than 2 days ago). A backfill is silent — at the 100-moment cap, four
  « nouveau trait » moments would push out its oldest memories. The mascot's traits are never journaled.
- Descriptions are **shared by both genders**, so they never agree with the Plynling (the harness
  bans il/elle in them); names have M and F forms.
- **Ping-Qilin's traits are chosen**, not drawn (`PlynlingMascot.TraitKeys`: Adorable, Vaniteuse,
  Méfiante, Moqueuse) and her die is fixed at 4, so she is the same character on every guild and
  on dev.

## Stats — `Helpers/PlynlingStats`

Computed, never stored: a hashed 1d6 per stat, +2 on its passion's stat (a taught passion the
catalog recognises takes the bonus; free text leaves it on the innate one), every trait's
modifier, and the stored growth columns. **Floor 0, no upper limit.** `PlynlingStat` is
append-only — the RPG appends Force, Agilité, Endurance. `PassionStat` is exhaustive and throws on a
passion it does not map.

## Personality title — `Helpers/PlynlingPersonality`

The traits' AI axes are summed; the two largest by size (ties: axis order) pick adjectives from an
18-entry table, « {Adj1} et {adj2} ». The card shows it with the trait emojis under the status;
« 📜 Personnalité » (`plyn:traits:{id}`, `PlynlingPersonalityHandler`, anyone, ephemeral, deferred)
shows each trait and each stat's breakdown.

## `StableRoll`

The one SplitMix64 for every roll that must come out the same whoever computes it.
`PlynlingSickness.Roll` delegates with `purpose + 1` as the third input, bit-identical to before —
never change that mapping, every stored morning depends on it.

## Events — `Helpers/PlynlingEvents`, `PlynlingEventEngine`, `PlynlingService.Events.cs`

- **Catalog:** C# data. Event and option keys are stored — **never renamed**, append only. Every event
  needs an **ungated option with no stress cost** (deciding alone); no « il »/« elle » in event text
  (one text serves every gender pair). The harness checks both.
- **Every rule is pure and hashed** (`StableRoll`): pulse time (08:00–20:00 Paris), which event,
  the target, the challenge roll (from the instance id), the in-character choice. Never a `Random`.
- **Pacing:** one pulse a day, skipped when 3 pulse events wait; 24 h to choose; frozen = nothing
  happens; a death cancels what waits; the mascot decides at once.
- **Deciding alone** never picks a hidden option or one with a stress cost for a held trait — the
  code enforces the "no penalty for not playing" rule, not just the content.
- **One context:** event logic lives in the `PlynlingService` partial, so growth, the relation change
  (`ShiftAffinityAsync`, shared with visits through `ApplyBondChangeAsync`), badges and the journal
  land in one save. Events never make a couple.
- **Never applied twice:** `ResolvedAt` is a concurrency token. When the sweep and a click resolve the
  same event, the second save throws and rolls back; `PickEventAsync` answers « déjà décidé ».
- **Stories are rebuilt from the row** on every page turn: the row stores the chance shown, the roll,
  and the bond before/after. No in-memory story store. A removed event still renders a plain story.
- **Where stories go:** the game channel for its guild; a pick made elsewhere (the dev guild) posts in
  the channel it was made in; a sweep resolution elsewhere is dropped.
- **Testing:** `/debug event key: mode:` (owner only) forces one — pending, expired, or decided now.
  The scratch harness also runs the service against an in-memory SQLite database.
- `/plynling help` is two pages (`plyn:help:0|1`); measure each after editing.
