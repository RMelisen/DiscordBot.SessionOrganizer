# Personality subsystem

Separate from scheduling. `ChatterService` decides *how* to react to a message, `BotResponses`
holds the canned lines, `ResponsePicker` chooses one, `MessageCues` detects intent (mood,
greeting, verdicts, threats), `BreakdownService` plays the easter egg, `ReactionService` answers
with an emote instead of words, `RivalryService` handles other bots. Verdicts ("good bot") are
their own subsystem — see `bot-feedback.md`.

## Who handles what

**`ChatterService` and `ReactionService` split the room.** Anything aimed at her — an @mention or
a reply to one of her messages — belongs to `ChatterService`; `ReactionService` skips those so she
never both roasts and decorates the same message. Reactions are for conversations *nobody*
addressed to her: a message qualifies on a `MessageCues` hit, or on being the owner's (anything he
writes qualifies — that's the favouritism), then passes a probability roll and a per-channel
cooldown.

**One exception: the energy drink can.** A message mentioning Monster or an energy drink
(`MessageCues.MentionsEnergyDrink`) gets an `EnergyDrinkReactions` can **every time**: no roll, no
cooldown, even on a message aimed at her (only bots, DMs and a running breakdown are skipped). It
names a drink rather than reading the message, so it is not a decoration on a comeback, and it never
takes the mood reaction's turn. "Monster Hunter" doesn't count; posting the `:monster:` emote does.

**`ReactionService`'s two paths are gated differently, on purpose — don't unify them.** Reacting to
a *message* is rationed by `Cooldown` (she volunteers an opinion). Copying someone else's
*reaction* (`HandleReactionAddedAsync`) is odds-only, no cooldown (piling on should feel reflexive).

**The pile-on path skips bots twice.** A reaction *added by* a bot is ignored (bookkeeping marks),
and a reaction *on* a bot's message is ignored (she'd be applauding a rival while
`RivalryService` sulks at it). The second check needs the author, so it costs a fetch and lives
inside the `try` with the owner/self checks.

## Reactions and emotes

- **She can only react with an emote she shares a guild with.** Unicode is always safe; a custom
  emote works only because it is the server's own. Copying a reaction goes through
  `ReactionService.CanUse` first (people paste emotes from other servers). Reactions come from
  curated pools only, **never** from the `EmoteStats` leaderboard, which records emotes from
  anywhere.
- **A custom emote in a `*Reactions` pool must carry its id** (`<:name:1234…>`, never
  `<:name:>`). `EmoteMarkup.Parse` falls back to `new Emoji(markup)` when `Emote.TryParse` fails,
  so id-less markup yields a non-null "emoji", Discord rejects it with a 400, the exception is
  swallowed — and the channel's 10-minute `Cooldown` is already burned, since `TryClaimChannel`
  runs before the parse. No compile-time check; the only symptom is a log warning. Using the
  `Helpers/Emotes` constants avoids this.
- **`BotResponses.MeanReactions` does double duty**: the pool she reacts *with* when a message
  reads hostile, and the definition of "hostile" used to decide what she refuses to pile onto on
  the owner's messages. Membership is tested on the parsed `IEmote`, so a renamed emote still
  matches.

## `MessageCues`

**Cues are weighted, not boolean.** `Analyze` returns a `MessageMood` (`Emotion` + `IsGreeting`)
scored over the whole message.

1. **Weak cues** (`_weakCues`) score 0.4 and cannot fire alone; two together reach the 0.8
   threshold. Words with an innocent reading go there (`cool`, `ferme`, `rate`, `zero`, `merde`,
   `claque` — "ça claque" is a compliment). Adding an ambiguous word to `_niceCues`/`_meanCues`
   without listing it as weak is how misfires happen.
2. **Negation** reaches *backwards* three tokens for every cue, and *forwards* two only for
   `_verbCues` (chat French drops the `ne`: "j'aime pas"). A forward window on every cue would let
   "merci, pas de souci" cancel its own thanks.
3. **Mean beats nice on margin**, not absolutely: "super nul" is mean, "merci, t'es pas nulle" is
   nice. Greeting is a separate axis; callers wanting "a mean word cancels the greeting" check
   `Emotion` themselves.
4. **Every cue must survive `TokenizeOrdered` unchanged** or it is unreachable (`"3.0"` tokenizes
   to `["3","0"]`). Cues are stored lowercase and accent-stripped. Mood-carrying custom emotes are
   matched by **id** (`_niceEmoteIds`, `GreetingEmoteId`), never by name.

`IsMistakenIdentity` is an identity check, not a mood, and stays a plain `bool`.

