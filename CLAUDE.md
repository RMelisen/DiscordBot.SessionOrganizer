# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Project S.Y.N.C.S. — a French-language Discord bot for scheduling gaming sessions,
polls and votes. See `README.md` for the user-facing feature list; this file covers
what you need to know before changing code.

## Commands

```bash
cd ProjectSYNCS
dotnet build                     # net10.0
dotnet run                       # needs Discord:Token
dotnet ef migrations add <Name>  # migrations are applied automatically on startup

dotnet user-secrets set "Discord:Token" "<token>"           # dev credential
dotnet user-secrets set "Discord:DevelopmentGuildId" "<id>" # instant registration
```

Leave `Discord:RegisterCommandsGlobally` at `false` in dev: guild-scoped commands
register instantly, global ones take up to an hour to propagate. Guild registration
runs with `deleteMissing: true`, so the dev guild's command list is replaced by
whatever the assembly declares on every start.

The bot needs the two privileged intents (`GuildMembers`, `MessageContent`) enabled
in the Discord developer portal. Without `MessageContent` the whole personality
subsystem silently reads empty strings and stops reacting.

There is no test project and no CI. Verify changes by building and, when it
matters, running the bot against the dev guild.

## Language

All user-facing strings — command names, descriptions, embeds, button labels,
error messages — are **in French**. Code, comments and logs are in English.

## Architecture

`Program.cs` is the composition root: DI wiring, `MigrateAsync()`, then three hosted
services.

- **`BotService`** — gateway login, slash-command registration, interaction
  dispatch. Fans `MessageReceived` out to `EmoteTracker`, `ReactionService` and
  `ChatterService`, `ReactionAdded` to `EmoteTracker` then `ReactionService`, and
  `ReactionRemoved` to `EmoteTracker`.
- **`ReminderService`** — a single 5-minute loop that does three independent jobs:
  reminder DMs, session lifecycle card re-renders, and poll auto-close.
- **`PresenceService`** — rotates the cosmetic status line every 5 minutes. Its
  interval is deliberately *not* shared with `ReminderService`, whose 5 minutes are
  load-bearing. Lines go out via `SetCustomStatusAsync`, **not** `SetGameAsync`: a
  custom status renders verbatim, whereas the `ActivityType` verbs are prepended and
  localised to whoever is *looking*, so a French line read "Watching le vide" on an
  English client. Any line needing a verb spells it out itself. Note a custom status
  carries its text in the wire model's `State` field rather than `Name` — which is
  why it needs the dedicated call; `SetGameAsync` with `ActivityType.CustomStatus`
  compiles fine and renders nothing.

`BotService` owns the gateway subscriptions, with one exception: `PresenceService`
hooks `Ready` itself, because Discord drops the bot's presence on every reconnect and
that re-apply belongs next to the rotation logic. Don't go looking for it in
`BotService`.

Layers: `Commands/` (slash modules + the embed/component builders), `Interactions/`
(component handlers and modal DTOs), `Services/` (EF repositories + behaviour),
`Models/`, `Data/AppDbContext.cs`, `Helpers/`.

Slash modules: `ScheduleModule`, `PollModule`, `VoteModule` (group modules),
plus the flat `EmoteStatsModule`, `HelpModule`, `SpeakModule` (`/tell`, `/dm`) and
`AbsenceModule` (`/absent`). Component handlers for the published cards live apart
from the wizards, in `Interactions/Components/` (`EventComponentHandler`,
`PollComponentHandler`).

Two stateless helpers wrap the outward Discord side effects of a session:
`SessionEventSync` (create/update/delete the native Guild Scheduled Event) and
`SessionNotifier` (cancellation DMs).

**DI lifetimes are not arbitrary.** `AppDbContext` and the services that wrap it
(`EventService`, `PollService`, `EmoteStatsService`) are **transient**; the
personality collaborators that hold in-memory state (`ChatterService`,
`BreakdownService`, `AvailabilityService`, `ResponsePicker`, `EmoteTracker`,
`ReactionService`) are **singletons**. Registering a stateful service as transient
silently drops its state — `ResponsePicker` as transient would forget every line the
instant it returned one, and `ReactionService` would lose its cooldown and react to
every single message.

