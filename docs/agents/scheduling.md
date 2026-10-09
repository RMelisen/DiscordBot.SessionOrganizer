# Scheduling — sessions, polls, votes, giveaways

`ScheduleModule`, `PollModule`, `VoteModule`, `GiveawayModule` are group modules (so
`ignoreGroupNames: true` on every component/modal attribute). `PollModule`'s static card builders
render both poll kinds, branching on `Poll.Kind`, so `/vote` cards go through them too.

## Sessions

Two stateless helpers wrap a session's outward side effects: `SessionEventSync` (create/update/delete
the native Guild Scheduled Event — swallows and logs everything; a missing Manage Events permission
degrades silently) and `SessionNotifier` (cancellation DMs).

**Lifecycle is idempotent.** `SessionEvent.RenderedPhase` records what was last drawn, so the loop
re-renders only on an actual Scheduled → InProgress → Finished transition.

## `ReminderService` — one 5-minute loop, three independent jobs

Reminder DMs, session lifecycle re-renders, poll auto-close. Each item is guarded separately (root
`CLAUDE.md`, reliability). **The timing constants are coupled to the 5-minute interval**:

- The reminder window in `EventService.GetEventsNeedingReminderAsync` is **25–35 minutes** before
  start — wider than the loop so none is missed, `ReminderSent` (reset when the time is edited) so
  none is sent twice.
- `SessionEvent.Duration` (2 h) sets both InProgress → Finished and the native event's end.
- `ReminderService.PollLifetime` (2 days) drives auto-close.

## Roll call — `SessionAttendanceService`

She calls the roll for **Game and Movie** sessions only (an Activity or Other can happen offline).
Timing rules are pure, in `Helpers/SessionAttendance`.

- **Fed by `VoiceXpService`'s sweep**, once per guild per minute, with every human connected to a
  voice channel (AFK aside, muted included). A session has no voice channel of its own (the native
  event is External), so "present" means any of the server's voice channels.
- **What it learns is stored**: `Participant.FirstSeenInVoiceAt` (first sighting from start − 15 min
  to start + `Duration`), `SessionEvent.LateCallSent` / `RecapSent`. Each flag is written **before**
  its message goes out: a crash costs the line, never a second ping. `UpdateEventAsync` resets all
  three when the time moves, as it does `ReminderSent`.
- **Late call** at start + 10 min, until start + 30 (later, it's skipped, never sent stale): pings
  the Joined people not seen yet, `AllowedMentions` narrowed to them. It waits for the first
  arrival — with nobody there, the session may be happening somewhere else.
- **Recap** once fewer than two of the people who came have been in voice for 15 min (not before
  start + 30), or at start + `Duration` + 2 h. **Absences are a count, never names** (the owner's
  call); the people who came and who were late (first seen after start + 10) are named, inert.
  Silent when nobody came or fewer than two were Joined. Headers gender nobody (« Au
  rendez-vous », « Absences »).
- The session list is cached and re-read every 5 minutes; the scatter clock is in memory, so a
  restart only delays a recap.

## Weekend poll — `WeekendPollService`

Every Wednesday at a minute drawn from the week between 18:00 and 20:00 (`Helpers/WeekendPoll`),
an ordinary date poll in her main channel (home guild): Friday, Saturday and Sunday at 21:00,
organizer = her id. Posted Wednesday so `PollLifetime` closes it Friday evening, before the first
slot; nothing is posted after 22:00.

- **She stands down** when a session is already planned that weekend or someone's date poll is
  still open. **Once a week**: the claim is the poll itself (one of hers in the last six days),
  so no state file. A card that fails to post deletes its poll.
- **On auto-close** `ReminderService` replies to the card with the winning evening(s)
  (`WeekendPollResultLines`) or sulks (`WeekendPollDeadLines`). A manual close says nothing.
- **Permission exception** in `PollModule.OnToSessionAsync`: on *her* poll, anyone who voted for a
  winning slot may create the session (and becomes its organizer); everyone else's polls stay with
  their organizer and staff. A second voter clicking after a session exists at that time is told
  so instead of creating a duplicate. Her poll's title is a question, so it doesn't pre-fill the
  session name.

## The two wizards keep state differently

**`ScheduleModule` threads state through custom-ids** (`schedule:min:{category}:{date}:{hour}:{minute}`):
a new step means a new segment through every handler *and* the matching `Retour` handler. Only the
title is in memory (`_draftTitles`), so a failed modal can be reopened pre-filled.

**`PollModule` and `VoteModule` keep the whole draft in a `static ConcurrentDictionary`** keyed by
user id (a variable-length list doesn't fit a custom-id), removed on finish or cancel.

**`/vote create` must create its own wizard message.** The command posts "Définir le titre" and
every later step only *updates* it; creating it from the modal response drops the first option-add
update. That's why the `vote:begin` button exists.

## Giveaways

`GiveawayModule` is a group module but **not a wizard**: one command with fixed options, no draft
state. The duration is a `[Choice]` list (nothing to parse or reject).

**The draw is one call, and that makes it crash-safe.** `GiveawayService.TryDrawAsync` picks
winners, marks them and sets `IsClosed` in one `SaveChanges`, and returns `null` if already drawn.
Worst case after a crash is a missing announcement — never a second draw. Any "tirer maintenant"
button must go through that call. Winners are `GiveawayEntry.IsWinner` (a winner is an entrant). The
pick is a partial Fisher-Yates over a copy, **not** `OrderBy(_ => Random)`.

**`GiveawayDrawService` ticks every minute** (five minutes late reads as broken on a 10-minute
giveaway). `EndsAt` is absolute, so a restart simply resumes. `GiveawayService` = transient DB
wrapper, `GiveawayDrawService` = hosted sweep.

**The card caps its entrant list** at 20 names then "… et N autre(s)" (worst case 499 chars of the
1024 field). Mentions in an embed don't ping.

**Entering is add/remove, never a toggle** — the card has both "Participer" and "Ne plus
participer". `AddEntryAsync` / `RemoveEntryAsync` return whether anything changed so a no-op click
can say so. A drawn card drops its buttons entirely.

**The announcement is meant to ping**: `AllowedMentions(AllowedMentionTypes.Users)` — users only.
