"""Animates the seasonal avatars (ProjectSYNCS/Assets/Avatars/syncs-<season>.png) into looping GIFs:
leaves falling in autumn, petals drifting in spring, snow in winter, and in summer sunbeams falling
across her with dust glinting in them, a glint across her sunglasses and heat rising (her sweat drop stays still: sliding, it
looked wrong).

Writes, per season:
- ProjectSYNCS/Assets/Avatars/syncs-<season>.gif, the one the bot uploads (SeasonalAvatarService):
  zoomed in by ZOOM_KEEP, 900×900;
- out/syncs-<season>.gif, the full 1000×1000 picture (the approved ones are copied by hand to
  docs/assets/avatars/), and a few PNG frames to look at (out/syncs-<season>-f<n>.png).

    python -I animate.py [season …]      (every season when none is named)

The PNG is the source: nothing is redrawn by hand. The art is worked at its true 200×200 pixels
(Gemini's grid, docs/syncs-appearance.md) and scaled back ×5 nearest-neighbour, so it stays crisp.
- The loose bits floating on the background (the drifting leaf, the petals, the snowflakes) are
  found as the small pieces not attached to her, cut out, and become the particles' sprites; their
  spot goes back to background.
- The background's faint JPEG-era speckle is flattened to one pink: invisible, but it defeats GIF
  compression.
- A particle "behind" her is drawn on background pixels only, so it passes behind the cap or the
  shoulders; the others pass in front.
- Every motion is periodic in the loop (whole falls, whole sway cycles), so the GIF loops seamlessly.
- One shared 256-colour palette, no dithering; the green LED and every sprite colour are pinned in
  it exactly (median cut alone drops the LED, which is a handful of pixels). Each frame after the
  first stores only what changed, the rest transparent over the previous frame: about 600 KB at
  1000×1000 instead of 10 MB. The result is decoded back and compared with what was drawn.

docs/assets/avatars/syncs-autumn-v1.gif is the first autumn one, kept as a reference.
"""
import colorsys
import math
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.normpath(os.path.join(HERE, "..", "..", "ProjectSYNCS", "Assets", "Avatars"))
OUT = os.path.join(HERE, "out")

N = 200         # true pixels
SCALE = 5       # back to the PNG's 1000×1000
DELAY = 70      # ms per frame
BG_DISTANCE = 15
T = 255         # palette index meaning "unchanged since the previous frame"
# The GIF the bot uploads comes zoomed in, like the PNG it replaces: SeasonalAvatarService.ZoomKeep
# crops the PNG at upload time, but it cannot crop a GIF, so the zoom is done here. Keep the two equal.
ZOOM_KEEP = 0.9