**Singletons never inject a DB service.** `ReminderService` and `EmoteTracker` take
`IServiceProvider` and open `_services.CreateAsyncScope()` around each unit of work,
resolving `EventService` / `PollService` / `EmoteStatsService` inside. Injecting the
transient service into the singleton would pin one `AppDbContext` for the process
lifetime. Match that pattern in any new background or gateway-driven work.

The "personality" subsystem is separate from the scheduling one: `ChatterService`
decides *how* to react to a message, `BotResponses` holds the canned lines,
`ResponsePicker` chooses which one, `MessageCues` does the nice/mean/greeting intent
detection, `BreakdownService` plays the easter egg, `ReactionService` answers with an
emote instead of words.

**`ChatterService` and `ReactionService` split the room.** Anything aimed at the bot
— an @mention, or a reply to one of its messages — belongs to `ChatterService`, and
`ReactionService` explicitly skips those so the bot never both roasts and decorates
the same message. Reactions exist for the conversations *nobody* addressed to it. A
message qualifies on a `MessageCues` hit, or on being the owner's (he qualifies on
anything — that's the favouritism), and is then gated by a probability roll and a
per-channel cooldown.

**`ReactionService`'s two paths are gated differently, on purpose.** Reacting to a
*message* is rationed by `Cooldown` because it is the bot volunteering an opinion;
copying someone else's *reaction* (`HandleReactionAddedAsync`) is odds-only with no
cooldown, because piling on is meant to read as reflexive. Don't "unify" them.

## Conventions that will bite you

**SQLite cannot translate `DateTimeOffset` comparisons.** Every query filters
booleans/ids in SQL, calls `ToListAsync()`, then applies the date window and
ordering **in memory**. See `EventService.GetActiveEventsAsync` and
`GetEventsNeedingReminderAsync`. A `.Where(e => e.ScheduledAt > now)` sent to the
database throws at runtime.

**`EmoteStat` and `EmoteDailyStat` are two different questions, not one denormalised.**
`EmoteStat` is the running all-time total and predates any notion of *when* — its
history has no dates to recover, which is exactly why `/emotestats`' rolling windows
needed a second table rather than a column. `EmoteDailyStat` buckets the same
counters per day and is written in the same call, so every bucketed emote has an
`EmoteStat` row and display markup is resolved from there (a rename stays in one
place). **Summing every bucket does not reproduce `EmoteStat`, and must not be made
to** — everything counted before the buckets existed lives only in the totals.
`EmoteDailyStat.Day` is an `int` `yyyymmdd` from `AppTime.DayKey`, not a date,
precisely so the rolling-window filter runs in SQL instead of joining the
in-memory-filtering pattern below. A reaction *removal* always decrements today's
bucket even if the reaction was added weeks ago; tracking the original day would
mean a row per reaction.

**`BotFeedback` / `BotFeedbackDailyStat` is the same pair for the same reason.**
All-time totals in one table, per-day buckets in the other, both written by
`BotFeedbackService.AddAsync` so every bucketed user has a totals row; the buckets
likewise do not sum to the totals. Any future dated leaderboard should follow that
shape rather than inventing a third one.

**Ranking commands share their window vocabulary.** `StatsPeriod` lives in its own
file (`Services/StatsPeriod.cs`) and `Helpers/StatsPeriodUi` owns the French labels
and the three-button filter row, so `/emotestats` and `/goodbot` cannot drift into
labelling the same window differently. Each passes its own custom-id prefix and gets
`{prefix}:{period}:0` ids — changing the window resets to page 0, since the ranking
is a different list. Their *defaults* deliberately differ: `/emotestats` opens on 30
days because recent activity is the point, `/goodbot` on all-time because verdicts
are rare enough that a rolling window is usually empty.

**Discord snowflakes are `ulong`; SQLite integers are signed 64-bit.** Every
snowflake property needs `.HasConversion<long>()` in `AppDbContext.OnModelCreating`.
Easy to forget when adding a model. A *derived* property on a model needs
`[NotMapped]` instead (see `EmoteStat.Markup`), or EF tries to map it and demands a
migration for a column that should not exist.

**Never use `DateTime.Now`.** Production runs in UTC; all wall-clock handling goes
through `Helpers/AppTime` (pinned to `Europe/Paris`, DST-aware via
`TryParseWallClock`). Store instants as UTC `DateTimeOffset`, render them to users
as Discord `<t:unix:...>` timestamps so the client localises them.

