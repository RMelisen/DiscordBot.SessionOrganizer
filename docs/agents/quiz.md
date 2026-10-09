# Pop quiz

Up to twice a day SYNCS posts a question in the guild's quiz channel; the first right answer
wins cailloux and a point on `/quiz leaderboard`.

| Piece | Role |
|---|---|
| `Services/QuizMasterService` | Singleton + hosted loop: when to post, typed answers, button picks, closing |
| `Services/QuizService` | Transient EF access: rounds, the win transaction, the board |
| `Helpers/QuizBank` | The questions (pure) |
| `Helpers/QuizAnswer` | Judges a typed answer (pure) |
| `Helpers/QuizSchedule` | Slots, gaps, daily cap, quiet clock rules (pure) |
| `Commands/QuizCards` | The round card and the board card (static, measurable) |
| `Interactions/Components/QuizComponentHandler` | The A/B/C/D buttons, `quiz:pick:{round}:{position}` |
| `Commands/QuizModule` | `/quiz leaderboard`, its window buttons `quiz:win:{period}:0` |

## Channel and schedule

- **Off by default.** `GuildSettings.QuizChannelId` (`/config quiz-channel set|clear`) has no
  hardcoded fallback: zero means no quiz. Clearing it leaves an open round to finish where it is
  (the round stores its own channel).
- **Two slots a day**, one in 11:00–16:00 and one in 16:30–22:00 Paris, each kept at
  `SlotChance` (75%): 0–2 a day. Drawn in memory on the first tick of the Paris day; a restart
  redraws and drops past slots, so an update can cost a quiz but never doubles one.
- **A due slot waits for people**: it posts only when a human spoke in the quiz channel within
  `ActiveWithin` (45 min), no round is open in the guild, fewer than `MaxPerDay` were posted
  today (counted in SQL on `QuizRound.Day`) and the last one is `MinGap` (3 h) old. Past `DayEnd`
  (22:00) or at the cap, the day's slots are dropped.
- **The quiet clock** is `_lastHumanAt`, fed by `HandleMessageAsync` for every message, seeded
  from the last 20 messages the first time a channel is needed.
- **Its own 1-minute tick**, never shared: a round must close close to the hour it announced.
- Breakdown in progress → no post and no typed answers in that channel.

## Rounds

- **The row is written before the card is sent**, so a click always finds its round; a failed
  send closes the row again. `MessageId` is filled in after the send.
- **Multiple choice** is shuffled at post time; `ChoiceOrder` stores the shuffle (`"2031"`:
  position i shows `Choices[order[i]]`, the right one is where `'0'` sits). Buttons and the reveal
  read it, so they agree after a restart.
- **Restarts resume rounds**: the first tick reloads open rounds into the typed-answer cache and
  closes anything that came due meanwhile; buttons look the round up by id. Who already clicked a
  multiple-choice round is in memory only, so after a restart someone who clicked wrong may click
  again. Accepted.
- **Timeout** after `OpenFor` (1 h): `QuizService.CloseAsync` is conditional (`!Closed`), so a win
  racing the timeout is announced once, by whichever got there first.

## Winning — once

`QuizMasterService.TryWinAsync` is the single path for both buttons and typed answers:

1. An in-memory claim (`_claimed.TryAdd(roundId)`) stops a second caller before any DB work.
2. `QuizService.RecordWinAsync`, in one transaction: a conditional `ExecuteUpdate`
   (`WinnerId = 0 AND !Closed`) — zero rows means someone else won — then the wallet
   (`PebbleService.GetOrCreateWalletAsync`), `EconomyLog.EarnQuiz`, `QuizStat` and today's
   `QuizDailyStat`, one save, commit.
3. **The claim is released if that throws**, and so is the clicker's one try.

Then the card is edited (closed, answer revealed, the right button green) and her win line is
posted as a reply: to the winner's message for a typed answer, to the card for a click. Every
send uses `AllowedMentions.None`: the winner's mention is a pill, not a ping.

- The button handler **defers first** (deferred update); refusals are ephemeral followups.
- A winning typed answer makes `HandleMessageAsync` return true, and `BotService` skips
  `ChatterService` for that message, like `AmbientService`'s scolding.

## Typed answers — `QuizAnswer`

- Lowercase, accents stripped, punctuation splits words, spaces squashed on both sides
  ("petri chor" = "pétrichor"). Leading articles are dropped from the accepted answers.
- The answer may sit in a sentence, but at most `MaxExtraWords` (2) non-filler words beside it:
  a list of guesses doesn't win. "mario luigi peach" still wins "Luigi"; that hedge is accepted.
- Typos by length of the squashed answer: none under 5 characters, 1 from 5, 2 from 10, never
  with a digit in the answer.

## The bank — `QuizBank`

- **Keys are stored** (`QuizRound.QuestionKey`): append-only, never renamed.
- **Categories are display only** (the card header): nothing stores one, so a question moves
  between categories freely. Its key keeps the prefix it was written under (`culture.octopus`
  is Nature): renaming the key to match would orphan its past rounds.
- **Every fact must be true**, as for `MorningFunFacts`; many questions are adapted from them.
  The joke is her `Aside`, shown under the revealed answer.
- **Every question has an aside** (the factories require one), in her voice
  (`docs/syncs-voice.md`): a reaction with a turn, not the fact restated. One tag at most, at the
  end; dry lines carry none.
- **Anything brute-forceable is multiple choice**: years, small numbers, yes/no, single letters.
  An open question lists the spellings people will actually type.
- Multiple choice: exactly four choices, the right one first, none arguably right too.
- Rewards by difficulty: 15 / 25 / 40 cailloux (`RewardFor`).
- **SYNCS questions stay a small share** and leave Synthia and the breakdown out: they state lore
  outright, which the rest of her voice avoids (`docs/syncs-voice.md`).
- The question is `DailyRotation.IndexFor(bank, guild's round count, QuestionSalt)`: nothing
  repeats until the bank is spent; editing the bank reshuffles the walk from the next round.

## Lines

`QuizIntroLines`, `QuizWinLines` / `QuizOwnerWinLines` (Papa wins: adoration, never a pinch) and
`QuizTimeoutLines` go through `DailyRotation` keyed by the round id (spent twice a day at most; a
restart would wipe `ResponsePicker`). The ephemeral `QuizWrongLines` / `QuizAlreadyTriedLines`
are spent often and use `ResponsePicker`. Win lines take `{0}` mention, `{1}` answer, `{2}`
reward; timeout lines `{0}` answer — no stray braces.

## Leaderboard

Totals + daily buckets (`QuizStat` / `QuizDailyStat`), the project's one pattern for dated
rankings: all-time reads the totals (wins, then fastest `BestMs`), windows sum the buckets.
The buckets do not sum to the totals and must not be made to.

## `/debug quiz`

Owner-only: `action:Post` posts now (the next question, or `key`, with autocomplete),
ignoring slots, cap and quiet clock but never beside an open round; `action:Close` ends the open
round as the timeout would. How to test in the dev guild after `/config quiz-channel set`.
