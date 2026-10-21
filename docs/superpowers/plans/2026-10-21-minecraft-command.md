# `/minecraft` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A `/minecraft` slash group (`help`, `status`, `inventory`, `where`, `link`, `invite`, `stats`) that shows her Minecraft life on Discord, links Discord members to in-game names, and lets a linked member bring her onto the server.

**Architecture:** One `MinecraftModule` (group `minecraft`) in `Commands/Minecraft/`, thin over `MinecraftBodyService`, which gains public read methods, the link-code redemption on whispers, the invite hold and an in-memory stats tally flushed by its own loop. Pure pieces (the help catalogue, link codes, « what she's doing », stat keys) live in `Helpers/Minecraft/` and are walked by `tests/ProjectSYNCS.Checks`. Links and stats are two new EF tables behind one transient `MinecraftDataService`.

**Tech Stack:** .NET 10, Discord.Net 3.20 Interactions, EF Core + SQLite, the checks console (`tests/ProjectSYNCS.Checks`, no framework).

**Spec:** `docs/superpowers/specs/2026-10-21-minecraft-command-design.md`

## Global Constraints

- Command/option names English; every user-facing string French; code, comments, logs English.
- `[Group("minecraft", "Le serveur Minecraft : son mode d'emploi, où elle en est, et l'y inviter")]` with `[CommandContextType(InteractionContextType.Guild)]`; 7 subcommands.
- `[ComponentInteraction]` in a group module needs `ignoreGroupNames: true` (put the help buttons' handler in `Interactions/Components/`, like `HelpComponentHandler`).
- Read-only replies are ephemeral. Handlers touching the database `DeferAsync(ephemeral: true)` first.
- Snowflakes `.HasConversion<long>()`; no `DateTimeOffset` comparison in SQL; stored string keys are append-only.
- `MinecraftBodyService` is a singleton: database through `IServiceProvider.CreateAsyncScope()`, never from the body's event thread synchronously; stats never written per event.
- In-game names and item ids in Discord replies: `Format.Sanitize` or code spans, `AllowedMentions.None`.
- In-game lines: new `Mc*` pools in `BotResponses.Minecraft.cs`, in her voice (`docs/syncs-voice.md`), plain drawable text ≤ 256, one face in four lines at most, added to the checks' Minecraft pool list.
- Embeds ≤ 6000 per message, fields ≤ 1024, 5 buttons per row, unique custom-ids per message.
- Version 7.25.0 → **7.26.0** in `ProjectSYNCS/config.yaml` (new commands + a migration), `CHANGELOG.md` entry in English; `/help` and `README.md` list `/minecraft`; `docs/agents/minecraft.md` gets a `/minecraft` section; CLAUDE.md's group-count line gains `/minecraft` (7).
- The owner commits by hand: no commit steps. Each task ends with `dotnet build ProjectSYNCS -warnaserror` and `dotnet run --project tests/ProjectSYNCS.Checks` green.

## Review Focus

- **Minecraft off or she's not in** (no `minecraft_host`, body absent, server down): every subcommand answers a clear French line, never throws. → tests in Tasks 2 and 3 (`McDoing`/text builders with null state).
- **A code whispered by another player than the one named** (`link name:Bob`, then Alice whispers Bob's code): refused, nothing saved. → Task 4 test.
- **A stale or replaced code** (whispered after 10 minutes, or after the member asked for a new one): refused. → Task 4 test.
- **Invite while forbidden, during the owner's leave hold, or while already in / already invited**: each a distinct French answer, no body started. → Task 5 test on the pure decision.
- **`where` with no answer from the body** (busy, dying, just leaving): answers « pas de réponse » after the timeout instead of hanging the interaction. → Task 3 (timeout path, by hand on the dev guild).

---

### Task 1: The help catalogue and `/minecraft help`

**Files:**
- Create: `ProjectSYNCS/Helpers/Minecraft/McHelp.cs`
- Create: `ProjectSYNCS/Commands/Minecraft/MinecraftModule.cs`
- Create: `ProjectSYNCS/Interactions/Components/MinecraftHelpHandler.cs`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` (new block before « The picker's narrow pick »)

**Interfaces:**
- Produces: `public sealed record McHelpExample(string Text, McOrder Order);` `public sealed record McHelpSection(string Title, string Blurb, McHelpExample[] Examples);` `public static class McHelp { public static readonly McHelpSection[] Sections; }`; `MinecraftModule.BuildHelp(int page) : Embed`, `MinecraftModule.BuildHelpButtons(int page) : MessageComponent`, `MinecraftModule.HelpPages : int`; custom-id `minecraft:help:{page}`.

- [ ] **Step 1: Write the failing check.** Every example parses as its order, with her names `["SYNCS", .. McCommand.HerNames]` and the known place keys `["maison", "mine"]`; talk examples (`McOrder.None`) must read as talk (`McTalkReader.Read(..., call: true, ...) is not null`); every page fits the caps.

```csharp
foreach (var s in McHelp.Sections)
foreach (var e in s.Examples)
{
    var got = McCommand.Parse(e.Text, ["SYNCS", .. McCommand.HerNames], places: ["maison", "mine"]).Order;
    if (e.Order != McOrder.None && got != e.Order) Fail($"/minecraft help « {e.Text} »: parses as {got}, shown as {e.Order}");
    if (e.Order == McOrder.None && McTalkReader.Read(e.Text, McKin.Anyone, true, false, false, 1, null, new ResponsePicker(), new Random(1)) is null)
        Fail($"/minecraft help « {e.Text} »: shown as talk, read as nothing");
}
for (var p = 0; p < MinecraftModule.HelpPages; p++)
{
    var embed = MinecraftModule.BuildHelp(p);
    if (embed.Length > 6000 || embed.Fields.Any(f => f.Value.Length > 1024)) Fail($"/minecraft help page {p}: over the caps");
}
```

- [ ] **Step 2: Run** `dotnet run --project tests/ProjectSYNCS.Checks` — Expected: build error (`McHelp` missing).
- [ ] **Step 3: Write `McHelp.Sections`.** Sections in this order: « Se déplacer » (Come, Follow, Stay, Sit, GoTo, Where, Jump), « Récolter, cultiver, pêcher » (Collect, Harvest, Farm, Ranch, Fish, Smelt), « Les lieux » (SavePlace, GoToPlace, Guide, WherePlace, ListPlaces, ForgetPlace, Patrol), « Objets et coffres » (Give, Store, Inventory, Have), « Combat et sommeil » (Guard, Sleep), « Minuteur » (Timer), « Lui parler » (Wave, Bye, Praise, Thanks, and `McOrder.None` talk examples: « syncs ça va ? », « syncs raconte une blague »). Two or three examples each, taken from the phrasings in `McCommand` and the existing checks (e.g. « syncs suis-moi », « syncs va chercher 20 bois », « syncs retiens la mine ici », « syncs rappelle-moi dans 10 min de sortir le pain »). Each `Blurb` one sentence on what she does (from `docs/agents/minecraft.md`, *Orders*), plus « Ton nom en premier ou en dernier : « syncs … » ou « … syncs ». » once in the first section.
- [ ] **Step 4: Write `MinecraftModule` and the handler.** The group as in Global Constraints; `help` responds ephemeral with page 0. `BuildHelp(page)`: one embed per page, one field per section, its examples as « `texte` » bullet lines; split sections across pages so every page fits (2 pages expected). Buttons « ◀ » / « ▶ » with ids `minecraft:help:{p}`, disabled at the ends. Handler mirrors `HelpComponentHandler` (`UpdateAsync`, no defer).
- [ ] **Step 5: Run** build and checks — Expected: `ALL CHECKS PASSED`.

### Task 2: « What she's doing » and `/minecraft status`

**Files:**
- Create: `ProjectSYNCS/Helpers/Minecraft/McDoing.cs`
- Modify: `ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs` (new public `PublicStatusText()` next to `StatusText()`)
- Modify: `ProjectSYNCS/Commands/Minecraft/MinecraftModule.cs`
- Test: checks

**Interfaces:**
- Produces: `public static string McDoing.Describe(McOrder? order, string? forWho, string? place)` → a French clause (« elle pêche pour Bob », « elle garde la ferme », « elle suit Papa », « elle ne fait rien de spécial » for null); `MinecraftBodyService.PublicStatusText() : string`.

- [ ] **Step 1: Failing checks:** `McDoing.Describe(McOrder.Fish, "Bob", null) == "elle pêche pour Bob"`, `Describe(McOrder.Follow, "Papa", null) == "elle suit Papa"`, `Describe(McOrder.Patrol, "Bob", "la mine") == "elle garde la mine pour Bob"`, `Describe(null, null, null) == "elle ne fait rien de spécial"`; every `McOrder` value gives a non-empty clause without throwing.
- [ ] **Step 2: Run** — Expected: build error.
- [ ] **Step 3: Implement `McDoing.Describe`** (a switch over `McOrder`; `forWho` already « Papa »/« Tata » via the service's `Who`).
- [ ] **Step 4: Implement `PublicStatusText()`**: under `_gate`, from `_lastStatus`, `_spawned`, `_inGameSince`, `_players`, `_order`/`_pending` (`McJob.Order`, `Who(job.Player)`, `job.Detail` as place). Disabled (`!Enabled` or no script) → « Elle n'a pas de serveur Minecraft configuré. »; server unreachable → « Le serveur ne répond pas. »; not in game → server line + « Elle n'est pas sur le serveur. ». No host, port, kick reasons or allow-switch details (owner-only, stays in `/debug`). Names through `Format.Sanitize`.
- [ ] **Step 5: Add `status`** (ephemeral `RespondAsync(_minecraft.PublicStatusText(), allowedMentions: AllowedMentions.None)`).
- [ ] **Step 6: Run** build and checks — Expected: pass.

### Task 3: `/minecraft inventory` and `/minecraft where`

**Files:**
- Modify: `ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs`
- Modify: `ProjectSYNCS/Commands/Minecraft/MinecraftModule.cs`

**Interfaces:**
- Produces: `MinecraftBodyService.PublicInventoryText() : string`; `MinecraftBodyService.WhereAsync(TimeSpan timeout) : Task<string>`.

- [ ] **Step 1: `PublicInventoryText()`**: `InventoryText()` without the 🔧 owner prefix (factor the shared body into a private `InventoryBody()`); not in game → « Elle n'est pas sur le serveur. ».
- [ ] **Step 2: `WhereAsync`**: not spawned → « Elle n'est pas sur le serveur. ». Otherwise register a `TaskCompletionSource<(int X, int Y, int Z, string Dimension)>` under `_gate` (one shared pending request; a second caller awaits the same one), send `{ op = "where", player = DiscordAsker }` with `private const string DiscordAsker = "\u0001discord"` (no real player name can hold U+0001), and in the `"position"` event complete it when `player == DiscordAsker` before the `_askedWhere` lookup. Timeout (`3 s`) → « Pas de réponse de sa part pour l'instant. ». Answer: « Elle est en `x y z`, {dimension} » with `BotResponses.McDimensionNames[DimensionOf(dimension)]`.
- [ ] **Step 3: Add `inventory`** (ephemeral respond) and **`where`** (`DeferAsync(ephemeral: true)` first: it waits on the body, then `FollowupAsync`).
- [ ] **Step 4: Run** build and checks — Expected: pass. By hand on the dev guild: `where` with her in game answers coordinates; with the body stopped, answers « Elle n'est pas sur le serveur. ».

### Task 4: Links (`/minecraft link`)

**Files:**
- Create: `ProjectSYNCS/Models/Minecraft/MinecraftLink.cs`
- Create: `ProjectSYNCS/Helpers/Minecraft/McLinkCodes.cs`
- Create: `ProjectSYNCS/Services/Minecraft/MinecraftDataService.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs`, `ProjectSYNCS/Program.cs` (register `MinecraftDataService` transient)
- Modify: `ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs` (whisper redemption)
- Modify: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Minecraft.cs` (`McLinkedLines`)
- Modify: `ProjectSYNCS/Commands/Minecraft/MinecraftModule.cs`
- Migration: none here; the one migration (`AddMinecraftLinksAndStats`) is added in Task 6 and covers both tables. Nothing in this task touches the database until then except through the command, which is tried by hand after Task 6.
- Test: checks

**Interfaces:**
- Produces: `public class MinecraftLink { ulong UserId (key); string Player; DateTimeOffset LinkedAt; }` with a unique index on `Player` (stored as typed; compare case-insensitively by storing `PlayerKey = Player.ToLowerInvariant()` with the unique index on it); `public sealed class McLinkCodes { string Issue(ulong userId, string player, DateTimeOffset now); ulong? Redeem(string player, string code, DateTimeOffset now); static string? ReadCode(string whisper); public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10); }`; `MinecraftDataService { Task<MinecraftLink?> LinkOfUserAsync(ulong); Task<MinecraftLink?> LinkOfPlayerAsync(string); Task SaveLinkAsync(ulong userId, string player); Task<bool> RemoveLinkAsync(ulong userId); }`; `MinecraftBodyService.LinkCodes : McLinkCodes`, `MinecraftBodyService.IsSpawned : bool`, `MinecraftBodyService.KinOfPlayer(string) : McKin`, `MinecraftBodyService.Username : string`.

- [ ] **Step 1: Failing checks on `McLinkCodes`** (clock passed in):

```csharp
var codes = new McLinkCodes();
var t0 = DateTimeOffset.UnixEpoch;
var c = codes.Issue(1, "Bob", t0);
if (c.Length != 4 || !c.All(char.IsDigit)) Fail("link code: not 4 digits");
if (codes.Redeem("Alice", c, t0) is not null) Fail("link code: redeemed by another player");
if (codes.Redeem("bob", c, t0.AddMinutes(11)) is not null) Fail("link code: redeemed after 10 minutes");
var c2 = codes.Issue(1, "Bob", t0); var c3 = codes.Issue(1, "Bob", t0.AddSeconds(1));
if (c2 != c3 && codes.Redeem("Bob", c2, t0.AddSeconds(2)) is not null) Fail("link code: an old code still works");
if (codes.Redeem("BOB", c3, t0.AddSeconds(2)) != 1) Fail("link code: the right code, any case, refused");
if (codes.Redeem("Bob", c3, t0.AddSeconds(3)) is not null) Fail("link code: used twice");
if (McLinkCodes.ReadCode("lien 4821") != "4821" || McLinkCodes.ReadCode("Lien : 4821") != "4821" || McLinkCodes.ReadCode("syncs viens") is not null)
    Fail("link code: ReadCode");
```

- [ ] **Step 2: Run** — Expected: build error.
- [ ] **Step 3: Implement `McLinkCodes`** (own lock; one live code per user; codes `Random.Shared.Next(10000):D4`; player compared case-insensitively; `ReadCode`: « lien » then 4 digits, punctuation allowed between).
- [ ] **Step 4: Model, `AppDbContext` (`DbSet<MinecraftLink> MinecraftLinks`, `UserId` key `.HasConversion<long>()`, unique index on `PlayerKey`), `MinecraftDataService`.**
- [ ] **Step 5: Redemption in `OnChat`**, first thing for `whispered` messages: `McLinkCodes.ReadCode(text)` → `LinkCodes.Redeem(player, code, now)` → if a user id: `Task.Run` a scope that calls `SaveLinkAsync(userId, player)` (try/catch, log), then `Reply(player, true, BotResponses.McLinkedLines)`; return. A wrong code: return silently (no talk answer to a code-looking whisper).
- [ ] **Step 6: `McLinkedLines`** (4–6 lines, {0} = who; e.g. « C'est noté, {0}. Ton compte Discord et toi, c'est officiel. Je surveille les deux. »), added to the checks' Minecraft pool list.
- [ ] **Step 7: The `link` subcommand** — options `name` (`[MaxLength(16)]`), `remove` (bool), `player` (owner-only target for `remove`). Defer ephemeral. No option: show the member's link or « Aucun compte lié ». `remove`: delete own (or, for the owner, `player`'s) link. `name`: refuse a name not matching `^[A-Za-z0-9_]{3,16}$`; refuse Papa's/Tata's name unless `Context.User.Id` is `AvailabilityService.OwnerId`/`BotResponses.TataId` (via `KinOfPlayer`); refuse a name linked to another member (« Ce pseudo est déjà lié à un autre compte. »); refuse when `!IsSpawned` (« Elle doit être sur le serveur pour vérifier : réessaie quand elle y est. »); else `LinkCodes.Issue` and reply « Chuchote-lui en jeu : `/msg {Username} lien {code}` (valable 10 minutes). ».
- [ ] **Step 8: Run** build and checks — Expected: pass.

### Task 5: `/minecraft invite`

**Files:**
- Create: `ProjectSYNCS/Helpers/Minecraft/McInvite.cs`
- Modify: `ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs`
- Modify: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Minecraft.cs` (`McInvitedLines`)
- Modify: `ProjectSYNCS/Commands/Minecraft/MinecraftModule.cs`
- Test: checks

**Interfaces:**
- Produces: `public enum McInviteVerdict { Go, NotAllowed, Held, AlreadyIn, AlreadyInvited }`; `public static McInviteVerdict McInvite.Decide(bool allowed, bool inGame, DateTimeOffset holdUntil, DateTimeOffset invitedUntil, DateTimeOffset now)`; `public static readonly TimeSpan McInvite.Length = TimeSpan.FromHours(1)`; `MinecraftBodyService.Invite(string player) : McInviteVerdict`.

- [ ] **Step 1: Failing checks:** `Decide(false, …) == NotAllowed`; `Decide(true, false, now+10m, min, now) == Held`; `Decide(true, false, min, now+30m, now) == AlreadyInvited`; `Decide(true, true, min, min, now) == AlreadyIn`; `Decide(true, false, min, now-1s, now) == Go`. Order of precedence exactly as listed in the enum.
- [ ] **Step 2: Run** — Expected: build error.
- [ ] **Step 3: Implement `McInvite.Decide`.**
- [ ] **Step 4: Service:** field `_invitedUntil` and `_invitedBy` (in-game name) under `_gate`. `Invite(player)`: decide under `_gate`; on `Go` set both, clear `_retryAfter`/`_failures` as `ForceJoin` does, `StartBody()`. In the presence tick, `join` and `leave` treat `now < _invitedUntil && _allowed` as « in game wanted » (OR with `verdict.InGame`); forbidding or `ForceLeave` clears `_invitedUntil`. On `"spawned"`, if `_invitedBy` is set: `Remark("invited", BotResponses.McInvitedLines, _invitedBy, force: true)` once, then clear `_invitedBy`.
- [ ] **Step 5: `McInvitedLines`** (4–6 lines, {0} = who invited; e.g. « {0} m'a invitée. Me voilà. Je reste une heure, après on verra si vous le méritez. »), in the checks' Minecraft pool list.
- [ ] **Step 6: The `invite` subcommand** — defer ephemeral; no link → « Lie d'abord ton compte avec `/minecraft link`. »; else `Invite(link.Player)` and one French line per verdict (Go: « Elle arrive. Elle reste au moins une heure. »).
- [ ] **Step 7: Run** build and checks — Expected: pass.

### Task 6: Stats (`/minecraft stats`)

**Files:**
- Create: `ProjectSYNCS/Models/Minecraft/MinecraftStat.cs`
- Create: `ProjectSYNCS/Helpers/Minecraft/McStats.cs`
- Modify: `ProjectSYNCS/Data/AppDbContext.cs`, `ProjectSYNCS/Services/Minecraft/MinecraftDataService.cs`, `ProjectSYNCS/Services/Minecraft/MinecraftBodyService.cs`, `ProjectSYNCS/Commands/Minecraft/MinecraftModule.cs`
- Migration: `dotnet ef migrations add AddMinecraftLinksAndStats` (both tables: `MinecraftLinks` from Task 4 and `MinecraftStats`)
- Test: checks

**Interfaces:**
- Produces: `public class MinecraftStat { string Key (key); long Count; }` (stored keys, append-only); `public static class McStats { const string Deaths = "deaths", Fish = "fish", Treasure = "treasure", Kills = "kills", Harvested = "harvested", Minutes = "minutes", Orders = "orders"; static string Gathered(string resource) => $"gathered:{resource}"; static string OrdersBy(string player) => $"orders:{player.ToLowerInvariant()}"; }`; `public sealed class McTally { void Add(string key, long n); IReadOnlyDictionary<string, long> Drain(); IReadOnlyDictionary<string, long> Peek(); }` (own lock); `MinecraftDataService.AddStatsAsync(IReadOnlyDictionary<string, long>)`, `StatsAsync() : Task<Dictionary<string, long>>`; `MinecraftModule.BuildStats(IReadOnlyDictionary<string, long> totals, int placesRemembered) : Embed`.

- [ ] **Step 1: Failing checks:** `McTally` adds and drains (`Drain` empties, `Peek` doesn't); `BuildStats` on an empty dictionary renders zeros and fits the caps; with 50 `orders:*` keys it lists the top 5 only and still fits.
- [ ] **Step 2: Run** — Expected: build error.
- [ ] **Step 3: Implement `McStats`, `McTally`, the model (+ `DbSet<MinecraftStat> MinecraftStats`), `AddStatsAsync` (upsert by key, one `SaveChanges`), `StatsAsync`.**
- [ ] **Step 4: Count in the service** (`private readonly McTally _tally = new();`): her own death (`McDeath.Classify` returns `McDeathKind.Self`) → Deaths; `"caught"` → Fish, plus Treasure when `kind == "treasure"`; `"killed"` → Kills; `"collected"` → `Gathered(resource)` by `count`; `"harvested"` → Harvested by `count`; an order confirmed under way (`Confirm`) → Orders and `OrdersBy(player)`; Minutes: every tick while spawned, add the whole minutes since the last tick (track `_countedUntil`).
- [ ] **Step 5: Flush** from the service's loop every 5 minutes and in `StopAsync`: `Drain()` then a scope `AddStatsAsync`; on exception log and `Add` the drained counts back.
- [ ] **Step 6: Run the migration command, then the `stats` subcommand**: defer ephemeral, `StatsAsync()` plus `_tally.Peek()` summed, places remembered from a new `MinecraftBodyService.PlacesCount` (sum of `_places` values' counts under `_gate`), `BuildStats`. Player names in the top list through `Format.Sanitize`; resource keys shown through a new `public static string MinecraftBodyService.ResourceLabel(string key)` wrapping the existing `ResourceName`.
- [ ] **Step 7: Run** build and checks — Expected: pass. `dotnet run` against the dev guild once: the migration applies on startup.

### Task 7: Docs, `/help`, version

**Files:**
- Modify: `ProjectSYNCS/Commands/Core/HelpModule.cs` (one line for `/minecraft` in a fitting field; measure page 0 stays ≤ 6000 — it is at 5435)
- Modify: `README.md`, `docs/agents/minecraft.md` (a `/minecraft` section: commands, link rules incl. family protection and no take-over, invite hold, stats keys and flush, where's `DiscordAsker`), `CLAUDE.md` (the group-count line: « `/minecraft` (7) »), `ProjectSYNCS/config.yaml` (7.26.0), `ProjectSYNCS/CHANGELOG.md` (`## 7.26.0 — <date>`, one bullet per command, English, no member names)

- [ ] **Step 1: Edit the files above.**
- [ ] **Step 2: Run** `dotnet build ProjectSYNCS -warnaserror` and `dotnet run --project tests/ProjectSYNCS.Checks` — Expected: `ALL CHECKS PASSED` (the `/help` size line still under 6000).
- [ ] **Step 3: By hand on the dev guild:** each subcommand once with her out of the game, then in; `link` end to end with a whisper; `invite` from a linked account.
