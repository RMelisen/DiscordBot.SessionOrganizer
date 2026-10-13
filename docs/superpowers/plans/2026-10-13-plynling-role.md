# Plynling Role Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SYNCS manages one Discord role per guild that every member with a living Plynling holds and nobody else does.

**Architecture:** A pure helper (`PlynlingRoleSync`) owns the diff and the "can she manage this role?" rules. A singleton `PlynlingRoleService` applies them: immediately for one person after adopt / abandon / staff resurrect, and for the whole guild at the end of every `PlynlingSweepService` pass. Staff configure the role with `/config plynling-role create|set|clear`; the id lives in `GuildSettings.PlynlingRoleId`.

**Tech Stack:** .NET 10, Discord.Net 3.20.1, EF Core 10 + SQLite.

**Spec:** `docs/superpowers/specs/2026-10-13-plynling-role-design.md` — read it first.

## Global Constraints

- Read root `CLAUDE.md`, `docs/agents/plynling.md` and `docs/agents/admin.md` before touching code; their rules bind.
- Command and option names English; every user-facing string French; code, comments, logs English.
- Every file keeps the flat namespace (`ProjectSYNCS.Helpers`, `.Services`, `.Commands`, `.Models`) whatever its subfolder.
- Snowflakes are `ulong` with `.HasConversion<long>()`; zero means unset.
- `PlynlingRoleService` is a **singleton** and never injects a DB service: `IServiceProvider` + `CreateAsyncScope()` per unit of work.
- Discord side effects never break the flow: swallow and log. Nothing may escape the sweep's hosted loop.
- All `/config` replies go through `ConfigModule.SayAsync` (ephemeral, `AllowedMentions.None`).
- **No commits.** The owner commits by hand. Leave changes in the working tree.
- **No test project exists.** Verification = `dotnet build -warnaserror` (run from `ProjectSYNCS/`), a throwaway scratch console for the pure helper (Task 2, never added to the repo), and the dev-guild checklist (Task 7).
- Version: `ProjectSYNCS/config.yaml` **6.10.1 → 6.11.0** (minor: subcommands + migration), with a `CHANGELOG.md` entry. Bump once.

## Review Focus

Failure modes the spec implies that no build will catch — each has a check in the owning task:

1. **A brand-new role not yet in the gateway cache.** `create` gets a REST role back before the `GUILD_ROLE_CREATE` event lands; an immediate sync would see "role doesn't exist" and log a false problem. → Task 4 delays the first sync 3 s; Task 7 step 1 checks the log stays clean.
2. **`set` pointed at a populated role.** People without a Plynling lose it — capped at 25 removals per pass. → Task 2 scratch check covers the cap's ordering; Task 4's reply says when the cap applies; Task 7 step 5.
3. **Partial member cache after a reconnect.** Owners missing from `guild.Users` must never be treated as departed in a way that strips or skips wrongly. → Task 3 skips the pass when `!guild.HasAllMembers`; Task 2 scratch check covers "owner not in members".
4. **A death nobody has noticed.** The owner list must settle in memory so a starved Plynling's owner loses the role, without the read writing anything. → Task 3 `GetLivingOwnerIdsAsync` (AsNoTracking + `Settle`); Task 7 step 4.
5. **Role moved above hers / `ManageRoles` revoked after setup.** Must be reported on `/config show` and logged once, not every hour. → Task 3 logged-once set; Task 4 card line; Task 7 step 6.

---

## File Structure

| File | Change | Responsibility |
|---|---|---|
| `ProjectSYNCS/Models/Admin/GuildSettings.cs` | modify | `PlynlingRoleId` column |
| `ProjectSYNCS/Data/AppDbContext.cs` | modify | `HasConversion<long>()` for it |
| `ProjectSYNCS/Migrations/*_AddPlynlingRole.cs` | create (generated) | schema |
| `ProjectSYNCS/Services/Admin/GuildConfigService.cs` | modify | `GuildConfig.PlynlingRoleId`, `SetPlynlingRoleAsync` |
| `ProjectSYNCS/Helpers/Core/InputCaps.cs` | modify | `RoleName = 100` |
| `ProjectSYNCS/Helpers/Plynling/PlynlingRoleSync.cs` | create | pure diff, refusals, French counts |
| `ProjectSYNCS/Services/Plynling/PlynlingService.cs` | modify | read-only owner queries |
| `ProjectSYNCS/Services/Plynling/PlynlingRoleService.cs` | create | applies the sync |
| `ProjectSYNCS/Program.cs` | modify | register the singleton |
| `ProjectSYNCS/Commands/Admin/ConfigModule.cs` | modify | `plynling-role` subgroup, `show` passes the problem |
| `ProjectSYNCS/Commands/Admin/ConfigCards.cs` | modify | « Rôle Plynling » section |
| `ProjectSYNCS/Commands/Plynling/PlynlingModule.cs` | modify | hooks after adopt / abandon |
| `ProjectSYNCS/Commands/Plynling/PlynlingGroups.cs` | modify | constructor pass-through (two classes) |
| `ProjectSYNCS/Commands/Admin/AdminModule.cs` | modify | hook after resurrect |
| `ProjectSYNCS/Services/Plynling/PlynlingSweepService.cs` | modify | guild sync as the last step |
| `ProjectSYNCS/Commands/Core/HelpModule.cs`, `README.md`, `docs/agents/plynling.md`, `docs/agents/admin.md`, `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md` | modify | docs and version |

---

### Task 1: Store the role id

**Files:**
- Modify: `ProjectSYNCS/Models/Admin/GuildSettings.cs` (after `QuizChannelId`)
- Modify: `ProjectSYNCS/Data/AppDbContext.cs:203-209`
- Modify: `ProjectSYNCS/Services/Admin/GuildConfigService.cs`
- Create (generated): `ProjectSYNCS/Migrations/<timestamp>_AddPlynlingRole.cs` + `.Designer.cs`, updated `AppDbContextModelSnapshot.cs`

**Interfaces:**
- Produces: `GuildConfig.PlynlingRoleId` (`ulong`, last positional parameter); `GuildConfigService.SetPlynlingRoleAsync(ulong guildId, ulong roleId) : Task`.

