# `/admin`, `/config`, `/debug` and permissions

The three authorization models are summarised in the root `CLAUDE.md`. Don't conflate them.

## `/admin`

Holds `xp`, `plynling` (`rename`, `resurrect`, `passion-reset`, `cure`), `pebble`, `stats`,
`dashboard`, `warn`, `warnings`, `unwarn`, `mute` and `unmute`. **Guarded once per handler by `SessionPermissions.IsStaff`, in code only — no
`[DefaultMemberPermissions]`**: a permission bit can't express "ManageGuild, plus the owner", and on a
server where the owner lacks ManageGuild Discord would hide *and refuse* the command for him. So the
commands are visible to everyone; `IsStaff` is the only gate. `/plynling freeze|thaw user:` uses the
same check.

- **`/admin xp add|remove` and `/admin pebble`** are ephemeral, capped, send no notification and
  **refuse bots** (`XpTracker` skips bots everywhere else, so a topped-up bot would be a row nothing
  else can produce). XP adjustments **fire no level-up card** — the card celebrates something earned.
  `pebble` goes through `PebbleService.AdjustAsync`, which clamps at 0.
- **`/admin plynling cure`** sends no DM on purpose. Staff freeze/thaw/rename **do** DM the owner
  (`plynling.md`).
- **`/admin stats`** is `AdminStatsService`, reads only: Plynlings are loaded `AsNoTracking` and
  settled in memory, so an unnoticed death counts as a grave without the stats ever writing it.
  Rendered by the static `AdminCards.BuildStats`.
- **`/admin dashboard`** — see `economy.md`.
- **`/admin mute|unmute`** are a Discord timeout, ephemeral. `AdminModule.MuteRefusal` checks, in
  order, what Discord would otherwise answer with a bare 403 — bot lacks `ModerateMembers`, target
  is the guild owner, bot's top role not above the target's — and names it, so a failed mute says
  what to fix (an Administrator target is refused by `mute` only). **The invoker's rank is checked
  too**: the bot acts with its own role, so without it a ManageGuild holder could silence someone
  ranked above them. `AvailabilityService.OwnerId` and the guild owner are exempt from that check.
  **`OwnerId` can't be muted by anyone else**, whatever their rank; `unmute` has no such guard.
