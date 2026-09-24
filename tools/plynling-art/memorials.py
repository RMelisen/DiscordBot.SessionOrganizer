"""Memorials v3: extruded 3D stones, a lawn tile with thickness, escalating flowers, and a
Plynling-shaped marble statue."""
from common import (N, Grid, lerp, tone, SPECIES, SPOTS, STEM, STONE, MARBLE, GRASS, GOLD, GOLD_HI,
                   GOLD_D, lit, stone_colour, blob, up, sheet)

SOIL = dict(top=(128, 90, 62), base=(98, 68, 46), out=(50, 34, 26))
LEAF = ((110, 178, 84), (78, 140, 64), (52, 104, 50))
PETALS = [(246, 150, 170), (252, 252, 252), (255, 214, 96), (190, 162, 248), (255, 168, 108)]
HEART = (255, 222, 112)
SLATE = dict(hi=(128, 134, 156), light=(100, 106, 128), base=(76, 82, 104), shadow=(56, 60, 80),
             deep=(40, 42, 60), out=(22, 22, 32))
LAWN_CY, LAWN_RX, LAWN_RY = 25.6, 14.2, 3.1

# The one colour that identifies each species. Usually its cap, but not always: a Rosé des
# prés is white on top, so a white gem on a white statue vanished. Its pink gills are what
# make it a Rosé, so that is what its memorials carry.
def accent(sp):
    p = SPECIES[sp]
    return {"rose": p["gill"], "cepe": p["cap"][3], "mystique": p["spot"][0], "dore": p["cap"][1]}.get(sp, p["cap"][2])


def gem(g, x, y, c):
    g.put(x, y, lerp(c, (255, 255, 255), 0.55))
    g.put(x + 1, y, c)
    g.put(x, y + 1, c)
    g.put(x + 1, y + 1, lerp(c, (0, 0, 0), 0.3))


def lawn(g):
    """A tile of lawn with a visible band of soil under its front edge, so it has thickness."""
    for y in range(N):
        for x in range(N):
            nx, ny = (x + 0.5 - 16) / LAWN_RX, (y + 0.5 - LAWN_CY) / LAWN_RY
            if nx * nx + ny * ny <= 1:
                c = GRASS["hi"] if ny < -0.45 else (GRASS["dark"] if ny > 0.6 else GRASS["base"])
                g.put(x, y, c, "grass")
    for x in range(N):
        col = [y for y in range(N) if g.r[y][x] == "grass"]
        if col:
            yb = max(col)
            g.put(x, yb + 1, SOIL["top"], "soil")
            g.put(x, yb + 2, SOIL["base"], "soil")


def cast_shadow(g, cx, cy, rx, ry):
    """The memorial's shadow falls on the grass to the lower right."""
    for y in range(N):
        for x in range(N):
            nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if nx * nx + ny * ny <= 1 and g.r[y][x] == "grass":
                g.put(x, y, GRASS["dark"], "grass")


def extrude(g, shape, pal, region, depth=2):
    """Draw a front face with visible thickness: the same shape pushed up-right, the top
    of it lit and the right side in shadow, then the front face over the top."""
    rows = {}
    for x, y in shape:
        rows.setdefault(y, []).append(x)
    right = {y: max(xs) for y, xs in rows.items()}
    for k in range(depth, 0, -1):
        for x, y in shape:
            bx, by = x + k, y - k
            if (bx, by) in shape:
                continue
            side = by in right and bx > right[by]
            c = (pal["deep"] if k == depth else pal["shadow"]) if side else (pal["light"] if k == depth else pal["hi"])
            g.put(bx, by, c, region)
    for x, y, c in shape_colours(shape, pal):
        g.put(x, y, c, region)


def shape_colours(shape, pal):
    rows = {}
    for x, y in shape:
        rows.setdefault(y, []).append(x)
    y0, y1 = min(rows), max(rows)
    for x, y in shape:
        x0, x1 = min(rows[y]), max(rows[y])
        t = (x + 0.5 - x0) / (x1 + 1 - x0)
        if x == x0:
            c = pal["hi"]
        elif y == y0 or (y - 1 not in rows) or x < min(rows[y - 1]) or x > max(rows[y - 1]):
            c = pal["light"]
        elif t > 0.74 or y == y1:
            c = pal["shadow"]
        elif t < 0.3:
            c = pal["light"]
        else:
            c = pal["base"]
        yield x, y, c


