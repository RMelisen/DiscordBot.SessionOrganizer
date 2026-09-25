# Plynling Life Stages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Plynlings grow through four cosmetic life stages (bébé, ado, adulte, ancien), labelled on every card, with the Cèpe drawn at each stage.

**Architecture:** The stage is derived from `PlynlingLife.Age` (time actually lived) and never stored. `PlynlingArt.Sprite` gains a stage and falls back to the adult file for species without stage art (`StagedSpecies`). The art pipeline draws three new Cèpe bodies through the same `finish`/`motion.py` machinery, exported as 18 new animated WebPs next to the untouched v3 files.

**Tech Stack:** .NET 10 / Discord.Net 3.20 (bot), Python 3.12 + Pillow 12 (art, `tools/plynling-art`), scratch console harnesses referencing the bot project (no test project exists).

Spec: `docs/superpowers/specs/2026-09-25-plynling-life-stages-design.md`.

## Global Constraints

- Thresholds, in time actually lived: bébé **< 2 days**, ado **< 14 days**, adulte **< 180 days**, ancien **≥ 180 days**.
- **Looks only**: hunger, happiness, food, freezing and death are untouched.
- Art is **body shape only** — no props. Nothing moves side to side; frame 0 of every loop is the still.
- The adult Cèpe and all 70 existing files in `assets/plynlings/` stay **byte-identical**. No art version bump: `ART_VERSION` and `PlynlingArt.Version` stay **3**.
- Every species shows the stage label; only species in `StagedSpecies` (= `STAGED` in `export.py`, `{"cepe"}`) get stage art.
- No announcement of a stage change.
- User-facing strings French, code/comments English. Build with `-warnaserror`.
- Commit, never push (the owner pushes). Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. A version bump is its own "Updated version" commit.

## Paths

