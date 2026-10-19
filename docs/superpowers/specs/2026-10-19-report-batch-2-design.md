# Design — `/report` batch 2: activity, commands, Plynlings, quiz, emotes, sleeping members

> **Status (2026-10-19): implemented in 7.6.0.** `MemberInfo` became `MemberSnapshot`
> (`System.Reflection.MemberInfo` made it ambiguous). The task-by-task implementation
> plan is `docs/superpowers/plans/2026-10-19-report-batch-2.md`. Batch 1 is committed (7.5.0).

## Context

Batch 1 (7.5.0) shipped `/report sessions|polls|community|syncs` and the monthly DM
digest, built on `ReportWindow`, `Compared<T>`, `ReportService`, `ReportCards`, `StatsPeriodUi`,
pure helpers plus measurement in `tests/ProjectSYNCS.Checks`. The owner now wants:

- **New data, then reports**: peak hours, channels, voice rooms, her own commands. Nothing records
  any of these. XP is not a message count: one grant per person per 60 s.
- **Reports on data already stored**: Plynlings, quiz, emote health, sleeping members.

Owner decisions:
- The digest gains **activity** and **commands**, for 6 DMs a month. The rest stays on demand.
- Sleeping members: **30 / 60 / 90-day buttons** on the report.
- Message and hour counts happen **everywhere**, excluded channels included (marked 🚫 in the
  report). Bots, webhooks, system messages and DMs never count.

## 1. New data: batched counters

**Why batching.** Counting every message would be the first per-message database write in the bot.
Today every counter is one round trip per event, there's no `SQLITE_BUSY` handling, and the Pi is
where that hurts (root CLAUDE.md, *Reliability*). So the counts accumulate **in memory** and a loop
flushes them.

- **`Services/Core/ActivityCounter.cs`** (singleton, holds its own lock). In-memory sums keyed by:
  - `(guild, day, hour)`: messages and voice person-minutes;
  - `(guild, channel, day)`: messages, voice person-minutes, voice peak (max people at once);
  - `(guild, day, command)`: uses and failures.

  Methods are synchronous and allocation-light: `CountMessage(SocketMessage)`,
  `CountVoice(guildId, channelId, humans, now)`, `CountCommand(guildId, path, success, now)`.
  `Drain()` swaps the dictionaries out under the lock; `Restore(batch)` merges a failed batch back
  in.
- **`Services/Core/ActivityFlushService.cs`**: a `BackgroundService` with **its own 5-minute
  interval** (loop no. 12).
  - Drains, opens one scope, and upserts each guild's rows in its own `try` (per item, root rule).
  - On failure it `Restore`s that guild's batch for the next pass.
  - Also flushes in `StopAsync`, so a clean restart loses nothing; a crash loses at most 5 minutes
    (in-memory state resets by design).
- **Pure key logic in `Helpers/Core/ActivityKeys.cs`**:
  - Day and hour come from **one** `AppTime.ToZoned` instant, so midnight can't split them.
  - A thread counts under its parent (`SocketThreadChannel.ParentChannel`, the idiom at
    `XpTracker.cs:371`).
  - `CommandPath.Of(SlashCommandInfo)` walks `Module.Parent` for `SlashGroupName` (nested groups:
    `/config excluded-channels add`). It maps `pl …` to `plynling …` so the two registrations count
    together.
- **Hooks**:
  - `BotService.HandleMessageAsync`: `_activity.CountMessage(raw)` right after the shortcut return
    (`BotService.cs:146`), before every early-return handler. A typed shortcut counts as a command,
    not a message.
  - `BotService.StartAsync`: `_interactions.SlashCommandExecuted += …` builds the path, reads
    `IResult.IsSuccess` and calls `CountCommand`. Slash commands only; buttons and modals are not
    features. DMs use GuildId 0.
  - `PlynlingShortcutService` (after `Parse` succeeds, `PlynlingShortcutService.cs:~58`):
    `CountCommand(guild, ";" + verb, true)`.
  - `VoiceXpService` sweep, inside the per-channel loop after the spectator call
    (`VoiceXpService.cs:~115`), in its own `try`: `CountVoice(guild, channel, humans)`. Every
    connected human counts, muted included, and the AFK channel is already skipped (line 88).
    That's presence, unlike `MemberDailyStat.VoiceMinutes`, which only counts XP-eligible minutes.