- [ ] **Step 1: Add the column** — append to `GuildSettings` after `QuizChannelId`:

```csharp
    /// <summary>
    /// The role SYNCS keeps on everyone with a living Plynling, and off everyone else.
    /// Zero means none: no role is managed. She only adds and removes it — name, colour
    /// and position stay staff's (see PlynlingRoleService).
    /// </summary>
    public ulong PlynlingRoleId { get; set; }
```

- [ ] **Step 2: Map it** — in `AppDbContext.OnModelCreating`, inside `modelBuilder.Entity<GuildSettings>(e => { ... })`, after the `QuizChannelId` line:

```csharp
            e.Property(x => x.PlynlingRoleId).HasConversion<long>();
```

- [ ] **Step 3: Carry it in the cached snapshot** — in `GuildConfigService.cs`:

  Append the parameter to the record and to `Empty`:

```csharp
public sealed record GuildConfig(
    ulong ModeratorRoleId,
    ulong GameChannelId,
    ulong MainChannelId,
    IReadOnlySet<ulong> ExcludedChannels,
    IReadOnlySet<ulong> IdleChannels,
    IReadOnlySet<ulong> ShameVoters,
    ulong QuizChannelId,
    ulong PlynlingRoleId)
{
    /// <summary>What an unconfigured guild looks like — nothing set, nothing added.</summary>
    public static readonly GuildConfig Empty = new(0, 0, 0,
        new HashSet<ulong>(), new HashSet<ulong>(), new HashSet<ulong>(), 0, 0);
}
```

  In `GetAsync`, the `new GuildConfig(...)` call becomes:

```csharp
            loaded = new GuildConfig(
                settings?.ModeratorRoleId ?? 0,
                settings?.GameChannelId ?? 0,
                settings?.MainChannelId ?? 0,
                excluded.ToHashSet(), idle.ToHashSet(), voters.ToHashSet(),
                settings?.QuizChannelId ?? 0,
                settings?.PlynlingRoleId ?? 0);
```

  After `SetQuizChannelAsync`, add:

```csharp
    /// <summary>Sets the managed Plynling role, or stops managing one with zero.</summary>
    public Task SetPlynlingRoleAsync(ulong guildId, ulong roleId) =>
        UpdateSettingsAsync(guildId, s => s.PlynlingRoleId = roleId);
```

- [ ] **Step 4: Find every other `new GuildConfig(` call** — `grep -rn "new GuildConfig(" ProjectSYNCS`. Expected: only the two above. Fix any other by appending `, 0`.

- [ ] **Step 5: Generate the migration** — from `ProjectSYNCS/`:

```bash
dotnet ef migrations add AddPlynlingRole
```

  Expected: one `AddColumn<long>("PlynlingRoleId", "GuildSettings", nullable: false, defaultValue: 0L)` in `Up`, the matching `DropColumn` in `Down`, nothing else. If it contains anything else, stop and investigate (a pending model change from someone else).

- [ ] **Step 6: Build**

```bash
dotnet build -warnaserror
```

  Expected: `Build succeeded`, 0 warnings.

---

### Task 2: The pure rules — `PlynlingRoleSync`

**Files:**
- Modify: `ProjectSYNCS/Helpers/Core/InputCaps.cs`
- Create: `ProjectSYNCS/Helpers/Plynling/PlynlingRoleSync.cs`
- Scratch (never in the repo): `<scratchpad>/rolecheck/`

**Interfaces:**
- Produces:
  - `InputCaps.RoleName` (`const int` = 100)
  - `PlynlingRoleSync.DefaultName` (`const string`), `PlynlingRoleSync.MaxRemovalsPerPass` (`const int` = 25)
  - `record struct RoleFacts(bool Exists, bool IsEveryone, bool IsManaged, int Position, ulong RawPermissions)` with `static RoleFacts Missing`
  - `record struct BotFacts(bool CanManageRoles, int TopPosition)`
  - `PlynlingRoleSync.Refusal(RoleFacts role, BotFacts bot, bool adopting) : string?`
  - `PlynlingRoleSync.Diff(IReadOnlySet<ulong> owners, IReadOnlySet<ulong> holders, IReadOnlySet<ulong> members) : (IReadOnlyList<ulong> ToAdd, IReadOnlyList<ulong> ToRemove)`
  - `PlynlingRoleSync.Counts(int toAdd, int toRemove) : string`
  - `PlynlingRoleSync.NoManageRoles` (`const string`)

- [ ] **Step 1: Add the input cap** — append to `InputCaps`:

```csharp
    /// <summary>
    /// A role name (<c>/config plynling-role create</c>) — Discord's own limit for role names.
    /// </summary>
    public const int RoleName = 100;
```

- [ ] **Step 2: Write the helper** — create `ProjectSYNCS/Helpers/Plynling/PlynlingRoleSync.cs`:

```csharp
namespace ProjectSYNCS.Helpers;

/// <summary>What the sync needs to know about the configured role, as plain values.</summary>
public readonly record struct RoleFacts(bool Exists, bool IsEveryone, bool IsManaged, int Position, ulong RawPermissions)
{
    /// <summary>The configured id no longer resolves: the role was deleted in Discord.</summary>
    public static RoleFacts Missing => new(false, false, false, 0, 0);
}

/// <summary>What the sync needs to know about her own standing in the guild.</summary>
public readonly record struct BotFacts(bool CanManageRoles, int TopPosition);

// The Plynling role's rules, without a gateway: who should gain or lose it, and whether she can
// manage a given role at all. One Refusal backs `/config plynling-role set`, `/config show` and the
// sweep's skip check, so the three can never disagree about what is manageable.
//
// Strict mirror: holding the role means owning a living Plynling (frozen counts), nothing else.
public static class PlynlingRoleSync
{
    public const string DefaultName = "Maîtres de Plynlings";

    // A wrong owner list (a bug, a bad read) could otherwise strip the role from the whole guild in
    // one pass. Additions are uncapped: a wrong one is harmless here.
    public const int MaxRemovalsPerPass = 25;

    public const string NoManageRoles =
        "Il me manque la permission **Gérer les rôles** (Paramètres du serveur → Rôles → mon rôle).";

    /// <summary>
    /// Why she cannot manage <paramref name="role"/>, in French, or null when she can.
    /// <paramref name="adopting"/> is true for <c>set</c>: picking a role that carries
    /// permissions is refused, because the sync would then hand out and strip those permissions.
    /// The sync itself passes false — staff adding a permission to the role later is their call.
    /// </summary>
    public static string? Refusal(RoleFacts role, BotFacts bot, bool adopting)
    {
        if (!role.Exists)
            return "Ce rôle n'existe plus. Recrée-le avec `/config plynling-role create`, ou choisis-en un autre avec `set`.";
        if (role.IsEveryone)
            return "`@everyone`, sérieusement ? Choisis un vrai rôle (¬‿¬)";
        if (role.IsManaged)
            return "Ce rôle appartient à une intégration (un bot, les boosts…) : je ne peux pas le distribuer.";
        if (!bot.CanManageRoles)
            return NoManageRoles;
        if (role.Position >= bot.TopPosition)
            return "Ce rôle est au-dessus du mien, ou au même niveau : monte mon rôle plus haut dans la liste (Paramètres du serveur → Rôles).";
        if (adopting && role.RawPermissions != 0)
            return "Ce rôle donne des permissions. Le rôle Plynling est décoratif : choisis-en un sans aucune permission, ou laisse-moi le créer avec `create`.";
        return null;
    }

    /// <summary>
    /// Who gains and who loses the role. Owners who are not in the guild are skipped (never
    /// retried, never an error). Both lists sorted by id, so a capped pass is deterministic.
    /// </summary>
    public static (IReadOnlyList<ulong> ToAdd, IReadOnlyList<ulong> ToRemove) Diff(
        IReadOnlySet<ulong> owners, IReadOnlySet<ulong> holders, IReadOnlySet<ulong> members)
    {
        var toAdd = owners.Where(id => members.Contains(id) && !holders.Contains(id)).Order().ToList();
        var toRemove = holders.Where(id => !owners.Contains(id)).Order().ToList();
        return (toAdd, toRemove);
    }

    /// <summary>« 4 personnes l'obtiendront, 2 le perdront. » — what a first sync is about to do.</summary>
    public static string Counts(int toAdd, int toRemove)
    {
        if (toAdd == 0 && toRemove == 0) return "Tout le monde l'a déjà comme il faut.";

        var parts = new List<string>();
        if (toAdd > 0) parts.Add(toAdd == 1 ? "1 personne l'obtiendra" : $"{toAdd} personnes l'obtiendront");
        if (toRemove > 0) parts.Add(toRemove == 1 ? "1 personne le perdra" : $"{toRemove} personnes le perdront");
        var text = string.Join(", ", parts) + ".";
        if (toRemove > MaxRemovalsPerPass)
            text += $" Je retire au plus {MaxRemovalsPerPass} rôles par heure, donc ça prendra un moment.";
        return text;
    }
}
```

- [ ] **Step 3: Build**

```bash
dotnet build -warnaserror
```

  Expected: `Build succeeded`, 0 warnings.

- [ ] **Step 4: Scratch check (throwaway, outside the repo)** — in the session scratchpad, create `rolecheck/rolecheck.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\ProjectSYNCS.csproj" />
  </ItemGroup>
</Project>
```

  and `rolecheck/Program.cs`:

```csharp
using ProjectSYNCS.Helpers;

static void Check(bool ok, string what) { Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}"); if (!ok) Environment.ExitCode = 1; }
static HashSet<ulong> S(params ulong[] ids) => ids.ToHashSet();

// Diff
var (add, remove) = PlynlingRoleSync.Diff(owners: S(1, 2, 3), holders: S(2, 9), members: S(1, 2, 9));
Check(add.SequenceEqual(new ulong[] { 1 }), "adds owners in the guild who lack it; skips owner 3 who left");
Check(remove.SequenceEqual(new ulong[] { 9 }), "removes holders without a Plynling");
var (_, many) = PlynlingRoleSync.Diff(S(), S(30, 10, 20), S(10, 20, 30));
Check(many.SequenceEqual(new ulong[] { 10, 20, 30 }), "removals sorted by id (deterministic cap)");
var (a0, r0) = PlynlingRoleSync.Diff(S(5), S(5), S(5));
Check(a0.Count == 0 && r0.Count == 0, "in sync → nothing to do");

// Refusal
var bot = new BotFacts(CanManageRoles: true, TopPosition: 10);
var plain = new RoleFacts(true, false, false, Position: 3, RawPermissions: 0);
Check(PlynlingRoleSync.Refusal(plain, bot, adopting: true) is null, "plain role below hers is accepted");
Check(PlynlingRoleSync.Refusal(RoleFacts.Missing, bot, false) is not null, "deleted role refused");
Check(PlynlingRoleSync.Refusal(plain with { IsEveryone = true }, bot, true) is not null, "@everyone refused");
Check(PlynlingRoleSync.Refusal(plain with { IsManaged = true }, bot, true) is not null, "managed refused");
Check(PlynlingRoleSync.Refusal(plain, bot with { CanManageRoles = false }, false) == PlynlingRoleSync.NoManageRoles, "no ManageRoles refused");
Check(PlynlingRoleSync.Refusal(plain with { Position = 10 }, bot, false) is not null, "same position as hers refused");
Check(PlynlingRoleSync.Refusal(plain with { RawPermissions = 8 }, bot, adopting: true) is not null, "permission role refused on set");
Check(PlynlingRoleSync.Refusal(plain with { RawPermissions = 8 }, bot, adopting: false) is null, "permission added later: sync keeps working");

// Counts
Check(PlynlingRoleSync.Counts(1, 0) == "1 personne l'obtiendra.", "singular add");
Check(PlynlingRoleSync.Counts(4, 2) == "4 personnes l'obtiendront, 2 personnes le perdront.", "plural both");
Check(PlynlingRoleSync.Counts(0, 30).Contains("25"), "cap mentioned over 25 removals");
Check(PlynlingRoleSync.Counts(0, 0).Length > 0, "nothing to do still says something");
```

  Run:

```bash
dotnet run --project <scratchpad>/rolecheck
```

  Expected: every line `ok`, exit code 0. Fix the helper (not the check) on any `FAIL`. Keep this project around — Task 6 reuses it to measure `/help`.

---

