# Admin dashboard — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record the economy day by day and show it to staff as `/admin dashboard`.

**Architecture:** A counter table `EconomyDailyStat` written by a static, never-saving
`EconomyLog.Add` from every place cailloux or items move, inside the action's own save. A transient
`EconomyDashboardService` reads a window; a static `AdminCards.BuildDashboard` renders it.

**Tech Stack:** C# / .NET 10, EF Core SQLite, Discord.Net 3.20, the scratch harness.

Spec: `docs/superpowers/specs/2026-09-28-admin-dashboard-design.md`.

## Global Constraints

- Metric keys are stored strings: append-only, never renamed.
- `EconomyLog.Add` never saves; a refused action records nothing.
- Day keys are `AppTime.DayKey` (Paris `yyyymmdd`); date windows filter on the int in SQL.
- Snowflakes need `HasConversion<long>()`; French text, English names; staff gate in code only.
- Checks first in the harness; build `-warnaserror`; commit per task; push only when asked.

---

### Task 1: Recording

**Files:** `Models/EconomyDailyStat.cs`, `Data/AppDbContext.cs`, migration `AddEconomyDailyStats`,
`Helpers/EconomyLog.cs` (metric constants + `Add`), and one call at every point of the spec's table
(`PebbleService`, `PlynlingService`, `InventoryService`, `CosmeticService`).

**Produces:** `EconomyLog.Add(AppDbContext db, ulong guildId, string metric, long amount, DateTimeOffset now)`;
constants `EconomyLog.EarnWork`, … (one per key in the spec).

- [ ] Checks (in-memory DB): each action records its metric and amount once; nothing on a refusal
  (too poor, wasted meal, not in shop…); two adds of one metric in one save land in one row.
- [ ] Implement; build; run; commit « Recorded the economy day by day ».

### Task 2: `/admin dashboard`

**Files:** `Services/EconomyDashboardService.cs` (transient, registered), `Commands/AdminCards.cs`
(`BuildDashboard`, `Sparkline`), `Commands/AdminModule.cs` (`dashboard`), a handler for `dash:win:*`.

**Produces:** `enum DashboardWindow { Week, Month, All }`; `DashboardData GetAsync(guildId, window, now)`
with per-metric daily series for the window and totals for the previous window.

- [ ] Checks: windows (today + 6 days before, Paris), previous-window comparison and « nouveau »,
  sparkline length/scaling/empty/compressed, the embed within caps with every metric non-zero.
- [ ] Implement; build; run; commit « Added /admin dashboard ».

### Task 3: Docs

- [ ] README (staff table and paragraph), CLAUDE.md (a note: the recording rule and the keys),
  `/help` staff field (caps re-checked); commit.
