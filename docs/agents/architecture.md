# Architecture: the loops, the Ready hooks, the hardcoded ids, the data migrations

The rules that bind every subsystem (one interval per loop, DI lifetimes, per-item `try` in
sweeps, totals + daily buckets) stay in the root `CLAUDE.md`. This file lists what exists. Read it
before adding a loop, a `Ready` hook, a gateway subscription, a dated ranking, a data migration or
a server-specific id, and update it when you do.

## Gateway fan-out

`BotService` owns every message and reaction subscription and fans them out to the trackers and
services: `MessageReceived`, `MessageUpdated`, `MessageDeleted`, `MessagesBulkDeleted`,
`ReactionAdded`, `ReactionRemoved`, `UserJoined`. Connection-lifecycle hooks (`Ready`,
`Connected`, `Disconnected`) live in the service that needs them: `PresenceService`,
`AmbientService`, `ApplicationEmojiService`, `PlynlingMascotService`, `PiHealthService`. So does
`JoinedGuild` (she was added to a server): `PlynlingMascotService` (Ping-Qilin there too) and
`GuildHelloService` (her hello card, see admin).

## Dated rankings

Totals + daily buckets instances (the rule is in `CLAUDE.md`): `EmoteStat`/`EmoteDailyStat`,
`BotFeedback`/`BotFeedbackDailyStat`, `MemberXp`/`MemberDailyStat`, `ShameRecord`/`ShameDailyStat`,
`QuizStat`/`QuizDailyStat`, `ChatAnswerStat`/`ChatAnswerDailyStat`,
`CollectionCompletion.Count`/`CollectionDailyStat`, `MinecraftStat`/`MinecraftDailyStat`,
`Plynling.Score`/`PlynlingScoreDay` (per Plynling, not per person; see `plynling.md`, *Score*).

Buckets alone, with no totals, on purpose (see admin): `MemberFlowDailyStat` (joins/leaves),
`ActivityHourStat`, `ChannelDailyStat`, `CommandDailyStat`, `CommandMemberDailyStat`,
`IncidentDailyStat` (activity counting).

A removal always decrements *today's* bucket, except a bought `/bribe shame`, which takes the most
recent day holding a hit, so the windows move (see shame).

## Data migrations

Migrations are schema-only except eight deliberate data migrations riding apply-on-startup:
`ResetMemberXp` and `ResetXpTotals` (XP wipes), `AddPlynlingPassions` (backfill),
`PrepareProdLaunch` (one-guild launch prep), `SwapLaunchPairGenders` (those three are detailed in
`plynling.md`), `RetireBackpackAndHeadphones`, `RetirePumpkinGraveFrame` and `RetireMagicWand` (cosmetics refunded and
taken off, `economy.md`). Before writing another:

- A wipe **resets the reward and keeps the record** (`UPDATE … SET TotalXp = 0`), never deleting
  rows that also carry facts like `ReactionsUsed`.
- A data migration's `Down` cannot restore anything, so it is for one-off corrections only.
- Never generate a SQL script from a migration that computes "now" in C#.

**One startup backfill that is not a migration**: `PlynlingScoreRebuild` (Program.cs, after
`MigrateAsync`, before the host runs) rebuilds the Plynling score of every row that predates it
(`Plynling.ScoreRebuilt` false). It needs the badge catalog, stage ages, RPG curve and event catalog,
which SQL can't reach, and it runs before the bot connects so no live action is counted twice.
Idempotent through the flag, one unit per Plynling in its own `try`. See `plynling.md`, *Score*.

## Background loops

Seventeen `BackgroundService` loops, each with **its own interval on purpose** — never share one:

- `ReminderService` (5 min, load-bearing — see scheduling).
- `PersonalReminderService` (1 min, `/remindme` — see scheduling).
- `WorkReminderService` (2 min, the `/work` DM reminder — see economy).
- `PresenceService` (5 min, cosmetic — see below).
- `VoiceXpService` (1 min — also feeds the voice spectator, the session roll call and her voice
  seat).
- `GiveawayDrawService` (1 min).
- `LotteryDrawService` (1 min, the weekly `/arcade lottery` draw — see arcade).
- `QuizMasterService` (1 min, see quiz).
- `PlynlingSweepService` (hourly).
- `PiHealthService` (2 min — the Pi's temperature, how the last run ended, gateway outages; see
  `personality/ambient.md`, *Her body*).
- `WeekendPollService` (15 min, her Wednesday poll; see scheduling).
- `SeasonalAvatarService` (hourly, plus every Ready — her seasonal profile picture; see
  `docs/syncs-appearance.md`).
