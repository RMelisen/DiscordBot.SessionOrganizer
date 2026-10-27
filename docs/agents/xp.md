# XP, `/level`, `/leaderboard`, voice XP, stats rankings

## The system

**`/level` is SYNCS's own XP system, deliberately parallel to the server's other leveling bot.**
They share no code, state or vocabulary beyond "niveau", and no code or comment may treat one as
the better system. (`Helpers/Xp/LevelUpAnnouncement` only *detects* the other bot's announcements so
she can answer them.) Her *lines* may sulk about it, but only in her jealousy pools
(`RivalLevelUpLines`, `JealousLines`, `RivalMutters`); `XpLevelUpLines` and the `/level` UI never
mention it. See `docs/syncs-voice.md`.

**`XpTracker` is the singleton every signal funnels through**: message, reaction, bot-interaction
bonus, verdict bonus, voice sweep. Same `IServiceProvider` + `CreateAsyncScope` shape as the other
trackers. It is on `BotService`'s `ReactionRemoved` fan-out too.

**`MemberXp.TotalXp` is the only number stored — `Level` is never cached.**
`Helpers/Xp/LevelCurve.ThresholdForLevel` is closed-form and `LevelForXp` a binary search over it;
a level can always be recomputed, so a column would only drift.

**XP is a reward; `ReactionsUsed` and `VoiceMinutes` are facts.** Every reaction counts even when it
earns no XP. Wipes reset the reward and keep the facts (see root `CLAUDE.md`, migrations). The two
counters are decided **only in `XpTracker`**, never in `EmoteTracker` or `VoiceXpService`: the
reaction count is taken *after* the exclusion check and *before* the 60 s cooldown claim; the voice
minute rides the same call that grants voice XP. The reaction count goes to `ActivityCounter` (in
memory) and `ActivityFlushService` writes it every 5 minutes (see admin), like `EmoteTracker`'s
counts: never a database write per reaction. Removal decrements (clamped at zero, a net removal
never creating a row), otherwise add/remove loops would inflate the ranking. No XP is ever withdrawn.

**`MemberXp` / `MemberDailyStat`** is a totals+buckets pair: all-time reads `MemberXp` and is exact;
windows only cover data since the buckets shipped.

## Excluded channels

**`XpTracker.ExcludedChannels` is checked before `TryClaim`, never after** — claiming first would
let spam in an excluded channel burn the person's cooldown and block them from earning elsewhere.
All four signals check it (message, reaction, verdict, voice — which is why
`GrantVoiceXpAsync` takes the channel id: the list lives only in `XpTracker`). A **thread** counts
as its parent. The decision is a pure `(channelId, parentId?, configured)` overload; the hardcoded
check runs first, so a hardcoded channel never reaches the database. `IsChannelExcludedAsync` is
public so `ShameTracker` can ask; the set itself is private, exposed read-only as
`HardcodedExcludedChannels` for `/config show` and `/config excluded-channels`. `/config` can add channels, never remove hardcoded
ones (see `admin.md`).

## Bonuses

**The verdict bonus is granted by `BotFeedbackTracker`, never detected independently** — a second
`ReadFeedback` watcher would make "good bot" farmable with nothing for her to have done.
`GrantVerdictBonusAsync` also keeps its own 30 s `VerdictCooldown` (attribution rations *which*
verdicts count, not *how often*). Good = 25 XP, Bad = 15 (judging her at all is engagement).

**Every XP grant also pays passive cailloux** from `XpTracker.GrantAsync` (see `economy.md`), in
its own `try` so a failure can't swallow the level-up card. The link is one-way: Plynling actions
grant no XP.

## Cards

**`/level` and `/leaderboard` are Components V2** (all the root V2 rules apply). `/emotestats` and
`/goodbot` stay paged embeds — they rank things with no avatar or level.

**`PageSize` is 5 because of the 40-component cap.** A row with an avatar costs three (Section +
TextDisplay + Thumbnail). A page of 5 with all three button rows uses 31 of 40; 10 would be 46. At 5
rows there is room for one more row of five buttons and no more.

