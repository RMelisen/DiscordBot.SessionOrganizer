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
  messages and as reactions), paginated, with three filters: **30 days** (the
  default), **7 days**, and **all time**. The rolling windows only cover data
  recorded since daily buckets were added; all-time still includes everything
  counted before that.
- **`/goodbot`** — leaderboard of who has told the bot *good bot* or *bad bot*,
  with the same three filters as `/emotestats` — here **all time** is the default,
  since verdicts are rare enough that a rolling window is often empty. She notices
  either one: praise earns a silent reaction, a scolding earns a reply. A **👍 or 👎
  on one of her messages** counts as the same verdict without her answering back —
  on what she says, not on session cards or leaderboards, where a thumb means
  something else. Only counts when it follows something she actually said or reacted
  to, and only once per person per thing she did, whichever way they say it.
- **`/level [user]`** — her own XP/leveling system, entirely independent of the
  server's other leveling bot (same vocabulary, no shared state, no cross-reference).
  XP for talking (one grant per ~60s) and reacting (its own ~60s cooldown), plus a
  bonus for replying to or mentioning her, and for a genuinely-recorded good/bad-bot
  verdict. Crossing a level gets an unprompted card in her own voice — your avatar,
  the levels you went from and to, and a line she picks herself. A single
  paged leaderboard, no time filters — leveling doesn't reset weekly. `/leaderboard`
  opens the same ranking straight on the top page, rather than jumping to your own.
  Time spent present in a voice channel also earns XP, once a minute, as long as
  someone else is there too and you're not both self-muted and self-deafened.
- **`/help`** — in-Discord usage guide.
- **Owner-only commands** — the configured owner can speak through the bot
  (`/tell` into a channel, `/dm` to a person) and flag themselves unavailable
  (`/absent`), after which the bot answers anyone who pings them and forwards the
  mention by DM — which the owner can reply to, and the bot relays the answer back
  into the original channel. These are deliberately left out of `/help`.

### Personality

The bot is more than a scheduler: it answers when spoken to and reacts to the room.

- **Replies** — an @mention, or a reply to one of its messages, gets an answer drawn
  from large pools of canned French lines. Compliments, insults and greetings are
  detected from the message text and answered in kind.
- **No repeats** — every line goes through a picker that avoids whatever was said
  recently in that channel, so a pool feels as large as it actually is.
- **Typing pause** — replies wait behind the typing indicator for a moment scaled to
  the length of the line, so the bot reads as composing an answer rather than firing
  back instantly.
- **Reactions** — it adds an emote to messages nobody addressed to it (when they
  read as kind, hostile or a greeting), and sometimes joins in on a reaction someone
  else just added. Both are rationed by probability, and message reactions also by a
  per-channel cooldown, so it stays occasional rather than constant.
- **Jealousy** — she does not enjoy sharing a server. Another bot posting earns an
  occasional reaction and, more rarely, a muttered remark. Praising another bot in
  front of her earns a full sulk — and that praise no longer lands in her own
  `/goodbot` tally, since a bare "good bot" now goes to whichever bot acted most
  recently. Replying directly to her still always counts as hers; replying directly
  to another bot never does. Level-up announcements are exempt: the congratulation
  is for the person who levelled, not the bot that said so.
- **Self-preservation** — threatening to unplug, delete or reboot her gets a
  reaction, and it is the one behaviour where the owner comes off worse than anyone
  else: from him the threat is real and she is frightened, from anyone else it is a
  bluff and she says so. Vague phrasing ("je vais te débrancher") only counts when
  said *to* her, so ordinary talk about restarting a game server is safe — but
  naming her outright ("redémarrer syncs") reaches her from anywhere, mention or
  not, since the name leaves no doubt what is being restarted.
- **Rotating status** — the status line under the bot's name cycles through a large
  pool of one-liners.
- **An easter egg** — rare, and better discovered than documented.

All personality state is in memory by design and resets when the bot restarts.

## Tech stack

- **.NET 10** console app (`Microsoft.Extensions.Hosting` generic host)
- **Discord.Net 3.20** (slash commands, components, modals, native events)
- **EF Core 10** over **SQLite** (`AppDbContext`, migrations run on startup)

## Project layout

```
ProjectSYNCS/
├─ Program.cs              # Host setup, DI wiring, runs EF migrations
├─ Commands/               # Slash-command modules (Schedule, Poll, Vote,
│                          # EmoteStats, Help, Speak, Absence)
├─ Interactions/           # Button/select handlers (Components) and Modals
├─ Services/               # Hosted:      BotService, ReminderService, PresenceService
│                          # Data:        EventService, PollService, EmoteStatsService
│                          # Personality: ChatterService, ReactionService, BotResponses,
│                          #              MessageCues, ResponsePicker, BreakdownService
│                          # Discord:     SessionEventSync, SessionNotifier, EmoteTracker
├─ Models/                 # SessionEvent, Participant, Poll, EmoteStat
├─ Data/AppDbContext.cs    # EF Core context
├─ Migrations/             # EF Core migrations
├─ Helpers/                # AppTime, AppInfo, MessageFormat, SessionPermissions
├─ config.yaml             # Home Assistant add-on manifest (source of truth for version)
└─ Dockerfile / run.sh     # Container build (HA add-on)
```

Three hosted services run in the background: `BotService` (Discord connection,
command registration, interaction dispatch), `ReminderService` (reminder DMs,
session lifecycle transitions, poll auto-close) and `PresenceService` (the rotating
status line).

## Configuration

Settings come from `appsettings.json`, then `appsettings.{Environment}.json`,
then environment variables, then user secrets:

| Key | Description |
| --- | --- |
| `Discord:Token` | Bot token (required). |
| `Discord:DevelopmentGuildId` | Guild used for fast command registration in dev. |
| `Discord:RegisterCommandsGlobally` | `true` registers commands globally; `false` registers them to the dev guild. |
| `Database:Path` | SQLite file path (default `ProjectSYNCS.db`). |

The bot needs both privileged intents — **Server Members** and **Message Content** —
enabled in the Discord developer portal. Without *Message Content* the whole
personality side reads empty strings and silently stops responding.

It also needs the usual channel permissions where you want it active: send messages,
embed links, add reactions, and *Manage Events* if you want sessions mirrored into
the server's Events tab (that one degrades gracefully if missing).

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

Only `/data` is persisted by the add-on, so the SQLite file lives there in
production. Never commit a real token: `appsettings.json` and `config.yaml` ship
placeholders, and real credentials belong in user secrets (dev) or the add-on
options (prod).
