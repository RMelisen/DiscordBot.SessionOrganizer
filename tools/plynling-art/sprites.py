"""The face and mood extras every Plynling shares, and build(), which hands each species to
its own drawing in species.py."""
from common import N, SPECIES, STEM, STEM_OUT, INK, sheet

WHITE = (255, 255, 255)
IRIS = (104, 80, 118)                      # the soft reflection low in each eye
PINK, PINK_SOFT = (246, 146, 160), (244, 190, 190)
MOUTH, TONGUE = (192, 58, 72), (255, 142, 152)
TEAR, TEAR_HI = (130, 200, 255), (220, 242, 255)
LID = (150, 122, 104)
PALE = (206, 204, 204)


def build(state, sp, frame=0, shadow=True, stage="adult"):
    """The living Plynling of species `sp` in one of the six moods, at a life stage. Each species
    draws its own silhouette in species.py, and every one of them wears the face below. Only
    species with baby art (export.STAGED) accept a stage other than "adult"."""
    from species import DRAW          # imported here: species.py imports this module
    if stage == "adult":
        return DRAW[sp](state, frame, shadow)
    return DRAW[sp](state, frame, shadow, stage=stage)


# ---- the face -------------------------------------------------------------------------
# Eyes sit at columns 12-13 and 18-19, rows 19-21; the mouth at rows 23-25 — before the
# (ox, oy) offset. Every species wears this face; skin / skin_out replace the stem tones
# used for the eye bags and the brows, so a tinted stem keeps a matching face.

def face(g, state, f, ox=0, oy=0, skin=None, skin_out=None, blink=False, tear=0, drool=0):
    """blink closes open eyes for a frame; tear (0 at rest, 1-3 further down, -1 only just
    welling) rolls the sad tear; drool (0 or 1) lets the hungry drool drip a pixel longer."""
    skin = STEM if skin is None else skin
    skin_out = STEM_OUT if skin_out is None else skin_out

    def P(x, y, c):
        g.put(x + ox, y + f + oy, c)

    def glossy_eye(x0, shine2=False, watery=False):
        if blink:
            closed_eye(x0)
            return
        for x in (x0, x0 + 1):
            for y in (19, 20, 21):
                P(x, y, INK)
        P(x0, 19, WHITE)                               # the shine
        P(x0 + 1, 21, TEAR if watery else IRIS)        # a soft reflection, or welling up
        if shine2:
            P(x0 + 1, 20, WHITE)                       # a second sparkle: wanting something

    def happy_eye(x0):                                 # closed, smiling "^"
        P(x0 - 1, 20, INK)
        P(x0, 19, INK)
        P(x0 + 1, 19, INK)
        P(x0 + 2, 20, INK)

    def tired_eye(x0):                                 # half-closed, heavy-lidded
        for x in (x0 - 1, x0, x0 + 1, x0 + 2):
            P(x, 20, LID)
        P(x0, 21, INK)
        P(x0 + 1, 21, INK)
        P(x0, 22, skin[4])                             # bags under the eyes
        P(x0 + 1, 22, skin[4])

    def closed_eye(x0):
        for x in (x0 - 1, x0, x0 + 1, x0 + 2):
            P(x, 20, INK)

    def blush(c):
        for x in (10, 11, 20, 21):
            P(x, 22, c)

    def brows(inner_raise=True, flat=False):
        if flat:
            for x in (11, 12, 13, 18, 19, 20):
                P(x, 17, skin_out)
            return
        for x, y in ((11, 18), (12, 17), (19, 17), (20, 18)) if not inner_raise else \
                    ((11, 17), (12, 17), (13, 16), (18, 16), (19, 17), (20, 17)):
            P(x, y, INK if inner_raise else skin_out)

    if state == "happy":
        happy_eye(12)
        happy_eye(18)
        for x, c in ((14, INK), (15, MOUTH), (16, MOUTH), (17, INK)):   # open laugh
            P(x, 23, c)
        P(15, 24, TONGUE)
        P(16, 24, TONGUE)
        P(14, 24, INK)
        P(17, 24, INK)
        P(15, 25, INK)
        P(16, 25, INK)
        blush(PINK)
    elif state == "content":
        glossy_eye(12)
        glossy_eye(18)
        for x, y in ((13, 23), (14, 24), (15, 23), (16, 23), (17, 24), (18, 23)):   # a little "ω"
            P(x, y, INK)
        blush(PINK_SOFT)
    elif state == "sad":
        glossy_eye(12, watery=True)
        glossy_eye(18, watery=True)
        brows(inner_raise=True)
        for x, y in ((14, 24), (15, 23), (16, 23), (17, 24)):
            P(x, y, INK)
        if tear == -1:
            P(12, 22, TEAR_HI)                                          # a new one welling up
        elif tear is not None:
            P(12, 22 + tear, TEAR_HI)                                   # a tear rolling down
            P(12, 23 + tear, TEAR)
            P(11, 24 + tear, TEAR)
    elif state == "hungry":
        glossy_eye(12, shine2=True)
        glossy_eye(18, shine2=True)
        P(15, 23, INK)
        P(16, 23, INK)
        P(14, 24, INK)
        P(15, 24, MOUTH)
        P(16, 24, MOUTH)
        P(17, 24, INK)
        P(15, 25, INK)
        P(16, 25, INK)
        P(17, 25, TEAR)                                                 # drooling
        P(17, 26, TEAR if drool else TEAR_HI)
        if drool:
            P(17, 27, TEAR_HI)
        blush(PINK_SOFT)
    elif state == "starving":
        tired_eye(12)
        tired_eye(18)
        brows(inner_raise=True)
        for x, y in ((13, 25), (14, 24), (15, 25), (16, 24), (17, 25), (18, 24)):   # wobbling
            P(x, y, INK)
    elif state == "frozen":
        closed_eye(12)
        closed_eye(18)
        for x in range(14, 18):                                         # a closed, flat mouth
            P(x, 24, INK)