- **`/admin warn|warnings|unwarn`** — `MemberWarning` rows through `WarningService`. **Private by
  design**: the person gets a DM (`BotResponses.WarnDmLines` + `AdminCards.BuildWarnDm`) and staff
  an ephemeral confirmation; nothing goes in the channel. **The DM never names the moderator**: it
  comes from « l'équipe ». A warning that reads as one person's grudge invites a feud with that
  person, not a change of behaviour. `ModeratorId` is stored and shown to staff only.
  - **Rows are never deleted.** `unwarn` sets `RevokedAt`; a warning older than
    `WarningRules.Lifetime` (90 days) stops counting. `WarningRules.IsActive` is the one
    definition of "counts", evaluated in memory (SQLite can't compare `DateTimeOffset`).
  - **Escalation is offered, never applied.** From `MuteSuggestionThreshold` (3) active warnings,
    the confirmation carries a `warn:mute:{userId}` button for a one-hour timeout, handled in
    `AdminComponentHandler` through the same `AdminModule.MuteBlock` / `TimeOutAsync` as
    `/admin mute`, so the two can't drift.
  - Refuses bots, yourself, `OwnerId`, people not on the server, and anyone ranked at or above the
    invoker (`OutranksInvoker`, shared with mute). No Discord permission is needed.
  - Closed DMs are not a failure: the warning is recorded and the confirmation says it wasn't
    delivered. `unwarn` is silent toward the person.
  - Kept apart from `/shame` on purpose: a real moderation record on a public joke wall would
    make the warning a humiliation and the wall less of a joke.

## `/report` and the monthly digest

`sessions`, `polls`, `community`, `syncs`, `activity`, `commands`, `quiz`, `plynlings`, `emotes`,
`sleeping`: her reports to the staff. Same gate as `/admin` (`IsStaff` per handler, no
`[DefaultMemberPermissions]`, guild-only).

- **Posted in the open, with every name** (no-shows, latecomers, the busiest, the ones going
  quiet, the sleepers): the owner's call, and the one exception to « absences are a count, never
  names » (scheduling, *Roll call*). Only the refusals are private. Every card still takes `names`:
  `false` drops every naming field (the checks assert no `<@` survives), for any future place that
  must not name; nothing passes `false` today.
- **Her line on top of the card**, as on `/health`: `ReportCards.WithHeadline` puts it first in the
  description (left out rather than pass 4096 / 6000). `ReportCards.DutyPool` picks the pool by who
  asked or clicked: `OwnerReportDutyLines` (Papa, on duty), `TataReportLines` (warm, the mute
  rights), `StaffReportLines` (any other staff member: suspicious, « pourquoi tu veux savoir
  ça ? »). A period or 30/60/90 click re-picks for the person who clicked, since the card is public
  and any staff member may change it. The digest has no line: its own opens the DM.
- **One path**: `ReportService.BuildAsync(kind, guild, window, names)` serves the command, the
  period buttons (`report:win:{kind}:{period}:0`, `ReportComponentHandler`, deferred first,
  checks `IsStaff` on every click, since anyone can see the buttons) and the digest. `ReportKind`
  names are in custom-ids: append only, never rename.
- **The live guild becomes plain records in `ReportService`** (`ChannelInfo`, `ServerEmote`,
  `MemberSnapshot`, the registered slash paths) before it reaches a helper. When the guild is
  unavailable (gateway down, the digest firing during a reconnect) those snapshots are empty and
  the reports render without their channel, emote or member lists: never a crash.
- **Windows are `Helpers/Core/ReportWindow`**: inclusive Paris day keys. Bucket tables filter
  `Day` in SQL; DateTimeOffset rows (sessions, polls, `/tonight`, outages) are read whole per guild
  and windowed in memory. Opens on 30 days; the buttons are `StatsPeriodUi`'s three.
  - **« Depuis toujours » reads the daily buckets from day 0, not the totals tables**, unlike
    every leaderboard: a report needs each day's shape, which totals don't have. So it covers only
    what was counted since each table's buckets existed, and its label says « depuis le début des
    relevés ».
  - Trends compare against `ReportWindow.Previous`: the same number of days before, the previous
    calendar month for the digest, none for all time.
- **The arithmetic is pure** (`SessionReport`, `PollReport`, `CommunityReport`, `SyncsReport`,
  `ActivityReport`, `CommandReport`, `QuizReport`, `PlynlingReport`, `EmoteReport`,
  `SleepingReport` in `Helpers/`), the services only fetch rows, the cards (`ReportCards`) are
  static. The checks feed the helpers cases and measure every card at its worst; re-run them after
  adding a field. A list that can grow without bound (silent channels, unused emotes, never-used
  commands) goes through `ReportCards.Fit`, which keeps whole items only and counts the rest
  (« … et N autres »), so an emote's `<:name:id>` is never cut in half.
- **Rules worth knowing**:
  - a session belongs to the day of its `ScheduledAt`, and so does a cancellation (there is no
    `CancelledAt`);
  - attendance goes through `SessionAttendance.IsMissed` / `CameLate` (scheduling, *Roll call*);
  - a poll « became a session » when a non-cancelled session sits on one of its slots: nothing
    links them, it is the match `PollModule` uses against duplicates;
  - « active » is any XP, reaction or voice minute that day (messages are counted per channel and
    hour, never per person);
  - uptime is measured over the days the Pi recorded its temperature;
  - a quiz round counts only if its card went out (`MessageId != 0`), in `quiz` and `community`
    alike;
  - `plynlings` never counts the mascot (`PlynlingMascot.Is`), and cannot report abandons: an
    abandoned Plynling is hard-deleted with its history. Needs are settled in memory
    (`AsNoTracking`, never saved), as `/admin stats` does;
  - `emotes`: an emote is the server's own when its id is in the live guild's list; the others
    were counted from other servers (`EmoteTracker` counts them too);
  - `sleeping` has no window: a silence of 30, 60 or 90 days, on its own buttons
    (`report:sleep:{days}`, its own verb so it never collides with a period row). « Last seen » is
    the last `MemberDailyStat` day with any counter above 0; « jamais » means never since
    2026-08-09, when the XP buckets became reliable (`ResetXpTotals`). Members who joined less than
    N days ago are left out, bots always. Activity in excluded channels earns nothing, so someone
    who only talks there looks asleep (said in the footer).
- **Joins and leaves** (`MemberFlowDailyStat`, `MemberFlowTracker` on `UserJoined` / `UserLeft`,
  bots skipped): daily buckets with **no totals table**, counted from 7.5.0 on. Nothing needs an
  all-time figure older than the buckets, and Discord shows the member count.
- **Activity counting** (`activity`, `commands`): the one batched counter. A database write per
  message would be the bot's first, on a Pi with no `SQLITE_BUSY` handling, so the counts go to
  `ActivityCounter` (a singleton holding its own lock, every method synchronous and cheap) and
  `ActivityFlushService` (its own 5-minute loop) writes them.
  - Fed by the message fan-out right after the shortcut return (a typed shortcut is counted as a
    command, under its `;` key, by `PlynlingShortcutService`), by `SlashCommandExecuted` (handlers
    run `RunMode.Async`, so success or failure only arrives there; DMs count under guild 0), and by
    `VoiceXpService`'s sweep.
  - The flush opens one scope per guild in its own `try`; a failed guild's counts are put back
    (`Restore`) for the next pass. It flushes once more on stop, and is registered before
    `BotService` so it stops after everything that feeds it. A crash loses at most 5 minutes.
  - `ActivityHourStat`, `ChannelDailyStat`, `CommandDailyStat`: buckets only, no totals, counted
    from 7.6.0 on. A thread counts under its parent. Excluded channels are counted and marked 🚫.
    Voice is presence (everyone connected, muted included, AFK room skipped), unlike
    `MemberDailyStat.VoiceMinutes`, which counts XP-eligible minutes only.
  - `CommandDailyStat.Command` is `CommandPath`'s key (`/pl` folded into `plynling`), a stored
    string, so append-only: a renamed command keeps its old rows under its old name.
- **The monthly digest** (`MonthlyDigestService`, its own hourly loop): from 09:00 Paris on the
  1st, two digests for the previous calendar month, by DM: Papa's (`OwnerId`) and Tata's
  (`BotResponses.TataId`, the server admin, not technical). Each is:
  - **an opening line plus « Le mois en bref »** in one message: a few figures and « À regarder »
    (`ReportService.BuildBriefAsync`, `ReportCards.BuildOwnerBrief` / `BuildTataBrief`). Papa's
    figures are his choice: commands (uses, errors, never used), the Pi, messages without a
    specific answer, the quiz. Tata's: members (active, joins, leaves, sleepers), sessions
    (attendance, the best-attended weekday), when the server talks, silent channels, unused emotes,
    the three most active;
  - **then the reports of `ReportKinds.Digest(recipient)`**, one embed per message (several in one
    could pass 6000), each in its own `try`: Papa `syncs`, `commands`, `quiz`; Tata `community`,
    `sessions`, `activity`, `sleeping`. Papa also gets the chat-answer counts over 30 days (as
    `/debug answers`: they are kept by rolling period) and `UnansweredLog` as a file (in memory:
    since the last restart).
  - **« À regarder » is `Helpers/Admin/DigestBrief`**, pure thresholds (constants there): a
    command failing ≥ 2 times and > 5 % of its uses, power cuts, crashes, a heat record, the
    unanswered share up ≥ 5 points, « difficile » found ≥ 80 % or « facile » < 30 % (≥ 5
    questions); for Tata, active members down ≥ 20 %, more leaves than joins, sleepers > 10 %. **A
    comparison is only made when the month before has data**, so a new counter's first month
    raises nothing. Nothing to flag shows a `DigestQuietLines` line.
  - Lines: `OwnerDigestLines`, `TataDigestLines` ({0} the month), `DigestQuietLines`, all picked by
    `DailyRotation`, since a once-a-month pool would gain nothing from the picker.
  - Timing is pure, in `Helpers/Admin/MonthlyDigest`.
  - `report-state.json` (beside the DB) holds the last month sent **to each** (`LastMonth` for
    Papa, a name kept from before; `TataLastMonth`), written **before** sending. They are claimed
    and sent apart, so Tata's closed DMs never cost Papa his digest.
  - No file, or no field yet for a recipient, means the previous month is marked sent without
    sending, so a deploy never DMs a surprise.
  - `/debug digest [version]` sends last month's now, Papa's or a preview of Tata's, **to Papa**,
    leaving the state alone.

## `/config`

A flat `/config show` (a static Components V2 card, `ConfigCards.Build`) plus one subgroup per
setting: `excluded-channels add|remove`, `idle-channels add|remove`, `shame-voters add|remove`,
`moderator-role set|clear`, `plynling-role create|set|clear`, `quiz-role create|set|clear`,
`game-channel set|clear`,
`main-channel set|clear`, `quiz-channel set|clear`, `bots-channel set|clear`. Three levels is
Discord's maximum nesting. (`/shame` is flat only because it had to stay invokable bare; nothing here
needs that.) Every handler goes through `ConfigModule.BeginAsync` (`IsStaff`, then an ephemeral
defer); no `[DefaultMemberPermissions]`, same reason as `/admin`.

