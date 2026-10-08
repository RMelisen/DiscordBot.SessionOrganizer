# Plynling personality and events

Traits, stats, the personality title, events, stress and modifiers (design:
`docs/superpowers/specs/2026-10-06-plynling-events-design.md`). The pet itself is in `plynling.md`.
**Before writing any trait, title or event text, read `docs/plynling-writing-style.md`.**

**Testing (owner only, absent from `/help`):** `/debug event key: [mode:] [target:]` forces an event —
pending, expired (the next sweep decides it alone) or decided now; `/debug stress value:`;
`/debug modifier key: [remove:]`. The scratch harness drives `PlynlingService` against in-memory
SQLite with the real migrations.

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
- **Held traits include the unsaved ones** (`HeldTraitKeysAsync`, behind `GetTraitsAsync` and
  `EnsureTraitsAsync`): two mental breaks decided in one sweep must see each other's coping trait.
  Reading the database alone let the second add the same trait again — the unique index refused the
  save, and since every roll is hashed, every later sweep of that Plynling failed the same way.
- **The adulte trait leans toward the ado years** (`AdultTraitWeight`), measured against an *average*
  ado (`AdoLeaningBaseline` / `AdoGrowthBaseline`, from the catalog), never in absolute terms: the
  options lean kind and sociable and grow Diplomacy most, so absolute sums made every adulte kind and
  talkative (×5 between the likeliest and rarest trait under random play). The leaning is measured
  along each trait's own direction (axes as a unit vector), or the traits with a 200 on one axis swing
  furthest and the 0.25 floor hands them a head start. Target, checked by simulation: random play
  within ~1.4× of uniform, a consistent ado about ×2–2.5 for the matching traits. Greed, Energy,
  Vengefulness and Zeal barely move it — few ado options lean on them; that is content, not the formula.
  The ado years start at its first stored personality trait's `AcquiredAt`, never at "now minus the
  age past the threshold": the age leaves out time spent frozen, which would start them late.
- **Only traits gained as it happens are journaled** (`PlynlingTraits.JustGained`: the stage that
  brings the trait began less than 2 days ago). A backfill is silent — at the 500-moment cap, four
  « nouveau trait » moments would push out its oldest memories. The mascot's traits are never journaled.
- Descriptions are **shared by both genders**, so they never agree with the Plynling (the harness
  bans il/elle in them); names have M and F forms.
- **Icons are CK3-style tiles** (`trait.<key>` in `tools/item-art`, uploaded as `tr_<key>`): the
  frame's colours give the kind (enfance gold on wine, personnalité bronze on teal, coping silver on
  slate). A new trait needs its tile, or it shows its `DefaultEmoji`. `TraitInfo.Emoji` is custom
  markup once uploaded: a button or select takes `EmoteMarkup.Parse`, never `new Emoji`.
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
- **Stages and keys:** keys are prefixed `baby_`, `teen_`, `grown_` (adulte and ancien) or `elder_` (the
  `Elder` array — ancien only, so any reachability check must simulate the Elder stage). A bébé holds
  only its childhood trait, so bébé gates and stress costs use childhood traits (`bossy`, `charming`,
  `curious`, `pensive`, `rowdy`); a personality trait there never applies.
- **Every rule is pure and hashed** (`StableRoll`): pulse time (08:00–20:00 Paris), which event,
  the target, the challenge roll (from the instance id), the in-character choice. Never a `Random`.
- **Pacing:** one pulse a day, skipped when 3 pulse events wait; 24 h to choose; a death cancels what
  waits; the mascot decides at once — a pulse, an on-action or an answer right away, anything else (a
  follow-up coming due, a break) on the next sweep.
- **Frozen = nothing happens, the owner's choices included:** no pulse, no decision alone, no pick
  (`EventPickOutcome.Frozen`), and the card hides « Événement » (`CountPendingEventsAsync` is 0). The
  24 h clock pauses too: `PlynlingEventEngine.DueAt` gives an event that was waiting before the thaw a
  fresh 24 h from `LiveSince`, which every thaw resets — use it, never `ExpiresAt`, to know when an
  event is decided alone. (`/debug event` *expired* sets `ExpiresAt`, so it does not hurry an event
  that was waiting across a freeze.)
