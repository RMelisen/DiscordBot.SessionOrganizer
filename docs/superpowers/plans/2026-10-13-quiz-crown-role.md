# Grosse tête de la semaine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every Monday 10:00 Paris, SYNCS gives a managed role to last calendar week's best quiz answerer(s) and announces it in the quiz channel, pinging them.

**Architecture:** The 6.11.0 Plynling-role mechanics are extracted into a shared `RoleMirror` engine (`Services/Core`) with generic rules (`Helpers/Core/RoleMirrorRules`). `PlynlingRoleService` and a new `QuizCrownService` each supply "who should hold the role". The crown runs inside `QuizMasterService`'s 1-minute tick; pure week/ranking logic lives in `Helpers/Quiz/QuizCrown`.

**Tech Stack:** .NET 10, Discord.Net 3.20.1, EF Core 10 + SQLite.

**Spec:** `docs/superpowers/specs/2026-10-13-quiz-crown-role-design.md` (and, for the inherited behaviour, `2026-10-13-plynling-role-design.md`).

## Global Constraints

- Root `CLAUDE.md`, `docs/agents/quiz.md`, `docs/agents/plynling.md`, `docs/agents/admin.md` and `docs/syncs-voice.md` bind.
- Flat namespaces whatever the folder (`ProjectSYNCS.Helpers`, `.Services`, `.Commands`, `.Models`).
- English names/code/logs; French user strings. Never gender the reader. No `(｡•́︿•̀｡)`. Sparkle only via `{Emotes.Sparkle}`.
- Singletons take `IServiceProvider` and scope per unit of work; never inject a DB service.
- Nothing may escape a hosted loop; Discord side effects are swallowed and logged.
- `GuildConfigService` stays the only writer of `GuildSettings`.
- The Plynling role must behave exactly as in 6.11.0 after the refactor.
- No test project: verification = `dotnet build ProjectSYNCS/ProjectSYNCS.csproj -warnaserror` + the scratchpad console `rolecheck` (outside the repo) + the owner's dev-guild run.
- Version 6.11.0 → **6.12.0**, one CHANGELOG entry. The owner asked for commit + merge at the end.

## Review Focus

1. **Monday 09:59 vs 10:00 and the DST Sunday (2026-10-25).** `RewardedWeekStart` must switch exactly at 10:00 Paris and never skip/duplicate a week across DST. → Task 3 scratch checks.
2. **Restart on Monday after the marker is saved but before the post.** Must not re-announce (marker saved first) and must still give the role. → Task 4 order: save, mirror, announce.
3. **A role configured mid-week.** Goes silently to last week's champion; Monday still announces. → Task 5 first sync uses the champion loader; marker untouched.
4. **Shared crown mentions.** All champions pinged, nobody else; the joined text stays short. → Task 4 `AllowedMentions { UserIds }`, `JoinMentions`.
5. **Refactor regression on the Plynling role** (audit reasons, logged-once, cap, snapshot order). → Task 2 keeps the code path byte-for-byte except the kind parameter; scratch re-runs `RoleMirrorRules` checks.

---

### Task 1: Data — `QuizRoleId`, `QuizCrownedWeek`

**Files:** `ProjectSYNCS/Models/Admin/GuildSettings.cs`, `ProjectSYNCS/Data/AppDbContext.cs`, `ProjectSYNCS/Services/Admin/GuildConfigService.cs`, generated migration `AddQuizCrown`.

**Produces:** `GuildConfig.QuizRoleId` (`ulong`), `GuildConfig.QuizCrownedWeek` (`int`) appended after `PlynlingRoleId`; `GuildConfigService.SetQuizRoleAsync(ulong guildId, ulong roleId)`, `SetQuizCrownedWeekAsync(ulong guildId, int weekStart)`.

- [ ] Append to `GuildSettings`:

```csharp
    /// <summary>
    /// The « Grosse tête de la semaine » role: held by last calendar week's best quiz answerer(s).
    /// Zero means none, and the whole crown is off (QuizCrownService).
    /// </summary>
    public ulong QuizRoleId { get; set; }

    /// <summary>
    /// The Monday (yyyymmdd, AppTime.DayKey) starting the last week whose crown was announced.
    /// Saved before the announcement, so a restart never announces twice. Zero means never.
    /// </summary>
    public int QuizCrownedWeek { get; set; }
```

