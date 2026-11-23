# `/report` and the monthly digest

Part of `docs/agents/admin.md`: the gate and the guides are there.

- **Posted in the open, with every name** (no-shows, latecomers, the busiest, the ones going
  quiet, the sleepers): the owner's call, and the one exception to « absences are a count, never
  names » (scheduling, *Roll call*). Only the refusals are private. Every card still takes `names`:
  `false` drops every naming field (the checks assert no `<@` survives), for any future place that
  must not name; nothing passes `false` today.
- **Every person goes through `ReportCards.Who`**: `**Name** (<@id>)`, never a bare mention. A
  mention in an embed renders only for a reader whose app already has that member loaded, which in
  the digest's DMs is rarely the case for the sleepers, so they came out as raw ids. The name comes
  from `ReportService.NamesIn` (the guild member, else the cached user, else the mention alone),
  is sanitized, and the checks measure every card with the longest escaped names.
- **Her line on top of the card**, as on `/health`: `ReportCards.WithHeadline` puts it first in the
  description (left out rather than pass 4096 / 6000). `ReportCards.DutyPool` picks the pool by who
  asked or clicked: `OwnerReportDutyLines` (Papa, on duty), `TataReportLines` (warm, the mute
  rights), `StaffReportLines` (any other staff member: suspicious, « pourquoi tu veux savoir
  ça ? »). A period or 30/60/90 click re-picks for the person who clicked, since the card is public
  and any staff member may change it. The digest has no line: its own opens the DM.
- **One path**: `ReportService.BuildAsync(kind, guild, window, names)` serves the command, the
  period buttons (`report:win:{kind}:{period}:0`, `ReportComponentHandler`, deferred first,
  checks `IsStaff` on every click, since anyone can see the buttons) and the digest. `ReportKind`
  names are in custom-ids: append only, never rename.
- **The live guild becomes plain records in `ReportService`** (`ChannelInfo`, `ServerEmote`,
  `MemberSnapshot`, the registered slash paths) before it reaches a helper. When the guild is
  unavailable (gateway down, the digest firing during a reconnect) those snapshots are empty and
  the reports render without their channel, emote or member lists: never a crash.
- **Windows are `Helpers/Core/ReportWindow`**: inclusive Paris day keys. Bucket tables filter
  `Day` in SQL; DateTimeOffset rows (sessions, polls, `/tonight`, outages) are read whole per guild
  and windowed in memory. Opens on 30 days; the buttons are `StatsPeriodUi`'s three.
  - **« Depuis toujours » reads the daily buckets from day 0, not the totals tables**, unlike
    every leaderboard: a report needs each day's shape, which totals don't have. So it covers only
    what was counted since each table's buckets existed, and its label says « depuis le début des
    relevés ».
  - Trends compare against `ReportWindow.Previous`: the same number of days before, the previous
    calendar month for the digest, none for all time.
  - **A trend is per counted day, and a partial window says so.** A counter that started inside a
    window would otherwise make the next one look like growth (13 days of commands in October
    against 30 in November reads +130 %). `Compared.FirstDay` is the counter's first row, read by each
    service (null for the reports not read from daily buckets, which then only even out 30 against 31
    days); `ReportWindow.Pace` turns it into the days on each side, for `TextChart.Trend`, and
    `ReportCards.PartialNote` / `DaysCounted` print it. A quiet day after the first row counts: nothing
    happening is a real zero. **Distinct counts stay raw** (active members, people), since they don't
    grow with the days. A report over several counters carries each one's first day (`SyncsReportData`).
- **The arithmetic is pure** (`SessionReport`, `PollReport`, `CommunityReport`, `SyncsReport`,
  `ActivityReport`, `CommandReport`, `QuizReport`, `PlynlingReport`, `EmoteReport`,
  `SleepingReport`, `MinecraftReport` in `Helpers/`), the services only fetch rows, the cards (`ReportCards`) are
  static. The checks feed the helpers cases and measure every card at its worst; re-run them after
  adding a field. A list that can grow without bound (silent channels, unused emotes, never-used
  commands) goes through `ReportCards.Fit`, which keeps whole items only and counts the rest
  (« … et N autres »), so an emote's `<:name:id>` is never cut in half.