- `$REPO` = `D:\Sources\Discord Bots\ProjectSYNCS`
- `$PY` = `C:\Users\rmeli\AppData\Local\Programs\Python\Python312\python.exe`
- `$SCRATCH` = the session scratchpad. Existing harnesses there: `artcheck/` (C#), `cardcheck/` (C#, reused as `stagecheck`), `animcheck.py`, `preview.py`, `v2/` (the shipped v2 stills).

## File Structure

| File | Change |
|---|---|
| `ProjectSYNCS/Helpers/PlynlingCatalog.cs` | add `enum PlynlingStage` beside `PlynlingMood` |
| `ProjectSYNCS/Helpers/PlynlingLife.cs` | add `Stage(p, now)` |
| `ProjectSYNCS/Helpers/PlynlingCardUi.cs` | add `StageLabel`; heading line 3 names the stage |
| `ProjectSYNCS/Helpers/PlynlingArt.cs` | `StagedSpecies`; `Sprite(species, stage, mood)` |
| `ProjectSYNCS/Commands/PlynlingModule.cs` | card thumbnail passes the stage; help line |
| `ProjectSYNCS/Services/PlynlingAnnouncer.cs` | resurrection post passes the stage |
| `tools/plynling-art/species.py` | `CEPE_STAGES`, `cepe_stage()`, `cepe(..., stage)` |
| `tools/plynling-art/sprites.py` | `build(..., stage)` |
| `tools/plynling-art/export.py` | `STAGED`, stage filenames |
| `README.md`, `tools/plynling-art/README.md`, `CLAUDE.md`, `ProjectSYNCS/config.yaml` | docs, version |

---

### Task 1: The stage rule and its label

**Files:**
- Modify: `ProjectSYNCS/Helpers/PlynlingCatalog.cs:8`
- Modify: `ProjectSYNCS/Helpers/PlynlingLife.cs` (after `Age`)
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs:18-28` (`Heading`), and after `MoodLabel`
- Test: `$SCRATCH/stagecheck/` (copy of `$SCRATCH/cardcheck/`)

**Interfaces:**
- Produces: `enum PlynlingStage { Baby, Teen, Adult, Elder }` (namespace `ProjectSYNCS.Helpers`); `PlynlingLife.Stage(Plynling p, DateTimeOffset now) : PlynlingStage`; `PlynlingCardUi.StageLabel(PlynlingStage stage, PlynlingGender gender) : string`.

- [ ] **Step 1: Write the failing check**

```bash
cp -r "$SCRATCH/cardcheck" "$SCRATCH/stagecheck" && mv "$SCRATCH/stagecheck/cardcheck.csproj" "$SCRATCH/stagecheck/stagecheck.csproj"
```

`$SCRATCH/stagecheck/Program.cs`:

```csharp
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

int pass = 0, fail = 0;
void Check(string label, bool ok) { if (ok) pass++; else { fail++; Console.WriteLine("  FAIL  " + label); } }

var t0 = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
Plynling Fresh(PlynlingGender g = PlynlingGender.Male) => PlynlingLife.Create(1, 42, "Pouf", PlynlingSpecies.Cepe, g, t0);
PlynlingStage At(double days) => PlynlingLife.Stage(Fresh(), t0.AddDays(days));

Check("0 d is bébé", At(0) == PlynlingStage.Baby);
Check("just under 2 d is bébé", At(2 - 1e-6) == PlynlingStage.Baby);
Check("2 d is ado", At(2) == PlynlingStage.Teen);
Check("just under 14 d is ado", At(14 - 1e-6) == PlynlingStage.Teen);
Check("14 d is adulte", At(14) == PlynlingStage.Adult);
Check("just under 180 d is adulte", At(180 - 1e-6) == PlynlingStage.Adult);
Check("180 d is ancien", At(180) == PlynlingStage.Elder);

// frozen time does not age it: frozen after 1 day, looked at 30 days later
var frozen = Fresh();
PlynlingLife.Freeze(frozen, t0.AddDays(1), byStaff: true);
Check("a frozen Plynling does not grow up", PlynlingLife.Stage(frozen, t0.AddDays(30)) == PlynlingStage.Baby);

Check("bébé", PlynlingCardUi.StageLabel(PlynlingStage.Baby, PlynlingGender.Female) == "bébé");
Check("ado", PlynlingCardUi.StageLabel(PlynlingStage.Teen, PlynlingGender.Male) == "ado");
Check("adulte", PlynlingCardUi.StageLabel(PlynlingStage.Adult, PlynlingGender.Female) == "adulte");
Check("ancien", PlynlingCardUi.StageLabel(PlynlingStage.Elder, PlynlingGender.Male) == "ancien");
Check("ancienne", PlynlingCardUi.StageLabel(PlynlingStage.Elder, PlynlingGender.Female) == "ancienne");

var heading = PlynlingCardUi.Heading(Fresh(PlynlingGender.Female), t0.AddHours(5));
Check("the heading names the stage beside the age", heading.Contains(" · bébé · âgée de "));

Console.WriteLine($"{pass} passed, {fail} failed");
if (fail > 0) Environment.Exit(1);
```

- [ ] **Step 2: Run it — expect a build failure**

Run: `dotnet run --project "$SCRATCH/stagecheck"`
Expected: build errors — `PlynlingStage`, `PlynlingLife.Stage`, `PlynlingCardUi.StageLabel` do not exist.

- [ ] **Step 3: Implement**

`PlynlingCatalog.cs`, replace `public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen }` with:

```csharp
public enum PlynlingMood { Content, Happy, Sad, Hungry, Starving, Frozen }

// Derived from age by PlynlingLife.Stage and never stored, so — unlike PlynlingSpecies —
// this is free to be reordered. The lowercase name is the art filename segment.
public enum PlynlingStage { Baby, Teen, Adult, Elder }
```

`PlynlingLife.cs`, directly after the `Age` method:

```csharp
    // Cosmetic only: the card's label and, for staged species, the picture. Measured on
    // time actually lived, so a frozen Plynling does not grow up. 6 months = 180 days,
    // a month taken as 30 days like the memorial tiers.
    public static PlynlingStage Stage(Plynling p, DateTimeOffset now) => Age(p, now).TotalDays switch
    {
        < 2 => PlynlingStage.Baby,
        < 14 => PlynlingStage.Teen,
        < 180 => PlynlingStage.Adult,
        _ => PlynlingStage.Elder,
    };
