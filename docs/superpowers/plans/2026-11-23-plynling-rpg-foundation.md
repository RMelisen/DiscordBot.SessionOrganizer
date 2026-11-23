# Plynling RPG Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every ado-or-older Plynling five body stats (Force, Agilité, Endurance, Intelligence, Vitesse), its own XP and level with a point to place per
level, an Énergie gauge, and `/plynling train`, a quick drill that spends energy for XP and a rare
permanent +1.

**Architecture:** All rules are pure and hashed, in a new `Helpers/Plynling/PlynlingRpg.cs` (XP curve, level, points,
energy, body-stat odds, the drill). The stats extend `PlynlingStats`. `PlynlingService.Rpg.cs` only loads, checks and saves,
inside `UnitAsync`. The command, the point buttons, the card line and « Personnalité » read those helpers.
Level and unspent points are **derived from XP and spent points, never stored**.

**Tech Stack:** .NET 10, Discord.Net (Components V2 card, ephemeral replies), EF Core + SQLite, `tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-23-plynling-rpg-design.md` (project 1). Also read
`docs/agents/plynling.md`, `docs/agents/plynling-events.md` and `docs/plynling-writing-style.md` before you start.

## Global Constraints

- `PlynlingStat` is append-only: `Force, Agility, Endurance, Intelligence, Speed` go **after** `Courage`. Modifier keys are append-only too.
- Level cap **100**. Reaching it takes about **a year of daily play** (180 XP/day of full play, of which training is about a third).
- One point per level-up, placed by the owner in any of the five body stats. **No respec.**
- Énergie: base max **100**, **+5 per innate Endurance** (die + species lean + traits only), refills fully in **24 h**.
- The drill costs **15** energy and gives **10** XP, plus a **0.5 %** chance (none at level 100) of a permanent +1 to the drilled stat (lowered from 7 % after the final review).
- Only an **ado or older** Plynling trains. Refused while dead, frozen, asleep or sick, or for the mascot.
- Body-stat tests use the soft curve, so a level is "an edge, not a win". The mind stats keep `50 + 5 × diff`.
- The passion's +2 stays on the five mind stats.
- Every roll goes through `StableRoll`, never a `Random`.
- English command and option names, French everywhere else. Every Plynling line is a `GenderedLines` (`.For(p.Gender)`).
- Components V2: `AllowedMentions.None`, one verb per button row, blurple for a pick, defer before database work.
- **No commits**: the owner commits by hand (CLAUDE.md). Leave each task's changes in the working tree.
- Version `8.3.0` → **`8.4.0`** (new subcommand + migration), with a CHANGELOG entry.

## Review Focus

- **An existing Plynling the day it ships**: it must show full energy and level 1, with no migration of data. `EnergyAsOf` defaults to year 1, so `EnergyAt` must clamp to max, not overflow. Pinned in Task 2.
- **A drill while frozen or asleep** must be refused before any energy is spent. Pinned in Task 3's refusal order.
- **Two drills at once** (a double-click, or `/plynling train` twice): the `Version` token makes the second retry and see the spent energy. Pinned by going through `UnitAsync`, Task 3.
- **A point button left open after the points are spent** (or clicked twice): the second click must say « plus de point », never push spent points past the level. Pinned in Task 3 (`SpendPointAsync`) and Task 2 (`UnspentPoints` never negative).
- **The adulte trait draw must not change**: adding body stats to traits must not shift `AdultTraitWeight`. Pinned in Task 1.

---

