# Personality — listening: intents

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

## Listening — intents, reply context, streaks, facts

What makes her answer the message rather than its mood alone. Four pieces, all in `ChatterService`.

### Intents — `MessageCues.ReadIntent`

`ChatIntent`, read on messages aimed at her: `Love`, `DemandApology`, `Apology`, `ShutUp`, `DoYouLoveMe`,
`HowAreYou`, `AreYouThere`, `AnswerMe`, `WhoAreYou`, `RememberMe`, `MyStats`, `AboutHer`, `AreYouABot`, `AreYouUpset`, `Crush`,
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
  squashed); « tu parles trop » only closing (« … trop vite » is something else). A « non » opening the
  message refuses what she said and doesn't cancel the hush (« non, tais-toi »). She answers once, then keeps quiet in that channel for
  `ChatHush.Quiet` (30 min): **`ChatHush`** is one singleton shared by `ChatterService` (every path; a
  reply or mention gets a `StreakSulkReactions` emote, counted `hushed`; her name and the eggs, nothing),
  `RobotHeartService` and `RivalryService`'s mutters (the silent reaction stays). Papa is never refused,
  and his « chut » holds for the others. Inabot and shutdown threats still come before the gate. « ta
  gueule » stays an insult: a mean message reads no intent, so the shush words stay weak cues, and « tais »
  is no mean cue at all (« TAIS-TOI ! » would reach the threshold on emphasis alone); an insult beside it
  still counts. Papa's twin is barely a word, a little hurt (« ... », « Désolée... », an emote). Not in game.
- **`Yes` / `No`**: a reply that is only a yes or a no (`MessageCues.ReadAnswer`: « oui », « bah non »,
  « carrément », « pas du tout », « oui merci »; fillers dropped, repeats folded, never with a `?`, never
  mixed), **under one of her messages that asked something** (a `?` in it). The one pair `ReadIntent`
  never returns: a bare « oui » under a statement stays `LowEffort`, and under a post with its own
  context line (« oui » under the late call) it gets that line instead — except from Papa, who has no
  context lines. Reply path only. `IntentLines[Yes]` / `[No]` must suit any question she asks,
  rhetorical ones included.
- **Open answers** (not an intent): a reply of one to five words, unasked, not about her (no « tu »,
  « toi »…), that no intent read (`MessageCues.ReadShortAnswer`: « Java », « 8 jours », « ça s'appelle
  comme ça »), under one of her messages with a `?`, gets `OpenAnswerLines` instead of a roast, its
  words quoted back as `{2}` (sanitised, muted, always inside « »). After question / low effort, so
  « ok » stays `LowEffort` and her post's context line still wins; never Papa, never a mean reply.
  Counted `open-answer`. Like the yes-or-no pools, every line must suit a rhetorical question.
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
  morte ? » (not « … à ma question ? », which is `AnswerMe`, not « t'es morte de rire »). No « tu
  m'entends » / « tu me reçois »: voice-chat checks.
- **`AnswerMe`**: the answer she didn't give, wanted. « réponds à ma question », « t'as pas répondu »,
  « j'attends ta réponse », « tu esquives » (closing or before `_answerMeFollowers`: « tu esquives bien
  ses attaques » is the game), « réponds ! » alone, or any form of « répondre » with « ma question »
  (« pourquoi tu réponds pas à ma question ? »), never after « il », « elle », « on », « personne »
  within three words. Fifteen words at most. Read after `AreYouThere`, so « tu réponds plus ? » stays
  there. She didn't forget, she chose (`IntentLines[AnswerMe]`); Papa's twin is sorry she missed it.
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
  Like `Question`, it comes after a reply's context (« j'ai joué tout l'après-midi » under the late
  call gets the late call's line; under a card, « j'ai voté pour samedi » never gets here, see
  `listening.md`, *Under her cards*). She is interested half the time (`IntentLines[Sharing]`) and « euh… ok ? » the other
  half (`SharingShrugLines`, `SharingInterestChance`); Papa always gets interest. Not read on her name.
- `LowEffort` is no letters at all (emotes, « ? », « ... ») or up to three words all from a short list
  (« ok », « ah bon », « bah oui », « d'accord »). It is not read on mentions: a bare tag is
  `Interrogations`' job.
- **« ça va ? » quotes her real temperature** half the time on the Pi (`HowAreYouTempLines`, `{2}` =
  `PiHealth.Celsius`), only below `PiHealth.HotMilli`: those lines call the number fine.
- **Papa has twins for twenty-five intents** (`OwnerIntentLines`: ça va, tu fais quoi, je t'aime,
  pardon, bonne nuit, peut-être, t'es un bot, t'as raison, t'as tort, t'es là, réponds-moi, merci, je m'ennuie,
  t'as un crush, son âge et sa maison, fais genre, sa journée, un coup de mou, oui, non, tu m'aimes,
  tu te souviens de moi, t'es fâchée, excuse-toi, chut) plus `OwnerRequestLines` (his requests and his help); an
  opinion, a ship, a yes-or-no, a blame, a joke, a fun fact, the schedule, his numbers, a roll, a
  side taken and his own rating are the same for him as for anyone. Any other intent from him goes on to his usual pools; he never
  gets `Question` or `LowEffort`.

