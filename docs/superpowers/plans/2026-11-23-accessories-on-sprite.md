# Accessories on the sprite — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** every worn or tried-on accessory is drawn on the Plynling's own picture, in the style the
player picked; the sac à dos and the casque audio leave the game, refunded.

**Architecture:** the Python art tool (`tools/plynling-art/accessories.py`, done and approved
species by species) draws 17 accessories, some in styles; `export.py` writes one animated WebP per
shape × card face × look. The bot's `PlynlingArt.Sprite` / `SpriteOf` pick the `_with_<accessory>[_<style>]`
file; the style is a new column on the Plynling, picked in the Garde-robe. A checks section proves
every URL the bot can build exists. One data migration retires the two dropped accessories.

**Tech Stack:** Python 3 + Pillow (art), .NET 10 / Discord.Net / EF Core SQLite (bot), `tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-23-accessories-on-sprite-design.md`

## Global Constraints

- Filenames: `plynling_<species>[_baby]_<face>[_dirty]_with_<accessory>[_<style>]_v4.webp`; `<accessory>` is
  the cosmetic key without `cos.accessory.`; a styled accessory always carries its style (default included).
  No `ART_VERSION` / `PlynlingArt.Version` bump.
- Card faces: happy, content, sad, hungry, starving, frozen, sleeping, sick (not angry); dirty for
  `DIRTY_STATES`; shapes: the 7 mushrooms, adult + baby. 36 looks → 7 560 new files.
- Every existing file re-exports byte-identical (`git status` shows only added files under assets).
- Adult + hat = 40 px canvas (×8 = 320 px); everything else 32 px (×8 = 256 px).
- Styles (bot and art must agree, the checks compare). Each accessory has 0–2 choices, a style is
  the picked options joined by `_` in choice order (default = first of each):
  noeud Couleur rose|bleu|dore|blanc; fleur Couleur blanche|rose|bleue|jaune;
  echarpe Couleur rouge|bleue|verte + Position haut|bas (`bleue_haut`); casquette Couleur bleue|rouge|verte;
  ballon Couleur rouge|bleu|jaune; moufles Couleur rouges|bleues|vertes;
  collier_coquillages / medaillon Position haut|bas.
- Retired: `cos.accessory.sac_a_dos`, `cos.accessory.casque_audio` — refund 1 200 cailloux per copy.
- Python: run scripts by path, never piped (memory: windows shell gotchas). CRLF files: `unix2dos` after `sed -i`.
- Bot: CLAUDE.md static-initialiser rule; 40-component cap re-summed; config 8.4.0 → 8.5.0 with a
  CHANGELOG entry; no commit, no push without asking.

## Review Focus

1. A Plynling wearing a retired/unknown accessory key → plain picture, no broken URL.
2. A stored style that isn't one of the accessory's (stale, hand-edited) → the default style's file.
3. Angry mood (visit face) with an accessory → plain angry picture.
4. Sunflower species (no art) → unchanged behaviour; the check must not demand files for them.
5. Wearing another accessory, taking it off, or losing the last copy (`InventoryService.TakeAsync`)
   resets the style; a dead Plynling's card → memorial, never an accessory URL.

---

### Task 1: Accessory drawings (done — owner gate passed 2026-11-23)

`tools/plynling-art/accessories.py`: `ACCESSORIES` (17, each `family` + `styles` {name: draw}, first =
default), `looks()`, `measured`, `wear(im, acc, body, state, style)`, `dressed(...)`, `tall(acc, stage)`,
`HAT_CANVAS`. README bullet with Task 7.

### Task 2: The guard checks (RED)

**Files:** Create `tests/ProjectSYNCS.Checks/AccessoryArtChecks.cs` (section 62); Modify `Program.cs`.