**Two rules, by the shape of the setting — and one exception.** An unconfigured guild behaves exactly as before either way.

- **Lists are additive to the code, never a replacement.** `GuildExcludedChannel`,
  `GuildIdleChannel` and `GuildShameVoter` hold only what `/config` added; `XpTracker.ExcludedChannels`,
  `AmbientService.IdleChannelIds` and `ShameModule.ExtraVoters` stay in force regardless, each exposed
  read-only as `Hardcoded…` for the refusals and the card. No config change can *remove* an exclusion
  or revoke a voting right: `remove` refuses a hardcoded entry outright (rather than appearing to
  work), and `add` refuses one too (rather than storing a redundant row that could drift). The
  moderator role and `shame-voters` only widen who may vote on `/shame`; voters refuse bots.
- **The bots channel** (`GuildSettings.BotsChannelId`) is a default like the game channel:
  `BotsChannel.DefaultId` (« spam-bots ») when it belongs to the guild, else the game channel.
  Resolved only through `BotsChannel.ResolveAsync`.
- **Single channels replace their default.** `GuildSettings.GameChannelId` / `MainChannelId`, zero
  meaning unset: a destination grants and revokes nothing, so a configured one simply wins, and
  `clear` (or setting the default itself, stored as zero) falls back. `set` refuses a channel the bot
  can't view and send in — a silent destination would otherwise fail on every use with nobody told.
  **`QuizChannelId` is the exception: it has no default**, so zero means no quiz, `clear` turns it
  off, and it is per guild (see `quiz.md`).
