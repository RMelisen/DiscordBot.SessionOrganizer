"""Close-up views of one sprite, for reading and fixing single pixels.

Claude reads PNGs but can't point at a pixel on a 32x32 sprite or a x8 sheet: these views blow one
frame up with a numbered grid, list its colours, and print it as text, so a fix can name its pixel.

    python zoom.py cepe happy                   # frame 0, x20, numbered grid, colour legend
    python zoom.py cepe happy --stage baby --frame 5 --dirty --no-shadow
    python zoom.py cepe happy --letters         # each cell also shows its legend symbol
    python zoom.py cepe happy --text            # the grid as text on the console, same symbols
    python zoom.py cepe happy --strip           # the 16 frames, onion-skinned, + what moved per frame
    python zoom.py cepe happy --raw             # keep the bare 32x32 (out/zoom/raw/) to compare later
    python zoom.py cepe happy --diff            # ... then, after an edit: before | after | changes
    python zoom.py path/to/picture.webp --frame 3   # any picture; an exported one is shrunk back
                                                    # to its art pixels first

Pictures go to out/zoom/ (git-ignored); the path is printed. Nothing here writes into the bot.
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out", "zoom")
RAW = os.path.join(OUT, "raw")
SYMBOLS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#$%&*+=?@!<>/^~"
BG, FG, GRID, GRID4 = (40, 42, 47, 255), (220, 221, 222), (70, 72, 78, 255), (120, 122, 130, 255)
CHECK = ((92, 94, 100), (78, 80, 86))
CHANGED = (255, 0, 220, 255)


def font(size):
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default()


# ---- loading ---------------------------------------------------------------------------

def block_size(im):
    """The largest k such that the picture is made of k x k blocks (an export at x8 gives 8)."""
    w, h = im.size
    for k in range(min(w, h), 1, -1):
        if w % k or h % k:
            continue
        small = im.resize((w // k, h // k), Image.NEAREST)
        if small.resize((w, h), Image.NEAREST).tobytes() == im.tobytes():
            return k
    return 1


def from_file(path, frame=0):
    im = Image.open(path)
    if getattr(im, "n_frames", 1) > 1:
        im.seek(min(frame, im.n_frames - 1))
    im = im.convert("RGBA")
    k = block_size(im)
    return im.resize((im.width // k, im.height // k), Image.NEAREST) if k > 1 else im


def frame_count(path):
    with Image.open(path) as im:
        return getattr(im, "n_frames", 1)


def built(sp, state, frame=0, stage="adult", dirty=False, shadow=True):
    from sprites import build
    return build(state, sp, frame, shadow=shadow, stage=stage, dirty=dirty).convert("RGBA")


def name_of(a):
    if a.file:
        return os.path.splitext(os.path.basename(a.file))[0]
    parts = [a.species] + ([a.stage] if a.stage != "adult" else []) + [a.state] + (["dirty"] if a.dirty else [])
    return "_".join(parts)


def load(a, frame=None):
    frame = a.frame if frame is None else frame
    if a.file:
        return from_file(a.file, frame)
    return built(a.species, a.state, frame, a.stage, a.dirty, not a.no_shadow)


def frames_of(a):
    if a.file:
        return [from_file(a.file, f) for f in range(frame_count(a.file))]
    from motion import FRAMES
    return [load(a, f) for f in range(FRAMES)]


# ---- colours ---------------------------------------------------------------------------

def pixels(im):
    """Every pixel, row by row (getdata is deprecated from Pillow 12)."""
    flat = getattr(im, "get_flattened_data", None)
    return flat() if flat else im.getdata()


def legend(*ims):
    """Every colour used (alpha > 0), most used first, each with its symbol."""
    counts = {}
    for im in ims:
        for c in pixels(im):
            if c[3]:
                counts[c] = counts.get(c, 0) + 1
    ranked = sorted(counts, key=lambda c: (-counts[c], c))
    sym = {c: (SYMBOLS[i] if i < len(SYMBOLS) else "?") for i, c in enumerate(ranked)}
    return ranked, counts, sym


def hexof(c):
    s = "#%02x%02x%02x" % c[:3]
    return s if c[3] == 255 else f"{s} a{c[3]}"


def text_grid(im, sym):
    w, h = im.size
    px = im.load()
    lines = ["    " + "".join(str(x // 10) if x % 10 == 0 else " " for x in range(w)),
             "    " + "".join(str(x % 10) for x in range(w))]
    for y in range(h):
        lines.append(f"{y:>3} " + "".join(sym[px[x, y]] if px[x, y][3] else "." for x in range(w)))
    return "\n".join(lines)


# ---- drawing ---------------------------------------------------------------------------

def checker(w, h, s):
    out = Image.new("RGBA", (w * s, h * s))
    d = ImageDraw.Draw(out)
    for y in range(h):
        for x in range(w):
            d.rectangle((x * s, y * s, x * s + s - 1, y * s + s - 1), fill=CHECK[(x + y) % 2])
    return out


def cells(im, s, sym=None, mark=()):
    """The sprite at x s over a checkerboard, a grid every cell (bolder every 4), optional symbols,
    and a thick outline round every cell in `mark`."""
    w, h = im.size
    out = checker(w, h, s)
    out.alpha_composite(im.resize((w * s, h * s), Image.NEAREST))
    d = ImageDraw.Draw(out)
    for i in range(w + 1):
        d.line((i * s, 0, i * s, h * s), fill=GRID4 if i % 4 == 0 else GRID)
    for j in range(h + 1):
        d.line((0, j * s, w * s, j * s), fill=GRID4 if j % 4 == 0 else GRID)
    if sym:
        f = font(max(8, s // 2))
        px = im.load()
        for y in range(h):
            for x in range(w):
                c = px[x, y]
                if c[3]:
                    lum = 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]
                    d.text((x * s + s // 2, y * s + s // 2), sym[c], anchor="mm", font=f,
                           fill=(0, 0, 0) if lum > 140 else (255, 255, 255))
    for x, y in mark:
        d.rectangle((x * s, y * s, x * s + s - 1, y * s + s - 1), outline=CHANGED, width=max(2, s // 6))
    return out


def framed(panel, w, h, s, title=None):
    """A panel with column numbers on top, row numbers on the left, and an optional title."""
    f = font(max(9, min(13, s // 2 + 2)))
    top, left = (40 if title else 22), 26
    out = Image.new("RGBA", (left + panel.width + 6, top + panel.height + 6), BG)
    d = ImageDraw.Draw(out)
    if title:
        d.text((left, 4), title, fill=FG, font=font(14))
    for x in range(w):
        if s >= 14 or x % 2 == 0:
            d.text((left + x * s + s // 2, top - 10), str(x), anchor="mm", font=f, fill=FG)
    for y in range(h):
        if s >= 14 or y % 2 == 0:
            d.text((left - 4, top + y * s + s // 2), str(y), anchor="rm", font=f, fill=FG)
    out.alpha_composite(panel, (left, top))
    return out


def with_legend(img, ranked, counts, sym, max_rows=60):
    f = font(13)
    rows = ranked[:max_rows]
    col_w, row_h = 210, 18
    cols = max(1, -(-len(rows) // 40))
    out = Image.new("RGBA", (img.width + 12 + cols * col_w, max(img.height, 30 + min(40, len(rows)) * row_h)), BG)
    out.alpha_composite(img)
    d = ImageDraw.Draw(out)
    x0 = img.width + 12
    d.text((x0, 6), f"{len(ranked)} colours", fill=FG, font=f)
    for i, c in enumerate(rows):
        x, y = x0 + (i // 40) * col_w, 28 + (i % 40) * row_h
        d.rectangle((x, y, x + 14, y + 14), fill=c, outline=(160, 160, 160))
        d.text((x + 20, y), f"{sym[c]}  {hexof(c)}  ×{counts[c]}", fill=FG, font=f)
    return out


def row(*imgs, gap=16):
    out = Image.new("RGBA", (sum(i.width for i in imgs) + gap * (len(imgs) - 1), max(i.height for i in imgs)), BG)
    x = 0
    for i in imgs:
        out.alpha_composite(i, (x, 0))
        x += i.width + gap
    return out


def onion(cur, prev):
    """The frame, with where the previous frame had a pixel and this one has none in faint red."""
    ghost = Image.new("RGBA", cur.size, (0, 0, 0, 0))
    gp, cp, pp = ghost.load(), cur.load(), prev.load()
    for y in range(cur.height):
        for x in range(cur.width):
            if pp[x, y][3] and not cp[x, y][3]:
                gp[x, y] = (255, 70, 70, 110)
    ghost.alpha_composite(cur)
    return ghost


def changed(a, b):
    w, h = max(a.width, b.width), max(a.height, b.height)
    pa, pb = pad(a, w, h).load(), pad(b, w, h).load()
    return [(x, y) for y in range(h) for x in range(w) if pa[x, y] != pb[x, y]]


def pad(im, w, h):
    if im.size == (w, h):
        return im
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.alpha_composite(im)
    return out


def bbox(points):
    xs, ys = [p[0] for p in points], [p[1] for p in points]
    return f"x {min(xs)}-{max(xs)}, y {min(ys)}-{max(ys)}"


# ---- the views ---------------------------------------------------------------------------

def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    img.save(path)
    print(path)
    return path


def view_zoom(a):
    im = load(a)
    ranked, counts, sym = legend(im)
    if a.text:
        print(text_grid(im, sym))
        for c in ranked:
            print(f"  {sym[c]}  {hexof(c):<16} rgb{c[:3]}  x{counts[c]}")
        return
    s = a.scale
    title = f"{name_of(a)}  frame {a.frame}  ({im.width}x{im.height})"
    img = framed(cells(im, s, sym if a.letters else None), im.width, im.height, s, title)
    save(with_legend(img, ranked, counts, sym), f"{name_of(a)}_f{a.frame}")


def view_strip(a):
    frames = frames_of(a)
    s = a.scale if a.scale_given else 8
    ranked, counts, sym = legend(*frames)
    tiles = []
    print(f"{len(frames)} frames; pixels changed from the previous frame:")
    for i, f in enumerate(frames):
        prev = frames[i - 1]
        diff = changed(prev, f)
        print(f"  f{i:<2} {len(diff):>4}" + (f"   {bbox(diff)}" if diff else ""))
        tiles.append(framed(cells(onion(f, prev) if i else f, s), f.width, f.height, s,
                            f"f{i}  ({len(diff)} changed{' from the last' if i == 0 else ''})"))
    per = 4
    rows = [row(*tiles[i:i + per]) for i in range(0, len(tiles), per)]
    out = Image.new("RGBA", (max(r.width for r in rows), sum(r.height for r in rows) + 12 * (len(rows) - 1)), BG)
    y = 0
    for r in rows:
        out.alpha_composite(r, (0, y))
        y += r.height + 12
    save(out, f"{name_of(a)}_strip")


def raw_path(a):
    return os.path.join(RAW, f"{name_of(a)}_f{a.frame}.png")


def view_raw(a):
    os.makedirs(RAW, exist_ok=True)
    load(a).save(raw_path(a))
    print(raw_path(a))


def view_diff(a):
    before_path = a.diff if isinstance(a.diff, str) else raw_path(a)
    if not os.path.exists(before_path):
        sys.exit(f"nothing to compare with: {before_path} (run with --raw before the edit)")
    before, after = from_file(before_path), load(a)
    w, h = max(before.width, after.width), max(before.height, after.height)
    before, after = pad(before, w, h), pad(after, w, h)
    diff = changed(before, after)
    print(f"{len(diff)} pixels changed" + (f"   {bbox(diff)}" if diff else ""))
    for x, y in diff[:60]:
        b, c = before.getpixel((x, y)), after.getpixel((x, y))
        print(f"  ({x:>2},{y:>2})  {hexof(b) if b[3] else 'empty':<14} -> {hexof(c) if c[3] else 'empty'}")
    if len(diff) > 60:
        print(f"  ... {len(diff) - 60} more")
    s = a.scale if a.scale_given else 14
    save(row(framed(cells(before, s), w, h, s, "before"), framed(cells(after, s), w, h, s, "after"),
             framed(cells(after, s, mark=diff), w, h, s, f"changes ({len(diff)})")),
         f"{name_of(a)}_f{a.frame}_diff")


def parse(argv):
    p = argparse.ArgumentParser(description="Close-up views of one Plynling sprite or any picture.")
    p.add_argument("target", help="a species (with a state after it), or a picture file")
    p.add_argument("state", nargs="?", default="content")
    p.add_argument("--stage", default="adult", choices=["adult", "baby", "teen", "elder"])
    p.add_argument("--frame", type=int, default=0)
    p.add_argument("--dirty", action="store_true")
    p.add_argument("--no-shadow", action="store_true")
    p.add_argument("--unlocked", action="store_true", help="draw as drawn, before the palette lock")
    p.add_argument("--scale", type=int)
    p.add_argument("--letters", action="store_true", help="write each cell's legend symbol in it")
    mode = p.add_mutually_exclusive_group()
    mode.add_argument("--text", action="store_true", help="print the grid as text instead")
    mode.add_argument("--strip", action="store_true", help="every frame, onion-skinned")
    mode.add_argument("--raw", action="store_true", help="save the bare picture to compare later")
    mode.add_argument("--diff", nargs="?", const=True, metavar="BEFORE.png",
                      help="compare with a picture (default: the one --raw saved)")
    a = p.parse_args(argv)
    a.scale_given = a.scale is not None
    a.scale = a.scale or 20
    a.file = a.target if os.path.exists(a.target) or a.target.lower().endswith((".png", ".webp", ".gif")) else None
    a.species = None if a.file else a.target
    if a.species:
        from common import SPECIES
        if a.species not in SPECIES:
            p.error(f"unknown species {a.species!r} (one of {', '.join(SPECIES)})")
    return a


def main(argv=None):
    a = parse(sys.argv[1:] if argv is None else argv)
    if a.unlocked:
        import palette
        palette.ENABLED = False
    if a.strip:
        view_strip(a)
    elif a.raw:
        view_raw(a)
    elif a.diff:
        view_diff(a)
    else:
        view_zoom(a)


if __name__ == "__main__":
    main()