def headstone_shape(x0, x1, y0, y1, r):
    shape = set()
    for y in range(y0, y1 + 1):
        k = y - y0
        inset = 0 if k >= r else r - int((max(0, r * r - (r - k) ** 2)) ** 0.5 + 0.5)
        for x in range(x0 + inset, x1 - inset + 1):
            shape.add((x, y))
    return shape


def rect_shape(x0, x1, y0, y1):
    return {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)}


# ---- flowers --------------------------------------------------------------------

def tuft(g, x, y):
    g.put(x, y, LEAF[1])
    g.put(x - 1, y - 1, LEAF[0])
    g.put(x + 1, y - 1, LEAF[1])


def tiny(g, x, y, petal):
    g.put(x, y, petal)
    g.put(x, y + 1, LEAF[1])


def bloom(g, x, y, petal, heart=HEART):
    for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
        g.put(x + dx, y + dy, petal)
    g.put(x, y, heart)
    g.put(x, y + 2, LEAF[1])
    g.put(x - 1, y + 2, LEAF[0])


def bush(g, cx, cy, rx, ry, flowers):
    for y in range(N):
        for x in range(N):
            nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if nx * nx + ny * ny <= 1:
                g.put(x, y, LEAF[0] if (nx + ny) < -0.4 else (LEAF[2] if (nx + ny) > 0.55 else LEAF[1]), "bush")
    for x, y, c in flowers:
        g.put(x, y, c)


def ivy(g, pts):
    for i, (x, y) in enumerate(pts):
        g.put(x, y, LEAF[i % 2])


def petals_on_lawn(g, pts):
    for x, y, c in pts:
        if g.r[y][x] == "grass":
            g.put(x, y, c)


def sprout(g, p, x0=24):
    cap = p["cap"]
    g.put(x0 + 1, 19, cap[4])
    for x in (x0, x0 + 2):
        g.put(x, 20, cap[4])
    g.put(x0 + 1, 20, cap[1])
    g.put(x0 - 1, 21, cap[4])
    for x in range(x0, x0 + 3):
        g.put(x, 21, cap[2] if x > x0 else cap[1])
    g.put(x0 + 3, 21, cap[4])
    if p["spots"] is SPOTS:
        g.put(x0 + 1, 21, p["spot"][0])
    g.put(x0, 22, p["gill"])
    g.put(x0 + 2, 22, p["gill"])
    g.put(x0 + 1, 22, STEM[2])
    g.put(x0 + 1, 23, STEM[3])


# ---- the marble Plynling ------------------------------------------------------------

