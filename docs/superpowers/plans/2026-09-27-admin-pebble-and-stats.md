# Admin: cailloux and stats — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/admin pebble add|remove` and `/admin stats`.

**Architecture:** `PebbleService.AdjustAsync` for the balance; a transient `AdminStatsService`
aggregating stored state into an `EconomyStats` record; a static `AdminCards.BuildStats` embed.
Both commands live in `AdminModule` and gate on `SessionPermissions.IsStaff`.

**Tech Stack:** C# / .NET 10, Discord.Net 3.20, EF Core SQLite, the scratch harness.

Spec: `docs/superpowers/specs/2026-09-27-admin-pebble-and-stats-design.md`.

## Global Constraints

- French user-facing text, English command names; ephemeral replies; `AllowedMentions.None`.
- Staff gate in code only (`IsStaff`), as every `/admin` command.
- SQLite cannot compare `DateTimeOffset` in SQL: filter by ids/booleans, then in memory.
- Checks first in the harness; build `-warnaserror`; commit per task; push only when asked.

---

### Task 1: `/admin pebble add|remove`

**Files:** `ProjectSYNCS/Services/PebbleService.cs` (`AdjustAsync(ulong guildId, ulong userId,
long delta)` → `(long Old, long New)`, clamped at 0, one save), `ProjectSYNCS/Commands/AdminModule.cs`
(nested `[Group("pebble")] PebbleAdminModule` with `add` / `remove`).

- [ ] Checks: add, remove, clamp at 0, a first-time wallet created; `/admin pebble` holds add/remove.
- [ ] Implement; build; run; commit « Added /admin pebble ».

### Task 2: `/admin stats`

**Files:** `ProjectSYNCS/Services/AdminStatsService.cs` (transient, registered), `ProjectSYNCS/Commands/AdminCards.cs`,
`ProjectSYNCS/Commands/AdminModule.cs` (`stats`).

- [ ] Checks (in-memory DB): totals, wallets, average, top 5 order; alive / frozen / graves with an
  unnoticed starvation counted as a grave; species counts; completions per set; discoveries;
  cosmetics held, worn and the top 3; embed within caps.
- [ ] Implement; build; run; commit « Added /admin stats ».

### Task 3: Docs and version

- [ ] README (staff table and the admin paragraph), CLAUDE.md (the `/admin` note), `/help`'s staff
  field (re-check caps); commit; bump the version as its own commit; push only if asked.
