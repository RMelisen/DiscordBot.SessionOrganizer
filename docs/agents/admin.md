# `/admin`, `/config`, `/debug` and permissions

The three authorization models are summarised in the root `CLAUDE.md`. Don't conflate them.

**The owner is staff on every server, on purpose** (the owner's call when she went multi-server):
`IsStaff` is not narrowed to the home server, and his mute and warn immunity follow him too.

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

`sessions`, `polls`, `community`, `syncs`, `activity`, `commands`, `incidents`, `quiz`, `plynlings`,
`emotes`, `sleeping`, `minecraft`: her reports to the staff. Same gate as `/admin` (`IsStaff` per handler, no
`[DefaultMemberPermissions]`, guild-only).

**`/report help` and `/debug help`** are the guides (`StaffHelpCards`, static, measured by the
checks): what each report shows, what each owner tool does. Both are private. `/report help` is gated
on `IsStaff`; `/debug help` is owner-only like every other `/debug` entry — they're dev tools, so it
stays out of `/help` and `README.md`. **A new `/report` or `/debug` subcommand gets a line in its
guide.**

**Everything else is in `admin/report.md`** — how report cards are built (`ReportCards`, `ReportService`, `ReportWindow`), each report's arithmetic and rules, incidents, her disk and power, joins and leaves (`MemberFlowDailyStat`), activity counting (`ActivityCounter`), and the monthly digest (`MonthlyDigestService`). Read it before touching any of those.

## `/config`

A flat `/config show` (a static Components V2 card, `ConfigCards.Build`) plus one subgroup per
setting: `excluded-channels add|remove`, `idle-channels add|remove`, `shame-voters add|remove`,
`moderator-role set|clear`, `plynling-role create|set|clear`, `quiz-role create|set|clear`,
`game-channel set|clear`,
`main-channel set|clear`, `quiz-channel set|clear`, `bots-channel set|clear`, `levelup-channel set|clear`,
`confess-channel set|clear`, `freegames-channel set|clear`, `feature enable|disable`, and the flat
`setup`. Three levels is
Discord's maximum nesting. (`/shame` is flat only because it had to stay invokable bare; nothing here
needs that.) Every handler goes through `ConfigModule.BeginAsync` (`IsStaff`, then an ephemeral
defer); no `[DefaultMemberPermissions]`, same reason as `/admin`. `ConfigModule` is a partial over
`Commands/Admin/ConfigModule/` (the class and shared helpers, `.Lists`, `.Roles`, `.Channels`,
`.Setup`, `.Features`); `/config` has 16 of Discord's 25 top-level children.

### `/config setup`, the health check, the hello card

- **`/config setup`** is the guided setup: one ephemeral V2 message, a page at a time
  (`ConfigSetupCards`, `SetupPage`: the channels, the optional channels, the roles, her life here —
  the main channel and a multi-select of the features —, a summary).
  **Stateless**: every pick is saved at once by `ConfigSetupComponentHandler`, through the very rules
  of the commands (`CanSendIn` refusal, the default stored as zero, the mirrored roles through
  `ConfigModule.SetManagedRoleAsync` / `CreateManagedRoleAsync`, an emptied select = `clear`); the
  page name rides each custom-id (`cfgsetup:ch:{slot}:{page}`, `…:role:{kind}:{page}`,
  `…:create:{kind}:{page}`, `…:feat:{page}`, `…:prev:{page}`, `…:next:{page}`, `…:done`), so a restart mid-setup loses
  nothing. `IsStaff` on every click; a refusal or a role's result comes as a private follow-up, then
  the page is redrawn. Channel selects read the raw values (an emptied select sends none). Default
  values carry only ids that still exist (Discord refuses the others).
- **The health check** is `Helpers/Admin/ServerHealth` (pure; facts from
  `Services/Admin/ServerHealthProbe`): each slot's channel against what it needs
  (`ServerHealth.NeedsOf`), named with Discord's French permission names, only the ones to give
  first (no « Voir le salon » → nothing else named; no « Envoyer des messages » → no links or
  files named: Discord denies those anyway), the fix said once (`HowToGrant`); no game nor bots
  channel; « Gérer les évènements »; the mirrored roles through `RoleMirror.Problem`. It opens
  `/config show` (`ConfigCards.HealthText`, with « Pas encore réglé ») and ends the setup.
  `CardPostFailure` takes its permission names from it. **Text budget**: a V2 message carries 4000
  characters; the checks build the worst `/config show` and measure it.
- **The hello card** (`GuildHelloService`, `JoinedGuild`; `GuildHelloCards`, `GuildHelloLines`):
  on being added to a server other than home, in its system channel if she may view, send and embed
  there, else the first text channel where she may. Swallow-and-log; not silence-gated (a notice).

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
  off, and it is per guild (see `quiz.md`). `ConfessChannelId` and `FreeGamesChannelId` work the same
  way (see `community.md`).
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

