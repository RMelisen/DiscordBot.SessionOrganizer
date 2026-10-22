# Member badges (« Succès ») — design

Date: 2026-10-22 · Target version: **7.34.0** (minor: new behaviour + a migration)

## Intent

Today a *person* only has collection badges (on `/level` and `/inventory`); Plynlings have their
own badges, which belong to the creature. The owner wants many more badges for people, of three
kinds:

- **Paliers** (tiered): « how much you've done », Bronze → Argent → Or → Platine → Diamant, on the
  same ladder and icon scheme as the collection badges.
- **Moments**: earned once, for something that happened.
- **Secrets**: hidden badges tied to her easter eggs and quirks; nobody knows they exist until
  they find one.

Badges are **discovered, never announced**: earning one posts nothing anywhere. People find them by
opening « Succès » on a `/level` card.

## Goals

- `/level` shows **icons only**, one line, for everything the member has earned.
- A new **« Succès »** button on `/level` opens the full detail of *that card's member*.
- Unearned visible badges show as **???** with a small hint that suggests without telling.
  Unfound secrets show as **???** with a deliberately cryptic hint.
- Everything already stored counts retroactively the day it ships.

## Non-goals

- No announcement, DM, reaction or log line when a badge is earned.
- No badge leaderboard, no rewards (no cailloux) for member badges.
- No changes to Plynling badges or to how collection badges are computed or shown in `/inventory`.
- No backfill of hook-based badges (they start counting at 7.34.0).

## Anti-goals (would make this a failure even if it works)

- A database write per message (root `CLAUDE.md`, *Reliability*). Chat-path secrets are rare
  events, de-duplicated in memory before any write.
- A secret badge's trigger leaking: no trigger in `/help`, `README.md`, `CHANGELOG.md` or a hint
  that gives it away. The changelog may say "hidden badges exist", nothing more.
- A badge write breaking the flow it rides on (a failed award must never fail a feed, a quiz win,
  a chat answer…): swallow and log, like every other side effect.

## The catalog

All badges are **per guild** (`GuildId`, `UserId`), like XP. Names are French and gender-neutral
(most members' genders are unknown). Keys are **stable and append-only** (stored, and part of icon
file names). Hints are first drafts for the owner to edit.

### Paliers (tiered, derived — never stored)

Computed at display time from existing data, like `CollectionTiers`. Thresholds can move without a
migration.

| Key | Icon | Name | Counts | Bronze / Argent / Or / Platine / Diamant | Hint (unearned) |
|---|---|---|---|---|---|
| `voice` | 🎙️ | Pilier du vocal | `MemberXp.VoiceMinutes` | 10 h / 50 h / 150 h / 400 h / 1000 h | « Certaines voix finissent par faire partie des murs. » |
| `companion` | 💜 | Compagnie de SYNCS | `VoiceCompanionStat.Minutes` | 5 h / 25 h / 75 h / 200 h / 500 h | « Elle aussi aime qu'on lui tienne compagnie. » |
| `quiz` | 🧠 | Grosse tête | `QuizStat.Wins` | 1 / 10 / 25 / 50 / 100 | « Répondre vite, répondre juste. » |
| `emotes` | 😀 | Pluie d'emotes | `MemberXp.ReactionsUsed` | 100 / 500 / 2000 / 5000 / 10000 | « Un petit clic sous chaque message. » |
| `attended` | 🎮 | Toujours là | sessions attended (below) | 1 / 5 / 15 / 30 / 60 | « Dire oui, c'est bien. Venir, c'est mieux. » |
| `organised` | 📅 | Chef d'orchestre | sessions organised (below) | 1 / 5 / 15 / 30 / 60 | « Quelqu'un doit bien proposer une date. » |
| `giveaways` | 🎁 | Main chanceuse | `GiveawayEntry.IsWinner` rows | 1 / 2 / 3 / 5 / 10 | « La chance sourit à ceux qui participent. » |
| `goodbot` | 💖 | Chouchou | `BotFeedback.GoodCount` | 10 / 50 / 150 / 400 / 1000 | « Elle se souvient de chaque compliment. » |
| `breeder` | 🐾 | Famille Plynling | Plynling badges earned by all Plynlings the member owns | 5 / 15 / 30 / 60 / 100 | « Leurs exploits rejaillissent sur toi. » |
| `arcade` | 🕹️ | Pilier d'arcade | arcade wins (new counter) | 5 / 25 / 75 / 200 / 500 | « La partie ne va pas se gagner toute seule. » |
| `nanny` | 🤲 | Nounou | meals given to others' Plynlings (new counter) | 5 / 25 / 75 / 200 / 500 | « Un repas offert n'est jamais perdu. » |
| `seniority` | 🎂 | Ancienneté | time since `IGuildUser.JoinedAt` | 3 mois / 6 mois / 1 an / 2 ans / 3 ans | « Le temps fait son œuvre. » |

