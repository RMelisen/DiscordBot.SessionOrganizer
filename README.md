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

### Giveaways
- **`/giveaway create`** — post a prize draw: a prize, a duration picked from a fixed
  list (10 minutes to 7 days), and optionally a description and a winner count (1 by
  default, 10 max). The card carries **Participer / Ne plus participer** buttons and
  lists the entrants, the way a session card lists its players — capped at 20 names
  with the rest summarised, since an embed field holds 1024 characters and a giveaway
  draws a bigger crowd than a session.
- **`/giveaway list` · `/giveaway delete <id>`** — list and republish running draws,
  or delete one you started.
- When the time is up a **1-minute sweep** draws the winners at random, closes the
  card and announces the result in the bot's own voice, pinging the winners (users
  only — never roles or `@everyone`). The end time is stored as an absolute instant,
  so a restart loses nothing and anything that came due while the bot was down is
  drawn on the next pass.

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
  the levels you went from and to, and a line she picks herself. `/level` shows your
  own card: avatar, level, rank and a progress bar toward the next one.
  **`/leaderboard`** is the ranked list, five per page, every row carrying that
  person's real avatar — both are built with Discord's *Components V2* rather than
  embeds, so nothing is rendered or uploaded; Discord loads the faces itself. Three
  buttons switch what it ranks: **Niveaux** (XP), **Réactions** (reactions added) and
  **Vocal** (eligible time in voice), and three more switch the window — **all time**
  (the default), **30 days** and **7 days**. In a window the XP view ranks by XP earned
  in that window and drops the level, which is a lifetime figure and can't be
  recomputed for a week. As with `/emotestats`, the windows only cover data recorded
  since daily buckets were added; all-time still includes everything counted before
  that. `/level` itself carries no filters — it's a profile, not a ranking. Time spent present in a voice channel
  also earns XP, once a minute, as long as someone else is there too and you're not
  self-muted or self-deafened — and muted people don't count as company either, so
  being the only unmuted person in a channel earns nothing. The AFK channel never
  earns. The rate also **tapers over the day**: the first hour pays the full 10 XP/min,
  then the rate steps down every half hour (8, 6, 5, 4, 3, 2) until it settles at 1,
  resetting at local midnight — so a normal session is worth what it always was, an
  evening-long one still pays, and idling all day doesn't. Minutes are still counted in full, so the Vocal leaderboard
  stays honest about time actually spent. A level gained that way is announced in the voice channel's own text chat
  rather than in the server's main one. Some channels are excluded from earning
  entirely.
- **`/shame`** — the wall of shame, three titles on one page with the same three
  filters as the other rankings (**30 days** by default, **7 days**, **all time**).
  Built with *Components V2* like `/level`, so each title shows its current holder's
  real avatar beside their name; the runners-up are listed as plain text beneath.
  **Le Malfaisant** is earned: she scores every message and files one hit per human it
  was hostile to, counting an explicit `@` or the person it replies to — roles and
  `@everyone` never count, other bots never count, and being mean to *her* does.
  It is uncapped, so a mean message aimed at four people is worth four. **Le Banni** is
  voted: **`/shame user:@someone`**, announced publicly in her voice. Voting is
  **staff-only** — Administrator or Manage Server, the owner, plus a short hand-kept
  list — which is what makes the title a deterrent rather than a game. A target can
  take at most **2 votes a day from everyone combined**, so nobody gets dogpiled; there
  is no per-voter quota. You can vote for yourself; you cannot vote for a bot, and least
  of all for her. **Le Perfide** is for consorting with the competition: replying to another
  bot, mentioning one, or running one of their slash commands — she can't see the
  command itself, but the rival's reply names whoever invoked it. Rationed to one hit
  per person per channel per minute, so it ranks how often you turn to another bot
  rather than how chatty that bot is. Ephemeral replies and old-style prefix commands
  (`!play`) leave no trace and go uncounted. All three counters start at zero the day
  they ship and nothing can be backfilled.
- **`/addxp <member> <amount>` · `/removexp <member> <amount>`** — staff-only manual XP
  adjustment (Administrator or Manage Server, plus the bot's owner). Ephemeral, clamped
  at zero, and deliberately silent: crossing a level this way fires no level-up card,
  since that card celebrates something earned. Hidden from Discord's command picker for
  anyone without the permission.
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
  to another bot never does. A level-up announced by the *other* leveling bot gets its
  own treatment: she congratulates the person and sulks about where the level came
  from, in the same line — she has her own XP system and nobody used it. That counts
  as her answer to the message, so the ordinary jealousy is skipped there rather than
  piling a reaction and a mutter on top.
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