**Globalization must stay on.** `InvariantGlobalization` is explicitly `false` and
the Dockerfile installs `libicu72`, because `AppTime` resolves `Europe/Paris` and
the day/slot labels are formatted with `fr-FR`. Dropping either turns dates into
invariant-culture output or falls back to `TimeZoneInfo.Local`.

**`[ComponentInteraction]` and `[ModalInteraction]` inside a `[Group]` module need
`ignoreGroupNames: true`.** `ScheduleModule`, `PollModule` and `VoteModule` are all
group modules, so without that flag Discord.Net prefixes the group name onto the
custom-id and the handler simply never fires — no error, no log. Every such
attribute in those files already passes it; match that when adding one.

**Custom-ids are a contract across files.** `PollModule.OnCategoryPickedAsync`
hand-builds a modal with custom-id `schedule:finalize:{category}:{datetime}` to
reuse `ScheduleModule`'s handler, and `VoteModule.VoteListAsync` reuses
`poll:republish` from `PollModule`. Renaming a custom-id means grepping the whole
project, not just the declaring module.

**Modal DTOs and hand-built `ModalBuilder`s must stay in sync.** `Interactions/Modals`
holds the `IModal` DTOs, but three paths build the same modals by hand to pre-fill
them — `ScheduleModule.BuildEditModal` (binds to `EditSessionModal`),
`ScheduleModule.OnRetryAsync` and `PollModule.OnCategoryPickedAsync` (both bind to
`ScheduleEventModal`). Adding or renaming a field means touching the DTO *and* every
hand-built builder, or the value silently fails to bind.

**Embed and component builders are shared.** `ScheduleModule.BuildEventEmbed` /
`BuildEventComponents` and the `PollModule` equivalents are `static` and are called
from the component handlers and from `ReminderService`. Change a card's rendering
in one place and every re-render path follows. `PollModule`'s pair renders both poll
kinds, branching on `Poll.Kind`, so `/vote` cards go through it too.

**The two wizards keep state differently.** `ScheduleModule` threads its state
through component custom-ids — e.g. `schedule:min:{category}:{date}:{hour}:{minute}`
— so adding a step means threading a new segment through every handler *and* the
matching `Retour` handler; only the title is held in memory (`_draftTitles`), so a
failed modal can be reopened pre-filled. `PollModule` and `VoteModule` cannot do
that (a variable-length list of slots/options does not fit in a custom-id), so they
keep the whole draft in a `static ConcurrentDictionary` keyed by user id, removed
on finish or cancel.

All in-memory state resets on restart **by design** — the draft dictionaries above,
`BreakdownService`'s channel cooldown, `ResponsePicker`'s per-channel line history,
`ReactionService`'s per-channel cooldown, and `AvailabilityService`'s absent flag and
its bounded (200-entry) map of forwarded mentions.

**The bot can only react with an emote it shares a guild with.** Unicode emoji in the
`*Reactions` pools are always safe; a **custom** emote only works because it is the
server's own, and Discord rejects the reaction otherwise. This is why copying someone
else's reaction runs through `ReactionService.CanUse` first — people paste emotes from
their other servers constantly, and those are not copyable. It is also why reactions
are drawn from curated pools only, never from the `EmoteStats` leaderboard, which
records emotes from anywhere.

**A custom emote in a `*Reactions` pool must carry its snowflake id** —
`<:name:1234…>`, never `<:name:>`. `ReactionService.ParseEmote` falls back to
`new Emoji(markup)` when `Emote.TryParse` fails, so id-less markup yields an "emoji"
whose name is the literal `<:name:>` string. It is non-null, so the guard below it
passes, Discord rejects the reaction with a 400, and the exception is swallowed and
logged. The reaction silently never appears **and** the wasted attempt has already
burned that channel's 10-minute `Cooldown`, since `TryClaimChannel` runs before the
parse. There is no compile-time or startup check for this; the only symptom is a
warning in the logs.

**`BotResponses.MeanReactions` does double duty.** It is both the pool the bot reacts
*with* when a message reads hostile, and the definition of "hostile" used to decide
what it refuses to pile on to on the owner's messages. Adding an emote there changes
both behaviours. Membership is tested on the parsed `IEmote`, not the markup string,
so a custom emote still matches after someone renames it on the server.

