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
`AmbientService`, `ApplicationEmojiService`, `PlynlingMascotService`, `PiHealthService`.

## Dated rankings

Totals + daily buckets instances (the rule is in `CLAUDE.md`): `EmoteStat`/`EmoteDailyStat`,
`BotFeedback`/`BotFeedbackDailyStat`, `MemberXp`/`MemberDailyStat`, `ShameRecord`/`ShameDailyStat`,
`QuizStat`/`QuizDailyStat`, `ChatAnswerStat`/`ChatAnswerDailyStat`,
`CollectionCompletion.Count`/`CollectionDailyStat`, `MinecraftStat`/`MinecraftDailyStat`.

Buckets alone, with no totals, on purpose (see admin): `MemberFlowDailyStat` (joins/leaves),
`ActivityHourStat`, `ChannelDailyStat`, `CommandDailyStat`, `CommandMemberDailyStat`,
`IncidentDailyStat` (activity counting).

A removal always decrements *today's* bucket, except a bought `/bribe shame`, which takes the most
recent day holding a hit, so the windows move (see shame).

## Data migrations

Migrations are schema-only except six deliberate data migrations riding apply-on-startup:
`ResetMemberXp` and `ResetXpTotals` (XP wipes), `AddPlynlingPassions` (backfill),
`PrepareProdLaunch` (one-guild launch prep), `SwapLaunchPairGenders` (those three are detailed in
`plynling.md`) and `RetireBackpackAndHeadphones` (two accessories refunded and taken off,
`economy.md`). Before writing another:

- A wipe **resets the reward and keeps the record** (`UPDATE … SET TotalXp = 0`), never deleting
  rows that also carry facts like `ReactionsUsed`.
- A data migration's `Down` cannot restore anything, so it is for one-off corrections only.
- Never generate a SQL script from a migration that computes "now" in C#.

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

- **`MorningGreetingService`** has no interval: it sleeps until one random slot per morning
  (8:00–10:00 Paris, `Helpers/Personality/MorningGreeting`) — see personality.
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
`Helpers/Core/HomeGuild.Id` (the one server the morning hello and `AmbientService` live in);
`Helpers/Core/BotsChannel.DefaultId` (« spam-bots », where her low-stakes posts go).

`ExcludedChannels`, `ExtraVoters` and `IdleChannelIds` are *floors*: `/config` can add to them but
never remove from them. The game, main and bots channels are *defaults*: a channel set with
`/config` replaces them, and clearing it falls back (see `admin.md`).
