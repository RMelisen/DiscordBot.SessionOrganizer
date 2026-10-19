# `/report` batch 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record peak hours, channel and voice-room activity and command usage (batched in memory, flushed every 5 minutes), and add six staff reports to `/report`: `activity`, `commands`, `plynlings`, `quiz`, `emotes`, `sleeping`. The first two also join the monthly DM digest.

**Architecture:** Gateway hooks feed a singleton `ActivityCounter` with synchronous, in-memory increments. `ActivityFlushService`, its own 5-minute loop, upserts the counts into three bucket-only tables. Every report follows batch 1's shape:
- a pure helper in `Helpers/`, checkable in `tests/ProjectSYNCS.Checks`;
- a transient service that only fetches rows;
- a static `ReportCards.BuildX`;
- routing through `ReportService.BuildAsync`.

The live guild (channels, emotes, members, registered commands) is turned into plain records inside `ReportService` before it reaches a helper.

**Tech Stack:** C# / .NET 10, Discord.Net 3.20 (`Discord.Interactions`), EF Core 10 + SQLite, a plain console checks project (no test framework).

**Spec:** `docs/superpowers/specs/2026-10-19-report-batch-2-design.md` (the approved design, owner decisions included). Read it first, then the root `CLAUDE.md` and `docs/agents/admin.md`, section *`/report` and the monthly digest*: batch 1's conventions are binding here.

## Global Constraints

- **Language:** user-facing strings (descriptions, embeds, buttons) are **French**; code, comments, logs and `CHANGELOG.md` are **English**; command and option names are English.
- **Discord caps:** 6000 characters per message's embeds, 1024 per field value, 256 per title, 25 fields per embed, 5 buttons per row, 100-character slash descriptions. Every card is measured in the checks.
- **SQLite:** never compare `DateTimeOffset` in a query. Filter ints/ids in SQL, then dates in memory. `Day` is the `AppTime.DayKey` int (`yyyymmdd`, Paris). Every `ulong` property needs `.HasConversion<long>()`. No `HasMaxLength`.
- **Stored string keys are append-only:** `CommandDailyStat.Command`, and the `ReportKind` names (they are in custom-ids). Append new enum members at the end, never rename.
- **Lifetimes:** state-holding classes are singletons and open `CreateAsyncScope()` per unit of work. Classes wrapping `AppDbContext` are transient.
- **Time:** never `DateTime.Now`; Paris wall clock goes through `Helpers/Core/AppTime`.
- **Replies:** `/report` replies are ephemeral, sent with `AllowedMentions.None`, gated by `SessionPermissions.IsStaff` in every handler (no `[DefaultMemberPermissions]`). A handler that reads the database defers first.
- **User-chosen text** (Plynling names) goes through `Format.Sanitize`.
- **Version:** **7.6.0** in `ProjectSYNCS/config.yaml`, with its own `## 7.6.0 — <date>` entry in `ProjectSYNCS/CHANGELOG.md`. If `config.yaml` already differs from `HEAD` when you start, another batch is in flight: extend it instead.
- **Commits:** the owner commits by hand. Commit only if the owner asked for commits in this session. Otherwise skip every *Commit* step and leave the work uncommitted.
- **Commands** (run from the repo root):
  - build: `dotnet build ProjectSYNCS/ProjectSYNCS.csproj -warnaserror`
  - checks: `dotnet run --project tests/ProjectSYNCS.Checks`, which prints failures and ends with `ALL CHECKS PASSED` or `N FAILURE(S)`.
  - migration: `cd ProjectSYNCS && dotnet ef migrations add <Name>`. Install the tool first if it's missing: `dotnet tool install --global dotnet-ef`.

## Review Focus

1. **The guild is unavailable** (gateway down, or the digest firing during a reconnect). `GetGuild` returns null, so the snapshots are empty. Every report must still render: no channel, emote or member lists, no crash. Pinned by the empty-snapshot cases in Tasks 5, 9 and 10.
2. **Huge lists** (100+ never-used commands, 60 unused emotes, 40 silent channels) must never break a field, and must never cut an emote's `<:name:id>` in half. `ReportCards.Fit` stops whole items before the cap and appends « … et N autres ». Pinned by the worst-case measurements in Tasks 6, 9 and 10.
3. **Deleted or renamed things.** A channel with counts but no longer in the guild still lists (as `<#id>`); a foreign emote with no `EmoteStat` row falls back to its id; a renamed command shows under its old key. Pinned in Tasks 5 and 9.
4. **Losing counts.** A clean stop flushes; a failed flush puts the counts back for the next pass. Pinned by the `Restore` case in Task 2; checked live in Task 11.
5. **Sleeping-member edges.** A member who joined recently is never listed. A `JoinedAt` of null counts as an old member. Someone never active shows « jamais ». Bots are never listed. Pinned in Task 10.

---

## File map

| File | Responsibility | Task |
|---|---|---|
| `ProjectSYNCS/Helpers/Core/ActivityKeys.cs` | Paris (day, hour) of an instant; `CommandPath` (slash path, shortcut keys) | 1 |
| `ProjectSYNCS/Services/Core/ActivityCounter.cs` | In-memory counts plus `Drain` / `Restore` (singleton) | 2 |
| `ProjectSYNCS/Models/Core/ActivityHourStat.cs`, `ChannelDailyStat.cs`, `CommandDailyStat.cs` | The three bucket tables | 3 |
| `ProjectSYNCS/Services/Core/ActivityFlushService.cs` | 5-minute flush loop and flush on stop | 3 |
| `ProjectSYNCS/Services/Core/BotService.cs`, `Services/Plynling/PlynlingShortcutService.cs`, `Services/Xp/VoiceXpService.cs` | The four hooks | 4 |
| `ProjectSYNCS/Helpers/Core/ActivityReport.cs`, `Helpers/Core/CommandReport.cs` | Pure activity and command arithmetic | 5 |
| `ProjectSYNCS/Services/Core/ActivityReportService.cs` | Fetches activity and command rows | 6 |
| `ProjectSYNCS/Helpers/Quiz/QuizReport.cs`, `Services/Quiz/QuizReportService.cs` | Quiz report | 7 |
| `ProjectSYNCS/Helpers/Plynling/PlynlingReport.cs`, `Services/Plynling/PlynlingReportService.cs` | Plynling report | 8 |
| `ProjectSYNCS/Helpers/Xp/EmoteReport.cs`, `Services/Xp/EmoteReportService.cs` | Emote report | 9 |
| `ProjectSYNCS/Helpers/Xp/SleepingReport.cs` (+ `CommunityReportService.LastActiveDaysAsync`) | Sleeping members | 10 |
| `ProjectSYNCS/Services/Admin/ReportService.cs`, `Commands/Admin/ReportCards.cs`, `Commands/Admin/ReportModule.cs`, `Interactions/Components/ReportComponentHandler.cs` | Routing, cards, subcommands, buttons (each task adds its piece) | 6–10 |
| Docs, `/help`, README, CHANGELOG, `config.yaml`, `CLAUDE.md` | Documentation and version | 11 |

**Adding check sections** to `tests/ProjectSYNCS.Checks/Program.cs`:
- Each task adds a numbered section `// 24. …`, `// 25. …` and so on.
- Wrap it in `{ … }` and insert it **just before** the last two lines of the file:
  `Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} FAILURE(S)");` and
  `return failures == 0 ? 0 : 1;`.
- The file is top-level code: a local whose name is already used in an enclosing scope fails with CS0136. Every local in these sections therefore carries a task prefix (`t1…`, `t2…`).
- `Fail(string)` is the file's failure reporter. `ProjectSYNCS.Helpers` and `ProjectSYNCS.Services` are imported; spell `ProjectSYNCS.Models.*` and `ProjectSYNCS.Commands.*` in full.

---

### Task 1: Activity keys and command paths

**Files:**
- Create: `ProjectSYNCS/Helpers/Core/ActivityKeys.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 24)

**Interfaces:**
- Consumes: `AppTime.ToZoned(DateTimeOffset)` (`Helpers/Core/AppTime.cs`); `ShortcutKind` (`Helpers/Plynling/PlynlingShortcut.cs`: View, Work, Forage, Visit, Unknown); `Discord.Interactions.SlashCommandInfo` / `ModuleInfo` (`Module`, `Parent`, `IsSlashGroup`, `SlashGroupName`, `Name`).
- Produces:
  - `ActivityKeys.DayHour(DateTimeOffset) -> (int Day, int Hour)`
  - `CommandPath.Join(IEnumerable<string> groups, string name) -> string`
  - `CommandPath.Of(SlashCommandInfo) -> string`
  - `CommandPath.Shortcut(ShortcutKind) -> string`

- [ ] **Step 1: Write the failing check.** Add section 24:

```csharp
// 24. Activity keys and command paths (/report activity and commands).
{
    // 23:30 UTC on 19 Oct 2026 (CEST, +2) is 01:30 on the 20th in Paris.
    if (ActivityKeys.DayHour(new DateTimeOffset(2026, 10, 19, 23, 30, 0, TimeSpan.Zero)) != (20261020, 1)) Fail("DayHour misses the Paris day");
    // Clocks go back on 25 Oct 2026 at 01:00 UTC: 01:30 UTC is 02:30 CET.
    if (ActivityKeys.DayHour(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero)) != (20261025, 2)) Fail("DayHour across the clock change");
    // Winter (CET, +1): 23:30 UTC on 1 Dec is 00:30 on the 2nd.
    if (ActivityKeys.DayHour(new DateTimeOffset(2026, 12, 1, 23, 30, 0, TimeSpan.Zero)) != (20261202, 0)) Fail("DayHour in winter");

    if (CommandPath.Join(new[] { "pl" }, "feed") != "plynling feed") Fail("/pl is not counted as /plynling");
    if (CommandPath.Join(new[] { "config", "excluded-channels" }, "add") != "config excluded-channels add") Fail("nested groups");
    if (CommandPath.Join(Array.Empty<string>(), "help") != "help") Fail("top-level command");
    if (CommandPath.Shortcut(ShortcutKind.Work) != ";work" || CommandPath.Shortcut(ShortcutKind.Visit) != ";pl visit") Fail("shortcut keys");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Run `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: a build error, `CS0103: The name 'ActivityKeys' does not exist`.

- [ ] **Step 3: Implement.** Create `ProjectSYNCS/Helpers/Core/ActivityKeys.cs`:

```csharp
using Discord.Interactions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Where in time a counted message, voice minute or command lands (ActivityCounter): the Paris day
/// and hour, both from one zoned instant so midnight can't split them.
/// </summary>
public static class ActivityKeys
{
    public static (int Day, int Hour) DayHour(DateTimeOffset instant)
    {
        var zoned = AppTime.ToZoned(instant);
        return (zoned.Year * 10000 + zoned.Month * 100 + zoned.Day, zoned.Hour);
    }
}

/// <summary>
/// The key a command is counted under (CommandDailyStat.Command, a stored string: append-only).
/// A slash command is its full path without the slash (« config excluded-channels add »); /pl is
/// folded into /plynling, its second registration. A typed shortcut starts with « ; ».
/// </summary>
public static class CommandPath
{
    public static string Join(IEnumerable<string> groups, string name)
    {
        var parts = groups.Append(name).ToList();
        if (parts.Count > 1 && parts[0] == "pl") parts[0] = "plynling";
        return string.Join(' ', parts);
    }

    public static string Of(SlashCommandInfo command)
    {
        var groups = new List<string>();
        for (var module = command.Module; module is not null; module = module.Parent)
            if (module.IsSlashGroup) groups.Add(module.SlashGroupName);
        groups.Reverse();
        return Join(groups, command.Name);
    }

    public static string Shortcut(ShortcutKind kind) => kind switch
    {
        ShortcutKind.View => ";pl",
        ShortcutKind.Work => ";work",
        ShortcutKind.Forage => ";forage",
        ShortcutKind.Visit => ";pl visit",
        _ => ";pl ?",
    };
}
```

- [ ] **Step 4: Run the checks.** Run `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: `ALL CHECKS PASSED`.

- [ ] **Step 5: Commit** (only if the owner asked):

```bash
git add ProjectSYNCS/Helpers/Core/ActivityKeys.cs tests/ProjectSYNCS.Checks/Program.cs
git commit -m "Activity keys and command paths for /report batch 2"
```

---

### Task 2: In-memory activity counter

**Files:**
- Create: `ProjectSYNCS/Services/Core/ActivityCounter.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 25)

**Interfaces:**
- Consumes: `ActivityKeys.DayHour` (Task 1).
- Produces (all in `ProjectSYNCS.Services`):
  - `readonly record struct HourKey(ulong GuildId, int Day, int Hour)`
  - `ChannelKey(ulong GuildId, ulong ChannelId, int Day)`
  - `CommandKey(ulong GuildId, int Day, string Command)`
  - `sealed record HourTally(long Messages, long VoiceMinutes)`
  - `ChannelTally(long Messages, long VoiceMinutes, int VoicePeak)`
  - `CommandTally(long Uses, long Failures)`
  - `sealed record ActivityBatch(Hours, Channels, Commands)` with `IsEmpty`, `GuildIds`, `For(ulong)`
  - `sealed class ActivityCounter` with:
    - `CountMessage(SocketMessage)`
    - `CountMessage(ulong guildId, ulong channelId, DateTimeOffset at)`
    - `CountVoice(ulong guildId, ulong channelId, int humans, long minutes, DateTimeOffset at)`
    - `CountCommand(ulong guildId, string command, bool success, DateTimeOffset at)`
    - `Drain() -> ActivityBatch`
    - `Restore(ActivityBatch)`

- [ ] **Step 1: Write the failing check.** Add section 25:

```csharp
// 25. ActivityCounter: counts, Drain empties, Restore merges a failed flush back.
{
    var t2 = new ActivityCounter();
    var t2At = new DateTimeOffset(2026, 10, 19, 18, 0, 0, TimeSpan.Zero); // 20:00 in Paris
    t2.CountMessage(1, 10, t2At);
    t2.CountMessage(1, 10, t2At);
    t2.CountMessage(1, 11, t2At.AddHours(1));
    t2.CountVoice(1, 20, 3, 1, t2At);
    t2.CountVoice(1, 20, 5, 1, t2At);
    t2.CountVoice(1, 20, 0, 1, t2At);                 // an empty room adds nothing
    t2.CountCommand(1, "report sessions", true, t2At);
    t2.CountCommand(1, "report sessions", false, t2At);
    var t2Batch = t2.Drain();
    if (t2Batch.Hours[new HourKey(1, 20261019, 20)] != new HourTally(2, 8)) Fail("hour tally");
    if (t2Batch.Channels[new ChannelKey(1, 20, 20261019)] != new ChannelTally(0, 8, 5)) Fail("voice minutes or peak");
    if (t2Batch.Commands[new CommandKey(1, 20261019, "report sessions")] != new CommandTally(2, 1)) Fail("command tally");
    if (!t2.Drain().IsEmpty) Fail("Drain left counts behind");
    if (!t2Batch.For(2).IsEmpty || t2Batch.GuildIds.Single() != 1) Fail("batch split by guild");
    t2.CountMessage(1, 10, t2At);
    t2.Restore(t2Batch);
    if (t2.Drain().Channels[new ChannelKey(1, 10, 20261019)] != new ChannelTally(3, 0, 0)) Fail("Restore did not merge");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Run `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: a build error, `CS0246: 'ActivityCounter' could not be found`.

- [ ] **Step 3: Implement.** Create `ProjectSYNCS/Services/Core/ActivityCounter.cs`:

```csharp
using Discord.WebSocket;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

public readonly record struct HourKey(ulong GuildId, int Day, int Hour);
public readonly record struct ChannelKey(ulong GuildId, ulong ChannelId, int Day);
public readonly record struct CommandKey(ulong GuildId, int Day, string Command);

public sealed record HourTally(long Messages, long VoiceMinutes);
public sealed record ChannelTally(long Messages, long VoiceMinutes, int VoicePeak);
public sealed record CommandTally(long Uses, long Failures);

/// <summary>What one flush writes: everything counted since the last drain.</summary>
public sealed record ActivityBatch(
    IReadOnlyDictionary<HourKey, HourTally> Hours,
    IReadOnlyDictionary<ChannelKey, ChannelTally> Channels,
    IReadOnlyDictionary<CommandKey, CommandTally> Commands)
{
    public bool IsEmpty => Hours.Count == 0 && Channels.Count == 0 && Commands.Count == 0;

    public IReadOnlyList<ulong> GuildIds => Hours.Keys.Select(k => k.GuildId)
        .Concat(Channels.Keys.Select(k => k.GuildId))
        .Concat(Commands.Keys.Select(k => k.GuildId))
        .Distinct().ToList();

    public ActivityBatch For(ulong guildId) => new(
        Hours.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Channels.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Commands.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value));
}

