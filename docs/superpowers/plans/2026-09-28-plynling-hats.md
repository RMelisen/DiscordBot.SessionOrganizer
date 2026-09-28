# Plynling hats Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Draw four accessories (nœud, haut-de-forme, petite couronne, casquette) on the Plynling's own animated sprite, on its card and in visits — the nœud first, the other three after it is judged on screen.

**Architecture:** The Python art tool stamps a small hand-drawn hat onto each finished frame, placed on the top of the head measured from the frame's body grid (fixed column, so it only moves up and down), and exports extra `_<hat>` files beside the bare ones. The bot learns which accessory has art from a new `CosmeticInfo.Hat`, and `PlynlingArt.SpriteOf(p, now)` becomes the one way to picture a living Plynling, adding the `_<hat>` segment when it wears one.

**Tech Stack:** .NET 10 / Discord.Net 3.20, Python 3.12 + Pillow, scratch harnesses in the session scratchpad.

**Spec:** `docs/superpowers/specs/2026-09-28-plynling-hats-design.md`

## Global Constraints

- Canvas stays **32×32**; the hat **overlays** the top of the cap (never a taller canvas). Do not propose redrawing Plynlings at a higher resolution.
- **Nothing moves side to side**: a hat's column is fixed for a whole loop; it follows the head only up and down.
- **No `ART_VERSION` / `PlynlingArt.Version` bump** (stays 4): hats are new filenames only, and every existing file must re-export **byte-identical**.
- Filenames: card `plynling_<sp>[_baby]_<state>_<hat>_v4.webp`, visit `plynling_<sp>[_baby]_<state>_<hat>_visit_v4.webp`. **154 files per hat** (98 card + 56 visit). Bare total today: **207**.
- Hat keys: `noeud`, `haut_de_forme`, `couronne`, `casquette` — the same text as the cosmetic key's suffix (`cos.accessory.<hat>`). Stored cosmetic keys never change; no migration.
- `HATS` in `tools/plynling-art/export.py` must equal `PlynlingArt.HatKeys` in the bot.
- Every mood wears the hat (frozen and starving tinted like the body).
- User-facing strings French; code, comments, logs English. Build with `-warnaserror`.
- The images resolve only once `assets/plynlings/` is **pushed to `main`** (GitHub raw URLs). Do not push; the owner does.

`$REPO` = `D:\Sources\Discord Bots\ProjectSYNCS`; `$ART` = `$REPO/tools/plynling-art`; `$PY` = `C:\Users\rmeli\AppData\Local\Programs\Python\Python312\python.exe`; `$SCRATCH` = the session scratchpad.

---

### Task 1: Stamp the nœud on the sprite (art tool, no export yet)

**Files:**
- Create: `tools/plynling-art/hats.py`
- Modify: `tools/plynling-art/species.py` (`finish`, ~line 300–339)
- Modify: `tools/plynling-art/sprites.py` (`build`, lines 15–22)
- Modify: `tools/plynling-art/common.py` (`SPECIES`: optional `hat_nudge`, only if the review asks for one)
- Test: `$SCRATCH/hatcheck.py`

**Interfaces:**
- Produces: `sprites.build(state, sp, frame=0, shadow=True, stage="adult", hat=None)` — `hat=None` returns exactly today's image.
- Produces: `hats.HATS_ART: dict[str, dict]` (drawn hats), `hats.place(g, hat, sp, stage) -> (x0, y0)`, `hats.wear(im, hat, sp, stage, state, frame) -> im`.
- Produces: finish() sets `im.body_grid` (the moved `Grid` of body + face, before tint and extras).

- [ ] **Step 1: Write the failing placement check**

Create `$SCRATCH/hatcheck.py`:

```python
"""Placement checks for the Plynling hats: run with the art folder as cwd."""
import sys
sys.path.insert(0, r"D:\Sources\Discord Bots\ProjectSYNCS\tools\plynling-art")

from common import SPECIES, N
from motion import FRAMES, pose
from sprites import build
from export import STAGED, STATES
import hats

HATS = sys.argv[1:] or list(hats.HATS_ART)
failed = 0


def fail(msg):
    global failed
    failed += 1
    print("FAIL", msg)


for hat in HATS:
    for sp in SPECIES:
        for stage in (["adult", "baby"] if sp in STAGED else ["adult"]):
            art = hats.HATS_ART[hat][stage]
            w, h = len(art[0]), len(art)
            limit = 15 if stage == "adult" else 18          # the row above the brows
            for state in STATES:
                xs = set()
                for f in range(FRAMES):
                    bare = build(state, sp, f, stage=stage)
                    x0, y0 = hats.place(bare.body_grid, hat, sp, stage)
                    xs.add(x0)
                    where = f"{hat} {sp} {stage} {state} f{f}"
                    if x0 < 0 or x0 + w > N or y0 < 0 or y0 + h > N:
                        fail(f"{where}: clipped at ({x0},{y0})")
                    dy = pose(sp, state, f).body[1]
                    if y0 + h - 1 > limit + dy:
                        fail(f"{where}: reaches row {y0 + h - 1}, face starts below {limit + dy}")
                    worn = build(state, sp, f, stage=stage, hat=hat)
                    if list(worn.getdata()) == list(bare.getdata()):
                        fail(f"{where}: the hat changed nothing")
                if len(xs) != 1:
                    fail(f"{hat} {sp} {stage} {state}: moves side to side {sorted(xs)}")

# hat=None is today's picture, pixel for pixel
for sp in SPECIES:
    for state in STATES:
        a, b = build(state, sp, 3), build(state, sp, 3, hat=None)
        if list(a.getdata()) != list(b.getdata()):
            fail(f"bare {sp} {state} differs")

print(f"{failed} failed")
```

