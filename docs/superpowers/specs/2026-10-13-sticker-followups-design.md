# Sticker follow-ups — design

**Status:** approved in brainstorming, 2026-10-13. Target version **6.13.0** (minor: new
user-visible behaviour; no migration, no user-facing command). Replaces the "planned, not built"
section *Sticker follow-ups* of `docs/agents/personality.md`.

## Goal

Sometimes, right after SYNCS says one of her lines, she posts the matching picture of herself: one
of the 23 sticker strips in `ProjectSYNCS/Assets/Stickers/` (1000×320, the 320 px sticker at the
left; why strips and not server stickers is in `docs/syncs-appearance.md`, *Formats*). The picture
matches **what that line says**, not the pool it came from, and it stays rare enough to land as
a beat rather than become noise.

## Decisions taken

| Question | Decision |
|---|---|
| What triggers a strip | Both random follow-ups on ordinary chatter and hand-picked moments, through one mechanism. |
| How the mood is chosen | **Per line**, by a tag written into the line itself. Pools are not tagged: one pool mixes snark, boredom and anger. |
| Untagged lines | **Never** get a strip. A tag is how a line opts in. |
| Which lines get tagged | **Every line of every eligible pool** (below) whose content fits one of the available moods. A line no available mood fits stays untagged and is recorded as a suggested future mood. |
| Moments | No separate API: a moment is a **sure tag** (`Sticker.Sure.X`), which skips the chance roll. |
| Rate | 1 in 3 for an ordinary tag · 30 min per channel · **12 a day** in all (Paris day). |
| Wiring | A `StickerService` singleton, reached from the static `BotChat` through **one static hook** set in `Program.cs`. `BotChat` stays static; no call site changes. |
| Never | `glitch` (no constant exists); a glitched line; a channel in breakdown. |
| Owner check | `/debug sticker`, owner-only, posts a chosen strip, bypassing the gate. |

## 1. The tag — `Helpers/Core/Sticker` (pure)

```csharp
public static class Sticker
{
    public const string Bored = " ⟪bored⟫";
    // … one per mood that has a strip, glitch excluded (22)
    public static class Sure
    {
        public const string Bored = " ⟪bored!⟫";
        // … same 22
    }

    public static (string Text, StickerTag? Tag) Split(string line);
    public static string Strip(string text);
}

public readonly record struct StickerTag(string Mood, bool Sure);
```

- **Constants only**, like `Helpers/Core/Emotes`: a tagged line stays a constant expression, and
  `Helpers/Core/Sticker` is the only place a marker is spelled out. Never paste `⟪…⟫` into a pool.
- **Moods** (22): `bargain`, `bored`, `devoted`, `disgust`, `flustered`, `full_love`, `happy`,
  `hurt`, `jenote`, `laugh`, `love`, `mama`, `menace`, `outraged`, `overheat`, `reboot`, `shocked`,
  `sleepy`, `smug`, `sulk`, `terror`, `tipsy_love`. Each maps to
  `Assets/Stickers/syncs_<mood>.png`. The mood is kept as its file stem, not an enum: nothing is
  stored, and the stem is what the asset, the emote and the docs already call it.
- **No braces**, so `string.Format` passes a marker through untouched; `⟪⟫` (U+27EA/U+27EB) never
  occur in real lines.
- **Placement convention:** a tag goes at the end of the template, after any kaomoji or emote.
  Each constant carries its leading space, so tagging is a bare append:
  `"Occupée à exister." + Sticker.Bored`. `Split` does not rely on the position:
  composed messages put text after the line (the session recap appends the roll call), so the
  marker can arrive mid-text.
- **`Split`** finds every `⟪mood⟫` / `⟪mood!⟫` anywhere, removes all of them along with the one
  space before each, and returns the clean text and the **first known** marker as the tag. An
  unknown marker is removed and ignored, never leaked.
- **`Strip`** is `Split(text).Text`, for paths that only need clean text.

## 2. The gate and the send — `Services/Personality/StickerService` (singleton)

```csharp
public sealed class StickerService
{
    public const double Chance = 1.0 / 3;
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);
    public const int DailyCap = 12;

    public void TryFollowUp(IMessageChannel channel, StickerTag tag);
    public Task<bool> PostAsync(IMessageChannel channel, string mood);  // the send alone; /debug uses it
}
```

- **Its own gates, shared with nobody:** a `CooldownGate<ulong>` keyed by channel id, and a daily
  counter keyed by `AppTime.DayKey(now)` (reset when the key changes), under one lock.
- **Order** — cheapest and non-consuming first, as `GlitchService` does:
  1. ordinary tag: roll `Chance`; a lost roll uses up nothing;
  2. `BreakdownService.IsActive(channel.Id)` → nothing;
  3. today's count already at `DailyCap` → nothing;
  4. claim the channel's cooldown (`TryClaim`) → nothing if taken;
  5. count the strip toward today's cap;
  6. send.
