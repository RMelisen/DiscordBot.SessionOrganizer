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
register instantly, global ones take up to an hour to propagate.

There is no test project and no CI. Verify changes by building and, when it
matters, running the bot against the dev guild.

## Language

All user-facing strings — command names, descriptions, embeds, button labels,
error messages — are **in French**. Code, comments and logs are in English.

## Architecture

`Program.cs` is the composition root: DI wiring, `MigrateAsync()`, then two hosted
services.

- **`BotService`** — gateway login, slash-command registration, interaction
  dispatch. Fans `MessageReceived` out to `EmoteTracker` and `ChatterService`.
- **`ReminderService`** — a single 5-minute loop that does three independent jobs:
  reminder DMs, session lifecycle card re-renders, and poll auto-close.

Layers: `Commands/` (slash modules + the embed/component builders), `Interactions/`
(component handlers and modal DTOs), `Services/` (EF repositories + behaviour),
`Models/`, `Data/AppDbContext.cs`, `Helpers/`.

**DI lifetimes are not arbitrary.** `AppDbContext` and the services that wrap it
(`EventService`, `PollService`, `EmoteStatsService`) are **transient**; the
personality collaborators that hold in-memory state (`ChatterService`,
`BreakdownService`, `AvailabilityService`, `EmoteTracker`) are **singletons**.
Registering a stateful service as transient silently drops its state.

The "personality" subsystem is separate from the scheduling one: `ChatterService`
decides *how* to react to a message, `BotResponses` holds the canned lines,
`MessageCues` does the nice/mean/greeting intent detection, `BreakdownService`
plays the easter egg.

## Conventions that will bite you

**SQLite cannot translate `DateTimeOffset` comparisons.** Every query filters
booleans/ids in SQL, calls `ToListAsync()`, then applies the date window and
ordering **in memory**. See `EventService.GetActiveEventsAsync` and
`GetEventsNeedingReminderAsync`. A `.Where(e => e.ScheduledAt > now)` sent to the
database throws at runtime.

**Discord snowflakes are `ulong`; SQLite integers are signed 64-bit.** Every
snowflake property needs `.HasConversion<long>()` in `AppDbContext.OnModelCreating`.
Easy to forget when adding a model.

**Never use `DateTime.Now`.** Production runs in UTC; all wall-clock handling goes
through `Helpers/AppTime` (pinned to `Europe/Paris`, DST-aware via
`TryParseWallClock`). Store instants as UTC `DateTimeOffset`, render them to users
as Discord `<t:unix:...>` timestamps so the client localises them.

**`[ComponentInteraction]` and `[ModalInteraction]` inside a `[Group]` module need
`ignoreGroupNames: true`.** `ScheduleModule`, `PollModule` and `VoteModule` are all
group modules, so without that flag Discord.Net prefixes the group name onto the
custom-id and the handler simply never fires — no error, no log. Every such
attribute in those files already passes it; match that when adding one.

**Embed and component builders are shared.** `ScheduleModule.BuildEventEmbed` /
`BuildEventComponents` and the `PollModule` equivalents are `static` and are called
from the component handlers and from `ReminderService`. Change a card's rendering
in one place and every re-render path follows.

**The two wizards keep state differently.** `ScheduleModule` threads its state
through component custom-ids — e.g. `schedule:min:{category}:{date}:{hour}:{minute}`
— so adding a step means threading a new segment through every handler *and* the
matching `Retour` handler; only the title is held in memory (`_draftTitles`), so a
failed modal can be reopened pre-filled. `PollModule` and `VoteModule` cannot do
that (a variable-length list of slots/options does not fit in a custom-id), so they
keep the whole draft in a `static ConcurrentDictionary` keyed by user id, removed
on finish or cancel.

All in-memory state resets on restart **by design** — the draft dictionaries above,
`BreakdownService`'s channel cooldown, and `AvailabilityService`'s absent flag and
its bounded (200-entry) map of forwarded mentions.

**Session lifecycle is idempotent.** `SessionEvent.RenderedPhase` records what was
last drawn on the card, so the background loop only re-renders on an actual
Scheduled → InProgress → Finished transition.

**Discord side effects must never break the flow.** `SessionEventSync` (native
Discord events) swallows and logs every exception; a missing Manage Events
permission degrades silently rather than failing the session. Keep that property.
Reminder DMs likewise catch `CannotSendMessageToUser` specifically.

**There are two separate authorization models.** Session and poll management uses
`Helpers/SessionPermissions.CanManage` — the organizer, or any guild
Administrator / ManageGuild holder. The owner-only commands (`/tell`, `/dm`,
`/absent`) instead compare `Context.User.Id` against `AvailabilityService.OwnerId`
inline in the module and reply ephemerally. Don't conflate them.

**Relayed text must never become a mass-ping vector.** Every path that sends text
on someone's behalf (`SpeakModule`, `ChatterService`'s DM relay) passes
`new AllowedMentions(AllowedMentionTypes.Users)` — users only, never `@everyone`,
`@here` or roles — and renders quoted text through `MessageFormat.Quote` so relayed
words are visibly not the bot's own. Preserve both when adding a relay.

## Version and deployment

`ProjectSYNCS/config.yaml` is the **single source of truth for the version** — the
csproj regex-parses it into `<Version>`, and `AppInfo.Version` surfaces it. Bump it
there and nowhere else.

The bot ships as a Home Assistant add-on: `Dockerfile` publishes a self-contained
`linux-arm64` build, and `run.sh` maps the add-on options to `Discord__Token`,
`Discord__RegisterCommandsGlobally` and `Database__Path=/data/ProjectSYNCS.db`.

The GitHub remote is **public**. `appsettings.json` ships a `BOT_TOKEN` placeholder
and `config.yaml` a `PASTE_YOUR_TOKEN_HERE` one; real tokens go in user secrets
(dev) or the add-on options (prod), never in a tracked file.

## Hardcoded ids

`AvailabilityService.OwnerId` (the owner, who gets special treatment throughout
`ChatterService`), the level-up bot id and the `hi_cat` emote id in `MessageCues`
and `ReminderService` are literal snowflakes tied to one specific server.
