# Plynling hygiene and sickness — design

A third need, **Hygiène**, and what neglecting it leads to: a Plynling that is left dirty grows sad
faster and, one morning, may fall **malade**. Sickness needs daily medicine; left untreated it can
kill. Hunger stays the only thing that kills on its own clock — illness kills only through the daily
morning roll described below.

## Decisions

- **Dirt never kills directly.** Below 33 % a Plynling is *sale*: its happiness drains faster and a
  dirt overlay shows on its card.
- **Sickness is a morning roll**, more likely the dirtier it is, with a small floor even when clean.
  Sick, it eats half as well, refuses games and visits, and from its third morning risks dying each
  day unless it had medicine since the previous morning. Medicine also speeds healing.
- **Washing is free, from the card, owner only.** Medicine is bought, and given from the card by the
  owner once a day.
- **Art:** a new card mood, *malade*, and a dirt **overlay** on the card's living moods. Visit pictures
  stay clean. How dirt combines with hats is decided with the hats plan, not here.
- **Staff can cure** with `/admin plynling cure` — silently, no DM.
- **The owner can force states for testing** with an owner-only `/debug plynling`.

## Hygiène and the bath

- **The bar.** `Plynling.Hygiene`, stored like hunger and happiness — its value at `NeedsAsOf` — and
  derived from elapsed time by `PlynlingLife.HygieneAt`. It empties in **`HygieneLife` = 3 days**.
  New Plynlings start at **100 %**; the migration sets existing rows to 1.0. Resurrection sets it to
  **50 %**. Frozen and dead, it does not move, like the other needs.
- **What dirties it besides time.** `/plynling forage` −10 % (`ForageDirt`), a game from
  `/plynling play` −5 % (`PlayDirt`), each applied in that action's own save.
- **Sale.** Below **`DirtyBelow` = 0.33** (strict, like the other thresholds):
  - happiness drains **1.5× faster** (`DirtyHappinessFactor`). `HappinessAt` stays exact: hygiene
    crosses 0.33 at a computable instant, so happiness is one slope up to it and the steeper slope
    after it;
  - the card's mood line adds « · sale » (« **Humeur** · *contente · sale* »);
  - the card's picture wears the dirt overlay (below), except when frozen.
- **« 🛁 Laver ».** A button on the card beside « Caresser », verb `plyn:bath:{id}`. Owner only; free;
  **+60 %** (`BathAmount`); a **6 h** cooldown (`BathCooldown`) held in memory like the pet cooldown
  (`CooldownGate` keyed on the Plynling, released if the bath is refused), never greyed out, the
  refusal saying when it is ready. Hidden while it sleeps, refused then too, like petting. A bath on a
  Plynling at 100 % is refused as pointless, like a meal at full hunger. Her line on the card comes
  from a new `PlynlingBathLines` pool, both genders.
- **The card.** A third bar, `**Hygiène**`, under Bonheur, in `PlynlingCardUi.Status`.

## Sickness

### Mornings

A **morning** is 05:00 Paris — when every Plynling wakes. Each morning is played **exactly once** per
Plynling, in order, by `PlynlingLife.Settle`, which every read already goes through:

- `Plynling.LastMorningDay` (int `yyyymmdd`, Paris) is the last morning played. `Settle` plays every
  morning after it whose 05:00 is at or before `now`, oldest first, then records the last.
- **Time order is kept.** Before each morning, a starvation whose effective instant is earlier wins:
  the Plynling dies of hunger at its instant and no later morning is played. A self-freeze that ended
  is thawed first, as today.
- **Frozen mornings are skipped** (the illness pauses with everything else); they still advance
  `LastMorningDay`.
- **The 05:00 instants are built from the Paris wall clock** (`AtWallClock`), like `WakeAfter`, so the
  two clock-change nights still wake at 05:00 local.
- Hygiene and hunger at a past morning are exact: every action settles before it rebases, so
  `NeedsAsOf` is never after an unplayed morning.
- New Plynlings get `LastMorningDay` = the day they were adopted; the migration sets existing rows to
  **the day it runs**, so nobody catches up mornings from before the feature.