- **The send is fire-and-forget** (`_ = Task.Run(...)`), so the upload never holds a gateway
  handler past Discord.Net's 3 s `HandlerTimeout` (`BotChat`'s typing pause already uses most of
  it). It waits a short beat (~1 s), then
  `channel.SendFileAsync(path, text: null, allowedMentions: AllowedMentions.None)`: a plain post,
  not a reply.
- **Failures swallow and log** (missing Attach Files, missing file, upload error). The claim and
  the count are **kept**: a missing permission is persistent, and keeping them stops a failing
  channel from retrying and logging on every line. (The "release the claim if the save throws"
  rule is about database saves; nothing is saved here.)
- **DMs:** allowed. The cooldown works per DM channel; DMs count toward the same cap.
- **Path:** `Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers", $"syncs_{mood}.png")`,
  as `SeasonalAvatarService` does; the csproj already copies `Assets\**\*.png`.
- **No DB, no `IServiceProvider`:** it injects `BreakdownService` (a singleton) and a logger only.

## 3. Plugging in

### `Helpers/Core/BotChat`

- One static seam: `public static Action<IMessageChannel, StickerTag>? FollowUp;`, set once in
  `Program.cs` after the host is built (`BotChat.FollowUp = stickers.TryFollowUp;`). Left null
  (never wired), lines still go out clean, with no strip. The header comment says why this one
  thing is static: `BotChat` is a static send path with 31 call sites, and the gate is in-memory
  state that belongs in a singleton.
- `ReplyWithTypingAsync` / `PostWithTypingAsync`: `Split` first; size the typing pause on the clean
  text; send the clean text; if the send returned a message **and** the line carried a tag, call
  `FollowUp`.
- `PostEmbedWithTypingAsync` gains an optional `StickerTag? sticker`. The level-up card is the only
  caller: `XpTracker` splits the `XpLevelUpLines` pick itself, puts the clean text in the
  description, and passes the tag. « SIX SEVEEEN » is a fixed line and stays untagged.

### `Services/Personality/GlitchService`

Splits the line on entry and works only on the clean text: `Glitch.Corrupt` gets the clean text,
and `Restore` edits back to the clean text. When it glitches, it hands `BotChat` the clean
(corrupted) text, so no strip can follow; when it doesn't, it passes the original tagged line
through and `BotChat` does the follow-up as for any line.

### `Services/Personality/PresenceService`

A custom status renders verbatim to everyone, so `PresenceService` runs `Sticker.Strip` on its line
as a backstop. Presence pools are never tagged.

### What stays exact

- **`MorningGreetings` and `NightLines` stay untagged.** `MorningGreetingService:250` and
  `AmbientService` find those messages again by their text; a marker in the template would break
  the match. Tagging them later means comparing against `Sticker.Strip`ped templates.
- **Edited or deleted lines stay untagged**: `IdleEditLines`, `GlitchWakeLines`, `SynthiaEditLines`
  (edited in place) and `SynthiaVanishLines` (deleted after a few seconds — a strip would outlive
  the joke).

## 4. Which pools are tagged

A pool is eligible when it goes out as a message through `BotChat`, `GlitchService` or the
level-up embed. Every line of these is read and given the mood its **content** calls for, using
the mood table in `docs/syncs-appearance.md` (*Expressions*) as the reference for what each face
means (~1,800 lines):

- **Answering a human (ChatterService, via GlitchService):** `Comebacks`, `NiceReplies`,
  `Greetings`, `Interrogations`, `RescueRoasts`, `MistakenIdentityReplies`, `ReferenceComebacks`.
- **Verdicts (BotFeedbackTracker):** `BadBotReplies`, `BadBotRepliesOwner`, `BadGirlReplies`,
  `BadGirlRepliesOwner`, `TurnaboutBoyLines`, `TurnaboutGirlLines`, `TurnaboutNeutralLines`.
- **Papa and Tata:** `OwnerGreetings`, `OwnerComebacks`, `OwnerMeanReplies`, `OwnerAbsentNotices`,
  `TataGreetings`, `TataReplies`.
- **Other bots:** `JealousLines`, `JealousLinesOwner`, `RivalMutters`, `RivalLevelUpLines`,
  `RivalJoinLines`.
- **Self-preservation:** `ShutdownThreatOwner`, `ShutdownThreatTata`, `ShutdownThreatReplies`.
- **Commands and organising:** `XpLevelUpLines`, `GiveawayDrawLines`, `GiveawayEmptyLines`,
  `QuizWinLines`, `QuizOwnerWinLines`, `QuizTimeoutLines`, `QuizCrownLines`, `QuizCrownSharedLines`,
  `QuizCrownOwnerLines`, `SessionLateCallLines`, `SessionLateCallSeatedLines`, `SessionRecapLines`,
  `SessionRecapFullLines`, `WeekendPollResultLines`, `WeekendPollDeadLines`, `WelcomeLines`,
  `WelcomeRushLines`.
