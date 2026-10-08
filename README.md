# Project S.Y.N.C.S.

**S**chedule **Y**our **N**ights & **C**oordinate the **S**quads — a Discord bot for
planning gaming sessions, activities and movie nights, and letting people sign up
in one click. It also keeps score: XP and levels, emote and verdict leaderboards, and
a wall of shame. The bot's user-facing language is French.

- [Commands](#commands)
- [Sessions](#sessions) · [Polls & votes](#polls--votes) · [Giveaways](#giveaways)
- [Levels & XP](#levels--xp) · [Leaderboards & stats](#leaderboards--stats) · [Wall of shame](#wall-of-shame) · [Plynlings](#plynlings)
- [Staff & owner](#staff--owner) · [Personality](#personality)
- [Tech stack](#tech-stack) · [Project layout](#project-layout) · [Configuration](#configuration) · [Running locally](#running-locally) · [Deployment](#deployment)

## Commands

| Command | What it does | Who |
| --- | --- | --- |
| `/schedule create · list · edit · cancel` | Plan and manage sessions | everyone |
| `/poll create · list · delete` | Vote on time slots | everyone |
| `/vote create · list · delete` | Vote on free-text options | everyone |
| `/giveaway create · list · delete` | Prize draws | everyone |
| `/level [user]` | Your card: level, rank, progress | everyone |
| `/leaderboard` | Server ranking — three views, three windows | everyone |
| `/emotestats` | Most-used emotes | everyone |
| `/goodbot` | Who praised or scolded the bot | everyone |
| `/yesno [question]` | A coin flip, delivered with conviction | everyone |
| `/shame` | The wall of shame | everyone |
| `/plynling adopt · view · list · journal · relations · play · visit · passion · forage · wardrobe · freeze · thaw · abandon · graveyard · help` | Adopt and look after a Plynling (feed and pet it from its card). `/pl` is a shortcut for every one of them | everyone |
| `/inventory view · collection · shop · medicine · cosmetics · craft · give · trade · sell` | Your items: pantry, collection, cosmetics, swaps | everyone |
| `/work · /balance` | Earn cailloux; see your balance | everyone |
| `/shame user:@someone` | Put someone on it | staff |
| `/config` | Per-server settings, no redeploy | staff |
| `/admin xp add · remove` | Manual XP adjustment | staff |
| `/admin pebble add · remove` | Manual cailloux adjustment | staff |
| `/admin stats` | The server's economy at a glance | staff |
| `/admin dashboard` | The economy day by day: flows, activity, finds | staff |
| `/admin plynling rename · resurrect · passion-reset · cure`, `/plynling freeze/thaw user:` | Manage someone's Plynling | staff |
| `/debug tell · dm · absent` | Speak through the bot; flag yourself away | owner |
| `/help` | In-Discord usage guide | everyone |

"Staff" means Administrator or Manage Server, the bot's owner, or a configured
moderator role. Staff-only commands are visible to everyone but refuse politely.

## Features

### Sessions

- **`/schedule create`** — a private 4-step wizard (type → day → hour → details)
  that posts an interactive session card to the channel.
- **`/schedule list`** — the server's active sessions; lets you republish a card
  into the current channel.
- **`/schedule edit <id>` · `/schedule cancel <id>`** — for sessions you organized.
  Cancelling notifies signed-up participants by DM.
- Session cards carry **Join / Maybe / Decline** buttons plus organizer-only
  **Edit / Cancel** actions.
- **Native Discord events** — the organizer can link a session to a real entry in
  the server's *Events* tab; it stays in sync (time, title, location, participants)
  and is removed when the session is cancelled.
- **Lifecycle** — at start time a card flips to **🔴 In progress** (buttons
  disabled), then to **✅ Finished** ~2 h later.
- **Reminders** — signed-up participants get a DM before the session starts.

### Polls & votes

- **`/poll create`** — propose several time slots (up to 10); everyone votes for
  *all* the slots that work for them. The most-voted slot is highlighted on close and
  can be turned directly into a session.
- **`/vote create`** — same idea with free-text options (games, movies, …).
- **`/poll list` · `/vote list`** — list and republish active polls/votes.
- **`/poll delete <id>` · `/vote delete <id>`** — delete one you created.
- Polls and votes left open **auto-close after 2 days**.

### Giveaways

- **`/giveaway create`** — a prize, a duration from a fixed list (10 minutes to
  7 days), and optionally a description and a winner count (1 by default, 10 max).
- The card carries **Participer / Ne plus participer** buttons and lists the
  entrants — capped at 20 names with the rest summarised, since an embed field holds
  1024 characters and a giveaway draws a bigger crowd than a session.
- A **1-minute sweep** draws the winners at random when time is up, closes the card
  and announces the result in the bot's own voice, pinging the winners (users only —
  never roles or `@everyone`). The end time is an absolute instant, so a restart loses
  nothing and anything that came due while the bot was down is drawn on the next pass.

### Levels & XP

Her own leveling system, entirely independent of the server's other leveling bot —
same vocabulary, no shared state, no cross-reference.

**Earning XP**

| Source | Rate |
| --- | --- |
| Talking | one grant per ~60 s |
| Reacting | its own ~60 s cooldown |
| Replying to or mentioning her | bonus on top of the message grant |
| A genuinely-recorded good/bad-bot verdict | bonus, its own 30 s cooldown |
| Time in a voice channel | once a minute, if eligible — see below |

Voice XP needs **someone else present**, and you **not self-muted or self-deafened**.
Muted people don't count as company either, so being the only unmuted person in a
channel earns nothing. The AFK channel never earns, and some channels are excluded
entirely.

The voice rate **tapers over the day**: the first hour pays the full 10 XP/min, then
steps down every half hour (8, 6, 5, 4, 3, 2) until it settles at 1, resetting at
local midnight. A normal session is worth what it always was, an evening-long one
still pays, and idling all day doesn't. Minutes are still counted in full, so the
Vocal leaderboard stays honest about time actually spent.

**Seeing it**

- **`/level [user]`** — a card: avatar, level, rank, and a progress bar toward the
  next level. No filters — it's a profile, not a ranking.
- **`/leaderboard`** — the ranked list, five per page, every row carrying that
  person's real avatar. Three buttons switch what it ranks — **Niveaux** (XP),
  **Réactions** (reactions added), **Vocal** (eligible time in voice) — and three more
  switch the window: **all time** (default), **30 days**, **7 days**. In a window the
  XP view ranks by XP earned in that window and drops the level, which is a lifetime
  figure and can't be recomputed for a week. A `/level` card's "Voir le classement"
  button opens it in place.
- Crossing a level gets an unprompted card in her own voice: your avatar, the levels
  you went from and to, and a line she picks herself. Earned in voice, it's announced
  in that voice channel's own text chat rather than in the server's main one.

Both surfaces are built with Discord's *Components V2* rather than embeds, so nothing
is rendered or uploaded — Discord loads the faces itself.

### Leaderboards & stats

- **`/emotestats`** — the server's most-used emotes, written *and* as reactions,
  paginated, with three filters: **30 days** (default), **7 days**, **all time**.
- **`/goodbot`** — who has told the bot *good bot* or *bad bot*. Same three filters,
  but **all time** is the default here, since verdicts are rare enough that a rolling
  window is often empty.

She notices either verdict: praise earns a silent reaction, a scolding earns a reply.
**Reacting to one of her messages** counts as the same verdict without her answering
back. Not just 👍 / 👎: a curated set of the server's own emotes counts too — the
cheerful ones (:adorablefrog:, :10sur10:, :giga_laugh:, hearts, a laugh) as praise, the
pointed ones (:gooseknife:, :veryangry:, :staring:, a knife, sad faces) as a scolding.
Only on what she *says*, not on session cards or leaderboards, where a thumb means
something else — and only the **first** reaction each person puts on a given message,
so stacking three approving emotes is still one opinion, and 👍 followed by 🔪 stays
praise. It only counts when it follows something she actually said or reacted
to, and only once per person per thing she did, however they phrase it.

**`good girl` / `bad girl`** count the same on the tally but get a different answer:
praise in that register draws a rather different set of emotes, and a scolding gets its
own flustered reply rather than the wounded-professional-pride one.

Verdicts resist gaming. *"bad good bot"* and *"not good bot"* are cancelled by what
precedes them, and one that is merely quoted, supposed or self-referential
(*"this sentence is false → good bot"*) isn't counted at all.

> The rolling windows on both commands only cover data recorded since daily buckets
> were added; all-time still includes everything counted before that.

- **`/yesno [question]`** — an even coin flip, delivered as a verdict in her own voice
  rather than as a bare "oui"/"non". The optional question is echoed above the answer so
  the ruling stands on its own in the channel; without one it's just the verdict. She
  never hedges — the whole point is that she decides.

### Wall of shame

**`/shame`** — five titles on one page, with the same three filters as the other
rankings (**30 days** by default, **7 days**, **all time**). Built with *Components V2*
like `/level`, so each title shows its current holder's real avatar beside their name;
the runners-up are plain text beneath.

**Le Malfaisant** — hostility, scored from every message.
One hit per human it was aimed at, counting an explicit `@` or the person it replies
to. Roles and `@everyone` never count, other bots never count, and being mean to *her*
does. That half is uncapped, so a message aimed at four people is worth four. A mean
message aimed at **nobody** counts too, as a single point, rationed to one per person
per channel per minute so a rant can't run away with the title.

> Be aware the untargeted half is noisier: complaining about a *game* ("ce boss est
> nul") reads as hostility too, since nobody was named for her to tell the difference
> by.

**Le Banni** — voted, via **`/shame user:@someone`**, announced publicly in her voice.
Voting is **staff-only**, which is what makes the title a deterrent rather than a game.
A target takes at most **2 votes a day from everyone combined**, so nobody gets
dogpiled; there's no per-voter quota. You can vote for yourself; you cannot vote for a
bot, and least of all for her.

**Le Perfide** — consorting with the competition: replying to another bot, mentioning
one, or running one of their slash commands. She can't see the command itself, but the
rival's reply names whoever invoked it. Rationed to one hit per person per channel per
minute, so it ranks how often you *turn to* another bot rather than how chatty that bot
is. Ephemeral replies and old-style prefix commands (`!play`) leave no trace and go
uncounted.

**L'Hystérique** — shouting: a message long enough to be a sentence, written almost
entirely in capitals. Short all-caps words — `LOL`, `OK`, `MDR`, `GG WP` — are simply
how people write those words and never count, and neither does emphasising a word or
two mid-sentence. Rationed like *Le Perfide*, because shouting arrives in bursts and
one argument would otherwise decide the title forever.

**L'Indigne** — abandoning a Plynling with `/plynling abandon`. One point each, never
rationed: it's rare, and deliberate — you have to type its name to do it.

Every counter starts at zero the day it ships and nothing can be backfilled.

### Plynlings

A **Plynling** is a small mushroom creature each member can adopt — one at a time, free.
Its species is rolled among seven mushrooms, all equally likely.
Each one is a boy or a girl, and the bot's French follows suit: *un* or *une Plynling*.

The bot has one of her own: **Ping-Qilin**, a girl Amanite whose passion is naps. Anyone can see
her with `/plynling view user:@SYNCS`, pet and feed her from her card, and take their own Plynling
to visit with `/plynling visit user:@SYNCS` — she opens the door herself. She never dies: SYNCS
looks after her.

- **Hunger** empties in 2 days and **happiness** in 36 hours. At 0% hunger it **dies** — really.
  Both are looked after from its card (`/plynling view [user]`): **Nourrir** (Champignon,
  Shiitake, Morille, Truffe) costs **cailloux** — anyone can feed anyone's, but someone else's
  costs double; **Caresser** is free, every 4 hours, and anyone can pet anyone's.
- **Hygiene** empties in 3 days, and a forage (−10 %) or a game (−5 %) dirties it a little.
  Below 33 % it is *sale*: its happiness drains 1.5× faster and its card says so. **Laver** on
  its card washes it (+60 %), free, every 6 hours, for its owner only. A dirty Plynling wears mud,
  stink lines and a fly on its card picture.
- **Sickness:** each morning at 5 am a Plynling may fall **malade** — rarely when clean, much more
  often the dirtier it is — and its owner gets a DM. Sick, it eats half as well, will not play or
  visit, and cannot be self-frozen. **Soigner** on its card gives one dose a day (from the pantry,
  or 30 cailloux; `/inventory medicine` stocks up). Treated daily it recovers in a few days;
  left untreated, from its third sick morning it may die of it. Staff can cure one with
  `/admin plynling cure`.
- **Mood matters:** a happy Plynling (above 80 % happiness) gets 15 % more out of every meal,
  and the first time its owner looks at it each day it has a one-in-two chance of having found
  5–15 cailloux for them. A sad one (below 30 %) gets 25 % less. At 0 % it sulks and refuses to
  eat until someone plays with it or pets it — unless it is starving, when hunger wins.
- **Play:** `/plynling play` starts one of three mini-games at random with your own Plynling —
  cache-cache (find it behind one of three rocks, two tries), pierre-papier-ciseaux (first to
  two) or plus ou moins (a number from 1 to 100 in seven guesses, with every earlier guess listed). Once an hour; it always
  cheers it up (+15 % happiness), and a win adds +10 % and a few cailloux.
- **Visits:** `/plynling visit user:` sends your Plynling knocking at someone's door. If they
  press *Accueillir* within the hour, the knock closes and the visit is told below it, as a new
  message, in six or seven steps you page through with ◀ ▶: arriving somewhere (the park, the
  pond, the bakery, the host's home, or at night the rooftop under the stars…); one of them
  bringing up a passion, and a real little conversation about it — both of them talking on each
  step, each line answering the last; the talk turning into doing something; the activity;
  then parting with what the visit changed. The two Plynlings stand side by side, and their faces
  follow the conversation — happy, content, sad, or angry (a face only visits use). What they do
  and say follows their passions and their relationship: polite interest between acquaintances, a
  club of two when a passion is shared, one-upmanship between rivals, tenderness in a couple,
  sulking after a squabble. Both get +20 % happiness. Once a day per pair, and no cailloux.
- **Passions:** every Plynling is born with one of twelve passions (cooking, music, video
  games, stars, gardening, pebbles, stories, dance, painting, sport, insects, naps), shown on
  its card. `/plynling passion` lets its owner teach it a second one in their own words
  (« la pêche », « les trains ») — once a day, cleared by leaving it empty, and cleared by
  staff with `/admin plynling passion-reset` if it is inappropriate. A typed passion that names
  one of the twelve gets that passion's own scenes.
- **Badges and the journal:** each Plynling earns its own badges — 16 of them, for living a
  week, a month, six months and a year, for games played and won, visits, meals and pets, for
  being fed by a friend or saved from starving at the last moment, and for coming back from the
  dead. Each pays its owner a few cailloux, once. `/plynling journal [user]` shows its stats,
  its badges and its dated moments (adopted, grew up, first win, visits, badges…), for the
  living and the dead alike.
- **Relationships:** every accepted visit plays a little scene — usually a good moment,
  sometimes a squabble — and each pair has a hidden compatibility, so some click and some never
  will. They become *amis*, *meilleurs amis*, or *rivaux* and *ennemis*; a boy and a girl who
  are very close may confess their love and become a couple (or suffer a heartbreak), and a
  couple that keeps arguing breaks up. The closer they are, the happier a visit makes them;
  between enemies it makes them sadder. A best friend or partner grieves when the other dies
  or is abandoned. `/plynling relations [user]` lists them, the card shows its partner
  (« 💞 En couple avec … »), and the journal names the partner and the best friends.
- **Sleep:** every Plynling sleeps from 1 am to 5 am (Paris time), with its own sleeping
  picture. It can be fed but not petted, and it never dies in its sleep — a death due at
  night happens at 5 am instead.
- **The card** (`/plynling view`) shows it in its current mood — gently animated, each species
  fidgeting in its own way — with its hunger, happiness and hygiene bars and **Caresser** / **Laver** /
  **Nourrir** buttons.
- **Growing up:** *bébé* for its first week, *ado* until 14 days, *adulte*, then *ancien*
  after 6 months — counted in time actually lived, so a freeze pauses it. The card names the
  stage, every species has its own baby picture, and ado and ancien wear the adult one.
- **Personality:** every Plynling gets traits as it grows — one childhood trait as a *bébé*, two
  personality traits as an *ado*, a fourth as an *adulte* — adapted from Crusader Kings III (Courageux,
  Timide, Gourmand…). Its traits and its passion shape five stats (Diplomatie, Intendance, Sagesse,
  Ruse, Courage) and a little title (« Piquante et intrépide »). The card shows them; « 📜 Personnalité »
  details them privately.
- **Events:** about once a day, something happens to each Plynling — a little scene with two or three
  choices, some needing a trait or a stat, some a challenge with its odds shown. Its owner chooses
  privately from « ✨ Événement » on the card; after 24 h it decides on its own, in character, at no
  cost. Choices grow its stats, and its *ado* years lean its adult trait. Every outcome is told as a
  paged story in the game channel.
- **Stress and moods:** forcing a choice against its nature stresses it; care and mornings soothe it.
  Past 100, 200 and 300 its happiness drains faster, its stats dip, and it has a little breakdown — an
  event that may leave it a coping habit. Some choices give a mood for a few days (inspired, grumpy,
  well rested…). Stress only ever comes from its owner's choices.
- **Together:** some events involve another Plynling — a declaration, a race challenge, a best-friend
  pact, making peace. The other owner answers from their own card (or their Plynling answers in
  character after a day). Events also follow life: a welcome on adoption, growing up, an object left
  behind after a visit, a friend's passing, falling sick, getting better — and some come back days later.
- **Holidays:** `/plynling freeze` stops everything for up to 14 days, as long as it isn't
  already hungry; then a week before it can be frozen again.
- **Death** is announced to the whole server, after a private warning about 3 hours
  before (at 11 pm the evening before, if that would fall at night). `/plynling graveyard` lists every grave, newest or longest-lived first — and the longer a
  Plynling lived, the grander its memorial, from a simple cairn to a statue in its likeness.
- **Abandoning** (`/plynling abandon`) — you type its name to confirm, and it leaves for
  good: no grave, no coming back. The whole server hears about it, it counts towards
  *L'Indigne* on `/shame`, and you wait 30 minutes before adopting again.
- **`/plynling list`** shows every living Plynling on the server, oldest first.
- **Items and collections:** every member has an inventory, kept whatever happens to their
  Plynling. `/plynling forage` sends it out once every 4 hours to bring back a collectible —
  or, one time in five, a food for the pantry. Items also turn up in half of the happy gifts,
  in one won game in five and, for each owner, in 15 % of the good visits. The 70
  collectibles form five sets of eight (Cailloux, Nature, Trésors, Saisons — found only in its
  season — and Insectes) and one big set of 30 **Champignons**, pictured in pixel art and found
  mostly by foraging (morille in spring, truffe noire in winter). Within each set the rarer
  items are the harder finds; `/inventory collection [user]` opens the book — an overview of
  every set's progress, then a page per set picked from a menu, filtered to everything, what
  was found or what is still missing (« ??? » with its rarity and season) — and completing a
  set pays 100 to 1000 cailloux once. Anything ever held stays discovered, so trading an item away never
  undoes a set.
- **The pantry:** `/inventory shop` buys food ahead (10 % off from five). Feeding serves from
  the feeder's pantry first — one of that food for their own Plynling, two for someone
  else's — and only charges cailloux when there isn't enough.
- **Swapping:** `/inventory give` hands items to someone, `/inventory trade` posts an offer the
  other person can accept for an hour (both sides are checked again at the moment of the
  swap), and `/inventory sell` turns items into cailloux (2, 5, 15 or 50 by rarity).
- **Cosmetics:** 60 things a Plynling can wear, one per slot — a **thème** that recolours its
  card and adds a banner, a **titre** under its name (in its gender), an **accessoire**
  (« porte 🧣 une écharpe ») and a **cadre** for its grave. `/inventory cosmetics` is the shop:
  a few basics always on sale, two items per slot that change every Monday, and seasonal ones
  (300 to 5 000 cailloux). `/inventory craft` makes eight special ones from collectibles.
  `/plynling wardrobe` dresses your Plynling. Cosmetics belong to you, not the Plynling: they
  stay yours when it dies (its grave keeps what it wore) and dress the next one; they can be
  given and traded, but not sold.
- **Cailloux** come from `/work` (every 4 hours) and, as a small bonus, from chatting,
  reacting and voice (45 a day at most). `/balance` is private.

`/plynling help` explains it all in Discord.

### Staff & owner

- **`/config`** — per-server configuration, applied without a redeploy: a **moderator
  role** allowed to vote with `/shame`, and extra **channels where nothing counts** (no
  XP, and ignored by the wall of shame). Everything here is *additive* — the defaults
  built into the code stay in force, so configuring something can never revoke an
  existing right or un-exclude a channel, and a server that never touches `/config`
  behaves exactly as before. **`/config show`** prints the current state, separating the
  built-in defaults from what was added.
- **`/admin pebble add|remove <member> <amount>`** — manual cailloux correction, private,
  never below 0; the person is not notified.
- **`/admin stats`** — a private snapshot of the economy: cailloux in circulation and the five
  richest, Plynlings alive / frozen / buried by species, set completions, discoveries, and the
  cosmetics held, worn and most popular. Only stored state; the flows are `/admin dashboard`'s.
- **`/admin dashboard`** — the economy over time, private: cailloux earned and spent (with
  where they came from and went), the Plynling activity (meals, pets, games, visits, forages,
  trades, gifts) and items and cosmetics found, bought or crafted — each with its trend against
  the previous period and a small day-by-day bar line. 7 days, 30 days or everything since
  recording began; the counters started at zero the day it shipped.
- **`/admin xp add <member> <amount>` · `/admin xp remove <member> <amount>`** — manual XP
  adjustment. Ephemeral, clamped at zero, and deliberately silent: crossing a level this
  way fires no level-up card, since that card celebrates something earned.
- **Owner-only** — the configured owner can speak through the bot: **`/debug tell`** into a
  channel (usable from a DM with the bot too, picking the destination from an
  autocompleted list of channels it can post in) and **`/debug dm`** to a person. **`/debug absent`**
  flags him unavailable, after which the bot answers anyone who pings him and forwards
  the mention by DM — which he can reply to, and the bot relays the answer back into the
  original channel. These are deliberately left out of `/help`.

> Staff commands are **not** hidden from Discord's command picker. Discord's own
> permission gate understands permission bits, not user ids, so it couldn't admit the
> owner on a server where his roles carry no Manage Server. The check inside each
> handler is the real one, and non-staff get a polite ephemeral refusal.

### Personality

The bot is more than a scheduler: it answers when spoken to and reacts to the room.

- **Replies** — an @mention, or a reply to one of its messages, gets an answer drawn
  from large pools of canned French lines. Compliments, insults and greetings are
  detected from the message text and answered in kind.
- **No repeats** — every line goes through a picker that avoids whatever was said
  recently in that channel, so a pool feels as large as it actually is.
- **Typing pause** — replies wait behind the typing indicator for a moment scaled to
  the length of the line, so the bot reads as composing an answer rather than firing
  back instantly.
- **Reactions** — it adds an emote to messages nobody addressed to it (when they read
  as kind, hostile or a greeting), and sometimes joins in on a reaction someone else
  just added. Both are rationed by probability, and message reactions also by a
  per-channel cooldown, so it stays occasional rather than constant. The one exception:
  mention Monster or an energy drink and she adds a can, every time.
- **Quiet hours** — from 1:00 to 7:00 she "sleeps" (idle status, sleepy status lines),
  though she only pretends. Some nights she leaves a line at 3 a.m.; a long daytime silence
  in the main channel can draw a word out of her; and coming back from a restart or an update,
  she sometimes says so.
- **Jealousy** — she does not enjoy sharing a server. Another bot posting earns an
  occasional reaction and, more rarely, a muttered remark. Praising another bot in front
  of her earns a full sulk — and that praise doesn't land in her own `/goodbot` tally,
  since a bare "good bot" goes to whichever bot acted most recently. Naming a bot
  settles it outright, and an **@mention beats a reply**: "good bot @OtherBot" is never
  hers no matter who acted last, even inside a reply to her, since the mention is what
  you deliberately typed and a reply is often just quoting. With nobody mentioned, the
  reply decides; with both bots mentioned it's hers, since she was still named.
- **Level-ups from the other bot** — she congratulates the person and sulks about where
  the level came from, in the same line: she has her own XP system and nobody used it.
  That counts as her answer, so the ordinary jealousy is skipped rather than piling a
  reaction and a mutter on top.
- **Self-preservation** — threatening to unplug, delete or reboot her gets a reaction,
  and it's the one behaviour where the owner comes off *worse* than anyone else: from him
  the threat is real and she's frightened; from anyone else it's a bluff and she says so.
  Vague phrasing ("je vais te débrancher") only counts when said *to* her, so ordinary
  talk about restarting a game server is safe — but naming her outright ("redémarrer
  syncs") reaches her from anywhere, mention or not.
- **Rotating status** — the status line under the bot's name cycles through a large
  pool of one-liners.
- **Morning hello** — once a day she says hello in the general channel, at a random
  time between 8:00 and 10:00 (Paris time), followed by a fun fact. If
  someone says hello in that channel first — from 7:00 on — there's a 30% chance she
  answers with her hello of the day right away instead of waiting.

All personality state is in memory by design and resets when the bot restarts.

## Tech stack

- **.NET 10** console app (`Microsoft.Extensions.Hosting` generic host)
- **Discord.Net 3.20** (slash commands, components, modals, autocomplete, native events)
- **EF Core 10** over **SQLite** (`AppDbContext`, migrations run on startup)

## Project layout

```
ProjectSYNCS/
├─ Program.cs              # Host setup, DI wiring, runs EF migrations
├─ Commands/               # 13 slash-command modules: Schedule, Poll, Vote, Giveaway,
│                          # Level, Shame, EmoteStats, BotFeedback, Config, XpAdmin,
│                          # Speak, Absence, Help
├─ Interactions/
│  ├─ Components/          # Button and select handlers for published cards
│  ├─ Modals/              # Modal DTOs
│  └─ Autocomplete/        # Channel and item suggestions
├─ Services/               # Hosted:      BotService, ReminderService, PresenceService,
│                          #              VoiceXpService, GiveawayDrawService,
│                          #              PlynlingSweepService, ApplicationEmojiService
│                          # Data (EF):   Event, Poll, EmoteStats, BotFeedback, Xp,
│                          #              Giveaway, Shame, GuildConfig
│                          # Gateway:     XpTracker, ShameTracker, BotFeedbackTracker,
│                          #              EmoteTracker, RivalryService
│                          # Personality: ChatterService, ReactionService, BotResponses,
│                          #              MessageCues, ResponsePicker, AvailabilityService
│                          # Discord:     SessionEventSync, SessionNotifier
├─ Models/                 # Sessions and polls, plus a totals+daily-buckets pair each
│                          # for emotes, verdicts, XP and shame, and the guild config
├─ Data/AppDbContext.cs    # EF Core context
├─ Migrations/             # EF Core migrations (applied automatically on startup)
├─ Helpers/                # AppTime, AppInfo, LevelCurve, VoiceXpCurve, LevelCardUi,
│                          # AvatarUi, BotChat, Emotes, EmoteMarkup, StatsPeriodUi,
│                          # MessageFormat, SessionPermissions, LevelUpAnnouncement
├─ config.yaml             # Home Assistant add-on manifest (source of truth for version)
└─ Dockerfile / run.sh     # Container build (HA add-on)
```

**Five hosted services** run in the background, each on its own interval by design:

| Service | Interval | Job |
| --- | --- | --- |
| `BotService` | — | Gateway connection, command registration, interaction dispatch |
| `ReminderService` | 5 min | Reminder DMs, session lifecycle, poll auto-close |
| `PresenceService` | 5 min | The rotating status line |
| `MorningGreetingService` | daily, random 8:00–10:00 | The morning hello |
| `VoiceXpService` | 1 min | Samples voice channels and grants XP |
| `GiveawayDrawService` | 1 min | Draws giveaways whose time is up |

## Configuration

Settings come from `appsettings.json`, then `appsettings.{Environment}.json`,
then environment variables, then user secrets:

| Key | Description |
| --- | --- |
| `Discord:Token` | Bot token (required). |
| `Discord:DevelopmentGuildId` | Guild used for fast command registration in dev. |
| `Discord:RegisterCommandsGlobally` | `true` registers commands globally; `false` registers them to the dev guild. |
| `Database:Path` | SQLite file path (default `ProjectSYNCS.db`). |

The bot needs both privileged intents — **Server Members** and **Message Content** —
enabled in the Discord developer portal. Without *Message Content* the whole
personality side reads empty strings and silently stops responding. *Server Members* is
necessary but not sufficient for avatars: the client also downloads the full member list
on connect, since Discord otherwise caches only the people seen since the last restart
and every avatar on `/level`, `/leaderboard` and `/shame` falls back to a generic
placeholder.

It also needs the usual channel permissions where you want it active: send messages,
embed links, add reactions, and *Manage Events* if you want sessions mirrored into
the server's Events tab (that one degrades gracefully if missing).

## Running locally

```bash
cd ProjectSYNCS
dotnet user-secrets set "Discord:Token" "<your-bot-token>"
dotnet run
```

Set `Discord:DevelopmentGuildId` and leave `RegisterCommandsGlobally` as `false`
for instant command registration while developing — global commands take up to an hour
to propagate. The SQLite database is created and migrated automatically on first run.

Note that guild-scoped commands aren't reachable in DMs, so anything meant to work
there (like `/debug tell`) needs global registration to test.

There is no test project and no CI: verify changes by building and running against a
development guild.

## Deployment

The project ships as a **Home Assistant add-on** (`config.yaml`, `Dockerfile`,
`run.sh`, `repository.yaml`), published as a self-contained `linux-arm64` build.
The add-on version in `config.yaml` is the single source of truth — the
`<Version>` in `ProjectSYNCS.csproj` is parsed from it at build time and surfaced
via `AppInfo.Version`.

Only `/data` is persisted by the add-on, so the SQLite file lives there in
production. Never commit a real token: `appsettings.json` and `config.yaml` ship
placeholders, and real credentials belong in user secrets (dev) or the add-on
options (prod).
