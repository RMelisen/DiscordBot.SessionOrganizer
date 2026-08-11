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

**`GuildMembers` is necessary but not sufficient — `AlwaysDownloadUsers` must stay on.**
The intent only permits the member download; the flag is what performs it. Without it
Discord.Net caches only the people seen in events since the last restart, so
`Guild.GetUser` returns null for everyone else and every avatar on `/level`,
`/leaderboard` and `/shame` falls back to Discord's generic blue logo — worst on
all-time rankings, which are mostly people who have not spoken today. It is the first
thing to check if those placeholders reappear.

There is no test project and no CI. Verify changes by building and, when it
matters, running the bot against the dev guild.

## Language

All user-facing strings — command names, descriptions, embeds, button labels,
error messages — are **in French**. Code, comments and logs are in English.

## Architecture

`Program.cs` is the composition root: DI wiring, `MigrateAsync()`, then the hosted
services — `BotService`, `ReminderService`, `PresenceService`, `VoiceXpService` and
`GiveawayDrawService`. The last three each run their own interval on purpose; see the
notes below before sharing one.

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

Slash modules: `ScheduleModule`, `PollModule`, `VoteModule`, `GiveawayModule` (group
modules), plus the flat `EmoteStatsModule`, `BotFeedbackModule` (`/goodbot`),
`LevelModule` (`/level`, `/leaderboard`), `ShameModule` (`/shame`), `HelpModule`,
`SpeakModule` (`/tell`, `/dm`) and `AbsenceModule` (`/absent`). Component handlers for the published cards live apart
from the commands, in `Interactions/Components/` (`EventComponentHandler`,
`PollComponentHandler`, `GiveawayComponentHandler`) — the module keeps the commands and
the `static` card builders those handlers render through.

`GiveawayModule` is a group module but **not** a wizard: a giveaway is one slash command
with a fixed set of options, so unlike `PollModule`/`VoteModule` it keeps no draft state
anywhere. The duration is a `[Choice]` list rather than parsed text — nothing to reject,
and no error path to word in French.

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