### Task 1: Body stats, columns and migration

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingStats.cs`
- Modify: `ProjectSYNCS/Models/Plynling/Plynling.cs` (after `GrowthCourage`)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingTraits.cs` (the `S(...)` helper and the traits listed below)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingModifiers.cs` (`All` append, `Practice`)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventEngine.cs:244` (`AdultTraitWeight`)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingPersonality.cs:181` (`StatText`)
- Create: migration `PlynlingRpgFoundation` (`dotnet ef migrations add PlynlingRpgFoundation` from `ProjectSYNCS/`)
- Create: `tests/ProjectSYNCS.Checks/RpgChecks.cs` (section 60), registered at the end of `Program.cs` like section 59

**Interfaces:**
- Produces:
  - `enum PlynlingStat { Diplomacy, Stewardship, Learning, Intrigue, Courage, Force, Agility, Endurance, Intelligence, Speed }`
  - `PlynlingStats.Mind` / `PlynlingStats.Body` (`IReadOnlyList<PlynlingStat>`), `PlynlingStats.IsBody(PlynlingStat) → bool`
  - `PlynlingStats.SpeciesLean(PlynlingSpecies, PlynlingStat) → int`, `PlynlingStats.Innate(Plynling, IReadOnlyList<TraitInfo>, PlynlingStat) → int` (die + species lean + traits)
  - `PlynlingStats.Points(Plynling, PlynlingStat) → int`, `PlynlingStats.AddPoint(Plynling, PlynlingStat)` (throws on a mind stat)
  - `record StatLine(PlynlingStat Stat, int Base, int Passion, int Species, int Traits, int Growth, int Points, int State, int Total)`
  - Plynling columns: `Growth`/`Points` + `Force`, `Agility`, `Endurance`, `Intelligence`, `Speed` (ten ints), `RpgXp` (long), `Energy` (double), `EnergyAsOf` (DateTimeOffset), `Drills` (long)

- [ ] **Step 1: Write the failing checks** in `RpgChecks.Run(Action<string> fail)`, section `// 60. The RPG foundation`:
  - `PlynlingStats.All.Count == 8`, `(int)PlynlingStat.Courage == 4`, `(int)PlynlingStat.Force == 5` (append-only).
  - `Name` for each body stat: `"Force"`, `"Agilité"`, `"Endurance"`, `"Intelligence"`, `"Vitesse"`. `Emoji`: `"💪"`, `"🍃"`, `"❤️"`, `"🧠"`, `"💨"`.
  - Species lean (each mushroom gets exactly +1 on one body stat, sunflowers none): Amanite Intelligence, Cepe Endurance, Rose Agility, Russule Force, Mystique Intelligence, Dore Speed, Coprin Speed. Check the sum over body stats is 1 for every mushroom and 0 for every sunflower.
  - `PassionStat` never returns a body stat, for every `PlynlingPassion`.
  - For a row with `PointsForce = 3, GrowthForce = 1`, `Compute(...)`'s Force line has `Points == 3`, `Growth == 1` and `Total == Base + Species + Traits + 1 + 3 + State`.
  - **Adulte draw unchanged:** for every personality trait and a fixed non-empty `AdoYears`, `AdultTraitWeight` equals what it gives with the trait's body-stat entries removed.
  - `PlynlingModifiers.Practice(PlynlingStat.Force).Stats[PlynlingStat.Force] == 1` (and the same for the other four body stats).
- [ ] **Step 2: Run** `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: build errors or section-60 failures.
- [ ] **Step 3: Implement.**
  - Stats: `Base` keeps `BaseSalt + (int)stat` (so 205–209 for the body stats). `Growth`/`AddGrowth` cover the new columns. `Compute` adds `SpeciesLean` and `Points`.
  - Trait body modifiers: extend `S(...)` with `frc`, `agi`, `end`, `intl`, `spd` parameters. Set these values: brave Force +1; craven Vitesse +1, Force −1; wrathful Force +1; calm Endurance +1; diligent Endurance +1; lazy Vitesse −1; gluttonous Endurance +1, Agilité −1; temperate Agilité +1; impatient Vitesse +1; eccentric Intelligence +1; patient Intelligence +1; rowdy (childhood) Force +1; curious (childhood) Agilité +1; pensive (childhood) Intelligence +1; athletic (coping) Endurance +1.
  - `AdultTraitWeight` counts only `PlynlingStats.Mind` in `raised`.
  - Append `practice_force`, `practice_agility`, `practice_endurance`, `practice_intelligence` and `practice_speed` to `PlynlingModifiers.All`, worded like the five existing practice modifiers (5 days, +1), and map them in `Practice`.
  - `StatText` adds `espèce {±n}` and `points {+n}` when non-zero.
  - Add the model columns, each with a comment in the file's style. The migration is schema-only, with no data step: an existing row reads zero XP and full energy.
- [ ] **Step 4: Run the checks.** Expected: section 60 passes and every other section stays green. `dotnet build` passes with no warnings.

### Task 2: The pure RPG rules — `PlynlingRpg`

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/PlynlingRpg.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingEventEngine.cs:117` (`Chance`)
- Test: `tests/ProjectSYNCS.Checks/RpgChecks.cs`

