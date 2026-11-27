"""The Plynlings' locked palette: every finished frame snaps its opaque pixels to it.

The drawings mix colours freely (`lerp` gradients, the frozen / starving / sick / dirt tints
applied pixel by pixel), which left the set with hundreds of colours the eye can't tell from a
neighbour. palette_lock.py lists the colours allowed; species.finish and sprites.marks pass their
picture through snap(), which moves each opaque pixel to the nearest locked colour when one is
within LIMIT (CIE76 ΔE). A colour further from every locked one is kept as drawn, and
`python lint.py` reports it as unlocked. Partly transparent pixels (the ground shadow, the
Mycena's halo) are never touched.

    python palette.py lock            # admit the unlocked colours a drawing change brought in
    python palette.py lock --fresh    # rebuild the lock from scratch (shifts colours set-wide)
    python palette.py lock --fresh --merge-declared   # ... merging hand-picked twins too
    python palette.py compare         # before / after sheets, one per species, into out/palette/

`lock` renders every sprite the bot links (each species, stage, face and frame, clean and dirty)
and counts its colours by pixels. Every colour the code names and the drawing uses as-is (the
cap ramps, STEM, INK, WHITE… see declared()) is locked exactly; every other colour — the mixes
made from them — is locked only if no locked colour is within LIMIT, most-used first. So the
named colours keep their values, and near-twins among them (the Mycena's and the Doré's stem
ramps, squeezed by their tint; the white caps beside STEM[0]) stay for the owner to redraw:
`lint.py` lists them. `--merge-declared` merges those too, most-used first. `lock` only ever adds
to the lock: colours already locked stay, so admitting one new colour never moves the rest.
"""
import os
import sys

LIMIT = 6.0
ENABLED = True
HERE = os.path.dirname(os.path.abspath(__file__))
LOCK_FILE = os.path.join(HERE, "palette_lock.py")


# ---- colour maths ------------------------------------------------------------------------

def lab(c):
    def lin(v):
        v /= 255
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(v) for v in c[:3])
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883

    def f(t):
        return t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    fx, fy, fz = f(x), f(y), f(z)
    return 116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)


def distance(la, lb):
    return sum((p - q) ** 2 for p, q in zip(la, lb)) ** 0.5


def delta_e(a, b):
    return distance(lab(a), lab(b))


def rgb(hexcode):
    return tuple(int(hexcode[i:i + 2], 16) for i in (1, 3, 5))


def hexof(c):
    return "#%02x%02x%02x" % tuple(c[:3])


# ---- the lock ----------------------------------------------------------------------------
# One palette per tint. A frozen, starving or sick Plynling, or a dirty one, is the clean drawing
# mixed toward a tint pixel by pixel, so its colours sit a few ΔE from the clean ones: snapped
# into one shared palette, half of a sick stem fell back onto the clean cream and the tint went
# patchy. Each tint snaps only to colours drawn under that same tint.

TINTS = ("frozen", "starving", "sick")


def tint_key(state, dirty=False):
    """Which palette a frame of this face snaps to: clean, frozen, starving or sick, + _dirty."""
    return (state if state in TINTS else "clean") + ("_dirty" if dirty else "")


def _load():
    try:
        from palette_lock import LOCK
    except ImportError:
        return {}
    return {k: [rgb(h) for h in v] for k, v in LOCK.items()}


LOCKED = _load()
_LABS = {k: [lab(c) for c in v] for k, v in LOCKED.items()}
_SETS = {k: set(v) for k, v in LOCKED.items()}
_SNAP = {}


def nearest(c, key="clean"):
    """The locked colour c becomes under this tint, or c itself when none is within LIMIT."""
    c = tuple(c[:3])
    hit = _SNAP.get((key, c))
    if hit is None:
        hit = c
        colours, labs = LOCKED.get(key), _LABS.get(key)
        if colours:
            lc = lab(c)
            d, i = min((distance(lc, l), i) for i, l in enumerate(labs))
            if d < LIMIT:
                hit = colours[i]
        _SNAP[(key, c)] = hit
    return hit


