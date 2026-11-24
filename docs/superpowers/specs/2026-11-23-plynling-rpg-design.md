# Plynling RPG — design

Design agreed with the owner on 2026-11-23 (brainstorm, then a builder check). Not implemented yet.

## Context

The owner wants an RPG layer: stats, expeditions, small fights, and training that gives XP and sometimes
a stat boost. It is the **Plynling's** RPG and extends the expeditions design agreed on 2026-10-28
(`docs/superpowers/specs/2026-10-28-plynling-expeditions-design.md`, not built). That design waited
for the story engine, which shipped in 8.0.0. This document **supersedes it where they differ**: it
adds levels and XP, training, and fights, and moves Plynling-vs-Plynling fights from "a later arena
project" into this design.

**Why (owner):** progression to chase (getting stronger over weeks), Plynlings against each other,
and deeper stories in Humeval. Spectacle in the channel is a bonus, not a goal.

Decisions are the owner's unless marked *(my call)*: proposed in the brainstorm and not contested,
but never chosen explicitly.

## Goals, non-goals, anti-goals

- **Goal:** a Plynling that grows stronger in ways you choose (levels, points, moves).
- **Goal:** live Plynling-vs-Plynling fights with both owners present.
- **Goal:** expeditions that tell deeper Humeval stories, with real choices and visible odds.
- **Non-goal:** groups/parties, a server campaign.
- **Anti-goal (kept from 10-28):** the RPG never kills a Plynling.
- **Anti-goal:** spam or a farmable money source. Énergie is the brake.
- **Anti-goal:** throttling the pet game. Care, play and visits stay free of energy.
- **Anti-goal:** harming a Plynling whose owner didn't choose to play. A fight needs the challenged
  owner to accept.

## Approaches considered

- **Order of the three projects.** The options were foundation → fights → expeditions, or
  foundation → expeditions → fights, or everything at once. **The owner chose foundation →
  expeditions → fights**, so stories come first. Expeditions therefore ship the battle engine and moves (against creatures),
  and PvP reuses them later.
- **Expedition format.** The options were one sitting, or spread out like events. **The owner chose one sitting** (the 10-28
  design): story momentum.
- **Fights.** The options were instant against an absent Plynling, a visit-shaped exchange, or a live battle. **The owner chose live:
  « like a small Pokémon battle, so the two must be present »**. This is a genre mechanic, built with our own moves, names
  and art. Nothing is copied from Pokémon.
- **Moves.** The options were the same four for everyone, learned, or fixed by species. **The owner chose a learned pool
  + 2 unique signature moves per species.**
- **Levels.** The options were a point to spend, automatic growth, or unlocks only. **The owner chose a point to spend.**
- **Training.** The options were a sparring bout, a quick drill, or both. **The owner chose a quick drill.**
- **PvP stakes.** The options were glory + XP, a wager, or the loser lightly hurt. **The owner chose glory + XP.**

## The design

### Body stats (project 1)
- **Five body stats** — Force, Agilité, Endurance, Intelligence, Vitesse (the owner added the last two after
  the design pass) — are **appended** to `PlynlingStat` (append-only, as already planned in
  `Helpers/Plynling/PlynlingStats.cs`), with names, emojis and growth columns.
- Each one is a hashed 1d6 (`BaseSalt + stat`, which continues naturally) + a species lean + trait modifiers
  (`TraitInfo.Stats` already takes any `PlynlingStat`) + growth + **points spent** + state. Existing
  Plynlings get theirs instantly, with no backfill.
- The passion's +2 stays on the five mind stats *(10-28 call)*. The body stats show in « Personnalité ».
- **Story files may test and grow the body stats** *(my call)*. Expedition scenes need them, and
  `StoryVocabulary` already accepts any `PlynlingStat`. Their tests use the body-stat odds curve
  (see *XP and levels*), not the mind stats' clamp.