**Interfaces:**
- Consumes: Task 1's `PlynlingStats.Innate`, `Points`, `AddPoint`, `AddGrowth`, the new columns.
- Produces (all `public static` in `PlynlingRpg`):
  - `const int MaxLevel = 100, DrillXp = 10, DrillCost = 15, BaseEnergy = 100, EnergyPerEndurance = 5;` `const double BoostChance = 0.07;` `static readonly TimeSpan EnergyRefill = TimeSpan.FromHours(24);`
  - `long XpForLevel(int level)`: `0` for level ≤ 1, else `(long)Math.Round(66 * Math.Pow(level - 1, 1.5))`.
  - `int Level(long xp)`: the highest level ≤ `MaxLevel` with `XpForLevel(level) <= xp`.
  - `int UnspentPoints(Plynling p)`: `Level(p.RpgXp) - 1 -` the sum of the three `Points*`, never below 0.
  - `int MaxEnergy(Plynling p, IReadOnlyList<TraitInfo> traits)`: `BaseEnergy + EnergyPerEndurance * Innate(p, traits, Endurance)`.
  - `double EnergyAt(Plynling p, int max, DateTimeOffset now)`: the stored `Energy` plus refill since `EnergyAsOf` at `max / 24 h`, clamped to `[0, max]`. Negative elapsed time counts as 0.
  - `bool CanTrain(Plynling p, DateTimeOffset now)`: ado or older and not the mascot (`PlynlingMascot.Is`).
  - `int BodyChance(int stat, int against)`: `Math.Clamp((int)Math.Round(100.0 * (stat + 1) / (stat + against + 2)), 5, 95)`.
  - `record DrillResult(int XpGained, int LevelBefore, int LevelAfter, bool Boosted, double EnergyLeft)`
  - `DrillResult Drill(Plynling p, IReadOnlyList<TraitInfo> traits, PlynlingStat drill, DateTimeOffset now)`: throws on a mind stat. It spends `DrillCost` (rebases `Energy`/`EnergyAsOf`), adds `DrillXp` (capped at `XpForLevel(MaxLevel)`), and rolls `StableRoll.Unit(p.Id, DrillSalt, (int)p.Drills) < BoostChance` with `DrillSalt = 500`. On success it calls `AddGrowth(p, drill, 1)`. Then `Drills++`. The caller checks the energy first.

- [ ] **Step 1: Write the failing checks**:
  - `XpForLevel(1) == 0`, `XpForLevel(2) == 66`, and `Level(XpForLevel(n)) == n` and `Level(XpForLevel(n) - 1) == n - 1` for n in 2..100. `Level(long.MaxValue / 2) == 100`.
  - The pace at 180 XP/day (round up): **level 10 on day 10, level 50 on day 126, level 100 on day 362** (±1 day). With training alone at 6 drills/day (60 XP), level 100 takes more than 900 days.
  - `UnspentPoints` is 0 at level 1, 4 at level 5 with nothing spent, 0 when the points spent exceed the level (never negative).
  - `EnergyAt` with `EnergyAsOf = default` equals `max` (the day-one row). Spending 15 then reading 6 h later gives `max - 15 + max/4`, clamped to max.
  - `MaxEnergy` uses innate Endurance only: raising `PointsEndurance` and `GrowthEndurance` leaves it unchanged.
  - `BodyChance(10, 10) == 50`, `BodyChance(20, 10) == 66`, `BodyChance(0, 0) == 50`, `BodyChance(100, 0) == 95`, `BodyChance(0, 100) == 5`. Ten points ahead at a high level is still an edge, not a sure win: `BodyChance(60, 50)` is between 50 and 60.
  - `Drill` on the same row and `Drills` value gives the same `Boosted` (hashed). Over `Drills` 0..9 999 for one id, a year of daily drills gives under half the points a year of training earns, and none at the cap. After a drill, `RpgXp` is +10 and the energy is −15.
  - `PlynlingEventEngine.Chance` on a body-stat challenge equals `BodyChance`, and on a mind stat it's still `50 + 5 × diff`, clamped.
- [ ] **Step 2: Run the checks.** Expected: failures for section 60.
- [ ] **Step 3: Implement `PlynlingRpg`**, and in `Chance` send body stats to `BodyChance(ctx.Stat(stat), against)`.
- [ ] **Step 4: Run the checks.** Expected: all pass.

