# Project S.Y.N.C.S.

**S**chedule **Y**our **N**ights & **C**oordinate the **S**quads — a Discord bot for
planning gaming sessions, activities and movie nights, and letting people sign up
in one click. It also keeps score: XP and levels, emote and verdict leaderboards, and
a wall of shame. The bot's user-facing language is French.

- [Commands](#commands)
- [Sessions](#sessions) · [Polls & votes](#polls--votes) · [Giveaways](#giveaways)
- [Levels & XP](#levels--xp) · [Leaderboards & stats](#leaderboards--stats) · [Wall of shame](#wall-of-shame)
- [Staff & owner](#staff--owner) · [Personality](#personality)
- [Tech stack](#tech-stack) · [Project layout](#project-layout) · [Configuration](#configuration) · [Running locally](#running-locally) · [Deployment](#deployment)

## Commands

| Command | What it does | Who |
| --- | --- | --- |
| `/schedule create · list · edit · cancel` | Plan and manage sessions | everyone |
| `/poll create · list · delete` | Vote on time slots | everyone |
| `/vote create · list · delete` | Vote on free-text options | everyone |
| `/giveaway create · list · delete` | Prize draws | everyone |
| `/level [user]` | Your card: level, rank, progress | everyone |
| `/leaderboard` | Server ranking — three views, three windows | everyone |
| `/emotestats` | Most-used emotes | everyone |
| `/goodbot` | Who praised or scolded the bot | everyone |
| `/shame` | The wall of shame | everyone |
| `/shame user:@someone` | Put someone on it | staff |
| `/config` | Per-server settings, no redeploy | staff |
| `/addxp · /removexp` | Manual XP adjustment | staff |
| `/tell · /dm · /absent` | Speak through the bot; flag yourself away | owner |
| `/help` | In-Discord usage guide | everyone |

"Staff" means Administrator or Manage Server, the bot's owner, or a configured
moderator role. Staff-only commands are visible to everyone but refuse politely.

## Features

### Sessions

- **`/schedule create`** — a private 4-step wizard (type → day → hour → details)
  that posts an interactive session card to the channel.
- **`/schedule list`** — the server's active sessions; lets you republish a card
  into the current channel.
- **`/schedule edit <id>` · `/schedule cancel <id>`** — for sessions you organized.
  Cancelling notifies signed-up participants by DM.
- Session cards carry **Join / Maybe / Decline** buttons plus organizer-only
  **Edit / Cancel** actions.
- **Native Discord events** — the organizer can link a session to a real entry in
  the server's *Events* tab; it stays in sync (time, title, location, participants)
  and is removed when the session is cancelled.
- **Lifecycle** — at start time a card flips to **🔴 In progress** (buttons
  disabled), then to **✅ Finished** ~2 h later.
- **Reminders** — signed-up participants get a DM before the session starts.

### Polls & votes

- **`/poll create`** — propose several time slots (up to 10); everyone votes for
  *all* the slots that work for them. The most-voted slot is highlighted on close and
  can be turned directly into a session.
- **`/vote create`** — same idea with free-text options (games, movies, …).
- **`/poll list` · `/vote list`** — list and republish active polls/votes.
- **`/poll delete <id>` · `/vote delete <id>`** — delete one you created.
- Polls and votes left open **auto-close after 2 days**.

### Giveaways

- **`/giveaway create`** — a prize, a duration from a fixed list (10 minutes to
  7 days), and optionally a description and a winner count (1 by default, 10 max).
- The card carries **Participer / Ne plus participer** buttons and lists the
  entrants — capped at 20 names with the rest summarised, since an embed field holds
  1024 characters and a giveaway draws a bigger crowd than a session.
- A **1-minute sweep** draws the winners at random when time is up, closes the card
  and announces the result in the bot's own voice, pinging the winners (users only —
  never roles or `@everyone`). The end time is an absolute instant, so a restart loses
  nothing and anything that came due while the bot was down is drawn on the next pass.

### Levels & XP

Her own leveling system, entirely independent of the server's other leveling bot —
same vocabulary, no shared state, no cross-reference.

**Earning XP**

| Source | Rate |
| --- | --- |
| Talking | one grant per ~60 s |
| Reacting | its own ~60 s cooldown |
| Replying to or mentioning her | bonus on top of the message grant |
| A genuinely-recorded good/bad-bot verdict | bonus, its own 30 s cooldown |
| Time in a voice channel | once a minute, if eligible — see below |

Voice XP needs **someone else present**, and you **not self-muted or self-deafened**.
Muted people don't count as company either, so being the only unmuted person in a
channel earns nothing. The AFK channel never earns, and some channels are excluded
entirely.

The voice rate **tapers over the day**: the first hour pays the full 10 XP/min, then
steps down every half hour (8, 6, 5, 4, 3, 2) until it settles at 1, resetting at
local midnight. A normal session is worth what it always was, an evening-long one
still pays, and idling all day doesn't. Minutes are still counted in full, so the
Vocal leaderboard stays honest about time actually spent.

**Seeing it**

- **`/level [user]`** — a card: avatar, level, rank, and a progress bar toward the
  next level. No filters — it's a profile, not a ranking.
- **`/leaderboard`** — the ranked list, five per page, every row carrying that
  person's real avatar. Three buttons switch what it ranks — **Niveaux** (XP),
  **Réactions** (reactions added), **Vocal** (eligible time in voice) — and three more
  switch the window: **all time** (default), **30 days**, **7 days**. In a window the
  XP view ranks by XP earned in that window and drops the level, which is a lifetime
  figure and can't be recomputed for a week.
- Crossing a level gets an unprompted card in her own voice: your avatar, the levels
  you went from and to, and a line she picks herself. Earned in voice, it's announced
  in that voice channel's own text chat rather than in the server's main one.

Both surfaces are built with Discord's *Components V2* rather than embeds, so nothing
is rendered or uploaded — Discord loads the faces itself.

### Leaderboards & stats

- **`/emotestats`** — the server's most-used emotes, written *and* as reactions,
  paginated, with three filters: **30 days** (default), **7 days**, **all time**.
- **`/goodbot`** — who has told the bot *good bot* or *bad bot*. Same three filters,
  but **all time** is the default here, since verdicts are rare enough that a rolling
  window is often empty.

She notices either verdict: praise earns a silent reaction, a scolding earns a reply.
A **👍 or 👎 on one of her messages** counts as the same verdict without her answering
back — on what she *says*, not on session cards or leaderboards, where a thumb means
something else. It only counts when it follows something she actually said or reacted
to, and only once per person per thing she did, however they phrase it.

**`good girl` / `bad girl`** count the same on the tally but get a different answer:
praise in that register draws a rather different set of emotes, and a scolding gets its
own flustered reply rather than the wounded-professional-pride one.

Verdicts resist gaming. *"bad good bot"* and *"not good bot"* are cancelled by what
precedes them, and one that is merely quoted, supposed or self-referential
(*"this sentence is false → good bot"*) isn't counted at all.

> The rolling windows on both commands only cover data recorded since daily buckets
> were added; all-time still includes everything counted before that.

### Wall of shame

**`/shame`** — four titles on one page, with the same three filters as the other
rankings (**30 days** by default, **7 days**, **all time**). Built with *Components V2*
like `/level`, so each title shows its current holder's real avatar beside their name;
the runners-up are plain text beneath.

**Le Malfaisant** — hostility, scored from every message.
One hit per human it was aimed at, counting an explicit `@` or the person it replies
to. Roles and `@everyone` never count, other bots never count, and being mean to *her*
does. That half is uncapped, so a message aimed at four people is worth four. A mean
message aimed at **nobody** counts too, as a single point, rationed to one per person
per channel per minute so a rant can't run away with the title.

> Be aware the untargeted half is noisier: complaining about a *game* ("ce boss est
> nul") reads as hostility too, since nobody was named for her to tell the difference
> by.

**Le Banni** — voted, via **`/shame user:@someone`**, announced publicly in her voice.
Voting is **staff-only**, which is what makes the title a deterrent rather than a game.
A target takes at most **2 votes a day from everyone combined**, so nobody gets
dogpiled; there's no per-voter quota. You can vote for yourself; you cannot vote for a
bot, and least of all for her.

**Le Perfide** — consorting with the competition: replying to another bot, mentioning
one, or running one of their slash commands. She can't see the command itself, but the
rival's reply names whoever invoked it. Rationed to one hit per person per channel per
minute, so it ranks how often you *turn to* another bot rather than how chatty that bot
is. Ephemeral replies and old-style prefix commands (`!play`) leave no trace and go
uncounted.

**L'Hystérique** — shouting: a message long enough to be a sentence, written almost
entirely in capitals. Short all-caps words — `LOL`, `OK`, `MDR`, `GG WP` — are simply
how people write those words and never count, and neither does emphasising a word or
two mid-sentence. Rationed like *Le Perfide*, because shouting arrives in bursts and
one argument would otherwise decide the title forever.

Every counter starts at zero the day it ships and nothing can be backfilled.

### Staff & owner

- **`/config`** — per-server configuration, applied without a redeploy: a **moderator
  role** allowed to vote with `/shame`, and extra **channels where nothing counts** (no
  XP, and ignored by the wall of shame). Everything here is *additive* — the defaults
  built into the code stay in force, so configuring something can never revoke an
  existing right or un-exclude a channel, and a server that never touches `/config`
  behaves exactly as before. **`/config show`** prints the current state, separating the
  built-in defaults from what was added.
- **`/addxp <member> <amount>` · `/removexp <member> <amount>`** — manual XP
  adjustment. Ephemeral, clamped at zero, and deliberately silent: crossing a level this
  way fires no level-up card, since that card celebrates something earned.
- **Owner-only** — the configured owner can speak through the bot: **`/tell`** into a
  channel (usable from a DM with the bot too, picking the destination from an
  autocompleted list of channels it can post in) and **`/dm`** to a person. **`/absent`**
  flags him unavailable, after which the bot answers anyone who pings him and forwards
  the mention by DM — which he can reply to, and the bot relays the answer back into the
  original channel. These are deliberately left out of `/help`.

> Staff commands are **not** hidden from Discord's command picker. Discord's own
> permission gate understands permission bits, not user ids, so it couldn't admit the
> owner on a server where his roles carry no Manage Server. The check inside each
> handler is the real one, and non-staff get a polite ephemeral refusal.

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
- **Reactions** — it adds an emote to messages nobody addressed to it (when they read
  as kind, hostile or a greeting), and sometimes joins in on a reaction someone else
  just added. Both are rationed by probability, and message reactions also by a
  per-channel cooldown, so it stays occasional rather than constant.
- **Jealousy** — she does not enjoy sharing a server. Another bot posting earns an
  occasional reaction and, more rarely, a muttered remark. Praising another bot in front
  of her earns a full sulk — and that praise doesn't land in her own `/goodbot` tally,
  since a bare "good bot" goes to whichever bot acted most recently. Naming a bot
  settles it outright, and an **@mention beats a reply**: "good bot @OtherBot" is never
  hers no matter who acted last, even inside a reply to her, since the mention is what
  you deliberately typed and a reply is often just quoting. With nobody mentioned, the
  reply decides; with both bots mentioned it's hers, since she was still named.
- **Level-ups from the other bot** — she congratulates the person and sulks about where
  the level came from, in the same line: she has her own XP system and nobody used it.
  That counts as her answer, so the ordinary jealousy is skipped rather than piling a
  reaction and a mutter on top.
- **Self-preservation** — threatening to unplug, delete or reboot her gets a reaction,
  and it's the one behaviour where the owner comes off *worse* than anyone else: from him
  the threat is real and she's frightened; from anyone else it's a bluff and she says so.
  Vague phrasing ("je vais te débrancher") only counts when said *to* her, so ordinary
  talk about restarting a game server is safe — but naming her outright ("redémarrer
  syncs") reaches her from anywhere, mention or not.
- **Rotating status** — the status line under the bot's name cycles through a large
  pool of one-liners.

All personality state is in memory by design and resets when the bot restarts.

## Tech stack

- **.NET 10** console app (`Microsoft.Extensions.Hosting` generic host)
- **Discord.Net 3.20** (slash commands, components, modals, autocomplete, native events)
- **EF Core 10** over **SQLite** (`AppDbContext`, migrations run on startup)

## Project layout

```
ProjectSYNCS/
├─ Program.cs              # Host setup, DI wiring, runs EF migrations
├─ Commands/               # 13 slash-command modules: Schedule, Poll, Vote, Giveaway,
│                          # Level, Shame, EmoteStats, BotFeedback, Config, XpAdmin,
│                          # Speak, Absence, Help
├─ Interactions/
│  ├─ Components/          # Button and select handlers for published cards
│  ├─ Modals/              # Modal DTOs
│  └─ Autocomplete/        # Channel suggestions for /tell
├─ Services/               # Hosted:      BotService, ReminderService, PresenceService,
│                          #              VoiceXpService, GiveawayDrawService
│                          # Data (EF):   Event, Poll, EmoteStats, BotFeedback, Xp,
│                          #              Giveaway, Shame, GuildConfig
│                          # Gateway:     XpTracker, ShameTracker, BotFeedbackTracker,
│                          #              EmoteTracker, RivalryService
│                          # Personality: ChatterService, ReactionService, BotResponses,
│                          #              MessageCues, ResponsePicker, AvailabilityService
│                          # Discord:     SessionEventSync, SessionNotifier
├─ Models/                 # Sessions and polls, plus a totals+daily-buckets pair each
│                          # for emotes, verdicts, XP and shame, and the guild config
├─ Data/AppDbContext.cs    # EF Core context
├─ Migrations/             # EF Core migrations (applied automatically on startup)
├─ Helpers/                # AppTime, AppInfo, LevelCurve, VoiceXpCurve, LevelCardUi,
│                          # AvatarUi, BotChat, Emotes, EmoteMarkup, StatsPeriodUi,
│                          # MessageFormat, SessionPermissions, LevelUpAnnouncement
├─ config.yaml             # Home Assistant add-on manifest (source of truth for version)
└─ Dockerfile / run.sh     # Container build (HA add-on)
```

**Five hosted services** run in the background, each on its own interval by design:

| Service | Interval | Job |
| --- | --- | --- |
| `BotService` | — | Gateway connection, command registration, interaction dispatch |
| `ReminderService` | 5 min | Reminder DMs, session lifecycle, poll auto-close |
| `PresenceService` | 5 min | The rotating status line |
| `VoiceXpService` | 1 min | Samples voice channels and grants XP |
| `GiveawayDrawService` | 1 min | Draws giveaways whose time is up |

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
personality side reads empty strings and silently stops responding. *Server Members* is
necessary but not sufficient for avatars: the client also downloads the full member list
on connect, since Discord otherwise caches only the people seen since the last restart
and every avatar on `/level`, `/leaderboard` and `/shame` falls back to a generic
placeholder.

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
for instant command registration while developing — global commands take up to an hour
to propagate. The SQLite database is created and migrated automatically on first run.

Note that guild-scoped commands aren't reachable in DMs, so anything meant to work
there (like `/tell`) needs global registration to test.

There is no test project and no CI: verify changes by building and running against a
development guild.

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