- [ ] **Step 2: Run it to see it fail**

Run (from `$ART`): `$PY "$SCRATCH/hatcheck.py" noeud`
Expected: `ModuleNotFoundError: No module named 'hats'`.

- [ ] **Step 3: Expose the body grid from `finish`**

In `tools/plynling-art/species.py`, `finish`: just before `return im` (the last line), add:

```python
    im.body_grid = g         # the moved body and face, before tint and extras: hats.place measures it
    return im
```

(`g` is already the moved grid at that point — `g = moved(g, pose)` ran above. It is a plain attribute, not `im.info`, so nothing is written into any file.)

- [ ] **Step 4: Write `hats.py` with the nœud**

Create `tools/plynling-art/hats.py`:

```python
"""The hats a Plynling can wear on its sprite: small hand-drawn grids stamped over the top of
the cap on every frame. Each hat has an adult size and a smaller baby size.

Placement is measured, not hand-set: the hat's column is fixed on the face's axis (so it never
moves side to side), and its top row sits on the highest solid pixel of the head within the hat's
columns on that frame — measured on the body grid (finish() leaves it on im.body_grid), so the
heart, the z's, the sparkles, the frost, the shadow and the Mycena's halo never count. It therefore
follows the breath, the hop, the Cèpe's cap and the Rosé's puff with no per-frame work. A species
where the measured spot reads wrong nudges it with `hat_nudge` in common.SPECIES.

A hat drawn over the angry face would hide the 💢, so wear() redraws the mark on top.
"""
from common import N, SPECIES, lerp
from motion import pose as pose_for
from sprites import PALE, anger_mark

FROST_TINT = (176, 226, 255)     # species.FROST_TINT; not imported, species.py imports sprites
AXIS = 16                        # the face's axis: eyes at 12-13 and 18-19

# ---- the hats ---------------------------------------------------------------------------------
# '.' is transparent; every other character is a colour in the hat's palette.

HATS_ART = {
    "noeud": dict(
        palette={"O": (92, 24, 52), "R": (236, 84, 120), "r": (255, 160, 186),
                 "D": (176, 48, 88), "K": (212, 62, 100)},
        adult=[".OO...OO.",
               "ORrOKOrRO",
               "ORRDKDRRO",
               "ODROKORDO",
               ".OO...OO."],
        baby=["OO.O.OO",
              "ORrKrRO",
              "OO.O.OO"],
    ),
}


def place(g, hat, sp, stage):
    """Where the hat's top-left pixel goes on this frame: (x0, y0)."""
    art = HATS_ART[hat][stage if stage == "baby" else "adult"]
    w = len(art[0])
    nx, ny = SPECIES[sp].get("hat_nudge", {}).get(stage, (0, 0))
    x0 = AXIS - (w + 1) // 2 + nx
    top = next((y for y in range(N) for x in range(x0, x0 + w) if g.get(x, y) is not None), 0)
    return x0, top + ny


def grid_corner(g):
    """The body's top-right corner — sprites.cap_corner, measured on the grid instead of the
    finished image, whose 💢 is already drawn."""
    best = None
    for y in range(N):
        for x in range(N):
            if g.c[y][x] is not None and (best is None or x - y > best[0] - best[1]):
                best = (x, y)
    return best or (27, 4)


def wear(im, hat, sp, stage, state, frame):
    """Stamps the hat onto a finished frame (in place) and returns it."""
    spec = HATS_ART[hat]
    art = spec[stage if stage == "baby" else "adult"]
    x0, y0 = place(im.body_grid, hat, sp, stage)

    def px(x, y, c, a=255):
        if 0 <= x < N and 0 <= y < N:
            im.putpixel((x, y), c + (a,))

    for dy, row in enumerate(art):
        for dx, ch in enumerate(row):
            if ch == ".":
                continue
            c = spec["palette"][ch]
            if state == "frozen":
                c = lerp(c, FROST_TINT, 0.45)
            elif state == "starving":
                c = lerp(c, PALE, 0.28)
            px(x0 + dx, y0 + dy, c)
    if state == "angry":
        anger_mark(px, pose_for(sp, state, frame), grid_corner(im.body_grid),
                   SPECIES[sp].get("anger_nudge", (0, 0)))
    return im
```

Note the nudge shape: `hat_nudge` is a dict by stage, e.g. `hat_nudge={"adult": (0, 1), "baby": (0, 0)}`, because a baby's cap is a different shape from its adult's.

