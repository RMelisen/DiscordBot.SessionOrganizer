"""A pixel-art linter for the icons (items, sets, cosmetics, traits, badges): places to look, in
palette letters and grid cells, so a finding is an edit in icons.py.

    python lint.py                        every icon and frame with a finding, as a table
    python lint.py badges_paliers         groups, keys or patterns (badge.voice_*): each finding
    python lint.py col.galet --grid       ... and the grid with its row and column numbers
    python lint.py --frames               the shield, rosette and tile frames alone

Advisory, like the Plynlings' lint.py: a finding is a place to look, not always a fault. check.py
is the one that fails (palette, outline, margins); this one judges how an icon reads. Exit code 0
unless --fail is given and something was found.

A badge or trait is a frame (shield(), rosette(), tile()) and a subject. Each frame's shape is
linted once per family and its colours once per tier; an icon built on one is linted on its
subject alone (the cells that differ from its frame). A subject drawn the same on several icons
(a palier's five tiers, a set and its badges) is linted once, under the first of them.

What it looks for (the outline "#" and transparency aside):
- colours  more than --max-colours (7, the README's "4-7 colours") in an icon or a subject.
- twins    two colours within CIE76 dE --twin (7) of each other *touching*: at 22 px the line
           between them disappears (the palette's close pairs: q/v, M/n, a/A, m/z, k/T).
           Used apart, each still reads; it is the shared edge that goes.
- specks   a lone pixel in a flat patch. Left out: glints ("w" and "y" in a drawing, the tier
           glints on a frame), a "#" hole or eye, the end of a 1-pixel diagonal stroke (its own
           colour in a corner), and dot patterns (another same speck two cells along a row or
           column: a calendar's days, a grille).
- crumbs   a piece of one or two pixels on its own.
- jaggies  a zigzag in the run lengths of the silhouette's top, bottom, left or right edge, like
           3,1,2 where a curve wants 3,2,1. Every piece of 12 pixels or more is walked. A flat
           shoulder (3+ pixels sideways, where a stem meets a body) is a corner, not a curve.
- thick    a 2x2 block of outline on the silhouette (touching air, or a subject's frame).
           Inside a drawing, doubled lines are the separators the README asks for.
- dark     a stretch of 4+ fill pixels just inside the silhouette's outline within dE --dark (12)
           of Discord's dark background (#313338): the outline is about that dark already
           (dE 8.8), so on the dark theme the shape loses that edge ("k", "T", "u").
           Not checked on a subject, which sits on its frame.
"""
import argparse
import fnmatch
import os
import sys

import export
from icons import GROUPS, ICONS, SHIELD_GLINTS, SHIELD_TIERS, ROSETTE_GLINTS, TRAIT_KINDS
from icons import rosette_frame, shield_frame, tile_frame
from palette import OUTLINE, PALETTE, TRANSPARENT

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "pixel-checks"))
from pixelchecks import delta_e, floating, jaggies, pieces  # noqa: E402  (shared with plynling-art/lint.py)

GLINT_CHARS = "wy"
DISCORD_DARK = (0x31, 0x33, 0x38)
SIDES = ("top", "bottom", "left", "right")
MIN_WALKED = 12         # pieces smaller than this are not walked for jaggies
SHOULDER = 1 / 3        # a run this flat (3+ pixels sideways per row) is a corner, not a curve
DARK_RUN = 4            # a lost edge shorter than this is a corner's shading, not a lost side
SIDE4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
RGB = {ch: export.rgb(h) for ch, h in PALETTE.items()}
CHAR = {rgb: ch for ch, rgb in RGB.items()}
SHAPE = {"specks", "crumbs", "jaggies", "thick"}
COLOUR = {"colours", "twins", "dark"}


# ---- what to lint ----------------------------------------------------------------------------

class Target:
    """A grid of palette characters and the cells to judge: all of it for an item or a frame, the
    subject for an icon built on a frame. checks: which kinds of check apply."""

    def __init__(self, label, grid, cells, glints=(), frame=None, subject=False, checks=SHAPE | COLOUR):
        self.label, self.grid, self.cells, self.glints = label, grid, cells, set(glints)
        self.frame, self.subject, self.checks = frame, subject, checks
        self.alike = []                 # other icons whose subject is drawn the same

    def at(self, x, y):
        if 0 <= y < len(self.grid) and 0 <= x < len(self.grid[y]):
            return self.grid[y][x]
        return TRANSPARENT

    def outside(self, x, y):
        """Air, or for a subject the frame showing round it."""
        return self.at(x, y) == TRANSPARENT or self.subject and (x, y) not in self.cells

    def signature(self):
        if not self.cells:
            return None
        mx, my = min(x for x, _ in self.cells), min(y for _, y in self.cells)
        return frozenset((x - mx, y - my, self.at(x, y)) for x, y in self.cells)