- **Models** (`Models/Core/`): daily buckets only, with no totals table (the `MemberFlowDailyStat`
  precedent). All snowflakes get `.HasConversion<long>()`, each table has a unique upsert index,
  and there is no `HasMaxLength`.
  - `ActivityHourStat {GuildId, Day, Hour, Messages, VoiceMinutes}`, unique on (GuildId, Day, Hour);
  - `ChannelDailyStat {GuildId, ChannelId, Day, Messages, VoiceMinutes, VoicePeak}`, unique on
    (GuildId, ChannelId, Day);
  - `CommandDailyStat {GuildId, Day, Command, Uses, Failures}`, unique on (GuildId, Day, Command).
    `Command` is a **stored string key, append-only**: renaming a command orphans its old rows,
    which then show under the old name. Documented.
- **Migration**: `ActivityStats`, schema only.

## 2. Reports

`ReportKind` gains `Activity, Commands, Plynlings, Quiz, Emotes, Sleeping`, **appended** (the names
are in custom-ids). That makes 10 `/report` subcommands (25 allowed). Each report follows the batch-1
shape: a pure helper in `Helpers/<subsystem>/XReport.cs`, a fetch in a transient service, and
`ReportCards.BuildX(Compared<T>, ReportWindow, bool names)`.

Several reports need the live guild (channel lists, emotes, members, registered commands):
- `ReportService` takes `DiscordSocketClient` (it already has it) and `InteractionService`;
- it passes plain snapshots (ids, names, flags) into the helpers, so they stay pure.