**`MessageCues` cues are weighted, not boolean.** `Analyze` returns a `MessageMood`
(`Emotion` + `IsGreeting`) and scores the whole message rather than short-circuiting
on the first hit. Four things follow. **One:** a cue listed in `_weakCues` scores 0.4
instead of 1.0 and cannot fire alone — that is where the words with an innocent
reading go (`cool`, `ferme`, `rate`, `zero`, `merde`, and `claque`, since "ça claque"
is a *compliment*). Two weak cues together reach the 0.8 threshold, so adding an
ambiguous word to `_niceCues` or `_meanCues` without also listing it in `_weakCues`
is how you get misfires. **Two:** negation reaches *backwards* three tokens for
everything, and also *forwards* two — but only for the cues in `_verbCues`, because
chat French drops the `ne` and leaves the `pas` after the verb (`j'aime pas`). A
forward window for every cue would let "merci, pas de souci" cancel its own thanks.
**Three:** mean no longer cancels nice absolutely, it wins on margin, so `super nul`
reads mean while `merci, t'es pas nulle` reads nice. Greeting is a separate axis and
survives alongside either; callers that want the old "a mean word cancels the
greeting" rule check `Emotion` themselves, as both do today. **Four:** every cue must
survive `TokenizeOrdered` unchanged or it is unreachable — that is why `"3.0"` was
removed (it tokenizes to `["3","0"]`), and why cues are stored lowercase and
accent-stripped. Custom emotes that carry a mood are matched by **id**
(`_niceEmoteIds`, `GreetingEmoteId`), never by name, so a rename on the server can't
break them. `IsMistakenIdentity` is untouched by all of this — it is an identity
check, not a mood, and stays a plain `bool`.

**A "good bot" / "bad bot" verdict short-circuits three services.**
`MessageCues.ReadFeedback` is checked *separately* from `Analyze` — it is a verdict
on her, not a mood — and `BotFeedbackTracker` owns the response. Both
`ChatterService` and `ReactionService` bail out early on a non-`None` verdict, and
they have to: without the `ChatterService` guard a "good bot" replying to one of her
messages falls into the reply-to-bot branch and fires a *comeback*, so praising her
gets you insulted; without the `ReactionService` guard, "gentil bot" also scores
`Nice` and both services reach for `NiceReactions` on the same message. The
`ChatterService` guard sits **after** the owner-DM-relay branch so a relayed reply
still works, and is guild-only.

**`BotFeedbackTracker` learns what the bot did by watching the bot.** `MessageReceived`
and `ReactionAdded` both fire for the bot's own traffic, so it records "I acted here
at T" without `ChatterService` or `ReactionService` telling it anything — which is
why it is a separate service rather than a branch in either. It is therefore the one
handler in `BotService`'s fan-out that must *not* skip the bot's own messages.
Attribution is anything attached to one of her messages — a reply, or a 👍/👎 on it
(both always count) — or a plain message within a 5-minute window of her last action
in that channel. The window exists because the server has other bots, and a bare
"good bot" after Quokka does something would otherwise land in her column. Counting
is one verdict per person per action *shared across all three routes*, which also
gates the response, so holding down Enter after one joke earns one ❤️ and so does
thumbing down her whole backlog. State is in-memory and resets on restart like the
rest of the personality.

**Her own acknowledgement must not count as a new action.** She answers "good bot"
with a reaction, and that reaction comes back on `ReactionAdded` looking exactly like
anyone else's. Recorded naively it becomes a fresh action, clears `Judged`, and hands
the same person another free verdict — praise, get thanked, praise again, forever.
`MarkAcknowledged` is therefore called *before* the reaction is sent (the gateway echo
races it) and `HandleReactionAddedAsync` skips those message ids. Her bad-bot *reply*
is deliberately left counting as an action: unlike a wordless ❤️ it is new content,
and judging it is fair.

**The thumb path counts on her chatter only** — `resolved.Components.Count > 0` skips
session cards, poll cards and leaderboards, where a 👍 means "I'm in" rather than
praise. Buttons are the test rather than embeds because Discord attaches an embed to
any message carrying a link, so a plain chatty line could grow one on its own. The
path is also silent by design: a 👎 on an hour-old message would otherwise fire a
comeback into a dead conversation. Removing a reaction does **not** decrement — the
counters only ever go up, and the claim already prevents a re-count.

