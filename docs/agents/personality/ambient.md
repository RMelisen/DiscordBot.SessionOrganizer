# Personality — ambient life: morning hello, idle lines, voice seat, welcome, her body

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

## Morning hello

`MorningGreetingService` posts one `MorningGreetings` line a day in her main channel
(`/config main-channel` in the home guild, else `MorningGreetingService.DefaultChannelId`), at a random slot from `Helpers/Personality/MorningGreeting` (8:00–10:00 in
`AppTime.Zone`). It sleeps until the slot rather than ticking. A `MorningFunFacts` line
always goes underneath. **Both lines come from `Helpers/Core/DailyRotation`, not `ResponsePicker`**: a
shuffled walk through the pool, one step per calendar day, computed from the date, so restarts
don't reset it and an early hello says what the slot would have. No line repeats until the whole
pool is used, and the join between two passes keeps any line at least `pool / 4` days from its
last use (17 days for 65 hellos). Each pool has its own salt (`GreetingSalt`, `FunFactSalt`);
editing a pool's length reshuffles it from that day, so one recent line may come back once.
**Every fun fact must be true**: the joke is
her commentary or the fact's uselessness, never an invented fact.

- **On 16 June (her birthday, `MorningGreeting.IsBirthday`) the hello is
  `BotResponses.BirthdayGreeting(age)`**: her age, the year minus `BirthYear` (2026), and **no fun
  fact** under it. It replaces the rotation for the day; the rotation is date-based, so nothing
  shifts. It is one line without a newline, and the restart scan below matches it by its exact
  text for today's age.
- **The claimed day is saved to `morning-state.json`** next to the database (under `/data` in
  prod), on every claim, early or on the slot. A restart between an early hello and the slot (a
  deploy at 8:30) otherwise forgets the claim and says hello twice; the history scan below misses
  it once 50 messages have gone by since the hello.
- **A restart inside the window draws a new slot for today**, so before posting she also scans the
  channel's last 50 messages for one of her own from today whose **first line** is exactly a
  `MorningGreetings` line. That match is why no two hellos may be identical, why a hello never
  contains a newline, and why the pool takes no `string.Format` placeholder.
- A slot that comes before the gateway has delivered the channel waits for it, never past the
  window's end. In the dev guild the channel never resolves and the day is skipped with a log.
- One attempt per day: a failed send is not retried that morning.

**Someone else's greeting can bring the hello forward.** `BotService` feeds every message to
`MorningGreetingService.HandleMessageAsync`, after `ReactionService` and before `ChatterService`. A
`MessageCues` greeting (not `Mean`, not a verdict) in her channel, from
`MorningGreeting.ReplyWindowStart` (7:00, an hour before her own window) and before her slot, rolls `EarlyHelloChance` (30%) to post the hello right away; the slot then finds
the day claimed and skips. Each person gets **one roll per morning**, so a chorus of "bonjour"
doesn't make it certain. Messages aimed at her are skipped: `ChatterService` already answers those.

- **The timer and the gateway share one claim** (`TryClaimDay` / `TryClaimEarly`, under one lock).
  Claiming outside that lock is how she would say hello twice.
- **The service is registered twice on one instance**: `AddSingleton` plus `AddHostedService(sp =>
  sp.GetRequiredService<…>())`. A plain `AddHostedService<MorningGreetingService>()` would make the
  host run a second instance whose claim `BotService` never sees.

## Ambient life

Her life in the main channel (`MorningGreetingService.MainChannelIdAsync`, the same one as the
morning hello) when nobody is talking to her.
Rare on purpose: the point is that she seems to be there, not that she talks. Odds, windows and
thresholds live in `Helpers/Personality/Ambient` (pure, Paris wall-clock hours, so DST never moves a rule).

