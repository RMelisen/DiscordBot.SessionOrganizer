# Item icons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the 40 collectibles, the 6 sets and the 59 cosmetics a hand-drawn 16×16 pixel icon in the Champignons' style, shown as the bot's own application emojis, with `/inventory view` split in two pages so it still fits.

**Architecture:** A Python tool (`tools/item-art/`) holds every icon as a text grid in the Pear36 palette and exports ×8 PNGs to `ProjectSYNCS/Assets/Icons/<key>.png` plus an HTML sheet per batch. The bot uploads them at startup like the mushrooms (`ApplicationEmojiService`), and sets and cosmetics read their emoji through `ItemEmojis` with today's Unicode as the fallback. Art comes in batches, each approved by the owner on its sheet.

**Tech Stack:** Python 3 + Pillow (no pytest — checks are plain scripts); C# / .NET 10, Discord.Net 3.20.1; a scratch console harness.

**Spec:** `docs/superpowers/specs/2026-09-29-item-icons-design.md`

## Global Constraints

- **Never commit or push.** The owner commits by hand. Each task ends by listing the files to commit.
- **No batch of art is final until the owner approves its sheet.** Each art task ends with a STOP.
- 16×16 grids, exported ×8 to 128×128 with nearest neighbour; alpha only 0 or 255.
- Palette: Pear36 + `#4b3837 #7d5d5c #a27c6e #cfa385 #193a66 #316196 #79a5d3 #732c47` (44 colours). Outline `#272736`.
- A 1-pixel `#272736` outline all round (every opaque pixel touching transparency is outline); nothing on the frame's outer row or column.
- Light from the top left; usually 4–7 colours; the object fills about 14×14; rarity shows through glints (`#ffe478`, `#ffffeb`), never a border.
- Icon keys: item keys (`col.galet`, `cos.accessory.noeud`) and `set.<set key>`. Files: `ProjectSYNCS/Assets/Icons/<key>.png`.
- Emoji names: `col.`→`c_`, `set.`→`s_`, `cos.theme.`→`th_`, `cos.title.`→`ti_`, `cos.accessory.`→`ac_`, `cos.grave.`→`gr_`; mushrooms keep `shroom_<slug>`. `[A-Za-z0-9_]`, 2–32 characters.
- The mushrooms (`Assets/Mushrooms/`, `CREDITS.txt`) are not touched.
- Everything shown falls back to today's Unicode emoji while an icon is not uploaded.
- Command and option names English, user-facing text French, code and comments English.
- Build with `dotnet build -warnaserror` (0 warnings).
- Git Bash mangles `python -c` and some heredocs here (`|| goto :error`): write scripts to files and run them.

`$REPO` = `C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer`; `$ART` = `$REPO/tools/item-art`; `$SCRATCH` = the session scratchpad directory.

---

### Task 1: The art tool and the Cailloux pilot

**Files:**
- Create: `tools/item-art/palette.py`, `tools/item-art/icons.py`, `tools/item-art/export.py`, `tools/item-art/check.py`, `tools/item-art/README.md`
- Create (output): `ProjectSYNCS/Assets/Icons/col.<8 cailloux>.png`
- Sheets go to `tools/item-art/out/` — already git-ignored by the root `.gitignore`'s `[Oo]ut/`.

**Interfaces:**
- Produces: `palette.PALETTE: dict[str, str]` (character → `#rrggbb`), `palette.TRANSPARENT = "."`; `icons.ICONS: dict[str, Icon]`, `icons.GROUPS: dict[str, list[str]]`, `Icon(name, unicode, grid)`; `export.render(key) -> Image` (16×16 RGBA), `export.check_image(key, im)`; `python export.py [group…]`; `python check.py`.

- [ ] **Step 1: Write the palette**

`tools/item-art/palette.py`:

```python
"""The icons' palette: Pear36 (the palette the Champignons pack is drawn in) plus the pack's eight
extra browns and blues. One character per colour; "." is transparent. A grid using any other
character is refused by export.py."""

TRANSPARENT = "."
OUTLINE = "#"

PALETTE = {
    # outline and greys
    "#": "#272736", "k": "#43434f", "g": "#606070", "G": "#7e7e8f", "s": "#c2c2d1", "w": "#ffffeb",
    # purples
    "p": "#322947", "P": "#473b78", "q": "#3e2347", "v": "#422445", "V": "#5a265e", "m": "#57294b",
    "M": "#73275c", "n": "#80366b",
    # reds and pinks
    "z": "#5e315b", "r": "#8c3f5d", "R": "#964253", "c": "#b0305c", "C": "#bd4882", "o": "#ba6156",
    "e": "#e36956", "E": "#eb564b", "f": "#ff9166", "i": "#ff6b97", "I": "#ffb5b5",
    # oranges and yellows
    "a": "#f2a65e", "A": "#ffb570", "y": "#ffe478", "Y": "#cfff70",
    # greens
    "l": "#8fde5d", "L": "#3ca370", "t": "#3d6e70", "T": "#323e4f",
    # blues (d, D, h are the pack's)
    "b": "#4b5bab", "B": "#4da6ff", "x": "#66ffe3", "d": "#193a66", "D": "#316196", "h": "#79a5d3",
    # the pack's browns
    "u": "#4b3837", "U": "#7d5d5c", "j": "#a27c6e", "J": "#cfa385", "Z": "#732c47",
}

assert len(PALETTE) == 44 and len(set(PALETTE.values())) == 44
```

- [ ] **Step 2: Write the icon table with one example**

`tools/item-art/icons.py` (the galet is the format example; the other seven are drawn in Step 6):

```python
"""Every item icon as a 16×16 grid in palette characters (palette.py), keyed by its icon key: the
item key (col.galet, cos.accessory.noeud) or set.<set key>. GROUPS orders them into the batches
the owner judges, one sheet each. name and unicode are only for the sheet: the bot's catalog is
the truth for both."""
from dataclasses import dataclass


@dataclass(frozen=True)
class Icon:
    name: str
    unicode: str
    grid: tuple


ICONS: dict = {}
GROUPS: dict = {}


def icon(group, key, name, unicode, *rows):
    assert key not in ICONS, f"{key} drawn twice"
    ICONS[key] = Icon(name, unicode, tuple(rows))
    GROUPS.setdefault(group, []).append(key)


# ---- Cailloux ---------------------------------------------------------------------------------

icon("cailloux", "col.galet", "Galet", "🪨",
     "................",
     "................",
     "................",
     ".....######.....",
     "...##ssssGG##...",
     "..#sswwsGGGGG#..",
     ".#sswsGGGGGGGg#.",
     ".#ssGGGGGGGGGg#.",
     ".#sGGGGGGGGGgg#.",
     ".#GGGGGGGGGggg#.",
     "..#GGGGGGGggg#..",
     "...##gggggg##...",
     ".....######.....",
     "................",
     "................",
     "................")
```

- [ ] **Step 3: Write the checks first**

`tools/item-art/check.py`:

```python
"""Checks every icon without writing into the bot: grid shape, palette, outline, margins, and that
an exported PNG is an exact ×8 of its grid. Run: python check.py (exit code 1 on any failure)."""
import os
import sys
import tempfile

from PIL import Image

import export
from icons import GROUPS, ICONS

failures = []


def check(ok, what):
    if not ok:
        failures.append(what)


check(sorted(k for keys in GROUPS.values() for k in keys) == sorted(ICONS), "GROUPS and ICONS disagree")
check(all(k.startswith(("col.", "set.", "cos.theme.", "cos.title.", "cos.accessory.", "cos.grave."))
          for k in ICONS), "a key of no known kind")

with tempfile.TemporaryDirectory() as tmp:
    for key in ICONS:
        try:
            small = export.render(key)          # runs check_image
        except SystemExit as e:
            failures.append(str(e))
            continue
        path = export.write_png(key, small, tmp)
        big = Image.open(path).convert("RGBA")
        check(big.size == (128, 128), f"{key}: exported {big.size}")
        check(big.resize((16, 16), Image.NEAREST).tobytes() == small.tobytes(), f"{key}: not an exact ×8")
        check(big.resize((16, 16), Image.NEAREST).resize((128, 128), Image.NEAREST).tobytes() == big.tobytes(),
              f"{key}: not made of 8×8 blocks")

print(f"{len(ICONS)} icons checked")
if failures:
    print("FAIL\n" + "\n".join(failures))
    sys.exit(1)
print("OK")
```

- [ ] **Step 4: Run it to see it fail**