class Avatar:
    def __init__(self, season):
        full = Image.open(os.path.join(SRC, f"syncs-{season}.png")).convert("RGB")
        self.base = full.resize((N, N), Image.NEAREST)
        p = self.px = self.base.load()
        edge = [p[x, y] for x in range(N) for y in (0, N - 1)] + [p[x, y] for y in range(N) for x in (0, N - 1)]
        self.bg = tuple(sorted(c[i] for c in edge)[len(edge) // 2] for i in range(3))

        # Background: bg-coloured pixels reachable from the edge.
        bgmask = self.bgmask = [[False] * N for _ in range(N)]
        stack = [(x, y) for x in range(N) for y in (0, N - 1)] + [(x, y) for y in range(N) for x in (0, N - 1)]
        while stack:
            x, y = stack.pop()
            if not (0 <= x < N and 0 <= y < N) or bgmask[y][x] or not self.is_bg(p[x, y]): continue
            bgmask[y][x] = True
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]

        # Everything else, in 8-connected pieces: the biggest is her, the rest float.
        seen = set(); pieces = []
        for y in range(N):
            for x in range(N):
                if bgmask[y][x] or (x, y) in seen: continue
                piece = []; stack = [(x, y)]; seen.add((x, y))
                while stack:
                    a, b = stack.pop(); piece.append((a, b))
                    for dx in (-1, 0, 1):
                        for dy in (-1, 0, 1):
                            c, d = a + dx, b + dy
                            if 0 <= c < N and 0 <= d < N and not bgmask[d][c] and (c, d) not in seen:
                                seen.add((c, d)); stack.append((c, d))
                pieces.append(piece)
        pieces.sort(key=len, reverse=True)

        # Cut the floating bits out, biggest first.
        self.particles = []
        for piece in pieces[1:]:
            xs = [a for a, _ in piece]; ys = [b for _, b in piece]
            x0, y0 = min(xs), min(ys)
            sprite = Image.new("RGBA", (max(xs) - x0 + 1, max(ys) - y0 + 1), (0, 0, 0, 0))
            sp = sprite.load()
            for a, b in piece:
                sp[a - x0, b - y0] = p[a, b] + (255,)
                bgmask[b][a] = True
            self.particles.append(sprite)

        for y in range(N):
            for x in range(N):
                if bgmask[y][x]: p[x, y] = self.bg

    def is_bg(self, c):
        return sum((a - b) ** 2 for a, b in zip(c, self.bg)) < BG_DISTANCE ** 2


def recolor(sprite, dh, ds=1.0, dv=1.0):
    """Shifts hue/saturation/value of the coloured pixels; the navy outline and greys stay."""
    out = sprite.copy(); p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if not a: continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if s < 0.3 or v < 0.35: continue
            h = (h + dh) % 1; s = min(1, s * ds); v = min(1, v * dv)
            r, g, b = colorsys.hsv_to_rgb(h, s, v)
            p[x, y] = (round(r * 255), round(g * 255), round(b * 255), 255)
    return out


def scaled(sprite, k):
    return sprite.resize((max(1, round(sprite.width * k)), max(1, round(sprite.height * k))), Image.NEAREST)


def place(av, frame, sprite, cx, cy, behind):
    fp = frame.load(); sp = sprite.load()
    ox = round(cx - sprite.width / 2); oy = round(cy - sprite.height / 2)
    for y in range(sprite.height):
        for x in range(sprite.width):
            r, g, b, a = sp[x, y]
            X, Y = ox + x, oy + y
            if a < 128 or not (0 <= X < N and 0 <= Y < N): continue
            if behind and not av.bgmask[Y][X]: continue
            fp[X, Y] = (r, g, b)


# The eight ways a small sprite can lie, for petals and flakes tumbling a quarter turn at a time.
ORIENTATIONS = [None, Image.ROTATE_90, Image.ROTATE_180, Image.ROTATE_270,
                Image.FLIP_LEFT_RIGHT, Image.TRANSPOSE, Image.FLIP_TOP_BOTTOM, Image.TRANSVERSE]


def oriented(sprite, i):
    o = ORIENTATIONS[i % 8]
    return sprite if o is None else sprite.transpose(o)


# --- Seasons. Each returns (frame count, draw(frame, t) with t in [0, 1), sprites to pin). ---

def autumn(av):
    leaf = av.particles[0]
    variants = [
        leaf,                               # orange, as drawn
        recolor(leaf, 0.035, 0.95, 1.08),   # golden
        recolor(leaf, -0.03, 1.05, 0.92),   # russet
    ]
    # x lane, start phase, variant, scale, sway amplitude, sway cycles per loop, behind her?
    leaves = [
        (22,  0.00, 0, 1.0, 9, 2, False),
        (176, 0.30, 1, 0.8, 7, 3, False),
        (40,  0.55, 2, 0.8, 6, 3, True),
        (160, 0.80, 0, 0.8, 8, 2, True),
        (100, 0.15, 1, 0.75, 12, 2, True),
        (192, 0.62, 2, 0.9, 6, 2, False),
        (10,  0.42, 1, 0.8, 7, 3, False),
    ]
    margin = 26

    def draw(fr, t):
        for layer in (True, False):  # behind first
            for lx, ph, vi, k, amp, cyc, behind in leaves:
                if behind != layer: continue
                u = (t + ph) % 1
                angle = 2 * math.pi * (cyc * t + ph)
                cx = lx + amp * math.sin(angle)
                cy = -margin + u * (N + 2 * margin)
                # Tilts with the sway and flips as it swings back, like a leaf rocking.
                spr = scaled(variants[vi], k)
                if math.cos(angle) < 0: spr = spr.transpose(Image.FLIP_LEFT_RIGHT)
                spr = spr.rotate(35 * math.cos(angle), resample=Image.NEAREST, expand=True)
                place(av, fr, spr, cx, cy, behind)

    return 90, draw, variants


def spring(av):
    petals = av.particles[:3]
    petals.append(recolor(petals[0], 0.0, 0.6, 1.04))  # a paler one
    # Carried by a breeze from the left: start lane, phase, petal, drift across the fall, sway
    # amplitude, sway cycles, tumble steps per loop, behind her?
    flight = [
        (-10, 0.00, 0, 70, 5, 2, 16, False),
        (30,  0.22, 1, 60, 4, 3, 24, True),
        (90,  0.45, 2, 55, 6, 2, 16, True),
        (120, 0.70, 3, 65, 4, 3, 24, False),
        (-30, 0.58, 1, 80, 5, 2, 16, False),
        (150, 0.10, 0, 50, 3, 3, 8, True),
        (60,  0.85, 3, 60, 5, 2, 24, False),
        (5,   0.33, 2, 75, 4, 3, 16, True),
    ]
    margin = 12

    def draw(fr, t):
        for layer in (True, False):
            for lx, ph, pi, drift, amp, cyc, steps, behind in flight:
                if behind != layer: continue
                u = (t + ph) % 1
                angle = 2 * math.pi * (cyc * t + ph)
                cx = lx + drift * u + amp * math.sin(angle)
                cy = -margin + u * (N + 2 * margin)
                spr = oriented(petals[pi], int((t + ph) * steps))
                place(av, fr, spr, cx, cy, behind)

    return 90, draw, petals


def winter(av):
    flakes = av.particles  # crystals first (biggest), then dots
    crystals = [f for f in flakes if f.width >= 3]
    dots = [f for f in flakes if f.width < 3] or [crystals[-1]]
    rng = __import__("random").Random(7)
    snow = []
    # Far snow: dots, behind her, slow. Near snow: crystals, some in front, a little faster.
    for i in range(18):
        snow.append(dict(x=rng.uniform(-5, N + 5), ph=rng.random(), spr=dots[i % len(dots)], amp=rng.uniform(1, 3),
                         cyc=rng.choice((1, 2)), extra=0, behind=True, steps=0))
    for i in range(9):
        snow.append(dict(x=rng.uniform(0, N), ph=rng.random(), spr=crystals[i % len(crystals)], amp=rng.uniform(2, 5),
                         cyc=rng.choice((1, 2)), extra=rng.choice((0, 40, 80)), behind=i % 3 == 0, steps=rng.choice((0, 4, 8))))
    margin = 10

    def draw(fr, t):
        for layer in (True, False):
            for f in snow:
                if f["behind"] != layer: continue
                u = (t + f["ph"]) % 1
                angle = 2 * math.pi * (f["cyc"] * t + f["ph"])
                # A faster flake travels further than the picture each loop and waits below it.
                cy = -margin + u * (N + 2 * margin + f["extra"])
                cx = f["x"] + f["amp"] * math.sin(angle)
                spr = oriented(f["spr"], int((t + f["ph"]) * f["steps"])) if f["steps"] else f["spr"]
                place(av, fr, spr, cx, cy, f["behind"])

    return 120, draw, flakes


# Summer: where things are on her face, in true pixels (see the PNG at ×5).
LENSES = [(85, 106, 105, 116), (118, 106, 140, 116)]


def summer(av):
    p = av.px
    lens = {(x, y) for x0, y0, x1, y1 in LENSES for x in range(x0, x1) for y in range(y0, y1)
            if sum(p[x, y]) / 3 < 130}

    white = (255, 255, 255); shine = (214, 214, 240)
    heat = tuple(min(255, c + 14) for c in av.bg)
    # Sunlight: the background warmed toward cream, in three steps so the beams stay pixel art.
    sun = (255, 236, 196)
    lit = [tuple(round(b + (s - b) * k) for b, s in zip(av.bg, sun)) for k in (0.16, 0.27, 0.38)]
    # Dust, from barely lit to catching the light full on.
    dust = [tuple(round(b + (s - b) * 0.8) for b, s in zip(av.bg, sun)), sun, (255, 250, 234)]
    swatch = Image.new("RGBA", (9, 1))
    swatch.putdata([c + (255,) for c in (white, shine, heat, *lit, *dust)])

    frames = 90
    sweep = (20, 38)   # frames the glint crosses both lenses
    sparkle_at = (139, 106)  # the right lens's outer corner

    # The sunbeam: shafts fanning from a sun just off the top-left corner, the side the glint comes
    # from. Discord crops the avatar round and that corner is the first thing cut, so the shafts run
    # long and fall across her too, softer on her than on the background. Each breathes (width and
    # reach) once per loop, out of step with the others. Every reach runs past the frame's edge even
    # at its shortest: a shaft ending inside the picture shows a blunt tip.
    sun_at = (-22, -32)
    # direction (degrees below the horizontal), half-width (degrees), reach, phase
    shafts = [(36, 3.4, 320, 0.0), (51, 5.0, 345, 0.35), (67, 3.6, 290, 0.7)]
    on_her = (0.07, 0.11, 0.15)   # how far her own colours are warmed toward the sun, per step

    # Dust floating everywhere, only seen where the light catches it, like a light-shaft shader:
    # each speck loops a slow closed path (whole cycles, so the GIF loops) and twinkles, so specks
    # drift into a shaft, glint, and fade out of it. A few big ones show as a small cross. Over her,
    # only the bright ones: a dim speck on her face reads as a freckle, not as light.
    rng = __import__("random").Random(11)
    motes = [dict(x=rng.uniform(-10, N + 10), y=rng.uniform(-10, N + 10),
                  ax=rng.uniform(3, 9), ay=rng.uniform(4, 12), cx=rng.choice((1, 1, 2)), cy=rng.choice((1, 2)),
                  px=rng.random(), py=rng.random(), tw=rng.choice((1, 2, 3)), pt=rng.random(),
                  big=rng.random() < 0.18)
             for _ in range(230)]

    def beam(x, y, t):
        """How lit a pixel is: 0 (dark) to 3 (the bright middle of a shaft, near the sun)."""
        dx, dy = x - sun_at[0], y - sun_at[1]
        a = math.degrees(math.atan2(dy, dx)); r = math.hypot(dx, dy)
        best = 0
        for deg, hw, reach, ph in shafts:
            s = math.sin(2 * math.pi * (t + ph))
            w = hw * (1 + 0.3 * s); far = reach * (1 + 0.12 * s)
            off = abs(a - deg) / w
            if off > 1 or r > far: continue
            # Brightest along the middle, nearer the sun; softer toward the edges and the far end.
            level = 3 if off < 0.3 else 2 if off < 0.65 else 1
            if r > far * 0.75: level = min(level, 1)
            elif r > far * 0.5: level = min(level, 2)
            best = max(best, level)
        return best

    def draw(fr, t):
        f = round(t * frames)
        fp = fr.load()
        for y in range(N):
            for x in range(N):
                level = beam(x, y, t)
                if not level: continue
                if av.bgmask[y][x]: fp[x, y] = lit[level - 1]
                else:
                    k = on_her[level - 1]
                    fp[x, y] = tuple(round(c + (s - c) * k) for c, s in zip(fp[x, y], sun))
        for m in motes:
            X = round(m["x"] + m["ax"] * math.sin(2 * math.pi * (m["cx"] * t + m["px"])))
            Y = round(m["y"] + m["ay"] * math.sin(2 * math.pi * (m["cy"] * t + m["py"])))
            if not (0 <= X < N and 0 <= Y < N): continue
            level = beam(X, Y, t)
            if not level: continue
            twinkle = 0.5 + 0.5 * math.sin(2 * math.pi * (m["tw"] * t + m["pt"]))
            glow = level / 3 * (0.35 + 0.65 * twinkle)   # 0..1
            if glow < 0.2 or (glow < 0.55 and not av.bgmask[Y][X]): continue
            tone = dust[0] if glow < 0.4 else dust[1] if glow < 0.7 else dust[2]
            fp[X, Y] = tone
            if m["big"] and glow >= 0.55:
                for ex, ey in ((X + 1, Y), (X - 1, Y), (X, Y + 1), (X, Y - 1)):
                    if 0 <= ex < N and 0 <= ey < N: fp[ex, ey] = dust[0] if glow < 0.75 else dust[1]
        # Heat rising from below, behind her: short wavy strands.
        for hx, ph in ((24, 0.0), (44, 0.5), (158, 0.25), (180, 0.75), (100, 0.6)):
            u = (t + ph) % 1
            cy = N + 10 - u * 70
            for i in range(9):
                X = round(hx + 1.5 * math.sin(i * 0.9 + 2 * math.pi * (2 * t + ph)))
                Y = round(cy - i)
                if 0 <= X < N and 0 <= Y < N and av.bgmask[Y][X]: fp[X, Y] = heat
        # The glint: a "/" band across both lenses, parallel to their streaks.
        if sweep[0] <= f < sweep[1]:
            pos = 80 + (f - sweep[0]) * 4
            for x, y in lens:
                d = x + y - 106 - pos
                if 0 <= d < 3: fp[x, y] = white
                elif d in (-1, 3): fp[x, y] = shine
        # …and a sparkle on the right lens's corner once it has passed.
        if sweep[1] <= f < sweep[1] + 8:
            r = (1, 2, 3, 3, 2, 2, 1, 1)[f - sweep[1]]
            sx, sy = sparkle_at
            fp[sx, sy] = white
            for k in range(1, r + 1):
                for X, Y in ((sx + k, sy), (sx - k, sy), (sx, sy + k), (sx, sy - k)):
                    if 0 <= X < N and 0 <= Y < N: fp[X, Y] = white if k < r else shine

    return frames, draw, [swatch]


SEASONS = {"spring": spring, "summer": summer, "autumn": autumn, "winter": winter}


def render(season):
    av = Avatar(season)
    frames_n, draw, sprites = SEASONS[season](av)
    frames = []
    for f in range(frames_n):
        fr = av.base.copy()
        draw(fr, f / frames_n)
        frames.append(fr)

    pinned = set()
    for y in range(52, 70):          # the LED
        for x in range(140, 158):
            r, g, b = av.px[x, y]
            if g > r + 20: pinned.add((r, g, b))
    for s in sprites:
        for c in s.get_flattened_data():
            if c[3]: pinned.add(c[:3])
    pinned = sorted(pinned)[:120]
    strip = Image.new("RGB", (N, N * 4))
    for i, fi in enumerate((0, frames_n // 4, frames_n // 2, 3 * frames_n // 4)):
        strip.paste(frames[fi], (0, N * i))
    rest = strip.quantize(colors=255 - len(pinned), method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = Image.new("P", (1, 1))
    pal.putpalette(rest.getpalette()[:3 * (255 - len(pinned))] + [c for rgb in pinned for c in rgb] + [0, 255, 0])

    idx = [fr.quantize(palette=pal, dither=Image.Dither.NONE) for fr in frames]
    os.makedirs(OUT, exist_ok=True)
    full = os.path.join(OUT, f"syncs-{season}.gif")
    shipped = os.path.join(SRC, f"syncs-{season}.gif")
    m = round(N * (1 - ZOOM_KEEP) / 2)
    results = [encode(idx, (0, 0, N, N), full), encode(idx, (m, m, N - m, N - m), shipped)]
    for f in (0, frames_n // 3, 2 * frames_n // 3):
        idx[f].convert("RGB").resize((N * SCALE, N * SCALE), Image.NEAREST).save(
            os.path.join(OUT, f"syncs-{season}-f{f}.png"))
    print(f"{season}: {frames_n} frames, {len(av.particles)} particles cut, {len(pinned)} colours pinned")
    for path, (size, bad) in zip((full, shipped), results):
        print(f"  {os.path.relpath(path, HERE)}: {size // 1024} KB, "
              f"{'decodes exactly' if not bad else f'{bad} FRAMES DIFFER'}")


def encode(idx, box, path):
    """Writes the frames cropped to `box` (true pixels), each storing only what changed, then
    decodes the file back and counts the frames that differ from what was drawn."""
    idx = [i.crop(box) for i in idx]
    w, h = idx[0].size
    out = [idx[0]]
    for prev, cur in zip(idx, idx[1:]):
        d = cur.copy(); dp = d.load(); pp = prev.load()
        for y in range(h):
            for x in range(w):
                if dp[x, y] == pp[x, y]: dp[x, y] = T
        out.append(d)
    out = [o.resize((w * SCALE, h * SCALE), Image.NEAREST) for o in out]
    out[0].save(path, save_all=True, append_images=out[1:], duration=DELAY, loop=0, optimize=False,
                disposal=1, transparency=T)

    g = Image.open(path); bad = 0
    for f in range(len(idx)):
        g.seek(f)
        want = idx[f].convert("RGB").resize((w * SCALE, h * SCALE), Image.NEAREST)
        if g.convert("RGB").tobytes() != want.tobytes(): bad += 1
    g.close()
    return os.path.getsize(path), bad


if __name__ == "__main__":
    for s in sys.argv[1:] or list(SEASONS):
        render(s)
