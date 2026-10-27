# Sessions & polls overhaul — solid, fast, beautiful

## Context

The owner wants the scheduling side of SYNCS (`/schedule`, `/poll`, `/vote`) to be very
user-friendly, good-looking and solid. Today:
- Session, poll and vote cards are classic embeds with grey buttons.
- `/schedule create` is a 5-step picker that stops 24 days out.
- A date poll costs about 4 clicks per slot. A vote costs one modal per option.
- Every poll closes 2 days after it was created.
- Reading the code turned up real bugs: the last place can be over-filled, a double-click can crash a handler, a failed post leaves an orphan row, buttons stay live after the start, a moved card can freeze, and reminders can be lost or sent twice.

The owner chose all four areas: solid first, faster creation, better-looking cards, smarter polls. They also chose **several releases**, a **Rich Components V2 card**, **weekly recurring sessions**, and all four poll options: custom deadline, single choice, anonymous, and editing a live poll.

**Starting point:** the uncommitted WIP is already scheduling work and stays as it is:
- `SessionWaitlist` and `Helpers/Scheduling/SessionSeats`, with the `MaybeNudgeSent` migration.
- Defer-first on the card buttons, and name caps on the cards.
- Checks for these in `tests/ProjectSYNCS.Checks`.

Release 1 extends that batch. The owner commits by hand, so check `git diff ProjectSYNCS/config.yaml` before bumping, per CLAUDE.md.

Read `docs/agents/scheduling.md` before each release, and update it in the same release. Command and option names are English; every user string is French; lines go through `ResponsePicker` and follow `docs/syncs-voice.md`.

---

## Release 1 — Solid (minor: WIP migration and waitlist behaviour)

Pure rules go in `Helpers/Scheduling/`, each with cases in `tests/ProjectSYNCS.Checks`.

1. **Per-row lock:** add `Helpers/Core/KeyedLock<TKey>`, a `SemaphoreSlim` per key, pruned when idle. Grep first for an existing one. The bot is a single process, so an in-process lock is enough. It lives in a singleton `SchedulingLocks` service (per CLAUDE.md, singletons own state).
   - Session joins take the session's lock around *check capacity → upsert → reload*. That ends over-filling.
   - Poll votes take the poll's lock around *check `IsClosed` → toggle → reload*. That ends late votes and out-of-order card edits, because the card edit also happens inside the lock.
   - Both also catch the unique-index `DbUpdateException` and treat it as « déjà fait ». A double-click then never crashes.
2. **Join rule:** `SessionRoster`/`SessionSeats` gets a `CanRespond(session, now)` that refuses after the start (or after cancel), with a French reply. It closes the 5-minute gap before the lifecycle pass removes the buttons.
3. **No orphans:**
   - `/schedule create`, the poll and vote Terminer, and `/tonight` → session delete the row if the card fails to post, then explain why, e.g. a missing permission in the channel. `WeekendPollService` already does this (`:114-119`).
   - Terminer claims the draft with `TryRemove` *first* and puts it back on failure, so a double-click publishes once.
4. **Edits:**
   - Re-check `IsCancelled` on edit submit.
   - Refuse a cap below the number joined.
   - One validation rule for create and edit: empty or 0 means unlimited, otherwise 2–67.
   - `UpdateEventAsync` resets `RenderedPhase` when the time moves, so a moved card can't freeze.
5. **Cancel:**
   - Annuler now asks first, with an ephemeral « Oui, annuler » / « Non » prompt.
   - The card edit in `/schedule cancel` is wrapped in a try/catch, so the DMs and the native-event delete always run.
   - `SessionNotifier` gets a logger at every call site.
6. **Reminders** (`ReminderService` / `EventService.GetEventsNeedingReminderAsync`):
   - Claim `ReminderSent` *before* sending, the project's crash rule.
   - Widen the window to 5–35 min, so a reminder missed while she was down still goes out late instead of never.
   - The DM links to the card.
7. **Drafts and custom-ids:**
   - Wizard drafts (`_draftTitles`, the poll and vote drafts, `_toSessionTitles`) carry a timestamp and are pruned, 30 min old, on each access.
   - The wizard passes times as **unix seconds**, not wall-clock strings, in `schedule:finalize:*` and `poll:catpick:*`. This fixes the DST ambiguity and the fragile `:` wildcard. Grep every builder and handler, since custom-ids are a contract.
8. **One category table:** `Helpers/Scheduling/SessionCategory` (emoji, label, colour) replaces the four copied switches (`ScheduleModule:274,546`, `SessionEventSync:23`, `ReminderService:381`).
9. **Sanitize text:** run poll and vote titles and labels through `Format.Sanitize`, on the cards and in `/… list`.

---

## Release 2 — Components V2 cards (minor)