def frames():
    """(family, tier or kind) -> (grid, glint cells). The tier glints are the frame's own."""
    out = {}
    for tier, spec in SHIELD_TIERS.items():
        out[("shield", tier)] = (shield_frame(tier), SHIELD_GLINTS[:spec[6]])
    for tier, spec in SHIELD_TIERS.items():
        out[("rosette", tier)] = (rosette_frame(tier), ROSETTE_GLINTS[:spec[6]])
    for kind in TRAIT_KINDS:
        out[("tile", kind)] = (tile_frame(kind), ())
    return out


FRAMES = frames()
FAMILY = {"badge.": "shield", "plbadge.": "rosette", "trait.": "tile"}


def grid_of(key):
    """The icon as exported (an override included), back in palette characters."""
    im = export.render(key)
    px = im.load()
    return ["".join(TRANSPARENT if px[x, y][3] == 0 else CHAR[px[x, y][:3]] for x in range(im.width))
            for y in range(im.height)]


def frame_of(key, grid):
    """The frame an icon is built on (the one it shares the most cells with), or None."""
    family = next((f for p, f in FAMILY.items() if key.startswith(p)), None)
    if not family:
        return None

    def shared(name):
        return sum(a == b for r, s in zip(grid, FRAMES[name][0]) for a, b in zip(r, s))
    return max((n for n in FRAMES if n[0] == family), key=shared)


def opaque(grid):
    return {(x, y) for y, r in enumerate(grid) for x, c in enumerate(r) if c != TRANSPARENT}


def icon_target(key):
    grid = grid_of(key)
    name = frame_of(key, grid)
    if not name:
        return Target(key, grid, opaque(grid))
    fg = FRAMES[name][0]
    cells = {(x, y) for y, r in enumerate(grid) for x, c in enumerate(r) if c != fg[y][x]}
    return Target(key, grid, cells, frame=name, subject=True, checks=SHAPE | COLOUR - {"dark"})


def frame_targets():
    """Each family's shape once (on its first tier: every tier is the same shape), then each
    tier's colours."""
    seen = set()
    for name, (grid, glints) in FRAMES.items():
        if name[0] not in seen:
            seen.add(name[0])
            yield Target(f"frame {name[0]} (shape)", grid, opaque(grid), glints=glints, checks=SHAPE)
        yield Target(f"frame {name[0]} {name[1]}", grid, opaque(grid), glints=glints, checks=COLOUR)


def merge_alike(targets):
    """One target per distinct subject drawing, the first in order (a set comes before its badges)."""
    first, out = {}, []
    for t in targets:
        sig = t.signature()
        if sig in first:
            first[sig].alike.append(t.label)
        else:
            first[sig] = t
            out.append(t)
    return out


# ---- the checks --------------------------------------------------------------------------------

def by_reading(points):
    return sorted(points, key=lambda p: (p[1], p[0]))


def colour_counts(t):
    counts = {}
    for x, y in t.cells:
        c = t.at(x, y)
        if c not in (OUTLINE, TRANSPARENT):
            counts[c] = counts.get(c, 0) + 1
    return counts


CLOSE = {}


def close(a, b, limit):
    if (a, b) not in CLOSE:
        CLOSE[a, b] = CLOSE[b, a] = delta_e(RGB[a], RGB[b])
    return CLOSE[a, b] < limit


def twins(t, counts, limit):
    """Pairs of different colours closer than `limit` that touch: (dE, less-used, more-used,
    the less-used one's cells on the shared edge). A neighbour may be the frame's."""
    edges = {}
    for x, y in t.cells:
        c = t.at(x, y)
        if c in (OUTLINE, TRANSPARENT):
            continue
        for dx, dy in SIDE4:
            n = t.at(x + dx, y + dy)
            if n not in (c, OUTLINE, TRANSPARENT) and close(c, n, limit):
                edges.setdefault(frozenset((c, n)), set()).add((x, y) if counts.get(c, 0) <= counts.get(n, 0)
                                                                 else (x + dx, y + dy))
    found = []
    for pair, cells in edges.items():
        a, b = sorted(pair, key=lambda c: (counts.get(c, 0), c))
        found.append((CLOSE[a, b], a, b, by_reading(cells)))
    return sorted(found)


