# Personality — listening: two-step messages, reply context, cards, streaks, facts, answer stats

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

### Two-step messages — `ChatListening`

« syncs ? » then « t'es en quelle version ? »: the Discord twin of her Minecraft body's call and
answered windows (`minecraft-orders.md`, *An order can come in two steps*). One singleton, per (channel,
person), in memory, `ChatListening.Window` (60 s).

- **A call** is her name or tag and nothing else (`MessageCues.IsBareCall`, shared with
  `McTalkReader.IsBareCall`: « syncs ? », « hé syncs », a bare tag), marked on the talked-about and
  mention paths. That person's **next message only** is read as a reply to her, whatever it is.
- **An answer**: every line she sends through `ReplyWithTypingAsync` keeps her listening to the
  person she answered. Within the window, a message is heard only when it plainly asks her
  something: a specific intent `TryIntent` answers, not mean, and not one the room shares
  (`RoomIntents`: a laugh, « peut-être », « t'as raison », a goodbye), or a shutdown threat.
- **Never** a message said to someone else (a reply to another message, another person, a role or
  everyone tagged), nor while hushed (Papa excepted) or during a breakdown.
- **Read as a reply, not a mention** (`HandleReplyToBotAsync`): the mention pools speak of being
  tagged. With no message replied to, it reads no post context and no yes-or-no. The streak treats a
  follow-up like any reply: it counts only if it wears her patience.

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
- **Some kinds need no memory.** The morning hello is recognised by its text
  (`MorningGreetingService.IsMorningHello`, the same first-line match its restart scan uses), so it
  survives a restart; her cards by their buttons (`Poll`, `Session`, `Tonight`, `Giveaway`; see
  *Under her cards*); any other slash-command result by its `InteractionMetadata` (`CommandResult`).
  The cards are checked before `CommandResult`: `/tonight` and `/giveaway` cards carry metadata too,
  and « le verdict est définitif » doesn't answer a « merci » under a giveaway.

### Late replies — `LateReply`

A reply to one of her messages written 6 h or more ago (`Helpers/Personality/LateReply.MinAge`)
gets, half the time (`LateReply.Chance`), `LateReplyLines` (Papa: `OwnerLateReplyLines`) with `{2}`
saying when she wrote it (« il y a 7 heures » the same Paris day, « hier », « il y a 3 jours », « la
semaine dernière », « il y a 2 semaines », « il y a des mois »). Counted as `late-reply`.

- **Only in place of a stock comeback**: it sits after every branch that answers something (intents,
  context, nice, greeting, sharing, questions, open answers) and before the reference roll, Tata's
  warmth and the comebacks. Never on an insult, which is roasted.
- **About her own old words, never the person's timing**: no « enfin », no « en retard » (checks
  section 47). Why they answer late is theirs, as with « ça faisait longtemps ».
- Every `{2}` reads with all the phrasings (« J'ai dit ça {2} », « C'était {2} », « commencée {2} »):
  never « de {2} » (« de hier »).

### Edited or deleted after she answered — `ReplyWatchService`

Every reply `ChatterService` sends (its `ReplyWithTypingAsync`) is watched for an hour
(`ReplyWatchService.Window`, in memory, 300 at most), with the text she answered and whether she read
it as mean (`ChatterService.IsMean`). `BotService` feeds it `MessageUpdated` and `MessageDeleted`.
She then rewrites her reply after 1.5–3.5 s: her old words struck through, line by line, and under
them one line (`EditReading.Struck`; just the new line past 2000 characters), mentions off.

| What happened (`EditReading.Read`) | Everyone | Papa |
|---|---|---|
| Deleted | `EditDeletedLines`: she read it, now she answers nothing | `OwnerEditLines` |
| Rewritten | `EditRewrittenLines`: suspicious | `OwnerEditLines` |
| Turned mean | `EditTurnedMeanLines`: pout or anger | `OwnerEditMeanLines`: hurt |
| Was mean, softened | `EditCoveredUpLines`: too late, she read it | `OwnerEditLines` |

