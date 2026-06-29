# Project S.Y.N.C.S.

**S**chedule **Y**our **N**ights & **C**oordinate the **S**quads — a Discord bot for
planning gaming sessions, activities and movie nights, and letting people sign up
in one click. The bot's user-facing language is French.

## Features

### Sessions
- **`/schedule create`** — a private 4-step wizard (type → day → hour → details)
  that posts an interactive session card to the channel.
- **`/schedule list`** — lists the server's active sessions; lets you republish a
  card into the current channel.
- **`/schedule edit <id>`** — edit a session you organized (title, date, time, max
  players).
- **`/schedule cancel <id>`** — cancel a session you organized; signed-up
  participants are notified by DM.
- Session cards carry **Join / Maybe / Decline** buttons plus organizer-only
  **Edit / Cancel** actions.
- **Native Discord events** — the organizer can link a session to a real entry in
  the server's *Events* tab; it stays in sync (time, title, location, participants)
  and is removed when the session is cancelled.
- **Lifecycle** — at start time a card flips to **🔴 In progress** (buttons
  disabled), then to **✅ Finished** ~2 h later (`SessionEvent.Duration`).
- **Reminders** — signed-up participants get a DM before the session starts.

### Polls & votes
- **`/poll create`** — propose several time slots (up to 10) and let everyone vote
  for all the slots that work for them; the most-voted slot is highlighted on
  close, and can be turned directly into a session.
- **`/vote create`** — same idea with free-text options (games, movies, …).
- **`/poll list` · `/vote list`** — list and republish active polls/votes.
- **`/poll delete <id>` · `/vote delete <id>`** — delete one you created.
- Polls and votes left open **auto-close after 2 days**.

### Other
- **`/emotestats`** — leaderboard of the server's most-used emotes (both in
  messages and as reactions).
- **`/help`** — in-Discord usage guide.
- **Personality** — the bot also reacts to chat with its own responses and cues
  (`ChatterService`, `BotResponses`, `MessageCues`, `BreakdownService`).

## Tech stack

- **.NET 10** console app (`Microsoft.Extensions.Hosting` generic host)
- **Discord.Net 3.20** (slash commands, components, modals, native events)
- **EF Core 10** over **SQLite** (`AppDbContext`, migrations run on startup)

## Project layout

```
ProjectSYNCS/
├─ Program.cs              # Host setup, DI wiring, runs EF migrations
├─ Commands/               # Slash-command modules (Schedule, Poll, Vote, EmoteStats, Help)
├─ Interactions/           # Button/select handlers (Components) and Modals
├─ Services/               # BotService (gateway), ReminderService, EventService,
│                          # PollService, EmoteStatsService, SessionEventSync, …
├─ Models/                 # SessionEvent, Participant, Poll, EmoteStat
├─ Data/AppDbContext.cs    # EF Core context
├─ Migrations/             # EF Core migrations
├─ Helpers/                # AppTime, AppInfo, SessionPermissions
├─ config.yaml             # Home Assistant add-on manifest (source of truth for version)
└─ Dockerfile / run.sh     # Container build (HA add-on)
```

Two hosted services run in the background: `BotService` (Discord connection,
command registration, interaction dispatch) and `ReminderService` (lifecycle
transitions and reminder DMs).

## Configuration

Settings come from `appsettings.json`, then `appsettings.{Environment}.json`,
then environment variables, then user secrets:

| Key | Description |
| --- | --- |
| `Discord:Token` | Bot token (required). |
| `Discord:DevelopmentGuildId` | Guild used for fast command registration in dev. |
| `Discord:RegisterCommandsGlobally` | `true` registers commands globally; `false` registers them to the dev guild. |
| `Database:Path` | SQLite file path (default `ProjectSYNCS.db`). |

## Running locally

```bash
cd ProjectSYNCS
dotnet user-secrets set "Discord:Token" "<your-bot-token>"
dotnet run
```

Set `Discord:DevelopmentGuildId` and leave `RegisterCommandsGlobally` as `false`
for instant command registration while developing. The SQLite database is created
and migrated automatically on first run.

## Deployment

The project ships as a **Home Assistant add-on** (`config.yaml`, `Dockerfile`,
`run.sh`, `repository.yaml`), published as a self-contained `linux-arm64` build.
The add-on version in `config.yaml` is the single source of truth — the
`<Version>` in `ProjectSYNCS.csproj` is parsed from it at build time and surfaced
via `AppInfo.Version`.
