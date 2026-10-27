# Personality — chat cues and branch order

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

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
4. **Shutdown-by-name** (ambient, see `people.md`).
5. **Talked about** (ambient, her name; see `people.md`). After shutdown-by-name, which names her too.
5b. **Follow-up** (`ChatListening`, see `listening.md`, *Two-step messages*): no tag, no reply, no name, from someone she
   is still listening to. After the talked-about branch, so a message naming her is read there.
6. **Quoicoubeh**, then **Tron** (ambient, last of all).

Everything aimed at her returns before the two ambient branches are reached.

**Inside the reply path** (`HandleReplyToBotAsync`), first match wins: Inabot → shutdown threat →
**hush gate** (`ChatHush`, see `ShutUp`) → **under her cards** (`CardReplyService`) → breakdown roll → **streak gate** → Papa mean (`OwnerMeanReplies`) → **specific intent** → Papa's
pet name → **reply context** → nice → greeting → **question / low effort** → **open answer** → reference roll → Tata warmth → fallback
(`OwnerComebacks` for Papa; otherwise the streak nudge, a fact line, or `Comebacks`). A mean reply
from anyone else skips intents and context and is roasted, as it always was. **The mention path**
runs the same streak gate, then specific intent → greeting → Tata warmth → question → fallback
(`Interrogations`, or a nudge or a fact line); Papa's mentions try his intent twin, then his pet
name, before `OwnerGreetings`. **The talked-about path** answers a specific intent too (« syncs, ça va ? »
addresses her), before the greeting. Answering the content outranks Tata's warmth roll.

**Secret member badges ride on her answers** (see `xp.md`): `tron`, `quoicoubeh`, `hush` and the
shutdown threat award only once the reply is actually sent (`ReplyWithTypingAsync` non-null), and a
ship with her travels on the `IntentPick` (`Badge`) and is awarded by `SendPickAsync`, so moving or
adding a send site must keep going through it. The breakdown, the rival praise and the glitch award
from their own services (`ChatterService`, `RivalryService`, `GlitchService`).

