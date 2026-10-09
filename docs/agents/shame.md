# `/shame` — the wall of shame

## Data

`ShameRecord` / `ShameDailyStat` is a totals+buckets pair, one row per (guild, user) because the
counters are never read apart. `MeanHits` (*Le Malfaisant*), `PerfidyHits` (*Le Perfide*),
`ShoutHits` (*L'Hystérique*) and `AbandonHits` (*L'Indigne*) are things you *did*; `BanVotes`
(*Le Banni*) is done *to* you. `ShameService.TryVoteAsync` checks the limit and writes both
counters in one `SaveChanges`.

## Voting

**Staff-only, and the daily cap is on the target, not the voter.** Anyone may open the wall; only
`SessionPermissions.IsStaff`, a name in `ShameModule.ExtraVoters`, someone added with
`/config shame-voters`, or the `/config` moderator role may vote (`CanVoteAsync` checks staff and
`ExtraVoters` before asking the database). Restricting
*who* makes it a deterrent rather than a game; the thing to prevent is a dogpile, not a moderator
voting twice.

**The cap needs no state**: `ShameService.MaxVotesPerTargetPerDay` (2) is checked against
`ShameDailyStat.BanVotes`, so it can't drift from what the wall shows and survives restarts. Don't
reintroduce per-voter rationing (`LastVoteDay` was dropped in `DropShameLastVoteDay`).

## The counters — rationing differs on purpose; don't harmonise

`ShameTracker` sees every message (it can't be a branch in `ChatterService`, which returns early in
a dozen places) and short-circuits on `ReadFeedback` like the others.

**Le Malfaisant — targeted hostility is uncapped.** One hit per distinct human a mean message
targets: an explicit @, or the author of the replied-to message (in the mention list only with the
reply ping on, so read both and dedupe). Roles and `@everyone` are never targets. Bots are never
targets **except SYNCS herself**. If it ever needs rationing, cap hits per message, not a cooldown
— the exploit is one message.

**Le Malfaisant — untargeted hostility scores one point, rationed** at one per person per channel
per 60 s (a rant is twenty messages). `CountTargets` returning 0 means "nobody named", not "ignore".
Known cost: it also scores hostility at game content ("ce boss est nul", "la hitbox est nulle").
This is the accepted trade; raising the mood threshold wouldn't separate them, since the false
positives come from strong cues.

**Le Perfide — rationed** at one hit per person per channel per 60 s and one per message however
many rivals: turning to another bot is bursty (forty music-bot replies a night). A slash command run
against another bot is never broadcast; the only trace is that bot's reply carrying the invoker in
`InteractionMetadata` (`Type == ApplicationCommand`). Ephemeral responses and prefix commands
(`!play`) are unavoidable blind spots. It uses the **looser** `RivalryService.IsRival` overloads
(no level-up carve-out), so replying to a rival's level-up announcement still counts — intentional.

**L'Hystérique — shouting, rationed** like Le Perfide. `MessageCues.CapsProfile` measures letters
and uppercase share; `Emphasis` (loose: 4 letters, >60%, only ever adds to a side that already
scored) and `IsShouting` (strict: **12 letters, 70%**, stands alone) apply different thresholds to
the same measurement. At 4 letters `LOL`, `OK`, `MDR`, `GG WP` would qualify. Deliberately **not**
short-circuited by `ReadFeedback`: nothing else records *how* a message was delivered.

**L'Indigne** — `AbandonHits`, from `/plynling abandon` (see `plynling.md`).

The spam-channel exclusion is asked of `XpTracker.IsChannelExcludedAsync` (the list stays there).

## The card

**Components V2; only the title holder wears an avatar.** Count: **31 of 40** — container 1 +
heading 1, five titles × 5 (separator, Section, its TextDisplay, avatar Thumbnail, runners-up
TextDisplay), filter row 1 + 3 buttons. **Room for exactly one more title**; a seventh throws in
`ComponentBuilderV2.Build()`. Re-do the sum and let the harness confirm it — hand counts here have
been wrong before. Runners-up are plain text (avatars would cost 3 each).

One button row, ids still carry the `shame:win` verb for the day a second row is added. Default
window **30 days** (an all-time default would be a hall of fame nobody can move). An empty title
renders a line from `ShameEmptyMalfaisant` / `ShameEmptyBanni` / `ShameEmptyPerfide` /
`ShameEmptyHysterique` / `ShameEmptyIndigne` instead of disappearing (a wall that changes shape
reads as broken). Those are interpolated, never `string.Format`-ed, so `{0}` would render literally.
No minimum count. Ties break on earliest row id (stable across re-renders).

**No footer line, deliberately** — a footer restating a rule went stale once. The command
description already says who may vote.