- [ ] **Step 5: Let `build` take a hat**

In `tools/plynling-art/sprites.py`, replace `build` (lines 15–22) with:

```python
def build(state, sp, frame=0, shadow=True, stage="adult", hat=None):
    """The living Plynling of species `sp` in one of the moods, at a life stage, optionally
    wearing a hat (hats.py). Each species draws its own silhouette in species.py, and every one
    of them wears the face below. Only species with baby art (export.STAGED) accept a stage other
    than "adult"."""
    from species import DRAW          # imported here: species.py imports this module
    im = DRAW[sp](state, frame, shadow) if stage == "adult" else DRAW[sp](state, frame, shadow, stage=stage)
    if hat is not None:
        from hats import wear         # likewise: hats.py imports this module
        wear(im, hat, sp, stage, state, frame)
    return im
```

- [ ] **Step 6: Run the check**

Run (from `$ART`): `$PY "$SCRATCH/hatcheck.py" noeud`
Expected: `0 failed`. If a species fails "reaches row", give it a `hat_nudge` in `common.SPECIES` only if the fix is a shift; otherwise shrink that size of the drawing. Re-run until `0 failed`.

- [ ] **Step 7: Add the contact sheet to `hats.py`**

Append to `tools/plynling-art/hats.py`:

```python
if __name__ == "__main__":
    import os
    import sys
    from PIL import Image
    from common import sheet, up
    from motion import FRAMES
    from sprites import build
    from export import STAGED

    out = sys.argv[1] if len(sys.argv) > 1 else "."
    wanted = sys.argv[2:] or list(HATS_ART)
    rows = [(sp, st) for sp in SPECIES for st in (["adult", "baby"] if sp in STAGED else ["adult"])]
    labels = [f"{sp} {st}" for sp, st in rows]
    moods = ["content", "happy", "sad", "sleeping", "frozen", "starving", "angry"]
    for hat in wanted:
        # frame 0 of each mood, then the bare content face beside it for comparison
        cols = ["bare"] + moods
        print(sheet(rows, cols,
                    lambda r, m: build("content" if m == "bare" else m, r[0], 0, stage=r[1],
                                       hat=None if m == "bare" else hat),
                    labels, cols, os.path.join(out, f"hats_{hat}.png"), K=4))
        # the happy loop (breath and hop) as a GIF, to judge that the hat rides the head
        frames = []
        for f in range(FRAMES):
            strip = Image.new("RGBA", (len(rows) * 32 * 4, 32 * 4), (47, 49, 54, 255))
            for i, (sp, st) in enumerate(rows):
                strip.alpha_composite(up(build("happy", sp, f, stage=st, hat=hat), 4), (i * 128, 0))
            frames.append(strip.convert("RGB"))
        frames[0].save(os.path.join(out, f"hats_{hat}_happy.gif"), save_all=True,
                       append_images=frames[1:], duration=125, loop=0)
```

Run (from `$ART`): `$PY hats.py "$SCRATCH" noeud`
Expected: prints the sheet size; `$SCRATCH/hats_noeud.png` and `$SCRATCH/hats_noeud_happy.gif` exist.

- [ ] **Step 8: Look at the sheet, then stop for the owner**

Read `$SCRATCH/hats_noeud.png` yourself first: the bow reads as a bow on every cap, sits centred on the head, hides no eye or brow, and the 💢 in the angry column is still on top. Fix obvious misplacements with `hat_nudge` (re-run Step 6 after each). Then send both files to the owner (SendUserFile, `display: render`) and **wait for approval** — the art is judged by eye. Iterate on the grid or the nudges as asked.

- [ ] **Step 9: Commit**

```bash
git add tools/plynling-art/hats.py tools/plynling-art/species.py tools/plynling-art/sprites.py tools/plynling-art/common.py
git commit -m "Drew the nœud on the Plynling sprite"
```

---

### Task 2: Export the nœud files

**Files:**
- Modify: `tools/plynling-art/export.py`
- Modify: `tools/plynling-art/README.md`
- Output: `assets/plynlings/*_noeud_*.webp` (154 new files)

**Interfaces:**
- Consumes: `sprites.build(..., hat=)` from Task 1.
- Produces: `export.HATS = ["noeud"]` — Task 3's harness parses this line (`HATS = [...]`, one line, double-quoted strings).

- [ ] **Step 1: Snapshot the current assets**

```bash
rm -rf "$SCRATCH/before" && cp -r "$REPO/assets/plynlings" "$SCRATCH/before" && ls "$SCRATCH/before" | wc -l
```
Expected: `207`.

- [ ] **Step 2: Export each hat beside the bare files**

In `tools/plynling-art/export.py`, below `VISIT_STATES`, add:

```python
# The hats drawn on the sprite (hats.py). Each one adds, beside every living file above, the same
# loop wearing it: plynling_<sp>[_baby]_<state>_<hat>[_visit]_v4.webp — 154 files a hat. New
# filenames only, so no ART_VERSION bump. Must match PlynlingArt.HatKeys in the bot.
HATS = ["noeud"]
```

