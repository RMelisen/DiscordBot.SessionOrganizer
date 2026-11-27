"""A pixel-art linter for the Plynlings: turns "it looks a bit muddy" into pixels to look at.

    python lint.py                       # every species x face x stage, frame 0: a summary table
    python lint.py cepe                  # one species, every face, with each finding's pixels
    python lint.py cepe happy --stage baby --frames all
    python lint.py some.png other.webp   # any picture (an export is shrunk back to its art pixels)
    python lint.py --palette             # the colours of the whole set, and the near-twins among them

Advisory, like a spell-checker: a finding is a place to look, not always a fault (a lone pixel may
be a spot on purpose). Exit code 0 unless --fail is given and something was found.

What it looks for, on fully opaque pixels (shadows and glows are partly transparent and skipped):
- colours      how many a sprite uses. Pixel art reads cleaner with few; `lerp` mixes add more.
- twins        a colour so close to a more-used one (CIE76 ΔE under --twin, default 6) that the eye
               can't tell them apart: it could be that one. Each listed once, with where it sits.
- unlocked     a colour outside its tint's palette in palette_lock.py (further than ΔE 6 from
               every locked colour, so the lock kept it as drawn): `python palette.py lock` admits
               it, or redraw it. A picture file is checked against every tint's palette.
- specks       a lone pixel in a flat patch: its 4 sides and 3+ corners all one other colour.
- floating     a body pixel touching no other (mood marks — heart, sweat, « z », 💢 — and the
               dirt's wisps are left out).
- jaggies      a bump in the silhouette's left or right edge. Walking an edge, the run lengths of a
               curve should grow or shrink steadily (3,2,1 or 1,1,2,3); a run that zigzags against
               that (3,1,2,1) breaks the line. Straight steps (2,1,2,1) are fine.
"""
import argparse
import os
import sys

import palette
import zoom

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "pixel-checks"))
from pixelchecks import body_mask, floating, jaggies, specks  # noqa: E402  (shared with item-art/lint.py)

STAGES = ("adult", "baby")


lab, delta_e = palette.lab, palette.delta_e


# ---- the checks ----------------------------------------------------------------------------

def opaque_counts(im):
    counts = {}
    for c in zoom.pixels(im):
        if c[3] == 255:
            counts[c] = counts.get(c, 0) + 1
    return counts


def near_twins(counts, limit):
    """Each colour that has a more-used twin, once: (dE, the colour, the twin it could become).
    Pairs would grow as the square of the palette; this counts the colours that could go."""
    ranked = sorted(counts, key=lambda c: (-counts[c], c))
    labs = {c: lab(c) for c in ranked}
    found = []
    for i, c in enumerate(ranked[1:], 1):
        best = None
        for k in ranked[:i]:
            d = sum((p - q) ** 2 for p, q in zip(labs[c], labs[k])) ** 0.5
            if d < limit and (best is None or d < best[0]):
                best = (d, c, k)
        if best:
            found.append(best)
    return sorted(found)


def clusters(counts, limit):
    """Greedy: the most-used colour left takes every colour left within `limit` of it."""
    left = sorted(counts, key=lambda c: (-counts[c], c))
    labs = {c: lab(c) for c in left}
    groups = []
    while left:
        seed, rest = left[0], []
        group = [seed]
        for c in left[1:]:
            d = sum((p - q) ** 2 for p, q in zip(labs[c], labs[seed])) ** 0.5
            (group if d < limit else rest).append(c)
        if len(group) > 1:
            groups.append(group)
        left = rest
    return groups


def where(im, colour, cap=6):
    px = im.load()
    pts = [(x, y) for y in range(im.height) for x in range(im.width) if px[x, y] == colour]
    return pts[:cap], len(pts)


def lint(im, bare=None, twin=6.0, key=None):
    """bare: the same frame without what floats beside the body (mood marks, wisps), for the
    floating and jaggy checks; colours and specks look at the whole picture. key: the tint whose
    locked palette the colours must be in (palette.tint_key), or None for any."""
    counts = opaque_counts(im)
    on, body = body_mask(bare or im)
    return {
        "colours": len(counts),
        "unlocked": sorted(c for c in counts if palette.LOCKED and not palette.is_locked(c, key)),
        "twins": near_twins(counts, twin),
        "specks": specks(im),
        "floating": floating(on),
        "jaggies": jaggies(body),
    }


# ---- targets -----------------------------------------------------------------------------

def without_loose_bits(state, sp, f, stage, dirty):
    """The frame drawn again without what floats beside the body: mood marks (heart, sweat, « z »,
    frost, the angry mark) and the dirt's wisps and fly. Masking sprites.marks out of the full
    picture instead would notch the cap wherever a mark overlaps it."""
    import dirt
    import species
    from sprites import build
    names = ("extras", "frost", "sweat")
    saved = {n: getattr(species, n) for n in names}
    saved_stink = dirt.stink
    for n in names:
        setattr(species, n, lambda *a, **k: None)
    dirt.stink = lambda *a, **k: None
    try:
        return build(state, sp, f, shadow=False, stage=stage, dirty=dirty).convert("RGBA")
    finally:
        for n, fn in saved.items():
            setattr(species, n, fn)
        dirt.stink = saved_stink


