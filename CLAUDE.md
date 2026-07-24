# CLAUDE.md

Project S.Y.N.C.S. — a French-language Discord bot for scheduling gaming sessions,
polls and votes. See `README.md` for the user-facing feature list; this file covers
what you need to know before changing code.

## Commands

```bash
cd ProjectSYNCS
dotnet build                     # net10.0
dotnet run                       # needs Discord:Token (user secrets in dev)
dotnet ef migrations add <Name>  # migrations are applied automatically on startup
```

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

**Embed and component builders are shared.** `ScheduleModule.BuildEventEmbed` /
`BuildEventComponents` and the `PollModule` equivalents are `static` and are called
from the component handlers and from `ReminderService`. Change a card's rendering
in one place and every re-render path follows.

**Wizard state lives in component custom-ids**, not in memory — e.g.
`schedule:min:{category}:{date}:{hour}:{minute}`. Adding a wizard step means
threading a new segment through every handler *and* the matching `Retour` handler.
The only in-memory state is `_draftTitles`, so a failed modal can be reopened
pre-filled.

**Session lifecycle is idempotent.** `SessionEvent.RenderedPhase` records what was
last drawn on the card, so the background loop only re-renders on an actual
Scheduled → InProgress → Finished transition.

**Discord side effects must never break the flow.** `SessionEventSync` (native
Discord events) swallows and logs every exception; a missing Manage Events
permission degrades silently rather than failing the session. Keep that property.
Reminder DMs likewise catch `CannotSendMessageToUser` specifically.

## Version and deployment

`ProjectSYNCS/config.yaml` is the **single source of truth for the version** — the
csproj regex-parses it into `<Version>`, and `AppInfo.Version` surfaces it. Bump it
there and nowhere else.

The bot ships as a Home Assistant add-on: `Dockerfile` publishes a self-contained
`linux-arm64` build, and `run.sh` maps the add-on options to `Discord__Token`,
`Discord__RegisterCommandsGlobally` and `Database__Path=/data/ProjectSYNCS.db`.

## Hardcoded ids

`AvailabilityService.OwnerId` (the owner, who gets special treatment throughout
`ChatterService`), the level-up bot id and the `hi_cat` emote id in `MessageCues`
and `ReminderService` are literal snowflakes tied to one specific server.