### Rolls are deterministic

`Helpers/PlynlingSickness` (pure) draws every roll from a hash of **(Plynling id, morning day key,
purpose)** — onset, death, recovery — into a double in [0, 1). A command and the hourly sweep
therefore always reach the same result for the same morning, and every morning is checkable. The
outcome is stored, so a morning is decided once.

### A healthy Plynling's morning — the onset roll

- Chance = `OnsetFloor` (**0.5 %**) + `OnsetPerPoint` (**1.2 %**) × the points of hygiene below 33 at
  that 05:00, none below 0. So: 33 %+ → 0.5 %, 30 % → 4.1 %, 20 % → 16.1 %, 10 % → 28.1 %,
  0 % → 40.1 %.
- On a hit: `SickSince` = that morning, `Recovery` = 0, `SickNotified` = false. The morning a Plynling
  falls sick is its **first sick morning**; no death roll and no recovery happen on it.

### A sick Plynling's morning

In this order, from its second sick morning on:

1. **Death roll** — only from the **third** sick morning (the first two are grace), and only if **no
   medicine was given since the previous morning** (`LastMedicineAt` is null or before the previous
   05:00). **20 %** (`IllnessDeathChance`). On a hit it dies **at that 05:00**: needs rebased to that
   instant, age banked, `DiedAt` set, **`DeathCause` = Illness**. Nothing further is played.
2. **Recovery** — `Recovery` gains **5–15** (`RecoveryDaily`), plus **15–25** (`RecoveryMedicine`)
   when medicine was given since the previous morning. At **100** it is cured: `SickSince` = null,
   `Recovery` = 0.

Treated every day it heals in about 3–4 mornings with no risk; untreated it takes about 10 and
survives the 20 % daily rolls from the third morning roughly one time in six.

### While sick

- **Mood.** `PlynlingMood.Sick`, shown as « malade », with its own face. Priority: frozen > asleep >
  **sick** > starving > hungry > sad / happy / content. The bars still show the real values.
- **Meals feed half** their hunger (`SickMealFactor` = 0.5), multiplied with the mood's `MealFactor`.
  Their happiness is unchanged.
- **No games, no visits.** `/plynling play` refuses; `/plynling visit` refuses when either Plynling is
  sick (asking or accepting), with a line saying which one is ill.
- **No self-freeze.** New `FreezeOutcome.Sick`: freezing pauses the illness, so it would be a way out
  of the death rolls. A staff freeze still works and still pauses it.
- **No gift.** `CanDrawGift` also requires not being sick — its happiness can still be above 80 %
  while its face shows « malade », and a sick Plynling handing out gifts would read wrong. A look while
  sick leaves the day's draw unspent, like an unhappy look.

### Medicine

- **The item.** `care.medicine`, « Médicament », 💊, **30 cailloux** (`MedicinePrice`), sold in
  `/inventory shop` (the same volume discount as food) and listed in the pantry. Its key is stored, so
  it is append-only. It is not food: the card's « Nourrir » menu never offers it. It can be given,
  traded and sold like any item.
- **« 💊 Soigner ».** A card button, verb `plyn:heal:{id}`, shown **only while sick** and not asleep.
  Owner only. **Once per day between mornings**: refused when `LastMedicineAt` is at or after the
  previous 05:00. Takes one dose from the owner's pantry, or charges `MedicinePrice` when there is
  none, in one save (the feeding rule); refused when neither is possible. Sets `LastMedicineAt`. Her
  line comes from a new `PlynlingMedicineLines` pool.

### Telling the owner

- **Onset DM.** The hourly sweep sends one DM when `SickSince` is set and `SickNotified` is false,
  setting the flag **before** sending (the `DeathAnnounced` rule), from a new
  `PlynlingSickWarningLines` pool. Night-time onsets are at 05:00, so the DM never lands in the night.
- **Illness deaths** are announced by the sweep like starvation deaths, from a new
  `PlynlingIllnessDeathLines` pool chosen when `DeathCause` is Illness.
- **The memorial line** on a dead card reads « Mort de maladie » instead of « Mort » when the cause is
  illness.
