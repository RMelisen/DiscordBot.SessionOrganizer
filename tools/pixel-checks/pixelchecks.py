"""The pixel-art checks shared by tools/plynling-art/lint.py and tools/item-art/lint.py: pure shape
logic on a PIL RGBA image or a set of (x, y) cells, no palette of their own. Each tool adds the
colour checks that fit its art (the Plynlings' locked palettes, the icons' 44 characters).

A tool puts this folder on sys.path *after* its own, so its own palette.py or export.py wins:
    sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "pixel-checks"))
"""


def lab(c):
    """CIE L*a*b* (D65) of an sRGB colour; CIE76 ΔE is the plain distance between two of these."""
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


def delta_e(a, b):
    return sum((p - q) ** 2 for p, q in zip(lab(a), lab(b))) ** 0.5


def specks(im):
    """A lone pixel in a flat patch: its 4 sides and 3+ corners all one other opaque colour."""
    px, w, h = im.load(), im.width, im.height
    found = []
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            p = px[x, y]
            if p[3] != 255:
                continue
            sides = {px[x + 1, y], px[x - 1, y], px[x, y + 1], px[x, y - 1]}
            if len(sides) != 1:
                continue
            c = sides.pop()
            if c == p or c[3] != 255:
                continue
            corners = [px[x + dx, y + dy] for dx in (-1, 1) for dy in (-1, 1)]
            if sum(k == c for k in corners) >= 3:
                found.append((x, y))
    return found


def pieces(on):
    """The 8-connected pieces of a set of cells, biggest first."""
    out, seen = [], set()
    for start in sorted(on):
        if start in seen:
            continue
        piece, todo = set(), [start]
        seen.add(start)
        while todo:
            x, y = todo.pop()
            piece.add((x, y))
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    n = (x + dx, y + dy)
                    if n in on and n not in seen:
                        seen.add(n)
                        todo.append(n)
        out.append(piece)
    return sorted(out, key=len, reverse=True)


def body_mask(im):
    """Opaque pixels, and the largest 8-connected piece of them."""
    w, h = im.size
    px = im.load()
    on = {(x, y) for y in range(h) for x in range(w) if px[x, y][3] == 255}
    found = pieces(on)
    return on, (found[0] if found else set())


def floating(on):
    """Cells touching no other cell, corners included."""
    return sorted((x, y) for x, y in on
                  if not any((x + dx, y + dy) in on for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy))


def runs(edge):
    """An edge as (first row, rows in the run, sideways jump to the next run), top to bottom."""
    out, rows = [], sorted(edge)
    i = 0
    while i < len(rows):
        j = i
        while j + 1 < len(rows) and rows[j + 1] == rows[j] + 1 and edge[rows[j + 1]] == edge[rows[i]]:
            j += 1
        if j + 1 < len(rows) and rows[j + 1] == rows[j] + 1:
            out.append((rows[i], j - i + 1, edge[rows[j + 1]] - edge[rows[i]]))
        i = j + 1
    return out


def _edge_jaggies(edge):
    """(row, slopes) for each zigzag in one edge's run lengths, within each stretch that keeps going
    the same way."""
    segs = runs(edge)
    arcs, cur = [], []
    for s in segs:
        if cur and (s[2] > 0) != (cur[-1][2] > 0):
            arcs.append(cur)
            cur = []
        cur.append(s)
    arcs.append(cur)
    found = []
    for arc in arcs:
        slope = [r / abs(j) for _, r, j in arc]
        for i in range(1, len(arc) - 2):
            a, b, c, d = slope[i - 1:i + 3]
            d1, d2, d3 = b - a, c - b, d - c
            if not (d1 and d2 and d3) or (d1 > 0) == (d2 > 0) or (d2 > 0) == (d3 > 0):
                continue
            before = slope[i - 2] if i >= 2 else None
            after = slope[i + 3] if i + 3 < len(slope) else None
            if a == c and b == d and (before in (None, b) or after in (None, a)):
                continue                    # a regular staircase that goes on: a straight line
            found.append((arc[i][0], [round(v, 2) for v in (a, b, c, d)]))
    return found


def jaggies(body, sides=("left", "right")):
    """Zigzags in the run lengths of the body's edges: a curve's runs should grow or shrink
    steadily (3,2,1 or 1,1,2,3); one that zigzags against that (3,1,2,1) breaks the line. Straight
    steps (2,1,2,1) are fine. sides: any of left, right (walked top to bottom), top, bottom
    (walked left to right). Returns (side, y, x, the slopes around it), (x, y) where the run starts."""
    if not body:
        return []
    found = []
    for side in sides:
        across = side in ("top", "bottom")
        cells = [(y, x) for x, y in body] if across else body
        edge = {}
        for x, y in cells:
            keep = max if side in ("right", "bottom") else min
            edge[y] = keep(edge.get(y, x), x)
        for row, around in _edge_jaggies(edge):
            x, y = (row, edge[row]) if across else (edge[row], row)
            found.append((side, y, x, around))
    return found
