# Cosmetics new batch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 18 cosmetics (4 of them second basics), retire the baguette magique with a 2 400 refund, and turn « Grand Illusionniste » into « Gentleman ».

**Architecture:** Each cosmetic is one line in `CosmeticCatalog.Build()`. Accessories and auras also need art: layers drawn in `tools/plynling-art/`, exported by `python export.py layers`. Icons come from `tools/item-art/icons.py` (`python export.py`). Rules are pinned in `tests/ProjectSYNCS.Checks`. The baguette leaves through a data migration copied from `RetirePumpkinGraveFrame`.

**Tech Stack:** .NET 10, EF Core + SQLite, Discord.Net; Python 3 + Pillow for art.

**Spec:** `docs/superpowers/specs/2026-11-25-cosmetics-new-batch-design.md`

## Global Constraints

- **Read `docs/agents/economy.md` and `docs/agents/plynling.md` (*Art*) before starting.** Their rules bind this work.
- Cosmetic keys are **append-only**. Never rename or reuse one. `cos.accessory.baguette` is retired for good.
- Rotating prices come from `TierPrice`: C 600, U 1 200, R 2 400, É 3 600, L 5 000. Basics cost 300 (titre, accessoire) or 500 (thème, cadre) through `PriceOf`. Chef-d'œuvre cailloux follow `TrophyPrice`: 200 under 90 forages, 300 for 90–115, 400 above 115.
- User-facing text is French; code, comments and logs are English.
- **Line endings:** files are CRLF. After any `sed -i`, run `unix2dos -q <file>`.
- **Python:** run `python <script>.py` from the script's folder. Never `python3` (it hangs) and never pipe a script into `python`.
- `dotnet build` prints in French: success is « 0 Erreur(s) ».
- **Run the checks after every task:** `dotnet run --project tests/ProjectSYNCS.Checks` must end with `ALL CHECKS PASSED`.
- **No `git commit`.** The owner commits by hand. Where a skill says "commit", stop at a passing check run instead.
- **Version:** one bump for the whole batch (Task 6).

## Review Focus

1. **Baguette in the 2nd or 3rd accessory slot.** After the migration, the Plynling's other accessories keep their order with no empty slot in front of them. Test: Task 1, `RetireWandMigration`.
2. **A baguette holder with no wallet, or with several copies.** They are refunded 2 400 × quantity, into a new wallet if they had none. Test: Task 1.
3. **Eyes clash.** Putting the monocle on a Plynling wearing lunettes rondes replaces them instead of stacking. Test: Task 3.
4. **The busiest shop week.** A winter week with 8 basics, the rotation and the season's items still fits the Boutique tab's caps. Test: Task 6, section 59 run over a winter week.
5. **Aura names starting with a vowel.** The card reads « Entouré d'éclairs » and « Entourée d'étoiles filantes ». Test: Task 5.

---

### Task 1: Retire the baguette magique and rename the collection

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs` (remove the `Accessory("baguette", …)` line)
- Modify: `ProjectSYNCS/Helpers/Economy/AccessoryFit.cs` (remove from `Category`)
- Modify: `ProjectSYNCS/Helpers/Economy/AccessoryStyles.cs` (remove from the `Hand` case)
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCollections.cs`
- Modify: `tools/plynling-art/accessories.py` (remove the `"baguette"` entry from `ACCESSORIES`; keep `BAGUETTE` and the drawing code)
- Delete: `ProjectSYNCS/Assets/Plynlings/acc/*_baguette_*.webp`, `ProjectSYNCS/Assets/Icons/cos.accessory.baguette.png`
- Create: `ProjectSYNCS/Migrations/<timestamp>_RetireMagicWand.cs` (+ Designer)
- Test: `tests/ProjectSYNCS.Checks/AccessoryArtChecks.cs`, `AccessoryFitChecks.cs`, `PaintChecks.cs`
- Docs: `docs/agents/economy.md` (retired list), `CLAUDE.md` and `docs/agents/architecture.md` (« seven data migrations » becomes « eight »)