- **Only a real change** (`EditReading.IsRealChange`, checks section 48): case, accents and spacing
  aside, a third of the text moved and at least 3 characters, or a negation or a yes/no appearing or
  going (« je viens » → « je viens pas »). A typo or a link preview loading (Discord reports it as an
  edit, same text) keeps the watch for the next edit. The mean reading turning either way always counts.
- **One rewrite per reply**: the watch is taken under the lock before anything is sent.
- Her old words are the line she meant (sticker marker stripped), not what a glitch made of it. The
  glitch's own restore could overwrite a rewrite landing in its few seconds: accepted, both are rare.
- Not during a breakdown in that channel. Only replies through `ChatterService`: the other services'
  answers (verdicts, cards, the 3 a.m. scolding) are not watched.

### Under her cards — `CardReplyService`

A reply under a poll, a vote, a session, `/tonight` or a giveaway card is usually not said to her:
people rally the room, say they voted, ask when it is, or answer in text. **The card is recognised
by its buttons' custom-ids** (`Helpers/Scheduling/CardRefs`: `poll:vote|close|tosession:{id}`,
`event:*:{id}`, `tonight:*:{id}`, `giveaway:enter|leave:{id}`), which carry the row's id, so it
survives a restart and needs no lookup. A card that lost its buttons (drawn, cancelled, started,
expired) is not recognised and reads like any reply. Renaming one of those custom-ids breaks this.

`MessageCues.ReadCardReply` (`MessageCues/MessageCues.Cards.cs`) reads `CardReplyIntent`, first match wins:

- **`Repost`**: « remonte-le », « republie », « re-poste », « bump », « reup », « up » (three words at
  most); « renvoie », « ramène », « remets », « rebalance », « redescends » only about the card (a
  pronoun next to them, the card named, or closing the reply: « ramène des chips », « remets la
  musique » are not; a noun after a possessive is someone's own, « remets ton vote »); a place verb
  with a place (« mets-le en bas », « remets-le à la fin »); buried (« on le voit plus », or the card
  named as « tout en haut », « noyé », « perdu », but not of a person: « je suis perdu avec ce
  sondage »). Never right after a subject (« tu remontes à quelle heure ? », « on remonte la pente »)
  or an auxiliary (« je l'ai remonté », « il a été remonté »: « remonté » reads « remonte »). A
  negator after cancels (« le remonte pas »), not before (« tu peux pas
  le remonter ? » asks). She replies, then `CardRepost` posts the card again at the bottom of the
  channel, so it is the last message.
- **`Rally`**: « votez », « faut voter », « venez », « bougez-vous », « personne a voté », « et
  vous ? » anywhere; the singular (« vote », « viens », « clique », « et toi ») and asking whether
  they did (« t'as voté ? », « tu viens ? », « t'es chaud ? ») only with another human tagged, and
  the imperatives never after « j' », « a », « le »… (`_notRallyAfter`: « voté » reads « vote »,
  « le vote » is a noun). Tags and nothing else (« @Luca @Sandra », « @Luca 👀 »), or a short
  question to someone tagged (« @Luca chaud ? », four words at most), point people at the card too.
  She names the tagged people who haven't answered the card (`CardRallyNamedLines`), says they all
  have (`CardRallyAllDoneLines`), or with nobody tagged quotes the count (`CardRallyLines`). One
  rally line per card every 5 minutes: three people urging the room get one backing, not a chorus.
- **`Done`**: « j'ai voté », « c'est fait », « c'est voté », « j'ai coché », « je me suis inscrit·e »,
  « on a tous voté », or bare « voté », « fait », « c'est bon », « voilà », a ✅ alone (bare only
  when nobody is tagged: « @Luca a voté » is about Luca). Checked against the card:
  `CardDoneSeenLines`, or `CardDoneLiarLines` while it is open.
