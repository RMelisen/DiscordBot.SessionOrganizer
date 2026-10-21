# Personality subsystem

Separate from scheduling. `ChatterService` decides *how* to react to a message, `BotResponses`
holds the canned lines (one partial file per category in `Services/Personality/BotResponses/`,
indexed at the top of `BotResponses.cs`), `ResponsePicker` chooses one, `MessageCues` detects intent (mood,
greeting, verdicts, threats, what a message aimed at her is doing), `HerMessages` remembers what her
own recent posts were, `BreakdownService` plays the easter egg, `ReactionService` answers
with an emote instead of words, `RivalryService` handles other bots. Verdicts ("good bot") are
their own subsystem — see `bot-feedback.md`.

## Who handles what

**`ChatterService` and `ReactionService` split the room.** Anything aimed at her — an @mention or
a reply to one of her messages — belongs to `ChatterService`; `ReactionService` skips those so she
never both roasts and decorates the same message. Reactions are for conversations *nobody*
addressed to her: a message qualifies on a `MessageCues` hit, or on being the owner's (anything he
writes qualifies — that's the favouritism), then passes a probability roll and a per-channel
cooldown.

**Her name counts as addressing her.** A message saying « syncs » or « sync » (`MessageCues.NamesHer`) is
`ChatterService`'s too, mention or not: `ReactionService` and the early morning hello skip it, as
they skip a mention, so she never both answers and decorates it. So does one of Papa's pet names,
from him only: every such check goes through `MessageCues.NamesHerFrom(content, fromOwner)`. See
*Talked about* below.

**One exception: the energy drink can.** A message mentioning Monster or an energy drink
(`MessageCues.MentionsEnergyDrink`) gets an `EnergyDrinkReactions` can **every time**: no roll, no
cooldown, even on a message aimed at her (only bots, DMs and a running breakdown are skipped). It
names a drink rather than reading the message, so it is not a decoration on a comeback, and it never
takes the mood reaction's turn. "Monster Hunter" doesn't count; posting the `:monster:` emote does.

**`ReactionService`'s two paths are gated differently, on purpose — don't unify them.** Reacting to
a *message* is rationed by `Cooldown` (she volunteers an opinion). Copying someone else's
*reaction* (`HandleReactionAddedAsync`) is odds-only, no cooldown (piling on should feel reflexive).

**The pile-on path skips bots twice.** A reaction *added by* a bot is ignored (bookkeeping marks),
and a reaction *on* a bot's message is ignored (she'd be applauding a rival while
`RivalryService` sulks at it). The second check needs the author, so it costs a fetch and lives
inside the `try` with the owner/self checks.

## Answer stats — `/debug answers`

What people say to her, so the owner can see which intents to add or widen. Owner only, so absent
from `/help` and the README.

- **Every chat answer of `ChatterService` is counted** by path (`ChatPath`: reply, mention, name) and
  kind (`Helpers/Personality/AnswerKinds`): each intent, reply context and fact, the mood pools and
  fallbacks, streak nudge / end / sulk, shutdown, Inabot, rescue, breakdown. The ambient eggs
  (quoicoubeh, Tron) and the rival level-up line are not chat answers and aren't counted.
- **Kinds and paths are stored keys, so append-only.** The builders put an enum's *name* in the key
  (`intent:Love`), so `ChatIntent`, `ReplyContext` and `ChatFact` may be reordered, never renamed.
  A new kind needs nothing else: it shows up once it's counted. A fact line that falls back is counted
  as the line it replaced.
- **Totals + daily buckets** (`ChatAnswerStat` / `ChatAnswerDailyStat`, `ChatAnswerService`), the root
  pattern; GuildId 0 is DMs. The write runs **off the gateway handler** (`ChatterService.Count`,
  `Task.Run` in its own scope, caught and logged): a failed count never costs an answer.
- **The examples are kept 30 rolling days** (`UnansweredExample` rows; rules in `UnansweredLog`): the
  messages whose answer `AnswerKinds.IsUnrecognised` (the plain fallbacks, and the fact and nudge
  lines standing in for one). Written by `ChatAnswerService.AddAsync` in the same save as the count
  (off the handler, like it), which also prunes anything older — `Day` is an int, so the delete and
  the window run in SQL. **No author is kept**, at the owner's request: only what was said,
  whitespace collapsed, 200 characters at most. The examples view follows `period` (7 or 30 days;
  « depuis toujours » is the 30 kept).
- The view (`AnswerStatsCards`, static): one embed, a field per family, rows by count with the
  split by path, and *Pistes* (the unrecognised share per path, the intents never hit in the
  period). Every key on every path measures about 3,300 characters, under the 6000 cap; fields are cut
  at 900. The examples go out as a `.txt` file.
- **Posted in the open, not ephemeral**, under an `OwnerReportLines` line (her reporting for duty
  to Papa); only the refusals stay ephemeral. Sent with `AllowedMentions.None`.

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
  `Helpers/Core/Emotes` constants avoids this.
- **`BotResponses.MeanReactions` does double duty**: the pool she reacts *with* when a message
  reads hostile, and the definition of "hostile" used to decide what she refuses to pile onto on
  the owner's messages. Membership is tested on the parsed `IEmote`, so a renamed emote still
  matches.

## Sticker follow-ups

After some of her lines she posts a picture of herself: a strip from `ProjectSYNCS/Assets/Stickers/`
(why strips and not server stickers: `docs/syncs-appearance.md`, *Formats*).

- **A line opts in by tag.** `Helpers/Core/Sticker` holds one constant per mood with a strip
  (`Sticker.Bored`…), appended to the template: `"Occupée à exister." + Sticker.Bored`. The tag names
  the face the *line* makes, not its pool's: one pool mixes snark, boredom and anger. An untagged
  line never gets a strip. Never paste a raw `⟪…⟫` marker into a pool.
- **Two strengths.** `Sticker.X` rolls 1 in 3; `Sticker.Sure.X` skips the roll. Sure tags live in the
  moment pools only (`HeatRecordLines`, `CrashWakeLines`, `PowerCutWakeLines`, `RouterReturnLines`,
  `WeekendPollDeadLines`, `SessionRecapFullLines`, `QuizCrown*Lines`, `GiveawayEmptyLines`), and
  every tag there is sure.
- **One gate, `StickerService`** (singleton): the roll, nothing in a channel in breakdown, then **one
  strip per channel every 5 minutes** (`ChannelCooldown`, sure tags included; claimed after the roll,
  so a lost roll spends nothing). No daily cap. The strip goes out about a second after the line, off the gateway handler, as
  a file with no text and `AllowedMentions.None`. A failure (no Attach Files) is logged and the
  line stands alone.
- **WALL-E's `HeartLines` roll 2 in 3, not 1 in 3.** `RobotHeartService` splits the tag off itself,
  rolls `RobotHeart.StickerChance`, and hands a win on promoted to sure so it doesn't roll twice;
  the lines keep ordinary tags, which name the picture.
- **Markers can be smuggled.** A user-chosen name formatted into an *untagged* line can carry a
  marker, and with no cap it would get its strip every time that line is sent. Tag every line of a
  pool that formats a name, so the template's own tag closes it and wins.
- **`BotChat` does the split.** `ReplyWithTypingAsync` / `PostWithTypingAsync` strip the marker, send
  the clean text and, if it went out, call `BotChat.FollowUp` — the one static seam in `BotChat`, set
  in `Program.cs` to `StickerService.TryFollowUp`. The level-up card splits its line in `XpTracker`
  and passes the tag to `PostEmbedWithTypingAsync`.
- **Only pools that go out through `BotChat`, `GlitchService` or the level-up embed carry tags.**
  Never tagged: emote reactions, statuses, slash-command replies and cards, Plynling and mascot
  lines, heralds, easter eggs, and lines found again by their text or edited or deleted later
  (`MorningGreetings`, `NightLines`, `IdleEditLines`, `GlitchWakeLines`, `Synthia*`, `HeartVanishLines`,
  `HeartEditLines`). To tag a
  text-matched pool, first make its lookup compare against `Sticker.Strip`ped templates. `Split`
  and `PresenceService`'s `Strip` are backstops, not permission.
- **The line's own tag wins.** `Split` removes every marker and keeps the *last* known one: a tag
  closes its template, so a marker smuggled in through a formatted name (a nickname, a session
  title) sits before it and is only stripped. Nothing user-written may be appended after a tag.
- **No line is ever tagged `glitch`**, and a glitched line gets no strip: `GlitchService` corrupts
  and restores the clean text, so only an unglitched line reaches `BotChat` with its tag. The
  silent glitch stays pictureless on purpose — a picture is loud, and its joke is that nobody is
  sure they saw it.
