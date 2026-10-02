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