- `MonthlyDigestService` (hourly, her monthly digests to the owner and Tata on the 1st; see admin).
- `ActivityFlushService` (5 min, writes `ActivityCounter`'s counts; see admin).
- `ChannelMilestoneService` (1 min: reads channels' history until caught up, saves the counts every
  30 min and on a clean stop; registered twice on one instance, since `BotService` and `/debug`
  share its state; see admin, *Channel message milestones*).
- `MinecraftBodyService` (1 min, her body on the Minecraft server, a Node child process; see
  minecraft).
- `SteamFreeGamesService` (3 h, first pass 5 min after start — Steam games turned free to keep;
  see community).

Services with another shape:

- **`MorningGreetingService`** has no interval: it sleeps until the earliest pending morning slot
  among the servers with the hello on (8:00–10:00 Paris, `Helpers/Personality/MorningGreeting`),
  never more than 5 minutes — see personality.
  **`QuokkaRaidService`** is the same shape: it sleeps until each of the day's Quokka visits
  (0–4, 12:00–23:00 Paris, `QuokkaRaid.SlotsFor`) — see arcade.
- **`AmbientService`** (10 min, cosmetic, its own interval like the loops above): the 3 a.m. line
  and the scolding for answering it, idle fillers and the wake-up line after a restart — see
  `personality/ambient.md`, *Ambient life*.
- **`ApplicationEmojiService`** runs once, on the first Ready (uploads item icons — see economy).
  **`PlynlingMascotService`** runs on every Ready (see plynling).

## Presence

**`PresenceService`** hooks `Ready` itself, because Discord drops the presence on every reconnect.
Lines go out via `SetCustomStatusAsync`, **not** `SetGameAsync`: a custom status renders verbatim,
whereas `ActivityType` verbs are prepended and localised to the *viewer* ("Watching le vide").
`SetGameAsync` with `ActivityType.CustomStatus` compiles and renders nothing — a custom status
carries its text in `State`, not `Name`.

## Hardcoded ids

These literal snowflakes are tied to one specific server: `AvailabilityService.OwnerId`; the other
leveling bot's id in `Helpers/Xp/LevelUpAnnouncement`; the custom emote ids in `Helpers/Core/Emotes`;
`XpTracker.ExcludedChannels`; `ShameModule.ExtraVoters`; the per-user maps in `BotResponses`
(`PersonalComebacks`, `RealNames`, `KnownGenders`, `TataId`, `FamilyNicknames`);
`PlynlingAnnouncer.DefaultGameChannelId`; `MorningGreetingService.DefaultChannelId`;
`AmbientService.IdleChannelIds` (the everyday channels that must all be quiet for an idle line);
`AmbientService.VoyagerChannelId` (where Voyager's letters land);
`RobotHeartService.EveStrictChannelId` (where « eve » means the game, so EVE needs WALL-E beside her);
the voice channels in `Helpers/Voice/VoiceRoom` (which spectator pool each one gets, and where she
waits for a session nobody has joined yet);
`Helpers/Core/HomeGuild.Id` (the home of her single-instance life — voice seat, Voyager, the
monthly digest — and the server where the `/config feature` switches default to on);
`Helpers/Core/BotsChannel.DefaultId` (« spam-bots », where her low-stakes posts go).

**On other servers.** Channel ids are global, so the hardcoded channel sets (`ExcludedChannels`,
`IdleChannelIds`, the defaults) are inert elsewhere: they only ever match home channels; `/config
show` lists them on the home server only. User ids are not: anything keyed by a person is gated by
`HomeGuild.Is(guildId)` — `ShameModule.HardcodedExtraVotersFor`, `BotResponses.RealNameFor`,
`PersonalComebacksFor`, `GenderFor` — except Papa (`OwnerId`) and Tata (`TataId`), whose tiers follow
them everywhere (`personality/people.md`).

**Per-server state in the singletons.** The home features other servers can switch on (`/config
feature`, see admin) keep their claims per guild in a dictionary under the service's one lock
(`AmbientService.GuildAmbient`, `MorningGreetingService`'s per-guild day and slot,
`QuokkaRaidService`'s activity ticks, `WeekendPollService`'s decisions, `VoiceSpectatorService`'s
line day, `WelcomeService`'s rush limit), the pattern of `QuizMasterService._plans`. Their loops walk
`_client.Guilds`, skip a guild whose feature is off or that is silenced, and **catch per guild**. What
is hers alone stays process-wide: the wake decision after a crash (one wake, told on every server),
PiHealth's heat record and router outage (one decision, told in every ambient main channel), her
voice seat, Voyager. `morning-state.json` and `silence-state.json` are keyed by guild and read their
old one-server shape as home.

`ExcludedChannels`, `ExtraVoters` and `IdleChannelIds` are *floors*: `/config` can add to them but
never remove from them. The game, main and bots channels are *defaults*: a channel set with
`/config` replaces them, and clearing it falls back (see `admin.md`).
