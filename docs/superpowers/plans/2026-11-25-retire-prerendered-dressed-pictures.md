# Retire the pre-rendered dressed pictures — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete the 30 492 pre-rendered `_with_` / `_aura_` pictures from `assets/plynlings/` and everything that writes, checks or describes them.

**Architecture:** The bot already draws dressed pictures from the layers (`PlynlingPainter`); only checks section 70, `tools/plynling-art/export.py` and the docs still depend on the old files. Remove the dependency in the checks first, then the exporter, then the files and the words.

**Tech Stack:** .NET 10 console checks (`tests/ProjectSYNCS.Checks`), Python/PIL art tools, git.

**Spec:** `docs/superpowers/specs/2026-11-25-retire-prerendered-dressed-pictures-design.md`

## Global Constraints

- **No history rewrite**, no force-push; nothing is committed — the owner commits by hand.
- **No version bump**: `ProjectSYNCS/config.yaml` and `CHANGELOG.md` stay untouched (the bot's only edits are two comments).
- Keep in `assets/plynlings/`: every bare, `_dirty`, `_visit`, memorial and food file, the old Coprin v5 files — **359 files** remain.
- Every file touched here has CRLF line endings: edit with the Edit tool, never `sed -i` (it strips CRLF).
- Old specs and plans under `docs/superpowers/` stay as written (history).
- Don't run `python export.py` (the plain run deletes and rewrites the whole folder; not needed here).

## Review Focus

1. **The visit check still has its references**: `VisitPictures` compares against bare `plynling_<shape>_<face>_visit_v<n>.webp` files — the deletion glob must not catch them (`_visit` contains neither `_with_` nor `_aura_`; Task 3 asserts the kept count).
2. **Coprin's two versions**: its bare v6 files and old v5 bare files both stay; only its `_with_`/`_aura_` v5 files go (covered by the 359 count).
3. **`LookNamesRoundTrip` loses no coverage of the seasonal auras**: it switches from the three pre-rendered auras to every catalogue aura (Task 1 asserts six).
4. **`export.py redraw coprin` still writes the bare files** at version 6 after `ONLY_EXISTING` goes (Task 2 checks by reading the code path; no run needed).
5. **No stray mention left** that would send a future reader to regenerate the files (Task 3's grep).

---

### Task 1: Checks section 70 stops reading the pre-rendered files

**Files:**
- Modify: `tests/ProjectSYNCS.Checks/PaintChecks.cs`
- Modify: `tests/ProjectSYNCS.Checks/Program.cs:150-152`

**Interfaces:**
- Produces: `public static void Run(Action<string> fail)` (the `args` parameter goes: only the `-- art` sweep read it).

- [ ] **Step 1: Make `LookNamesRoundTrip` use every aura and assert it**

Replace `PrerenderedAuras` and `Auras()` with one helper over the whole catalogue:

```csharp
private static string[] Auras() => CosmeticCatalog.All.Where(c => c.Slot == CosmeticSlot.Aura)
    .Select(c => c.Key[AuraPrefix.Length..]).ToArray();
```

and add in `Run`, before `LookNamesRoundTrip()`:

```csharp
Expect(Auras().Length == 6, $"{Auras().Length} auras in the catalogue, expected 6");
```

- [ ] **Step 2: Remove the sweep**

Delete `SinglesMatchToday`, `Sample`, `InSample`, `AllShapes`, and the `art` / `SinglesMatchToday` lines in `Run`. Change `Run(Action<string> fail, string[] args)` to `Run(Action<string> fail)`. Then delete whatever the compiler shows is now unreferenced among `AuraFaces`, `DirtyFaces`, `AccessoryPrefix`, and the `System.Diagnostics` using (`Stopwatch` was only in the sweep; `ConcurrentQueue`/`Parallel` are still used further down — keep them). Keep `TodaysLooks`, `Differs`, `Ticks`, `VisitPictures(old)` and the `old` folder variable (the bare `_visit` files stay).

- [ ] **Step 3: Rewrite the comments**

- File header: drop « every single-accessory picture … against today's pre-rendered file in assets/plynlings/ » and the `-- art` usage block; it now lists: look names round-trip, the marks repaint changes nothing on its own, missing art is null, the cache hits, the visit canvas lines up with today's bare `_visit` files, and `FilesFor` finds a card's pictures.
- `TodaysLooks`: « Every (accessory key, colour/place style) pair, sides left out; no-style accessories as a single null. »
- `Program.cs:150-152`: drop the `-- art` comment line; call `PaintChecks.Run(Fail);`.

- [ ] **Step 4: Build and run the checks**

Run: `dotnet run --project tests/ProjectSYNCS.Checks`
Expected: exit 0, last line `ALL CHECKS PASSED`, no `paint:` failure.

---

### Task 2: The exporter stops writing dressed pictures

**Files:**
- Modify: `tools/plynling-art/export.py`
- Modify: `tools/plynling-art/auras.py:237`

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces: `export.py` modes `(none)`, `layers`, `redraw <species>` only.

- [ ] **Step 1: Remove the dressed exports**

In `export.py` delete `export_accessories` and `export_auras`, their two calls in `main()`, the `auras` and `accessories <name>…` branches of `__main__`, the dressed half of the `redraw` branch (it keeps `bare = export_bare(sp)` and prints « {bare} bare files written to {OUT}; now run `python export.py layers` »), `ONLY_EXISTING` (line 68, the guard in `save_loop`, the redraw assignment), and the imports that end up unused: `file_style`, `tall`, `PRERENDERED_AURAS`, `surround`. Keep `wear`, `loop`, `looks`, `AURA_STATES`, `AURAS`, `_Spray`, `_bounds`, `HAT_AT`, `HAT_CANVAS` (used by `export_layers`).

- [ ] **Step 2: Rewrite the module docstring**

Drop the two paragraphs on `_with_` and `_aura_` files and the `auras` / `accessories` usage lines. Add one line: « Dressed pictures are not exported: the bot draws them from the layers. The pre-rendered `_with_` / `_aura_` files were retired on 2026-11-25. » Redraw usage line becomes « its new bare files; then re-export the layers ».

- [ ] **Step 3: Remove `PRERENDERED_AURAS` from `auras.py`**

Delete line 237 and any comment above it that only explains it.

- [ ] **Step 4: Verify it still parses and nothing references the removed names**

Run: `python -c "import ast,sys; [ast.parse(open(f,encoding='utf-8').read()) for f in sys.argv[1:]]; print('ok')" tools/plynling-art/export.py tools/plynling-art/auras.py`
Expected: `ok` (don't import the module — `export.py` runs nothing on import, but PIL loads are slow).
Run: `git grep -n -E "PRERENDERED_AURAS|export_accessories|export_auras|ONLY_EXISTING" -- tools`
Expected: no output.

---

### Task 3: Delete the files and update the words

**Files:**
- Delete: `assets/plynlings/*_with_*`, `assets/plynlings/*_aura_*` (30 492 files)
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingArt.cs:50-53`, `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs:425-426`
- Modify: `docs/agents/plynling.md` (*Art*, lines ~371-407), `docs/agents/economy.md:405-408`, `tools/plynling-art/README.md` (~67, 79, 93-102, 116-118), `CLAUDE.md:60`

- [ ] **Step 1: Delete the files**

Run: `git rm -q -- "assets/plynlings/*_with_*" "assets/plynlings/*_aura_*"`
Then: `ls assets/plynlings | wc -l` → **359**; `ls assets/plynlings | grep -c _visit_` → unchanged from before (count it before deleting).

- [ ] **Step 2: Bot comments**

- `PlynlingArt.cs`: end the `Sprite` comment at « A dressed Plynling is drawn by the bot instead (LookOf, PlynlingPainter). »
- `CosmeticCatalog.cs`: « Drawn by the bot from the layers alone, like every aura. »

- [ ] **Step 3: Docs**

- `plynling.md` *Art*: in the « Dressed pictures are drawn at runtime » item, drop the two sentences on checks reading the old folder and `-- art`, and replace « The pre-rendered `_with_`/`_aura_` files … stay … The two items below describe those files » with « The pre-rendered `_with_`/`_aura_` files were retired on 2026-11-25: posted messages from 8.5.0–8.8.x may show a broken image. » Delete the two following items (« The worn accessory is drawn on the picture (8.5.0) » and « The worn aura is drawn too (8.6.0) ») but keep their still-true rules elsewhere in that item: angry is never dressed; the aura shows only on `PlynlingArt.AuraMoods` faces (not frozen, sick, angry); an unknown accessory key falls back to plain, an unknown style to the default (`AccessoryStyles.Resolve`); a hat on an adult is the 40 px canvas; to redraw one species prefer `REDRAWN` over a `Version` bump. Keep the 8.16.1 sentence on visits and event cards.
- `economy.md`: « A new aura costs ~126 layer files »: drop « and no pre-rendered ones (the seasonal three were the first) » and « (never to `PRERENDERED_AURAS`) ». Add after it: « A new accessory or style likewise costs only its layers: draw it in `accessories.py`, `python export.py layers`. »
- `tools/plynling-art/README.md`: remove the pre-rendered file names, counts and the `auras` / `accessories` commands; the redraw paragraph keeps only « new bare files under its own version; re-export the layers ». `file_style`'s sentence stays only if `accessories.py` sheets still use it (they do) — reword « keeps the old file names » to « names a style without its default side ».
- `CLAUDE.md:60`: drop « ; `-- art` runs the slow pixel-exact sweep of the dressed pictures ».

- [ ] **Step 4: Grep for leftovers**

Run: `git grep -n -E "_with_|_aura_|PRERENDERED|pre-render|-- art" -- . ":!docs/superpowers" ":!ProjectSYNCS/Stories"`
Expected: only lines that say the files were retired, `PlynlingLook`/painter naming that is not a filename pattern, or none. Anything telling a reader to export or keep them is a miss.

---

### Task 4: Final verification

- [ ] **Step 1:** `cd ProjectSYNCS; dotnet build -warnaserror` → `0 Warning(s)`, `0 Error(s)` (output may be in French: « 0 Avertissement(s) », « 0 Erreur(s) »).
- [ ] **Step 2:** `dotnet run --project tests/ProjectSYNCS.Checks` → exit 0, `ALL CHECKS PASSED`.
- [ ] **Step 3:** `git status --short | grep -v "^D  assets/plynlings/"` lists only the files named in Tasks 1–3 plus this plan; `git diff --cached --stat | tail -1` shows 30 492 deletions under `assets/plynlings/`. `ProjectSYNCS/config.yaml` unchanged.
- [ ] **Step 4:** Report to the owner: files removed, what changed, nothing committed.