- **Rules worth knowing**:
  - a session belongs to the day of its `ScheduledAt`, and so does a cancellation (there is no
    `CancelledAt`);
  - attendance goes through `SessionAttendance.IsMissed` / `CameLate` (scheduling, *Roll call*);
  - a poll « became a session » when a non-cancelled session sits on one of its slots: nothing
    links them, it is the match `PollModule` uses against duplicates;
  - « active » is any XP, reaction or voice minute that day (messages are counted per channel and
    hour, never per person);
  - `community` ranks the busiest three ways, since long calls can outweigh a lot of writing in XP:
    all XP, `CommunityReport.ChatXp` and voice minutes. Chat XP is derived per day, the day's XP minus
    what `VoiceXpCurve` pays for its minutes, so it needs no column and covers past months. It relies
    on `VoiceXpCurve` staying as it is: **a change to the tiers misreads every earlier day** unless
    the old rates are kept for them. It also holds reactions, verdict bonuses and `/admin xp`. Each
    person's share is of that list's own total, never of the server's XP;
  - uptime is measured from the first day of the window the Pi recorded its temperature, and always
    shows that span (« sur 23 jours, depuis le 09/10 »): a percentage alone hides a short measure;
  - `commands` leaves `/debug` out of the totals, the ranking and the never-used lists (the owner testing
    isn't the server's use; its count is a line apart), but a failing `/debug` still reaches « À regarder ».
    « Pas utilisées » (over the window, not ever) lists the members' commands first, then the staff's (`CommandPath.IsStaff`:
    `/admin`, `/config`, `/report`), so the real signal isn't buried alphabetically under theirs;
  - **Reach** in `commands` is different people, never names (`CommandMemberDailyStat`, one row per
    guild, day, command and person, written through `ActivityCounter`, from 7.56.0 on). A shortcut is
    folded into the command it stands for (`CommandPath.Reached`, so a new `ShortcutKind` needs a line
    there); `/debug` is never written;
  - a quiz round counts only if its card went out (`MessageId != 0`), in `quiz` and `community`
    alike;
  - `plynlings` never counts the mascot (`PlynlingMascot.Is`), and cannot report abandons: an
    abandoned Plynling is hard-deleted with its history. Needs are settled in memory
    (`AsNoTracking`, never saved), as `/admin stats` does;
  - `emotes`: an emote is the server's own when its id is in the live guild's list; the others
    were counted from other servers (`EmoteTracker` counts them too). « Pas utilisées » is over the
    window, not ever. Bots' messages and reactions aren't counted;
  - `activity`: « silencieux » and « inutilisés » only list open channels (`ChannelInfo.Open`,
    `ReportService.ChannelsAsync`): she can see it and a non-staff member may write in or join it. A
    read-only rules channel, a bot's own channel or a staff room staying quiet says nothing. The busiest
    weekday is messages per occurrence of that weekday, since a period can hold more of some;
  - `sleeping` has no window: a silence of 30, 60 or 90 days, on its own buttons
    (`report:sleep:{days}`, its own verb so it never collides with a period row). « Last seen » is
    the last `MemberDailyStat` day with any counter above 0; « jamais » means never since
    2026-08-09, when the XP buckets became reliable (`ResetXpTotals`). Members who joined less than
    N days ago are left out, bots always. Activity in excluded channels earns nothing, so someone
    who only talks there looks asleep (said in the footer). The most recent silences are listed first,
    one per line; the never-active share one field, so they can't push the others out;
  - `minecraft` reads `MinecraftDailyStat`, the daily buckets beside her all-time `MinecraftStat`
    (same `McStats` keys, written in the same save by `MinecraftDataService.AddStatsAsync`, from
    7.50.0 on). Player names are her in-game ones, sanitized, shown only with `names`. A window
    she didn't play says so (`MinecraftReportData.Played`: any minute in game, or any order).
- **Incidents** (`incidents`, `IncidentDailyStat`, `IncidentReport`): what `commands`' error count can't
  see. Buckets only, not per guild, from 7.56.0 on; `IncidentKinds` keys are stored, so append-only.
  - **Failed clicks and forms**: `BotService` on `ComponentCommandExecuted` / `ModalCommandExecuted`,
    under the custom-id's verb (`CommandPath.Verb`). Uses aren't counted, only failures; a click nothing
    handles any more (a button on an old message) is one.
  - **Late answers**: `BotService.WatchAnswerAsync` looks at `HasResponded` 3 s after every interaction
    but autocompletes. It catches what a handler's own success hides: the work saved while the click
    looks dead.
  - **Log lines**: `IncidentLogProvider` counts every warning and error by logger. It wraps the one
    `ActivityCounter`, so `Program.cs` builds that counter before the container: logging exists first.
    Lines are not faults, one failure can write several, so its alert's bar is high. `SQLITE_BUSY` is
    counted once per failed command, from EF's `Database.*` loggers only.
  - **Restarts and lost connections**: `PiHealthService`. A restart is counted under how the run before
    it stopped, updates included, since each wipes her in-memory state.
  - The flush writes them apart from the guild batch, in its own `try` (`DrainIncidents`).
- **Her disk, memory and power** (`PiBodyDay`, `PiBodyReport`, a field in `syncs`): what `/health` reads
  live, kept per day by `PiHealthService`'s tick, so a slow leak or a filling disk shows before it
  breaks anything (only `/data` persists). Sizes keep the day's last reading, peaks the worst. The disk's
  pace needs `MinDaysForPace` days and is shown within `DiskFullShownDays`.