### Task 3: `PlynlingRoleService` and the owner queries

**Files:**
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingService.cs` (next to `GetLivingAsync`, ~line 705)
- Create: `ProjectSYNCS/Services/Plynling/PlynlingRoleService.cs`
- Modify: `ProjectSYNCS/Program.cs` (near line 86, `services.AddSingleton<GuildConfigService>();`)

**Interfaces:**
- Consumes: `GuildConfigService.GetAsync(ulong) → GuildConfig` (`.PlynlingRoleId`), everything from Task 2.
- Produces:
  - `PlynlingService.GetLivingOwnerIdsAsync(ulong guildId, DateTimeOffset now) : Task<HashSet<ulong>>`
  - `PlynlingService.HasLivingAsync(ulong guildId, ulong ownerId, DateTimeOffset now) : Task<bool>`
  - `PlynlingRoleService.Problem(SocketGuild guild, SocketRole? role, bool adopting) : string?` (static)
  - `PlynlingRoleService.SyncGuildAsync(SocketGuild guild) : Task` — never throws
  - `PlynlingRoleService.SyncMemberAsync(SocketGuild guild, ulong userId) : Task` — never throws
  - `PlynlingRoleService.PreviewAsync(SocketGuild guild, ulong roleId) : Task<(int ToAdd, int ToRemove)>`
  - `PlynlingRoleService.StartSync(SocketGuild guild, TimeSpan delay) : void` — fire-and-forget

- [ ] **Step 1: Read-only owner queries** — add to `PlynlingService` after `GetLivingAsync`:

```csharp
    // Who owns a living Plynling here, for the Plynling role. Read-only on purpose: loaded untracked
    // and settled in memory (like AdminStatsService), so a death nobody has noticed yet already
    // counts as a death, without this read writing it — the sweep stamps and announces it.
    public async Task<HashSet<ulong>> GetLivingOwnerIdsAsync(ulong guildId, DateTimeOffset now)
    {
        var living = await _db_context.Plynlings.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.DiedAt == null)
            .ToListAsync();
        foreach (var p in living) PlynlingLife.Settle(p, now);   // in memory only: AsNoTracking, never saved
        return living.Where(x => x.DiedAt is null).Select(x => x.OwnerId).ToHashSet();
    }

    // The same question for one person (the role hook after adopt / abandon / resurrect).
    public async Task<bool> HasLivingAsync(ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var p = await _db_context.Plynlings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt == null);
        if (p is null) return false;
        PlynlingLife.Settle(p, now);   // in memory only
        return p.DiedAt is null;
    }
```

- [ ] **Step 2: Write the service** — create `ProjectSYNCS/Services/Plynling/PlynlingRoleService.cs`:

```csharp
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Keeps the configured Plynling role on everyone with a living Plynling and off everyone else
// (strict mirror — see docs/agents/plynling.md, "The Plynling role"). Two paths, one rule:
//
// - SyncMemberAsync, right after adopt / abandon / staff resurrect, so the role arrives at once;
// - SyncGuildAsync, the last step of every PlynlingSweepService pass, which catches what raises
//   nothing: lazy deaths, a rejoin, a role given or taken by hand, a hook that failed.
//
// Singleton: it holds the pass lock and the logged-once set. It takes IServiceProvider and scopes
// per unit of work, never a DB service. Nothing here throws to its caller: a failed role change is
// logged, and the next hourly pass is the backstop.
public sealed class PlynlingRoleService
{
    private static readonly RequestOptions Granted = new() { AuditLogReason = "Plynling vivant" };
    private static readonly RequestOptions Revoked = new() { AuditLogReason = "Plus de Plynling" };

    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly ILogger<PlynlingRoleService> _logger;

    // One guild pass at a time: the sweep and a `set` can overlap. The member hook does not take it —
    // same rule, and adding a held role or removing an absent one is a no-op on Discord's side.
    private readonly SemaphoreSlim _passGate = new(1, 1);

    // Guilds whose role problem was already logged, so a broken setup logs once, not hourly. A plain
    // set under a lock rather than a BoundedSet: entries are removed when the problem goes away, and
    // there are only ever a handful of guilds.
    private readonly object _loggedGate = new();
    private readonly HashSet<ulong> _loggedProblem = new();

