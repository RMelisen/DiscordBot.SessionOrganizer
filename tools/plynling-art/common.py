"""Shared palette, species table, pixel grid and sheet helpers for the Plynling art."""
import math
from PIL import Image, ImageDraw, ImageFont

N = 32
LABEL = ImageFont.truetype("arial.ttf", 13)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


SPOTS = [(8, 10, 1.7), (16, 5, 2.0), (23, 9, 1.5), (12, 12, 1.1), (20, 12, 1.0), (5, 12, 0.9)]
SCALES = [(9, 9, 0.8), (18, 6, 0.8), (22, 11, 0.7), (13, 11, 0.6), (6, 11, 0.6)]
SPECIES = {
    "amanite":  dict(tier="commun", cap=[(255, 160, 142), (238, 96, 86), (206, 50, 58), (148, 30, 50), (92, 20, 40)],
                     spot=((252, 247, 238), (212, 198, 190)), spots=SPOTS, gill=(238, 222, 200), sparkle=None),
    "cepe":     dict(tier="commun", cap=[(230, 178, 120), (190, 132, 80), (152, 98, 58), (110, 68, 42), (70, 42, 30)],
                     spot=((208, 158, 106), (158, 108, 66)), spots=SCALES, gill=(206, 196, 120), sparkle=None),
    "rose":     dict(tier="commun", cap=[(255, 255, 252), (248, 244, 236), (226, 218, 206), (182, 172, 162), (122, 112, 106)],
                     spot=((238, 228, 214), (208, 196, 182)), spots=SCALES, gill=(236, 146, 158), sparkle=None),
    "russule":  dict(tier="peu commun", cap=[(206, 240, 176), (152, 210, 122), (98, 170, 88), (60, 120, 64), (34, 74, 44)],
                     spot=((182, 228, 152), (122, 188, 106)), spots=SCALES[:3], gill=(244, 238, 222), sparkle=None),
    "mystique": dict(tier="rare", cap=[(214, 186, 255), (170, 130, 242), (126, 82, 204), (84, 52, 152), (50, 30, 100)],
                     spot=((176, 252, 255), (92, 200, 232)), spots=SPOTS, gill=(206, 196, 240), sparkle=(176, 252, 255),
                     stem_tint=((196, 176, 226), 0.3)),     # a Mycena's stem: lavender, near see-through
    "dore":     dict(tier="légendaire", cap=[(255, 248, 186), (252, 216, 96), (224, 170, 42), (162, 112, 26), (100, 66, 18)],
                     spot=((255, 255, 240), (236, 206, 140)), spots=SPOTS, gill=(250, 232, 176), sparkle=(255, 250, 205),
                     stem_tint=((252, 216, 96), 0.55)),     # a chanterelle is gold from cap to foot
}
STEM = [(255, 252, 242), (250, 240, 220), (238, 222, 196), (212, 190, 162), (178, 154, 126)]
STEM_OUT, INK = (128, 102, 84), (44, 28, 40)
PINK, TEAR, MOUTH = (246, 150, 162), (140, 206, 255), (204, 64, 76)


class Grid:
    def __init__(self):
        self.c = [[None] * N for _ in range(N)]
        self.r = [[None] * N for _ in range(N)]

    def put(self, x, y, c, region=None):
        if 0 <= x < N and 0 <= y < N:
            self.c[y][x] = c
            if region:
                self.r[y][x] = region

    def get(self, x, y):
        return self.c[y][x] if 0 <= x < N and 0 <= y < N else None

    def outline(self, colour_for):
        """Selective outline: every empty pixel touching a region takes that region's dark tone."""
        filled = [[self.c[y][x] is not None for x in range(N)] for y in range(N)]
        for y in range(N):
            for x in range(N):
                if filled[y][x]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < N and 0 <= ny < N and filled[ny][nx]:
                        self.put(x, y, colour_for(self.r[ny][nx], ny))
                        break

    def image(self, alpha=255):
        im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        for y in range(N):
            for x in range(N):
                if self.c[y][x] is not None:
                    im.putpixel((x, y), self.c[y][x] + (alpha,))
        return im


def tone(shade, cuts, x, y, band=0.018):
    for i, c in enumerate(cuts):
        if shade > c + band:
            return i
        if shade > c - band:
            return i + ((x + y) % 2)
    return len(cuts)


# ---- the living Plynling --------------------------------------------------------