- [ ] `AppDbContext`, in the `GuildSettings` block: `e.Property(x => x.QuizRoleId).HasConversion<long>();`
- [ ] `GuildConfig` record: append `ulong QuizRoleId, int QuizCrownedWeek`; `Empty` gets `, 0, 0`; `GetAsync` passes `settings?.QuizRoleId ?? 0, settings?.QuizCrownedWeek ?? 0`.
- [ ] Setters after `SetPlynlingRoleAsync`:

```csharp
    /// <summary>Sets the Grosse tête role, or turns the crown off with zero.</summary>
    public Task SetQuizRoleAsync(ulong guildId, ulong roleId) =>
        UpdateSettingsAsync(guildId, s => s.QuizRoleId = roleId);

    /// <summary>Records the week whose crown was just announced (QuizCrownService).</summary>
    public Task SetQuizCrownedWeekAsync(ulong guildId, int weekStart) =>
        UpdateSettingsAsync(guildId, s => s.QuizCrownedWeek = weekStart);
```

- [ ] `dotnet ef migrations add AddQuizCrown` (from `ProjectSYNCS/`). Expected: two `AddColumn` (`QuizRoleId` long, `QuizCrownedWeek` int, default 0), nothing else.
- [ ] Build `-warnaserror`. Expected: 0 warnings, 0 errors.

---

### Task 2: Extract the shared engine

**Files:**
- Move+rename: `Helpers/Plynling/PlynlingRoleSync.cs` → `Helpers/Core/RoleMirrorRules.cs`
- Create: `Services/Core/RoleMirror.cs`
- Rewrite: `Services/Plynling/PlynlingRoleService.cs`
- Modify: `Commands/Admin/ConfigModule.cs` (shared static helpers; `plynling-role` uses them), `Program.cs`
- Scratch: `rolecheck/Program.cs` (rename references)

**Produces:**
- `RoleMirrorRules.Refusal(RoleFacts, BotFacts, bool adopting, string command)`; `Diff`, `Counts`, `MaxRemovalsPerPass`, `NoManageRoles` unchanged.
- `record RoleKind(string Key, string Command, string GrantedReason, string RevokedReason)` with `RoleKind.Plynling`, `RoleKind.QuizCrown`.
- `RoleMirror`: `static Problem(SocketGuild, SocketRole?, bool adopting, RoleKind)`, `MirrorGuildAsync(SocketGuild, RoleKind, ulong roleId, Func<Task<HashSet<ulong>>> loadHolders)`, `MirrorMemberAsync(SocketGuild, RoleKind, ulong roleId, ulong userId, bool shouldHold)`, `PreviewAsync(SocketGuild, ulong roleId, Func<Task<HashSet<ulong>>> loadHolders)`, `StartMirror(TimeSpan delay, Func<Task> pass)`.
- `PlynlingRoleService`: `DefaultName`, `LoadHoldersAsync(ulong guildId)`, `SyncGuildAsync(SocketGuild)`, `SyncMemberAsync(SocketGuild, ulong)` (signatures callers use are unchanged).
- `ConfigModule` statics: `CreateManagedRoleAsync`, `SetManagedRoleAsync`, `ClearManagedRoleAsync` (below).

- [ ] `git mv` the helper to `Helpers/Core/RoleMirrorRules.cs`; rename the class to `RoleMirrorRules`; drop `DefaultName`; header comment generalised (« a role SYNCS mirrors: Plynling owners, the quiz crown »); `Refusal` gains `string command` and its first message becomes
  ``$"Ce rôle n'existe plus. Recrée-le avec `/config {command} create`, ou choisis-en un autre avec `set`."``; the permission refusal's tail stays « …ou laisse-moi le créer avec `create`. »
