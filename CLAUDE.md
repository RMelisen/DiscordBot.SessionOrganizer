# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Project S.Y.N.C.S. — a French-language Discord bot for scheduling gaming sessions, polls and
votes, with a personality, an XP system and a virtual-pet game (Plynlings). See `README.md` for
the user-facing feature list.

This file holds only what applies **everywhere**. Each subsystem has its own notes — **read the
matching file before changing that subsystem**; the rules there are as binding as these.

| Working on… | Read first |
|---|---|
| Chatter, reactions, `MessageCues`, favouritism, rivals, `/yesno` | `docs/agents/personality.md` |
| "good bot" / "bad bot" verdicts, `BotFeedbackTracker`, `/goodbot` | `docs/agents/bot-feedback.md` |
| XP, `/level`, `/leaderboard`, voice XP, `/emotestats` | `docs/agents/xp.md` |
| `/shame` | `docs/agents/shame.md` |
| Sessions, polls, votes, giveaways, `ReminderService` | `docs/agents/scheduling.md` |
| Pop quiz, `QuizBank`, `/quiz leaderboard` | `docs/agents/quiz.md` |
| Plynlings (life, care, visits, relations, art) | `docs/agents/plynling.md` |
| Plynling traits, stats, personality, events | `docs/agents/plynling-events.md` |
| Writing any Plynling line | `docs/plynling-writing-style.md` |
| Writing any line SYNCS says in her own voice | `docs/syncs-voice.md` |
| Any picture of SYNCS (avatar, emotes, poses, moods) | `docs/syncs-appearance.md` |
| Cailloux, inventory, items, icons, cosmetics, `/admin dashboard` | `docs/agents/economy.md` |
| `/admin`, `/config`, `/debug`, permissions | `docs/agents/admin.md` |

When you add a rule, put it in the subsystem file unless it genuinely applies across subsystems.
Write the rule and its reason, not the story of how it was discovered — git keeps the history.

## Commands

```bash
cd ProjectSYNCS
dotnet build                     # net10.0
dotnet run                       # needs Discord:Token
dotnet ef migrations add <Name>  # migrations are applied automatically on startup

dotnet user-secrets set "Discord:Token" "<token>"           # dev credential
dotnet user-secrets set "Discord:DevelopmentGuildId" "<id>" # instant registration
```

Leave `Discord:RegisterCommandsGlobally` at `false` in dev: guild-scoped commands register
instantly, global ones take up to an hour. Guild registration runs with `deleteMissing: true`, so
the dev guild's command list is replaced by whatever the assembly declares on every start.

The bot needs the two privileged intents (`GuildMembers`, `MessageContent`) enabled in the Discord
developer portal. Without `MessageContent` the personality subsystem silently reads empty strings.
**`AlwaysDownloadUsers` must stay on** — the intent only *permits* the member download, the flag
performs it. Without it `Guild.GetUser` returns null for anyone not seen since the last restart,
and every avatar on `/level`, `/leaderboard` and `/shame` falls back to Discord's generic logo.
First thing to check if those placeholders reappear.

There is no test project and no CI. Verify by building and, when it matters, running against the
dev guild. Notes that mention "the harness" refer to the owner's scratch checks, which are not in
this repo.

## Language

**Command and option names are English; every other user-facing string — descriptions, replies,
embeds, button labels, choice display names, errors — is French.** Code, comments and logs are in
English. Renaming a command or option changes what people type: do it rarely, and in one batch.

## Architecture

`Program.cs` is the composition root: DI wiring, `MigrateAsync()`, then the hosted services.

- **`BotService`** — gateway login, command registration, interaction dispatch, and the gateway
  fan-out (`MessageReceived`, `ReactionAdded`, `ReactionRemoved` to the trackers and services).
  It owns every message and reaction subscription; connection-lifecycle hooks (`Ready`,
  `Connected`, `Disconnected`) live in the service that needs them (`PresenceService`,
  `AmbientService`, `ApplicationEmojiService`, `PlynlingMascotService`, `PiHealthService`).
