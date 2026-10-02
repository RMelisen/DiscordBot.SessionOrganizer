# Bot feedback — "good bot" / "bad bot"

`MessageCues.ReadFeedback` reads a verdict *on her*, separately from `Analyze` (a verdict is not a
mood). `BotFeedbackTracker` attributes it, counts it and answers it. `/goodbot` ranks it via the
`BotFeedback` / `BotFeedbackDailyStat` totals+buckets pair (default window **all-time** — verdicts
are rare, so a rolling window is usually empty).

## A verdict short-circuits three services

`ChatterService`, `ReactionService` and `ShameTracker` all bail early on a non-`None` verdict.
Without the `ChatterService` guard, "good bot" as a reply to her fires a *comeback* (praise gets you
insulted); without the `ReactionService` guard, "gentil bot" also scores `Nice` and both services
react. The `ChatterService` guard sits after the owner-DM relay and is guild-only.

## Reading the verdict

**A verdict is cancelled by what sits just before it.** `SaysVerdict` scans by token index and
skips an occurrence preceded by a negator or the opposite adjective ("bad good bot", "not good bot"
used to count as praise).

- **The window is two tokens, not three.** Two covers "pas un bon bot"; three let the `non` of a
  correction ("good bot… non en fait bad bot") cancel the complaint.
- **A cancelled occurrence is skipped, not fatal** — "not good bot… ok fine, good bot" lands on the
  second.
- **Cancelling yields `None`, never the opposite verdict** ("pas un mauvais bot" means praise).

`_goodBotAdjectives` / `_badBotAdjectives` are **derived** from the phrase lists, so a new phrase
teaches the canceller its adjective. `_verdictNegators` is `_negators` plus English ones, kept
separate so mood scoring is untouched.

**A framed verdict is not a verdict.** `IsFramed` refuses the whole message on reported speech
("il a dit good bot"), an explicit hypothetical ("imagine que…", "supposons", "théoriquement") or a
self-reference ("cette phrase", "ce message", "this sentence"). It only catches framings that name
themselves; the real bound is attribution (one verdict per person per action). Whole-message,
because a framing clause colours everything after it.

## Answering

**"Good girl" is the same verdict, different answer.** `ReadFeedback` has an overload returning a
`VerdictForm`: the tally treats both identically, `RespondAsync` branches on it. `VerdictForm` is
separate from `FeedbackKind` because the axes are independent.

**Who said it × how they said it are crossed: four pools per verdict.** `RespondAsync` switches on
`(byOwner, form)`: `GoodGirlReactionsOwner` / `OwnerReactions` / `GoodGirlReactions` /
`NiceReactions`, and the same shape on the bad side. "Bad girl" gets `BadGirlReplies`, not
`BadBotReplies` (wounded *professional* pride lands wrong against a personal scolding). Testing
`byOwner` first and returning once meant the owner never reached the girl pools. The harness
asserts the four are distinct references.

**The praise turnabout (~1 in 100 "good bot"s).** `RespondAsync` rolls `TurnaboutChance` first; on
a hit `SendTurnaboutAsync` sends a line and returns. The pool (`TurnaboutBoyLines` /
`TurnaboutGirlLines` / `TurnaboutNeutralLines`) is chosen by `BotResponses.GenderFor` (who said it), **not** crossed with `VerdictForm` or `byOwner` — a small
pool stays small by not multiplying across axes, so there's no owner-flavoured turnabout.

**`GenderFor` is seeded from confirmation, never from a name.** `KnownGenders` holds the owner
(`Boy`), Tata (`Girl`), and people who stated it directly. **Never add anyone on the strength of a
username or first name looking gendered.** The `// Name` comments repeat `RealNames`' (including
the two Lucas' disambiguation). Everyone else gets `TurnaboutNeutralLines`, which only use
gender-invariant French (*adorable*, *sage*, "quelqu'un de bien").