Replace the living-sprite part of `main()` (the `for state in STATES:` loop, lines 78–89) with:

```python
        for state in STATES:
            for hat in [None] + HATS:
                tag = f"_{hat}" if hat else ""
                save_loop([build(state, sp, f, hat=hat) for f in range(FRAMES)], f"plynling_{sp}_{state}{tag}")
                count += 1
                if sp in STAGED:
                    save_loop([build(state, sp, f, stage="baby", hat=hat) for f in range(FRAMES)],
                              f"plynling_{sp}_baby_{state}{tag}")
                    count += 1
                if state in VISIT_STATES:
                    save_visit_loop([build(state, sp, f, hat=hat) for f in range(FRAMES)], f"plynling_{sp}_{state}{tag}")
                    count += 1
                    if sp in STAGED:
                        save_visit_loop([build(state, sp, f, stage="baby", hat=hat) for f in range(FRAMES)],
                                        f"plynling_{sp}_baby_{state}{tag}")
                        count += 1
```

- [ ] **Step 3: Export**

Run (from `$ART`): `$PY export.py`
Expected: `361 files written to …assets\plynlings` (207 + 154).

- [ ] **Step 4: Nothing existing changed**

```bash
cd "$SCRATCH/before" && for f in *; do cmp -s "$f" "$REPO/assets/plynlings/$f" || echo "CHANGED $f"; done; echo done
```
Expected: only `done` — no `CHANGED` line. Then:
```bash
ls "$REPO/assets/plynlings" | grep -c "_noeud_"
```
Expected: `154`.

- [ ] **Step 5: Document the hats in the art README**

In `tools/plynling-art/README.md`, after the "Visit pictures" bullet, add:

```markdown
- **Hats** (`hats.py`): accessories drawn on the sprite — a small hand-drawn grid per hat, in an
  adult and a smaller baby size, stamped over the top of the cap on every frame. Its column is
  fixed on the face's axis (nothing side to side); its top row sits on the highest solid pixel of
  the head within its columns, measured on the body grid `finish()` leaves on `im.body_grid`, so
  it rides the breath, the hop and each species' touch. `hat_nudge` in `common.SPECIES` (a dict by
  stage) shifts it where the measured spot reads wrong. Frozen and starving tint it like the body,
  and the angry 💢 is redrawn on top. `HATS` in `export.py` lists the exported ones —
  `plynling_<sp>[_baby]_<state>_<hat>[_visit]_v4.webp`, 154 files a hat, no version bump — and
  must match `PlynlingArt.HatKeys`. `python hats.py <out> [hat…]` renders a contact sheet and a
  GIF of the happy loop to judge a hat before exporting it.
```

Also change the file count sentence in the `export.py` bullet from "renders all 207 files" to "renders every file (207 bare, plus 154 per hat in `HATS`)".

- [ ] **Step 6: Commit**

```bash
git add tools/plynling-art/export.py tools/plynling-art/README.md assets/plynlings
git commit -m "Exported the Plynlings wearing the nœud"
```

---

### Task 3: The bot shows the hat

**Files:**
- Modify: `ProjectSYNCS/Helpers/CosmeticCatalog.cs` (record at line 17; `Accessory` helper at 187; nœud at 239)
- Modify: `ProjectSYNCS/Helpers/PlynlingArt.cs`
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs:607`, `ProjectSYNCS/Commands/PlynlingPlayCards.cs:23,65`, `ProjectSYNCS/Commands/PlynlingJournalCards.cs:62`, `ProjectSYNCS/Services/PlynlingAnnouncer.cs:44,54`
- Modify: `ProjectSYNCS/Helpers/PlynlingVisitStory.cs:18-22, 779-785`
- Modify: `ProjectSYNCS/Commands/CosmeticCards.cs` (shop line and options, wardrobe options)
- Modify: `CLAUDE.md`, `docs/superpowers/specs/2026-09-27-plynling-cosmetics-design.md`
- Test: `$SCRATCH/hatcheck-bot/` (console project)

**Interfaces:**
- Consumes: `HATS = [...]` line in `tools/plynling-art/export.py`; files in `assets/plynlings/`.
- Produces: `CosmeticInfo.Hat` (`string?`, last positional parameter, default `null`); `PlynlingArt.HatKeys` (`IReadOnlySet<string>`, derived from the catalog); `PlynlingArt.Sprite(species, stage, mood, string? hat = null)`; `PlynlingArt.VisitSprite(species, stage, mood, string? hat = null)`; `PlynlingArt.SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null)`; `VisitCast.Hat` (`string?`, last positional parameter).

- [ ] **Step 1: Write the failing harness**

Create `$SCRATCH/hatcheck-bot/hatcheck-bot.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="D:\Sources\Discord Bots\ProjectSYNCS\ProjectSYNCS\ProjectSYNCS.csproj" />
  </ItemGroup>
</Project>
```

Create `$SCRATCH/hatcheck-bot/Program.cs`:

```csharp
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

