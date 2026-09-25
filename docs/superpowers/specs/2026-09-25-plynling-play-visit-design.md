# Plynlings: play and visit — design

**Date:** 2026-09-25 · **Status:** approved

Part 1 of the Plynlings' V2. Part 2 (achievements and a journal) gets its own design; this part
starts recording the counts it will need.

## 1. `/plynling play` — your own Plynling only

One of three games, **picked at random** each time:

| Game | Rules |
|---|---|
| **Cache-cache** | It hides behind one of 3 rocks (🪨 buttons). **2 rounds**; found once = won, and the game ends the moment it is found (a find on round 1 ends it there). |
| **Pierre-feuille-ciseaux** | You against it, **best of 3** (first to 2); a tie is replayed. |
| **Plus ou moins** | A number from 1 to 100, **6 tries**. Each guess is typed in a small modal (« Deviner »); it answers « plus » / « moins ». |

- **Rewards** (applied when the game ends): **+15 % happiness** for playing, **+10 % more** on a
  win, and **5–10 cailloux** to the player on a win. Win and cailloux land in one save.
- **Once per hour per Plynling**, the hour starting when the game **starts** — abandoning a
  game never rolls a new one.
- Refused while it **sleeps**, is **frozen** or **dead**; buttons pressed by anyone but the owner
  are refused privately.
- One public **Components V2** message, edited in place: the Plynling's sprite, the game's state in
  its voice, the buttons.
- The secrets (the rock, the number, its throw) live **in memory** (`PlynlingPlayService`,
  singleton), never in a custom-id. A game expires after **10 minutes**; a restart ends games in
  progress — harmless, the hour was already spent.

## 2. `/plynling visit user:`

- Your Plynling **knocks**: a public message « 🚪 Pouf toque chez Mimi ! » with an « Accueillir »
  button. **Only the other owner** can press it; the invitation **expires after 1 hour** (the
  expiry travels in the custom-id).
- **On accept**: the message becomes one card — both Plynlings side by side (a media gallery of
  the two sprites) and a line — and **both get +20 % happiness**. **No cailloux**, so two
  accounts cannot farm it.
- **Once a day per pair** (Paris day, either direction), held in memory. Neither Plynling may be
  asleep, frozen or dead, when knocking or when accepting; you cannot visit yourself.

## 3. Counts for the achievements

`Plynling.Plays`, `Plynling.PlaysWon`, `Plynling.Visits` (one migration,
`AddPlynlingPlayCounts`), incremented in the same saves as the rewards. A visit counts for both
Plynlings.

## 4. Structure

- `Helpers/PlynlingGames` — the pure rules of the three games (rounds, results, hints), checkable
  without a gateway, with the randomness passed in.
- `Services/PlynlingPlayService` — singleton holding live games by id, with expiry.
- `PlynlingLife` — `PlayCooldown`, `PlayAmount`, `PlayWinBonus`, `VisitAmount`, `Play`, `Visit`.
- `PlynlingService` — `FinishPlayAsync` (happiness + counts + cailloux in one save),
  `VisitAsync` (both Plynlings in one save).
- `Commands/PlynlingPlayCards` — static builders for the game and visit messages.
- `PlynlingModule` — `play`, `visit`; `PlynlingComponentHandler` — the game buttons, the guess
  modal, « Accueillir ».
- `BotResponses` — gendered pools for the game's reactions and the visit.

## Checks

The games' rules (every round, win and loss, tie replays, the 6 tries and the hints), the
rewards and their clamping, the play cooldown, the once-a-day pair rule, refusals, custom-id
uniqueness, component counts, the help caps. Build `-warnaserror`.
