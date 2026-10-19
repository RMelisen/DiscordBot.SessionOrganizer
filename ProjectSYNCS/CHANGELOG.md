# Changelog

Every version that reached the add-on, newest first. Entries up to 6.5.0 were reconstructed from
the git history; versions that were skipped or only existed on a side branch are absent.

## 7.5.0 — 2026-10-19

- She now answers to a few pet names from her creator, with her own reaction to each kind.
- New `/report` for staff, private: `sessions` (attendance, lateness, no-shows, best time slots,
  organisers), `polls` (polls, her Wednesday poll, `/tonight`), `community` (active members,
  arrivals and departures, emotes, quiz) and `syncs` (temperature, outages, chatter, reputation,
  time in voice), over 7 days, 30 days or all time, with trends.
- She now counts members joining and leaving the server, day by day.
- On the 1st of each month she sends her creator the previous month's reports by DM.

## 7.4.0 — 2026-10-19

- A crafted chat message can no longer keep her busy reading it: she stays responsive whatever
  is sent to her.
- Gifts, trades, sales, purchases and game payouts made at the same moment are now applied one
  after the other, so items and cailloux can no longer be counted twice.
- The Discord token is now masked in the add-on's configuration screen.
- `/debug answers` is now posted in the channel for everyone to see, with a line of her
  reporting for duty.

## 7.3.0 — 2026-10-16

- Answer one of her questions with a plain « oui » or « non » and she reacts to the answer instead of
  shrugging at a one-word reply.
- Ask « tu m'aimes ? » and her answer depends on the good bots and bad bots you've given her over
  the last 30 days.
- Ask « tu te souviens de moi ? » or « comment je m'appelle ? » and she says your name, often with
  something her records remember about you.
- Ask « t'es fâchée ? » or « tu boudes ? » and she denies it, not very convincingly.

## 7.2.0 — 2026-10-16

- Tell her you're having a bad day and she drops the teasing to offer something gentle to hold on to.
- Ask « je devrais… ? » or « X ou Y ? » and she decides, the same way as `/yesno`.
- Ask « c'est la faute de qui ? » and she picks a culprit from your voice call, as `/blame` does.
- She tells jokes and fun facts when asked.
- « tu m'as manqué » now gets the same flustered answer as a declaration.

## 7.1.3 — 2026-10-16