**`/leaderboard` is four views** (`LeaderboardView`: XP, reactions, voice, collections) — only the
ordering and each row's second line change. The first three read `MemberXp`/`MemberDailyStat`;
**Collections** reads `InventoryService.GetCollectorRankingAsync` (sets completed: the
`CollectionCompletion.Count` totals all-time, `CollectionDailyStat` buckets in a window — see
`economy.md`), shaped into the same `MemberTally` (`Completions`) by `LevelModule.RankingAsync`, so
the page, rows and footer stay one path. Its button joins row 0; the component count doesn't move. Custom-id: **`level:{verb}:{view}:{period}:{page}`**,
view before period on purpose, so the period row is built by `StatsPeriodUi` with `level:win:{view}`
as its prefix. Verbs: `level:view:` (metric row), `level:win:` (period row), `level:page:`
(paging) — see the root rule on one verb per row; the active view button and active period button
collided on every render before this. Changing either filter resets to page 0 and leaves the other alone. Default
period **all-time** (a standing).

**A level belongs only to the all-time view** (a level is a function of a *lifetime* total):
`LevelCardUi.RowValue` prints `Niveau N · X XP` for all-time and `X XP gagnés` for a window; the
footer switches the same way. The footer takes the viewer's rank **from the list already loaded**,
so it follows the view and can't disagree with the rows. (`XpService.GetRankAsync` serves `/level`.)