- **Journal.** `JournalKind.FellSick` and `JournalKind.Recovered`, appended to the stored enum; the
  existing `Died` moment carries the cause in its detail.

### Staff and testing

- **`/admin plynling cure user:`** — staff (`SessionPermissions.IsStaff`), ephemeral: cures at once
  (`SickSince` = null, `Recovery` = 0). **No DM** to the owner. Refused if not sick.
- **`/debug plynling`** — owner only (`AvailabilityService.OwnerId`), ephemeral, on the owner's own
  living Plynling: options `hygiene` (0–100) and `sick` (true/false). Absent from `/help` like the other
  `/debug` commands. It exists because sickness is rare by design and could take days to reach.

### Resurrection

Cures it (`SickSince` = null, `Recovery` = 0, `SickNotified` = false), sets hygiene to 50 % and
`LastMorningDay` to the day it comes back, and clears `DeathCause`.

## Data

One migration per phase:

- **`AddPlynlingHygiene`**: `Hygiene` (double, default 1.0), `LastMorningDay` (int, set to the Paris
  day the migration runs).
- **`AddPlynlingSickness`**: `SickSince` (DateTimeOffset?), `Recovery` (int, 0), `LastMedicineAt`
  (DateTimeOffset?), `SickNotified` (bool, false), `DeathCause` (int, 0 = Starvation — stored, so
  **append-only**).

The bath cooldown is in memory; nothing else is.

## The art (`tools/plynling-art`)

- **`sick`** joins the card `STATES` (not `VISIT_STATES`): greenish tint, droopy eyes, a thermometer —
  and, like every Plynling motion, nothing moves side to side. 14 files (7 species × bébé / adulte).
- **The dirt overlay** is stamped on the finished frame for the card's living moods — happy, content,
  sad, hungry, starving, sleeping, sick — as `plynling_<sp>[_baby]_<state>_dirty_v4.webp`: 98 files.
  Mud smudges placed from the body, the way the anger mark is placed, following the head up and down.
  Not on frozen, angry or the visit files.
- New filenames only: **no `ART_VERSION` bump**, every existing file re-exports byte-identical.
- **A preview sheet first**, judged by the owner before the 112 files are exported.
- `PlynlingArt.Sprite(species, stage, mood, dirty)` adds the `_dirty` segment; every place that shows a
  living Plynling's own picture passes it (the card, the play card, the journal, the announcer). The
  visit story does not.

## Order of work

1. **Hygiène and the bath** — `AddPlynlingHygiene`; the bar, « Laver », faster sadness when dirty,
   « · sale », forage and games dirtying it. The card shows the ordinary face until phase 2.
2. **The art** — the `sick` face and the dirt overlay, preview first; `PlynlingArt` learns `dirty`.
3. **The sickness rules, pure** — `AddPlynlingSickness`, `PlynlingSickness`, the mornings in `Settle`.
4. **The sickness in the bot** — the medicine and « Soigner », the malade mood, half meals, refused
   games / visits / self-freeze, the onset DM and illness deaths, the journal, `/admin plynling cure`,
   `/debug plynling`, the docs (`/plynling help`, README, CLAUDE.md) and the version.

## Checks (scratch harness)

- `HygieneAt` decay; `HappinessAt` switching to 1.5× at the exact crossing, against a minute-by-minute
  simulation; forage and play dirt; bath +60 % capped at 100 %.
- The onset curve: 0.5 % at 33 % and above, 40.1 % at 0 %.
- Deterministic rolls: same (id, day, purpose) → same value, and different purposes differ.
- Mornings: several caught up in order; a starvation before a morning wins; the first sick morning
  plays nothing; two mornings of grace; medicine cancels the death roll and adds recovery; cured at
  100; an illness death at 05:00 with `DeathCause` = Illness; frozen mornings skipped but recorded;
  05:00 local on 2026-03-29 and 2026-10-25.
- While sick: the Sick mood's priority, half meals, the play / visit / self-freeze refusals.
- Medicine once per day between mornings; pantry first, then the price.
- The card: the Components V2 budget with « Laver » and « Soigner » both present; every new pool in both
  genders (the `plynlingui` gender check).