- **The draw:** an event still waiting is never drawn again. The last 14 resolved **pulses** are
  excluded most recent first — follow-ups, responses, breaks and on-actions never take a slot, or a
  story chain would let the same pulses come back sooner — **but never every eligible event**: the
  least recently seen comes back. A hard exclusion locks any stage with fewer events than the window
  for good — nothing new resolves, so the window never moves.
- **A key gone from the catalog** is cancelled by the next sweep, waiting or not, and never offered:
  it would sit first in the queue, unopenable, in front of the real ones.
- **Deciding alone** never picks a hidden option or one with a stress cost for a held trait — the
  code enforces the "no penalty for not playing" rule, not just the content.
- **One context:** event logic lives in the `PlynlingService` partial, so growth, the relation change
  (`ShiftAffinityAsync`, shared with visits through `ApplyBondChangeAsync`), badges and the journal
  land in one save. Events never make a couple.
- **Never applied twice:** `ResolvedAt` is a concurrency token. When the sweep and a click resolve the
  same event, the second save throws and rolls back; `PickEventAsync` answers « déjà décidé ».
  **The sweep opens a scope per Plynling** (ids first, then `GetForSweepAsync`): a context shared by
  the pass served rows read early to every later Plynling, stale after any click made meanwhile,
  and kept a refused unit of work tracked for every later save to retry. Keep it per item.
- **Stories are rebuilt from the row** on every page turn: the row stores the chance shown, the roll,
  and the bond before/after. No in-memory story store. A removed event still renders a plain story.
- **Where stories go:** the game channel for its guild; a pick made elsewhere (the dev guild) posts in
  the channel it was made in; a sweep resolution elsewhere is dropped.
- `/plynling help` is two pages (`plyn:help:0|1`); measure each after editing.

## Stress and modifiers — `Helpers/PlynlingStress`, `Helpers/PlynlingModifiers`

- **On the row** (`Stress`, `Modifiers` as `key:unix;…`, `StressLossBonusPercent`): `PlynlingLife` is
  pure, and its drain helpers read the current segment's rates from the row. **Every change of stress
  or modifiers goes through `PlynlingLife` and rebases first** — never set the fields directly.
- **`Settle` is a timeline:** modifier ends, mornings (sickness, stress decay) and starvation, in time
  order, rebasing at each. A death is always computed within one segment. A modifier end that falls
  before `NeedsAsOf` (inside a self-freeze that has since thawed) drops the modifier without rebasing:
  rebasing backwards would run the clock in reverse and refill the needs.
- **Hunger can only slow** (clamp 0.5–1): the death clock and the warning only get more lenient.
- **Mental breaks** (`BreakLevel` 1–3, several per level, picked by hash, never filtered by stage — so
  no grown-ups-only scenes): **every option** is either a lot of relief **with a malus** (a negative
  modifier, or a coping trait — CK3 ties each to its choice: `GainCoping("comfort_eater")` for eating)
  or **no malus and less relief** (30–50 points less); no option carries a bonus. Each break keeps at
  least one malus-free option. The scratch checker enforces all of it. **One break per level at a
  time** (`QueueBreaksAsync`): stress can dip below a level overnight and climb back while its break
  still waits, and that must not queue a second one.
- **The "not playing is free" rule is code:** `PlynlingEventEngine.AppliesWhenAlone` drops stress gains,
  negative modifiers and coping traits when it decided alone — except in a mental break, which only
  stress (the owner's own choices) can trigger. The harness simulates 60 days of deciding alone.
- `StressLossBonusPercent` caches the traits' decay multiplier for `Settle`; `EnsureTraitsAsync`
  refreshes it every sweep and a new coping trait refreshes it at once.
- Modifier keys are stored — never renamed, append only. A key can be **retired** (removed from the
  catalog; a row still holding it is skipped by `Active`) but never reused for another meaning.
  Retired: `lucky`. Rewards follow CK3's habit: name the modifier after what happened (« La
  conscience tranquille » for an honest choice, « Le sens des affaires » for a good deal) rather than
  reaching for a generic one. `Negative` must mean exactly "makes
  something worse" (harness).
