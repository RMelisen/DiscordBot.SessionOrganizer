# Channel message milestones Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Count each text channel's messages (history included), announce 1 000 / 10 000 / 100 000 /
every 100 000 when reached live, and list them in `/debug milestones`.

**Architecture:** A singleton hosted service reads each channel's history forward from a saved
cursor (REST, 100 a page), then counts live from `BotService`'s fan-out, uncounting deletions.
Counts and cursors are saved every 30 min; milestones are written when reached. Pure rules live in
`Helpers/Admin/ChannelMilestones`, lines in `BotResponses.Milestones.cs`.

**Tech Stack:** .NET 10, Discord.Net 3.20, EF Core + SQLite, `tests/ProjectSYNCS.Checks` (plain console).

**Spec:** `docs/superpowers/specs/2026-11-17-channel-milestones-design.md`

## Global Constraints

- Milestones: 1 000, 10 000, 100 000, then every 100 000.
- Never announce a milestone crossed while reading history; record it silently (`Announced = false`).
- Deleted messages (single and bulk) with id ≤ cursor are uncounted, never below 0.
- Save changed counts every 30 minutes and on a clean stop; never a database write per message.
- Snowflakes `.HasConversion<long>()`; no `DateTimeOffset` comparison in SQL.
- User-facing text French, code and logs English; `/debug` stays owner-only.
- Every guild the bot is in (so the dev guild can be checked), text channels only (no threads, no
  voice/stage chats), minus XP-excluded channels (hardcoded + `/config`), and only where she can
  view and read history.
- Minor bump (migration): 7.63.0, CHANGELOG entry.

## Review Focus

- A message arriving while its channel is still catching up must be counted once, not zero or twice
  (held, then replayed above the cursor at hand-over).
- A deletion of a message newer than the cursor (not yet counted) must not decrement.
- Crossing a milestone again after deletions must not announce it twice (unique row, `Reached` set).
- A channel the bot loses access to mid-scan must not stop the loop (per-channel `try`).
- A milestone hit by a system message (not `IUserMessage`) is recorded, not announced.

---

### Task 1: Pure milestone rules

**Files:**
- Create: `ProjectSYNCS/Helpers/Admin/ChannelMilestones.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 49)

**Interfaces:**
- Produces: `ChannelMilestones.IsMilestone(long count) : bool`, `ChannelMilestones.NextAfter(long count) : long`,
  `ChannelMilestones.Format(long n) : string` (fr-FR grouping, « 10 000 »).

- [ ] Step 1: checks section 49 — `IsMilestone` true for 1000, 10000, 100000, 200000, 1300000; false for
  0, 999, 5000, 50000, 150000, 100001. `NextAfter(0)=1000`, `(1000)=10000`, `(9999)=10000`,
  `(10000)=100000`, `(100000)=200000`, `(250000)=300000`. `Format(10000)` equals
  `10000.ToString("N0", fr-FR)` and contains no `,` or `.`.
- [ ] Step 2: run checks, expect FAIL (type missing → build error).
- [ ] Step 3: implement.
- [ ] Step 4: run checks, expect PASS.

### Task 2: Models, DbContext, migration

**Files:**
- Create: `ProjectSYNCS/Models/Admin/ChannelMessageCount.cs`, `ProjectSYNCS/Models/Admin/ChannelMilestone.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs` (DbSets + `OnModelCreating`)
- Create: migration `AddChannelMilestones`

**Interfaces:**
- `ChannelMessageCount { int Id; ulong GuildId; ulong ChannelId; long Count; ulong Cursor; }`, unique (GuildId, ChannelId).
- `ChannelMilestone { int Id; ulong GuildId; ulong ChannelId; long Milestone; ulong MessageId; ulong AuthorId; DateTimeOffset ReachedAt; bool Announced; }`,
  unique (ChannelId, Milestone), index (GuildId).

- [ ] Step 1: add models and config (every ulong `.HasConversion<long>()`).
- [ ] Step 2: `dotnet ef migrations add AddChannelMilestones`; check the migration creates only the two tables.
- [ ] Step 3: `dotnet build -warnaserror`, expect 0 errors.

### Task 3: `ChannelMilestoneService`

**Files:**
- Create: `ProjectSYNCS/Services/Admin/ChannelMilestoneService.cs` (public sealed, `BackgroundService`)

**Interfaces:**
- Consumes: Task 1, Task 2, `GuildConfigService.GetAsync`, `XpTracker.HardcodedExcludedChannels`,
  `SilenceService.IsSilent`, `ChatHush.IsHushed`, `BreakdownService.IsActive`, `BotChat.ReplyWithTypingAsync`.
- Produces: `HandleMessageAsync(SocketMessage)`, `HandleDeletedAsync(Cacheable<IMessage,ulong>, Cacheable<IMessageChannel,ulong>)`,
  `HandleBulkDeletedAsync(IReadOnlyCollection<Cacheable<IMessage,ulong>>, Cacheable<IMessageChannel,ulong>)`,
  `ViewAsync(ulong guildId, ulong channelId) : Task<MilestoneView?>` with
  `record MilestoneView(long Count, bool CaughtUp, IReadOnlyList<ChannelMilestone> Reached, long Next)`.

- [ ] Step 1: in-memory state per channel (count, cursor, caught-up, dirty, held messages ≤ 500,
  reached milestones), one lock. Loop tick 1 min: on first tick load rows; each tick add new tracked
  channels and catch up those not caught up (REST `GetMessagesAsync(cursor, Direction.After, 100)`,
  ascending, per-channel `try`); every 30 min save dirty channels (per-row `try`); `StopAsync` saves.
- [ ] Step 2: live counting, hand-over replay, deletions, record (one write) and announce (reply,
  pinging only the author; none for a bot), skipping silence, hush and breakdown.
- [ ] Step 3: `dotnet build -warnaserror`.

### Task 4: Wiring and lines

**Files:**
- Modify: `ProjectSYNCS/Program.cs` (singleton + `AddHostedService(sp => …)`), `ProjectSYNCS/Services/Core/BotService.cs`
  (`MessageReceived` fan-out, `MessageDeleted`, `MessagesBulkDeleted`)
- Create: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Milestones.cs`:
  `MilestoneLines`, `MilestoneBotLines`, `MilestoneSelfLines`; {0} name, {1} number (« 10 000 »), {2} channel mention.
- Test: checks lint list + every line uses {1}.

- [ ] Step 1: wire; Step 2: lines; Step 3: checks PASS, build clean.

### Task 5: `/debug milestones`

**Files:**
- Create: `ProjectSYNCS/Commands/Admin/DebugModule/DebugModule.Milestones.cs`, `ProjectSYNCS/Commands/Admin/MilestoneCards.cs`
  (`static Embed Build(string channelName, MilestoneView view)`)
- Modify: `DebugModule.cs` constructor (inject the service)
- Test: checks measure the embed with 30 milestones under 6000 / 4096.

- [ ] Step 1: owner check, guild check, `DeferAsync(ephemeral: true)`, `FollowupAsync(embed)`.
- [ ] Step 2: checks PASS, build clean.

### Task 6: Docs, version, verify

- [ ] `docs/agents/admin.md` (counting + `/debug milestones`), `docs/agents/architecture.md` (the loop),
  `docs/agents/personality/ambient.md` (the announcement row), `CLAUDE.md` fan-out list,
  BotResponses index, `config.yaml` 7.63.0, CHANGELOG.
- [ ] Checks PASS, `dotnet build -warnaserror` clean.
