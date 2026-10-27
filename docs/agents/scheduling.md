# Scheduling — sessions, polls, votes, giveaways

`ScheduleModule`, `PollModule`, `VoteModule`, `GiveawayModule` are group modules (so
`ignoreGroupNames: true` on every component/modal attribute). `PollCards` renders both poll kinds,
branching on `Poll.Kind`, so `/vote` cards go through it too.

## The cards — Components V2

`SessionCards` and `PollCards` (static, Context-free, in `Commands/Scheduling/`) build the session
and poll cards as Components V2 (root `CLAUDE.md`: no content, no embeds, the flag on every edit,
`AllowedMentions.None`). Every send goes through their `SendAsync`, every edit through `ApplyTo`.

- **A card from before V2 is an embed card.** `ApplyTo` clears content and embeds, so an edit
  converts it; if Discord refuses the edit anyway, `CardUpdater` reposts it through `CardRepost`.
  `CardUpdater` is the one way a background job or a panel redraws a public card.
- **Budgets**: 40 components in the whole tree, 4000 characters of text. The session card is about
  15 components, the poll card 21 (ten vote buttons). Rosters cap at `SessionSeats.MaxCardNames` (20)
  names, polls at `MaxPollVoterNames` (8) per option, a closed card's tie line at three options. The
  checks build every card at its worst (open, closed, cancelled, anonymous, date poll): **re-run
  them after adding anything to a card.**
- Categories (emoji and name) live in `Helpers/Scheduling/SessionCategories`, the one table the
  cards, the native event, the voice status and `/report` read.

## The « Gérer » panel

The public cards keep only what everyone uses: Rejoindre / Peut-être / Refuser and the native
event link on a session, the vote buttons and « Mes votes » on a poll. Everything for the organiser
(Modifier, Annuler, Republier, remove the native event, stop a weekly series; on a poll Clôturer,
Renommer, Prolonger, add or remove an option) is behind **« Gérer »**, an ephemeral panel
(`SessionManage`, `PollManage`). `SessionPermissions.CanManage` is checked on every panel click,
not only on « Gérer ».

- **`ManageSnapshots`** (singleton, 10 min) keeps the row « Gérer » loaded, so « Modifier » and
  « Renommer » open their modal without a database read: a modal must be the first response.
  Anything that changes the row forgets its snapshot.
- Cancelling and removing an option ask for confirmation in the panel (`event:cancelyes|no`,
  `poll:rmoptyes|no`).
- A panel click redraws the public card through `CardUpdater`; a click on the card itself redraws
  through `ModifyOriginalResponseAsync`. `OnEphemeral` in each handler tells them apart.

## Sessions

Two stateless helpers wrap a session's outward side effects: `SessionEventSync` (create/update/delete
the native Guild Scheduled Event — swallows and logs everything; a missing Manage Events permission
degrades silently) and `SessionNotifier` (cancellation DMs). The native event's location is the
session's voice channel when it has one.

**Lifecycle is idempotent.** `SessionEvent.RenderedPhase` records what was last drawn, so the loop
re-renders only on an actual Scheduled → InProgress → Finished transition. An edit of the time or
the duration resets it to Scheduled.

**A session has a length** (`DurationMinutes`, 15 min – 6 h, default 2 h; `SessionEvent.Length`).
It sets InProgress → Finished, the native event's end, the roll call's window and the recap. Read
it through `Length`, never a constant.

**The buttons defer** (`EventComponentHandler`, `PollComponentHandler`): every handler but those
opening a modal calls `DeferAsync` before its first read and refuses with ephemeral follow-ups. The
same goes for the giveaway buttons and the « publish » steps. Keep it that way for anything added.

**Answers close at the start.** `SessionSeats.CanRespond` refuses a click once the session is
cancelled or has started; the lifecycle pass removes the buttons up to five minutes later, and this
closes the gap. Clicking the status you already have says so instead of rewriting it.

**One rule for places**: `SessionSeats.TryParseCap` (empty or 0 unlimited, otherwise 2–67) at
creation and at edit, and an edit below the number already joined is refused (`CapBelowJoined`):
nobody is ever kicked out.

## Locks and drafts