def statue(g, p):
    """The living sprite's anatomy at two-thirds size, carved in marble: dome cap with a
    lip, gills beneath, a rounded body with little arms, a peaceful closed-eye face."""
    M = MARBLE
    cx, cy, rx, ry = 16.0, 10.6, 9.6, 6.8
    for y in range(N):                                          # cap
        for x in range(N):
            if y > 10:
                continue
            rr = ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2
            if rr > 1:
                continue
            d = ((x - 12.5) / 5.2) ** 2 + ((y - 6.0) / 3.0) ** 2
            i = 0 if d < 0.2 else (1 if d < 0.65 else 2)
            if (rr > 0.74 and (x + 0.5 > cx or y > cy - 2)) or y >= 10:
                i = max(i, 3)
            g.put(x, y, [M["hi"], M["light"], M["base"], M["shadow"], M["deep"]][i], "statue")
    for x in range(9, 23):                                      # gills
        g.put(x, 11, M["deep"] if x % 2 == 0 else M["shadow"], "statue")
    spans = {12: (13, 18), 13: (12, 19), 17: (12, 19), 18: (13, 18)}
    for y in range(12, 19):                                     # body: a lit cylinder
        x0, x1 = spans.get(y, (12, 19))
        for x in range(x0, x1 + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            nx = 2 * t - 1
            shade = nx * -0.6 + (max(0.0, 1 - nx * nx)) ** 0.5 * 0.8
            i = tone(shade, (0.86, 0.58, 0.2, -0.2), x, y)
            if y == 12:
                i = min(4, i + 1)
            g.put(x, y, [M["hi"], M["light"], M["base"], M["shadow"], M["deep"]][i], "statue")
    for y in (14, 15):                                          # arms
        g.put(11, y, M["light"], "statue")
        g.put(20, y, M["shadow"], "statue")


def statue_details(g, p):
    M = MARBLE
    for x in range(8, 24):                                      # gold trim along the cap's lip
        if g.r[10][x] == "statue":
            g.put(x, 10, GOLD if x != 10 else GOLD_HI)
    gem(g, 15, 6, p["accent"])                                  # a gem in the species' accent
    for x, y in ((13, 15), (14, 15), (17, 15), (18, 15)):       # closed, peaceful eyes
        g.put(x, y, M["deep"])
    g.put(15, 17, M["deep"])
    g.put(16, 17, M["deep"])


# ---- the five memorials -------------------------------------------------------------

def memorial(tier, sp):
    p = dict(SPECIES[sp], accent=accent(sp))
    cap = p["cap"]
    g = Grid()
    lawn(g)
    urn_pal = dict(hi=cap[0], light=cap[1], base=cap[2], shadow=cap[3], deep=cap[4])

    if tier == 1:
        cast_shadow(g, 18.5, 25.4, 6.0, 1.6)
        blob(g, 16.0, 23.2, 5.4, 2.6, STONE, "stone")
        blob(g, 15.6, 19.6, 4.0, 2.3, STONE, "stone")
        blob(g, 16.4, 16.5, 2.7, 1.9, STONE, "stone")
    elif tier == 2:
        cast_shadow(g, 19.0, 25.2, 6.0, 1.7)
        extrude(g, headstone_shape(11, 19, 14, 25, 4), STONE, "stone")
    elif tier == 3:
        cast_shadow(g, 19.5, 25.2, 8.0, 1.9)
        extrude(g, headstone_shape(9, 20, 8, 25, 6), STONE, "stone")
    elif tier == 4:
        cast_shadow(g, 19.0, 25.3, 8.0, 1.8)
        extrude(g, rect_shape(10, 20, 24, 25), STONE, "stone")
        extrude(g, rect_shape(11, 19, 19, 23), STONE, "stone")
        extrude(g, rect_shape(10, 20, 18, 18), STONE, "stone")
        for x in range(13, 18):
            g.put(x, 17, urn_pal["shadow"], "urn")                # foot
        blob(g, 15.5, 12.6, 4.8, 4.4, urn_pal, "urn")              # body
        for x in range(14, 17):
            g.put(x, 8, urn_pal["base"], "urn")                    # neck
        for x in range(12, 19):
            g.put(x, 7, urn_pal["light"], "urn")                   # lip
        for y in (11, 12, 13):                                     # handles
            g.put(10, y, GOLD, "urn")
            g.put(21, y, GOLD_D, "urn")
    else:
        cast_shadow(g, 19.5, 25.4, 9.5, 1.9)
        extrude(g, rect_shape(8, 22, 25, 25), SLATE, "plinth")
        extrude(g, rect_shape(9, 21, 20, 24), SLATE, "plinth")
        extrude(g, rect_shape(8, 22, 19, 19), SLATE, "plinth")
        statue(g, p)
        edge = [(x, y) for y in range(N) for x in range(N) if g.r[y][x] == "statue" and any(
            0 <= x + dx < N and 0 <= y + dy < N and g.r[y + dy][x + dx] == "plinth"
            for dx, dy in ((1, 0), (-1, 0), (0, 1)))]
        for x, y in edge:
            g.put(x, y, MARBLE["deep"])

    out = {"stone": STONE["out"], "marble": MARBLE["out"], "statue": MARBLE["out"], "plinth": SLATE["out"], "grass": GRASS["out"],
           "soil": SOIL["out"], "urn": cap[4], "bush": LEAF[2]}
    g.outline(lambda reg, ny: out.get(reg, STONE["out"]))

    # the lawn's front edge goes back over each memorial's foot, so it stands in the grass
    for y in range(24, N):
        for x in range(N):
            nx, ny = (x + 0.5 - 16) / LAWN_RX, (y + 0.5 - LAWN_CY) / LAWN_RY
            if nx * nx + ny * ny <= 1 and ny > 0.35 and g.r[y][x] not in ("grass", "soil"):
                g.put(x, y, GRASS["base"] if ny < 0.6 else GRASS["dark"], "grass")

    # details, and flowers that grow more lavish with each tier
    if tier == 1:
        tuft(g, 7, 24)
        tuft(g, 24, 25)
        sprout(g, p, 24)
    elif tier == 2:
        for x in range(13, 18):
            g.put(x, 18, STONE["shadow"])
        tuft(g, 6, 24)
        tiny(g, 8, 24, PETALS[1])
        tiny(g, 22, 25, PETALS[0])
        sprout(g, p, 24)
    elif tier == 3:
        for x, y in ((12, 11), (13, 10), (14, 10), (15, 10), (16, 10), (17, 11)):
            g.put(x, y, STONE["shadow"])                           # carved emblem
        g.put(14, 12, STONE["shadow"])
        g.put(15, 12, STONE["shadow"])
        for x in range(11, 19):                                    # a bevelled nameplate
            for y in range(15, 21):
                edge_dark = x == 11 or y == 15
                edge_lit = x == 18 or y == 20
                g.put(x, y, STONE["deep"] if edge_dark else (STONE["hi"] if edge_lit else STONE["light"]))
        for x in range(13, 17):
            g.put(x, 17, STONE["deep"])
        for x in range(14, 16):
            g.put(x, 18, STONE["deep"])
        ivy(g, [(9, 24), (9, 23), (10, 22), (9, 21), (10, 20), (9, 19)])
        bloom(g, 5, 23, PETALS[0])
        tiny(g, 7, 25, PETALS[2])
        tiny(g, 21, 25, PETALS[1])
        sprout(g, p, 24)
    elif tier == 4:
        for x in range(10, 22):
            if g.r[12][x] == "urn":
                g.put(x, 12, GOLD if x != 12 else GOLD_HI)          # gold band
        g.put(15, 6, GOLD_HI)                                       # lid knob
        g.put(16, 6, GOLD)
        dot = p["spot"][0] if p["spots"] is SPOTS else p["accent"]
        for x, y in ((14, 10), (17, 15), (12, 14)):
            if g.r[y][x] == "urn":
                g.put(x, y, dot)
        ivy(g, [(11, 24), (11, 23), (12, 22), (11, 21), (12, 20), (19, 23), (20, 22)])
        bush(g, 5.5, 23.0, 3.2, 2.3, [(4, 22, PETALS[0]), (6, 21, PETALS[1]), (7, 23, PETALS[2])])
        bush(g, 25.5, 23.0, 3.2, 2.3, [(24, 22, PETALS[3]), (26, 21, cap[1]), (27, 23, PETALS[0])])
        petals_on_lawn(g, [(9, 27, PETALS[0]), (22, 27, PETALS[3]), (13, 28, PETALS[1])])
    else:
        statue_details(g, p)
        for x in range(12, 19):                                     # a gold plaque
            for y in range(21, 24):
                g.put(x, y, GOLD_HI if y == 21 else (GOLD_D if y == 23 else GOLD))
        g.put(15, 22, p["accent"])
        g.put(16, 22, lerp(p["accent"], (0, 0, 0), 0.25))
        for i, x in enumerate(range(9, 22)):                        # a garland across the plinth
            g.put(x, 20, LEAF[i % 2] if i % 3 else PETALS[i % len(PETALS)])
        bush(g, 4.5, 22.6, 3.6, 2.6, [(3, 21, PETALS[0]), (5, 20, PETALS[1]), (6, 22, PETALS[2]),
                                       (3, 23, PETALS[3])])
        bush(g, 26.5, 22.6, 3.6, 2.6, [(25, 21, cap[1]), (27, 20, PETALS[0]), (28, 22, PETALS[1]),
                                        (26, 23, PETALS[4])])
        bloom(g, 8, 25, PETALS[3])
        bloom(g, 23, 26, PETALS[0])
        petals_on_lawn(g, [(11, 27, PETALS[0]), (19, 28, PETALS[1]), (14, 28, PETALS[2]),
                           (21, 27, cap[1]), (6, 27, PETALS[4])])

    im = g.image()
    if tier == 5:
        for x, y in ((3, 5), (28, 3), (27, 12), (4, 13), (24, 7)):
            im.putpixel((x, y), GOLD_HI + (230,))
    return im


if __name__ == "__main__":
    mem_sp = ["amanite", "russule", "mystique", "dore"]
    print(sheet(mem_sp, [1, 2, 3, 4, 5], lambda sp, t: memorial(t, sp), mem_sp,
                ["1 cairn", "2 petite stèle", "3 stèle gravée", "4 urne", "5 statue"],
                "plynling_memorials_v3.png"))