- **Seven `BackgroundService` loops**, each with **its own interval on purpose** — never share one:
  `ReminderService` (5 min, load-bearing — see scheduling), `PresenceService` (5 min, cosmetic),
  `VoiceXpService` (1 min), `GiveawayDrawService` (1 min), `QuizMasterService` (1 min, see quiz),
  `PlynlingSweepService` (hourly), `PiHealthService` (2 min — the Pi's temperature, how the last
  run ended, gateway outages; see personality, *Her body*).
- **`MorningGreetingService`** has no interval: it sleeps until one random slot per morning
  (8:00–10:00 Paris, `Helpers/MorningGreeting`) — see personality.
- **`AmbientService`** (10 min, cosmetic, its own interval like the loops above): the 3 a.m. line
  and the scolding for answering it, idle fillers and the wake-up line after a restart — see
  personality, *Ambient life*.
- **`ApplicationEmojiService`** runs once, on the first Ready (uploads item icons — see economy).
  **`PlynlingMascotService`** runs on every Ready (see plynling).

**`PresenceService`** hooks `Ready` itself, because Discord drops the presence on every reconnect.
Lines go out via `SetCustomStatusAsync`, **not** `SetGameAsync`: a custom status renders verbatim,
whereas `ActivityType` verbs are prepended and localised to the *viewer* ("Watching le vide").
`SetGameAsync` with `ActivityType.CustomStatus` compiles and renders nothing — a custom status
carries its text in `State`, not `Name`.

Layers: `Commands/` (slash modules + `static` embed/component builders), `Interactions/`
(`Components/` handlers, `Modals/` DTOs, `Autocomplete/`), `Services/` (EF repositories +
behaviour), `Models/`, `Data/AppDbContext.cs`, `Helpers/` (pure logic — put anything you want to
be checkable without a gateway here).

**Commands are grouped by whose thing it is**: `/plynling` the creature, `/inventory` the person's
belongings, `/admin` moderation actions, `/config` settings, `/debug` the owner's tools. Discord
allows **25 subcommands per top-level command** and the 26th throws at registration on startup —
count before adding to `/plynling` (15) or `/inventory` (9); a new batch goes to the group that owns
the thing, or a new group. `/pl` is a second registration of `/plynling`'s commands (see
`plynling.md` before touching `PlynlingModule`). Component handlers live in `Interactions/Components/`; the module keeps
the commands and the static card builders those handlers render through.

**DI lifetimes are not arbitrary.** `AppDbContext` and the services wrapping it are **transient**.
Anything holding in-memory state (cooldowns, bounded sets, line history, caches) is a
**singleton** — registering one as transient silently drops its state (`ResponsePicker` would
forget every line, `ReactionService` would react to every message).

**Singletons never inject a DB service.** They take `IServiceProvider` and open
`CreateAsyncScope()` around each unit of work; injecting a transient would pin one `AppDbContext`
for the process lifetime. Match this in any new background or gateway-driven work.

All in-memory state (drafts, cooldowns, line history, trade offers, visit stories…) resets on
restart **by design**.

## Conventions that will bite you

### Data

- **SQLite cannot translate `DateTimeOffset` comparisons.** Filter booleans/ids in SQL,
  `ToListAsync()`, then apply date windows and ordering **in memory** (see
  `EventService.GetActiveEventsAsync`). `.Where(e => e.ScheduledAt > now)` throws at runtime.
- **Snowflakes are `ulong`; SQLite integers are signed.** Every snowflake property needs
  `.HasConversion<long>()` in `OnModelCreating`. A derived model property needs `[NotMapped]`
  (see `EmoteStat.Markup`) or EF demands a migration for a column that should not exist.
- **Anything stored by int or by string key is append-only**: enums stored as ints
  (`PlynlingSpecies`, `PlynlingBond`, `JournalKind`, `DeathCause`, `UptimeEventKind`, passions) and stored keys
  (item keys, cosmetic keys, badge keys, economy metric keys). Inserting in the middle or renaming
  silently turns or orphans every existing row.