Run: `cd "$ART" && python check.py`
Expected: FAIL with `ModuleNotFoundError: No module named 'export'`.

- [ ] **Step 5: Write the exporter**

`tools/item-art/export.py`:

```python
"""Exports the item icons: icons.py → ProjectSYNCS/Assets/Icons/<key>.png (×8, nearest
neighbour), and one HTML sheet per group in out/ to judge them at the size Discord shows them.

    python export.py              every group
    python export.py cailloux     only these groups (other groups' files are left alone)

A hand-edited overrides/<key>.png (16×16, palette colours) replaces that key's grid.
"""
import base64
import html
import io
import os
import sys

from PIL import Image

from icons import GROUPS, ICONS
from palette import OUTLINE, PALETTE, TRANSPARENT

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.normpath(os.path.join(HERE, "..", "..", "ProjectSYNCS", "Assets"))
ICON_DIR = os.path.join(ASSETS, "Icons")
OUT = os.path.join(HERE, "out")
OVERRIDES = os.path.join(HERE, "overrides")
REFERENCE = os.path.join(ASSETS, "Mushrooms", "girolle.png")
SIZE, SCALE = 16, 8
COLOURS = {tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) for h in PALETTE.values()}
OUTLINE_RGB = tuple(int(PALETTE[OUTLINE][i:i + 2], 16) for i in (1, 3, 5))


def rgb(hex_colour):
    return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))


def check_image(key, im):
    """The style rules that can be checked: size, alpha, palette, margin, outline."""
    if im.size != (SIZE, SIZE):
        raise SystemExit(f"{key}: {im.size}, expected {SIZE}×{SIZE}")
    px = im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            r, g, b, a = px[x, y]
            if a not in (0, 255):
                raise SystemExit(f"{key}: half-transparent pixel at ({x},{y})")
            if a == 0:
                continue
            if (r, g, b) not in COLOURS:
                raise SystemExit(f"{key}: #{r:02x}{g:02x}{b:02x} at ({x},{y}) is not in the palette")
            if x in (0, SIZE - 1) or y in (0, SIZE - 1):
                raise SystemExit(f"{key}: pixel on the frame's edge at ({x},{y})")
            touches_air = any(px[x + dx, y + dy][3] == 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if touches_air and (r, g, b) != OUTLINE_RGB:
                raise SystemExit(f"{key}: ({x},{y}) touches transparency but is not outline")


def render(key):
    override = os.path.join(OVERRIDES, key + ".png")
    if os.path.exists(override):
        im = Image.open(override).convert("RGBA")
    else:
        grid = ICONS[key].grid
        if len(grid) != SIZE or any(len(row) != SIZE for row in grid):
            raise SystemExit(f"{key}: the grid must be {SIZE} rows of {SIZE} characters")
        im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        for y, row in enumerate(grid):
            for x, ch in enumerate(row):
                if ch == TRANSPARENT:
                    continue
                if ch not in PALETTE:
                    raise SystemExit(f"{key}: '{ch}' at ({x},{y}) is not a palette character")
                im.putpixel((x, y), rgb(PALETTE[ch]) + (255,))
    check_image(key, im)
    return im


def write_png(key, small, folder=ICON_DIR):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, key + ".png")
    small.resize((SIZE * SCALE, SIZE * SCALE), Image.NEAREST).save(path, optimize=True)
    return path


def data_uri(path):
    with open(path, "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()


def write_sheet(group, keys):
    """Each icon at 128 px, then at 22 px (Discord's inline emoji size) beside the Unicode it
    replaces and a mushroom, then in a line of text as the inventory prints it."""
    ref = data_uri(REFERENCE)
    rows = []
    for key in keys:
        uri = data_uri(os.path.join(ICON_DIR, key + ".png"))
        ic = ICONS[key]
        rows.append(
            f"<tr><td><img src='{uri}' width=128 height=128 class=px></td>"
            f"<td><img src='{uri}' width=22 height=22> <span class=u>{ic.unicode}</span> "
            f"<img src='{ref}' width=22 height=22></td>"
            f"<td><img src='{uri}' width=22 height=22> {html.escape(ic.name)} ×3<br>"
            f"<span class=u>{ic.unicode}</span> {html.escape(ic.name)} ×3</td>"
            f"<td class=k>{key}</td></tr>")
    page = ("<!doctype html><meta charset=utf-8><title>" + group + "</title><style>"
            "body{background:#313338;color:#dbdee1;font:16px 'gg sans','Segoe UI',sans-serif;margin:16px}"
            "td{padding:6px 14px;vertical-align:middle}.px{image-rendering:pixelated}"
            ".u{font-size:22px;line-height:22px}.k{color:#949ba4;font-size:13px}img{vertical-align:middle}"
            f"</style><h2>{group}</h2><table>{''.join(rows)}</table>")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"sheet_{group}.html")
    with open(path, "w", encoding="utf-8") as f:
        f.write(page)
    return path


def main(groups):
    unknown = [g for g in groups if g not in GROUPS]
    if unknown:
        raise SystemExit(f"unknown group(s): {unknown}; known: {list(GROUPS)}")
    for group in groups or list(GROUPS):
        for key in GROUPS[group]:
            write_png(key, render(key))
        print(f"{group}: {len(GROUPS[group])} icons, sheet {write_sheet(group, GROUPS[group])}")


if __name__ == "__main__":
    main(sys.argv[1:])
```

- [ ] **Step 6: Draw the other seven Cailloux**

Add them to `icons.py` under the galet, one `icon("cailloux", …)` each, following the Global Constraints' style rules. Briefs (what each shows; the Unicode it replaces is the 4th argument):

| Key | Name | Unicode | Brief |
|---|---|---|---|
| `col.caillou_plat` | Caillou plat | 🥏 | a wide, thin skipping stone seen slightly from above: a flat ellipse, blue-grey (`h`/`D` shading with `s` highlight), much flatter than the galet |
| `col.silex` | Silex | 🔪 | a knapped flint shard with a sharp point: dark grey-brown (`u`/`U`) with pale conchoidal facet lines (`j`/`J`) |
| `col.quartz` | Quartz | 🔷 | one upright hexagonal crystal point, milky white (`w`/`s`) with pale blue faces (`h`), a glint at the tip |
| `col.agate` | Agate | 🟠 | a round slice showing concentric bands: orange, cream and wine (`a`/`J`/`o`/`R`) |
| `col.amethyste` | Améthyste | 🟣 | three purple crystal points (`n`/`V`/`C`) rising from a small grey rock (`g`/`G`) |
| `col.opale` | Opale | 🌈 | a rounded cabochon, cream body (`w`/`s`) with flecks of pink, green and blue (`I`/`l`/`x`) |
| `col.meteorite` | Météorite | ☄️ | legendary: a dark pitted iron rock (`k`/`g`, pits in `#`) with a short flame trail up to the right (`E`/`a`/`y`) and one `w` glint |

- [ ] **Step 7: Run the checks until they pass**

Run: `cd "$ART" && python check.py`
Expected: `8 icons checked` then `OK`. Fix any grid it names (the message gives the key and the pixel).

- [ ] **Step 8: Export and look at the sheet yourself first**

Run: `cd "$ART" && python export.py cailloux`
Expected: `cailloux: 8 icons, sheet …\out\sheet_cailloux.html`, and eight files in `ProjectSYNCS/Assets/Icons/`.
Open the sheet (SendUserFile with `display: render`, or the Browser pane). Compare at 22 px with the Unicode and the girolle beside it: is each recognisable, is the weight close to the mushroom's? Redraw anything that is not, re-run Steps 7–8.

- [ ] **Step 9: Write the README**

`tools/item-art/README.md`:

```markdown
# Item icons

The 16×16 icons of the collectibles, sets and cosmetics, drawn as text grids so a tweak is an edit.
They are exported to `ProjectSYNCS/Assets/Icons/<key>.png` (×8) and uploaded by the bot as its own
application emojis at startup (`ApplicationEmojiService`). The Champignons are a separate,
third-party pack in `Assets/Mushrooms/` — these icons are drawn in its palette (Pear36 plus eight of
the pack's colours) to match it.

- `palette.py` — the 44 colours, one character each (`.` transparent, `#` the outline).
- `icons.py` — every icon, by key, in `GROUPS` (the batches).
- `export.py [group…]` — writes the PNGs and `out/sheet_<group>.html`, which shows each icon at
  128 px, at 22 px (Discord's inline emoji size) beside the Unicode it replaces and a mushroom.
- `check.py` — grid shape, palette, a 1-pixel `#272736` outline wherever the drawing meets air,
  nothing on the frame's edge, and an exact ×8 export.
- `overrides/<key>.png` — a hand-edited 16×16 that replaces that key's grid (same checks).

Style: light from the top left, 4–7 colours, the object filling about 14×14, rarity in glints
(`y`, `w`) rather than borders. **An uploaded emoji is never replaced**: to change a committed icon,
delete its emoji in the developer portal and restart the bot.
```

- [ ] **Step 10: STOP — the owner judges the pilot**

Send `tools/item-art/out/sheet_cailloux.html` to the owner (SendUserFile, `display: render`). Wait for approval; redraw what they reject and re-send. Do not start Task 2 before an explicit yes. Then list for the owner to commit: `tools/item-art/*.py`, `tools/item-art/README.md`, `ProjectSYNCS/Assets/Icons/col.*.png` (8).

---

### Task 2: Emoji names and uploading the icons

**Files:**
- Modify: `ProjectSYNCS/Helpers/ItemEmojis.cs`
- Modify: `ProjectSYNCS/Helpers/ItemCatalog.cs` (add `IsIconKey`)
- Modify: `ProjectSYNCS/Services/ApplicationEmojiService.cs`
- Test: `$SCRATCH/iconcheck/` (console project)

**Interfaces:**
- Consumes: the PNGs in `ProjectSYNCS/Assets/Icons/` (Task 1); `tools/item-art/palette.py` (read by the harness for its colours).
- Produces: `ItemEmojis.SetKey(string setKey) -> string` (`"set." + setKey`); `ItemEmojis.EmojiName(string iconKey) -> string?`; `ItemEmojis.IconDirectory`, `ItemEmojis.MushroomDirectory` (`string`); `ItemEmojis.Sources(string mushroomDir, string iconDir) -> IEnumerable<IconSource>` with `public sealed record IconSource(string File, string Key, string? EmojiName)` (null name = skip); `ItemCatalog.IsIconKey(string key) -> bool`.

- [ ] **Step 1: Create the harness**

`$SCRATCH/iconcheck/iconcheck.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
    <NoWarn>$(NoWarn);MSB3277</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer\ProjectSYNCS\ProjectSYNCS.csproj" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.7" />
  </ItemGroup>
</Project>
```

`$SCRATCH/iconcheck/Program.cs`:

```csharp
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

const string Repo = @"C:\Users\c235773\Desktop\Sources - RME\Discord Bots\SessionOrganizer";
var iconDir = Path.Combine(Repo, "ProjectSYNCS", "Assets", "Icons");
var mushroomDir = Path.Combine(Repo, "ProjectSYNCS", "Assets", "Mushrooms");
var failures = new List<string>();
void Check(bool ok, string what) { if (!ok) failures.Add(what); }

// Every key that can have an icon: the collectibles outside Champignons, the sets, the cosmetics.
var iconKeys = ItemCatalog.All
    .Where(i => (i.Kind == ItemKind.Collectible && i.Set != ItemCatalog.MushroomSet) || i.Kind == ItemKind.Cosmetic)
    .Select(i => i.Key)
    .Concat(ItemCatalog.Sets.Select(s => ItemEmojis.SetKey(s.Key)))
    .ToList();
Check(iconKeys.Count == 105, $"expected 105 icon keys, got {iconKeys.Count}");

// Emoji names: valid, unique, and never a mushroom's.
var names = iconKeys.Select(k => (k, n: ItemEmojis.EmojiName(k))).ToList();
foreach (var (k, n) in names)
    Check(n is not null && Regex.IsMatch(n, "^[A-Za-z0-9_]{2,32}$") && !n.StartsWith(ItemEmojis.MushroomPrefix), $"{k}: bad emoji name {n}");
Check(names.Select(x => x.n).Distinct().Count() == names.Count, "two icons share an emoji name");
Check(ItemEmojis.EmojiName("food.mushroom") is null, "a food got an icon name");
Check(ItemEmojis.EmojiName("cos.accessory.collier_coquillages") == "ac_collier_coquillages", "accessory prefix");
Check(ItemCatalog.IsIconKey("set.cailloux") && !ItemCatalog.IsIconKey("set.nope") && ItemCatalog.IsIconKey("col.galet") && !ItemCatalog.IsIconKey("col.nope"), "IsIconKey");

// The folders as the bot will read them: every file is a known key, nothing skipped.
var sources = ItemEmojis.Sources(mushroomDir, iconDir).ToList();
foreach (var s in sources) Check(s.EmojiName is not null, $"{Path.GetFileName(s.File)} would be skipped");
Check(sources.Count(s => s.File.StartsWith(mushroomDir)) == 30, "the 30 mushrooms are not all read");
foreach (var s in sources.Where(s => s.File.StartsWith(iconDir)))
    Check(iconKeys.Contains(s.Key), $"{s.Key}: an icon for a key that should not have one");

// Every icon PNG: 128×128, 8×8 blocks, alpha 0/255, palette colours only.
var palette = Regex.Matches(File.ReadAllText(Path.Combine(Repo, "tools", "item-art", "palette.py")), "\"#([0-9a-f]{6})\"")
    .Select(m => Convert.ToUInt32(m.Groups[1].Value, 16)).ToHashSet();
Check(palette.Count == 44, $"palette.py: {palette.Count} colours");
foreach (var file in Directory.Exists(iconDir) ? Directory.GetFiles(iconDir, "*.png") : Array.Empty<string>())
{
    using var img = Image.Load<Rgba32>(file);
    var name = Path.GetFileName(file);
    Check(img.Width == 128 && img.Height == 128, $"{name}: {img.Width}×{img.Height}");
    if (img.Width != 128 || img.Height != 128) continue;
    for (var y = 0; y < 128; y++)
        for (var x = 0; x < 128; x++)
        {
            var p = img[x, y];
            if (!p.Equals(img[x / 8 * 8, y / 8 * 8])) { failures.Add($"{name}: not 8×8 blocks at ({x},{y})"); goto next; }
            if (p.A is not (0 or 255)) { failures.Add($"{name}: alpha {p.A}"); goto next; }
            if (p.A == 255 && !palette.Contains((uint)(p.R << 16 | p.G << 8 | p.B))) { failures.Add($"{name}: colour off-palette"); goto next; }
        }
    next:;
}

// Completeness, once the last batch is in (Task 11 makes this unconditional).
var have = sources.Select(s => s.Key).ToHashSet();
var missing = iconKeys.Where(k => !have.Contains(k)).ToList();
if (Environment.GetEnvironmentVariable("COMPLETE") == "1") Check(missing.Count == 0, $"missing icons: {string.Join(", ", missing)}");

Console.WriteLine($"icons: {have.Count(k => !k.StartsWith("col.") || iconKeys.Contains(k))} present, {missing.Count} still Unicode");
if (failures.Count > 0) { Console.WriteLine("FAIL\n" + string.Join("\n", failures)); return 1; }
Console.WriteLine("OK");
return 0;
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/iconcheck" && dotnet run`
Expected: build errors — `ItemEmojis` has no `SetKey`, `EmojiName`, `Sources`; `ItemCatalog` has no `IsIconKey`.

- [ ] **Step 3: Add the names and the folder reading to `ItemEmojis`**

In `ProjectSYNCS/Helpers/ItemEmojis.cs`, replace the summary and the `MushroomPrefix` block with the following (keep `ByKey`, `For`, `Set`, `Count`, `Markup`, `Clear` as they are):

```csharp
/// <summary>
/// The custom emoji markup of items and sets pictured by the bot's own application emojis — the
/// Champignons (Assets/Mushrooms, a third-party pack) and every other icon (Assets/Icons, drawn by
/// tools/item-art) — by icon key. Filled once the gateway is ready by
/// <see cref="Services.ApplicationEmojiService"/>; until then, and for any key missing from it,
/// each caller falls back to its catalog's Unicode emoji.
/// </summary>
```

```csharp
    // A mushroom's emoji is named after its sprite file, which is named after its key:
    // Assets/Mushrooms/girolle.png is col.girolle and the emoji shroom_girolle.
    public const string MushroomPrefix = "shroom_";

    public static string MushroomDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Mushrooms");
    public static string IconDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

    // Sets are not items, so their icon keys get a prefix of their own.
    public static string SetKey(string setKey) => "set." + setKey;

    // Every other icon is Assets/Icons/<icon key>.png, and its emoji name swaps the key's prefix for
    // a short one: Discord allows [A-Za-z0-9_] and 32 characters, and « cos.accessory. » alone would
    // eat 14 of them. The longest today is ac_collier_coquillages (22).
    private static readonly (string Key, string Emoji)[] Prefixes =
    {
        ("col.", "c_"), ("set.", "s_"), ("cos.theme.", "th_"), ("cos.title.", "ti_"),
        ("cos.accessory.", "ac_"), ("cos.grave.", "gr_"),
    };

    /// <summary>The emoji name for an icon in Assets/Icons; null for a key of no known kind.</summary>
    public static string? EmojiName(string iconKey)
    {
        foreach (var (key, emoji) in Prefixes)
            if (iconKey.StartsWith(key, StringComparison.Ordinal)) return emoji + iconKey[key.Length..];
        return null;
    }

    /// <summary>
    /// Every sprite the bot ships, with its icon key and emoji name — or a null name when the file
    /// matches nothing and must be skipped. Pure apart from listing the two folders, so the check
    /// reads them exactly as the bot does.
    /// </summary>
    public static IEnumerable<IconSource> Sources(string mushroomDir, string iconDir)
    {
        foreach (var file in PngsIn(mushroomDir))
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            var key = $"col.{slug}";
            yield return new IconSource(file, key, ItemCatalog.ByKey(key) is null ? null : MushroomPrefix + slug);
        }
        foreach (var file in PngsIn(iconDir))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            yield return new IconSource(file, key, ItemCatalog.IsIconKey(key) ? EmojiName(key) : null);
        }
    }

    private static string[] PngsIn(string dir) => Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png") : Array.Empty<string>();
```

And after the class, in the same file:

```csharp
/// <summary>A sprite file, the icon key it pictures, and its emoji name (null: skip it).</summary>
public sealed record IconSource(string File, string Key, string? EmojiName);
```

- [ ] **Step 4: Add `IsIconKey` to `ItemCatalog`**

In `ProjectSYNCS/Helpers/ItemCatalog.cs`, after `InSet`:

```csharp
    // What may carry an icon: any item, or a set by its « set.<key> » icon key.
    public static bool IsIconKey(string key) =>
        ByKey(key) is not null || Sets.Any(s => ItemEmojis.SetKey(s.Key) == key);
```

- [ ] **Step 5: Make the upload read both folders**

In `ProjectSYNCS/Services/ApplicationEmojiService.cs`:

1. Replace the class comment's first paragraph with:

```csharp
// Makes sure the bot's own application emojis exist for every sprite it ships with — the
// Champignons (Assets/Mushrooms) and the other icons (Assets/Icons: collectibles, sets and
// cosmetics) — then records their markup in ItemEmojis.
```

2. Delete the `SpriteDirectory` property.

3. Replace the `foreach` loop in `SyncAsync` with:

```csharp
        foreach (var source in ItemEmojis.Sources(ItemEmojis.MushroomDirectory, ItemEmojis.IconDirectory))
        {
            if (source.EmojiName is not { } name)
            {
                _logger.LogWarning("Sprite {File} matches no item or set; skipped.", Path.GetFileName(source.File));
                continue;
            }

            try
            {
                if (!byName.TryGetValue(name, out var emote))
                {
                    using var image = new Image(source.File);
                    emote = await _client.CreateApplicationEmoteAsync(name, image);
                    uploaded++;
                }
                ItemEmojis.Set(source.Key, ItemEmojis.Markup(emote.Name, emote.Id));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not upload the {Name} emoji; that item keeps its fallback.", name);
            }
        }
```

4. In the comment « 30 first-time uploads take a while », change `30` to `up to 135`.

- [ ] **Step 6: Find anything else that used `SpriteDirectory`**

Run: `cd "$REPO" && grep -rn "SpriteDirectory" --include=*.cs .`
Expected: no match. If the existing mushroom harness used it, point it at `ItemEmojis.MushroomDirectory`.

- [ ] **Step 7: Run the harness and the build**

Run: `cd "$SCRATCH/iconcheck" && dotnet run` → Expected: `icons: 8 present, 97 still Unicode` then `OK`.
Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` → Expected: `0 Avertissement(s)`, `0 Erreur(s)`.

- [ ] **Step 8: Hand over**

List for the owner to commit: `ItemEmojis.cs`, `ItemCatalog.cs`, `ApplicationEmojiService.cs`.

---

### Task 3: Sets and cosmetics read their icon; menus take custom emojis

**Files:**
- Modify: `ProjectSYNCS/Helpers/ItemCatalog.cs:19` (`CollectionSet`), and the cosmetics' `ItemInfo` twins in `BuildAll`
- Modify: `ProjectSYNCS/Helpers/CosmeticCatalog.cs:17-20` (`CosmeticInfo`) and its four builders (`Theme`, `Title`, `Accessory`, `Grave`, lines 176–196)
- Modify: `ProjectSYNCS/Commands/CosmeticCards.cs:44,89,125`, `ProjectSYNCS/Commands/InventoryModule.cs:207`
- Test: `$SCRATCH/iconcheck/Program.cs`

**Interfaces:**
- Consumes: `ItemEmojis.For`, `ItemEmojis.Set`, `ItemEmojis.Clear`, `ItemEmojis.SetKey` (Task 2); `EmoteMarkup.Parse(string) -> IEmote?` (existing).
- Produces: `CollectionSet(string Key, string DefaultEmoji, string Name, long Reward)` with computed `Emoji`; `CosmeticInfo(Key, Slot, Source, Rarity, Season, string DefaultEmoji, Name, NameF, Accent, Price, Recipe)` with computed `Emoji`, `Banner`, `GraveLeft`, `GraveRight`.

- [ ] **Step 1: Add the failing checks**

In `$SCRATCH/iconcheck/Program.cs`, insert before the `// Completeness` block (add `using Discord;` and `using ProjectSYNCS.Commands;` and `using ProjectSYNCS.Models;` at the top):

```csharp
// Computed emojis: the Unicode before an upload, the icon after — sets, cosmetics and their twins.
ItemEmojis.Clear();
var set = ItemCatalog.Sets.First(s => s.Key == "cailloux");
var theme = CosmeticCatalog.All.First(c => c.Key == "cos.theme.prairie");
var grave = CosmeticCatalog.All.First(c => c.Key == "cos.grave.bougies");
Check(set.Emoji == "🪨" && theme.Emoji == "🌿" && theme.Banner == "🌿 · 🌿 · 🌿 · 🌿 · 🌿" && grave.GraveLeft == "🕯️ 🕯️", "fallbacks");
Check(CosmeticCatalog.All.First(c => c.Slot == CosmeticSlot.Title).Banner is null && theme.GraveLeft is null, "banner/grave only on their slot");
string Fake(string key) => $"<:{ItemEmojis.EmojiName(key) ?? "shroom_x"}:1234567890123456789>";
ItemEmojis.Set(ItemEmojis.SetKey("cailloux"), Fake("set.cailloux"));
ItemEmojis.Set(theme.Key, Fake(theme.Key));
ItemEmojis.Set(grave.Key, Fake(grave.Key));
Check(set.Emoji == Fake("set.cailloux"), "set icon");
Check(theme.Emoji == Fake(theme.Key) && ItemCatalog.ByKey(theme.Key)!.Emoji == Fake(theme.Key), "cosmetic icon and its ItemInfo twin");
Check(theme.Banner == string.Join(" · ", Enumerable.Repeat(Fake(theme.Key), 5)), "banner follows the icon");
Check(grave.GraveLeft == $"{Fake(grave.Key)} {Fake(grave.Key)}" && grave.GraveRight == grave.GraveLeft, "grave sides follow the icon");

// Every select option carries an Emote for markup, an Emoji for Unicode — never an Emoji("<:…>").
foreach (var k in iconKeys.Concat(ItemCatalog.All.Where(i => i.Set == ItemCatalog.MushroomSet).Select(i => i.Key))) ItemEmojis.Set(k, Fake(k));
var everything = ItemCatalog.All.Select(i => new InventoryItem { Key = i.Key, Quantity = 9 }).ToList();
var owned = everything.Select(i => i.Key).ToHashSet();
var now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
var dressed = new Plynling { Id = 1, Name = "Lila", Gender = PlynlingGender.Female, ThemeKey = theme.Key, AccessoryKey = "cos.accessory.noeud", GraveKey = grave.Key };
var menus = new List<MessageComponent>
{
    CosmeticCards.BuildShop(now, new HashSet<string>(), 9999, null).Components,
    CosmeticCards.BuildCraft(everything.ToDictionary(i => i.Key, i => i.Quantity), new HashSet<string>(), 9999, null).Components,
    CosmeticCards.BuildWardrobe(dressed, CosmeticCatalog.All, null).Components,
    InventoryModule.BuildCollectionPage(1, everything, Array.Empty<CollectionCompletion>(), "cailloux", CollectionFilter.All).Components,
};
foreach (var option in menus.SelectMany(m => m.Components).OfType<ActionRowComponent>().SelectMany(r => r.Components).OfType<SelectMenuComponent>().SelectMany(s => s.Options))
    Check(option.Emote is null || option.Emote is Emote || !option.Emote.Name.StartsWith("<"), $"option {option.Label}: markup passed as a Unicode Emoji");
```

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/iconcheck" && dotnet run`
Expected: FAIL — `fallbacks` passes but `set icon`, `cosmetic icon…`, `banner follows the icon`, `grave sides follow the icon` fail (the emojis are fixed strings), and options fail with « markup passed as a Unicode Emoji ».

- [ ] **Step 3: Compute the set emoji**

`ProjectSYNCS/Helpers/ItemCatalog.cs:19`, replace the record:

```csharp
// A collection set. DefaultEmoji is its Unicode; Emoji is its own icon once uploaded (set.<key>).
public sealed record CollectionSet(string Key, string DefaultEmoji, string Name, long Reward)
{
    public string Emoji => ItemEmojis.For(ItemEmojis.SetKey(Key)) ?? DefaultEmoji;
}
```

- [ ] **Step 4: Compute the cosmetic emoji, banner and grave sides**

`ProjectSYNCS/Helpers/CosmeticCatalog.cs:17-20`, replace the record:

```csharp
public sealed record CosmeticInfo(
    string Key, CosmeticSlot Slot, CosmeticSource Source, ItemRarity Rarity, Season Season,
    string DefaultEmoji, string Name, string? NameF, uint? Accent,
    long Price, IReadOnlyList<(string ItemKey, int Count)> Recipe)
{
    // Its icon once uploaded (ItemEmojis), else its Unicode. The thème's banner and the cadre's two
    // sides are made of it, so they are read at display time and picture the icon too.
    public string Emoji => ItemEmojis.For(Key) ?? DefaultEmoji;
    public string? Banner => Slot == CosmeticSlot.Theme ? string.Join(" · ", Enumerable.Repeat(Emoji, 5)) : null;
    public string? GraveLeft => Slot == CosmeticSlot.Grave ? $"{Emoji} {Emoji}" : null;
    public string? GraveRight => GraveLeft;
}
```

Then in the four builders drop the banner and grave arguments. Replace lines 176–196 with:

```csharp
        void Theme(string key, string name, string emoji, uint accent, CosmeticSource source, ItemRarity rarity = C,
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.theme.{key}", CosmeticSlot.Theme, source, rarity, season, emoji, name, null, accent,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Theme, source, rarity), recipe ?? none));

        void Title(string key, string m, string f, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.title.{key}", CosmeticSlot.Title, source, rarity, Season.None, "🏷️", m, f, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Title, source, rarity), recipe ?? none));

        void Accessory(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C,
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.accessory.{key}", CosmeticSlot.Accessory, source, rarity, season, emoji, name, null, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Accessory, source, rarity), recipe ?? none));

        void Grave(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.grave.{key}", CosmeticSlot.Grave, source, rarity, Season.None, emoji, name, null, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Grave, source, rarity), recipe ?? none));
```

- [ ] **Step 5: Build the cosmetics' `ItemInfo` twins from the Unicode**

`ProjectSYNCS/Helpers/ItemCatalog.cs`, in `BuildAll`, change `c.Emoji` to `c.DefaultEmoji`:

```csharp
            items.Add(new ItemInfo(c.Key, ItemKind.Cosmetic, c.DefaultEmoji, CosmeticCatalog.Label(c), null, c.Rarity, c.Season, null));
```

(`ItemInfo.Emoji` then reads `ItemEmojis.For(Key)`, the same icon as the cosmetic.)

- [ ] **Step 6: Menus parse the markup**

Replace `new Emoji(c.Emoji)` with `EmoteMarkup.Parse(c.Emoji)` at `CosmeticCards.cs:44` and `:89`, `emote: new Emoji(c.Emoji)` with `emote: EmoteMarkup.Parse(c.Emoji)` at `:125`, and `new Emoji(s.Emoji)` with `EmoteMarkup.Parse(s.Emoji)` at `InventoryModule.cs:207`. Then:

Run: `cd "$REPO/ProjectSYNCS" && grep -rn "new Emoji(.*\.Emoji)" --include=*.cs .`
Expected: no match.

- [ ] **Step 7: Build, fix any leftover `Banner:`/`Emoji:` named arguments, run the harness**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` → 0 warnings, 0 errors (a compile error here names any other place that built a `CosmeticInfo` positionally; there were none at planning time).
Run: `cd "$SCRATCH/iconcheck" && dotnet run` → `OK`.

- [ ] **Step 8: Hand over**

List for the owner to commit: `ItemCatalog.cs`, `CosmeticCatalog.cs`, `CosmeticCards.cs`, `InventoryModule.cs`.

---

### Task 4: `/inventory view` in two pages, and every screen measured

**Files:**
- Modify: `ProjectSYNCS/Commands/InventoryModule.cs` (`InventoryAsync` ~line 158, `BuildInventoryEmbed` ~line 293)
- Modify: `ProjectSYNCS/Interactions/Components/InventoryComponentHandler.cs`
- Modify: `ProjectSYNCS/Commands/HelpModule.cs:79` (one word)
- Test: `$SCRATCH/iconcheck/Program.cs`

**Interfaces:**
- Consumes: the computed emojis (Task 3).
- Produces: `public enum InventoryPage { Items, Wardrobe }`; `InventoryModule.BuildInventoryPage(IReadOnlyCollection<InventoryItem> held, int setsCompleted, long balance, InventoryPage page) -> (Embed Embed, MessageComponent Components)`; custom id `inv:page:{Items|Wardrobe}` handled by `InventoryComponentHandler.OnInventoryPageAsync`. `BuildInventoryEmbed` is removed.

- [ ] **Step 1: Add the failing caps checks**

In `$SCRATCH/iconcheck/Program.cs`, after the menu checks from Task 3 (the fake markup is still set for every key), add:

```csharp
// Discord's caps with every icon uploaded and someone owning everything. Worst-case markup is the
// real emoji name plus a 19-digit id.
void Caps(string what, Embed e)
{
    Check(e.Length <= 6000, $"{what}: embed {e.Length} > 6000");
    Check((e.Description?.Length ?? 0) <= 4096, $"{what}: description {e.Description?.Length}");
    foreach (var f in e.Fields) Check(f.Value.Length <= 1024 && f.Name.Length <= 256, $"{what}: field '{f.Name}' {f.Value.Length}");
}
IEnumerable<string> Texts(IEnumerable<IMessageComponent> cs) => cs.SelectMany(c => c switch
{
    TextDisplayComponent t => new[] { t.Content },
    ContainerComponent k => Texts(k.Components),
    SectionComponent s => Texts(s.Components),
    _ => Array.Empty<string>(),
});
void CapsV2(string what, MessageComponent m) => Check(Texts(m.Components).Sum(t => t.Length) <= 4000, $"{what}: V2 text over 4000");

var done = ItemCatalog.Sets.Select(s => new CollectionCompletion { SetKey = s.Key }).ToList();
foreach (var page in Enum.GetValues<InventoryPage>())
    Caps($"inventory {page}", InventoryModule.BuildInventoryPage(everything, 6, 123456, page).Embed);
foreach (var p in new[] { InventoryModule.OverviewPage }.Concat(ItemCatalog.Sets.Select(s => s.Key)))
    foreach (var f in Enum.GetValues<CollectionFilter>())
    {
        Caps($"book {p}/{f}", InventoryModule.BuildCollectionPage(1, everything, done, p, f).Embed);
        Caps($"book {p}/{f}, nothing found", InventoryModule.BuildCollectionPage(1, Array.Empty<InventoryItem>(), Array.Empty<CollectionCompletion>(), p, f).Embed);
    }
for (var week = 0; week < 60; week++)
    Caps($"shop week {week}", CosmeticCards.BuildShop(now.AddDays(7 * week), new HashSet<string>(), 9999, "notice").Embed);
Caps("craft", CosmeticCards.BuildCraft(everything.ToDictionary(i => i.Key, i => i.Quantity), owned, 9999, "notice").Embed);
Caps("wardrobe", CosmeticCards.BuildWardrobe(dressed, CosmeticCatalog.All, "notice").Embed);
CapsV2("card", PlynlingModule.BuildCard(dressed, now, "une ligne", partnerName: "Hugo"));
var graves = Enumerable.Range(1, 12).Select(i => new Plynling { Id = i, Name = "Nom" + i, OwnerId = 1, GraveKey = grave.Key, DiedAt = now.AddDays(-i), LiveSince = now.AddDays(-i - 30) }).ToList();
CapsV2("graveyard", PlynlingGraveyardCards.BuildPage(graves, GraveSort.Recent, 1, 0, now));

// The two inventory pages carry distinct ids, the active one disabled.
var (_, itemsRow) = InventoryModule.BuildInventoryPage(everything, 6, 1, InventoryPage.Items);
var buttons = itemsRow.Components.OfType<ActionRowComponent>().SelectMany(r => r.Components).OfType<ButtonComponent>().ToList();
Check(buttons.Select(b => b.CustomId).SequenceEqual(new[] { "inv:page:Items", "inv:page:Wardrobe" }) && buttons[0].IsDisabled && !buttons[1].IsDisabled, "inventory page buttons");
```

(If Discord.Net 3.20.1 names the V2 types differently, the build error names them — use `TextDisplayComponent`/`ContainerComponent`/`SectionComponent`'s actual names; `Plynling` properties used here — `LiveSince`, `DiedAt`, `GraveKey`, `OwnerId` — exist on the model.)

- [ ] **Step 2: Run it to see it fail**

Run: `cd "$SCRATCH/iconcheck" && dotnet run`
Expected: build errors — `InventoryPage` and `BuildInventoryPage` do not exist.

- [ ] **Step 3: Split the inventory into two pages**

In `ProjectSYNCS/Commands/InventoryModule.cs`, next to `CollectionFilter` at the top:

```csharp
// /inventory view's two pages. One page no longer fits once every item has its icon: a custom
// emoji is ~35 characters where Unicode is 2, and someone holding everything passed Discord's 6000
// for a message's embeds (measured ≈ 7 100).
public enum InventoryPage { Items, Wardrobe }
```

Replace `InventoryAsync`'s last line:

```csharp
        var (embed, components) = BuildInventoryPage(held, completions.Count, balance, InventoryPage.Items);
        await RespondAsync(embed: embed, components: components, ephemeral: true);
```

Replace the whole `BuildInventoryEmbed` method with:

```csharp
    // Static and Context-free, like every other builder here, so its size is checkable. Both pages
    // share the title and the footer; the buttons carry one id per page (inv:page:{page}), so the
    // two can never collide, and the page on screen is the disabled one.
    public static (Embed Embed, MessageComponent Components) BuildInventoryPage(
        IReadOnlyCollection<InventoryItem> held, int setsCompleted, long balance, InventoryPage page)
    {
        var byKey = held.ToDictionary(i => i.Key);
        int Count(string key) => byKey.TryGetValue(key, out var row) ? row.Quantity : 0;

        var embed = new EmbedBuilder()
            .WithTitle(page == InventoryPage.Items ? "Ton inventaire" : "Ton inventaire — garde-robe")
            .WithColor(Color.Purple)
            .WithFooter($"🪨 {PebbleEconomy.Cailloux(balance)}");

        if (page == InventoryPage.Items)
        {
            var pantry = string.Join("\n", PlynlingCatalog.Foods.Select(f =>
            {
                var item = ItemCatalog.ByKey(ItemCatalog.FoodKey(f.Food))!;
                return $"{item.Emoji} {item.Name} : **{Count(item.Key)}**";
            }));
            embed.AddField("Garde-manger", pantry + "\n-# Nourrir puise ici d'abord : 1 pour ton Plynling, 2 pour celui d'un autre.");

            // One field per set (per rarity for a big one), holding only what is in hand.
            foreach (var set in ItemCatalog.Sets)
                foreach (var (label, section) in ItemCatalog.Sections(set.Key))
                {
                    var lines = section.Where(i => Count(i.Key) > 0).Select(i => $"{i.Emoji} {i.Name} ×{Count(i.Key)}").ToList();
                    if (lines.Count > 0)
                        embed.AddField($"{set.Name}{(label.Length > 0 ? $" ({label})" : "")}", string.Join("\n", lines), inline: true);
                }

            var discovered = ItemCatalog.Collectibles.Count(i => byKey.ContainsKey(i.Key));
            embed.AddField("Collection",
                $"{discovered}/{ItemCatalog.Collectibles.Count()} objets découverts · {setsCompleted}/{ItemCatalog.Sets.Count} collections complètes");
        }
        else
        {
            // What is held, one field per slot.
            var any = false;
            foreach (var slot in Enum.GetValues<CosmeticSlot>())
            {
                var owned = CosmeticCatalog.InSlot(slot).Where(c => Count(c.Key) > 0).Select(c => $"{c.Emoji} {CosmeticCatalog.ShortName(c)}").ToList();
                if (owned.Count == 0) continue;
                any = true;
                embed.AddField(CosmeticCatalog.SlotPlural(slot), string.Join(" · ", owned));
            }
            embed.WithDescription(any
                ? "Pour habiller ton Plynling : `/plynling wardrobe`."
                : "Aucun cosmétique pour l'instant. La boutique : `/inventory cosmetics`.");
        }

        var components = new ComponentBuilder()
            .WithButton("🎒 Objets", $"inv:page:{InventoryPage.Items}", page == InventoryPage.Items ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: page == InventoryPage.Items)
            .WithButton("👗 Garde-robe", $"inv:page:{InventoryPage.Wardrobe}", page == InventoryPage.Wardrobe ? ButtonStyle.Primary : ButtonStyle.Secondary,
                disabled: page == InventoryPage.Wardrobe)
            .Build();
        return (embed.Build(), components);
    }
```

- [ ] **Step 4: Handle the page buttons**

In `ProjectSYNCS/Interactions/Components/InventoryComponentHandler.cs`, update the class comment's first line to « /inventory collection's controls (the category menu and the filter buttons) and /inventory view's two pages. » and add:

```csharp
    // The inventory is ephemeral, so whoever clicks is its owner.
    [ComponentInteraction("inv:page:*", ignoreGroupNames: true)]
    public async Task OnInventoryPageAsync(string pageStr)
    {
        if (!Enum.TryParse<InventoryPage>(pageStr, out var page)) page = InventoryPage.Items;
        var userId = Context.User.Id;
        var held = await _inventory.GetAllAsync(Context.Guild.Id, userId);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, userId);
        var balance = await _inventory.BalanceAsync(Context.Guild.Id, userId);
        var (embed, components) = InventoryModule.BuildInventoryPage(held, completions.Count, balance, page);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }
```

- [ ] **Step 5: Mention the wardrobe in `/help`**

`ProjectSYNCS/Commands/HelpModule.cs:79`: « Tes objets, ton garde-manger et ta collection. » → « Tes objets, ton garde-manger, ta collection et ta garde-robe. »

- [ ] **Step 6: Build and run**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` → 0/0.
Run: `cd "$SCRATCH/iconcheck" && dotnet run` → `OK`. If a cap fails, split that screen's field (as the book does by rarity) and re-run; the /help embed is re-measured by the existing help check if one is in the scratchpad, else check `HelpModule.BuildEmbed().Length <= 6000` in this harness.

- [ ] **Step 7: Try it on the dev guild**

Run the bot (`cd "$REPO/ProjectSYNCS" && dotnet run`), wait for « Item emojis ready: … 8 uploaded now », then in the dev guild:
- `/inventory view` → Objets page; click « 👗 Garde-robe » → the page swaps in place; click back.
- `/inventory collection` → Cailloux page shows the eight pixel icons; the set menu still opens.
- `/inventory cosmetics` and `/plynling wardrobe` → menus open (Unicode for now).
Stop the bot.

- [ ] **Step 8: Hand over**

List for the owner to commit: `InventoryModule.cs`, `InventoryComponentHandler.cs`, `HelpModule.cs`.

---

### Tasks 5–10: The art batches

Each batch is one task, run the same way. **Steps, for batch `<group>`:**

- [ ] **Step 1:** Add the batch's icons to `tools/item-art/icons.py` under a `# ---- <Group> ----` header, one `icon("<group>", key, name, unicode, *rows)` each, from the brief table.
- [ ] **Step 2:** Run `cd "$ART" && python check.py` until `OK`.
- [ ] **Step 3:** Run `cd "$ART" && python export.py <group>` and look at `out/sheet_<group>.html` yourself: recognisable at 22 px, weight close to the mushroom, and **distinct from its look-alikes** named in the brief (open their sheets side by side). Redraw what fails.
- [ ] **Step 4:** Run `cd "$SCRATCH/iconcheck" && dotnet run` → `OK`.
- [ ] **Step 5: STOP** — send the sheet (SendUserFile, `display: render`), wait for the owner's yes, redraw what they reject. Then list for the owner to commit: `tools/item-art/icons.py` and the batch's PNGs. Remind them that an icon already uploaded by a bot is never replaced (delete that emoji in the portal first).

#### Task 5: `nature` (8)

| Key | Name | Unicode | Brief |
|---|---|---|---|
| `col.feuille_chene` | Feuille de chêne | 🍂 | a lobed oak leaf, autumn orange-brown (`a`/`o`/`R`) with a darker midrib, diagonal |
| `col.gland` | Gland | 🌰 | an acorn: glossy brown nut (`o`/`R`, `a` highlight) under a textured cap (`U`/`u` cross-hatch) with a tiny stem |
| `col.plume` | Plume | 🪶 | a plain feather, grey-white (`s`/`G`, `w` edge), diagonal, with its quill |
| `col.trefle` | Trèfle | ☘️ | a three-leaf clover, mid green (`L`/`l`) with a stem |
| `col.pomme_pin` | Pomme de pin | 🌲 | an upright pine cone, rows of brown scales (`U`/`j`/`u`) |
| `col.coquille_escargot` | Coquille d'escargot | 🐌 | an empty spiral snail shell, tan and cream (`J`/`j`/`w`), spiral in `U` |
| `col.trefle_quatre` | Trèfle à quatre feuilles | 🍀 | rare: four heart-shaped leaves, brighter (`l`/`Y`/`L`), one `w` glint — look-alike: `col.trefle` |
| `col.plume_doree` | Plume dorée | ✨ | legendary: the feather's shape in gold (`y`/`a`/`A`) with two sparkles — look-alikes: `col.plume`, `cos.accessory.plume_doree` |

#### Task 6: `tresors` (8)

| Key | Name | Unicode | Brief |
|---|---|---|---|
| `col.bouton` | Bouton | 🔘 | a round sewing button with four holes and a rim, red (`E`/`c`/`e`) |
| `col.bille` | Bille | 🔮 | a glass marble, blue (`B`/`b`) with a white swirl and a strong `w` highlight |
| `col.cle_rouillee` | Clé rouillée | 🗝️ | an old key, diagonal, ring bow and bit, rust (`o`/`R`/`a` flecks) |
| `col.piece_ancienne` | Pièce ancienne | 🪙 | a worn gold coin (`a`/`y`/`A`), rim and a simple star stamped in the middle |
| `col.bague` | Bague | 💍 | a gold ring seen at an angle with a pink gem (`i`/`I`) |
| `col.boussole` | Boussole | 🧭 | a brass compass case (`a`/`o`), cream face (`w`), red-and-grey needle |
| `col.carte_tresor` | Carte au trésor | 🗺️ | parchment (`J`/`j`) with rolled ends, a dotted path and a red X (`E`) |
| `col.couronne` | Couronne | 👑 | legendary: an ornate gold crown (`y`/`a`), five points, red gems (`E`), glints — look-alikes: `cos.accessory.couronne` (simpler), `cos.theme.royal` |

#### Task 7: `saisons` (8)

| Key | Name | Unicode | Brief |
|---|---|---|---|
| `col.primevere` | Primevère | 🌼 | a pale-yellow five-petal primrose (`y`/`w`), darker centre (`a`), two leaves |
| `col.cerise` | Cerise | 🍒 | two red cherries (`E`/`c`, `I` highlight) on joined stems with a leaf |
| `col.coquelicot` | Coquelicot | 🌺 | a red poppy seen from the side (`E`/`e`/`c`), dark centre (`p`), stem |
| `col.coquillage` | Coquillage | 🐚 | a scallop shell, pink-cream fan with ribs (`I`/`w`/`i`) |
| `col.chataigne` | Châtaigne | 🌰 | a glossy chestnut (`R`/`o`, pale base `J`) peeking from a split spiky husk (`L`/`l`) — look-alike: `col.gland` |
| `col.citrouille` | Citrouille | 🎃 | a round ribbed pumpkin (`a`/`f`/`o`) with a green stem (`L`) |
| `col.flocon` | Flocon | ❄️ | a six-armed snowflake, pale blue and white (`h`/`w`/`x`) |
| `col.cristal_givre` | Cristal de givre | 💎 | rare: an icy crystal cluster, cyan and blue (`x`/`B`/`h`), glints — look-alikes: `col.quartz`, `cos.grave.cristaux` |

#### Task 8: `insectes` (8)

| Key | Name | Unicode | Brief |
|---|---|---|---|
| `col.fourmi` | Fourmi | 🐜 | an ant in profile: three body parts (`u`/`U`), six thin legs, antennae |
| `col.mouche` | Mouche | 🪰 | a fly from above: dark body (`k`/`g`), big red eyes (`E`), two pale wings (`s`/`h`) |
| `col.moustique` | Moustique | 🦟 | a thin mosquito in profile: long legs, striped body (`g`/`s`), needle, clear wings |
| `col.chenille` | Chenille | 🐛 | a curled green caterpillar (`L`/`l`), round segments with pale dots, a face |
| `col.coccinelle` | Coccinelle | 🐞 | a ladybird from above, red shell (`E`/`c`) with black spots (`#`), black head |
| `col.grillon` | Grillon | 🦗 | a cricket in profile, brown-green (`t`/`L`/`u`), big hind leg, long antennae |
| `col.abeille` | Abeille | 🐝 | a bee: yellow and black stripes (`y`/`a`/`#`), white wings (`w`/`s`) |
| `col.morpho_bleu` | Morpho bleu | 🦋 | legendary: a butterfly with vivid blue wings (`B`/`x`/`b`) edged dark (`d`), glints — look-alike: `cos.grave.papillons` |

#### Task 9: `sets` (6)

| Key | Name | Unicode | Brief |
|---|---|---|---|
| `set.cailloux` | Cailloux | 🪨 | a small pile of three pebbles, greys and a warm brown |
| `set.nature` | Nature | 🍂 | an oak leaf with an acorn in front of it |
| `set.tresors` | Trésors | 💎 | a small open chest (`U`/`u` wood, `a` bands) with gold and one gem showing |
| `set.saisons` | Saisons | ❄️ | a round wheel in four quarters: green, yellow, orange, pale blue |
| `set.insectes` | Insectes | 🐞 | a glass jar (`h`/`s` glass, `w` shine) with a ladybird inside |
| `set.champignons` | Champignons | 🍄 | a wicker basket (`J`/`j`/`U`) holding two mushrooms (a red cap and a brown one) |

#### Task 10: `cosmetiques` — four sub-batches, one sheet each

Run Tasks 5–9's steps once per sub-batch, with groups `themes`, `titres`, `accessoires`, `cadres` (four STOPs). A thème's icon is the motif its banner repeats, its accent colour leading.

**`themes` (17):**

| Key | Unicode | Accent | Brief |
|---|---|---|---|
| `cos.theme.prairie` | 🌿 | #6AB04C | a tuft of grass with one small white flower |
| `cos.theme.aurore` | 🌸 | #F4A7C0 | a pink sun half-risen over a line of horizon |
| `cos.theme.ocean` | 🌊 | #2E86DE | a curling blue wave crest with foam |
| `cos.theme.braises` | 🔥 | #E8663C | glowing embers with a small flame |
| `cos.theme.foret` | 🌲 | #2D6A3E | a dark green fir tree |
| `cos.theme.lavande` | 💜 | #9B7FD4 | a lavender sprig, purple buds up a stem |
| `cos.theme.desert` | 🌵 | #D9B77E | a cactus on a little dune of sand |
| `cos.theme.arc_en_ciel` | 🌈 | #FF9FF3 | a rainbow arc resting on a cloud |
| `cos.theme.nuit_etoilee` | ✨ | #1B2A5A | a pale crescent moon with two stars (`d`/`b` night around the moon's edge only — no background) — look-alike: `cos.grave.clair_de_lune` |
| `cos.theme.aurore_boreale` | 🌌 | #2BB5A8 | three wavy ribbons of light, teal to green (`x`/`L`/`l`) |
| `cos.theme.royal` | 👑 | #E6B422 | a gold-and-purple pennant with a small crown on it — look-alikes: both couronnes |
| `cos.theme.cerisiers` | 🍒 | #F7C5D5 | a pink five-petal cherry blossom — look-alike: `col.cerise` (fruit, not flower) |
| `cos.theme.plage` | 🏖️ | #F3D9A4 | a striped parasol planted in sand |
| `cos.theme.feuilles_mortes` | 🍂 | #C06A2B | two falling leaves, orange and brown — look-alike: `col.feuille_chene` (one leaf) |
| `cos.theme.flocons` | ❄️ | #BFE3F5 | three small snowflakes — look-alike: `col.flocon` (one big) |
| `cos.theme.champignonniere` | 🍄 | #B5523B | a cluster of three red-capped mushrooms |
| `cos.theme.caverne_tresors` | 💎 | #5B3F8C | a purple cave arch with gems glinting inside |

**`titres` (13)** — each an object, since a title has no emoji of its own (all 🏷️ today):

| Key | Title | Brief |
|---|---|---|
| `cos.title.petit` | Le Petit / La Petite | a tiny baby rattle |
| `cos.title.gourmet` | Le Gourmet | a wooden spoon with a drop of honey |
| `cos.title.reveur` | Le Rêveur | a small cloud with a star |
| `cos.title.curieux` | Le Curieux | a magnifying glass |
| `cos.title.dormeur` | Le Dormeur | a striped nightcap with a pompom |
| `cos.title.mysterieux` | Le Mystérieux | a purple eye mask |
| `cos.title.farceur` | Le Farceur | a jester's cap, red and yellow, with bells |
| `cos.title.aventurier` | L'Aventurier | a bindle: a stick with a knotted bundle |
| `cos.title.champion` | Le Champion | a gold medal on a red ribbon |
| `cos.title.sage` | Le Sage | an owl |
| `cos.title.legendaire` | Le Légendaire | a gold star with glints |
| `cos.title.chercheur_tresors` | Le Chercheur de trésors | a shovel |
| `cos.title.botaniste` | Le Botaniste | a sprout in a terracotta pot |

**`accessoires` (16):**

| Key | Unicode | Brief |
|---|---|---|
| `cos.accessory.noeud` | 🎀 | a pink-red bow |
| `cos.accessory.echarpe` | 🧣 | a striped scarf, red and white, with fringes |
| `cos.accessory.fleur` | 🌼 | a white daisy, yellow centre — look-alike: `col.primevere` (yellow petals) |
| `cos.accessory.casquette` | 🧢 | a blue cap seen from the front, brim towards the viewer |
| `cos.accessory.ballon` | 🎈 | a red balloon on a string |
| `cos.accessory.lunettes` | 🕶️ | black sunglasses with a `w` glint |
| `cos.accessory.sac_a_dos` | 🎒 | a small backpack, green with brown straps |
| `cos.accessory.casque_audio` | 🎧 | headphones |
| `cos.accessory.haut_de_forme` | 🎩 | a black top hat with a red band |
| `cos.accessory.baguette` | 🪄 | a magic wand with a star tip and sparkles |
| `cos.accessory.couronne` | 👑 | a simple small gold crown, three points, one blue gem — look-alike: `col.couronne` (ornate) |
| `cos.accessory.tulipe` | 🌷 | a red tulip on a stem |
| `cos.accessory.glace` | 🍦 | an ice-cream cone with a pink scoop |
| `cos.accessory.moufles` | 🧤 | a pair of mittens |
| `cos.accessory.plume_doree` | 🪶 | a golden quill with a nib — look-alike: `col.plume_doree` (no nib) |
| `cos.accessory.collier_coquillages` | 🐚 | a necklace of small shells on an arc of string |

**`cadres` (13)** — what surrounds a grave:

| Key | Unicode | Brief |
|---|---|---|
| `cos.grave.bougies` | 🕯️ | two lit candles |
| `cos.grave.couronne_fleurs` | 💐 | a round wreath of flowers |
| `cos.grave.feuilles` | 🍂 | a small heap of fallen leaves |
| `cos.grave.galets` | 🪨 | a cairn: three stacked pebbles — look-alike: `set.cailloux` (a pile, not a stack) |
| `cos.grave.ble` | 🌾 | a sheaf of wheat |
| `cos.grave.etoiles` | ⭐ | two stars |
| `cos.grave.colombes` | 🕊️ | a white dove |
| `cos.grave.papillons` | 🦋 | two small pale-yellow butterflies |
| `cos.grave.clair_de_lune` | 🌙 | a pale crescent moon alone |
| `cos.grave.lanternes` | 🏮 | a red paper lantern |
| `cos.grave.aura_doree` | ✨ | a golden halo ring with sparkles |
| `cos.grave.cristaux` | 💎 | a crystal cluster in mixed colours (pink, blue, purple) |
| `cos.grave.cercle_fees` | 🍄 | a ring of tiny mushrooms seen from above |

---

### Task 11: Completeness, docs and version

**Files:**
- Modify: `$SCRATCH/iconcheck/Program.cs` (completeness unconditional)
- Modify: `CLAUDE.md` (the Champignons emoji note, ~line 1422)
- Modify: `ProjectSYNCS/config.yaml:3` (version)

- [ ] **Step 1: Make completeness mandatory**

In `Program.cs`, replace `if (Environment.GetEnvironmentVariable("COMPLETE") == "1") Check(` with `Check(`.
Run: `cd "$SCRATCH/iconcheck" && dotnet run` → `icons: 105 present, 0 still Unicode` then `OK`.
Run: `cd "$ART" && python check.py` → `105 icons checked`, `OK`.

- [ ] **Step 2: Document it in CLAUDE.md**

After the paragraph that starts « **The Champignons set is the one big set, and its pictures are the bot's own emojis.** » (and its continuation about `SharedPictures` / `ClearName`), add:

```markdown
**Every other collectible, the six sets and every cosmetic have icons too, drawn in the same
style.** They are ours, not the pack's: 16×16 text grids in `tools/item-art/icons.py`, in the pack's
palette (Pear36 plus eight of its colours), exported ×8 to `ProjectSYNCS/Assets/Icons/<key>.png` —
the key is the item key, or `set.<set key>` for a set. `ApplicationEmojiService` reads both folders
through `ItemEmojis.Sources`; the mushrooms keep their `shroom_` names and the rest get
`ItemEmojis.EmojiName`'s short prefixes (`c_`, `s_`, `th_`, `ti_`, `ac_`, `gr_`), since Discord caps a
name at 32 characters. `CollectionSet.Emoji` and `CosmeticInfo.Emoji` are computed like
`ItemInfo.Emoji` — the icon once uploaded, else the stored `DefaultEmoji` — and so are a thème's
`Banner` and a cadre's `GraveLeft`/`GraveRight`, which are made of it. A select option must get
`EmoteMarkup.Parse(emoji)`, never `new Emoji(emoji)`, which Discord rejects for custom markup.
**A custom emoji costs ~35 characters where Unicode costs 2**, which is why `/inventory view` is two
pages (`InventoryPage`, `inv:page:{page}`): on one, someone holding everything reached ≈ 7 100 of
the 6 000 a message's embeds may hold. Anything new that lists many items must be measured with every
icon uploaded. Like the mushrooms, an uploaded icon is never replaced: delete it in the developer
portal and restart.
```

- [ ] **Step 3: Bump the version**

`ProjectSYNCS/config.yaml:3`: `version: "5.10.9"` → `version: "5.10.10"` (or 0.0.1 above whatever it is by then).

- [ ] **Step 4: Build, run on the dev guild once more**

Run: `cd "$REPO/ProjectSYNCS" && dotnet build -warnaserror` → 0/0. Run the bot, wait for « Item emojis ready: 135 in use », and open `/inventory collection` (overview and a set), `/inventory view` (both pages), `/inventory cosmetics`, `/inventory craft`, `/plynling wardrobe`, `/plynling view` on a Plynling wearing a thème and an accessory, and `/plynling graveyard` with a cadre.

- [ ] **Step 5: Hand over**

List for the owner to commit: `CLAUDE.md`, `ProjectSYNCS/config.yaml`, and anything not yet committed from earlier tasks.