def snap(im, key="clean"):
    """Every opaque pixel of im (RGBA, in place) moved to its locked colour under this tint
    (tint_key). Returns im."""
    if not ENABLED or not LOCKED.get(key):
        return im
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if c[3] == 255:
                s = nearest(c, key)
                if s != c[:3]:
                    px[x, y] = s + (255,)
    return im


def is_locked(c, key=None):
    """Whether c is a locked colour — under this tint, or under any when key is None."""
    c = tuple(c[:3])
    if key is not None:
        return c in _SETS.get(key, ())
    return any(c in s for s in _SETS.values())


# ---- building it -------------------------------------------------------------------------

def every_frame():
    """Every frame the bot links or layers, with its tint key: species x stage x face x frame,
    clean and dirty."""
    from common import SPECIES
    from export import STATES, STAGED, DIRTY_STATES
    from motion import FRAMES
    from sprites import build
    for sp in SPECIES:
        for stage in (["adult", "baby"] if sp in STAGED else ["adult"]):
            for state in STATES:
                for dirty in ([False, True] if state in DIRTY_STATES else [False]):
                    for f in range(FRAMES):
                        yield tint_key(state, dirty), build(state, sp, f, stage=stage, dirty=dirty)


def count_colours(frames):
    """{tint key: {colour: pixels}} over (key, picture) pairs."""
    counts = {}
    for key, im in frames:
        bucket = counts.setdefault(key, {})
        for c in (im.get_flattened_data() if hasattr(im, "get_flattened_data") else im.getdata()):
            if c[3] == 255:
                bucket[c[:3]] = bucket.get(c[:3], 0) + 1
    return counts


def declared():
    """Every colour the drawing code names: the RGB constants of common, sprites, species and dirt
    (SPECIES' cap ramps, STEM, INK, WHITE, MOUTH, ICE…) and each species' tinted stem ramp. These
    were picked by hand, so the lock keeps them exactly and merges the mixes made from them."""
    import common
    import dirt
    import species
    import sprites
    found = []

    def walk(v):
        if isinstance(v, tuple) and len(v) == 3 and all(isinstance(t, int) and 0 <= t <= 255 for t in v):
            found.append(v)
        elif isinstance(v, (tuple, list)):
            for k in v:
                walk(k)
        elif isinstance(v, dict):
            for k in v.values():
                walk(k)

    for mod in (common, sprites, species, dirt):
        for name, v in vars(mod).items():
            if name.isupper():
                walk(v)
    for p in common.SPECIES.values():
        walk(species.stem_palette(p))
    return list(dict.fromkeys(found))


def seeds(counts, keep=(), first=()):
    """`keep` (the current lock) and the `first` colours (the declared ones) that are drawn at all
    are seeds outright, even when two are near-twins: those were picked by hand (the eye's pure
    white shine on the cream stem), so merging them is the owner's call — lint shows the pairs.
    Then every other drawn colour, most-used first, becomes a seed unless it is within LIMIT of
    one already kept."""
    kept = list(keep)
    named = sorted((c for c in set(first) if counts.get(c) and c not in kept), key=lambda c: (-counts[c], c))
    kept += named
    labs = [lab(c) for c in kept]
    taken = set(kept)
    for c in sorted((c for c in counts if c not in taken), key=lambda c: (-counts[c], c)):
        lc = lab(c)
        if all(distance(lc, l) >= LIMIT for l in labs):
            kept.append(c)
            labs.append(lc)
    return kept