const string Repo = @"D:\Sources\Discord Bots\ProjectSYNCS";
var assets = Path.Combine(Repo, "assets", "plynlings");
var failed = 0;
void Check(bool ok, string what) { if (!ok) { failed++; Console.WriteLine($"FAIL {what}"); } }

// HatKeys == HATS in export.py
var export = File.ReadAllText(Path.Combine(Repo, "tools", "plynling-art", "export.py"));
var hatsLine = Regex.Match(export, @"^HATS = \[(.*)\]", RegexOptions.Multiline).Groups[1].Value;
var pyHats = Regex.Matches(hatsLine, "\"([a-z_]+)\"").Select(m => m.Groups[1].Value).ToHashSet();
Check(pyHats.Count > 0, "HATS found in export.py");
Check(pyHats.SetEquals(PlynlingArt.HatKeys), $"HATS [{string.Join(",", pyHats)}] == HatKeys [{string.Join(",", PlynlingArt.HatKeys)}]");

// Only accessories carry a hat, and each hat key is its cosmetic key's suffix
foreach (var c in CosmeticCatalog.All.Where(c => c.Hat is not null))
{
    Check(c.Slot == CosmeticSlot.Accessory, $"{c.Key} carries a hat but is not an accessory");
    Check(c.Key == $"cos.accessory.{c.Hat}", $"{c.Key} hat '{c.Hat}' is not its key's suffix");
}

// Every URL the bot can build with a hat is a file (species with art: those with a bare content file)
string FileOf(string url) => Path.Combine(assets, url[PlynlingArt.BaseUrl.Length..]);
var drawn = Enum.GetValues<PlynlingSpecies>()
    .Where(s => File.Exists(FileOf(PlynlingArt.Sprite(s, PlynlingStage.Adult, PlynlingMood.Content)))).ToList();
Check(drawn.Count == 7, $"7 drawn species, got {drawn.Count}");
var urls = 0;
foreach (var hat in PlynlingArt.HatKeys)
foreach (var s in drawn)
foreach (var stage in Enum.GetValues<PlynlingStage>())
foreach (var mood in Enum.GetValues<PlynlingMood>())
{
    foreach (var url in new[] { PlynlingArt.Sprite(s, stage, mood, hat), PlynlingArt.VisitSprite(s, stage, mood, hat) })
    {
        urls++;
        Check(url.Contains($"_{hat}_"), $"{url} has no hat segment");
        Check(File.Exists(FileOf(url)), $"missing {url}");
    }
}

// No hat, or an unknown one: the bare picture
Check(PlynlingArt.Sprite(PlynlingSpecies.Cepe, PlynlingStage.Adult, PlynlingMood.Happy, null)
      == PlynlingArt.Sprite(PlynlingSpecies.Cepe, PlynlingStage.Adult, PlynlingMood.Happy), "null hat is bare");
Check(PlynlingArt.Sprite(PlynlingSpecies.Cepe, PlynlingStage.Adult, PlynlingMood.Happy, "echarpe")
      == PlynlingArt.Sprite(PlynlingSpecies.Cepe, PlynlingStage.Adult, PlynlingMood.Happy), "undrawn hat is bare");

// SpriteOf reads the worn accessory
var now = DateTimeOffset.UtcNow;
var p = new Plynling { Species = PlynlingSpecies.Amanite, Name = "Test", Hunger = 1, Happiness = 0.7,
    NeedsAsOf = now, LiveSince = now, AdoptedAt = now };
Check(PlynlingArt.SpriteOf(p, now) == PlynlingArt.Sprite(p.Species, PlynlingLife.Stage(p, now), PlynlingLife.Mood(p, now)), "bare SpriteOf");
p.AccessoryKey = "cos.accessory.noeud";
Check(PlynlingArt.SpriteOf(p, now) == PlynlingArt.Sprite(p.Species, PlynlingLife.Stage(p, now), PlynlingLife.Mood(p, now), "noeud"), "noeud SpriteOf");
Check(PlynlingArt.SpriteOf(p, now, PlynlingMood.Sad).Contains("_sad_noeud_"), "SpriteOf with a given mood");
p.AccessoryKey = "cos.accessory.echarpe";
Check(!PlynlingArt.SpriteOf(p, now).Contains("echarpe"), "text-only accessory stays bare");

// A visit cast carries the hat into its pictures
var cast = new VisitCast("A", PlynlingSpecies.Cepe, PlynlingStage.Baby, "Cèpe", PlynlingGender.Male, [], "noeud");
Check(cast.Sprite(PlynlingMood.Happy) == PlynlingArt.VisitSprite(PlynlingSpecies.Cepe, PlynlingStage.Baby, PlynlingMood.Happy, "noeud"), "cast sprite");

