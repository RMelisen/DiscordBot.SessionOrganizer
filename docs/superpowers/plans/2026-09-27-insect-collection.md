# The Insectes collection — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an 8-insect collection set, found like the other small sets.

**Architecture:** One `CollectionSet` and eight `Add(...)` rows in `ItemCatalog`; the draws,
the book, the inventory and set completion already work per set, so nothing else changes.

**Tech Stack:** C# / .NET 10, the scratch harness `extrascheck`.

Spec: `docs/superpowers/specs/2026-09-27-insect-collection-design.md`.

## Global Constraints

- Item keys are stored: `col.<name>`, never renamed.
- User-facing text French; the set is named « Insectes ».
- Discord caps: embed 6000 / field 1024, 25 select options.
- Checks go in the scratch harness first; build with `-warnaserror`; commit per task; push only
  when the owner asks.

---

### Task 1: The set

**Files:** Modify `ProjectSYNCS/Helpers/ItemCatalog.cs` (`Sets`, `BuildAll`).

**Produces:** set key `insectes`; items `col.fourmi`, `col.mouche`, `col.moustique`,
`col.chenille`, `col.coccinelle`, `col.grillon`, `col.abeille`, `col.morpho_bleu`.

- [ ] **Step 1: Update and write the checks** — the counts (70 collectibles, 6 sets, the set
  reward list `100, 150, 200, 300, 250, 1000`, « every set has 8, the mushrooms 30 »), the book's
  « 8/70 » and « 1/6 » texts; new checks: the insects' rarities, keys and names unique, drawn by
  both draws and never in the Champignons half, completing the set pays 250 once, every book page
  and the full inventory within the caps.
- [ ] **Step 2: Run the harness** — expect failures.
- [ ] **Step 3: Implement** the set and its eight items.
- [ ] **Step 4: Run the harness and build.**
- [ ] **Step 5: Commit** — « Added the Insectes collection ».

### Task 2: Docs and version

**Files:** `ProjectSYNCS/Commands/PlynlingModule.cs` (help text), `README.md`, `CLAUDE.md`,
`ProjectSYNCS/config.yaml`.

- [ ] **Step 1:** Update the help text, README and CLAUDE.md; run the harness (help caps); build.
- [ ] **Step 2:** Commit; bump the version as its own « Updated version » commit; push only if
  the owner asks.
