# Personality — people: favouritism, shutdown threats, her name, other bots

Part of the personality subsystem: read `docs/agents/personality.md` (the index and the rules
that apply to all of it) first.

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

