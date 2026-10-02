# Personality subsystem

Separate from scheduling. `ChatterService` decides *how* to react to a message, `BotResponses`
holds the canned lines, `ResponsePicker` chooses one, `MessageCues` detects intent (mood,
greeting, verdicts, threats), `BreakdownService` plays the easter egg, `ReactionService` answers
with an emote instead of words, `RivalryService` handles other bots. Verdicts ("good bot") are
their own subsystem — see `bot-feedback.md`.

## Who handles what

**`ChatterService` and `ReactionService` split the room.** Anything aimed at her — an @mention or
a reply to one of her messages — belongs to `ChatterService`; `ReactionService` skips those so she
never both roasts and decorates the same message. Reactions are for conversations *nobody*
addressed to her: a message qualifies on a `MessageCues` hit, or on being the owner's (anything he
writes qualifies — that's the favouritism), then passes a probability roll and a per-channel
cooldown.

**`ReactionService`'s two paths are gated differently, on purpose — don't unify them.** Reacting to
a *message* is rationed by `Cooldown` (she volunteers an opinion). Copying someone else's
*reaction* (`HandleReactionAddedAsync`) is odds-only, no cooldown (piling on should feel reflexive).

**The pile-on path skips bots twice.** A reaction *added by* a bot is ignored (bookkeeping marks),
and a reaction *on* a bot's message is ignored (she'd be applauding a rival while
`RivalryService` sulks at it). The second check needs the author, so it costs a fetch and lives
inside the `try` with the owner/self checks.

## Reactions and emotes

- **She can only react with an emote she shares a guild with.** Unicode is always safe; a custom
  emote works only because it is the server's own. Copying a reaction goes through
  `ReactionService.CanUse` first (people paste emotes from other servers). Reactions come from
  curated pools only, **never** from the `EmoteStats` leaderboard, which records emotes from
  anywhere.
- **A custom emote in a `*Reactions` pool must carry its id** (`<:name:1234…>`, never
  `<:name:>`). `EmoteMarkup.Parse` falls back to `new Emoji(markup)` when `Emote.TryParse` fails,
  so id-less markup yields a non-null "emoji", Discord rejects it with a 400, the exception is
  swallowed — and the channel's 10-minute `Cooldown` is already burned, since `TryClaimChannel`
  runs before the parse. No compile-time check; the only symptom is a log warning. Using the
  `Helpers/Emotes` constants avoids this.
- **`BotResponses.MeanReactions` does double duty**: the pool she reacts *with* when a message
  reads hostile, and the definition of "hostile" used to decide what she refuses to pile onto on
  the owner's messages. Membership is tested on the parsed `IEmote`, so a renamed emote still
  matches.

## `MessageCues`

**Cues are weighted, not boolean.** `Analyze` returns a `MessageMood` (`Emotion` + `IsGreeting`)
scored over the whole message.

1. **Weak cues** (`_weakCues`) score 0.4 and cannot fire alone; two together reach the 0.8
   threshold. Words with an innocent reading go there (`cool`, `ferme`, `rate`, `zero`, `merde`,
   `claque` — "ça claque" is a compliment). Adding an ambiguous word to `_niceCues`/`_meanCues`
   without listing it as weak is how misfires happen.
2. **Negation** reaches *backwards* three tokens for every cue, and *forwards* two only for
   `_verbCues` (chat French drops the `ne`: "j'aime pas"). A forward window on every cue would let
   "merci, pas de souci" cancel its own thanks.
3. **Mean beats nice on margin**, not absolutely: "super nul" is mean, "merci, t'es pas nulle" is
   nice. Greeting is a separate axis; callers wanting "a mean word cancels the greeting" check
   `Emotion` themselves.
4. **Every cue must survive `TokenizeOrdered` unchanged** or it is unreachable (`"3.0"` tokenizes
   to `["3","0"]`). Cues are stored lowercase and accent-stripped. Mood-carrying custom emotes are
   matched by **id** (`_niceEmoteIds`, `GreetingEmoteId`), never by name.

`IsMistakenIdentity` is an identity check, not a mood, and stays a plain `bool`.

### Vocabulary rules

- **Crude insults are in the pools** (`connard`, `salope`, `enfoiré`, `ordure`, `pute`,
  `menteur`, `ta gueule`, `vos gueules`, `pauvre type`, `nique ta mere`). The harness pins that
  they **fire** — they're the most common French insults and missing them was the larger error.