- **The mirrored roles are owned, not configured.** `GuildSettings.PlynlingRoleId` and `QuizRoleId`,
  zero meaning none (for the quiz role, zero also turns the weekly crown off). SYNCS decides who holds
  them (a strict mirror — see `plynling.md` and `quiz.md`; both go through `RoleMirror`), and the two
  subgroups share one create / set (`ConfigModule.CreateManagedRoleAsync` / `SetManagedRoleAsync`),
  so they can't drift. `set` can take a role away from
  people: it refuses `@everyone`, managed roles, roles at or above hers and any role carrying
  permissions, and its reply says how many will gain and lose it. `create` makes a mentionable,
  permission-less role and refuses while a configured role still exists (no orphan from a second
  click). `clear` only stops managing it: it touches no member and deletes nothing. After creation
  she never edits the role — name, colour and position are staff's.

**Main and idle channels are home-guild only.** `MorningGreetingService` and `AmbientService` keep
one state each, so they live in `Helpers/Core/HomeGuild.Id` and read that guild's config
(`MorningGreetingService.MainChannelIdAsync` / `MainChannelOf`, shared by both). Elsewhere those
subcommands refuse rather than store a row nothing reads. The main channel always counts as an idle
channel wherever it is set. The game channel is per guild (`PlynlingAnnouncer.ResolveGameChannelAsync`),
so a dev guild gets announcements once it configures one.

**`GuildConfigService` is a singleton that reads the database — the cache is why.** It takes
`IServiceProvider` and scopes per unit of work (never inject `AppDbContext`). The cache is
load-bearing: `XpTracker`'s exclusion check runs on *every* message, before `TryClaim`; uncached, that
is an EF scope and a query per message on a Raspberry Pi. It's cheap to keep correct because this
service is the only writer: any write drops that guild's entry. A failed read degrades to
`GuildConfig.Empty` and is **not cached**, so a transient fault can't pin a guild as unconfigured.

## `/debug` — owner only

`tell`, `dm`, `absent`, `plynling`, `emotes`, `digest` (last month's digest in your DMs now, yours or a preview of Tata's, see
*`/report` and the monthly digest*), `quiz` (post or close a pop quiz now, see `quiz.md`),
`sticker` (post any of her sticker strips here, bypassing the gate, see `personality.md`),
`answers` (which kinds of chat answer she gives most, and what she couldn't answer specifically,
see `personality.md`, *Answer stats*),
plus the Plynling testing tools (`event`, `stress`, `modifier`, `sweep`, in `plynling-events.md`), each comparing `Context.User.Id` to
`AvailabilityService.OwnerId` inline and replying ephemerally — except `answers`, which posts its
result in the open. `DebugModule` carries no
`[CommandContextType]` (it never reads `Context.Guild`); `/debug plynling` checks for a DM itself.

**`/debug tell`'s destination is an autocompleted string, not a channel option** — that's what makes
it work from a DM (the native channel picker resolves against the invoking guild, which a DM lacks).
It replaced the native picker rather than sitting beside it. `ChannelAutocompleteHandler` offers only
channels the bot can **send** in, and suggests nothing to anyone but the owner (the command is
registered globally). `DebugModule.ParseChannelRef` turns the text back into a channel; it's pure,
because a misread silently sends the owner's message to the wrong place, and a *name* is accepted
only when exactly one channel matches. `tell` keeps its own `MaxMessageLength` (1500) with an
explicit refusal — it truncates a body rather than rejecting a title, so it predates `InputCaps`.

`absent` toggles `AvailabilityService`'s absent flag; mentions of the owner while absent are
forwarded to him (a bounded 200-entry map, `AllowedMentions.None` since it quotes others verbatim).