- **Joins and leaves** (`MemberFlowDailyStat`, `MemberFlowTracker` on `UserJoined` / `UserLeft`,
  bots skipped): daily buckets with **no totals table**, counted from 7.5.0 on. Nothing needs an
  all-time figure older than the buckets, and Discord shows the member count.
- **Activity counting** (`activity`, `commands`, and the per-message counts of other subsystems:
  emotes, reactions used, her chat answers): the one batched counter. A database write per message
  or reaction is what fills a Pi with no `SQLITE_BUSY` handling, so the counts go to
  `ActivityCounter` (a singleton holding its own lock, every method synchronous and cheap) and
  `ActivityFlushService` (its own 5-minute loop) writes them.
  - Fed by the message fan-out right after the shortcut return (a typed shortcut is counted as a
    command, under its `;` key, by `PlynlingShortcutService`), by `SlashCommandExecuted` (handlers
    run `RunMode.Async`, so success or failure only arrives there; DMs count under guild 0), and by
    `VoiceXpService`'s sweep.
  - The flush opens one scope per guild in its own `try`; a failed guild's counts are put back
    (`Restore`) for the next pass. It flushes once more on stop, and is registered before
    `BotService` so it stops after everything that feeds it. A crash loses at most 5 minutes.
  - `ActivityHourStat`, `ChannelDailyStat`, `CommandDailyStat`: buckets only, no totals, counted
    from 7.6.0 on. A thread counts under its parent. Excluded channels are counted and marked 🚫.
    Voice is presence (everyone connected, muted included, AFK room skipped), unlike
    `MemberDailyStat.VoiceMinutes`, which counts XP-eligible minutes only.
  - `CommandDailyStat.Command` is `CommandPath`'s key (`/pl` folded into `plynling`), a stored
    string, so append-only: a renamed command keeps its old rows under its old name.
- **The monthly digest** (`MonthlyDigestService`, its own hourly loop): from 09:00 Paris on the
  1st, two digests for the previous calendar month, by DM: Papa's (`OwnerId`) and Tata's
  (`BotResponses.TataId`, the server admin, not technical). Each is:
  - **an opening line plus « Le mois en bref »** in one message: a few figures and « À regarder »
    (`ReportService.BuildBriefAsync`, `ReportCards.BuildOwnerBrief` / `BuildTataBrief`). Papa's
    figures are his choice: commands (uses, people, errors, never used), incidents, the Pi (with the base and the disk), messages without a
    specific answer, the quiz, her hours in Minecraft (« Pas joué ce mois-ci. » for none). Tata's: members (active, joins, leaves, sleepers), sessions
    (attendance, the best-attended weekday), when the server talks, silent channels, unused emotes,
    the three most active, then who leads in chat and in voice;
  - **then the reports of `ReportKinds.Digest(recipient)`**, one embed per message (several in one
    could pass 6000), each in its own `try`: Papa `syncs`, `commands`, `incidents`, `quiz`, `minecraft` (left out
    for a month she didn't play: `ReportService.IsEmptyAsync`, the one report skipped when empty);
    Tata `community`, `sessions`, `activity`, `sleeping`. Papa also gets the chat-answer counts over 30 days (as
    `/debug answers`: they are kept by rolling period) and the unanswered messages of the same 30
    rolling days as a file (`ChatAnswerService.GetUnansweredAsync`).
  - **« À regarder » is `Helpers/Admin/DigestBrief`**, pure thresholds (constants there): a
    command failing ≥ 2 times and > 5 % of its uses, power cuts, crashes, a heat record, the
    unanswered share up ≥ 5 points, « difficile » found ≥ 80 % or « facile » < 30 % (≥ 5
    questions), ≥ 3 failed clicks or forms, ≥ 5 late answers, ≥ 20 logged errors, any `SQLITE_BUSY`, the
    disk under 15 % or full within 90 days, memory under 10 %, any under-voltage; for Tata, active members down ≥ 20 %, more leaves than joins (from 2 leaves), ≥ 3 members last seen the month before the report (new silences only: the never-active would raise it every month), no session held with ≥ 10 h in voice. **A
    comparison is only made when the month before has data**, so a new counter's first month
    raises nothing. Nothing to flag shows a `DigestQuietLines` line.
  - Lines: `OwnerDigestLines`, `TataDigestLines` ({0} the month), `DigestQuietLines`, all picked by
    `DailyRotation`, since a once-a-month pool would gain nothing from the picker.
  - Timing is pure, in `Helpers/Admin/MonthlyDigest`.
  - `report-state.json` (beside the DB) holds the last month sent **to each** (`LastMonth` for
    Papa, a name kept from before; `TataLastMonth`), written **before** sending. They are claimed
    and sent apart, so Tata's closed DMs never cost Papa his digest.
  - No file, or no field yet for a recipient, means the previous month is marked sent without
    sending, so a deploy never DMs a surprise.
  - `/debug digest [version]` sends last month's now, Papa's or a preview of Tata's, **to Papa**,
    leaving the state alone.