- [ ] `Services/Core/RoleMirror.cs`: the body of 6.11.0's `PlynlingRoleService` minus the Plynling specifics — pass gate, logged-once set keyed by `(ulong GuildId, string Key)`, `Problem` building `RoleFacts`/`BotFacts` and calling `Refusal(..., kind.Command)`, `MirrorGuildAsync` = old `SyncGuildCoreAsync` with `loadHolders()` in place of `LoadOwnersAsync` and `RequestOptions` built from `kind` (static per-kind instances are fine), `MirrorMemberAsync` = old `SyncMemberAsync` minus the DB read (`shouldHold` passed in), `PreviewAsync` = old preview with `loadHolders`, `StartMirror` = `_ = Task.Run(async () => { await Task.Delay(delay); await pass(); })`. Constructor takes only `ILogger<RoleMirror>`. Log messages say « {Kind} role » using `kind.Key`.
- [ ] `RoleKind` in the same file:

```csharp
public sealed record RoleKind(string Key, string Command, string GrantedReason, string RevokedReason)
{
    public static readonly RoleKind Plynling = new("plynling", "plynling-role", "Plynling vivant", "Plus de Plynling");
    public static readonly RoleKind QuizCrown = new("quiz", "quiz-role", "Grosse tête de la semaine", "Couronne passée");
}
```

- [ ] `PlynlingRoleService` becomes:

```csharp
public sealed class PlynlingRoleService
{
    public const string DefaultName = "Maîtres de Plynlings";

    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly RoleMirror _mirror;
    private readonly ILogger<PlynlingRoleService> _logger;

    // ctor assigns all four

    public async Task<HashSet<ulong>> LoadHoldersAsync(ulong guildId)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PlynlingService>()
            .GetLivingOwnerIdsAsync(guildId, DateTimeOffset.UtcNow);
    }

    public async Task SyncGuildAsync(SocketGuild guild)
    {
        var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
        if (roleId == 0) return;
        await _mirror.MirrorGuildAsync(guild, RoleKind.Plynling, roleId, () => LoadHoldersAsync(guild.Id));
    }

    public async Task SyncMemberAsync(SocketGuild guild, ulong userId)
    {
        try
        {
            var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
            if (roleId == 0) return;
            bool owns;
            await using (var scope = _services.CreateAsyncScope())
                owns = await scope.ServiceProvider.GetRequiredService<PlynlingService>()
                    .HasLivingAsync(guild.Id, userId, DateTimeOffset.UtcNow);
            await _mirror.MirrorMemberAsync(guild, RoleKind.Plynling, roleId, userId, owns);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plynling role update failed for {UserId} in guild {GuildId}; the hourly pass will retry.",
                userId, guild.Id);
        }
    }
}
```

  (`SyncGuildAsync` must never throw: `GuildConfigService.GetAsync` never throws and `MirrorGuildAsync` catches everything — keep it that way.)
- [ ] `Program.cs`: register `services.AddSingleton<RoleMirror>();` before `PlynlingRoleService`.
- [ ] `ConfigModule`: move the bodies of `PlynlingRoleModule.CreateAsync/SetAsync/ClearAsync` into private static helpers on the outer class, parameterised:

```csharp
    // The two mirrored roles (Plynling, quiz crown) share one create / set / clear, so they can't drift.
    private static async Task CreateManagedRoleAsync(SocketInteractionContext context, ILogger logger,
        RoleMirror mirror, RoleKind kind, string? name, string defaultName, ulong currentId,
        Func<ulong, Task> store, Func<Task<HashSet<ulong>>> loadHolders, Func<Task> firstPass, string doneTail)
    private static async Task SetManagedRoleAsync(SocketInteractionContext context, RoleMirror mirror,
        RoleKind kind, IRole role, Func<ulong, Task> store, Func<Task<HashSet<ulong>>> loadHolders, Func<Task> firstPass, string doneLead)
    private static async Task ClearManagedRoleAsync(SocketInteractionContext context, Func<ulong, Task> store, string done)
```

  `CreateManagedRoleAsync` keeps every 6.11.0 behaviour: existing-role refusal (« Il y a déjà un rôle : <@&id>. `/config {kind.Command} clear` d'abord… »), `NoManageRoles`, `CreateRoleAsync` (`GuildPermissions.None`, mentionable, audit « /config {kind.Command} create »), both catches, store-or-delete-orphan, preview via `mirror.PreviewAsync`, reply, `mirror.StartMirror(FirstSyncDelay, firstPass)`. `SetManagedRoleAsync`: `RoleMirror.Problem(..., adopting: true, kind)`, store, preview, reply, `StartMirror(TimeSpan.Zero, firstPass)`. `PlynlingRoleModule` handlers become one call each, passing `RoleKind.Plynling`, `PlynlingRoleService.DefaultName`, `_config.SetPlynlingRoleAsync`, `_roles.LoadHoldersAsync`, `() => _roles.SyncGuildAsync(Context.Guild)`, and the 6.11.0 texts. `ShowAsync` calls `RoleMirror.Problem(..., adopting: false, RoleKind.Plynling)`.
- [ ] Scratch `rolecheck/Program.cs`: `PlynlingRoleSync` → `RoleMirrorRules`, every `Refusal(...)` call gains `, "plynling-role"`; add `Check(RoleMirrorRules.Refusal(RoleFacts.Missing, bot, false, "quiz-role")!.Contains("/config quiz-role create"), "missing-role text names the command");`. Run: all `ok`.
- [ ] Grep: `grep -rn "PlynlingRoleSync\|PlynlingRoleService.Problem\|_roles.PreviewAsync\|_roles.StartSync" ProjectSYNCS docs/agents` → only docs hits (fixed in Task 6).
- [ ] Build `-warnaserror`: 0/0.

---

### Task 3: Pure crown rules — `Helpers/Quiz/QuizCrown`

**Files:** Create `ProjectSYNCS/Helpers/Quiz/QuizCrown.cs`; scratch checks.

**Produces:** `QuizCrown.CrownHour` (10), `RewardedWeekStart(DateTimeOffset now) : int`, `WeekRange(int weekStart) : (int From, int To)`, `record CrownEntry(ulong UserId, int Wins, long Cailloux, double AvgMs)`, `record CrownWin(ulong WinnerId, long Reward, DateTimeOffset PostedAt, DateTimeOffset WonAt)`, `Champions(IEnumerable<CrownWin>) : IReadOnlyList<CrownEntry>`, `WinsText(int wins) : string`, `JoinMentions(IReadOnlyList<ulong>) : string`.

- [ ] Create:

```csharp
namespace ProjectSYNCS.Helpers;

/// <summary>One won round, as the crown needs it.</summary>
public readonly record struct CrownWin(ulong WinnerId, long Reward, DateTimeOffset PostedAt, DateTimeOffset WonAt);

/// <summary>One answerer's week.</summary>
public sealed record CrownEntry(ulong UserId, int Wins, long Cailloux, double AvgMs);

// « Grosse tête de la semaine »: which week is being rewarded right now, and who won it. Pure, so the
// Monday 10:00 edge and the DST Sunday are checkable without a clock or a gateway.
public static class QuizCrown
{
    /// <summary>Monday, this hour Paris: the crown changes hands (after the morning hello, before the first quiz slot).</summary>
    public const int CrownHour = 10;

    /// <summary>
    /// The DayKey of the Monday starting the week whose champion reigns at <paramref name="now"/>:
    /// from Monday 10:00 Paris, the week that just ended; before that, the one before it.
    /// Date arithmetic on the Paris calendar date only, so DST never skips or doubles a week.
    /// </summary>
    public static int RewardedWeekStart(DateTimeOffset now)
    {
        var paris = AppTime.ToZoned(now);
        var date = paris.Date;
        int sinceMonday = ((int)date.DayOfWeek + 6) % 7;          // Monday = 0
        var thisMonday = date.AddDays(-sinceMonday);
        bool crownedYet = sinceMonday > 0 || paris.Hour >= CrownHour;
        var rewarded = thisMonday.AddDays(crownedYet ? -7 : -14);
        return rewarded.Year * 10000 + rewarded.Month * 100 + rewarded.Day;
    }

    /// <summary>The week's first and last DayKeys (Monday .. Sunday).</summary>
    public static (int From, int To) WeekRange(int weekStart)
    {
        var sunday = AppTime.FromDayKey(weekStart).AddDays(6);
        return (weekStart, sunday.Year * 10000 + sunday.Month * 100 + sunday.Day);
    }

    /// <summary>
    /// Everyone tied for first: most wins, then most cailloux (harder questions pay more), then the
    /// fastest average answer. Empty when nobody won anything.
    /// </summary>
    public static IReadOnlyList<CrownEntry> Champions(IEnumerable<CrownWin> wins)
    {
        var ranked = wins
            .GroupBy(w => w.WinnerId)
            .Select(g => new CrownEntry(g.Key, g.Count(), g.Sum(w => w.Reward),
                g.Average(w => (w.WonAt - w.PostedAt).TotalMilliseconds)))
            .OrderByDescending(e => e.Wins).ThenByDescending(e => e.Cailloux).ThenBy(e => e.AvgMs)
            .ToList();
        if (ranked.Count == 0) return ranked;
        var top = ranked[0];
        return ranked.TakeWhile(e => e.Wins == top.Wins && e.Cailloux == top.Cailloux && e.AvgMs == top.AvgMs).ToList();
    }

    /// <summary>« 1 bonne réponse » / « 5 bonnes réponses ».</summary>
    public static string WinsText(int wins) => wins == 1 ? "1 bonne réponse" : $"{wins} bonnes réponses";

    /// <summary>« <@a> », « <@a> et <@b> », « <@a>, <@b> et <@c> ».</summary>
    public static string JoinMentions(IReadOnlyList<ulong> ids)
    {
        var m = ids.Select(id => $"<@{id}>").ToList();
        return m.Count <= 1 ? string.Concat(m) : string.Join(", ", m.Take(m.Count - 1)) + " et " + m[^1];
    }
}
```

- [ ] Scratch checks (append to `rolecheck/Program.cs`), using Paris wall clocks via `AppTime.AtWallClock(new DateTime(...))`:
  - Mon 2026-10-12 09:59 → **20260928** (week before last, not crowned yet); Mon 2026-10-12 10:00 → **20261005**; Sun 2026-10-18 23:59 → 20261005; Tue 2026-10-13 08:00 → 20261005.
  - DST: Sun 2026-10-25 12:00 → 20261012; Mon 2026-10-26 10:00 → **20261019**; Mon 2026-10-26 09:00 → 20261012.
  - `WeekRange(20261228)` → (20261228, 20270103) (year boundary).
  - `Champions`: more wins beats more cailloux; equal wins → more cailloux wins; equal wins+cailloux → faster average; perfect tie → both; empty → empty.
  - `WinsText(1)`, `WinsText(5)`; `JoinMentions` for 1, 2, 3 ids.
  Run: all `ok`.
- [ ] Build `-warnaserror`: 0/0.

---

### Task 4: `QuizCrownService`, lines, the tick

**Files:** Create `Services/Quiz/QuizCrownService.cs`; modify `Services/Quiz/QuizService.cs` (week read), `Services/Personality/BotResponses.cs` (three pools + index comment), `Services/Quiz/QuizMasterService.cs` (tick call), `Program.cs`.

**Consumes:** Task 1 config, Task 2 `RoleMirror`/`RoleKind.QuizCrown`, Task 3 `QuizCrown`.
**Produces:** `QuizService.GetWeekWinsAsync(ulong guildId, int fromDay, int toDay) : Task<List<CrownWin>>`; `QuizCrownService.DefaultName`, `LoadHoldersAsync(ulong guildId)`, `TickAsync(SocketGuild, DateTimeOffset)`, `CrownNowAsync(SocketGuild) : Task<string>`.

- [ ] `QuizService`:

```csharp
    // The week's won rounds for the crown. Day is an int key, so the range filters in SQL.
    public async Task<List<CrownWin>> GetWeekWinsAsync(ulong guildId, int fromDay, int toDay)
    {
        var rows = await _db.QuizRounds   // use the field name QuizService already has
            .Where(r => r.GuildId == guildId && r.Day >= fromDay && r.Day <= toDay && r.WinnerId != 0 && r.WonAt != null)
            .Select(r => new { r.WinnerId, r.Reward, r.PostedAt, r.WonAt })
            .ToListAsync();
        return rows.Select(r => new CrownWin(r.WinnerId, r.Reward, r.PostedAt, r.WonAt!.Value)).ToList();
    }
```

- [ ] `BotResponses`: after `QuizTimeoutLines`, three pools (≈ 8 / 4 / 6 lines), in her voice per `docs/syncs-voice.md` — proprietary warmth with a pinch for others (her quiz, her logs, the server's habits), adoration for Papa; invariant gender; one tag max; `{0}`/`{1}` only:
  - `QuizCrownLines` — `{0}` mention, `{1}` `WinsText`.
  - `QuizCrownSharedLines` — `{0}` `JoinMentions`.
  - `QuizCrownOwnerLines` — `{0}` mention, `{1}` `WinsText`.
  Add them to the index comment under the quiz lines.
- [ ] `QuizCrownService` (singleton):

```csharp
public sealed class QuizCrownService
{
    public const string DefaultName = "Grosse tête de la semaine";

    // Weekly, so ResponsePicker's history would never survive to help: DailyRotation by the week's day number.
    private const ulong CrownSalt = 0x5359_4E43_5343_5257; // "SYNCSCRW"
    private static readonly TimeSpan MirrorEvery = TimeSpan.FromHours(1);

    // The hourly mirror between Mondays; per guild, its own gate (never shared).
    private readonly CooldownGate<ulong> _mirrorGate = new(MirrorEvery);
    // ctor: DiscordSocketClient? no — guild is passed in. IServiceProvider, GuildConfigService, RoleMirror, ILogger.

    public async Task<HashSet<ulong>> LoadHoldersAsync(ulong guildId) =>
        (await LoadChampionsAsync(guildId, QuizCrown.RewardedWeekStart(DateTimeOffset.UtcNow))).Select(e => e.UserId).ToHashSet();

    private async Task<IReadOnlyList<CrownEntry>> LoadChampionsAsync(ulong guildId, int weekStart)
    {
        var (from, to) = QuizCrown.WeekRange(weekStart);
        await using var scope = _services.CreateAsyncScope();
        var wins = await scope.ServiceProvider.GetRequiredService<QuizService>().GetWeekWinsAsync(guildId, from, to);
        return QuizCrown.Champions(wins);
    }

    // Called from QuizMasterService's tick, inside its per-guild try.
    public async Task TickAsync(SocketGuild guild, DateTimeOffset now)
    {
        var config = await _config.GetAsync(guild.Id);
        if (config.QuizRoleId == 0) return;                       // off until staff set a role

        int week = QuizCrown.RewardedWeekStart(now);
        if (week > config.QuizCrownedWeek)
        {
            // Saved first: a crash or a failed post after this is logged, never announced twice.
            await _config.SetQuizCrownedWeekAsync(guild.Id, week);
            await CrownAsync(guild, config, week);
            return;
        }
        if (_mirrorGate.TryClaim(guild.Id))
            await _mirror.MirrorGuildAsync(guild, RoleKind.QuizCrown, config.QuizRoleId, () => LoadHoldersAsync(guild.Id));
    }

    // /debug quiz action:Crown — the last finished week, now, marker untouched.
    public async Task<string> CrownNowAsync(SocketGuild guild)
    {
        var config = await _config.GetAsync(guild.Id);
        if (config.QuizRoleId == 0) return "Pas de rôle Grosse tête ici : `/config quiz-role create` d'abord.";
        int week = QuizCrown.RewardedWeekStart(DateTimeOffset.UtcNow);
        var champions = await CrownAsync(guild, config, week);
        return champions.Count == 0
            ? $"Aucune bonne réponse la semaine du {week}. Personne n'a le rôle."
            : $"Couronné pour la semaine du {week} : {QuizCrown.JoinMentions(champions.Select(c => c.UserId).ToList())}.";
    }

    private async Task<IReadOnlyList<CrownEntry>> CrownAsync(SocketGuild guild, GuildConfig config, int week)
    {
        var champions = await LoadChampionsAsync(guild.Id, week);
        var ids = champions.Select(c => c.UserId).ToHashSet();
        await _mirror.MirrorGuildAsync(guild, RoleKind.QuizCrown, config.QuizRoleId, () => Task.FromResult(ids));
        if (champions.Count > 0) await AnnounceAsync(guild, config, champions, week);
        return champions;
    }

    private async Task AnnounceAsync(SocketGuild guild, GuildConfig config, IReadOnlyList<CrownEntry> champions, int week)
    {
        if (config.QuizChannelId == 0 || guild.GetTextChannel(config.QuizChannelId) is not { } channel) return;
        int day = AppTime.DayNumber(AppTime.AtWallClock(AppTime.FromDayKey(week)));
        string line = champions.Count > 1
            ? string.Format(DailyRotation.Pick(BotResponses.QuizCrownSharedLines, day, CrownSalt),
                QuizCrown.JoinMentions(champions.Select(c => c.UserId).ToList()))
            : string.Format(DailyRotation.Pick(champions[0].UserId == AvailabilityService.OwnerId
                    ? BotResponses.QuizCrownOwnerLines : BotResponses.QuizCrownLines, day, CrownSalt),
                $"<@{champions[0].UserId}>", QuizCrown.WinsText(champions[0].Wins));
        // Meant to ping — the champions and nobody else (never the role, never everyone).
        var mentions = new AllowedMentions { UserIds = champions.Select(c => c.UserId).ToList() };
        await BotChat.PostWithTypingAsync(channel, line, _logger, "quiz crown", mentions);
    }
}
```

  Adapt to the real APIs as you go (the `CooldownGate` constructor/claim names, `QuizService`'s DbContext field, `AppTime.DayNumber`'s parameter) — read them, don't guess.
- [ ] `QuizMasterService.TickAsync`, in the existing `foreach (var guild in _client.Guilds)` body, after `PostIfDueAsync` and inside the same `try` — **no**: give it its own `try` right after, so a crown failure can't be logged as "Quiz scheduling failed" nor skip the next guild's quiz:

```csharp
            try
            {
                await _crown.TickAsync(guild, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quiz crown failed in guild {GuildId}.", guild.Id);
            }
```

  `QuizMasterService` takes `QuizCrownService crown` in its constructor.
- [ ] `Program.cs`: `services.AddSingleton<QuizCrownService>();` before `QuizMasterService`.
- [ ] Build `-warnaserror`: 0/0.

---

### Task 5: `/config quiz-role`, `/config show`, `/debug quiz action:Crown`

**Files:** `Commands/Admin/ConfigModule.cs`, `Commands/Admin/ConfigCards.cs`, `Commands/Admin/DebugModule.cs`.

- [ ] `ConfigModule`: new `[Group("quiz-role", "Le rôle de la grosse tête du quiz de la semaine")] QuizRoleModule` (ctor: `GuildConfigService`, `QuizCrownService`, `RoleMirror`, `ILogger<QuizRoleModule>`), three handlers over the Task 2 helpers with `RoleKind.QuizCrown`, `QuizCrownService.DefaultName`, `_config.SetQuizRoleAsync`, `_crown.LoadHoldersAsync`, first pass `() => _mirror.MirrorGuildAsync(Context.Guild, RoleKind.QuizCrown, roleId, () => _crown.LoadHoldersAsync(Context.Guild.Id))` (roleId from the stored value — the helper passes it), descriptions:
  - `create`: « Créer le rôle de la grosse tête de la semaine et le gérer »; option `name` « Le nom du rôle (par défaut : Grosse tête de la semaine) », `[MaxLength(InputCaps.RoleName)]`; done tail « Chaque lundi à 10 h, je le donne à la meilleure tête du quiz de la semaine passée ദ്ദി◝ ⩊ ◜.ᐟ »
  - `set`: « Gérer un rôle existant comme rôle de la grosse tête »; done lead « est maintenant le rôle de la grosse tête. »
  - `clear`: « Ne plus gérer de rôle de la grosse tête »; text « Plus de grosse tête : je ne couronne plus personne. Le rôle et ses membres restent tels quels : supprime-le dans Discord si tu n'en veux plus (ᵕ • ᴗ •) »
  If the helper signature from Task 2 can't carry the role id into the first pass cleanly, make `firstPass` a `Func<ulong, Task>` taking the stored role id — ledger the ruling.
- [ ] `ShowAsync`: compute `quizRoleProblem` like the Plynling one with `RoleKind.QuizCrown`; `ConfigCards.Build(..., plynlingRoleProblem, quizRoleProblem)`.
- [ ] `ConfigCards`: new parameter `string? quizRoleProblem`; a section after « Rôle Plynling »: `"**Rôle Grosse tête** — la meilleure tête du quiz de la semaine passée, couronnée le lundi"`, same value shape; header count `9 × … = 20 of the 40`.
- [ ] `DebugModule`: `[ChoiceDisplay("Couronner la grosse tête")] Crown` appended to `QuizAction`; `DebugModule` takes `QuizCrownService`; the handler dispatches `Crown` to `_crown.CrownNowAsync(Context.Guild)`; update the summary text to « Poser une question, fermer celle en cours, ou couronner la grosse tête ». Append-only enum (not stored, but keep order).
- [ ] Count `/config` top-level: `show` + 9 subgroups = 10 ≤ 25.
- [ ] Build `-warnaserror`: 0/0.

---

### Task 6: Docs, help, version

- [ ] `docs/agents/quiz.md`: add `QuizCrownService` / `QuizCrown` rows to the table and a `## Grosse tête de la semaine` section: Monday 10:00 Paris, calendar week from `QuizRound` (`Day` range in SQL), ranking (wins → cailloux → avg time → shared), marker `QuizCrownedWeek` saved before mirror+post, hourly mirror via `RoleMirror`, off until `quiz-role` set, announcement pings only champions, lines via `DailyRotation` (weekly), `/debug quiz action:Crown` ignores the marker. Mention it in the `/debug quiz` section.
- [ ] `docs/agents/plynling.md` *The Plynling role*: mechanics now live in `RoleMirror` (`Services/Core`), rules in `Helpers/Core/RoleMirrorRules`; replace the `PlynlingRoleSync` references.
- [ ] `docs/agents/admin.md`: `quiz-role create|set|clear` in the list; the "owned, not configured" bullet covers both roles and says they share one create/set/clear.
- [ ] Root `CLAUDE.md`: `QuizMasterService` (1 min, see quiz) — no new loop; no change unless wording names what the tick does (it doesn't). Confirm.
- [ ] `HelpModule`: `/config` line — measure; target wording « …le salon principal et les rôles Plynling et Grosse tête. » Measure with the scratch console: ≤ 6000 total, ≤ 1024 field; if over, shorten this line only.
- [ ] `README.md`: `quiz-role create|set|clear` bullet under `/config`; one sentence in the quiz feature section (« Every Monday at 10:00 she crowns last week's best answerer… »).
- [ ] `config.yaml` → `6.12.0`; `CHANGELOG.md`:

```markdown
## 6.12.0 — 2026-10-13

- Grosse tête de la semaine : chaque lundi à 10 h, SYNCS donne un rôle à la meilleure tête du quiz
  de la semaine passée et l'annonce dans le salon du quiz (`/config quiz-role`).
```

- [ ] Build `-warnaserror`: 0/0.

---

### Task 7: Dev-guild verification (owner)

1. Startup: `AddQuizCrown` applied, commands registered without error.
2. `/config quiz-role create` → reply counts; last week's winner gets the role silently; no announcement.
3. `/debug quiz action:Crown` → announcement in the quiz channel pings only the winner; reply names the week.
4. Plynling role unchanged: `plynling-role create`/adopt/abandon/`/debug sweep` as in 6.11.0.
5. `/config show` shows both roles; moving the quiz role above hers flags it and logs once (separately from the Plynling role).
6. `/config quiz-role clear` → no more crown; nobody's roles change.
