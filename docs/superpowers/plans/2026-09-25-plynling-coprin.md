# The Coprin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the Coprin, a second rare mushroom Plynling whose ink grows with hunger, with adult and baby art.

**Architecture:** The Coprin is data plus art like every species: an appended `PlynlingSpecies` value, a catalog row sharing the rare slot with the Mystique, an art key, and two drawings (`coprin`, `coprin_baby`) in `tools/plynling-art/species.py` routed through the shared `finish`/`motion.py` machinery. Its idle touch is a vertical ink drop (`Pose.drip`).

**Tech Stack:** .NET 10 / Discord.Net 3.20, Python 3.12 + Pillow 12, scratch harnesses in the session scratchpad.

Spec: `docs/superpowers/specs/2026-09-25-plynling-coprin-design.md`.

## Global Constraints

- Rarity: **Mystique 27 → 14, Coprin 13**; the mushroom family still totals **300**.
- Card name **« Coprin »**, accent **`0x5A5A70`**, tier rare, sparkles silver-white.
- `PlynlingSpecies.Coprin` is **appended after `Solaire`** — never inserted (the enum is stored as an int).
- Ink by mood — adult: happy/content/sad thin rim, hungry thicker, starving melting drips, frozen frozen drips. Baby: no ink at rest, rim when hungry, melting when starving, frozen drips when frozen; no idle drip.
- Nothing moves side to side; frame 0 is the still. No existing art file changes; no art version bump.
- Commit, never push (the owner pushes). Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; a version bump is its own "Updated version" commit.

## Paths

`$REPO` = `D:\Sources\Discord Bots\ProjectSYNCS`; `$PY` = `C:\Users\rmeli\AppData\Local\Programs\Python\Python312\python.exe`; `$SCRATCH` = the session scratchpad (`artcheck/`, `stagecheck/`, `animcheck.py`, `preview.py`, `babypreview.py`).

---

### Task 1: The Coprin's art (prototype + owner review)

**Files:**
- Modify: `tools/plynling-art/common.py` (`SPECIES`), `tools/plynling-art/motion.py` (`Pose`), `tools/plynling-art/species.py` (new section before `DRAW`, and `DRAW`), `tools/plynling-art/memorials.py:19` (`accent`)
- Test: `$SCRATCH/preview.py`, `$SCRATCH/coprinpreview.py`

**Interfaces:**
- Produces: `SPECIES["coprin"]`; `Pose.drip` (the frame number, or `None`); `species.coprin(state, frame=0, shadow=True, stage="adult")`; `DRAW["coprin"]`.

- [ ] **Step 1: The palette** — `common.py`, after the `"dore"` entry in `SPECIES`:

```python
    "coprin":   dict(tier="rare", cap=[(255, 255, 255), (246, 244, 240), (226, 222, 216), (190, 186, 182), (120, 116, 118)],
                     spot=((224, 198, 162), (176, 144, 108)), spots=SCALES, gill=(46, 40, 54), sparkle=(236, 236, 250)),
```

(`spot` is the brown skullcap and the tint of the scales; `gill` is the ink.)

- [ ] **Step 2: The drip's clock** — `motion.py`, after the `self.ripple = …` line:

```python
        # the Coprin's ink drop: forms at the rim, falls straight down, splashes (see coprin())
        self.drip = f if (sp == "coprin" and self.touch) else None
```

- [ ] **Step 3: The drawings** — `species.py`, insert before `DRAW = {`:

```python
# ---- Coprin: a shaggy ink cap ---------------------------------------------------------------

# How much of the cap has turned to ink, by mood: the hungrier, the more it melts. A baby is
# too young to melt until hunger makes it.
COPRIN_INK = {"happy": 1, "content": 1, "sad": 1, "hungry": 2, "starving": 3, "frozen": "frozen"}
COPRIN_BABY_INK = {"happy": 0, "content": 0, "sad": 0, "hungry": 1, "starving": 3, "frozen": "frozen"}
DROP, DROP_SHINE = (86, 78, 104), (170, 160, 196)      # lighter than the ink, to read on a dark theme


def coprin_cap(g, p, apex, rim, half, egg=False):
    """A tall white cap, taller than wide, with a brown skullcap and little upturned scales.
    An egg (the baby) closes in at the bottom instead of flaring."""
    cap, tip = p["cap"], p["spot"]
    for y in range(apex, rim + 1):
        h = half * min(1.0, math.sqrt((y + 0.5 - apex) / 4.5))
        if egg:
            h = min(h, half * math.sqrt(min(1.0, (rim + 1 - y) / 3.0)))
        elif y >= rim - 1:
            h += 0.6                                                   # the slight flare of the bell
        for x in range(N):
            dx = x + 0.5 - CX
            if abs(dx) > h:
                continue
            u = dx / h
            c = cap[0] if u < -0.4 else cap[1] if u < 0.3 else cap[2] if u < 0.72 else cap[3]
            if y < apex + 3:
                c = tip[0] if u < 0.3 else tip[1]                      # the skullcap
            elif y == apex + 3 and x % 2 == 0:
                c = tip[1]                                             # its ragged edge
            g.put(x, y, c, "cap")
    for row, y in enumerate(range(apex + 5, rim - 1, 2)):              # the shag, in staggered rows
        for x in range(1, N - 1):
            if (x + row * 2) % 4 == 0 and g.r[y][x - 1] == g.r[y][x] == g.r[y][x + 1] == "cap":
                g.put(x, y, lerp(g.c[y][x], tip[1], 0.5))
                g.put(x + 1, y - 1, lerp(g.c[y - 1][x + 1], tip[1], 0.25))


def ink_rim(g, p, rim, level, drips):
    """The melting edge: the bottom rows of the cap turned to ink (the very edge darkest), the
    row above greying into it, and drips hanging from the rim."""
    if level == 0:
        return
    ink = p["gill"]
    deep = lerp(ink, (0, 0, 0), 0.45)
    rows = 1 if level == "frozen" else level
    for x in range(N):
        for y in range(rim - rows + 1, rim + 1):
            if g.r[y][x] == "cap":
                g.put(x, y, deep if y == rim else ink)
        y = rim - rows
        if g.r[y][x] == "cap":
            g.put(x, y, lerp(g.c[y][x], ink, 0.45))
    for x, length in drips:
        for k in range(1, length + 1):
            g.put(x, rim + k, deep if k == length else ink, "cap")


def ink_drop(im, pose, rim, x):
    """The idle drop: a bead at the rim, stretching, falling straight down, a splash."""
    f = pose.drip
    if f is None:
        return

    def px(xx, yy, c):
        if 0 <= xx < N and 0 <= yy < N:
            im.putpixel((xx, yy), c + (255,))
    top = rim + 1 + pose.body[1]                                       # hangs from the (breathing) rim
    if f <= 5:
        px(x, top, DROP)
    elif f <= 7:
        px(x, top, DROP)
        px(x, top + 1, DROP)
    elif f <= 13:
        y = min(29, rim + 3 + (f - 8) * 2)                             # falling, free of the breath
        px(x, y, DROP_SHINE)
        px(x, y + 1, DROP)
    elif f == 14:
        for dx in (-1, 0, 1):
            px(x + dx, 29, DROP)                                       # the splash


def coprin(state, frame=0, shadow=True, stage="adult"):
    """A shaggy ink cap: a tall white cap with a brown skullcap on a slim stem. Its rim is ink,
    and the hungrier it gets the more it melts."""
    if stage == "baby":
        return coprin_baby(state, frame, shadow)
    p = SPECIES["coprin"]
    pose = pose_for("coprin", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    stem(g, STEM, 11, 20, 14 + sag, 27)
    feet(g, STEM)
    apex, rim = 1 + sag, 16 + sag
    coprin_cap(g, p, apex, rim, 7.6)
    drips = {3: [(8, 3), (10, 2), (21, 2), (23, 4)], "frozen": [(9, 3), (22, 2)]}.get(COPRIN_INK[state], [])
    ink_rim(g, p, rim, COPRIN_INK[state], drips)
    outline(g, p["cap"])
    im = finish(g, p, state, STEM, face_oy=1, shadow=shadow, pose=pose)
    ink_drop(im, pose, rim, 9)
    return im


def coprin_baby(state, frame, shadow):
    """The young Coprin: a small closed white egg with a brown tip — no ink until it is hungry,
    and no idle drip."""
    p = SPECIES["coprin"]
    pose = pose_for("coprin", state, frame)
    sag = 1 if state == "starving" else 0
    g = Grid()
    baby_body(g, STEM)
    apex, rim = 7 + sag, 18 + sag
    coprin_cap(g, p, apex, rim, 6.2, egg=True)
    level = COPRIN_BABY_INK[state]
    drips = {3: [(11, 2), (20, 3)], "frozen": [(11, 2), (20, 2)]}.get(level, [])
    ink_rim(g, p, rim, level, drips)
    outline(g, p["cap"])
    return finish(g, p, state, STEM, face_oy=BABY_FACE_OY, shadow=shadow, pose=pose)


```