Console.WriteLine($"{urls} hat urls checked, {failed} failed");
```

- [ ] **Step 2: Run it to see it fail**

Run: `dotnet run --project "$SCRATCH/hatcheck-bot"`
Expected: build errors — `CosmeticInfo` has no `Hat`, `PlynlingArt` has no `HatKeys` / `SpriteOf`, `Sprite` takes 3 arguments, `VisitCast` takes 6.

- [ ] **Step 3: `CosmeticInfo.Hat`**

In `ProjectSYNCS/Helpers/CosmeticCatalog.cs`, extend the record's comment and signature:

```csharp
// for titles. Price is the shop price — or, for a crafted one, the cailloux its recipe adds.
// Hat is the art key of an accessory drawn on the sprite (PlynlingArt.HatKeys), the same text as
// its key's suffix; null for every cosmetic that only shows as text.
public sealed record CosmeticInfo(
    string Key, CosmeticSlot Slot, CosmeticSource Source, ItemRarity Rarity, Season Season,
    string Emoji, string Name, string? NameF, uint? Accent, string? Banner, string? GraveLeft, string? GraveRight,
    long Price, IReadOnlyList<(string ItemKey, int Count)> Recipe, string? Hat = null);
```

Give the `Accessory` helper a `drawn` flag that sets `Hat` to the key:

```csharp
        void Accessory(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C,
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null, bool drawn = false) =>
            list.Add(new CosmeticInfo($"cos.accessory.{key}", CosmeticSlot.Accessory, source, rarity, season, emoji, name, null,
                null, null, null, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Accessory, source, rarity), recipe ?? none,
                drawn ? key : null));
```

and mark the nœud (line 239):

```csharp
        Accessory("noeud", "🎀", "un nœud", B, drawn: true);
```

- [ ] **Step 4: `PlynlingArt`**

In `ProjectSYNCS/Helpers/PlynlingArt.cs`, add below `StagedSpecies`:

```csharp
    // The accessories drawn on the sprite: every cosmetic with a Hat. Must match HATS in
    // tools/plynling-art/export.py — artcheck compares them — or a card links a file that was
    // never exported. Derived rather than listed, so marking an accessory `drawn` is the one edit.
    public static readonly IReadOnlySet<string> HatKeys =
        CosmeticCatalog.All.Select(c => c.Hat).OfType<string>().ToHashSet();
```

Replace `Sprite` and `VisitSprite` with:

```csharp
    // The adult filename deliberately carries no stage segment: it is the file every species
    // already had, so adding the baby invalidated nothing Discord had cached. A hat inserts its
    // own segment after the mood; a hat with no art (not in HatKeys) is ignored.
    public static string Sprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood, string? hat = null) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_{mood.ToString().ToLowerInvariant()}{HatSegment(hat)}_v{Version}.webp";

    /// <summary>
    /// The picture a visit story shows (happy, content, sad or angry): the same animation on a larger
    /// transparent canvas, so the two side-by-side Plynlings are a little smaller than the card's
    /// (see VISIT_CANVAS in tools/plynling-art/export.py). Any other mood shows the content face.
    /// </summary>
    public static string VisitSprite(PlynlingSpecies species, PlynlingStage stage, PlynlingMood mood, string? hat = null) =>
        $"{BaseUrl}plynling_{Key(species)}{StageSegment(species, stage)}_" +
        $"{(mood is PlynlingMood.Happy or PlynlingMood.Sad or PlynlingMood.Angry ? mood : PlynlingMood.Content).ToString().ToLowerInvariant()}" +
        $"{HatSegment(hat)}_visit_v{Version}.webp";

    /// <summary>
    /// A living Plynling's picture — the one way to get it, so a hat it wears shows everywhere it
    /// is pictured: its species, stage, mood (or <paramref name="mood"/>) and its accessory's hat.
    /// </summary>
    public static string SpriteOf(Plynling p, DateTimeOffset now, PlynlingMood? mood = null) =>
        Sprite(p.Species, PlynlingLife.Stage(p, now), mood ?? PlynlingLife.Mood(p, now), HatOf(p));

    /// <summary>The hat a Plynling wears on its sprite, or null.</summary>
    public static string? HatOf(Plynling p) => CosmeticSlots.Worn(p, CosmeticSlot.Accessory)?.Hat;

    private static string HatSegment(string? hat) => hat is not null && HatKeys.Contains(hat) ? $"_{hat}" : "";