- **Totals + daily buckets** is the one pattern for dated rankings: an all-time totals table plus a
  per-day table keyed by `Day`, an `int` `yyyymmdd` from `AppTime.DayKey` (so windows filter in
  SQL), both written in the same call. Instances: `EmoteStat`/`EmoteDailyStat`,
  `BotFeedback`/`BotFeedbackDailyStat`, `MemberXp`/`MemberDailyStat`,
  `ShameRecord`/`ShameDailyStat`, `QuizStat`/`QuizDailyStat`. **The buckets do not sum to the totals and must not be made
  to** — everything before the buckets existed lives only in the totals. A removal always
  decrements *today's* bucket. Follow this shape for any new dated leaderboard.
- **No `HasMaxLength`**: SQLite doesn't enforce it. Cap at the input instead (below).
- **Migrations are schema-only, except five deliberate data migrations** riding apply-on-startup:
  `ResetMemberXp` and `ResetXpTotals` (XP wipes), `AddPlynlingPassions` (backfill),
  `PrepareProdLaunch` (one-guild launch prep) and `SwapLaunchPairGenders` (details of the last three
  in `plynling.md`). Rules learned from them:
  a wipe **resets the reward and keeps the record** (`UPDATE … SET TotalXp = 0`, never deleting
  rows that also carry facts like `ReactionsUsed`); a data migration's `Down` cannot restore
  anything, so it is for one-off corrections only; never generate a SQL script from a migration
  that computes "now" in C#.

### Time and culture

**Never use `DateTime.Now`.** Production runs in UTC; wall-clock handling goes through
`Helpers/AppTime` (`Europe/Paris`, DST-aware via `TryParseWallClock`). Store instants as UTC
`DateTimeOffset`, show them as Discord `<t:unix:…>` timestamps. **Globalization must stay on**:
`InvariantGlobalization` is `false` and the Dockerfile installs `libicu72`, for `Europe/Paris` and
`fr-FR` formatting.

### Interactions and custom-ids

- **`[ComponentInteraction]` / `[ModalInteraction]` inside a `[Group]` module need
  `ignoreGroupNames: true`**, or Discord.Net prefixes the group name and the handler never fires —
  no error, no log.
