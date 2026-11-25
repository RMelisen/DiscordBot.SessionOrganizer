# Soleil-Levant collection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the 12-piece « Soleil-Levant » cosmetics collection (with a koï aura), after splitting `/inventory view`'s wardrobe page in two. The kimono is deferred (owner, 2026-11-25).

**Architecture:** Cosmetics are catalogue lines (`CosmeticCatalog.Build`) plus art drawn by code (`tools/plynling-art`) and exported as layers the bot composes at runtime. Category, styles and families are mirrored between C# and Python and the checks compare them, so art and catalogue for the same pieces land in one task.

**Tech Stack:** C# / .NET 10 (Discord.Net), Python 3 + Pillow for the art, `tests/ProjectSYNCS.Checks` console checks.

**Spec:** `docs/superpowers/specs/2026-11-25-soleil-levant-collection-design.md`

## Global Constraints

- All 12 pieces are `CosmeticSource.Rotating`, price `TierPrice(rarity)` (600 / 1 200 / 2 400 / 3 600 / 5 000).
- Keys exactly: `cos.accessory.{onigiri,sugegasa,kanzashi,eventail,chapeau_chinois,katana}`, `cos.title.{zen,sensei}`, `cos.theme.{jardin_bambou,torii}`, `cos.grave.bambous`, `cos.aura.koi`. Append-only once shipped.
- Style option keys never contain « _ »: colours `rouge`, `indigo`, `sakura`; sides `gauche`, `droite`.
- French for every user-facing string; code, comments, logs English.
- No `git commit` step inside the tasks: the owner commits by hand unless they ask otherwise. The working tree may hold other uncommitted work (the 8.19.0 Gentleman batch): leave it alone.
- Art: preview sheets go to the owner (species by species) **before** `python export.py layers`. Never `python - <<EOF` on this machine; write a script file. `sed -i` strips CRLF on `.cs` files: use the Edit tool.
- Version: `config.yaml` already says 8.19.0 in the working tree → no bump; extend the 8.19.0 changelog entry.
- Checks: `dotnet run --project tests/ProjectSYNCS.Checks` must exit 0 at the end of every task that touches C#.

## Review Focus

1. Someone who owns every cosmetic opens `/inventory view` → both wardrobe pages keep names (not the icons-only fallback) — pinned in Task 1.
2. Sugegasa worn, then the chapeau chinois: one replaces the other (both Hat) — pinned in Task 3.
3. A katana in the left hand, then an éventail on the left: the katana moves to the right hand — pinned in Task 3.
4. A wide sugegasa on an adult with a wide cap (Cèpe, Russule): the brim stays inside the 40 px hat canvas — the Task 2 sheet covers every shape.
5. A frozen or happy (hopping) Plynling with a held katana or fan: the item follows the hop and takes the ice tint — the Task 2 faces sheet covers it.

---

### Task 1: Split the wardrobe page in two

**Files:**
- Modify: `ProjectSYNCS/Commands/Economy/InventoryModule.cs` (enum `InventoryPage` line 16; `BuildInventoryPage` 354–428)
- Modify: `ProjectSYNCS/Interactions/Components/InventoryComponentHandler.cs:95` only if it parses the page by anything other than `Enum.TryParse`
- Test: `tests/ProjectSYNCS.Checks/ReportChecks.cs` (around 235–258)

**Interfaces:**
- Produces: `enum InventoryPage { Items, Wardrobe, Wardrobe2 }` (append; code-only, travels in `inv:page:{page}`). `Wardrobe` = thèmes, titres, auras; `Wardrobe2` = accessoires, cadres, then the « Collections » field.

- [ ] **Step 1: Write the failing check** in `ReportChecks.cs`, after the worst-case loop: with `heldEverything`, for each of `Wardrobe` and `Wardrobe2`, fail unless the page's field values contain `CosmeticCatalog.ShortName` of the last item of each of its slots (names kept, not icons-only), and fail unless `Wardrobe2` has a field named « Collections » and `Wardrobe` has none. Keep the existing « every title » check pointing at `Wardrobe`.
- [ ] **Step 2: Run** `dotnet run --project tests/ProjectSYNCS.Checks` — expect failures naming `Wardrobe2` (enum value missing → compile error first; add the value, re-run, expect the names failure on `Wardrobe`).
- [ ] **Step 3: Implement.** Each wardrobe page lists only its slots (`SlotOrder` filtered: page 1 Theme, Title, Aura; page 2 Accessory, Grave), keeps the per-page icons-only fallback against `WardrobeBudget`, and the empty-description rule per page. Buttons: « 🎒 Objets », « 👗 Thèmes, titres, auras » (`inv:page:Wardrobe`), « 🎀 Accessoires, cadres » (`inv:page:Wardrobe2`) — Secondary, the current one Primary and disabled, as today. Titles: « Ton inventaire — garde-robe » on both, with « (1/2) » / « (2/2) ».
- [ ] **Step 4: Run** the checks — expect exit 0, and the printed worst-case sizes for both wardrobe pages well under 6 000.

