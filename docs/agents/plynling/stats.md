# `/plynling stats` — counters, sheet, ranks, duel

A Plynling's life in numbers, five tabs (`StatsTab`: Vie, Soins, Jeux, Social, Collection), or two
Plynlings side by side with `vs:` (8.26.0). Design: `docs/superpowers/specs/2026-11-26-plynling-stats-design.md`.

## The counters — `Helpers/Plynling/PlynlingCounters`

- **`PlynlingCounter`** (Plynling, key, value; unique on the pair) holds every count nobody kept per
  Plynling before. **Keys are stored strings: append-only, never renamed.** A new counter is a new
  `const` (and its line in `PlynlingStatsUi`), never a migration. Per-food and per-game keys are built
  from the enum names (`Food`, `Game`, `GameWon`), so **renaming a `PlynlingFood` or `PlynlingGame`
  member orphans its counts**.
- **`PlynlingCarer`** (Plynling, person: meals, pets) — who cares for it; the owner counts too, the
  « plus grand fan » is the top non-owner.
- **The writers never save**: `AddAsync` / `AddCarerAsync` sit beside the action's `EconomyLog` and
  `PlynlingScore` calls and ride its `SaveChanges`, so a refusal counts nothing. A new Plynling action
  that should show on the sheet needs its call there.
- **Both tables carry their own `Version` token** (`AppDbContext.BumpVersions`): an event decided alone
  can pay `earn.event` without touching the Plynling row, so the row's token alone would not serialise
  two increments. `UnitAsync` retries the conflict and the first-insert race; `TryGiftAsync`, which is
  not retried, drops the draw on that race like on a conflict.
- **What `Settle` finds rides `Plynling.PendingCounters`** (not mapped), written by
  `FlushMomentsAsync` like `PendingScore`: a morning lived sick (`sick.days`, counted from the morning
  after the onset) and every thaw's length (`frozen.seconds`, in `EndFreeze`, the one path for own,
  staff and expired thaws). **A save path after a thaw must flush** (`ThawOnceAsync` does).
- Visits received are `Plynling.Hosted` (8.25.0), not a counter.
- Cascade with the Plynling: gone when abandoned, kept on a grave.

## Counting since

`PlynlingCounters.Since` (2026-11-26, Paris) is when counting started. A Plynling adopted before it is
`Partial`: every line read from the new tables carries « * » and the tab says « compté depuis le 26/11 ».
A property, not a field (it reads `AppTime`'s zone, another file).

## The sheet — `PlynlingService.GetStatSheetAsync`

Read-only and untracked: the counters, the carers, `StatFacts` (journal `FellSick` / `Frozen` counts —
the journal is capped at 500, accepted —, badges, traits and marks by kind, memories, bonds by kind,
meetings and the most met, resolved events, challenges, story endings) and the ranks.

**Ranks** (`RankedStat`): among every Plynling of the guild, dead included, never the mascot — the
population of `/leaderboard`'s Plynlings view; the denominator is that whole population. A value of 0
gets no rank; ties share the better place (1 + how many are strictly ahead). Level ranks by
`PlynlingRpg.Level`, not raw XP. The mascot's sheet has none. **Badges count like the journal**
(`PlynlingJournalCards.BadgeText`): catalogue keys only, once each, so « n / total » never disagrees with
it — the sheet and the badge rank both go through `KnownBadges`.

« Jours au frais » adds a freeze still running to the stored `frozen.seconds` (counted for good at the thaw).

## Lines and the duel — `Helpers/Plynling/PlynlingStatsUi` (pure)

Each line is a `SheetLine` with a stable `Id` (a duel pairs lines by it), a `Value` to compare and a
`Better`: `Higher` for counts, `Lower` for stress, sicknesses, sick mornings and hunger alerts (the
healthier life wins), `None` for dates, stage, the favourite food, the fan, the most met, freezes,
medicine, refused confessions, traits and bonds. **A « * » line is no contest when exactly one side is
`Partial`.** List items (`food.`, `game.`, `bond.`, `mark.`) are one line each, left out at 0 (both
sides at 0 in a duel). Equal values: nobody scores. Labels are neutral: a duel can pair a boy and a girl.

## The cards — `Commands/Plynling/PlynlingStatsCards`, `Interactions/Components/PlynlingStatsHandler`

Components V2: single card 12 of 40, duel 15. One row of five tab buttons with its own verb:
`plst:tab:{id}:{tab}` and `plst:duel:{a}:{b}:{tab}` (tab as its int). Anyone may switch tabs; each
click defers, re-reads and redraws through `CardSend`. A Plynling abandoned since answers
« Ce Plynling n'existe plus. » privately. Checks section 74 (`StatsChecks`) builds every tab of both
cards at their fullest (32-character names, 7-digit values) and walks the counters' hooks against an
in-memory SQLite: run it after adding a line.