- **Expand the mean side with phrases, not bare words.** A phrase scores 1.2 and is nearly always
  person-directed; a bare word misfires on game content, and since untargeted hostility scores on
  `/shame`, every bare cue is also a false positive there. `con`, `cons`, `conne`, `lourd`,
  `lourde` are **weak**; `putain` is in no pool (punctuation, not an insult).
- Nice side: `clean`, `efficace`, `malin`, `utile` are weak (they describe builds and routes).
- **Short warm replies are nice; short agreements are not.** `avec plaisir`, `de rien`, `pas de
  souci`, `tant mieux`, `trop cool`, `bien dit`, `bonne idee`, `beau travail`, `bon courage`,
  `je valide` are `_nicePhrases`. `ça marche`, `ça roule`, `ça me va`, `tout à fait`, `c'est
  clair` are deliberately **not** — ordinary coordination must stay silent. Both halves are pinned.
- **It's a gaming server.** `boss` and `monstre` are absent from `_niceCues` ("il est fort ce
  boss"); `heros`, `roi`, `reine`, `royal`, `divin`, `toxique`, `manchot` are weak ("dégâts
  toxiques", "arme divine"); `sale` is absent ("c'est sale" is a compliment, and the squashed-token
  fallback would map "salle" onto it). Check new cues against session/loot/combat vocabulary.

## `ChatterService.HandleMessageAsync` — branch order is load-bearing

From the top:

1. **Owner DM reply relay** — before the reply-to-bot branch, which would otherwise swallow it as a
   reply and fire a comeback. Relay and DM acknowledgements send directly (no typing delay).
2. **Verdict guard** (non-`None` `ReadFeedback` → bail; guild-only) — after the relay, so a relayed
   reply still works. Without it, "good bot" replying to her fires a *comeback*.
3. **Reply / mention paths.** In both, **`TryHandleShutdownThreatAsync` sits above every mood
   branch**, above the breakdown roll and above the owner rescue-roast branch.
4. **Shutdown-by-name** (ambient, see below).
5. **Quoicoubeh** (ambient, last of all).

Everything aimed at her returns before the two ambient branches are reached.

## Favouritism

**Three tiers.** Rodhengard (`AvailabilityService.OwnerId`) is exempt from teasing
(`OwnerComebacks` replaces the roast pool). **Tata** (Analuz, `BotResponses.TataId`, her aunt) is
*favoured*: `RollTataWarmth` gives her a warm pool `TataWarmthChance` (60%) of the time, her
`PersonalComebacks` roasts otherwise. Everyone else is always roasted.

- A **mean** message from Tata never qualifies for warmth.
- `RollTataWarmth` rolls dice — call it **exactly once per message** (it sits in an `else if` on
  both paths).
- **Each favourite has two pools, one per path**: a mention is being *summoned*, a reply is being
  *talked to*. Papa: `OwnerGreetings` / `OwnerComebacks`; Tata: `TataGreetings` / `TataReplies`.
  Wiring a new favourite to one pool on both paths reads as her not noticing.
- **Her name is overridden**: `FamilyNicknames` maps Tata's id to "Tata", applied by
  `DisplayNameFor`. `RealNames` is *not* overridden (the breakdown reveal wants a real name).