and add `"coprin": coprin` at the end of the `DRAW` dict.

- [ ] **Step 4: The memorials' accent** — `memorials.py:19`, add `"coprin": p["gill"]` to the mapping (its memorials carry its ink).

- [ ] **Step 5: The existing art is untouched** — Run: `$PY "$SCRATCH/preview.py" cepe` → first line `frame 0 == still: all 36 OK` (the check reads the baseline for the six existing species; the Coprin has no baseline and is skipped by name if needed).

- [ ] **Step 6: Preview** — `$SCRATCH/coprinpreview.py` renders an animated GIF with two rows (adult, baby) × six moods and a frame strip for the adult (same pattern as `stagepreview.py`, `build(m, "coprin", f, stage=st)` for `st in ("adult", "baby")`). Read the still: the cap reads as a shaggy ink cap, the ink level differs per mood as the table says, the drop is visible on the dark background and falls straight down, the face sits on the stem.

- [ ] **Step 7: Owner review** — send the GIF and the still; iterate Steps 3–6 until approved. Nothing is exported.

- [ ] **Step 8: Commit**

```bash
git add tools/plynling-art/common.py tools/plynling-art/motion.py tools/plynling-art/species.py tools/plynling-art/memorials.py
git commit -m "Drew the Coprin" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: The Coprin in the bot, exported

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs:6-10`, `ProjectSYNCS/Helpers/PlynlingCatalog.cs` (`Species`), `ProjectSYNCS/Helpers/PlynlingArt.cs` (`StagedSpecies`, `Key`), `tools/plynling-art/export.py` (`STAGED`)
- Test: `$SCRATCH/stagecheck/Program.cs`, `$SCRATCH/artcheck/Program.cs`, `$SCRATCH/animcheck.py`
- Create (generated): 17 files `assets/plynlings/*coprin*`

**Interfaces:**
- Consumes: `DRAW["coprin"]`, `SPECIES["coprin"]` (Task 1).
- Produces: `PlynlingSpecies.Coprin`; `PlynlingArt.Key(PlynlingSpecies.Coprin) == "coprin"`.

- [ ] **Step 1: Snapshot** — `rm -rf "$SCRATCH/v3_before" && cp -r "$REPO/assets/plynlings" "$SCRATCH/v3_before"` (106 files).

- [ ] **Step 2: Failing odds check** — append to `$SCRATCH/stagecheck/Program.cs`, before the final `Console.WriteLine`:

```csharp
Check("mushroom weights still total 300", PlynlingCatalog.TotalWeight(PlynlingFamily.Mushroom) == 300);
Check("Mystique weight 14", PlynlingCatalog.Info(PlynlingSpecies.Mystique).Weight == 14);
Check("Coprin weight 13, rare, « Coprin »", PlynlingCatalog.Info(PlynlingSpecies.Coprin) is { Weight: 13, Rarity: PlynlingRarity.Rare, Name: "Coprin", Family: PlynlingFamily.Mushroom });
Check("roll 277 is the Mystique", PlynlingCatalog.PickSpecies(PlynlingFamily.Mushroom, 277) == PlynlingSpecies.Mystique);
Check("roll 278 is the Coprin", PlynlingCatalog.PickSpecies(PlynlingFamily.Mushroom, 278) == PlynlingSpecies.Coprin);
Check("roll 290 is the Coprin", PlynlingCatalog.PickSpecies(PlynlingFamily.Mushroom, 290) == PlynlingSpecies.Coprin);
Check("roll 291 is the Doré", PlynlingCatalog.PickSpecies(PlynlingFamily.Mushroom, 291) == PlynlingSpecies.Dore);
Check("Coprin is appended, after Solaire", (int)PlynlingSpecies.Coprin == 12);
```

Run: `dotnet run --project "$SCRATCH/stagecheck"` → build error (`PlynlingSpecies.Coprin` missing).

- [ ] **Step 3: Implement**

`Plynling.cs`, the enum body becomes:

```csharp
    Amanite, Cepe, Rose, Russule, Mystique, Dore,                   // mushrooms
    Tournesol, Citron, Roux, Ivoire, Nocturne, Solaire,             // sunflowers
    Coprin,                                                         // a mushroom, appended later
```

`PlynlingCatalog.cs`: change the Mystique row's weight `27` → `14`; insert after it

```csharp
        new SpeciesInfo(PlynlingSpecies.Coprin,    PlynlingFamily.Mushroom,  "Coprin",             PlynlingRarity.Rare,      13, 0x5A5A70),
```

and replace the ladder comment's last sentence with: `Every family uses this ladder; a tier may be shared (the mushrooms' 27 rare points are the Mystique's 14 and the Coprin's 13).`

`PlynlingArt.cs`: add `PlynlingSpecies.Coprin` to `StagedSpecies`; add `PlynlingSpecies.Coprin => "coprin",` to `Key` before the throwing arm.

`export.py`: add `"coprin"` to `STAGED`.

Run: `dotnet build "$REPO/ProjectSYNCS" -warnaserror` → 0 errors; `dotnet run --project "$SCRATCH/stagecheck"` → `0 failed`.

- [ ] **Step 4: Export** — `cd "$REPO/tools/plynling-art" && $PY export.py` → `123 files written`; `git status --short assets/plynlings` shows 17 `??`, no `M`.

- [ ] **Step 5: Checks** — in `$SCRATCH/artcheck/Program.cs` change `sprites.Count == 72` → `84` and `stills.Count == 34` → `39`. Run `dotnet run --project "$SCRATCH/artcheck"` → `0 failed`; `$PY "$SCRATCH/animcheck.py"` → `0 failed` (the adult "v2 still" comparison must skip species with no v2 file: guard it with `os.path.exists`).

- [ ] **Step 6: Commit**

```bash
git add ProjectSYNCS/Models/Plynling.cs ProjectSYNCS/Helpers/PlynlingCatalog.cs ProjectSYNCS/Helpers/PlynlingArt.cs tools/plynling-art/export.py assets/plynlings
git commit -m "Added the Coprin, a second rare Plynling" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Docs and version

- [ ] **Step 1:** `README.md:205` → `Its species is rolled: three common, one uncommon, two rares — the Mystique and the Coprin — and a legendary Doré.`
- [ ] **Step 2:** `tools/plynling-art/README.md`: in the `species.py` bullet add "a Coprin (shaggy ink cap) whose rim turns to ink — more the hungrier it is (`COPRIN_INK`), with a falling ink drop as its idle touch"; `76`/`106` counts → `123` files, `72` → `84` living sprites.
- [ ] **Step 3:** Build `-warnaserror`; commit `Documented the Coprin`.
- [ ] **Step 4:** `ProjectSYNCS/config.yaml` version +0.0.1; commit `Updated version`.