def specks(t):
    """A lone pixel whose 4 sides and 3+ corners are one other colour, in the judged cells."""
    lone = []
    for x, y in by_reading(t.cells):
        p = t.at(x, y)
        if p in (TRANSPARENT, OUTLINE) or (x, y) in t.glints or (not t.glints and p in GLINT_CHARS):
            continue
        sides = {t.at(x + dx, y + dy) for dx, dy in SIDE4}
        if len(sides) != 1:
            continue
        c = sides.pop()
        if c in (p, TRANSPARENT):
            continue
        corners = [t.at(x + dx, y + dy) for dx in (-1, 1) for dy in (-1, 1)]
        if sum(k == c for k in corners) >= 3 and p not in corners:      # p in a corner: a diagonal stroke
            lone.append((x, y))
    spots = set(lone)
    return [(x, y) for x, y in lone
            if not any((x + dx, y + dy) in spots and t.at(x + dx, y + dy) == t.at(x, y)
                       for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2)))]


def crumbs(t):
    on = {p for p in t.cells if t.at(*p) != TRANSPARENT}
    small = {p for piece in pieces(on) if len(piece) <= 2 for p in piece}
    return by_reading(small | set(floating(on)))


def edge_jaggies(t):
    on = {p for p in t.cells if t.at(*p) != TRANSPARENT}
    found = []
    for piece in pieces(on):
        if len(piece) >= MIN_WALKED:
            found += [j for j in jaggies(piece, SIDES) if min(j[3]) > SHOULDER + 1e-9]
    return found


def thick(t):
    found = []
    for x, y in by_reading(t.cells):
        block = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
        if not all(p in t.cells and t.at(*p) == OUTLINE for p in block):
            continue
        if any(t.outside(bx + dx, by + dy) for bx, by in block for dx, dy in SIDE4):
            found.append((x, y))
    return found


def dark_edge(t, limit):
    """Stretches of fill pixels 4-next to an outline pixel that itself touches air, too close to
    Discord's dark background to show where the shape ends."""
    near = {c for c in RGB if c != OUTLINE and delta_e(RGB[c], DISCORD_DARK) < limit}
    lost = set()
    for x, y in t.cells:
        if t.at(x, y) not in near:
            continue
        for dx, dy in SIDE4:
            ox, oy = x + dx, y + dy
            if t.at(ox, oy) == OUTLINE and any(t.at(ox + ex, oy + ey) == TRANSPARENT for ex, ey in SIDE4):
                lost.add((x, y))
                break
    return by_reading(p for piece in pieces(lost) if len(piece) >= DARK_RUN for p in piece)


KINDS = ("colours", "twins", "specks", "crumbs", "jaggies", "thick", "dark")


def lint(t, a):
    counts = colour_counts(t)
    r = {"counts": counts, **{k: [] for k in KINDS}}
    run = t.checks
    if "colours" in run and len(counts) > a.max_colours:
        r["colours"] = [len(counts)]
    if "twins" in run:
        r["twins"] = twins(t, counts, a.twin)
    if "dark" in run:
        r["dark"] = dark_edge(t, a.dark)
    if "specks" in run:
        r["specks"] = specks(t)
    if "crumbs" in run:
        r["crumbs"] = crumbs(t)
    if "jaggies" in run:
        r["jaggies"] = edge_jaggies(t)
    if "thick" in run:
        r["thick"] = thick(t)
    return r


def found(r):
    return sum(len(r[k]) for k in KINDS)


TIER_SUFFIX = tuple("_" + t for t in SHIELD_TIERS)


def merge_tiers(results):
    """Tiers of one badge (badge.voice_bronze, _silver…) whose findings are the same, as one row: a
    subject pixel that matches one tier's field drops out of that tier's subject, so the drawings
    differ while what is wrong with them does not. A tier that differs (a subject colour touching
    that tier's field) keeps its own row."""
    first, out = {}, []
    for t, r in results:
        base = t.label[:-len(next(s for s in TIER_SUFFIX if t.label.endswith(s)))] \
            if t.label.endswith(TIER_SUFFIX) and t.subject else None
        mark = (base, repr([r[k] for k in KINDS])) if base else None
        if mark in first:
            keep = first[mark]
            keep.alike += [t.label] + t.alike
        else:
            if mark:
                first[mark] = t
            out.append((t, r))
    return out


# ---- reporting ---------------------------------------------------------------------------------

def pts(points, cap=8):
    s = " ".join(f"({x},{y})" for x, y in points[:cap])
    return s + (f" +{len(points) - cap}" if len(points) > cap else "")


def lettered(t, points, cap=8):
    s = " ".join(f"{t.at(x, y)}({x},{y})" for x, y in points[:cap])
    return s + (f" +{len(points) - cap}" if len(points) > cap else "")


def label(t):
    return t.label + (f" +{len(t.alike)} alike" if t.alike else "")


