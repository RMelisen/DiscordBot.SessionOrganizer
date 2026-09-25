# Plynlings: play and visit — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/plynling play` (three random mini-games, own Plynling, hourly) and `/plynling visit user:` (an invitation the other owner accepts), recording play/visit counts for the future achievements.

**Architecture:** Pure game rules in `Helpers/PlynlingGames.cs` (randomness injected), live games in a singleton `PlynlingPlayService`, rewards through `PlynlingLife` + `PlynlingService` in single saves, static Components V2 builders in `Commands/PlynlingPlayCards.cs`, handlers in `PlynlingComponentHandler`.

**Tech Stack:** .NET 10, Discord.Net 3.20 (Components V2), EF Core SQLite.

Spec: `docs/superpowers/specs/2026-09-25-plynling-play-visit-design.md`.

## Global Constraints

- Cache-cache: 3 rocks, **2 rounds**, a find ends the game (won). PFC: **first to 2**, ties replayed. Plus ou moins: **1–100, 6 tries**.
- Rewards at game end: **+15 %** happiness, **+10 %** more on a win, **5–10 cailloux** to the player on a win. Play: **own Plynling only, once per hour per Plynling**, the hour claimed at start.
- Visit: **+20 %** happiness to both, **no cailloux**, invitation **expires after 1 h**, **once per Paris day per pair** (in memory).
- Refused while asleep, frozen or dead. Secrets never in custom-ids. Every V2 send/update re-asserts the flag; every send passes `AllowedMentions.None`, except the knock, which pings the invited owner only.
- Counts: `Plays`, `PlaysWon`, `Visits` on `Plynling` (migration `AddPlynlingPlayCounts`).
- French user text, gendered via `GenderedLines` / `Agree`. Build `-warnaserror`; commit per task, never push; a final "Updated version" commit.

---

### Task 1: The games' rules

**Create:** `ProjectSYNCS/Helpers/PlynlingGames.cs` — `enum PlynlingGame { HideAndSeek, RockPaperScissors, HigherLower }`, `enum GameStatus { Playing, Won, Lost }`, `enum RpsThrow { Rock, Paper, Scissors }`, `enum RpsResult { Win, Lose, Tie }`, `enum GuessHint { Higher, Lower, Correct }`, and `sealed class PlynlingGameState(PlynlingGame game, Random rng)` with `Hide(int rock, Random rng) : bool`, `Throw(RpsThrow player, Random rng) : RpsResult`, `Guess(int n) : GuessHint`, and read-only state (`Status`, `Round`, `HiddenBehind`, `LastPick`, `PlayerScore`, `PlynlingScore`, `LastPlayerThrow`, `LastPlynlingThrow`, `LastResult`, `Secret`, `TriesLeft`, `LastGuess`, `LastHint`).

- [ ] Checks first (`$SCRATCH/extrascheck`), with a `FixedRandom : Random` replaying a queue: hide found on round 1 → Won after one pick; missed then found → Won in round 2; missed twice → Lost; PFC win/lose/tie from each pair, ties don't score, 2–0 and 2–1 finish, a tie at 1–1 keeps playing; plus-ou-moins hints both ways, Correct wins, the 6th wrong guess loses, and a finished game refuses further moves.
- [ ] Implement; checks pass; commit « Added the Plynling mini-games' rules ».

### Task 2: Rewards, counts, cooldowns

**Modify:** `Models/Plynling.cs` (`Plays`, `PlaysWon`, `Visits` + migration), `Helpers/PlynlingLife.cs` (`PlayCooldown`, `PlayAmount`, `PlayWinBonus`, `VisitAmount`, `RollPlayPebbles(Random)`, `Play(p, now, won)`, `Visit(p, now)`), `Services/PlynlingCooldowns.cs` (`Play` gate keyed by Plynling id; `TryClaimVisit(a, b, dayKey)` / `ReleaseVisit` over an ordered pair), `Services/PlynlingService.cs` (`FinishPlayAsync(plynlingId, ownerId, won, pebbles, now)`, `VisitAsync(visitorId, hostId, now)`).

- [ ] Checks: happiness +15/+25 and clamped at 100 %, counts, `Visit` +20 % and +1 each, the play gate, the pair rule (either direction, next day free).
- [ ] Implement, migration (two… three `AddColumn`s only), commit « Added play and visit rewards and counts ».

### Task 3: `/plynling play`

**Create:** `Services/PlynlingPlayService.cs` (singleton; sessions by short id; 10-minute expiry; per-session lock), `Commands/PlynlingPlayCards.cs` (`BuildGame(session, plynling, now, reward)`), `Helpers/PlynlingGameUi.cs` (pure text), `Interactions/Modals/GuessModal.cs`. **Modify:** `PlynlingModule` (`play`), `PlynlingComponentHandler` (`plyn:hide:{id}:{0-2}`, `plyn:rps:{id}:{0-2}`, `plyn:guess:{id}` → modal, `plyn:guessm:{id}`), `BotResponses` (`PlynlingPlayWinLines`, `PlynlingPlayLoseLines`), `PlynlingText`, `Program.cs` (singleton).

- [ ] Checks: the text of every state of every game, button ids distinct, component count ≤ 40, reward line.
- [ ] Implement; commit « Added /plynling play ».

### Task 4: `/plynling visit`

**Modify:** `PlynlingModule` (`visit`), `PlynlingComponentHandler` (`plyn:visit:{visitorId}:{hostOwnerId}:{expiresUnix}`), `PlynlingPlayCards` (`BuildKnock`, `BuildKnockClosed`, `BuildMeeting`), `BotResponses` (`PlynlingVisitKnockLines`, `PlynlingVisitMeetLines`), `PlynlingText`.

- [ ] Checks: the knock's custom-id round-trips, the meeting card fits, the lines' genders.
- [ ] Implement; commit « Added /plynling visit ».

### Task 5: Docs and version

- [ ] `/plynling help` (play, visit; caps), `README.md`, `CLAUDE.md`; commit; bump `config.yaml`; commit « Updated version ».