    public PlynlingRoleService(IServiceProvider services, GuildConfigService config, ILogger<PlynlingRoleService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Why she cannot manage <paramref name="role"/> in <paramref name="guild"/>, or null. A null
    /// <paramref name="role"/> means the configured id no longer resolves.
    /// </summary>
    public static string? Problem(SocketGuild guild, SocketRole? role, bool adopting)
    {
        var me = guild.CurrentUser;
        var bot = me is null
            ? new BotFacts(false, 0)
            : new BotFacts(me.GuildPermissions.ManageRoles, me.Hierarchy);
        var facts = role is null
            ? RoleFacts.Missing
            : new RoleFacts(true, role.IsEveryone, role.IsManaged, role.Position, role.Permissions.RawValue);
        return PlynlingRoleSync.Refusal(facts, bot, adopting);
    }

    /// <summary>What a sync would do now, for the <c>set</c> / <c>create</c> reply.</summary>
    public async Task<(int ToAdd, int ToRemove)> PreviewAsync(SocketGuild guild, ulong roleId)
    {
        var owners = await LoadOwnersAsync(guild.Id);
        var holders = guild.GetRole(roleId)?.Members.Select(m => m.Id).ToHashSet() ?? new HashSet<ulong>();
        var (toAdd, toRemove) = PlynlingRoleSync.Diff(owners, holders, guild.Users.Select(u => u.Id).ToHashSet());
        return (toAdd.Count, toRemove.Count);
    }

    /// <summary>
    /// A guild sync in the background, after <paramref name="delay"/> — for <c>set</c> and
    /// <c>create</c>, whose reply must not wait on dozens of role calls.
    /// </summary>
    public void StartSync(SocketGuild guild, TimeSpan delay) =>
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            await SyncGuildAsync(guild);
        });

    /// <summary>Brings the whole guild in line with the database. Never throws.</summary>
    public async Task SyncGuildAsync(SocketGuild guild)
    {
        await _passGate.WaitAsync();
        try
        {
            await SyncGuildCoreAsync(guild);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plynling role sync failed for guild {GuildId}.", guild.Id);
        }
        finally
        {
            _passGate.Release();
        }
    }

    /// <summary>Brings one person in line, right after a life change. Never throws.</summary>
    public async Task SyncMemberAsync(SocketGuild guild, ulong userId)
    {
        try
        {
            var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
            if (roleId == 0) return;
            var role = guild.GetRole(roleId);
            if (Problem(guild, role, adopting: false) is not null) return;   // the guild pass logs it
            if (guild.GetUser(userId) is not { } member) return;

            bool owns;
            await using (var scope = _services.CreateAsyncScope())
                owns = await scope.ServiceProvider.GetRequiredService<PlynlingService>()
                    .HasLivingAsync(guild.Id, userId, DateTimeOffset.UtcNow);

            bool holds = member.Roles.Any(r => r.Id == roleId);
            if (owns && !holds) await member.AddRoleAsync(roleId, Granted);
            else if (!owns && holds) await member.RemoveRoleAsync(roleId, Revoked);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plynling role update failed for {UserId} in guild {GuildId}; the hourly pass will retry.",
                userId, guild.Id);
        }
    }

    private async Task SyncGuildCoreAsync(SocketGuild guild)
    {
        var roleId = (await _config.GetAsync(guild.Id)).PlynlingRoleId;
        if (roleId == 0) return;

        var role = guild.GetRole(roleId);
        if (Problem(guild, role, adopting: false) is { } problem)
        {
            bool first;
            lock (_loggedGate) first = _loggedProblem.Add(guild.Id);
            if (first)
                _logger.LogWarning("Plynling role {RoleId} in guild {GuildId} can't be managed: {Problem}",
                    roleId, guild.Id, problem);
            return;
        }
        lock (_loggedGate) _loggedProblem.Remove(guild.Id);

        // With a partial member cache an owner looks absent and would be skipped as "left the
        // guild"; doing nothing for an hour is better than acting on half a list.
        if (!guild.HasAllMembers)
        {
            _logger.LogDebug("Skipping the Plynling role sync for guild {GuildId}: member list incomplete.", guild.Id);
            return;
        }

        var owners = await LoadOwnersAsync(guild.Id);
        var (toAdd, toRemove) = PlynlingRoleSync.Diff(
            owners,
            role!.Members.Select(m => m.Id).ToHashSet(),
            guild.Users.Select(u => u.Id).ToHashSet());

        foreach (var id in toAdd)
        {
            try
            {
                if (guild.GetUser(id) is { } member) await member.AddRoleAsync(roleId, Granted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to give the Plynling role to {UserId} in guild {GuildId}.", id, guild.Id);
            }
        }

        if (toRemove.Count > PlynlingRoleSync.MaxRemovalsPerPass)
            _logger.LogWarning("Plynling role: {Count} removals due in guild {GuildId}; doing {Max} this pass.",
                toRemove.Count, guild.Id, PlynlingRoleSync.MaxRemovalsPerPass);
        foreach (var id in toRemove.Take(PlynlingRoleSync.MaxRemovalsPerPass))
        {
            try
            {
                if (guild.GetUser(id) is { } member) await member.RemoveRoleAsync(roleId, Revoked);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove the Plynling role from {UserId} in guild {GuildId}.", id, guild.Id);
            }
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
            _logger.LogInformation("Plynling role synced in guild {GuildId}: +{Added} −{Removed}.",
                guild.Id, toAdd.Count, Math.Min(toRemove.Count, PlynlingRoleSync.MaxRemovalsPerPass));
    }

    private async Task<HashSet<ulong>> LoadOwnersAsync(ulong guildId)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PlynlingService>()
            .GetLivingOwnerIdsAsync(guildId, DateTimeOffset.UtcNow);
    }
}
```

- [ ] **Step 3: Register it** — in `Program.cs`, right after `services.AddSingleton<GuildConfigService>();`:

```csharp
        services.AddSingleton<PlynlingRoleService>();
```

- [ ] **Step 4: Build**

```bash
dotnet build -warnaserror
```

  Expected: `Build succeeded`, 0 warnings. If `AddRoleAsync(ulong, RequestOptions)` doesn't resolve on `SocketGuildUser`, check the Discord.Net 3.20 overloads (`IGuildUser.AddRoleAsync(ulong roleId, RequestOptions options = null)`) — don't switch to passing the `IRole`, the id overload is what lets the hook work without resolving the role object.

---

### Task 4: `/config plynling-role` and `/config show`

**Files:**
- Modify: `ProjectSYNCS/Commands/Admin/ConfigModule.cs` (new subgroup after `ModeratorRoleModule`; `ShowAsync`; header comment)
- Modify: `ProjectSYNCS/Commands/Admin/ConfigCards.cs`

**Interfaces:**
- Consumes: `GuildConfigService.SetPlynlingRoleAsync`, `GuildConfig.PlynlingRoleId`, `PlynlingRoleService.{Problem, PreviewAsync, StartSync}`, `PlynlingRoleSync.{DefaultName, NoManageRoles, Counts}`, `InputCaps.RoleName`.
- Produces: `ConfigCards.Build(GuildConfig config, bool isHomeGuild, bool defaultGameChannelHere, string? plynlingRoleProblem)`.

- [ ] **Step 1: The card section** — in `ConfigCards.cs`:

  Change the signature and add a doc param:

```csharp
    /// <param name="plynlingRoleProblem">Why she can't manage the configured Plynling role, or null.</param>
    public static MessageComponent Build(GuildConfig config, bool isHomeGuild, bool defaultGameChannelHere,
        string? plynlingRoleProblem)
```

  After the « Rôle modérateur » section, add:

```csharp
        Section(container,
            "**Rôle Plynling** — tous les maîtres d'un Plynling vivant, et personne d'autre",
            config.PlynlingRoleId == 0
                ? "> *non configuré*"
                : $"> <@&{config.PlynlingRoleId}>" + (plynlingRoleProblem is null ? "" : $"\n> ⚠️ {plynlingRoleProblem}"));