**Rodhengard praising a rival**: `OnPraiseStolenAsync` draws `JealousLinesOwner` (betrayal) for him,
`JealousLines` (wounded pride) for everyone else — same split as `BadBotReplies` /
`BadBotRepliesOwner`.

## Attribution — `BotFeedbackTracker`

**It learns what she did by watching her own traffic** (`MessageReceived`, `ReactionAdded`), so it
is the one handler in the fan-out that must **not** skip her own messages. A verdict is hers if it
is attached to one of her messages (a reply, or a verdict reaction on it — both always count), or
is a plain message within 5 minutes of her last action in that channel. **One verdict per person
per action, shared across all three routes**, which also gates the response. State is in-memory.

**`TryClaim` reports why a verdict missed.** `Claim.RivalOwns` (jealousy) is distinct from
`NoAction`. Rules: anything **unambiguous** (naming her, or a thumb on her chatter) is hers
regardless of timing; one naming *another bot* and not her is never hers; everything else goes to
the most recent actor (from `RivalryService`). Only a **Good** verdict fires jealousy.
`_rivalry.LastAction` is read **outside** `_gate` — nesting the two locks in opposite orders would
deadlock.

**`ReadTarget`: mentions decide, the reply only counts when nobody was mentioned.** An @mention
beside the verdict is a deliberate "this one"; a reply is often just context. Within a tier, naming
her wins. Discord puts the replied-to user in `MentionedUsers` only with the reply ping on, which is
why the tiers are ordered rather than merged. Reading only the reply once made "good bot @AutreBot"
count for her. `ReadTarget` is pure and gateway-free.

## Loops she must not feed

**Her own acknowledgement must not count as an action.** Her reaction comes back on
`ReactionAdded`; recorded naively it clears `Judged` and hands out another free verdict forever.
`MarkAcknowledged` is called **before** the reaction is sent (the echo races it) and
`HandleReactionAddedAsync` skips those ids.

**Her bad-bot reply is not judgeable.** Otherwise "bad bot" → comeback → "bad bot" loops forever.
`_notJudgeable` is consulted in three places: `RecordAction` won't open an action for one, the
reply path drops a verdict aimed at one, the reaction path skips them. A reply's id exists only
**after** sending, so the echo can win: `SuppressJudgement` both records the id and withdraws an
action already opened for it (that's why `LastAction` carries `MessageId`). Cover both orderings.
The `_notJudgeable` bail is gated on the verdict being **hers** — a verdict aimed at a rival can't
start the loop, and bailing would swallow it.

**The turnabout reply is deliberately not in `_notJudgeable`** — more praise looping is harmless.

## The reaction path

- **Counts on her chatter only**: `resolved.Components.Count > 0` skips cards and leaderboards,
  where 👍 means "I'm in". Buttons, not embeds, because Discord adds an embed to any message with
  a link.
- **Silent by design** (a 👎 on an hour-old message must not fire a comeback into a dead room).
- **Removing a reaction does not decrement**; the claim already prevents a re-count.
- **Not thumbs-only.** On this server custom emotes are how people react. `_goodEmoteIds` /
  `_badEmoteIds` (matched by **id**) and `_goodUnicode` / `_badUnicode` (matched by code-point
  **prefix**, so `👍🏽` and `❤️` land) are curated here and deliberately **not shared** with
  `NiceReactions` / `MeanReactions` — those are what she reacts *with*, these what she *reads*
  (she'd never react with 🔪; a person doing it to her means something).
- **Only the first verdict reaction per person per message counts.** `TryClaimFirstReaction`, keyed
  on `(messageId, userId)`, is checked **before** `TryClaim` and spent even when the claim refuses
  — otherwise people keep adding emotes until one lands. 👍 then 🔪 counts as praise. Bounded FIFO
  at 500.

## XP bonus

The verdict XP bonus is granted **only** through this attribution (`XpTracker.GrantVerdictBonusAsync`
after `TryClaim` lets a verdict through), never by re-detecting verdicts — see `xp.md`. The call sits
**after** `RespondAsync` so her acknowledgement is never delayed by a level-up card.
