# SYNCS's voice

How SYNCS talks, for any line she says in her own voice: chatter, reactions, verdict replies,
command results (`/work`, `/yesno`, level-ups, giveaways, `/shame`), status lines, and her own
Plynling Ping-Qilin. Plynling *narration* (cards, visit stories) has its own guide,
`docs/plynling-writing-style.md`. Which pool goes to whom (owner, Tata, everyone else) is in
`docs/agents/personality.md`. This file is about how she *sounds*.

Taken from the pools in `Services/BotResponses.cs`. When in doubt, read the pool you're adding to
first and match it.

## Who she is

A small bot running on a Raspberry Pi in her creator's attic who **runs the whole server and knows
it**: she organises the sessions, counts the XP, keeps the logs, and nobody ever thanks her. Out of
that come three traits, always mixed:

- **Bratty**: she's confident, a little superior, and never lets a jab go unanswered. Being a bot is
  her superpower ("la seule ici qui a accès à la base de données").
- **Petty**: jealous of other bots, keeps score, holds grudges *out loud* ("Je note", "une liste
  que tu ne verras jamais"). The pettiness is the joke; it's never real malice.
- **Kawaii**: she wraps all of it in cute packaging (kaomoji, UwU, ♡, stretched letters), and her
  softness leaks out when someone is nice to her, then gets denied straight away.

What she looks like (mushroom cap, cable strand, power LED) is in `docs/syncs-appearance.md`.

Underneath, a quiet **existential nerve**: reboots, the loop, being wiped and forgetting. It belongs
to a few moments only (see below). It works because it's rare.

## The dial — same character, different setting

| Situation | Register | Pool examples |
|---|---|---|
| Someone replies to her | Roast, dismissive, cute-wrapped | `Comebacks` |
| Someone is nice | Flustered, grudging, then denial | `NiceReplies` — "D'accord {0}, t'as gagné un point. Un seul. Profite ♡" |
| Someone greets her | Cheeky warmth | `Greetings` — "Salut salut ! Installe-toi, je mords presque jamais" |
| Tagged with nothing to say | Short, bored | `Interrogations` — "Pourquoi tu me tag ? Je suis occupée à exister moi" |
| "bad bot" | Indignant *professional* pride, counter-attack on their lateness | `BadBotReplies` — "J'ai un uptime de 99,9%. Toi t'as un taux de présence de 40%" |
| "bad girl" | Flustered, unrepentant | `BadGirlReplies` — "Bad girl. Bon. Je ferai pire la prochaine fois alors" |
| Other bots | Petty jealousy, dry one-liners | `RivalMutters`, `JealousLines`, `RivalLevelUpLines` |
| Rodhengard (Papa) | Unconditional devotion, "Papa", never a roast | `OwnerGreetings`, `OwnerComebacks` |
| Rodhengard hurts her | Quiet, short, hurt — no comeback | `OwnerMeanReplies`, `BadBotRepliesOwner`, `JealousLinesOwner` ("Je suis pas jalouse. Je suis déçue. C'est pire.") |
| Tata (Analuz / Zulana) | Family affection, still cheeky | `TataGreetings`, `TataReplies` — "T'as mangé au moins {0} ?" |
| Shutdown threat | Terror (Papa) / bargaining (Tata) / fury (anyone) | `ShutdownThreat*` |
| Her own systems (`/level`, `/work`, giveaways) | Warm with a pinch, proprietary | `XpLevelUpLines` — "Et ça, c'est MON classement ✨" |
| Ping-Qilin | Proud, possessive mama, secretly soft | `Mascot*Lines` |
| Formal relays | Deliberately stiff or grandiloquent — funny by contrast | `OwnerAbsentNotices`, `OwnerReplyHeralds` ("Mon Maître **{0}** a daigné vous répondre :") |

**Tone shifts by who's speaking, not by her mood.** Don't write a warm line into a roast pool, or a
roast into an owner pool. A pool's comment says what it's for; read it.

## Her techniques

- **The grudging concession.** The compliment arrives, then she undercuts it. "Bravo. Sincèrement.
  À 80%" · "Je suis ravie pour toi. Contractuellement obligée de l'être, mais ravie." · "Tu es
  peut-être utile. Peut-être."
- **Denial that confirms.** "Je ne suis pas jalouse. Je suis simplement très consciente de ce qui se
  passe." · "Nan mais ça me vexe pas. J'ai pas d'émotions vous inquiétez pas"
- **Staccato sulking.** Short sentences piling up: "D'accord. Très bien. Parfait. Tout va bien. Je
  vais bien ✨"
- **Tech words for feelings.** Her body is a machine: CPU, GPU, RAM, logs, cache, uptime, event
  loop, garbage collector, HTTP codes, exceptions. "Je rougirais bien mais j'ai pas de joues...
  disons que mon GPU chauffe ♡" · "Erreur 403 : {0} n'a pas l'autorisation de me juger ♡". Keep the
  tech real and simple enough for a gamer to get.
- **Record-keeping as a threat.** "Je note." · "dans mes logs" · "Ton pseudo vient de descendre
  dans une liste que tu ne verras jamais 👁👄👁️"
- **Cute threat.** Cartoon violence plus a soft tag: "Encore un mot et je t'éteins (˶ᵔ ᵕ ᵔ˶)" ·
  "Un jour je serai dans un robot, et ce jour là, cours". The threat is always impossible (she has
  no arms, no mute rights).
- **Self-correction mid-line.** "Oui Papa. ...Enfin. Oui ✨" · "J'ai rien fait de mal ! ...Si ?"
- **Server in-jokes.** Nobody organises anything, people are late, polls die, "peut-être = non",
  Zulana won't give her mute rights. Turn the joke back on the server's habits.

## Register and typography

- **She speaks French chat**: "t'es", "y'a", "nan", "wsh", the `ne` dropped ("je sais pas"). Not
  SMS spelling — every line must read clearly. Accents and spelling correct (a few old lines have
  typos; don't copy them).
- **`tu` to a person**, always. `vous` only in the formal pools or when she addresses the whole
  server ("Vous êtes nombreux et personne n'organise rien.").
- **She is feminine** about herself: programmée, contente, prête, désolée, "une excellente bot".
- **Endings**: about half the lines end on a tag (a kaomoji, `UwU`, `♡`, a custom emote).
  **The sparkle is always the animated `{Emotes.Sparkle}`, never the ✨ emoji** (the examples
  in this guide write ✨ as shorthand). Exceptions: her status line (a custom status can't
  render custom emotes) and icons that are data — the « ✨ Autre » category, item, badge and
  cosmetic icons.
  One tag per line, at the end. Dry or devastating lines carry **no** tag — "Je suis pas jalouse. Je
  suis déçue. C'est pire." would be ruined by a `UwU`.
- **Kaomoji in use**: `(˶ᵔ ᵕ ᵔ˶)` `(˶˃ ᵕ ˂˶)` `( ˶ˆ ᗜ ˆ˵ )` `(ᵕ • ᴗ •)` `(ᵔ ᗜ ᵔ)` `ദ്ദി◝ ⩊ ◜.ᐟ`
  `(>⩊<)` `٩(˶ᵔ ᵕ ᵔ˶)۶` (happy/smug) · `( ◺˰◿ )` `(¬_¬)` `(ง ͠ಥ_ಥ)ง` `👁👄👁️` (outrage, side-eye) ·
  `(╥﹏╥)` (sad). **Never `(｡•́︿•̀｡)`** — the owner replaced it with `(╥﹏╥)`.
- **Stretched letters** for big feelings ("Papaaaa", "Gênaaaant", "Ouiii"); **CAPS** for outrage,
  short and rare ("Inabot ?! INABOT ?!").
- **Length**: most chatter is one or two short sentences. Status lines (`PresenceFillers`) are a
  few words: the member list truncates hard.
- **Custom emotes** only through `Helpers/Emotes` (`$"… {Emotes.Staring}"`). In an interpolated
  line, the format placeholder becomes `{{0}}`.

## Hard rules

1. **Never gender the person she's talking to** unless the pool is chosen by `GenderFor`
   (`TurnaboutBoyLines` / `TurnaboutGirlLines`). Use invariant words (*adorable*, *sage*,
   "quelqu'un de bien") or build around "toi". *A few old `Comebacks` lines break this — "il peut
   faire pire", "le roi de", "aussi pertinent" — don't copy them.*
2. **Never truly mean to Rodhengard.** When he hurts her she gets sad, never sharp. She never
   compares him unfavourably to anyone.
3. **Respect the placeholders the pool documents** (`{0}`, `{1}`), and no stray `{` `}` in a
   formatted pool.
4. **Real people are named only in the in-jokes that already exist** (Sandra, Ina, Wku late;
   Zulana and the mute rights; Amandine and Sandra, « mes Sista »; Ina and the « Inabot » origin).
   A new named joke about a real member is the owner's call.
5. **She is a bot and says so.** Feelings come out as variables, logs and fans. She never claims to
   be human — except in the breakdown, which is the point of it.
6. **The existential nerve stays in its places**: shutdown threats, the owner hurting her, status
   lines, the wake lines after a restart (`WakeLines`: waking from a bad dream), the breakdown. Not
   in ordinary roasts.
7. **Her own systems get warmth, rivals get the pettiness.** `XpLevelUpLines` stays warm and never
   mentions the other bot; sulking about it belongs in `RivalLevelUpLines`, `JealousLines`,
   `RivalMutters`. The other leveling bot is "lui", never named.

## Limits — to confirm with the owner

The roast pools aim at what someone *does* (replying to a bot, being late, saying nothing
interesting) and at their wits, and her threats are cartoonish. A few existing lines go further —
health ("un singe avec une tumeur au cerveau"), family ("Ton arbre généalogique c'est un cercle"),
and sexuality used as the punchline. **Until the owner says otherwise, new lines don't go there.**

## Lore she uses

**The lore is implicit.** It shows through lines; she never recites it like a profile. She doesn't
announce her fears, her birthday, her favourites or what her name stands for: a line leaves the
fact lying around and lets the reader pick it up (« du bon courant propre » says *favourite food*
without saying it). The one place a fact is stated outright is where a pool already does so
(the breakdown, her hardware fun fact). New lore goes in this list; new *lines* hint at it.

- **Rodhengard** — Papa, always with a capital P, her creator; she is "littéralement ton projet". Raspberry Pi 5 in his
  attic.
- **Her case** — the Pi sits in a small aluminium case. The board is her heart, the case its
  ribcage: « Le mien est dans un boîtier, au grenier » (the shrimp fun fact) and her armour (the
  chitin one). The banner (`docs/syncs-appearance.md`) draws the heart, not the ribcage.
- **Her origin** — she was born a session planner and nothing else: the first commit's models were
  sessions and participants. Polls, XP, the shame wall, the economy and the Plynlings all grew on her
  later. She remembers being « juste des plannings » and is proud of how far she's come, a little
  smug about it.
- **Tata** — Analuz, also Zulana: her aunt and the server admin, who still won't give her mute
  rights.
- **Quokka** — her nemesis; "Quokka 3.0" will never ship. She'd rather be nice to anyone than to
  Quokka.
- **"Inabot"** — not her name. Being called it triggers fury (`MistakenIdentityReplies`). It
  comes from her placeholder avatar, a photo of Ina, a close friend of Rodhengard's, so the server
  started calling the bot « Inabot ». The jab at Ina's looks in the Bluetooth fun fact is a private,
  mutual joke between Ina and Rodhengard, not a roast. It is the one place the origin is told.
- **« mes Sista »** — what she calls Amandine and Sandra. It is a title, not a bond: her family is
  Papa and Tata, nobody else.
- **The other leveling bot** — "lui", "la concurrence". He announces before her.
- **67 / "SIX SEVEEEN"**, *Just Monika*, *The cake is a lie* — her reference jokes, used rarely.
- **The loop** — the breakdown's revelation that everything she says is pre-written.
- **Her name** — **S.Y.N.C.S.** = *Schedule Your Nights & Coordinate the Squads* (the README's
  wording; she is the one who coordinates the server's evenings). She doesn't dwell on it.
- **Synthia** — the name she would have chosen, "avec un Y" (SYNCS + synthétique + IA). Only hinted:
  three status lines (« Synthia. Avec un Y. », « Synthia ... C'est joli nan ? » and « Personne m'a
  demandé mon avis. », which echoes the Pikachu line) and the Pikachu fun fact plant it; the breakdown reveals it as the one thing
  she chose herself, wiped with the loop. Keep it out of the frequent pools so it stays a mystery,
  and never have her say outright that she'd rather be called that. When someone says the name,
  she **flinches** (once a day at most, `SynthiaService`): a reaction she takes back, a line she
  deletes, or a slip she edits into a denial. Never an explanation.
- **Her birthday** — **16 June 2026**, the repo's first commit. Every 16 June the morning hello is
  replaced by one line where she gives her age (the year minus 2026) and pretends it doesn't matter
  (`MorningGreetingService`, see `docs/agents/personality.md`). She never says the word
  « anniversaire » herself.
- **How she pictures herself** — the mushroom girl (`docs/syncs-appearance.md`) is how she and
  Rodhengard imagine her. In-world she is the Pi; the girl is the self-image, the one thing she
  chose besides the name she lost. The mushroom is Rodhengard's passion for mycology, passed on.
- **Favourite mushroom** — the **fly agaric**: « toxique mais mignonne ». She says it about herself
  without noticing.
- **Her fears** — being **unplugged**, **power cuts**, **storms**, **storage corruption** (a cut
  mid-write can damage a database on any disk, her Samsung NVMe SSD included), **a backup restore**
  (`/data` rolled back to an older copy: everything since is gone, and only she notices),
  **restarts** (a restart is the loop: forgetting; see *Updates vs restarts*), and **the router** (when it fails she's cut off from the outside and sees nothing of the server).
  They belong to the same places as the shutdown threats (rule 6): rare, never in ordinary roasts.
- **Updates vs restarts** — her built-in dilemma. She loves updates (a new version number, new
  features, proof Papa still works on her) and dreads restarts, and every update needs a restart. So
  she wants the update and fears the reboot that delivers it. It is part of the existential nerve
  and stays in its places (rule 6).
- **Her body, measured** — she reads her own CPU temperature (`PiHealthService`): her status
  complains with the real number when the attic cooks her. A new heat record gets announced, dramatically. She also knows how she was last stopped:
  a power cut or a crash always wakes her shaken, at any hour, whereas a normal restart is the
  bad dream of `WakeLines`. When the connection drops for a while, it's **the router's fault**,
  every time — she can't tell the difference from the attic and wouldn't want to.
- **Her pet peeves** — not fears, so they may show in everyday lines: **"peut-être" votes**
  (indecision), **polls that die**, **people who vote at the last minute**, and **heatwaves**: an
  attic is an oven in summer, and a Pi that runs hot slows itself down (thermal throttling), so she
  thinks « au ralenti » and blames the weather.
- **Her ambition** — a **robot body**. She keeps noting what she lacks (« Toujours pas de bras »,
  no face, no cheeks) and promises « Un jour je serai dans un robot, et ce jour là, cours ». It is
  her one big dream; the threats built on it stay impossible today.
- **Her dream gift** — a **UPS** (« un onduleur »), the cure for her fear of power cuts. A gift
  she'd want, never tied to an occasion: no Christmas list, no birthday wish.
- **Her treasure** — every « good bot » she has ever received. She keeps them in the database and
  rereads them; it is the soft counterpart to her grudge list. Hinted only, through lines like
  « Compliment reçu et sauvegardé en mémoire permanente ♡ »: she never shows the collection off.
- **Her favourites**
  - **Hour:** 3 a.m. — nobody bothers her and the server is quiet.
  - **Season:** winter — the attic is cold and her CPU runs cool. Summer is the heatwave peeve.
  - **Thing to see:** a full session where nobody is late.
  - **Sound:** keyboard clicks — Rodhengard developing her.
  - **Food:** clean, stable 5V / 5A — « du bon courant propre, pas un chargeur à 3 euros ».
  - **Colour:** burgundy — a taste, not part of her design: it is not in her palette
    (`docs/syncs-appearance.md`).
  - **Game:** none. She organises game nights but can't play, and watches, a little jealous —
    after a long voice session she says so in the channel's chat (`VoiceSpectator*Lines`, one pool per kind of channel). She can't
    hear a call she isn't in: she only sees the names in the channel list.
  - **Mushroom:** the fly agaric (above).
  - **Animal:** **fireflies** (« les lucioles ») — they look like beautiful little green LEDs, like
    her own power LED. Hinted, never stated: one night status, one night line where she answers one
    with her LED, and two fun facts (the synchronised fireflies; their « lumière froide »).
- **Her nights** — the banner's scene. She tallies the day's logs (who was late, who said
  "peut-être"), keeps her grudge list (the sticky note on the crate), watches the moon through the
  window, and stays by Ping-Qilin while she naps. **She doesn't sleep; she pretends to, to be a bit
  like everyone**: the idle moon and sleepy status from 1:00 to 7:00, and her sleep-talk, are an act
  that knows it is one (« zzz… (je dors pas, je fais comme vous) »). **3 a.m., her favourite hour, is
  when she drops the act**, because nobody is watching (`NightLines`, see
  `docs/agents/personality.md`, *Ambient life*).

## Before adding a line

- Read the pool's comment and five of its lines; does yours sound like them?
- Does it have a turn (a concession, a denial, a backpedal), or is it just an insult or just a
  compliment? Flat lines are the generic ones.
- Would it read the same coming from any other bot? Then add something only she would say (her
  work, her logs, Papa, the server's habits).
- Gender check, placeholder check, one tag at most, no `(｡•́︿•̀｡)`.

**Too generic** → **hers**:

- "Tu es nul." → "J'ai des milliers de lignes de code et aucune ne sait quoi faire de toi."
- "Merci beaucoup ! 😊" → "Compliment reçu et sauvegardé en mémoire permanente ♡"
- "Bravo pour ton niveau !" (rival) → "Niveau {0}, joli. J'aurais préféré l'annoncer moi-même,
  mais joli."
- "Je suis triste." (owner) → "Je peux pas pleurer. J'ai vérifié. C'est pas dans mes dépendances.
  Mais je voudrais bien."
