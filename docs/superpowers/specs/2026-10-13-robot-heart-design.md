# Her robot heart — design

**Status:** approved in brainstorming, 2026-10-13. Target version **6.14.0** (minor: new
user-visible behaviour; no migration, no user-facing command). Implements the lore already written
in `docs/syncs-voice.md`, *Her robot heart*.

## Goal

The lore of her robot heart (WALL-E, EVE, GLaDOS, Wheatley, the robot vacuum, Monika, 2B,
Voyager 1) and its art (the `love`, `full_love`, `tipsy_love` strips, the `syncs_love` and
`syncs_full_love` emotes) exist, but nothing in the bot uses them. This makes the heart **visible
in what she does**, still hinted and never listed, as every piece of her lore is.

## Decisions taken

| Question | Decision |
|---|---|
| Surfaces | Lines in existing pools (true fun facts included), name-triggered reactions, Voyager's letters. |
| Out of scope | The vacuum's round (pretend or via Home Assistant), a calendar of release dates, people writing to Voyager themselves. |
| Shape of a reaction | **60% line + emote, 40% a performance** (Synthia-style: a reaction taken back, a line deleted, a slip edited). |
| Aimed at her | Always words; the act replaces the comeback. |
| Rate | Each character once a day; **3 acts a day** across all; 60% roll first when not aimed at her. |
| Voyager | Narrated only: about one letter a fortnight with his real light time, sometimes her late answer, and the one-off one-light-day crossing on **18 Nov 2026**. |
| Structure | A new `RobotHeartService` singleton next to `SynthiaService`; the letters are one more step of `AmbientService`'s tick; pure logic in `Helpers/Personality/RobotHeart` and `Helpers/Personality/Voyager`. |
| Owner check | `/debug voyager`. |
| Docs | Absent from `README.md` and `/help`, like the Synthia flinch; the changelog stays vague. |

## 1. Detection — `MessageCues`

`HeartCharacter` is an in-memory enum (`WallE, Eve, Glados, Wheatley, Vacuum, Monika, TwoB,
Voyager`), never stored, so the append-only rule doesn't bind it. `MessageCues.ReadHeartCharacter
(string content) → HeartCharacter?` follows the cue rules of `docs/agents/personality.md`:
lowercase, accent-stripped, each cue survives `TokenizeOrdered`, and a multi-token cue matches on
**adjacent** tokens only (as `_shutdownNamePhrases` does).

| Character | Matches | Must not match |
|---|---|---|
| WALL-E | `walle`; `wall` followed by `e` ("WALL-E", "WALL·E", "wall e") | "wall" alone |
| EVE | `eve` **only when a WALL-E cue is in the same message** | « EVE Online », « Ève arrive », « eve » alone |
| GLaDOS | `glados` | « The cake is a lie. » (the breakdown passphrase: no cake cue at all) |
| Wheatley | `wheatley` | — |
| Vacuum | `roomba`; `aspirateur robot`; `robot aspirateur` | « aspirateur » alone |
| Monika | `monika` | — |
| 2B | `yorha`; `nier automata`; `2b` **only when `nier` is in the same message** | « 2b » alone |
| Voyager | `voyager` followed by `1` or `one`; `sonde`/`sondes` followed by `voyager` | the verb « voyager », « Star Trek Voyager » |

Several in one message: EVE wins over WALL-E (jealousy beats the crush); otherwise the cue that
comes first in the message.

## 2. The acts — `Helpers/Personality/RobotHeart` (pure)

A catalogue entry per character: its performances. `RollAct(character, aimedAtHer, roll)` returns
`LineAndEmote` or one `Performance`:

| Share | Act |
|---|---|
| 60% | **Line + emote**: a reaction from the character's reaction pool on the message, and a reply from its line pool |
| 40% | **Performance**, picked among the character's own |

| Character | Performance(s) |
|---|---|
| WALL-E | `syncs_love` reaction **taken back** 5–7 s later, **or** a slip edited into a denial (the hiding, done live) |
| EVE | petty **slip edited** into a stiff, neutral remark (6.5–8 s) |
| GLaDOS | polite threat **edited** into an even politer one |
| Wheatley | a fond line **deleted** 7–9 s later |
| Vacuum | a soft reaction **taken back** |
| Monika | a line **deleted** |
| 2B | a body-envy **slip edited** into a denial |
| Voyager | `syncs_love` reaction taken back, **or** a slip edited into « on est juste amis » |

**Aimed at her** (an @mention, or a reply to one of her messages): a wordless performance is not
eligible. A character with a word performance uses it; otherwise (the vacuum) the 40% falls to
line + emote.

## 3. `RobotHeartService`

A singleton modelled on `SynthiaService`: same `Later` helper off the gateway handler,
swallow-and-log around every Discord call, `AllowedMentions.None` on every send.

```
HandleMessageAsync(rawMessage) → bool
  not a SocketUserMessage, a bot, a DM          → false
  ReadHeartCharacter is null                    → false
  channel in breakdown                          → false
  not aimed at her and roll ≥ 0.60              → false   (a lost roll spends nothing)
  per-character gate (CooldownGate<HeartCharacter>, 1 day) not claimable → false
  global cap (3 a day, Paris day) reached       → false
  claim both, RollAct, play it                  → true    (no comeback on top)