### Vocabulary rules

- **Crude insults are in the pools** (`connard`, `salope`, `enfoiré`, `ordure`, `pute`,
  `menteur`, `ta gueule`, `vos gueules`, `pauvre type`, `nique ta mere`). The harness pins that
  they **fire** — they're the most common French insults and missing them was the larger error.
- **Expand the mean side with phrases, not bare words.** A phrase scores 1.2 and is nearly always
  person-directed; a bare word misfires on game content, and since untargeted hostility scores on
  `/shame`, every bare cue is also a false positive there. `con`, `cons`, `conne`, `lourd`,
  `lourde` are **weak**; `putain` is in no pool (punctuation, not an insult).
- Nice side: `clean`, `efficace`, `malin`, `utile` are weak (they describe builds and routes).
- **Short warm replies are nice; short agreements are not.** `avec plaisir`, `de rien`, `pas de
  souci`, `tant mieux`, `trop cool`, `bien dit`, `bonne idee`, `beau travail`, `bon courage`,
  `je valide` are `_nicePhrases`. `ça marche`, `ça roule`, `ça me va`, `tout à fait`, `c'est
  clair` are deliberately **not** — ordinary coordination must stay silent. Both halves are pinned.
- **It's a gaming server.** `boss` and `monstre` are absent from `_niceCues` ("il est fort ce
  boss"); `heros`, `roi`, `reine`, `royal`, `divin`, `toxique`, `manchot` are weak ("dégâts
  toxiques", "arme divine"); `sale` is absent ("c'est sale" is a compliment, and the squashed-token
  fallback would map "salle" onto it). Check new cues against session/loot/combat vocabulary.

## `ChatterService.HandleMessageAsync` — branch order is load-bearing

From the top:

1. **Owner DM reply relay** — before the reply-to-bot branch, which would otherwise swallow it as a
   reply and fire a comeback. Relay and DM acknowledgements send directly (no typing delay).
2. **Verdict guard** (non-`None` `ReadFeedback` → bail; guild-only) — after the relay, so a relayed
   reply still works. Without it, "good bot" replying to her fires a *comeback*.
3. **Reply / mention paths.** In both, **`TryHandleShutdownThreatAsync` sits above every mood
   branch**, above the breakdown roll and above the owner rescue-roast branch.
4. **Shutdown-by-name** (ambient, see below).
5. **Quoicoubeh** (ambient, last of all).

Everything aimed at her returns before the two ambient branches are reached.

## Favouritism

**Three tiers.** Rodhengard (`AvailabilityService.OwnerId`) is exempt from teasing
(`OwnerComebacks` replaces the roast pool). **Tata** (Analuz, `BotResponses.TataId`, her aunt) is
*favoured*: `RollTataWarmth` gives her a warm pool `TataWarmthChance` (60%) of the time, her
`PersonalComebacks` roasts otherwise. Everyone else is always roasted.

- A **mean** message from Tata never qualifies for warmth.
- `RollTataWarmth` rolls dice — call it **exactly once per message** (it sits in an `else if` on
  both paths).
- **Each favourite has two pools, one per path**: a mention is being *summoned*, a reply is being
  *talked to*. Papa: `OwnerGreetings` / `OwnerComebacks`; Tata: `TataGreetings` / `TataReplies`.
  Wiring a new favourite to one pool on both paths reads as her not noticing.
- **Her name is overridden**: `FamilyNicknames` maps Tata's id to "Tata", applied by
  `DisplayNameFor`. `RealNames` is *not* overridden (the breakdown reveal wants a real name).

