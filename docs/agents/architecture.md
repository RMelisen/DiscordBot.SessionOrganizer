# Architecture: the loops, the Ready hooks and the hardcoded ids

The rules that bind every subsystem (one interval per loop, DI lifetimes, per-item `try` in
sweeps) stay in the root `CLAUDE.md`. This file lists what exists. Read it before adding a loop, a
`Ready` hook, a state file or a server-specific id, and update it when you do.

## Background loops

Fourteen `BackgroundService` loops, each with **its own interval on purpose** — never share one:

- `ReminderService` (5 min, load-bearing — see scheduling).
- `PersonalReminderService` (1 min, `/remindme` — see scheduling).
- `PresenceService` (5 min, cosmetic — see below).
- `VoiceXpService` (1 min — also feeds the voice spectator, the session roll call and her voice
  seat).
- `GiveawayDrawService` (1 min).
- `QuizMasterService` (1 min, see quiz).
- `PlynlingSweepService` (hourly).
- `PiHealthService` (2 min — the Pi's temperature, how the last run ended, gateway outages; see
  `personality/ambient.md`, *Her body*).
- `WeekendPollService` (15 min, her Wednesday poll; see scheduling).
- `SeasonalAvatarService` (hourly, plus every Ready — her seasonal profile picture; see
  `docs/syncs-appearance.md`).
- `MonthlyDigestService` (hourly, her monthly digests to the owner and Tata on the 1st; see admin).
- `ActivityFlushService` (5 min, writes `ActivityCounter`'s counts; see admin).
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