**`RivalryService` is the only handler that looks at other bots.** Every other one
bails on `IsBot`. It does two jobs with that traffic. **One:** it records when and on
which message a rival last acted, which `BotFeedbackTracker.TryClaim` reads so a bare
"good bot" goes to whoever acted *most recently* rather than always to her — before
this, praise a rival earned landed in her column whenever she happened to have spoken
in the last five minutes. **Two:** it sulks — 15% odds of a reaction on a rival's
message, 5% of a muttered line, sharing one per-channel cooldown of its own. That
cooldown is deliberately **not** `ReactionService`'s: a third trigger population
deserves its own gate rather than competing with her reactions to humans.

**Two exclusions in `RivalryService.IsRival` are load-bearing.** Webhooks are not
rivals (they post relentlessly and belong to no one). And a **level-up announcement**
is skipped while the rest of that bot's traffic stays fair game, because
`ChatterService` congratulates those and the congratulation is aimed at the person who
levelled — cheering and sulking at one message would be incoherent. The two services
therefore have to agree on what an announcement *is*, which is why the bot id, phrase
and regex live in `Helpers/LevelUpAnnouncement` instead of privately in
`ChatterService`.

**`TryClaim` reports *why* a verdict missed, not just that it did.** `Claim.RivalOwns`
is the jealousy trigger, and it is separate from `NoAction` precisely because "nobody
earned this" and "someone else earned this" call for different behaviour. Three rules
compose there: anything `unambiguous` (a reply to her, a thumb on her chatter) is hers
regardless of timing; a reply aimed at *another bot* is never hers regardless of
timing, the exact mirror; everything else goes to the most recent actor. Only a
**Good** verdict fires jealousy — someone calling a rival a bad bot is not a loss.
`_rivalry.LastAction` is read *outside* `_gate`, since `RivalryService` holds a lock of
its own and nesting the two in opposite orders would deadlock.

**`Helpers/BotChat` is the single send path for the bot's own chatter**, and
`Helpers/EmoteMarkup.Parse` the single reaction parser. Both were private members of
`ChatterService` / `ReactionService` until `BotFeedbackTracker` needed them too. The
typing-delay clamp in `BotChat` has to stay inside Discord.Net's 3 s `HandlerTimeout`,
and the parser carries the id-less-markup trap above — neither is a constant worth
having two copies of. `BreakdownService` still keeps its own much slower pacing.