- **`Status`**: « c'est quand ? », « qui vient ? », « on en est où ? », « qui a gagné ? », « y a
  combien de votes ? », « alors ? » alone; unasked only opening a reply of six words at most (« c'est
  quand », « qui vient ce soir »). Each phrase names the card's own business: no bare « quand »,
  « combien » or « quelle heure » (« il est quelle heure ? », « t'as fait combien de kills ? »), and
  « résultat » never as an opener (« résultat, je suis mort »). → `CardStatusLines`, `{2}` the card
  in words (poll: voters and the leader; session: time and `SessionRoster`; `/tonight`: chauds,
  peut-être, time; giveaway: entries and the draw).
- **`TextAnswer`**, not asked, ten words at most: « samedi pour moi », « je peux pas », « dispo »,
  « j'y serai », « compte sur moi », « ça me va », « moi », « pareil », « +1 », « go ». « chaud »
  is keen except after « fait », « est », « être » (the weather, « c'est chaud »); « je peux
  voter / cliquer… » asks. A day counts only among the words of a choice (« plutôt dimanche »,
  « samedi ou dimanche », « ça va ce soir »), never said in passing (« je dors chez Luca ce soir »,
  « à demain »). `CardTextAnsweredLines` if they clicked too, else `CardTextClickLines`.

**The same repost asked with no card replied to** (« syncs remonte le sondage », « @SYNCS remets la
session en bas », or under one of her other posts) goes through `MessageCues.ReadNamedRepost`: a
`Repost` as above **with the card named** (`sondage`/`vote`/`poll` → a poll, `session`/`tonight`/
`event` → a session or `/tonight`, `tirage`/`giveaway`/`concours`, `carte` → any). « remonte-le » or
« up » alone points at nothing outside a reply. `CardReplyService.TryRepostNamed` takes the newest
open card of that kind **in this channel only** (by message id) and answers as if it were a reply
under it; none there → `CardRepostNoneLines`. **Asked for every one** (`NamedRepost.All`: the noun
in the plural, « remonte les sondages », or « tout » / « tous » / « toutes » within two words of
the verb, « remonte tous les sondages », « remonte-les tous », « tu peux tout remonter », which alone
means any card), she brings back every open one there, oldest first so they keep their order,
after one `CardRepostAllLines` line (`{2}` the count); each card keeps its own 5-minute gate, and all
of them fresh → `CardRepostTooSoonLines`. « tout » as a determiner or adverb doesn't count
(« remonte tout le monde », « remets-le tout en bas »), nor « pour tous » far from the verb. Never from another channel: moving the weekend poll is the `/… list` menus'
job. On the name path it needs `AddressesHer` (« syncs a remonté le sondage » is news); on the
mention path it runs before Papa's branch and respects a « chut » for everyone else.

**`dotnet run --project tests/ProjectSYNCS.Checks -- --card-review`** lists the everyday sweeps'
messages that would read as a card reply; each must be one a card reply would really be.

Any intent but `Status` on a card that is over gets `CardClosedLines`. **It sits right after the
hush gate**, before the breakdown roll and the streak gate: someone rallying the room is not in a
conversation with her. A mean reply skips it and is roasted; a reply it doesn't read goes on down
the path, where the card's context line is the fallback. Papa gets the same as everyone (facts and
reposts are the same whoever asks). The DB read runs off the gateway handler; titles are sanitised
and everything goes out with mentions muted (the reply ping stays). Counted `card:<Intent>`.

**`CardRepost`** (`Services/Scheduling/`) is the one repost path, shared with the `/when list`,
`/vote list`, `/schedule list` and `/giveaway list` menus: delete the old card, redraw it through
the module's builders, store the new location (and point a session's native event at it). Open to
anyone, like the menus; a repost that throws gives the 5-minute gate back.
- **NightLines are not registered**: `AmbientService` answers a reply to the 3 a.m. line itself,
  before `ChatterService` sees it.
