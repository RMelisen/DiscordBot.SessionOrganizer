# Plynling Aura Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A fifth cosmetic slot, « Aura », worn with any accessory: three animated effects
(Scintillements, Bulles, Pétales) drawn on the Plynling's own picture.

**Architecture:** Pre-rendered, like accessories: the Python art tool draws each aura over every
plain and dressed card loop and exports `…_aura_<key>_v5.webp`; the bot only builds the filename.
In the bot the aura is one more slot through `CosmeticSlots` / `Outfit` / `CosmeticCatalog`, one
nullable column on `Plynling`, and one more segment in `PlynlingArt.Sprite`.

**Tech Stack:** C# / .NET 10, EF Core + SQLite, Discord.Net Components V2; Python 3 + Pillow for the art.

**Spec:** `docs/superpowers/specs/2026-11-23-plynling-aura-design.md` (read it first).

## Global Constraints

- Keys `cos.aura.scintillements`, `cos.aura.bulles`, `cos.aura.petales`; aura key in a filename is the part after `cos.aura.`.
- Filename: `plynling_<species>[_baby]_<face>[_dirty][_with_<accessory>[_<style>]]_aura_<aura>_v5.webp` — aura segment **last**; no existing filename changes; `ART_VERSION` / `PlynlingArt.Version` stay **5**.
- Aura faces: happy, content, sad, hungry, starving, sleeping (clean and dirty). **Never** frozen, sick, angry, nor visit pictures.
- Expected new files: 14 shapes × 12 face/dirt × 37 looks (none + 36 accessory looks) × 3 auras = **18 648**.
- Scintillements: Rotating, Rare (2 400). Bulles: Rotating, Uncommon (1 200). Pétales: Crafted, trophy, spring-only recipe from `col.primevere` + `col.cerise`, craft price 300, mean aimed forages within **60–150**.
- `CosmeticSlot.Aura` is **appended** to the enum (the shop seeds on `(int)slot`; existing slots must keep their draw).
- User-facing strings French, code/comments English. Slot label « Aura », plural « auras ».
- Try-on note, exact copy: `-# Son aura ne se voit pas tant qu'il est malade ou gelé.` / feminine `-# Son aura ne se voit pas tant qu'elle est malade ou gelée.`
- Version **8.6.0** (minor: migration + new slot) with a `CHANGELOG.md` entry; no easter eggs or member names.
- **Never `git commit`** — the owner commits by hand. Each task ends by stopping for review instead.
- Windows: run Python as `python` (not `python3`), never `python - <<EOF`; `sed -i` strips CRLF — use the Edit tool.
- Checks: `dotnet run --project tests/ProjectSYNCS.Checks` must end `ALL CHECKS PASSED`.

## Review Focus

1. **A Plynling wearing an aura that turns sick or frozen** — its card must show the plain sick/frozen picture (no `_aura_`), not a missing file. Pinned in Task 4.
2. **A retired, unknown or non-aura key in `AuraKey`** (e.g. an accessory key written there) — shows no aura, never a broken URL. Pinned in Task 4.
3. **Trying on an accessory while wearing an aura (and the reverse)** — the try-on keeps the other one; taking one off keeps the other. Pinned in Task 3.
4. **Losing the last copy of a worn aura** (gift, trade) — `InventoryService.TakeAsync` must clear `AuraKey` like any slot. Pinned in Task 3.
5. **The shop's existing weekly draw** — appending the slot must not change which thèmes/titres/accessoires/cadres a given week shows. Pinned in Task 3.

---

### Task 1: Draw the three auras and the preview sheets (owner review gate)

**Files:**
- Create: `tools/plynling-art/auras.py`

**Interfaces:**
- Consumes: `accessories.measured`, `accessories.wear`, `accessories.tall`, `accessories.Body`, `accessories.Pen`, `HAT_CANVAS`, `HAT_AT`, `common.sheet`, `common.SPECIES`, `motion.FRAMES`.
- Produces:
  - `AURAS: dict[str, callable]` — keys `"scintillements"`, `"bulles"`, `"petales"` in that order.
  - `AURA_STATES = ["happy", "content", "sad", "hungry", "starving", "sleeping"]`.
  - `surround(im, body, state, aura, frame, tall=False) -> Image` — returns a copy of `im` (32-cell sprite, or the 40-cell hat canvas when `tall`) with the aura drawn for that frame; when `tall`, positions are offset by `HAT_AT` so the aura surrounds the body, not the canvas.