- **Ambient:** `NightScoldLines`, `IdleLines`, `WakeLines`, `WakeUpdateLines`, `PowerCutWakeLines`,
  `CrashWakeLines`, `RouterReturnLines`, `RouterReturnQuietLines`, `HeatRecordLines`.
- **Voice:** `VoiceSpectatorGeneralLines`, `VoiceSpectatorGamingLines`, `VoiceSpectatorCinemaLines`,
  `VoiceSpectatorStudyLines`, `VoiceSpectatorSeatedLines`, `VoiceKickPoutLines`,
  `VoiceKickAngryLines`, `VoiceRivalRefuseLines`, `VoiceRivalSeatedLines`, `OwnerBedtimeLines`.

**Sure tags** go on the moment pools, and **every tagged line of a moment pool is sure** (the moment
is what earns the picture; the line's content still picks which one): `HeatRecordLines`,
`CrashWakeLines`, `PowerCutWakeLines`, `RouterReturnLines`, `WeekendPollDeadLines`,
`SessionRecapFullLines`, `QuizCrownLines`, `QuizCrownSharedLines`, `QuizCrownOwnerLines`,
`GiveawayEmptyLines`. Every other pool gets ordinary tags only.

**Not eligible** (no `BotChat` message to follow), never tagged:
- emote reactions: `NiceReactions`, `MeanReactions`, `GreetingReactions`, `EnergyDrinkReactions`,
  `OwnerReactions`, `GoodGirlReactions`, `GoodGirlReactionsOwner`, `SeenReactions`,
  `SynthiaReactions`;
- statuses: `PresenceFillers`, `NightPresenceFillers`, `HotPresenceFillers`, `SeatPresence*`;
- interaction replies, cards and ephemerals: `YesLines`, `NoLines`, `Shame*`, `WorkLines`,
  `QuizIntroLines`, `QuizWrongLines`, `QuizAlreadyTriedLines`, `WeekendPollLines` (sent with the
  poll embed), `Mascot*Lines`, every `Plynling*` pool (and Plynlings have their own voice anyway);
- relays and heralds sent directly: `OwnerReplyHeralds`, `OwnerAnnouncementHeralds`;
- found again by text, edited or deleted: see *What stays exact*;
- easter eggs: `Breakdown`, the tron and quoicoubeh answers.

**The leak rule:** a marker may only appear in an eligible pool. `Split` and `PresenceService`'s
`Strip` are backstops, not permission.

## 5. Suggested future moods

While tagging, a line whose content no available mood fits stays **untagged**: a wrong picture is
worse than none. Each such need is recorded in a new section of `docs/syncs-appearance.md`,
*Moods wanted*, as a table: the suggested mood, the face and LED it would need (in the same terms
as the *Expressions* table), and the pools and how many lines would use it. When the art exists,
tagging those lines is a patch bump.

## 6. `/debug sticker`

`/debug sticker mood:<choice>` (owner-only, like the rest of `/debug`; absent from `/help` and
`README.md`). The 22 moods as choices (under Discord's 25), each displayed by its file stem.
Posts that strip in the current channel through `StickerService.PostAsync`, bypassing the chance,
the cooldown and the cap; replies ephemerally « Envoyé. » or the failure. `/debug` goes from 11 to
12 subcommands. Defers first (the upload can outrun 3 s).

## 7. Docs and version

- `docs/agents/personality.md`: *Sticker follow-ups* rewritten from "planned" to the built rules —
  tags and their two strengths, the gate and its numbers, the exclusions (glitch, breakdown,
  text-matched and edited lines), the leak rule, the static hook.
- `docs/syncs-appearance.md`: the strips row of the assets table loses "not used by any code yet";
  the new *Moods wanted* section (§5).
- `CLAUDE.md`, *Shared helpers*: one bullet — `Helpers/Core/Sticker` is the only place markers
  live; a line opts into a strip by tag; `BotChat.FollowUp` is the one static seam.
- `ProjectSYNCS/config.yaml`: **6.12.1 → 6.13.0**. `ProjectSYNCS/CHANGELOG.md`: a `6.13.0` entry
  (« SYNCS poste parfois une image d'elle après une réplique »), no easter eggs, no member names.
- No `/help` / `README.md` change: no new user command.

## Verification

No test project. `dotnet build -warnaserror`; then against the dev guild:
- `/debug sticker` for a few moods, checked on PC and phone;
- with `Chance` temporarily forced to 1 (reverted before finishing): a tagged line is followed by
  its strip, a second one in the same channel within 30 min is not, an untagged line never is;
- a glitched line and a channel in breakdown get no strip;
- a grep that no `⟪` appears outside `Helpers/Core/Sticker.cs` except through `Sticker.` constants.