```

  Update the header comment's count: `1 container + 1 title + 8 × (separator + text) = 18 of the 40 components.`

- [ ] **Step 2: `show` passes the problem** — in `ConfigModule.ShowAsync`, replace the `FollowupAsync` call's `components:` argument:

```csharp
        var roleProblem = config.PlynlingRoleId == 0
            ? null
            : PlynlingRoleService.Problem(Context.Guild, Context.Guild.GetRole(config.PlynlingRoleId), adopting: false);

        await FollowupAsync(
            components: ConfigCards.Build(config, Context.Guild.Id == HomeGuild.Id, defaultGameHere, roleProblem),
            flags: MessageFlags.ComponentsV2,
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
```

- [ ] **Step 3: The subgroup** — add after `ModeratorRoleModule` in `ConfigModule.cs` (add `using Discord.Net;` at the top for `HttpException`):

```csharp
    [Group("plynling-role", "Le rôle de tous les maîtres d'un Plynling vivant")]
    public class PlynlingRoleModule : InteractionModuleBase<SocketInteractionContext>
    {
        // A role created over REST reaches the gateway cache a moment later; syncing before that
        // would find "no such role" and log a problem that isn't one.
        private static readonly TimeSpan FirstSyncDelay = TimeSpan.FromSeconds(3);

        private readonly GuildConfigService _config;
        private readonly PlynlingRoleService _roles;

        public PlynlingRoleModule(GuildConfigService config, PlynlingRoleService roles)
        {
            _config = config;
            _roles = roles;
        }

        [SlashCommand("create", "Créer le rôle des maîtres de Plynlings et le gérer")]
        public async Task CreateAsync(
            [Summary("name", "Le nom du rôle (par défaut : Maîtres de Plynlings)")]
            [MaxLength(InputCaps.RoleName)] string? name = null)
        {
            if (!await BeginAsync(Context)) return;

            // A configured role that was deleted in Discord counts as none.
            var current = (await _config.GetAsync(Context.Guild.Id)).PlynlingRoleId;
            if (current != 0 && Context.Guild.GetRole(current) is not null)
            {
                await SayAsync(Context,
                    $"Il y a déjà un rôle Plynling : <@&{current}>. `/config plynling-role clear` d'abord si tu en veux un autre (ᵕ • ᴗ •)");
                return;
            }
            if (Context.Guild.CurrentUser?.GuildPermissions.ManageRoles != true)
            {
                await SayAsync(Context, PlynlingRoleSync.NoManageRoles);
                return;
            }

            name = string.IsNullOrWhiteSpace(name) ? PlynlingRoleSync.DefaultName : name.Trim();
            IRole role;
            try
            {
                // No permissions, mentionable by everyone (it is a ping group). A new role lands at
                // the bottom of the list, below hers, so she can manage it from the start.
                role = await Context.Guild.CreateRoleAsync(name,
                    permissions: GuildPermissions.None,
                    isMentionable: true,
                    options: new RequestOptions { AuditLogReason = "/config plynling-role create" });
            }
            catch (HttpException ex)
            {
                await SayAsync(Context, $"Discord a refusé de créer le rôle ({ex.Reason ?? ex.HttpCode.ToString()}) {Emotes.Staring}");
                return;
            }

            await _config.SetPlynlingRoleAsync(Context.Guild.Id, role.Id);
            var (toAdd, toRemove) = await _roles.PreviewAsync(Context.Guild, role.Id);
            await SayAsync(Context,
                $"<@&{role.Id}> est créé. {PlynlingRoleSync.Counts(toAdd, toRemove)} "
                + "Renomme-le ou change sa couleur comme tu veux : moi, je ne fais que le donner et le retirer ദ്ദി◝ ⩊ ◜.ᐟ");
            _roles.StartSync(Context.Guild, FirstSyncDelay);
        }

        [SlashCommand("set", "Gérer un rôle existant comme rôle Plynling")]
        public async Task SetAsync(
            [Summary("role", "Le rôle à donner aux maîtres de Plynlings")] IRole role)
        {
            if (!await BeginAsync(Context)) return;

            if (PlynlingRoleService.Problem(Context.Guild, Context.Guild.GetRole(role.Id), adopting: true) is { } problem)
            {
                await SayAsync(Context, problem);
                return;
            }

            await _config.SetPlynlingRoleAsync(Context.Guild.Id, role.Id);
            var (toAdd, toRemove) = await _roles.PreviewAsync(Context.Guild, role.Id);
            await SayAsync(Context,
                $"<@&{role.Id}> est maintenant le rôle Plynling. {PlynlingRoleSync.Counts(toAdd, toRemove)}");
            _roles.StartSync(Context.Guild, TimeSpan.Zero);
        }

        [SlashCommand("clear", "Ne plus gérer de rôle Plynling")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetPlynlingRoleAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Je ne gère plus de rôle Plynling. Le rôle et ses membres restent tels quels : "
                + "supprime-le dans Discord si tu n'en veux plus (ᵕ • ᴗ •)");
        }
    }