- « État » in « Personnalité » is cut before the 1024-character field cap (every modifier at once
  would pass it).

## Social events, follow-ups, on-actions

- **Responses** (`AskTarget`) are `Response` instances on the *other* Plynling, pointing back
  (`TargetPlynlingId` = the asker, `ParentInstanceId` = the ask). Decided alone by acceptance
  (`AcceptWeight`: affinity, compatibility). The mascot answers right after the save
  (`AnswerForMascotAsync`, given every parent resolved in that unit of work). A response whose asker
  is gone — abandoned (its id set null) or dead — is never offered nor answered: hidden from the card
  at once, cancelled by the next sweep or the click (`GoneAskerResponsesAsync`). That is also why
  « trop tard » after a `Couple` always means someone else got there first.
- **The ask's story follows the answer** (`ReplyState`, from the instance's Response children): « attend
  la réponse » while one is open, nothing once answered, « n'est jamais arrivée » when it was cancelled
  or never queued (the other one was frozen or gone when asked).
- **Rewards beyond stats (CK3's gold, artifacts):** `GiveCailloux` pays the *owner's* wallet
  (`EconomyLog.EarnEvent`), `GiveItem` puts one `ItemCatalog` item in the owner's inventory (a set it
  completes pays in the same save), `LiftNeed` raises one of the Plynling's needs. All three are
  positive, so they apply when it decides alone too. Never paid to the mascot (no player behind it);
  the story leaves those lines out for it. Keep cailloux small (≤ 30; a `/work` shift pays 25–40),
  items collectible or food and never legendary, lifts ≤ 0.3 — the scratch checker enforces all three.
- **Story cycles** (wave 5+) are chains of `FollowUp`s, three or four steps, each scene 3–5 sentences.
  A follow-up cannot know which option led to it, so a branch that must remember (a chosen mentor)
  gets its own follow-up key per branch. A social chain's follow-ups declare the parent's `Target` kind
  (they carry its target anyway): `/debug event` reads it to attach the other Plynling.
- **Anti-griefing is in the effect types:** another owner's Plynling is touched only through the
  relation. The one exception, `Heartbreak`, saddens the one who *declared* — declaring is
  `OwnerOnly`, so it is always that owner's choice (never while frozen). **`OwnerOnly` options are
  never picked alone.**
- **Couples** reuse the visit rules: `TargetInfo.CanCouple` is `PlynlingBonds.CanConfess` (gender
  included — a condition on the target alone cannot see it), checked when asked (target condition)
  and again when answered; a couple formed meanwhile makes the story say « trop tard ».
- **Affinity lifts** (`SetAffinityAtLeast`) must clear `BondFor`'s `BondMargin` to change the bond:
  the pact lifts to `BestFriendsFrom + BondMargin` (harness).
- **Follow-ups** are instances with a future `AvailableAt`; "open" includes them, so a death cancels them.
- **On-actions** are queued during a unit of work and created after its save by
  `FlushOnActionsAsync`, each in its own save and `try` — never what breaks an adoption, a visit or a
  sweep; a failed one's half-made changes are dropped (`ResetTracked`), and `DiscardChanges` also
  drops the queue of a failed sweep item. Any read that settles a Plynling into sickness or recovery
  flushes too (`SettledAsync`), not only the sweep. An on-action never queues an event already open
  for that Plynling. Growing up brings its event only within `PlynlingTraits.JournalWindow` of the
  stage — a stage moment rewritten later must not bring it again. `{T}` (trait reveals) comes from
  the instance's `GainedTraitKey` ("key,key").
- **What the mascot decides from an on-action waits in `TakeUntold`**, not in a return value: a flush
  also runs inside any settling read, where nobody holds a channel. Whoever does tells them after its
  own story — the sweep, a pick (`EventPick.Told`), a visit (`PlynlingVisitRunner` hands them back,
  since it wraps its own `PlynlingService` instance). A new caller that tells stories drains it too.
- Labels may name the other (`{B}`): `PlynlingEventStory.Label` expands them for the card, the
  button (clipped to 80) and the story.
- Social pulses check `TargetCondition` against each candidate before an event is drawn.