**`/level` is a card, not the leaderboard opened at your row**: avatar, level, rank, and a progress
bar from `LevelCurve.XpIntoLevel` over `XpForLevel` (the same pair printed beneath it). Its "Voir le
classement" button reuses the paging id `level:page:Xp:AllTime:0` and replaces the card in place. No XP → niveau 0, `non
classé`. A **bot** gets one fixed `const` refusal (not a pool — a refusal isn't chatter). Someone who
has earned something gets one more separator + TextDisplay at the bottom plus a « Succès »
button (the card is 11 components of the 40): see *Member badges* below.

**`/recap` is a card about one person over a window** (`RecapModule`, Components V2, the
`StatsPeriodUi` row with prefix `recap:win:{userId}`, opening on **30 days**). `RecapService` reads
only tables other subsystems already write, so it has no migration and no table of its own.
- **All-time reads the totals; a window sums the daily buckets** (`MemberDailyStat`,
  `VoiceCompanionDailyStat`, `BotFeedbackDailyStat`, `QuizDailyStat`, `ShameDailyStat`), filtered in
  SQL on `Day`. Never one standing in for the other.
- Level, rank and the quiz record are lifetime figures, shown all-time only.
- Sessions and `/tonight` answers have no buckets, so their DateTimeOffset windows are applied in
  memory.
- **A missed session only counts if she called its roll** (`RecapSent`). Sessions from before
  attendance existed must not read as no-shows.
- The Plynling, its graves and the balance are state, never windowed.
- There is **no per-person emote stat** (`EmoteStat` is per guild): don't invent one.

Her last line comes from `RecapProfile.Classify`: the first clear trait in a fixed order (shame
before hate before flaking … before « good bot »), with thresholds scaled to the window. Papa
always gets `RecapOwnerLines`. Her own recap answers with `RecapHerselfLines` (go see `/health`),
and another bot gets a `const` refusal.

**`Helpers/Xp/LevelCardUi` holds the string work only** (medals, fr-FR grouping, block-glyph bar,
row/card text) so it's checkable without a gateway. The bar clamps at both ends and treats a zero
span as full — decoration must never throw.

**The level-up announcement is a card**: `XpTracker.AnnounceAsync` posts an embed (avatar,
`Color.Purple`, title `Niveau {old} → {new} !` — one grant can cross several levels and announces
once, so `GrantAsync` forwards both). At level **7 or 67** the description is the literal
`"SIX SEVEEEN"` instead of an `XpLevelUpLines` pick — a literal level check (17, 70, 167 stay
quiet), and `ResponsePicker` is never consulted for it. If the member doesn't resolve, skip the
card, keep the XP. **Admin adjustments never fire it** (see `admin.md`).

### Member badges (« Succès »)

- **The catalog is `Helpers/Xp/MemberBadges`** (pure; keys stable and append-only, they are stored
  and part of icon file names). Three kinds: paliers (five tiers over a counter), moments (earned
  once) and secrets (earned once, hidden until found). Storage is one table, `MemberBadge`, per
  guild and member.
- **Paliers are derived at display time and never stored**, so thresholds can move without a
  migration. The exceptions are the two counters `arcade` and `nanny`, stored as `MemberBadge.Count`.
  The moments `lightning` and `punctual` are derived too and shown without a date.
- **Every write goes through the singleton `BadgeAwarder`**, after the flow's own save,
  fire-and-forget. It ignores guild 0 (DMs), unknown keys and her own id, and releases its in-memory
  claim when a write fails. `MemberBadgeService.AwardAsync/IncrementAsync` rethrow anything that is
  not a lost race so the awarder can log and release. Never write a badge from a flow directly.
- **Arcade wins**: she never earns one; a human beating her at a board game counts; Race and Crash
  count a positive net only.
- **No announcement of any kind** when a badge is earned (no message, DM or reaction).
- **« Succès » is an ephemeral view** (`MemberBadgeCards`, ids `level:badges:{userId}` and
  `level:badgetab:{userId}:{section}`) with four tabs — Collections, Paliers, Moments, Secrets —
  opening on Paliers.
- **Secrets are never named** in `/help`, `README.md` or `CHANGELOG.md` (the changelog may say some
  badges are secret, nothing more), and no member names go in the changelog.

## Voice XP — `VoiceXpService`

**A 1-minute sweep, not join/leave tracking.** The payout is per minute, so a 1-minute sample is
exactly as coarse as the reward. It reads Discord.Net's live voice cache, so it needs no
`UserVoiceStateUpdated` subscription.

**Eligibility is one predicate, `IsActive`, used for both earning and counting "someone else is
here" — that is the anti-abuse design.** Splitting them lets muted alts unlock XP for others.
Self-muted **or** self-deafened is out; a missing `VoiceState` is out. Server mute/deafen by a
moderator is deliberately *not* checked (not self-inflicted, not farmable). **The AFK channel is
skipped entirely.**

**Voice XP tapers with the day's total** — the answer to two unmuted idle accounts, which no mute
rule can catch. `Helpers/Xp/VoiceXpCurve`: first hour at 10 XP/min, then down every half hour (8, 6, 5,
4, 3, 2) to a trickle of 1. A taper, not a cap (a cap teaches people exactly how long to park). Keep
the tier table sorted by minute, rates non-increasing, the trickle **non-zero**, and avoid big
single-step drops.

- The rate is a pure function of minutes banked today: `XpService.AddVoiceMinutesAsync` returns
  today's total *before* the increment, in the same round trip.
- The taper rations XP only; minutes are recorded in full (they're a fact).
- **`TotalForMinutes` is the single source of truth**: `XpForSpan` is a difference of two of its
  values, `RateAt` a one-minute span. Define any new rate function that way round.
- If counting minutes fails, **skip the payout** (fail closed).

**Every level-up card, voice included, goes to the level-up channel** (`BotsChannel.ResolveLevelUpAsync`:
`/config levelup-channel`, else the bots channel), not to where the XP was earned; the earning channel is only the fallback
if the bots channel doesn't resolve.

## Shared ranking vocabulary — `/emotestats`, `/goodbot`, `/leaderboard`, `/shame`

`StatsPeriod` (`Services/Core/StatsPeriod.cs`) and `Helpers/Core/StatsPeriodUi` own the windows, their French
labels and the three-button filter row, so commands can't label the same window differently. Each
caller passes its prefix and gets `{prefix}:{period}:0` ids — changing the window resets to page 0.
Defaults differ on purpose: `/emotestats` 30 days (recent activity), `/goodbot` and `/leaderboard`
all-time, `/shame` 30 days.

**`EmoteStat` vs `EmoteDailyStat`**: totals+buckets. `EmoteStat` predates dates; display markup is
resolved from it so a rename stays in one place.