### Task 3: Service — `TrainAsync` and `SpendPointAsync`

**Files:**
- Create: `ProjectSYNCS/Services/Plynling/PlynlingService.Rpg.cs` (a partial of `PlynlingService`)
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingService.cs` (next to `ForageResult`: the records and enums below)

**Interfaces:**
- Consumes: `PlynlingRpg.*`, `GetCurrentAsync`, `GetTraitsAsync`, `UnitAsync`, `SettledAsync`.
- Produces:
  - `enum TrainOutcome { Done, NoPlynling, Dead, Frozen, Asleep, Sick, TooYoung, Mascot, NoEnergy }`
  - `record TrainResult(TrainOutcome Outcome, Plynling? Plynling, DrillResult? Drill = null, int MaxEnergy = 0, DateTimeOffset? EnoughAt = null)`
  - `Task<TrainResult> TrainAsync(ulong guildId, ulong ownerId, PlynlingStat drill, DateTimeOffset now)`, as `UnitAsync(() => TrainOnceAsync(...))`
  - `enum PointOutcome { Done, NoPlynling, NotOwner, NoPoint }`
  - `Task<(PointOutcome Outcome, Plynling? Plynling)> SpendPointAsync(int plynlingId, ulong userId, PlynlingStat stat, DateTimeOffset now)`, through `UnitAsync`

- [ ] **Step 1: Implement `TrainOnceAsync`.** Refusals in this order, each before anything is spent: no Plynling → Dead → Frozen → Mascot → TooYoung → Asleep (`PlynlingLife.IsAsleep`) → Sick → NoEnergy. On NoEnergy, `EnoughAt` is when `EnergyAt` reaches `DrillCost`. If it passes, call `PlynlingRpg.Drill`, then `EconomyLog.AddAsync(_db_context, guildId, EconomyLog.ActTrain, 1, now)` with a new metric key `ActTrain = "act.train"` in `Helpers/Economy/EconomyLog.cs`, appended to `Activities` after `ActMedicine` and worded « entraînements » wherever `ActForage` gets its dashboard label (grep `ActForage`), then **one** `SaveChangesAsync`. Grant no member XP (economy.md: Plynling actions grant none).
- [ ] **Step 2: Implement `SpendPointAsync`.** Load by id and settle it. Return `NotOwner` unless `userId == p.OwnerId`, and `NoPoint` when `UnspentPoints == 0`, a dead Plynling or a mind stat. Otherwise call `AddPoint` and save.
- [ ] **Step 3: Build.** Run `dotnet build`. Expected: no warnings. The rules themselves are pinned by Task 2. These methods only order the checks and save once, and the dev-guild pass (Task 5) runs them.

### Task 4: `/plynling train`, point buttons, card line, « Personnalité »

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.cs` (new `[SlashCommand("train", …)]` after `forage`)
- Create: `ProjectSYNCS/Commands/Plynling/PlynlingTrainCards.cs` (static, Context-free builders)
- Create: `ProjectSYNCS/Interactions/Components/PlynlingRpgHandler.cs` (`rpg:point:{plynlingId}:{stat}`, `ignoreGroupNames: true`)
- Create: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Plynlings.Rpg.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingText.cs` (refusal texts), `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Rendering.cs:114` (`BuildCard`)
- Test: `tests/ProjectSYNCS.Checks/RpgChecks.cs`

**Interfaces:**
- Consumes: Task 3's `TrainAsync`, `SpendPointAsync`, `TrainResult`. Task 2's `UnspentPoints`, `MaxEnergy`, `EnergyAt`, `Level`.
- Produces: `PlynlingTrainCards.Reply(TrainResult r) → (string Text, MessageComponent? Buttons)`, `PlynlingTrainCards.PointButtons(int plynlingId) → MessageComponent`, `PlynlingRpg.CardLine(Plynling p, IReadOnlyList<TraitInfo> traits, DateTimeOffset now) → string?` (added to `PlynlingRpg`)

- [ ] **Step 1: Write the failing checks**:
  - `CardLine` is null for a bébé, a dead Plynling and the mascot. For an ado at level 1 with full energy it is `"Niv. 1 · ⚡ {max}/{max}"`, and it appends `" · 1 point à placer"` (or `"N points à placer"`) when points are unspent.
  - `BuildCard` for a Plynling with every slot filled has the **same component count** as before (the line goes inside the status `TextDisplay`, never a new component) and stays ≤ 40.
  - `PointButtons(1)` is one row of five blurple buttons (a row's maximum), ids `rpg:point:1:Force|Agility|Endurance|Intelligence|Speed`, labels ≤ 80, no duplicate ids.
  - Every new pool is a `GenderedLines` with at least 6 lines per half; the existing `plynlingui` gender rules apply to them (no `il`/`-le`/`mort` in F, no `elle`/`-la`/`morte` in M).
  - `/plynling help`, both pages, still < 6000 (the existing measurement in Program.cs covers it, so run it).
- [ ] **Step 2: Run the checks.** Expected: failures.
- [ ] **Step 3: Implement.**
  - **Command:** `train(drill: PlynlingStat)` with five `[Choice]`s (« Force », « Agilité », « Endurance », « Intelligence », « Vitesse »), description « Entraîner ton Plynling : de l'expérience, et parfois un vrai progrès ». It defers ephemerally first. The reply is ephemeral: a drill line for the stat (`PlynlingDrillForceLines`, `…Agility…`, `…Endurance…`, `…Intelligence…`, `…Speed…`), then `+10 XP · Niv. N · ⚡ left/max`, a boost line when `Boosted`, and `PointButtons` when points are unspent. A refusal is ephemeral too, and NoEnergy names `<t:EnoughAt:R>`; it still offers the point buttons if points are unspent.
  - **Level-up**: when `LevelAfter > LevelBefore`, post one public line in the channel (`PlynlingLevelUpLines`, name through `PlynlingCardUi.SafeName`, `AllowedMentions.None`, swallow-and-log). It's a command answer, so no silence check.
  - **Point buttons:** the handler defers ephemerally, calls `SpendPointAsync`, and answers with the new stat total (`PlynlingPointLines`) plus the remaining buttons if points are left. NotOwner and NoPoint get short French refusals.
  - **Card:** append `CardLine` to the status text in `BuildCard`, using the `traits` already passed.
  - « Personnalité » needs no change: Task 1's `StatText` already lists the body stats. Check it reads well on the dev guild and that the field stays under 1024.
  - Read `docs/plynling-writing-style.md` before writing any line. Nothing about caps, petals or spores (family-neutral).
- [ ] **Step 4: Run the checks.** Expected: all pass. `dotnet build` passes with no warnings.

### Task 5: Docs, help, version — then the dev-guild pass

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Help.cs` (one line for `train`, Énergie and levels, on whichever page has room)
- Modify: `README.md` (the Plynling section), `ProjectSYNCS/config.yaml` (`8.4.0`), `ProjectSYNCS/CHANGELOG.md`
- Create: `docs/agents/plynling-rpg.md`. Modify: `CLAUDE.md` (a table row for "Plynling RPG: body stats, levels, Énergie, `/plynling train`"), and the stats paragraph in `docs/agents/plynling-events.md` (body stats are no longer "to be appended")