def sprite_targets(a):
    from common import SPECIES
    from export import STATES, STAGED
    from motion import FRAMES
    from sprites import build
    species = [a.species] if a.species else list(SPECIES)
    states = [a.state] if a.state else STATES
    stages = [a.stage] if a.stage else STAGES
    frames = range(FRAMES) if a.frames == "all" else [int(a.frames)]
    for sp in species:
        for stage in stages:
            if stage != "adult" and sp not in STAGED:
                continue
            for state in states:
                for f in frames:
                    im = build(state, sp, f, shadow=False, stage=stage, dirty=a.dirty).convert("RGBA")
                    label = f"{sp} {state}" + (f" {stage}" if stage != "adult" else "") + f" f{f}"
                    yield label, im, without_loose_bits(state, sp, f, stage, a.dirty), palette.tint_key(state, a.dirty)


def file_targets(a):
    for path in a.files:
        n = zoom.frame_count(path)
        frames = range(n) if a.frames == "all" else [min(int(a.frames), n - 1)]
        for f in frames:
            yield f"{os.path.basename(path)} f{f}", zoom.from_file(path, f), None, None


# ---- reporting -----------------------------------------------------------------------------

def hexof(c):
    return zoom.hexof(c)


def pts(points, cap=8):
    s = " ".join(f"({x},{y})" for x, y in points[:cap])
    return s + (f" +{len(points) - cap}" if len(points) > cap else "")


def detail(label, im, r):
    print(f"\n{label}: {r['colours']} colours")
    for d, c, twin in r["twins"]:
        spots, n = where(im, c)
        print(f"  twin     {hexof(c)} could be {hexof(twin)} (dE {d:.1f}): {n} px at {pts(spots)}"
              + (f" +{n - len(spots)}" if n > len(spots) else ""))
    for c in r["unlocked"]:
        spots, n = where(im, c)
        print(f"  unlocked {hexof(c)}: {n} px at {pts(spots)}")
    if r["specks"]:
        print(f"  specks   {pts(r['specks'])}")
    if r["floating"]:
        print(f"  floating {pts(r['floating'])}")
    for side, row, x, around in r["jaggies"]:
        print(f"  jaggy    {side} edge, run from ({x},{row}); slopes {around}")


def palette_report(images, twin):
    counts = {}
    for _, im, _, _ in images:
        for c, n in opaque_counts(im).items():
            counts[c] = counts.get(c, 0) + n
    groups = clusters(counts, twin)
    spare = sum(len(g) - 1 for g in groups)
    print(f"{len(counts)} opaque colours across {len(images)} sprites; {spare} of them sit within dE {twin}"
          f" of a more-used one.")
    print(f"{len(groups)} clusters (the most-used colour first, then its twins), biggest first:")
    groups.sort(key=lambda g: -len(g))
    for g in groups[:30]:
        print("  " + "  ".join(f"{hexof(c)} x{counts[c]}" for c in g[:8]) + (f"  +{len(g) - 8}" if len(g) > 8 else ""))


def parse(argv):
    p = argparse.ArgumentParser(description="Pixel-art lint for the Plynling sprites or any picture.")
    p.add_argument("targets", nargs="*", help="a species, then a face; or picture files")
    p.add_argument("--stage", choices=STAGES)
    p.add_argument("--frames", default="0", help="a frame number, or all")
    p.add_argument("--dirty", action="store_true")
    p.add_argument("--twin", type=float, default=6.0, help="CIE76 dE under which two colours are twins")
    p.add_argument("--palette", action="store_true", help="the whole set's palette and its twin clusters")
    p.add_argument("--detail", action="store_true", help="every finding, even for many sprites")
    p.add_argument("--fail", action="store_true", help="exit 1 if anything is found")
    a = p.parse_args(argv)
    a.files = [t for t in a.targets if os.path.exists(t) or t.lower().endswith((".png", ".webp", ".gif"))]
    words = [t for t in a.targets if t not in a.files]
    if a.files and words:
        p.error("give either picture files or a species and a face, not both")
    a.species = words[0] if words else None
    a.state = words[1] if len(words) > 1 else None
    if a.species:
        from common import SPECIES
        if a.species not in SPECIES:
            p.error(f"unknown species {a.species!r} (one of {', '.join(SPECIES)})")
    return a


def main(argv=None):
    a = parse(sys.argv[1:] if argv is None else argv)
    targets = list(file_targets(a) if a.files else sprite_targets(a))
    if a.palette:
        palette_report(targets, a.twin)
        return 0
    results = [(label, im, lint(im, bare, a.twin, key)) for label, im, bare, key in targets]
    many = not a.detail and not a.species and len(results) > 12
    if many:
        width = max(len(l) for l, _, _ in results)
        print(f"{'sprite':<{width}}  colours  twins  unlocked  specks  floating  jaggies")
        for label, _, r in results:
            print(f"{label:<{width}}  {r['colours']:>7}  {len(r['twins']):>5}  {len(r['unlocked']):>8}  {len(r['specks']):>6}"
                  f"  {len(r['floating']):>8}  {len(r['jaggies']):>7}")
        print("\n(run with a species, or --detail, for each finding's pixels)")
    else:
        for label, im, r in results:
            detail(label, im, r)
    found = sum(len(r[k]) for _, _, r in results for k in ("twins", "unlocked", "specks", "floating", "jaggies"))
    print(f"\n{len(results)} sprites, {found} findings.")
    return 1 if a.fail and found else 0


if __name__ == "__main__":
    sys.exit(main())