```

`PlynlingCardUi.cs`, in `Heading`, replace
`$"à <@{p.OwnerId}> · {p.Gender.Agree("âgé", "âgée")} de {age}";` with
`$"à <@{p.OwnerId}> · {StageLabel(PlynlingLife.Stage(p, now), p.Gender)} · {p.Gender.Agree("âgé", "âgée")} de {age}";`

and after `MoodLabel`:

```csharp
    public static string StageLabel(PlynlingStage stage, PlynlingGender gender) => stage switch
    {
        PlynlingStage.Baby => "bébé",
        PlynlingStage.Teen => "ado",
        PlynlingStage.Elder => gender.Agree("ancien", "ancienne"),
        _ => "adulte",
    };
```

- [ ] **Step 4: Run it — expect a pass**

Run: `dotnet run --project "$SCRATCH/stagecheck"` → `14 passed, 0 failed`.
Run: `dotnet build "$REPO/ProjectSYNCS" -warnaserror` → `0 Error(s)`.

- [ ] **Step 5: Mutation-check** — change `< 14 =>` to `<= 14 =>`, rerun: "14 d is adulte" must fail. Restore byte-identical (`git diff` shows only the Step 3 edits).

- [ ] **Step 6: Commit**

```bash
git add ProjectSYNCS/Helpers/PlynlingCatalog.cs ProjectSYNCS/Helpers/PlynlingLife.cs ProjectSYNCS/Helpers/PlynlingCardUi.cs
git commit -m "Added Plynling life stages to the card" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: The Cèpe at every stage (art prototype + owner review)

**Files:**
- Modify: `tools/plynling-art/species.py` (the Cèpe section; `DRAW` unchanged)
- Modify: `tools/plynling-art/sprites.py:14-18` (`build`)
- Test: `$SCRATCH/stagepreview.py`, `$SCRATCH/preview.py` (frame-0 check)

**Interfaces:**
- Consumes: `motion.pose_for`, `finish(..., pose=, original=)`, `stem`, `feet`, `gills`, `shade_cap` (all existing in `species.py`).
- Produces: `sprites.build(state, sp, frame=0, shadow=True, stage="adult")`; `species.cepe(state, frame=0, shadow=True, stage="adult")`; stage names `"baby"`, `"teen"`, `"adult"`, `"elder"`.

The numbers below are the **starting proposal**; the owner reviews previews and they are tuned until approved. The structure (adult path untouched, stages through `finish` with `original=False`) is fixed.

- [ ] **Step 1: `build` passes the stage**

`sprites.py`, replace the body of `build`:

```python
def build(state, sp, frame=0, shadow=True, stage="adult"):
    """The living Plynling of species `sp` in one of the six moods, at a life stage. Each species
    draws its own silhouette in species.py, and every one of them wears the face below. Only
    species with stage art (export.STAGED) accept a stage other than "adult"."""
    from species import DRAW          # imported here: species.py imports this module
    if stage == "adult":
        return DRAW[sp](state, frame, shadow)
    return DRAW[sp](state, frame, shadow, stage=stage)
```

- [ ] **Step 2: The stage bodies**

`species.py`: add `PALE` to nothing (already imported). Change `def cepe(state, frame=0, shadow=True):` to

```python
def cepe(state, frame=0, shadow=True, stage="adult"):
    """The original Plynling: a broad cap, gills, a chubby body. The adult is the original
    sprite, untouched; the other life stages are drawn by cepe_stage."""
    if stage != "adult":
        return cepe_stage(state, frame, shadow, stage)
```

(the rest of the adult body stays exactly as it is). Insert directly before `# ---- shared pieces for the reshaped species`:

```python
# The Cèpe's other life stages, body shape only. Each is measured through finish() like the
# reshaped species (clipped face, measured sweat and frost), since the original's fixed
# extras were placed for the adult's silhouette.
#   top/span: the body from row `top` to 27, columns span; arms: the two rows of its nubs
#   cap: (centre row, x radius, y radius); cut: the cap's last row (gills sit under it)
#   face_oy: the face moved down; tilt: rows the far edge drops; droop: rows the rim edges
#   hang; fade: how far every colour leans to PALE
CEPE_STAGES = {
    "baby":  dict(top=18, span=(11, 20), arms=(22, 23), cap=(16.5, 12.6, 13.2), cut=16, face_oy=2,
                  tilt=0.0, droop=0.0, fade=0.0),
    "teen":  dict(top=14, span=(11, 20), arms=(20, 21), cap=(12.0, 12.2, 9.0), cut=12, face_oy=0,
                  tilt=0.8, droop=0.0, fade=0.0),
    "elder": dict(top=16, span=(10, 21), arms=(21, 22), cap=(14.8, 15.4, 9.8), cut=15, face_oy=0,
                  tilt=0.0, droop=1.6, fade=0.18),
}
FRINGE = (244, 240, 232)      # an elder's gills, gone white and hanging


def cepe_stage(state, frame, shadow, stage):
    p = SPECIES["cepe"]
    s = CEPE_STAGES[stage]
    pose = pose_for("cepe", state, frame)
    sag = 1 if state == "starving" else 0
    cap = [lerp(c, PALE, s["fade"]) for c in p["cap"]]
    S = [lerp(c, PALE, s["fade"]) for c in STEM]
    x0, x1 = s["span"]
    g = Grid()

    stem(g, S, x0, x1, s["top"], 27)
    for y in s["arms"]:
        g.put(x0 - 1, y, S[2], "stem")
        g.put(x1 + 1, y, S[3], "stem")
    feet(g, S)

    cy, rx, ry = s["cap"]
    cy += sag
    rx += 0.9 * pose.widen                                           # wider on the out-breath
    cut = s["cut"] + sag

    def lift(x):                                                    # how far this column sits lower
        nx = (x + 0.5 - 15.5) / rx
        return s["tilt"] * nx + s["droop"] * max(0.0, abs(nx) - 0.7) / 0.3

    gills(g, p, cut + 1, x0 - 4, x1 + 4, lambda x: int(round(lift(x))))
    for y in range(N):
        for x in range(N):
            yy = y - lift(x)
            if yy > cut + 0.5 or ((x + 0.5 - 15.5) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 > 1:
                continue
            g.put(x, y, shade_cap(x, yy, cap, 15.5, cy, rx, ry, 10, cy - 7.5, 8.5, 5.2, cut - 1), "cap")
    for sx, sy, r in p["spots"]:                                      # the scales, moved with the cap
        oy = sy + (cy - 14.0)
        for y in range(N):
            for x in range(N):
                if (x - sx) ** 2 + (y - lift(x) - oy) ** 2 <= r * r and g.r[y][x] == "cap" and y - lift(x) < cut - 1:
                    g.put(x, y, p["spot"][1] if (x - sx) + (y - oy) > r * 0.55 else p["spot"][0])
    if stage == "elder":                                              # the white fringe
        for x in range(x0 + 1, x1):
            g.put(x, cut + 2, FRINGE, "stem")
            if x % 2 == 0:
                g.put(x, cut + 3, FRINGE, "stem")

    g.outline(lambda reg, ny: INK if ny >= 27 else (cap[4] if reg == "cap" else STEM_OUT))
    return finish(g, p, state, S, face_oy=s["face_oy"], shadow=shadow, pose=pose)
```

- [ ] **Step 3: Check the adult is unchanged**

Run: `$PY "$SCRATCH/preview.py" cepe`
Expected first line: `frame 0 == still: all 36 OK` (every adult still, all species, identical to the shipped v2 stills).

- [ ] **Step 4: The stage preview script**

`$SCRATCH/stagepreview.py`:

```python
"""The Cèpe at every life stage: an animated GIF (rows = stages, columns = moods) on Discord's
dark background, and a strip of every frame per stage."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
ART = r"D:\Sources\Discord Bots\ProjectSYNCS\tools\plynling-art"
sys.path.insert(0, ART); os.chdir(ART)
from PIL import Image, ImageDraw
from common import N, LABEL
from motion import FRAMES, FRAME_MS
from sprites import build

STATES = ["happy", "content", "sad", "hungry", "starving", "frozen"]
STAGES = ["baby", "teen", "adult", "elder"]
BG, K = (49, 51, 56, 255), 5
T, GAP, TOP, LEFT = N * K, 12, 26, 90
frames = {(st, m): [build(m, "cepe", f, stage=st) for f in range(FRAMES)] for st in STAGES for m in STATES}

def sheet(f):
    out = Image.new("RGBA", (LEFT + len(STATES) * (T + GAP), TOP + len(STAGES) * (T + GAP)), BG)
    d = ImageDraw.Draw(out)
    for j, m in enumerate(STATES):
        d.text((LEFT + j * (T + GAP) + T // 2, TOP // 2), m, fill=(220, 221, 222), anchor="mm", font=LABEL)
    for i, st in enumerate(STAGES):
        d.text((LEFT // 2, TOP + i * (T + GAP) + T // 2), st, fill=(220, 221, 222), anchor="mm", font=LABEL)
        for j, m in enumerate(STATES):
            out.alpha_composite(frames[(st, m)][f].resize((T, T), Image.NEAREST), (LEFT + j * (T + GAP), TOP + i * (T + GAP)))
    return out

os.makedirs(os.path.join(HERE, "out"), exist_ok=True)
sheets = [sheet(f) for f in range(FRAMES)]
gif = os.path.join(HERE, "out", "anim_cepe_stages.gif")
sheets[0].convert("RGB").save(gif, save_all=True, append_images=[s.convert("RGB") for s in sheets[1:]],
                              duration=FRAME_MS, loop=0, disposal=1)
still = os.path.join(HERE, "out", "cepe_stages_still.png")
sheets[0].save(still)
print(gif, still, sep="\n")
```

Run: `$PY "$SCRATCH/stagepreview.py"` → two paths printed, no exception.

- [ ] **Step 5: Look before sending.** Read `cepe_stages_still.png`: each stage reads as its age at thumbnail size, the face sits on the body (no blush or brows floating off it), frost and sweat sit on the silhouette, the adult row is today's Cèpe. Fix anything wrong in `CEPE_STAGES` / `cepe_stage` and rerun Steps 3–4.

- [ ] **Step 6: Owner review.** Send `anim_cepe_stages.gif` (display `render`) and the still, and ask for changes. Iterate Steps 2–5 until the owner approves. Nothing is exported in this task.

- [ ] **Step 7: Commit the approved art code**

```bash
git add tools/plynling-art/species.py tools/plynling-art/sprites.py
git commit -m "Drew the Cèpe at every life stage" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Export the stages and point the bot at them

**Files:**
- Modify: `tools/plynling-art/export.py`
- Modify: `ProjectSYNCS/Helpers/PlynlingArt.cs`
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs:312`, `ProjectSYNCS/Services/PlynlingAnnouncer.cs:44`
- Test: `$SCRATCH/artcheck/Program.cs`, `$SCRATCH/animcheck.py`
- Create (generated): 18 files `assets/plynlings/plynling_cepe_{baby,teen,elder}_{mood}_v3.webp`

**Interfaces:**
- Consumes: `PlynlingStage`, `PlynlingLife.Stage` (Task 1); `build(..., stage=)` (Task 2).
- Produces: `PlynlingArt.StagedSpecies : IReadOnlySet<PlynlingSpecies>`; `PlynlingArt.Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood) : string`; `STAGED = {"cepe"}` in `export.py`.

- [ ] **Step 1: Snapshot today's files** (the byte-identical baseline)

```bash
rm -rf "$SCRATCH/v3_before" && cp -r "$REPO/assets/plynlings" "$SCRATCH/v3_before"
```

- [ ] **Step 2: Update artcheck (it must fail)**

In `$SCRATCH/artcheck/Program.cs`, replace the block from `var sprites = new List<string>();` through the `foreach (var f in Enum.GetValues<PlynlingFood>())` line with:

```csharp
var sprites = new HashSet<string>();
var stills = new List<string>();
foreach (var s in species)
{
    foreach (var stage in Enum.GetValues<PlynlingStage>())
        foreach (var m in Enum.GetValues<PlynlingMood>())
        {
            var url = PlynlingArt.Sprite(s, stage, m);
            sprites.Add(url);
            if (!PlynlingArt.StagedSpecies.Contains(s))
                Check($"{s} {stage} {m} falls back to the adult picture", url == PlynlingArt.Sprite(s, PlynlingStage.Adult, m));
        }
    for (var tier = 1; tier <= 5; tier++) stills.Add(PlynlingArt.Memorial(s, tier));
}
foreach (var f in Enum.GetValues<PlynlingFood>()) stills.Add(PlynlingArt.Food(f));
```

replace `Check($"{sprites.Count} sprite URLs", sprites.Count == 36);` with `Check($"{sprites.Count} sprite URLs", sprites.Count == 54);`, and before the final `Console.WriteLine` add:

```csharp
// STAGED in export.py and StagedSpecies must name the same species.
var exportPy = File.ReadAllText(Path.Combine(Path.GetDirectoryName(commonPy)!, "export.py"));
var staged = Regex.Match(exportPy, @"^STAGED = \{([^}]*)\}", RegexOptions.Multiline).Groups[1].Value;
var stagedKeys = Regex.Matches(staged, @"""([a-z_]+)""").Select(m => m.Groups[1].Value).ToHashSet();
Check("STAGED in export.py matches PlynlingArt.StagedSpecies",
    stagedKeys.SetEquals(PlynlingArt.StagedSpecies.Select(PlynlingArt.Key)));
```

Run: `dotnet run --project "$SCRATCH/artcheck"` → build errors (`PlynlingStage` overload / `StagedSpecies` missing).

- [ ] **Step 3: `PlynlingArt`**

Replace the `Sprite` method with:

```csharp
    // Species whose life stages have their own art. Must match STAGED in
    // tools/plynling-art/export.py — artcheck compares the two. Every other species shows its
    // adult picture at every stage until its art lands.
    public static readonly IReadOnlySet<PlynlingSpecies> StagedSpecies =
        new HashSet<PlynlingSpecies> { PlynlingSpecies.Cepe };

    // The adult filename deliberately carries no stage segment: it is the file every species
    // already had, so adding stages invalidated nothing Discord had cached.
    public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{mood.ToString().ToLowerInvariant()}_v{Version}.webp";

    private static string StageSegment(PlynlingSpecies species, PlynlingStage stage) =>
        stage == PlynlingStage.Adult || !StagedSpecies.Contains(species)
            ? ""
            : $"_{stage.ToString().ToLowerInvariant()}";
```

`PlynlingModule.cs:312`: `PlynlingArt.Sprite(plynling.Species, PlynlingLife.Mood(plynling, now))` →
`PlynlingArt.Sprite(plynling.Species, PlynlingLife.Stage(plynling, now), PlynlingLife.Mood(plynling, now))`.

`PlynlingAnnouncer.cs:44`: same replacement.

Run: `dotnet build "$REPO/ProjectSYNCS" -warnaserror` → `0 Error(s)`.
Run: `dotnet run --project "$SCRATCH/artcheck"` → fails: 18 `plynling_cepe_*_v3.webp … exists`, and the STAGED check (no `STAGED` in `export.py` yet).

- [ ] **Step 4: `export.py`**

Below `FOODS = [...]` add:

```python
# Species with their own life-stage art. Must match PlynlingArt.StagedSpecies in the bot
# (artcheck compares them). The adult keeps its stage-less filename; the others insert one.
STAGED = {"cepe"}
YOUNG_AND_OLD = ["baby", "teen", "elder"]
```

In `main`, replace the sprite loop body

```python
        for state in STATES:
            save_loop([build(state, sp, f) for f in range(FRAMES)], f"plynling_{sp}_{state}")
            count += 1
```

with

```python
        for state in STATES:
            save_loop([build(state, sp, f) for f in range(FRAMES)], f"plynling_{sp}_{state}")
            count += 1
            if sp in STAGED:
                for stage in YOUNG_AND_OLD:
                    save_loop([build(state, sp, f, stage=stage) for f in range(FRAMES)],
                              f"plynling_{sp}_{stage}_{state}")
                    count += 1
```

Run: `cd "$REPO/tools/plynling-art" && $PY export.py` → `88 files written`.

- [ ] **Step 5: Run the checks**

Run: `dotnet run --project "$SCRATCH/artcheck"` → `0 failed`.

Update `$SCRATCH/animcheck.py`: change `for st in STATES:` to iterate `(stage, st)` over `[("adult", m) for m in STATES] + ([(g, m) for g in ("baby", "teen", "elder") for m in STATES] if sp == "cepe" else [])`, build the filename as `f"plynling_{sp}_{st}_v3.webp"` for adult else `f"plynling_{sp}_{stage}_{st}_v3.webp"`, pass `stage=stage` to every `build(...)`, apply the "starts on the shipped v2 still" check to adult only, and append:

