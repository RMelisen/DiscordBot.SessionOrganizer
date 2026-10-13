# Grosse tête de la semaine — design

**Status:** approved in brainstorming, 2026-10-13. Target version **6.12.0** (minor: new
subcommands + migration). Builds on the Plynling role (6.11.0,
`2026-10-13-plynling-role-design.md`).

## Goal

Every Monday at 10:00 Paris, SYNCS crowns the best quiz answerer of the calendar week that just
ended: she gives them a managed role (default name « Grosse tête de la semaine ») for the whole
week and announces it in the quiz channel, pinging the winner. Like the Plynling role it is flair
plus a ping group, a **strict mirror** (she adds and removes it so that exactly the current
champion(s) hold it), and **off until staff configure the role**.

## Decisions taken

| Question | Decision |
|---|---|
| Which week | Calendar week, Monday 00:00 – Sunday 23:59 Paris. The crown changes hands Monday 10:00. |
| Winner | Most wins, then most cailloux earned that week (`Reward`), then fastest average answer (`WonAt − PostedAt`). A perfect tie on all three shares the role. |
| Empty week | No announcement; nobody holds the role (last week's holder loses it). |
| Announcement | In the quiz channel, pinging the winner(s) only — never the role, never `@everyone`. |
| Name | « Grosse tête de la semaine » (default of `create`; staff may rename). |
| Code shape | Approach A: extract a shared `RoleMirror` engine from `PlynlingRoleService`; both roles feed it "who should hold it". |
| Off by default | No `quiz-role` configured → no announcement, no role work. Shipping changes nothing anywhere. |

## Shared role engine (refactor of 6.11.0, no behaviour change for the Plynling role)

### `Helpers/Core/RoleMirrorRules` (renamed from `Helpers/Plynling/PlynlingRoleSync`)

- `Diff`, `Counts`, `MaxRemovalsPerPass` (25), `NoManageRoles`, `RoleFacts`, `BotFacts` — unchanged.
- `Refusal(RoleFacts role, BotFacts bot, bool adopting, string command)` — `command` is the
  `/config` subgroup (`plynling-role`, `quiz-role`), used in the "role no longer exists" text.
- `DefaultName` leaves: each owner holds its own (`PlynlingRoleService.DefaultName`,
  `QuizCrownService.DefaultName`).

### `Services/Core/RoleMirror` (singleton; `IServiceProvider` not needed — it touches no DB)

`public sealed record RoleKind(string Key, string Command, string GrantedReason, string RevokedReason)`,
with two instances: `RoleKind.Plynling` (`"plynling"`, `"plynling-role"`, « Plynling vivant »,
« Plus de Plynling ») and `RoleKind.QuizCrown` (`"quiz"`, `"quiz-role"`,
« Grosse tête de la semaine », « Couronne passée »).

- `static string? Problem(SocketGuild guild, SocketRole? role, bool adopting, RoleKind kind)`
- `Task MirrorGuildAsync(SocketGuild guild, RoleKind kind, ulong roleId, Func<Task<HashSet<ulong>>> loadHolders)` —
  the whole 6.11.0 pass: problem check (logged once per **(guild, kind)**), skip on
  `!guild.HasAllMembers`, snapshot holders and members **before** awaiting `loadHolders`, `Diff`,
  apply with the removal cap, audit reasons from `kind`, per-person `try`. Never throws. One
  `SemaphoreSlim` serialises every pass of every kind.
- `Task MirrorMemberAsync(SocketGuild guild, RoleKind kind, ulong roleId, ulong userId, bool shouldHold)` —
  one person. Never throws.
- `Task<(int ToAdd, int ToRemove)?> PreviewAsync(SocketGuild guild, ulong roleId, Func<Task<HashSet<ulong>>> loadHolders)` —
  null on failure.
- `void StartMirror(SocketGuild guild, TimeSpan delay, Func<Task> pass)` — the background first
  sync after `set` / `create`.

### `PlynlingRoleService` (shrinks)

Keeps "who owns a living Plynling" (`GetLivingOwnerIdsAsync`, `HasLivingAsync`), its
`SyncGuildAsync` / `SyncMemberAsync` signatures (callers unchanged), and delegates to `RoleMirror`
with `RoleKind.Plynling`. The mascot exception (SYNCS holds the role herself) stands.

## Quiz crown

### Data

- `GuildSettings.QuizRoleId` (`ulong`, `.HasConversion<long>()`, 0 = none).
- `GuildSettings.QuizCrownedWeek` (`int`, `yyyymmdd` of the Monday that **starts** the last
  announced week; 0 = never).
- One schema-only migration `AddQuizCrown`. `GuildConfig` gains both;
  `GuildConfigService.SetQuizRoleAsync` and `SetQuizCrownedWeekAsync` (it stays the only writer of
  `GuildSettings`, so its cache stays correct).

### Pure — `Helpers/Quiz/QuizCrown`

- `CrownHour = 10` (Paris).
- `int RewardedWeekStart(DateTimeOffset now)` — the `DayKey` of the Monday starting the week whose
  champion reigns at `now`: from Monday 10:00 Paris it is last week's Monday; before it (Monday
  00:00–09:59) the Monday before that. Built on `AppTime.ToZoned`; DST-safe because it only does
  date arithmetic on the Paris calendar date.