**Cue vocabulary is scoped to a *gaming* server, and that constrains it.** Words that
compliment a person in general French name game content here, so `boss` and `monstre`
are deliberately **absent** from `_niceCues` entirely ("il est fort ce boss" read as
praise), and `heros`, `roi`, `reine`, `royal`, `divin`, `toxique` and `manchot` are
held at weak weight for the same reason ("dégâts toxiques", "arme divine", "la garde
royale"). `sale` is absent too: "c'est sale" is a *compliment* in gaming slang, and
the squashed-token fallback would also map the innocent "salle" onto it. Check a new
cue against session/loot/combat vocabulary before adding it at full strength.

**Never pick a response line with a bare `Random`.** Every pool goes through
`ResponsePicker.Pick(bucketId, pool)`, which avoids the entries most recently used in
that bucket — back-to-back repeats are what make a 95-line pool feel like a 5-line
one. The exclusion window is `min(10, pool.Length / 2)` precisely so a small pool
(`ReferenceComebacks` has 5 lines) can never have every candidate excluded. Pick the
raw template *before* `string.Format`, so the history dedupes on the template rather
than on one user's rendered name. The bucket is normally a channel id but only needs
to be stable — `PresenceService` uses `0`, which is never a real snowflake.

**Personality lines pause behind the typing indicator.** `ChatterService`'s
`ReplyWithTypingAsync` / `PostWithTypingAsync` are the only send paths for the bot's
own chatter, and they also carry the swallow-and-log. Two paths deliberately skip
them and send directly — the owner-reply relay and the DM acknowledgements — because
delaying a human's words or a "✅ transmis" receipt only adds latency. The pause is
capped at 2 s to stay inside Discord.Net's 3 s `HandlerTimeout`; `BreakdownService`
keeps its own much slower pacing, which knowingly exceeds it for ~a minute once a
month.

**`/vote create` must create its own wizard message.** The slash command posts the
"Définir le titre" message and every later step only *updates* it. Creating the
wizard message from the modal response instead drops the first option-add update —
that's why the extra `vote:begin` button exists rather than opening the modal
straight from the command.

**`ChatterService.HandleMessageAsync`'s branch order is load-bearing.** The
owner's DM reply relay is checked before the reply-to-bot branch, which would
otherwise swallow it as a reply to the bot and fire a comeback instead of relaying.
Add new branches with that precedence in mind.

**Session lifecycle is idempotent.** `SessionEvent.RenderedPhase` records what was
last drawn on the card, so the background loop only re-renders on an actual
Scheduled → InProgress → Finished transition.

**The timing constants are coupled to the 5-minute loop.** The reminder window in
`EventService.GetEventsNeedingReminderAsync` is 25–35 minutes before start — wider
than the loop interval so no session is missed and none is reminded twice
(`ReminderSent`, reset when the time is edited). `SessionEvent.Duration` (2 h) sets
both the InProgress → Finished transition and the native event's end time.
`ReminderService.PollLifetime` (2 days) drives auto-close, `BreakdownService.Cooldown`
(30 days) gates the easter egg.

**Discord side effects must never break the flow.** `SessionEventSync` (native
Discord events) swallows and logs every exception; a missing Manage Events
permission degrades silently rather than failing the session. Keep that property.
Reminder DMs likewise catch `CannotSendMessageToUser` specifically.

**Respect Discord's hard caps when building components.** 25 options per select
menu (the day picker and every `list` republish menu `.Take(25)`), 5 buttons per
row, 80-char button labels, 100-char select labels, 1000-char scheduled-event
description, 2000-char message (relays truncate to 1200–1500 to leave room for the
herald line and blockquote markers). Exceeding one throws at send time, not at build.

**There are two separate authorization models.** Session and poll management uses
`Helpers/SessionPermissions.CanManage` — the organizer, or any guild
Administrator / ManageGuild holder. The owner-only commands (`/tell`, `/dm`,
`/absent`) instead compare `Context.User.Id` against `AvailabilityService.OwnerId`
inline in the module and reply ephemerally. Don't conflate them.

**Relayed text must never become a mass-ping vector.** Every path that sends text
on someone's behalf (`SpeakModule`, `ChatterService`'s DM relay) passes
`new AllowedMentions(AllowedMentionTypes.Users)` — users only, never `@everyone`,
`@here` or roles — and renders quoted text through `MessageFormat.Quote` so relayed
words are visibly not the bot's own. Preserve both when adding a relay. The absence
notice forwarded to the owner goes further and uses `AllowedMentions.None`, since it
quotes someone else's text verbatim.

**`/help` is hand-maintained.** `HelpModule` duplicates the feature list in prose,
as does `README.md`; neither is generated. A new user-facing command means updating
both — except the owner-only ones, which are deliberately absent from `/help`.

## Version and deployment

`ProjectSYNCS/config.yaml` is the **single source of truth for the version** — the
csproj regex-parses it into `<Version>`, and `AppInfo.Version` surfaces it (in the
`/help` footer). Bump it there and nowhere else.

The bot ships as a Home Assistant add-on: `Dockerfile` publishes a self-contained
`linux-arm64` build, and `run.sh` maps the add-on options to `Discord__Token`,
`Discord__RegisterCommandsGlobally` and `Database__Path=/data/ProjectSYNCS.db`.
Only `/data` is persisted, so the SQLite file must stay under it.

The GitHub remote is **public**. `appsettings.json` ships a `BOT_TOKEN` placeholder
and `config.yaml` a `PASTE_YOUR_TOKEN_HERE` one; real tokens go in user secrets
(dev) or the add-on options (prod), never in a tracked file.

## Hardcoded ids

`AvailabilityService.OwnerId` (the owner, who gets special treatment throughout
`ChatterService` and again in `ReactionService`, both for what he says and for what
he reacts to), the level-up bot id in `ChatterService`, the `hi_cat` emote id in
`MessageCues` and `ReminderService`, and the per-user `PersonalComebacks` /
`RealNames` maps in `BotResponses` are literal snowflakes tied to one specific
server.