```

  `BeginAsync` and `SayAsync` are `private static` on the outer `ConfigModule`; the nested classes already call them (see `ModeratorRoleModule`), so no visibility change is needed.

- [ ] **Step 4: Header comment** — in the `ConfigModule` class comment, after the "Single channels" bullet, add:

```csharp
// - **The Plynling role** is neither: SYNCS *owns* who holds it (a strict mirror of who has a
//   living Plynling — see PlynlingRoleService). Setting it can therefore take the role away from
//   people, which is why `set` refuses @everyone, managed roles, roles above hers and any role
//   carrying permissions, and why `clear` only stops managing it and touches nobody.
```

- [ ] **Step 5: Build**

```bash
dotnet build -warnaserror
```

  Expected: `Build succeeded`, 0 warnings. If the named arguments to `CreateRoleAsync` don't match Discord.Net 3.20's parameter names, look up `SocketGuild.CreateRoleAsync` and fix the names — keep `GuildPermissions.None` and `isMentionable: true`.

- [ ] **Step 6: Option count and callers** — Discord allows 25 options per level: `/config`'s top level holds `show` plus one entry per subgroup, and each subgroup its own subcommands. After this task the top level is `show` + 8 subgroups = 9, and `plynling-role` holds 3 — far from either cap; confirm by counting the `[Group(` attributes nested in `ConfigModule`. Then `grep -rn "ConfigCards.Build(" ProjectSYNCS`: expected only `ShowAsync`; any other caller passes the new `plynlingRoleProblem` argument the same way.

---

### Task 5: The hooks and the sweep step

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule.cs` (field, constructor, `AdoptAsync` ~line 79, `OnAbandonConfirmedAsync` ~line 134)
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingGroups.cs` (both classes)
- Modify: `ProjectSYNCS/Commands/Admin/AdminModule.cs` (`PlynlingAdminModule`, ~lines 321-395)
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingSweepService.cs`

**Interfaces:**
- Consumes: `PlynlingRoleService.SyncMemberAsync(SocketGuild, ulong)`, `PlynlingRoleService.SyncGuildAsync(SocketGuild)` — both never throw.

- [ ] **Step 1: `PlynlingModule` dependency** — `plynling.md`: a new dependency goes in three places.

  In `PlynlingModule.cs`, add a field next to the others:

```csharp
    private readonly PlynlingRoleService _roles;
```

  Add `PlynlingRoleService roles` to the protected constructor as the parameter **before** `ILogger<PlynlingModule> logger`, and assign `_roles = roles;` in its body.

  In `PlynlingGroups.cs`, both classes become (same change in each — add `PlynlingRoleService roles` before `logger` in the primary constructor and pass it through):

```csharp
[Group("plynling", "Ton Plynling : l'adopter, t'en occuper, le regarder vivre")]
public sealed class PlynlingLongModule(PlynlingService plynlings, ResponsePicker picker,
    PlynlingAnnouncer announcer, PlynlingCooldowns cooldowns, ShameService shame, PlynlingPlayService play,
    CosmeticService cosmetics, PlynlingVisitRunner visits, PlynlingRoleService roles, ILogger<PlynlingModule> logger)
    : PlynlingModule(plynlings, picker, announcer, cooldowns, shame, play, cosmetics, visits, roles, logger)
```

```csharp
[Group("pl", "Raccourci de /plynling")]
public sealed class PlynlingShortModule(PlynlingService plynlings, ResponsePicker picker,
    PlynlingAnnouncer announcer, PlynlingCooldowns cooldowns, ShameService shame, PlynlingPlayService play,
    CosmeticService cosmetics, PlynlingVisitRunner visits, PlynlingRoleService roles, ILogger<PlynlingModule> logger)
    : PlynlingModule(plynlings, picker, announcer, cooldowns, shame, play, cosmetics, visits, roles, logger);
```

  Grep for any other `: PlynlingModule(` or `new PlynlingModule` — `grep -rn "PlynlingModule(" ProjectSYNCS`. Fix any other caller the same way.

- [ ] **Step 2: Adopt hook** — at the end of `AdoptAsync`, after `await RespondCardAsync(plynling, now, line);`:

```csharp
        await _roles.SyncMemberAsync(Context.Guild, Context.User.Id);   // after the card: never delays it, never throws
```

- [ ] **Step 3: Abandon hook** — at the end of `OnAbandonConfirmedAsync`, after `await _announcer.AnnounceAbandonAsync(gone, now);`:

```csharp
        await _roles.SyncMemberAsync(Context.Guild, Context.User.Id);
```

- [ ] **Step 4: Resurrect hook** — in `AdminModule.PlynlingAdminModule`: add `PlynlingRoleService roles` to its constructor (after `PlynlingAnnouncer announcer`), a `private readonly PlynlingRoleService _roles;` field, `_roles = roles;`. Then at the end of `ResurrectAsync`, after `await _announcer.AnnounceResurrectionAsync(plynling, now);`:

```csharp
            await _roles.SyncMemberAsync(Context.Guild, user.Id);
```

- [ ] **Step 5: Sweep step** — in `PlynlingSweepService`:

  Add `PlynlingRoleService roles` to the constructor (after `PlynlingAnnouncer announcer`), field `private readonly PlynlingRoleService _roles;`, assignment.

  In `SweepAsync`, between the end of the `foreach (var id in ids) { ... }` loop and `return ids.Count;`, add:

```csharp
        // Last, after every death above is stamped: a Plynling that died this pass loses its owner
        // the Plynling role in the same pass. Per guild, and SyncGuildAsync never throws — nothing
        // may escape the hosted loop.
        foreach (var guild in _client.Guilds)
        {
            try
            {
                await _roles.SyncGuildAsync(guild);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plynling role sync failed for guild {GuildId}.", guild.Id);
            }
        }
```

  Also extend the class header comment's first sentence: `…and sends the single warning DM about three hours before a death; last, it brings each guild's Plynling role in line (PlynlingRoleService).`

  Note: the early `return 0;` when loading ids fails skips the role step too — correct, a DB that can't list Plynlings can't list owners either.

- [ ] **Step 6: Build**

```bash
dotnet build -warnaserror
```

  Expected: `Build succeeded`, 0 warnings.

---

### Task 6: Docs, help, version

**Files:**
- Modify: `docs/agents/plynling.md`, `docs/agents/admin.md`, `README.md`, `ProjectSYNCS/Commands/Core/HelpModule.cs`, `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`
- Maybe: root `CLAUDE.md`

- [ ] **Step 1: `docs/agents/plynling.md`** — add a section after "State is computed; nothing ticks" (before "Sleep"), as its own `##`:

```markdown
## The Plynling role

**A strict mirror**: the role configured with `/config plynling-role` is held by everyone with a
living Plynling (frozen counts) and by nobody else — `PlynlingRoleService` adds it where missing and
removes it everywhere else, hand-given copies included. Flair and a ping group; it gates nothing,
so being an hour late is harmless.

- **Two paths, one rule.** `SyncMemberAsync` right after a life change, so the role arrives at once;
  `SyncGuildAsync` as the **last step of every `PlynlingSweepService` pass**, after deaths are
  stamped. Deaths are lazy and raise nothing, so only the sweep covers them.
- **The three call sites are the only life changes**: adopt and abandon (`PlynlingModule`) and staff
  resurrect (`AdminModule`). **A fourth path that creates or ends a living Plynling must call
  `SyncMemberAsync`** — otherwise its people wait up to an hour.
- The owner list is **read-only**: loaded `AsNoTracking`, settled in memory
  (`GetLivingOwnerIdsAsync`), so an unnoticed death counts without the read writing it.
- **The pass is skipped when `!guild.HasAllMembers`**: on half a member list, owners look departed.
- **At most 25 removals a pass** (`PlynlingRoleSync.MaxRemovalsPerPass`): a wrong owner list must not
  strip the whole guild at once. Additions are uncapped.
- **The rules are pure** (`Helpers/Plynling/PlynlingRoleSync`): `Diff` and one `Refusal` shared by
  `set`, `/config show` and the sweep, so they can't disagree about what is manageable. A problem
  is logged once per guild per process, and shown on `/config show`.
- Both paths never throw to their caller; a failed role change is logged and retried next pass.
```

- [ ] **Step 2: `docs/agents/admin.md`** — in the `/config` section:

  In the subcommand list sentence, add `plynling-role create|set|clear,` after `moderator-role set|clear,`.

  After the "Single channels replace their default" bullet, add a third bullet:

```markdown
- **The Plynling role is owned, not configured.** `GuildSettings.PlynlingRoleId`, zero meaning none.
  SYNCS decides who holds it (a strict mirror — see `plynling.md`), so `set` can take it away from
  people: it refuses `@everyone`, managed roles, roles at or above hers and any role carrying
  permissions, and its reply says how many will gain and lose it. `create` makes a mentionable,
  permission-less role and refuses while a configured role still exists (no orphan from a second
  click). `clear` only stops managing it: it touches no member and deletes nothing. After creation
  she never edits the role — name, colour and position are staff's.
```

  Also change the first line of that section's "Two rules, by the shape of the setting." to "**Two rules, by the shape of the setting — and one exception.**"

- [ ] **Step 3: `HelpModule`** — the `/config` line in the "Commandes — Staff & aide" field becomes:

```csharp
                "**`/config`** — Qui vote avec `/shame`, les salons où rien ne compte, le salon de jeu, " +
                "le salon du quiz, le salon principal et le rôle Plynling. **`/config show`** affiche la configuration actuelle.\n" +
```

- [ ] **Step 4: Measure `/help`** — append to the Task 2 scratch `Program.cs`:

```csharp
var help = ProjectSYNCS.Commands.HelpModule.BuildEmbed();
Check(help.Length <= 6000, $"/help total {help.Length} ≤ 6000");
foreach (var f in help.Fields) Check(f.Value.Length <= 1024, $"/help field '{f.Name}' {f.Value.Length} ≤ 1024");
```

  Run `dotnet run --project <scratchpad>/rolecheck`. Expected: all `ok`. If a cap fails, shorten the new wording (e.g. drop « et le rôle Plynling » to « le rôle Plynling ») rather than other lines. If `BuildEmbed` isn't public/static as expected, read `HelpModule.cs:32` and adapt the call.

- [ ] **Step 5: `README.md`**

  In "Staff & owner", the `/config` list gains, after the `quiz-channel` bullet:

```markdown
  - `plynling-role create|set|clear` — a role SYNCS keeps on everyone with a living Plynling and
    off everyone else (flair and a ping group); `create` makes it for you, `clear` stops managing it
    without touching anyone;
```

  In the permissions paragraph (~line 540, "It also needs the usual channel permissions…"), append:

```markdown
*Manage Roles* is needed only for `/config plynling-role`, and her top role must sit above the
Plynling role (a role she creates starts below hers).
```

- [ ] **Step 6: Version and changelog** — first check the batch isn't already bumped:

```bash
git diff --stat -- ProjectSYNCS/config.yaml
```

  Expected: no output. Then in `ProjectSYNCS/config.yaml` set `version: "6.11.0"`, and add at the top of `ProjectSYNCS/CHANGELOG.md` (below the intro paragraph, above `## 6.10.1`):

```markdown
## 6.11.0 — 2026-10-13

- Rôle Plynling : SYNCS peut créer et gérer un rôle pour tous les maîtres d'un Plynling vivant
  (`/config plynling-role`). Il arrive à l'adoption et s'en va avec le Plynling.
```

- [ ] **Step 7: Root `CLAUDE.md`** — the architecture section lists background loops and Ready hooks, not singletons; `PlynlingRoleService` is neither. **No change** unless you added a loop or hook (you shouldn't have). Confirm and move on.

- [ ] **Step 8: Build**

```bash
dotnet build -warnaserror
```

  Expected: `Build succeeded`, 0 warnings.

---

### Task 7: Dev-guild verification

Needs the owner's dev token and guild (`dotnet user-secrets`, see root `CLAUDE.md`), the bot granted **Manage Roles**, and a second account. Run `dotnet run` from `ProjectSYNCS/`; the migration applies on startup — check the log shows `AddPlynlingRole` applied and no exception.

- [ ] **Step 1: create** — `/config plynling-role create`. Expected: reply with the pill and counts; within ~5 s every existing owner holds the role; the log shows `Plynling role synced … +N −0` and **no** "can't be managed" warning (Review Focus 1).
- [ ] **Step 2: create again** — expected: refused, naming the existing role.
- [ ] **Step 3: adopt / abandon** — with the second account: adopt → role at once; abandon → removed at once.
- [ ] **Step 4: sweep fixes drift** — give the role by hand to someone without a Plynling; `/debug sweep`. Expected: removed, audit log reason « Plus de Plynling ». (Review Focus 4, if you can: `/debug` a Plynling to starvation, don't look at it, sweep → owner loses the role.)
- [ ] **Step 5: set guards** — `/config plynling-role set` with `@everyone`, a bot's managed role, a role with any permission, a role above hers: each refused with its own French reason. A plain populated role: reply counts match what the sync then does (Review Focus 2).
- [ ] **Step 6: broken setup** — move the role above hers. `/config show` shows ⚠️ with the reason; `/debug sweep` twice → the warning is logged **once**. Move it back, sweep → syncs again (Review Focus 5).
- [ ] **Step 7: clear** — `/config plynling-role clear`. Expected: nobody's roles change; `/config show` says « non configuré »; a sweep touches nothing.
- [ ] **Step 8: delete the role in Discord while configured** — `/config show` warns « n'existe plus »; `create` is allowed again without `clear`.

Report results to the owner; leave everything uncommitted.