- `Wake` replies are someone checking on her after a stop: one of the existential nerve's places.

### Conversation streaks — `ConversationStreak`

Per (channel, person), pure, self-locking like `CooldownGate`, in memory. It counts **what wears her
patience, not how much someone talks**: replies and mentions that are mean, or read as nothing or as
a low-effort word with nothing else to answer them (`ConversationStreak.Wears`). Real talk is free,
because a real question or a thanks shouldn't end in « conversation over »: a specific intent, a
generic question, news, a kind word, a greeting, a line about her own post, an answer to her question,
a character from her robot heart. A free message neither adds to the count nor keeps it alive. A gap
of more than `Gap` (3 min) between counted messages starts it again. Her name said untagged isn't
counted either (that path answers once and listens; see `people.md`).

- The **3rd** gets `StreakNudgeLines` whatever it would have got, **in the gate**, so every ending
  has had its warning. The **5th** gets `StreakEndLines` and closes the conversation for `Closed`
  (10 min): until then that person, in that channel, only gets a `StreakSulkReactions` emote, no
  words, free messages included.
- **`{2}` is how many of those messages, not how many messages**, so a line never says « d'affilée »
  or « de suite » (the Checks project pins it): they may have said something else in between, and
  people do count. **At most one nudge in four quotes the number**; the rest pout or hold the grudge.
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

### Requests she acts on — `ChatterService.Requests`, `ChatRequest`, `ChatOffers`

A `Request` for a session, a `/tonight` call, a vote, a reminder or teams (`personality/intents.md`,
*Request*) is answered with the thing, not a pointer to the command. It hooks into
`TryAnswerFromRecords`, so all three paths (reply, mention, name) get it, off the gateway handler.

- **`Helpers/Personality/ChatRequest` reads the details, pure** (checks section 50). The time is the
  run of words `SessionTime.Parse` / `ReminderTime.Parse` accept that says the most (date words, a
  number breaking ties, then the tightest); a run never ends on a filler (« à 18h la lessive » keeps
  « la »), needs a letter, `:` or `/` (« 3 équipes » is no hour), and a lone chat word FrenchDate also
  reads as a day (« jeu », « sam », « mer »…) isn't one. The title is what's left once the request's
  own words are stripped from both ends, rebuilt from the original spans. Never a date parser of its
  own: a misread is fixed in `FrenchDate`, with its check.
- **Stripping never eats what was asked.** An article with a capital mid-sentence starts a name and
  stays (« Les Sims », « A Way Out »). A reminder's head ends at the link after its verb (« de »,
  « d' », « à », « que »): what follows is theirs even when it reuses the asking's words (« de pas
  écouter Ina », « de me coucher », « de rappeler Paul »), and its end strips only politeness.
- **What would show in public is offered, never done**: her line (`RequestActionLines[topic]`, Papa
  `OwnerRequestActionLines`, `{2}` what she read) and one button, `chatreq:session:{id}`,
  `chatreq:vote:{id}` or `chatreq:tonight:{id}`. The offer is in `ChatOffers` (singleton, in memory,
  an hour, gone on restart: the click then says so and names the command). **Only the asker may
  press** (`RequestNotYoursLines`, private).
- **Her answer is public, so no flow may edit it.** A component's `DeferAsync` is an *update* of the
  message it sits on, and a modal opened from it updates that message on submit. So the session click
  answers with a new private message (`DeferLoadingAsync` then the preview, or the time step, or a
  « Lui donner un nom » step whose modal opens from that private message), and the vote click with
  the vote's own private wizard message (the review step straight away when she read a title and two
  options). `/tonight`'s card is posted in the channel; her button then greys out (« Appel lancé »).