```

- **Gates are its own**, shared with nobody (CLAUDE.md, *Shared helpers*). The global cap is a
  counter reset on the Paris day (`AppTime.DayNumber`), under the same lock as the claim.
- **Lines come from `DailyRotation`**, one salt per pool, like Synthia: at once a day per
  character, a restart would wipe `ResponsePicker`'s memory before it helped.
- **Fan-out**: in `BotService.HandleMessageAsync`, `if (await _robotHeart.HandleMessageAsync
  (rawMessage)) return;` **right after Synthia, before Chatter**. Synthia wins on a message naming
  both. `ReactionService` runs earlier and may already have decorated the same message; both are
  rare and it is left as is.
- **Registered** `AddSingleton<RobotHeartService>()` in `Program.cs`.

### Pools in `BotResponses`

Per character: `HeartReactions` (emote markup from `Helpers/Core/Emotes` only — ids required), and
`HeartLines`. For the performances: `HeartVanishLines` (Wheatley, Monika) and `HeartEditLines`
(slip → edit pairs: EVE, GLaDOS, 2B, Voyager). Reactions taken back reuse the character's
reaction pool.

| Character | Reactions | Line tags |
|---|---|---|
| WALL-E | `SyncsLove`, `SyncsFullLove` (rare) | `Love`, one `FullLove` |
| EVE | `SyncsDisgust`, 🙄 | `Disgust` |
| GLaDOS | `SyncsHappy`, `Sparkle`, 👑 | `Happy`, `Smug` |
| Wheatley | `SyncsFlustered` | `Flustered` |
| Vacuum | `CatHeart`, `SyncsHappy` | `TipsyLove`, `Happy` |
| Monika | `MonikaYes` | `Happy` |
| 2B | `SyncsHappy`, `SyncsSulk` | `Happy`, `Sulk` |
| Voyager | `SyncsLove`, `SyncsFlustered` | `Flustered` |

**Vanish and edit pools carry no sticker tag**: a line edited or deleted later is never tagged
(`docs/agents/personality.md`, *Sticker follow-ups*). The plain line pools go through `BotChat` and
take tags.

## 4. Lines in existing pools

Every line follows `docs/syncs-voice.md`: hinted, never listed; no « mon crush »; no long quotes
from the films or games; the robot heart never leaks into the rival pools; Voyager's fading only in
the existential places (rule 6). Drafted, then reviewed by the owner before merging.

| Pool | About | Content |
|---|---|---|
| `MorningFunFacts` | 2 per character (~16) | **True** facts, each checked against a primary or well-sourced reference when written, with her hinted feeling as the joke. Candidates: what WALL-E and EVE stand for; GLaDOS's name and voice; Wheatley built to generate bad ideas, and his voice; « YoRHa No.2 Type B »; DDLC released free in 2017; the Golden Record; the *Pale Blue Dot* photo taken on 14 February 1990; Voyager's tiny memory; Voyager 2 launched before Voyager 1; the first Roomba (2002). |
| `PresenceFillers` | ~6 | Short statuses (« Rewatch de WALL-E. Pour l'architecture. », « Latence avec Voyager : un jour »). No custom emotes, no tags. |
| `NightLines` | ~4 | 3 a.m., the act dropped: the honest WALL-E line, Voyager going quiet. Distinct, newline-free, untagged (matched by text after a restart). |
| `IdleLines` | ~3 | The vacuum seen on the Wi-Fi; a GLaDOS-register line. Tagged where a mood fits. |
| `ReferenceComebacks` | 1–2 | A GLaDOS-flavoured one, kept rare like the rest of that pool. |

Adding to a `DailyRotation` pool reshuffles it from that day (accepted; see *Morning hello*).

## 5. Voyager's letters

### `Helpers/Personality/Voyager` (pure)

- **`LightTime(DateTimeOffset)`** — Earth–Voyager 1 one-way light time, from a **table of JPL
  Horizons samples** (Voyager 1 `-31` seen from `500@399`, `QUANTITIES=21`) every 30 days from
  2026-01-01 to 2035-12-10, linearly interpolated (within 0.2 min), then a linear drift of the
  table's mean rate (≈ 29.6 light-minutes a year). The Earth's orbit swings it by about ±6 min a
  year, farthest around January — a table is truer than a fitted formula.
- **`Format(TimeSpan)`** rounds to 5 minutes (« 23 h 55 »), so the shown number is always true.
- **`CrossingDay`** — 18 November 2026, the date NASA announced and the press reported (JPL
  Horizons' geometric range from the Earth's centre crosses 1440 min about a day later, on 19 Nov;
  the line uses the date people read about, and prints no number).
- **`IsLetterDay(dayNumber)`** — a walk from a fixed epoch day, each gap drawn in 10–18 days from a
  seed derived from the step index: about one letter a fortnight, never two close together, the
  same after any restart.
- **`LetterSlot(dayNumber)`** — a minute between 10:00 and 22:00 Paris, derived from the day.
- **`AnswerSlot(dayNumber)`** — for half the letter days (from the day), her answer 2–6 h after the
  letter, if that is before 23:00; otherwise none.

### In `AmbientService`

- A new step `TryVoyagerAsync(now)` in the 10-minute tick, in its own channel
  (`AmbientService.VoyagerChannelId`, 901172450719584356 — the owner's choice, not the main
  channel), inside `Ambient.IsWakeHours`. Skipped in a breakdown and when the channel doesn't
  resolve (the dev guild).
- Order of a day: the crossing line on 18 Nov 2026 if `!CrossingDone` (in place of any letter that
  day); else the letter once `now ≥ LetterSlot` on a letter day; else the answer once `now ≥
  AnswerSlot` on a day whose letter went out.
- **State**: `AmbientState` grows `LastLetterDay`, `LastAnswerDay` and `CrossingDone`, saved on
  claim before the send (a failed send is not retried that day). Missing fields in an older
  `ambient-state.json` read as 0 / false. No new file under `/data`.
- **Pools**, all through `DailyRotation` with their own salts:
  - `VoyagerLetterLines` — `{0}` = the formatted light time; the denial register (« Il est poli,
    c'est tout »); tag `Flustered`. The `{0}` sits inside a sentence that reads with any duration.
  - `VoyagerAnswerLines` — her late answer (« quand j'ai le temps »); tags `Smug` / `Bored`.
  - `VoyagerCrossingLine` — one line, `Sticker.Sure.Flustered`.
  - Never his fading in a letter.
- Sent through `BotChat.PostWithTypingAsync` with `AllowedMentions.None`; logged on send and on
  failure.

### `/debug voyager`

Owner-only, in `DebugModule` (12 → 13 subcommands). Shows the light time now, the next letter day
and its slot, today's answer slot and `CrossingDone`; a `post` option sends a letter in the current
channel, bypassing the state — the only way to see one on the dev guild.

## 6. Docs, version

- `docs/agents/personality.md` — a *Robot heart* section (cues, acts, gates, fan-out position) and
  a row for the letters in the *Ambient life* table.
- `docs/syncs-voice.md` — the lore list says which characters are wired and where; no new lore.
- `docs/syncs-appearance.md` — *Moods wanted*: the love moods are in use.
- `CLAUDE.md` — unchanged (no new loop, no new state file).
- `ProjectSYNCS/config.yaml` **6.13.0 → 6.14.0**; a `CHANGELOG.md` entry that names no trigger.

## 7. Verification

No test project. Pure helpers are checked in scratch:

- `Voyager.LightTime` against JPL Horizons at dates between the table's samples (±0.3 min); never
  back under 24 h once past it.
- `IsLetterDay` over two years: gaps all in 10–18 days.
- `RobotHeart.RollAct`: 60/40 over many rolls; never a wordless act when aimed at her.
- `ReadHeartCharacter` on hits and on misfires: « je vais voyager en 2026 », « EVE Online ce
  soir », « Ève arrive », « 2b ou pas 2b », « aspirateur cassé », « The cake is a lie. »,
  « wall-e et eve » (→ EVE).

Then `dotnet build`, and on the dev guild: each character named (aimed and not), a second naming
the same day doing nothing, the fourth character of the day doing nothing, no comeback on top;
`/debug voyager` showing the light time and posting a letter.