def print_grid(t):
    w = len(t.grid[0])
    print("      " + "".join(str(x // 10) if x >= 10 else " " for x in range(w)))
    print("      " + "".join(str(x % 10) for x in range(w)))
    for y, r in enumerate(t.grid):
        alone = "".join(c if (x, y) in t.cells else "," for x, c in enumerate(r))
        print(f"  {y:>2}  {r}" + (f"   {alone}" if t.subject else ""))
    if t.subject:
        print("        (right: the subject alone, \",\" where the frame shows)")


def detail(t, r, a):
    on = f" (subject on {t.frame[0]} {t.frame[1]})" if t.subject else ""
    used = " ".join(f"{c}x{n}" for c, n in sorted(r["counts"].items(), key=lambda kv: (-kv[1], kv[0])))
    print(f"\n{t.label}{on}: {len(r['counts'])} colours  {used}")
    if t.alike:
        print(f"  alike    {', '.join(t.alike)}")
    if r["colours"]:
        print(f"  colours  {r['colours'][0]}, over {a.max_colours}")
    for d, less, more, cells in r["twins"]:
        print(f"  twin     {less} {PALETTE[less]} touches {more} {PALETTE[more]} (dE {d:.1f}): {less} at {pts(cells, 6)}")
    if r["specks"]:
        print(f"  specks   {lettered(t, r['specks'])}")
    if r["crumbs"]:
        print(f"  crumbs   {lettered(t, r['crumbs'])}")
    for side, y, x, around in r["jaggies"]:
        print(f"  jaggy    {side} edge, run from ({x},{y}); slopes {around}")
    if r["thick"]:
        print(f"  thick    2x2 outline from {pts(r['thick'])}")
    if r["dark"]:
        print(f"  dark     {lettered(t, r['dark'])}: lost against the dark theme")
    if a.grid:
        print_grid(t)


def table(rows):
    width = max(len(label(t)) for t, _ in rows)
    print(f"{'icon':<{width}}  " + "  ".join(f"{k:>7}" for k in KINDS))
    for t, r in rows:
        cells = [str(r["colours"][0]) if r["colours"] else "."] + [str(len(r[k])) if r[k] else "." for k in KINDS[1:]]
        print(f"{label(t):<{width}}  " + "  ".join(f"{c:>7}" for c in cells))


def parse(argv):
    p = argparse.ArgumentParser(description="Pixel-art lint for the item icons, badges and traits.")
    p.add_argument("targets", nargs="*", help="groups, icon keys, or key patterns (badge.voice_*)")
    p.add_argument("--frames", action="store_true", help="the shield, rosette and tile frames alone")
    p.add_argument("--twin", type=float, default=7.0, help="CIE76 dE under which two touching colours are twins")
    p.add_argument("--max-colours", type=int, default=7, help="colours over which an icon is busy")
    p.add_argument("--dark", type=float, default=12.0, help="dE to Discord's dark background of a lost edge")
    p.add_argument("--grid", action="store_true", help="print each icon's grid with row and column numbers")
    p.add_argument("--detail", action="store_true", help="every finding, even for many icons")
    p.add_argument("--all", action="store_true", help="list the icons with no finding too")
    p.add_argument("--fail", action="store_true", help="exit 1 if anything is found")
    a = p.parse_args(argv)
    keys = []
    for t in a.targets:
        hits = GROUPS.get(t) or [k for k in ICONS if fnmatch.fnmatchcase(k, t)]
        if not hits:
            p.error(f"{t!r} is no group, key or pattern; groups: {', '.join(GROUPS)}")
        keys += [k for k in hits if k not in keys]
    a.keys = keys
    return a


def main(argv=None):
    a = parse(sys.argv[1:] if argv is None else argv)
    icons = [] if a.frames else merge_alike([icon_target(k) for k in (a.keys or ICONS)])
    every = ([] if a.keys else list(frame_targets())) + icons
    results = merge_tiers([(t, lint(t, a)) for t in every])
    icons = [t for t, _ in results if not t.label.startswith("frame ")]
    few = a.keys and len(results) <= 12
    if a.detail or few:
        for t, r in results:
            if a.all or few or found(r):
                detail(t, r, a)
    else:
        shown = [(t, r) for t, r in results if a.all or found(r)]
        if shown:
            table(shown)
        print("\n(name groups or keys, or add --detail, for each finding's cells; --grid for the grids)")
    total = sum(found(r) for _, r in results)
    drawn = sum(1 + len(t.alike) for t in icons)
    flagged = sum(1 for _, r in results if found(r))
    frame_checks = sum(1 for t in every if t.label.startswith("frame "))
    print(f"\n{drawn} icons as {len(icons)} distinct rows, {frame_checks} frame checks; "
          f"{flagged} with findings, {total} findings.")
    return 1 if a.fail and total else 0


if __name__ == "__main__":
    sys.exit(main())