- **`SchedulingLocks`** (singleton) holds a `KeyedLock<int>` per session and per poll: a join, a
  vote, an edit or a cancel runs one at a time per row, so two clicks on the last place can't both
  take it, and single-choice votes can't leave two options. Entries are pruned when released; a
  wait over 10 s throws (the click is refused, nothing is half-written). A unique index backs the
  vote and the participant rows anyway, and the handlers catch the `DbUpdateException`.
- **Drafts are a `DraftBox`** (30 min, refreshed on every touch) per wizard. Finishing **takes**
  the draft (`TryTake`), so a double click on « Terminer » publishes once; if the publish fails,
  the draft is put back so the person can retry.

## Fast creation

`/schedule create` with a title and a « quand » creates and posts at once; otherwise the picker
opens, pre-filled with whatever was given. « quand » is read by `SessionTime.Parse` (future only,
90 days at most; a day alone is 21:00; « dans 1h » rounded up to five minutes) and previewed by
`SessionWhenAutocompleteHandler`, whose value is the typed text.

**`Helpers/Scheduling/FrenchDate` is the one reader of a typed day and time**, for sessions, date
poll slots and `/remindme` alike: any new place where people type a date goes through it (through
`SessionTime.Parse` for anything session-shaped), never a parser of its own. It takes chat
spellings in any order (« ajd 20h », « 20h ajd », « demain soir 9h », « samedi en huit », « le 25
à 21h », « 25 déc », « 21h moins le quart », « dans 3 jours »), and **refuses rather than guesses**:
an unknown word, two times, or a weekday that disagrees with the date (« vendredi 31 octobre » when
the 31st is a Saturday) give null, and the reply quotes `FrenchDate.Examples`. A bare number after a
day is an hour (« demain 21 »), never a day of the month — that needs « le 25 ». The checks pin about
seventy chat readings: add one for every misread found in the wild.
`/when create` takes its slots the same way (`SessionTime.ParseList`, comma or line separated, ten
at most); `/vote create`'s modal takes one option per line (`PollRules.ParseOptions`). The pickers'
day lists end with « Autre date… », a modal read by the same parser.

The session picker is three steps, every one a Components V2 message:

1. **When**: one-click times (`SessionTime.QuickSlots`: tonight while ahead, tomorrow, the coming
   Saturday and Sunday, at the person's usual hour, 21:00 otherwise), « Autre jour… » for day → hour
   → minutes, or « Taper une date » (`schedule:typedate`), the same typed modal as « Autre date… »
   at the end of the day list.
2. **Name and description**, a modal, skipped when the name is already known.
3. **The preview** (`schedule:rv:*`): the real card (`SessionCards.Container`, the card without its
   buttons), then selects for category, places, duration and voice channel (clearable), and
   « Publier », « Chaque semaine », « Changer l'heure » (back to step 1, everything else kept),
   « Modifier » (the modal again), « Annuler ». Above the card, the sessions it overlaps
   (`SessionTime.Overlaps` against `GetUpcomingOrRunningAsync`), so every redraw defers first.
   Places are picked, never typed: there's nothing to refuse.

A poll's « Créer une session » lands on the same preview. **`SessionHabits`** (singleton, in
memory) holds each person's last published session per server: category, places, duration, voice
channel and hour seed the next picker; what the command was given wins, and « Chaque semaine »
is never remembered. The one-line `/schedule create` uses exactly what was typed (nothing would
show a remembered choice before the card is out), but still records its habits.

If the wizard message can't be edited into the preview (a poll's steps are not V2, and Discord may
refuse to switch a message to V2), the preview goes out as a new private message.

A card that fails to post deletes what was created for it (the session and its series, the poll),
and `CardPostFailure.Explain` names the missing permission (Voir le salon, Envoyer des messages,
Intégrer des liens).

## Polls — deadline, single choice, anonymous

- **`Poll.ClosesAt`** is `EndsAt`, or `CreatedAt` + 2 days for polls from before `EndsAt` existed.
  `/when create` and `/vote create` take a length; « Prolonger » pushes it (`PollRules.Extend`, 14
  days from now at most). `PollRules.IsOpen` is the one test: a vote past the deadline is refused
  even before the sweep closes it.