```

- [ ] **Step 5: Route the six call sites through `SpriteOf`**

- `Commands/PlynlingModule.cs:607` → `? PlynlingArt.SpriteOf(plynling, now)`
- `Commands/PlynlingPlayCards.cs:23` → `var sprite = PlynlingArt.SpriteOf(plynling, now);`
- `Commands/PlynlingPlayCards.cs:65` → `var sprite = PlynlingArt.SpriteOf(visitor, now);`
- `Commands/PlynlingJournalCards.cs:62` → `? PlynlingArt.SpriteOf(p, now)`
- `Services/PlynlingAnnouncer.cs:44` → `return PostAsync(plynling.GuildId, line, PlynlingArt.SpriteOf(plynling, now), "resurrection");`
- `Services/PlynlingAnnouncer.cs:54` → `PlynlingArt.SpriteOf(plynling, now, PlynlingMood.Sad), "abandon");`

Then confirm none is left:

Run: Grep for `PlynlingArt\.Sprite\(` in `ProjectSYNCS/` (excluding `PlynlingArt.cs`).
Expected: no matches.

- [ ] **Step 6: Visits carry the hat**

In `ProjectSYNCS/Helpers/PlynlingVisitStory.cs`, lines 15–22:

```csharp
// One Plynling as the story shows it — captured when the visit happens, so paging back through
// the story later needs no database and shows them as they were, hat included. The picture is
// built per step from the species, the stage, the hat and that step's face.
public sealed record VisitCast(string Name, PlynlingSpecies Species, PlynlingStage Stage, string SpeciesName,
    PlynlingGender Gender, IReadOnlyList<Passion> Passions, string? Hat = null)
{
    public string Sprite(PlynlingMood face) => PlynlingArt.VisitSprite(Species, Stage, face, Hat);
}
```

and `Cast` (line 779):

```csharp
    private static VisitCast Cast(Plynling p, DateTimeOffset now) => new(
        PlynlingCardUi.SafeName(p.Name),
        p.Species,
        PlynlingLife.Stage(p, now),
        PlynlingCatalog.Info(p.Species).Name,
        p.Gender,
        PlynlingPassions.Of(p),
        PlynlingArt.HatOf(p));
```

- [ ] **Step 7: The shop and the wardrobe say which ones show**

In `ProjectSYNCS/Commands/CosmeticCards.cs`, add below `SourceLabel`:

```csharp
    // Said beside an accessory drawn on the sprite, so people know which ones change the picture.
    private const string ShowsOnSprite = "✨ se voit sur ton Plynling";
```

In `BuildShop`, the `Line` local becomes:

```csharp
        string Line(CosmeticInfo c) =>
            $"{c.Emoji} **{CosmeticCatalog.Label(c)}** · {SourceLabel(c)} · {PebbleEconomy.Cailloux(c.Price)}" +
            (c.Hat is null ? "" : $" · {ShowsOnSprite}") + (owned.Contains(c.Key) ? " ✅" : "");
```

and the buy option's description:

```csharp
                menu.AddOption($"{CosmeticCatalog.Label(c)} — {PebbleEconomy.Cailloux(c.Price)}", c.Key,
                    $"{CosmeticCatalog.SlotLabel(c.Slot)} · {SourceLabel(c)}" + (c.Hat is null ? "" : $" · {ShowsOnSprite}"),
                    new Emoji(c.Emoji));
```

In `BuildWardrobe`, the option loop:

```csharp
            foreach (var c in owned.Where(c => c.Slot == slot))
                menu.AddOption(Shown(c), c.Key, c.Hat is null ? null : ShowsOnSprite,
                    emote: new Emoji(c.Emoji), isDefault: c.Key == worn?.Key);
```

(Longest shop option description: « Accessoire · de saison (printemps) · ✨ se voit sur ton Plynling » is well under Discord's 100.)

- [ ] **Step 8: Build and run the harness**

Run: `dotnet build "$REPO/ProjectSYNCS" -warnaserror`
Expected: `0 Warning(s)`, `0 Error(s)`.

Run: `dotnet run --project "$SCRATCH/hatcheck-bot"`
Expected: `448 hat urls checked, 0 failed` (1 hat × 7 species × 4 stages × 8 moods × 2).

- [ ] **Step 9: Docs**

In `CLAUDE.md`, after the paragraph beginning **"A living Plynling is an animated WebP; memorials and foods are PNG."**, add:

```markdown
**Some accessories are drawn on the sprite, and that is a property of the cosmetic.**
`CosmeticInfo.Hat` names the art (the key's own suffix, set by `drawn: true` in the catalog);
`PlynlingArt.HatKeys` is derived from it and **must equal `HATS` in
`tools/plynling-art/export.py`**, or a card links a file that was never exported — artcheck
compares them and checks every hatted URL exists. Nothing is stored per Plynling beyond the
`AccessoryKey` it already had. **`PlynlingArt.SpriteOf(p, now)` is the only way to picture a
living Plynling** — it replaced six copies of `Sprite(p.Species, Stage, Mood)`, and a new call
site that skips it silently drops the hat. A visit's `VisitCast` snapshots the hat, so an old
story keeps what it wore. Hatted files are new filenames beside the bare ones, so adding a hat
never bumps `Version`; the hat overlays the cap on the 32×32 canvas and only ever moves up and
down.
```

In the same file, in the paragraph **"The cosmetics shop is a function of the week, not state."**, the sentence ending the paragraph wraps across two lines (`…the titre and accessoire ride in` / `the heading's text.`); replace that second line, `the heading's text.`, with `the heading's text, and a drawn accessory also shows on the sprite.`

In `docs/superpowers/specs/2026-09-27-plynling-cosmetics-design.md`, line 5–6, append to the Phase 2 sentence: " — designed in `2026-09-28-plynling-hats-design.md`."

In `docs/superpowers/specs/2026-09-28-plynling-hats-design.md`, the `HatKeys` bullet: replace "`HatKeys` — the hats that have art." with "`HatKeys` — the hats that have art, derived from the catalog's `Hat`s."

- [ ] **Step 10: Commit**

```bash
git add ProjectSYNCS CLAUDE.md docs/superpowers/specs
git commit -m "Showed the nœud on the Plynling's picture"
```

- [ ] **Step 11: Stop for the owner**

Tell the owner: the nœud is ready; it shows on Discord only after `main` is pushed (the images are GitHub raw URLs). Suggest they push, wear a nœud with `/plynling wardrobe`, and open `/plynling view` and a visit. **Wait for the go-ahead before Task 4** — the other three hats follow only once the nœud is judged on screen.

---

### Task 4: The haut-de-forme, the couronne and the casquette

**Files:**
- Modify: `tools/plynling-art/hats.py` (`HATS_ART`)
- Modify: `tools/plynling-art/common.py` (`hat_nudge`, only if needed)
- Modify: `tools/plynling-art/export.py` (`HATS`)
- Modify: `ProjectSYNCS/Helpers/CosmeticCatalog.cs` (lines 242, 247, 249)
- Modify: `tools/plynling-art/README.md` (nothing if the hats paragraph already reads generally)
- Output: 462 new files in `assets/plynlings/`

**Interfaces:**
- Consumes: everything from Tasks 1–3 unchanged.

- [ ] **Step 1: Draw the three hats**

Add to `HATS_ART` in `tools/plynling-art/hats.py`:

```python
    "haut_de_forme": dict(      # short and squat on purpose: it overlays the cap
        palette={"O": (24, 20, 32), "B": (58, 52, 72), "H": (104, 96, 124), "b": (196, 60, 72)},
        adult=["..OOOOO..",
               "..OHBBO..",
               "..OHBBO..",
               "..ObbbO..",
               "OOHBBBBBO",
               ".OOOOOOO."],
        baby=[".OOOOO.",
              ".OHBBO.",
              ".ObbbO.",
              "OHBBBBO",
              ".OOOOO."],
    ),
    "couronne": dict(
        palette={"y": (255, 236, 140), "Y": (240, 196, 64), "d": (184, 132, 32),
                 "r": (220, 50, 70), "g": (80, 180, 240)},
        adult=["y...y...y",
               "Yy.yYy.yY",
               "YYYYYYYYY",
               "YrYYgYYrY",
               "ddddddddd"],
        baby=["y..y..y",
              "YyYYYyY",
              "YrYgYrY",
              "ddddddd"],
    ),
    "casquette": dict(          # faces the viewer, brim towards us: nothing turns to the side
        palette={"O": (22, 40, 90), "C": (70, 120, 220), "c": (130, 176, 255),
                 "D": (48, 88, 170), "B": (40, 80, 170), "w": (250, 250, 250)},
        adult=["..OOOOO..",
               ".OcCCCDO.",
               "OcCCwCCDO",
               "OBBBBBBBO",
               ".OOOOOOO."],
        baby=[".OOOOO.",
              "OcCwCDO",
              "OBBBBBO",
              ".OOOOO."],
    ),
```

- [ ] **Step 2: Check the placement**

Run (from `$ART`): `$PY "$SCRATCH/hatcheck.py" haut_de_forme couronne casquette`
Expected: `0 failed`. Fix any failure with a smaller drawing or a `hat_nudge`, as in Task 1.

- [ ] **Step 3: Contact sheets, then stop for the owner**

Run (from `$ART`): `$PY hats.py "$SCRATCH" haut_de_forme couronne casquette`
Read the three sheets yourself first (same criteria as Task 1 Step 8), then send the three PNGs and three GIFs to the owner and **wait for approval**, iterating as asked.

- [ ] **Step 4: Export**

In `tools/plynling-art/export.py`: `HATS = ["noeud", "haut_de_forme", "couronne", "casquette"]`.

```bash
rm -rf "$SCRATCH/before" && cp -r "$REPO/assets/plynlings" "$SCRATCH/before"
```

Run (from `$ART`): `$PY export.py`
Expected: `823 files written to …` (207 + 4 × 154).

```bash
cd "$SCRATCH/before" && for f in *; do cmp -s "$f" "$REPO/assets/plynlings/$f" || echo "CHANGED $f"; done; echo done
```
Expected: only `done` (every bare file and every nœud file byte-identical).

- [ ] **Step 5: The bot knows them**

In `ProjectSYNCS/Helpers/CosmeticCatalog.cs`:

```csharp
        Accessory("casquette", "🧢", "une casquette", Rot, C, drawn: true);
        Accessory("haut_de_forme", "🎩", "un haut-de-forme", Rot, R, drawn: true);
        Accessory("couronne", "👑", "une petite couronne", Rot, L, drawn: true);
```

Run: `dotnet build "$REPO/ProjectSYNCS" -warnaserror` → `0 Error(s)`, `0 Warning(s)`.
Run: `dotnet run --project "$SCRATCH/hatcheck-bot"` → `1792 hat urls checked, 0 failed` (4 × 448).

- [ ] **Step 6: Commit**

```bash
git add tools/plynling-art assets/plynlings ProjectSYNCS/Helpers/CosmeticCatalog.cs
git commit -m "Drew the haut-de-forme, the couronne and the casquette on the sprite"
```

Tell the owner the three are ready once `main` is pushed.
