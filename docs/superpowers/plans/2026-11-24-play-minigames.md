# `/plynling play` minigames and `/arcade guess` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace plus ou moins in `/plynling play` with four quick button-only games, and move plus ou moins to `/arcade guess` with 6 tries and a stake.

**Architecture:** Play rules stay pure in `PlynlingGameState` (one class, a `Random` passed in), text in `PlynlingGameUi`, card in `PlynlingPlayCards`, clicks in `PlynlingComponentHandler` through the existing `PlayMoveAsync`. The arcade game follows the blackjack shape: `GuessGame : ArcadeGame`, pure rules in `Helpers/Arcade/NumberGuess`, card in `PartyCards`, clicks in `PartyComponentHandler`, idle watch and settlement in `PartyRunner`.

**Tech Stack:** .NET 10, Discord.Net (Components V2 for play, embeds for arcade), console checks in `tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-24-play-minigames-design.md`

## Global Constraints

- Command/option names English; every user-facing string French (`CLAUDE.md`).
- Play games: buttons only, no modal, no timer. Blurple for every pick; grey for unpicked grid cells and disabled leftovers.
- Every custom-id on one message unique, disabled buttons included.
- V2 play card ≤ 40 components; `AllowedMentions.None`; `MessageFlags.ComponentsV2` on every update.
- Plynling names through `PlynlingCardUi.SafeName`.
- Lines: Plynling text per `docs/plynling-writing-style.md`; SYNCS lines per `docs/syncs-voice.md`; picked via `ResponsePicker` before `string.Format`.
- Version `8.9.0` → **`8.10.0`** (minor: new subcommand), one `CHANGELOG.md` entry dated 2026-11-24. Do not commit (owner commits by hand).

## Review Focus

- Mushroom emojis not loaded (dev before Ready, upload failure): Memory must never be drawn; play falls back to the other five. → Task 3 check.
- A pebble pile under 3: buttons that take more than the pile are disabled, never clickable into a negative pile. → Task 1 check.
- Digging an already-dug cell (double click, stale card): refused as no move, the dig count unchanged. → Task 1 check.
- `/arcade guess` with a stake the player can no longer cover at the end: settles nothing, says so (like blackjack). → Task 4 via `SettleAsync`'s payers.
- A modal submission arriving after the idle watch ended the game: answers « partie terminée », no second settlement. → Task 4 uses `ArcadeTables.End` claim.

---

### Task 1: Play rules — four new games in `PlynlingGameState`

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingGames.cs`
- Create: `tests/ProjectSYNCS.Checks/PlynlingPlayChecks.cs` (section 66)
- Modify: `tests/ProjectSYNCS.Checks/Program.cs` (call it before the final summary)

**Interfaces:**
- Produces:
  - `enum PlynlingGame { HideAndSeek, RockPaperScissors, PebbleBag, Memory, LastPebble, TreasureHunt }` (HigherLower, `GuessHint`, `Guess`, `Secret`, `TriesLeft`, `Guesses`, `GuessMin/Max/Tries` removed).
  - `new PlynlingGameState(PlynlingGame game, Random rng, IReadOnlyList<string>? mushrooms = null)` — Memory requires ≥ `MemoryOptions` mushroom keys, else throws.
  - Pebble bag: `const BagMin = 1, BagMax = 10, BagToWin = 3`; `int Pebble`, `int Streak`, `IReadOnlyList<int> Drawn`; `bool? LastRight`; `bool Bag(bool higher, Random rng)` — draws until ≠ current; right → `Streak++`, `Streak == BagToWin` wins; wrong → lost.
  - Memory: `const MemoryLength = 4, MemoryOptions = 6`; `IReadOnlyList<string> Sequence`, `IReadOnlyList<string> Options` (sequence + 2 decoys, shuffled), `bool Memorised`, `int Progress`, `int? WrongSlot`; `void Memorise()`; `bool Pick(int slot)` — `Options[slot] == Sequence[Progress]` → `Progress++` (wins at `MemoryLength`), else lost with `WrongSlot = slot`. `Pick` before `Memorise` throws.
  - Last pebble: `const NimMin = 12, NimMax = 16, NimMaxTake = 3; const double NimSlip = 0.25`; `int Pile`, `int? LastTake`, `int? PlynlingTake`; `void Take(int n, Random rng)` — `1 ≤ n ≤ min(3, Pile)` else throws; player taking the last wins; else the Plynling takes `Pile % 4` with chance `1 − NimSlip` when non-zero, otherwise random `1..min(3, Pile)`; Plynling taking the last → lost. `static int NimBest(int pile)` (0 when none).
  - Treasure: `const GridSize = 4, Digs = 3`; `int Treasure` (cell 0..15), `IReadOnlyDictionary<int, DigHint> Dug`; `enum DigHint { Found, Burning, Warm, Cold }`; `static DigHint HintFor(int cell, int treasure)` — Manhattan 0/1/2/3+; `bool Dig(int cell)` — a dug cell returns false and changes nothing; three misses lose.

- [ ] **Step 1: Write `PlynlingPlayChecks.Run(Action<string> fail)` with these checks**

```csharp
// Bag: seeded games; a wrong call ends Lost, three right Win; ties never count; Drawn never holds two equal neighbours.
// Bag simulation: careful policy (higher when Pebble <= 5), 20 000 games, win rate in [0.38, 0.48].
// Memory: Sequence has 4 distinct keys, Options 6 distinct keys containing all of Sequence; picking in order wins;
//   a decoy loses with WrongSlot set; Pick before Memorise throws; constructor with 5 keys throws.
// Nim: Pile starts in [12, 16]; Take(4) and Take(Pile + 1) throw; NimBest(13)==1, NimBest(12)==0, NimBest(7)==3;
//   random policy 20 000 games win rate in [0.11, 0.21]; perfect policy (NimBest, else 1) in [0.72, 0.82].
// Treasure: HintFor(0,0)==Found, HintFor(0,1)==Burning, HintFor(0,5)==Warm, HintFor(0,15)==Cold;
//   Dig on a dug cell returns false and leaves Dug.Count; three misses → Lost; a hit → Won.
```

- [ ] **Step 2: Run `dotnet run --project tests/ProjectSYNCS.Checks` — expect a compile failure (members missing).**

- [ ] **Step 3: Implement the members above in `PlynlingGames.cs`.** Every move calls the existing `Expect(game)`. Memory draws the sequence and decoys with `rng` from the passed keys, then shuffles `Options` with `rng`.

- [ ] **Step 4: Run the checks — section 66 passes; the build may still fail elsewhere on removed `Guess` members until Task 2/3 (do Tasks 1–3 before the next full run if so).**

### Task 2: Play text — `PlynlingGameUi`

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingGameUi.cs`
- Modify: `tests/ProjectSYNCS.Checks/PlynlingPlayChecks.cs`