- [ ] Step 1: write the checks:
  - `EveryLookHasItsFiles`: for every species, stage, `PlynlingMood`, dirty: if the plain `Sprite(...)`
    file exists, then for every accessory in the catalogue and every one of its styles (or none),
    the `Sprite(..., accessory, style)` file exists — angry: equals the plain URL.
  - `StylesMatchTheArt`: for every accessory, the set of `_with_<acc>[_<style>]` stems found on disk
    for `amanite content` equals the bot's styles (no drawn style the bot can't pick, and back).
  - `SpriteOfReadsTheOutfit`: couronne → `_with_couronne_v4.webp`; noeud with style null →
    `_with_noeud_rose_`; style `bleu` → `_with_noeud_bleu_`; style `vert` → `_with_noeud_rose_`; echarpe null → `_with_echarpe_rouge_haut_`, `verte_bas` → `_with_echarpe_verte_bas_`;
    key `cos.accessory.nope` → no `_with_`; `Outfit.Of(p).With(haut_de_forme)` → `_with_haut_de_forme_`.
  - `RetiredAreGone`: `CosmeticCatalog.ByKey` of the two retired keys is null; not in any week's shop.
  - `StyleResets`: `CosmeticSlots.Set(p, Accessory, other)` and `null` clear `AccessoryStyle`; setting
    the same key keeps it.
  - `WardrobeStyleSelect`: the Garde-robe on the Accessoire slot wearing an écharpe shows two selects
    `cosm:style:0:cos.accessory.echarpe` (3 colours) and `cosm:style:1:…` (2 places); at its fullest ≤ 40 components
    (through the existing `CardCheck`).
- [ ] Step 2: run checks. Expected: build fails (no style parameter / column).

### Task 3: The style in the model and the picture (GREEN for the non-file checks)

**Files:** Create `ProjectSYNCS/Helpers/Economy/AccessoryStyles.cs`; Modify `Models/Plynling/Plynling.cs`,
`Helpers/Economy/CosmeticCatalog.cs` (`CosmeticSlots`), `Helpers/Economy/Outfit.cs`,
`Helpers/Plynling/PlynlingArt.cs`, `PlynlingModule.Rendering.cs` (`CardHead`); migration `AddAccessoryStyle`.

- `AccessoryStyles`: `Choices(string accessoryKey) -> IReadOnlyList<(string Label, IReadOnlyList<(string Key, string Name)> Options)>`
  (« Couleur », « Position »; empty when unstyled), `Resolve(key, style) -> string?` (each part kept if valid, else its default; null
  for an unstyled accessory). One method, no cross-file static reads.
- `Plynling.AccessoryStyle` (`string?`); `CosmeticSlots.Set` for the accessory slot clears it when the key changes.
- `Outfit` gains `AccessoryStyle`; `Of` copies it; `With`/`Without` of the accessory slot reset it unless the key is unchanged.
- `PlynlingArt.Sprite(species, stage, mood, dirty = false, string? accessory = null, string? style = null)`;
  `SpriteOf(p, now, mood = null, Outfit? outfit = null)`; `CardHead` passes `worn`.
- [ ] run checks: only the file checks fail.

### Task 4: Export the files

**Files:** Modify `tools/plynling-art/export.py`; add `assets/plynlings/*_with_*`.

- [ ] For each shape × card face × dirty variant: build the 16 frames once (with their Body), then save
  each look via `wear`; scale ×8 whatever the canvas. `main()` still deletes and rewrites all.
- [ ] Run in the background. Expected: 319 + 7 560 files; `git status --short ../assets` only `??`.
- [ ] Run checks: ALL CHECKS PASSED except the wardrobe/retired ones (Tasks 5–6).

### Task 5: The style picker in the Garde-robe

**Files:** `Commands/Economy/CosmeticCards.cs`, `Interactions/Components/CosmeticComponentHandler.cs`,
`Services/Economy/CosmeticService.cs`.

- [ ] On the Accessoire slot, wearing a styled accessory: one select per choice (« Couleur », « Position »)
  (`cosm:style:{choice}:{key}`, current option selected); a pick defers, `CosmeticService.StyleAsync` sets it if
  that accessory is still worn by the living Plynling, saves, redraws. Run checks: wardrobe check green.

### Task 6: Retire the sac à dos and the casque audio

**Files:** `CosmeticCatalog.cs` (remove both entries); migration `RetireBackpackAndHeadphones` (data);
`docs/agents/architecture.md` (six data migrations).

- [ ] SQL, in one migration: wallets += 1 200 × the copies held (same guild and user); delete those
  inventory rows; `AccessoryKey`/`AccessoryStyle` → null where it was one of them. `Down` is empty.
- [ ] Run checks: ALL CHECKS PASSED; `dotnet build` 0 warnings.

### Task 7: Docs, version, changelog, review

**Files:** `tools/plynling-art/README.md`, `docs/agents/plynling.md` (art: filenames, styles, hat
canvas, fallback, the check), `docs/agents/economy.md` (Cosmetics: drawn accessories, styles, the
`cosm:style` id, the retirement), `ProjectSYNCS/config.yaml` 8.5.0, `ProjectSYNCS/CHANGELOG.md`.

- [ ] Write them; checks and build once more. Expected: green.
- [ ] Final whole-change review, fixes, then report (no commit, no push unless asked).