| Report | Shows |
|---|---|
| **`activity`** (digest) | **Peak hours**: a 24-slot sparkline of messages and one of voice, the 3 busiest hours, the busiest weekday. **Channels**: top 8 text channels by messages, with trend; channels with no message in the window; 🚫 on excluded ones. **Voice rooms**: top voice channels by person-minutes, with peak; rooms nobody used. Channels render as `<#id>` |
| **`commands`** (digest) | Total uses, with trend. Top 15 commands, with failure counts. Commands **never used** in the window (registered slash paths minus used ones). Shortcuts (`;pl`, `;work`…) on their own line |
| **`plynlings`** | Births and deaths in the window (by cause) and the average lifespan of the dead. The living now: sick, hungry or starving, dirty, frozen, by stage (settled in memory via `PlynlingLife`, as `/admin stats` does). Events resolved, decided by the owner vs. alone (a neglect signal), challenge success rate, most common events (`PlynlingEvents.ByKey(...).Title`). Bonds formed (journal `Became*`), badges and traits gained. *Names*: « à surveiller », owners of sick or starving pets. The mascot is excluded (`PlynlingMascot.Is`); species is not repeated (it's in `/admin stats`) |
| **`quiz`** | Rounds posted (`MessageId != 0`), found vs. unanswered, found rate by category and by difficulty (`QuizBank.ByKey`), median time to answer (`WonAt − PostedAt`), the questions nobody found (prompt, capped). *Names*: top winners, fastest answer |
| **`emotes`** | The server's own emotes (`guild.Emotes`) ranked in the window, with trend; **never used** in the window; rising and falling vs. the previous window; usage split between own emotes, other servers' emotes and Unicode |
| **`sleeping`** | Members (no bots) on the server for longer than N days with **no activity day** (XP, reaction or voice, `MemberDailyStat`) in the last N days. Ordered by longest silence; each shows « dernier signe : dd/MM » or « jamais (depuis le 09/08/2026) », the date the XP buckets became reliable. Total count; list capped at 2 fields. Buttons **30 / 60 / 90 days** (`report:sleep:{days}`, own verb), no period row. Footer: activity in excluded channels doesn't count, so someone who only talks there looks asleep |

Fix on the way: `CommunityReport` counts quiz rounds whose send failed; filter `MessageId != 0`, as
the quiz report will.

**Digest**: `MonthlyDigestService` stops looping `Enum.GetValues<ReportKind>()` and sends an explicit
`ReportKinds.Digest = [Sessions, Polls, Community, Syncs, Activity, Commands]`. `Sleeping` has no
window and stays out.

## Files

New:
- `Services/Core/ActivityCounter.cs`, `Services/Core/ActivityFlushService.cs`
- `Helpers/Core/ActivityKeys.cs` (with `CommandPath`)
- `Models/Core/ActivityHourStat.cs`, `ChannelDailyStat.cs`, `CommandDailyStat.cs`, plus the migration
- `Helpers/Core/ActivityReport.cs`, `Helpers/Core/CommandReport.cs`
- `Helpers/Plynling/PlynlingReport.cs`, `Helpers/Quiz/QuizReport.cs`
- `Helpers/Xp/EmoteReport.cs`, `Helpers/Xp/SleepingReport.cs`
- transient fetch services: `Services/Core/ActivityReportService.cs` (activity and commands),
  `Services/Plynling/PlynlingReportService.cs`, `Services/Quiz/QuizReportService.cs`,
  `Services/Xp/EmoteReportService.cs` (emotes and sleeping)

Changed:
- `Data/AppDbContext.cs`, `Program.cs`
- `Services/Core/BotService.cs`, `Services/Plynling/PlynlingShortcutService.cs`,
  `Services/Xp/VoiceXpService.cs`
- `Services/Admin/ReportService.cs`, `Commands/Admin/ReportCards.cs`,
  `Commands/Admin/ReportModule.cs`, `Interactions/Components/ReportComponentHandler.cs`
  (`report:sleep:*`)
- `Services/Admin/MonthlyDigestService.cs`, `Helpers/Xp/CommunityReport.cs`

Docs:
- `README.md`; `/help`: the `/report` line stays generic, re-measured by the checks;
  `CHANGELOG.md`.
- `docs/agents/admin.md`: the reports and the digest list.
- `CLAUDE.md`: twelve loops (`ActivityFlushService`, 5 min); the batched-counter pattern (when to use
  it, and that it is the only one); the new bucket-only tables; `ActivityCounter` as a singleton
  holding state.

**Version**: 7.5.0 was pushed with batch 1, so this batch is **7.6.0** (new commands plus a
migration), with its own CHANGELOG entry. If `config.yaml` already differs from `HEAD` when you
start, another batch is in flight: extend it instead (root `CLAUDE.md`, *Version and deployment*).

**Notes from exploration** (so you don't have to redo it):
- **Hook sites**:
  - `BotService.HandleMessageAsync` fan-out: the shortcut return is at `BotService.cs:~146`, and
    the early-return handlers start around line 168.
  - `VoiceXpService.SweepAsync` per-channel loop: the spectator `try` computes `humans`, around
    lines 105-115; the AFK channel is skipped above it.
  - `PlynlingShortcutService.HandleMessageAsync`: count after `PlynlingShortcut.Parse` succeeds and
    the repeat gate is claimed. Key it from `shortcut.Kind` (Work, Forage, View, Visit), not the raw
    text.
- **Commands**:
  - No `SlashCommandExecuted` subscription exists yet.
  - `InteractionService` runs `RunMode.Async` by default, so success and failure only arrive
    through that event.
  - `/pl` and `/plynling` are two module registrations over one base (`PlynlingGroups.cs`).
- **Quiz**: a round whose send failed has `MessageId == 0`. An unanswered round is `Closed`,
  `WinnerId == 0`. Category and difficulty come from `QuizBank.ByKey(key)` (not stored); key
  prefixes are historical.
- **Plynlings**:
  - Abandoned Plynlings are hard-deleted, with their journal and events, so abandons can't be
    reported.
  - Exclude the mascot with `PlynlingMascot.Is`.
  - Settle the needs in memory (`PlynlingLife.Settle` on an `AsNoTracking` load), as
    `AdminStatsService` does.
- **Emotes**:
  - `EmoteDailyStat` exists from 2026-08-05; emotes from other servers are counted too. A server
    emote is one whose id is in `guild.Emotes`.
  - `EmoteStatsService.GetRankingAsync` drops ids, so query the tables yourself.
- **Members**:
  - `AlwaysDownloadUsers` is on, so `guild.Users` is complete.
  - No last-seen is stored anywhere: use `max(Day)` of `MemberDailyStat` rows with any counter
    above 0.
  - XP buckets are reliable from 2026-08-09 (`ResetXpTotals`).
  - `/admin xp` adjustments also write today's bucket.

## Build order (each step builds green)

1. Counter, keys, models, migration, flush loop, the four hooks.
2. `activity` and `commands` reports, and the digest list.
3. `plynlings`, `quiz`, `emotes`, `sleeping` (with its buttons); the `CommunityReport` quiz fix.
4. Checks, docs, version.

## Verification

- `dotnet build -warnaserror` after each step.
- `dotnet run --project tests/ProjectSYNCS.Checks`, extended with:
  - `ActivityKeys`: Paris hour and day from one instant around midnight and DST; a thread maps to
    its parent;
  - `CommandPath`: nested groups; `pl` becomes `plynling`;
  - `ActivityCounter`: count, `Drain` empties, `Restore` merges back;
  - each new helper with small cases: never-used emotes and commands, sleeping thresholds and
    « jamais », a quiz round whose send failed excluded, the mascot excluded;
  - every new card measured at worst (≤ 6000, fields ≤ 1024, ≤ 25 fields), and `names: false`
    leaves no `<@`;
  - `/help` re-measured.
- Dev guild:
  - send messages in a channel and a thread, run a few commands and a `;pl`, sit in voice;
  - after the 5-minute flush, check `/report activity` and `/report commands`;
  - open every new report, click the periods and the 30/60/90 buttons;
  - check `/debug digest` sends 6 reports;
  - restart cleanly and confirm the counts survive (the flush on stop).