### XP and levels (project 1)
- The Plynling has its own XP and level, **separate from member XP** (`xp.md`). Plynling actions still
  grant no member XP. Name it distinctly in code (e.g. `RpgXp`, `RpgLevel`) to avoid confusion with
  `XpService`.
- **Each level-up gives one point** that the owner places in any of the five body stats. The owner chose this,
  so two same-level Plynlings play differently. **No respec at launch** *(my call)*.
- Levels also **unlock**: move slots 3 and 4 (project 2), and harder expedition places (project 2).
- **Level cap 100, reached in about a year of daily play** (owner). The XP curve is the builder's,
  aimed at that pace. Training alone (project 1) must not outrun it: once expeditions and fights
  exist, training is roughly a third of the XP *(my call)*. Pin the curve in the checks (days to
  level 10 / 50 / 100 with daily play).
- **A level is an edge, not a win** (owner). A point stays +1, but fights and body-stat tests use a
  **softer curve** than the events' `50 + 5 × (stat − difficulty)` clamp (`PlynlingEventEngine.Chance`),
  so being about 10 levels up is a clear advantage, never a sure win, and anyone may challenge anyone. With about 99
  points at the cap, project 1 defines the body-stat odds formula, because stats and points can't be
  undone (no respec). The battle formula follows it in project 2, tuned by simulation.
- XP sources: training (project 1), expeditions (project 2), fights (project 3).
- The RPG starts at **ado** *(my call, matching 10-28's expedition gate)*: a bébé can't train.
- Progress lives on the Plynling: death loses it with the creature, and a staff resurrection keeps it.

### Énergie (project 1)
- From 10-28: 0–100, refills fully in 24 h, **+5 max per Endurance point**, stored as a value at an
  instant and derived like the needs (rebases on change, nothing ticks). Shown on the card.
- **Only innate Endurance raises the max** (the die, the species lean and the traits), never spent points or growth
  *(my call)*. Otherwise high levels would train more often and level faster, which snowballs.
- Costs *(my call)*: training ~15, fights ~20, expeditions 30/50/70 (short/medium/long, 10-28).
  Care, play and visits cost none.

### `/plynling train` (`/pl train`) — the quick drill (project 1)
- Pick a drill (any of the five body stats) and the result comes at once: a gendered line, XP, and a **rare permanent
  +1** to the drilled stat (0.5 %, none at the cap) *(my call: "a chance for a stat boost" read as permanent and rare; first set at 5–10 %, lowered after review — at ~8 drills a day that gave ~200 permanent +1s a year against 99 level points, so growth would have decided body stats, not levels)*.
  From project 2 on, it also has a small chance to **teach a move** from the pool.
- Refused while sick, frozen, asleep, away, or a bébé, or with too little energy. Énergie is the only
  cooldown *(my call)*.
- **The drill's result is private (ephemeral); a level-up is one short public line** with mentions off
  *(my call: about six drills a day each, posted publicly, would be spam)*. No `;` shortcut at launch, because a typed
  shortcut can only answer publicly.
- Rolls are hashed (`StableRoll`), never a `Random`.

### Battle engine and moves (project 2, with expeditions)
- **Turn-based, one public message** (Components V2) with health bars. Each turn both sides pick a move, then the turn
  resolves. **Vitesse decides who goes first, Force how hard, Agilité dodging and aim, Endurance the health,
  Intelligence the special and signature moves** *(my call)*. A fight lasts a
  few turns, not twenty.
- **Moves:** a shared pool of ~12–16, plus **2 signature moves per species** (7 adoptable mushroom
  species today = 14). A Plynling starts with 2 and carries 4 (slots 3 and 4 unlock by level), and
  the owner picks which. Move keys are stored, so append-only.
- In expeditions, the battle runs against **creatures** whose moves the engine picks.
- **Balance by simulation in `tests/ProjectSYNCS.Checks`**, never by feel (like `VisitSimulation`).

### Expeditions (project 2)
- The 10-28 design holds as written: one sitting, one public message rewritten in place, the owner
  clicks, 2–4 options with the stat and odds shown, always one safe option, wounds and
  « Rentrer / Continuer », rescue on the **second** wound (owner, 2026-11-23, for every expedition: on a
  three-scene trip a third wound could only land on the last scene), never death, abandon after 15 min, the run
  persisted, then away 1–4 h; place-specific loot set, small cailloux (< a `/work` shift), rare
  cosmetics, story chains (`ConditionKind.Chain` in `StoryConditions.cs` is the placeholder).
- What changes: some scenes are **creature fights** on the battle engine, and runs give **XP**.
- Runs on the story engine and story files (`plynling-storylines.md`). **The first place's content
  gets its own deep design session** before it's written (10-28, the owner's wish), using
  `docs/village-lore.md`.