/// <summary>
/// Messages, voice presence and commands, counted in memory for /report activity and commands, and
/// written every few minutes by ActivityFlushService. A write per message would be the bot's first,
/// on a Pi with no SQLITE_BUSY handling; this is the one batched counter (root CLAUDE.md).
/// </summary>
/// <remarks>
/// Singleton: it holds state. It owns its lock, and every method is synchronous and cheap, safe to
/// call from a gateway handler. What a crash loses is at most one flush interval (in-memory state
/// resets by design); a clean stop flushes.
/// </remarks>
public sealed class ActivityCounter
{
    private readonly object _lock = new();
    private Dictionary<HourKey, HourTally> _hours = new();
    private Dictionary<ChannelKey, ChannelTally> _channels = new();
    private Dictionary<CommandKey, CommandTally> _commands = new();

    /// <summary>
    /// A message in a guild, by a person: bots, webhooks, system messages and DMs are skipped. A
    /// thread counts under its parent channel. Excluded channels count: these are traffic stats, not
    /// rewards.
    /// </summary>
    public void CountMessage(SocketMessage raw)
    {
        if (raw is not SocketUserMessage message || message.Author.IsBot || message.Author.IsWebhook) return;
        if (message.Channel is not SocketGuildChannel channel) return;
        var channelId = message.Channel is SocketThreadChannel { ParentChannel: { } parent } ? parent.Id : channel.Id;
        CountMessage(channel.Guild.Id, channelId, message.Timestamp);
    }

    public void CountMessage(ulong guildId, ulong channelId, DateTimeOffset at)
    {
        var (day, hour) = ActivityKeys.DayHour(at);
        lock (_lock)
        {
            AddHour(new HourKey(guildId, day, hour), new HourTally(1, 0));
            AddChannel(new ChannelKey(guildId, channelId, day), new ChannelTally(1, 0, 0));
        }
    }

    /// <summary>One sweep of a voice room: <paramref name="humans"/> people present for <paramref name="minutes"/>.</summary>
    public void CountVoice(ulong guildId, ulong channelId, int humans, long minutes, DateTimeOffset at)
    {
        if (humans <= 0 || minutes <= 0) return;
        var (day, hour) = ActivityKeys.DayHour(at);
        lock (_lock)
        {
            AddHour(new HourKey(guildId, day, hour), new HourTally(0, humans * minutes));
            AddChannel(new ChannelKey(guildId, channelId, day), new ChannelTally(0, humans * minutes, humans));
        }
    }

    public void CountCommand(ulong guildId, string command, bool success, DateTimeOffset at)
    {
        var day = ActivityKeys.DayHour(at).Day;
        lock (_lock) AddCommand(new CommandKey(guildId, day, command), new CommandTally(1, success ? 0 : 1));
    }

    /// <summary>Takes everything counted so far and starts afresh.</summary>
    public ActivityBatch Drain()
    {
        lock (_lock)
        {
            var batch = new ActivityBatch(_hours, _channels, _commands);
            _hours = new();
            _channels = new();
            _commands = new();
            return batch;
        }
    }

    /// <summary>Puts a batch that could not be written back, to be written with the next one.</summary>
    public void Restore(ActivityBatch batch)
    {
        lock (_lock)
        {
            foreach (var (key, tally) in batch.Hours) AddHour(key, tally);
            foreach (var (key, tally) in batch.Channels) AddChannel(key, tally);
            foreach (var (key, tally) in batch.Commands) AddCommand(key, tally);
        }
    }

    private void AddHour(HourKey key, HourTally add) =>
        _hours[key] = _hours.TryGetValue(key, out var t)
            ? new HourTally(t.Messages + add.Messages, t.VoiceMinutes + add.VoiceMinutes)
            : add;

    private void AddChannel(ChannelKey key, ChannelTally add) =>
        _channels[key] = _channels.TryGetValue(key, out var t)
            ? new ChannelTally(t.Messages + add.Messages, t.VoiceMinutes + add.VoiceMinutes, Math.Max(t.VoicePeak, add.VoicePeak))
            : add;

    private void AddCommand(CommandKey key, CommandTally add) =>
        _commands[key] = _commands.TryGetValue(key, out var t)
            ? new CommandTally(t.Uses + add.Uses, t.Failures + add.Failures)
            : add;
}
```

- [ ] **Step 4: Run the checks.** Expected: `ALL CHECKS PASSED`.

- [ ] **Step 5: Commit** (only if the owner asked): `git add ProjectSYNCS/Services/Core/ActivityCounter.cs tests/ProjectSYNCS.Checks/Program.cs && git commit -m "In-memory activity counter"`.

---

### Task 3: Storage, migration and flush loop

**Files:**
- Create: `ProjectSYNCS/Models/Core/ActivityHourStat.cs`, `ProjectSYNCS/Models/Core/ChannelDailyStat.cs`, `ProjectSYNCS/Models/Core/CommandDailyStat.cs`, `ProjectSYNCS/Services/Core/ActivityFlushService.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs` (DbSets after `MemberFlowDailyStats`; config after the `MemberFlowDailyStat` entity block), `ProjectSYNCS/Program.cs`
- Generated: `ProjectSYNCS/Migrations/<timestamp>_ActivityStats.cs` (+ Designer, snapshot)

**Interfaces:**
- Consumes: `ActivityCounter.Drain/Restore`, `ActivityBatch.For/GuildIds` (Task 2).
- Produces:
  - DbSets `ActivityHourStats`, `ChannelDailyStats`, `CommandDailyStats`;
  - `ActivityFlushService.FlushAsync() -> Task`, public, so the owner tools could call it later.

- [ ] **Step 1: Create the models.** `ProjectSYNCS/Models/Core/ActivityHourStat.cs`:

```csharp
namespace ProjectSYNCS.Models;

// Messages and voice person-minutes for one guild, one Paris day and one hour (0–23): the peak hours
// of /report activity. Written by ActivityFlushService from ActivityCounter. Buckets only, no
// totals: nothing needs an all-time figure older than the buckets (root CLAUDE.md, the
// MemberFlowDailyStat precedent). Counted from 7.6.0 on. One row per (guild, day, hour), unique.
public class ActivityHourStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public int Hour { get; set; }
    public long Messages { get; set; }
    // Minutes × people present (muted included, AFK channel excluded): presence, not XP-eligible time.
    public long VoiceMinutes { get; set; }
}
```

`ProjectSYNCS/Models/Core/ChannelDailyStat.cs`:

```csharp
namespace ProjectSYNCS.Models;

// One channel's day: messages written there (a thread counts under its parent; a voice room's text
// chat under the room) and, for a voice room, person-minutes and the most people at once. For the
// channel and voice-room lists of /report activity. Excluded channels are counted too (shown as
// such). Buckets only, from 7.6.0 on. One row per (guild, channel, day), unique.
public class ChannelDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int Day { get; set; }
    public long Messages { get; set; }
    public long VoiceMinutes { get; set; }
    public int VoicePeak { get; set; }
}
```

`ProjectSYNCS/Models/Core/CommandDailyStat.cs`:

```csharp
namespace ProjectSYNCS.Models;

// How often each command was used in one guild on one Paris day, for /report commands. Command is
// CommandPath's key (« report sessions », « ;work »): a stored string, so append-only. A renamed
// command keeps its old rows under its old name. Failures are uses whose handler reported an error.
// Buckets only, from 7.6.0 on; DMs are GuildId 0. One row per (guild, day, command), unique.
public class CommandDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public string Command { get; set; } = string.Empty;
    public long Uses { get; set; }
    public long Failures { get; set; }
}
```

- [ ] **Step 2: Register them in `AppDbContext`.** After `public DbSet<MemberFlowDailyStat> MemberFlowDailyStats => Set<MemberFlowDailyStat>();` add:

```csharp
    public DbSet<ActivityHourStat> ActivityHourStats => Set<ActivityHourStat>();
    public DbSet<ChannelDailyStat> ChannelDailyStats => Set<ChannelDailyStat>();
    public DbSet<CommandDailyStat> CommandDailyStats => Set<CommandDailyStat>();
```

Right after the `modelBuilder.Entity<MemberFlowDailyStat>(e => { … });` block add:

```csharp
        modelBuilder.Entity<ActivityHourStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.HasIndex(x => new { x.GuildId, x.Day, x.Hour }).IsUnique();
        });

        modelBuilder.Entity<ChannelDailyStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.Property(x => x.ChannelId).HasConversion<long>();
            e.HasIndex(x => new { x.GuildId, x.ChannelId, x.Day }).IsUnique();
            e.HasIndex(x => new { x.GuildId, x.Day });
        });

        modelBuilder.Entity<CommandDailyStat>(e =>
        {
            e.Property(x => x.GuildId).HasConversion<long>();
            e.HasIndex(x => new { x.GuildId, x.Day, x.Command }).IsUnique();
        });
```

- [ ] **Step 3: Create the flush loop.** `ProjectSYNCS/Services/Core/ActivityFlushService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Writes ActivityCounter's counts to ActivityHourStat, ChannelDailyStat and CommandDailyStat. Its own
// 5-minute loop (root CLAUDE.md: every loop keeps its own interval). One scope per guild, each in
// its own try: a guild whose write fails gets its counts put back (ActivityCounter.Restore) for the
// next pass, and the others are written anyway. StopAsync flushes once more, so a clean restart
// loses nothing.
public sealed class ActivityFlushService : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(5);

    private readonly ActivityCounter _counter;
    private readonly IServiceProvider _services;
    private readonly ILogger<ActivityFlushService> _logger;

    public ActivityFlushService(ActivityCounter counter, IServiceProvider services, ILogger<ActivityFlushService> logger)
    {
        _counter = counter;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(FlushInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            await FlushAsync();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync();
    }

    public async Task FlushAsync()
    {
        var batch = _counter.Drain();
        if (batch.IsEmpty) return;
        foreach (var guildId in batch.GuildIds)
        {
            var part = batch.For(guildId);
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await WriteAsync(db, guildId, part);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _counter.Restore(part);
                _logger.LogWarning(ex, "Activity counts for guild {GuildId} not saved; kept for the next flush.", guildId);
            }
        }
    }

    // Find-or-create every row the batch touches, in three reads (one per table, by the batch's days).
    private static async Task WriteAsync(AppDbContext db, ulong guildId, ActivityBatch part)
    {
        var days = part.Hours.Keys.Select(k => k.Day)
            .Concat(part.Channels.Keys.Select(k => k.Day))
            .Concat(part.Commands.Keys.Select(k => k.Day))
            .Distinct().ToList();

        var hours = (await db.ActivityHourStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.Day, x.Hour));
        foreach (var (key, t) in part.Hours)
        {
            if (!hours.TryGetValue((key.Day, key.Hour), out var row))
            {
                row = new ActivityHourStat { GuildId = guildId, Day = key.Day, Hour = key.Hour };
                db.ActivityHourStats.Add(row);
                hours[(key.Day, key.Hour)] = row;
            }
            row.Messages += t.Messages;
            row.VoiceMinutes += t.VoiceMinutes;
        }

        var channels = (await db.ChannelDailyStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.ChannelId, x.Day));
        foreach (var (key, t) in part.Channels)
        {
            if (!channels.TryGetValue((key.ChannelId, key.Day), out var row))
            {
                row = new ChannelDailyStat { GuildId = guildId, ChannelId = key.ChannelId, Day = key.Day };
                db.ChannelDailyStats.Add(row);
                channels[(key.ChannelId, key.Day)] = row;
            }
            row.Messages += t.Messages;
            row.VoiceMinutes += t.VoiceMinutes;
            row.VoicePeak = Math.Max(row.VoicePeak, t.VoicePeak);
        }

        var commands = (await db.CommandDailyStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.Day, x.Command));
        foreach (var (key, t) in part.Commands)
        {
            if (!commands.TryGetValue((key.Day, key.Command), out var row))
            {
                row = new CommandDailyStat { GuildId = guildId, Day = key.Day, Command = key.Command };
                db.CommandDailyStats.Add(row);
                commands[(key.Day, key.Command)] = row;
            }
            row.Uses += t.Uses;
            row.Failures += t.Failures;
        }
    }
}
```

- [ ] **Step 4: Register both in `Program.cs`.** Next to `services.AddSingleton<MemberFlowTracker>();` add:

```csharp
        // Counts messages, voice presence and commands in memory; ActivityFlushService writes them.
        services.AddSingleton<ActivityCounter>();
        services.AddHostedService<ActivityFlushService>();