**Interfaces:**
- Produces: the collection `new CosmeticCollection("Gentleman", …)`, in Grand Illusionniste's place in `All`. It holds `cos.title.mysterieux`, `cos.title.fripon`, `cos.accessory.haut_de_forme`. Later tasks append their keys to it.

- [ ] **Step 1: Write the failing checks.** In `AccessoryArtChecks`:
  - Add `"cos.accessory.baguette"` to `Retired`. `RetiredAreGone` then covers the catalogue, the item and 60 weeks of shop.
  - Add `RetireWandMigration()`, called from `Run`, modelled on `RetireMigrationRefunds`: `migrator.Migrate("AddGuildFeatureSettings")`, then seed:
    - wallet (1, 7) with 100 cailloux, holding 2 baguettes;
    - user 9, holding 1 baguette and no wallet;
    - user 8, holding lunettes;
    - Plynlings through `InsertAtOldSchema`:
      - A wears baguette (slot 1, style `gauche`), ballon `rouge_droite` (slot 2), couronne (slot 3);
      - B wears noeud (slot 1), baguette (slot 2), tulipe `gauche` (slot 3);
      - C is dead and wears noeud (slot 1), fleur (slot 2), baguette (slot 3);
      - D wears lunettes only.

  Then `migrator.Migrate()` and expect:
  - `wallets[7] == 100 + 2 * 2400`;
  - `wallets[9] == 2400` (a new wallet);
  - `wallets[8]` unchanged;
  - `earn.admin` logged `3 * 2400`;
  - no baguette inventory row left, and the lunettes row still there;
  - A wears [ballon `rouge_droite`, couronne, null];
  - B wears [noeud, tulipe `gauche`, null];
  - C wears [noeud, fleur, null];
  - D is unchanged.

  Replace every `"baguette"` sample in `AccessoryFitChecks` and `PaintChecks` with `"canne_peche"`, a held item with sides and the same shape. Keep the expected sides.

- [ ] **Step 2: Run the checks and confirm they fail.** Expect « cos.accessory.baguette is still in the catalogue » and a migration-not-found failure.

- [ ] **Step 3: Remove the baguette from the code.**
  - In the catalogue: its line goes, along with its `AccessoryFit.Category` case and its `AccessoryStyles` case.
  - In `CosmeticCollections`: rename « Grand Illusionniste » to « Gentleman », and remove `cos.accessory.baguette` and `cos.grave.colombes` from it.
  - In the art: remove the `"baguette"` entry from `ACCESSORIES`, delete its layer files and its icon PNG.
  - If `icons.py` has a `cos.accessory.baguette` entry, remove it so the next export doesn't bring the PNG back.