- [ ] **Step 1: Write `plynling-rpg.md`**, under 40 KB. Include: the rules and where they live; derived vs stored (level and unspent points derived, energy rebased, `EnergyAsOf` default = full); innate Endurance only for max energy; why `AdultTraitWeight` reads mind stats only; the soft curve for body stats; the drill's hashed roll on `Drills`; the private reply and public level-up; the append-only lists (stats, practice keys, `act.train`); and that projects 2–3 follow the spec.
- [ ] **Step 2: Update help, README and `/help`.** `/help` already points to `/plynling help` in one line, so it needs no change. Add the CHANGELOG entry `## 8.4.0 — 2026-11-23` with English bullets: body stats, levels and points, Énergie, `/plynling train`.
- [ ] **Step 3: Run** `dotnet build`, then `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: no failures.
- [ ] **Step 4: Dev-guild pass** (`dotnet run`, dev guild):
  - Use an ado Plynling. Train each drill once and check the energy goes down 15 each time and the card shows « Niv. · ⚡ ».
  - Refusals: drain to NoEnergy and read the `<t:…:R>`. Check the bébé, frozen and mascot refusals.
  - Use `/debug` (or a manual `RpgXp` bump in the dev database) to reach level 2. Check the public level-up line, then place the point and click the old button again (« plus de point »).
  - Open « Personnalité » and check the body stats' breakdown.
