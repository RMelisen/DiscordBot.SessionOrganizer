# Button Colours Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every button colour one meaning across the bot, write the rule down, and recolour the five buttons that break it.

**Architecture:** No new code: a rule in `CLAUDE.md` (*Conventions*) and `ButtonStyle` swaps at the outlying call sites. Card builders are static; nothing else reads a button's style.

**Tech Stack:** C# / .NET 10, Discord.Net.

**Spec:** `docs/superpowers/specs/2026-11-23-button-colours-design.md`

## Global Constraints

- Green `Success` = yes, I'm in, commit. Red `Danger` = no, stop, or it can hurt. Blurple `Primary` = do this; pick one of these; the selected tab. Grey `Secondary` = neutral.
- « 💌 Lui avouer ses sentiments » is `Success`, the theme's one exception, said in a comment.
- No label, emoji, layout or custom-id changes.
- Patch bump to `8.2.6` in `ProjectSYNCS/config.yaml`, one English bullet in `ProjectSYNCS/CHANGELOG.md` (`## 8.2.6 — 2026-11-23`).

## Review Focus

- A visit card offering « 💌 »: the options must be blurple and « 💌 » green, so it still stands apart (no automated check; read `BuildChoice`).
- The poll card after a session exists: the disabled « Session créée » stays `Secondary` (only the live « Créer une session » changes).
- Blackjack with « Doubler » disabled: still renders (style change only).
- `ChoiceStyle` must stay the one constant both visit steps use; no step keeps a literal `ButtonStyle`.
- No other `ButtonStyle` call site changes (`git diff --stat` shows only the files listed below).

---

### Task 1: Recolour the outliers and write the rule

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingVisitCards.cs:32-34` (`ChoiceStyle` → `ButtonStyle.Primary`, comment updated to the theme) and `:75` (« 💌 » → `ButtonStyle.Success`, comment: the one special move)
- Modify: `ProjectSYNCS/Commands/Scheduling/PollCards.cs:87` and `:114` (« Créer une session » → `ButtonStyle.Primary`)
- Modify: `ProjectSYNCS/Commands/Arcade/PartyCards.cs:145` (« Doubler » → `ButtonStyle.Danger`)
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Rendering.cs:199` (« Événement (n) » → `ButtonStyle.Primary`)
- Modify: `CLAUDE.md` — a `### Button colours` subsection under *Conventions that will bite you*: the four meanings in one line each, « pick one » is blurple, a button opening a form or the next step is blurple (green is the answer or final commit), « 💌 » the one exception.
- Modify: `ProjectSYNCS/config.yaml` (`8.2.6`), `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1: Make the edits above.**
- [ ] **Step 2: Build** — `cd ProjectSYNCS && dotnet build`. Expected: `Build succeeded`, 0 warnings, 0 errors.
- [ ] **Step 3: Checks** — `dotnet run --project tests/ProjectSYNCS.Checks` from the repo root. Expected: exit 0 (cards are measured there).
- [ ] **Step 4: Scope check** — `git diff --stat` lists only the files above plus the spec and this plan.
- [ ] **Step 5: Commit** spec, plan and changes: `Buttons: one meaning per colour; visit choices blurple, 💌 green (8.2.6)`.