| What | Who | When |
|---|---|---|
| Idle moon + `NightPresenceFillers` | `PresenceService` | 1:00–7:00 |
| 3 a.m. line (`NightLines`) | `AmbientService` tick | decided once a night (25%, random minute 3:00–3:49), main channel quiet ≥ 1 h; posted in the bots channel (`/config bots-channel`) |
| Night scolding (`NightScoldLines`) | `AmbientService.HandleMessageAsync` | any human message in the main channel after tonight's line and before 5:30, once a person a night, verdicts excepted; a reply, and `ChatterService` is skipped for that message |
| Idle turn | `AmbientService` tick | 10:00–23:00, **all idle channels** (`IdleChannelIds`, plus `/config idle-channels`, plus the main channel) quiet ≥ 6 h (threads count for their parent), one 50% roll per silence, max 1/day, posted in the main channel: 30% late `SeenReactions` on the main channel's last human message, else an `IdleLines` line, a quarter of the time an `IdleEditLines` pair (edited 5 min later, or 2 s after someone speaks in the main channel after it) |
| Ghost typing | `AmbientService.HandleMessageAsync` | a human message after ≥ 1 h quiet, not aimed at her, 3%, max 1/day |
| Hesitant reaction | `ReactionService` | 3% of mood reactions are removed 2–4 s later |
| Abrupt wake | `AmbientService`, first `Ready` only | after a power cut or a crash (`PiHealthService.LastStop`): **always**, any hour, no daily cap, `PowerCutWakeLines` / `CrashWakeLines`, or 35% of the time a corrupted `GlitchWakeLines` pair (posted glitched with the glitch strip under it, edited clean 2 min later, or 2 s after someone speaks in the main channel, the strip deleted then) |
| Hot status | `PresenceService` | awake, Pi ≥ 70 °C: half the rotations `HotPresenceFillers` |
| Heat record | `PiHealthService` tick | today's max beats every other day by ≥ 1 °C, ≥ 30 days of history, 10:00–23:00, max 1/day: `HeatRecordLines` |
| Voice spectator | `VoiceSpectatorService`, fed by `VoiceXpService`'s sweep | a home-guild voice channel where ≥ 2 active people spent ≥ 1 h together empties (or has < 2 active for 15 min): in that voice channel's chat, from the pool of its room (`Helpers/Voice/VoiceRoom`: `VoiceSpectatorGamingLines` for both Gaming channels, `…CinemaLines`, `…StudyLines`, `…GeneralLines` for Général and any unlisted channel), max 1/day across all rooms, any hour |
| Synthia flinch | `SynthiaService`, before `ChatterService` | « Synthia » in any guild message, max 1/day: 40% a reaction taken back (`SynthiaReactions`), 35% a reply deleted 4–6 s later (`SynthiaVanishLines`), 25% a slip edited into a denial (`SynthiaEditLines`); returns true so no comeback lands on top |
| Robot heart | `RobotHeartService`, after `SynthiaService` | WALL-E, EVE (alone, except in `RobotHeartService.EveStrictChannelId` and its threads, where only beside WALL-E; never « EVE Online »), GLaDOS, Wheatley, a robot vacuum, Monika, 2B, Voyager 1 (« voyager 1 », « sonde Voyager »), JARVIS (« jarvis », or spelled out « J.A.R.V.I.S. ») named in any guild message, verdicts and emote markup excepted; **no limits**. Named in passing: an emote reaction every time, taken back 2–4 s later for WALL-E, Voyager, Wheatley and the vacuum (`RobotHeart.TakesBackReaction`), kept for EVE, GLaDOS, Monika, 2B and JARVIS; then 40% of the time (`RobotHeart.AmbientSpeakChance`) the same words as below, without a second reaction, and returns true; otherwise returns false, so `ChatterService` still runs. Named to her face (a mention, a reply to her, or her name said — so the robot wins over *Talked about*): always words — 60% a reaction + a line (`HeartReactions`, `HeartLines` via `ResponsePicker`; the reaction taken back for WALL-E and Voyager; WALL-E's line followed by its strip 2 times in 3), 40% the character's word performance (a `HeartVanishLines` reply deleted 4–6 s later, a `HeartEditLines` slip edited 3.5–5 s later; the vacuum has none); returns true so no comeback lands on top |
| Voyager's letters | `AmbientService` tick | about one a fortnight (`Helpers/Personality/Voyager`: gaps of 10–18 days, slot 10:00–22:00), in their own channel (`AmbientService.VoyagerChannelId`, home guild only): `VoyagerLetterLines` with his real light time; half the time `VoyagerAnswerLines` 2–6 h later (before 23:00); on 18 Nov 2026 only, `VoyagerCrossingLine` instead; claimed in `ambient-state.json` |
| Voice seat | `VoiceSeatService`, fed by `VoiceXpService`'s sweep | a home-guild voice channel with ≥ 3 active people for ~10 min rolls 66% **once per gathering** (Tata 80%; Papa or a live session: ≥ 2 for ~5 min, 90%; a better tier arriving late rolls once more); a win seats her there (muted, deafened) until it has had fewer than that for ~5 min; not at night without Papa; see *Her seat in voice* |
| Router return | `PiHealthService`, `Connected` | gateway back after ≥ 10 min, any hour, max 1/day: `RouterReturnLines` (what she missed in the main channel) or `RouterReturnQuietLines` |

- **`AmbientService` is registered twice on one instance**, like `MorningGreetingService`: `BotService`
  feeds it every human message (two quiet clocks: the main channel's, and all idle channels
  together) and the host runs its 10-minute tick. The first tick seeds both from history (50
  messages in the main channel, 10 in each other one; a forum is skipped), or from the start time
  when none is human, so a restart is never followed straight away by an idle line. It also finds
  tonight's line there, so a restart before 5:30 keeps the scolding on.
- **`HandleMessageAsync` returns whether she answered** (the scolding); `BotService` then skips
  `ChatterService`, so a reply to her 3 a.m. line gets sent to bed instead of roasted.
- **The pending edit runs off the tick** (`Task.Run`), waiting on whichever comes first: 5 minutes,
  or the next human message in the main channel (a `TaskCompletionSource` the handler completes). A
  restart before the edit leaves the first version standing.
- **Every pool but `NightPresenceFillers` and `NightScoldLines` goes through `DailyRotation`**: each is spent at most once a
  day, and a restart would wipe `ResponsePicker`. `NightLines` must stay distinct and newline-free:
  a restart inside the hour re-rolls the night, and she checks the last 50 messages for tonight's
  line by exact text before posting another.
- **The wake line's memory is `ambient-state.json`** next to the SQLite file (`/data` in prod), not
  the database: `LastVersion` (compared with `AppInfo.Version`) and `LastWakeDay`. Missing or
  unreadable means "same version, not woken today". The version is written on every start, even
  when she stays quiet.
- **The hesitation runs off the gateway handler** (`Task.Run`): waiting inline would hold up every
  handler after `ReactionService` in the fan-out. Not on the energy-drink can or the pile-on path.
- Nothing fires in DMs, on bots, or during a breakdown; every send uses `AllowedMentions.None`. In the
  dev guild the channel never resolves, so only the night status is visible there.
- Ghost typing and the hesitant reaction stay out of `README.md` and `/help`, like the easter eggs.
  So do the robot heart and Voyager's letters: lore is hinted, never listed.
- **Voyager's light time is a table, not a formula** (`Helpers/Personality/Voyager`): JPL Horizons
  samples every 30 days from 2026, interpolated, then a linear drift past 2035. To extend it, fetch
  the same query (Voyager 1, `-31`, from `500@399`, `QUANTITIES=21`) and append the values. Lines
  show it rounded to 5 minutes.
- **Every `ambient-state.json` save starts from `LoadState()`**: the wake line and the letters
  share the file, and building a fresh `AmbientState` would forget the other's fields.

### Her seat in voice — `VoiceSeatService`

She joins with `ConnectAsync(selfDeaf: true, selfMute: true, external: true)`: `external` sends the
voice-state update only, with no audio client, so no libsodium/opus and no voice encryption. Rules
are pure, in `Helpers/Voice/VoiceSeat`.

- **Three tiers**, the best present wins (`VoiceSeat.TierFor`), each person counted only when
  active (not muted, not deafened):

  | Tier | When | Threshold | Settle | Chance |
  |---|---|---|---|---|
  | Favoured | Papa (`AvailabilityService.OwnerId`), or a session under way there | 2 (never him alone) | 3–7 min | 90% |
  | Tata | `BotResponses.TataId` | 3 | 8–12 min | 80% |
  | Base | anyone | 3 | 8–12 min | 66% |

  A better tier arriving after a lost roll rolls once more at its chance (a tier never rolls twice).
  90%, not 100%: every single time reads as a script.
- **The draws keep the averages.** The settle time is drawn once per gathering (whole minutes, so
  8–12 averages 10 and 3–7 averages 5, as before the draws), the grace once per dip (4–6, average
  5): she joins and leaves at the same average times as with fixed ones, so the average stay is
  unchanged — only the regularity is gone. Don't widen one end of a range without the other.
- **She follows Papa, and only him**: a won channel where he is active comes first, even over her
  current seat. When he leaves her channel and she can't follow him, half the time she leaves too,
  1–3 min later (`Release`: only his return rolls her back into that gathering); him coming back
  before then cancels it. Muting counts as leaving, like everywhere else here.
- **At night** (`Ambient.IsSleepHours`) she sits down nowhere he isn't; a seat taken before 1:00 is
  kept.
- **Thrown out of the call** — seen there (the sweep, or her own `UserVoiceStateUpdated`, which is why
  the seat is recorded *before* the call to Discord), then gone, with no `Connected` since she sat
  down. 40% she **pouts**: `VoiceKickPoutLines` in that voice channel's chat, that gathering given up,
  and no sitting down for the rest of the Paris day anywhere he isn't. Otherwise she gets **angry**:
  `VoiceKickAngryLines`, and back in 10–20 s, once; a second kick in the same gathering is always a
  pout. A seat lost to a reconnect, or a join that never took, is given up silently.
- **First one there.** A Game or Movie session with ≥ 2 Joined
  (`SessionAttendanceService.SessionsToHost`): from 2 min before the start to 15 min after, if
  nothing else seats her she sits in the channel holding most of its confirmed people, else
  `VoiceRooms.DefaultFor` (Cinéma for a film, the first Gaming channel otherwise). A gathering
  forming there **adopts** her (she was there first); with none by start + 15 min she leaves. No
  early arrival while she pouts, after bedtime, or at night without Papa in that room. The late
  call switches to `SessionLateCallSeatedLines` when she is in voice.
- **Another bot in the call** (any bot but her) halves Base and Tata odds; Favoured is untouched.
  When a start roll is lost *because of it* (the same draw would have won without it) she says so
  once per gathering in that voice chat (`VoiceRivalRefuseLines`); seated with one anyway, one
  grumble per seat (`VoiceRivalSeatedLines`).
- **Bedtime.** Seated with Papa active at night, once a night at a minute drawn from the date
  between 1:15 and 2:15 (`VoiceSeat.BedtimeFor`), she scolds him (`OwnerBedtimeLines`: caring
  anger, never sharp) and leaves a minute later; then no call at all until 7:00, his included. A
  gathering still won at 7:00 gets her back.
- **The mic flicker.** 20% of seats, once, 5–40 min in: self-deaf and self-mute off for 2 s, then
  back on — the icons flicker, no sound (she has no audio client). Only works on a seat this
  process joined (Discord.Net keeps the voice-state parameters); otherwise skipped.
- **Time together** is recorded: every sweep she is confirmed in a call credits one minute to every
  human connected there, muted or not (`VoiceCompanionStat` totals + `VoiceCompanionDailyStat`
  buckets, the root pattern). Read by `/recap` (per person) and `/report syncs` and the monthly
  digest (her top companions, see `admin.md`).
- **One seat per guild** (Discord's rule). With two won channels and no Papa: most active people, then
  one hosting a live session (`SessionAttendanceService.IsInLiveSession`), then the first started.
  **She never switches while her gathering holds her** (Papa aside) — every move plays Discord's
  sounds in two channels; when hers ends she moves to another won one in one voice-state update
  (`disconnect: false`).
- **She reconciles against where Discord says she is** every sweep, 90 s after her last move:
  dragged elsewhere → she adopts that channel while its gathering lasts; sitting somewhere after a
  restart → she leaves.
- **Her status says where she is** (`PresenceService`, refreshed on `SeatChanged`, which also fires
  when the streamer in her call changes): `SeatStreamPresenceLines` while someone streams there
  (Papa first; she "watches", no remark in the chat), else `SeatPresenceOwnerLines` with Papa
  active in her call, else `SeatPresenceLines` with the channel's name. It wins over every other status, night included (at night she's only in a call with him).
- Channels excluded with `/config` never get her. Home guild only, so it can't be seen on the dev
  guild. Bots count for neither voice XP nor the spectator, so her presence changes neither.
- **The spectator line knows**: when she sat in that channel during the stretch,
  `VoiceSpectatorSeatedLines` (« j'y étais, sourde et muette ») replaces the room pools, which are
  written for a bot watching names from outside.

## Welcome — `WelcomeService`

She answers Discord's own join line (`SocketSystemMessage`, `MessageType.GuildMemberJoin`, which
every other handler skips) as a reply, so there is one welcome and she follows Discord's timing
(membership screening included). `WelcomeLines` pings the newcomer, narrowed to them, whether
or not they were here before: there is no welcome back. More than 3 welcomes in an hour → one
`WelcomeRushLines`, then silence for the hour. Bots are skipped there; a bot gets `RivalJoinLines`
in her main channel through `UserJoined` (home guild), since an OAuth-added bot may get no join
line. Departures are deliberately silent: without the audit log she can't tell a leave from a ban.

## Her body — `PiHealthService`

Reads the real Pi (`Helpers/Pi/PiHardware`: `/sys/class/thermal/thermal_zone0/temp`, `/proc/uptime`),
null everywhere else, so dev on Windows simply has no temperature. `/health` shows what it
reads — the first thing to run after a deploy if the status never mentions the heat.

- **Temperature** is sampled every tick into `PiThermalDay` (one row per Paris day: min, max and
  when, sum and count for the average, minutes ≥ 80 °C). Thresholds live in `Helpers/Pi/PiHealth`
  and are first guesses: tune them against real rows.
- **How the last run ended** comes from `health-state.json` next to the database: `CleanExit` is
  written false on start and true **on `ApplicationStopping`** — the moment Home Assistant's
  SIGTERM arrives, not after every service has stopped, because the Supervisor kills an add-on
  that overruns its stop timeout (10 s by default) and that must not read as a crash.
  `LastAliveAt` is refreshed every tick. Unclean on a host up for under 15 minutes, or booted
  after the last heartbeat, is a **power cut** (the whole Pi went down — a hard reset looks the
  same); the uptime test needs no clock, since a Pi without an RTC battery may start with the
  wrong time until NTP syncs. Unclean on a machine that stayed up is a **crash** (killed, OOM,
  a native fault). Both are recorded as `UptimeEvent`s.
  `PiHealth.Classify` is pure; the state is read once, before the file is overwritten, on first
  use — `AmbientService` may ask before `StartAsync` runs.
- **Outages**: the first `Disconnected` starts the clock, the next `Connected` stops it; ten
  minutes or more is an outage (`UptimeEvent`, plus the router line). Shorter gaps are Discord's
  ordinary reconnects. She blames the router whatever the cause — she can't tell from the attic.
- **`/health`** is public: anyone, posted in the channel, once per 30 s each (the owner skips
  the cooldown and also sees the state file's path). `PiHealthService.SnapshotAsync` reads
  everything into a plain `HealthReport`, which `HealthCards` renders as one V2 card (16 of 40
  components). Besides the temperature and the stops above it reads the load, memory and CPU
  frequency (`/proc`, `cpufreq`), the firmware's **power flags** (`get_throttled`, searched two
  levels under `/sys/devices/platform`, decoded by `PiHealth.DecodeThrottled`; failing that the
  `rpi_volt` sensor, which only knows « right now »), every gateway drop this run, the member
  cache of the server it's run in, the database and `-wal` sizes and free disk, and the
  `UptimeEvent` log (last 5, 30-day tally). Her line on top comes from `HealthHotLines` (≥ 70 °C or
  slowed by the firmware), `HealthShakenLines` (under-voltage now, or a stop or outage that ended
  within 24 h) or `HealthFineLines` — `PiHealth.Mood`, pure. A relative timestamp renders
  « il y a 3 jours », so no « depuis » goes in front of one.
- **The duration is optional.** About a third of the wake lines and some router lines don't
  mention it at all — a frightened bot doesn't always look at the clock. Where `{0}` is used in
  `PowerCutWakeLines` / `CrashWakeLines`, it must read with `PiHealth.UnknownDowntime`
  (« un bon moment ») as well as with « 2 h 05 »: no sentence starts with `{0}`, and no
  « de {0} ». In the router pools `{1}` may be « 1 message », so nothing around it assumes a
  plural. Interpolated lines (the ones with a custom emote) write the placeholder `{{0}}`: a
  bare `{0}` inside `$"…"` prints the number 0.

