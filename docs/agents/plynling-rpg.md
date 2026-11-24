# Plynling RPG

The RPG layer on the Plynling: body stats, XP and levels with points to place, Énergie, and
`/plynling train`. Design: `docs/superpowers/specs/2026-11-23-plynling-rpg-design.md`. That design has three
projects; **only the foundation is built** (8.7.0). Expeditions (with the battle engine, moves and creature
fights) come next, then live Plynling-vs-Plynling fights. The pet itself is in `plynling.md`; the mind
stats, traits and events in `plynling-events.md`. **Before writing any line, read
`docs/plynling-writing-style.md`.**

## Where things live

- **Rules, pure:** `Helpers/Plynling/PlynlingRpg.cs` (XP curve, level, points, Énergie, body-stat odds, the
  drill, the card line). The stats themselves: `Helpers/Plynling/PlynlingStats.cs`.
- **Service:** `Services/Plynling/PlynlingService.Rpg.cs`. `TrainAsync` and `SpendPointAsync`, each a
  `UnitAsync` (a `Version` conflict reruns it on fresh values, so a double click finds the energy spent or
  the point placed).
- **Command:** `/plynling train drill:` in `PlynlingModule` (so `/pl train` too). Point buttons
  `rpg:point:{plynlingId}:{stat}` in `Interactions/Components/PlynlingRpgHandler.cs`. Answers are built in
  `Commands/Plynling/PlynlingTrainCards.cs` (static, measured by the checks). Her lines are in
  `BotResponses.Plynlings.Rpg.cs`.
- **Checks:** section 65, `tests/ProjectSYNCS.Checks/RpgChecks.cs`. It covers the pure rules, the pace, the
  card, the buttons, the replies, the pools' gender rules, and the service against an in-memory SQLite.

## Body stats

`PlynlingStat` is **append-only**: the five mind stats (0–4), then Force, Agility, Endurance,
Intelligence, Speed (5–9). `PlynlingStats.Mind` / `Body` / `IsBody` split them. `IsBody` is a closed
range (Force..Speed), so an undefined value (a forged `rpg:point:1:42`) is never a body stat.
Every stat's icon is distinct from every other stat's and every trait's (Agilité is 🤸, not the Calme
trait's 🍃), and each practice modifier's from every other modifier's: the card lines them up side by side (checked).

- A body stat is a hashed 1d6 (`BaseSalt + stat`, so 205–209), plus its **species lean** (`LeanOf`: each mushroom +1
  on one body stat, the dormant sunflowers none), plus trait modifiers (±1, a dozen traits, in
  `PlynlingTraits.S(..., frc, agi, end, intl, spd)`), growth (`Growth*` columns), **points** (`Points*`)
  and state. The passion's +2 stays on the mind stats.
- **`AdultTraitWeight` reads mind stats only.** A trait's body-stat bonus would otherwise dilute its
  growth pull and shift every adulte draw. The checks compare each trait's weight with and without its body entries.
- **Body-stat tests use `PlynlingRpg.BodyChance`**, a ratio `(s+1)/(s+d+2)` clamped 5–95, not the mind
  stats' `50 + 5 × diff`. With up to 99 points at the cap, a difference would show 95 % everywhere. With the ratio,
  ten points ahead is an edge, never a sure win (60 vs 50 is 54 %). `PlynlingEventEngine.Chance` sends body
  stats there, so a story file's `roll Force …` uses it too.
- Story files may `grow` a body stat: `PlynlingModifiers.Practice` covers all ten (`practice_force` …
  `practice_speed`, appended at the end of `All`).

## XP, levels, points

- **Stored:** `RpgXp`, the five `Points*`. **Derived, never stored:** `Level(xp)`, `UnspentPoints(p)`
  (level − 1 − points placed, never below 0).
- **The curve:** `XpForLevel(n) = round(66 × (n−1)^1.5)`, cap **100**. At 180 XP a day (full play once
  expeditions and fights exist, training a third of it) that is level 10 on day 10, 50 on day 126, 100 on day 362.
  Training alone (6 drills, 60 XP a day) takes about three years. The checks pin these days; re-run
  them if the curve or `DrillXp` changes. **Levels and points are irreversible (no respec)**: never
  lower the curve's XP for a level, or existing Plynlings would gain points they never earned.
- Plynling XP is **not member XP** (`xp.md`). Plynling actions still grant none.
- Progress is the creature's: a death loses it, and a staff resurrection keeps it (it's on the row).

## Énergie

- `Energy` at `EnergyAsOf`, refilled at `max / 24 h`, clamped `[0, max]`, like the needs. **Every
  spend rebases** (`Drill` does). A row that never spent has `EnergyAsOf` at year 1 and reads full, so
  the migration needed no data step. A clock going backwards refills nothing.
- **`MaxEnergy = 100 + 5 × innate Endurance`** (die + species + traits: `PlynlingStats.Innate`). Points and
  growth never count, or a higher level would train more often and level faster still.
- Costs: a drill 15. The design reserves about 20 for a fight and 30/50/70 for expeditions. Care, play and visits never
  cost energy.

## `/plynling train`

- **Refusal order**, all before anything is spent: not a body stat, no Plynling, dead, frozen, the
  mascot, a bébé, asleep (01:00–05:00), sick, not enough energy (says when, `<t:…:R>`).
- A drill gives `DrillXp` (10, stopping at the cap) and, at `BoostChance` (0.5 %, none at level 100), a permanent +1 to the
  drilled stat as growth. **The roll is hashed on `(Id, 500, Drills)`**, so the same drill always decides
  the same way. `Drills` counts after the roll. One save, with `act.train` on the dashboard
  (« Entraînements »).
- **The answer is private** (about six a day each would flood the channel). **A level-up posts one public
  line** (`PlynlingLevelUpLines`, mentions off, swallow-and-log). It's a command answer, so no silence check.
  There's no `;` shortcut, because a typed one can only answer publicly.
- **Point buttons:** one row of five blurple picks, under the drill's answer (and under the asleep, sick
  and too-tired refusals) whenever a point waits. The id carries no count: `SpendPointAsync` re-checks
  the owner and what's left, so a stale or second click gets « Plus de point à placer ».
  **« 📜 Personnalité » brings them back** to its owner (`PlynlingTrainCards.OwnerPointButtons`: owner,
  alive, ado or older, points waiting), so a dismissed drill answer never strands the points until the next drill.
- **The mascot is out of the RPG**: no line on her card, can't train.

## The card

`PlynlingRpg.CardLine` (« Niv. 4 · ⚡ 12/120 · 3 points à placer ») rides **inside the status
`TextDisplay`**, so it adds no component (the checks compare the card's count with a bébé's). Null for a
bébé, the dead and the mascot. « Personnalité » lists the body stats, with `espèce` and `points` in their
breakdown, **only where the RPG applies** (`PlynlingRpg.CanTrain`: ado or older, never the mascot), so
`DetailEmbed` takes `now`. The stats field is 710 of its 1024 characters at worst (three-digit growth on
every stat, 99 points on every body stat: measured by `StoryEngineChecks`' fullest Plynling).

## Append-only lists this added

Stats 5–9, `practice_force` … `practice_speed`, the metric `act.train`. The columns `Growth*` / `Points*` for
the five body stats, `RpgXp`, `Energy`, `EnergyAsOf`, `Drills` (migration `PlynlingRpgFoundation`, schema
only).