- [ ] **Step 1:** Write the three effects as functions of the frame (16 frames, 2 s loop; frame 15 must flow back into frame 0): twinkling stars (the 8.5.1-removed look: single pixels plus a small 5-pixel cross, pale gold), soap bubbles rising and popping, petals drifting down. Particles stay in the margin around the body (use `Body` regions to keep off the cap and face), never tinted by dirt.
- [ ] **Step 2:** `__main__` writes preview sheets to the directory given as `argv[1]`: one sheet **per aura**, rows = the 14 shapes (adult + baby per species), columns = the six aura faces, frame 0, plain; plus one sheet per aura of the content Amanite adult worn with `couronne`, `echarpe`, `ballon`, `lunettes` (the hat case must show the aura around the body on the taller canvas), and one animated WebP per aura of the content Amanite so the motion can be seen.
- [ ] **Step 3:** Run `python tools/plynling-art/auras.py <scratchpad>/auras` — expected: the sheet paths printed, no exception.
- [ ] **Step 4: Owner review gate.** Send the sheets and the animations to the owner, aura by aura; apply corrections (they come in small steps: one pixel, another colour) and re-send until each aura is approved. **Do not start Task 2 before all three are approved.**

### Task 2: Export the aura pictures

**Files:**
- Modify: `tools/plynling-art/export.py` (`main`, new `export_auras`, `__main__`, module docstring)
- Modify: `tools/plynling-art/README.md` (the aura files, their count, the 18 648)

**Interfaces:**
- Consumes: Task 1's `AURAS`, `AURA_STATES`, `surround`; existing `loop`, `looks`, `wear`, `tall`, `save_loop`, `STAGED`.
- Produces: the 18 648 files in `assets/plynlings/` named per Global Constraints.

- [ ] **Step 1:** `export_auras() -> int`: for each species, stage (`STAGED`), state in `AURA_STATES`, dirty in (False, True): measure `loop(...)` once, then for the plain look and every `looks()` entry, dress each frame (`wear` for an accessory), then `surround(..., tall=tall(acc, stage) if acc else False)` per aura, and `save_loop` under the name. Returns the count.
- [ ] **Step 2:** `main()` calls `export_auras()` after `export_accessories()`. `python export.py auras` runs **only** `export_auras()` (no deletion of existing files) and prints its count.
- [ ] **Step 3:** Run `python tools/plynling-art/export.py auras` in the background (it is long). Expected: `18648 files written`.
- [ ] **Step 4:** Verify: `ls assets/plynlings | grep -c _aura_` → `18648`; `git status --short assets/plynlings | grep -v '^??'` → empty (no existing file modified).
- [ ] **Step 5:** Stop for the owner's review (no commit).