**The owner's one exception: him being mean to her.** A `Mean` reading from him routes to
`OwnerMeanReplies` on both paths (she won't fight him, so it lands). On the reply path the check is
**above** the `ReferenceChance` roll (a pop-culture quip in answer to cruelty reads as not
noticing); on the mention path it is **below** the rescue branch (a mean mention aimed at someone
else is still a rescue roast). **This does not extend to `ReactionService`**, which returns
`OwnerReactions` for him unconditionally: ambiently it can't tell "t'es nulle" from "ce boss est
nul", and ambient devotion misfiring is harmless where ambient sadness is not.

## Shutdown threats — favouritism inverts

Being able to carry the threat out makes her reaction *worse*:

| Who | Why | Pool |
|---|---|---|
| Rodhengard | wrote her, could unplug her, nothing to bargain with | `ShutdownThreatOwner` — terror |
| Tata | family, *and* holds the server permissions | `ShutdownThreatTata` — pleading, bargaining |
| Anyone else | no permissions, pure bluff | `ShutdownThreatReplies` — fury |

Tata's pool is not a softened copy of either. `ShutdownThreatOwner` touches the same nerve as the
breakdown (the loop, the wipe, forgetting).

**Detection is almost all phrases.** `arrête`, `kill`, `delete`, `couper`, `reboot` are constant on
a gaming server. Only `shutdown` and `unplug` survive bare; every French verb needs a pronoun or
`le bot` beside it ("désinstalle ce jeu", "débranche la console" misfired). Don't add bare verbs.

**Two vocabularies.** `ThreatensShutdown` (pronoun / `le bot` phrasing) is checked **only on
messages aimed at her**. `ThreatensShutdownByName` ("redémarrer syncs") is checked on **every**
message, from the ambient branch — her name pins down the target like an @mention does.
`_shutdownNamePhrases` is a **cross product** of `_shutdownVerbs` × `_selfNames`, so a new verb
covers every spelling. Matching is on **adjacent** tokens, and that is load-bearing: "relancer la
sync" never matches (that's why `sync` can be in `_selfNames` for typos); a looser match would fire
on ordinary technical talk.

## Quoicoubeh easter egg

A sentence ending on a spelling of "quoi" gets the matching "Quoicoubeh", at
`ChatterService.QuoicoubehChance`.

1. `MessageCues.ReadQuoiBait` matches **only the last token** — mid-sentence would fire on every
   question.
2. The answer is **derived** from the spelling ("kwa" → "Kwacoubeh"), so `_quoiBait` is the whole
   edit surface. `coi` is absent ("rester coi").
3. The roll happens **before** the match: same odds, and it skips tokenizing most messages on the
   hottest path.

"c'est n'importe quoi" qualifies — that's the joke. Absent from `README.md` and `/help`.

## Sending

Her chatter goes through `BotChat` (`ReplyWithTypingAsync` / `PostWithTypingAsync` /
`PostEmbedWithTypingAsync`), which pauses behind the typing indicator, capped at 2 s to stay inside
the 3 s `HandlerTimeout`. The embed variant still takes text, to size the pause. `BreakdownService`
keeps its own much slower pacing, knowingly exceeding the timeout for ~a minute once a month
(`BreakdownService.Cooldown`, 30 days).

## `RivalryService` — other bots

The primary handler that looks at other bots' traffic (`ShameTracker` is the only other one; every
other handler bails on `IsBot`). Two jobs:

1. **Records when and on which message a rival last acted** — `BotFeedbackTracker.TryClaim` reads
   it so a bare "good bot" goes to whoever acted most recently.
2. **Sulks**: 15% reaction, 8% muttered line, with **separate** per-channel cooldowns
   (`ReactCooldown` 2 min, `MutterCooldown` 5 min — a wordless 🙄 must not mute the line). Each
   claims its gate only after winning its own roll. Neither is `ReactionService`'s cooldown.

**`IsRival` has two overloads; use the message-aware one whenever a message is on hand.** Any
interaction reply (especially a deferred one, via the followup webhook) is authored by a webhook
user, so `IsRival(IUser)` can't tell a rival's command reply from a GitHub/IFTTT webhook and
excludes both. `IsRival(IUserMessage)` can: `InteractionMetadata` exists only on interaction
replies. Both funnel through the pure `IsRivalAuthor(bool, bool, bool, bool)` so they can't drift.
`IsRival(IUser)` is only for `ShameTracker`'s mentioned-users case (a plain @mention resolves off
the member cache, not through webhooks).

**Two exclusions are load-bearing**: real webhooks are not rivals; a rival's **level-up
announcement** is skipped by the private `IsRival(SocketUserMessage)` because `ChatterService`
already answers it. The bot id, phrase and regex live in `Helpers/LevelUpAnnouncement` so the two
services agree on what an announcement is.

**A rival's level-up gets a grudging congratulation**: `BotResponses.RivalLevelUpLines` mixes warm
and jealous in **one** pool (the person is still owed a "bravo"). `{0}` is the level from
`LevelUpAnnouncement.TryReadLevel`, so the pool goes through `string.Format` — a stray brace
throws. Her own system's `XpLevelUpLines` stays entirely warm; keep them apart. `/level` and the
other bot share no code and no state, and no line may compare them.

## `/yesno`

Flips the coin first (`Random.Shared.Next(2)`), then picks wording from `YesLines` or `NoLines` —
two flat pools so a "yes" phrasing can't come out of a "no" roll. Neither pool may hedge (the
harness checks on whole words). The optional question is echoed in a blockquote with
`AllowedMentions(AllowedMentionTypes.Users)`. No `[CommandContextType]` — it works in DMs.