- `(int From, int To) WeekRange(int weekStart)` — `weekStart` .. `weekStart + 6 days`, as `DayKey`s.
- `record CrownEntry(ulong UserId, int Wins, long Cailloux, double AvgMs)` and
  `IReadOnlyList<CrownEntry> Champions(IEnumerable<(ulong WinnerId, long Reward, DateTimeOffset PostedAt, DateTimeOffset WonAt)> wins)` —
  groups by winner, ranks by wins ↓, cailloux ↓, average ms ↑, returns every entry tied with the
  first on all three (empty input → empty list).

### Service — `Services/Quiz/QuizCrownService` (singleton, `IServiceProvider` + scope per unit)

- `DefaultName = "Grosse tête de la semaine"`.
- `Task<HashSet<ulong>> LoadChampionsAsync(ulong guildId, int weekStart)` — `QuizService` reads the
  week's won rounds (`GuildId`, `Day` between the range, `WinnerId != 0`, in SQL; `WonAt` non-null),
  `QuizCrown.Champions` ranks them.
- `Task TickAsync(SocketGuild guild, DateTimeOffset now)` — called by `QuizMasterService.TickAsync`
  for every guild, inside the existing per-guild `try`:
  1. `QuizRoleId == 0` → return (off).
  2. `week = RewardedWeekStart(now)`. If `week > QuizCrownedWeek`: **save `QuizCrownedWeek = week`
     first**, then mirror the role now, then announce (one attempt; a failed post is logged,
     never repeated).
  3. Otherwise mirror at most hourly (`CooldownGate<ulong>` per guild, 1 h) — fixes a hand-made
     change or a rejoin within the hour.
- `Task<string> CrownNowAsync(SocketGuild guild)` — for `/debug quiz action:Crown`: same as step 2
  for the last finished week, ignoring and not writing the marker; returns a French report.
- Announcement: only when the guild has a quiz channel (`QuizChannelId`) the bot can send in and
  the champion list is non-empty. One line from `QuizCrownLines` (one champion: `{0}` mention,
  `{1}` wins) or `QuizCrownSharedLines` (`{0}` the mentions joined « , » / « et »), picked with
  `DailyRotation` keyed by the week's day number; `QuizCrownOwnerLines` when the sole champion is
  `AvailabilityService.OwnerId` (adoration, never a pinch — same rule as `QuizOwnerWinLines`).
  Sent with `new AllowedMentions { UserIds = champions }` so only they are pinged. Lines follow
  `docs/syncs-voice.md`; no stray braces.

### `/config quiz-role create|set|clear`

Identical behaviour to `plynling-role` (6.11.0 spec), through shared static helpers in
`ConfigModule` (`CreateRoleAsync`, `SetRoleAsync`, `ClearRoleAsync` taking the `RoleKind`, the
default name, the setter and the holder loader), so both subgroups stay identical — including the
orphan cleanup and the never-throwing preview. The first background sync for `quiz-role` mirrors
the current champion (`RewardedWeekStart(now)`), so a role created mid-week goes straight to last
week's winner — without an announcement: storing the role also raises `QuizCrownedWeek` to the
reigning week, in the same write, so the next announcement is the next Monday's.

`/config show` gains « Rôle Grosse tête » — pill or « non configuré », plus the ⚠️ problem line.
Card: 20 of 40 components after this.

### `/debug quiz action:Crown`

New `QuizAction.Crown` (`[ChoiceDisplay("Couronner la grosse tête")]`), owner only, calls
`CrownNowAsync`. Refuses (French) when no quiz role is configured.

## Docs and version

- `docs/agents/quiz.md`: *Grosse tête de la semaine* section — week rule, ranking, marker saved
  before the post, hourly mirror, off until configured, `/debug quiz action:Crown`.
- `docs/agents/plynling.md`: the Plynling role section points at `RoleMirror` for the mechanics.
- `docs/agents/admin.md`: `quiz-role` in the list; the "owned, not configured" bullet covers both roles.
- `/help` (31 chars of headroom: measure; fold to « les rôles Plynling et Grosse tête » if needed)
  and `README.md`.
- `config.yaml` **6.11.0 → 6.12.0**; `CHANGELOG.md` entry.

## Verification

Build `-warnaserror`; scratch console: `RewardedWeekStart` (Monday 09:59 vs 10:00, Sunday night,
the 2026-10-25 DST change), `Champions` (each tie-break level, a perfect tie, empty), and the
renamed `RoleMirrorRules`; `/help` measure. Dev guild: `quiz-role create` → last week's winner gets
it silently; `/debug quiz action:Crown` → announcement pings only the winner; the Plynling role
still behaves exactly as in 6.11.0.

## Out of scope

History of past champions, a crown mark on `/quiz leaderboard`, a cailloux reward for the crown,
mid-week re-crowning, a per-guild crown hour.