### Task 3: The Aura slot in the bot (catalogue, model, migration)

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs`, `ProjectSYNCS/Helpers/Economy/Outfit.cs`, `ProjectSYNCS/Models/Plynling/Plynling.cs`
- Create: migration `AddAura` (`dotnet ef migrations add AddAura` from `ProjectSYNCS/`)
- Create: `tests/ProjectSYNCS.Checks/AuraChecks.cs`; Modify: `tests/ProjectSYNCS.Checks/Program.cs` (register section 64), `tests/ProjectSYNCS.Checks/CraftingChecks.cs` (`Catalog`)

**Interfaces:**
- Produces:
  - `enum CosmeticSlot { Theme, Title, Accessory, Grave, Aura }`
  - `Plynling.AuraKey : string?` (comment: read/written only through `CosmeticSlots`)
  - `CosmeticCatalog.SlotOrder : IReadOnlyList<CosmeticSlot>` = Theme, Title, Accessory, Aura, Grave (display order; enum order stays storage/seed order). Declared above `All` in the same file.
  - `SlotLabel(Aura) == "Aura"`, `SlotPlural(Aura) == "auras"`; `Label` and `ShortName` of an aura = its `Name` (« Scintillements », « Bulles », « Pétales »).
  - `Outfit` gains `CosmeticInfo? Aura` (positional, after `Grave`, before `AccessoryStyle`); `Of`, `In`, `With`, `Without` handle it.
  - Catalogue entries via a local `Aura(key, emoji, name, source, rarity, craftPrice, recipe, trophy)` helper: `scintillements` ✨ Rot R; `bulles` 🫧 Rot U; `petales` 🌸 Cr, trophy, craftPrice 300, recipe `("col.primevere", n), ("col.cerise", m)`.

- [ ] **Step 1: Write the failing checks** in `AuraChecks.cs` (`// 64. Auras: …`, same `Expect`/`_fail` pattern as `AccessoryArtChecks`), method `Catalogue()`:
  - the three keys exist with the slot, source, rarity and price above; `CosmeticCatalog.SlotOrder` is exactly the five in the order above;
  - `Outfit.Of(p)` with `AuraKey = "cos.aura.bulles"` and an accessory: `.With(<another accessory>)` keeps `.Aura`; `.With(<scintillements>)` keeps `.Accessory` and `AccessoryStyle`; `.Without(Aura)` keeps the accessory;
  - `CosmeticSlots.Set(p, Aura, k)` does not touch `AccessoryStyle`;
  - for 60 weeks from 2026-01-05, `Shop(week)` filtered to the four old slots equals the same list computed with a copy of the old algorithm (`new Random(week * 10 + (int)slot)` over the old slot list) — the draw of existing slots is unchanged;
  - in-memory SQLite (copy the `Db` pattern of `CraftingChecks`): a Plynling wearing `cos.aura.bulles`, owner holds 1; `InventoryService.TakeAsync` of it → the Plynling's `AuraKey` is null.
  Register `AuraChecks.Run(Fail)` (async if the DB case needs it) after section 63 in `Program.cs`.
- [ ] **Step 2:** Run `dotnet run --project tests/ProjectSYNCS.Checks` — expected: build error (no `CosmeticSlot.Aura`) / failures.
- [ ] **Step 3:** Implement the interfaces above; `CosmeticSlots.Get/Set` map `Aura` ↔ `AuraKey` (`_ => GraveKey` defaults must become explicit cases so Aura never falls into Grave — audit every `switch` on `CosmeticSlot`, see `grep -rn "CosmeticSlot\." ProjectSYNCS`). Run `dotnet ef migrations add AddAura` in `ProjectSYNCS/` (schema-only: one nullable TEXT column).
- [ ] **Step 4:** Update `CraftingChecks.Catalog`: recipes 19, Ouvrages 8; the « 2 Ouvrages per slot » loop skips `CosmeticSlot.Aura` (comment why: the spec fixes three auras, no Ouvrage). Tune the Pétales counts until `Bands()` prints it inside 60–150.
- [ ] **Step 5:** Run checks — expected `ALL CHECKS PASSED` except section 62/64 file checks not yet written; `dotnet build` with no warnings.
- [ ] **Step 6:** Stop for review (no commit).

### Task 4: The aura on the picture

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingArt.cs`
- Modify: `tests/ProjectSYNCS.Checks/AuraChecks.cs`

**Interfaces:**
- Consumes: Task 3's `Outfit.Aura`, `Plynling.AuraKey`.
- Produces: `PlynlingArt.Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood, bool dirty = false, string? accessory = null, string? style = null, string? aura = null)`; `SpriteOf` passes `worn.Aura?.Key`. A private `AuraMoods` set = Happy, Content, Sad, Hungry, Starving, Sleeping (comment: must match `AURA_STATES` in `tools/plynling-art/auras.py`).

- [ ] **Step 1: Write the failing checks** in `AuraChecks.cs`, method `EveryAuraHasItsFiles(assets)` modelled on `AccessoryArtChecks.EveryLookHasItsFiles`: for every species/stage/mood/dirty with a plain file, every aura, no accessory plus every accessory × style: if mood ∉ aura moods, expect the URL equals the same URL without aura (Review Focus 1); else the file exists. Expect the checked count is exactly 18 648. Method `SpriteOfReadsTheAura()`: worn `bulles` → ends `_aura_bulles_v5.webp`; worn `bulles` + `noeud` `bleu` → contains `_with_noeud_bleu_aura_bulles_`; `AuraKey = "cos.aura.nope"` and `AuraKey = "cos.accessory.couronne"` → no `_aura_` (Review Focus 2); try-on `Outfit.With(petales)` → `_aura_petales_`; sick and frozen moods → no `_aura_`.
- [ ] **Step 2:** Run checks — expected: these fail.
- [ ] **Step 3:** Implement: the segment is `_aura_<key after "cos.aura.">` appended after the accessory segment, only for a catalogue entry with `Slot == Aura` and a mood in `AuraMoods`.
- [ ] **Step 4:** Run checks — expected: sections 62 and 64 pass (section 62's angry/plain expectations unchanged).
- [ ] **Step 5:** Stop for review (no commit).

### Task 5: The aura on the screens

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingCardUi.cs` (`Heading`), `ProjectSYNCS/Commands/Economy/CosmeticCards.cs` (head + `WardrobeBody`), `ProjectSYNCS/Commands/Economy/InventoryModule.cs` (cosmetics fields), `ProjectSYNCS/Services/Admin/AdminStatsService.cs` only if its count needs nothing (it iterates the enum — leave it)
- Modify: `tests/ProjectSYNCS.Checks/AuraChecks.cs`, `tests/ProjectSYNCS.Checks/CosmeticScreenChecks.cs`