### Plynling-vs-Plynling fights — `/plynling fight` (project 3)
- Someone challenges, and the other owner **accepts** (consent). **Both owners are present** and each picks moves in secret.
- A missed turn (~60 s) is played by the Plynling from its traits. **Three missed turns in a row forfeit**
  *(my call)*.
- **Stakes: glory + XP.** The winner gets good XP and the loser a little. Nothing is lost. Wins and losses are recorded
  with a ranking (the totals + daily buckets pattern, `architecture.md`).
- **Fights nudge the relationship** through `ApplyBondChangeAsync` *(my call)*: repeated fights drift a pair
  toward rivals, and a close, fair one can draw them closer.

### Constraints from the project rules (all projects)
- Append-only: stats, moves, places, scenes, journal kinds, modifiers, metric keys.
- Every Plynling line is a `GenderedLines` and follows `docs/plynling-writing-style.md`.
- Components V2: 40 components across the whole tree, 80-char labels, one custom-id verb per row,
  `AllowedMentions.None`, static measurable builders, defer before database work. Button colours per
  CLAUDE.md: a move pick is blurple, Rentrer/Abandonner is red.
- Concurrency: every new `PlynlingService` entry point goes through `UnitAsync`.
- `/plynling` has 16 of 25 subcommands now: +train, +expedition, +fight (and maybe a ranking) still
  fit. Each lands in `/pl` too. `/plynling help` is close to 6000, so measure.
- Each project is a migration plus a subcommand, so a **minor** bump, a CHANGELOG entry, `/help`, README and
  `/plynling help`. A new `docs/agents/plynling-rpg.md` note gets a row in CLAUDE.md's table.
- The mascot stays out of the RPG *(my call, as in 10-28)*: no level, no training, can't be challenged.

### Left to the builder
The XP curve's shape (aimed at cap 100 in about a year), the body-stat odds formula (aimed at "an edge, not a win"), exact energy and drill numbers, species leans, table shapes, custom-id verbs,
move numbers (set by the simulation), and how the public message refreshes on timeouts.

## Next steps

1. Save this document as `docs/superpowers/specs/2026-11-23-plynling-rpg-design.md` and note in the
   10-28 spec that it is superseded where they differ.
2. Run `superpowers:writing-plans` for **project 1 (foundation)**: body stats, XP and levels with points,
   Énergie, `/pl train`, card and « Personnalité » display, help/README/changelog, and the subsystem note.
3. Projects 2 (expeditions with the battle engine and moves, plus a content session for the first place)
   and 3 (PvP fights) get their own plans later.

## Verification (project 1)
- `dotnet build` (net10.0, `-warnaserror` rules) and the migration applied on startup.
- `dotnet run --project tests/ProjectSYNCS.Checks` gets new cases: energy derivation across rebases,
  level-up points, hashed drill rolls, the card's component count, and `/plynling help` under 6000.
- Dev guild: adopt or use an ado, `/pl train` several times, check the energy drain and the card, level up
  and spend a point, and confirm the « Personnalité » breakdown shows the body stats.
