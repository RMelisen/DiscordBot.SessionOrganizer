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

`tell`, `dm`, `absent`, `plynling`, `emotes`, `quiz` (post or close a pop quiz now, see `quiz.md`),
`sticker` (post any of her sticker strips here, bypassing the gate, see `personality.md`),
`answers` (which kinds of chat answer she gives most, and what she couldn't answer specifically,
see `personality.md`, *Answer stats*),
plus the Plynling testing tools (`event`, `stress`, `modifier`, `sweep`, in `plynling-events.md`), each comparing `Context.User.Id` to
`AvailabilityService.OwnerId` inline and replying ephemerally. `DebugModule` carries no
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