- **Custom-ids are a contract across files** (e.g. `PollModule` builds a `schedule:finalize:…`
  modal for `ScheduleModule`'s handler; `/vote list` reuses `poll:republish`). Renaming one means
  grepping the whole project.
- **Every button row needs its own custom-id verb.** Discord rejects a message carrying the same
  custom-id twice (`COMPONENT_CUSTOM_ID_DUPLICATED`, **disabled buttons included**) — this crashed
  prod. Rows that encode the same state collide by construction, so give each row its own verb
  (`…:win:`, `…:view:`, `…:page:`) and delegate to one shared `ShowAsync`.
- **Modal DTOs and hand-built `ModalBuilder`s must stay in sync.** Several paths build modals by
  hand to pre-fill them (`ScheduleModule.BuildEditModal`, `OnRetryAsync`,
  `PollModule.OnCategoryPickedAsync`); a field added or renamed in only one place silently fails
  to bind.
- **Card builders are `static` and shared** by the module, the component handlers and the
  background loops — change rendering in one place and every re-render follows.
- **Every module that reads `Context.Guild` carries
  `[CommandContextType(InteractionContextType.Guild)]`**: `config.yaml` ships
  `register_globally: true`, and global commands are DM-enabled by default, where `Context.Guild`
  is null. (`[EnabledInDm(false)]` is obsolete in Discord.Net 3.20
  and fails the build under `-warnaserror`.)

### Discord's hard caps

Exceeding any of these throws at **send** time, often with nothing in the logs naming the length:
25 options per select (`.Take(25)`), 5 buttons per row, 80-char button labels, 100-char select
labels, 1000-char scheduled-event description, 2000-char message, embed title 256, field value
1024, **6000 per message's embeds in total**, **40 components per Components V2 message counting
the whole tree** (Discord.Net throws in `ComponentBuilderV2.Build()`). Keep builders `static` and
Context-free so they can be measured, and **re-do the component sum before adding anything** to a
V2 card. `/help` and `/plynling help` are close to the 6000 cap — measure before adding.

**Cap user-supplied text at the option**, not at the point of use: `[MaxLength(n)]` on slash
options (default 6000) and `maxLength:` on `[ModalTextInput]` (default 4000). Discord then refuses
the input client-side and nothing over-long reaches the database, where it would break every later
re-render. Numbers live in `Helpers/InputCaps` (`Title` 150, `Prize` 200, `Description` 1000,
`Question` 400); `Title` is used at several modal sites that must agree.

### Components V2

A message with `MessageFlags.ComponentsV2` may have **no content and no embeds**, must
**re-assert the flag on every `UpdateAsync`**, and must send with **`AllowedMentions.None`**: a
`TextDisplay` is real content, so `<@id>` in one genuinely pings (an embed got inert mentions for
free). `AllowedMentions.None` keeps the clickable pill while silencing it. Avatars go through
`Helpers/AvatarUi`.

### Mentions and hostile text

- **Relayed text never becomes a mass-ping vector**: anything sent on someone's behalf passes
  `new AllowedMentions(AllowedMentionTypes.Users)` and quotes through `MessageFormat.Quote`. The
  absence notice forwarded to the owner uses `AllowedMentions.None`.
- The few messages **meant** to ping narrow to users (giveaway winners) or to one id (the Plynling
  visit knock) — never roles or `@everyone`. `BotChat.PostWithTypingAsync` takes an optional
  `AllowedMentions` for this.
- **User-chosen names and texts are hostile input** (Plynling names, taught passions): render
  through `Format.Sanitize` and send with mentions off.

### Reliability

- **Discord side effects must never break the flow.** Swallow and log (`SessionEventSync` degrades
  silently without Manage Events; reminder DMs catch `CannotSendMessageToUser`).
- **An interaction must be answered within 3 s, and the Pi's database can outrun that.** A handler
  that reads or writes the database before it can show anything defers first (`DeferAsync`, then
  follow-ups / `ModifyOriginalResponseAsync`). Otherwise the work is saved while the click looks
  dead — « Accueillir » on a visit did exactly that.
- **An in-memory claim taken before a save is released if the save throws**, or the person is
  refused for something that never happened.
- **An exception escaping a hosted loop stops the whole bot** (default `StopHost`). Every sweep
  catches **per item**, not per pass, so one bad row doesn't stop the batch — and
  `BackgroundServiceExceptionBehavior.Ignore` is *not* the fix (it leaves the loop silently dead).
  Anything added to a sweep goes inside the existing per-item `try`. The real exposure is DB writes
  (`SQLITE_BUSY` under write contention on the Pi).

### Shared helpers — use them, don't re-inline

- **`Helpers/CooldownGate<TKey>`** ("claimed recently?") and **`Helpers/BoundedSet<T>`** ("seen
  before?"). They share the mechanism, **never the policy**: each service owns its own instances
  and durations, and two trigger populations never share one gate. `CooldownGate` owns its lock;
  `BoundedSet` owns **none**, because callers need its add to be atomic with their own state.
- **`Helpers/BotChat`** is the single send path for her chatter (typing pause, clamped inside
  Discord.Net's 3 s `HandlerTimeout`, plus swallow-and-log). **`Helpers/EmoteMarkup.Parse`** is the
  single reaction parser.
- **Never pick a response line with a bare `Random`.** Use `ResponsePicker.Pick(pool)`, which
  excludes the pool's `min(50, pool.Length / 2)` most recently said lines. Its memory is **global**,
  not per channel — the same people read every channel and their DMs — and records when each line
  was last said, so busy pools (emote reactions) never crowd out rare ones. Pick the template
  *before* `string.Format`. A pool that goes through `string.Format` throws on a stray brace. **A pool spent once a day uses
  `Helpers/DailyRotation` instead** — a restart wipes the picker's history, which at one pick a
  day means it never helps; the rotation is computed from the date, with nothing stored.
- **Custom emote markup lives in `Helpers/Emotes` and nowhere else**, as `const string` pairs
  (`XId` + `X`), so lines stay constant expressions. Never paste raw `<:name:id>` into a pool.
- **`BotResponses.DisplayNameFor(IUser)`** is the only place the
  `Nickname ?? GlobalName ?? Username` chain lives (it also applies family nicknames). Never
  re-inline it.

### Authorization — three models, don't conflate them

- `SessionPermissions.CanManage` — the organizer, or an Administrator / ManageGuild holder
  (sessions, polls).
- `SessionPermissions.IsStaff` — Administrator / ManageGuild **or** the owner, regardless of who
  owns the thing (`/admin`, `/config`, staff Plynling actions).
- Owner-only — `Context.User.Id == AvailabilityService.OwnerId`, checked inline (`/debug`).

`/admin` and `/config` deliberately carry **no `[DefaultMemberPermissions]`**: a permission bit
cannot express "ManageGuild, plus the owner". The `IsStaff` check in each handler is the only gate.

### Docs

`/help` (`HelpModule`) and `README.md` are hand-maintained. A new user-facing command means
updating both — except owner-only commands, deliberately absent from `/help`. Easter eggs
(breakdown, quoicoubeh) stay out of both.

## Version and deployment

`ProjectSYNCS/config.yaml` is the **single source of truth for the version** — the csproj
regex-parses it into `<Version>` and `AppInfo.Version` shows it in the `/help` footer. Bump it there
only.

**Every change to the bot bumps the version**, because Home Assistant only offers the add-on update
when `version` changes: an unbumped push never reaches the Pi.

- **Once per batch, not per edit.** If `config.yaml` already differs from `HEAD`, the batch is
  bumped — don't bump again. The owner commits by hand, so check `git diff` rather than assuming.
- **Patch** (`5.18.11` → `5.18.12`): lines, fun facts, emotes, fixes, art, tweaks to existing
  behaviour.
- **Minor** (`5.18.12` → `5.19.0`, patch reset): a new command, subcommand, subsystem or
  user-visible behaviour, or anything that adds a migration.
- **Major**: the owner's call only; never bump it unasked.
- **No bump** for changes that stay out of the build: `docs/`, `README.md`, `CLAUDE.md`, `tools/`.
- Numbers don't roll over: `5.11.26` is fine. When you bump, say so in the summary, with the
  new number.

The bot ships as a Home Assistant add-on: the `Dockerfile` publishes a self-contained
`linux-arm64` build, and `run.sh` maps add-on options to `Discord__Token`,
`Discord__RegisterCommandsGlobally` and `Database__Path=/data/ProjectSYNCS.db`. Only `/data` is
persisted, so the SQLite file must stay under it — and so do `health-state.json`,
`ambient-state.json` and `morning-state.json`, which `PiHealthService`, `AmbientService` and
`MorningGreetingService` write next to it.

The GitHub remote is **public**. `appsettings.json` and `config.yaml` ship token placeholders; real
tokens go in user secrets (dev) or add-on options (prod), never in a tracked file.

## Hardcoded ids

These literal snowflakes are tied to one specific server: `AvailabilityService.OwnerId`; the other
leveling bot's id in `Helpers/LevelUpAnnouncement`; the custom emote ids in `Helpers/Emotes`;
`XpTracker.ExcludedChannels`; `ShameModule.ExtraVoters`; the per-user maps in `BotResponses`
(`PersonalComebacks`, `RealNames`, `KnownGenders`, `TataId`, `FamilyNicknames`);
`PlynlingAnnouncer.DefaultGameChannelId`; `MorningGreetingService.DefaultChannelId`;
`AmbientService.IdleChannelIds` (the everyday channels that must all be quiet for an idle line); and
`Helpers/HomeGuild.Id` (the one server the morning hello and `AmbientService` live in).

`ExcludedChannels`, `ExtraVoters` and `IdleChannelIds` are *floors*: `/config` can add to them but
never remove from them. The game and main channels are *defaults*: a channel set with `/config`
replaces them, and clearing it falls back (see `docs/agents/admin.md`). `OwnerId` is deliberately **not** configurable — it gates `/debug` and the DM relay, so making
it editable would let any ManageGuild holder hand themselves those powers.