def write_lock(lock, totals, added):
    lines = ['"""Generated by `python palette.py lock`: the colours a Plynling sprite may use, one',
             'palette per tint (see palette.py). Hand edits are fine; `lock` only ever appends."""',
             "LOCK = {"]
    for key in sorted(lock, key=lambda k: (k != "clean", k)):
        colours = lock[key]
        lines.append(f"    # {len(colours)} colours; {added[key]} added by the last lock, "
                     f"which saw {totals[key]} drawn colours.")
        lines.append(f'    "{key}": [')
        for i in range(0, len(colours), 8):
            lines.append("        " + " ".join(f'"{hexof(c)}",' for c in colours[i:i + 8]))
        lines.append("    ],")
    lines.append("}")
    with open(LOCK_FILE, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("\n".join(lines) + "\n")


def lock(fresh=False, merge_declared=False):
    global ENABLED
    ENABLED = False                     # count what the drawings make, not what the lock made of it
    try:
        counts = count_colours(every_frame())
    finally:
        ENABLED = True
    named = () if merge_declared else declared()
    out, totals, added = {}, {}, {}
    for key, bucket in counts.items():
        keep = [] if fresh else LOCKED.get(key, [])
        out[key] = seeds(bucket, keep, named)
        totals[key], added[key] = len(bucket), len(out[key]) - len(keep)
    if not fresh:
        for key, colours in LOCKED.items():
            out.setdefault(key, colours)
            totals.setdefault(key, 0)
            added.setdefault(key, 0)
    write_lock(out, totals, added)
    for key in sorted(out, key=lambda k: (k != "clean", k)):
        print(f"  {key:<16} {totals[key]:>4} drawn colours -> {len(out[key]):>3} locked ({added[key]} added)")
    print(LOCK_FILE)


# ---- before / after -----------------------------------------------------------------------

def compare(outdir):
    """One sheet per species: each stage's faces (and two dirty ones) as drawn, then locked."""
    global ENABLED
    from PIL import Image, ImageDraw
    from common import SPECIES, LABEL, N
    from export import STATES, STAGED
    import zoom
    from sprites import build
    os.makedirs(outdir, exist_ok=True)
    K, GAP, TOP, LEFT = 5, 10, 30, 190
    cols = [(s, False) for s in STATES] + [("happy", True), ("sick", True)]
    T = N * K
    for sp in SPECIES:
        stages = ["adult", "baby"] if sp in STAGED else ["adult"]
        rows = [(stage, on) for stage in stages for on in (False, True)]
        out = Image.new("RGBA", (LEFT + len(cols) * (T + GAP), TOP + len(rows) * (T + GAP)), (47, 49, 54, 255))
        d = ImageDraw.Draw(out)
        for j, (state, dirty) in enumerate(cols):
            d.text((LEFT + j * (T + GAP) + T // 2, TOP // 2), state + (" sale" if dirty else ""),
                   fill=(220, 221, 222), anchor="mm", font=LABEL)
        for i, (stage, on) in enumerate(rows):
            used = set()
            ENABLED = on
            try:
                for j, (state, dirty) in enumerate(cols):
                    im = build(state, sp, 0, stage=stage, dirty=dirty)
                    used |= {c[:3] for c in zoom.pixels(im) if c[3] == 255}
                    big = im.resize((T, T), Image.NEAREST)
                    out.alpha_composite(big, (LEFT + j * (T + GAP), TOP + i * (T + GAP)))
            finally:
                ENABLED = True
            label = f"{stage}  {'locked' if on else 'as drawn'}\n{len(used)} colours"
            d.multiline_text((LEFT // 2, TOP + i * (T + GAP) + T // 2), label, fill=(220, 221, 222),
                             anchor="mm", align="center", font=LABEL)
        path = os.path.join(outdir, f"palette_{sp}.png")
        out.save(path)
        print(path)


def main(args):
    if args[:1] == ["lock"]:
        lock(fresh="--fresh" in args, merge_declared="--merge-declared" in args)
    elif args[:1] == ["compare"]:
        compare(args[1] if len(args) > 1 else os.path.join(HERE, "out", "palette"))
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    # Run as a script this file is `__main__`, a module apart from the `palette` that species.py
    # imports: ENABLED must be switched on that one, so hand over to it.
    import palette
    palette.main(sys.argv[1:])
