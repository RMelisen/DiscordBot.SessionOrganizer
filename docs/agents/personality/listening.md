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
  tagged. With no message replied to, it reads no post context and no yes-or-no. The streak counts
  each follow-up, so a two-step conversation still ends on its fifth message.

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

### Under her cards — `CardReplyService`

A reply under a poll, a vote, a session, `/tonight` or a giveaway card is usually not said to her:
people rally the room, say they voted, ask when it is, or answer in text. **The card is recognised
by its buttons' custom-ids** (`Helpers/Scheduling/CardRefs`: `poll:vote|close|tosession:{id}`,
`event:*:{id}`, `tonight:*:{id}`, `giveaway:enter|leave:{id}`), which carry the row's id, so it
survives a restart and needs no lookup. A card that lost its buttons (drawn, cancelled, started,
expired) is not recognised and reads like any reply. Renaming one of those custom-ids breaks this.

`MessageCues.ReadCardReply` (`MessageCues/MessageCues.Cards.cs`) reads `CardReplyIntent`, first match wins:

- **`Repost`**: « remonte-le », « republie », « reposte », « bump », « up » (three words at most);
  « renvoie », « ramène », « remets » only about the card (a pronoun next to them, the card named,
  or closing the reply: « ramène des chips », « remets la musique » are not); a place verb with a
  place (« mets-le en bas », « remets-le à la fin »); buried (« on le voit plus », or the card named
  as « tout en haut », « noyé »). Never right after a subject (« tu remontes à quelle heure ? »,
  « on remonte la pente »). A negator after cancels (« le remonte pas »), not before (« tu peux pas
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