- **Single choice** replaces the voter's other vote, under the poll's lock.
- **Anonymous** hides voter names on the card, in `/report polls`' list and in her replies under the
  card (`CardReplyService`: « j'ai voté » gets `CardDoneAnonymousLines`, a rally never names who
  hasn't voted). The counts stay.
- **« Créer une session »** stores the link (`Poll.SessionEventId`); the closed card then links to
  the session, and a second click gets « existe déjà » with the link.

## The waiting list — `SessionWaitlist`

A full session's card says « Peut-être » is the waiting list, so it has to be one. When a place
opens, every « peut-être » gets a DM (`SessionSpotFreedLines`) with the card's link; the first click
on « Rejoindre » takes it. **Nobody is promoted automatically**: « peut-être » also means maybe.

- **When** is pure, in `Helpers/Scheduling/SessionSeats`: `PlaceOpened` (a button took a full
  session below its cap) and `PlaceOpenedByEdit` (the organiser raised the cap of a full session,
  or made it unlimited). An unlimited session never fills, so it never notifies.
- **A singleton**, for its per-session `CooldownGate` (10 min): someone leaving and rejoining in a
  loop can't turn it into a DM cannon. Sent after the card is redrawn, so the click never waits.
- A command set it up, so silence mode doesn't hold it back.

## `ReminderService` — one 5-minute loop, independent jobs

Weekly series, reminder DMs, the « peut-être » nudge, session lifecycle re-renders, poll
auto-close, `/tonight` expiry. Each item is guarded separately (root `CLAUDE.md`, reliability).
**The timing constants are coupled to the 5-minute interval**:

- The reminder window in `EventService.GetEventsNeedingReminderAsync` is **5–35 minutes** before
  start (`ReminderLeadMin` / `ReminderLeadMax`) — wide enough that a session created or moved 20
  minutes ahead still gets one. `ReminderSent` (reset when the time is edited) is set before the
  DMs, so none is sent twice. The DM names the organiser, the voice channel and the card's link.
- **The « peut-être » nudge** (`SessionMaybeNudgeLines`, by DM, with the card's link) goes out
  **55–70 minutes** before start (`SessionSeats.NudgeDue`), only while the session has room: on a
  full one « Peut-être » is the waiting list, and nudging it to join would be a tease.
  `MaybeNudgeSent` is set before the DMs (a crash costs the nudge, never a second one) and reset
  with `ReminderSent`. Nobody asked for it, so **silence mode holds it back**; the window then
  closes on its own.
- **Auto-close** takes the polls whose `ClosesAt` has passed (filtered in memory, root `CLAUDE.md`,
  data). Her own polls get her weekend lines (below); everyone else's get a reply under the card
  with the result (`PollClosedSlotLines`, `VoteClosedResultLines`, `PollClosedEmptyLines`; {0} the
  organiser, {1} the title, {2} the result), pinging the organiser only. A command set it up, so
  silence mode doesn't hold it back. A manual close says nothing.

## Weekly series — `RecurringSession`

`/schedule create weekly:True` stores a `RecurringSession` (day and minute **in Paris wall-clock
time**, so 21:00 stays 21:00 across the changes of time) and posts its first session. Each pass,
`ReminderService.ProcessRecurringAsync` posts the next one `LeadDays` (3) ahead
(`Recurrence.NextOccurrence`, `DueToPost`; pure, the checks cover both DST changes).

- **`LastOccurrence` is the claim**: `EventService.CreateOccurrenceAsync` creates the session and
  advances it in one save, and a unique index on (`RecurringSessionId`, `ScheduledAt`) refuses a
  second copy. The next occurrence is always after the last one, so a moved or cancelled occurrence
  never comes back. If the card fails to post, `UndoOccurrenceAsync` gives the claim back.
- Each occurrence is an ordinary session (edit, cancel, roster), linked by `RecurringSessionId`;
  its card says « 🔁 chaque jeudi ». « Arrêter la série » in the panel stops the series and keeps
  the sessions already posted. A series whose channel is gone stops itself; a guild that is missing
  (disconnected) is skipped. The series runs only while the gateway is connected.

## Roll call — `SessionAttendanceService`

She calls the roll for **Game and Movie** sessions only (an Activity or Other can happen offline).
Timing rules are pure, in `Helpers/Scheduling/SessionAttendance`.

- **Fed by `VoiceXpService`'s sweep**, once per guild per minute, with every human connected to a
  voice channel (AFK aside, muted included). A session's voice channel is optional and only shown
  (the native event stays External), so "present" means any of the server's voice channels.
- **What it learns is stored**: `Participant.FirstSeenInVoiceAt` (first sighting from start − 15 min
  to start + `Length`), `SessionEvent.LateCallSent` / `RecapSent`. Each flag is written **before**
  its message goes out: a crash costs the line, never a second ping. `UpdateEventAsync` resets all
  three when the time moves, as it does `ReminderSent`.
- **Late call** at start + 10 min, until start + 30 (later, it's skipped, never sent stale): pings
  the Joined people not seen yet, `AllowedMentions` narrowed to them. It waits for the first
  arrival — with nobody there, the session may be happening somewhere else.
- **Recap** once fewer than two of the people who came have been in voice for 15 min (not before
  start + 30), or at start + `Length` + 2 h (`SessionAttendance.RecapAfterEnd`). **Absences are a count, never names** (the owner's
  call; `/report sessions`, asked by staff, is the one place that names them, also his call); the people who came and who were late (first seen after start + 10) are named, inert.
  Silent when nobody came or fewer than two were Joined. Headers gender nobody (« Au
  rendez-vous », « Absences »).
- The session list is cached and re-read every 5 minutes (recapped sessions included, until their
  window closes); the scatter clock is in memory, so a restart only delays a recap.
- **Voice channel status**: while a session is live, the voice channel holding most of its Joined
  people shows « 🎮 Title » (the category's emoji) under its name, cleared when the live window ends.
  Stateless against Discord, so restart-safe: a status equal to one she would write for a cached
  session is hers to clear; anything else is someone's and is never overwritten. Needs Set Voice
  Channel Status; a channel she can't set is left alone for 30 min.
- She also **sits in voice before the start** (`VoiceSeatService`, see `personality/ambient.md`, *Her seat in
  voice*); the late call then uses `SessionLateCallSeatedLines` ({2} = the start time).
- **One no-show rule**: `SessionAttendance.IsMissed` (Joined, roll called — `RecapSent` — and never
  seen) and `CameLate`. `/recap`, her chat facts and `/report sessions` all count through them, so
  sessions from before attendance existed never read as no-shows anywhere, and a « peut-être » is
  never absent. Change the rule there, not at a call site.

## Weekend poll — `WeekendPollService`

Every Wednesday at a minute drawn from the week between 18:00 and 20:00 (`Helpers/Scheduling/WeekendPoll`),
an ordinary date poll in her main channel (home guild): Friday, Saturday and Sunday at 21:00,
organizer = her id. Posted Wednesday so the default two days close it Friday evening, before the
first slot; nothing is posted after 22:00.

- **Her line goes first, as its own message** (a V2 card carries no content); `HerMessages`
  remembers that one, and the card follows.
- **She stands down** when a session is already planned that weekend, a weekly series falls on it
  (its occurrence may not be posted yet), or someone's date poll is still open. **Once a week**:
  the claim is the poll itself (one of hers in the last six days), so no state file. A card that
  fails to post deletes its poll and her line.
- **On auto-close** `ReminderService` replies to the card with the winning evening(s)
  (`WeekendPollResultLines`) or sulks (`WeekendPollDeadLines`). A manual close says nothing.
- **Permission exception** in `PollModule.OnToSessionAsync`: on *her* poll, anyone who voted for a
  winning slot may create the session (and becomes its organizer); everyone else's polls stay with
  their organizer and staff. A second voter clicking after a session exists at that time is told
  so instead of creating a duplicate. Her poll's title is a question, so it doesn't pre-fill the
  session name.

## Replies under a card, and reposting

A reply under a poll, vote, session, `/tonight` or giveaway card is answered about the card by
`CardReplyService` (rallying, « j'ai voté », « c'est quand ? », answers typed instead of clicked,
« remonte-le »): see `personality/listening.md`, *Under her cards*. It recognises a card by its buttons'
custom-ids, so **the `poll:vote|close|tosession|manage|mine`, `event:*`, `tonight:*` and
`giveaway:enter|leave` prefixes, each with the row id third, are a contract** with
`Helpers/Scheduling/CardRefs`.

**`CardRepost` is the one way a card moves**: the `/… list` republish menus, « Republier » in the
panel, « remonte-le » and `CardUpdater`'s fallback all go through it (delete the old card, redraw through the static builders, store the new location,
repoint a session's native event). A new card kind that can be republished goes there.

## The two wizards keep state differently

**`ScheduleModule` keeps the session in a `DraftBox`** keyed by user id, from the command to
« Publier », which takes it. Only « Autre… »'s date travels in custom-ids
(`schedule:hour:{date}`, `schedule:min:{date}:{hour}:{minute}`, `schedule:back:hour:{date}`), so
its `Retour` buttons can rebuild the step before; **instants travel as unix seconds**
(`schedule:quick:{unix}`, `schedule:frompoll:{pollId}:{category}:{unix}`), never as wall-clock
strings. A poll's session starts from a fresh draft, so options left over from an earlier
`/schedule create` never leak into it. A typed date that doesn't read keeps the draft, and
« Corriger » (`schedule:retrytyped`) reopens the modal pre-filled.

**`PollModule` and `VoteModule` keep the whole draft in a `DraftBox`** keyed by user id (a
variable-length list doesn't fit a custom-id), taken on finish, removed on cancel.

**`/vote create` must create its own wizard message.** The command posts « Définir le vote » and
every later step only *updates* it; creating it from the modal response drops the first update.
That's why the `vote:begin` button exists. `poll:typedslot` (the date poll's « Autre date… » modal)
updates the wizard message from a modal the same way: if it ever drops an update, it needs the same
fix.

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

## `/tonight` — « qui est chaud ce soir ? »

`TonightModule` (command + static card), `TonightComponentHandler` (buttons), `TonightService`
(EF). Stored in `TonightCall` / `TonightResponse` (`TonightStatus` is stored as int — append-only).

- **One open call per guild**; a second `/tonight` gets the link. The organizer is « In » from the
  start. Buttons add or switch an answer, never toggle; they carry **no emotes** (the owner's call).
- **Threshold ping, once**: when « chauds » reach the minimum, `ClaimThresholdAsync` flips the flag
  in SQL *before* the reply goes out, pinging only them.
- **« Créer la session »** (organizer or staff, `SessionPermissions.CanManage(IUser, TonightCall)`)
  claims the close first (`ClaimCloseAsync`, so a double click or the midnight sweep can't make two),
  creates a Game `SessionEvent` at the call's hour, signs up In → Joined and Maybe → Maybe, and posts
  the session card through `SessionCards`. If creating the session or posting its card throws, the
  session is deleted and the claim given back (`ReopenAsync`). No native event: that stays the organizer's opt-in on the card.
- **Expiry** is `ReminderService`'s fourth job: midnight Paris, or an hour after the start for a
  late-night call. Claimed before the card is redrawn.
- Her line on the card is chosen by the call's id (`StableRoll`), not the picker: the card is
  redrawn on every click and must keep saying the same thing.

## `/teams`

Stateless: « Remélanger » carries `teams:roll:{channel}:{count}:{mode}` and re-reads the voice
channel. Humans only (her seat and other bots are skipped). `TeamDraw` deals a shuffle round-robin
(sizes differ by one at most); **balanced** keeps the most even of 400 random deals by level, not a
snake draft — a snake draft is deterministic, so a reroll would deal the same teams.

## `/remindme` and `/reminders` — `PersonalReminderService`

Its **own 1-minute loop** (five minutes late reads as broken, so it can't ride `ReminderService`).
`ReminderTime` parses « quand » (pure), its durations itself and its days and times through
`FrenchDate`: « 21h » / « 1h30 » alone are **clock times**, the way French
says « à 21h »; a duration in hours needs « dans » or a spelled unit, and minutes after an hour stop
at 59 (« 21h75 » is refused, never read as a duration). « ce soir 9h » is 21:00, « ce soir » alone
20:00. The confirmation shows the exact time either way. The checks pin about ninety readings: add
one for every misread found in the wild.

- The row is claimed (`Sent`) before sending and deleted after: a crash costs the reminder, never a
  second copy. Per-item `try`, like every sweep.
- DM first (`AllowedMentions.None`); on `CannotSendMessageToUser`, a ping in the channel where it was
  asked, narrowed to that person. Asked in a DM (`GuildId` 0), there's nowhere to fall back to.
- More than 10 minutes late (she was down), the line says so (`ReminderLateLines`).
- 15 pending per person; 1 minute to 60 days ahead. No guild guard on either command.