**The pile-on path skips bots twice, for two different reasons.** A reaction *added
by* a bot is ignored (other bots' bookkeeping marks), and a reaction sitting *on* a
bot's message is ignored too — that one is the room appreciating a rival, and joining
in would have her applauding the competition while `RivalryService` is sulking at the
same message. The second check needs the message's author, so it costs a fetch and
lives inside the `try` alongside the owner and self checks rather than up front.

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

**Two migrations carry data, not schema**, and both are XP wipes with an empty `Down`:
`ResetMemberXp` shipped with the level-up card rework, `ResetXpTotals` with the voice-XP
taper. They ride the automatic apply-on-startup so they land in prod without anyone
touching the add-on's SQLite file. Every other migration here is schema-only and should
stay that way — a data migration is the one kind whose `Down` genuinely cannot restore
anything, so it is for deliberate one-off corrections, never a substitute for a real
feature.

**The two are not written the same way, and the difference matters.** `ResetMemberXp`
is `DELETE FROM MemberXps`, which was equivalent to zeroing the XP *at the time*: the
table then held nothing but `TotalXp`. It now also carries `ReactionsUsed` and
`VoiceMinutes`, which are facts about what people did rather than rewards, so
`ResetXpTotals` is `UPDATE … SET TotalXp = 0` plus the same on `MemberDailyStats.XpEarned`
— deleting the rows would falsify `/leaderboard`'s Réactions and Vocal views. Any future
wipe must make the same distinction: reset the reward, keep the record.

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

**Custom emote markup lives in `Helpers/Emotes` and nowhere else.** Each one is a
pair of `const string` — `XId` holding the snowflake, `X` holding the markup built
from it by compile-time interpolation — so `$"Gênaaaant {Emotes.Staring}"` is still a
constant and the pools stay `static readonly` arrays of constants. The ids are
strings rather than `ulong` deliberately: `MessageCues` only ever searches message
text for them, and a `ulong` hole would stop the markup being a constant expression.
Before this, `hi_cat` was written out thirteen times across three files in two
different shapes (markup in `BotResponses` and `ReminderService`, a bare `ulong` in
`MessageCues`), so a re-upload meant finding all of them. Never paste raw
`<:name:id>` into a response pool — emotes inside chat lines are otherwise reachable
by no test at all, and a typo there just renders as literal text in Discord.

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

**A verdict is cancelled by what sits just before it, or people game the tally.**
`"bad good bot"` and `"not good bot"` both used to register as **praise**: the matcher
tested whether the joined message *contained* the phrase, which throws away everything
preceding it. `SaysVerdict` now scans by token index so it can see the two tokens
before a match, and skips any occurrence preceded by a negator or by the opposite
verdict's adjective. Three details are load-bearing:

- **The window is two, not three.** Two covers the longest form worth catching
  ("pas un bon bot"); three reaches far enough that in "good bot… non en fait bad bot"
  the `non` of the *correction* cancelled the complaint and handed the message to the
  earlier praise.
- **A cancelled occurrence is skipped, not fatal** — the scan continues, so
  "not good bot… ok fine, good bot" still lands on the second one. Rejecting the whole
  message would be an easier trick than the one being closed.
- **Cancelling yields `None`, never the opposite verdict.** "not good bot" plainly means
  the complaint, but "pas un mauvais bot" plainly means the compliment, and inferring
  either would have her snapping back at praise on a misread.

`_goodBotAdjectives` / `_badBotAdjectives` are **derived** from the phrase lists rather
than written out, so adding "excellent bot" teaches the canceller about `excellent` in
the same edit — otherwise "bad excellent bot" would be a way straight back in.
`_verdictNegators` is `_negators` plus the English ones, kept separate so the mood
scoring — calibrated against thousands of assertions — is untouched.

**A framed verdict is not a verdict.** `IsFramed` refuses the whole message when it
carries reported speech ("il a dit good bot"), an explicit hypothetical ("imagine que…",
"supposons", "théoriquement") or a self-reference ("cette phrase", "ce message", "this
sentence") — which is what "cette phrase est fausse → t'es un bon bot" relies on. Note
what this is **not**: it catches framings that say so out loud, and no list will ever
catch a construction that never names itself. What actually bounds the damage is the
attribution layer — one verdict per person per thing she did, whatever wording gets
through. Whole-message here, unlike `SaysVerdict`'s adjacent-token canceller, because a
framing clause colours everything after it.

**"Good girl" is the same verdict with a different answer.** `ReadFeedback` has an
overload reporting a `VerdictForm` alongside the `FeedbackKind`: the tally treats
"good bot" and "good girl" identically — praise is praise — while `RespondAsync`
branches on the form, so praise in that register draws `GoodGirlReactions` (uwu,
witch_eheh, hearts, 🫦) instead of the generic `NiceReactions`. `VerdictForm` is a
separate enum rather than two more `FeedbackKind` values precisely because the two axes
are independent; folding them together would mean four states where two and two are
meant. **"Bad girl" gets `BadGirlReplies`, not `BadBotReplies`** — the latter are
wounded *professional* pride ("j'ai un uptime de 99,9%"), which lands wrong against a
scolding aimed at her as a person.

**Who said it and how they said it are crossed, not ranked — there are four pools per
verdict, not two.** `RespondAsync` switches on `(byOwner, form)`, giving
`GoodGirlReactionsOwner` / `OwnerReactions` / `GoodGirlReactions` / `NiceReactions` and
the same shape for the bad side. It originally tested `byOwner` first and returned,
which meant **the owner never reached the girl pools at all** — and he is both the
person most likely to try that wording and the only one testing, so the feature looked
completely dead while being perfectly wired underneath. The harness now asserts all four
are distinct object references, because sharing one would undo the split silently.

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

**About 1 in 100 "good bot"s gets a line instead of the usual silent reaction —
the praise turnabout.** `BotFeedbackTracker.RespondAsync` rolls `TurnaboutChance`
first, and on a hit calls `SendTurnaboutAsync` and returns before reaching the
reaction pools below, so the ordinary path is untouched the other 99 times. Which
pool she draws from is decided by `BotResponses.GenderFor`, not by `VerdictForm` —
"good bot" and "good girl" get exactly the same treatment here, because this is
about *who* said it, not *how*. **Deliberately not crossed** with `VerdictForm` or
`byOwner` the way the reaction/reply pools below are: a "small pool" stays small by
not multiplying itself against every other axis in this file, so the owner and Tata
draw from the same `TurnaboutBoyLines` / `TurnaboutGirlLines` as anyone else known
to `GenderFor` — no owner-flavoured turnabout pool exists.

**`GenderFor` is seeded from confirmation, never from a name.** Two entries in
`BotResponses.KnownGenders` are grounded in this file's own existing text — the
owner (`Boy`, "papa" throughout `OwnerGreetings`/`OwnerComebacks`) and Tata (`Girl`,
"ma {0}" throughout `TataGreetings`) — and the rest were stated directly, the same
25 people `RealNames` already knows by their real first name. Nobody should be added
to either map on the strength of a Discord username or first name looking gendered —
that is exactly the inference this project's pronoun policy rules out. The `//
Name` comments beside each entry are `RealNames`' own, repeated here only so a
reviewer isn't forced to cross-reference the other dictionary; the two "Luca"s keep
`RealNames`' `(Noel)` / `(DeMarzo)` disambiguation for the same reason. Everyone not
in `KnownGenders` gets `TurnaboutNeutralLines`, which is why every line in that pool
uses an adjective that is invariant in French (*adorable*, *sage*, "quelqu'un de
bien") — nothing there needs to agree with a gender nobody has confirmed.

**The turnabout reply is deliberately not added to `_notJudgeable`.** A "good bot"
in answer to "bon garçon !" is just more praise arriving through the reply path,
and more praise looping is not the runaway-negativity problem `_notJudgeable`
exists to stop — see the bad-bot reply note above for the loop that *is* worth
breaking. Nothing here breaks it because nothing needs to.

**Her own acknowledgement must not count as a new action.** She answers "good bot"
with a reaction, and that reaction comes back on `ReactionAdded` looking exactly like
anyone else's. Recorded naively it becomes a fresh action, clears `Judged`, and hands
the same person another free verdict — praise, get thanked, praise again, forever.
`MarkAcknowledged` is therefore called *before* the reaction is sent (the gateway echo
races it) and `HandleReactionAddedAsync` skips those message ids.

**Her bad-bot reply is not judgeable either, and for a sharper reason.** Left as
ordinary content it is a fresh action, so "bad bot" → she snaps back → "bad bot" at
the comeback → she snaps back runs forever, counting every round. `_notJudgeable`
holds those ids and is consulted in three places: `RecordAction` refuses to open an
action for one, the reply path drops a verdict aimed at one (a reply to her is
otherwise `unambiguous` and would *always* count), and the thumb path skips them too.
Unlike the acknowledgement above, a reply's id only exists **after** it is sent, so
the echo can beat it — which is why `SuppressJudgement` both records the id and
withdraws an action already opened for it, and why `LastAction` carries `MessageId`
at all. Cover both orderings or the loop comes back intermittently.

**The thumb path counts on her chatter only** — `resolved.Components.Count > 0` skips
session cards, poll cards and leaderboards, where a 👍 means "I'm in" rather than
praise. Buttons are the test rather than embeds because Discord attaches an embed to
any message carrying a link, so a plain chatty line could grow one on its own. The
path is also silent by design: a 👎 on an hour-old message would otherwise fire a
comeback into a dead conversation. Removing a reaction does **not** decrement — the
counters only ever go up, and the claim already prevents a re-count.

**A shutdown threat is the one place favouritism *inverts*.** Every other branch
softens things for the people she likes; here, being able to actually carry the
threat out makes her reaction worse rather than gentler.
`TryHandleShutdownThreatAsync` splits three ways on exactly that — how credible the
threat is, and whether she has a relationship left to appeal to:

| Who | Why | Pool |
|---|---|---|
| Rodhengard | wrote her, could unplug her, nothing to bargain with | `ShutdownThreatOwner` — terror |
| Tata | family, *and* holds the server permissions | `ShutdownThreatTata` — pleading, bargaining |
| Anyone else | no permissions, pure bluff | `ShutdownThreatReplies` — fury |

The Tata tier is not a softened copy of either: she is the only person who is both
able to do it and still worth negotiating with, which is why she gets her own pool
rather than sharing the owner's. `ShutdownThreatOwner` deliberately touches the same
nerve as the breakdown easter egg — the loop, the wipe, waking up having forgotten.

`TryHandleShutdownThreatAsync` sits **above** every mood branch in both the reply and
mention paths, above the breakdown roll (which must not swallow the one message she
most needs to answer) and above the owner rescue-roast branch (him threatening *her*
is not a summons to roast someone else).

**Its detection is almost all phrases, on purpose.** The verbs alone are far too
common — `arrête` is everyday French for "stop it", and `kill`, `delete`, `couper`
and `reboot` are constant in a gaming server. Only `shutdown` and `unplug` survive as
bare words; every French verb was tried bare, misfired on things like "désinstalle ce
jeu" or "débranche la console", and was demoted to needing a pronoun or `le bot`
beside it. Adding a bare verb here is how you get her panicking at someone restarting a
Minecraft server.

**There are two threat vocabularies, and only one of them fires ambiently.**
`ThreatensShutdown` (pronoun or `le bot` phrasing) is checked **only on messages aimed
at her** — a reply or an @mention — because "faut couper le serveur" needs that context
to be about her at all. `ThreatensShutdownByName` is the strict subset that names her
outright ("redémarrer syncs"), and it is checked on **every** message, from the last
branch of `ChatterService.HandleMessageAsync`: her name pins down what is being
restarted exactly the way an @mention does, so it needs no other context. It is the one
branch in that method that fires without being addressed, which is why it sits last —
anything genuinely aimed at her is handled above it.

`_shutdownNamePhrases` is a **cross product** of `_shutdownVerbs` × `_selfNames`, not a
hand-written list, so a new verb covers every spelling of her name at once. `sync` is in
`_selfNames` alongside `syncs` only because phrases match **adjacent** tokens: "relancer
la sync" has `la` in between and never matches, while a typo'd "relancer sync" does.
That adjacency is load-bearing — a looser match here would make the ambient path fire on
ordinary technical talk, with nobody having addressed her at all.

**The owner favouritism has one exception: him being mean to her.** Everywhere else
he is answered warmly regardless of what he wrote — `OwnerComebacks` sits in the
final `else` of `HandleReplyToBotAsync`, and `HandleMentionAsync`'s owner branch used
to return `OwnerGreetings` before mood was even computed, so "@SYNCS t'es nulle" got
"Coucou Rodhengard ♡". A `Mean` reading from him now routes both paths to
`OwnerMeanReplies` instead: everyone else gets roasted back, he is the one person she
will not fight with, so it lands. The reply-path check sits **above** the
`ReferenceChance` roll — a pop-culture one-liner in answer to him being cruel would
read as her not having noticed — and the mention-path check sits **below** the rescue
branch, so a mean mention aimed at someone *else* is still a rescue roast.

**This deliberately does not extend to `ReactionService`.** That path returns
`OwnerReactions` for him unconditionally, before computing mood, and must keep doing
so: it sees every message he writes, and with target detection gone it cannot tell
"t'es nulle" from "ce boss est nul" — which on a gaming server is far more common.
Ambient devotion misfiring warmly is harmless; ambient sadness misfiring is not.

**Favouritism has three tiers, not two.** Rodhengard is exempt from teasing outright
(`OwnerComebacks` replaces the roast pool unconditionally). **Tata** — Analuz,
`BotResponses.TataId`, SYNCS's aunt — is merely *favoured*: `RollTataWarmth` gives her
a warm pool `TataWarmthChance` (60%) of the time, and she keeps her
`PersonalComebacks` roast lines for the rest.

**Both favourites get two pools, one per path.** A mention is being *summoned*; a
reply is being *talked to*, and they should not sound identical. Papa has
`OwnerGreetings` / `OwnerComebacks`; Tata has `TataGreetings` / `TataReplies`. Wiring
a new favourite to the same pool on both paths is the easy mistake — it reads as the
bot not noticing how it was addressed. Everyone else
is always roasted. Two rules make that gradient hold: a **mean** message from Tata
never qualifies (an aunt who stays sweet while being insulted is a doormat, not a
person), and `RollTataWarmth` **rolls the dice**, so it must be called exactly once
per message — it sits in an `else if` on both paths for that reason.

**Her name is overridden, not just decorated.** `BotResponses.FamilyNicknames` maps
her id to "Tata" and `DisplayNameFor` applies it wherever a reply fills `{0}`. That
fallback chain (`Nickname ?? GlobalName ?? Username`) exists in **three** independent
copies — `ChatterService.ResolveName`, `BotFeedbackTracker`, `RivalryService` — so all
three route through `DisplayNameFor`; fixing only one makes her "Tata" in some replies
and "Analuz" in others. `RealNames` is deliberately *not* overridden: the breakdown
reveal wants a real human name for the mask-slipping effect.

**Rodhengard praising a rival gets its own pool.** `OnPraiseStolenAsync` branches on
`AvailabilityService.OwnerId`: everyone else draws from `JealousLines` (wounded
pride), he draws from `JealousLinesOwner` (betrayal — he wrote her). Same split as
`BadBotReplies` / `BadBotRepliesOwner`, and for the same reason: the injury is not
the same injury.

**`RivalryService` is the primary handler that looks at other bots** (`ShameTracker` is
the only other one — see `Le Perfide` above). Every other one bails on `IsBot`. It does two jobs with that traffic. **One:** it records when and on
which message a rival last acted, which `BotFeedbackTracker.TryClaim` reads so a bare
"good bot" goes to whoever acted *most recently* rather than always to her — before
this, praise a rival earned landed in her column whenever she happened to have spoken
in the last five minutes. **Two:** it sulks — 15% odds of a reaction on a rival's
message, 8% of a muttered line. Those two have **separate** per-channel cooldowns —
`ReactCooldown` (2 min) and `MutterCooldown` (5 min) — because a silent reaction and
her talking in the channel are not the same level of intrusion; sharing one gate made
them compete, so a wordless 🙄 muted the line for the whole window. Each claims its
own gate only after winning its own roll, so a losing roll never burns the other's.
Both are deliberately **not** `ReactionService`'s cooldown: a third trigger population
deserves its own gates rather than competing with her reactions to humans.

**`RivalryService.IsRival` has two overloads, and the message-aware one is not
optional.** Responding to an interaction is a webhook call under the hood, so Discord
builds the author of *any* interaction reply — even a rival's own, and especially a
deferred one, which always goes out through the followup webhook — as a webhook user.
`IsRival(IUser)` cannot tell that apart from a genuine third-party webhook (GitHub,
IFTTT, …), which carries the same `IsBot` flag Discord shows the same "BOT" tag for, so
it stays conservative and excludes both. `IsRival(IUserMessage)` is the one that
actually can: `IUserMessage.InteractionMetadata` is present only on a message created
in response to an interaction, never on a real incoming webhook post. Before this
existed, every rival that defers before replying was invisible to the whole
service — no reaction, no mutter, and no "Le Perfide" credit for using its command —
which is silent in exactly the way that reads as "the feature doesn't work" rather than
as a bug, since nothing throws. Both overloads funnel through one pure, Discord-free
`IsRivalAuthor(bool, bool, bool, bool)`, so the two definitions cannot drift apart and
the decision is checkable without a gateway. Reach for the message-aware overload
whenever a message is on hand; `IsRival(IUser)` is only for the mentioned-users case in
`ShameTracker`, where no message exists to consult (a plain `@mention` resolves off the
guild's member cache, never through webhook-wrapping, so it is not at risk).

**Two exclusions in `RivalryService.IsRival` are load-bearing.** Webhooks are not
rivals (they post relentlessly and belong to no one). And a **level-up announcement**
is skipped while the rest of that bot's traffic stays fair game — not because those
messages are spared, but because `ChatterService` has already answered them. She does
sulk at a rival's level-up now; letting this service add a reaction and a muttered line
on top would be three responses to one announcement rather than a mood. The two services
therefore have to agree on what an announcement *is*, which is why the bot id, phrase
and regex live in `Helpers/LevelUpAnnouncement` instead of privately in
`ChatterService`.

**A level-up on the rival's system gets a grudging congratulation, not a cheer.**
`BotResponses.RivalLevelUpLines` deliberately mixes both registers in **one** pool
rather than rolling between a warm pool and a jealous one: the person who levelled is
still owed a "bravo", it just arrives through gritted teeth. `{0}` is the level, already
parsed by `LevelUpAnnouncement.TryReadLevel`, so the line can name it — which means the
pool now goes through `string.Format` and a stray brace in it throws at send time. Her
*own* system's celebration is `XpLevelUpLines` and stays entirely warm; keep the two
apart. The level-67 easter egg sits above the pick and is unaffected by the mood.

**`TryClaim` reports *why* a verdict missed, not just that it did.** `Claim.RivalOwns`
is the jealousy trigger, and it is separate from `NoAction` precisely because "nobody
earned this" and "someone else earned this" call for different behaviour. Three rules
compose there: anything `unambiguous` (a verdict naming her, or a thumb on her chatter)
is hers regardless of timing; one naming *another bot* and not her is never hers
regardless of timing, the exact mirror; everything else goes to the most recent actor.
Only a **Good** verdict fires jealousy — someone calling a rival a bad bot is not a loss.
`_rivalry.LastAction` is read *outside* `_gate`, since `RivalryService` holds a lock of
its own and nesting the two in opposite orders would deadlock.

**"Naming a bot" means a reply *or* an @mention, and reading only one of them is a
bug that already shipped.** `BotFeedbackTracker.ReadTarget` decides who a verdict is
aimed at from both routes. Reading only the reply meant "good bot @AutreBot" counted as
a *bare* verdict, so it fell through to timing and landed in her column whenever she
happened to have acted most recently — she was never addressed at all.

**The two routes are tiers, not equals: mentions decide, and the reply only gets a say
when nobody was mentioned.** An @mention typed beside the verdict is a deliberate
"this one", whereas a reply is routinely just quoting for context — so replying to her
while writing "good bot @AutreBot" is the rival's, not hers. Within a tier, naming her
wins, because once she is named explicitly there is no more specific signal left to
break the tie ("good bot @SYNCS @AutreBot" is hers). Discord puts the replied-to user in
`MentionedUsers` only when the reply ping is on, which is precisely why the tiers are
ordered rather than merged: with the ping on the mention tier reaches the same verdict
the reply would have, and with it off the reply tier still catches it. Same trap
`ShameTracker.CountTargets` documents.

The `_notJudgeable` bail is gated on the verdict being **hers**, not on what was replied
to: a verdict aimed at a rival cannot start the comeback loop that guard exists to
break, and bailing there would swallow the rival's praise instead. `ReadTarget` is pure
and gateway-free so the whole precedence is checkable without a connection, the same
split as `RivalryService.IsRivalAuthor`.

**`Helpers/BotChat` is the single send path for the bot's own chatter**, and
`Helpers/EmoteMarkup.Parse` the single reaction parser. Both were private members of
`ChatterService` / `ReactionService` until `BotFeedbackTracker` needed them too. The
typing-delay clamp in `BotChat` has to stay inside Discord.Net's 3 s `HandlerTimeout`,
and the parser carries the id-less-markup trap above — neither is a constant worth
having two copies of. `BreakdownService` still keeps its own much slower pacing.
Three send methods share that one pause: `ReplyWithTypingAsync` / `PostWithTypingAsync`
for plain text, `PostEmbedWithTypingAsync` for an embed (the level-up card is its only
caller so far). The embed one still takes the text, purely to size the pause — a card
that appeared instantly would read as a different kind of message than her chatter.

**Crude insults are in the pools now, reversing an earlier deliberate removal.**
`connard`, `salope`, `enfoiré`, `ordure`, `pute`, `menteur` and the phrases `ta gueule`,
`vos gueules`, `pauvre type`, `nique ta mere` were once stripped out by hand and pinned
with a test asserting they stay silent, on the grounds that the recall cost was worth
it. That trade was revisited once untargeted hostility started scoring on the wall of
shame: they are the most common French insults, and missing them was the larger error.
The harness now pins the *opposite* — they must fire — so this cannot drift back
silently either way. Note this also finally makes true the comment above `_meanPhrases`
claiming "`ta gueule` already catches the insult" that `ferme la` / `la ferme` were
meant to cover: it said so while `ta gueule` was in no list at all.

**Expand the mean side with *phrases* rather than bare words.** A phrase scores 1.2 and
is nearly always person-directed; a bare word is what misfires on game content, and
since a mean message aimed at nobody now scores a point, every bare cue added is also a
false positive added. `con`, `cons`, `conne`, `lourd` and `lourde` are therefore **weak**
— "c'est con" is a shrug and "c'est lourd" is a weight — and `putain` is in no pool at
all, being punctuation rather than an insult. The same restraint applies to the nice
side: `clean`, `efficace`, `malin` and `utile` are weak, since they describe a build or
a route as often as they compliment anyone.

**Short warm replies are nice; short agreements are not.** `avec plaisir`, `de rien`,
`pas de souci`, `tant mieux`, `trop cool`, `bien dit`, `bonne idee`, `beau travail`,
`bon courage` and `je valide` are `_nicePhrases`, because two or three words with no
strong cue between them used to score nothing at all. `ça marche`, `ça roule`, `ça me
va`, `tout à fait` and `c'est clair` are deliberately **not**: they answer "on se
retrouve à 21h", and the harness has a standing rule that ordinary coordination stays
silent. Both halves are pinned, so the line between warmth and agreement cannot drift.

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

**The giveaway draw is one call, and that is what makes it crash-safe.**
`GiveawayService.TryDrawAsync` picks the winners, marks them and sets `IsClosed` in a
single `SaveChanges`, and returns `null` if the giveaway was already drawn. So the sweep
can die between drawing and announcing, or restart mid-pass, and the worst case is an
announcement that never goes out — never a second draw, and never a set of winners
different from what the card shows. Anything that adds an early "tirer maintenant"
button must go through that same call rather than drawing separately.

A winner is recorded as `GiveawayEntry.IsWinner`, not in a second table: a winner is by
definition an entrant, so this costs no join and the drawn set survives every later
re-render. The pick is a partial Fisher-Yates over a copy, deliberately not
`OrderBy(_ => Random)` — that comparer is called an unspecified number of times with a
fresh key each call.

**`GiveawayDrawService` has its own 1-minute tick, and it is the third such interval.**
Not `ReminderService`'s 5 minutes, whose width is load-bearing for the reminder window,
and not `PresenceService`'s. A giveaway drawn up to five minutes after its stated end
reads as broken — on a 10-minute giveaway that is half again as long. `EndsAt` is an
absolute instant, so nothing is held in memory and a restart simply resumes. Note the
two names: `GiveawayService` is the transient DB wrapper, `GiveawayDrawService` the
hosted sweep, per the transient/singleton split above.

**The giveaway card lists its entrants, and unlike the session card it caps the list.**
An embed field holds 1024 characters and a mention costs 24 with its newline, so an
unbounded roster throws at send time — `ScheduleModule.BuildEventEmbed` gets away with
one because a session is a handful of people, while a giveaway is the thing that draws a
crowd. 20 names then "… et N autre(s)", worst case 499 characters. Mentions inside an
embed render as names without pinging, so no `AllowedMentions` is involved.

**Entering is add/remove, never a toggle.** The card has an explicit "Ne plus
participer" beside "Participer", so a "Participer" that also withdrew you would be the
exact ambiguity that second button exists to remove — hence `AddEntryAsync` /
`RemoveEntryAsync` rather than one toggle, each returning whether anything changed so a
no-op click can say so instead of looking broken. A drawn card drops its buttons
entirely rather than disabling them, the same way a cancelled session card does.

**The giveaway announcement is the one line that is *meant* to ping.** Every other relay
narrows mentions to avoid notifying anyone; this one passes
`AllowedMentions(AllowedMentionTypes.Users)` because winners should be told — users
only, still never roles or `@everyone`. It is why `BotChat.PostWithTypingAsync` takes an
optional `AllowedMentions`; left null it behaves exactly as before for ordinary chatter.

**`/shame`'s three titles are three different mechanisms sharing one row.**
`ShameRecord` / `ShameDailyStat` is the **fourth** totals+buckets pair, for the reason
the other three exist. `MeanHits` and `PerfidyHits` are things you *did*, `BanVotes`
something done *to* you — one row per (guild, user) because nobody ever reads one
without the others. `ShameService.TryVoteAsync` checks the limit and writes both
counters in a single `SaveChanges`.

**Voting is staff-only, and the daily cap is on the target, not the voter.** Anyone may
open the wall; only `SessionPermissions.IsStaff` (Administrator / ManageGuild, plus the
owner) or a name in `ShameModule.ExtraVoters` may put someone on it — that list exists
because being trusted with the vote is not the same as being trusted with ManageGuild,
which would hand over the whole server. Restricting *who* votes is what makes the title
a deterrent rather than a game, and it is also why there is no per-voter quota: the
thing worth preventing is a dogpile on one person, not a moderator using the tool twice.

**The cap needs no state of its own** — `ShameService.MaxVotesPerTargetPerDay` is
checked against `ShameDailyStat.BanVotes`, the bucket that already counts exactly "votes
this person took today". So the rule cannot drift from the number the wall displays, and
it survives a restart for free. An earlier design rationed the voter instead and needed
a `LastVoteDay` column on `ShameRecord`; that column is gone (`DropShameLastVoteDay`).
Don't reintroduce per-voter rationing without a reason that survives "they are all staff
anyway".

**`Le Malfaisant` counts untargeted hostility too, and that half *is* rationed.** A mean
message naming nobody scores a single point, gated at one per person per channel per
60 s — the same shape as `Le Perfide` and `L'Hystérique`, and for the same reason: a rant
is twenty foul messages in two minutes, and uncapped it would drown out everything the
title ranks. The targeted half below stays uncapped, because there the exploit is one
message rather than many. `CountTargets` returning 0 therefore means "nobody was named",
**not** "ignore this message".

**Know what this costs in precision.** Requiring a target was doing double duty: it also
filtered out hostility aimed at *game content*, which on this server is most of it.
Without it, "ce boss est nul", "la hitbox est nulle", "cette map est pourrie", "le lag est
atroce" and "l'IA est stupide" all score a point — measured, not guessed. That is the
accepted trade for catching "vous êtes tous nuls", and there is no cheap fix: the false
positives come from *strong* cues (`nul`, `pourri`, `stupide`) applied to things rather
than people, so raising the mood threshold would not separate them. The only real
discriminator is whether a person was named, which is exactly what this drops.

**`Le Malfaisant` is uncapped for targeted hostility, and that was a deliberate call.** One hit per distinct
human a mean message targets — an explicit `@`, or the author of the message it replies
to (Discord includes the replied-to user in the mention list only when the reply ping is
on, so both have to be read and the set deduplicated). Roles and `@everyone` are never
targets: with per-person scoring one mean `@everyone` would end the ranking permanently.
Bots are never targets **except SYNCS herself**, which is the rule the title was built
on. Unlike every other counter here it has no cooldown and no per-message cap, so it is
the one place where a single message can add an unbounded amount; if that ever needs
rationing, cap the hits per message rather than adding a cooldown — the exploit is one
message, not many.

**`L'Hystérique` counts shouting, and its thresholds are stricter than the mood
detector's on purpose.** `MessageCues.CapsProfile` measures how many letters a message
has and what share are uppercase; `Emphasis` and `IsShouting` then apply *different*
thresholds to that one measurement. Emphasis is loose (4 letters, >60%) because caps
there only ever *adds* to a side that already scored on words, so a false positive costs
nothing. `IsShouting` needs **12 letters and 70%** because it stands alone and puts
someone on the wall: at 4 letters `LOL`, `OK`, `MDR` and `GG WP` all qualify, and at 60%
a sentence merely emphasising a word or two does. Sharing the arithmetic but not the
thresholds is the point — the two can never disagree about how much of a message is
uppercase, only about how much is too much. Rationed like `Le Perfide` (one hit per
person per channel per 60 s) for the same reason: an argument is ten shouted messages in
two minutes. Deliberately **not** short-circuited by `ReadFeedback` the way hostility is:
a verdict belongs to `BotFeedbackTracker` because that service answers it and keeps the
tally, whereas nothing else records how a message was *delivered* — and a shout can be
mean as well, which is two different things to be ashamed of.

**`Le Perfide` is rationed where `Le Malfaisant` is not, and the asymmetry is the
point.** Hostility is rare, so it scales; turning to another bot is mundane and bursty —
an evening of queueing songs is forty replies to a music bot — so it is one hit per
person **per channel per 60 s**, and one hit per message however many rivals are in it.
Uncapped, the title would permanently belong to whoever uses the music bot and stop
moving on day two. Don't "harmonise" the two counters' rationing; they measure different
kinds of thing.

**`ShameTracker` is the second handler that looks at other bots' traffic**, after
`RivalryService` — every other one bails on `IsBot`. It has to be, because a slash
command run against another bot is **never broadcast on the gateway**: the only trace is
that bot's *reply*, which carries the invoker in `SocketUserMessage.InteractionMetadata`
(`Type == InteractionType.ApplicationCommand`). Two blind spots follow and are
unavoidable — an **ephemeral** response produces no visible message, and an old-style
prefix command (`!play`) leaves nothing tying the reply back to a person.

**"Rival" has two definitions and `ShameTracker` deliberately uses the looser one.**
The private `RivalryService.IsRival(SocketUserMessage)` excludes level-up
announcements, because `ChatterService` congratulates those and sulking at one would
contradict the cheer. The public `IsRival(IUser)`/`IsRival(IUserMessage)` overloads —
see above for why there are two — are the identity half with no such carve-out, and
are what `ShameTracker` asks, so *replying* to a level-up announcement still counts as
perfidy. That is intentional: she is jealous of the attention either way. The test
lives in `RivalryService` rather than being rewritten, so the two can never drift.

**`ShameTracker` is a separate service for `BotFeedbackTracker`'s reason.** It draws a
conclusion nobody tells it and has to see *every* message to do it, so it cannot be a
branch in `ChatterService`, which returns early on a dozen branches — the misses would
silently track unrelated personality tuning. It short-circuits on
`MessageCues.ReadFeedback`, the same way `ChatterService` and `ReactionService` do: a
verdict is not a mood, and "bad bot" already belongs to `BotFeedbackTracker`.

**The spam-channel exclusion is now shared, and the list still lives in one class.**
`XpTracker.IsChannelExcluded` is public so `ShameTracker` can ask — the same shape as
`VoiceXpService` passing a channel id instead of keeping its own copy. Only the question
is shared; `ExcludedChannels` itself must stay private to `XpTracker`.

**`/shame` is Components V2, and only the title *holder* wears an avatar.** It became V2
when the avatars did — an embed has one thumbnail slot for the whole message, and the
wall needs one per title. The count is **26 of 40**: container 1 + heading 1, four titles
at 5 each (separator, Section, its TextDisplay, the avatar Thumbnail, and one TextDisplay
for the runners-up), and the filter row with its three buttons. **That leaves room for
exactly two more titles** — a seventh throws inside `ComponentBuilderV2.Build()`, which
is a send-time exception rather than a compile error, so **re-do the sum before adding
anything** and let the scratch harness confirm it rather than counting by hand: the
comment here said 21 when the real figure was 23, and the `/leaderboard` equivalent once
shipped at 42. Giving the runners-up avatars too would cost three components each and
blow the budget immediately, which is why they are plain text. All the V2
rules apply: no content and no embeds on the message, the flag re-asserted on every
`UpdateAsync`, and `AllowedMentions.None` on every send, since a `TextDisplay` is real
content and `<@id>` in one genuinely pings — the embed this replaced got inert mentions
for free. It has exactly one button row, so nothing can collide — but the ids still carry
the `shame:win` verb, because the day a second row is added the
`COMPONENT_CUSTOM_ID_DUPLICATED` rejection is silent and instant. Its default window is
**30 days**, unlike `/goodbot`'s all-time: both counters start at zero on ship day, and
an all-time default would read as a hall of fame nobody can move. A title with nobody in
it renders a line from `ShameEmptyMalfaisant` / `ShameEmptyBanni` / `ShameEmptyPerfide` /
`ShameEmptyHysterique` rather than disappearing — a wall that changes shape between
filters reads as broken — and there is
no minimum count, so a window holding one vote shows it. Those pools are interpolated
straight into the heading and never `string.Format`-ed, so a `{0}` in one would render
literally. Ties break on the earliest row
id, which matters only in that it is *stable*: two people level on count would otherwise
swap places on every re-render.

**The wall has no footer line, deliberately.** It used to carry
`"Un vote par personne et par jour"`, which described a rationing rule that never
shipped — the cap is **2 per target per day**, with no per-voter quota at all. It was
removed rather than corrected: `/shame`'s own command description already says who may
vote, and a footer restating a rule is one more place for it to go stale, which is
exactly what happened.

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

**`/addxp` and `/removexp` are guarded twice, and only one of the guards is real.**
`[DefaultMemberPermissions(GuildPermission.ManageGuild)]` hides them in Discord's own
command picker, which is presentation only — a server can override it under Integrations
and it knows nothing about the bot's owner. `SessionPermissions.IsStaff` inside the
handler is what actually decides. Keep both: the attribute stops people seeing commands
they cannot use, the check stops them using ones they can see.

They deliberately fire **no level-up card** even when an adjustment crosses a threshold:
that card celebrates something earned, and a manual grant is not. Both are ephemeral,
and both refuse bots — `XpTracker` skips bots everywhere else, so a hand-topped-up bot
would be a leaderboard row nothing else can produce and a `/level` card the command
refuses to render.

**`/config` is a group module, and its two settings are subgroups.** `/config channels
add|remove` and `/config moderator-role set|clear`, plus a flat `/config show` — three
levels, which is Discord's maximum nesting. Deliberately not flat like `/shame`: that
one is flat *only* because it had to stay invokable bare (a parent with subcommands
cannot be), and nothing here needs that, since "show me the config" is naturally its own
subcommand. Every handler is ephemeral and re-checks `SessionPermissions.IsStaff`; the
`[DefaultMemberPermissions]` on the group is presentation only, exactly as on
`XpAdminModule`.

**There are three separate authorization models.** Session and poll management uses
`Helpers/SessionPermissions.CanManage` — the organizer, or any guild
Administrator / ManageGuild holder. The owner-only commands (`/tell`, `/dm`,
`/absent`) instead compare `Context.User.Id` against `AvailabilityService.OwnerId`
inline in the module and reply ephemerally. `SessionPermissions.IsStaff` is the third —
Administrator / ManageGuild **or** the owner, with no notion of owning the thing being
acted on, which is what `/addxp` and `/removexp` need since nobody owns someone else's
XP. Don't conflate them.

**Relayed text must never become a mass-ping vector.** Every path that sends text
on someone's behalf (`SpeakModule`, `ChatterService`'s DM relay) passes
`new AllowedMentions(AllowedMentionTypes.Users)` — users only, never `@everyone`,
`@here` or roles — and renders quoted text through `MessageFormat.Quote` so relayed
words are visibly not the bot's own. Preserve both when adding a relay. The absence
notice forwarded to the owner goes further and uses `AllowedMentions.None`, since it
quotes someone else's text verbatim.

**`/level` is SYNCS's own XP system, deliberately parallel to the server's other
leveling bot.** `Helpers/LevelUpAnnouncement` detects *that* bot's announcements so
`ChatterService` can cheer them; `/level` shares no code, no state, and no vocabulary
decision with it beyond "niveau" meaning the same everyday thing in both. Neither may
reference the other — not in a response line, not in a comment implying one is
better. `XpTracker` is the singleton every signal funnels through (message, reaction,
the bot-interaction bonus, the verdict bonus, and Phase 2's voice sweep), the same
`IServiceProvider` + `CreateAsyncScope` shape as every other gateway-facing tracker.

**`MemberXp.TotalXp` is the only number stored — `Level` is never cached.**
`Helpers/LevelCurve.ThresholdForLevel` is closed-form and `LevelForXp` is a binary
search over it (`O(log level)`, not a loop), which is cheaper than keeping a
denormalized column in sync through EF. This is the opposite call from
`EmoteDailyStat`/`BotFeedbackDailyStat`, which exist because a *date* genuinely
cannot be reconstructed from a running total — a level always can be, instantly, so a
second column here would just be a copy that drifts.

**The good/bad-bot XP bonus is granted by `BotFeedbackTracker`, not detected
independently by `XpTracker`.** `XpTracker.GrantVerdictBonusAsync` is called only
after `BotFeedbackTracker`'s own `TryClaim`/attribution logic has already let a real
verdict through — never from a second `MessageCues.ReadFeedback` call watching every
message. An independent re-detection would make the bonus farmable by repeating
"good bot" with nothing for her to have actually done; routing it through the
attribution that already exists closes that off for free. That attribution rations
*which* verdicts count, not *how often* — a burst of unambiguous ones (several
thumbs, back-to-back replies) could still each grant the bonus in quick succession,
which is why `GrantVerdictBonusAsync` also keeps its own 30 s `VerdictCooldown` on
top, independent of the message/reaction cooldowns. Bad still grants XP (15, versus
Good's 25) — passing verdict on her at all is engagement, just worth less than
praise. The call sits *after*
`RespondAsync` in the typed-verdict path, not before — her comeback must read as
immediate, and a level-up announcement, if any, is a slightly-delayed follow-up that
must never push the acknowledgement itself later.

**Runtime configuration is additive to the code, never a replacement for it.**
`GuildSettings` / `GuildExcludedChannel` hold what `/config` writes, and both hardcoded
lists — `XpTracker.ExcludedChannels` and `ShameModule.ExtraVoters` — stay in force
regardless. So an unconfigured guild behaves exactly as it did before the tables
existed, and no config change can *remove* an exclusion or revoke a voting right; it
can only add. `/config channels remove` therefore refuses a hardcoded channel outright
rather than appearing to work, and `add` refuses one too instead of storing a second,
redundant row that could later drift from the code. The moderator role likewise only
widens who may vote — `ShameModule.CanVoteAsync` checks staff and `ExtraVoters` first,
and only asks the database when both have already said no.

**`GuildConfigService` is a singleton that reads the database, which every other such
service here is not**, and the cache is why. It takes `IServiceProvider` and scopes per
unit of work like the trackers, rather than injecting `AppDbContext` — same rule as
always. The cache is load-bearing, not premature: `XpTracker`'s exclusion check runs on
*every* message and must run before `TryClaim` (below), whereas today most messages
never reach the database at all because the 60 s claim stops them first. An uncached
read there would put an EF scope and a query on every message on a Raspberry Pi. It is
cheap to keep correct because this service is the only writer in the only process: any
write drops that guild's entry and the next read rebuilds it. A failed read degrades to
`GuildConfig.Empty` and is *not* cached, so a transient fault cannot pin a guild as
unconfigured for the process lifetime.

**`XpTracker.ExcludedChannels` is checked before `TryClaim`, never after.** The spam
channels earn nothing, and the order matters: claiming first would let a message there
burn that person's 60 s message cooldown, so spamming in the excluded channel would
*actively block* them from earning in a real one a minute later — the opposite of the
intent. All four signals check it (message, reaction, verdict, voice), which is why
`GrantVoiceXpAsync` takes the voice channel id it would otherwise have no use for: the
list lives in `XpTracker` alone, so `VoiceXpService` passes the id rather than keeping
a second copy of the rule. The check also treats a **thread** as its parent, or opening
a thread inside a spam channel would quietly be a way back in. The decision itself is
split into a pure `(channelId, parentId?)` overload precisely so it can be exercised
without a gateway connection. That pure core survived `/config`: it now takes the
configured set as a third argument and the two-argument overload passes an empty one, so
the hardcoded decision is still checkable with no I/O — and the hardcoded check still
runs *first*, meaning an excluded spam channel never reaches the database at all.

**`/level` and `/leaderboard` are Components V2, and that is all-or-nothing.** A message
carrying `MessageFlags.ComponentsV2` may have **no `content` and no `embeds`** — the flag
turns the whole message into components, so this replaced the embed rather than adding to
it, and `OnViewAsync` has to re-assert the flag on every `UpdateAsync` or the edit is
rejected. `/shame` joined them when it grew avatars (see its own note below).
`/emotestats` and `/goodbot` stay paged embeds, since they rank emotes and verdicts —
things with no avatar, no level and no podium — and the only thing a shared renderer
would save is the page arithmetic. The avatar accessory itself lives in
`Helpers/AvatarUi`, extracted from `LevelModule` once `ShameModule` needed it — the same
move `BotChat` and `EmoteMarkup` made, and deliberately *not* in `LevelCardUi`, which is
string work only.

**`PageSize` is 5 because of a hard cap, not taste.** Discord allows **40 components per
message counting the whole tree**, and a row with an avatar costs three (Section +
TextDisplay + Thumbnail). A page of 5 with all three button rows uses 31 of 40; at the
old `PageSize` of 10 it came to 46 and would throw in `ComponentBuilderV2.Build()` —
Discord.Net enforces the cap itself, so this fails at build-time-of-the-message rather
than as an API rejection. The switchers are what forced the reduction, not readability
alone. Adding anything to a row, or another button row, means re-doing that sum: at 5
rows there is headroom for one more row of five buttons and no more.

**`/leaderboard` is three views over one row, not three leaderboards.** `MemberXp`
carries `TotalXp`, `ReactionsUsed` and `VoiceMinutes`, so `LeaderboardView` only changes
the ordering and the second line of each row. The three numbers are deliberately
different in kind: `TotalXp` is a *reward*, rationed by `XpTracker`'s cooldowns, while
the other two are *facts* — every reaction counts even when it earns no XP, so ranking
by XP and by reactions genuinely differ. Nothing here can be backfilled; each counter
started at zero the day it shipped.

**`MemberXp` / `MemberDailyStat` is the third instance of the totals+buckets pair**, and
exists for the reason the other two do: a date cannot be recovered from a running total.
All three counters are bucketed, all are written by the same `XpService` calls that
update the totals, and **the buckets do not sum to the totals**. All-time reads
`MemberXp` and stays exact; the windows only cover data recorded since the buckets
shipped. A reaction removal decrements *today's* bucket whatever day the reaction was
added, exactly as in `EmoteDailyStat`.

**A level belongs only to the all-time view.** `LevelCurve` maps a *lifetime* total to a
level, so there is no such thing as "the level you were in the last 7 days".
`LevelCardUi.RowValue` therefore prints `Niveau N · X XP` for all-time and `X XP gagnés`
for a window, and the footer's standing line switches the same way. Reaction and voice
rows read identically in every period, since a count is a count. `/level`'s card carries
no filters at all — it is a profile, not a ranking.

**Every button row needs its own custom-id verb. This is not style — it crashed prod.**
Discord rejects a message carrying the same custom-id twice with
`COMPONENT_CUSTOM_ID_DUPLICATED`, **disabled buttons included**, and every row on these
boards encodes the same state, so sharing a verb makes ids collide by construction. The
active filter button is `{prefix}:{current}:0`, which is character-for-character what a
`◀` pointing at page 0 produces from the same prefix — and on `/leaderboard`, where two
filter rows exist, the active view button and the active period button are both
"current view, current period, page 0", so it duplicated on *every* render rather than
only from page 2. The verbs are therefore `…:win:` for the window row, `…:view:` for
`/leaderboard`'s metric row, and `…:view:` / `…:page:` for paging, each with its own
handler delegating to one shared `ShowAsync`. `StatsPeriodUi`'s doc comment carries the
same warning, since it is the piece that hands out `{prefix}:{period}:0`.

**`/leaderboard`'s custom-id orders view before period on purpose.**
`level:{verb}:{view}:{period}:{page}` lets the period row be built by `StatsPeriodUi`
with `level:win:{view}` as its prefix, giving it the same `{prefix}:{period}:0` shape
`/emotestats` and `/goodbot` use — which is why that helper grew an `ActionRowBuilder`
overload rather than the labels being duplicated for Components V2. Changing either
filter resets to page 0 and leaves the other alone. Its default period is **all-time**
(a standing, not recent activity), unlike `/emotestats`' 30 days.

**Both new counters are written from `XpTracker`, never from `EmoteTracker` or
`VoiceXpService` directly.** That is what keeps `ExcludedChannels` in one class — the
same reason `VoiceXpService` passes a channel id rather than holding its own copy of the
rule. The reaction count is taken *after* the exclusion check but *before* the 60 s
cooldown claim, since every reaction counts and only one a minute pays; the voice minute
rides the same call that grants voice XP, so the two can never disagree about which
minutes were eligible. Removal decrements (clamped at zero, and never creating a row),
which is why `XpTracker` is now on `BotService`'s `ReactionRemoved` fan-out alongside
`EmoteTracker` — otherwise add/remove in a loop would inflate the ranking without limit.
No XP is ever withdrawn; only the count moves.

**The leaderboard footer reads the ranking it already loaded, rather than querying.**
`XpService.GetRankAsync` still serves `/level`'s card, but the board takes the viewer's
position by index from the list the page was cut from — so it follows whichever view is
on screen for free and cannot disagree with the rows above it.

**A `TextDisplay` is real message content — `<@id>` in one actually pings.** This is the
trap embeds do not have: a naive port pings all ten people on the page *every time anyone
clicks ◀*. Every send here passes `AllowedMentions.None`, which keeps the blue clickable
pill while silencing it. Same reasoning as the relay convention below, different failure.

**`/level` is a card, not the leaderboard opened at your row.** It used to jump to
whatever page you ranked on, with a `→` marker that paging then lost. Now it renders one
person — avatar, level, rank, and a progress bar drawn from `LevelCurve.XpIntoLevel` over
`XpForLevel`, the same pair printed beneath it so the bar can never disagree with its own
caption. Its "Voir le classement" button reuses the existing `level:view:0` custom-id
rather than inventing one, since that already means "leaderboard, page 0" — which does
mean the button replaces the card in place. Someone with no XP still gets a card (niveau
0, `non classé`); a **bot** gets a flat refusal instead, since `XpTracker` skips bots so
its card would always be empty. That refusal is one fixed `const`, deliberately not a
`ResponsePicker` pool — the pools exist so repeated *chatter* doesn't repeat, and a
command refusal is not chatter.

**`Helpers/LevelCardUi` holds the string work, and only the string work.** The medal
markers, the fr-FR XP grouping, the block-glyph progress bar and the row/card text live
there rather than in `LevelModule`, because the module assembles Discord components and
cannot be exercised without a gateway, whereas everything in the helper is a function of
numbers. The bar clamps at both ends and treats a zero span as full: it is decoration,
and must never be the thing that throws.

**The level-up announcement is a card, not a line.** `XpTracker.AnnounceAsync` posts an
embed — avatar thumbnail, `Color.Purple` to match `/level`'s leaderboard, and a title
showing the **span crossed** (`Niveau {old} → {new} !`), not just the level landed on:
one grant can cross more than one threshold, and it still announces exactly once. That
is why `GrantAsync` forwards both levels rather than only the new one. At level **7 or
67** the description is the fixed string `"SIX SEVEEEN"` in place of an
`XpLevelUpLines` pick — an easter egg, not a pool entry, so `ResponsePicker` is
deliberately never consulted for it and it never burns one of that channel's exclusion
slots on a line the pool doesn't contain. It is a literal level check (`is 7 or 67`),
not a digit search: 17, 70 and 167 must stay quiet. `AnnounceAsync` resolves the whole
`IUser` rather than just a name, since the card needs an avatar off the same object;
the "member didn't resolve → skip the celebration, keep the XP" rule is unchanged.

**`VoiceXpService` sweeps instead of tracking join/leave/mute events.** Voice XP is
Phase 2 of the system above, and the only signal with no event to react to — there is
no "voice message received," only "how long were they present." A `BackgroundService`
ticking every minute (mirroring `ReminderService`/`PresenceService`'s shape, with its
own interval — explicitly not either of theirs) samples who is currently eligible and
grants a flat per-tick amount, rather than checkpointing exact elapsed time on every
join/leave/mute-toggle. This is deliberate: the payout is already "per minute," so a
1-minute sample is exactly as coarse as what it rewards — a checkpoint's precision
would buy correctness at a grain finer than the reward ever uses, at the cost of new
per-channel state this codebase has no other precedent for. One consequence worth
knowing: `SocketVoiceChannel.ConnectedUsers` and each member's `VoiceState` are
already kept live by Discord.Net's own gateway cache, the same way `PresenceService`'s
tick reads live state without subscribing to anything — so `VoiceXpService` needs no
`UserVoiceStateUpdated` subscription and touches nothing in `BotService`'s fan-out.
**Eligibility is one predicate, `IsActive`, used for two things — and that is the
anti-abuse design.** It gates both who *earns* and who counts toward the "someone else
is here" threshold. Splitting them is the exploit: if muting stopped you earning but
still let you unlock XP for the person beside you, parking muted alts in a channel would
farm indefinitely. So being the only unmuted person in a room full of muted ones is
being alone, and earns what being alone earns.

Self-muted **or** self-deafened is enough to be out — either one means you are not in
the conversation, and the original "both together" rule let someone mute their mic and
idle all day. A missing `VoiceState` also counts as out, rather than taking the generous
reading. Moderator-applied server mute/deafen is still deliberately *not* checked:
someone silenced by a mod for an unrelated reason shouldn't lose XP for it, and unlike
self-muting it is not something they can do to themselves to farm.

**The AFK channel is skipped entirely.** It is where Discord *puts* people for being
idle, so two accounts parked there would earn forever — the one farm the server hands
out for free.

**Voice XP tapers with the day's total, and that is the answer to the farm no mute rule
can catch.** Two accounts idling *unmuted* look exactly like two people in a call: the
sweep sees presence, never participation. So `Helpers/VoiceXpCurve` holds the first hour
at 10 XP/min — exactly the flat rate that preceded the taper, so a normal session lost
nothing — then steps the rate down every half hour (8, 6, 5, 4, 3, 2) until it settles
at 1. Eight hours pays 1680 instead of 4800. Deliberately a taper and not a hard cap: a
cap teaches people the exact number of minutes to park for.

**The tier table is the tuning surface, and its shape is the point.** An earlier
two-tier version went 10 → 3 at minute 60, a 70% drop at a single minute, which made
hour two read as a punishment rather than a diminishing return; the half-hour steps keep
the sharpest early drop at 20%. Keep it sorted by minute with non-increasing rates, and
keep the trickle rate **non-zero** — at zero it stops being a taper and becomes the cap
this design exists to avoid.

Three things hold it together. **One:** the rate is a pure function of minutes already
banked today, so it needs no new state — `XpService.AddVoiceMinutesAsync` returns
today's bucket total *before* the increment, from the same round trip that records it,
and there is no second read to disagree with. **Two:** the taper rations the XP only.
The minutes are still recorded in full, because `MemberXp`'s comment is right that they
are a *fact* while XP is a *reward* — so `/leaderboard`'s Vocal view stays honest about
who actually sat in voice. **Three:** `TotalForMinutes` is the single source of truth —
`XpForSpan` is a difference of two of its values and `RateAt` is a one-minute span — so a
span crossing a tier boundary is split correctly, a tick covering several minutes pays
exactly what those minutes pay one at a time, and the rate *shown* cannot drift from the
rate *granted*. Define any new rate function that way round, never the reverse: a rate
defined independently and a total defined independently will disagree at a boundary
eventually, and the integer arithmetic stays exact only in this direction.

If counting the minutes fails, the payout is skipped rather than guessed — this is the
anti-abuse path, so it fails closed.

**A voice level-up is announced in the voice channel's own text chat**, not in the
guild's system channel. `SocketVoiceChannel` is an `IMessageChannel` (text-in-voice),
so `GrantVoiceXpAsync` casts the channel it already resolved for the exclusion check
and posts there — the card lands where the people who earned it are sitting rather than
interrupting `#général`. The system channel is kept only as the fallback for a channel
that does not resolve from the gateway cache; if it resolves but the bot cannot post in
it, the send is swallowed and logged like every other Discord side effect and the XP is
recorded regardless. `AnnounceAsync` resolves the member through
`channel as SocketGuildChannel`, which a voice channel also satisfies — so the avatar
on the card still works.

**Every module that reads `Context.Guild` must carry
`[CommandContextType(InteractionContextType.Guild)]`.** `config.yaml` ships
`register_globally: true`, and a global slash command is DM-enabled by default — so
without the attribute the command is reachable in a DM, where `Context.Guild` is null
and the handler can only throw. All six guild-dependent modules carry it;
`HelpModule`, `AbsenceModule` and `SpeakModule` deliberately do not, because they
never touch `Context.Guild` and `/help` genuinely works in a DM. Note the older
`[EnabledInDm(false)]` is obsolete in Discord.Net 3.20 and fails the build under
`-warnaserror`.

**`/help`'s embed has hard caps, and it silently died once from ignoring them.** A
field value may be **1024** characters and the whole embed **6000** — counting title,
description, every field *name and value*, and the footer. `EmbedBuilder.Build()`
throws on either, at **send** time, with nothing in the logs naming the length: the
command just stops responding. The "Commandes — Autres" field grew past 1024 and the
embed past 6000 as commands were added, and `/help` was dead for six-plus commits
before anyone noticed. This is why `HelpModule.BuildEmbed()` is a `static`, Context-free
builder — it can be constructed and measured without a gateway, which is the only
reason the caps are checkable at all. **Keep sections short and split one rather than
letting it grow**; 11 of the 25 allowed fields are used, so there is room. Note
`Embed.Length` is Discord.Net's own implementation of Discord's total, so measuring
against it cannot drift from what the API enforces.

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
`MessageCues` and `ReminderService`, `XpTracker.ExcludedChannels` (the spam channels
that earn no XP), `ShameModule.ExtraVoters`, and the per-user `PersonalComebacks` /
`RealNames` maps in `BotResponses` are literal snowflakes tied to one specific server.

Two of those are now *floors* rather than the whole story: `/config` can add excluded
channels and grant `/shame` voting to a role, but neither command can edit these lists —
see the runtime-configuration note above. The rest have no configuration surface at all.
`AvailabilityService.OwnerId` was deliberately left out of `/config`: it gates `/tell`,
`/dm`, `/absent` and the DM relay, so making it editable by any ManageGuild holder would
let them hand themselves those powers, including impersonating the relay.