**Interfaces:**
- Consumes: Task 1 state.
- Produces: `PlynlingGameUi.Text(state, name, gender)` covering all six games; `static string MushroomLabel(string key)` (French name, ≤ 80 chars), `static string MushroomEmoji(string key)` (markup via `ItemCatalog.ByKey(key)!.Emoji`); `static string HintEmoji(DigHint h)` (🔥 ♨️ ❄️, ✨ for Found).

- [ ] **Step 1: Add checks:** for every game, play seeded games to Won and Lost and assert `Text` is non-empty, starts with `## `, has no `{`/`}`, and stays ≤ 1500 chars; Memory text before `Memorise` lists all 4 sequence names, after it lists none of them.
- [ ] **Step 2: Run — fails.**
- [ ] **Step 3: Implement.** Headings: « 🪨 Le sac de cailloux », « 🍄 Mémoire », « 🪨 Le dernier caillou », « 🌰 Chasse au trésor ». Remove `GuessText`, `History`, `Hint`. Plynling wording per the style guide; gendered agreement via `g.Agree`.
- [ ] **Step 4: Run — passes.**

### Task 3: Play card, clicks and the draw

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingPlayCards.cs` (`BuildGame`)
- Modify: `ProjectSYNCS/Interactions/Components/PlynlingComponentHandler.cs` (play section, ~411–510)
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingPlayService.cs` (`Start`)
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.cs:164`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingText.cs` (drop `GuessRange`)
- Modify: `tests/ProjectSYNCS.Checks/PlynlingPlayChecks.cs`

**Interfaces:**
- Consumes: Tasks 1–2.
- Produces:
  - `PlynlingPlayService.Start(ulong guildId, ulong ownerId, int plynlingId, DateTimeOffset now, Random rng, IReadOnlyList<string> mushrooms)` — draws uniformly among the six, Memory only when `mushrooms.Count >= MemoryOptions`.
  - `static IReadOnlyList<string> PlynlingGameUi.LoadedMushrooms()` — keys of `ItemCatalog.InSet(ItemCatalog.MushroomSet)` whose `ItemEmojis.For(ItemCatalog.PictureKey(key))` is non-null.
  - Custom-ids: `plyn:bag:{id}:{up|down}`, `plyn:memogo:{id}`, `plyn:memo:{id}:{slot}`, `plyn:nim:{id}:{n}`, `plyn:dig:{id}:{cell}`. All `ignoreGroupNames: true`, all through `PlayMoveAsync`. Bad parses answer `PlynlingText.Unknown`.
  - `GuessModal` stays (moves to arcade in Task 4); `plyn:guess`/`plyn:guessm` handlers removed; the `SocketModal` branch of `PlayMoveAsync` removed.

