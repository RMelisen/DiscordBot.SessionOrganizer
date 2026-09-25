# Plynlings: badges and the journal — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Per-Plynling badges (stored, paid once) and a journal card of stats, badges and dated moments.

**Architecture:** Pure catalog and wording in `Helpers/PlynlingBadges` + `Helpers/PlynlingJournalUi`; two new tables (`PlynlingBadge`, `PlynlingJournalEntry`) written by `PlynlingService` in the same saves as the actions; the sweep awards what time earns and writes stage and death moments; `/plynling journal` renders `PlynlingJournalCards.BuildJournal`.

**Tech Stack:** .NET 10, EF Core SQLite (migration), Discord.Net 3.20 Components V2.

Spec: `docs/superpowers/specs/2026-09-25-plynling-badges-journal-design.md`.

## Global Constraints

- 16 badges exactly as the spec's table (keys stable, ☀️ for « Un an »); rewards to the owner; stored once per Plynling (unique index).
- Moments: capped at **100** per Plynling; `JournalKind` append-only; gendered at display.
- Checked after actions in the action's save; the sweep handles age badges, stage moments, the death moment.
- Journal card: latest **6** moments per page, `plyn:jprev:` / `plyn:jnext:`, `AllowedMentions.None`.
- Build `-warnaserror`; commit per task, never push; final "Updated version" commit.

### Task 1: Catalog, models, migration
- [ ] Checks (`$SCRATCH/extrascheck`): each badge's condition at its threshold and one below; `Newly` never returns an earned key; the event badges only with their event; moment wording per kind in both genders.
- [ ] `Helpers/PlynlingBadges.cs` (`BadgeInfo`, `All`, `ByKey`, `Newly(p, earned, now, evt)`, `BadgeEvent`), `Helpers/PlynlingJournalUi.cs` (`JournalKind`, `Line(kind, detail, name, gender)`), `Models/PlynlingBadge.cs`, `Models/PlynlingJournalEntry.cs`, `Plynling.Meals/Pets/FedByOthers`, `AppDbContext` (sets, FK cascade, unique index, snowflake conversion), migration `AddPlynlingBadgesAndJournal`. Commit.

### Task 2: Earning in the actions
- [ ] Checks against an in-memory SQLite `AppDbContext`: feeding counts and awards (100 repas at the 100th meal, paid once), a friend's meal counts and journals once, a starving meal earns « Sauvé de justesse », petting, a finished game, a visit (both), resurrection; the journal cap.
- [ ] `PlynlingService`: private `AwardAsync`, `JournalAsync`; hooks in adopt, feed, pet, finish-play, visit, freeze, thaw, resurrect; results carry the new badges; `PlynlingCareService` / play / visit add « 🏅 Nouveau badge … ». Commit.

### Task 3: The sweep
- [ ] `PlynlingService.ProgressAsync(p, now)`: stage moments dated when reached, age (and any other) badges; the death moment when a death is announced. Checked with the in-memory database. Commit.

### Task 4: `/plynling journal`
- [ ] `PlynlingJournalCards.BuildJournal(...)` static; command; paging handlers. Checks: builds for living and dead, ids distinct, 6 per page. Commit.

### Task 5: Docs and version
- [ ] Help, README, CLAUDE.md; commit; bump version; commit.