**Interfaces:**
- Consumes: `Outfit.Aura`, `CosmeticCatalog.SlotOrder`, `PlynlingLife.Mood(p, now)`.

- [ ] **Step 1: Write the failing checks:** `Heading` of a male Plynling wearing `bulles` contains `\n🫧 Entouré de bulles`, female wearing `petales` contains `Entourée de pétales`, with an accessory too both lines appear (accessory line first); the cosmetics head for a sick (and a frozen) Plynling trying `scintillements` contains the exact note from Global Constraints (both genders), and a healthy one does not; the Garde-robe slot row's button labels are in `SlotOrder` order (5 buttons). Extend `CosmeticScreenChecks.Caps` so its fullest case wears/tries an aura and a sick Plynling (the note line counts).
- [ ] **Step 2:** Run checks — expected: these fail.
- [ ] **Step 3:** Implement: the aura line `\n{emoji} {p.Gender.Agree("Entouré", "Entourée")} de {Name lowercased with fr-FR culture}` right after the accessory line; the note in `CosmeticCards` head when `tried.Aura is not null` and the mood is Sick or Frozen; `WardrobeBody` (both the slot row and the no-Plynling list) and `InventoryModule` iterate `CosmeticCatalog.SlotOrder` instead of `Enum.GetValues<CosmeticSlot>()`.
- [ ] **Step 4:** Run checks — expected `ALL CHECKS PASSED`; `dotnet build` clean.
- [ ] **Step 5:** Stop for review (no commit).

### Task 6: Docs and release

**Files:**
- Modify: `docs/agents/economy.md` (*Cosmetics*: five keys incl. `AuraKey`; the aura slot; drawn on the picture with the faces it skips; the file cost — every new accessory look costs 4 files per face/shape now, every new aura ~6 200; Pétales spring-only and why; Aura has no Ouvrage), `docs/agents/plynling.md` *Art* if it lists the sprite filename
- Modify: `README.md` and `HelpModule` / `PlynlingModule.Help` only where they list the cosmetic slots (`grep -rn "accessoire" README.md ProjectSYNCS/Commands/Core/HelpModule.cs ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Help.cs`)
- Modify: `ProjectSYNCS/config.yaml` → `8.6.0`; `ProjectSYNCS/CHANGELOG.md` → `## 8.6.0 — <date>` entry (the slot, the three effects and where they come from, Pétales craftable from spring, the sick/frozen note)

- [ ] **Step 1:** Write the docs and the changelog entry; bump the version.
- [ ] **Step 2:** Run `dotnet build` (from `ProjectSYNCS/`) and `dotnet run --project tests/ProjectSYNCS.Checks` — expected: build clean, `ALL CHECKS PASSED` (help near 6 000 chars still inside the caps).
- [ ] **Step 3:** Report to the owner: version 8.6.0, files changed, the 18 648 assets, and that testing on the dev guild needs the assets pushed to GitHub `main` — **ask before pushing**. No commit.