SPARKLES = [(2, 6), (29, 9), (27, 1), (3, 1)]
SPARKLES_ALT = [(1, 9), (30, 5), (26, 3), (5, 2)]
STAR = (220, 246, 255)


def star(px, pose):
    """The frost star at the top right: a small cross that shrinks to a point, then flares."""
    phase = pose.star if pose else 0
    px(28, 3, STAR)
    if phase >= 0:
        for x, y in ((28, 2), (27, 3), (29, 3), (28, 4)):
            px(x, y, STAR)
    if phase == 1:
        for x, y in ((28, 1), (26, 3), (30, 3), (28, 5)):
            px(x, y, STAR, 150)


def sparkles(px, s, pose):
    """A rare species' sparkles: each one blinks out in turn and glints beside where it was."""
    f = pose.f if pose else 0
    for i, ((x, y), (ax, ay)) in enumerate(zip(SPARKLES, SPARKLES_ALT)):
        if f in (4 * i + 2, 4 * i + 3):
            px(ax, ay, s)
        else:
            px(x, y, s)


def sweat_drops(f):
    """Where the sliding sweat drops are on this frame: None when they have run off."""
    k = f.sweat if f else 0
    return None if k == -1 else (k or 0)


def extras(im, state, p, pose=None, body=(0, 0)):
    """The mood extras. `body` is how far the head has moved this frame, so anything stuck to
    the face (the sweat) moves with it; the heart, the frost and the sparkles float free."""
    def px(x, y, c, a=255):
        if 0 <= x < N and 0 <= y < N:
            im.putpixel((x, y), c + (a,))

    bx, by = body
    k = sweat_drops(pose)
    if state == "happy":                                                 # a little heart
        h = pose.heart if pose else 0
        for x, y in ((25, 17), (27, 17), (24, 18), (25, 18), (26, 18), (27, 18), (28, 18),
                     (25, 19), (26, 19), (27, 19), (26, 20)):
            px(x, y + h, (246, 116, 140))
        px(25, 18 + h, (255, 196, 206))
    if state == "hungry" and k is not None:                              # sweat drop
        for x, y in ((25, 17), (25, 18), (24, 18), (25, 19)):
            px(x + bx, y + by + k, TEAR)
        px(25 + bx, 17 + by + k, TEAR_HI)
    if state == "starving" and k is not None:                            # a cold sweat, both sides
        for x, y in ((25, 18), (25, 19), (24, 19), (25, 20), (6, 19), (6, 20), (7, 20), (6, 21)):
            px(x + bx, y + by + k, TEAR)
    if state == "frozen":
        for x, y in ((4, 16), (4, 17), (7, 16), (24, 16), (27, 16), (27, 17), (26, 16)):
            px(x, y, (206, 242, 255))
        for x, y in ((9, 5), (10, 4), (15, 3), (20, 4)):
            px(x, y, WHITE)
        star(px, pose)
    if p["sparkle"] and state not in ("frozen", "starving"):
        sparkles(px, p["sparkle"], pose)


if __name__ == "__main__":
    species = list(SPECIES)
    states = ["happy", "content", "sad", "hungry", "starving", "frozen"]
    print(sheet(species, states, lambda s, st: build(st, s), species, states, "plynling_faces_v5.png", K=4))
    # a close-up of one species, big enough to judge the faces pixel by pixel
    print(sheet(["amanite"], states, lambda s, st: build(st, s), ["amanite"], states,
                "plynling_faces_closeup_v5.png", K=7))