```python
import filecmp
before = os.path.join(HERE, "v3_before")
for f in os.listdir(before):
    check(f"{f} is byte-identical to before", filecmp.cmp(os.path.join(before, f), os.path.join(OUT, f), shallow=False))
```

Run: `$PY "$SCRATCH/animcheck.py"` → `0 failed`.

- [ ] **Step 6: Mutation-check** — set `STAGED = set()` in `export.py`: artcheck's STAGED check must fail. Restore.

- [ ] **Step 7: Commit**

```bash
git add tools/plynling-art/export.py assets/plynlings ProjectSYNCS/Helpers/PlynlingArt.cs ProjectSYNCS/Commands/PlynlingModule.cs ProjectSYNCS/Services/PlynlingAnnouncer.cs
git status --short assets/plynlings   # only 18 added files, nothing modified
git commit -m "Showed the Cèpe's life-stage art on its card" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Docs and version

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (help field "Adopter & regarder")
- Modify: `README.md` (Plynlings section), `tools/plynling-art/README.md`, `CLAUDE.md`, `ProjectSYNCS/config.yaml`

- [ ] **Step 1: `/plynling help`** — in the "Adopter & regarder" field, after the `/plynling view` line, append:

```csharp
                "\nIl grandit : **bébé** ses 2 premiers jours, **ado** jusqu'à 14 jours, **adulte**, " +
                "puis **ancien** après 6 mois. Le temps passé gelé ne compte pas."
```

Check the caps with `stagecheck` (append to `Program.cs`):

```csharp
var help = ProjectSYNCS.Commands.PlynlingModule.BuildHelpEmbed();
Check("help embed ≤ 6000", help.Length <= 6000);
Check("every help field ≤ 1024", help.Fields.All(f => f.Value.Length <= 1024));
```

Run: `dotnet run --project "$SCRATCH/stagecheck"` → `0 failed`.

- [ ] **Step 2: `README.md`** — after the "**The card**" bullet add:

```markdown
- **Growing up:** *bébé* for its first 2 days, *ado* until 14 days, *adulte*, then *ancien*
  after 6 months — counted in time actually lived, so a freeze pauses it. The card names the
  stage; the Cèpe already looks the part at every age, and the other species will follow.
```

- [ ] **Step 3: `tools/plynling-art/README.md`** — after the `motion.py` bullet add:

```markdown
- **Life stages:** `sprites.build(..., stage=)` takes `"baby"`, `"teen"`, `"adult"` or `"elder"`.
  Only species in `STAGED` (`export.py`) draw the non-adult stages — today the Cèpe, through
  `CEPE_STAGES` / `cepe_stage` in `species.py`. `STAGED` must match `PlynlingArt.StagedSpecies`.
  The adult file keeps its stage-less name (`plynling_cepe_happy_v3.webp`); the others insert
  the stage (`plynling_cepe_baby_happy_v3.webp`). Adding a species' stages adds files and
  changes none, so it needs no `ART_VERSION` bump.
```

and change "renders all 70 files" to "renders all 88 files".

- [ ] **Step 4: `CLAUDE.md`** — after the "**A living Plynling is an animated WebP…**" paragraph add:

```markdown
**Life stages are derived from `Age` and never stored.** `PlynlingLife.Stage` maps time actually
lived to bébé (< 2 d), ado (< 14 d), adulte (< 180 d) and ancien — so a frozen Plynling does not
grow up, a resurrected one resumes where its age puts it, and there is no column to migrate.
They are cosmetic only: nothing about hunger, happiness or death reads them. Every species shows
the label; only `PlynlingArt.StagedSpecies` gets stage *art*, and it must match `STAGED` in
`tools/plynling-art/export.py` (artcheck compares them). The adult filename has no stage segment
on purpose — it is the file every species already had.
```

- [ ] **Step 5: Build and commit**

Run: `dotnet build "$REPO/ProjectSYNCS" -warnaserror` → `0 Error(s)`.

```bash
git add ProjectSYNCS/Commands/PlynlingModule.cs README.md tools/plynling-art/README.md CLAUDE.md
git commit -m "Documented Plynling life stages" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 6: Version** — `ProjectSYNCS/config.yaml`: `version: "5.4.14"` → `version: "5.4.15"`.

```bash
git add ProjectSYNCS/config.yaml
git commit -m "Updated version" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