- **What stays private is done at once**: a reminder is saved (`/remindme`'s range and its 15-pending
  cap; « rappelle-moi demain 9h » with nothing to remind quotes the message's link) and her answer
  carries « Annuler » (`chatreq:remundo:{id}:{asker}`); teams are dealt in the asker's voice channel
  (`TeamsModule.DrawAsync`, same card and « Remélanger »), or `TeamsNoVoiceLines`.
- **Anything she can't do falls back to the pointer line** (`RequestLines`): no time she can read for a
  reminder, one out of range, too late for tonight, or a read that throws.
- These answers don't go through her reply watch (an edit of the request would strike through a line
  whose button still works) and count under `request:{topic}` in `/debug answers`.

### Messages she passes on — `Relay`

`ChatIntent.Relay` (`Helpers/Personality/Relay`, `ChatterService.TryRelay`), read first of all intents: « dis à @Luca que c'est un caca »,
« tu peux dire à @Luca qu'il est en retard ? », « répète à @Luca : t'as gagné », « dis à @Luca de venir »,
« préviens @Luca que… », « rappelle à @Luca que… » (with « à » only: « rappelle @Luca demain » is a reminder),
« dis bonjour à @Luca », « passe le bonjour à @Luca de ma part », « fais un coucou / envoie des bisous à
@Luca », « souhaite bonne nuit / un bon anniversaire à @Luca », « dis merci à @Luca ». « dit » counts as
« dis » (how it's typed); one target only (« à @A et @B » would need « vous dire »). The target is
a **tag**, never a name typed out; never a past tense (« j'ai dit à @Luca que… » tells). She delivers
it (`RelayLines`, `{2}` = the target's tag, `{3}` = the words as said, keeping their « que » / « qu' » / « de » / « d' », or
« : « … » » after a colon, so `{3}` always follows « dire » / « dise »; greetings in
`RelayGreetingLines`, `{3}` the greeting alone, inside « »). **Pings that one person only**
(`AllowedMentions` with that user id, nothing else, whatever the words tag) and sanitises the words:
she is nobody's way of mass-pinging. Words that read as mean (`Analyze` on the words alone) are
refused (`RelayRefusedLines`, the target shown muted): she won't carry an insult, and must not roast
the one who asked (Papa gets these too). Not to her, the asker, a bot or someone not tagged in the
message, and not when another person's tag opens the message (said to them). The same for
everyone; 300 characters read at most, 200 passed on, every pattern on a timeout. Not in game.
The words sit inside her own sentence rather than in a `MessageFormat.Quote` block: every line
names who sent them, which is what the blockquote is for elsewhere.

On her name path the relay is read before `AddressesHer` (the tag is who to tell, not who is
spoken to), when her name opens or closes the message; on a reply or a mention, like any intent.

### Answer stats — `/debug answers`

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
  pattern; GuildId 0 is DMs. **Counted in memory** (`ActivityCounter.CountAnswer`, from
  `ChatterService.Count` and `CardReplyService`) and written by `ActivityFlushService` every 5
  minutes (see admin): never a database write per answer, and a count can never cost one. The view
  lags by up to 5 minutes.
- **The examples are kept 30 rolling days** (`UnansweredExample` rows; rules in `UnansweredLog`): the
  messages whose answer `AnswerKinds.IsUnrecognised` (the plain fallbacks, and the fact and nudge
  lines standing in for one). Batched with the counts; the flush that writes them also prunes
  anything older — `Day` is an int, so the delete and
  the window run in SQL. **No author is kept**, at the owner's request: only what was said,
  whitespace collapsed, 200 characters at most. The examples view follows `period` (7 or 30 days;
  « depuis toujours » is the 30 kept).
- The view (`AnswerStatsCards`, static): one embed, a field per family, rows by count with the
  split by path, and *Pistes* (the unrecognised share per path, the intents never hit in the
  period). Every key on every path measures about 3,300 characters, under the 6000 cap; fields are cut
  at 900. The examples go out as a `.txt` file.
- **Ephemeral**, owner's eyes only, under an `OwnerReportLines` line (her reporting for duty to
  Papa). Sent with `AllowedMentions.None`.

