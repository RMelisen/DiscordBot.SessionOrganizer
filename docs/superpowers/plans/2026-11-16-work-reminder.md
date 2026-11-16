# `/work` DM reminder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Members can switch on, from `/balance`, a single DM telling them when `/work` is ready again.

**Architecture:** Two columns on `PebbleWallet`, a pure due rule in `PebbleEconomy`, a toggle button
on the `/balance` card, and a dedicated `WorkReminderService` loop that claims each due wallet and
then DMs.

**Tech Stack:** .NET 10, Discord.Net, EF Core + SQLite.

**Spec:** `docs/superpowers/specs/2026-11-16-work-reminder-design.md`

## Global Constraints

- User-facing strings French; command/option/custom-id names English.
- No `DateTimeOffset` comparison in SQL: filter `WorkReminder` in SQL, dates in memory.
- Singleton loop opens `CreateAsyncScope()` per unit of work; per-item `try`; own interval (2 min).
- Button handler defers before DB work; ephemeral card.
- DMs: `AllowedMentions.None`, `CannotSendMessageToUser` swallowed, no channel fallback.
- Lines picked with `ResponsePicker.Pick`, written per `docs/syncs-voice.md`.
- Version `7.57.9` → `7.58.0` (minor: migration + new behaviour), `CHANGELOG.md` entry.

## Review Focus

1. Switching on after a cooldown already ended → no DM until the *next* shift ends (pinned in checks).
2. Two `/work` shifts with the bot down in between → still one DM per shift, never two (claim before send).
3. Working, then switching on mid-cooldown (the main use) → this shift's DM goes out. Switching on
   stamps `now` only when `/work` is already available (`PebbleEconomy.ReminderStampOnEnable`).
4. A member who never worked (`LastWorkAt` null) opts in → never due until they work.
5. A forged `bal:remind:` click on someone else's card → impossible: the card is ephemeral and the handler acts on `Context.User`.

---

### Task 1: Model, migration, due rule

**Files:** Modify `Models/Economy/PebbleWallet.cs`, `Helpers/Economy/PebbleEconomy.cs`,
`tests/ProjectSYNCS.Checks/Program.cs` (section 40); Create migration `AddWorkReminder`.

**Produces:** `PebbleWallet.WorkReminder : bool`, `PebbleWallet.WorkRemindedAt : DateTimeOffset?`,
`PebbleEconomy.WorkReminderDue(DateTimeOffset? lastWorkAt, DateTimeOffset? remindedAt, DateTimeOffset now) : bool`,
`PebbleEconomy.ReminderStampOnEnable(DateTimeOffset? lastWorkAt, DateTimeOffset? remindedAt, DateTimeOffset now) : DateTimeOffset?`
(returns `now` when `/work` is available now, else `remindedAt` unchanged).

- [ ] Add checks to section 40: due at `peAt+4h` with `remindedAt` null; not due at `peAt+4h-1s`;
  not due with `lastWorkAt` null; not due when `remindedAt == peAt+5h` (switched on after it ended);
  due when `remindedAt < peAt` (reminded for an earlier shift); enabling at `peAt+1h` then due at
  `peAt+4h`; enabling at `peAt+5h` then not due at `peAt+6h`; enabling with `lastWorkAt` null then not due.
- [ ] Implement the columns and `WorkReminderDue`; `dotnet ef migrations add AddWorkReminder`.
- [ ] `dotnet run --project tests/ProjectSYNCS.Checks` → no economy failures.

### Task 2: Toggle on `/balance`, hint on `/work`

**Files:** Modify `Commands/Economy/EconomyModule.cs`, `Services/Economy/PebbleService.cs`,
`Services/Plynling/PlynlingShortcutService.cs` (if it needs the flag); Create
`Interactions/Components/EconomyComponentHandler.cs`.

**Produces:** `PebbleBalance` gains `bool WorkReminder`; `WorkResult` gains `bool WorkReminder`;
`PebbleService.SetWorkReminderAsync(ulong guildId, ulong userId, bool on, DateTimeOffset now) : Task`
(via `EconomySave.RetryAsync`; on → `WorkRemindedAt = ReminderStampOnEnable(...)`);
`EconomyModule.BalanceCard(PebbleBalance, DateTimeOffset now, string? notice) : (string Text, MessageComponent Components)`.
Custom-ids `bal:remind:on` / `bal:remind:off` (button shows the action, not the state).

- [ ] Build the card statically; `/balance` responds with it, ephemeral.
- [ ] Handler: `DeferAsync()`, set the flag, re-read the balance, `ModifyOriginalResponseAsync` with the card and a notice.
- [ ] `WorkReply` footer appends « · 🔔 rappel en MP : `/balance` » only when `!result.WorkReminder`.
- [ ] `dotnet build`.

### Task 3: `WorkReminderService`

**Files:** Create `Services/Economy/WorkReminderService.cs`; modify `Program.cs`,
`Services/Economy/PebbleService.cs`, `Services/Personality/BotResponses/BotResponses.Commands.Work.cs`
(+ index line in `BotResponses.cs`).

**Consumes:** `PebbleEconomy.WorkReminderDue`.
**Produces:** `PebbleService.GetReminderWalletsAsync() : Task<List<PebbleWallet>>` (no tracking, `WorkReminder` true),
`PebbleService.ClaimWorkReminderAsync(int walletId, DateTimeOffset now) : Task<bool>` (re-checks due, stamps, saves);
`BotResponses.WorkReadyLines : string[]` (no placeholders).

- [ ] Loop: `Task.Delay(2 min)`, skip unless connected, read candidates, per item: claim → DM `Pick(WorkReadyLines)`.
- [ ] Register `AddHostedService<WorkReminderService>()`.
- [ ] `dotnet build`.

### Task 4: Docs and version

- [ ] `HelpModule` `/balance` line mentions the button; `README.md` command row + Cailloux paragraph.
- [ ] `docs/agents/economy.md` rule; `docs/agents/architecture.md` loop list (fifteen).
- [ ] `config.yaml` 7.58.0; `CHANGELOG.md` `## 7.58.0 — 2026-11-16`.
- [ ] `dotnet build` (`ProjectSYNCS`) and the checks project pass.