- [ ] **Step 4: Write the migration.**
  - Run `dotnet ef migrations add RetireMagicWand` from `ProjectSYNCS/`. The model is unchanged, so `Up` comes out empty.
  - Fill `Up` by copying `RetirePumpkinGraveFrame`: `Retired = "('cos.accessory.baguette')"`, `Refund = 2400`, the same wallet / log / refund / delete statements.
  - Replace its last statement with these, in order:
    1. Clear the slot: `UPDATE Plynlings SET AccessoryKey = NULL, AccessoryStyle = NULL WHERE AccessoryKey IN {Retired}`. Do the same for `Accessory2Key`/`Accessory2Style` and `Accessory3Key`/`Accessory3Style`.
    2. Close a gap in slot 1: move 2→1 and 3→2 and null slot 3, where `AccessoryKey IS NULL AND Accessory2Key IS NOT NULL`.
    3. Close a gap in slot 2: move 3→2 and null slot 3, where `Accessory2Key IS NULL AND Accessory3Key IS NOT NULL`.
  - `SavedAccessoryStyles` is left alone: a remembered style for an item nobody can wear is never read (builder's call, noted in the spec).
  - `Down` stays empty. The doc comment says what the migration does and why, like its model's.

- [ ] **Step 5: Build and run the checks.** Expect `ALL CHECKS PASSED`. Then run `dotnet ef migrations script AddGuildFeatureSettings RetireMagicWand` and read the SQL once.

- [ ] **Step 6: Update the docs.**
  - `economy.md`: add the baguette to *Retired cosmetics*, with `RetireMagicWand` and the 2 400 refund.
  - `CLAUDE.md` and `architecture.md`: « seven » data migrations becomes « eight », and `RetireMagicWand` joins `architecture.md`'s list.

### Task 2: Titles, thèmes and cadres (no Plynling art)

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs`, `CosmeticCollections.cs`
- Modify: `tools/item-art/icons.py`; export to `ProjectSYNCS/Assets/Icons/`
- Test: `tests/ProjectSYNCS.Checks/CraftingChecks.cs`

**Interfaces:**
- Produces these keys, in each slot's block of `Build()`:

| Key | Name (F for titles) | Source, tier | Emoji | Accent |
|---|---|---|---|---|
| `cos.theme.ciel` | Ciel | `B` | 🌤️ | `0x8EC5E8` |
| `cos.theme.velours` | Velours | `Rot, R` | 🍷 | `0x7A1F3D` |
| `cos.title.gentil` | Le Gentil / La Gentille | `B` | | |
| `cos.title.studieux` | Le Studieux / La Studieuse | `Rot, C` | | |
| `cos.title.distingue` | Le Distingué / La Distinguée | `Rot, U` | | |
| `cos.title.magnifique` | Le Magnifique / La Magnifique | `Rot, L` | | |
| `cos.grave.paquerettes` | Pâquerettes | `B` | 🌼 | |
| `cos.grave.roses` | Roses rouges | `Rot, U` | 🌹 | |

- [ ] **Step 1: Write the failing check.** In `CraftingChecks.Collections()`, expect the Gentleman collection to contain `cos.theme.velours`, `cos.title.distingue`, `cos.title.magnifique` and `cos.grave.roses`. In `Rarities()`, add: `CosmeticCatalog.All.Count(c => c.Source == CosmeticSource.Basic && c.Slot == slot) == 2` for Thème, Titre and Cadre. Accessoire joins this assertion in Task 3, once the café exists.
- [ ] **Step 2: Run the checks and confirm they fail.**
- [ ] **Step 3: Add the 8 catalogue lines** from the table, and append the Gentleman keys to the collection.
- [ ] **Step 4: Draw 16×16 icons** for `cos.theme.ciel`, `cos.theme.velours`, `cos.grave.paquerettes` and `cos.grave.roses` in `icons.py`, in the `themes` and `cadres` groups. Then run `python check.py` and `python export.py themes cadres` from `tools/item-art/`. Titles get no icon: most rotating titles have none.
- [ ] **Step 5: Run the checks.** Expect `ALL CHECKS PASSED`.

### Task 3: Five new accessories (café, lunettes rondes, nœud papillon, monocle, canne)

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs`, `AccessoryFit.cs`, `AccessoryStyles.cs`, `CosmeticCollections.cs`
- Modify: `tools/plynling-art/accessories.py`; run `python export.py layers`
- Modify: `tools/item-art/icons.py`; export the `accessoires` group
- Test: `tests/ProjectSYNCS.Checks/AccessoryFitChecks.cs`, `CraftingChecks.cs`

**Interfaces:**
- Produces:

| Key | Name | Source, tier | Emoji | Category / `accessories.py` family | Styles |
|---|---|---|---|---|---|
| `cos.accessory.cafe` | une tasse de café | `B` | ☕ | Held / `held` | `Hand` |
| `cos.accessory.lunettes_rondes` | des lunettes rondes | `Rot, C` | 👓 | Eyes / `eyes` | none |
| `cos.accessory.noeud_papillon` | un nœud papillon | `Rot, U` | 🎀 | Neck / `neck` | optional colour |
| `cos.accessory.monocle` | un monocle | `Rot, R` | 🧐 | Eyes / `eyes` | none |
| `cos.accessory.canne` | une canne à pommeau | `Rot, E` | 🦯 | Held / `held` | `Hand` |

- The Gentleman collection gains `noeud_papillon`, `monocle` and `canne` (full keys).

- [ ] **Step 1: Write the failing checks.**
  - In `AccessoryFitChecks`: `TryOn` of `monocle` on a Plynling wearing `lunettes_rondes` gives `monocle` with `lunettes_rondes` replaced.
  - In the same file: `TryOn` of `cafe` `droite` over a ballon `gauche` leaves both, one per side.
  - In `CraftingChecks.Rarities()`: extend the two-basics assertion to Accessoire.
- [ ] **Step 2: Run the checks and confirm they fail.**
- [ ] **Step 3: Add the 5 catalogue lines, the `AccessoryFit.Category` cases and the `AccessoryStyles` cases** (café and canne join the `Hand` list), and append the Gentleman keys.
- [ ] **Step 4: Draw the five** in `accessories.py`. Each needs a drawing constant and an `ACCESSORIES` entry whose family matches the table above. Held items use `held_sides(DRAWING[, GRIP])`, eyes ones `{"": fn}` like `lunettes`, neck ones like `medaillon`. Then, from `tools/plynling-art/`, run `python export.py layers`. Check that `StylesMatchTheArt` (section 62) and the family comparison (section 71) pass.
- [ ] **Step 5: Draw the five 16×16 icons** in `icons.py` (`accessoires` group), then run `python check.py` and `python export.py accessoires`.
- [ ] **Step 6: Run the checks.** Expect `ALL CHECKS PASSED`.

### Task 4: The montre à gousset (Chef-d'œuvre)

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs`, `AccessoryFit.cs`, `AccessoryStyles.cs`, `CosmeticCollections.cs`
- Modify: `tools/plynling-art/accessories.py`, `tools/item-art/icons.py`
- Test: `tests/ProjectSYNCS.Checks/CraftingChecks.cs`

**Interfaces:**
- Produces `cos.accessory.montre_gousset` « une montre à gousset », 🕰️, `Cr, trophy: true`, Held / `held`, `Hand` styles, in the Gentleman collection. **Held, not Neck** (builder's call): as a neck item it would clash with the nœud papillon, and the two belong together.

- [ ] **Step 1: Write the failing check.** In `CraftingChecks.Catalog()`, change `Crafted.Count() == 27` to `28`. The Ouvrage count stays 14. Run the checks and confirm they fail.
- [ ] **Step 2: Add the catalogue line.**
  - Start the recipe with Puits treasures only, from `col.piece_ancienne`, `col.cle_rouillee`, `col.bouton`, `col.bille`, `col.bague`, `col.boussole` and `col.carte_tresor`, with one rare or légendaire anchor. Example: `("col.boussole", 1), ("col.piece_ancienne", 3), ("col.cle_rouillee", 2)`.
  - Run the checks: section 61 prints `recipe cos.accessory.montre_gousset: N forages`.
  - Retune until N is between 60 and 150, then set `craftPrice` to `TrophyPrice(N)`. Keep N at least 5 forages away from 90 and 115, so a reshuffled draw doesn't move its price.
- [ ] **Step 3: Add the category, the styles and the collection key.** Draw it in `accessories.py` (`held_sides`) and add its icon. Run `python export.py layers` and `python export.py accessoires`.
- [ ] **Step 4: Run the checks.** Expect `ALL CHECKS PASSED`.

### Task 5: Four auras and the elision on the card

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs`, `CosmeticCollections.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingCardUi.cs:53`
- Modify: `tools/plynling-art/auras.py` (drawings + `AURAS`), `tools/item-art/icons.py` (`auras` group)
- Test: `tests/ProjectSYNCS.Checks/AuraChecks.cs`

**Interfaces:**
- Produces:

| Key | Name | Source, tier | Emoji | `AURAS` key | Collection |
|---|---|---|---|---|---|
| `cos.aura.mouches` | Mouches | `Rot, C` | 🪰 | `mouches` | — |
| `cos.aura.confettis` | Confettis | `Rot, U` | 🎊 | `confettis` | Jour de fête |
| `cos.aura.eclairs` | Éclairs | `Rot, E` | ⚡ | `eclairs` | — |
| `cos.aura.etoiles_filantes` | Étoiles filantes | `Rot, L` | 🌠 | `etoiles_filantes` | Ciel de nuit |

- The Aura rotating pool grows from 2 to 6. `RotatingCount(Aura)` stays 1 (non-goal: the rotation size doesn't change).

- [ ] **Step 1: Write the failing checks** in `AuraChecks`:
  - four `Entry(...)` lines with the table's source, tier, `TierPrice` and name;
  - the aura count goes from `== 6` to `== 10`;
  - the heading of a male `Living(aura: "cos.aura.eclairs")` contains `"Entouré d'éclairs"`;
  - the heading of a female `Living(aura: "cos.aura.etoiles_filantes")` contains `"Entourée d'étoiles filantes"`;
  - the existing « Entouré de bulles » and « Entourée de pétales » lines still pass.
- [ ] **Step 2: Run the checks and confirm they fail.**
- [ ] **Step 3: Add the four catalogue lines and the collection keys.** In `PlynlingCardUi`, choose `"d'"` or `"de "` from the lowered name's first letter, using the same vowel test as `AccessoryNotices.StartsWithVowelSound`. Make that method `internal static` and call it rather than copying it.
- [ ] **Step 4: Draw the four auras** in `auras.py` and add them to `AURAS` in catalogue order. Follow the existing ones for loop timing (8 frames, 125 ms) and the `_tall` canvas. Then run `python export.py layers`.
- [ ] **Step 5: Draw the four 16×16 icons** (`auras` group), then run `python check.py` and `python export.py auras`.
- [ ] **Step 6: Run the checks.** Expect `ALL CHECKS PASSED`. Section 64 reports no missing aura layer.

### Task 6: Measure, docs, version

**Files:**
- Test: `tests/ProjectSYNCS.Checks/CosmeticScreenChecks.cs` (section 59), `ReportChecks.cs` / the wardrobe measure (section 23)
- Modify: `docs/agents/economy.md`, `docs/cosmetics-balance.md`, `guide/cailloux.md` (only if it names counts or items), `ProjectSYNCS/config.yaml`, `ProjectSYNCS/CHANGELOG.md`

- [ ] **Step 1: Measure the busiest week.** Section 59 must measure the Boutique tab on a **winter** week, which has the most seasonal items. If it only measures "now", add one measure at `new DateTimeOffset(2027, 1, 11, 12, 0, 0, TimeSpan.Zero)`. Expect it to stay within 40 components and 25 options per select.
- [ ] **Step 2: Run the checks.** Expect `ALL CHECKS PASSED`, and note the printed component counts.
- [ ] **Step 3: Update `economy.md`.**
  - The count becomes « 117 cosmetics »: 100 + 18 − the baguette.
  - Auras become « Ten: … ».
  - The crafted count becomes 28.
  - Note the two basics per slot.
- [ ] **Step 4: Regenerate `docs/cosmetics-balance.md`** for the new catalogue: counts, rotation odds, cost totals, collections. Bump its snapshot line.
- [ ] **Step 5: Bump the version.**
  - If `git diff ProjectSYNCS/config.yaml` still shows 8.19.0 uncommitted, don't bump; extend the 8.19.0 changelog entry. Otherwise bump to **8.20.0** and add a new entry.
  - Changelog bullets: the 18 new cosmetics (by tier), two basics per slot, the baguette retired with a 2 400 refund, the Gentleman collection, the « d' » fix.
  - Say the final version in the summary.
- [ ] **Step 6: Final run.** `dotnet build` in `ProjectSYNCS/` shows « 0 Erreur(s) », and the checks end with `ALL CHECKS PASSED`.
