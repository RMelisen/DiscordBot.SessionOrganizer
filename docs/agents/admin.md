# `/admin`, `/config`, `/debug` and permissions

The three authorization models are summarised in the root `CLAUDE.md`. Don't conflate them.

## `/admin`

Holds `xp`, `plynling` (`rename`, `resurrect`, `passion-reset`, `cure`), `pebble`, `stats` and
`dashboard`. **Guarded once per handler by `SessionPermissions.IsStaff`, in code only — no
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

## `/config`

A flat `/config show` (a static Components V2 card, `ConfigCards.Build`) plus one subgroup per
setting: `excluded-channels add|remove`, `idle-channels add|remove`, `shame-voters add|remove`,
`moderator-role set|clear`, `game-channel set|clear`, `main-channel set|clear`,
`quiz-channel set|clear`. Three levels is
Discord's maximum nesting. (`/shame` is flat only because it had to stay invokable bare; nothing here
needs that.) Every handler goes through `ConfigModule.BeginAsync` (`IsStaff`, then an ephemeral
defer); no `[DefaultMemberPermissions]`, same reason as `/admin`.

**Two rules, by the shape of the setting.** An unconfigured guild behaves exactly as before either way.

- **Lists are additive to the code, never a replacement.** `GuildExcludedChannel`,
  `GuildIdleChannel` and `GuildShameVoter` hold only what `/config` added; `XpTracker.ExcludedChannels`,
  `AmbientService.IdleChannelIds` and `ShameModule.ExtraVoters` stay in force regardless, each exposed
  read-only as `Hardcoded…` for the refusals and the card. No config change can *remove* an exclusion
  or revoke a voting right: `remove` refuses a hardcoded entry outright (rather than appearing to
  work), and `add` refuses one too (rather than storing a redundant row that could drift). The
  moderator role and `shame-voters` only widen who may vote on `/shame`; voters refuse bots.
- **Single channels replace their default.** `GuildSettings.GameChannelId` / `MainChannelId`, zero
  meaning unset: a destination grants and revokes nothing, so a configured one simply wins, and
  `clear` (or setting the default itself, stored as zero) falls back. `set` refuses a channel the bot
  can't view and send in — a silent destination would otherwise fail on every use with nobody told.
  **`QuizChannelId` is the exception: it has no default**, so zero means no quiz, `clear` turns it
  off, and it is per guild (see `quiz.md`).

**Main and idle channels are home-guild only.** `MorningGreetingService` and `AmbientService` keep
one state each, so they live in `Helpers/HomeGuild.Id` and read that guild's config
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