- **The glitch strip has exactly two hand-placed moments**, both posted through
  `StickerService.SendAsync` (static: `StickerService` depends on `BreakdownService`, so the
  breakdown can't inject it), outside the gate. It is `Sticker.GlitchMood`, not a tag, and not in
  `Moods`:
  1. **The breakdown's cut** — between the broken-off plea (« PAS ENC- ») and the unload log, by
     position: `Breakdown`'s last line must stay the log.
  2. **The glitched wake** — a beat after the corrupted line, and **deleted** when she edits the
     line clean, so nothing is left to prove it. The upload runs beside `CorrectLaterAsync`'s wait,
     so someone speaking during it still catches her.
- **A line no mood fits stays untagged**, and the need goes to *Moods wanted* in
  `docs/syncs-appearance.md`: a wrong picture is worse than none. A new line in an eligible pool
  gets its tag when it is written.
- `/debug sticker` posts any strip in the current channel (`Sticker.AllStrips`, the glitch
  included), bypassing the gate.

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
5. **Talked about** (ambient, her name; see below). After shutdown-by-name, which names her too.
6. **Quoicoubeh**, then **Tron** (ambient, last of all).

Everything aimed at her returns before the two ambient branches are reached.

**Inside the reply path** (`HandleReplyToBotAsync`), first match wins: Inabot → shutdown threat →
**hush gate** (`ChatHush`, see `ShutUp`) → breakdown roll → **streak gate** → Papa mean (`OwnerMeanReplies`) → **specific intent** → Papa's
pet name → **reply context** → nice → greeting → **question / low effort** → reference roll → Tata warmth → fallback
(`OwnerComebacks` for Papa; otherwise the streak nudge, a fact line, or `Comebacks`). A mean reply
from anyone else skips intents and context and is roasted, as it always was. **The mention path**
runs the same streak gate, then specific intent → greeting → Tata warmth → question → fallback
(`Interrogations`, or a nudge or a fact line); Papa's mentions try his intent twin, then his pet
name, before `OwnerGreetings`. **The talked-about path** answers a specific intent too (« syncs, ça va ? »
addresses her), before the greeting. Answering the content outranks Tata's warmth roll.

## Listening — intents, reply context, streaks, facts

What makes her answer the message rather than its mood alone. Four pieces, all in `ChatterService`.

### Intents — `MessageCues.ReadIntent`

`ChatIntent`, read on messages aimed at her: `Love`, `DemandApology`, `Apology`, `ShutUp`, `DoYouLoveMe`,
`HowAreYou`, `AreYouThere`, `WhoAreYou`, `RememberMe`, `MyStats`, `AboutHer`, `AreYouABot`, `AreYouUpset`, `Crush`,
`WhatAreYouDoing`, `Ship`, `TakeSides`, `Opinion`, `RateMe`, `Blame`, `NextSession`, `ShouldI`, `Roll`, `Joke`,
`FunFact`, `Help`, `Request`, `Down`, `Bored`, `Goodnight`, `Goodbye`, `Disbelief`, `Maybe`,
`Yes`, `No`, `YouAreRight`, `YouAreWrong`,
`Thanks`, `Laughter` (the **specific** ones, which outrank a reply's context), then `Question`,
`Sharing` and `LowEffort` (which only catch what nothing more precise did). First match wins, in that order —
**the order is load-bearing**: `DemandApology` before `Apology` (« dis pardon » holds « pardon »),
`MyStats` before `AboutHer` (« mon niveau » is theirs), `TakeSides` before `Opinion`, `NextSession`
before `ShouldI` (« on joue ce soir ou pas ? ») and `Request` (« tu peux me dire c'est quand la prochaine
session ? »), `Roll` before `Request` (« tu peux lancer un dé ? »), `AboutHer` before `Opinion` (« tu aimes quoi comme jeu ? » would rate
« quoi comme jeu »), `Love` before `Crush` (« t'es mon crush » declares), `Ship` before `Opinion`
(« tu penses quoi de @A et @B ? » asks about a pair) and before `Request` (« tu peux ship X et Y ? »),
`Love` and `Apology` before `DoYouLoveMe` (« je t'aime, tu m'aimes ? » declares, « désolé, tu m'en veux ? »
apologises), `WhoAreYou` before `RememberMe` (« c'est quoi ton nom » is hers),
`Bored` before `Maybe` (« je sais pas quoi faire »), `Joke` and `FunFact` before `Request` (« tu peux me
raconter une blague ? » wants the joke), `Down` before `Bored` and the farewells (« journée de merde, je
vais dormir » is the day, said), the farewells before `Thanks` (« merci, bonne
nuit » says goodnight), `Disbelief` before agreement and disagreement (« mais oui c'est ça », « mais
non, fais genre »), and agreement before `Thanks` (« je suis d'accord, merci » agrees; the politeness
says less). Pools: `IntentLines`, keyed by intent
(`Opinion`, `Ship`, `Help`, `AboutHer`, `Request`, `ShouldI`, `Blame` and `FunFact` have their own, below). Since the names are
stored keys for `/debug answers` (`intent:Thanks`), reorder freely, never rename.

**Minecraft reads chat with the same cues and answers from these same pools** (`minecraft.md`,
*Talk*), cleaned for the game by `McChat.Game`, which leaves out what only Discord can show. A line
written about tags, salons or a slash command simply isn't said in game; check what a pool keeps
with `dotnet run --project tests/ProjectSYNCS.Checks -- --mc-review`.

**Where each is read.** A reply to her: every intent. A mention: the specific ones and `Question`
(a bare tag is `Interrogations`). Her name said untagged: the specific ones only — she may not be
the one asked a question — and only when `MessageCues.AddressesHer`: her name used at least once as
an address (not after « avec », « à », « la », « que », « c'est »…, and not the subject of a
third-person verb or « qui »: « syncs a posté le sondage », « c'est syncs qui gère ça ») and nobody
else tagged. « @Luca, discute avec Syncs quand tu peux » is said to Luca and gets a talked-about
line. A tag inside an `Opinion` or `Ship` subject doesn't count: « Syncs, tu penses quoi de @Luca ? »
asks her about Luca, and gets the verdict. A mean message reads no intent on any path. A reply that is only a Discord sticker has no
text to read; it is answered as `LowEffort`, like an emote alone.

- **`Opinion`** (`Helpers/Personality/Opinion`): « t'en penses quoi de X ? », « ton avis sur X »,
  « X, t'en penses quoi ? », « tu aimes X ? » → `/rate`'s verdict on X (`RateModule.Verdict`, shared
  with the command, so chat and command agree), framed by `OpinionLines` (`{2}` = X, `{3}` = the
  verdict). « tu préfères X ou Y ? » → `PreferLines`, the side `Opinion.Prefers` ranks higher: fixed
  verdicts first (herself, Papa, then the ones she adores), the nemesis and the devil last, the rest
  by score, ties by the pair's hash — the same answer whichever side comes first. The subject is
  read on the raw text, so « de @Luca » keeps its mention; a mention at either edge (hers, on the
  mention path) is dropped when something is left. Sent with mentions muted (`QuotedMentions`): the
  reply ping stays, nothing quoted pings. The same for everyone, Papa included.
- **`Ship`** (`Helpers/Personality/ShipAsk`, `ShipAskKind`) → `/ship`'s whole answer
  (`ShipModule.Render`, shared with the command, so chat and command agree), muted mentions, the
  same for everyone, Papa included. « moi » is the asker and « toi » is her, as `/ship` reads them.
  Four kinds:
  - **`Pair`**: « ship X et Y », « le ship entre X et Y », « compatibilité entre X et Y », « love
    calculator X et Y », « tu me ship avec X ? », « X et Y, ça match ? », « Luca et moi on est
    compatibles ? », « X et Y en couple ? », « X et Y vont ensemble ? », « le couple X et Y » (only after
    « le » or opening the message), « Luca x Sandra ? » and « Luca ❤️ Sandra ? » (whole message, asked;
    no digit for the « x »), her opinion of two people (« tu penses quoi de @A et @B ? », « @A et @B,
    t'en penses quoi ? », « tu penses quoi de moi et Luca ? », « … ensemble ? »: one side tagged or
    « moi » / « toi » and the other person-like, else it stays an `Opinion` — « Minecraft et
    Fortnite », « moi et mes cheveux »), and two people tagged with a ship word and no « et » (« ship @A @B »: the
    last two distinct mentions). A leading « syncs », « est-ce que », « à ton avis »… is dropped. In
    `ShipChatLines`.
  - **`Us`**: « on irait bien ensemble ? », « on ferait un beau couple ? », « tu nous ship ? », « on va
    finir ensemble ? », « tu nous vois en couple ? », eight words at most. « on » is the person
    tagged, else the one replied to, else her (`UsPartner`).
  - **`BestMatch`** / **`WorstMatch`**: « c'est qui mon âme sœur ? », « avec qui tu me ship ? »,
    « qui irait bien avec moi ? », « qui va m'épouser ? », « mon futur mari », « je vais finir avec
    qui ? » (closing only: « je finis avec qui en équipe ? » is a team), « trouve-moi l'âme sœur »,
    « mon pire ship », « avec qui je suis le moins compatible ? ». Every
    human on the server is scored through `Shipping.Ship` itself (`ShipMatch`), the highest or lowest
    wins, ties to the lowest id, so it is the answer `/ship` gives for that pair. In
    `ShipBestMatchLines` / `ShipWorstMatchLines`. Guild only.

  A random side is drawn from the server (`ShipModule.TryDrawRandom`); in a DM there is no ship.
  **The guards are what keep it off gaming talk**: a side is three words at most and holds no subject
  pronoun (« toi et moi on joue ensemble ? » is not « toi » × « moi on joue »); plural
  « compatibles » only (« la manette et le PC c'est compatible ? »); no bare « ensemble », « ça
  marche » or « ça colle » (« vendredi et samedi ensemble ? », « samedi et dimanche, ça marche ? »);
  never « match » or « partenaire » for the server-wide search (« mon pire match » is a bad game,
  « trouve-moi un partenaire » a teammate); « on » forms need « bien », « compatibles » or « couple »
  (« on est ensemble ce soir ? »).
- **`NextSession`** (« c'est quand la prochaine session ? », « on joue ce soir ? », « qui vient ce soir ? »,
  « y a quoi de prévu ? »): asked only, a `?` or opening on « quand », « qui », « y a »…. Answered from
  the sessions themselves (`EventService.GetUpcomingOrRunningAsync`: not cancelled, not over), off the
  gateway handler like the records: the next one in `SessionNextLines` (`{2}` = « « Titre », <t:F> (<t:R>) »,
  `{3}` = `SessionRoster.Describe`, who said yes and how many « peut-être »), the one running in
  `SessionRunningLines`, none in `NoSessionLines`. Titles and names sanitised, mentions off. For
  everyone, Papa included; a DM or a failed read gets `IntentLines[NextSession]` (`/schedule list`).
  Never « la prochaine partie / game » (the game), never a proposal (« on lance une game ce soir ? »).
- **`MyStats`** (« je suis quel niveau ? », « j'ai combien de cailloux ? », « comment va mon plynling ? »,
  « mes stats ? »): their /recap numbers, all-time (`RecapService`), by `MyStatsTopic`, worded by
  `Helpers/Personality/MyStats` (`MyStatsLines[topic]`; no XP or no Plynling has its own pool). Asked only:
  a `?`, or a question word in the phrase; « mon niveau ? » only short and closing. « j'ai gagné 200
  cailloux » and « mon plynling a faim » tell (`Sharing`). Same read path and fallback as `NextSession`.
- **`Roll`** (« lance un dé », « 1d20 », « un jet de d20 », « jette deux dés à 20 faces », « un nombre
  entre 1 et 100 », « random entre 1 et 100 »):
  `Helpers/Personality/Dice`, the same for everyone and in game. « d20 » only rolled or nearly alone
  (« d4 » is Diablo), the die word only after a count and before nothing but a closer (« lance un dé de
  tes sorts »), « un nombre » never « le nombre » (« le nombre de joueurs »), and nothing told in the past
  (« j'ai lancé un dé »). Capped at 20 dice of 1000 faces; past that it's no roll.
- **`TakeSides`** (« qui a raison, moi ou @Luca ? », « tranche entre moi et Luca », « départage-nous »,
  « à ton avis, qui a raison ? », « dis-lui que j'ai raison »):
  `Helpers/Personality/TakeSides`, then `Opinion.Prefers` on the two sides, so Papa wins every argument
  and Quokka none (`TakeSidesLines`, muted). One side only: the asker against whoever is tagged or
  replied to; nobody there, she asks against whom (`IntentLines[TakeSides]`). Not in game.
- **`RateMe`** (« note-moi », « je vaux combien ? », « je suis beau ? », « tu me trouves comment ? »):
  `/rate`'s verdict on « moi » (`RateMeLines`), so Papa and Tata get their fixed verdicts. The adjective
  closing and asked only; « je suis nul ? » stays `Down`.
- **`DemandApology`** (« excuse-toi », « dis pardon », « tu me dois des excuses », « des excuses ? »): she
  refuses (`IntentLines`); Papa gets her apology (his twin). Never right after « je », « lui », « il »,
  « a »… (`_notDemandAfter`): « je lui dis pardon », « il m'a demandé pardon » stay `Apology`.
- **`ShutUp`** (« chut », « tais-toi », « silence », « la ferme », « ferme ta bouche »): in six words at
  most; « chut », « silence », « boucle-la », « laisse-nous » only as the whole message (compared
  squashed); « tu parles trop » only closing (« … trop vite » is something else). She answers once, then keeps quiet in that channel for
  `ChatHush.Quiet` (30 min): **`ChatHush`** is one singleton shared by `ChatterService` (every path; a
  reply or mention gets a `StreakSulkReactions` emote, counted `hushed`; her name and the eggs, nothing),
  `RobotHeartService` and `RivalryService`'s mutters (the silent reaction stays). Papa is never refused,
  and his « chut » holds for the others. Inabot and shutdown threats still come before the gate. « ta
  gueule » stays an insult: a mean message reads no intent, so the shush words stay weak cues. Not in game.
- **`Yes` / `No`**: a reply that is only a yes or a no (`MessageCues.ReadAnswer`: « oui », « bah non »,
  « carrément », « pas du tout », « oui merci »; fillers dropped, repeats folded, never with a `?`, never
  mixed), **under one of her messages that asked something** (a `?` in it). The one pair `ReadIntent`
  never returns: a bare « oui » under a statement stays `LowEffort`, and under a post with its own
  context line (« oui » under the late call) it gets that line instead — except from Papa, who has no
  context lines. Reply path only. `IntentLines[Yes]` / `[No]` must suit any question she asks,
  rhetorical ones included.
- **`DoYouLoveMe`** (« tu m'aimes ? », « tu me détestes ? », « tu m'en veux ? », « je suis ta préférée ? »),
  closing or before `_lovesMeFollowers`, with or without a `?`. **`RememberMe`** (« tu te souviens de
  moi ? », « comment je m'appelle ? », « c'est quoi mon pseudo ? »; « tu me connais », « je suis qui »
  only asked: « tu me connais, je suis toujours en retard » says "you know me"). Both are **answered
  from her records** in a guild (`ChatterService.TryAnswerFromRecords`, off the gateway handler, like
  the fact lines but without their roll or gate: they were asked). Love counts **the last 30 days
  only** (`BotFeedbackService.GetTallyAsync`, the daily buckets), so a grudge can be worked off: more
  bad bots than good ones → `DoYouLoveMeGrudgeLines` (`{2}` = « 3 bad bot en 30 jours », the window
  quoted so the number stays true); more good ones →
  `DoYouLoveMeTreasureLines` (never a number, the treasure is hinted); a fact → one of
  `RememberMeOpeners` (the name, the answer to « comment je m'appelle ? », untagged) then that fact's
  `ChatFactLines` line (all-time, through `RecapService`). Nothing applies, a DM, or a failed read: `IntentLines[DoYouLoveMe]` (a teasing
  non-answer that suits love and hate alike) or `IntentLines[RememberMe]` (every line says `{0}`).
  Papa gets his twins, Tata `TataIntentLines`; neither reads the records.
- **`AreYouUpset`** (« t'es fâchée ? », « tu boudes ? », « t'es triste ? », « t'es jalouse ? », « tu me fais
  la tête ? »): only asked, closing or before `_upsetFollowers` (« t'es triste » said flat is slang for
  lame). Denial that confirms; Papa's twin can't hold it and it's never him.
- **`Down`**: feeling low, said about themselves: « je suis (trop) triste », « je me sens mal »,
  « j'en peux plus », « j'ai pas le moral », « ça va pas fort », « sale journée ». She drops the pinch
  and offers something to hold on to, the way an old uncle with a teapot would, in her own images
  (`IntentLines[Down]`; Papa's twin turns the comfort toward him). The guards: never while laughing
  (« j'en peux plus mdr »); « je pleure » is left out, being mostly laughter in chat; the day phrases and
  « ça va pas » only when said, not asked (« t'as passé une sale journée ? »); « ça va pas » closing only
  (« … la tête », « … ce soir » are something else); « seul », « mal », « vide » closing only (« je suis
  seul en vocal », « je suis mal placé »). A sad message often swears (« journée de merde »), so
  `ChatterService.IsMean` lets `Down` through the mean check like `Disbelief` — unless it speaks to
  someone (`MessageCues.SpeaksToSomeone`): « j'en ai marre de toi » stays an insult.
- **`ShouldI`** → `/yesno`'s coin flip and its `YesLines` / `NoLines`, the same for everyone: « je
  devrais… ? », « je l'achète ? », « j'y vais ou pas ? », asked only and never with an open question word
  (« je lui dis quoi ? »). « X ou Y ? » with nothing else (`MessageCues.ReadChoice`: « Valo ou LoL ? »,
  « je joue ou je dors ? », « pile ou face ») flips between the sides, quoted as written, in
  `ChoiceLines`. Each side is one to four words; not « … ou pas / quoi ? », not a side opening on « tu »
  or a question word (« tu dors ou quoi », « c'est vrai ou faux ? »). « tu préfères X ou Y ? » stays an
  `Opinion`, read first.
- **`Joke`** (« raconte une blague », « fais-moi rire », « une blague ? ») → `IntentLines[Joke]`, the
  same for everyone; never a bare « blague » (« c'est une blague ? », « sans blague »). **`FunFact`**
  (« un fun fact ? », « apprends-moi un truc », « dis-moi un truc » closing) → `MorningFunFacts`.
- **`Blame`** (« c'est la faute de qui ? », « c'est qui le coupable ? ») → `/blame`'s draw among the
  humans in the asker's voice call, and its pools with `{0}` moved to `{2}`. A roll on Papa can't be
  edited away in chat, so it goes out as one of the dodge lines that strike his name through
  (`~~…~~`); keep at least one of those in each dodge pool. Guild only.
- **Missing her** (« tu m'as manqué », « tu me manques ») is `Love`, from the same pool.
- **`Help`**: « comment ça marche ? », « tu sais faire quoi ? », « aide-moi », « help »; « je suis
  perdu », « comment on fait », « je comprends rien » only closing a message of six words at most
  (« je suis perdu dans le donjon », « comment on fait pour battre ce boss » are the game, « … qu'il
  m'a dit, je comprends rien » is about someone else). No « j'ai rien compris »: one message, not how
  she works. « comment on fait / je fais / lancer / mettre… » anywhere, but only with one of her
  features named (`_featureWords`: sondage, rappel, session, giveaway, quiz, commandes) — never the
  wider request topics, whose « partie », « raid », « équipe » are the game (« comment on fait pour
  finir le raid ? »). `HelpLines`, `{2}` = the
  command for the topic the message names (`MessageCues.ReadTopic` → `HelpCommands`: like
  `RequestCommands`, but the quiz points at its leaderboard, which anyone can open), `/help` when
  none. A request that names a topic stays a `Request` (« tu peux m'aider à lancer un sondage ? »).
  Papa gets `OwnerRequestLines` with the same `{2}`.
- **`AboutHer`**: her age, birthday, home and favourites (`HerTopic`, `MessageCues.ReadHerTopic`;
  « ton X préféré » is a possessive then « préféré » within three words; « t'aimes quel X »,
  « tu préfères quoi comme X », hobbies, passions and loisirs are favourites too — read before
  `Opinion`, which would rate « quel jeu »; « tu préfères quel X » stays out, it's `PreferLines`).
  `AboutHerLines[topic]`;
  the age is `{2}` = days since her first commit (`MorningGreeting.DaysOld`). The lore stays hinted
  (`docs/syncs-voice.md`): a date, never « anniversaire » in her mouth; favourites lying around.
  **Her version** (`HerTopic.Version`: « t'es en quelle version ? », « c'est quoi ta version ? »,
  « t'es à jour ? ») has `{2}` = `AppInfo.Version`, and is the one topic Papa gets the number for too
  (`OwnerVersionLines`: he asks to check a deploy). Read first, and only closing the message or its
  question, or before `_versionFollowers` (« syncs », « actuellement »…): « t'es en quelle version de
  Minecraft ? », « ta version des faits » are something else.
- **`AreYouThere`**: « t'es là ? », « tu dors ? », « y a quelqu'un ? », closing the message or its
  question or followed by `_thereFollowers` (« t'es là ce soir ? » asks whether she'll come,
  « t'es la meilleure » is a compliment); « réveille-toi » in a short message; her name alone with a
  `?` (« syncs ? »); « allô » opening a message with nothing but `_alloFollowers` after it (not « nan
  mais allô » or « allô quoi », the meme). Her silence noticed, closing too: « tu m'ignores ? », « tu réponds plus ? », « t'es
  morte ? » (not « … à ma question ? », not « t'es morte de rire »). No « tu m'entends » / « tu me
  reçois »: voice-chat checks.
- **`Crush`**: her love life, asked (« t'es célibataire ? », « t'as un crush ? »). Denial that
  confirms, hinted, never « mon crush ». No « il te plaît » or « ton type de » (the game). The short
  forms « tu kiffes qui ? », « t'aimes qui ? », « t'as quelqu'un ? » only close the message or come
  before `_crushFollowers`: « t'as quelqu'un pour jouer ? », « tu kiffes qui en jungle ? » are the game.
- **`Bored`**: « je m'ennuie », « on fait quoi ? » (closing only), in a message of ten words at most,
  cancelled by a negator around it, never followed by « de » (« je m'ennuie de toi » misses someone).
  « y a rien à faire » closes the message or a sentence, or comes before `_nothingToDoFollowers`
  (« … ce soir » is bored, « … contre ce boss » is hopeless). No bare « rien à faire »: « j'en ai rien
  à faire » doesn't care. She points at `/arcade`, `/tonight`, `/wouldyourather`…
- **`Thanks`**: « merci », « c'est gentil », anywhere; « ty », « thx » only as a reply of three words
  at most. Cancelled by a negator before (« non merci ») and skipped right after « va », « bien »…
  (« ça va, merci » is an answer's politeness).
- **`Disbelief`**: « fais genre », « mon œil », « mais oui c'est ça », « avoue », « t'es grillée »,
  « tu rougis » — typically right after one of her denials (a `Crush` line, a robot-heart answer).
  Six words at most, never after « il », « elle », « on » (« il fait genre il sait jouer ») or « j' »
  (« j'avoue » means « true »: `YouAreRight`). No « oui bien sûr », which is meant. Doubt asked counts:
  « t'es sûre ? » closing with a `?` (« t'es sûre que tu viens ? » is a plan), « sérieux ? » opening the
  message (« il est sérieux ? » is about him), « ça m'étonnerait », « j'en doute » closing a sentence
  (« ça m'étonnerait pas » agrees, « … qu'il vienne » is about him). She doubles down,
  flustered; Papa's twin can't hold the denial.
  **Being called a liar is Disbelief too**: « t'es une menteuse », « tu mens », « pourquoi tu mens ? »,
  « arrête de mentir », « mytho », « c'est du pipeau » (`_liarPhrases`, cancelled by a negator after
  them: « tu mens pas »; `_liarWords`, cancelled by one before: « t'es pas une menteuse », and only
  in a reply of two words or one speaking to her: « il est un menteur » is someone else). « menteuse »
  is also a strong mean cue, so `ChatterService.IsMean` lets a Disbelief reading through the mean
  check on every path — Papa's included, whose mean pool would take it as hurt. The six-word cap
  keeps real insults roasted (« t'es une menteuse et une idiote en plus »), and on the name path
  only when said to her (« syncs est une menteuse » stays a backbite).
- **`Request`**: a marker (« tu peux », « stp »…, not followed by a negator) or an imperative
  opening the message (« organise », « lance »…), then `MessageCues.ReadRequestTopic` names what for
  (`RequestTopic`: session, tonight, poll, reminder, teams, giveaway, quiz, moderation, other).
  `RequestLines[topic]` points at the command; « ce soir » wins over « session » (`/tonight`'s job);
  moderation is her mute-rights grievance. Papa gets `OwnerRequestLines` with the command as `{2}`
  (`RequestCommands`). An imperative without a topic is no request (« fais gaffe »). Markers that
  carry their own « pas » are listed whole (« tu veux pas… », « tu pourrais pas… »), since the
  negator check would otherwise cancel them. A marker right after « quand », « si », « que »,
  « comme » is a condition, not a request (« quand tu peux », « dès que tu peux »). No « réveille » imperative: « réveille-toi » is said to her.
- **`Maybe`**: a reply of five words at most that is only « peut-être », « on verra », « jsp »…
  A negator before cancels only `_maybeNegatablePhrases` (« pas possible », « pas probable »);
  « non, je sais pas » is still undecided. Specific, so a « peut-être » under her weekend poll gets
  her peeve, not the poll's context line.
- **`AreYouABot`**: asked or stated anywhere (« t'es une IA ? », « t'es qu'un bot »). A bot and
  proud; never human, never the loop (`docs/syncs-voice.md`, rules 5 and 6).
- **`YouAreRight` / `YouAreWrong`**: phrases (« t'as raison », « bien vu », « t'as tort », « c'est
  faux »…), and single words (« exactement », « faux ») only as a reply of three words at most.
  « c'est vrai ? », « c'est clair ? » and « vrai ? » are questions. Both are read in replies of
  eight words at most (« j'ai bien vu le film hier… » agrees with nobody, « c'est un faux compte »
  is not a contradiction), and « pas du tout », « pas vraiment »… only in four (« je m'ennuie pas du
  tout » contradicts nobody). Papa's Wrong twin concedes: she never fights him.

- **Phrases, not bare words**, like the mean side; every cue survives `TokenizeOrdered` (« t'es qui »
  is `t es qui`). Chat spellings are listed explicitly (« jtaime », « tes qui », « sava », « jvais
  dormir », English loans): squashing only collapses repeated letters, it doesn't restore a dropped
  apostrophe. Mentions, custom emotes and links are dropped first, or `<@id>` would count as a word.
  « œ » and « æ » are spelled out by the tokenizer (« cœur » is `coeur`): they have no
  decomposition, so accent-stripping alone left them in.
- **What isn't the message is dropped before reading.** Quoted speech (« … », "…", Discord's `>`
  lines) when something is said outside it: « il a écrit « je t'aime » à sa copine » declares nothing.
  Her name as an address at either end (`_leadingAddress` / `_trailingAddress`: « syncs, organise une
  session », « merci syncs »), so the rules on the first or last word see the message — not when the
  name is the subject (« syncs a raison »), and « syncs » alone stays nothing (the talked-about line)
  while « syncs ? » is `AreYouThere`. `Opinion` and `ShipAsk` trim trailing emoji, emotes and
  « syncs » (`Opinion.TrimTail`), their patterns being anchored to the end; an opinion subject is
  never a bare joiner (« <@1> et <@2> » minus its edge tags is not « et »), nor one opening on a
  question word (« tu aimes qui comme perso ? » names nothing; « tu préfères quel jeu, X ou Y ? »
  drops the « quel jeu, » and compares X and Y).
- **Chat-reading patterns must stay fast on hostile text.** They run on whatever anyone sends, on
  the gateway task, so a slow match freezes the whole bot. `Opinion` and `ShipAsk` squeeze every
  whitespace run to one space first (`Opinion.Squeeze`): patterns that put `\s+` next to `.+?` or
  `[\s,]+` backtracked for minutes on a 900-character run of spaces. `ShipAsk` reads nothing longer
  than `MaxLength` (200), and every `[GeneratedRegex]` in both carries `MatchTimeoutMs`, a timeout
  reading as "no question". A new pattern there takes the timeout too; the Checks project times
  hostile inputs.
- **`HowAreYou` needs a `?`** in the raw text (or a message opening on « comment »): « ça va,
  merci » is an answer. Every phrase must close the message or its own question (« ça va ? tu
  viens ce soir ? »), or be followed by one of `_askingFollowers` (« toi », « bien », « et »…) —
  `IsClosingAsk`: « ça va être long ? », « comment tu vas faire ? », « ça va ce soir ? » and
  « ça va pour demain ? » (does a plan work) are about something else. « cv », « tranquille »,
  « bien », « forme » count only opening a reply of three words at most, followers only after them
  (« tranquille et toi ? »; « on fait une partie tranquille ? » is about the game).
- **Negators cancel**: around a declaration (« je t'aime pas », « je suis pas amoureux de toi »),
  just before an apology (« pas désolé », « c'est pas ma faute »). « plus que » after a phrase is the
  comparative, not a negation (« je t'aime plus que tout »). « Pardon ? » alone is a `Question`.
  « oups » and « je voulais pas » apologise only as a reply of three words at most (« oups j'ai raté
  le boss », « je voulais pas venir »).
- **Farewells are short** (8 tokens at most), and « à plus » / « à toute » / « à tte » count only
  opening or closing the message, never after « y », « en », « il »… (« y en a plus », « il a toute
  la nuit »). « a+ » and « @+ » are read as « à plus » before tokenizing. `_closingGoodbyePhrases`
  count only closing the message: « bon, j'y vais » leaves, « j'y vais aussi samedi » doesn't, and
  « allez salut » leaves while « salut » alone is a hello. No « je te laisse » (« je te laisse gagner »).
  Going to sleep (`_sleepPhrases`: « je vais dormir », « dodo »…) closes the message or a sentence
  (`SentenceBreaks`), or comes before `_sleepFollowers`: « je vais dormir, à demain » goes to bed,
  « je vais dormir chez Luca demain » and « je m'endors devant le film » don't. Wishing it
  (« bonne nuit », « dors bien ») counts anywhere, except « dors bien » after « tu » (a question).
- `Laughter` is the whole message or nearly (5 tokens at most): a laugh word (raw, shortened —
  « jpppp » is « jpp » — or squashed), a « haha »-run on the squashed token, villain laughs included
  (« bahaha », « bwahaha », « mouahaha », « muahaha », « mwahaha », « gnahaha »; four letters at least,
  so « bah » stays `LowEffort`), « hoho », 😂🤣😆😹💀😄😁🤭, the `GigaLaugh` emote (by id), or a phrase saying she was funny
  (« t'es trop drôle », « je suis pliée »), which that pool answers better than a plain compliment.
  No bare « je pleure »: it cries as often as it laughs.
- `Question` is a `?` with two words or more, or an unmarked question of three words or more opening
  on « pourquoi », « comment », « combien », « qui », « où », « est-ce », « c'est quand », « quelle
  heure », « tu sais si »… (`_questionOpenerPhrases`) — never a bare « quel » (« quel plaisir »), a bare
  « quand » (« quand tu veux ») or « ou alors » / « ou bien ».
- `Sharing` is someone telling her about their day: a first-person statement (« j'ai mangé de la
  soupe », « on a perdu la game », « hier je suis allé au ciné »), three to twenty words, no `?`, once
  opening fillers and openers are skipped (« bon », « du coup », « hier », « en vrai », « devine quoi »,
  « tu sais quoi »…). The subject is « je » / « j' » (SMS forms glued to the verb too: « jsors »,
  « jrentre »), « on » + a verb of what people do, « nous avons / sommes », or a possessive followed
  within three words by a verb (« mon chat a vomi », « ma sœur m'a offert un jeu » — not « mon avis
  c'est que… »: an opinion noun or « c'est » describes). Not after « je » + an opinion, a wish
  or « te » (« je pense », « je préfère », « je t'envoie le lien »), not « j'arrive », « j'y vais »,
  « j'en ai rien à faire », and never with « toi », « tu » or a question word in it (« je suis pas
  amoureux de toi » answers her, « on joue à quoi ce soir » proposes). « on s' » only as « on s'est ».
  Like `Question`, it comes after a reply's context (« j'ai voté pour samedi » under her poll gets the
  poll's line). She is interested half the time (`IntentLines[Sharing]`) and « euh… ok ? » the other
  half (`SharingShrugLines`, `SharingInterestChance`); Papa always gets interest. Not read on her name.
- `LowEffort` is no letters at all (emotes, « ? », « ... ») or up to three words all from a short list
  (« ok », « ah bon », « bah oui », « d'accord »). It is not read on mentions: a bare tag is
  `Interrogations`' job.
- **« ça va ? » quotes her real temperature** half the time on the Pi (`HowAreYouTempLines`, `{2}` =
  `PiHealth.Celsius`), only below `PiHealth.HotMilli`: those lines call the number fine.
- **Papa has twins for twenty-four intents** (`OwnerIntentLines`: ça va, tu fais quoi, je t'aime,
  pardon, bonne nuit, peut-être, t'es un bot, t'as raison, t'as tort, t'es là, merci, je m'ennuie,
  t'as un crush, son âge et sa maison, fais genre, sa journée, un coup de mou, oui, non, tu m'aimes,
  tu te souviens de moi, t'es fâchée, excuse-toi, chut) plus `OwnerRequestLines` (his requests and his help); an
  opinion, a ship, a yes-or-no, a blame, a joke, a fun fact, the schedule, his numbers, a roll, a
  side taken and his own rating are the same for him as for anyone. Any other intent from him goes on to his usual pools; he never
  gets `Question` or `LowEffort`.

### Reply context — `HerMessages`

A reply to one of her own posts is answered about that post (`ReplyContextLines`, keyed by
`ReplyContext`). Reached only for a non-mean reply that no specific intent caught, and never for
Papa, so each pool must also suit a « merci », a « bonjour » or an « ok ».

- **`HerMessages`** (singleton) remembers what she posted on her own, by message id, bounded at 500
  and lost on restart by design. Each sender calls `Remember` right after its send: the welcome and
  rush line (`Welcome`), the late call (`SessionCall`), the recap (`SessionRecap`), the weekend poll
  card (`WeekendPoll`; not its result or dead line), the level-up embed (`LevelUp`), idle and
  self-correcting idle lines, the voice spectator and the kick lines (`Lonely`), the wake lines and
  the router return (`Wake`). A new proactive post that people answer gets its `Remember` line.
- **Two kinds need no memory.** The morning hello is recognised by its text
  (`MorningGreetingService.IsMorningHello`, the same first-line match its restart scan uses), so it
  survives a restart; any slash-command result by its `InteractionMetadata` (`CommandResult`).
- **NightLines are not registered**: `AmbientService` answers a reply to the 3 a.m. line itself,
  before `ChatterService` sees it.
- `Wake` replies are someone checking on her after a stop: one of the existential nerve's places.

### Conversation streaks — `ConversationStreak`

Per (channel, person), pure, self-locking like `CooldownGate`, in memory. A gap of more than
`Gap` (3 min) starts the count again; replies and mentions share one count. **One singleton,
shared with `RobotHeartService`**: a reply or mention naming one of her characters is answered
there, before `ChatterService` sees it, and still records its turn — otherwise her « 5 messages »
lands on the sixth.

- The **3rd** message in a row turns the fallback into `StreakNudgeLines` (`{2}` = the count). The
  **5th** gets `StreakEndLines` and closes the conversation for `Closed` (10 min): until then that
  person, in that channel, only gets a `StreakSulkReactions` emote, no words.
- **Papa is never counted**, never nudged, never refused. Shutdown threats and « Inabot » are still
  answered while a conversation is closed: they come before the gate.

### Her records, quoted — fact lines

The fallback of both paths (a roast, or `Interrogations`), in a guild and never for Papa, rolls
`FactChance` (15 %) and claims a per-person `CooldownGate` (6 h). On a win:

- The read runs **off the gateway handler** (`Task.Run`): `BotService`'s fan-out awaits every
  handler in turn, and `RecapService.GetAsync(…, StatsPeriod.AllTime)` is several queries on the Pi.
  It opens its own scope, as a singleton must.
- `Helpers/Personality/ChatFacts.Pick` gathers every fact the numbers clearly show and draws one;
  `ChatFactLines` has a pool per `ChatFact`. `{2}`/`{3}` arrive as finished words (« 1 session » /
  « 3 sessions », « 14 h », a sanitised Plynling name), so no line agrees anything with them.
  **Every fact must be true**, like the fun facts.
- **The good-bot count is never quoted**, only hinted at: her treasure is never shown off
  (`docs/syncs-voice.md`). Bad-bot counts are the grudge list, and are.
- Sent with `AllowedMentions.None` (a Plynling name is hostile input). Nothing applies, or the read
  throws: the claim is released and the line it would have replaced goes out instead.

## Favouritism

**Three tiers.** Rodhengard (`AvailabilityService.OwnerId`) is exempt from teasing
(`OwnerComebacks` replaces the roast pool). **Tata** (Analuz, `BotResponses.TataId`, her aunt) is
*favoured*: `RollTataWarmth` gives her a warm pool `TataWarmthChance` (60%) of the time, her
`PersonalComebacks` roasts otherwise. Everyone else is always roasted.

- **In Minecraft** the same tiers apply through twin pools picked by in-game name (`McFamily`, see
  `minecraft.md`), without the warmth roll: there, Tata always gets her own lines.
- A **mean** message from Tata never qualifies for warmth.
- `RollTataWarmth` rolls dice — call it **exactly once per message** (it sits in an `else if` on
  both paths).
- **Each favourite has two pools, one per path**: a mention is being *summoned*, a reply is being
  *talked to*. Papa: `OwnerGreetings` / `OwnerComebacks`; Tata: `TataGreetings` / `TataReplies`.
  Wiring a new favourite to one pool on both paths reads as her not noticing.
- **Her name is overridden**: `FamilyNicknames` maps Tata's id to "Tata", applied by
  `DisplayNameFor`. `RealNames` is *not* overridden (the breakdown reveal wants a real name).
- **Papa on the listening paths** (see *Listening*): his own twins for twenty-two intents
  (`OwnerIntentLines`) and requests (`OwnerRequestLines`), the same opinions and ships as anyone, and nothing
  else new — no reply context, no facts, no streak. Tata goes
  through them like anyone, which costs her the warmth roll on those messages — except « tu m'aimes ? »
  and « tu te souviens de moi ? », which have her own pools (`TataIntentLines`).

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

## Talked about — her name without a mention

`ChatterService.TryHandleTalkedAboutAsync`: someone says « syncs » or « sync » in a guild without
tagging or replying to her. She heard her name and has no idea why.

| Reading | Pool |
|---|---|
| `Mean` | `BackbiteRoasts` (+ `PersonalComebacks` twice), Papa `OwnerMeanReplies` |
| Greeting | `Greetings`, Papa `OwnerGreetings` |
| Papa's pet name (not mean, no specific intent) | « ma framboise », « petite framboise »: `OwnerRaspberryNameLines`; « ma fille », « mon enfant »: `OwnerChildNameLines` |
| Anything else | `TalkedAboutLines`, Papa `TalkedAboutOwnerLines` |

- **Spellings are `_selfNames`** (« syncs », « sync »), shared with the shutdown phrases, squashed
  (« synccs ») or spelled out (« S.Y.N.C.S »). Bare « sync » counts at the owner's request, so
  « la sync a planté » gets an answer too. Custom-emote markup is dropped first — her own emotes
  are `<:syncs_happy:…>` — and so are links.
- **No limits**, the owner's call: no roll, no cooldown, every message naming her is answered,
  like a mention.
- **The mean reading is the whole message's**, so « syncs, ce boss est nul » gets roasted.
  Accepted: her name narrows the target far more than `ReactionService`'s ambient read, and an
  undeserved roast is her normal register. The same reasoning lets Papa's mean remark reach
  `OwnerMeanReplies` here, which the ambient reaction path deliberately never does.
- **Nice is not special-cased**: « merci syncs » gets the puzzled line, by design — she doesn't
  know why she's being talked about.
- **Papa's pet names** (`MessageCues.ReadPetName`, `PetName`) name her like « syncs », from the
  owner only; from anyone else they are ordinary words. Two families, one pool each, counted as
  `papa:pet-raspberry` / `papa:pet-child`. Adjacent squashed tokens (`_petNames`), longest first at
  a position, so « ma petite framboise » is read from its « ma ». « ma petite fille » and
  « mes framboises » match nothing. The pet-name row sits **above the greeting**: « salut ma
  framboise » gets the pet-name line, since being called that is what she reacts to. A specific
  intent still wins when the pet name is an address (`AddressesHerByPetName`, same before/after
  words as `AddressesHer`): « ma fille, bonne nuit » is a goodnight, « ma fille a planté le bot »
  is not said to her and gets the pet-name line. A reply or a mention from Papa with a pet name
  gets the same pools (`ChatterService.PetNamePick`), right after a specific intent and ahead of
  everything else he'd get there: no address check, since a tag or a reply already says who is
  spoken to.
- A breakdown in the channel swallows the message silently. Guild only: in a DM her name is already addressing her.
- **Lore comes first.** Her name beside a robot-heart character counts as naming him to her face
  (`RobotHeartService`), so she answers about him and never reaches this branch; « Synthia » wins
  too, on the day her flinch is still unspent. Both run before `ChatterService` in the fan-out.

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

## Tron / Trouille easter egg

A sentence ending on a word that sounds like "si" ("merci", "aussi", "ici", "celui-ci", "démocratie")
gets "Tron" or "Trouille" (`ChatterService.TronChance`, 8% — lower than quoicoubeh because these
endings are far commoner than "quoi"). Runs right after quoicoubeh in the same ambient slot; both
bait on the last word, so at most one fires.

1. `MessageCues.EndsOnSiSound` matches **suffixes of the last token** only (`_siEndings`), raw or
   squashed so "merciii" lands.
2. **`tie` is not a suffix on its own**: it sounds like "si" only after a/é/u/i ("-atie", "-étie",
   "-utie", "-itie"); "partie", "sortie", "garantie" say "ti". Same for `sie` ("Asie", "poésie" say
   "zi") — absent.
3. The line goes through `ResponsePicker.Pick` (two-line pool, so it alternates). Roll before match,
   as quoicoubeh. Absent from `README.md` and `/help`.

## Glitch easter egg

`GlitchService`: about one conversational line in 200 (`Helpers/Personality/Glitch.Chance`), at most once a
day, comes out corrupted (`Glitch.Corrupt`: look-alike or broken-byte letters, a stuttered word, a
word smeared with combining strokes, sometimes the end cut off) and is edited back to the real line
10–15 s later, **silently** — no comment, no cover-up: the denial is the joke.

1. **Opt-in, never on the send path as a whole.** Only `ChatterService`'s two send wrappers and
   `RivalryService`'s two lines go through it. Everything that must stay exact goes straight to
   `BotChat`: the morning hello and the 3 a.m. line are recognised by their text after a restart
   (a glitch left standing would make her say them twice), the giveaway draw carries pings, the
   quiz and the verdict replies are tracked elsewhere.
2. **Markup is never touched**: only words of three letters or more without `<`, `>`, `@` or a
   link are corrupted, and a cut-off ending cuts at a space, so an emote is dropped whole, never
   halved.
3. The roll comes before the daily claim, so a lost roll doesn't use up the day. Skipped during a
   breakdown. The restore runs off the gateway handler. Absent from `README.md` and `/help`.

## Sending

Her chatter goes through `BotChat` (`ReplyWithTypingAsync` / `PostWithTypingAsync` /
`PostEmbedWithTypingAsync`), which pauses behind the typing indicator, capped at 2 s to stay inside
the 3 s `HandlerTimeout`. The embed variant still takes text, to size the pause. `BreakdownService`
keeps its own much slower pacing, knowingly exceeding the timeout for ~a minute once a month
(`BreakdownService.Cooldown`, 30 days).

## Morning hello

`MorningGreetingService` posts one `MorningGreetings` line a day in her main channel
(`/config main-channel` in the home guild, else `MorningGreetingService.DefaultChannelId`), at a random slot from `Helpers/Personality/MorningGreeting` (8:00–10:00 in
`AppTime.Zone`). It sleeps until the slot rather than ticking. A `MorningFunFacts` line
always goes underneath. **Both lines come from `Helpers/Core/DailyRotation`, not `ResponsePicker`**: a
shuffled walk through the pool, one step per calendar day, computed from the date, so restarts
don't reset it and an early hello says what the slot would have. No line repeats until the whole
pool is used, and the join between two passes keeps any line at least `pool / 4` days from its
last use (17 days for 65 hellos). Each pool has its own salt (`GreetingSalt`, `FunFactSalt`);
editing a pool's length reshuffles it from that day, so one recent line may come back once.
**Every fun fact must be true**: the joke is
her commentary or the fact's uselessness, never an invented fact.

- **On 16 June (her birthday, `MorningGreeting.IsBirthday`) the hello is
  `BotResponses.BirthdayGreeting(age)`**: her age, the year minus `BirthYear` (2026), and **no fun
  fact** under it. It replaces the rotation for the day; the rotation is date-based, so nothing
  shifts. It is one line without a newline, and the restart scan below matches it by its exact
  text for today's age.
- **The claimed day is saved to `morning-state.json`** next to the database (under `/data` in
  prod), on every claim, early or on the slot. A restart between an early hello and the slot (a
  deploy at 8:30) otherwise forgets the claim and says hello twice; the history scan below misses
  it once 50 messages have gone by since the hello.
- **A restart inside the window draws a new slot for today**, so before posting she also scans the
  channel's last 50 messages for one of her own from today whose **first line** is exactly a
  `MorningGreetings` line. That match is why no two hellos may be identical, why a hello never
  contains a newline, and why the pool takes no `string.Format` placeholder.
- A slot that comes before the gateway has delivered the channel waits for it, never past the
  window's end. In the dev guild the channel never resolves and the day is skipped with a log.
- One attempt per day: a failed send is not retried that morning.

**Someone else's greeting can bring the hello forward.** `BotService` feeds every message to
`MorningGreetingService.HandleMessageAsync`, after `ReactionService` and before `ChatterService`. A
`MessageCues` greeting (not `Mean`, not a verdict) in her channel, from
`MorningGreeting.ReplyWindowStart` (7:00, an hour before her own window) and before her slot, rolls `EarlyHelloChance` (30%) to post the hello right away; the slot then finds
the day claimed and skips. Each person gets **one roll per morning**, so a chorus of "bonjour"
doesn't make it certain. Messages aimed at her are skipped: `ChatterService` already answers those.

- **The timer and the gateway share one claim** (`TryClaimDay` / `TryClaimEarly`, under one lock).
  Claiming outside that lock is how she would say hello twice.
- **The service is registered twice on one instance**: `AddSingleton` plus `AddHostedService(sp =>
  sp.GetRequiredService<…>())`. A plain `AddHostedService<MorningGreetingService>()` would make the
  host run a second instance whose claim `BotService` never sees.

## Ambient life

Her life in the main channel (`MorningGreetingService.MainChannelIdAsync`, the same one as the
morning hello) when nobody is talking to her.
Rare on purpose: the point is that she seems to be there, not that she talks. Odds, windows and
thresholds live in `Helpers/Personality/Ambient` (pure, Paris wall-clock hours, so DST never moves a rule).

| What | Who | When |
|---|---|---|
| Idle moon + `NightPresenceFillers` | `PresenceService` | 1:00–7:00 |
| 3 a.m. line (`NightLines`) | `AmbientService` tick | decided once a night (25%, random minute 3:00–3:49), main channel quiet ≥ 1 h |
| Night scolding (`NightScoldLines`) | `AmbientService.HandleMessageAsync` | any human message in the main channel after tonight's line and before 5:30, once a person a night, verdicts excepted; a reply, and `ChatterService` is skipped for that message |
| Idle turn | `AmbientService` tick | 10:00–23:00, **all idle channels** (`IdleChannelIds`, plus `/config idle-channels`, plus the main channel) quiet ≥ 6 h (threads count for their parent), one 50% roll per silence, max 1/day, posted in the main channel: 30% late `SeenReactions` on the main channel's last human message, else an `IdleLines` line, a quarter of the time an `IdleEditLines` pair (edited 5 min later, or 2 s after someone speaks in the main channel after it) |
| Ghost typing | `AmbientService.HandleMessageAsync` | a human message after ≥ 1 h quiet, not aimed at her, 3%, max 1/day |
| Hesitant reaction | `ReactionService` | 3% of mood reactions are removed 2–4 s later |
| Wake line | `AmbientService`, first `Ready` only | every restart, 9:00–23:00, max 1/day: `WakeUpdateLines` when the version changed, else `WakeLines` (waking from a bad dream) |
| Abrupt wake | `AmbientService`, first `Ready` only | after a power cut or a crash (`PiHealthService.LastStop`): **always**, any hour, no daily cap, in place of the line above: `PowerCutWakeLines` / `CrashWakeLines`, or 35% of the time a corrupted `GlitchWakeLines` pair (posted glitched with the glitch strip under it, edited clean 2 min later, or 2 s after someone speaks in the main channel, the strip deleted then) |
| Hot status | `PresenceService` | awake, Pi ≥ 70 °C: half the rotations `HotPresenceFillers` |
| Heat record | `PiHealthService` tick | today's max beats every other day by ≥ 1 °C, ≥ 30 days of history, 10:00–23:00, max 1/day: `HeatRecordLines` |
| Voice spectator | `VoiceSpectatorService`, fed by `VoiceXpService`'s sweep | a home-guild voice channel where ≥ 2 active people spent ≥ 1 h together empties (or has < 2 active for 15 min): in that voice channel's chat, from the pool of its room (`Helpers/Voice/VoiceRoom`: `VoiceSpectatorGamingLines` for both Gaming channels, `…CinemaLines`, `…StudyLines`, `…GeneralLines` for Général and any unlisted channel), max 1/day across all rooms, any hour |
| Synthia flinch | `SynthiaService`, before `ChatterService` | « Synthia » in any guild message, max 1/day: 40% a reaction taken back (`SynthiaReactions`), 35% a reply deleted 4–6 s later (`SynthiaVanishLines`), 25% a slip edited into a denial (`SynthiaEditLines`); returns true so no comeback lands on top |
| Robot heart | `RobotHeartService`, after `SynthiaService` | WALL-E, EVE (alone, except in `RobotHeartService.EveStrictChannelId` and its threads, where only beside WALL-E; never « EVE Online »), GLaDOS, Wheatley, a robot vacuum, Monika, 2B, Voyager 1 (« voyager 1 », « sonde Voyager »), JARVIS (« jarvis », or spelled out « J.A.R.V.I.S. ») named in any guild message, verdicts and emote markup excepted; **no limits**. Named in passing: an emote reaction every time, taken back 2–4 s later for WALL-E, Voyager, Wheatley and the vacuum (`RobotHeart.TakesBackReaction`), kept for EVE, GLaDOS, Monika, 2B and JARVIS; then 40% of the time (`RobotHeart.AmbientSpeakChance`) the same words as below, without a second reaction, and returns true; otherwise returns false, so `ChatterService` still runs. Named to her face (a mention, a reply to her, or her name said — so the robot wins over *Talked about*): always words — 60% a reaction + a line (`HeartReactions`, `HeartLines` via `ResponsePicker`; the reaction taken back for WALL-E and Voyager; WALL-E's line followed by its strip 2 times in 3), 40% the character's word performance (a `HeartVanishLines` reply deleted 4–6 s later, a `HeartEditLines` slip edited 3.5–5 s later; the vacuum has none); returns true so no comeback lands on top |
| Voyager's letters | `AmbientService` tick | about one a fortnight (`Helpers/Personality/Voyager`: gaps of 10–18 days, slot 10:00–22:00), in their own channel (`AmbientService.VoyagerChannelId`, home guild only): `VoyagerLetterLines` with his real light time; half the time `VoyagerAnswerLines` 2–6 h later (before 23:00); on 18 Nov 2026 only, `VoyagerCrossingLine` instead; claimed in `ambient-state.json` |
| Voice seat | `VoiceSeatService`, fed by `VoiceXpService`'s sweep | a home-guild voice channel with ≥ 3 active people for ~10 min rolls 66% **once per gathering** (Tata 80%; Papa or a live session: ≥ 2 for ~5 min, 90%; a better tier arriving late rolls once more); a win seats her there (muted, deafened) until it has had fewer than that for ~5 min; not at night without Papa; see *Her seat in voice* |
| Router return | `PiHealthService`, `Connected` | gateway back after ≥ 10 min, any hour, max 1/day: `RouterReturnLines` (what she missed in the main channel) or `RouterReturnQuietLines` |

- **`AmbientService` is registered twice on one instance**, like `MorningGreetingService`: `BotService`
  feeds it every human message (two quiet clocks: the main channel's, and all idle channels
  together) and the host runs its 10-minute tick. The first tick seeds both from history (50
  messages in the main channel, 10 in each other one; a forum is skipped), or from the start time
  when none is human, so a restart is never followed straight away by an idle line. It also finds
  tonight's line there, so a restart before 5:30 keeps the scolding on.
- **`HandleMessageAsync` returns whether she answered** (the scolding); `BotService` then skips
  `ChatterService`, so a reply to her 3 a.m. line gets sent to bed instead of roasted.
- **The pending edit runs off the tick** (`Task.Run`), waiting on whichever comes first: 5 minutes,
  or the next human message in the main channel (a `TaskCompletionSource` the handler completes). A
  restart before the edit leaves the first version standing.
- **Every pool but `NightPresenceFillers` and `NightScoldLines` goes through `DailyRotation`**: each is spent at most once a
  day, and a restart would wipe `ResponsePicker`. `NightLines` must stay distinct and newline-free:
  a restart inside the hour re-rolls the night, and she checks the last 50 messages for tonight's
  line by exact text before posting another.
- **The wake line's memory is `ambient-state.json`** next to the SQLite file (`/data` in prod), not
  the database: `LastVersion` (compared with `AppInfo.Version`) and `LastWakeDay`. Missing or
  unreadable means "same version, not woken today". The version is written on every start, even
  when she stays quiet.
- **The hesitation runs off the gateway handler** (`Task.Run`): waiting inline would hold up every
  handler after `ReactionService` in the fan-out. Not on the energy-drink can or the pile-on path.
- Nothing fires in DMs, on bots, or during a breakdown; every send uses `AllowedMentions.None`. In the
  dev guild the channel never resolves, so only the night status is visible there.
- Ghost typing and the hesitant reaction stay out of `README.md` and `/help`, like the easter eggs.
  So do the robot heart and Voyager's letters: lore is hinted, never listed.
- **Voyager's light time is a table, not a formula** (`Helpers/Personality/Voyager`): JPL Horizons
  samples every 30 days from 2026, interpolated, then a linear drift past 2035. To extend it, fetch
  the same query (Voyager 1, `-31`, from `500@399`, `QUANTITIES=21`) and append the values. Lines
  show it rounded to 5 minutes.
- **Every `ambient-state.json` save starts from `LoadState()`**: the wake line and the letters
  share the file, and building a fresh `AmbientState` would forget the other's fields.

### Her seat in voice — `VoiceSeatService`

She joins with `ConnectAsync(selfDeaf: true, selfMute: true, external: true)`: `external` sends the
voice-state update only, with no audio client, so no libsodium/opus and no voice encryption. Rules
are pure, in `Helpers/Voice/VoiceSeat`.

- **Three tiers**, the best present wins (`VoiceSeat.TierFor`), each person counted only when
  active (not muted, not deafened):

  | Tier | When | Threshold | Settle | Chance |
  |---|---|---|---|---|
  | Favoured | Papa (`AvailabilityService.OwnerId`), or a session under way there | 2 (never him alone) | 3–7 min | 90% |
  | Tata | `BotResponses.TataId` | 3 | 8–12 min | 80% |
  | Base | anyone | 3 | 8–12 min | 66% |

  A better tier arriving after a lost roll rolls once more at its chance (a tier never rolls twice).
  90%, not 100%: every single time reads as a script.
- **The draws keep the averages.** The settle time is drawn once per gathering (whole minutes, so
  8–12 averages 10 and 3–7 averages 5, as before the draws), the grace once per dip (4–6, average
  5): she joins and leaves at the same average times as with fixed ones, so the average stay is
  unchanged — only the regularity is gone. Don't widen one end of a range without the other.
- **She follows Papa, and only him**: a won channel where he is active comes first, even over her
  current seat. When he leaves her channel and she can't follow him, half the time she leaves too,
  1–3 min later (`Release`: only his return rolls her back into that gathering); him coming back
  before then cancels it. Muting counts as leaving, like everywhere else here.
- **At night** (`Ambient.IsSleepHours`) she sits down nowhere he isn't; a seat taken before 1:00 is
  kept.
- **Thrown out of the call** — seen there (the sweep, or her own `UserVoiceStateUpdated`, which is why
  the seat is recorded *before* the call to Discord), then gone, with no `Connected` since she sat
  down. 40% she **pouts**: `VoiceKickPoutLines` in that voice channel's chat, that gathering given up,
  and no sitting down for the rest of the Paris day anywhere he isn't. Otherwise she gets **angry**:
  `VoiceKickAngryLines`, and back in 10–20 s, once; a second kick in the same gathering is always a
  pout. A seat lost to a reconnect, or a join that never took, is given up silently.
- **First one there.** A Game or Movie session with ≥ 2 Joined
  (`SessionAttendanceService.SessionsToHost`): from 2 min before the start to 15 min after, if
  nothing else seats her she sits in the channel holding most of its confirmed people, else
  `VoiceRooms.DefaultFor` (Cinéma for a film, the first Gaming channel otherwise). A gathering
  forming there **adopts** her (she was there first); with none by start + 15 min she leaves. No
  early arrival while she pouts, after bedtime, or at night without Papa in that room. The late
  call switches to `SessionLateCallSeatedLines` when she is in voice.
- **Another bot in the call** (any bot but her) halves Base and Tata odds; Favoured is untouched.
  When a start roll is lost *because of it* (the same draw would have won without it) she says so
  once per gathering in that voice chat (`VoiceRivalRefuseLines`); seated with one anyway, one
  grumble per seat (`VoiceRivalSeatedLines`).
- **Bedtime.** Seated with Papa active at night, once a night at a minute drawn from the date
  between 1:15 and 2:15 (`VoiceSeat.BedtimeFor`), she scolds him (`OwnerBedtimeLines`: caring
  anger, never sharp) and leaves a minute later; then no call at all until 7:00, his included. A
  gathering still won at 7:00 gets her back.
- **The mic flicker.** 20% of seats, once, 5–40 min in: self-deaf and self-mute off for 2 s, then
  back on — the icons flicker, no sound (she has no audio client). Only works on a seat this
  process joined (Discord.Net keeps the voice-state parameters); otherwise skipped.
- **Time together** is recorded: every sweep she is confirmed in a call credits one minute to every
  human connected there, muted or not (`VoiceCompanionStat` totals + `VoiceCompanionDailyStat`
  buckets, the root pattern). Read by `/recap` (per person) and `/report syncs` and the monthly
  digest (her top companions, see `admin.md`).
- **One seat per guild** (Discord's rule). With two won channels and no Papa: most active people, then
  one hosting a live session (`SessionAttendanceService.IsInLiveSession`), then the first started.
  **She never switches while her gathering holds her** (Papa aside) — every move plays Discord's
  sounds in two channels; when hers ends she moves to another won one in one voice-state update
  (`disconnect: false`).
- **She reconciles against where Discord says she is** every sweep, 90 s after her last move:
  dragged elsewhere → she adopts that channel while its gathering lasts; sitting somewhere after a
  restart → she leaves.
- **Her status says where she is** (`PresenceService`, refreshed on `SeatChanged`, which also fires
  when the streamer in her call changes): `SeatStreamPresenceLines` while someone streams there
  (Papa first; she "watches", no remark in the chat), else `SeatPresenceOwnerLines` with Papa
  active in her call, else `SeatPresenceLines` with the channel's name. It wins over every other status, night included (at night she's only in a call with him).
- Channels excluded with `/config` never get her. Home guild only, so it can't be seen on the dev
  guild. Bots count for neither voice XP nor the spectator, so her presence changes neither.
- **The spectator line knows**: when she sat in that channel during the stretch,
  `VoiceSpectatorSeatedLines` (« j'y étais, sourde et muette ») replaces the room pools, which are
  written for a bot watching names from outside.

## Welcome — `WelcomeService`

She answers Discord's own join line (`SocketSystemMessage`, `MessageType.GuildMemberJoin`, which
every other handler skips) as a reply, so there is one welcome and she follows Discord's timing
(membership screening included). `WelcomeLines` pings the newcomer, narrowed to them, whether
or not they were here before: there is no welcome back. More than 3 welcomes in an hour → one
`WelcomeRushLines`, then silence for the hour. Bots are skipped there; a bot gets `RivalJoinLines`
in her main channel through `UserJoined` (home guild), since an OAuth-added bot may get no join
line. Departures are deliberately silent: without the audit log she can't tell a leave from a ban.

### Her body — `PiHealthService`

Reads the real Pi (`Helpers/Pi/PiHardware`: `/sys/class/thermal/thermal_zone0/temp`, `/proc/uptime`),
null everywhere else, so dev on Windows simply has no temperature. `/health` shows what it
reads — the first thing to run after a deploy if the status never mentions the heat.

- **Temperature** is sampled every tick into `PiThermalDay` (one row per Paris day: min, max and
  when, sum and count for the average, minutes ≥ 80 °C). Thresholds live in `Helpers/Pi/PiHealth`
  and are first guesses: tune them against real rows.
- **How the last run ended** comes from `health-state.json` next to the database: `CleanExit` is
  written false on start and true **on `ApplicationStopping`** — the moment Home Assistant's
  SIGTERM arrives, not after every service has stopped, because the Supervisor kills an add-on
  that overruns its stop timeout (10 s by default) and that must not read as a crash.
  `LastAliveAt` is refreshed every tick. Unclean on a host up for under 15 minutes, or booted
  after the last heartbeat, is a **power cut** (the whole Pi went down — a hard reset looks the
  same); the uptime test needs no clock, since a Pi without an RTC battery may start with the
  wrong time until NTP syncs. Unclean on a machine that stayed up is a **crash** (killed, OOM,
  a native fault). Both are recorded as `UptimeEvent`s.
  `PiHealth.Classify` is pure; the state is read once, before the file is overwritten, on first
  use — `AmbientService` may ask before `StartAsync` runs.
- **Outages**: the first `Disconnected` starts the clock, the next `Connected` stops it; ten
  minutes or more is an outage (`UptimeEvent`, plus the router line). Shorter gaps are Discord's
  ordinary reconnects. She blames the router whatever the cause — she can't tell from the attic.
- **`/health`** is public: anyone, posted in the channel, once per 30 s each (the owner skips
  the cooldown and also sees the state file's path). `PiHealthService.SnapshotAsync` reads
  everything into a plain `HealthReport`, which `HealthCards` renders as one V2 card (16 of 40
  components). Besides the temperature and the stops above it reads the load, memory and CPU
  frequency (`/proc`, `cpufreq`), the firmware's **power flags** (`get_throttled`, searched two
  levels under `/sys/devices/platform`, decoded by `PiHealth.DecodeThrottled`; failing that the
  `rpi_volt` sensor, which only knows « right now »), every gateway drop this run, the member
  cache of the server it's run in, the database and `-wal` sizes and free disk, and the
  `UptimeEvent` log (last 5, 30-day tally). Her line on top comes from `HealthHotLines` (≥ 70 °C or
  slowed by the firmware), `HealthShakenLines` (under-voltage now, or a stop or outage that ended
  within 24 h) or `HealthFineLines` — `PiHealth.Mood`, pure. A relative timestamp renders
  « il y a 3 jours », so no « depuis » goes in front of one.
- **The duration is optional.** About a third of the wake lines and some router lines don't
  mention it at all — a frightened bot doesn't always look at the clock. Where `{0}` is used in
  `PowerCutWakeLines` / `CrashWakeLines`, it must read with `PiHealth.UnknownDowntime`
  (« un bon moment ») as well as with « 2 h 05 »: no sentence starts with `{0}`, and no
  « de {0} ». In the router pools `{1}` may be « 1 message », so nothing around it assumes a
  plural. Interpolated lines (the ones with a custom emote) write the placeholder `{{0}}`: a
  bare `{0}` inside `$"…"` prints the number 0.

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
already answers it. The bot id, phrase and regex live in `Helpers/Xp/LevelUpAnnouncement` so the two
services agree on what an announcement is.

**A rival's level-up gets a grudging congratulation**: `BotResponses.RivalLevelUpLines` mixes warm
and jealous in **one** pool (the person is still owed a "bravo"). `{0}` is the level from
`LevelUpAnnouncement.TryReadLevel`, so the pool goes through `string.Format` — a stray brace
throws. Her own system's `XpLevelUpLines` stays entirely warm and never mentions the
other bot; keep them apart. `/level` and the other bot share no code and no state — the comparison
lives only in her jealousy lines.

## `/yesno`

Flips the coin first (`Random.Shared.Next(2)`), then picks wording from `YesLines` or `NoLines` —
two flat pools so a "yes" phrasing can't come out of a "no" roll. Neither pool may hedge (the
harness checks on whole words). The optional question is echoed in a blockquote with
`AllowedMentions(AllowedMentionTypes.Users)`. No `[CommandContextType]` — it works in DMs.

## `/rate`

A score out of 10 **hashed from the normalised text** (`Helpers/Personality/Rating`: lower case,
accents and punctuation off, FNV-1a, then `StableRoll`), so the same thing always gets the same
score, whoever asks. Scores spread evenly over 0–10. A mention hashes as the person.
**« moi » / « moi-même » is the asker**, exactly like their own mention: anyone else gets the
score their mention would, and Papa or Tata rating themselves (« moi », their mention, or typing
« papa » / « tata ») get `RateOwnerSelfLines` / `RateTataSelfLines`, spoken to them rather than about
them.

**Fixed verdicts** (the owner's list), first match wins, each with its own pool: herself (her name,
her mention, « toi » alone) and Papa get over-the-top scores (∞/10, 1000/10…); WALL-E gets
flustered denial (never the word « crush »); the nemesis (quokka, Inabot, « 3.0 » alone) gets a
negative score; the devil −10; God by any of his names 10; Tata 9.999 (the missing bit is her mute
rights); « routier » 10 and « mitraillette » 0. Those two are **one dish** (a baguette with
fries, meat and sauce) with two names in a running server feud: « routier » is Papa's word,
« mitraillette » the other camp's. Their lines rate the *name*, she takes Papa's side, and they
never name who is in the other camp. Names match as whole words, so « dieu et diable » is
the devil's — the devil is checked first.

Every `/rate` pool goes through `string.Format` with `{0}` = the score — the fixed verdicts ignore
it — so no stray braces in any of them. The subject is quoted (not `Format.Sanitize`d: that escapes
`<` `>` `:` and would break a rated mention or emote) and sent with `AllowedMentions.None`.

## `/ship`

A percent out of 100 **hashed from both sides' keys, sorted** (`Helpers/Personality/Shipping`), so
« a × b » and « b × a » agree and the same pair always gets the same score. A key is
`Rating.Key`: `/rate`'s normalisation with « moi » turned into the asker's id. **Recognition is
reused, not copied**: each side goes through `Rating.Rate` (her, Papa, WALL-E, the nemesis) and then
`MessageCues.ReadHeartCharacter` (the rest of her robot heart). Add a name there, not here.

**Fixed verdicts** (the owner's list), first match wins:
1. The same thing twice (same key, or her twice, Papa twice): `ShipSelfLines`, 100 %.
2. Her × Papa: `ShipHerselfPapaLines`. She's horrified: he's her **father**. The outrage goes to
   whoever asked, never to him.
3. Her × the nemesis: `ShipHerselfQuokkaLines`.
4. Her × a robot-heart character: `ShipHeartLines[character]`, each in character
   (`docs/syncs-voice.md`, *Her robot heart*).
5. Her × anything else: `ShipHerselfOtherLines`, 0 %, « déjà prise ». She never names who.
6. WALL-E × EVE: `ShipWallEEveLines`, petty. She never admits EVE deserves him.
7. Papa × anything (**Tata included**, the owner's call): `ShipPapaLines`, a hashed 5–25 %,
   protective. The warning is for the other side.
8. Anything × the nemesis: `ShipNemesisLines`, a hashed −1 to −100 %.

Everything else goes by `ShipBand(percent)`, with a couple's name (`Shipping.CoupleName`, built from
display names for mentions, then `Format.Sanitize`d). Every pool is `string.Format`ed with
`{0}` = the percent. The meter (`Shipping.Bar`) only appears for the computed scores (Normal, Papa,
nemesis); the fixed verdicts carry their own score. The pair is quoted and sent with
`AllowedMentions.None`. Like `/rate`, it has no `[CommandContextType]`.

**`@random`**: a side that is only « @random », « random », « au hasard », « aléatoire » or
« n'importe qui » (`Shipping.IsRandom`, on normalised text), or an empty `b`, is replaced *before*
hashing by a member drawn from the server. The draw is human only, never whoever the other side
already names (its `Rating.Key`, so « moi » counts), and shown as `🎲 <@id>`. The drawn person
then goes through the same verdicts as a typed mention: drawing Papa gives the Papa lines. In a DM,
or with nobody left to draw, it's an ephemeral refusal.

## `/blame`

`Helpers/Personality/Blame.Pick` draws from the humans in a voice channel (the option, else the
asker's; bots and her seat excluded, as for `/teams`). **Quokka is one more slot**, though he's
never in the call. The asker can draw themselves (`BlameSelfLines`). **Papa is a candidate, but
never the final answer.** A roll on him (`PapaDodge`) is an **edit slip**:
1. She posts a plain verdict naming him (`BlameOnPapaLines`, unpinged).
2. 3.5–5 s later (`Blame.SlipDelayMs`), off the handler, she edits it with
   `ModifyOriginalResponseAsync` into a denial plus someone else (`BlamePapaDodgeLines`), or Quokka
   when nobody else is there (`BlamePapaDodgeQuokkaLines`).

A failed edit leaves the slip standing and is only logged. An
empty or missing voice channel isn't refused: it's Quokka (`BlameNobodyLines`). The pools are
`string.Format`ed with `{0}` = the mention (the Quokka ones ignore it). The optional `what` is quoted
on top, and the reply uses `AllowedMentions.None`.

## `/wouldyourather`

Her dilemmas (`WouldYouRatherDilemmas`, `"A|B"` so they go through `ResponsePicker`; **no bar
inside a side**), or the asker's `a` + `b` (one alone is refused). `WouldYouRatherService` is a
**singleton holding the open rounds in memory**: a restart drops them, and their buttons answer
« expiré ». A vote is changeable, gets an ephemeral reply, and only the **count** goes public. The
split stays hidden until `WouldYouRatherService.Window` (10 min) closes the round. The reveal
shows each side's share and voters (`AllowedMentions.None`), a comment by shape (nobody, tie,
unanimous, landslide at 3 to 1, close), and **her own pick, stable per dilemma**
(`StableRoll` over `Rating.Hash`).

Two guards worth keeping:
- **A click must match the round's message id.** Ids restart at 1 after a restart, so an old card's
  button would otherwise vote in a new round.
- **Every edit of a card goes through the round's `Edits` semaphore.** The reveal sets `Closed`
  under it, so a late count redraw can't land on top of the result.