def build(state, sp, frame=0, shadow=True):
    p = SPECIES[sp]
    cap = p["cap"]
    dy = 1 if frame == 1 else 0
    g = Grid()

    # gills (v3): the underside, seen between the rim and the stem
    for y, (a, b) in ((15 + dy, (3, 28)), (16 + dy, (6, 25))):
        for x in range(a, b + 1):
            c = p["gill"]
            if (x - 16) % 2 == 0:
                c = lerp(c, cap[4], 0.25)
            if abs(x + 0.5 - 16) < 7:
                c = lerp(c, cap[4], 0.3)
            g.put(x, y, c, "cap")

    # stem (v3): a lit cylinder
    spans = {16: (12, 19), 17: (11, 20), 26: (11, 20), 27: (12, 19)}
    for y in range(16 + dy, 28):
        x0, x1 = spans[y - dy] if y - dy in (16, 17) else (spans[y] if y in (26, 27) else (10, 21))
        for x in range(x0, x1 + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            nx = 2 * t - 1
            nz = math.sqrt(max(0.0, 1 - nx * nx))
            i = tone(nx * -0.6 + nz * 0.8, (0.86, 0.58, 0.2, -0.2), x, y)
            if y <= 17 + dy or y >= 26:
                i = min(4, i + 1)
            g.put(x, y, STEM[i], "stem")
    for y in (21 + dy, 22 + dy):
        g.put(9, y, STEM[2], "stem")
        g.put(22, y, STEM[3], "stem")
    for x in (12, 13, 18, 19):
        g.put(x, 28, STEM[3], "stem")

    # cap (v2): a soft sheen top-left, dithered into the shadow on the far side
    cx, cy, rx, ry = 15.5, 14.0 + dy, 14.6, 11.4
    for y in range(N):
        for x in range(N):
            if y > 14 + dy:
                continue
            rr = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if rr > 1:
                continue
            d = ((x - 10) / 8.5) ** 2 + ((y - (6.5 + dy)) / 5.2) ** 2
            i = 0 if d < 0.16 else (1 if d < 0.55 else 2)
            far = x > cx or y > cy - 3
            if rr > 0.78 and far:
                i = max(i, 3)
            elif 0.6 < rr <= 0.78 and far and (x + y) % 2 == 0:
                i = max(i, 3)
            if y >= 13 + dy:
                i = max(i, 3)
            g.put(x, y, cap[i], "cap")
    for sx, sy, r in p["spots"]:
        for y in range(N):
            for x in range(N):
                if (x - sx) ** 2 + (y - sy - dy) ** 2 <= r * r and g.r[y][x] == "cap" and y < 13 + dy:
                    g.put(x, y, p["spot"][1] if (x - sx) + (y - sy - dy) > r * 0.55 else p["spot"][0])

    g.outline(lambda reg, ny: INK if ny >= 27 else (cap[4] if reg == "cap" else STEM_OUT))

    f = dy

    def eyes_open():
        for ox in (12, 18):
            for x, y in ((ox, 20), (ox + 1, 20), (ox, 21), (ox + 1, 21)):
                g.put(x, y + f, INK)
            g.put(ox, 20 + f, (255, 255, 255))

    def eyes_line():
        for ox in (11, 18):
            for x in (ox, ox + 1, ox + 2):
                g.put(x, 21 + f, INK)

    if state == "happy":
        for x, y in ((11, 21), (12, 20), (13, 20), (14, 21), (17, 21), (18, 20), (19, 20), (20, 21)):
            g.put(x, y + f, INK)
        for x, y in ((14, 23), (17, 23), (15, 24), (16, 24)):
            g.put(x, y + f, INK)
        g.put(15, 23 + f, MOUTH)
        g.put(16, 23 + f, MOUTH)
        for x in (10, 11, 20, 21):
            g.put(x, 22 + f, PINK)
    elif state == "content":
        eyes_line() if frame == 1 else eyes_open()
        for x, y in ((14, 23), (15, 24), (16, 24), (17, 23)):
            g.put(x, y + f, INK)
        for x in (10, 11, 20, 21):
            g.put(x, 22 + f, lerp(PINK, STEM[2], 0.5))
    elif state == "sad":
        eyes_open()
        for x, y in ((11, 18), (12, 18), (13, 17), (18, 17), (19, 18), (20, 18)):
            g.put(x, y + f, INK)
        for x, y in ((15, 23), (16, 23), (14, 24), (17, 24)):
            g.put(x, y + f, INK)
        g.put(13, 22 + f, TEAR)
        g.put(13, 23 + f, TEAR)
    elif state == "hungry":
        eyes_open()
        for x, y in ((15, 23), (16, 23), (14, 24), (17, 24), (15, 25), (16, 25)):
            g.put(x, y + f, INK)
        g.put(15, 24 + f, MOUTH)
        g.put(16, 24 + f, MOUTH)
        for x, y in ((25, 17), (25, 18), (24, 18), (25, 19)):
            g.put(x, y, TEAR)
        g.put(25, 17, (230, 246, 255))
    elif state == "frozen":
        eyes_line()
        for x in range(14, 18):
            g.put(x, 24 + f, INK)

    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    if shadow:
        for x in range(8, 24):
            im.putpixel((x, 29), (0, 0, 0, 58))
        for x in range(10, 22):
            im.putpixel((x, 30), (0, 0, 0, 38))
    for y in range(N):
        for x in range(N):
            c = g.c[y][x]
            if c is not None:
                im.putpixel((x, y), (lerp(c, (176, 226, 255), 0.45) if state == "frozen" else c) + (255,))
    if state == "frozen":
        for x, y in ((4, 16), (4, 17), (7, 16), (24, 16), (27, 16), (27, 17), (26, 16)):
            im.putpixel((x, y), (206, 242, 255, 255))
        for x, y in ((9, 5), (10, 4), (15, 3), (20, 4)):
            im.putpixel((x, y), (255, 255, 255, 255))
        for x, y in ((28, 2), (27, 3), (28, 3), (29, 3), (28, 4)):
            im.putpixel((x, y), (220, 246, 255, 255))
    if p["sparkle"] and state != "frozen":
        s = p["sparkle"]
        for x, y in ([(2, 6), (29, 9), (27, 1), (3, 1)] if frame == 0 else [(1, 9), (30, 5), (26, 3), (5, 2)]):
            im.putpixel((x, y), s + (255,))
        for x, y in ((28, 13), (29, 12), (28, 12), (27, 12), (28, 11)):
            im.putpixel((x, y), s + (200,))
    return im


# ---- memorials --------------------------------------------------------------------

STONE = dict(hi=(230, 228, 236), light=(204, 202, 214), base=(170, 168, 184), shadow=(132, 130, 150),
             deep=(102, 100, 120), out=(52, 48, 66))
MARBLE = dict(hi=(255, 255, 255), light=(246, 246, 252), base=(224, 224, 236), shadow=(188, 188, 206),
              deep=(152, 152, 176), out=(66, 62, 88))
GRASS = dict(hi=(132, 198, 98), base=(98, 168, 78), dark=(68, 128, 60), out=(38, 76, 42))
GOLD, GOLD_HI, GOLD_D = (228, 178, 60), (255, 230, 128), (164, 116, 30)
FLOWERS = [((246, 150, 170), (255, 214, 96)), ((250, 250, 250), (255, 200, 80)), ((186, 160, 246), (255, 236, 150))]


def lit(x, y, cx, cy, rx, ry):
    """0..1 lighting for a rounded form lit from the top left."""
    nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
    nz = math.sqrt(max(0.0, 1 - nx * nx - ny * ny))
    return -0.5 * nx - 0.6 * ny + 0.62 * nz


def stone_colour(pal, shade, x, y):
    return [pal["hi"], pal["light"], pal["base"], pal["shadow"], pal["deep"]][
        tone(shade, (0.78, 0.55, 0.25, -0.05), x, y, band=0.015)]


def lawn(g):
    """The patch of grass every memorial stands on."""
    cx, cy, rx, ry = 16.0, 28.2, 13.8, 3.2
    for y in range(N):
        for x in range(N):
            nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if nx * nx + ny * ny <= 1 and y <= 30:
                c = GRASS["hi"] if y <= 25 else (GRASS["dark"] if y >= 30 else GRASS["base"])
                if y == 26 and (x * 7) % 5 == 0:
                    c = GRASS["hi"]
                g.put(x, y, c, "grass")


def blob(g, cx, cy, rx, ry, pal, region):
    for y in range(N):
        for x in range(N):
            nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            if nx * nx + ny * ny <= 1:
                g.put(x, y, stone_colour(pal, lit(x, y, cx, cy, rx, ry), x, y), region)


def slab(g, x0, x1, y0, y1, r, pal, region):
    """A chunky headstone: rounded top, bevelled left edge, shadowed right edge."""
    for y in range(y0, y1 + 1):
        k = y - y0
        inset = 0 if k >= r else r - int(math.sqrt(max(0, r * r - (r - k) ** 2)) + 0.5)
        for x in range(x0 + inset, x1 - inset + 1):
            t = (x + 0.5 - x0) / (x1 + 1 - x0)
            if x == x0 + inset or y == y0:
                c = pal["hi"]
            elif x == x1 - inset:
                c = pal["deep"]
            elif t > 0.74 or y == y1:
                c = pal["shadow"]
            elif t < 0.28 or y <= y0 + 1:
                c = pal["light"]
            else:
                c = pal["base"]
            g.put(x, y, c, region)


def box(g, x0, x1, y0, y1, pal, region):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if x == x0 or y == y0:
                c = pal["hi"] if y == y0 else pal["light"]
            elif x == x1 or y == y1:
                c = pal["shadow"] if x != x1 else pal["deep"]
            else:
                c = pal["base"]
            g.put(x, y, c, region)


def flower(g, x, y, petal, heart):
    for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
        g.put(x + dx, y + dy, petal)
    g.put(x, y, heart)
    g.put(x, y + 2, GRASS["dark"])


def sprout(g, p, x0=24):
    cap = p["cap"]
    g.put(x0 + 1, 21, cap[4])
    for x in (x0, x0 + 2):
        g.put(x, 22, cap[4])
    g.put(x0 + 1, 22, cap[1])
    g.put(x0 - 1, 23, cap[4])
    for x in range(x0, x0 + 3):
        g.put(x, 23, cap[2] if x > x0 else cap[1])
    g.put(x0 + 3, 23, cap[4])
    if p["spots"] is SPOTS:
        g.put(x0 + 1, 23, p["spot"][0])
    g.put(x0 + 1, 24, STEM[2])
    g.put(x0 + 1, 25, STEM[3])


def memorial(tier, sp):
    p = SPECIES[sp]
    cap = p["cap"]
    g = Grid()
    lawn(g)

    if tier == 1:  # a small cairn of three pebbles
        blob(g, 16.0, 24.4, 5.4, 2.6, STONE, "stone")
        blob(g, 15.6, 20.6, 4.0, 2.3, STONE, "stone")
        blob(g, 16.4, 17.4, 2.7, 1.9, STONE, "stone")
    elif tier == 2:  # a small, plain headstone
        slab(g, 11, 20, 14, 26, 5, STONE, "stone")
    elif tier == 3:  # a headstone with a nameplate and a carved emblem
        slab(g, 9, 22, 8, 26, 7, STONE, "stone")
    elif tier == 4:  # an urn on a pedestal, glazed in the species' colours
        box(g, 10, 21, 25, 26, STONE, "stone")
        box(g, 11, 20, 20, 24, STONE, "stone")
        box(g, 10, 21, 19, 19, STONE, "stone")
        urn = dict(hi=cap[0], light=cap[1], base=cap[2], shadow=cap[3], deep=cap[4], out=STONE["out"])
        for x in range(14, 18):
            g.put(x, 18, urn["shadow"], "urn")          # foot
        blob(g, 16.0, 13.6, 4.8, 4.4, urn, "urn")       # body
        for x in range(14, 18):
            g.put(x, 9, urn["base"], "urn")              # neck
        for x in range(13, 19):
            g.put(x, 8, urn["light"], "urn")             # lip
    else:  # a marble statue of the Plynling on a plinth
        box(g, 9, 22, 25, 26, MARBLE, "marble")
        box(g, 10, 21, 19, 24, MARBLE, "marble")
        box(g, 9, 22, 18, 18, MARBLE, "marble")
        for y in range(12, 18):                           # statue: a stout stem
            for x in range(13, 19):
                c = stone_colour(MARBLE, lit(x, 14, 16, 14, 3.4, 9), x, y)
                g.put(x, y, MARBLE["shadow"] if y == 12 else c, "marble")
        for y in range(N):                                # statue: a wide, flat cap
            for x in range(N):
                if y <= 11 and ((x + 0.5 - 16) / 9.4) ** 2 + ((y + 0.5 - 12.0) / 6.0) ** 2 <= 1:
                    c = stone_colour(MARBLE, lit(x, y, 16, 12.0, 9.4, 6.0), x, y)
                    g.put(x, y, MARBLE["shadow"] if y == 11 else c, "marble")

    region_out = {"stone": STONE["out"], "marble": MARBLE["out"], "grass": GRASS["out"], "urn": cap[4]}
    g.outline(lambda reg, ny: region_out.get(reg, STONE["out"]))

    # the front of the lawn goes back over the memorial's foot, so it reads as standing in grass
    for y in range(27, 31):
        for x in range(N):
            nx, ny = (x + 0.5 - 16.0) / 13.8, (y + 0.5 - 28.2) / 3.2
            if nx * nx + ny * ny <= 1:
                g.put(x, y, GRASS["dark"] if y >= 30 else GRASS["base"], "grass")
    for x in (4, 7, 10, 21, 25, 28):
        g.put(x, 25 if x in (7, 25) else 26, GRASS["dark"])

    if tier == 2:
        for x in range(13, 19):
            g.put(x, 19, STONE["shadow"])
    if tier == 3:
        for x, y in ((13, 11), (14, 10), (15, 10), (16, 10), (17, 10), (18, 11)):
            g.put(x, y, STONE["shadow"])                   # carved emblem: a little cap
        g.put(15, 12, STONE["shadow"])
        g.put(16, 12, STONE["shadow"])
        box(g, 11, 20, 15, 21, STONE, "plate")             # the nameplate
        for x in range(12, 20):
            g.put(x, 15, STONE["deep"])
            g.put(x, 21, STONE["hi"])
        for x in (12, 20):
            for y in range(15, 22):
                g.put(x, y, STONE["deep"] if x == 12 else STONE["hi"])
        for x in range(14, 19):
            g.put(x, 17, STONE["deep"])
        for x in range(15, 18):
            g.put(x, 19, STONE["deep"])
        flower(g, 6, 26, *FLOWERS[0])
    if tier == 4:
        for x in range(12, 21):
            if g.r[14][x] == "urn":
                g.put(x, 14, GOLD if x != 13 else GOLD_HI)   # gold band round the urn
        if p["spots"] is SPOTS:
            g.put(15, 12, p["spot"][0])
            g.put(17, 16, p["spot"][0])
        flower(g, 6, 26, *FLOWERS[0])
        flower(g, 26, 26, *FLOWERS[2])
    if tier == 5:
        for x, y in ((14, 14), (17, 14)):
            g.put(x, y, MARBLE["deep"])                    # the statue's closed eyes
        g.put(15, 16, MARBLE["deep"])
        g.put(16, 16, MARBLE["deep"])
        box(g, 13, 18, 20, 23, dict(hi=GOLD_HI, light=GOLD, base=GOLD, shadow=GOLD_D, deep=GOLD_D), "gold")
        g.put(15, 21, cap[1])                              # a gem in the species' colour
        g.put(16, 21, cap[2])
        g.put(15, 22, cap[3])
        g.put(16, 22, cap[2])
        flower(g, 5, 26, *FLOWERS[0])
        flower(g, 8, 27, *FLOWERS[1])
        flower(g, 24, 27, *FLOWERS[2])
        flower(g, 27, 26, *FLOWERS[0])

    if tier <= 3:
        sprout(g, p, 24)

    im = g.image()
    if tier == 5:  # a faint shimmer
        for x, y in ((4, 6), (27, 4), (26, 12), (5, 14)):
            im.putpixel((x, y), GOLD_HI + (220,))
    return im


def up(im, k):
    return im.resize((N * k, N * k), Image.NEAREST)


def sheet(rows, cols, cell, row_labels, col_labels, name, K=5):
    GAP, TOP, LEFT = 14, 30, 130
    T = N * K
    out = Image.new("RGBA", (LEFT + len(cols) * (T + GAP), TOP + len(rows) * (T + GAP)), (47, 49, 54, 255))
    d = ImageDraw.Draw(out)
    for j, lab in enumerate(col_labels):
        d.text((LEFT + j * (T + GAP) + T // 2, TOP // 2), lab, fill=(220, 221, 222), anchor="mm", font=LABEL)
    for i, r in enumerate(rows):
        d.text((LEFT // 2, TOP + i * (T + GAP) + T // 2), row_labels[i], fill=(220, 221, 222), anchor="mm", font=LABEL)
        for j, c in enumerate(cols):
            out.alpha_composite(up(cell(r, c), K), (LEFT + j * (T + GAP), TOP + i * (T + GAP)))
    out.save(name)
    return out.size


if __name__ == "__main__":
    species = list(SPECIES)
    states = ["happy", "content", "sad", "hungry", "frozen"]
    print(sheet(species, states, lambda sp, st: build(st, sp),
                [f"{sp} ({SPECIES[sp]['tier']})" for sp in species], states, "plynling_mushrooms_v4.png"))
    mem_sp = ["amanite", "russule", "dore"]
    print(sheet(mem_sp, [1, 2, 3, 4, 5], lambda sp, t: memorial(t, sp), mem_sp,
                ["1 cairn", "2 petite stèle", "3 stèle gravée", "4 urne", "5 statue"], "plynling_memorials_v2.png"))