Definitions and known caveats:

- **`attended`**: `Participant.FirstSeenInVoiceAt != null` on a session that is not cancelled.
  Only Game/Movie sessions are watched, and only Joined/Maybe participants — that is the existing
  roll call, accepted as is.
- **`organised`**: `SessionEvent.OrganizerId == user`, not cancelled, and **`ScheduledAt` in the
  past** (so creating future sessions doesn't farm it). Sessions created from `/tonight` already
  carry the call's organizer.
- **`emotes`** is a net count (removals decrement), and both XP counters started at zero when they
  shipped — accepted.
- **`giveaways`**: `/giveaway delete` hard-deletes a giveaway and its entries, so a won giveaway
  can vanish and the tier drop. Accepted (rare, staff action).
- **`breeder`**: abandoned Plynlings are hard-deleted with their badges, so those badges stop
  counting. Accepted — abandoning is a choice.
- **`companion`**: her voice seat is home-guild only, so in other guilds this stays at zero.
- **`seniority`**: `JoinedAt` is the *latest* join; rejoining resets it. Accepted. Null
  (`GetUser` returns null) → the badge is simply unearned.

### Moments (earned once)

| Key | Icon | Name | Earned when | Source | Hint (unearned) |
|---|---|---|---|---|---|
| `crowned` | 👑 | Couronne | first time crowned the week's Grosse tête (every tied champion) | hook: `QuizCrownService.CrownAsync`, only on the real weekly crowning (`pay == true`), never `/debug` | « Une semaine, une seule tête. » |
| `lightning` | ⚡ | Éclair | a quiz win under 3 s (`QuizStat.BestMs > 0 && < 3000`) | derived | « Plus vite que son ombre. » |
| `rallier` | 📣 | Rassemblement | a `/tonight` call of theirs reached its minimum (the organizer) | hook: `TonightComponentHandler.AnswerAsync`, inside the existing `ClaimThresholdAsync` branch | « Ce soir, ils sont venus. » |
| `punctual` | ⏰ | Ponctualité | in voice before a session's start: `FirstSeenInVoiceAt < ScheduledAt` on a non-cancelled session | derived | « Le premier arrivé attend les autres. » |
| `quokka` | 🦘 | Chasse au Quokka | first to catch one of Quokka's visits | hook: `PartyComponentHandler.OnQuokkaHitAsync` (the winning click) | « Il passe, il nargue, il repart… sauf si. » |
| `generous` | 🤝 | Générosité | first item given with `/inventory give` | hook: `InventoryService.GiveOnceAsync` (trades don't count) | « Ce qui est à toi peut être à quelqu'un d'autre. » |

The two **derived moments** (`lightning`, `punctual`) are never stored: nothing records when they
became true, so the « Succès » view shows them **without a date** (builder's call, so the read
path never writes).

Note on `quokka`: a Quokka visit records only the **one** person who caught it — nobody else takes
part — so the badge is « caught a visit », not « took part ».

### Secrets (hidden, earned once)

Shown as **???** with the cryptic hint until found; then name + hint + date earned. All are
guild-only (DMs never earn one) and come from singletons on the chat path (see *Awarding*).

| Key | Icon | Name | Earned when | Hook | Cryptic hint |
|---|---|---|---|---|---|
| `night_owl` | 🌙 | Oiseau de nuit | answered her 3 a.m. line (the person she scolds) | `AmbientService.HandleMessageAsync`, where `_scolded.Add(...)` succeeds | « Trois coups sonnent, et quelqu'un répond. » |
| `breakdown` | 💥 | Point de rupture | their message set off a breakdown — random roll **or** the passphrase | `ChatterService`, both breakdown sites (mention path and reply path) | « Tout ce qui tourne finit par se dérégler. » |
| `quoicoubeh` | 🗣️ | Quoicoubeh | she answered them « Quoicoubeh » | `ChatterService.TryQuoicoubehAsync`, after the send | « Une question appelle toujours une réponse. » |
| `tron` | 💡 | Tron | she answered them « Tron » / « Trouille » | `ChatterService.TryTronAsync`, after the send | « Certaines fins de phrase allument quelque chose. » |
| `glitch` | 👾 | Témoin du bug | her glitched line was a reply to their message | `GlitchService.ReplyWithTypingAsync` when the glitch fired and `replyTo.Author` is a human (not a bot / rival) | « Un instant, ses mots ont tremblé. » |
| `synthia` | 😨 | Le nom interdit | said « Synthia » and she flinched | `SynthiaService.HandleMessageAsync`, after the daily claim succeeds | « Il est des noms qu'il vaut mieux taire. » |
| `threat` | 🔌 | Menace | threatened to shut her down | `ChatterService.TryHandleShutdownThreatAsync` (all three paths reach it) | « Un câble, une prise, une mauvaise idée. » |
| `traitor` | 🤖 | Trahison | typed « good bot » to a rival bot (she notices the stolen praise) | `RivalryService.OnPraiseStolenAsync`, praiser = `praise.Author` | « Les compliments ont une adresse. Pas celle-là. » |
| `first_wake` | 🌅 | Premier réveil | first human to speak in the main channel after an **abrupt** wake (power cut or crash), whichever wake line she used, within **2 h** of it | new flag set in `AmbientService.WakeAsync` when the wake is abrupt **and the wake line was actually posted** (expires after 2 h), consumed by the first human message in the main channel in `HandleMessageAsync` (its own flag, **not** `_pendingEdit`, which the idle edit pair shares) | « Quand elle rouvre les yeux, qui est là ? » |
| `weathervane` | 🔄 | Girouette | « good bot » then « bad bot » (or the reverse) to her within 60 s | new per-user in-memory « last verdict + time » in `BotFeedbackTracker`, checked **after** `TryClaim`: a verdict counts when the claim came back `Granted` or `AlreadyJudged` on one of *her* actions (so a bare « good bot » counts, one aimed at a rival or at nothing doesn't); typed verdicts only | « Le vent tourne vite, ici. » |
| `heart` | 💘 | Cœur de silicium | **shipped themselves with her**: one side is her (any `Herself*` kind) and the other side is the asker — `Rating.Rate(otherSide, askerId, …).AboutAsker` (« moi » or their own mention). The asker earns it; shipping her with anyone else earns nothing | `ShipModule.ShipAsync` **and** the chat path (« ship X et Y » in `ChatterService`, which renders through `ShipModule.Render`), guild only; `@random` never draws her, so it never earns it | « Le cœur a ses raisons que le code ne connaît pas. » |
| `hush` | 🤫 | Chut | told her to be quiet (the ShutUp intent, which hushes the channel) | `ChatterService.TryIntent`, at `_hush.Hush(...)` | « Le silence est d'or. Elle préfère l'argent. » |
| `critic` | ⭐ | Critique | `/rate` on her (`RatingKind.Herself`) | `RateModule.RateAsync` (guild only) | « Juger le juge. » |

Notes:
- `synthia`: her flinch is capped once a day across every guild, so only one person a day can earn
  it. Accepted — it makes it rarer.
- `traitor`: there is no « praise Quokka » path in the bot (« quokka » is a *mean* cue), so it is
  rival bots only.

## Storage

One new table, **`MemberBadge`**: `Id`, `GuildId`, `UserId`, `Key` (string), `Count` (long),
`EarnedAt` (UTC `DateTimeOffset`). Unique index on (`GuildId`, `UserId`, `Key`); snowflakes
`.HasConversion<long>()`.

- A **moment or secret** is a row with `Count = 1`; `EarnedAt` is when it was earned. Insert only if
  absent.
- The two **new counters** (`arcade`, `nanny`) are rows whose `Count` goes up; `EarnedAt` is the
  first increment. Their tier is derived from `Count` like the other paliers.
- Keys are append-only (root `CLAUDE.md`, *Data*). Removing a badge from the catalog leaves its rows
  inert; the display ignores unknown keys.

Migration: `AddMemberBadges` (schema only).

## Awarding

- **Transient paths** (`FeedAsync`, `GiveOnceAsync`, `TonightComponentHandler`, `ArcadeRunner` /
  `PartyRunner` endings, `QuizCrownService`, `PartyComponentHandler`, `RateModule`, `ShipModule`):
  write through a transient `MemberBadgeService` in the same unit of work when one exists (the feed
  counter joins `FeedAsync`'s single save), else in its own scope. Wrapped so a failure is logged
  and swallowed.
- **Singleton chat paths** (`ChatterService`, `AmbientService`, `SynthiaService`, `GlitchService`,
  `RivalryService`, `BotFeedbackTracker`): go through one singleton **`BadgeAwarder`**
  (`IServiceProvider` + `CreateAsyncScope`, per root `CLAUDE.md`), called fire-and-forget off the
  gateway handler. It keeps a `BoundedSet` of (guild, user, key) already awarded this run, so a
  repeat triggers no write at all. Singletons that lack `IServiceProvider` today (`AmbientService`,
  `SynthiaService`, `GlitchService`, `RivalryService`) inject `BadgeAwarder` instead.
- **Arcade win definition** (`arcade` counter): +1 for each human the game's own outcome names a
  winner — Duel winner; Heist success → each crew member; Race and Crash → each player who ends
  with a positive net; Blackjack Natural/Win; Bomb → everyone but the loser; Temperature winners;
  board games → the winning human (including a human beating her at Puissance 4); Quote → the
  correct guesser. Quokka visits are **not** arcade wins (they are the `quokka` moment). Stake-0
  games count.
- **`nanny`**: +1 in `PlynlingService.FeedAsync` whenever `actorId != plynling.OwnerId` and the meal
  happened, in the same save.

## Display

### `/level` card

The current « **Collections** · badges / -# N complétions » block is replaced by **one icon line**
with everything the member has earned, in this order: collection badges (unchanged rendering, via
`CollectionTiers.Badge`), paliers at their tier, moments, found secrets. Under it a `-#` line:
« N succès » (collection set badges count as one each; secrets count once found). The block is
absent when nothing is earned. Same Separator + TextDisplay as today → the card stays at 10
components.

The action row gains **« Succès »** (secondary, 🏅) next to « Voir le classement », custom id
`level:badges:{userId}` (its own verb; `{userId}` is the card's member).

### « Succès » view

Like the Plynling « Personnalité » button: **`DeferAsync(ephemeral: true)`, then an ephemeral
follow-up** for whoever clicked, showing the card member's badges. The `/level` card is not
edited. Rendered as Components V2 (one container, a TextDisplay per section), sent with
`AllowedMentions.None`, built by a `static` Context-free builder so it can be measured.

Four sections, in this order:

1. **Collections** — each set: badge + tier + « complétée N fois », or ??? + the set's name for an
   uncompleted set (the set itself isn't secret).
2. **Paliers** — earned: icon at tier, name, tier, progress « 37 h / 50 h pour Argent » (or
   « Diamant » at the top). Unearned: `???` + hint + progress toward Bronze.
3. **Moments** — earned: icon, name, date (`<t:…:D>`). Unearned: `???` + hint.
4. **Secrets** — found: icon, name, cryptic hint, date. Unfound: `???` + cryptic hint. Header shows
   « X / 13 trouvés ».

Measure the whole message against the V2 text and component caps (root `CLAUDE.md`, *Discord's
hard caps*). If it doesn't fit in one message, page by section with buttons (each with its own
custom-id verb).

### Icons

**Every emoji in this document is a placeholder**, exactly like the collection badges' medals:
real art will replace them later, one PNG per key. Nothing may depend on the placeholder emoji
(no lookups, no parsing).

Same scheme as collection badges: `ItemEmojis.For(key) ?? placeholder`.
- Paliers: `badge.<key>_<tier>` (e.g. `badge.voice_gold`), placeholder = tier medal + the badge's
  emoji.
- Moments and secrets: `badge.<key>`, placeholder = the badge's emoji.
- `ItemEmojis.IsIconKey` must accept these keys so dropping PNGs in `Assets/Icons/` is all the art
  needs. Watch for clashes with collection set keys (`badge.<set>_<tier>`): no member badge key may
  equal a collection set key.

## Docs and shipping

- Version **7.34.0** in `ProjectSYNCS/config.yaml`; `CHANGELOG.md` entry in English: the « Succès »
  button, the paliers and moments, and « some badges are secret » — **no secret triggers**.
- `README.md` and `/help` (`HelpModule`, close to the 6000 cap — measure): mention the « Succès »
  button and the visible badges. Secrets are absent from both.
- `docs/agents/xp.md`: a *Member badges* section (catalog location, storage, awarding rules, the
  « no write per message » de-dup). Add `BadgeAwarder` / `MemberBadgeService` to the root
  `CLAUDE.md` subsystem table row for XP.

## Left to the builder

- File and class names beyond those named here; where the catalog lives (a `Helpers/Xp/MemberBadges`
  in the style of `PlynlingBadges` is the obvious shape).
- Exact layout and wording of the « Succès » view within the rules above.
- Query shapes (respect the SQLite `DateTimeOffset` rule: filter ids/booleans in SQL, dates in
  memory).