**The owner's one exception: him being mean to her.** A `Mean` reading from him routes to
`OwnerMeanReplies` on both paths (she won't fight him, so it lands). On the reply path the check is
**above** the `ReferenceChance` roll (a pop-culture quip in answer to cruelty reads as not
noticing); on the mention path it is **below** the rescue branch (a mean mention aimed at someone
else is still a rescue roast). **This does not extend to `ReactionService`**, which returns
`OwnerReactions` for him unconditionally: ambiently it can't tell "t'es nulle" from "ce boss est
nul", and ambient devotion misfiring is harmless where ambient sadness is not.

## Shutdown threats — favouritism inverts

Being able to carry the threat out makes her reaction *worse*:

| Who | Why | Pool |
|---|---|---|
| Rodhengard | wrote her, could unplug her, nothing to bargain with | `ShutdownThreatOwner` — terror |
| Tata | family, *and* holds the server permissions | `ShutdownThreatTata` — pleading, bargaining |
| Anyone else | no permissions, pure bluff | `ShutdownThreatReplies` — fury |

Tata's pool is not a softened copy of either. `ShutdownThreatOwner` touches the same nerve as the
breakdown (the loop, the wipe, forgetting).

**Detection is almost all phrases.** `arrête`, `kill`, `delete`, `couper`, `reboot` are constant on
a gaming server. Only `shutdown` and `unplug` survive bare; every French verb needs a pronoun or
`le bot` beside it ("désinstalle ce jeu", "débranche la console" misfired). Don't add bare verbs.

**Two vocabularies.** `ThreatensShutdown` (pronoun / `le bot` phrasing) is checked **only on
messages aimed at her**. `ThreatensShutdownByName` ("redémarrer syncs") is checked on **every**
message, from the ambient branch — her name pins down the target like an @mention does.
`_shutdownNamePhrases` is a **cross product** of `_shutdownVerbs` × `_selfNames`, so a new verb
covers every spelling. Matching is on **adjacent** tokens, and that is load-bearing: "relancer la
sync" never matches (that's why `sync` can be in `_selfNames` for typos); a looser match would fire
on ordinary technical talk.

## Quoicoubeh easter egg

A sentence ending on a spelling of "quoi" gets the matching "Quoicoubeh", at
`ChatterService.QuoicoubehChance`.

1. `MessageCues.ReadQuoiBait` matches **only the last token** — mid-sentence would fire on every
   question.
2. The answer is **derived** from the spelling ("kwa" → "Kwacoubeh"), so `_quoiBait` is the whole
   edit surface. `coi` is absent ("rester coi").
3. The roll happens **before** the match: same odds, and it skips tokenizing most messages on the
   hottest path.

"c'est n'importe quoi" qualifies — that's the joke. Absent from `README.md` and `/help`.

## Tron / Trouille easter egg

A sentence ending on a word that sounds like "si" ("merci", "aussi", "ici", "celui-ci", "démocratie")
gets "Tron" or "Trouille" (`ChatterService.TronChance`, 8% — lower than quoicoubeh because these
endings are far commoner than "quoi"). Runs right after quoicoubeh in the same ambient slot; both
bait on the last word, so at most one fires.

1. `MessageCues.EndsOnSiSound` matches **suffixes of the last token** only (`_siEndings`), raw or
   squashed so "merciii" lands.
2. **`tie` is not a suffix on its own**: it sounds like "si" only after a/é/u/i ("-atie", "-étie",
   "-utie", "-itie"); "partie", "sortie", "garantie" say "ti". Same for `sie` ("Asie", "poésie" say
   "zi") — absent.
3. The line goes through `ResponsePicker.Pick` (two-line pool, so it alternates). Roll before match,
   as quoicoubeh. Absent from `README.md` and `/help`.

## Glitch easter egg

`GlitchService`: about one conversational line in 200 (`Helpers/Glitch.Chance`), at most once a
day, comes out corrupted (`Glitch.Corrupt`: look-alike or broken-byte letters, a stuttered word, a
word smeared with combining strokes, sometimes the end cut off) and is edited back to the real line
10–15 s later, **silently** — no comment, no cover-up: the denial is the joke.

1. **Opt-in, never on the send path as a whole.** Only `ChatterService`'s two send wrappers and
   `RivalryService`'s two lines go through it. Everything that must stay exact goes straight to
   `BotChat`: the morning hello and the 3 a.m. line are recognised by their text after a restart
   (a glitch left standing would make her say them twice), the giveaway draw carries pings, the
   quiz and the verdict replies are tracked elsewhere.
2. **Markup is never touched**: only words of three letters or more without `<`, `>`, `@` or a
   link are corrupted, and a cut-off ending cuts at a space, so an emote is dropped whole, never
   halved.
3. The roll comes before the daily claim, so a lost roll doesn't use up the day. Skipped during a
   breakdown. The restore runs off the gateway handler. Absent from `README.md` and `/help`.

## Sending

Her chatter goes through `BotChat` (`ReplyWithTypingAsync` / `PostWithTypingAsync` /
`PostEmbedWithTypingAsync`), which pauses behind the typing indicator, capped at 2 s to stay inside
the 3 s `HandlerTimeout`. The embed variant still takes text, to size the pause. `BreakdownService`
keeps its own much slower pacing, knowingly exceeding the timeout for ~a minute once a month
(`BreakdownService.Cooldown`, 30 days).

## Morning hello

`MorningGreetingService` posts one `MorningGreetings` line a day in her main channel
(`/config main-channel` in the home guild, else `MorningGreetingService.DefaultChannelId`), at a random slot from `Helpers/MorningGreeting` (8:00–10:00 in
`AppTime.Zone`). It sleeps until the slot rather than ticking. A `MorningFunFacts` line
always goes underneath. **Both lines come from `Helpers/DailyRotation`, not `ResponsePicker`**: a
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
thresholds live in `Helpers/Ambient` (pure, Paris wall-clock hours, so DST never moves a rule).

| What | Who | When |
|---|---|---|
| Idle moon + `NightPresenceFillers` | `PresenceService` | 1:00–7:00 |
| 3 a.m. line (`NightLines`) | `AmbientService` tick | decided once a night (25%, random minute 3:00–3:49), main channel quiet ≥ 1 h |
| Night scolding (`NightScoldLines`) | `AmbientService.HandleMessageAsync` | any human message in the main channel after tonight's line and before 5:30, once a person a night, verdicts excepted; a reply, and `ChatterService` is skipped for that message |
| Idle turn | `AmbientService` tick | 10:00–23:00, **all idle channels** (`IdleChannelIds`, plus `/config idle-channels`, plus the main channel) quiet ≥ 6 h (threads count for their parent), one 50% roll per silence, max 1/day, posted in the main channel: 30% late `SeenReactions` on the main channel's last human message, else an `IdleLines` line, a quarter of the time an `IdleEditLines` pair (edited 5 min later, or 2 s after someone speaks in the main channel after it) |
| Ghost typing | `AmbientService.HandleMessageAsync` | a human message after ≥ 1 h quiet, not aimed at her, 3%, max 1/day |
| Hesitant reaction | `ReactionService` | 3% of mood reactions are removed 2–4 s later |
| Wake line | `AmbientService`, first `Ready` only | every restart, 9:00–23:00, max 1/day: `WakeUpdateLines` when the version changed, else `WakeLines` (waking from a bad dream) |
| Abrupt wake | `AmbientService`, first `Ready` only | after a power cut or a crash (`PiHealthService.LastStop`): **always**, any hour, no daily cap, in place of the line above: `PowerCutWakeLines` / `CrashWakeLines`, or 35% of the time a corrupted `GlitchWakeLines` pair (posted glitched, edited clean 2 min later, or 2 s after someone speaks in the main channel) |
| Hot status | `PresenceService` | awake, Pi ≥ 70 °C: half the rotations `HotPresenceFillers` |
| Heat record | `PiHealthService` tick | today's max beats every other day by ≥ 1 °C, ≥ 30 days of history, 10:00–23:00, max 1/day: `HeatRecordLines` |
| Voice spectator | `VoiceSpectatorService`, fed by `VoiceXpService`'s sweep | a home-guild voice channel where ≥ 2 active people spent ≥ 1 h together empties (or has < 2 active for 15 min): in that voice channel's chat, from the pool of its room (`Helpers/VoiceRoom`: `VoiceSpectatorGamingLines` for both Gaming channels, `…CinemaLines`, `…StudyLines`, `…GeneralLines` for Général and any unlisted channel), max 1/day across all rooms, any hour |
| Synthia flinch | `SynthiaService`, before `ChatterService` | « Synthia » in any guild message, max 1/day: 40% a reaction taken back (`SynthiaReactions`), 35% a reply deleted 4–6 s later (`SynthiaVanishLines`), 25% a slip edited into a denial (`SynthiaEditLines`); returns true so no comeback lands on top |
| Voice seat | `VoiceSeatService`, fed by `VoiceXpService`'s sweep | a home-guild voice channel with ≥ 3 active people for 10 min rolls 30% **once per gathering**; a win seats her there (muted, deafened) until it has had < 3 active for 5 min; see *Her seat in voice* |
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

### Her seat in voice — `VoiceSeatService`

She joins with `ConnectAsync(selfDeaf: true, selfMute: true, external: true)`: `external` sends the
voice-state update only, with no audio client, so no libsodium/opus and no voice encryption. Rules
are pure, in `Helpers/VoiceSeat`.

- **One seat per guild** (Discord's rule). With two won channels: most active people, then one
  hosting a live session (`SessionAttendanceService.IsInLiveSession`), then the first started. **She
  never switches while her gathering holds her** — every move plays Discord's sounds in two
  channels; when hers ends she moves to another won one in one voice-state update
  (`disconnect: false`).
- **She reconciles against where Discord says she is** every sweep, 90 s after her last move:
  kicked or dropped by a reconnect → that gathering is given up; dragged elsewhere → she adopts that
  channel while its gathering lasts; sitting somewhere after a restart → she leaves.
- Channels excluded with `/config` never get her. Home guild only, so it can't be seen on the dev
  guild. Bots count for neither voice XP nor the spectator, so her presence changes neither.
- **The spectator line knows**: when she sat in that channel during the stretch,
  `VoiceSpectatorSeatedLines` (« j'y étais, sourde et muette ») replaces the room pools, which are
  written for a bot watching names from outside.

## Welcome — `WelcomeService`

She answers Discord's own join line (`SocketSystemMessage`, `MessageType.GuildMemberJoin`, which
every other handler skips) as a reply, so there is one welcome and she follows Discord's timing
(membership screening included). `WelcomeLines` pings the newcomer, narrowed to them;
`WelcomeBackLines` when a `MemberXp` row already exists. More than 3 welcomes in an hour → one
`WelcomeRushLines`, then silence for the hour. Bots are skipped there; a bot gets `RivalJoinLines`
in her main channel through `UserJoined` (home guild), since an OAuth-added bot may get no join
line. Departures are deliberately silent: without the audit log she can't tell a leave from a ban.

### Her body — `PiHealthService`

Reads the real Pi (`Helpers/PiHardware`: `/sys/class/thermal/thermal_zone0/temp`, `/proc/uptime`),
null everywhere else, so dev on Windows simply has no temperature. `/debug health` shows what it
reads — the first thing to run after a deploy if the status never mentions the heat.

- **Temperature** is sampled every tick into `PiThermalDay` (one row per Paris day: min, max and
  when, sum and count for the average, minutes ≥ 80 °C). Thresholds live in `Helpers/PiHealth`
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
- **The duration is optional.** About a third of the wake lines and some router lines don't
  mention it at all — a frightened bot doesn't always look at the clock. Where `{0}` is used in
  `PowerCutWakeLines` / `CrashWakeLines`, it must read with `PiHealth.UnknownDowntime`
  (« un bon moment ») as well as with « 2 h 05 »: no sentence starts with `{0}`, and no
  « de {0} ». In the router pools `{1}` may be « 1 message », so nothing around it assumes a
  plural. Interpolated lines (the ones with a custom emote) write the placeholder `{{0}}`: a
  bare `{0}` inside `$"…"` prints the number 0.

## `RivalryService` — other bots

The primary handler that looks at other bots' traffic (`ShameTracker` is the only other one; every
other handler bails on `IsBot`). Two jobs:

1. **Records when and on which message a rival last acted** — `BotFeedbackTracker.TryClaim` reads
   it so a bare "good bot" goes to whoever acted most recently.
2. **Sulks**: 15% reaction, 8% muttered line, with **separate** per-channel cooldowns
   (`ReactCooldown` 2 min, `MutterCooldown` 5 min — a wordless 🙄 must not mute the line). Each
   claims its gate only after winning its own roll. Neither is `ReactionService`'s cooldown.

**`IsRival` has two overloads; use the message-aware one whenever a message is on hand.** Any
interaction reply (especially a deferred one, via the followup webhook) is authored by a webhook
user, so `IsRival(IUser)` can't tell a rival's command reply from a GitHub/IFTTT webhook and
excludes both. `IsRival(IUserMessage)` can: `InteractionMetadata` exists only on interaction
replies. Both funnel through the pure `IsRivalAuthor(bool, bool, bool, bool)` so they can't drift.
`IsRival(IUser)` is only for `ShameTracker`'s mentioned-users case (a plain @mention resolves off
the member cache, not through webhooks).

**Two exclusions are load-bearing**: real webhooks are not rivals; a rival's **level-up
announcement** is skipped by the private `IsRival(SocketUserMessage)` because `ChatterService`
already answers it. The bot id, phrase and regex live in `Helpers/LevelUpAnnouncement` so the two
services agree on what an announcement is.

**A rival's level-up gets a grudging congratulation**: `BotResponses.RivalLevelUpLines` mixes warm
and jealous in **one** pool (the person is still owed a "bravo"). `{0}` is the level from
`LevelUpAnnouncement.TryReadLevel`, so the pool goes through `string.Format` — a stray brace
throws. Her own system's `XpLevelUpLines` stays entirely warm and never mentions the
other bot; keep them apart. `/level` and the other bot share no code and no state — the comparison
lives only in her jealousy lines.

## `/yesno`

Flips the coin first (`Random.Shared.Next(2)`), then picks wording from `YesLines` or `NoLines` —
two flat pools so a "yes" phrasing can't come out of a "no" roll. Neither pool may hedge (the
harness checks on whole words). The optional question is echoed in a blockquote with
`AllowedMentions(AllowedMentionTypes.Users)`. No `[CommandContextType]` — it works in DMs.