```

- [ ] **Step 5: Build.** Run `dotnet build ProjectSYNCS/ProjectSYNCS.csproj -warnaserror`. Expected: `0 Warning(s), 0 Error(s)`. A French SDK prints `0 Avertissement(s), 0 Erreur(s)`.

- [ ] **Step 6: Generate the migration.** Run `cd ProjectSYNCS && dotnet ef migrations add ActivityStats && cd ..`. Open the generated `Migrations/*_ActivityStats.cs`. Expected: `CreateTable` for exactly `ActivityHourStats`, `ChannelDailyStats` and `CommandDailyStats`, plus their indexes, and nothing else. If it touches any other table, the model has drifted: stop and investigate.

- [ ] **Step 7: Build and run the checks again.** Expected: build clean, `ALL CHECKS PASSED`.

- [ ] **Step 8: Commit** (only if the owner asked): `git add ProjectSYNCS/Models/Core ProjectSYNCS/Services/Core/ActivityFlushService.cs ProjectSYNCS/Data/AppDbContext.cs ProjectSYNCS/Program.cs ProjectSYNCS/Migrations && git commit -m "Activity tables, migration and 5-minute flush loop"`.

---

### Task 4: The four hooks

**Files:**
- Modify: `ProjectSYNCS/Services/Core/BotService.cs`, `ProjectSYNCS/Services/Plynling/PlynlingShortcutService.cs`, `ProjectSYNCS/Services/Xp/VoiceXpService.cs`

**Interfaces:**
- Consumes: `ActivityCounter.CountMessage(SocketMessage)`, `CountVoice`, `CountCommand` (Task 2); `CommandPath.Of`, `CommandPath.Shortcut` (Task 1).
- Produces: live counts (no new API).

- [ ] **Step 1: Hook messages and slash commands in `BotService`.**
  - Add a field `private readonly ActivityCounter _activity;`, a constructor parameter `ActivityCounter activity` (last), and the assignment `_activity = activity;`.
  - If missing, add `using ProjectSYNCS.Helpers;` at the top.

  In `HandleMessageAsync`, right after `if (await _shortcuts.HandleMessageAsync(rawMessage)) return;` insert:

```csharp
        // Every message a person writes in a guild, for /report activity (in memory: costs nothing here).
        // After the shortcut return (a shortcut is a command, counted as one) and before every
        // handler that may end the fan-out early.
        _activity.CountMessage(rawMessage);
```

In `StartAsync`, right after `_interactions.Log += LogAsync;` insert:

```csharp
        // Handlers run RunMode.Async: success or failure only arrives through this event.
        _interactions.SlashCommandExecuted += OnSlashCommandExecutedAsync;
```

Add this method next to `HandleInteractionAsync`:

```csharp
    // Every slash command, for /report commands (in memory, never slows a command). Buttons and
    // modals are not counted: they belong to a command already counted. DMs count under guild 0.
    private Task OnSlashCommandExecutedAsync(SlashCommandInfo? command, IInteractionContext context, IResult result)
    {
        if (command is not null)
            _activity.CountCommand(context.Guild?.Id ?? 0, CommandPath.Of(command), result.IsSuccess, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
```

- [ ] **Step 2: Hook the typed shortcuts in `PlynlingShortcutService`.**
  - Add a field `private readonly ActivityCounter _activity;`, a constructor parameter `ActivityCounter activity` (last), and the assignment.
  - Replace the `try`/`catch` around `RunAsync` in `HandleMessageAsync` with:

```csharp
        var success = true;
        try
        {
            await RunAsync(message, channel.Guild, shortcut);
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogWarning(ex, "Shortcut {Kind} from {UserId} failed.", shortcut.Kind, message.Author.Id);
        }
        // Counted like the slash command it stands for (/report commands), under its own « ; » key.
        _activity.CountCommand(channel.Guild.Id, CommandPath.Shortcut(shortcut.Kind), success, message.Timestamp);
        return true;
```

  This replaces the original `return true;` at the end of that method. A repeat dropped by `_repeat` returns earlier and is not counted, which is intended.

- [ ] **Step 3: Hook voice presence in `VoiceXpService`.**
  - Add a field `private readonly ActivityCounter _activity;`, a constructor parameter `ActivityCounter activity` (last), and the assignment.
  - In `SweepAsync`'s per-channel loop, replace this block:

```csharp
                try
                {
                    var humans = channel.ConnectedUsers.Count(u => !u.IsBot);
                    await _spectator.ObserveAsync(channel, active.Count, humans, CheckInterval);
                }
```

with:

```csharp
                var humans = channel.ConnectedUsers.Count(u => !u.IsBot);
                // Presence per room and per hour for /report activity: everyone connected, muted
                // included (the AFK channel is skipped above). In memory, cannot throw.
                _activity.CountVoice(guild.Id, channel.Id, humans, MinutesPerTick, DateTimeOffset.UtcNow);

                try
                {
                    await _spectator.ObserveAsync(channel, active.Count, humans, CheckInterval);
                }
```

  The `catch` that follows stays as it is.

- [ ] **Step 4: Build and run the checks.** Expected: build clean, `ALL CHECKS PASSED`.

- [ ] **Step 5: Commit** (only if the owner asked): `git add ProjectSYNCS/Services && git commit -m "Count messages, voice presence and commands"`.

---

### Task 5: Activity and command arithmetic

**Files:**
- Create: `ProjectSYNCS/Helpers/Core/ActivityReport.cs`, `ProjectSYNCS/Helpers/Core/CommandReport.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 26)

**Interfaces:**
- Consumes: the models from Task 3; `AppTime.FromDayKey`.
- Produces:
  - `ChannelInfo(ulong Id, bool IsVoice, bool Excluded)`
  - `ChannelLine(ulong ChannelId, long Value, int Peak, bool Excluded)`
  - `ActivityReportData(...)` (fields below)
  - `ActivityReport.Compute(IReadOnlyList<ActivityHourStat>, IReadOnlyList<ChannelDailyStat>, IReadOnlyList<ChannelInfo>) -> ActivityReportData`
  - `CommandLine(string Command, long Uses, long Failures)`
  - `CommandReportData(...)`
  - `CommandReport.Compute(IReadOnlyList<CommandDailyStat>, IReadOnlyCollection<string> registered) -> CommandReportData`

- [ ] **Step 1: Write the failing check.** Add section 26:

```csharp
// 26. Activity and command arithmetic.
{
    ProjectSYNCS.Models.ActivityHourStat T5Hour(int day, int hour, long messages, long voice) =>
        new() { GuildId = 1, Day = day, Hour = hour, Messages = messages, VoiceMinutes = voice };
    ProjectSYNCS.Models.ChannelDailyStat T5Chan(ulong id, long messages, long voice, int peak) =>
        new() { GuildId = 1, ChannelId = id, Day = 20261019, Messages = messages, VoiceMinutes = voice, VoicePeak = peak };
    var t5Guild = new[] { new ChannelInfo(10, false, false), new ChannelInfo(11, false, true), new ChannelInfo(12, false, false),
                          new ChannelInfo(20, true, false), new ChannelInfo(21, true, false) };
    var t5 = ActivityReport.Compute(
        new[] { T5Hour(20261019, 21, 50, 30), T5Hour(20261019, 14, 5, 0), T5Hour(20261018, 21, 10, 0) },  // 19 Oct 2026 is a Monday
        new[] { T5Chan(10, 40, 0, 0), T5Chan(11, 15, 0, 0), T5Chan(20, 0, 30, 3), T5Chan(99, 10, 0, 0) },  // 99: deleted channel
        t5Guild);
    if (t5.Messages != 65 || t5.VoiceMinutes != 30) Fail($"activity totals {t5.Messages}/{t5.VoiceMinutes}");
    if (t5.MessagesByHour.Count != 24 || t5.MessagesByHour[21] != 60 || t5.BusiestHours[0] != 21) Fail("hour profile");
    if (t5.BusiestWeekday != DayOfWeek.Monday) Fail($"busiest weekday {t5.BusiestWeekday}");
    if (t5.TopText[0].ChannelId != 10 || !t5.TopText.Single(l => l.ChannelId == 11).Excluded) Fail("text ranking or excluded mark");
    if (!t5.TopText.Any(l => l.ChannelId == 99)) Fail("a deleted channel with counts must still be listed");
    if (t5.SilentText.Single().ChannelId != 12) Fail("silent channels");
    if (t5.TopVoice.Single().Peak != 3 || t5.EmptyVoice.Single().ChannelId != 21) Fail("voice rooms");
    var t5Empty = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
    if (t5Empty.Messages != 0 || t5Empty.BusiestWeekday is not null || t5Empty.SilentText.Count != 0) Fail("empty activity (guild unavailable)");

    ProjectSYNCS.Models.CommandDailyStat T5Cmd(string command, long uses, long failures) =>
        new() { GuildId = 1, Day = 20261019, Command = command, Uses = uses, Failures = failures };
    var t5c = CommandReport.Compute(
        new[] { T5Cmd("report sessions", 3, 1), T5Cmd("report sessions", 2, 0), T5Cmd("help", 9, 0), T5Cmd(";work", 4, 0), T5Cmd("old name", 1, 0) },
        new[] { "help", "report sessions", "report polls", "plynling feed" });
    if (t5c.Uses != 19 || t5c.Failures != 1) Fail("command totals");
    if (t5c.Top[0].Command != "help" || t5c.Top.Single(l => l.Command == "report sessions").Uses != 5) Fail("command ranking");
    if (t5c.Shortcuts.Single().Command != ";work" || t5c.Top.Any(l => l.Command.StartsWith(';'))) Fail("shortcuts kept apart");
    if (!t5c.NeverUsed.SequenceEqual(new[] { "plynling feed", "report polls" })) Fail($"never used: {string.Join(",", t5c.NeverUsed)}");
    if (!t5c.Top.Any(l => l.Command == "old name")) Fail("a renamed command keeps its old rows");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Expected: build error, `ActivityReport` / `ChannelInfo` not found.

- [ ] **Step 3: Implement `ProjectSYNCS/Helpers/Core/ActivityReport.cs`:**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>A channel of the guild as the report sees it: plain data, so the arithmetic stays pure.</summary>
public sealed record ChannelInfo(ulong Id, bool IsVoice, bool Excluded);

/// <summary>One channel in a list: messages (text) or person-minutes (voice), and the voice peak.</summary>
public sealed record ChannelLine(ulong ChannelId, long Value, int Peak, bool Excluded);

/// <summary>What /report activity shows. Built by <see cref="ActivityReport.Compute"/>.</summary>
public sealed record ActivityReportData(
    long Messages,
    long VoiceMinutes,
    IReadOnlyList<long> MessagesByHour,
    IReadOnlyList<long> VoiceByHour,
    IReadOnlyList<int> BusiestHours,
    DayOfWeek? BusiestWeekday,
    IReadOnlyList<ChannelLine> TopText,
    IReadOnlyList<ChannelLine> SilentText,
    IReadOnlyList<ChannelLine> TopVoice,
    IReadOnlyList<ChannelLine> EmptyVoice,
    IReadOnlyDictionary<ulong, long> MessagesByChannel);

/// <summary>
/// When and where the server talks, over one window, from ActivityHourStat and ChannelDailyStat rows
/// (filtered on Day in SQL by the caller) and a snapshot of the guild's channels. A channel with
/// counts that is no longer in the guild is still ranked; channels the snapshot lists with nothing
/// counted are « silencieux » (text) or « inutilisés » (voice). Pure, so the checks can feed it cases.
/// </summary>
public static class ActivityReport
{
    public const int TopChannels = 8;

    public static ActivityReportData Compute(
        IReadOnlyList<ActivityHourStat> hours, IReadOnlyList<ChannelDailyStat> channels, IReadOnlyList<ChannelInfo> guildChannels)
    {
        var byHour = new long[24];
        var voiceByHour = new long[24];
        foreach (var h in hours)
        {
            byHour[h.Hour] += h.Messages;
            voiceByHour[h.Hour] += h.VoiceMinutes;
        }
        var busiest = Enumerable.Range(0, 24).Where(h => byHour[h] > 0)
            .OrderByDescending(h => byHour[h]).ThenBy(h => h).Take(3).ToList();
        var weekday = hours.Where(h => h.Messages > 0)
            .GroupBy(h => AppTime.FromDayKey(h.Day).DayOfWeek)
            .Select(g => (Day: g.Key, Messages: g.Sum(h => h.Messages)))
            .OrderByDescending(x => x.Messages)
            .Select(x => (DayOfWeek?)x.Day)
            .FirstOrDefault();

        var info = guildChannels.ToDictionary(c => c.Id);
        bool Excluded(ulong id) => info.TryGetValue(id, out var c) && c.Excluded;
        var perChannel = channels.GroupBy(c => c.ChannelId)
            .Select(g => (Id: g.Key, Messages: g.Sum(c => c.Messages), Voice: g.Sum(c => c.VoiceMinutes), Peak: g.Max(c => c.VoicePeak)))
            .ToList();

        var topText = perChannel.Where(c => c.Messages > 0)
            .OrderByDescending(c => c.Messages).Take(TopChannels)
            .Select(c => new ChannelLine(c.Id, c.Messages, 0, Excluded(c.Id))).ToList();
        var written = perChannel.Where(c => c.Messages > 0).Select(c => c.Id).ToHashSet();
        var silent = guildChannels.Where(c => !c.IsVoice && !written.Contains(c.Id))
            .Select(c => new ChannelLine(c.Id, 0, 0, c.Excluded)).ToList();

        var topVoice = perChannel.Where(c => c.Voice > 0)
            .OrderByDescending(c => c.Voice).Take(TopChannels)
            .Select(c => new ChannelLine(c.Id, c.Voice, c.Peak, Excluded(c.Id))).ToList();
        var occupied = perChannel.Where(c => c.Voice > 0).Select(c => c.Id).ToHashSet();
        var emptyVoice = guildChannels.Where(c => c.IsVoice && !occupied.Contains(c.Id))
            .Select(c => new ChannelLine(c.Id, 0, 0, c.Excluded)).ToList();

        return new ActivityReportData(
            byHour.Sum(), voiceByHour.Sum(), byHour, voiceByHour, busiest, weekday,
            topText, silent, topVoice, emptyVoice,
            perChannel.ToDictionary(c => c.Id, c => c.Messages));
    }
}
```

- [ ] **Step 4: Implement `ProjectSYNCS/Helpers/Core/CommandReport.cs`:**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public sealed record CommandLine(string Command, long Uses, long Failures);

/// <summary>What /report commands shows. Built by <see cref="CommandReport.Compute"/>.</summary>
public sealed record CommandReportData(
    long Uses,
    long Failures,
    IReadOnlyList<CommandLine> Top,
    IReadOnlyList<CommandLine> Shortcuts,
    IReadOnlyList<string> NeverUsed,
    IReadOnlyDictionary<string, long> UsesByCommand);

/// <summary>
/// Her commands over one window, from CommandDailyStat rows and the paths registered right now
/// (CommandPath.Of over InteractionService.SlashCommands). A key no longer registered (a renamed
/// command) still ranks under its old name. Shortcuts (« ; ») are listed apart. Pure.
/// </summary>
public static class CommandReport
{
    public const int TopCommands = 15;

    public static CommandReportData Compute(IReadOnlyList<CommandDailyStat> rows, IReadOnlyCollection<string> registered)
    {
        var per = rows.GroupBy(r => r.Command)
            .Select(g => new CommandLine(g.Key, g.Sum(r => r.Uses), g.Sum(r => r.Failures)))
            .ToList();
        var slash = per.Where(c => !c.Command.StartsWith(';'))
            .OrderByDescending(c => c.Uses).ThenBy(c => c.Command, StringComparer.Ordinal).ToList();
        var shortcuts = per.Where(c => c.Command.StartsWith(';')).OrderByDescending(c => c.Uses).ToList();
        var used = per.Where(c => c.Uses > 0).Select(c => c.Command).ToHashSet();
        var never = registered.Where(c => !used.Contains(c)).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
        return new CommandReportData(
            per.Sum(c => c.Uses), per.Sum(c => c.Failures),
            slash.Take(TopCommands).ToList(), shortcuts, never,
            per.ToDictionary(c => c.Command, c => c.Uses));
    }
}
```

- [ ] **Step 5: Run the checks.** Expected: `ALL CHECKS PASSED`.

- [ ] **Step 6: Commit** (only if the owner asked): `git add ProjectSYNCS/Helpers/Core tests/ProjectSYNCS.Checks/Program.cs && git commit -m "Activity and command report arithmetic"`.

---

### Task 6: `/report activity` and `/report commands`, and the digest list

**Files:**
- Create: `ProjectSYNCS/Services/Core/ActivityReportService.cs`
- Modify: `ProjectSYNCS/Services/Admin/ReportService.cs`, `ProjectSYNCS/Commands/Admin/ReportCards.cs`, `ProjectSYNCS/Commands/Admin/ReportModule.cs`, `ProjectSYNCS/Services/Admin/MonthlyDigestService.cs`, `ProjectSYNCS/Program.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 27)

**Interfaces:**
- Consumes:
  - Task 5's helpers;
  - batch 1's `Compared<T>`, `ReportWindow` (`FromDay`, `ToDay`, `Previous`), and these private helpers inside `ReportCards`: `Start`, `Footer`, `Cap`, `Plural`, `Duration`, `Weekday(int)`, `MaxFieldLength`;
  - `XpTracker.HardcodedExcludedChannels` and `GuildConfigService.GetAsync(guildId).ExcludedChannels`.
- Produces:
  - `ReportKind.Activity`, `ReportKind.Commands` (appended);
  - `ReportKinds.Digest` (`IReadOnlyList<ReportKind>`);
  - `ActivityReportService.GetActivityAsync(ulong, ReportWindow, IReadOnlyList<ChannelInfo>) -> Task<Compared<ActivityReportData>>`;
  - `ActivityReportService.GetCommandsAsync(ulong, ReportWindow, IReadOnlyCollection<string>) -> Task<Compared<CommandReportData>>`;
  - `ReportCards.BuildActivity(Compared<ActivityReportData>, ReportWindow, bool) -> Embed`;
  - `ReportCards.BuildCommands(Compared<CommandReportData>, ReportWindow, bool) -> Embed`;
  - `ReportCards.Fit(IReadOnlyList<string> items, string separator) -> string` (private, reused by Tasks 9 and 10).

- [ ] **Step 1: Write the failing check.** Add section 27, with worst-case data:

```csharp
// 27. /report activity and commands: every card measured at its worst.
{
    void T6Fits(string what, Discord.Embed e, bool names)
    {
        if (e.Length > 6000) Fail($"{what} embed is {e.Length} characters");
        if (e.Fields.Length > 25 || e.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"{what} has an empty, oversized or 26th field");
        if (!names && (e.Description + string.Concat(e.Fields.Select(f => f.Value))).Contains("<@")) Fail($"{what} names someone");
    }
    const ulong t6Big = 1_234_567_890_123_456_789;
    var t6Lines = Enumerable.Range(0, ActivityReport.TopChannels).Select(i => new ChannelLine(t6Big + (ulong)i, 99_999_999, 99, i % 2 == 0)).ToList();
    var t6Many = Enumerable.Range(0, 60).Select(i => new ChannelLine(t6Big + 100 + (ulong)i, 0, 0, true)).ToList();
    var t6Act = new ActivityReportData(99_999_999, 99_999_999, Enumerable.Repeat(9_999L, 24).ToList(), Enumerable.Repeat(9_999L, 24).ToList(),
        new[] { 21, 22, 23 }, DayOfWeek.Wednesday, t6Lines, t6Many, t6Lines, t6Many, t6Lines.ToDictionary(l => l.ChannelId, l => 5L));
    var t6Cmd = new CommandReportData(9_999_999, 99_999,
        Enumerable.Range(0, CommandReport.TopCommands).Select(i => new CommandLine($"plynling some-long-subcommand-{i}", 999_999, 999)).ToList(),
        new[] { new CommandLine(";pl", 9999, 0), new CommandLine(";work", 9999, 0), new CommandLine(";pl visit", 9999, 0) },
        Enumerable.Range(0, 150).Select(i => $"config some-group-name sub-{i}").ToList(),
        new Dictionary<string, long>());
    var t6Month = ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow);
    foreach (var t6Names in new[] { true, false })
    {
        T6Fits("activity", ProjectSYNCS.Commands.ReportCards.BuildActivity(new(t6Act, t6Act), t6Month, t6Names), t6Names);
        T6Fits("commands", ProjectSYNCS.Commands.ReportCards.BuildCommands(new(t6Cmd, t6Cmd), t6Month, t6Names), t6Names);
    }
    var t6Quiet = ActivityReport.Compute(Array.Empty<ProjectSYNCS.Models.ActivityHourStat>(), Array.Empty<ProjectSYNCS.Models.ChannelDailyStat>(), Array.Empty<ChannelInfo>());
    T6Fits("empty activity", ProjectSYNCS.Commands.ReportCards.BuildActivity(new(t6Quiet, null), ReportWindow.From(StatsPeriod.AllTime, DateTimeOffset.UtcNow), true), true);
    if (!ReportKinds.Digest.SequenceEqual(new[] { ReportKind.Sessions, ReportKind.Polls, ReportKind.Community, ReportKind.Syncs, ReportKind.Activity, ReportKind.Commands }))
        Fail("digest list");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Expected: build error, `ReportCards.BuildActivity` not found.

- [ ] **Step 3: Create `ProjectSYNCS/Services/Core/ActivityReportService.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the buckets behind /report activity and /report commands; the arithmetic is in
/// <see cref="ActivityReport"/> and <see cref="CommandReport"/>. Every table has an int Day, so windows filter in SQL.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class ActivityReportService
{
    private readonly AppDbContext _db;

    public ActivityReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<ActivityReportData>> GetActivityAsync(ulong guildId, ReportWindow window, IReadOnlyList<ChannelInfo> channels)
    {
        async Task<ActivityReportData> ComputeAsync(ReportWindow w)
        {
            var hours = await _db.ActivityHourStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
            var perChannel = await _db.ChannelDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
            return ActivityReport.Compute(hours, perChannel, channels);
        }
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }

    public async Task<Compared<CommandReportData>> GetCommandsAsync(ulong guildId, ReportWindow window, IReadOnlyCollection<string> registered)
    {
        async Task<CommandReportData> ComputeAsync(ReportWindow w) => CommandReport.Compute(
            await _db.CommandDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync(),
            registered);
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }
}
```

  Register it in `Program.cs` next to `services.AddTransient<ReportService>();` with `services.AddTransient<ActivityReportService>();`.

- [ ] **Step 4: Extend `ReportService`** (`ProjectSYNCS/Services/Admin/ReportService.cs`).
  - In `enum ReportKind`, after `Syncs,` append `Activity,` and `Commands,`. Append only: these names are in custom-ids.
  - Add below the enum:

```csharp
/// <summary>Which reports the monthly digest sends, in order. The others stay on demand.</summary>
public static class ReportKinds
{
    public static readonly IReadOnlyList<ReportKind> Digest = new[]
    {
        ReportKind.Sessions, ReportKind.Polls, ReportKind.Community, ReportKind.Syncs, ReportKind.Activity, ReportKind.Commands,
    };
}
```

  Add these usings: `using Discord.Interactions;`, `using ProjectSYNCS.Helpers;` (if absent). Then change the constructor and add the snapshot helpers:

```csharp
    private readonly SchedulingReportService _scheduling;
    private readonly CommunityReportService _community;
    private readonly SyncsReportService _syncs;
    private readonly ActivityReportService _activity;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly GuildConfigService _config;

    public ReportService(SchedulingReportService scheduling, CommunityReportService community, SyncsReportService syncs,
        ActivityReportService activity, DiscordSocketClient client, InteractionService interactions, GuildConfigService config)
    {
        _scheduling = scheduling;
        _community = community;
        _syncs = syncs;
        _activity = activity;
        _client = client;
        _interactions = interactions;
        _config = config;
    }

    // The guild's text and voice channels as plain data, with the excluded ones marked (the hardcoded
    // floor plus /config's additions). Threads, voice-room chats and the AFK room are left out. Empty
    // when the guild is unavailable (gateway down): the report renders without the channel lists.
    private async Task<IReadOnlyList<ChannelInfo>> ChannelsAsync(ulong guildId)
    {
        if (_client.GetGuild(guildId) is not { } guild) return Array.Empty<ChannelInfo>();
        var added = (await _config.GetAsync(guildId)).ExcludedChannels;
        bool Excluded(ulong id) => XpTracker.HardcodedExcludedChannels.Contains(id) || added.Contains(id);
        var text = guild.TextChannels
            .Where(c => c is not SocketVoiceChannel && c is not SocketThreadChannel)
            .Select(c => new ChannelInfo(c.Id, false, Excluded(c.Id)));
        var voice = guild.VoiceChannels
            .Where(c => c.Id != guild.AFKChannel?.Id)
            .Select(c => new ChannelInfo(c.Id, true, Excluded(c.Id)));
        return text.Concat(voice).ToList();
    }

    // Every slash path registered now, /pl folded into /plynling (CommandPath).
    private IReadOnlyCollection<string> RegisteredCommands() =>
        _interactions.SlashCommands.Select(CommandPath.Of).ToHashSet();
```

  In `BuildAsync`'s switch, turn the default arm `_ => ReportCards.BuildSyncs(...)` into `ReportKind.Syncs => ReportCards.BuildSyncs(...)`, then add:

```csharp
            ReportKind.Activity => ReportCards.BuildActivity(
                await _activity.GetActivityAsync(guildId, window, await ChannelsAsync(guildId)), window, names),
            ReportKind.Commands => ReportCards.BuildCommands(
                await _activity.GetCommandsAsync(guildId, window, RegisteredCommands()), window, names),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No windowed report for this kind."),
```

- [ ] **Step 5: Add the cards** to `ProjectSYNCS/Commands/Admin/ReportCards.cs`.
  - Add `using System.Text;` at the top.
  - Insert before the `// ---- Shared ----` line:

```csharp
    // ---- /report activity ---------------------------------------------------------------------

    public static Embed BuildActivity(Compared<ActivityReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("📈 L'activité", w);
        if (d.Messages == 0 && d.VoiceMinutes == 0)
            return embed.WithDescription("Rien de compté sur cette période (comptage depuis la 7.6.0).").WithFooter(Footer(w)).Build();

        embed.WithDescription(
            $"**{LevelCardUi.Xp(d.Messages)}** messages{TextChart.Trend(d.Messages, p?.Messages)}" +
            $" · **{Duration(d.VoiceMinutes)}** de vocal cumulé{TextChart.Trend(d.VoiceMinutes, p?.VoiceMinutes)}");

        var peak = $"Messages `0h {TextChart.Sparkline(d.MessagesByHour)} 23h`\n" +
                   $"Vocal       `0h {TextChart.Sparkline(d.VoiceByHour)} 23h`";
        if (d.BusiestHours.Count > 0) peak += $"\nHeures de pointe : {string.Join(" · ", d.BusiestHours.Select(h => $"{h} h"))}";
        if (d.BusiestWeekday is { } day) peak += $"\nJour le plus bavard : {Weekday((int)day)}";
        embed.AddField("Heures de pointe", peak);

        embed.AddField("Salons écrits", d.TopText.Count == 0 ? "—" : Cap(string.Join('\n', d.TopText.Select(l =>
            $"{Mark(l)}<#{l.ChannelId}> — {LevelCardUi.Xp(l.Value)}{TextChart.Trend(l.Value, p?.MessagesByChannel.GetValueOrDefault(l.ChannelId))}"))));
        if (d.SilentText.Count > 0)
            embed.AddField($"Salons silencieux ({d.SilentText.Count})", Fit(d.SilentText.Select(l => $"{Mark(l)}<#{l.ChannelId}>").ToList(), " · "));

        embed.AddField("Salons vocaux", d.TopVoice.Count == 0 ? "Personne en vocal." : Cap(string.Join('\n', d.TopVoice.Select(l =>
            $"{Mark(l)}<#{l.ChannelId}> — {Duration(l.Value)} · jusqu'à {l.Peak} {Plural(l.Peak, "personne", "personnes")}"))));
        if (d.EmptyVoice.Count > 0)
            embed.AddField($"Vocaux inutilisés ({d.EmptyVoice.Count})", Fit(d.EmptyVoice.Select(l => $"{Mark(l)}<#{l.ChannelId}>").ToList(), " · "));

        return embed.WithFooter(Footer(w,
            "🚫 : salon exclu (rien n'y rapporte d'XP). Vocal cumulé : minutes × personnes présentes. Comptage depuis la 7.6.0.")).Build();
    }

    // ---- /report commands ---------------------------------------------------------------------

    public static Embed BuildCommands(Compared<CommandReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("⌨️ Mes commandes", w);
        embed.WithDescription(d.Uses == 0
            ? "Aucune commande utilisée sur cette période (comptage depuis la 7.6.0)."
            : $"**{LevelCardUi.Xp(d.Uses)}** {Plural(d.Uses, "utilisation", "utilisations")}{TextChart.Trend(d.Uses, p?.Uses)}" +
              (d.Failures > 0 ? $" · **{d.Failures}** en erreur ({TextChart.Percent(d.Failures, d.Uses)})" : ""));

        if (d.Top.Count > 0)
            embed.AddField("Les plus utilisées", Cap(string.Join('\n', d.Top.Select(l =>
                $"`/{l.Command}` — {LevelCardUi.Xp(l.Uses)}{TextChart.Trend(l.Uses, p?.UsesByCommand.GetValueOrDefault(l.Command))}" +
                (l.Failures > 0 ? $" · ⚠️ {l.Failures}" : "")))));
        if (d.Shortcuts.Count > 0)
            embed.AddField("Raccourcis tapés", Fit(d.Shortcuts.Select(l => $"`{l.Command}` {LevelCardUi.Xp(l.Uses)}").ToList(), " · "));
        if (d.NeverUsed.Count > 0)
            embed.AddField($"Jamais utilisées ({d.NeverUsed.Count})", Fit(d.NeverUsed.Select(x => $"`/{x}`").ToList(), " · "));

        return embed.WithFooter(Footer(w, "Les commandes en message privé ne comptent pas ici. Comptage depuis la 7.6.0.")).Build();
    }
```

  And in the `// ---- Shared ----` part add:

```csharp
    private static string Mark(ChannelLine l) => l.Excluded ? "🚫 " : "";

    // Whole items only, up to a field's length: a list never cuts an emote's <:name:id> or a mention
    // in half. What doesn't fit is counted instead.
    private static string Fit(IReadOnlyList<string> items, string separator)
    {
        var sb = new StringBuilder();
        var shown = 0;
        foreach (var item in items)
        {
            var add = (shown == 0 ? "" : separator) + item;
            if (sb.Length + add.Length > MaxFieldLength - 30) break;
            sb.Append(add);
            shown++;
        }
        if (shown < items.Count) sb.Append($" … et {items.Count - shown} autres");
        return sb.Length == 0 ? "—" : sb.ToString();
    }
```

- [ ] **Step 6: Add the subcommands** to `ReportModule` (`ProjectSYNCS/Commands/Admin/ReportModule.cs`), after `SyncsAsync`:

```csharp
    [SlashCommand("activity", "Heures de pointe, salons écrits et salons vocaux")]
    public Task ActivityAsync() => ShowAsync(ReportKind.Activity);

    [SlashCommand("commands", "Mes commandes : les plus utilisées, les erreurs, celles qu'on oublie")]
    public Task CommandsAsync() => ShowAsync(ReportKind.Commands);
```

- [ ] **Step 7: Send the digest list.** In `MonthlyDigestService.SendAsync`, replace `foreach (var kind in Enum.GetValues<ReportKind>())` with `foreach (var kind in ReportKinds.Digest)`. In its header comment, change « the four /report embeds » to « the /report embeds of ReportKinds.Digest ».

- [ ] **Step 8: Build and run the checks.** Expected: build clean, `ALL CHECKS PASSED`, the new cards within their caps.

- [ ] **Step 9: Commit** (only if the owner asked): `git add ProjectSYNCS tests && git commit -m "/report activity and commands; both join the monthly digest"`.

---

### Task 7: `/report quiz`

**Files:**
- Create: `ProjectSYNCS/Helpers/Quiz/QuizReport.cs`, `ProjectSYNCS/Services/Quiz/QuizReportService.cs`
- Modify: `ProjectSYNCS/Helpers/Xp/CommunityReport.cs`, `ProjectSYNCS/Services/Admin/ReportService.cs`, `ProjectSYNCS/Commands/Admin/ReportCards.cs`, `ProjectSYNCS/Commands/Admin/ReportModule.cs`, `ProjectSYNCS/Program.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 28)

**Interfaces:**
- Consumes:
  - `QuizRound` (`MessageId`, `QuestionKey`, `PostedAt`, `WinnerId`, `WonAt`, `Closed`, `Day`, `GuildId`);
  - `QuizBank.ByKey(string) -> QuizQuestion?` (`Category`, `Difficulty`, `Prompt`);
  - `QuizBank.Label(QuizCategory)` and `QuizBank.Label(QuizDifficulty)`;
  - `PersonTally(ulong UserId, int Count, int Of)` and `SessionReport.ListSize` (batch 1).
- Produces:
  - `QuizGroupLine(string Label, int Posted, int Found)`
  - `QuizReportData(...)`
  - `QuizReport.Compute(IReadOnlyList<QuizRound>) -> QuizReportData`
  - `QuizReportService.GetAsync(ulong, ReportWindow) -> Task<Compared<QuizReportData>>`
  - `ReportKind.Quiz`
  - `ReportCards.BuildQuiz(Compared<QuizReportData>, ReportWindow, bool) -> Embed`

- [ ] **Step 1: Write the failing check.** Add section 28:

```csharp
// 28. /report quiz: posted, found, unanswered; a round whose send failed is not a question.
{
    var t7At = new DateTimeOffset(2026, 10, 19, 18, 0, 0, TimeSpan.Zero);
    var t7Q0 = QuizBank.All[0];
    var t7Q1 = QuizBank.All[1];
    var t7Rounds = new List<ProjectSYNCS.Models.QuizRound>
    {
        new() { GuildId = 1, MessageId = 5, QuestionKey = t7Q0.Key, PostedAt = t7At, Closed = true, WinnerId = 7, WonAt = t7At.AddSeconds(42), Day = 20261019 },
        new() { GuildId = 1, MessageId = 6, QuestionKey = t7Q1.Key, PostedAt = t7At, Closed = true, Day = 20261019 },
        new() { GuildId = 1, MessageId = 0, QuestionKey = t7Q1.Key, PostedAt = t7At, Closed = true, Day = 20261019 },   // send failed
    };
    var t7 = QuizReport.Compute(t7Rounds);
    if (t7.Posted != 2 || t7.Found != 1 || t7.Unanswered != 1) Fail($"quiz {t7.Posted}/{t7.Found}/{t7.Unanswered}");
    if (t7.MedianSeconds != 42 || t7.Fastest?.UserId != 7) Fail("quiz timing");
    if (!t7.UnfoundPrompts.SequenceEqual(new[] { t7Q1.Prompt })) Fail("unfound prompts");
    if (t7.ByCategory.Sum(l => l.Posted) != 2) Fail("quiz by category");
    var t7Worst = new QuizReportData(999, 999, 999,
        Enum.GetValues<QuizCategory>().Select(x => new QuizGroupLine(QuizBank.Label(x), 999, 999)).ToList(),
        Enum.GetValues<QuizDifficulty>().Select(x => new QuizGroupLine(QuizBank.Label(x), 999, 999)).ToList(),
        999, new PersonTally(1_234_567_890_123_456_789, 999_999, 0),
        Enumerable.Repeat(new string('Q', 400), QuizReport.ListedPrompts).ToList(),
        Enumerable.Range(0, 5).Select(i => new PersonTally(1_234_567_890_123_456_789 + (ulong)i, 999, 999)).ToList());
    var t7Embed = ProjectSYNCS.Commands.ReportCards.BuildQuiz(new(t7Worst, t7Worst), ReportWindow.From(StatsPeriod.Month, t7At), true);
    if (t7Embed.Length > 6000 || t7Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"quiz embed {t7Embed.Length}");
    // The community report counts the same posted rounds.
    var t7Community = CommunityReport.Compute(Array.Empty<ProjectSYNCS.Models.MemberDailyStat>(), Array.Empty<ProjectSYNCS.Models.MemberFlowDailyStat>(),
        Array.Empty<ProjectSYNCS.Models.EmoteDailyStat>(), new Dictionary<(ulong, string), string>(), t7Rounds, ReportWindow.From(StatsPeriod.Month, t7At));
    if (t7Community.QuizRounds != 2) Fail($"community counts {t7Community.QuizRounds} quiz rounds, a failed send included");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Expected: build error, `QuizReport` not found.

- [ ] **Step 3: Implement `ProjectSYNCS/Helpers/Quiz/QuizReport.cs`:**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public sealed record QuizGroupLine(string Label, int Posted, int Found);

/// <summary>What /report quiz shows. Built by <see cref="QuizReport.Compute"/>.</summary>
public sealed record QuizReportData(
    int Posted,
    int Found,
    int Unanswered,
    IReadOnlyList<QuizGroupLine> ByCategory,
    IReadOnlyList<QuizGroupLine> ByDifficulty,
    int? MedianSeconds,
    PersonTally? Fastest,
    IReadOnlyList<string> UnfoundPrompts,
    IReadOnlyList<PersonTally> Winners);

/// <summary>
/// The quiz over one window, from its QuizRound rows (filtered on Day in SQL by the caller). A round
/// whose card never went out (MessageId 0) is not a question. Category and difficulty are not
/// stored: they come from QuizBank by key (a key no longer in the bank is left out of those two
/// lists). Wrong answers are never stored, so there is no « hardest by attempts ». Pure.
/// </summary>
public static class QuizReport
{
    public const int ListedPrompts = 5;

    public static QuizReportData Compute(IReadOnlyList<QuizRound> rounds)
    {
        var posted = rounds.Where(r => r.MessageId != 0).ToList();
        var found = posted.Where(r => r.WinnerId != 0 && r.WonAt is not null).ToList();
        var unanswered = posted.Where(r => r.Closed && r.WinnerId == 0).ToList();
        var known = posted.Select(r => (Round: r, Question: QuizBank.ByKey(r.QuestionKey)))
            .Where(x => x.Question is not null).ToList();

        IReadOnlyList<QuizGroupLine> By<TKey>(Func<QuizQuestion, TKey> key, Func<TKey, string> label) where TKey : notnull =>
            known.GroupBy(x => key(x.Question!))
                .Select(g => new QuizGroupLine(label(g.Key), g.Count(), g.Count(x => x.Round.WinnerId != 0)))
                .OrderByDescending(l => l.Posted)
                .ToList();

        var times = found
            .Select(r => (Round: r, Ms: (int)(r.WonAt!.Value - r.PostedAt).TotalMilliseconds))
            .OrderBy(x => x.Ms)
            .ToList();

        return new QuizReportData(
            posted.Count, found.Count, unanswered.Count,
            By<QuizCategory>(q => q.Category, QuizBank.Label),
            By<QuizDifficulty>(q => q.Difficulty, QuizBank.Label),
            times.Count == 0 ? null : times[times.Count / 2].Ms / 1000,
            times.Count == 0 ? null : new PersonTally(times[0].Round.WinnerId, times[0].Ms, 0),
            unanswered.Select(r => QuizBank.ByKey(r.QuestionKey)?.Prompt).OfType<string>().Distinct().Take(ListedPrompts).ToList(),
            found.GroupBy(r => r.WinnerId)
                .Select(g => new PersonTally(g.Key, g.Count(), posted.Count))
                .OrderByDescending(x => x.Count)
                .Take(SessionReport.ListSize)
                .ToList());
    }
}
```

- [ ] **Step 4: Fix `CommunityReport`.** In `ProjectSYNCS/Helpers/Xp/CommunityReport.cs` (`Compute`'s return), replace the line `rounds.Count,` with:

```csharp
            rounds.Count(r => r.MessageId != 0),   // a round whose card never went out is not a question
```

- [ ] **Step 5: Create `ProjectSYNCS/Services/Quiz/QuizReportService.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>Reads the QuizRound rows behind /report quiz (Day is an int: filtered in SQL).</summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class QuizReportService
{
    private readonly AppDbContext _db;

    public QuizReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<QuizReportData>> GetAsync(ulong guildId, ReportWindow window)
    {
        async Task<QuizReportData> ComputeAsync(ReportWindow w) => QuizReport.Compute(
            await _db.QuizRounds.AsNoTracking()
                .Where(r => r.GuildId == guildId && r.Day >= w.FromDay && r.Day <= w.ToDay).ToListAsync());
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }
}
```

  Register it in `Program.cs` with `services.AddTransient<QuizReportService>();`.

- [ ] **Step 6: Wire `ReportService`.**
  - Append `Quiz,` to `ReportKind`.
  - Add a `QuizReportService quiz` constructor parameter and `_quiz` field.
  - Add this switch arm before the `_ =>` arm:

```csharp
            ReportKind.Quiz => ReportCards.BuildQuiz(await _quiz.GetAsync(guildId, window), window, names),
```

- [ ] **Step 7: Add the card** to `ReportCards`, before `// ---- Shared ----`:

```csharp
    // ---- /report quiz -------------------------------------------------------------------------

    public static Embed BuildQuiz(Compared<QuizReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("❓ Le quiz", w);
        if (d.Posted == 0)
            return embed.WithDescription("Aucune question posée sur cette période.").WithFooter(Footer(w)).Build();

        embed.WithDescription(
            $"**{d.Posted}** {Plural(d.Posted, "question posée", "questions posées")}{TextChart.Trend(d.Posted, p?.Posted)}" +
            $" · trouvées : **{d.Found}** ({TextChart.Percent(d.Found, d.Posted)})" +
            (p is { Posted: > 0 } ? $" · avant : {TextChart.Percent(p.Found, p.Posted)}" : "") +
            $" · sans réponse : **{d.Unanswered}**" +
            (d.MedianSeconds is { } s ? $"\nTemps de réponse médian : **{Seconds(s)}**" : ""));
        embed.AddField("Par catégorie", Cap(Groups(d.ByCategory)), inline: true);
        embed.AddField("Par difficulté", Cap(Groups(d.ByDifficulty)), inline: true);
        if (d.UnfoundPrompts.Count > 0)
            embed.AddField("Personne n'a trouvé", Cap(string.Join('\n', d.UnfoundPrompts.Select(q => $"• {q}"))));

        if (names)
        {
            AddPeople(embed, "Les meilleurs", d.Winners, t => $"{t.Count} {Plural(t.Count, "victoire", "victoires")}");
            if (d.Fastest is { } f)
                embed.AddField("Le plus rapide", $"<@{f.UserId}> — {(f.Count / 1000.0).ToString("0.0", Fr)} s", inline: true);
        }
        return embed.WithFooter(Footer(w)).Build();
    }

    private static string Groups(IReadOnlyList<QuizGroupLine> lines) =>
        lines.Count == 0 ? "—" : string.Join('\n', lines.Select(l => $"{l.Label} — {l.Found}/{l.Posted}"));

    private static string Seconds(int s) => s < 60 ? $"{s} s" : $"{s / 60} min {s % 60:00}";
```

- [ ] **Step 8: Add the subcommand** to `ReportModule`:

```csharp
    [SlashCommand("quiz", "Le quiz : trouvé ou pas, par catégorie, les questions qui résistent")]
    public Task QuizAsync() => ShowAsync(ReportKind.Quiz);
```

- [ ] **Step 9: Build and run the checks.** Expected: build clean, `ALL CHECKS PASSED`.

- [ ] **Step 10: Commit** (only if the owner asked): `git add ProjectSYNCS tests && git commit -m "/report quiz; community stops counting failed quiz sends"`.

---

### Task 8: `/report plynlings`

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/PlynlingReport.cs`, `ProjectSYNCS/Services/Plynling/PlynlingReportService.cs`
- Modify: `ProjectSYNCS/Services/Admin/ReportService.cs`, `ProjectSYNCS/Commands/Admin/ReportCards.cs`, `ProjectSYNCS/Commands/Admin/ReportModule.cs`, `ProjectSYNCS/Program.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 29)

**Interfaces:**
- Consumes:
  - `Plynling` (`Id`, `GuildId`, `OwnerId`, `Name`, `AdoptedAt`, `DiedAt`, `DeathCause`, `Hunger`, `NeedsAsOf`, `SickSince`, `FrozenAt`);
  - `PlynlingLife`: `Create(guildId, ownerId, name, species, gender, now)`, `IsSick`, `IsFrozen`, `IsDirty(p, t)`, `HungerAt(p, t)`, `Age(p, t)`, `Stage(p, t)`, `Settle(p, now)`, and the constants `StarvingBelow` (0.25) and `HungryBelow` (0.50);
  - `PlynlingMascot.Is(p)`, `PlynlingMascot.Bind(ulong)`;
  - `PlynlingEvents.ByKey(key)?.Title`;
  - `PlynlingEventInstance` (`PlynlingId`, `EventKey`, `ResolvedAt`, `DecidedAlone`, `ChallengeSucceeded`);
  - `PlynlingRelation` (`PlynlingAId`, `Bond`, `Since`), `PlynlingBadge` (`PlynlingId`, `EarnedAt`), `PlynlingTrait` (`PlynlingId`, `AcquiredAt`);
  - the enums `PlynlingBond`, `PlynlingStage`, `DeathCause`.
- Produces:
  - `PlynlingWatch(ulong OwnerId, string Name, bool Sick, bool Starving)`
  - `PlynlingReportData(...)`
  - `PlynlingReport.Compute(...) -> PlynlingReportData`
  - `PlynlingReportService.GetAsync(ulong, ReportWindow, DateTimeOffset) -> Task<Compared<PlynlingReportData>>`
  - `ReportKind.Plynlings`, `ReportCards.BuildPlynlings(...)`

- [ ] **Step 1: Write the failing check.** Add section 29:

```csharp
// 29. /report plynlings: births, deaths, the living's state; the mascot never counts.
{
    var t8Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    var t8Window = ReportWindow.From(StatsPeriod.Week, t8Now);
    ProjectSYNCS.Models.Plynling T8Pet(int id, ulong owner, int adoptedDaysAgo)
    {
        var p = PlynlingLife.Create(1, owner, $"Pet{id}", ProjectSYNCS.Models.PlynlingSpecies.Amanite, ProjectSYNCS.Models.PlynlingGender.Male, t8Now.AddDays(-adoptedDaysAgo));
        p.Id = id;
        p.Hunger = 0.9;
        p.NeedsAsOf = t8Now;
        return p;
    }
    var t8Healthy = T8Pet(1, 101, 30);
    var t8Starving = T8Pet(2, 102, 2);
    t8Starving.Hunger = 0.1;
    var t8Sick = T8Pet(3, 103, 40);
    t8Sick.SickSince = t8Now.AddHours(-3);
    var t8Dead = T8Pet(4, 104, 20);
    t8Dead.DiedAt = t8Now.AddDays(-1);
    t8Dead.DeathCause = ProjectSYNCS.Models.DeathCause.Illness;
    var t8Mascot = T8Pet(5, 999, 1);
    PlynlingMascot.Bind(999);
    var t8 = PlynlingReport.Compute(new[] { t8Healthy, t8Starving, t8Sick, t8Dead, t8Mascot },
        new[] { new ProjectSYNCS.Models.PlynlingEventInstance { PlynlingId = 1, EventKey = "nope", ResolvedAt = t8Now.AddDays(-2), DecidedAlone = true, ChallengeSucceeded = true } },
        Array.Empty<ProjectSYNCS.Models.PlynlingRelation>(), Array.Empty<ProjectSYNCS.Models.PlynlingBadge>(), Array.Empty<ProjectSYNCS.Models.PlynlingTrait>(),
        t8Window, t8Now);
    PlynlingMascot.Bind(0);
    if (t8.Living != 3) Fail($"living {t8.Living} (the mascot must not count)");
    if (t8.Born != 1) Fail($"born {t8.Born}");
    if (t8.Deaths.Single() != (ProjectSYNCS.Models.DeathCause.Illness, 1)) Fail("deaths by cause");
    if (t8.Starving != 1 || t8.Sick != 1) Fail($"starving {t8.Starving}, sick {t8.Sick}");
    if (t8.Watch.Count != 2 || !t8.Watch[0].Sick) Fail("watch list (sick first)");
    if (t8.EventsResolved != 1 || t8.DecidedAlone != 1 || t8.ChallengesWon != 1 || t8.TopEvents.Single().Title != "nope") Fail("events (an unknown key shows its key)");

    var t8Worst = t8 with
    {
        TopEvents = Enumerable.Range(0, PlynlingReport.TopEventCount).Select(i => (new string('E', 80), 999)).ToList(),
        Watch = Enumerable.Range(0, PlynlingReport.WatchCount).Select(i => new PlynlingWatch(1_234_567_890_123_456_789 + (ulong)i, "**@everyone** " + new string('N', 30), true, true)).ToList(),
    };
    var t8Embed = ProjectSYNCS.Commands.ReportCards.BuildPlynlings(new(t8Worst, t8Worst), t8Window, true);
    if (t8Embed.Length > 6000 || t8Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"plynlings embed {t8Embed.Length}");
    if (t8Embed.Fields.Any(f => f.Value.Contains("**@everyone**"))) Fail("a Plynling name was not sanitized");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Expected: build error, `PlynlingReport` not found.

- [ ] **Step 3: Implement `ProjectSYNCS/Helpers/Plynling/PlynlingReport.cs`:**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>A Plynling worth a look, by its owner: sick, or about to starve.</summary>
public sealed record PlynlingWatch(ulong OwnerId, string Name, bool Sick, bool Starving);

/// <summary>What /report plynlings shows. Built by <see cref="PlynlingReport.Compute"/>.</summary>
public sealed record PlynlingReportData(
    int Living,
    int Born,
    IReadOnlyList<(DeathCause Cause, int Count)> Deaths,
    TimeSpan? AverageLifespan,
    int Sick,
    int Starving,
    int Hungry,
    int Dirty,
    int Frozen,
    IReadOnlyList<(PlynlingStage Stage, int Count)> Stages,
    int EventsResolved,
    int DecidedAlone,
    int Challenges,
    int ChallengesWon,
    IReadOnlyList<(string Title, int Count)> TopEvents,
    IReadOnlyList<(PlynlingBond Bond, int Count)> Bonds,
    int Badges,
    int Traits,
    IReadOnlyList<PlynlingWatch> Watch);

/// <summary>
/// The Plynlings over one window, plus the living's state now. The caller loads the guild's
/// Plynlings AsNoTracking and settles them to now in memory (as /admin stats does); the other rows
/// are those Plynlings'. The mascot (Ping-Qilin) never counts. An abandoned Plynling is
/// hard-deleted with its history, so abandons can't be reported. Species is in /admin stats, not here.
/// A relation is counted once (its row), when its current bond began (Since). Pure.
/// </summary>
public static class PlynlingReport
{
    public const int TopEventCount = 5;
    public const int WatchCount = 10;

    public static PlynlingReportData Compute(
        IReadOnlyList<Plynling> plynlings, IReadOnlyList<PlynlingEventInstance> events, IReadOnlyList<PlynlingRelation> relations,
        IReadOnlyList<PlynlingBadge> badges, IReadOnlyList<PlynlingTrait> traits, ReportWindow window, DateTimeOffset now)
    {
        var pets = plynlings.Where(p => !PlynlingMascot.Is(p)).ToList();
        var ids = pets.Select(p => p.Id).ToHashSet();
        var living = pets.Where(p => p.DiedAt is null).ToList();
        var awake = living.Where(p => !PlynlingLife.IsFrozen(p)).ToList();
        var dead = pets.Where(p => p.DiedAt is { } died && window.Contains(died)).ToList();
        bool Starving(Plynling p) => PlynlingLife.HungerAt(p, now) < PlynlingLife.StarvingBelow;
        var resolved = events.Where(e => ids.Contains(e.PlynlingId) && e.ResolvedAt is { } at && window.Contains(at)).ToList();

        return new PlynlingReportData(
            living.Count,
            pets.Count(p => window.Contains(p.AdoptedAt)),
            dead.GroupBy(p => p.DeathCause).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList(),
            dead.Count == 0 ? null : TimeSpan.FromTicks((long)dead.Average(p => PlynlingLife.Age(p, p.DiedAt!.Value).Ticks)),
            awake.Count(PlynlingLife.IsSick),
            awake.Count(Starving),
            awake.Count(p => !Starving(p) && PlynlingLife.HungerAt(p, now) < PlynlingLife.HungryBelow),
            awake.Count(p => PlynlingLife.IsDirty(p, now)),
            living.Count(PlynlingLife.IsFrozen),
            living.GroupBy(p => PlynlingLife.Stage(p, now)).OrderBy(g => g.Key).Select(g => (g.Key, g.Count())).ToList(),
            resolved.Count,
            resolved.Count(e => e.DecidedAlone),
            resolved.Count(e => e.ChallengeSucceeded is not null),
            resolved.Count(e => e.ChallengeSucceeded == true),
            resolved.GroupBy(e => e.EventKey)
                .Select(g => (PlynlingEvents.ByKey(g.Key)?.Title ?? g.Key, g.Count()))
                .OrderByDescending(x => x.Item2).Take(TopEventCount).ToList(),
            relations.Where(r => ids.Contains(r.PlynlingAId) && r.Bond != PlynlingBond.Acquaintances && window.Contains(r.Since))
                .GroupBy(r => r.Bond).OrderBy(g => g.Key).Select(g => (g.Key, g.Count())).ToList(),
            badges.Count(b => ids.Contains(b.PlynlingId) && window.Contains(b.EarnedAt)),
            traits.Count(t => ids.Contains(t.PlynlingId) && window.Contains(t.AcquiredAt)),
            awake.Where(p => PlynlingLife.IsSick(p) || Starving(p))
                .OrderByDescending(PlynlingLife.IsSick).ThenBy(p => PlynlingLife.HungerAt(p, now))
                .Take(WatchCount)
                .Select(p => new PlynlingWatch(p.OwnerId, p.Name, PlynlingLife.IsSick(p), Starving(p)))
                .ToList());
    }
}
```

- [ ] **Step 4: Create `ProjectSYNCS/Services/Plynling/PlynlingReportService.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the rows behind /report plynlings: the guild's Plynlings (settled to now in memory only, as
/// AdminStatsService does: AsNoTracking, never saved) and their events, relations, badges and traits,
/// windowed in memory (their dates are DateTimeOffsets).
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class PlynlingReportService
{
    private readonly AppDbContext _db;

    public PlynlingReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<PlynlingReportData>> GetAsync(ulong guildId, ReportWindow window, DateTimeOffset now)
    {
        var plynlings = await _db.Plynlings.AsNoTracking().Where(p => p.GuildId == guildId).ToListAsync();
        foreach (var p in plynlings) PlynlingLife.Settle(p, now);   // in memory only: AsNoTracking, never saved
        var ids = plynlings.Select(p => p.Id).ToList();
        var events = await _db.PlynlingEventInstances.AsNoTracking()
            .Where(e => ids.Contains(e.PlynlingId) && e.ResolvedAt != null).ToListAsync();
        var relations = await _db.PlynlingRelations.AsNoTracking().Where(r => ids.Contains(r.PlynlingAId)).ToListAsync();
        var badges = await _db.PlynlingBadges.AsNoTracking().Where(b => ids.Contains(b.PlynlingId)).ToListAsync();
        var traits = await _db.PlynlingTraits.AsNoTracking().Where(t => ids.Contains(t.PlynlingId)).ToListAsync();

        PlynlingReportData Compute(ReportWindow w) => PlynlingReport.Compute(plynlings, events, relations, badges, traits, w, now);
        return new(Compute(window), window.Previous is { } prev ? Compute(prev) : null);
    }
}
```

  Register it in `Program.cs` with `services.AddTransient<PlynlingReportService>();`.

- [ ] **Step 5: Wire `ReportService`.**
  - Append `Plynlings,` to `ReportKind`.
  - Add a `PlynlingReportService plynlings` constructor parameter and `_plynlings` field.
  - Add this switch arm before `_ =>`:

```csharp
            ReportKind.Plynlings => ReportCards.BuildPlynlings(
                await _plynlings.GetAsync(guildId, window, DateTimeOffset.UtcNow), window, names),
```

- [ ] **Step 6: Add the card** to `ReportCards`, before `// ---- Shared ----`. The labels are gender-neutral (Plynlings have genders):

```csharp
    // ---- /report plynlings --------------------------------------------------------------------

    public static Embed BuildPlynlings(Compared<PlynlingReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("🌱 Les Plynlings", w);
        var deaths = d.Deaths.Sum(x => x.Count);
        embed.WithDescription(
            $"**{d.Living}** en vie · **{d.Born}** {Plural(d.Born, "adoption", "adoptions")}{TextChart.Trend(d.Born, p?.Born)}" +
            $" · **{deaths}** {Plural(deaths, "décès", "décès")}" +
            (deaths > 0 ? $" ({string.Join(", ", d.Deaths.Select(x => $"{DeathLabel(x.Cause)} : {x.Count}"))})" : "") +
            (d.AverageLifespan is { } life ? $"\nDurée de vie moyenne des disparus : **{Math.Max(1, (int)Math.Round(life.TotalDays))} jours**" : ""));

        embed.AddField("Les vivants, maintenant",
            $"Malades : **{d.Sick}** · très faim : **{d.Starving}** · faim : **{d.Hungry}**\n" +
            $"Sales : **{d.Dirty}** · gelés : **{d.Frozen}**\n" +
            (d.Stages.Count == 0 ? "—" : string.Join(" · ", d.Stages.Select(s => $"{StageLabel(s.Stage)} : {s.Count}"))));

        embed.AddField("Événements", d.EventsResolved == 0
            ? "Aucun événement résolu."
            : Cap($"Résolus : **{d.EventsResolved}**{TextChart.Trend(d.EventsResolved, p?.EventsResolved)}" +
                  $" · choisis seuls : **{d.DecidedAlone}** ({TextChart.Percent(d.DecidedAlone, d.EventsResolved)})\n" +
                  (d.Challenges > 0 ? $"Défis réussis : **{d.ChallengesWon}/{d.Challenges}**\n" : "") +
                  string.Join('\n', d.TopEvents.Select(e => $"• {e.Title} ×{e.Count}"))));

        embed.AddField("Liens et progrès",
            (d.Bonds.Count == 0 ? "Aucun lien noué." : string.Join(" · ", d.Bonds.Select(b => $"{BondLabel(b.Bond)} : {b.Count}"))) +
            $"\nBadges gagnés : **{d.Badges}** · traits acquis : **{d.Traits}**");

        if (names && d.Watch.Count > 0)
            embed.AddField("À surveiller", Cap(string.Join('\n', d.Watch.Select(x =>
                $"<@{x.OwnerId}> — {Format.Sanitize(x.Name)}{(x.Sick ? " · malade" : "")}{(x.Starving ? " · très faim" : "")}"))));

        return embed.WithFooter(Footer(w,
            "« Choisis seuls » : sans réponse de son maître, le Plynling a décidé. Ping-Qilin n'est pas compté.")).Build();
    }

    private static string DeathLabel(DeathCause cause) => cause switch
    {
        DeathCause.Illness => "maladie",
        _ => "faim",
    };

    private static string StageLabel(PlynlingStage stage) => stage switch
    {
        PlynlingStage.Baby => "Bébés",
        PlynlingStage.Teen => "Ados",
        PlynlingStage.Adult => "Adultes",
        _ => "Anciens",
    };

    private static string BondLabel(PlynlingBond bond) => bond switch
    {
        PlynlingBond.Friends => "Amitiés",
        PlynlingBond.BestFriends => "Meilleurs amis",
        PlynlingBond.Lovers => "Amours",
        PlynlingBond.Rivals => "Rivalités",
        PlynlingBond.Enemies => "Inimitiés",
        _ => "Connaissances",
    };
```

- [ ] **Step 7: Add the subcommand** to `ReportModule`:

```csharp
    [SlashCommand("plynlings", "Les Plynlings : naissances, décès, santé, événements, liens")]
    public Task PlynlingsAsync() => ShowAsync(ReportKind.Plynlings);
```

- [ ] **Step 8: Build and run the checks.** Expected: build clean, `ALL CHECKS PASSED`.

- [ ] **Step 9: Commit** (only if the owner asked): `git add ProjectSYNCS tests && git commit -m "/report plynlings"`.

---

### Task 9: `/report emotes`

**Files:**
- Create: `ProjectSYNCS/Helpers/Xp/EmoteReport.cs`, `ProjectSYNCS/Services/Xp/EmoteReportService.cs`
- Modify: `ProjectSYNCS/Services/Admin/ReportService.cs`, `ProjectSYNCS/Commands/Admin/ReportCards.cs`, `ProjectSYNCS/Commands/Admin/ReportModule.cs`, `ProjectSYNCS/Program.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 30)

**Interfaces:**
- Consumes:
  - `EmoteDailyStat` (`GuildId`, `EmoteId` (0 = Unicode), `Unicode`, `Day`, `WrittenCount`, `ReactedCount`);
  - `EmoteStat.Markup` (not mapped; computed after load);
  - `SocketGuild.Emotes` (`GuildEmote.Id`, `ToString()` gives `<:name:id>` or `<a:name:id>`);
  - `ReportCards.Fit` (Task 6).
- Produces:
  - `ServerEmote(ulong Id, string Markup)`, `EmoteLine(string Markup, long Count)`
  - `EmoteReportData(...)`
  - `EmoteReport.Compute(...)`, `EmoteReport.Movers(EmoteReportData, EmoteReportData, bool rising)`
  - `EmoteReportService.GetAsync(ulong, ReportWindow, IReadOnlyList<ServerEmote>) -> Task<Compared<EmoteReportData>>`
  - `ReportKind.Emotes`, `ReportCards.BuildEmotes(...)`

- [ ] **Step 1: Write the failing check.** Add section 30:

```csharp
// 30. /report emotes: own, foreign and Unicode; never-used server emotes; movers.
{
    ProjectSYNCS.Models.EmoteDailyStat T9Row(ulong id, string unicode, long written, long reacted) =>
        new() { GuildId = 1, EmoteId = id, Unicode = unicode, Day = 20261019, WrittenCount = written, ReactedCount = reacted };
    var t9Server = new[] { new ServerEmote(1, "<:one:1>"), new ServerEmote(2, "<:two:2>"), new ServerEmote(3, "<:three:3>") };
    var t9Markup = new Dictionary<(ulong, string), string> { [(50, "")] = "<:elsewhere:50>" };
    var t9 = EmoteReport.Compute(new[] { T9Row(1, "", 5, 5), T9Row(2, "", 1, 0), T9Row(50, "", 4, 0), T9Row(77, "", 1, 0), T9Row(0, "😂", 9, 0) }, t9Server, t9Markup);
    if (t9.Own != 11 || t9.Foreign != 5 || t9.Unicode != 9) Fail($"emote split {t9.Own}/{t9.Foreign}/{t9.Unicode}");
    if (t9.TopOwn[0].Markup != "<:one:1>" || !t9.UnusedOwn.SequenceEqual(new[] { "<:three:3>" })) Fail("own ranking or never used");
    if (!t9.TopForeign.Any(l => l.Markup.Contains("77"))) Fail("a foreign emote without a stat row must still show");
    var t9Before = EmoteReport.Compute(new[] { T9Row(2, "", 8, 0) }, t9Server, t9Markup);
    if (EmoteReport.Movers(t9, t9Before, rising: true)[0].Markup != "<:one:1>" || EmoteReport.Movers(t9, t9Before, rising: false)[0].Markup != "<:two:2>") Fail("movers");
    var t9NoGuild = EmoteReport.Compute(new[] { T9Row(1, "", 5, 5) }, Array.Empty<ServerEmote>(), t9Markup);
    if (t9NoGuild.Own != 0 || t9NoGuild.UnusedOwn.Count != 0) Fail("guild unavailable: no emote counts as the server's");

    var t9ManyServer = Enumerable.Range(0, 80).Select(i => new ServerEmote(1000 + (ulong)i, $"<a:a_long_emote_name_{i}:{1_234_567_890_123_456_789 + (ulong)i}>")).ToList();
    var t9Worst = EmoteReport.Compute(t9ManyServer.Take(40).Select(e => T9Row(e.Id, "", 999_999, 999_999)).ToList(), t9ManyServer, t9Markup);
    var t9Embed = ProjectSYNCS.Commands.ReportCards.BuildEmotes(new(t9Worst, t9Before), ReportWindow.From(StatsPeriod.Month, DateTimeOffset.UtcNow), true);
    if (t9Embed.Length > 6000 || t9Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"emotes embed {t9Embed.Length}");
    if (t9Embed.Fields.Any(f => System.Text.RegularExpressions.Regex.IsMatch(f.Value, @"<a?:\w+:\d*(?!\d|>)$|<a?:\w*$"))) Fail("an emote was cut in half");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Expected: build error, `EmoteReport` not found.

- [ ] **Step 3: Implement `ProjectSYNCS/Helpers/Xp/EmoteReport.cs`:**

```csharp
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>One of the server's own emotes, from the live guild: its id and markup.</summary>
public sealed record ServerEmote(ulong Id, string Markup);

public sealed record EmoteLine(string Markup, long Count);

/// <summary>What /report emotes shows. Built by <see cref="EmoteReport.Compute"/>.</summary>
public sealed record EmoteReportData(
    long Own,
    long Foreign,
    long Unicode,
    IReadOnlyList<EmoteLine> TopOwn,
    IReadOnlyList<string> UnusedOwn,
    IReadOnlyList<EmoteLine> TopForeign,
    IReadOnlyDictionary<ulong, long> OwnById,
    IReadOnlyList<ServerEmote> Server);

/// <summary>
/// Emote health over one window, from EmoteDailyStat rows (written and reacted together; buckets
/// from 2026-08-05). Emotes from other servers are counted by EmoteTracker too: an emote is the
/// server's own when its id is in the live guild's list. A server emote with no row is « never
/// used ». A foreign emote with no EmoteStat row shows by id. Pure.
/// </summary>
public static class EmoteReport
{
    public const int TopOwnCount = 10;
    public const int TopForeignCount = 5;
    public const int MoverCount = 3;

    public static EmoteReportData Compute(
        IReadOnlyList<EmoteDailyStat> rows, IReadOnlyList<ServerEmote> server, IReadOnlyDictionary<(ulong, string), string> markup)
    {
        var own = server.ToDictionary(e => e.Id);
        var per = rows.GroupBy(r => (r.EmoteId, r.Unicode))
            .Select(g => (g.Key.EmoteId, g.Key.Unicode, Count: g.Sum(r => r.WrittenCount + r.ReactedCount)))
            .Where(x => x.Count > 0)
            .ToList();
        var ownCounts = per.Where(x => x.EmoteId != 0 && own.ContainsKey(x.EmoteId)).ToDictionary(x => x.EmoteId, x => x.Count);
        var foreign = per.Where(x => x.EmoteId != 0 && !own.ContainsKey(x.EmoteId)).ToList();

        return new EmoteReportData(
            ownCounts.Values.Sum(),
            foreign.Sum(x => x.Count),
            per.Where(x => x.EmoteId == 0).Sum(x => x.Count),
            ownCounts.OrderByDescending(x => x.Value).Take(TopOwnCount).Select(x => new EmoteLine(own[x.Key].Markup, x.Value)).ToList(),
            server.Where(e => !ownCounts.ContainsKey(e.Id)).Select(e => e.Markup).ToList(),
            foreign.OrderByDescending(x => x.Count).Take(TopForeignCount)
                .Select(x => new EmoteLine(markup.TryGetValue((x.EmoteId, x.Unicode), out var m) && m.Length > 0 ? m : $"`{x.EmoteId}`", x.Count))
                .ToList(),
            ownCounts,
            server);
    }

    /// <summary>The server's emotes that rose (or fell) the most against the previous window, with the change.</summary>
    public static IReadOnlyList<EmoteLine> Movers(EmoteReportData current, EmoteReportData previous, bool rising) =>
        current.Server
            .Select(e => new EmoteLine(e.Markup, current.OwnById.GetValueOrDefault(e.Id) - previous.OwnById.GetValueOrDefault(e.Id)))
            .Where(l => rising ? l.Count > 0 : l.Count < 0)
            .OrderByDescending(l => rising ? l.Count : -l.Count)
            .Take(MoverCount)
            .ToList();
}
```

- [ ] **Step 4: Create `ProjectSYNCS/Services/Xp/EmoteReportService.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the EmoteDailyStat buckets behind /report emotes (Day filtered in SQL) and EmoteStat's
/// markup (where a renamed emote's current name lives, as EmoteStatsService does).
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class EmoteReportService
{
    private readonly AppDbContext _db;

    public EmoteReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<EmoteReportData>> GetAsync(ulong guildId, ReportWindow window, IReadOnlyList<ServerEmote> server)
    {
        var markup = await _db.EmoteStats.AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .ToDictionaryAsync(s => (s.EmoteId, s.Unicode), s => s.Markup);
        async Task<EmoteReportData> ComputeAsync(ReportWindow w) => EmoteReport.Compute(
            await _db.EmoteDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync(),
            server, markup);
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }
}
```

  Register it in `Program.cs` with `services.AddTransient<EmoteReportService>();`.

- [ ] **Step 5: Wire `ReportService`.**
  - Append `Emotes,` to `ReportKind`.
  - Add an `EmoteReportService emotes` constructor parameter and `_emotes` field.
  - Add the arm (before `_ =>`) and the snapshot helper:

```csharp
            ReportKind.Emotes => ReportCards.BuildEmotes(
                await _emotes.GetAsync(guildId, window, ServerEmotes(guildId)), window, names),
```

```csharp
    // The guild's own emotes as plain data; empty when the guild is unavailable.
    private IReadOnlyList<ServerEmote> ServerEmotes(ulong guildId) =>
        _client.GetGuild(guildId)?.Emotes.Select(e => new ServerEmote(e.Id, e.ToString())).ToList()
        ?? (IReadOnlyList<ServerEmote>)Array.Empty<ServerEmote>();
```

- [ ] **Step 6: Add the card** to `ReportCards`, before `// ---- Shared ----`:

```csharp
    // ---- /report emotes -----------------------------------------------------------------------

    public static Embed BuildEmotes(Compared<EmoteReportData> c, ReportWindow w, bool names)
    {
        var d = c.Current;
        var p = c.Previous;
        var embed = Start("😀 Les émotes", w);
        var total = d.Own + d.Foreign + d.Unicode;
        embed.WithDescription(total == 0
            ? "Aucune émote utilisée sur cette période."
            : $"**{LevelCardUi.Xp(total)}** émotes{TextChart.Trend(total, p is null ? null : p.Own + p.Foreign + p.Unicode)}" +
              $" · du serveur : **{TextChart.Percent(d.Own, total)}** · d'ailleurs : **{TextChart.Percent(d.Foreign, total)}**" +
              $" · emoji : **{TextChart.Percent(d.Unicode, total)}**");

        if (d.TopOwn.Count > 0)
            embed.AddField("Les préférées du serveur", Fit(d.TopOwn.Select(e => $"{e.Markup} ×{LevelCardUi.Xp(e.Count)}").ToList(), " · "));
        if (p is not null)
        {
            var up = EmoteReport.Movers(d, p, rising: true);
            var down = EmoteReport.Movers(d, p, rising: false);
            if (up.Count + down.Count > 0)
                embed.AddField("Tendances",
                    (up.Count > 0 ? "En hausse : " + string.Join(" · ", up.Select(e => $"{e.Markup} +{e.Count}")) : "") +
                    (up.Count > 0 && down.Count > 0 ? "\n" : "") +
                    (down.Count > 0 ? "En baisse : " + string.Join(" · ", down.Select(e => $"{e.Markup} −{-e.Count}")) : ""));
        }
        if (d.UnusedOwn.Count > 0)
            embed.AddField($"Jamais utilisées ({d.UnusedOwn.Count})", Fit(d.UnusedOwn, " "));
        if (d.TopForeign.Count > 0)
            embed.AddField("Venues d'autres serveurs", Fit(d.TopForeign.Select(e => $"{e.Markup} ×{LevelCardUi.Xp(e.Count)}").ToList(), " · "));

        return embed.WithFooter(Footer(w, "Écrites et en réaction.")).Build();
    }
```

- [ ] **Step 7: Add the subcommand** to `ReportModule`:

```csharp
    [SlashCommand("emotes", "Les émotes du serveur : préférées, oubliées, tendances")]
    public Task EmotesAsync() => ShowAsync(ReportKind.Emotes);
```

- [ ] **Step 8: Build and run the checks.** Expected: build clean, `ALL CHECKS PASSED`.

- [ ] **Step 9: Commit** (only if the owner asked): `git add ProjectSYNCS tests && git commit -m "/report emotes"`.

---

### Task 10: `/report sleeping` with 30 / 60 / 90-day buttons

**Files:**
- Create: `ProjectSYNCS/Helpers/Xp/SleepingReport.cs`
- Modify: `ProjectSYNCS/Services/Xp/CommunityReportService.cs`, `ProjectSYNCS/Services/Admin/ReportService.cs`, `ProjectSYNCS/Commands/Admin/ReportCards.cs`, `ProjectSYNCS/Commands/Admin/ReportModule.cs`, `ProjectSYNCS/Interactions/Components/ReportComponentHandler.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (section 31)

**Interfaces:**
- Consumes:
  - `MemberDailyStat` (`GuildId`, `UserId`, `Day`, `XpEarned`, `ReactionsUsed`, `VoiceMinutes`);
  - `SocketGuild.Users` (`Id`, `JoinedAt`, `IsBot`; complete because `AlwaysDownloadUsers` is on);
  - `AppTime.DayKey`, `AppTime.FromDayKey`; `ReportCards.MaxFieldLength`.
- Produces:
  - `MemberInfo(ulong UserId, DateTimeOffset? JoinedAt, bool IsBot)`, `Sleeper(ulong UserId, int? LastDay)`
  - `SleepingReportData(int Days, int Members, IReadOnlyList<Sleeper> Sleepers)`
  - `SleepingReport.Choices` (30, 60, 90), `SleepingReport.ReliableSince` (20260809)
  - `SleepingReport.Compute(...)`
  - `CommunityReportService.LastActiveDaysAsync(ulong) -> Task<IReadOnlyDictionary<ulong,int>>`
  - `ReportService.BuildSleepingAsync(ulong, int) -> Task<Embed>`
  - `ReportCards.BuildSleeping(SleepingReportData) -> Embed`, `ReportCards.SleepRow(int) -> MessageComponent`
  - custom-id `report:sleep:{days}`
  - `ReportKind.Sleeping`

- [ ] **Step 1: Write the failing check.** Add section 31:

```csharp
// 31. /report sleeping: who has been silent N days; newcomers and bots never listed.
{
    var t10Now = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    var t10Members = new[]
    {
        new MemberInfo(1, t10Now.AddDays(-200), false),   // active 5 days ago
        new MemberInfo(2, t10Now.AddDays(-200), false),   // last seen 40 days ago
        new MemberInfo(3, t10Now.AddDays(-200), false),   // never active
        new MemberInfo(4, t10Now.AddDays(-10), false),    // joined recently: never listed
        new MemberInfo(5, null, false),                    // unknown join date: an old member
        new MemberInfo(6, t10Now.AddDays(-200), true),    // a bot
    };
    var t10Last = new Dictionary<ulong, int> { [1] = AppTime.DayKey(t10Now.AddDays(-5)), [2] = AppTime.DayKey(t10Now.AddDays(-40)) };
    var t10 = SleepingReport.Compute(t10Members, t10Last, 30, t10Now);
    if (t10.Members != 5) Fail($"members {t10.Members} (bots excluded)");
    if (!t10.Sleepers.Select(s => s.UserId).SequenceEqual(new ulong[] { 3, 5, 2 })) Fail($"sleepers {string.Join(",", t10.Sleepers.Select(s => s.UserId))} (never-active first)");
    if (SleepingReport.Compute(t10Members, t10Last, 60, t10Now).Sleepers.Any(s => s.UserId == 2)) Fail("40 days silent is not asleep at 60");
    if (SleepingReport.Compute(Array.Empty<MemberInfo>(), t10Last, 30, t10Now).Sleepers.Count != 0) Fail("guild unavailable");

    var t10Many = Enumerable.Range(0, 300).Select(i => new Sleeper(1_234_567_890_123_456_789 + (ulong)i, i % 2 == 0 ? null : 20260901)).ToList();
    var t10Embed = ProjectSYNCS.Commands.ReportCards.BuildSleeping(new SleepingReportData(90, 400, t10Many));
    if (t10Embed.Length > 6000 || t10Embed.Fields.Length > 2 || t10Embed.Fields.Any(f => f.Value.Length is 0 or > 1024)) Fail($"sleeping embed {t10Embed.Length}, {t10Embed.Fields.Length} fields");
    if (!t10Embed.Footer!.Value.Text.Contains("autres")) Fail("the sleepers that don't fit must be counted");
}
```

- [ ] **Step 2: Run the checks to see them fail.** Expected: build error, `SleepingReport` not found.

- [ ] **Step 3: Implement `ProjectSYNCS/Helpers/Xp/SleepingReport.cs`:**

```csharp
namespace ProjectSYNCS.Helpers;

/// <summary>A member of the guild as the report sees it: plain data.</summary>
public sealed record MemberInfo(ulong UserId, DateTimeOffset? JoinedAt, bool IsBot);

/// <summary>A member with no sign of life: the last active day (yyyymmdd), or null for never.</summary>
public sealed record Sleeper(ulong UserId, int? LastDay);

public sealed record SleepingReportData(int Days, int Members, IReadOnlyList<Sleeper> Sleepers);

/// <summary>
/// Members silent for N days: no XP, reaction or voice minute (MemberDailyStat) since then. Only
/// people on the server for longer than N days are listed (an unknown join date counts as old);
/// bots never are. XP buckets are reliable from <see cref="ReliableSince"/> (the ResetXpTotals
/// migration), so « never » means never since then. Activity in excluded channels earns nothing and
/// so is invisible here. Pure.
/// </summary>
public static class SleepingReport
{
    public static readonly IReadOnlyList<int> Choices = new[] { 30, 60, 90 };
    public const int ReliableSince = 20260809;

    public static SleepingReportData Compute(
        IReadOnlyList<MemberInfo> members, IReadOnlyDictionary<ulong, int> lastActiveDay, int days, DateTimeOffset now)
    {
        var since = now.AddDays(-days);
        var threshold = AppTime.DayKey(since);
        var people = members.Where(m => !m.IsBot).ToList();
        var sleepers = people
            .Where(m => m.JoinedAt is null || m.JoinedAt <= since)
            .Select(m => new Sleeper(m.UserId, lastActiveDay.TryGetValue(m.UserId, out var day) ? day : null))
            .Where(s => s.LastDay is null || s.LastDay < threshold)
            .OrderBy(s => s.LastDay ?? 0).ThenBy(s => s.UserId)
            .ToList();
        return new SleepingReportData(days, people.Count, sleepers);
    }
}
```

  If the compiler rejects the conditional `? day : null`, write `? (int?)day : null`.

- [ ] **Step 4: Add the read** to `CommunityReportService` (`ProjectSYNCS/Services/Xp/CommunityReportService.cs`):

```csharp
    /// <summary>Each member's last day with any XP, reaction or voice minute (yyyymmdd), for /report sleeping.</summary>
    public async Task<IReadOnlyDictionary<ulong, int>> LastActiveDaysAsync(ulong guildId) =>
        await _db.MemberDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && (x.XpEarned > 0 || x.ReactionsUsed > 0 || x.VoiceMinutes > 0))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(x => x.Day) })
            .ToDictionaryAsync(x => x.UserId, x => x.Last);
```

- [ ] **Step 5: Wire `ReportService`.**
  - Append `Sleeping,` to `ReportKind`.
  - Add this arm before `_ =>`:

```csharp
            ReportKind.Sleeping => await BuildSleepingAsync(guildId, SleepingReport.Choices[0]),
```

  And add the method:

```csharp
    /// <summary>/report sleeping: no window, a silence of <paramref name="days"/> days.</summary>
    public async Task<Embed> BuildSleepingAsync(ulong guildId, int days)
    {
        var members = _client.GetGuild(guildId)?.Users.Select(u => new MemberInfo(u.Id, u.JoinedAt, u.IsBot)).ToList()
            ?? new List<MemberInfo>();
        var last = await _community.LastActiveDaysAsync(guildId);
        return ReportCards.BuildSleeping(SleepingReport.Compute(members, last, days, DateTimeOffset.UtcNow));
    }
```

- [ ] **Step 6: Add the card and its buttons** to `ReportCards`, before `// ---- Shared ----`:

```csharp
    // ---- /report sleeping ---------------------------------------------------------------------

    public static Embed BuildSleeping(SleepingReportData d)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"💤 Les endormis — rien depuis {d.Days} jours")
            .WithColor(Color.Purple)
            .WithDescription(d.Sleepers.Count == 0
                ? "Tout le monde a donné signe de vie. Pour l'instant."
                : $"**{d.Sleepers.Count}** sur **{d.Members}** {Plural(d.Members, "membre", "membres")}" +
                  $" ({TextChart.Percent(d.Sleepers.Count, d.Members)}) sans XP, réaction ni vocal depuis {d.Days} jours.");

        var lines = d.Sleepers.Select(s => $"<@{s.UserId}> — " + (s.LastDay is { } day
            ? $"dernier signe : {AppTime.FromDayKey(day):dd/MM/yyyy}"
            : $"jamais (depuis le {AppTime.FromDayKey(SleepingReport.ReliableSince):dd/MM/yyyy})")).ToList();
        var left = AddLineFields(embed, "Endormis", lines, maxFields: 2);

        var footer = $"Arrivés depuis moins de {d.Days} jours : pas comptés. L'activité dans les salons exclus ne compte pas : " +
                     "qui ne parle que là semble endormi.";
        if (left > 0) footer = $"… et {left} autres. " + footer;
        return embed.WithFooter(footer).Build();
    }

    public static MessageComponent SleepRow(int days)
    {
        var row = new ComponentBuilder();
        foreach (var choice in SleepingReport.Choices)
            row.WithButton($"{choice} jours", $"report:sleep:{choice}",
                choice == days ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: choice == days);
        return row.Build();
    }

    // Whole lines into up to maxFields fields; returns how many lines didn't fit.
    private static int AddLineFields(EmbedBuilder embed, string title, IReadOnlyList<string> lines, int maxFields)
    {
        var index = 0;
        for (var f = 0; f < maxFields && index < lines.Count; f++)
        {
            var sb = new StringBuilder();
            while (index < lines.Count && sb.Length + lines[index].Length + 1 <= MaxFieldLength - 30)
            {
                sb.Append(lines[index]).Append('\n');
                index++;
            }
            if (sb.Length == 0) break;
            embed.AddField(f == 0 ? title : "​", sb.ToString().TrimEnd());
        }
        return lines.Count - index;
    }
```

- [ ] **Step 7: Add the subcommand** to `ReportModule`.
  - Extract the refusal text into `private const string Refusal = "Mes rapports sont réservés aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";` and use it in `ShowAsync`.
  - Add:

```csharp
    [SlashCommand("sleeping", "Les membres sans signe de vie depuis 30, 60 ou 90 jours")]
    public async Task SleepingAsync()
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(Refusal, ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var days = SleepingReport.Choices[0];
        var embed = await _reports.BuildSleepingAsync(Context.Guild.Id, days);
        await FollowupAsync(embed: embed, components: ReportCards.SleepRow(days), ephemeral: true, allowedMentions: AllowedMentions.None);
    }
```

- [ ] **Step 8: Handle the buttons.** In `ReportComponentHandler` add the handler. Its verb is `sleep`, not `win`, so it can never collide with the period row:

```csharp
    [ComponentInteraction("report:sleep:*", ignoreGroupNames: true)]
    public async Task OnSleepAsync(string daysStr)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Mes rapports sont réservés aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        if (!int.TryParse(daysStr, out var days) || !SleepingReport.Choices.Contains(days)) days = SleepingReport.Choices[0];
        await DeferAsync();
        var embed = await _reports.BuildSleepingAsync(Context.Guild.Id, days);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed;
            m.Components = ReportCards.SleepRow(days);
            m.AllowedMentions = AllowedMentions.None;
        });
    }
```

- [ ] **Step 9: Build and run the checks.** Expected: build clean, `ALL CHECKS PASSED`.

- [ ] **Step 10: Commit** (only if the owner asked): `git add ProjectSYNCS tests && git commit -m "/report sleeping with 30/60/90-day buttons"`.

---

### Task 11: Docs, help, version, and a live run

**Files:**
- Modify: `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`, `README.md`, `docs/agents/admin.md`, `CLAUDE.md`, and possibly `ProjectSYNCS/Commands/Core/HelpModule.cs`
- Test: the checks (the existing `/help` measurement) and a dev-guild run

- [ ] **Step 1: Bump the version.**
  - In `ProjectSYNCS/config.yaml` set `version: "7.6.0"`. If `config.yaml` already differs from `HEAD` (`git diff HEAD -- ProjectSYNCS/config.yaml`), don't bump: extend that batch's entry instead.
  - Add at the top of `ProjectSYNCS/CHANGELOG.md`, below the intro paragraph:

```markdown
## 7.6.0 — <today, yyyy-mm-dd>

- She now counts messages per hour and per channel, time spent in each voice room, and how often
  each of her commands is used.
- New `/report` subcommands for staff: `activity` (peak hours, busiest and silent channels, voice
  rooms), `commands` (most used, errors, never used), `plynlings` (births, deaths, health, events,
  bonds), `quiz` (found rate by category and difficulty, unanswered questions), `emotes` (the
  server's favourites, forgotten emotes, trends) and `sleeping` (members silent for 30, 60 or 90
  days).
- The monthly report she sends her creator now includes activity and commands.
```

- [ ] **Step 2: Update the README.**
  - In the command table row `| \`/report sessions · polls · community · syncs\` | … |`, list all ten subcommands.
  - In *Staff & owner*, under the `/report` bullet, add one sub-bullet per new report, in the same style as the existing four, describing what each shows (see the spec's table).
  - Add « activity and commands » to the digest sentence.
  - Say that the activity and command counts start at 7.6.0.

- [ ] **Step 3: Update `docs/agents/admin.md`**, section *`/report` and the monthly digest*. Add:
  - **The batched counters**: `ActivityCounter`, a singleton holding its own lock, synchronous, fed by the message fan-out after the shortcut return, by `SlashCommandExecuted`, by the shortcut service and by the voice sweep. `ActivityFlushService`, its own 5-minute loop, has a per-guild `try`, `Restore` on failure and a flush on stop. Why: a per-message write is a Pi risk. The three tables are buckets only, from 7.6.0. `Command` keys are append-only; a renamed command keeps its old rows. Excluded channels are counted and marked 🚫. Voice is presence (muted included), unlike `MemberDailyStat.VoiceMinutes`.
  - **The six new reports**, one line each, with their rules: a quiz round needs `MessageId != 0`; the Plynling mascot is excluded; abandons can't be reported; emotes are « own » by the live guild list; « sleeping » uses the 2026-08-09 reliability date and has its own `report:sleep:{days}` verb; the guild snapshot is empty when the guild is unavailable.
  - **The digest** sends `ReportKinds.Digest` (six reports), not every `ReportKind`.

- [ ] **Step 4: Update `CLAUDE.md`.**
  - « Eleven `BackgroundService` loops » becomes « Twelve ». Add `ActivityFlushService` (5 min, writes `ActivityCounter`'s counts; see admin) to that list.
  - Under *Shared helpers* (or *Reliability*), add one line: high-frequency counters go through `ActivityCounter`, in memory and flushed, never a database write per event.
  - Next to `MemberFlowDailyStat` in *Totals + daily buckets*, add `ActivityHourStat`, `ChannelDailyStat` and `CommandDailyStat` (buckets only, on purpose).
  - In the read-first table row for `/report`, add « activity counting ».

- [ ] **Step 5: Check `/help`.** The `/report` line in `HelpModule` is generic (« Mes rapports : sessions, sondages, vie du serveur, et moi »). Leave it unless the owner asks. Run the checks: the existing `/help` measurement must still pass.

- [ ] **Step 6: Build and run the checks.** Run `dotnet build ProjectSYNCS/ProjectSYNCS.csproj -warnaserror`, then `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: build clean, `ALL CHECKS PASSED`, and the report-embed summary lines printed.

- [ ] **Step 7: Live run in the dev guild.** This needs the owner's `Discord:Token` and `Discord:DevelopmentGuildId` user secrets. If you are in a cloud session without them, skip this step and say so in the summary.
  - Start with `cd ProjectSYNCS && dotnet run`.
  - Write in a channel and in a thread, run two slash commands and `;work`, and sit in a voice channel for 2 minutes.
  - Wait 5 minutes for the flush, or stop the bot with Ctrl+C, which flushes, and restart it.
  - Run each new `/report` subcommand as staff and click 7 / 30 / all.
  - Click the 30 / 60 / 90 buttons of `sleeping`.
  - Run `/debug digest`. Expected: an opening line plus 6 reports in DMs.
  - A non-staff account must be refused.

- [ ] **Step 8: Commit** (only if the owner asked): `git add -A && git commit -m "/report batch 2 docs; version 7.6.0"`. Don't push unless asked. A push to `main` touching `ProjectSYNCS/` publishes the add-on image.