**One render path per card kind.** New static builders `Commands/Scheduling/SessionCards.cs` and `PollCards.cs` expose:
- `Build(…) → MessageComponent`, Context-free so they can be measured.
- `ApplyTo(MessageProperties)`, which sets components, `MessageFlags.ComponentsV2`, `Embeds = []`, `Content = ""` and `AllowedMentions.None`.
- `SendAsync(IMessageChannel)`.

Every call site listed by the grep goes through these: `ScheduleModule`, `EventComponentHandler`, `TonightComponentHandler`, `PollModule`, `PollComponentHandler`, `VoteModule`, `ReminderService`, `CardRepost`, `WeekendPollService`. Once they do, the old `BuildEventEmbed`/`BuildPollEmbed` builders are removed.

**Session card**, following the preview the owner picked:
- A container with an accent colour per state. The free/full/live/finished/cancelled colours stay.
- A header section: category emoji and title, « par @organisateur », and the organiser's avatar as accessory via `Helpers/Core/AvatarUi`.
- After a separator: date (`<t:F>` plus `<t:R>`), duration, voice channel and description. The last three are filled from release 3.
- After a separator: « Inscrits n/max » with a fill bar (`▰▱`; add `TextChart.Bar(part, total, width)`), the joined mentions capped by `SessionSeats.MaxCardNames`, and the Peut-être and Absents counts plus names.
- Rows: [Rejoindre (success)] [Peut-être] [Refuser (danger)], then [⚙️ Gérer] [Événement Discord].
- Live, finished and cancelled states drop the action rows, as today.

**Poll and vote card:**
- One line per option: label, a `TextChart.Bar` bar, count and %.
- Voter names (`MaxPollVoterNames`), shown unless the poll is anonymous (release 3).
- The leader is highlighted.
- Total voters, and « se termine <t:R> ».
- Option buttons; [Mes votes] (ephemeral, what *you* picked); [⚙️ Gérer].

**« Gérer » panel**, ephemeral, after `DeferAsync`:
- `SessionPermissions.CanManage` is checked there. Everyone else is told it's not theirs, so Modifier and Annuler leave the public card.
- Sessions: Modifier, Annuler (with the release 1 confirmation), and repost here.
- Polls: Clôturer, then release 3's tools.
- The panel caches a snapshot of the row in a singleton, short TTL, keyed by (user, id). « Modifier » can then open its modal **without a DB read**, inside 3 s, falling back to a read when the snapshot is gone.

**Contracts to keep:**
- The `event:*` / `poll:vote|close|tosession` custom-id prefixes, with the row id third, are what `Helpers/Scheduling/CardRefs` recognises. Add `event:manage`, `poll:manage` and `poll:mine` there, so a closed card without `poll:close` is still recognised.
- Every button row keeps its own verb.

**Limits:** measure the worst case, a 10-option anonymous-off poll and a full 67-place session, against **40 components** and the text limits. Add a check for each in `tests/ProjectSYNCS.Checks`.

**Legacy embed cards** already in channels are converted on their next update by `ApplyTo`, which clears embeds and content. Verify on the dev guild that Discord accepts adding the V2 flag on edit. If it refuses, `ApplyTo`'s caller falls back to `CardRepost`.

---

## Release 3 — Faster creation & smarter polls (minor, migration)

**Migration** `SchedulingOverhaul` (schema only):
- `SessionEvent`: `DurationMinutes` (default 120), `Description`, `VoiceChannelId` (`HasConversion<long>`).
- `Poll`: `EndsAt` (nullable; null = `CreatedAt + PollLifetime`, so old rows keep working), `SingleChoice`, `Anonymous`, `SessionEventId` (nullable).
- Grep every use of the static `SessionEvent.Duration` (attendance, lifecycle, native event, voice status) and move them to the per-session value.

**Typed dates:**
- New `Helpers/Scheduling/SessionTime.Parse(text, now)` wraps `ReminderTime`'s absolute parser with session defaults: a day without a time means 21:00, the horizon is 90 days, only future times. This means `ReminderTime`'s `DefaultHour` becomes a parameter; its own behaviour doesn't change.
- Checks cover « samedi 21h », « 25/12 20:30 », « demain », « ce soir ».

**`/schedule create` fast path** — all options are optional, so the current picker stays for anyone who likes it:
- The options are `title`, `when` (autocomplete previews the parsed date: « sam. 1 nov. à 21:00 »), `category`, `places`, `duration` (choices 30 min to 6 h), `voice` (voice channel), `description` (`[MaxLength(InputCaps.Description)]`), and `weekly` (release 4).
- With `title` and `when` set, the session is created at once, after a defer. Otherwise the picker opens pre-filled.
- The picker's day select gets « Autre date… », which opens a typed-date modal, so the 24-day wall is gone.

**Ids:** autocomplete on `/schedule edit|cancel event-id` and `/poll|/vote delete` (`Interactions/Autocomplete/`): upcoming or open items the user can manage, shown with title and date.