- She understands more ways of asking about her love life, her hobbies and tastes, how to use her
  sessions, polls, reminders, giveaways and quiz, and of doubting her (« t'es sûre ? », « sérieux ? »).
- « tu m'ignores ? » and « tu réponds plus ? » are now read as checking she's still there.
- « tu aimes qui comme perso ? » no longer makes her rate « qui comme perso ».

## 7.1.1 — 2026-10-16

- Calling her a liar (« t'es une menteuse », « pourquoi tu mens ? ») gets her flustered denial
  instead of a roast; longer insults are still roasted.

## 7.1.0 — 2026-10-16

- She answers more of what people say to her: checking she's there (« t'es là ? », « syncs ? »),
  thanks, boredom (she suggests her games), questions about her age, birthday, home and tastes,
  and questions about her love life.
- Asking how she works (« comment ça marche ? », « tu sais faire quoi ? ») gets pointed to `/help`,
  or to the right command when the message names one.
- « ship X et Y » or « X et moi, ça match ? » in chat gets exactly the answer `/ship` would give;
  so do « on irait bien ensemble ? » and « compatibilité entre X et Y ».
- Asking her opinion of two people (« tu penses quoi de @A et @B ? ») ships them, instead of rating
  the word « et ».
- Quoted words (« il a écrit « je t'aime » à sa copine ») are no longer answered as if said to her.
- Ask her « c'est qui mon âme sœur ? » and she runs `/ship` against everyone on the server to find
  your best match (or your worst one, if you dare).
- « fais genre », « mon œil » or « avoue » after one of her denials gets her doubling down.
- Tell her about your day (« j'ai mangé de la soupe ») and she's either all ears or, just as often,
  « euh… ok ? ».
- A reply that's only a sticker is answered like an emote, rather than roasted as if unread.
- Her conversation count no longer skips a message when she answers about one of her favourite
  robots.
- More laughs are recognised, emotes included.
- She recognises many more ways of saying the things she already answers: more chat spellings,
  slang and English for love, apologies, « ça va ? », goodbyes (« a+ » included), « peut-être »,
  agreeing and disagreeing, requests, laughs and unmarked questions.
- « ça va ? » is understood even when another question follows it in the same message.
- Fewer misreads: « je voulais pas venir », « oups j'ai raté le boss » or « j'ai bien vu le film »
  in a long message no longer get an apology or an agreement answer, and « quand tu peux » is no
  longer a request.
- When her name comes up in a message meant for someone else (another person tagged, or « parle
  avec Syncs »), she reacts to being talked about instead of answering as if asked.
- Words written with « œ » (« cœur ») now match their cues.

## 7.0.0 — 2026-10-16

- She answers what you say, not just its mood: asking how she is (on the Pi, sometimes with her
  real temperature), who she is or what she's doing, goodnight, goodbye, an apology, a declaration,
  laughter, an open question, or a bare « ok ». Her creator gets his own answers to several of these.
- She also answers « peut-être » (badly), « t'es un bot ? », « t'as raison » / « t'as tort », requests
  (pointing to the command that does it), and opinions: « t'en penses quoi de X ? » gets the same
  verdict as `/rate`, and « tu préfères X ou Y ? » always the same side.
- A reply to one of her own posts — a welcome, the morning hello, the late call, a session recap,
  the weekend poll, a level-up, an idle or wake-up line, a command result — is answered about that post.
- Now and then she quotes her real records about you instead of a roast: missed sessions, time
  together in voice, your Plynling, quiz wins, the wall of shame, your rank.
- Reply to her again and again and she notices on the third message, ends the conversation herself
  on the fifth, then answers only with an emote for ten minutes.
- Reworked lines: about thirty generic roasts replaced with ones only she would say, her creator's
  replies and her aunt's greetings rewritten for more range, and « TLDR »-style brush-offs gone.
- New owner tool `/debug answers`: how many of each kind of chat answer she gave over 7 days, 30 days
  or all time, split by how she was reached, and the last messages she had no specific answer for
  (kept in memory, without their authors).
- Fixed three lines that printed « 0 » instead of a number (a `/work` payslip, a wake-up line and a
  voice-session remark).

## 6.20.2 — 2026-10-16

- `/wouldyourather`: thirty new dilemmas of hers (games, life, a little lore), and a couple of old
  ones tidied.

## 6.20.1 — 2026-10-16

- Internal: her lines are split into one file per category. Nothing she says changes.
- Her 15 face emotes were re-uploaded; the bot now points at the new ones.

## 6.20.0 — 2026-10-15

- New `/recap [user]`: your numbers over 7 days, 30 days or all time (XP, voice, sessions, quiz,
  Plynling, cailloux, the wall of shame), with her verdict.
- New `/ship a b`: how well two things go together, out of 100, the same every time. She has
  opinions about some pairs. `@random` (or an empty `b`) draws someone from the server.
- New `/blame`: whose fault it is, picked from the people in voice. Quokka is always a suspect.
- New `/wouldyourather`: a two-button dilemma, hers or yours; votes are revealed after 10 minutes,
  along with her own pick.

## 6.19.0 — 2026-10-15

- Eight new `/arcade` games:
  - `/arcade race`: a Plynling race with 30 minutes of pari-mutuel betting; the winner's owner gets
    a small bonus.
  - `/arcade crash`: board the rocket, cash out before it explodes.
  - `/arcade blackjack`: one hand against her, fair and square.
  - `/arcade bomb`: hot potato for a fixed 20-cailloux stake.
  - `/arcade quote`: guess who wrote an old message from a public channel.
  - `/arcade temperature`: guess her CPU temperature.
  - `/arcade puissance4` and `/arcade morpion`: against someone, or against her.
- Quokka now sneaks into the bots channel up to four times a day; the first to hit him wins a few
  cailloux.
- New `/config bots-channel set|clear` for where her low-stakes announcements go.
- Accepting a `/arcade duel` now pings whoever started it, and every step of a duel (accepted, a
  move played, the result) moves its card to the bottom of the channel, showing who has played.

## 6.18.0 — 2026-10-15

- `/tonight` asks who's up for playing tonight. Once enough people are in, the organizer can turn
  it into a real session in one click; otherwise it closes at midnight.
- `/teams` splits your voice channel into 2 to 4 teams, at random or balanced by level, with a
  button to reshuffle.
- `/remindme` sends you a reminder by DM later (« 10m », « dans 2h », « demain 9h », « 25/12
  10:00 »…), or pings you in the channel if your DMs are closed. `/reminders` lists and cancels them.
- `/rate` scores anything out of 10, the same score every time. A few names get her very own
  verdict.
- New `/arcade` games for cailloux: `/arcade duel` (rock-paper-scissors against someone, or against
  her if you dare) and `/arcade heist` (gather a crew to rob Quokka's stash).
- The economy dashboard counts arcade winnings and losses.

## 6.17.8 — 2026-10-15

- `/work` has 100 new shifts, some borrowed from real oddball jobs (dog food taster, professional
  sleeper, golf ball diver…), others straight from the village and her attic.
- One more name makes SYNCS react: a very polite admirer she is flattered by, and not interested in.

## 6.17.7 — 2026-10-15

- Two new stickers she can post after her lines: a too-cool face in sunglasses, and a serene,
  fabulous one showing off her nails. A few of her show-off lines now come with them.
- Her new fabulous face is a server emote, and reacting to her with it counts as praise.

## 6.17.6 — 2026-10-14

- Reacting to her with more of the server's emotes now counts as a verdict: the pink heart, the
  yes sign, the agreeing face and her own warm faces read as praise; the disgusted, side-eye and
  jaded faces and her own outraged, disgusted and grumpy faces read as a complaint.

## 6.17.5 — 2026-10-14

- Her summer avatar catches the sun: warm shafts of light fall across her from the top-left,
  with dust floating through them and glinting as it crosses the light.

## 6.17.4 — 2026-10-14

- Her glitched face finally gets used: it flickers under her line when she comes back from a
  crash or a power cut in a bad state, gone the moment she pulls herself together, and at one
  other very rare moment.

## 6.17.3 — 2026-10-14

- Her seasonal profile pictures are now animated: petals drifting in spring, a glint across her
  sunglasses in summer, leaves falling in autumn, snow in winter.

## 6.17.2 — 2026-10-14

- Her online status can now switch to do-not-disturb on rare occasions, and goes back to normal
  afterwards.

## 6.17.1 — 2026-10-14

- Five new faces for SYNCS — formal, sad, confused, grumpy and horrified — as emotes in her
  lines and as the pictures she posts after them. Her absence notices now come in secretary mode,
  dead polls and empty quizzes get a properly sad face, and she looks puzzled when her name comes
  up out of nowhere.
- Her grumpy face joins the reactions she puts on hostile messages.

## 6.17.0 — 2026-10-14

- New `/admin warn`, for staff: a formal warning, sent privately to the member by DM with the
  reason, from the team rather than a named moderator. Warnings count for 90 days; from the third
  active one, staff are offered a one-hour timeout button. Nothing is posted in the channel.
- New `/admin warnings` to see someone's history, and `/admin unwarn` to revoke one (it stays in
  the history but no longer counts).

## 6.16.1 — 2026-10-14

- `/health` now posts its check-up in the channel for everyone to see, instead of only to the
  person who asked.

## 6.16.0 — 2026-10-14

- New `/health` command, for everyone: SYNCS's check-up, opened by a line in her voice. It shows
  the Pi's temperature (now, today, the last 7 days, the record), its load, memory, CPU speed and
  power warnings, her own memory and uptime, the Discord connection, the database's size and free
  disk space, and the power cuts, crashes and network outages of the last 30 days. Replaces the
  owner-only `/debug health`.
- `/help` is now two pages, switched with buttons: the commands, and how everything works.

## 6.15.2 — 2026-10-14

- Naming one of her favourite robots and SYNCS in the same message now always gets her answer
  about the robot, instead of a puzzled line about her name.
- The add-on now installs a prebuilt image from GitHub instead of compiling on the device, so
  updates no longer fill the disk with build cache.

## 6.15.1 — 2026-10-14

- 90 new lines for when people talk about SYNCS without tagging her: 30 puzzled ones, 30 comebacks
  for when someone is mean about her, and 30 happy ones for when her creator says her name.

## 6.15.0 — 2026-10-14

- SYNCS notices when people talk about her without tagging her ("syncs" or "sync"), every time: a
  puzzled "Eh ?", a hello back if she is greeted, and a sharp comeback if someone is mean about her.

## 6.14.3 — 2026-10-14

- A few Plynling lines and events are reworded.

## 6.14.2 — 2026-10-14

- SYNCS's robot heart is quicker again: she takes back her reaction, deletes her line or corrects
  her slip after 2 to 6 seconds instead of 5 to 9.

## 6.14.1 — 2026-10-13

- SYNCS's robot heart reacts every time, with no limit, and reacts with an emoji when one of them
  is named without talking to her (sometimes with a few words too).

## 6.14.0 — 2026-10-13

- SYNCS has a robot heart: some names make her react, and a very distant pen pal writes to her now
  and then.
- New morning fun facts, new status lines and a few new lines.
- The pictures SYNCS posts after her lines are no longer limited per channel or per day.

## 6.13.0 — 2026-10-13

- SYNCS sometimes posts a picture of herself right after one of her lines, matching what she just
  said (at most one every 30 minutes per channel, and 12 a day).

## 6.12.1 — 2026-10-13

- SYNCS's emotes were re-uploaded to the server: she uses the new ones.
- Five new faces: disgust (Quokka), overheating (heat records), rebooting (wake-ups), love and
  head over heels (not used yet).

## 6.12.0 — 2026-10-13

- Big brain of the week: every Monday at 10:00, SYNCS gives a role to the best quiz player of the
  past week and announces it in the quiz channel (`/config quiz-role`).
- Four of SYNCS's emotes removed from the server (sleepy, shocked, mama, jenote): she no longer
  uses them.

## 6.11.0 — 2026-10-13

- Plynling role: SYNCS can create and manage a role for every owner of a living Plynling
  (`/config plynling-role`). It comes with the adoption and leaves with the Plynling.

## 6.10.1 — 2026-10-13

- Her seasonal profile pictures are zoomed in a little when applied.

## 6.10.0 — 2026-10-12

- `/admin mute` and `/admin unmute` (staff): a Discord timeout of up to 28 days, with an optional
  reason for the audit log. When the bot can't do it, the reply says which permission or role is
  missing.

## 6.9.0 — 2026-10-12

- Typed shortcuts for the Plynling routine, sent as a plain message: `;pl` shows your card
  (`;pl @someone` theirs), `;pl visit @someone` knocks at their door, `;forage` and `;work` do what
  their commands do (`;pl forage` and `;pl work` too). A refusal answers for a few seconds, then
  disappears.

## 6.8.2 — 2026-10-12

- No visible change: the source files are reorganised into one folder per subsystem.

## 6.8.1 — 2026-10-11

- SYNCS now has 15 emotes of her own, one per mood (smug, sulking, outraged, sleepy…), and uses
  them: in about forty new lines, and in her reactions.

## 6.8.0 — 2026-10-11

- SYNCS's profile picture now follows the seasons: blossoms in spring, sunglasses in summer, leaves
  and a scarf in autumn, snow in winter. It changes on the first day of each season.

## 6.7.1 — 2026-10-11

- 20 new lines for SYNCS's Wednesday weekend poll, 20 for its result and 20 for when nobody votes.
- 20 new welcome lines. Someone rejoining the server now gets the same welcome as a newcomer.
- 30 new status lines for when SYNCS sits in a voice call.
- 100 new lines for what SYNCS says after a long voice get-together.
- 40 new lines for when someone throws her out of a voice call.
- 30 new lines for when another bot is in the voice call.
- 20 new lines for when SYNCS sends the owner to bed.

## 6.7.0 — 2026-10-11

- With the owner in a voice channel, SYNCS joins much more willingly (two people are enough,
  90% chance, a second chance if he arrives late), follows him between channels, and sometimes
  leaves when he does.
- Better odds with her aunt there (80%), and the owner's odds during a session she organised.
- Her status shows when she's in a voice call.
- No joining at night, except with the owner.
- Less regular join and leave times, with the same average stay.
- Thrown out of a call, she either sulks for the rest of the day or comes straight back, once.
- For a Game or Movie session, SYNCS sits in voice a couple of minutes before the start, and the
  session's title shows under the voice channel's name while it runs.
- Her status shows whose stream she is "watching".
- Another bot in the call halves her odds of joining, and she says so.
- Late at night in a call with the owner, she sends him to bed.
- Now and then her mic and headset icons flicker for a moment.
- Time spent in voice with her is recorded for a future monthly report.

## 6.6.1 — 2026-10-10

- SYNCS joins busy voice channels more often (66% of gatherings instead of 30%).

## 6.6.0 — 2026-10-09

- Roll call for Game and Movie sessions: a ping for whoever is late ten minutes in, then a tally
  (who came, who was late, how many never showed).
- Weekend poll every Wednesday evening; anyone who voted for the winning evening can turn it into
  a session.
- Newcomers get a welcome (or a welcome back) in reply to Discord's join message.
- SYNCS sometimes sits in a busy voice channel, muted and deafened, and her end-of-session line
  changes when she was there.

## 6.5.1 — 2026-10-09

- The voice spectator line now fits the channel: Général, Gaming (both channels), Cinéma and
  Étude/Travail each get their own lines.

## 6.5.0 — 2026-10-09

- Voice spectator line after long voice sessions.
- SYNCS flinches at the name Synthia.
- Gemini prompts for the emote pack and the seasonal avatars (docs only).

## 6.4.0 — 2026-10-09

- Corrupted wake lines after a power cut or a crash, edited clean after 2 minutes or as soon as
  someone speaks.
- A rare easter egg on her chatter.

## 6.3.0 — 2026-10-09

- Pi health: real CPU temperature, wake lines after a power cut or a crash, router outages.
- `/debug health`.

## 6.2.4 — 2026-10-09

- Quiz: 40 more intro, win and wrong-answer lines.
- Plynling events: the « anniversaire » event is removed.

## 6.2.3 — 2026-10-09

- More quiz questions.

## 6.2.2 — 2026-10-09

- More quiz questions.

## 6.2.1 — 2026-10-09

- Quiz: typos fixed in five questions.

## 6.2.0 — 2026-10-09

- Pop quiz: questions with answer buttons, categories and `/quiz leaderboard`.
- The morning hello survives restarts.

## 6.1.2 — 2026-10-09

- Version bump only.

## 6.1.1 — 2026-10-09

- Plynling events: the broken mirror is retold without superstition.

## 6.1.0 — 2026-10-09

- `/config`: game, main and idle channels, and extra `/shame` voters.

## 6.0.3 — 2026-10-08

- Plynling events: wave 8, 181 CK3-inspired events.
- Religious and superstitious scenes rewritten.

## 6.0.2 — 2026-10-08

- Plynling events: picking a choice refreshes the card it was opened from.

## 6.0.1 — 2026-10-08

- Plynlings: temporary stat lessons, stress shown on the card, sweep on demand.

## 6.0.0 — 2026-10-08

- Plynling events: review fixes and adult-trait balance.

## 5.20.0 — 2026-10-08

- More lines.

## 5.19.0 — 2026-10-08

- Livelier bot, especially at night: the 3 a.m. line, idle fillers, a wake-up line after a restart,
  more varied presence.

## 5.18.12 — 2026-10-08

- New easter egg.

## 5.18.11 — 2026-10-08

- More lines.

## 5.18.10 — 2026-10-08

- More lines.

## 5.18.9 — 2026-10-08

- More lines.

## 5.18.8 — 2026-10-08

- More lore.

## 5.18.7 — 2026-10-08

- New emotes.

## 5.18.6 — 2026-10-07

- More lore.

## 5.18.5 — 2026-10-07

- Plynling events waves 2–7 merged with the Synthia lore.

## 5.18.4 — 2026-10-07

- Plynling traits show their icons.

## 5.18.3 — 2026-10-07

- Plynling trait icons: 51 CK3-style tiles.

## 5.18.2 — 2026-10-07

- Plynling events: waves 2 to 7, story cycles, rewards and crises.

## 5.16.2 — 2026-10-07

- Synthia name lore in the status lines, a fun fact and the breakdown.

## 5.16.1 — 2026-10-07

- Plynlings: each swept Plynling gets its own database scope; card paths defer before touching the
  database.

## 5.16.0 — 2026-10-07

- Plynling journal keeps up to 500 moments.
- The mascot decides which events are due.

## 5.15.2 — 2026-10-07

- Plynling events: cleanup pass.

## 5.15.1 — 2026-10-06

- Plynling events: content wave 1 (10 events).
- Dropped a growing-up guard now covered by the stage-moment window.

## 5.15.0 — 2026-10-06

- Plynling events: social engine with responses, follow-ups and owner-only choices; big moments,
  on-action events, a first event chain and social stories.

## 5.14.0 — 2026-10-06

- Plynling stress: stats, decay, care relief, modifiers and mental breaks, shown on the card and in
  the stories; debug tools.

## 5.13.1 — 2026-10-06

- Plynling events: fixes from the phase 2 review.

## 5.13.0 — 2026-10-06

- Plynling life events (phase 2).

## 5.12.0 — 2026-10-06

- Plynling personality: traits, stats and title (phase 1).
- The hourly sweep no longer re-adds trimmed stage moments.

## 5.11.26 — 2026-10-05

- Version bump only.

## 5.11.25 — 2026-10-05

- `/debug emote`.
- More interrogation, yes/no and absence lines.

## 5.11.24 — 2026-10-05

- Reordered the bot's line pools.

## 5.11.23 — 2026-10-05

- More lines.

## 5.11.22 — 2026-10-05

- Fixed some lines.

## 5.11.21 — 2026-10-05

- Plynling events are announced in the game channel.

## 5.11.20 — 2026-10-05

- More lines.

## 5.11.19 — 2026-10-05

- More morning lines, animated sparkle.

## 5.11.18 — 2026-10-05

- The line picker's memory is global across channels; morning pools rotate daily.

## 5.11.17 — 2026-10-05

- Morning hello message.

## 5.11.16 — 2026-10-02

- Version bump only.

## 5.11.15 — 2026-10-02

- One more channel excluded from XP.

## 5.11.14 — 2026-10-02

- More personal lines; typos and stale lines fixed after a voice review.

## 5.11.13 — 2026-10-02

- One more known member's gender.

## 5.11.12 — 2026-10-02

- Version bump only.

## 5.11.11 — 2026-09-30

- Fixed a visit claim that could stay stuck.
- `/pl` as a shortcut for `/plynling`.
- No more rock emoji on the hide-and-seek buttons.

## 5.11.10 — 2026-09-30

- Version bump only.

## 5.11.9 — 2026-09-30

- New Ping-Qilin.

## 5.11.8 — 2026-09-30

- Version bump only.

## 5.11.7 — 2026-09-30

- Plynling launch pairing fixes.

## 5.11.6 — 2026-09-30

- Plynlings' production launch preparation.

## 5.11.5 — 2026-09-30

- Version bump only.

## 5.11.4 — 2026-09-30

- Visits no longer write « Visited » / « Hosted » journal entries.

## 5.11.3 — 2026-09-30

- No more emojis on the Plynling card's passions and controls.

## 5.11.2 — 2026-09-30

- Capitalised the card's owner and accessory lines.

## 5.11.1 — 2026-09-30

- Aligned the card's mood line with its bars.

## 5.11.0 — 2026-09-30

- Aligned the Plynling card's three bars.

## 5.10.16 — 2026-09-30

- Guard against a Plynling with no recorded morning.

## 5.10.15 — 2026-09-30

- Plynling sickness: medicine and the « Soigner » button; a sick Plynling eats half, can't play or
  visit; the owner is told, and illness deaths are marked.
- `/admin plynling cure` and `/debug plynling`.

## 5.10.14 — 2026-09-30

- Plynling hygiene: the « Hygiène » bar, the « Laver » button and dirty / sick pictures.

## 5.10.13 — 2026-09-30

- Plus ou moins: a seventh try and a guess history.
- Visit stories can skip to the end.
- Compatibility, widened to -20..+20, decides where a Plynling pair ends up.
- Twelve theme icons redrawn.

## 5.10.12 — 2026-09-30

- `/leaderboard` is open to everyone again.

## 5.10.11 — 2026-09-30

- Pixel icons for the sets and every cosmetic.

## 5.10.10 — 2026-09-29

- Pixel icons for the 40 collectibles.

## 5.10.9 — 2026-09-29

- Economy tweaks, more lines.

## 5.10.8 — 2026-09-29

- Reworked some lines.

## 5.10.7 — 2026-09-29

- Text fix.

## 5.10.6 — 2026-09-29

- A sad visit no longer ends « playfully ».

## 5.10.5 — 2026-09-29

- Version bump only.

## 5.10.4 — 2026-09-28

- Every visit conversation rewritten as a short scene.

## 5.10.3 — 2026-09-28

- Both Plynlings talk on every visit step.
- One cosmetic removed.

## 5.10.2 — 2026-09-28

- More visit conversations around taught passions.

## 5.10.1 — 2026-09-28

- More visit conversations.

## 5.10.0 — 2026-09-28

- More visit conversations.

## 5.9.1 — 2026-09-28

- Visit conversations written as whole scripts.

## 5.9.0 — 2026-09-28

- Smaller visit sprites and a way back to the start.

## 5.8.0 — 2026-09-28

- Visits told as eight-step conversations with faces.

## 5.7.0 — 2026-09-28

- Plynling passions and five-beat visit stories.

## 5.6.4 — 2026-09-28

- More lines.

## 5.6.3 — 2026-09-28

- `/admin dashboard`, with the economy recorded day by day.
- Visits play as a little story.

## 5.6.2 — 2026-09-27

- Every Plynling gets little arms (art v4).

## 5.6.1 — 2026-09-27

- `/admin pebble` and `/admin stats`.

## 5.6.0 — 2026-09-27

- Insectes collection.

## 5.5.3 — 2026-09-25

- Plynling cosmetics: weekly shop rotation, buying, crafting, the wardrobe; shown on the card, the
  grave and the inventory.

## 5.5.2 — 2026-09-25

- Doré renamed Girolle, Mystique renamed Mycène.

## 5.5.1 — 2026-09-25

- Lentin du chêne renamed Shiitake.

## 5.5.0 — 2026-09-25

- Foods get their mushroom pictures.

## 5.4.26 — 2026-09-25

- Feeding and petting move to the card; forage gets its own message; the pet cooldown is shown.
- The game is renamed Pierre-papier-ciseaux.
- The bot uploads its mushroom emojis itself.
- Inventory and collection book tidied.

## 5.4.25 — 2026-09-25

- Champignons collection; the collection is an interactive book.

## 5.4.24 — 2026-09-25

- Commands regrouped by whose thing it is (`/plynling`, `/inventory`, `/admin`, `/config`, `/debug`).

## 5.4.23 — 2026-09-25

- Inventory and collections: item catalog, shop, pantry, giving, foraging, collection book, trade
  offers and selling.

## 5.4.22 — 2026-09-25

- A Plynling's partner is shown on its card.

## 5.4.21 — 2026-09-25

- Plynling relationships, grown by visits; `/plynling relations`.

## 5.4.20 — 2026-09-25

- Happiness matters: mood changes what meals are worth, and a happy Plynling may bring back a gift.

## 5.4.19 — 2026-09-25

- Plynling badges and `/plynling journal`.

## 5.4.18 — 2026-09-25

- `/plynling play` and `/plynling visit`, with rewards.

## 5.4.17 — 2026-09-25

- `/plynling abandon`, `/plynling list`, Plynlings sleep at night, anyone can feed a Plynling at
  double the price.

## 5.4.16 — 2026-09-25

- The Coprin, a second rare Plynling.

## 5.4.15 — 2026-09-25

- A baby drawing for every species.

## 5.4.14 — 2026-09-25

- Plynling life stages (2 days, 14 days, 6 months), shown on the card.

## 5.4.13 — 2026-09-25

- The warning DM moves to 3 hours before death.

## 5.4.12 — 2026-09-25

- Hunger life halved to 2 days, happiness to 36 hours.

## 5.4.11 — 2026-09-25

- The gender sign moves out of the card heading.

## 5.4.10 — 2026-09-25

- No more starvation countdown in the warning DM.

## 5.4.9 — 2026-09-25

- No more starvation countdown on the card.

## 5.4.8 — 2026-09-24

- Idle animation for every Plynling (art v3).

## 5.4.7 — 2026-09-24

- Every Plynling line written in both genders.
- Each mushroom Plynling gets its own shape (art v2).

## 5.4.6 — 2026-09-24

- Plynling gender.

## 5.4.5 — 2026-09-24

- `/plynling help`.

## 5.4.4 — 2026-09-24

- Plynling graveyard.

## 5.4.3 — 2026-09-24

- Hourly Plynling sweep.

## 5.4.2 — 2026-09-24

- Plynling management commands.

## 5.4.1 — 2026-09-24

- Basic Plynling care commands.

## 5.4.0 — 2026-09-24

- Economy commands (cailloux).

## 5.3.0 — 2026-09-24

- Plynling database tables.

## 5.2.0 — 2026-09-24

- Plynling art export.

## 5.1.0 — 2026-09-24

- Version bump only.

## 5.0.1 — 2026-09-24

- Plynlings core: creature, timing, rarity, foods and currency.

## 5.0.0 — 2026-09-24

- Plynling plan and sprites.

## 4.9.2 — 2026-09-24

- Long user text no longer breaks cards silently.

## 4.9.0 — 2026-09-23

- Internal refactoring: shared helpers.

## 4.8.20 — 2026-09-22

- `/leaderboard` temporarily removed.

## 4.8.19 — 2026-09-10

- Response tweaks.

## 4.8.18 — 2026-09-07

- More emotes count as good / bad bot reactions.

## 4.8.17 — 2026-09-04

- More emotes.

## 4.8.16 — 2026-08-20

- Number tweaks.

## 4.8.15 — 2026-08-12

- Small easter egg.

## 4.8.14 — 2026-08-12

- `/yesno`.

## 4.8.13 — 2026-08-11

- Command visibility fixed.

## 4.8.12 — 2026-08-11

- Emote id fixed.

## 4.8.11 — 2026-08-11

- `/tell` is global.

## 4.8.10 — 2026-08-11

- Version bump only.

## 4.8.9 — 2026-08-10

- New taunt.

## 4.8.8 — 2026-08-10

- More lines, reorganised.

## 4.8.7 — 2026-08-10

- More cues.

## 4.8.6 — 2026-08-10

- Version bump only.

## 4.8.5 — 2026-08-10

- Better cue and frame detection.

## 4.8.4 — 2026-08-10

- Bot feedback verdict miscalculation fixed.

## 4.8.3 — 2026-08-10

- One more channel excluded from XP by default.

## 4.8.2 — 2026-08-10

- A category on the wall of shame.

## 4.8.1 — 2026-08-10

- `/help` was too long.

## 4.8.0 — 2026-08-10

- `/config` commands.

## 4.7.8 — 2026-08-10

- Reworked some lines.

## 4.7.7 — 2026-08-10

- Version bump only.

## 4.7.6 — 2026-08-09

- `/shame` restricted to admins.
- Rival-bot detection catches deferred slash-command replies.
- « Mean » emotes fire again.

## 4.7.5 — 2026-08-09

- More lines.

## 4.7.4 — 2026-08-09

- Avatar display fixed.

## 4.7.3 — 2026-08-09

- Emote id fixed.

## 4.7.2 — 2026-08-09

- Wall of shame UI tweaks.

## 4.7.1 — 2026-08-09

- Emote fixed.

## 4.7.0 — 2026-08-09

- Commands to add and remove XP.

## 4.6.1 — 2026-08-09

- New voice XP curve.

## 4.6.0 — 2026-08-09

- Wall of shame (`/shame`).

## 4.5.0 — 2026-08-09

- Voice XP exploit fixed.

## 4.4.2 — 2026-08-09

- Level-ups earned in voice are announced in the voice channel.

## 4.4.1 — 2026-08-07

- Minor tweaks.

## 4.4.0 — 2026-08-07

- `/giveaway`.

## 4.3.1 — 2026-08-07

- « Invalid Form Body » error fixed.

## 4.3.0 — 2026-08-07

- Leaderboard filters.

## 4.2.1 — 2026-08-07

- Reactions and voice time on the leaderboard.

## 4.1.6 — 2026-08-07

- Version bump only.

## 4.1.5 — 2026-08-07

- `/level` and `/leaderboard` rebuilt with Components V2.
- More channels excluded from XP.

## 4.1.4 — 2026-08-07

- Detects shutdown threats that name her.

## 4.1.3 — 2026-08-07

- Level-up card.
- Spam channels excluded from XP.

## 4.1.2 — 2026-08-07

- Level-up announcement as an embed card.
- One-time XP reset.

## 4.1.0 — 2026-08-07

- Voice channel XP.
- Commands no longer crash in DM.
- Reacts when threatened.

## 4.0.0 — 2026-08-07

- XP system.

## 3.2.8 — 2026-08-06

- Minor tweaks.

## 3.2.7 — 2026-08-06

- No longer copies emotes on another bot's message.

## 3.2.6 — 2026-08-06

- Family member recognition.

## 3.2.5 — 2026-08-06

- More greetings.

## 3.2.4 — 2026-08-06

- Reacts when the owner is mean to her.

## 3.2.3 — 2026-08-06

- More lines and emotes.

## 3.2.2 — 2026-08-06

- Internal refactoring: custom emotes.

## 3.2.1 — 2026-08-06

- More lines.

## 3.2.0 — 2026-08-06

- Jealousy when a rival bot is praised.

## 3.1.5 — 2026-08-05

- More lines.

## 3.1.4 — 2026-08-05

- Version bump only.

## 3.1.3 — 2026-08-05

- Weekly and monthly filters on `/goodbot`.
- Thumbs up / down count as good / bad bot.

## 3.1.1 — 2026-08-05

- Date filter on `/emotestats`.

## 3.1.0 — 2026-08-05

- Good bot / bad bot verdicts are recorded.

## 3.0.1 — 2026-07-30

- Greeting cue tweaks.

## 3.0.0 — 2026-07-30

- Messages are weighed by tone (mean, nice, greeting, negation) instead of single cues.

## 2.2.2 — 2026-07-29

- Version bump only.

## 2.2.1 — 2026-07-28

- Reaction tweaks.
- SQLite vulnerability fixed.

## 2.2.0 — 2026-07-28

- The bot sometimes reacts to messages.

## 2.1.2 — 2026-07-28

- Presence line format fixed.

## 2.1.1 — 2026-07-28

- More lines.

## 2.1.0 — 2026-07-28

- Presence lines.

## 2.0.0 — 2026-07-28

- Typing delay before speaking, and a memory of recent lines so she doesn't repeat herself.

## 1.6.4 — 2026-07-24

- Version bump only.

## 1.6.3 — 2026-07-24

- `/dm`.

## 1.6.2 — 2026-07-24

- Reply feature on `/tell`.

## 1.6.1 — 2026-07-24

- Announcements are anonymous by default.

## 1.5.10 — 2026-07-01

- `/tell`.
- The owner is notified by DM when `/absent`, and can reply.
- Better at recognising being mistaken for another bot.
- More lines.

## 1.5.9 — 2026-06-30

- Easter egg trigger works for everyone.

## 1.5.8 — 2026-06-29

- « Unavailable » mode.

## 1.5.7 — 2026-06-29

- More lines.

## 1.5.6 — 2026-06-29

- More lines.

## 1.5.5 — 2026-06-29

- Replies when mistaken for someone else.

## 1.5.4 — 2026-06-28

- Event description fixed when moving a session card.

## 1.5.3 — 2026-06-28

- Participant list on Discord events.

## 1.5.2 — 2026-06-28

- Logging in event management.

## 1.5.1 — 2026-06-28

- Field size fixed.

## 1.5.0 — 2026-06-28

- Sessions sync to native Discord scheduled events.

## 1.4.2 — 2026-06-28

- Minor tweaks.

## 1.4.1 — 2026-06-28

- Internal refactoring of the bot service.

## 1.3.3 — 2026-06-26

- Emote leaderboard reworked.
- Greetings trigger when she is mentioned.
- More lines.

## 1.3.2 — 2026-06-26

- Easter egg.

## 1.3.1 — 2026-06-26

- Emote counter, more lines.

## 1.3.0 — 2026-06-26

- Nice talk.

## 1.2.12 — 2026-06-25

- Personalised comebacks weigh more.

## 1.2.11 — 2026-06-25

- Typo fixed, easter egg tweak.

## 1.2.10 — 2026-06-25

- Mention handling.

## 1.2.9 — 2026-06-25

- More replies, some personalised.

## 1.2.8 — 2026-06-25

- Version bump only.

## 1.2.7 — 2026-06-25

- Emojis in reminder messages.

## 1.2.6 — 2026-06-25

- More replies.

## 1.2.5 — 2026-06-25

- Personalised roasts.

## 1.2.4 — 2026-06-25

- Easter egg test config removed.

## 1.2.3 — 2026-06-25

- More time between replies.

## 1.2.2 — 2026-06-25

- Typing speed tweaks.

## 1.2.1 — 2026-06-25

- More replies, easter egg.

## 1.1.10 — 2026-06-23

- Polls close automatically.
- More replies.

## 1.1.9 — 2026-06-19

- Poll delete command.

## 1.1.8 — 2026-06-19

- The first option of a vote is no longer wiped.

## 1.1.7 — 2026-06-19

- Category polls.

## 1.1.6 — 2026-06-19

- Typo fixed.

## 1.1.5 — 2026-06-19

- Minor tweak.

## 1.1.4 — 2026-06-19

- Roast replies.

## 1.1.3 — 2026-06-19

- Polls convert to sessions.
- Session status, and a notification when a session is cancelled.

## 1.1.2 — 2026-06-18

- Poll listing.

## 1.1.0 — 2026-06-18

- All tied slots shown when a poll ends in a tie; poll design tweaks; the time picker no longer
  offers elapsed hours.

## 1.0.6 — 2026-06-18

- Version bump only.

## 1.0.5 — 2026-06-18

- Version number in the `/help` footer.

## 1.0.4 — 2026-06-18

- `/poll`.

## 1.0.3 — 2026-06-17

- Version bump only.

## 1.0.2 — 2026-06-17

- `/help`.

## 1.0.1 — 2026-06-17

- Packaging only.

## 1.0.0 — 2026-06-17

- First release as a Home Assistant add-on: session scheduling wizard, session cards with edit and
  cancel, `/list`, reminders.
- Globalization fix for Paris time.