- [ ] **Step 1: Add checks:** for each game, build the card for a seeded game at start, mid-game and over (fake `Plynling` as other checks do) — `Build()` succeeds, all custom-ids distinct, no buttons once over; Memory before `Memorise` has exactly one button, after it six (two rows of three, each with emoji and name); Nim at Pile 2 has « 3 » disabled; Treasure has 16 buttons in four rows, dug ones disabled with their hint emoji; `Start` with an empty mushroom list over 600 seeds never yields Memory and yields each of the other five.
- [ ] **Step 2: Run — fails.**
- [ ] **Step 3: Implement.** Bag: ⬆️ « Plus haut » / ⬇️ « Plus bas », blurple. Memory: « J'ai retenu ! » blurple; picks blurple, already-picked ones disabled grey. Nim: « 1 » « 2 » « 3 » blurple. Treasure: undug cells grey with label « · », dug cells disabled with the hint emoji. `PlynlingModule.PlayAsync` passes `PlynlingGameUi.LoadedMushrooms()`.
- [ ] **Step 4: Run `dotnet build` (warnings as errors as configured) and the checks — pass.**

### Task 4: `/arcade guess`

**Files:**
- Create: `ProjectSYNCS/Helpers/Arcade/NumberGuess.cs`
- Modify: `ProjectSYNCS/Services/Arcade/ArcadeGames.cs` (`GuessGame`)
- Modify: `ProjectSYNCS/Commands/Arcade/ArcadeModule.Party.cs` (new region after Temperature)
- Modify: `ProjectSYNCS/Commands/Arcade/PartyCards.cs` (`GuessCard`, `GuessButtons`)
- Modify: `ProjectSYNCS/Interactions/Components/PartyComponentHandler.cs`
- Modify: `ProjectSYNCS/Services/Arcade/PartyRunner.cs` (`WatchGuess`, `FinishGuessAsync`)
- Modify: `ProjectSYNCS/Interactions/Modals/GuessModal.cs` (comment: now `/arcade guess`)
- Modify: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Arcade.MoreGames.cs` (`GuessIntroLines` `{0}` = player mention, `GuessWinLines`, `GuessLoseLines`; 6+ each, her voice)
- Modify: `tests/ProjectSYNCS.Checks/PlynlingPlayChecks.cs` (or a new `ArcadeGuessChecks` section 67)

**Interfaces:**
- Produces:
  - `static class NumberGuess { const int Min = 1, Max = 100, Tries = 6, MaxStake = 500; const double Payout = 1.4; static readonly TimeSpan IdleLimit = 5 min; enum Hint { Higher, Lower, Correct }; static Hint Judge(int guess, int secret); static long Net(bool won, long stake) /* won: floor(stake*0.4), lost: -stake */ }`
  - `GuessGame : ArcadeGame { required ulong PlayerId; long Stake; int Secret; List<(int Guess, NumberGuess.Hint Hint)> Guesses; DateTimeOffset LastActionAt; string Line; int TriesLeft => Tries - Guesses.Count; bool Won }`
  - `/arcade guess [stake: 0..500, default 0]` — balance check like blackjack, `TryOpen` conflict one `GuessGame` per player, card + « 🔢 Deviner » (`num:try:{id}`, blurple) opens `GuessModal` with id `num:m:{id}`.
  - Modal handler: not the player → « Ce n'est pas ta partie. Lance la tienne avec `/arcade guess`. »; out of range → the old range message; apply under `_tables.With`; Correct or no tries left → `_tables.End(id)` claim, defer, `FinishGuessAsync`, edit. Otherwise `UpdateAsync` the card.
  - `FinishGuessAsync(GuessGame g)` settles `(PlayerId, Stake)` with `NumberGuess.Net`, badge `arcade` on a settled win, money line like blackjack's, card with the secret revealed and no buttons.

- [ ] **Step 1: Add checks:** `Judge` three cases; `Net(true,100)==40`, `Net(true,1)==0`, `Net(false,50)==-50`, `Net(true,0)==0`; a binary-search player over all 100 secrets with 6 tries wins exactly 63 and returns ≤ 0.9 of the stake on average; `GuessCard`/`GuessButtons` build for a fresh, a mid and a finished game, and the finished one has no components.
- [ ] **Step 2: Run — fails.**
- [ ] **Step 3: Implement**, copying the blackjack flow (`ArcadeModule.Party.cs:121–160`, `PartyRunner.cs:170–225`, `PartyComponentHandler.cs:162–230`) and the temperature modal handler for `RespondWithModalAsync`/`[ModalInteraction]`.
- [ ] **Step 4: Build and run the checks — pass.** Count `/arcade` subcommands: 12.

### Task 5: Docs and version

**Files:** `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`, `ProjectSYNCS/Commands/Core/HelpModule.cs`, `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Help.cs`, `README.md`, `docs/agents/plynling.md` (`/plynling play`), `docs/agents/arcade.md` (the newer games list + a Guess note).

- [ ] **Step 1:** Version `8.10.0`; changelog entry: four new `/plynling play` games, plus ou moins moved to `/arcade guess` (6 tries, stake, ×1.4).
- [ ] **Step 2:** `/help` and `/plynling help` mention the games and `/arcade guess`; re-run the checks (the 6000-char help caps are checked there).
- [ ] **Step 3:** Final `dotnet build` in `ProjectSYNCS` and `dotnet run --project tests/ProjectSYNCS.Checks` → `ALL CHECKS PASSED`. No commit.