**Edit modal:** gets the duration and description. Date and time become one « Quand » field parsed by `SessionTime`.

**`/poll create` fast path:**
- `title`, `slots` (« ven 21h, sam 21h, dim 18h », comma-separated, each through `SessionTime`; a slot that fails to parse is named in the reply), `ends` (choices 1 h to 7 days, default 2 days), `single`, `anonymous`.
- Without `slots`, the wizard runs as today, and its review step can now remove a slot.

**`/vote create`:** one modal with title and a paragraph field, « une option par ligne » (2–10 options, `maxLength`), plus the same `ends` / `single` / `anonymous` options.
- The `vote:begin` button rule in `scheduling.md` still holds for the message the wizard creates.

**Poll behaviour:**
- **Single choice:** `PollService` clears the voter's other option inside the poll lock, so clicking another option moves the vote.
- **Anonymous:**
  - The card shows bars and counts only. « Mes votes » still works.
  - Check `PollReport`, `/report polls` and `CardReplyService`'s « j'ai voté » so none of them name voters on an anonymous poll.
- **Deadline:** auto-close reads `EndsAt`. The Gérer panel can **extend** it (+1 h / +1 j / +2 j), **rename** it, **add an option** (modal) and **remove an option** (select, with a confirmation when it has votes).
- **Close announcement:** every poll and vote, not just hers, gets a reply under its card with the winner(s). It pings only the organiser (`AllowedMentions` narrowed to them). Hers keeps `WeekendPollResultLines`; a manual close stays silent, as today.
- **Poll → session link:** set `Poll.SessionEventId`. The card then shows a link to the session instead of « Créer une session », and a second click is told the session exists. This generalises the weekend-poll guard at `PollModule.cs:320`.

**Reminders:** the organiser also gets the 30-minute reminder.

**Docs:** `/help` (measure; it is close to 6000) and `README.md` cover the new options.

---

## Release 4 — Weekly recurring sessions (minor, migration)

- **Model:** `RecurringSession` holds guild, channel, organiser, title, category, `DayOfWeek` (stored as int, append-only), Paris wall-clock time, places, duration, voice, description, `LeadDays` (default 3) and `Active`.
  - `SessionEvent.RecurringSessionId` is nullable.
  - A unique index on (`RecurringSessionId`, `ScheduledAt`) makes posting crash-safe: a second insert fails rather than duplicating.
- **Recurrence rule:** `Helpers/Scheduling/Recurrence.NextOccurrence(template, now)` is pure and DST-safe through `AppTime`, with checks across the late-October and late-March changes.
- **Posting:** a fifth independent job in `ReminderService`'s 5-minute loop, with a per-item `try`. Once the next occurrence is within `LeadDays` and no session exists for it, she creates the session and posts the card through `SessionCards.SendAsync`.
  - A channel that is gone deactivates the template and logs it.
  - A command set this up, so silence mode doesn't apply.
- **UX:**
  - `/schedule create … weekly:true` creates the template and the first session.
  - The card shows « 🔁 chaque jeudi ».
  - Gérer offers « Arrêter la série », and « Annuler juste celle-ci » (the normal cancel).
  - `/schedule list` shows active series.
  - No new subcommands, so the 25 limit is untouched.
- **`WeekendPollService`:** already stands down when a session is planned that weekend, so recurring sessions count with no change. Verify this.

---

## Per-release checklist

- Run `dotnet build` (`net10.0`, no new warnings).
- Run `dotnet run --project tests/ProjectSYNCS.Checks`; it must pass, with the new cases.
- Bump `config.yaml` once per batch and add a `CHANGELOG.md` entry, in English, with no member names.
- Update `docs/agents/scheduling.md`, plus `/help` and `README.md` for user-facing commands.
- **Dev guild run**, with the dev token and guild id in user-secrets:
  - **Release 1:**
    - Two accounts click Rejoindre on the last place: only one gets it.
    - A double-click on Rejoindre, an option and Terminer: no crash, nothing duplicated.
    - Post into a channel with Send Messages denied: no orphan row.
    - Move a finished session forward: the card comes back to life.
    - Cancel asks for confirmation.
  - **Release 2:**
    - A legacy embed card converts on its next click.
    - The worst-case cards render.
    - Gérer is refused to a non-organiser.
    - Modifier opens its modal instantly.
  - **Release 3:**
    - `/schedule create title:… when:"samedi 21h"` in one shot.
    - A poll from the `slots` text.
    - A single-choice vote moves.
    - The anonymous card shows no names, in the card, in `/report polls`, or in the « j'ai voté » reply.
    - Extend, add and remove an option.
    - Poll → session shows the link, and a second click is refused.
  - **Release 4:** a weekly template posts once inside `LeadDays`, survives a restart without duplicating, and Arrêter la série stops it.