**Main and idle channels are per server.** `MorningGreetingService.MainChannelIdAsync` /
`MainChannelOf(config, guildId)` is the one resolver (morning, ambient, weekend poll, welcome's
new-bot line, PiHealth's lines): the configured channel, else `DefaultChannelId` **on home only**,
else 0 — the feature can't post there, and `/config show` warns « Sans salon principal : … ne font
rien ». The main channel always counts as an idle channel wherever it is set.

### `/config feature` — what she does on her own

`Helpers/Admin/ServerFeatures` (named so to stay clear of `Discord.GuildFeatures`): the keys
`morning`, `ambient`, `quokka`, `weekend-poll`, `voice-spectator`, `welcome` — **stored strings,
append-only**; `ConfigModule.FeatureChoice` mirrors them (the checks hold the two together).
`GuildFeatureSetting` (guild + feature, unique) stores **only staff's explicit choices**; with no
row a feature takes `DefaultFor(key, isHome)`: **all on at home, only `welcome` elsewhere** — so an
untouched home server behaves as before, and a new server stays quiet until staff opt in.
`GuildConfigService` loads the rows into `GuildConfig.Features` (cached like the rest);
`GuildConfig.IsOn(key, guildId)` is the one test. Set by `/config feature enable|disable` and the
setup's life page (`cfgsetup:feat:{page}`); `/config show` lists them on/off. Who reads which key:
`personality/ambient.md` (morning, ambient, voice spectator, welcome), scheduling (weekend poll),
arcade (Quokka), economy (`/shoutout` rides `morning`). **No switch, home only** (her single-instance
life): her voice seat, Voyager's letters, the monthly digest. The game channel is per guild (`PlynlingAnnouncer.ResolveGameChannelAsync`),
so a dev guild gets announcements once it configures one.

**`GuildConfigService` is a singleton that reads the database — the cache is why.** It takes
`IServiceProvider` and scopes per unit of work (never inject `AppDbContext`). The cache is
load-bearing: `XpTracker`'s exclusion check runs on *every* message, before `TryClaim`; uncached, that
is an EF scope and a query per message on a Raspberry Pi. It's cheap to keep correct because this
service is the only writer: any write drops that guild's entry. A failed read degrades to
`GuildConfig.Empty` and is **not cached**, so a transient fault can't pin a guild as unconfigured.

## `/debug` — owner only

(Except `help`, readable by staff: see *`/report` and the monthly digest*.)

`tell`, `delete` (one of her own messages, by link; refuses anyone else's), `react` (adds — or with `remove` takes back — her reaction on any message, by link; the emoji goes through `EmoteMarkup.Parse`, and Discord refusing it is reported, not thrown), `confession` (who wrote
confession #N, see `community.md`), `dm`, `absent`, `plynling`, `emotes`, `digest` (last month's digest in your DMs now, yours or a preview of Tata's, see
*`/report` and the monthly digest*), `quiz` (post or close a pop quiz now, see `quiz.md`),
`sticker` (post any of her sticker strips here, bypassing the gate, see `personality/reactions.md`),
`silence` (see below),
`answers` (which kinds of chat answer she gives most, and what she couldn't answer specifically,
see `personality/listening.md`, *Answer stats*),
`role` (give a member a role; refuses managed / `@everyone` / not-below-her roles up front),
`milestones` (this channel's message count and milestones, see *Channel message milestones*),
plus the Plynling testing tools (`event`, `stress`, `modifier`, `sweep`, in `plynling-events.md`), and
`heal` (any member's living Plynling, yours by default: needs to 100 %, cured — journalled as a
recovery, like `/admin plynling cure` — stress 0, the death warning re-armed), `affinity` (any
member's shown Plynling, yours by default: every pair's raw affinity, gauge, hidden compatibility and
the affinity it draws toward — the numbers players never see, `DebugModule.Affinity`), each comparing `Context.User.Id` to
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

### `/debug silence`

`SilenceService` (singleton) holds **one switch per server** (the owner's call: per server only, no
global switch), kept in `silence-state.json` beside the database so it **survives a restart** (unlike
`absent`: the owner silences her for a reason that a power cut doesn't end). `/debug silence
state:[server:]` acts on the server it's run in, or the one picked (`GuildAutocompleteHandler`,
owner-only suggestions, needed from a DM); the reply lists the other silenced servers. **A DM falls
under the home server's switch** (`IsSilentIn`). The file's older one-switch shape (`Silent: true`)
reads as the home server silenced (`SilenceStateFile`, pure, checked in section 72). Every check asks
with the server the post would land in: `IsSilent(guildId)` in loops, `IsSilentIn(channel)` on the
gateway paths. While it is on she says nothing on her own initiative; **answers to commands, buttons
and `;` shortcuts still go out**, and so do the notices someone set up with a command: session
reminders, recaps and the roll call, giveaway draws, `/remindme`, the weekend poll's result, and
Plynling DMs to owners (warnings, sickness). **Level-up cards go out too**, the one exception that
nobody set up: the card reports something the member earned, it isn't her chatter. The monthly
digest (a DM) and her Minecraft body are not touched either, nor is her Discord status.

Where it is checked:

- **The message fan-out** (`BotService`): the counters (activity, emotes, XP, shame, Quokka's
  clock) and the verdict pair (`RivalryService`, `BotFeedbackTracker`) still run, then it returns
  before reactions, the morning wave, ambient, the quiz's typed answers, Synthia, robot heart and
  the chatter. The welcome line and her reaction pile-ons are skipped too, and so is the absence
  relay. **Verdicts keep counting** (tally, XP bonus, badges): the rivals' actions stay on record
  so attribution holds, and her command replies are still actions to judge. Only the answers go —
  her reaction or comeback to a verdict, the jealous line, the rival mutter and mark.
- **Each loop at the post itself**: the morning hello, ambient (night, idle, Voyager, wake line),
  a new bot's welcome, Quokka's slots, the Wednesday poll (left undecided, so it still goes out if
  silence lifts within its window), pop quizzes, the
  Monday crown announcement (purse and role still move), voice spectator lines, the heat record and
  router lines, and the sweep's Plynling death announcements and event stories (marked as told,
  never posted later).
- **Her voice seat**: she joins no call (a session she organised included), and leaves the one she
  sits in at the next settled sweep. No seat lines, no mic flicker.
- **`/debug quiz` and `/debug quokka` bypass it**: the owner asking is a command.

`absent` toggles `AvailabilityService`'s absent flag; mentions of the owner while absent are
forwarded to him (a bounded 200-entry map, `AllowedMentions.None` since it quotes others verbatim).

## Channel message milestones — `ChannelMilestoneService`

Every text channel's message count, history included, and the milestones it reaches
(`Helpers/Admin/ChannelMilestones`: **1 000, 10 000, 100 000, then every 100 000**, the owner's
numbers). Design: `docs/superpowers/specs/2026-11-17-channel-milestones-design.md`.

- **Which channels**: every guild's text channels she can view and read the history of (not threads,
  not voice or stage chats), minus the XP-excluded ones (`XpTracker.HardcodedExcludedChannels` +
  `/config`). Every guild, not only the home one, so the dev guild can be checked. Every message
  counts, bots' and hers included, shortcuts too: `BotService` feeds it **first** in the fan-out,
  before the shortcut return.
- **Catching up**: Discord gives no total, so each channel's history is read once, oldest first
  (REST, 100 a page, one channel at a time, each in its own `try`), from its saved `Cursor` (the last
  message counted) or its creation. **A milestone crossed while reading history is recorded, never
  announced** (the owner's rule). Live messages arriving meanwhile are held; when a page comes back
  empty the channel is marked caught up under the lock and the held ones past the cursor are
  counted, so the hand-over neither loses nor doubles one.
- **Live**: a caught-up channel counts in memory; a milestone reached is written at once (rare) and
  announced by replying to that message (`MilestoneLines`, `MilestoneBotLines` for another bot,
  `MilestoneSelfLines` for hers), pinging only its author, never a bot. Not in silence mode, under
  a « chut », during a breakdown, in a channel excluded since, or on a system message: recorded
  with `Announced = false` then.
- **Deletions are uncounted** (`MessageDeleted`, `MessagesBulkDeleted`): a message at or before the
  cursor (already counted) takes one off, never below zero. Deletions while she is offline are
  missed. A milestone is reached once per channel (`ChannelMilestone` unique on channel and number):
  dropping back under it and crossing it again announces nothing.
- **Saving**: `ChannelMessageCount` (count + cursor) every 30 minutes, the changed channels only,
  as each channel finishes catching up, and on a clean stop; never a write per message. After a crash
  the catch-up reads on from the saved
  cursor, so nothing counted is lost (deletions in that window are).
- **`/debug milestones`** (owner, ephemeral, deferred): the current channel's count (« je lis encore
  l'historique » while catching up), each milestone reached (author, date, link, announced or not),
  and the next one still to reach (`ChannelMilestones.NextUnreached`). **Paged**, `MilestoneCards.PageSize`
  = 20 a page (a line runs to ~175 characters, so a page stays under the 4096 cap; the checks
  measure it): page 0 is the newest, « Plus anciens » / « Plus récents » step back and forth
  (`MilestoneComponentHandler`, custom-ids `milestones:older:{channel}:{page}` and
  `milestones:newer:{channel}:{page}`, one verb per button; owner only, deferred, re-read each click).
  No buttons when everything fits on one page.