### Task 2: Draw the six accessories (art only)

**Files:**
- Modify: `tools/plynling-art/accessories.py` (palette/INKS, drawings, `ACCESSORIES` entries, the `__main__` sheets)

**Interfaces:**
- Produces (Python `ACCESSORIES` keys, families, styles — Task 3 mirrors them exactly):
  - `onigiri` held, `held_sides(...)` → `gauche`, `droite`
  - `sugegasa` hat, `{"": hat(SUGEGASA)}`
  - `chapeau_chinois` hat, `{"": hat(CHAPEAU_CHINOIS)}`
  - `kanzashi` cap, sided via `on_cap`, default side `droite` (like the fleur) → `droite`, `gauche`; add `"kanzashi": "droite"` to `DEFAULT_SIDE`
  - `eventail` held, colours × sides → `rouge_gauche`, `rouge_droite`, `indigo_…`, `sakura_…`; add `"eventail": "gauche"` to `DEFAULT_SIDE`
  - `katana` held, `held_sides` → `gauche`, `droite`

- [ ] **Step 1: The six drawings.** Sugegasa: wide flat straw cone (`STRAW_*`), chin cord down both sides of the cap. Chapeau chinois: taller, narrower cone in red lacquer with a gold rim and a gold knob. Kanzashi: a pin with a dangling sakura flower and two small bead drops, on the cap at the fleur's spot. Éventail: a half-open folding fan, ribs dark, paper in the colour. Katana: sheathed blade, dark saya with a gold tsuba, held clear of the body. Onigiri: white triangle with a nori band.
- [ ] **Step 2: Preview.** Add a `soleil.png` sheet to `__main__`: rows = all 14 shapes, columns = each new look (every style), content face; plus `soleil_faces.png`: katana, éventail and sugegasa on the amanite across happy, content, sad, hungry, starving, frozen, sleeping, sick, content-sale. Run `python accessories.py <scratch dir>`, check every shape, and send both sheets to the owner (the owner asked for the plan to be finished in one go: fixes they ask for later are a follow-up redraw).

### Task 3: Catalogue, categories, styles, collection, layers

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs` (`Build`: 12 lines, the aura's included so the collection resolves)
- Modify: `ProjectSYNCS/Helpers/Economy/AccessoryFit.cs`
- Modify: `ProjectSYNCS/Helpers/Economy/AccessoryStyles.cs`
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCollections.cs`
- Modify: `ProjectSYNCS/Helpers/Economy/AccessoryNotices.cs` only if a notice switches on category names
- Test: `tests/ProjectSYNCS.Checks/AccessoryFitChecks.cs` (families map ~384), `tests/ProjectSYNCS.Checks/CraftingChecks.cs` (`Collections` ~108)
- Generated: `ProjectSYNCS/Assets/Plynlings/acc/*` via `python export.py layers`

**Interfaces:**
- Consumes: Task 2's keys, families and style names.
- Produces: the categories and styles below; no new category.

- [ ] **Step 1: Write the failing checks.**
  - `AccessoryFitChecks`, with `AccessoryFit.Pick`: worn `[sugegasa]`, pick chapeau_chinois in picker 1 → `Worn == [chapeau_chinois]`, `Replaced == "cos.accessory.sugegasa"`; worn `[katana(gauche)]`, pick eventail with style `rouge_gauche` in picker 1 → katana moved to `droite`, nothing replaced.
  - `CraftingChecks.Collections`: « Soleil-Levant » exists and holds exactly the 12 keys.
- [ ] **Step 2: Run** the checks — expect the new failures (unknown keys).
- [ ] **Step 3: Implement.**
  - Catalogue lines (rotating, tier from the spec table): onigiri 🍙 « un onigiri » C; sugegasa 👒 « un sugegasa » C; Zen « Le Zen » / « La Zen » C; kanzashi 🌸 « une épingle kanzashi » U; éventail 🪭 « un éventail » U; Jardin de bambou 🎋 accent `0x5E9E4A` U; Bambous 🎋 cadre U; chapeau chinois 🎎 « un chapeau chinois » R; Sensei « Le Sensei » / « La Sensei » R; Torii ⛩️ accent `0xD23C2A` R; katana 🗡️ « un katana » É; aura « Carpes koï » 🎏 É.
  - `AccessoryFit.Category`: kanzashi → Head; sugegasa, chapeau_chinois → Hat; onigiri, eventail, katana → Held.
  - `AccessoryStyles.Choices`: kanzashi → `Sides("Côté", false)`; eventail → `Colour(("rouge","Rouge"),("indigo","Indigo"),("sakura","Sakura")), Hand`; onigiri, katana → `Hand`.
  - `CosmeticCollections`: « Soleil-Levant » after « Gentleman », the 12 keys.
- [ ] **Step 4: Export layers:** from `tools/plynling-art`, `python export.py layers` (background it if over 2 min).
- [ ] **Step 5: Run** the checks — expect exit 0 (section 62 compares styles with the files on disk, 71 families with categories, 59/23 re-measure the screens).

### Task 4: The koï aura

**Files:**
- Modify: `tools/plynling-art/auras.py` (a `koi(spray, box, f)` and its `AURAS` entry)
- Test: `tests/ProjectSYNCS.Checks/AuraChecks.cs` (~221)

**Interfaces:**
- Produces: `cos.aura.koi`, Name « Carpes koï », 🎏, Rotating, Epic (3 600).

- [ ] **Step 1: Failing check:** `Entry("cos.aura.koi", CosmeticSource.Rotating, ItemRarity.Epic, 3600, false, "Carpes koï");` and assert `PlynlingCardUi`'s aura line for it reads « Entouré de carpes koï ».
- [ ] **Step 2: Run** — expect the missing-key failure.
- [ ] **Step 3: Draw** `koi`: two or three small koï (orange/white, one red-and-white), 4–5 px long, swimming a slow loop around the body's rest box over the 16 frames, with a faint ripple ring trailing; empty pixels only, like the others. Preview with `python auras.py <scratch dir>` (faces, accessories, animated WebP) and send it to the owner.
- [ ] **Step 4: Export** `python export.py layers`.
- [ ] **Step 5: Run** the checks — expect exit 0.

### Task 5: Icons

**Files:**
- Modify: `tools/item-art/icons.py` (10 `icon("…", key, name, unicode, *rows)` entries: 6 accessories, 2 thèmes, 1 cadre, 1 aura; titles use 🏷️ and get none)
- Generated: `ProjectSYNCS/Assets/Icons/<key>.png`

- [ ] **Step 1: Draw** the 10 icons at 16×16 in the pack palette, groups `accessoires`, `thèmes`, `cadres`, `auras` as the neighbours use.
- [ ] **Step 2: Export** with the script's usual command and send its preview to the owner.
- [ ] **Step 3: Run** the checks — expect exit 0 (icon files exist for every non-title cosmetic and every emoji name is valid).

### Task 6: Docs, guide, changelog

**Files:**
- Modify: `ProjectSYNCS/CHANGELOG.md` (8.19.0 entry), `docs/agents/economy.md` (cosmetic count 117 → 129, the two wardrobe pages replacing the « It is close » paragraph, aura count), `tools/plynling-art/README.md` (accessory and aura counts), `guide/cailloux.md` (and any guide page listing accessory places or the wardrobe page), `docs/cosmetics-balance.md` if it is regenerated by a checks command.

- [ ] **Step 1: Changelog bullets** (English, one per user-visible change): the Soleil-Levant collection and its pieces by tier; `/inventory view`'s wardrobe in two pages.
- [ ] **Step 2: Docs and guide** as listed; guide pages stay member-facing (no internals), in SYNCS's voice for asides.
- [ ] **Step 3: Build and check:** `cd ProjectSYNCS && dotnet build` (0 warnings as errors) and `dotnet run --project tests/ProjectSYNCS.Checks` — expect exit 0; `mkdocs build --strict` if MkDocs is installed.
